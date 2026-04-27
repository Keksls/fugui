using UnityEngine;
using UTexture = UnityEngine.Texture;

namespace Fu
{
        /// <summary>
        /// Represents the Sprite Info type.
        /// </summary>
        internal sealed class SpriteInfo
        {
            #region State
            public UTexture Texture;
            public Vector2 Size;
            public Vector2 UV0;
            public Vector2 UV1;
            #endregion
        }
}
