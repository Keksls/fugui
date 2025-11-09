using ImGuiNET;
using System;
using UnityEngine;

namespace Fu
{
    public static partial class Fugui
    {
        private static bool _showPopup = false;
        private static string _popupMessage = string.Empty;
        private static Vector2 _popupSize = Vector2.one;
        private static IFuWindowContainer _popupContainer = null;
        private static Action _popupUI = null;

        /// <summary>
        /// Show a Popup Message
        /// </summary>
        /// <param name="message">message to show</param>
        /// <param name="container">container to show the message in (Main Container if null)</param>
        public static void ShowPopupMessage(string message, IFuWindowContainer container = null)
        {
            if (container == null)
            {
                container = DefaultContainer;
            }
            _popupContainer = container;
            _popupMessage = message;
            _showPopup = true;

            _popupUI = () =>
            {
                ImGui.Text(_popupMessage);
                _popupSize = ImGui.CalcTextSize(_popupMessage) + new Vector2(32f, 32f) * CurrentContext.Scale;
            };
        }

        /// <summary>
        /// Show a Popup Message with a custom UI
        /// </summary>
        /// <param name="UI"> Action to render the custom UI in the popup</param>
        /// <param name="size"> Size of the popup window</param>
        /// <param name="container"> container to show the message in (Main Container if null)</param>
        public static void ShowPopupMessage(Action UI, Vector2 size, IFuWindowContainer container = null)
        {
            if (container == null)
            {
                container = DefaultContainer;
            }
            _popupContainer = container;
            _showPopup = true;
            _popupUI = UI;
            _popupSize = size * CurrentContext.Scale;
        }

        /// <summary>
        /// Close the popup message
        /// </summary>
        public static void ClosePopupMessage()
        {
            _showPopup = false;
        }

        /// <summary>
        /// Render the Popup message
        /// </summary>
        public static void RenderPopupMessage()
        {
            if (!_showPopup)
            {
                return;
            }

            ImGui.OpenPopup("FuguiPopupMessage");
            bool open = true;
            Push(ImGuiStyleVar.WindowPadding, new Vector2(8f, 8f) * Fugui.CurrentContext.Scale);
            ImGui.SetNextWindowPos(new Vector2(_popupContainer.Size.x / 2f - _popupSize.x / 2f, _popupContainer.Size.y / 2f - _popupSize.y / 2f), ImGuiCond.Always);
            ImGui.SetNextWindowSize(_popupSize, ImGuiCond.Always);
            if (ImGui.BeginPopupModal("FuguiPopupMessage", ref open, ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMouseInputs | ImGuiWindowFlags.NoResize))
            {
                ImGui.SetWindowFocus();
                _popupUI?.Invoke();
                ImGui.EndPopup();
            }
            PopStyle();
        }
    }
}