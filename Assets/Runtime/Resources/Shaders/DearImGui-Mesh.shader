﻿Shader "DearImGui/Mesh"
{
    //// shader for Universal render pipeline
    //SubShader
    //{
    //    Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "PreviewType" = "Plane" }
    //    LOD 100

    //    Lighting Off
    //    Cull Off ZWrite On ZTest Always
    //    Blend SrcAlpha OneMinusSrcAlpha

    //    Pass
    //    {
    //        PackageRequirements {
    //            "com.unity.render-pipelines.universal"
    //        }

    //        Name "DEARIMGUI URP"

    //        HLSLPROGRAM
    //        #pragma vertex ImGuiPassVertex
    //        #pragma fragment ImGuiPassFrag
    //        #include "./PassesUniversal.hlsl"
    //        ENDHLSL
    //    }
    //}

    // shader for builtin rendering
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        LOD 100

        Lighting Off
        Cull Off ZWrite On ZTest Always
        //Blend SrcAlpha OneMinusSrcAlpha
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha

        Pass
        {
            Name "DEARIMGUI BUILTIN"

            CGPROGRAM
            #pragma require 2darray
            #pragma vertex ImGuiPassVertex
            #pragma fragment ImGuiPassFrag
            #include "./PassesBuiltin.hlsl"
            ENDCG
        }
    }

    //// shader for HD render pipeline
    //SubShader
    //{
    //    Tags { "RenderType" = "Transparent" "RenderPipeline" = "HDRenderPipeline" "PreviewType" = "Plane" }
    //    LOD 100

    //    Lighting Off
    //    Cull Off ZWrite On ZTest Always
    //    Blend SrcAlpha OneMinusSrcAlpha

    //    Pass
    //    {
    //        PackageRequirements {
    //            "com.unity.render-pipelines.high-definition"
    //        }

    //        Name "DEARIMGUI HDRP"

    //        HLSLPROGRAM
    //        #pragma vertex ImGuiPassVertex
    //        #pragma fragment ImGuiPassFrag
    //        #include "./PassesHD.hlsl"
    //        ENDHLSL
    //    }
    //}
}
