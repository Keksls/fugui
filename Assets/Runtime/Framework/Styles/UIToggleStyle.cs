using ImGuiNET;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fugui.Framework
{
    public struct UIToggleStyle : IUIElementStyle
    {
        // background
        internal Color BGColor;
        internal Color SelectedBGColor;
        // knob
        internal Color KnobColor;
        internal Color SelectedKnobColor;
        // knob
        internal Color TextColor;
        internal Color SelectedTextColor;

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
                BGColor = ThemeManager.GetColor(ImGuiCol.FrameBg),
                SelectedBGColor = ThemeManager.GetColor(ImGuiCustomCol.Selected),

                KnobColor = ThemeManager.GetColor(ImGuiCol.Text),
                SelectedKnobColor = ThemeManager.GetColor(ImGuiCol.Text),

                TextColor = ThemeManager.GetColor(ImGuiCol.Text),
                SelectedTextColor = ThemeManager.GetColor(ImGuiCustomCol.SelectedText),
            };
        }
    }
}