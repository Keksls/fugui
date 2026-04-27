using System;

namespace Fu.Framework
{
    /// <summary>
    /// Force Figui Object mapping to ignore this field and do not draw it
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class FuHidden : Attribute
    {
    }
}