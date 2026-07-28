using Dissonance;
using Dissonance.Audio.Capture;
using Fu;
using Fu.Framework;

using Saravr.Network.Common;
using Saravr.Network.Voice;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.UI.Windows.Common
{
    /// <summary>
    /// Draws network voice settings as a reusable widget.
    /// </summary>
    public sealed class NetworkSettingsWidget
    {
        /// <summary>
        /// Height of a standard settings row.
        /// </summary>
        private const float RowHeight = 52f;

        /// <summary>
        /// Height of a section label row.
        /// </summary>
        private const float SectionHeight = 40f;

        /// <summary>
        /// Height of the panel header.
        /// </summary>
        private const float HeaderHeight = 72f;

        /// <summary>
        /// Height of a dropdown option row.
        /// </summary>
        private const float DropdownOptionHeight = 34f;

        /// <summary>
        /// Label used for the default device choice.
        /// </summary>
        private const string SystemDefaultLabel = "System Default";

        /// <summary>
        /// Ordered Dissonance sensitivity values exposed as a threshold control.
        /// </summary>
        private static readonly VadSensitivityLevels[] ThresholdSensitivityValues =
        {
            VadSensitivityLevels.LowSensitivity,
            VadSensitivityLevels.MediumSensitivity,
            VadSensitivityLevels.HighSensitivity,
            VadSensitivityLevels.VeryHighSensitivity
        };

        /// <summary>
        /// Labels mapped to the threshold control values.
        /// </summary>
        private static readonly string[] ThresholdLabels =
        {
            "High",
            "Med",
            "Low",
            "Min"
        };

        /// <summary>
        /// Stores one device dropdown option.
        /// </summary>
        private struct DeviceOption
        {
            public string Label;
            public string Value;
            public bool Enabled;
        }

        /// <summary>
        /// Stores the active dropdown layer request.
        /// </summary>
        private struct DeviceMenuRequest
        {
            public bool HasValue;
            public Rect TriggerRect;
            public Rect ClipRect;
            public List<DeviceOption> Options;
            public string CurrentValue;
            public TimelineWidgetTheme Theme;
            public float Alpha;
            public bool Interactable;
            public Action<string, bool> OnSelected;
        }

        /// <summary>
        /// Current visual theme.
        /// </summary>
        private TimelineWidgetTheme _theme;

        /// <summary>
        /// Current vertical body scroll offset.
        /// </summary>
        private float _scrollY;

        /// <summary>
        /// Animated mute toggle amount.
        /// </summary>
        private float _muteToggleAmount;

        /// <summary>
        /// Animated push-to-talk toggle amount.
        /// </summary>
        private float _pushToTalkToggleAmount;

        /// <summary>
        /// Whether toggle animation state has been initialized.
        /// </summary>
        private bool _toggleAnimationsInitialized;

        /// <summary>
        /// Whether the input device menu is open.
        /// </summary>
        private bool _inputMenuOpen;

        /// <summary>
        /// Whether the input menu was opened on the current frame.
        /// </summary>
        private bool _inputMenuOpenedThisFrame;

        /// <summary>
        /// Animated input menu amount.
        /// </summary>
        private float _inputMenuAmount;

        /// <summary>
        /// Deferred input menu data drawn after the row pass.
        /// </summary>
        private DeviceMenuRequest _inputMenuRequest;

        /// <summary>
        /// Last observer unmute request status.
        /// </summary>
        private string _voiceRequestMessage = string.Empty;

        /// <summary>
        /// Gets or sets the active widget theme.
        /// </summary>
        public TimelineWidgetTheme Theme
        {
            get { return _theme != null ? _theme : TimelineWidgetTheme.LoadDefault(); }
            set { _theme = value; }
        }

        /// <summary>
        /// Sets the active widget theme.
        /// </summary>
        public void SetTheme(TimelineWidgetTheme theme)
        {
            _theme = theme;
        }

        /// <summary>
        /// Draws the widget and returns true when the close button is clicked.
        /// </summary>
        public bool Draw(Rect panelRect, float opacity = 1f)
        {
            if (panelRect.width <= 0f || panelRect.height <= 0f)
                return false;

            TimelineWidgetTheme theme = Theme;
            float alpha = Mathf.Clamp01(opacity);
            bool interactable = alpha > 0.92f;
            FuDrawList drawList = Fugui.GetCurrentWindowDrawList();

            if (!interactable)
                _inputMenuOpen = false;

            FlatCameraInputBlocker.RegisterRect(panelRect);

            Rect headerRect = new Rect(panelRect.x, panelRect.y, panelRect.width, HeaderHeight * Fugui.Scale);
            bool closeClicked = DrawHeader(drawList, headerRect, theme, alpha, interactable);

            Rect bodyRect = new Rect(panelRect.x, headerRect.yMax, panelRect.width, Mathf.Max(0f, panelRect.yMax - headerRect.yMax));
            DrawBody(drawList, bodyRect, theme, alpha, interactable);

            return closeClicked;
        }

        /// <summary>
        /// Draws the panel header.
        /// </summary>
        private bool DrawHeader(FuDrawList drawList, Rect rect, TimelineWidgetTheme theme, float alpha, bool interactable)
        {
            float scale = Fugui.Scale;
            Rect titleRect = new Rect(rect.x + 22f * scale, rect.y, rect.width - 82f * scale, rect.height);
            Rect closeRect = new Rect(rect.xMax - 54f * scale, rect.y + 20f * scale, 32f * scale, 32f * scale);

            PushFont(18, true);
            DrawTextLeftCentered(drawList, titleRect, "Network Voice", ColorU32(theme.Text, alpha), 0f);
            PopFont(true);

            drawList.AddLine(
                new Vector2(rect.x, rect.yMax),
                new Vector2(rect.xMax, rect.yMax),
                ColorU32(theme.DockBorder, alpha * 0.60f),
                Mathf.Max(1f, scale));

            return DrawCloseButton(drawList, closeRect, theme, alpha, interactable);
        }

        /// <summary>
        /// Draws the scrollable widget body.
        /// </summary>
        private void DrawBody(FuDrawList drawList, Rect bodyRect, TimelineWidgetTheme theme, float alpha, bool interactable)
        {
            float scale = Fugui.Scale;
            float contentHeight = CalculateBodyContentHeight(scale);
            float maxScroll = Mathf.Max(0f, contentHeight - bodyRect.height);
            if (maxScroll <= 0f)
            {
                _scrollY = 0f;
            }
            else if (interactable && bodyRect.Contains(Fugui.GetCurrentMouse().Position))
            {
                float wheel = Fugui.GetIO().MouseWheel;
                if (Mathf.Abs(wheel) > 0.001f)
                    _scrollY = Mathf.Clamp(_scrollY - wheel * 34f * scale, 0f, maxScroll);
            }

            _scrollY = Mathf.Clamp(_scrollY, 0f, maxScroll);
            InitializeToggleAnimationsIfNeeded();
            _inputMenuRequest = default;

            bool rowsInteractable = interactable && !_inputMenuOpen && _inputMenuAmount <= 0.001f;
            bool adminMuted = IsMutedBySessionAdmin(out string adminMuteLabel);
            bool displayedMuted = adminMuted || SaraVoiceSettings.Muted;
            bool observerMutedByAdmin = IsMutedObserverBySessionAdmin();
            bool observerAllowedToSpeak = IsObserverAllowedToSpeak();
            float y = bodyRect.y + 8f * scale - _scrollY;

            Fugui.PushClipRect(bodyRect.min, bodyRect.max, true);

            y = DrawSection(drawList, bodyRect, bodyRect.x, y, bodyRect.width, "V O I C E", theme, alpha);
            if (observerMutedByAdmin)
                y = DrawObserverUnmuteRequestRow(drawList, bodyRect, bodyRect.x, y, bodyRect.width, adminMuteLabel, theme, alpha, rowsInteractable);
            else if (observerAllowedToSpeak)
                y = DrawObserverSelfMuteRow(drawList, bodyRect, bodyRect.x, y, bodyRect.width, theme, alpha, rowsInteractable);
            else
                y = DrawToggleRow(drawList, bodyRect, bodyRect.x, y, bodyRect.width, "Mute", adminMuted ? adminMuteLabel : "Stop sending microphone audio", displayedMuted, ref _muteToggleAmount, theme, alpha, rowsInteractable && !adminMuted, SetMuted);
            y = DrawToggleRow(drawList, bodyRect, bodyRect.x, y, bodyRect.width, "Push to Talk", "Space or VR control", SaraVoiceSettings.PushToTalk, ref _pushToTalkToggleAmount, theme, alpha, rowsInteractable, SetPushToTalk);
            y = DrawThresholdRow(drawList, bodyRect, bodyRect.x, y, bodyRect.width, theme, alpha, rowsInteractable);

            y = DrawSection(drawList, bodyRect, bodyRect.x, y, bodyRect.width, "D E V I C E S", theme, alpha);
            y = DrawDeviceRow(drawList, bodyRect, bodyRect.x, y, bodyRect.width, "Input", "Microphone capture device", GetInputDeviceOptions(), SaraVoiceSettings.MicrophoneName, theme, alpha, interactable, true, SaraVoiceSettings.SelectMicrophone);
            DrawDeviceRow(drawList, bodyRect, bodyRect.x, y, bodyRect.width, "Output", "Unity system audio output", GetOutputDeviceOptions(), SaraVoiceSettings.OutputDeviceName, theme, alpha, rowsInteractable, false, (value, resetCapture) => SaraVoiceSettings.SelectOutputDevice(value));

            DrawInputMenuLayer(drawList);

            Fugui.PopClipRect();
        }

        /// <summary>
        /// Initializes toggle animation amounts from persisted state.
        /// </summary>
        private void InitializeToggleAnimationsIfNeeded()
        {
            if (_toggleAnimationsInitialized)
                return;

            _muteToggleAmount = SaraVoiceSettings.Muted ? 1f : 0f;
            _pushToTalkToggleAmount = SaraVoiceSettings.PushToTalk ? 1f : 0f;
            _toggleAnimationsInitialized = true;
        }

        /// <summary>
        /// Calculates the body content height.
        /// </summary>
        private static float CalculateBodyContentHeight(float scale)
        {
            return 8f * scale
                + SectionHeight * scale
                + RowHeight * 3f * scale
                + SectionHeight * scale
                + RowHeight * 2f * scale
                + 24f * scale;
        }

        /// <summary>
        /// Draws a section label row.
        /// </summary>
        private static float DrawSection(FuDrawList drawList, Rect clipRect, float x, float y, float width, string label, TimelineWidgetTheme theme, float alpha)
        {
            float scale = Fugui.Scale;
            Rect rect = new Rect(x, y, width, SectionHeight * scale);
            if (IsVisible(rect, clipRect))
            {
                Rect labelRect = new Rect(rect.x + 22f * scale, rect.y + 16f * scale, rect.width - 44f * scale, 14f * scale);
                PushFont(10, true);
                DrawTextLeftCentered(drawList, labelRect, label, ColorU32(theme.TextFaint, alpha), 0f);
                PopFont(true);
            }

            return rect.yMax;
        }

        /// <summary>
        /// Draws the observer self-mute row shown after admin allows microphone use.
        /// </summary>
        private float DrawObserverSelfMuteRow(
            FuDrawList drawList,
            Rect clipRect,
            float x,
            float y,
            float width,
            TimelineWidgetTheme theme,
            float alpha,
            bool interactable)
        {
            float scale = Fugui.Scale;
            Rect rect = new Rect(x, y, width, RowHeight * scale);
            if (IsVisible(rect, clipRect))
            {
                DrawRowTopDivider(drawList, rect, theme, alpha);

                Rect textRect = new Rect(rect.x + 22f * scale, rect.y, rect.width - 154f * scale, rect.height);
                string hint = string.IsNullOrWhiteSpace(_voiceRequestMessage) ? "Microphone allowed by admin" : _voiceRequestMessage;
                DrawSettingText(drawList, textRect, "Microphone", hint, theme, alpha, interactable);

                Rect buttonRect = new Rect(rect.xMax - 122f * scale, rect.y + (rect.height - 32f * scale) * 0.5f, 100f * scale, 32f * scale);
                if (DrawPillButton(drawList, buttonRect, "Mute", false, theme, alpha, interactable && IsMouseInClip(clipRect)))
                    ObserverMuteSelf();
            }

            return rect.yMax;
        }

        /// <summary>
        /// Draws the observer request-to-talk row shown while admin muted.
        /// </summary>
        private float DrawObserverUnmuteRequestRow(
            FuDrawList drawList,
            Rect clipRect,
            float x,
            float y,
            float width,
            string adminMuteLabel,
            TimelineWidgetTheme theme,
            float alpha,
            bool interactable)
        {
            float scale = Fugui.Scale;
            Rect rect = new Rect(x, y, width, RowHeight * scale);
            if (IsVisible(rect, clipRect))
            {
                DrawRowTopDivider(drawList, rect, theme, alpha);

                SaraUser user = Sara.CurrentSession != null ? Sara.CurrentSession.User : null;
                bool requestPending = user != null && user.WantsUnmute;
                string hint = requestPending
                    ? "Request sent to admin"
                    : string.IsNullOrWhiteSpace(_voiceRequestMessage) ? adminMuteLabel : _voiceRequestMessage;

                Rect textRect = new Rect(rect.x + 22f * scale, rect.y, rect.width - 154f * scale, rect.height);
                DrawSettingText(drawList, textRect, "Microphone", hint, theme, alpha, interactable);

                Rect buttonRect = new Rect(rect.xMax - 122f * scale, rect.y + (rect.height - 32f * scale) * 0.5f, 100f * scale, 32f * scale);
                if (DrawPillButton(drawList, buttonRect, requestPending ? "Requested" : "Ask", requestPending, theme, alpha, interactable && IsMouseInClip(clipRect) && !requestPending))
                    RequestUnmute();
            }

            return rect.yMax;
        }

        /// <summary>
        /// Draws one toggle row.
        /// </summary>
        private float DrawToggleRow(
            FuDrawList drawList,
            Rect clipRect,
            float x,
            float y,
            float width,
            string label,
            string hint,
            bool value,
            ref float toggleAmount,
            TimelineWidgetTheme theme,
            float alpha,
            bool interactable,
            Action<bool> onChanged)
        {
            float scale = Fugui.Scale;
            Rect rect = new Rect(x, y, width, RowHeight * scale);

            if (IsVisible(rect, clipRect))
            {
                DrawRowTopDivider(drawList, rect, theme, alpha);

                Rect textRect = new Rect(rect.x + 22f * scale, rect.y, rect.width - 112f * scale, rect.height);
                DrawSettingText(drawList, textRect, label, hint, theme, alpha, interactable);

                Rect toggleRect = new Rect(rect.xMax - 70f * scale, rect.y + (rect.height - 28f * scale) * 0.5f, 48f * scale, 28f * scale);
                if (DrawToggle(drawList, toggleRect, value, ref toggleAmount, theme, alpha, interactable && IsMouseInClip(clipRect)))
                    onChanged(!value);
            }

            return rect.yMax;
        }

        /// <summary>
        /// Draws the voice detection threshold row.
        /// </summary>
        private float DrawThresholdRow(FuDrawList drawList, Rect clipRect, float x, float y, float width, TimelineWidgetTheme theme, float alpha, bool interactable)
        {
            float scale = Fugui.Scale;
            Rect rect = new Rect(x, y, width, RowHeight * scale);
            if (!IsVisible(rect, clipRect))
                return rect.yMax;

            DrawRowTopDivider(drawList, rect, theme, alpha);

            float segmentedWidth = Mathf.Min(204f * scale, rect.width - 44f * scale);
            Rect textRect = new Rect(rect.x + 22f * scale, rect.y, Mathf.Max(1f, rect.width - segmentedWidth - 56f * scale), rect.height);
            DrawSettingText(drawList, textRect, "Voice Threshold", "VAD sensitivity", theme, alpha, interactable);

            Rect segmentedRect = new Rect(rect.xMax - 22f * scale - segmentedWidth, rect.y + (rect.height - 32f * scale) * 0.5f, segmentedWidth, 32f * scale);
            DrawThresholdControl(drawList, segmentedRect, clipRect, theme, alpha, interactable);

            return rect.yMax;
        }

        /// <summary>
        /// Draws the threshold segmented control.
        /// </summary>
        private void DrawThresholdControl(FuDrawList drawList, Rect rect, Rect clipRect, TimelineWidgetTheme theme, float alpha, bool interactable)
        {
            float scale = Fugui.Scale;
            float rounding = rect.height * 0.5f;
            VadSensitivityLevels current = SaraVoiceSettings.VadSensitivity;

            FlatCameraInputBlocker.RegisterRect(rect);
            drawList.AddRectFilled(rect.min, rect.max, ColorU32(theme.SettingsDropdownBackground, alpha), rounding);
            drawList.AddRect(rect.min, rect.max, ColorU32(theme.DockBorder, alpha), rounding);

            for (int i = 0; i < ThresholdSensitivityValues.Length; i++)
            {
                VadSensitivityLevels option = ThresholdSensitivityValues[i];
                float segmentWidth = rect.width / ThresholdSensitivityValues.Length;
                Rect optionRect = new Rect(rect.x + segmentWidth * i, rect.y, segmentWidth, rect.height);
                bool selected = current == option;
                bool hovered = interactable && optionRect.Contains(Fugui.GetCurrentMouse().Position) && IsMouseInClip(clipRect);
                bool active = hovered && Fugui.GetCurrentMouse().IsPressed(FuMouseButton.Left);
                Color bg = selected ? theme.PillBackgroundActive : active ? theme.PillBackgroundActive : hovered ? theme.PillBackgroundHover : Color.clear;

                if (bg.a > 0f)
                    drawList.AddRectFilled(optionRect.min + new Vector2(2f * scale, 2f * scale), optionRect.max - new Vector2(2f * scale, 2f * scale), ColorU32(bg, alpha), theme.SmallRadius * scale);

                if (i > 0)
                {
                    drawList.AddLine(
                        new Vector2(optionRect.x, rect.y + 7f * scale),
                        new Vector2(optionRect.x, rect.yMax - 7f * scale),
                        ColorU32(theme.DockBorder, alpha * 0.75f),
                        Mathf.Max(1f, scale));
                }

                PushFont(12, selected);
                DrawTextCentered(drawList, optionRect, ThresholdLabels[i], ColorU32(selected ? theme.Accent : interactable ? theme.TextDim : theme.TextFaint, alpha));
                PopFont(selected);

                if (!hovered)
                    continue;

                Fugui.SetMouseCursor(FuMouseCursor.Hand);
                if (Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left))
                    SaraVoiceSettings.VadSensitivity = option;
            }
        }

        /// <summary>
        /// Draws one device selection row.
        /// </summary>
        private float DrawDeviceRow(
            FuDrawList drawList,
            Rect clipRect,
            float x,
            float y,
            float width,
            string label,
            string hint,
            List<DeviceOption> options,
            string currentValue,
            TimelineWidgetTheme theme,
            float alpha,
            bool interactable,
            bool openable,
            Action<string, bool> onSelected)
        {
            float scale = Fugui.Scale;
            Rect rect = new Rect(x, y, width, RowHeight * scale);
            if (!IsVisible(rect, clipRect))
                return rect.yMax;

            DrawRowTopDivider(drawList, rect, theme, alpha);
            Rect textRect = new Rect(rect.x + 22f * scale, rect.y, rect.width - 214f * scale, rect.height);
            DrawSettingText(drawList, textRect, label, hint, theme, alpha, interactable);

            float dropdownWidth = Mathf.Min(184f * scale, rect.width - 44f * scale);
            Rect dropdownRect = new Rect(rect.xMax - 22f * scale - dropdownWidth, rect.y + (rect.height - 32f * scale) * 0.5f, dropdownWidth, 32f * scale);
            if (openable)
                DrawInputDropdown(drawList, dropdownRect, clipRect, options, currentValue, theme, alpha, interactable && options.Count > 1, onSelected);
            else
                DrawStaticDeviceDropdown(drawList, dropdownRect, options, currentValue, theme, alpha);

            return rect.yMax;
        }

        /// <summary>
        /// Draws a non-interactive device value pill.
        /// </summary>
        private static void DrawStaticDeviceDropdown(FuDrawList drawList, Rect rect, List<DeviceOption> options, string currentValue, TimelineWidgetTheme theme, float alpha)
        {
            float scale = Fugui.Scale;
            string label = GetDeviceLabel(options, currentValue);

            FlatCameraInputBlocker.RegisterRect(rect);
            drawList.AddRectFilled(rect.min, rect.max, ColorU32(theme.SettingsDropdownBackground, alpha), rect.height * 0.5f);
            drawList.AddRect(rect.min, rect.max, ColorU32(theme.DockBorder, alpha), rect.height * 0.5f);

            PushFont(12, true);
            Rect labelRect = new Rect(rect.x + 14f * scale, rect.y, rect.width - 28f * scale, rect.height);
            DrawTextLeftCentered(drawList, labelRect, ClipTextToWidth(label, labelRect.width), ColorU32(theme.TextFaint, alpha), 0f);
            PopFont(true);
        }

        /// <summary>
        /// Draws a device dropdown trigger.
        /// </summary>
        private void DrawInputDropdown(
            FuDrawList drawList,
            Rect triggerRect,
            Rect clipRect,
            List<DeviceOption> options,
            string currentValue,
            TimelineWidgetTheme theme,
            float alpha,
            bool interactable,
            Action<string, bool> onSelected)
        {
            bool hovered = interactable && triggerRect.Contains(Fugui.GetCurrentMouse().Position) && IsMouseInClip(clipRect);
            bool active = hovered && Fugui.GetCurrentMouse().IsPressed(FuMouseButton.Left);
            bool clicked = hovered && Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left);
            string label = GetDeviceLabel(options, currentValue);
            Color bg = active || _inputMenuOpen ? theme.PillBackgroundActive : hovered ? theme.PillBackgroundHover : theme.SettingsDropdownBackground;
            float scale = Fugui.Scale;

            FlatCameraInputBlocker.RegisterRect(triggerRect);
            drawList.AddRectFilled(triggerRect.min, triggerRect.max, ColorU32(bg, alpha), triggerRect.height * 0.5f);
            drawList.AddRect(triggerRect.min, triggerRect.max, ColorU32(theme.DockBorder, alpha), triggerRect.height * 0.5f);

            PushFont(12, true);
            Rect labelRect = new Rect(triggerRect.x + 14f * scale, triggerRect.y, triggerRect.width - 38f * scale, triggerRect.height);
            DrawTextLeftCentered(drawList, labelRect, ClipTextToWidth(label, labelRect.width), ColorU32(interactable ? theme.Text : theme.TextFaint, alpha), 0f);
            PopFont(true);

            Rect arrowRect = new Rect(triggerRect.xMax - 26f * scale, triggerRect.y, 14f * scale, triggerRect.height);
            DrawChevron(drawList, arrowRect, _inputMenuOpen, interactable ? theme.TextDim : theme.TextFaint, alpha);

            if (hovered)
                Fugui.SetMouseCursor(FuMouseCursor.Hand);

            if (clicked)
            {
                _inputMenuOpen = !_inputMenuOpen;
                _inputMenuOpenedThisFrame = _inputMenuOpen;
            }

            _inputMenuRequest = new DeviceMenuRequest
            {
                HasValue = true,
                TriggerRect = triggerRect,
                ClipRect = clipRect,
                Options = options,
                CurrentValue = currentValue,
                Theme = theme,
                Alpha = alpha,
                Interactable = interactable,
                OnSelected = onSelected
            };
        }

        /// <summary>
        /// Draws the deferred input device menu layer.
        /// </summary>
        private void DrawInputMenuLayer(FuDrawList drawList)
        {
            if (!_inputMenuRequest.HasValue)
            {
                _inputMenuOpen = false;
                _inputMenuOpenedThisFrame = false;
                return;
            }

            DeviceMenuRequest request = _inputMenuRequest;
            DrawInputMenu(drawList, request);
            _inputMenuRequest = default;
        }

        /// <summary>
        /// Draws the input device dropdown menu.
        /// </summary>
        private void DrawInputMenu(FuDrawList drawList, DeviceMenuRequest request)
        {
            float step = Time.unscaledDeltaTime / Mathf.Max(0.001f, request.Theme.SpeedPopupTransitionSeconds);
            _inputMenuAmount = Mathf.MoveTowards(_inputMenuAmount, _inputMenuOpen ? 1f : 0f, step);

            if (!_inputMenuOpen && _inputMenuAmount <= 0.001f)
                return;

            float scale = Fugui.Scale;
            float t = SmoothStep01(_inputMenuAmount);
            float menuWidth = Mathf.Max(request.TriggerRect.width, 190f * scale);
            float menuHeight = Mathf.Min(request.Options.Count, 6) * DropdownOptionHeight * scale + 10f * scale;
            float menuY = request.TriggerRect.yMax + 6f * scale;
            if (menuY + menuHeight > request.ClipRect.yMax - 6f * scale)
                menuY = request.TriggerRect.y - menuHeight - 6f * scale;

            Rect targetRect = new Rect(request.TriggerRect.xMax - menuWidth, menuY, menuWidth, menuHeight);
            Rect closedRect = new Rect(request.TriggerRect.xMax - menuWidth * 0.80f, request.TriggerRect.y + request.TriggerRect.height * 0.25f, menuWidth * 0.80f, request.TriggerRect.height * 0.55f);
            Rect menuRect = LerpRect(closedRect, targetRect, t);
            float menuAlpha = request.Alpha * t;

            FlatCameraInputBlocker.RegisterRect(menuRect);
            drawList.AddRectFilled(menuRect.min, menuRect.max, ColorU32(request.Theme.MenuBackground, menuAlpha), request.Theme.MediumRadius * scale);
            drawList.AddRect(menuRect.min, menuRect.max, ColorU32(request.Theme.DockBorder, menuAlpha), request.Theme.MediumRadius * scale);

            Fugui.PushClipRect(menuRect.min, menuRect.max, true);
            for (int i = 0; i < request.Options.Count; i++)
            {
                DeviceOption option = request.Options[i];
                Rect optionRect = new Rect(
                    menuRect.x + 5f * scale,
                    menuRect.y + 5f * scale + i * DropdownOptionHeight * scale,
                    menuRect.width - 10f * scale,
                    DropdownOptionHeight * scale);
                DrawInputMenuOption(drawList, optionRect, option, request, menuAlpha);
            }
            Fugui.PopClipRect();

            if (!_inputMenuOpenedThisFrame
                && _inputMenuOpen
                && request.Interactable
                && Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left)
                && !menuRect.Contains(Fugui.GetCurrentMouse().Position)
                && !request.TriggerRect.Contains(Fugui.GetCurrentMouse().Position))
            {
                _inputMenuOpen = false;
            }

            _inputMenuOpenedThisFrame = false;
        }

        /// <summary>
        /// Draws one input menu option.
        /// </summary>
        private void DrawInputMenuOption(FuDrawList drawList, Rect optionRect, DeviceOption option, DeviceMenuRequest request, float menuAlpha)
        {
            float scale = Fugui.Scale;
            bool selected = SameDeviceValue(option.Value, request.CurrentValue);
            bool canInteract = request.Interactable && option.Enabled && _inputMenuOpen && _inputMenuAmount > 0.92f && IsMouseInClip(request.ClipRect);
            bool hovered = canInteract && optionRect.Contains(Fugui.GetCurrentMouse().Position);
            bool active = hovered && Fugui.GetCurrentMouse().IsPressed(FuMouseButton.Left);
            Color bg = active ? request.Theme.PillBackgroundActive : hovered ? request.Theme.PillBackgroundHover : Color.clear;

            if (bg.a > 0f)
                drawList.AddRectFilled(optionRect.min, optionRect.max, ColorU32(bg, menuAlpha), request.Theme.SmallRadius * scale);

            PushFont(12, selected);
            Rect labelRect = new Rect(optionRect.x + 9f * scale, optionRect.y, optionRect.width - 34f * scale, optionRect.height);
            Color labelColor = selected ? request.Theme.Accent : option.Enabled ? request.Theme.TextDim : request.Theme.TextFaint;
            DrawTextLeftCentered(drawList, labelRect, ClipTextToWidth(option.Label, labelRect.width), ColorU32(labelColor, menuAlpha), 0f);
            PopFont(selected);

            if (selected)
                DrawCheckIcon(drawList, new Rect(optionRect.xMax - 25f * scale, optionRect.y, 14f * scale, optionRect.height), request.Theme.Accent, menuAlpha);

            if (!hovered)
                return;

            Fugui.SetMouseCursor(FuMouseCursor.Hand);
            if (Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left))
            {
                request.OnSelected(option.Value, true);
                _inputMenuOpen = false;
            }
        }

        /// <summary>
        /// Sets the persisted user mute setting.
        /// </summary>
        private static void SetMuted(bool value)
        {
            SaraVoiceSettings.Muted = value;
        }

        /// <summary>
        /// Sets the persisted push-to-talk setting.
        /// </summary>
        private static void SetPushToTalk(bool value)
        {
            SaraVoiceSettings.PushToTalk = value;
        }

        /// <summary>
        /// Requests observer speaking permission from the admin.
        /// </summary>
        private void RequestUnmute()
        {
            if (Sara.Network == null)
                return;

            _voiceRequestMessage = "Sending request...";
            Sara.Network.RequestUnmute((response) =>
            {
                if (response == null)
                {
                    _voiceRequestMessage = "Request failed.";
                    return;
                }

                _voiceRequestMessage = response.Success
                    ? "Request sent to admin"
                    : string.IsNullOrWhiteSpace(response.Message) ? "Request rejected." : response.Message;
            });
        }

        /// <summary>
        /// Mutes the local observer through session state.
        /// </summary>
        private void ObserverMuteSelf()
        {
            if (Sara.Network == null)
                return;

            _voiceRequestMessage = "Muting microphone...";
            Sara.Network.ObserverMuteSelf((response) =>
            {
                if (response == null)
                {
                    _voiceRequestMessage = "Mute failed.";
                    return;
                }

                _voiceRequestMessage = response.Success
                    ? string.Empty
                    : string.IsNullOrWhiteSpace(response.Message) ? "Mute rejected." : response.Message;
            });
        }

        /// <summary>
        /// Returns whether the local user's microphone is force muted by session moderation.
        /// </summary>
        private static bool IsMutedBySessionAdmin(out string label)
        {
            label = "Muted by Admin";
            SaraSession session = Sara.CurrentSession;
            SaraUser localUser = session != null ? session.User : null;
            if (session == null || !session.IsMultiplayer || localUser == null || !localUser.IsMuted)
                return false;

            label = "Muted by " + GetSessionAdminName(session);
            return true;
        }

        /// <summary>
        /// Returns whether the local user is a muted observer.
        /// </summary>
        private static bool IsMutedObserverBySessionAdmin()
        {
            SaraSession session = Sara.CurrentSession;
            SaraUser localUser = session != null ? session.User : null;
            return session != null
                && session.IsMultiplayer
                && localUser != null
                && localUser.IsObservator
                && localUser.IsMuted;
        }

        /// <summary>
        /// Returns whether the local observer is currently allowed to speak by admin.
        /// </summary>
        private static bool IsObserverAllowedToSpeak()
        {
            SaraSession session = Sara.CurrentSession;
            SaraUser localUser = session != null ? session.User : null;
            return session != null
                && session.IsMultiplayer
                && localUser != null
                && localUser.IsObservator
                && !localUser.IsMuted;
        }

        /// <summary>
        /// Returns the best display name for the session admin.
        /// </summary>
        private static string GetSessionAdminName(SaraSession session)
        {
            if (session == null || session.Users == null)
                return "Admin";

            for (int i = 0; i < session.Users.Length; i++)
            {
                SaraSessionUser sessionUser = session.Users[i];
                if (sessionUser == null || sessionUser.User == null || !sessionUser.User.IsAdmin)
                    continue;

                if (!string.IsNullOrWhiteSpace(sessionUser.User.Name))
                    return sessionUser.User.Name.Trim();

                return "Admin";
            }

            return "Admin";
        }

        /// <summary>
        /// Builds the input device option list.
        /// </summary>
        private static List<DeviceOption> GetInputDeviceOptions()
        {
            List<DeviceOption> options = new List<DeviceOption>();
            List<string> devices = new List<string>();
            DissonanceComms comms = DissonanceComms.GetSingleton();

            options.Add(new DeviceOption { Label = SystemDefaultLabel, Value = null, Enabled = true });

            // Prefer Dissonance's device provider, then fall back to Unity's microphone list.
            if (comms != null)
                comms.GetMicrophoneDevices(devices);
            else
                devices.AddRange(Microphone.devices);

            for (int i = 0; i < devices.Count; i++)
            {
                string device = devices[i];
                if (string.IsNullOrWhiteSpace(device) || ContainsDeviceValue(options, device))
                    continue;

                options.Add(new DeviceOption { Label = device, Value = device, Enabled = true });
            }

            string selected = SaraVoiceSettings.MicrophoneName;
            if (!string.IsNullOrWhiteSpace(selected) && !ContainsDeviceValue(options, selected))
                options.Add(new DeviceOption { Label = selected + " (missing)", Value = selected, Enabled = false });

            return options;
        }

        /// <summary>
        /// Builds the output device option list.
        /// </summary>
        private static List<DeviceOption> GetOutputDeviceOptions()
        {
            // Dissonance playback is routed through Unity, which exposes only system output here.
            return new List<DeviceOption>
            {
                new DeviceOption { Label = SystemDefaultLabel, Value = null, Enabled = true }
            };
        }

        /// <summary>
        /// Returns the visible label for a device value.
        /// </summary>
        private static string GetDeviceLabel(List<DeviceOption> options, string value)
        {
            for (int i = 0; i < options.Count; i++)
            {
                if (SameDeviceValue(options[i].Value, value))
                    return options[i].Label;
            }

            return string.IsNullOrWhiteSpace(value) ? SystemDefaultLabel : value;
        }

        /// <summary>
        /// Returns whether the option list already contains a device value.
        /// </summary>
        private static bool ContainsDeviceValue(List<DeviceOption> options, string value)
        {
            for (int i = 0; i < options.Count; i++)
            {
                if (SameDeviceValue(options[i].Value, value))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns whether two device values refer to the same selection.
        /// </summary>
        private static bool SameDeviceValue(string a, string b)
        {
            return string.Equals(NormalizeDeviceValue(a), NormalizeDeviceValue(b), StringComparison.Ordinal);
        }

        /// <summary>
        /// Returns the normalized device value.
        /// </summary>
        private static string NormalizeDeviceValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        /// <summary>
        /// Draws the close button.
        /// </summary>
        private static bool DrawCloseButton(FuDrawList drawList, Rect rect, TimelineWidgetTheme theme, float alpha, bool interactable)
        {
            bool hovered = interactable && rect.Contains(Fugui.GetCurrentMouse().Position);
            bool active = hovered && Fugui.GetCurrentMouse().IsPressed(FuMouseButton.Left);
            bool clicked = hovered && Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left);
            float scale = Fugui.Scale;
            Color bg = hovered || active ? theme.SettingsCloseBackgroundHover : theme.SettingsCloseBackground;
            Color iconColor = hovered || active ? theme.Text : theme.TextDim;

            FlatCameraInputBlocker.RegisterRect(rect);
            drawList.AddRectFilled(rect.min, rect.max, ColorU32(bg, alpha), 8f * scale);
            drawList.AddRect(rect.min, rect.max, ColorU32(theme.DockBorder, alpha), 8f * scale);

            float pad = 10f * scale;
            float thickness = Mathf.Max(2f * scale, 1f);
            uint col = ColorU32(iconColor, alpha);
            drawList.AddLine(rect.min + new Vector2(pad, pad), rect.max - new Vector2(pad, pad), col, thickness);
            drawList.AddLine(new Vector2(rect.xMax - pad, rect.y + pad), new Vector2(rect.x + pad, rect.yMax - pad), col, thickness);

            if (hovered)
                Fugui.SetMouseCursor(FuMouseCursor.Hand);

            return clicked;
        }

        /// <summary>
        /// Draws a toggle control.
        /// </summary>
        private static bool DrawToggle(FuDrawList drawList, Rect rect, bool value, ref float amount, TimelineWidgetTheme theme, float alpha, bool interactable)
        {
            bool hovered = interactable && rect.Contains(Fugui.GetCurrentMouse().Position);
            bool active = hovered && Fugui.GetCurrentMouse().IsPressed(FuMouseButton.Left);
            bool clicked = hovered && Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left);
            float scale = Fugui.Scale;
            float rounding = rect.height * 0.5f;
            float step = Time.unscaledDeltaTime / Mathf.Max(0.001f, theme.SettingsToggleTransitionSeconds);
            amount = Mathf.MoveTowards(amount, value ? 1f : 0f, step);
            float t = SmoothStep01(amount);
            Color offColor = hovered || active ? theme.PillBackgroundHover : theme.SettingsToggleOff;
            Color onColor = active ? theme.AccentHi : theme.Accent;
            Color trackColor = Color.Lerp(offColor, onColor, t);

            FlatCameraInputBlocker.RegisterRect(rect);
            drawList.AddRectFilled(rect.min, rect.max, ColorU32(trackColor, alpha), rounding);

            float knobDiameter = rect.height - 4f * scale;
            float knobWidth = active ? Mathf.Min(knobDiameter + 4f * scale, rect.width - 4f * scale) : knobDiameter;
            float knobX = Mathf.Lerp(rect.x + 2f * scale, rect.xMax - knobWidth - 2f * scale, t);
            Rect knobRect = new Rect(knobX, rect.y + 2f * scale, knobWidth, knobDiameter);
            drawList.AddRectFilled(knobRect.min, knobRect.max, ColorU32(theme.SettingsToggleKnob, alpha), knobDiameter * 0.5f);

            if (hovered)
                Fugui.SetMouseCursor(FuMouseCursor.Hand);

            return clicked;
        }

        /// <summary>
        /// Draws a compact pill action button.
        /// </summary>
        private static bool DrawPillButton(FuDrawList drawList, Rect rect, string label, bool selected, TimelineWidgetTheme theme, float alpha, bool interactable)
        {
            bool hovered = interactable && rect.Contains(Fugui.GetCurrentMouse().Position);
            bool active = hovered && Fugui.GetCurrentMouse().IsPressed(FuMouseButton.Left);
            bool clicked = hovered && Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left);
            Color background = selected ? theme.PillBackgroundActive : active ? theme.PillBackgroundActive : hovered ? theme.PillBackgroundHover : theme.SettingsDropdownBackground;
            Color textColor = interactable || selected ? (selected ? theme.Accent : theme.Text) : theme.TextFaint;

            FlatCameraInputBlocker.RegisterRect(rect);
            drawList.AddRectFilled(rect.min, rect.max, ColorU32(background, alpha), rect.height * 0.5f);
            drawList.AddRect(rect.min, rect.max, ColorU32(theme.DockBorder, alpha), rect.height * 0.5f);

            PushFont(12, true);
            DrawTextCentered(drawList, rect, ClipTextToWidth(label, rect.width - 14f * Fugui.Scale), ColorU32(textColor, alpha));
            PopFont(true);

            if (hovered)
                Fugui.SetMouseCursor(FuMouseCursor.Hand);

            return clicked;
        }

        /// <summary>
        /// Draws the primary and secondary text for a row.
        /// </summary>
        private static void DrawSettingText(FuDrawList drawList, Rect rect, string label, string hint, TimelineWidgetTheme theme, float alpha, bool enabled)
        {
            float scale = Fugui.Scale;
            Color labelColor = enabled ? theme.Text : theme.TextDim;
            Color hintColor = enabled ? theme.TextFaint : WithAlpha(theme.TextFaint, 0.65f);

            PushFont(18, true);
            Rect labelRect = new Rect(rect.x, rect.y + 10f * scale, rect.width, 19f * scale);
            DrawTextLeftCentered(drawList, labelRect, ClipTextToWidth(label, labelRect.width), ColorU32(labelColor, alpha), 0f);
            PopFont(true);

            PushFont(12, false);
            Rect hintRect = new Rect(rect.x, labelRect.yMax + 1f * scale, rect.width, 16f * scale);
            DrawTextLeftCentered(drawList, hintRect, ClipTextToWidth(hint, hintRect.width), ColorU32(hintColor, alpha), 0f);
            PopFont(false);
        }

        /// <summary>
        /// Draws the top divider for a row.
        /// </summary>
        private static void DrawRowTopDivider(FuDrawList drawList, Rect rect, TimelineWidgetTheme theme, float alpha)
        {
            drawList.AddLine(rect.min, new Vector2(rect.xMax, rect.y), ColorU32(theme.SettingsRowDivider, alpha), Mathf.Max(1f, Fugui.Scale));
        }

        /// <summary>
        /// Draws a chevron icon.
        /// </summary>
        private static void DrawChevron(FuDrawList drawList, Rect rect, bool up, Color color, float alpha)
        {
            float scale = Fugui.Scale;
            Vector2 center = rect.center;
            float halfWidth = 5f * scale;
            float halfHeight = 3.5f * scale;
            float sign = up ? -1f : 1f;
            uint col = ColorU32(color, alpha);
            float thickness = Mathf.Max(1.7f * scale, 1f);

            Vector2 tip = new Vector2(center.x, center.y + sign * halfHeight);
            drawList.AddLine(new Vector2(center.x - halfWidth, center.y - sign * halfHeight), tip, col, thickness);
            drawList.AddLine(tip, new Vector2(center.x + halfWidth, center.y - sign * halfHeight), col, thickness);
        }

        /// <summary>
        /// Draws a check icon.
        /// </summary>
        private static void DrawCheckIcon(FuDrawList drawList, Rect rect, Color color, float alpha)
        {
            float scale = Fugui.Scale;
            Vector2 center = rect.center;
            uint col = ColorU32(color, alpha);
            float thickness = Mathf.Max(1.8f * scale, 1f);
            drawList.AddLine(center + new Vector2(-5f * scale, 0f), center + new Vector2(-1f * scale, 4f * scale), col, thickness);
            drawList.AddLine(center + new Vector2(-1f * scale, 4f * scale), center + new Vector2(7f * scale, -5f * scale), col, thickness);
        }

        /// <summary>
        /// Returns whether a row is visible in a clip rect.
        /// </summary>
        private static bool IsVisible(Rect rect, Rect clipRect)
        {
            return rect.yMax >= clipRect.y && rect.y <= clipRect.yMax;
        }

        /// <summary>
        /// Returns whether the mouse is inside a clip rect.
        /// </summary>
        private static bool IsMouseInClip(Rect clipRect)
        {
            return clipRect.Contains(Fugui.GetCurrentMouse().Position);
        }

        /// <summary>
        /// Converts a color to an ImGui packed color.
        /// </summary>
        private static uint ColorU32(Color color)
        {
            return Fugui.GetColorU32(new Vector4(color.r, color.g, color.b, color.a));
        }

        /// <summary>
        /// Converts a color and opacity to an ImGui packed color.
        /// </summary>
        private static uint ColorU32(Color color, float opacity)
        {
            return Fugui.GetColorU32(new Vector4(color.r, color.g, color.b, color.a * Mathf.Clamp01(opacity)));
        }

        /// <summary>
        /// Returns a copy of the color with a different alpha.
        /// </summary>
        private static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
        }

        /// <summary>
        /// Smooths a normalized value.
        /// </summary>
        private static float SmoothStep01(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * (3f - 2f * t);
        }

        /// <summary>
        /// Interpolates between two rects.
        /// </summary>
        private static Rect LerpRect(Rect from, Rect to, float t)
        {
            return new Rect(
                Mathf.Lerp(from.x, to.x, t),
                Mathf.Lerp(from.y, to.y, t),
                Mathf.Lerp(from.width, to.width, t),
                Mathf.Lerp(from.height, to.height, t));
        }

        /// <summary>
        /// Pushes a Fugui font and optional bold face.
        /// </summary>
        private static void PushFont(int size, bool bold)
        {
            Fugui.PushFont(size);
            if (bold)
                Fugui.PushFont(FontType.Bold);
        }

        /// <summary>
        /// Pops a Fugui font and optional bold face.
        /// </summary>
        private static void PopFont(bool bold)
        {
            if (bold)
                Fugui.PopFont();
            Fugui.PopFont();
        }

        /// <summary>
        /// Draws left-aligned vertically centered text.
        /// </summary>
        private static void DrawTextLeftCentered(FuDrawList drawList, Rect rect, string text, uint color, float padding)
        {
            Vector2 textSize = Fugui.CalcTextSize(text);
            Vector2 textPos = new Vector2(rect.x + padding, rect.y + (rect.height - textSize.y) * 0.5f);
            drawList.AddText(textPos, color, text);
        }

        /// <summary>
        /// Draws centered text.
        /// </summary>
        private static void DrawTextCentered(FuDrawList drawList, Rect rect, string text, uint color)
        {
            Vector2 textSize = Fugui.CalcTextSize(text);
            Vector2 textPos = new Vector2(rect.x + (rect.width - textSize.x) * 0.5f, rect.y + (rect.height - textSize.y) * 0.5f);
            drawList.AddText(textPos, color, text);
        }

        /// <summary>
        /// Clips text to the requested width.
        /// </summary>
        private static string ClipTextToWidth(string text, float maxWidth)
        {
            if (string.IsNullOrEmpty(text) || Fugui.CalcTextSize(text).x <= maxWidth)
                return text;

            const string suffix = "...";
            float suffixWidth = Fugui.CalcTextSize(suffix).x;
            if (suffixWidth >= maxWidth)
                return suffix;

            int bestLength = 0;
            int low = 1;
            int high = text.Length - 1;
            while (low <= high)
            {
                int mid = low + ((high - low) / 2);
                string candidate = text.Substring(0, mid).TrimEnd() + suffix;
                if (Fugui.CalcTextSize(candidate).x <= maxWidth)
                {
                    bestLength = mid;
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return bestLength > 0 ? text.Substring(0, bestLength).TrimEnd() + suffix : suffix;
        }
    }
}
