#if FU_CUSTOM_MATERIALS_ENABLED
using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Tracks custom material scopes encoded as internal ImGui callback commands.
    /// </summary>
    internal unsafe struct FuCustomDrawMaterialState
    {
        internal static readonly IntPtr PushCallback = new IntPtr(-67002);
        internal static readonly IntPtr PopCallback = new IntPtr(-67003);

        private List<IntPtr> _nestedBindingStack;
        private IntPtr _activeBindingId;
        private IntPtr _previousBindingId;
        private int _depth;

        internal IntPtr ActiveBindingId => _activeBindingId;

        /// <summary>
        /// Consumes a Fugui custom material callback and updates the active binding stack.
        /// </summary>
        /// <param name="command">ImGui draw command to inspect.</param>
        /// <returns>True when the command is an internal custom material marker.</returns>
        internal bool TryHandleCommand(ImDrawCmd command)
        {
            if (command.UserCallback == PushCallback)
            {
                // Keep the common non-nested scope allocation-free.
                if (_depth == 0)
                {
                    _previousBindingId = _activeBindingId;
                }
                else
                {
                    _nestedBindingStack ??= new List<IntPtr>(2);
                    _nestedBindingStack.Add(_activeBindingId);
                }

                _depth++;
                _activeBindingId = (IntPtr)command.UserCallbackData;
                return true;
            }

            if (command.UserCallback != PopCallback)
            {
                return false;
            }

            // Malformed external draw lists safely fall back to the default material.
            if (_depth <= 0)
            {
                _activeBindingId = IntPtr.Zero;
                return true;
            }

            _depth--;
            if (_depth == 0)
            {
                _activeBindingId = _previousBindingId;
                _previousBindingId = IntPtr.Zero;
                return true;
            }

            int previousIndex = _nestedBindingStack.Count - 1;
            _activeBindingId = _nestedBindingStack[previousIndex];
            _nestedBindingStack.RemoveAt(previousIndex);
            return true;
        }

        /// <summary>
        /// Resolves the overlay material represented by the active custom binding.
        /// </summary>
        /// <param name="textureManager">Texture manager that owns the custom binding.</param>
        /// <param name="fallbackMaterial">Default Fugui overlay material.</param>
        /// <param name="material">Resolved material.</param>
        /// <param name="pass">Resolved shader pass.</param>
        internal void ResolveOverlay(
            TextureManager textureManager,
            Material fallbackMaterial,
            out Material material,
            out int pass)
        {
            material = fallbackMaterial;
            pass = 0;
            if (!TryGetActiveDrawMaterial(textureManager, out FuDrawMaterial drawMaterial))
            {
                return;
            }

            // Invalid or caller-destroyed custom materials fall back to the normal Fugui renderer.
            if (drawMaterial.TryGetOverlayMaterial(out Material customMaterial, out int customPass))
            {
                material = customMaterial;
                pass = customPass;
            }
        }

        /// <summary>
        /// Resolves the world material represented by the active custom binding.
        /// </summary>
        /// <param name="textureManager">Texture manager that owns the custom binding.</param>
        /// <param name="depthMode">Depth behavior requested by the world surface.</param>
        /// <param name="fallbackMaterial">Default Fugui world material.</param>
        /// <param name="fallbackPass">Default Fugui world shader pass.</param>
        /// <param name="material">Resolved material.</param>
        /// <param name="pass">Resolved shader pass.</param>
        internal void ResolveWorld(
            TextureManager textureManager,
            FuguiWorldDepthMode depthMode,
            Material fallbackMaterial,
            int fallbackPass,
            out Material material,
            out int pass)
        {
            material = fallbackMaterial;
            pass = fallbackPass;
            if (!TryGetActiveDrawMaterial(textureManager, out FuDrawMaterial drawMaterial))
            {
                return;
            }

            // Overlay-only bindings deliberately use the standard Fugui world material.
            if (drawMaterial.TryGetWorldMaterial(depthMode, out Material customMaterial, out int customPass))
            {
                material = customMaterial;
                pass = customPass;
            }
        }

        /// <summary>
        /// Resolves the material configuration stored by the active custom binding.
        /// </summary>
        /// <param name="textureManager">Texture manager that owns the custom binding.</param>
        /// <param name="drawMaterial">Resolved custom material configuration.</param>
        /// <returns>True when the active identifier references a valid custom binding.</returns>
        private bool TryGetActiveDrawMaterial(TextureManager textureManager, out FuDrawMaterial drawMaterial)
        {
            // Binding identifiers are context-local and must be resolved by their owning manager.
            if (_activeBindingId != IntPtr.Zero &&
                textureManager != null &&
                textureManager.TryGetCustomDrawBinding(_activeBindingId, out FuCustomDrawBinding binding) &&
                binding.DrawMaterial != null)
            {
                drawMaterial = binding.DrawMaterial;
                return true;
            }

            drawMaterial = null;
            return false;
        }
    }
}
#endif
