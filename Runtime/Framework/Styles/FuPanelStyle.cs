using ImGuiNET;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fu.Framework
{
    public struct FuPanelStyle : IFuElementStyle
    {
        private Color _bgColor;
        private Color _borderColor;

        #region Pressets
        // default container style
        static FuPanelStyle _defaultContainerStyle;
        /// <summary>
        /// Default panel (container) style, use 'WindowBg' and 'Border' theme colors
        /// </summary>
        public static FuPanelStyle Default { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _defaultContainerStyle; } }
       
        // popup container style
        static FuPanelStyle _popupContainerStyle;
        /// <summary>
        /// PopUp panel (container) style, use 'PopupBg' and 'Border' theme colors
        /// </summary>
        public static FuPanelStyle PopUp { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _popupContainerStyle; } }
        
        // transparent container style
        static FuPanelStyle _transparentContainerStyle;
        /// <summary>
        /// Transparent panel (container) style, set transparent BG and borders
        /// </summary>
        public static FuPanelStyle Transparent { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _transparentContainerStyle; } }
        #endregion

        /// <summary>
        /// Pushes the style for the container element.
        /// </summary>
        public void Push(bool enabled)
        {
            Fugui.Push(ImGuiCol.ChildBg, _bgColor);
            Fugui.Push(ImGuiCol.Border, _borderColor);
        }

        /// <summary>
        /// Pops the style for the container element.
        /// </summary>
        public void Pop()
        {
            Fugui.PopColor(2);
        }

        /// <summary>
        /// Event rised whenever a them is set.
        /// Use this event to set static presset according to UIThemeManager.CurrentTheme
        /// </summary>
        private static void OnThemeSet()
        {
            // default container style
            _defaultContainerStyle = new FuPanelStyle()
            {
                _bgColor = FuThemeManager.GetColor(FuColors.WindowBg),
                _borderColor = FuThemeManager.GetColor(FuColors.Border)
            };
            // popup container style
            _popupContainerStyle = new FuPanelStyle()
            {
                _bgColor = FuThemeManager.GetColor(FuColors.PopupBg),
                _borderColor = FuThemeManager.GetColor(FuColors.Border)
            };
            // transparent container style
            _transparentContainerStyle = new FuPanelStyle()
            {
                _bgColor = FuThemeManager.GetColor(FuColors.WindowBg) * 0f,// / 254f,
                _borderColor = FuThemeManager.GetColor(FuColors.Border) * 0f// 254f
            };
        }
    }
}