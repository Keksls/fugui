using ImGuiNET;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fugui.Framework
{
    public struct UITextStyle : IUIElementStyle
    {
        // enabled
        private Color _text;
        // disabled
        private Color _disabledText;

        #region Pressets
        static UITextStyle _defaultTextStyle;
        public static UITextStyle Default { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _defaultTextStyle; } }
        static UITextStyle _pureTextStyle;
        public static UITextStyle Pure { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _pureTextStyle; } }
        #endregion

        public UITextStyle(Color text, Color disabledText)
        {
            _text = text;
            _disabledText = disabledText;
        }

        public void Push(bool enabled)
        {
            if (enabled)
            {
                FuGui.Push(ImGuiCol.Text, _text);
            }
            else
            {
                FuGui.Push(ImGuiCol.Text, _disabledText);
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
                _text = ThemeManager.GetColor(ImGuiCol.Text),
                _disabledText = ThemeManager.GetColor(ImGuiCol.TextDisabled)
            };
            // pure text style
            _pureTextStyle = new UITextStyle()
            {
                _text = ThemeManager.CurrentTheme == Theme.Dark ? Color.white : Color.black,
                _disabledText = ThemeManager.GetColor(ImGuiCol.TextDisabled)
            };
        }
    }
}