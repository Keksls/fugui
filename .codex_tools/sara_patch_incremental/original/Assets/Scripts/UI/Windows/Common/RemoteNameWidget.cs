using Assets.Scripts.UI.Windows.Common;
using Fu;
using Fu.Framework;

using Saravr.Network.Common;
using UnityEngine;

/// <summary>
/// Implements the remote name widget logic.
/// </summary>
public class RemoteNameWidget
{
    public const int MaxNameLength = 32;
    private const float DesktopPanelMaxWidth = 560f;
    private const float MobilePanelMaxWidth = 520f;

    private static readonly Color ErrorColor = new Color(1f, 0.36f, 0.36f, 1f);
    private static readonly Color SuccessColor = new Color(0.42f, 0.83f, 0.55f, 1f);

    private TimelineWidgetTheme _theme;
    private SaraSession _session;
    private string _name = string.Empty;
    private string _statusMessage = "Confirm your display name";
    private bool _sending;
    private bool _hasError;
    private readonly FuTextInputOptions _nameInputOptions = new FuTextInputOptions();
    private System.Action _forceDraw;
    private System.Action _submitted;

    /// <summary>
    /// Initializes the high-level Fugui text input callbacks.
    /// </summary>
    public RemoteNameWidget()
    {
        _nameInputOptions.Submitted = _ => SubmitName();
    }

    public TimelineWidgetTheme Theme
    {
        get { return _theme != null ? _theme : TimelineWidgetTheme.LoadDefault(); }
        set { _theme = value; }
    }

    public bool CanEdit
    {
        get { return !_sending && _session != null; }
    }

    public bool CanSubmit
    {
        get { return !_sending && _session != null; }
    }

    /// <summary>
    /// Runs the bind logic.
    /// </summary>
    public void Bind(System.Action forceDraw, System.Action submitted)
    {
        _forceDraw = forceDraw;
        _submitted = submitted;
    }

    /// <summary>
    /// Runs the unbind logic.
    /// </summary>
    public void Unbind()
    {
        _forceDraw = null;
        _submitted = null;
    }

    /// <summary>
    /// Sets the session value.
    /// </summary>
    public void SetSession(SaraSession session)
    {
        _session = session != null ? new SaraSession(session) : null;
        _name = _session != null && _session.User != null ? _session.User.Name ?? string.Empty : string.Empty;
        _statusMessage = "Confirm your display name";
        _sending = false;
        _hasError = false;
        _nameInputOptions.RequestFocus = true;
    }

    /// <summary>
    /// Runs the insert text logic.
    /// </summary>
    public void InsertText(string text)
    {
        if (!CanEdit || string.IsNullOrEmpty(text) || _name.Length >= MaxNameLength)
            return;

        int remaining = MaxNameLength - _name.Length;
        _name += text.Length <= remaining ? text : text.Substring(0, remaining);
        ClearTransientError();
        _forceDraw?.Invoke();
    }

    /// <summary>
    /// Runs the backspace logic.
    /// </summary>
    public void Backspace()
    {
        if (!CanEdit || string.IsNullOrEmpty(_name))
            return;

        _name = _name.Substring(0, _name.Length - 1);
        ClearTransientError();
        _forceDraw?.Invoke();
    }

    /// <summary>
    /// Clears the name state.
    /// </summary>
    public void ClearName()
    {
        if (!CanEdit || string.IsNullOrEmpty(_name))
            return;

        _name = string.Empty;
        ClearTransientError();
        _forceDraw?.Invoke();
    }

    /// <summary>
    /// Runs the submit from keyboard logic.
    /// </summary>
    public void SubmitFromKeyboard()
    {
        SubmitName();
    }


    /// <summary>
    /// Draws the name panel UI.
    /// </summary>
    public Rect DrawNamePanel(FuWindow window, bool drawBackground = false)
    {
        if (window == null || window.Container == null)
            return new Rect();

        Vector2 containerSize = new Vector2(window.Container.Size.x, window.Container.Size.y);
        return DrawNamePanel(Fugui.GetCurrentWindowDrawList(), window.LocalPosition, containerSize, drawBackground);
    }

    /// <summary>
    /// Draws the name panel UI.
    /// </summary>
    public Rect DrawNamePanel(FuDrawList drawList, Vector2 origin, Vector2 containerSize, bool drawBackground = false)
    {
        if (containerSize.x <= 0f || containerSize.y <= 0f)
            return new Rect(origin.x, origin.y, 0f, 0f);

        float scale = Fugui.Scale;
        TimelineWidgetTheme theme = Theme;
        bool mobileLayout = CodeWidget.IsMobileLayout(containerSize, scale);
        Rect viewportRect = new Rect(origin.x, origin.y, containerSize.x, containerSize.y);
        Rect safeRect = CodeWidget.GetSafeContentRect(origin, containerSize, mobileLayout, scale);
        Rect panelRect = GetPanelRect(safeRect, mobileLayout, scale);

        if (drawBackground)
            CodeWidget.DrawBackground(drawList, viewportRect, scale, theme);
        else
            panelRect = new Rect(origin, containerSize);

        Fugui.PushFont(18);
        DrawNamePanel(drawList, panelRect, scale, theme);
        Fugui.PopFont();

        Fugui.SetCursorScreenPos(new Vector2(origin.x, origin.y + containerSize.y));
        return panelRect;
    }

