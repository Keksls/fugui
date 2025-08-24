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
        /// <param name="isClickable">make the image clickable and change the cursor if hovered</param>
        public override bool Image(string text, Texture2D texture, FuElementSize size, Vector4 color, bool addBorder = false, bool isClickable = true)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(text, FuTextStyle.Default);
            return base.Image(text, texture, size, color, addBorder, isClickable);
        }

        /// <summary>
        /// Display and immage
        /// </summary>
        /// <param name="text">ID/Label of the image</param>
        /// <param name="texture">Texture to draw</param>
        /// <param name="size">Size of the image</param>
        /// <param name="color">color of the image</param>
        /// <param name="addBorder">if true, add a frame border around the image</param>
        /// <param name="isClickable">make the image clickable and change the cursor if hovered</param>
        public override bool Image(string text, RenderTexture texture, FuElementSize size, Vector4 color, bool addBorder = false, bool isClickable = true)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(text, FuTextStyle.Default);
            return base.Image(text, texture, size, color, addBorder, isClickable);
        }

        /// <summary>
        /// Draw a clickable image (button)
        /// </summary>
        /// <param name="text">ID/Label of the image</param>
        /// <param name="texture">Texture to draw</param>
        /// <param name="size">Size of the image</param>
        /// <param name="imagePadding">padding of the image inside the button</param>
        /// <returns>true if clicked</returns>
        public override bool ImageButton(string text, Texture2D texture, FuElementSize size, Vector2 imagePadding)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(text, FuTextStyle.Default);
            return base.ImageButton(text, texture, size, imagePadding);
        }

        /// <summary>
        /// Draw a clickable image (button)
        /// </summary>
        /// <param name="text">ID/Label of the image</param>
        /// <param name="texture">Texture to draw</param>
        /// <param name="size">Size of the image</param>
        /// <param name="color">tint color of the texture</param>
        /// <param name="imagePadding">padding of the image inside the button</param>
        /// <param name="border">Whatever you want to draw borders</param>
        /// <returns>true if clicked</returns>
        public override bool ImageButton(string text, Texture2D texture, FuElementSize size, Vector4 color, Vector2 imagePadding, bool border)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(text, FuTextStyle.Default);
            return base.ImageButton(text, texture, size, color, imagePadding, border);
        }
    }
}