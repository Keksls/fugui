using ImGuiNET;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fu.Framework
{
    public struct FuListboxStyle : IFuElementStyle
    {
        public FuButtonStyle ButtonStyle { get; private set; }
        // enabled
        private Color _frame;
        private Color _frameHovered;
        private Color _frameActive;
        // disabled
        private Color _frameDisabled;
        // additional styles
        public FuTextStyle TextStyle { get; private set; }

        #region Pressets
        // default button style
        static FuListboxStyle defaultListboxStyle;
        public static FuListboxStyle Default { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return defaultListboxStyle; } }
        #endregion

        /// <summary>
        /// Pushes the style for the combobox element.
        /// </summary>
        public void Push(bool enabled)
        {
            ButtonStyle.Push(enabled);
            if (enabled)
            {
                Fugui.Push(ImGuiCol.FrameBg, _frame);
                Fugui.Push(ImGuiCol.FrameBgHovered, _frameHovered);
                Fugui.Push(ImGuiCol.FrameBgActive, _frameActive);
            }
            else
            {
                Fugui.Push(ImGuiCol.FrameBg, _frameDisabled);
                Fugui.Push(ImGuiCol.FrameBgHovered, _frameDisabled);
                Fugui.Push(ImGuiCol.FrameBgActive, _frameDisabled);
            }
            TextStyle.Push(enabled); // push the text style onto the stack
        }

        /// <summary>
        /// Pops the style for the combobox element.
        /// </summary>
        public void Pop()
        {
            TextStyle.Pop(); // pop the text style off the stack
            Fugui.PopColor(3);
            ButtonStyle.Pop();
        }

        /// <summary>
        /// Event rised whenever a them is set.
        /// Use this event to set static presset according to UIThemeManager.CurrentTheme
        /// </summary>
        private static void OnThemeSet()
        {
            // default button style
            defaultListboxStyle = new FuListboxStyle()
            {
                ButtonStyle = FuButtonStyle.Default,
                _frame = ImGui.GetStyle().Colors[(int)ImGuiCol.Button],
                _frameHovered = ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonHovered],
                _frameActive = ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive],
                _frameDisabled = ImGui.GetStyle().Colors[(int)ImGuiCol.Button] / 2f,
                TextStyle = FuTextStyle.Default
            };
        }
    }
}