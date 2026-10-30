"""Local stereo analysis, no cloud/model downloads. Depth is relative, not metric."""
from pathlib import Path
import json
import cv2
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'SpatialWindowUnity/Assets/Resources/PhotoData'
DIAG = ROOT / 'spatial/diagnostics'

def main():
    OUT.mkdir(parents=True, exist_ok=True)
    DIAG.mkdir(parents=True, exist_ok=True)
    original = cv2.imread(str(ROOT / 'spatial/source-sbs.jpg'))
    if original is None or original.shape[1] % 2:
        raise ValueError('Expected an even-width SBS image')
    h, w = original.shape[:2]
    width = 1280
    height = round(h * width / (w / 2))
    left = cv2.resize(original[:, :w//2], (width, height), interpolation=cv2.INTER_AREA)
    right = cv2.resize(original[:, w//2:], (width, height), interpolation=cv2.INTER_AREA)
    sift = cv2.SIFT_create(5000)
    kl, dl = sift.detectAndCompute(left, None)
    kr, dr = sift.detectAndCompute(right, None)
    matches = [a for a,b in cv2.BFMatcher().knnMatch(dl, dr, k=2) if a.distance < .7*b.distance]
    pl = np.float32([kl[m.queryIdx].pt for m in matches])
    pr = np.float32([kr[m.trainIdx].pt for m in matches])
    _, inliers = cv2.findFundamentalMat(pl, pr, cv2.FM_RANSAC, 1.5, .999)
    pl, pr = pl[inliers.ravel()>0], pr[inliers.ravel()>0]
    # Correct small vertical alignment errors without warping the source left image.
    A = np.column_stack((pr, np.ones(len(pr))))
    target = pl[:,1] - pr[:,1]
    keep = np.ones(len(pr), bool)
    for _ in range(5):
        correction = np.linalg.lstsq(A[keep], target[keep], rcond=None)[0]
        keep = np.abs(A @ correction - target) < 1.2
    ax, ay, offset = correction
    right = cv2.warpAffine(right, np.float32([[1,0,0],[ax,1+ay,offset]]), (width,height))
    gray_l, gray_r = [cv2.cvtColor(x, cv2.COLOR_BGR2GRAY) for x in (left,right)]
    def matcher(minimum):
        return cv2.StereoSGBM_create(minDisparity=minimum,numDisparities=64,blockSize=5,
            P1=8*25,P2=32*25,disp12MaxDiff=1,uniquenessRatio=8,
            speckleWindowSize=80,speckleRange=2,mode=cv2.STEREO_SGBM_MODE_SGBM_3WAY)
    disparity = matcher(-8).compute(gray_l,gray_r).astype(np.float32)/16
    reverse = matcher(-56).compute(gray_r,gray_l).astype(np.float32)/16
    yy, xx = np.indices(disparity.shape)
    xr = np.rint(xx-disparity).astype(int)
    reverse_at = reverse[yy,np.clip(xr,0,width-1)]
    valid = (disparity>-8)&(reverse_at>-56)&(xr>=0)&(xr<width)&(np.abs(disparity+reverse_at)<1.5)
    valid[:4]=False; valid[-4:]=False
    valid[:,:64]=False; valid[:,-24:]=False
    if valid.mean()<.25:
        raise ValueError('Insufficient reliable stereo matches; do not build a misleading scene')
    lo, hi = np.percentile(disparity[valid],[5,97])
    # Fill unknown depth from the closest measured pixel; never invent photo content.
    distances, labels = cv2.distanceTransformWithLabels((~valid).astype(np.uint8),cv2.DIST_L2,5,labelType=cv2.DIST_LABEL_PIXEL)
    lookup = np.zeros(int(labels.max())+1,np.float32)
    lookup[labels[valid]] = disparity[valid]
    filled = lookup[labels]
    filled = cv2.bilateralFilter(filled,9,2.0,5.0)
    relative = np.clip((filled-lo)/max(hi-lo,1),0,1)
    # Artistic compression into a comfortable 1.5..4m viewing volume. Not a survey.
    depth = 1/(.25+relative*(1/1.5-.25))
    gw, gh = 257, 193
    small = cv2.resize(depth,(gw,gh),interpolation=cv2.INTER_AREA)
    coverage = cv2.resize((distances<20).astype(np.uint8),(gw,gh),interpolation=cv2.INTER_NEAREST)
    coverage[:,:14]=0; coverage[:,-5:]=0
    count, components, stats, _ = cv2.connectedComponentsWithStats(coverage,8)
    for index in range(1,count):
        if stats[index,cv2.CC_STAT_AREA]<500:
            coverage[components==index]=0
    data = {'width':gw,'height':gh,'aspect':width/height,'horizontalFov':50,
            'depth':small.ravel().round(5).tolist(),'valid':coverage.ravel().tolist()}
    (OUT/'depth.json').write_text(json.dumps(data,separators=(',',':')))
    cv2.imwrite(str(OUT/'left.jpg'),left,[cv2.IMWRITE_JPEG_QUALITY,95])
    # Preserve both original views for a true stereo comparison (4:3 per eye).
    cv2.imwrite(str(OUT/'stereo.jpg'),cv2.resize(original,(4096,1536),interpolation=cv2.INTER_AREA),[cv2.IMWRITE_JPEG_QUALITY,95])
    colored = cv2.applyColorMap((relative*255).astype(np.uint8),cv2.COLORMAP_TURBO)
    colored[distances>=20]=0
    cv2.imwrite(str(DIAG/'depth-check.jpg'),np.hstack((left,colored)))
    report = {'original_size':[w,h],'feature_inliers':len(pl),'vertical_fit_inliers':int(keep.sum()),
              'vertical_error_median_px':float(np.median(np.abs(A@correction-target))),
              'reliable_pixel_fraction':float(valid.mean()),'disparity_percentiles':[float(lo),float(hi)],
              'note':'Relative stereo-derived depth; no camera calibration; unknown geometry masked.'}
    (DIAG/'report.json').write_text(json.dumps(report,indent=2))
    print(json.dumps(report,indent=2))

if __name__=='__main__': main()
