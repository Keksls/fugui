using ImGuiNET;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fu
{
        /// <summary>
        /// Class that represent all DrawList for a frame
        /// </summary>
        public class DrawData
        {
            #region State
            public List<DrawList> DrawLists;
            internal List<DrawDataRenderItem> RenderItems;
            public int TotalVtxCount;
            public int TotalIdxCount;
            public Vector2 DisplayPos;
            public Vector2 DisplaySize;
            public Vector2 FramebufferScale;
            public int CmdListsCount;
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Draw Data class.
            /// </summary>
            public DrawData()
            {
                DrawLists = new List<DrawList>();
                RenderItems = new List<DrawDataRenderItem>();
                Clear();
            }
            #endregion

            #region Methods
            /// <summary>
            /// Clear all Draw Lists
            /// </summary>
            public void Clear()
            {
                for (int i = 0; i < DrawLists.Count; i++)
                {
                    DrawLists[i].Dispose();
                }
                DrawLists.Clear();
                RenderItems.Clear();
                TotalVtxCount = 0;
                TotalIdxCount = 0;
                CmdListsCount = 0;
            }

            /// <summary>
            /// Add some Draw Lists
            /// </summary>
            /// <param name="dLists">Draw Lists to Add</param>
            public void AddDrawLists(IEnumerable<DrawList> dLists)
            {
                foreach (DrawList drawList in dLists)
                {
                    AddDrawList(drawList);
                }
            }

            /// <summary>
            /// Add a DrawList
            /// </summary>
            /// <param name="dList">DrawList to Add</param>
            public void AddDrawList(DrawList dList)
            {
                AddTransientDrawList(dList);
            }

            /// <summary>
            /// Adds cached draw data owned by a Fugui window.
            /// </summary>
            /// <param name="window">Window that owns the cached draw lists and render mesh.</param>
            internal void AddWindowDrawData(FuWindow window)
            {
                if (window == null || window.CachedDrawLists.Count == 0)
                {
                    return;
                }

                RenderItems.Add(DrawDataRenderItem.ForWindow(window));
                foreach (DrawList drawList in window.CachedDrawLists)
                {
                    AddDrawListCounters(drawList);
                }
            }

            /// <summary>
            /// Adds a non-window draw list that still has to be meshed by the renderer.
            /// </summary>
            /// <param name="drawList">Draw list to add.</param>
            internal void AddTransientDrawList(DrawList drawList)
            {
                if (drawList == null)
                {
                    return;
                }

                RenderItems.Add(DrawDataRenderItem.ForDrawList(drawList));
                AddDrawListCounters(drawList);
            }

            /// <summary>
            /// Adds a draw list to aggregate counters without creating a render item.
            /// </summary>
            /// <param name="drawList">Draw list to count.</param>
            private void AddDrawListCounters(DrawList drawList)
            {
                DrawLists.Add(drawList);
                CmdListsCount++;
                TotalVtxCount += drawList.VtxBuffer.Length;
                TotalIdxCount += drawList.IdxBuffer.Length;
            }

            /// <summary>
            /// Bind this drawData from ImGui drawData Ptr
            /// </summary>
            /// <param name="imDrawData">ImDrawDataPtr for this frame</param>
            public void Bind(ImDrawDataPtr imDrawData)
            {
                Clear();
                for (int i = 0; i < imDrawData.CmdListsCount; i++)
                {
                    AddTransientDrawList(new DrawList(imDrawData.CmdLists[i]));
                }
                FramebufferScale = imDrawData.FramebufferScale;
                DisplayPos = imDrawData.DisplayPos;
                DisplaySize = imDrawData.DisplaySize;
            }
            #endregion
        }

        /// <summary>
        /// Render item for either a cached Fugui window or a transient ImGui draw list.
        /// </summary>
        internal class DrawDataRenderItem
        {
            #region State
            private readonly DrawList[] _drawLists;

            public FuWindow Window { get; private set; }
            public bool IsWindow { get { return Window != null; } }
            public IReadOnlyList<DrawList> DrawLists
            {
                get
                {
                    if (Window != null)
                    {
                        return Window.CachedDrawLists;
                    }
                    return _drawLists;
                }
            }
            #endregion

            #region Constructors
            private DrawDataRenderItem(FuWindow window, DrawList drawList)
            {
                Window = window;
                _drawLists = drawList != null ? new DrawList[1] { drawList } : new DrawList[0];
            }
            #endregion

            #region Methods
            public static DrawDataRenderItem ForWindow(FuWindow window)
            {
                return new DrawDataRenderItem(window, null);
            }

            public static DrawDataRenderItem ForDrawList(DrawList drawList)
            {
                return new DrawDataRenderItem(null, drawList);
            }
            #endregion
        }

        /// <summary>
        /// Unity mesh cache built from one or more ImGui draw lists.
        /// </summary>
        internal class DrawListMesh
        {
            #region State
            private Mesh _mesh;
            private int _prevSubMeshCount = -1;
            private int _prevVertexCount = -1;
            private int _prevIndexCount = -1;
            private readonly List<SubMeshDescriptor> _subMeshDescriptors = new List<SubMeshDescriptor>();

            public Mesh Mesh { get { return _mesh; } }
            public int SubMeshCount { get; private set; }
            public int TotalVtxCount { get; private set; }
            public int TotalIdxCount { get; private set; }

            private const MeshUpdateFlags NoMeshChecks = MeshUpdateFlags.DontNotifyMeshUsers |
                MeshUpdateFlags.DontRecalculateBounds |
                MeshUpdateFlags.DontResetBoneBounds |
                MeshUpdateFlags.DontValidateIndices;

            private static readonly VertexAttributeDescriptor[] VertexAttributes = new[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position , VertexAttributeFormat.Float32, 2),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.UInt32, 1),
            };
            #endregion

            #region Constructors
            public DrawListMesh(string name)
            {
                _mesh = new Mesh
                {
                    name = string.IsNullOrEmpty(name) ? "FuguiMesh" : name,
                    hideFlags = HideFlags.HideAndDontSave
                };
                _mesh.MarkDynamic();
            }
            #endregion

            #region Methods
            public void Update(IReadOnlyList<DrawList> drawLists, Vector2 displaySize, Vector2 framebufferScale)
            {
                int subMeshCount = 0;
                int totalVtxCount = 0;
                int totalIdxCount = 0;

                if (drawLists != null)
                {
                    for (int i = 0; i < drawLists.Count; i++)
                    {
                        DrawList drawList = drawLists[i];
                        if (drawList == null)
                        {
                            continue;
                        }

                        subMeshCount += drawList.CmdBuffer.Length;
                        totalVtxCount += drawList.VtxBuffer.Length;
                        totalIdxCount += drawList.IdxBuffer.Length;
                    }
                }

                SubMeshCount = subMeshCount;
                TotalVtxCount = totalVtxCount;
                TotalIdxCount = totalIdxCount;

                if (subMeshCount == 0 || totalVtxCount == 0 || totalIdxCount == 0)
                {
                    _mesh.Clear(true);
                    _prevSubMeshCount = 0;
                    _prevVertexCount = 0;
                    _prevIndexCount = 0;
                    return;
                }

                bool meshLayoutChanged = _prevSubMeshCount != subMeshCount;
                if (meshLayoutChanged)
                {
                    _mesh.Clear(true);
                    _mesh.subMeshCount = subMeshCount;
                    _prevSubMeshCount = subMeshCount;
                }

                if (_prevVertexCount != totalVtxCount || meshLayoutChanged)
                {
                    _mesh.SetVertexBufferParams(totalVtxCount, VertexAttributes);
                    _prevVertexCount = totalVtxCount;
                }

                if (_prevIndexCount != totalIdxCount || meshLayoutChanged)
                {
                    _mesh.SetIndexBufferParams(totalIdxCount, IndexFormat.UInt16);
                    _prevIndexCount = totalIdxCount;
                }

                int vtxOffset = 0;
                int idxOffset = 0;
                _subMeshDescriptors.Clear();
                if (_subMeshDescriptors.Capacity < subMeshCount)
                {
                    _subMeshDescriptors.Capacity = subMeshCount;
                }

                for (int n = 0; n < drawLists.Count; ++n)
                {
                    DrawList drawList = drawLists[n];
                    if (drawList == null)
                    {
                        continue;
                    }

                    _mesh.SetVertexBufferData(drawList.VtxBuffer, 0, vtxOffset, drawList.VtxBuffer.Length, 0, NoMeshChecks);
                    _mesh.SetIndexBufferData(drawList.IdxBuffer, 0, idxOffset, drawList.IdxBuffer.Length, NoMeshChecks);

                    ImDrawCmd[] commands = drawList.CmdBuffer;
                    for (int i = 0; i < commands.Length; ++i)
                    {
                        ImDrawCmd command = commands[i];
                        _subMeshDescriptors.Add(new SubMeshDescriptor
                        {
                            topology = MeshTopology.Triangles,
                            indexStart = idxOffset + (int)command.IdxOffset,
                            indexCount = (int)command.ElemCount,
                            baseVertex = vtxOffset + (int)command.VtxOffset,
                        });
                    }

                    vtxOffset += drawList.VtxBuffer.Length;
                    idxOffset += drawList.IdxBuffer.Length;
                }

                _mesh.SetSubMeshes(_subMeshDescriptors, NoMeshChecks);

                Vector2 fbSize = displaySize * framebufferScale;
                _mesh.bounds = new Bounds(
                    new Vector3(fbSize.x * 0.5f, fbSize.y * 0.5f, 0f),
                    new Vector3(fbSize.x + 4f, fbSize.y + 4f, 1f)
                );

                _mesh.UploadMeshData(false);
            }

            public void Destroy()
            {
                if (_mesh == null)
                {
                    return;
                }

                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(_mesh);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(_mesh);
                }
                _mesh = null;
                SubMeshCount = 0;
                TotalVtxCount = 0;
                TotalIdxCount = 0;
            }
            #endregion
        }
}
