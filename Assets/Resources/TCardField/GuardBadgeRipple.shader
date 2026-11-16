Shader "T-card/UI/Guard Badge Ripple"
{
    Properties
    {
        [PerRendererData] _MainTex ("UI Texture", 2D) = "white" {}
        _RippleColor ("Glow Tint", Color) = (.192, .843, .776, 1)
        [HideInInspector] _Strength ("Effect Strength", Float) = 1
        [HideInInspector] _Ripple ("Phase, mode, spread, secondary strength", Vector) = (0,0,1,0)
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
            float4 _Ripple, _RippleColor;
            float _Strength;
            static const float Tau = 6.2831853;

            Output vert(Input input)
            {
                Output output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            float SegmentDistanceSquared(float2 p, float2 a, float2 b)
            {
                float2 ab = b - a;
                float2 offset = p - a - ab * saturate(dot(p - a, ab) / dot(ab, ab));
                return dot(offset, offset);
            }

            // Half of the outer path from the approved AE shield, in units of the badge width.
            float ShieldEdgeDistance(float2 p)
            {
                p.x = abs(p.x);
                float d = SegmentDistanceSquared(p, float2(0, -.422), float2(.074, -.358));
                d = min(d, SegmentDistanceSquared(p, float2(.074, -.358), float2(.164, -.315)));
                d = min(d, SegmentDistanceSquared(p, float2(.164, -.315), float2(.255, -.283)));
                d = min(d, SegmentDistanceSquared(p, float2(.255, -.283), float2(.332, -.265)));
                d = min(d, SegmentDistanceSquared(p, float2(.332, -.265), float2(.331, -.145)));
                d = min(d, SegmentDistanceSquared(p, float2(.331, -.145), float2(.304, .016)));
                d = min(d, SegmentDistanceSquared(p, float2(.304, .016), float2(.246, .158)));
                d = min(d, SegmentDistanceSquared(p, float2(.246, .158), float2(.147, .291)));
                d = min(d, SegmentDistanceSquared(p, float2(.147, .291), float2(0, .407)));
                return sqrt(d);
            }

            float Gaussian(float d, float width)
            {
                float scaled = d / width;
                return exp2(-1.442695 * scaled * scaled);
            }

            float2 Wave(float distance, float angle, float phase, float offset, float weight, float aa)
            {
                float age = frac(phase - offset);
                float radius = .44 + .97 * (1 - pow(1 - age, 1.45));
                float fade = saturate(age / .11) * pow(1 - age, 1.2) * weight;
                float delta = abs(distance - radius);
                float crest = 1 - smoothstep(max(0, .007 - aa), .007 + aa, delta);
                float arc = pow(abs(cos(angle - Tau * phase + offset * 8)), 16);
                float inner = abs(distance - (radius - .025));
                float travellingCrest = (1 - smoothstep(.003, .011 + aa, inner)) * arc;
                return fade * float2(.42 * crest + .58 * travellingCrest,
                    .24 * Gaussian(delta, .074) + .36 * Gaussian(delta, .025));
            }

            float2 Sparkles(float2 p, float phase, float aa)
            {
                float2 light = 0;
                [loop]
                for (int index = 0; index < 25; index++)
                {
                    float n = (float)index;
                    float age = frac(phase - n * .17331);
                    if (age >= .72) continue;
                    float progress = age / .72;
                    float radius = .41 + progress * (.59 + frac(n * .39) * .58);
                    float angle = frac(n * .618034) * Tau + .24 * progress;
                    float2 offset = abs(p - float2(cos(angle), .90 * sin(angle)) * radius);
                    if (max(offset.x, offset.y) > .082) continue;
                    float twinkle = cos(progress * UNITY_PI * 3);
                    float flicker = pow(saturate(sin(progress * UNITY_PI)), 1.6)
                        * (.67 + .33 * twinkle * twinkle);
                    float size = .007 + frac(n * .37) * .012;
                    float delta = length(offset);
                    float sparkCore = 1 - smoothstep(size * .22, size * .72 + aa, delta);
                    if (index % 5 == 0)
                    {
                        float arms = min(offset.x + offset.y * .22, offset.y + offset.x * .29);
                        sparkCore = max(sparkCore, (1 - smoothstep(size * .28, size * .55 + aa, arms))
                            * (1 - smoothstep(size, size * 3, delta)));
                    }
                    light += flicker * float2(sparkCore, .32 * Gaussian(delta, size * 1.8 + aa));
                }
                return light;
            }

            float4 frag(Output input) : SV_Target
            {
                float2 p = (input.uv - .5) * float2(3.3, -3.3);
                float aa = max(.002, .55 * fwidth(p.x));
                float core = 0;
                float bloom = 0;
                if (_Ripple.y < .5)
                {
                    float edge = ShieldEdgeDistance(p);
                    float spread = _Ripple.z * lerp(.83, 1.08, _Ripple.x);
                    bloom = _Strength * (.40 * Gaussian(edge, .27 * spread)
                        + .34 * Gaussian(edge, .106 * spread) + .20 * Gaussian(edge, .040));
                    bloom *= 1 - smoothstep(1.42, 1.64, max(abs(p.x), abs(p.y)));
                }
                else if (_Ripple.y < 1.5)
                {
                    float edge = ShieldEdgeDistance(p);
                    float angle = atan2(p.y, p.x);
                    float reflection = pow(saturate(.5 + .5 * cos(angle - _Ripple.x * Tau)), 12);
                    core = (1 - smoothstep(.004, .014 + aa, edge)) * (.29 + .71 * reflection);
                    bloom = Gaussian(edge, .030) * (.21 + .30 * reflection);
                    core *= _Strength * lerp(.68, 1, _Ripple.w);
                    bloom *= _Strength;
                }
                else
                {
                    float2 ellipse = p * float2(1, 1.097);
                    float distance = length(ellipse);
                    float angle = atan2(ellipse.y, ellipse.x);
                    float2 light = (_Strength / 1.8) * (
                        Wave(distance, angle, _Ripple.x, 0, 1, aa)
                        + Wave(distance, angle, _Ripple.x, .18, .66, aa)
                        + Wave(distance, angle, _Ripple.x, .42, .34, aa));
                    if (_Ripple.w > 0) light += _Ripple.w * Sparkles(p, _Ripple.x, aa);
                    core = light.x;
                    bloom = light.y;
                }

                core = saturate(core);
                bloom = saturate(bloom) * (1 - core);
                float opacity = input.color.a * _RippleColor.a;
                float3 color = (lerp(_RippleColor.rgb, 1, .83) * core + _RippleColor.rgb * bloom) * input.color.rgb;
                return float4(color * opacity, (core + bloom) * opacity);
            }
            ENDCG
        }
    }
}
