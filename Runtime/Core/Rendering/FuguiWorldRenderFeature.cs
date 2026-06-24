using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_6000_4_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif
using UnityEngine.Rendering.Universal;

namespace Fu
{
    /// <summary>
    /// URP render feature that renders Fugui draw lists directly as world-space meshes.
    /// </summary>
    public class FuguiWorldRenderFeature : ScriptableRendererFeature
    {
        #region State
        /// <summary>
        /// Shader used to render Fugui world-space meshes.
        /// </summary>
        public Shader _shader;

        /// <summary>
        /// Render pass event used for Fugui world-space meshes.
        /// </summary>
        public RenderPassEvent PassEvent = RenderPassEvent.AfterRenderingTransparents;

        private FuguiWorldRenderPass _pass;
        #endregion

        #region Methods
        /// <summary>
        /// Creates the render pass used by this feature.
        /// </summary>
        public override void Create()
        {
            if (_shader == null)
            {
                _shader = Shader.Find("Fugui/URP_WorldMesh");
            }

            _pass = new FuguiWorldRenderPass(_shader, PassEvent);
        }

        /// <summary>
        /// Enqueues the Fugui world-space render pass when the frame has submitted surfaces.
        /// </summary>
        /// <param name="renderer">Renderer that receives the pass.</param>
        /// <param name="renderingData">Current rendering data.</param>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;
            if (_shader == null || camera == null || !Fugui.World.ShouldRenderCamera(camera) || !Fugui.World.HasCurrentFrameItems())
            {
                return;
            }

            if (_pass == null)
            {
                _pass = new FuguiWorldRenderPass(_shader, PassEvent);
            }

            _pass.ConfigureShader(_shader, PassEvent);
            renderer.EnqueuePass(_pass);
        }
        #endregion

        /// <summary>
        /// Render pass that uploads Fugui world surfaces to dynamic meshes and draws them.
        /// </summary>
        private sealed class FuguiWorldRenderPass : ScriptableRenderPass
        {
            #region State
            private const string SampleName = "Fugui.World.ExecuteDrawCommands";
            private readonly Dictionary<int, FuguiWorldMeshCache> _meshCaches = new Dictionary<int, FuguiWorldMeshCache>();
            private readonly List<FuguiWorldDrawItem> _sortedItems = new List<FuguiWorldDrawItem>();
            private MaterialPropertyBlock _properties = new MaterialPropertyBlock();
            private Material _material;
            private Shader _shader;
            private int _textureID;
            private int _textureIsAlphaID;
            private int _clipRectID;
            #endregion

            #region Constructors
            /// <summary>
            /// Creates a Fugui world render pass.
            /// </summary>
            /// <param name="shader">Shader used by the pass.</param>
            /// <param name="passEvent">Render pass event.</param>
            public FuguiWorldRenderPass(Shader shader, RenderPassEvent passEvent)
            {
                _textureID = Shader.PropertyToID("_Texture");
                _textureIsAlphaID = Shader.PropertyToID("_TextureIsAlpha");
                _clipRectID = Shader.PropertyToID("_ClipRect");
                ConfigureShader(shader, passEvent);
            }
            #endregion

            #region Methods
            /// <summary>
            /// Configures the shader and pass event used for rendering.
            /// </summary>
            /// <param name="shader">Shader used by the pass.</param>
            /// <param name="passEvent">Render pass event.</param>
            public void ConfigureShader(Shader shader, RenderPassEvent passEvent)
            {
                renderPassEvent = passEvent;
                if (_shader == shader && _material != null)
                {
                    return;
                }

                _shader = shader;
                if (_material != null)
                {
                    DestroyMaterial(_material);
                    _material = null;
                }

                if (_shader != null)
                {
                    _material = new Material(_shader)
                    {
                        hideFlags = HideFlags.HideAndDontSave & ~HideFlags.DontUnloadUnusedAsset
                    };
                }
            }

            /// <summary>
            /// Destroys a generated material using the correct Unity lifetime API.
            /// </summary>
            /// <param name="material">Material to destroy.</param>
            private static void DestroyMaterial(Material material)
            {
                if (material == null)
                {
                    return;
                }

                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(material);
                    return;
                }

                UnityEngine.Object.DestroyImmediate(material);
            }

#if UNITY_6000_4_OR_NEWER
            /// <summary>
            /// Records the render graph pass for Fugui world-space surfaces.
            /// </summary>
            /// <param name="renderGraph">Render graph that records the pass.</param>
            /// <param name="frameData">Frame data for the current camera.</param>
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                TextureHandle color = resourceData.activeColorTexture;
                TextureHandle depth = resourceData.activeDepthTexture;

