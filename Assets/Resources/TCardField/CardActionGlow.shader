Shader "T-card/UI/Card Action Glow"
{
 Properties
 {
  [PerRendererData] _MainTex ("Texture",2D)="white" {}
  _StencilComp ("Stencil Comparison",Float)=8
  _Stencil ("Stencil ID",Float)=0
  _StencilOp ("Stencil Operation",Float)=0
  _StencilWriteMask ("Stencil Write Mask",Float)=255
  _StencilReadMask ("Stencil Read Mask",Float)=255
  _ColorMask ("Color Mask",Float)=15
 }
 SubShader
 {
  Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane"}
  Stencil {Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask]}
  Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
  Blend SrcAlpha OneMinusSrcAlpha
  ColorMask [_ColorMask]
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct appdata {float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
   struct v2f {float4 vertex:SV_POSITION;float2 local:TEXCOORD0;float4 color:COLOR;};
   float4 _Shape,_Light,_GlowColor,_PanelSize;
   v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.local=(v.uv-.5)*_PanelSize.xy;o.color=v.color;return o;}
   float4 frag(v2f i):SV_Target
   {
    float2 halfSize=_Shape.xy*.5;float radius=min(_Shape.z,min(halfSize.x,halfSize.y));
    float2 q=abs(i.local)-halfSize+radius;float d=length(max(q,0))+min(max(q.x,q.y),0)-radius;
    float blur=max(1,_Shape.w),lineWidth=max(.1,_Light.y);
    float broad=exp(-d*d/(blur*blur*1.44))*.43;
    float mid=exp(-d*d/(blur*blur*.5625))*.4;
    float narrow=exp(-d*d/(blur*blur*.16))*.3;
    float rim=exp(-d*d/(lineWidth*lineWidth))*.6;
    float alpha=saturate((broad+mid+narrow+rim)*_Light.x*_GlowColor.a);
    return float4(_GlowColor.rgb,alpha)*i.color;
   }
   ENDCG
  }
 }
}
