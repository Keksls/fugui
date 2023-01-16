using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Fugui.Framework
{
    public abstract class UIField
    {
        public bool Disabled = false;
        public string FieldName;
        protected FieldInfo _fieldInfo;

        public UIField(FieldInfo fieldInfo)
        {
            _fieldInfo = fieldInfo;
            FieldName = FuGui.AddSpacesBeforeUppercase(_fieldInfo.Name);
            Disabled = fieldInfo.IsDefined(typeof(Disabled));
        }

        public abstract bool Draw(UIGrid grid, object objectInstance);
    }

    public class ComboboxField : UIField
    {
        public ComboboxField(FieldInfo fieldInfo) : base(fieldInfo)
        {
        }

        public override bool Draw(UIGrid grid, object objectInstance)
        {
            if(Disabled)
            {
                grid.DisableNextElement();
            }
            int value = (int)_fieldInfo.GetValue(objectInstance);
            List<string> Values = new List<string>();
            foreach (var Value in Enum.GetValues(_fieldInfo.FieldType))
            {
                Values.Add(FuGui.AddSpacesBeforeUppercase(Value.ToString()));
            }
            bool updated = false;
            grid.Combobox(FieldName, Values, (newValue) =>
            {
                newValue = newValue.Replace(" ", "");
                _fieldInfo.SetValue(objectInstance, Enum.Parse(_fieldInfo.FieldType, newValue));
                updated = true;
            }, () => { return _fieldInfo.GetValue(objectInstance).ToString(); });
            return updated;
        }
    }

    public class CheckboxField : UIField
    {
        public CheckboxField(FieldInfo fieldInfo) : base(fieldInfo)
        {
        }

        public override bool Draw(UIGrid grid, object objectInstance)
        {
            if (Disabled)
            {
                grid.DisableNextElement();
            }
            bool isChecked = (bool)_fieldInfo.GetValue(objectInstance);
            bool updated = grid.CheckBox(FieldName, ref isChecked);
            if (updated)
            {
                _fieldInfo.SetValue(objectInstance, isChecked);
            }
            return updated;
        }
    }

    public class ToggleField : UIField
    {
        public ToggleField(FieldInfo fieldInfo) : base(fieldInfo)
        {
        }

        public override bool Draw(UIGrid grid, object objectInstance)
        {
            if (Disabled)
            {
                grid.DisableNextElement();
            }
            bool isChecked = (bool)_fieldInfo.GetValue(objectInstance);
            bool updated = grid.Toggle(FieldName, ref isChecked);
            if (updated)
            {
                _fieldInfo.SetValue(objectInstance, isChecked);
            }
            return updated;
        }
    }

    public class NonEditableField : UIField
    {
        public NonEditableField(FieldInfo fieldInfo) : base(fieldInfo)
        {
        }

        public override bool Draw(UIGrid grid, object objectInstance)
        {
            if (Disabled)
            {
                grid.DisableNextElement();
            }
            grid.Text(FieldName);
            object value = _fieldInfo.GetValue(objectInstance);
            grid.Text(value.ToString());
            return false;
        }
    }

    public class TextField : UIField
    {
        string _hint = "";
        float _height = -1f;
        uint _lenght = 4096;
        public TextField(FieldInfo fieldInfo) : base(fieldInfo)
        {
            if (fieldInfo.IsDefined(typeof(Text), false))
            {
                Text attribute = fieldInfo.GetCustomAttribute<Text>(false);
                _hint = attribute.Hint;
                _height = attribute.Height;
                _lenght = (uint)attribute.Lenght;
            }
        }

        public override bool Draw(UIGrid grid, object objectInstance)
        {
            if (Disabled)
            {
                grid.DisableNextElement();
            }
            string value = (string)_fieldInfo.GetValue(objectInstance);
            bool updated = grid.TextInput(FieldName, _hint, ref value, _lenght, _height);
            if (updated)
            {
                _fieldInfo.SetValue(objectInstance, value);
            }
            return updated;
        }
    }

    public class SliderField : UIField
    {
        float _min;
        float _max;
        NumericFieldType _fieldType;

        public SliderField(FieldInfo fieldInfo) : base(fieldInfo)
        {
            _fieldType = UIObjectDescription.GetNumericFieldType(fieldInfo.FieldType);
            if (_fieldType == NumericFieldType.None)
            {
                return;
            }
            if (fieldInfo.IsDefined(typeof(Slider), false))
            {
                Slider attribute = fieldInfo.GetCustomAttribute<Slider>(false);
                _min = attribute.Min;
                _max = attribute.Max;
            }
            else
            {
                _min = 0;
                _max = 100;
            }
        }

        public override bool Draw(UIGrid grid, object objectInstance)
        {
            if (Disabled)
            {
                grid.DisableNextElement();
            }
            bool updated = false;
            switch (_fieldType)
            {
                case NumericFieldType.Byte:
                    int bval = (byte)_fieldInfo.GetValue(objectInstance);
                    updated = grid.Slider(FieldName, ref bval, (int)Mathf.Max(byte.MinValue, _min), (int)Mathf.Min(byte.MaxValue, _max));
                    if (updated)
                    {
                        _fieldInfo.SetValue(objectInstance, (byte)bval);
                    }
                    break;

                case NumericFieldType.Short:
                    int sval = (short)_fieldInfo.GetValue(objectInstance);
                    updated = grid.Slider(FieldName, ref sval, (int)Mathf.Max(short.MinValue, _min), (short)Mathf.Min(byte.MaxValue, _max));
                    if (updated)
                    {
                        _fieldInfo.SetValue(objectInstance, (short)sval);
                    }
                    break;

                case NumericFieldType.UShort:
                    int usval = (ushort)_fieldInfo.GetValue(objectInstance);
                    updated = grid.Slider(FieldName, ref usval, (int)Mathf.Max(ushort.MinValue, _min), (int)Mathf.Min(ushort.MaxValue, _max));
                    if (updated)
                    {
                        _fieldInfo.SetValue(objectInstance, (short)usval);
                    }
                    break;

                case NumericFieldType.Int:
                    int ival = (int)_fieldInfo.GetValue(objectInstance);
                    updated = grid.Slider(FieldName, ref ival, (int)Mathf.Max(int.MinValue, _min), (int)Mathf.Min(int.MaxValue, _max));
                    if (updated)
                    {
                        _fieldInfo.SetValue(objectInstance, ival);
                    }
                    break;

                case NumericFieldType.Float:
                    float fval = (float)_fieldInfo.GetValue(objectInstance);
                    updated = grid.Slider(FieldName, ref fval, (float)Mathf.Max(float.MinValue, _min), (float)Mathf.Min(float.MaxValue, _max));
                    if (updated)
                    {
                        _fieldInfo.SetValue(objectInstance, fval);
                    }
                    break;

                default:
                    grid.SetNextElementToolTip("can't draw an object of type " + _fieldInfo.FieldType.ToString() + " using a slider.");
                    grid.Text(_fieldInfo.FieldType.ToString());
                    return false;
            }
            return updated;
        }
    }

    public class DragField : UIField
    {
        float _min;
        float _max;
        NumericFieldType _fieldType;
        string[] _labels;

        public DragField(FieldInfo fieldInfo) : base(fieldInfo)
        {
            _fieldType = UIObjectDescription.GetNumericFieldType(fieldInfo.FieldType);
            if (_fieldType == NumericFieldType.None)
            {
                return;
            }

            if (fieldInfo.IsDefined(typeof(Drag), false))
            {
                Drag attribute = fieldInfo.GetCustomAttribute<Drag>(false);
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

        public override bool Draw(UIGrid grid, object objectInstance)
        {
            if (Disabled)
            {
                grid.DisableNextElement();
            }
            bool updated = false;
            switch (_fieldType)
            {
                case NumericFieldType.Byte:
                    int bval = (byte)_fieldInfo.GetValue(objectInstance);
                    updated = grid.Drag(FieldName, getLabel(0), ref bval, (int)Mathf.Max(byte.MinValue, _min), (int)Mathf.Min(byte.MaxValue, _max));
                    if (updated)
                    {
                        _fieldInfo.SetValue(objectInstance, (byte)bval);
                    }
                    break;

                case NumericFieldType.Short:
                    int sval = (short)_fieldInfo.GetValue(objectInstance);
                    updated = grid.Drag(FieldName, getLabel(0), ref sval, (int)Mathf.Max(short.MinValue, _min), (short)Mathf.Min(byte.MaxValue, _max));
                    if (updated)
                    {
                        _fieldInfo.SetValue(objectInstance, (short)sval);
                    }
                    break;

                case NumericFieldType.UShort:
                    int usval = (ushort)_fieldInfo.GetValue(objectInstance);
                    updated = grid.Drag(FieldName, getLabel(0), ref usval, (int)Mathf.Max(ushort.MinValue, _min), (int)Mathf.Min(ushort.MaxValue, _max));
                    if (updated)
                    {
                        _fieldInfo.SetValue(objectInstance, (short)usval);
                    }
                    break;

                case NumericFieldType.Int:
                    int ival = (int)_fieldInfo.GetValue(objectInstance);
                    updated = grid.Drag(FieldName, getLabel(0), ref ival, (int)Mathf.Max(int.MinValue, _min), (int)Mathf.Min(int.MaxValue, _max));
                    if (updated)
                    {
                        _fieldInfo.SetValue(objectInstance, ival);
                    }
                    break;

                case NumericFieldType.Float:
                    float fval = (float)_fieldInfo.GetValue(objectInstance);
                    updated = grid.Drag(FieldName, ref fval, getLabel(0), (float)Mathf.Max(float.MinValue, _min), (float)Mathf.Min(float.MaxValue, _max));
                    if (updated)
                    {
                        _fieldInfo.SetValue(objectInstance, fval);
                    }
                    break;

                case NumericFieldType.Vector2:
                    Vector2 v2val = (Vector2)_fieldInfo.GetValue(objectInstance);
                    updated = grid.Drag(FieldName, ref v2val, getLabel(0), getLabel(1), (float)Mathf.Max(float.MinValue, _min), (float)Mathf.Min(float.MaxValue, _max));
                    if (updated)
                    {
                        _fieldInfo.SetValue(objectInstance, v2val);
                    }
                    break;

                case NumericFieldType.Vector3:
                    Vector3 v3val = (Vector3)_fieldInfo.GetValue(objectInstance);
                    updated = grid.Drag(FieldName, ref v3val, getLabel(0), getLabel(1), getLabel(2), (float)Mathf.Max(float.MinValue, _min), (float)Mathf.Min(float.MaxValue, _max));
                    if (updated)
                    {
                        _fieldInfo.SetValue(objectInstance, v3val);
                    }
                    break;

                case NumericFieldType.Vector4:
                    Vector4 v4val = (Vector4)_fieldInfo.GetValue(objectInstance);
                    updated = grid.Drag(FieldName, ref v4val, getLabel(0), getLabel(1), getLabel(2), getLabel(3), (float)Mathf.Max(float.MinValue, _min), (float)Mathf.Min(float.MaxValue, _max));
                    if (updated)
                    {
                        _fieldInfo.SetValue(objectInstance, v4val);
                    }
                    break;

                case NumericFieldType.None:
                default:
                    grid.SetNextElementToolTip("can't draw an object of type " + _fieldInfo.FieldType.ToString() + " using a drag.");
                    grid.Text(_fieldInfo.FieldType.ToString());
                    return false;
            }
            return updated;
        }
    }

    public class ColorPickerField : UIField
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
            if (Disabled)
            {
                grid.DisableNextElement();
            }
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