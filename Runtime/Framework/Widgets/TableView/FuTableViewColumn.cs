using ImGuiNET;
using System;

namespace Fu.Framework
{
    /// <summary>
    /// Column definition used by Fugui table views.
    /// </summary>
    public sealed class FuTableViewColumn<T>
    {
        #region State
        public string Header { get; }
        public float Width { get; }
        public ImGuiTableColumnFlags Flags { get; }
        public FuTextWrapping Wrapping { get; }
        public Func<T, string> TextGetter { get; }
        public Func<T, string> SearchGetter { get; }
        public Action<T, FuLayout> DrawCell { get; }
        public Comparison<T> SortComparison { get; }
        public bool CanSort => SortComparison != null || TextGetter != null;
        #endregion

        #region Constructors
        /// <summary>
        /// Create a text column with optional search and sort customization.
        /// </summary>
        /// <param name="header">Header label shown in the table.</param>
        /// <param name="textGetter">Text renderer used for the cell value and default sorting/searching.</param>
        /// <param name="width">Initial column width in unscaled Fugui pixels. 0 lets ImGui stretch the column.</param>
        /// <param name="flags">Extra ImGui column flags.</param>
        /// <param name="sortComparison">Optional typed comparison used when this column is sorted.</param>
        /// <param name="searchGetter">Optional searchable text for this column.</param>
        /// <param name="wrapping">Text wrapping mode used when drawing this column.</param>
        public FuTableViewColumn(string header, Func<T, string> textGetter, float width = 0f, ImGuiTableColumnFlags flags = ImGuiTableColumnFlags.None, Comparison<T> sortComparison = null, Func<T, string> searchGetter = null, FuTextWrapping wrapping = FuTextWrapping.Clip)
        {
            Header = header ?? string.Empty;
            TextGetter = textGetter;
            SearchGetter = searchGetter;
            Width = width;
            Flags = flags;
            SortComparison = sortComparison;
            Wrapping = wrapping;
        }

        /// <summary>
        /// Create a custom-drawn column with optional search and sort customization.
        /// </summary>
        /// <param name="header">Header label shown in the table.</param>
        /// <param name="drawCell">Callback used to draw each cell with the current layout.</param>
        /// <param name="width">Initial column width in unscaled Fugui pixels. 0 lets ImGui stretch the column.</param>
        /// <param name="flags">Extra ImGui column flags.</param>
        /// <param name="sortComparison">Optional typed comparison used when this column is sorted.</param>
        /// <param name="searchGetter">Optional searchable text for this column.</param>
        public FuTableViewColumn(string header, Action<T, FuLayout> drawCell, float width = 0f, ImGuiTableColumnFlags flags = ImGuiTableColumnFlags.None, Comparison<T> sortComparison = null, Func<T, string> searchGetter = null)
        {
            Header = header ?? string.Empty;
            DrawCell = drawCell;
            SearchGetter = searchGetter;
            Width = width;
            Flags = flags;
            SortComparison = sortComparison;
            Wrapping = FuTextWrapping.None;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Helper factory for columns that draw custom content instead of plain text.
        /// </summary>
        /// <param name="header">Header label shown in the table.</param>
        /// <param name="drawCell">Callback used to draw each cell with the current layout.</param>
        /// <param name="width">Initial column width in unscaled Fugui pixels. 0 lets ImGui stretch the column.</param>
        /// <param name="flags">Extra ImGui column flags.</param>
        /// <param name="sortComparison">Optional typed comparison used when this column is sorted.</param>
        /// <param name="searchGetter">Optional searchable text for this column.</param>
        /// <returns>A custom-drawn table view column.</returns>
        public static FuTableViewColumn<T> Custom(string header, Action<T, FuLayout> drawCell, float width = 0f, ImGuiTableColumnFlags flags = ImGuiTableColumnFlags.None, Comparison<T> sortComparison = null, Func<T, string> searchGetter = null)
        {
            return new FuTableViewColumn<T>(header, drawCell, width, flags, sortComparison, searchGetter);
        }

        /// <summary>
        /// Resolve the display text for a row item.
        /// </summary>
        /// <param name="item">Row item.</param>
        /// <returns>Cell display text.</returns>
        internal string GetText(T item)
        {
            if (TextGetter != null)
            {
                return TextGetter(item) ?? string.Empty;
            }

            return item?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Resolve the searchable text for a row item.
        /// </summary>
        /// <param name="item">Row item.</param>
        /// <returns>Searchable text for this column.</returns>
        internal string GetSearchText(T item)
        {
            if (SearchGetter != null)
            {
                return SearchGetter(item) ?? string.Empty;
            }

            return TextGetter != null ? GetText(item) : string.Empty;
        }

        /// <summary>
        /// Compare two row items for table sorting.
        /// </summary>
        /// <param name="left">Left row item.</param>
        /// <param name="right">Right row item.</param>
        /// <returns>Standard comparison result.</returns>
        internal int Compare(T left, T right)
        {
            if (SortComparison != null)
            {
                return SortComparison(left, right);
            }

            return string.Compare(GetText(left), GetText(right), StringComparison.OrdinalIgnoreCase);
        }
        #endregion
    }
}