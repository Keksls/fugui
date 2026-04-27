using System.Text;

namespace ImGuiNET
{
    /// <summary>
    /// Represents the Null Terminated String data structure.
    /// </summary>
    public unsafe struct NullTerminatedString
    {
        #region State
        public readonly byte* Data;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the Null Terminated String class.
        /// </summary>
        /// <param name="data">The data value.</param>
        public NullTerminatedString(byte* data)
        {
            Data = data;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Returns the to string result.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public override string ToString()
        {
            int length = 0;
            byte* ptr = Data;
            while (*ptr != 0)
            {
                length += 1;
                ptr += 1;
            }

            return Encoding.ASCII.GetString(Data, length);
        }
        #endregion

        #region Members
        /// <summary>
        /// Converts the value to string.
        /// </summary>
        /// <param name="nts">The nts value.</param>
        /// <returns>The result of the operation.</returns>
        public static implicit operator string(NullTerminatedString nts) => nts.ToString();
        #endregion
    }
}