Shader "T-card/UI/Field Drop Glow"
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
   #pragma target 3.0
   #include "UnityCG.cginc"
   struct appdata {float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
   struct v2f {float4 vertex:SV_POSITION;float2 localPosition:TEXCOORD0;float4 color:COLOR;};
   float4 _Shape,_Appearance,_Motion,_FieldColor,_PanelSize,_Breath;
   v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.localPosition=(v.uv-.5)*_PanelSize.xy;o.color=v.color;return o;}
   float4 frag(v2f i):SV_Target
   {
    float2 halfSize=_Shape.xy*.5;float radius=min(_Shape.z,min(halfSize.x,halfSize.y));
    float2 q=abs(i.localPosition)-halfSize+radius;float d=length(max(q,0))+min(max(q.x,q.y),0)-radius;
    float blur=max(1,_Shape.w),strokeWidth=max(.1,_Motion.z),clock=_Appearance.w;
    float breath=lerp(_Breath.y,_Breath.z,(1-cos(clock/max(.2,_Breath.x)*2*UNITY_PI))*.5);
    float rim=exp(-d*d/(strokeWidth*strokeWidth));float soft=exp(-d*d/(blur*blur))* .62;
    float wisps=0;
    for(int n=0;n<4;n++)
    {
     float u=frac(clock/_Motion.x+n*.25);float offset=4+_Motion.y*u*u*(3-2*u);
     float width=blur*(1+u*.75);float dist=d-offset;
     wisps+=exp(-dist*dist/(width*width))*sin(u*UNITY_PI)*.22*_Motion.w;
    }
    float inside=1-smoothstep(-1,1,d);
    float2 center=float2(sin(clock*.63)*_Shape.x*.095,sin(clock*.49)*_Shape.y*.125);
    float surface=saturate(1-length((i.localPosition-center)/max(halfSize,float2(1,1))));
    float fill=inside*_Appearance.z*(.13+surface*.25);
    float alpha=saturate((rim*.65+soft+wisps+fill)*_Appearance.y*breath*_Appearance.x*_FieldColor.a);
    float3 tint=lerp(_FieldColor.rgb,float3(.81,1,.965),saturate(rim*.6+surface*inside*_Appearance.z*.3));
    return float4(tint,alpha)*i.color;
   }
   ENDCG
  }
 }
}
