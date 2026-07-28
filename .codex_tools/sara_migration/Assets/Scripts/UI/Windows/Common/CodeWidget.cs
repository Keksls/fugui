using Assets.Scripts.UI.Windows.Common;
using Fu;
using Fu.Framework;
using NetSquare.Client;
using Saravr.Network;
using Saravr.Network.Common;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Implements the code widget logic.
/// </summary>
public class CodeWidget
{
    private const int MaxCodeLength = 6;
    private const float DesktopPanelMaxWidth = 620f;
    private const float MobilePanelMaxWidth = 540f;
    private const float CompactHeightThreshold = 500f;

    private static readonly Color ErrorColor = new Color(1f, 0.36f, 0.36f, 1f);
    private static readonly Color SuccessColor = new Color(0.42f, 0.83f, 0.55f, 1f);
    private static readonly Color WarningColor = new Color(1f, 0.71f, 0.28f, 1f);

    private TimelineWidgetTheme _theme;
    private string _code = string.Empty;
    private bool _sending;
    private bool _hasError;
    private bool _keyboardFocused = true;
    private int _lastKeyboardFrame = -1;
    private int _submissionGeneration;
    private string _statusMessage = "Waiting for flight code";

    public TimelineWidgetTheme Theme
    {
        get { return _theme != null ? _theme : TimelineWidgetTheme.LoadDefault(); }
        set { _theme = value; }
    }

    #region Panel Entry Points
    /// <summary>
    /// Sets the theme value.
    /// </summary>
    public void SetTheme(TimelineWidgetTheme theme)
    {
        _theme = theme;
    }

    /// <summary>
    /// Draws the code panel UI.
    /// </summary>
    public Rect DrawCodePanel(FuWindow window, bool drawBackground = false)
    {
        if (window == null || window.Container == null)
            return new Rect();

        Vector2 containerSize = new Vector2(window.Container.Size.x, window.Container.Size.y);
        return DrawCodePanel(Fugui.GetCurrentWindowDrawList(), window.LocalPosition, containerSize, drawBackground);
    }

    /// <summary>
    /// Draws the code panel UI.
    /// </summary>
    public Rect DrawCodePanel(FuDrawList drawList, Vector2 origin, Vector2 containerSize, bool drawBackground = false)
    {
        if (containerSize.x <= 0f || containerSize.y <= 0f)
            return new Rect(origin.x, origin.y, 0f, 0f);

        float scale = Fugui.Scale;
        bool mobileLayout = IsMobileLayout(containerSize, scale);
        bool compactLayout = IsCompactLayout(containerSize, scale);
        Rect viewportRect = new Rect(origin.x, origin.y, containerSize.x, containerSize.y);
        Rect safeRect = GetSafeContentRect(origin, containerSize, mobileLayout, scale);
        Rect panelRect = GetPanelRect(safeRect, mobileLayout, compactLayout, scale);
        TimelineWidgetTheme theme = Theme;

        if (drawBackground)
            DrawBackground(drawList, viewportRect, scale, theme);
        else
            panelRect = new Rect(origin, containerSize);

        Fugui.PushFont(18);
        DrawCodePanel(drawList, panelRect, mobileLayout, compactLayout, scale);
        Fugui.PopFont();

        Fugui.SetCursorScreenPos(new Vector2(origin.x, origin.y + containerSize.y));
        return panelRect;
    }

    /// <summary>
    /// Draws the code panel UI.
    /// </summary>
    public void DrawCodePanel(FuDrawList drawList, Rect panelRect, bool mobileLayout, bool compactLayout, float scale)
    {
        TimelineWidgetTheme theme = Theme;
        float padding = (mobileLayout ? 18f : 22f) * scale;
        float rounding = (mobileLayout ? theme.MediumRadius : theme.DockRadius) * scale;

        if (!Sara.IsVR)
            FlatCameraInputBlocker.BlockAllForFrame();

        drawList.AddRectFilled(panelRect.min + new Vector2(0f, 6f * scale), panelRect.max + new Vector2(0f, 8f * scale), ColorU32(theme.DockShadow, 0.70f), rounding);
        drawList.AddRectFilled(panelRect.min, panelRect.max, ColorU32(theme.DockBackground), rounding);
        drawList.AddRect(panelRect.min, panelRect.max, ColorU32(theme.DockBorder), rounding);

        float headerHeight = (compactLayout ? 64f : 78f) * scale;
        Rect headerRect = new Rect(panelRect.x, panelRect.y, panelRect.width, headerHeight);
        Rect contentRect = new Rect(
            panelRect.x + padding,
            headerRect.yMax + (compactLayout ? 10f : 14f) * scale,
            Mathf.Max(1f, panelRect.width - padding * 2f),
            Mathf.Max(1f, panelRect.yMax - headerRect.yMax - padding - (compactLayout ? 10f : 14f) * scale));

        drawList.PushClipRect(panelRect.min, panelRect.max, true);
        SynchronizeServerVersionStatus();
        HandleKeyboardInput();
        DrawHeader(drawList, headerRect, mobileLayout, compactLayout, scale, theme);
        DrawBody(drawList, contentRect, compactLayout, scale, theme);
        drawList.PopClipRect();
    }

    /// <summary>
    /// Draws the background UI.
    /// </summary>
    public static void DrawBackground(FuDrawList drawList, Rect rect, float scale, TimelineWidgetTheme theme = null)
    {
        theme = theme != null ? theme : TimelineWidgetTheme.LoadDefault();
        uint bgColor = ColorU32(WithAlpha(theme.SettingsPanelBackground, 0.96f));
        uint topBandColor = ColorU32(WithAlpha(theme.DockBackground, 0.78f));
        uint accentColor = ColorU32(WithAlpha(theme.AccentGlow, 0.18f));
        uint lineColor = ColorU32(WithAlpha(theme.DockBorder, 0.72f));
        float bandHeight = Mathf.Min(rect.height * 0.34f, 240f * scale);

        drawList.AddRectFilled(rect.min, rect.max, bgColor);
        drawList.AddRectFilled(rect.min, new Vector2(rect.xMax, rect.y + bandHeight), topBandColor);
        drawList.AddRectFilled(
            new Vector2(rect.x, rect.y + bandHeight - 2f * scale),
            new Vector2(rect.xMax, rect.y + bandHeight + 2f * scale),
            accentColor,
            0f);
        drawList.AddLine(new Vector2(rect.x, rect.y + bandHeight), new Vector2(rect.xMax, rect.y + bandHeight), lineColor, Mathf.Max(1f, scale));
    }

