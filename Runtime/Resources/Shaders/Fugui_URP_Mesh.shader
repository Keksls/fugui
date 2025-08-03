Shader "Fugui/URP_Mesh"
{
    // shader for Universal render pipeline
    SubShader
    {
       Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "PreviewType" = "Plane" }
       LOD 100

       Lighting Off
       Cull Off ZWrite On ZTest Always
       Blend SrcAlpha OneMinusSrcAlpha

       Pass
       {
           PackageRequirements {
               "com.unity.render-pipelines.universal"
           }

           Name "FUGUI URP"

           HLSLPROGRAM
           #pragma vertex ImGuiPassVertex
           #pragma fragment ImGuiPassFrag
           #include "./PassesUniversal.hlsl"
           ENDHLSL
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