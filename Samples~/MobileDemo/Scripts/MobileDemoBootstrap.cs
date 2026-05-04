using Fu.Framework;
using ImGuiNET;
using System.Collections.Generic;
using UnityEngine;

namespace Fu.Samples.MobileDemo
{
    /// <summary>
    /// Fugui mobile sample window built as a touch-first smart home application.
    /// </summary>
    public class MobileDemoBootstrap : FuWindowBehaviour
    {
        #region State
        [SerializeField]
        private bool _fitWindowToScreen = true;
        [SerializeField]
        private bool _configureMobileScale = true;
        [SerializeField]
        private Vector2Int _desktopPreviewSize = new Vector2Int(430, 760);

        private readonly List<string> _footerTabs = new List<string>()
        {
            "Home",
            "Rooms",
            "Activity"
        };

        private readonly List<string> _roomTabs = new List<string>()
        {
            "All",
            "Living",
            "Kitchen",
            "Studio"
        };

        private readonly string[] _activityFeed =
        {
            "19:42 - Focus scene activated",
            "19:24 - Front door locked",
            "18:58 - Kitchen air quality improved",
            "18:31 - Studio lights set to 68%",
            "17:10 - Energy stayed below target",
            "16:45 - Notification channel tested"
        };

        private readonly List<float> _energyValues = new List<float>(36);

        private int _selectedTab;
        private int _selectedRoom;
        private bool _liveData = true;
        private bool _cloudConnected = true;
        private bool _presenceEnabled = true;
        private bool _securityArmed = true;
        private bool _livingLightsOn = true;
        private bool _kitchenLightsOn;
        private bool _studioLightsOn = true;
        private bool _mediaPlaying;
        private bool _climateEco = true;
        private bool _quietNotifications;
        private float _livingBrightness = 0.74f;
        private float _kitchenBrightness = 0.38f;
        private float _studioBrightness = 0.68f;
        private float _targetTemperature = 21.5f;
        private float _speakerVolume = 0.42f;
        private float _homeHealth = 0.91f;
        private float _energyToday = 5.8f;
        private float _airQuality = 0.83f;
        private float _syncProgress = 0.72f;
        private string _searchText = string.Empty;
        private float _phase;
        private float _lastNotificationTime = -10f;
        #endregion

        #region Methods
        /// <summary>
        /// Configures the inherited Fugui window definition before the base registration.
        /// </summary>
        public override void FuguiAwake()
        {
            _windowName = FuMobileDemoWindowNames.MobileDemo;
            _windowFlags = FuWindowFlags.NoDocking | FuWindowFlags.NoExternalization | FuWindowFlags.NoClosable;
            _size = _desktopPreviewSize;
            _position = Vector2Int.zero;
            _forceCreateAloneOnAwake = true;

            if (_configureMobileScale && Fugui.DefaultContainer != null)
            {
                Fugui.DefaultContainer.SetContainerScaleConfig(
                    FuContainerScaleConfig.Reference(
                        new Vector2Int(430, 760),
                        0.7f,
                        0.85f,
                        1.8f,
                        1f,
                        1f,
                        true,
                        true));
            }

            SeedEnergyValues();
            base.FuguiAwake();
        }

        /// <summary>
        /// Adds app-like chrome to the Fugui window definition.
        /// </summary>
        /// <param name="windowDefinition">The created Fugui window definition.</param>
        public override void OnWindowDefinitionCreated(FuWindowDefinition windowDefinition)
        {
            windowDefinition.SetHeaderUI(DrawHeader, 62f)
                .SetFooterUI(DrawFooter, FuLayout.GetTabBarHeight(FuTabsFlags.Default) / Fugui.Scale);
        }

        /// <summary>
        /// Applies phone-app window constraints when the window is created.
        /// </summary>
        /// <param name="window">The created Fugui window.</param>
        public override void OnWindowCreated(FuWindow window)
        {
            base.OnWindowCreated(window);
            window.AddWindowFlag(ImGuiWindowFlags.NoMove
                | ImGuiWindowFlags.NoResize
                | ImGuiWindowFlags.NoDecoration);

            if (_fitWindowToScreen)
            {
                window.LocalPosition = Vector2Int.zero;
                window.Size = _size;
            }
        }

