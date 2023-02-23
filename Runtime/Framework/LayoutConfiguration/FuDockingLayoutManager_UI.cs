using Fu.Framework;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fu
{
    public static partial class Fugui
    {
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
                        _layoutManagement_grid.Combobox("Available layouts", FuDockingLayoutManager.Layouts.Keys.ToList(), (key) =>
                        {
                            FuDockingLayoutManager.CurrentLayout = FuDockingLayoutManager.Layouts[key];
                            FuDockingLayoutManager.DisplayLayoutName = key;
                        },
                        () =>
                        {
                            return FuDockingLayoutManager.DisplayLayoutName;
                        });

                        _layoutManagement_grid.TextInput("Edit layout name", ref FuDockingLayoutManager.DisplayLayoutName);
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
                            if (string.IsNullOrEmpty(FuDockingLayoutManager.DisplayLayoutName))
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
                        gridInfo.ListBox("Binded windows", dockSpaceDefinition.WindowsDefinition.Values.ToList());
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
                    using (FuGrid windowsDefinition_grid = new FuGrid("windowsDefinition_grid"))
                    {
                        windowsDefinition_grid.Combobox("Windows definition", FuDockingLayoutManager._fuguiWindows.Values.ToList(), (x) => { FuDockingLayoutManager._selectedWindowDefinition = x; }, () => { return FuDockingLayoutManager._selectedWindowDefinition; });
                        windowsDefinition_grid.TextInput("Window name", ref FuDockingLayoutManager._windowsToAdd);

                        if (!string.IsNullOrEmpty(FuDockingLayoutManager._windowsToAdd))
                        {
                            if (FuDockingLayoutManager._fuguiWindows.Values.Contains(FuDockingLayoutManager._windowsToAdd))
                            {
                                layout.SmartText(string.Format($"<color=red>The name <b>'{FuDockingLayoutManager._windowsToAdd}'</b> is already present in the current FuGui windows definition !</color>"));
                            }
                            else
                            {
                                if (!Fugui.IsAlphaNumericWithSpaces(FuDockingLayoutManager._windowsToAdd))
                                {
                                    layout.SmartText(string.Format($"<color=red>The name <b>'{FuDockingLayoutManager._windowsToAdd}'</b> is not a valid name for a FuGui window !</color>"));
                                }
                                else
                                {
                                    layout.Spacing();
                                    if (layout.Button("Add new FuGui window definition", FuButtonStyle.Success))
                                    {
                                        if (!FuDockingLayoutManager._fuguiWindows.Values.Contains(FuDockingLayoutManager._windowsToAdd))
                                        {
                                            int newIndex = FuDockingLayoutManager._fuguiWindows.Max(x => x.Key) + 1;
                                            FuDockingLayoutManager._fuguiWindows.Add((ushort)(FuDockingLayoutManager._fuguiWindows.Keys.Last() + 1), FuDockingLayoutManager._windowsToAdd);

                                            Dictionary<ushort, string> formatedFuguiWindowsName = new Dictionary<ushort, string>();

                                            foreach (KeyValuePair<ushort, string> fuguiItem in FuDockingLayoutManager._fuguiWindows)
                                            {
                                                formatedFuguiWindowsName.Add(fuguiItem.Key, RemoveSpaceAndCapitalize(fuguiItem.Value));
                                            }

                                            FuDockingLayoutManager.writeToFile(Settings.FUGUI_WINDOWS_DEF_ENUM_PATH, FuDockingLayoutManager.generateEnum("FuWindowsNames", formatedFuguiWindowsName));
                                            FuDockingLayoutManager._windowsToAdd = string.Empty;
                                        }
                                    }
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(FuDockingLayoutManager._selectedWindowDefinition) && FuDockingLayoutManager._selectedWindowDefinition != "None")
                        {
                            if (layout.Button(string.Format($"Remove {FuDockingLayoutManager._selectedWindowDefinition}"), FuButtonStyle.Danger))
                            {
                                if (FuDockingLayoutManager._fuguiWindows.Values.Contains(FuDockingLayoutManager._selectedWindowDefinition))
                                {
                                    ushort keyToDelete = ushort.MaxValue;

                                    foreach (KeyValuePair<ushort, string> item in FuDockingLayoutManager._fuguiWindows)
                                    {
                                        if (item.Value == FuDockingLayoutManager._selectedWindowDefinition)
                                        {
                                            keyToDelete = item.Key;
                                            break;
                                        }
                                    }

                                    if (keyToDelete < ushort.MaxValue)
                                    {
                                        FuDockingLayoutManager._fuguiWindows.Remove(keyToDelete);
                                        FuDockingLayoutManager.writeToFile(Settings.FUGUI_WINDOWS_DEF_ENUM_PATH, FuDockingLayoutManager.generateEnum("FuWindowsNames", FuDockingLayoutManager._fuguiWindows));
                                        FuDockingLayoutManager._selectedWindowDefinition = "None";
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
                            for (int i = 0; i < FuDockingLayoutManager._fuguiWindows.Count; i++)
                            {
                                KeyValuePair<ushort, string> item = FuDockingLayoutManager._fuguiWindows.ElementAt(i);

                                if (item.Value == "None")
                                {
                                    continue;
                                }

                                if (FuDockingLayoutManager._definedDockSpaces != null)
                                {
                                    tempGrid.Combobox(item.Value, FuDockingLayoutManager._definedDockSpaces.Values.ToList(), (x) =>
                                    {
                                        if (x != null)
                                        {
                                            if (x == "None")
                                            {
                                                FuDockingLayoutManager.unbindWindowToDockspace(item.Key);
                                            }
                                            else
                                            {
                                                FuDockingLayoutManager.bindWindowToDockspace(item.Key, x);
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
    }
}