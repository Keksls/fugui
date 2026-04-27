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
        #endregion
    }
}