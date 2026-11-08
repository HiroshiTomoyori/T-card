Shader "T-card/UI/Wall Destruction Gradient Backdrop"
{
 Properties { _Falloff("Gradient shape",Float)=1.8 }
 SubShader
 {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
  Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
  Blend SrcAlpha OneMinusSrcAlpha
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct appdata{float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
   struct v2f{float4 vertex:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
   float _Falloff;
   v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.color=v.color;o.uv=v.uv;return o;}
   fixed4 frag(v2f i):SV_Target
   {
    float radius=length((i.uv-.5)*2);
    float gradient=exp(-3.5*pow(radius,max(.5,_Falloff)))*(1-smoothstep(.65,1,radius));
    return fixed4(i.color.rgb,i.color.a*gradient);
   }
   ENDCG
  }
 }
}
