using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

// Bounded operational diagnostics, no camera images or personal content.
public class RenderDiagnostics : MonoBehaviour
{
    float next;
    readonly List<XRDisplaySubsystem> displays=new List<XRDisplaySubsystem>();
    void Update()
    {
        if(Time.unscaledTime<next)return;next=Time.unscaledTime+5;
        SubsystemManager.GetSubsystems(displays);
        foreach(var d in displays)Debug.Log("[Raumfenster Render] XR running="+d.running+" passes="+(d.running?d.GetRenderPassCount():0));
        foreach(var c in Camera.allCameras)Debug.Log("[Raumfenster Render] camera="+c.name+" enabled="+c.enabled+" display="+c.targetDisplay+" stereo="+c.stereoTargetEye+" activeStereo="+c.stereoEnabled+" far="+c.farClipPlane+" pos="+c.transform.position+" target="+(c.targetTexture!=null?c.targetTexture.name:"display")+" mask="+c.cullingMask);
        Debug.Log("[Raumfenster Render] delta="+Time.unscaledDeltaTime+" focus="+Application.isFocused);
    }
}
