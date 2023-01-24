using Fugui.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fugui.Framework
{
    public static partial class FuGui
    {
        public static void DockSpaceManager(UIWindow window)
        {
            using (new UIPanel("mainPanel"))
            {
                using (UIGrid grid = new UIGrid("mainGrid"))
                {

                }
            }
        }

        // TODO : some of inner logic must be func/method into DockingLayoutManager
        public static void WindowsDefinitionManager(UIWindow window)
        {
            using (UIGrid grid = new UIGrid("mainGrid"))
            {
                grid.Combobox("Windows definition :", DockingLayoutManager._fuguiWindows.Values.ToList(), (x) => { DockingLayoutManager._selectedValue = x; });
                grid.Spacing();
                grid.TextInput("Window name :", ref DockingLayoutManager._windowsToAdd);

                using (UILayout layout = new UILayout())
                {
                    if (!string.IsNullOrEmpty(DockingLayoutManager._windowsToAdd))
                    {
                        if (DockingLayoutManager._fuguiWindows.Values.Contains(DockingLayoutManager._windowsToAdd))
                        {
                            layout.SmartText(string.Format($"<color=red>The name <b>'{DockingLayoutManager._windowsToAdd}'</b> is already present in the current FuGui windows definition !</color>"));
                        }
                        else
                        {
                            if (!FuGui.IsAlphaNumericWithSpaces(DockingLayoutManager._windowsToAdd))
                            {
                                layout.SmartText(string.Format($"<color=red>The name <b>'{DockingLayoutManager._windowsToAdd}'</b> is not a valid name for a FuGui window !</color>"));
                            }
                            else
                            {
                                layout.Spacing();
                                if (layout.Button("Add new FuGui window definition", UIButtonStyle.FullSize, UIButtonStyle.Default))
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

                    if (!string.IsNullOrEmpty(DockingLayoutManager._selectedValue) && DockingLayoutManager._selectedValue != "None")
                    {
                        if (layout.Button(string.Format($"Remove {DockingLayoutManager._selectedValue}", UIButtonStyle.FullSize, UIButtonStyle.Default)))
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
        }

    }
}