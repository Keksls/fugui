using System;
using System.Runtime.CompilerServices;

namespace ImGuiNET
{
    /// <summary>
    /// Represents the Range Accessor data structure.
    /// </summary>
    public unsafe struct RangeAccessor<T> where T : struct
    {
        #region State
        private static readonly int s_sizeOfT = Unsafe.SizeOf<T>();

        public readonly void* Data;
        public readonly int Count;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the Range Accessor class.
        /// </summary>
        /// <param name="data">The data value.</param>
        /// <param name="count">The count value.</param>
        public RangeAccessor(IntPtr data, int count) : this(data.ToPointer(), count) { }
        /// <summary>
        /// Initializes a new instance of the Range Accessor class.
        /// </summary>
        /// <param name="data">The data value.</param>
        /// <param name="count">The count value.</param>
        public RangeAccessor(void* data, int count)
        {
            Data = data;
            Count = count;
        }
        #endregion

        #region State
        public ref T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    throw new IndexOutOfRangeException();
                }

                return ref Unsafe.AsRef<T>((byte*)Data + s_sizeOfT * index);
            }
        }
        #endregion
    }
}