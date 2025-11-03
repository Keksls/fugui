using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework
{
    public class FuContextMenuBuilder
    {
        public int Count { get => _items.Count; }
        // The list of items in the current context menu
        private List<FuContextMenuItem> _items = null;
        // The current level of items in the context menu
        private List<FuContextMenuItem> _currentLevel = null;

        /// <summary>
        /// Create a new ContextMenuBuilder that will help you to create 
        /// </summary>
        public FuContextMenuBuilder()
        {
            _items = new List<FuContextMenuItem>();
            _currentLevel = _items;
        }

        /// <summary>
        /// Start building a context menu
        /// </summary>
        /// <returns>Context menu builder ready to build</returns>
        public static FuContextMenuBuilder Start()
        {
            return new FuContextMenuBuilder();
        }

        /// <summary>
        /// Set the title of the current context menu level
        /// </summary>
        /// <param name="title"> The title text of the context menu</param>
        /// <returns> The current instance of the FuContextMenuBuilder</returns>
        public FuContextMenuBuilder SetTitle(string title)
        {
            // check if first item is already a title item
            if(_currentLevel.Count > 0 && _currentLevel[0].Type == FuContextMenuItemType.Title)
            {
                return this;
            }

            // insert at the beginning
            _currentLevel.Insert(0, new FuContextMenuItem(title)
            {
                Type = FuContextMenuItemType.Title
            });

            return this;
        }

        /// <summary>
        /// AddItem method adds a new context menu item to the current level with the given label, shortcut, enabled status, and click action.
        /// </summary>
        /// <param name="label"> The label of the context menu item</param>
        /// <param name="clickAction"> The action to perform when the item is clicked</param>
        /// <param name="shortcut"> The shortcut text for the context menu item</param>
        /// <param name="image"> Optional image to display next to the label</param>
        /// <param name="imageSize"> Optional image size</param>
        /// <param name="enabled"> Whatever the item is enabled</param>
        /// <returns> The current instance of the FuContextMenuBuilder</returns>
        public FuContextMenuBuilder AddItem(string label, Action clickAction, string shortcut = "", Texture2D image = null, FuElementSize imageSize = default, Func<bool> enabled = null)
        {
            // Adds a new context menu item to the current level with the given label, shortcut, enabled status, and click action
            _currentLevel.Add(new FuContextMenuItem(label, shortcut, enabled, clickAction, image, imageSize, null));
            return this;
        }

        /// <summary>
        /// AddSeparator method adds a separator to the context menu.
        /// </summary>
        public FuContextMenuBuilder AddSeparator()
        {
            // prevend to add 2 separators on top of each others
            if (_currentLevel.Count > 0 && _currentLevel[_currentLevel.Count - 1].Type == FuContextMenuItemType.Separator)
            {
                return this;
            }
            // Adds a separator to the context menu.
            _currentLevel.Add(new FuContextMenuItem());
            return this;
        }

        /// <summary>
        /// BeginChild method starts a new child context menu with the specified label and sets it enabled by default.
        /// </summary>
        /// <param name="label">Label of the parent item</param>
        /// <param name="image">Optional image to display next to the label</param>
        /// <param name="imageSize">Optional image size</param>
        public FuContextMenuBuilder BeginChild(string label, Texture2D image = null, FuElementSize imageSize = default)
        {
            return BeginChild(label, null, image, imageSize);
        }

        /// <summary>
        /// BeginChild method starts a new child context menu with the specified label and enabled status.
        /// </summary>
        /// <param name="label">Label of the parent item</param>
        /// <param name="enabled">Whatever the item is enabled</param>
        /// <param name="image">Optional image to display next to the label</param>
        /// <param name="imageSize">Optional image size</param>
        public FuContextMenuBuilder BeginChild(string label, Func<bool> enabled, Texture2D image = null, FuElementSize imageSize = default)
        {
            // Starts a new child context menu with the specified label and enabled status.
            var item = new FuContextMenuItem(label, string.Empty, enabled, null, image, imageSize, new List<FuContextMenuItem>());
            _currentLevel.Add(item);
            _currentLevel = item.Children;
            return this;
        }

        /// <summary>
        /// Ends the current child level.
        /// </summary>
        public FuContextMenuBuilder EndChild()
        {
            bool finded = false;
            GetParentLevel(_items, ref finded);
            if (!finded)
            {
                _currentLevel = _items;
            }
            return this;
        }

        /// <summary>
        /// Recursively searches for the parent level of the current level in the context menu items.
        /// </summary>
        /// <param name="level"> The current level of context menu items to search through</param>
        /// <param name="finded"> A reference boolean indicating whether the parent level has been found</param>
        private void GetParentLevel(List<FuContextMenuItem> level, ref bool finded)
        {
            foreach (var item in level)
            {
                if (item.Children == _currentLevel)
                {
                    _currentLevel = level;
                    finded = true;
                    return;
                }

                GetParentLevel(item.Children, ref finded);
            }
        }

        /// <summary>
        /// Builds and returns the context menu items list.
        /// </summary>
        /// <returns>A list of `FuContextMenuItem` objects representing the context menu items.</returns>
        public List<FuContextMenuItem> Build()
        {
            List<FuContextMenuItem> builded = new List<FuContextMenuItem>(_items);
            _items.Clear();
            _currentLevel = _items;
            return builded;
        }
    }
}