using Fu.Core;
using ImGuiNET;
using System;
using UnityEngine;

namespace Fu.Framework
{
    public struct FuModalButton
    {
        public string Text;
        public Action Callback;
        public FuButtonStyle Style;
        public FuKeysCode KeyCodeExecute;

        public FuModalButton(string text)
        {
            Text = text;
            Callback = null;
            Style = FuButtonStyle.Default;
            KeyCodeExecute = FuKeysCode.None;
        }

        public FuModalButton(string text, FuKeysCode keyCodeExecute)
        {
            Text = text;
            Callback = null;
            Style = FuButtonStyle.Default;
            KeyCodeExecute = keyCodeExecute;
        }

        public FuModalButton(string text, Action callback, FuButtonStyle style)
        {
            Text = text;
            Callback = callback;
            Style = style;
            KeyCodeExecute = FuKeysCode.None;
        }

        public FuModalButton(string text, Action callback, FuButtonStyle style, FuKeysCode keyCodeExecute)
        {
            Text = text;
            Callback = callback;
            Style = style;
            KeyCodeExecute = keyCodeExecute;
        }

        public void Draw(FuLayout layout)
        {
            if (layout.Button(Text, FuElementSize.AutoSize, Style))
            {
                Callback?.Invoke();
                Fugui.CloseModal();
            }
            else if(KeyCodeExecute != FuKeysCode.None && Fugui.MainContainer.Keyboard.GetKeyDown(KeyCodeExecute))
            {
                Callback?.Invoke();
                Fugui.CloseModal();
            }
        }

        public Vector2 GetButtonSize()
        {
            Vector2 framePadding = new Vector2(6f, 4f) * Fugui.CurrentContext.Scale;
            return ImGui.CalcTextSize(Text) + (framePadding * 2f);
        }

        public void SetStyle(FuButtonStyle style)
        {
            Style = style;
        }
    }
}