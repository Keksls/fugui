using ImGuiNET;
using System;
using UnityEngine;

namespace Fu.Framework
{
    public partial class FuGrid
    {
        /// <summary>
        /// Displays a combobox that allows the user to choose from a list of predefined items. 
        /// When an item is selected, the specified callback function is called.
        /// </summary>
        /// <param name="text">The label displayed next to the combobox</param>
        /// <param name="selectedItemText">The currently selected item</param>
        /// <param name="callback">custom UI to draw when Combobox is open</param>
        /// <param name="size">The size of the Combobox button</param>
        /// <param name="popupSize">The size of  the combobox Popup</param>
        /// <param name="style">The style of the combobox</param>
        /// <param name="popupPosition">Position of  the combobox Popup</param>
        public override void Combobox(string text, string selectedItemText, Action callback, FuElementSize size, Vector2 popupSize, FuButtonStyle style, FuComboboxPopupPosition popupPosition = FuComboboxPopupPosition.BottomLeftAlign)
        {
            if (!_gridCreated)
            {
                return;
            }
            drawElementLabel(text, style.TextStyle);
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().x);
            base.Combobox("##" + text, selectedItemText, callback, size, popupSize, style, popupPosition);
        }
    }
}