using ImGuiNET;
using UnityEngine;

namespace Fu.Framework
{
    public partial class FuGrid
    {
        /// <summary>
        /// Displays a text string with the specified style.
        /// </summary>
        /// <param name="text">The string to display.</param>
        /// <param name="style">The style to apply to the text.</param>
        /// <param name="size">size of the text</param>
        /// <param name="wrapping">However you want to wrapp the text</param>
        public override void Text(string text, FuTextStyle style, Vector2 size, FuTextWrapping wrapping = FuTextWrapping.None)
        {
            if (!_gridCreated)
            {
                return;
            }
            beginElement(ref text, style, true);
            // return if item must no be draw
            if (!_drawElement)
            {
                return;
            }

            // verticaly center Label
            float textHeight = ImGui.CalcTextSize(text).y;
            if (textHeight < _minLineHeight * Fugui.Scale)
            {
                float padding = ((_minLineHeight * Fugui.Scale) - textHeight) / 2f;
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + padding - 1f);
            }
            // draw text
            _text(text, wrapping, size);
            // set states for this element
            setBaseElementState(text, _currentItemStartPos, ImGui.GetItemRectSize(), false, false);
            // handle tooltip
            if (_currentToolTipsOnLabels)
            {
                displayToolTip();
            }
            endElement(style);
        }

        /// <summary>
        /// Display a Horizontaly centered Smart Text (tagged richtext)
        /// </summary>
        /// <param name="text">text to draw</param>
        /// <param name="style">Text Style</param>
        public override void SmartText(string text, FuTextStyle style)
        {
            if (!_gridCreated)
            {
                return;
            }
            beginElement(ref text, style);
            // return if item must no be draw
            if (!_drawElement)
            {
                return;
            }

            // horizontaly center Label
            float textHeight = ImGui.CalcTextSize(text).y;
            if (textHeight < _minLineHeight * Fugui.Scale)
            {
                float padding = ((_minLineHeight * Fugui.Scale) - textHeight) / 2f;
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + padding - 1f);
            }
            // draw text
            _customText(text);
            // set states for this element
            setBaseElementState(text, _currentItemStartPos, ImGui.GetItemRectMax() - _currentItemStartPos, true, false);
            // handle tooltip
            if (_currentToolTipsOnLabels)
            {
                displayToolTip();
            }
            endElement(style);
        }
    }
}