                using var builder = renderGraph.AddRasterRenderPass<PassData>("Fugui_WorldPass", out var passData);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderAttachment(color, 0, AccessFlags.ReadWrite);
                if (depth.IsValid())
                {
                    builder.SetRenderAttachmentDepth(depth, AccessFlags.ReadWrite);
                }

                passData.Camera = cameraData.camera;
                builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                {
                    RenderWorldItemsRenderGraph(ctx.cmd, data.Camera);
                });
            }
#endif

#if !UNITY_6000_4_OR_NEWER
            /// <summary>
            /// Executes the pass when URP compatibility mode is used.
            /// </summary>
            /// <param name="context">Scriptable render context.</param>
            /// <param name="renderingData">Current rendering data.</param>
            [Obsolete]
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer commandBuffer = CommandBufferPool.Get("FuguiWorldRenderPass");
                RenderWorldItems(commandBuffer, renderingData.cameraData.camera);
                context.ExecuteCommandBuffer(commandBuffer);
                CommandBufferPool.Release(commandBuffer);
            }
#endif

            /// <summary>
            /// Draws all current-frame world items visible to the supplied camera.
            /// </summary>
            /// <param name="commandBuffer">Command buffer to record draw calls into.</param>
            /// <param name="camera">Camera currently rendering.</param>
            private void RenderWorldItems(CommandBuffer commandBuffer, Camera camera)
            {
                if (_material == null || camera == null)
                {
                    return;
                }

                IReadOnlyList<FuguiWorldDrawItem> items = Fugui.World.GetCurrentFrameItems();
                if (items == null || items.Count == 0)
                {
                    return;
                }

                commandBuffer.BeginSample(SampleName);
                BuildSortedItems(items);
                for (int i = 0; i < _sortedItems.Count; i++)
                {
                    DrawItem(commandBuffer, _sortedItems[i]);
                }
                commandBuffer.EndSample(SampleName);
            }

#if UNITY_6000_4_OR_NEWER
            /// <summary>
            /// Draws all current-frame world items visible to the supplied camera through RenderGraph.
            /// </summary>
            /// <param name="commandBuffer">Raster command buffer to record draw calls into.</param>
            /// <param name="camera">Camera currently rendering.</param>
            private void RenderWorldItemsRenderGraph(RasterCommandBuffer commandBuffer, Camera camera)
            {
                if (_material == null || camera == null)
                {
                    return;
                }

                IReadOnlyList<FuguiWorldDrawItem> items = Fugui.World.GetCurrentFrameItems();
                if (items == null || items.Count == 0)
                {
                    return;
                }

                commandBuffer.BeginSample(SampleName);
                BuildSortedItems(items);
                for (int i = 0; i < _sortedItems.Count; i++)
                {
                    DrawItemRenderGraph(commandBuffer, _sortedItems[i]);
                }
                commandBuffer.EndSample(SampleName);
            }
#endif

            /// <summary>
            /// Builds the sorted item list for the current frame.
            /// </summary>
            /// <param name="items">Submitted world items.</param>
            private void BuildSortedItems(IReadOnlyList<FuguiWorldDrawItem> items)
            {
                _sortedItems.Clear();
                int frame = Time.frameCount;
                for (int i = 0; i < items.Count; i++)
                {
                    FuguiWorldDrawItem item = items[i];
                    if (item.Frame != frame)
                    {
                        continue;
                    }

                    _sortedItems.Add(item);
                }

                _sortedItems.Sort(CompareItems);
            }

            /// <summary>
            /// Compares two items by explicit order and stable surface id.
            /// </summary>
            /// <param name="left">Left item.</param>
            /// <param name="right">Right item.</param>
            /// <returns>Comparison result.</returns>
            private static int CompareItems(FuguiWorldDrawItem left, FuguiWorldDrawItem right)
            {
                int order = left.Desc.SortingOrder.CompareTo(right.Desc.SortingOrder);
                return order != 0 ? order : left.SurfaceId.CompareTo(right.SurfaceId);
            }

            /// <summary>
            /// Draws one Fugui world item.
            /// </summary>
            /// <param name="commandBuffer">Command buffer to record draw calls into.</param>
            /// <param name="item">World item to draw.</param>
            private void DrawItem(CommandBuffer commandBuffer, FuguiWorldDrawItem item)
            {
                if (item.DrawList == null || item.TextureManager == null)
                {
                    return;
                }

                FuguiWorldMeshCache meshCache = GetMeshCache(item.SurfaceId);
                meshCache.Update(item);
                if (meshCache.Mesh == null || meshCache.SubMeshCount <= 0)
                {
                    return;
                }

                Matrix4x4 matrix = item.Desc.GetLocalToWorldMatrix();
                ImDrawCmd[] commands = item.DrawList.CmdBuffer;
                IntPtr previousTextureId = IntPtr.Zero;
                bool hasPreviousTexture = false;
                int passIndex = GetPassIndex(item.Desc.DepthMode);

                for (int i = 0; i < item.DrawList.CmdCount; i++)
                {
                    ImDrawCmd command = commands[i];
                    if (command.UserCallback != IntPtr.Zero || command.ElemCount == 0)
                    {
                        continue;
                    }

                    if (!hasPreviousTexture || previousTextureId != command.TextureId)
                    {
                        previousTextureId = command.TextureId;
                        hasPreviousTexture = true;
                        BindTexture(item.TextureManager, command.TextureId);
                    }

                    _properties.SetVector(_clipRectID, command.ClipRect);
                commandBuffer.DrawMesh(meshCache.Mesh, matrix, _material, i, passIndex, _properties);
                }
            }

