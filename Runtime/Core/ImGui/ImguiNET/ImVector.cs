using System;
using System.Runtime.CompilerServices;

namespace ImGuiNET
{
    /// <summary>
    /// Represents the Im Vector data structure.
    /// </summary>
    public unsafe struct ImVector
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

        #region Methods
        /// <summary>
        /// Returns the ref result.
        /// </summary>
        /// <param name="index">The index value.</param>
        /// <returns>The result of the operation.</returns>
        public ref T Ref<T>(int index)
        {
            return ref Unsafe.AsRef<T>((byte*)Data + index * Unsafe.SizeOf<T>());
        }

        /// <summary>
        /// Returns the address result.
        /// </summary>
        /// <param name="index">The index value.</param>
        /// <returns>The result of the operation.</returns>
        public IntPtr Address<T>(int index)
        {
            return (IntPtr)((byte*)Data + index * Unsafe.SizeOf<T>());
        }
        #endregion
    }
}