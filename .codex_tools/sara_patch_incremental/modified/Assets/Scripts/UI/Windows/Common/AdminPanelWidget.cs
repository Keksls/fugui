using Fu;
using Fu.Framework;
using NetSquare.Client;

using Saravr.Network.Common;
using System;
using UnityEngine;

namespace Assets.Scripts.UI.Windows.Common
{
    /// <summary>
    /// Draws the multiplayer admin panel used by both flat and XR UI.
    /// </summary>
    public sealed class AdminPanelWidget
    {
        private const float HeaderHeight = 72f;
        private const float SectionHeight = 34f;
        private const float RowHeight = 58f;
        private const float SeatMapHeight = 178f;
        private const string SeatDragPayloadId = "SARA_ADMIN_SEAT";
        private readonly Rect[] seatRects = new Rect[(int)SeatType.NBSeat];

        private TimelineWidgetTheme _theme;
        private SeatType _selectedSeat = SeatType.Center;
        private float _scrollY;
        private string _statusMessage = string.Empty;

        /// <summary>
        /// Drag/drop payload for seat move requests.
        /// </summary>
        private sealed class SeatDragPayload
        {
            public SeatType Seat;
        }

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
            DrawTextLeftCentered(drawList, titleRect, "Admin Panel", ColorU32(theme.Text, alpha), 0f);
            PopFont(true);

            drawList.AddLine(
                new Vector2(rect.x, rect.yMax),
                new Vector2(rect.xMax, rect.yMax),
                ColorU32(theme.DockBorder, alpha * 0.60f),
                Mathf.Max(1f, scale));

            return DrawCloseButton(drawList, closeRect, theme, alpha, interactable);
        }

        /// <summary>
        /// Draws the scrollable panel body.
        /// </summary>
        private void DrawBody(FuDrawList drawList, Rect bodyRect, TimelineWidgetTheme theme, float alpha, bool interactable)
        {
            float scale = Fugui.Scale;
            bool adminSession = IsAdminSession();
            float contentHeight = CalculateContentHeight(scale, adminSession);
            float maxScroll = Mathf.Max(0f, contentHeight - bodyRect.height);
            if (maxScroll <= 0f)
            {
                _scrollY = 0f;
            }
            else if (interactable && bodyRect.Contains(Fugui.GetCurrentMouse().Position))
            {
                float wheel = Fugui.GetCurrentMouse().Wheel.y;
                if (Mathf.Abs(wheel) > 0.001f)
                    _scrollY = Mathf.Clamp(_scrollY - wheel * 34f * scale, 0f, maxScroll);
            }

            _scrollY = Mathf.Clamp(_scrollY, 0f, maxScroll);

            float y = bodyRect.y + 8f * scale - _scrollY;
            drawList.PushClipRect(bodyRect.min, bodyRect.max, true);

            if (!adminSession)
            {
                DrawUnavailable(drawList, bodyRect, theme, alpha);
                drawList.PopClipRect();
                return;
            }

            y = DrawSection(drawList, bodyRect, bodyRect.x, y, bodyRect.width, "S E A T S", theme, alpha);
            y = DrawSeatMap(drawList, bodyRect, bodyRect.x, y, bodyRect.width, theme, alpha, interactable);

            y = DrawSection(drawList, bodyRect, bodyRect.x, y, bodyRect.width, "U S E R", theme, alpha);
            y = DrawSelectedUserControls(drawList, bodyRect, bodyRect.x, y, bodyRect.width, theme, alpha, interactable);

            y = DrawSection(drawList, bodyRect, bodyRect.x, y, bodyRect.width, "O B S E R V E R S", theme, alpha);
            DrawObserversList(drawList, bodyRect, bodyRect.x, y, bodyRect.width, theme, alpha, interactable);

            drawList.PopClipRect();
        }

