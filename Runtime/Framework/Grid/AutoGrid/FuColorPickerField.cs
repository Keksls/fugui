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
                if (_fieldInfo.FieldType == typeof(Vector4))
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
            #endregion
        }
}
