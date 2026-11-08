Shader "T-card/UI/Wall Destruction Platinum Light"
{
 Properties
 {
  [PerRendererData] _MainTex("Native AE alpha",2D)="white"{}
  _BottomColor("Lower platinum",Color)=(.64,.70,.78,1)
  _TopColor("Upper light",Color)=(.94,.97,1,1)
  _RimColor("Thin platinum rim",Color)=(.62,.79,.9,1)
  _GradientStrength("Gradient",Float)=1
  _RimStrength("Rim",Float)=.55
  _SheenStrength("Soft highlight",Float)=.08
  _CropY("Native crop Y",Vector)=(0,1,0,0)
  _EdgeUV("Edge width",Vector)=(.004,.004,0,0)
 }
 SubShader
 {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="False"}
  Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
  Blend SrcAlpha OneMinusSrcAlpha
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #include "UnityCG.cginc"
   struct appdata{float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
   struct v2f{float4 vertex:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
   sampler2D _MainTex;
   float4 _BottomColor,_TopColor,_RimColor;float2 _CropY,_EdgeUV;
   float _GradientStrength,_RimStrength,_SheenStrength;
   v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
   fixed4 frag(v2f i):SV_Target
   {
    float a=tex2D(_MainTex,i.uv).r;
    float y=_CropY.x+i.uv.y*_CropY.y;
    float t=saturate((y-.25)/.65);t=t*t*(3-2*t);
    float3 platinum=lerp(_BottomColor.rgb,_TopColor.rgb,t);
    float sheen=exp(-pow((y-.77)/.12,2))*_SheenStrength;
    platinum=saturate(platinum+sheen);
    // Keep the low-alpha AE bloom bright; shade only the luminous solid shards.
    float solid=smoothstep(.45,.95,a);
    float3 light=i.color.rgb*lerp(float3(1,1,1),platinum,solid*_GradientStrength);
    float neighbor=a;
    [unroll]for(int v=-1;v<=1;v++)[unroll]for(int u=-1;u<=1;u++)
        neighbor=min(neighbor,tex2D(_MainTex,i.uv+float2(u,v)*_EdgeUV).r);
    float rim=saturate((a-neighbor)*3)*_RimStrength;
    light=lerp(light,i.color.rgb*_RimColor.rgb,rim);
    return fixed4(light,a*i.color.a);
   }
   ENDCG
  }
 }
}
