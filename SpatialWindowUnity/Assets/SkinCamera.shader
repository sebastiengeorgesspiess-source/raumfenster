Shader "Raumfenster/SkinCamera" {
 Properties { _YTex("Y",2D)="black"{} _UTex("U",2D)="gray"{} _VTex("V",2D)="gray"{} }
 SubShader { Tags {"RenderType"="Opaque"} Cull Off Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "UnityCG.cginc"
 sampler2D _YTex,_UTex,_VTex;
 struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
 v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_OUTPUT(v2f,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
 fixed4 frag(v2f i):SV_Target {
 float y=tex2D(_YTex,i.uv).a,u=tex2D(_UTex,i.uv).a,v=tex2D(_VTex,i.uv).a;
 // Match XREAL SDK YUVTransRGB's plane/channel convention.
 float3 rgb=float3(y+1.771*u-.8855,y-.3456*u-.7145*v+.53005,y+1.4022*v-.7011);
 return float4(GammaToLinearSpace(saturate(rgb)),1);
 }
 ENDCG
 } }
}
