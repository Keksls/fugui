using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework
{
    public partial class FuLayout
    {
        public const float COMBOBOX_POPUP_MAXIMUM_HEIGHT = 320f;

        #region Enum Types List
        /// <summary>
        /// Displays a combobox with all the enum values of type TEnum. The selected item can be changed by the user, and the change will be reported through the itemChange action.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum that will be displayed in the combobox. It must be an enumerated type.</typeparam>
        /// <param name="text">The label text to be displayed next to the combobox</param>
        /// <param name="itemChange">The action that will be called when the selected item changes</param>
        /// <param name="itemGetter">A func that return a way to get current stored value for the combobox. can be null if combobox il not linked to an object's field</param>
        public void ComboboxEnum<TEnum>(string text, Action<int> itemChange, Func<TEnum> itemGetter = null, FuComboboxPopupPosition popupPosition = FuComboboxPopupPosition.BottomLeftAlign) where TEnum : struct, IConvertible
        {
            ComboboxEnum<TEnum>(text, itemChange, itemGetter, FuElementSize.FullSize, Vector2.zero, FuButtonStyle.Default, popupPosition);
        }

        /// <summary>
        /// Displays a combobox with all the enum values of type TEnum. The selected item can be changed by the user, and the change will be reported through the itemChange action.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum that will be displayed in the combobox. It must be an enumerated type.</typeparam>
        /// <param name="text">The label text to be displayed next to the combobox</param>
        /// <param name="itemChange">The action that will be called when the selected item changes</param>
        /// <param name="itemGetter">A func that return a way to get current stored value for the combobox. can be null if combobox il not linked to an object's field</param>
        /// <param name="style">The style to be applied to the combobox</param>
        public void ComboboxEnum<TEnum>(string text, Action<int> itemChange, Func<TEnum> itemGetter, FuElementSize size, Vector2 popupSize, FuButtonStyle style, FuComboboxPopupPosition popupPosition = FuComboboxPopupPosition.BottomLeftAlign) where TEnum : struct, IConvertible
        {
            FuSelectableBuilder.BuildFromEnum<TEnum>(out List<int> enumValues, out List<string> enumSelectables);
            // call the custom combobox function, passing in the lists and the itemChange
            _customCombobox(text, enumSelectables, (index) =>
            {
                itemChange?.Invoke(enumValues[index]);
            }, () => { return itemGetter?.Invoke().ToString(); }, size, popupSize, style, popupPosition);
        }
        #endregion

        #region Generic Types List
        /// <summary>
        /// Displays a dropdown box with a list of items of type T and calls the specified action with the selected item when changed.
        /// </summary>
        /// <typeparam name="T">The type of the items in the list.</typeparam>
        /// <param name="text">The label of the dropdown box.</param>
        /// <param name="items">The list of items to display in the dropdown box.</param>
        /// <param name="itemChange">The action to call when the selected item changes.</param>
        /// <param name="itemGetter">A func that return a way to get current stored value for the combobox. can be null if combobox il not linked to an object's field</param>
        /// <param name="listUpdated">whatever the list has been updated since last call (list or values inside. it's for performances on large. You can handle it using ObservableCollections)
        /// If you keep it as null, values will be reprocess each frames (better accuratie, but can lead on slowing down on large lists)</param>
        public void Combobox<T>(string text, List<T> items, Action<int> itemChange, Func<T> itemGetter = null, Func<bool> listUpdated = null)
        {
            Combobox<T>(text, items, itemChange, itemGetter, FuElementSize.FullSize, Vector2.zero, FuButtonStyle.Default, FuComboboxPopupPosition.BottomLeftAlign, listUpdated);
        }

        /// <summary>
        /// Displays a dropdown box with a list of items of type T and calls the specified action with the selected item when changed.
        /// </summary>
        /// <typeparam name="T">The type of the items in the list.</typeparam>
        /// <param name="text">The label of the dropdown box.</param>
        /// <param name="items">The list of items to display in the dropdown box.</param>
        /// <param name="itemChange">The action to call when the selected item changes.</param>
        /// <param name="itemGetter">A func that return a way to get current stored value for the combobox. can be null if combobox il not linked to an object's field</param>
        /// <param name="style">The style to use for the dropdown box.</param>
        /// <param name="listUpdated">whatever the list has been updated since last call (list or values inside. it's for performances on large. You can handle it using ObservableCollections)
        /// If you keep it as null, values will be reprocess each frames (better accuratie, but can lead on slowing down on large lists)</param>
        public void Combobox<T>(string text, List<T> items, Action<int> itemChange, Func<T> itemGetter, FuElementSize size, Vector2 popupSize, FuButtonStyle style, FuComboboxPopupPosition popupPosition = FuComboboxPopupPosition.BottomLeftAlign, Func<bool> listUpdated = null)
        {
            // Display the custom combobox and call the specified action when the selected item changes
            _customCombobox(text, items, itemChange, () => { return itemGetter?.Invoke()?.ToString(); }, size, popupSize, style, popupPosition);
        }
        #endregion

        #region IComboboxItems
        ///<summary>
        /// Renders a combobox with a list of custom items.
        ///</summary>
        ///<param name="text">The label for the combobox.</param>
        ///<param name="items">The list of custom items to be displayed in the combobox.</param>
        ///<param name="itemChange">The action to be performed when an item is selected.</param>
        /// <param name="itemGetter">A func that return a way to get current stored value for the combobox. can be null if combobox il not linked to an object's field</param>
        ///<param name="style">The style for the combobox element.</param>
        private void _customCombobox<T>(string text, List<T> items, Action<int> itemChange, Func<string> itemGetter, FuElementSize size, Vector2 popupSize, FuButtonStyle style, FuComboboxPopupPosition popupPosition)
        {
            // return if item must no be draw
            if (!_drawElement)
            {
                return;
            }

            // get the current selected index
            int selectedIndex = FuSelectableBuilder.GetSelectedIndex(text, items, itemGetter);

            // draw the combobox
            Combobox(text, items.Count > 0 ? items[selectedIndex].ToString() : "No Items", () =>
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (ImGui.Selectable(Fugui.AddSpacesBeforeUppercase(items[i].ToString()), selectedIndex == i, LastItemDisabled ? ImGuiSelectableFlags.Disabled : ImGuiSelectableFlags.None))
                    {
                        // Update the selected index and invoke the item change action
                        selectedIndex = i;
                        FuSelectableBuilder.SetSelectedIndex(text, selectedIndex);
                        itemChange?.Invoke(i);
                    }
                }
            }, size, popupSize, style, popupPosition);
        }
        #endregion

        #region Fully custom combobox content
        /// <summary>
        /// Displays a combobox that allows the user to choose from a list of predefined items. 
        /// When an item is selected, the specified callback function is called.
        /// </summary>
        /// <param name="text">The label displayed next to the combobox</param>
        /// <param name="selectedItemText">The currently selected item</param>
        /// <param name="callback">custom UI to draw when Combobox is open</param>
        public void Combobox(string text, string selectedItemText, Action callback)
        {
            Combobox(text, selectedItemText, callback, FuElementSize.FullSize, Vector2.zero, FuButtonStyle.Default);
        }

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
        public virtual void Combobox(string text, string selectedItemText, Action callback, FuElementSize size, Vector2 popupSize, FuButtonStyle style, FuComboboxPopupPosition popupPosition = FuComboboxPopupPosition.BottomLeftAlign)
        {
            beginElement(ref text, style);
            // return if item must no be draw
            if (!_drawElement)
            {
                return;
            }

            // draw combobox button
            string popupID = text + "pu";
            float carretWidth = 16f * Fugui.CurrentContext.Scale;
            if (_customButton(selectedItemText + "##" + text, size.BrutSize, FuThemeManager.FramePadding, Vector2.zero, style, FuThemeManager.CurrentTheme.ButtonsGradientStrenght, true, 0f, carretWidth))
            {
                Fugui.OpenPopUp(popupID, () =>
                {
                    Spacing();
                    Spacing();
                    SameLine();
                    BeginGroup();
                    callback?.Invoke();
                    EndGroup();
                    SameLine();
                    Spacing();
                    Spacing();
                },
                isComboBoxPopup: true);
            }
            // get popup open state
            bool opened = Fugui.IsPopupOpen(popupID);

            // get button rect info
            Vector2 btnMin = ImGui.GetItemRectMin();
            Vector2 btnMax = ImGui.GetItemRectMax();
            Vector2 btnSize = btnMax - btnMin;

            // draw carret
            if (opened)
            {
                Fugui.DrawCarret_Top(ImGui.GetWindowDrawList(), btnMax - new Vector2(carretWidth, btnSize.y), carretWidth / 3f, btnSize.y, LastItemDisabled ? style.TextStyle.DisabledText : style.TextStyle.Text);
            }
            else
            {
                Fugui.DrawCarret_Down(ImGui.GetWindowDrawList(), btnMax - new Vector2(carretWidth, btnSize.y), carretWidth / 3f, btnSize.y, LastItemDisabled ? style.TextStyle.DisabledText : style.TextStyle.Text);
            }
            // End the element with the current combobox style
            endElement(style);

            // draw the popup
            if (opened)
            {
                // calculate popup transform
                Vector2 pos = default;
                // help popup size
                if (popupSize.x == 0f)
                {
                    popupSize.x = btnSize.x;
                }
                else if (popupSize.x == -1f)
                {
                    popupSize.x = 0f;
                }
                if (popupSize.y <= 0f)
                {
                    popupSize.y = -1f;
                }
                Rect lastFramePopupRect = Fugui.GetPopupLastFrameRect(Fugui.GetUniquePopupID(popupID));
                // calculate position
                switch (popupPosition)
                {
                    // Bottom Left
                    default:
                    case FuComboboxPopupPosition.BottomLeftAlign:
                        pos = new Vector2(btnMin.x, btnMax.y + 2f);
                        break;
                    // Bottom Right
                    case FuComboboxPopupPosition.BottomRightAlign:
                        pos = new Vector2(btnMin.x - (popupSize.x - btnSize.x), btnMax.y + 2f);
                        break;

                    // Top Left
                    case FuComboboxPopupPosition.TopLeftAlign:
                        pos = new Vector2(btnMin.x, btnMin.y - lastFramePopupRect.size.y - 2f);
                        break;

                    // Bottom Right
                    case FuComboboxPopupPosition.TopRightAlign:
                        pos = new Vector2(btnMin.x - (popupSize.x - btnSize.x), btnMin.y - lastFramePopupRect.size.y - 2f);
                        break;
                }

                // clamp height of the popup
                if (popupSize.y == -1f && lastFramePopupRect.size.y >= COMBOBOX_POPUP_MAXIMUM_HEIGHT)
                {
                    popupSize.y = COMBOBOX_POPUP_MAXIMUM_HEIGHT;
                }
                // draw the popup
                Fugui.DrawPopup(popupID, popupSize, pos);
            }
        }

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
        private void _internalCombobox(string text, string selectedItemText, Action callback, FuElementSize size, Vector2 popupSize, FuButtonStyle style, FuComboboxPopupPosition popupPosition = FuComboboxPopupPosition.BottomLeftAlign)
        {
            // draw combobox button
            string popupID = text + "pu";
            float carretWidth = 16f * Fugui.CurrentContext.Scale;
            if (_customButton(selectedItemText + "##" + text, size.BrutSize, FuThemeManager.FramePadding, Vector2.zero, style, FuThemeManager.CurrentTheme.ButtonsGradientStrenght, true, 0f, carretWidth))
            {
                Fugui.OpenPopUp(popupID, () =>
                {
                    Spacing();
                    Spacing();
                    SameLine();
                    BeginGroup();
                    callback?.Invoke();
                    EndGroup();
                    SameLine();
                    Spacing();
                    Spacing();
                },
                isComboBoxPopup: true);
            }
            // get popup open state
            bool opened = Fugui.IsPopupOpen(popupID);

            // get button rect info
            Vector2 btnMin = ImGui.GetItemRectMin();
            Vector2 btnMax = ImGui.GetItemRectMax();
            Vector2 btnSize = btnMax - btnMin;

            // draw carret
            if (opened)
            {
                Fugui.DrawCarret_Top(ImGui.GetWindowDrawList(), btnMax - new Vector2(carretWidth, btnSize.y), carretWidth / 3f, btnSize.y, LastItemDisabled ? style.TextStyle.DisabledText : style.TextStyle.Text);
            }
            else
            {
                Fugui.DrawCarret_Down(ImGui.GetWindowDrawList(), btnMax - new Vector2(carretWidth, btnSize.y), carretWidth / 3f, btnSize.y, LastItemDisabled ? style.TextStyle.DisabledText : style.TextStyle.Text);
            }

            // draw the popup
            if (opened)
            {
                // calculate popup transform
                Vector2 pos = default;
                // help popup size
                if (popupSize.x == 0f)
                {
                    popupSize.x = btnSize.x;
                }
                if (popupSize.y <= 0f)
                {
                    popupSize.y = -1f;
                }
                Rect lastFramePopupRect = Fugui.GetPopupLastFrameRect(Fugui.GetUniquePopupID(popupID));
                // calculate position
                switch (popupPosition)
                {
                    // Bottom Left
                    default:
                    case FuComboboxPopupPosition.BottomLeftAlign:
                        pos = new Vector2(btnMin.x, btnMax.y + 2f);
                        break;
                    // Bottom Right
                    case FuComboboxPopupPosition.BottomRightAlign:
                        pos = new Vector2(btnMin.x - (popupSize.x - btnSize.x), btnMax.y + 2f);
                        break;

                    // Top Left
                    case FuComboboxPopupPosition.TopLeftAlign:
                        pos = new Vector2(btnMin.x, btnMin.y - lastFramePopupRect.size.y - 2f);
                        break;

                    // Bottom Right
                    case FuComboboxPopupPosition.TopRightAlign:
                        pos = new Vector2(btnMin.x - (popupSize.x - btnSize.x), btnMin.y - lastFramePopupRect.size.y - 2f);
                        break;
                }

                // clamp height of the popup
                if (popupSize.y == -1f && lastFramePopupRect.size.y >= COMBOBOX_POPUP_MAXIMUM_HEIGHT)
                {
                    popupSize.y = COMBOBOX_POPUP_MAXIMUM_HEIGHT;
                }
                // draw the popup
                Fugui.DrawPopup(popupID, popupSize, pos);
            }
        }
        #endregion
    }
}