        /// <summary>
        /// Draws the selected app screen inside a scrollable Fugui panel.
        /// </summary>
        /// <param name="window">The active Fugui window.</param>
        /// <param name="layout">The window layout.</param>
        public override void OnUI(FuWindow window, FuLayout layout)
        {
            UpdateLiveValues(window);

            using (new FuPanel("mobile-demo-scroll", false, height: GetBodyPanelHeight(window), flags: FuPanelFlags.Default))
            {
                switch (_selectedTab)
                {
                    case 1:
                        DrawRooms(layout);
                        break;
                    case 2:
                        DrawActivity(layout);
                        break;
                    default:
                        DrawHome(layout);
                        break;
                }

                layout.Dummy(0f, 14f);
            }
        }

        /// <summary>
        /// Gets a panel height that stays inside the Fugui body working area.
        /// </summary>
        /// <param name="window">The active Fugui window.</param>
        /// <returns>The unscaled panel height.</returns>
        private float GetBodyPanelHeight(FuWindow window)
        {
            if (window == null || window.WorkingAreaSize.y <= 0)
            {
                return 1f;
            }

            return Mathf.Max(1f, window.WorkingAreaSize.y / Mathf.Max(0.0001f, Fugui.Scale));
        }

        /// <summary>
        /// Draws the mobile app header using regular Fugui widgets.
        /// </summary>
        /// <param name="window">The active Fugui window.</param>
        /// <param name="size">Header size.</param>
        private void DrawHeader(FuWindow window, Vector2 size)
        {
            FuLayout layout = window.Layout;
            layout.Text("Fugui Home", FuTextStyle.Highlight, FuTextWrapping.Clip);
            layout.Text((_securityArmed ? "Armed" : "Disarmed") + " - Loft Studio - " + GetOnlineDeviceCount() + " online", FuTextStyle.Deactivated, FuTextWrapping.Clip);
            layout.Text((_cloudConnected ? "Cloud sync" : "Local only") + " - " + _energyToday.ToString("0.0") + " kWh today", FuTextStyle.Info, FuTextWrapping.Clip);
        }

        /// <summary>
        /// Draws bottom navigation in the mobile thumb zone.
        /// </summary>
        /// <param name="window">The active Fugui window.</param>
        /// <param name="size">Footer size.</param>
        private void DrawFooter(FuWindow window, Vector2 size)
        {
            FuLayout layout = window.Layout;
            if (layout.Tabs("mobile-demo-bottom-navigation", _footerTabs, ref _selectedTab, FuTabsFlags.Stretch | FuTabsFlags.EqualWidth))
            {
                window.ForceDraw();
            }
        }

        /// <summary>
        /// Draws the home dashboard.
        /// </summary>
        /// <param name="layout">The current Fugui layout.</param>
        private void DrawHome(FuLayout layout)
        {
            DrawScreenTitle(layout, "Tonight", "Everything important is one tap away.");
            DrawStatusOverview(layout);
            DrawScenes(layout);
            DrawFavorites(layout);
            DrawEnergy(layout);
        }

        /// <summary>
        /// Draws the room and device control screen.
        /// </summary>
        /// <param name="layout">The current Fugui layout.</param>
        private void DrawRooms(FuLayout layout)
        {
            DrawScreenTitle(layout, "Rooms", "Search, filter and control the home.");
            layout.SearchBox("home-device-search", ref _searchText, "Search devices", 0f);
            layout.Dummy(0f, 10f);
            layout.Tabs("home-room-filter", _roomTabs, ref _selectedRoom, FuTabsFlags.Stretch | FuTabsFlags.EqualWidth | FuTabsFlags.Compact);
            layout.Dummy(0f, 12f);

            DrawSectionTitle(layout, "Devices");
            DrawLightDevice(layout, "Living", "Pendant lights", ref _livingLightsOn, ref _livingBrightness);
            DrawClimateDevice(layout, "Living", "Thermostat", ref _climateEco, ref _targetTemperature);
            DrawMediaDevice(layout, "Living", "Speaker", ref _mediaPlaying, ref _speakerVolume);
            DrawLightDevice(layout, "Kitchen", "Counter lights", ref _kitchenLightsOn, ref _kitchenBrightness);
            DrawSensorDevice(layout, "Kitchen", "Air sensor", _airQuality);
            DrawLightDevice(layout, "Studio", "Desk lights", ref _studioLightsOn, ref _studioBrightness);
            DrawSecurityDevice(layout, "Studio", "Security", ref _presenceEnabled, ref _securityArmed);
        }

