using Fugui.Core;
using UnityEngine;

namespace Fugui.Framework
{
    public partial class UILayout
    {
        /// <summary>
        /// Draw an image
        /// </summary>
        /// <param name="id">ID/Label of the Image</param>
        /// <param name="texture">Texture2D to draw</param>
        /// <param name="size">size of the image</param>
        public virtual void Image(string id, Texture2D texture, Vector2 size)
        {
            UIWindow.CurrentDrawingWindow?.Container.ImGuiImage(texture, size);
            displayToolTip();
        }

        /// <summary>
        /// Draw a RenderTexture
        /// </summary>
        /// <param name="id">ID/Label of the RenderTexture</param>
        /// <param name="texture">RenderTexture to draw</param>
        /// <param name="size">size of the RenderTexture</param>
        public virtual void Image(string id, RenderTexture texture, Vector2 size)
        {
            UIWindow.CurrentDrawingWindow?.Container.ImGuiImage(texture, size);
            displayToolTip();
        }

        /// <summary>
        /// Draw an image button (clickable image)
        /// </summary>
        /// <param name="id">ID/Label of the Image</param>
        /// <param name="texture">Texture2D to draw</param>
        /// <param name="size">size of the image</param>
        /// <returns>true if clicked</returns>
        public virtual bool ImageButton(string id, Texture2D texture, Vector2 size)
        {
            bool clicked = UIWindow.CurrentDrawingWindow?.Container.ImGuiImageButton(texture, size) ?? false;
            displayToolTip();
            return clicked;
        }

        /// <summary>
        /// Draw an image button (clickable image)
        /// </summary>
        /// <param name="id">ID/Label of the Image</param>
        /// <param name="texture">Texture2D to draw</param>
        /// <param name="size">size of the image</param>
        /// <param name="color">tint color of the button</param>
        /// <returns>true if clicked</returns>
        public virtual bool ImageButton(string id, Texture2D texture, Vector2 size, Vector4 color)
        {
            bool clicked = UIWindow.CurrentDrawingWindow?.Container.ImGuiImageButton(texture, size, color) ?? false;
            displayToolTip();
            return clicked;
        }
    }
}