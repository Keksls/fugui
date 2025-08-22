using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework
{
    public partial class FuLayout
    {
        /// <summary>
        /// Draw a tabBar
        /// </summary>
        /// <param name="items">items to draw</param>
        /// <param name="callback">Callback of the UI to draw, the param is the index of the value of the selected item</param>
        /// <param name="forceSelectTabIndex">Force select a tab index (use -1 to not force any)</param>
        public void Tabs(string ID, IEnumerable<string> items, Action<int> callback, int forceSelectTabIndex = -1)
        {
            // draw tab bg color
            Vector2 pos = ImGui.GetCursorScreenPos();
            Vector2 size = ImGui.GetContentRegionAvail();
            size.y = ImGui.CalcTextSize("Ap").y + FuThemeManager.FramePadding.y;
            ImGui.GetWindowDrawList().AddRectFilled(pos, pos + size, ImGui.GetColorU32(ImGuiCol.TitleBg));

            // draw the tab bar
            if (ImGui.BeginTabBar(ID))
            {
                // draw the tab items
                int index = 0;
                foreach (string item in items)
                {
                    if (forceSelectTabIndex == index)
                    {
                        bool open = true;
                        if (ImGui.BeginTabItem(item, ref open, ImGuiTabItemFlags.SetSelected))
                        {
                            callback?.Invoke(index);
                            ImGui.EndTabItem();
                        }
                    }
                    else
                    {
                        if (ImGui.BeginTabItem(item))
                        {
                            callback?.Invoke(index);
                            ImGui.EndTabItem();
                        }
                    }
                    index++;
                }
                ImGui.EndTabBar();
            }
        }
    }
}