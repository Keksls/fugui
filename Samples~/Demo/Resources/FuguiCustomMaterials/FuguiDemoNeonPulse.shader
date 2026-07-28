Shader "Fugui/Demo/Neon Pulse"
{
    Properties
    {
        [MainTexture] _Texture("Texture", 2D) = "white" {}
        _ColorA("Electric Cyan", Color) = (0.00, 0.95, 1.00, 1.00)
        _ColorB("Plasma Magenta", Color) = (1.00, 0.04, 0.72, 1.00)
        _Glow("Glow", Range(0.0, 3.0)) = 1.35
        _Speed("Animation Speed", Range(0.0, 5.0)) = 1.20
        _ScanDensity("Scanline Density", Range(8.0, 160.0)) = 72.0
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
            Name "FUGUI DEMO NEON"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex NeonVertex
            #pragma fragment NeonFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #ifndef UNITY_COLORSPACE_GAMMA
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #endif

            CBUFFER_START(UnityPerMaterial)
                float4 _Texture_ST;
                float4 _ColorA;
                float4 _ColorB;
                float _TextureIsAlpha;
                float _Glow;
                float _Speed;
                float _ScanDensity;
                float _Interaction;
            CBUFFER_END

            TEXTURE2D(_Texture);
            SAMPLER(sampler_Texture);

            struct NeonAttributes
            {
                float2 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                uint color : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct NeonVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            /// Converts the packed ImGui vertex color into the active Unity color space.
            half4 UnpackNeonColor(uint packedColor)
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
            NeonVaryings NeonVertex(NeonAttributes input)
            {
                NeonVaryings output = (NeonVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(float3(input.positionOS, 0.0));
                output.uv = float2(input.uv.x, 1.0 - input.uv.y);
                output.color = UnpackNeonColor(input.color);
                return output;
            }

            /// Draws an animated cyan-magenta plasma field with scanlines and an emissive rim.
            half4 NeonFragment(NeonVaryings input) : SV_Target
            {
                float2 uv = saturate(input.uv);
                float time = _Time.y * _Speed;
                half4 sampled = SAMPLE_TEXTURE2D(_Texture, sampler_Texture, input.uv);
                if (_TextureIsAlpha > 0.5h)
                {
                    sampled = half4(1.0h, 1.0h, 1.0h, sampled.a);
                }

                float plasma = sin((uv.x * 1.35 + uv.y * 0.72) * 8.0 + time * 2.4);
                plasma += sin((uv.x * 0.45 - uv.y * 1.60) * 11.0 - time * 1.7) * 0.55;
                plasma = saturate(plasma * 0.28 + 0.52);

                float movingCoordinate = frac(uv.x * 0.78 + uv.y * 0.32 - time * 0.13);
                float energyStreak = pow(saturate(1.0 - abs(movingCoordinate * 2.0 - 1.0)), 12.0);
                float scanline = sin(uv.y * _ScanDensity + time * 7.0) * 0.5 + 0.5;
                scanline = lerp(0.82, 1.08, scanline);

                float2 centered = abs(uv - 0.5) * 2.0;
                float edge = smoothstep(0.68, 1.0, max(centered.x, centered.y));
                half3 plasmaColor = lerp(_ColorA.rgb, _ColorB.rgb, plasma);
                plasmaColor *= scanline * (0.66 + _Interaction * 0.22);
                plasmaColor += (_ColorA.rgb + _ColorB.rgb) * edge * (0.18 + _Glow * 0.22);
                plasmaColor += lerp(_ColorA.rgb, _ColorB.rgb, uv.y) * energyStreak * (0.45 + _Glow * 0.45);

                half alpha = sampled.a * input.color.a * saturate(0.88 + _Interaction * 0.10);
                half3 color = plasmaColor * sampled.rgb * input.color.rgb;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
