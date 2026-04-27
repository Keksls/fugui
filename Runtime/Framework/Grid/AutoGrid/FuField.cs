
using System.Reflection;

namespace Fu.Framework
{
    /// <summary>
    /// Represents the Fu Field type.
    /// </summary>
    public abstract class FuField
    {
        #region State
        public string ToolTipText;
        public bool Disabled = false;
        public string FieldName;
        private protected FieldInfo _fieldInfo;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the Fu Field class.
        /// </summary>
        /// <param name="fieldInfo">The field Info value.</param>
        public FuField(FieldInfo fieldInfo)
        {
            _fieldInfo = fieldInfo;
            FieldName = Fugui.AddSpacesBeforeUppercase(_fieldInfo.Name);
            Disabled = fieldInfo.IsDefined(typeof(FuDisabled));
            if (_fieldInfo.IsDefined(typeof(FuTooltip)))
            {
                ToolTipText = _fieldInfo.GetCustomAttribute<FuTooltip>().Text;
            }
            else
            {
                ToolTipText = string.Empty;
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
        public abstract bool Draw(string objectID, FuGrid grid, object objectInstance);
        #endregion
    }
}
