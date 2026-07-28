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

            internal ImFontPtr Regular
            {
                get { return _regular; }
                set
                {
                    _regular = value;
                    RebuildResolvedFonts();
                }
            }

            internal ImFontPtr Bold
            {
                get { return _bold; }
                set
                {
                    _bold = value;
                    RebuildResolvedFonts();
                }
            }

            internal ImFontPtr Italic
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

            /// <summary>
            /// Resolves every font style against the native fonts that loaded successfully.
            /// </summary>
            internal void RebuildResolvedFonts()
            {
                // A partially configured family remains usable by falling back to any loaded style.
                ImFontPtr fallback = HasNativeFont(Regular)
                    ? Regular
                    : HasNativeFont(Bold)
                        ? Bold
                        : Italic;

                _regularPushFont = fallback;
                _boldPushFont = HasNativeFont(Bold) ? Bold : fallback;
                _italicPushFont = HasNativeFont(Italic) ? Italic : fallback;
            }

            /// <summary>
            /// Returns the resolved native font for one requested style.
            /// </summary>
            /// <param name="type">Requested font style.</param>
            /// <returns>Resolved native font pointer.</returns>
            internal ImFontPtr GetFont(FontType type)
            {
                // Resolved pointers already include style fallback decisions.
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

            /// <summary>
            /// Attempts to resolve a usable native font for one requested style.
            /// </summary>
            /// <param name="type">Requested font style.</param>
            /// <param name="font">Resolved native font pointer.</param>
            /// <returns>True when a native font is available.</returns>
            internal bool TryGetFont(FontType type, out ImFontPtr font)
            {
                // Validate the pointer because failed font loads keep a managed configuration entry.
                font = GetFont(type);
                return HasNativeFont(font);
            }

            /// <summary>
            /// Returns whether this set contains at least one successfully loaded native font.
            /// </summary>
            /// <returns>True when at least one style owns a valid native font pointer.</returns>
            internal bool HasAnyNativeFont()
            {
                // Any native style can serve as the family fallback.
                return HasNativeFont(Regular) ||
                       HasNativeFont(Bold) ||
                       HasNativeFont(Italic);
            }

            /// <summary>
            /// Returns whether an ImGui font pointer references a native font.
            /// </summary>
            /// <param name="font">Font pointer to validate.</param>
            /// <returns>True when the pointer is non-null.</returns>
            internal static bool HasNativeFont(ImFontPtr font)
            {
                // ImFontPtr is a value wrapper, so native-pointer validation must be explicit.
                unsafe
                {
                    return (IntPtr)font.NativePtr != IntPtr.Zero;
                }
            }
        }
}
