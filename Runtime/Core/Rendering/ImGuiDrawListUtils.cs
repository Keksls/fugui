using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Represents the Im Gui Draw List Utils type.
    /// </summary>
    public static class ImGuiDrawListUtils
    {
        #region State
        public static long ImDrawCmdSize { get; private set; }
        public static long ImDrawVertSize { get; private set; }

        private static Dictionary<string, string> _unIconnizedTitleMapping = new Dictionary<string, string>();
        private static readonly Dictionary<IntPtr, CachedOwnerName> _ownerNameCache = new Dictionary<IntPtr, CachedOwnerName>();
        private static readonly Dictionary<string, string> _childRootWindowNameCache = new Dictionary<string, string>();
        private static readonly List<ResolvedDrawList> _resolvedDrawLists = new List<ResolvedDrawList>();
        private static readonly Dictionary<FuWindow, int> _lastWindowDrawListIndices = new Dictionary<FuWindow, int>();
        private static readonly HashSet<FuWindow> _rebuiltWindows = new HashSet<FuWindow>();
        private static readonly List<DrawList> _orderedDrawLists = new List<DrawList>();
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the Im Gui Draw List Utils class.
        /// </summary>
        static ImGuiDrawListUtils()
        {
            ImDrawCmdSize = (long)Unsafe.SizeOf<ImDrawCmd>();
            ImDrawVertSize = (long)Unsafe.SizeOf<ImDrawVert>();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Gets the draw cmd.
        /// </summary>
        /// <param name="windows">The windows value.</param>
        /// <param name="imDrawDataPtr">The im Draw Data Ptr value.</param>
        /// <param name="cmd">The cmd value.</param>
        public static void GetDrawCmd(Dictionary<string, FuWindow> windows, ImDrawDataPtr imDrawDataPtr, ref DrawData cmd)
        {
            cmd.Clear();

            _resolvedDrawLists.Clear();
            if (_resolvedDrawLists.Capacity < imDrawDataPtr.CmdListsCount)
            {
                _resolvedDrawLists.Capacity = imDrawDataPtr.CmdListsCount;
            }

            for (int i = 0; i < imDrawDataPtr.CmdListsCount; i++)
            {
                ImDrawListPtr nativeDrawList = imDrawDataPtr.CmdLists[i];
                string name = getWindowName(windows, nativeDrawList);
                FuWindow window = null;
                bool isWindowRoot = windows.TryGetValue(name, out window);
                bool isWindowChild = false;

                if (!isWindowRoot) // it may be a window's child
                {
                    string rootWindowName = getChildRootWindowName(name);
                    if (!string.IsNullOrEmpty(rootWindowName))
                    {
                        if (windows.TryGetValue(rootWindowName, out FuWindow rootWindow)) // it's a window's child
                        {
                            window = rootWindow;
                            isWindowChild = true;
                        }
                    }
                }

                _resolvedDrawLists.Add(new ResolvedDrawList
                {
                    NativeDrawList = nativeDrawList,
                    OwnerName = name,
                    Window = window,
                    IsWindowRoot = isWindowRoot,
                    IsWindowChild = isWindowChild
                });
            }

            RebuildWindowRenderMeshes(_resolvedDrawLists, imDrawDataPtr.DisplaySize, imDrawDataPtr.FramebufferScale);

            GetLastWindowDrawListIndices(_resolvedDrawLists, _lastWindowDrawListIndices);
            for (int i = 0; i < _resolvedDrawLists.Count; i++)
            {
                ResolvedDrawList drawList = _resolvedDrawLists[i];
                if (drawList.Window != null)
                {
                    if (_lastWindowDrawListIndices.TryGetValue(drawList.Window, out int lastIndex) &&
                        i == lastIndex &&
                        drawList.Window.IsVisible)
                    {
                        cmd.AddWindowDrawData(drawList.Window);
                    }
                    continue;
                }

                // it's nothing to do with an UIWindow, let store it without any edition
                cmd.AddTransientDrawList(drawList.NativeDrawList);
            }

            cmd.FramebufferScale = imDrawDataPtr.FramebufferScale;
            cmd.DisplayPos = imDrawDataPtr.DisplayPos;
            cmd.DisplaySize = imDrawDataPtr.DisplaySize;
        }

        /// <summary>
        /// Returns the last native draw-list index for each Fugui window in the current ImGui frame.
        /// </summary>
        /// <param name="drawLists">Resolved draw lists for the current frame.</param>
        /// <param name="result">Reusable result dictionary.</param>
        private static void GetLastWindowDrawListIndices(List<ResolvedDrawList> drawLists, Dictionary<FuWindow, int> result)
        {
            result.Clear();
            for (int i = 0; i < drawLists.Count; i++)
            {
                FuWindow window = drawLists[i].Window;
                if (window == null)
                {
                    continue;
                }

                result[window] = i;
            }
        }

        /// <summary>
        /// Rebuild render meshes for windows that were redrawn in the just-finished ImGui frame.
        /// </summary>
        /// <param name="drawLists">Resolved draw lists for the current frame.</param>
        /// <param name="displaySize">Draw data display size.</param>
        /// <param name="framebufferScale">Draw data framebuffer scale.</param>
        private static void RebuildWindowRenderMeshes(List<ResolvedDrawList> drawLists, Vector2 displaySize, Vector2 framebufferScale)
        {
            _rebuiltWindows.Clear();
            for (int i = 0; i < drawLists.Count; i++)
            {
                FuWindow window = drawLists[i].Window;
                if (window == null || !window.HasJustBeenDraw || _rebuiltWindows.Contains(window))
                {
                    continue;
                }

                _orderedDrawLists.Clear();
                window.BeginDrawDataCacheRebuild();
                int firstIncludedIndex = GetLastRootDrawListIndex(drawLists, window);
                for (int j = 0; j < drawLists.Count; j++)
                {
                    if (j < firstIncludedIndex)
                    {
                        continue;
                    }

                    ResolvedDrawList drawList = drawLists[j];
                    if (drawList.Window != window)
                    {
                        continue;
                    }

                    if (drawList.IsWindowRoot)
                    {
                        window.DrawList.Bind(drawList.NativeDrawList, drawList.OwnerName);
                        _orderedDrawLists.Add(window.DrawList);
                    }
                    else if (drawList.IsWindowChild)
                    {
                        _orderedDrawLists.Add(window.BindCachedChildDrawList(drawList.NativeDrawList, drawList.OwnerName));
                    }
                }

                window.CacheDrawData(_orderedDrawLists, displaySize, framebufferScale);
                window.HasJustBeenDraw = false;
                _rebuiltWindows.Add(window);
            }
        }

        /// <summary>
        /// Returns the last root draw-list index for a window when it appears more than once in a frame.
        /// </summary>
        /// <param name="drawLists">Resolved draw lists for the current frame.</param>
        /// <param name="window">Window to inspect.</param>
        /// <returns>Last root draw-list index, or the first owned draw-list index when no root was found.</returns>
        private static int GetLastRootDrawListIndex(List<ResolvedDrawList> drawLists, FuWindow window)
        {
            int firstOwnedIndex = -1;
            int lastRootIndex = -1;

            for (int i = 0; i < drawLists.Count; i++)
            {
                if (drawLists[i].Window != window)
                {
                    continue;
                }

                if (firstOwnedIndex < 0)
                {
                    firstOwnedIndex = i;
                }

                if (drawLists[i].IsWindowRoot)
                {
                    lastRootIndex = i;
                }
            }

            if (lastRootIndex >= 0)
            {
                return lastRootIndex;
            }

            return firstOwnedIndex >= 0 ? firstOwnedIndex : 0;
        }

        /// <summary>
        /// Gets the draw list window name used by Fugui.
        /// </summary>
        /// <param name="windows">The windows value.</param>
        /// <param name="drawList">Native ImGui draw list.</param>
        /// <returns>The Fugui window name.</returns>
        private static unsafe string getWindowName(Dictionary<string, FuWindow> windows, ImDrawListPtr drawList)
        {
            IntPtr ownerNamePtr = (IntPtr)drawList.NativePtr->_OwnerName;
            if (ownerNamePtr == IntPtr.Zero)
            {
                return string.Empty;
            }

            byte* nameData = (byte*)ownerNamePtr;
            int length = 0;
            int hash = 17;
            byte* ptr = nameData;
            while (*ptr != 0)
            {
                hash = hash * 31 + *ptr;
                length++;
                ptr++;
            }

            if (_ownerNameCache.TryGetValue(ownerNamePtr, out CachedOwnerName cachedName) &&
                cachedName.Length == length &&
                cachedName.Hash == hash)
            {
                return cachedName.Name;
            }

            string resolvedName = getWindowName(windows, new NullTerminatedString(nameData));
            _ownerNameCache[ownerNamePtr] = new CachedOwnerName
            {
                Length = length,
                Hash = hash,
                Name = resolvedName
            };
            return resolvedName;
        }

        /// <summary>
        /// Gets the root window name for a child draw-list owner name.
        /// </summary>
        /// <param name="name">Resolved owner name.</param>
        /// <returns>Root window name, or null when the name is not a child path.</returns>
        private static string getChildRootWindowName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            if (_childRootWindowNameCache.TryGetValue(name, out string rootWindowName))
            {
                return rootWindowName;
            }

            int firstSlashIndex = name.IndexOf('/');
            if (firstSlashIndex < 0)
            {
                return null;
            }

            rootWindowName = name.Substring(0, firstSlashIndex);
            _childRootWindowNameCache.Add(name, rootWindowName);
            return rootWindowName;
        }

        /// <summary>
        /// Gets the draw list window name used by Fugui.
        /// </summary>
        /// <param name="windows">The windows value.</param>
        /// <param name="name">The native draw list owner name.</param>
        /// <returns>The Fugui window name.</returns>
        private static string getWindowName(Dictionary<string, FuWindow> windows, string name)
        {
            // prevent icons name to switch render (for some reason, ImGui copy the name like 'name/name##pathID' when name contain Icon)
            if (!name.StartsWith("???"))
            {
                return name;
            }

            if (_unIconnizedTitleMapping.TryGetValue(name, out string mappedName))
            {
                return mappedName;
            }

            string escapedTitleSource = name.Substring(3);
            int slashIndex = escapedTitleSource.IndexOf('/');
            string escapedTitle = slashIndex >= 0 ? escapedTitleSource.Substring(0, slashIndex) : escapedTitleSource;
            string csharpeEquivalentTitle = string.Empty;
            foreach (string windowTitle in windows.Keys)
            {
                if (windowTitle.Length > 0 && windowTitle.Substring(1) == escapedTitle)
                {
                    csharpeEquivalentTitle = windowTitle.Substring(0, 1) + name.Substring(3);
                }
            }

            if (!string.IsNullOrEmpty(csharpeEquivalentTitle))
            {
                _unIconnizedTitleMapping.Add(name, csharpeEquivalentTitle);
                return csharpeEquivalentTitle;
            }

            return name;
        }

        /// <summary>
        /// Resolved ownership for a native ImGui draw list.
        /// </summary>
        private struct ResolvedDrawList
        {
            public ImDrawListPtr NativeDrawList;
            public string OwnerName;
            public FuWindow Window;
            public bool IsWindowRoot;
            public bool IsWindowChild;
        }

        /// <summary>
        /// Cached conversion of an ImGui owner-name pointer.
        /// </summary>
        private struct CachedOwnerName
        {
            public int Length;
            public int Hash;
            public string Name;
        }
        #endregion
    }
}
