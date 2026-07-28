using ImGuiNET;
using UnityEngine;
using System;
using Fu;

namespace Fu.Framework
{
    /// <summary>
    /// Represents the Fu Layout type.
    /// </summary>
    public partial class FuLayout
    {
        #region State
        private static readonly string[] _monthStr = new string[] {
            "January", "February", "March", "April", "May", "June",
            "July", "August", "September", "October", "November", "December" };
        private static readonly string[] _monthShortStr = new string[] {
            "Jan", "Feb", "Mar", "Apr", "May", "Jun",
            "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        private static readonly string[] _dayStr = new string[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
        private static DateTime _currentDateTimeValue;
        private static DateTime _updatedDateTime;
        private static bool _datetimeUpdated = false;
        private const int DatePickerStateCacheCapacity = 512;
        private static readonly FuBoundedCache<string, FuDatePickerState> _datePickerStates =
            new FuBoundedCache<string, FuDatePickerState>(DatePickerStateCacheCapacity, StringComparer.Ordinal);
        #endregion

        #region Methods
        /// <summary>
        /// Returns the date time picker popup result.
        /// </summary>
        /// <param name="text">The text value.</param>
        /// <param name="currentDate">The current Date value.</param>
        /// <returns>The result of the operation.</returns>
        public bool DateTimePickerPopup(string text, ref DateTime currentDate)
        {
            string ppID = "dtPkr" + text;
            _datetimeUpdated = false;
            _currentDateTimeValue = currentDate;

            FuDatePickerState popupState = GetDatePickerState(text, currentDate.Date);
            string dtValue = popupState.GetPopupLabel(currentDate);
            if (Button(dtValue))
            {
                Fugui.OpenPopUp(ppID, drawPicker);
            }

            void drawPicker()
            {
                Spacing();
                Spacing();
                SameLine();
                BeginGroup();
                try
                {
                    _datetimeUpdated = DateTimePicker(text, ref _currentDateTimeValue);
                    if (_datetimeUpdated)
                    {
                        Fugui.ClosePopup(ppID);
                    }
                }
                finally
                {
                    EndGroup();
                }
                SameLine();
                Spacing();
                Spacing();
            }

            Fugui.DrawPopup(ppID);
            currentDate = _currentDateTimeValue;

            return _datetimeUpdated;
        }

        /// <summary>
        /// Returns the date time picker result.
        /// </summary>
        /// <param name="text">The text value.</param>
        /// <param name="currentDate">The current Date value.</param>
        /// <returns>The result of the operation.</returns>
        public bool DateTimePicker(string text, ref DateTime currentDate)
        {
            _datetimeUpdated = false;
            beginElement(ref text);
            if (!_drawElement)
            {
                return false;
            }

            _currentDateTimeValue = currentDate;
            _updatedDateTime = _currentDateTimeValue;

            DateTime selectedDate = currentDate.Date;
            FuDatePickerState pickerState = GetDatePickerState(text, selectedDate);
            DateTime viewDate = pickerState.ViewDate;
            DateTime today = DateTime.Today;

            float scale = Fugui.CurrentContext.Scale;
            float panelPadding = 8f * scale;
            float itemGap = 4f * scale;
            float navButtonSize = 32f * scale;
            float daySize = Mathf.Max(30f * scale, ImGui.CalcTextSize("88").x + 16f * scale);
            float weekdayHeight = 18f * scale;
            float calendarWidth = daySize * 7f + itemGap * 6f;
            float headerHeight = navButtonSize;
            float headerGap = 8f * scale;
            float gridHeight = weekdayHeight + itemGap + daySize * 6f + itemGap * 5f;
            float footerHeight = navButtonSize;
            float footerGap = 8f * scale;
            Vector2 panelSize = new Vector2(calendarWidth + panelPadding * 2f, panelPadding * 2f + headerHeight + headerGap + gridHeight + footerGap + footerHeight);
            Vector2 panelPos = ImGui.GetCursorScreenPos();
            Vector2 innerPos = panelPos + new Vector2(panelPadding, panelPadding);
            FuDrawList drawList = Fugui.GetCurrentWindowDrawList();

            drawList.AddRectFilled(panelPos, panelPos + panelSize, ImGui.GetColorU32(Fugui.GetColor(FuColors.FrameBg, 0.92f)), Fugui.Themes.FrameRounding * scale);
            drawList.AddRect(panelPos, panelPos + panelSize, ImGui.GetColorU32(Fugui.GetColor(FuColors.Border, 0.9f)), Fugui.Themes.FrameRounding * scale);

            drawDatePickerHeader(pickerState, ref viewDate, innerPos, calendarWidth, navButtonSize, itemGap);
            pickerState.SetViewDate(viewDate);

            Vector2 gridPos = innerPos + new Vector2(0f, headerHeight + headerGap);
            drawDatePickerGrid(pickerState, ref selectedDate, ref viewDate, today, gridPos, daySize, itemGap, weekdayHeight);

            Vector2 footerPos = gridPos + new Vector2(0f, gridHeight + footerGap);
            drawDatePickerFooter(pickerState, today, ref selectedDate, ref viewDate, footerPos, calendarWidth, navButtonSize);

            ImGui.SetCursorScreenPos(panelPos + panelSize - Vector2.one * scale);
            ImGui.Dummy(Vector2.one * scale);

            setBaseElementState(text, panelPos, panelSize, false, _datetimeUpdated);

            endElement();
            if (_datetimeUpdated)
            {
                currentDate = new DateTime(_updatedDateTime.Year, _updatedDateTime.Month, _updatedDateTime.Day, 0, 0, 0, currentDate.Kind).Add(currentDate.TimeOfDay);
                _currentDateTimeValue = currentDate;
                pickerState.SetSelectedDate(currentDate.Date);
                pickerState.SetViewDate(currentDate);
            }

            return _datetimeUpdated;
        }

        /// <summary>
        /// Gets or creates the bounded persistent state for one date picker.
        /// </summary>
        /// <param name="id">Stable picker identifier.</param>
        /// <param name="selectedDate">Selection supplied by the caller.</param>
        /// <returns>Reusable picker state.</returns>
        private static FuDatePickerState GetDatePickerState(string id, DateTime selectedDate)
        {
            // A changed external selection recenters the view exactly like first initialization.
            if (!_datePickerStates.TryGetValue(id, out FuDatePickerState state))
            {
                state = new FuDatePickerState(id, selectedDate);
                _datePickerStates.Set(id, state);
            }
            else if (state.SelectedDate.Date != selectedDate.Date)
            {
                state.SetSelectedDate(selectedDate);
                state.SetViewDate(selectedDate);
            }

            return state;
        }

        /// <summary>
        /// Draws the month navigation header of a date picker.
        /// </summary>
        /// <param name="state">Persistent picker state.</param>
        /// <param name="viewDate">Month currently displayed.</param>
        /// <param name="pos">Header screen position.</param>
        /// <param name="width">Header width.</param>
        /// <param name="navButtonSize">Navigation button size.</param>
        /// <param name="gap">Horizontal control gap.</param>
        private void drawDatePickerHeader(FuDatePickerState state, ref DateTime viewDate, Vector2 pos, float width, float navButtonSize, float gap)
        {
            DateTime workingViewDate = viewDate;
            float scale = Fugui.CurrentContext.Scale;
            float titleWidth = Mathf.Min(184f * scale, width - navButtonSize * 2f - gap * 2f);
            Vector2 navSize = new Vector2(navButtonSize, navButtonSize);

            ImGui.SetCursorScreenPos(pos);
            if (drawDatePickerIconButton(state.PreviousMonthId, pos, navSize, ImGuiDir.Left))
            {
                workingViewDate = workingViewDate.AddMonths(-1);
                state.SetViewDate(workingViewDate);
            }

            Vector2 titlePos = pos + new Vector2((width - titleWidth) * 0.5f, 0f);
            if (drawDatePickerTitleButton(state.TitleId, state.TitleLabel, titlePos, new Vector2(titleWidth, navButtonSize)))
            {
                state.SetViewDate(workingViewDate);
                Fugui.OpenPopUp(state.MonthYearPopupId, () => drawDatePickerMonthYearPopup(state));
            }

            Vector2 nextPos = pos + new Vector2(width - navButtonSize, 0f);
            if (drawDatePickerIconButton(state.NextMonthId, nextPos, navSize, ImGuiDir.Right))
            {
                workingViewDate = workingViewDate.AddMonths(1);
                state.SetViewDate(workingViewDate);
            }

            Vector2 popupSize = new Vector2(226f * scale, 180f * scale);
            Fugui.DrawPopup(state.MonthYearPopupId, popupSize, titlePos + new Vector2(0f, navButtonSize + 6f * scale));
            viewDate = state.ViewDate;
        }

        /// <summary>
        /// Draws the fixed six-week grid using identifiers cached for the displayed month.
        /// </summary>
        /// <param name="state">Persistent picker state.</param>
        /// <param name="selectedDate">Currently selected date.</param>
        /// <param name="viewDate">Month currently displayed.</param>
        /// <param name="today">Current local date.</param>
        /// <param name="pos">Grid screen position.</param>
        /// <param name="daySize">Square day cell size.</param>
        /// <param name="gap">Cell gap.</param>
        /// <param name="weekdayHeight">Weekday header height.</param>
        private void drawDatePickerGrid(FuDatePickerState state, ref DateTime selectedDate, ref DateTime viewDate, DateTime today, Vector2 pos, float daySize, float gap, float weekdayHeight)
        {
            FuDrawList drawList = Fugui.GetCurrentWindowDrawList();
            Vector2 cellSize = new Vector2(daySize, daySize);

            for (int i = 0; i < _dayStr.Length; i++)
            {
                Vector2 weekdayPos = pos + new Vector2(i * (daySize + gap), 0f);
                drawCenteredText(drawList, _dayStr[i], weekdayPos, new Vector2(daySize, weekdayHeight), ImGui.GetColorU32(Fugui.GetColor(FuColors.TextDisabled, 0.82f)));
            }

            Vector2 firstDayPos = pos + new Vector2(0f, weekdayHeight + gap);

            for (int row = 0; row < 6; row++)
            {
                for (int column = 0; column < 7; column++)
                {
                    int cellIndex = row * 7 + column;
                    DateTime cellDate = state.CellDates[cellIndex];
                    Vector2 cellPos = firstDayPos + new Vector2(column * (daySize + gap), row * (daySize + gap));
                    bool inCurrentMonth = cellDate.Month == viewDate.Month && cellDate.Year == viewDate.Year;
                    bool selected = cellDate.Date == selectedDate.Date;
                    bool isToday = cellDate.Date == today.Date;
                    bool hovered = !LastItemDisabled && IsItemHovered(cellPos, cellSize);

                    drawDatePickerDayBackground(drawList, cellPos, cellSize, selected, isToday, hovered);

                    Vector4 textColor = LastItemDisabled
                        ? Fugui.GetColor(FuColors.TextDisabled, 0.72f)
                        : Fugui.GetColor(inCurrentMonth ? FuColors.Text : FuColors.TextDisabled, inCurrentMonth ? 1f : 0.62f);
                    if (selected && !LastItemDisabled)
                    {
                        textColor = Fugui.GetColor(FuColors.SelectedText);
                    }

                    Fugui.Push(ImGuiCol.Text, textColor);
                    if (selected || isToday)
                    {
                        Fugui.PushFont(FontType.Bold);
                    }

                    ImGui.SetCursorScreenPos(cellPos);
                    if (_customButton(state.CellIds[cellIndex], cellSize, Vector2.zero, Vector2.zero, FuButtonStyle.Transparent, 0f, false, 0.5f))
                    {
                        selectedDate = cellDate.Date;
                        viewDate = new DateTime(cellDate.Year, cellDate.Month, 1);
                        state.SetSelectedDate(selectedDate);
                        _updatedDateTime = selectedDate;
                        _datetimeUpdated = true;
                    }

                    if (selected || isToday)
                    {
                        Fugui.PopFont();
                    }
                    Fugui.PopColor();
                }
            }

            ImGui.SetCursorScreenPos(pos + new Vector2(0f, weekdayHeight + gap + daySize * 6f + gap * 5f));
        }

        /// <summary>
        /// Draws the selected date label and the shortcut to today's date.
        /// </summary>
        /// <param name="state">Persistent picker state.</param>
        /// <param name="today">Current local date.</param>
        /// <param name="selectedDate">Currently selected date.</param>
        /// <param name="viewDate">Month currently displayed.</param>
        /// <param name="pos">Footer screen position.</param>
        /// <param name="width">Footer width.</param>
        /// <param name="height">Footer height.</param>
        private void drawDatePickerFooter(FuDatePickerState state, DateTime today, ref DateTime selectedDate, ref DateTime viewDate, Vector2 pos, float width, float height)
        {
            FuDrawList drawList = Fugui.GetCurrentWindowDrawList();
            string selectedText = state.SelectedLabel;
            Vector2 textSize = ImGui.CalcTextSize(selectedText);
            Vector2 textPos = pos + new Vector2(0f, height * 0.5f - textSize.y * 0.5f);
            drawList.AddText(textPos, ImGui.GetColorU32(Fugui.GetColor(FuColors.TextDisabled, 0.86f)), selectedText);

            Vector2 todaySize = new Vector2(62f * Fugui.CurrentContext.Scale, height);
            if (drawDatePickerTextButton(state.TodayId, "Today", pos + new Vector2(width - todaySize.x, 0f), todaySize, true, true))
            {
                selectedDate = today.Date;
                viewDate = new DateTime(today.Year, today.Month, 1);
                state.SetSelectedDate(selectedDate);
                state.SetViewDate(viewDate);
                _updatedDateTime = selectedDate;
                _datetimeUpdated = true;
            }
        }

        /// <summary>
        /// Draws selection, hover and today feedback behind one calendar day.
        /// </summary>
        /// <param name="drawList">Destination draw list.</param>
        /// <param name="pos">Cell screen position.</param>
        /// <param name="size">Cell size.</param>
        /// <param name="selected">Whether the day is selected.</param>
        /// <param name="today">Whether the cell represents today.</param>
        /// <param name="hovered">Whether the pointer is over the cell.</param>
        private static void drawDatePickerDayBackground(FuDrawList drawList, Vector2 pos, Vector2 size, bool selected, bool today, bool hovered)
        {
            float rounding = Mathf.Min(8f * Fugui.CurrentContext.Scale, size.y * 0.35f);
            if (selected)
            {
                drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(Fugui.GetColor(FuColors.Selected)), rounding);
                return;
            }

            if (hovered)
            {
                drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(Fugui.GetColor(FuColors.HeaderHovered, 0.72f)), rounding);
            }