        /// <summary>
        /// Calculates the body content height.
        /// </summary>
        private static float CalculateContentHeight(float scale, bool adminSession)
        {
            if (!adminSession)
                return 160f * scale;

            return 8f * scale
                + SectionHeight * scale
                + SeatMapHeight * scale
                + SectionHeight * scale
                + 38f * scale
                + RowHeight * 3f * scale
                + SectionHeight * scale
                + Mathf.Max(1, CountObservers()) * RowHeight * scale
                + 36f * scale;
        }

        /// <summary>
        /// Draws the unavailable message.
        /// </summary>
        private static void DrawUnavailable(FuDrawList drawList, Rect rect, TimelineWidgetTheme theme, float alpha)
        {
            Rect messageRect = new Rect(rect.x + 22f * Fugui.Scale, rect.y + 18f * Fugui.Scale, rect.width - 44f * Fugui.Scale, 60f * Fugui.Scale);
            PushFont(16, true);
            DrawTextLeftCentered(drawList, messageRect, "Admin controls are only available to the multiplayer session admin.", ColorU32(theme.TextDim, alpha), 0f);
            PopFont(true);
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
                Rect labelRect = new Rect(rect.x + 22f * scale, rect.y + 14f * scale, rect.width - 44f * scale, 14f * scale);
                PushFont(10, true);
                DrawTextLeftCentered(drawList, labelRect, label, ColorU32(theme.TextFaint, alpha), 0f);
                PopFont(true);
            }

