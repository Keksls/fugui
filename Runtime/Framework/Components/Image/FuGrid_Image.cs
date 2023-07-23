using UnityEngine;

namespace Fu.Framework
{
    public partial class FuGrid
    {
        /// <summary>
        /// Display and immage
        /// </summary>
        /// <param name="text">ID/Label of the image</param>
        /// <param name="texture">Texture to draw</param>
        /// <param name="size">Size of the image</param>
        /// <param name="color">color of the image</param>
        /// <param name="addBorder">if true, add a frame border around the image</param>
        public override bool Image(string text, Texture2D texture, FuElementSize size, Vector4 color, bool addBorder = false)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(text, FuTextStyle.Default);
            return base.Image(text, texture, size, color, addBorder);
        }

        /// <summary>
        /// Display and immage
        /// </summary>
        /// <param name="text">ID/Label of the image</param>
        /// <param name="texture">Texture to draw</param>
        /// <param name="size">Size of the image</param>
        /// <param name="color">color of the image</param>
        /// <param name="addBorder">if true, add a frame border around the image</param>
        public override bool Image(string text, RenderTexture texture, FuElementSize size, Vector4 color, bool addBorder = false)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(text, FuTextStyle.Default);
            return base.Image(text, texture, size, color, addBorder);
        }

        /// <summary>
        /// Draw a clickable image (button)
        /// </summary>
        /// <param name="text">ID/Label of the image</param>
        /// <param name="texture">Texture to draw</param>
        /// <param name="size">Size of the image</param>
        /// <returns>true if clicked</returns>
        public override bool ImageButton(string text, Texture2D texture, FuElementSize size)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(text, FuTextStyle.Default);
            return base.ImageButton(text, texture, size);
        }

        /// <summary>
        /// Draw a clickable image (button)
        /// </summary>
        /// <param name="text">ID/Label of the image</param>
        /// <param name="texture">Texture to draw</param>
        /// <param name="size">Size of the image</param>
        /// <returns>true if clicked</returns>
        public override bool ImageButton(string text, Texture2D texture, FuElementSize size, Vector4 color)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(text, FuTextStyle.Default);
            return base.ImageButton(text, texture, size, color);
        }
    }
}