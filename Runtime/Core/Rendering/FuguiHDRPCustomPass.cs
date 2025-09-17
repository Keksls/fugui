using ImGuiNET;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
#if HAS_HDRP
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
#endif

namespace Fu
{
#if HAS_HDRP
    /// <summary>
    /// HDRP Custom Pass that renders Fugui overlay using the camera color/depth targets.
    /// Mirrors the original URP Render Feature logic (viewport, ortho VP matrices, scissor, mesh submeshes, texture binding).
    /// Injection point: After Post Process (overlay).
    /// </summary>
    [Serializable]
    public class FuguiHDRPCustomPass : CustomPass
    {
        #region CONSTANTS
        private const string SAMPLE_NAME = "Fugui.ExecuteDrawCommands";
        private const MeshUpdateFlags NO_MESH_CHECKS = MeshUpdateFlags.DontNotifyMeshUsers |
                                                       MeshUpdateFlags.DontRecalculateBounds |
                                                       MeshUpdateFlags.DontResetBoneBounds |
                                                       MeshUpdateFlags.DontValidateIndices;
        #endregion

        #region ATTRIBUTES (Inspector)
        /// <summary>
        /// Shader used for Fugui draw (same as in URP feature).
        /// </summary>
        public Shader FuguiShader;

        /// <summary>
        /// Only render when the camera GameObject is on this layer (default Unity UI layer = 5).
        /// </summary>
        public int CameraLayer = 5;
        #endregion

        #region RUNTIME STATE
        private Material _material;
        private Mesh _mesh;
        private int _textureID;
        private int _prevSubMeshCount = 1;
        private TextureManager _textureManager;
        private MaterialPropertyBlock _materialProperties;

