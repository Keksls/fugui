using System.Runtime.CompilerServices;

namespace Fugui.Framework
{
    public struct UIButtonsGroupStyle : IUIElementStyle
    {
        public UIButtonStyle ButtonStyle { get; private set; }
        public UIButtonStyle SelectedButtonStyle { get; private set; }

        #region Pressets
        static UIButtonsGroupStyle defaultButtonsGroupStyle;
        public static UIButtonsGroupStyle Default { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return defaultButtonsGroupStyle; } }
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
            defaultButtonsGroupStyle = new UIButtonsGroupStyle()
            {
                ButtonStyle = UIButtonStyle.Default,
                SelectedButtonStyle = UIButtonStyle.Selected
            };
        }
    }
}