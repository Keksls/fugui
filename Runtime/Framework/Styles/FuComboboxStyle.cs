using ImGuiNET;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fu.Framework
{
    public struct FuComboboxStyle : IFuElementStyle
    {
        public FuButtonStyle ButtonStyle { get; private set; }
        // enabled
        private Color _frame;
        private Color _frameHovered;
        private Color _frameActive;
        // disabled
        private Color _frameDisabled;

        #region Pressets
        // Default combobox style
        static FuComboboxStyle _defaultStyle;
        /// <summary>
        /// Default combox colors, use 'Button' theme colors
        /// </summary>
        public static FuComboboxStyle Default { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _defaultStyle; } }

        // Highlight combobox style
        static FuComboboxStyle _highlightStyle;
        /// <summary>
        /// Highlight combox colors, use 'Highlight' theme colors
        /// </summary>
        public static FuComboboxStyle Highlight { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _highlightStyle; } }

        // Selected combobox style
        static FuComboboxStyle _selectedStyle;
        /// <summary>
        /// Selected combox colors, use 'Selected' theme colors
        /// </summary>
        public static FuComboboxStyle Selected { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _selectedStyle; } }
        #endregion

        #region constructor
        public FuComboboxStyle(Color color, Color colorHovered, Color colorActive, Color colorDisabled, FuButtonStyle buttonStyle)
        {
            _frame = color;
            _frameHovered = colorHovered;
            _frameActive = colorActive;
            _frameDisabled = colorDisabled;
            ButtonStyle = buttonStyle;
        }
        #endregion

        /// <summary>
        /// Pushes the style for the combobox element.
        /// </summary>
        public void Push(bool enabled)
        {
            ButtonStyle.Push(enabled);
            if (enabled)
            {
                Fugui.Push(ImGuiCol.FrameBg, _frame);
                Fugui.Push(ImGuiCol.FrameBgHovered, _frameHovered);
                Fugui.Push(ImGuiCol.FrameBgActive, _frameActive);
            }
            else
            {
                Fugui.Push(ImGuiCol.FrameBg, _frameDisabled);
                Fugui.Push(ImGuiCol.FrameBgHovered, _frameDisabled);
                Fugui.Push(ImGuiCol.FrameBgActive, _frameDisabled);
            }
        }
        /// <summary>
        /// Pops the style for the combobox element.
        /// </summary>
        public void Pop()
        {
            Fugui.PopColor(3);
            ButtonStyle.Pop();
        }

        /// <summary>
        /// Event rised whenever a them is set.
        /// Use this event to set static presset according to UIThemeManager.CurrentTheme
        /// </summary>
        private static void OnThemeSet()
        {
            // default button style
            _defaultStyle = new FuComboboxStyle()
            {
                ButtonStyle = FuButtonStyle.Default,
                _frame = ImGui.GetStyle().Colors[(int)ImGuiCol.Button],
                _frameHovered = ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonHovered],
                _frameActive = ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive],
                _frameDisabled = ImGui.GetStyle().Colors[(int)ImGuiCol.Button] / 2f,
            };

            // blue button style
            _highlightStyle = new FuComboboxStyle()
            {
                ButtonStyle = FuButtonStyle.Highlight,
                _frame = FuThemeManager.GetColor(FuColors.Highlight),
                _frameHovered = FuThemeManager.GetColor(FuColors.HighlightHovered),
                _frameActive = FuThemeManager.GetColor(FuColors.HighlightActive),
                _frameDisabled = FuThemeManager.GetColor(FuColors.HighlightDisabled)
            };

            // selected style
            _selectedStyle = new FuComboboxStyle()
            {
                ButtonStyle = FuButtonStyle.Selected,
                _frame = FuThemeManager.GetColor(FuColors.Selected),
                _frameHovered = FuThemeManager.GetColor(FuColors.SelectedHovered),
                _frameActive = FuThemeManager.GetColor(FuColors.SelectedActive),
                _frameDisabled = FuThemeManager.GetColor(FuColors.Selected) * 0.5f
            };
        }
    }
}