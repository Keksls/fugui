using ImGuiNET;

namespace Fu.Framework
{
    public partial class FuGrid
    {
        /// <summary>
        /// Display a Horizontaly centered text (centered according to minimum line height)
        /// </summary>
        /// <param name="text">text to draw</param>
        /// <param name="style">Text Style</param>
        public override void Text(string text, FuTextStyle style)
        {
            if (!_gridCreated)
            {
                return;
            }
            beginElement(ref text, style, true);
            // return if item must no be draw
            if (!_drawItem)
            {
                return;
            }

            // horizontaly center Label
            float textHeight = ImGui.CalcTextSize(text).y;
            if (textHeight < _minLineHeight)
            {
                float padding = (_minLineHeight - textHeight) / 2f;
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + padding - 1f);
            }
            // draw text
            ImGui.Text(text);
            // handle tooltip
            if (_currentToolTipsOnLabels)
            {
                displayToolTip();
            }
            endElement(style);
        }

        /// <summary>
        /// Display a wrapped auto lineBreak text
        /// </summary>
        /// <param name="text">text to draw</param>
        /// <param name="style">Text Style</param>
        public override void TextWrapped(string text, FuTextStyle style)
        {
            if (!_gridCreated)
            {
                return;
            }
            beginElement(ref text, style, true);
            // return if item must no be draw
            if (!_drawItem)
            {
                return;
            }

            // draw text
            ImGui.TextWrapped(text);
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
            if (!_drawItem)
            {
                return;
            }

            // horizontaly center Label
            float textHeight = ImGui.CalcTextSize(text).y;
            if (textHeight < _minLineHeight)
            {
                float padding = (_minLineHeight - textHeight) / 2f;
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + padding - 1f);
            }
            // draw text
            _customText(text);
            // handle tooltip
            if (_currentToolTipsOnLabels)
            {
                displayToolTip();
            }
            endElement(style);
        }
    }
}