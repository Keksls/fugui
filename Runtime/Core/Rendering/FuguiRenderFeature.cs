using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
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
            private Dictionary<int, Material> _materials;
            private Dictionary<int, RTHandle> _targetHandles;
            private Dictionary<int, List<DrawListMesh>> _transientMeshes;
            private readonly Shader _shader;
            private int _textureID;
            private int _textureIsAlphaID;
            private int _blitTextureID;
            private int _backdropPrefilterTexelSizeID;
            private int _backdropBlurDirectionID;
            private int _backdropBlurRadiusID;
            private int _backdropScreenSizeID;
            private int _backdropRenderOffsetID;
            private int _backdropCompositeTexelSizeID;
            private Material _backdropMaterial;
            private Dictionary<int, int> _prevSubMeshCounts;
            private Dictionary<int, int> _prevVertexCounts;
            private Dictionary<int, int> _prevIndexCounts;
            private Dictionary<int, List<SubMeshDescriptor>> _subMeshDescriptors;
            private TextureManager _textureManager;
            private MaterialPropertyBlock _materialProperties;
            private MaterialPropertyBlock _backdropProperties;
            private bool _renderMainSurfaceContexts;
            private bool _renderOffscreenContexts;
            private static readonly Vector4 FullBlitScaleBias = new Vector4(1f, 1f, 0f, 0f);
            private const int BackdropCopyPass = 0;
            private const int BackdropBlurPass = 1;
            private const int BackdropCompositePass = 2;
            private const MeshUpdateFlags NoMeshChecks = MeshUpdateFlags.DontNotifyMeshUsers |
                MeshUpdateFlags.DontRecalculateBounds |
                MeshUpdateFlags.DontResetBoneBounds |
                MeshUpdateFlags.DontValidateIndices;
            // Skip all checks and validation when updating the mesh.
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
            public FuguiRenderGraphPass(Shader shader, RenderPassEvent passEvent)
            {
                _shader = shader;
                _textureID = Shader.PropertyToID("_Texture");
                _textureIsAlphaID = Shader.PropertyToID("_TextureIsAlpha");
                _blitTextureID = Shader.PropertyToID("_BlitTexture");
                _backdropPrefilterTexelSizeID = Shader.PropertyToID("_FuguiBackdropPrefilterTexelSize");
                _backdropBlurDirectionID = Shader.PropertyToID("_FuguiBackdropBlurDirection");
                _backdropBlurRadiusID = Shader.PropertyToID("_FuguiBackdropBlurRadius");
                _backdropScreenSizeID = Shader.PropertyToID("_FuguiBackdropScreenSize");
                _backdropRenderOffsetID = Shader.PropertyToID("_FuguiBackdropRenderOffset");
                _backdropCompositeTexelSizeID = Shader.PropertyToID("_FuguiBackdropCompositeTexelSize");
                _materialProperties = new MaterialPropertyBlock();
                _backdropProperties = new MaterialPropertyBlock();
                _prevSubMeshCounts = new Dictionary<int, int>();
                _prevVertexCounts = new Dictionary<int, int>();
                _prevIndexCounts = new Dictionary<int, int>();
                _subMeshDescriptors = new Dictionary<int, List<SubMeshDescriptor>>();
                _targetHandles = new Dictionary<int, RTHandle>();
                _transientMeshes = new Dictionary<int, List<DrawListMesh>>();

                renderPassEvent = passEvent;

                // IMPORTANT : aucune dépendance implicite (depth/normals, etc.)
                ConfigureInput(ScriptableRenderPassInput.None);

                // (optionnel mais propre) évite les allocs répétées
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
            /// Returns whether a context needs the backdrop blur render path this frame.
            /// </summary>
            /// <param name="drawData">Draw data to inspect.</param>
            /// <returns>True when at least one generic backdrop blur command is present.</returns>
            private bool CanUseBackdropBlur(DrawData drawData)
            {
#if FU_BACKDROP_ENABLED
                return ContainsBackdropCommand(drawData) && GetBackdropMaterial() != null;
#else
                return false;
#endif
            }

            /// <summary>
            /// Adds an unsafe render graph pass capable of sampling the current target while drawing Fugui.
            /// </summary>
            /// <param name="renderGraph">Render graph to record into.</param>
            /// <param name="passName">Pass name.</param>
            /// <param name="target">Color target.</param>
            /// <param name="context">Fugui context to render.</param>
            /// <param name="clearTarget">Whether the target must be cleared before drawing.</param>
            private void AddUnsafeFuguiPass(RenderGraph renderGraph, string passName, TextureHandle target, FuUnityContext context, bool clearTarget)
            {
                Vector2 fbSize = context.DrawData.DisplaySize * context.DrawData.FramebufferScale;
                int downsample = GetBackdropDownsample(context.DrawData);
                int blurWidth = Mathf.Max(1, Mathf.CeilToInt(fbSize.x / downsample));
                int blurHeight = Mathf.Max(1, Mathf.CeilToInt(fbSize.y / downsample));

                TextureDesc blurDesc = new TextureDesc(blurWidth, blurHeight)
                {
                    colorFormat = GraphicsFormat.R8G8B8A8_UNorm,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    msaaSamples = MSAASamples.None,
                    depthBufferBits = DepthBits.None,
                    name = passName + "_BackdropA"
                };

                using var builder = renderGraph.AddUnsafePass<PassData>(passName, out var passData);
                builder.AllowGlobalStateModification(true);
                builder.UseTexture(target, AccessFlags.ReadWrite);

                passData.Context = context;
                passData.Target = target;
                passData.ClearTarget = clearTarget;
                passData.Downsample = downsample;
                passData.BlurWidth = blurWidth;
                passData.BlurHeight = blurHeight;
                passData.BackdropA = builder.CreateTransientTexture(blurDesc);
                blurDesc.name = passName + "_BackdropB";
                passData.BackdropB = builder.CreateTransientTexture(blurDesc);

                builder.SetRenderFunc((PassData data, UnsafeGraphContext ctx) =>
                {
                    if (data.Context == null || data.Context.DrawData.CmdListsCount <= 0 || data.Context.DrawData.TotalVtxCount <= 0)
                    {
                        return;
                    }

                    ctx.cmd.SetRenderTarget(data.Target);
                    if (data.ClearTarget)
                    {
                        ctx.cmd.ClearRenderTarget(false, true, Color.clear);
                    }

                    _textureManager = data.Context.TextureManager;
                    RenderDrawLists(data.Context.ID, ctx.cmd, data.Context.DrawData, data.Target, data.BackdropA, data.BackdropB, data.Downsample, data.BlurWidth, data.BlurHeight);
                });
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
                    if (CanUseBackdropBlur(defaultContext.DrawData))
                    {
                        AddUnsafeFuguiPass(renderGraph, "Fugui_MainPass", color, defaultContext, false);
                    }
                    else
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

                        if (CanUseBackdropBlur(unityContext.DrawData))
                        {
                            AddUnsafeFuguiPass(renderGraph, $"Fugui_Offscreen_{unityContext.ID}", importedTarget, unityContext, true);
                        }
                        else
                        {
                            using var builder = renderGraph.AddRasterRenderPass<PassData>($"Fugui_Offscreen_{unityContext.ID}", out var passData);
                            builder.AllowGlobalStateModification(true);
                            builder.SetRenderAttachment(importedTarget, 0, AccessFlags.Write);

                            builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                            {
                                //DebugDumpDrawData("OFFSCREEN", unityContext.DrawData);
                                ctx.cmd.ClearRenderTarget(false, true, Color.clear);
                                _textureManager = unityContext.TextureManager;
                                RenderDrawLists(unityContext.ID, ctx.cmd, unityContext.DrawData);
                            });
                        }
                    }
                    else
                    {
                        if (!_renderMainSurfaceContexts)
                        {
                            continue;
                        }

                        if (CanUseBackdropBlur(unityContext.DrawData))
                        {
                            AddUnsafeFuguiPass(renderGraph, $"Fugui_Context_{unityContext.ID}", color, unityContext, false);
                        }
                        else
                        {
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
            }

            /// <summary>
            /// Returns whether the draw data contains at least one Fugui backdrop draw command.
            /// </summary>
            /// <param name="drawData">Draw data to inspect.</param>
            /// <returns>True when a backdrop command is present.</returns>
            private bool ContainsBackdropCommand(DrawData drawData)
            {
                if (drawData == null)
                {
                    return false;
                }

                if (drawData.RenderItems != null && drawData.RenderItems.Count > 0)
                {
                    for (int i = 0; i < drawData.RenderItems.Count; i++)
                    {
                        if (ContainsBackdropCommand(drawData.RenderItems[i].DrawLists))
                        {
                            return true;
                        }
                    }
                    return false;
                }

                return ContainsBackdropCommand(drawData.DrawLists);
            }

            /// <summary>
            /// Returns whether any draw list contains a Fugui backdrop draw command.
            /// </summary>
            /// <param name="drawLists">Draw lists to inspect.</param>
            /// <returns>True when a backdrop command is present.</returns>
            private bool ContainsBackdropCommand(IReadOnlyList<DrawList> drawLists)
            {
                if (drawLists == null)
                {
                    return false;
                }

                for (int n = 0; n < drawLists.Count; n++)
                {
                    DrawList drawList = drawLists[n];
                    if (drawList == null || drawList.CmdBuffer == null)
                    {
                        continue;
                    }

                    ImDrawCmd[] commands = drawList.CmdBuffer;
                    for (int i = 0; i < commands.Length; i++)
                    {
                        if (Fugui.IsBackdropTextureID(commands[i].TextureId))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            /// <summary>
            /// Returns the runtime backdrop blur material.
            /// </summary>
            /// <returns>Backdrop material, or null when the shader is unavailable.</returns>
            private Material GetBackdropMaterial()
            {
                if (_backdropMaterial != null)
                {
                    return _backdropMaterial;
                }

                Shader shader = Shader.Find("Fugui/BackdropBlur");
                if (shader == null)
                {
                    shader = Resources.Load<Shader>("Shaders/Fugui_BackdropBlur");
                }

                if (shader == null)
                {
                    return null;
                }

                _backdropMaterial = new Material(shader)
                {
                    hideFlags = HideFlags.HideAndDontSave & ~HideFlags.DontUnloadUnusedAsset
                };
                return _backdropMaterial;
            }

            /// <summary>
            /// Returns the adaptive backdrop blur downsample value.
            /// </summary>
            /// <returns>Clamped downsample value.</returns>
            private int GetBackdropDownsample(DrawData drawData)
            {
                float maxBlurRadius = GetMaxBackdropBlurRadius(drawData);
                int maxDownsample = GetBackdropMaxDownsample();
                if (maxBlurRadius <= GetBackdropFullResolutionMaxRadius() || maxDownsample <= 1)
                {
                    return 1;
                }

                if (maxBlurRadius <= GetBackdropHalfResolutionMaxRadius() || maxDownsample <= 2)
                {
                    return Mathf.Min(2, maxDownsample);
                }

                if (maxBlurRadius <= GetBackdropThirdResolutionMaxRadius() || maxDownsample <= 3)
                {
                    return Mathf.Min(3, maxDownsample);
                }

                return Mathf.Min(4, maxDownsample);
            }

            /// <summary>
            /// Returns the largest blur radius encoded in the draw data.
            /// </summary>
            private float GetMaxBackdropBlurRadius(DrawData drawData)
            {
                if (drawData == null)
                {
                    return 0f;
                }

                if (drawData.RenderItems != null && drawData.RenderItems.Count > 0)
                {
                    float maxRadius = 0f;
                    for (int i = 0; i < drawData.RenderItems.Count; i++)
                    {
                        maxRadius = Mathf.Max(maxRadius, GetMaxBackdropBlurRadius(drawData.RenderItems[i].DrawLists));
                    }
                    return maxRadius;
                }

                return GetMaxBackdropBlurRadius(drawData.DrawLists);
            }

            /// <summary>
            /// Returns the largest blur radius encoded in the draw lists.
            /// </summary>
            private float GetMaxBackdropBlurRadius(IReadOnlyList<DrawList> drawLists)
            {
                if (drawLists == null)
                {
                    return 0f;
                }

                float maxRadius = 0f;
                for (int n = 0; n < drawLists.Count; n++)
                {
                    DrawList drawList = drawLists[n];
                    if (drawList == null || drawList.CmdBuffer == null)
                    {
                        continue;
                    }

                    ImDrawCmd[] commands = drawList.CmdBuffer;
                    for (int i = 0; i < commands.Length; i++)
                    {
                        ImDrawCmd command = commands[i];
                        if (Fugui.IsBackdropTextureID(command.TextureId))
                        {
                            maxRadius = Mathf.Max(maxRadius, GetBackdropBlurRadius(drawList, command));
                        }
                    }
                }

                return maxRadius;
            }

            /// <summary>
            /// Renders the draw lists using the provided command buffer and draw data.
            /// </summary>
            /// <param name="commandBuffer"> The command buffer to use for rendering.</param>
            /// <param name="drawData"></ param>
            public void RenderDrawLists(int ctxId, RasterCommandBuffer commandBuffer, DrawData drawData)
            {
                // ensure material exists for this context
                if (_materials == null) _materials = new Dictionary<int, Material>();
                if (!_materials.ContainsKey(ctxId))
                {
                    _materials[ctxId] = new Material(_shader)
                    {
                        hideFlags = HideFlags.HideAndDontSave & ~HideFlags.DontUnloadUnusedAsset
                    };
                }
                Material _material = _materials[ctxId];

                Vector2 fbOSize = drawData.DisplaySize * drawData.FramebufferScale;

                // Avoid rendering when minimized.
                if (fbOSize.x <= 0f || fbOSize.y <= 0f || drawData.TotalVtxCount == 0) return;

                commandBuffer.BeginSample(_sampleName);
                RenderDrawItems(ctxId, commandBuffer, _material, drawData, fbOSize);
                commandBuffer.EndSample(_sampleName);
            }

            /// <summary>
            /// Renders the draw lists through an unsafe command buffer so backdrop commands can sample the current target.
            /// </summary>
            /// <param name="ctxId">Context id.</param>
            /// <param name="commandBuffer">Unsafe command buffer to use for rendering.</param>
            /// <param name="drawData">Draw data to render.</param>
            /// <param name="target">Color target currently being rendered.</param>
            /// <param name="backdropA">First temporary blur texture.</param>
            /// <param name="backdropB">Second temporary blur texture.</param>
            /// <param name="downsample">Backdrop texture downsample value.</param>
            /// <param name="blurWidth">Backdrop texture width.</param>
            /// <param name="blurHeight">Backdrop texture height.</param>
            public void RenderDrawLists(int ctxId, UnsafeCommandBuffer commandBuffer, DrawData drawData, TextureHandle target, TextureHandle backdropA, TextureHandle backdropB, int downsample, int blurWidth, int blurHeight)
            {
                if (_materials == null) _materials = new Dictionary<int, Material>();
                if (!_materials.ContainsKey(ctxId))
                {
                    _materials[ctxId] = new Material(_shader)
                    {
                        hideFlags = HideFlags.HideAndDontSave & ~HideFlags.DontUnloadUnusedAsset
                    };
                }
                Material _material = _materials[ctxId];

                Vector2 fbOSize = drawData.DisplaySize * drawData.FramebufferScale;

                // Avoid rendering when minimized.
                if (fbOSize.x <= 0f || fbOSize.y <= 0f || drawData.TotalVtxCount == 0) return;

                commandBuffer.BeginSample(_sampleName);
                RenderDrawItems(ctxId, commandBuffer, _material, drawData, fbOSize, target, backdropA, backdropB, downsample, blurWidth, blurHeight);
                commandBuffer.EndSample(_sampleName);
            }

            /// <summary>
            /// Renders cached window meshes and transient non-window draw lists in draw order.
            /// </summary>
            /// <param name="ctxId">Context id.</param>
            /// <param name="commandBuffer">Command buffer to use for rendering.</param>
            /// <param name="material">Material used to render Fugui meshes.</param>
            /// <param name="drawData">Draw data for the context.</param>
            /// <param name="fbSize">Framebuffer size.</param>
            private void RenderDrawItems(int ctxId, RasterCommandBuffer commandBuffer, Material material, DrawData drawData, Vector2 fbSize)
            {
                int transientMeshIndex = 0;

                if (drawData.RenderItems == null || drawData.RenderItems.Count == 0)
                {
                    DrawListMesh fallbackMesh = GetOrCreateTransientMesh(ctxId, transientMeshIndex);
                    fallbackMesh.Update(drawData.DrawLists, drawData.DisplaySize, drawData.FramebufferScale);
                    CreateDrawCommands(fallbackMesh.Mesh, material, commandBuffer, drawData.DrawLists, drawData, fbSize, Vector2.zero);
                    return;
                }

                for (int i = 0; i < drawData.RenderItems.Count; i++)
                {
                    DrawDataRenderItem item = drawData.RenderItems[i];
                    IReadOnlyList<DrawList> drawLists = item.DrawLists;
                    if (drawLists == null || drawLists.Count == 0)
                    {
                        continue;
                    }

                    DrawListMesh meshData;
                    Vector2 renderOffset = Vector2.zero;
                    if (item.IsWindow)
                    {
                        meshData = item.Window.RenderMeshData;
                        renderOffset = item.Window.RenderMeshOffset;
                    }
                    else
                    {
                        meshData = GetOrCreateTransientMesh(ctxId, transientMeshIndex);
                        transientMeshIndex++;
                        meshData.Update(drawLists, drawData.DisplaySize, drawData.FramebufferScale);
                    }

                    if (meshData == null || meshData.Mesh == null || meshData.SubMeshCount == 0 || meshData.TotalVtxCount == 0)
                    {
                        continue;
                    }

                    CreateDrawCommands(meshData.Mesh, material, commandBuffer, drawLists, drawData, fbSize, renderOffset);
                }
            }

            /// <summary>
            /// Renders cached window meshes and transient non-window draw lists in draw order through an unsafe pass.
            /// </summary>
            /// <param name="ctxId">Context id.</param>
            /// <param name="commandBuffer">Unsafe command buffer to use for rendering.</param>
            /// <param name="material">Material used to render Fugui meshes.</param>
            /// <param name="drawData">Draw data for the context.</param>
            /// <param name="fbSize">Framebuffer size.</param>
            /// <param name="target">Color target currently being rendered.</param>
            /// <param name="backdropA">First temporary blur texture.</param>
            /// <param name="backdropB">Second temporary blur texture.</param>
            /// <param name="downsample">Backdrop texture downsample value.</param>
            /// <param name="blurWidth">Backdrop texture width.</param>
            /// <param name="blurHeight">Backdrop texture height.</param>
            private void RenderDrawItems(int ctxId, UnsafeCommandBuffer commandBuffer, Material material, DrawData drawData, Vector2 fbSize, TextureHandle target, TextureHandle backdropA, TextureHandle backdropB, int downsample, int blurWidth, int blurHeight)
            {
                int transientMeshIndex = 0;

                if (drawData.RenderItems == null || drawData.RenderItems.Count == 0)
                {
                    DrawListMesh fallbackMesh = GetOrCreateTransientMesh(ctxId, transientMeshIndex);
                    fallbackMesh.Update(drawData.DrawLists, drawData.DisplaySize, drawData.FramebufferScale);
                    CreateDrawCommands(fallbackMesh.Mesh, material, commandBuffer, drawData.DrawLists, drawData, fbSize, Vector2.zero, target, backdropA, backdropB, downsample, blurWidth, blurHeight);
                    return;
                }

                for (int i = 0; i < drawData.RenderItems.Count; i++)
                {
                    DrawDataRenderItem item = drawData.RenderItems[i];
                    IReadOnlyList<DrawList> drawLists = item.DrawLists;
                    if (drawLists == null || drawLists.Count == 0)
                    {
                        continue;
                    }

                    DrawListMesh meshData;
                    Vector2 renderOffset = Vector2.zero;
                    if (item.IsWindow)
                    {
                        meshData = item.Window.RenderMeshData;
                        renderOffset = item.Window.RenderMeshOffset;
                    }
                    else
                    {
                        meshData = GetOrCreateTransientMesh(ctxId, transientMeshIndex);
                        transientMeshIndex++;
                        meshData.Update(drawLists, drawData.DisplaySize, drawData.FramebufferScale);
                    }

                    if (meshData == null || meshData.Mesh == null || meshData.SubMeshCount == 0 || meshData.TotalVtxCount == 0)
                    {
                        continue;
                    }

                    CreateDrawCommands(meshData.Mesh, material, commandBuffer, drawLists, drawData, fbSize, renderOffset, target, backdropA, backdropB, downsample, blurWidth, blurHeight);
                }
            }

            /// <summary>
            /// Gets a reusable mesh for non-window draw lists.
            /// </summary>
            /// <param name="ctxId">Context id.</param>
            /// <param name="meshIndex">Transient mesh index for this frame.</param>
            /// <returns>Reusable mesh cache.</returns>
            private DrawListMesh GetOrCreateTransientMesh(int ctxId, int meshIndex)
            {
                if (_transientMeshes == null)
                {
                    _transientMeshes = new Dictionary<int, List<DrawListMesh>>();
                }

                if (!_transientMeshes.TryGetValue(ctxId, out List<DrawListMesh> meshes))
                {
                    meshes = new List<DrawListMesh>();
                    _transientMeshes.Add(ctxId, meshes);
                }

                while (meshes.Count <= meshIndex)
                {
                    meshes.Add(new DrawListMesh("FuguiTransientMesh_" + ctxId + "_" + meshes.Count));
                }

                return meshes[meshIndex];
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
            private void CreateDrawCommands(Mesh _mesh, Material _material, RasterCommandBuffer commandBuffer, IReadOnlyList<DrawList> drawLists, DrawData drawData, Vector2 fbSize, Vector2 renderOffset)
            {
                IntPtr prevTextureId = IntPtr.Zero;
                Vector4 clipOffset = new Vector4(drawData.DisplayPos.x, drawData.DisplayPos.y,
                    drawData.DisplayPos.x, drawData.DisplayPos.y);
                Vector4 clipScale = new Vector4(drawData.FramebufferScale.x, drawData.FramebufferScale.y,
                    drawData.FramebufferScale.x, drawData.FramebufferScale.y);
                Vector4 clipRenderOffset = new Vector4(renderOffset.x, renderOffset.y, renderOffset.x, renderOffset.y);
                Matrix4x4 meshMatrix = renderOffset == Vector2.zero
                    ? Matrix4x4.identity
                    : Matrix4x4.Translate(new Vector3(renderOffset.x, renderOffset.y, 0f));

                commandBuffer.SetViewport(new Rect(0f, 0f, fbSize.x, fbSize.y));
                commandBuffer.SetViewProjectionMatrices(
                    Matrix4x4.Translate(new Vector3(0.5f / fbSize.x, 0.5f / fbSize.y, 0f)), // Small adjustment to improve text.
                    Matrix4x4.Ortho(0f, fbSize.x, fbSize.y, 0f, 0f, 1f));

                int subOf = 0;
                for (int n = 0, nMax = drawLists.Count; n < nMax; ++n)
                {
                    DrawList drawList = drawLists[n];
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
                            Vector4 clipSize = drawCmd.ClipRect + clipRenderOffset - clipOffset;
                            Vector4 clip = Vector4.Scale(clipSize, clipScale);

                            if (clip.x >= fbSize.x || clip.y >= fbSize.y || clip.z < 0f || clip.w < 0f) continue;

                            if (prevTextureId != drawCmd.TextureId)
                            {
                                prevTextureId = drawCmd.TextureId;

                                if (Fugui.IsBackdropTextureID(prevTextureId))
                                {
                                    _materialProperties.SetTexture(_textureID, Texture2D.whiteTexture);
                                    _materialProperties.SetFloat(_textureIsAlphaID, 0f);
                                }
                                else
                                {
                                    // TODO: Implement ImDrawCmdPtr.GetTexID().
                                    bool hasTexture = _textureManager.TryGetTexture(prevTextureId, out UnityEngine.Texture texture);

                                    //Assert.IsTrue(hasTexture, $"Texture {prevTextureId} does not exist. Try to use UImGuiUtility.GetTextureID().");
                                    if (!hasTexture)
                                    {
                                        _materialProperties.SetFloat(_textureIsAlphaID, 0f);
                                        Debug.LogError($"Texture {prevTextureId} does not exist. Try to use UImGuiUtility.GetTextureID().");
                                    }
                                    else
                                    {
                                        if (texture && texture != null)
                                        {
                                            _materialProperties.SetTexture(_textureID, texture);
                                            _materialProperties.SetFloat(_textureIsAlphaID, _textureManager != null && _textureManager.IsFontAtlasTexture(texture) ? 1f : 0f);
                                        }
                                        else
                                        {
                                            _materialProperties.SetFloat(_textureIsAlphaID, 0f);
                                            Debug.LogWarning($"Texture {prevTextureId} is null or not a valid texture.");
                                        }
                                    }
                                }
                            }
                            commandBuffer.EnableScissorRect(new Rect(clip.x, fbSize.y - clip.w, clip.z - clip.x, clip.w - clip.y)); // Invert y.
                            commandBuffer.DrawMesh(_mesh, meshMatrix, _material, subOf, 0, _materialProperties);
                        }
                    }
                }
                commandBuffer.DisableScissorRect();
            }

            /// <summary>
            /// Creates the draw commands for rendering Fugui through an unsafe pass.
            /// </summary>
            /// <param name="_mesh">Mesh to render.</param>
            /// <param name="_material">Material to render regular Fugui commands.</param>
            /// <param name="commandBuffer">Unsafe command buffer to use for rendering.</param>
            /// <param name="drawLists">Draw lists to render.</param>
            /// <param name="drawData">Draw data containing framebuffer information.</param>
            /// <param name="fbSize">Framebuffer size.</param>
            /// <param name="renderOffset">Additional mesh render offset.</param>
            /// <param name="target">Color target currently being rendered.</param>
            /// <param name="backdropA">First temporary blur texture.</param>
            /// <param name="backdropB">Second temporary blur texture.</param>
            /// <param name="downsample">Backdrop texture downsample value.</param>
            /// <param name="blurWidth">Backdrop texture width.</param>
            /// <param name="blurHeight">Backdrop texture height.</param>
            private void CreateDrawCommands(Mesh _mesh, Material _material, UnsafeCommandBuffer commandBuffer, IReadOnlyList<DrawList> drawLists, DrawData drawData, Vector2 fbSize, Vector2 renderOffset, TextureHandle target, TextureHandle backdropA, TextureHandle backdropB, int downsample, int blurWidth, int blurHeight)
            {
                IntPtr prevTextureId = IntPtr.Zero;
                Vector4 clipOffset = new Vector4(drawData.DisplayPos.x, drawData.DisplayPos.y,
                    drawData.DisplayPos.x, drawData.DisplayPos.y);
                Vector4 clipScale = new Vector4(drawData.FramebufferScale.x, drawData.FramebufferScale.y,
                    drawData.FramebufferScale.x, drawData.FramebufferScale.y);
                Vector4 clipRenderOffset = new Vector4(renderOffset.x, renderOffset.y, renderOffset.x, renderOffset.y);
                Matrix4x4 meshMatrix = renderOffset == Vector2.zero
                    ? Matrix4x4.identity
                    : Matrix4x4.Translate(new Vector3(renderOffset.x, renderOffset.y, 0f));

                SetUiRenderState(commandBuffer, target, fbSize);

                int subOf = 0;
                for (int n = 0, nMax = drawLists.Count; n < nMax; ++n)
                {
                    DrawList drawList = drawLists[n];
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
                            Vector4 clipSize = drawCmd.ClipRect + clipRenderOffset - clipOffset;
                            Vector4 clip = Vector4.Scale(clipSize, clipScale);

                            if (clip.x >= fbSize.x || clip.y >= fbSize.y || clip.z < 0f || clip.w < 0f) continue;

                            bool isBackdrop = Fugui.IsBackdropTextureID(drawCmd.TextureId);
                            if (isBackdrop && _backdropMaterial != null && backdropA.IsValid() && backdropB.IsValid())
                            {
                                DrawBackdropCommand(commandBuffer, _mesh, meshMatrix, subOf, drawList, drawCmd, drawData, fbSize, renderOffset, clip, target, backdropA, backdropB, downsample, blurWidth, blurHeight);
                                prevTextureId = IntPtr.Zero;
                                continue;
                            }

                            if (prevTextureId != drawCmd.TextureId)
                            {
                                prevTextureId = drawCmd.TextureId;

                                if (isBackdrop)
                                {
                                    _materialProperties.SetTexture(_textureID, Texture2D.whiteTexture);
                                    _materialProperties.SetFloat(_textureIsAlphaID, 0f);
                                }
                                else
                                {
                                    bool hasTexture = _textureManager.TryGetTexture(prevTextureId, out UnityEngine.Texture texture);

                                    if (!hasTexture)
                                    {
                                        _materialProperties.SetFloat(_textureIsAlphaID, 0f);
                                        Debug.LogError($"Texture {prevTextureId} does not exist. Try to use UImGuiUtility.GetTextureID().");
                                    }
                                    else
                                    {
                                        if (texture && texture != null)
                                        {
                                            _materialProperties.SetTexture(_textureID, texture);
                                            _materialProperties.SetFloat(_textureIsAlphaID, _textureManager != null && _textureManager.IsFontAtlasTexture(texture) ? 1f : 0f);
                                        }
                                        else
                                        {
                                            _materialProperties.SetFloat(_textureIsAlphaID, 0f);
                                            Debug.LogWarning($"Texture {prevTextureId} is null or not a valid texture.");
                                        }
                                    }
                                }
                            }
                            commandBuffer.EnableScissorRect(new Rect(clip.x, fbSize.y - clip.w, clip.z - clip.x, clip.w - clip.y)); // Invert y.
                            commandBuffer.DrawMesh(_mesh, meshMatrix, _material, subOf, 0, _materialProperties);
                        }
                    }
                }
                commandBuffer.DisableScissorRect();
            }

            /// <summary>
            /// Draws a single backdrop blur command.
            /// </summary>
            private void DrawBackdropCommand(UnsafeCommandBuffer commandBuffer, Mesh mesh, Matrix4x4 meshMatrix, int subMesh, DrawList drawList, ImDrawCmd drawCmd, DrawData drawData, Vector2 fbSize, Vector2 renderOffset, Vector4 clip, TextureHandle target, TextureHandle backdropA, TextureHandle backdropB, int downsample, int blurWidth, int blurHeight)
            {
                float blurRadius = GetBackdropBlurRadius(drawList, drawCmd);
                float framebufferScale = Mathf.Max(drawData.FramebufferScale.x, drawData.FramebufferScale.y);
                float scaledBlurRadius = blurRadius * framebufferScale / Mathf.Max(1, downsample);

                commandBuffer.DisableScissorRect();

                commandBuffer.SetRenderTarget(backdropA);
                commandBuffer.SetViewport(new Rect(0f, 0f, blurWidth, blurHeight));
                float prefilterRadius = GetBackdropPrefilterRadius();
                commandBuffer.SetGlobalVector(_backdropPrefilterTexelSizeID, new Vector4(
                    1f / Mathf.Max(1, fbSize.x) * Mathf.Max(1, downsample) * prefilterRadius,
                    1f / Mathf.Max(1, fbSize.y) * Mathf.Max(1, downsample) * prefilterRadius,
                    0f,
                    0f));
                Blitter.BlitTexture(commandBuffer, target, FullBlitScaleBias, _backdropMaterial, BackdropCopyPass);

                if (scaledBlurRadius > 0f)
                {
                    int iterationCount = GetBackdropBlurIterationCount(scaledBlurRadius);
                    float iterationRadius = scaledBlurRadius / Mathf.Sqrt(iterationCount);
                    for (int iteration = 0; iteration < iterationCount; iteration++)
                    {
                        commandBuffer.SetGlobalVector(_backdropBlurDirectionID, new Vector4(1f, 0f, 0f, 0f));
                        commandBuffer.SetGlobalFloat(_backdropBlurRadiusID, iterationRadius);
                        commandBuffer.SetRenderTarget(backdropB);
                        commandBuffer.SetViewport(new Rect(0f, 0f, blurWidth, blurHeight));
                        Blitter.BlitTexture(commandBuffer, backdropA, FullBlitScaleBias, _backdropMaterial, BackdropBlurPass);

                        commandBuffer.SetGlobalVector(_backdropBlurDirectionID, new Vector4(0f, 1f, 0f, 0f));
                        commandBuffer.SetGlobalFloat(_backdropBlurRadiusID, iterationRadius);
                        commandBuffer.SetRenderTarget(backdropA);
                        commandBuffer.SetViewport(new Rect(0f, 0f, blurWidth, blurHeight));
                        Blitter.BlitTexture(commandBuffer, backdropB, FullBlitScaleBias, _backdropMaterial, BackdropBlurPass);
                    }
                }

                SetUiRenderState(commandBuffer, target, fbSize);
                commandBuffer.SetGlobalTexture(_blitTextureID, backdropA);
                commandBuffer.SetGlobalVector(_backdropScreenSizeID, new Vector4(fbSize.x, fbSize.y, 0f, 0f));
                commandBuffer.SetGlobalVector(_backdropRenderOffsetID, new Vector4(renderOffset.x, renderOffset.y, 0f, 0f));
                float compositeFilterRadius = GetBackdropCompositeFilterRadius();
                commandBuffer.SetGlobalVector(_backdropCompositeTexelSizeID, new Vector4(
                    compositeFilterRadius / Mathf.Max(1, blurWidth),
                    compositeFilterRadius / Mathf.Max(1, blurHeight),
                    0f,
                    0f));
                commandBuffer.EnableScissorRect(new Rect(clip.x, fbSize.y - clip.w, clip.z - clip.x, clip.w - clip.y)); // Invert y.
                commandBuffer.DrawMesh(mesh, meshMatrix, _backdropMaterial, subMesh, BackdropCompositePass, _backdropProperties);
            }

            /// <summary>
            /// Returns how many moderate passes should approximate the requested blur radius.
            /// </summary>
            private int GetBackdropBlurIterationCount(float scaledBlurRadius)
            {
                return Mathf.Clamp(Mathf.CeilToInt(scaledBlurRadius / GetBackdropTargetPassRadius()), 1, GetBackdropMaxIterations());
            }

            /// <summary>
            /// Returns the current backdrop renderer max downsample.
            /// </summary>
            private int GetBackdropMaxDownsample()
            {
                return Fugui.Themes != null ? Mathf.Clamp(Fugui.Themes.BackdropBlurMaxDownsample, 1, 8) : 4;
            }

            /// <summary>
            /// Returns the blur radius under which the backdrop texture stays full resolution.
            /// </summary>
            private float GetBackdropFullResolutionMaxRadius()
            {
                return Fugui.Themes != null ? Mathf.Max(1f, Fugui.Themes.BackdropBlurFullResolutionMaxRadius) : 24f;
            }

            /// <summary>
            /// Returns the blur radius under which the backdrop texture uses half resolution.
            /// </summary>
            private float GetBackdropHalfResolutionMaxRadius()
            {
                return Fugui.Themes != null ? Mathf.Max(GetBackdropFullResolutionMaxRadius(), Fugui.Themes.BackdropBlurHalfResolutionMaxRadius) : 64f;
            }

            /// <summary>
            /// Returns the blur radius under which the backdrop texture uses third resolution.
            /// </summary>
            private float GetBackdropThirdResolutionMaxRadius()
            {
                return Fugui.Themes != null ? Mathf.Max(GetBackdropHalfResolutionMaxRadius(), Fugui.Themes.BackdropBlurThirdResolutionMaxRadius) : 96f;
            }

            /// <summary>
            /// Returns the current backdrop blur max iteration count.
            /// </summary>
            private int GetBackdropMaxIterations()
            {
                return Fugui.Themes != null ? Mathf.Clamp(Fugui.Themes.BackdropBlurMaxIterations, 1, 12) : 6;
            }

            /// <summary>
            /// Returns the target radius for each iterative blur pass.
            /// </summary>
            private float GetBackdropTargetPassRadius()
            {
                return Fugui.Themes != null ? Mathf.Max(0.5f, Fugui.Themes.BackdropBlurTargetPassRadius) : 4f;
            }

            /// <summary>
            /// Returns the prefilter radius used while copying into the backdrop texture.
            /// </summary>
            private float GetBackdropPrefilterRadius()
            {
                return Fugui.Themes != null ? Mathf.Max(0.01f, Fugui.Themes.BackdropBlurPrefilterRadius) : 0.5f;
            }

            /// <summary>
            /// Returns the final composite filter radius.
            /// </summary>
            private float GetBackdropCompositeFilterRadius()
            {
                return Fugui.Themes != null ? Mathf.Max(0.01f, Fugui.Themes.BackdropBlurCompositeFilterRadius) : 1.0f;
            }

            /// <summary>
            /// Returns the blur radius encoded in a backdrop draw command.
            /// </summary>
            private float GetBackdropBlurRadius(DrawList drawList, ImDrawCmd drawCmd)
            {
                if (drawList == null || drawList.IdxBuffer == null || drawList.VtxBuffer == null || drawCmd.ElemCount == 0)
                {
                    return 0f;
                }

                int idx = (int)drawCmd.IdxOffset;
                if (idx < 0 || idx >= drawList.IdxBuffer.Length)
                {
                    return 0f;
                }

                int vtx = drawList.IdxBuffer[idx] + (int)drawCmd.VtxOffset;
                if (vtx < 0 || vtx >= drawList.VtxBuffer.Length)
                {
                    return 0f;
                }

                return Mathf.Max(0f, drawList.VtxBuffer[vtx].uv.x);
            }

            /// <summary>
            /// Restores the Fugui draw target, viewport, and projection after backdrop blits.
            /// </summary>
            private void SetUiRenderState(UnsafeCommandBuffer commandBuffer, TextureHandle target, Vector2 fbSize)
            {
                commandBuffer.SetRenderTarget(target);
                commandBuffer.SetViewport(new Rect(0f, 0f, fbSize.x, fbSize.y));
                commandBuffer.SetViewProjectionMatrices(
                    Matrix4x4.Translate(new Vector3(0.5f / fbSize.x, 0.5f / fbSize.y, 0f)), // Small adjustment to improve text.
                    Matrix4x4.Ortho(0f, fbSize.x, fbSize.y, 0f, 0f, 1f));
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
                // ensure material exists for this context
                if (_materials == null) _materials = new Dictionary<int, Material>();
                if (!_materials.ContainsKey(ctxId))
                {
                    _materials[ctxId] = new Material(_shader)
                    {
                        hideFlags = HideFlags.HideAndDontSave & ~HideFlags.DontUnloadUnusedAsset
                    };
                }
                Material _material = _materials[ctxId];

                Vector2 fbOSize = drawData.DisplaySize * drawData.FramebufferScale;

                // Avoid rendering when minimized.
                if (fbOSize.x <= 0f || fbOSize.y <= 0f || drawData.TotalVtxCount == 0) return;

                commandBuffer.BeginSample(_sampleName);
                RenderDrawItems(ctxId, commandBuffer, _material, drawData, fbOSize);
                commandBuffer.EndSample(_sampleName);
            }

            /// <summary>
            /// Renders cached window meshes and transient non-window draw lists in draw order.
            /// </summary>
            /// <param name="ctxId">Context id.</param>
            /// <param name="commandBuffer">Command buffer to use for rendering.</param>
            /// <param name="material">Material used to render Fugui meshes.</param>
            /// <param name="drawData">Draw data for the context.</param>
            /// <param name="fbSize">Framebuffer size.</param>
            private void RenderDrawItems(int ctxId, CommandBuffer commandBuffer, Material material, DrawData drawData, Vector2 fbSize)
            {
                int transientMeshIndex = 0;

                if (drawData.RenderItems == null || drawData.RenderItems.Count == 0)
                {
                    DrawListMesh fallbackMesh = GetOrCreateTransientMesh(ctxId, transientMeshIndex);
                    fallbackMesh.Update(drawData.DrawLists, drawData.DisplaySize, drawData.FramebufferScale);
                    CreateDrawCommands(fallbackMesh.Mesh, material, commandBuffer, drawData.DrawLists, drawData, fbSize, Vector2.zero);
                    return;
                }

                for (int i = 0; i < drawData.RenderItems.Count; i++)
                {
                    DrawDataRenderItem item = drawData.RenderItems[i];
                    IReadOnlyList<DrawList> drawLists = item.DrawLists;
                    if (drawLists == null || drawLists.Count == 0)
                    {
                        continue;
                    }

                    DrawListMesh meshData;
                    Vector2 renderOffset = Vector2.zero;
                    if (item.IsWindow)
                    {
                        meshData = item.Window.RenderMeshData;
                        renderOffset = item.Window.RenderMeshOffset;
                    }
                    else
                    {
                        meshData = GetOrCreateTransientMesh(ctxId, transientMeshIndex);
                        transientMeshIndex++;
                        meshData.Update(drawLists, drawData.DisplaySize, drawData.FramebufferScale);
                    }

                    if (meshData == null || meshData.Mesh == null || meshData.SubMeshCount == 0 || meshData.TotalVtxCount == 0)
                    {
                        continue;
                    }

                    CreateDrawCommands(meshData.Mesh, material, commandBuffer, drawLists, drawData, fbSize, renderOffset);
                }
            }

            /// <summary>
            /// Creates the draw commands for rendering Fugui using the provided command buffer and draw data.
            /// </summary>
            /// <param name="commandBuffer"> The command buffer to use for rendering.</param>
            /// <param name="drawData"> The draw data containing the information to render.</param>
            /// <param name="fbSize"> The framebuffer size to use for rendering.</param>
            private void CreateDrawCommands(Mesh _mesh, Material _material, CommandBuffer commandBuffer, IReadOnlyList<DrawList> drawLists, DrawData drawData, Vector2 fbSize, Vector2 renderOffset)
            {
                IntPtr prevTextureId = IntPtr.Zero;
                Vector4 clipOffset = new Vector4(drawData.DisplayPos.x, drawData.DisplayPos.y,
                    drawData.DisplayPos.x, drawData.DisplayPos.y);
                Vector4 clipScale = new Vector4(drawData.FramebufferScale.x, drawData.FramebufferScale.y,
                    drawData.FramebufferScale.x, drawData.FramebufferScale.y);
                Vector4 clipRenderOffset = new Vector4(renderOffset.x, renderOffset.y, renderOffset.x, renderOffset.y);
                Matrix4x4 meshMatrix = renderOffset == Vector2.zero
                    ? Matrix4x4.identity
                    : Matrix4x4.Translate(new Vector3(renderOffset.x, renderOffset.y, 0f));

                commandBuffer.SetViewport(new Rect(0f, 0f, fbSize.x, fbSize.y));
                commandBuffer.SetViewProjectionMatrices(
                    Matrix4x4.Translate(new Vector3(0.5f / fbSize.x, 0.5f / fbSize.y, 0f)), // Small adjustment to improve text.
                    Matrix4x4.Ortho(0f, fbSize.x, fbSize.y, 0f, 0f, 1f));

                int subOf = 0;
                for (int n = 0, nMax = drawLists.Count; n < nMax; ++n)
                {
                    DrawList drawList = drawLists[n];
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
                            Vector4 clipSize = drawCmd.ClipRect + clipRenderOffset - clipOffset;
                            Vector4 clip = Vector4.Scale(clipSize, clipScale);

                            if (clip.x >= fbSize.x || clip.y >= fbSize.y || clip.z < 0f || clip.w < 0f) continue;

                            if (prevTextureId != drawCmd.TextureId)
                            {
                                prevTextureId = drawCmd.TextureId;

                                if (Fugui.IsBackdropTextureID(prevTextureId))
                                {
                                    _materialProperties.SetTexture(_textureID, Texture2D.whiteTexture);
                                    _materialProperties.SetFloat(_textureIsAlphaID, 0f);
                                }
                                else
                                {
                                    // TODO: Implement ImDrawCmdPtr.GetTexID().
                                    bool hasTexture = _textureManager.TryGetTexture(prevTextureId, out UnityEngine.Texture texture);

                                    //Assert.IsTrue(hasTexture, $"Texture {prevTextureId} does not exist. Try to use UImGuiUtility.GetTextureID().");
                                    if (!hasTexture)
                                    {
                                        _materialProperties.SetFloat(_textureIsAlphaID, 0f);
                                        Debug.LogError($"Texture {prevTextureId} does not exist. Try to use UImGuiUtility.GetTextureID().");
                                    }
                                    else
                                    {
                                        if (texture && texture != null)
                                        {
                                            _materialProperties.SetTexture(_textureID, texture);
                                            _materialProperties.SetFloat(_textureIsAlphaID, _textureManager != null && _textureManager.IsFontAtlasTexture(texture) ? 1f : 0f);
                                        }
                                        else
                                        {
                                            _materialProperties.SetFloat(_textureIsAlphaID, 0f);
                                            Debug.LogWarning($"Texture {prevTextureId} is null or not a valid texture.");
                                        }
                                    }
                                }
                            }
                            commandBuffer.EnableScissorRect(new Rect(clip.x, fbSize.y - clip.w, clip.z - clip.x, clip.w - clip.y)); // Invert y.
                            commandBuffer.DrawMesh(_mesh, meshMatrix, _material, subOf, 0, _materialProperties);
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

                bool meshLayoutChanged = prevSubMeshCount != subMeshCount;
                if (meshLayoutChanged)
                {
                    _mesh.Clear(true);
                    _mesh.subMeshCount = subMeshCount;
                    _prevSubMeshCounts[ctxId] = subMeshCount;
                }

                if (!_prevVertexCounts.TryGetValue(ctxId, out int prevVertexCount) || prevVertexCount != drawData.TotalVtxCount || meshLayoutChanged)
                {
                    _mesh.SetVertexBufferParams(drawData.TotalVtxCount, _vertexAttributes);
                    _prevVertexCounts[ctxId] = drawData.TotalVtxCount;
                }

                if (!_prevIndexCounts.TryGetValue(ctxId, out int prevIndexCount) || prevIndexCount != drawData.TotalIdxCount || meshLayoutChanged)
                {
                    _mesh.SetIndexBufferParams(drawData.TotalIdxCount, IndexFormat.UInt16);
                    _prevIndexCounts[ctxId] = drawData.TotalIdxCount;
                }

                int vtxOf = 0;
                int idxOf = 0;
                if (!_subMeshDescriptors.TryGetValue(ctxId, out List<SubMeshDescriptor> descriptors))
                {
                    descriptors = new List<SubMeshDescriptor>(subMeshCount);
                    _subMeshDescriptors.Add(ctxId, descriptors);
                }
                descriptors.Clear();
                if (descriptors.Capacity < subMeshCount)
                {
                    descriptors.Capacity = subMeshCount;
                }

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
                public FuUnityContext Context;
                public TextureHandle Target;
                public TextureHandle BackdropA;
                public TextureHandle BackdropB;
                public bool ClearTarget;
                public int Downsample;
                public int BlurWidth;
                public int BlurHeight;
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
                pass = new FuguiRenderGraphPass(_shader, PassEvent);
                _passPerCamera[camera] = pass;
            }

            pass.ConfigureFrame(renderMainSurfaceContexts, renderOffscreenContexts);
            renderer.EnqueuePass(pass);
        }

        /// <summary>
        /// Returns whether the current camera can drive offscreen Fugui render textures.
        /// </summary>
        /// <param name="renderingData">The current camera rendering data.</param>
        /// <returns>True when the camera is Fugui's non-XR offscreen driver camera.</returns>
        private static bool IsOffscreenDriverCamera(RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;
            return camera != null
                && Fugui.IsOffscreenDriverCamera(camera)
                && renderingData.cameraData.renderType == CameraRenderType.Base;
        }
        #endregion
    }
}
