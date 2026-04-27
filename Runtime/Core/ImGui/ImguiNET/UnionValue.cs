using System;
using System.Runtime.InteropServices;

namespace ImGuiNET
{
        /// <summary>
        /// Represents the Union Value data structure.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        public struct UnionValue
        {
            #region State
            [FieldOffset(0)]
            public int ValueI32;
            [FieldOffset(0)]
            public float ValueF32;
            [FieldOffset(0)]
            public IntPtr ValuePtr;
            #endregion
        }
}