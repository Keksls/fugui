﻿using UnityEngine;

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
        public override void Image(string id, Texture2D texture, FuElementSize size, Vector4 color)
        {
            if (!_gridCreated)
            {
                return;
            }
            drawElementLabel(id, FuTextStyle.Default);
            base.Image(id, texture, size, color);
        }

        /// <summary>
        /// Display and immage
        /// </summary>
        /// <param name="id">ID/Label of the image</param>
        /// <param name="texture">Texture to draw</param>
        /// <param name="size">Size of the image</param>
        public override void Image(string id, RenderTexture texture, FuElementSize size, Vector4 color)
        {
            if (!_gridCreated)
            {
                return;
            }
            drawElementLabel(id, FuTextStyle.Default);
            base.Image(id, texture, size, color);
        }

        /// <summary>
        /// Draw a clickable image (button)
        /// </summary>
        /// <param name="id">ID/Label of the image</param>
        /// <param name="texture">Texture to draw</param>
        /// <param name="size">Size of the image</param>
        /// <returns>true if clicked</returns>
        public override bool ImageButton(string id, Texture2D texture, FuElementSize size)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(id, FuTextStyle.Default);
            return base.ImageButton(id, texture, size);
        }

        /// <summary>
        /// Draw a clickable image (button)
        /// </summary>
        /// <param name="id">ID/Label of the image</param>
        /// <param name="texture">Texture to draw</param>
        /// <param name="size">Size of the image</param>
        /// <returns>true if clicked</returns>
        public override bool ImageButton(string id, Texture2D texture, FuElementSize size, Vector4 color)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(id, FuTextStyle.Default);
            return base.ImageButton(id, texture, size);
        }
    }
}