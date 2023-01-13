using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Fugui.Framework
{
    public abstract class UIAutoGridField
    {
        public string FieldName;
        protected FieldInfo _fieldInfo;

        public UIAutoGridField(FieldInfo fieldInfo)
        {
            _fieldInfo = fieldInfo;
            FieldName = addSpacesBeforeUppercase(_fieldInfo.Name);
        }

        public abstract bool Draw(UIGrid grid, object objectInstance);

        /// <summary>
        /// Adds spaces before uppercase letters in the input string.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>The input string with spaces added before uppercase letters.</returns>
        protected string addSpacesBeforeUppercase(string input)
        {
            // Use a regular expression to add spaces before uppercase letters, but ignore the first letter of the string
            return Regex.Replace(input, "(?<!^)([A-Z])", " $1");
        }
    }

    public class ComboboxField : UIAutoGridField
    {
        public ComboboxField(FieldInfo fieldInfo) : base(fieldInfo)
        {
        }

        public override bool Draw(UIGrid grid, object objectInstance)
        {
            int value = (int)_fieldInfo.GetValue(objectInstance);
            List<string> Values = new List<string>();
            foreach (var Value in Enum.GetValues(_fieldInfo.FieldType))
            {
                Values.Add(addSpacesBeforeUppercase(Value.ToString()));
            }
            bool updated = false;
            grid.Combobox(FieldName, Values, (newValue) =>
            {
                newValue = newValue.Replace(" ", "");
                _fieldInfo.SetValue(objectInstance, Enum.Parse(_fieldInfo.FieldType, newValue));
                updated = true;
            });
            return updated;
        }
    }

    public class CheckboxField : UIAutoGridField
    {
        public CheckboxField(FieldInfo fieldInfo) : base(fieldInfo)
        {
        }

        public override bool Draw(UIGrid grid, object objectInstance)
        {
            bool isChecked = (bool)_fieldInfo.GetValue(objectInstance);
            bool updated = grid.CheckBox(FieldName, ref isChecked);
            if (updated)
            {
                _fieldInfo.SetValue(objectInstance, isChecked);
            }
            return updated;
        }
    }

    public class ToggleField : UIAutoGridField
    {
        public ToggleField(FieldInfo fieldInfo) : base(fieldInfo)
        {
        }

        public override bool Draw(UIGrid grid, object objectInstance)
        {
            bool isChecked = (bool)_fieldInfo.GetValue(objectInstance);
            bool updated = grid.Toggle(FieldName, ref isChecked);
            if (updated)
            {
                _fieldInfo.SetValue(objectInstance, isChecked);
            }
            return updated;
        }
    }

    public class TextField : UIAutoGridField
    {
        string _hint = "";
        float _height = -1f;
        uint _lenght = 4096;
        public TextField(FieldInfo fieldInfo) : base(fieldInfo)
        {
            if (fieldInfo.IsDefined(typeof(TextAtttribute), false))
            {
                TextAtttribute attribute = fieldInfo.GetCustomAttribute<TextAtttribute>(false);
                _hint = attribute.Hint;
                _height = attribute.Height;
                _lenght = (uint)attribute.Lenght;
            }
        }

        public override bool Draw(UIGrid grid, object objectInstance)
        {
            string value = (string)_fieldInfo.GetValue(objectInstance);
            bool updated = grid.TextInput(FieldName, _hint, ref value, _lenght, _height);
            if (updated)
            {
                _fieldInfo.SetValue(objectInstance, value);
            }
            return updated;
        }
    }

    public class SliderIntField : UIAutoGridField
    {
        int _min;
        int _max;

        public SliderIntField(FieldInfo fieldInfo) : base(fieldInfo)
        {
            if (fieldInfo.IsDefined(typeof(SliderIntAtttribute), false))
            {
                SliderIntAtttribute attribute = fieldInfo.GetCustomAttribute<SliderIntAtttribute>(false);
                _min = attribute.Min;
                _max = attribute.Max;
            }
            else
            {
                _min = -100;
                _max = 100;
            }
        }

        public override bool Draw(UIGrid grid, object objectInstance)
        {
            int value = (int)_fieldInfo.GetValue(objectInstance);
            bool updated = grid.Slider(FieldName, ref value, _min, _max);
            if (updated)
            {
                _fieldInfo.SetValue(objectInstance, value);
            }
            return updated;
        }
    }

    public class SliderFloatField : UIAutoGridField
    {
        float _min;
        float _max;

        public SliderFloatField(FieldInfo fieldInfo) : base(fieldInfo)
        {
            if (fieldInfo.IsDefined(typeof(SliderFloatAtttribute), false))
            {
                SliderFloatAtttribute attribute = fieldInfo.GetCustomAttribute<SliderFloatAtttribute>(false);
                _min = attribute.Min;
                _max = attribute.Max;
            }
            else
            {
                _min = -100;
                _max = 100;
            }
        }

        public override bool Draw(UIGrid grid, object objectInstance)
        {
            float value = (float)_fieldInfo.GetValue(objectInstance);
            bool updated = grid.Slider(FieldName, ref value, _min, _max);
            if (updated)
            {
                _fieldInfo.SetValue(objectInstance, value);
            }
            return updated;
        }
    }

    public class DragIntField : UIAutoGridField
    {
        int _min;
        int _max;

        public DragIntField(FieldInfo fieldInfo) : base(fieldInfo)
        {
            if (fieldInfo.IsDefined(typeof(DragIntAtttribute), false))
            {
                DragIntAtttribute attribute = fieldInfo.GetCustomAttribute<DragIntAtttribute>(false);
                _min = attribute.Min;
                _max = attribute.Max;
            }
            else
            {
                _min = -100;
                _max = 100;
            }
        }

        public override bool Draw(UIGrid grid, object objectInstance)
        {
            int value = (int)_fieldInfo.GetValue(objectInstance);
            bool updated = grid.Drag(FieldName, "", ref value, _min, _max);
            if (updated)
            {
                _fieldInfo.SetValue(objectInstance, value);
            }
            return updated;
        }
    }

    public class DragFloatField : UIAutoGridField
    {
        float _min;
        float _max;

        public DragFloatField(FieldInfo fieldInfo) : base(fieldInfo)
        {
            if (fieldInfo.IsDefined(typeof(DragFloatAtttribute), false))
            {
                DragFloatAtttribute attribute = fieldInfo.GetCustomAttribute<DragFloatAtttribute>(false);
                _min = attribute.Min;
                _max = attribute.Max;
            }
            else
            {
                _min = -100;
                _max = 100;
            }
        }

        public override bool Draw(UIGrid grid, object objectInstance)
        {
            float value = (float)_fieldInfo.GetValue(objectInstance);
            bool updated = grid.Drag(FieldName, ref value, "", _min, _max);
            if (updated)
            {
                _fieldInfo.SetValue(objectInstance, value);
            }
            return updated;
        }
    }

    public class DragVector2Field : UIAutoGridField
    {
        float _min;
        float _max;

        public DragVector2Field(FieldInfo fieldInfo) : base(fieldInfo)
        {
            if (fieldInfo.IsDefined(typeof(DragVector2Atttribute), false))
            {
                DragVector2Atttribute attribute = fieldInfo.GetCustomAttribute<DragVector2Atttribute>(false);
                _min = attribute.Min;
                _max = attribute.Max;
            }
            else
            {
                _min = -100;
                _max = 100;
            }
        }

        public override bool Draw(UIGrid grid, object objectInstance)
        {
            Vector2 value = (Vector2)_fieldInfo.GetValue(objectInstance);
            bool updated = grid.Drag(FieldName, ref value, "", "", _min, _max);
            if (updated)
            {
                _fieldInfo.SetValue(objectInstance, value);
            }
            return updated;
        }
    }

    public class DragVector3Field : UIAutoGridField
    {
        float _min;
        float _max;

        public DragVector3Field(FieldInfo fieldInfo) : base(fieldInfo)
        {
            if (fieldInfo.IsDefined(typeof(DragVector3Atttribute), false))
            {
                DragVector3Atttribute attribute = fieldInfo.GetCustomAttribute<DragVector3Atttribute>(false);
                _min = attribute.Min;
                _max = attribute.Max;
            }
            else
            {
                _min = -100;
                _max = 100;
            }
        }

        public override bool Draw(UIGrid grid, object objectInstance)
        {
            Vector3 value = (Vector3)_fieldInfo.GetValue(objectInstance);
            bool updated = grid.Drag(FieldName, ref value, "", "", "", _min, _max);
            if (updated)
            {
                _fieldInfo.SetValue(objectInstance, value);
            }
            return updated;
        }
    }

    public class DragVector4Field : UIAutoGridField
    {
        float _min;
        float _max;

        public DragVector4Field(FieldInfo fieldInfo) : base(fieldInfo)
        {
            if (fieldInfo.IsDefined(typeof(DragVector4Atttribute), false))
            {
                DragVector4Atttribute attribute = fieldInfo.GetCustomAttribute<DragVector4Atttribute>(false);
                _min = attribute.Min;
                _max = attribute.Max;
            }
            else
            {
                _min = -100;
                _max = 100;
            }
        }

        public override bool Draw(UIGrid grid, object objectInstance)
        {
            Vector4 value = (Vector4)_fieldInfo.GetValue(objectInstance);
            bool updated = grid.Drag(FieldName, ref value, "", "", "", "", _min, _max);
            if (updated)
            {
                _fieldInfo.SetValue(objectInstance, value);
            }
            return updated;
        }
    }

    public class ColorPickerField : UIAutoGridField
    {
        bool alpha = false;

        public ColorPickerField(FieldInfo fieldInfo) : base(fieldInfo)
        {
            if (_fieldInfo.FieldType == typeof(Vector4))
            {
                alpha = true;
            }
        }

        public override bool Draw(UIGrid grid, object objectInstance)
        {
            bool updated = false;
            if (alpha)
            {
                Vector4 value = (Vector4)_fieldInfo.GetValue(objectInstance);
                updated = grid.ColorPicker(FieldName, ref value);
                if (updated)
                {
                    _fieldInfo.SetValue(objectInstance, value);
                }
            }
            else
            {
                Vector3 value = (Vector3)_fieldInfo.GetValue(objectInstance);
                updated = grid.ColorPicker(FieldName, ref value);
                if (updated)
                {
                    _fieldInfo.SetValue(objectInstance, value);
                }
            }
            return updated;
        }
    }
}