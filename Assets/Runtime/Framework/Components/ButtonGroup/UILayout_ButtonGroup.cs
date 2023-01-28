using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fugui.Framework
{
    public partial class UILayout
    {
        public void ButtonsGroup<TEnum>(string text, Action<TEnum> itemChange, int defaultSelected = 0, ButtonsGroupFlags flags = ButtonsGroupFlags.Default) where TEnum : struct, IConvertible
        {
            ButtonsGroup<TEnum>(text, itemChange, defaultSelected, flags, UIButtonsGroupStyle.Default);
        }

        public void ButtonsGroup<TEnum>(string text, Action<TEnum> itemChange, int defaultSelected, ButtonsGroupFlags flags, UIButtonsGroupStyle style) where TEnum : struct, IConvertible
        {
            if (!typeof(TEnum).IsEnum)
            {
                throw new ArgumentException("TEnum must be an enumerated type");
            }
            // list to store the enum values
            List<TEnum> enumValues = new List<TEnum>();
            // list to store the combobox items
            List<string> cItems = new List<string>();
            // iterate over the enum values and add them to the lists
            foreach (TEnum enumValue in Enum.GetValues(typeof(TEnum)))
            {
                enumValues.Add(enumValue);
                cItems.Add(FuGui.AddSpacesBeforeUppercase(enumValue.ToString()));
            }
            // call the custom combobox function, passing in the lists and the itemChange
            _buttonsGroup(text, cItems, (index) =>
            {
                itemChange?.Invoke(enumValues[index]);
            }, defaultSelected, flags, style);
        }

        public void ButtonsGroup<T>(string id, List<T> items, Action<T> callback, int defaultSelected, ButtonsGroupFlags flags = ButtonsGroupFlags.Default)
        {
            _buttonsGroup<T>(id, items, (index) => { callback?.Invoke(items[index]); }, defaultSelected, flags, UIButtonsGroupStyle.Default);
        }

        public void ButtonsGroup<T>(string id, List<T> items, Action<T> callback, int defaultSelected, ButtonsGroupFlags flags, UIButtonsGroupStyle style)
        {
            _buttonsGroup<T>(id, items, (index) => { callback?.Invoke(items[index]); }, defaultSelected, flags, style);
        }

        protected virtual void _buttonsGroup<T>(string id, List<T> items, Action<int> callback, int defaultSelected, ButtonsGroupFlags flags, UIButtonsGroupStyle style)
        {
            beginElement(id, style);
            // get selected
            if (!_buttonsGroupIndex.ContainsKey(id))
            {
                _buttonsGroupIndex.Add(id, defaultSelected);
            }
            int selected = _buttonsGroupIndex[id];

            // draw data
            int nbItems = items.Count;
            float cursorPos = ImGui.GetCursorPos().x;
            float avail = ImGui.GetContentRegionAvail().x;
            float itemWidth = avail / nbItems;
            bool autoSize = flags.HasFlag(ButtonsGroupFlags.AutoSizeButtons);

            float naturalSize = 0f;
            // align group to the right
            if (!flags.HasFlag(ButtonsGroupFlags.AlignLeft) && autoSize)
            {
                for (int i = 0; i < nbItems; i++)
                {
                    Vector2 txtSize = ImGui.CalcTextSize(items[i].ToString());
                    naturalSize += 8f + Mathf.Max(txtSize.x, txtSize.y + 4f * FuGui.CurrentContext.Scale);
                }
                naturalSize += nbItems;
                cursorPos = cursorPos + ImGui.GetContentRegionAvail().x - naturalSize;
            }

            FuGui.Push(ImGuiStyleVar.FrameRounding, 0f);
            // draw buttons
            for (int i = 0; i < nbItems; i++)
            {
                if (selected == i)
                {
                    style.SelectedButtonStyle.Push(!_nextIsDisabled);
                }
                else
                {
                    style.ButtonStyle.Push(!_nextIsDisabled);
                }

                ImGui.SetCursorPosX(cursorPos);
                if (autoSize)
                {
                    Vector2 txtSize = ImGui.CalcTextSize(items[i].ToString());
                    itemWidth = 8f * FuGui.CurrentContext.Scale + Mathf.Max(txtSize.x, txtSize.y + 4f * FuGui.CurrentContext.Scale);
                }
                cursorPos += itemWidth - 1f;
                FuGui.Push(ImGuiStyleVar.FramePadding, new Vector4(4f, 4f) * FuGui.CurrentContext.Scale);
                if (ImGui.Button(items[i].ToString() + "##" + id, new Vector2(itemWidth, 0)) && !_nextIsDisabled)
                {
                    _buttonsGroupIndex[id] = i;
                    callback?.Invoke(i);
                }
                if (i < nbItems - 1)
                {
                    ImGui.SameLine();
                }
                FuGui.PopStyle();
                UIButtonStyle.Default.Pop();
                displayToolTip();
            }
            FuGui.PopStyle();
            endElement();
        }
    }
}