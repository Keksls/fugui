using System.Runtime.CompilerServices;

namespace Fugui.Framework
{
    public struct UICheckboxStyle : IUIElementStyle
    {
        internal UIFrameStyle Unchecked;
        internal UIFrameStyle Checked;

        #region Pressets
        // default button style
        static UICheckboxStyle _defaultCheckboxStyle;
        public static UICheckboxStyle Default { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _defaultCheckboxStyle; } }
        #endregion

        /// <summary>
        /// Pushes the style for the Frame elements.
        /// </summary>
        public void Push(bool enabled)
        {
        }

        /// <summary>
        /// Pushes the style for the Frame elements.
        /// </summary>
        public void Push(bool enabled, bool isChecked)
        {
            if(isChecked)
            {
                Checked.Push(enabled);
            }
            else
            {
                Unchecked.Push(enabled);
            }
        }

        /// <summary>
        /// Pops the style for the Frame elements.
        /// </summary>
        public void Pop()
        {
            Unchecked.Pop();
        }

        /// <summary>
        /// Event rised whenever a them is set.
        /// Use this event to set static presset according to UIThemeManager.CurrentTheme
        /// </summary>
        private static void OnThemeSet()
        {
            // default button style
            _defaultCheckboxStyle = new UICheckboxStyle()
            {
                Unchecked = UIFrameStyle.Default,
                Checked = UIFrameStyle.Checked
            };
        }
    }
}