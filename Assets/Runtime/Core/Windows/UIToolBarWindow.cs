using Fugui.Framework;
using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fugui.Core
{
    public class ShortcutAction
    {
        public string Icon { get; set; }
        public string ToolTip { get; set; }
        public Action UI { get; set; }

        public ShortcutAction(string icon, string toolTip, Action ui)
        {
            Icon = icon;
            ToolTip = toolTip;
            UI = ui;
        }
    }

    public class Shortcut
    {
        public string Icon { get; set; }
        public string ToolTip { get; set; }
        public Action UI { get; set; }
        internal List<ShortcutAction> Actions { get; private set; }

        public Shortcut(string icon, string toolTip, Action uI)
        {
            Icon = icon;
            ToolTip = toolTip;
            UI = uI;
            Actions = new List<ShortcutAction>();
        }

        public Shortcut AddAction(string icon, string toolTip, Action ui)
        {
            Actions.Add(new ShortcutAction(icon, toolTip, ui));
            return this;
        }
    }

    public class UIToolBarWindow : UIWindow
    {
        private Dictionary<string, Shortcut> _shortcuts;
        private readonly Vector2 _buttonSize = new Vector2(32, 32);

        public UIToolBarWindow(UIWindowDefinition windowDefinition) : base(windowDefinition)
        {
            _shortcuts = new Dictionary<string, Shortcut>();
            UI = (window) =>
            {
                drawBar();
            };
            OnResized += UIShortcutbar_OnResized;
        }

        private void UIShortcutbar_OnResized(UIWindow obj)
        {
            // if window is docked, let's set docked window data
            Vector2Int size = Size;
            Vector2 windowPadding = ImGui.GetStyle().WindowPadding;
            Vector2 framePadding = ImGui.GetStyle().FramePadding;

            // determinate whatever the bar need to be horizontal or vertical
            bool horizontal = size.x > size.y;
            // if both x and y are > to 1/3 container size
            if (size.x > Container.Size.x * 0.3f && size.y > Container.Size.y * 0.3f)
            {
                // horizontality must be computed by size over container size
                float hRatio = (float)Size.x / (float)Container.Size.x;
                float vRatio = (float)Size.y / (float)Container.Size.y;
                horizontal = hRatio > vRatio;
            }
            // horizontal
            if (horizontal)
            {
                int nbCols = (int)((WorkingAreaSize.x - (windowPadding.x * 2f)) / (_buttonSize.x + framePadding.x));
                if (nbCols <= 0)
                    nbCols = 1;
                int nbRows = Mathf.CeilToInt((float)_shortcuts.Count / (float)nbCols);
                if (nbRows <= 0)
                    nbRows = 1;

                size.y = (int)(nbRows * _buttonSize.y + Math.Max(0, nbRows - 2) * framePadding.y + windowPadding.y * 2f);
                size.y += 16;
            }
            // vertical
            else
            {
                int nbRows = (int)((WorkingAreaSize.y - (windowPadding.y * 2f)) / (_buttonSize.y + framePadding.y));
                if (nbRows <= 0)
                    nbRows = 1;
                int nbCols = Mathf.CeilToInt((float)_shortcuts.Count / (float)nbRows);
                if (nbCols <= 0)
                    nbCols = 1;

                size.x = (int)(nbCols * _buttonSize.x + Math.Max(0, nbCols - 2) * framePadding.x + windowPadding.x * 2f);
            }
            if (!size.Equals(Size))
            {
                //_ignoreResizeForThisFrame = true;
                if (IsDocked)
                {
                    ImGuiDocking.DockBuilderSetNodeSize(CurrentDockID, size);
                    ImGuiDocking.DockBuilderFinish(FuGui.MainContainer.Dockspace_id);
                }
                else
                {
                    Size = size;
                }
            }
        }

        public bool AddShortcut(string icon, string toolTip, Action ui)
        {
            return AddShortcut(new Shortcut(icon, toolTip, ui));
        }

        /// <summary>
        /// Add a shortcut to the shortcut bar
        /// </summary>
        /// <param name="shortcut">shortcut to add</param>
        /// <returns>true if added</returns>
        public bool AddShortcut(Shortcut shortcut)
        {
            if (_shortcuts.ContainsKey(shortcut.Icon))
            {
                return false;
            }
            _shortcuts[shortcut.ToolTip] = shortcut;
            return true;
        }

        /// <summary>
        /// Method to remove a shortcut from the toolbar
        /// </summary>
        /// <param name="id">id (tooltip) of the shortcut to remove</param>
        /// <returns>true if removed</returns>
        public bool RemoveShortcut(string id)
        {
            // Remove the shortcut from the dictionary
            return _shortcuts.Remove(id);
        }

        /// <summary>
        /// Draw the shortcut bar
        /// </summary>
        private void drawBar()
        {
            // process nb cols
            Vector2 windowPadding = ImGui.GetStyle().WindowPadding;
            Vector2 framePadding = ImGui.GetStyle().FramePadding;
            int nbCols = (int)((WorkingAreaSize.x - (windowPadding.x * 2f)) / (_buttonSize.x + framePadding.x));

            // Iterate through the dictionary of shortcuts
            int btnIndex = 0;
            foreach (KeyValuePair<string, Shortcut> shortcut in _shortcuts)
            {
                // Begin the shortcut button
                if (ImGui.Button(shortcut.Key, _buttonSize))
                {
                    // Call the UI callback for the shortcut when the button is clicked
                    shortcut.Value.UI?.Invoke();
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(shortcut.Key);
                }

                // Draw an icon if the shortcut has at least one action
                if (shortcut.Value.Actions.Count > 0)
                {
                    // Begin the context menu for the actions icon
                    if (ImGui.BeginPopupContextItem(shortcut.Key + "contextMenu"))
                    {
                        // Iterate through the list of actions and   // draw a menu item for each action
                        foreach (ShortcutAction action in shortcut.Value.Actions)
                        {
                            if (ImGui.MenuItem(action.ToolTip))
                            {
                                // Call the UI callback for the action when the menu item is clicked
                                action.UI?.Invoke();
                            }
                        }
                        ImGui.EndPopup();
                    }
                }
                if (btnIndex >= nbCols)
                {
                    btnIndex = 0;
                }
                else
                {
                    ImGui.SameLine();
                }
                btnIndex++;
            }
        }
    }
}