Shader "Fugui/URP_WorldMesh"
{
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "PreviewType"="Plane" }
        LOD 100
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "FUGUI WORLD DEPTH NONE"
            Tags { "LightMode"="UniversalForward" }
            ZTest Always
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex FuguiWorldPassVertex
            #pragma fragment FuguiWorldPassFrag
            #pragma multi_compile_instancing

            #define FUGUI_WORLD_PASS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #ifndef UNITY_COLORSPACE_GAMMA
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #endif
            #include "./WorldCommon.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "FUGUI WORLD DEPTH TEST"
            Tags { "LightMode"="UniversalForward" }
            ZTest LEqual
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex FuguiWorldPassVertex
            #pragma fragment FuguiWorldPassFrag
            #pragma multi_compile_instancing

            #define FUGUI_WORLD_PASS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #ifndef UNITY_COLORSPACE_GAMMA
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #endif
            #include "./WorldCommon.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "FUGUI WORLD DEPTH TEST WRITE"
            Tags { "LightMode"="UniversalForward" }
            ZTest LEqual
            ZWrite On

            HLSLPROGRAM
            #pragma vertex FuguiWorldPassVertex
            #pragma fragment FuguiWorldPassFrag
            #pragma multi_compile_instancing

            #define FUGUI_WORLD_PASS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #ifndef UNITY_COLORSPACE_GAMMA
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #endif
            #include "./WorldCommon.hlsl"
            ENDHLSL
        }
    }
}
