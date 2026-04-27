using ImGuiNET;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Represents the Fugui type.
    /// </summary>
    public partial class Fugui
    {
        #region Methods
        /// <summary>
        /// Draws the mobile touch feedback.
        /// </summary>
        private static void DrawMobileTouchFeedback()
        {
            ImDrawListPtr drawList = ImGui.GetForegroundDrawList();

            foreach(Vector2 touchPos in mobileTouches)
            {
                Vector2 pos = new Vector2(touchPos.x, ImGui.GetIO().DisplaySize.y - touchPos.y);
                Color borderColor = Color.white;
                Color fillColor = new Color(1, 1, 1, 0.5f);
                float circleRadius = 30f;
                // draw filled circle
                drawList.AddCircleFilled(pos, circleRadius, ImGui.ColorConvertFloat4ToU32(fillColor));
                // draw circle border
                drawList.AddCircle(pos, circleRadius, ImGui.ColorConvertFloat4ToU32(borderColor), 0, 2f);
            }
        }
        #endregion
    }
}