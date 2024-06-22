using System;
using System.Collections.Generic;

namespace Fu.Framework
{
    /// <summary>
    /// Class that will store menu items data
    /// </summary>
    internal class MainMenuItem
    {
        /// <summary>
        /// The name of the menu item.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The name of the menu item.
        /// </summary>
        public Func<string> NameFunc { get; set; }
        /// <summary>
        /// The optional shortcut key for the menu item.
        /// </summary>
        public string Shortcut { get; private set; }
        /// <summary>
        /// A flag indicating whether the menu item is enabled or disabled.
        /// </summary>
        public bool Enabled { get; internal set; }
        /// <summary>
        /// A flag indicating whether the menu item is selected or not.
        /// </summary>
        public bool Selected { get; private set; }
        /// <summary>
        /// A flag indicating whether the menu item is a separator or not.
        /// </summary>
        public bool Separator { get; private set; }
        /// <summary>
        /// The callback action to be executed when the menu item is selected.
        /// </summary>
        public Action Callback { get; private set; }
        /// <summary>
        /// The parent menu item, if any.
        /// </summary>
        public MainMenuItem Parent { get; private set; }
        /// <summary>
        /// The list of child menu items, if any.
        /// </summary>
        public List<MainMenuItem> Children { get; }
        /// <summary>
        /// The callback action to be executed before drawing the item.
        /// </summary>
        public Action PreDrawCallback { get; private set; }
        /// <summary>
        /// The callback action to be executed after drawing the item.
        /// </summary>
        public Action PostDrawCallback { get; private set; }

        /// <summary>
        /// Creates a new menu item with the provided name, shortcut key, enabled/disabled status,
        /// selected/unselected status, callback action, and parent menu item. If a parent menu item is
        /// specified, the new menu item is added to the parent's list of children.
        /// </summary>
        /// <param name="name">The name of the menu item.</param>
        /// <param name="shortcut">The optional shortcut key for the menu item.</param>
        /// <param name="enabled">The optional enabled/disabled status of the menu item. Defaults to true.</param>
        /// <param name="selected">The optional selected/unselected status of the menu item. Defaults to false.</param>
        /// <param name="callback">The callback action to be executed when the menu item is selected.</param>
        /// <param name="parent">The optional parent menu item.</param>
        /// <param name="funcName">Function that compute the name (use it for dynamic name, keep null to use name as string var)</param>
        /// <param name="predrawCallback">optional callback action to be executed before drawing the item.</param>
        /// <param name="postdrawCallback">optional callback action to be executed after drawing the item.</param>
        public MainMenuItem(string name, string shortcut, bool enabled, bool selected, Action callback, MainMenuItem parent, Func<string> funcName, Action predrawCallback, Action postdrawCallback)
        {
            Name = name;
            Shortcut = shortcut;
            Enabled = enabled;
            Selected = selected;
            Callback = callback;
            Parent = parent;
            Children = new List<MainMenuItem>();
            if (parent != null)
            {
                parent.Children.Add(this);
            }
            NameFunc = funcName;
            PreDrawCallback = predrawCallback;
            PostDrawCallback = postdrawCallback;
        }

        /// <summary>
        /// Creates a new separator menu item with the provided parent menu item. If a parent menu item is
        /// specified, the new menu item is added to the parent's list of children.
        /// </summary>
        /// <param name="parent">The optional parent menu item.</param>
        public MainMenuItem(MainMenuItem parent)
        {
            Separator = true;
            Parent = parent;
            if (parent != null)
            {
                parent.Children.Add(this);
            }
        }
    }
}