    /// <summary>
    /// Returns the panel rect value.
    /// </summary>
    public Rect GetPanelRect(Rect safeRect, bool mobileLayout, bool compactLayout, float scale)
    {
        float maxWidth = Mathf.Min(safeRect.width, (mobileLayout ? MobilePanelMaxWidth : DesktopPanelMaxWidth) * scale);
        float minDesktopWidth = Mathf.Min(440f * scale, maxWidth);
        float panelWidth = mobileLayout
            ? maxWidth
            : Mathf.Clamp(safeRect.width * 0.52f, minDesktopWidth, maxWidth);
        float desiredHeight = (compactLayout ? 430f : 540f) * scale;
        float minHeight = Mathf.Min(330f * scale, safeRect.height);
        float panelHeight = Mathf.Clamp(desiredHeight, minHeight, safeRect.height);

        return new Rect(
            safeRect.x + (safeRect.width - panelWidth) * 0.5f,
            safeRect.y + (safeRect.height - panelHeight) * 0.5f,
            panelWidth,
            panelHeight);
    }

    /// <summary>
    /// Returns the safe content rect value.
    /// </summary>
    public static Rect GetSafeContentRect(Vector2 origin, Vector2 containerSize, bool mobileLayout, float scale)
    {
        float margin = (mobileLayout ? 16f : 28f) * scale;
        Rect safeRect = new Rect(origin.x, origin.y, containerSize.x, containerSize.y);

        if (mobileLayout && Screen.width > 0 && Screen.height > 0)
        {
            Rect unitySafeArea = Screen.safeArea;
            if (unitySafeArea.width > 0f && unitySafeArea.height > 0f)
            {
                float scaleX = containerSize.x / Screen.width;
                float scaleY = containerSize.y / Screen.height;
                safeRect = new Rect(
                    origin.x + unitySafeArea.x * scaleX,
                    origin.y + (Screen.height - unitySafeArea.yMax) * scaleY,
                    unitySafeArea.width * scaleX,
                    unitySafeArea.height * scaleY);
            }
        }

        return InsetRect(safeRect, margin);
    }

    /// <summary>
    /// Returns whether the compact layout condition is met.
    /// </summary>
    public static bool IsCompactLayout(Vector2 containerSize, float scale)
    {
        return containerSize.y < CompactHeightThreshold * scale;
    }

    /// <summary>
    /// Returns whether the mobile layout condition is met.
    /// </summary>
    public static bool IsMobileLayout(Vector2 containerSize, float scale)
    {
        return !Sara.IsVR &&
            (Application.isMobilePlatform ||
             Application.platform == RuntimePlatform.Android ||
             Application.platform == RuntimePlatform.IPhonePlayer ||
             containerSize.x <= 720f * scale ||
             containerSize.y > containerSize.x * 1.08f);
    }

    #endregion

    #region Panel Rendering
    /// <summary>
    /// Draws the header UI.
    /// </summary>
    private void DrawHeader(FuDrawList drawList, Rect rect, bool mobileLayout, bool compactLayout, float scale, TimelineWidgetTheme theme)
    {
        float headerPadding = (mobileLayout ? 18f : 22f) * scale;
        float iconSize = (compactLayout ? 34f : 40f) * scale;
        float pillWidth = mobileLayout && rect.width < 390f * scale ? 0f : 92f * scale;
        Rect iconRect = new Rect(rect.x + headerPadding, rect.y + (rect.height - iconSize) * 0.5f, iconSize, iconSize);
        Rect pillRect = new Rect(rect.xMax - headerPadding - pillWidth, rect.y + (rect.height - 28f * scale) * 0.5f, pillWidth, 28f * scale);
        float textRight = pillWidth > 0f ? pillRect.x - 12f * scale : rect.xMax - headerPadding;
        Rect titleRect = new Rect(iconRect.xMax + 14f * scale, rect.y + 16f * scale, Mathf.Max(1f, textRight - iconRect.xMax - 14f * scale), 24f * scale);
        Rect subtitleRect = new Rect(titleRect.x, titleRect.yMax + 3f * scale, titleRect.width, 20f * scale);

        if (_sending)
            DrawSpinner(drawList, iconRect.center, iconRect.width * 0.34f, 4f * scale, scale, theme);
        else
            DrawHeaderIcon(drawList, iconRect, theme);

        PushFont(16, true);
        DrawTextLeft(drawList, titleRect, ClipTextToWidth("Flight access", titleRect.width), ColorU32(theme.Text));
        PopFont(true);

        PushFont(12, false);
        DrawTextLeft(drawList, subtitleRect, ClipTextToWidth("Session code required", subtitleRect.width), ColorU32(theme.TextDim));
        PopFont(false);

        if (pillWidth > 0f)
            DrawConnectionPill(drawList, pillRect, scale, theme);

        drawList.AddLine(
            new Vector2(rect.x, rect.yMax),
            new Vector2(rect.xMax, rect.yMax),
            ColorU32(theme.DockBorder, 0.60f),
            Mathf.Max(1f, scale));
    }

