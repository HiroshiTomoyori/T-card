Shader "T-card/UI/Attack Motion Focus"
{
 Properties { [PerRendererData] _MainTex ("Texture",2D)="white" {} }
 SubShader
 {
  Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
  Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
  Blend SrcAlpha OneMinusSrcAlpha
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #include "UnityCG.cginc"
   struct appdata{float4 vertex:POSITION;float2 uv:TEXCOORD0;};
   struct v2f{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;};
   float4 _Entries[32],_Colors[32],_Flags[32];
   float4 _ViewSize,_DimColor,_Tune;int _Count;float _Opacity;
   v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
   float distanceToBox(float2 p,float4 box,float radius){float2 halfSize=box.zw*.5;radius=min(radius,min(halfSize.x,halfSize.y));float2 q=abs(p-box.xy)-halfSize+radius;return length(max(q,0))+min(max(q.x,q.y),0)-radius;}
   float4 frag(v2f i):SV_Target
   {
    float2 p=i.uv*_ViewSize.xy;float hole=0;float3 light=0;float glowAlpha=0;
    for(int n=0;n<32;n++)
    {
     if(n>=_Count)break;
     float d=distanceToBox(p,_Entries[n],8*_ViewSize.z);
     // Fade outwards from the zone. Cards inside the wall zone remain fully lit.
     float feather=max(1,_Flags[n].x*_ViewSize.z);
     hole=max(hole,(1-smoothstep(0,feather,max(0,d)))*_Flags[n].y);
     float blur=max(1,_Tune.x),width=max(.3,_Tune.y);
     float halo=exp(-d*d/(blur*blur))*.55+exp(-d*d/(blur*blur*.25))*.35+exp(-d*d/(width*width))*.65;
     float a=saturate(halo*_Colors[n].a*_Opacity)*smoothstep(-2*_ViewSize.z,0,d);
     light+=_Colors[n].rgb*a;glowAlpha+=a;
    }
    float dim=_DimColor.a*_Opacity*(1-hole);
    // The preview's alpha describes visible darkness. Preserve that appearance in linear rendering.
    #ifndef UNITY_COLORSPACE_GAMMA
     dim=1-pow(saturate(1-dim),2.2);
    #endif
    float alpha=saturate(dim+glowAlpha);
    return float4((_DimColor.rgb*dim+light)/max(.0001,alpha),alpha);
   }
   ENDCG
  }
 }
}
