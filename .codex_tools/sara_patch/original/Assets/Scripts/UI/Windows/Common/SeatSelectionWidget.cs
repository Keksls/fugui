using Assets.Scripts.UI.Windows.Common;
using Fu;
using Fu.Framework;

using Saravr.Network.Common;
using System;
using UnityEngine;

/// <summary>
/// Implements the seat selection widget logic.
/// </summary>
public class SeatSelectionWidget
{
    private const float DesktopPanelMaxWidth = 620f;
    private const float MobilePanelMaxWidth = 540f;
    private const float CompactHeightThreshold = 430f;

    private TimelineWidgetTheme _theme;
    private SaraSession _session;
    private Action _requestRedraw;
    private Action _seatSelected;
    private bool _joining;
    private string _statusMessage = "Select your cockpit seat.";

    public TimelineWidgetTheme Theme
    {
        get { return _theme != null ? _theme : TimelineWidgetTheme.LoadDefault(); }
        set { _theme = value; }
    }

    /// <summary>
    /// Runs the bind logic.
    /// </summary>
    public void Bind(Action requestRedraw, Action seatSelected)
    {
        _requestRedraw = requestRedraw;
        _seatSelected = seatSelected;
        SetSession(Sara.CurrentSession);
    }

    /// <summary>
    /// Runs the unbind logic.
    /// </summary>
    public void Unbind()
    {
        _requestRedraw = null;
        _seatSelected = null;
    }

    /// <summary>
    /// Sets the theme value.
    /// </summary>
    public void SetTheme(TimelineWidgetTheme theme)
    {
        _theme = theme;
    }

    /// <summary>
    /// Sets the session value.
    /// </summary>
    public void SetSession(SaraSession session)
    {
        _session = session;
        if (!_joining)
            _statusMessage = HasAvailableSeat(session) ? "Select your cockpit seat." : "No cockpit seat is currently available.";

        RequestRedraw();
    }

    /// <summary>
    /// Runs per-frame runtime updates.
    /// </summary>
    public void Update()
    {
        if (_joining)
            RequestRedraw();
    }


    /// <summary>
    /// Draws the seat panel UI.
    /// </summary>
    public Rect DrawSeatPanel(FuWindow window, bool drawBackground = false)
    {
        if (window == null || window.Container == null)
            return new Rect();

        Vector2 containerSize = new Vector2(window.Container.Size.x, window.Container.Size.y);
        return DrawSeatPanel(Fugui.GetCurrentWindowDrawList(), Fugui.GetWindowPos(), containerSize, drawBackground);
    }

    /// <summary>
    /// Draws the seat panel UI.
    /// </summary>
    public Rect DrawSeatPanel(FuDrawList drawList, Vector2 origin, Vector2 containerSize, bool drawBackground = false)
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
        DrawSeatPanel(drawList, panelRect, mobileLayout, compactLayout, scale);
        Fugui.PopFont();

