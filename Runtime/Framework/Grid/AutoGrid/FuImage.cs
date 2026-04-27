using System;

namespace Fu.Framework
{
        /// <summary>
        /// Force Figui Object mapping to use FuImage data according to this attribute
        /// </summary>
        [AttributeUsage(AttributeTargets.Field)]
        public class FuImage : Attribute
        {
            #region State
            public UnityEngine.Vector2 Size;
            public UnityEngine.Vector4 Color;
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Fu Image class.
            /// </summary>
            /// <param name="width">The width value.</param>
            /// <param name="height">The height value.</param>
            /// <param name="r">The r value.</param>
            /// <param name="g">The g value.</param>
            /// <param name="b">The b value.</param>
            /// <param name="a">The a value.</param>
            public FuImage(float width = 32f, float height = 32f, float r = 1f, float g = 1f, float b = 1f, float a = 1f)
            {
                Size = new UnityEngine.Vector2(width, height);
                Color = new UnityEngine.Vector4(r, g, b, a);
            }
            #endregion
        }
}