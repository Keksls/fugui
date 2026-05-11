using System;
using UnityEngine;

namespace Fu
{
        /// <summary>
        /// Represents the Fu Raycaster type.
        /// </summary>
        public class FuRaycaster
        {
            #region State
            public Func<bool> IsActif { get; private set; }
            public Func<bool> MouseButton0 { get; private set; }
            public Func<bool> MouseButton1 { get; private set; }
            public Func<bool> MouseButton2 { get; private set; }
            public Func<float> MouseWheel { get; private set; }
            public Func<Ray> GetRay { get; private set; }
            public Func<Transform> GetTransform { get; private set; }
            public string ID { get; private set; }
            public bool RaycastThisFrame { get; private set; }
            public RaycastHit Hit { get; private set; }
            private const int MouseButtonCount = 3;
            private readonly bool[] _mouseButtons = new bool[MouseButtonCount];
            private readonly bool[] _previousMouseButtons = new bool[MouseButtonCount];
            private RaycastHit[] _hits = Array.Empty<RaycastHit>();
            private int _inputStateFrame = -1;
            private float _mouseWheel;
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Fu Raycaster class.
            /// </summary>
            /// <param name="name">The name value.</param>
            /// <param name="rayGetter">The ray Getter value.</param>
            /// <param name="mouseButton0">The mouse Button0 value.</param>
            /// <param name="mouseButton1">The mouse Button1 value.</param>
            /// <param name="mouseButton2">The mouse Button2 value.</param>
            /// <param name="mouseWheel">The mouse Wheel value.</param>
            /// <param name="actifGetter">The actif Getter value.</param>
            public FuRaycaster(string name, Func<Ray> rayGetter, Func<bool> mouseButton0, Func<bool> mouseButton1, Func<bool> mouseButton2, Func<float> mouseWheel, Func<bool> actifGetter)
                : this(name, rayGetter, mouseButton0, mouseButton1, mouseButton2, mouseWheel, actifGetter, null)
            {
            }

            /// <summary>
            /// Initializes a new instance of the Fu Raycaster class.
            /// </summary>
            /// <param name="name">The name value.</param>
            /// <param name="rayGetter">The ray Getter value.</param>
            /// <param name="mouseButton0">The mouse Button0 value.</param>
            /// <param name="mouseButton1">The mouse Button1 value.</param>
            /// <param name="mouseButton2">The mouse Button2 value.</param>
            /// <param name="mouseWheel">The mouse Wheel value.</param>
            /// <param name="actifGetter">The actif Getter value.</param>
            /// <param name="transformGetter">Optional transform Getter used by interactions that need a raycaster pose.</param>
            public FuRaycaster(string name, Func<Ray> rayGetter, Func<bool> mouseButton0, Func<bool> mouseButton1, Func<bool> mouseButton2, Func<float> mouseWheel, Func<bool> actifGetter, Func<Transform> transformGetter)
            {
                ID = name;
                IsActif = actifGetter;
                MouseButton0 = mouseButton0;
                MouseButton1 = mouseButton1;
                MouseButton2 = mouseButton2;
                MouseWheel = mouseWheel;
                GetRay = rayGetter;
                GetTransform = transformGetter;
            }
            #endregion

            #region Methods
            /// <summary>
            /// Tries to get the optional transform associated with this raycaster.
            /// </summary>
            /// <param name="raycasterTransform">Raycaster transform when one is available.</param>
            /// <returns>True if a transform is available.</returns>
            public bool TryGetTransform(out Transform raycasterTransform)
            {
                raycasterTransform = GetTransform != null ? GetTransform() : null;
                return raycasterTransform != null;
            }

            /// <summary>
            /// Returns the current sampled state for a mouse/controller button.
            /// </summary>
            /// <param name="buttonIndex">Button index.</param>
            /// <returns>True if the button is currently pressed.</returns>
            public bool GetMouseButton(int buttonIndex)
            {
                UpdateInputState();
                return isValidMouseButtonIndex(buttonIndex) && _mouseButtons[buttonIndex];
            }

