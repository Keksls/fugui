﻿using ImGuiNET;
using UnityEngine.Rendering;

namespace Fu.Core.DearImGui.Renderer
{
	/// <summary>
	/// TODO: Write
	/// </summary>
	public interface IRenderer
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="io"></param>
		void Initialize(ImGuiIOPtr io);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="io"></param>
		void Shutdown(ImGuiIOPtr io);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="commandBuffer"></param>
		/// <param name="drawData"></param>
		void RenderDrawLists(CommandBuffer commandBuffer, DrawData drawData);
	}
}