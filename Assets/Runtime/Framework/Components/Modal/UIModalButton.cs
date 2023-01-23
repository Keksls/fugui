using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fugui.Framework
{
    public struct UIModalButton
    {
        public string text;
        public Action callback;
        public UIButtonStyle style;

        public UIModalButton(string text, Action callback, UIButtonStyle style)
        {
            this.text = text;
            this.callback = callback;
            this.style = style;
        }

        public void Draw(UILayout layout)
        {
            if (layout.Button(text, GetButtonSize(), style))
            {
                callback();
            }
        }

        public Vector2 GetButtonSize()
        {
            Vector2 framePadding = ThemeManager.CurrentTheme.FramePadding;
            return ImGui.CalcTextSize(text) + (framePadding * 2f);
        }
    }
}