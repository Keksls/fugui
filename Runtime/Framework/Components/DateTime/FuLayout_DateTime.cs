using ImGuiNET;
using UnityEngine;
using System;
using Fu.Core;

namespace Fu.Framework
{
    public partial class FuLayout
    {
        string[] monthStr = new string[] {
                    "January", "February", "March", "April", "May", "June",
                    "July", "August", "September", "October", "November", "December" };
        private static DateTime _currentDateTimeValue;
        private static DateTime _updatedDateTime;
        private static bool _datetimeUpdated = false;

        public bool DateTimePickerPopup(string text, ref DateTime currentDate)
        {
            string ppID = "dtPkr" + text;
            _currentDateTimeValue = currentDate;

            string dtValue = currentDate.ToString("MMMM") + " " + currentDate.Day + " " + currentDate.ToString("yyyy");
            if (Button(dtValue))
            {
                OpenPopUp(ppID, drawPicker);
            }

            void drawPicker()
            {
                Spacing();
                Spacing();
                SameLine();
                BeginGroup();
                _datetimeUpdated = DateTimePicker(text, ref _currentDateTimeValue);
                EndGroup();
                SameLine();
                Spacing();
                Spacing();
            }

            DrawPopup(ppID);
            currentDate = _currentDateTimeValue;

            return _datetimeUpdated;
        }