    /// <summary>
    /// Draws the body UI.
    /// </summary>
    private void DrawBody(FuDrawList drawList, Rect rect, bool compactLayout, float scale, TimelineWidgetTheme theme)
    {
        if (ShouldDrawConnectionFeedback())
        {
            DrawConnectionFeedback(drawList, rect, compactLayout, scale, theme);
            return;
        }

        float gap = (compactLayout ? 8f : 12f) * scale;
        float sectionHeight = (compactLayout ? 18f : 22f) * scale;
        float inputHeight = (compactLayout ? 50f : 58f) * scale;
        float statusHeight = (compactLayout ? 44f : 58f) * scale;
        float actionHeight = (compactLayout ? 40f : 46f) * scale;
        float keypadGap = (compactLayout ? 8f : 10f) * scale;

        Rect sectionRect = new Rect(rect.x, rect.y, rect.width, sectionHeight);
        Rect inputRect = new Rect(rect.x, sectionRect.yMax + 6f * scale, rect.width, inputHeight);
        Rect statusRect = new Rect(rect.x, inputRect.yMax + 5f * scale, rect.width, statusHeight);
        Rect actionRect = new Rect(rect.x, rect.yMax - actionHeight, rect.width, actionHeight);
        Rect keypadRect = new Rect(
            rect.x,
            statusRect.yMax + gap,
            rect.width,
            Mathf.Max(1f, actionRect.y - statusRect.yMax - gap - keypadGap));

        DrawSectionLabel(drawList, sectionRect, "S E S S I O N", theme, 1f);
        DrawCodeInput(drawList, inputRect, scale, theme);
        DrawStatus(drawList, statusRect, scale, theme);
        DrawKeypad(drawList, keypadRect, scale, theme);
        DrawSubmitButton(drawList, actionRect, scale, theme);
    }

    /// <summary>
    /// Draws the connection lifecycle feedback that replaces code entry while the server is unavailable.
    /// </summary>
    /// <param name="drawList">The active Fugui draw list.</param>
    /// <param name="rect">The available body rectangle.</param>
    /// <param name="compactLayout">Whether compact spacing is active.</param>
    /// <param name="scale">The current UI scale.</param>
    /// <param name="theme">The active timeline theme.</param>
    private void DrawConnectionFeedback(
        FuDrawList drawList,
        Rect rect,
        bool compactLayout,
        float scale,
        TimelineWidgetTheme theme)
    {
        Saravr.Network.NetworkManager network = Sara.Network;
        if (network == null)
            return;

        Rect sectionRect = new Rect(rect.x, rect.y, rect.width, (compactLayout ? 18f : 22f) * scale);
        DrawSectionLabel(drawList, sectionRect, "C O N N E C T I O N", theme, 1f);

        bool connecting = network.ConnectionStatus == SaraConnectionStatus.Connecting;
        Color stateColor = network.IsConnectionBlocked || network.ConnectionStatus == SaraConnectionStatus.Failed
            ? ErrorColor
            : connecting
                ? theme.Accent
                : WarningColor;
        Vector2 stateCenter = new Vector2(rect.center.x, rect.y + (compactLayout ? 76f : 96f) * scale);

        if (connecting)
        {
            DrawSpinner(drawList, stateCenter, 18f * scale, 4f * scale, scale, theme);
        }
        else
        {
            drawList.AddCircleFilled(stateCenter, 23f * scale, ColorU32(WithAlpha(stateColor, 0.16f)), 32);
            drawList.AddCircle(stateCenter, 23f * scale, ColorU32(WithAlpha(stateColor, 0.55f)), 32, Mathf.Max(1f, scale));
            Fugui.PushFont(20);
            Rect iconRect = new Rect(stateCenter.x - 18f * scale, stateCenter.y - 18f * scale, 36f * scale, 36f * scale);
            DrawIconCenteredTinted(drawList, iconRect, Icons.PlaneCircleExclamation_duotone, ColorU32(stateColor), ColorU32(stateColor), ColorU32(WithAlpha(stateColor, 0.70f)));
            Fugui.PopFont();
        }

        string message = GetConnectionFeedbackMessage(network);
        Rect messageRect = new Rect(
            rect.x + 16f * scale,
            stateCenter.y + 38f * scale,
            Mathf.Max(1f, rect.width - 32f * scale),
            (compactLayout ? 58f : 76f) * scale);
        DrawWrappedTextCentered(drawList, messageRect, message, ColorU32(stateColor), 14, true, 3);

        if (network.ConnectionStatusExpiresUtc.HasValue)
        {
            string expiration = "Available again: " + network.ConnectionStatusExpiresUtc.Value.ToLocalTime().ToString("g");
            Rect expirationRect = new Rect(messageRect.x, messageRect.yMax + 8f * scale, messageRect.width, 24f * scale);
            PushFont(11, false);
            DrawTextCentered(drawList, expirationRect, expiration, ColorU32(theme.TextDim));
            PopFont(false);
        }

        if (network.CanRetryConnection && !network.IsConnectionBlocked)
        {
            float actionHeight = (compactLayout ? 40f : 46f) * scale;
            Rect actionRect = new Rect(rect.x, rect.yMax - actionHeight, rect.width, actionHeight);
            DrawRetryConnectionButton(drawList, actionRect, scale, theme);
        }
    }

    /// <summary>
    /// Draws the retry action for recoverable connection failures.
    /// </summary>
    /// <param name="drawList">The active Fugui draw list.</param>
    /// <param name="rect">The action rectangle.</param>
    /// <param name="scale">The current UI scale.</param>
    /// <param name="theme">The active timeline theme.</param>
    private void DrawRetryConnectionButton(FuDrawList drawList, Rect rect, float scale, TimelineWidgetTheme theme)
    {
        bool enabled = !NSClient.IsConnecting;
        bool clicked = DrawInvisibleHitBox(rect, "retryConnection", enabled, out bool hovered, out bool active);
        Color background = enabled
            ? active ? theme.AccentHi : hovered ? Color.Lerp(theme.Accent, theme.AccentHi, 0.35f) : theme.Accent
            : theme.PillBackground;
        Color text = enabled ? theme.TextInk : theme.TextFaint;

        FlatCameraInputBlocker.RegisterRect(rect);
        drawList.AddRectFilled(rect.min, rect.max, ColorU32(background, enabled ? 1f : 0.66f), rect.height * 0.5f);
        drawList.AddRect(rect.min, rect.max, ColorU32(enabled ? theme.AccentGlow : theme.DockBorder, enabled ? 0.70f : 0.45f), rect.height * 0.5f);

        PushFont(12, true);
        DrawTextCentered(drawList, rect, "Retry connection", ColorU32(text));
        PopFont(true);

        if (hovered)
            Fugui.SetMouseCursor(FuMouseCursor.Hand);

        if (clicked)
            Sara.Network?.RetryConnection();
    }

