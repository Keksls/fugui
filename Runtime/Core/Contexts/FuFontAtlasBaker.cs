using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Builds Fugui font atlas textures from a FontConfig.
    /// </summary>
    public static class FuFontAtlasBaker
    {
        /// <summary>
        /// Builds a font atlas texture for the given scale.
        /// </summary>
        public static bool TryBuildTexture(FontConfig fontConfig, float fontScale, string streamingAssetsPath, out Texture2D texture, out string error)
        {
            texture = null;
            error = null;

            if (fontConfig == null)
            {
                error = "FontConfig is null.";
                return false;
            }

            IntPtr previousContext = ImGuiNative.igGetCurrentContext();
            IntPtr context = IntPtr.Zero;
            List<byte[]> loadedFontBuffers = new List<byte[]>();

            try
            {
                context = ImGui.CreateContext();
                ImGuiNative.igSetCurrentContext(context);

                ImGuiIOPtr io = ImGui.GetIO();
                FuFontLoader.LoadFonts(io, fontConfig, Mathf.Max(0.0001f, fontScale), streamingAssetsPath, null, out _, loadedFontBuffers);

                if (!io.Fonts.Build())
                {
                    error = $"ImGui failed to build the font atlas for scale {fontScale:0.###}.";
                    return false;
                }

                texture = FuFontAtlasCache.CreateTextureFromAtlas(io.Fonts, $"Fugui Font Atlas {fontScale:0.###}");
                if (texture == null)
                {
                    error = $"Unable to create the font atlas texture for scale {fontScale:0.###}.";
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                Debug.LogException(exception);
                return false;
            }
            finally
            {
                if (context != IntPtr.Zero)
                {
                    ImGuiNative.igSetCurrentContext(context);
                    ImGui.DestroyContext(context);
                }

                if (previousContext != IntPtr.Zero)
                {
                    ImGuiNative.igSetCurrentContext(previousContext);
                }
            }
        }
    }
}
