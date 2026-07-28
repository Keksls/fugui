using Fu;
using Fu.Framework;

using Assets.Scripts.UI.Shortcuts;
using Saravr.Core;
using Saravr.Core.Performance;
using Saravr.Engine.RunwayLights;
using Saravr.Engine.Weather;
using Saravr.Network.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.UI.Windows.Common
{
    /// <summary>
    /// Implements the settings widget logic.
    /// </summary>
    public sealed class SettingsWidget
    {
        private const int MinutesInDay = 24 * 60;
        private const float RowHeight = 52f;
        private const float SliderRowHeight = 106f;
        private const float SectionHeight = 40f;
        private const float HeaderHeight = 72f;
        private const float DropdownOptionHeight = 34f;
        private static readonly SaraXrRenderPriority[] XrRenderPriorities =
        {
            SaraXrRenderPriority.QualityGround,
            SaraXrRenderPriority.Balanced,
            SaraXrRenderPriority.Cockpit
        };

        /// <summary>
        /// Stores performance menu request data.
        /// </summary>
        private struct PerformanceMenuRequest
        {
            public bool HasValue;
            public Rect TriggerRect;
            public Rect ClipRect;
            public List<SaraPerformanceProfile> Profiles;
            public SaraPerformanceProfile CurrentProfile;
            public TimelineWidgetTheme Theme;
            public float Alpha;
            public bool Interactable;
        }

        /// <summary>
        /// Stores weather preset menu request data.
        /// </summary>
        private struct WeatherPresetMenuRequest
        {
            public bool HasValue;
            public Rect TriggerRect;
            public Rect ClipRect;
            public TimelineWidgetTheme Theme;
            public float Alpha;
            public bool Interactable;
        }

        private TimelineWidgetTheme _theme;
        private bool _toggleAnimationsInitialized;
        private float _flightPathToggleAmount;
        private float _eventsToggleAmount;
        private float _ilsToggleAmount;
        private float _sticksGazHighlightToggleAmount;
        private float _weatherToggleAmount;
        private bool _weatherPresetMenuOpen;
        private bool _weatherPresetMenuOpenedThisFrame;
        private float _weatherPresetMenuAmount;
        private WeatherPresetMenuRequest _weatherPresetMenuRequest;
        private float _runwayLightsToggleAmount;
        private float _hdrToggleAmount;
        private bool _timeDragging;
        private bool _performanceMenuOpen;
        private bool _performanceMenuOpenedThisFrame;
        private float _performanceMenuAmount;
        private float _scrollY;
        private bool _scrollbarDragging;
        private float _scrollbarDragOffsetY;
        private PerformanceMenuRequest _performanceMenuRequest;

        public TimelineWidgetTheme Theme
        {
            get { return _theme != null ? _theme : TimelineWidgetTheme.LoadDefault(); }
            set { _theme = value; }
        }
        /// <summary>
        /// Raised when the shortcut settings popup should open.
        /// </summary>
        public Action OnShortcutSettingsRequested;

        #region Settings Panel Entry
        /// <summary>
        /// Sets the theme value.
        /// </summary>
        public void SetTheme(TimelineWidgetTheme theme)
        {
            _theme = theme;
        }

        /// <summary>
        /// Runs the draw logic.
        /// </summary>
        public bool Draw(Rect panelRect, float opacity = 1f)
        {
            if (panelRect.width <= 0f || panelRect.height <= 0f)
                return false;

            TimelineWidgetTheme theme = Theme;
            float scale = Fugui.Scale;
            float alpha = Mathf.Clamp01(opacity);
            bool interactable = alpha > 0.92f;
            FuDrawList drawList = Fugui.GetCurrentWindowDrawList();

            if (!interactable)
            {
                _timeDragging = false;
                _scrollbarDragging = false;
                _performanceMenuOpen = false;
                _weatherPresetMenuOpen = false;
            }

            FlatCameraInputBlocker.RegisterRect(panelRect);

            Rect headerRect = new Rect(panelRect.x, panelRect.y, panelRect.width, HeaderHeight * scale);
            bool closeClicked = DrawHeader(drawList, headerRect, theme, alpha, interactable);

            Rect bodyRect = new Rect(panelRect.x, headerRect.yMax, panelRect.width, Mathf.Max(0f, panelRect.yMax - headerRect.yMax));
            DrawBody(drawList, bodyRect, theme, alpha, interactable);

            return closeClicked;
        }

        /// <summary>
        /// Draws the header UI.
        /// </summary>
        private bool DrawHeader(FuDrawList drawList, Rect rect, TimelineWidgetTheme theme, float alpha, bool interactable)
        {
            float scale = Fugui.Scale;
            Rect titleRect = new Rect(rect.x + 22f * scale, rect.y, rect.width - 82f * scale, rect.height);
            Rect closeRect = new Rect(rect.xMax - 54f * scale, rect.y + 20f * scale, 32f * scale, 32f * scale);

            PushFont(18, true);
            DrawTextLeftCentered(drawList, titleRect, "Settings", ColorU32(theme.Text, alpha), 0f);
            PopFont(true);

            drawList.AddLine(
                new Vector2(rect.x, rect.yMax),
                new Vector2(rect.xMax, rect.yMax),
                ColorU32(theme.DockBorder, alpha * 0.60f),
                Mathf.Max(1f, scale));

            return DrawCloseButton(drawList, closeRect, theme, alpha, interactable);
        }

        /// <summary>
        /// Draws the body UI.
        /// </summary>
        private void DrawBody(FuDrawList drawList, Rect bodyRect, TimelineWidgetTheme theme, float alpha, bool interactable)
        {
            float scale = Fugui.Scale;
            float contentHeight = CalculateBodyContentHeight(scale);
            float maxScroll = Mathf.Max(0f, contentHeight - bodyRect.height);
            if (maxScroll <= 0f)
            {
                _scrollY = 0f;
                _scrollbarDragging = false;
            }
            else if (interactable && bodyRect.Contains(Fugui.GetCurrentMouse().Position))
            {
                float wheel = Fugui.GetIO().MouseWheel;
                if (Mathf.Abs(wheel) > 0.001f)
                    _scrollY = Mathf.Clamp(_scrollY - wheel * 34f * scale, 0f, maxScroll);
            }

            _scrollY = Mathf.Clamp(_scrollY, 0f, maxScroll);
            float contentWidth = maxScroll > 0f ? Mathf.Max(0f, bodyRect.width - 14f * scale) : bodyRect.width;
            float y = bodyRect.y + 8f * scale - _scrollY;
            InitializeToggleAnimationsIfNeeded();
            _performanceMenuRequest = default;
            bool rowsInteractable = interactable && !IsPerformanceMenuBlockingInput() && !IsWeatherPresetMenuBlockingInput();
            bool sharedRowsInteractable = rowsInteractable && CanEditSharedSettings();

            Fugui.PushClipRect(bodyRect.min, bodyRect.max, true);

            y = DrawSection(drawList, bodyRect, bodyRect.x, y, contentWidth, "D I S P L A Y", theme, alpha);
            y = DrawToggleRow(drawList, bodyRect, bodyRect.x, y, contentWidth, "Flight Path", "3D trajectory ribbon in the world", GetFlightPathEnabled(), ref _flightPathToggleAmount, theme, alpha, sharedRowsInteractable && Sara.FlightPath != null, SetFlightPathEnabled);
            y = DrawToggleRow(drawList, bodyRect, bodyRect.x, y, contentWidth, "Events", "Show event markers in 3D view", GetEventMarkersVisible(), ref _eventsToggleAmount, theme, alpha, sharedRowsInteractable, SetEventMarkersVisible);
            y = DrawToggleRow(drawList, bodyRect, bodyRect.x, y, contentWidth, "ILS", "Glideslope & localizer beams", SaraSharedSettingsState.LocalIlsVisible, ref _ilsToggleAmount, theme, alpha, sharedRowsInteractable, SetIlsVisible);
            y = DrawToggleRow(drawList, bodyRect, bodyRect.x, y, contentWidth, "Highlight Sticks and Gaz", "Highlight sidesticks and throttles when moving", Sara.HighlightSticksAndGaz, ref _sticksGazHighlightToggleAmount, theme, alpha, sharedRowsInteractable, SetHighlightSticksAndGaz);

            y = DrawSection(drawList, bodyRect, bodyRect.x, y, contentWidth, "E N V I R O N M E N T", theme, alpha);
            y = DrawTimeOfDayRow(drawList, bodyRect, bodyRect.x, y, contentWidth, theme, alpha, sharedRowsInteractable);
            bool metarsAvailable = WeatherController.MetarsAvailable;
            string weatherHint = metarsAvailable
                ? "Live weather recorded during the flight"
                : "METARs are not available for this flight";
            bool metarWeatherActive = metarsAvailable && SaraSharedSettingsState.LocalWeatherVisible;
            y = DrawToggleRow(drawList, bodyRect, bodyRect.x, y, contentWidth, "Weather", weatherHint, metarWeatherActive, ref _weatherToggleAmount, theme, alpha, sharedRowsInteractable && metarsAvailable, SetWeatherVisible);

            // Custom preset combobox, shown when METAR weather is off. Options come from the
            // WeatherPresetDefinition assets found in Resources/WeatherPresets. Stays editable
            // when METARs are missing: presets are then the only weather control available.
            // Bypasses the menu-blocking row gate (like the performance row) so the open
            // dropdown keeps receiving clicks; shared-settings rights still apply.
            if (ShouldDrawWeatherPresetRow())
                y = DrawWeatherPresetRow(drawList, bodyRect, bodyRect.x, y, contentWidth, theme, alpha, interactable && CanEditSharedSettings());
            y = DrawToggleRow(drawList, bodyRect, bodyRect.x, y, contentWidth, "Runway Lights", "PAPI, threshold & centerline", GetRunwayLightsVisible(), ref _runwayLightsToggleAmount, theme, alpha, sharedRowsInteractable, SetRunwayLightsVisible);

            if (!Sara.IsVR)
            {
                y = DrawSection(drawList, bodyRect, bodyRect.x, y, contentWidth, "C O N T R O L S", theme, alpha);
                y = DrawShortcutSettingsRow(drawList, bodyRect, bodyRect.x, y, contentWidth, theme, alpha, rowsInteractable);
            }

            y = DrawSection(drawList, bodyRect, bodyRect.x, y, contentWidth, "R E N D E R I N G", theme, alpha);
            y = DrawToggleRow(drawList, bodyRect, bodyRect.x, y, contentWidth, "HDR", "High dynamic range with ACES tonemapping", GetHdrEnabled(), ref _hdrToggleAmount, theme, alpha, rowsInteractable && Sara.Hdr != null, SetHdrEnabled);
            y = DrawPerformanceRow(drawList, bodyRect, bodyRect.x, y, contentWidth, theme, alpha, interactable);
            if (Sara.IsVR)
                DrawXrRenderPriorityRow(drawList, bodyRect, bodyRect.x, y, contentWidth, theme, alpha, rowsInteractable);

            DrawPerformanceMenuLayer(drawList);
            DrawWeatherPresetMenuLayer(drawList);

            Fugui.PopClipRect();
            DrawScrollbar(drawList, bodyRect, contentHeight, maxScroll, theme, alpha, interactable);
        }

        /// <summary>
        /// Initializes the toggle animations if needed state.
        /// </summary>
        private void InitializeToggleAnimationsIfNeeded()
        {
            if (_toggleAnimationsInitialized)
                return;

            _flightPathToggleAmount = GetFlightPathEnabled() ? 1f : 0f;
            _eventsToggleAmount = GetEventMarkersVisible() ? 1f : 0f;
            _ilsToggleAmount = SaraSharedSettingsState.LocalIlsVisible ? 1f : 0f;
            _sticksGazHighlightToggleAmount = Sara.HighlightSticksAndGaz ? 1f : 0f;
            _weatherToggleAmount = SaraSharedSettingsState.LocalWeatherVisible ? 1f : 0f;
            _runwayLightsToggleAmount = GetRunwayLightsVisible() ? 1f : 0f;
            _hdrToggleAmount = GetHdrEnabled() ? 1f : 0f;
            _toggleAnimationsInitialized = true;
        }

        /// <summary>
        /// Returns whether the performance menu blocking input condition is met.
        /// </summary>
        private bool IsPerformanceMenuBlockingInput()
        {
            return _performanceMenuOpen || _performanceMenuAmount > 0.001f;
        }

        /// <summary>
        /// Returns whether the weather preset menu blocking input condition is met.
        /// </summary>
        private bool IsWeatherPresetMenuBlockingInput()
        {
            return _weatherPresetMenuOpen || _weatherPresetMenuAmount > 0.001f;
        }

        /// <summary>
        /// Runs the calculate body content height logic.
        /// </summary>
        private static float CalculateBodyContentHeight(float scale)
        {
            return 8f * scale
                + SectionHeight * scale
                + RowHeight * 4f * scale
                + SectionHeight * scale
                + SliderRowHeight * scale
                + RowHeight * 2f * scale
                + (ShouldDrawWeatherPresetRow() ? RowHeight * scale : 0f)
                + (!Sara.IsVR ? (SectionHeight + RowHeight) * scale : 0f)
                + SectionHeight * scale
                + RowHeight * 2f * scale
                + (Sara.IsVR ? RowHeight * scale : 0f)
                + 24f * scale;
        }

        /// <summary>
        /// Returns whether the custom weather preset row is currently displayed.
        /// </summary>
        private static bool ShouldDrawWeatherPresetRow()
        {
            // Keep the rendered rows and the manually calculated scroll height in sync.
            return !WeatherController.MetarsAvailable || !SaraSharedSettingsState.LocalWeatherVisible;
        }

        #endregion

        #region Settings Rows
        /// <summary>
        /// Draws the section UI.
        /// </summary>
        private float DrawSection(FuDrawList drawList, Rect clipRect, float x, float y, float width, string label, TimelineWidgetTheme theme, float alpha)
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
        /// Draws the toggle row UI.
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
        /// Draws the time of day row UI.
        /// </summary>
        private float DrawTimeOfDayRow(FuDrawList drawList, Rect clipRect, float x, float y, float width, TimelineWidgetTheme theme, float alpha, bool interactable)
        {
            float scale = Fugui.Scale;
            Rect rect = new Rect(x, y, width, SliderRowHeight * scale);
            if (!IsVisible(rect, clipRect))
                return rect.yMax;

            DrawRowTopDivider(drawList, rect, theme, alpha);

            bool hasFlightTime = TryGetFlightUtcTime(out DateTime flightTime);
            DateTime currentTime = Sara.CurrentUtcTime;
            if (currentTime == DateTime.MinValue && hasFlightTime)
                currentTime = flightTime;
            if (currentTime == DateTime.MinValue)
                currentTime = DateTime.UtcNow;
            currentTime = NormalizeUtc(currentTime);

            int currentMinute = Mathf.Clamp(currentTime.Hour * 60 + currentTime.Minute, 0, MinutesInDay - 1);
            Rect headRect = new Rect(rect.x + 22f * scale, rect.y + 14f * scale, rect.width - 44f * scale, 22f * scale);

            PushFont(12, true);
            string timeText = FormatMinute(currentMinute) + " UTC";
            Vector2 timeSize = Fugui.CalcTextSize(timeText);
            PopFont(true);

            const float ResetButtonWidth = 70f;
            float gap = 8f * scale;
            Rect timeRect = new Rect(headRect.xMax - timeSize.x, headRect.y, timeSize.x, headRect.height);
            Rect resetRect = new Rect(timeRect.x - gap - ResetButtonWidth * scale, headRect.y - 2f * scale, ResetButtonWidth * scale, 26f * scale);
            Rect titleRect = new Rect(headRect.x, headRect.y, Mathf.Max(1f, resetRect.x - headRect.x - gap), headRect.height);

            PushFont(18, true);
            DrawTextLeftCentered(drawList, titleRect, ClipTextToWidth("Time of day", titleRect.width), ColorU32(theme.Text, alpha), 0f);
            PopFont(true);

            if (DrawSmallPillButton(drawList, resetRect, "Flight", theme, alpha, Sara.SyncSun, interactable && hasFlightTime && IsMouseInClip(clipRect)))
            {
                Sara.SyncSun = true;
                Sara.CurrentUtcTime = flightTime;
                currentTime = flightTime;
                currentMinute = Mathf.Clamp(currentTime.Hour * 60 + currentTime.Minute, 0, MinutesInDay - 1);
                timeText = FormatMinute(currentMinute) + " UTC";
                _timeDragging = false;
                PublishSharedSettingsIfAllowed();
            }

            PushFont(12, true);
            DrawTextLeftCentered(drawList, timeRect, timeText, ColorU32(theme.Accent, alpha), 0f);
            PopFont(true);

            Rect trackRowRect = new Rect(rect.x + 22f * scale, headRect.yMax + 10f * scale, rect.width - 44f * scale, 28f * scale);
            if (DrawTimeSlider(drawList, trackRowRect, ref currentMinute, theme, alpha, interactable && IsMouseInClip(clipRect)))
            {
                try
                {
                    Sara.SyncSun = false;
                    Sara.CurrentUtcTime = new DateTime(
                        currentTime.Year,
                        currentTime.Month,
                        currentTime.Day,
                        currentMinute / 60,
                        currentMinute % 60,
                        0,
                        DateTimeKind.Utc);
                    PublishSharedSettingsIfAllowed();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            Rect ticksRect = new Rect(trackRowRect.x, trackRowRect.yMax + 4f * scale, trackRowRect.width, 16f * scale);
            DrawSliderTicks(drawList, ticksRect, theme, alpha);

            return rect.yMax;
        }


        /// <summary>
        /// Draws the performance row UI.
        /// </summary>
        private float DrawPerformanceRow(FuDrawList drawList, Rect clipRect, float x, float y, float width, TimelineWidgetTheme theme, float alpha, bool interactable)
        {
            float scale = Fugui.Scale;
            Rect rect = new Rect(x, y, width, RowHeight * scale);
            if (!IsVisible(rect, clipRect))
                return rect.yMax;

            DrawRowTopDivider(drawList, rect, theme, alpha);
            Rect textRect = new Rect(rect.x + 22f * scale, rect.y, rect.width - 198f * scale, rect.height);
            DrawSettingText(drawList, textRect, "Performance", "Visual quality preset", theme, alpha, interactable);

            List<SaraPerformanceProfile> profiles = GetPerformanceProfiles();
            SaraPerformanceProfile currentProfile = Sara.Performance != null ? Sara.Performance.CurrentProfile : SaraPerformanceProfileApplier.CurrentProfile;
            string currentLabel = GetProfileLabel(currentProfile);
            if (currentProfile == null)
                currentLabel = profiles.Count > 0 ? GetProfileLabel(profiles[0]) : "Unavailable";

            PushFont(12, true);
            float labelWidth = Fugui.CalcTextSize(currentLabel).x;
            PopFont(true);

            float dropdownWidth = Mathf.Clamp(labelWidth + 52f * scale, 108f * scale, 172f * scale);
            Rect dropdownRect = new Rect(rect.xMax - 22f * scale - dropdownWidth, rect.y + (rect.height - 32f * scale) * 0.5f, dropdownWidth, 32f * scale);
            DrawPerformanceDropdown(drawList, dropdownRect, clipRect, profiles, currentProfile, theme, alpha, interactable && profiles.Count > 0);

            return rect.yMax;
        }

        /// <summary>
        /// Draws the shortcut settings row UI.
        /// </summary>
        private float DrawShortcutSettingsRow(FuDrawList drawList, Rect clipRect, float x, float y, float width, TimelineWidgetTheme theme, float alpha, bool interactable)
        {
            float scale = Fugui.Scale;
            Rect rect = new Rect(x, y, width, RowHeight * scale);
            if (!IsVisible(rect, clipRect))
                return rect.yMax;

            DrawRowTopDivider(drawList, rect, theme, alpha);
            Rect textRect = new Rect(rect.x + 22f * scale, rect.y, rect.width - 150f * scale, rect.height);
            DrawSettingText(drawList, textRect, "Keyboard shortcuts", "Desktop controls and timeline keys", theme, alpha, interactable);

            Rect buttonRect = new Rect(rect.xMax - 102f * scale, rect.y + (rect.height - 32f * scale) * 0.5f, 80f * scale, 32f * scale);
            if (DrawSmallPillButton(drawList, buttonRect, "Open", theme, alpha, false, interactable && IsMouseInClip(clipRect)))
                OnShortcutSettingsRequested?.Invoke();

            return rect.yMax;
        }

        /// <summary>
        /// Draws the XR render priority row UI.
        /// </summary>
        private void DrawXrRenderPriorityRow(FuDrawList drawList, Rect clipRect, float x, float y, float width, TimelineWidgetTheme theme, float alpha, bool interactable)
        {
            float scale = Fugui.Scale;
            Rect rect = new Rect(x, y, width, RowHeight * scale);
            if (!IsVisible(rect, clipRect))
                return;

            SaraUnityQualitySettings settings = GetCurrentUnityQualitySettings();
            bool enabled = interactable && settings != null && settings.ApplyXrRenderPriority;
            DrawRowTopDivider(drawList, rect, theme, alpha);

            float segmentedWidth = Mathf.Min(204f * scale, rect.width - 44f * scale);
            Rect textRect = new Rect(rect.x + 22f * scale, rect.y, Mathf.Max(1f, rect.width - segmentedWidth - 56f * scale), rect.height);
            DrawSettingText(drawList, textRect, "XR Priority", "Render focus preset", theme, alpha, enabled);

            Rect segmentedRect = new Rect(rect.xMax - 22f * scale - segmentedWidth, rect.y + (rect.height - 32f * scale) * 0.5f, segmentedWidth, 32f * scale);
            DrawXrRenderPriorityControl(drawList, segmentedRect, clipRect, settings, theme, alpha, enabled);
        }

        /// <summary>
        /// Draws the XR render priority control UI.
        /// </summary>
        private void DrawXrRenderPriorityControl(FuDrawList drawList, Rect rect, Rect clipRect, SaraUnityQualitySettings settings, TimelineWidgetTheme theme, float alpha, bool interactable)
        {
            float scale = Fugui.Scale;
            float rounding = rect.height * 0.5f;

            FlatCameraInputBlocker.RegisterRect(rect);
            drawList.AddRectFilled(rect.min, rect.max, ColorU32(theme.SettingsDropdownBackground, alpha), rounding);
            drawList.AddRect(rect.min, rect.max, ColorU32(theme.DockBorder, alpha), rounding);

            if (settings == null || !settings.ApplyXrRenderPriority)
            {
                PushFont(12, true);
                DrawTextCentered(drawList, rect, "Unavailable", ColorU32(theme.TextFaint, alpha));
                PopFont(true);
                return;
            }

            for (int i = 0; i < XrRenderPriorities.Length; i++)
            {
                SaraXrRenderPriority priority = XrRenderPriorities[i];
                float segmentWidth = rect.width / XrRenderPriorities.Length;
                Rect optionRect = new Rect(rect.x + segmentWidth * i, rect.y, segmentWidth, rect.height);
                bool selected = settings.XrRenderPriority == priority;
                bool hovered = interactable && optionRect.Contains(Fugui.GetCurrentMouse().Position) && IsMouseInClip(clipRect);
                bool active = hovered && Fugui.GetCurrentMouse().IsPressed(FuMouseButton.Left);
                Color bg = selected ? theme.PillBackgroundActive : active ? theme.PillBackgroundActive : hovered ? theme.PillBackgroundHover : Color.clear;

                if (bg.a > 0f)
                    drawList.AddRectFilled(optionRect.min + new Vector2(2f * scale, 2f * scale), optionRect.max - new Vector2(2f * scale, 2f * scale), ColorU32(bg, alpha), theme.SmallRadius * scale);

                if (i > 0)
                {
                    float separatorX = optionRect.x;
                    drawList.AddLine(
                        new Vector2(separatorX, rect.y + 7f * scale),
                        new Vector2(separatorX, rect.yMax - 7f * scale),
                        ColorU32(theme.DockBorder, alpha * 0.75f),
                        Mathf.Max(1f, scale));
                }

                PushFont(12, selected);
                DrawTextCentered(drawList, optionRect, ClipTextToWidth(GetXrRenderPriorityLabel(priority), optionRect.width - 8f * scale), ColorU32(selected ? theme.Accent : interactable ? theme.TextDim : theme.TextFaint, alpha));
                PopFont(selected);

                if (hovered)
                {
                    Fugui.SetMouseCursor(FuMouseCursor.Hand);
                    if (Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left))
                        SaraPerformanceProfileApplier.ApplyXrRenderPriority(settings, priority);
                }
            }
        }


        /// <summary>
        /// Draws the close button UI.
        /// </summary>
        private bool DrawCloseButton(FuDrawList drawList, Rect rect, TimelineWidgetTheme theme, float alpha, bool interactable)
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
        /// Draws a compact pill button.
        /// </summary>
        private bool DrawSmallPillButton(FuDrawList drawList, Rect rect, string label, TimelineWidgetTheme theme, float alpha, bool selected, bool interactable)
        {
            bool hovered = interactable && rect.Contains(Fugui.GetCurrentMouse().Position);
            bool active = hovered && Fugui.GetCurrentMouse().IsPressed(FuMouseButton.Left);
            bool clicked = hovered && Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left);
            Color bg = selected ? theme.PillBackgroundActive : active ? theme.PillBackgroundActive : hovered ? theme.PillBackgroundHover : theme.SettingsDropdownBackground;
            Color textColor = selected ? theme.Accent : interactable ? theme.TextDim : theme.TextFaint;
            float scale = Fugui.Scale;
            float rounding = rect.height * 0.5f;

            FlatCameraInputBlocker.RegisterRect(rect);
            drawList.AddRectFilled(rect.min, rect.max, ColorU32(bg, alpha), rounding);
            drawList.AddRect(rect.min, rect.max, ColorU32(theme.DockBorder, alpha), rounding);

            PushFont(12, true);
            DrawTextCentered(drawList, rect, ClipTextToWidth(label, rect.width - 12f * scale), ColorU32(textColor, alpha));
            PopFont(true);

            if (hovered)
                Fugui.SetMouseCursor(FuMouseCursor.Hand);

            return clicked;
        }

        /// <summary>
        /// Draws the toggle UI.
        /// </summary>
        private bool DrawToggle(FuDrawList drawList, Rect rect, bool value, ref float amount, TimelineWidgetTheme theme, float alpha, bool interactable)
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
        /// Draws the time slider UI.
        /// </summary>
        private bool DrawTimeSlider(FuDrawList drawList, Rect rect, ref int minute, TimelineWidgetTheme theme, float alpha, bool interactable)
        {
            float scale = Fugui.Scale;
            Rect railRect = new Rect(rect.x, rect.y + (rect.height - 4f * scale) * 0.5f, rect.width, 4f * scale);
            Rect hitRect = new Rect(rect.x, rect.y, rect.width, rect.height);
            FuMouseState mouse = Fugui.GetCurrentMouse();
            bool hovered = interactable && hitRect.Contains(Fugui.GetCurrentMouse().Position);
            bool changed = false;

            FlatCameraInputBlocker.RegisterRect(hitRect);
            if (hovered && mouse.IsDown(FuMouseButton.Left))
                _timeDragging = true;

            if (_timeDragging)
            {
                if (mouse.IsPressed(FuMouseButton.Left))
                {
                    float normalized = Mathf.Clamp01((Fugui.GetCurrentMouse().Position.x - railRect.x) / Mathf.Max(1f, railRect.width));
                    int nextMinute = Mathf.Clamp(Mathf.RoundToInt(normalized * (MinutesInDay - 1)), 0, MinutesInDay - 1);
                    if (nextMinute != minute)
                    {
                        minute = nextMinute;
                        changed = true;
                    }
                }

                if (mouse.IsUp(FuMouseButton.Left))
                    _timeDragging = false;
            }

            DrawDayCycleRail(drawList, railRect, theme, alpha);

            float t = minute / (float)(MinutesInDay - 1);
            Vector2 thumbCenter = new Vector2(railRect.x + railRect.width * t, railRect.y + railRect.height * 0.5f);
            float thumbRadius = 11f * scale * (_timeDragging ? 1.08f : 1f);
            drawList.AddCircleFilled(thumbCenter, thumbRadius, ColorU32(theme.SettingsToggleKnob, alpha), 32);
            drawList.AddCircle(thumbCenter, thumbRadius, ColorU32(theme.Accent, alpha), 32, Mathf.Max(3f * scale, 1f));

            if (hovered || _timeDragging)
                Fugui.SetMouseCursor(FuMouseCursor.ResizeEW);

            return changed;
        }

        /// <summary>
        /// Draws the day cycle rail UI.
        /// </summary>
        private void DrawDayCycleRail(FuDrawList drawList, Rect railRect, TimelineWidgetTheme theme, float alpha)
        {
            float rounding = railRect.height * 0.5f;
            drawList.AddRectFilled(railRect.min, railRect.max, ColorU32(theme.SettingsSliderNight, alpha), rounding);

            Fugui.PushClipRect(railRect.min, railRect.max, true);
            const int SegmentCount = 84;
            for (int i = 0; i < SegmentCount; i++)
            {
                float t0 = i / (float)SegmentCount;
                float t1 = (i + 1) / (float)SegmentCount;
                float mid = (t0 + t1) * 0.5f;
                if (mid <= 0.18f || mid >= 0.85f)
                    continue;

                Color color = GetDayCycleColor(theme, mid);
                Rect segmentRect = new Rect(
                    Mathf.Lerp(railRect.x, railRect.xMax, t0),
                    railRect.y,
                    Mathf.Max(1f, railRect.width / SegmentCount + 1f),
                    railRect.height);
                drawList.AddRectFilled(segmentRect.min, segmentRect.max, ColorU32(color, alpha), 0f);
            }
            Fugui.PopClipRect();
        }

        /// <summary>
        /// Returns the day cycle color value.
        /// </summary>
        private static Color GetDayCycleColor(TimelineWidgetTheme theme, float t)
        {
            if (t < 0.18f)
                return theme.SettingsSliderNight;
            if (t < 0.28f)
                return Color.Lerp(theme.SettingsSliderNight, theme.SettingsSliderDawn, Mathf.InverseLerp(0.18f, 0.28f, t));
            if (t < 0.38f)
                return Color.Lerp(theme.SettingsSliderDawn, theme.SettingsSliderDayWarm, Mathf.InverseLerp(0.28f, 0.38f, t));
            if (t < 0.50f)
                return Color.Lerp(theme.SettingsSliderDayWarm, theme.SettingsSliderDaySky, Mathf.InverseLerp(0.38f, 0.50f, t));
            if (t < 0.65f)
                return Color.Lerp(theme.SettingsSliderDaySky, theme.SettingsSliderDayWarm, Mathf.InverseLerp(0.50f, 0.65f, t));
            if (t < 0.75f)
                return Color.Lerp(theme.SettingsSliderDayWarm, theme.SettingsSliderDawn, Mathf.InverseLerp(0.65f, 0.75f, t));
            if (t < 0.85f)
                return Color.Lerp(theme.SettingsSliderDawn, theme.SettingsSliderNight, Mathf.InverseLerp(0.75f, 0.85f, t));

            return theme.SettingsSliderNight;
        }

        /// <summary>
        /// Draws the slider ticks UI.
        /// </summary>
        private void DrawSliderTicks(FuDrawList drawList, Rect rect, TimelineWidgetTheme theme, float alpha)
        {
            string[] labels = { "00", "06", "12", "18", "24" };
            PushFont(10, true);
            for (int i = 0; i < labels.Length; i++)
            {
                float t = i / (float)(labels.Length - 1);
                Vector2 textSize = Fugui.CalcTextSize(labels[i]);
                Vector2 pos = new Vector2(Mathf.Lerp(rect.x, rect.xMax, t) - textSize.x * 0.5f, rect.y + (rect.height - textSize.y) * 0.5f);
                drawList.AddText(pos, ColorU32(theme.TextFaint, alpha), labels[i]);
            }
            PopFont(true);
        }


        #endregion

        #region Performance Menu
        /// <summary>
        /// Draws the performance dropdown UI.
        /// </summary>
        private void DrawPerformanceDropdown(
            FuDrawList drawList,
            Rect triggerRect,
            Rect clipRect,
            List<SaraPerformanceProfile> profiles,
            SaraPerformanceProfile currentProfile,
            TimelineWidgetTheme theme,
            float alpha,
            bool interactable)
        {
            bool hovered = interactable && triggerRect.Contains(Fugui.GetCurrentMouse().Position) && IsMouseInClip(clipRect);
            bool active = hovered && Fugui.GetCurrentMouse().IsPressed(FuMouseButton.Left);
            bool clicked = hovered && Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left);
            string currentLabel = currentProfile != null ? GetProfileLabel(currentProfile) : profiles.Count > 0 ? GetProfileLabel(profiles[0]) : "Unavailable";
            Color bg = active || _performanceMenuOpen ? theme.PillBackgroundActive : hovered ? theme.PillBackgroundHover : theme.SettingsDropdownBackground;
            float scale = Fugui.Scale;

            FlatCameraInputBlocker.RegisterRect(triggerRect);
            drawList.AddRectFilled(triggerRect.min, triggerRect.max, ColorU32(bg, alpha), triggerRect.height * 0.5f);
            drawList.AddRect(triggerRect.min, triggerRect.max, ColorU32(theme.DockBorder, alpha), triggerRect.height * 0.5f);

            PushFont(12, true);
            Rect labelRect = new Rect(triggerRect.x + 14f * scale, triggerRect.y, triggerRect.width - 38f * scale, triggerRect.height);
            DrawTextLeftCentered(drawList, labelRect, ClipTextToWidth(currentLabel, labelRect.width), ColorU32(interactable ? theme.Text : theme.TextFaint, alpha), 0f);
            PopFont(true);

            Rect arrowRect = new Rect(triggerRect.xMax - 26f * scale, triggerRect.y, 14f * scale, triggerRect.height);
            DrawChevron(drawList, arrowRect, _performanceMenuOpen, interactable ? theme.TextDim : theme.TextFaint, alpha);

            if (hovered)
                Fugui.SetMouseCursor(FuMouseCursor.Hand);

            if (clicked)
            {
                _performanceMenuOpen = !_performanceMenuOpen;
                _performanceMenuOpenedThisFrame = _performanceMenuOpen;
            }

            _performanceMenuRequest = new PerformanceMenuRequest
            {
                HasValue = true,
                TriggerRect = triggerRect,
                ClipRect = clipRect,
                Profiles = profiles,
                CurrentProfile = currentProfile,
                Theme = theme,
                Alpha = alpha,
                Interactable = interactable
            };
        }

        /// <summary>
        /// Draws the performance menu layer UI.
        /// </summary>
        private void DrawPerformanceMenuLayer(FuDrawList drawList)
        {
            if (!_performanceMenuRequest.HasValue)
            {
                _performanceMenuOpen = false;
                _performanceMenuOpenedThisFrame = false;
                return;
            }

            PerformanceMenuRequest request = _performanceMenuRequest;
            DrawPerformanceMenu(drawList, request.TriggerRect, request.ClipRect, request.Profiles, request.CurrentProfile, request.Theme, request.Alpha, request.Interactable);
            _performanceMenuRequest = default;
        }

        /// <summary>
        /// Draws the performance menu UI.
        /// </summary>
        private void DrawPerformanceMenu(
            FuDrawList drawList,
            Rect triggerRect,
            Rect clipRect,
            List<SaraPerformanceProfile> profiles,
            SaraPerformanceProfile currentProfile,
            TimelineWidgetTheme theme,
            float alpha,
            bool interactable)
        {
            float step = Time.unscaledDeltaTime / Mathf.Max(0.001f, theme.SpeedPopupTransitionSeconds);
            _performanceMenuAmount = Mathf.MoveTowards(_performanceMenuAmount, _performanceMenuOpen ? 1f : 0f, step);

            if (!_performanceMenuOpen && _performanceMenuAmount <= 0.001f)
                return;

            float scale = Fugui.Scale;
            float t = SmoothStep01(_performanceMenuAmount);
            float menuWidth = Mathf.Max(triggerRect.width, 140f * scale);
            float menuHeight = profiles.Count * DropdownOptionHeight * scale + 10f * scale;
            float menuY = triggerRect.yMax + 6f * scale;
            if (menuY + menuHeight > clipRect.yMax - 6f * scale)
                menuY = triggerRect.y - menuHeight - 6f * scale;

            Rect targetRect = new Rect(triggerRect.xMax - menuWidth, menuY, menuWidth, menuHeight);
            Rect closedRect = new Rect(triggerRect.xMax - menuWidth * 0.80f, triggerRect.y + triggerRect.height * 0.25f, menuWidth * 0.80f, triggerRect.height * 0.55f);
            Rect menuRect = LerpRect(closedRect, targetRect, t);
            float menuAlpha = alpha * t;
            Color menuBackground = WithAlpha(theme.MenuBackground, 1f);

            FlatCameraInputBlocker.RegisterRect(menuRect);
            drawList.AddRectFilled(menuRect.min, menuRect.max, ColorU32(menuBackground, menuAlpha), theme.MediumRadius * scale);
            drawList.AddRect(menuRect.min, menuRect.max, ColorU32(theme.DockBorder, menuAlpha), theme.MediumRadius * scale);

            Fugui.PushClipRect(menuRect.min, menuRect.max, true);
            for (int i = 0; i < profiles.Count; i++)
            {
                SaraPerformanceProfile profile = profiles[i];
                Rect optionRect = new Rect(
                    menuRect.x + 5f * scale,
                    menuRect.y + 5f * scale + i * DropdownOptionHeight * scale,
                    menuRect.width - 10f * scale,
                    DropdownOptionHeight * scale);
                bool selected = IsSameProfile(profile, currentProfile);
                bool canInteract = interactable && _performanceMenuOpen && _performanceMenuAmount > 0.92f && IsMouseInClip(clipRect);
                bool hovered = canInteract && optionRect.Contains(Fugui.GetCurrentMouse().Position);
                bool active = hovered && Fugui.GetCurrentMouse().IsPressed(FuMouseButton.Left);
                Color bg = active ? theme.PillBackgroundActive : hovered ? theme.PillBackgroundHover : Color.clear;

                if (bg.a > 0f)
                    drawList.AddRectFilled(optionRect.min, optionRect.max, ColorU32(bg, menuAlpha), theme.SmallRadius * scale);

                PushFont(12, selected);
                Rect labelRect = new Rect(optionRect.x + 9f * scale, optionRect.y, optionRect.width - 34f * scale, optionRect.height);
                DrawTextLeftCentered(drawList, labelRect, ClipTextToWidth(GetProfileLabel(profile), labelRect.width), ColorU32(selected ? theme.Accent : theme.TextDim, menuAlpha), 0f);
                PopFont(selected);

                if (selected)
                    DrawCheckIcon(drawList, new Rect(optionRect.xMax - 25f * scale, optionRect.y, 14f * scale, optionRect.height), theme.Accent, menuAlpha);

                if (hovered)
                {
                    Fugui.SetMouseCursor(FuMouseCursor.Hand);
                    if (Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left))
                    {
                        if (Sara.Performance != null)
                            Sara.Performance.ApplyProfile(profile);
                        _performanceMenuOpen = false;
                    }
                }
            }
            Fugui.PopClipRect();

            if (!_performanceMenuOpenedThisFrame
                && _performanceMenuOpen
                && interactable
                && Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left)
                && !menuRect.Contains(Fugui.GetCurrentMouse().Position)
                && !triggerRect.Contains(Fugui.GetCurrentMouse().Position))
            {
                _performanceMenuOpen = false;
            }

            _performanceMenuOpenedThisFrame = false;
        }


        /// <summary>
        /// Draws the setting text UI.
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
        /// Draws the row top divider UI.
        /// </summary>
        private static void DrawRowTopDivider(FuDrawList drawList, Rect rect, TimelineWidgetTheme theme, float alpha)
        {
            drawList.AddLine(rect.min, new Vector2(rect.xMax, rect.y), ColorU32(theme.SettingsRowDivider, alpha), Mathf.Max(1f, Fugui.Scale));
        }

        /// <summary>
        /// Draws the settings body scrollbar UI.
        /// </summary>
        private void DrawScrollbar(FuDrawList drawList, Rect bodyRect, float contentHeight, float maxScroll, TimelineWidgetTheme theme, float alpha, bool interactable)
        {
            if (maxScroll <= 0f || bodyRect.height <= 0f || contentHeight <= bodyRect.height)
                return;

            float scale = Fugui.Scale;
            float trackWidth = Mathf.Max(4f * scale, 2f);
            Rect trackRect = new Rect(
                bodyRect.xMax - 8f * scale,
                bodyRect.y + 8f * scale,
                trackWidth,
                Mathf.Max(1f, bodyRect.height - 16f * scale));
            float thumbHeight = Mathf.Clamp(trackRect.height * (bodyRect.height / contentHeight), 24f * scale, trackRect.height);
            float thumbTravel = Mathf.Max(1f, trackRect.height - thumbHeight);
            float scrollT = Mathf.Clamp01(_scrollY / Mathf.Max(1f, maxScroll));
            Rect thumbRect = new Rect(trackRect.x, trackRect.y + thumbTravel * scrollT, trackRect.width, thumbHeight);
            Rect hitRect = new Rect(trackRect.x - 6f * scale, trackRect.y, trackRect.width + 12f * scale, trackRect.height);
            Vector2 mousePos = Fugui.GetCurrentMouse().Position;
            FuMouseState mouse = Fugui.GetCurrentMouse();
            bool hovered = interactable && hitRect.Contains(mousePos);
            bool active = _scrollbarDragging || hovered && mouse.IsPressed(FuMouseButton.Left);

            FlatCameraInputBlocker.RegisterRect(hitRect);

            if (hovered && mouse.IsDown(FuMouseButton.Left))
            {
                _scrollbarDragging = true;
                _scrollbarDragOffsetY = thumbRect.Contains(mousePos) ? mousePos.y - thumbRect.y : thumbHeight * 0.5f;
            }

            if (_scrollbarDragging)
            {
                if (mouse.IsPressed(FuMouseButton.Left))
                {
                    float normalized = Mathf.Clamp01((mousePos.y - _scrollbarDragOffsetY - trackRect.y) / thumbTravel);
                    _scrollY = Mathf.Clamp(maxScroll * normalized, 0f, maxScroll);
                }

                if (mouse.IsUp(FuMouseButton.Left))
                    _scrollbarDragging = false;
            }

            Color thumbColor = active ? theme.Accent : hovered ? theme.TextDim : WithAlpha(theme.TextFaint, 0.74f);
            drawList.AddRectFilled(trackRect.min, trackRect.max, ColorU32(theme.SettingsToggleOff, alpha * 0.38f), trackRect.width * 0.5f);
            drawList.AddRectFilled(thumbRect.min, thumbRect.max, ColorU32(thumbColor, alpha), thumbRect.width * 0.5f);

            if (hovered || _scrollbarDragging)
                Fugui.SetMouseCursor(FuMouseCursor.ResizeNS);
        }


        #endregion

        #region Settings Data

        /// <summary>
        /// Returns whether the independent local HDR setting is enabled.
        /// </summary>
        private static bool GetHdrEnabled()
        {
            return Sara.Hdr != null && Sara.Hdr.IsEnabled;
        }

        /// <summary>
        /// Sets the independent local HDR state.
        /// </summary>
        private static void SetHdrEnabled(bool enabled)
        {
            Sara.Hdr?.SetEnabled(enabled);
        }

        /// <summary>
        /// Returns the flight path enabled value.
        /// </summary>
        private static bool GetFlightPathEnabled()
        {
            return Sara.FlightPath != null && Sara.FlightPath.enabled;
        }

        /// <summary>
        /// Sets the flight path enabled value.
        /// </summary>
        private static void SetFlightPathEnabled(bool enabled)
        {
            if (Sara.FlightPath != null)
                Sara.FlightPath.enabled = enabled;

            PublishSharedSettingsIfAllowed();
        }

        /// <summary>
        /// Returns the event markers visible value.
        /// </summary>
        private static bool GetEventMarkersVisible()
        {
            return FlightPathPhaseMarkerManager.EventMarkersVisible;
        }

        /// <summary>
        /// Sets the event markers visible value.
        /// </summary>
        private static void SetEventMarkersVisible(bool visible)
        {
            FlightPathPhaseMarkerManager.SetEventMarkersVisible(visible);
            PublishSharedSettingsIfAllowed();
        }

        /// <summary>
        /// Sets the ILS visible value.
        /// </summary>
        private static void SetIlsVisible(bool visible)
        {
            SaraSharedSettingsState.LocalIlsVisible = visible;
            PublishSharedSettingsIfAllowed();
        }

        /// <summary>
        /// Sets the stick and throttle highlight value.
        /// </summary>
        private static void SetHighlightSticksAndGaz(bool visible)
        {
            Sara.HighlightSticksAndGaz = visible;
            PublishSharedSettingsIfAllowed();
        }

        /// <summary>
        /// Sets the weather visible value.
        /// </summary>
        private static void SetWeatherVisible(bool visible)
        {
            SaraSharedSettingsState.LocalWeatherVisible = visible;
            PublishSharedSettingsIfAllowed();
        }

        /// <summary>
        /// Sets the custom weather preset and broadcasts the shared settings.
        /// </summary>
        private static void SetWeatherPreset(int presetIndex)
        {
            SaraSharedSettingsState.LocalWeatherPreset = presetIndex;
            PublishSharedSettingsIfAllowed();
        }

        /// <summary>
        /// Draws the weather preset row UI (label + combobox trigger).
        /// </summary>
        private float DrawWeatherPresetRow(FuDrawList drawList, Rect clipRect, float x, float y, float width, TimelineWidgetTheme theme, float alpha, bool interactable)
        {
            float scale = Fugui.Scale;
            Rect rect = new Rect(x, y, width, RowHeight * scale);
            if (!IsVisible(rect, clipRect))
                return rect.yMax;

            DrawRowTopDivider(drawList, rect, theme, alpha);
            Rect textRect = new Rect(rect.x + 22f * scale, rect.y, rect.width - 198f * scale, rect.height);
            DrawSettingText(drawList, textRect, "Weather Preset", "Custom weather when METAR is off", theme, alpha, interactable);

            IReadOnlyList<WeatherPresetDefinition> presets = WeatherController.Presets;
            string currentLabel = GetWeatherPresetLabel(presets, SaraSharedSettingsState.LocalWeatherPreset);

            PushFont(12, true);
            float labelWidth = Fugui.CalcTextSize(currentLabel).x;
            PopFont(true);

            float dropdownWidth = Mathf.Clamp(labelWidth + 52f * scale, 108f * scale, 172f * scale);
            Rect dropdownRect = new Rect(rect.xMax - 22f * scale - dropdownWidth, rect.y + (rect.height - 32f * scale) * 0.5f, dropdownWidth, 32f * scale);
            DrawWeatherPresetDropdown(drawList, dropdownRect, clipRect, currentLabel, theme, alpha, interactable && presets.Count > 0);

            return rect.yMax;
        }

        /// <summary>
        /// Returns the display label of a preset index, clamped to the loaded list.
        /// </summary>
        private static string GetWeatherPresetLabel(IReadOnlyList<WeatherPresetDefinition> presets, int index)
        {
            if (presets.Count == 0)
                return "Unavailable";

            return presets[Mathf.Clamp(index, 0, presets.Count - 1)].DisplayName;
        }

        /// <summary>
        /// Draws the weather preset dropdown trigger UI.
        /// </summary>
        private void DrawWeatherPresetDropdown(
            FuDrawList drawList,
            Rect triggerRect,
            Rect clipRect,
            string currentLabel,
            TimelineWidgetTheme theme,
            float alpha,
            bool interactable)
        {
            bool hovered = interactable && triggerRect.Contains(Fugui.GetCurrentMouse().Position) && IsMouseInClip(clipRect);
            bool active = hovered && Fugui.GetCurrentMouse().IsPressed(FuMouseButton.Left);
            bool clicked = hovered && Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left);
            Color bg = active || _weatherPresetMenuOpen ? theme.PillBackgroundActive : hovered ? theme.PillBackgroundHover : theme.SettingsDropdownBackground;
            float scale = Fugui.Scale;

            FlatCameraInputBlocker.RegisterRect(triggerRect);
            drawList.AddRectFilled(triggerRect.min, triggerRect.max, ColorU32(bg, alpha), triggerRect.height * 0.5f);
            drawList.AddRect(triggerRect.min, triggerRect.max, ColorU32(theme.DockBorder, alpha), triggerRect.height * 0.5f);

            PushFont(12, true);
            Rect labelRect = new Rect(triggerRect.x + 14f * scale, triggerRect.y, triggerRect.width - 38f * scale, triggerRect.height);
            DrawTextLeftCentered(drawList, labelRect, ClipTextToWidth(currentLabel, labelRect.width), ColorU32(interactable ? theme.Text : theme.TextFaint, alpha), 0f);
            PopFont(true);

            Rect arrowRect = new Rect(triggerRect.xMax - 26f * scale, triggerRect.y, 14f * scale, triggerRect.height);
            DrawChevron(drawList, arrowRect, _weatherPresetMenuOpen, interactable ? theme.TextDim : theme.TextFaint, alpha);

            if (hovered)
                Fugui.SetMouseCursor(FuMouseCursor.Hand);

            if (clicked)
            {
                _weatherPresetMenuOpen = !_weatherPresetMenuOpen;
                _weatherPresetMenuOpenedThisFrame = _weatherPresetMenuOpen;
            }

            _weatherPresetMenuRequest = new WeatherPresetMenuRequest
            {
                HasValue = true,
                TriggerRect = triggerRect,
                ClipRect = clipRect,
                Theme = theme,
                Alpha = alpha,
                Interactable = interactable
            };
        }

        /// <summary>
        /// Draws the weather preset menu layer UI (deferred so it renders above the rows).
        /// </summary>
        private void DrawWeatherPresetMenuLayer(FuDrawList drawList)
        {
            if (!_weatherPresetMenuRequest.HasValue)
            {
                _weatherPresetMenuOpen = false;
                _weatherPresetMenuOpenedThisFrame = false;
                return;
            }

            WeatherPresetMenuRequest request = _weatherPresetMenuRequest;
            DrawWeatherPresetMenu(drawList, request.TriggerRect, request.ClipRect, request.Theme, request.Alpha, request.Interactable);
            _weatherPresetMenuRequest = default;
        }

        /// <summary>
        /// Draws the weather preset dropdown menu UI.
        /// </summary>
        private void DrawWeatherPresetMenu(
            FuDrawList drawList,
            Rect triggerRect,
            Rect clipRect,
            TimelineWidgetTheme theme,
            float alpha,
            bool interactable)
        {
            float step = Time.unscaledDeltaTime / Mathf.Max(0.001f, theme.SpeedPopupTransitionSeconds);
            _weatherPresetMenuAmount = Mathf.MoveTowards(_weatherPresetMenuAmount, _weatherPresetMenuOpen ? 1f : 0f, step);

            if (!_weatherPresetMenuOpen && _weatherPresetMenuAmount <= 0.001f)
                return;

            IReadOnlyList<WeatherPresetDefinition> presets = WeatherController.Presets;
            int selectedIndex = Mathf.Clamp(SaraSharedSettingsState.LocalWeatherPreset, 0, Mathf.Max(presets.Count - 1, 0));

            float scale = Fugui.Scale;
            float t = SmoothStep01(_weatherPresetMenuAmount);
            float menuWidth = Mathf.Max(triggerRect.width, 140f * scale);
            float menuHeight = presets.Count * DropdownOptionHeight * scale + 10f * scale;
            float menuY = triggerRect.yMax + 6f * scale;
            if (menuY + menuHeight > clipRect.yMax - 6f * scale)
                menuY = triggerRect.y - menuHeight - 6f * scale;

            Rect targetRect = new Rect(triggerRect.xMax - menuWidth, menuY, menuWidth, menuHeight);
            Rect closedRect = new Rect(triggerRect.xMax - menuWidth * 0.80f, triggerRect.y + triggerRect.height * 0.25f, menuWidth * 0.80f, triggerRect.height * 0.55f);
            Rect menuRect = LerpRect(closedRect, targetRect, t);
            float menuAlpha = alpha * t;
            Color menuBackground = WithAlpha(theme.MenuBackground, 1f);

            FlatCameraInputBlocker.RegisterRect(menuRect);
            drawList.AddRectFilled(menuRect.min, menuRect.max, ColorU32(menuBackground, menuAlpha), theme.MediumRadius * scale);
            drawList.AddRect(menuRect.min, menuRect.max, ColorU32(theme.DockBorder, menuAlpha), theme.MediumRadius * scale);

            Fugui.PushClipRect(menuRect.min, menuRect.max, true);
            for (int i = 0; i < presets.Count; i++)
            {
                Rect optionRect = new Rect(
                    menuRect.x + 5f * scale,
                    menuRect.y + 5f * scale + i * DropdownOptionHeight * scale,
                    menuRect.width - 10f * scale,
                    DropdownOptionHeight * scale);
                bool selected = i == selectedIndex;
                bool canInteract = interactable && _weatherPresetMenuOpen && _weatherPresetMenuAmount > 0.92f && IsMouseInClip(clipRect);
                bool hovered = canInteract && optionRect.Contains(Fugui.GetCurrentMouse().Position);
                bool active = hovered && Fugui.GetCurrentMouse().IsPressed(FuMouseButton.Left);
                Color bg = active ? theme.PillBackgroundActive : hovered ? theme.PillBackgroundHover : Color.clear;

                if (bg.a > 0f)
                    drawList.AddRectFilled(optionRect.min, optionRect.max, ColorU32(bg, menuAlpha), theme.SmallRadius * scale);

                PushFont(12, selected);
                Rect labelRect = new Rect(optionRect.x + 9f * scale, optionRect.y, optionRect.width - 34f * scale, optionRect.height);
                DrawTextLeftCentered(drawList, labelRect, ClipTextToWidth(presets[i].DisplayName, labelRect.width), ColorU32(selected ? theme.Accent : theme.TextDim, menuAlpha), 0f);
                PopFont(selected);

                if (selected)
                    DrawCheckIcon(drawList, new Rect(optionRect.xMax - 25f * scale, optionRect.y, 14f * scale, optionRect.height), theme.Accent, menuAlpha);

                if (hovered)
                {
                    Fugui.SetMouseCursor(FuMouseCursor.Hand);
                    if (Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left))
                    {
                        SetWeatherPreset(i);
                        _weatherPresetMenuOpen = false;
                    }
                }
            }
            Fugui.PopClipRect();

            if (!_weatherPresetMenuOpenedThisFrame
                && _weatherPresetMenuOpen
                && interactable
                && Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left)
                && !menuRect.Contains(Fugui.GetCurrentMouse().Position)
                && !triggerRect.Contains(Fugui.GetCurrentMouse().Position))
            {
                _weatherPresetMenuOpen = false;
            }

            _weatherPresetMenuOpenedThisFrame = false;
        }

        /// <summary>
        /// Returns the runway lights visible value.
        /// </summary>
        private static bool GetRunwayLightsVisible()
        {
            // Use the renderer-facing state instead of a local UI-only flag.
            return RunwayLightsVisibility.Visible;
        }

        /// <summary>
        /// Sets the runway lights visible value.
        /// </summary>
        private static void SetRunwayLightsVisible(bool visible)
        {
            // Notify the runtime renderer when the settings toggle changes.
            RunwayLightsVisibility.SetVisible(visible);
            PublishSharedSettingsIfAllowed();
        }

        /// <summary>
        /// Returns whether the local user can edit admin-controlled shared settings.
        /// </summary>
        private static bool CanEditSharedSettings()
        {
            // Outside multiplayer, the settings remain regular local controls.
            SaraSession session = Sara.CurrentSession;
            if (session == null || !session.IsMultiplayer)
                return true;

            SaraUser user = session.User;
            return user != null && user.IsAdmin;
        }

        /// <summary>
        /// Publishes the current shared settings if this client is the multiplayer admin.
        /// </summary>
        private static void PublishSharedSettingsIfAllowed()
        {
            // NetworkManager keeps the transport checks centralized.
            if (Sara.Network != null)
                Sara.Network.PublishSharedSettings();
        }
        /// <summary>
        /// Returns whether the current flight UTC time can be read.
        /// </summary>
        private static bool TryGetFlightUtcTime(out DateTime utcTime)
        {
            utcTime = DateTime.MinValue;
            if (!Sara.IsReady || Sara.Time == null || Sara.Flight == null)
                return false;

            DateTime flightTime = Sara.Time.GetFlightDateTime();
            if (flightTime == DateTime.MinValue)
                return false;

            utcTime = NormalizeUtc(flightTime);
            return true;
        }

        /// <summary>
        /// Normalizes a DateTime value as UTC without shifting recorded flight timestamps.
        /// </summary>
        private static DateTime NormalizeUtc(DateTime value)
        {
            return value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }


        /// <summary>
        /// Returns the performance profiles value.
        /// </summary>
        private static List<SaraPerformanceProfile> GetPerformanceProfiles()
        {
            if (Sara.Performance == null)
                return new List<SaraPerformanceProfile>();

            Sara.Performance.InitializeIfNeeded();
            if (Sara.Performance.Catalog == null)
                return new List<SaraPerformanceProfile>();

            return Sara.Performance.AvailableProfiles
                .Where(profile => profile != null)
                .OrderBy(profile => profile.SortOrder)
                .ToList();
        }

        /// <summary>
        /// Returns the profile label value.
        /// </summary>
        private static string GetProfileLabel(SaraPerformanceProfile profile)
        {
            if (profile == null)
                return "Missing profile";

            if (!string.IsNullOrWhiteSpace(profile.DisplayName))
                return profile.DisplayName;

            if (!string.IsNullOrWhiteSpace(profile.name))
                return profile.name;

            if (!string.IsNullOrWhiteSpace(profile.Id))
                return profile.Id;

            return "Unnamed profile";
        }

        /// <summary>
        /// Returns whether the same profile condition is met.
        /// </summary>
        private static bool IsSameProfile(SaraPerformanceProfile profile, SaraPerformanceProfile currentProfile)
        {
            if (profile == null || currentProfile == null)
                return false;

            if (!string.IsNullOrEmpty(profile.Id) && !string.IsNullOrEmpty(currentProfile.Id))
                return profile.Id == currentProfile.Id;

            return ReferenceEquals(profile, currentProfile);
        }

        /// <summary>
        /// Returns the current unity quality settings value.
        /// </summary>
        private static SaraUnityQualitySettings GetCurrentUnityQualitySettings()
        {
            SaraPerformanceProfile profile = Sara.Performance != null && Sara.Performance.CurrentProfile != null
                ? Sara.Performance.CurrentProfile
                : SaraPerformanceProfileApplier.CurrentProfile;
            return profile != null ? profile.Unity : null;
        }

        /// <summary>
        /// Returns the XR render priority label value.
        /// </summary>
        private static string GetXrRenderPriorityLabel(SaraXrRenderPriority priority)
        {
            switch (priority)
            {
                case SaraXrRenderPriority.Cockpit:
                    return "Cockpit";
                case SaraXrRenderPriority.Balanced:
                    return "Balanced";
                default:
                    return "Ground";
            }
        }

        /// <summary>
        /// Formats the minute value for display.
        /// </summary>
        private static string FormatMinute(int minute)
        {
            minute = Mathf.Clamp(minute, 0, MinutesInDay - 1);
            return (minute / 60).ToString("00") + ":" + (minute % 60).ToString("00");
        }


        #endregion

        #region Drawing Utilities
        /// <summary>
        /// Draws the chevron UI.
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
        /// Draws the check icon UI.
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
        /// Returns whether the visible condition is met.
        /// </summary>
        private static bool IsVisible(Rect rect, Rect clipRect)
        {
            return rect.yMax >= clipRect.y && rect.y <= clipRect.yMax;
        }

        /// <summary>
        /// Returns whether the mouse in clip condition is met.
        /// </summary>
        private static bool IsMouseInClip(Rect clipRect)
        {
            return clipRect.Contains(Fugui.GetCurrentMouse().Position);
        }

        /// <summary>
        /// Runs the color u 32 logic.
        /// </summary>
        private static uint ColorU32(Color color)
        {
            return Fugui.GetColorU32(new Vector4(color.r, color.g, color.b, color.a));
        }

        /// <summary>
        /// Runs the color u 32 logic.
        /// </summary>
        private static uint ColorU32(Color color, float opacity)
        {
            return Fugui.GetColorU32(new Vector4(color.r, color.g, color.b, color.a * Mathf.Clamp01(opacity)));
        }

        /// <summary>
        /// Runs the with alpha logic.
        /// </summary>
        private static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
        }

        /// <summary>
        /// Runs the smooth step 01 logic.
        /// </summary>
        private static float SmoothStep01(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * (3f - 2f * t);
        }

        /// <summary>
        /// Interpolates the rect value.
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
        /// Runs the push font logic.
        /// </summary>
        private static void PushFont(int size, bool bold)
        {
            Fugui.PushFont(size);
            if (bold)
                Fugui.PushFont(FontType.Bold);
        }

        /// <summary>
        /// Runs the pop font logic.
        /// </summary>
        private static void PopFont(bool bold)
        {
            if (bold)
                Fugui.PopFont();
            Fugui.PopFont();
        }

        /// <summary>
        /// Draws the text left centered UI.
        /// </summary>
        private static void DrawTextLeftCentered(FuDrawList drawList, Rect rect, string text, uint color, float padding)
        {
            Vector2 textSize = Fugui.CalcTextSize(text);
            Vector2 textPos = new Vector2(rect.x + padding, rect.y + (rect.height - textSize.y) * 0.5f);
            drawList.AddText(textPos, color, text);
        }

        /// <summary>
        /// Draws the text centered UI.
        /// </summary>
        private static void DrawTextCentered(FuDrawList drawList, Rect rect, string text, uint color)
        {
            Vector2 textSize = Fugui.CalcTextSize(text);
            Vector2 textPos = new Vector2(
                rect.x + (rect.width - textSize.x) * 0.5f,
                rect.y + (rect.height - textSize.y) * 0.5f);
            drawList.AddText(textPos, color, text);
        }

        /// <summary>
        /// Runs the clip text to width logic.
        /// </summary>
        private static string ClipTextToWidth(string text, float maxWidth)
        {
            if (string.IsNullOrEmpty(text) || Fugui.CalcTextSize(text).x <= maxWidth)
                return text;

            const string suffix = "...";
            float suffixWidth = Fugui.CalcTextSize(suffix).x;
            if (suffixWidth >= maxWidth)
                return suffix;

            for (int i = text.Length - 1; i > 0; i--)
            {
                string candidate = text.Substring(0, i).TrimEnd() + suffix;
                if (Fugui.CalcTextSize(candidate).x <= maxWidth)
                    return candidate;
            }

            return suffix;
        }
        #endregion
    }
}
