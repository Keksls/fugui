using System;
using System.Collections.Generic;
using System.Globalization;

namespace Fu.Framework
{
    /// <summary>
    /// Represents the Fu Selectable Builder type.
    /// </summary>
    public static class FuSelectableBuilder
    {
        #region State
        private const int SelectableEnumCacheCapacity = 256;
        private static readonly FuBoundedCache<Type, List<int>> _selectablesValues =
            new FuBoundedCache<Type, List<int>>(SelectableEnumCacheCapacity);
        private static readonly FuBoundedCache<Type, List<string>> _selectablesObjects =
            new FuBoundedCache<Type, List<string>>(SelectableEnumCacheCapacity);
        private const int SelectableStateCacheCapacity = 1024;
        // A bounded cache of integers representing combo and list selected indices.
        private static readonly FuBoundedCache<string, int> _selectableSelectedIndices =
            new FuBoundedCache<string, int>(SelectableStateCacheCapacity, StringComparer.Ordinal);
        private static readonly FuBoundedCache<string, List<string>> _selectableDisplayLabels =
            new FuBoundedCache<string, List<string>>(SelectableStateCacheCapacity, StringComparer.Ordinal);
        #endregion

        #region Methods
        /// <summary>
        /// Get Selectables Data from a enum
        /// </summary>
        /// <typeparam name="TEnum">Type of the enum (must be an enum)</typeparam>
        /// <returns>A dict that store selectables enum values as follow : key is a int that represent the enum value, value is the Selectable object</returns>
        /// <exception cref="ArgumentException">Fail it the tye is not an enum</exception>
        public static void BuildFromEnum<TEnum>(out List<int> values, out List<string> selectables) where TEnum : struct, IConvertible
        {
            Type type = typeof(TEnum);
            // throw an exception if the type is not an enum
            if (!type.IsEnum)
            {
                throw new ArgumentException(type.Name + " must be an enum type");
            }

            // type not binded, let's bind it
            if (!_selectablesValues.TryGetValue(type, out values) ||
                !_selectablesObjects.TryGetValue(type, out selectables))
            {
                values = new List<int>();
                selectables = new List<string>();
                // iterate over the enum values and add them to the lists
                foreach (TEnum enumValue in Enum.GetValues(typeof(TEnum)))
                {
                    values.Add(enumValue.ToInt32(CultureInfo.InvariantCulture));
                    selectables.Add(enumValue.ToString());
                }
                // Retain enum metadata with a deterministic upper bound.
                _selectablesValues.Set(type, values);
                _selectablesObjects.Set(type, selectables);
            }
        }

        /// <summary>
        /// Get the selected index of a selectable list
        /// </summary>
        /// <param name="id">ID of the selectable list</param>
        /// <param name="items">list of selectable items</param>
        /// <param name="itemGetter">how to get the current selecte value string</param>
        /// <returns>the index of the selected index</returns>
        public static int GetSelectedIndex<T>(string id, List<T> items, Func<string> itemGetter)
        {
            return GetSelectedIndex(id, items, itemGetter?.Invoke());
        }

        /// <summary>
        /// Gets the selected index from an already resolved external value.
        /// </summary>
        /// <typeparam name="T">Selectable item type.</typeparam>
        /// <param name="id">ID of the selectable list.</param>
        /// <param name="items">Selectable items.</param>
        /// <param name="selectedItemString">Current external value, or null.</param>
        /// <returns>The selected item index.</returns>
        internal static int GetSelectedIndex<T>(string id, List<T> items, string selectedItemString)
        {
            // Initialize the selected index for the list
            if (!_selectableSelectedIndices.TryGetValue(id, out int selectedIndex))
            {
                selectedIndex = 0;
                _selectableSelectedIndices.Set(id, selectedIndex);
            }

            // Set current item as setted by getter
            if (!string.IsNullOrEmpty(selectedItemString))
            {
                int i = 0;
                foreach (var item in items)
                {
                    if (item.ToString() == selectedItemString)
                    {
                        selectedIndex = i;
                        SetSelectedIndex(id, i);
                        break;
                    }
                    i++;
                }
            }

            // get and clamp current selectable index
            if (selectedIndex >= items.Count && items.Count > 0)
            {
                selectedIndex = items.Count - 1;
            }

            return selectedIndex;
        }

        /// <summary>
        /// Set the selected index of a selectable list
        /// </summary>
        /// <param name="id">ID of the selectable list</param>
        /// <param name="index">index of the selected item in the list</param>
        public static void SetSelectedIndex(string id, int index)
        {
            // Selection state uses the same bounded lifetime as its owning selectable widget.
            _selectableSelectedIndices.Set(id, index);
        }

        /// <summary>
        /// Gets the cached display labels for a selectable list.
        /// </summary>
        /// <typeparam name="T">The selectable item type.</typeparam>
        /// <param name="id">ID of the selectable list.</param>
        /// <param name="items">List of selectable items.</param>
        /// <param name="listUpdated">Returns true when the list values must be reprocessed.</param>
        /// <returns>The display labels for the selectable list.</returns>
        public static List<string> GetDisplayLabels<T>(string id, List<T> items, Func<bool> listUpdated)
        {
            bool mustRebuild = listUpdated == null || listUpdated();
            if (!_selectableDisplayLabels.TryGetValue(id, out List<string> labels))
            {
                labels = new List<string>(items.Count);
                _selectableDisplayLabels.Set(id, labels);
                mustRebuild = true;
            }

            if (labels.Count != items.Count)
            {
                mustRebuild = true;
            }

            if (mustRebuild)
            {
                labels.Clear();
                int desiredCapacity = Math.Max(8, items.Count);
                int excessiveCapacityThreshold = items.Count <= int.MaxValue / 4
                    ? Math.Max(32, items.Count * 4)
                    : int.MaxValue;
                if (labels.Capacity < items.Count || labels.Capacity > excessiveCapacityThreshold)
                {
                    // Shrink obsolete spikes while keeping a small reserve for normal list fluctuations.
                    labels.Capacity = desiredCapacity;
                }
                for (int i = 0; i < items.Count; i++)
                {
                    labels.Add(items[i] != null ? Fugui.AddSpacesBeforeUppercase(items[i].ToString()) : string.Empty);
                }
            }

            return labels;
        }

        /// <summary>
        /// Clears selectable widget caches owned by the current Fugui session.
        /// </summary>
        internal static void ResetCaches()
        {
            // Enum metadata and per-ID state must not retain references across runtime sessions.
            _selectablesValues.Clear();
            _selectablesObjects.Clear();
            _selectableSelectedIndices.Clear();
            _selectableDisplayLabels.Clear();
        }
        #endregion
    }
}