            return rect.yMax;
        }

        /// <summary>
        /// Draws the three-seat map and handles drag/drop swaps.
        /// </summary>
        private float DrawSeatMap(FuDrawList drawList, Rect clipRect, float x, float y, float width, TimelineWidgetTheme theme, float alpha, bool interactable)
        {
            float scale = Fugui.Scale;
            Rect rect = new Rect(x, y, width, SeatMapHeight * scale);
            if (!IsVisible(rect, clipRect))
                return rect.yMax;

            float gap = 12f * scale;
            float seatSize = Mathf.Clamp((rect.width - 44f * scale - gap * 2f) / 3f, 72f * scale, 122f * scale);
            float totalWidth = seatSize * 3f + gap * 2f;
            float startX = rect.x + (rect.width - totalWidth) * 0.5f;
            float sideY = rect.y + 10f * scale;
            float centerY = sideY + 24f * scale;

            SetSeatRect(SeatType.Pilot, new Rect(startX, sideY, seatSize, seatSize));
            SetSeatRect(SeatType.Center, new Rect(startX + seatSize + gap, centerY, seatSize, seatSize));
            SetSeatRect(SeatType.CoPilot, new Rect(startX + (seatSize + gap) * 2f, sideY, seatSize, seatSize));

            SeatType hoveredSeat = SeatType.NBSeat;
            DrawSeatCard(drawList, SeatType.Pilot, "Left", theme, alpha, interactable, ref hoveredSeat);
            DrawSeatCard(drawList, SeatType.Center, "Center", theme, alpha, interactable, ref hoveredSeat);
            DrawSeatCard(drawList, SeatType.CoPilot, "Right", theme, alpha, interactable, ref hoveredSeat);

            return rect.yMax;
        }

        /// <summary>
        /// Stores a seat rect in the reusable array.
        /// </summary>
        private void SetSeatRect(SeatType seat, Rect rect)
        {
            if (Seats.IsValidSeat(seat))
                seatRects[(int)seat] = rect;
        }

        /// <summary>
        /// Draws one seat square.
        /// </summary>
        private void DrawSeatCard(FuDrawList drawList, SeatType seat, string label, TimelineWidgetTheme theme, float alpha, bool interactable, ref SeatType hoveredSeat)
        {
            Rect rect = seatRects[(int)seat];
            SaraUser user = GetSeatUser(seat, out uint clientId);
            bool occupied = clientId != 0 && user != null;
            bool selected = _selectedSeat == seat;
            SeatDragPayload currentPayload = Fugui.IsDraggingPayload(SeatDragPayloadId) ? Fugui.GetDragDropPayload<SeatDragPayload>() : null;
            bool dragging = currentPayload != null && currentPayload.Seat == seat;
            FuLayout layout = FuWindow.CurrentDrawingWindow?.Layout;
            bool clicked = false;
            bool hovered = false;
            bool active = false;

            if (layout != null)
            {
                // Keep a native Fugui item for drag-and-drop while restoring the custom layout cursor.
                Fugui.PushScreenPos(rect.min);
                clicked = layout.InvisibleInteraction(
                    "##adminSeat" + seat,
                    rect.size,
                    out hovered,
                    out active,
                    FuButtonFlags.MouseButtonLeft,
                    interactable);

                if (interactable)
                {
                    Fugui.BeginDragDropSource(
                        SeatDragPayloadId,
                        FuDragDropFlags.None,
                        () => DrawSeatPayloadPreview(layout, seat),
                        new SeatDragPayload { Seat = seat });

                    Fugui.BeginDragDropTarget<SeatDragPayload>(
                        SeatDragPayloadId,
                        payload =>
                        {
                            if (payload != null && Seats.IsValidSeat(payload.Seat) && payload.Seat != seat)
                            {
                                _selectedSeat = seat;
                                SwapSeats(payload.Seat, seat);
                            }
                        });
                }

                Fugui.PopScreenPos();
            }

            Color background = selected ? theme.PillBackgroundActive : active ? theme.PillBackgroundActive : hovered ? theme.PillBackgroundHover : theme.PillBackground;
            Color border = selected ? theme.Accent : occupied ? theme.AccentGlow : theme.DockBorder;
            float scale = Fugui.Scale;

            FlatCameraInputBlocker.RegisterRect(rect);
            drawList.AddRectFilled(rect.min, rect.max, ColorU32(background, dragging ? alpha * 0.62f : alpha), theme.MediumRadius * scale);
            drawList.AddRect(rect.min, rect.max, ColorU32(border, occupied ? alpha * 0.82f : alpha * 0.48f), theme.MediumRadius * scale, FuDrawFlags.None, Mathf.Max(1f, scale));

            Rect titleRect = new Rect(rect.x + 8f * scale, rect.y + 10f * scale, rect.width - 16f * scale, 22f * scale);
            Rect nameRect = new Rect(rect.x + 8f * scale, rect.y + rect.height * 0.42f, rect.width - 16f * scale, 24f * scale);
            Rect roleRect = new Rect(rect.x + 8f * scale, nameRect.yMax + 2f * scale, rect.width - 16f * scale, 18f * scale);

            PushFont(14, true);
            DrawTextCentered(drawList, titleRect, label, ColorU32(selected ? theme.Accent : theme.Text, alpha));
            PopFont(true);

            PushFont(14, true);
            DrawTextCentered(drawList, nameRect, ClipTextToWidth(occupied ? GetUserName(user) : "Empty", nameRect.width), ColorU32(occupied ? theme.Text : theme.TextFaint, alpha));
            PopFont(true);

            PushFont(10, false);
            DrawTextCentered(drawList, roleRect, occupied ? GetRoleLabel(user.Role) : string.Empty, ColorU32(occupied ? theme.TextDim : theme.TextFaint, alpha));
            PopFont(false);

            if (hovered)
            {
                hoveredSeat = seat;
                Fugui.SetMouseCursor(FuMouseCursor.Hand);
                if (clicked)
                    _selectedSeat = seat;
            }
        }

        /// <summary>
        /// Draws the Fugui drag payload preview.
        /// </summary>
        private static void DrawSeatPayloadPreview(FuLayout layout, SeatType seat)
        {
            SaraUser user = GetSeatUser(seat, out uint clientId);
            string label = clientId != 0 && user != null
                ? GetSeatLabel(seat) + ": " + GetUserName(user)
                : GetSeatLabel(seat) + ": Empty";

            layout.Text(label);
        }

        /// <summary>
        /// Draws controls for the user currently selected by seat.
        /// </summary>
        private float DrawSelectedUserControls(FuDrawList drawList, Rect clipRect, float x, float y, float width, TimelineWidgetTheme theme, float alpha, bool interactable)
        {
            float scale = Fugui.Scale;
            SaraUser user = GetSeatUser(_selectedSeat, out uint clientId);
            bool hasUser = user != null && clientId != 0;
            Rect summaryRect = new Rect(x + 22f * scale, y, width - 44f * scale, 38f * scale);
            string owner = GetTimelineOwnerName();

            PushFont(14, true);
            DrawTextLeftCentered(drawList, summaryRect, hasUser ? GetSeatLabel(_selectedSeat) + ": " + GetUserName(user) : GetSeatLabel(_selectedSeat) + ": Empty", ColorU32(hasUser ? theme.Text : theme.TextFaint, alpha), 0f);
            PopFont(true);

            PushFont(12, false);
            Rect ownerRect = new Rect(summaryRect.x, summaryRect.yMax - 14f * scale, summaryRect.width, 14f * scale);
            DrawTextLeftCentered(drawList, ownerRect, "Timeline: " + owner, ColorU32(theme.TextFaint, alpha), 0f);
            PopFont(false);

            float rowY = summaryRect.yMax + 4f * scale;
            if (!hasUser)
            {
                DrawEmptySelection(drawList, new Rect(x, rowY, width, RowHeight * scale), theme, alpha);
                return rowY + RowHeight * scale + 10f * scale;
            }

            rowY = DrawActionRow(
                drawList,
                clipRect,
                x,
                rowY,
                width,
                "Voice",
                user.IsMuted ? "Microphone is muted by admin" : "Microphone allowed",
                user.IsMuted ? "Unmute" : "Mute",
                user.IsMuted,
                theme,
                alpha,
                interactable,
                () => SetUserMuted(clientId, !user.IsMuted));

            rowY = DrawActionRow(
                drawList,
                clipRect,
                x,
                rowY,
                width,
                "Pointing",
                user.CanPoint ? "Shared pointer allowed" : "Shared pointer blocked",
                user.CanPoint ? "Disable" : "Enable",
                user.CanPoint,
                theme,
                alpha,
                interactable,
                () => SetUserCanPoint(clientId, !user.CanPoint));

            DrawActionRow(
                drawList,
                clipRect,
                x,
                rowY,
                width,
                "Timeline",
                user.CanControlTimeline ? "Current timeline controller" : "Give this user timeline control",
                user.CanControlTimeline ? "Owner" : (clientId == NSClient.ClientID ? "Take" : "Give"),
                user.CanControlTimeline,
                theme,
                alpha,
                interactable && !user.CanControlTimeline,
                () => SetTimelineController(clientId));

            if (!string.IsNullOrWhiteSpace(_statusMessage))
            {
                Rect statusRect = new Rect(x + 22f * scale, rowY + RowHeight * scale + 6f * scale, width - 44f * scale, 22f * scale);
                PushFont(12, false);
                DrawTextLeftCentered(drawList, statusRect, ClipTextToWidth(_statusMessage, statusRect.width), ColorU32(theme.TextDim, alpha), 0f);
                PopFont(false);
            }

            return rowY + RowHeight * scale + 36f * scale;
        }

        /// <summary>
        /// Draws connected observers below the seat user controls.
        /// </summary>
        private void DrawObserversList(FuDrawList drawList, Rect clipRect, float x, float y, float width, TimelineWidgetTheme theme, float alpha, bool interactable)
        {
            SaraSession session = Sara.CurrentSession;
            if (session == null || session.Users == null || CountObservers() == 0)
            {
                DrawNoObservers(drawList, new Rect(x, y, width, RowHeight * Fugui.Scale), theme, alpha);
                return;
            }

            for (int i = 0; i < session.Users.Length; i++)
            {
                SaraSessionUser sessionUser = session.Users[i];
                if (sessionUser == null || sessionUser.User == null || !sessionUser.User.IsObservator)
                    continue;

                Rect rowRect = new Rect(x, y, width, RowHeight * Fugui.Scale);
                DrawObserverRow(drawList, clipRect, rowRect, sessionUser.ClientID, sessionUser.User, theme, alpha, interactable);
                y = rowRect.yMax;
            }
        }

        /// <summary>
        /// Draws one observer moderation row.
        /// </summary>
        private void DrawObserverRow(FuDrawList drawList, Rect clipRect, Rect rect, uint clientId, SaraUser user, TimelineWidgetTheme theme, float alpha, bool interactable)
        {
            if (!IsVisible(rect, clipRect))
                return;

            float scale = Fugui.Scale;
            DrawRowTopDivider(drawList, rect, theme, alpha);

            Rect textRect = new Rect(rect.x + 22f * scale, rect.y, rect.width - 146f * scale, rect.height);
            string hint = user.WantsUnmute ? "Requests microphone access" : user.IsMuted ? "Muted observer" : "Microphone allowed";
            DrawSettingText(drawList, textRect, GetUserName(user), hint, theme, alpha, interactable);

            Rect buttonRect = new Rect(rect.xMax - 112f * scale, rect.y + (rect.height - 32f * scale) * 0.5f, 90f * scale, 32f * scale);
            if (DrawPillButton(drawList, buttonRect, user.IsMuted ? "Unmute" : "Mute", !user.IsMuted, theme, alpha, interactable && IsMouseInClip(clipRect)))
                SetUserMuted(clientId, !user.IsMuted);

            if (user.WantsUnmute)
                DrawRequestDot(drawList, new Vector2(buttonRect.xMax - 2f * scale, buttonRect.y + 2f * scale), alpha);
        }

        /// <summary>
        /// Draws the empty observers row.
        /// </summary>
        private static void DrawNoObservers(FuDrawList drawList, Rect rect, TimelineWidgetTheme theme, float alpha)
        {
            DrawRowTopDivider(drawList, rect, theme, alpha);
            Rect textRect = new Rect(rect.x + 22f * Fugui.Scale, rect.y, rect.width - 44f * Fugui.Scale, rect.height);
            PushFont(14, true);
            DrawTextLeftCentered(drawList, textRect, "No observers connected.", ColorU32(theme.TextFaint, alpha), 0f);
            PopFont(true);
        }

        /// <summary>
        /// Draws a row for an empty selected seat.
        /// </summary>
        private static void DrawEmptySelection(FuDrawList drawList, Rect rect, TimelineWidgetTheme theme, float alpha)
        {
            DrawRowTopDivider(drawList, rect, theme, alpha);
            Rect textRect = new Rect(rect.x + 22f * Fugui.Scale, rect.y, rect.width - 44f * Fugui.Scale, rect.height);
            PushFont(14, true);
            DrawTextLeftCentered(drawList, textRect, "No user in this seat.", ColorU32(theme.TextFaint, alpha), 0f);
            PopFont(true);
        }

        /// <summary>
        /// Draws one admin action row.
        /// </summary>
        private static float DrawActionRow(
            FuDrawList drawList,
            Rect clipRect,
            float x,
            float y,
            float width,
            string label,
            string hint,
            string buttonLabel,
            bool activeState,
            TimelineWidgetTheme theme,
            float alpha,
            bool interactable,
            Action onClicked)
        {
            float scale = Fugui.Scale;
            Rect rect = new Rect(x, y, width, RowHeight * scale);
            if (IsVisible(rect, clipRect))
            {
                DrawRowTopDivider(drawList, rect, theme, alpha);

                Rect textRect = new Rect(rect.x + 22f * scale, rect.y, rect.width - 144f * scale, rect.height);
                DrawSettingText(drawList, textRect, label, hint, theme, alpha, interactable);

                Rect buttonRect = new Rect(rect.xMax - 112f * scale, rect.y + (rect.height - 32f * scale) * 0.5f, 90f * scale, 32f * scale);
                if (DrawPillButton(drawList, buttonRect, buttonLabel, activeState, theme, alpha, interactable && IsMouseInClip(clipRect)))
                    onClicked?.Invoke();
            }

            return rect.yMax;
        }

        /// <summary>
        /// Draws a pill button.
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
        /// Draws primary and secondary row text.
        /// </summary>
        private static void DrawSettingText(FuDrawList drawList, Rect rect, string label, string hint, TimelineWidgetTheme theme, float alpha, bool enabled)
        {
            float scale = Fugui.Scale;
            Color labelColor = enabled ? theme.Text : theme.TextDim;
            Color hintColor = enabled ? theme.TextFaint : WithAlpha(theme.TextFaint, 0.65f);

            PushFont(16, true);
            Rect labelRect = new Rect(rect.x, rect.y + 10f * scale, rect.width, 19f * scale);
            DrawTextLeftCentered(drawList, labelRect, ClipTextToWidth(label, labelRect.width), ColorU32(labelColor, alpha), 0f);
            PopFont(true);

            PushFont(12, false);
            Rect hintRect = new Rect(rect.x, labelRect.yMax + 1f * scale, rect.width, 16f * scale);
            DrawTextLeftCentered(drawList, hintRect, ClipTextToWidth(hint, hintRect.width), ColorU32(hintColor, alpha), 0f);
            PopFont(false);
        }

        /// <summary>
        /// Requests a seat swap from the server.
        /// </summary>
        private void SwapSeats(SeatType sourceSeat, SeatType targetSeat)
        {
            if (Sara.Network == null)
                return;

            _statusMessage = "Swapping seats...";
            Sara.Network.SwapSessionSeats(sourceSeat, targetSeat, HandleCommandResponse);
        }

        /// <summary>
        /// Requests a forced mute change from the server.
        /// </summary>
        private void SetUserMuted(uint clientId, bool muted)
        {
            if (Sara.Network == null)
                return;

            _statusMessage = muted ? "Muting user..." : "Unmuting user...";
            Sara.Network.SetSessionUserMuted(clientId, muted, HandleCommandResponse);
        }

        /// <summary>
        /// Requests a pointing permission change from the server.
        /// </summary>
        private void SetUserCanPoint(uint clientId, bool canPoint)
        {
            if (Sara.Network == null)
                return;

            _statusMessage = canPoint ? "Enabling pointer..." : "Disabling pointer...";
            Sara.Network.SetSessionUserCanPoint(clientId, canPoint, HandleCommandResponse);
        }

        /// <summary>
        /// Requests timeline control transfer from the server.
        /// </summary>
        private void SetTimelineController(uint clientId)
        {
            if (Sara.Network == null)
                return;

            _statusMessage = "Transferring timeline control...";
            Sara.Network.SetTimelineController(clientId, HandleCommandResponse);
        }

        /// <summary>
        /// Handles admin command replies.
        /// </summary>
        private void HandleCommandResponse(APIResponse response)
        {
            if (response == null)
            {
                _statusMessage = "Command failed.";
                return;
            }

            _statusMessage = response.Success
                ? "Updated."
                : string.IsNullOrWhiteSpace(response.Message) ? "Command rejected." : response.Message;
        }

        /// <summary>
        /// Returns the user occupying a seat.
        /// </summary>
        private static SaraUser GetSeatUser(SeatType seat, out uint clientId)
        {
            clientId = 0;
            SaraSession session = Sara.CurrentSession;
            if (session == null || session.Seats == null || !Seats.IsValidSeat(seat))
                return null;

            Seat sessionSeat = session.Seats[seat];
            if (sessionSeat == null || sessionSeat.OccupiedByClientID == 0)
                return null;

            clientId = sessionSeat.OccupiedByClientID;
            return session.TryGetUser(clientId, out SaraUser user) ? user : null;
        }

        /// <summary>
        /// Returns the current timeline owner display name.
        /// </summary>
        private static string GetTimelineOwnerName()
        {
            SaraSession session = Sara.CurrentSession;
            if (session == null || session.Users == null)
                return "None";

            for (int i = 0; i < session.Users.Length; i++)
            {
                SaraSessionUser sessionUser = session.Users[i];
                if (sessionUser != null && sessionUser.User != null && sessionUser.User.CanControlTimeline)
                    return GetUserName(sessionUser.User);
            }

            return "None";
        }

        /// <summary>
        /// Counts connected observer users in the current session.
        /// </summary>
        private static int CountObservers()
        {
            SaraSession session = Sara.CurrentSession;
            if (session == null || session.Users == null)
                return 0;

            int count = 0;
            for (int i = 0; i < session.Users.Length; i++)
            {
                SaraSessionUser sessionUser = session.Users[i];
                if (sessionUser != null && sessionUser.User != null && sessionUser.User.IsObservator)
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Returns whether the local user may see admin controls.
        /// </summary>
        private static bool IsAdminSession()
        {
            SaraUser user = Sara.CurrentSession != null ? Sara.CurrentSession.User : null;
            return Sara.CurrentSession != null
                && Sara.CurrentSession.IsMultiplayer
                && user != null
                && user.IsAdmin;
        }

        /// <summary>
        /// Returns a display name for a user.
        /// </summary>
        private static string GetUserName(SaraUser user)
        {
            if (user == null)
                return "Unknown";

            if (!string.IsNullOrWhiteSpace(user.Name))
                return user.Name.Trim();

            return GetRoleLabel(user.Role);
        }

        /// <summary>
        /// Returns a display label for a seat.
        /// </summary>
        private static string GetSeatLabel(SeatType seat)
        {
            switch (seat)
            {
                case SeatType.Pilot:
                    return "Left";
                case SeatType.Center:
                    return "Center";
                case SeatType.CoPilot:
                    return "Right";
                default:
                    return "Seat";
            }
        }

        /// <summary>
        /// Returns a display label for a role.
        /// </summary>
        private static string GetRoleLabel(SaraUserRole role)
        {
            switch (role)
            {
                case SaraUserRole.Admin:
                    return "Admin";
                case SaraUserRole.Captain:
                    return "Captain";
                case SaraUserRole.FirstOfficer:
                    return "First Officer";
                case SaraUserRole.Observator:
                    return "Observer";
                default:
                    return "User";
            }
        }

        /// <summary>
        /// Draws a top divider for a row.
        /// </summary>
        private static void DrawRowTopDivider(FuDrawList drawList, Rect rect, TimelineWidgetTheme theme, float alpha)
        {
            drawList.AddLine(rect.min, new Vector2(rect.xMax, rect.y), ColorU32(theme.SettingsRowDivider, alpha), Mathf.Max(1f, Fugui.Scale));
        }

        /// <summary>
        /// Draws the observer unmute-request indicator dot.
        /// </summary>
        private static void DrawRequestDot(FuDrawList drawList, Vector2 center, float alpha)
        {
            float scale = Fugui.Scale;
            drawList.AddCircleFilled(center, 5f * scale, ColorU32(new Color(1f, 0.13f, 0.13f, 1f), alpha), 16);
            drawList.AddCircle(center, 5f * scale, ColorU32(new Color(1f, 1f, 1f, 0.70f), alpha), 16, Mathf.Max(1f, scale));
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
        /// Returns whether a row is visible.
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
        /// Converts a color to a packed Fugui color.
        /// </summary>
        private static uint ColorU32(Color color)
        {
            return Fugui.GetColorU32(new Vector4(color.r, color.g, color.b, color.a));
        }

        /// <summary>
        /// Converts a color and opacity to a packed Fugui color.
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
