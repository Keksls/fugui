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
        private static Dictionary<Type, List<int>> _selectablesValues = new Dictionary<Type, List<int>>();
        private static Dictionary<Type, List<string>> _selectablesObjects = new Dictionary<Type, List<string>>();
         // A dictionary of integers representing the combo selected indices.
        private static Dictionary<string, int> _selectableSelectedIndices = new Dictionary<string, int>();
        private static Dictionary<string, List<string>> _selectableDisplayLabels = new Dictionary<string, List<string>>();
        private static Dictionary<string, int> _selectableDisplayLabelsCounts = new Dictionary<string, int>();
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
            if (!_selectablesValues.ContainsKey(type))
            {
                values = new List<int>();
                selectables = new List<string>();
                // iterate over the enum values and add them to the lists
                foreach (TEnum enumValue in Enum.GetValues(typeof(TEnum)))
                {
                    values.Add(enumValue.ToInt32(CultureInfo.InvariantCulture));
                    selectables.Add(enumValue.ToString());
                }
                // add values to dic
                _selectablesValues.Add(type, values);
                _selectablesObjects.Add(type, selectables);
            }

            // get values/selectables
            values = _selectablesValues[type];
            selectables = _selectablesObjects[type];
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
            // Initialize the selected index for the list
            if (!_selectableSelectedIndices.ContainsKey(id))
            {
                _selectableSelectedIndices.Add(id, 0);
            }

            // Set current item as setted by getter
            if (itemGetter != null)
            {
                int i = 0;
                string selectedItemString = itemGetter.Invoke();
                if (!string.IsNullOrEmpty(selectedItemString))
                {
                    foreach (var item in items)
                    {
                        if (item.ToString() == selectedItemString)
                        {
                            SetSelectedIndex(id, i);
                            break;
                        }
                        i++;
                    }
                }
            }

            // get and clamp current selectable index
            int selectedIndex = _selectableSelectedIndices[id];
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
            _selectableSelectedIndices[id] = index;
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
                _selectableDisplayLabels.Add(id, labels);
                mustRebuild = true;
            }

            if (!_selectableDisplayLabelsCounts.TryGetValue(id, out int cachedCount) || cachedCount != items.Count)
            {
                mustRebuild = true;
            }

            if (mustRebuild)
            {
                labels.Clear();
                if (labels.Capacity < items.Count)
                {
                    labels.Capacity = items.Count;
                }
                for (int i = 0; i < items.Count; i++)
                {
                    labels.Add(items[i] != null ? Fugui.AddSpacesBeforeUppercase(items[i].ToString()) : string.Empty);
                }
                _selectableDisplayLabelsCounts[id] = items.Count;
            }

            return labels;
        }
        #endregion
    }
}
