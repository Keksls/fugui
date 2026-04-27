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
            public IntPtr CmdPtr { get { return _cmdPtr; } }
            public ushort[] IdxBuffer { get { return _idxBuffer; } }
            public IntPtr IdxPtr { get { return _idxPtr; } }
            public ImDrawVert[] VtxBuffer { get { return _vtxBuffer; } }
            public IntPtr VtxPtr { get { return _vtxPtr; } }
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
                _cmdBuffer = new ImDrawCmd[drawList.CmdBuffer.Size]; // allocate manager memory
                _cmdHandle = GCHandle.Alloc(_cmdBuffer, GCHandleType.Pinned); // allocate unmanager memory
                _cmdPtr = _cmdHandle.AddrOfPinnedObject(); // get unmanager memory ptr
                // copy to unmanager memory and keep it (Pinned mem)
                Buffer.MemoryCopy((void*)drawList.CmdBuffer.Data, (void*)_cmdPtr, ImGuiDrawListUtils.ImDrawCmdSize * _cmdBuffer.Length, ImGuiDrawListUtils.ImDrawCmdSize * _cmdBuffer.Length);

                // save idx buffer
                _idxBuffer = new ushort[drawList.IdxBuffer.Size];
                _idxHandle = GCHandle.Alloc(_idxBuffer, GCHandleType.Pinned);
                _idxPtr = _idxHandle.AddrOfPinnedObject();
                Buffer.MemoryCopy((void*)drawList.IdxBuffer.Data, (void*)_idxPtr, 2 * _idxBuffer.Length, 2 * _idxBuffer.Length);

                // save vtx buffer
                _vtxBuffer = new ImDrawVert[drawList.VtxBuffer.Size];
                _vtxHandle = GCHandle.Alloc(_vtxBuffer, GCHandleType.Pinned);
                _vtxPtr = _vtxHandle.AddrOfPinnedObject();
                Buffer.MemoryCopy((void*)drawList.VtxBuffer.Data, (void*)_vtxPtr, ImGuiDrawListUtils.ImDrawVertSize * _vtxBuffer.Length, ImGuiDrawListUtils.ImDrawVertSize * _vtxBuffer.Length);

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
                if (!_cmdHandle.IsAllocated)
                {
                    return;
                }
                _cmdHandle.Free();
                _idxHandle.Free();
                _vtxHandle.Free();
            }
            #endregion
        }
}
