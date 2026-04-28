using ImGuiNET;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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

            // bind current draw lists
            for (int i = 0; i < imDrawDataPtr.CmdListsCount; i++)
            {
                string name = getWindowName(windows, imDrawDataPtr.CmdLists[i]._OwnerName);

                bool isChild = false;
                if (!windows.ContainsKey(name)) // it may be a window's child
                {
                    int firstSlashIndex = name.IndexOf('/');
                    if (firstSlashIndex >= 0)
                    {
                        string rootWindowName = name.Substring(0, firstSlashIndex);
                        if (windows.TryGetValue(rootWindowName, out FuWindow rootWindow)) // it's a window's child
                        {
                            if (rootWindow.IsDocked) // the window is docked
                            {
                                int secondSlashIndex = name.IndexOf('/', firstSlashIndex + 1);
                                if (secondSlashIndex < 0) // it's a direct window's child (so it may be the forced child used to store vtx)
                                {
                                    string childWindowName = name.Substring(firstSlashIndex + 1);
                                    isChild = childWindowName.StartsWith(rootWindowName + "ctnr"); // is it the forced child ?
                                    name = isChild ? rootWindowName : "";
                                }
                                else // it's a child lvl 2+, so child of child, we need to store it too
                                {
                                    // it's a child of a windows, we must store it.
                                    rootWindow.ChildrenDrawLists[name] = new DrawList(imDrawDataPtr.CmdLists[i]);
                                    continue;
                                }
                            }
                            else // the window is not docked
                            {
                                // it's a child of a windows, we must store it.
                                rootWindow.ChildrenDrawLists[name] = new DrawList(imDrawDataPtr.CmdLists[i]);
                            }
                        }
                    }
                }

                // save window draw cmd if window has just been draw and it's an UIWindow
                if (windows.TryGetValue(name, out FuWindow window) && (window.IsDocked && isChild || !window.IsDocked))
                {
                    // frame has just been redraw, we must store drawList
                    if (window.HasJustBeenDraw)
                    {
                        // dispose window draw list GH handles (free memory)
                        window.DrawList.Dispose();
                        // copy cmd, idx and vtx buffers
                        window.DrawList.Bind(imDrawDataPtr.CmdLists[i]);
                        // window is stored, it's not just draw anymore
                        window.HasJustBeenDraw = false;
                        // add window draw list to current drawData
                        cmd.AddDrawList(window.DrawList);
                        // add window children lists to current drawData
                        cmd.AddDrawLists(window.ChildrenDrawLists.Values);
                        // dispose children draw list before clearing it => will not be done by garbage collectore because GChandle are pinned
                        foreach (var pair in window.ChildrenDrawLists)
                        {
                            pair.Value.Dispose();
                        }
                        // clear children draw lists (because we just redraw the window, so children will follow, see above)
                        window.ChildrenDrawLists.Clear();
                    }
                    else
                    {
                        // add stored draw lists related to this window
                        cmd.AddDrawList(window.DrawList);
                        cmd.AddDrawLists(window.ChildrenDrawLists.Values);
                    }
                }
                else
                {
                    // it's nothing to do with an UIWindow, let store it without any edition
                    cmd.AddDrawList(new DrawList(imDrawDataPtr.CmdLists[i]));
                }
            }

            cmd.FramebufferScale = imDrawDataPtr.FramebufferScale;
            cmd.DisplayPos = imDrawDataPtr.DisplayPos;
            cmd.DisplaySize = imDrawDataPtr.DisplaySize;
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
        #endregion
    }
}
