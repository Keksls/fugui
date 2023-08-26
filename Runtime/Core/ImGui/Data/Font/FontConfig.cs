using ImGuiNET;
using System;
using UnityEngine;

namespace Fu.Core.DearImGui
{
    /// <summary>
    /// Read this carefully
    /// 
    /// Fugui allow you to add icons but you need to use given glyph ranges.
    /// 
    /// Classic (non duotone) Icons must be from range 57344 (0xE000) to 60542 (0xEC7E)
    /// Duotone Icons must be from range 60543 (0xEC7F) to 63743 (0xF8FF)
    /// </summary>
    [CreateAssetMenu(fileName = "FontConfig", menuName = "Fugui/FontConfig", order = 1)]
    public class FontConfig : ScriptableObject
    {
        public int DefaultSize = 14;
        public int[] AdditionnalFontSizes = new int[] { 12, 16 };
        [Header("The font folder must be paced into StreamingAssets")]
        public string FontsFolder = "Fugui/fonts/current/";
        public string RegularFontName = "regular.ttf";
        public string BoldFontName = "bold.ttf";
        public IconConfig[] FuguiIcons;
        public IconConfig[] CustomIcons;

        // used by font helper
        public bool ImportFontHelperIcons;
        public IconConfig FontHelperIcons;

        public bool AddBold = true;
        public bool AddIconsToBold = false;
    }

    [Serializable]
    public class IconConfig
    {
        public string IconsFontName = "icons.ttf";
        public ushort StartGlyph = '\uE000';
        public ushort EndGlyph = '\uF8FF';
        public float FontIconsSizeOffset = 0f;
        [HideInInspector]
        public IntPtr GlyphRangePtr;
        [HideInInspector]
        public string IconFilePath;
        public ImFontPtr FontPtr;
    }
}