    /// <summary>
    /// Returns whether connection feedback must replace the code-entry controls.
    /// </summary>
    /// <returns>True while connecting or after any actionable connection failure.</returns>
    private static bool ShouldDrawConnectionFeedback()
    {
        if (Sara.Network == null)
            return false;

        return Sara.Network.ConnectionStatus == SaraConnectionStatus.Connecting
            || Sara.Network.ConnectionStatus == SaraConnectionStatus.Rejected
            || Sara.Network.ConnectionStatus == SaraConnectionStatus.Failed
            || (Sara.Network.ConnectionStatus == SaraConnectionStatus.Disconnected
                && (Sara.Network.CanRetryConnection || !string.IsNullOrWhiteSpace(Sara.Network.ConnectionStatusMessage)));
    }

    /// <summary>
    /// Returns the best user-facing message for the current connection state.
    /// </summary>
    /// <param name="network">The application network manager.</param>
    /// <returns>A non-empty connection feedback message.</returns>
    private static string GetConnectionFeedbackMessage(Saravr.Network.NetworkManager network)
    {
        if (!string.IsNullOrWhiteSpace(network.ConnectionStatusMessage))
            return network.ConnectionStatusMessage;

        if (network.ConnectionStatus == SaraConnectionStatus.Connecting)
            return "Connecting to server...";

        return "Not connected to the server.";
    }

    /// <summary>
    /// Draws the header icon UI.
    /// </summary>
    private void DrawHeaderIcon(FuDrawList drawList, Rect rect, TimelineWidgetTheme theme)
    {
        float scale = Fugui.Scale;
        drawList.AddCircleFilled(rect.center, rect.width * 0.5f, ColorU32(theme.PillBackgroundActive), 32);
        drawList.AddCircle(rect.center, rect.width * 0.5f, ColorU32(theme.DockBorder), 32, Mathf.Max(1f, scale));

        Fugui.PushFont(20);
        DrawIconCenteredTinted(drawList, rect, Icons.PlaneLock_duotone, ColorU32(theme.Accent), ColorU32(theme.Accent), ColorU32(WithAlpha(theme.AccentHi, 0.72f)));
        Fugui.PopFont();
    }

    /// <summary>
    /// Draws the connection pill UI.
    /// </summary>
    private void DrawConnectionPill(FuDrawList drawList, Rect rect, float scale, TimelineWidgetTheme theme)
    {
        SaraConnectionStatus status = Sara.Network != null
            ? Sara.Network.ConnectionStatus
            : SaraConnectionStatus.Disconnected;
        bool connected = status == SaraConnectionStatus.Connected && NSClient.IsConnected;
        string label = connected
            ? "ONLINE"
            : status == SaraConnectionStatus.Connecting
                ? "CONNECTING"
                : Sara.Network != null && Sara.Network.IsConnectionBlocked
                    ? "BLOCKED"
                    : "OFFLINE";
        Color color = connected
            ? SuccessColor
            : status == SaraConnectionStatus.Connecting
                ? theme.Accent
                : Sara.Network != null && Sara.Network.IsConnectionBlocked
                    ? ErrorColor
                    : WarningColor;

        drawList.AddRectFilled(rect.min, rect.max, ColorU32(WithAlpha(color, connected ? 0.18f : 0.14f)), rect.height * 0.5f);
        drawList.AddRect(rect.min, rect.max, ColorU32(WithAlpha(color, 0.42f)), rect.height * 0.5f);

        PushFont(10, true);
        DrawTextCentered(drawList, rect, label, ColorU32(color));
        PopFont(true);
    }

    /// <summary>
    /// Draws the code input UI.
    /// </summary>
    private void DrawCodeInput(FuDrawList drawList, Rect rect, float scale, TimelineWidgetTheme theme)
    {
        FlatCameraInputBlocker.RegisterRect(rect);
        bool clicked = DrawInvisibleHitBox(rect, "codeInputFocus", true, out bool hovered, out bool active);
        if (clicked)
            _keyboardFocused = true;

        bool focused = _keyboardFocused || active;
        float rounding = theme.MediumRadius * scale;
        Color fillColor = hovered || focused ? theme.PillBackground : theme.SettingsDropdownBackground;

        drawList.AddRectFilled(rect.min, rect.max, ColorU32(fillColor), rounding);
        DrawCodeSlots(drawList, rect, scale, theme);
        Color borderColor = _hasError ? ErrorColor : focused ? theme.Accent : theme.DockBorder;
        drawList.AddRect(rect.min, rect.max, ColorU32(borderColor, focused || _hasError ? 0.85f : 1f), rounding, FuDrawFlags.None, Mathf.Max(1f, scale));

        if (hovered)
            Fugui.SetMouseCursor(FuMouseCursor.TextInput);
    }

    /// <summary>
    /// Draws the code slots UI.
    /// </summary>
    private void DrawCodeSlots(FuDrawList drawList, Rect rect, float scale, TimelineWidgetTheme theme)
    {
        float innerPadding = 10f * scale;
        float slotGap = 6f * scale;
        float availableWidth = rect.width - innerPadding * 2f - slotGap * (MaxCodeLength - 1);
        float slotWidth = Mathf.Max(18f * scale, availableWidth / MaxCodeLength);
        float slotHeight = Mathf.Min(36f * scale, rect.height - 18f * scale);
        float startX = rect.x + (rect.width - (slotWidth * MaxCodeLength + slotGap * (MaxCodeLength - 1))) * 0.5f;
        float y = rect.y + (rect.height - slotHeight) * 0.5f;

        for (int i = 0; i < MaxCodeLength; i++)
        {
            Rect slotRect = new Rect(startX + i * (slotWidth + slotGap), y, slotWidth, slotHeight);
            bool filled = i < _code.Length;
            Color fill = filled ? theme.PillBackgroundActive : theme.PillBackground;
            Color border = filled ? theme.AccentGlow : theme.DockBorder;

            drawList.AddRectFilled(slotRect.min, slotRect.max, ColorU32(fill), theme.SmallRadius * scale);
            drawList.AddRect(slotRect.min, slotRect.max, ColorU32(border, filled ? 0.65f : 0.60f), theme.SmallRadius * scale);

            if (filled)
            {
                PushFont(18, true);
                DrawTextCentered(drawList, slotRect, _code[i].ToString(), ColorU32(theme.Text));
                PopFont(true);
            }
            else
            {
                Vector2 center = slotRect.center;
                drawList.AddCircleFilled(center, 2.3f * scale, ColorU32(theme.TextFaint, 0.55f), 12);
            }
        }
    }

