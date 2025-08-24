using ImGuiNET;
using System;
using UnityEngine;

namespace Fu
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
        public FontSizeConfig[] Fonts;
        [Header("The font folder must be paced into StreamingAssets")]
        public string FontsFolder = "Fugui/fonts/current/";
    }

    [Serializable]
    public class FontSizeConfig
    {
        public int Size = 14;
        public ImFontPtr FontPtr;
        public SubFontConfig[] SubFonts_Regular;
        public SubFontConfig[] SubFonts_Bold;
        public SubFontConfig[] SubFonts_Italic;
    }

    [Serializable]
    public class SubFontConfig
    {
        public string FileName = "icons.ttf";
        [HideInInspector]
        public string FilePath;
        public ushort StartGlyph = '\uE000';
        public ushort EndGlyph = '\uF8FF';
        public ushort[] CustomGlyphRanges;
        [HideInInspector]
        public IntPtr GlyphRangePtr;
        [HideInInspector]
        public ImFontConfigPtr FontConfigPtr;
        public float SizeOffset = 0f;
        public Vector2 GlyphOffset = Vector2.zero;
    }
}