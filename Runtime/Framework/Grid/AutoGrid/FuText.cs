using System;

namespace Fu.Framework
{
        /// <summary>
        /// TEXT INPUT : STRING ONLY
        /// Force Figui Object mapping to draw this field as a text input area
        /// </summary>
        [AttributeUsage(AttributeTargets.Field)]
        public class FuText : Attribute
        {
            #region State
            public string Hint = string.Empty;
            public float Height = -1f;
            public int Lenght = 4096;
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Fu Text class.
            /// </summary>
            /// <param name="hint">The hint value.</param>
            /// <param name="height">The height value.</param>
            /// <param name="lenght">The lenght value.</param>
            public FuText(string hint, float height, int lenght)
            {
                Hint = hint;
                Height = height;
                Lenght = lenght;
            }
            #endregion
        }
}