using System.Reflection;
using UnityEngine;

namespace Fu.Framework
{
        /// <summary>
        /// Represents the Fu Image Field type.
        /// </summary>
        public class FuImageField : FuField
        {
            #region State
            private Texture2D _texture;
            private Vector2 _size;
            private Vector4 _color;
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Fu Image Field class.
            /// </summary>
            /// <param name="fieldInfo">The field Info value.</param>
            public FuImageField(FieldInfo fieldInfo) : base(fieldInfo)
            {
                _size = Vector2.zero;
                _color = Vector4.one;
                if (fieldInfo.IsDefined(typeof(FuImage)))
                {
                    _size = fieldInfo.GetCustomAttribute<FuImage>().Size;
                    _color = fieldInfo.GetCustomAttribute<FuImage>().Color;
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
                if (_texture == null)
                {
                    _texture = (Texture2D)_fieldInfo.GetValue(objectInstance);
                    if (_size.x == 0f || _size.y == 0f)
                    {
                        _size = new Vector2(_texture.width, _texture.height);
                    }
                }
                if (!string.IsNullOrEmpty(ToolTipText))
                {
                    grid.SetNextElementToolTipWithLabel(FieldName + " : " + ToolTipText, ToolTipText);
                }
                grid.Image(FieldName + "##" + objectID, _texture, _size, _color);
                return false;
            }
            #endregion
        }
}
