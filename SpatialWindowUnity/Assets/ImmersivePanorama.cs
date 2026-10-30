using UnityEngine;

// Photo-based, rotation-only environment. Not a reconstruction or captured video.
public class ImmersivePanorama : MonoBehaviour
{
    public SpaceFlight Owner;
    Transform dome;
    Material material;
    float animationTime;
    void Start()
    {
        var sphere=GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name="Cyberpunk generated panorama";
        Destroy(sphere.GetComponent<Collider>());dome=sphere.transform;
        dome.SetParent(transform,false);dome.localScale=new Vector3(-160,160,160);
        material=new Material(Resources.Load<Material>("PanoramaMaterial"));
        material.mainTexture=Resources.Load<Texture2D>("ImmersiveData/cyberpunk");
        material.SetFloat("_Dream",0);
        sphere.GetComponent<Renderer>().sharedMaterial=material;
    }
    void LateUpdate()
    {
        if(dome==null)return;
        if(Camera.main!=null)dome.position=Camera.main.transform.position;
        if(Owner!=null&&!Owner.Paused)animationTime+=Time.deltaTime;
        material.SetFloat("_AnimationTime",animationTime);
    }
    void OnDestroy(){if(material!=null)Destroy(material);}
}
