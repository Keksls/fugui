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

            List<FuTableViewRow<T>> rows = BuildTableViewRows(items, columns, searchQuery, searchTextGetter);
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

                ApplyTableViewSort(rows, columns, flags);
                selectionChanged = DrawTableViewRows(tableID, rows, columns, ref selectedIndex, flags);

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
        /// <param name="items">Source items.</param>
        /// <param name="columns">Column definitions used for default search text.</param>
        /// <param name="searchQuery">Optional search query.</param>
        /// <param name="searchTextGetter">Optional row-level search text.</param>
        /// <returns>Filtered rows with original source indices.</returns>
        private List<FuTableViewRow<T>> BuildTableViewRows<T>(IList<T> items, IList<FuTableViewColumn<T>> columns, string searchQuery, Func<T, string> searchTextGetter)
        {
            List<FuTableViewRow<T>> rows = new List<FuTableViewRow<T>>(items.Count);

            for (int i = 0; i < items.Count; i++)
            {
                T item = items[i];
                if (!PassesTableViewSearch(item, columns, searchQuery, searchTextGetter))
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
        /// <param name="item">Source row item.</param>
        /// <param name="columns">Column definitions used for default search text.</param>
        /// <param name="searchQuery">Optional search query.</param>
        /// <param name="searchTextGetter">Optional row-level search text.</param>
        /// <returns>true if the row should be visible.</returns>
        private bool PassesTableViewSearch<T>(T item, IList<FuTableViewColumn<T>> columns, string searchQuery, Func<T, string> searchTextGetter)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return true;
            }

            if (searchTextGetter != null)
            {
                return FuSearchFilter.Passes(searchQuery, searchTextGetter(item));
            }

            string[] values = new string[columns.Count];
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
                ImGuiTableColumnFlags columnFlags = column.Flags;
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
        /// <param name="rows">Filtered rows to sort in-place.</param>
        /// <param name="columns">Column definitions used to resolve the active comparer.</param>
        /// <param name="flags">Table view flags used to disable sorting.</param>
        private unsafe void ApplyTableViewSort<T>(List<FuTableViewRow<T>> rows, IList<FuTableViewColumn<T>> columns, FuTableViewFlags flags)
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
            bool descending = spec.SortDirection == ImGuiSortDirection.Descending;
            rows.Sort((left, right) =>
            {
                int result = column.Compare(left.Item, right.Item);
                return descending ? -result : result;
            });

            if (sortSpecs.SpecsDirty)
            {
                sortSpecs.SpecsDirty = false;
            }
        }

        /// <summary>
        /// Draw all visible rows, optionally using the shared ImGui list clipper.
        /// </summary>
        /// <typeparam name="T">Row item type.</typeparam>
        /// <param name="tableID">Resolved table ID.</param>
        /// <param name="rows">Filtered and sorted rows to draw.</param>
        /// <param name="columns">Column definitions.</param>
        /// <param name="selectedIndex">Selected source item index, or -1.</param>
        /// <param name="flags">Table view behaviour flags.</param>
        /// <returns>true if the selected source index changed this frame.</returns>
        private bool DrawTableViewRows<T>(string tableID, List<FuTableViewRow<T>> rows, IList<FuTableViewColumn<T>> columns, ref int selectedIndex, FuTableViewFlags flags)
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
                while (Fugui.ListClipperStep())
                {
                    int start = Mathf.Clamp(Fugui.ListClipperDisplayStart(), 0, rows.Count);
                    int end = Mathf.Clamp(Fugui.ListClipperDisplayEnd(), start, rows.Count);
                    for (int i = start; i < end; i++)
                    {
                        selectionChanged |= DrawTableViewRow(tableID, rows[i], columns, ref selectedIndex, flags, rowHeight);
                    }
                }
                Fugui.ListClipperEnd();
            }
            else
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    selectionChanged |= DrawTableViewRow(tableID, rows[i], columns, ref selectedIndex, flags, rowHeight);
                }
            }

            return selectionChanged;
        }

        /// <summary>
        /// Draw one table row and update the single selection when requested.
        /// </summary>
        /// <typeparam name="T">Row item type.</typeparam>
        /// <param name="tableID">Resolved table ID.</param>
        /// <param name="row">Row to draw, including its source index.</param>
        /// <param name="columns">Column definitions.</param>
        /// <param name="selectedIndex">Selected source item index, or -1.</param>
        /// <param name="flags">Table view behaviour flags.</param>
        /// <param name="rowHeight">Stable row height used for selectable hit boxes and clipping.</param>
        /// <returns>true if the selected source index changed this frame.</returns>
        private bool DrawTableViewRow<T>(string tableID, FuTableViewRow<T> row, IList<FuTableViewColumn<T>> columns, ref int selectedIndex, FuTableViewFlags flags, float rowHeight)
        {
            ImGui.TableNextRow(ImGuiTableRowFlags.None, rowHeight);
            bool selectionChanged = false;
            bool selected = selectedIndex == row.SourceIndex;

            if (selected)
            {
                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.Selected)));
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

                if (ImGui.Selectable("##" + tableID + "_row_" + row.SourceIndex, selected, selectableFlags, new Vector2(0f, rowHeight)))
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
                column.DrawCell(item, this);
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
        #endregion
    }
}