    /// <summary>
    /// Draws the status UI.
    /// </summary>
    private void DrawStatus(FuDrawList drawList, Rect rect, float scale, TimelineWidgetTheme theme)
    {
        Color color = _hasError ? ErrorColor : _sending ? theme.Accent : theme.TextDim;
        string message = _sending ? "Sending code..." : _statusMessage;
        Rect iconRect = new Rect(rect.x, rect.y, 24f * scale, rect.height);
        Rect textRect = new Rect(iconRect.xMax + 7f * scale, rect.y, Mathf.Max(1f, rect.width - iconRect.width - 7f * scale), rect.height);

        Fugui.PushFont(14);
        string icon = _hasError ? Icons.PlaneCircleXMark_duotone : _sending ? Icons.LocationArrowCircle_duotone : Icons.PlaneCircleExclamation_duotone;
        DrawIconCenteredTinted(drawList, iconRect, icon, ColorU32(color), ColorU32(color), ColorU32(WithAlpha(color, 0.70f)));
        Fugui.PopFont();

        DrawWrappedTextCentered(drawList, textRect, message, ColorU32(color), 12, true, 3);
    }

    /// <summary>
    /// Draws the keypad UI.
    /// </summary>
    private void DrawKeypad(FuDrawList drawList, Rect rect, float scale, TimelineWidgetTheme theme)
    {
        if (rect.height <= 0f)
            return;

        string[] labels =
        {
            "1", "2", "3",
            "4", "5", "6",
            "7", "8", "9",
            "CLR", "0", "DEL"
        };

        const int columns = 3;
        const int rows = 4;
        float gap = Mathf.Clamp(9f * scale, 5f, 12f * scale);
        float keyWidth = (rect.width - gap * (columns - 1)) / columns;
        float keyHeight = (rect.height - gap * (rows - 1)) / rows;
        keyHeight = Mathf.Max(30f * scale, keyHeight);

        for (int i = 0; i < labels.Length; i++)
        {
            int row = i / columns;
            int column = i % columns;
            Rect keyRect = new Rect(rect.x + column * (keyWidth + gap), rect.y + row * (keyHeight + gap), keyWidth, keyHeight);
            string label = labels[i];

            if (DrawKeyButton(drawList, keyRect, label, i, scale, theme))
                HandleKey(label);
        }
    }

    /// <summary>
    /// Draws the key button UI.
    /// </summary>
    private bool DrawKeyButton(FuDrawList drawList, Rect rect, string label, int index, float scale, TimelineWidgetTheme theme)
    {
        bool enabled = !_sending && (label != "DEL" || _code.Length > 0) && (label != "CLR" || _code.Length > 0);
        bool clicked = DrawInvisibleHitBox(rect, "key" + index, enabled, out bool hovered, out bool active);
        Color background = active ? theme.PillBackgroundActive : hovered ? theme.PillBackgroundHover : theme.PillBackground;
        Color textColor = enabled ? theme.Text : theme.TextFaint;

        FlatCameraInputBlocker.RegisterRect(rect);
        drawList.AddRectFilled(rect.min, rect.max, ColorU32(background, enabled ? 1f : 0.42f), theme.SmallRadius * scale);
        drawList.AddRect(rect.min, rect.max, ColorU32(theme.DockBorder, enabled ? 0.82f : 0.38f), theme.SmallRadius * scale);

        if (label == "DEL")
        {
            DrawBackspaceIcon(drawList, rect, textColor, enabled ? 1f : 0.48f);
        }
        else
        {
            PushFont(label.Length == 1 ? 18 : 10, true);
            DrawTextCentered(drawList, rect, label, ColorU32(textColor, enabled ? 1f : 0.48f));
            PopFont(true);
        }

        if (hovered)
            Fugui.SetMouseCursor(FuMouseCursor.Hand);

        return clicked;
    }

    /// <summary>
    /// Draws the submit button UI.
    /// </summary>
    private void DrawSubmitButton(FuDrawList drawList, Rect rect, float scale, TimelineWidgetTheme theme)
    {
        bool canSubmit = !_sending && _code.Length == MaxCodeLength;
        bool clicked = DrawInvisibleHitBox(rect, "submitCode", canSubmit, out bool hovered, out bool active);
        Color bg = canSubmit
            ? active ? theme.AccentHi : hovered ? Color.Lerp(theme.Accent, theme.AccentHi, 0.35f) : theme.Accent
            : theme.PillBackground;
        Color text = canSubmit ? theme.TextInk : theme.TextFaint;

        FlatCameraInputBlocker.RegisterRect(rect);
        drawList.AddRectFilled(rect.min, rect.max, ColorU32(bg, canSubmit ? 1f : 0.66f), rect.height * 0.5f);
        drawList.AddRect(rect.min, rect.max, ColorU32(canSubmit ? theme.AccentGlow : theme.DockBorder, canSubmit ? 0.70f : 0.45f), rect.height * 0.5f);

        string label = _sending ? "Sending code" : "Retrieve flight";
        float iconSize = 18f * scale;

        PushFont(12, true);
        Vector2 labelSize = Fugui.CalcTextSize(label);
        PopFont(true);

        float totalWidth = iconSize + 8f * scale + labelSize.x;
        Rect iconRect = new Rect(rect.x + (rect.width - totalWidth) * 0.5f, rect.y, iconSize, rect.height);
        Rect labelRect = new Rect(iconRect.xMax + 8f * scale, rect.y, labelSize.x + 2f * scale, rect.height);

        if (_sending)
            DrawSpinner(drawList, iconRect.center, iconSize * 0.42f, 2.6f * scale, scale, theme);
        else
        {
            Fugui.PushFont(14);
            DrawIconCenteredTinted(drawList, iconRect, Icons.LocationArrow_duotone, ColorU32(text), ColorU32(text), ColorU32(WithAlpha(text, 0.68f)));
            Fugui.PopFont();
        }

        PushFont(12, true);
        DrawTextLeft(drawList, labelRect, label, ColorU32(text));
        PopFont(true);

        if (hovered)
            Fugui.SetMouseCursor(FuMouseCursor.Hand);

        if (clicked)
            SubmitCode();
    }

