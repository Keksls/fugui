using System;
using System.Drawing;
using System.Numerics;

namespace Fu.Framework
{
    /// <summary>
    /// Force Figui Object mapping to ignore this field and do not draw it
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class FuHidden : Attribute
    {
    }

    /// <summary>
    /// Force Figui Object mapping to disable this field (draw, but not editable)
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class FuDisabled : Attribute
    {
    }

    /// <summary>
    /// TOOLTIP¨: ANY Field
    /// Force Figui Object mapping to add a custom tooltip on the FuElement
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class FuTooltip : Attribute
    {
        public string Text;

        public FuTooltip(string text)
        {
            Text = text;
        }
    }

    /// <summary>
    /// Force Figui Object mapping to use FuImage data according to this attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class FuImage : Attribute
    {
        public UnityEngine.Vector2 Size;
        public UnityEngine.Vector4 Color;

        public FuImage(float width = 32f, float height = 32f, float r = 1f, float g = 1f, float b = 1f, float a = 1f)
        {
            Size = new UnityEngine.Vector2(width, height);
            Color = new UnityEngine.Vector4(r, g, b, a);
        }
    }

    /// <summary>
    /// TOGGLE : BOOLEAN ONLY
    /// Force Figui Object mapping to draw this field as a toggle
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class FuToggle : Attribute
    {
    }

    /// <summary>
    /// COLORPICKER : VECTOR4 OR VECTOR3
    /// Force Figui Object mapping to draw this field as a ColorPicker
    /// If it'a Vector3, Alpha will not be displayed
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class FuColorPicker : Attribute
    {
    }

    /// <summary>
    /// TEXT INPUT : STRING ONLY
    /// Force Figui Object mapping to draw this field as a text input area
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class FuText : Attribute
    {
        public string Hint = string.Empty;
        public float Height = -1f;
        public int Lenght = 4096;
        public FuText(string hint, float height, int lenght)
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
    public class FuSlider : Attribute
    {
        public float Min;
        public float Max;
        public string[] Labels;

        public FuSlider(byte min, byte max)
        {
            Min = min;
            Max = max;
        }
        public FuSlider(short min, short max)
        {
            Min = min;
            Max = max;
        }
        public FuSlider(ushort min, ushort max)
        {
            Min = min;
            Max = max;
        }
        public FuSlider(int min, int max)
        {
            Min = min;
            Max = max;
        }
        public FuSlider(uint min, uint max)
        {
            Min = min;
            Max = max;
        }
        public FuSlider(long min, long max)
        {
            Min = min;
            Max = max;
        }
        public FuSlider(ulong min, ulong max)
        {
            Min = min;
            Max = max;
        }
        public FuSlider(float min, float max)
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
    public class FuDrag : Attribute
    {
        public float Min;
        public float Max;
        public string[] Labels;

        public FuDrag(byte min, byte max, params string[] labels)
        {
            Min = min;
            Max = max;
            Labels = labels;
        }
        public FuDrag(short min, short max, params string[] labels)
        {
            Min = min;
            Max = max;
            Labels = labels;
        }
        public FuDrag(ushort min, ushort max, params string[] labels)
        {
            Min = min;
            Max = max;
            Labels = labels;
        }
        public FuDrag(int min, int max, params string[] labels)
        {
            Min = min;
            Max = max;
            Labels = labels;
        }
        public FuDrag(uint min, uint max, params string[] labels)
        {
            Min = min;
            Max = max;
            Labels = labels;
        }
        public FuDrag(long min, long max, params string[] labels)
        {
            Min = min;
            Max = max;
            Labels = labels;
        }
        public FuDrag(ulong min, ulong max, params string[] labels)
        {
            Min = min;
            Max = max;
            Labels = labels;
        }
        public FuDrag(float min, float max, params string[] labels)
        {
            Min = min;
            Max = max;
            Labels = labels;
        }
    }
}