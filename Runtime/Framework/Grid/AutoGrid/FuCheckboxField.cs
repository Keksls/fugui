
using System.Reflection;

namespace Fu.Framework
{
        /// <summary>
        /// Represents the Fu Checkbox Field type.
        /// </summary>
        public class FuCheckboxField : FuField
        {
            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Fu Checkbox Field class.
            /// </summary>
            /// <param name="fieldInfo">The field Info value.</param>
            public FuCheckboxField(FieldInfo fieldInfo) : base(fieldInfo)
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
                bool isChecked = (bool)_fieldInfo.GetValue(objectInstance);
                bool updated = grid.CheckBox(FieldName + "##" + objectID, ref isChecked);
                if (updated)
                {
                    _fieldInfo.SetValue(objectInstance, isChecked);
                }
                return updated;
            }
            #endregion
        }
}
