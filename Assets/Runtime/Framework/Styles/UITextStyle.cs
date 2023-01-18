using ImGuiNET;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fugui.Framework
{
    public struct UITextStyle : IUIElementStyle
    {
        // enabled
        internal Color Text;
        // disabled
        internal Color DisabledText;

        #region Pressets
        static UITextStyle _defaultTextStyle;
        public static UITextStyle Default { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _defaultTextStyle; } }
        static UITextStyle _selectedTextStyle;
        public static UITextStyle Selected { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _selectedTextStyle; } }
        static UITextStyle _highlightTextStyle;
        public static UITextStyle Highlight { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _highlightTextStyle; } }
        #endregion

        public UITextStyle(Color text, Color disabledText)
        {
            Text = text;
            DisabledText = disabledText;
        }

        public void Push(bool enabled)
        {
            if (enabled)
            {
                FuGui.Push(ImGuiCol.Text, Text);
            }
            else
            {
                FuGui.Push(ImGuiCol.Text, DisabledText);
            }
        }

        public void Pop()
        {
            FuGui.PopColor();
        }

        /// <summary>
        /// Event rised whenever a them is set.
        /// Use this event to set static presset according to UIThemeManager.CurrentTheme
        /// </summary>
        private static void OnThemeSet()
        {
            // default text style
            _defaultTextStyle = new UITextStyle()
            {
                Text = ThemeManager.GetColor(ImGuiCol.Text),
                DisabledText = ThemeManager.GetColor(ImGuiCol.TextDisabled)
            };
            // selected text style
            _selectedTextStyle = new UITextStyle()
            {
                Text = ThemeManager.GetColor(ImGuiCustomCol.SelectedText),
                DisabledText = ThemeManager.GetColor(ImGuiCustomCol.SelectedText) * 0.8f
            };
            // highlight text style
            _highlightTextStyle = new UITextStyle()
            {
                Text = ThemeManager.GetColor(ImGuiCustomCol.HighlightText),
                DisabledText = ThemeManager.GetColor(ImGuiCustomCol.HighlightTextDisabled)
            };
        }
    }
}