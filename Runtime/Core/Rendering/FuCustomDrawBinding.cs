#if FU_CUSTOM_MATERIALS_ENABLED
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Backend resource referenced by a custom negative ImGui texture identifier.
    /// </summary>
    internal readonly struct FuCustomDrawBinding
    {
        internal FuDrawMaterial DrawMaterial { get; }
        internal Texture Texture { get; }

        /// <summary>
        /// Creates an immutable custom draw binding.
        /// </summary>
        /// <param name="drawMaterial">Custom material configuration.</param>
        /// <param name="texture">Texture sampled by the custom command.</param>
        internal FuCustomDrawBinding(FuDrawMaterial drawMaterial, Texture texture)
        {
            // The binding only references resources; their lifetime remains controlled by the caller.
            DrawMaterial = drawMaterial;
            Texture = texture;
        }
    }
}
#endif
