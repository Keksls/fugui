using Fugui.Core;
using ImGuiNET;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fugui.Framework
{
    public struct UICollapsableStyle : IUIElementStyle
    {
        private Color _color;
        private Color _colorHovered;
        private Color _colorActive;
        private Color _disabledColor;
        private UITextStyle _text;
        private UIStyle _layout;

        #region Pressets
        // default collapsable style
        static UICollapsableStyle _defaultContainerStyle;
        public static UICollapsableStyle Default { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _defaultContainerStyle; } }
        #endregion

        /// <summary>
        /// Pushes the style for the header element.
        /// </summary>
        /// <param name="enabled">Determines if the element is enabled or disabled. If disabled, the colors will be dimmed.</param>
        public void Push(bool enabled)
        {
            if (enabled)
            {
                FuGui.Push(ImGuiCol.Header, _color);
                FuGui.Push(ImGuiCol.HeaderHovered, _colorHovered);
                FuGui.Push(ImGuiCol.HeaderActive, _colorActive);
            }
            else
            {
                FuGui.Push(ImGuiCol.Header, _disabledColor);
                FuGui.Push(ImGuiCol.HeaderHovered, _disabledColor);
                FuGui.Push(ImGuiCol.HeaderActive, _disabledColor);
            }
            _text.Push(enabled);
            _layout.Push(enabled);
            FuGui.Push(ImGuiStyleVar.FrameBorderSize, 0f);
            FuGui.Push(ImGuiStyleVar.FrameRounding, 0f);
        }

        /// <summary>
        /// Pops the style for the header element.
        /// </summary>
        public void Pop()
        {
            FuGui.PopStyle(2);
            _text.Pop();
            _layout.Pop();
            FuGui.PopColor(3);
        }

        /// <summary>
        /// Event rised whenever a them is set.
        /// Use this event to set static presset according to UIThemeManager.CurrentTheme
        /// </summary>
        private static void OnThemeSet()
        {
            // default collapsable style
            _defaultContainerStyle = new UICollapsableStyle()
            {
                _color = ThemeManager.GetColor(FuguiColors.Collapsable),
                _colorHovered = ThemeManager.GetColor(FuguiColors.CollapsableHovered),
                _colorActive = ThemeManager.GetColor(FuguiColors.CollapsableActive),
                _disabledColor = ThemeManager.GetColor(FuguiColors.CollapsableDisabled),
                _text = UITextStyle.Default,
                _layout = UIStyle.Default
            };
        }
    }
}