        public bool DateTimePicker(string text, ref DateTime currentDate)
        {
            beginElement(ref text);

            _datetimeUpdated = false;
            _currentDateTimeValue = currentDate;
            _updatedDateTime = _currentDateTimeValue;

            #region values calculations
            // get current month data
            int month = currentDate.Month;
            int year = currentDate.Year;
            int day = currentDate.Day;
            int daysInMonth = DateTime.DaysInMonth(year, month);
            int dayOfWeek = (int)new DateTime(year, month, 1).DayOfWeek;
            int dayCount = 0;

            // get last month data
            int lastMonth = month - 1;
            int lastMonthYear = year;
            if (lastMonth < 1)
            {
                lastMonth = 12;
                lastMonthYear--;
            }
            int daysInLastMonth = DateTime.DaysInMonth(lastMonthYear, lastMonth);

            // calculate button size
            Vector2 btnSize = ImGui.CalcTextSize("99");
            btnSize += (Fugui.CurrentContext.Scale * FuThemeManager.CurrentTheme.FramePadding);
            btnSize.y = btnSize.x;
            #endregion

            // ========================= CALENDAR HEADER
            // Last month
            if (_customButton("<##dtp", Vector2.zero, FuThemeManager.CurrentTheme.FramePadding, Vector2.zero, FuButtonStyle.Default, FuThemeManager.CurrentTheme.ButtonsGradientStrenght))
            {
                // Previous month button clicked
                if (--month < 1)
                {
                    month = 12;
                    year--;
                }
                _updatedDateTime = new DateTime(year, month, 1);
                _datetimeUpdated = true;
            }
            ImGui.SameLine();

            // Month combobox
            _internalCombobox("##Month", monthStr[month - 1], () =>
            {
                int index = 1;
                foreach (string monthstr in monthStr)
                {
                    if (ImGui.Selectable(monthstr))
                    {
                        month = index;
                        _updatedDateTime = new DateTime(year, month, 1);
                        _datetimeUpdated = true;
                    }
                    index++;
                }
            }, new FuElementSize(82f, 0f), new Vector2(82f, 256f), FuButtonStyle.Default);

            ImGui.SameLine();
            // Year input
            Fugui.MoveY(FuThemeManager.CurrentTheme.FramePadding.y);
            string txtYear = year.ToString();
            if (_internalTextInput("##" + text + "yearInpt", year.ToString(), ref txtYear, 4, 0f, 72f, FuInputTextFlags.CharsDecimal))
            {
                int parsedYear = 0;
                if (int.TryParse(txtYear, out parsedYear))
                {
                    if (parsedYear > DateTime.MinValue.Year && parsedYear < DateTime.MaxValue.Year)
                    {
                        year = parsedYear;
                        _updatedDateTime = new DateTime(year, month, 1);
                        _datetimeUpdated = true;
                    }
                }
            }

            // Next month
            ImGui.SameLine();
            if (_customButton(">##dtp", Vector2.zero, FuThemeManager.CurrentTheme.FramePadding, Vector2.zero, FuButtonStyle.Default, FuThemeManager.CurrentTheme.ButtonsGradientStrenght))
            {
                // Next month button clicked
                if (++month > 12)
                {
                    month = 1;
                    year++;
                }
                _updatedDateTime = new DateTime(year, month, 1);
                _datetimeUpdated = true;
            }

            // ========================= CALENDAR BODY
            // Begin table to draw calendar
            Fugui.Push(ImGuiStyleVar.CellPadding, new Vector2(4f * Fugui.CurrentContext.Scale, 4f * Fugui.CurrentContext.Scale));
            ImGui.BeginTable("Calendar", 7, ImGuiTableFlags.NoBordersInBody);
            ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
            for (int i = 0; i < 7; i++)
            {
                ImGui.TableSetColumnIndex(i);
                ImGui.Text(((DayOfWeek)i).ToString().Substring(0, 3));
            }

            Fugui.Push(ImGuiStyleVar.FrameRounding, 16f);
            for (int i = 0; i < 6; i++)
            {
                ImGui.TableNextRow();
                for (int j = 0; j < 7; j++)
                {
                    ImGui.TableSetColumnIndex(j);
                    if (i == 0 && j < dayOfWeek || dayCount >= daysInMonth)
                    {
                        FuButtonStyle.Default.Push(false);
                        bool lastItemDisabled = LastItemDisabled;
                        LastItemDisabled = true;
                        // lines of last month
                        if (i == 0)
                        {
                            // get end of month offset
                            _customButton((daysInLastMonth - (dayOfWeek - j) + 1) + "##lm", btnSize, Vector2.zero, Vector2.zero, FuButtonStyle.Transparent, FuThemeManager.CurrentTheme.ButtonsGradientStrenght, false, 0.5f);
                        }
                        // lines of next month
                        else
                        {
                            // prevent to draw last line if beyond last month day
                            if (i == 5 && j == 0)
                            {
                                FuButtonStyle.Default.Pop();
                                LastItemDisabled = lastItemDisabled;
                                break;
                            }
                            _customButton(((dayCount % daysInMonth) + 1) + "##nm", btnSize, Vector2.zero, Vector2.zero, FuButtonStyle.Transparent, FuThemeManager.CurrentTheme.ButtonsGradientStrenght, false, 0.5f);
                            dayCount++;
                        }
                        FuButtonStyle.Default.Pop();
                        LastItemDisabled = lastItemDisabled;
                    }
                    else
                    {
                        bool selected = day == dayCount + 1;
                        if (selected)
                        {
                            Fugui.PushFont(FontType.Bold);
                        }
                        if (_customButton((dayCount + 1).ToString(), btnSize, Vector2.zero, Vector2.zero, selected ? FuButtonStyle.Selected : FuButtonStyle.Transparent, FuThemeManager.CurrentTheme.ButtonsGradientStrenght, false, 0.5f))
                        {
                            // Day button clicked
                            _updatedDateTime = new DateTime(year, month, dayCount + 1);
                            _datetimeUpdated = true;
                        }
                        if (selected)
                        {
                            Fugui.PopFont();
                        }
                        dayCount++;
                    }
                }
            }
            Fugui.PopStyle();
            ImGui.EndTable();
            Fugui.PopStyle();

            setBaseElementState(text, ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), false, _datetimeUpdated);

            endElement();
            if (_datetimeUpdated)
            {
                currentDate = _updatedDateTime;
                _currentDateTimeValue = _updatedDateTime;
            }

            return _datetimeUpdated;
        }
    }
}