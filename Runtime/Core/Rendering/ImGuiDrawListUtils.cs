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
        [ThreadStatic]
        private static Scratch _scratch;
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
        /// <returns>True when the render output changed and an offscreen target should be redrawn.</returns>
        public static bool GetDrawCmd(Dictionary<string, FuWindow> windows, ImDrawDataPtr imDrawDataPtr, ref DrawData cmd)
        {
            Scratch scratch = GetScratch();
            scratch.ClearFrame(imDrawDataPtr.CmdListsCount);
            bool hadRenderableData = cmd.CmdListsCount > 0 && cmd.TotalVtxCount > 0;
            bool hadTransientDrawData = cmd.HasTransientRenderItems;

            cmd.Clear();

            for (int i = 0; i < imDrawDataPtr.CmdListsCount; i++)
            {
                ImDrawListPtr nativeDrawList = imDrawDataPtr.CmdLists[i];
                string name = getWindowName(windows, nativeDrawList._OwnerName);
                FuWindow window = null;
                bool isWindowRoot = windows.TryGetValue(name, out window);
                bool isWindowChild = false;

                if (!isWindowRoot) // it may be a window's child
                {
                    isWindowChild = TryResolveChildWindow(windows, name, out window);
                }

                scratch.DrawLists.Add(new ResolvedDrawList
                {
                    NativeDrawList = nativeDrawList,
                    Window = window,
                    IsWindowRoot = isWindowRoot,
                    IsWindowChild = isWindowChild
                });
            }

            bool renderOutputChanged = RebuildWindowRenderMeshes(
                scratch.DrawLists,
                imDrawDataPtr.DisplaySize,
                imDrawDataPtr.FramebufferScale,
                scratch.RebuiltWindows,
                scratch.OrderedDrawLists);

            BuildLastWindowDrawListIndices(scratch.DrawLists, scratch.LastWindowDrawListIndices);
            BuildWindowsWithNativeGeometry(scratch.DrawLists, scratch.WindowsWithNativeGeometry);

            bool hasTransientDrawData = false;
            for (int i = 0; i < scratch.DrawLists.Count; i++)
            {
                ResolvedDrawList drawList = scratch.DrawLists[i];
                if (drawList.Window != null)
                {
                    if (scratch.LastWindowDrawListIndices.TryGetValue(drawList.Window, out int lastIndex) &&
                        i == lastIndex &&
                        (scratch.WindowsWithNativeGeometry.Contains(drawList.Window) ||
                         drawList.Window.CachedDrawLists.Count > 0))
                    {
                        cmd.AddWindowDrawData(drawList.Window);
                    }
                    continue;
                }

                // it's nothing to do with an UIWindow, let store it without any edition
                if (HasNativeGeometry(drawList.NativeDrawList))
                {
                    cmd.AddTransientNativeDrawList(drawList.NativeDrawList);
                    hasTransientDrawData = true;
                }
            }

            cmd.FramebufferScale = imDrawDataPtr.FramebufferScale;
            cmd.DisplayPos = imDrawDataPtr.DisplayPos;
            cmd.DisplaySize = imDrawDataPtr.DisplaySize;

            bool hasRenderableData = cmd.CmdListsCount > 0 && cmd.TotalVtxCount > 0;
            renderOutputChanged |= hasTransientDrawData;
            renderOutputChanged |= hadTransientDrawData != hasTransientDrawData;
            renderOutputChanged |= hadRenderableData != hasRenderableData;
            return renderOutputChanged;
        }

        /// <summary>
        /// Fills the last native draw-list index for each Fugui window in the current ImGui frame.
        /// </summary>
        /// <param name="drawLists">Resolved draw lists for the current frame.</param>
        /// <param name="result">Last draw-list index by window.</param>
        private static void BuildLastWindowDrawListIndices(List<ResolvedDrawList> drawLists, Dictionary<FuWindow, int> result)
        {
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
        /// Fills windows that still produced native ImGui geometry in this frame.
        /// </summary>
        /// <param name="drawLists">Resolved draw lists for the current frame.</param>
        /// <param name="result">Windows with at least one non-empty native draw list.</param>
        private static void BuildWindowsWithNativeGeometry(List<ResolvedDrawList> drawLists, HashSet<FuWindow> result)
        {
            for (int i = 0; i < drawLists.Count; i++)
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
        /// <param name="rebuiltWindows">Scratch set used to avoid rebuilding a window twice.</param>
        /// <param name="orderedDrawLists">Scratch list used to collect the window draw lists in render order.</param>
        /// <returns>True when at least one window mesh was rebuilt.</returns>
        private static bool RebuildWindowRenderMeshes(
            List<ResolvedDrawList> drawLists,
            Vector2 displaySize,
            Vector2 framebufferScale,
            HashSet<FuWindow> rebuiltWindows,
            List<DrawList> orderedDrawLists)
        {
            bool rebuiltAnyWindow = false;
            for (int i = 0; i < drawLists.Count; i++)
            {
                FuWindow window = drawLists[i].Window;
                if (window == null || !window.HasJustBeenDraw || rebuiltWindows.Contains(window))
                {
                    continue;
                }

                orderedDrawLists.Clear();
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
                rebuiltAnyWindow = true;
            }

            orderedDrawLists.Clear();
            return rebuiltAnyWindow;
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
        /// Gets the reusable per-thread scratch buffers used while resolving draw data.
        /// </summary>
        /// <returns>Scratch buffers.</returns>
        private static Scratch GetScratch()
        {
            return _scratch ??= new Scratch();
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
        /// Resolve an ImGui child draw list back to the Fugui window that owns it.
        /// </summary>
        /// <param name="windows">Known Fugui windows in the current container.</param>
        /// <param name="name">Native ImGui draw list owner name.</param>
        /// <param name="window">Resolved owner window.</param>
        /// <returns>True when the draw list belongs to a Fugui child surface.</returns>
        private static bool TryResolveChildWindow(Dictionary<string, FuWindow> windows, string name, out FuWindow window)
        {
            window = null;
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            int firstSlashIndex = name.IndexOf('/');
            if (firstSlashIndex >= 0)
            {
                string rootWindowName = name.Substring(0, firstSlashIndex);
                if (windows.TryGetValue(rootWindowName, out FuWindow rootWindow))
                {
                    window = rootWindow;
                    return true;
                }
            }

            return false;
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

        /// <summary>
        /// Reusable scratch data for one GetDrawCmd invocation.
        /// </summary>
        private sealed class Scratch
        {
            public readonly List<ResolvedDrawList> DrawLists = new List<ResolvedDrawList>();
            public readonly Dictionary<FuWindow, int> LastWindowDrawListIndices = new Dictionary<FuWindow, int>();
            public readonly HashSet<FuWindow> WindowsWithNativeGeometry = new HashSet<FuWindow>();
            public readonly HashSet<FuWindow> RebuiltWindows = new HashSet<FuWindow>();
            public readonly List<DrawList> OrderedDrawLists = new List<DrawList>();

            public void ClearFrame(int drawListCount)
            {
                DrawLists.Clear();
                LastWindowDrawListIndices.Clear();
                WindowsWithNativeGeometry.Clear();
                RebuiltWindows.Clear();
                OrderedDrawLists.Clear();

                if (DrawLists.Capacity < drawListCount)
                {
                    DrawLists.Capacity = drawListCount;
                }

                if (OrderedDrawLists.Capacity < drawListCount)
                {
                    OrderedDrawLists.Capacity = drawListCount;
                }
            }
        }
        #endregion
    }
}
