using ImGuiNET;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fu.Framework
{
    public struct FuFrameStyle : IFuElementStyle
    {
        // enabled
        public Color Frame;
        public Color HoveredFrame;
        public Color ActiveFrame;
        public Color CheckMark;
        public Color Border;
        public Color Shadow;
        // disabled
        public Color DisabledFrame;
        public Color DisabledCheckMark;
        public Color DisabledBorder;
        public Color DisabledShadow;
        // text
        public FuTextStyle TextStyle;

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
                Frame = Fugui.Themes.GetColor(FuColors.FrameBg),
                HoveredFrame = Fugui.Themes.GetColor(FuColors.FrameBgHovered),
                ActiveFrame = Fugui.Themes.GetColor(FuColors.FrameBgActive),
                CheckMark = Fugui.Themes.GetColor(FuColors.CheckMark),
                Border = Fugui.Themes.GetColor(FuColors.Border),
                Shadow = Fugui.Themes.GetColor(FuColors.BorderShadow),

                DisabledFrame = Fugui.Themes.GetColor(FuColors.FrameBg) * 0.3f,
                DisabledCheckMark = Fugui.Themes.GetColor(FuColors.CheckMark) * 0.3f,
                DisabledBorder = Fugui.Themes.GetColor(FuColors.Border),
                DisabledShadow = Fugui.Themes.GetColor(FuColors.BorderShadow),

                TextStyle = FuTextStyle.Default
            };
        }
    }
}