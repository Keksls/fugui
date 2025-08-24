using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
#if HAS_URP
using UnityEngine.Rendering.Universal;
#endif
#if HAS_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

namespace Fu
{
    public static partial class Fugui
    {
        private static FuRenderPipelineType _renderPipeline = FuRenderPipelineType.Unknown;

        /// <summary>
        /// Sets the current render pipeline type, if known.
        /// </summary>
        /// <returns> True if set, false if unknown or already set. </returns>
        public static FuRenderPipelineType GetCurrentPipelineType()
        {
            if(_renderPipeline != FuRenderPipelineType.Unknown)
                return _renderPipeline;

            var asset = GraphicsSettings.currentRenderPipeline
                       ?? QualitySettings.renderPipeline
                       ?? GraphicsSettings.defaultRenderPipeline;

            if (asset == null)
                return FuRenderPipelineType.BuiltIn;

            // Try strong-typed checks when packages are present
#if HAS_URP
            if (asset is UniversalRenderPipelineAsset)
                return FuRenderPipelineType.URP;
#endif
#if HAS_HDRP
        if (asset is HDRenderPipelineAsset) 
            return FuRenderPipelineType.HDRP;
#endif

            // Fallback without package refs (e.g., custom SRP or missing usings)
            var full = asset.GetType().FullName ?? asset.GetType().Name;
            if (!string.IsNullOrEmpty(full))
            {
                if (full.Contains("UnityEngine.Rendering.Universal") || full.Contains("UniversalRenderPipelineAsset"))
                    return FuRenderPipelineType.URP;
                if (full.Contains("UnityEngine.Rendering.HighDefinition") || full.Contains("HDRenderPipelineAsset"))
                    return FuRenderPipelineType.HDRP;
            }

            return FuRenderPipelineType.CustomSRP;
        }

        /// <summary>
        /// Returns SRP MSAA sample count if available on the active asset, else a sensible default.
        /// Works with URP/HDRP and falls back via reflection if needed.
        /// </summary>
        public static int GetSrpMsaaSampleCount(int @default = 4)
        {
            if(!IsCurrentRenderPipelineSupported())
                return @default;

            var asset = GraphicsSettings.currentRenderPipeline
                       ?? QualitySettings.renderPipeline
                       ?? GraphicsSettings.defaultRenderPipeline;

            if (asset == null) return @default;

            // Strong-typed path if packages are present
#if HAS_URP
            if (asset is UniversalRenderPipelineAsset urp)
                return urp.msaaSampleCount;
#endif
#if HAS_HDRP
        if (asset is HDRenderPipelineAsset hdrp)
            return hdrp.msaaSampleCount;
#endif

            // Fallback by reflection (property exists on both URP/HDRP assets)
            try
            {
                var prop = asset.GetType().GetProperty("msaaSampleCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop != null && prop.PropertyType == typeof(int))
                {
                    object value = prop.GetValue(asset, null);
                    if (value is int i) return i;
                }
            }
            catch { /* ignore and use default */ }

            return @default;
        }

        /// <summary>
        /// Returns true if the current Render Pipeline is marked as Supported (URP or HDRP).
        /// </summary>
        public static bool IsCurrentRenderPipelineSupported()
        {
            FuRenderPipelineType current = GetCurrentPipelineType();
            return (FuRenderPipelineType.Supported & current) == current;
        }
    }
}