    #endregion

    #region Input And Submission
    /// <summary>
    /// Handles the keyboard input flow.
    /// </summary>
    private void HandleKeyboardInput()
    {
        if (_lastKeyboardFrame == Time.frameCount || _sending || ShouldDrawConnectionFeedback())
            return;

        _lastKeyboardFrame = Time.frameCount;
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (IsPasteShortcutPressed(keyboard))
        {
            ReplaceCodeFromClipboard();
            return;
        }

        for (int i = 0; i <= 9; i++)
        {
            if (WasDigitPressed(keyboard, i))
            {
                HandleKey(i.ToString());
                _keyboardFocused = true;
            }
        }

        if (keyboard.backspaceKey.wasPressedThisFrame || keyboard.deleteKey.wasPressedThisFrame)
        {
            HandleKey("DEL");
            _keyboardFocused = true;
        }

        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
        {
            _keyboardFocused = true;
            SubmitCode();
        }
    }

    /// <summary>
    /// Returns whether the keyboard paste shortcut was pressed during the current frame.
    /// </summary>
    private static bool IsPasteShortcutPressed(Keyboard keyboard)
    {
        // Accept either Ctrl key so the shortcut behaves consistently with the active keyboard layout.
        bool controlPressed = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
        return controlPressed && keyboard.vKey.wasPressedThisFrame;
    }

    /// <summary>
    /// Replaces the current flight code with the sanitized clipboard content.
    /// </summary>
    private void ReplaceCodeFromClipboard()
    {
        // Keep only the supported digits and enforce the same length limit as manual input.
        _code = SanitizeCode(GUIUtility.systemCopyBuffer);
        _keyboardFocused = true;
        ClearError();
    }


    /// <summary>
    /// Runs the was digit pressed logic.
    /// </summary>
    private static bool WasDigitPressed(Keyboard keyboard, int digit)
    {
        switch (digit)
        {
            case 0:
                return keyboard[Key.Digit0].wasPressedThisFrame || keyboard[Key.Numpad0].wasPressedThisFrame;
            case 1:
                return keyboard[Key.Digit1].wasPressedThisFrame || keyboard[Key.Numpad1].wasPressedThisFrame;
            case 2:
                return keyboard[Key.Digit2].wasPressedThisFrame || keyboard[Key.Numpad2].wasPressedThisFrame;
            case 3:
                return keyboard[Key.Digit3].wasPressedThisFrame || keyboard[Key.Numpad3].wasPressedThisFrame;
            case 4:
                return keyboard[Key.Digit4].wasPressedThisFrame || keyboard[Key.Numpad4].wasPressedThisFrame;
            case 5:
                return keyboard[Key.Digit5].wasPressedThisFrame || keyboard[Key.Numpad5].wasPressedThisFrame;
            case 6:
                return keyboard[Key.Digit6].wasPressedThisFrame || keyboard[Key.Numpad6].wasPressedThisFrame;
            case 7:
                return keyboard[Key.Digit7].wasPressedThisFrame || keyboard[Key.Numpad7].wasPressedThisFrame;
            case 8:
                return keyboard[Key.Digit8].wasPressedThisFrame || keyboard[Key.Numpad8].wasPressedThisFrame;
            case 9:
                return keyboard[Key.Digit9].wasPressedThisFrame || keyboard[Key.Numpad9].wasPressedThisFrame;
            default:
                return false;
        }
    }

    /// <summary>
    /// Handles the key flow.
    /// </summary>
    private void HandleKey(string label)
    {
        if (_sending)
            return;

        if (label == "CLR")
        {
            _code = string.Empty;
            ClearError();
            return;
        }

        if (label == "DEL")
        {
            if (_code.Length > 0)
                _code = _code.Substring(0, _code.Length - 1);

            ClearError();
            return;
        }

        if (_code.Length >= MaxCodeLength)
            return;

        _code += label;
        ClearError();
    }

    /// <summary>
    /// Draws the invisible hit box UI.
    /// </summary>
    private static bool DrawInvisibleHitBox(Rect rect, string id, bool enabled, out bool hovered, out bool active)
    {
        FuLayout layout = FuWindow.CurrentDrawingWindow?.Layout;
        if (layout == null)
        {
            hovered = false;
            active = false;
            return false;
        }

        // Register the absolute custom-drawn rectangle in Fugui's interaction system.
        return layout.InvisibleInteractionAt("##" + id, rect.min, rect.size, out hovered, out active, FuButtonFlags.MouseButtonLeft, enabled);
    }

    /// <summary>
    /// Runs the submit code logic.
    /// </summary>
    private void SubmitCode()
    {
        if (_sending)
            return;

        _code = SanitizeCode(_code);
        if (_code.Length != MaxCodeLength)
        {
            SetError("Enter a 6-digit code.");
            return;
        }

        if (Sara.Network == null)
        {
            SetError("Network client is not initialized.");
            return;
        }

        if (!Sara.Network.ServerVersionVerified)
        {
            SetError("Checking server version...");
            return;
        }

        if (!Sara.Network.IsServerVersionCompatible)
        {
            SetError(string.IsNullOrWhiteSpace(Sara.Network.ServerVersionMessage) ? "Please update the application." : Sara.Network.ServerVersionMessage);
            return;
        }

        _sending = true;
        _hasError = false;
        _statusMessage = "Sending code...";

        int submissionGeneration = ++_submissionGeneration;
        Sara.Network.GetSession(_code, (response) => HandleCodeResponse(response, submissionGeneration));
    }

