using Fugui.Core;
using ImGuiNET;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fugui.Framework
{
    public struct UIButtonStyle : IUIElementStyle
    {
        // enabled
        private Color _button;
        private Color _buttonHovered;
        private Color _buttonActive;
        // disabled
        private Color _disabledButton;
        // additional styles
        public UITextStyle TextStyle { get; private set; }
        private Vector2 _framePadding;

        #region Pressets
        static UIButtonStyle _defaultButtonStyle;
        public static UIButtonStyle Default { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _defaultButtonStyle; } }
        static UIButtonStyle _selectedButtonStyle;
        public static UIButtonStyle Selected { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _selectedButtonStyle; } }
        static UIButtonStyle _highlightButtonStyle;
        public static UIButtonStyle Highlight { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _highlightButtonStyle; } }

        // button sizes
        static readonly Vector2 _frameAutoSize = new Vector2(0, 0);
        public static Vector2 AutoSize { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _frameAutoSize; } }
        static readonly Vector2 _frameFullSize = new Vector2(-1, 0);
        public static Vector2 FullSize { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _frameFullSize; } }
        #endregion

        /// <summary>
        /// Pushes the UIButtonStyle onto the ImGui style stack. If enabled is true, the enabled style will be used. If enabled is false, the disabled style will be used.
        /// </summary>
        /// <param name="enabled">Determines whether to use the enabled or disabled style</param>
        public void Push(bool enabled)
        {
            if (enabled)
            {
                FuGui.Push(ImGuiCol.Button, _button); // push the enabled button color onto the stack
                FuGui.Push(ImGuiCol.ButtonHovered, _buttonHovered); // push the enabled button hovered color onto the stack
                FuGui.Push(ImGuiCol.ButtonActive, _buttonActive); // push the enabled button active color onto the stack
            }
            else
            {
                FuGui.Push(ImGuiCol.Button, _disabledButton); // push the disabled button color onto the stack
                FuGui.Push(ImGuiCol.ButtonHovered, _disabledButton); // push the disabled button hovered color onto the stack
                FuGui.Push(ImGuiCol.ButtonActive, _disabledButton); // push the disabled button active color onto the stack
            }
            FuGui.Push(ImGuiStyleVar.FramePadding, _framePadding); // push the frame padding onto the stack
            FuGui.Push(ImGuiStyleVar.FrameBorderSize, 0.5f); // push the frame border size onto the stack
            TextStyle.Push(enabled); // push the text style onto the stack
        }

        /// <summary>
        /// Pops the UIButtonStyle off the ImGui style stack.
        /// </summary>
        public void Pop()
        {
            TextStyle.Pop(); // pop the text style off the stack
            FuGui.PopStyle(2); // pop the frame padding and frame border size off the stack
            FuGui.PopColor(3); // pop the button, button hovered, and button active colors off the stack
        }

        /// <summary>
        /// Event rised whenever a them is set.
        /// Use this event to set static presset according to UIThemeManager.CurrentTheme
        /// </summary>
        private static void OnThemeSet()
        {
            // default button style
            _defaultButtonStyle = new UIButtonStyle()
            {
                _framePadding = new Vector2(8f, 4f),
                _button = ThemeManager.GetColor(ImGuiCol.Button),
                _buttonHovered = ThemeManager.GetColor(ImGuiCol.ButtonHovered),
                _buttonActive = ThemeManager.GetColor(ImGuiCol.ButtonActive),
                _disabledButton = ThemeManager.GetColor(ImGuiCol.Button) * 0.5f,
                TextStyle = UITextStyle.Default
            };
            // blue button style
            _highlightButtonStyle = new UIButtonStyle()
            {
                _framePadding = new Vector2(8f, 2f),
                _button = ThemeManager.GetColor(ImGuiCustomCol.Highlight),
                _buttonHovered = ThemeManager.GetColor(ImGuiCustomCol.HighlightHovered),
                _buttonActive = ThemeManager.GetColor(ImGuiCustomCol.HighlightActive),
                _disabledButton = ThemeManager.GetColor(ImGuiCustomCol.HighlightDisabled),
                TextStyle = UITextStyle.Default
            };
            // selected
            _selectedButtonStyle = new UIButtonStyle()
            {
                _button = ThemeManager.GetColor(ImGuiCustomCol.Selected),
                _buttonHovered = ThemeManager.GetColor(ImGuiCustomCol.SelectedHovered),
                _buttonActive = ThemeManager.GetColor(ImGuiCustomCol.SelectedActive),
                _disabledButton = ThemeManager.GetColor(ImGuiCustomCol.Selected) * 0.5f,
                TextStyle = UITextStyle.Selected
            };
        }
    }
}