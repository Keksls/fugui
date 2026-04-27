using System;

namespace Fu.Framework
{
        /// <summary>
        /// DRAG : BYTE, SHORT, USHORT, INT, UINT, LONG, ULONG, FLOAT
        /// Force Figui Object mapping to draw this field as a Slider
        /// </summary>
        [AttributeUsage(AttributeTargets.Field)]
        public class FuSlider : Attribute
        {
            #region State
            public float Min;
            public float Max;
            public string[] Labels;
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Fu Slider class.
            /// </summary>
            /// <param name="min">The min value.</param>
            /// <param name="max">The max value.</param>
            public FuSlider(byte min, byte max)
            {
                Min = min;
                Max = max;
            }
            /// <summary>
            /// Initializes a new instance of the Fu Slider class.
            /// </summary>
            /// <param name="min">The min value.</param>
            /// <param name="max">The max value.</param>
            public FuSlider(short min, short max)
            {
                Min = min;
                Max = max;
            }
            /// <summary>
            /// Initializes a new instance of the Fu Slider class.
            /// </summary>
            /// <param name="min">The min value.</param>
            /// <param name="max">The max value.</param>
            public FuSlider(ushort min, ushort max)
            {
                Min = min;
                Max = max;
            }
            /// <summary>
            /// Initializes a new instance of the Fu Slider class.
            /// </summary>
            /// <param name="min">The min value.</param>
            /// <param name="max">The max value.</param>
            public FuSlider(int min, int max)
            {
                Min = min;
                Max = max;
            }
            /// <summary>
            /// Initializes a new instance of the Fu Slider class.
            /// </summary>
            /// <param name="min">The min value.</param>
            /// <param name="max">The max value.</param>
            public FuSlider(uint min, uint max)
            {
                Min = min;
                Max = max;
            }
            /// <summary>
            /// Initializes a new instance of the Fu Slider class.
            /// </summary>
            /// <param name="min">The min value.</param>
            /// <param name="max">The max value.</param>
            public FuSlider(long min, long max)
            {
                Min = min;
                Max = max;
            }
            /// <summary>
            /// Initializes a new instance of the Fu Slider class.
            /// </summary>
            /// <param name="min">The min value.</param>
            /// <param name="max">The max value.</param>
            public FuSlider(ulong min, ulong max)
            {
                Min = min;
                Max = max;
            }
            /// <summary>
            /// Initializes a new instance of the Fu Slider class.
            /// </summary>
            /// <param name="min">The min value.</param>
            /// <param name="max">The max value.</param>
            public FuSlider(float min, float max)
            {
                Min = min;
                Max = max;
            }
            #endregion
        }
}