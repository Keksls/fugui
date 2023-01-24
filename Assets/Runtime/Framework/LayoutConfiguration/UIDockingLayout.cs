using Fugui.Core;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fugui.Framework
{
    public partial class FuGui
    {
        /// <summary>
        /// Creates a UI panel that contains the dock space manager and the layout manager
        /// </summary>
        /// <param name="windowDockSpaceDefinition">The UI window for the dock space manager</param>
        public static void DockSpaceManager(UIWindow windowDockSpaceDefinition)
        {
            using (UIPanel scrollablePanel = new UIPanel("scrollablePanel"))
            {
                using (UILayout windowDockSpaceDefinition_layout = new UILayout())
                {
                    ShowTreeView(windowDockSpaceDefinition_layout, DockingLayoutManager._dockSpaceDefinitionRoot);
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
        private static void ShowTreeView(UILayout layout, UIDockSpaceDefinition dockSpaceDefinition)
        {
            if (ImGui.TreeNode("DockSpace : " + dockSpaceDefinition.ID))
            {
                using (UIGrid gridInfo = new UIGrid("gridInfo" + dockSpaceDefinition.ID, UIGridDefinition.DefaultAuto))
                {
                    gridInfo.TextInput("Name", ref dockSpaceDefinition.Name);
                    string tempID = dockSpaceDefinition.ID.ToString();
                    gridInfo.TextInput("Dockspace Id", ref tempID, UIFrameStyle.Default);
                    gridInfo.Slider("Proportion" + dockSpaceDefinition.ID, ref dockSpaceDefinition.Proportion, 0f, 1f);
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
                                    int nextID = DockingLayoutManager._dockSpaceDefinitionRoot.GetTotalChildren();

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
                                    int nextID = DockingLayoutManager._dockSpaceDefinitionRoot.GetTotalChildren();

                                    UIDockSpaceDefinition topPart = new UIDockSpaceDefinition(dockSpaceDefinition.Name + "_SplitV_Top", nextID + 1);
                                    UIDockSpaceDefinition bottomPart = new UIDockSpaceDefinition(dockSpaceDefinition.Name + "_SplitV_Bottom", nextID + 2);

                                    dockSpaceDefinition.Children.Add(topPart);
                                    dockSpaceDefinition.Children.Add(bottomPart);
                                }
                                break;
                        }
                    });

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

        // TODO : some of inner logic must be func/method into DockingLayoutManager
        public static void WindowsDefinitionManager(UIWindow window)
        {
            using (UILayout windowsDefinition_layout = new UILayout())
            {
                windowsDefinition_layout.Collapsable("Manage Windows definitions", () =>
                {
                    using (UIGrid windowsDefinition_grid = new UIGrid("windowsDefinition_grid"))
                    {
                        windowsDefinition_grid.Combobox("Windows definition :", DockingLayoutManager._fuguiWindows.Values.ToList(), (x) => { DockingLayoutManager._selectedValue = x; });
                        windowsDefinition_grid.TextInput("Window name :", ref DockingLayoutManager._windowsToAdd);

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
                                        if (buttonLayout.Button("Add new FuGui window definition", UIButtonStyle.FullSize, UIButtonStyle.Success))
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

                                                DockingLayoutManager.writeToFile(DockingLayoutManager.FUGUI_WINDOWS_DEFINTION_ENUM_PATH, DockingLayoutManager.generateEnum("FuGuiWindows", formatedFuGuiWindowsName));
                                                DockingLayoutManager._windowsToAdd = string.Empty;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(DockingLayoutManager._selectedValue) && DockingLayoutManager._selectedValue != "None")
                        {
                            using(UILayout buttonLayout = new UILayout())
                            {
                                if (buttonLayout.Button(string.Format($"Remove {DockingLayoutManager._selectedValue}"), UIButtonStyle.FullSize, UIButtonStyle.Danger))
                                {
                                    if (DockingLayoutManager._fuguiWindows.Values.Contains(DockingLayoutManager._selectedValue))
                                    {
                                        int keyToDelete = -1;

                                        foreach (KeyValuePair<int, string> item in DockingLayoutManager._fuguiWindows)
                                        {
                                            if (item.Value == DockingLayoutManager._selectedValue)
                                            {
                                                keyToDelete = item.Key;
                                                break;
                                            }
                                        }

                                        if (keyToDelete != -1)
                                        {
                                            DockingLayoutManager._fuguiWindows.Remove(keyToDelete);
                                            DockingLayoutManager.writeToFile(DockingLayoutManager.FUGUI_WINDOWS_DEFINTION_ENUM_PATH, DockingLayoutManager.generateEnum("FuGuiWindows", DockingLayoutManager._fuguiWindows));
                                            DockingLayoutManager._selectedValue = "None";
                                        }
                                    }
                                }
                            }
                        }
                    }
                });                
            }

            using (UILayout bindWindowsDef_Layout = new UILayout())
            {
                bindWindowsDef_Layout.Collapsable("Bind Windows definition to DockSpace", () =>
                {
                    using (UIGrid tempGrid = new UIGrid("bindWinDefToDockSpaceGrid", new UIGridDefinition(2, new int[] { 150 })))
                    {
                        for (int i = 0; i < DockingLayoutManager._dockSpacesToWindow.Count; i++)
                        {
                            KeyValuePair<string, string> item = DockingLayoutManager._dockSpacesToWindow.ElementAt(i);
                            tempGrid.Combobox(item.Key, DockingLayoutManager._dockSpaces.Values.ToList(), (x) => { DockingLayoutManager._dockSpacesToWindow[item.Key] = x; });
                        }
                    };
                });
            }
        }
    }
}