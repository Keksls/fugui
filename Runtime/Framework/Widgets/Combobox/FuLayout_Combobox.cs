using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework
{
    /// <summary>
    /// Represents the Fu Layout type.
    /// </summary>
    public partial class FuLayout
    {
        #region State
        public const float COMBOBOX_POPUP_MAXIMUM_HEIGHT = 320f;
        private static readonly FuBoundedCache<string, bool> _comboboxPopupCloseOnClickRelease =
            new FuBoundedCache<string, bool>(256);
        private static readonly FuBoundedCache<string, object> _selectablePopupStates =
            new FuBoundedCache<string, object>(1024, StringComparer.Ordinal);
        #endregion

        #region Methods
        /// <summary>
        /// Clears transient combobox interaction state when the Fugui session is reset.
        /// </summary>
        internal static void ResetComboboxState()
        {
            // Popup release state belongs to the current ImGui session.
            _comboboxPopupCloseOnClickRelease.Clear();
            _selectablePopupStates.Clear();
        }

        /// <summary>
        /// Gets reusable typed popup state for a combobox or listbox.
        /// </summary>
        /// <typeparam name="T">Selectable item type.</typeparam>
        /// <param name="stateId">Window-scoped state identifier.</param>
        /// <returns>Reusable popup renderer.</returns>
        private static FuSelectablePopupState<T> GetSelectablePopupState<T>(string stateId)
        {
            // Reusing an identifier with another item type replaces the incompatible bounded entry.
            if (!_selectablePopupStates.TryGetValue(stateId, out object cachedState) ||
                !(cachedState is FuSelectablePopupState<T> state))
            {
                state = new FuSelectablePopupState<T>();
                _selectablePopupStates.Set(stateId, state);
            }

            return state;
        }

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
            string selectedItemString = itemGetter != null ? itemGetter.Invoke().ToString() : null;
            // Map the visual enum index inside the reusable popup state without a capturing delegate.
            _customCombobox(text, enumSelectables, itemChange, selectedItemString, size, popupSize, style, popupPosition, null, enumValues);
        }

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
            // Resolve the current value directly so no wrapper delegate is created for this frame.
            string selectedItemString = null;
            if (itemGetter != null)
            {
                T selectedItem = itemGetter.Invoke();
                selectedItemString = selectedItem is null ? null : selectedItem.ToString();
            }
            _customCombobox(text, items, itemChange, selectedItemString, size, popupSize, style, popupPosition, listUpdated);
        }

        /// <summary>
        /// Renders a combobox with a list of custom items.
        /// </summary>
        ///<param name="text">The label for the combobox.</param>
        ///<param name="items">The list of custom items to be displayed in the combobox.</param>
        ///<param name="itemChange">The action to be performed when an item is selected.</param>
        /// <param name="selectedItemString">Current external selected value, or null.</param>
        ///<param name="style">The style for the combobox element.</param>
        /// <param name="mappedCallbackValues">Optional callback values mapped from visible indices.</param>
        private void _customCombobox<T>(string text, List<T> items, Action<int> itemChange, string selectedItemString, FuElementSize size, Vector2 popupSize, FuButtonStyle style, FuComboboxPopupPosition popupPosition, Func<bool> listUpdated = null, IList<int> mappedCallbackValues = null)
        {
            // return if item must no be draw
            if (!_drawElement)
            {
                return;
            }

            string windowId = FuWindow.CurrentDrawingWindow?.ID;
            string stateId = GetCachedCompositeId(text, "##FuSelectableCombobox_", windowId);
            int selectedIndex = FuSelectableBuilder.GetSelectedIndex(stateId, items, selectedItemString);
            if (items.Count > 0)
            {
                selectedIndex = Mathf.Clamp(selectedIndex, 0, items.Count - 1);
            }

            FuSelectablePopupState<T> state = GetSelectablePopupState<T>(stateId);
            state.Prepare(LastItemDisabled, stateId, items, selectedIndex, itemChange, null, listUpdated, mappedCallbackValues, true);
            Combobox(text, items.Count > 0 ? items[selectedIndex].ToString() : "No Items", state.DrawAction, size, popupSize, style, popupPosition);
        }

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
            string popupID = GetCachedCompositeId(text, "pu");
            float carretWidth = 16f * Fugui.CurrentContext.Scale;
            bool openedBeforeClick = Fugui.IsPopupOpen(popupID);
            bool closePopupThisFrame = false;
            bool clicked = _customButton(GetCachedCompositeId(selectedItemText, "##", text), size.BrutSize, Fugui.Themes.FramePadding, Vector2.zero, style, Fugui.Themes.CurrentTheme.ButtonsGradientStrenght, true, 0f, carretWidth);
            if (openedBeforeClick && LastItemJustActivated)
            {
                Fugui.ClosePopup(popupID);
                _comboboxPopupCloseOnClickRelease.Set(popupID, true);
                closePopupThisFrame = true;
            }
            if (clicked)
            {
                if (openedBeforeClick || _comboboxPopupCloseOnClickRelease.Remove(popupID))
                {
                    Fugui.ClosePopup(popupID);
                    closePopupThisFrame = true;
                }
                else
                {
                    Fugui.OpenPopUp(popupID, () =>
                    {
                        Fugui.MoveY(4f);
                        Fugui.MoveX(6f);
                        BeginGroup();
                        try
                        {
                            callback?.Invoke();
                        }
                        finally
                        {
                            EndGroup();
                        }
                        ImGui.Dummy(new Vector2(0f, 4f * Fugui.CurrentContext.Scale));
                    },
                    isComboBoxPopup: true);
                }
            }
            else if (_comboboxPopupCloseOnClickRelease.TryGetValue(popupID, out _) && Fugui.GetCurrentMouse().IsUp(FuMouseButton.Left))
            {
                _comboboxPopupCloseOnClickRelease.Remove(popupID);
            }
            // get popup open state
            bool opened = !closePopupThisFrame && Fugui.IsPopupOpen(popupID);

            // get button rect info
            Vector2 btnMin = ImGui.GetItemRectMin();
            Vector2 btnMax = ImGui.GetItemRectMax();
            Vector2 btnSize = btnMax - btnMin;

            // draw carret
            float caretSize = carretWidth / 3f;
            Vector2 caretPos = new Vector2(btnMax.x - (carretWidth + caretSize) * 0.5f, btnMin.y);
            DrawComboboxChrome(Fugui.GetCurrentWindowDrawList(), new Rect(btnMin, btnSize), carretWidth, opened, LastItemDisabled);
            if (opened)
            {
                Fugui.DrawCarret_Top(Fugui.GetCurrentWindowDrawList(), caretPos, caretSize, btnSize.y, LastItemDisabled ? style.TextStyle.DisabledText : style.TextStyle.Text);
            }
            else
            {
                Fugui.DrawCarret_Down(Fugui.GetCurrentWindowDrawList(), caretPos, caretSize, btnSize.y, LastItemDisabled ? style.TextStyle.DisabledText : style.TextStyle.Text);
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

                // Predict popup width/height we will need
                float desiredW = (popupSize.x <= 0f) ? btnSize.x : popupSize.x; // 0 or -1 -> fallback to button width
                float predictedH = (popupSize.y > 0f) ? popupSize.y : lastFramePopupRect.size.y;

                // Apply max height cap prediction (same rule as later)
                if (predictedH >= COMBOBOX_POPUP_MAXIMUM_HEIGHT)
                    predictedH = COMBOBOX_POPUP_MAXIMUM_HEIGHT;

                // Viewport working area
                var vp = ImGui.GetMainViewport();
                Vector2 vpMin = vp.WorkPos;
                Vector2 vpMax = vp.WorkPos + vp.WorkSize;

                // Vertical free space
                float spaceBelow = vpMax.y - (btnMax.y + 2f);
                float spaceAbove = (btnMin.y - 2f) - vpMin.y;

                // If we target bottom but there's not enough room (and above is better), flip to top
                if (popupPosition == FuComboboxPopupPosition.BottomLeftAlign || popupPosition == FuComboboxPopupPosition.BottomRightAlign)
                {
                    if (predictedH > spaceBelow && spaceAbove >= spaceBelow)
                        popupPosition = (popupPosition == FuComboboxPopupPosition.BottomLeftAlign)
                            ? FuComboboxPopupPosition.TopLeftAlign
                            : FuComboboxPopupPosition.TopRightAlign;
                }
                // If we target top but there's not enough room (and below is better), flip to bottom
                else
                {
                    if (predictedH > spaceAbove && spaceBelow > spaceAbove)
                        popupPosition = (popupPosition == FuComboboxPopupPosition.TopLeftAlign)
                            ? FuComboboxPopupPosition.BottomLeftAlign
                            : FuComboboxPopupPosition.BottomRightAlign;
                }

                // Horizontal alignment sanity: if left-aligned would overflow to the right, switch to right-aligned.
                // If right-aligned would overflow to the left, switch to left-aligned.
                float leftAlignedX = btnMin.x;
                float rightAlignedX = btnMin.x - (desiredW - btnSize.x);

                bool leftWouldOverflowRight = (leftAlignedX + desiredW) > vpMax.x;
                bool rightWouldOverflowLeft = rightAlignedX < vpMin.x;

                if ((popupPosition == FuComboboxPopupPosition.BottomLeftAlign || popupPosition == FuComboboxPopupPosition.TopLeftAlign) && leftWouldOverflowRight)
                {
                    popupPosition = (popupPosition == FuComboboxPopupPosition.BottomLeftAlign)
                        ? FuComboboxPopupPosition.BottomRightAlign
                        : FuComboboxPopupPosition.TopRightAlign;
                }
                else if ((popupPosition == FuComboboxPopupPosition.BottomRightAlign || popupPosition == FuComboboxPopupPosition.TopRightAlign) && rightWouldOverflowLeft)
                {
                    popupPosition = (popupPosition == FuComboboxPopupPosition.BottomRightAlign)
                        ? FuComboboxPopupPosition.BottomLeftAlign
                        : FuComboboxPopupPosition.TopLeftAlign;
                }

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
            string popupID = GetCachedCompositeId(text, "pu");
            float carretWidth = 16f * Fugui.CurrentContext.Scale;
            bool openedBeforeClick = Fugui.IsPopupOpen(popupID);
            bool closePopupThisFrame = false;
            bool clicked = _customButton(GetCachedCompositeId(selectedItemText, "##", text), size.BrutSize, Fugui.Themes.FramePadding, Vector2.zero, style, Fugui.Themes.CurrentTheme.ButtonsGradientStrenght, true, 0f, carretWidth);
            if (openedBeforeClick && LastItemJustActivated)
            {
                Fugui.ClosePopup(popupID);
                _comboboxPopupCloseOnClickRelease.Set(popupID, true);
                closePopupThisFrame = true;
            }
            if (clicked)
            {
                if (openedBeforeClick || _comboboxPopupCloseOnClickRelease.Remove(popupID))
                {
                    Fugui.ClosePopup(popupID);
                    closePopupThisFrame = true;
                }
                else
                {
                    Fugui.OpenPopUp(popupID, () =>
                    {
                        Fugui.MoveY(4f);
                        Fugui.MoveX(6f);
                        BeginGroup();
                        try
                        {
                            callback?.Invoke();
                        }
                        finally
                        {
                            EndGroup();
                        }
                        ImGui.Dummy(new Vector2(0f, 4f * Fugui.CurrentContext.Scale));
                    },
                    isComboBoxPopup: true);
                }
            }
            else if (_comboboxPopupCloseOnClickRelease.TryGetValue(popupID, out _) && Fugui.GetCurrentMouse().IsUp(FuMouseButton.Left))
            {
                _comboboxPopupCloseOnClickRelease.Remove(popupID);
            }
            // get popup open state
            bool opened = !closePopupThisFrame && Fugui.IsPopupOpen(popupID);

            // get button rect info
            Vector2 btnMin = ImGui.GetItemRectMin();
            Vector2 btnMax = ImGui.GetItemRectMax();
            Vector2 btnSize = btnMax - btnMin;

            // draw carret
            float caretSize = carretWidth / 3f;
            Vector2 caretPos = new Vector2(btnMax.x - (carretWidth + caretSize) * 0.5f, btnMin.y);
            DrawComboboxChrome(Fugui.GetCurrentWindowDrawList(), new Rect(btnMin, btnSize), carretWidth, opened, LastItemDisabled);
            if (opened)
            {
                Fugui.DrawCarret_Top(Fugui.GetCurrentWindowDrawList(), caretPos, caretSize, btnSize.y, LastItemDisabled ? style.TextStyle.DisabledText : style.TextStyle.Text);
            }
            else
            {
                Fugui.DrawCarret_Down(Fugui.GetCurrentWindowDrawList(), caretPos, caretSize, btnSize.y, LastItemDisabled ? style.TextStyle.DisabledText : style.TextStyle.Text);
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