    /// <summary>
    /// Handles the code response flow.
    /// </summary>
    /// <param name="response">The structured session-access response.</param>
    /// <param name="submissionGeneration">The generation that issued this request.</param>
    private void HandleCodeResponse(SessionAccessResponse response, int submissionGeneration)
    {
        if (submissionGeneration != _submissionGeneration)
            return;

        _sending = false;

        if (response == null)
        {
            SetError("No response from server.");
            return;
        }

        if (!response.Success)
        {
            SetError(BuildCodeFailureMessage(response));
            return;
        }

        SaraSession session = response.Data;
        if (session == null || string.IsNullOrWhiteSpace(session.HeaderSas) || string.IsNullOrWhiteSpace(session.DataSas))
        {
            SetError("Server returned incomplete flight data.");
            return;
        }

        if (Sara.Loader == null)
        {
            SetError("Flight loader is not available.");
            return;
        }

        _hasError = false;
        if (session.IsMultiplayer)
        {
            _statusMessage = "Code accepted. Confirm display name...";
            RemoteNamePrompt.Request(session);
            return;
        }

        _statusMessage = "Code accepted. Downloading flight...";
        Sara.Loader.LoadFlight(session.HeaderSas, session.DataSas);
    }

    /// <summary>
    /// Builds the code-rejection message from structured server counters.
    /// </summary>
    /// <param name="response">The rejected session-access response.</param>
    /// <returns>A concise user-facing rejection and warning message.</returns>
    private static string BuildCodeFailureMessage(SessionAccessResponse response)
    {
        string message = string.IsNullOrWhiteSpace(response.Message)
            ? "Code rejected by server."
            : response.Message;
        CodeAttemptFeedback feedback = response.AttemptFeedback;
        if (feedback == null || feedback.ConnectionFailureThreshold <= 0)
            return message;

        int remainingConnectionAttempts = feedback.RemainingConnectionAttempts;
        if (remainingConnectionAttempts > 0)
        {
            message += remainingConnectionAttempts == 1
                ? " One attempt remains before disconnection."
                : " " + remainingConnectionAttempts + " attempts remain before disconnection.";
        }

        int remainingBanAttempts = feedback.RemainingBanAttempts;
        if (remainingBanAttempts > 0 && remainingBanAttempts <= 2)
        {
            message += remainingBanAttempts == 1
                ? " One more invalid attempt will permanently ban this network."
                : " " + remainingBanAttempts + " more invalid attempts will permanently ban this network.";
        }

        return message;
    }

    /// <summary>
    /// Sets the error value.
    /// </summary>
    private void SetError(string message)
    {
        _hasError = true;
        _statusMessage = string.IsNullOrWhiteSpace(message) ? "Unable to retrieve flight." : message;
    }


    /// <summary>
    /// Clears the error state.
    /// </summary>
    private void ClearError()
    {
        if (!_hasError)
            return;

        _hasError = false;
        _statusMessage = "Waiting for flight code";
    }

    /// <summary>
    /// Synchronizes the visible status with the server version check state.
    /// </summary>
    private void SynchronizeServerVersionStatus()
    {
        if (Sara.Network == null)
            return;

        if (Sara.Network.ConnectionStatus != SaraConnectionStatus.Connected)
        {
            if (_sending)
            {
                // Invalidate callbacks that may arrive after the connection was terminated.
                _sending = false;
                _submissionGeneration++;
            }

            if (!string.IsNullOrWhiteSpace(Sara.Network.ConnectionStatusMessage))
            {
                _hasError = Sara.Network.ConnectionStatus == SaraConnectionStatus.Rejected
                    || Sara.Network.ConnectionStatus == SaraConnectionStatus.Failed;
                _statusMessage = Sara.Network.ConnectionStatusMessage;
            }

            return;
        }

        if (_sending)
            return;

        if (Sara.Network.HasServerVersionMismatch)
        {
            _hasError = true;
            _statusMessage = string.IsNullOrWhiteSpace(Sara.Network.ServerVersionMessage)
                ? "Please update the application."
                : Sara.Network.ServerVersionMessage;
            return;
        }

        if (NSClient.IsConnected && !Sara.Network.ServerVersionVerified)
        {
            _hasError = false;
            _statusMessage = "Checking server version...";
            return;
        }

        if (!_hasError && Sara.Network.ServerVersionVerified && Sara.Network.IsServerVersionCompatible && _statusMessage == "Checking server version...")
            _statusMessage = "Waiting for flight code";
    }

    #endregion

    #region Drawing Utilities
    /// <summary>
    /// Runs the sanitize code logic.
    /// </summary>
    private static string SanitizeCode(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        char[] digits = new char[Mathf.Min(value.Length, MaxCodeLength)];
        int count = 0;
        for (int i = 0; i < value.Length && count < MaxCodeLength; i++)
        {
            if (value[i] >= '0' && value[i] <= '9')
                digits[count++] = value[i];
        }

        return count > 0 ? new string(digits, 0, count) : string.Empty;
    }


    /// <summary>
    /// Draws the section label UI.
    /// </summary>
    private static void DrawSectionLabel(FuDrawList drawList, Rect rect, string label, TimelineWidgetTheme theme, float alpha)
    {
        PushFont(10, true);
        DrawTextLeftCentered(drawList, rect, label, ColorU32(theme.TextFaint, alpha), 0f);
        PopFont(true);
    }

    /// <summary>
    /// Draws the spinner UI.
    /// </summary>
    private static void DrawSpinner(FuDrawList drawList, Vector2 center, float radius, float thickness, float scale, TimelineWidgetTheme theme)
    {
        float t = Time.unscaledTime * 4.8f;
        float start = t;
        float end = t + Mathf.PI * 1.45f;

        drawList.AddCircleFilled(center, radius + 6f * scale, ColorU32(theme.AccentGlow, 0.22f), 40);
        drawList.AddCircle(center, radius, ColorU32(theme.Track), 40, thickness);
        drawList.PathArcTo(center, radius, start, end, 28);
        drawList.PathStroke(ColorU32(theme.Accent), FuDrawFlags.None, thickness);
        drawList.AddCircleFilled(center, Mathf.Max(2f * scale, thickness * 0.55f), ColorU32(theme.Text), 16);
    }

