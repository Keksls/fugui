using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Fu.Framework
{
    public abstract class FuField
    {
        public string ToolTipText;
        public bool Disabled = false;
        public string FieldName;
        private protected FieldInfo _fieldInfo;

        public FuField(FieldInfo fieldInfo)
        {
            _fieldInfo = fieldInfo;
            FieldName = Fugui.AddSpacesBeforeUppercase(_fieldInfo.Name);
            Disabled = fieldInfo.IsDefined(typeof(FuDisabled));
            if (_fieldInfo.IsDefined(typeof(FuTooltip)))
            {
                ToolTipText = _fieldInfo.GetCustomAttribute<FuTooltip>().Text;
            }
            else
            {
                ToolTipText = string.Empty;
            }
        }

        public abstract bool Draw(string objectID, FuGrid grid, object objectInstance);
    }

    public class FuImageField : FuField
    {
        private Texture2D _texture;
        private Vector2 _size;
        private Vector4 _color;

        public FuImageField(FieldInfo fieldInfo) : base(fieldInfo)
        {
            _size = Vector2.zero;
            _color = Vector4.one;
            if (fieldInfo.IsDefined(typeof(FuImage)))
            {
                _size = fieldInfo.GetCustomAttribute<FuImage>().Size;
                _color = fieldInfo.GetCustomAttribute<FuImage>().Color;
            }
        }

        public override bool Draw(string objectID, FuGrid grid, object objectInstance)
        {
            if (_texture == null)
            {
                _texture = (Texture2D)_fieldInfo.GetValue(objectInstance);
                if (_size.x == 0f || _size.y == 0f)
                {
                    _size = new Vector2(_texture.width, _texture.height);
                }
            }
            if (!string.IsNullOrEmpty(ToolTipText))
            {
                grid.SetNextElementToolTipWithLabel(FieldName + " : " + ToolTipText, ToolTipText);
            }
            grid.Image(FieldName + "##" + objectID, _texture, _size, _color);
            return false;
        }
    }
    public class FuComboboxField : FuField
    {
        public FuComboboxField(FieldInfo fieldInfo) : base(fieldInfo)
        {
        }

        public override bool Draw(string objectID, FuGrid grid, object objectInstance)
        {
            if (Disabled)
            {
                grid.DisableNextElement();
            }
            if (!string.IsNullOrEmpty(ToolTipText))
            {
                grid.SetNextElementToolTipWithLabel(FieldName + " : " + ToolTipText, ToolTipText);
            }
            int value = (int)_fieldInfo.GetValue(objectInstance);
            List<string> Values = new List<string>();
            foreach (var Value in Enum.GetValues(_fieldInfo.FieldType))
            {
                Values.Add(Fugui.AddSpacesBeforeUppercase(Value.ToString()));
            }
            bool updated = false;
            grid.Combobox(FieldName + "##" + objectID, Values, (index) =>
            {
                string newValue = Values[index].Replace(" ", "");
                _fieldInfo.SetValue(objectInstance, Enum.Parse(_fieldInfo.FieldType, newValue));
                updated = true;
            }, () => { return _fieldInfo.GetValue(objectInstance).ToString(); });
            return updated;
        }
    }

    public class FuCheckboxField : FuField
    {
        public FuCheckboxField(FieldInfo fieldInfo) : base(fieldInfo)
        {
        }

        public override bool Draw(string objectID, FuGrid grid, object objectInstance)
        {
            if (Disabled)
            {
                grid.DisableNextElement();
            }
            if (!string.IsNullOrEmpty(ToolTipText))
            {
                grid.SetNextElementToolTipWithLabel(FieldName + " : " + ToolTipText, ToolTipText);
            }
            bool isChecked = (bool)_fieldInfo.GetValue(objectInstance);
            bool updated = grid.CheckBox(FieldName + "##" + objectID, ref isChecked);
            if (updated)
            {
                _fieldInfo.SetValue(objectInstance, isChecked);
            }
            return updated;
        }
    }

    public class FuToggleField : FuField
    {
        public FuToggleField(FieldInfo fieldInfo) : base(fieldInfo)
        {
        }

        public override bool Draw(string objectID, FuGrid grid, object objectInstance)
        {
            if (Disabled)
            {
                grid.DisableNextElement();
            }
            if (!string.IsNullOrEmpty(ToolTipText))
            {
                grid.SetNextElementToolTipWithLabel(FieldName + " : " + ToolTipText, ToolTipText);
            }
            bool isChecked = (bool)_fieldInfo.GetValue(objectInstance);
            bool updated = grid.Toggle(FieldName + "##" + objectID, ref isChecked);
            if (updated)
            {
                _fieldInfo.SetValue(objectInstance, isChecked);
            }
            return updated;
        }
    }

    public class FuNonEditableField : FuField
    {
        public FuNonEditableField(FieldInfo fieldInfo) : base(fieldInfo)
        {
        }

        public override bool Draw(string objectID, FuGrid grid, object objectInstance)
        {
            if (Disabled)
            {
                grid.DisableNextElement();
            }
            if (!string.IsNullOrEmpty(ToolTipText))
            {
                grid.SetNextElementToolTipWithLabel(FieldName + " : " + ToolTipText, ToolTipText);
            }
            grid.Text(FieldName + "##" + objectID);
            object value = _fieldInfo.GetValue(objectInstance);
            grid.Text(value.ToString());
            return false;
        }
    }

    public class FuTextField : FuField
    {
        string _hint = "";
        float _height = -1f;
        uint _lenght = 4096;
        public FuTextField(FieldInfo fieldInfo) : base(fieldInfo)
        {
            if (fieldInfo.IsDefined(typeof(FuText), false))
            {
                FuText attribute = fieldInfo.GetCustomAttribute<FuText>(false);
                _hint = attribute.Hint;
                _height = attribute.Height;
                _lenght = (uint)attribute.Lenght;
            }
        }

        public override bool Draw(string objectID, FuGrid grid, object objectInstance)
        {
            if (Disabled)
            {
                grid.DisableNextElement();
            }
            if (!string.IsNullOrEmpty(ToolTipText))
            {
                grid.SetNextElementToolTipWithLabel(FieldName + " : " + ToolTipText, ToolTipText);
            }
            string value = (string)_fieldInfo.GetValue(objectInstance);
            bool updated = grid.TextInput(FieldName + "##" + objectID, _hint, ref value, _lenght, _height, 0f);
            if (updated)
            {
                _fieldInfo.SetValue(objectInstance, value);
            }
            return updated;
        }
    }

    public class FuSliderField : FuField
    {
        float _min;
        float _max;
        NumericFieldType _fieldType;

        public FuSliderField(FieldInfo fieldInfo) : base(fieldInfo)
        {
            _fieldType = FuObjectDescription.GetNumericFieldType(fieldInfo.FieldType);
            if (_fieldType == NumericFieldType.None)
            {
                return;
            }
            if (fieldInfo.IsDefined(typeof(FuSlider), false))
            {
                FuSlider attribute = fieldInfo.GetCustomAttribute<FuSlider>(false);
                _min = attribute.Min;
                _max = attribute.Max;
            }
            else
            {
                _min = 0;
                _max = 100;
            }
        }

        public override bool Draw(string objectID, FuGrid grid, object objectInstance)
        {
            if (Disabled)
            {
                grid.DisableNextElement();
            }
            if (!string.IsNullOrEmpty(ToolTipText))
            {
                grid.SetNextElementToolTipWithLabel(FieldName + " : " + ToolTipText, ToolTipText);
            }
            bool updated = false;
            switch (_fieldType)
            {
                case NumericFieldType.Byte:
                    int bval = (byte)_fieldInfo.GetValue(objectInstance);
                    updated = grid.Slider(FieldName + "##" + objectID, ref bval, (int)Mathf.Max(byte.MinValue, _min), (int)Mathf.Min(byte.MaxValue, _max));
                    if (updated)
                    {
                        _fieldInfo.SetValue(objectInstance, (byte)bval);
                    }
                    break;

                case NumericFieldType.Short:
                    int sval = (short)_fieldInfo.GetValue(objectInstance);
                    updated = grid.Slider(FieldName + "##" + objectID, ref sval, (int)Mathf.Max(short.MinValue, _min), (short)Mathf.Min(byte.MaxValue, _max));
                    if (updated)
                    {
                        _fieldInfo.SetValue(objectInstance, (short)sval);
                    }
                    break;

                case NumericFieldType.UShort:
                    int usval = (ushort)_fieldInfo.GetValue(objectInstance);
                    updated = grid.Slider(FieldName + "##" + objectID, ref usval, (int)Mathf.Max(ushort.MinValue, _min), (int)Mathf.Min(ushort.MaxValue, _max));
                    if (updated)
                    {
                        _fieldInfo.SetValue(objectInstance, (short)usval);
                    }
                    break;

                case NumericFieldType.Int:
                    int ival = (int)_fieldInfo.GetValue(objectInstance);
                    updated = grid.Slider(FieldName + "##" + objectID, ref ival, (int)Mathf.Max(int.MinValue, _min), (int)Mathf.Min(int.MaxValue, _max));
                    if (updated)
                    {
                        _fieldInfo.SetValue(objectInstance, ival);
                    }
                    break;

                case NumericFieldType.Float:
                    float fval = (float)_fieldInfo.GetValue(objectInstance);
                    updated = grid.Slider(FieldName + "##" + objectID, ref fval, (float)Mathf.Max(float.MinValue, _min), (float)Mathf.Min(float.MaxValue, _max));
                    if (updated)
                    {
                        _fieldInfo.SetValue(objectInstance, fval);
                    }
                    break;

                default:
                    grid.SetNextElementToolTipWithLabel("can't draw an object of type " + _fieldInfo.FieldType.ToString() + " using a slider.");
                    grid.Text(_fieldInfo.FieldType.ToString() + "##objectID");
                    return false;
            }
            return updated;
        }
    }

    public class FuDragField : FuField
    {
        float _min;
        float _max;
        NumericFieldType _fieldType;
        string[] _labels;

        public FuDragField(FieldInfo fieldInfo) : base(fieldInfo)
        {
            _fieldType = FuObjectDescription.GetNumericFieldType(fieldInfo.FieldType);
            if (_fieldType == NumericFieldType.None)
            {
                return;
            }

            if (fieldInfo.IsDefined(typeof(FuDrag), false))
            {
                FuDrag attribute = fieldInfo.GetCustomAttribute<FuDrag>(false);
                _min = attribute.Min;
                _max = attribute.Max;
                _labels = attribute.Labels;
            }
            else
            {
                _labels = null;
                _min = -100;
                _max = 100;
            }
        }

        private string getLabel(int index)
        {
            if (_labels == null || _labels.Length - 1 < index)
            {
                return null;
            }
            return _labels[index];
        }

        public override bool Draw(string objectID, FuGrid grid, object objectInstance)
        {
            if (Disabled)
            {
                grid.DisableNextElement();
            }
            if (!string.IsNullOrEmpty(ToolTipText))
            {
                grid.SetNextElementToolTipWithLabel(FieldName + " : " + ToolTipText, ToolTipText);
            }
            bool updated = false;
            switch (_fieldType)
            {
                case NumericFieldType.Byte:
                    int bval = (byte)_fieldInfo.GetValue(objectInstance);
                    updated = grid.Drag(FieldName + "##" + objectID, getLabel(0), ref bval, (int)Mathf.Max(byte.MinValue, _min), (int)Mathf.Min(byte.MaxValue, _max));
                    if (updated)
                    {
                        _fieldInfo.SetValue(objectInstance, (byte)bval);
                    }
                    break;

                case NumericFieldType.Short:
                    int sval = (short)_fieldInfo.GetValue(objectInstance);
                    updated = grid.Drag(FieldName + "##" + objectID, getLabel(0), ref sval, (int)Mathf.Max(short.MinValue, _min), (short)Mathf.Min(byte.MaxValue, _max));
                    if (updated)
                    {
                        _fieldInfo.SetValue(objectInstance, (short)sval);
                    }
                    break;

                case NumericFieldType.UShort:
                    int usval = (ushort)_fieldInfo.GetValue(objectInstance);
                    updated = grid.Drag(FieldName + "##" + objectID, getLabel(0), ref usval, (int)Mathf.Max(ushort.MinValue, _min), (int)Mathf.Min(ushort.MaxValue, _max));
                    if (updated)
                    {
                        _fieldInfo.SetValue(objectInstance, (short)usval);
                    }
                    break;

                case NumericFieldType.Int:
                    int ival = (int)_fieldInfo.GetValue(objectInstance);
                    updated = grid.Drag(FieldName + "##" + objectID, getLabel(0), ref ival, (int)Mathf.Max(int.MinValue, _min), (int)Mathf.Min(int.MaxValue, _max));
                    if (updated)
                    {
                        _fieldInfo.SetValue(objectInstance, ival);
                    }
                    break;

                case NumericFieldType.Float:
                    float fval = (float)_fieldInfo.GetValue(objectInstance);
                    updated = grid.Drag(FieldName + "##" + objectID, ref fval, getLabel(0), (float)Mathf.Max(float.MinValue, _min), (float)Mathf.Min(float.MaxValue, _max));
                    if (updated)
                    {
                        _fieldInfo.SetValue(objectInstance, fval);
                    }
                    break;

                case NumericFieldType.Vector2:
                    Vector2 v2val = (Vector2)_fieldInfo.GetValue(objectInstance);
                    updated = grid.Drag(FieldName + "##" + objectID, ref v2val, getLabel(0), getLabel(1), (float)Mathf.Max(float.MinValue, _min), (float)Mathf.Min(float.MaxValue, _max));
                    if (updated)
                    {
                        _fieldInfo.SetValue(objectInstance, v2val);
                    }
                    break;

                case NumericFieldType.Vector3:
                    Vector3 v3val = (Vector3)_fieldInfo.GetValue(objectInstance);
                    updated = grid.Drag(FieldName + "##" + objectID, ref v3val, getLabel(0), getLabel(1), getLabel(2), (float)Mathf.Max(float.MinValue, _min), (float)Mathf.Min(float.MaxValue, _max));
                    if (updated)
                    {
                        _fieldInfo.SetValue(objectInstance, v3val);
                    }
                    break;

                case NumericFieldType.Vector4:
                    Vector4 v4val = (Vector4)_fieldInfo.GetValue(objectInstance);
                    updated = grid.Drag(FieldName + "##" + objectID, ref v4val, getLabel(0), getLabel(1), getLabel(2), getLabel(3), (float)Mathf.Max(float.MinValue, _min), (float)Mathf.Min(float.MaxValue, _max));
                    if (updated)
                    {
                        _fieldInfo.SetValue(objectInstance, v4val);
                    }
                    break;

                case NumericFieldType.None:
                default:
                    grid.SetNextElementToolTipWithLabel("can't draw an object of type " + _fieldInfo.FieldType.ToString() + " using a drag.");
                    grid.Text(_fieldInfo.FieldType.ToString() + "##" + objectID);
                    return false;
            }
            return updated;
        }
    }

    public class FuColorPickerField : FuField
    {
        bool alpha = false;

        public FuColorPickerField(FieldInfo fieldInfo) : base(fieldInfo)
        {
            if (_fieldInfo.FieldType == typeof(Vector4))
            {
                alpha = true;
            }
        }

        public override bool Draw(string objectID, FuGrid grid, object objectInstance)
        {
            if (Disabled)
            {
                grid.DisableNextElement();
            }
            if (!string.IsNullOrEmpty(ToolTipText))
            {
                grid.SetNextElementToolTipWithLabel(FieldName + " : " + ToolTipText, ToolTipText);
            }
            bool updated = false;
            if (alpha)
            {
                Vector4 value = (Vector4)_fieldInfo.GetValue(objectInstance);
                updated = grid.ColorPicker(FieldName + "##" + objectID, ref value);
                if (updated)
                {
                    _fieldInfo.SetValue(objectInstance, value);
                }
            }
            else
            {
                Vector3 value = (Vector3)_fieldInfo.GetValue(objectInstance);
                updated = grid.ColorPicker(FieldName + "##" + objectID, ref value);
                if (updated)
                {
                    _fieldInfo.SetValue(objectInstance, value);
                }
            }
            return updated;
        }
    }
}