        /// <summary>
        /// Draws the activity and automation screen.
        /// </summary>
        /// <param name="layout">The current Fugui layout.</param>
        private void DrawActivity(FuLayout layout)
        {
            DrawScreenTitle(layout, "Activity", "Recent events and mobile Fugui flows.");

            DrawSectionTitle(layout, "Automations");
            layout.Toggle("Live updates", ref _liveData, "Paused", "Live", FuToggleFlags.AlignLeft | FuToggleFlags.MaximumTextSize);
            layout.Toggle("Quiet mode", ref _quietNotifications, "Normal", "Quiet", FuToggleFlags.AlignLeft | FuToggleFlags.MaximumTextSize);
            layout.ProgressBar("activity-sync", _syncProgress, new FuElementSize(-1f, 22f), ProgressBarTextPosition.Right, Mathf.RoundToInt(_syncProgress * 100f) + "% sync");
            layout.Dummy(0f, 12f);

            DrawSectionTitle(layout, "Actions");
            if (layout.Button("Run away mode", new FuElementSize(-1f, 46f), FuButtonStyle.Warning))
            {
                ApplyAwayScene();
            }

            if (layout.Button("Show summary", new FuElementSize(-1f, 46f), FuButtonStyle.Info))
            {
                Fugui.ShowInfo(
                    "Fugui Home",
                    "A mobile app sample using Fugui: shell, tabs, search, charts, toggles, sliders, notifications and modal flow.",
                    FuModalSize.Medium,
                    new FuModalButton("Close"));
            }

            layout.Dummy(0f, 12f);
            DrawSectionTitle(layout, "Timeline");
            for (int i = 0; i < _activityFeed.Length; i++)
            {
                DrawTimelineRow(layout, _activityFeed[i]);
            }

            DrawSectionTitle(layout, "Runtime");
            DrawInfoRow(layout, "Fugui scale", Fugui.Scale.ToString("0.00"), FuTextStyle.Info);
            DrawInfoRow(layout, "Window", _desktopPreviewSize.x + "x" + _desktopPreviewSize.y, FuTextStyle.Default);
            DrawInfoRow(layout, "Safe area", GetSafeAreaText(), FuTextStyle.Success);
        }

        /// <summary>
        /// Draws a screen heading.
        /// </summary>
        /// <param name="layout">The current Fugui layout.</param>
        /// <param name="title">The screen title.</param>
        /// <param name="subtitle">The screen subtitle.</param>
        private void DrawScreenTitle(FuLayout layout, string title, string subtitle)
        {
            layout.Text(title, FuTextStyle.Highlight, FuTextWrapping.Clip);
            layout.Text(subtitle, FuTextStyle.Deactivated, FuTextWrapping.Wrap);
            layout.Dummy(0f, 12f);
        }

        /// <summary>
        /// Draws a compact section title.
        /// </summary>
        /// <param name="layout">The current Fugui layout.</param>
        /// <param name="title">Section title.</param>
        private void DrawSectionTitle(FuLayout layout, string title)
        {
            layout.Text(title, FuTextStyle.Highlight, FuTextWrapping.Clip);
            layout.Separator();
            layout.Dummy(0f, 8f);
        }

        /// <summary>
        /// Draws the main status overview.
        /// </summary>
        /// <param name="layout">The current Fugui layout.</param>
        private void DrawStatusOverview(FuLayout layout)
        {
            DrawSectionTitle(layout, "Overview");
            DrawMetricRow(layout, "Security", _securityArmed ? "Armed" : "Disarmed", _securityArmed ? FuTextStyle.Success : FuTextStyle.Warning);
            DrawMetricRow(layout, "Climate", _targetTemperature.ToString("0.0") + " C", FuTextStyle.Info);
            DrawMetricRow(layout, "Energy", _energyToday.ToString("0.0") + " kWh", _energyToday < 7f ? FuTextStyle.Success : FuTextStyle.Warning);
            layout.ProgressBar("home-health", _homeHealth, new FuElementSize(-1f, 24f), ProgressBarTextPosition.Right, Mathf.RoundToInt(_homeHealth * 100f) + "% ready");
            layout.Dummy(0f, 12f);
        }

