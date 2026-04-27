#if (UNITY_ANDROID || UNITY_IOS)// && !UNITY_EDITOR && !FUMOBILE
#define FUMOBILE
#endif
using System.Collections.Generic;
using ImGuiNET;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Represents the Fugui type.
    /// </summary>
    public partial class Fugui
    {
        #region State
        public static bool IsScrolling => _isScrolling;

        private const float ScrollStartThreshold = 8f;
        private const float ScrollDeadZone = 0.05f;
        private const float ScrollSpeedMultiplier = 1.0f;
        private const float MaxScrollDeltaPerFrame = 35f;

        // Inertia tuning
        private const float VelocityLerp = 0.22f;
        private const float ReleaseVelocityBoost = 1.08f;
        private const float InertiaDecayPerSecond = 7.5f;
        private const float MinInertiaSpeed = 18f;
        private const float MaxInertiaSpeed = 2200f;

        private static bool _touchWasPressed;
        private static bool _isScrolling;
        private static bool _isPressed;
        private static bool _wasScrollingLastFrame;

        private static Vector2 _touchStartPosition;
        private static Vector2 _lastTouchPosition;
        private static Vector2 _smoothedScrollDelta;

        private static float _scrollVelocityY;
        private static bool _inertiaActive;

        private static uint _activeChildId;
        private static Vector2 _currentChildPos;
        private static Vector2 _currentChildSize;
        private static uint _currentChildId;
        private static Dictionary<uint, Rect> _childRects = new Dictionary<uint, Rect>();
        #endregion

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

        /// <summary>
        /// Returns the begin child result.
        /// </summary>
        /// <param name="str_id">The str id value.</param>
        /// <param name="size">The size value.</param>
        /// <returns>The result of the operation.</returns>
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

        /// <summary>
        /// Call this once per frame before drawing UI.
        /// </summary>
        private static void TouchScrollBeginFrame()
        {
            _isPressed = GetCurrentMouse().IsPressed(FuMouseButton.Left);
            Vector2 position = DefaultContainer.LocalMousePos;

            _wasScrollingLastFrame = _isScrolling;

            if (_isPressed && !_touchWasPressed)
            {
                _touchStartPosition = position;
                _lastTouchPosition = position;
                _smoothedScrollDelta = Vector2.zero;
                _isScrolling = false;
                _activeChildId = 0;
                _scrollVelocityY = 0f;
                _inertiaActive = false;
            }
            else if (!_isPressed && _touchWasPressed)
            {
                if (_isScrolling && Mathf.Abs(_scrollVelocityY) >= MinInertiaSpeed)
                {
                    _scrollVelocityY *= ReleaseVelocityBoost;
                    _scrollVelocityY = Mathf.Clamp(_scrollVelocityY, -MaxInertiaSpeed, MaxInertiaSpeed);
                    _inertiaActive = true;
                    _isScrolling = false;
                }
                else
                {
                    _scrollVelocityY = 0f;
                    _inertiaActive = false;
                    _isScrolling = false;
                    _activeChildId = 0;
                }
            }

            _touchWasPressed = _isPressed;
        }

        /// <summary>
        /// Handles touch scrolling for the current child if applicable.
        /// Should be called every frame after BeginChild and before EndChild for the child you want to have touch scroll support.
        /// </summary>
        private static void HandleCurrentChildScroll()
        {
            if (!_childRects.ContainsKey(_currentChildId) || Fugui.IsDraggingAnything())
                return;

            if (!_isPressed && !(_inertiaActive && _activeChildId == _currentChildId))
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
                    _smoothedScrollDelta = Vector2.zero;
                    _scrollVelocityY = 0f;
                    _inertiaActive = false;
                }
            }

            if (_isScrolling && _activeChildId == _currentChildId)
            {
                float dt = Mathf.Max(UnityEngine.Time.unscaledDeltaTime, 0.0001f);

                Vector2 rawDelta = mousePosition - _lastTouchPosition;
                _lastTouchPosition = mousePosition;

                float absRawY = Mathf.Abs(rawDelta.y);

                float adaptiveDeltaLerp = Mathf.Lerp(
                    0.35f,
                    0.18f,
                    Mathf.Clamp01(absRawY / (10f * Fugui.Scale)));

                _smoothedScrollDelta = Vector2.Lerp(_smoothedScrollDelta, rawDelta, adaptiveDeltaLerp);

                float scrollDeltaY = _smoothedScrollDelta.y;
                // --- Velocity-based boost ---
                float speed = Mathf.Abs(rawDelta.y);

                // Normalize speed (tweak 10f)
                float normalizedSpeed = Mathf.Clamp01(speed / (10f * Fugui.Scale));

                // Curve for stronger fast scroll (quadratic feel)
                float speedMultiplier = 1f + (normalizedSpeed * normalizedSpeed) * 2.5f;

                // Apply boost
                scrollDeltaY *= speedMultiplier;

                if (Mathf.Abs(rawDelta.y) < ScrollDeadZone * Fugui.Scale)
                {
                    scrollDeltaY = 0f;
                }

                if (Mathf.Abs(rawDelta.y) > ScrollDeadZone * Fugui.Scale &&
                    Mathf.Abs(scrollDeltaY) < 0.5f * Fugui.Scale)
                {
                    scrollDeltaY = Mathf.Sign(rawDelta.y) * 0.5f * Fugui.Scale;
                }

                scrollDeltaY = Mathf.Clamp(
                    scrollDeltaY * ScrollSpeedMultiplier,
                    -MaxScrollDeltaPerFrame * Fugui.Scale,
                    MaxScrollDeltaPerFrame * Fugui.Scale);

                float currentScrollY = ImGui.GetScrollY();
                float maxScrollY = ImGui.GetScrollMaxY();

                float newScrollY = Mathf.Clamp(currentScrollY - scrollDeltaY, 0f, maxScrollY);
                ImGui.SetScrollY(newScrollY);

                float instantaneousVelocity = (-scrollDeltaY) / dt;
                _scrollVelocityY = Mathf.Lerp(_scrollVelocityY, instantaneousVelocity, VelocityLerp);
                _scrollVelocityY = Mathf.Clamp(_scrollVelocityY, -MaxInertiaSpeed, MaxInertiaSpeed);

                _inertiaActive = false;

                ImGui.GetForegroundDrawList().AddRect(
                    childRect.position,
                    childRect.position + childRect.size,
                    ImGui.GetColorU32(new Vector4(1f, 0f, 0f, 0.5f)));
            }
            else if (!_isPressed && _inertiaActive && _activeChildId == _currentChildId)
            {
                float dt = Mathf.Max(UnityEngine.Time.unscaledDeltaTime, 0.0001f);

                float currentScrollY = ImGui.GetScrollY();
                float maxScrollY = ImGui.GetScrollMaxY();

                float deltaFromInertia = _scrollVelocityY * dt;
                float newScrollY = Mathf.Clamp(currentScrollY + deltaFromInertia, 0f, maxScrollY);

                ImGui.SetScrollY(newScrollY);

                float decay = Mathf.Exp(-InertiaDecayPerSecond * dt);
                _scrollVelocityY *= decay;

                bool reachedTop = Mathf.Approximately(newScrollY, 0f);
                bool reachedBottom = Mathf.Approximately(newScrollY, maxScrollY);

                if (reachedTop || reachedBottom)
                {
                    _scrollVelocityY = 0f;
                    _inertiaActive = false;
                    _activeChildId = 0;
                }
                else if (Mathf.Abs(_scrollVelocityY) < MinInertiaSpeed)
                {
                    _scrollVelocityY = 0f;
                    _inertiaActive = false;
                    _activeChildId = 0;
                }

                //ImGui.GetForegroundDrawList().AddRect(
                //    childRect.position,
                //    childRect.position + childRect.size,
                //    ImGui.GetColorU32(new Vector4(1f, 0f, 0f, 0.5f)));
            }
        }

        /// <summary>
        /// Resets all touch scroll state.
        /// </summary>
        private static void ResetTouchState()
        {
            _touchWasPressed = false;
            _isScrolling = false;
            _isPressed = false;
            _wasScrollingLastFrame = false;
            _activeChildId = 0;
            _touchStartPosition = Vector2.zero;
            _lastTouchPosition = Vector2.zero;
            _smoothedScrollDelta = Vector2.zero;
            _scrollVelocityY = 0f;
            _inertiaActive = false;
        }
    }
}