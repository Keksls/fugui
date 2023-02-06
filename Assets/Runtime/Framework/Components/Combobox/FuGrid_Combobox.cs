using ImGuiNET;
using System;
using System.Collections.Generic;

namespace Fu.Framework
{
    public partial class FuGrid
    {
        /// <summary>
        /// Draw a combobox using fully custom UI
        /// </summary>
        /// <param name="text">Label/ID of the combobox</param>
        /// <param name="selectedItemText">text displayed on combobox</param>
        /// <param name="callback">custom UI to draw when Combobox is open</param>
        /// <param name="style">Combobox style to apply</param>
        public override void Combobox(string text, string selectedItemText, Action callback, FuComboboxStyle style)
        {
            if (!_gridCreated)
            {
                return;
            }
            drawElementLabel(text, style.ButtonStyle.TextStyle);
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().x);
            base.Combobox("##" + text, selectedItemText, callback, style);
        }
    }
}