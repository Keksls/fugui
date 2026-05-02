using ImGuiNET;
using UnityEngine;

namespace Fu.Framework
{
    /// <summary>
    /// Search input widgets.
    /// </summary>
    public partial class FuLayout
    {
        #region State
        private const uint SEARCH_BOX_BUFFER_SIZE = 2048;
        #endregion

        #region Methods
        /// <summary>
        /// Draw a search field with a clear button.
        /// </summary>
        /// <param name="id">Unique ID of the search field.</param>
        /// <param name="search">Search string to edit.</param>
        /// <param name="hint">Placeholder displayed when the search string is empty.</param>
        /// <param name="width">Widget width. 0 uses available width, positive values are scaled pixels, negative values subtract from available width.</param>
        /// <returns>true if the search string changed this frame.</returns>
        public bool SearchBox(string id, ref string search, string hint = "Search...", float width = 0f)
        {
            return SearchBox(id, ref search, hint, width, FuFrameStyle.Default);
        }

        /// <summary>
        /// Draw a search field bound to a reusable filter object.
        /// </summary>
        /// <param name="id">Unique ID of the search field.</param>
        /// <param name="filter">Filter object to edit.</param>
        /// <param name="hint">Placeholder displayed when the search string is empty.</param>
        /// <param name="width">Widget width. 0 uses available width, positive values are scaled pixels, negative values subtract from available width.</param>
        /// <returns>true if the filter query changed this frame.</returns>
        public bool SearchBox(string id, FuSearchFilter filter, string hint = "Search...", float width = 0f)
        {
            return SearchBox(id, filter, hint, width, FuFrameStyle.Default);
        }

        /// <summary>
        /// Draw a search field bound to a reusable filter object.
        /// </summary>
        /// <param name="id">Unique ID of the search field.</param>
        /// <param name="filter">Filter object to edit.</param>
        /// <param name="hint">Placeholder displayed when the search string is empty.</param>
        /// <param name="width">Widget width. 0 uses available width, positive values are scaled pixels, negative values subtract from available width.</param>
        /// <param name="style">Frame style to use.</param>
        /// <returns>true if the filter query changed this frame.</returns>
        public bool SearchBox(string id, FuSearchFilter filter, string hint, float width, FuFrameStyle style)
        {
            string search = filter?.Query ?? string.Empty;
            bool edited = SearchBox(id, ref search, hint, width, style);
            if (filter != null && edited)
            {
                filter.Query = search;
            }
            return edited;
        }

        /// <summary>
        /// Draw a search field with a clear button.
        /// </summary>
        /// <param name="id">Unique ID of the search field.</param>
        /// <param name="search">Search string to edit.</param>
        /// <param name="hint">Placeholder displayed when the search string is empty.</param>
        /// <param name="width">Widget width. 0 uses available width, positive values are scaled pixels, negative values subtract from available width.</param>
        /// <param name="style">Frame style to use.</param>
        /// <returns>true if the search string changed this frame.</returns>
        public virtual bool SearchBox(string id, ref string search, string hint, float width, FuFrameStyle style)
        {
            if (search == null)
            {
                search = string.Empty;
            }

            string elementID = id;
            beginElement(ref elementID, style);
            if (!_drawElement)
            {
                return false;
            }

            float totalWidth = ResolveSearchBoxWidth(width);
            float height = ImGui.GetFrameHeight();
            float scale = Fugui.CurrentContext.Scale;
            float rounding = Mathf.Min(Mathf.Max(Fugui.Themes.FrameRounding, 6f * scale), height * 0.5f);
            float iconWidth = height;
            float clearWidth = height;
            float inputWidth = Mathf.Max(24f * scale, totalWidth - iconWidth - clearWidth - 6f * scale);
            bool disabled = LastItemDisabled;
            bool edited = false;
            Vector2 framePos = ImGui.GetCursorScreenPos();
            Vector2 frameSize = new Vector2(totalWidth, height);
            Rect frameRect = new Rect(framePos, frameSize);
            bool hovered = IsItemHovered(framePos, frameSize);
            bool canClear = !disabled && !string.IsNullOrEmpty(search);
            Rect clearRect = new Rect(framePos + new Vector2(totalWidth - clearWidth, 0f), new Vector2(clearWidth, height));
            bool clearHovered = canClear && IsItemHovered(clearRect.position, clearRect.size);

            ImGui.Dummy(frameSize);
            Vector2 afterFramePos = ImGui.GetCursorScreenPos();

            Vector4 frameColor = disabled
                ? style.DisabledFrame
                : hovered
                    ? style.HoveredFrame
                    : style.Frame;
            Vector4 borderColor = disabled ? style.DisabledBorder : style.Border;
            frameColor.w = Mathf.Max(frameColor.w, disabled ? 0.35f : 0.92f);
            borderColor.w = Mathf.Max(borderColor.w, hovered ? 0.75f : 0.48f);

            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            drawList.AddRectFilled(frameRect.min, frameRect.max, ImGui.GetColorU32(frameColor), rounding, ImDrawFlags.RoundCornersAll);
            drawList.AddRect(frameRect.min, frameRect.max, ImGui.GetColorU32(borderColor), rounding, ImDrawFlags.RoundCornersAll, Mathf.Max(1f, Fugui.Themes.FrameBorderSize));

            Vector4 iconColor = disabled
                ? style.TextStyle.DisabledText
                : string.IsNullOrEmpty(search)
                    ? Fugui.Themes.GetColor(FuColors.TextDisabled)
                    : style.TextStyle.Text;
            iconColor.w *= string.IsNullOrEmpty(search) ? 0.78f : 0.9f;
            DrawSearchGlyph(drawList, framePos + new Vector2(iconWidth * 0.5f, height * 0.5f), Mathf.Max(4f, height * 0.18f), iconColor);

            if (hovered && !clearHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left) && !disabled)
            {
                ImGui.SetKeyboardFocusHere();
            }

