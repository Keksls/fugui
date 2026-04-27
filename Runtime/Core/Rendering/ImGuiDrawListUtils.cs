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
                string name = imDrawDataPtr.CmdLists[i]._OwnerName;

                // prevent icons name to switch render (for some reason, ImGui copy the name like 'name/name##pathID' when name contain Icon)
                if (name.StartsWith("???"))
                {
                    if (!_unIconnizedTitleMapping.ContainsKey(name))
                    {
                        string escapedTitle = name.Remove(0, 3).Split('/')[0]; // get icon escaped title
                        // search csharp formated window title
                        string csharpeEquivalentTitle = string.Empty;
                        foreach (string windowTitle in windows.Keys)
                        {
                            // icon escaped csharp formated title match excaped native formated title
                            if (windowTitle.Remove(0, 1) == escapedTitle)
                            {
                                csharpeEquivalentTitle = name.Replace("???", windowTitle.Substring(0, 1));
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
                                windows[name.Split('/')[0]].ChildrenDrawLists[name] = new DrawList(imDrawDataPtr.CmdLists[i]);
                                continue;
                            }
                        }
                        else // the window is not docked
                        {
                            // it's a child of a windows, we must store it.
                            windows[name.Split('/')[0]].ChildrenDrawLists[name] = new DrawList(imDrawDataPtr.CmdLists[i]);
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
                        windows[name].DrawList.Bind(imDrawDataPtr.CmdLists[i]);
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
                    cmd.AddDrawList(new DrawList(imDrawDataPtr.CmdLists[i]));
                }
            }

            cmd.FramebufferScale = imDrawDataPtr.FramebufferScale;
            cmd.DisplayPos = imDrawDataPtr.DisplayPos;
            cmd.DisplaySize = imDrawDataPtr.DisplaySize;
        }
        #endregion
    }
}