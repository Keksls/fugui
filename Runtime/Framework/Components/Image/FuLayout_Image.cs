using Fu.Core;
using ImGuiNET;
using UnityEngine;

namespace Fu.Framework
{
    public partial class FuLayout
    {
        /// <summary>
        /// Draw an image
        /// </summary>
        /// <param name="text">ID/Label of the Image</param>
        /// <param name="texture">Texture2D to draw</param>
        /// <param name="size">size of the image</param>
        /// <param name="addBorder">if true, add a frame border around the image</param>
        /// <param name="isClickable">make the image clickable and change the cursor if hovered</param>
        public bool Image(string text, Texture2D texture, FuElementSize size, bool addBorder = false, bool isClickable = true)
        {
            return Image(text, texture, size, Vector4.one, addBorder, isClickable);
        }

        /// <summary>
        /// Draw a RenderTexture
        /// </summary>
        /// <param name="text">ID/Label of the RenderTexture</param>
        /// <param name="texture">RenderTexture to draw</param>
        /// <param name="size">size of the RenderTexture</param>
        /// <param name="addBorder">if true, add a frame border around the image</param>
        /// <param name="isClickable">make the image clickable and change the cursor if hovered</param>
        public bool Image(string text, RenderTexture texture, FuElementSize size, bool addBorder = false, bool isClickable = true)
        {
            return Image(text, texture, size, Vector4.one, addBorder, isClickable);
        }

        /// <summary>
        /// Draw an image
        /// </summary>
        /// <param name="text">ID/Label of the Image</param>
        /// <param name="texture">Texture2D to draw</param>
        /// <param name="size">size of the image</param>
        /// <param name="color">color of the image</param>
        /// <param name="addBorder">if true, add a frame border around the image</param>
        /// <param name="isClickable">make the image clickable and change the cursor if hovered</param>
        public virtual bool Image(string text, Texture2D texture, FuElementSize size, Vector4 color, bool addBorder = false, bool isClickable = true)
        {
            beginElement(ref text);
            // return if item must no be draw
            if (!_drawElement)
            {
                return false;
            }
            if (LastItemDisabled)
            {
                color *= 0.5f;
            }
            if (FuWindow.CurrentDrawingWindow == null)
            {
                Fugui.MainContainer.ImGuiImage(texture, size.GetSize(), color);
            }
            else
            {
                FuWindow.CurrentDrawingWindow.Container.ImGuiImage(texture, size.GetSize(), color);
            }

            // set states for this element
            setBaseElementState(text, ImGui.GetItemRectMin(), ImGui.GetItemRectSize(), isClickable, false);
            displayToolTip();
            if (_lastItemHovered && isClickable)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }
            if (addBorder)
            {
                _elementHoverFramedEnabled = true;
                drawBorderFrame(new Rect(ImGui.GetItemRectMin(), ImGui.GetItemRectSize()), false);
            }
            endElement();
            return _lastItemJustDeactivated && _lastItemHovered;
        }

