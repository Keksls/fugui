using Fugui.Core;
using ImGuiNET;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fugui.Framework
{
    public struct UIContainerStyle : IUIElementStyle
    {
        private Color _bgColor;
        private Color _borderColor;

        #region Pressets
        // default container style
        static UIContainerStyle _defaultContainerStyle;
        public static UIContainerStyle Default { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _defaultContainerStyle; } }
        // popup container style
        static UIContainerStyle _popupContainerStyle;
        public static UIContainerStyle PopUp { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _popupContainerStyle; } }
        // transparent container style
        static UIContainerStyle _transparentContainerStyle;
        public static UIContainerStyle Transparent { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _transparentContainerStyle; } }
        #endregion

        /// <summary>
        /// Pushes the style for the container element.
        /// </summary>
        public void Push(bool enabled)
        {
            FuGui.Push(ImGuiCol.ChildBg, _bgColor);
            FuGui.Push(ImGuiCol.Border, _borderColor);
        }

        /// <summary>
        /// Pops the style for the container element.
        /// </summary>
        public void Pop()
        {
            FuGui.PopColor(2);
        }

        /// <summary>
        /// Event rised whenever a them is set.
        /// Use this event to set static presset according to UIThemeManager.CurrentTheme
        /// </summary>
        private static void OnThemeSet()
        {
            // default container style
            _defaultContainerStyle = new UIContainerStyle()
            {
                _bgColor = ThemeManager.GetColor(ImGuiCol.WindowBg),
                _borderColor = ThemeManager.GetColor(ImGuiCol.Border)
            };
            // popup container style
            _popupContainerStyle = new UIContainerStyle()
            {
                _bgColor = ThemeManager.GetColor(ImGuiCol.PopupBg),
                _borderColor = ThemeManager.GetColor(ImGuiCol.Border)
            };
            // transparent container style
            _transparentContainerStyle = new UIContainerStyle()
            {
                _bgColor = Vector4.zero,
                _borderColor = Vector4.zero
            };
        }
    }
}