        /// <summary>
        /// Draws scene actions.
        /// </summary>
        /// <param name="layout">The current Fugui layout.</param>
        private void DrawScenes(FuLayout layout)
        {
            DrawSectionTitle(layout, "Scenes");
            DrawSceneButton(layout, "Morning comfort", FuButtonStyle.Highlight, ApplyMorningScene);
            DrawSceneButton(layout, "Focus studio", FuButtonStyle.Info, ApplyFocusScene);
            DrawSceneButton(layout, "Away mode", FuButtonStyle.Warning, ApplyAwayScene);
            DrawSceneButton(layout, "Night mode", FuButtonStyle.Success, ApplyNightScene);
            layout.Dummy(0f, 6f);
        }

        /// <summary>
        /// Draws one scene button.
        /// </summary>
        /// <param name="layout">The current Fugui layout.</param>
        /// <param name="label">Button label.</param>
        /// <param name="style">Button style.</param>
        /// <param name="action">Action to run.</param>
        private void DrawSceneButton(FuLayout layout, string label, FuButtonStyle style, System.Action action)
        {
            if (layout.Button(label, new FuElementSize(-1f, 46f), style))
            {
                action?.Invoke();
            }
        }

        /// <summary>
        /// Draws favorite controls on the home dashboard.
        /// </summary>
        /// <param name="layout">The current Fugui layout.</param>
        private void DrawFavorites(FuLayout layout)
        {
            DrawSectionTitle(layout, "Favorites");
            DrawCompactLightControl(layout, "Living lights", ref _livingLightsOn, ref _livingBrightness);
            DrawCompactLightControl(layout, "Studio desk", ref _studioLightsOn, ref _studioBrightness);
            DrawCompactMediaControl(layout);
            layout.Dummy(0f, 8f);
        }

        /// <summary>
        /// Draws the energy chart.
        /// </summary>
        /// <param name="layout">The current Fugui layout.</param>
        private void DrawEnergy(FuLayout layout)
        {
            DrawSectionTitle(layout, "Energy");
            layout.Chart("home-energy-chart", _energyValues, FuChartSeriesType.Area);
            layout.ProgressBar("home-energy-target", Mathf.Clamp01(1f - _energyToday / 12f), new FuElementSize(-1f, 22f), ProgressBarTextPosition.Right, "target");
        }

        /// <summary>
        /// Draws a compact metric row.
        /// </summary>
        /// <param name="layout">The current Fugui layout.</param>
        /// <param name="label">Metric label.</param>
        /// <param name="value">Metric value.</param>
        /// <param name="style">Value style.</param>
        private void DrawMetricRow(FuLayout layout, string label, string value, FuTextStyle style)
        {
            layout.FramedText(label + " - " + value, new FuElementSize(-1f, 38f), FuFrameStyle.Default, 0f, FuTextWrapping.Clip, 12f, 12f);
            layout.Text(value, style, FuTextWrapping.Clip);
            layout.Dummy(0f, 6f);
        }

        /// <summary>
        /// Draws a compact light control.
        /// </summary>
        /// <param name="layout">The current Fugui layout.</param>
        /// <param name="label">Control label.</param>
        /// <param name="enabled">Power state.</param>
        /// <param name="level">Brightness level.</param>
        private void DrawCompactLightControl(FuLayout layout, string label, ref bool enabled, ref float level)
        {
            layout.FramedText(label + " - " + (enabled ? Mathf.RoundToInt(level * 100f) + "%" : "Off"), new FuElementSize(-1f, 38f), FuFrameStyle.Default, 0f, FuTextWrapping.Clip, 12f, 12f);
            layout.Toggle(label + " power", ref enabled, "Off", "On", FuToggleFlags.AlignLeft | FuToggleFlags.MaximumTextSize);
            layout.Slider(label + " level", ref level, 0f, 1f, new FuElementSize(-1f, 10f), 0.01f, FuSliderFlags.NoDrag | FuSliderFlags.UpdateOnBarClick, "%.0f");
            layout.Dummy(0f, 10f);
        }

