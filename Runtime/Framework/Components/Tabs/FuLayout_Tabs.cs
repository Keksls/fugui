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
        /// <param name="callback">Callback of the UI to draw, the param is the value of the selected item</param>
        public void Tabs(string ID, IEnumerable<string> items, Action<string> callback)
        {
            // draw tab bg color
            Vector2 pos = ImGui.GetCursorScreenPos();
            Vector2 size = ImGui.GetContentRegionAvail();
            size.y = ImGui.CalcTextSize("Ap").y + (FuThemeManager.CurrentTheme.FramePadding.y * Fugui.CurrentContext.Scale * 2f);
            ImGui.GetWindowDrawList().AddRectFilled(pos, pos + size, ImGui.GetColorU32(ImGuiCol.TitleBg));

            // draw the tab bar
            if (ImGui.BeginTabBar(ID))
            {
                // draw the tab items
                foreach (string item in items)
                {
                    if (ImGui.BeginTabItem(item))
                    {
                        callback?.Invoke(item);
                        ImGui.EndTabItem();
                    }
                }
                ImGui.EndTabBar();
            }
        }
    }
}