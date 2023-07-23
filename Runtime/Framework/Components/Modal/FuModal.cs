using Fu.Core;
using Fu.Framework;
using ImGuiNET;
using System;
using UnityEngine;

namespace Fu
{
    public static partial class Fugui
    {
        #region Variables
        private static bool _showModal = false;
        private static bool _preventCloseModal = false;
        private static string _modalTitle;
        private static Action _modalBody;
        private static FuModalButton[] _modalButtons;
        private static FuModalSize _currentModalSize;
        private static Vector2 _currentBodySize;
        private static float _currentBodyHeight = 0f;
        private static float _currentTitleHeight = 0f;
        private static float _currentFooterheight = 0f;
        private static Vector2 _currentModalPos;
        private static float _enlapsed = 0f;
        #endregion

        #region Show Hide
        /// <summary>
        /// Show a modal with a custom title, body, and buttons
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="body">Body of the modal</param>
        /// <param name="buttons">List of buttons in the modal, each button contains a text and callback</param>
        public static void ShowModal(string title, Action body, FuModalSize modalSize, params FuModalButton[] buttons)
        {
            _modalTitle = title; //store the title
            _modalBody = body; //store the body
            // add default button if needed
            if (buttons == null || buttons.Length == 0)
            {
                buttons = new FuModalButton[]
                {
                    new FuModalButton("OK", CloseModal, FuButtonStyle.Default)
                };
            }
            // set default button style if needed
            if (!Settings.StateModalsUseButtonColors)
            {
                foreach (var button in buttons)
                {
                    button.SetStyle(FuButtonStyle.Default);
                }
            }
            _modalButtons = buttons; //store the buttons
            _showModal = true; //set showModal to true to show the modal
            _currentModalSize = modalSize;
            _currentBodyHeight = 0f;
            _enlapsed = 0f;
            _currentTitleHeight = 0f;
            _currentFooterheight = 0f;
        }

        /// <summary>
        /// Hide the currently shown modal
        /// </summary>
        public static void CloseModal()
        {
            if (!_preventCloseModal)
            {
                _enlapsed = Settings.ModalAnimationDuration;
                _showModal = false; //set showModal to false to hide the modal
            }
            _preventCloseModal = false;
        }

        /// <summary>
        /// Prevent the modal to close on next CloseModal call
        /// </summary>
        public static void CancelNextModalClose()
        {
            _preventCloseModal = true;
        }
        #endregion

