using Fu.Core;
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
        /// <summary>
        /// Default frame style, use 'Frame', 'Border' and 'CheckMark' theme colors
        /// </summary>
        public static FuFrameStyle Default { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _defaultFrameStyle; } }
        #endregion

        /// <summary>
        /// Pushes the style for the Frame elements.
        /// </summary>
        public void Push(bool enabled)
        {
            if (enabled)
            {
                Fugui.Push(ImGuiCols.FrameBg, Frame);
                Fugui.Push(ImGuiCols.FrameBgHovered, HoveredFrame);
                Fugui.Push(ImGuiCols.FrameBgActive, ActiveFrame);
                Fugui.Push(ImGuiCols.CheckMark, CheckMark);
                Fugui.Push(ImGuiCols.Border, Border);
                Fugui.Push(ImGuiCols.BorderShadow, Shadow);
            }
            else
            {
                Fugui.Push(ImGuiCols.FrameBg, DisabledFrame);
                Fugui.Push(ImGuiCols.FrameBgHovered, DisabledFrame);
                Fugui.Push(ImGuiCols.FrameBgActive, DisabledFrame);
                Fugui.Push(ImGuiCols.CheckMark, DisabledCheckMark);
                Fugui.Push(ImGuiCols.Border, DisabledBorder);
                Fugui.Push(ImGuiCols.BorderShadow, DisabledShadow);
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