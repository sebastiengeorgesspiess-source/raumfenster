using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.XR;
using Unity.XR.XREAL;

public class SpatialWindow : MonoBehaviour
{
    [Serializable] class DepthData { public int width, height; public float aspect, horizontalFov; public float[] depth; public int[] valid; }
    Camera view;
    Transform anchor;
    GameObject relief, stereo;
    GameObject forest, gallery;
    Text status, instruction, controllerLabel;
    Button pauseButton,soundButton,contextButton;
    XREALVirtualController controller;
    GameObject controls;
    SpaceFlight flight;
    readonly List<Button> menuButtons=new List<Button>();
    Button pressedMenuButton;
    bool exiting;
    float lastMenuAction=-1;
    readonly List<InputDevice> inputControllers=new List<InputDevice>();
    bool uiHeld, previousHeld, holdDone;
    Vector3 origin;
    bool centered, trackingReady;
    int mode=2;
    float pressedAt, lastAction=-1;
    const float MovementLimit=.18f;

    void Start()
    {
        Application.targetFrameRate=60;
        Screen.sleepTimeout=SleepTimeout.NeverSleep;
        if(EventSystem.current==null)
            new GameObject("Touch input",typeof(EventSystem),typeof(StandaloneInputModule));
        view=new GameObject("Tracked eyes",typeof(Camera)).GetComponent<Camera>();
        view.tag="MainCamera"; view.clearFlags=CameraClearFlags.SolidColor;
        view.backgroundColor=Color.black; view.nearClipPlane=.05f; view.farClipPlane=500;
        view.stereoTargetEye=StereoTargetEyeMask.Both;
        view.gameObject.AddComponent<RenderDiagnostics>();
        anchor=new GameObject("Photo origin").transform;
        BuildPhoto();
        BuildWorlds();
        flight=new GameObject("Weltraumflug").AddComponent<SpaceFlight>();
        flight.gameObject.SetActive(false);
        var canvas=new GameObject("Status",typeof(Canvas)).GetComponent<Canvas>();
        canvas.renderMode=RenderMode.WorldSpace;
        canvas.transform.SetParent(view.transform,false);
        canvas.transform.localPosition=new Vector3(0,-.30f,1.6f);
        canvas.transform.localScale=Vector3.one*.001f;
        status=Label(canvas.transform,"RAUMFENSTER · Eye-Verfolgung wird gesucht",24,Vector2.zero);
        instruction=Label(canvas.transform,"Tippen: Welt wechseln  ·  Halten: neu zentrieren",18,new Vector2(0,-42));
        Application.onBeforeRender+=Track;
    }
    Text Label(Transform parent,string text,int size,Vector2 position)
    {
        var label=new GameObject("Label",typeof(RectTransform),typeof(Text)).GetComponent<Text>();
        label.transform.SetParent(parent,false); label.rectTransform.sizeDelta=new Vector2(1100,80);
        label.rectTransform.anchoredPosition=position;
        label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize=size; label.alignment=TextAnchor.MiddleCenter;
        label.color=new Color(.68f,.94f,1); label.text=text; label.raycastTarget=false;
        return label;
    }
    void BuildPhoto()
    {
        var data=JsonUtility.FromJson<DepthData>(Resources.Load<TextAsset>("PhotoData/depth").text);
        var verts=new Vector3[data.width*data.height]; var uv=new Vector2[verts.Length];
        float half=Mathf.Tan(data.horizontalFov*Mathf.Deg2Rad/2);
        for(int y=0;y<data.height;y++) for(int x=0;x<data.width;x++)
        {
            int i=y*data.width+x; float u=(float)x/(data.width-1),v=(float)y/(data.height-1),z=data.depth[i];
            verts[i]=new Vector3((u-.5f)*2*half*z,(.5f-v)*2*half*z/data.aspect,z);
            uv[i]=new Vector2(u,1-v);
        }
        var triangles=new List<int>();
        void Triangle(int a,int b,int c)
        {
            if(data.valid[a]==0||data.valid[b]==0||data.valid[c]==0) return;
            float min=Mathf.Min(data.depth[a],Mathf.Min(data.depth[b],data.depth[c]));
            float max=Mathf.Max(data.depth[a],Mathf.Max(data.depth[b],data.depth[c]));
            if(max-min>.22f) return; // Do not stretch photograph across depth discontinuities.
            triangles.Add(a);triangles.Add(b);triangles.Add(c);
        }
        for(int y=0;y<data.height-1;y++) for(int x=0;x<data.width-1;x++)
        {int a=y*data.width+x;Triangle(a,a+1,a+data.width);Triangle(a+1,a+data.width+1,a+data.width);}
        var mesh=new Mesh{name="Stereo-derived relief"};mesh.vertices=verts;mesh.uv=uv;mesh.triangles=triangles.ToArray();mesh.RecalculateBounds();
        relief=new GameObject("Relative depth experiment",typeof(MeshFilter),typeof(MeshRenderer));
        relief.transform.SetParent(anchor,false);relief.GetComponent<MeshFilter>().mesh=mesh;
        relief.GetComponent<MeshRenderer>().material=PhotoMaterial("left",false);
        stereo=GameObject.CreatePrimitive(PrimitiveType.Quad);Destroy(stereo.GetComponent<Collider>());
        stereo.name="Original stereo photograph";stereo.transform.SetParent(anchor,false);
        stereo.transform.localPosition=new Vector3(0,0,2.5f);
        stereo.transform.localScale=new Vector3(5*half,5*half/data.aspect,1);
        stereo.GetComponent<MeshRenderer>().material=PhotoMaterial("stereo",true);
        Debug.Log($"[Raumfenster] Mesh {verts.Length} vertices, {triangles.Count/3} triangles");
    }
    Material PhotoMaterial(string texture,bool sbs)
    {
        var mat=new Material(Resources.Load<Material>("PhotoMaterial"));
        mat.mainTexture=Resources.Load<Texture2D>("PhotoData/"+texture);mat.SetFloat("_SBS",sbs?1:0);
        return mat;
    }
    void BuildWorlds()
    {
        forest=GameObject.CreatePrimitive(PrimitiveType.Sphere);
        forest.name="Autumn Forest 04 · Poly Haven · CC0";
        Destroy(forest.GetComponent<Collider>());
        forest.transform.localScale=new Vector3(-16,16,16);
        var panorama=new Material(Resources.Load<Material>("PhotoMaterial"));
        panorama.mainTexture=Resources.Load<Texture2D>("WorldData/forest");
        forest.GetComponent<MeshRenderer>().material=panorama;
        gallery=new GameObject("Walkable light gallery");
        var light=new GameObject("Gallery light",typeof(Light)).GetComponent<Light>();
        light.transform.SetParent(gallery.transform,false);light.transform.localRotation=Quaternion.Euler(45,-30,0);
        light.type=LightType.Directional;light.intensity=1.2f;light.shadows=LightShadows.None;
        RenderSettings.ambientLight=new Color(.45f,.5f,.58f);
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
        var stone=new Material(Resources.Load<Material>("WorldMaterial"));stone.color=new Color(.19f,.24f,.3f);
        var pale=new Material(stone);pale.color=new Color(.72f,.76f,.8f);
        var cyan=new Material(stone);cyan.color=new Color(.08f,.8f,.8f);cyan.EnableKeyword("_EMISSION");cyan.SetColor("_EmissionColor",new Color(.04f,.45f,.45f));
        void Shape(string name,PrimitiveType type,Vector3 pos,Vector3 scale,Material mat)
        {
            var obj=GameObject.CreatePrimitive(type);obj.name=name;obj.transform.SetParent(gallery.transform,false);
            obj.transform.localPosition=pos;obj.transform.localScale=scale;Destroy(obj.GetComponent<Collider>());
            obj.GetComponent<MeshRenderer>().sharedMaterial=mat;
        }
        Shape("Floor",PrimitiveType.Cube,new Vector3(0,-1.55f,0),new Vector3(12,.1f,12),stone);
        for(int i=-5;i<=5;i++)
        {
            Shape("Floor line",PrimitiveType.Cube,new Vector3(i,-1.492f,0),new Vector3(.012f,.006f,12),pale);
            Shape("Floor line",PrimitiveType.Cube,new Vector3(0,-1.492f,i),new Vector3(12,.006f,.012f),pale);
        }
        for(int i=0;i<8;i++)
        {
            float a=i*Mathf.PI/4;var p=new Vector3(Mathf.Sin(a)*4.8f,0,Mathf.Cos(a)*4.8f);
            Shape("Column",PrimitiveType.Cylinder,p,new Vector3(.35f,1.5f,.35f),pale);
            Shape("Light capital",PrimitiveType.Cylinder,p+Vector3.up*1.5f,new Vector3(.6f,.035f,.6f),cyan);
        }
        Shape("Floating study",PrimitiveType.Sphere,new Vector3(0,.1f,3),Vector3.one*.65f,cyan);
        Shape("Pedestal",PrimitiveType.Cylinder,new Vector3(0,-1,3),new Vector3(.9f,.5f,.9f),pale);
        var art=GameObject.CreatePrimitive(PrimitiveType.Quad);Destroy(art.GetComponent<Collider>());
        art.name="Your photograph";art.transform.SetParent(gallery.transform,false);
        art.transform.localPosition=new Vector3(0,.15f,4.7f);art.transform.localScale=new Vector3(2.4f,1.8f,1);
        art.GetComponent<MeshRenderer>().material=PhotoMaterial("left",false);
    }
    void Update()
    {
        if(exiting)return;
        BindController();Track();
        if(Input.GetKeyDown(KeyCode.Escape)){ExitDemo();return;}
        view.farClipPlane=mode==4?200:20;
        if(Input.GetKeyDown(KeyCode.Space)) Toggle();
        if(Input.GetKeyDown(KeyCode.R)) Recenter();
        PollMenuTouch();
        if(exiting)return;
        PollTrigger();
        float distance=centered?Vector3.Distance(view.transform.position,origin):0;
        bool rangeOk=distance<MovementLimit;
        relief.SetActive(centered&&mode==0&&trackingReady&&rangeOk);
        stereo.SetActive(centered&&mode==1);
        forest.SetActive(centered&&mode==2);
        gallery.SetActive(centered&&mode==3&&trackingReady);
        flight.gameObject.SetActive(centered&&mode==4&&(!flight.NeedsPosition||trackingReady));
        RenderSettings.ambientLight=mode==4?new Color(.065f,.07f,.09f):new Color(.45f,.5f,.58f);
        forest.transform.position=view.transform.position; // Panorama: rotation only, never pretend it is reconstructed geometry.
        if(!centered) status.text="RAUMFENSTER · Warte auf Brille und Kopfverfolgung";
        else if((mode==0||mode==3)&&!trackingReady) status.text="Keine Positionsverfolgung · Eye prüfen oder Panorama wählen";
        else if(mode==0&&!rangeOk) status.text="Bitte zur Mitte zurück · oder Taste 1 Sekunde zum Zentrieren halten";
        else if(mode==0) status.text=$"TIEFENSZENE · {distance*100:0} / 18 cm · experimentelle Geometrie";
        else if(mode==1) status.text="ORIGINAL · Stereo-Foto · fester Aufnahmeblickwinkel";
        else if(mode==2) status.text="WALD · 360°-Panorama · Umschauen ohne räumliche Bewegung";
        else if(mode==3) status.text="LICHTGALERIE · echte 3D-Geometrie · 6DoF";
        else status.text=flight.NeedsPosition&&!trackingReady?"EYE · Keine Positionsverfolgung verfügbar":flight.Title+" · "+flight.Hint;
        instruction.text=mode==3?"Freie Fläche nötig · kein Hindernisschutz":"Am Beam Pro: Nächste Welt oder Zentrieren";
        bool showInfo=(mode==4&&flight.IsSkin)||(mode==4&&flight.IsVideo&&(flight.ActiveVideo==null||flight.ActiveVideo.NeedsAttention))||(mode==4&&flight.NeedsPosition&&!trackingReady)||Time.unscaledTime-lastAction<8||!centered||((mode==0||mode==3)&&!trackingReady)||(mode==0&&!rangeOk);
        status.gameObject.SetActive(showInfo);instruction.gameObject.SetActive(showInfo);
        if(contextButton!=null)contextButton.GetComponentInChildren<Text>().text=mode==4&&flight.IsSkin?"FOTO · KAMERA / SPEICHERN":mode==4&&flight.IsMedical?"MODELL VERGRÖSSERN":mode==4&&flight.IsVideo?"LINKES / RECHTES AUGE TAUSCHEN":"TEMPO WECHSELN · "+flight.FlightSpeed.ToString("0.0")+"x";
        if(pauseButton!=null)pauseButton.GetComponentInChildren<Text>().text=mode==4&&flight.IsSkin?"LIVE / FOTO-VERGLEICH":flight.Paused?"BEWEGUNG: PAUSE · FORTSETZEN":"BEWEGUNG: AN · PAUSIEREN";
        if(soundButton!=null)soundButton.GetComponentInChildren<Text>().text=flight.Muted?"TON: AUS · EINSCHALTEN":"TON: AN · AUSSCHALTEN";
        if(controllerLabel!=null) controllerLabel.text="RAUMFENSTER\n"+new[]{"Tiefenszene","Originalfoto","Wald 360°","Lichtgalerie 3D",flight.Title}[mode]+(mode==4?"\n"+(trackingReady?"Eye: Position aktiv · "+(Vector3.Distance(view.transform.position,origin)*100).ToString("0")+" cm":"Nur Kopfrotation")+" · Ton "+(flight.Muted?"AUS":"AN"):" ");
    }
    void Track()
    {
        if(view==null)return;
        var head=InputDevices.GetDeviceAtXRNode(XRNode.Head);
        bool tracked=head.TryGetFeatureValue(CommonUsages.isTracked,out bool active)&&active;
        bool rotationOk=head.TryGetFeatureValue(CommonUsages.deviceRotation,out Quaternion rotation);
        bool positionOk=head.TryGetFeatureValue(CommonUsages.devicePosition,out Vector3 position);
        trackingReady=tracked&&positionOk&&XREALPlugin.GetTrackingType()==TrackingType.MODE_6DOF;
        if(head.TryGetFeatureValue(CommonUsages.trackingState,out InputTrackingState state))
            trackingReady&=(state&InputTrackingState.Position)!=0;
        if(rotationOk)view.transform.localRotation=rotation;
        if(positionOk)view.transform.localPosition=position;
        if(!centered&&tracked&&rotationOk)Recenter();
    }
    void Recenter()
    {
        lastAction=Time.unscaledTime;
        origin=view.transform.position;anchor.SetPositionAndRotation(origin,view.transform.rotation);
        var yaw=Quaternion.Euler(0,view.transform.eulerAngles.y,0);
        if(gallery!=null)gallery.transform.SetPositionAndRotation(origin,yaw);
        if(forest!=null)forest.transform.rotation=yaw;
        if(flight!=null)flight.transform.SetPositionAndRotation(origin,yaw);
        centered=true;Debug.Log("[Raumfenster] Recenter; 6DoF="+trackingReady);
    }
    void Toggle()
    {
        if(Time.unscaledTime-lastAction<.3f)return;
        lastAction=Time.unscaledTime;mode=(mode+1)%5;Recenter();
        Debug.Log("[Raumfenster] Mode="+mode);
    }
    void BindController()
    {
        var next=XREALVirtualController.Singleton;
        if(next==null||next==controller)return;
        Unbind();controller=next;controller.pointerDown+=Down;controller.pointerUp+=Up;
        BuildControls();
        Debug.Log("[Raumfenster] Controller bound; explicit buttons ready");
    }
    void BuildControls()
    {
        if(controls!=null)Destroy(controls);
        menuButtons.Clear();
        var canvas=controller.GetComponentInChildren<Canvas>(true);
        if(canvas==null)return;
        controls=new GameObject("Raumfenster controls",typeof(RectTransform),typeof(Image));
        controls.transform.SetParent(canvas.transform,false);controls.transform.SetAsLastSibling();
        var rect=controls.GetComponent<RectTransform>();rect.anchorMin=new Vector2(.05f,.12f);rect.anchorMax=new Vector2(.95f,.88f);rect.offsetMin=rect.offsetMax=Vector2.zero;
        controls.GetComponent<Image>().color=new Color(.025f,.05f,.08f,.98f);
        controllerLabel=Label(controls.transform,"RAUMFENSTER",34,Vector2.zero);
        controllerLabel.rectTransform.anchorMin=new Vector2(0,.85f);controllerLabel.rectTransform.anchorMax=new Vector2(1,1);controllerLabel.rectTransform.offsetMin=controllerLabel.rectTransform.offsetMax=Vector2.zero;
        ControlButton("NÄCHSTE WELT",.755f,.825f,()=>Toggle());
        ControlButton("ZENTRIEREN",.68f,.75f,()=>Recenter());
        ControlButton("DEMO · NÄCHSTE SZENE",.605f,.675f,()=>{if(mode==4)flight.NextExperiment();else flight.Select(0);mode=4;Recenter();});
        ControlButton("EYE · MEDIZIN / STRUKTUR",.53f,.60f,()=>{if(mode==4&&flight.IsMedical&&flight.ActiveMedical!=null){flight.ActiveMedical.NextStructure();lastAction=Time.unscaledTime;}else {flight.Select(6);mode=4;Recenter();}});
        ControlButton("HAUT · DOKUMENTATION",.455f,.525f,()=>{flight.Select(7);mode=4;Recenter();});
        contextButton=ControlButton("TEMPO WECHSELN",.38f,.45f,()=>{if(mode!=4)return;if(flight.IsSkin)flight.ActiveSkin?.Capture();else if(flight.IsMedical)flight.ActiveMedical?.ChangeScale();else if(flight.IsVideo)flight.ActiveVideo?.SwapEyes();else flight.ChangeSpeed();});
        ControlButton("BIENEN · 3D-VIDEO",.305f,.375f,()=>{flight.Select(5);mode=4;Recenter();});
        ControlButton("APP VERLASSEN",.23f,.30f,ExitDemo);
        var pause=ControlButton("BEWEGUNG: AN · PAUSIEREN",.155f,.225f,()=>{});
        pauseButton=pause;
        pause.onClick.AddListener(()=>{if(mode==4&&flight.IsSkin){flight.ActiveSkin?.ToggleComparison();return;}flight.Paused=!flight.Paused;pause.GetComponentInChildren<Text>().text=mode==4&&flight.IsSkin?"LIVE / FOTO-VERGLEICH":flight.Paused?"BEWEGUNG: PAUSE · FORTSETZEN":"BEWEGUNG: AN · PAUSIEREN";});
        var sound=ControlButton("TON: AN · AUSSCHALTEN",.08f,.15f,()=>{});
        soundButton=sound;
        sound.onClick.AddListener(()=>{flight.Muted=!flight.Muted;sound.GetComponentInChildren<Text>().text=flight.Muted?"TON: AUS · EINSCHALTEN":"TON: AN · AUSSCHALTEN";lastAction=Time.unscaledTime;});
        foreach(var native in controller.GetComponentsInChildren<XREALButton>(true))
        {
            if(native.name!="Home")continue;
            native.enabled=false;
            var home=native.GetComponent<RectTransform>();home.SetParent(canvas.transform,false);
            home.anchorMin=home.anchorMax=new Vector2(.5f,0);home.anchoredPosition=new Vector2(0,130);home.sizeDelta=new Vector2(250,150);home.localScale=Vector3.one;
            var homeImage=native.GetComponent<Image>();if(homeImage!=null)homeImage.raycastTarget=false;
            var homeButton=native.GetComponent<Button>()??native.gameObject.AddComponent<Button>();
            homeButton.onClick.RemoveAllListeners();homeButton.onClick.AddListener(ExitDemo);menuButtons.Add(homeButton);
        }
        var credit=Label(controls.transform,"Herz: Z-Anatomy / Lluís Vinent · CC BY-SA 4.0\nPlaneten: Solar System Scope · CC BY 4.0",20,Vector2.zero);
        credit.rectTransform.anchorMin=new Vector2(0,0);credit.rectTransform.anchorMax=new Vector2(1,.065f);credit.rectTransform.offsetMin=credit.rectTransform.offsetMax=Vector2.zero;
    }
    Button ControlButton(string title,float bottom,float top,UnityEngine.Events.UnityAction action)
    {
        var obj=new GameObject(title,typeof(RectTransform),typeof(Image),typeof(Button));
        obj.transform.SetParent(controls.transform,false);var rect=obj.GetComponent<RectTransform>();
        rect.anchorMin=new Vector2(.07f,bottom);rect.anchorMax=new Vector2(.93f,top);rect.offsetMin=rect.offsetMax=Vector2.zero;
        obj.GetComponent<Image>().color=new Color(.08f,.155f,.225f);
        obj.GetComponent<Image>().raycastTarget=false;
        var button=obj.GetComponent<Button>();button.targetGraphic=obj.GetComponent<Image>();button.onClick.AddListener(action);
        menuButtons.Add(button);
        var text=Label(obj.transform,title,32,Vector2.zero);text.rectTransform.anchorMin=Vector2.zero;text.rectTransform.anchorMax=Vector2.one;text.rectTransform.offsetMin=text.rectTransform.offsetMax=Vector2.zero;
        button.onClick.AddListener(()=>Debug.Log("[Raumfenster] Touch="+title));return button;
    }
    void ExitDemo()
    {
        if(exiting)return;
        exiting=true;Debug.Log("[Raumfenster] Exit requested");
        XREALPlugin.QuitApplication();
    }
    void PollMenuTouch()
    {
        // Read the Android screen directly as a fallback for XR's UI event routing.
        Vector2 position; bool began,ended,canceled;
        if(Input.touchCount>0)
        {
            var touch=Input.GetTouch(0);position=touch.position;
            began=touch.phase==TouchPhase.Began;ended=touch.phase==TouchPhase.Ended;canceled=touch.phase==TouchPhase.Canceled;
        }
        else
        {
            var screen=UnityEngine.InputSystem.Touchscreen.current;
            if(screen==null)return;
            var touch=screen.primaryTouch;position=touch.position.ReadValue();
            began=touch.press.wasPressedThisFrame;ended=touch.press.wasReleasedThisFrame;
            canceled=touch.phase.ReadValue()==UnityEngine.InputSystem.TouchPhase.Canceled;
        }
        if(began)
        {
            pressedMenuButton=null;
            foreach(var button in menuButtons)
                if(RectTransformUtility.RectangleContainsScreenPoint((RectTransform)button.transform,position,null))
                {pressedMenuButton=button;break;}
            Debug.Log($"[Raumfenster] Screen touch {position}; target={pressedMenuButton?.name}; screen={Screen.width}x{Screen.height}");
        }
        if(ended&&!canceled&&pressedMenuButton!=null)
        {
            var button=pressedMenuButton;pressedMenuButton=null;
            if(RectTransformUtility.RectangleContainsScreenPoint((RectTransform)button.transform,position,null))
            {
                // Disable the regular raycast on our buttons: one physical tap, one action.
                lastMenuAction=Time.unscaledTime;button.onClick.Invoke();
            }
        }
        if(canceled)pressedMenuButton=null;
    }
    void PollTrigger()
    {
        bool held=uiHeld;
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller,inputControllers);
        foreach(var device in inputControllers)
            if(device.TryGetFeatureValue(CommonUsages.triggerButton,out bool value))held|=value;
        foreach(var device in UnityEngine.InputSystem.InputSystem.devices)
            if(device is XREALController xreal)held|=xreal.TriggerButton.isPressed;
        if(held&&!previousHeld){pressedAt=Time.unscaledTime;holdDone=false;Debug.Log("[Raumfenster] Trigger down");}
        if(held&&!holdDone&&Time.unscaledTime-pressedAt>=1){holdDone=true;Recenter();}
        if(!held&&previousHeld&&!holdDone&&Time.unscaledTime-lastMenuAction>.3f)Toggle();
        previousHeld=held;
    }
    void Down(XREALButtonType type,GameObject target,PointerEventData data)
    {if(type==XREALButtonType.TriggerButton)uiHeld=true;}
    void Up(XREALButtonType type,GameObject target,PointerEventData data)
    {if(type==XREALButtonType.TriggerButton)uiHeld=false;}
    void Unbind(){if(controller!=null){controller.pointerDown-=Down;controller.pointerUp-=Up;}}
    void OnApplicationPause(bool paused){uiHeld=previousHeld=false;holdDone=false;pressedMenuButton=null;if(!paused)centered=false;}
    void OnDestroy(){Unbind();Application.onBeforeRender-=Track;}
}
