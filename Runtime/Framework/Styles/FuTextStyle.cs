using ImGuiNET;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fu.Framework
{
    public struct FuTextStyle : IFuElementStyle
    {
        // enabled
        internal Color Text;
        // enabled
        internal Color LinkText;
        // disabled
        internal Color DisabledText;

        #region Pressets
        static FuTextStyle _defaultTextStyle;
        /// <summary>
        /// Default text style, use 'Text' theme colors
        /// </summary>
        public static FuTextStyle Default { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _defaultTextStyle; } }

        static FuTextStyle _defaultDeactivated;
        /// <summary>
        /// Default text style, use 'Text' theme colors
        /// </summary>
        public static FuTextStyle Deactivated { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _defaultDeactivated; } }
        
        static FuTextStyle _selectedTextStyle;
        /// <summary>
        /// Selected text style, use 'SelectedText' theme colors
        /// </summary>
        public static FuTextStyle Selected { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _selectedTextStyle; } }
       
        static FuTextStyle _highlightTextStyle;
        /// <summary>
        /// Highlight text style, use 'HighlightText' theme colors
        /// </summary>
        public static FuTextStyle Highlight { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _highlightTextStyle; } }
        
        static FuTextStyle _infoTextStyle;
        /// <summary>
        /// Info text style, use 'InfoText' theme colors
        /// </summary>
        public static FuTextStyle Info { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _infoTextStyle; } }
        
        static FuTextStyle _warningTextStyle;
        /// <summary>
        /// Warning text style, use 'WarningText' theme colors
        /// </summary>
        public static FuTextStyle Warning { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _warningTextStyle; } }
        
        static FuTextStyle _dangerTextStyle;
        /// <summary>
        /// Danger text style, use 'DangerText' theme colors
        /// </summary>
        public static FuTextStyle Danger { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _dangerTextStyle; } }
        
        static FuTextStyle _successTextStyle;
        /// <summary>
        /// Success text style, use 'SuccessText' theme colors
        /// </summary>
        public static FuTextStyle Success { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _successTextStyle; } }
        #endregion

        public FuTextStyle(Color text, Color linkText, Color disabledText)
        {
            Text = text;
            LinkText = linkText;
            DisabledText = disabledText;
        }

        public void Push(bool enabled)
        {
            if (enabled)
            {
                Fugui.Push(ImGuiCol.Text, Text);
                Fugui.Push(ImGuiCol.TextLink, LinkText);
            }
            else
            {
                Fugui.Push(ImGuiCol.Text, DisabledText);
                Fugui.Push(ImGuiCol.TextLink, DisabledText);
            }
        }

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
            // default text style
            _defaultTextStyle = new FuTextStyle()
            {
                Text = FuThemeManager.GetColor(FuColors.Text),
                LinkText = FuThemeManager.GetColor(FuColors.TextLink),
                DisabledText = FuThemeManager.GetColor(FuColors.TextDisabled)
            };
            // default text style
            _defaultDeactivated = new FuTextStyle()
            {
                Text = FuThemeManager.GetColor(FuColors.Text) * 0.66f,
                LinkText = FuThemeManager.GetColor(FuColors.TextLink) * 0.66f,
                DisabledText = FuThemeManager.GetColor(FuColors.TextDisabled) * 0.66f
            };
            // selected text style
            _selectedTextStyle = new FuTextStyle()
            {
                Text = FuThemeManager.GetColor(FuColors.SelectedText),
                LinkText = FuThemeManager.GetColor(FuColors.TextLink),
                DisabledText = FuThemeManager.GetColor(FuColors.SelectedText) * 0.5f
            };
            // highlight text style
            _highlightTextStyle = new FuTextStyle()
            {
                Text = FuThemeManager.GetColor(FuColors.HighlightText),
                LinkText = FuThemeManager.GetColor(FuColors.TextLink),
                DisabledText = FuThemeManager.GetColor(FuColors.HighlightTextDisabled)
            };
            // highlight text style
            _successTextStyle = new FuTextStyle()
            {
                Text = FuThemeManager.GetColor(FuColors.TextSuccess),
                LinkText = FuThemeManager.GetColor(FuColors.TextLink),
                DisabledText = FuThemeManager.GetColor(FuColors.TextSuccess) * 0.5f
            };
            // highlight text style
            _dangerTextStyle = new FuTextStyle()
            {
                Text = FuThemeManager.GetColor(FuColors.TextDanger),
                LinkText = FuThemeManager.GetColor(FuColors.TextLink),
                DisabledText = FuThemeManager.GetColor(FuColors.TextDanger) * 0.5f
            };
            // highlight text style
            _infoTextStyle = new FuTextStyle()
            {
                Text = FuThemeManager.GetColor(FuColors.TextInfo),
                DisabledText = FuThemeManager.GetColor(FuColors.TextInfo) * 0.5f
            };
            // highlight text style
            _warningTextStyle = new FuTextStyle()
            {
                Text = FuThemeManager.GetColor(FuColors.TextWarning),
                LinkText = FuThemeManager.GetColor(FuColors.TextLink),
                DisabledText = FuThemeManager.GetColor(FuColors.TextWarning) * 0.5f
            };
        }
    }
}