#if UNITY_6000_4_OR_NEWER
            /// <summary>
            /// Draws one Fugui world item through RenderGraph.
            /// </summary>
            /// <param name="commandBuffer">Raster command buffer to record draw calls into.</param>
            /// <param name="item">World item to draw.</param>
            private void DrawItemRenderGraph(RasterCommandBuffer commandBuffer, FuguiWorldDrawItem item)
            {
                if (item.DrawList == null || item.TextureManager == null)
                {
                    return;
                }

                FuguiWorldMeshCache meshCache = GetMeshCache(item.SurfaceId);
                meshCache.Update(item);
                if (meshCache.Mesh == null || meshCache.SubMeshCount <= 0)
                {
                    return;
                }

                Matrix4x4 matrix = item.Desc.GetLocalToWorldMatrix();
                ImDrawCmd[] commands = item.DrawList.CmdBuffer;
                IntPtr previousTextureId = IntPtr.Zero;
                bool hasPreviousTexture = false;
                int passIndex = GetPassIndex(item.Desc.DepthMode);

                for (int i = 0; i < item.DrawList.CmdCount; i++)
                {
                    ImDrawCmd command = commands[i];
                    if (command.UserCallback != IntPtr.Zero || command.ElemCount == 0)
                    {
                        continue;
                    }

                    if (!hasPreviousTexture || previousTextureId != command.TextureId)
                    {
                        previousTextureId = command.TextureId;
                        hasPreviousTexture = true;
                        BindTexture(item.TextureManager, command.TextureId);
                    }

                    _properties.SetVector(_clipRectID, command.ClipRect);
                    commandBuffer.DrawMesh(meshCache.Mesh, matrix, _material, i, passIndex, _properties);
                }
            }
#endif

            /// <summary>
            /// Binds the texture referenced by one ImGui draw command.
            /// </summary>
            /// <param name="textureManager">Texture manager that owns the texture id.</param>
            /// <param name="textureId">Texture id to bind.</param>
            private void BindTexture(TextureManager textureManager, IntPtr textureId)
            {
                if (textureId != IntPtr.Zero && textureManager.TryGetTexture(textureId, out Texture texture) && texture != null)
                {
                    _properties.SetTexture(_textureID, texture);
                    _properties.SetFloat(_textureIsAlphaID, textureManager.IsFontAtlasTexture(texture) ? 1f : 0f);
                    return;
                }

                // Color-only ImGui geometry still references the atlas white pixel in normal paths.
                _properties.SetTexture(_textureID, Texture2D.whiteTexture);
                _properties.SetFloat(_textureIsAlphaID, 0f);
            }

            /// <summary>
            /// Gets the shader pass index for a depth mode.
            /// </summary>
            /// <param name="depthMode">Depth mode to map.</param>
            /// <returns>The shader pass index.</returns>
            private static int GetPassIndex(FuguiWorldDepthMode depthMode)
            {
                switch (depthMode)
                {
                    case FuguiWorldDepthMode.None:
                        return 0;
                    case FuguiWorldDepthMode.TestAndWrite:
                        return 2;
                    case FuguiWorldDepthMode.Test:
                    default:
                        return 1;
                }
            }

            /// <summary>
            /// Gets or creates a mesh cache for one surface id.
            /// </summary>
            /// <param name="surfaceId">Surface id.</param>
            /// <returns>The matching mesh cache.</returns>
            private FuguiWorldMeshCache GetMeshCache(int surfaceId)
            {
                if (_meshCaches.TryGetValue(surfaceId, out FuguiWorldMeshCache meshCache))
                {
                    return meshCache;
                }

                meshCache = new FuguiWorldMeshCache($"FuguiWorldMesh_{surfaceId}");
                _meshCaches.Add(surfaceId, meshCache);
                return meshCache;
            }
            #endregion

            #region Nested Types
