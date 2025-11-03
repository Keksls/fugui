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
        /// Whatever the context menu is currently disabled (will disable each menu items)
        /// </summary>
        public static bool IsContextMenuDisabled { get; private set; }
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
        /// <param name="label"> The label of the context menu item</param>
        /// <param name="clickAction"> The action to perform when the item is clicked</param>
        /// <param name="shortcut"> The shortcut text for the context menu item</param>
        /// <param name="image"> Optional image to display next to the label</param>
        /// <param name="imageSize"> Optional image size</param>
        /// <param name="enabled"> Whatever the item is enabled</param>
        public static void PushContextMenuItem(string label, Action clickAction, string shortcut = "", Texture2D image = null, FuElementSize imageSize = default, Func<bool> enabled = null)
        {
            // Add a new level to the context menu stack
            _contextMenuItemsStack[_currentContextMenuStackIndex] = new List<FuContextMenuItem>()
            {
                new FuContextMenuItem(label, shortcut, enabled, clickAction, image, imageSize, null)
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
        /// <param name="nbPop">Number of levels to pop</param>
        public static void PopContextMenuItems(int nbPop = 1)
        {
            for (int i = 0; i < nbPop; i++)
            {
                // Remove a level from the context menu stack
                _currentContextMenuStackIndex--;
                // Check if the current context menu stack index is within the bounds of the stack
                if (_currentContextMenuStackIndex < _contextMenuItemsStack.Length && _currentContextMenuStackIndex >= 0)
                {
                    // If so, set the current level of the stack to null
                    _contextMenuItemsStack[_currentContextMenuStackIndex] = null;
                }
                // Ensure the current context menu stack index is not negative
                if (_currentContextMenuStackIndex < 0)
                {
                    _currentContextMenuStackIndex = 0;
                }
            }
        }
        #endregion

        #region Try Open
        /// <summary>
        /// Open the context menu if the last item drawed has just been right clicked
        /// </summary>
        /// <param name="mouseButton">Mouse button to use to open the context menu</param>
        public static void TryOpenContextMenuOnItemClick(FuMouseButton mouseButton = FuMouseButton.Right)
        {
            // Whatever the last item drawed has just been clicked
            if (ImGui.IsItemClicked((ImGuiMouseButton)mouseButton))
            {
                // Open the context menu
                TryOpenContextMenuOnWindowClick(mouseButton);
            }
        }

        /// <summary>
        /// Open the context menu if the mouse right click on a rect
        /// </summary>
        /// <param name="min">Left Up corner of the rect</param>
        /// <param name="max">Right Down corner of the rect</param>
        /// <param name="mouseButton">Mouse button to use to open the context menu</param>
        public static bool TryOpenContextMenuOnRectClick(Vector2 min, Vector2 max, FuMouseButton mouseButton = FuMouseButton.Right)
        {
            // check if the mouse is hovering the rect
            if (ImGui.IsMouseHoveringRect(min, max))
                return TryOpenContextMenuOnWindowClick(mouseButton);
            return false;
        }

        /// <summary>
        /// Open the context menu if the current window is right clicked
        /// </summary>
        /// <param name="mouseButton">Mouse button to use to open the context menu</param>
        public static bool TryOpenContextMenuOnWindowClick(FuMouseButton mouseButton = FuMouseButton.Right)
        {
            // whatever the current window is hovered and right clicked
            if (FuWindow.CurrentDrawingWindow != null && FuWindow.CurrentDrawingWindow.IsHoveredContent && FuWindow.CurrentDrawingWindow.Mouse.IsClicked(mouseButton))
            {
                // open the context menu
                TryOpenContextMenu();
                return true;
            }
            return false;
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
            if (ImGui.BeginPopup(CONTEXT_MENU_NAME, ImGuiWindowFlags.NoMove))
            {
                IsContextMenuOpen = true;
                // draw the items
                FuLayout layout = FuWindow.CurrentDrawingWindow?.Layout ?? new FuLayout();
                try
                {
                    drawContextMenuItems(_currentContextMenuItems, layout, 0);
                }
                catch (Exception e)
                {
                    OnUIException?.Invoke(e);
                }
                finally
                {
                    if (FuWindow.CurrentDrawingWindow == null)
                        layout.Dispose();
                }
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
        private static void drawContextMenuItems(List<FuContextMenuItem> items, FuLayout layout, int id)
        {
            // draw each items
            foreach (FuContextMenuItem menuItem in items)
            {
                switch (menuItem.Type)
                {
                    default:
                    //Display Text menu
                    case FuContextMenuItemType.Item:
                        string label = menuItem.Label ?? string.Empty;
                        Vector2 imageSize = menuItem.ImageSize;
                        if(imageSize == Vector2.zero && menuItem.Image != null)
                        {
                            float currentFontSize = Fugui.GetFontSize();
                            imageSize = new Vector2(currentFontSize, currentFontSize); // Default size for image if not specified is current font size
                        }
                        bool hasLabel = !string.IsNullOrEmpty(label);
                        bool hasImage = menuItem.Image != null;

                        bool enabled = (menuItem.Enabled?.Invoke() ?? true) && !IsContextMenuDisabled;
                        if (!enabled)
                            Push(ImGuiCol.Text, Fugui.Themes.GetColor(FuColors.TextDisabled));

                        // Draw secondary duotone glyph if needed
                        DrawDuotoneSecondaryGlyph(label, ImGui.GetCursorScreenPos(), ImGui.GetWindowDrawList(), enabled);

                        // Parent item (submenu)
                        if (menuItem.Children.Count > 0)
                        {
                            if (hasImage && hasLabel)
                            {
                                layout.Image("imgBtnCm" + id, menuItem.Image, imageSize, false, false);
                                ImGui.SameLine();
                                bool open = ImGui.BeginMenu(label, enabled);
                                if (open)
                                {
                                    drawContextMenuItems(menuItem.Children, layout, id + 1);
                                    ImGuiNative.igEndMenu();
                                }
                            }
                            else if (!hasLabel && hasImage)
                            {
                                ImGui.SetCursorPosX((ImGui.GetContentRegionAvail().x - imageSize.x) * 0.5f);
                                bool open = ImGui.BeginMenu("##imgmenu" + id, enabled);
                                layout.Image("imgBtnCm" + id, menuItem.Image, imageSize, false, false);
                                if (open)
                                {
                                    drawContextMenuItems(menuItem.Children, layout, id + 1);
                                    ImGuiNative.igEndMenu();
                                }
                            }
                            else // label only
                            {
                                if (ImGui.BeginMenu(label, enabled))
                                {
                                    drawContextMenuItems(menuItem.Children, layout, id + 1);
                                    ImGuiNative.igEndMenu();
                                }
                            }
                        }
                        // Leaf item
                        else
                        {
                            if (hasImage && hasLabel)
                            {
                                bool clicked = layout.Image("imgBtnCm" + id, menuItem.Image, imageSize, false, true);
                                ImGui.SameLine();
                                clicked |= ImGui.MenuItem(label, menuItem.Shortcut, false, enabled);
                                if (clicked)
                                {
                                    menuItem.ClickAction?.Invoke();
                                    CloseContextMenu();
                                }
                            }
                            else if (!hasLabel && hasImage)
                            {
                                layout.CenterNextItemH(imageSize.x);
                                if (layout.Image("imgBtnCm" + id, menuItem.Image, imageSize, false, true))
                                {
                                    menuItem.ClickAction?.Invoke();
                                    CloseContextMenu();
                                }
                            }
                            else // label only
                            {
                                if (ImGui.MenuItem(label, menuItem.Shortcut, false, enabled))
                                {
                                    menuItem.ClickAction?.Invoke();
                                    CloseContextMenu();
                                }
                            }
                        }

                        if (!enabled)
                            PopColor();

                        break;

                    //Display Separator
                    case FuContextMenuItemType.Separator:
                        ImGuiNative.igSeparator();
                        break;

                    //Display Title
                    case FuContextMenuItemType.Title:
                        if (!string.IsNullOrEmpty(menuItem.Label))
                        {
                            Vector2 region = ImGui.GetContentRegionAvail();
                            Vector2 textSize = Fugui.CalcTextSize(menuItem.Label, FuTextWrapping.None);

                            //Center label title text
                            float offsetX = (region.x - textSize.x) * 0.5f;
                            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Mathf.Max(0f, offsetX));

                            PushFont(FontType.Bold);
                            ImGui.Text(menuItem.Label);
                            PopFont();

                            //Add always a separator under title
                            ImGuiNative.igSeparator();
                        }
                        break;
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
                    }
                }
            }

            // Return the merged list
            return mergedItems;
        }
        #endregion

        #region Public utils
        /// <summary>
        /// Force disabling all items in cotext menu
        /// </summary>
        public static void DisableContextMenu()
        {
            IsContextMenuDisabled = true;
        }

        /// <summary>
        /// Enable context menu items
        /// </summary>
        public static void EnableContextMenu()
        {
            IsContextMenuDisabled = false;
        }

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

        /// <summary>
        /// Clear the context menu stack data. 
        /// </summary>
        public static void ClearContextMenuStack()
        {
            // Reset the current context menu stack index to 0
            _currentContextMenuStackIndex = 0;
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