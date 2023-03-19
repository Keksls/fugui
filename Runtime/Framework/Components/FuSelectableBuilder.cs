using System;
using System.Collections.Generic;
using System.Globalization;

namespace Fu.Framework
{
    public static class FuSelectableBuilder
    {
        private static Dictionary<Type, List<int>> _selectablesValues = new Dictionary<Type, List<int>>();
        private static Dictionary<Type, List<string>> _selectablesObjects = new Dictionary<Type, List<string>>();
         // A dictionary of integers representing the combo selected indices.
        private static Dictionary<string, int> _selectableSelectedIndices = new Dictionary<string, int>();

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
                    selectables.Add(Fugui.AddSpacesBeforeUppercase(enumValue.ToString()));
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
                    selectedItemString = Fugui.AddSpacesBeforeUppercase(selectedItemString);
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
    }
}