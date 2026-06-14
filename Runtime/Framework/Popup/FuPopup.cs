using ImGuiNET;
using System;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Represents the Fugui type.
    /// </summary>
    public static partial class Fugui
    {
        #region State
        private static bool _showPopup = false;
        private static string _popupMessage = string.Empty;
        private static Vector2 _popupSize = Vector2.one;
        private static IFuWindowContainer _popupContainer = null;
        private static Action _popupUI = null;
        private static bool _popupAllowMouseInputs = false;
        private static bool _popupCloseOnEscape = false;
        #endregion

        #region Methods
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
            _popupAllowMouseInputs = false;
            _popupCloseOnEscape = false;

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
            ShowPopupMessage(UI, size, true, false, container);
        }

        /// <summary>
        /// Show a Popup Message with a custom UI
        /// </summary>
        /// <param name="UI"> Action to render the custom UI in the popup</param>
        /// <param name="size"> Size of the popup window</param>
        /// <param name="allowMouseInputs">Whether the custom popup UI can receive mouse inputs.</param>
        /// <param name="container"> container to show the message in (Main Container if null)</param>
        public static void ShowPopupMessage(Action UI, Vector2 size, bool allowMouseInputs, IFuWindowContainer container = null)
        {
            ShowPopupMessage(UI, size, allowMouseInputs, false, container);
        }

        /// <summary>
        /// Show a Popup Message with a custom UI
        /// </summary>
        /// <param name="UI"> Action to render the custom UI in the popup</param>
        /// <param name="size"> Size of the popup window</param>
        /// <param name="allowMouseInputs">Whether the custom popup UI can receive mouse inputs.</param>
        /// <param name="closeOnEscape">Whether ImGui close requests such as Escape close the popup. Leave false to close only through ClosePopupMessage.</param>
        /// <param name="container"> container to show the message in (Main Container if null)</param>
        public static void ShowPopupMessage(Action UI, Vector2 size, bool allowMouseInputs, bool closeOnEscape, IFuWindowContainer container = null)
        {
            if (container == null)
            {
                container = DefaultContainer;
            }
            _popupContainer = container;
            _showPopup = true;
            _popupUI = UI;
            _popupSize = size * CurrentContext.Scale;
            _popupAllowMouseInputs = allowMouseInputs;
            _popupCloseOnEscape = closeOnEscape;
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
            bool usePopupBackdrop = Fugui.ShouldUseThemeBackdrop(FuColors.PopupBg, 0.98f);
            Fugui.Push(ImGuiCol.PopupBg, Fugui.GetColor(FuColors.PopupBg, usePopupBackdrop ? 0f : 1f));
            ImGuiWindowFlags popupFlags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoResize;
            if (!_popupAllowMouseInputs)
            {
                popupFlags |= ImGuiWindowFlags.NoMouseInputs;
            }
            if (usePopupBackdrop)
            {
                popupFlags |= ImGuiWindowFlags.NoBackground;
            }

            if (ImGui.BeginPopupModal("FuguiPopupMessage", ref open, popupFlags))
            {
                Fugui.RegisterSurface(
                    _popupContainer,
                    "FuguiPopupMessage",
                    FuSurfaceType.Modal,
                    FuLayer.Top,
                    null,
                    new Rect(ImGui.GetWindowPos(), ImGui.GetWindowSize()),
                    true,
                    _popupAllowMouseInputs);
                Fugui.BeginModalSurfaceDrawing(_popupAllowMouseInputs);
                try
                {
                    ImGui.SetWindowFocus();
                    if (usePopupBackdrop)
                    {
                        Fugui.DrawCurrentWindowThemeBackdrop(FuColors.PopupBg, 0.98f);
                    }
                    _popupUI?.Invoke();
                }
                finally
                {
                    Fugui.EndModalSurfaceDrawing();
                    ImGui.EndPopup();
                }
            }
            if (_popupCloseOnEscape && !open)
            {
                ClosePopupMessage();
            }
            Fugui.PopColor();
            PopStyle();
        }
        #endregion
    }
}
