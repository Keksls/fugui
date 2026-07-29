using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework
{
    /// <summary>
    /// Data table widgets.
    /// </summary>
    public partial class FuLayout
    {
        #region State
        private const int TableViewStateCacheCapacity = 256;
        private static readonly FuBoundedCache<string, object> _tableViewStates =
            new FuBoundedCache<string, object>(TableViewStateCacheCapacity, StringComparer.Ordinal);
        #endregion

        #region Methods
        /// <summary>
        /// Draw a data table view without exposing row selection state.
        /// </summary>
        /// <typeparam name="T">Row item type.</typeparam>
        /// <param name="id">Unique ID of the table.</param>
        /// <param name="items">Source items.</param>
        /// <param name="columns">Column definitions.</param>
        /// <param name="searchQuery">Optional search query. All terms must match one of the searchable fields.</param>
        /// <param name="searchTextGetter">Optional row-level search text. If null, searchable column text is used.</param>
        /// <param name="height">Table height. 0 uses auto height, positive values are scaled pixels, negative values subtract from available height.</param>
        /// <param name="flags">Table view behaviour flags.</param>
        /// <returns>true if an internal temporary selection changed this frame.</returns>
        public bool TableView<T>(string id, IList<T> items, IList<FuTableViewColumn<T>> columns, string searchQuery = null, Func<T, string> searchTextGetter = null, float height = 0f, FuTableViewFlags flags = FuTableViewFlags.Default)
        {
            int selectedIndex = -1;
            return TableView(id, items, columns, ref selectedIndex, searchQuery, searchTextGetter, height, flags);
        }

        /// <summary>
        /// Draw a data table view with optional filtering, sorting and single row selection.
        /// </summary>
        /// <typeparam name="T">Row item type.</typeparam>
        /// <param name="id">Unique ID of the table.</param>
        /// <param name="items">Source items.</param>
        /// <param name="columns">Column definitions.</param>
        /// <param name="selectedIndex">Selected source item index, or -1.</param>
        /// <param name="searchQuery">Optional search query. All terms must match one of the searchable fields.</param>
        /// <param name="searchTextGetter">Optional row-level search text. If null, searchable column text is used.</param>
        /// <param name="height">Table height. 0 uses auto height, positive values are scaled pixels, negative values subtract from available height.</param>
        /// <param name="flags">Table view behaviour flags.</param>
        /// <returns>true if the selected source index changed this frame.</returns>
        public virtual bool TableView<T>(string id, IList<T> items, IList<FuTableViewColumn<T>> columns, ref int selectedIndex, string searchQuery = null, Func<T, string> searchTextGetter = null, float height = 0f, FuTableViewFlags flags = FuTableViewFlags.Default)
        {
            string tableID = id;
            beginElement(ref tableID, canBeHidden: false);
            if (!_drawElement)
            {
                return false;
            }

            bool selectionChanged = false;
            if (items == null || columns == null || columns.Count == 0)
            {
                ImGui.TextUnformatted("No table data");
                setBaseElementState(tableID, _currentItemStartPos, ImGui.GetItemRectMax() - _currentItemStartPos, false, false);
                endElement();
                return false;
            }

            FuTableViewState<T> state = GetTableViewState<T>(tableID);
            List<FuTableViewRow<T>> rows = BuildTableViewRows(state, items, columns, searchQuery, searchTextGetter);
            Vector2 outerSize = new Vector2(ImGui.GetContentRegionAvail().x, ResolveTableViewHeight(height, flags));
            ImGuiTableFlags tableFlags = BuildImGuiTableFlags(flags);

            if (ImGui.BeginTable(tableID, columns.Count, tableFlags, outerSize))
            {
                SetupTableViewColumns(columns, flags);

                if (!flags.HasFlag(FuTableViewFlags.NoHeader))
                {
                    ImGui.TableSetupScrollFreeze(0, 1);
                    ImGui.TableHeadersRow();
                }

                ApplyTableViewSort(state, rows, columns, flags);
                selectionChanged = DrawTableViewRows(state, rows, columns, ref selectedIndex, flags);

                ImGui.EndTable();
            }

            setBaseElementState(tableID, _currentItemStartPos, ImGui.GetItemRectMax() - _currentItemStartPos, true, selectionChanged);
            displayToolTip();
            endElement();
            return selectionChanged;
        }

        /// <summary>
        /// Build the visible row list while keeping source indices stable for selection callbacks.
        /// </summary>
        /// <typeparam name="T">Row item type.</typeparam>
        /// <param name="state">Reusable state owned by the table identifier.</param>
        /// <param name="items">Source items.</param>
        /// <param name="columns">Column definitions used for default search text.</param>
        /// <param name="searchQuery">Optional search query.</param>
        /// <param name="searchTextGetter">Optional row-level search text.</param>
        /// <returns>Filtered rows with original source indices.</returns>
        private List<FuTableViewRow<T>> BuildTableViewRows<T>(FuTableViewState<T> state, IList<T> items, IList<FuTableViewColumn<T>> columns, string searchQuery, Func<T, string> searchTextGetter)
        {
            // Reuse the typed row buffer while rebuilding visibility from live source data.
            List<FuTableViewRow<T>> rows = state.Rows;
            rows.Clear();
            state.PrepareItemCapacity(items.Count);

            for (int i = 0; i < items.Count; i++)
            {
                T item = items[i];
                if (!PassesTableViewSearch(state, item, columns, searchQuery, searchTextGetter))
                {
                    continue;
                }

                rows.Add(new FuTableViewRow<T>(i, item));
            }

            return rows;
        }

        /// <summary>
        /// Check whether a row item matches the active table search query.
        /// </summary>
        /// <typeparam name="T">Row item type.</typeparam>
        /// <param name="state">Reusable state owned by the table identifier.</param>
        /// <param name="item">Source row item.</param>
        /// <param name="columns">Column definitions used for default search text.</param>
        /// <param name="searchQuery">Optional search query.</param>
        /// <param name="searchTextGetter">Optional row-level search text.</param>
        /// <returns>true if the row should be visible.</returns>
        private bool PassesTableViewSearch<T>(FuTableViewState<T> state, T item, IList<FuTableViewColumn<T>> columns, string searchQuery, Func<T, string> searchTextGetter)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return true;
            }

            if (searchTextGetter != null)
            {
                return FuSearchFilter.Passes(searchQuery, searchTextGetter(item));
            }

            string[] values = state.GetSearchValues(columns.Count);
            for (int i = 0; i < columns.Count; i++)
            {
                values[i] = columns[i].GetSearchText(item);
            }

            return FuSearchFilter.Passes(searchQuery, false, true, values);
        }

        /// <summary>
        /// Convert Fugui table view flags to ImGui table flags.
        /// </summary>
        /// <param name="flags">Fugui table view flags.</param>
        /// <returns>Equivalent ImGui table flags.</returns>
        private ImGuiTableFlags BuildImGuiTableFlags(FuTableViewFlags flags)
        {
            ImGuiTableFlags tableFlags = ImGuiTableFlags.SizingStretchProp;

            if (flags.HasFlag(FuTableViewFlags.RowBackground))
            {
                tableFlags |= ImGuiTableFlags.RowBg;
            }
            if (flags.HasFlag(FuTableViewFlags.Borders))
            {
                tableFlags |= ImGuiTableFlags.Borders;
            }
            if (flags.HasFlag(FuTableViewFlags.ResizableColumns))
            {
                tableFlags |= ImGuiTableFlags.Resizable;
            }
            if (flags.HasFlag(FuTableViewFlags.ReorderableColumns))
            {
                tableFlags |= ImGuiTableFlags.Reorderable;
            }
            if (flags.HasFlag(FuTableViewFlags.HideableColumns))
            {
                tableFlags |= ImGuiTableFlags.Hideable;
            }
            if (flags.HasFlag(FuTableViewFlags.Sortable))
            {
                tableFlags |= ImGuiTableFlags.Sortable;
            }
            if (flags.HasFlag(FuTableViewFlags.ScrollX))
            {
                tableFlags |= ImGuiTableFlags.ScrollX;
            }
            if (flags.HasFlag(FuTableViewFlags.ScrollY))
            {
                tableFlags |= ImGuiTableFlags.ScrollY;
            }
            if (flags.HasFlag(FuTableViewFlags.NoSavedSettings))
            {
                tableFlags |= ImGuiTableFlags.NoSavedSettings;
            }

            return tableFlags;
        }

        /// <summary>
        /// Resolve Fugui height conventions into the ImGui table outer height.
        /// </summary>
        /// <param name="height">0 uses automatic height, positive values are scaled pixels, negative values subtract from available height.</param>
        /// <param name="flags">Table view flags, used to detect vertical scrolling mode.</param>
        /// <returns>Resolved height in current context pixels, or 0 for automatic height.</returns>
        private float ResolveTableViewHeight(float height, FuTableViewFlags flags)
        {
            if (!flags.HasFlag(FuTableViewFlags.ScrollY))
            {
                return height > 0f ? height * Fugui.CurrentContext.Scale : 0f;
            }

            float availableHeight = ImGui.GetContentRegionAvail().y;
            if (height == 0f)
            {
                return Mathf.Max(64f * Fugui.CurrentContext.Scale, availableHeight);
            }

            if (height < 0f)
            {
                return Mathf.Max(64f * Fugui.CurrentContext.Scale, availableHeight + height * Fugui.CurrentContext.Scale);
            }

            return height * Fugui.CurrentContext.Scale;
        }

        /// <summary>
        /// Register all table columns with ImGui before rows are rendered.
        /// </summary>
        /// <typeparam name="T">Row item type.</typeparam>
        /// <param name="columns">Column definitions.</param>
        /// <param name="tableViewFlags">Fugui table view flags used to disable unsupported sorting.</param>
        private void SetupTableViewColumns<T>(IList<FuTableViewColumn<T>> columns, FuTableViewFlags tableViewFlags)
        {
            for (int i = 0; i < columns.Count; i++)
            {
                FuTableViewColumn<T> column = columns[i];
                ImGuiTableColumnFlags columnFlags = (ImGuiTableColumnFlags)column.Flags;
                if (!tableViewFlags.HasFlag(FuTableViewFlags.Sortable) || !column.CanSort)
                {
                    columnFlags |= ImGuiTableColumnFlags.NoSort;
                }

                if (column.Width > 0f && (columnFlags & ImGuiTableColumnFlags.WidthMask) == 0)
                {
                    columnFlags |= ImGuiTableColumnFlags.WidthFixed;
                }

                float width = column.Width > 0f ? column.Width * Fugui.CurrentContext.Scale : 0f;
                ImGui.TableSetupColumn(column.Header, columnFlags, width, (uint)i);
            }
        }

        /// <summary>
        /// Apply the current ImGui single-column sort to the filtered row list.
        /// </summary>
        /// <typeparam name="T">Row item type.</typeparam>
        /// <param name="state">Reusable state containing the stable comparison delegate.</param>
        /// <param name="rows">Filtered rows to sort in-place.</param>
        /// <param name="columns">Column definitions used to resolve the active comparer.</param>
        /// <param name="flags">Table view flags used to disable sorting.</param>
        private unsafe void ApplyTableViewSort<T>(FuTableViewState<T> state, List<FuTableViewRow<T>> rows, IList<FuTableViewColumn<T>> columns, FuTableViewFlags flags)
        {
            if (!flags.HasFlag(FuTableViewFlags.Sortable) || rows.Count <= 1)
            {
                return;
            }

            ImGuiTableSortSpecsPtr sortSpecs = ImGui.TableGetSortSpecs();
            if (sortSpecs.NativePtr == null || sortSpecs.SpecsCount <= 0 || sortSpecs.Specs.NativePtr == null)
            {
                return;
            }

            ImGuiTableColumnSortSpecsPtr spec = sortSpecs.Specs;
            int columnIndex = spec.ColumnIndex;
            if (columnIndex < 0 || columnIndex >= columns.Count || !columns[columnIndex].CanSort)
            {
                return;
            }

            FuTableViewColumn<T> column = columns[columnIndex];
            state.SortColumn = column;
            state.SortDescending = spec.SortDirection == ImGuiSortDirection.Descending;
            rows.Sort(state.RowComparison);

            if (sortSpecs.SpecsDirty)
            {
                sortSpecs.SpecsDirty = false;
            }
        }

        /// <summary>
        /// Draw all visible rows, optionally using the shared ImGui list clipper.
        /// </summary>
        /// <typeparam name="T">Row item type.</typeparam>
        /// <param name="state">Reusable state containing stable row identifiers.</param>
        /// <param name="rows">Filtered and sorted rows to draw.</param>
        /// <param name="columns">Column definitions.</param>
        /// <param name="selectedIndex">Selected source item index, or -1.</param>
        /// <param name="flags">Table view behaviour flags.</param>
        /// <returns>true if the selected source index changed this frame.</returns>
        private bool DrawTableViewRows<T>(FuTableViewState<T> state, List<FuTableViewRow<T>> rows, IList<FuTableViewColumn<T>> columns, ref int selectedIndex, FuTableViewFlags flags)
        {
            bool selectionChanged = false;
            float rowHeight = ImGui.GetFrameHeight();

            if (rows.Count == 0)
            {
                ImGui.TableNextRow(ImGuiTableRowFlags.None, rowHeight);
                ImGui.TableSetColumnIndex(0);
                ImGui.TextUnformatted("No matching rows");
                return false;
            }

            if (flags.HasFlag(FuTableViewFlags.UseClipper))
            {
                Fugui.ListClipperBegin(rows.Count, rowHeight);
                try
                {
                    while (Fugui.ListClipperStep())
                    {
                        int start = Mathf.Clamp(Fugui.ListClipperDisplayStart(), 0, rows.Count);
                        int end = Mathf.Clamp(Fugui.ListClipperDisplayEnd(), start, rows.Count);
                        for (int i = start; i < end; i++)
                        {
                            selectionChanged |= DrawTableViewRow(state, rows[i], columns, ref selectedIndex, flags, rowHeight);
                        }
                    }
                }
                finally
                {
                    Fugui.ListClipperEnd();
                }
            }
            else
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    selectionChanged |= DrawTableViewRow(state, rows[i], columns, ref selectedIndex, flags, rowHeight);
                }
            }

            return selectionChanged;
        }

        /// <summary>
        /// Draw one table row and update the single selection when requested.
        /// </summary>
        /// <typeparam name="T">Row item type.</typeparam>
        /// <param name="state">Reusable state containing stable row identifiers.</param>
        /// <param name="row">Row to draw, including its source index.</param>
        /// <param name="columns">Column definitions.</param>
        /// <param name="selectedIndex">Selected source item index, or -1.</param>
        /// <param name="flags">Table view behaviour flags.</param>
        /// <param name="rowHeight">Stable row height used for selectable hit boxes and clipping.</param>
        /// <returns>true if the selected source index changed this frame.</returns>
        private bool DrawTableViewRow<T>(FuTableViewState<T> state, FuTableViewRow<T> row, IList<FuTableViewColumn<T>> columns, ref int selectedIndex, FuTableViewFlags flags, float rowHeight)
        {
            ImGui.TableNextRow(ImGuiTableRowFlags.None, rowHeight);
            bool selectionChanged = false;
            bool selected = selectedIndex == row.SourceIndex;

            if (selected)
            {
                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(Fugui.GetColor(FuColors.Selected)));
            }

            if (flags.HasFlag(FuTableViewFlags.SelectableRows))
            {
                ImGui.TableSetColumnIndex(0);
                Vector2 rowStart = ImGui.GetCursorScreenPos();
                ImGuiSelectableFlags selectableFlags = ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick;
                if (LastItemDisabled)
                {
                    // Table rows use raw ImGui selectables, so the shared Fugui disabled state must be forwarded explicitly.
                    selectableFlags |= ImGuiSelectableFlags.Disabled;
                }

                if (ImGui.Selectable(state.RowIds[row.SourceIndex], selected, selectableFlags, new Vector2(0f, rowHeight)))
                {
                    if (selectedIndex != row.SourceIndex)
                    {
                        selectedIndex = row.SourceIndex;
                        selectionChanged = true;
                    }
                }
                ImGui.SetCursorScreenPos(rowStart);
            }

            for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
            {
                if (!ImGui.TableSetColumnIndex(columnIndex))
                {
                    continue;
                }

                DrawTableViewCell(row.Item, columns[columnIndex]);
            }

            return selectionChanged;
        }

        /// <summary>
        /// Draw a table cell through either a custom callback or the column text getter.
        /// </summary>
        /// <typeparam name="T">Row item type.</typeparam>
        /// <param name="item">Row item being rendered.</param>
        /// <param name="column">Column definition for this cell.</param>
        private void DrawTableViewCell<T>(T item, FuTableViewColumn<T> column)
        {
            if (column.DrawCell != null)
            {
                column.DrawCell(item);
                return;
            }

            string text = column.GetText(item);
            switch (column.Wrapping)
            {
                case FuTextWrapping.Wrap:
                    ImGui.TextWrapped(text);
                    break;
                default:
                    ImGui.TextUnformatted(text);
                    break;
            }
        }

        /// <summary>
        /// Gets or creates reusable typed state for one table view.
        /// </summary>
        /// <typeparam name="T">Row item type.</typeparam>
        /// <param name="tableId">Resolved table identifier.</param>
        /// <returns>Reusable row, search and sorting buffers.</returns>
        private static FuTableViewState<T> GetTableViewState<T>(string tableId)
        {
            // Reusing an ID with another row type replaces the incompatible bounded entry.
            if (!_tableViewStates.TryGetValue(tableId, out object cachedState) ||
                !(cachedState is FuTableViewState<T> state))
            {
                state = new FuTableViewState<T>(tableId);
                _tableViewStates.Set(tableId, state);
            }

            return state;
        }

        /// <summary>
        /// Clears table view buffers owned by the current Fugui session.
        /// </summary>
        internal static void ResetTableViewState()
        {
            // Typed states retain caller row references and must follow the runtime lifetime.
            _tableViewStates.Clear();
        }
        #endregion

        #region Nested Types
        private readonly struct FuTableViewRow<T>
        {
            public readonly int SourceIndex;
            public readonly T Item;

            /// <summary>
            /// Store a visible row with its original source index.
            /// </summary>
            /// <param name="sourceIndex">Index in the unfiltered source item list.</param>
            /// <param name="item">Source row item.</param>
            public FuTableViewRow(int sourceIndex, T item)
            {
                SourceIndex = sourceIndex;
                Item = item;
            }
        }

        private sealed class FuTableViewState<T>
        {
            public readonly List<FuTableViewRow<T>> Rows = new List<FuTableViewRow<T>>();
            public readonly Comparison<FuTableViewRow<T>> RowComparison;
            public FuTableViewColumn<T> SortColumn;
            public bool SortDescending;
            public string[] RowIds = Array.Empty<string>();
            private readonly string _tableId;
            private string[] _searchValues = Array.Empty<string>();

            /// <summary>
            /// Creates reusable table buffers and a stable sort delegate.
            /// </summary>
            /// <param name="tableId">Resolved table identifier.</param>
            public FuTableViewState(string tableId)
            {
                // The comparison delegate is allocated once instead of once per sorted frame.
                _tableId = tableId;
                RowComparison = CompareRows;
            }

            /// <summary>
            /// Prepares row and identifier capacity for the current item count.
            /// </summary>
            /// <param name="itemCount">Current source item count.</param>
            public void PrepareItemCapacity(int itemCount)
            {
                // Managed buffers grow geometrically and shed obsolete fourfold spikes.
                int requiredCapacity = Mathf.NextPowerOfTwo(Mathf.Max(4, itemCount));
                if (RowIds.Length < itemCount)
                {
                    Array.Resize(ref RowIds, requiredCapacity);
                }
                else if (RowIds.Length > 16 && requiredCapacity <= RowIds.Length / 4)
                {
                    Array.Resize(ref RowIds, Mathf.Max(8, requiredCapacity * 2));
                }

                int excessiveRowCapacity = itemCount <= int.MaxValue / 4
                    ? Mathf.Max(32, itemCount * 4)
                    : int.MaxValue;
                if (Rows.Capacity < itemCount || Rows.Capacity > excessiveRowCapacity)
                {
                    Rows.Capacity = Mathf.Max(8, itemCount);
                }

                for (int i = 0; i < itemCount; i++)
                {
                    if (RowIds[i] == null)
                    {
                        RowIds[i] = "##" + _tableId + "_row_" + i;
                    }
                }
            }

            /// <summary>
            /// Gets a reusable search-value array matching the active column count.
            /// </summary>
            /// <param name="columnCount">Number of searchable columns.</param>
            /// <returns>Exact-length search-value buffer.</returns>
            public string[] GetSearchValues(int columnCount)
            {
                // Exact length prevents stale values from participating in the params-based filter.
                if (_searchValues.Length != columnCount)
                {
                    _searchValues = new string[columnCount];
                }

                return _searchValues;
            }

            /// <summary>
            /// Compares two visible rows using the current ImGui sort specification.
            /// </summary>
            /// <param name="left">Left row.</param>
            /// <param name="right">Right row.</param>
            /// <returns>Sort order for the current column and direction.</returns>
            private int CompareRows(FuTableViewRow<T> left, FuTableViewRow<T> right)
            {
                // Sort fields are updated before List.Sort invokes this stable delegate.
                if (SortColumn == null)
                {
                    return 0;
                }

                int result = SortColumn.Compare(left.Item, right.Item);
                if (!SortDescending)
                {
                    return result;
                }

                return result > 0 ? -1 : result < 0 ? 1 : 0;
            }
        }
        #endregion
    }
}
