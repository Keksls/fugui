using System.Text;

namespace ImGuiNET
{
        /// <summary>
        /// Represents the Range Accessor Extensions type.
        /// </summary>
        public static class RangeAccessorExtensions
        {
            #region Methods
            /// <summary>
            /// Gets the string ascii.
            /// </summary>
            /// <param name="stringAccessor">The string Accessor value.</param>
            /// <returns>The result of the operation.</returns>
            public static unsafe string GetStringASCII(this RangeAccessor<byte> stringAccessor)
            {
                return Encoding.ASCII.GetString((byte*)stringAccessor.Data, stringAccessor.Count);
            }
            #endregion
        }
}