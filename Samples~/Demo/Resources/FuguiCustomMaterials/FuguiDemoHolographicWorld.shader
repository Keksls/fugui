Shader "Fugui/Demo/Holographic World"
{
    Properties
    {
        [MainTexture] _Texture("Texture", 2D) = "white" {}
        _ColorA("Deep Glass", Color) = (0.018, 0.06, 0.15, 1.00)
        _ColorB("Aurora", Color) = (0.06, 0.95, 0.88, 1.00)
        _EdgeColor("Iridescent Edge", Color) = (0.75, 0.22, 1.00, 1.00)
        _Glow("Glow", Range(0.0, 3.0)) = 1.35
        _Speed("Animation Speed", Range(0.0, 5.0)) = 0.85
        _GridDensity("Grid Density", Range(2.0, 32.0)) = 14.0
        _Interaction("Interaction", Range(0.0, 1.0)) = 0.45
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
        Blend SrcAlpha OneMinusSrcAlpha

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #ifndef UNITY_COLORSPACE_GAMMA
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #endif

        CBUFFER_START(UnityPerMaterial)
            float4 _Texture_ST;
            float4 _ColorA;
            float4 _ColorB;
            float4 _EdgeColor;
            float4 _ClipRect;
            float _TextureIsAlpha;
            float _Glow;
            float _Speed;
            float _GridDensity;
            float _Interaction;
        CBUFFER_END

        TEXTURE2D(_Texture);
        SAMPLER(sampler_Texture);

        struct HolographicWorldAttributes
        {
            float3 positionOS : POSITION;
            float2 uv : TEXCOORD0;
            uint color : TEXCOORD1;
            float2 clipPosition : TEXCOORD2;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct HolographicWorldVaryings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            half4 color : COLOR;
            float2 clipPosition : TEXCOORD1;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        /// Converts the packed ImGui vertex color into the active Unity color space.
        half4 UnpackHolographicWorldColor(uint packedColor)
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

        /// Transforms Fugui world vertices and forwards their draw-list clipping position.
        HolographicWorldVaryings HolographicWorldVertex(HolographicWorldAttributes input)
        {
            HolographicWorldVaryings output = (HolographicWorldVaryings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionCS = TransformObjectToHClip(input.positionOS);
            output.uv = float2(input.uv.x, 1.0 - input.uv.y);
            output.color = UnpackHolographicWorldColor(input.color);
            output.clipPosition = input.clipPosition;
            return output;
        }

        /// Draws the holographic glass effect while respecting Fugui world-space clipping.
        half4 HolographicWorldFragment(HolographicWorldVaryings input) : SV_Target
        {
            clip(input.clipPosition.x - _ClipRect.x);
            clip(input.clipPosition.y - _ClipRect.y);
            clip(_ClipRect.z - input.clipPosition.x);
            clip(_ClipRect.w - input.clipPosition.y);

            float2 uv = saturate(input.uv);
            float time = _Time.y * _Speed;
            half4 sampled = SAMPLE_TEXTURE2D(_Texture, sampler_Texture, input.uv);
            if (_TextureIsAlpha > 0.5h)
            {
                sampled = half4(1.0h, 1.0h, 1.0h, sampled.a);
            }

            float aurora = sin(uv.x * 9.0 - time * 1.8 + sin(uv.y * 7.0 + time) * 1.5);
            aurora = saturate(aurora * 0.33 + 0.54);

            float2 gridUv = uv * float2(_GridDensity, max(2.0, _GridDensity * 0.34));
            float2 gridDerivative = max(fwidth(gridUv), 0.001);
            float2 gridDistance = abs(frac(gridUv - 0.5) - 0.5) / gridDerivative;
            float grid = 1.0 - saturate(min(gridDistance.x, gridDistance.y));

            float sweepCoordinate = frac(uv.x * 0.72 + uv.y * 0.42 - time * 0.20);
            float sweep = pow(saturate(1.0 - abs(sweepCoordinate * 2.0 - 1.0)), 18.0);
            float2 centered = abs(uv - 0.5) * 2.0;
            float edge = smoothstep(0.70, 1.0, max(centered.x, centered.y));

            half3 color = lerp(_ColorA.rgb, _ColorB.rgb, aurora * 0.44 + uv.y * 0.16);
            color += _ColorB.rgb * grid * (0.10 + _Interaction * 0.10);
            color += _EdgeColor.rgb * sweep * (0.42 + _Glow * 0.30);
            color += lerp(_ColorB.rgb, _EdgeColor.rgb, uv.x) * edge * (0.16 + _Glow * 0.22);
            color *= sampled.rgb * input.color.rgb;

            half alpha = sampled.a * input.color.a * saturate(0.80 + edge * 0.10 + _Interaction * 0.06);
            return half4(color, alpha);
        }
        ENDHLSL

        Pass
        {
            Name "FUGUI WORLD DEPTH NONE"
            Tags { "LightMode" = "UniversalForward" }
            ZTest Always
            ZWrite Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex HolographicWorldVertex
            #pragma fragment HolographicWorldFragment
            #pragma multi_compile_instancing
            ENDHLSL
        }

        Pass
        {
            Name "FUGUI WORLD DEPTH TEST"
            Tags { "LightMode" = "UniversalForward" }
            ZTest LEqual
            ZWrite Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex HolographicWorldVertex
            #pragma fragment HolographicWorldFragment
            #pragma multi_compile_instancing
            ENDHLSL
        }

        Pass
        {
            Name "FUGUI WORLD DEPTH TEST WRITE"
            Tags { "LightMode" = "UniversalForward" }
            ZTest LEqual
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex HolographicWorldVertex
            #pragma fragment HolographicWorldFragment
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }

    Fallback Off
}
