using ImGuiNET;
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

            ResolvedDrawList[] drawLists = new ResolvedDrawList[imDrawDataPtr.CmdListsCount];
            for (int i = 0; i < imDrawDataPtr.CmdListsCount; i++)
            {
                ImDrawListPtr nativeDrawList = imDrawDataPtr.CmdLists[i];
                string name = getWindowName(windows, nativeDrawList._OwnerName);
                FuWindow window = null;
                bool isWindowRoot = windows.TryGetValue(name, out window);
                bool isWindowChild = false;

                if (!isWindowRoot) // it may be a window's child
                {
                    int firstSlashIndex = name.IndexOf('/');
                    if (firstSlashIndex >= 0)
                    {
                        string rootWindowName = name.Substring(0, firstSlashIndex);
                        if (windows.TryGetValue(rootWindowName, out FuWindow rootWindow)) // it's a window's child
                        {
                            window = rootWindow;
                            isWindowChild = true;
                        }
                    }
                }

                drawLists[i] = new ResolvedDrawList
                {
                    NativeDrawList = nativeDrawList,
                    Window = window,
                    IsWindowRoot = isWindowRoot,
                    IsWindowChild = isWindowChild
                };
            }

            RebuildWindowRenderMeshes(drawLists, imDrawDataPtr.DisplaySize, imDrawDataPtr.FramebufferScale);

            Dictionary<FuWindow, int> lastWindowDrawListIndices = GetLastWindowDrawListIndices(drawLists);
            HashSet<FuWindow> windowsWithNativeGeometry = GetWindowsWithNativeGeometry(drawLists);
            for (int i = 0; i < drawLists.Length; i++)
            {
                ResolvedDrawList drawList = drawLists[i];
                if (drawList.Window != null)
                {
                    if (lastWindowDrawListIndices.TryGetValue(drawList.Window, out int lastIndex) &&
                        i == lastIndex &&
                        windowsWithNativeGeometry.Contains(drawList.Window))
                    {
                        cmd.AddWindowDrawData(drawList.Window);
                    }
                    continue;
                }

                // it's nothing to do with an UIWindow, let store it without any edition
                cmd.AddTransientDrawList(new DrawList(drawList.NativeDrawList));
            }

            cmd.FramebufferScale = imDrawDataPtr.FramebufferScale;
            cmd.DisplayPos = imDrawDataPtr.DisplayPos;
            cmd.DisplaySize = imDrawDataPtr.DisplaySize;
        }

        /// <summary>
        /// Returns the last native draw-list index for each Fugui window in the current ImGui frame.
        /// </summary>
        /// <param name="drawLists">Resolved draw lists for the current frame.</param>
        /// <returns>Last draw-list index by window.</returns>
        private static Dictionary<FuWindow, int> GetLastWindowDrawListIndices(ResolvedDrawList[] drawLists)
        {
            Dictionary<FuWindow, int> result = new Dictionary<FuWindow, int>();
            for (int i = 0; i < drawLists.Length; i++)
            {
                FuWindow window = drawLists[i].Window;
                if (window == null)
                {
                    continue;
                }

                result[window] = i;
            }

            return result;
        }

        /// <summary>
        /// Returns windows that still produced native ImGui geometry in this frame.
        /// </summary>
        /// <param name="drawLists">Resolved draw lists for the current frame.</param>
        /// <returns>Windows with at least one non-empty native draw list.</returns>
        private static HashSet<FuWindow> GetWindowsWithNativeGeometry(ResolvedDrawList[] drawLists)
        {
            HashSet<FuWindow> result = new HashSet<FuWindow>();
            for (int i = 0; i < drawLists.Length; i++)
            {
                FuWindow window = drawLists[i].Window;
                if (window == null || result.Contains(window))
                {
                    continue;
                }

                if (HasNativeGeometry(drawLists[i].NativeDrawList))
                {
                    result.Add(window);
                }
            }

            return result;
        }

        /// <summary>
        /// Returns whether a native draw list contains geometry that can be rendered.
        /// </summary>
        /// <param name="drawList">Native ImGui draw list.</param>
        /// <returns>True when the draw list has commands, vertices and indices.</returns>
        private static bool HasNativeGeometry(ImDrawListPtr drawList)
        {
            return drawList.CmdBuffer.Size > 0 &&
                   drawList.VtxBuffer.Size > 0 &&
                   drawList.IdxBuffer.Size > 0;
        }

        /// <summary>
        /// Rebuild render meshes for windows that were redrawn in the just-finished ImGui frame.
        /// </summary>
        /// <param name="drawLists">Resolved draw lists for the current frame.</param>
        /// <param name="displaySize">Draw data display size.</param>
        /// <param name="framebufferScale">Draw data framebuffer scale.</param>
        private static void RebuildWindowRenderMeshes(ResolvedDrawList[] drawLists, Vector2 displaySize, Vector2 framebufferScale)
        {
            HashSet<FuWindow> rebuiltWindows = new HashSet<FuWindow>();
            for (int i = 0; i < drawLists.Length; i++)
            {
                FuWindow window = drawLists[i].Window;
                if (window == null || !window.HasJustBeenDraw || rebuiltWindows.Contains(window))
                {
                    continue;
                }

                List<DrawList> orderedDrawLists = new List<DrawList>();
                int firstIncludedIndex = GetLastRootDrawListIndex(drawLists, window);
                for (int j = 0; j < drawLists.Length; j++)
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
                        window.DrawList.Dispose();
                        window.DrawList.Bind(drawList.NativeDrawList);
                        orderedDrawLists.Add(window.DrawList);
                    }
                    else if (drawList.IsWindowChild)
                    {
                        orderedDrawLists.Add(new DrawList(drawList.NativeDrawList));
                    }
                }

                window.CacheDrawData(orderedDrawLists, displaySize, framebufferScale);
                window.HasJustBeenDraw = false;
                rebuiltWindows.Add(window);
            }
        }

        /// <summary>
        /// Returns the last root draw-list index for a window when it appears more than once in a frame.
        /// </summary>
        /// <param name="drawLists">Resolved draw lists for the current frame.</param>
        /// <param name="window">Window to inspect.</param>
        /// <returns>Last root draw-list index, or the first owned draw-list index when no root was found.</returns>
        private static int GetLastRootDrawListIndex(ResolvedDrawList[] drawLists, FuWindow window)
        {
            int firstOwnedIndex = -1;
            int lastRootIndex = -1;

            for (int i = 0; i < drawLists.Length; i++)
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
            public FuWindow Window;
            public bool IsWindowRoot;
            public bool IsWindowChild;
        }
        #endregion
    }
}
