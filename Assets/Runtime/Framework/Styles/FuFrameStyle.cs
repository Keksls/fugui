using ImGuiNET;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fu.Framework
{
    public struct FuFrameStyle : IFuElementStyle
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
        internal FuTextStyle TextStyle;

        #region Pressets
        // default button style
        static FuFrameStyle _defaultFrameStyle;
        public static FuFrameStyle Default { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _defaultFrameStyle; } }
        #endregion

        /// <summary>
        /// Pushes the style for the Frame elements.
        /// </summary>
        public void Push(bool enabled)
        {
            if (enabled)
            {
                Fugui.Push(ImGuiCol.FrameBg, Frame);
                Fugui.Push(ImGuiCol.FrameBgHovered, HoveredFrame);
                Fugui.Push(ImGuiCol.FrameBgActive, ActiveFrame);
                Fugui.Push(ImGuiCol.CheckMark, CheckMark);
                Fugui.Push(ImGuiCol.Border, Border);
                Fugui.Push(ImGuiCol.BorderShadow, Shadow);
            }
            else
            {
                Fugui.Push(ImGuiCol.FrameBg, DisabledFrame);
                Fugui.Push(ImGuiCol.FrameBgHovered, DisabledFrame);
                Fugui.Push(ImGuiCol.FrameBgActive, DisabledFrame);
                Fugui.Push(ImGuiCol.CheckMark, DisabledCheckMark);
                Fugui.Push(ImGuiCol.Border, DisabledBorder);
                Fugui.Push(ImGuiCol.BorderShadow, DisabledShadow);
            }
            TextStyle.Push(enabled);
        }

        /// <summary>
        /// Pops the style for the Frame elements.
        /// </summary>
        public void Pop()
        {
            Fugui.PopColor(6);
            TextStyle.Pop();
        }

        /// <summary>
        /// Event rised whenever a them is set.
        /// Use this event to set static presset according to UIThemeManager.CurrentTheme
        /// </summary>
        private static void OnThemeSet()
        {
            // default button style
            _defaultFrameStyle = new FuFrameStyle()
            {
                Frame = FuThemeManager.GetColor(FuColors.FrameBg),
                HoveredFrame = FuThemeManager.GetColor(FuColors.FrameBgHovered),
                ActiveFrame = FuThemeManager.GetColor(FuColors.FrameBgActive),
                CheckMark = FuThemeManager.GetColor(FuColors.CheckMark),
                Border = FuThemeManager.GetColor(FuColors.Border),
                Shadow = FuThemeManager.GetColor(FuColors.BorderShadow),

                DisabledFrame = FuThemeManager.GetColor(FuColors.FrameBg) * 0.3f,
                DisabledCheckMark = FuThemeManager.GetColor(FuColors.CheckMark) * 0.3f,
                DisabledBorder = FuThemeManager.GetColor(FuColors.Border),
                DisabledShadow = FuThemeManager.GetColor(FuColors.BorderShadow),

                TextStyle = FuTextStyle.Default
            };
        }
    }
}