#if UNITY_6000_4_OR_NEWER
            /// <summary>
            /// Render graph pass data.
            /// </summary>
            private sealed class PassData
            {
                /// <summary>
                /// Camera currently rendering.
                /// </summary>
                public Camera Camera;
            }
#endif
            #endregion
        }

        /// <summary>
        /// Dynamic mesh cache for one Fugui world draw-list surface.
        /// </summary>
        private sealed class FuguiWorldMeshCache
        {
            #region State
            private const MeshUpdateFlags NoMeshChecks = MeshUpdateFlags.DontNotifyMeshUsers |
                MeshUpdateFlags.DontRecalculateBounds |
                MeshUpdateFlags.DontResetBoneBounds |
                MeshUpdateFlags.DontValidateIndices;

            private static readonly VertexAttributeDescriptor[] VertexAttributes = new[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.UInt32, 1),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord2, VertexAttributeFormat.Float32, 2),
            };

            private readonly Mesh _mesh;
            private readonly List<SubMeshDescriptor> _subMeshDescriptors = new List<SubMeshDescriptor>();
            private FuguiWorldVertex[] _vertices = Array.Empty<FuguiWorldVertex>();
            private int _previousVertexCount = -1;
            private int _previousIndexCount = -1;
            private int _previousSubMeshCount = -1;
            #endregion

            #region Properties
            /// <summary>
            /// Gets the cached mesh.
            /// </summary>
            public Mesh Mesh => _mesh;

            /// <summary>
            /// Gets the current submesh count.
            /// </summary>
            public int SubMeshCount { get; private set; }
            #endregion

            #region Constructors
            /// <summary>
            /// Creates a dynamic world mesh cache.
            /// </summary>
            /// <param name="name">Mesh name.</param>
            public FuguiWorldMeshCache(string name)
            {
                _mesh = new Mesh
                {
                    name = name,
                    hideFlags = HideFlags.HideAndDontSave
                };
                _mesh.MarkDynamic();
            }
            #endregion

            #region Methods
            /// <summary>
            /// Updates the cached mesh from a Fugui world draw item.
            /// </summary>
            /// <param name="item">World draw item.</param>
            public void Update(FuguiWorldDrawItem item)
            {
                DrawList drawList = item.DrawList;
                if (drawList == null || drawList.VtxCount <= 0 || drawList.IdxCount <= 0 || drawList.CmdCount <= 0)
                {
                    Clear();
                    return;
                }

                SubMeshCount = drawList.CmdCount;
                EnsureMeshLayout(drawList);
                FillVertices(drawList, item.Desc);
                _mesh.SetVertexBufferData(_vertices, 0, 0, drawList.VtxCount, 0, NoMeshChecks);
                _mesh.SetIndexBufferData(drawList.IdxBuffer, 0, 0, drawList.IdxCount, NoMeshChecks);
                UpdateSubMeshes(drawList);
                UpdateBounds(item.Desc);
                _mesh.UploadMeshData(false);
            }

            /// <summary>
            /// Clears the cached mesh when no geometry is available.
            /// </summary>
            private void Clear()
            {
                _mesh.Clear(true);
                _previousVertexCount = 0;
                _previousIndexCount = 0;
                _previousSubMeshCount = 0;
                SubMeshCount = 0;
            }

            /// <summary>
            /// Ensures the Unity mesh buffers match the draw-list layout.
            /// </summary>
            /// <param name="drawList">Draw list used as source.</param>
            private void EnsureMeshLayout(DrawList drawList)
            {
                bool layoutChanged = _previousSubMeshCount != drawList.CmdCount;
                if (layoutChanged)
                {
                    _mesh.Clear(true);
                    _mesh.subMeshCount = drawList.CmdCount;
                    _previousSubMeshCount = drawList.CmdCount;
                }

                if (_previousVertexCount != drawList.VtxCount || layoutChanged)
                {
                    _mesh.SetVertexBufferParams(drawList.VtxCount, VertexAttributes);
                    _previousVertexCount = drawList.VtxCount;
                    if (_vertices.Length < drawList.VtxCount)
                    {
                        _vertices = new FuguiWorldVertex[GetExpandedCapacity(_vertices.Length, drawList.VtxCount)];
                    }
                }

                if (_previousIndexCount != drawList.IdxCount || layoutChanged)
                {
                    _mesh.SetIndexBufferParams(drawList.IdxCount, IndexFormat.UInt16);
                    _previousIndexCount = drawList.IdxCount;
                }
            }

            /// <summary>
            /// Converts ImGui vertices into local surface-space vertices.
            /// </summary>
            /// <param name="drawList">Source draw list.</param>
            /// <param name="desc">Surface description.</param>
            private void FillVertices(DrawList drawList, FuguiWorldSurfaceDesc desc)
            {
                Vector2 normalizedPivot = desc.GetNormalizedPivot();
                Vector2 resolution = new Vector2(desc.Resolution.x, desc.Resolution.y);
                Vector2 size = desc.Size;

                ImDrawVert[] sourceVertices = drawList.VtxBuffer;
                for (int i = 0; i < drawList.VtxCount; i++)
                {
                    ImDrawVert source = sourceVertices[i];
                    Vector2 normalized = new Vector2(
                        source.pos.x / resolution.x,
                        source.pos.y / resolution.y);
                    Vector3 localPosition = new Vector3(
                        (normalized.x - normalizedPivot.x) * size.x,
                        (normalizedPivot.y - normalized.y) * size.y,
                        0f);

                    _vertices[i] = new FuguiWorldVertex
                    {
                        Position = localPosition,
                        UV = source.uv,
                        Color = source.col,
                        ClipPosition = source.pos
                    };
                }
            }

            /// <summary>
            /// Updates mesh submeshes to match ImGui draw commands.
            /// </summary>
            /// <param name="drawList">Source draw list.</param>
            private void UpdateSubMeshes(DrawList drawList)
            {
                _subMeshDescriptors.Clear();
                if (_subMeshDescriptors.Capacity < drawList.CmdCount)
                {
                    _subMeshDescriptors.Capacity = drawList.CmdCount;
                }

                ImDrawCmd[] commands = drawList.CmdBuffer;
                for (int i = 0; i < drawList.CmdCount; i++)
                {
                    ImDrawCmd command = commands[i];
                    _subMeshDescriptors.Add(new SubMeshDescriptor
                    {
                        topology = MeshTopology.Triangles,
                        indexStart = (int)command.IdxOffset,
                        indexCount = (int)command.ElemCount,
                        baseVertex = (int)command.VtxOffset
                    });
                }

                _mesh.SetSubMeshes(_subMeshDescriptors, NoMeshChecks);
            }

            /// <summary>
            /// Updates local mesh bounds from the surface size and pivot.
            /// </summary>
            /// <param name="desc">Surface description.</param>
            private void UpdateBounds(FuguiWorldSurfaceDesc desc)
            {
                Vector2 pivot = desc.GetNormalizedPivot();
                Vector3 center = new Vector3(
                    (0.5f - pivot.x) * desc.Size.x,
                    (pivot.y - 0.5f) * desc.Size.y,
                    0f);
                _mesh.bounds = new Bounds(center, new Vector3(desc.Size.x + 0.01f, desc.Size.y + 0.01f, 0.02f));
            }

            /// <summary>
            /// Returns an amortized array capacity for the requested count.
            /// </summary>
            /// <param name="currentCapacity">Current capacity.</param>
            /// <param name="requiredCount">Required element count.</param>
            /// <returns>Expanded capacity.</returns>
            private static int GetExpandedCapacity(int currentCapacity, int requiredCount)
            {
                int capacity = currentCapacity > 0 ? currentCapacity : 4;
                while (capacity < requiredCount)
                {
                    capacity *= 2;
                }

                return capacity;
            }
            #endregion
        }

        /// <summary>
        /// Vertex layout uploaded for Fugui world-space meshes.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct FuguiWorldVertex
        {
            #region State
            /// <summary>
            /// Local surface-space vertex position.
            /// </summary>
            public Vector3 Position;

            /// <summary>
            /// Source ImGui texture coordinate.
            /// </summary>
            public Vector2 UV;

            /// <summary>
            /// Packed ImGui vertex color.
            /// </summary>
            public uint Color;

            /// <summary>
            /// Source ImGui pixel position used by shader clipping.
            /// </summary>
            public Vector2 ClipPosition;
            #endregion
        }
    }
}
