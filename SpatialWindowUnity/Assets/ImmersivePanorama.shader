Shader "Raumfenster/ImmersivePanorama" {
 Properties { _MainTex("Panorama",2D)="white"{} _Dream("Dream effect",Float)=0 _AnimationTime("Animation time",Float)=0 }
 SubShader { Tags { "RenderType"="Opaque" } Cull Off
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "UnityCG.cginc"
 sampler2D _MainTex; float _Dream,_AnimationTime;
 struct appdata { float4 vertex:POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID };
 struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
 v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_OUTPUT(v2f,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
 fixed4 frag(v2f i):SV_Target {
 UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
 float t=_AnimationTime;float2 uv=i.uv;
 // Periodic longitude terms preserve the panorama wrap. Pole distortion fades out.
 float pole=sin(uv.y*UNITY_PI);
 uv+=_Dream*.0018*pole*float2(sin(uv.x*UNITY_PI*12+t*.35),sin(uv.x*UNITY_PI*8+uv.y*9-t*.28));
 float3 c=tex2D(_MainTex,uv).rgb;
 if(_Dream>.5){
  float angle=.7+.8*sin(t*.12+uv.x*UNITY_PI*2);
  float3 axis=normalize(float3(1,1,1));
  float3 shifted=c*cos(angle)+cross(axis,c)*sin(angle)+axis*dot(axis,c)*(1-cos(angle));
  c=lerp(c,saturate(shifted),.65);
 }else{
  // Gentle glow changes in bright neon/reflections, not a global flashing effect.
  float highlights=smoothstep(.3,.85,max(c.r,max(c.g,c.b)));
  c*=1+highlights*.045*sin(t*.8+uv.x*UNITY_PI*8);
 }
 return float4(c,1);
 }
 ENDCG
 }
 }
}
