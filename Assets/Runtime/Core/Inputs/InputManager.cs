using Fugui.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fugui.Core
{
    public static class InputManager
    {
        private static int _raycastLayer;
        private static Dictionary<string, FuguiRaycaster> _raycasters = new Dictionary<string, FuguiRaycaster>();
        private static Dictionary<string, containerRaycasters> _containersRaycasters = new Dictionary<string, containerRaycasters>();

        public static void Initialize()
        {
            _raycastLayer = LayerMask.NameToLayer(FuGui.Settings.UILayer);
        }

        public static InputState GetInputState(string containerID, GameObject raycastableGameObject)
        {
            if (!_containersRaycasters.ContainsKey(containerID))
            {
                _containersRaycasters.Add(containerID, new containerRaycasters());
            }

            // determinate which raycaster has the hand
            containerRaycasters lastFrameRaycasters = new containerRaycasters(_containersRaycasters[containerID]);
            _containersRaycasters[containerID].Clear();

            foreach (FuguiRaycaster raycaster in _raycasters.Values)
            {
                // the raycaster just hit the 3D UI Container GameObject
                if (raycaster.RaycastThisFrame && raycaster.Hit.collider.gameObject == raycastableGameObject)
                {
                    _containersRaycasters[containerID].Add(raycaster.ID);
                    if (!lastFrameRaycasters.Contains(raycaster.ID))
                    {
                        _containersRaycasters[containerID].CurrentRaycaster = raycaster.ID;
                    }
                }
            }

            if (_containersRaycasters[containerID].Count == 0)
            {
                _containersRaycasters[containerID].CurrentRaycaster = string.Empty;
            }

            if (string.IsNullOrEmpty(_containersRaycasters[containerID].CurrentRaycaster))
            {
                return new InputState(string.Empty, false, false, false, false, 0f, new Vector2(-1f, -1f));
            }
            else
            {
                FuguiRaycaster raycaster = _raycasters[_containersRaycasters[containerID].CurrentRaycaster];
                Vector3 localHitPoint = raycastableGameObject.transform.InverseTransformPoint(raycaster.Hit.point);
                Vector2 localPosition = new Vector2(localHitPoint.x, localHitPoint.y);
                return new InputState(_containersRaycasters[containerID].CurrentRaycaster, true, raycaster.MouseButton0(), raycaster.MouseButton1(), raycaster.MouseButton2(), raycaster.MouseWheel(), localPosition);
            }
        }

        public static void Update()
        {
            foreach (FuguiRaycaster raycaster in _raycasters.Values)
            {
                raycaster.Raycast(_raycastLayer);
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

        private class containerRaycasters : HashSet<string>
        {
            public string CurrentRaycaster = string.Empty;

            public containerRaycasters(containerRaycasters container) : base(container)
            {
                CurrentRaycaster = container.CurrentRaycaster;
            }

            public containerRaycasters() : base()
            {
                CurrentRaycaster = string.Empty;
            }
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

        internal void Raycast(int raycastLayer)
        {
            RaycastThisFrame = false;
            if (IsActif())
            {
                if (Physics.Raycast(GetRay(), out RaycastHit hit, raycastLayer))
                {
                    Debug.Log("raycast");
                    Hit = hit;
                    RaycastThisFrame = true;
                }
            }
        }
    }
}