        /// <summary>
        /// Draw a RenderTexture
        /// </summary>
        /// <param name="text">ID/Label of the RenderTexture</param>
        /// <param name="texture">RenderTexture to draw</param>
        /// <param name="size">size of the RenderTexture</param>
        /// <param name="color">color of the image</param>
        /// <param name="addBorder">if true, add a frame border around the image</param>
        /// <param name="isClickable">make the image clickable and change the cursor if hovered</param>
        public virtual bool Image(string text, RenderTexture texture, FuElementSize size, Vector4 color, bool addBorder = false, bool isClickable = true)
        {
            beginElement(ref text);
            // return if item must no be draw
            if (!_drawElement)
            {
                return false;
            }
            if (LastItemDisabled)
            {
                color *= 0.5f;
            }
            if (FuWindow.CurrentDrawingWindow == null)
            {
                Fugui.MainContainer.ImGuiImage(texture, size.GetSize(), color);
            }
            else
            {
                FuWindow.CurrentDrawingWindow.Container.ImGuiImage(texture, size.GetSize(), color);
            }
            // set states for this element
            setBaseElementState(text, _currentItemStartPos, ImGui.GetItemRectMax() - _currentItemStartPos, isClickable, false);
            if (_lastItemHovered && isClickable)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }
            if (addBorder)
            {
                drawBorderFrame(new Rect(_currentItemStartPos, ImGui.GetItemRectMax() - _currentItemStartPos), false);
                _elementHoverFramedEnabled = true;
            }
            displayToolTip();
            endElement();
            return _lastItemJustDeactivated && _lastItemHovered;
        }

        /// <summary>
        /// Draw an image button (clickable image)
        /// </summary>
        /// <param name="text">ID/Label of the Image</param>
        /// <param name="texture">Texture2D to draw</param>
        /// <param name="size">size of the image</param>
        /// <param name="color">tint color of the image</param>
        /// <param name="imagePadding">padding of the image inside the button</param>
        /// <param name="border">Whatever you want to draw borders</param>
        /// <returns>true if clicked</returns>
        public virtual bool ImageButton(string text, Texture2D texture, FuElementSize size, Vector4 color, Vector2 imagePadding, bool border)
        {
            return ImageButton(text, texture, size, color, imagePadding, border, FuButtonStyle.Default);
        }

        /// <summary>
        /// Draw an image button (clickable image)
        /// </summary>
        /// <param name="text">ID/Label of the Image</param>
        /// <param name="texture">Texture2D to draw</param>
        /// <param name="size">size of the image</param>
        /// <param name="color">tint color of the image</param>
        /// <param name="imagePadding">padding of the image inside the button</param>
        /// <param name="border">Whatever you want to draw borders</param>
        /// <param name="style">the style of the image button</param>
        /// <returns>true if clicked</returns>
        public virtual bool ImageButton(string text, Texture2D texture, FuElementSize size, Vector4 color, Vector2 imagePadding, bool border, FuButtonStyle style)
        {
            beginElement(ref text);
            // return if item must no be draw
            if (!_drawElement)
            {
                return false;
            }

            imagePadding *= Fugui.CurrentContext.Scale;
            Vector2 padding = FuThemeManager.FramePadding;
            padding.x = Mathf.Min(padding.x, padding.y);
            padding.y = padding.x;
            Vector2 btnSize = size.BrutSize;
            Vector2 imgSize = size.BrutSize;
            float imgRatio = 1f;

            // compute button size
            if (btnSize.x < 0)
            {
                btnSize.x = ImGui.GetContentRegionAvail().x;
            }
            else if (btnSize.x == 0)
            {
                if (btnSize.y > 0)
                {
                    imgRatio = texture.height / btnSize.y;
                }
                btnSize.x = Mathf.Min(texture.width * Fugui.CurrentContext.Scale * imgRatio, ImGui.GetContentRegionAvail().x);
            }
            else if (btnSize.x > 0)
            {
                btnSize.x = Mathf.Min(btnSize.x * Fugui.CurrentContext.Scale, ImGui.GetContentRegionAvail().x);
            }
            imgRatio = Mathf.Clamp01(btnSize.x / (texture.width * Fugui.CurrentContext.Scale));

            if (btnSize.y <= 0)
            {
                btnSize.y = texture.height * Fugui.CurrentContext.Scale * imgRatio;
            }
            else if (btnSize.y > 0)
            {
                btnSize.y *= Fugui.CurrentContext.Scale;
                if (size.BrutSize.x < 0)
                {
                    imgRatio = Mathf.Clamp01(btnSize.y / (texture.height * Fugui.CurrentContext.Scale));
                }
            }

            // compute image size
            imgSize.y = btnSize.y;
            imgSize.x = texture.width * imgRatio * Fugui.CurrentContext.Scale;

            Vector2 centerImageOffset = (btnSize - imgSize) / 2f;
            centerImageOffset.x = Mathf.Max(0f, centerImageOffset.x);
            centerImageOffset.y = Mathf.Max(0f, centerImageOffset.y);

            imgSize -= (padding * 2f);
            imgSize -= (imagePadding * 2f);

            bool clicked = _customButton("##imb" + text, btnSize, padding, Vector2.zero, style, FuThemeManager.CurrentTheme.ButtonsGradientStrenght, border);

            if (LastItemDisabled)
            {
                color *= 0.75f;
            }

            Vector2 btnPos = ImGui.GetItemRectMin();
            ImGui.SetCursorScreenPos(btnPos + padding + imagePadding + centerImageOffset);
            if (FuWindow.CurrentDrawingWindow == null)
            {
                Fugui.MainContainer.ImGuiImage(texture, imgSize, color);
            }
            else
            {
                FuWindow.CurrentDrawingWindow.Container.ImGuiImage(texture, imgSize, color);
            }
            ImGui.SetCursorScreenPos(btnPos);
            ImGui.Dummy(size);

            endElement();
            return clicked && !LastItemDisabled;
        }

        /// <summary>
        /// Draw an image button (clickable image)
        /// </summary>
        /// <param name="text">ID/Label of the Image</param>
        /// <param name="texture">Texture2D to draw</param>
        /// <param name="size">size of the image</param>
        /// <param name="imagePadding">padding of the image inside the button</param>
        /// <returns>true if clicked</returns>
        public virtual bool ImageButton(string text, Texture2D texture, FuElementSize size, Vector2 imagePadding)
        {
            return ImageButton(text, texture, size, Color.white, imagePadding, true);
        }
    }
}