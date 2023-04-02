using Fu.Framework;
using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu
{
    public static partial class Fugui
    {
        public static bool IsContextMenuOpen { get; private set; } = false;
        private const string CONTEXT_MENU_NAME = "ContextMenuPopup";
        private static List<FuContextMenuItem>[] _contextMenuItemsStack = new List<FuContextMenuItem>[10];
        private static int _currentContextMenuStackIndex = 0;
        private static List<FuContextMenuItem> _currentContextMenuItems = null;
        private static int _openThisFrameLevel = -1;
        private static int _currentOpenContextID = -1;

        #region Push / Pop
        /// <summary>
        /// Push some item to the context menu items stack
        /// If the context menu will be open betwin this call and the next 'Pop' call, these items will be added to the context menu
        /// Must call a Pop after pushing items before the end of the frame
        /// </summary>
        /// <param name="items">Items to push</param>
        public static void PushContextMenuItems(List<FuContextMenuItem> items)
        {
            // Add a new level to the context menu stack
            _contextMenuItemsStack[_currentContextMenuStackIndex] = items;
            // Increment the current context menu stack index
            _currentContextMenuStackIndex++;
            // Check if the current context menu stack index is larger than the size of the stack
            if (_currentContextMenuStackIndex >= _contextMenuItemsStack.Length)
            {
                // If so, set the current context menu stack index to the maximum size of the stack
                _currentContextMenuStackIndex = _contextMenuItemsStack.Length - 1;
            }
        }

        /// <summary>
        /// Push an item to the context menu items stack
        /// If the context menu will be open betwin this call and the next 'Pop' call, the item will be added to the context menu
        /// Must call a Pop after pushing items before the end of the frame
        /// </summary>
        /// <param name="item">Item to push</param>
        public static void PushContextMenuItem(FuContextMenuItem item)
        {
            // Add a new level to the context menu stack
            _contextMenuItemsStack[_currentContextMenuStackIndex] = new List<FuContextMenuItem>() { item };
            // Increment the current context menu stack index
            _currentContextMenuStackIndex++;
            // Check if the current context menu stack index is larger than the size of the stack
            if (_currentContextMenuStackIndex >= _contextMenuItemsStack.Length)
            {
                // If so, set the current context menu stack index to the maximum size of the stack
                _currentContextMenuStackIndex = _contextMenuItemsStack.Length - 1;
            }
        }

        /// <summary>
        /// Push an item to the context menu items stack
        /// If the context menu will be open betwin this call and the next 'Pop' call, the item will be added to the context menu
        /// Must call a Pop after pushing items before the end of the frame
        /// </summary>
        /// <param name="itemLabel">The label for the item</param>
        /// <param name="enabled">Whether the item is enabled</param>
        /// <param name="itemCallback">The action to perform when the item is clicked</param>
        public static void PushContextMenuItem(string itemLabel, Action itemCallback, Func<bool> enabled = null)
        {
            // Add a new level to the context menu stack
            _contextMenuItemsStack[_currentContextMenuStackIndex] = new List<FuContextMenuItem>()
            {
                new FuContextMenuItem(itemLabel, null, enabled, false, itemCallback, null)
            };
            // Increment the current context menu stack index
            _currentContextMenuStackIndex++;
            // Check if the current context menu stack index is larger than the size of the stack
            if (_currentContextMenuStackIndex >= _contextMenuItemsStack.Length)
            {
                // If so, set the current context menu stack index to the maximum size of the stack
                _currentContextMenuStackIndex = _contextMenuItemsStack.Length - 1;
            }
        }

        /// <summary>
        /// Pop last pushed items to the context menu items stack
        /// Must be call after a push before the end of the frame
        /// </summary>
        public static void PopContextMenuItems()
        {
            // Remove a level from the context menu stack
            _currentContextMenuStackIndex--;
            // Check if the current context menu stack index is within the bounds of the stack
            if (_currentContextMenuStackIndex < _contextMenuItemsStack.Length && _currentContextMenuStackIndex >= 0)
            {
                // If so, set the current level of the stack to null
                _contextMenuItemsStack[_currentContextMenuStackIndex] = null;
            }
        }
        #endregion

        #region Try Open
        /// <summary>
        /// Open the context menu if the last item drawed has just been right clicked
        /// </summary>
        public static void TryOpenContextMenuOnItemClick()
        {
            // Whatever the last item drawed has just been clicked
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                // Open the context menu
                TryOpenContextMenu();
            }
        }

        /// <summary>
        /// Open the context menu if the mouse right click on a rect
        /// </summary>
        /// <param name="min">Left Up corner of the rect</param>
        /// <param name="max">Right Down corner of the rect</param>
        public static void TryOpenContextMenuOnRectClick(Vector2 min, Vector2 max)
        {
            // Whatever the mouse is hovering a rect and click on it
            if (ImGui.IsMouseHoveringRect(min, max) && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                // Open the context menu
                TryOpenContextMenu();
            }
        }

        /// <summary>
        /// Open the context menu if the current window is right clicked
        /// </summary>
        public static void TryOpenContextMenuOnWindowClick()
        {
            // whatever the current window is hovered and right clicked
            if (ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows | ImGuiHoveredFlags.AllowWhenBlockedByActiveItem | ImGuiHoveredFlags.AllowWhenBlockedByPopup) && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                // open the context menu
                TryOpenContextMenu();
            }
        }
        #endregion

        #region Open / Close
        /// <summary>
        /// Open the context menu now.
        /// If the context menu if not already open to a higher level, open it. owether, do nothing
        /// </summary>
        public static void TryOpenContextMenu()
        {
            // Code updates the current context menu items if a new level has been opened
            if (_openThisFrameLevel < _currentContextMenuStackIndex)
            {
                // save the context ID to draw the context menu inside the right FuContext
                _currentOpenContextID = CurrentContext.ID;
                // Set the open level this frame to the current context menu stack index
                _openThisFrameLevel = _currentContextMenuStackIndex;
                // Merge the items from the current context menu stack and set the result as the current context menu items
                _currentContextMenuItems = mergeContextMenuItemsStack(_contextMenuItemsStack);
                // cancel openning if there is no items at current level
                if (_currentContextMenuItems.Count == 0)
                {
                    _currentOpenContextID = -1;
                    _openThisFrameLevel = -1;
                }
            }
        }

        /// <summary>
        /// Close the context menu if open
        /// </summary>
        public static void CloseContextMenu()
        {
            // do not close the context menu if it's not open
            if (IsContextMenuOpen)
            {
                _currentOpenContextID = -1;
                ImGui.CloseCurrentPopup();
            }
        }
        #endregion

        #region Rendering
        /// <summary>
        /// Render the Desktop context menu.
        /// Call this into a Container render loop.
        /// </summary>
        public static void RenderContextMenu()
        {
            // ignore rendering id the context menu if not open on the current FuContext
            if (CurrentContext.ID != _currentOpenContextID)
            {
                return;
            }

            // Open the context menu if needed
            if (_openThisFrameLevel > -1)
            {
                ImGui.OpenPopup(CONTEXT_MENU_NAME);
                _openThisFrameLevel = -1;
            }

            Push(ImGuiStyleVar.WindowPadding, new Vector2(8f, 8f));
            Push(ImGuiStyleVar.ItemSpacing, new Vector2(8f, 8f));
            // draw the context menu
            if (ImGui.BeginPopupContextWindow(CONTEXT_MENU_NAME))
            {
                IsContextMenuOpen = true;
                // draw the items
                drawContextMenuItems(_currentContextMenuItems);
                ImGuiNative.igEndPopup();
            }
            else
            {
                IsContextMenuOpen = false;
            }
            PopStyle(2);
        }

        /// <summary>
        /// Draw each items recursively
        /// </summary>
        /// <param name="items">list of items to draw</param>
        private static void drawContextMenuItems(List<FuContextMenuItem> items)
        {
            // draw each items
            foreach (FuContextMenuItem menuItem in items)
            {
                // whatever the item is a separator
                if (menuItem.IsSeparator)
                {
                    ImGuiNative.igSeparator();
                }
                // whatever the item is a parent (contain children)
                else if (menuItem.Children.Count > 0)
                {
                    // draw the parent and bind children if parent is open
                    if (ImGui.BeginMenu(menuItem.Label, menuItem.Enabled?.Invoke() ?? true))
                    {
                        // bind children
                        drawContextMenuItems(menuItem.Children);
                        ImGuiNative.igEndMenu();
                    }
                }
                // whatever the item is a 'leaf' (no child)
                else if (ImGui.MenuItem(menuItem.Label, menuItem.Shortcut, false, menuItem.Enabled?.Invoke() ?? true))
                {
                    // invoke the callback action of the item if clicked and close the context menu
                    menuItem.ClickAction?.Invoke();
                    CloseContextMenu();
                }
            }
        }
        #endregion

        #region Items
        /// <summary>
        /// Get the current displaying items
        /// </summary>
        /// <returns>null if nothing need to be displayed</returns>
        public static List<FuContextMenuItem> GetCurrentItems()
        {
            return _currentContextMenuItems;
        }

        /// <summary>
        /// Merge the context Menu items stack into a single list, taking care about parent-children relationship
        /// </summary>
        /// <param name="itemsStack">the current items stack</param>
        /// <returns>merged list of items</returns>
        private static List<FuContextMenuItem> mergeContextMenuItemsStack(List<FuContextMenuItem>[] itemsStack)
        {
            // Initialize a list to store the merged context menu items
            List<FuContextMenuItem> mergedItems = new List<FuContextMenuItem>();

            // Loop through each list in the stack
            for (int i = 0; i < itemsStack.Length; i++)
            {
                // Get the items at this level
                var levelItems = itemsStack[i];
                // If there are no items, skip to the next level
                if (levelItems == null)
                {
                    continue;
                }

                // Keep track of the last parent item added to the merged list
                int parentIndex = mergedItems.Count - 1;
                // Loop through the items at this level
                for (int j = 0; j < levelItems.Count; j++)
                {
                    // Add the current item to the merged list
                    mergedItems.Add(levelItems[j]);
                    // If the current item has children, recursively merge them
                    if (levelItems[j].Children.Count > 0)
                    {
                        var childrenItems = mergeContextMenuItemsStack(new List<FuContextMenuItem>[] { levelItems[j].Children });
                        levelItems[j].Children = childrenItems;
                        parentIndex = mergedItems.Count - 1;
                    }
                    // If the last item in the merged list is a separator and the current item doesn't have children, remove it
                    else if (parentIndex >= 0 && mergedItems[parentIndex].IsSeparator)
                    {
                        mergedItems.RemoveAt(parentIndex);
                        parentIndex--;
                    }
                }
            }

            // Return the merged list
            return mergedItems;
        }
        #endregion

        #region Public utils
        /// <summary>
        /// Reset the context menu data and clean the stack. 
        /// </summary>
        /// <param name="close">whatever you want to close the context menu</param>
        public static void ResetContextMenu(bool close = true)
        {
            // Close the current context menu
            if (close)
            {
                CloseContextMenu();
            }

            // Reset the current context menu stack index to 0
            _currentContextMenuStackIndex = 0;
            // Reset the current context menu items to null
            _currentContextMenuItems = null;
            // Loop through the context menu items stack
            for (int i = 0; i < _contextMenuItemsStack.Length; i++)
            {
                // Set each level of the stack to null
                _contextMenuItemsStack[i] = null;
            }
        }
        #endregion
    }
}