            Vector2 inputPos = framePos + new Vector2(iconWidth, 0f);
            ImGui.SetCursorScreenPos(inputPos);
            ImGui.SetNextItemWidth(inputWidth);
            ImGuiInputTextFlags inputFlags = disabled ? ImGuiInputTextFlags.ReadOnly : ImGuiInputTextFlags.None;
            Vector4 placeholder = Fugui.Themes.GetColor(FuColors.TextDisabled);
            placeholder.w *= disabled ? 0.45f : 0.72f;
            Fugui.Push(ImGuiCol.FrameBg, Vector4.zero);
            Fugui.Push(ImGuiCol.FrameBgHovered, Vector4.zero);
            Fugui.Push(ImGuiCol.FrameBgActive, Vector4.zero);
            Fugui.Push(ImGuiCol.TextDisabled, placeholder);
            Fugui.Push(ImGuiStyleVar.FrameBorderSize, 0f);
            Fugui.Push(ImGuiStyleVar.FramePadding, new Vector2(0f, ImGui.GetStyle().FramePadding.y));
            edited = ImGui.InputTextWithHint("##" + elementID + "_input", hint, ref search, SEARCH_BOX_BUFFER_SIZE, inputFlags);
            bool focused = ImGui.IsItemActive() || ImGui.IsItemFocused();
            Fugui.PopStyle(2);
            Fugui.PopColor(4);

            if (canClear)
            {
                ImGui.SetCursorScreenPos(clearRect.position);
                ImGui.InvisibleButton("##" + elementID + "_clear", clearRect.size);
                clearHovered = IsItemHovered(clearRect.position, clearRect.size);
                DrawClearGlyph(drawList, clearRect.position + clearRect.size * 0.5f, height * 0.28f, iconColor, clearHovered);
                if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                {
                    search = string.Empty;
                    edited = true;
                }
            }

            DrawWidgetFeedback(drawList, frameRect, focused, hovered, disabled, rounding);
            ImGui.SetCursorScreenPos(afterFramePos);

            setBaseElementState(elementID, _currentItemStartPos, frameSize, true, edited);
            displayToolTip();
            _elementHoverFramedEnabled = false;
            endElement(style);
            return edited;
        }

        /// <summary>
        /// Resolve Fugui width conventions into a scaled ImGui pixel width.
        /// </summary>
        /// <param name="width">0 uses available width, positive values are scaled pixels, negative values subtract from available width.</param>
        /// <returns>Resolved width in current context pixels.</returns>
        private float ResolveSearchBoxWidth(float width)
        {
            float availableWidth = ImGui.GetContentRegionAvail().x;
            float scale = Fugui.CurrentContext.Scale;

            if (width == 0f)
            {
                return availableWidth;
            }

            if (width < 0f)
            {
                return Mathf.Max(32f * scale, availableWidth + width * scale);
            }

            return Mathf.Max(32f * scale, width * scale);
        }
        #endregion
    }
}