        #region Drawing
        /// <summary>
        /// Render the currently shown modal
        /// </summary>
        public static void RenderModal(IFuWindowContainer container)
        {
            if (_showModal)
            {
                ImGui.OpenPopup(_modalTitle); //open the modal with the stored title

                // claculate y padding
                float yPadding = FuThemeManager.CurrentTheme.FramePadding.y * 2f + FuThemeManager.CurrentTheme.WindowPadding.y * 2f;
                // calculate footer height
                if (_currentFooterheight == 0f)
                {
                    _currentFooterheight = _modalButtons[0].GetButtonSize().y + yPadding / 2f;
                }
                // calculate title height
                if (_currentTitleHeight == 0f)
                {
                    _currentTitleHeight = ImGui.CalcTextSize(_modalTitle).y + yPadding;
                }

                // calculate body size
                _currentBodySize = _currentModalSize.Size;
                if (_currentBodyHeight > 0f)
                {
                    _currentBodySize.y = _currentBodyHeight;
                }
                _currentBodySize.y = Mathf.Clamp(_currentBodySize.y, 32f, container.Size.y - 256f);

                // calculate full size and pos
                Vector2 modalSize = new Vector2(_currentBodySize.x, _currentBodySize.y + _currentFooterheight + _currentTitleHeight);
                _currentModalPos = new Vector2(container.Size.x / 2f - modalSize.x / 2f, container.Size.y / 2f - modalSize.y / 2f);
                Vector2 modalStartPos = new Vector2(container.Size.x / 2f - (modalSize.x * 0.01f) / 2f, 64f);
                if (_enlapsed < 1f)
                {
                    ImGui.SetNextWindowSize(Vector2.Lerp(modalSize * 0.01f, modalSize, _enlapsed / Settings.ModalAnimationDuration), ImGuiCond.Always);
                    ImGui.SetNextWindowPos(Vector2.Lerp(modalStartPos, _currentModalPos, _enlapsed / Settings.ModalAnimationDuration), ImGuiCond.Always);
                }
                else
                {
                    ImGui.SetNextWindowSize(modalSize, ImGuiCond.Always);
                    ImGui.SetNextWindowPos(_currentModalPos, ImGuiCond.Always);
                }
                ImGui.SetNextWindowFocus();
                // beggin modal
                if (ImGui.BeginPopupModal(_modalTitle, ref _showModal, ImGuiWindowFlags.Modal | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                {
                    // draw modal title
                    DrawTitle(_modalTitle);

                    ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                    // draw body BG
                    drawList.AddRectFilled(new Vector2(_currentModalPos.x, _currentModalPos.y + _currentTitleHeight), new Vector2(_currentModalPos.x + modalSize.x, _currentModalPos.y + _currentTitleHeight + _currentBodySize.y), ImGui.GetColorU32(FuThemeManager.GetColor(FuColors.WindowBg)));
                    // draw title line
                    drawList.AddLine(new Vector2(_currentModalPos.x, _currentModalPos.y + _currentTitleHeight), new Vector2(_currentModalPos.x + modalSize.x, _currentModalPos.y + _currentTitleHeight), ImGui.GetColorU32(FuThemeManager.GetColor(FuColors.Separator)));
                    // draw  footer line
                    drawList.AddLine(new Vector2(_currentModalPos.x, _currentModalPos.y + _currentTitleHeight + _currentBodySize.y), new Vector2(_currentModalPos.x + modalSize.x, _currentModalPos.y + _currentTitleHeight + _currentBodySize.y), ImGui.GetColorU32(FuThemeManager.GetColor(FuColors.Separator)));

                    // draw modal body
                    if (_modalBody != null)
                    {
                        //call the stored body callback
                        using (new FuPanel("FuguiModalBody", FuStyle.NoBackgroundUnpadded, true, _currentBodySize.y))
                        {
                            float cursorY = ImGui.GetCursorScreenPos().y;
                            ImGui.Dummy(Vector2.zero);
                            _modalBody();
                            ImGui.Dummy(Vector2.zero);
                            // get body height for this frame
                            _currentBodyHeight = ImGui.GetCursorScreenPos().y - cursorY;
                        }
                    }

                    // draw footer
                    DrawFooter();

                    //end the modal
                    ImGui.EndPopup();
                }

                // animate the modal
                AnimateModal();
            }
        }

        /// <summary>
        /// draw the modal title
        /// </summary>
        /// <param name="title">title text to draw on the modal</param>
        private static void DrawTitle(string title)
        {
            float cursorPos = ImGui.GetCursorScreenPos().y;
            using (FuLayout layout = new FuLayout())
            {
                layout.CenterNextItem(title);
                layout.Text(title);
            }
            _currentTitleHeight = ImGui.GetCursorScreenPos().y - cursorPos;
        }

        /// <summary>
        /// draw the buttons footer
        /// </summary>
        private static void DrawFooter()
        {
            // Return if there are no modal buttons
            if (_modalButtons.Length == 0)
            {
                return;
            }

            // Set the cursor position
            ImGui.SetCursorScreenPos(new Vector2(ImGui.GetCursorScreenPos().x, _currentModalPos.y + _currentTitleHeight + _currentBodySize.y));
            ImGui.Dummy(Vector2.zero);
            using (FuLayout layout = new FuLayout())
            {
                float cursorX = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().x;
                ImGui.SetCursorPosX(cursorX);

                // Draw each button
                foreach (var button in _modalButtons)
                {
                    // Set cursor position
                    Vector2 size = button.GetButtonSize();
                    cursorX -= (size.x + 8f);
                    ImGui.SetCursorPosX(cursorX);

                    // Draw button
                    button.Draw(layout);
                    layout.SameLine();
                }
            }

            // Create a dummy element for spacing
            ImGui.Dummy(Vector2.zero);
            _currentFooterheight = (int)ImGui.GetCursorScreenPos().y - (int)(_currentModalPos.y + _currentTitleHeight + _currentBodySize.y);
             }

        /// <summary>
        /// Update the modal open animation avancement
        /// </summary>
        private static void AnimateModal()
        {
            if (_enlapsed > Settings.ModalAnimationDuration)
            {
                return;
            }
            _enlapsed += ImGui.GetIO().DeltaTime;
        }
        #endregion

        #region Modals
        /// <summary>
        /// Show a modal with yes and no buttons
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="callback">Callback to be called when the yes button is pressed</param>
        /// <param name="yesButtonText">text of the yes button</param>
        /// <param name="noButtonText">text of the no button</param>
        public static void ShowYesNoModal(string title, Action<bool> callback, FuModalSize modalSize, string yesButtonText = "Yes", string noButtonText = "No")
        {
            //call the ShowModal method with the title and buttons
            ShowModal(title, null, modalSize,
                new FuModalButton(yesButtonText, () =>
                {
                    CloseModal();
                    callback?.Invoke(true);
                }, FuButtonStyle.Default),
                new FuModalButton(noButtonText, () =>
                {
                    CloseModal();
                    callback?.Invoke(false);
                }, FuButtonStyle.Default));
        }

        /// <summary>
        /// Show a modal with yes and no buttons
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="body">body callback of the modal</param>
        /// <param name="callback">Callback to be called when the yes button is pressed</param>
        /// <param name="yesButtonText">text of the yes button</param>
        /// <param name="noButtonText">text of the no button</param>
        public static void ShowYesNoModal(string title, Action body, Action<bool> callback, FuModalSize modalSize, string yesButtonText = "Yes", string noButtonText = "No")
        {
            //call the ShowModal method with the title and buttons
            ShowModal(title, body, modalSize,
                new FuModalButton(yesButtonText, () =>
                {
                    CloseModal();
                    callback?.Invoke(true);
                }, FuButtonStyle.Default),
                new FuModalButton(noButtonText, () =>
                {
                    CloseModal();
                    callback?.Invoke(false);
                }, FuButtonStyle.Default));
        }

        /// <summary>
        /// show a modal box
        /// </summary>
        /// <param name="title">title of the modal</param>
        /// <param name="body">body callback of the modal</param>
        /// <param name="modalSize">size of the modal</param>
        /// <param name="icon">icon of the modal box</param>
        /// <param name="color">color of the icon</param>
        private static void ShowBox(string title, Action body, FuModalSize modalSize, Texture2D icon, Color color, params FuModalButton[] buttons)
        {
            //call the ShowModal method with the title, body, and buttons
            ShowModal(title, () =>
            {
                using (FuGrid grid = new FuGrid("modal" + title + "infoGrid", new FuGridDefinition(2, new int[] { 40 }), FuGridFlag.NoAutoLabels, outterPadding: 8f))
                {
                    // vertical align image
                    float mh = _currentBodySize.y;
                    float imgH = 32f;
                    float pad = ((mh / 2f) - (imgH / 2f)) / 2f;
                    grid.NextElementYPadding(pad);
                    grid.Image("modalBoxIcon" + title, icon, new Vector2(imgH, imgH), color);
                    grid.NextColumn();
                    body?.Invoke();
                };
            }, modalSize, buttons);
        }

        /// <summary>
        /// show a modal box
        /// </summary>
        /// <param name="title">title of the modal</param>
        /// <param name="body">body callback of the modal</param>
        /// <param name="modalSize">size of the modal</param>
        /// <param name="icon">icon of the modal box</param>
        /// <param name="color">color of the icon</param>
        private static void ShowBox(string title, string body, FuModalSize modalSize, Texture2D icon, Color color, params FuModalButton[] buttons)
        {
            ShowBox(title, () =>
            {
                using (FuLayout layout = new FuLayout())
                {
                    layout.Text(body);
                }
            }, modalSize, icon, color, buttons);
        }

        /// <summary>
        /// Show a modal with an info box and an ok button
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="body">Body of the modal</param>
        /// <param name="modalSize">Size of the modal</param>
        public static void ShowInfo(string title, Action body, FuModalSize modalSize, params FuModalButton[] buttons)
        {
            ShowBox(title, body, modalSize, Fugui.Settings.InfoIcon, FuTextStyle.Info.Text, buttons);
        }

        /// <summary>
        /// Show a modal with an info box and an ok button
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="body">Body of the modal</param>
        /// <param name="modalSize">Size of the modal</param>
        public static void ShowInfo(string title, string body, FuModalSize modalSize, params FuModalButton[] buttons)
        {
            ShowBox(title, body, modalSize, Fugui.Settings.InfoIcon, FuTextStyle.Info.Text, buttons);
        }

        /// <summary>
        /// Show a modal with an info box and an ok button
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="body">Body of the modal</param>
        /// <param name="modalSize">Size of the modal</param>
        public static void ShowDanger(string title, Action body, FuModalSize modalSize, params FuModalButton[] buttons)
        {
            ShowBox(title, body, modalSize, Fugui.Settings.DangerIcon, FuTextStyle.Danger.Text, buttons);
        }

        /// <summary>
        /// Show a modal with an info box and an ok button
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="body">Body of the modal</param>
        /// <param name="modalSize">Size of the modal</param>
        public static void ShowDanger(string title, string body, FuModalSize modalSize, params FuModalButton[] buttons)
        {
            ShowBox(title, body, modalSize, Fugui.Settings.DangerIcon, FuTextStyle.Danger.Text, buttons);
        }

        /// <summary>
        /// Show a modal with an info box and an ok button
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="body">Body of the modal</param>
        /// <param name="modalSize">Size of the modal</param>
        public static void ShowWarning(string title, Action body, FuModalSize modalSize, params FuModalButton[] buttons)
        {
            ShowBox(title, body, modalSize, Fugui.Settings.WarningIcon, FuTextStyle.Warning.Text, buttons);
        }

        /// <summary>
        /// Show a modal with an info box and an ok button
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="body">Body of the modal</param>
        /// <param name="modalSize">Size of the modal</param>
        public static void ShowWarning(string title, string body, FuModalSize modalSize, params FuModalButton[] buttons)
        {
            ShowBox(title, body, modalSize, Fugui.Settings.WarningIcon, FuTextStyle.Warning.Text, buttons);
        }

        /// <summary>
        /// Show a modal with an info box and an ok button
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="body">Body of the modal</param>
        /// <param name="modalSize">Size of the modal</param>
        public static void ShowSuccess(string title, Action body, FuModalSize modalSize, params FuModalButton[] buttons)
        {
            ShowBox(title, body, modalSize, Fugui.Settings.SuccessIcon, FuTextStyle.Success.Text, buttons);
        }

        /// <summary>
        /// Show a modal with an info box and an ok button
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="body">Body of the modal</param>
        /// <param name="modalSize">Size of the modal</param>
        public static void ShowSuccess(string title, string body, FuModalSize modalSize, params FuModalButton[] buttons)
        {
            ShowBox(title, body, modalSize, Fugui.Settings.SuccessIcon, FuTextStyle.Success.Text, buttons);
        }
        #endregion
    }
}