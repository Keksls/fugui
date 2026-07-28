using Fu;
using Fu.Framework;

using Assets.Scripts.UI.Shortcuts;
using Saravr.Engine;
using Saravr.Engine.Visuals;
using Saravr.Network.Common;
using System;
using System.Collections.Generic;
using TekelKernel3.Flight;
using UnityEngine;

/// <summary>
/// Lists the supported event severity values.
/// </summary>
public enum EventSeverity
{
    None = -1,
    Info = 0,
    Low = 1,
    Medium = 2,
    High = 3,
}

/// <summary>
/// Stores timeline event data.
/// </summary>
public struct TimelineEvent
{
    public TID Tid;
    public string Label;
    public EventSeverity Tone;
    public bool IsImportant;

    /// <summary>
    /// Creates a new timeline event instance.
    /// </summary>
    public TimelineEvent(TID tid, string label, EventSeverity tone = EventSeverity.Info, bool isImportant = false)
    {
        Tid = tid;
        Label = label;
        Tone = tone;
        IsImportant = isImportant;
    }
}

namespace Assets.Scripts.UI.Windows.Common
{
    /// <summary>
    /// Implements the timeline widget logic.
    /// </summary>
    public class TimelineWidget
    {
        private const float SeekStepSeconds = 10f;
        private const float FastSeekSecondsPerSecond = 75f;
        private const float HoldSeekDelaySeconds = 0.34f;
        private const float DoubleTapDelaySeconds = 0.32f;
        private const float SeekFeedbackDurationSeconds = 0.55f;
        private const float SpeedMenuOptionHeight = 34f;

        private static readonly float[] PlaybackSpeeds = { 0.5f, 1.0f, 2.0f, 4.0f, 8.0f };
        private static readonly string[] PlaybackSpeedLabels = { "0.5x", "1x", "2x", "4x", "8x" };
        private static readonly FuLayout ShortcutTooltipLayout = new FuLayout();

        private readonly List<TimelineEvent> _events = new List<TimelineEvent>();
        private TimelineWidgetTheme _theme;
        private object _eventsSourceContainer;
        private int _eventsSourceSignature;
        private bool _usingManualEvents;
        private int _openEventClusterHash;
        private int _currentPhaseIndex;
        private bool _timelineDragging;
        private bool _speedMenuOpen;
        private bool _speedMenuOpenedThisFrame;
        private float _speedMenuAmount;
        private int _buttonSeekDirection;
        private float _buttonSeekStartTime;
        private int _screenSeekDirection;
        private bool _screenSeekHolding;
        private float _screenSeekHoldStartTime;
        private float _lastScreenTapTime = -1f;
        private int _lastScreenTapSide;
        private Vector2 _lastScreenTapPosition;
        private int _seekFeedbackDirection;
        private bool _seekFeedbackFast;
        private float _seekFeedbackStartTime = -10f;
        private bool _inputBlocked;

        /// <summary>
        /// Implements the event group logic.
        /// </summary>
        private sealed class EventGroup
        {
            public readonly List<TimelineEvent> Events = new List<TimelineEvent>();
            public float StartNormalized;
            public float EndNormalized;
            public float CenterNormalized;
            public EventSeverity Tone;
            public int Hash;
            /// <summary>
            /// Gets or sets whether the cluster state is active.
            /// </summary>
            public bool IsCluster => Events.Count > 1;
        }

        /// <summary>
        /// Stores event visual data.
        /// </summary>
        private struct EventVisual
        {
            public Vector2 DotCenter;
            public float DotRadius;
            public Rect DotHitRect;
            public Rect PillRect;
            public Rect? PopoverRect;
            public bool IsClusterOpen;
        }

        /// <summary>
        /// Stores event popover request data.
        /// </summary>
        private struct EventPopoverRequest
        {
            public bool HasValue;
            public Rect Rect;
            public EventGroup Group;
            public Color EventColor;
            public bool ClickedInsideEvent;
            public bool MouseClicked;
        }

        public TimelineWidgetTheme Theme
        {
            get { return _theme != null ? _theme : TimelineWidgetTheme.LoadDefault(); }
            set { _theme = value; }
        }

        /// <summary>
        /// Gets or sets the clock mode label value.
        /// </summary>
        public string ClockModeLabel => TimelineClock.ModeLabel;
        /// <summary>
        /// Gets or sets the clock text value.
        /// </summary>
        public string ClockText => TimelineClock.Text;
        /// <summary>
        /// Gets whether the timeline is visible but cannot be controlled by the local user.
        /// </summary>
        public bool IsReadOnly => CanReadTimeline() && !CanControlTimeline();
        /// <summary>
        /// Gets whether the local user can control the timeline.
        /// </summary>
        public bool CanControl => CanReadTimeline() && CanControlTimeline();
        /// <summary>
        /// Gets or sets the shortcut tooltip provider.
        /// </summary>
        public Func<SaraShortcutAction, string> ShortcutTooltipProvider { get; set; }
        /// <summary>
        /// Gets or sets whether timeline input is blocked by a modal flat overlay.
        /// </summary>
        public bool InputBlocked
        {
            get { return _inputBlocked; }
            set
            {
                if (_inputBlocked == value)
                    return;

                _inputBlocked = value;
                if (_inputBlocked)
                    ResetInteractiveState();
            }
        }


        #region Timeline Data And Controls
        /// <summary>
        /// Sets the theme value.
        /// </summary>
        public void SetTheme(TimelineWidgetTheme theme)
        {
            _theme = theme;
        }



        /// <summary>
        /// Toggles the clock mode state.
        /// </summary>
        public void ToggleClockMode()
        {
            // Keep every timeline surface and cockpit clock on the same display mode.
            TimelineClock.ToggleMode();
        }

        /// <summary>
        /// Toggles the play state through the shared timeline control path.
        /// </summary>
        public bool TryTogglePlayPause()
        {
            if (!CanControl)
                return false;

            SetTimelinePlaying(!Sara.Time.IsPlaying);
            return true;
        }

        /// <summary>
        /// Seeks the timeline by the requested number of seconds.
        /// </summary>
        public bool TrySeekSeconds(float seconds, bool immediate = true)
        {
            if (!CanControl || Mathf.Approximately(seconds, 0f))
                return false;

            SeekSeconds(seconds, immediate);
            TriggerSeekFeedback(seconds < 0f ? -1 : 1, false);
            return true;
        }

        /// <summary>
        /// Draws the screen seek gestures UI.
        /// </summary>
        public void DrawScreenSeekGestures(Vector2 containerSize, bool enabled)
        {
            if (!enabled || InputBlocked || containerSize.x <= 0f || containerSize.y <= 0f || !CanReadTimeline() || !CanControlTimeline())
            {
                ResetScreenSeekGesture();
                return;
            }

            UpdateScreenSeekGestures(containerSize);
            DrawSeekFeedback(Fugui.GetCurrentWindowDrawList(), containerSize);
        }





        /// <summary>
        /// Sets the events value.
        /// </summary>
        public void SetEvents(IEnumerable<TimelineEvent> events)
        {
            List<TimelineEvent> nextEvents = events != null ? new List<TimelineEvent>(events) : new List<TimelineEvent>();
            if (_usingManualEvents && AreTimelineEventsEqual(_events, nextEvents))
                return;

            _events.Clear();
            _usingManualEvents = true;
            _eventsSourceContainer = null;
            _eventsSourceSignature = 0;
            _openEventClusterHash = 0;

            foreach (TimelineEvent timelineEvent in nextEvents)
                _events.Add(timelineEvent);
        }

        /// <summary>
        /// Runs the add event logic.
        /// </summary>
        public void AddEvent(TimelineEvent timelineEvent)
        {
            _usingManualEvents = true;
            _eventsSourceContainer = null;
            _eventsSourceSignature = 0;
            _events.Add(timelineEvent);
        }

        /// <summary>
        /// Clears the events state.
        /// </summary>
        public void ClearEvents()
        {
            _events.Clear();
            _usingManualEvents = false;
            _eventsSourceContainer = null;
            _eventsSourceSignature = 0;
            _openEventClusterHash = 0;
        }



        #endregion

        #region Timeline Rendering
        /// <summary>
        /// Draws the dock UI.
        /// </summary>
        public void DrawDock(Rect rect)
        {
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            TimelineWidgetTheme theme = Theme;
            float scale = Fugui.Scale;
            FuDrawList drawList = Fugui.GetCurrentWindowDrawList();
            float rounding = theme.DockRadius * scale;

            FlatCameraInputBlocker.RegisterRect(rect);
            drawList.AddRectFilled(rect.min, rect.max, ColorU32(theme.DockBackground), rounding);
            drawList.AddRect(rect.min, rect.max, ColorU32(theme.DockBorder), rounding);

            Rect contentRect = new Rect(
                rect.x + theme.DockPaddingLeft * scale,
                rect.y + theme.DockPaddingTop * scale,
                rect.width - (theme.DockPaddingLeft + theme.DockPaddingRight) * scale,
                rect.height - (theme.DockPaddingTop + theme.DockPaddingBottom) * scale);

            if (contentRect.width <= 0f || contentRect.height <= 0f)
                return;

            float controlsWidth = Mathf.Min(theme.ControlsWidth * scale, Mathf.Max(0f, contentRect.width * 0.42f));
            Rect controlsRect = new Rect(contentRect.x, contentRect.y, controlsWidth, contentRect.height);
            float separatorX = controlsRect.xMax + theme.ControlsRightPadding * scale;
            Rect timelineRect = new Rect(
                separatorX + theme.DockColumnGap * scale,
                contentRect.y,
                Mathf.Max(0f, contentRect.xMax - separatorX - theme.DockColumnGap * scale),
                contentRect.height);

            DrawControls(controlsRect);

            drawList.AddLine(
                new Vector2(separatorX, contentRect.y + 1f * scale),
                new Vector2(separatorX, contentRect.yMax - 1f * scale),
                ColorU32(theme.Divider),
                Mathf.Max(1f, scale));

            DrawTimeline(timelineRect);
        }





        /// <summary>
        /// Draws the controls UI.
        /// </summary>
        public void DrawControls(float width)
        {
            float scale = Fugui.Scale;
            TimelineWidgetTheme theme = Theme;
            Vector2 pos = Fugui.GetCursorScreenPos();
            Rect rect = new Rect(pos.x, pos.y, width, Mathf.Max(theme.PlayButtonSize, theme.DockHeight - theme.DockPaddingTop - theme.DockPaddingBottom) * scale);
            DrawControls(rect);
            Fugui.SetCursorScreenPos(new Vector2(pos.x, rect.yMax));
        }

