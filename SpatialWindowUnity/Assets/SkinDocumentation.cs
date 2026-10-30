using System;
using System.IO;
using UnityEngine;
using Unity.XR.XREAL;

// Local photographic documentation only. No lesion classification or diagnostic scores.
public class SkinDocumentation : MonoBehaviour
{
    public string Status { get; private set; }="Nur mit Einverständnis · FOTO startet Kamera · keine Diagnose";
    Material previewMaterial, recentMaterial, olderMaterial;
    Transform preview, recent, older;
    TextMesh recentLabel, olderLabel;
    Texture2D recentTexture, olderTexture;
    string folder;
    bool comparison;
    float lastFrame=-100, started;
#if UNITY_ANDROID && !UNITY_EDITOR
    XREALRGBCameraTexture cameraSource;
#endif
    void Start()
    {
        folder=Path.Combine(Application.persistentDataPath,"skin-documentation");
        previewMaterial=new Material(Resources.Load<Material>("SkinCameraMaterial"));
        recentMaterial=new Material(Resources.Load<Material>("PhotoMaterial"));
        olderMaterial=new Material(Resources.Load<Material>("PhotoMaterial"));
        preview=Panel("Live",Vector3.zero,previewMaterial);
        recent=Panel("Latest",new Vector3(-.64f,0,0),recentMaterial);
        older=Panel("Previous",new Vector3(.64f,0,0),olderMaterial);
        recentLabel=Caption(new Vector3(-.64f,-.51f,2),"Noch keine Aufnahme");
        olderLabel=Caption(new Vector3(.64f,-.51f,2),"Noch keine Vergleichsaufnahme");
        Caption(new Vector3(0,.65f,2),"HAUT-DOKUMENTATION · LOKAL\nKeine Diagnose · keine automatische Messung");
        Reload(); Show();
    }
    Transform Panel(string name,Vector3 offset,Material mat)
    {
        var panel=GameObject.CreatePrimitive(PrimitiveType.Quad);panel.name=name;
        Destroy(panel.GetComponent<Collider>());panel.transform.SetParent(transform,false);
        panel.transform.localPosition=offset+new Vector3(0,0,2);
        panel.transform.localScale=new Vector3(1.2f,.9f,1);
        panel.GetComponent<Renderer>().sharedMaterial=mat;return panel.transform;
    }
    TextMesh Caption(Vector3 position,string value)
    {
        var t=new GameObject("Caption").AddComponent<TextMesh>();t.transform.SetParent(transform,false);
        t.transform.localPosition=position;t.fontSize=48;t.characterSize=.018f;
        t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=Color.white;t.text=value;return t;
    }
    void Show()
    {
        if(preview==null)return;
        preview.gameObject.SetActive(!comparison&&lastFrame>0);
        recent.gameObject.SetActive(comparison&&recentTexture!=null);
        older.gameObject.SetActive(comparison&&olderTexture!=null);
        recentLabel.gameObject.SetActive(comparison);olderLabel.gameObject.SetActive(comparison);
    }
    public void ToggleComparison(){comparison=!comparison;Show();Status=comparison?"Letzte Aufnahme links · vorherige rechts · Aufnahmezeit unter dem Bild":"Live-Vorschau · FOTO für eine neue Aufnahme";}
    public void Capture()
    {
        if(previewMaterial==null)return;
#if UNITY_ANDROID && !UNITY_EDITOR
        if(!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
            Status="Kamerazugriff bestätigen · danach erneut FOTO drücken";return;
        }
        if(cameraSource==null)
        {
            try {
                cameraSource=XREALRGBCameraTexture.CreateSingleton();
                cameraSource.OnRGBCameraUpdate+=Frame;
                if(!cameraSource.StartCapture()){StopCamera();Status="Eye-Kamera nicht verfügbar · Anschluss prüfen";return;}
                started=Time.unscaledTime;comparison=false;Status="Kamera startet · danach FOTO zum Speichern drücken";Show();return;
            }catch(Exception e){Debug.LogWarning("[Skin] Camera start: "+e.Message);StopCamera();Status="Eye-Kamera konnte nicht gestartet werden";return;}
        }
        if(Time.unscaledTime-lastFrame>2){Status="Kein aktuelles Eye-Bild · es wird nichts gespeichert";return;}
        var resolution=cameraSource.GetResolution();
        RenderTexture target=null;Texture2D image=null;var previous=RenderTexture.active;
        try {
            target=RenderTexture.GetTemporary(resolution.x,resolution.y,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            Graphics.Blit(Texture2D.blackTexture,target,previewMaterial);
            RenderTexture.active=target;image=new Texture2D(resolution.x,resolution.y,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,resolution.x,resolution.y),0,0);image.Apply();
            Directory.CreateDirectory(folder);
            string name=DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff")+"Z.jpg";
            File.WriteAllBytes(Path.Combine(folder,name),image.EncodeToJPG(95));
            Reload();comparison=true;Show();Status="Foto lokal gespeichert · Vergleich: letzte / vorherige Aufnahme";
        }catch(Exception e){Debug.LogWarning("[Skin] Save: "+e.Message);Status="Foto konnte nicht gespeichert werden";}
        finally {RenderTexture.active=previous;if(target!=null)RenderTexture.ReleaseTemporary(target);if(image!=null)Destroy(image);}
#else
        Status="Eye-Kamera nur auf dem Beam Pro verfügbar";
#endif
    }
#if UNITY_ANDROID && !UNITY_EDITOR
    void Frame()
    {
        var planes=cameraSource.GetYUVFormatTextures();if(planes[0]==null)return;
        previewMaterial.SetTexture("_YTex",planes[0]);previewMaterial.SetTexture("_UTex",planes[1]);previewMaterial.SetTexture("_VTex",planes[2]);
        var size=cameraSource.GetResolution();preview.localScale=new Vector3(1.2f,1.2f*size.y/size.x,1);
        bool first=lastFrame<0;lastFrame=Time.unscaledTime;if(first)Status="Eye-Livebild · Person um Einverständnis bitten · FOTO speichert";Show();
    }
#endif
    void Reload()
    {
        try {
            var files=Directory.Exists(folder)?Directory.GetFiles(folder,"*.jpg"):Array.Empty<string>();Array.Sort(files,StringComparer.Ordinal);Array.Reverse(files);
            Load(files,0,ref recentTexture,recentMaterial,recent,recentLabel);
            Load(files,1,ref olderTexture,olderMaterial,older,olderLabel);
        }catch(Exception e){Debug.LogWarning("[Skin] Load: "+e.Message);Status="Gespeicherte Aufnahmen konnten nicht geladen werden";}
    }
    void Load(string[] files,int index,ref Texture2D texture,Material material,Transform panel,TextMesh label)
    {
        if(texture!=null)Destroy(texture);texture=null;material.mainTexture=Texture2D.blackTexture;
        if(files.Length<=index){label.text="Noch keine Aufnahme";return;}
        var loaded=new Texture2D(2,2);if(!loaded.LoadImage(File.ReadAllBytes(files[index]))){Destroy(loaded);label.text="Bild nicht lesbar";return;}
        texture=loaded;material.mainTexture=texture;panel.localScale=new Vector3(1.2f,1.2f*texture.height/texture.width,1);
        label.text=(index==0?"LETZTE":"VORHERIGE")+" · "+File.GetLastWriteTime(files[index]).ToString("dd.MM.yyyy HH:mm:ss");
    }
    void Update()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if(cameraSource!=null&&Time.unscaledTime-Mathf.Max(started,lastFrame)>10){StopCamera();Status="Kein Eye-Kamerabild empfangen · FOTO versucht erneut";}
#endif
    }
    void StopCamera()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if(cameraSource!=null){cameraSource.OnRGBCameraUpdate-=Frame;try{cameraSource.StopCapture();}catch(Exception e){Debug.LogWarning("[Skin] Stop: "+e.Message);}cameraSource=null;}
#endif
        lastFrame=-100;Show();
    }
    void OnDisable(){StopCamera();}
    void OnDestroy(){if(recentTexture!=null)Destroy(recentTexture);if(olderTexture!=null)Destroy(olderTexture);Destroy(previewMaterial);Destroy(recentMaterial);Destroy(olderMaterial);}
}
