Shader "T-card/UI/Confirmation Glass"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Fill ("Glass", Color) = (0.067, 0.114, 0.196, 0.67)
        _Lower ("Lower light", Color) = (0.345, 0.475, 0.624, 1)
        _Frame ("Frame", Color) = (0.678, 0.757, 0.855, 0.41)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha

        // The active scene uses the Built-in pipeline and an Overlay Canvas.
        GrabPass { "_TCardConfirmationBackdrop" }
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _TCardConfirmationBackdrop;
            float4 _Fill, _Lower, _Frame, _Shape, _Look;
            struct Input { float4 position : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Output { float4 position : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; float4 grab : TEXCOORD1; };
            Output vert(Input input)
            {
                Output output;
                output.position = UnityObjectToClipPos(input.position);
                output.grab = ComputeGrabScreenPos(output.position);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }
            float roundedBox(float2 localPoint, float2 halfSize, float radius)
            {
                float2 q = abs(localPoint) - halfSize + radius;
                return length(max(q, 0)) + min(max(q.x, q.y), 0) - radius;
            }
            float3 blurBackdrop(float2 uv)
            {
                if (_Look.y <= .01) return tex2D(_TCardConfirmationBackdrop, uv).rgb;
                float2 pixel = 1 / _ScreenParams.xy;
                float2 limit = pixel * .5;
                float2 spacing = _Look.y * pixel * .34;
                const float weights[7] = { .03125, .109375, .21875, .28125, .21875, .109375, .03125 };
                float3 result = 0;
                // Rotate a denser Gaussian kernel so samples do not follow the card edges.
                [unroll] for (int y = 0; y < 7; y++)
                {
                    [unroll] for (int x = 0; x < 7; x++)
                    {
                        float2 p = float2(x - 3, y - 3);
                        float2 offset = float2(dot(p, float2(.9063, -.4226)), dot(p, float2(.4226, .9063)));
                        float2 sampleUv = clamp(uv + spacing * offset, limit, 1 - limit);
                        result += tex2D(_TCardConfirmationBackdrop, sampleUv).rgb * weights[x] * weights[y];
                    }
                }
                return result;
            }
            float3 tintBackdrop(float3 backdrop, float3 fill, float3 lower, float gradient, float opacity)
            {
                // Match the design's sRGB tint in projects that render in linear space.
                #ifndef UNITY_COLORSPACE_GAMMA
                backdrop = LinearToGammaSpace(backdrop);
                fill = LinearToGammaSpace(fill);
                lower = LinearToGammaSpace(lower);
                #endif
                float gray = dot(backdrop, float3(.2126, .7152, .0722));
                float3 result = lerp(lerp(gray.xxx, backdrop, .70), lerp(fill, lower, gradient), opacity);
                #ifndef UNITY_COLORSPACE_GAMMA
                result = GammaToLinearSpace(result);
                #endif
                return result;
            }
            float4 frag(Output input) : SV_Target
            {
                float2 size = max(_Shape.xy, 1);
                float2 localPoint = (input.uv - .5) * (size + 2 * _Shape.w);
                float2 uv = saturate(localPoint / size + .5);
                float radius = min(_Shape.z, min(size.x, size.y) * .5);
                float sdf = roundedBox(localPoint, size * .5, radius);
                float feather = max(fwidth(sdf), .32);
                float edge = 1 - smoothstep(-feather * .5, feather * .5, sdf);
                float shadowDistance = max(0, roundedBox(localPoint + float2(0, _Shape.w * .25), size * .5, radius));
                float shadow = (1-edge) * (1-smoothstep(0, _Shape.w, shadowDistance)) * lerp(.31, .26, _Look.x);
                float y = 1 - uv.y;
                float rise = lerp(.18 * saturate((y - .27) / .25), .18 + .56 * saturate((y - .52) / .48), step(.52, y));
                float3 fill = _Fill.rgb * lerp(.90, 1.08, uv.y);
                float alpha = _Fill.a;
                if (_Look.x < .5 && edge > 0)
                {
                    fill = tintBackdrop(blurBackdrop(input.grab.xy / input.grab.w), _Fill.rgb, _Lower.rgb, rise, alpha);
                    alpha = 1;
                }
                float rimWidth = max(.75, _Look.w);
                float rim = smoothstep(-rimWidth - feather, -rimWidth + feather, sdf);
                float2 top = float2((uv.x - .26) / .132, (1 - uv.y) / .034);
                float2 bottom = float2((uv.x - .88) / .094, uv.y / .046);
                float reflection = (exp(-dot(top, top)) + .86 * exp(-dot(bottom, bottom))) * _Look.z;
                float frameAlpha = saturate((_Frame.a + reflection * (1 - _Frame.a)) * rim);
                float3 frame = lerp(_Frame.rgb, float3(.89, .94, 1), saturate(reflection));
                float3 panel = lerp(fill * alpha, frame, frameAlpha);
                float panelAlpha = lerp(alpha, 1, frameAlpha);
                return float4(panel * edge * input.color.rgb + float3(.015, .025, .045) * shadow,
                    panelAlpha * edge + shadow) * input.color.a;
            }
            ENDCG
        }
    }
}
