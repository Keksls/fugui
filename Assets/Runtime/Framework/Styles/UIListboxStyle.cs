using ImGuiNET;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fugui.Framework
{
    public struct UIListboxStyle : IUIElementStyle
    {
        public UIButtonStyle ButtonStyle { get; private set; }
        // enabled
        private Color _frame;
        private Color _frameHovered;
        private Color _frameActive;
        // disabled
        private Color _frameDisabled;
        // additional styles
        public UITextStyle TextStyle { get; private set; }

        #region Pressets
        // default button style
        static UIListboxStyle defaultListboxStyle;
        public static UIListboxStyle Default { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return defaultListboxStyle; } }
        #endregion

        /// <summary>
        /// Pushes the style for the combobox element.
        /// </summary>
        public void Push(bool enabled)
        {
            ButtonStyle.Push(enabled);
            if (enabled)
            {
                FuGui.Push(ImGuiCol.FrameBg, _frame);
                FuGui.Push(ImGuiCol.FrameBgHovered, _frameHovered);
                FuGui.Push(ImGuiCol.FrameBgActive, _frameActive);
            }
            else
            {
                FuGui.Push(ImGuiCol.FrameBg, _frameDisabled);
                FuGui.Push(ImGuiCol.FrameBgHovered, _frameDisabled);
                FuGui.Push(ImGuiCol.FrameBgActive, _frameDisabled);
            }
            TextStyle.Push(enabled); // push the text style onto the stack
        }

        /// <summary>
        /// Pops the style for the combobox element.
        /// </summary>
        public void Pop()
        {
            TextStyle.Pop(); // pop the text style off the stack
            FuGui.PopColor(3);
            ButtonStyle.Pop();
        }

        /// <summary>
        /// Event rised whenever a them is set.
        /// Use this event to set static presset according to UIThemeManager.CurrentTheme
        /// </summary>
        private static void OnThemeSet()
        {
            // default button style
            defaultListboxStyle = new UIListboxStyle()
            {
                ButtonStyle = UIButtonStyle.Default,
                _frame = ImGui.GetStyle().Colors[(int)ImGuiCol.Button],
                _frameHovered = ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonHovered],
                _frameActive = ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive],
                _frameDisabled = ImGui.GetStyle().Colors[(int)ImGuiCol.Button] / 2f,
                TextStyle = UITextStyle.Default
            };
        }
    }
}