using ImGuiNET;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fugui.Framework
{
    public struct UISliderStyle : IUIElementStyle
    {
        // enabled
        internal Color Line;
        internal Color Knob;
        // disabled
        internal Color DisabledLine;
        internal Color DisabledKnob;
        // text
        internal UIFrameStyle Frame;

        #region Pressets
        // default button style
        static UISliderStyle defaultSliderStyle;
        public static UISliderStyle Default { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return defaultSliderStyle; } }
        #endregion

        public void Push(bool enabled)
        {
            Frame.Push(enabled);
        }

        public void Pop()
        {
            Frame.Pop();
        }

        /// <summary>
        /// Event rised whenever a them is set.
        /// Use this event to set static presset according to UIThemeManager.CurrentTheme
        /// </summary>
        private static void OnThemeSet()
        {
            defaultSliderStyle = new UISliderStyle()
            {
                Line = ThemeManager.GetColor(ImGuiCol.CheckMark),
                Knob = ThemeManager.GetColor(ImGuiCol.Text),
                DisabledLine = ThemeManager.GetColor(ImGuiCol.Text) * 0.5f,
                DisabledKnob = ThemeManager.GetColor(ImGuiCol.Text) * 0.6f,
                Frame = UIFrameStyle.Default
            };
        }
    }
}