    /// <summary>
    /// Returns the panel rect value.
    /// </summary>
    private Rect GetPanelRect(Rect safeRect, bool mobileLayout, float scale)
    {
        float maxWidth = Mathf.Min(safeRect.width, (mobileLayout ? MobilePanelMaxWidth : DesktopPanelMaxWidth) * scale);
        float minDesktopWidth = Mathf.Min(420f * scale, maxWidth);
        float panelWidth = mobileLayout ? maxWidth : Mathf.Clamp(safeRect.width * 0.46f, minDesktopWidth, maxWidth);
        float panelHeight = Mathf.Clamp(340f * scale, Mathf.Min(280f * scale, safeRect.height), safeRect.height);

        return new Rect(
            safeRect.x + (safeRect.width - panelWidth) * 0.5f,
            safeRect.y + (safeRect.height - panelHeight) * 0.5f,
            panelWidth,
            panelHeight);
    }

    /// <summary>
    /// Draws the name panel UI.
    /// </summary>
    private void DrawNamePanel(FuDrawList drawList, Rect panelRect, float scale, TimelineWidgetTheme theme)
    {
        if (!Sara.IsVR)
            FlatCameraInputBlocker.BlockAllForFrame();

        float padding = 24f * scale;
        float rounding = theme.DockRadius * scale;

        drawList.AddRectFilled(panelRect.min + new Vector2(0f, 6f * scale), panelRect.max + new Vector2(0f, 8f * scale), ColorU32(theme.DockShadow, 0.70f), rounding);
        drawList.AddRectFilled(panelRect.min, panelRect.max, ColorU32(theme.DockBackground), rounding);
        drawList.AddRect(panelRect.min, panelRect.max, ColorU32(theme.DockBorder), rounding);

        Rect headerRect = new Rect(panelRect.x, panelRect.y, panelRect.width, 82f * scale);
        Rect contentRect = new Rect(
            panelRect.x + padding,
            headerRect.yMax + 18f * scale,
            Mathf.Max(1f, panelRect.width - padding * 2f),
            Mathf.Max(1f, panelRect.height - headerRect.height - padding - 18f * scale));

        drawList.PushClipRect(panelRect.min, panelRect.max, true);
        DrawHeader(drawList, headerRect, scale, theme);
        DrawBody(drawList, contentRect, scale, theme);
        drawList.PopClipRect();
    }

    /// <summary>
    /// Draws the header UI.
    /// </summary>
    private void DrawHeader(FuDrawList drawList, Rect rect, float scale, TimelineWidgetTheme theme)
    {
        float padding = 22f * scale;
        float iconSize = 40f * scale;
        Rect iconRect = new Rect(rect.x + padding, rect.y + (rect.height - iconSize) * 0.5f, iconSize, iconSize);
        Rect titleRect = new Rect(iconRect.xMax + 14f * scale, rect.y + 18f * scale, rect.width - iconRect.width - padding * 2f - 14f * scale, 24f * scale);
        Rect subtitleRect = new Rect(titleRect.x, titleRect.yMax + 3f * scale, titleRect.width, 20f * scale);

        drawList.AddCircleFilled(iconRect.center, iconRect.width * 0.5f, ColorU32(theme.PillBackgroundActive), 32);
        drawList.AddCircle(iconRect.center, iconRect.width * 0.5f, ColorU32(theme.DockBorder), 32, Mathf.Max(1f, scale));

        Fugui.PushFont(18);
        DrawIconCentered(drawList, iconRect, Icons.IDCard_duotone, ColorU32(theme.Accent));
        Fugui.PopFont();

        PushFont(16, true);
        DrawTextLeft(drawList, titleRect, ClipTextToWidth("Display name", titleRect.width), ColorU32(theme.Text));
        PopFont();

        PushFont(12, false);
        DrawTextLeft(drawList, subtitleRect, ClipTextToWidth(GetRoleLabel(), subtitleRect.width), ColorU32(theme.TextDim));
        PopFont();

        drawList.AddLine(
            new Vector2(rect.x, rect.yMax),
            new Vector2(rect.xMax, rect.yMax),
            ColorU32(theme.DockBorder, 0.60f),
            Mathf.Max(1f, scale));
    }

