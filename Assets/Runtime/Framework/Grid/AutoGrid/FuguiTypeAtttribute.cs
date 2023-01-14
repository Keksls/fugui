using System;
using UnityEngine;

namespace Fugui.Framework
{
    // ================== LABEL
    [AttributeUsage(AttributeTargets.Field)]
    public class LabelAtttribute : Attribute
    {
        public string Label;
        public LabelAtttribute(string label)
        {
            Label = label;
        }
    }

    // ================== DISABLED
    [AttributeUsage(AttributeTargets.Field)]
    public class DisabledAtttribute : Attribute
    {
    }

    // ================== COMBOBOX
    [AttributeUsage(AttributeTargets.Field)]
    public class ComboboxAtttribute : Attribute
    {
    }

    // ================== CHECKBOX
    [AttributeUsage(AttributeTargets.Field)]
    public class CheckboxAtttribute : Attribute
    {
    }

    // ================== TOGGLE
    [AttributeUsage(AttributeTargets.Field)]
    public class ToggleAtttribute : Attribute
    {
    }

    // ================== SLIDER INT
    [AttributeUsage(AttributeTargets.Field)]
    public class SliderIntAtttribute : Attribute
    {
        public int Min;
        public int Max;
        public SliderIntAtttribute(int min, int max)
        {
            Min = min;
            Max = max;
        }
    }

    // ================== TEXT INPUT
    [AttributeUsage(AttributeTargets.Field)]
    public class TextAtttribute : Attribute
    {
        public string Hint = "";
        public float Height = -1f;
        public int Lenght = 4096;
        public TextAtttribute(string hint, float height, int lenght)
        {
            Hint = hint;
            Height = height;
            Lenght = lenght;
        }
    }

    // ================== SLIDER FLOAT
    [AttributeUsage(AttributeTargets.Field)]
    public class SliderFloatAtttribute : Attribute
    {
        public float Min;
        public float Max;
        public SliderFloatAtttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }

    // ================== DRAG FLOAT
    [AttributeUsage(AttributeTargets.Field)]
    public class DragFloatAtttribute : Attribute
    {
        public float Min;
        public float Max;
        public DragFloatAtttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }

    // ================== DRAG FLOAT
    [AttributeUsage(AttributeTargets.Field)]
    public class DragIntAtttribute : Attribute
    {
        public int Min;
        public int Max;
        public DragIntAtttribute(int min, int max)
        {
            Min = min;
            Max = max;
        }
    }

    // ================== DRAG VECTOR2
    [AttributeUsage(AttributeTargets.Field)]
    public class DragVector2Atttribute : Attribute
    {
        public float Min;
        public float Max;
        public DragVector2Atttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }

    // ================== DRAG VECTOR3
    [AttributeUsage(AttributeTargets.Field)]
    public class DragVector3Atttribute : Attribute
    {
        public float Min;
        public float Max;
        public DragVector3Atttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }

    // ================== DRAG VECTOR4
    [AttributeUsage(AttributeTargets.Field)]
    public class DragVector4Atttribute : Attribute
    {
        public float Min;
        public float Max;
        public DragVector4Atttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }

    // ================== COLORPICKER
    [AttributeUsage(AttributeTargets.Field)]
    public class ColorPickerAtttribute : Attribute
    {
    }
}