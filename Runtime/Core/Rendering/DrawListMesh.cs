using ImGuiNET;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fu
{
    /// <summary>
    /// Unity mesh cache built from one or more ImGui draw lists.
    /// </summary>
    internal class DrawListMesh
    {
        #region State
        private Mesh _mesh;
        private int _vertexCapacity;
        private int _indexCapacity;
        private int _subMeshCapacity;
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
        /// <summary>
        /// Creates the dynamic mesh whose buffers are retained for this cache's lifetime.
        /// </summary>
        public DrawListMesh(string name)
        {
            // The owning window or context releases retained capacity through Destroy.
            _mesh = new Mesh
            {
                name = string.IsNullOrEmpty(name) ? "FuguiMesh" : name,
                hideFlags = HideFlags.HideAndDontSave
            };
            _mesh.MarkDynamic();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Uploads a single draw list while retaining unused vertex and index capacity.
        /// </summary>
        public void Update(DrawList drawList, Vector2 displaySize, Vector2 framebufferScale)
        {
            // Empty frames disable all draw ranges without releasing the buffers.
            if (drawList == null || drawList.CmdCount == 0 || drawList.VtxCount == 0 || drawList.IdxCount == 0)
            {
                ClearDrawRanges();
                return;
            }

            SubMeshCount = drawList.CmdCount;
            TotalVtxCount = drawList.VtxCount;
            TotalIdxCount = drawList.IdxCount;

            EnsureBufferCapacity(drawList.VtxCount, drawList.IdxCount);

            _mesh.SetVertexBufferData(drawList.VtxBuffer, 0, 0, drawList.VtxCount, 0, NoMeshChecks);
            _mesh.SetIndexBufferData(drawList.IdxBuffer, 0, 0, drawList.IdxCount, NoMeshChecks);

            _subMeshDescriptors.Clear();
            PrepareSubMeshDescriptorCapacity(drawList.CmdCount);

            ImDrawCmd[] commands = drawList.CmdBuffer;
            for (int i = 0; i < drawList.CmdCount; ++i)
            {
                ImDrawCmd command = commands[i];
                _subMeshDescriptors.Add(new SubMeshDescriptor
                {
                    topology = MeshTopology.Triangles,
                    indexStart = (int)command.IdxOffset,
                    indexCount = (int)command.ElemCount,
                    baseVertex = (int)command.VtxOffset,
                });
            }

            PublishSubMeshes();

            Vector2 fbSize = displaySize * framebufferScale;
            _mesh.bounds = new Bounds(
                new Vector3(fbSize.x * 0.5f, fbSize.y * 0.5f, 0f),
                new Vector3(fbSize.x + 4f, fbSize.y + 4f, 1f)
            );

            _mesh.UploadMeshData(false);
        }

        /// <summary>
        /// Uploads multiple lists using active offsets independently of allocated buffer capacity.
        /// </summary>
        public void Update(IReadOnlyList<DrawList> drawLists, Vector2 displaySize, Vector2 framebufferScale)
        {
            // ImGui's per-command baseVertex preserves UInt16 indices across combined lists.
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

                    subMeshCount += drawList.CmdCount;
                    totalVtxCount += drawList.VtxCount;
                    totalIdxCount += drawList.IdxCount;
                }
            }

            if (subMeshCount == 0 || totalVtxCount == 0 || totalIdxCount == 0)
            {
                ClearDrawRanges();
                return;
            }

            SubMeshCount = subMeshCount;
            TotalVtxCount = totalVtxCount;
            TotalIdxCount = totalIdxCount;
            EnsureBufferCapacity(totalVtxCount, totalIdxCount);

            int vtxOffset = 0;
            int idxOffset = 0;
            _subMeshDescriptors.Clear();
            PrepareSubMeshDescriptorCapacity(subMeshCount);

            for (int n = 0; n < drawLists.Count; ++n)
            {
                DrawList drawList = drawLists[n];
                if (drawList == null)
                {
                    continue;
                }

                if (drawList.VtxCount > 0)
                    _mesh.SetVertexBufferData(drawList.VtxBuffer, 0, vtxOffset, drawList.VtxCount, 0, NoMeshChecks);
                if (drawList.IdxCount > 0)
                    _mesh.SetIndexBufferData(drawList.IdxBuffer, 0, idxOffset, drawList.IdxCount, NoMeshChecks);

                ImDrawCmd[] commands = drawList.CmdBuffer;
                for (int i = 0; i < drawList.CmdCount; ++i)
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

                vtxOffset += drawList.VtxCount;
                idxOffset += drawList.IdxCount;
            }

            PublishSubMeshes();

            Vector2 fbSize = displaySize * framebufferScale;
            _mesh.bounds = new Bounds(
                new Vector3(fbSize.x * 0.5f, fbSize.y * 0.5f, 0f),
                new Vector3(fbSize.x + 4f, fbSize.y + 4f, 1f)
            );

            _mesh.UploadMeshData(false);
        }

        /// <summary>
        /// Grows GPU buffers only when active data exceeds their retained capacities.
        /// </summary>
        private void EnsureBufferCapacity(int vertexCount, int indexCount)
        {
            // Reconfiguration invalidates buffer contents; each Update uploads all active data afterwards.
            if (vertexCount > _vertexCapacity)
            {
                _vertexCapacity = GetExpandedCapacity(_vertexCapacity, vertexCount, 256);
                _mesh.SetVertexBufferParams(_vertexCapacity, VertexAttributes);
            }
            if (indexCount > _indexCapacity)
            {
                _indexCapacity = GetExpandedCapacity(_indexCapacity, indexCount, 512);
                _mesh.SetIndexBufferParams(_indexCapacity, IndexFormat.UInt16);
            }
        }

        /// <summary>
        /// Retains descriptor capacity instead of reallocating it on draw-command fluctuations.
        /// </summary>
        private void PrepareSubMeshDescriptorCapacity(int requiredCount)
        {
            // Keep the Unity descriptor count monotonic too: shrinking submeshes can truncate indices.
            if (requiredCount <= _subMeshCapacity)
                return;
            _subMeshCapacity = GetExpandedCapacity(_subMeshCapacity, requiredCount, 8);
            _subMeshDescriptors.Capacity = _subMeshCapacity;
        }

        /// <summary>
        /// Publishes active commands and empty reserved descriptors without shrinking the index buffer.
        /// </summary>
        private void PublishSubMeshes()
        {
            // The renderer iterates active ImGui commands, never the reserved mesh.subMeshCount.
            while (_subMeshDescriptors.Count < _subMeshCapacity)
                _subMeshDescriptors.Add(new SubMeshDescriptor(0, 0, MeshTopology.Triangles));
            _mesh.SetSubMeshes(_subMeshDescriptors, NoMeshChecks);
        }

        /// <summary>
        /// Disables geometry for an empty frame while keeping all buffer capacities reusable.
        /// </summary>
        private void ClearDrawRanges()
        {
            // Clear stale ranges once; subsequent empty updates do not submit another mesh change.
            if (_subMeshDescriptors.Count > 0 && (TotalVtxCount > 0 || TotalIdxCount > 0 || SubMeshCount > 0))
            {
                _subMeshDescriptors.Clear();
                PublishSubMeshes();
            }
            SubMeshCount = 0;
            TotalVtxCount = 0;
            TotalIdxCount = 0;
        }

        /// <summary>
        /// Returns a geometrically grown capacity without overflowing signed element counts.
        /// </summary>
        private static int GetExpandedCapacity(int currentCapacity, int requiredCount, int minimumCapacity)
        {
            // Growth by doubling amortizes allocations; extreme sizes fall back to the exact request.
            int capacity = Mathf.Max(currentCapacity, minimumCapacity);
            while (capacity < requiredCount)
            {
                if (capacity > int.MaxValue / 2)
                    return requiredCount;
                capacity *= 2;
            }
            return capacity;
        }

        /// <summary>
        /// Releases the mesh and all retained capacity when its owner is disposed.
        /// </summary>
        public void Destroy()
        {
            // Retention ends with this cache; no mesh or descriptor storage survives its owner.
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
            _vertexCapacity = 0;
            _indexCapacity = 0;
            _subMeshCapacity = 0;
            _subMeshDescriptors.Clear();
            _subMeshDescriptors.Capacity = 0;
            SubMeshCount = 0;
            TotalVtxCount = 0;
            TotalIdxCount = 0;
        }
        #endregion
    }
}
