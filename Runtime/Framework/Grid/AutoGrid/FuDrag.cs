using System;

namespace Fu.Framework
{
        /// <summary>
        /// DRAG : BYTE, SHORT, USHORT, INT, UINT, LONG, ULONG, FLOAT, VECTOR2, VECTOR3, VECTOR4
        /// Force Figui Object mapping to draw this field as an integer Draggable input
        /// </summary>
        [AttributeUsage(AttributeTargets.Field)]
        public class FuDrag : Attribute
        {
            #region State
            public float Min;
            public float Max;
            public string[] Labels;
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Fu Drag class.
            /// </summary>
            /// <param name="min">The min value.</param>
            /// <param name="max">The max value.</param>
            /// <param name="labels">The labels value.</param>
            public FuDrag(byte min, byte max, params string[] labels)
            {
                Min = min;
                Max = max;
                Labels = labels;
            }
            /// <summary>
            /// Initializes a new instance of the Fu Drag class.
            /// </summary>
            /// <param name="min">The min value.</param>
            /// <param name="max">The max value.</param>
            /// <param name="labels">The labels value.</param>
            public FuDrag(short min, short max, params string[] labels)
            {
                Min = min;
                Max = max;
                Labels = labels;
            }
            /// <summary>
            /// Initializes a new instance of the Fu Drag class.
            /// </summary>
            /// <param name="min">The min value.</param>
            /// <param name="max">The max value.</param>
            /// <param name="labels">The labels value.</param>
            public FuDrag(ushort min, ushort max, params string[] labels)
            {
                Min = min;
                Max = max;
                Labels = labels;
            }
            /// <summary>
            /// Initializes a new instance of the Fu Drag class.
            /// </summary>
            /// <param name="min">The min value.</param>
            /// <param name="max">The max value.</param>
            /// <param name="labels">The labels value.</param>
            public FuDrag(int min, int max, params string[] labels)
            {
                Min = min;
                Max = max;
                Labels = labels;
            }
            /// <summary>
            /// Initializes a new instance of the Fu Drag class.
            /// </summary>
            /// <param name="min">The min value.</param>
            /// <param name="max">The max value.</param>
            /// <param name="labels">The labels value.</param>
            public FuDrag(uint min, uint max, params string[] labels)
            {
                Min = min;
                Max = max;
                Labels = labels;
            }
            /// <summary>
            /// Initializes a new instance of the Fu Drag class.
            /// </summary>
            /// <param name="min">The min value.</param>
            /// <param name="max">The max value.</param>
            /// <param name="labels">The labels value.</param>
            public FuDrag(long min, long max, params string[] labels)
            {
                Min = min;
                Max = max;
                Labels = labels;
            }
            /// <summary>
            /// Initializes a new instance of the Fu Drag class.
            /// </summary>
            /// <param name="min">The min value.</param>
            /// <param name="max">The max value.</param>
            /// <param name="labels">The labels value.</param>
            public FuDrag(ulong min, ulong max, params string[] labels)
            {
                Min = min;
                Max = max;
                Labels = labels;
            }
            /// <summary>
            /// Initializes a new instance of the Fu Drag class.
            /// </summary>
            /// <param name="min">The min value.</param>
            /// <param name="max">The max value.</param>
            /// <param name="labels">The labels value.</param>
            public FuDrag(float min, float max, params string[] labels)
            {
                Min = min;
                Max = max;
                Labels = labels;
            }
            #endregion
        }
}