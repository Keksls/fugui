﻿using Fu.Core.DearImGui.Assets;
using Fu.Core.DearImGui.Renderer;
using Fu.Core.DearImGui.Texture;
using UImGui.Renderer;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
#if HAS_URP
using UnityEngine.Rendering.Universal;
#elif HAS_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

namespace Fu.Core.DearImGui
{
	public static class RenderUtility
	{
		public static IRenderer Create(RenderType type, ShaderResourcesAsset shaders, TextureManager textures)
		{
			Assert.IsNotNull(shaders, "Shaders not assigned.");

			switch (type)
			{
#if UNITY_2020_1_OR_NEWER
				case RenderType.Mesh:
					return new RendererMesh(shaders, textures);
#endif
				case RenderType.Procedural:
					return new RendererProcedural(shaders, textures);
				default:
					return null;
			}
		}

		public static bool IsUsingURP()
		{
			RenderPipelineAsset currentRP = GraphicsSettings.currentRenderPipeline;
#if HAS_URP
			return currentRP is UniversalRenderPipelineAsset;
#else
			return false;
#endif
		}

		public static bool IsUsingHDRP()
		{
			RenderPipelineAsset currentRP = GraphicsSettings.currentRenderPipeline;

#if HAS_HDRP
			return currentRP is HDRenderPipelineAsset;
#else
			return false;
#endif
		}

		public static CommandBuffer GetCommandBuffer(string name)
		{
#if HAS_URP || HAS_HDRP
			return CommandBufferPool.Get(name);
#else
			return new CommandBuffer { name = name };
#endif
		}

		public static void ReleaseCommandBuffer(CommandBuffer commandBuffer)
		{
#if HAS_URP || HAS_HDRP
			CommandBufferPool.Release(commandBuffer);
#else
			commandBuffer.Release();
#endif
		}
	}
}