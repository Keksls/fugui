Shader "Fugui/URP_DebugPositionOnly"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }

        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float2 _FramebufferSize;
            CBUFFER_END

            struct Attributes
            {
                float2 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };


            Varyings Vert(Attributes input)
            {
                Varyings o;

                float2 ndc;
                ndc.x = (input.positionOS.x / _FramebufferSize.x) * 2.0 - 1.0;
                ndc.y = 1.0 - (input.positionOS.y / _FramebufferSize.y) * 2.0;
                o.positionCS = float4(ndc, 0.0, 1.0);

                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                return half4(1, 1, 1, 1);
            }
            ENDHLSL
        }
    }
}