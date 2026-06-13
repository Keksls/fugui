using ImGuiNET;
using System;
using System.Runtime.InteropServices;

namespace Fu
{
        /// <summary>
        /// Represent a memory copy of an ImGui DrawList.
        /// It need to be a class because we store it on x frames, and it will significatively incrase GC.Collect() time if we keep it too long.
        /// </summary>
        internal class DrawList
        {
            #region State
            // ImGui DrawList stuffs
            // for each buffer, we store buffer data as array, a pointor of the first array element
            // and a GCHandle that pin teh pointor memory until we release it
            private string _windowName;
            private ImDrawCmd[] _cmdBuffer;
            private IntPtr _cmdPtr;
            private ushort[] _idxBuffer;
            private IntPtr _idxPtr;
            private ImDrawVert[] _vtxBuffer;
            private IntPtr _vtxPtr;
            private ImDrawListFlags _flags;
            private uint _vtxCurrentIdx;
            private int _cmdCount;
            private int _idxCount;
            private int _vtxCount;
            private GCHandle _cmdHandle;
            private GCHandle _idxHandle;
            private GCHandle _vtxHandle;

            public string WindowName { get { return _windowName; } }
            /// <summary>
            /// Backing command buffer. Only the first CmdCount entries are valid.
            /// </summary>
            public ImDrawCmd[] CmdBuffer { get { return _cmdBuffer; } }
            public int CmdCount { get { return _cmdCount; } }
            public IntPtr CmdPtr { get { return getCmdPtr(); } }
            /// <summary>
            /// Backing index buffer. Only the first IdxCount entries are valid.
            /// </summary>
            public ushort[] IdxBuffer { get { return _idxBuffer; } }
            public int IdxCount { get { return _idxCount; } }
            public IntPtr IdxPtr { get { return getIdxPtr(); } }
            /// <summary>
            /// Backing vertex buffer. Only the first VtxCount entries are valid.
            /// </summary>
            public ImDrawVert[] VtxBuffer { get { return _vtxBuffer; } }
            public int VtxCount { get { return _vtxCount; } }
            public IntPtr VtxPtr { get { return getVtxPtr(); } }
            public ImDrawListFlags Flags { get { return _flags; } }
            public uint VtxCurrentIdx { get { return _vtxCurrentIdx; } }
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Draw List class.
            /// </summary>
            internal DrawList()
            {
                // Allocate arrays ptr for sizeof(ArrayType)
                _cmdBuffer = Array.Empty<ImDrawCmd>();
                _idxBuffer = Array.Empty<ushort>();
                _vtxBuffer = Array.Empty<ImDrawVert>();
            }

            /// <summary>
            /// Initializes a new instance of the Draw List class.
            /// </summary>
            /// <param name="drawList">The draw List value.</param>
            internal DrawList(FuDrawList drawList)
            {
                Bind(drawList);
            }

            /// <summary>
            /// Initializes a new instance of the Draw List class with an already resolved owner name.
            /// </summary>
            /// <param name="drawList">The draw List value.</param>
            /// <param name="windowName">Already resolved owner name.</param>
            internal DrawList(FuDrawList drawList, string windowName)
            {
                Bind(drawList, windowName);
            }
            #endregion

            #region Methods
            /// <summary>
            /// Bind the current drawList with an ImGui ImDrawListPtr.
            /// </summary>
            /// <param name="drawList">ImGui drawList ptr handle</param>
            internal unsafe void Bind(FuDrawList drawList)
            {
                bindBuffers(drawList);
                _windowName = null;
            }

            /// <summary>
            /// Bind the current drawList with an ImGui ImDrawListPtr and a cached owner name.
            /// </summary>
            /// <param name="drawList">ImGui drawList ptr handle.</param>
            /// <param name="windowName">Already resolved owner name.</param>
            internal unsafe void Bind(FuDrawList drawList, string windowName)
            {
                bindBuffers(drawList);
                _windowName = windowName;
            }

            /// <summary>
            /// Copies native ImGui draw-list buffers without converting native strings.
            /// </summary>
            /// <param name="drawList">ImGui drawList ptr handle.</param>
            private unsafe void bindBuffers(FuDrawList drawList)
            {
                // save cmd buffer
                _cmdCount = drawList.CmdBuffer.Size;
                ensureCmdCapacity(_cmdCount);
                long cmdBufferSize = ImGuiDrawListUtils.ImDrawCmdSize * _cmdCount;
                if (cmdBufferSize > 0)
                {
                    fixed (ImDrawCmd* cmdPtr = _cmdBuffer)
                    {
                        Buffer.MemoryCopy((void*)drawList.CmdBuffer.Data, cmdPtr, cmdBufferSize, cmdBufferSize);
                    }
                }

                // save idx buffer
                _idxCount = drawList.IdxBuffer.Size;
                ensureIdxCapacity(_idxCount);
                long idxBufferSize = 2 * _idxCount;
                if (idxBufferSize > 0)
                {
                    fixed (ushort* idxPtr = _idxBuffer)
                    {
                        Buffer.MemoryCopy((void*)drawList.IdxBuffer.Data, idxPtr, idxBufferSize, idxBufferSize);
                    }
                }

                // save vtx buffer
                _vtxCount = drawList.VtxBuffer.Size;
                ensureVtxCapacity(_vtxCount);
                long vtxBufferSize = ImGuiDrawListUtils.ImDrawVertSize * _vtxCount;
                if (vtxBufferSize > 0)
                {
                    fixed (ImDrawVert* vtxPtr = _vtxBuffer)
                    {
                        Buffer.MemoryCopy((void*)drawList.VtxBuffer.Data, vtxPtr, vtxBufferSize, vtxBufferSize);
                    }
                }

                // save flags and vtx/idx
                _flags = drawList.Flags;
                _vtxCurrentIdx = drawList._VtxCurrentIdx;
            }