        /// <summary>
        /// Draws the compact media control.
        /// </summary>
        /// <param name="layout">The current Fugui layout.</param>
        private void DrawCompactMediaControl(FuLayout layout)
        {
            layout.FramedText("Speaker - " + (_mediaPlaying ? "Playing" : "Paused"), new FuElementSize(-1f, 38f), FuFrameStyle.Default, 0f, FuTextWrapping.Clip, 12f, 12f);
            layout.Toggle("Speaker playback", ref _mediaPlaying, "Paused", "Playing", FuToggleFlags.AlignLeft | FuToggleFlags.MaximumTextSize);
            layout.Slider("Speaker volume", ref _speakerVolume, 0f, 1f, new FuElementSize(-1f, 10f), 0.01f, FuSliderFlags.NoDrag | FuSliderFlags.UpdateOnBarClick, "%.0f");
            layout.Dummy(0f, 10f);
        }

        /// <summary>
        /// Draws one light device card.
        /// </summary>
        /// <param name="layout">The current Fugui layout.</param>
        /// <param name="room">Room name.</param>
        /// <param name="name">Device name.</param>
        /// <param name="enabled">Whether the device is on.</param>
        /// <param name="level">Brightness level.</param>
        private void DrawLightDevice(FuLayout layout, string room, string name, ref bool enabled, ref float level)
        {
            if (!ShouldDrawDevice(room, name))
            {
                return;
            }

            DrawDeviceHeader(layout, room, name, enabled ? Mathf.RoundToInt(level * 100f) + "%" : "Off");
            layout.Toggle(name + " power", ref enabled, "Off", "On", FuToggleFlags.AlignLeft | FuToggleFlags.MaximumTextSize);
            layout.Slider(name + " brightness", ref level, 0f, 1f, new FuElementSize(-1f, 10f), 0.01f, FuSliderFlags.NoDrag | FuSliderFlags.UpdateOnBarClick, "%.0f");
            layout.Dummy(0f, 12f);
        }

        /// <summary>
        /// Draws the climate device card.
        /// </summary>
        /// <param name="layout">The current Fugui layout.</param>
        /// <param name="room">Room name.</param>
        /// <param name="name">Device name.</param>
        /// <param name="eco">Eco mode.</param>
        /// <param name="target">Target temperature.</param>
        private void DrawClimateDevice(FuLayout layout, string room, string name, ref bool eco, ref float target)
        {
            if (!ShouldDrawDevice(room, name))
            {
                return;
            }

            DrawDeviceHeader(layout, room, name, target.ToString("0.0") + " C");
            layout.Toggle("Eco mode", ref eco, "Comfort", "Eco", FuToggleFlags.AlignLeft | FuToggleFlags.MaximumTextSize);
            layout.Slider("Target temperature", ref target, 17f, 25f, new FuElementSize(-1f, 10f), 0.1f, FuSliderFlags.NoDrag | FuSliderFlags.UpdateOnBarClick, "%.1f C");
            layout.Dummy(0f, 12f);
        }

        /// <summary>
        /// Draws the media device card.
        /// </summary>
        /// <param name="layout">The current Fugui layout.</param>
        /// <param name="room">Room name.</param>
        /// <param name="name">Device name.</param>
        /// <param name="playing">Whether media is playing.</param>
        /// <param name="volume">Speaker volume.</param>
        private void DrawMediaDevice(FuLayout layout, string room, string name, ref bool playing, ref float volume)
        {
            if (!ShouldDrawDevice(room, name))
            {
                return;
            }

            DrawDeviceHeader(layout, room, name, playing ? "Playing" : "Paused");
            layout.Toggle("Playback", ref playing, "Paused", "Playing", FuToggleFlags.AlignLeft | FuToggleFlags.MaximumTextSize);
            layout.Slider("Volume", ref volume, 0f, 1f, new FuElementSize(-1f, 10f), 0.01f, FuSliderFlags.NoDrag | FuSliderFlags.UpdateOnBarClick, "%.0f");
            layout.Dummy(0f, 12f);
        }

        /// <summary>
        /// Draws the air sensor card.
        /// </summary>
        /// <param name="layout">The current Fugui layout.</param>
        /// <param name="room">Room name.</param>
        /// <param name="name">Device name.</param>
        /// <param name="quality">Air quality value.</param>
        private void DrawSensorDevice(FuLayout layout, string room, string name, float quality)
        {
            if (!ShouldDrawDevice(room, name))
            {
                return;
            }

            DrawDeviceHeader(layout, room, name, Mathf.RoundToInt(quality * 100f) + "%");
            layout.ProgressBar(name + " air quality", quality, new FuElementSize(-1f, 22f), ProgressBarTextPosition.Right, Mathf.RoundToInt(quality * 100f) + "% air");
            layout.Dummy(0f, 12f);
        }

