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
        private static Dictionary<string, FuRaycaster> latestRaycasters = new Dictionary<string, FuRaycaster>();
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

            FuRaycaster selectedRaycaster = getRaycasterForInput(containerID, raycastableGameObject);
            if (selectedRaycaster == null)
            {
                latestRaycasters.Remove(containerID);
                return getInactiveInputState();
            }

            latestRaycasters[containerID] = selectedRaycaster;
            Vector3 localHitPoint = raycastableGameObject.transform.InverseTransformPoint(selectedRaycaster.Hit.point);
            Vector2 localPosition = new Vector2(localHitPoint.x, localHitPoint.y);
            if (panelMesh != null && panelMesh.TryGetLocalPositionFromUV(selectedRaycaster.Hit.textureCoord, out Vector2 panelLocalPosition))
            {
                localPosition = panelLocalPosition;
            }
            return new InputState(selectedRaycaster.ID, true, selectedRaycaster.MouseButton0(), selectedRaycaster.MouseButton1(), selectedRaycaster.MouseButton2(), selectedRaycaster.MouseWheel(), localPosition);
        }

        /// <summary>
        /// Returns the raycaster that should drive input for a target this frame.
        /// </summary>
        /// <param name="containerID">Container or target input ID.</param>
        /// <param name="raycastableGameObject">Raycastable target.</param>
        /// <returns>The selected raycaster, or null.</returns>
        private static FuRaycaster getRaycasterForInput(string containerID, GameObject raycastableGameObject)
        {
            bool hasLatestRaycaster = latestRaycasters.TryGetValue(containerID, out FuRaycaster latestRaycaster) && latestRaycaster != null;
            bool latestRaycasterStillHits = false;
            FuRaycaster inputCandidate = null;
            FuRaycaster closestCandidate = null;
            float closestDistance = float.MaxValue;

            foreach (FuRaycaster raycaster in _raycasters.Values)
            {
                if (!raycasterHits(raycaster, raycastableGameObject))
                {
                    continue;
                }

                if (hasLatestRaycaster && latestRaycaster == raycaster)
                {
                    latestRaycasterStillHits = true;
                }

                if (inputCandidate == null && raycasterHasInput(raycaster))
                {
                    inputCandidate = raycaster;
                }

                float distance = raycaster.Hit.distance;
                if (closestCandidate == null || distance < closestDistance)
                {
                    closestCandidate = raycaster;
                    closestDistance = distance;
                }
            }

            if (hasLatestRaycaster && latestRaycasterStillHits && raycasterHasInput(latestRaycaster))
            {
                return latestRaycaster;
            }

            if (inputCandidate != null)
            {
                return inputCandidate;
            }

            if (hasLatestRaycaster && latestRaycasterStillHits)
            {
                return latestRaycaster;
            }

            return closestCandidate;
        }

        /// <summary>
        /// Returns whether the raycaster hit the requested target this frame.
        /// </summary>
        /// <param name="raycaster">Raycaster to inspect.</param>
        /// <param name="raycastableGameObject">Target object.</param>
        /// <returns>True if the raycaster hit the target.</returns>
        private static bool raycasterHits(FuRaycaster raycaster, GameObject raycastableGameObject)
        {
            return raycaster != null &&
                   raycaster.RaycastThisFrame &&
                   raycaster.Hit.collider != null &&
                   raycaster.Hit.collider.gameObject == raycastableGameObject;
        }

        /// <summary>
        /// Returns whether the raycaster currently carries input that should own the target.
        /// </summary>
        /// <param name="raycaster">Raycaster to inspect.</param>
        /// <returns>True if a button or wheel input is active.</returns>
        private static bool raycasterHasInput(FuRaycaster raycaster)
        {
            return raycaster != null &&
                   (raycaster.MouseButton0() ||
                    raycaster.MouseButton1() ||
                    raycaster.MouseButton2() ||
                    Mathf.Abs(raycaster.MouseWheel()) > Mathf.Epsilon);
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
            if (!_raycasters.TryGetValue(raycasterName, out FuRaycaster raycaster))
            {
                return false;
            }

            bool removed = _raycasters.Remove(raycasterName);
            if (removed)
            {
                removeLatestRaycasterReferences(raycaster);
            }

            return removed;
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

        /// <summary>
        /// Removes every cached latest-raycaster reference matching the given raycaster.
        /// </summary>
        /// <param name="raycaster">Raycaster to remove from caches.</param>
        private static void removeLatestRaycasterReferences(FuRaycaster raycaster)
        {
            List<string> containersToClear = new List<string>();
            foreach (KeyValuePair<string, FuRaycaster> cachedRaycaster in latestRaycasters)
            {
                if (cachedRaycaster.Value == raycaster)
                {
                    containersToClear.Add(cachedRaycaster.Key);
                }
            }

            foreach (string containerID in containersToClear)
            {
                latestRaycasters.Remove(containerID);
            }
        }
        #endregion
    }
}
