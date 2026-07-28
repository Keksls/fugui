Shader "Fugui/Demo/Holographic Glass"
{
    Properties
    {
        [MainTexture] _Texture("Texture", 2D) = "white" {}
        _ColorA("Deep Glass", Color) = (0.025, 0.08, 0.18, 1.00)
        _ColorB("Aurora", Color) = (0.16, 0.92, 0.84, 1.00)
        _EdgeColor("Iridescent Edge", Color) = (0.70, 0.25, 1.00, 1.00)
        _Glow("Glow", Range(0.0, 3.0)) = 1.10
        _Speed("Animation Speed", Range(0.0, 5.0)) = 0.75
        _GridDensity("Grid Density", Range(2.0, 32.0)) = 12.0
        _Interaction("Interaction", Range(0.0, 1.0)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "PreviewType" = "Plane"
        }

        Cull Off
        ZTest Always
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "FUGUI DEMO HOLOGRAPHIC GLASS"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex GlassVertex
            #pragma fragment GlassFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #ifndef UNITY_COLORSPACE_GAMMA
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #endif

            CBUFFER_START(UnityPerMaterial)
                float4 _Texture_ST;
                float4 _ColorA;
                float4 _ColorB;
                float4 _EdgeColor;
                float _TextureIsAlpha;
                float _Glow;
                float _Speed;
                float _GridDensity;
                float _Interaction;
            CBUFFER_END

            TEXTURE2D(_Texture);
            SAMPLER(sampler_Texture);

            struct GlassAttributes
            {
                float2 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                uint color : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct GlassVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            /// Converts the packed ImGui vertex color into the active Unity color space.
            half4 UnpackGlassColor(uint packedColor)
            {
                half4 color = half4(
                    packedColor & 0xff,
                    (packedColor >> 8) & 0xff,
                    (packedColor >> 16) & 0xff,
                    (packedColor >> 24) & 0xff) / 255.0h;

                #ifndef UNITY_COLORSPACE_GAMMA
                    color.rgb = FastSRGBToLinear(color.rgb);
                #endif
                return color;
            }

            /// Transforms Fugui overlay vertices and preserves their normalized image UVs.
            GlassVaryings GlassVertex(GlassAttributes input)
            {
                GlassVaryings output = (GlassVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(float3(input.positionOS, 0.0));
                output.uv = float2(input.uv.x, 1.0 - input.uv.y);
                output.color = UnpackGlassColor(input.color);
                return output;
            }

            /// Draws an animated aurora, a technical grid and a travelling glass highlight.
            half4 GlassFragment(GlassVaryings input) : SV_Target
            {
                float2 uv = saturate(input.uv);
                float time = _Time.y * _Speed;
                half4 sampled = SAMPLE_TEXTURE2D(_Texture, sampler_Texture, input.uv);
                if (_TextureIsAlpha > 0.5h)
                {
                    sampled = half4(1.0h, 1.0h, 1.0h, sampled.a);
                }

                float aurora = sin(uv.x * 8.0 - time * 1.6 + sin(uv.y * 6.0 + time) * 1.4);
                aurora = saturate(aurora * 0.34 + 0.52);

                float2 gridUv = uv * float2(_GridDensity, max(2.0, _GridDensity * 0.38));
                float2 gridDerivative = max(fwidth(gridUv), 0.001);
                float2 gridDistance = abs(frac(gridUv - 0.5) - 0.5) / gridDerivative;
                float grid = 1.0 - saturate(min(gridDistance.x, gridDistance.y));

                float sweepCoordinate = frac(uv.x * 0.72 + uv.y * 0.45 - time * 0.18);
                float sweep = pow(saturate(1.0 - abs(sweepCoordinate * 2.0 - 1.0)), 18.0);
                float2 centered = abs(uv - 0.5) * 2.0;
                float edge = smoothstep(0.72, 1.0, max(centered.x, centered.y));

                half3 color = lerp(_ColorA.rgb, _ColorB.rgb, aurora * 0.38 + uv.y * 0.18);
                color += _ColorB.rgb * grid * (0.08 + _Interaction * 0.10);
                color += _EdgeColor.rgb * sweep * (0.34 + _Glow * 0.28);
                color += lerp(_ColorB.rgb, _EdgeColor.rgb, uv.x) * edge * (0.12 + _Glow * 0.20);
                color *= sampled.rgb * input.color.rgb;

                half alpha = sampled.a * input.color.a * saturate(0.78 + edge * 0.10 + _Interaction * 0.08);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
