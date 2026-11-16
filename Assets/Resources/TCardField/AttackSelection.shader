Shader "T-card/UI/Attack Selection"
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
   #include "CardActionGlowProfile.cginc"
   struct appdata{float4 vertex:POSITION;float2 uv:TEXCOORD0;};
   struct v2f{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;};
   float4 _Entries[32],_Colors[32],_Flags[32];
   float4 _GuardBadgeCutouts[32];
   float4 _DefenseGuardArtwork[32], _DefenseGuardShape, _DefenseGuardLight, _DefenseGuardColor;
   float4 _WallTune;
   float4 _ViewSize,_DimColor,_HighlightColor,_Tune;
   float4 _IncomingCard, _IncomingRipple, _IncomingRippleColor;
   int _Count;float _Opacity;
   v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
   float distanceToBox(float2 p,float4 box,float radius){float2 halfSize=box.zw*.5;radius=min(radius,min(halfSize.x,halfSize.y));float2 q=abs(p-box.xy)-halfSize+radius;return length(max(q,0))+min(max(q.x,q.y),0)-radius;}
   float softLight(float d, float width)
   {
    float x = d / width;
    return exp(-x*x);
   }
   // x is the diffuse gradient, y the narrow luminous core.
   float2 incomingWave(float radius, float angle, float age, float duration, float growth, float weight, float aa)
   {
    float t = saturate(age / duration);
    float fade = smoothstep(0,.16,t)*pow(1-t,1.6)*weight;
    float edge = abs(radius - (.377 + growth*(1-pow(1-t,2.35))));
    float reflection = pow(abs(cos(angle - .64 - t*.4)), 9);
    float core = softLight(edge, .0028 + aa)*(.40 + reflection*.47);
    float halo = .54*softLight(edge,.030) + .24*softLight(edge,.061);
    return float2(halo, core)*fade;
   }
   float incomingMotes(float2 p, float age, float aa)
   {
    float light = 0;
    [unroll]
    for (int i = 0; i < 8; i++)
    {
     float angle = .4 + i*2.399963;
     float t = saturate((age - (.10 + i*.047)) / (.54 + i*.024));
     float distance = length(p-float2(cos(angle),sin(angle))*(.40+.14*(1-(1-t)*(1-t))));
     float flicker = sin(t*UNITY_PI);
     light += softLight(distance,.004+aa)*flicker*flicker*.42;
    }
    return light;
   }
   float4 incomingLight(float2 pixel)
   {
    float height = max(1,_IncomingCard.w);
    float2 offset = (pixel-_IncomingCard.xy)/height;
    float radius = length(offset);
    float age = _IncomingRipple.x;
    float spread = _IncomingRipple.z;
    if (_IncomingRipple.y <= 0 || age >= 1.15 || radius > .85*max(1,spread)) return 0;

    float d = distanceToBox(pixel,_IncomingCard,8*_ViewSize.z);
    float outside = smoothstep(-.3,1.15,d);
    if (outside <= 0) return 0;

    float aa = .67/height;
    float angle = atan2(offset.y,offset.x);
    float2 light = incomingWave(radius,angle,age,1.08,.235*spread,1,aa)
        + incomingWave(radius,angle,age-.18,.96,.204*spread,.55,aa);
    light.y += incomingMotes(offset,age,aa);

    float cardTime = saturate(age/.68);
    float cardFlash = smoothstep(0,.17,cardTime)*pow(1-cardTime,1.45);
    light.x += cardFlash*(.60*softLight(d/height,.063)+.35*softLight(d/height,.027));
    light.y += cardFlash*.62*softLight(d/height,.009+aa);

    light *= outside*_IncomingRipple.y*_IncomingRippleColor.a;
    float core = saturate(light.y);
    float halo = saturate(light.x)*(1-core);
    float3 color = _IncomingRippleColor.rgb*halo + lerp(_IncomingRippleColor.rgb,1,.54)*core;
    return float4(color,core+halo);
   }
   float4 frag(v2f i):SV_Target
   {
    float2 p=i.uv*_ViewSize.xy;float hole=0,highlight=0;float3 light=0;float glowAlpha=0;
    for(int n=0;n<32;n++)
    {
     if(n>=_Count)break;
     float d=distanceToBox(p,_Entries[n],lerp(8*_ViewSize.z,_WallTune.x,_Flags[n].z));
     float inside = 1-smoothstep(-1,1,d);
     if (_Flags[n].z > .5)
     {
      float feather = max(1,_WallTune.y);
      inside = 1-smoothstep(-min(feather*.17,6*_ViewSize.z),feather*1.03,d);
     }
     bool defenseGuard = _Flags[n].w > 1.5;
     float guardAlpha = 0;
     if (defenseGuard)
     {
      float4 artwork = _DefenseGuardArtwork[n];
      float scale = artwork.z / 118;
      float radius = _DefenseGuardShape.w * scale;
      float cardEdge = distanceToBox(p,artwork,radius);
      // Clear only the artwork and feather its edge. The glow is composited above the lowlight.
      inside = 1-smoothstep(-1,max(2,3.5*scale),cardEdge);
      float4 outline = artwork;
      outline.xy += _DefenseGuardShape.xy * scale;
      outline.zw += 2 * _DefenseGuardShape.z * scale;
      float edge = distanceToBox(p,outline,radius);
      guardAlpha = CardActionGlowAlpha(edge,max(1,_DefenseGuardLight.x*scale),
          max(.1,_DefenseGuardLight.y*scale),_DefenseGuardLight.z*_DefenseGuardColor.a)
          *smoothstep(-1,1,cardEdge)*_Opacity;
     }
     if (_Flags[n].w > .5)
     {
      float4 badge = _GuardBadgeCutouts[n];
      float fromBadge = length(p-badge.xy)/max(1,badge.z);
      float badgeInside = 1-smoothstep(.48,.74+.24*badge.w,fromBadge);
      inside = max(inside,badgeInside);
     }
     hole=max(hole,inside*_Flags[n].x);highlight=max(highlight,inside*_Flags[n].y);
     if (defenseGuard)
     {
      light=_DefenseGuardColor.rgb*guardAlpha+light*(1-guardAlpha);
      glowAlpha=guardAlpha+glowAlpha*(1-guardAlpha);
      continue;
     }
     float blur=max(1,lerp(_Tune.x,_WallTune.y,_Flags[n].z)),strokeWidth=max(.3,_Tune.y);
     float halo=exp(-d*d/(blur*blur))* .55+exp(-d*d/(blur*blur*.25))*.35+exp(-d*d/(strokeWidth*strokeWidth))*.65;
     float outside = smoothstep(-2*_ViewSize.z,0,d);
     if (_Flags[n].z > .5)
     {
      // Keep the luminous boundary outside the cards; let the outer tail dissolve gradually.
      float falloff = max(0,d);
      halo = .72*softLight(falloff,blur*.92) + .26*softLight(falloff,blur*1.82);
      outside = smoothstep(-min(blur*.20,7*_ViewSize.z),blur*.20,d);
     }
     float a=saturate(halo*_Colors[n].a*_Opacity*_Tune.z)*outside;
     light=_Colors[n].rgb*a+light*(1-a);
     glowAlpha=a+glowAlpha*(1-a);
    }
    float dim=_DimColor.a*_Opacity*(1-hole);float high=highlight*_Opacity;
    // Put the card light over the dimmed board, so the shade never mixes into its color.
    float baseAlpha=high+dim*(1-high);
    float alpha=glowAlpha+baseAlpha*(1-glowAlpha);
    float3 premul=light+(_HighlightColor.rgb*high+_DimColor.rgb*dim*(1-high))*(1-glowAlpha);
    if (_IncomingRipple.y > 0)
    {
     float4 incoming = incomingLight(p);
     premul = incoming.rgb + premul*(1-incoming.a);
     alpha = incoming.a + alpha*(1-incoming.a);
    }
    return float4(premul/max(.0001,alpha),alpha);
   }
   ENDCG
  }
 }
}
