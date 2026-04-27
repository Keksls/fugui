using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ImGuiNET
{
    /// <summary>
    /// Represents the Util type.
    /// </summary>
    internal static unsafe class Util
    {
        #region State
        internal const int StackAllocationSizeLimit = 2048;
        #endregion

        /// <summary>
        /// Returns the string from ptr result.
        /// </summary>
        /// <param name="ptr">The ptr value.</param>
        /// <returns>The result of the operation.</returns>
        public static string StringFromPtr(byte* ptr)
        {
            int characters = 0;
            while (ptr[characters] != 0)
            {
                characters++;
            }

            return Encoding.UTF8.GetString(ptr, characters);
        }

        /// <summary>
        /// Returns the are strings equal result.
        /// </summary>
        /// <param name="a">The a value.</param>
        /// <param name="aLength">The a Length value.</param>
        /// <param name="b">The b value.</param>
        /// <returns>The result of the operation.</returns>
        internal static bool AreStringsEqual(byte* a, int aLength, byte* b)
        {
            for (int i = 0; i < aLength; i++)
            {
                if (a[i] != b[i]) { return false; }
            }

            if (b[aLength] != 0) { return false; }

            return true;
        }

        /// <summary>
        /// Returns the allocate result.
        /// </summary>
        /// <param name="byteCount">The byte Count value.</param>
        /// <returns>The result of the operation.</returns>
        internal static byte* Allocate(int byteCount) => (byte*)Marshal.AllocHGlobal(byteCount);

        /// <summary>
        /// Runs the free workflow.
        /// </summary>
        /// <param name="ptr">The ptr value.</param>
        internal static void Free(byte* ptr) => Marshal.FreeHGlobal((IntPtr)ptr);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        internal static int CalcSizeInUtf8(ReadOnlySpan<char> s, int start, int length)
#else
        /// <summary>
        /// Returns the calc size in utf8 result.
        /// </summary>
        /// <param name="s">The s value.</param>
        /// <param name="start">The start value.</param>
        /// <param name="length">The length value.</param>
        /// <returns>The result of the operation.</returns>
        internal static int CalcSizeInUtf8(string s, int start, int length)
#endif
        {
            if (start < 0 || length < 0 || start + length > s.Length)
            {
                throw new ArgumentOutOfRangeException();
            }
            if(s.Length == 0) return 0;

            fixed (char* utf16Ptr = s)
            {
                return Encoding.UTF8.GetByteCount(utf16Ptr + start, length);
            }
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        internal static int GetUtf8(ReadOnlySpan<char> s, byte* utf8Bytes, int utf8ByteCount)
        {
            if (s.IsEmpty)
            {
                return 0;
            }

            fixed (char* utf16Ptr = s)
            {
                return Encoding.UTF8.GetBytes(utf16Ptr, s.Length, utf8Bytes, utf8ByteCount);
            }
        }
#endif

        /// <summary>
        /// Gets the utf8.
        /// </summary>
        /// <param name="s">The s value.</param>
        /// <param name="utf8Bytes">The utf8 Bytes value.</param>
        /// <param name="utf8ByteCount">The utf8 Byte Count value.</param>
        /// <returns>The result of the operation.</returns>
        internal static int GetUtf8(string s, byte* utf8Bytes, int utf8ByteCount)
        {
            fixed (char* utf16Ptr = s)
            {
                return Encoding.UTF8.GetBytes(utf16Ptr, s.Length, utf8Bytes, utf8ByteCount);
            }
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        internal static int GetUtf8(ReadOnlySpan<char> s, int start, int length, byte* utf8Bytes, int utf8ByteCount)
#else
        /// <summary>
        /// Gets the utf8.
        /// </summary>
        /// <param name="s">The s value.</param>
        /// <param name="start">The start value.</param>
        /// <param name="length">The length value.</param>
        /// <param name="utf8Bytes">The utf8 Bytes value.</param>
        /// <param name="utf8ByteCount">The utf8 Byte Count value.</param>
        /// <returns>The result of the operation.</returns>
        internal static int GetUtf8(string s, int start, int length, byte* utf8Bytes, int utf8ByteCount)
#endif
        {
            if (start < 0 || length < 0 || start + length > s.Length)
            {
                throw new ArgumentOutOfRangeException();
            }
            if (s.Length == 0) return 0;

            fixed (char* utf16Ptr = s)
            {
                return Encoding.UTF8.GetBytes(utf16Ptr + start, length, utf8Bytes, utf8ByteCount);
            }
        }
    }
}