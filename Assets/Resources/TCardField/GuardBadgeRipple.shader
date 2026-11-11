Shader "T-card/UI/Guard Badge Ripple"
{
    Properties
    {
        [PerRendererData] _MainTex ("UI Texture", 2D) = "white" {}
        _RippleColor ("Ripple Color", Color) = (.192, .843, .776, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Output { float4 vertex:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            sampler2D _MainTex;
            float4 _Ripple, _RippleColor;

            Output vert(Input input)
            {
                Output output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            float2 Wave(float distance, float age, float duration, float expansion, float weight, float aa)
            {
                float life = saturate(age / duration);
                float radius = .30 + expansion * (1 - pow(1 - life, 1.75));
                float fade = saturate(life / .13) * pow(1 - life, 1.7) * weight;
                float delta = abs(distance - radius);
                float lineAlpha = 1 - smoothstep(max(0, .012 - aa), .012 + aa, delta);
                float bloom = exp(-pow(delta / .043, 2)) * .7;
                return float2(lineAlpha, bloom) * fade;
            }

            float4 frag(Output input) : SV_Target
            {
                if (_Ripple.z > .5)
                {
                    float2 uv = input.uv;
                    float center = tex2D(_MainTex, uv).a;
                    float4 nearEdge = float4(
                        tex2D(_MainTex, uv + float2(.018, 0)).a,
                        tex2D(_MainTex, uv - float2(.018, 0)).a,
                        tex2D(_MainTex, uv + float2(0, .018)).a,
                        tex2D(_MainTex, uv - float2(0, .018)).a);
                    float4 farEdge = float4(
                        tex2D(_MainTex, uv + float2(.042, 0)).a,
                        tex2D(_MainTex, uv - float2(.042, 0)).a,
                        tex2D(_MainTex, uv + float2(0, .042)).a,
                        tex2D(_MainTex, uv - float2(0, .042)).a);
                    float edge = max(center, max(max(nearEdge.x, nearEdge.y), max(nearEdge.z, nearEdge.w)))
                               - min(center, min(min(nearEdge.x, nearEdge.y), min(nearEdge.z, nearEdge.w)));
                    float halo = saturate(dot(farEdge, float4(.25, .25, .25, .25)) - center);
                    float alpha = saturate(edge * .82 + halo * .4) * _Ripple.y * _RippleColor.a * input.color.a;
                    return float4(lerp(_RippleColor.rgb, 1, edge * .62) * input.color.rgb * alpha, alpha);
                }
                float distance = length((input.uv - .5) * 2.32);
                float aa = max(.003, fwidth(distance) * .58);
                float2 waves = Wave(distance, _Ripple.x, .95, .667, .82, aa) +
                               Wave(distance, _Ripple.x - .21, .83, .467, .46, aa);
                waves *= _Ripple.y * _RippleColor.a * input.color.a;
                float bloom = saturate(waves.y);
                float lineAlpha = saturate(waves.x);
                float3 core = lerp(_RippleColor.rgb, 1, .82);
                return float4((core * lineAlpha + _RippleColor.rgb * bloom * (1 - lineAlpha)) * input.color.rgb,
                              lineAlpha + bloom * (1 - lineAlpha));
            }
            ENDCG
        }
    }
}
