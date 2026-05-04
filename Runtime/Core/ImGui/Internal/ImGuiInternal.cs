using System.Runtime.InteropServices;
using UnityEngine;

namespace ImGuiNET
{
    /// <summary>
    /// Represents the Im Gui Internal type.
    /// </summary>
    public unsafe class ImGuiInternal : MonoBehaviour
    {
        #region Methods
        /// <summary>
        /// Runs the ig shade verts linear color gradient keep alpha workflow.
        /// </summary>
        /// <param name="draw_list">The draw list value.</param>
        /// <param name="vert_start_idx">The vert start idx value.</param>
        /// <param name="vert_end_idx">The vert end idx value.</param>
        /// <param name="gradient_p0">The gradient p0 value.</param>
        /// <param name="gradient_p1">The gradient p1 value.</param>
        /// <param name="col0">The col0 value.</param>
        /// <param name="col1">The col1 value.</param>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern void igShadeVertsLinearColorGradientKeepAlpha(ImDrawList* draw_list, int vert_start_idx, int vert_end_idx, Vector2 gradient_p0, Vector2 gradient_p1, uint col0, uint col1);

        /// <summary>
        /// Runs the shade verts linear uv workflow.
        /// </summary>
        /// <param name="draw_list">The draw list value.</param>
        /// <param name="vert_start_idx">The vert start idx value.</param>
        /// <param name="vert_end_idx">The vert end idx value.</param>
        /// <param name="a">The a value.</param>
        /// <param name="b">The b value.</param>
        /// <param name="uv_a">The uv a value.</param>
        /// <param name="uv_b">The uv b value.</param>
        /// <param name="clamp">The clamp value.</param>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ShadeVertsLinearUV(ImDrawList* draw_list, int vert_start_idx, int vert_end_idx, ref Vector2 a, ref Vector2 b, ref Vector2 uv_a, ref Vector2 uv_b, bool clamp);

        /// <summary>
        /// Runs the ig render text workflow.
        /// </summary>
        /// <param name="pos">The pos value.</param>
        /// <param name="text">The text value.</param>
        /// <param name="text_end">The text end value.</param>
        /// <param name="hide_text_after_hash">The hide text after hash value.</param>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern void igRenderText(Vector2 pos, byte* text, byte* text_end, bool hide_text_after_hash);

        /// <summary>
        /// Runs the ig render text wrapped workflow.
        /// </summary>
        /// <param name="pos">The pos value.</param>
        /// <param name="text">The text value.</param>
        /// <param name="text_end">The text end value.</param>
        /// <param name="wrap_width">The wrap width value.</param>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern void igRenderTextWrapped(Vector2 pos, byte* text, byte* text_end, float wrap_width);

        /// <summary>
        /// Runs the ig render text clipped workflow.
        /// </summary>
        /// <param name="pos_min">The pos min value.</param>
        /// <param name="pos_max">The pos max value.</param>
        /// <param name="text">The text value.</param>
        /// <param name="text_end">The text end value.</param>
        /// <param name="text_size_if_known">The text size if known value.</param>
        /// <param name="align">The align value.</param>
        /// <param name="clip_rect">The clip rect value.</param>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern void igRenderTextClipped(Vector2 pos_min, Vector2 pos_max, byte* text, byte* text_end, Vector2* text_size_if_known, Vector2 align, ImRect* clip_rect);

        /// <summary>
        /// Runs the ig render text clipped ex workflow.
        /// </summary>
        /// <param name="draw_list">The draw list value.</param>
        /// <param name="pos_min">The pos min value.</param>
        /// <param name="pos_max">The pos max value.</param>
        /// <param name="text">The text value.</param>
        /// <param name="text_end">The text end value.</param>
        /// <param name="text_size_if_known">The text size if known value.</param>
        /// <param name="align">The align value.</param>
        /// <param name="clip_rect">The clip rect value.</param>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern void igRenderTextClippedEx(ImDrawList* draw_list, Vector2 pos_min, Vector2 pos_max, byte* text, byte* text_end, Vector2* text_size_if_known, Vector2 align, ImRect* clip_rect);

        /// <summary>
        /// Runs the ig clear active id workflow.
        /// </summary>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern void igClearActiveID();

        /// <summary>
        /// Returns the internal ImGui window for a native UTF8 window name.
        /// </summary>
        /// <param name="name">The null-terminated native window name.</param>
        /// <returns>The internal ImGui window pointer, or null.</returns>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern ImGuiWindow* igFindWindowByName(byte* name);

        /// <summary>
        /// Requests focus for an internal ImGui window.
        /// </summary>
        /// <param name="window">The internal ImGui window pointer.</param>
        /// <param name="flags">The ImGuiFocusRequestFlags value.</param>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern void igFocusWindow(ImGuiWindow* window, int flags);

        /// <summary>
        /// Moves an internal ImGui window to the front of the focus order.
        /// </summary>
        /// <param name="window">The internal ImGui window pointer.</param>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern void igBringWindowToFocusFront(ImGuiWindow* window);

        /// <summary>
        /// Moves an internal ImGui window to the front of the display order.
        /// </summary>
        /// <param name="window">The internal ImGui window pointer.</param>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern void igBringWindowToDisplayFront(ImGuiWindow* window);
        #endregion
    }
}
