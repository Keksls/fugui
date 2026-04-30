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
            float spacing = ImGui.GetStyle().ItemInnerSpacing.x;
            float clearWidth = height;
            float inputWidth = Mathf.Max(24f * Fugui.CurrentContext.Scale, totalWidth - clearWidth - spacing);
            bool disabled = LastItemDisabled;
            bool edited;

            ImGui.SetNextItemWidth(inputWidth);
            ImGuiInputTextFlags inputFlags = disabled ? ImGuiInputTextFlags.ReadOnly : ImGuiInputTextFlags.None;
            edited = ImGui.InputTextWithHint("##" + elementID + "_input", hint, ref search, SEARCH_BOX_BUFFER_SIZE, inputFlags);

            ImGui.SameLine(0f, spacing);
            bool canClear = !disabled && !string.IsNullOrEmpty(search);
            if (!canClear)
            {
                ImGui.BeginDisabled();
            }

            bool clearClicked = ImGui.Button("x##" + elementID + "_clear", new Vector2(clearWidth, height));

            if (!canClear)
            {
                ImGui.EndDisabled();
            }

            if (clearClicked)
            {
                search = string.Empty;
                edited = true;
            }

            setBaseElementState(elementID, _currentItemStartPos, new Vector2(totalWidth, height), true, edited);
            displayToolTip();
            _elementHoverFramedEnabled = true;
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