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
        #region State
        public int DefaultSize = 14;
        public FontSizeConfig[] Fonts;
        [Header("The font folder must be paced into StreamingAssets")]
        public string FontsFolder = "Fugui/fonts/current/";
        [Header("Baked atlas cache")]
        public bool UseBakedFontAtlas = false;
        [Tooltip("Folder where baked font atlas textures are stored, relative to StreamingAssets.")]
        public string BakedFontAtlasFolder = FuFontAtlasCache.DefaultBakedAtlasFolder;
        [Tooltip("Font scale values to bake from the editor.")]
        public float[] BakedFontAtlasScales = new[] { 1f, 2f };
        [Tooltip("Share one native ImGui font atlas between contexts that use the same quantized font scale.")]
        public bool UseSharedFontAtlas = true;
        [Tooltip("Round font scales to stable buckets before loading/building atlases.")]
        public bool QuantizeFontScale = true;
        [Tooltip("Step used when QuantizeFontScale is enabled. 0.25 means 1.0, 1.25, 1.5, 1.75, 2.0, ...")]
        public float FontScaleQuantizationStep = 0.25f;
        [Tooltip("Use an 8-bit alpha texture for generated font atlas textures. Baked PNG atlases may still decode as RGBA.")]
        public bool UseAlpha8FontAtlasTexture = true;
        #endregion
    }
}
