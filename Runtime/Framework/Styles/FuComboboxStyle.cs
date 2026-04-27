using ImGuiNET;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fu.Framework
{
    /// <summary>
    /// Represents the Fu Combobox Style data structure.
    /// </summary>
    public struct FuComboboxStyle : IFuElementStyle
    {
        #region State
        public FuButtonStyle ButtonStyle { get; private set; }
        // enabled

        private Color _frame;
        private Color _frameHovered;
        private Color _frameActive;
        // disabled
        private Color _frameDisabled;

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

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the Fu Combobox Style class.
        /// </summary>
        /// <param name="color">The color value.</param>
        /// <param name="colorHovered">The color Hovered value.</param>
        /// <param name="colorActive">The color Active value.</param>
        /// <param name="colorDisabled">The color Disabled value.</param>
        /// <param name="buttonStyle">The button Style value.</param>
        public FuComboboxStyle(Color color, Color colorHovered, Color colorActive, Color colorDisabled, FuButtonStyle buttonStyle)
        {
            _frame = color;
            _frameHovered = colorHovered;
            _frameActive = colorActive;
            _frameDisabled = colorDisabled;
            ButtonStyle = buttonStyle;
        }
        #endregion

        #region Methods
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
                _frame = Fugui.Themes.GetColor(FuColors.Highlight),
                _frameHovered = Fugui.Themes.GetColor(FuColors.HighlightHovered),
                _frameActive = Fugui.Themes.GetColor(FuColors.HighlightActive),
                _frameDisabled = Fugui.Themes.GetColor(FuColors.HighlightDisabled)
            };

            // selected style
            _selectedStyle = new FuComboboxStyle()
            {
                ButtonStyle = FuButtonStyle.Selected,
                _frame = Fugui.Themes.GetColor(FuColors.Selected),
                _frameHovered = Fugui.Themes.GetColor(FuColors.SelectedHovered),
                _frameActive = Fugui.Themes.GetColor(FuColors.SelectedActive),
                _frameDisabled = Fugui.Themes.GetColor(FuColors.Selected) * 0.5f
            };
        }
        #endregion
    }
}