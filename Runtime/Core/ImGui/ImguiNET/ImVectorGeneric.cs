using System;
using System.Runtime.CompilerServices;

namespace ImGuiNET
{
        /// <summary>
        /// Represents the Im Vector data structure.
        /// </summary>
        public unsafe struct ImVector<T>
        {
            #region State
            public readonly int Size;
            public readonly int Capacity;
            public readonly IntPtr Data;
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Im Vector class.
            /// </summary>
            /// <param name="vector">The vector value.</param>
            public ImVector(ImVector vector)
            {
                Size = vector.Size;
                Capacity = vector.Capacity;
                Data = vector.Data;
            }

            /// <summary>
            /// Initializes a new instance of the Im Vector class.
            /// </summary>
            /// <param name="size">The size value.</param>
            /// <param name="capacity">The capacity value.</param>
            /// <param name="data">The data value.</param>
            public ImVector(int size, int capacity, IntPtr data)
            {
                Size = size;
                Capacity = capacity;
                Data = data;
            }
            #endregion

            #region State
            public ref T this[int index] => ref Unsafe.AsRef<T>((byte*)Data + index * Unsafe.SizeOf<T>());
            #endregion
        }
}