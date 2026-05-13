#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR && !FUMOBILE
#define FUMOBILE
#endif
using ImGuiNET;
using System;

namespace Fu
{
        internal readonly struct FontKey : IEquatable<FontKey>
        {
            public readonly string Name;
            public readonly int Size;

            internal FontKey(string name, int size)
            {
                Name = NormalizeName(name);
                Size = size;
            }

            internal static string NormalizeName(string name)
            {
                return string.IsNullOrWhiteSpace(name)
                    ? FontConfig.FallbackFontName
                    : name.Trim();
            }

            public bool Equals(FontKey other)
            {
                return Size == other.Size &&
                       string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
            }

            public override bool Equals(object obj)
            {
                return obj is FontKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (StringComparer.OrdinalIgnoreCase.GetHashCode(Name ?? string.Empty) * 397) ^ Size;
                }
            }

            public override string ToString()
            {
                return $"{Name} {Size}px";
            }
        }

        /// <summary>
        /// Represents the Font Set type.
        /// </summary>
        internal class FontSet
        {
            #region State
            public string Name;
            public int Size;
            public ImFontPtr Regular;
            public ImFontPtr Bold;
            public ImFontPtr Italic;
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Font Set class.
            /// </summary>
            /// <param name="name">The font name value.</param>
            /// <param name="size">The size value.</param>
            internal FontSet(string name, int size)
            {
                Name = FontKey.NormalizeName(name);
                Size = size;
            }
            #endregion

            internal ImFontPtr GetFont(FontType type)
            {
                ImFontPtr font;
                switch (type)
                {
                    case FontType.Bold:
                        font = Bold;
                        break;
                    case FontType.Italic:
                        font = Italic;
                        break;
                    default:
                        font = Regular;
                        break;
                }

                if (font.Equals(default(ImFontPtr)) && type != FontType.Regular)
                {
                    font = Regular;
                }

                return font;
            }
        }
}
