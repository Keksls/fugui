using Codice.Client.BaseCommands;
using Fu.Core;
using Fu.Framework;
using ImGuiNET;
using System;
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
        public static void DrawDockSpacelayoutCreator()
        {
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            _currentWindowRect = new Rect(ImGui.GetCursorScreenPos(), ImGui.GetContentRegionAvail());

            if (FuDockingLayoutManager.CurrentLayout == null)
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
                var items = FuContextMenuBuilder.Start()
                    .AddItem("Horizontal", () => { SetDockNodeOrientation(_hoveredNode, UIDockSpaceOrientation.Horizontal); })
                    .AddItem("Vertical", () => { SetDockNodeOrientation(_hoveredNode, UIDockSpaceOrientation.Vertical); })
                    .AddSeparator()
                    .AddItem("Remove", () =>
                    {
                        FuDockingLayoutDefinition parent = null;
                        GetParentNodeOnNode(FuDockingLayoutManager.CurrentLayout, _hoveredNode, ref parent);
                        if (parent != null)
                        {
                            SetDockNodeOrientation(parent, UIDockSpaceOrientation.None);
                        }
                    })
                    .Build();
                PushContextMenuItems(items);
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
        #endregion

        #region Old UI
        /// <summary>
        /// Creates a UI panel that contains the dock space manager and the layout manager
        /// </summary>
        public static void DrawDockSpaceManager()
        {
            using (FuLayout loadSave_layout = new FuLayout())
            {
                loadSave_layout.Collapsable("Dockspace management", () =>
                {
                    bool saveAvailable = true;

                    using (FuGrid _layoutManagement_grid = new FuGrid("_layoutManagement_grid"))
                    {
                        _layoutManagement_grid.NextColumn();
                        _layoutManagement_grid.Text("Select a FuGui Layout Configuration in the list to edit. You can also create a new one and associate windows defination to layout and dockspaces. If you create a new one or edit an existing FuGui Layout, please clic on 'Save layout' button to save changes.");
                        _layoutManagement_grid.Combobox("Available layouts", FuDockingLayoutManager.Layouts.Keys.ToList(), (index) =>
                        {
                            var key = FuDockingLayoutManager.Layouts.Keys.ToList()[index];
                            FuDockingLayoutManager.CurrentLayout = FuDockingLayoutManager.Layouts[key];
                            FuDockingLayoutManager.CurrentLayoutName = key;
                        },
                        () =>
                        {
                            return FuDockingLayoutManager.CurrentLayoutName;
                        });

                        string layoutName = FuDockingLayoutManager.CurrentLayoutName;
                        if (_layoutManagement_grid.TextInput("Edit layout name", ref layoutName))
                        {
                            FuDockingLayoutManager.CurrentLayoutName = layoutName;
                        }
                        saveAvailable = FuDockingLayoutManager.checkSelectedName();

                        if (!saveAvailable)
                        {
                            _layoutManagement_grid.NextColumn();
                            _layoutManagement_grid.SmartText("<color=red>Current layout name is not allowed. Please enter a valid and unused file name, using only alphanumeric characters and underscores or dashes. The file name must end with <b>.flg</b>.</color>");
                        }

                        using (FuGrid _buttonsAction_grid = new FuGrid("_buttonsAction_grid", new FuGridDefinition(3), FuGridFlag.Default))
                        {
                            // create button
                            if (_buttonsAction_grid.Button("Create a new FuGui layout", FuButtonStyle.Highlight))
                            {
                                FuDockingLayoutManager.createNewLayout();
                            }

                            // save button and behaviors
                            if (!saveAvailable)
                            {
                                _buttonsAction_grid.DisableNextElement();
                            }

                            if (_buttonsAction_grid.Button("Save selected layout", FuButtonStyle.Info))
                            {
                                FuDockingLayoutManager.saveSelectedLayout();
                            }

                            // delete button and behaviors
                            if (string.IsNullOrEmpty(FuDockingLayoutManager.CurrentLayoutName))
                            {
                                _buttonsAction_grid.DisableNextElement();
                            }

                            if (_buttonsAction_grid.Button("Delete selected layout", FuButtonStyle.Danger))
                            {
                                FuDockingLayoutManager.deleteSelectedLayout();
                            }
                        }
                    }
                });

                if (FuDockingLayoutManager.CurrentLayout != null)
                {
                    loadSave_layout.Collapsable("Dockspace configuration", () =>
                    {
                        using (FuPanel dockSpaceList_panel = new FuPanel("dockSpaceList_panel"))
                        {
                            using (FuLayout dockSpaceList_layout = new FuLayout())
                            {
                                ShowTreeView(dockSpaceList_layout, FuDockingLayoutManager.CurrentLayout, true);
                                FuDockingLayoutManager.RefreshDockSpaces();
                            }
                        }
                    });
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

        /// <summary>
        /// Displays the specified dock space definition in a tree view using ImGui.
        /// The tree view allows the user to view and edit the properties of the dock space, such as its name, ID, proportion, and orientation.
        /// The tree view also allows the user to view and edit the child dock spaces of the current dock space.
        /// </summary>
        /// <param name="layout">The layout to use for displaying the tree view</param>
        /// <param name="dockSpaceDefinition">The dock space definition to show in the tree view</param>
        /// <param name="root">Flag to set the root node</param>
        private static void ShowTreeView(FuLayout layout, FuDockingLayoutDefinition dockSpaceDefinition, bool root = false)
        {
            if (root)
            {
                ImGui.SetNextItemOpen(true);
            }

            if (ImGui.TreeNode("DockSpace : " + dockSpaceDefinition.ID))
            {
                using (FuGrid gridInfo = new FuGrid("gridInfo" + dockSpaceDefinition.ID, FuGridDefinition.DefaultAuto))
                {
                    if (dockSpaceDefinition.Name == "None")
                    {
                        dockSpaceDefinition.Name = "Dockspace";
                    }

                    if (gridInfo.TextInput("Name" + dockSpaceDefinition.ID, ref dockSpaceDefinition.Name))
                    {
                        //Dockspace names changed, refresh other UI component
                        FuDockingLayoutManager.RefreshDockSpaces();
                    }

                    string tempID = dockSpaceDefinition.ID.ToString();
                    gridInfo.DisableNextElement();
                    gridInfo.TextInput("Dockspace Id" + dockSpaceDefinition.ID, ref tempID, FuFrameStyle.Default);

                    if (dockSpaceDefinition.Orientation != UIDockSpaceOrientation.None)
                    {
                        gridInfo.Slider("Proportion" + dockSpaceDefinition.ID, ref dockSpaceDefinition.Proportion, 0f, 1f);
                    }

                    if (dockSpaceDefinition.WindowsDefinition.Count > 0)
                    {
                        gridInfo.ListBox("Binded windows", () =>
                        {
                            foreach (ushort windowID in dockSpaceDefinition.WindowsDefinition)
                            {
                                ImGui.Selectable(FuDockingLayoutManager.RegisteredWindowsNames[windowID].Name);
                            }
                        }, FuElementSize.FullSize);
                    }
                }

                if (dockSpaceDefinition.Children != null)
                {
                    // This code checks the value of the 'orientation' enum and performs a specific action based on its value :
                    // None : Delete all children and set orientation to None
                    // Horizontal : Create 2 childs (left and right) and set orientation to Horizontal
                    // Vertical : Create 2 childs (top and buttom) and set orientation to Vertical
                    layout.ButtonsGroup<UIDockSpaceOrientation>("Orientation_" + dockSpaceDefinition.ID, (enumSelection) =>
                    {
                        switch ((UIDockSpaceOrientation)enumSelection)
                        {
                            default:
                            case UIDockSpaceOrientation.None:
                                DockSpaceDefinitionClearChildren(dockSpaceDefinition);
                                break;
                            case UIDockSpaceOrientation.Horizontal:
                                if (dockSpaceDefinition.Children.Count == 0 && dockSpaceDefinition.Orientation == UIDockSpaceOrientation.None)
                                {
                                    DockSpaceDefinitionClearChildren(dockSpaceDefinition);

                                    dockSpaceDefinition.Orientation = UIDockSpaceOrientation.Horizontal;
                                    uint nextID = FuDockingLayoutManager.CurrentLayout.GetTotalChildren();

                                    FuDockingLayoutDefinition leftPart = new FuDockingLayoutDefinition(dockSpaceDefinition.Name + "_SplitH_Left", nextID + 1);
                                    FuDockingLayoutDefinition rightPart = new FuDockingLayoutDefinition(dockSpaceDefinition.Name + "_SplitH_Right", nextID + 2);

                                    dockSpaceDefinition.Children.Add(leftPart);
                                    dockSpaceDefinition.Children.Add(rightPart);
                                }
                                break;
                            case UIDockSpaceOrientation.Vertical:
                                {
                                    DockSpaceDefinitionClearChildren(dockSpaceDefinition);

                                    dockSpaceDefinition.Orientation = UIDockSpaceOrientation.Vertical;
                                    uint nextID = FuDockingLayoutManager.CurrentLayout.GetTotalChildren();

                                    FuDockingLayoutDefinition topPart = new FuDockingLayoutDefinition(dockSpaceDefinition.Name + "_SplitV_Top", nextID + 1);
                                    FuDockingLayoutDefinition bottomPart = new FuDockingLayoutDefinition(dockSpaceDefinition.Name + "_SplitV_Bottom", nextID + 2);

                                    dockSpaceDefinition.Children.Add(topPart);
                                    dockSpaceDefinition.Children.Add(bottomPart);
                                }
                                break;
                        }

                        //Dockspace list has changed, refresh other UI component
                        FuDockingLayoutManager.RefreshDockSpaces();
                    }, () => dockSpaceDefinition.Orientation);

                    // REcursive display for children
                    foreach (FuDockingLayoutDefinition child in dockSpaceDefinition.Children)
                    {
                        layout.Separator();
                        ShowTreeView(layout, child);
                    }
                }

                ImGui.TreePop();
            }
        }

        /// <summary>
        /// Draw the UI of the Windows Definition Manager
        /// </summary>
        public static void DrawWindowsDefinitionManager()
        {
            using (FuLayout layout = new FuLayout())
            {
                layout.Collapsable("Manage Windows definitions", () =>
                {
                    using (FuGrid grid = new FuGrid("windowsDefinition_grid"))
                    {
                        grid.Combobox("Windows definition", FuDockingLayoutManager.RegisteredWindowsNames.Values.ToList(), (index) => { FuDockingLayoutManager.SelectedWindowDefinition = FuDockingLayoutManager.RegisteredWindowsNames.Values.ToList()[index]; }, () => { return FuDockingLayoutManager.SelectedWindowDefinition; });

                        string windowName = FuDockingLayoutManager.WindowsToAdd.Name;
                        if (grid.TextInput("Window name", ref windowName))
                        {
                            FuDockingLayoutManager.WindowsToAdd.SetName(windowName);
                        }

                        bool autoInstantiate = FuDockingLayoutManager.WindowsToAdd.AutoInstantiateWindowOnlayoutSet;
                        if (grid.Toggle("Auto Instantiate on layout set", ref autoInstantiate))
                        {
                            FuDockingLayoutManager.WindowsToAdd.SetAutoInstantiateOnLayoutSet(autoInstantiate);
                        }

                        int idleFPS = FuDockingLayoutManager.WindowsToAdd.IdleFPS;
                        if (grid.Slider("Auto Instantiate on layout set", ref idleFPS, -1, 144))
                        {
                            FuDockingLayoutManager.WindowsToAdd.SetIdleFPS((short)idleFPS);
                        }

                        if (!FuDockingLayoutManager.WindowsToAdd.Equals(FuSystemWindowsNames.None))
                        {
                            if (FuDockingLayoutManager.RegisteredWindowsNames.Values.Contains(FuDockingLayoutManager.WindowsToAdd))
                            {
                                layout.SmartText(string.Format($"<color=red>The name <b>'{FuDockingLayoutManager.WindowsToAdd}'</b> is already present in the current FuGui windows definition !</color>"));
                            }
                            else
                            {
                                if (!Fugui.IsAlphaNumericWithSpaces(FuDockingLayoutManager.WindowsToAdd.Name))
                                {
                                    layout.SmartText(string.Format($"<color=red>The name <b>'{FuDockingLayoutManager.WindowsToAdd}'</b> is not a valid name for a FuGui window !</color>"));
                                }
                                else
                                {
                                    layout.Spacing();
                                    if (layout.Button("Add new FuGui window definition", FuButtonStyle.Success))
                                    {
                                        if (!FuDockingLayoutManager.RegisteredWindowsNames.Values.Contains(FuDockingLayoutManager.WindowsToAdd))
                                        {
                                            int newIndex = FuDockingLayoutManager.RegisteredWindowsNames.Max(x => x.Key) + 1;
                                            FuDockingLayoutManager.RegisteredWindowsNames.Add((ushort)(FuDockingLayoutManager.RegisteredWindowsNames.Keys.Last() + 1), FuDockingLayoutManager.WindowsToAdd);

                                            foreach (KeyValuePair<ushort, FuWindowName> pair in FuDockingLayoutManager.RegisteredWindowsNames)
                                            {
                                                pair.Value.SetName(RemoveSpaceAndCapitalize(pair.Value.Name));
                                            }

                                            FuDockingLayoutManager.writeToFile(Settings.FUGUI_WINDOWS_DEF_ENUM_PATH, FuDockingLayoutManager.generateEnum("FuWindowsNames", FuDockingLayoutManager.RegisteredWindowsNames));
                                            FuDockingLayoutManager.WindowsToAdd = FuSystemWindowsNames.None;
                                        }
                                    }
                                }
                            }
                        }

                        if (!FuDockingLayoutManager.WindowsToAdd.Equals(FuSystemWindowsNames.None))
                        {
                            if (layout.Button(string.Format($"Remove {FuDockingLayoutManager.SelectedWindowDefinition}"), FuButtonStyle.Danger))
                            {
                                if (FuDockingLayoutManager.RegisteredWindowsNames.Values.Contains(FuDockingLayoutManager.SelectedWindowDefinition))
                                {
                                    ushort keyToDelete = ushort.MaxValue;

                                    foreach (KeyValuePair<ushort, FuWindowName> item in FuDockingLayoutManager.RegisteredWindowsNames)
                                    {
                                        if (item.Value.Equals(FuDockingLayoutManager.SelectedWindowDefinition))
                                        {
                                            keyToDelete = item.Key;
                                            break;
                                        }
                                    }

                                    if (keyToDelete < ushort.MaxValue)
                                    {
                                        FuDockingLayoutManager.RegisteredWindowsNames.Remove(keyToDelete);
                                        FuDockingLayoutManager.writeToFile(Settings.FUGUI_WINDOWS_DEF_ENUM_PATH, FuDockingLayoutManager.generateEnum("FuWindowsNames", FuDockingLayoutManager.RegisteredWindowsNames));
                                        FuDockingLayoutManager.SelectedWindowDefinition = FuSystemWindowsNames.None;
                                    }
                                }
                            }
                        }
                    }
                });

                if (FuDockingLayoutManager.CurrentLayout != null)
                {
                    layout.Collapsable("Bind Windows definition to DockSpace", () =>
                    {
                        using (FuGrid tempGrid = new FuGrid("bindWinDefToDockSpace_grid", new FuGridDefinition(2, new int[] { 150 })))
                        {
                            for (int i = 0; i < FuDockingLayoutManager.RegisteredWindowsNames.Count; i++)
                            {
                                KeyValuePair<ushort, FuWindowName> item = FuDockingLayoutManager.RegisteredWindowsNames.ElementAt(i);

                                if (item.Value.Equals(FuSystemWindowsNames.None))
                                {
                                    continue;
                                }

                                if (FuDockingLayoutManager.DefinedDockSpaces != null)
                                {
                                    tempGrid.Combobox(item.Value.Name, FuDockingLayoutManager.DefinedDockSpaces.Values.ToList(), (index) =>
                                    {
                                        var value = FuDockingLayoutManager.DefinedDockSpaces.Values.ToList()[index];
                                        if (value != null)
                                        {
                                            if (value == "None")
                                            {
                                                FuDockingLayoutManager.unbindWindowToDockspace(item.Key);
                                            }
                                            else
                                            {
                                                FuDockingLayoutManager.bindWindowToDockspace(item.Key, value);
                                            }
                                        }
                                    },
                                    () =>
                                    {
                                        return FuDockingLayoutManager.getBindedLayout(item.Key);
                                    });
                                }
                            }
                        };
                    });
                }
            }
        }
        #endregion
    }
}