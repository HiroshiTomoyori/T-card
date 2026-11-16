Shader "T-card/UI/Monarch Wall Ornament Glow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Ornament", 2D) = "white" {}
        _InkTint ("Ink Tint", Color) = (1,1,1,1)
        _Breath ("Breath", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            half4 _InkTint;
            half _Breath;

            struct Vertex
            {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct Fragment
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            Fragment vert(Vertex v)
            {
                Fragment o;
                o.position = UnityObjectToClipPos(v.position);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            half ringAlpha(float2 uv, float2 radius)
            {
                return (
                    tex2D(_MainTex, uv + float2(radius.x, 0)).a +
                    tex2D(_MainTex, uv - float2(radius.x, 0)).a +
                    tex2D(_MainTex, uv + float2(0, radius.y)).a +
                    tex2D(_MainTex, uv - float2(0, radius.y)).a +
                    tex2D(_MainTex, uv + radius * .707).a +
                    tex2D(_MainTex, uv - radius * .707).a +
                    tex2D(_MainTex, uv + radius * float2(.707, -.707)).a +
                    tex2D(_MainTex, uv - radius * float2(.707, -.707)).a) * .125;
            }

            half4 frag(Fragment i) : SV_Target
            {
                half4 art = tex2D(_MainTex, i.uv);
                float2 unit = _MainTex_TexelSize.xy;
                half closeLight = ringAlpha(i.uv, unit * 4.5);
                half wideLight = ringAlpha(i.uv, unit * 13.5);
                half ink = saturate(art.a * 1.18 + ringAlpha(i.uv, unit * 1.4) * .48);
                half white = saturate(closeLight * 1.6 + wideLight * .92 + art.a * .22)
                    * lerp(.76, .97, _Breath) * (1 - ink);
                return half4(saturate(art.rgb * _InkTint.rgb) * ink + white, ink + white) * i.color.a;
            }
            ENDCG
        }
    }
}
