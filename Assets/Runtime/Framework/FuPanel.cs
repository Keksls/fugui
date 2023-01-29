using ImGuiNET;
using System;
using UnityEngine;

namespace Fu.Framework
{
    public class FuPanel : IDisposable
    {
        /// <summary>
        /// The current layout style for the panel.
        /// </summary>
        private FuStyle _currentStyle;
        /// <summary>
        /// A flag indicating whether the panel has been created or not.
        /// </summary>
        private bool _panelCreated = false;
        /// <summary>
        /// A static flag indicating whether the current thread is inside a panel or not.
        /// </summary>
        private static bool _isInsidePanel = false;

        /// <summary>
        /// Creates a new panel with the provided ID, height, width, scrollable flag,
        /// and border flag.
        /// </summary>
        /// <param name="id">The ID of the panel.</param>
        /// <param name="height">The optional height of the panel. Defaults to 0.</param>
        /// <param name="width">The optional width of the panel. Defaults to 0.</param>
        /// <param name="flags">Behaviour flags of this panel.</param>
        public FuPanel(string id, float height = 0, float width = 0, FuPanelFlags flags = FuPanelFlags.Default)
        {
            _currentStyle = FuStyle.Default;
            beginPanel(id, height, width, !flags.HasFlag(FuPanelFlags.NoScroll), flags.HasFlag(FuPanelFlags.DrawBorders));
        }

        /// <summary>
        /// Creates a new panel with the provided ID, layout style, height, width, scrollable flag,
        /// and border flag.
        /// </summary>
        /// <param name="id">The ID of the panel.</param>
        /// <param name="style">The layout style of the panel.</param>
        /// <param name="height">The optional height of the panel. Defaults to 0.</param>
        /// <param name="width">The optional width of the panel. Defaults to 0.</param>
        /// <param name="flags">Behaviour flags of this panel.</param>
        public FuPanel(string id, FuStyle style, float height = 0, float width = 0, FuPanelFlags flags = FuPanelFlags.Default)
        {
            _currentStyle = style;
            beginPanel(id, height, width, !flags.HasFlag(FuPanelFlags.NoScroll), flags.HasFlag(FuPanelFlags.DrawBorders));
        }

        /// <summary>
        /// Initializes a new panel with the provided ID, height, width, scrollable flag, and border flag.
        /// </summary>
        /// <param name="id">The ID of the panel.</param>
        /// <param name="height">The optional height of the panel. Defaults to 0.</param>
        /// <param name="width">The optional width of the panel. Defaults to 0.</param>
        /// <param name="scrollable">A flag indicating whether the panel is scrollable or not. Defaults to true.</param>
        /// <param name="border">A flag indicating whether the panel has a border or not. Defaults to false.</param>
        private void beginPanel(string id, float height, float width, bool scrollable, bool border)
        {
            // Check if the current thread is already inside a panel.
            if (_isInsidePanel)
            {
                Debug.LogWarning("Cannot create panel inside another panel. Switched for you, but please check your code and remove it.");
                return;
            }

            // Add padding to the top of the panel.
            if (_currentStyle.WindowPadding.y != 0)
            {
                ImGui.Dummy(new Vector2(0f, _currentStyle.WindowPadding.y));
            }

            // Add padding to the left of the panel.
            if (_currentStyle.WindowPadding.x != 0)
            {
                ImGui.Dummy(new Vector2(_currentStyle.WindowPadding.x, 0f));
                ImGui.SameLine();
            }

            // Calculate the width of the panel. If a width was not specified, use the available width in the content region.
            if (width <= 0)
            {
                width = ImGui.GetContentRegionAvail().x - width;
            }
            width -= _currentStyle.WindowPadding.x * 2f;
            width = Mathf.Max(1f, Mathf.Clamp(width, 1f, ImGui.GetContentRegionAvail().x));

            // Calculate the height of the panel. If a height was not specified, use the available height in the content region.
            if (height <= 0)
            {
                height = ImGui.GetContentRegionAvail().y - height;
            }
            height -= _currentStyle.WindowPadding.y * 2f;
            height = Mathf.Max(1f, Mathf.Clamp(height, 1f, ImGui.GetContentRegionAvail().y));

            // Create the panel frag
            ImGuiWindowFlags flag = ImGuiWindowFlags.ChildWindow | ImGuiWindowFlags.AlwaysUseWindowPadding;
            if (!scrollable)
            {
                flag |= ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
            }
            // push style if created
            _currentStyle.Push(true);
            // draw imgui child panel
            _panelCreated = ImGui.BeginChild(id, new Vector2(width, height), border, flag);
            if (!_panelCreated)
            {
                // push style if created
                _currentStyle.Pop();
            }
            // assume we now are inside a panel
            _isInsidePanel = _panelCreated;
        }

        /// <summary>
        /// Closes and cleans up the panel.
        /// </summary>
        public void Dispose()
        {
            if (_panelCreated)
            {
                ImGui.EndChild();
                _isInsidePanel = false;
                _currentStyle.Pop();
            }
        }
    }
}