        /// <summary>
        /// Draws the security card.
        /// </summary>
        /// <param name="layout">The current Fugui layout.</param>
        /// <param name="room">Room name.</param>
        /// <param name="name">Device name.</param>
        /// <param name="presence">Presence detection state.</param>
        /// <param name="armed">Security state.</param>
        private void DrawSecurityDevice(FuLayout layout, string room, string name, ref bool presence, ref bool armed)
        {
            if (!ShouldDrawDevice(room, name))
            {
                return;
            }

            DrawDeviceHeader(layout, room, name, armed ? "Armed" : "Disarmed");
            layout.Toggle("Presence", ref presence, "Off", "On", FuToggleFlags.AlignLeft | FuToggleFlags.MaximumTextSize);
            layout.Toggle("Security", ref armed, "Disarmed", "Armed", FuToggleFlags.AlignLeft | FuToggleFlags.MaximumTextSize);
            if (layout.Button("Test notification", new FuElementSize(-1f, 44f), FuButtonStyle.Info))
            {
                Notify("Security check", armed ? "Sensors are armed and reporting normally." : "Security is disarmed.", armed ? StateType.Success : StateType.Warning);
            }
            layout.Dummy(0f, 12f);
        }

        /// <summary>
        /// Draws a repeated device card header.
        /// </summary>
        /// <param name="layout">The current Fugui layout.</param>
        /// <param name="room">Room name.</param>
        /// <param name="name">Device name.</param>
        /// <param name="state">Device state.</param>
        private void DrawDeviceHeader(FuLayout layout, string room, string name, string state)
        {
            layout.FramedText(room + " - " + name + " - " + state, new FuElementSize(-1f, 38f), FuFrameStyle.Default, 0f, FuTextWrapping.Clip, 12f, 12f);
            layout.Dummy(0f, 6f);
        }

        /// <summary>
        /// Draws a key/value row using Fugui text.
        /// </summary>
        /// <param name="layout">The current Fugui layout.</param>
        /// <param name="label">Row label.</param>
        /// <param name="value">Row value.</param>
        /// <param name="style">Value style.</param>
        private void DrawInfoRow(FuLayout layout, string label, string value, FuTextStyle style)
        {
            layout.Text(label, FuTextStyle.Deactivated, FuTextWrapping.Clip);
            layout.Text(value, style, FuTextWrapping.Clip);
            layout.Separator();
            layout.Dummy(0f, 6f);
        }

        /// <summary>
        /// Draws an event row with a Fugui frame.
        /// </summary>
        /// <param name="layout">The current Fugui layout.</param>
        /// <param name="text">Event text.</param>
        private void DrawTimelineRow(FuLayout layout, string text)
        {
            layout.FramedText(text, new FuElementSize(-1f, 38f), FuFrameStyle.Default, 0f, FuTextWrapping.Clip, 12f, 12f);
            layout.Dummy(0f, 6f);
        }

        /// <summary>
        /// Checks whether a room/device should be visible for the current filter.
        /// </summary>
        /// <param name="room">Room name.</param>
        /// <param name="name">Device name.</param>
        /// <returns>True if visible.</returns>
        private bool ShouldDrawDevice(string room, string name)
        {
            if (_selectedRoom > 0 && _roomTabs[_selectedRoom] != room)
            {
                return false;
            }

            string filter = _searchText == null ? string.Empty : _searchText.Trim().ToLowerInvariant();
            if (filter.Length == 0)
            {
                return true;
            }

            return room.ToLowerInvariant().Contains(filter) || name.ToLowerInvariant().Contains(filter);
        }

        /// <summary>
        /// Applies the morning scene.
        /// </summary>
        private void ApplyMorningScene()
        {
            _livingLightsOn = true;
            _kitchenLightsOn = true;
            _studioLightsOn = false;
            _livingBrightness = 0.82f;
            _kitchenBrightness = 0.62f;
            _targetTemperature = 21.5f;
            _quietNotifications = false;
            Notify("Morning scene", "Lights and climate are ready.", StateType.Success);
        }

