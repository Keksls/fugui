Shader "Fugui/URP_Mesh"
{
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "PreviewType"="Plane" }
        LOD 100
        Cull Off
        ZTest Always
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "FUGUI URP"
            Tags { "LightMode"="UniversalForward" } // harmless for overlay, keeps URP happy

            HLSLPROGRAM
            #pragma vertex ImGuiPassVertex
            #pragma fragment ImGuiPassFrag
            #pragma multi_compile_instancing
            #pragma exclude_renderers gles // optional if you target modern
            // #pragma target 2.0 // keep low if you need wide compat

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #ifndef UNITY_COLORSPACE_GAMMA
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #endif
            #include "./Common.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Texture_ST; // if you need ST; keep an empty cbuffer to enable SRP Batcher
            CBUFFER_END

            TEXTURE2D_X(_Texture);
            SAMPLER(sampler_Texture);

            half4 unpack_color(uint c)
            {
                half4 color = half4((c)&0xff, (c>>8)&0xff, (c>>16)&0xff, (c>>24)&0xff) / 255.0h;
                #ifndef UNITY_COLORSPACE_GAMMA
                    color.rgb = FastSRGBToLinear(color.rgb);
                #endif
                return color;
            }

            Varyings ImGuiPassVertex(ImVert input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                // One mul instead of two:
                o.vertex = TransformObjectToHClip(float3(input.vertex, 0.0));
                // Robust UV flip:
                #if UNITY_UV_STARTS_AT_TOP
                    o.uv = float2(input.uv.x, 1.0 - input.uv.y);
                #else
                    o.uv = input.uv;
                #endif
                o.color = unpack_color(input.color);
                return o;
            }

            half4 ImGuiPassFrag(Varyings i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D_X(_Texture, sampler_Texture, i.uv);
                return i.color * tex;
            }
            ENDHLSL
        }
    }
}