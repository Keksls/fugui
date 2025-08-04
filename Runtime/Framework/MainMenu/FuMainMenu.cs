using Fu.Core;
using Fu.Framework;
using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu
{
    public static partial class Fugui
    {
        /// <summary>
        /// Whatever the main menu is currently disabled (will disable each menu items)
        /// </summary>
        public static bool IsMainMenuDisabled { get; private set; }
        /// <summary>
        /// Event fired after main menu draw, The rect given is the available rect after menu items
        /// </summary>
        public static event Action<Rect> OnMainMenuDraw;
        /// <summary>
        /// menu items of the main menu
        /// </summary>
        private static readonly Dictionary<string, MainMenuItem> _mainMenuItems = new Dictionary<string, MainMenuItem>();

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
        /// <param name="funcName">Function that compute the name (use it for dynamic name, keep null to use name as string var)</param>
        /// <param name="predrawCallback">optional callback action to be executed before drawing the item.</param>
        /// <param name="postdrawCallback">optional callback action to be executed after drawing the item.</param>
        public static void RegisterMainMenuItem(string name, Action callback, string parentName = null, string shortcut = null, bool enabled = true, bool selected = false, Func<string> funcName = null, Action predrawCallback = null, Action postdrawCallback = null)
        {
            // check whatever item name is null
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError($"Menu item name is null");
                return;
            }

            // Check if a menu item with the same name has already been registered
            if (_mainMenuItems.ContainsKey(name))
            {
                Debug.LogError($"Menu item with name '{name}' is already registered");
                return;
            }

            // Try to get the parent menu item, if one was specified
            MainMenuItem parent = null;
            if (parentName != null)
            {
                // registter parent if not already registered
                if (!_mainMenuItems.ContainsKey(parentName))
                {
                    RegisterMainMenuItem(parentName, null);
                }
                // parent foes not exists
                if (!_mainMenuItems.TryGetValue(parentName, out parent))
                {
                    Debug.LogError($"Parent menu item with name '{parentName}' was not found");
                    return;
                }
            }

            // Create a new MenuItem object with the provided parameters
            var menuItem = new MainMenuItem(name, shortcut, enabled, selected, callback, parent, funcName, predrawCallback, postdrawCallback);

            // Add the menu item to the collection of registered menu items
            _mainMenuItems.Add(name, menuItem);
        }

        /// <summary>
        /// Unregistered an existing menu. Il menu has children, children will be unregistered too
        /// </summary>
        /// <param name="name">Menu to unregistered</param>
        public static void UnregisterMainMenuItem(string name)
        {
            // Check if a menu item with the same name has already been registered
            if (!string.IsNullOrEmpty(name) && _mainMenuItems.ContainsKey(name))
            {
                MainMenuItem menuToRemove = _mainMenuItems[name];

                //If menu item to remove has children
                if (menuToRemove.Children != null)
                {
                    for (int i = 0; i < menuToRemove.Children.Count; i++)
                    {
                        UnregisterMainMenuItem(menuToRemove.Children[i].Name);
                    }
                }

                _mainMenuItems.Remove(name);
            }
        }

        /// <summary>
        /// Enable a main menu item
        /// </summary>
        /// <param name="name">Menu item to enable</param>
        public static void EnableMainMenuItem(string name)
        {
            // Check if a menu item with the same name has already been registered
            if (!string.IsNullOrEmpty(name) && _mainMenuItems.ContainsKey(name))
            {
                _mainMenuItems[name].Enabled = true;
            }
        }

        /// <summary>
        /// Disable a main menu item
        /// </summary>
        /// <param name="name">Menu item to disable</param>
        public static void DisableMainMenuItem(string name)
        {
            // Check if a menu item with the same name has already been registered
            if (_mainMenuItems.ContainsKey(name))
            {
                _mainMenuItems[name].Enabled = false;
            }
        }

        /// <summary>
        /// Check if a name is already registered
        /// </summary>
        /// <param name="name">Name to check</param>
        /// <returns>TRUE if registered, FALSE either</returns>
        public static bool IsMainMenuRegisteredItem(string name)
        {
            return _mainMenuItems.ContainsKey(name);
        }

        /// <summary>
        /// Registers a new separator menu item under the specified parent menu item. If the parent menu item
        /// is not found, an exception is thrown.
        /// </summary>
        /// <param name="parentName">The name of the parent menu item.</param>
        public static void RegisterMainMenuSeparator(string parentName)
        {
            // Try to get the parent menu item
            MainMenuItem parent = null;
            if (parentName != null)
            {
                // registter parent if not already registered
                if (!_mainMenuItems.ContainsKey(parentName))
                {
                    RegisterMainMenuItem(parentName, null);
                }
                if (!_mainMenuItems.TryGetValue(parentName, out parent))
                {
                    throw new Exception($"Parent menu item with name '{parentName}' was not found");
                }
            }

            // Create a new separator menu item under the parent menu item
            new MainMenuItem(parent);
        }

        /// <summary>
        /// Force disabling all items in main menu
        /// </summary>
        public static void DisableMainMenu()
        {
            IsMainMenuDisabled = true;
        }

        /// <summary>
        /// Enable main menu items
        /// </summary>
        public static void EnableMainMenu()
        {
            IsMainMenuDisabled = false;
        }

        /// <summary>
        /// Draws the main menu bar and all top-level menu items.
        /// </summary>
        public static void RenderMainMenu()
        {
            // Return early if no menu items are registered
            if (_mainMenuItems.Count == 0)
            {
                return;
            }

            // Set various style options for the main menu bar and its items
            Push(ImGuiStyleVar.ItemInnerSpacing, new Vector2(0f, 0f));
            Push(ImGuiStyleVar.FramePadding, new Vector2(4f, 4f));
            Push(ImGuiStyleVar.ItemSpacing, new Vector2(4f, 4f));
            Push(ImGuiStyleVar.WindowPadding, new Vector2(8f, 4f));
            Push(ImGuiCol.Header, FuThemeManager.GetColor(FuColors.HeaderHovered));
            Push(ImGuiCol.Text, FuThemeManager.GetColor(FuColors.MainMenuText));
            Push(ImGuiCol.PopupBg, FuThemeManager.GetColor(FuColors.MenuBarBg));
            Push(ImGuiCol.Separator, FuThemeManager.GetColor(FuColors.MainMenuText) * 0.33f);
            // Begin the main menu bar
            if (ImGui.BeginMainMenuBar())
            {
                Vector2 startPos = ImGui.GetCursorScreenPos();
                // Draw all top-level menu items
                foreach (var item in _mainMenuItems.Values)
                {
                    if (item.Parent == null)
                    {
                        drawMainMenuItem(item);
                    }
                }

                OnMainMenuDraw?.Invoke(new Rect(ImGui.GetItemRectMax() - startPos, ImGui.GetContentRegionAvail()));
                // End the main menu bar
                ImGui.EndMainMenuBar();
            }

            // Pop the set style options
            PopColor(4);
            PopStyle(4);
        }

        /// <summary>
        /// Recursively draws a menu item and all of its children. If the menu item has children, it is
        /// drawn as a submenu. If the menu item is a separator, a separator line is drawn. Otherwise, the
        /// menu item is drawn as a regular menu item that can be clicked and has an optional shortcut key.
        /// If the menu item is clicked and has a callback action registered, the action is executed.
        /// </summary>
        /// <param name="item">The menu item to be drawn.</param>
        private static void drawMainMenuItem(MainMenuItem item)
        {
            if (item.Parent != null)
            {
                Push(ImGuiStyleVar.ItemSpacing, new Vector2(8f, 8f));
            }
            string itemText = item.NameFunc?.Invoke() ?? item.Name;
            Push(ImGuiStyleVar.WindowPadding, new Vector2(12f, 12f));
            if (item.Children != null && item.Children.Count > 0)
            {
                item.PreDrawCallback?.Invoke();
                // draw secondary duotone glyph if needed
                DrawDuotoneSecondaryGlyph(item.Parent == null ? "  " + itemText + "   " : itemText, ImGui.GetCursorScreenPos(), ImGui.GetWindowDrawList(), IsMainMenuDisabled || !item.Enabled);

                // Begin a submenu if the menu item has children
                if (ImGui.BeginMenu(item.Parent == null ? "  " + itemText + "   " : itemText, item.Enabled && !IsMainMenuDisabled))
                {
                    // Draw all children of the menu item
                    foreach (var child in item.Children)
                    {
                        drawMainMenuItem(child);
                    }
                    ImGui.EndMenu();
                }
                else
                {
                    item.PostDrawCallback?.Invoke();
                }
            }
            else
            {
                if (item.Separator)
                {
                    // Draw a separator line if the menu item is a separator
                    ImGui.Separator();
                }
                else
                {
                    item.PreDrawCallback?.Invoke();
                    // draw secondary duotone glyph if needed
                    DrawDuotoneSecondaryGlyph(item.Parent == null ? "  " + itemText + "   " : itemText, ImGui.GetCursorScreenPos(), ImGui.GetWindowDrawList(), IsMainMenuDisabled || !item.Enabled);

                    if (ImGui.MenuItem(item.Parent == null ? "  " + itemText + "   " : itemText, item.Shortcut, item.Selected, item.Enabled && !IsMainMenuDisabled))
                    {
                        // Draw a regular menu item and execute its callback action if clicked
                        item.Callback?.Invoke();
                    }
                    item.PostDrawCallback?.Invoke();
                }
            }
            if (item.Parent != null)
            {
                PopStyle();
            }
            PopStyle();
        }
    }
}