    /// <summary>
    /// Draws the backspace icon UI.
    /// </summary>
    private static void DrawBackspaceIcon(FuDrawList drawList, Rect rect, Color color, float alpha)
    {
        float scale = Fugui.Scale;
        Vector2 center = rect.center;
        float width = 22f * scale;
        float height = 15f * scale;
        float left = center.x - width * 0.5f;
        float right = center.x + width * 0.5f;
        float top = center.y - height * 0.5f;
        float bottom = center.y + height * 0.5f;
        float notch = 7f * scale;
        uint col = ColorU32(color, alpha);
        float thickness = Mathf.Max(1.8f * scale, 1f);

        drawList.AddLine(new Vector2(left + notch, top), new Vector2(right, top), col, thickness);
        drawList.AddLine(new Vector2(right, top), new Vector2(right, bottom), col, thickness);
        drawList.AddLine(new Vector2(right, bottom), new Vector2(left + notch, bottom), col, thickness);
        drawList.AddLine(new Vector2(left + notch, bottom), new Vector2(left, center.y), col, thickness);
        drawList.AddLine(new Vector2(left, center.y), new Vector2(left + notch, top), col, thickness);
        drawList.AddLine(center + new Vector2(1f * scale, -4f * scale), center + new Vector2(8f * scale, 4f * scale), col, thickness);
        drawList.AddLine(center + new Vector2(8f * scale, -4f * scale), center + new Vector2(1f * scale, 4f * scale), col, thickness);
    }

    /// <summary>
    /// Runs the inset rect logic.
    /// </summary>
    private static Rect InsetRect(Rect rect, float inset)
    {
        return new Rect(
            rect.x + inset,
            rect.y + inset,
            Mathf.Max(1f, rect.width - inset * 2f),
            Mathf.Max(1f, rect.height - inset * 2f));
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
    /// Runs the push font logic.
    /// </summary>
    private static void PushFont(int size, bool bold)
    {
        Fugui.PushFont(size, bold ? FontType.Bold : FontType.Regular);

    }

    /// <summary>
    /// Runs the pop font logic.
    /// </summary>
    private static void PopFont(bool bold)
    {
        Fugui.PopFont();
    }


    /// <summary>
    /// Draws the text left UI.
    /// </summary>
    private static void DrawTextLeft(FuDrawList drawList, Rect rect, string text, uint color)
    {
        Vector2 textSize = Fugui.CalcTextSize(text);
        Vector2 textPos = new Vector2(rect.x, rect.y + (rect.height - textSize.y) * 0.5f);
        drawList.AddText(textPos, color, text);
    }

    /// <summary>
    /// Draws the text left centered UI.
    /// </summary>
    private static void DrawTextLeftCentered(FuDrawList drawList, Rect rect, string text, uint color, float padding)
    {
        string clippedText = ClipTextToWidth(text, Mathf.Max(1f, rect.width - padding * 2f));
        Vector2 textSize = Fugui.CalcTextSize(clippedText);
        Vector2 textPos = new Vector2(rect.x + padding, rect.y + (rect.height - textSize.y) * 0.5f);
        drawList.AddText(textPos, color, clippedText);
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
    /// Draws a bounded number of centered text lines with word wrapping.
    /// </summary>
    /// <param name="drawList">The active Fugui draw list.</param>
    /// <param name="rect">The available text rectangle.</param>
    /// <param name="text">The text to wrap and draw.</param>
    /// <param name="color">The packed text color.</param>
    /// <param name="fontSize">The Fugui font size.</param>
    /// <param name="bold">Whether to use the bold font.</param>
    /// <param name="maxLines">The maximum number of visible lines.</param>
    private static void DrawWrappedTextCentered(
        FuDrawList drawList,
        Rect rect,
        string text,
        uint color,
        int fontSize,
        bool bold,
        int maxLines)
    {
        PushFont(fontSize, bold);
        List<string> lines = WrapText(text, rect.width, maxLines);
        float lineHeight = Fugui.GetTextLineHeight();
        float totalHeight = lines.Count * lineHeight;
        float y = rect.y + (rect.height - totalHeight) * 0.5f;

        for (int i = 0; i < lines.Count; i++)
        {
            Rect lineRect = new Rect(rect.x, y + i * lineHeight, rect.width, lineHeight);
            DrawTextCentered(drawList, lineRect, lines[i], color);
        }

        PopFont(bold);
    }

    /// <summary>
    /// Wraps text to a width and truncates it to a bounded number of lines.
    /// </summary>
    /// <param name="text">The text to wrap.</param>
    /// <param name="maxWidth">The maximum width of one line.</param>
    /// <param name="maxLines">The maximum number of returned lines.</param>
    /// <returns>The wrapped lines ready for drawing.</returns>
    private static List<string> WrapText(string text, float maxWidth, int maxLines)
    {
        List<string> lines = new List<string>();
        if (string.IsNullOrWhiteSpace(text) || maxLines <= 0)
            return lines;

        string[] words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        string currentLine = string.Empty;
        for (int i = 0; i < words.Length; i++)
        {
            string candidate = string.IsNullOrEmpty(currentLine)
                ? words[i]
                : currentLine + " " + words[i];
            if (Fugui.CalcTextSize(candidate).x <= maxWidth)
            {
                currentLine = candidate;
                continue;
            }

            if (!string.IsNullOrEmpty(currentLine))
                lines.Add(currentLine);

            currentLine = Fugui.CalcTextSize(words[i]).x <= maxWidth
                ? words[i]
                : ClipTextToWidth(words[i], maxWidth);
        }

        if (!string.IsNullOrEmpty(currentLine))
            lines.Add(currentLine);

        if (lines.Count > maxLines)
        {
            lines.RemoveRange(maxLines, lines.Count - maxLines);
            lines[maxLines - 1] = ClipTextToWidth(lines[maxLines - 1] + "...", maxWidth);
        }

        return lines;
    }

    /// <summary>
    /// Draws the icon centered tinted UI.
    /// </summary>
    private static void DrawIconCenteredTinted(FuDrawList drawList, Rect rect, string icon, uint solidColor, uint duotonePrimaryColor, uint duotoneSecondaryColor)
    {
        if (string.IsNullOrEmpty(icon))
            return;

        Vector2 iconSize = Fugui.CalcTextSize(icon, FuTextWrapping.None);
        Vector2 iconPos = new Vector2(
            rect.x + (rect.width - iconSize.x) * 0.5f,
            rect.y + (rect.height - iconSize.y) * 0.5f);
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
    #endregion
}
