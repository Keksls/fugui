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
        static UIComboboxStyle _defaultStyle;
        public static UIComboboxStyle Default { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _defaultStyle; } }

        // blue button style
        static UIComboboxStyle _highlightStyle;
        public static UIComboboxStyle Highlight { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _highlightStyle; } }
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
            _defaultStyle = new UIComboboxStyle()
            {
                ButtonStyle = UIButtonStyle.Default,
                _frame = ImGui.GetStyle().Colors[(int)ImGuiCol.Button],
                _frameHovered = ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonHovered],
                _frameActive = ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive],
                _frameDisabled = ImGui.GetStyle().Colors[(int)ImGuiCol.Button] / 2f,
            };

            // blue button style
            _highlightStyle = new UIComboboxStyle()
            {
                ButtonStyle = UIButtonStyle.Highlight,
                _frame = ThemeManager.GetColor(FuguiColors.Highlight),
                _frameHovered = ThemeManager.GetColor(FuguiColors.HighlightHovered),
                _frameActive = ThemeManager.GetColor(FuguiColors.HighlightActive),
                _frameDisabled = ThemeManager.GetColor(FuguiColors.HighlightDisabled)
            };
        }
    }
}