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
                Line = ThemeManager.GetColor(ImGuiCustomCol.SliderLine),
                Knob = ThemeManager.GetColor(ImGuiCol.CheckMark),
                DisabledLine = ThemeManager.GetColor(ImGuiCustomCol.SliderLineDisabled),
                DisabledKnob = ThemeManager.GetColor(ImGuiCustomCol.SliderKnobDisabled),
                Frame = UIFrameStyle.Default
            };
        }
    }
}