using UnityEngine;
using System.Collections.Generic;

public class SpaceFlight : MonoBehaviour
{
    public bool Paused;
    public int Experiment { get; private set; }
    public bool NeedsPosition => Experiment==3||Experiment==6;
    public bool IsSkin => Experiment==7;
    public SkinDocumentation ActiveSkin => IsSkin&&scenes[7]!=null?scenes[7].GetComponent<SkinDocumentation>():null;
    public bool IsMedical => Experiment==6;
    public MedicalDemo ActiveMedical => IsMedical&&scenes[6]!=null?scenes[6].GetComponent<MedicalDemo>():null;
    public float FlightSpeed { get; private set; }=1;
    public void ChangeSpeed(){FlightSpeed=FlightSpeed==1?.5f:FlightSpeed==.5f?1.5f:1;}
    public bool IsVideo => Experiment==5;
    public OfflineVideo360 ActiveVideo => IsVideo&&scenes[Experiment]!=null?scenes[Experiment].GetComponent<OfflineVideo360>():null;
    public string Title => new[]{"ERDE & MOND","ASTEROIDEN","LICHTTUNNEL","EYE · PLANETARIUM","ENTFERNT","BIENEN · STEREO-3D","EYE · HERZANATOMIE","HAUT-DOKUMENTATION"}[Experiment];
    public string Hint => IsSkin?(ActiveSkin!=null?ActiveSkin.Status:"Dokumentation wird vorbereitet"):IsMedical?(ActiveMedical!=null?ActiveMedical.Description:"Anatomiemodell wird geladen"):IsVideo?(ActiveVideo!=null?ActiveVideo.Status:"Video wird vorbereitet"):new[]{"Erde beobachten und frei umschauen","Felsen kommen näher und ziehen vorbei","Leuchtringe fliegen auf dich zu · sitzend testen","Seitlich lehnen: an den Planeten vorbeischauen","Detailliertes KI-Panorama · Umschauen","Fotografierte Grotte · animierte Traumfarben"}[Experiment];
    GameObject[] scenes=new GameObject[8];
    readonly List<Transform> rocks=new List<Transform>(),rings=new List<Transform>();
    Transform earth,moon,tableEarth,tableMoon;
    Material glow;
    GameObject starBackground;
    float elapsed;
    readonly System.Random random=new System.Random(427);
    float Range(float a,float b)=>(float)(a+(b-a)*random.NextDouble());
    public void NextExperiment(){int next=(Experiment+1)%8;Select(next==4?5:next);}
    public void Select(int index){Experiment=Mathf.Clamp(index,0,7);if(Experiment==4)Experiment=5;Paused=IsMedical||IsSkin;ApplyScene();}
    void ApplyScene(){for(int i=0;i<scenes.Length;i++)if(scenes[i]!=null)scenes[i].SetActive(i==Experiment);}
    Material Surface(string texture,Color color)
    {
        var m=new Material(Resources.Load<Material>("WorldMaterial"));m.color=color;m.SetFloat("_Glossiness",.12f);
        if(texture!=null)m.mainTexture=Resources.Load<Texture2D>("SpaceData/"+texture);
        return m;
    }
    Transform Shape(Transform parent,string name,PrimitiveType type,Vector3 position,Vector3 scale,Material material)
    {
        var g=GameObject.CreatePrimitive(type);g.name=name;Destroy(g.GetComponent<Collider>());
        g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=scale;
        g.GetComponent<Renderer>().sharedMaterial=material;return g.transform;
    }
    LineRenderer Line(Transform parent,string name,Vector3[] points,Color color,float width,bool loop=false)
    {
        var g=new GameObject(name,typeof(LineRenderer));g.transform.SetParent(parent,false);
        var line=g.GetComponent<LineRenderer>();line.sharedMaterial=glow;line.useWorldSpace=false;line.loop=loop;
        line.positionCount=points.Length;line.SetPositions(points);line.startWidth=line.endWidth=width;line.startColor=line.endColor=color;return line;
    }
    void Start()
    {
        glow=new Material(Resources.Load<Material>("StarMaterial"));
        for(int i=0;i<scenes.Length;i++){scenes[i]=new GameObject("Demo "+i);scenes[i].transform.SetParent(transform,false);}
        var sky=Shape(transform,"Milky Way",PrimitiveType.Sphere,Vector3.zero,new Vector3(-350,350,350),new Material(Resources.Load<Material>("PhotoMaterial")));
        starBackground=sky.gameObject;
        sky.GetComponent<Renderer>().material.mainTexture=Resources.Load<Texture2D>("SpaceData/stars_milky_way");
        var sun=new GameObject("Sunlight",typeof(Light)).GetComponent<Light>();sun.transform.SetParent(transform,false);
        sun.type=LightType.Directional;sun.intensity=1.4f;sun.transform.localRotation=Quaternion.Euler(15,-55,0);
        earth=Shape(scenes[0].transform,"Earth",PrimitiveType.Sphere,new Vector3(-7,-5,29),Vector3.one*22,Surface("earth",Color.white));
        Shape(scenes[0].transform,"Atmospheric limb",PrimitiveType.Sphere,new Vector3(-7,-5,29),Vector3.one*22.4f,Resources.Load<Material>("AtmosphereMaterial"));
        moon=Shape(scenes[0].transform,"Moon",PrimitiveType.Sphere,new Vector3(12,5,40),Vector3.one*5,Surface("moon",Color.white));
        var rockMat=Surface("moon",new Color(.62f,.58f,.53f));
        for(int i=0;i<28;i++)
        {
            float size=Range(.45f,1.4f);var rock=Shape(scenes[1].transform,"Asteroid",PrimitiveType.Sphere,Vector3.zero,new Vector3(size,size*Range(.65f,1),size*Range(.8f,1.4f)),rockMat);
            var mesh=Instantiate(rock.GetComponent<MeshFilter>().sharedMesh);var vertices=mesh.vertices;
            for(int j=0;j<vertices.Length;j++){var p=vertices[j];float n=Mathf.PerlinNoise(p.x*5+12,p.y*5+p.z*3+6);vertices[j]=p*(.72f+n*.5f);}mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();rock.GetComponent<MeshFilter>().mesh=mesh;
            rocks.Add(rock);PlaceRock(rock,5+i*3.5f);
        }
        for(int i=0;i<14;i++)
        {
            var points=new Vector3[80];for(int j=0;j<points.Length;j++){float a=j*Mathf.PI*2/points.Length;points[j]=new Vector3(Mathf.Cos(a)*2.6f,Mathf.Sin(a)*2.6f,0);}
            var ring=Line(scenes[2].transform,"Approaching gate",points,Color.Lerp(Color.cyan,new Color(1,.2f,.65f),i/13f),.045f,true).transform;
            ring.localPosition=new Vector3(0,0,i*6);rings.Add(ring);
        }
        tableEarth=Shape(scenes[3].transform,"Earth exhibit",PrimitiveType.Sphere,new Vector3(-.32f,0,1.35f),Vector3.one*.52f,Surface("earth",Color.white));
        tableMoon=Shape(scenes[3].transform,"Moon exhibit",PrimitiveType.Sphere,new Vector3(.4f,.12f,1.7f),Vector3.one*.2f,Surface("moon",Color.white));
        Shape(scenes[3].transform,"Jupiter exhibit",PrimitiveType.Sphere,new Vector3(.6f,-.2f,2.6f),Vector3.one*.7f,Surface("jupiter",Color.white));
        Shape(scenes[3].transform,"Mars exhibit",PrimitiveType.Sphere,new Vector3(-.65f,.25f,2.4f),Vector3.one*.3f,Surface("mars",Color.white));
        Line(scenes[3].transform,"Depth frame",new[]{new Vector3(-.9f,-.55f,1),new Vector3(-.9f,.6f,1),new Vector3(.9f,.6f,1),new Vector3(.9f,-.55f,1)},new Color(.15f,.7f,1),.008f,true);
        var video=scenes[5].AddComponent<OfflineVideo360>();video.Owner=this;video.FileName="bees-h264.mp4";video.StereoFlat=true;
        var medical=scenes[6].AddComponent<MedicalDemo>();medical.Owner=this;
        scenes[7].AddComponent<SkinDocumentation>();
        ApplyScene();

    }
    void PlaceRock(Transform rock,float z){float a=Range(0,Mathf.PI*2),r=Range(1.9f,6);rock.localPosition=new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,z);}
    void Update()
    {
        if(starBackground!=null)starBackground.SetActive(!IsMedical&&!IsVideo&&!IsSkin);
        if(Paused||earth==null)return;elapsed+=Time.deltaTime;
        if(Experiment==0){earth.Rotate(Vector3.up,1.5f*Time.deltaTime);moon.Rotate(Vector3.up,Time.deltaTime);}
        if(Experiment==1)foreach(var rock in rocks){rock.localPosition+=Vector3.back*(7*FlightSpeed*Time.deltaTime);rock.Rotate(new Vector3(8,13,5)*Time.deltaTime);if(rock.localPosition.z< -9)PlaceRock(rock,95);}
        if(Experiment==2)foreach(var ring in rings){ring.localPosition+=Vector3.back*(8*FlightSpeed*Time.deltaTime);if(ring.localPosition.z< -6)ring.localPosition+=Vector3.forward*84;}
        if(Experiment==3){tableEarth.Rotate(Vector3.up,7*Time.deltaTime);tableMoon.localPosition=new Vector3(Mathf.Cos(elapsed*.2f)*.65f,.12f,1.65f+Mathf.Sin(elapsed*.2f)*.35f);}

    }
}