        /// <summary>
        /// Draws the controls UI.
        /// </summary>
        public void DrawControls(FuLayout layout)
        {
            float width = layout != null ? layout.GetAvailableWidth() : Theme.ControlsWidth * Fugui.Scale;
            DrawControls(width);
        }

        /// <summary>
        /// Draws the controls UI.
        /// </summary>
        public void DrawControls(Rect rect)
        {
            if (!CanReadTimeline() || rect.width <= 0f || rect.height <= 0f)
                return;

            TimelineWidgetTheme theme = Theme;
            FuDrawList drawList = Fugui.GetCurrentWindowDrawList();
            float scale = Fugui.Scale;
            bool canControlTimeline = CanControlTimeline();

            if (!canControlTimeline)
            {
                _speedMenuOpen = false;
                _buttonSeekDirection = 0;
                DrawReadOnlyControls(drawList, rect, theme, scale);
                return;
            }

            FlightPhase currentPhase = UpdateCurrentPhaseIndex();
            string timeText = ClockText;
            string modeLabel = ClockModeLabel;

            Fugui.PushFont(Mathf.RoundToInt(theme.TimeFontSize));
            Vector2 timeSize = Fugui.CalcTextSize(timeText);
            Fugui.PopFont();

            float pillHeight = theme.ModePillHeight * scale;
            float pillGap = 10f * scale;
            float modePillWidth = Mathf.Max(34f * scale, GetTextWidth(modeLabel, 10, true) + theme.ModePillPaddingX * 2f * scale);
            float speedPillWidth = 38f * scale;
            float timeRowWidth = timeSize.x + pillGap + modePillWidth + pillGap + speedPillWidth;
            float playSize = theme.PlayButtonSize * scale;
            float timeRowHeight = Mathf.Max(timeSize.y, pillHeight);
            float currentPhaseHeight = Mathf.Max(12f * scale, theme.CurrentPhaseHeight * scale);
            float currentPhaseGap = Mathf.Max(0f, theme.CurrentPhaseGap * scale);
            float totalHeight = timeRowHeight + currentPhaseGap + currentPhaseHeight + currentPhaseGap + playSize;
            float startY = rect.y + Mathf.Max(0f, (rect.height - totalHeight) * 0.5f);
            float rowX = rect.x + Mathf.Max(0f, (rect.width - timeRowWidth) * 0.5f);
            float timeY = startY + Mathf.Max(0f, (pillHeight - timeSize.y) * 0.5f);

            Fugui.PushFont(Mathf.RoundToInt(theme.TimeFontSize));
            drawList.AddText(new Vector2(rowX, timeY), ColorU32(theme.Text), timeText);
            Fugui.PopFont();

            Rect modeRect = new Rect(rowX + timeSize.x + pillGap, startY, modePillWidth, pillHeight);
            if (DrawPillButton(drawList, modeRect, modeLabel, "timelineMode", false))
            {
                ToggleClockMode();
                _speedMenuOpen = false;
            }

            Rect speedRect = new Rect(modeRect.xMax + pillGap, startY, speedPillWidth, pillHeight);
            if (canControlTimeline && DrawPillButton(drawList, speedRect, GetCurrentPlaybackSpeedLabel(), "timelineSpeed", _speedMenuOpen))
            {
                _speedMenuOpen = !_speedMenuOpen;
                _speedMenuOpenedThisFrame = _speedMenuOpen;
            }

            float currentPhaseY = startY + timeRowHeight + currentPhaseGap;
            DrawCurrentPhasePill(drawList, new Rect(rect.x, currentPhaseY, rect.width, currentPhaseHeight), currentPhase, theme, scale);

            float phaseSize = theme.PhaseButtonSize * scale;
            float seekSize = theme.SeekButtonSize * scale;
            float gap = theme.TransportGap * scale;
            float controlsWidth = phaseSize * 2f + seekSize * 2f + playSize + gap * 4f;
            float controlsX = rect.x + Mathf.Max(0f, (rect.width - controlsWidth) * 0.5f);
            float controlsY = currentPhaseY + currentPhaseHeight + currentPhaseGap;
            float phaseY = controlsY + (playSize - phaseSize) * 0.5f;
            float seekY = controlsY + (playSize - seekSize) * 0.5f;

            Rect prevPhaseRect = new Rect(controlsX, phaseY, phaseSize, phaseSize);
            Rect backRect = new Rect(prevPhaseRect.xMax + gap, seekY, seekSize, seekSize);
            Rect playRect = new Rect(backRect.xMax + gap, controlsY, playSize, playSize);
            Rect forwardRect = new Rect(playRect.xMax + gap, seekY, seekSize, seekSize);
            Rect nextPhaseRect = new Rect(forwardRect.xMax + gap, phaseY, phaseSize, phaseSize);

            float normalizedTime = Mathf.Clamp01(Sara.Time.GetNormalized());
            bool canInteract = !_speedMenuOpen && canControlTimeline;
            bool backPressed;
            bool forwardPressed;

            if (DrawGhostIconButton(drawList, prevPhaseRect, Icons.AnglesLeft_light, "timelinePrevPhase", _currentPhaseIndex > 0 && canInteract, 18f))
                PreviousPhase();

            if (DrawSeekButton(drawList, backRect, Icons.RotateLeft_light, "timelineBack", normalizedTime > 0.001f && canInteract, out backPressed, GetShortcutTooltip(SaraShortcutAction.TimelineBack10)))
            {
                SeekSeconds(-SeekStepSeconds);
                TriggerSeekFeedback(-1, false);
            }

            if (DrawPlayButton(drawList, playRect, Sara.Time.IsPlaying, canInteract, GetShortcutTooltip(SaraShortcutAction.TimelinePlayPause)))
            {
                SetTimelinePlaying(!Sara.Time.IsPlaying);
            }

            if (DrawSeekButton(drawList, forwardRect, Icons.RotateRight_light, "timelineForward", normalizedTime < 0.999f && canInteract, out forwardPressed, GetShortcutTooltip(SaraShortcutAction.TimelineForward10)))
            {
                SeekSeconds(SeekStepSeconds);
                TriggerSeekFeedback(1, false);
            }

            if (DrawGhostIconButton(drawList, nextPhaseRect, Icons.AnglesRight_light, "timelineNextPhase", _currentPhaseIndex < Sara.Flight.PhasesCount - 1 && canInteract, 18f))
                NextPhase();

            UpdateButtonHoldSeek(backPressed ? -1 : forwardPressed ? 1 : 0);
            DrawSpeedMenu(drawList, speedRect, rect);
        }





        /// <summary>
        /// Draws the timeline UI.
        /// </summary>
        public void DrawTimeline(float width)
        {
            TimelineWidgetTheme theme = Theme;
            float scale = Fugui.Scale;
            Vector2 pos = Fugui.GetCursorScreenPos();
            Rect rect = new Rect(pos.x, pos.y, width, Mathf.Max(1f, theme.DockHeight - theme.DockPaddingTop - theme.DockPaddingBottom) * scale);
            DrawTimeline(rect);
            Fugui.SetCursorScreenPos(new Vector2(pos.x, rect.yMax));
        }

        /// <summary>
        /// Draws the timeline UI.
        /// </summary>
        public void DrawTimeline(Rect rect)
        {
            if (!CanReadTimeline() || rect.width <= 0f || rect.height <= 0f)
                return;

            if (!CanControlTimeline())
                _openEventClusterHash = 0;

            EnsureTimelineEventsPopulated();
            UpdateCurrentPhaseIndex();

            TimelineWidgetTheme theme = Theme;
            float scale = Fugui.Scale;
            Rect contentRect = new Rect(
                rect.x + theme.TimelineLeftPadding * scale,
                rect.y,
                Mathf.Max(1f, rect.width - theme.TimelineLeftPadding * scale),
                rect.height);
            Rect phasesRect = new Rect(contentRect.x, contentRect.y, contentRect.width, theme.PhaseRowHeight * scale);
            float scrubberHeight = theme.ScrubberHeight * scale;
            float desiredScrubberCenterY = phasesRect.yMax + 4f * scale + theme.ScrubberTopPadding * scale + scrubberHeight * 0.5f;
            float eventBottomSpace = (Mathf.Max(theme.EventPillTop, theme.EventClusterPillTop) + theme.EventPillLaneGap + theme.EventPillHeight) * scale;
            float contentBottom = rect.yMax + theme.DockPaddingBottom * scale - 2f * scale;
            float maxScrubberCenterY = contentBottom - eventBottomSpace;
            float minScrubberCenterY = phasesRect.yMax + Mathf.Max(2f * scale, theme.TrackHeight * 0.5f * scale);
            float scrubberCenterY = desiredScrubberCenterY;

            if (maxScrubberCenterY >= minScrubberCenterY)
                scrubberCenterY = Mathf.Clamp(desiredScrubberCenterY, minScrubberCenterY, maxScrubberCenterY);
            else
                scrubberCenterY = minScrubberCenterY;

            Rect scrubberRect = new Rect(
                contentRect.x,
                scrubberCenterY - scrubberHeight * 0.5f,
                contentRect.width,
                scrubberHeight);

            Vector2 clipMin = new Vector2(rect.x, rect.y - theme.DockPaddingTop * scale);
            Vector2 clipMax = new Vector2(rect.xMax, rect.yMax + theme.DockPaddingBottom * scale);
            Fugui.PushClipRect(clipMin, clipMax, true);
            DrawPhaseRow(phasesRect);
            DrawScrubber(scrubberRect);
            Fugui.PopClipRect();
        }



