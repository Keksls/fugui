using Fugui.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fugui.Framework
{
    public partial class FuGui
    {
        public static void DockSpaceManager(UIWindow window)
        {
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