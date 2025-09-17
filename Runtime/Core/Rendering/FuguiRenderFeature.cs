using ImGuiNET;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
#if HAS_UDP
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace Fu
{
#if HAS_URP
    public class FuguiRenderFeature : ScriptableRendererFeature
    {
        internal const string _sampleName = "Fugui.ExecuteDrawCommands";

        private class FuguiRenderGraphPass : ScriptableRenderPass
        {
            #region Variables
            private Material _material;
            private Mesh _mesh;
            private int _textureID;
            private int _prevSubMeshCount = 1;
            private TextureManager _textureManager;
            private MaterialPropertyBlock _materialProperties;
            // Skip all checks and validation when updating the mesh.
            private const MeshUpdateFlags NoMeshChecks = MeshUpdateFlags.DontNotifyMeshUsers |
                MeshUpdateFlags.DontRecalculateBounds |
                MeshUpdateFlags.DontResetBoneBounds |
                MeshUpdateFlags.DontValidateIndices;
            // Color sent with TexCoord1 semantics because otherwise Color attribute would be reordered to come before UVs.
            private static VertexAttributeDescriptor[] _vertexAttributes = new[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position , VertexAttributeFormat.Float32, 2), // ¨Pos
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2), // UV
                new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.UInt32, 1), // Color
            };
            #endregion

            /// <summary>
            /// Creates a new Fugui Render Graph Pass.
            /// </summary>
            /// <param name="shader"> Shader to use for rendering Fugui.</param>
            public FuguiRenderGraphPass(Shader shader)
            {
                _textureID = Shader.PropertyToID("_Texture");
                _materialProperties = new MaterialPropertyBlock();
                _material = new Material(shader)
                {
                    hideFlags = HideFlags.HideAndDontSave & ~HideFlags.DontUnloadUnusedAsset
                };
                _mesh = new Mesh
                {
                    name = "FuguiMesh"
                };
                _mesh.MarkDynamic();
            }

            #region Rendergraph Pass
            /// <summary>
            /// Records the render graph pass for rendering Fugui.
            /// </summary>
            /// <param name="renderGraph"> The render graph to record the pass into.</param>
            /// <param name="frameData"> The frame data containing the active color texture.</param>
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                // get the active color texture from the frame data
                var urpRes = frameData.Get<UniversalResourceData>();

                // Use the *active* camera targets provided by URP
                TextureHandle color = urpRes.activeColorTexture;
                TextureHandle depth = urpRes.activeDepthTexture; // important : bind depth as attachment

                using var builder = renderGraph.AddRasterRenderPass<PassData>("Fugui_RenderGraph_Pass", out var passData);

                // Let this pass modify GL state if needed (you already had this)
                builder.AllowGlobalStateModification(true);

                // >>> Attach the actual render targets of the camera <<<
                builder.SetRenderAttachment(color, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(depth, AccessFlags.Read); // depth test only

                // set the pass data
                builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                {
                    // If not ready to render, skip this pass.
                    if (Fugui.RenderingState != FuguiRenderingState.UpdateComplete)
                    {
                        return;
                    }
                    Fugui.RenderingState = FuguiRenderingState.Rendering;

                    // render the default context
                    Fugui.DefaultContext.EndRender();
                    _textureManager = Fugui.DefaultContext.TextureManager;
                    RenderDrawLists(ctx.cmd, Fugui.DefaultContext.DrawData);

                    // render any other contexts
                    foreach (var context in Fugui.Contexts)
                    {
                        if (context.Key != 0 && context.Value.Started)
                        {
                            context.Value.EndRender();
                            _textureManager = context.Value.TextureManager;
                            RenderDrawLists(ctx.cmd, context.Value.DrawData);
                        }
                    }

                    Fugui.RenderingState = FuguiRenderingState.RenderComplete;
                });
            }

            /// <summary>
            /// Renders the draw lists using the provided command buffer and draw data.
            /// </summary>
            /// <param name="commandBuffer"> The command buffer to use for rendering.</param>
            /// <param name="drawData"></ param>
            public void RenderDrawLists(RasterCommandBuffer commandBuffer, DrawData drawData)
            {
                Vector2 fbOSize = drawData.DisplaySize * drawData.FramebufferScale;

                // Avoid rendering when minimized.
                if (fbOSize.x <= 0f || fbOSize.y <= 0f || drawData.TotalVtxCount == 0) return;

                UpdateMesh(drawData);
                commandBuffer.BeginSample(_sampleName);
                CreateDrawCommands(commandBuffer, drawData, fbOSize);
                commandBuffer.EndSample(_sampleName);
            }

            /// <summary>
            /// Creates the draw commands for rendering Fugui using the provided command buffer and draw data.
            /// </summary>
            /// <param name="commandBuffer"> The command buffer to use for rendering.</param>
            /// <param name="drawData"> The draw data containing the information to render.</param>
            /// <param name="fbSize"> The framebuffer size to use for rendering.</param>
            private void CreateDrawCommands(RasterCommandBuffer commandBuffer, DrawData drawData, Vector2 fbSize)
            {
                IntPtr prevTextureId = IntPtr.Zero;
                Vector4 clipOffset = new Vector4(drawData.DisplayPos.x, drawData.DisplayPos.y,
                    drawData.DisplayPos.x, drawData.DisplayPos.y);
                Vector4 clipScale = new Vector4(drawData.FramebufferScale.x, drawData.FramebufferScale.y,
                    drawData.FramebufferScale.x, drawData.FramebufferScale.y);

                commandBuffer.SetViewport(new Rect(0f, 0f, fbSize.x, fbSize.y));
                commandBuffer.SetViewProjectionMatrices(
                    Matrix4x4.Translate(new Vector3(0.5f / fbSize.x, 0.5f / fbSize.y, 0f)), // Small adjustment to improve text.
                    Matrix4x4.Ortho(0f, fbSize.x, fbSize.y, 0f, 0f, 1f));

                int subOf = 0;
                for (int n = 0, nMax = drawData.CmdListsCount; n < nMax; ++n)
                {
                    DrawList drawList = drawData.DrawLists[n];
                    for (int i = 0, iMax = drawList.CmdBuffer.Length; i < iMax; ++i, ++subOf)
                    {
                        ImDrawCmd drawCmd = drawList.CmdBuffer[i];
                        if (drawCmd.UserCallback != IntPtr.Zero)
                        {
                            Debug.Log("unhandled user callback");
                        }
                        else
                        {
                            // Project scissor rectangle into framebuffer space and skip if fully outside.
                            Vector4 clipSize = drawCmd.ClipRect - clipOffset;
                            Vector4 clip = Vector4.Scale(clipSize, clipScale);

                            if (clip.x >= fbSize.x || clip.y >= fbSize.y || clip.z < 0f || clip.w < 0f) continue;

                            if (prevTextureId != drawCmd.TextureId)
                            {
                                prevTextureId = drawCmd.TextureId;

                                // TODO: Implement ImDrawCmdPtr.GetTexID().
                                bool hasTexture = _textureManager.TryGetTexture(prevTextureId, out UnityEngine.Texture texture);

                                //Assert.IsTrue(hasTexture, $"Texture {prevTextureId} does not exist. Try to use UImGuiUtility.GetTextureID().");
                                if (!hasTexture)
                                {
                                    Debug.LogError($"Texture {prevTextureId} does not exist. Try to use UImGuiUtility.GetTextureID().");
                                }
                                else
                                {
                                    if (texture && texture != null)
                                        _materialProperties.SetTexture(_textureID, texture);
                                    else
                                        Debug.LogWarning($"Texture {prevTextureId} is null or not a valid texture.");
                                }
                            }
                            commandBuffer.EnableScissorRect(new Rect(clip.x, fbSize.y - clip.w, clip.z - clip.x, clip.w - clip.y)); // Invert y.
                            commandBuffer.DrawMesh(_mesh, Matrix4x4.identity, _material, subOf, 0, _materialProperties);
                        }
                    }
                }
                commandBuffer.DisableScissorRect();
            }
            #endregion

            #region Old Render Pass
            /// <summary>
            /// Executes the rendering logic for Fugui.
            /// This Render Pass should be used only for compatibility purposes if renderGraph is disabled.
            /// </summary>
            /// <param name="context"> The scriptable render context.</param>
            /// <param name="renderingData"> The camera data used for the render pass.</param>
            [Obsolete]
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                // If not ready to render, skip this pass.
                if (Fugui.RenderingState != FuguiRenderingState.UpdateComplete)
                    return;
                Fugui.RenderingState = FuguiRenderingState.Rendering;

                var commandBuffer = CommandBufferPool.Get("FuguiRenderPass");

                // Render the default context
                Fugui.DefaultContext.EndRender();
                _textureManager = Fugui.DefaultContext.TextureManager;
                RenderDrawLists(commandBuffer, Fugui.DefaultContext.DrawData);

                // Render other contexts if available.
                foreach (var contextPair in Fugui.Contexts)
                {
                    if (contextPair.Key != 0 && contextPair.Value.Started)
                    {
                        contextPair.Value.EndRender();
                        _textureManager = contextPair.Value.TextureManager;
                        RenderDrawLists(commandBuffer, contextPair.Value.DrawData);
                    }
                }

                context.ExecuteCommandBuffer(commandBuffer);
                CommandBufferPool.Release(commandBuffer);

                // Reset the rendering state after rendering is complete.
                Fugui.RenderingState = FuguiRenderingState.RenderComplete;
            }

            /// <summary>
            /// Renders the draw lists using the provided command buffer and draw data.
            /// </summary>
            /// <param name="commandBuffer"> The command buffer to use for rendering.</param>
            /// <param name="drawData"></ param>
            public void RenderDrawLists(CommandBuffer commandBuffer, DrawData drawData)
            {
                Vector2 fbOSize = drawData.DisplaySize * drawData.FramebufferScale;

                // Avoid rendering when minimized.
                if (fbOSize.x <= 0f || fbOSize.y <= 0f || drawData.TotalVtxCount == 0) return;

                UpdateMesh(drawData);
                commandBuffer.BeginSample(_sampleName);
                CreateDrawCommands(commandBuffer, drawData, fbOSize);
                commandBuffer.EndSample(_sampleName);
            }

            /// <summary>
            /// Creates the draw commands for rendering Fugui using the provided command buffer and draw data.
            /// </summary>
            /// <param name="commandBuffer"> The command buffer to use for rendering.</param>
            /// <param name="drawData"> The draw data containing the information to render.</param>
            /// <param name="fbSize"> The framebuffer size to use for rendering.</param>
            private void CreateDrawCommands(CommandBuffer commandBuffer, DrawData drawData, Vector2 fbSize)
            {
                IntPtr prevTextureId = IntPtr.Zero;
                Vector4 clipOffset = new Vector4(drawData.DisplayPos.x, drawData.DisplayPos.y,
                    drawData.DisplayPos.x, drawData.DisplayPos.y);
                Vector4 clipScale = new Vector4(drawData.FramebufferScale.x, drawData.FramebufferScale.y,
                    drawData.FramebufferScale.x, drawData.FramebufferScale.y);

                commandBuffer.SetViewport(new Rect(0f, 0f, fbSize.x, fbSize.y));
                commandBuffer.SetViewProjectionMatrices(
                    Matrix4x4.Translate(new Vector3(0.5f / fbSize.x, 0.5f / fbSize.y, 0f)), // Small adjustment to improve text.
                    Matrix4x4.Ortho(0f, fbSize.x, fbSize.y, 0f, 0f, 1f));

                int subOf = 0;
                for (int n = 0, nMax = drawData.CmdListsCount; n < nMax; ++n)
                {
                    DrawList drawList = drawData.DrawLists[n];
                    for (int i = 0, iMax = drawList.CmdBuffer.Length; i < iMax; ++i, ++subOf)
                    {
                        ImDrawCmd drawCmd = drawList.CmdBuffer[i];
                        if (drawCmd.UserCallback != IntPtr.Zero)
                        {
                            Debug.Log("unhandled user callback");
                        }
                        else
                        {
                            // Project scissor rectangle into framebuffer space and skip if fully outside.
                            Vector4 clipSize = drawCmd.ClipRect - clipOffset;
                            Vector4 clip = Vector4.Scale(clipSize, clipScale);

                            if (clip.x >= fbSize.x || clip.y >= fbSize.y || clip.z < 0f || clip.w < 0f) continue;

                            if (prevTextureId != drawCmd.TextureId)
                            {
                                prevTextureId = drawCmd.TextureId;

                                // TODO: Implement ImDrawCmdPtr.GetTexID().
                                bool hasTexture = _textureManager.TryGetTexture(prevTextureId, out UnityEngine.Texture texture);

                                //Assert.IsTrue(hasTexture, $"Texture {prevTextureId} does not exist. Try to use UImGuiUtility.GetTextureID().");
                                if (!hasTexture)
                                {
                                    Debug.LogError($"Texture {prevTextureId} does not exist. Try to use UImGuiUtility.GetTextureID().");
                                }
                                else
                                {
                                    if (texture && texture != null)
                                        _materialProperties.SetTexture(_textureID, texture);
                                    else
                                        Debug.LogWarning($"Texture {prevTextureId} is null or not a valid texture.");
                                }
                            }
                            commandBuffer.EnableScissorRect(new Rect(clip.x, fbSize.y - clip.w, clip.z - clip.x, clip.w - clip.y)); // Invert y.
                            commandBuffer.DrawMesh(_mesh, Matrix4x4.identity, _material, subOf, 0, _materialProperties);
                        }
                    }
                }
                commandBuffer.DisableScissorRect();
            }
            #endregion

            /// <summary>
            /// Updates the mesh with the provided draw data.
            /// </summary>
            /// <param name="drawData"> The draw data containing the vertex and index buffers.</param>
            private void UpdateMesh(DrawData drawData)
            {
                // Number of submeshes is the same as the nr of ImDrawCmd.
                int subMeshCount = 0;
                for (int n = 0, nMax = drawData.CmdListsCount; n < nMax; ++n)
                {
                    subMeshCount += drawData.DrawLists[n].CmdBuffer.Length;
                }

                if (_prevSubMeshCount != subMeshCount)
                {
                    // Occasionally crashes when changing subMeshCount without clearing first.
                    _mesh.Clear(true);
                    _mesh.subMeshCount = _prevSubMeshCount = subMeshCount;
                }

                _mesh.SetVertexBufferParams(drawData.TotalVtxCount, _vertexAttributes);
                _mesh.SetIndexBufferParams(drawData.TotalIdxCount, IndexFormat.UInt16);

                //  Upload data into mesh.
                int vtxOf = 0;
                int idxOf = 0;
                List<SubMeshDescriptor> descriptors = new List<SubMeshDescriptor>();

                for (int n = 0, nMax = drawData.CmdListsCount; n < nMax; ++n)
                {
                    DrawList drawList = drawData.DrawLists[n];

                    unsafe
                    {
                        // TODO: Convert NativeArray to C# array or list (remove collections).
                        NativeArray<ImDrawVert> vtxArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<ImDrawVert>(
                            (void*)drawList.VtxPtr, drawList.VtxBuffer.Length, Allocator.None);
                        NativeArray<ushort> idxArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<ushort>(
                            (void*)drawList.IdxPtr, drawList.IdxBuffer.Length, Allocator.None);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                        NativeArrayUnsafeUtility
                            .SetAtomicSafetyHandle(ref vtxArray, AtomicSafetyHandle.GetTempMemoryHandle());
                        NativeArrayUnsafeUtility
                            .SetAtomicSafetyHandle(ref idxArray, AtomicSafetyHandle.GetTempMemoryHandle());
#endif
                        // Upload vertex/index data.
                        _mesh.SetVertexBufferData(vtxArray, 0, vtxOf, vtxArray.Length, 0, NoMeshChecks);
                        _mesh.SetIndexBufferData(idxArray, 0, idxOf, idxArray.Length, NoMeshChecks);

                        // Define subMeshes.
                        for (int i = 0, iMax = drawList.CmdBuffer.Length; i < iMax; ++i)
                        {
                            ImDrawCmd cmd = drawList.CmdBuffer[i];
                            SubMeshDescriptor descriptor = new SubMeshDescriptor
                            {
                                topology = MeshTopology.Triangles,
                                indexStart = idxOf + (int)cmd.IdxOffset,
                                indexCount = (int)cmd.ElemCount,
                                baseVertex = vtxOf + (int)cmd.VtxOffset,
                            };
                            descriptors.Add(descriptor);
                        }

                        vtxOf += vtxArray.Length;
                        idxOf += idxArray.Length;
                    }
                }

                _mesh.SetSubMeshes(descriptors, NoMeshChecks);

                // Set a safe, large 2D bounds in clip space pixels
                Vector2 fbSize = drawData.DisplaySize * drawData.FramebufferScale;
                _mesh.bounds = new Bounds(new Vector3(fbSize.x * 0.5f, fbSize.y * 0.5f, 0f), new Vector3(fbSize.x + 4f, fbSize.y + 4f, 1f));
                _mesh.UploadMeshData(false);
            }

            /// <summary>
            /// Data structure to hold the render pass data for the Fugui Render Graph Pass.
            /// </summary>
            private class PassData
            {
                public TextureHandle colorTarget;
            }
        }

        public RenderPassEvent PassEvent = RenderPassEvent.AfterRendering;
        public Shader _shader;
        public int _cameraLayer = 5; // 5 is default unity UI layer
        private Dictionary<Camera, FuguiRenderGraphPass> _passPerCamera = new();

        /// <summary>
        /// Creates the Fugui Render Graph Pass with the specified shader.
        /// </summary>
        public override void Create()
        {
        }

        /// <summary>
        /// Adds the Fugui Render Graph Pass to the renderer's render passes.
        /// </summary>
        /// <param name="renderer"> The scriptable renderer to which the pass will be added.</param>
        /// <param name="renderingData"> The rendering data containing information about the current rendering state.</param>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData.camera;
            if (camera.gameObject.layer != _cameraLayer) return;

            if (!_passPerCamera.TryGetValue(camera, out var pass))
            {
                pass = new FuguiRenderGraphPass(_shader);
                _passPerCamera[camera] = pass;
            }

            renderer.EnqueuePass(pass);
        }
    }
#endif
}
