Shader "T-card/Card Boundary Shadow"
{
 Properties
 {
  [PerRendererData] _MainTex("Texture",2D)="white"{}
  _StencilComp("Stencil Comparison",Float)=8
  _Stencil("Stencil ID",Float)=0
  _StencilOp("Stencil Operation",Float)=0
  _StencilWriteMask("Stencil Write Mask",Float)=255
  _StencilReadMask("Stencil Read Mask",Float)=255
  _ColorMask("Color Mask",Float)=15
 }
 SubShader
 {
  Tags{"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
  Stencil{Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask]}
  Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
  Blend SrcAlpha OneMinusSrcAlpha ColorMask [_ColorMask]
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
   #include "UnityCG.cginc"
   #include "UnityUI.cginc"
   float4 _RectSize,_Card,_Shadow,_Offset,_ShadowColor,_ClipRect;
   struct a2v{float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   struct v2f{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;float4 local:TEXCOORD1;};
   v2f vert(a2v v){v2f o;o.local=v.vertex;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
   float box(float2 p,float2 halfSize,float radius){float2 q=abs(p)-halfSize+radius;return length(max(q,0))+min(max(q.x,q.y),0)-radius;}
   float fog(float distance,float blur){float z=distance/max(.15,blur*.5);return .5-.5*z/sqrt(1+z*z);}
   float4 frag(v2f i):SV_Target
   {
    float2 p=(i.uv-.5)*_RectSize.xy;
    float outer=box(p-_Offset.xy,_Card.xy*.5,_Card.z);
    float contact=box(p,_Card.xy*.5,_Card.z);
    float a=fog(outer,_Shadow.y*_Card.w)*_Shadow.x;
    float b=fog(contact,_Shadow.w*_Card.w)*_Shadow.z;
    float alpha=(1-(1-a)*(1-b))*_ShadowColor.a*i.color.a;
    float2 bounds=(.5-abs(i.uv-.5))*_RectSize.xy;
    alpha*=smoothstep(0,max(.5,_Shadow.y*_Card.w),min(bounds.x,bounds.y));
    #ifdef UNITY_UI_CLIP_RECT
    alpha*=UnityGet2DClipping(i.local.xy,_ClipRect);
    #endif
    return float4(_ShadowColor.rgb,alpha);
   }
   ENDCG
  }
 }
}