        // Color sent with TexCoord1 semantics because otherwise Color attribute would be reordered to come before UVs.
        private static readonly VertexAttributeDescriptor[] _vertexAttributes = new[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position , VertexAttributeFormat.Float32, 2), // Pos
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2), // UV
            new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.UInt32, 1),  // Color
        };
        #endregion

        #region CUSTOM PASS LIFECYCLE
        /// <summary>
        /// Called once when the pass is created or enabled.
        /// </summary>
        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            _textureID = Shader.PropertyToID("_Texture");
            _materialProperties = new MaterialPropertyBlock();

            if (FuguiShader != null)
            {
                _material = CoreUtils.CreateEngineMaterial(FuguiShader);
                if (_material != null)
                {
                    _material.hideFlags = HideFlags.HideAndDontSave & ~HideFlags.DontUnloadUnusedAsset;
                }
            }

            _mesh = new Mesh
            {
                name = "FuguiMesh_HDRP"
            };
            _mesh.MarkDynamic();
        }

        /// <summary>
        /// Called every frame this pass executes.
        /// </summary>
        protected override void Execute(CustomPassContext ctx)
        {
            // Layer gate comme ta RenderFeature URP
            if (ctx.hdCamera != null && ctx.hdCamera.camera != null)
            {
                if (ctx.hdCamera.camera.gameObject.layer != CameraLayer)
                {
                    return;
                }
            }

            // Vérifie l’état de rendu
            if (Fugui.RenderingState != FuguiRenderingState.UpdateComplete)
            {
                return;
            }

            Fugui.RenderingState = FuguiRenderingState.Rendering;

            CommandBuffer commandBuffer = ctx.cmd;

            // Rendu du contexte principal
            Fugui.DefaultContext.EndRender();
            _textureManager = Fugui.DefaultContext.TextureManager;
            RenderDrawLists(commandBuffer, Fugui.DefaultContext.DrawData);

            Fugui.RenderingState = FuguiRenderingState.RenderComplete;
        }


        /// <summary>
        /// Called when the pass is disabled or destroyed.
        /// </summary>
        protected override void Cleanup()
        {
            if (_mesh != null)
            {
                UnityEngine.Object.DestroyImmediate(_mesh);
                _mesh = null;
            }

            if (_material != null)
            {
                CoreUtils.Destroy(_material);
                _material = null;
            }
        }
        #endregion

        #region METHODS
        /// <summary>
        /// Renders all draw lists into the current camera color target.
        /// Sets viewport + pixel-space orthographic VP, then issues submesh draws with scissor rectangles.
        /// </summary>
        private void RenderDrawLists(CommandBuffer commandBuffer, DrawData drawData)
        {
            Vector2 fbSize = drawData.DisplaySize * drawData.FramebufferScale;

            // Avoid rendering when minimized / empty
            if (fbSize.x <= 0.0f || fbSize.y <= 0.0f || drawData.TotalVtxCount == 0)
            {
                return;
            }

            UpdateMesh(drawData);

            commandBuffer.BeginSample(SAMPLE_NAME);

            // Full viewport in pixels
            commandBuffer.SetViewport(new Rect(0.0f, 0.0f, fbSize.x, fbSize.y));

            // Pixel-space ortho (origin top-left like your URP code with y inverted by scissor)
            Matrix4x4 view = Matrix4x4.Translate(new Vector3(0.5f / fbSize.x, 0.5f / fbSize.y, 0.0f));
            Matrix4x4 proj = Matrix4x4.Ortho(0.0f, fbSize.x, fbSize.y, 0.0f, 0.0f, 1.0f);
            commandBuffer.SetViewProjectionMatrices(view, proj);

            CreateDrawCommands(commandBuffer, drawData, fbSize);

            commandBuffer.EndSample(SAMPLE_NAME);
        }

        /// <summary>
        /// Issues the submesh draws with correct scissor and bound texture.
        /// </summary>
        private void CreateDrawCommands(CommandBuffer commandBuffer, DrawData drawData, Vector2 fbSize)
        {
            IntPtr prevTextureId = IntPtr.Zero;

            Vector4 clipOffset = new Vector4(drawData.DisplayPos.x, drawData.DisplayPos.y,
                                             drawData.DisplayPos.x, drawData.DisplayPos.y);
            Vector4 clipScale = new Vector4(drawData.FramebufferScale.x, drawData.FramebufferScale.y,
                                             drawData.FramebufferScale.x, drawData.FramebufferScale.y);

            int subOf = 0;

            for (int n = 0, nMax = drawData.CmdListsCount; n < nMax; ++n)
            {
                DrawList drawList = drawData.DrawLists[n];

                for (int i = 0, iMax = drawList.CmdBuffer.Length; i < iMax; ++i, ++subOf)
                {
                    ImDrawCmd drawCmd = drawList.CmdBuffer[i];

                    if (drawCmd.UserCallback != IntPtr.Zero)
                    {
                        Debug.Log("FuguiHDRPCustomPass: Unhandled ImGui UserCallback.");
                        continue;
                    }

                    // Project scissor rect into framebuffer space and skip if fully outside
                    Vector4 clipSize = drawCmd.ClipRect - clipOffset;
                    Vector4 clip = Vector4.Scale(clipSize, clipScale);

                    if (clip.x >= fbSize.x || clip.y >= fbSize.y || clip.z < 0.0f || clip.w < 0.0f)
                    {
                        continue;
                    }

                    // Texture switch
                    if (prevTextureId != drawCmd.TextureId)
                    {
                        prevTextureId = drawCmd.TextureId;

                        UnityEngine.Texture texture;
                        bool hasTexture = _textureManager.TryGetTexture(prevTextureId, out texture);

                        if (!hasTexture)
                        {
                            Debug.LogError($"FuguiHDRPCustomPass: Texture {prevTextureId} does not exist. Use UImGuiUtility.GetTextureID().");
                        }
                        else
                        {
                            if (texture != null)
                            {
                                _materialProperties.SetTexture(_textureID, texture);
                            }
                            else
                            {
                                Debug.LogWarning($"FuguiHDRPCustomPass: Texture {prevTextureId} is null.");
                            }
                        }
                    }

                    // Invert Y like your URP code
                    Rect scissor = new Rect(clip.x, fbSize.y - clip.w, clip.z - clip.x, clip.w - clip.y);
                    commandBuffer.EnableScissorRect(scissor);

                    // Draw this submesh index (material pass 0)
                    commandBuffer.DrawMesh(_mesh, Matrix4x4.identity, _material, subOf, 0, _materialProperties);
                }
            }

            commandBuffer.DisableScissorRect();
        }

        /// <summary>
        /// Builds/updates the dynamic mesh from ImGui draw data (submeshes per ImDrawCmd).
        /// </summary>
        private void UpdateMesh(DrawData drawData)
        {
            // Count total submeshes
            int subMeshCount = 0;
            for (int n = 0, nMax = drawData.CmdListsCount; n < nMax; ++n)
            {
                subMeshCount += drawData.DrawLists[n].CmdBuffer.Length;
            }

            if (_prevSubMeshCount != subMeshCount)
            {
                _mesh.Clear(true);
                _mesh.subMeshCount = _prevSubMeshCount = subMeshCount;
            }

            _mesh.SetVertexBufferParams(drawData.TotalVtxCount, _vertexAttributes);
            _mesh.SetIndexBufferParams(drawData.TotalIdxCount, IndexFormat.UInt16);

            int vtxOf = 0;
            int idxOf = 0;
            List<SubMeshDescriptor> descriptors = new List<SubMeshDescriptor>(subMeshCount);

            for (int n = 0, nMax = drawData.CmdListsCount; n < nMax; ++n)
            {
                DrawList drawList = drawData.DrawLists[n];

                unsafe
                {
                    NativeArray<ImDrawVert> vtxArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<ImDrawVert>(
                        (void*)drawList.VtxPtr, drawList.VtxBuffer.Length, Allocator.None);
                    NativeArray<ushort> idxArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<ushort>(
                        (void*)drawList.IdxPtr, drawList.IdxBuffer.Length, Allocator.None);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref vtxArray, AtomicSafetyHandle.GetTempMemoryHandle());
                    NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref idxArray, AtomicSafetyHandle.GetTempMemoryHandle());
#endif
                    _mesh.SetVertexBufferData(vtxArray, 0, vtxOf, vtxArray.Length, 0, NO_MESH_CHECKS);
                    _mesh.SetIndexBufferData(idxArray, 0, idxOf, idxArray.Length, NO_MESH_CHECKS);

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

            _mesh.SetSubMeshes(descriptors, NO_MESH_CHECKS);

            // Safe 2D bounds in clip-space pixels
            Vector2 fbSize = drawData.DisplaySize * drawData.FramebufferScale;
            _mesh.bounds = new Bounds(
                new Vector3(fbSize.x * 0.5f, fbSize.y * 0.5f, 0.0f),
                new Vector3(fbSize.x + 4.0f, fbSize.y + 4.0f, 1.0f));

            _mesh.UploadMeshData(false);
        }
        #endregion
    }
#endif
}
