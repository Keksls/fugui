using System;
using System.Collections.Generic;
using System.Reflection;

namespace Fu.Framework
{
        /// <summary>
        /// Represents the Fu Combobox Field type.
        /// </summary>
        public class FuComboboxField : FuField
        {
            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Fu Combobox Field class.
            /// </summary>
            /// <param name="fieldInfo">The field Info value.</param>
            public FuComboboxField(FieldInfo fieldInfo) : base(fieldInfo)
            {
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
            #endregion
        }
}
