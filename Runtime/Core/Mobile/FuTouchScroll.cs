#if (UNITY_ANDROID || UNITY_IOS)// && !UNITY_EDITOR && !FUMOBILE
#define FUMOBILE
using System.Collections.Generic;
#endif
using ImGuiNET;
using UnityEngine;

namespace Fu
{
    public partial class Fugui
    {
        public static bool IsScrolling => _isScrolling;
#if FUMOBILE
        private const float ScrollStartThreshold = 8f;
        private static bool _touchWasPressed;
        private static bool _isScrolling;
        private static Vector2 _touchStartPosition;
        private static Vector2 _lastTouchPosition;
        private static uint _activeChildId;
        private static Vector2 _currentChildPos;
        private static Vector2 _currentChildSize;
        private static uint _currentChildId;
        private static bool _isPressed = false;
        private static Dictionary<uint, Rect> _childRects = new Dictionary<uint, Rect>();
#endif

        /// <summary>
        /// Call this once per frame before drawing UI.
        /// </summary>
        public static void BeginMobileFrame()
        {
#if FUMOBILE
            _isPressed = GetCurrentMouse().IsPressed(FuMouseButton.Left);
            Vector2 position = DefaultContainer.LocalMousePos;

            if (_isPressed && !_touchWasPressed)
            {
                _touchStartPosition = position;
                _lastTouchPosition = position;
                _isScrolling = false;
                _activeChildId = 0;
            }
            else if (!_isPressed && _touchWasPressed)
            {
                ResetTouchState();
            }

            _touchWasPressed = _isPressed;
#endif
        }

        /// <summary>
        /// Begin a child with mobile touch scroll support.
        /// </summary>
        public static bool BeginChild(string id, Vector2 size, ImGuiChildFlags childFlags = ImGuiChildFlags.None, ImGuiWindowFlags windowFlags = ImGuiWindowFlags.None)
        {
#if FUMOBILE
            _currentChildId = ImGui.GetID(id);
            _currentChildPos = ImGui.GetCursorScreenPos();
            bool opened = ImGui.BeginChild(id, size, childFlags, windowFlags);
            if (!windowFlags.HasFlag(ImGuiWindowFlags.NoScrollbar))
                HandleCurrentChildScroll();
            return opened;
#else
            return ImGui.BeginChild(id, size, childFlags, windowFlags);
#endif
        }

        public static bool BeginChild(string str_id, Vector2 size)
        {
#if FUMOBILE
            _currentChildId = ImGui.GetID(str_id);
            _currentChildPos = ImGui.GetCursorScreenPos();
            bool opened = ImGui.BeginChild(str_id, size);
            HandleCurrentChildScroll();
            return opened;
#else
                        return ImGui.BeginChild(str_id, size);
#endif
        }

        /// <summary>
        /// Begin a child with mobile touch scroll support.
        /// </summary>
        public static bool BeginChild(uint id, Vector2 size, ImGuiChildFlags childFlags = ImGuiChildFlags.None, ImGuiWindowFlags windowFlags = ImGuiWindowFlags.None)
        {
#if FUMOBILE
            _currentChildId = id;
            _currentChildPos = ImGui.GetCursorScreenPos();
            bool opened = ImGui.BeginChild(id, size, childFlags, windowFlags);
            if (!windowFlags.HasFlag(ImGuiWindowFlags.NoScrollbar))
                HandleCurrentChildScroll();
            return opened;
#else
            return ImGui.BeginChild(id, size, childFlags, windowFlags);
#endif
        }

        /// <summary>
        /// End the child.
        /// </summary>
        public static void EndChild()
        {
#if FUMOBILE
            float childWidth = ImGui.GetContentRegionAvail().x;
            ImGuiNative.igEndChild();
            _currentChildSize = new Vector2(childWidth, ImGui.GetCursorScreenPos().y - _currentChildPos.y);
            if (_childRects.ContainsKey(_currentChildId))
            {
                _childRects[_currentChildId] = new Rect(_currentChildPos, _currentChildSize);
            }
            else
            {
                _childRects.Add(_currentChildId, new Rect(_currentChildPos, _currentChildSize));
            }
#else
            ImGuiNative.igEndChild();
#endif
        }

        private static void HandleCurrentChildScroll()
        {
#if FUMOBILE
            if (!_childRects.ContainsKey(_currentChildId) || !_isPressed || Fugui.IsDraggingAnything())
                return;

            Vector2 mousePosition = DefaultContainer.LocalMousePos;
            Vector2 imguiTouch = new Vector2(mousePosition.x, ImGui.GetIO().DisplaySize.y - mousePosition.y);

            Rect childRect = _childRects[_currentChildId];
            Vector2 windowPos = childRect.position;
            Vector2 windowSize = childRect.size;

            bool isInside =
                imguiTouch.x >= windowPos.x &&
                imguiTouch.x <= windowPos.x + windowSize.x &&
                imguiTouch.y >= windowPos.y &&
                imguiTouch.y <= windowPos.y + windowSize.y;

            if (!_isScrolling)
            {
                if (!isInside)
                {
                    return;
                }

                Vector2 dragFromStart = mousePosition - _touchStartPosition;
                if (Mathf.Abs(dragFromStart.y) >= ScrollStartThreshold * Fugui.Scale &&
                    Mathf.Abs(dragFromStart.y) > Mathf.Abs(dragFromStart.x))
                {
                    _isScrolling = true;
                    _activeChildId = _currentChildId;
                    _lastTouchPosition = mousePosition;
                }
            }

            if (_isScrolling && _activeChildId == _currentChildId)
            {
                Vector2 delta = mousePosition - _lastTouchPosition;
                _lastTouchPosition = mousePosition;

                float currentScrollY = ImGui.GetScrollY();
                float maxScrollY = ImGui.GetScrollMaxY();

                float newScrollY = Mathf.Clamp(currentScrollY - delta.y, 0f, maxScrollY);
                ImGui.SetScrollY(newScrollY);

                // for debug purpuse, draw a red rectangle around the active child
                ImGui.GetForegroundDrawList().AddRect(childRect.position, childRect.position + childRect.size, ImGui.GetColorU32(new Vector4(1f, 0f, 0f, 0.5f)));
            }
#endif
        }

        private static void ResetTouchState()
        {
#if FUMOBILE
            _touchWasPressed = false;
            _isScrolling = false;
            _activeChildId = 0;
            _touchStartPosition = Vector2.zero;
            _lastTouchPosition = Vector2.zero;
#endif
        }
    }
}