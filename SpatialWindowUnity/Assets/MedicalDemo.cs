using UnityEngine;
public class MedicalDemo : MonoBehaviour
{
    public SpaceFlight Owner;
    Transform heart;
    Renderer[] parts;
    Material[][] materials;
    Color[][] originalColors;
    Vector3[] separation;
    int selection,zoom;
    bool isolated,exploded;
    float yaw,pitch;
    TextMesh caption;
    readonly string[] sourceNames={"","Right atrium","Right ventricle","Left atrium","Left ventricle","Ascending aorta","Aortic arch","Pulmonary trunk"};
    readonly string[] labels={"Gesamtes Herz","Rechter Vorhof","Rechte Herzkammer","Linker Vorhof","Linke Herzkammer","Aufsteigende Aorta","Aortenbogen","Lungenarterienstamm"};
    public string Description=>labels[selection]+(isolated?" · einzeln":"")+(exploded?" · auseinandergezogen":"");
    public void NextStructure(){selection=(selection+1)%sourceNames.Length;if(selection==0)isolated=false;Highlight();}
    public void ToggleIsolation(){if(selection==0)selection=1;isolated=!isolated;Highlight();}
    public void ToggleExploded(){exploded=!exploded;RefreshCaption();}
    public void ChangeScale(){zoom=(zoom+1)%3;if(heart!=null)heart.localScale=Vector3.one*new[]{.65f,.85f,1.05f}[zoom];RefreshCaption();}
    public void Drag(Vector2 movement)
    {
        if(heart==null)return;
        Owner.Paused=true;yaw-=movement.x*240;pitch=Mathf.Clamp(pitch+movement.y*180,-65,65);
        heart.localRotation=Quaternion.Euler(pitch,yaw,0);
    }
    public void ResetModel(){selection=zoom=0;isolated=exploded=false;yaw=pitch=0;Owner.Paused=true;if(heart!=null){heart.localRotation=Quaternion.identity;heart.localScale=Vector3.one*.65f;}Highlight();}
    void Start()
    {
        heart=Instantiate(Resources.Load<GameObject>("MedicalHeart"),transform).transform;
        heart.localPosition=new Vector3(0,0,1.7f);heart.localScale=Vector3.one*.65f;
        parts=heart.GetComponentsInChildren<Renderer>();originalColors=new Color[parts.Length][];materials=new Material[parts.Length][];separation=new Vector3[parts.Length];
        for(int i=0;i<parts.Length;i++){
            materials[i]=parts[i].materials;originalColors[i]=new Color[materials[i].Length];
            for(int j=0;j<materials[i].Length;j++)originalColors[i][j]=materials[i][j].color;
            var center=parts[i].GetComponent<MeshFilter>().sharedMesh.bounds.center;
            separation[i]=center.sqrMagnitude>.001f?center.normalized*.32f:Vector3.up*.32f;
        }
        caption=new GameObject("Heart label").AddComponent<TextMesh>();caption.transform.SetParent(transform,false);
        caption.transform.localPosition=new Vector3(0,-.56f,1.7f);caption.fontSize=48;caption.characterSize=.013f;
        caption.anchor=TextAnchor.MiddleCenter;caption.alignment=TextAlignment.Center;caption.color=new Color(.75f,.94f,1);
        var key=new GameObject("Exhibit light",typeof(Light)).GetComponent<Light>();key.transform.SetParent(transform,false);key.type=LightType.Directional;key.intensity=1.2f;key.transform.localRotation=Quaternion.Euler(20,30,0);
        Highlight();
    }
    void RefreshCaption(){if(caption!=null)caption.text=labels[selection].ToUpperInvariant()+"\n"+(exploded?"Teile zur Übersicht versetzt · keine anatomische Lage":"Anatomisches Lehrmodell · schematische Farben");}
    void Highlight()
    {
        if(parts==null)return;
        for(int i=0;i<parts.Length;i++){
            bool active=selection==0||parts[i].name==sourceNames[selection];parts[i].enabled=!isolated||active;
            for(int j=0;j<materials[i].Length;j++){
                materials[i][j].color=active?originalColors[i][j]:new Color(.12f,.13f,.15f);
                materials[i][j].EnableKeyword("_EMISSION");materials[i][j].SetColor("_EmissionColor",active&&selection>0?originalColors[i][j]*.25f:Color.black);
            }
        }
        RefreshCaption();
    }
    void Update()
    {
        if(heart==null)return;
        if(!Owner.Paused){yaw+=12*Time.deltaTime;heart.localRotation=Quaternion.Euler(pitch,yaw,0);}
        for(int i=0;i<parts.Length;i++)parts[i].transform.localPosition=Vector3.Lerp(parts[i].transform.localPosition,exploded?separation[i]:Vector3.zero,1-Mathf.Exp(-8*Time.deltaTime));
    }
    void OnDestroy(){if(materials!=null)foreach(var group in materials)foreach(var material in group)Destroy(material);}
}
