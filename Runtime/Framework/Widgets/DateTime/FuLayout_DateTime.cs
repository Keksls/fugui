using ImGuiNET;
using UnityEngine;
using System;
using System.Collections.Generic;
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
        private static readonly string[] _dayStr = new string[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
        private static DateTime _currentDateTimeValue;
        private static DateTime _updatedDateTime;
        private static bool _datetimeUpdated = false;
        private static Dictionary<string, DateTime> _datePickerViewDates = new Dictionary<string, DateTime>();
        private static Dictionary<string, DateTime> _datePickerSelectedDates = new Dictionary<string, DateTime>();
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

            string dtValue = currentDate.ToString("ddd, MMM d yyyy");
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
                _datetimeUpdated = DateTimePicker(text, ref _currentDateTimeValue);
                if (_datetimeUpdated)
                {
                    Fugui.ClosePopup(ppID);
                }
                EndGroup();
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
            DateTime viewDate = getDatePickerViewDate(text, selectedDate);
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
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            drawList.AddRectFilled(panelPos, panelPos + panelSize, ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.FrameBg, 0.92f)), Fugui.Themes.FrameRounding * scale);
            drawList.AddRect(panelPos, panelPos + panelSize, ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.Border, 0.9f)), Fugui.Themes.FrameRounding * scale);

            drawDatePickerHeader(text, ref viewDate, innerPos, calendarWidth, navButtonSize, itemGap);
            _datePickerViewDates[text] = new DateTime(viewDate.Year, viewDate.Month, 1);

            Vector2 gridPos = innerPos + new Vector2(0f, headerHeight + headerGap);
            drawDatePickerGrid(text, ref selectedDate, ref viewDate, today, gridPos, daySize, itemGap, weekdayHeight);

            Vector2 footerPos = gridPos + new Vector2(0f, gridHeight + footerGap);
            drawDatePickerFooter(text, today, ref selectedDate, ref viewDate, footerPos, calendarWidth, navButtonSize);

            ImGui.SetCursorScreenPos(panelPos + panelSize - Vector2.one * scale);
            ImGui.Dummy(Vector2.one * scale);

            setBaseElementState(text, panelPos, panelSize, false, _datetimeUpdated);

            endElement();
            if (_datetimeUpdated)
            {
                currentDate = new DateTime(_updatedDateTime.Year, _updatedDateTime.Month, _updatedDateTime.Day, 0, 0, 0, currentDate.Kind).Add(currentDate.TimeOfDay);
                _currentDateTimeValue = currentDate;
                _datePickerSelectedDates[text] = currentDate.Date;
                _datePickerViewDates[text] = new DateTime(currentDate.Year, currentDate.Month, 1);
            }

            return _datetimeUpdated;
        }

        private DateTime getDatePickerViewDate(string id, DateTime selectedDate)
        {
            if (!_datePickerViewDates.TryGetValue(id, out DateTime viewDate) ||
                !_datePickerSelectedDates.TryGetValue(id, out DateTime storedSelectedDate) ||
                storedSelectedDate.Date != selectedDate.Date)
            {
                viewDate = new DateTime(selectedDate.Year, selectedDate.Month, 1);
                _datePickerViewDates[id] = viewDate;
                _datePickerSelectedDates[id] = selectedDate.Date;
            }
            return viewDate;
        }

        private void drawDatePickerHeader(string id, ref DateTime viewDate, Vector2 pos, float width, float navButtonSize, float gap)
        {
            DateTime workingViewDate = viewDate;
            float scale = Fugui.CurrentContext.Scale;
            float titleWidth = Mathf.Min(184f * scale, width - navButtonSize * 2f - gap * 2f);
            Vector2 navSize = new Vector2(navButtonSize, navButtonSize);
            string popupID = "DatePickerMonthYear" + id;

            ImGui.SetCursorScreenPos(pos);
            if (drawDatePickerIconButton(id + "PrevMonth", pos, navSize, ImGuiDir.Left))
            {
                workingViewDate = workingViewDate.AddMonths(-1);
                _datePickerViewDates[id] = new DateTime(workingViewDate.Year, workingViewDate.Month, 1);
            }

            Vector2 titlePos = pos + new Vector2((width - titleWidth) * 0.5f, 0f);
            if (drawDatePickerTitleButton(id + "Title", _monthStr[workingViewDate.Month - 1] + " " + workingViewDate.Year, titlePos, new Vector2(titleWidth, navButtonSize)))
            {
                _datePickerViewDates[id] = new DateTime(workingViewDate.Year, workingViewDate.Month, 1);
                Fugui.OpenPopUp(popupID, () => drawDatePickerMonthYearPopup(id));
            }

            Vector2 nextPos = pos + new Vector2(width - navButtonSize, 0f);
            if (drawDatePickerIconButton(id + "NextMonth", nextPos, navSize, ImGuiDir.Right))
            {
                workingViewDate = workingViewDate.AddMonths(1);
                _datePickerViewDates[id] = new DateTime(workingViewDate.Year, workingViewDate.Month, 1);
            }

            Vector2 popupSize = new Vector2(226f * scale, 180f * scale);
            Fugui.DrawPopup(popupID, popupSize, titlePos + new Vector2(0f, navButtonSize + 6f * scale));
            viewDate = _datePickerViewDates.TryGetValue(id, out DateTime storedViewDate) ? storedViewDate : workingViewDate;
        }

        private void drawDatePickerGrid(string id, ref DateTime selectedDate, ref DateTime viewDate, DateTime today, Vector2 pos, float daySize, float gap, float weekdayHeight)
        {
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 cellSize = new Vector2(daySize, daySize);

            for (int i = 0; i < _dayStr.Length; i++)
            {
                Vector2 weekdayPos = pos + new Vector2(i * (daySize + gap), 0f);
                drawCenteredText(drawList, _dayStr[i], weekdayPos, new Vector2(daySize, weekdayHeight), ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.TextDisabled, 0.82f)));
            }

            DateTime monthStart = new DateTime(viewDate.Year, viewDate.Month, 1);
            DateTime firstCellDate = monthStart.AddDays(-(int)monthStart.DayOfWeek);
            Vector2 firstDayPos = pos + new Vector2(0f, weekdayHeight + gap);

            for (int row = 0; row < 6; row++)
            {
                for (int column = 0; column < 7; column++)
                {
                    DateTime cellDate = firstCellDate.AddDays(row * 7 + column);
                    Vector2 cellPos = firstDayPos + new Vector2(column * (daySize + gap), row * (daySize + gap));
                    bool inCurrentMonth = cellDate.Month == viewDate.Month && cellDate.Year == viewDate.Year;
                    bool selected = cellDate.Date == selectedDate.Date;
                    bool isToday = cellDate.Date == today.Date;
                    bool hovered = !LastItemDisabled && IsItemHovered(cellPos, cellSize);

                    drawDatePickerDayBackground(drawList, cellPos, cellSize, selected, isToday, hovered);

                    Vector4 textColor = LastItemDisabled
                        ? Fugui.Themes.GetColor(FuColors.TextDisabled, 0.72f)
                        : Fugui.Themes.GetColor(inCurrentMonth ? FuColors.Text : FuColors.TextDisabled, inCurrentMonth ? 1f : 0.62f);
                    if (selected && !LastItemDisabled)
                    {
                        textColor = Fugui.Themes.GetColor(FuColors.SelectedText);
                    }

                    Fugui.Push(ImGuiCol.Text, textColor);
                    if (selected || isToday)
                    {
                        Fugui.PushFont(FontType.Bold);
                    }

                    ImGui.SetCursorScreenPos(cellPos);
                    if (_customButton(cellDate.Day + "##" + id + cellDate.ToString("yyyyMMdd"), cellSize, Vector2.zero, Vector2.zero, FuButtonStyle.Transparent, 0f, false, 0.5f))
                    {
                        selectedDate = cellDate.Date;
                        viewDate = new DateTime(cellDate.Year, cellDate.Month, 1);
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

        private void drawDatePickerFooter(string id, DateTime today, ref DateTime selectedDate, ref DateTime viewDate, Vector2 pos, float width, float height)
        {
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            string selectedText = selectedDate.ToString("d MMM yyyy");
            Vector2 textSize = ImGui.CalcTextSize(selectedText);
            Vector2 textPos = pos + new Vector2(0f, height * 0.5f - textSize.y * 0.5f);
            drawList.AddText(textPos, ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.TextDisabled, 0.86f)), selectedText);

            Vector2 todaySize = new Vector2(62f * Fugui.CurrentContext.Scale, height);
            if (drawDatePickerTextButton(id + "Today", "Today", pos + new Vector2(width - todaySize.x, 0f), todaySize, true, true))
            {
                selectedDate = today.Date;
                viewDate = new DateTime(today.Year, today.Month, 1);
                _updatedDateTime = selectedDate;
                _datetimeUpdated = true;
            }
        }

        private static void drawDatePickerDayBackground(ImDrawListPtr drawList, Vector2 pos, Vector2 size, bool selected, bool today, bool hovered)
        {
            float rounding = Mathf.Min(8f * Fugui.CurrentContext.Scale, size.y * 0.35f);
            if (selected)
            {
                drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.Selected)), rounding);
                return;
            }

            if (hovered)
            {
                drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.HeaderHovered, 0.72f)), rounding);
            }

            if (today)
            {
                Vector2 inset = Vector2.one * (1.5f * Fugui.CurrentContext.Scale);
                drawList.AddRect(pos + inset, pos + size - inset, ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.Highlight, 0.95f)), rounding, ImDrawFlags.RoundCornersDefault, 1.4f * Fugui.CurrentContext.Scale);
            }
        }

        private static void drawCenteredText(ImDrawListPtr drawList, string text, Vector2 pos, Vector2 size, uint color)
        {
            Vector2 textSize = ImGui.CalcTextSize(text);
            Vector2 textPos = pos + new Vector2((size.x - textSize.x) * 0.5f, (size.y - textSize.y) * 0.5f);
            drawList.AddText(textPos, color, text);
        }

        private void drawDatePickerMonthYearPopup(string id)
        {
            DateTime viewDate = _datePickerViewDates.TryGetValue(id, out DateTime storedViewDate) ? storedViewDate : DateTime.Today;
            float scale = Fugui.CurrentContext.Scale;
            float padding = 8f * scale;
            float gap = 6f * scale;
            float width = 210f * scale;
            float yearButtonSize = 28f * scale;
            Vector2 startPos = ImGui.GetCursorScreenPos() + new Vector2(padding, padding);

            if (drawDatePickerIconButton(id + "PopupPrevYear", startPos, new Vector2(yearButtonSize, yearButtonSize), ImGuiDir.Left))
            {
                viewDate = viewDate.AddYears(-1);
                _datePickerViewDates[id] = new DateTime(viewDate.Year, viewDate.Month, 1);
            }

            if (drawDatePickerTextButton(id + "PopupYear", viewDate.Year.ToString(), startPos + new Vector2(yearButtonSize + gap, 0f), new Vector2(width - yearButtonSize * 2f - gap * 2f, yearButtonSize), true, true))
            {
                viewDate = new DateTime(DateTime.Today.Year, viewDate.Month, 1);
                _datePickerViewDates[id] = viewDate;
            }

            if (drawDatePickerIconButton(id + "PopupNextYear", startPos + new Vector2(width - yearButtonSize, 0f), new Vector2(yearButtonSize, yearButtonSize), ImGuiDir.Right))
            {
                viewDate = viewDate.AddYears(1);
                _datePickerViewDates[id] = new DateTime(viewDate.Year, viewDate.Month, 1);
            }

            Vector2 monthStartPos = startPos + new Vector2(0f, yearButtonSize + gap * 1.4f);
            Vector2 monthButtonSize = new Vector2((width - gap * 2f) / 3f, 26f * scale);
            for (int i = 0; i < _monthStr.Length; i++)
            {
                int column = i % 3;
                int row = i / 3;
                Vector2 buttonPos = monthStartPos + new Vector2(column * (monthButtonSize.x + gap), row * (monthButtonSize.y + gap));
                bool selected = viewDate.Month == i + 1;
                if (drawDatePickerTextButton(id + "PopupMonth" + i, _monthStr[i].Substring(0, 3), buttonPos, monthButtonSize, selected, false))
                {
                    viewDate = new DateTime(viewDate.Year, i + 1, 1);
                    _datePickerViewDates[id] = viewDate;
                    Fugui.ForceCloseOpenPopup();
                }
            }

            ImGui.SetCursorScreenPos(startPos + new Vector2(0f, yearButtonSize + gap * 1.4f + monthButtonSize.y * 4f + gap * 3f));
            ImGui.Dummy(Vector2.one * scale);
        }

        private bool drawDatePickerIconButton(string id, Vector2 pos, Vector2 size, ImGuiDir direction)
        {
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            ImGui.SetCursorScreenPos(pos);
            bool clicked = !LastItemDisabled && ImGui.InvisibleButton(id, size, ImGuiButtonFlags.MouseButtonLeft);
            bool hovered = !LastItemDisabled && ImGui.IsItemHovered();
            bool active = !LastItemDisabled && ImGui.IsItemActive();
            float rounding = Mathf.Min(8f * Fugui.CurrentContext.Scale, size.y * 0.45f);

            if (active)
            {
                drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.HeaderActive, 0.85f)), rounding);
            }
            else if (hovered)
            {
                drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.HeaderHovered, 0.8f)), rounding);
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            drawChevron(drawList, pos, size, direction, ImGui.GetColorU32(Fugui.Themes.GetColor(LastItemDisabled ? FuColors.TextDisabled : FuColors.Text, LastItemDisabled ? 0.45f : 0.9f)));
            return clicked;
        }

        private bool drawDatePickerTitleButton(string id, string title, Vector2 pos, Vector2 size)
        {
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            ImGui.SetCursorScreenPos(pos);
            bool clicked = !LastItemDisabled && ImGui.InvisibleButton(id, size, ImGuiButtonFlags.MouseButtonLeft);
            bool hovered = !LastItemDisabled && ImGui.IsItemHovered();
            bool active = !LastItemDisabled && ImGui.IsItemActive();
            float rounding = Mathf.Min(10f * Fugui.CurrentContext.Scale, size.y * 0.45f);
            Vector4 bg = active
                ? Fugui.Themes.GetColor(FuColors.HeaderActive, 0.95f)
                : hovered
                    ? Fugui.Themes.GetColor(FuColors.HeaderHovered, 0.82f)
                    : Fugui.Themes.GetColor(FuColors.Header, 0.48f);

            drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(bg), rounding);
            drawList.AddRect(pos, pos + size, ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.Border, 0.55f)), rounding);

            if (hovered)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            Fugui.PushFont(FontType.Bold);
            Vector2 textSize = ImGui.CalcTextSize(title);
            Vector2 textPos = pos + new Vector2((size.x - textSize.x) * 0.5f - 5f * Fugui.CurrentContext.Scale, (size.y - textSize.y) * 0.5f);
            drawList.AddText(textPos, ImGui.GetColorU32(Fugui.Themes.GetColor(LastItemDisabled ? FuColors.TextDisabled : FuColors.Text, LastItemDisabled ? 0.55f : 0.96f)), title);
            Fugui.PopFont();

            Vector2 caretPos = pos + new Vector2(size.x - 18f * Fugui.CurrentContext.Scale, 0f);
            Fugui.DrawCarret_Down(drawList, caretPos, 7f * Fugui.CurrentContext.Scale, size.y, Fugui.Themes.GetColor(LastItemDisabled ? FuColors.TextDisabled : FuColors.Text, 0.72f));
            return clicked;
        }

        private bool drawDatePickerTextButton(string id, string label, Vector2 pos, Vector2 size, bool selected, bool bold)
        {
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            ImGui.SetCursorScreenPos(pos);
            bool clicked = !LastItemDisabled && ImGui.InvisibleButton(id, size, ImGuiButtonFlags.MouseButtonLeft);
            bool hovered = !LastItemDisabled && ImGui.IsItemHovered();
            bool active = !LastItemDisabled && ImGui.IsItemActive();
            float rounding = Mathf.Min(8f * Fugui.CurrentContext.Scale, size.y * 0.45f);

            if (selected)
            {
                drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.Selected, LastItemDisabled ? 0.35f : 1f)), rounding);
            }
            else if (active)
            {
                drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.HeaderActive, 0.84f)), rounding);
            }
            else if (hovered)
            {
                drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.HeaderHovered, 0.76f)), rounding);
            }

            if (hovered)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            if (bold || selected)
            {
                Fugui.PushFont(FontType.Bold);
            }

            uint textColor = ImGui.GetColorU32(selected && !LastItemDisabled ? Fugui.Themes.GetColor(FuColors.SelectedText) : Fugui.Themes.GetColor(LastItemDisabled ? FuColors.TextDisabled : FuColors.Text, LastItemDisabled ? 0.55f : 0.92f));
            drawCenteredText(drawList, label, pos, size, textColor);

            if (bold || selected)
            {
                Fugui.PopFont();
            }

            return clicked;
        }

        private static void drawChevron(ImDrawListPtr drawList, Vector2 pos, Vector2 size, ImGuiDir direction, uint color)
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
        #endregion
    }
}
