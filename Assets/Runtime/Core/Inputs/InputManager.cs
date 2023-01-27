using Fugui.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fugui.Core
{
    public static class InputManager
    {
        private static Dictionary<string, Stack<FuguiRaycaster>> _raycasterStack = new Dictionary<string, Stack<FuguiRaycaster>>();
        private static Dictionary<string, FuguiRaycaster> _raycasters = new Dictionary<string, FuguiRaycaster>();
        
        public static InputState GetInputState(string containerID, GameObject raycastableGameObject)
        {
            if (!_raycasterStack.ContainsKey(containerID))
            {
                _raycasterStack.Add(containerID, new Stack<FuguiRaycaster>());
            }

            foreach (FuguiRaycaster raycaster in _raycasters.Values)
            {
                if (raycaster.RaycastThisFrame && raycaster.Hit.collider.gameObject.Equals(raycastableGameObject))
                {
                    // Pop any raycasters that are no longer hovering the collider
                    while (_raycasterStack[containerID].Count > 0 && _raycasterStack[containerID].Peek() != raycaster)
                    {
                        _raycasterStack[containerID].Pop();
                    }
                    // Push the current raycaster onto the stack
                    _raycasterStack[containerID].Push(raycaster);
                }
            }

            if (_raycasterStack[containerID].Count == 0)
            {
                return new InputState(string.Empty, false, false, false, false, 0f, new Vector2(-1f, -1f));
            }
            else
            {
                FuguiRaycaster raycaster = _raycasterStack[containerID].Peek();
                Vector3 localHitPoint = raycastableGameObject.transform.InverseTransformPoint(raycaster.Hit.point);
                Vector2 localPosition = new Vector2(localHitPoint.x, localHitPoint.y);
                return new InputState(raycaster.ID, true, raycaster.MouseButton0(), raycaster.MouseButton1(), raycaster.MouseButton2(), raycaster.MouseWheel(), localPosition);
            }
        }

        public static void Update()
        {
            foreach (FuguiRaycaster raycaster in _raycasters.Values)
            {
                raycaster.Raycast();
            }
        }

        public static bool RegisterRaycaster(FuguiRaycaster raycaster)
        {
            if (_raycasters.ContainsKey(raycaster.ID))
            {
                Debug.Log("You are trying to register a raycaster with the name '" + raycaster.ID + "' that already exists.");
                return false;
            }
            _raycasters.Add(raycaster.ID, raycaster);
            return true;
        }

        public static bool UnRegisterRaycaster(string raycasterName)
        {
            return _raycasters.Remove(raycasterName);
        }

        public static IEnumerable<FuguiRaycaster> GetAllRaycasters()
        {
            return _raycasters.Values;
        }
    }

    public struct InputState
    {
        private string _raycasterID;
        private bool _hovered;
        private float _mouseWheel;
        private bool[] _mouseDown;
        private Vector2 _mousePosition;
        public string RaycasterID { get { return _raycasterID; } }
        public float MouseWheel { get { return _mouseWheel; } }
        public bool Hovered { get { return _hovered; } }
        public Vector2 MousePosition { get { return _mousePosition; } }
        public bool[] MouseDown { get { return _mouseDown; } }

        public InputState(string raycasterID, bool hovered, bool mouseDown0, bool mouseDown1, bool mouseDown2, float mouseWheel, Vector2 mousePosition)
        {
            _mouseWheel = mouseWheel;
            _raycasterID = raycasterID;
            _hovered = hovered;
            _mouseDown = new bool[] { mouseDown0, mouseDown1, mouseDown2 };
            _mousePosition = mousePosition;
        }
    }

    public class FuguiRaycaster
    {
        public Func<bool> IsActif { get; private set; }
        public Func<bool> MouseButton0 { get; private set; }
        public Func<bool> MouseButton1 { get; private set; }
        public Func<bool> MouseButton2 { get; private set; }
        public Func<float> MouseWheel { get; private set; }
        public Func<Ray> GetRay { get; private set; }
        public string ID { get; private set; }
        public bool RaycastThisFrame { get; private set; }
        public RaycastHit Hit { get; private set; }

        public FuguiRaycaster(string name, Func<Ray> rayGetter, Func<bool> mouseButton0, Func<bool> mouseButton1, Func<bool> mouseButton2, Func<float> mouseWheel, Func<bool> actifGetter)
        {
            ID = name;
            IsActif = actifGetter;
            MouseButton0 = mouseButton0;
            MouseButton1 = mouseButton1;
            MouseButton2 = mouseButton2;
            MouseWheel = mouseWheel;
            GetRay = rayGetter;
        }

        internal void Raycast()
        {
            RaycastThisFrame = false;
            if (IsActif())
            {
                if (Physics.Raycast(GetRay(), out RaycastHit hit, FuGui.Settings.UIRaycastDistance, FuGui.Settings.UILayer.value))
                {
                    Debug.Log("raycast");
                    Hit = hit;
                    RaycastThisFrame = true;
                }
            }
        }
    }
}