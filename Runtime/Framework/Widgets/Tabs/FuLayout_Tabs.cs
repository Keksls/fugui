using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework
{
    /// <summary>
    /// Flags used to tune Fugui custom tab bars.
    /// </summary>
    [Flags]
    public enum FuTabsFlags
    {
        /// <summary>
        /// Default tab bar behaviour.
        /// </summary>
        Default = 0,
        /// <summary>
        /// Every tab uses the largest natural tab width.
        /// </summary>
        EqualWidth = 1 << 0,
        /// <summary>
        /// Expand tabs to fill the available width when there is free space.
        /// </summary>
        Stretch = 1 << 1,
        /// <summary>
        /// Reduce vertical padding for dense tool panels.
        /// </summary>
        Compact = 1 << 2,
        /// <summary>
        /// Hide scroll buttons when tabs overflow. Mouse wheel scrolling still works.
        /// </summary>
        NoScrollButtons = 1 << 3
    }

    /// <summary>
    /// Represents the Fu Layout type.
    /// </summary>
    public partial class FuLayout
    {
        #region State
        private const float TabFallbackMinWidth = 32f;
        private const float TabFallbackMaxWidth = 180f;
        private const float TabScrollSpeed = 360f;
        private const float TabWheelScrollStep = 58f;

        private static readonly Dictionary<string, int> _tabSelectedIndices = new Dictionary<string, int>();
        private static readonly Dictionary<string, float> _tabScrollOffsets = new Dictionary<string, float>();
        private static readonly Dictionary<string, FuElementAnimationData> _tabSelectionAnimations = new Dictionary<string, FuElementAnimationData>();
        private static readonly Dictionary<string, Rect[]> _tabHitRects = new Dictionary<string, Rect[]>();

        private struct FuTabLayoutData
        {
            public float X;
            public float Width;
            public string DisplayLabel;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Draw a modern Fugui tab bar and invoke the callback for the selected tab content.
        /// </summary>
        /// <param name="ID">Unique tab bar ID.</param>
        /// <param name="items">Tabs to draw.</param>
        /// <param name="callback">Callback of the UI to draw, the param is the selected item index.</param>
        /// <param name="forceSelectTabIndex">Force select a tab index (use -1 to keep the current selection).</param>
        public void Tabs(string ID, IEnumerable<string> items, Action<int> callback, int forceSelectTabIndex = -1)
        {
            Tabs(ID, items, callback, forceSelectTabIndex, FuTabsFlags.Default);
        }

        /// <summary>
        /// Draw a modern Fugui tab bar and invoke the callback for the selected tab content.
        /// </summary>
        /// <param name="ID">Unique tab bar ID.</param>
        /// <param name="items">Tabs to draw.</param>
        /// <param name="callback">Callback of the UI to draw, the param is the selected item index.</param>
        /// <param name="forceSelectTabIndex">Force select a tab index (use -1 to keep the current selection).</param>
        /// <param name="flags">Tab bar behaviour flags.</param>
        public void Tabs(string ID, IEnumerable<string> items, Action<int> callback, int forceSelectTabIndex, FuTabsFlags flags)
        {
            List<string> tabItems = BuildTabItems(items);
            int selectedIndex = -1;
            DrawTabs(ID, tabItems, ref selectedIndex, forceSelectTabIndex, flags);

            if (selectedIndex >= 0 && selectedIndex < tabItems.Count)
            {
                callback?.Invoke(selectedIndex);
            }
        }

        /// <summary>
        /// Draw a modern Fugui tab bar and update the selected index.
        /// </summary>
        /// <param name="ID">Unique tab bar ID.</param>
        /// <param name="items">Tabs to draw.</param>
        /// <param name="selectedIndex">Selected tab index.</param>
        /// <param name="flags">Tab bar behaviour flags.</param>
        /// <returns>True if the selection changed this frame.</returns>
        public bool Tabs(string ID, IList<string> items, ref int selectedIndex, FuTabsFlags flags = FuTabsFlags.Default)
        {
            return DrawTabs(ID, items, ref selectedIndex, -1, flags);
        }

        /// <summary>
        /// Draw a modern Fugui tab bar and update the selected index.
        /// </summary>
        /// <param name="ID">Unique tab bar ID.</param>
        /// <param name="items">Tabs to draw.</param>
        /// <param name="selectedIndex">Selected tab index.</param>
        /// <param name="forceSelectTabIndex">Forced selected index, or -1 to use current selection.</param>
        /// <param name="flags">Tab bar behaviour flags.</param>
        /// <returns>True if the selection changed this frame.</returns>
        private bool DrawTabs(string ID, IList<string> items, ref int selectedIndex, int forceSelectTabIndex, FuTabsFlags flags)
        {
            string elementID = ID;
            beginElement(ref elementID, null);
            if (!_drawElement)
            {
                return false;
            }

            int count = items != null ? items.Count : 0;
            Vector2 barPos = ImGui.GetCursorScreenPos();
            float availableWidth = Mathf.Max(1f, ImGui.GetContentRegionAvail().x);
            float barHeight = GetTabBarHeight(flags);
            Vector2 barSize = new Vector2(availableWidth, barHeight);

            ImGui.Dummy(barSize);

            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            DrawTabBarTrack(drawList, barPos, barSize, flags);

            bool selectionChanged = false;
            if (count <= 0)
            {
                setBaseElementState(elementID, barPos, barSize, false, false);
                ImGui.SetCursorScreenPos(new Vector2(ImGui.GetCursorScreenPos().x, barPos.y + barHeight));
                endElement();
                return false;
            }

            int previousSelectedIndex = ResolveSelectedTabIndex(elementID, selectedIndex, -1, count);
            selectedIndex = ResolveSelectedTabIndex(elementID, selectedIndex, forceSelectTabIndex, count);
            selectionChanged = previousSelectedIndex != selectedIndex;

            FuTabLayoutData[] layout = BuildTabsLayout(items, flags, availableWidth);
            Rect[] hitRects = new Rect[count];
            float totalTabsWidth = GetTotalTabsWidth(layout);
            float scale = Fugui.CurrentContext.Scale;
            float borderSize = Mathf.Max(0f, Fugui.Themes.TabBorderSize);
            float inset = Mathf.Max(1f * scale, borderSize);
            float scrollButtonWidth = flags.HasFlag(FuTabsFlags.NoScrollButtons) ? 0f : Mathf.Max(18f * scale, barHeight - inset * 2f);
            bool overflow = totalTabsWidth > availableWidth;
            float leftControlsWidth = overflow && scrollButtonWidth > 0f ? scrollButtonWidth + Fugui.Themes.TabSpacing : 0f;
            float rightControlsWidth = leftControlsWidth;
            float tabsViewportWidth = Mathf.Max(1f, availableWidth - leftControlsWidth - rightControlsWidth);
            Vector2 tabsClipMin = barPos + new Vector2(leftControlsWidth + inset, inset);
            Vector2 tabsClipMax = barPos + new Vector2(leftControlsWidth + tabsViewportWidth - inset, barHeight);

            float scrollOffset = GetTabScrollOffset(elementID);
            scrollOffset = ClampTabScroll(scrollOffset, totalTabsWidth, tabsViewportWidth);
            scrollOffset = EnsureTabVisible(scrollOffset, tabsViewportWidth, totalTabsWidth, layout[selectedIndex].X, layout[selectedIndex].Width, 16f * scale);

            bool barHovered = IsItemHovered(barPos, barSize);
            if (overflow && barHovered && !LastItemDisabled)
            {
                ImGuiIOPtr io = ImGui.GetIO();
                float wheel = Mathf.Abs(io.MouseWheelH) > 0f ? io.MouseWheelH : io.MouseWheel;
                if (Mathf.Abs(wheel) > 0.0001f)
                {
                    scrollOffset = ClampTabScroll(scrollOffset - wheel * TabWheelScrollStep * scale, totalTabsWidth, tabsViewportWidth);
                }
            }

            if (overflow && scrollButtonWidth > 0f)
            {
                Rect leftRect = new Rect(barPos + new Vector2(inset, inset), new Vector2(scrollButtonWidth, barHeight - inset * 2f));
                Rect rightRect = new Rect(barPos + new Vector2(availableWidth - scrollButtonWidth - inset, inset), new Vector2(scrollButtonWidth, barHeight - inset * 2f));

                scrollOffset += DrawTabScrollButton(elementID + "##left-scroll", drawList, leftRect, true, scrollOffset > 0.5f, flags);
                scrollOffset += DrawTabScrollButton(elementID + "##right-scroll", drawList, rightRect, false, scrollOffset < totalTabsWidth - tabsViewportWidth - 0.5f, flags);
                scrollOffset = ClampTabScroll(scrollOffset, totalTabsWidth, tabsViewportWidth);
            }

            drawList.PushClipRect(tabsClipMin, tabsClipMax, true);
            for (int i = 0; i < count; i++)
            {
                Vector2 tabMin = new Vector2(tabsClipMin.x + layout[i].X - scrollOffset, tabsClipMin.y);
                Vector2 tabMax = tabMin + new Vector2(layout[i].Width, tabsClipMax.y - tabsClipMin.y);
                if (tabMax.x <= tabsClipMin.x || tabMin.x >= tabsClipMax.x)
                {
                    UpdateTabSelectionAnimation(elementID, i, selectedIndex == i);
                    continue;
                }

                Rect hitRect = ClipTabHitRect(tabMin, tabMax, tabsClipMin, tabsClipMax);
                hitRects[i] = hitRect;
                string tabID = elementID + "##tab-" + i + "-" + (items[i] ?? string.Empty);
                setBaseElementState(tabID, hitRect.position, hitRect.size, !LastItemDisabled, false, true);

                bool hovered = _lastItemHovered && !LastItemDisabled;
                bool active = _lastItemActive && !LastItemDisabled;
                bool selected = selectedIndex == i;
                if (_lastItemUpdate && !selected && !LastItemDisabled)
                {
                    selectedIndex = i;
                    selected = true;
                    selectionChanged = true;
                    scrollOffset = EnsureTabVisible(scrollOffset, tabsViewportWidth, totalTabsWidth, layout[i].X, layout[i].Width, 16f * scale);
                }

                if (hovered)
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }

                float selectedAmount = UpdateTabSelectionAnimation(elementID, i, selected);
                DrawTabItem(drawList, tabMin, tabMax, layout[i].DisplayLabel, selected, hovered, active, selectedAmount, flags);
            }
            drawList.PopClipRect();

            _tabSelectedIndices[elementID] = selectedIndex;
            _tabScrollOffsets[elementID] = ClampTabScroll(scrollOffset, totalTabsWidth, tabsViewportWidth);
            _tabHitRects[elementID] = hitRects;

            ImGui.SetCursorScreenPos(new Vector2(ImGui.GetCursorScreenPos().x, barPos.y + barHeight));
            endElement();
            return selectionChanged;
        }

        /// <summary>
        /// Try to resolve which tab was under a screen-space point during the last draw of a tab bar.
        /// </summary>
        /// <param name="ID">Unique tab bar ID.</param>
        /// <param name="screenPosition">Screen-space point to test.</param>
        /// <param name="tabIndex">Index of the hovered tab.</param>
        /// <returns>True if a visible tab contains the point.</returns>
        internal static bool TryGetLastTabHitIndex(string ID, Vector2 screenPosition, out int tabIndex)
        {
            tabIndex = -1;
            if (string.IsNullOrEmpty(ID) || !_tabHitRects.TryGetValue(ID, out Rect[] hitRects) || hitRects == null)
            {
                return false;
            }

            for (int i = 0; i < hitRects.Length; i++)
            {
                Rect hitRect = hitRects[i];
                if (hitRect.width <= 0f || hitRect.height <= 0f)
                {
                    continue;
                }

                if (hitRect.Contains(screenPosition))
                {
                    tabIndex = i;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Build a stable list from an enumerable, because tab layout needs multiple passes.
        /// </summary>
        /// <param name="items">Tab labels.</param>
        /// <returns>Tab labels as a list.</returns>
        private static List<string> BuildTabItems(IEnumerable<string> items)
        {
            List<string> result = new List<string>();
            if (items == null)
            {
                return result;
            }

            foreach (string item in items)
            {
                result.Add(item ?? string.Empty);
            }
            return result;
        }

        /// <summary>
        /// Resolve the selected index from external input, forced input, and Fugui persistent state.
        /// </summary>
        /// <param name="elementID">Unique element ID.</param>
        /// <param name="requestedIndex">Index requested by the caller.</param>
        /// <param name="forcedIndex">Index forced for this frame.</param>
        /// <param name="count">Tab count.</param>
        /// <returns>Clamped selected index.</returns>
        private static int ResolveSelectedTabIndex(string elementID, int requestedIndex, int forcedIndex, int count)
        {
            int selected = requestedIndex;
            if (forcedIndex >= 0)
            {
                selected = forcedIndex;
            }
            else if (selected < 0 && !_tabSelectedIndices.TryGetValue(elementID, out selected))
            {
                selected = 0;
            }

            return Mathf.Clamp(selected, 0, count - 1);
        }

        /// <summary>
        /// Compute the tab bar height.
        /// </summary>
        /// <param name="flags">Tab flags.</param>
        /// <returns>Height in screen pixels.</returns>
        private static float GetTabBarHeight(FuTabsFlags flags)
        {
            float scale = Fugui.CurrentContext.Scale;
            float textHeight = ImGui.CalcTextSize("Ap").y;
            float minHeight = flags.HasFlag(FuTabsFlags.Compact) ? 22f * scale : 25f * scale;
            float verticalPadding = Fugui.Themes.TabPadding.y * (flags.HasFlag(FuTabsFlags.Compact) ? 0.65f : 1f);
            float witnessReserve = (flags.HasFlag(FuTabsFlags.Compact) ? 2f : 4f) * scale;
            return Mathf.Max(minHeight, textHeight + verticalPadding * 2f + witnessReserve);
        }

        /// <summary>
        /// Build tab positions and widths.
        /// </summary>
        /// <param name="items">Tab labels.</param>
        /// <param name="flags">Tab flags.</param>
        /// <param name="availableWidth">Available bar width.</param>
        /// <returns>Tab layout data.</returns>
        private static FuTabLayoutData[] BuildTabsLayout(IList<string> items, FuTabsFlags flags, float availableWidth)
        {
            int count = items.Count;
            float scale = Fugui.CurrentContext.Scale;
            float paddingX = Fugui.Themes.TabPadding.x * (flags.HasFlag(FuTabsFlags.Compact) ? 0.75f : 1f);
            float gap = Mathf.Max(0f, Fugui.Themes.TabSpacing);
            float minWidth = Mathf.Max(TabFallbackMinWidth * scale, Fugui.Themes.TabMinWidth);
            float maxWidth = Mathf.Max(minWidth, Fugui.Themes.TabMaxWidth > 0f ? Fugui.Themes.TabMaxWidth : TabFallbackMaxWidth * scale);
            float largestWidth = 0f;
            float totalWidth = 0f;
            FuTabLayoutData[] layout = new FuTabLayoutData[count];

            for (int i = 0; i < count; i++)
            {
                string displayLabel = GetTabDisplayLabel(items[i]);
                float textWidth = ImGui.CalcTextSize(displayLabel).x;
                float width = Mathf.Clamp(textWidth + paddingX * 2f, minWidth, Mathf.Max(minWidth, maxWidth));
                layout[i].DisplayLabel = displayLabel;
                layout[i].Width = width;
                largestWidth = Mathf.Max(largestWidth, width);
            }

            if (flags.HasFlag(FuTabsFlags.EqualWidth))
            {
                for (int i = 0; i < count; i++)
                {
                    layout[i].Width = largestWidth;
                }
            }

            for (int i = 0; i < count; i++)
            {
                totalWidth += layout[i].Width;
                if (i < count - 1)
                {
                    totalWidth += gap;
                }
            }

            if (flags.HasFlag(FuTabsFlags.Stretch) && totalWidth < availableWidth && count > 0)
            {
                float extraWidth = (availableWidth - totalWidth) / count;
                for (int i = 0; i < count; i++)
                {
                    layout[i].Width += extraWidth;
                }
            }
            else if (totalWidth > availableWidth && count > 0)
            {
                float availableForTabs = Mathf.Max(0f, availableWidth - gap * (count - 1));
                float constrainedWidth = availableForTabs / count;
                float uniformWidth = constrainedWidth >= minWidth ? Mathf.Min(constrainedWidth, maxWidth) : minWidth;
                for (int i = 0; i < count; i++)
                {
                    layout[i].Width = uniformWidth;
                }
            }

            float cursor = 0f;
            for (int i = 0; i < count; i++)
            {
                layout[i].X = cursor;
                cursor += layout[i].Width + gap;
            }

            return layout;
        }

        /// <summary>
        /// Get the full width occupied by tab items.
        /// </summary>
        /// <param name="layout">Tab layout data.</param>
        /// <returns>Total width.</returns>
        private static float GetTotalTabsWidth(FuTabLayoutData[] layout)
        {
            if (layout == null || layout.Length == 0)
            {
                return 0f;
            }

            FuTabLayoutData lastTab = layout[layout.Length - 1];
            return lastTab.X + lastTab.Width;
        }

        /// <summary>
        /// Read the current horizontal scroll offset of a tab bar.
        /// </summary>
        /// <param name="elementID">Unique element ID.</param>
        /// <returns>Scroll offset.</returns>
        private static float GetTabScrollOffset(string elementID)
        {
            float scrollOffset;
            if (!_tabScrollOffsets.TryGetValue(elementID, out scrollOffset))
            {
                scrollOffset = 0f;
            }

            return scrollOffset;
        }

        /// <summary>
        /// Clamp the horizontal scroll offset.
        /// </summary>
        /// <param name="scrollOffset">Current scroll offset.</param>
        /// <param name="totalWidth">Full tab content width.</param>
        /// <param name="viewportWidth">Visible tab viewport width.</param>
        /// <returns>Clamped scroll offset.</returns>
        private static float ClampTabScroll(float scrollOffset, float totalWidth, float viewportWidth)
        {
            return Mathf.Clamp(scrollOffset, 0f, Mathf.Max(0f, totalWidth - viewportWidth));
        }

        /// <summary>
        /// Keep a selected tab inside the visible viewport.
        /// </summary>
        /// <param name="scrollOffset">Current scroll offset.</param>
        /// <param name="viewportWidth">Visible viewport width.</param>
        /// <param name="totalWidth">Full tab content width.</param>
        /// <param name="tabX">Tab local X.</param>
        /// <param name="tabWidth">Tab width.</param>
        /// <param name="edgePadding">Padding to keep around selected tabs.</param>
        /// <returns>Adjusted scroll offset.</returns>
        private static float EnsureTabVisible(float scrollOffset, float viewportWidth, float totalWidth, float tabX, float tabWidth, float edgePadding)
        {
            if (tabX - edgePadding < scrollOffset)
            {
                scrollOffset = tabX - edgePadding;
            }
            else if (tabX + tabWidth + edgePadding > scrollOffset + viewportWidth)
            {
                scrollOffset = tabX + tabWidth + edgePadding - viewportWidth;
            }

            return ClampTabScroll(scrollOffset, totalWidth, viewportWidth);
        }

        /// <summary>
        /// Strip ImGui ID suffixes from labels before drawing them.
        /// </summary>
        /// <param name="label">Raw label.</param>
        /// <returns>Display label.</returns>
        private static string GetTabDisplayLabel(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                return string.Empty;
            }

            int idSeparator = label.IndexOf("##", StringComparison.Ordinal);
            return idSeparator >= 0 ? label.Substring(0, idSeparator) : label;
        }

        /// <summary>
        /// Clip a tab hit rectangle to the visible tab viewport.
        /// </summary>
        /// <param name="tabMin">Tab min.</param>
        /// <param name="tabMax">Tab max.</param>
        /// <param name="clipMin">Clip min.</param>
        /// <param name="clipMax">Clip max.</param>
        /// <returns>Clipped hit rectangle.</returns>
        private static Rect ClipTabHitRect(Vector2 tabMin, Vector2 tabMax, Vector2 clipMin, Vector2 clipMax)
        {
            Vector2 min = new Vector2(Mathf.Max(tabMin.x, clipMin.x), Mathf.Max(tabMin.y, clipMin.y));
            Vector2 max = new Vector2(Mathf.Min(tabMax.x, clipMax.x), Mathf.Min(tabMax.y, clipMax.y));
            return new Rect(min, new Vector2(Mathf.Max(0f, max.x - min.x), Mathf.Max(0f, max.y - min.y)));
        }

        /// <summary>
        /// Update the selected animation of one tab.
        /// </summary>
        /// <param name="elementID">Unique element ID.</param>
        /// <param name="index">Tab index.</param>
        /// <param name="selected">Whether the tab is selected.</param>
        /// <returns>Animated selection amount.</returns>
        private float UpdateTabSelectionAnimation(string elementID, int index, bool selected)
        {
            string animationID = elementID + "##tab-animation-" + index;
            FuElementAnimationData animationData;
            if (!_tabSelectionAnimations.TryGetValue(animationID, out animationData))
            {
                animationData = new FuElementAnimationData(false);
                _tabSelectionAnimations.Add(animationID, animationData);
            }

            animationData.Update(selected, _animationEnabled);
            return animationData.CurrentValue;
        }

        /// <summary>
        /// Draw the tab bar background track.
        /// </summary>
        /// <param name="drawList">Draw list.</param>
        /// <param name="pos">Position.</param>
        /// <param name="size">Size.</param>
        /// <param name="flags">Tab flags.</param>
        private static void DrawTabBarTrack(ImDrawListPtr drawList, Vector2 pos, Vector2 size, FuTabsFlags flags)
        {
            float rounding = Mathf.Min(Fugui.Themes.TabRounding, size.y * 0.5f);
            float borderSize = Fugui.Themes.TabBorderSize;
            Vector4 bg = Fugui.Themes.GetColor(FuColors.TitleBg);
            Vector4 border = Fugui.Themes.GetColor(FuColors.Border, 0.70f);
            Vector2 max = pos + size;

            drawList.AddRectFilled(pos, max, ImGui.GetColorU32(bg), rounding, ImDrawFlags.RoundCornersTop);
            if (borderSize > 0f)
            {
                uint borderColor = ImGui.GetColorU32(border);
                float topInset = Mathf.Min(rounding, size.x * 0.25f);
                drawList.AddLine(new Vector2(pos.x + topInset, pos.y), new Vector2(max.x - topInset, pos.y), borderColor, borderSize);
                drawList.AddLine(new Vector2(pos.x, pos.y + rounding), new Vector2(pos.x, max.y), borderColor, borderSize);
                drawList.AddLine(new Vector2(max.x, pos.y + rounding), new Vector2(max.x, max.y), borderColor, borderSize);
            }
        }

        /// <summary>
        /// Draw a single tab.
        /// </summary>
        /// <param name="drawList">Draw list.</param>
        /// <param name="min">Min position.</param>
        /// <param name="max">Max position.</param>
        /// <param name="label">Display label.</param>
        /// <param name="selected">Whether selected.</param>
        /// <param name="hovered">Whether hovered.</param>
        /// <param name="active">Whether active.</param>
        /// <param name="selectedAmount">Animated selected amount.</param>
        /// <param name="flags">Tab flags.</param>
        private void DrawTabItem(ImDrawListPtr drawList, Vector2 min, Vector2 max, string label, bool selected, bool hovered, bool active, float selectedAmount, FuTabsFlags flags)
        {
            float scale = Fugui.CurrentContext.Scale;
            Vector2 size = max - min;
            float rounding = Mathf.Min(Fugui.Themes.TabRounding, size.y * 0.5f);
            float borderSize = Fugui.Themes.TabBorderSize;
            float disabledAlpha = LastItemDisabled ? 0.45f : 1f;
            float visualSelectedAmount = selected ? Mathf.Max(0.68f, selectedAmount) : selectedAmount;

            Vector4 idle = Fugui.Themes.GetColor(FuColors.Tab);
            Vector4 hover = active ? Fugui.Themes.GetColor(FuColors.TabHovered, 1f) : Fugui.Themes.GetColor(FuColors.TabHovered, 0.82f);
            Vector4 selectedBg = Fugui.Themes.GetColor(FuColors.TabSelected);
            Vector4 fill = Vector4.Lerp(hovered || active ? hover : idle, selectedBg, visualSelectedAmount);
            fill.w = Mathf.Clamp01(fill.w * disabledAlpha);
            ImDrawFlags roundFlags = visualSelectedAmount > 0.001f ? ImDrawFlags.RoundCornersTop : ImDrawFlags.RoundCornersAll;
            drawList.AddRectFilled(min, max, ImGui.GetColorU32(fill), rounding, roundFlags);

            if (visualSelectedAmount > 0.001f && size.y > 8f * scale)
            {
                float thickness = Mathf.Max(2f, 3f * scale);
                float horizontalPadding = Mathf.Max(4f * scale, rounding + 2f * scale);
                float availableLength = size.x - horizontalPadding * 2f;
                float minDrawableLength = Mathf.Max(6f * scale, thickness * 2f);
                if (availableLength >= minDrawableLength)
                {
                    Vector4 witness = active
                        ? Fugui.Themes.GetColor(FuColors.HighlightActive)
                        : hovered ? Fugui.Themes.GetColor(FuColors.HighlightHovered) : Fugui.Themes.GetColor(FuColors.Highlight);
                    witness.w = Mathf.Clamp01((hovered || active ? 1f : 0.82f) * disabledAlpha * visualSelectedAmount);

                    float idealLength = Mathf.Clamp(size.x * 0.34f, 18f * scale, 54f * scale);
                    float length = Mathf.Min(idealLength, availableLength);
                    Vector2 center = new Vector2((min.x + max.x) * 0.5f, min.y + Mathf.Max(3f * scale, thickness * 0.5f + scale));
                    Vector2 witnessMin = new Vector2(center.x - length * 0.5f, center.y - thickness * 0.5f);
                    Vector2 witnessMax = new Vector2(center.x + length * 0.5f, center.y + thickness * 0.5f);
                    drawList.AddRectFilled(witnessMin, witnessMax, ImGui.GetColorU32(witness), thickness * 0.5f, ImDrawFlags.RoundCornersAll);
                }
            }

            if (borderSize > 0f && (hovered || active) && visualSelectedAmount <= 0.001f)
            {
                Vector4 border = Fugui.Themes.GetColor(FuColors.Border, 0.50f * disabledAlpha);
                drawList.AddRect(min, max, ImGui.GetColorU32(border), rounding, ImDrawFlags.RoundCornersAll, borderSize);
            }

            Vector4 textColor;
            if (LastItemDisabled)
            {
                textColor = Fugui.Themes.GetColor(FuColors.TextDisabled, 0.82f);
            }
            else if (selected)
            {
                textColor = Fugui.Themes.GetColor(FuColors.SelectedText);
            }
            else
            {
                textColor = Fugui.Themes.GetColor(FuColors.Text, hovered ? 0.95f : 0.74f);
            }

            Fugui.Push(ImGuiCol.Text, textColor);
            Vector2 padding = new Vector2(Fugui.Themes.TabPadding.x * (flags.HasFlag(FuTabsFlags.Compact) ? 0.75f : 1f), 0f);
            EnboxedText(label, min, size, padding, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), FuTextWrapping.Clip);
            Fugui.PopColor();
        }

        /// <summary>
        /// Draw and process a scroll arrow button.
        /// </summary>
        /// <param name="id">Unique ID.</param>
        /// <param name="drawList">Draw list.</param>
        /// <param name="rect">Button rect.</param>
        /// <param name="left">Whether this is the left button.</param>
        /// <param name="enabled">Whether the button can scroll.</param>
        /// <param name="flags">Tab flags.</param>
        /// <returns>Scroll delta.</returns>
        private float DrawTabScrollButton(string id, ImDrawListPtr drawList, Rect rect, bool left, bool enabled, FuTabsFlags flags)
        {
            setBaseElementState(id, rect.position, rect.size, enabled && !LastItemDisabled, false, true);

            bool hovered = enabled && _lastItemHovered && !LastItemDisabled;
            bool active = enabled && _lastItemActive && !LastItemDisabled;
            float scale = Fugui.CurrentContext.Scale;
            float rounding = Mathf.Min(rect.height * 0.5f, Fugui.Themes.TabRounding);
            float borderSize = Fugui.Themes.TabBorderSize;
            Vector4 bg = active
                ? Fugui.Themes.GetColor(FuColors.ButtonActive, 0.85f)
                : hovered ? Fugui.Themes.GetColor(FuColors.ButtonHovered, 0.72f) : Fugui.Themes.GetColor(FuColors.Button, enabled ? 0.35f : 0.14f);
            Vector4 border = Fugui.Themes.GetColor(FuColors.Border, enabled ? 0.55f : 0.22f);

            drawList.AddRectFilled(rect.min, rect.max, ImGui.GetColorU32(bg), rounding, ImDrawFlags.RoundCornersAll);
            if (borderSize > 0f)
            {
                drawList.AddRect(rect.min, rect.max, ImGui.GetColorU32(border), rounding, ImDrawFlags.RoundCornersAll, borderSize);
            }

            Vector4 arrowColor = Fugui.Themes.GetColor(enabled ? FuColors.Text : FuColors.TextDisabled, enabled ? 0.82f : 0.42f);
            float caretSize = Mathf.Max(6f * scale, rect.height * 0.24f);
            Vector2 caretPos = new Vector2(rect.center.x - caretSize * 0.5f, rect.yMin);
            if (left)
            {
                Fugui.DrawCarret_Left(drawList, caretPos, caretSize, rect.height, arrowColor);
            }
            else
            {
                Fugui.DrawCarret_Right(drawList, caretPos, caretSize, rect.height, arrowColor);
            }

            if (hovered)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            if (!enabled)
            {
                return 0f;
            }

            float delta = 0f;
            if (_lastItemUpdate)
            {
                delta += (left ? -1f : 1f) * TabWheelScrollStep * scale;
            }
            if (active)
            {
                delta += (left ? -1f : 1f) * TabScrollSpeed * scale * ImGui.GetIO().DeltaTime;
            }

            return delta;
        }

        #endregion
    }
}
