using Fu.Core.DearImGui.Assets;
using Fu.Core.DearImGui.Renderer;
using Fu.Core.DearImGui.Texture;
using UImGui.Renderer;
using UnityEngine.Assertions;

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
    }
}