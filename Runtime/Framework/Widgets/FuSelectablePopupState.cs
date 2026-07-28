using ImGuiNET;
using System;
using System.Collections.Generic;

namespace Fu.Framework
{
    /// <summary>
    /// Reusable popup renderer for generic combobox and listbox items.
    /// </summary>
    internal sealed class FuSelectablePopupState<T>
    {
        #region State
        private FuLayout _layout;
        private string _selectableId;
        private List<T> _items;
        private Action<int> _indexChange;
        private Action<T> _itemChange;
        private Func<bool> _listUpdated;
        private IList<int> _mappedCallbackValues;
        private bool _highlightSelection;
        private int _selectedIndex;
        internal readonly Action DrawAction;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a selectable popup state with one stable draw delegate.
        /// </summary>
        internal FuSelectablePopupState()
        {
            // The bound delegate is reused for every frame of this cached widget state.
            DrawAction = Draw;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Updates the live inputs consumed when the popup is drawn.
        /// </summary>
        /// <param name="layout">Owning Fugui layout.</param>
        /// <param name="selectableId">Window-scoped selectable identifier.</param>
        /// <param name="items">Current selectable items.</param>
        /// <param name="selectedIndex">Current selected index.</param>
        /// <param name="indexChange">Optional index callback.</param>
        /// <param name="itemChange">Optional selected-item callback.</param>
        /// <param name="listUpdated">Optional display-label invalidation callback.</param>
        /// <param name="mappedCallbackValues">Optional values mapped from visual indices.</param>
        /// <param name="highlightSelection">Whether selected entries use combobox highlighting.</param>
        internal void Prepare(
            FuLayout layout,
            string selectableId,
            List<T> items,
            int selectedIndex,
            Action<int> indexChange,
            Action<T> itemChange,
            Func<bool> listUpdated,
            IList<int> mappedCallbackValues,
            bool highlightSelection)
        {
            // Only references and scalar state change per frame; the draw delegate remains stable.
            _layout = layout;
            _selectableId = selectableId;
            _items = items;
            _selectedIndex = selectedIndex;
            _indexChange = indexChange;
            _itemChange = itemChange;
            _listUpdated = listUpdated;
            _mappedCallbackValues = mappedCallbackValues;
            _highlightSelection = highlightSelection;
        }

        /// <summary>
        /// Draws the current selectable items without allocating a capturing callback.
        /// </summary>
        private void Draw()
        {
            if (_layout == null || _items == null)
            {
                return;
            }

            List<string> displayLabels = FuSelectableBuilder.GetDisplayLabels(_selectableId, _items, _listUpdated);
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i] is null)
                {
                    continue;
                }

                bool selected = _selectedIndex == i;
                int pushedColors = 0;
                if (_highlightSelection && selected)
                {
                    Fugui.Push(ImGuiCol.Header, Fugui.GetColor(FuColors.Selected));
                    Fugui.Push(ImGuiCol.HeaderHovered, Fugui.GetColor(FuColors.SelectedHovered));
                    Fugui.Push(ImGuiCol.HeaderActive, Fugui.GetColor(FuColors.SelectedActive));
                    pushedColors = 3;
                }

                try
                {
                    ImGuiSelectableFlags flags = _layout.LastItemDisabled
                        ? ImGuiSelectableFlags.Disabled
                        : ImGuiSelectableFlags.None;
                    if (!ImGui.Selectable(displayLabels[i], selected, flags))
                    {
                        continue;
                    }

                    _selectedIndex = i;
                    FuSelectableBuilder.SetSelectedIndex(_selectableId, i);
                    int callbackValue = _mappedCallbackValues != null && i < _mappedCallbackValues.Count
                        ? _mappedCallbackValues[i]
                        : i;
                    _indexChange?.Invoke(callbackValue);
                    _itemChange?.Invoke(_items[i]);
                }
                finally
                {
                    if (pushedColors > 0)
                    {
                        Fugui.PopColor(pushedColors);
                    }
                }
            }
        }
        #endregion
    }
}
