using Fu.Core;
using ImGuiNET;
using UnityEngine;

namespace Fu
{
    public static partial class Fugui
    {
        private static bool _showPopup = false;
        private static string _popupMessage = string.Empty;
        private static Vector2 _popupSize = Vector2.one;
        private static IFuWindowContainer _popupContainer = null;

        /// <summary>
        /// Show a Popup Message
        /// </summary>
        /// <param name="message">message to show</param>
        /// <param name="container">container to show the message in (Main Container if null)</param>
        public static void ShowPopupMessage(string message, IFuWindowContainer container = null)
        {
            if (container == null)
            {
                container = MainContainer;
            }
            _popupContainer = container;
            _popupMessage = message;
            _showPopup = true;
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
                ImGui.Text(_popupMessage);
                _popupSize = ImGui.CalcTextSize(_popupMessage) + new Vector2(32f, 32f) * CurrentContext.Scale;
                ImGui.EndPopup();
            }
            PopStyle();
        }
    }
}