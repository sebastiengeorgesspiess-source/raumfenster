using System.IO;
using UnityEngine;
using UnityEngine.Video;

// Reads only installed local files. Mono equirectangular capture, not 6DoF geometry.
public class OfflineVideo360 : MonoBehaviour
{
    public SpaceFlight Owner;
    public string FileName;
    public bool StereoFlat;
    bool swapped;
    public void SwapEyes(){swapped=!swapped;if(material!=null)material.SetFloat("_SwapEyes",swapped?1:0);}
    public string Status { get; private set; }="Video wird vorbereitet";
    public bool NeedsAttention { get; private set; }=true;
    VideoPlayer player;
    AudioSource audioSource;
    Transform dome;
    Material material;
    RenderTexture output;
    bool firstFrame, failed;
    float prepareStarted;
    void Start()
    {
        var sphere=GameObject.CreatePrimitive(StereoFlat?PrimitiveType.Quad:PrimitiveType.Sphere);
        sphere.name="Offline 360 video";Destroy(sphere.GetComponent<Collider>());
        dome=sphere.transform;dome.SetParent(transform,false);
        dome.localScale=StereoFlat?new Vector3(2.4f,1.35f,1):new Vector3(-160,160,160);
        if(StereoFlat)dome.localPosition=new Vector3(0,0,2.6f);
        material=new Material(Resources.Load<Material>("PhotoMaterial"));material.mainTexture=Texture2D.blackTexture;
        material.SetFloat("_SBS",StereoFlat?1:0);
        sphere.GetComponent<Renderer>().sharedMaterial=material;
        audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.spatialBlend=0;audioSource.volume=.65f;
        player=gameObject.AddComponent<VideoPlayer>();player.playOnAwake=false;player.source=VideoSource.Url;
        player.isLooping=true;player.waitForFirstFrame=true;player.skipOnDrop=true;
        player.renderMode=VideoRenderMode.RenderTexture;
        player.audioOutputMode=VideoAudioOutputMode.AudioSource;player.controlledAudioTrackCount=1;
        player.EnableAudioTrack(0,true);player.SetTargetAudioSource(0,audioSource);
        player.prepareCompleted+=Prepared;player.frameReady+=FrameReady;player.errorReceived+=Failed;
        Begin();
    }
    void Begin()
    {
        firstFrame=false;failed=false;NeedsAttention=true;Status="Lokales Video wird geladen";prepareStarted=Time.unscaledTime;
        var path=Path.Combine(Application.persistentDataPath,"videos",FileName);
        if(!File.Exists(path)){Failed(player,"Lokale Videodatei fehlt: "+FileName);return;}
        player.url=path;player.sendFrameReadyEvents=true;player.Prepare();
        Debug.Log("[Raumfenster Video] Preparing "+FileName);
    }
    void Prepared(VideoPlayer vp)
    {
        if(!isActiveAndEnabled)return;
        if(vp.width==0||vp.height==0||vp.width>4096||vp.height>2160){Failed(vp,"Videoformat nicht unterstützt");return;}
        output=new RenderTexture((int)vp.width,(int)vp.height,0,RenderTextureFormat.ARGB32);
        output.Create();vp.targetTexture=output;material.mainTexture=output;
        audioSource.mute=true;vp.Play(); // Decode a first frame even when the demo starts paused.
        Debug.Log($"[Raumfenster Video] Prepared {FileName} {vp.width}x{vp.height}, audio={vp.audioTrackCount}");
    }
    void FrameReady(VideoPlayer vp,long frame)
    {
        if(firstFrame)return;
        firstFrame=true;vp.sendFrameReadyEvents=false;
        if(Owner.Paused)vp.Pause();
        Debug.Log("[Raumfenster Video] First frame "+FileName);
    }
    void Failed(VideoPlayer vp,string error)
    {
        failed=true;NeedsAttention=true;Status="Video kann nicht abgespielt werden · andere Szene wählen";
        if(vp!=null)vp.Stop();if(audioSource!=null)audioSource.mute=true;
        Debug.LogError("[Raumfenster Video] "+FileName+": "+error);
    }
    void Update()
    {
        if(player==null||failed)return;
        if(!firstFrame){if(Time.unscaledTime-prepareStarted>25)Failed(player,"Zeitüberschreitung beim Laden");return;}
        audioSource.mute=Owner.Muted;
        if(Owner.Paused){if(player.isPlaying)player.Pause();Status="Video pausiert · FORTSETZEN zum Starten";NeedsAttention=true;}
        else {if(!player.isPlaying)player.Play();Status=StereoFlat?"Full-SBS · 3D-Leinwand · Originalton":"Echtes 360°-Video · lokal · frei umschauen";NeedsAttention=false;}
    }
    void LateUpdate(){if(!StereoFlat&&dome!=null&&Camera.main!=null)dome.position=Camera.main.transform.position;}
    void OnEnable(){if(player!=null)Begin();}
    void OnApplicationPause(bool paused){if(paused&&Owner!=null)Owner.Paused=true;}
    void OnDisable()
    {
        if(player!=null){player.Stop();player.targetTexture=null;}
        if(material!=null)material.mainTexture=Texture2D.blackTexture;
        if(output!=null){output.Release();Destroy(output);output=null;}
    }
    void OnDestroy(){if(material!=null)Destroy(material);}
}
