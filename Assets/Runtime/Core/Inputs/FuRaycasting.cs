using Fu.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu.Core
{
    public static class FuRaycasting
    {
        static Dictionary<string, FuRaycaster> latestRaycasters = new Dictionary<string, FuRaycaster>();
        private static Dictionary<string, FuRaycaster> _raycasters = new Dictionary<string, FuRaycaster>();

        public static InputState GetInputState(string containerID, GameObject raycastableGameObject)
        {
            foreach (FuRaycaster raycaster in _raycasters.Values)
            {
                if (raycaster.RaycastThisFrame && raycaster.Hit.collider.gameObject == raycastableGameObject)
                {
                    if (!latestRaycasters.ContainsKey(containerID) || latestRaycasters[containerID] != raycaster)
                    {
                        latestRaycasters[containerID] = raycaster;
                        // do something with the latest raycaster
                    }
                }
                else if (latestRaycasters.ContainsKey(containerID) && latestRaycasters[containerID] == raycaster)
                {
                    latestRaycasters.Remove(containerID);
                    // do something with the previous latest raycaster
                }
            }

            if (!latestRaycasters.ContainsKey(containerID))
            {
                return new InputState(string.Empty, false, false, false, false, 0f, new Vector2(-1f, -1f));
            }
            else
            {
                FuRaycaster raycaster = latestRaycasters[containerID];
                Vector3 localHitPoint = raycastableGameObject.transform.InverseTransformPoint(raycaster.Hit.point);
                Vector2 localPosition = new Vector2(localHitPoint.x, localHitPoint.y);
                return new InputState(raycaster.ID, true, raycaster.MouseButton0(), raycaster.MouseButton1(), raycaster.MouseButton2(), raycaster.MouseWheel(), localPosition);
            }
        }

        public static void Update()
        {
            foreach (FuRaycaster raycaster in _raycasters.Values)
            {
                raycaster.Raycast();
            }
        }

        public static bool RegisterRaycaster(FuRaycaster raycaster)
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

        public static IEnumerable<FuRaycaster> GetAllRaycasters()
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

    public class FuRaycaster
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

        public FuRaycaster(string name, Func<Ray> rayGetter, Func<bool> mouseButton0, Func<bool> mouseButton1, Func<bool> mouseButton2, Func<float> mouseWheel, Func<bool> actifGetter)
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
                if (Physics.Raycast(GetRay(), out RaycastHit hit, Fugui.Settings.UIRaycastDistance, Fugui.Settings.UILayer.value))
                {
                    Hit = hit;
                    RaycastThisFrame = true;
                }
            }
        }
    }
}