using ImGuiNET;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fu.Core.DearImGui.Renderer
{
	/// <summary>
	/// TODO: Write
	/// </summary>
	public interface IRenderer
	{
        /// <summary>
        /// Initialize the renderer with the given ImGui IO.
        /// </summary>
        /// <param name="io"> the ImGui IO to initialize with</param>
        void Initialize(ImGuiIOPtr io);

        /// <summary>
        /// Shutdown the renderer and release any resources it holds.
        /// </summary>
        /// <param name="io"> the ImGui IO to shutdown</param>
        void Shutdown(ImGuiIOPtr io);

        /// <summary>
        /// Render the ImGui draw lists using the provided command buffer.
        /// </summary>
        /// <param name="commandBuffer"> the command buffer to use for rendering</param>
        /// <param name="drawData"> the draw data to render</param>
        void RenderDrawLists(CommandBuffer commandBuffer, DrawData drawData);

        /// <summary>
        /// Render the ImGui draw lists using the provided command buffer in a raster graph context.
        /// </summary>
        /// <param name="commandBuffer"> the command buffer to use for rendering</param>
        /// <param name="drawData"> the draw data to render</param>
        void RenderDrawLists(RasterCommandBuffer commandBuffer, DrawData drawData);
    }
}