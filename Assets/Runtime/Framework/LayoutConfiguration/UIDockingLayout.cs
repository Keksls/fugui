using Fugui.Core;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Fugui.Framework
{
    public static partial class FuGui
    {

        /// <summary>
        /// Creates a UI panel that contains the dock space manager and the layout manager
        /// </summary>
        /// <param name="windowDockSpaceDefinition">The UI window for the dock space manager</param>
        public static void DockSpaceManager(UIWindow windowDockSpaceDefinition)
        {
            using (UILayout loadSave_layout = new UILayout())
            {
                loadSave_layout.Collapsable("Dockspace management", () =>
                {
                    bool saveAvailable = true;

                    using (UIGrid _layoutManagement_grid = new UIGrid("_layoutManagement_grid"))
                    {
                        _layoutManagement_grid.NextColumn();
                        _layoutManagement_grid.Text("Select a FuGui Layout Configuration in the list to edit. You can also create a new one and associate windows defination to layout and dockspaces. If you create a new one or edit an existing FuGui Layout, please clic on 'Save layout' button to save changes.");
                        _layoutManagement_grid.Combobox("Available layouts", DockingLayoutManager.Layouts.Keys.ToList(), (key) =>
                        {
                            DockingLayoutManager.DisplayedLayout = DockingLayoutManager.Layouts[key];
                            DockingLayoutManager.DisplayLayoutName = key;
                        },
                        () =>
                        {
                            return DockingLayoutManager.DisplayLayoutName;
                        });

                        _layoutManagement_grid.TextInput("Edit layout name", ref DockingLayoutManager.DisplayLayoutName);
                        saveAvailable = DockingLayoutManager.checkSelectedName();

                        if (!saveAvailable)
                        {
                            _layoutManagement_grid.NextColumn();
                            _layoutManagement_grid.SmartText("<color=red>Current layout name is not allowed. Please enter a valid and unused file name, using only alphanumeric characters and underscores or dashes. The file name must end with <b>.flg</b>.</color>");
                        }

                        using (UIGrid _buttonsAction_grid = new UIGrid("_buttonsAction_grid", new UIGridDefinition(3), UIGridFlag.Default))
                        {
                            // create button
                            if (_buttonsAction_grid.Button("Create a new FuGui layout", UIButtonStyle.Highlight))
                            {
                                DockingLayoutManager.createNewLayout();
                            }

                            // save button and behaviors
                            if (!saveAvailable)
                            {
                                _buttonsAction_grid.DisableNextElement();
                            }

                            if (_buttonsAction_grid.Button("Save selected layout", UIButtonStyle.Info))
                            {
                                DockingLayoutManager.saveSelectedLayout();
                            }

                            // delete button and behaviors
                            if (string.IsNullOrEmpty(DockingLayoutManager.DisplayLayoutName))
                            {
                                _buttonsAction_grid.DisableNextElement();
                            }

                            if (_buttonsAction_grid.Button("Delete selected layout", UIButtonStyle.Danger))
                            {
                                DockingLayoutManager.deleteSelectedLayout();
                            }
                        }
                    }
                });

                if (DockingLayoutManager.DisplayedLayout != null)
                {
                    loadSave_layout.Collapsable("Dockspace configuration", () =>
                    {
                        using (UIPanel dockSpaceList_panel = new UIPanel("dockSpaceList_panel"))
                        {
                            using (UILayout dockSpaceList_layout = new UILayout())
                            {
                                ShowTreeView(dockSpaceList_layout, DockingLayoutManager.DisplayedLayout, true);
                                DockingLayoutManager.RefreshDockSpaces();
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
        private static void DockSpaceDefinitionClearChildren(UIDockSpaceDefinition dockSpaceDefinition)
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
        private static void ShowTreeView(UILayout layout, UIDockSpaceDefinition dockSpaceDefinition, bool root = false)
        {
            if (root)
            {
                ImGui.SetNextItemOpen(true);
            }

            if (ImGui.TreeNode("DockSpace : " + dockSpaceDefinition.ID))
            {
                using (UIGrid gridInfo = new UIGrid("gridInfo" + dockSpaceDefinition.ID, UIGridDefinition.DefaultAuto))
                {
                    if (dockSpaceDefinition.Name == "None")
                    {
                        dockSpaceDefinition.Name = "Dockspace";
                    }

                    if (gridInfo.TextInput("Name" + dockSpaceDefinition.ID, ref dockSpaceDefinition.Name))
                    {
                        //Dockspace names changed, refresh other UI component
                        DockingLayoutManager.RefreshDockSpaces();
                    }

                    string tempID = dockSpaceDefinition.ID.ToString();
                    gridInfo.DisableNextElement();
                    gridInfo.TextInput("Dockspace Id" + dockSpaceDefinition.ID, ref tempID, UIFrameStyle.Default);

                    if (dockSpaceDefinition.Orientation != UIDockSpaceOrientation.None)
                    {
                        gridInfo.Slider("Proportion" + dockSpaceDefinition.ID, ref dockSpaceDefinition.Proportion, 0f, 1f);
                    }

                    if (dockSpaceDefinition.WindowsDefinition.Count > 0)
                    {
                        gridInfo.Listbox("Binded windows", dockSpaceDefinition.WindowsDefinition.Values.ToList());
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
                        switch (enumSelection)
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
                                    uint nextID = DockingLayoutManager.DisplayedLayout.GetTotalChildren();

                                    UIDockSpaceDefinition leftPart = new UIDockSpaceDefinition(dockSpaceDefinition.Name + "_SplitH_Left", nextID + 1);
                                    UIDockSpaceDefinition rightPart = new UIDockSpaceDefinition(dockSpaceDefinition.Name + "_SplitH_Right", nextID + 2);

                                    dockSpaceDefinition.Children.Add(leftPart);
                                    dockSpaceDefinition.Children.Add(rightPart);
                                }
                                break;
                            case UIDockSpaceOrientation.Vertical:
                                {
                                    DockSpaceDefinitionClearChildren(dockSpaceDefinition);

                                    dockSpaceDefinition.Orientation = UIDockSpaceOrientation.Vertical;
                                    uint nextID = DockingLayoutManager.DisplayedLayout.GetTotalChildren();

                                    UIDockSpaceDefinition topPart = new UIDockSpaceDefinition(dockSpaceDefinition.Name + "_SplitV_Top", nextID + 1);
                                    UIDockSpaceDefinition bottomPart = new UIDockSpaceDefinition(dockSpaceDefinition.Name + "_SplitV_Bottom", nextID + 2);

                                    dockSpaceDefinition.Children.Add(topPart);
                                    dockSpaceDefinition.Children.Add(bottomPart);
                                }
                                break;
                        }

                        //Dockspace list has changed, refresh other UI component
                        DockingLayoutManager.RefreshDockSpaces();
                    }, (int)dockSpaceDefinition.Orientation);

                    // REcursive display for children
                    foreach (UIDockSpaceDefinition child in dockSpaceDefinition.Children)
                    {
                        layout.Separator();
                        ShowTreeView(layout, child);
                    }
                }

                ImGui.TreePop();
            }
        }

        public static void WindowsDefinitionManager(UIWindow window)
        {
            using (UILayout windowsDefinition_layout = new UILayout())
            {
                windowsDefinition_layout.Collapsable("Manage Windows definitions", () =>
                {
                    using (UIGrid windowsDefinition_grid = new UIGrid("windowsDefinition_grid"))
                    {
                        windowsDefinition_grid.Combobox("Windows definition", DockingLayoutManager._fuguiWindows.Values.ToList(), (x) => { DockingLayoutManager._selectedWindowDefinition = x; }, () => { return DockingLayoutManager._selectedWindowDefinition; });
                        windowsDefinition_grid.TextInput("Window name", ref DockingLayoutManager._windowsToAdd);

                        if (!string.IsNullOrEmpty(DockingLayoutManager._windowsToAdd))
                        {
                            using (UILayout buttonLayout = new UILayout())
                            {
                                if (DockingLayoutManager._fuguiWindows.Values.Contains(DockingLayoutManager._windowsToAdd))
                                {
                                    buttonLayout.SmartText(string.Format($"<color=red>The name <b>'{DockingLayoutManager._windowsToAdd}'</b> is already present in the current FuGui windows definition !</color>"));
                                }
                                else
                                {
                                    if (!FuGui.IsAlphaNumericWithSpaces(DockingLayoutManager._windowsToAdd))
                                    {
                                        buttonLayout.SmartText(string.Format($"<color=red>The name <b>'{DockingLayoutManager._windowsToAdd}'</b> is not a valid name for a FuGui window !</color>"));
                                    }
                                    else
                                    {
                                        buttonLayout.Spacing();
                                        if (buttonLayout.Button("Add new FuGui window definition", UIButtonStyle.Success))
                                        {
                                            if (!DockingLayoutManager._fuguiWindows.Values.Contains(DockingLayoutManager._windowsToAdd))
                                            {
                                                int newIndex = DockingLayoutManager._fuguiWindows.Max(x => x.Key) + 1;
                                                DockingLayoutManager._fuguiWindows.Add(DockingLayoutManager._fuguiWindows.Keys.Last() + 1, DockingLayoutManager._windowsToAdd);

                                                Dictionary<int, string> formatedFuGuiWindowsName = new Dictionary<int, string>();

                                                foreach (KeyValuePair<int, string> fuguiItem in DockingLayoutManager._fuguiWindows)
                                                {
                                                    formatedFuGuiWindowsName.Add(fuguiItem.Key, RemoveSpaceAndCapitalize(fuguiItem.Value));
                                                }

                                                DockingLayoutManager.writeToFile(DockingLayoutManager.FUGUI_WINDOWS_DEF_ENUM_PATH, DockingLayoutManager.generateEnum("FuguiWindows", formatedFuGuiWindowsName));
                                                DockingLayoutManager._windowsToAdd = string.Empty;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(DockingLayoutManager._selectedWindowDefinition) && DockingLayoutManager._selectedWindowDefinition != "None")
                        {
                            using (UILayout buttonLayout = new UILayout())
                            {
                                if (buttonLayout.Button(string.Format($"Remove {DockingLayoutManager._selectedWindowDefinition}"), UIButtonStyle.Danger))
                                {
                                    if (DockingLayoutManager._fuguiWindows.Values.Contains(DockingLayoutManager._selectedWindowDefinition))
                                    {
                                        int keyToDelete = -1;

                                        foreach (KeyValuePair<int, string> item in DockingLayoutManager._fuguiWindows)
                                        {
                                            if (item.Value == DockingLayoutManager._selectedWindowDefinition)
                                            {
                                                keyToDelete = item.Key;
                                                break;
                                            }
                                        }

                                        if (keyToDelete != -1)
                                        {
                                            DockingLayoutManager._fuguiWindows.Remove(keyToDelete);
                                            DockingLayoutManager.writeToFile(DockingLayoutManager.FUGUI_WINDOWS_DEF_ENUM_PATH, DockingLayoutManager.generateEnum("FuguiWindows", DockingLayoutManager._fuguiWindows));
                                            DockingLayoutManager._selectedWindowDefinition = "None";
                                        }
                                    }
                                }
                            }
                        }
                    }
                });
            }

            if (DockingLayoutManager.DisplayedLayout != null)
            {
                using (UILayout bindWindowsDef_Layout = new UILayout())
                {
                    bindWindowsDef_Layout.Collapsable("Bind Windows definition to DockSpace", () =>
                    {
                        using (UIGrid tempGrid = new UIGrid("bindWinDefToDockSpace_grid", new UIGridDefinition(2, new int[] { 150 })))
                        {
                            for (int i = 0; i < DockingLayoutManager._fuguiWindows.Count; i++)
                            {
                                KeyValuePair<int, string> item = DockingLayoutManager._fuguiWindows.ElementAt(i);

                                if (item.Value == "None")
                                {
                                    continue;
                                }

                                if (DockingLayoutManager._definedDockSpaces != null)
                                {
                                    tempGrid.Combobox(item.Value, DockingLayoutManager._definedDockSpaces.Values.ToList(), (x) =>
                                    {
                                        if (x != null)
                                        {
                                            if (x == "None")
                                            {
                                                DockingLayoutManager.unbindWindowToDockspace(item.Key);
                                            }
                                            else
                                            {
                                                DockingLayoutManager.bindWindowToDockspace(item.Key, x);
                                            }
                                        }
                                    },
                                    () =>
                                    {
                                        return DockingLayoutManager.getBindedLayout(item.Key);
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