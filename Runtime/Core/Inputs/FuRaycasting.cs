using System.Collections.Generic;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Represents the Fu Raycasting type.
    /// </summary>
    public static class FuRaycasting
    {
        #region State
        static Dictionary<string, FuRaycaster> latestRaycasters = new Dictionary<string, FuRaycaster>();
        private static Dictionary<string, FuRaycaster> _raycasters = new Dictionary<string, FuRaycaster>();
        #endregion

        #region Methods
        /// <summary>
        /// Gets the input state.
        /// </summary>
        /// <param name="containerID">The container ID value.</param>
        /// <param name="raycastableGameObject">The raycastable Game Object value.</param>
        /// <returns>The result of the operation.</returns>
        public static InputState GetInputState(string containerID, GameObject raycastableGameObject)
        {
            if (raycastableGameObject == null)
            {
                latestRaycasters.Remove(containerID);
                return getInactiveInputState();
            }

            FuPanelMesh panelMesh = raycastableGameObject.GetComponent<FuPanelMesh>();
            if (panelMesh != null && !panelMesh.CanReceiveInput)
            {
                latestRaycasters.Remove(containerID);
                return getInactiveInputState();
            }

            foreach (FuRaycaster raycaster in _raycasters.Values)
            {
                if (raycaster.RaycastThisFrame && raycaster.Hit.collider.gameObject == raycastableGameObject)
                {
                    if (!latestRaycasters.ContainsKey(containerID) || latestRaycasters[containerID] != raycaster)
                    {
                        latestRaycasters[containerID] = raycaster;
                    }
                }
                else if (latestRaycasters.ContainsKey(containerID) && latestRaycasters[containerID] == raycaster)
                {
                    latestRaycasters.Remove(containerID);
                }
            }

            if (!latestRaycasters.ContainsKey(containerID))
            {
                return getInactiveInputState();
            }
            else
            {
                FuRaycaster raycaster = latestRaycasters[containerID];
                Vector3 localHitPoint = raycastableGameObject.transform.InverseTransformPoint(raycaster.Hit.point);
                Vector2 localPosition = new Vector2(localHitPoint.x, localHitPoint.y);
                if (panelMesh != null && panelMesh.TryGetLocalPositionFromUV(raycaster.Hit.textureCoord, out Vector2 panelLocalPosition))
                {
                    localPosition = panelLocalPosition;
                }
                return new InputState(raycaster.ID, true, raycaster.MouseButton0(), raycaster.MouseButton1(), raycaster.MouseButton2(), raycaster.MouseWheel(), localPosition);
            }
        }

        /// <summary>
        /// Returns an input state without active hover or buttons.
        /// </summary>
        /// <returns>Inactive input state.</returns>
        private static InputState getInactiveInputState()
        {
            return new InputState(string.Empty, false, false, false, false, 0f, new Vector2(-1f, -1f));
        }

        /// <summary>
        /// Updates the value.
        /// </summary>
        public static void Update()
        {
            foreach (FuRaycaster raycaster in _raycasters.Values)
            {
                raycaster.Raycast();
            }
        }

        /// <summary>
        /// Returns the register raycaster result.
        /// </summary>
        /// <param name="raycaster">The raycaster value.</param>
        /// <returns>The result of the operation.</returns>
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

        /// <summary>
        /// Returns the un register raycaster result.
        /// </summary>
        /// <param name="raycasterName">The raycaster Name value.</param>
        /// <returns>The result of the operation.</returns>
        public static bool UnRegisterRaycaster(string raycasterName)
        {
            return _raycasters.Remove(raycasterName);
        }

        /// <summary>
        /// Attempts to get raycaster.
        /// </summary>
        /// <param name="raycasterName">The raycaster Name value.</param>
        /// <param name="raycaster">The raycaster value.</param>
        /// <returns>The result of the operation.</returns>
        public static bool TryGetRaycaster(string raycasterName, out FuRaycaster raycaster)
        {
            return _raycasters.TryGetValue(raycasterName, out raycaster);
        }

        /// <summary>
        /// Gets the all raycasters.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public static IEnumerable<FuRaycaster> GetAllRaycasters()
        {
            return _raycasters.Values;
        }
        #endregion
    }
}
