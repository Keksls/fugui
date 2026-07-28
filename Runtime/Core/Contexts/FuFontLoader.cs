#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR && !FUMOBILE
#define FUMOBILE
#endif
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
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
        /// <param name="io">ImGui IO whose atlas receives the fonts.</param>
        /// <param name="fontConf">Fugui font configuration.</param>
        /// <param name="fontScale">Scale applied to font sizes and offsets.</param>
        /// <param name="streamingAssetsPath">Root path used to resolve font files.</param>
        /// <param name="fonts">Runtime font-set registry to populate.</param>
        /// <param name="defaultFont">Resolved default font set.</param>
        /// <returns>Scope that owns build-only native allocations.</returns>
        internal static FuFontLoadResources LoadFonts(
            ImGuiIOPtr io,
            FontConfig fontConf,
            float fontScale,
            string streamingAssetsPath,
            Dictionary<string, Dictionary<int, FontSet>> fonts,
            out FontSet defaultFont)
        {
            FuFontLoadResources resources = new FuFontLoadResources();

            try
            {
                // The returned scope keeps temporary glyph ranges alive until the caller builds the atlas.
                LoadFonts(
                    io,
                    fontConf,
                    fontScale,
                    streamingAssetsPath,
                    fonts,
                    out defaultFont,
                    resources);
                return resources;
            }
            catch (Exception loadException)
            {
                try
                {
                    resources.Dispose();
                }
                catch (Exception cleanupException)
                {
                    // Preserve the font-loading cause when temporary native cleanup also fails.
                    throw new AggregateException(
                        "Font loading failed and its temporary native allocations could not be fully released.",
                        loadException,
                        cleanupException);
                }

                throw;
            }
        }

        /// <summary>
        /// Populates one ImGui font atlas while assigning all temporary native allocations to a build scope.
        /// </summary>
        /// <param name="io">ImGui IO whose atlas receives the fonts.</param>
        /// <param name="fontConf">Fugui font configuration.</param>
        /// <param name="fontScale">Scale applied to font sizes and offsets.</param>
        /// <param name="streamingAssetsPath">Root path used to resolve font files.</param>
        /// <param name="fonts">Runtime font-set registry to populate.</param>
        /// <param name="defaultFont">Resolved default font set.</param>
        /// <param name="resources">Owner of native data borrowed until atlas build.</param>
        private static void LoadFonts(
            ImGuiIOPtr io,
            FontConfig fontConf,
            float fontScale,
            string streamingAssetsPath,
            Dictionary<string, Dictionary<int, FontSet>> fonts,
            out FontSet defaultFont,
            FuFontLoadResources resources)
        {
            defaultFont = null;

            if (fontConf == null)
            {
                Debug.LogError("[FontLoader] FontConfig is null.");
                return;
            }

            // Reset the target atlas before adding the configuration's complete font set.
            string fontPath = FuFontAtlasCache.CombineStreamingPath(streamingAssetsPath, fontConf.FontsFolder);
            io.Fonts.Clear();
            io.NativePtr->FontDefault = default;
            fonts?.Clear();

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

                string fontName = font.Name;
                FontSet fontSet = null;
                if (fonts != null)
                {
                    if (!fonts.TryGetValue(fontName, out var sizeDict))
                    {
                        sizeDict = new Dictionary<int, FontSet>();
                        fonts[fontName] = sizeDict;
                    }

                    if (sizeDict.ContainsKey(font.Size))
                    {
                        Debug.LogWarning($"[FontLoader] Duplicate font config for {fontName} with size {font.Size}. The last entry will be used.");
                    }

                    fontSet = new FontSet(fontName, font.Size);
                    sizeDict[font.Size] = fontSet;
                }

                if (ProcessSubFont(io, fontPath, fontScale, font, GetAvailableSubFonts("Regular", fontPath, font.SubFonts_Regular), resources, out ImFontPtr regular))
                {
                    if (fontSet != null)
                    {
                        fontSet.Regular = regular;
                    }
                }

                if (ProcessSubFont(io, fontPath, fontScale, font, GetAvailableSubFonts("Bold", fontPath, font.SubFonts_Bold), resources, out ImFontPtr bold))
                {
                    if (fontSet != null)
                    {
                        fontSet.Bold = bold;
                    }
                }

                if (ProcessSubFont(io, fontPath, fontScale, font, GetAvailableSubFonts("Italic", fontPath, font.SubFonts_Italic), resources, out ImFontPtr italic))
                {
                    if (fontSet != null)
                    {
                        fontSet.Italic = italic;
                    }
                }

                fontSet?.RebuildResolvedFonts();

                if (fontSet != null &&
                    fontSet.HasAnyNativeFont() &&
                    font.Size == fontConf.DefaultSize &&
                    string.Equals(fontSet.Name, fontConf.DefaultFontName, StringComparison.OrdinalIgnoreCase))
                {
                    defaultFont = fontSet;
                }
            }

            if (defaultFont == null && fonts != null)
            {
                defaultFont = FindFallbackFont(fonts, fontConf);
            }
        }

        /// <summary>
        /// Resolves the closest usable font set when the configured default was not loaded.
        /// </summary>
        /// <param name="fonts">Loaded font sets grouped by name and size.</param>
        /// <param name="fontConf">Configuration that defines the preferred name and size.</param>
        /// <returns>A usable fallback font set, or null when no font was loaded.</returns>
        private static FontSet FindFallbackFont(Dictionary<string, Dictionary<int, FontSet>> fonts, FontConfig fontConf)
        {
            // Preserve the configured size preference before falling back to the first usable font.
            if (fonts == null || fonts.Count == 0)
            {
                return null;
            }

            if (fonts.TryGetValue(fontConf.DefaultFontName, out var sizeDict) &&
                sizeDict.TryGetValue(fontConf.DefaultSize, out FontSet defaultFont) &&
                defaultFont != null &&
                defaultFont.HasAnyNativeFont())
            {
                return defaultFont;
            }

            foreach (var sDict in fonts.Values)
            {
                if (sDict.TryGetValue(fontConf.DefaultSize, out FontSet fontSet) &&
                    fontSet != null &&
                    fontSet.HasAnyNativeFont())
                {
                    return fontSet;
                }
            }

            foreach (var sDict in fonts.Values)
            {
                foreach (var fontSet in sDict.Values)
                {
                    if (fontSet != null && fontSet.HasAnyNativeFont())
                    {
                        return fontSet;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Filters sub-font declarations whose files are available on the current platform.
        /// </summary>
        /// <param name="label">Font style label used by diagnostics.</param>
        /// <param name="fontPath">Resolved font directory.</param>
        /// <param name="subFonts">Configured sub-font declarations.</param>
        /// <returns>Sub-fonts that can be loaded.</returns>
        private static SubFontConfig[] GetAvailableSubFonts(string label, string fontPath, SubFontConfig[] subFonts)
        {
            // Mobile streaming assets are validated by the byte-loading path instead of File.Exists.
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

        /// <summary>
        /// Loads a base sub-font and merges every subsequent available sub-font into it.
        /// </summary>
        /// <param name="io">ImGui IO whose atlas receives the font.</param>
        /// <param name="fontPath">Resolved font directory.</param>
        /// <param name="fontScale">Scale applied to sizes and glyph offsets.</param>
        /// <param name="font">Parent font-size configuration.</param>
        /// <param name="subFonts">Ordered base and merge font declarations.</param>
        /// <param name="resources">Owner of build-only native allocations.</param>
        /// <param name="fontPtr">First successfully loaded font.</param>
        /// <returns>True when at least one sub-font loaded successfully.</returns>
        private static bool ProcessSubFont(
            ImGuiIOPtr io,
            string fontPath,
            float fontScale,
            FontSizeConfig font,
            SubFontConfig[] subFonts,
            FuFontLoadResources resources,
            out ImFontPtr fontPtr)
        {
            fontPtr = default;

            if (subFonts == null || subFonts.Length == 0)
            {
                return false;
            }

            bool hasBaseFont = false;

            foreach (SubFontConfig subFont in subFonts)
            {
                if (subFont == null)
                {
                    continue;
                }

                bool useDefaultGlyphRange = UsesDefaultGlyphRange(subFont);
                IntPtr glyphRanges = useDefaultGlyphRange
                    ? IntPtr.Zero
                    : BuildGlyphRanges(subFont, resources);

                ImFontConfig* nativeConfig = ImGuiNative.ImFontConfig_ImFontConfig();
                if (nativeConfig == null)
                {
                    Debug.LogError($"[FontLoader] Unable to allocate ImFontConfig for '{subFont.FileName}'.");
                    continue;
                }

                ImFontPtr tmpFontPtr;
                try
                {
                    // ImGui copies ImFontConfig into the atlas, so this local native object can be destroyed after AddFont.
                    ImFontConfigPtr config = new ImFontConfigPtr(nativeConfig);
                    config.MergeMode = hasBaseFont;
                    config.GlyphOffset = subFont.GlyphOffset * fontScale;

                    string fontFilePath = FuFontAtlasCache.CombineStreamingPath(fontPath, subFont.FileName);
                    float sizePixels = (font.Size * fontScale) + (subFont.SizeOffset * fontScale);
                    tmpFontPtr = LoadFont(io, fontFilePath, sizePixels, config, glyphRanges);
                }
                finally
                {
                    ImGuiNative.ImFontConfig_destroy(nativeConfig);
                }

                if ((IntPtr)tmpFontPtr.NativePtr != IntPtr.Zero && !hasBaseFont)
                {
                    fontPtr = tmpFontPtr;
                    hasBaseFont = true;
                }
            }

            return (IntPtr)fontPtr.NativePtr != IntPtr.Zero;
        }

        /// <summary>
        /// Loads one font from disk and falls back to explicitly owned native memory when required.
        /// </summary>
        /// <param name="io">ImGui IO whose atlas receives the font.</param>
        /// <param name="fontFilePath">Resolved font file path.</param>
        /// <param name="sizePixels">Font size in pixels.</param>
        /// <param name="config">Temporary ImGui font configuration.</param>
        /// <param name="glyphRanges">Borrowed glyph-range buffer, or zero for ImGui defaults.</param>
        /// <returns>Loaded font pointer, or default on failure.</returns>
        private static ImFontPtr LoadFont(
            ImGuiIOPtr io,
            string fontFilePath,
            float sizePixels,
            ImFontConfigPtr config,
            IntPtr glyphRanges)
        {
#if FUMOBILE
            return LoadFontFromMemory(io, fontFilePath, sizePixels, config, glyphRanges, "Unable to load font bytes");
#else
            ImFontPtr tmpFontPtr = glyphRanges == IntPtr.Zero
                ? io.Fonts.AddFontFromFileTTF(fontFilePath, sizePixels, config)
                : io.Fonts.AddFontFromFileTTF(fontFilePath, sizePixels, config, glyphRanges);

            if ((IntPtr)tmpFontPtr.NativePtr != IntPtr.Zero)
            {
                return tmpFontPtr;
            }

            Debug.LogWarning($"[FontLoader] Failed to load font from file -> {fontFilePath}. Trying memory fallback.");
            return LoadFontFromMemory(io, fontFilePath, sizePixels, config, glyphRanges, "Memory fallback failed to read bytes");
#endif
        }

        /// <summary>
        /// Copies one font into native memory and transfers that allocation to the target atlas.
        /// </summary>
        /// <param name="io">ImGui IO whose atlas receives the font.</param>
        /// <param name="fontFilePath">Resolved font file path.</param>
        /// <param name="sizePixels">Font size in pixels.</param>
        /// <param name="config">Temporary ImGui font configuration.</param>
        /// <param name="glyphRanges">Borrowed glyph-range buffer, or zero for ImGui defaults.</param>
        /// <param name="errorPrefix">Diagnostic prefix used when the file cannot be read.</param>
        /// <returns>Loaded font pointer, or default on failure.</returns>
        private static ImFontPtr LoadFontFromMemory(
            ImGuiIOPtr io,
            string fontFilePath,
            float sizePixels,
            ImFontConfigPtr config,
            IntPtr glyphRanges,
            string errorPrefix)
        {
            byte[] fontData = Fugui.ReadAllBytes(fontFilePath);
            if (fontData == null || fontData.Length == 0)
            {
                Debug.LogError($"[FontLoader] {errorPrefix} for -> {fontFilePath}");
                return default;
            }

            IntPtr fontMemory = ImGui.MemAlloc((uint)fontData.Length);
            if (fontMemory == IntPtr.Zero)
            {
                Debug.LogError($"[FontLoader] Unable to allocate {fontData.Length} native bytes for -> {fontFilePath}");
                return default;
            }

            try
            {
                // Transfer a stable ImGui allocation to the atlas instead of borrowing movable managed memory.
                Marshal.Copy(fontData, 0, fontMemory, fontData.Length);
                config.FontDataOwnedByAtlas = true;
                ImFontPtr tmpFontPtr = glyphRanges == IntPtr.Zero
                    ? io.Fonts.AddFontFromMemoryTTF(fontMemory, fontData.Length, sizePixels, config)
                    : io.Fonts.AddFontFromMemoryTTF(fontMemory, fontData.Length, sizePixels, config, glyphRanges);

#if FUMOBILE
                Debug.Log($"[FontLoader] Trying to load font from memory -> {fontFilePath} : {((IntPtr)tmpFontPtr.NativePtr != IntPtr.Zero ? "Success" : "Failed")}");
#endif
                if ((IntPtr)tmpFontPtr.NativePtr == IntPtr.Zero)
                {
                    Debug.LogError($"[FontLoader] Memory fallback also failed for -> {fontFilePath}");
                    ImGui.MemFree(fontMemory);
                }

                return tmpFontPtr;
            }
            catch
            {
                ImGui.MemFree(fontMemory);
                throw;
            }
        }

        /// <summary>
        /// Returns whether a sub-font can use ImGui's built-in default glyph range.
        /// </summary>
        /// <param name="subFont">Sub-font declaration to inspect.</param>
        /// <returns>True when no custom range is configured.</returns>
        private static bool UsesDefaultGlyphRange(SubFontConfig subFont)
        {
            // Zero bounds and no custom glyph list mean the ImGui default range.
            return subFont.StartGlyph == 0 &&
                   subFont.EndGlyph == 0 &&
                   (subFont.CustomGlyphRanges == null || subFont.CustomGlyphRanges.Length == 0);
        }

        /// <summary>
        /// Builds a compact native ImGui glyph-range buffer for one sub-font.
        /// </summary>
        /// <param name="subFont">Sub-font whose glyph selection is encoded.</param>
        /// <param name="resources">Owner that keeps the buffer alive through atlas build.</param>
        /// <returns>Native zero-terminated start/end range pairs.</returns>
        private static IntPtr BuildGlyphRanges(SubFontConfig subFont, FuFontLoadResources resources)
        {
            // Compact ranges avoid the leaked native builder and vector previously stored in configuration objects.
            List<ushort> ranges = new List<ushort>();

            if (subFont.CustomGlyphRanges != null && subFont.CustomGlyphRanges.Length > 0)
            {
                // ImGui expects compact start/end pairs terminated by zero.
                SortedSet<ushort> glyphs = new SortedSet<ushort>();
                for (int i = 0; i < subFont.CustomGlyphRanges.Length; i++)
                {
                    if (subFont.CustomGlyphRanges[i] != 0)
                    {
                        glyphs.Add(subFont.CustomGlyphRanges[i]);
                    }
                }

                AppendCompactGlyphRanges(glyphs, ranges);
            }
            else
            {
                ushort start = Math.Min(subFont.StartGlyph, subFont.EndGlyph);
                ushort end = Math.Max(subFont.StartGlyph, subFont.EndGlyph);
                if (start != 0 && end != 0)
                {
                    ranges.Add(start);
                    ranges.Add(end);
                }
            }

            if (ranges.Count == 0)
            {
                // Invalid or empty custom selections fall back to ImGui's safe default range.
                return IntPtr.Zero;
            }

            ranges.Add(0);
            IntPtr allocation = ImGui.MemAlloc((uint)(ranges.Count * sizeof(ushort)));
            if (allocation == IntPtr.Zero)
            {
                throw new InvalidOperationException("Unable to allocate an ImGui glyph-range buffer.");
            }

            ushort* destination = (ushort*)allocation;
            for (int i = 0; i < ranges.Count; i++)
            {
                destination[i] = ranges[i];
            }

            resources.Own(allocation);
            return allocation;
        }

        /// <summary>
        /// Converts a sorted set of glyphs into ImGui start/end range pairs.
        /// </summary>
        /// <param name="glyphs">Sorted glyph code points.</param>
        /// <param name="ranges">Destination start/end pair list.</param>
        private static void AppendCompactGlyphRanges(SortedSet<ushort> glyphs, List<ushort> ranges)
        {
            // Consecutive code points collapse into one inclusive pair.
            bool hasRange = false;
            ushort rangeStart = 0;
            ushort previous = 0;

            foreach (ushort glyph in glyphs)
            {
                if (!hasRange)
                {
                    rangeStart = glyph;
                    previous = glyph;
                    hasRange = true;
                    continue;
                }

                if (glyph == previous + 1)
                {
                    previous = glyph;
                    continue;
                }

                ranges.Add(rangeStart);
                ranges.Add(previous);
                rangeStart = glyph;
                previous = glyph;
            }

            if (hasRange)
            {
                ranges.Add(rangeStart);
                ranges.Add(previous);
            }
        }
    }
}
