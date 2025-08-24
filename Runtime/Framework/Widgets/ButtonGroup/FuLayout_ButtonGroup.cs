using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework
{
    public partial class FuLayout
    {
        #region Enum Types List
        /// <summary>
        /// Displays a ButtonGroup with all the enum values of type TEnum. The selected item can be changed by the user, and the change will be reported through the itemChange action.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum that will be displayed in the combobox. It must be an enumerated type.</typeparam>
        /// <param name="text">The label text to be displayed next to the combobox</param>
        /// <param name="itemChange">The action that will be called when the selected item changes</param>
        /// <param name="itemGetter">A func that return a way to get current stored value for the combobox. can be null if combobox il not lined to an object's field</param>
        /// <param name="width">maximum width of the group (0 tu let fugui descide)</param>
        /// <param name="padding">padding to use for each buttons</param>
        /// <param name="flags">behaviour flags of the button group</param>
        public void ButtonsGroup<TEnum>(string text, Action<int> itemChange, Func<TEnum> itemGetter = null, float width = 0f, FuButtonsGroupFlags flags = FuButtonsGroupFlags.Default) where TEnum : struct, IConvertible
        {
            ButtonsGroup<TEnum>(text, itemChange, itemGetter, width, Fugui.Themes.FramePadding, flags, FuButtonsGroupStyle.Default);
        }

        /// <summary>
        /// Displays a ButtonGroup with all the enum values of type TEnum. The selected item can be changed by the user, and the change will be reported through the itemChange action.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum that will be displayed in the combobox. It must be an enumerated type.</typeparam>
        /// <param name="text">The label text to be displayed next to the combobox</param>
        /// <param name="itemChange">The action that will be called when the selected item changes</param>
        /// <param name="itemGetter">A func that return a way to get current stored value for the combobox. can be null if combobox il not lined to an object's field</param>
        /// <param name="width">maximum width of the group (0 tu let fugui descide)</param>
        /// <param name="padding">padding to use for each buttons</param>
        /// <param name="flags">behaviour flags of the button group</param>
        /// <param name="style">style of the element</param>
        public void ButtonsGroup<TEnum>(string text, Action<int> itemChange, Func<TEnum> itemGetter, float width, Vector2 padding, FuButtonsGroupFlags flags, FuButtonsGroupStyle style) where TEnum : struct, IConvertible
        {
            FuSelectableBuilder.BuildFromEnum<TEnum>(out List<int> enumValues, out List<string> enumSelectables);

            // call the custom combobox function, passing in the lists and the itemChange
            _buttonsGroup(text, enumSelectables, (index) =>
            {
                itemChange?.Invoke(enumValues[index]);
            }, () => { return itemGetter?.Invoke().ToString(); }, width, padding, flags, style);
        }
        #endregion

        #region Generic Types List
        /// <summary>
        /// Displays a ButtonGroup with a list of items of type T and calls the specified action with the selected item when changed.
        /// </summary>
        /// <typeparam name="T">The type of the items in the list.</typeparam>
        /// <param name="text">The label of the element.</param>
        /// <param name="items">The list of items to display in the buttonGroup.</param>
        /// <param name="itemGetter">A func that return a way to get current stored value for the buttonGroup. can be null if buttonGroup il not linked to an object's field
        /// If you keep it as null, values will be reprocess each frames (better accuratie, but can lead on slowing down on large lists)</param>
        /// <param name="width">maximum width of the group (0 tu let fugui descide)</param>
        /// <param name="padding">padding to use for each buttons</param>
        /// <param name="flags">behaviour flags of the button group</param>
        public void ButtonsGroup<T>(string text, List<T> items, Action<int> callback, Func<string> itemGetter, float width = 0f, FuButtonsGroupFlags flags = FuButtonsGroupFlags.Default)
        {
            _buttonsGroup<T>(text, items, callback, itemGetter, width, Fugui.Themes.FramePadding, flags, FuButtonsGroupStyle.Default);
        }

        /// <summary>
        /// Displays a ButtonGroup with a list of items of type T and calls the specified action with the selected item when changed.
        /// </summary>
        /// <typeparam name="T">The type of the items in the list.</typeparam>
        /// <param name="text">The label of the element.</param>
        /// <param name="items">The list of items to display in the buttonGroup.</param>
        /// <param name="callback">Callback raised whenever selected value change</param>
        /// <param name="itemGetter">A func that return a way to get current stored value for the buttonGroup. can be null if buttonGroup il not linked to an object's field
        /// If you keep it as null, values will be reprocess each frames (better accuratie, but can lead on slowing down on large lists)</param>
        /// <param name="width">maximum width of the group (0 tu let fugui descide)</param>
        /// <param name="padding">padding to use for each buttons</param>
        /// <param name="flags">behaviour flags of the button group</param>
        /// <param name="style">style of the element</param>
        public void ButtonsGroup<T>(string text, List<T> items, Action<int> callback, Func<string> itemGetter, float width, Vector2 padding, FuButtonsGroupFlags flags, FuButtonsGroupStyle style)
        {
            _buttonsGroup<T>(text, items, callback, itemGetter, width, padding, flags, style);
        }
        #endregion

        /// <summary>
        /// Displays a ButtonGroup with a list of items of type T and calls the specified action with the selected item when changed.
        /// </summary>
        /// <typeparam name="T">The type of the items in the list.</typeparam>
        /// <param name="text">The label of the element.</param>
        /// <param name="items">The list of items to display in the buttonGroup.</param>
        /// <param name="callback">Callback raised whenever selected value change</param>
        /// <param name="itemGetter">A func that return a way to get current stored value for the buttonGroup. can be null if buttonGroup il not linked to an object's field
        /// If you keep it as null, values will be reprocess each frames (better accuratie, but can lead on slowing down on large lists)</param>
        /// <param name="width">maximum width of the group (0 tu let fugui descide)</param>
        /// <param name="padding">padding to use for each buttons</param>
        /// <param name="flags">behaviour flags of the button group</param>
        /// <param name="style">style of the element</param>
        protected virtual void _buttonsGroup<T>(string text, List<T> items, Action<int> callback, Func<string> itemGetter, float width, Vector2 padding, FuButtonsGroupFlags flags, FuButtonsGroupStyle style)
        {
            beginElement(ref text, style, true);
            // return if item must no be draw
            if (!_drawElement)
            {
                return;
            }

            // get the current selected index
            int selectedIndex = FuSelectableBuilder.GetSelectedIndex(text, items, itemGetter);

            // draw data
            int nbItems = items.Count;
            Vector2 startPos = ImGui.GetCursorScreenPos();
            float cursorPos = ImGui.GetCursorPosX();
            float avail = ImGui.GetContentRegionAvail().x;
            if (width < avail && width > 0f)
            {
                avail = width;
            }
            float itemWidth = avail / nbItems;
            bool autoSize = flags.HasFlag(FuButtonsGroupFlags.AutoSizeButtons);

            float naturalSize = 0f;
            // align group to the right
            if (!flags.HasFlag(FuButtonsGroupFlags.AlignLeft) && autoSize)
            {
                for (int i = 0; i < nbItems; i++)
                {
                    Vector2 txtSize = ImGui.CalcTextSize(items[i].ToString());
                    naturalSize += 8f * Fugui.CurrentContext.Scale + Mathf.Max(txtSize.x, txtSize.y + 4f * Fugui.CurrentContext.Scale);
                }
                naturalSize += nbItems;
                cursorPos = cursorPos + ImGui.GetContentRegionAvail().x - naturalSize;
            }

            bool updated = false;
            Fugui.Push(ImGuiStyleVar.FrameRounding, 0f);
            // draw buttons
            for (int i = 0; i < nbItems; i++)
            {
                FuButtonStyle btnStyle = default;
                if (selectedIndex == i)
                {
                    btnStyle = style.SelectedButtonStyle;
                }
                else
                {
                    btnStyle = style.ButtonStyle;
                }

                ImGui.SetCursorPosX(cursorPos);
                if (autoSize)
                {
                    Vector2 txtSize = ImGui.CalcTextSize(items[i].ToString());
                    itemWidth = 8f * Fugui.CurrentContext.Scale + Mathf.Max(txtSize.x, txtSize.y + 4f * Fugui.CurrentContext.Scale);
                }
                cursorPos += itemWidth - 1f;
                if (this._customButton(Fugui.AddSpacesBeforeUppercase(items[i].ToString()), new Vector2(itemWidth, 0), padding, Vector2.zero, btnStyle, Fugui.Themes.CurrentTheme.ButtonsGradientStrenght) && !LastItemDisabled)
                {
                    FuSelectableBuilder.SetSelectedIndex(text, i);
                    callback?.Invoke(i);
                    updated = true;
                }
                if (i < nbItems - 1)
                {
                    ImGui.SetCursorScreenPos(startPos + new Vector2(cursorPos, 0f));
                    //ImGui.SameLine();
                }
                displayToolTip();
            }
            Fugui.PopStyle();

            setBaseElementState(text, startPos, ImGui.GetItemRectMax() - startPos, true, updated);
            endElement();
        }
    }
}