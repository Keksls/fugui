using Fugui.Core;
using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fugui.Framework
{
    public partial class UILayout
    {
        #region Generic Types List
        public void Listbox<T>(string text, List<T> items)
        {
            Listbox<T>(text, items, null, null, UIListboxStyle.Default);
        }

        public void Listbox<T>(string text, List<T> items, Action<T> itemChange, int height, Func<T> itemGetter = null)
        {
            Listbox<T>(text, items, itemChange, itemGetter, UIListboxStyle.Default);
        }

        public void Listbox<T>(string text, List<T> items, Action<T> itemChange, Func<T> itemGetter = null)
        {
            Listbox<T>(text, items, itemChange, itemGetter, UIListboxStyle.Default);
        }

        public void Listbox<T>(string text, List<T> items, Action<T> itemChange, Func<T> itemGetter, UIListboxStyle style)
        {
            List<IListboxItem> cItems = new List<IListboxItem>();
            foreach (T item in items)
            {
                if (item is IListboxItem)
                {
                    cItems.Add((IListboxItem)item);
                }
                else
                {
                    cItems.Add(new ListboxTextItem(FuGui.AddSpacesBeforeUppercase(item.ToString()), true));
                }
            }

            _customListbox(text, cItems, (index) =>
            {
                itemChange?.Invoke(items[index]);
            }, () => { return itemGetter?.Invoke()?.ToString(); }, style);
        }
        #endregion

        #region IListboxItems
        protected virtual void _customListbox(string text, List<IListboxItem> items, Action<int> itemChange, Func<string> itemGetter, UIListboxStyle style)
        {
            text = beginElement(text, style);

            if (!_listboxSelectedItem.ContainsKey(text))
            {
                _listboxSelectedItem.Add(text, 0);
            }

            // Set current item as setted by getter
            if (itemGetter != null)
            {
                int i = 0;
                string selectedItemString = itemGetter.Invoke();
                if (!string.IsNullOrEmpty(selectedItemString))
                {
                    selectedItemString = FuGui.AddSpacesBeforeUppercase(selectedItemString);
                    foreach (var item in items)
                    {
                        if (item.ToString() == selectedItemString)
                        {
                            _listboxSelectedItem[text] = i;
                            break;
                        }
                        i++;
                    }
                }
            }

            int selectedIndex = _listboxSelectedItem[text];

            if (selectedIndex >= items.Count)
            {
                selectedIndex = items.Count - 1;
            }

            FuGui.Push(ImGuiStyleVar.FramePadding, new Vector2(8f, 2f));
            FuGui.Push(ImGuiStyleVar.WindowPadding, new Vector2(8f, 8f));
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().x);

            if (ImGui.BeginListBox("##" + text))
            {
                // Pop the style to use the default style for the listbox
                FuGui.PopStyle();
                IsInsidePopUp = true;
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].DrawItem(i == selectedIndex) && items[i].Enabled)
                    {
                        // Update the selected index and perform the item change action
                        selectedIndex = i;
                        _listboxSelectedItem[text] = selectedIndex;
                        itemChange?.Invoke(i);
                    }
                }
                IsInsidePopUp = false;

                // Update the current pop-up window and ID
                if (CurrentPopUpID != text)
                {
                    CurrentPopUpWindowID = UIWindow.CurrentDrawingWindow?.ID;
                    CurrentPopUpID = text;
                }

                // Set CurrentPopUpRect to ImGui item rect
                CurrentPopUpRect = new Rect(ImGui.GetWindowPos(), ImGui.GetWindowSize());
                ImGui.EndListBox();
            }
            else
            {
                // Pop the style to use the default style for the combo dropdown
                FuGui.PopStyle();
                if (CurrentPopUpID == text)
                {
                    CurrentPopUpWindowID = null;
                    CurrentPopUpID = null;
                }
            }
            FuGui.PopStyle();
            displayToolTip();
            endElement(style);
        }
        #endregion
    }
}