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
        public bool Image(string text, Texture2D texture, FuElementSize size, bool addBorder = false)
        {
            return Image(text, texture, size, Vector4.one, addBorder);
        }

        /// <summary>
        /// Draw a RenderTexture
        /// </summary>
        /// <param name="text">ID/Label of the RenderTexture</param>
        /// <param name="texture">RenderTexture to draw</param>
        /// <param name="size">size of the RenderTexture</param>
        /// <param name="addBorder">if true, add a frame border around the image</param>
        public bool Image(string text, RenderTexture texture, FuElementSize size, bool addBorder = false)
        {
            return Image(text, texture, size, Vector4.one, addBorder);
        }

        /// <summary>
        /// Draw an image
        /// </summary>
        /// <param name="text">ID/Label of the Image</param>
        /// <param name="texture">Texture2D to draw</param>
        /// <param name="size">size of the image</param>
        /// <param name="color">color of the image</param>
        /// <param name="addBorder">if true, add a frame border around the image</param>
        public virtual bool Image(string text, Texture2D texture, FuElementSize size, Vector4 color, bool addBorder = false)
        {
            beginElement(ref text);
            // return if item must no be draw
            if (!_drawElement)
            {
                return false;
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
            setBaseElementState(text, _currentItemStartPos, ImGui.GetItemRectMax() - _currentItemStartPos, true, false);
            displayToolTip();
            if (LastItemHovered)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }
            if (addBorder)
            {
                drawBorderFrame(new Rect(_currentItemStartPos, ImGui.GetItemRectMax() - _currentItemStartPos), false);
                _elementHoverFramedEnabled = true;
            }
            endElement();
            return LastItemJustDeactivated && LastItemHovered;
        }

        /// <summary>
        /// Draw a RenderTexture
        /// </summary>
        /// <param name="text">ID/Label of the RenderTexture</param>
        /// <param name="texture">RenderTexture to draw</param>
        /// <param name="size">size of the RenderTexture</param>
        /// <param name="color">color of the image</param>
        /// <param name="addBorder">if true, add a frame border around the image</param>
        public virtual bool Image(string text, RenderTexture texture, FuElementSize size, Vector4 color, bool addBorder = false)
        {
            beginElement(ref text);
            // return if item must no be draw
            if (!_drawElement)
            {
                return false;
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
            setBaseElementState(text, _currentItemStartPos, ImGui.GetItemRectMax() - _currentItemStartPos, true, false);
            if (LastItemHovered)
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
            return LastItemJustDeactivated && LastItemHovered;
        }

        /// <summary>
        /// Draw an image button (clickable image)
        /// </summary>
        /// <param name="text">ID/Label of the Image</param>
        /// <param name="texture">Texture2D to draw</param>
        /// <param name="size">size of the image</param>
        /// <returns>true if clicked</returns>
        public virtual bool ImageButton(string text, Texture2D texture, FuElementSize size)
        {
            beginElement(ref text);
            // return if item must no be draw
            if (!_drawElement)
            {
                return false;
            }
            bool clicked = default;
            if (FuWindow.CurrentDrawingWindow == null)
            {
                clicked = Fugui.MainContainer.ImGuiImageButton(texture, size);
            }
            else
            {
                clicked = FuWindow.CurrentDrawingWindow.Container.ImGuiImageButton(texture, size);
            }
            // set states for this element
            setBaseElementState(text, _currentItemStartPos, ImGui.GetItemRectMax() - _currentItemStartPos, true, false);        
            displayToolTip();
            endElement();
            return clicked;
        }

        /// <summary>
        /// Draw an image button (clickable image)
        /// </summary>
        /// <param name="text">ID/Label of the Image</param>
        /// <param name="texture">Texture2D to draw</param>
        /// <param name="size">size of the image</param>
        /// <param name="color">tint color of the button</param>
        /// <returns>true if clicked</returns>
        public virtual bool ImageButton(string text, Texture2D texture, FuElementSize size, Vector4 color)
        {
            beginElement(ref text);
            // return if item must no be draw
            if (!_drawElement)
            {
                return false;
            }
            bool clicked = default;
            if (FuWindow.CurrentDrawingWindow == null)
            {
                clicked = Fugui.MainContainer.ImGuiImageButton(texture, size, color);
            }
            else
            {
                clicked = FuWindow.CurrentDrawingWindow.Container.ImGuiImageButton(texture, size, color);
            }
            // set states for this element
            setBaseElementState(text, _currentItemStartPos, ImGui.GetItemRectMax() - _currentItemStartPos, true, false);
            displayToolTip();
            endElement();
            return clicked;
        }
    }
}