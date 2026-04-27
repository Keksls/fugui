using System;

namespace Fu.Framework
{
        /// <summary>
        /// TOOLTIP¨: ANY Field
        /// Force Figui Object mapping to add a custom tooltip on the FuElement
        /// </summary>
        [AttributeUsage(AttributeTargets.Field)]
        public class FuTooltip : Attribute
        {
            #region State
            public string Text;
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Fu Tooltip class.
            /// </summary>
            /// <param name="text">The text value.</param>
            public FuTooltip(string text)
            {
                Text = text;
            }
            #endregion
        }
}