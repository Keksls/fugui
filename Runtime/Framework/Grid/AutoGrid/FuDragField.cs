using System.Reflection;
using UnityEngine;

namespace Fu.Framework
{
        /// <summary>
        /// Represents the Fu Drag Field type.
        /// </summary>
        public class FuDragField : FuField
        {
            #region State
            float _min;
            float _max;
            NumericFieldType _fieldType;
            string[] _labels;
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Fu Drag Field class.
            /// </summary>
            /// <param name="fieldInfo">The field Info value.</param>
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
            #endregion

            #region Methods
            /// <summary>
            /// Returns the get label result.
            /// </summary>
            /// <param name="index">The index value.</param>
            /// <returns>The result of the operation.</returns>
            private string getLabel(int index)
            {
                if (_labels == null || _labels.Length - 1 < index)
                {
                    return null;
                }
                return _labels[index];
            }

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
            #endregion
        }
}
