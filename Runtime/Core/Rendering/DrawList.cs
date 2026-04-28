using ImGuiNET;
using System;
using System.Runtime.InteropServices;

namespace Fu
{
        /// <summary>
        /// Represent a memory copy of an ImGui DrawList.
        /// It need to be a class because we store it on x frames, and it will significatively incrase GC.Collect() time if we keep it too long.
        /// </summary>
        public class DrawList
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
            private GCHandle _cmdHandle;
            private GCHandle _idxHandle;
            private GCHandle _vtxHandle;

            public string WindowName { get { return _windowName; } }
            public ImDrawCmd[] CmdBuffer { get { return _cmdBuffer; } }
            public IntPtr CmdPtr { get { return getCmdPtr(); } }
            public ushort[] IdxBuffer { get { return _idxBuffer; } }
            public IntPtr IdxPtr { get { return getIdxPtr(); } }
            public ImDrawVert[] VtxBuffer { get { return _vtxBuffer; } }
            public IntPtr VtxPtr { get { return getVtxPtr(); } }
            public ImDrawListFlags Flags { get { return _flags; } }
            public uint VtxCurrentIdx { get { return _vtxCurrentIdx; } }
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Draw List class.
            /// </summary>
            public DrawList()
            {
                // Allocate arrays ptr for sizeof(ArrayType)
                _cmdBuffer = new ImDrawCmd[0];
                _idxBuffer = new ushort[0];
                _vtxBuffer = new ImDrawVert[0];
            }

            /// <summary>
            /// Initializes a new instance of the Draw List class.
            /// </summary>
            /// <param name="drawList">The draw List value.</param>
            public DrawList(ImDrawListPtr drawList)
            {
                Bind(drawList);
            }
            #endregion

            #region Methods
            /// <summary>
            /// Bind the curretn drawList with an ImGui ImDrawListPtr
            /// </summary>
            /// <param name="drawList">ImGui drawList ptr handle</param>
            public unsafe void Bind(ImDrawListPtr drawList)
            {
                // save cmd buffer
                if (_cmdBuffer == null || _cmdBuffer.Length != drawList.CmdBuffer.Size)
                {
                    releaseCmdHandle();
                    _cmdBuffer = new ImDrawCmd[drawList.CmdBuffer.Size];
                }
                long cmdBufferSize = ImGuiDrawListUtils.ImDrawCmdSize * _cmdBuffer.Length;
                if (cmdBufferSize > 0)
                {
                    fixed (ImDrawCmd* cmdPtr = _cmdBuffer)
                    {
                        Buffer.MemoryCopy((void*)drawList.CmdBuffer.Data, cmdPtr, cmdBufferSize, cmdBufferSize);
                    }
                }

                // save idx buffer
                if (_idxBuffer == null || _idxBuffer.Length != drawList.IdxBuffer.Size)
                {
                    releaseIdxHandle();
                    _idxBuffer = new ushort[drawList.IdxBuffer.Size];
                }
                long idxBufferSize = 2 * _idxBuffer.Length;
                if (idxBufferSize > 0)
                {
                    fixed (ushort* idxPtr = _idxBuffer)
                    {
                        Buffer.MemoryCopy((void*)drawList.IdxBuffer.Data, idxPtr, idxBufferSize, idxBufferSize);
                    }
                }

                // save vtx buffer
                if (_vtxBuffer == null || _vtxBuffer.Length != drawList.VtxBuffer.Size)
                {
                    releaseVtxHandle();
                    _vtxBuffer = new ImDrawVert[drawList.VtxBuffer.Size];
                }
                long vtxBufferSize = ImGuiDrawListUtils.ImDrawVertSize * _vtxBuffer.Length;
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
                _windowName = drawList._OwnerName;
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
                if (_cmdBuffer == null || _cmdBuffer.Length == 0)
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
                if (_idxBuffer == null || _idxBuffer.Length == 0)
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
                if (_vtxBuffer == null || _vtxBuffer.Length == 0)
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
            #endregion
        }
}