        Fugui.SetCursorScreenPos(new Vector2(origin.x, origin.y + containerSize.y));
        return panelRect;
    }

    /// <summary>
    /// Draws the seat panel UI.
    /// </summary>
    public void DrawSeatPanel(FuDrawList drawList, Rect panelRect, bool mobileLayout, bool compactLayout, float scale)
    {
        TimelineWidgetTheme theme = Theme;
        float padding = (mobileLayout ? 18f : 22f) * scale;
        float rounding = (mobileLayout ? theme.MediumRadius : theme.DockRadius) * scale;

        if (!Sara.IsVR)
            FlatCameraInputBlocker.BlockAllForFrame();

        drawList.AddRectFilled(panelRect.min + new Vector2(0f, 6f * scale), panelRect.max + new Vector2(0f, 8f * scale), ColorU32(theme.DockShadow, 0.70f), rounding);
        drawList.AddRectFilled(panelRect.min, panelRect.max, ColorU32(theme.DockBackground), rounding);
        drawList.AddRect(panelRect.min, panelRect.max, ColorU32(theme.DockBorder), rounding);

        float headerHeight = (compactLayout ? 58f : 72f) * scale;
        Rect headerRect = new Rect(panelRect.x, panelRect.y, panelRect.width, headerHeight);
        Rect contentRect = new Rect(
            panelRect.x + padding,
            headerRect.yMax + (compactLayout ? 10f : 14f) * scale,
            Mathf.Max(1f, panelRect.width - padding * 2f),
            Mathf.Max(1f, panelRect.yMax - headerRect.yMax - padding - (compactLayout ? 10f : 14f) * scale));

        Fugui.PushClipRect(panelRect.min, panelRect.max, true);
        DrawHeader(drawList, headerRect, mobileLayout, compactLayout, scale, theme);
        DrawSeatButtons(drawList, contentRect, compactLayout, scale, theme);
        Fugui.PopClipRect();
    }

    /// <summary>
    /// Draws the header UI.
    /// </summary>
    private void DrawHeader(FuDrawList drawList, Rect rect, bool mobileLayout, bool compactLayout, float scale, TimelineWidgetTheme theme)
    {
        float headerPadding = (mobileLayout ? 18f : 22f) * scale;
        float iconSize = (compactLayout ? 34f : 40f) * scale;
        Rect iconRect = new Rect(rect.x + headerPadding, rect.y + (rect.height - iconSize) * 0.5f, iconSize, iconSize);
        Rect titleRect = new Rect(iconRect.xMax + 14f * scale, rect.y + 14f * scale, rect.xMax - iconRect.xMax - 28f * scale, 24f * scale);
        Rect subtitleRect = new Rect(titleRect.x, titleRect.yMax + 3f * scale, titleRect.width, 20f * scale);

        drawList.AddCircleFilled(iconRect.center, iconRect.width * 0.5f, ColorU32(theme.PillBackgroundActive), 32);
        drawList.AddCircle(iconRect.center, iconRect.width * 0.5f, ColorU32(theme.DockBorder), 32, Mathf.Max(1f, scale));

        Fugui.PushFont(Mathf.RoundToInt(20f * scale));
        DrawIconCenteredTinted(drawList, iconRect, Icons.Seat_duotone, ColorU32(theme.Accent), ColorU32(theme.Accent), ColorU32(WithAlpha(theme.AccentHi, 0.72f)));
        Fugui.PopFont();

        PushFont(17, true);
        DrawTextLeft(drawList, titleRect, ClipTextToWidth("Choose a cockpit seat", titleRect.width), ColorU32(theme.Text));
        PopFont(true);

        PushFont(12, false);
        DrawTextLeft(drawList, subtitleRect, ClipTextToWidth(_statusMessage, subtitleRect.width), ColorU32(_joining ? theme.Accent : theme.TextDim));
        PopFont(false);

        drawList.AddLine(new Vector2(rect.x, rect.yMax), new Vector2(rect.xMax, rect.yMax), ColorU32(theme.DockBorder, 0.60f), Mathf.Max(1f, scale));
    }

    /// <summary>
    /// Draws the seat buttons UI.
    /// </summary>
    private void DrawSeatButtons(FuDrawList drawList, Rect rect, bool compactLayout, float scale, TimelineWidgetTheme theme)
    {
        float sectionHeight = (compactLayout ? 18f : 22f) * scale;
        float gap = 12f * scale;
        Rect sectionRect = new Rect(rect.x, rect.y, rect.width, sectionHeight);
        Rect seatsRect = new Rect(rect.x, sectionRect.yMax + 12f * scale, rect.width, Mathf.Max(1f, rect.height - sectionHeight - 12f * scale));
        float buttonWidth = (seatsRect.width - gap * 2f) / 3f;

        DrawSectionLabel(drawList, sectionRect, "S E A T S", theme, 1f);
        DrawSeatButton(drawList, new Rect(seatsRect.x, seatsRect.y, buttonWidth, seatsRect.height), SeatType.Pilot, "Left seat", scale, theme);
        DrawSeatButton(drawList, new Rect(seatsRect.x + buttonWidth + gap, seatsRect.y, buttonWidth, seatsRect.height), SeatType.Center, "Center", scale, theme);
        DrawSeatButton(drawList, new Rect(seatsRect.x + (buttonWidth + gap) * 2f, seatsRect.y, buttonWidth, seatsRect.height), SeatType.CoPilot, "Right seat", scale, theme);
    }

    /// <summary>
    /// Draws the seat button UI.
    /// </summary>
    private void DrawSeatButton(FuDrawList drawList, Rect rect, SeatType seat, string label, float scale, TimelineWidgetTheme theme)
    {
        bool available = IsSeatAvailable(seat);
        bool occupiedByMe = IsOccupiedByLocalClient(seat);
        bool enabled = (available || occupiedByMe) && !_joining;
        bool clicked = DrawInvisibleButton(rect, "seat" + seat, enabled, out bool hovered, out bool active);
        Color background = !available && !occupiedByMe ? theme.SettingsDropdownBackground : active ? theme.PillBackgroundActive : hovered ? theme.PillBackgroundHover : theme.PillBackground;
        Color border = occupiedByMe ? theme.Accent : available ? theme.AccentGlow : theme.DockBorder;
        Color text = available || occupiedByMe ? theme.Text : theme.TextFaint;

        FlatCameraInputBlocker.RegisterRect(rect);
        drawList.AddRectFilled(rect.min, rect.max, ColorU32(background, available || occupiedByMe ? 1f : 0.58f), theme.MediumRadius * scale);
        drawList.AddRect(rect.min, rect.max, ColorU32(border, available || occupiedByMe ? 0.78f : 0.42f), theme.MediumRadius * scale);

        Rect iconRect = new Rect(rect.x, rect.y + 22f * scale, rect.width, 34f * scale);
        Rect labelRect = new Rect(rect.x + 8f * scale, iconRect.yMax + 10f * scale, rect.width - 16f * scale, 24f * scale);
        Rect stateRect = new Rect(labelRect.x, labelRect.yMax + 4f * scale, labelRect.width, 20f * scale);

        Fugui.PushFont(20);
        DrawIconCenteredTinted(drawList, iconRect, Icons.Seat_duotone, ColorU32(available || occupiedByMe ? theme.Accent : theme.TextFaint), ColorU32(available || occupiedByMe ? theme.Accent : theme.TextFaint), ColorU32(theme.AccentHi, available || occupiedByMe ? 0.72f : 0.28f));
        Fugui.PopFont();

        PushFont(13, true);
        DrawTextCentered(drawList, labelRect, label, ColorU32(text));
        PopFont(true);

        PushFont(12, false);
        DrawTextCentered(drawList, stateRect, GetSeatStateLabel(seat, available, occupiedByMe), ColorU32(available || occupiedByMe ? theme.Accent : theme.TextFaint));
        PopFont(false);

        if (hovered)
            Fugui.SetMouseCursor(FuMouseCursor.Hand);

        if (clicked)
            SelectSeat(seat);
    }

    /// <summary>
    /// Selects the seat option.
    /// </summary>
    private void SelectSeat(SeatType seat)
    {
        if (_joining || Sara.Network == null)
            return;

        _joining = true;
        _statusMessage = "Joining cockpit seat...";
        RequestRedraw();

        Sara.Network.SelectSeat(seat, (response) =>
        {
            _joining = false;

            if (response == null || !response.Success)
            {
                _statusMessage = response == null || string.IsNullOrWhiteSpace(response.Message)
                    ? "Unable to join this seat."
                    : response.Message;
                RequestRedraw();
                return;
            }

            _statusMessage = "Seat confirmed.";
            RequestRedraw();
            _seatSelected?.Invoke();
        });
    }

    /// <summary>
    /// Returns whether the seat available condition is met.
    /// </summary>
    private bool IsSeatAvailable(SeatType seat)
    {
        return _session != null && _session.Seats != null && _session.Seats.IsAvailable(seat);
    }

    /// <summary>
    /// Returns whether the occupied by local client condition is met.
    /// </summary>
    private bool IsOccupiedByLocalClient(SeatType seat)
    {
        if (_session == null || _session.Seats == null || NSClient.ClientID == 0)
            return false;

        Seat sessionSeat = _session.Seats[seat];
        return sessionSeat != null && sessionSeat.OccupiedByClientID == NSClient.ClientID;
    }

    /// <summary>
    /// Returns the seat state label value.
    /// </summary>
    private string GetSeatStateLabel(SeatType seat, bool available, bool occupiedByMe)
    {
        if (occupiedByMe)
            return "Selected";

        return available ? "Available" : "Occupied";
    }

    /// <summary>
    /// Returns whether available seat exists.
    /// </summary>
    private static bool HasAvailableSeat(SaraSession session)
    {
        return session != null
            && session.Seats != null
            && (session.Seats.IsAvailable(SeatType.Pilot)
                || session.Seats.IsAvailable(SeatType.Center)
                || session.Seats.IsAvailable(SeatType.CoPilot));
    }


    /// <summary>
    /// Runs the request redraw logic.
    /// </summary>
    private void RequestRedraw()
    {
        _requestRedraw?.Invoke();
    }

    /// <summary>
    /// Returns the panel rect value.
    /// </summary>
    private static Rect GetPanelRect(Rect safeRect, bool mobileLayout, bool compactLayout, float scale)
    {
        float maxWidth = Mathf.Min(safeRect.width, (mobileLayout ? MobilePanelMaxWidth : DesktopPanelMaxWidth) * scale);
        float minDesktopWidth = Mathf.Min(440f * scale, maxWidth);
        float panelWidth = mobileLayout
            ? maxWidth
            : Mathf.Clamp(safeRect.width * 0.52f, minDesktopWidth, maxWidth);
        float desiredHeight = (compactLayout ? 270f : 330f) * scale;
        float minHeight = Mathf.Min(240f * scale, safeRect.height);
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
    private static Rect GetSafeContentRect(Vector2 origin, Vector2 containerSize, bool mobileLayout, float scale)
    {
        float margin = (mobileLayout ? 16f : 28f) * scale;
        return InsetRect(new Rect(origin.x, origin.y, containerSize.x, containerSize.y), margin);
    }

    /// <summary>
    /// Returns whether the compact layout condition is met.
    /// </summary>
    private static bool IsCompactLayout(Vector2 containerSize, float scale)
    {
        return containerSize.y < CompactHeightThreshold * scale;
    }

    /// <summary>
    /// Returns whether the mobile layout condition is met.
    /// </summary>
    private static bool IsMobileLayout(Vector2 containerSize, float scale)
    {
        return !Sara.IsVR &&
            (Application.isMobilePlatform ||
             Application.platform == RuntimePlatform.Android ||
             Application.platform == RuntimePlatform.IPhonePlayer ||
             containerSize.x <= 720f * scale ||
             containerSize.y > containerSize.x * 1.08f);
    }

    /// <summary>
    /// Draws the background UI.
    /// </summary>
    private static void DrawBackground(FuDrawList drawList, Rect rect, float scale, TimelineWidgetTheme theme)
    {
        uint bgColor = ColorU32(WithAlpha(theme.SettingsPanelBackground, 0.96f));
        uint topBandColor = ColorU32(WithAlpha(theme.DockBackground, 0.78f));
        uint accentColor = ColorU32(WithAlpha(theme.AccentGlow, 0.18f));
        uint lineColor = ColorU32(WithAlpha(theme.DockBorder, 0.72f));
        float bandHeight = Mathf.Min(rect.height * 0.34f, 240f * scale);

        drawList.AddRectFilled(rect.min, rect.max, bgColor);
        drawList.AddRectFilled(rect.min, new Vector2(rect.xMax, rect.y + bandHeight), topBandColor);
        drawList.AddRectFilled(new Vector2(rect.x, rect.y + bandHeight - 2f * scale), new Vector2(rect.xMax, rect.y + bandHeight + 2f * scale), accentColor, 0f);
        drawList.AddLine(new Vector2(rect.x, rect.y + bandHeight), new Vector2(rect.xMax, rect.y + bandHeight), lineColor, Mathf.Max(1f, scale));
    }

    /// <summary>
    /// Draws the invisible button UI.
    /// </summary>
    private static bool DrawInvisibleButton(Rect rect, string id, bool enabled, out bool hovered, out bool active)
    {
        Fugui.SetCursorScreenPos(rect.min);
        Fugui.InvisibleButton("##" + id, rect.size);
        hovered = enabled && Fugui.IsItemHovered();
        active = enabled && Fugui.IsItemActive();
        return enabled && Fugui.IsItemClicked(ImGuiMouseButton.Left);
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
    /// Runs the inset rect logic.
    /// </summary>
    private static Rect InsetRect(Rect rect, float inset)
    {
        return new Rect(rect.x + inset, rect.y + inset, Mathf.Max(1f, rect.width - inset * 2f), Mathf.Max(1f, rect.height - inset * 2f));
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
        Vector2 textPos = new Vector2(rect.x + (rect.width - textSize.x) * 0.5f, rect.y + (rect.height - textSize.y) * 0.5f);
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
}
