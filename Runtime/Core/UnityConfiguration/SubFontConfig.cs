using ImGuiNET;
using System;
using UnityEngine;

namespace Fu
{
        /// <summary>
        /// Represents the Sub Font Config type.
        /// </summary>
        [Serializable]
        public class SubFontConfig
        {
            #region State
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
            #endregion
        }
}
