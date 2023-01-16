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
        internal Color CheckMark;
        internal Color Border;
        internal Color Shadow;
        // disabled
        internal Color DisabledFrame;
        internal Color DisabledCheckMark;
        internal Color DisabledBorder;
        internal Color DisabledShadow;
        // text
        internal UITextStyle TextStyle;

        #region Pressets
        // default button style
        static UIFrameStyle _defaultFrameStyle;
        public static UIFrameStyle Default { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _defaultFrameStyle; } }
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
                FuGui.Push(ImGuiCol.CheckMark, CheckMark);
                FuGui.Push(ImGuiCol.Border, Border);
                FuGui.Push(ImGuiCol.BorderShadow, Shadow);
            }
            else
            {
                FuGui.Push(ImGuiCol.FrameBg, DisabledFrame);
                FuGui.Push(ImGuiCol.FrameBgHovered, DisabledFrame);
                FuGui.Push(ImGuiCol.FrameBgActive, DisabledFrame);
                FuGui.Push(ImGuiCol.CheckMark, DisabledCheckMark);
                FuGui.Push(ImGuiCol.Border, DisabledBorder);
                FuGui.Push(ImGuiCol.BorderShadow, DisabledShadow);
            }
            TextStyle.Push(enabled);
        }

        /// <summary>
        /// Pops the style for the Frame elements.
        /// </summary>
        public void Pop()
        {
            FuGui.PopColor(6);
            TextStyle.Pop();
        }

        /// <summary>
        /// Event rised whenever a them is set.
        /// Use this event to set static presset according to UIThemeManager.CurrentTheme
        /// </summary>
        private static void OnThemeSet()
        {
            // default button style
            _defaultFrameStyle = new UIFrameStyle()
            {
                Frame = ThemeManager.GetColor(ImGuiCol.FrameBg),
                HoveredFrame = ThemeManager.GetColor(ImGuiCol.FrameBgHovered),
                ActiveFrame = ThemeManager.GetColor(ImGuiCol.FrameBgActive),
                CheckMark = ThemeManager.GetColor(ImGuiCol.CheckMark),
                Border = ThemeManager.GetColor(ImGuiCol.Border),
                Shadow = ThemeManager.GetColor(ImGuiCol.BorderShadow),

                DisabledFrame = ThemeManager.GetColor(ImGuiCol.FrameBg) * 0.3f,
                DisabledCheckMark = ThemeManager.GetColor(ImGuiCol.CheckMark) * 0.3f,
                DisabledBorder = ThemeManager.GetColor(ImGuiCol.Border),
                DisabledShadow = ThemeManager.GetColor(ImGuiCol.BorderShadow),

                TextStyle = UITextStyle.Default
            };
        }
    }
}