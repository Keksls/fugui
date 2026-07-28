#if FU_CUSTOM_MATERIALS_ENABLED
using System;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Immutable material configuration used by custom Fugui draw-list commands.
    /// Unity materials remain owned by the caller.
    /// </summary>
    public sealed class FuDrawMaterial
    {
        public Material OverlayMaterial { get; }
        public int OverlayPass { get; }
        public Material WorldMaterial { get; }
        public int WorldPassNone { get; }
        public int WorldPassDepthTest { get; }
        public int WorldPassDepthWrite { get; }

        /// <summary>
        /// Creates a custom draw material for overlay rendering and optionally world-space rendering.
        /// </summary>
        /// <param name="overlayMaterial">Material used by Fugui overlay renderers.</param>
        /// <param name="overlayPass">Shader pass used for overlay rendering.</param>
        /// <param name="worldMaterial">Optional material compatible with Fugui world-space vertices and clipping.</param>
        /// <param name="worldPassNone">World material pass used without depth testing.</param>
        /// <param name="worldPassDepthTest">World material pass used with depth testing.</param>
        /// <param name="worldPassDepthWrite">World material pass used with depth testing and writing.</param>
        public FuDrawMaterial(
            Material overlayMaterial,
            int overlayPass = 0,
            Material worldMaterial = null,
            int worldPassNone = 0,
            int worldPassDepthTest = 1,
            int worldPassDepthWrite = 2)
        {
            if (overlayMaterial == null)
            {
                throw new ArgumentNullException(nameof(overlayMaterial));
            }

            ValidatePass(overlayMaterial, overlayPass, nameof(overlayPass));
            if (worldMaterial != null)
            {
                ValidatePass(worldMaterial, worldPassNone, nameof(worldPassNone));
                ValidatePass(worldMaterial, worldPassDepthTest, nameof(worldPassDepthTest));
                ValidatePass(worldMaterial, worldPassDepthWrite, nameof(worldPassDepthWrite));
            }

            // Keep render configuration immutable so cached draw commands always resolve identically.
            OverlayMaterial = overlayMaterial;
            OverlayPass = overlayPass;
            WorldMaterial = worldMaterial;
            WorldPassNone = worldPassNone;
            WorldPassDepthTest = worldPassDepthTest;
            WorldPassDepthWrite = worldPassDepthWrite;
        }

        /// <summary>
        /// Resolves the overlay material and shader pass when the caller-owned material is still valid.
        /// </summary>
        /// <param name="material">Resolved overlay material.</param>
        /// <param name="pass">Resolved overlay shader pass.</param>
        /// <returns>True when the overlay material can be rendered.</returns>
        internal bool TryGetOverlayMaterial(out Material material, out int pass)
        {
            // Revalidate caller-owned resources because Unity materials may be destroyed after registration.
            material = OverlayMaterial;
            pass = OverlayPass;
            return material != null && pass >= 0 && pass < material.passCount;
        }

        /// <summary>
        /// Resolves the world-space material and shader pass for a Fugui depth mode.
        /// </summary>
        /// <param name="depthMode">World-space depth behavior requested by the surface.</param>
        /// <param name="material">Resolved world-space material.</param>
        /// <param name="pass">Resolved world-space shader pass.</param>
        /// <returns>True when a compatible world-space material is configured and valid.</returns>
        internal bool TryGetWorldMaterial(FuguiWorldDepthMode depthMode, out Material material, out int pass)
        {
            // Keep depth behavior explicit so a custom shader cannot accidentally use the wrong pass.
            material = WorldMaterial;
            switch (depthMode)
            {
                case FuguiWorldDepthMode.None:
                    pass = WorldPassNone;
                    break;
                case FuguiWorldDepthMode.TestAndWrite:
                    pass = WorldPassDepthWrite;
                    break;
                case FuguiWorldDepthMode.Test:
                default:
                    pass = WorldPassDepthTest;
                    break;
            }

            return material != null && pass >= 0 && pass < material.passCount;
        }

        /// <summary>
        /// Validates that a configured shader pass exists on its material.
        /// </summary>
        /// <param name="material">Material that owns the pass.</param>
        /// <param name="pass">Shader pass index to validate.</param>
        /// <param name="parameterName">Constructor parameter reported when validation fails.</param>
        private static void ValidatePass(Material material, int pass, string parameterName)
        {
            // Reject invalid configurations once rather than logging one error per draw command.
            if (pass < 0 || pass >= material.passCount)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    pass,
                    $"Material '{material.name}' exposes {material.passCount} shader pass(es).");
            }
        }
    }
}
#endif
