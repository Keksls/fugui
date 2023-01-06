using ImGuiNET;
using System;
using System.Collections.Generic;

// NOT TESTED : GPT GENERATED
// Define a struct to store the data for a single menu item
public struct MenuItemData
{
    public string Label;
    public string Shortcut;
    public bool Enabled;
    public bool Selected;
    public bool IsSeparator;
    public Action ClickAction;
    public List<MenuItemData> Children;

    public MenuItemData(string label, string shortcut, bool enabled, bool selected, bool isSeparator, Action clickAction, List<MenuItemData> children = null)
    {
        Label = label;
        Shortcut = shortcut;
        Enabled = enabled;
        Selected = selected;
        IsSeparator = isSeparator;
        ClickAction = clickAction;
        Children = children ?? new List<MenuItemData>();
    }
}

public static class ImGuiContextMenuAuto
{
    private static Dictionary<string, List<MenuItemData>> _menuItemData = new Dictionary<string, List<MenuItemData>>();

    public static void AddMenuItem(string id, string label, string shortcut, bool isSeparator = false, bool selected = false, bool enabled = true, List<MenuItemData> children = null, Action clickAction = null)
    {
        if (!_menuItemData.ContainsKey(id))
        {
            _menuItemData[id] = new List<MenuItemData>();
        }

        _menuItemData[id].Add(new MenuItemData(label, shortcut, isSeparator, selected, enabled, clickAction, children));
    }

    public static void ClearMenuItems(string id)
    {
        if (_menuItemData.ContainsKey(id))
        {
            _menuItemData[id].Clear();
        }
    }

    public static bool IsClicked(string id)
    {
        return ImGui.IsItemClicked(ImGuiMouseButton.Right);
    }

    public static void DrawMenu(string id)
    {
        if (IsClicked(id))
        {
            ImGui.OpenPopup(id);
        }

        if (ImGui.BeginPopupContextItem(id))
        {
            DrawMenuItems(_menuItemData[id]);
            ImGui.EndPopup();
        }
    }

    public static void DrawMenuItems(List<MenuItemData> menuItems)
    {
        foreach (var menuItem in menuItems)
        {
            if (menuItem.IsSeparator)
            {
                ImGui.Separator();
            }
            else if (menuItem.Children.Count > 0)
            {
                if (ImGui.BeginMenu(menuItem.Label, menuItem.Enabled))
                {
                    DrawMenuItems(menuItem.Children);
                    ImGui.EndMenu();
                }
            }
            else if (ImGui.MenuItem(menuItem.Label, menuItem.Shortcut, menuItem.Selected, menuItem.Enabled))
            {
                menuItem.ClickAction?.Invoke();
            }
        }
    }
}