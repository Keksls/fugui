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
            #region State
            private readonly List<string> _displayValues;
            private readonly List<string> _enumValues;
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Fu Combobox Field class.
            /// </summary>
            /// <param name="fieldInfo">The field Info value.</param>
            public FuComboboxField(FieldInfo fieldInfo) : base(fieldInfo)
            {
                Array values = Enum.GetValues(fieldInfo.FieldType);
                _displayValues = new List<string>(values.Length);
                _enumValues = new List<string>(values.Length);
                foreach (object value in values)
                {
                    string enumValue = value.ToString();
                    _enumValues.Add(enumValue);
                    _displayValues.Add(Fugui.AddSpacesBeforeUppercase(enumValue));
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
                grid.Combobox(FieldName + "##" + objectID, _displayValues, (index) =>
                {
                    _fieldInfo.SetValue(objectInstance, Enum.Parse(_fieldInfo.FieldType, _enumValues[index]));
                    updated = true;
                }, () => { return Fugui.AddSpacesBeforeUppercase(_fieldInfo.GetValue(objectInstance).ToString()); }, () => false);
                return updated;
            }
            #endregion
        }
}
