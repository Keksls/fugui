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

        public FuModalButton(string text, Action callback, FuButtonStyle style)
        {
            this.Text = text;
            this.Callback = callback;
            this.Style = style;
        }

        public void Draw(FuLayout layout)
        {
            if (layout.Button(Text, GetButtonSize(), Style))
            {
                Callback?.Invoke();
                Fugui.CloseModal();
            }
        }

        public Vector2 GetButtonSize()
        {
            Vector2 framePadding = FuThemeManager.CurrentTheme.FramePadding;
            return ImGui.CalcTextSize(Text) + (framePadding * 2f);
        }

        public void SetStyle(FuButtonStyle style)
        {
            Style = style;
        }
    }
}