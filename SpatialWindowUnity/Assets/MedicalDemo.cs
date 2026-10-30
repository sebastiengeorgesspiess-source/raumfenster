using UnityEngine;
public class MedicalDemo : MonoBehaviour
{
    public SpaceFlight Owner;
    Transform heart;
    Renderer[] parts;
    Color[][] originalColors;
    int selection;
    int zoom;
    readonly string[] sourceNames={"","Right atrium","Right ventricle","Left atrium","Left ventricle"};
    readonly string[] labels={"Gesamtes Herz","Rechter Vorhof","Rechte Herzkammer","Linker Vorhof","Linke Herzkammer"};
    public string Description=>labels[selection]+" · didaktische Farben · vergrössert";
    public void NextStructure(){selection=(selection+1)%sourceNames.Length;Highlight();}
    public void ChangeScale(){zoom=(zoom+1)%3;if(heart!=null)heart.localScale=Vector3.one*new[]{.65f,.9f,1.15f}[zoom];}
    void Start()
    {
        heart=Instantiate(Resources.Load<GameObject>("MedicalHeart"),transform).transform;
        heart.localPosition=new Vector3(0,0,1.7f);heart.localScale=Vector3.one*.65f;
        parts=heart.GetComponentsInChildren<Renderer>();originalColors=new Color[parts.Length][];
        for(int i=0;i<parts.Length;i++){var mats=parts[i].materials;originalColors[i]=new Color[mats.Length];for(int j=0;j<mats.Length;j++)originalColors[i][j]=mats[j].color;}
        var key=new GameObject("Exhibit light",typeof(Light)).GetComponent<Light>();key.transform.SetParent(transform,false);key.type=LightType.Directional;key.intensity=1.2f;key.transform.localRotation=Quaternion.Euler(20,30,0);
        Highlight();
    }
    void Highlight()
    {
        if(parts==null)return;
        for(int i=0;i<parts.Length;i++)
        {
            bool active=selection==0||parts[i].name==sourceNames[selection];var mats=parts[i].materials;
            for(int j=0;j<mats.Length;j++){mats[j].color=active?originalColors[i][j]:new Color(.12f,.13f,.15f);mats[j].EnableKeyword("_EMISSION");mats[j].SetColor("_EmissionColor",active&&selection>0?originalColors[i][j]*.25f:Color.black);}
        }
    }
    void Update(){if(heart!=null&&Owner!=null&&!Owner.Paused)heart.Rotate(Vector3.up,8*Time.deltaTime,Space.Self);}
}
