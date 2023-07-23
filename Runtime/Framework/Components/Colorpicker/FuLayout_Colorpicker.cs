using ImGuiNET;
using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework
{
    public partial class FuLayout
    {
        private static Dictionary<string, Vector4> _pickersColors = new Dictionary<string, Vector4>();
        private static Dictionary<string, bool> _pickersEditedStates = new Dictionary<string, bool>();
        //private static bool _colorPickerEdited = false;

        ///<summary>
        /// Displays a color picker widget that allows the user to select a color in the RGB color space.
        ///</summary>
        ///<param name="text">A unique identifier for the color picker widget.</param>
        ///<param name="color">A reference to the vector3 containing the current color value. This value will be updated if the user changes the color.</param>
        public bool ColorPicker(string text, ref Vector4 color)
        {
            // Use the custom color picker function to display the widget and get the result
            return _customColorPicker(text, true, ref color, FuFrameStyle.Default);
        }

        ///<summary>
        /// Displays a color picker widget that allows the user to select a color in the RGBA color space.
        ///</summary>
        ///<param name="text">A unique identifier for the color picker widget.</param>
        ///<param name="color">A reference to the vector4 containing the current color value. This value will be updated if the user changes the color.</param>
        public bool ColorPicker(string text, ref Vector3 color)
        {
            // Convert the vector3 color value to a vector4 value
            Vector4 col = color;
            // Use the custom color picker function to display the widget and get the result
            if (_customColorPicker(text, false, ref col, FuFrameStyle.Default))
            {
                // If the color was edited, update the vector3 value with the new color
                color = (Vector3)col;
                // Return whether the color was edited
                return true;
            }
            return false;
        }

        ///<summary>
        /// Displays a color picker widget that allows the user to select a color in the RGBA color space.
        ///</summary>
        ///<param name="text">A unique identifier for the color picker widget.</param>
        ///<param name="color">A reference to the vector4 containing the current color value. This value will be updated if the user changes the color.</param>
        ///<param name="style">The style to apply to the color picker widget.</param>
        public bool ColorPicker(string text, ref Vector4 color, FuFrameStyle style)
        {
            // Use the custom color picker function to display the widget and get the result
            return _customColorPicker(text, true, ref color, style);
        }

        ///<summary>
        /// Displays a color picker widget that allows the user to select a color in the RGB color space.
        ///</summary>
        ///<param name="text">A unique identifier for the color picker widget.</param>
        ///<param name="color">A reference to the vector3 containing the current color value. This value will be updated if the user changes the color.</param>
        ///<param name="style">The style to apply to the color picker widget.</param>
        public bool ColorPicker(string text, ref Vector3 color, FuFrameStyle style)
        {
            // Convert the vector3 color value to a vector4 value
            Vector4 col = color;
            // Use the custom color picker function to display the widget and get the result
            if (_customColorPicker(text, false, ref col, style))
            {
                // If the color was edited, update the vector3 value with the new color
                color = (Vector3)col;
                // Return whether the color was edited
                return true;
            }
            return false;
        }

        /// <summary>
        /// Draw a custom Unity-Like Color Picker
        /// </summary>
        /// <param name="text">ID / Label of the color picker</param>
        /// <param name="alpha">did the color picker must draw alpha line and support alpha</param>
        /// <param name="color">reference of the color to display and edit</param>
        /// <param name="style">the FrameStyle to apply to the right-side input</param>
        /// <returns>true if value has been edited</returns>
        protected virtual bool _customColorPicker(string text, bool alpha, ref Vector4 color, FuFrameStyle style)
        {
            beginElement(ref text, style);
            // return if item must no be draw
            if (!_drawElement)
            {
                return false;
            }

            // register color so we can ref inside the popup lambda callback
            if (!_pickersColors.ContainsKey(text))
            {
                _pickersColors.Add(text, color);
            }
            _pickersColors[text] = color;
            // register edited states so we can predict Activation / Deractivation states from lambda callback
            if (!_pickersEditedStates.ContainsKey(text))
            {
                _pickersEditedStates.Add(text, false);
            }

            // set padding
            Fugui.Push(ImGuiStyleVar.WindowPadding, new Vector2(8f * Fugui.CurrentContext.Scale, 8f * Fugui.CurrentContext.Scale));

            float height = 18f * Fugui.CurrentContext.Scale;
            float width = ImGui.GetContentRegionAvail().x;
            float rounding = FuThemeManager.CurrentTheme.FrameRounding;
            var drawList = ImGui.GetWindowDrawList();

            Vector2 min = ImGui.GetCursorScreenPos();
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
                drawList.AddRectFilled(new Vector2(min.x, max.y - 4 * Fugui.CurrentContext.Scale), new Vector2(min.x + alphaWidth, max.y), ImGui.GetColorU32(Vector4.one), rounding, ImDrawFlags.RoundCornersBottomLeft);
                if (color.w < 1.0f)
                {
                    // draw alpha 0
                    drawList.AddRectFilled(new Vector2(min.x + alphaWidth, max.y - 4 * Fugui.CurrentContext.Scale), new Vector2(max.x, max.y), ImGui.GetColorU32(new Vector4(0, 0, 0, 1)), rounding, ImDrawFlags.RoundCornersBottomRight);
                }
            }

            // draw frame
            drawList.AddRect(min, max, ImGui.GetColorU32(FuThemeManager.GetColor(FuColors.Border)), rounding, ImDrawFlags.RoundCornersDefault, 1f);
            // fake draw the element
            ImGui.Dummy(max - min + Vector2.one * 2f);
            _elementHoverFramedEnabled = true;
            drawHoverFrame();
            _elementHoverFramedEnabled = false;

            // set states for this element
            setBaseElementState(text, min, max - min, true, false);
            displayToolTip(_lastItemHovered);

            // set mouse cursor
            if (_lastItemHovered && !LastItemDisabled)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            endElement(style);

            Vector2 size = new Vector2(404f * Fugui.CurrentContext.Scale, 224f * Fugui.CurrentContext.Scale);
            if (alpha)
            {
                size = new Vector2(404f * Fugui.CurrentContext.Scale, 212f * Fugui.CurrentContext.Scale);
            }

            string popupID = "ColorPicker" + text;
            if (_lastItemClickedButton == FuMouseButton.Left)
            {
                OpenPopUp(popupID, () =>
                {
                    // Draw the color picker
                    ImGui.SetNextItemWidth(184f * Fugui.CurrentContext.Scale);
                    Vector4 col = _pickersColors[text];
                    if (Fugui.Colorpicker("##picker" + text, ref col, false, true, alpha, alpha))
                    {
                        if (!_pickersEditedStates[text])
                        {
                            _pickersEditedStates[text] = true;
                            _lastItemJustActivated = true;
                        }
                        _pickersColors[text] = col;
                    }
                    else if (_pickersEditedStates[text] && !ImGui.IsMouseDown(ImGuiMouseButton.Left))
                    {
                        _pickersEditedStates[text] = false;
                        _lastItemJustDeactivated = true;
                    }
                }, size);
            }
            DrawPopup(popupID);
            if (_pickersEditedStates[text])
            {
                color = _pickersColors[text];
            }

            Fugui.PopStyle();

            bool edited = _pickersEditedStates[text];
            return edited;
        }
    }
}