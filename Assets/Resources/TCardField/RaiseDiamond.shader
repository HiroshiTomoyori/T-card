Shader "T-card/UI/Raise Diamond"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _GemTint ("Gem Tint", Color) = (.63,.30,.98,1)
        _Lit ("Charge", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Vertex { float4 position : POSITION; float2 uv : TEXCOORD0; };
            struct Fragment { float4 position : SV_POSITION; float2 uv : TEXCOORD0; };
            half4 _GemTint;
            half _Lit;

            Fragment vert(Vertex v)
            {
                Fragment o;
                o.position = UnityObjectToClipPos(v.position);
                o.uv = v.uv;
                return o;
            }

            half4 frag(Fragment i) : SV_Target
            {
                float2 p = (i.uv - .5) * 4;
                float d = abs(p.x) + abs(p.y) - 1;
                float aa = max(fwidth(d), .015);
                half inside = 1 - smoothstep(-aa, aa, d);
                half edge = 1 - smoothstep(.035, .11 + aa, abs(d));
                half lit = smoothstep(0, 1, _Lit);
                half fadeFlash = pow(sin(UNITY_PI * saturate((1 - _Lit) * 1.9)), 4) * step(.47, _Lit);
                half flicker = .94 + .06 * cos(_Time.y * 2.0);
                half halo = exp(-max(0,d) * max(0,d) * 8) * (.53 * lit + .19 * fadeFlash) * (1 - inside);
                half facet = .36 + .42 * saturate(.5 + p.y * .7 - p.x * .35)
                    + .22 * step(-p.y * .55, p.x);
                half3 face = lerp(half3(.12,.08,.18), _GemTint.rgb * facet, lit * flicker);
                half3 rim = lerp(half3(.39,.28,.51), half3(.91,.79,1), lit);
                half body = saturate(inside * .94 + edge * .62);
                half3 color = lerp(face, rim, edge * .8) * body + _GemTint.rgb * halo * (1 - body);
                return half4(color, body + halo * (1 - body));
            }
            ENDCG
        }
    }
}
