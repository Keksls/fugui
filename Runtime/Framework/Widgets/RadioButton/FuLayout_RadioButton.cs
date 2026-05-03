using ImGuiNET;
using UnityEngine;

namespace Fu.Framework
{
    /// <summary>
    /// Represents the Fu Layout type.
    /// </summary>
    public partial class FuLayout
    {
        #region Methods
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
            string id = text;
            beginElement(ref id, style); // Push the style for the checkbox element
            // return if item must no be draw
            if (!_drawElement)
            {
                return false;
            }

            text = Fugui.GetUntagedText(text);
            // get or create animation data
            if (!_uiElementAnimationDatas.ContainsKey(id))
            {
                _uiElementAnimationDatas.Add(id, new FuElementAnimationData(!isChecked));
            }
            FuElementAnimationData animationData = _uiElementAnimationDatas[id];

            // layout states
            float height = 18f * Fugui.CurrentContext.Scale;
            Vector2 pos = ImGui.GetCursorScreenPos();
            Vector2 CircleCenter = new Vector2(pos.x + height / 2f, pos.y + height / 2f);
            float radius = height * 0.42f;
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            // input stats
            bool hovered = ImGui.IsMouseHoveringRect(pos, pos + new Vector2(height, height));
            bool active = hovered && ImGui.IsMouseDown(0);
            bool clicked = hovered && ImGui.IsMouseReleased(0);
            animationData.Update(isChecked, _animationEnabled);
            // frame colors
            Vector4 BGColor;
            Vector4 knobColor;
            Vector4 borderColor;
            if (LastItemDisabled)
            {
                BGColor = style.DisabledFrame;
                knobColor = style.TextStyle.DisabledText;
                borderColor = style.DisabledBorder;
            }
            else
            {
                BGColor = isChecked ? style.CheckMark : style.Frame;
                knobColor = style.TextStyle.Text;
                borderColor = style.Border;
                if (active)
                {
                    BGColor = isChecked ? style.CheckMark * 0.82f : style.ActiveFrame;
                }
                else if (hovered)
                {
                    BGColor = isChecked ? style.CheckMark * 0.92f : style.HoveredFrame;
                }
            }

            // draw radio button
            AddCircleFilledAntiAliased(drawList, CircleCenter, radius, ImGui.GetColorU32(BGColor), 40);
            drawList.AddCircle(CircleCenter, radius, ImGui.GetColorU32(borderColor), 40, Mathf.Max(1f, Fugui.Themes.FrameBorderSize));
            if (animationData.CurrentValue > 0f)
            {
                float knobSize = Mathf.Lerp(0f, radius * 0.48f, animationData.CurrentValue);
                Vector4 dotColor = knobColor;
                dotColor.w *= animationData.CurrentValue;
                AddCircleFilledAntiAliased(drawList, CircleCenter, knobSize, ImGui.GetColorU32(dotColor), 32);
            }

            //draw hover frame
            if (hovered && !LastItemDisabled)
            {
                // set mouse cursor
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                Vector4 hoverColor = Fugui.Themes.GetColor(active ? FuColors.FrameSelectedFeedback : FuColors.FrameHoverFeedback);
                hoverColor.w = Mathf.Max(hoverColor.w, active ? 0.7f : 0.42f);
                drawList.AddCircle(CircleCenter, radius + 2f * Fugui.CurrentContext.Scale, ImGui.GetColorU32(hoverColor), 40, Mathf.Max(1f, Fugui.CurrentContext.Scale));
            }

            // dummy display button
            ImGuiNative.igDummy(new Vector2(height, height));
            ImGuiNative.igSameLine(0f, -1f);
            // align and draw text
            ImGuiNative.igAlignTextToFramePadding();
            ImGui.Text(text);
            // set states for this element
            setBaseElementState(text, _currentItemStartPos, ImGui.GetItemRectMax() - _currentItemStartPos, true, false);

            // display tooltip if needed
            displayToolTip(); // Display a tooltip if one has been set for this element
            _elementHoverFramedEnabled = false; // Set the flag indicating that this element should have a hover frame drawn around it
            endElement(style); // Pop the style for the checkbox element
            return clicked; // Return a boolean indicating whether the checkbox was clicked by the user
        }
        #endregion
    }
}
