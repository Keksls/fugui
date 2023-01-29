using ImGuiNET;
using System;
using System.Collections.Generic;

namespace Fu.Framework
{
    public partial class FuGrid
    {
        /// <summary>
        /// Draw a listbox using a list of IListboxItem
        /// </summary>
        /// <param name="text">Label/ID of the listbox</param>
        /// <param name="items">List of items of the listbox</param>
        /// <param name="itemChange">event raised on item change. When raised, param (int) is ID of new selected item in items list</param>
        /// <param name="itemGetter">A func that return a way to get current stored value for the listbox. can be null if listbox il not lined to an object's field</param>
        /// <param name="style">Listbox style to apply</param>
        protected override void _customListbox(string text, List<IListboxItem> items, Action<int> itemChange, Func<string> itemGetter, FuListboxStyle style)
        {
            if (!_gridCreated)
            {
                return;
            }
            drawElementLabel(text, style.ButtonStyle.TextStyle);
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().x);
            base._customListbox("##" + text, items, itemChange, itemGetter, style);
        }

        /// <summary>
        /// Draw a listbox using fully custom UI
        /// </summary>
        /// <param name="text">Label/ID of the listbox</param>
        /// <param name="selectedItemText">text displayed on listbox</param>
        /// <param name="callback">custom UI to draw when listbox is open</param>
        /// <param name="style">Listbox style to apply</param>
        /// <param name="height">Height of the open UI</param>
        public override void Listbox(string text, string selectedItemText, Action callback, FuListboxStyle style, int height)
        {
            if (!_gridCreated)
            {
                return;
            }
            drawElementLabel(text, style.ButtonStyle.TextStyle);
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().x);
            base.Listbox("##" + text, selectedItemText, callback, style, height);
        }
    }
}