    /// <summary>
    /// Draws the body UI.
    /// </summary>
    private void DrawBody(FuDrawList drawList, Rect rect, float scale, TimelineWidgetTheme theme)
    {
        float inputHeight = 52f * scale;
        float statusHeight = 28f * scale;
        float actionHeight = 46f * scale;
        Rect labelRect = new Rect(rect.x, rect.y, rect.width, 22f * scale);
        Rect inputRect = new Rect(rect.x, labelRect.yMax + 6f * scale, rect.width, inputHeight);
        Rect statusRect = new Rect(rect.x, inputRect.yMax + 8f * scale, rect.width, statusHeight);
        Rect actionRect = new Rect(rect.x, rect.yMax - actionHeight, rect.width, actionHeight);

        PushFont(10, true);
        DrawTextLeft(drawList, labelRect, "N A M E", ColorU32(theme.TextFaint));
        PopFont();

        DrawNameInput(drawList, inputRect, scale, theme);
        DrawStatus(drawList, statusRect, scale, theme);
        DrawSubmitButton(drawList, actionRect, scale, theme);
    }

    /// <summary>
    /// Draws the name input UI.
    /// </summary>
    private void DrawNameInput(FuDrawList drawList, Rect rect, float scale, TimelineWidgetTheme theme)
    {
        FlatCameraInputBlocker.RegisterRect(rect);
        drawList.AddRectFilled(rect.min, rect.max, ColorU32(theme.SettingsDropdownBackground), theme.MediumRadius * scale);
        drawList.AddRect(rect.min, rect.max, ColorU32(_hasError ? ErrorColor : theme.DockBorder), theme.MediumRadius * scale, FuDrawFlags.None, Mathf.Max(1f, scale));

        Rect inputRect = InsetRect(rect, 12f * scale);
        if (Sara.IsVR)
        {
            DrawNameValue(drawList, inputRect, scale, theme);
            return;
        }

        FuLayout layout = FuWindow.CurrentDrawingWindow?.Layout;
        if (layout == null)
            return;

        Fugui.SetCursorScreenPos(inputRect.min + new Vector2(0f, (inputRect.height - 24f * scale) * 0.5f));
        FuTextStyle textStyle = new FuTextStyle(theme.Text, theme.Text, theme.TextFaint);
        FuFrameStyle inputStyle = new FuFrameStyle
        {
            Frame = Color.clear,
            HoveredFrame = Color.clear,
            ActiveFrame = Color.clear,
            CheckMark = theme.Accent,
            Border = Color.clear,
            Shadow = Color.clear,
            DisabledFrame = Color.clear,
            DisabledCheckMark = theme.TextFaint,
            DisabledBorder = Color.clear,
            DisabledShadow = Color.clear,
            TextStyle = textStyle
        };

        PushFont(16, false);
        layout.TextInput(
            "##remoteName",
            string.Empty,
            ref _name,
            MaxNameLength + 1u,
            0f,
            inputStyle,
            inputRect.width / scale,
            _nameInputOptions);
        PopFont();
    }

    /// <summary>
    /// Draws the externally edited name value.
    /// </summary>
    private void DrawNameValue(FuDrawList drawList, Rect rect, float scale, TimelineWidgetTheme theme)
    {
        string text = string.IsNullOrEmpty(_name) ? "Enter name" : _name;
        Color color = string.IsNullOrEmpty(_name) ? theme.TextFaint : theme.Text;

        PushFont(16, false);
        string clippedText = ClipTextToWidth(text, rect.width);
        Vector2 textSize = Fugui.CalcTextSize(clippedText);
        Vector2 textPos = new Vector2(rect.x, rect.y + (rect.height - textSize.y) * 0.5f);
        drawList.AddText(textPos, ColorU32(color), clippedText);
        PopFont();
    }

    /// <summary>
    /// Draws the status UI.
    /// </summary>
    private void DrawStatus(FuDrawList drawList, Rect rect, float scale, TimelineWidgetTheme theme)
    {
        Color color = _hasError ? ErrorColor : _sending ? theme.Accent : theme.TextDim;
        string message = _sending ? "Saving name..." : _statusMessage;
        Rect iconRect = new Rect(rect.x, rect.y, 24f * scale, rect.height);
        Rect textRect = new Rect(iconRect.xMax + 7f * scale, rect.y, Mathf.Max(1f, rect.width - iconRect.width - 7f * scale), rect.height);

        Fugui.PushFont(14);
        string icon = _hasError ? Icons.PlaneCircleXMark_duotone : _sending ? Icons.LocationArrowCircle_duotone : Icons.PlaneCircleCheck_duotone;
        DrawIconCentered(drawList, iconRect, icon, ColorU32(color));
        Fugui.PopFont();

        PushFont(12, true);
        DrawTextLeft(drawList, textRect, ClipTextToWidth(message, textRect.width), ColorU32(color));
        PopFont();
    }

