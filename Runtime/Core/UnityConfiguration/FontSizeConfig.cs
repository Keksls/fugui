using ImGuiNET;
using System;

namespace Fu
{
        /// <summary>
        /// Represents the Font Size Config type.
        /// </summary>
        [Serializable]
        public class FontSizeConfig
        {
            #region State
            public int Size = 14;
            public ImFontPtr FontPtr;
            public SubFontConfig[] SubFonts_Regular;
            public SubFontConfig[] SubFonts_Bold;
            public SubFontConfig[] SubFonts_Italic;
            #endregion
        }
}