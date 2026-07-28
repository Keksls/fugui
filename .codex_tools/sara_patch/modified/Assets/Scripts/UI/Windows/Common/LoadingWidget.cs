using Assets.Scripts.UI.Windows.Common;
using Fu;
using Fu.Framework;

using Saravr.Core;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>
/// Implements the loading widget logic.
/// </summary>
public class LoadingWidget
{
    private const float DesktopPanelMaxWidth = 760f;
    private const float MobilePanelMaxWidth = 560f;
    private const float CompactHeightThreshold = 430f;
    private const float ProgressSmoothSpeed = 6f;
    private const float RedrawInterval = 1f / 20f;

    private string[] _flightInfos = { Icons.PlaneCircleExclamation_duotone + " Getting flight data..." };
    private string _stepDescription = "Initializing...";
    private float _globalProgress;
    private float _progress;
    private float _displayedGlobalProgress;
    private float _displayedProgress;
    private Coroutine _flightInfoAnimation;
    private MonoBehaviour _coroutineOwner;
    private Action _requestRedraw;
    private Action _loadingComplete;
    private FlightLoader _loader;
    private float _redrawTimer;
    private bool _redrawRequested;
    private bool _subscribedToLoader;
    private bool _flightInfosInitialized;
    private TimelineWidgetTheme _theme;

    public TimelineWidgetTheme Theme
    {
        get { return _theme != null ? _theme : TimelineWidgetTheme.LoadDefault(); }
        set { _theme = value; }
    }

    /// <summary>
    /// Sets the theme value.
    /// </summary>
    public void SetTheme(TimelineWidgetTheme theme)
    {
        _theme = theme;
    }

    /// <summary>
    /// Binds this widget to a host behaviour and starts listening to loading events.
    /// </summary>
    /// <param name="coroutineOwner">Behaviour used to run the flight info text animation.</param>
    /// <param name="requestRedraw">Callback invoked when the host window must redraw.</param>
    /// <param name="loadingComplete">Optional callback invoked when the loader completes.</param>
    public void Bind(MonoBehaviour coroutineOwner, Action requestRedraw, Action loadingComplete = null)
    {
        if (_coroutineOwner != null && _coroutineOwner != coroutineOwner)
            StopFlightInfoAnimation();

        _coroutineOwner = coroutineOwner;
        _requestRedraw = requestRedraw;
        _loadingComplete = loadingComplete;
        TrySubscribeToLoader();
        RequestRedraw();
    }

    /// <summary>
    /// Stops listening to loading events and cancels any running animation.
    /// </summary>
    public void Unbind()
    {
        UnsubscribeFromLoader();
        StopFlightInfoAnimation();
        _coroutineOwner = null;
        _requestRedraw = null;
        _loadingComplete = null;
    }

    /// <summary>
    /// Updates loader subscriptions and throttled redraw requests.
    /// </summary>
    public void Update()
    {
        TrySubscribeToLoader();

        FlightLoader loader = _loader ?? Sara.Loader;
        bool loadingActive = loader != null && loader.CurrentStep != FlightLoadingStep.Complete;
        bool progressStillAnimating =
            !Mathf.Approximately(_displayedProgress, _progress) ||
            !Mathf.Approximately(_displayedGlobalProgress, _globalProgress);

        if (!loadingActive && !_redrawRequested && !progressStillAnimating)
            return;

        _redrawTimer += Time.unscaledDeltaTime;
        if (_redrawTimer >= RedrawInterval)
        {
            _redrawTimer = 0f;
            _redrawRequested = false;
            _requestRedraw?.Invoke();
        }
    }

    /// <summary>
    /// Requests a host redraw on the next throttled widget update.
    /// </summary>
    public void RequestRedraw()
    {
        _redrawRequested = true;
    }

    /// <summary>
    /// Draws the loading panel centered in the current Fu window.
    /// </summary>
    /// <param name="window">Current Fu window, including 3D windows.</param>
    /// <param name="drawBackground">Whether to draw the full-window loading background.</param>
    /// <returns>The drawn loading panel rectangle.</returns>
    public Rect DrawLoadingPanel(FuWindow window, bool drawBackground = false)
    {
        if (window == null || window.Container == null)
            return new Rect();

        Vector2 containerSize = new Vector2(window.Container.Size.x, window.Container.Size.y);
        return DrawLoadingPanel(Fugui.GetCurrentWindowDrawList(), window.LocalPosition, containerSize, drawBackground);
    }

