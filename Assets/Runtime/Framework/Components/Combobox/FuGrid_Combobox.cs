using ImGuiNET;
using System;
using System.Collections.Generic;

namespace Fu.Framework
{
    public partial class FuGrid
    {
        /// <summary>
        /// Draw a combobox using a list of IComboboxItem
        /// </summary>
        /// <param name="text">Label/ID of the combobox</param>
        /// <param name="items">List of items of the combobox</param>
        /// <param name="itemChange">event raised on item change. When raised, param (int) is ID of new selected item in items list</param>
        /// <param name="itemGetter">A func that return a way to get current stored value for the combobox. can be null if combobox il not lined to an object's field</param>
        /// <param name="style">Combobox style to apply</param>
        protected override void _customCombobox(string text, List<IFuSelectable> items, Action<int> itemChange, Func<string> itemGetter, FuComboboxStyle style)
        {
            if (!_gridCreated)
            {
                return;
            }
            drawElementLabel(text, style.ButtonStyle.TextStyle);
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().x);
            base._customCombobox("##" + text, items, itemChange, itemGetter, style);
        }

        /// <summary>
        /// Draw a combobox using fully custom UI
        /// </summary>
        /// <param name="text">Label/ID of the combobox</param>
        /// <param name="selectedItemText">text displayed on combobox</param>
        /// <param name="callback">custom UI to draw when Combobox is open</param>
        /// <param name="style">Combobox style to apply</param>
        /// <param name="height">Height of the open UI</param>
        public override void Combobox(string text, string selectedItemText, Action callback, FuComboboxStyle style, int height)
        {
            if (!_gridCreated)
            {
                return;
            }
            drawElementLabel(text, style.ButtonStyle.TextStyle);
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().x);
            base.Combobox("##" + text, selectedItemText, callback, style, height);
        }
    }
}