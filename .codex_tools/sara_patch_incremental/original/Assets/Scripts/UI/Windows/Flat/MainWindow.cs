using Assets.Scripts.UI.Windows.Common;
using Assets.Scripts.UI.Shortcuts;
using Fu;
using Fu.Framework;

using Saravr.Engine.Visuals;
using Saravr.Interaction;
using Saravr.Network.Common;
using UnityEngine;

/// <summary>
/// Coordinates main window behavior in the Unity scene.
/// </summary>
public class MainWindow : FuWindowBehaviour
{
    /// <summary>
    /// Lists the supported glass icon kind values.
    /// </summary>
    private enum GlassIconKind
    {
        Font,
        Timeline,
        SeatPosition,
        NetworkVoice,
        AdminSeats,
        CaretUp
    }

    [SerializeField]
    private TimelineWidgetTheme timelineTheme;

    private readonly TimelineWidget _timelineWidget = new TimelineWidget();
    private readonly SettingsWidget _settingsWidget = new SettingsWidget();
    private readonly NetworkSettingsWidget _networkSettingsWidget = new NetworkSettingsWidget();
    private readonly AdminPanelWidget _adminPanelWidget = new AdminPanelWidget();
    private readonly ShortcutSettingsWidget _shortcutSettingsWidget = new ShortcutSettingsWidget();
    private static readonly FuLayout ShortcutTooltipLayout = new FuLayout();
    private bool _timelineRetracted;
    private float _timelineOpenAmount = 1f;
    private bool _settingsOpen;
    private bool _settingsOpenedThisFrame;
    private float _settingsOpenAmount;
    private bool _networkSettingsOpen;
    private bool _networkSettingsOpenedThisFrame;
    private float _networkSettingsOpenAmount;
    private bool _adminPanelOpen;
    private bool _adminPanelOpenedThisFrame;
    private float _adminPanelOpenAmount;
    private bool _seatMenuOpen;
    private bool _seatMenuOpenedThisFrame;
    private object _timelineTestEventsSource;
    private bool _windowDefinitionRegistered;

    // Fullscreen HUD window ignored by non-UI pointer arbitration.
    public static FuWindow HudWindow { get; private set; }

    #region Lifecycle
    /// <summary>
    /// Subscribes to runtime events when the component is enabled.
    /// </summary>
    private void OnEnable()
    {
        Sara.Loader.OnLoadingComplete += HandleLoadingComplete;
    }

    /// <summary>
    /// Unsubscribes from runtime events when the component is disabled.
    /// </summary>
    private void OnDisable()
    {
        FlatRaycaster.Current?.EndPointing();
        Sara.Loader.OnLoadingComplete -= HandleLoadingComplete;

        if (ReferenceEquals(HudWindow, _fuWindow))
            HudWindow = null;

        _fuWindow?.Close();
    }

    /// <summary>
    /// Registers and creates the fullscreen Fugui HUD after loading completes.
    /// </summary>
    private void HandleLoadingComplete()
    {
        if (!_windowDefinitionRegistered)
        {
            _windowName = FuWindowsNames.Main;
            _windowLayer = FuLayer.Hud;
            _windowFlags = FuWindowFlags.NoExternalization
                | FuWindowFlags.NoDocking
                | FuWindowFlags.NoClosable
                | FuWindowFlags.NoMouseInputFocus
                | FuWindowFlags.NoKeyboardInputFocus;
            _windowStyleFlags = FuWindowStyleFlags.NoDecoration
                | FuWindowStyleFlags.NoMove
                | FuWindowStyleFlags.NoResize
                | FuWindowStyleFlags.NoScrollbar
                | FuWindowStyleFlags.NoScrollWithMouse
                | FuWindowStyleFlags.NoBackground
                | FuWindowStyleFlags.NoSavedSettings
                | FuWindowStyleFlags.NoBringToFrontOnFocus
                | FuWindowStyleFlags.NoFocusOnAppearing;
            _position = Vector2Int.zero;
            _size = Fugui.DefaultContainer.Size;

            base.FuguiAwake();
            _windowDefinitionRegistered = true;
        }

        if (_fuWindow == null || !_fuWindow.IsOpened)
            Fugui.CreateWindowAsync(_windowName, null);
    }

    /// <summary>
    /// Keeps the animated HUD rendering and input state current every frame.
    /// </summary>
    private void Update()
    {
        if (_fuWindow == null)
            return;

        _fuWindow.IsInterractable = !Sara.IsVR && Sara.IsReady;
        _fuWindow.ForceDraw();
    }

    #endregion

    #region Window Rendering
    /// <summary>
    /// Configures the created fullscreen HUD window.
    /// </summary>
    public override void OnWindowCreated(FuWindow window)
    {
        HudWindow = window;
        window.IsInterractable = !Sara.IsVR && Sara.IsReady;
        ResizeFullscreen(window);
        window.ForceDraw();
    }

    /// <summary>
    /// Draws the fullscreen HUD through the Fugui window lifecycle.
    /// </summary>
    public override void OnUI(FuWindow window, FuLayout layout)
    {
        if (Sara.IsVR || !Sara.IsReady)
        {
            FlatRaycaster.Current?.EndPointing();
            return;
        }

        ResizeFullscreen(window);
        TimelineWidgetTheme theme = ResolveTimelineTheme();
        _timelineWidget.SetTheme(theme);
        _timelineWidget.ShortcutTooltipProvider = SaraShortcutSettings.GetTooltip;
        _settingsWidget.SetTheme(theme);
        _settingsWidget.OnShortcutSettingsRequested = OpenShortcutSettingsPopup;
        _networkSettingsWidget.SetTheme(theme);
        _adminPanelWidget.SetTheme(theme);
        AddTimelineTestEvents();
        bool modalInputBlocked = IsFlatModalInputBlocked();
        HandleFlatShortcuts(modalInputBlocked);

        Vector2 size = window.Size;

        if (modalInputBlocked)
            BlockFlatModalInput();

        DrawTopButtons(size, theme, modalInputBlocked);

        modalInputBlocked = IsFlatModalInputBlocked();
        _timelineWidget.InputBlocked = modalInputBlocked;
        if (modalInputBlocked)
            BlockFlatModalInput();

        DrawTimelinePanel(size, theme, modalInputBlocked);
        DrawSettingsPanel(size, theme);
        DrawNetworkSettingsPanel(size, theme);
        DrawAdminPanel(size, theme);
        DrawPointingReticle(size, theme);
        _timelineWidget.DrawScreenSeekGestures(size, !modalInputBlocked);
        _shortcutSettingsWidget.Draw(size, theme);

    }