        /// <summary>
        /// Applies the focus scene.
        /// </summary>
        private void ApplyFocusScene()
        {
            _livingLightsOn = false;
            _studioLightsOn = true;
            _studioBrightness = 0.86f;
            _mediaPlaying = true;
            _speakerVolume = 0.28f;
            _quietNotifications = true;
            Notify("Focus scene", "Studio is bright and notifications are quiet.", StateType.Info);
        }

        /// <summary>
        /// Applies the away scene.
        /// </summary>
        private void ApplyAwayScene()
        {
            _livingLightsOn = false;
            _kitchenLightsOn = false;
            _studioLightsOn = false;
            _mediaPlaying = false;
            _presenceEnabled = true;
            _securityArmed = true;
            _quietNotifications = false;
            Notify("Away mode", "Home is locked, dimmed and armed.", StateType.Warning);
        }

        /// <summary>
        /// Applies the night scene.
        /// </summary>
        private void ApplyNightScene()
        {
            _livingLightsOn = true;
            _kitchenLightsOn = false;
            _studioLightsOn = false;
            _livingBrightness = 0.18f;
            _targetTemperature = 19.5f;
            _quietNotifications = true;
            Notify("Night scene", "Soft lighting and quiet mode are active.", StateType.Success);
        }

        /// <summary>
        /// Seeds chart values before the first frame.
        /// </summary>
        private void SeedEnergyValues()
        {
            _energyValues.Clear();
            for (int i = 0; i < 36; i++)
            {
                float t = i / 35f;
                _energyValues.Add(4.8f + Mathf.Sin(t * Mathf.PI * 3f) * 1.2f + Mathf.Sin(t * Mathf.PI * 9f) * 0.35f);
            }
        }

        /// <summary>
        /// Updates simulated live values.
        /// </summary>
        /// <param name="window">The Fugui window.</param>
        private void UpdateLiveValues(FuWindow window)
        {
            if (!_liveData)
            {
                return;
            }

            _phase += Time.unscaledDeltaTime;
            _syncProgress = Mathf.Repeat(_syncProgress + Time.unscaledDeltaTime * 0.035f, 1f);
            _energyToday = Mathf.Lerp(4.6f, 8.4f, (Mathf.Sin(_phase * 0.18f) + 1f) * 0.5f);
            _airQuality = Mathf.Lerp(0.64f, 0.96f, (Mathf.Sin(_phase * 0.42f) + 1f) * 0.5f);
            _homeHealth = Mathf.Lerp(0.82f, 0.98f, (Mathf.Sin(_phase * 0.31f) + 1f) * 0.5f);
            UpdateEnergyValues();
            window?.ForceDraw(2);
        }

        /// <summary>
        /// Updates the chart data while the live stream runs.
        /// </summary>
        private void UpdateEnergyValues()
        {
            if (_energyValues.Count == 0)
            {
                SeedEnergyValues();
            }

            float energy = Mathf.Lerp(3.8f, 8.9f, (Mathf.Sin(_phase * 0.85f) + 1f) * 0.5f);
            _energyValues.RemoveAt(0);
            _energyValues.Add(energy);
        }

        /// <summary>
        /// Sends a throttled Fugui notification.
        /// </summary>
        /// <param name="title">Notification title.</param>
        /// <param name="message">Notification body.</param>
        /// <param name="type">Notification type.</param>
        private void Notify(string title, string message, StateType type)
        {
            if (Time.unscaledTime - _lastNotificationTime < 0.25f)
            {
                return;
            }

            _lastNotificationTime = Time.unscaledTime;
            Fugui.Notify(title, message, type);
        }

        /// <summary>
        /// Gets the number of online devices.
        /// </summary>
        /// <returns>The online device count.</returns>
        private int GetOnlineDeviceCount()
        {
            int count = 2;
            count += _livingLightsOn ? 1 : 0;
            count += _kitchenLightsOn ? 1 : 0;
            count += _studioLightsOn ? 1 : 0;
            count += _mediaPlaying ? 1 : 0;
            count += _presenceEnabled ? 1 : 0;
            count += _securityArmed ? 1 : 0;
            return count;
        }

        /// <summary>
        /// Returns a compact safe area text.
        /// </summary>
        /// <returns>The safe area text.</returns>
        private string GetSafeAreaText()
        {
            Rect safeArea = Screen.safeArea;
            return Mathf.RoundToInt(safeArea.width) + "x" + Mathf.RoundToInt(safeArea.height);
        }
        #endregion
    }
}
