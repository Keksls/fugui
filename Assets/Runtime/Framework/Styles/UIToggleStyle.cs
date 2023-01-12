using ImGuiNET;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fugui.Framework
{
    public struct UIToggleStyle : IUIElementStyle
    {
        // text
        internal UITextStyle TextStyle;
        internal UITextStyle SelectedTextStyle;
        // background
        internal Color BGColor;
        internal Color SelectedBGColor;
        internal Color DisabledBGColor;
        internal Color DisabledSelectedBGColor;
        // knob
        internal Color KnobColor;
        internal Color SelectedKnobColor;
        internal Color DisabledKnobColor;
        internal Color DisabledSelectedKnobColor;

        #region Pressets
        // default toggle style
        static UIToggleStyle defaultToggleStyle;
        public static UIToggleStyle Default { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return defaultToggleStyle; } }
        #endregion

        /// <summary>
        /// Pushes the style for the Frame elements.
        /// </summary>
        public void Push(bool enabled)
        {
        }

        /// <summary>
        /// Pops the style for the Frame elements.
        /// </summary>
        public void Pop()
        {
        }

        /// <summary>
        /// Event rised whenever a them is set.
        /// Use this event to set static presset according to UIThemeManager.CurrentTheme
        /// </summary>
        private static void OnThemeSet()
        {
            // default button style
            defaultToggleStyle = new UIToggleStyle()
            {
                TextStyle = UITextStyle.Default,
                SelectedTextStyle = new UITextStyle(ThemeManager.GetColor(ImGuiCustomCol.ToggleTextSelected),
                ThemeManager.GetColor(ImGuiCustomCol.ToggleTextSelected) * 0.3f),

                BGColor = ThemeManager.GetColor(ImGuiCol.FrameBg),
                SelectedBGColor = ThemeManager.GetColor(ImGuiCustomCol.Highlight),
                DisabledBGColor = ThemeManager.GetColor(ImGuiCol.FrameBg) * 0.3f,
                DisabledSelectedBGColor = ThemeManager.GetColor(ImGuiCustomCol.Highlight) * 0.3f,

                KnobColor = ThemeManager.GetColor(ImGuiCustomCol.ToggleKnob),
                SelectedKnobColor = ThemeManager.GetColor(ImGuiCustomCol.ToggleKnobSelected),
                DisabledKnobColor = ThemeManager.GetColor(ImGuiCustomCol.ToggleKnobDisabled),
                DisabledSelectedKnobColor = ThemeManager.GetColor(ImGuiCustomCol.ToggleKnobSelectedDisabled),
            };
        }
    }
}