using System;
using System.Runtime.CompilerServices;

namespace ImGuiNET
{
        /// <summary>
        /// Represents the Range Ptr Accessor data structure.
        /// </summary>
        public unsafe struct RangePtrAccessor<T> where T : struct
        {
            #region State
            public readonly void* Data;
            public readonly int Count;
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Range Ptr Accessor class.
            /// </summary>
            /// <param name="data">The data value.</param>
            /// <param name="count">The count value.</param>
            public RangePtrAccessor(IntPtr data, int count) : this(data.ToPointer(), count) { }
            /// <summary>
            /// Initializes a new instance of the Range Ptr Accessor class.
            /// </summary>
            /// <param name="data">The data value.</param>
            /// <param name="count">The count value.</param>
            public RangePtrAccessor(void* data, int count)
            {
                Data = data;
                Count = count;
            }
            #endregion

            #region State
            public T this[int index]
            {
                get
                {
                    if (index < 0 || index >= Count)
                    {
                        throw new IndexOutOfRangeException();
                    }

                    return Unsafe.Read<T>((byte*)Data + sizeof(void*) * index);
                }
            }
            #endregion
        }
}