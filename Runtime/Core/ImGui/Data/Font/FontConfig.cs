using UnityEngine;

namespace Fu.Core.DearImGui
{
    [CreateAssetMenu(fileName = "FontConfig", menuName = "Fugui/FontConfig", order = 1)]
    public class FontConfig : ScriptableObject
    {
        public int DefaultSize = 14;
        public int[] AdditionnalFontSizes = new int[] { 12, 16 };
        [Header("The font folder must be paced into StreamingAssets")]
        public string FontsFolder = "Fugui/fonts/current/";
        public string RegularFontName = "regular.ttf";
        public string BoldFontName = "bold.ttf";
        public string IconsFontName = "icons.ttf";
        public ushort StartIconsGlyph = '\uEF00';
        public ushort EndIconsGlyph = '\uEFD8';
        public bool AddBold = true;
        public bool AddIconsToBold = false;
    }
}