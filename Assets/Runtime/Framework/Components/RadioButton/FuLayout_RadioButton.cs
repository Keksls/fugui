using ImGuiNET;
using UnityEngine;

namespace Fu.Framework
{
    public partial class FuLayout
    {
        /// <summary>
        /// Renders a Radio Button with the given text and returns true if the checkbox was clicked. The value of the checkbox is stored in the provided boolean variable.
        /// </summary>
        /// <param name="text">Text to display next to the checkbox</param>
        /// <param name="isChecked">Boolean variable to store the value of the checkbox</param>
        /// <returns>True if the checkbox was clicked, false otherwise</returns>
        public bool RadioButton(string text, bool isChecked)
        {
            return RadioButton(text, isChecked, FuFrameStyle.Default);
        }

        /// <summary>
        /// Renders a Radio Button with the given text and returns true if the checkbox was clicked. The value of the checkbox is stored in the provided boolean variable.
        /// </summary>
        /// <param name="text">Text to display next to the checkbox</param>
        /// <param name="isChecked">Boolean variable to store the value of the checkbox</param>
        /// <param name="style">Style to use for the checkbox</param>
        /// <returns>True if the checkbox was clicked, false otherwise</returns>
        public virtual bool RadioButton(string text, bool isChecked, FuFrameStyle style)
        {
            string id = beginElement(text, style); // Push the style for the checkbox element
            text = text.Split(new char[] { '#', '#' })[0];
            // get or create animation data
            if (!_uiElementAnimationDatas.ContainsKey(id))
            {
                _uiElementAnimationDatas.Add(id, new FuElementAnimationData(!isChecked));
            }
            FuElementAnimationData animationData = _uiElementAnimationDatas[id];

            // layout states
            float height = 18f * Fugui.CurrentContext.Scale;
            Vector2 pos = ImGui.GetCursorScreenPos();
            Vector2 CircleCenter = new Vector2(pos.x + height / 2f + 2f * Fugui.CurrentContext.Scale, pos.y + height / 2f + 2f * Fugui.CurrentContext.Scale);
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            // input stats
            bool hovered = ImGui.IsMouseHoveringRect(pos, pos + new Vector2(height + 4f * Fugui.CurrentContext.Scale, height));
            bool active = hovered && ImGui.IsMouseDown(0);
            bool clicked = hovered && ImGui.IsMouseReleased(0);
            // frame colors
            Vector4 BGColor = default;
            Vector4 knobColor = default;
            if (_nextIsDisabled)
            {
                BGColor = style.DisabledFrame;
                knobColor = FuThemeManager.GetColor(FuColors.Knob) * 0.3f;
            }
            else
            {
                BGColor = style.CheckMark;
                knobColor = FuThemeManager.GetColor(FuColors.Knob);
                if (active)
                {
                    BGColor = style.CheckMark * 0.8f;
                    knobColor = FuThemeManager.GetColor(FuColors.KnobActive);
                }
                else if (hovered)
                {
                    BGColor = style.CheckMark * 0.9f;
                    knobColor = FuThemeManager.GetColor(FuColors.KnobHovered);
                }
            }

            // draw radio button
            drawList.AddCircleFilled(CircleCenter, height / 2f, ImGui.GetColorU32(!isChecked ? FuThemeManager.GetColor(FuColors.FrameBg) : BGColor), 64);
            if (animationData.CurrentValue > 0f)
            {
                float knobSize = Mathf.Lerp(0f, height / 5f, animationData.CurrentValue);
                drawList.AddCircleFilled(CircleCenter, knobSize, ImGui.GetColorU32(knobColor), 64);
            }
            else
            {
                drawList.AddCircle(CircleCenter, height / 2f, ImGui.GetColorU32(style.Border), 64);
            }

            //draw hover frame
            if (hovered && !_nextIsDisabled)
            {
                drawList.AddCircle(CircleCenter, height / 2f, ImGui.GetColorU32(FuThemeManager.GetColor(FuColors.FrameHoverFeedback)), 64, 1f);
            }

            // update animation data
            animationData.Update(isChecked, _animationEnabled);

            // dummy display button
            ImGui.Dummy(new Vector2(height + 4f * Fugui.CurrentContext.Scale, height));
            ImGui.SameLine();
            // align and draw text
            ImGui.AlignTextToFramePadding();
            ImGui.Text(text);

            // display tooltip if needed
            displayToolTip(); // Display a tooltip if one has been set for this element
            _elementHoverFramed = false; // Set the flag indicating that this element should have a hover frame drawn around it
            endElement(style); // Pop the style for the checkbox element
            return clicked; // Return a boolean indicating whether the checkbox was clicked by the user
        }
    }
}