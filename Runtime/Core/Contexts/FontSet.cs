#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR && !FUMOBILE
#define FUMOBILE
#endif
using ImGuiNET;
using System;

namespace Fu
{
        /// <summary>
        /// Represents the Font Set type.
        /// </summary>
        internal class FontSet
        {
            #region State
            public string Name;
            public int Size;
            private ImFontPtr _regular;
            private ImFontPtr _bold;
            private ImFontPtr _italic;
            private ImFontPtr _regularPushFont;
            private ImFontPtr _boldPushFont;
            private ImFontPtr _italicPushFont;

            public ImFontPtr Regular
            {
                get { return _regular; }
                set
                {
                    _regular = value;
                    RebuildResolvedFonts();
                }
            }

            public ImFontPtr Bold
            {
                get { return _bold; }
                set
                {
                    _bold = value;
                    RebuildResolvedFonts();
                }
            }

            public ImFontPtr Italic
            {
                get { return _italic; }
                set
                {
                    _italic = value;
                    RebuildResolvedFonts();
                }
            }
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Font Set class.
            /// </summary>
            /// <param name="name">The font name value.</param>
            /// <param name="size">The size value.</param>
            internal FontSet(string name, int size)
            {
                Name = name;
                Size = size;
            }
            #endregion

            internal void RebuildResolvedFonts()
            {
                _regularPushFont = Regular;
                _boldPushFont = HasNativeFont(Bold) ? Bold : Regular;
                _italicPushFont = HasNativeFont(Italic) ? Italic : Regular;
            }

            internal ImFontPtr GetFont(FontType type)
            {
                switch (type)
                {
                    case FontType.Bold:
                        return _boldPushFont;
                    case FontType.Italic:
                        return _italicPushFont;
                    default:
                        return _regularPushFont;
                }
            }

            internal bool TryGetFont(FontType type, out ImFontPtr font)
            {
                font = GetFont(type);
                return HasNativeFont(font);
            }

            internal static bool HasNativeFont(ImFontPtr font)
            {
                unsafe
                {
                    return (IntPtr)font.NativePtr != IntPtr.Zero;
                }
            }
        }
}