            /// <summary>
            /// Returns whether a mouse/controller button started being pressed this frame.
            /// </summary>
            /// <param name="buttonIndex">Button index.</param>
            /// <returns>True if the button went down this frame.</returns>
            public bool GetMouseButtonDownThisFrame(int buttonIndex)
            {
                UpdateInputState();
                return isValidMouseButtonIndex(buttonIndex) &&
                       _mouseButtons[buttonIndex] &&
                       !_previousMouseButtons[buttonIndex];
            }

            /// <summary>
            /// Returns whether a mouse/controller button was already pressed before this frame.
            /// </summary>
            /// <param name="buttonIndex">Button index.</param>
            /// <returns>True if the button is held from an earlier frame.</returns>
            public bool IsMouseButtonPressedBeforeCurrentFrame(int buttonIndex)
            {
                UpdateInputState();
                return isValidMouseButtonIndex(buttonIndex) &&
                       _mouseButtons[buttonIndex] &&
                       _previousMouseButtons[buttonIndex];
            }

            /// <summary>
            /// Returns the current sampled mouse/controller wheel value.
            /// </summary>
            /// <returns>Wheel delta.</returns>
            public float GetMouseWheelDelta()
            {
                UpdateInputState();
                return _mouseWheel;
            }

            /// <summary>
            /// Attempts to get this frame's raycast hit for a specific GameObject.
            /// </summary>
            /// <param name="raycastableGameObject">Target object.</param>
            /// <param name="hit">Target hit.</param>
            /// <returns>True if this raycaster hit the target this frame.</returns>
            public bool TryGetHit(GameObject raycastableGameObject, out RaycastHit hit)
            {
                hit = default;
                if (raycastableGameObject == null || !RaycastThisFrame)
                {
                    return false;
                }

                if (Hit.collider != null && Hit.collider.gameObject == raycastableGameObject)
                {
                    hit = Hit;
                    return true;
                }

                bool found = false;
                float closestDistance = float.MaxValue;
                for (int i = 0; i < _hits.Length; i++)
                {
                    RaycastHit candidate = _hits[i];
                    if (candidate.collider == null ||
                        candidate.collider.gameObject != raycastableGameObject ||
                        candidate.distance >= closestDistance)
                    {
                        continue;
                    }

                    hit = candidate;
                    closestDistance = candidate.distance;
                    found = true;
                }

                return found;
            }

            /// <summary>
            /// Runs the raycast workflow.
            /// </summary>
            internal void Raycast()
            {
                RaycastThisFrame = false;
                Hit = default;
                _hits = Array.Empty<RaycastHit>();
                if (IsActif())
                {
                    _hits = Physics.RaycastAll(GetRay(), Fugui.Settings.UIRaycastDistance, Fugui.Settings.UILayer.value);
                    if (_hits.Length > 0)
                    {
                        Array.Sort(_hits, (left, right) => left.distance.CompareTo(right.distance));
                        Hit = _hits[0];
                        RaycastThisFrame = true;
                    }
                }
            }

            /// <summary>
            /// Samples input delegates once per Unity frame.
            /// </summary>
            internal void UpdateInputState()
            {
                if (_inputStateFrame == Time.frameCount)
                {
                    return;
                }

                _inputStateFrame = Time.frameCount;
                for (int i = 0; i < MouseButtonCount; i++)
                {
                    _previousMouseButtons[i] = _mouseButtons[i];
                }

                _mouseButtons[0] = MouseButton0 != null && MouseButton0();
                _mouseButtons[1] = MouseButton1 != null && MouseButton1();
                _mouseButtons[2] = MouseButton2 != null && MouseButton2();
                _mouseWheel = MouseWheel != null ? MouseWheel() : 0f;
            }

            /// <summary>
            /// Returns whether the requested button index is supported.
            /// </summary>
            /// <param name="buttonIndex">Button index.</param>
            /// <returns>True when the index is valid.</returns>
            private static bool isValidMouseButtonIndex(int buttonIndex)
            {
                return buttonIndex >= 0 && buttonIndex < MouseButtonCount;
            }
            #endregion
        }
}
