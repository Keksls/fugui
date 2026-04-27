using System;
using System.Runtime.CompilerServices;

namespace ImGuiNET
{
        /// <summary>
        /// Represents the Im Ptr Vector data structure.
        /// </summary>
        public unsafe struct ImPtrVector<T>
        {
            #region State
            public readonly int Size;
            public readonly int Capacity;
            public readonly IntPtr Data;
            private readonly int _stride;
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Im Ptr Vector class.
            /// </summary>
            /// <param name="vector">The vector value.</param>
            /// <param name="stride">The stride value.</param>
            public ImPtrVector(ImVector vector, int stride)
                : this(vector.Size, vector.Capacity, vector.Data, stride)
            { }

            /// <summary>
            /// Initializes a new instance of the Im Ptr Vector class.
            /// </summary>
            /// <param name="size">The size value.</param>
            /// <param name="capacity">The capacity value.</param>
            /// <param name="data">The data value.</param>
            /// <param name="stride">The stride value.</param>
            public ImPtrVector(int size, int capacity, IntPtr data, int stride)
            {
                Size = size;
                Capacity = capacity;
                Data = data;
                _stride = stride;
            }
            #endregion

            #region State
            public T this[int index]
            {
                get
                {
                    byte* address = (byte*)Data + index * _stride;
                    T ret = Unsafe.Read<T>(&address);
                    return ret;
                }
            }
            #endregion
        }
}