using ImGuiNET;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fugui.Framework
{
    public struct UIButtonStyle : IUIElementStyle
    {
        // enabled
        internal Color Button;
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
        static UIButtonStyle _infoButtonStyle;
        public static UIButtonStyle Info { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _infoButtonStyle; } }
        static UIButtonStyle _successButtonStyle;
        public static UIButtonStyle Success { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _successButtonStyle; } }
        static UIButtonStyle _warningButtonStyle;
        public static UIButtonStyle Warning { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _warningButtonStyle; } }
        static UIButtonStyle _dangerButtonStyle;
        public static UIButtonStyle Danger { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _dangerButtonStyle; } }
        #endregion

        /// <summary>
        /// Get the instance of UIButtonStyle that match with the StateType enum value givent in parameter
        /// </summary>
        /// <param name="type">the StateType enum value to get Style on</param>
        /// <returns>mathcing UIButtonStyle</returns>
        public static UIButtonStyle GetStyleForState(StateType type)
        {
            switch (type)
            {
                case StateType.Danger:
                    return UIButtonStyle.Danger;
                case StateType.Success:
                    return UIButtonStyle.Success;
                case StateType.Info:
                    return UIButtonStyle.Info;
                case StateType.Warning:
                    return UIButtonStyle.Warning;
                default:
                    return UIButtonStyle.Default;
            }
        }

        /// <summary>
        /// Pushes the UIButtonStyle onto the ImGui style stack. If enabled is true, the enabled style will be used. If enabled is false, the disabled style will be used.
        /// </summary>
        /// <param name="enabled">Determines whether to use the enabled or disabled style</param>
        public void Push(bool enabled)
        {
            if (enabled)
            {
                FuGui.Push(ImGuiCol.Button, Button); // push the enabled button color onto the stack
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
                _framePadding = new Vector2(8f, 4f) * FuGui.CurrentContext.Scale,
                Button = ThemeManager.GetColor(FuguiColors.Button),
                _buttonHovered = ThemeManager.GetColor(FuguiColors.ButtonHovered),
                _buttonActive = ThemeManager.GetColor(FuguiColors.ButtonActive),
                _disabledButton = ThemeManager.GetColor(FuguiColors.Button) * 0.5f,
                TextStyle = UITextStyle.Default
            };
            // blue button style
            _highlightButtonStyle = new UIButtonStyle()
            {
                _framePadding = new Vector2(8f, 4f) * FuGui.CurrentContext.Scale,
                Button = ThemeManager.GetColor(FuguiColors.Highlight),
                _buttonHovered = ThemeManager.GetColor(FuguiColors.HighlightHovered),
                _buttonActive = ThemeManager.GetColor(FuguiColors.HighlightActive),
                _disabledButton = ThemeManager.GetColor(FuguiColors.HighlightDisabled),
                TextStyle = UITextStyle.Highlight
            };
            // selected
            _selectedButtonStyle = new UIButtonStyle()
            {
                _framePadding = new Vector2(8f, 4f) * FuGui.CurrentContext.Scale,
                Button = ThemeManager.GetColor(FuguiColors.Selected),
                _buttonHovered = ThemeManager.GetColor(FuguiColors.SelectedHovered),
                _buttonActive = ThemeManager.GetColor(FuguiColors.SelectedActive),
                _disabledButton = ThemeManager.GetColor(FuguiColors.Selected) * 0.5f,
                TextStyle = UITextStyle.Selected
            };
            // danger
            _dangerButtonStyle = new UIButtonStyle()
            {
                _framePadding = new Vector2(8f, 4f) * FuGui.CurrentContext.Scale,
                Button = ThemeManager.GetColor(FuguiColors.BackgroundDanger),
                _buttonHovered = ThemeManager.GetColor(FuguiColors.BackgroundDanger) * 0.9f,
                _buttonActive = ThemeManager.GetColor(FuguiColors.BackgroundDanger) * 0.8f,
                _disabledButton = ThemeManager.GetColor(FuguiColors.BackgroundDanger) * 0.5f,
                TextStyle = UITextStyle.Default
            };
            // info
            _infoButtonStyle = new UIButtonStyle()
            {
                _framePadding = new Vector2(8f, 4f) * FuGui.CurrentContext.Scale,
                Button = ThemeManager.GetColor(FuguiColors.BackgroundInfo),
                _buttonHovered = ThemeManager.GetColor(FuguiColors.BackgroundInfo) * 0.9f,
                _buttonActive = ThemeManager.GetColor(FuguiColors.BackgroundInfo) * 0.8f,
                _disabledButton = ThemeManager.GetColor(FuguiColors.BackgroundInfo) * 0.5f,
                TextStyle = UITextStyle.Default
            };
            // success
            _successButtonStyle = new UIButtonStyle()
            {
                _framePadding = new Vector2(8f, 4f) * FuGui.CurrentContext.Scale,
                Button = ThemeManager.GetColor(FuguiColors.BackgroundSuccess),
                _buttonHovered = ThemeManager.GetColor(FuguiColors.BackgroundSuccess) * 0.9f,
                _buttonActive = ThemeManager.GetColor(FuguiColors.BackgroundSuccess) * 0.8f,
                _disabledButton = ThemeManager.GetColor(FuguiColors.BackgroundSuccess) * 0.5f,
                TextStyle = UITextStyle.Default
            };
            // warning
            _warningButtonStyle = new UIButtonStyle()
            {
                _framePadding = new Vector2(8f, 4f) * FuGui.CurrentContext.Scale,
                Button = ThemeManager.GetColor(FuguiColors.BackgroundWarning),
                _buttonHovered = ThemeManager.GetColor(FuguiColors.BackgroundWarning) * 0.9f,
                _buttonActive = ThemeManager.GetColor(FuguiColors.BackgroundWarning) * 0.8f,
                _disabledButton = ThemeManager.GetColor(FuguiColors.BackgroundWarning) * 0.5f,
                TextStyle = UITextStyle.Default
            };
        }
    }
}