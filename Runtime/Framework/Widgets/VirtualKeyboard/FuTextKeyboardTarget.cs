using System;

namespace Fu.Framework
{
    /// <summary>
    /// A string a <see cref="FuVirtualKeyboard"/> types in, read and written through two delegates: typically the
    /// string a text input of the same window shows. The characters go at the end of the string.
    /// </summary>
    /// <remarks>
    /// In the window of the keyboard itself, a click on a key takes the focus from the text input: the input then shows
    /// the string as the keyboard changed it. To type in a text input that keeps its focus (a keyboard in another
    /// context), use <see cref="FuContextKeyboardTarget"/>.
    /// </remarks>
    public class FuTextKeyboardTarget : IFuVirtualKeyboardTarget
    {
        #region State
        private readonly Func<string> _getter;
        private readonly Action<string> _setter;
        private readonly int _maxLength;
        private readonly Action _onSubmit;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a target over a string.
        /// </summary>
        /// <param name="getter">Reads the string.</param>
        /// <param name="setter">Writes the string.</param>
        /// <param name="maxLength">Longest string the target takes.</param>
        /// <param name="onSubmit">Called when Enter is pressed, or null.</param>
        public FuTextKeyboardTarget(Func<string> getter, Action<string> setter, int maxLength = int.MaxValue, Action onSubmit = null)
        {
            _getter = getter ?? throw new ArgumentNullException(nameof(getter));
            _setter = setter ?? throw new ArgumentNullException(nameof(setter));
            _maxLength = Math.Max(0, maxLength);
            _onSubmit = onSubmit;
        }
        #endregion

        #region Methods
        /// <inheritdoc/>
        public void Insert(string text)
        {
            string value = (_getter() ?? string.Empty) + text;
            _setter(value.Length > _maxLength ? value.Substring(0, _maxLength) : value);
        }

        /// <inheritdoc/>
        public void Backspace()
        {
            string value = _getter() ?? string.Empty;

            if (value.Length > 0)
            {
                _setter(value.Substring(0, value.Length - 1));
            }
        }

        /// <inheritdoc/>
        public void Submit()
        {
            _onSubmit?.Invoke();
        }
        #endregion
    }
}
