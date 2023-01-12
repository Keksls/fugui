using ImGuiNET;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fugui.Framework
{
    public struct UIFrameStyle : IUIElementStyle
    {
        // enabled
        internal Color Frame;
        internal Color HoveredFrame;
        internal Color ActiveFrame;
        // disabled
        internal Color DisabledFrame;
        internal Color DisabledHoveredFrame;
        internal Color DisabledActiveFrame;
        // text
        internal UITextStyle TextStyle;

        #region Pressets
        // default button style
        static UIFrameStyle defaultFrameStyle;
        public static UIFrameStyle Default { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return defaultFrameStyle; } }
        #endregion

        /// <summary>
        /// Pushes the style for the Frame elements.
        /// </summary>
        public void Push(bool enabled)
        {
            if (enabled)
            {
                FuGui.Push(ImGuiCol.FrameBg, Frame);
                FuGui.Push(ImGuiCol.FrameBgHovered, HoveredFrame);
                FuGui.Push(ImGuiCol.FrameBgActive, ActiveFrame);
            }
            else
            {
                FuGui.Push(ImGuiCol.FrameBg, DisabledFrame);
                FuGui.Push(ImGuiCol.FrameBgHovered, DisabledHoveredFrame);
                FuGui.Push(ImGuiCol.FrameBgActive, DisabledActiveFrame);
            }
            TextStyle.Push(enabled);
        }

        /// <summary>
        /// Pops the style for the Frame elements.
        /// </summary>
        public void Pop()
        {
            FuGui.PopColor(3);
            TextStyle.Pop();
        }

        /// <summary>
        /// Event rised whenever a them is set.
        /// Use this event to set static presset according to UIThemeManager.CurrentTheme
        /// </summary>
        private static void OnThemeSet()
        {
            // default button style
            defaultFrameStyle = new UIFrameStyle()
            {
                Frame = ThemeManager.GetColor(ImGuiCol.FrameBg),
                HoveredFrame = ThemeManager.GetColor(ImGuiCol.FrameBgHovered),
                ActiveFrame = ThemeManager.GetColor(ImGuiCol.FrameBgActive),

                DisabledFrame = ThemeManager.GetColor(ImGuiCol.FrameBg) * 0.3f,
                DisabledHoveredFrame = ThemeManager.GetColor(ImGuiCol.FrameBg) * 0.3f,
                DisabledActiveFrame = ThemeManager.GetColor(ImGuiCol.FrameBg) * 0.3f,

                TextStyle = UITextStyle.Default
            };
        }
    }
}