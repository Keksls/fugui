using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Fu.Framework
{
    public static class FuSelectableBuilder
    {
        private static Dictionary<Type, List<int>> _selectablesValues = new Dictionary<Type, List<int>>();
        private static Dictionary<Type, List<IFuSelectable>> _selectablesObjects = new Dictionary<Type, List<IFuSelectable>>();
        private static Dictionary<string, List<IFuSelectable>> _selectablesList = new Dictionary<string, List<IFuSelectable>>();
        private static Dictionary<Type, bool> _typeImplementSelectable = new Dictionary<Type, bool>();
        // A dictionary of integers representing the combo selected indices.
        private static Dictionary<string, int> _selectableSelectedIndices = new Dictionary<string, int>();

        /// <summary>
        /// Get Selectables Data from a enum
        /// </summary>
        /// <typeparam name="TEnum">Type of the enum (must be an enum)</typeparam>
        /// <returns>A dict that store selectables enum values as follow : key is a int that represent the enum value, value is the Selectable object</returns>
        /// <exception cref="ArgumentException">Fail it the tye is not an enum</exception>
        public static void BuildFromEnum<TEnum>(out List<int> values, out List<IFuSelectable> selectables) where TEnum : struct, IConvertible
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
                selectables = new List<IFuSelectable>();
                // iterate over the enum values and add them to the lists
                foreach (TEnum enumValue in Enum.GetValues(typeof(TEnum)))
                {
                    values.Add(enumValue.ToInt32(CultureInfo.InvariantCulture));
                    selectables.Add(new FuSelectable_Text(Fugui.AddSpacesBeforeUppercase(enumValue.ToString()), true));
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
        /// Get Selectables Data from a List of objects
        /// </summary>
        /// <typeparam name="T">Type of the objects in the list</typeparam>
        /// <param name="listID">unique ID of the list (MUST BE UNIQUE)</param>
        /// <param name="items">items to convert</param>
        /// <param name="update">whatever the list has been updated since last process (list or values inside. it's for performances on large. You can handle it using ObservableCollections)</param>
        /// <returns>the Selectables list</returns>
        public static List<IFuSelectable> BuildFromList<T>(string listID, List<T> items, bool update)
        {
            Type type = typeof(T);

            if (!_selectablesList.ContainsKey(listID))
            {
                update = true;
                _selectablesList.Add(listID, new List<IFuSelectable>());
            }
            if (update)
            {
                // Create a list of combobox items from the list of items
                _selectablesList[listID].Clear();

                // check whatever the type of objects inside the list is of type IFuSelectables
                if (!_typeImplementSelectable.ContainsKey(type))
                {
                    _typeImplementSelectable.Add(type, type == typeof(IFuSelectable) || type.GetInterfaces().Contains(typeof(IFuSelectable)));
                }

                // if it's some IFuSelectables
                if (_typeImplementSelectable[type])
                {
                    // we just convert type of the list
                    _selectablesList[listID] = items.OfType<IFuSelectable>().ToList();
                }
                else
                {
                    // let's create a new list and bind it with FuSelectables_Text objects
                    List<IFuSelectable> cItems = _selectablesList[listID];
                    foreach (T item in items)
                    {
                        // Add the item to the list of combobox items
                        cItems.Add(new FuSelectable_Text(Fugui.AddSpacesBeforeUppercase(item.ToString()), true));
                    }
                    _selectablesList[listID] = cItems;
                }
            }

            return _selectablesList[listID];
        }

        /// <summary>
        /// Get the selected index of a selectable list
        /// </summary>
        /// <param name="id">ID of the selectable list</param>
        /// <param name="items">list of selectable items</param>
        /// <param name="itemGetter">how to get the current selecte value string</param>
        /// <returns>the index of the selected index</returns>
        public static int GetSelectedIndex(string id, List<IFuSelectable> items, Func<string> itemGetter)
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
            if (selectedIndex >= items.Count)
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