            /// <summary>
            /// Free pinned memory
            /// </summary>
            public void Dispose()
            {
                releaseCmdHandle();
                releaseIdxHandle();
                releaseVtxHandle();
            }

            /// <summary>
            /// Gets the pinned command buffer pointer.
            /// </summary>
            /// <returns>The command buffer pointer.</returns>
            private IntPtr getCmdPtr()
            {
                if (_cmdBuffer == null || _cmdCount == 0)
                {
                    return IntPtr.Zero;
                }

                if (!_cmdHandle.IsAllocated)
                {
                    _cmdHandle = GCHandle.Alloc(_cmdBuffer, GCHandleType.Pinned);
                    _cmdPtr = _cmdHandle.AddrOfPinnedObject();
                }
                return _cmdPtr;
            }

            /// <summary>
            /// Gets the pinned index buffer pointer.
            /// </summary>
            /// <returns>The index buffer pointer.</returns>
            private IntPtr getIdxPtr()
            {
                if (_idxBuffer == null || _idxCount == 0)
                {
                    return IntPtr.Zero;
                }

                if (!_idxHandle.IsAllocated)
                {
                    _idxHandle = GCHandle.Alloc(_idxBuffer, GCHandleType.Pinned);
                    _idxPtr = _idxHandle.AddrOfPinnedObject();
                }
                return _idxPtr;
            }

            /// <summary>
            /// Gets the pinned vertex buffer pointer.
            /// </summary>
            /// <returns>The vertex buffer pointer.</returns>
            private IntPtr getVtxPtr()
            {
                if (_vtxBuffer == null || _vtxCount == 0)
                {
                    return IntPtr.Zero;
                }

                if (!_vtxHandle.IsAllocated)
                {
                    _vtxHandle = GCHandle.Alloc(_vtxBuffer, GCHandleType.Pinned);
                    _vtxPtr = _vtxHandle.AddrOfPinnedObject();
                }
                return _vtxPtr;
            }

            /// <summary>
            /// Releases the pinned command buffer pointer.
            /// </summary>
            private void releaseCmdHandle()
            {
                if (_cmdHandle.IsAllocated)
                {
                    _cmdHandle.Free();
                }
                _cmdPtr = IntPtr.Zero;
            }

            /// <summary>
            /// Releases the pinned index buffer pointer.
            /// </summary>
            private void releaseIdxHandle()
            {
                if (_idxHandle.IsAllocated)
                {
                    _idxHandle.Free();
                }
                _idxPtr = IntPtr.Zero;
            }

            /// <summary>
            /// Releases the pinned vertex buffer pointer.
            /// </summary>
            private void releaseVtxHandle()
            {
                if (_vtxHandle.IsAllocated)
                {
                    _vtxHandle.Free();
                }
                _vtxPtr = IntPtr.Zero;
            }

            /// <summary>
            /// Ensures the command buffer can hold the requested element count.
            /// </summary>
            /// <param name="count">The requested valid command count.</param>
            private void ensureCmdCapacity(int count)
            {
                if (count <= 0)
                {
                    _cmdBuffer ??= Array.Empty<ImDrawCmd>();
                    return;
                }

                if (_cmdBuffer != null && _cmdBuffer.Length >= count)
                {
                    return;
                }

                releaseCmdHandle();
                _cmdBuffer = new ImDrawCmd[getExpandedCapacity(_cmdBuffer != null ? _cmdBuffer.Length : 0, count)];
            }

            /// <summary>
            /// Ensures the index buffer can hold the requested element count.
            /// </summary>
            /// <param name="count">The requested valid index count.</param>
            private void ensureIdxCapacity(int count)
            {
                if (count <= 0)
                {
                    _idxBuffer ??= Array.Empty<ushort>();
                    return;
                }

                if (_idxBuffer != null && _idxBuffer.Length >= count)
                {
                    return;
                }

                releaseIdxHandle();
                _idxBuffer = new ushort[getExpandedCapacity(_idxBuffer != null ? _idxBuffer.Length : 0, count)];
            }

            /// <summary>
            /// Ensures the vertex buffer can hold the requested element count.
            /// </summary>
            /// <param name="count">The requested valid vertex count.</param>
            private void ensureVtxCapacity(int count)
            {
                if (count <= 0)
                {
                    _vtxBuffer ??= Array.Empty<ImDrawVert>();
                    return;
                }

                if (_vtxBuffer != null && _vtxBuffer.Length >= count)
                {
                    return;
                }

                releaseVtxHandle();
                _vtxBuffer = new ImDrawVert[getExpandedCapacity(_vtxBuffer != null ? _vtxBuffer.Length : 0, count)];
            }

            /// <summary>
            /// Returns an amortized capacity for a requested count.
            /// </summary>
            /// <param name="currentCapacity">Current buffer capacity.</param>
            /// <param name="requiredCount">Required valid element count.</param>
            /// <returns>Capacity to allocate.</returns>
            private static int getExpandedCapacity(int currentCapacity, int requiredCount)
            {
                if (requiredCount <= 0)
                {
                    return 0;
                }

                int capacity = currentCapacity > 0 ? currentCapacity : 4;
                while (capacity < requiredCount)
                {
                    if (capacity > int.MaxValue / 2)
                    {
                        return requiredCount;
                    }

                    capacity *= 2;
                }

                return capacity;
            }
            #endregion
        }
}
