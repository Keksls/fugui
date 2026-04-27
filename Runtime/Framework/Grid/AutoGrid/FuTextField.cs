
using System.Reflection;

namespace Fu.Framework
{
        /// <summary>
        /// Represents the Fu Text Field type.
        /// </summary>
        public class FuTextField : FuField
        {
            #region State
            string _hint = "";
            float _height = -1f;
            uint _lenght = 4096;
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Fu Text Field class.
            /// </summary>
            /// <param name="fieldInfo">The field Info value.</param>
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
                string value = (string)_fieldInfo.GetValue(objectInstance);
                bool updated = grid.TextInput(FieldName + "##" + objectID, _hint, ref value, _lenght, _height, 0f);
                if (updated)
                {
                    _fieldInfo.SetValue(objectInstance, value);
                }
                return updated;
            }
            #endregion
        }
}
