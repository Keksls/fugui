using Fu;
using ImGuiNET;
using System;
using UnityEngine;

namespace Fu.Framework
{
    /// <summary>
    /// Represents the Fu Modal Button data structure.
    /// </summary>
    public struct FuModalButton
    {
        #region State
        public string Text;
        public Action Callback;
        public FuButtonStyle Style;
        public FuKeysCode KeyCodeExecute;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the Fu Modal Button class.
        /// </summary>
        /// <param name="text">The text value.</param>
        public FuModalButton(string text)
        {
            Text = text;
            Callback = null;
            Style = FuButtonStyle.Default;
            KeyCodeExecute = FuKeysCode.None;
        }

        /// <summary>
        /// Initializes a new instance of the Fu Modal Button class.
        /// </summary>
        /// <param name="text">The text value.</param>
        /// <param name="keyCodeExecute">The key Code Execute value.</param>
        public FuModalButton(string text, FuKeysCode keyCodeExecute)
        {
            Text = text;
            Callback = null;
            Style = FuButtonStyle.Default;
            KeyCodeExecute = keyCodeExecute;
        }

        /// <summary>
        /// Initializes a new instance of the Fu Modal Button class.
        /// </summary>
        /// <param name="text">The text value.</param>
        /// <param name="callback">The callback value.</param>
        /// <param name="style">The style value.</param>
        public FuModalButton(string text, Action callback, FuButtonStyle style)
        {
            Text = text;
            Callback = callback;
            Style = style;
            KeyCodeExecute = FuKeysCode.None;
        }

        /// <summary>
        /// Initializes a new instance of the Fu Modal Button class.
        /// </summary>
        /// <param name="text">The text value.</param>
        /// <param name="callback">The callback value.</param>
        /// <param name="style">The style value.</param>
        /// <param name="keyCodeExecute">The key Code Execute value.</param>
        public FuModalButton(string text, Action callback, FuButtonStyle style, FuKeysCode keyCodeExecute)
        {
            Text = text;
            Callback = callback;
            Style = style;
            KeyCodeExecute = keyCodeExecute;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Draws the value.
        /// </summary>
        /// <param name="layout">The layout value.</param>
        public void Draw(FuLayout layout)
        {
            if (layout.Button(Text, FuElementSize.AutoSize, Style))
            {
                Callback?.Invoke();
                Fugui.CloseModal();
            }
            else if(KeyCodeExecute != FuKeysCode.None && Fugui.DefaultContainer.Keyboard.GetKeyDown(KeyCodeExecute))
            {
                Callback?.Invoke();
                Fugui.CloseModal();
            }
        }

        /// <summary>
        /// Gets the button size.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public Vector2 GetButtonSize()
        {
            Vector2 framePadding = new Vector2(6f, 4f) * Fugui.CurrentContext.Scale;
            return ImGui.CalcTextSize(Text) + (framePadding * 2f);
        }

        /// <summary>
        /// Sets the style.
        /// </summary>
        /// <param name="style">The style value.</param>
        public void SetStyle(FuButtonStyle style)
        {
            Style = style;
        }
        #endregion
    }
}