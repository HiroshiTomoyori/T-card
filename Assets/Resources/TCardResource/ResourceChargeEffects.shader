Shader "T-card/Resource Charge Effects"
{
 Properties {
 [PerRendererData] _MainTex("Texture",2D)="white"{}
 _StencilComp("Stencil Comparison",Float)=8
 _Stencil("Stencil ID",Float)=0
 _StencilOp("Stencil Operation",Float)=0
 _StencilWriteMask("Stencil Write Mask",Float)=255
 _StencilReadMask("Stencil Read Mask",Float)=255
 _ColorMask("Color Mask",Float)=15
 }
 SubShader {
 Tags{"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
 Stencil{Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask]}
 Cull Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha ColorMask [_ColorMask]
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma target 3.0
 #include "UnityCG.cginc"
 float4 _RectSize,_Panel,_Motion,_Shape,_Beam,_Timing,_Root,_Target,_BeamColor,_RootColor,_GlowColor,_WaveColor;
 float _TargetGlow;
 struct a2v{float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 struct v2f{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 v2f vert(a2v v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
 float rounded(float2 p,float2 size,float radius){float2 q=abs(p)-size*.5+radius;return length(max(q,0))+min(max(q.x,q.y),0)-radius;}
 float4 frag(v2f i):SV_Target {
  if(_Panel.w>.5){
   float2 p=(i.uv-.5)*_RectSize.xy;float d=rounded(p,_Panel.xy,min(_Panel.z,min(_Panel.x,_Panel.y)*.45));
   float entry=smoothstep(0,.16,_Motion.z),period=max(.15,_Target.x),ripple=0;
   [unroll]for(int n=0;n<2;n++){
    float phase=frac(_Motion.z/period+n*.5),spread=(3+phase*_Target.w);
    float rd=rounded(p,_Panel.xy+float2(spread*2,spread),_Panel.z+spread*.5);
    float env=pow(max(0,sin(phase*3.14159265)),1.6)*(1-phase);
    float direction=.4+.6*saturate(.5+(p.x-p.y)/max(1,_Panel.x));
    ripple+=exp(-abs(rd)/max(.1,_Target.z))*env*_Target.y*direction;
   }
   float border=exp(-abs(d)/max(.5,_Target.z))*.6;
   float glow=exp(-max(0,d)/12)*_TargetGlow*.18;
   float alpha=saturate((border+ripple+glow)*entry);
   float2 edge=(.5-abs(i.uv-.5))*_RectSize.xy;alpha*=smoothstep(0,8,min(edge.x,edge.y));
   return float4(lerp(_GlowColor.rgb,_WaveColor.rgb,saturate(ripple+border)),alpha)*i.color;
  }
  // y=0 is the water surface. A shallow horizontal glow covers the base.
  float x=(i.uv.x-.5)*_RectSize.x,y=i.uv.y*_RectSize.y-_Shape.w;
  float age=_Motion.y,fade=smoothstep(0,.09,age)*pow(1-smoothstep(_Timing.x,1,age),max(.1,_Timing.y));
  float rise=smoothstep(0,max(.02,_Beam.w),age),u=max(0,y)/max(1,_Shape.y),tip=1-smoothstep(max(0,rise-.12),max(.001,rise),u);
  float width=max(.2,_Shape.x*.5*(1-u*.83)),sway=sin(u*3+_Motion.x*.2)*_Timing.z*_Shape.x*.1;
  float halo=exp(-pow((x-sway)/width,2)*lerp(3.5,1.4,saturate(_Beam.z)))*(.38+.25*_Beam.z);
  float inner=exp(-pow((x-sway)/max(.5,width*.5),2)*2.2)*.32;
  float core=_Beam.y<=0?0:exp(-pow(x/max(.25,_Beam.y*.5),2)*2)*.8;
  float filaments=exp(-pow((abs(x)-width*.16-sin(u*5+_Motion.x*.2)*_Timing.z*2)/.6,2))*_Timing.w;
  float base=smoothstep(0,5,y),top=pow(saturate(1-u),.7)*tip;
  float column=(halo+inner+core+filaments)*base*top*fade*_Beam.x;
  float rootEnv=smoothstep(0,.05,age)*(1-smoothstep(.18,1+_Root.y,age));
  float pool=exp(-pow(x/max(1,_Shape.z*.5),2)*2-pow(y/max(1,_Shape.w*.5),2)*2)*rootEnv*_Root.x;
  float heart=exp(-pow(x/max(1,_Shape.z*.23),2)*2-pow(y/max(1,_Shape.w*.13),2)*2)*rootEnv*_Root.x;
  float alpha=saturate(column+pool*.62+heart*.46);
  float3 col=lerp(_BeamColor.rgb,float3(1,1,.96),saturate(core*fade+heart*.6));col=lerp(col,_RootColor.rgb,saturate(pool*.35));
  float2 edge=(.5-abs(i.uv-.5))*_RectSize.xy;alpha*=smoothstep(0,8,min(edge.x,edge.y));
  return float4(col,alpha)*i.color;
 }
 ENDCG
 }
 }
}
