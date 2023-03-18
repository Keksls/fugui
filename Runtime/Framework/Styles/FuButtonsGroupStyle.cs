using System.Runtime.CompilerServices;

namespace Fu.Framework
{
    public struct FuButtonsGroupStyle : IFuElementStyle
    {
        public FuButtonStyle ButtonStyle { get; private set; }
        public FuButtonStyle SelectedButtonStyle { get; private set; }

        #region Pressets
        static FuButtonsGroupStyle defaultButtonsGroupStyle;
        /// <summary>
        /// Default style of buttonGroups widgets
        /// unselected buttons are 'Default' button style
        /// selected buttons are 'Selected' button style
        /// </summary>
        public static FuButtonsGroupStyle Default { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return defaultButtonsGroupStyle; } }
        #endregion

        #region constructor
        public FuButtonsGroupStyle(FuButtonStyle buttonStyle, FuButtonStyle selectedButtonStyle)
        {
            ButtonStyle = buttonStyle;
            SelectedButtonStyle = selectedButtonStyle;
        }
        #endregion

        /// <summary>
        /// Pushes the UIButtonStyle onto the ImGui style stack. If enabled is true, the enabled style will be used. If enabled is false, the disabled style will be used.
        /// </summary>
        /// <param name="enabled">Determines whether to use the enabled or disabled style</param>
        public void Push(bool enabled)
        {
        }

        /// <summary>
        /// Pops the UIButtonStyle off the ImGui style stack.
        /// </summary>
        public void Pop()
        {
        }

        /// <summary>
        /// Event rised whenever a them is set.
        /// Use this event to set static presset according to UIThemeManager.CurrentTheme
        /// </summary>
        private static void OnThemeSet()
        {
            // default button style
            defaultButtonsGroupStyle = new FuButtonsGroupStyle()
            {
                ButtonStyle = FuButtonStyle.Default,
                SelectedButtonStyle = FuButtonStyle.Selected
            };
        }
    }
}