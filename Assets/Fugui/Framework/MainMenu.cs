using Fugui.Core;
using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fugui.Framework
{
    public static class MainMenu
    {
        private static readonly Dictionary<string, MenuItem> _menuItems = new Dictionary<string, MenuItem>();

        /// <summary>
        /// Registers a new menu item with the provided name, callback action, and optional parent menu item,
        /// shortcut key, enabled/disabled status, and selected/unselected status. If a menu item with the
        /// same name has already been registered, or if a parent menu item was specified but not found, an
        /// exception is thrown. The new menu item is then added to a collection of registered menu items.
        /// </summary>
        /// <param name="name">The name of the menu item to be registered.</param>
        /// <param name="callback">The callback action to be executed when the menu item is selected.</param>
        /// <param name="parentName">The optional name of the parent menu item, if any.</param>
        /// <param name="shortcut">The optional shortcut key for the menu item.</param>
        /// <param name="enabled">The optional enabled/disabled status of the menu item. Defaults to true.</param>
        /// <param name="selected">The optional selected/unselected status of the menu item. Defaults to false.</param>
        public static void RegisterItem(string name, Action callback, string parentName = null, string shortcut = null, bool enabled = true, bool selected = false)
        {
            // Check if a menu item with the same name has already been registered
            if (_menuItems.ContainsKey(name))
            {
                throw new Exception($"Menu item with name '{name}' is already registered");
            }

            // Try to get the parent menu item, if one was specified
            MenuItem parent = null;
            if (parentName != null)
            {
                if (!_menuItems.TryGetValue(parentName, out parent))
                {
                    throw new Exception($"Parent menu item with name '{parentName}' was not found");
                }
            }

            // Create a new MenuItem object with the provided parameters
            var menuItem = new MenuItem(name, shortcut, enabled, selected, callback, parent);

            // Add the menu item to the collection of registered menu items
            _menuItems.Add(name, menuItem);
        }

        /// <summary>
        /// Registers a new separator menu item under the specified parent menu item. If the parent menu item
        /// is not found, an exception is thrown.
        /// </summary>
        /// <param name="parentName">The name of the parent menu item.</param>
        public static void RegisterSeparator(string parentName)
        {
            // Try to get the parent menu item
            MenuItem parent = null;
            if (parentName != null)
            {
                if (!_menuItems.TryGetValue(parentName, out parent))
                {
                    throw new Exception($"Parent menu item with name '{parentName}' was not found");
                }
            }

            // Create a new separator menu item under the parent menu item
            new MenuItem(parent);
        }

        /// <summary>
        /// Draws the main menu bar and all top-level menu items.
        /// </summary>
        public static void Draw()
        {
            // Return early if no menu items are registered
            if (_menuItems.Count == 0)
            {
                return;
            }

            // Set various style options for the main menu bar and its items
            FuGui.Push(ImGuiStyleVar.ItemInnerSpacing, new Vector2(0f, 0f));
            FuGui.Push(ImGuiStyleVar.FramePadding, new Vector2(8f, 8f));
            FuGui.Push(ImGuiStyleVar.WindowPadding, new Vector2(8f, 8f));
            FuGui.Push(ImGuiCol.Header, ThemeManager.GetColor(ImGuiCol.HeaderHovered));

            // Begin the main menu bar
            if (ImGui.BeginMainMenuBar())
            {
                // Draw all top-level menu items
                foreach (var item in _menuItems.Values)
                {
                    if (item.Parent == null)
                    {
                        DrawItem(item);
                    }
                }

                // End the main menu bar
                ImGui.EndMainMenuBar();
            }

            // Pop the set style options
            FuGui.PopColor();
            FuGui.PopStyle(3);
        }

        /// <summary>
        /// Recursively draws a menu item and all of its children. If the menu item has children, it is
        /// drawn as a submenu. If the menu item is a separator, a separator line is drawn. Otherwise, the
        /// menu item is drawn as a regular menu item that can be clicked and has an optional shortcut key.
        /// If the menu item is clicked and has a callback action registered, the action is executed.
        /// </summary>
        /// <param name="item">The menu item to be drawn.</param>
        private static void DrawItem(MenuItem item)
        {
            if (item.Children != null && item.Children.Count > 0)
            {
                // Begin a submenu if the menu item has children
                if (ImGui.BeginMenu(item.Parent == null ? "  " + item.Name + "   " : item.Name, item.Enabled))
                {
                    // Draw all children of the menu item
                    foreach (var child in item.Children)
                    {
                        DrawItem(child);
                    }
                    ImGui.EndMenu();
                }
            }
            else
            {
                if (item.Separator)
                {
                    // Draw a separator line if the menu item is a separator
                    ImGui.Separator();
                }
                else if (ImGui.MenuItem(item.Parent == null ? "  " + item.Name + "   " : item.Name, item.Shortcut, item.Selected, item.Enabled))
                {
                    // Draw a regular menu item and execute its callback action if clicked
                    item.Callback?.Invoke();
                }
            }
        }

        private class MenuItem
        {
            /// <summary>
            /// The name of the menu item.
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// The optional shortcut key for the menu item.
            /// </summary>
            public string Shortcut { get; private set; }
            /// <summary>
            /// A flag indicating whether the menu item is enabled or disabled.
            /// </summary>
            public bool Enabled { get; private set; }
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
            public MenuItem Parent { get; private set; }
            /// <summary>
            /// The list of child menu items, if any.
            /// </summary>
            public List<MenuItem> Children { get; }

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
            public MenuItem(string name, string shortcut, bool enabled, bool selected, Action callback, MenuItem parent)
            {
                Name = name;
                Shortcut = shortcut;
                Enabled = enabled;
                Selected = selected;
                Callback = callback;
                Parent = parent;
                Children = new List<MenuItem>();
                if (parent != null)
                {
                    parent.Children.Add(this);
                }
            }

            /// <summary>
            /// Creates a new separator menu item with the provided parent menu item. If a parent menu item is
            /// specified, the new menu item is added to the parent's list of children.
            /// </summary>
            /// <param name="parent">The optional parent menu item.</param>
            public MenuItem(MenuItem parent)
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
}