using Fu.Framework;
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
        private static bool _showModal = false;
        private static bool _preventCloseModal = false;
        private static string _modalTitle;
        private static Action<FuLayout> _modalBody;
        private static FuModalButton[] _modalButtons;
        private static FuModalSize _currentModalSize;
        private static FuModalFlags _currentModalFlags = FuModalFlags.Default;
        private static Vector2 _currentBodySize;
        private static float _currentBodyHeight = 0f;
        private static float _currentTitleHeight = 0f;
        private static float _currentFooterheight = 0f;
        private static Vector2 _currentModalPos;
        private static float _enlapsed = 0f;
        private static readonly Vector2 _modalMinScreenSpacing = new Vector2(16f, 16f);
        private const float _modalMinBodySize = 32f;
        #endregion

        #region Methods
        /// <summary>
        /// Show a modal with a custom title, body, and buttons
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="body">Body of the modal</param>
        /// <param name="buttons">List of buttons in the modal, each button contains a text and callback</param>
        public static void ShowModal(string title, Action<FuLayout> body, FuModalSize modalSize, params FuModalButton[] buttons)
        {
            ShowModal(title, body, modalSize, FuModalFlags.Default, buttons);
        }

        /// <summary>
        /// Show a modal with a custom title, body, buttons, and chrome flags
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="body">Body of the modal</param>
        /// <param name="modalSize">Size of the modal</param>
        /// <param name="flags">Modal chrome flags</param>
        /// <param name="buttons">List of buttons in the modal, each button contains a text and callback</param>
        public static void ShowModal(string title, Action<FuLayout> body, FuModalSize modalSize, FuModalFlags flags, params FuModalButton[] buttons)
        {
            _modalTitle = title; //store the title
            _modalBody = body; //store the body
            // add default button if needed
            if (!flags.HasFlag(FuModalFlags.NoFooterBar) && (buttons == null || buttons.Length == 0))
            {
                buttons = new FuModalButton[]
                {
                    new FuModalButton("OK", CloseModal, FuButtonStyle.Default)
                };
            }
            else if (buttons == null)
            {
                buttons = new FuModalButton[0];
            }
            // set default button style if needed
            if (!Settings.StateModalsUseButtonColors)
            {
                for (int i = 0; i < buttons.Length; i++)
                {
                    buttons[i].SetStyle(FuButtonStyle.Default);
                }
            }
            _modalButtons = buttons; //store the buttons
            _showModal = true; //set showModal to true to show the modal
            _currentModalSize = modalSize;
            _currentModalFlags = flags;
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
        /// Determines whether any Fugui modal-style popup is currently open.
        /// </summary>
        /// <returns>true if a modal or popup message is open; otherwise, false.</returns>
        public static bool IsAnyModalOpen()
        {
            return _showModal || _showPopup;
        }

        /// <summary>
        /// Prevent the modal to close on next CloseModal call
        /// </summary>
        public static void CancelNextModalClose()
        {
            _preventCloseModal = true;
        }

        /// <summary>
        /// Render the currently shown modal
        /// </summary>
        public static void RenderModal(IFuWindowContainer container)
        {
            if (_showModal)
            {
                ImGui.OpenPopup(_modalTitle); //open the modal with the stored title

                bool hasTitleBar = HasModalTitleBar();
                bool hasFooterBar = HasModalFooterBar();

                // calculate y padding
                float yPadding = Fugui.Themes.FramePadding.y * 2f + Fugui.Themes.WindowPadding.y * 2f;
                _currentTitleHeight = hasTitleBar ? Mathf.Ceil(ImGui.CalcTextSize(_modalTitle).y + yPadding) : 0f;
                _currentFooterheight = hasFooterBar ? CalculateFooterHeight(yPadding) : 0f;

                // calculate body size
                Vector2 minSpacing = _modalMinScreenSpacing * Fugui.Scale;
                Vector2 availableSize = new Vector2(
                    Mathf.Max(_modalMinBodySize, container.Size.x - minSpacing.x * 2f),
                    Mathf.Max(_modalMinBodySize, container.Size.y - minSpacing.y * 2f)
                );

                Vector2 requestedSize = _currentModalSize.Size;
                float targetBodyWidth = requestedSize.x;
                float targetBodyHeight = requestedSize.y > 0f
                    ? requestedSize.y
                    : _currentBodyHeight + GetAutoBodyHeightPadding();

                _currentBodySize = new Vector2(
                    Mathf.Ceil(Mathf.Clamp(targetBodyWidth, _modalMinBodySize, availableSize.x)),
                    Mathf.Ceil(Mathf.Clamp(targetBodyHeight, _modalMinBodySize, Mathf.Max(_modalMinBodySize, availableSize.y - _currentTitleHeight - _currentFooterheight)))
                );

                Vector2 modalSize = new Vector2(
                    _currentBodySize.x,
                    Mathf.Min(availableSize.y, _currentBodySize.y + _currentFooterheight + _currentTitleHeight)
                );

                modalSize = CeilVector(modalSize);
                _currentBodySize.y = Mathf.Max(_modalMinBodySize, modalSize.y - _currentTitleHeight - _currentFooterheight);

                _currentModalPos = RoundVector(new Vector2(
                    Mathf.Clamp(container.Size.x / 2f - modalSize.x / 2f, minSpacing.x, Mathf.Max(minSpacing.x, container.Size.x - modalSize.x - minSpacing.x)),
                    Mathf.Clamp(container.Size.y / 2f - modalSize.y / 2f, minSpacing.y, Mathf.Max(minSpacing.y, container.Size.y - modalSize.y - minSpacing.y))
                ));

                Vector2 modalStartPos = RoundVector(new Vector2(
                    container.Size.x / 2f - (modalSize.x * 0.01f) / 2f,
                    minSpacing.y
                ));

                if (_enlapsed < Settings.ModalAnimationDuration)
                {
                    float animationRatio = Settings.ModalAnimationDuration <= 0f
                        ? 1f
                        : Mathf.Clamp01(_enlapsed / Settings.ModalAnimationDuration);

                    ImGui.SetNextWindowSize(RoundVector(Vector2.Lerp(modalSize * 0.01f, modalSize, animationRatio)), ImGuiCond.Always);
                    ImGui.SetNextWindowPos(RoundVector(Vector2.Lerp(modalStartPos, _currentModalPos, animationRatio)), ImGuiCond.Always);
                }
                else
                {
                    ImGui.SetNextWindowSize(modalSize, ImGuiCond.Always);
                    ImGui.SetNextWindowPos(_currentModalPos, ImGuiCond.Always);
                }
                //ImGui.SetNextWindowFocus();
                // beggin modal
                ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, Fugui.Themes.PopupRounding);
                bool usePopupBackdrop = Fugui.ShouldUseThemeBackdrop(FuColors.PopupBg, 0.98f);
                Fugui.Push(ImGuiCol.PopupBg, Fugui.Themes.GetColor(FuColors.PopupBg, usePopupBackdrop ? 0f : 1f));
                ImGuiWindowFlags modalFlags = ImGuiWindowFlags.Modal | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
                if (usePopupBackdrop)
                {
                    modalFlags |= ImGuiWindowFlags.NoBackground;
                }

                if (ImGui.BeginPopupModal(_modalTitle, ref _showModal, modalFlags))
                {
                    Fugui.RegisterSurface(
                        container,
                        _modalTitle,
                        FuSurfaceType.Modal,
                        FuLayer.Top,
                        null,
                        new Rect(ImGui.GetWindowPos(), ImGui.GetWindowSize()),
                        true,
                        true);
                    Fugui.BeginModalSurfaceDrawing(true);
                    try
                    {
                        if (usePopupBackdrop)
                        {
                            Fugui.DrawThemeBackdrop(new Rect(_currentModalPos, modalSize), FuColors.PopupBg, 0.98f, Fugui.Themes.PopupRounding);
                        }

                        // draw modal title
                        if (hasTitleBar)
                        {
                            DrawTitle(_modalTitle);
                        }

                        ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                        // draw body BG
                        if (!usePopupBackdrop)
                        {
                            drawList.AddRectFilled(new Vector2(_currentModalPos.x, _currentModalPos.y + _currentTitleHeight), new Vector2(_currentModalPos.x + modalSize.x, _currentModalPos.y + _currentTitleHeight + _currentBodySize.y), ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.PopupBg)));
                        }
                        // draw title line
                        if (hasTitleBar)
                        {
                            drawList.AddLine(new Vector2(_currentModalPos.x, _currentModalPos.y + _currentTitleHeight), new Vector2(_currentModalPos.x + modalSize.x, _currentModalPos.y + _currentTitleHeight), ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.Separator)));
                        }
                        // draw  footer line
                        if (hasFooterBar)
                        {
                            drawList.AddLine(new Vector2(_currentModalPos.x, _currentModalPos.y + _currentTitleHeight + _currentBodySize.y), new Vector2(_currentModalPos.x + modalSize.x, _currentModalPos.y + _currentTitleHeight + _currentBodySize.y), ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.Separator)));
                        }

                        // draw modal body
                        if (_modalBody != null)
                        {
                            //call the stored body callback
                            ImGui.SetCursorScreenPos(new Vector2(ImGui.GetCursorScreenPos().x, _currentModalPos.y + _currentTitleHeight));
                            FuStyle.NoBackgroundUnpadded.Push(true);
                            try
                            {
                                Fugui.BeginChild("FuguiModalBody", _currentBodySize);
                                try
                                {
                                    float cursorY = ImGui.GetCursorScreenPos().y;
                                    ImGui.Dummy(Vector2.zero);
                                    using (FuLayout layout = new FuLayout())
                                    {
                                        _modalBody(layout);
                                    }
                                    ImGui.Dummy(Vector2.zero);
                                    // get body height for this frame
                                    SetMeasuredModalBodyHeight(ImGui.GetCursorScreenPos().y - cursorY);
                                }
                                finally
                                {
                                    Fugui.EndChild();
                                }
                            }
                            finally
                            {
                                FuStyle.NoBackgroundUnpadded.Pop();
                            }
                        }

                        // draw footer
                        if (hasFooterBar)
                        {
                            DrawFooter();
                        }
                    }
                    finally
                    {
                        Fugui.EndModalSurfaceDrawing();
                        //end the modal
                        ImGui.EndPopup();
                    }
                }
                Fugui.PopColor();
                ImGui.PopStyleVar();
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
            Vector2 cursorPos = ImGui.GetCursorScreenPos();
            using (FuLayout layout = new FuLayout())
            {
                layout.CenterNextItemH(title);
                layout.CenterNextItemV(title, _currentTitleHeight);
                layout.Text(title);
            }
            ImGui.SetCursorScreenPos(new Vector2(cursorPos.x, cursorPos.y + _currentTitleHeight));
        }

        /// <summary>
        /// draw the buttons footer
        /// </summary>
        private static void DrawFooter()
        {
            if (!HasModalFooterBar())
            {
                return;
            }

            float spacing = 8f * Fugui.Scale;
            float footerY = _currentModalPos.y + _currentTitleHeight + _currentBodySize.y;
            float cursorX = _currentModalPos.x + _currentBodySize.x;
            using (FuLayout layout = new FuLayout())
            {
                // Draw each button
                foreach (var button in _modalButtons)
                {
                    // Set cursor position
                    Vector2 size = button.GetButtonSize();
                    cursorX -= size.x + spacing;
                    ImGui.SetCursorScreenPos(new Vector2(cursorX, footerY + Mathf.Max(0f, (_currentFooterheight - size.y) * 0.5f)));

                    // Draw button
                    button.Draw(layout);
                }
            }

            ImGui.SetCursorScreenPos(new Vector2(ImGui.GetCursorScreenPos().x, footerY + _currentFooterheight));
        }

        private static bool HasModalTitleBar()
        {
            return !_currentModalFlags.HasFlag(FuModalFlags.NoTitleBar);
        }

        private static bool HasModalFooterBar()
        {
            return !_currentModalFlags.HasFlag(FuModalFlags.NoFooterBar) && _modalButtons != null && _modalButtons.Length > 0;
        }

        private static float CalculateFooterHeight(float yPadding)
        {
            float maxButtonHeight = 0f;
            foreach (var button in _modalButtons)
            {
                maxButtonHeight = Mathf.Max(maxButtonHeight, button.GetButtonSize().y);
            }
            return Mathf.Ceil(maxButtonHeight + yPadding);
        }

        private static void SetMeasuredModalBodyHeight(float measuredHeight)
        {
            measuredHeight = Mathf.Ceil(Mathf.Max(0f, measuredHeight));
            if (Mathf.Abs(_currentBodyHeight - measuredHeight) > 0.5f)
            {
                _currentBodyHeight = measuredHeight;
            }
        }

        private static float GetAutoBodyHeightPadding()
        {
            return Mathf.Max(2f * Fugui.Scale, ImGui.GetStyle().ItemSpacing.y);
        }

        private static Vector2 CeilVector(Vector2 value)
        {
            return new Vector2(Mathf.Ceil(value.x), Mathf.Ceil(value.y));
        }

        private static Vector2 RoundVector(Vector2 value)
        {
            return new Vector2(Mathf.Round(value.x), Mathf.Round(value.y));
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

        /// <summary>
        /// Show a modal with yes and no buttons
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="callback">Callback to be called when the yes button is pressed</param>
        /// <param name="yesButtonText">text of the yes button</param>
        /// <param name="noButtonText">text of the no button</param>
        public static void ShowYesNoModal(string title, Action<bool> callback, FuModalSize modalSize, string yesButtonText = "Yes", string noButtonText = "No")
        {
            ShowYesNoModal(title, callback, modalSize, FuModalFlags.Default, yesButtonText, noButtonText);
        }

        /// <summary>
        /// Show a modal with yes and no buttons
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="callback">Callback to be called when the yes button is pressed</param>
        /// <param name="modalSize">Size of the modal</param>
        /// <param name="flags">Modal chrome flags</param>
        /// <param name="yesButtonText">text of the yes button</param>
        /// <param name="noButtonText">text of the no button</param>
        public static void ShowYesNoModal(string title, Action<bool> callback, FuModalSize modalSize, FuModalFlags flags, string yesButtonText = "Yes", string noButtonText = "No")
        {
            //call the ShowModal method with the title and buttons
            ShowModal(title, null, modalSize, flags,
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
        public static void ShowYesNoModal(string title, Action<FuLayout> body, Action<bool> callback, FuModalSize modalSize, string yesButtonText = "Yes", string noButtonText = "No")
        {
            ShowYesNoModal(title, body, callback, modalSize, FuModalFlags.Default, yesButtonText, noButtonText);
        }

        /// <summary>
        /// Show a modal with yes and no buttons
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="body">body callback of the modal</param>
        /// <param name="callback">Callback to be called when the yes button is pressed</param>
        /// <param name="modalSize">Size of the modal</param>
        /// <param name="flags">Modal chrome flags</param>
        /// <param name="yesButtonText">text of the yes button</param>
        /// <param name="noButtonText">text of the no button</param>
        public static void ShowYesNoModal(string title, Action<FuLayout> body, Action<bool> callback, FuModalSize modalSize, FuModalFlags flags, string yesButtonText = "Yes", string noButtonText = "No")
        {
            //call the ShowModal method with the title and buttons
            ShowModal(title, body, modalSize, flags,
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
        private static void ShowBox(string title, Action<FuLayout> body, FuModalSize modalSize, Texture2D icon, Color color, params FuModalButton[] buttons)
        {
            ShowBox(title, body, modalSize, FuModalFlags.Default, icon, color, buttons);
        }

        /// <summary>
        /// show a modal box
        /// </summary>
        /// <param name="title">title of the modal</param>
        /// <param name="body">body callback of the modal</param>
        /// <param name="modalSize">size of the modal</param>
        /// <param name="flags">Modal chrome flags</param>
        /// <param name="icon">icon of the modal box</param>
        /// <param name="color">color of the icon</param>
        private static void ShowBox(string title, Action<FuLayout> body, FuModalSize modalSize, FuModalFlags flags, Texture2D icon, Color color, params FuModalButton[] buttons)
        {
            //call the ShowModal method with the title, body, and buttons
            ShowModal(title, (layout) =>
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
                    body?.Invoke(layout);
                }
                ;
            }, modalSize, flags, buttons);
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
            ShowBox(title, body, modalSize, FuModalFlags.Default, icon, color, buttons);
        }

        /// <summary>
        /// show a modal box
        /// </summary>
        /// <param name="title">title of the modal</param>
        /// <param name="body">body callback of the modal</param>
        /// <param name="modalSize">size of the modal</param>
        /// <param name="flags">Modal chrome flags</param>
        /// <param name="icon">icon of the modal box</param>
        /// <param name="color">color of the icon</param>
        private static void ShowBox(string title, string body, FuModalSize modalSize, FuModalFlags flags, Texture2D icon, Color color, params FuModalButton[] buttons)
        {
            ShowBox(title, (layout) =>
            {
                layout.Text(body);
            }, modalSize, flags, icon, color, buttons);
        }

        /// <summary>
        /// Show a modal with an info box and an ok button
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="body">Body of the modal</param>
        /// <param name="modalSize">Size of the modal</param>
        public static void ShowInfo(string title, Action<FuLayout> body, FuModalSize modalSize, params FuModalButton[] buttons)
        {
            ShowInfo(title, body, modalSize, FuModalFlags.Default, buttons);
        }

        /// <summary>
        /// Show a modal with an info box and an ok button
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="body">Body of the modal</param>
        /// <param name="modalSize">Size of the modal</param>
        /// <param name="flags">Modal chrome flags</param>
        public static void ShowInfo(string title, Action<FuLayout> body, FuModalSize modalSize, FuModalFlags flags, params FuModalButton[] buttons)
        {
            ShowBox(title, body, modalSize, flags, Fugui.Settings.InfoIcon, FuTextStyle.Info.Text, buttons);
        }

        /// <summary>
        /// Show a modal with an info box and an ok button
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="body">Body of the modal</param>
        /// <param name="modalSize">Size of the modal</param>
        public static void ShowInfo(string title, string body, FuModalSize modalSize, params FuModalButton[] buttons)
        {
            ShowInfo(title, body, modalSize, FuModalFlags.Default, buttons);
        }

        /// <summary>
        /// Show a modal with an info box and an ok button
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="body">Body of the modal</param>
        /// <param name="modalSize">Size of the modal</param>
        /// <param name="flags">Modal chrome flags</param>
        public static void ShowInfo(string title, string body, FuModalSize modalSize, FuModalFlags flags, params FuModalButton[] buttons)
        {
            ShowBox(title, body, modalSize, flags, Fugui.Settings.InfoIcon, FuTextStyle.Info.Text, buttons);
        }

        /// <summary>
        /// Show a modal with an info box and an ok button
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="body">Body of the modal</param>
        /// <param name="modalSize">Size of the modal</param>
        public static void ShowDanger(string title, Action<FuLayout> body, FuModalSize modalSize, params FuModalButton[] buttons)
        {
            ShowDanger(title, body, modalSize, FuModalFlags.Default, buttons);
        }

        /// <summary>
        /// Show a modal with an info box and an ok button
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="body">Body of the modal</param>
        /// <param name="modalSize">Size of the modal</param>
        /// <param name="flags">Modal chrome flags</param>
        public static void ShowDanger(string title, Action<FuLayout> body, FuModalSize modalSize, FuModalFlags flags, params FuModalButton[] buttons)
        {
            ShowBox(title, body, modalSize, flags, Fugui.Settings.DangerIcon, FuTextStyle.Danger.Text, buttons);
        }

        /// <summary>
        /// Show a modal with an info box and an ok button
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="body">Body of the modal</param>
        /// <param name="modalSize">Size of the modal</param>
        public static void ShowDanger(string title, string body, FuModalSize modalSize, params FuModalButton[] buttons)
        {
            ShowDanger(title, body, modalSize, FuModalFlags.Default, buttons);
        }

        /// <summary>
        /// Show a modal with an info box and an ok button
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="body">Body of the modal</param>
        /// <param name="modalSize">Size of the modal</param>
        /// <param name="flags">Modal chrome flags</param>
        public static void ShowDanger(string title, string body, FuModalSize modalSize, FuModalFlags flags, params FuModalButton[] buttons)
        {
            ShowBox(title, body, modalSize, flags, Fugui.Settings.DangerIcon, FuTextStyle.Danger.Text, buttons);
        }

        /// <summary>
        /// Show a modal with an info box and an ok button
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="body">Body of the modal</param>
        /// <param name="modalSize">Size of the modal</param>
        public static void ShowWarning(string title, Action<FuLayout> body, FuModalSize modalSize, params FuModalButton[] buttons)
        {
            ShowWarning(title, body, modalSize, FuModalFlags.Default, buttons);
        }

        /// <summary>
        /// Show a modal with an info box and an ok button
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="body">Body of the modal</param>
        /// <param name="modalSize">Size of the modal</param>
        /// <param name="flags">Modal chrome flags</param>
        public static void ShowWarning(string title, Action<FuLayout> body, FuModalSize modalSize, FuModalFlags flags, params FuModalButton[] buttons)
        {
            ShowBox(title, body, modalSize, flags, Fugui.Settings.WarningIcon, FuTextStyle.Warning.Text, buttons);
        }

        /// <summary>
        /// Show a modal with an info box and an ok button
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="body">Body of the modal</param>
        /// <param name="modalSize">Size of the modal</param>
        public static void ShowWarning(string title, string body, FuModalSize modalSize, params FuModalButton[] buttons)
        {
            ShowWarning(title, body, modalSize, FuModalFlags.Default, buttons);
        }

        /// <summary>
        /// Show a modal with an info box and an ok button
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="body">Body of the modal</param>
        /// <param name="modalSize">Size of the modal</param>
        /// <param name="flags">Modal chrome flags</param>
        public static void ShowWarning(string title, string body, FuModalSize modalSize, FuModalFlags flags, params FuModalButton[] buttons)
        {
            ShowBox(title, body, modalSize, flags, Fugui.Settings.WarningIcon, FuTextStyle.Warning.Text, buttons);
        }

        /// <summary>
        /// Show a modal with an info box and an ok button
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="body">Body of the modal</param>
        /// <param name="modalSize">Size of the modal</param>
        public static void ShowSuccess(string title, Action<FuLayout> body, FuModalSize modalSize, params FuModalButton[] buttons)
        {
            ShowSuccess(title, body, modalSize, FuModalFlags.Default, buttons);
        }

        /// <summary>
        /// Show a modal with an info box and an ok button
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="body">Body of the modal</param>
        /// <param name="modalSize">Size of the modal</param>
        /// <param name="flags">Modal chrome flags</param>
        public static void ShowSuccess(string title, Action<FuLayout> body, FuModalSize modalSize, FuModalFlags flags, params FuModalButton[] buttons)
        {
            ShowBox(title, body, modalSize, flags, Fugui.Settings.SuccessIcon, FuTextStyle.Success.Text, buttons);
        }

        /// <summary>
        /// Show a modal with an info box and an ok button
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="body">Body of the modal</param>
        /// <param name="modalSize">Size of the modal</param>
        public static void ShowSuccess(string title, string body, FuModalSize modalSize, params FuModalButton[] buttons)
        {
            ShowSuccess(title, body, modalSize, FuModalFlags.Default, buttons);
        }

        /// <summary>
        /// Show a modal with an info box and an ok button
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="body">Body of the modal</param>
        /// <param name="modalSize">Size of the modal</param>
        /// <param name="flags">Modal chrome flags</param>
        public static void ShowSuccess(string title, string body, FuModalSize modalSize, FuModalFlags flags, params FuModalButton[] buttons)
        {
            ShowBox(title, body, modalSize, flags, Fugui.Settings.SuccessIcon, FuTextStyle.Success.Text, buttons);
        }
        #endregion
    }
}
