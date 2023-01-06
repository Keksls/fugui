using ImGuiNET;
using System;
using UnityEngine;

namespace Fugui.Framework
{
    public class UIContainer : IDisposable
    {
        /// <summary>
        /// The current layout style for the container.
        /// </summary>
        private UILayoutStyle _currentStyle;
        /// <summary>
        /// A flag indicating whether the container has been created or not.
        /// </summary>
        private bool _containerCreated = false;
        /// <summary>
        /// A static flag indicating whether the current thread is inside a container or not.
        /// </summary>
        private static bool _isInsideContainer = false;

        /// <summary>
        /// Creates a new container with the provided ID, height, width, scrollable flag,
        /// and border flag.
        /// </summary>
        /// <param name="id">The ID of the container.</param>
        /// <param name="height">The optional height of the container. Defaults to 0.</param>
        /// <param name="width">The optional width of the container. Defaults to 0.</param>
        /// <param name="scrollable">A flag indicating whether the container is scrollable or not. Defaults to true.</param>
        /// <param name="border">A flag indicating whether the container has a border or not. Defaults to false.</param>
        public UIContainer(string id, float height = 0, float width = 0, bool scrollable = true, bool border = false)
        {
            _currentStyle = UILayoutStyle.Default;
            beginContainer(id, height, width, scrollable, border);
        }

        /// <summary>
        /// Creates a new container with the provided ID, layout style, height, width, scrollable flag,
        /// and border flag.
        /// </summary>
        /// <param name="id">The ID of the container.</param>
        /// <param name="style">The layout style of the container.</param>
        /// <param name="height">The optional height of the container. Defaults to 0.</param>
        /// <param name="width">The optional width of the container. Defaults to 0.</param>
        /// <param name="scrollable">A flag indicating whether the container is scrollable or not. Defaults to true.</param>
        /// <param name="border">A flag indicating whether the container has a border or not. Defaults to false.</param>
        public UIContainer(string id, UILayoutStyle style, float height = 0, float width = 0, bool scrollable = true, bool border = false)
        {
            _currentStyle = style;
            beginContainer(id, height, width, scrollable, border);
        }

        /// <summary>
        /// Initializes a new container with the provided ID, height, width, scrollable flag, and border flag.
        /// </summary>
        /// <param name="id">The ID of the container.</param>
        /// <param name="height">The optional height of the container. Defaults to 0.</param>
        /// <param name="width">The optional width of the container. Defaults to 0.</param>
        /// <param name="scrollable">A flag indicating whether the container is scrollable or not. Defaults to true.</param>
        /// <param name="border">A flag indicating whether the container has a border or not. Defaults to false.</param>

        private void beginContainer(string id, float height, float width, bool scrollable, bool border)
        {
            // Check if the current thread is already inside a container.
            if (_isInsideContainer)
            {
                Debug.LogWarning("Cannot create Container inside another Container. Switched for you, but please check your code and remove it.");
                return;
            }

            // Add padding to the top of the container.
            if (_currentStyle.WindowPadding.y != 0)
            {
                ImGui.Dummy(new Vector2(0f, _currentStyle.WindowPadding.y));
            }

            // Add padding to the left of the container.
            if (_currentStyle.WindowPadding.x != 0)
            {
                ImGui.Dummy(new Vector2(_currentStyle.WindowPadding.x, 0f));
                ImGui.SameLine();
            }

            // Calculate the width of the container. If a width was not specified, use the available width in the content region.
            if (width <= 0)
            {
                width = ImGui.GetContentRegionAvail().x - width;
            }
            width -= _currentStyle.WindowPadding.x * 2f;
            width = Mathf.Max(1f, Mathf.Clamp(width, 1f, ImGui.GetContentRegionAvail().x));

            // Calculate the height of the container. If a height was not specified, use the available height in the content region.
            if (height <= 0)
            {
                height = ImGui.GetContentRegionAvail().y - height;
            }
            height -= _currentStyle.WindowPadding.y * 2f;
            height = Mathf.Max(1f, Mathf.Clamp(height, 1f, ImGui.GetContentRegionAvail().y));

            // Create the container frag
            ImGuiWindowFlags flag = ImGuiWindowFlags.ChildWindow | ImGuiWindowFlags.AlwaysUseWindowPadding;
            if (!scrollable)
            {
                flag |= ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
            }
            // draw imgui child container
            _containerCreated = ImGui.BeginChild(id, new Vector2(width, height), border, flag);
            if (_containerCreated)
            {
                // push style if created
                _currentStyle.Push(true);
            }
            // assume we now are inside a container
            _isInsideContainer = _containerCreated;
        }

        /// <summary>
        /// Closes and cleans up the container.
        /// </summary>
        public void Dispose()
        {
            if (_containerCreated)
            {
                ImGui.EndChild();
                _isInsideContainer = false;
                _currentStyle.Pop();
            }
        }
    }
}