    /// <summary>
    /// Draws the loading panel centered in the provided container.
    /// </summary>
    /// <param name="drawList">Fugui draw list receiving the panel geometry.</param>
    /// <param name="origin">Screen-space origin for the container.</param>
    /// <param name="containerSize">Screen-space size for the container.</param>
    /// <param name="drawBackground">Whether to draw the full-container loading background.</param>
    /// <returns>The drawn loading panel rectangle.</returns>
    public Rect DrawLoadingPanel(FuDrawList drawList, Vector2 origin, Vector2 containerSize, bool drawBackground = false)
    {
        if (containerSize.x <= 0f || containerSize.y <= 0f)
            return new Rect(origin.x, origin.y, 0f, 0f);

        UpdateDisplayedProgress();
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
        DrawLoadingPanel(drawList, panelRect, mobileLayout, compactLayout, scale);
        Fugui.PopFont();

        Fugui.SetCursorScreenPos(new Vector2(origin.x, origin.y + containerSize.y));
        return panelRect;
    }

    /// <summary>
    /// Draws only the loading panel inside an already computed rectangle.
    /// </summary>
    public void DrawLoadingPanel(FuDrawList drawList, Rect panelRect, bool mobileLayout, bool compactLayout, float scale)
    {
        TimelineWidgetTheme theme = Theme;
        float padding = (mobileLayout ? 18f : 22f) * scale;
        float gap = (compactLayout ? 10f : 14f) * scale;
        float rounding = (mobileLayout ? theme.MediumRadius : theme.DockRadius) * scale;

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

        float progressHeight = (compactLayout ? 88f : 106f) * scale;
        Rect progressRect = new Rect(contentRect.x, contentRect.y, contentRect.width, Mathf.Min(progressHeight, contentRect.height));
        Rect infoRect = new Rect(contentRect.x, progressRect.yMax + gap, contentRect.width, Mathf.Max(0f, contentRect.yMax - progressRect.yMax - gap));

        drawList.PushClipRect(panelRect.min, panelRect.max, true);
        DrawHeader(drawList, headerRect, mobileLayout, compactLayout, scale, theme);
        DrawProgressSection(drawList, progressRect, compactLayout, scale, theme);
        DrawFlightInfoList(drawList, infoRect, compactLayout, scale, theme);
        drawList.PopClipRect();
    }

