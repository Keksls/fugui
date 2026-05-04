Shader "Fugui/BackdropBlur"
{
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "PreviewType"="Plane" }
        LOD 100
        Cull Off
        ZTest Always
        ZWrite Off

        Pass
        {
            Name "Copy"
            Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragBilinear

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Blur"
            Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragBlur

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float2 _FuguiBackdropBlurDirection;
            float _FuguiBackdropBlurRadius;

            half4 FragBlur(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 stepUV = _FuguiBackdropBlurDirection * _FuguiBackdropBlurRadius * _BlitTexture_TexelSize.xy;
                float2 uv = input.texcoord.xy;

                half4 color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, _BlitMipLevel) * 0.227027h;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + stepUV * 1.384615f, _BlitMipLevel) * 0.316216h;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv - stepUV * 1.384615f, _BlitMipLevel) * 0.316216h;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + stepUV * 3.230769f, _BlitMipLevel) * 0.070270h;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv - stepUV * 3.230769f, _BlitMipLevel) * 0.070270h;
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Composite"
            Blend Off

            HLSLPROGRAM
            #pragma vertex BackdropVertex
            #pragma fragment BackdropFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #ifndef UNITY_COLORSPACE_GAMMA
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #endif
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"
            #include "./Common.hlsl"

            TEXTURE2D_X(_BlitTexture);
            float2 _FuguiBackdropScreenSize;
            float2 _FuguiBackdropRenderOffset;

            struct BackdropVaryings
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            half4 unpack_color(uint c)
            {
                half4 color = half4((c)&0xff, (c>>8)&0xff, (c>>16)&0xff, (c>>24)&0xff) / 255.0h;
                #ifndef UNITY_COLORSPACE_GAMMA
                    color.rgb = FastSRGBToLinear(color.rgb);
                #endif
                return color;
            }

            BackdropVaryings BackdropVertex(ImVert input)
            {
                BackdropVaryings o = (BackdropVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = TransformObjectToHClip(float3(input.vertex, 0.0));
                float2 screenPos = input.vertex + _FuguiBackdropRenderOffset;
                o.uv = float2(
                    screenPos.x / max(1.0f, _FuguiBackdropScreenSize.x),
                    1.0f - screenPos.y / max(1.0f, _FuguiBackdropScreenSize.y));
                o.color = unpack_color(input.color);
                return o;
            }

            half4 BackdropFragment(BackdropVaryings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half4 blurred = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, i.uv, 0);
                half4 overlay = i.color;
                half3 rgb = lerp(blurred.rgb, overlay.rgb, overlay.a);
                return half4(rgb, 1.0h);
            }
            ENDHLSL
        }
    }
}
