using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework
{
    public struct FuModalButton
    {
        public string text;
        public Action callback;
        public FuButtonStyle style;

        public FuModalButton(string text, Action callback, FuButtonStyle style)
        {
            this.text = text;
            this.callback = callback;
            this.style = style;
        }

        public void Draw(FuLayout layout)
        {
            if (layout.Button(text, GetButtonSize(), style))
            {
                callback();
            }
        }

        public Vector2 GetButtonSize()
        {
            Vector2 framePadding = FuThemeManager.CurrentTheme.FramePadding;
            return ImGui.CalcTextSize(text) + (framePadding * 2f);
        }
    }
}