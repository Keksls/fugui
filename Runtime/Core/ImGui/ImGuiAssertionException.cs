using System;

namespace ImGuiNET
{
        /// <summary>
        /// Exception thrown when an ImGui assertion fails.
        /// </summary>
        public class ImGuiAssertionException : Exception
        {
            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Im Gui Assertion Exception class.
            /// </summary>
            /// <param name="message">The message value.</param>
            public ImGuiAssertionException(string message) : base(message)
            {
            }
            #endregion
        }
}