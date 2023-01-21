using ImGuiNET;
using System;
using UnityEngine;

namespace Fugui.Framework
{
    public static class FuguiModal
    {
        private static bool _showModal = false;
        private static string _modalTitle;
        private static Action _modalBody;
        private static UIModalButton[] _modalButtons;
        private static float _currentBodyHeight = 0f;
        private static UIModalSize _currentModalSize;
        private static float _currentModalTitleHeight = 24f;
        private static float _currentModalFooterheight = 36f;
        private const float ANIMATION_DURATION = 0.2f;
        private static float _enlapsed = 0f;

        /// <summary>
        /// Show a modal with a custom title, body, and buttons
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="body">Body of the modal</param>
        /// <param name="buttons">List of buttons in the modal, each button contains a text and callback</param>
        public static void ShowModal(string title, Action body, UIModalSize modalSize, params UIModalButton[] buttons)
        {
            _modalTitle = title; //store the title
            _modalBody = body; //store the body
            // add default button if needed
            if (buttons == null || buttons.Length == 0)
            {
                buttons = new UIModalButton[]
                {
                    new UIModalButton("OK", HideModal, UIButtonStyle.Default)
                };
            }
            _modalButtons = buttons; //store the buttons
            _showModal = true; //set showModal to true to show the modal
            _currentModalSize = modalSize;
            _currentBodyHeight = 0f;
            _enlapsed = 0f;
        }

        /// <summary>
        /// Hide the currently shown modal
        /// </summary>
        public static void HideModal()
        {
            _enlapsed = ANIMATION_DURATION;
            _showModal = false; //set showModal to false to hide the modal
        }

