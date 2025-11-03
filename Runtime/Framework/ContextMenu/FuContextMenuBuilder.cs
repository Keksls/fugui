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
        /// Adds an item to the current level of the context menu with a label and click action
        /// </summary>
        /// <param name="label">The label text of the item</param>
        /// <param name="clickAction">The action to perform when the item is clicked</param>
        public FuContextMenuBuilder AddTitle(string label)
        {
            // Adds a context menu item with label and click action.
            _currentLevel.Add(new FuContextMenuItem(label));
            return this;
        }


        /// <summary>
        /// Adds an item to the current level of the context menu with a label, shortcut, and click action
        /// </summary>
        /// <param name="label">The label text of the item</param>
        /// <param name="shortcut">The keyboard shortcut of the item</param>
        /// <param name="enabled">Whether the item is enabled or not</param>
        /// <param name="clickAction">The action to perform when the item is clicked</param>
        public FuContextMenuBuilder AddItem(string label, string shortcut, Func<bool> enabled, Action clickAction)
        {
            // Adds a new context menu item to the current level with the given label, shortcut, enabled status, and click action
            _currentLevel.Add(new FuContextMenuItem(label, shortcut, enabled, false, clickAction));
            return this;
        }

        /// <summary>
        /// Adds an item to the current level of the context menu with a label and enabled status
        /// </summary>
        /// <param name="label">The label text of the item</param>
        /// <param name="enabled">Whether the item is enabled or not</param>
        public FuContextMenuBuilder AddItem(string label, Func<bool> enabled)
        {
            // Adds a new context menu item to the current level with the given label and enabled status
            _currentLevel.Add(new FuContextMenuItem(label, string.Empty, enabled, false, null));
            return this;
        }

        /// <summary>
        /// Adds an enabled item to the current level of the context menu with a label
        /// </summary>
        /// <param name="label">The label text of the item</param>
        public FuContextMenuBuilder AddItem(string label)
        {
            // Adds a new enabled context menu item to the current level with the given label
            _currentLevel.Add(new FuContextMenuItem(label, string.Empty, null, false, null));
            return this;
        }

        /// <summary>
        /// Adds an enabled item to the current level of the context menu with a label and keyboard shortcut
        /// </summary>
        /// <param name="label">The label text of the item</param>
        /// <param name="shortcut">The keyboard shortcut of the item</param>
        /// <param name="clickAction">The action to perform when the item is clicked</param>
        public FuContextMenuBuilder AddItem(string label, string shortcut, Action clickAction)
        {
            // Adds a new enabled context menu item to the current level with the given label and keyboard shortcut
            _currentLevel.Add(new FuContextMenuItem(label, shortcut, null, false, clickAction));
            return this;
        }

        /// <summary>
        /// Adds an item to the current level of the context menu with a label, enabled status, and click action
        /// </summary>
        /// <param name="label">The label text of the item</param>
        /// <param name="enabled">Whether the item is enabled or not</param>
        /// <param name="clickAction">The action to perform when the item is clicked</param>
        public FuContextMenuBuilder AddItem(string label, Func<bool> enabled, Action clickAction)
        {
            // Adds a context menu item with label, enabled status, and click action.
            _currentLevel.Add(new FuContextMenuItem(label, string.Empty, enabled, false, clickAction));
            return this;
        }

        /// <summary>
        /// Adds an item to the current level of the context menu with a label and click action
        /// </summary>
        /// <param name="label">The label text of the item</param>
        /// <param name="clickAction">The action to perform when the item is clicked</param>
        public FuContextMenuBuilder AddItem(string label, Action clickAction)
        {
            // Adds a context menu item with label and click action.
            _currentLevel.Add(new FuContextMenuItem(label, string.Empty, null, false, clickAction));
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
            _currentLevel.Add(new FuContextMenuItem(null, null, null, true, null));
            return this;
        }

        /// <summary>
        /// Adds a clickable image item to the current level of the context menu.
        /// The image will be displayed full width, scaled to preserve its aspect ratio,
        /// and will trigger the specified action when clicked.
        /// </summary>
        /// <param name="image">The texture to display in the context menu</param>
        /// <param name="size">The size of the displayed texture</param>
        /// <param name="clickAction">The action to perform when the image is clicked</param>
        public FuContextMenuBuilder AddImage(Texture2D image, FuElementSize size, int border, Action clickAction)
        {
            // Prevent adding null images
            if (image == null)
            {
                return this;
            }

            // Adds a new clickable image item
            FuContextMenuItem item = new FuContextMenuItem(image, size, border, clickAction)
            {
                ClickAction = clickAction,
                Enabled = null, // clickable images are considered always enabled by default
                Type = FuContextMenuItemType.Image
            };

            _currentLevel.Add(item);
            return this;
        }

        /// <summary>
        /// BeginChild method starts a new child context menu with the specified label and sets it enabled by default.
        /// </summary>
        /// <param name="label">Label of the parent item</param>
        public FuContextMenuBuilder BeginChild(string label)
        {
            return BeginChild(label, null);
        }

        /// <summary>
        /// BeginChild method starts a new child context menu with the specified label and enabled status.
        /// </summary>
        /// <param name="label">Label of the parent item</param>
        /// <param name="enabled">Whatever the item is enabled</param>
        public FuContextMenuBuilder BeginChild(string label, Func<bool> enabled)
        {
            // Starts a new child context menu with the specified label and enabled status.
            var item = new FuContextMenuItem(label, string.Empty, enabled, false, null, new List<FuContextMenuItem>());
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