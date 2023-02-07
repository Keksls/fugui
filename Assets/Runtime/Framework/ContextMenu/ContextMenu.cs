using Fu.Framework;
using ImGuiNET;
using System.Collections.Generic;
using UnityEngine;

namespace Fu
{
    public static partial class Fugui
    {
        public static bool IsContextMenuOpen { get; private set; } = false;
        private const string CONTEXT_MENU_NAME = "ContextMenuPopup";
        private static List<ContextMenuItem>[] _contextMenuItemsStack = new List<ContextMenuItem>[10];
        private static int _currentContextMenuStackIndex = 0;
        private static List<ContextMenuItem> _currentContextMenuItems = null;
        private static int _openThisFrameLevel = -1;

        public static void ResetContextMenu(bool close = true)
        {
            if (close)
            {
                CloseContextMenu();
            }
            _currentContextMenuStackIndex = 0;
            _currentContextMenuItems = null;

            for (int i = 0; i < _contextMenuItemsStack.Length; i++)
            {
                _contextMenuItemsStack[i] = null;
            }
        }

        public static void PushContextMenuItems(List<ContextMenuItem> items)
        {
            _contextMenuItemsStack[_currentContextMenuStackIndex] = items;
            _currentContextMenuStackIndex++;
            if(_currentContextMenuStackIndex >= _contextMenuItemsStack.Length)
            {
                _currentContextMenuStackIndex = _contextMenuItemsStack.Length - 1;
            }
        }

        public static void TryOpenContextMenuOnItemClick()
        {
            // open popup if needed
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                if (_openThisFrameLevel < _currentContextMenuStackIndex)
                {
                    _openThisFrameLevel = _currentContextMenuStackIndex;
                    _currentContextMenuItems = mergeContextMenuItemsStack(_contextMenuItemsStack);
                }
            }
        }

        public static void TryOpenContextMenuOnRectClick(Vector2 min, Vector2 max)
        {
            // open popup if needed
            if (ImGui.IsMouseHoveringRect(min, max) && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                if (_openThisFrameLevel < _currentContextMenuStackIndex)
                {
                    _openThisFrameLevel = _currentContextMenuStackIndex;
                    _currentContextMenuItems = mergeContextMenuItemsStack(_contextMenuItemsStack);
                }
            }
        }

        public static void TryOpenContextMenuOnWindowClick()
        {
            // open popup if needed
            if (ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows | ImGuiHoveredFlags.AllowWhenBlockedByActiveItem | ImGuiHoveredFlags.AllowWhenBlockedByPopup) && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                if (_openThisFrameLevel < _currentContextMenuStackIndex)
                {
                    _openThisFrameLevel = _currentContextMenuStackIndex;
                    _currentContextMenuItems = mergeContextMenuItemsStack(_contextMenuItemsStack);
                }
            }
        }

        public static void PopContextMenuItems()
        {
            // pop the current stack
            _currentContextMenuStackIndex--;
            if (_currentContextMenuStackIndex < _contextMenuItemsStack.Length && _currentContextMenuStackIndex >= 0)
            {
                _contextMenuItemsStack[_currentContextMenuStackIndex] = null;
            }
        }

        public static void CloseContextMenu()
        {
            if (IsContextMenuOpen)
            {
                ImGui.CloseCurrentPopup();
            }
        }

        public static void RenderContextMenu()
        {
            // Open the context menu if needed
            if (_openThisFrameLevel > -1)
            {
                ImGui.OpenPopup(CONTEXT_MENU_NAME);
                _openThisFrameLevel = -1;
            }

            if (ImGui.BeginPopupContextWindow(CONTEXT_MENU_NAME))
            {
                IsContextMenuOpen = true;
                drawContextMenuItems(_currentContextMenuItems);
                ImGuiNative.igEndPopup();
            }
            else
            {
                IsContextMenuOpen = false;
            }
        }

        private static void drawContextMenuItems(List<ContextMenuItem> items)
        {
            foreach (ContextMenuItem menuItem in items)
            {
                if (menuItem.IsSeparator)
                {
                    ImGuiNative.igSeparator();
                }
                else if (menuItem.Children.Count > 0)
                {
                    if (ImGui.BeginMenu(menuItem.Label, menuItem.Enabled))
                    {
                        drawContextMenuItems(menuItem.Children);
                        ImGuiNative.igEndMenu();
                    }
                }
                else if (ImGui.MenuItem(menuItem.Label, menuItem.Shortcut, false, menuItem.Enabled))
                {
                    menuItem.ClickAction?.Invoke();
                    CloseContextMenu();
                }
            }
        }

        private static List<ContextMenuItem> mergeContextMenuItemsStack(List<ContextMenuItem>[] itemsStack)
        {
            List<ContextMenuItem> mergedItems = new List<ContextMenuItem>();
            for (int i = 0; i < itemsStack.Length; i++)
            {
                var levelItems = itemsStack[i];
                if (levelItems == null) continue;
                int parentIndex = mergedItems.Count - 1;

                for (int j = 0; j < levelItems.Count; j++)
                {
                    mergedItems.Add(levelItems[j]);
                    if (levelItems[j].Children.Count > 0)
                    {
                        var childrenItems = mergeContextMenuItemsStack(new List<ContextMenuItem>[] { levelItems[j].Children });
                        levelItems[j].Children = childrenItems;
                        parentIndex = mergedItems.Count - 1;
                    }
                    else if (parentIndex >= 0 && mergedItems[parentIndex].IsSeparator)
                    {
                        mergedItems.RemoveAt(parentIndex);
                        parentIndex--;
                    }
                }
            }
            return mergedItems;
        }
    }
}