        /// <summary>
        /// Render the currently shown modal
        /// </summary>
        public static void RenderModal()
        {
            // TODO : calculate padding using style instead of const
            if (_showModal)
            {
                ImGui.OpenPopup(_modalTitle); //open the modal with the stored title

                // claculate y padding
                float yPadding = ThemeManager.CurrentTheme.FramePadding.y * 2f + ThemeManager.CurrentTheme.WindowPadding.y * 2f;

                // calculate footer height
                _currentModalFooterheight = _modalButtons[0].GetButtonSize().y;

                // calculate title height
                _currentModalTitleHeight = ImGui.CalcTextSize(_modalTitle).y;

                // calculate body size
                Vector2 bodySize = _currentModalSize.Size;
                if (_currentBodyHeight > 0f)
                {
                    bodySize.y = _currentBodyHeight;
                }
                bodySize.y = Mathf.Clamp(bodySize.y, 32f, FuGui.MainContainer.Size.y - 256f);

                // calculate full size and pos
                Vector2 modalSize = new Vector2(bodySize.x, bodySize.y + _currentModalFooterheight + _currentModalTitleHeight);
                Vector2 modalPos = new Vector2(FuGui.MainContainer.Size.x / 2f - modalSize.x / 2f, FuGui.MainContainer.Size.y / 2f - modalSize.y / 2f);
                Vector2 modalStartPos = new Vector2(FuGui.MainContainer.Size.x / 2f - (modalSize.x * 0.01f) / 2f, 64f);
                ImGui.SetNextWindowSize(Vector2.Lerp(modalSize * 0.01f, modalSize, _enlapsed / ANIMATION_DURATION), ImGuiCond.Always);
                ImGui.SetNextWindowPos(Vector2.Lerp(modalStartPos, modalPos, _enlapsed / ANIMATION_DURATION), ImGuiCond.Always);

                // beggin modal
                if (ImGui.BeginPopupModal(_modalTitle, ref _showModal, ImGuiWindowFlags.Modal | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                {
                    // draw body BG
                    ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                    //drawList.AddRectFilled(new Vector2(modalPos.x, modalPos.y + _currentModalTitleHeight), new Vector2(modalPos.x + modalSize.x, modalPos.y + _currentModalTitleHeight + bodySize.y), ImGui.GetColorU32(ThemeManager.GetColor(FuguiColor.WindowBg)));
                    // draw title and footer line
                    drawList.AddLine(new Vector2(modalPos.x, modalPos.y + _currentModalTitleHeight), new Vector2(modalPos.x + modalSize.x, modalPos.y + _currentModalTitleHeight), ImGui.GetColorU32(ThemeManager.GetColor(FuguiColors.Separator)));
                    drawList.AddLine(new Vector2(modalPos.x, modalPos.y + _currentModalTitleHeight + bodySize.y), new Vector2(modalPos.x + modalSize.x, modalPos.y + _currentModalTitleHeight + bodySize.y), ImGui.GetColorU32(ThemeManager.GetColor(FuguiColors.Separator)));

                    // draw modal title
                    drawTitle(_modalTitle);

                    // draw modal body
                    using (new UIPanel("FuguiModalBody", UIStyle.Modal, bodySize.y, scrollable: true))
                    {
                        using (UILayout layout = new UILayout())
                        {
                            if (_modalBody != null)
                            {
                                float cursorY = ImGui.GetCursorScreenPos().y;
                                //call the stored body callback
                                _modalBody();
                                // get body height for this frame
                                _currentBodyHeight = ImGui.GetCursorScreenPos().y - cursorY;
                            }
                        }
                    }

                    // draw footer
                    drawFooter();
                    //end the modal
                    ImGui.EndPopup();
                }

                // animate the modal
                animateModal();
            }
        }

        /// <summary>
        /// draw the modal title
        /// </summary>
        /// <param name="title"></param>
        private static void drawTitle(string title)
        {
            using (UILayout layout = new UILayout())
            {
                layout.Dummy(ImGui.GetContentRegionAvail().x / 2f - ImGui.CalcTextSize(title).x / 2f);
                layout.SameLine();
                layout.Text(title);
            }
        }

        /// <summary>
        /// draw the buttons footer
        /// </summary>
        private static void drawFooter()
        {
            if (_modalButtons.Length == 0)
            {
                return;
            }

            using (UILayout layout = new UILayout())
            {
                float buttonsHeight = _modalButtons[0].GetButtonSize().y;
                ImGui.Dummy(new Vector2(0f, ImGui.GetContentRegionAvail().y - _currentModalFooterheight / 2f - buttonsHeight / 2f));
                float cursorX = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().x;
                ImGui.SetCursorPosX(cursorX);

                foreach (var button in _modalButtons)
                {
                    // set cursor position
                    Vector2 size = button.GetButtonSize();
                    cursorX -= size.x - 8f;
                    ImGui.SetCursorPosX(cursorX);
                    // draw button
                    button.Draw(layout);
                    layout.SameLine();
                }
            }
        }

        private static void animateModal()
        {
            if (_enlapsed > ANIMATION_DURATION)
            {
                return;
            }
            _enlapsed += ImGui.GetIO().DeltaTime;
        }

        /// <summary>
        /// Show a modal with yes and no buttons
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="callback">Callback to be called when the yes button is pressed</param>
        /// <param name="yesButtonText">text of the yes button</param>
        /// <param name="noButtonText">text of the no button</param>
        public static void ShowYesNoModal(string title, Action<bool> callback, UIModalSize modalSize, string yesButtonText = "Yes", string noButtonText = "No")
        {
            //call the ShowModal method with the title and buttons
            ShowModal(title, null, modalSize,
                new UIModalButton(yesButtonText, () =>
                {
                    HideModal();
                    callback?.Invoke(true);
                }, UIButtonStyle.Default),
                new UIModalButton(noButtonText, () =>
                {
                    HideModal();
                    callback?.Invoke(false);
                }, UIButtonStyle.Default));
        }

        /// <summary>
        /// Show a modal with an info box and an ok button
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="body">Body of the modal</param>
        public static void ShowInfoBoxModal(string title, Action body, UIModalSize modalSize)
        {
            //call the ShowModal method with the title, body, and buttons
            ShowModal(title, body, modalSize, new UIModalButton("OK", HideModal, UIButtonStyle.Default));
        }
    }

    public struct UIModalButton
    {
        public string text;
        public Action callback;
        public UIButtonStyle style;

        public UIModalButton(string text, Action callback, UIButtonStyle style)
        {
            this.text = text;
            this.callback = callback;
            this.style = style;
        }

        public void Draw(UILayout layout)
        {
            if (layout.Button(text, GetButtonSize(), style))
            {
                callback();
            }
        }

        public Vector2 GetButtonSize()
        {
            Vector2 framePadding = ThemeManager.CurrentTheme.FramePadding;
            return ImGui.CalcTextSize(text) + (framePadding * 2f);
        }
    }
}