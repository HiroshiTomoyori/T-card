Shader "T-card/Liquid Resource"
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
   #include "UnityCG.cginc"
   float4 _RectSize,_Panel,_Motion,_Waves,_DryColor,_LiquidColor,_DepthColor,_WaveColor,_BeamColor,_Wave2Color,_GlowColor,_Light;
   struct a2v{float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   struct v2f{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   v2f vert(a2v v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
   float roundBox(float2 p,float2 halfSize,float radius)
   {float2 q=abs(p)-halfSize+radius;return length(max(q,0))+min(max(q.x,q.y),0)-radius;}
   float noise(float t,float seed){float n=floor(t),f=frac(t);f=f*f*(3-2*f);return lerp(frac(sin(n*127.1+seed*311.7)*43758.5453),frac(sin((n+1)*127.1+seed*311.7)*43758.5453),f);}
   float4 frag(v2f i):SV_Target
   {
    float time=_Motion.x;
    if(_Waves.w>.5)
    {
     float age=_Motion.w;
     float rise=saturate(age/.23);
     float env=smoothstep(0,.065,age)*(1-smoothstep(.25,1,age));
     float x=(i.uv.x-.5)*2,y=i.uv.y;
     float radius=.95*(1-y*.67);
     float halo=exp(-pow(x/max(.02,radius),2)*2.4);
     float core=exp(-pow(x/.12,2)*2);
     float heightFade=pow(saturate(1-y/max(.001,rise)),1.3);
     float glow=(halo*.62+core)*heightFade*env;
     float3 col=lerp(_BeamColor.rgb,float3(1,1,.94),saturate(core+halo*.12));
     [unroll]for(int n=0;n<10;n++)
     {
      float yy=frac(age*(1.1+n*.055)+n*.137);
      float xx=sin(n*6.7)*.75;
      float spark=exp(-pow((x-xx)/.018,2)-pow((y-yy)/.032,2))*env*(1-yy);
      glow+=spark*.8;
     }
     return float4(col,saturate(glow))*i.color;
    }
    float2 p=(i.uv-.5)*_RectSize.xy;
    float2 size=max(_Panel.xy,float2(1,1));
    float d=roundBox(p,size*.5,min(_Panel.z,min(size.x,size.y)*.45));
    float aa=max(fwidth(d),.65);
    float body=1-smoothstep(-aa,aa,d);
    float aura=exp(-max(0,d)/max(1,_Light.w*.34))*_Panel.w*.65;
    float2 orbit=p/(size*.5+float2(22,26));
    float ang=atan2(orbit.y,orbit.x);
    float orbitRadius=length(orbit);
    float ring=exp(-pow((orbitRadius-1.04-.08*sin(ang*2+time*.6))/.09,2));
    aura+=ring*_Panel.w*.17*(.6+.4*sin(ang+time*.45));
    float2 bounds=(.5-abs(i.uv-.5))*_RectSize.xy;
    float edgeFade=smoothstep(0,max(18,_Light.w*.75),min(bounds.x,bounds.y));
    float grain=(frac(sin(dot(p,float2(12.9898,78.233)))*43758.5453)-.5)/255;
    aura=max(0,aura+grain)*edgeFade;
    float2 uv=p/size+.5;
    float ripple=_Motion.z;
    float amp=(_Waves.x+_Waves.z*ripple)/size.y;
    float phase=time*_Waves.y;
    float wave=.06+.85*saturate(_Motion.y)+amp*(sin(uv.x*6.283+phase*1.4)*.65+sin(uv.x*10.2-phase*.92)*.35);
    wave+=ripple*.035*sin(uv.x*18-phase*7)*exp(-abs(uv.x-.5)*1.7);
    float fill=1-smoothstep(wave-.004,wave+.004,uv.y);
    float depth=saturate((wave-uv.y)*1.8);
    float flickerTime=time*_Light.z;
    float energy=lerp(.55,.12+.66*noise(flickerTime*.80,2),saturate(_Light.y));
    float3 liquid=lerp(_LiquidColor.rgb,_DepthColor.rgb,depth*.20);
    liquid=lerp(liquid,float3(1,1,1),(.02+.10*noise(flickerTime*.75,31))*_Light.x);
    liquid*=1-(.07+.27*(1-noise(flickerTime*.60,32)))*depth*saturate(_Light.y);
    float3 dry=_DryColor.rgb*(.8+.2*uv.y);
    float3 col=lerp(dry,liquid,fill);
    [unroll]for(int n=0;n<3;n++)
    {
        float2 center=float2(noise(flickerTime*.40,n+11),noise(flickerTime*.34,n+21));
        float radius=.22+.3*noise(flickerTime*.28,n+51);
        float well=exp(-dot(uv-center,uv-center)/(radius*radius)*2);
        float power=(.35+1.55*noise(flickerTime*.95,n+1))*_Light.x;
        col+=lerp(_LiquidColor.rgb,float3(1,1,1),n==1?.35:.75)*well*power*.14*fill*(.4+energy);
    }
    float stripe=exp(-abs(uv.y-wave)*size.y/1.3);
    float wave2=wave-.026+.022*sin(uv.x*7.2-phase*.6);
    float stripe2=exp(-abs(uv.y-wave2)*size.y/1.0);
    float soft1=exp(-abs(uv.y-wave)*size.y/10);
    float soft2=exp(-abs(uv.y-wave2)*size.y/9);
    col+=(_WaveColor.rgb*soft1+_Wave2Color.rgb*soft2)*.22*_Light.x;
    col=lerp(col,_WaveColor.rgb+stripe*.4,saturate(stripe*.9));
    col=lerp(col,_Wave2Color.rgb+stripe2*.3,saturate(stripe2*.85));
    float caustic=pow(saturate(.5+.5*sin(uv.x*10+sin(uv.y*7+phase)+phase*.9)),10);
    caustic*=pow(saturate(.5+.5*sin(uv.y*17+phase*.8)),5)*fill;
    col+=float3(1,1,.96)*caustic*(.2+.55*noise(flickerTime*.72,13))*_Light.x;
    float border=exp(-abs(d+1.4)/1.25)*body;
    col=lerp(col,float3(1,1,.94),border*.6);
    float3 auraColor=lerp(_GlowColor.rgb,float3(1,1,.95),.4+.25*sin(ang+time*.4));
    float alpha=body+aura*(1-body);
    float3 result=lerp(auraColor,col,body);
    return float4(result,saturate(alpha))*i.color;
   }
   ENDCG
  }
 }
}