    /// <summary>
    /// Draws the submit button UI.
    /// </summary>
    private void DrawSubmitButton(FuDrawList drawList, Rect rect, float scale, TimelineWidgetTheme theme)
    {
        bool enabled = !_sending && _session != null;
        bool clicked = DrawInvisibleHitBox(rect, "remoteNameSubmit", enabled, out bool hovered, out bool active);
        Color bg = enabled
            ? active ? theme.AccentHi : hovered ? Color.Lerp(theme.Accent, theme.AccentHi, 0.35f) : theme.Accent
            : theme.PillBackground;
        Color text = enabled ? theme.TextInk : theme.TextFaint;

        FlatCameraInputBlocker.RegisterRect(rect);
        drawList.AddRectFilled(rect.min, rect.max, ColorU32(bg, enabled ? 1f : 0.66f), rect.height * 0.5f);
        drawList.AddRect(rect.min, rect.max, ColorU32(enabled ? theme.AccentGlow : theme.DockBorder, enabled ? 0.70f : 0.45f), rect.height * 0.5f);

        string label = _sending ? "Saving name" : "Continue";
        PushFont(12, true);
        DrawTextCentered(drawList, rect, label, ColorU32(text));
        PopFont();

        if (hovered)
            Fugui.SetMouseCursor(FuMouseCursor.Hand);

        if (clicked)
            SubmitName();
    }


    /// <summary>
    /// Runs the submit name logic.
    /// </summary>
    private void SubmitName()
    {
        if (_sending || _session == null)
            return;

        if (Sara.Network == null)
        {
            SetError("Network client is not initialized.");
            return;
        }

        _name = SanitizeName(_name);
        _sending = true;
        _hasError = false;
        _statusMessage = "Saving name...";
        _forceDraw?.Invoke();

        Sara.Network.SetUserName(_name, HandleSetNameResponse);
    }

    /// <summary>
    /// Handles the set name response flow.
    /// </summary>
    private void HandleSetNameResponse(APIResponse<SaraSession> response)
    {
        _sending = false;

        if (response == null)
        {
            SetError("No response from server.");
            return;
        }

        if (!response.Success)
        {
            SetError(string.IsNullOrWhiteSpace(response.Message) ? "Name rejected by server." : response.Message);
            return;
        }

        SaraSession session = response.Data ?? _session;
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
        _statusMessage = "Name saved. Downloading flight...";
        RemoteNamePrompt.Clear(session);
        _submitted?.Invoke();
        Sara.Loader.LoadFlight(session.HeaderSas, session.DataSas);
    }

    /// <summary>
    /// Sets the error value.
    /// </summary>
    private void SetError(string message)
    {
        _hasError = true;
        _statusMessage = string.IsNullOrWhiteSpace(message) ? "Unable to save name." : message;
        _forceDraw?.Invoke();
    }

    /// <summary>
    /// Clears the transient error state.
    /// </summary>
    private void ClearTransientError()
    {
        if (!_hasError)
            return;

        _hasError = false;
        _statusMessage = "Confirm your display name";
    }

    /// <summary>
    /// Returns the role label value.
    /// </summary>
    private string GetRoleLabel()
    {
        SaraUser user = _session != null ? _session.User : null;
        if (user == null)
            return "Multiplayer session";

        switch (user.Role)
        {
            case SaraUserRole.Admin:
                return "Admin";
            case SaraUserRole.Captain:
                return "Captain";
            case SaraUserRole.FirstOfficer:
                return "First officer";
            case SaraUserRole.Observator:
                return "Observer";
            default:
                return "Multiplayer session";
        }
    }

    /// <summary>
    /// Runs the sanitize name logic.
    /// </summary>
    private static string SanitizeName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string name = value.Trim();
        return name.Length <= MaxNameLength ? name : name.Substring(0, MaxNameLength);
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
    /// Runs the push font logic.
    /// </summary>
    private static void PushFont(int size, bool bold)
    {
        Fugui.PushFont(size, bold ? FontType.Bold : FontType.Regular);
    }

    /// <summary>
    /// Runs the pop font logic.
    /// </summary>
    private static void PopFont()
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
    /// Draws the icon centered UI.
    /// </summary>
    private static void DrawIconCentered(FuDrawList drawList, Rect rect, string icon, uint color)
    {
        if (string.IsNullOrEmpty(icon))
            return;

        Vector2 iconSize = Fugui.CalcTextSize(icon, FuTextWrapping.None);
        Vector2 iconPos = new Vector2(
            rect.x + (rect.width - iconSize.x) * 0.5f,
            rect.y + (rect.height - iconSize.y) * 0.5f);

        drawList.AddText(iconPos, color, icon);
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
