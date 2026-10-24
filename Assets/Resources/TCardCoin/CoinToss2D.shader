Shader "T-card/Coin Toss 2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Coin Face", 2D) = "white" {}
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="False" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float4 local:TEXCOORD1; };
            sampler2D _MainTex;
            float4 _FaceRect, _PanelSize, _Pose, _Spin, _Appearance, _Edge;
            float4 _CoinTint, _EdgeDark, _EdgeLight, _EdgeHighlight, _ClipRect;
            v2f vert(appdata v)
            {
                v2f o; o.local=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex); o.color=v.color; o.uv=v.uv; return o;
            }
            float ellipse(float2 p, float2 r)
            {
                float d=length(p/max(r,float2(0.5,0.5)));
                float aa=max(fwidth(d),0.001);
                return 1-smoothstep(1-aa,1+aa,d);
            }
            // Straight-alpha foreground over background.
            float4 over(float4 a,float4 b)
            {
                float alpha=a.a+b.a*(1-a.a);
                return float4((a.rgb*a.a+b.rgb*b.a*(1-a.a))/max(alpha,0.00001),alpha);
            }
            float4 frag(v2f i):SV_Target
            {
                float fit=max(0.0001,min(_PanelSize.x/432,_PanelSize.y/924));
                float2 panel=(i.uv-0.5)*_PanelSize.xy/fit;
                panel.y=-panel.y; panel+=float2(216,462);
                float4 result=float4(0,0,0,saturate(_Appearance.w));
                float2 sh=(panel-float2(_Pose.x,924*0.745))/float2(60-9*_Spin.z,10+7*_Spin.z);
                float shadow=exp(-dot(sh,sh)*1.6)*_Edge.w*(1-0.55*_Spin.z);
                result=over(float4(0,0,0,shadow),result);
                float2 delta=panel-_Pose.xy;
                float cs=cos(_Spin.y),sn=sin(_Spin.y);
                // Match the approved raster preview's image rotation convention.
                float2 q=float2(cs*delta.x-sn*delta.y,sn*delta.x+cs*delta.y);
                float2 radius=float2(_Pose.z*0.5,max(0.5,_Pose.z*abs(_Pose.w)*0.5));
                float edge=_Pose.z*max(0,_Edge.x)*abs(_Spin.x);
                float side=_Spin.x>=0?1:-1;
                float nearest=clamp(q.y,min(0,side*edge),max(0,side*edge));
                float shell=ellipse(float2(q.x,q.y-nearest),radius);
                float faceMask=ellipse(q,radius);
                float ey=sqrt(saturate(1-pow(q.x/max(radius.x,0.5),2)))*radius.y;
                float wall=saturate((side*q.y-ey)/max(edge,0.001));
                float light=pow(saturate(sin(wall*UNITY_PI)),0.7);
                float3 edgeColor=lerp(_EdgeDark.rgb,_EdgeLight.rgb,light);
                float grooves=0.5+0.5*cos(q.x*UNITY_PI*2/max(1,_Edge.y));
                edgeColor*=lerp(1,0.65+0.45*grooves,saturate(_Edge.z));
                float bevel=1-smoothstep(0.3,1.1,abs(side*q.y-ey));
                edgeColor=lerp(edgeColor,_EdgeHighlight.rgb,bevel*0.55);
                result=over(float4(edgeColor*_CoinTint.rgb,shell*_CoinTint.a),result);
                float2 uv=float2(q.x/max(1,_Pose.z)+0.5,0.5-q.y/max(1,radius.y*2));
                float2 sampleUV=_FaceRect.xy+saturate(uv)*_FaceRect.zw;
                float4 face=tex2D(_MainTex,sampleUV);
                float luma=dot(face.rgb,float3(0.2126,0.7152,0.0722));
                face.rgb=lerp(luma.xxx,face.rgb,_Appearance.y)*_CoinTint.rgb*_Appearance.x*(0.76+0.24*abs(_Pose.w));
                float sheenCenter=0.3+0.25*sin(_Spin.w*3);
                float shine=exp(-pow((uv.x-sheenCenter)/0.15,2))*_Appearance.z;
                face.rgb=lerp(face.rgb,float3(1,0.92,0.73),shine);
                face.a*=faceMask*_CoinTint.a;
                result=over(face,result);
                result*=i.color;
                #ifdef UNITY_UI_CLIP_RECT
                result.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a-0.001);
                #endif
                return result;
            }
            ENDCG
        }
    }
}