    /// <summary>
    /// Keeps the HUD aligned with the current main container.
    /// </summary>
    private static void ResizeFullscreen(FuWindow window)
    {
        if (window == null || window.Container == null)
            return;

        if (window.Size != window.Container.Size)
            window.Size = window.Container.Size;

        if (window.LocalPosition != Vector2Int.zero)
            window.LocalPosition = Vector2Int.zero;
    }

    #endregion

    #region Flat Input
    /// <summary>
    /// Handles the flat shortcuts flow.
    /// </summary>
    private void HandleFlatShortcuts(bool inputBlocked)
    {
        if (inputBlocked || !IsDesktopShortcutRuntime() || Fugui.GetWantCaptureInputs(true))
        {
            FlatRaycaster.Current?.EndPointing();
            return;
        }

        if (SaraShortcutSettings.WasPressedThisFrame(SaraShortcutAction.OpenSettings))
            OpenSettingsPanel();
        else if (SaraShortcutSettings.WasPressedThisFrame(SaraShortcutAction.OpenNetworkSettings))
            OpenNetworkSettingsPanel();
        else if (SaraShortcutSettings.WasPressedThisFrame(SaraShortcutAction.OpenAdminPanel))
            OpenAdminPanel();
        else if (SaraShortcutSettings.WasPressedThisFrame(SaraShortcutAction.ToggleSeatMenu))
            ToggleSeatMenu();
        else if (SaraShortcutSettings.WasPressedThisFrame(SaraShortcutAction.TogglePointing))
            TogglePointing();
        else if (SaraShortcutSettings.WasPressedThisFrame(SaraShortcutAction.ToggleTimelineDock))
            _timelineRetracted = !_timelineRetracted;
        else if (SaraShortcutSettings.WasPressedThisFrame(SaraShortcutAction.TimelinePlayPause))
            _timelineWidget.TryTogglePlayPause();
        else if (SaraShortcutSettings.WasPressedThisFrame(SaraShortcutAction.TimelineBack10))
            _timelineWidget.TrySeekSeconds(-10f);
        else if (SaraShortcutSettings.WasPressedThisFrame(SaraShortcutAction.TimelineForward10))
            _timelineWidget.TrySeekSeconds(10f);
    }

    /// <summary>
    /// Returns whether a flat modal panel should capture input.
    /// </summary>
    private bool IsFlatModalInputBlocked()
    {
        return _settingsOpen
            || _networkSettingsOpen
            || _adminPanelOpen
            || _shortcutSettingsWidget.IsOpen
            || _settingsOpenAmount > 0.001f
            || _networkSettingsOpenAmount > 0.001f
            || _adminPanelOpenAmount > 0.001f;
    }

    /// <summary>
    /// Captures flat input for the current frame.
    /// </summary>
    private static void BlockFlatModalInput()
    {
        FlatRaycaster.Current?.EndPointing();
        FlatCameraInputBlocker.BlockAllForFrame();
    }

    /// <summary>
    /// Returns whether desktop shortcuts should be active for the current platform.
    /// </summary>
    private static bool IsDesktopShortcutRuntime()
    {
        return Application.platform == RuntimePlatform.WindowsPlayer
            || Application.platform == RuntimePlatform.OSXPlayer
            || Application.platform == RuntimePlatform.WindowsEditor
            || Application.platform == RuntimePlatform.OSXEditor;
    }

    /// <summary>
    /// Resolves the timeline theme reference or value.
    /// </summary>
    private TimelineWidgetTheme ResolveTimelineTheme()
    {
        if (timelineTheme == null)
            timelineTheme = TimelineWidgetTheme.LoadDefault();

        return timelineTheme;
    }

    /// <summary>
    /// Runs the add timeline test events logic.
    /// </summary>
    private void AddTimelineTestEvents()
    {
        object source = Sara.Flight != null ? Sara.Flight.Container : null;
        if (source == null || Sara.Time == null || ReferenceEquals(_timelineTestEventsSource, source))
            return;

        //_timelineWidget.SetEvents(new[]
        //{
        //    CreateTimelineTestEvent(30f, "Top of climb", EventSeverity.Info),
        //    CreateTimelineTestEvent(42f, "Step climb", EventSeverity.Info),
        //    CreateTimelineTestEvent(67f, "High descent rate", EventSeverity.Medium),
        //    CreateTimelineTestEvent(67.8f, "Flaps extended", EventSeverity.Info),
        //    CreateTimelineTestEvent(68.4f, "Speed deviation", EventSeverity.Medium),
        //    CreateTimelineTestEvent(69f, "Gear down", EventSeverity.Info),
        //    CreateTimelineTestEvent(81f, "GPWS alert", EventSeverity.High),
        //    CreateTimelineTestEvent(82f, "Bank angle", EventSeverity.High),
        //    CreateTimelineTestEvent(92f, "Touchdown", EventSeverity.Info),
        //});

        _timelineTestEventsSource = source;
    }

    /// <summary>
    /// Creates the timeline test event instance or data.
    /// </summary>
    private static TimelineEvent CreateTimelineTestEvent(float percent, string label, EventSeverity severity)
    {
        double normalized = Mathf.Clamp01(percent / 100f);
        double deltaTid = (Sara.Time.LastTid - Sara.Time.FirstTid) * normalized;
        return new TimelineEvent(Sara.Time.FirstTid + deltaTid, label, severity, (int)severity >= (int)EventSeverity.Medium);
    }


    #endregion

    #region Timeline And Top Bar
    /// <summary>
    /// Draws the timeline panel UI.
    /// </summary>
    private void DrawTimelinePanel(Vector2 containerSize, TimelineWidgetTheme theme, bool inputBlocked)
    {
        float scale = Fugui.Scale;
        float target = _timelineRetracted ? 0f : 1f;
        float step = Time.unscaledDeltaTime / Mathf.Max(0.001f, theme.TimelineTransitionSeconds);
        _timelineOpenAmount = Mathf.MoveTowards(_timelineOpenAmount, target, step);
        float t = SmoothStep01(_timelineOpenAmount);

        float expandedY = containerSize.y - theme.DockBottomMargin * scale - theme.DockHeight * scale;
        float collapsedY = containerSize.y + 20f * scale;
        Rect dockRect = new Rect(
            theme.DockMarginX * scale,
            Mathf.Lerp(collapsedY, expandedY, t),
            Mathf.Max(1f, containerSize.x - theme.DockMarginX * 2f * scale),
            theme.DockHeight * scale);

        if (_timelineOpenAmount > 0.001f)
            _timelineWidget.DrawDock(dockRect);

        if (_timelineOpenAmount < 0.999f)
            DrawTimelineRetracted(containerSize, theme, 1f - t, inputBlocked);
    }

