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
            #pragma fragment FragCopy

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float2 _FuguiBackdropPrefilterTexelSize;

            half4 FragCopy(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord.xy;
                float2 offset = _FuguiBackdropPrefilterTexelSize;

                half4 color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, _BlitMipLevel) * 0.25h;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + float2(offset.x, 0.0f), _BlitMipLevel) * 0.125h;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv - float2(offset.x, 0.0f), _BlitMipLevel) * 0.125h;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + float2(0.0f, offset.y), _BlitMipLevel) * 0.125h;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv - float2(0.0f, offset.y), _BlitMipLevel) * 0.125h;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + offset, _BlitMipLevel) * 0.0625h;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv - offset, _BlitMipLevel) * 0.0625h;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + float2(offset.x, -offset.y), _BlitMipLevel) * 0.0625h;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + float2(-offset.x, offset.y), _BlitMipLevel) * 0.0625h;
                return color;
            }
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

                half4 color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, _BlitMipLevel) * 0.196483h;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + stepUV * 1.411765f, _BlitMipLevel) * 0.296907h;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv - stepUV * 1.411765f, _BlitMipLevel) * 0.296907h;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + stepUV * 3.294118f, _BlitMipLevel) * 0.094470h;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv - stepUV * 3.294118f, _BlitMipLevel) * 0.094470h;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + stepUV * 5.176471f, _BlitMipLevel) * 0.010381h;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv - stepUV * 5.176471f, _BlitMipLevel) * 0.010381h;
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
            float2 _FuguiBackdropCompositeTexelSize;

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

            half4 SampleBackdrop(float2 uv)
            {
                float2 offset = _FuguiBackdropCompositeTexelSize;
                half4 color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, 0) * 0.25h;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + float2(offset.x, 0.0f), 0) * 0.125h;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv - float2(offset.x, 0.0f), 0) * 0.125h;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + float2(0.0f, offset.y), 0) * 0.125h;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv - float2(0.0f, offset.y), 0) * 0.125h;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + offset, 0) * 0.0625h;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv - offset, 0) * 0.0625h;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + float2(offset.x, -offset.y), 0) * 0.0625h;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + float2(-offset.x, offset.y), 0) * 0.0625h;
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
                half4 blurred = SampleBackdrop(i.uv);
                half4 overlay = i.color;
                half3 rgb = lerp(blurred.rgb, overlay.rgb, overlay.a);
                return half4(rgb, 1.0h);
            }
            ENDHLSL
        }
    }
}
