using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Fu.Core
{
    public static class ImGuiDrawListUtils
    {
        public static long ImDrawCmdSize { get; private set; }
        public static long ImDrawVertSize { get; private set; }
        private static DrawData cmd = new DrawData();
        private static Dictionary<string, string> _unIconnizedTitleMapping = new Dictionary<string, string>();

        static ImGuiDrawListUtils()
        {
            ImDrawCmdSize = (long)Unsafe.SizeOf<ImDrawCmd>();
            ImDrawVertSize = (long)Unsafe.SizeOf<ImDrawVert>();

        }

        public static DrawData GetDrawCmd(Dictionary<string, FuWindow> windows, ImDrawDataPtr imDrawDataPtr)
        {
            cmd.Clear();

            // bind current draw lists
            for (int i = 0; i < imDrawDataPtr.CmdListsCount; i++)
            {
                string name = imDrawDataPtr.CmdListsRange[i]._OwnerName;

                // prevent icons name to switch render (for some reason, ImGui copy the name like 'name/name##pathID' when name contain Icon)
                if (name.StartsWith("??? ") && name.Contains("/"))
                {
                    if (!_unIconnizedTitleMapping.ContainsKey(name))
                    {
                        string escapedTitle = name.Remove(0, 4).Split('/')[0]; // get icon escaped title
                        // search csharp formated window title
                        string csharpeEquivalentTitle = string.Empty;
                        foreach (string windowTitle in windows.Keys)
                        {
                            // icon escaped csharp formated title match excaped native formated title
                            if (windowTitle.Remove(0, 2) == escapedTitle)
                            {
                                csharpeEquivalentTitle = name.Replace("??? ", windowTitle.Substring(0, 2));
                            }
                        }

                        // save mapping unescaped values (native => csharp formated)
                        if (!string.IsNullOrEmpty(csharpeEquivalentTitle))
                        {
                            _unIconnizedTitleMapping.Add(name, csharpeEquivalentTitle);
                        }
                    }

                    // replace native formated icon values by csharp formated versions
                    if (_unIconnizedTitleMapping.ContainsKey(name))
                    {
                        name = _unIconnizedTitleMapping[name];
                    }
                }

                bool isChild = false;
                if (!windows.ContainsKey(name) && name.Contains("/")) // it may be a window's child
                {
                    if (windows.ContainsKey(name.Split('/')[0])) // it's a window's child
                    {
                        if (windows[name.Split('/')[0]].IsDocked) // the window is docked
                        {
                            if (name.Split('/').Length <= 2) // it's a direct window's child (so it may be the forced child used to store vtx)
                            {
                                isChild = name.Split('/')[1].StartsWith(name.Split('/')[0] + "ctnr"); // is it the forced child ?
                                name = isChild ? name.Split('/')[0] : "";
                            }
                            else // it's a child lvl 2+, so child of child, we need to store it too
                            {
                                // it's a child of a windows, we must store it.
                                windows[name.Split('/')[0]].ChildrenDrawLists[name] = new DrawList(imDrawDataPtr.CmdListsRange[i]);
                                continue;
                            }
                        }
                        else // the window is not docked
                        {
                            // it's a child of a windows, we must store it.
                            windows[name.Split('/')[0]].ChildrenDrawLists[name] = new DrawList(imDrawDataPtr.CmdListsRange[i]);
                        }
                    }
                }

                // save window draw cmd if window has just been draw and it's an UIWindow
                if (windows.ContainsKey(name) && (windows[name].IsDocked && isChild || !windows[name].IsDocked))
                {
                    // frame has just been redraw, we must store drawList
                    if (windows[name].HasJustBeenDraw)
                    {
                        // dispose window draw list GH handles (free memory)
                        windows[name].DrawList.Dispose();
                        // copy cmd, idx and vtx buffers
                        windows[name].DrawList.Bind(imDrawDataPtr.CmdListsRange[i]);
                        // window is stored, it's not just draw anymore
                        windows[name].HasJustBeenDraw = false;
                        // add window draw list to current drawData
                        cmd.AddDrawList(windows[name].DrawList);
                        // add window children lists to current drawData
                        cmd.AddDrawLists(windows[name].ChildrenDrawLists.Values);
                        // dispose children draw list before clearing it => will not be done by garbage collectore because GChandle are pinned
                        foreach (var pair in windows[name].ChildrenDrawLists)
                        {
                            pair.Value.Dispose();
                        }
                        // clear children draw lists (because we just redraw the window, so children will follow, see above)
                        windows[name].ChildrenDrawLists.Clear();
                    }
                    else
                    {
                        // add stored draw lists related to this window
                        cmd.AddDrawList(windows[name].DrawList);
                        cmd.AddDrawLists(windows[name].ChildrenDrawLists.Values);
                    }
                }
                else
                {
                    // it's nothing to do with an UIWindow, let store it without any edition
                    cmd.AddDrawList(new DrawList(imDrawDataPtr.CmdListsRange[i]));
                }
            }

            cmd.FramebufferScale = imDrawDataPtr.FramebufferScale;
            cmd.DisplayPos = imDrawDataPtr.DisplayPos;
            cmd.DisplaySize = imDrawDataPtr.DisplaySize;
            return cmd;
        }
    }

    /// <summary>
    /// Represent a memory copy of an ImGui DrawList.
    /// It need to be a class because we store it on x frames, and it will significatively incrase GC.Collect() time if we keep it too long.
    /// </summary>
    public unsafe class DrawList
    {
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

        public DrawList()
        {
            // Allocate arrays ptr for sizeof(ArrayType)
            _cmdBuffer = new ImDrawCmd[0];
            _idxBuffer = new ushort[0];
            _vtxBuffer = new ImDrawVert[0];
        }

        public DrawList(ImDrawListPtr drawList)
        {
            Bind(drawList);
        }

        /// <summary>
        /// Bind the curretn drawList with an ImGui ImDrawListPtr
        /// </summary>
        /// <param name="drawList">ImGui drawList ptr handle</param>
        public void Bind(ImDrawListPtr drawList)
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
    }

    /// <summary>
    /// Class that represent all DrawList for a frame
    /// </summary>
    public unsafe class DrawData
    {
        public List<DrawList> DrawLists;
        public int TotalVtxCount;
        public int TotalIdxCount;
        public Vector2 DisplayPos;
        public Vector2 DisplaySize;
        public Vector2 FramebufferScale;
        public int CmdListsCount;

        public DrawData()
        {
            DrawLists = new List<DrawList>();
            Clear();
        }

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
            DrawLists.Add(dList);
            CmdListsCount++;
            TotalVtxCount += dList.VtxBuffer.Length;
            TotalIdxCount += dList.IdxBuffer.Length;
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
                AddDrawList(new DrawList(imDrawData.CmdListsRange[i]));
            }
            FramebufferScale = imDrawData.FramebufferScale;
            DisplayPos = imDrawData.DisplayPos;
            DisplaySize = imDrawData.DisplaySize;
        }
    }
}