    /// <summary>
    /// Draws the timeline retracted UI.
    /// </summary>
    private void DrawTimelineRetracted(Vector2 containerSize, TimelineWidgetTheme theme, float visibility, bool inputBlocked)
    {
        visibility = Mathf.Clamp01(visibility);
        if (visibility <= 0.001f)
            return;

        float scale = Fugui.Scale;
        FuDrawList drawList = Fugui.GetCurrentWindowDrawList();
        string timeText = _timelineWidget.ClockText;
        string modeText = _timelineWidget.ClockModeLabel;
        float buttonSize = theme.TopButtonSize * scale;
        float gap = theme.TopButtonGap * scale;

        Fugui.PushFont(18);
        float timeWidth = Fugui.CalcTextSize(timeText).x;
        Fugui.PopFont();

        float chipWidth = timeWidth + 58f * scale;
        float clusterWidth = buttonSize + gap + chipWidth;
        float y = containerSize.y - 20f * scale - buttonSize + (1f - visibility) * 18f * scale;
        float x = (containerSize.x - clusterWidth) * 0.5f;
        Rect showButtonRect = new Rect(x, y, buttonSize, buttonSize);
        Rect timeChipRect = new Rect(showButtonRect.xMax + gap, y, chipWidth, buttonSize);

        if (DrawGlassIconButton(drawList, showButtonRect, null, "timelineShowDock", theme, false, GlassIconKind.CaretUp, GetCurrentSeat(), visibility, SaraShortcutSettings.GetTooltip(SaraShortcutAction.ToggleTimelineDock), !inputBlocked))
            _timelineRetracted = false;

        FlatCameraInputBlocker.RegisterRect(timeChipRect);
        DrawGlassShell(drawList, timeChipRect, theme, theme.MediumRadius * scale, false, visibility);

        Fugui.PushFont(18);
        Vector2 textSize = Fugui.CalcTextSize(timeText);
        Vector2 textPos = new Vector2(timeChipRect.x + 14f * scale, timeChipRect.y + (timeChipRect.height - textSize.y) * 0.5f);
        drawList.AddText(textPos, ColorU32(theme.Text, visibility), timeText);
        Fugui.PopFont();

        float dividerX = textPos.x + textSize.x + 12f * scale;
        drawList.AddLine(
            new Vector2(dividerX, timeChipRect.y + 13f * scale),
            new Vector2(dividerX, timeChipRect.yMax - 13f * scale),
            ColorU32(theme.DockBorder, visibility),
            Mathf.Max(1f, scale));

        PushCompactFont(10);
        Rect modeRect = new Rect(dividerX + 8f * scale, timeChipRect.y, timeChipRect.xMax - dividerX - 8f * scale, timeChipRect.height);
        DrawTextCentered(drawList, modeRect, modeText, ColorU32(theme.TextFaint, visibility));
        PopCompactFont();

        if (!inputBlocked && !_timelineWidget.IsReadOnly && timeChipRect.Contains(Fugui.GetCurrentMouse().Position))
        {
            Fugui.SetMouseCursor(FuMouseCursor.Hand);
            if (Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left))
                _timelineWidget.ToggleClockMode();
        }
    }

    /// <summary>
    /// Draws the top buttons UI.
    /// </summary>
    private void DrawTopButtons(Vector2 containerSize, TimelineWidgetTheme theme, bool inputBlocked)
    {
        float scale = Fugui.Scale;
        FuDrawList drawList = Fugui.GetCurrentWindowDrawList();
        float buttonSize = theme.TopButtonSize * scale;
        float gap = theme.TopButtonGap * scale;
        bool buttonsInteractable = !inputBlocked;
        bool showNetworkButton = IsMultiplayerSession();
        bool showSeatButton = !showNetworkButton || IsLocalObserver();
        bool showAdminButton = IsAdminMultiplayerSession();
        bool showInteractionButton = CanUsePointingButton();
        float buttonCount = (showSeatButton ? 1f : 0f)
            + (showNetworkButton ? 1f : 0f)
            + (showAdminButton ? 1f : 0f)
            + 2f
            + (showInteractionButton ? 1f : 0f);
        float totalWidth = buttonSize * buttonCount + gap * (buttonCount - 1f);
        float x = containerSize.x - theme.TopButtonMargin * scale - totalWidth;
        float y = theme.TopButtonMargin * scale;

        Rect seatRect = new Rect();
        if (showSeatButton)
        {
            seatRect = new Rect(x, y, buttonSize, buttonSize);
            x = seatRect.xMax + gap;
        }
        else
        {
            _seatMenuOpen = false;
        }

        Rect networkRect = new Rect();
        if (showNetworkButton)
        {
            networkRect = new Rect(x, y, buttonSize, buttonSize);
            x = networkRect.xMax + gap;
        }
        else
        {
            _networkSettingsOpen = false;
        }

        Rect adminRect = new Rect();
        if (showAdminButton)
        {
            adminRect = new Rect(x, y, buttonSize, buttonSize);
            x = adminRect.xMax + gap;
        }
        else
        {
            _adminPanelOpen = false;
        }

        Rect timelineRect = new Rect(x, y, buttonSize, buttonSize);
        x = timelineRect.xMax + gap;
        Rect interactionRect = new Rect();
        if (showInteractionButton)
        {
            interactionRect = new Rect(x, y, buttonSize, buttonSize);
            x = interactionRect.xMax + gap;
        }
        else
        {
            FlatRaycaster.Current?.EndPointing();
        }
        Rect settingsRect = new Rect(x, y, buttonSize, buttonSize);

        if (inputBlocked)
            _seatMenuOpen = false;

        if (showSeatButton && DrawGlassIconButton(drawList, seatRect, null, "timelineSeat", theme, _seatMenuOpen, GlassIconKind.SeatPosition, GetCurrentSeat(), 1f, SaraShortcutSettings.GetTooltip(SaraShortcutAction.ToggleSeatMenu), buttonsInteractable))
            ToggleSeatMenu();

        FlatRaycaster flatPointer = FlatRaycaster.Current;
        if (showNetworkButton && DrawGlassIconButton(drawList, networkRect, null, "networkVoiceSettings", theme, _networkSettingsOpen || _networkSettingsOpenAmount > 0.001f, GlassIconKind.NetworkVoice, GetCurrentSeat(), 1f, SaraShortcutSettings.GetTooltip(SaraShortcutAction.OpenNetworkSettings), buttonsInteractable))
            OpenNetworkSettingsPanel();

        if (showAdminButton && DrawGlassIconButton(drawList, adminRect, null, "adminPanel", theme, _adminPanelOpen || _adminPanelOpenAmount > 0.001f, GlassIconKind.AdminSeats, GetCurrentSeat(), 1f, SaraShortcutSettings.GetTooltip(SaraShortcutAction.OpenAdminPanel), buttonsInteractable, HasPendingObserverUnmuteRequest()))
            OpenAdminPanel();

        if (DrawGlassIconButton(drawList, timelineRect, null, "timelineDockToggle", theme, _timelineRetracted, GlassIconKind.Timeline, GetCurrentSeat(), 1f, SaraShortcutSettings.GetTooltip(SaraShortcutAction.ToggleTimelineDock), buttonsInteractable))
            _timelineRetracted = !_timelineRetracted;

        bool interactionActive = flatPointer != null && flatPointer.IsPointingActive;
        if (showInteractionButton && DrawGlassIconButton(drawList, interactionRect, Icons.LocationArrow_solid, "flatInteractionMode", theme, interactionActive, GlassIconKind.Font, GetCurrentSeat(), 1f, SaraShortcutSettings.GetTooltip(SaraShortcutAction.TogglePointing), buttonsInteractable))
            TogglePointing();

        if (DrawGlassIconButton(drawList, settingsRect, Icons.Gear_solid, "timelineSettings", theme, _settingsOpen || _settingsOpenAmount > 0.001f, GlassIconKind.Font, GetCurrentSeat(), 1f, SaraShortcutSettings.GetTooltip(SaraShortcutAction.OpenSettings), buttonsInteractable))
            OpenSettingsPanel();

        if (showSeatButton && buttonsInteractable)
            DrawSeatMenu(seatRect, theme);
    }

    /// <summary>
    /// Draws the pointing reticle UI.
    /// </summary>
    private static void DrawPointingReticle(Vector2 containerSize, TimelineWidgetTheme theme)
    {
        FlatRaycaster flatPointer = FlatRaycaster.Current;
        if (flatPointer == null || !flatPointer.IsPointingActive)
            return;

        float scale = Fugui.Scale;
        Vector2 center = containerSize * 0.5f;
        float gap = 5f * scale;
        float length = 11f * scale;
        float thin = Mathf.Max(1.5f * scale, 1f);
        float thick = thin + Mathf.Max(2f * scale, 1f);
        FuDrawList drawList = Fugui.GetCurrentWindowDrawList();
        uint shadow = ColorU32(new Color(0f, 0f, 0f, 0.72f));
        uint color = ColorU32(theme.Accent);

        DrawReticleLines(drawList, center, gap, length, shadow, thick);
        DrawReticleLines(drawList, center, gap, length, color, thin);
    }

    /// <summary>
    /// Draws the reticle lines UI.
    /// </summary>
    private static void DrawReticleLines(FuDrawList drawList, Vector2 center, float gap, float length, uint color, float thickness)
    {
        drawList.AddLine(new Vector2(center.x - gap - length, center.y), new Vector2(center.x - gap, center.y), color, thickness);
        drawList.AddLine(new Vector2(center.x + gap, center.y), new Vector2(center.x + gap + length, center.y), color, thickness);
        drawList.AddLine(new Vector2(center.x, center.y - gap - length), new Vector2(center.x, center.y - gap), color, thickness);
        drawList.AddLine(new Vector2(center.x, center.y + gap), new Vector2(center.x, center.y + gap + length), color, thickness);
    }


    #endregion

    #region Seat Menu
    /// <summary>
    /// Draws the seat menu UI.
    /// </summary>
    private void DrawSeatMenu(Rect anchorRect, TimelineWidgetTheme theme)
    {
        if (!_seatMenuOpen)
            return;

        if (!CanChangeLocalSeat())
        {
            _seatMenuOpen = false;
            return;
        }

        float scale = Fugui.Scale;
        FuDrawList drawList = Fugui.GetCurrentWindowDrawList();
        float width = 200f * scale;
        float rowHeight = 40f * scale;
        Rect menuRect = new Rect(anchorRect.xMax - width, anchorRect.yMax + 8f * scale, width, rowHeight * 3f + 12f * scale);
        float rounding = theme.MediumRadius * scale;

        FlatCameraInputBlocker.RegisterRect(menuRect);
        drawList.AddRectFilled(menuRect.min, menuRect.max, ColorU32(theme.MenuBackground), rounding);
        drawList.AddRect(menuRect.min, menuRect.max, ColorU32(theme.DockBorder), rounding);

        DrawSeatOption(new Rect(menuRect.x + 6f * scale, menuRect.y + 6f * scale, menuRect.width - 12f * scale, rowHeight), SeatType.Pilot, "Left seat", theme);
        DrawSeatOption(new Rect(menuRect.x + 6f * scale, menuRect.y + 6f * scale + rowHeight, menuRect.width - 12f * scale, rowHeight), SeatType.Center, "Center", theme);
        DrawSeatOption(new Rect(menuRect.x + 6f * scale, menuRect.y + 6f * scale + rowHeight * 2f, menuRect.width - 12f * scale, rowHeight), SeatType.CoPilot, "Right seat", theme);

        if (!_seatMenuOpenedThisFrame && Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left) && !menuRect.Contains(Fugui.GetCurrentMouse().Position) && !anchorRect.Contains(Fugui.GetCurrentMouse().Position))
            _seatMenuOpen = false;

        _seatMenuOpenedThisFrame = false;
    }

    /// <summary>
    /// Draws the seat option UI.
    /// </summary>
    private void DrawSeatOption(Rect rect, SeatType seat, string label, TimelineWidgetTheme theme)
    {
        FuDrawList drawList = Fugui.GetCurrentWindowDrawList();
        bool selected = Sara.Cockpit != null && Sara.Cockpit.CurrentSeatType == seat;
        bool hovered = rect.Contains(Fugui.GetCurrentMouse().Position);
        bool active = hovered && Fugui.GetCurrentMouse().IsPressed(FuMouseButton.Left);
        Color bg = selected ? theme.PillBackgroundActive : active ? theme.PillBackgroundActive : hovered ? theme.PillBackgroundHover : Color.clear;

        if (bg.a > 0f)
            drawList.AddRectFilled(rect.min, rect.max, ColorU32(bg), theme.SmallRadius * Fugui.Scale);

        Rect iconRect = new Rect(rect.x + 10f * Fugui.Scale, rect.y, 24f * Fugui.Scale, rect.height);
        DrawSeatPositionIcon(drawList, iconRect, seat, selected ? theme.Accent : theme.TextFaint, selected ? theme.TextDim : theme.TextFaint);

        PushCompactFont(12);
        Rect textRect = new Rect(iconRect.xMax + 10f * Fugui.Scale, rect.y, rect.width - 54f * Fugui.Scale, rect.height);
        DrawTextLeftCentered(drawList, textRect, label, ColorU32(selected ? theme.Accent : theme.TextDim), 0f);
        PopCompactFont();

        if (selected)
        {
            Rect checkRect = new Rect(rect.xMax - 32f * Fugui.Scale, rect.y, 18f * Fugui.Scale, rect.height);
            DrawCheckIcon(drawList, checkRect, theme.Accent);
        }

        if (!hovered)
            return;

        Fugui.SetMouseCursor(FuMouseCursor.Hand);
        if (Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left))
        {
            if (Sara.Cockpit != null && CanChangeLocalSeat())
                SelectLocalSeat(seat);

            _seatMenuOpen = false;
        }
    }


    /// <summary>
    /// Draws the settings panel UI.
    /// </summary>
    private void DrawSettingsPanel(Vector2 containerSize, TimelineWidgetTheme theme)
    {
        float step = Time.unscaledDeltaTime / Mathf.Max(0.001f, theme.SettingsTransitionSeconds);
        _settingsOpenAmount = Mathf.MoveTowards(_settingsOpenAmount, _settingsOpen ? 1f : 0f, step);

        if (!_settingsOpen && _settingsOpenAmount <= 0.001f)
            return;

        float scale = Fugui.Scale;
        float t = SmoothStep01(_settingsOpenAmount);
        FuDrawList drawList = Fugui.GetCurrentWindowDrawList();
        Rect overlayRect = new Rect(0f, 0f, containerSize.x, containerSize.y);
        float panelWidth = Mathf.Min(theme.SettingsPanelWidth * scale, containerSize.x * 0.92f);
        Rect panelRect = new Rect(Mathf.Lerp(containerSize.x, containerSize.x - panelWidth, t), 0f, panelWidth, containerSize.y);

        FlatRaycaster.Current?.EndPointing();
        FlatCameraInputBlocker.BlockAllForFrame();
        drawList.AddRectFilled(overlayRect.min, overlayRect.max, ColorU32(theme.SettingsOverlay, t));
        drawList.AddRectFilled(panelRect.min, panelRect.max, ColorU32(theme.SettingsPanelBackground, t));
        drawList.AddLine(panelRect.min, new Vector2(panelRect.x, panelRect.yMax), ColorU32(theme.DockBorder, t), Mathf.Max(1f, scale));

        if (_settingsWidget.Draw(panelRect, t))
            _settingsOpen = false;

        bool clickedOutside = _settingsOpen
            && !_settingsOpenedThisFrame
            && _settingsOpenAmount > 0.92f
            && Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left)
            && !panelRect.Contains(Fugui.GetCurrentMouse().Position);

        if (clickedOutside)
            _settingsOpen = false;

        _settingsOpenedThisFrame = false;
    }

    /// <summary>
    /// Draws the network settings panel UI.
    /// </summary>
    private void DrawNetworkSettingsPanel(Vector2 containerSize, TimelineWidgetTheme theme)
    {
        if (!IsMultiplayerSession())
            _networkSettingsOpen = false;

        float step = Time.unscaledDeltaTime / Mathf.Max(0.001f, theme.SettingsTransitionSeconds);
        _networkSettingsOpenAmount = Mathf.MoveTowards(_networkSettingsOpenAmount, _networkSettingsOpen ? 1f : 0f, step);

        if (!_networkSettingsOpen && _networkSettingsOpenAmount <= 0.001f)
            return;

        float scale = Fugui.Scale;
        float t = SmoothStep01(_networkSettingsOpenAmount);
        FuDrawList drawList = Fugui.GetCurrentWindowDrawList();
        Rect overlayRect = new Rect(0f, 0f, containerSize.x, containerSize.y);
        float panelWidth = Mathf.Min(theme.SettingsPanelWidth * scale, containerSize.x * 0.92f);
        Rect panelRect = new Rect(Mathf.Lerp(containerSize.x, containerSize.x - panelWidth, t), 0f, panelWidth, containerSize.y);

        FlatRaycaster.Current?.EndPointing();
        FlatCameraInputBlocker.BlockAllForFrame();
        drawList.AddRectFilled(overlayRect.min, overlayRect.max, ColorU32(theme.SettingsOverlay, t));
        drawList.AddRectFilled(panelRect.min, panelRect.max, ColorU32(theme.SettingsPanelBackground, t));
        drawList.AddLine(panelRect.min, new Vector2(panelRect.x, panelRect.yMax), ColorU32(theme.DockBorder, t), Mathf.Max(1f, scale));

        if (_networkSettingsWidget.Draw(panelRect, t))
            _networkSettingsOpen = false;

        bool clickedOutside = _networkSettingsOpen
            && !_networkSettingsOpenedThisFrame
            && _networkSettingsOpenAmount > 0.92f
            && Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left)
            && !panelRect.Contains(Fugui.GetCurrentMouse().Position);

        if (clickedOutside)
            _networkSettingsOpen = false;

        _networkSettingsOpenedThisFrame = false;
    }

    /// <summary>
    /// Draws the multiplayer admin panel UI.
    /// </summary>
    private void DrawAdminPanel(Vector2 containerSize, TimelineWidgetTheme theme)
    {
        if (!IsAdminMultiplayerSession())
            _adminPanelOpen = false;

        float step = Time.unscaledDeltaTime / Mathf.Max(0.001f, theme.SettingsTransitionSeconds);
        _adminPanelOpenAmount = Mathf.MoveTowards(_adminPanelOpenAmount, _adminPanelOpen ? 1f : 0f, step);

        if (!_adminPanelOpen && _adminPanelOpenAmount <= 0.001f)
            return;

        float scale = Fugui.Scale;
        float t = SmoothStep01(_adminPanelOpenAmount);
        FuDrawList drawList = Fugui.GetCurrentWindowDrawList();
        Rect overlayRect = new Rect(0f, 0f, containerSize.x, containerSize.y);
        float panelWidth = Mathf.Min(theme.SettingsPanelWidth * scale, containerSize.x * 0.92f);
        Rect panelRect = new Rect(Mathf.Lerp(containerSize.x, containerSize.x - panelWidth, t), 0f, panelWidth, containerSize.y);

        FlatRaycaster.Current?.EndPointing();
        FlatCameraInputBlocker.BlockAllForFrame();
        drawList.AddRectFilled(overlayRect.min, overlayRect.max, ColorU32(theme.SettingsOverlay, t));
        drawList.AddRectFilled(panelRect.min, panelRect.max, ColorU32(theme.SettingsPanelBackground, t));
        drawList.AddLine(panelRect.min, new Vector2(panelRect.x, panelRect.yMax), ColorU32(theme.DockBorder, t), Mathf.Max(1f, scale));

        if (_adminPanelWidget.Draw(panelRect, t))
            _adminPanelOpen = false;

        bool clickedOutside = _adminPanelOpen
            && !_adminPanelOpenedThisFrame
            && _adminPanelOpenAmount > 0.92f
            && Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left)
            && !panelRect.Contains(Fugui.GetCurrentMouse().Position);

        if (clickedOutside)
            _adminPanelOpen = false;

        _adminPanelOpenedThisFrame = false;
    }

    /// <summary>
    /// Draws the glass icon button UI.
    /// </summary>
    private static bool DrawGlassIconButton(FuDrawList drawList, Rect rect, string icon, string id, TimelineWidgetTheme theme, bool selected, GlassIconKind iconKind, SeatType seat, float opacity, string tooltip, bool interactable = true, bool alert = false)
    {
        opacity = Mathf.Clamp01(opacity);
        bool hovered = interactable && rect.Contains(Fugui.GetCurrentMouse().Position);
        bool active = hovered && Fugui.GetCurrentMouse().IsPressed(FuMouseButton.Left);
        bool clicked = hovered && Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left);

        FlatCameraInputBlocker.RegisterRect(rect);
        DrawGlassShell(drawList, rect, theme, theme.MediumRadius * Fugui.Scale, selected || hovered || active, opacity);

        Color iconColor = selected ? theme.Accent : theme.Text;
        if (active)
            iconColor = theme.AccentHi;

        switch (iconKind)
        {
            case GlassIconKind.Timeline:
                DrawTimelineIcon(drawList, rect, iconColor, opacity);
                break;

            case GlassIconKind.SeatPosition:
                DrawSeatPositionIcon(drawList, rect, seat, iconColor, theme.TextFaint, opacity);
                break;

            case GlassIconKind.NetworkVoice:
                DrawNetworkVoiceIcon(drawList, rect, iconColor, opacity);
                break;

            case GlassIconKind.AdminSeats:
                DrawAdminSeatsIcon(drawList, rect, iconColor, opacity);
                break;

            case GlassIconKind.CaretUp:
                DrawCaretIcon(drawList, rect, iconColor, true, opacity);
                break;

            default:
                Fugui.PushFont(Mathf.RoundToInt(theme.TopButtonIconFontSize));
                DrawIconCenteredTinted(drawList, rect, icon, ColorU32(iconColor, opacity), ColorU32(iconColor, opacity), ColorU32(iconColor, opacity));
                Fugui.PopFont();
                break;
        }

        if (alert)
            DrawAlertDot(drawList, rect, opacity);

        if (hovered)
        {
            Fugui.SetMouseCursor(FuMouseCursor.Hand);
            if (!string.IsNullOrEmpty(tooltip))
                ShortcutTooltipLayout.SetToolTip(id + "Shortcut", tooltip, true);
        }

        return clicked;
    }

    /// <summary>
    /// Draws a compact red notification dot on a top-bar button.
    /// </summary>
    private static void DrawAlertDot(FuDrawList drawList, Rect rect, float opacity)
    {
        float scale = Fugui.Scale;
        Vector2 center = new Vector2(rect.xMax - 9f * scale, rect.y + 9f * scale);
        drawList.AddCircleFilled(center, 5.2f * scale, ColorU32(new Color(1f, 0.13f, 0.13f, 1f), opacity), 16);
        drawList.AddCircle(center, 5.2f * scale, ColorU32(new Color(1f, 1f, 1f, 0.72f), opacity), 16, Mathf.Max(1f, scale));
    }

    /// <summary>
    /// Draws the glass shell UI.
    /// </summary>
    private static void DrawGlassShell(FuDrawList drawList, Rect rect, TimelineWidgetTheme theme, float rounding, bool highlighted, float opacity)
    {
        Color fill = highlighted ? theme.ButtonHover : theme.DockBackground;
        drawList.AddRectFilled(rect.min, rect.max, ColorU32(fill, opacity), rounding);
        drawList.AddRect(rect.min, rect.max, ColorU32(theme.DockBorder, opacity), rounding);
    }

    /// <summary>
    /// Draws the timeline icon UI.
    /// </summary>
    private static void DrawTimelineIcon(FuDrawList drawList, Rect rect, Color color, float opacity)
    {
        float scale = Fugui.Scale;
        Vector2 center = rect.center;
        float w = 20f * scale;
        float h = 17f * scale;
        Rect screenRect = new Rect(center.x - w * 0.5f, center.y - h * 0.48f, w, h * 0.58f);
        uint col = ColorU32(color, opacity);
        float thickness = Mathf.Max(1.5f * scale, 1f);

        drawList.AddRect(screenRect.min, screenRect.max, col, 2.5f * scale, FuDrawFlags.None, thickness);
        Vector2 stemTop = new Vector2(center.x, screenRect.yMax);
        Vector2 stemBottom = new Vector2(center.x, center.y + h * 0.42f);
        drawList.AddLine(stemTop, stemBottom, col, thickness);
        drawList.AddLine(stemBottom, stemBottom + new Vector2(-5f * scale, -4f * scale), col, thickness);
        drawList.AddLine(stemBottom, stemBottom + new Vector2(5f * scale, -4f * scale), col, thickness);
    }

    /// <summary>
    /// Draws the network voice icon UI.
    /// </summary>
    private static void DrawNetworkVoiceIcon(FuDrawList drawList, Rect rect, Color color, float opacity)
    {
        float scale = Fugui.Scale;
        Vector2 center = rect.center;
        uint col = ColorU32(color, opacity);
        float thickness = Mathf.Max(1.6f * scale, 1f);

        // Microphone capsule.
        Rect micRect = new Rect(center.x - 5f * scale, center.y - 13f * scale, 10f * scale, 17f * scale);
        drawList.AddRect(micRect.min, micRect.max, col, 5f * scale, FuDrawFlags.None, thickness);

        // Stem and base.
        drawList.AddLine(new Vector2(center.x, micRect.yMax), new Vector2(center.x, center.y + 11f * scale), col, thickness);
        drawList.AddLine(new Vector2(center.x - 7f * scale, center.y + 11f * scale), new Vector2(center.x + 7f * scale, center.y + 11f * scale), col, thickness);

        // Side waves imply network voice rather than a plain local mic.
        drawList.AddLine(new Vector2(center.x - 13f * scale, center.y - 3f * scale), new Vector2(center.x - 16f * scale, center.y + 1f * scale), col, thickness);
        drawList.AddLine(new Vector2(center.x + 13f * scale, center.y - 3f * scale), new Vector2(center.x + 16f * scale, center.y + 1f * scale), col, thickness);
    }

    /// <summary>
    /// Draws the admin seats icon.
    /// </summary>
    private static void DrawAdminSeatsIcon(FuDrawList drawList, Rect rect, Color color, float opacity)
    {
        float scale = Fugui.Scale;
        Vector2 center = rect.center;
        uint col = ColorU32(color, opacity);
        float rounding = Mathf.Max(1.5f * scale, 1f);
        float size = 6f * scale;
        float gap = 4f * scale;
        float y = center.y - 6f * scale;
        Rect left = new Rect(center.x - size * 1.5f - gap, y, size, size + 8f * scale);
        Rect middle = new Rect(center.x - size * 0.5f, y + 6f * scale, size, size + 8f * scale);
        Rect right = new Rect(center.x + size * 0.5f + gap, y, size, size + 8f * scale);

        drawList.AddRect(left.min, left.max, col, rounding, FuDrawFlags.None, Mathf.Max(1.6f * scale, 1f));
        drawList.AddRectFilled(middle.min, middle.max, col, rounding);
        drawList.AddRect(right.min, right.max, col, rounding, FuDrawFlags.None, Mathf.Max(1.6f * scale, 1f));

        Vector2 crownCenter = new Vector2(center.x, center.y - 14f * scale);
        drawList.AddTriangleFilled(
            crownCenter + new Vector2(-7f * scale, 5f * scale),
            crownCenter,
            crownCenter + new Vector2(7f * scale, 5f * scale),
            col);
    }

    /// <summary>
    /// Draws the caret icon UI.
    /// </summary>
    private static void DrawCaretIcon(FuDrawList drawList, Rect rect, Color color, bool up, float opacity)
    {
        float scale = Fugui.Scale;
        Vector2 center = rect.center;
        float halfWidth = 6f * scale;
        float halfHeight = 4f * scale;
        float sign = up ? -1f : 1f;
        uint col = ColorU32(color, opacity);
        float thickness = Mathf.Max(2f * scale, 1f);

        Vector2 tip = new Vector2(center.x, center.y + sign * halfHeight);
        drawList.AddLine(new Vector2(center.x - halfWidth, center.y - sign * halfHeight), tip, col, thickness);
        drawList.AddLine(tip, new Vector2(center.x + halfWidth, center.y - sign * halfHeight), col, thickness);
    }


    /// <summary>
    /// Draws the seat position icon UI.
    /// </summary>
    private static void DrawSeatPositionIcon(FuDrawList drawList, Rect rect, SeatType seat, Color selectedColor, Color inactiveColor)
    {
        DrawSeatPositionIcon(drawList, rect, seat, selectedColor, inactiveColor, 1f);
    }

    /// <summary>
    /// Draws the seat position icon UI.
    /// </summary>
    private static void DrawSeatPositionIcon(FuDrawList drawList, Rect rect, SeatType seat, Color selectedColor, Color inactiveColor, float opacity)
    {
        float scale = Fugui.Scale;
        Vector2 center = rect.center;
        float width = 22f * scale;
        float x = center.x - width * 0.5f;
        float y = center.y - 6f * scale;
        Rect leftRect = new Rect(x, y + 2f * scale, 5f * scale, 9f * scale);
        Rect centerRect = new Rect(x + 8f * scale, y, 5f * scale, 13f * scale);
        Rect rightRect = new Rect(x + 16f * scale, y + 2f * scale, 5f * scale, 9f * scale);

        DrawSeatSegment(drawList, leftRect, seat == SeatType.Pilot, selectedColor, inactiveColor, opacity);
        DrawSeatSegment(drawList, centerRect, seat == SeatType.Center, selectedColor, inactiveColor, opacity);
        DrawSeatSegment(drawList, rightRect, seat == SeatType.CoPilot, selectedColor, inactiveColor, opacity);
    }

    /// <summary>
    /// Draws the seat segment UI.
    /// </summary>
    private static void DrawSeatSegment(FuDrawList drawList, Rect rect, bool selected, Color selectedColor, Color inactiveColor, float opacity)
    {
        float rounding = Mathf.Max(1.5f * Fugui.Scale, 1f);
        uint selectedCol = ColorU32(selectedColor, opacity);
        uint inactiveCol = ColorU32(inactiveColor, opacity * 0.72f);

        if (selected)
            drawList.AddRectFilled(rect.min, rect.max, selectedCol, rounding);
        else
            drawList.AddRect(rect.min, rect.max, inactiveCol, rounding, FuDrawFlags.None, Mathf.Max(1.5f * Fugui.Scale, 1f));
    }

    /// <summary>
    /// Draws the check icon UI.
    /// </summary>
    private static void DrawCheckIcon(FuDrawList drawList, Rect rect, Color color)
    {
        float scale = Fugui.Scale;
        Vector2 center = rect.center;
        uint col = ColorU32(color);
        float thickness = Mathf.Max(1.8f * scale, 1f);
        drawList.AddLine(center + new Vector2(-5f * scale, 0f), center + new Vector2(-1f * scale, 4f * scale), col, thickness);
        drawList.AddLine(center + new Vector2(-1f * scale, 4f * scale), center + new Vector2(7f * scale, -5f * scale), col, thickness);
    }

    /// <summary>
    /// Returns the current seat value.
    /// </summary>
    private static SeatType GetCurrentSeat()
    {
        return Sara.Cockpit != null ? Sara.Cockpit.CurrentSeatType : SeatType.Pilot;
    }

    /// <summary>
    /// Returns whether the change local seat action is allowed.
    /// </summary>
    private static bool CanChangeLocalSeat()
    {
        SaraUser user = Sara.CurrentSession != null ? Sara.CurrentSession.User : null;
        if (IsMultiplayerSession())
            return user != null && user.IsObservator;

        return user == null
            || user.Role == SaraUserRole.Unknown
            || user.IsAdmin;
    }

    /// <summary>
    /// Returns whether the current session is multiplayer.
    /// </summary>
    private static bool IsMultiplayerSession()
    {
        return Sara.CurrentSession != null && Sara.CurrentSession.IsMultiplayer;
    }

    /// <summary>
    /// Returns whether the local session user can access admin controls.
    /// </summary>
    private static bool IsAdminMultiplayerSession()
    {
        SaraUser user = Sara.CurrentSession != null ? Sara.CurrentSession.User : null;
        return Sara.CurrentSession != null
            && Sara.CurrentSession.IsMultiplayer
            && user != null
            && user.IsAdmin;
    }

    /// <summary>
    /// Returns whether the local user is a multiplayer observer.
    /// </summary>
    private static bool IsLocalObserver()
    {
        SaraUser user = Sara.CurrentSession != null ? Sara.CurrentSession.User : null;
        return Sara.CurrentSession != null
            && Sara.CurrentSession.IsMultiplayer
            && user != null
            && user.IsObservator;
    }

    /// <summary>
    /// Returns whether the local user can access the pointing button.
    /// </summary>
    private static bool CanUsePointingButton()
    {
        SaraUser user = Sara.CurrentSession != null ? Sara.CurrentSession.User : null;
        return Sara.CurrentSession == null
            || user == null
            || user.CanPoint;
    }

    /// <summary>
    /// Returns whether at least one observer is waiting for admin unmute.
    /// </summary>
    private static bool HasPendingObserverUnmuteRequest()
    {
        SaraSession session = Sara.CurrentSession;
        if (session == null || session.Users == null)
            return false;

        for (int i = 0; i < session.Users.Length; i++)
        {
            SaraSessionUser sessionUser = session.Users[i];
            if (sessionUser != null
                && sessionUser.User != null
                && sessionUser.User.IsObservator
                && sessionUser.User.WantsUnmute)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Selects the local seat option.
    /// </summary>
    private static void SelectLocalSeat(SeatType seat)
    {
        if (!CanChangeLocalSeat())
            return;

        if (Sara.Network != null && Sara.CurrentSession != null && Sara.CurrentSession.IsMultiplayer)
        {
            if (Sara.CurrentSession.User != null && Sara.CurrentSession.User.IsObservator)
                Sara.Network.SelectLocalSeatOnly(seat);
            else
                Sara.Network.SelectSeat(seat, null);
            return;
        }

        Sara.Cockpit.SetupCamera(seat);
    }

    /// <summary>
    /// Opens the settings panel.
    /// </summary>
    private void OpenSettingsPanel()
    {
        FlatRaycaster.Current?.EndPointing();
        _settingsOpen = true;
        _settingsOpenedThisFrame = true;
        _networkSettingsOpen = false;
        _adminPanelOpen = false;
        _seatMenuOpen = false;
    }

    /// <summary>
    /// Opens the network settings panel when available.
    /// </summary>
    private void OpenNetworkSettingsPanel()
    {
        if (!IsMultiplayerSession())
            return;

        FlatRaycaster.Current?.EndPointing();
        _networkSettingsOpen = true;
        _networkSettingsOpenedThisFrame = true;
        _settingsOpen = false;
        _adminPanelOpen = false;
        _seatMenuOpen = false;
    }

    /// <summary>
    /// Opens the admin panel when available.
    /// </summary>
    private void OpenAdminPanel()
    {
        if (!IsAdminMultiplayerSession())
            return;

        FlatRaycaster.Current?.EndPointing();
        _adminPanelOpen = true;
        _adminPanelOpenedThisFrame = true;
        _networkSettingsOpen = false;
        _settingsOpen = false;
        _seatMenuOpen = false;
    }

    /// <summary>
    /// Toggles the seat menu when available.
    /// </summary>
    private void ToggleSeatMenu()
    {
        if (!CanChangeLocalSeat())
            return;

        FlatRaycaster.Current?.EndPointing();
        _seatMenuOpen = !_seatMenuOpen;
        _seatMenuOpenedThisFrame = _seatMenuOpen;
        _networkSettingsOpen = false;
        _adminPanelOpen = false;
        _settingsOpen = false;
    }

    /// <summary>
    /// Toggles flat pointing mode when available.
    /// </summary>
    private static void TogglePointing()
    {
        if (!CanUsePointingButton())
            return;

        FlatRaycaster.Current?.TogglePointing();
    }

    /// <summary>
    /// Opens the shortcut remapping popup.
    /// </summary>
    private void OpenShortcutSettingsPopup()
    {
        _shortcutSettingsWidget.Open();
    }


    #endregion

    #region Drawing Utilities
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
    /// Runs the rgba logic.
    /// </summary>
    private static Color Rgba(int r, int g, int b, float alpha)
    {
        return new Color(r / 255f, g / 255f, b / 255f, alpha);
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
    /// Runs the push compact font logic.
    /// </summary>
    private static void PushCompactFont(int size)
    {
        Fugui.PushFont(size);
        Fugui.PushFont(FontType.Bold);
    }

    /// <summary>
    /// Runs the pop compact font logic.
    /// </summary>
    private static void PopCompactFont()
    {
        Fugui.PopFont();
        Fugui.PopFont();
    }


    /// <summary>
    /// Draws the text centered UI.
    /// </summary>
    private static void DrawTextCentered(FuDrawList drawList, Rect rect, string text, uint color)
    {
        Vector2 textSize = Fugui.CalcTextSize(text);
        Vector2 textPos = new Vector2(rect.x + (rect.width - textSize.x) * 0.5f, rect.y + (rect.height - textSize.y) * 0.5f);
        drawList.AddText(textPos, color, text);
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
    /// Draws the icon centered tinted UI.
    /// </summary>
    private static void DrawIconCenteredTinted(FuDrawList drawList, Rect rect, string icon, uint solidColor, uint duotonePrimaryColor, uint duotoneSecondaryColor)
    {
        if (string.IsNullOrEmpty(icon))
            return;

        Vector2 iconSize = Fugui.CalcTextSize(icon, FuTextWrapping.None);
        Vector2 iconPos = new Vector2(rect.x + (rect.width - iconSize.x) * 0.5f, rect.y + (rect.height - iconSize.y) * 0.5f);
        char primary = icon[0];

        if (Fugui.IsDuoToneChar(primary))
        {
            drawList.AddText(iconPos, duotonePrimaryColor, primary.ToString());
            drawList.AddText(iconPos, duotoneSecondaryColor, ((char)(((ushort)primary) + 1)).ToString());
        }
        else
        {
            drawList.AddText(iconPos, solidColor, icon);
        }
    }
    #endregion
}
