using Fu.Core;
using ImGuiNET;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fu.Framework
{
    public struct FuCollapsableStyle : IFuElementStyle
    {
        private Color _color;
        private Color _colorHovered;
        private Color _colorActive;
        private Color _disabledColor;
        private FuTextStyle _text;
        private FuStyle _layout;

        #region Pressets
        // default collapsable style
        static FuCollapsableStyle _defaultContainerStyle;
        /// <summary>
        /// Default collapsable style, use 'Collapsable' theme colors + default text
        /// </summary>
        public static FuCollapsableStyle Default { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _defaultContainerStyle; } }
        #endregion

        #region constructor
        public FuCollapsableStyle(Color color, Color colorHovered, Color colorActive, Color colorDisabled, FuTextStyle textStyle, FuStyle layout)
        {
            _color = color;
            _colorHovered = colorHovered;
            _colorActive = colorActive;
            _disabledColor = colorDisabled;
            _text = textStyle;
            _layout = layout;
        }
        #endregion

        /// <summary>
        /// Pushes the style for the header element.
        /// </summary>
        /// <param name="enabled">Determines if the element is enabled or disabled. If disabled, the colors will be dimmed.</param>
        public void Push(bool enabled)
        {
            if (enabled)
            {
                Fugui.Push(ImGuiCol.Header, _color);
                Fugui.Push(ImGuiCol.HeaderHovered, _colorHovered);
                Fugui.Push(ImGuiCol.HeaderActive, _colorActive);
            }
            else
            {
                Fugui.Push(ImGuiCol.Header, _disabledColor);
                Fugui.Push(ImGuiCol.HeaderHovered, _disabledColor);
                Fugui.Push(ImGuiCol.HeaderActive, _disabledColor);
            }
            _text.Push(enabled);
            _layout.Push(enabled);
            Fugui.Push(ImGuiStyleVar.FrameBorderSize, 0f);
            Fugui.Push(ImGuiStyleVar.FrameRounding, 0f);
        }

        /// <summary>
        /// Pops the style for the header element.
        /// </summary>
        public void Pop()
        {
            Fugui.PopStyle(2);
            _text.Pop();
            _layout.Pop();
            Fugui.PopColor(3);
        }

        /// <summary>
        /// Event rised whenever a them is set.
        /// Use this event to set static presset according to UIThemeManager.CurrentTheme
        /// </summary>
        private static void OnThemeSet()
        {
            // default collapsable style
            _defaultContainerStyle = new FuCollapsableStyle()
            {
                _color = FuThemeManager.GetColor(FuColors.Collapsable),
                _colorHovered = FuThemeManager.GetColor(FuColors.CollapsableHovered),
                _colorActive = FuThemeManager.GetColor(FuColors.CollapsableActive),
                _disabledColor = FuThemeManager.GetColor(FuColors.CollapsableDisabled),
                _text = FuTextStyle.Default,
                _layout = FuStyle.Content
            };
        }
    }
}