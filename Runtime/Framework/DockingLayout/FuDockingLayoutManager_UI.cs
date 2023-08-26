using Fu.Core;
using Fu.Framework;
using ImGuiNET;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fu
{
    public static partial class Fugui
    {
        #region New UI
        private static FuDockingLayoutDefinition _hoveredNode = null;
        private static FuDockingLayoutDefinition _lastFrameHoveredNode = null;
        private static Rect _currentWindowRect = new Rect();
        private static FuWindowName _selectedWindowDefinition = FuSystemWindowsNames.None;

        public static void DrawDockSpacelayoutCreator()
        {
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            _currentWindowRect = new Rect(ImGui.GetCursorScreenPos(), ImGui.GetContentRegionAvail());

            if (FuDockingLayoutManager.CurrentLayout == null && FuDockingLayoutManager.Layouts.Count == 0)
            {
                FuDockingLayoutManager.createNewLayout();
            }
            if (FuDockingLayoutManager.CurrentLayout == null)
            {
                FuDockingLayoutManager.CurrentLayout = FuDockingLayoutManager.Layouts.First().Value;
            }
            _hoveredNode = null;
            DrawDockNode(FuDockingLayoutManager.CurrentLayout, UIDockSpaceOrientation.None, 0f, 0, _currentWindowRect, drawList);
            _lastFrameHoveredNode = _hoveredNode;
            // draw context menu
            if (_hoveredNode != null)
            {
                var builder = FuContextMenuBuilder.Start()
                     .AddItem("Horizontal", () => { SetDockNodeOrientation(_hoveredNode, UIDockSpaceOrientation.Horizontal); })
                     .AddItem("Vertical", () => { SetDockNodeOrientation(_hoveredNode, UIDockSpaceOrientation.Vertical); })
                     .AddItem("Remove", () =>
                     {
                         FuDockingLayoutDefinition parent = null;
                         GetParentNodeOnNode(FuDockingLayoutManager.CurrentLayout, _hoveredNode, ref parent);
                         if (parent != null)
                         {
                             SetDockNodeOrientation(parent, UIDockSpaceOrientation.None);
                         }
                     })
                     .AddSeparator()
                     .BeginChild("Windows");

                foreach (var windowName in FuDockingLayoutManager.RegisteredWindowsNames.Values)
                {
                    builder.AddItem((_hoveredNode.WindowsDefinition.Contains(windowName.ID) ? "V " : "  ") + windowName.Name, () =>
                    {
                        if (_hoveredNode.WindowsDefinition.Contains(windowName.ID))
                        {
                            _hoveredNode.WindowsDefinition.Remove(windowName.ID);
                        }
                        else
                        {
                            _hoveredNode.WindowsDefinition.Add(windowName.ID);
                        }
                    });
                }
                builder.EndChild();

                PushContextMenuItems(builder.Build());
                TryOpenContextMenuOnWindowClick();
                PopContextMenuItems();
            }
        }

        private static void DrawDockNode(FuDockingLayoutDefinition node, UIDockSpaceOrientation orientation, float proportion, int childIndex, Rect parentRect, ImDrawListPtr drawList)
        {
            Vector2 min = parentRect.min;
            Vector2 max;
            Vector2 padding = Vector2.one * 6f;
            switch (orientation)
            {
                default:
                case UIDockSpaceOrientation.None:
                    max = parentRect.max;
                    break;

                case UIDockSpaceOrientation.Horizontal:
                    if (childIndex == 0)
                    {
                        max = new Vector2(min.x + parentRect.width * proportion, parentRect.max.y);
                    }
                    else
                    {
                        min.x += parentRect.width * proportion;
                        max = new Vector2(parentRect.max.x, parentRect.max.y);
                    }
                    break;

                case UIDockSpaceOrientation.Vertical:
                    if (childIndex == 0)
                    {
                        max = new Vector2(parentRect.max.x, min.y + parentRect.height * proportion);
                    }
                    else
                    {
                        min.y += parentRect.height * proportion;
                        max = new Vector2(parentRect.max.x, parentRect.max.y);
                    }
                    break;
            }

            if (_lastFrameHoveredNode == node || node.Children.Count == 0)
            {
                drawList.AddRectFilled(min + padding, max - padding, ImGui.GetColorU32(_lastFrameHoveredNode == node ? new Vector4(0, 0, 0, 0.66f) : new Vector4(0, 0, 0, 0.33f)));
                drawList.AddRect(min + padding, max - padding, ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 1f)), 2f);
            }

            if (ImGui.IsMouseHoveringRect(min + padding, max - padding) && _draggingNode == null && node.Children.Count == 0)
            {
                _hoveredNode = node;
            }

            if (node.Orientation == UIDockSpaceOrientation.Horizontal || node.Orientation == UIDockSpaceOrientation.Vertical)
            {
                DrawSeparator(node, new Rect(min, max - min), drawList);
            }

            // write windows names
            ImGui.SetCursorScreenPos(min + padding * 2f);
            foreach (var windowName in node.WindowsDefinition)
            {
                ImGui.SetCursorScreenPos(new Vector2(min.x + padding.x * 2f, ImGui.GetCursorScreenPos().y));
                ImGui.Text(FuDockingLayoutManager.RegisteredWindowsNames[windowName].Name);
            }

            int i = 0;
            foreach (var child in node.Children)
            {
                DrawDockNode(child, node.Orientation, node.Proportion, i, new Rect(min, max - min), drawList);
                i++;
            }
        }

        private static FuDockingLayoutDefinition _draggingNode = null;
        private static Vector2 _draggingNodeMousePosition = Vector2.zero;
        private static void DrawSeparator(FuDockingLayoutDefinition node, Rect parentRect, ImDrawListPtr drawList)
        {
            Vector2 min = default, max = default;
            switch (node.Orientation)
            {
                case UIDockSpaceOrientation.Horizontal:
                    min = new Vector2(parentRect.xMin + (parentRect.xMax - parentRect.xMin) * node.Proportion, parentRect.yMin);
                    max = new Vector2(parentRect.xMin + (parentRect.xMax - parentRect.xMin) * node.Proportion, parentRect.yMax);
                    break;

                case UIDockSpaceOrientation.Vertical:
                    min = new Vector2(parentRect.xMin, parentRect.yMin + (parentRect.yMax - parentRect.yMin) * node.Proportion);
                    max = new Vector2(parentRect.xMax, parentRect.yMin + (parentRect.yMax - parentRect.yMin) * node.Proportion);
                    break;
            }

            // get states
            Vector2 dragPadding = new Vector2(4f, 4f);
            bool hovered = ImGui.IsMouseHoveringRect(min - dragPadding, max + dragPadding);
            bool active = false;
            if (_draggingNode != null)
            {
                active = _draggingNode == node;
            }
            else
            {
                if (hovered && ImGui.IsMouseDown(ImGuiMouseButton.Left))
                {
                    _draggingNode = node;
                    _draggingNodeMousePosition = ImGui.GetMousePos();
                    active = true;
                }
            }
            if (active && !ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                _draggingNode = null;
                active = false;
            }

            // draw line
            drawList.AddLine(min, max, ImGui.GetColorU32(active ? Color.white : hovered ? Color.gray : Color.black), 4f);

            // dragging
            if (active)
            {
                Vector2 mousePos = ImGui.GetMousePos();
                mousePos.x = Mathf.Clamp(mousePos.x, parentRect.min.x, parentRect.max.x);
                mousePos.y = Mathf.Clamp(mousePos.y, parentRect.min.y, parentRect.max.y);

                float proportion = 0f;
                switch (node.Orientation)
                {
                    case UIDockSpaceOrientation.Horizontal:
                        proportion = (mousePos.x - parentRect.min.x) / (parentRect.max.x - parentRect.min.x);
                        break;

                    case UIDockSpaceOrientation.Vertical:
                        proportion = (mousePos.y - parentRect.min.y) / (parentRect.max.y - parentRect.min.y);
                        break;
                }
                if (ImGui.IsKeyDown(ImGuiKey.LeftCtrl) || ImGui.IsKeyDown(ImGuiKey.RightCtrl))
                {
                    proportion *= (1f / 0.01f);
                    proportion = (int)proportion;
                    proportion *= 0.01f;
                }
                node.Proportion = proportion;

                float screenProportion = 0f;
                switch (node.Orientation)
                {
                    case UIDockSpaceOrientation.Horizontal:
                        screenProportion = parentRect.x / _currentWindowRect.width + (parentRect.width * proportion) / _currentWindowRect.width;
                        break;

                    case UIDockSpaceOrientation.Vertical:
                        screenProportion = (parentRect.height * proportion) / _currentWindowRect.height;
                        break;
                }

                ImGui.SetTooltip("Node Proportion : " + (proportion * 100f).ToString("f1") + " %\n" +
                    "Screen Proportion : " + (screenProportion * 100f).ToString("f1") + " %\n" +
                    "\nKeep ctrl pressed to snap");
            }
        }

        private static void SetDockNodeOrientation(FuDockingLayoutDefinition node, UIDockSpaceOrientation orientation)
        {
            switch (orientation)
            {
                default:
                case UIDockSpaceOrientation.None:
                    DockSpaceDefinitionClearChildren(node);
                    break;

                case UIDockSpaceOrientation.Horizontal:
                    if (node.Children.Count == 0 && node.Orientation == UIDockSpaceOrientation.None)
                    {
                        DockSpaceDefinitionClearChildren(node);

                        node.Orientation = UIDockSpaceOrientation.Horizontal;
                        uint nextID = FuDockingLayoutManager.CurrentLayout.GetTotalChildren();

                        FuDockingLayoutDefinition leftPart = new FuDockingLayoutDefinition(node.Name + "_SplitH_Left", nextID + 1);
                        FuDockingLayoutDefinition rightPart = new FuDockingLayoutDefinition(node.Name + "_SplitH_Right", nextID + 2);

                        node.Children.Add(leftPart);
                        node.Children.Add(rightPart);
                    }
                    break;

                case UIDockSpaceOrientation.Vertical:
                    {
                        DockSpaceDefinitionClearChildren(node);

                        node.Orientation = UIDockSpaceOrientation.Vertical;
                        uint nextID = FuDockingLayoutManager.CurrentLayout.GetTotalChildren();

                        FuDockingLayoutDefinition topPart = new FuDockingLayoutDefinition(node.Name + "_SplitV_Top", nextID + 1);
                        FuDockingLayoutDefinition bottomPart = new FuDockingLayoutDefinition(node.Name + "_SplitV_Bottom", nextID + 2);

                        node.Children.Add(topPart);
                        node.Children.Add(bottomPart);
                    }
                    break;
            }
        }

        private static void GetParentNodeOnNode(FuDockingLayoutDefinition node, FuDockingLayoutDefinition target, ref FuDockingLayoutDefinition parent)
        {
            foreach (var child in node.Children)
            {
                if (child == target)
                {
                    parent = node;
                    break;
                }
                GetParentNodeOnNode(child, target, ref parent);
            }
        }

        private static void DrawLayoutConfigPanel()
        {
            using (FuLayout layout = new FuLayout())
            {
                PushFont(FontType.Bold);
                layout.FramedText("Docking Layouts");
                PopFont();
            }

            bool saveAvailable = true;
            using (FuGrid grid = new FuGrid("_layoutManagement_grid"))
            {
                string layoutName = FuDockingLayoutManager.CurrentLayout?.Name ?? "Nothing selected";
                grid.Combobox("Available layouts", FuDockingLayoutManager.Layouts.Keys.ToList(), (index) =>
                {
                    var key = FuDockingLayoutManager.Layouts.Keys.ToList()[index];
                    FuDockingLayoutManager.CurrentLayout = FuDockingLayoutManager.Layouts[key];
                    FuDockingLayoutManager.CurrentLayout.Name = key;
                    Fugui.RefreshWindowsInstances(FuSystemWindowsNames.DockSpaceManager);
                },
                () =>
                {
                    return layoutName;
                });

                if (FuDockingLayoutManager.CurrentLayout == null)
                {
                    grid.DisableNextElement();
                }
                if (grid.TextInput("Edit layout name", ref layoutName))
                {
                    FuDockingLayoutManager.CurrentLayout.Name = layoutName;
                }
                saveAvailable = FuDockingLayoutManager.checkLayoutName(layoutName);

                if (!saveAvailable)
                {
                    grid.NextColumn();
                    grid.SmartText("<color=red>Current layout name is not allowed. Please enter a valid and unused file name, using only alphanumeric characters and underscores or dashes.</color>");
                }
            }

            using (FuGrid grid = new FuGrid("_buttonsAction_grid", new FuGridDefinition(3), FuGridFlag.Default))
            {
                // create button
                if (grid.Button("New##fdl", FuButtonStyle.Highlight))
                {
                    FuDockingLayoutManager.createNewLayout();
                }

                // save button and behaviors
                if (!saveAvailable || FuDockingLayoutManager.CurrentLayout == null)
                {
                    grid.DisableNextElement();
                }

                if (grid.Button("Save##fdl", FuButtonStyle.Info))
                {
                    string selectedName = FuDockingLayoutManager.CurrentLayout.Name;
                    FuDockingLayoutManager.saveSelectedLayout();
                    FuDockingLayoutManager.CurrentLayout = FuDockingLayoutManager.Layouts[selectedName];
                }

                if (grid.Button("Delete##fdl", FuButtonStyle.Danger))
                {
                    FuDockingLayoutManager.deleteSelectedLayout();
                }
            }
        }

        /// <summary>
        /// Clears the children of the dock space definition
        /// </summary>
        /// <param name="dockSpaceDefinition">The dock space definition to clear the children of</param>
        private static void DockSpaceDefinitionClearChildren(FuDockingLayoutDefinition dockSpaceDefinition)
        {
            // Set the orientation to None
            dockSpaceDefinition.Orientation = UIDockSpaceOrientation.None;

            // Clear the children list
            for (int i = 0; i < dockSpaceDefinition.Children.Count; i++)
            {
                dockSpaceDefinition.Children.Clear();
            }
        }

        private static bool _windowNamesMustSaveLayouts = false;
        /// <summary>
        /// Draw the UI of the Windows Definition Manager
        /// </summary>
        public static void DrawWindowsDefinitionManager()
        {
            DrawLayoutConfigPanel();

            bool canSave = true;
            using (FuLayout layout = new FuLayout())
            {
                PushFont(FontType.Bold);
                layout.FramedText("Windows Definitions");
                PopFont();

                var windows = FuDockingLayoutManager.RegisteredWindowsNames.Values.ToList();
                using (FuGrid grid = new FuGrid("windowsDefinition_grid"))
                {
                    windows.Remove(FuSystemWindowsNames.None);
                    windows.Remove(FuSystemWindowsNames.WindowsDefinitionManager);
                    windows.Remove(FuSystemWindowsNames.DockSpaceManager);
                    windows.Remove(FuSystemWindowsNames.FuguiSettings);

                    if (_selectedWindowDefinition.Equals(FuSystemWindowsNames.None) && windows.Count > 0)
                    {
                        _selectedWindowDefinition = windows[0];
                    }

                    grid.Combobox("Windows definition", windows,
                        (index) =>
                        {
                            _selectedWindowDefinition = windows[index];
                        }, () =>
                        {
                            return _selectedWindowDefinition;
                        });

                    string windowName = _selectedWindowDefinition.Name;
                    grid.SetNextElementToolTipWithLabel("The name (title and ID) of the window");
                    if (grid.TextInput("Window name", ref windowName))
                    {
                        _selectedWindowDefinition.SetName(windowName);
                        FuDockingLayoutManager.RegisteredWindowsNames[_selectedWindowDefinition.ID] = _selectedWindowDefinition;
                    }

                    if (FuDockingLayoutManager.RegisteredWindowsNames.Values.Where(wd => wd.Name == _selectedWindowDefinition.Name).Count() > 1)
                    {
                        grid.NextColumn();
                        grid.SmartText(string.Format($"<color=red>The name <b>'{_selectedWindowDefinition}'</b> is already present in the current FuGui windows definition !</color>"));
                        canSave = false;
                    }
                    else if (!IsAlphaNumericWithSpaces(_selectedWindowDefinition.Name) || !char.IsLetter(_selectedWindowDefinition.Name[0]))
                    {
                        grid.NextColumn();
                        grid.SmartText(string.Format($"<color=red>The name <b>'{_selectedWindowDefinition}'</b> is not a valid name for a FuGui window !</color>"));
                        canSave = false;
                    }

                    bool autoInstantiate = _selectedWindowDefinition.AutoInstantiateWindowOnlayoutSet;
                    grid.SetNextElementToolTipWithLabel("Whatever you want to instantiate this window whenever a layout that contains it is set");
                    if (grid.Toggle("Auto Instance", ref autoInstantiate))
                    {
                        _selectedWindowDefinition.SetAutoInstantiateOnLayoutSet(autoInstantiate);
                        FuDockingLayoutManager.RegisteredWindowsNames[_selectedWindowDefinition.ID] = _selectedWindowDefinition;
                    }

                    int idleFPS = _selectedWindowDefinition.IdleFPS;
                    grid.SetNextElementToolTipWithLabel("Target FPS to set when window is in Idle state.\n-1 will use Fugui settings value.\n0 will shut down window until it switch to ");
                    if (grid.Slider("Idle FPS", ref idleFPS, -1, 144))
                    {
                        _selectedWindowDefinition.SetIdleFPS((short)idleFPS);
                        FuDockingLayoutManager.RegisteredWindowsNames[_selectedWindowDefinition.ID] = _selectedWindowDefinition;
                    }
                }

                using (FuGrid grid = new FuGrid("_buttonsAction_grid", new FuGridDefinition(3), FuGridFlag.Default))
                {
                    // create button
                    if (grid.Button("New##wn", FuButtonStyle.Highlight))
                    {
                        ushort newIndex = (ushort)(FuDockingLayoutManager.RegisteredWindowsNames.Max(x => x.Key) + 1);
                        FuDockingLayoutManager.RegisteredWindowsNames.Add(newIndex, new FuWindowName(newIndex, "WindowName", true, -1));

                        _selectedWindowDefinition = FuDockingLayoutManager.RegisteredWindowsNames.Values.ToList().Last();
                    }

                    // delete button
                    if (grid.Button("Delete##wn", FuButtonStyle.Danger))
                    {
                        FuDockingLayoutManager.RegisteredWindowsNames.Remove(_selectedWindowDefinition.ID);
                        windows.Remove(_selectedWindowDefinition);

                        // remove window from all layout
                        foreach(var fdlayout in FuDockingLayoutManager.Layouts.Values)
                        {
                            fdlayout.RemoveWindowsDefinitionInChildren(_selectedWindowDefinition.ID);
                        }
                        _windowNamesMustSaveLayouts = true;
                        _selectedWindowDefinition = windows.Count > 0 ? windows[0] : FuSystemWindowsNames.None;
                    }

                    // save button
                    if (!canSave)
                    {
                        grid.DisableNextElements();
                    }
                    if (grid.Button("Save##wn", FuButtonStyle.Info))
                    {
                        foreach (KeyValuePair<ushort, FuWindowName> pair in FuDockingLayoutManager.RegisteredWindowsNames)
                        {
                            pair.Value.SetName(RemoveSpaceAndCapitalize(pair.Value.Name));
                        }

                        FuDockingLayoutManager.writeToFile(Settings.FUGUI_WINDOWS_DEF_ENUM_PATH, FuDockingLayoutManager.generateEnum("FuWindowsNames", FuDockingLayoutManager.RegisteredWindowsNames));

                        if(_windowNamesMustSaveLayouts)
                        {
                            foreach (var fdlayout in FuDockingLayoutManager.Layouts.Values)
                            {
                                FuDockingLayoutManager.saveLayoutFile(fdlayout);
                            }
                            _windowNamesMustSaveLayouts = false;
                        }

                        var names = FuDockingLayoutManager.RegisteredWindowsNames.Values.ToList();
                        _selectedWindowDefinition = names.Count > FuSystemWindowsNames.FuguiReservedLastID ? names[FuSystemWindowsNames.FuguiReservedLastID] : FuSystemWindowsNames.None;
                    }
                }
            }
        }
        #endregion
    }
}