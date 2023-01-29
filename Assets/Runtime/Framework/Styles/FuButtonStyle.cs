using ImGuiNET;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fu.Framework
{
    public struct FuButtonStyle : IFuElementStyle
    {
        // enabled
        internal Color Button;
        private Color _buttonHovered;
        private Color _buttonActive;
        // disabled
        private Color _disabledButton;
        // additional styles
        public FuTextStyle TextStyle { get; private set; }
        private Vector2 _framePadding;

        #region Pressets
        static FuButtonStyle _defaultButtonStyle;
        public static FuButtonStyle Default { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _defaultButtonStyle; } }
        static FuButtonStyle _selectedButtonStyle;
        public static FuButtonStyle Selected { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _selectedButtonStyle; } }
        static FuButtonStyle _highlightButtonStyle;
        public static FuButtonStyle Highlight { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _highlightButtonStyle; } }
        static FuButtonStyle _infoButtonStyle;
        public static FuButtonStyle Info { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _infoButtonStyle; } }
        static FuButtonStyle _successButtonStyle;
        public static FuButtonStyle Success { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _successButtonStyle; } }
        static FuButtonStyle _warningButtonStyle;
        public static FuButtonStyle Warning { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _warningButtonStyle; } }
        static FuButtonStyle _dangerButtonStyle;
        public static FuButtonStyle Danger { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _dangerButtonStyle; } }
        #endregion

        /// <summary>
        /// Get the instance of UIButtonStyle that match with the StateType enum value givent in parameter
        /// </summary>
        /// <param name="type">the StateType enum value to get Style on</param>
        /// <returns>mathcing UIButtonStyle</returns>
        public static FuButtonStyle GetStyleForState(StateType type)
        {
            switch (type)
            {
                case StateType.Danger:
                    return FuButtonStyle.Danger;
                case StateType.Success:
                    return FuButtonStyle.Success;
                case StateType.Info:
                    return FuButtonStyle.Info;
                case StateType.Warning:
                    return FuButtonStyle.Warning;
                default:
                    return FuButtonStyle.Default;
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
                Fugui.Push(ImGuiCol.Button, Button); // push the enabled button color onto the stack
                Fugui.Push(ImGuiCol.ButtonHovered, _buttonHovered); // push the enabled button hovered color onto the stack
                Fugui.Push(ImGuiCol.ButtonActive, _buttonActive); // push the enabled button active color onto the stack
            }
            else
            {
                Fugui.Push(ImGuiCol.Button, _disabledButton); // push the disabled button color onto the stack
                Fugui.Push(ImGuiCol.ButtonHovered, _disabledButton); // push the disabled button hovered color onto the stack
                Fugui.Push(ImGuiCol.ButtonActive, _disabledButton); // push the disabled button active color onto the stack
            }
            Fugui.Push(ImGuiStyleVar.FramePadding, _framePadding * Fugui.CurrentContext.Scale); // push the frame padding onto the stack
            Fugui.Push(ImGuiStyleVar.FrameBorderSize, 0.5f * Fugui.CurrentContext.Scale); // push the frame border size onto the stack
            TextStyle.Push(enabled); // push the text style onto the stack
        }

        /// <summary>
        /// Pops the UIButtonStyle off the ImGui style stack.
        /// </summary>
        public void Pop()
        {
            TextStyle.Pop(); // pop the text style off the stack
            Fugui.PopStyle(2); // pop the frame padding and frame border size off the stack
            Fugui.PopColor(3); // pop the button, button hovered, and button active colors off the stack
        }

        /// <summary>
        /// Event rised whenever a them is set.
        /// Use this event to set static presset according to UIThemeManager.CurrentTheme
        /// </summary>
        private static void OnThemeSet()
        {
            // default button style
            _defaultButtonStyle = new FuButtonStyle()
            {
                _framePadding = new Vector2(8f, 4f),
                Button = FuThemeManager.GetColor(FuColors.Button),
                _buttonHovered = FuThemeManager.GetColor(FuColors.ButtonHovered),
                _buttonActive = FuThemeManager.GetColor(FuColors.ButtonActive),
                _disabledButton = FuThemeManager.GetColor(FuColors.Button) * 0.5f,
                TextStyle = FuTextStyle.Default
            };
            // blue button style
            _highlightButtonStyle = new FuButtonStyle()
            {
                _framePadding = new Vector2(8f, 4f),
                Button = FuThemeManager.GetColor(FuColors.Highlight),
                _buttonHovered = FuThemeManager.GetColor(FuColors.HighlightHovered),
                _buttonActive = FuThemeManager.GetColor(FuColors.HighlightActive),
                _disabledButton = FuThemeManager.GetColor(FuColors.HighlightDisabled),
                TextStyle = FuTextStyle.Highlight
            };
            // selected
            _selectedButtonStyle = new FuButtonStyle()
            {
                _framePadding = new Vector2(8f, 4f),
                Button = FuThemeManager.GetColor(FuColors.Selected),
                _buttonHovered = FuThemeManager.GetColor(FuColors.SelectedHovered),
                _buttonActive = FuThemeManager.GetColor(FuColors.SelectedActive),
                _disabledButton = FuThemeManager.GetColor(FuColors.Selected) * 0.5f,
                TextStyle = FuTextStyle.Selected
            };
            // danger
            _dangerButtonStyle = new FuButtonStyle()
            {
                _framePadding = new Vector2(8f, 4f),
                Button = FuThemeManager.GetColor(FuColors.BackgroundDanger),
                _buttonHovered = FuThemeManager.GetColor(FuColors.BackgroundDanger) * 0.9f,
                _buttonActive = FuThemeManager.GetColor(FuColors.BackgroundDanger) * 0.8f,
                _disabledButton = FuThemeManager.GetColor(FuColors.BackgroundDanger) * 0.5f,
                TextStyle = FuTextStyle.Default
            };
            // info
            _infoButtonStyle = new FuButtonStyle()
            {
                _framePadding = new Vector2(8f, 4f),
                Button = FuThemeManager.GetColor(FuColors.BackgroundInfo),
                _buttonHovered = FuThemeManager.GetColor(FuColors.BackgroundInfo) * 0.9f,
                _buttonActive = FuThemeManager.GetColor(FuColors.BackgroundInfo) * 0.8f,
                _disabledButton = FuThemeManager.GetColor(FuColors.BackgroundInfo) * 0.5f,
                TextStyle = FuTextStyle.Default
            };
            // success
            _successButtonStyle = new FuButtonStyle()
            {
                _framePadding = new Vector2(8f, 4f),
                Button = FuThemeManager.GetColor(FuColors.BackgroundSuccess),
                _buttonHovered = FuThemeManager.GetColor(FuColors.BackgroundSuccess) * 0.9f,
                _buttonActive = FuThemeManager.GetColor(FuColors.BackgroundSuccess) * 0.8f,
                _disabledButton = FuThemeManager.GetColor(FuColors.BackgroundSuccess) * 0.5f,
                TextStyle = FuTextStyle.Default
            };
            // warning
            _warningButtonStyle = new FuButtonStyle()
            {
                _framePadding = new Vector2(8f, 4f),
                Button = FuThemeManager.GetColor(FuColors.BackgroundWarning),
                _buttonHovered = FuThemeManager.GetColor(FuColors.BackgroundWarning) * 0.9f,
                _buttonActive = FuThemeManager.GetColor(FuColors.BackgroundWarning) * 0.8f,
                _disabledButton = FuThemeManager.GetColor(FuColors.BackgroundWarning) * 0.5f,
                TextStyle = FuTextStyle.Default
            };
        }
    }
}