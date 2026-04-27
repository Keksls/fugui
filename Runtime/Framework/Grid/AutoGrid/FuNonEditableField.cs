
using System.Reflection;

namespace Fu.Framework
{
        /// <summary>
        /// Represents the Fu Non Editable Field type.
        /// </summary>
        public class FuNonEditableField : FuField
        {
            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Fu Non Editable Field class.
            /// </summary>
            /// <param name="fieldInfo">The field Info value.</param>
            public FuNonEditableField(FieldInfo fieldInfo) : base(fieldInfo)
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
                grid.Text(FieldName + "##" + objectID);
                object value = _fieldInfo.GetValue(objectInstance);
                grid.Text(value.ToString());
                return false;
            }
            #endregion
        }
}
