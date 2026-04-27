using System.Runtime.InteropServices;
using UnityEngine;

namespace ImGuiNET
{
    /// <summary>
    /// Represents the Native Docking type.
    /// </summary>
    public unsafe static class NativeDocking
    {
        #region Methods
        /// <summary>
        /// Returns the ig dock builder add node result.
        /// </summary>
        /// <param name="node_id">The node id value.</param>
        /// <param name="flags">The flags value.</param>
        /// <returns>The result of the operation.</returns>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint igDockBuilderAddNode(uint node_id, ImGuiDockNodeFlags flags);
        /// <summary>
        /// Runs the ig dock builder copy dock space workflow.
        /// </summary>
        /// <param name="src_dockspace_id">The src dockspace id value.</param>
        /// <param name="dst_dockspace_id">The dst dockspace id value.</param>
        /// <param name="in_window_remap_pairs">The in window remap pairs value.</param>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern void igDockBuilderCopyDockSpace(uint src_dockspace_id, uint dst_dockspace_id, ImVector* in_window_remap_pairs);
        /// <summary>
        /// Runs the ig dock builder copy node workflow.
        /// </summary>
        /// <param name="src_node_id">The src node id value.</param>
        /// <param name="dst_node_id">The dst node id value.</param>
        /// <param name="out_node_remap_pairs">The out node remap pairs value.</param>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern void igDockBuilderCopyNode(uint src_node_id, uint dst_node_id, ImVector* out_node_remap_pairs);
        /// <summary>
        /// Runs the ig dock builder copy window settings workflow.
        /// </summary>
        /// <param name="src_name">The src name value.</param>
        /// <param name="dst_name">The dst name value.</param>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern void igDockBuilderCopyWindowSettings(byte* src_name, byte* dst_name);
        /// <summary>
        /// Runs the ig dock builder dock window workflow.
        /// </summary>
        /// <param name="window_name">The window name value.</param>
        /// <param name="node_id">The node id value.</param>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern void igDockBuilderDockWindow(byte* window_name, uint node_id);
        /// <summary>
        /// Runs the ig dock builder finish workflow.
        /// </summary>
        /// <param name="node_id">The node id value.</param>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern void igDockBuilderFinish(uint node_id);
        /// <summary>
        /// Runs the ig dock builder remove node workflow.
        /// </summary>
        /// <param name="node_id">The node id value.</param>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern void igDockBuilderRemoveNode(uint node_id);
        /// <summary>
        /// Runs the ig dock builder remove node child nodes workflow.
        /// </summary>
        /// <param name="node_id">The node id value.</param>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern void igDockBuilderRemoveNodeChildNodes(uint node_id);
        /// <summary>
        /// Runs the ig dock builder remove node docked windows workflow.
        /// </summary>
        /// <param name="node_id">The node id value.</param>
        /// <param name="clear_settings_refs">The clear settings refs value.</param>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern void igDockBuilderRemoveNodeDockedWindows(uint node_id, byte clear_settings_refs);
        /// <summary>
        /// Runs the ig dock builder set node pos workflow.
        /// </summary>
        /// <param name="node_id">The node id value.</param>
        /// <param name="pos">The pos value.</param>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern void igDockBuilderSetNodePos(uint node_id, Vector2 pos);
        /// <summary>
        /// Runs the ig dock builder set node size workflow.
        /// </summary>
        /// <param name="node_id">The node id value.</param>
        /// <param name="size">The size value.</param>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern void igDockBuilderSetNodeSize(uint node_id, Vector2 size);
        /// <summary>
        /// Returns the ig dock builder split node result.
        /// </summary>
        /// <param name="node_id">The node id value.</param>
        /// <param name="split_dir">The split dir value.</param>
        /// <param name="size_ratio_for_node_at_dir">The size ratio for node at dir value.</param>
        /// <param name="out_id_at_dir">The out id at dir value.</param>
        /// <param name="out_id_at_opposite_dir">The out id at opposite dir value.</param>
        /// <returns>The result of the operation.</returns>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint igDockBuilderSplitNode(uint node_id, ImGuiDir split_dir, float size_ratio_for_node_at_dir, uint* out_id_at_dir, uint* out_id_at_opposite_dir);
        /// <summary>
        /// Returns the ig dock builder get central node result.
        /// </summary>
        /// <param name="node_id">The node id value.</param>
        /// <returns>The result of the operation.</returns>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern ImGuiDockNode* igDockBuilderGetCentralNode(uint node_id);
        /// <summary>
        /// Returns the ig dock builder get node result.
        /// </summary>
        /// <param name="node_id">The node id value.</param>
        /// <returns>The result of the operation.</returns>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern ImGuiDockNode* igDockBuilderGetNode(uint node_id);
        /// <summary>
        /// Runs the im gui dock node destroy workflow.
        /// </summary>
        /// <param name="self">The self value.</param>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImGuiDockNode_destroy(ImGuiDockNode* self);
        /// <summary>
        /// Returns the im gui dock node get merged flags result.
        /// </summary>
        /// <param name="self">The self value.</param>
        /// <returns>The result of the operation.</returns>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern ImGuiDockNodeFlags ImGuiDockNode_GetMergedFlags(ImGuiDockNode* self);
        /// <summary>
        /// Returns the im gui dock node im gui dock node result.
        /// </summary>
        /// <param name="id">The id value.</param>
        /// <returns>The result of the operation.</returns>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern ImGuiDockNode* ImGuiDockNode_ImGuiDockNode(uint id);
        /// <summary>
        /// Returns the im gui dock node is central node result.
        /// </summary>
        /// <param name="self">The self value.</param>
        /// <returns>The result of the operation.</returns>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern byte ImGuiDockNode_IsCentralNode(ImGuiDockNode* self);
        /// <summary>
        /// Returns the im gui dock node is dock space result.
        /// </summary>
        /// <param name="self">The self value.</param>
        /// <returns>The result of the operation.</returns>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern byte ImGuiDockNode_IsDockSpace(ImGuiDockNode* self);
        /// <summary>
        /// Returns the im gui dock node is empty result.
        /// </summary>
        /// <param name="self">The self value.</param>
        /// <returns>The result of the operation.</returns>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern byte ImGuiDockNode_IsEmpty(ImGuiDockNode* self);
        /// <summary>
        /// Returns the im gui dock node is floating node result.
        /// </summary>
        /// <param name="self">The self value.</param>
        /// <returns>The result of the operation.</returns>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ImGuiDockNode_IsFloatingNode(ImGuiDockNode* self);
        /// <summary>
        /// Returns the im gui dock node is hidden tab bar result.
        /// </summary>
        /// <param name="self">The self value.</param>
        /// <returns>The result of the operation.</returns>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern byte ImGuiDockNode_IsHiddenTabBar(ImGuiDockNode* self);
        /// <summary>
        /// Returns the im gui dock node is leaf node result.
        /// </summary>
        /// <param name="self">The self value.</param>
        /// <returns>The result of the operation.</returns>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern byte ImGuiDockNode_IsLeafNode(ImGuiDockNode* self);
        /// <summary>
        /// Returns the im gui dock node is no tab bar result.
        /// </summary>
        /// <param name="self">The self value.</param>
        /// <returns>The result of the operation.</returns>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern byte ImGuiDockNode_IsNoTabBar(ImGuiDockNode* self);
        /// <summary>
        /// Returns the im gui dock node is root node result.
        /// </summary>
        /// <param name="self">The self value.</param>
        /// <returns>The result of the operation.</returns>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern byte ImGuiDockNode_IsRootNode(ImGuiDockNode* self);
        /// <summary>
        /// Returns the im gui dock node is split node result.
        /// </summary>
        /// <param name="self">The self value.</param>
        /// <returns>The result of the operation.</returns>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern byte ImGuiDockNode_IsSplitNode(ImGuiDockNode* self);
        /// <summary>
        /// Runs the im gui dock node rect workflow.
        /// </summary>
        /// <param name="pOut">The p Out value.</param>
        /// <param name="self">The self value.</param>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImGuiDockNode_Rect(ImRect* pOut, ImGuiDockNode* self);
        #endregion
    }
}