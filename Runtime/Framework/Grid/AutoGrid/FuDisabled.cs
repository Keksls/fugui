using System;

namespace Fu.Framework
{
        /// <summary>
        /// Force Figui Object mapping to disable this field (draw, but not editable)
        /// </summary>
        [AttributeUsage(AttributeTargets.Field)]
        public class FuDisabled : Attribute
        {
        }
}