using System.Reflection;
using UnityEngine;

namespace Fu.Framework
{
        /// <summary>
        /// Represents the Fu Color Picker Field type.
        /// </summary>
        public class FuColorPickerField : FuField
        {
            #region State
            bool alpha = false;
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Fu Color Picker Field class.
            /// </summary>
            /// <param name="fieldInfo">The field Info value.</param>
            public FuColorPickerField(FieldInfo fieldInfo) : base(fieldInfo)
            {
                if (_fieldInfo.FieldType == typeof(Vector4) || _fieldInfo.FieldType == typeof(Color))
                {
                    alpha = true;
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
                if (_fieldInfo.FieldType == typeof(Color))
                {
                    Color color = (Color)_fieldInfo.GetValue(objectInstance);
                    Vector4 value = new Vector4(color.r, color.g, color.b, color.a);
                    updated = grid.ColorPicker(FieldName + "##" + objectID, ref value);
                    if (updated)
                    {
                        _fieldInfo.SetValue(objectInstance, new Color(value.x, value.y, value.z, value.w));
                    }
                }
                else if (alpha)
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
            #endregion
        }
}
