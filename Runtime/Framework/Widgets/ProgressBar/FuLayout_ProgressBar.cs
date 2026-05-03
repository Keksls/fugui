using Fu;
using ImGuiNET;
using System;
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
        /// Renders a Idle progress bar with the given text. The progress bar will have the default size and style.
        /// </summary>
        /// <param name="text">The text to display on the progress bar.</param>
        /// <param name="displayText">Text to display as value</param>
        public void ProgressBar(string text, string displayText = null)
        {
            ProgressBar(text, 0f, false, new FuElementSize(-1f, 18f), displayText: displayText);
        }

        /// <summary>
        /// Renders a Idle progress bar with the given text. The progress bar will have the default size and style.
        /// </summary>
        /// <param name="text">The text to display on the progress bar.</param>
        /// <param name="size">The size of the progress bar</param>
        /// <param name="displayText">Text to display as value</param>
        public void ProgressBar(string text, FuElementSize size, string displayText = null)
        {
            ProgressBar(text, 0f, false, size, ProgressBarTextPosition.Inside, displayText);
        }

        /// <summary>
        /// Renders a progress bar with the given text. The progress bar will have the default size and style.
        /// </summary>
        /// <param name="text">The text to display on the progress bar.</param>
        /// <param name="value">The value of the progress bar (between 0f and 1f)</param>
        /// <param name="textPosition">Position of the text value inside the progressbar</param>
        /// <param name="displayText">Text to display as value</param>
        public void ProgressBar(string text, float value, ProgressBarTextPosition textPosition = ProgressBarTextPosition.Inside, string displayText = null)
        {
            ProgressBar(text, value, true, new FuElementSize(-1f, 18f), textPosition, displayText);
        }

        /// <summary>
        /// Renders a progress bar with the given text. The progress bar will have the default size and style.
        /// </summary>
        /// <param name="text">The text to display on the progress bar.</param>
        /// <param name="value">The value of the progress bar (between 0f and 1f)</param>
        /// <param name="progressbarSize">size of the progressbar</param>
        /// <param name="textPosition">Position of the text value inside the progressbar</param>
        /// <param name="displayText">Text to display as value</param>
        public void ProgressBar(string text, float value, FuElementSize progressbarSize, ProgressBarTextPosition textPosition = ProgressBarTextPosition.Inside, string displayText = null)
        {
            ProgressBar(text, value, true, progressbarSize, textPosition, displayText);
        }

        /// <summary>
        /// Renders a progress bar with the given text. The progress bar will have the default size and style.
        /// </summary>
        /// <param name="text">The text to display on the progress bar.</param>
        /// <param name="value">The value of the progress bar (between 0f and 1f)</param>
        /// <param name="isContinuous">Whatever the progressbar is continuous or idle</param>
        /// <param name="progressbarSize">size of the progressbar</param>
        /// <param name="textPosition">Position of the text value inside the progressbar</param>
        /// <param name="displayText">Text to display as value</param>
        protected virtual void ProgressBar(string text, float value, bool isContinuous, FuElementSize progressbarSize, ProgressBarTextPosition textPosition = ProgressBarTextPosition.Inside, string displayText = null)
        {
            beginElement(ref text);
            if (!_drawElement)
            {
                return;
            }

            Vector2 size = progressbarSize.GetSize();
            if (isContinuous)
            {
                continuousProgressBar(value, textPosition, size, displayText);
            }
            else
            {
                idleProgressBar(size);
            }
            // set states for this element
            setBaseElementState(text, _currentItemStartPos, ImGui.GetItemRectMax() - _currentItemStartPos, false, false);
            displayToolTip();
            endElement();
        }

        /// <summary>
        /// Draw a continuous progressBar
        /// </summary>
        /// <param name="value">value of the prpogressbar</param>
        /// <param name="textPosition">position of the text value inside the progressbar</param>
        /// <param name="size">size of the progressbar</param>
        /// <param name="displayText">Text to display as value</param>
        private void continuousProgressBar(float value, ProgressBarTextPosition textPosition, Vector2 size, string displayText)
        {
            float scale = Fugui.CurrentContext.Scale;
            float rounding = Mathf.Min(Mathf.Max(Fugui.Themes.FrameRounding, 5f * scale), size.y * 0.5f);
            value = Mathf.Clamp01(value);
            Vector2 cursorPos = ImGui.GetCursorScreenPos();
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector4 frameColor = Fugui.Themes.GetColor(FuColors.FrameBg);
            Vector4 fillColor = Fugui.Themes.GetColor(FuColors.CheckMark);
            Vector4 borderColor = Fugui.Themes.GetColor(FuColors.Border);
            if (LastItemDisabled)
            {
                frameColor *= 0.5f;
                fillColor *= 0.5f;
                borderColor *= 0.5f;
            }

            // Draw the container
            DrawRoundedSegment(drawList, cursorPos, cursorPos + size, frameColor, rounding, true);
            // Calculate the size of the filled part
            var filledPartSize = new Vector2(size.x * value, size.y);
            // Draw the filled part
            if (filledPartSize.x > 0)
            {
                DrawRoundedSegment(drawList, cursorPos, cursorPos + filledPartSize, fillColor, rounding, true);
            }
            drawList.AddRect(cursorPos, cursorPos + size, ImGui.GetColorU32(borderColor), rounding, ImDrawFlags.RoundCornersAll, Mathf.Max(1f, Fugui.Themes.FrameBorderSize));

            // Display the text
            Vector2 textPos;
            string text = displayText == null ? string.Format("{0}%", (int)(value * 100)) : displayText;
            Vector2 textSize = ImGui.CalcTextSize(text);
            Vector4 textColor;
            switch (textPosition)
            {
                case ProgressBarTextPosition.Left:
                    textPos = cursorPos + new Vector2(6f * scale, (filledPartSize.y - textSize.y) / 2);
                    textColor = GetReadableTextColor(textPos.x + textSize.x * 0.5f <= cursorPos.x + filledPartSize.x ? fillColor : frameColor, LastItemDisabled);
                    drawList.AddText(textPos, ImGui.GetColorU32(textColor), text);
                    break;
                case ProgressBarTextPosition.Right:
                    textPos = cursorPos + new Vector2(size.x - (textSize.x + 6f * scale), (filledPartSize.y - textSize.y) / 2);
                    textColor = GetReadableTextColor(textPos.x + textSize.x * 0.5f <= cursorPos.x + filledPartSize.x ? fillColor : frameColor, LastItemDisabled);
                    drawList.AddText(textPos, ImGui.GetColorU32(textColor), text);
                    break;
                case ProgressBarTextPosition.Inside:
                    textPos = cursorPos + new Vector2((size.x - textSize.x) * 0.5f, (filledPartSize.y - textSize.y) / 2);
                    textColor = GetReadableTextColor(textPos.x + textSize.x * 0.5f <= cursorPos.x + filledPartSize.x ? fillColor : frameColor, LastItemDisabled);
                    drawList.AddText(textPos, ImGui.GetColorU32(textColor), text);
                    break;
            }
            ImGui.Dummy(size);
        }

        /// <summary>
        /// Draw a Idle progressbar
        /// </summary>
        /// <param name="size">size of the progressbar</param>
        private void idleProgressBar(Vector2 size)
        {
            float scale = Fugui.CurrentContext.Scale;
            float rounding = Mathf.Min(Mathf.Max(Fugui.Themes.FrameRounding, 5f * scale), size.y * 0.5f);
            var drawList = ImGui.GetWindowDrawList();
            float animationTime = (float)ImGui.GetTime();
            float animationSpeed = .5f;
            float animationPosition = (float)Math.Sin(animationTime * animationSpeed * Math.PI * 2.0f) * 0.5f + 0.5f;
            float barFillerWidth = size.x * 0.25f;
            Vector2 barSize = new Vector2(barFillerWidth * (1.0f - 0.05f * Math.Abs(2.0f * animationPosition - 1.0f)), size.y);
            Vector2 barPos = ImGui.GetCursorScreenPos() + new Vector2(size.x * 0.5f - barFillerWidth * 0.5f + (animationPosition - 0.5f) * (size.x - barFillerWidth), 0.0f);
            Vector2 cursorPos = ImGui.GetCursorScreenPos();
            Vector4 frameColor = Fugui.Themes.GetColor(FuColors.FrameBg);
            Vector4 fillColor = Fugui.Themes.GetColor(FuColors.CheckMark);
            Vector4 shineColor = Color.white;
            if (LastItemDisabled)
            {
                frameColor *= 0.5f;
                fillColor *= 0.5f;
            }
            fillColor.w *= 0.9f;
            shineColor.w = LastItemDisabled ? 0.04f : 0.13f;

            DrawRoundedSegment(drawList, cursorPos, cursorPos + size, frameColor, rounding, true);
            DrawRoundedSegment(drawList, barPos, barPos + barSize, fillColor, rounding, true);
            DrawRoundedSegment(drawList, barPos + new Vector2(2f * scale, 2f * scale), barPos + new Vector2(Mathf.Max(2f * scale, barSize.x * 0.45f), size.y - 2f * scale), shineColor, rounding, true);
            ImGui.Dummy(size);

            // for draw current window to ensure animation fluidity
            FuWindow.CurrentDrawingWindow?.ForceDraw();
        }
        #endregion
    }
}
