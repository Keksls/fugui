using Fu.Core;
using ImGuiNET;
using UnityEngine;

namespace Fu.Framework
{
    public partial class FuLayout
    {
        ///<summary>
        /// Displays a color picker widget that allows the user to select a color in the RGB color space.
        ///</summary>
        ///<param name="id">A unique identifier for the color picker widget.</param>
        ///<param name="color">A reference to the vector3 containing the current color value. This value will be updated if the user changes the color.</param>
        public bool ColorPicker(string id, ref Vector4 color)
        {
            return ColorPicker(id, ref color, FuFrameStyle.Default);
        }

        ///<summary>
        /// Displays a color picker widget that allows the user to select a color in the RGBA color space.
        ///</summary>
        ///<param name="id">A unique identifier for the color picker widget.</param>
        ///<param name="color">A reference to the vector4 containing the current color value. This value will be updated if the user changes the color.</param>
        public bool ColorPicker(string id, ref Vector3 color)
        {
            return ColorPicker(id, ref color, FuFrameStyle.Default);
        }

        ///<summary>
        /// Displays a color picker widget that allows the user to select a color in the RGBA color space.
        ///</summary>
        ///<param name="id">A unique identifier for the color picker widget.</param>
        ///<param name="color">A reference to the vector4 containing the current color value. This value will be updated if the user changes the color.</param>
        ///<param name="style">The style to apply to the color picker widget.</param>
        public virtual bool ColorPicker(string id, ref Vector4 color, FuFrameStyle style)
        {
            // Use the custom color picker function to display the widget and get the result
            return _customColorPicker(id, true, ref color, FuFrameStyle.Default);
        }

        ///<summary>
        /// Displays a color picker widget that allows the user to select a color in the RGB color space.
        ///</summary>
        ///<param name="id">A unique identifier for the color picker widget.</param>
        ///<param name="color">A reference to the vector3 containing the current color value. This value will be updated if the user changes the color.</param>
        ///<param name="style">The style to apply to the color picker widget.</param>
        public virtual bool ColorPicker(string id, ref Vector3 color, FuFrameStyle style)
        {
            // Convert the vector3 color value to a vector4 value
            Vector4 col = color;
            // Use the custom color picker function to display the widget and get the result
            bool edited = _customColorPicker(id, false, ref col, FuFrameStyle.Default);
            // If the color was edited, update the vector3 value with the new color
            if (edited)
            {
                color = (Vector3)col;
            }
            // Return whether the color was edited
            return edited;
        }

        /// <summary>
        /// Draw a custom Unity-Like Color Picker
        /// </summary>
        /// <param name="id">ID / Label of the color picker</param>
        /// <param name="alpha">did the color picker must draw alpha line and support alpha</param>
        /// <param name="color">reference of the color to display and edit</param>
        /// <param name="style">the FrameStyle to apply to the right-side input</param>
        /// <returns>true if value has been edited</returns>
        private bool _customColorPicker(string id, bool alpha, ref Vector4 color, FuFrameStyle style)
        {
            bool edited = false;
            beginElement(ref id, style);
            // return if item must no be draw
            if (!_drawElement)
            {
                return false;
            }

            // set padding
            Fugui.Push(ImGuiStyleVar.WindowPadding, new Vector2(8f * Fugui.CurrentContext.Scale, 8f * Fugui.CurrentContext.Scale));

            float height = 18f * Fugui.CurrentContext.Scale;
            float width = ImGui.GetContentRegionAvail().x;
            float rounding = 0f;
            var drawList = ImGui.GetWindowDrawList();

            Vector2 min = ImGui.GetCursorScreenPos();
            min.x += 2 * Fugui.CurrentContext.Scale;
            min.y += 2 * Fugui.CurrentContext.Scale;
            Vector2 max = min + new Vector2(width, height);
            max.x -= 2 * Fugui.CurrentContext.Scale;
            max.y -= 2 * Fugui.CurrentContext.Scale;
            // draw color part
            Vector4 alphalessColor = color;
            alphalessColor.w = 1f;
            drawList.AddRectFilled(min, new Vector2(max.x, max.y - (alpha ? 4 * Fugui.CurrentContext.Scale : 0)), ImGui.GetColorU32(alphalessColor), rounding);
            if (alpha)
            {
                // draw alpha 1
                float alphaWidth = color.w * (max.x - min.x);
                drawList.AddRectFilled(new Vector2(min.x, max.y - 4 * Fugui.CurrentContext.Scale), new Vector2(min.x + alphaWidth, max.y), ImGui.GetColorU32(Vector4.one), rounding);
                if (color.w < 1.0f)
                {
                    // draw alpha 0
                    drawList.AddRectFilled(new Vector2(min.x + alphaWidth, max.y - 4 * Fugui.CurrentContext.Scale), new Vector2(max.x, max.y), ImGui.GetColorU32(new Vector4(0, 0, 0, 1)), rounding);
                }
            }

            // draw frame
            drawList.AddRect(min, max, ImGui.GetColorU32(FuThemeManager.GetColor(FuColors.Border)), rounding, ImDrawFlags.None, 1f);
            // fake draw the element
            ImGui.Dummy(max - min + Vector2.one * 2f);
            _elementHoverFramed = true;
            drawHoverFrame();
            _elementHoverFramed = false;

            bool hovered = ImGui.IsMouseHoveringRect(min, max) && ImGui.IsWindowHovered();
            displayToolTip(hovered);
            if (hovered && ImGui.IsMouseClicked(0) && !_nextIsDisabled)
            {
                ImGui.OpenPopup("ColorPicker" + id);
            }

            if (alpha)
            {
                ImGui.SetNextWindowSize(new Vector2(256f * Fugui.CurrentContext.Scale, 202f * Fugui.CurrentContext.Scale));
            }
            else
            {
                ImGui.SetNextWindowSize(new Vector2(256f * Fugui.CurrentContext.Scale, 224f * Fugui.CurrentContext.Scale));
            }
            if (ImGui.BeginPopup("ColorPicker" + id))
            {
                // Draw the color picker
                ImGui.SetNextItemWidth(184f * Fugui.CurrentContext.Scale);
                edited = ImGui.ColorPicker4("##picker" + id, ref color, ImGuiColorEditFlags.DefaultOptions | ImGuiColorEditFlags.DisplayHex | (alpha ? ImGuiColorEditFlags.AlphaBar : ImGuiColorEditFlags.NoAlpha));
                if (CurrentPopUpID != "ColorPicker" + id)
                {
                    CurrentPopUpWindowID = FuWindow.CurrentDrawingWindow?.ID;
                    CurrentPopUpID = "ColorPicker" + id;
                }
                // Set CurrentPopUpRect to ImGui item rect
                CurrentPopUpRect = new Rect(ImGui.GetWindowPos(), ImGui.GetWindowSize());
                ImGui.EndPopup();
            }
            else if (CurrentPopUpID == "ColorPicker" + id)
            {
                CurrentPopUpWindowID = null;
                CurrentPopUpID = null;
            }
            Fugui.PopStyle();

            // set mouse cursor
            if (hovered && !_nextIsDisabled)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            endElement(style);
            return edited;
        }
    }
}