            if (today)
            {
                Vector2 inset = Vector2.one * (1.5f * Fugui.CurrentContext.Scale);
                drawList.AddRect(pos + inset, pos + size - inset, ImGui.GetColorU32(Fugui.GetColor(FuColors.Highlight, 0.95f)), rounding, FuDrawFlags.RoundCornersDefault, 1.4f * Fugui.CurrentContext.Scale);
            }
        }

        /// <summary>
        /// Draws text centered within a screen-space rectangle.
        /// </summary>
        /// <param name="drawList">Destination draw list.</param>
        /// <param name="text">Text to draw.</param>
        /// <param name="pos">Rectangle screen position.</param>
        /// <param name="size">Rectangle size.</param>
        /// <param name="color">Packed text color.</param>
        private static void drawCenteredText(FuDrawList drawList, string text, Vector2 pos, Vector2 size, uint color)
        {
            Vector2 textSize = ImGui.CalcTextSize(text);
            Vector2 textPos = pos + new Vector2((size.x - textSize.x) * 0.5f, (size.y - textSize.y) * 0.5f);
            drawList.AddText(textPos, color, text);
        }

        /// <summary>
        /// Draws the month and year selector popup for one date picker.
        /// </summary>
        /// <param name="state">Persistent picker state.</param>
        private void drawDatePickerMonthYearPopup(FuDatePickerState state)
        {
            DateTime viewDate = state.ViewDate;
            float scale = Fugui.CurrentContext.Scale;
            float padding = 8f * scale;
            float gap = 6f * scale;
            float width = 210f * scale;
            float yearButtonSize = 28f * scale;
            Vector2 startPos = ImGui.GetCursorScreenPos() + new Vector2(padding, padding);

            if (drawDatePickerIconButton(state.PreviousYearId, startPos, new Vector2(yearButtonSize, yearButtonSize), ImGuiDir.Left))
            {
                viewDate = viewDate.AddYears(-1);
                state.SetViewDate(viewDate);
            }

            if (drawDatePickerTextButton(state.YearId, state.YearLabel, startPos + new Vector2(yearButtonSize + gap, 0f), new Vector2(width - yearButtonSize * 2f - gap * 2f, yearButtonSize), true, true))
            {
                viewDate = new DateTime(DateTime.Today.Year, viewDate.Month, 1);
                state.SetViewDate(viewDate);
            }

            if (drawDatePickerIconButton(state.NextYearId, startPos + new Vector2(width - yearButtonSize, 0f), new Vector2(yearButtonSize, yearButtonSize), ImGuiDir.Right))
            {
                viewDate = viewDate.AddYears(1);
                state.SetViewDate(viewDate);
            }

            Vector2 monthStartPos = startPos + new Vector2(0f, yearButtonSize + gap * 1.4f);
            Vector2 monthButtonSize = new Vector2((width - gap * 2f) / 3f, 26f * scale);
            for (int i = 0; i < _monthStr.Length; i++)
            {
                int column = i % 3;
                int row = i / 3;
                Vector2 buttonPos = monthStartPos + new Vector2(column * (monthButtonSize.x + gap), row * (monthButtonSize.y + gap));
                bool selected = viewDate.Month == i + 1;
                if (drawDatePickerTextButton(state.MonthIds[i], _monthShortStr[i], buttonPos, monthButtonSize, selected, false))
                {
                    viewDate = new DateTime(viewDate.Year, i + 1, 1);
                    state.SetViewDate(viewDate);
                    Fugui.ForceCloseOpenPopup();
                }
            }

            ImGui.SetCursorScreenPos(startPos + new Vector2(0f, yearButtonSize + gap * 1.4f + monthButtonSize.y * 4f + gap * 3f));
            ImGui.Dummy(Vector2.one * scale);
        }