        /// <summary>
        /// Draws the phase row UI.
        /// </summary>
        private void DrawPhaseRow(Rect rect)
        {
            int phaseCount = Sara.Flight.PhasesCount;
            if (phaseCount <= 0)
                return;

            TimelineWidgetTheme theme = Theme;
            FuDrawList drawList = Fugui.GetCurrentWindowDrawList();
            float scale = Fugui.Scale;
            float gap = theme.PhaseGap * scale;
            float rounding = theme.SmallRadius * scale;
            float normalizedTime = Mathf.Clamp01(Sara.Time.GetNormalized());
            bool canControl = CanControlTimeline();

            for (int i = 0; i < phaseCount; i++)
            {
                FlightPhase phase = Sara.Flight.GetPhase(i);
                float phaseStart = Mathf.Clamp01(NormalizeTid(phase.TID));
                float phaseEnd = i < phaseCount - 1 ? Mathf.Clamp01(NormalizeTid(Sara.Flight.GetPhase(i + 1).TID)) : 1f;
                if (phaseEnd <= phaseStart)
                    continue;

                float xMin = rect.x + rect.width * phaseStart;
                float xMax = rect.x + rect.width * phaseEnd;
                Rect phaseRect = new Rect(xMin + gap * 0.5f, rect.y, Mathf.Max(1f, xMax - xMin - gap), rect.height);
                bool active = i == _currentPhaseIndex;
                bool hovered = canControl && IsHovered(phaseRect);
                TimelinePhaseTheme phaseStyle = theme.GetPhaseStyle(phase.Name);
                Color bg = active ? phaseStyle.ActiveBackground : phaseStyle.Background;
                Color text = active ? theme.Text : phaseStyle.Text;

                drawList.AddRectFilled(phaseRect.min, phaseRect.max, ColorU32(bg), rounding);

                if (normalizedTime > phaseStart)
                {
                    float progress = phaseEnd <= phaseStart ? 0f : Mathf.Clamp01((normalizedTime - phaseStart) / (phaseEnd - phaseStart));
                    if (progress > 0f)
                    {
                        Rect progressRect = new Rect(phaseRect.x, phaseRect.y, phaseRect.width * progress, phaseRect.height);
                        FuDrawFlags progressFlags = GetPhaseProgressRoundingFlags(progress);
                        drawList.AddRectFilled(
                            progressRect.min,
                            progressRect.max,
                            ColorU32(WithAlpha(phaseStyle.Text, active ? 0.22f : 0.14f)),
                            rounding,
                            progressFlags);
                    }
                }

                if (active)
                {
                    drawList.AddRect(phaseRect.min, phaseRect.max, ColorU32(WithAlpha(theme.Text, 0.40f)), rounding, FuDrawFlags.None, 1.5f * scale);
                    drawList.AddRect(
                        phaseRect.min - new Vector2(2f * scale, 2f * scale),
                        phaseRect.max + new Vector2(2f * scale, 2f * scale),
                        ColorU32(WithAlpha(theme.Text, 0.10f)),
                        rounding + 2f * scale,
                        FuDrawFlags.None,
                        2f * scale);
                }
                else if (hovered)
                {
                    drawList.AddRect(phaseRect.min, phaseRect.max, ColorU32(WithAlpha(phaseStyle.Text, 0.55f)), rounding);
                }

                if (hovered)
                {
                    Fugui.SetMouseCursor(FuMouseCursor.Hand);
                    if (Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left))
                        SeekTimelineTid(phase.TID);
                }

                if (phaseRect.width < 18f * scale)
                    continue;

                PushCompactFont(Mathf.RoundToInt(theme.PhaseTextSize));
                string label = ClipTextToWidth(GetPhaseLabel(phase.Name), Mathf.Max(1f, phaseRect.width - 8f * scale));
                DrawTextCentered(drawList, phaseRect, label, ColorU32(text));
                PopCompactFont();
            }
        }

        /// <summary>
        /// Returns the corner rounding flags used by the phase progress fill.
        /// </summary>
        private static FuDrawFlags GetPhaseProgressRoundingFlags(float progress)
        {
            // Avoid per-phase clip rects so ImGui can keep phase geometry in the same draw command.
            return progress >= 0.999f ? FuDrawFlags.RoundCornersAll : FuDrawFlags.RoundCornersLeft;
        }



        /// <summary>
        /// Draws the scrubber UI.
        /// </summary>
        private void DrawScrubber(Rect rect)
        {
            TimelineWidgetTheme theme = Theme;
            FuDrawList drawList = Fugui.GetCurrentWindowDrawList();
            float scale = Fugui.Scale;
            float trackHeight = Mathf.Max(2f, theme.TrackHeight * scale);
            float trackY = rect.y + (rect.height - trackHeight) * 0.5f;
            Rect trackRect = new Rect(rect.x, trackY, rect.width, trackHeight);
            Rect hitRect = new Rect(rect.x, rect.y - 8f * scale, rect.width, rect.height + 20f * scale);
            FuMouseState mouse = Fugui.GetCurrentMouse();
            bool canControl = CanControlTimeline();
            bool hovered = canControl && IsHovered(hitRect);
            bool eventHovered = canControl && IsMouseOverTimelineEvent(trackRect);

            if (canControl && hovered && !eventHovered && mouse.IsDown(FuMouseButton.Left))
                _timelineDragging = true;

            if (_timelineDragging)
            {
                if (canControl && mouse.IsPressed(FuMouseButton.Left))
                    SeekTimelineFromMouse(trackRect);

                if (!canControl || mouse.IsUp(FuMouseButton.Left))
                    _timelineDragging = false;
            }

            if (canControl && (hovered || _timelineDragging))
                Fugui.SetMouseCursor(FuMouseCursor.ResizeEW);

            float normalized = Mathf.Clamp01(Sara.Time.GetNormalized());
            float currentX = trackRect.x + trackRect.width * normalized;
            float rounding = trackRect.height * 0.5f;

            drawList.AddRectFilled(trackRect.min, trackRect.max, ColorU32(theme.Track), rounding);
            if (currentX > trackRect.x)
            {
                drawList.AddRectFilled(
                    new Vector2(trackRect.x, trackRect.y - 1f * scale),
                    new Vector2(currentX, trackRect.yMax + 1f * scale),
                    ColorU32(WithAlpha(theme.AccentGlow, 0.20f)),
                    rounding + 1f * scale);
                drawList.AddRectFilled(trackRect.min, new Vector2(currentX, trackRect.yMax), ColorU32(theme.Accent), rounding);
            }

            DrawTimelineEvents(trackRect, out EventPopoverRequest eventPopoverRequest);

            Vector2 playheadCenter = new Vector2(currentX, trackRect.y + trackRect.height * 0.5f);
            float playheadRadius = theme.PlayheadSize * 0.5f * scale;
            drawList.AddCircleFilled(playheadCenter + new Vector2(0f, 2f * scale), playheadRadius + 1f * scale, ColorU32(WithAlpha(Color.black, 0.35f)), 32);
            drawList.AddCircleFilled(playheadCenter, playheadRadius, ColorU32(theme.Playhead), 32);
            drawList.AddCircle(playheadCenter, playheadRadius, ColorU32(theme.Accent), 32, 3f * scale);

            DrawEventPopoverLayer(drawList, eventPopoverRequest, theme, scale);
        }





        #endregion

        #region Timeline Event Rendering
        /// <summary>
        /// Draws the timeline events UI.
        /// </summary>
        private void DrawTimelineEvents(Rect trackRect, out EventPopoverRequest popoverRequest)
        {
            popoverRequest = default;
            if (_events.Count == 0)
                return;

            TimelineWidgetTheme theme = Theme;
            FuDrawList drawList = Fugui.GetCurrentWindowDrawList();
            float scale = Fugui.Scale;
            List<EventGroup> groups = BuildEventGroups(theme);
            bool clickedInsideEvent = false;
            bool clicked = !InputBlocked && Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left);
            bool canControl = CanControlTimeline();
            EventGroup openPopoverGroup = null;
            EventVisual openPopoverVisual = default;
            Color openPopoverColor = Color.clear;

            for (int i = 0; i < groups.Count; i++)
            {
                EventGroup group = groups[i];
                EventVisual visual = GetEventVisual(group, i, trackRect, theme, scale);
                Color eventColor = theme.GetEventColor(group.Tone);
                bool dotHovered = canControl && IsHovered(visual.DotHitRect);
                bool pillHovered = canControl && !visual.IsClusterOpen && IsHovered(visual.PillRect);
                bool popoverHovered = canControl && visual.IsClusterOpen && visual.PopoverRect.HasValue && IsHovered(visual.PopoverRect.Value);
                bool hovered = dotHovered || pillHovered || popoverHovered;

                DrawEventDot(drawList, visual.DotCenter, group, eventColor, theme, scale, dotHovered || visual.IsClusterOpen);

                if (!visual.IsClusterOpen)
                    DrawEventPill(drawList, visual, group, eventColor, theme, scale, hovered);

                if (hovered)
                {
                    Fugui.SetMouseCursor(FuMouseCursor.Hand);
                }

                if (clicked && canControl && (dotHovered || pillHovered))
                {
                    clickedInsideEvent = true;
                    if (group.IsCluster)
                        _openEventClusterHash = _openEventClusterHash == group.Hash ? 0 : group.Hash;
                    else
                        SeekTimelineTid(group.Events[0].Tid);
                }

                if (visual.IsClusterOpen && visual.PopoverRect.HasValue && _openEventClusterHash == group.Hash)
                {
                    openPopoverGroup = group;
                    openPopoverVisual = visual;
                    openPopoverColor = eventColor;
                }
            }

            if (openPopoverGroup != null && openPopoverVisual.PopoverRect.HasValue)
            {
                popoverRequest = new EventPopoverRequest
                {
                    HasValue = true,
                    Rect = openPopoverVisual.PopoverRect.Value,
                    Group = openPopoverGroup,
                    EventColor = openPopoverColor,
                    ClickedInsideEvent = clickedInsideEvent,
                    MouseClicked = clicked
                };
                return;
            }

