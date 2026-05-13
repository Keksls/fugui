#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR && !FUMOBILE
#define FUMOBILE
#endif
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Shared ImGui font loader used by runtime contexts and editor atlas baking.
    /// </summary>
    internal static unsafe class FuFontLoader
    {
        /// <summary>
        /// Loads all fonts from a Fugui font config into an ImGui font atlas.
        /// </summary>
        internal static void LoadFonts(
            ImGuiIOPtr io,
            FontConfig fontConf,
            float fontScale,
            string streamingAssetsPath,
            Dictionary<FontKey, FontSet> fonts,
            out FontSet defaultFont,
            List<byte[]> loadedFontBuffers)
        {
            defaultFont = null;

            if (fontConf == null)
            {
                Debug.LogError("[FontLoader] FontConfig is null.");
                return;
            }

            string fontPath = FuFontAtlasCache.CombineStreamingPath(streamingAssetsPath, fontConf.FontsFolder);
            io.Fonts.Clear();
            io.NativePtr->FontDefault = default;
            fonts?.Clear();
            loadedFontBuffers?.Clear();

            if (fontConf.Fonts == null)
            {
                return;
            }

            foreach (FontSizeConfig font in fontConf.Fonts)
            {
                if (font == null)
                {
                    continue;
                }

                string fontName = ResolveFontName(fontConf, font);
                FontSet fontSet = null;
                if (fonts != null)
                {
                    FontKey fontKey = new FontKey(fontName, font.Size);
                    if (fonts.ContainsKey(fontKey))
                    {
                        Debug.LogWarning($"[FontLoader] Duplicate font config for {fontKey}. The last entry will be used.");
                    }

                    fontSet = new FontSet(fontName, font.Size);
                    fonts[fontKey] = fontSet;
                }

                if (ProcessSubFont(io, fontPath, fontScale, font, GetAvailableSubFonts("Regular", fontPath, font.SubFonts_Regular), loadedFontBuffers, out ImFontPtr regular))
                {
                    if (fontSet != null)
                    {
                        fontSet.Regular = regular;
                    }
                }

                if (ProcessSubFont(io, fontPath, fontScale, font, GetAvailableSubFonts("Bold", fontPath, font.SubFonts_Bold), loadedFontBuffers, out ImFontPtr bold))
                {
                    if (fontSet != null)
                    {
                        fontSet.Bold = bold;
                    }
                }

                if (ProcessSubFont(io, fontPath, fontScale, font, GetAvailableSubFonts("Italic", fontPath, font.SubFonts_Italic), loadedFontBuffers, out ImFontPtr italic))
                {
                    if (fontSet != null)
                    {
                        fontSet.Italic = italic;
                    }
                }

                if (fontSet != null &&
                    font.Size == fontConf.DefaultSize &&
                    string.Equals(fontSet.Name, fontConf.GetDefaultFontName(), StringComparison.OrdinalIgnoreCase))
                {
                    defaultFont = fontSet;
                }
            }

            if (defaultFont == null && fonts != null)
            {
                defaultFont = FindFallbackFont(fonts, fontConf);
            }
        }

        private static string ResolveFontName(FontConfig fontConf, FontSizeConfig font)
        {
            return fontConf != null
                ? fontConf.ResolveFontName(font?.Name)
                : FontKey.NormalizeName(font?.Name);
        }

        private static FontSet FindFallbackFont(Dictionary<FontKey, FontSet> fonts, FontConfig fontConf)
        {
            if (fonts == null || fonts.Count == 0)
            {
                return null;
            }

            FontKey defaultKey = new FontKey(fontConf.GetDefaultFontName(), fontConf.DefaultSize);
            if (fonts.TryGetValue(defaultKey, out FontSet defaultFont))
            {
                return defaultFont;
            }

            foreach (FontSet fontSet in fonts.Values)
            {
                if (fontSet != null && fontSet.Size == fontConf.DefaultSize)
                {
                    return fontSet;
                }
            }

            foreach (FontSet fontSet in fonts.Values)
            {
                if (fontSet != null)
                {
                    return fontSet;
                }
            }

            return null;
        }

        private static SubFontConfig[] GetAvailableSubFonts(string label, string fontPath, SubFontConfig[] subFonts)
        {
            if (subFonts == null || subFonts.Length == 0)
            {
                return Array.Empty<SubFontConfig>();
            }

#if FUMOBILE
            return subFonts;
#else
            List<SubFontConfig> availableFonts = new List<SubFontConfig>();

            foreach (SubFontConfig subFont in subFonts)
            {
                if (subFont == null)
                {
                    continue;
                }

                string fullPath = FuFontAtlasCache.CombineStreamingPath(fontPath, subFont.FileName);
                if (File.Exists(fullPath))
                {
                    availableFonts.Add(subFont);
                }
                else
                {
                    Debug.LogWarning($"[FontLoader] {label} font file not found: {fullPath}");
                }
            }

            return availableFonts.ToArray();
#endif
        }

        private static bool ProcessSubFont(
            ImGuiIOPtr io,
            string fontPath,
            float fontScale,
            FontSizeConfig font,
            SubFontConfig[] subFonts,
            List<byte[]> loadedFontBuffers,
            out ImFontPtr fontPtr)
        {
            fontPtr = default;

            if (subFonts == null || subFonts.Length == 0)
            {
                return false;
            }

            int subFontIndex = 0;

            foreach (SubFontConfig subFont in subFonts)
            {
                if (subFont == null)
                {
                    continue;
                }

                bool useDefaultGlyphRange = UsesDefaultGlyphRange(subFont);
                if (!useDefaultGlyphRange)
                {
                    BuildGlyphRanges(subFont);
                }

                ImFontConfig* conf = ImGuiNative.ImFontConfig_ImFontConfig();
                subFont.FontConfigPtr = new ImFontConfigPtr(conf);
                subFont.FontConfigPtr.MergeMode = subFontIndex > 0;
                subFont.FontConfigPtr.GlyphOffset = subFont.GlyphOffset * fontScale;
                subFont.FontConfigPtr.FontDataOwnedByAtlas = false;

                string fontFilePath = FuFontAtlasCache.CombineStreamingPath(fontPath, subFont.FileName);
                float sizePixels = (font.Size * fontScale) + (subFont.SizeOffset * fontScale);
                ImFontPtr tmpFontPtr = LoadFont(io, fontFilePath, sizePixels, subFont, useDefaultGlyphRange, loadedFontBuffers);

                if ((IntPtr)tmpFontPtr.NativePtr != IntPtr.Zero && subFontIndex == 0)
                {
                    fontPtr = tmpFontPtr;
                }

                subFontIndex++;
            }

            return (IntPtr)fontPtr.NativePtr != IntPtr.Zero;
        }

        private static ImFontPtr LoadFont(
            ImGuiIOPtr io,
            string fontFilePath,
            float sizePixels,
            SubFontConfig subFont,
            bool useDefaultGlyphRange,
            List<byte[]> loadedFontBuffers)
        {
#if FUMOBILE
            return LoadFontFromMemory(io, fontFilePath, sizePixels, subFont, useDefaultGlyphRange, loadedFontBuffers, "Unable to load font bytes");
#else
            ImFontPtr tmpFontPtr = useDefaultGlyphRange
                ? io.Fonts.AddFontFromFileTTF(fontFilePath, sizePixels, subFont.FontConfigPtr)
                : io.Fonts.AddFontFromFileTTF(fontFilePath, sizePixels, subFont.FontConfigPtr, subFont.GlyphRangePtr);

            if ((IntPtr)tmpFontPtr.NativePtr != IntPtr.Zero)
            {
                return tmpFontPtr;
            }

            Debug.LogWarning($"[FontLoader] Failed to load font from file -> {fontFilePath}. Trying memory fallback.");
            return LoadFontFromMemory(io, fontFilePath, sizePixels, subFont, useDefaultGlyphRange, loadedFontBuffers, "Memory fallback failed to read bytes");
#endif
        }

        private static ImFontPtr LoadFontFromMemory(
            ImGuiIOPtr io,
            string fontFilePath,
            float sizePixels,
            SubFontConfig subFont,
            bool useDefaultGlyphRange,
            List<byte[]> loadedFontBuffers,
            string errorPrefix)
        {
            byte[] fontData = Fugui.ReadAllBytes(fontFilePath);
            if (fontData == null || fontData.Length == 0)
            {
                Debug.LogError($"[FontLoader] {errorPrefix} for -> {fontFilePath}");
                return default;
            }

            loadedFontBuffers?.Add(fontData);

            fixed (byte* fontPtrRaw = fontData)
            {
                ImFontPtr tmpFontPtr = useDefaultGlyphRange
                    ? io.Fonts.AddFontFromMemoryTTF((IntPtr)fontPtrRaw, fontData.Length, sizePixels, subFont.FontConfigPtr)
                    : io.Fonts.AddFontFromMemoryTTF((IntPtr)fontPtrRaw, fontData.Length, sizePixels, subFont.FontConfigPtr, subFont.GlyphRangePtr);

#if FUMOBILE
                Debug.Log($"[FontLoader] Trying to load font from memory -> {fontFilePath} : {((IntPtr)tmpFontPtr.NativePtr != IntPtr.Zero ? "Success" : "Failed")}");
#endif
                if ((IntPtr)tmpFontPtr.NativePtr == IntPtr.Zero)
                {
                    Debug.LogError($"[FontLoader] Memory fallback also failed for -> {fontFilePath}");
                }

                return tmpFontPtr;
            }
        }

        private static bool UsesDefaultGlyphRange(SubFontConfig subFont)
        {
            return subFont.StartGlyph == 0 &&
                   subFont.EndGlyph == 0 &&
                   (subFont.CustomGlyphRanges == null || subFont.CustomGlyphRanges.Length == 0);
        }

        private static void BuildGlyphRanges(SubFontConfig subFont)
        {
            subFont.GlyphRangePtr = IntPtr.Zero;

            ImFontGlyphRangesBuilder* builder = ImGuiNative.ImFontGlyphRangesBuilder_ImFontGlyphRangesBuilder();
            if (subFont.CustomGlyphRanges != null && subFont.CustomGlyphRanges.Length > 0)
            {
                for (int i = 0; i < subFont.CustomGlyphRanges.Length; i++)
                {
                    ImGuiNative.ImFontGlyphRangesBuilder_AddChar(builder, subFont.CustomGlyphRanges[i]);
                }
            }
            else
            {
                for (int glyph = subFont.StartGlyph; glyph <= subFont.EndGlyph; glyph++)
                {
                    ImGuiNative.ImFontGlyphRangesBuilder_AddChar(builder, (ushort)glyph);
                }
            }

            ImVector vec = default;
            ImVector* vecPtr = &vec;
            ImGuiNative.ImFontGlyphRangesBuilder_BuildRanges(builder, vecPtr);
            subFont.GlyphRangePtr = vecPtr->Data;
        }
    }
}
