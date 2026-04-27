#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR && !FUMOBILE
#define FUMOBILE
#endif
using ImGuiNET;

namespace Fu
{
        /// <summary>
        /// Represents the Font Set type.
        /// </summary>
        internal class FontSet
        {
            #region State
            public int Size;
            public ImFontPtr Regular;
            public ImFontPtr Bold;
            public ImFontPtr Italic;
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Font Set class.
            /// </summary>
            /// <param name="size">The size value.</param>
            internal FontSet(int size)
            {
                Size = size;
            }
            #endregion
        }
}