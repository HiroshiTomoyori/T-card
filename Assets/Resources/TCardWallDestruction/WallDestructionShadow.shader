Shader "T-card/UI/Wall Destruction Soft Shadow"
{
 Properties { [PerRendererData] _MainTex("AE silhouette",2D)="white"{} _BlurUV("Softness",Vector)=(.04,.04,0,0) }
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
   #include "UnityCG.cginc"
   struct appdata{float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
   struct v2f{float4 vertex:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
   sampler2D _MainTex;float2 _BlurUV;
   v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
   float mask(float2 uv){return tex2D(_MainTex,uv).r*step(0,uv.x)*step(uv.x,1)*step(0,uv.y)*step(uv.y,1);}
   fixed4 frag(v2f i):SV_Target
   {
    float a=0;
    [unroll]for(int y=-2;y<=2;y++)[unroll]for(int x=-2;x<=2;x++)
    {
     float wx=x==0?6:(abs(x)==1?4:1),wy=y==0?6:(abs(y)==1?4:1);
     a+=mask(i.uv+float2(x,y)*_BlurUV)*wx*wy/256;
    }
    return fixed4(i.color.rgb,a*i.color.a);
   }
   ENDCG
  }
 }
}
