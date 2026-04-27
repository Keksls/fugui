using System;

namespace Fu.Framework
{
        /// <summary>
        /// COLORPICKER : VECTOR4 OR VECTOR3
        /// Force Figui Object mapping to draw this field as a ColorPicker
        /// If it'a Vector3, Alpha will not be displayed
        /// </summary>
        [AttributeUsage(AttributeTargets.Field)]
        public class FuColorPicker : Attribute
        {
        }
}