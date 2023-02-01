using UnityEngine;

namespace Fu.Framework
{
    public partial class FuGrid
    {
        /// <summary>
        /// Display and immage
        /// </summary>
        /// <param name="id">ID/Label of the image</param>
        /// <param name="texture">Texture to draw</param>
        /// <param name="size">Size of the image</param>
        public override void Image(string id, Texture2D texture, Vector2 size, Vector4 color)
        {
            if (!_gridCreated)
            {
                return;
            }
            drawElementLabel(id, FuTextStyle.Default);
            base.Image(id, texture, size, color);
            endElement();
        }

        /// <summary>
        /// Display and immage
        /// </summary>
        /// <param name="id">ID/Label of the image</param>
        /// <param name="texture">Texture to draw</param>
        /// <param name="size">Size of the image</param>
        public override void Image(string id, RenderTexture texture, Vector2 size, Vector4 color)
        {
            if (!_gridCreated)
            {
                return;
            }
            drawElementLabel(id, FuTextStyle.Default);
            base.Image(id, texture, size, color);
            endElement();
        }

        /// <summary>
        /// Draw a clickable image (button)
        /// </summary>
        /// <param name="id">ID/Label of the image</param>
        /// <param name="texture">Texture to draw</param>
        /// <param name="size">Size of the image</param>
        /// <returns>true if clicked</returns>
        public override bool ImageButton(string id, Texture2D texture, Vector2 size)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(id, FuTextStyle.Default);
            bool clicked = base.ImageButton(id, texture, size);
            endElement();
            return clicked;
        }

        /// <summary>
        /// Draw a clickable image (button)
        /// </summary>
        /// <param name="id">ID/Label of the image</param>
        /// <param name="texture">Texture to draw</param>
        /// <param name="size">Size of the image</param>
        /// <returns>true if clicked</returns>
        public override bool ImageButton(string id, Texture2D texture, Vector2 size, Vector4 color)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(id, FuTextStyle.Default);
            bool clicked = base.ImageButton(id, texture, size);
            endElement();
            return clicked;
        }
    }
}