        /// <summary>
        /// Draws an invisible interaction button with a chevron icon.
        /// </summary>
        /// <param name="id">Stable ImGui identifier.</param>
        /// <param name="pos">Button screen position.</param>
        /// <param name="size">Button size.</param>
        /// <param name="direction">Chevron direction.</param>
        /// <returns>True when the button was clicked.</returns>
        private bool drawDatePickerIconButton(string id, Vector2 pos, Vector2 size, ImGuiDir direction)
        {
            FuDrawList drawList = Fugui.GetCurrentWindowDrawList();
            ImGui.SetCursorScreenPos(pos);
            bool clicked = InvisibleInteraction(id, size, out bool hovered, out bool active, FuButtonFlags.MouseButtonLeft, !LastItemDisabled);
            float rounding = Mathf.Min(8f * Fugui.CurrentContext.Scale, size.y * 0.45f);

            if (active)
            {
                drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(Fugui.GetColor(FuColors.HeaderActive, 0.85f)), rounding);
            }
            else if (hovered)
            {
                drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(Fugui.GetColor(FuColors.HeaderHovered, 0.8f)), rounding);
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            drawChevron(drawList, pos, size, direction, ImGui.GetColorU32(Fugui.GetColor(LastItemDisabled ? FuColors.TextDisabled : FuColors.Text, LastItemDisabled ? 0.45f : 0.9f)));
            return clicked;
        }

        /// <summary>
        /// Draws the month and year title button.
        /// </summary>
        /// <param name="id">Stable ImGui identifier.</param>
        /// <param name="title">Month and year label.</param>
        /// <param name="pos">Button screen position.</param>
        /// <param name="size">Button size.</param>
        /// <returns>True when the button was clicked.</returns>
        private bool drawDatePickerTitleButton(string id, string title, Vector2 pos, Vector2 size)
        {
            FuDrawList drawList = Fugui.GetCurrentWindowDrawList();
            ImGui.SetCursorScreenPos(pos);
            bool clicked = InvisibleInteraction(id, size, out bool hovered, out bool active, FuButtonFlags.MouseButtonLeft, !LastItemDisabled);
            float rounding = Mathf.Min(10f * Fugui.CurrentContext.Scale, size.y * 0.45f);
            Vector4 bg = active
                ? Fugui.GetColor(FuColors.HeaderActive, 0.95f)
                : hovered
                    ? Fugui.GetColor(FuColors.HeaderHovered, 0.82f)
                    : Fugui.GetColor(FuColors.Header, 0.48f);

            drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(bg), rounding);
            drawList.AddRect(pos, pos + size, ImGui.GetColorU32(Fugui.GetColor(FuColors.Border, 0.55f)), rounding);

            if (hovered)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            Fugui.PushFont(FontType.Bold);
            Vector2 textSize = ImGui.CalcTextSize(title);
            Vector2 textPos = pos + new Vector2((size.x - textSize.x) * 0.5f - 5f * Fugui.CurrentContext.Scale, (size.y - textSize.y) * 0.5f);
            drawList.AddText(textPos, ImGui.GetColorU32(Fugui.GetColor(LastItemDisabled ? FuColors.TextDisabled : FuColors.Text, LastItemDisabled ? 0.55f : 0.96f)), title);
            Fugui.PopFont();

            Vector2 caretPos = pos + new Vector2(size.x - 18f * Fugui.CurrentContext.Scale, 0f);
            Fugui.DrawCarret_Down(drawList, caretPos, 7f * Fugui.CurrentContext.Scale, size.y, Fugui.GetColor(LastItemDisabled ? FuColors.TextDisabled : FuColors.Text, 0.72f));
            return clicked;
        }

        /// <summary>
        /// Draws a date-picker text button with optional selection emphasis.
        /// </summary>
        /// <param name="id">Stable ImGui identifier.</param>
        /// <param name="label">Visible button label.</param>
        /// <param name="pos">Button screen position.</param>
        /// <param name="size">Button size.</param>
        /// <param name="selected">Whether the button is selected.</param>
        /// <param name="bold">Whether the label uses the bold font.</param>
        /// <returns>True when the button was clicked.</returns>
        private bool drawDatePickerTextButton(string id, string label, Vector2 pos, Vector2 size, bool selected, bool bold)
        {
            FuDrawList drawList = Fugui.GetCurrentWindowDrawList();
            ImGui.SetCursorScreenPos(pos);
            bool clicked = InvisibleInteraction(id, size, out bool hovered, out bool active, FuButtonFlags.MouseButtonLeft, !LastItemDisabled);
            float rounding = Mathf.Min(8f * Fugui.CurrentContext.Scale, size.y * 0.45f);

            if (selected)
            {
                drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(Fugui.GetColor(FuColors.Selected, LastItemDisabled ? 0.35f : 1f)), rounding);
            }
            else if (active)
            {
                drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(Fugui.GetColor(FuColors.HeaderActive, 0.84f)), rounding);
            }
            else if (hovered)
            {
                drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(Fugui.GetColor(FuColors.HeaderHovered, 0.76f)), rounding);
            }

            if (hovered)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            if (bold || selected)
            {
                Fugui.PushFont(FontType.Bold);
            }

            uint textColor = ImGui.GetColorU32(selected && !LastItemDisabled ? Fugui.GetColor(FuColors.SelectedText) : Fugui.GetColor(LastItemDisabled ? FuColors.TextDisabled : FuColors.Text, LastItemDisabled ? 0.55f : 0.92f));
            drawCenteredText(drawList, label, pos, size, textColor);

            if (bold || selected)
            {
                Fugui.PopFont();
            }

            return clicked;
        }

        /// <summary>
        /// Draws a left or right chevron centered in a rectangle.
        /// </summary>
        /// <param name="drawList">Destination draw list.</param>
        /// <param name="pos">Rectangle screen position.</param>
        /// <param name="size">Rectangle size.</param>
        /// <param name="direction">Chevron direction.</param>
        /// <param name="color">Packed line color.</param>
        private static void drawChevron(FuDrawList drawList, Vector2 pos, Vector2 size, ImGuiDir direction, uint color)
        {
            float scale = Fugui.CurrentContext.Scale;
            Vector2 center = pos + size * 0.5f;
            float w = Mathf.Max(4f * scale, size.x * 0.16f);
            float h = Mathf.Max(6f * scale, size.y * 0.22f);
            float thickness = Mathf.Max(1.4f * scale, 1f);

            if (direction == ImGuiDir.Left)
            {
                Vector2 p1 = center + new Vector2(w * 0.5f, -h);
                Vector2 p2 = center + new Vector2(-w * 0.5f, 0f);
                Vector2 p3 = center + new Vector2(w * 0.5f, h);
                drawList.AddLine(p1, p2, color, thickness);
                drawList.AddLine(p2, p3, color, thickness);
            }
            else
            {
                Vector2 p1 = center + new Vector2(-w * 0.5f, -h);
                Vector2 p2 = center + new Vector2(w * 0.5f, 0f);
                Vector2 p3 = center + new Vector2(-w * 0.5f, h);
                drawList.AddLine(p1, p2, color, thickness);
                drawList.AddLine(p2, p3, color, thickness);
            }
        }

        /// <summary>
        /// Clears date picker state owned by the current Fugui session.
        /// </summary>
        internal static void ResetDatePickerState()
        {
            // Cached labels, identifiers and dates all belong to the disposed Fugui session.
            _datePickerStates.Clear();
        }

        private sealed class FuDatePickerState
        {
            public readonly string PreviousMonthId;
            public readonly string TitleId;
            public readonly string MonthYearPopupId;
            public readonly string NextMonthId;
            public readonly string TodayId;
            public readonly string PreviousYearId;
            public readonly string YearId;
            public readonly string NextYearId;
            public readonly string[] MonthIds = new string[12];
            public readonly DateTime[] CellDates = new DateTime[42];
            public readonly string[] CellIds = new string[42];
            public DateTime ViewDate { get; private set; }
            public DateTime SelectedDate { get; private set; }
            public string TitleLabel { get; private set; }
            public string YearLabel { get; private set; }
            public string SelectedLabel { get; private set; }
            private DateTime _popupLabelDate;
            private string _popupLabel;

            /// <summary>
            /// Creates persistent picker data and every control identifier that never changes.
            /// </summary>
            /// <param name="id">Stable picker identifier.</param>
            /// <param name="selectedDate">Initial selected date.</param>
            public FuDatePickerState(string id, DateTime selectedDate)
            {
                // Static control IDs are composed once for the bounded cache entry lifetime.
                PreviousMonthId = id + "PrevMonth";
                TitleId = id + "Title";
                MonthYearPopupId = "DatePickerMonthYear" + id;
                NextMonthId = id + "NextMonth";
                TodayId = id + "Today";
                PreviousYearId = id + "PopupPrevYear";
                YearId = id + "PopupYear";
                NextYearId = id + "PopupNextYear";
                for (int i = 0; i < MonthIds.Length; i++)
                {
                    MonthIds[i] = id + "PopupMonth" + i;
                }

                SetSelectedDate(selectedDate);
                SetViewDate(selectedDate);
            }

            /// <summary>
            /// Updates the selected date and its stable display label.
            /// </summary>
            /// <param name="date">New selected date.</param>
            public void SetSelectedDate(DateTime date)
            {
                // Formatting occurs only when the externally visible calendar day changes.
                DateTime normalizedDate = date.Date;
                if (SelectedLabel != null && SelectedDate == normalizedDate)
                {
                    return;
                }

                SelectedDate = normalizedDate;
                SelectedLabel = normalizedDate.ToString("d MMM yyyy");
            }

            /// <summary>
            /// Updates the displayed month and rebuilds its 42 cached day-cell identifiers.
            /// </summary>
            /// <param name="date">Date contained by the month to display.</param>
            public void SetViewDate(DateTime date)
            {
                // Month-dependent strings remain stable across every frame that shows the same month.
                DateTime normalizedDate = new DateTime(date.Year, date.Month, 1);
                if (TitleLabel != null && ViewDate == normalizedDate)
                {
                    return;
                }

                ViewDate = normalizedDate;
                TitleLabel = _monthStr[normalizedDate.Month - 1] + " " + normalizedDate.Year;
                YearLabel = normalizedDate.Year.ToString();

                DateTime firstCellDate = normalizedDate.AddDays(-(int)normalizedDate.DayOfWeek);
                for (int i = 0; i < CellDates.Length; i++)
                {
                    DateTime cellDate = firstCellDate.AddDays(i);
                    CellDates[i] = cellDate;
                    CellIds[i] = cellDate.Day + "##" + TitleId + cellDate.ToString("yyyyMMdd");
                }
            }

            /// <summary>
            /// Gets the popup button label without formatting it again while the date is unchanged.
            /// </summary>
            /// <param name="date">Date displayed by the popup trigger.</param>
            /// <returns>Cached formatted date.</returns>
            public string GetPopupLabel(DateTime date)
            {
                // The popup label omits time, so a day-level comparison is sufficient.
                DateTime normalizedDate = date.Date;
                if (_popupLabel == null || _popupLabelDate != normalizedDate)
                {
                    _popupLabelDate = normalizedDate;
                    _popupLabel = normalizedDate.ToString("ddd, MMM d yyyy");
                }

                return _popupLabel;
            }
        }
        #endregion
    }
}
