using System;

namespace Fugui.Framework
{
    /// <summary>
    /// Force Figui Object mapping to ignore this field and do not draw it
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class Hiden : Attribute
    {
    }

    /// <summary>
    /// Force Figui Object mapping to disable this field (draw, but not editable)
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class Disabled : Attribute
    {
    }

    /// <summary>
    /// TOGGLE : BOOLEAN ONLY
    /// Force Figui Object mapping to draw this field as a toggle
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class Toggle : Attribute
    {
    }

    /// <summary>
    /// COLORPICKER : VECTOR4 OR VECTOR3
    /// Force Figui Object mapping to draw this field as a ColorPicker
    /// If it'a Vector3, Alpha will not be displayed
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ColorPicker : Attribute
    {
    }

    /// <summary>
    /// TEXT INPUT : STRING ONLY
    /// Force Figui Object mapping to draw this field as a text input area
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class Text : Attribute
    {
        public string Hint = "";
        public float Height = -1f;
        public int Lenght = 4096;
        public Text(string hint, float height, int lenght)
        {
            Hint = hint;
            Height = height;
            Lenght = lenght;
        }
    }

    /// <summary>
    /// DRAG : BYTE, SHORT, USHORT, INT, UINT, LONG, ULONG, FLOAT
    /// Force Figui Object mapping to draw this field as a Slider
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class Slider : Attribute
    {
        public float Min;
        public float Max;
        public string[] Labels;

        public Slider(byte min, byte max)
        {
            Min = min;
            Max = max;
        }
        public Slider(short min, short max)
        {
            Min = min;
            Max = max;
        }
        public Slider(ushort min, ushort max)
        {
            Min = min;
            Max = max;
        }
        public Slider(int min, int max)
        {
            Min = min;
            Max = max;
        }
        public Slider(uint min, uint max)
        {
            Min = min;
            Max = max;
        }
        public Slider(long min, long max)
        {
            Min = min;
            Max = max;
        }
        public Slider(ulong min, ulong max)
        {
            Min = min;
            Max = max;
        }
        public Slider(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }

    /// <summary>
    /// DRAG : BYTE, SHORT, USHORT, INT, UINT, LONG, ULONG, FLOAT, VECTOR2, VECTOR3, VECTOR4
    /// Force Figui Object mapping to draw this field as an integer Draggable input
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class Drag : Attribute
    {
        public float Min;
        public float Max;
        public string[] Labels;

        public Drag(byte min, byte max, params string[] labels)
        {
            Min = min;
            Max = max;
            Labels = labels;
        }
        public Drag(short min, short max, params string[] labels)
        {
            Min = min;
            Max = max;
            Labels = labels;
        }
        public Drag(ushort min, ushort max, params string[] labels)
        {
            Min = min;
            Max = max;
            Labels = labels;
        }
        public Drag(int min, int max, params string[] labels)
        {
            Min = min;
            Max = max;
            Labels = labels;
        }
        public Drag(uint min, uint max, params string[] labels)
        {
            Min = min;
            Max = max;
            Labels = labels;
        }
        public Drag(long min, long max, params string[] labels)
        {
            Min = min;
            Max = max;
            Labels = labels;
        }
        public Drag(ulong min, ulong max, params string[] labels)
        {
            Min = min;
            Max = max;
            Labels = labels;
        }
        public Drag(float min, float max, params string[] labels)
        {
            Min = min;
            Max = max;
            Labels = labels;
        }
    }
}