    /// <summary>
    /// Draws the full loading background behind the panel.
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
        drawList.AddLine(new Vector2(rect.x, rect.y + bandHeight), new Vector2(rect.xMax, rect.y + bandHeight), lineColor, 1f * scale);
    }

    /// <summary>
    /// Calculates the centered loading panel rectangle for the current widget state.
    /// </summary>
    public Rect GetPanelRect(Rect safeRect, bool mobileLayout, bool compactLayout, float scale)
    {
        float maxWidth = Mathf.Min(safeRect.width, (mobileLayout ? MobilePanelMaxWidth : DesktopPanelMaxWidth) * scale);
        float minDesktopWidth = Mathf.Min(520f * scale, maxWidth);
        float panelWidth = mobileLayout
            ? maxWidth
            : Mathf.Clamp(safeRect.width * 0.62f, minDesktopWidth, maxWidth);
        float padding = (mobileLayout ? 18f : 22f) * scale;
        float gap = (compactLayout ? 10f : 14f) * scale;
        float headerHeight = (compactLayout ? 58f : 72f) * scale;
        float bodyTopPadding = (compactLayout ? 10f : 14f) * scale;
        float progressHeight = (compactLayout ? 88f : 106f) * scale;
        float sectionHeight = (compactLayout ? 24f : 30f) * scale;
        float rowHeight = (compactLayout ? 38f : 44f) * scale;
        int infoCount = Mathf.Min(5, Mathf.Max(1, Mathf.FloorToInt((safeRect.height * 0.42f - sectionHeight) / rowHeight)));
        int actualInfoRows = Mathf.Min(_flightInfos == null ? 0 : _flightInfos.Length, infoCount);
        float infoHeight = actualInfoRows > 0 ? sectionHeight + actualInfoRows * rowHeight : 0f;
        float panelHeight = headerHeight + bodyTopPadding + progressHeight + (actualInfoRows > 0 ? gap + infoHeight : 0f) + padding;
        panelHeight = Mathf.Min(panelHeight, safeRect.height);

        return new Rect(
            safeRect.x + (safeRect.width - panelWidth) * 0.5f,
            safeRect.y + (safeRect.height - panelHeight) * 0.5f,
            panelWidth,
            panelHeight);
    }

    /// <summary>
    /// Returns the safe content rectangle used to center the loading panel.
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
    /// Returns whether the container should use compact vertical measurements.
    /// </summary>
    public static bool IsCompactLayout(Vector2 containerSize, float scale)
    {
        return containerSize.y < CompactHeightThreshold * scale;
    }

    /// <summary>
    /// Returns whether the container should use the mobile layout.
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

    #region Loader state
    /// <summary>
    /// Subscribes to the loader events if not already subscribed and if the loader is available.
    /// </summary>
    private void TrySubscribeToLoader()
    {
        if (_subscribedToLoader || Sara.Loader == null)
            return;

        _loader = Sara.Loader;
        _loader.OnStepChanged += Loader_OnStepChanged;
        _loader.OnProgressUpdated += Loader_OnProgressUpdated;
        _loader.OnLoadingComplete += Loader_OnLoadingComplete;
        _subscribedToLoader = true;

        SynchronizeWithLoader();
    }

    /// <summary>
    /// Unsubscribes from the loader events and clears related state.
    /// </summary>
    private void UnsubscribeFromLoader()
    {
        if (!_subscribedToLoader || _loader == null)
        {
            _subscribedToLoader = false;
            _loader = null;
            return;
        }

        _loader.OnStepChanged -= Loader_OnStepChanged;
        _loader.OnProgressUpdated -= Loader_OnProgressUpdated;
        _loader.OnLoadingComplete -= Loader_OnLoadingComplete;
        _subscribedToLoader = false;
        _loader = null;
    }

    /// <summary>
    /// Synchronizes the widget state with the current loader state, including progress, step description and flight infos if available.
    /// </summary>
    private void SynchronizeWithLoader()
    {
        FlightLoader loader = _loader ?? Sara.Loader;
        if (loader == null)
            return;

        FlightLoadingStep currentStep = loader.CurrentStep;
        _stepDescription = GetStepDescription(currentStep);
        _progress = loader.CurrentStepProgress;
        _globalProgress = loader.GetCurrentGlobalProgress();

        if (currentStep == FlightLoadingStep.DownloadingFlightData)
        {
            _displayedProgress = _progress;
            _displayedGlobalProgress = _globalProgress;
        }

        if (!_flightInfosInitialized && Sara.Flight != null && currentStep >= FlightLoadingStep.BakingOffsets)
            UpdateFlightInfos(currentStep == FlightLoadingStep.BakingOffsets);

        RequestRedraw();
    }

    /// <summary>
    /// Whenever the loader step changes, updates the step description and resets the step progress. Also updates flight infos when reaching the baking step.
    /// </summary>
    /// <param name="loadingStep"> New loading step.</param>
    private void Loader_OnStepChanged(FlightLoadingStep loadingStep)
    {
        FlightLoader loader = _loader ?? Sara.Loader;
        if (loader == null)
            return;

        if (loadingStep == FlightLoadingStep.DownloadingFlightData)
        {
            _displayedGlobalProgress = 0f;
            _displayedProgress = 0f;
        }

        _stepDescription = GetStepDescription(loadingStep);
        _progress = 0f;
        _globalProgress = loader.GetCurrentGlobalProgress();

        if (loadingStep == FlightLoadingStep.BakingOffsets)
            UpdateFlightInfos(true);

        RequestRedraw();
    }

    /// <summary>
    /// Handles the loader progress updated event.
    /// </summary>
    private void Loader_OnProgressUpdated(FlightLoadingStep loadingStep, float currentStepProgress)
    {
        FlightLoader loader = _loader ?? Sara.Loader;
        if (loader == null)
            return;

        _progress = currentStepProgress;
        _globalProgress = loader.GetCurrentGlobalProgress();
        RequestRedraw();
    }

    /// <summary>
    /// Handles the loader loading complete event.
    /// </summary>
    private void Loader_OnLoadingComplete()
    {
        StopFlightInfoAnimation();

        _flightInfos = new string[] { Icons.PlaneCircleCheck_duotone + " Loading complete!" };
        _progress = 1f;
        _globalProgress = 1f;
        _displayedProgress = 1f;
        _displayedGlobalProgress = 1f;
        RequestRedraw();
        _loadingComplete?.Invoke();
    }

    /// <summary>
    /// Updates the flight infos state.
    /// </summary>
    private void UpdateFlightInfos(bool animate)
    {
        if (Sara.Flight == null)
            return;

        _flightInfosInitialized = true;

        string flightNumber = Sara.Flight.Container.FlightNumber;
        string departure = Sara.Flight.Container.Origin;
        string arrival = Sara.Flight.Container.Destination;
        string date = Sara.Flight.Container.TakeOffDate.ToString("MMMM dd, yyyy");
        string aircraft = Sara.Flight.AircraftTypeDash;
        Sara.Flight.GetRawDoubleAt("rk:Latitude", Sara.Flight.GetFirstPhase("TO").TID, out double departurLat);
        Sara.Flight.GetRawDoubleAt("rk:Longitude", Sara.Flight.GetFirstPhase("TO").TID, out double departureLon);
        Sara.Flight.GetRawDoubleAt("rk:Latitude", Sara.Flight.GetLastPhase("LANDING").TID, out double arrivalLat);
        Sara.Flight.GetRawDoubleAt("rk:Longitude", Sara.Flight.GetLastPhase("LANDING").TID, out double arrivalLon);
        string departureCoords = $"{departurLat:F2} - {departureLon:F2}";
        string arrivalCoords = $"{arrivalLat:F2} - {arrivalLon:F2}";

        string[] flightInfosLines = new string[5] {
            $"{Icons.Airline_duotone} Flight {flightNumber}",
            $"{Icons.PlaneDeparture_duotone} {departure} [{departureCoords}]",
            $"{Icons.PlaneArrival_duotone} {arrival} [{arrivalCoords}]",
            $"{Icons.CalendarDay_duotone} {date}",
            $"{Icons.Plane_duotone} {aircraft}"
        };

        StopFlightInfoAnimation();

        if (animate && _coroutineOwner != null)
            _flightInfoAnimation = _coroutineOwner.StartCoroutine(AnimateTextAppear(flightInfosLines, 0.5f));
        else
            _flightInfos = flightInfosLines;

        RequestRedraw();
    }

    /// <summary>
    /// Runs the stop flight info animation logic.
    /// </summary>
    private void StopFlightInfoAnimation()
    {
        if (_flightInfoAnimation == null || _coroutineOwner == null)
        {
            _flightInfoAnimation = null;
            return;
        }

        _coroutineOwner.StopCoroutine(_flightInfoAnimation);
        _flightInfoAnimation = null;
    }

    /// <summary>
    /// Runs the animate text appear logic.
    /// </summary>
    private IEnumerator AnimateTextAppear(string[] lines, float duration)
    {
        _flightInfos = new string[lines.Length];
        int longestLineLength = lines.Max(l => l.Length);
        if (longestLineLength <= 0)
        {
            _flightInfos = lines;
            _flightInfoAnimation = null;
            yield break;
        }

        float timePerChar = duration / longestLineLength;

        for (int i = 1; i <= longestLineLength; i++)
        {
            for (int j = 0; j < lines.Length; j++)
            {
                if (lines[j].Length >= i)
                    _flightInfos[j] = lines[j].Substring(0, Mathf.Min(i, lines[j].Length));
            }
            yield return new WaitForSeconds(timePerChar);
            RequestRedraw();
        }

        _flightInfos = lines;
        _flightInfoAnimation = null;
        RequestRedraw();
    }

    /// <summary>
    /// Updates the displayed progress state.
    /// </summary>
    private void UpdateDisplayedProgress()
    {
        float delta = Time.unscaledDeltaTime > 0f ? Time.unscaledDeltaTime : Time.deltaTime;
        float maxDelta = delta * ProgressSmoothSpeed;

        _displayedGlobalProgress = Mathf.MoveTowards(_displayedGlobalProgress, _globalProgress, maxDelta);
        _displayedProgress = _progress < _displayedProgress
            ? _progress
            : Mathf.MoveTowards(_displayedProgress, _progress, maxDelta);
    }

    /// <summary>
    /// Returns the step description value.
    /// </summary>
    private static string GetStepDescription(FlightLoadingStep loadingStep)
    {
        switch (loadingStep)
        {
            case FlightLoadingStep.None:
                return "Preparing loading";
            case FlightLoadingStep.DownloadingFlightData:
                return "Getting flight data";
            case FlightLoadingStep.BakingOffsets:
                return "Aligning terrain data";
            case FlightLoadingStep.GeneratingFlightPath:
                return "Building flight path";
            case FlightLoadingStep.SceneTransition:
                return "Preparing cockpit";
            case FlightLoadingStep.Complete:
                return "Flight ready";
            default:
                return "Loading flight";
        }
    }
    #endregion

    /// <summary>
    /// Draws the header UI.
    /// </summary>
    private void DrawHeader(FuDrawList drawList, Rect rect, bool mobileLayout, bool compactLayout, float scale, TimelineWidgetTheme theme)
    {
        float spinnerRadius = (compactLayout ? 15f : 18f) * scale;
        float spinnerSize = spinnerRadius * 2f;
        float iconGap = 16f * scale;
        float headerPadding = (mobileLayout ? 18f : 22f) * scale;
        float percentWidth = mobileLayout && rect.width < 360f * scale ? 0f : 74f * scale;
        Rect spinnerRect = new Rect(rect.x + headerPadding, rect.y + Mathf.Max(0f, (rect.height - spinnerSize) * 0.5f), spinnerSize, spinnerSize);
        Rect percentRect = new Rect(rect.xMax - headerPadding - percentWidth, rect.y + (rect.height - 30f * scale) * 0.5f, percentWidth, 30f * scale);
        float textRight = percentWidth > 0f ? percentRect.x - 12f * scale : rect.xMax - headerPadding;
        Rect titleRect = new Rect(spinnerRect.xMax + iconGap, rect.y + 14f * scale, Mathf.Max(1f, textRight - spinnerRect.xMax - iconGap), 24f * scale);
        Rect subtitleRect = new Rect(titleRect.x, titleRect.yMax + 3f * scale, titleRect.width, 20f * scale);

        DrawSpinner(drawList, spinnerRect.center, spinnerRadius, 4f * scale, scale, theme);

        string title = _displayedGlobalProgress >= 1f ? "Flight ready" : "Loading flight data";
        PushFont(16, true);
        DrawTextLeft(drawList, titleRect, ClipTextToWidth(title, titleRect.width), ColorU32(theme.Text));
        PopFont(true);

        PushFont(12, false);
        DrawTextLeft(drawList, subtitleRect, ClipTextToWidth(_stepDescription, subtitleRect.width), ColorU32(theme.TextDim));
        PopFont(false);

        if (percentWidth > 0f)
        {
            string percentText = FormatPercent(_displayedGlobalProgress);
            drawList.AddRectFilled(percentRect.min, percentRect.max, ColorU32(theme.PillBackgroundActive), percentRect.height * 0.5f);
            drawList.AddRect(percentRect.min, percentRect.max, ColorU32(theme.DockBorder), percentRect.height * 0.5f);

            PushFont(14, true);
            DrawTextCentered(drawList, percentRect, percentText, ColorU32(theme.Accent));
            PopFont(true);
        }

        drawList.AddLine(
            new Vector2(rect.x, rect.yMax),
            new Vector2(rect.xMax, rect.yMax),
            ColorU32(theme.DockBorder, 0.60f),
            Mathf.Max(1f, scale));
    }

    /// <summary>
    /// Draws the progress section UI.
    /// </summary>
    private void DrawProgressSection(FuDrawList drawList, Rect rect, bool compactLayout, float scale, TimelineWidgetTheme theme)
    {
        float sectionHeight = (compactLayout ? 18f : 22f) * scale;
        float labelHeight = 20f * scale;
        float mainBarHeight = (compactLayout ? 12f : 16f) * scale;
        float stepBarHeight = 6f * scale;
        float lineGap = (compactLayout ? 8f : 12f) * scale;
        Rect sectionRect = new Rect(rect.x, rect.y, rect.width, sectionHeight);
        Rect labelRect = new Rect(rect.x, sectionRect.yMax + 2f * scale, rect.width, labelHeight);
        Rect mainBarRect = new Rect(rect.x, labelRect.yMax + 6f * scale, rect.width, mainBarHeight);
        Rect stepLabelRect = new Rect(rect.x, mainBarRect.yMax + lineGap, rect.width, labelHeight);
        Rect stepBarRect = new Rect(rect.x, stepLabelRect.yMax + 5f * scale, rect.width, stepBarHeight);

        DrawSectionLabel(drawList, sectionRect, "P R O G R E S S", theme, 1f);

        PushFont(14, true);
        DrawTextLeft(drawList, labelRect, "Overall progress", ColorU32(theme.TextDim));
        DrawTextRight(drawList, labelRect, FormatPercent(_displayedGlobalProgress), ColorU32(theme.Text));
        PopFont(true);

        DrawProgressBar(drawList, mainBarRect, _displayedGlobalProgress, theme.Accent, theme, true);

        PushFont(14, true);
        DrawTextLeft(drawList, stepLabelRect, ClipTextToWidth(_stepDescription, stepLabelRect.width - 56f * scale), ColorU32(theme.Text));
        DrawTextRight(drawList, stepLabelRect, FormatPercent(_displayedProgress), ColorU32(theme.TextDim));
        PopFont(true);

        DrawProgressBar(drawList, stepBarRect, _displayedProgress, theme.AccentHi, theme, false);
    }

    /// <summary>
    /// Draws the flight info list UI.
    /// </summary>
    private void DrawFlightInfoList(FuDrawList drawList, Rect rect, bool compactLayout, float scale, TimelineWidgetTheme theme)
    {
        if (rect.height <= 0f || _flightInfos == null || _flightInfos.Length == 0)
            return;

        float sectionHeight = (compactLayout ? 24f : 30f) * scale;
        float rowHeight = (compactLayout ? 38f : 44f) * scale;
        Rect sectionRect = new Rect(rect.x, rect.y, rect.width, Mathf.Min(sectionHeight, rect.height));
        DrawSectionLabel(drawList, sectionRect, "F L I G H T", theme, 1f);

        Rect rowsRect = new Rect(rect.x, sectionRect.yMax, rect.width, Mathf.Max(0f, rect.yMax - sectionRect.yMax));
        int maxRows = Mathf.FloorToInt(rowsRect.height / rowHeight);
        if (maxRows <= 0)
            return;

        int rowCount = Mathf.Min(_flightInfos.Length, maxRows);

        for (int i = 0; i < rowCount; i++)
        {
            Rect rowRect = new Rect(rowsRect.x, rowsRect.y + i * rowHeight, rowsRect.width, rowHeight);
            DrawFlightInfoRow(drawList, rowRect, _flightInfos[i], scale, theme);
        }
    }

    /// <summary>
    /// Draws the flight info row UI.
    /// </summary>
    private void DrawFlightInfoRow(FuDrawList drawList, Rect rect, string line, float scale, TimelineWidgetTheme theme)
    {
        float iconSize = 22f * scale;
        float padding = 2f * scale;
        Rect iconRect = new Rect(rect.x + padding, rect.y + (rect.height - iconSize) * 0.5f, iconSize, iconSize);
        Rect textRect = new Rect(iconRect.xMax + 10f * scale, rect.y, Mathf.Max(1f, rect.xMax - iconRect.xMax - padding - 10f * scale), rect.height);

        drawList.AddLine(rect.min, new Vector2(rect.xMax, rect.y), ColorU32(theme.SettingsRowDivider), Mathf.Max(1f, scale));

        if (!string.IsNullOrEmpty(line))
        {
            string icon = line.Substring(0, 1);
            string text = line.Length > 1 ? line.Substring(1).TrimStart() : string.Empty;

            Fugui.PushFont(18);
            DrawIconCenteredTinted(drawList, iconRect, icon, ColorU32(theme.Accent), ColorU32(theme.Accent), ColorU32(WithAlpha(theme.AccentHi, 0.72f)));
            Fugui.PopFont();

            PushFont(14, true);
            DrawTextLeftCentered(drawList, textRect, text, ColorU32(theme.TextDim), 0f);
            PopFont(true);
        }
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

        drawList.AddCircleFilled(center, radius + 7f * scale, ColorU32(theme.AccentGlow, 0.26f), 40);
        drawList.AddCircle(center, radius, ColorU32(theme.Track), 40, thickness);
        drawList.PathArcTo(center, radius, start, end, 28);
        drawList.PathStroke(ColorU32(theme.Accent), FuDrawFlags.None, thickness);
        drawList.AddCircleFilled(center, Mathf.Max(2f * scale, thickness * 0.55f), ColorU32(theme.Text), 16);
    }

    /// <summary>
    /// Draws the progress bar UI.
    /// </summary>
    private static void DrawProgressBar(FuDrawList drawList, Rect rect, float value, Color fillColor, TimelineWidgetTheme theme, bool drawBorder)
    {
        float clampedValue = SanitizeProgress(value);
        float rounding = rect.height * 0.5f;

        drawList.AddRectFilled(rect.min, rect.max, ColorU32(theme.Track), rounding);

        if (clampedValue > 0f)
        {
            float fillWidth = Mathf.Max(rect.height, rect.width * clampedValue);
            fillWidth = Mathf.Min(fillWidth, rect.width);
            Rect fillRect = new Rect(rect.x, rect.y, fillWidth, rect.height);
            drawList.AddRectFilled(
                fillRect.min - new Vector2(0f, drawBorder ? 1f * Fugui.Scale : 0f),
                fillRect.max + new Vector2(0f, drawBorder ? 1f * Fugui.Scale : 0f),
                ColorU32(theme.AccentGlow, drawBorder ? 0.36f : 0.22f),
                rounding + 1f * Fugui.Scale);
            drawList.AddRectFilled(fillRect.min, fillRect.max, ColorU32(fillColor), rounding);
        }

        if (drawBorder)
            drawList.AddRect(rect.min, rect.max, ColorU32(theme.DockBorder), rounding);
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
    /// Runs the sanitize progress logic.
    /// </summary>
    private static float SanitizeProgress(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            return 0f;

        return Mathf.Clamp01(value);
    }

    /// <summary>
    /// Formats the percent value for display.
    /// </summary>
    private static string FormatPercent(float value)
    {
        return Mathf.RoundToInt(SanitizeProgress(value) * 100f) + "%";
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
    /// Draws the text right UI.
    /// </summary>
    private static void DrawTextRight(FuDrawList drawList, Rect rect, string text, uint color)
    {
        Vector2 textSize = Fugui.CalcTextSize(text);
        Vector2 textPos = new Vector2(rect.xMax - textSize.x, rect.y + (rect.height - textSize.y) * 0.5f);
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
    /// Draws the icon centered UI.
    /// </summary>
    private static void DrawIconCentered(FuDrawList drawList, Rect rect, string icon, bool disabled)
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
            drawList.AddText(iconPos, Fugui.GetPrimaryDuotoneColor(disabled), primary.ToString());
            drawList.AddText(iconPos, Fugui.GetSecondaryDuotoneColor(disabled), ((char)(((ushort)primary) + 1)).ToString());
        }
        else
        {
            drawList.AddText(iconPos, Fugui.GetColorU32(disabled ? FuColors.TextDisabled : FuColors.Text), icon);
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
