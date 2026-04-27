using System.Reflection;
using UnityEngine;

namespace Fu.Framework
{
        /// <summary>
        /// Represents the Fu Slider Field type.
        /// </summary>
        public class FuSliderField : FuField
        {
            #region State
            float _min;
            float _max;
            NumericFieldType _fieldType;
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Fu Slider Field class.
            /// </summary>
            /// <param name="fieldInfo">The field Info value.</param>
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
            #endregion

            #region Methods
            /// <summary>
            /// Draws the value.
            /// </summary>
            /// <param name="objectID">The object ID value.</param>
            /// <param name="grid">The grid value.</param>
            /// <param name="objectInstance">The object Instance value.</param>
            /// <returns>The result of the operation.</returns>
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
            #endregion
        }
}
