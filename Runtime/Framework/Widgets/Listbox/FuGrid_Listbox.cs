using ImGuiNET;
using System;

namespace Fu.Framework
{
    public partial class FuGrid
    {
        /// <summary>
        /// Draw a listbox using fully custom UI
        /// </summary>
        /// <param name="text">Label/ID of the listbox</param>
        /// <param name="selectedItemText">text displayed on listbox</param>
        /// <param name="callback">custom UI to draw when listbox is open</param>
        /// <param name="size">Size of the open UI</param>
        public override void ListBox(string text, Action callback, FuElementSize size)
        {
            if (!_gridCreated)
            {
                return;
            }
            drawElementLabel(text, FuTextStyle.Default);
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().x);
            base.ListBox("##" + text, callback, size);
        }
    }
}