            if (clicked && !clickedInsideEvent && _openEventClusterHash != 0)
                _openEventClusterHash = 0;
        }

        /// <summary>
        /// Draws the event popover layer UI.
        /// </summary>
        private void DrawEventPopoverLayer(FuDrawList drawList, EventPopoverRequest request, TimelineWidgetTheme theme, float scale)
        {
            if (!request.HasValue)
                return;

            bool clickedInsideEvent = request.ClickedInsideEvent;
            if (DrawEventPopover(drawList, request.Rect, request.Group, request.EventColor, theme, scale, request.MouseClicked))
                clickedInsideEvent = true;

            if (request.MouseClicked && !clickedInsideEvent && _openEventClusterHash != 0)
                _openEventClusterHash = 0;
        }

        /// <summary>
        /// Returns whether the mouse over timeline event condition is met.
        /// </summary>
        private bool IsMouseOverTimelineEvent(Rect trackRect)
        {
            if (_events.Count == 0 || !CanControlTimeline())
                return false;

            TimelineWidgetTheme theme = Theme;
            float scale = Fugui.Scale;
            List<EventGroup> groups = BuildEventGroups(theme);
            for (int i = 0; i < groups.Count; i++)
            {
                EventVisual visual = GetEventVisual(groups[i], i, trackRect, theme, scale);
                if (IsHovered(visual.DotHitRect) || IsHovered(visual.PillRect))
                    return true;

                if (visual.IsClusterOpen && visual.PopoverRect.HasValue && IsHovered(visual.PopoverRect.Value))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Builds the event groups data.
        /// </summary>
        private List<EventGroup> BuildEventGroups(TimelineWidgetTheme theme)
        {
            List<TimelineEvent> sortedEvents = new List<TimelineEvent>(_events);
            sortedEvents.Sort((left, right) => NormalizeTid(left.Tid).CompareTo(NormalizeTid(right.Tid)));

            List<EventGroup> groups = new List<EventGroup>();
            float threshold = Mathf.Max(0f, theme.EventClusterThresholdPercent) / 100f;
            EventGroup current = null;

            for (int i = 0; i < sortedEvents.Count; i++)
            {
                TimelineEvent timelineEvent = sortedEvents[i];
                float normalized = Mathf.Clamp01(NormalizeTid(timelineEvent.Tid));
                if (current == null || normalized - current.EndNormalized > threshold)
                {
                    current = new EventGroup();
                    current.StartNormalized = normalized;
                    current.EndNormalized = normalized;
                    groups.Add(current);
                }

                current.Events.Add(timelineEvent);
                current.EndNormalized = normalized;
            }

            for (int i = 0; i < groups.Count; i++)
            {
                EventGroup group = groups[i];
                group.CenterNormalized = (group.StartNormalized + group.EndNormalized) * 0.5f;
                group.Tone = GetDominantSeverity(group.Events);
                group.Hash = GetEventGroupHash(group);
            }

            return groups;
        }

        /// <summary>
        /// Returns the event visual value.
        /// </summary>
        private EventVisual GetEventVisual(EventGroup group, int groupIndex, Rect trackRect, TimelineWidgetTheme theme, float scale)
        {
            float trackCenterY = trackRect.y + trackRect.height * 0.5f;
            float x = trackRect.x + trackRect.width * group.CenterNormalized;
            int lane = groupIndex % 2;
            bool isClusterOpen = group.IsCluster && _openEventClusterHash == group.Hash;
            float dotSize = group.IsCluster ? theme.EventClusterDotSize : theme.EventDotSize;
            float dotRadius = dotSize * 0.5f * scale;
            Vector2 dotCenter = new Vector2(x, trackCenterY);
            float hitRadius = Mathf.Max(14f * scale, (dotSize + theme.EventClusterOuterRingSize * 2f) * 0.5f * scale);

            float pillWidth = GetEventPillWidth(group, theme, scale);
            float pillTop = group.IsCluster ? theme.EventClusterPillTop : theme.EventPillTop;
            float pillX = Mathf.Clamp(x - pillWidth * 0.5f, trackRect.x, trackRect.xMax - pillWidth);
            float pillY = trackCenterY + (pillTop + lane * theme.EventPillLaneGap) * scale;
            Rect pillRect = new Rect(pillX, pillY, pillWidth, theme.EventPillHeight * scale);
            Rect dotHitRect = new Rect(x - hitRadius, trackCenterY - hitRadius, hitRadius * 2f, hitRadius * 2f);

            Rect? popoverRect = null;
            if (isClusterOpen)
                popoverRect = GetEventPopoverRect(group, trackRect, x, theme, scale);

            return new EventVisual
            {
                DotCenter = dotCenter,
                DotRadius = dotRadius,
                DotHitRect = dotHitRect,
                PillRect = pillRect,
                PopoverRect = popoverRect,
                IsClusterOpen = isClusterOpen
            };
        }

        /// <summary>
        /// Returns the event pill width value.
        /// </summary>
        private float GetEventPillWidth(EventGroup group, TimelineWidgetTheme theme, float scale)
        {
            float arrowWidth = 10f * scale;
            float width;
            PushCompactFont(Mathf.RoundToInt(theme.EventTextSize));
            if (group.IsCluster)
            {
                string label = group.Events.Count + " events";
                float badgeWidth = Mathf.Max(16f * scale, Fugui.CalcTextSize(group.Events.Count.ToString()).x + 10f * scale);
                width = badgeWidth + 4f * scale + Fugui.CalcTextSize(label).x + arrowWidth + theme.EventPillPaddingX * 2f * scale + 6f * scale;
            }
            else
            {
                width = Fugui.CalcTextSize(GetEventLabel(group.Events[0])).x + arrowWidth + theme.EventPillPaddingX * 2f * scale + 8f * scale;
            }
            PopCompactFont();

            return Mathf.Clamp(width, 44f * scale, group.IsCluster ? 126f * scale : 172f * scale);
        }

        /// <summary>
        /// Returns the event popover rect value.
        /// </summary>
        private Rect GetEventPopoverRect(EventGroup group, Rect trackRect, float anchorX, TimelineWidgetTheme theme, float scale)
        {
            int maxRows = Mathf.Max(1, theme.EventPopoverMaxRows);
            int rows = Mathf.Min(maxRows, group.Events.Count);
            int columns = Mathf.CeilToInt(group.Events.Count / (float)maxRows);
            float columnWidth = theme.EventPopoverWidth * scale;
            float padding = theme.EventPopoverPadding * scale;
            float gap = theme.EventPopoverGap * scale;
            float width = padding * 2f + columns * columnWidth + Mathf.Max(0, columns - 1) * gap;
            float height = padding * 2f + rows * theme.EventPopoverRowHeight * scale + Mathf.Max(0, rows - 1) * scale;
            float bottom = trackRect.y + trackRect.height * 0.5f + theme.EventPopoverBottomOffset * scale;

            float x;
            if (group.CenterNormalized > 0.8f)
                x = anchorX - width;
            else if (group.CenterNormalized < 0.2f)
                x = anchorX;
            else
                x = anchorX - width * 0.5f;

            x = Mathf.Clamp(x, trackRect.x, trackRect.xMax - width);
            return new Rect(x, bottom - height, width, height);
        }

        /// <summary>
        /// Draws the event dot UI.
        /// </summary>
        private void DrawEventDot(FuDrawList drawList, Vector2 center, EventGroup group, Color eventColor, TimelineWidgetTheme theme, float scale, bool highlighted)
        {
            bool cluster = group.IsCluster;
            float baseRadius = (cluster ? theme.EventClusterDotSize : theme.EventDotSize) * 0.5f * scale;
            float radius = baseRadius * (highlighted ? theme.EventDotHoverScale : 1f);
            float ring = theme.EventDotRingSize * scale;
            float glow = theme.EventDotGlowSize * scale;

            drawList.AddCircleFilled(center, radius + glow * 0.5f, ColorU32(WithAlpha(eventColor, highlighted ? 0.22f : 0.16f)), 32);

            if (cluster)
            {
                drawList.AddCircleFilled(center, radius + theme.EventClusterOuterRingSize * scale, ColorU32(eventColor), 32);
                drawList.AddCircleFilled(center, radius + ring, ColorU32(WithAlpha(theme.DockBackground, 0.95f)), 32);
                drawList.AddCircleFilled(center, radius, ColorU32(WithAlpha(theme.DockBackground, 0.85f)), 32);

                Fugui.PushFont(10);
                DrawTextCenteredScaled(drawList, new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f), group.Events.Count.ToString(), ColorU32(eventColor), 0.86f);
                Fugui.PopFont();
            }
            else
            {
                drawList.AddCircleFilled(center, radius + ring, ColorU32(WithAlpha(theme.DockBackground, 0.95f)), 32);
                drawList.AddCircleFilled(center, radius, ColorU32(eventColor), 32);
            }
        }

        /// <summary>
        /// Draws the event pill UI.
        /// </summary>
        private void DrawEventPill(FuDrawList drawList, EventVisual visual, EventGroup group, Color eventColor, TimelineWidgetTheme theme, float scale, bool hovered)
        {
            Rect pillRect = visual.PillRect;
            float arrowWidth = 10f * scale;

            drawList.AddLine(
                visual.DotCenter + new Vector2(0f, visual.DotRadius + 2f * scale),
                new Vector2(visual.DotCenter.x, pillRect.y),
                ColorU32(WithAlpha(eventColor, theme.EventConnectorAlpha)),
                Mathf.Max(1f, scale));

            Color pillBackground = WithAlpha(theme.DockBackground, hovered ? theme.EventPillBackgroundHoverAlpha : theme.EventPillBackgroundAlpha);
            drawList.AddRectFilled(pillRect.min, pillRect.max, ColorU32(pillBackground), pillRect.height * 0.5f);
            drawList.AddRect(pillRect.min, pillRect.max, ColorU32(eventColor), pillRect.height * 0.5f);
            FlatCameraInputBlocker.RegisterRect(pillRect);
            FlatCameraInputBlocker.RegisterRect(visual.DotHitRect);

            PushCompactFont(Mathf.RoundToInt(theme.EventTextSize));
            if (group.IsCluster)
            {
                string countText = group.Events.Count.ToString();
                float badgeWidth = Mathf.Max(16f * scale, Fugui.CalcTextSize(countText).x + 10f * scale);
                Rect badgeRect = new Rect(pillRect.x + 6f * scale, pillRect.y + (pillRect.height - 16f * scale) * 0.5f, badgeWidth, 16f * scale);
                drawList.AddRectFilled(badgeRect.min, badgeRect.max, ColorU32(eventColor), badgeRect.height * 0.5f);
                DrawTextCentered(drawList, badgeRect, countText, ColorU32(WithAlpha(theme.DockBackground, 0.95f)));

                Rect labelRect = new Rect(badgeRect.xMax + 4f * scale, pillRect.y, Mathf.Max(1f, pillRect.xMax - badgeRect.xMax - arrowWidth - theme.EventPillPaddingX * scale), pillRect.height);
                DrawTextLeftCentered(drawList, labelRect, group.Events.Count + " events", ColorU32(eventColor), 0f);
            }
            else
            {
                Rect textRect = new Rect(pillRect.x + theme.EventPillPaddingX * scale, pillRect.y, Mathf.Max(1f, pillRect.width - arrowWidth - theme.EventPillPaddingX * 2f * scale), pillRect.height);
                DrawTextLeftCentered(drawList, textRect, ClipTextToWidth(GetEventLabel(group.Events[0]), textRect.width), ColorU32(eventColor), 0f);
            }
            PopCompactFont();

            Rect arrowRect = new Rect(pillRect.xMax - arrowWidth - theme.EventPillPaddingX * scale * 0.5f, pillRect.y, arrowWidth, pillRect.height);
            Fugui.PushFont(10);
            DrawIconCenteredTinted(drawList, arrowRect, Icons.AngleRight_solid, ColorU32(WithAlpha(eventColor, 0.75f)), ColorU32(eventColor), ColorU32(eventColor));
            Fugui.PopFont();
        }

        /// <summary>
        /// Draws the event popover UI.
        /// </summary>
        private bool DrawEventPopover(FuDrawList drawList, Rect rect, EventGroup group, Color eventColor, TimelineWidgetTheme theme, float scale, bool mouseClicked)
        {
            bool canControl = CanControlTimeline();
            bool hoveredPopover = canControl && IsHovered(rect);
            FlatCameraInputBlocker.RegisterRect(rect);

            drawList.AddRectFilled(rect.min, rect.max, ColorU32(theme.EventPopoverBackground), theme.MediumRadius * scale);
            drawList.AddRect(rect.min, rect.max, ColorU32(WithAlpha(eventColor, theme.EventPopoverBorderAlpha)), theme.MediumRadius * scale);

            int maxRows = Mathf.Max(1, theme.EventPopoverMaxRows);
            int rows = Mathf.Min(maxRows, group.Events.Count);
            float padding = theme.EventPopoverPadding * scale;
            float rowHeight = theme.EventPopoverRowHeight * scale;
            float columnWidth = theme.EventPopoverWidth * scale;
            float gap = theme.EventPopoverGap * scale;

            for (int i = 0; i < group.Events.Count; i++)
            {
                TimelineEvent item = group.Events[i];
                int column = i / rows;
                int row = i % rows;
                Rect itemRect = new Rect(rect.x + padding + column * (columnWidth + gap), rect.y + padding + row * (rowHeight + scale), columnWidth, rowHeight);
                bool hovered = canControl && IsHovered(itemRect);
                bool active = hovered && Fugui.GetCurrentMouse().IsPressed(FuMouseButton.Left);
                Color itemBackground = active ? theme.EventPopoverItemActive : hovered ? theme.EventPopoverItemHover : Color.clear;
                Color itemColor = theme.GetEventColor(item.Tone);

                if (itemBackground.a > 0f)
                    drawList.AddRectFilled(itemRect.min, itemRect.max, ColorU32(itemBackground), theme.SmallRadius * scale);

                drawList.AddCircleFilled(new Vector2(itemRect.x + 12f * scale, itemRect.center.y), 3.5f * scale, ColorU32(itemColor), 16);
                drawList.AddCircleFilled(new Vector2(itemRect.x + 12f * scale, itemRect.center.y), 6f * scale, ColorU32(WithAlpha(itemColor, 0.16f)), 16);

                PushFont(12, false);
                string timeText = FormatEventTime(item.Tid);
                float timeWidth = GetTextWidth(timeText, 10, false);
                Rect labelRect = new Rect(itemRect.x + 24f * scale, itemRect.y, Mathf.Max(1f, itemRect.width - 34f * scale - timeWidth), itemRect.height);
                DrawTextLeftCentered(drawList, labelRect, ClipTextToWidth(GetEventLabel(item), labelRect.width), ColorU32(theme.Text), 0f);
                PopFont(false);

                PushFont(10, false);
                Rect timeRect = new Rect(itemRect.xMax - timeWidth - 9f * scale, itemRect.y, timeWidth, itemRect.height);
                DrawTextLeftCentered(drawList, timeRect, timeText, ColorU32(theme.TextFaint), 0f);
                PopFont(false);

                if (hovered)
                {
                    Fugui.SetMouseCursor(FuMouseCursor.Hand);
                    if (mouseClicked)
                    {
                        SeekTimelineTid(item.Tid);
                        _openEventClusterHash = 0;
                    }
                }
            }

            return hoveredPopover;
        }

        /// <summary>
        /// Returns the dominant severity value.
        /// </summary>
        private static EventSeverity GetDominantSeverity(List<TimelineEvent> events)
        {
            EventSeverity severity = EventSeverity.None;
            for (int i = 0; i < events.Count; i++)
            {
                if ((int)events[i].Tone > (int)severity)
                    severity = events[i].Tone;
            }

            return severity;
        }

        /// <summary>
        /// Returns the event group hash value.
        /// </summary>
        private static int GetEventGroupHash(EventGroup group)
        {
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < group.Events.Count; i++)
                {
                    TimelineEvent timelineEvent = group.Events[i];
                    hash = hash * 31 + timelineEvent.Tid.Value.GetHashCode();
                    hash = hash * 31 + GetEventLabel(timelineEvent).GetHashCode();
                    hash = hash * 31 + timelineEvent.Tone.GetHashCode();
                }

                return hash != 0 ? hash : 1;
            }
        }

        /// <summary>
        /// Formats the event time value for display.
        /// </summary>
        private static string FormatEventTime(TID tid)
        {
            if (!CanReadTimeline())
                return "--:--:--";

            double seconds = (tid - Sara.Time.TidZero) / Sara.Time.TidPerSecond;
            return FormatFlightClock(seconds);
        }

        /// <summary>
        /// Returns the event label value.
        /// </summary>
        private static string GetEventLabel(TimelineEvent timelineEvent)
        {
            return string.IsNullOrEmpty(timelineEvent.Label) ? "Event" : timelineEvent.Label;
        }



        /// <summary>
        /// Draws the current phase pill UI.
        /// </summary>
        private void DrawCurrentPhasePill(FuDrawList drawList, Rect rowRect, FlightPhase currentPhase, TimelineWidgetTheme theme, float scale)
        {
            string phaseName = currentPhase != null ? currentPhase.Name : null;
            string label = GetCurrentPhasePillLabel(phaseName);

            PushCompactFont(Mathf.RoundToInt(theme.CurrentPhaseTextSize));
            float padding = theme.CurrentPhasePaddingX * scale;
            float dotRadius = Mathf.Clamp(rowRect.height * 0.30f, 3f * scale, 5f * scale);
            float dotGap = 7f * scale;
            float maxWidth = Mathf.Max(1f, Mathf.Min(rowRect.width, theme.CurrentPhaseMaxWidth * scale));
            float minWidth = Mathf.Min(66f * scale, maxWidth);
            float width = Mathf.Clamp(Fugui.CalcTextSize(label).x + padding * 2f + dotRadius * 2f + dotGap, minWidth, maxWidth);
            Rect rect = new Rect(rowRect.x + (rowRect.width - width) * 0.5f, rowRect.y, width, rowRect.height);
            Vector2 dotCenter = new Vector2(rect.x + padding + dotRadius, rect.center.y);
            Rect labelRect = new Rect(
                dotCenter.x + dotRadius + dotGap,
                rect.y,
                Mathf.Max(1f, rect.xMax - dotCenter.x - dotRadius - dotGap - padding),
                rect.height);
            string clippedLabel = ClipTextToWidth(label, labelRect.width);
            float rounding = rect.height * 0.5f;

            drawList.AddRectFilled(rect.min, rect.max, ColorU32(theme.CurrentPhaseBackground), rounding);
            drawList.AddRect(rect.min, rect.max, ColorU32(theme.CurrentPhaseBorder), rounding, FuDrawFlags.None, Mathf.Max(1f, scale));
            drawList.AddCircleFilled(dotCenter, dotRadius, ColorU32(theme.CurrentPhaseDot), 16);
            DrawTextLeftCentered(drawList, labelRect, clippedLabel, ColorU32(theme.Text), 0f);
            PopCompactFont();
        }



        /// <summary>
        /// Runs the are timeline events equal logic.
        /// </summary>
        private static bool AreTimelineEventsEqual(List<TimelineEvent> left, List<TimelineEvent> right)
        {
            if (left == null || right == null || left.Count != right.Count)
                return false;

            for (int i = 0; i < left.Count; i++)
            {
                TimelineEvent leftEvent = left[i];
                TimelineEvent rightEvent = right[i];
                if (!leftEvent.Tid.Value.Equals(rightEvent.Tid.Value)
                    || !string.Equals(leftEvent.Label, rightEvent.Label, StringComparison.Ordinal)
                    || leftEvent.Tone != rightEvent.Tone
                    || leftEvent.IsImportant != rightEvent.IsImportant)
                {
                    return false;
                }
            }

            return true;
        }





        #endregion

        #region Interactive Control Drawing
        /// <summary>
        /// Draws the read-only control replacement UI.
        /// </summary>
        private static void DrawReadOnlyControls(FuDrawList drawList, Rect rect, TimelineWidgetTheme theme, float scale)
        {
            FlatCameraInputBlocker.RegisterRect(rect);

            string text = "Timeline controled by " + GetTimelineControllerName();
            Rect messageRect = new Rect(
                rect.x + 8f * scale,
                rect.y,
                Mathf.Max(1f, rect.width - 16f * scale),
                rect.height);

            PushCompactFont(12);
            DrawTextCentered(drawList, messageRect, ClipTextToWidth(text, messageRect.width), ColorU32(theme.TextDim));
            PopCompactFont();
        }

        /// <summary>
        /// Draws the pill button UI.
        /// </summary>
        private bool DrawPillButton(FuDrawList drawList, Rect rect, string label, string id, bool selected)
        {
            TimelineWidgetTheme theme = Theme;
            float scale = Fugui.Scale;
            bool hovered = IsHovered(rect);
            bool active = hovered && Fugui.GetCurrentMouse().IsPressed(FuMouseButton.Left);
            bool clicked = hovered && Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left);
            Color fill = active ? theme.PillBackgroundActive : hovered || selected ? theme.PillBackgroundHover : theme.PillBackground;

            FlatCameraInputBlocker.RegisterRect(rect);
            drawList.AddRectFilled(rect.min, rect.max, ColorU32(fill), rect.height * 0.5f);

            PushCompactFont(10);
            DrawTextCentered(drawList, rect, ClipTextToWidth(label, rect.width - 8f * scale), ColorU32(theme.Text));
            PopCompactFont();

            if (hovered)
                Fugui.SetMouseCursor(FuMouseCursor.Hand);

            return clicked;
        }

        /// <summary>
        /// Draws the ghost icon button UI.
        /// </summary>
        private bool DrawGhostIconButton(FuDrawList drawList, Rect rect, string icon, string id, bool enabled, float iconSize)
        {
            TimelineWidgetTheme theme = Theme;
            float scale = Fugui.Scale;
            bool hovered = enabled && IsHovered(rect);
            bool active = hovered && Fugui.GetCurrentMouse().IsPressed(FuMouseButton.Left);
            bool clicked = hovered && Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left);
            Color color = enabled ? hovered || active ? theme.Text : theme.TextFaint : WithAlpha(theme.TextFaint, 0.30f);

            FlatCameraInputBlocker.RegisterRect(rect);
            if (hovered || active)
                drawList.AddCircleFilled(rect.center, Mathf.Min(rect.width, rect.height) * 0.5f, ColorU32(active ? theme.ControlCircleActive : theme.ControlCircleHover), 24);

            Fugui.PushFont(Mathf.RoundToInt(iconSize));
            DrawIconCenteredTinted(drawList, rect, icon, ColorU32(color), ColorU32(color), ColorU32(color));
            Fugui.PopFont();

            if (hovered)
                Fugui.SetMouseCursor(FuMouseCursor.Hand);

            return clicked;
        }

        /// <summary>
        /// Draws the seek button UI.
        /// </summary>
        private bool DrawSeekButton(FuDrawList drawList, Rect rect, string icon, string id, bool enabled, out bool pressed, string tooltip)
        {
            TimelineWidgetTheme theme = Theme;
            bool hovered = enabled && IsHovered(rect);
            pressed = hovered && Fugui.GetCurrentMouse().IsPressed(FuMouseButton.Left);
            bool clicked = hovered && Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left);
            Color color = enabled ? hovered || pressed ? theme.Text : theme.TextDim : WithAlpha(theme.TextFaint, 0.35f);

            FlatCameraInputBlocker.RegisterRect(rect);
            if (hovered || pressed)
                drawList.AddCircleFilled(rect.center, Mathf.Min(rect.width, rect.height) * 0.5f, ColorU32(pressed ? theme.ControlCircleActive : theme.ControlCircleHover), 24);

            Fugui.PushFont(20);
            DrawIconCenteredTinted(drawList, rect, icon, ColorU32(color), ColorU32(color), ColorU32(color));
            Fugui.PopFont();

            Fugui.PushFont(10);
            DrawTextCenteredScaled(drawList, rect, "10", ColorU32(color), 0.72f);
            Fugui.PopFont();

            if (hovered)
            {
                Fugui.SetMouseCursor(FuMouseCursor.Hand);
                DrawShortcutTooltip(id, tooltip);
            }

            return clicked;
        }

        /// <summary>
        /// Draws the play button UI.
        /// </summary>
        private bool DrawPlayButton(FuDrawList drawList, Rect rect, bool isPlaying, bool enabled, string tooltip)
        {
            TimelineWidgetTheme theme = Theme;
            float scale = Fugui.Scale;
            bool hovered = enabled && IsHovered(rect);
            bool active = hovered && Fugui.GetCurrentMouse().IsPressed(FuMouseButton.Left);
            bool clicked = hovered && Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left);
            Color fill = enabled ? active ? theme.AccentHi : theme.Accent : WithAlpha(theme.Accent, 0.45f);

            FlatCameraInputBlocker.RegisterRect(rect);
            drawList.AddCircleFilled(rect.center, rect.width * 0.5f, ColorU32(fill), 32);
            drawList.AddCircle(rect.center, rect.width * 0.5f, ColorU32(WithAlpha(theme.Text, 0.12f)), 32, Mathf.Max(1f, scale));

            Fugui.PushFont(20);
            DrawIconCenteredTinted(drawList, rect, isPlaying ? Icons.Pause_solid : Icons.Play_solid, ColorU32(Color.white), ColorU32(Color.white), ColorU32(Color.white));
            Fugui.PopFont();

            if (hovered)
            {
                Fugui.SetMouseCursor(FuMouseCursor.Hand);
                DrawShortcutTooltip("timelinePlay", tooltip);
            }

            return clicked;
        }

        /// <summary>
        /// Draws the speed menu UI.
        /// </summary>
        private void DrawSpeedMenu(FuDrawList drawList, Rect anchorRect, Rect bounds)
        {
            TimelineWidgetTheme theme = Theme;
            if (!CanControlTimeline())
                _speedMenuOpen = false;

            float step = Time.unscaledDeltaTime / Mathf.Max(0.001f, theme.SpeedPopupTransitionSeconds);
            _speedMenuAmount = Mathf.MoveTowards(_speedMenuAmount, _speedMenuOpen ? 1f : 0f, step);

            if (!_speedMenuOpen && _speedMenuAmount <= 0.001f)
                return;

            float scale = Fugui.Scale;
            float t = SmoothStep01(_speedMenuAmount);
            float width = 74f * scale;
            Rect windowBounds = GetCurrentWindowBounds(bounds);
            float margin = 4f * scale;
            float gap = 8f * scale;
            float requestedOptionHeight = SpeedMenuOptionHeight * scale;
            float availableHeight = Mathf.Max(1f, windowBounds.height - margin * 2f);
            float optionHeight = Mathf.Min(requestedOptionHeight, Mathf.Max(1f, (availableHeight - 10f * scale) / PlaybackSpeeds.Length));
            float height = optionHeight * PlaybackSpeeds.Length + 10f * scale;
            Rect targetRect = GetPopupRectInsideBounds(anchorRect, width, height, windowBounds, gap, margin);
            Rect closedRect = new Rect(
                anchorRect.center.x - Mathf.Max(anchorRect.width, width * 0.65f) * 0.5f,
                anchorRect.y - anchorRect.height * 0.15f,
                Mathf.Max(anchorRect.width, width * 0.65f),
                anchorRect.height * 0.65f);
            Rect menuRect = LerpRect(closedRect, targetRect, t);
            float rounding = theme.MediumRadius * scale;

            FlatCameraInputBlocker.RegisterRect(menuRect);
            drawList.AddRectFilled(menuRect.min, menuRect.max, ColorU32(theme.MenuBackground, t), rounding);
            drawList.AddRect(menuRect.min, menuRect.max, ColorU32(theme.DockBorder, t), rounding);

            Fugui.PushClipRect(menuRect.min, menuRect.max, true);
            for (int i = 0; i < PlaybackSpeeds.Length; i++)
            {
                Rect optionRect = new Rect(menuRect.x + 5f * scale, menuRect.y + 5f * scale + i * optionHeight, menuRect.width - 10f * scale, optionHeight);
                bool selected = Math.Abs(Sara.Time.Speed - PlaybackSpeeds[i]) <= 0.01;
                bool canInteract = _speedMenuOpen && _speedMenuAmount > 0.92f;
                bool hovered = canInteract && IsHovered(optionRect);
                bool active = hovered && Fugui.GetCurrentMouse().IsPressed(FuMouseButton.Left);
                Color bg = selected ? theme.PillBackgroundActive : active ? theme.PillBackgroundActive : hovered ? theme.PillBackgroundHover : Color.clear;

                if (bg.a > 0f)
                    drawList.AddRectFilled(optionRect.min, optionRect.max, ColorU32(bg, t), theme.SmallRadius * scale);

                PushCompactFont(12);
                DrawTextCentered(drawList, optionRect, PlaybackSpeedLabels[i], ColorU32(selected ? theme.Accent : theme.TextDim, t));
                PopCompactFont();

                if (hovered)
                {
                    Fugui.SetMouseCursor(FuMouseCursor.Hand);
                    if (Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left))
                    {
                        SetTimelineSpeed(PlaybackSpeeds[i]);
                        _speedMenuOpen = false;
                    }
                }
            }
            Fugui.PopClipRect();

            if (!_speedMenuOpenedThisFrame && _speedMenuOpen && Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left) && !IsHovered(menuRect) && !IsHovered(anchorRect))
                _speedMenuOpen = false;

            _speedMenuOpenedThisFrame = false;
        }





        /// <summary>
        /// Returns the current window bounds value.
        /// </summary>
        private static Rect GetCurrentWindowBounds(Rect fallbackBounds)
        {
            FuWindow window = FuWindow.CurrentDrawingWindow;
            if (window != null)
            {
                Rect worldRect = window.WorldRect;
                if (worldRect.width > 1f && worldRect.height > 1f)
                    return worldRect;
            }

            Vector2 windowPos = Fugui.GetWindowPos();
            Vector2 windowSize = Fugui.GetWindowSize();
            if (windowSize.x > 1f && windowSize.y > 1f)
                return new Rect(windowPos, windowSize);

            return fallbackBounds;
        }

        /// <summary>
        /// Returns the popup rect inside bounds value.
        /// </summary>
        private static Rect GetPopupRectInsideBounds(Rect anchorRect, float width, float height, Rect bounds, float gap, float margin)
        {
            float minX = bounds.x + margin;
            float maxX = bounds.xMax - width - margin;
            float x = ClampEvenIfInverted(anchorRect.center.x - width * 0.5f, minX, maxX);

            float aboveY = anchorRect.y - height - gap;
            float belowY = anchorRect.yMax + gap;
            float minY = bounds.y + margin;
            float maxY = bounds.yMax - height - margin;

            bool fitsAbove = aboveY >= minY;
            bool fitsBelow = belowY <= maxY;
            float y;

            if (fitsAbove)
                y = aboveY;
            else if (fitsBelow)
                y = belowY;
            else
                y = ClampEvenIfInverted(aboveY, minY, maxY);

            return new Rect(x, y, width, height);
        }

        /// <summary>
        /// Clamps the even if inverted value to a valid range.
        /// </summary>
        private static float ClampEvenIfInverted(float value, float min, float max)
        {
            if (max < min)
                return (min + max) * 0.5f;

            return Mathf.Clamp(value, min, max);
        }





        #endregion

        #region Timeline State And Seeking
        /// <summary>
        /// Runs the ensure timeline events populated logic.
        /// </summary>
        private void EnsureTimelineEventsPopulated()
        {
            if (_usingManualEvents)
                return;

            if (!TryGetCurrentFlightEventsSignature(out object sourceContainer, out int signature))
            {
                if (_eventsSourceContainer != null || _events.Count > 0)
                {
                    _eventsSourceContainer = null;
                    _eventsSourceSignature = 0;
                    _events.Clear();
                    _openEventClusterHash = 0;
                }

                return;
            }

            if (ReferenceEquals(_eventsSourceContainer, sourceContainer) && _eventsSourceSignature == signature)
                return;

            _events.Clear();
            foreach (var evt in Sara.Flight.Container.Events)
            {
                var tid = new TID(Sara.Flight.Container.TIDInfos, evt.Tid);
                var severity = (EventSeverity)evt.Severity;
                _events.Add(new TimelineEvent(tid, evt.Name, severity, severity == EventSeverity.Medium || severity == EventSeverity.High));
            }

            _eventsSourceContainer = sourceContainer;
            _eventsSourceSignature = signature;
            _openEventClusterHash = 0;
        }

        /// <summary>
        /// Attempts to resolve the current flight events signature value.
        /// </summary>
        private static bool TryGetCurrentFlightEventsSignature(out object sourceContainer, out int signature)
        {
            sourceContainer = null;
            signature = 0;

            if (!CanReadCurrentFlightEvents())
                return false;

            sourceContainer = Sara.Flight.Container;

            unchecked
            {
                int hash = 17;
                int count = 0;

                foreach (var evt in Sara.Flight.Container.Events)
                {
                    count++;
                    hash = hash * 31 + evt.Tid.GetHashCode();
                    hash = hash * 31 + (evt.Name != null ? evt.Name.GetHashCode() : 0);
                    hash = hash * 31 + evt.Severity.GetHashCode();
                }

                signature = hash * 31 + count;
            }

            return true;
        }



        /// <summary>
        /// Returns whether the read current flight events action is allowed.
        /// </summary>
        private static bool CanReadCurrentFlightEvents()
        {
            return Sara.IsReady
                && Sara.Flight != null
                && Sara.Flight.Container != null
                && Sara.Flight.Container.Events != null;
        }





        /// <summary>
        /// Returns whether the read timeline action is allowed.
        /// </summary>
        private static bool CanReadTimeline()
        {
            return Sara.IsReady
                && Sara.Time != null
                && Sara.Flight != null;
        }

        /// <summary>
        /// Returns whether the control timeline action is allowed.
        /// </summary>
        private static bool CanControlTimeline()
        {
            SaraUser user = Sara.CurrentSession != null ? Sara.CurrentSession.User : null;
            return user == null || user.CanControlTimeline;
        }

        /// <summary>
        /// Returns the display name of the user controlling the shared timeline.
        /// </summary>
        private static string GetTimelineControllerName()
        {
            SaraSession session = Sara.CurrentSession;
            if (session != null && session.Users != null)
            {
                for (int i = 0; i < session.Users.Length; i++)
                {
                    SaraSessionUser sessionUser = session.Users[i];
                    string name = GetTimelineControllerDisplayName(sessionUser != null ? sessionUser.User : null);
                    if (!string.IsNullOrEmpty(name))
                        return name;
                }
            }

            return "another user";
        }

        /// <summary>
        /// Returns the display name when the user controls the shared timeline.
        /// </summary>
        private static string GetTimelineControllerDisplayName(SaraUser user)
        {
            if (user == null || !user.CanControlTimeline)
                return null;

            if (!string.IsNullOrWhiteSpace(user.Name))
                return user.Name.Trim();

            switch (user.Role)
            {
                case SaraUserRole.Admin:
                    return "admin";
                case SaraUserRole.Captain:
                    return "captain";
                case SaraUserRole.FirstOfficer:
                    return "first officer";
                case SaraUserRole.Observator:
                    return "observer";
                default:
                    return "another user";
            }
        }





        /// <summary>
        /// Updates the current phase index state.
        /// </summary>
        private FlightPhase UpdateCurrentPhaseIndex()
        {
            if (!CanReadTimeline())
                return null;

            int phaseCount = Sara.Flight.PhasesCount;
            if (phaseCount <= 0)
                return null;

            _currentPhaseIndex = Mathf.Clamp(_currentPhaseIndex, 0, phaseCount - 1);
            for (int i = 0; i < phaseCount; i++)
            {
                FlightPhase phase = Sara.Flight.GetPhase(i);
                if (phase.TID <= Sara.Time.CurrentTid)
                    _currentPhaseIndex = i;
                else
                    break;
            }

            return Sara.Flight.GetPhase(_currentPhaseIndex);
        }

        /// <summary>
        /// Runs the previous phase logic.
        /// </summary>
        private void PreviousPhase()
        {
            if (!CanReadTimeline() || Sara.Flight.PhasesCount == 0)
                return;

            UpdateCurrentPhaseIndex();
            if (_currentPhaseIndex <= 0)
                return;

            _currentPhaseIndex--;
            SeekTimelineTid(Sara.Flight.GetPhase(_currentPhaseIndex).TID);
        }

        /// <summary>
        /// Runs the next phase logic.
        /// </summary>
        private void NextPhase()
        {
            if (!CanReadTimeline() || Sara.Flight.PhasesCount == 0)
                return;

            UpdateCurrentPhaseIndex();
            if (_currentPhaseIndex >= Sara.Flight.PhasesCount - 1)
                return;

            _currentPhaseIndex++;
            SeekTimelineTid(Sara.Flight.GetPhase(_currentPhaseIndex).TID);
        }



        /// <summary>
        /// Updates the button hold seek state.
        /// </summary>
        private void UpdateButtonHoldSeek(int direction)
        {
            if (!CanControlTimeline())
            {
                _buttonSeekDirection = 0;
                return;
            }

            if (direction == 0)
            {
                _buttonSeekDirection = 0;
                return;
            }

            if (_buttonSeekDirection != direction)
            {
                _buttonSeekDirection = direction;
                _buttonSeekStartTime = Time.unscaledTime;
            }

            if (Time.unscaledTime - _buttonSeekStartTime < HoldSeekDelaySeconds)
                return;

            SeekSeconds(direction * FastSeekSecondsPerSecond * Time.unscaledDeltaTime, false);
            TriggerSeekFeedback(direction, true);
        }




        /// <summary>
        /// Updates the screen seek gestures state.
        /// </summary>
        private void UpdateScreenSeekGestures(Vector2 containerSize)
        {
            FuMouseState mouse = Fugui.GetCurrentMouse();
            Vector2 mousePos = Fugui.GetCurrentMouse().Position;

            if (_screenSeekHolding)
            {
                if (mouse.IsPressed(FuMouseButton.Left))
                    UpdateScreenSeekHold();

                if (mouse.IsUp(FuMouseButton.Left))
                {
                    _screenSeekHolding = false;
                    _screenSeekDirection = 0;
                }
            }

            if (!mouse.IsDown(FuMouseButton.Left))
                return;

            if (!CanStartScreenSeekGesture(mousePos, containerSize))
            {
                _lastScreenTapSide = 0;
                _lastScreenTapTime = -1f;
                _lastScreenTapPosition = Vector2.zero;
                return;
            }

            int side = mousePos.x < containerSize.x * 0.5f ? -1 : 1;
            float now = Time.unscaledTime;
            float maxTapTravel = Mathf.Max(96f * Fugui.Scale, containerSize.x * 0.35f);
            bool isDoubleTap =
                side == _lastScreenTapSide &&
                now - _lastScreenTapTime <= DoubleTapDelaySeconds &&
                Vector2.Distance(mousePos, _lastScreenTapPosition) <= maxTapTravel;

            if (!isDoubleTap)
            {
                _lastScreenTapSide = side;
                _lastScreenTapTime = now;
                _lastScreenTapPosition = mousePos;
                return;
            }

            SeekSeconds(side * SeekStepSeconds);
            TriggerSeekFeedback(side, false);

            _screenSeekDirection = side;
            _screenSeekHolding = true;
            _screenSeekHoldStartTime = now;
            _lastScreenTapSide = 0;
            _lastScreenTapTime = -1f;
            _lastScreenTapPosition = Vector2.zero;
        }

        /// <summary>
        /// Returns whether the start screen seek gesture action is allowed.
        /// </summary>
        private bool CanStartScreenSeekGesture(Vector2 mousePos, Vector2 containerSize)
        {
            if (InputBlocked)
                return false;

            if (_timelineDragging || _speedMenuOpen || _speedMenuAmount > 0.001f)
                return false;

            Rect screenRect = new Rect(0f, 0f, containerSize.x, containerSize.y);
            if (!screenRect.Contains(mousePos))
                return false;

            return !FlatCameraInputBlocker.IsPointerBlocked(mousePos);
        }

        /// <summary>
        /// Updates the screen seek hold state.
        /// </summary>
        private void UpdateScreenSeekHold()
        {
            if (_screenSeekDirection == 0)
                return;

            if (Time.unscaledTime - _screenSeekHoldStartTime < HoldSeekDelaySeconds)
                return;

            SeekSeconds(_screenSeekDirection * FastSeekSecondsPerSecond * Time.unscaledDeltaTime, false);
            TriggerSeekFeedback(_screenSeekDirection, true);
        }

        /// <summary>
        /// Runs the trigger seek feedback logic.
        /// </summary>
        private void TriggerSeekFeedback(int direction, bool fast)
        {
            _seekFeedbackDirection = direction < 0 ? -1 : 1;
            _seekFeedbackFast = fast;
            _seekFeedbackStartTime = Time.unscaledTime;
        }

        /// <summary>
        /// Draws the seek feedback UI.
        /// </summary>
        private void DrawSeekFeedback(FuDrawList drawList, Vector2 containerSize)
        {
            float age = Time.unscaledTime - _seekFeedbackStartTime;
            if (age < 0f || age > SeekFeedbackDurationSeconds || _seekFeedbackDirection == 0)
                return;

            TimelineWidgetTheme theme = Theme;
            float scale = Fugui.Scale;
            float t = SmoothStep01(Mathf.Clamp01(age / SeekFeedbackDurationSeconds));
            float alpha = 1f - t;
            Vector2 center = new Vector2(
                _seekFeedbackDirection < 0 ? containerSize.x * 0.25f : containerSize.x * 0.75f,
                containerSize.y * 0.5f);
            float radius = (theme.SeekButtonSize * 0.78f + 12f * t) * scale;
            float glowRadius = radius + (_seekFeedbackFast ? 16f : 9f) * scale;
            Color fill = _seekFeedbackFast ? theme.ControlCircleActive : theme.ControlCircleHover;
            string icon = _seekFeedbackDirection < 0 ? Icons.RotateLeft_light : Icons.RotateRight_light;
            Color iconColor = _seekFeedbackFast ? theme.AccentHi : theme.Text;

            drawList.AddCircleFilled(center, glowRadius, ColorU32(theme.AccentGlow, (_seekFeedbackFast ? 0.34f : 0.22f) * alpha), 48);
            drawList.AddCircleFilled(center, radius, ColorU32(fill, Mathf.Min(1f, 1.7f * alpha)), 48);
            drawList.AddCircle(center, radius, ColorU32(theme.DockBorder, alpha), 48, Mathf.Max(1f, scale));
            drawList.AddCircle(center, radius + 3f * scale, ColorU32(theme.Accent, (_seekFeedbackFast ? 0.42f : 0.26f) * alpha), 48, Mathf.Max(1f, 1.5f * scale));

            Rect iconRect = new Rect(center.x - 22f * scale, center.y - 24f * scale, 44f * scale, 38f * scale);

            Fugui.PushFont(20);
            DrawIconCenteredTinted(drawList, iconRect, icon, ColorU32(iconColor, alpha), ColorU32(iconColor, alpha), ColorU32(iconColor, alpha));
            Fugui.PopFont();

            Fugui.PushFont(10);
            DrawTextCenteredScaled(drawList, iconRect, "10", ColorU32(iconColor, alpha), 0.72f);
            Fugui.PopFont();

            string label = _seekFeedbackDirection < 0 ? "-10s" : "+10s";
            PushCompactFont(12);
            Rect labelRect = new Rect(center.x - radius, center.y + 14f * scale, radius * 2f, 18f * scale);
            DrawTextCentered(drawList, labelRect, label, ColorU32(theme.TextDim, alpha));
            PopCompactFont();
        }

        /// <summary>
        /// Resets the screen seek gesture state.
        /// </summary>
        private void ResetScreenSeekGesture()
        {
            _screenSeekHolding = false;
            _screenSeekDirection = 0;
            _lastScreenTapSide = 0;
            _lastScreenTapTime = -1f;
            _lastScreenTapPosition = Vector2.zero;
        }

        /// <summary>
        /// Resets active timeline interactions when input is captured by another flat UI layer.
        /// </summary>
        private void ResetInteractiveState()
        {
            _timelineDragging = false;
            _speedMenuOpen = false;
            _speedMenuOpenedThisFrame = false;
            _buttonSeekDirection = 0;
            ResetScreenSeekGesture();
        }

        /// <summary>
        /// Seeks the timeline from mouse position.
        /// </summary>
        private static void SeekTimelineFromMouse(Rect trackRect)
        {
            if (trackRect.width <= 0f)
                return;

            SeekTimelineNormalized(Mathf.Clamp01((Fugui.GetCurrentMouse().Position.x - trackRect.x) / trackRect.width), false);
        }

        /// <summary>
        /// Seeks the seconds position.
        /// </summary>
        private static void SeekSeconds(float seconds, bool immediate = true)
        {
            if (!CanReadTimeline() || Sara.Time.DurationSeconds <= 0f)
                return;

            if (Sara.Network != null)
                Sara.Network.SeekTimelineSeconds(seconds, immediate);
            else
                Sara.Time.SeekNormalized(Sara.Time.GetNormalized() + seconds / Sara.Time.DurationSeconds);
        }

        /// <summary>
        /// Seeks the timeline to a TID through the shared timeline control path.
        /// </summary>
        private static void SeekTimelineTid(TID tid)
        {
            if (Sara.Network != null)
                Sara.Network.SeekTimelineTid(tid);
            else
                Sara.Time.SeekTid(tid);
        }

        /// <summary>
        /// Seeks the timeline to a normalized position through the shared timeline control path.
        /// </summary>
        private static void SeekTimelineNormalized(double normalized, bool immediate)
        {
            if (Sara.Network != null)
                Sara.Network.SeekTimelineNormalized(normalized, immediate);
            else
                Sara.Time.SeekNormalized(normalized);
        }

        /// <summary>
        /// Sets the timeline play state through the shared timeline control path.
        /// </summary>
        private static void SetTimelinePlaying(bool isPlaying)
        {
            if (Sara.Network != null)
                Sara.Network.SetTimelinePlaying(isPlaying);
            else if (isPlaying)
                Sara.Time.Play();
            else
                Sara.Time.Pause();
        }

        /// <summary>
        /// Sets the timeline speed through the shared timeline control path.
        /// </summary>
        private static void SetTimelineSpeed(float speed)
        {
            if (Sara.Network != null)
                Sara.Network.SetTimelineSpeed(speed);
            else
                Sara.Time.SetSpeed(speed);
        }

        /// <summary>
        /// Normalizes the TID value.
        /// </summary>
        private static float NormalizeTid(TID tid)
        {
            TID startTID = Sara.Time.FirstTid;
            TID endTID = Sara.Time.LastTid;
            if (endTID == startTID)
                return 0f;

            return (float)((tid - startTID) / (endTID - startTID));
        }

        /// <summary>
        /// Formats the flight clock value for display.
        /// </summary>
        private static string FormatFlightClock(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds))
                return "--:--:--";

            TimeSpan time = TimeSpan.FromSeconds(Math.Max(0.0, seconds));
            return ((int)time.TotalHours).ToString("00") + ":" + time.Minutes.ToString("00") + ":" + time.Seconds.ToString("00");
        }

        /// <summary>
        /// Returns the current playback speed label value.
        /// </summary>
        private static string GetCurrentPlaybackSpeedLabel()
        {
            for (int i = 0; i < PlaybackSpeeds.Length; i++)
            {
                if (Math.Abs(Sara.Time.Speed - PlaybackSpeeds[i]) <= 0.01)
                    return PlaybackSpeedLabels[i];
            }

            return Sara.Time.Speed.ToString("0.#") + "x";
        }



        /// <summary>
        /// Returns the current phase pill label value.
        /// </summary>
        private static string GetCurrentPhasePillLabel(string phaseName)
        {
            string normalized = TimelineWidgetTheme.NormalizePhaseKey(phaseName);
            switch (normalized)
            {
                case "takeoff":
                case "take_off":
                case "to":
                case "t_o":
                    return "TAKE-OFF";
                case "ini_climb":
                case "initial_climb":
                case "init_climb":
                    return "INI CLIMB";
                case "fin_app":
                case "fin_approach":
                case "final_app":
                case "final_approach":
                    return "FIN APP";
                case "taxi_out":
                case "taxi_in":
                    return "TAXI";
                case "no_phase":
                    return "P...";
                default:
                    return string.IsNullOrEmpty(phaseName)
                        ? "P..."
                        : phaseName.Trim().Replace("_", " ").Replace("-", " ").ToUpperInvariant();
            }
        }



        /// <summary>
        /// Returns the phase label value.
        /// </summary>
        private static string GetPhaseLabel(string phaseName)
        {
            string normalized = TimelineWidgetTheme.NormalizePhaseKey(phaseName);
            switch (normalized)
            {
                case "takeoff":
                case "take_off":
                case "to":
                case "t_o":
                    return "T/O";
                case "ini_climb":
                case "initial_climb":
                case "init_climb":
                    return "INI CLIMB";
                case "fin_app":
                case "fin_approach":
                case "final_app":
                case "final_approach":
                    return "FIN APP";
                case "taxi_out":
                    return "TAXI";
                case "taxi_in":
                    return "TAXI";
                case "no_phase":
                    return "P...";
                default:
                    return string.IsNullOrEmpty(phaseName) ? "P..." : phaseName.ToUpperInvariant();
            }
        }





        #endregion

        #region Drawing Utilities
        /// <summary>
        /// Returns whether the hovered condition is met.
        /// </summary>
        private bool IsHovered(Rect rect)
        {
            return !InputBlocked && rect.Contains(Fugui.GetCurrentMouse().Position);
        }

        /// <summary>
        /// Gets the shortcut tooltip text for an action.
        /// </summary>
        private string GetShortcutTooltip(SaraShortcutAction action)
        {
            return ShortcutTooltipProvider != null ? ShortcutTooltipProvider(action) : null;
        }

        /// <summary>
        /// Draws a shortcut tooltip when available.
        /// </summary>
        private static void DrawShortcutTooltip(string id, string tooltip)
        {
            if (!string.IsNullOrEmpty(tooltip))
                ShortcutTooltipLayout.SetToolTip(id + "Shortcut", tooltip, true);
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
        /// Runs the rgba logic.
        /// </summary>
        private static Color Rgba(int r, int g, int b, float alpha)
        {
            return new Color(r / 255f, g / 255f, b / 255f, alpha);
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
        /// Returns the text width value.
        /// </summary>
        private static float GetTextWidth(string text, int size, bool bold)
        {
            Fugui.PushFont(size);
            if (bold)
                Fugui.PushFont(FontType.Bold);
            float width = Fugui.CalcTextSize(text).x;
            if (bold)
                Fugui.PopFont();
            Fugui.PopFont();
            return width;
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
        /// Draws the text centered scaled UI.
        /// </summary>
        private static void DrawTextCenteredScaled(FuDrawList drawList, Rect rect, string text, uint color, float scale)
        {
            ImFontPtr font = Fugui.GetFont();
            float currentFontSize = Mathf.Max(1f, Fugui.GetFontSize());
            float targetFontSize = currentFontSize * Mathf.Clamp(scale, 0.1f, 1f);
            Vector2 textSize = Fugui.CalcTextSize(text) * (targetFontSize / currentFontSize);
            Vector2 textPos = new Vector2(rect.x + (rect.width - textSize.x) * 0.5f, rect.y + (rect.height - textSize.y) * 0.5f);
            drawList.AddText(font, targetFontSize, textPos, color, text);
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
}
