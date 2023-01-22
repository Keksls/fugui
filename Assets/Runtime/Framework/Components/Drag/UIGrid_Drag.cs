using ImGuiNET;
using UnityEngine;

namespace Fugui.Framework
{
    public partial class UIGrid
    {
        /// <summary>
        /// Draw a drag element.
        /// This is a Label/ID + value name (optional) + Input
        /// </summary>
        /// <param name="text">Label/ID of the drag</param>
        /// <param name="value">refered value of the drag</param>
        /// <param name="vString">(optional nullable) name of the drag value</param>
        /// <param name="min">minimum value of the drag</param>
        /// <param name="max">minimum value of the drag</param>
        /// <param name="style">style of the drag (FrameStyle)</param>
        /// <returns>true if value changes</returns>
        public override bool Drag(string text, ref float value, string vString, float min, float max, UIFrameStyle style)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(text, style.TextStyle);
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().x);
            return base.Drag(text, ref value, vString, min, max, style);
        }

        /// <summary>
        /// Draw a drag element.
        /// This is a Label/ID + value name (optional) + Input
        /// </summary>
        /// <param name="text">Label/ID of the drag</param>
        /// <param name="value">refered value of the drag</param>
        /// <param name="v1String">(optional nullable) name of the drag value 1</param>
        /// <param name="v2String">(optional nullable) name of the drag value 2</param>
        /// <param name="min">minimum value of the drag</param>
        /// <param name="max">minimum value of the drag</param>
        /// <param name="style">style of the drag (FrameStyle)</param>
        /// <returns>true if value changes</returns>
        public override bool Drag(string text, ref Vector2 value, string v1String, string v2String, float min, float max, UIFrameStyle style)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(text, style.TextStyle);
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().x);
            return base.Drag(text, ref value, v1String, v2String, min, max, style);
        }

        /// <summary>
        /// Draw a drag element.
        /// This is a Label/ID + value name (optional) + Input
        /// </summary>
        /// <param name="text">Label/ID of the drag</param>
        /// <param name="value">refered value of the drag</param>
        /// <param name="v1String">(optional nullable) name of the drag value 1</param>
        /// <param name="v2String">(optional nullable) name of the drag value 2</param>
        /// <param name="v3String">(optional nullable) name of the drag value 3</param>
        /// <param name="min">minimum value of the drag</param>
        /// <param name="max">minimum value of the drag</param>
        /// <param name="style">style of the drag (FrameStyle)</param>
        /// <returns>true if value changes</returns>
        public override bool Drag(string text, ref Vector3 value, string v1String, string v2String, string v3String, float min, float max, UIFrameStyle style)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(text, style.TextStyle);
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().x);
            return base.Drag(text, ref value, v1String, v2String, v3String, min, max, style);
        }

        /// <summary>
        /// Draw a drag element.
        /// This is a Label/ID + value name (optional) + Input
        /// </summary>
        /// <param name="text">Label/ID of the drag</param>
        /// <param name="value">refered value of the drag</param>
        /// <param name="v1String">(optional nullable) name of the drag value 1</param>
        /// <param name="v2String">(optional nullable) name of the drag value 2</param>
        /// <param name="v3String">(optional nullable) name of the drag value 3</param>
        /// <param name="v4String">(optional nullable) name of the drag value 4</param>
        /// <param name="min">minimum value of the drag</param>
        /// <param name="max">minimum value of the drag</param>
        /// <param name="style">style of the drag (FrameStyle)</param>
        /// <returns>true if value changes</returns>
        public override bool Drag(string text, ref Vector4 value, string v1String, string v2String, string v3String, string v4String, float min, float max, UIFrameStyle style)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(text, style.TextStyle);
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().x);
            return base.Drag(text, ref value, v1String, v2String, v3String, v4String, min, max, style);
        }

        /// <summary>
        /// Draw a drag element.
        /// This is a Label/ID + value name (optional) + Input
        /// </summary>
        /// <param name="text">Label/ID of the drag</param>
        /// <param name="value">refered value of the drag</param>
        /// <param name="vString">(optional nullable) name of the drag value</param>
        /// <param name="min">minimum value of the drag</param>
        /// <param name="max">minimum value of the drag</param>
        /// <param name="style">style of the drag (FrameStyle)</param>
        /// <returns>true if value changes</returns>
        public override bool Drag(string text, string vString, ref int value, int min, int max, UIFrameStyle style)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(text, style.TextStyle);
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().x);
            return base.Drag(text, vString, ref value, min, max, style);
        }
    }
}