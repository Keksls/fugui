using Fugui.Core;
using ImGuiNET;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fugui.Framework
{
    public struct UIPanelStyle : IUIElementStyle
    {
        private Color _bgColor;
        private Color _borderColor;

        #region Pressets
        // default container style
        static UIPanelStyle _defaultContainerStyle;
        public static UIPanelStyle Default { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _defaultContainerStyle; } }
        // popup container style
        static UIPanelStyle _popupContainerStyle;
        public static UIPanelStyle PopUp { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _popupContainerStyle; } }
        // transparent container style
        static UIPanelStyle _transparentContainerStyle;
        public static UIPanelStyle Transparent { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _transparentContainerStyle; } }
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
            _defaultContainerStyle = new UIPanelStyle()
            {
                _bgColor = ThemeManager.GetColor(FuguiColors.WindowBg),
                _borderColor = ThemeManager.GetColor(FuguiColors.Border)
            };
            // popup container style
            _popupContainerStyle = new UIPanelStyle()
            {
                _bgColor = ThemeManager.GetColor(FuguiColors.PopupBg),
                _borderColor = ThemeManager.GetColor(FuguiColors.Border)
            };
            // transparent container style
            _transparentContainerStyle = new UIPanelStyle()
            {
                _bgColor = ThemeManager.GetColor(FuguiColors.WindowBg) / 254f,
                _borderColor = ThemeManager.GetColor(FuguiColors.Border) / 254f
            };
        }
    }
}