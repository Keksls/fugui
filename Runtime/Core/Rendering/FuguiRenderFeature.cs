using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Fu
{
    /// <summary>
    /// Represents the Fugui Render Feature type.
    /// </summary>
    public class FuguiRenderFeature : ScriptableRendererFeature
    {
        #region State
        internal const string _sampleName = "Fugui.ExecuteDrawCommands";
        #endregion

        /// <summary>
        /// Represents the Fugui Render Graph Pass type.
        /// </summary>
        private class FuguiRenderGraphPass : ScriptableRenderPass
        {
            #region State
            private Dictionary<int, Mesh> _meshs;
            private Dictionary<int, Material> _materials;
            private Dictionary<int, RTHandle> _targetHandles;
            private readonly Shader _shader;
            private int _textureID;
            private Dictionary<int, int> _prevSubMeshCounts;
            private TextureManager _textureManager;
            private MaterialPropertyBlock _materialProperties;
            private bool _renderMainSurfaceContexts;
            private bool _renderOffscreenContexts;
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

            #region Constructors
            /// <summary>
            /// Creates a new Fugui Render Graph Pass.
            /// </summary>
            /// <param name="shader"> Shader to use for rendering Fugui.</param>
            public FuguiRenderGraphPass(Shader shader)
            {
                _shader = shader;
                _textureID = Shader.PropertyToID("_Texture");
                _materialProperties = new MaterialPropertyBlock();
                _prevSubMeshCounts = new Dictionary<int, int>();
                _targetHandles = new Dictionary<int, RTHandle>();

                renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

                // IMPORTANT : aucune dépendance implicite (depth/normals, etc.)
                ConfigureInput(ScriptableRenderPassInput.None);

                // (optionnel mais propre) évite les allocs répétées
                _meshs = new Dictionary<int, Mesh>();
                _materials = new Dictionary<int, Material>();
            }
            #endregion

            /// <summary>
            /// Configures which Fugui contexts this pass should render for the current camera.
            /// </summary>
            /// <param name="renderMainSurfaceContexts">Render contexts that target the camera color buffer.</param>
            /// <param name="renderOffscreenContexts">Render contexts that target render textures.</param>
            public void ConfigureFrame(bool renderMainSurfaceContexts, bool renderOffscreenContexts)
            {
                _renderMainSurfaceContexts = renderMainSurfaceContexts;
                _renderOffscreenContexts = renderOffscreenContexts;
            }

            /// <summary>
            /// Records the render graph pass for rendering Fugui.
            /// </summary>
            /// <param name="renderGraph"> The render graph to record the pass into.</param>
            /// <param name="frameData"> The frame data containing the active color texture.</param>
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var urpRes = frameData.Get<UniversalResourceData>();
                TextureHandle color = urpRes.activeColorTexture;
                TextureHandle depth = urpRes.activeDepthTexture;

                // Default context rendered on current camera color target
                if (_renderMainSurfaceContexts && Fugui.MainContainerEnabled && Fugui.DefaultContext is FuUnityContext defaultContext && defaultContext.Started)
                {
                    using var builder = renderGraph.AddRasterRenderPass<PassData>("Fugui_MainPass", out var passData);
                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderAttachment(color, 0, AccessFlags.ReadWrite);

                    if (depth.IsValid())
                    {
                        builder.SetRenderAttachmentDepth(depth, AccessFlags.Read);
                    }

                    builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                    {
                        if (defaultContext.DrawData.CmdListsCount <= 0 || defaultContext.DrawData.TotalVtxCount <= 0)
                        {
                            return;
                        }

                        _textureManager = defaultContext.TextureManager;
                        RenderDrawLists(defaultContext.ID, ctx.cmd, defaultContext.DrawData);
                    });
                }

                // Other contexts
                foreach (var pair in Fugui.Contexts)
                {
                    var context = pair.Value;

                    if (context == null || !context.Started)
                    {
                        continue;
                    }

#if FU_EXTERNALIZATION
                    // SDL / external windows are rendered outside Unity
                    if (context is FuExternalContext)
                    {
                        continue;
                    }
#endif

                    if (context is not FuUnityContext unityContext)
                    {
                        continue;
                    }

                    if (unityContext.DrawData.CmdListsCount <= 0 || unityContext.DrawData.TotalVtxCount <= 0)
                    {
                        continue;
                    }

                    // Skip default context if also present in Fugui.Contexts
                    if (ReferenceEquals(unityContext, Fugui.DefaultContext))
                    {
                        continue;
                    }

                    // Offscreen target
                    if (unityContext.IsOffscreen)
                    {
                        if (!_renderOffscreenContexts)
                        {
                            continue;
                        }

                        RenderTexture targetTexture = unityContext.TargetTexture;

                        if (targetTexture == null || !targetTexture.IsCreated())
                        {
                            continue;
                        }

                        RTHandle rtHandle = GetOrCreateTargetHandle(unityContext.ID, targetTexture);
                        TextureHandle importedTarget = renderGraph.ImportTexture(rtHandle);

                        using var builder = renderGraph.AddRasterRenderPass<PassData>($"Fugui_Offscreen_{unityContext.ID}", out var passData);
                        builder.AllowGlobalStateModification(true);
                        builder.SetRenderAttachment(importedTarget, 0, AccessFlags.Write);

                        builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                        {
                            //DebugDumpDrawData("OFFSCREEN", unityContext.DrawData);
                            _textureManager = unityContext.TextureManager;
                            RenderDrawLists(unityContext.ID, ctx.cmd, unityContext.DrawData);
                        });
                    }
                    else
                    {
                        if (!_renderMainSurfaceContexts)
                        {
                            continue;
                        }

                        using var builder = renderGraph.AddRasterRenderPass<PassData>($"Fugui_Context_{unityContext.ID}", out var passData);
                        builder.AllowGlobalStateModification(true);
                        builder.SetRenderAttachment(color, 0, AccessFlags.Write);

                        if (depth.IsValid())
                        {
                            builder.SetRenderAttachmentDepth(depth, AccessFlags.Read);
                        }

                        builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                        {
                            _textureManager = unityContext.TextureManager;
                            RenderDrawLists(unityContext.ID, ctx.cmd, unityContext.DrawData);
                        });
                    }
                }
            }

            /// <summary>
            /// Renders the draw lists using the provided command buffer and draw data.
            /// </summary>
            /// <param name="commandBuffer"> The command buffer to use for rendering.</param>
            /// <param name="drawData"></ param>
            public void RenderDrawLists(int ctxId, RasterCommandBuffer commandBuffer, DrawData drawData)
            {
                // ensure mesh and material exist for this context
                if (_meshs == null) _meshs = new Dictionary<int, Mesh>();
                if (!_meshs.ContainsKey(ctxId))
                {
                    _meshs[ctxId] = new Mesh
                    {
                        name = "FuguiMesh"
                    };
                    _meshs[ctxId].MarkDynamic();
                }
                if (_materials == null) _materials = new Dictionary<int, Material>();
                if (!_materials.ContainsKey(ctxId))
                {
                    _materials[ctxId] = new Material(_shader)
                    {
                        hideFlags = HideFlags.HideAndDontSave & ~HideFlags.DontUnloadUnusedAsset
                    };
                }
                Mesh _mesh = _meshs[ctxId];
                Material _material = _materials[ctxId];

                Vector2 fbOSize = drawData.DisplaySize * drawData.FramebufferScale;

                // Avoid rendering when minimized.
                if (fbOSize.x <= 0f || fbOSize.y <= 0f || drawData.TotalVtxCount == 0) return;

                // display draw data for debug
                UpdateMesh(ctxId, _mesh, _material, drawData);
                commandBuffer.BeginSample(_sampleName);
                CreateDrawCommands(_mesh, _material, commandBuffer, drawData, fbOSize);
                commandBuffer.EndSample(_sampleName);
            }

            /// <summary>
            /// Runs the debug dump draw data workflow.
            /// </summary>
            /// <param name="label">The label value.</param>
            /// <param name="drawData">The draw Data value.</param>
            private void DebugDumpDrawData(string label, DrawData drawData)
            {
                try
                {
                    Debug.Log($"[{label}] CmdLists={drawData.CmdListsCount}, TotalVtx={drawData.TotalVtxCount}, TotalIdx={drawData.TotalIdxCount}, DisplaySize={drawData.DisplaySize}, FramebufferScale={drawData.FramebufferScale}, DisplayPos={drawData.DisplayPos}");

                    for (int n = 0; n < drawData.CmdListsCount; n++)
                    {
                        var dl = drawData.DrawLists[n];
                        Debug.Log($"[{label}] DrawList #{n} -> Vtx={dl.VtxBuffer.Length}, Idx={dl.IdxBuffer.Length}, Cmd={dl.CmdBuffer.Length}");

                        int maxV = Mathf.Min(8, dl.VtxBuffer.Length);
                        for (int i = 0; i < maxV; i++)
                        {
                            var v = dl.VtxBuffer[i];
                            Debug.Log($"[{label}] V[{i}] pos=({v.pos.x}, {v.pos.y}) uv=({v.uv.x}, {v.uv.y}) col=0x{v.col:X8}");
                        }

                        int maxI = Mathf.Min(18, dl.IdxBuffer.Length);
                        string idxText = "";
                        for (int i = 0; i < maxI; i++)
                        {
                            idxText += dl.IdxBuffer[i].ToString();
                            if (i < maxI - 1)
                            {
                                idxText += ", ";
                            }
                        }
                        Debug.Log($"[{label}] Indices: {idxText}");

                        int maxC = Mathf.Min(8, dl.CmdBuffer.Length);
                        for (int i = 0; i < maxC; i++)
                        {
                            var cmd = dl.CmdBuffer[i];
                            Debug.Log($"[{label}] Cmd[{i}] ElemCount={cmd.ElemCount}, IdxOffset={cmd.IdxOffset}, VtxOffset={cmd.VtxOffset}, ClipRect={cmd.ClipRect}, TextureId={cmd.TextureId}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[{label}] Dump failed: {e}");
                }
            }

            /// <summary>
            /// Creates the draw commands for rendering Fugui using the provided command buffer and draw data.
            /// </summary>
            /// <param name="commandBuffer"> The command buffer to use for rendering.</param>
            /// <param name="drawData"> The draw data containing the information to render.</param>
            /// <param name="fbSize"> The framebuffer size to use for rendering.</param>
            private void CreateDrawCommands(Mesh _mesh, Material _material, RasterCommandBuffer commandBuffer, DrawData drawData, Vector2 fbSize)
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

#if !UNITY_6000_4_OR_NEWER

            /// <summary>
            /// Executes the rendering logic for Fugui.
            /// This Render Pass should be used only for compatibility purposes if renderGraph is disabled.
            /// </summary>
            /// <param name="context"> The scriptable render context.</param>
            /// <param name="renderingData"> The camera data used for the render pass.</param>
            [Obsolete]
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var commandBuffer = CommandBufferPool.Get("FuguiRenderPass");

                // Render the default context
                if (_renderMainSurfaceContexts && Fugui.MainContainerEnabled && Fugui.DefaultContext != null)
                {
                    _textureManager = Fugui.DefaultContext.TextureManager;
                    RenderDrawLists(Fugui.DefaultContext.ID, commandBuffer, Fugui.DefaultContext.DrawData);
                }

                // Render other contexts if available.
                foreach (var contextPair in Fugui.Contexts)
                {
                    if (contextPair.Key != 0 && contextPair.Value.Started)
                    {
                        if (contextPair.Value is FuUnityContext unityContext)
                        {
                            if (unityContext.IsOffscreen && !_renderOffscreenContexts)
                            {
                                continue;
                            }

                            if (!unityContext.IsOffscreen && !_renderMainSurfaceContexts)
                            {
                                continue;
                            }
                        }

                        _textureManager = contextPair.Value.TextureManager;
                        RenderDrawLists(contextPair.Key, commandBuffer, contextPair.Value.DrawData);
                    }
                }

                context.ExecuteCommandBuffer(commandBuffer);
                CommandBufferPool.Release(commandBuffer);
            }

            /// <summary>
            /// Renders the draw lists using the provided command buffer and draw data.
            /// </summary>
            /// <param name="commandBuffer"> The command buffer to use for rendering.</param>
            /// <param name="drawData"></ param>
            public void RenderDrawLists(int ctxId, CommandBuffer commandBuffer, DrawData drawData)
            {
                // ensure mesh and material exist for this context
                if (_meshs == null) _meshs = new Dictionary<int, Mesh>();
                if (!_meshs.ContainsKey(ctxId))
                {
                    _meshs[ctxId] = new Mesh
                    {
                        name = "FuguiMesh"
                    };
                    _meshs[ctxId].MarkDynamic();
                }
                if (_materials == null) _materials = new Dictionary<int, Material>();
                if (!_materials.ContainsKey(ctxId))
                {
                    _materials[ctxId] = new Material(_shader)
                    {
                        hideFlags = HideFlags.HideAndDontSave & ~HideFlags.DontUnloadUnusedAsset
                    };
                }
                Mesh _mesh = _meshs[ctxId];
                Material _material = _materials[ctxId];

                Vector2 fbOSize = drawData.DisplaySize * drawData.FramebufferScale;

                // Avoid rendering when minimized.
                if (fbOSize.x <= 0f || fbOSize.y <= 0f || drawData.TotalVtxCount == 0) return;

                // display draw data for debug
                UpdateMesh(ctxId, _mesh, _material, drawData);
                commandBuffer.BeginSample(_sampleName);
                CreateDrawCommands(_mesh, _material, commandBuffer, drawData, fbOSize);
                commandBuffer.EndSample(_sampleName);
            }

            /// <summary>
            /// Creates the draw commands for rendering Fugui using the provided command buffer and draw data.
            /// </summary>
            /// <param name="commandBuffer"> The command buffer to use for rendering.</param>
            /// <param name="drawData"> The draw data containing the information to render.</param>
            /// <param name="fbSize"> The framebuffer size to use for rendering.</param>
            private void CreateDrawCommands(Mesh _mesh, Material _material, CommandBuffer commandBuffer, DrawData drawData, Vector2 fbSize)
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
#endif
            /// <summary>
            /// Updates the mesh with the provided draw data.
            /// </summary>
            /// <param name="drawData"> The draw data containing the vertex and index buffers.</param>
            private void UpdateMesh(int ctxId, Mesh _mesh, Material _material, DrawData drawData)
            {
                // Nombre de submeshes = nombre total de ImDrawCmd
                int subMeshCount = 0;
                for (int n = 0; n < drawData.CmdListsCount; ++n)
                    subMeshCount += drawData.DrawLists[n].CmdBuffer.Length;

                if (!_prevSubMeshCounts.TryGetValue(ctxId, out int prevSubMeshCount))
                {
                    prevSubMeshCount = -1;
                }

                if (prevSubMeshCount != subMeshCount)
                {
                    _mesh.Clear(true);
                    _mesh.subMeshCount = subMeshCount;
                    _prevSubMeshCounts[ctxId] = subMeshCount;
                }

                _mesh.SetVertexBufferParams(drawData.TotalVtxCount, _vertexAttributes);
                _mesh.SetIndexBufferParams(drawData.TotalIdxCount, IndexFormat.UInt16);

                int vtxOf = 0;
                int idxOf = 0;
                var descriptors = new List<SubMeshDescriptor>(subMeshCount);

                for (int n = 0; n < drawData.CmdListsCount; ++n)
                {
                    var dl = drawData.DrawLists[n];

                    // Upload direct des tableaux managés
                    _mesh.SetVertexBufferData(dl.VtxBuffer, 0, vtxOf, dl.VtxBuffer.Length, 0, NoMeshChecks);
                    _mesh.SetIndexBufferData(dl.IdxBuffer, 0, idxOf, dl.IdxBuffer.Length, NoMeshChecks);

                    // Définition des submeshes pour chaque ImDrawCmd
                    var cmds = dl.CmdBuffer;
                    for (int i = 0; i < cmds.Length; ++i)
                    {
                        var cmd = cmds[i];
                        var desc = new SubMeshDescriptor
                        {
                            topology = MeshTopology.Triangles,
                            indexStart = idxOf + (int)cmd.IdxOffset,
                            indexCount = (int)cmd.ElemCount,
                            baseVertex = vtxOf + (int)cmd.VtxOffset,
                        };
                        descriptors.Add(desc);
                    }

                    vtxOf += dl.VtxBuffer.Length;
                    idxOf += dl.IdxBuffer.Length;
                }

                _mesh.SetSubMeshes(descriptors, NoMeshChecks);

                Vector2 fbSize = drawData.DisplaySize * drawData.FramebufferScale;
                _mesh.bounds = new Bounds(
                    new Vector3(fbSize.x * 0.5f, fbSize.y * 0.5f, 0f),
                    new Vector3(fbSize.x + 4f, fbSize.y + 4f, 1f)
                );

                _mesh.UploadMeshData(false);
            }

            /// <summary>
            /// Gets or creates a render target handle for the specified context ID and target texture.
            /// </summary>
            /// <param name="ctxId"> The context ID for which to get or create the render target handle.</param>
            /// <param name="targetTexture"> The target texture to use for the render target handle.</param>
            /// <returns> The render target handle for the specified context ID and target texture.</returns>
            private RTHandle GetOrCreateTargetHandle(int ctxId, RenderTexture targetTexture)
            {
                if (_targetHandles.TryGetValue(ctxId, out RTHandle handle))
                {
                    if (handle != null && handle.rt == targetTexture)
                    {
                        return handle;
                    }

                    handle.Release();
                    _targetHandles.Remove(ctxId);
                }

                handle = RTHandles.Alloc(targetTexture);
                _targetHandles[ctxId] = handle;
                return handle;
            }

            #region Nested Types
            /// <summary>
            /// Data structure to hold the render pass data for the Fugui Render Graph Pass.
            /// </summary>
            private class PassData
            {
            }
            #endregion
        }

        #region State
        public RenderPassEvent PassEvent = RenderPassEvent.AfterRendering;
        public Shader _shader;
        public int _cameraLayer = 5; // 5 is default unity UI layer
        private Dictionary<Camera, FuguiRenderGraphPass> _passPerCamera = new();
        #endregion

        #region Methods
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
            bool renderMainSurfaceContexts = camera.gameObject.layer == _cameraLayer && Fugui.MainContainerEnabled;
            bool renderOffscreenContexts = renderMainSurfaceContexts || (!Fugui.MainContainerEnabled && IsOffscreenDriverCamera(renderingData));

            if (!renderMainSurfaceContexts && !renderOffscreenContexts) return;

            if (!_passPerCamera.TryGetValue(camera, out var pass))
            {
                pass = new FuguiRenderGraphPass(_shader);
                _passPerCamera[camera] = pass;
            }

            pass.ConfigureFrame(renderMainSurfaceContexts, renderOffscreenContexts);
            renderer.EnqueuePass(pass);
        }

        /// <summary>
        /// Returns whether a scene camera can drive offscreen Fugui render textures.
        /// </summary>
        /// <param name="renderingData">The current camera rendering data.</param>
        /// <returns>True when the camera is a base game camera.</returns>
        private static bool IsOffscreenDriverCamera(RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;
            return camera != null
                && camera.cameraType == CameraType.Game
                && renderingData.cameraData.renderType == CameraRenderType.Base;
        }
        #endregion
    }
}
