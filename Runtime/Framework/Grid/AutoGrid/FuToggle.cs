using System;

namespace Fu.Framework
{
        /// <summary>
        /// TOGGLE : BOOLEAN ONLY
        /// Force Figui Object mapping to draw this field as a toggle
        /// </summary>
        [AttributeUsage(AttributeTargets.Field)]
        public class FuToggle : Attribute
        {
        }
}