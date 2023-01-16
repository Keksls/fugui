using Fugui.Core;
using ImGuiNET;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fugui.Framework
{
    public struct UIComboboxStyle : IUIElementStyle
    {
        public UIButtonStyle ButtonStyle { get; private set; }
        // enabled
        private Color _frame;
        private Color _frameHovered;
        private Color _frameActive;
        // disabled
        private Color _frameDisabled;

        #region Pressets
        // default button style
        static UIComboboxStyle defaultButtonStyle;
        public static UIComboboxStyle Default { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return defaultButtonStyle; } }

        // blue button style
        static UIComboboxStyle blueButtonStyle;
        public static UIComboboxStyle Blue { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return blueButtonStyle; } }
        #endregion
       
        /// <summary>
        /// Pushes the style for the combobox element.
        /// </summary>
        public void Push(bool enabled)
        {
            ButtonStyle.Push(enabled);
            if (enabled)
            {
                FuGui.Push(ImGuiCol.FrameBg, _frame);
                FuGui.Push(ImGuiCol.FrameBgHovered, _frameHovered);
                FuGui.Push(ImGuiCol.FrameBgActive, _frameActive);
            }
            else
            {
                FuGui.Push(ImGuiCol.FrameBg, _frameDisabled);
                FuGui.Push(ImGuiCol.FrameBgHovered, _frameDisabled);
                FuGui.Push(ImGuiCol.FrameBgActive, _frameDisabled);
            }
        }
        /// <summary>
        /// Pops the style for the combobox element.
        /// </summary>
        public void Pop()
        {
            FuGui.PopColor(3);
            ButtonStyle.Pop();
        }

        /// <summary>
        /// Event rised whenever a them is set.
        /// Use this event to set static presset according to UIThemeManager.CurrentTheme
        /// </summary>
        private static void OnThemeSet()
        {
            // default button style
            defaultButtonStyle = new UIComboboxStyle()
            {
                ButtonStyle = UIButtonStyle.Default,
                _frame = ImGui.GetStyle().Colors[(int)ImGuiCol.Button],
                _frameHovered = ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonHovered],
                _frameActive = ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive],
                _frameDisabled = ImGui.GetStyle().Colors[(int)ImGuiCol.Button] / 2f,
            };

        // blue button style
        blueButtonStyle = new UIComboboxStyle()
        {
            ButtonStyle = UIButtonStyle.Highlight,
            _frame = ThemeManager.GetColor(ImGuiCustomCol.Highlight),
            _frameHovered = ThemeManager.GetColor(ImGuiCustomCol.HighlightHovered),
            _frameActive = ThemeManager.GetColor(ImGuiCustomCol.HighlightActive),
            _frameDisabled = ThemeManager.GetColor(ImGuiCustomCol.HighlightDisabled)
        };
    }
    }
}