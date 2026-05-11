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

            FuRaycaster selectedRaycaster = getRaycasterForInput(containerID, raycastableGameObject, out RaycastHit selectedHit);
            if (selectedRaycaster == null)
            {
                latestRaycasters.Remove(containerID);
                return getInactiveInputState();
            }

            latestRaycasters[containerID] = selectedRaycaster;
            Vector3 localHitPoint = raycastableGameObject.transform.InverseTransformPoint(selectedHit.point);
            Vector2 localPosition = new Vector2(localHitPoint.x, localHitPoint.y);
            if (panelMesh != null && panelMesh.TryGetLocalPositionFromUV(selectedHit.textureCoord, out Vector2 panelLocalPosition))
            {
                localPosition = panelLocalPosition;
            }
            return new InputState(selectedRaycaster.ID, true, selectedRaycaster.GetMouseButton(0), selectedRaycaster.GetMouseButton(1), selectedRaycaster.GetMouseButton(2), selectedRaycaster.GetMouseWheelDelta(), localPosition);
        }

        /// <summary>
        /// Returns the raycaster that should drive input for a target this frame.
        /// </summary>
        /// <param name="containerID">Container or target input ID.</param>
        /// <param name="raycastableGameObject">Raycastable target.</param>
        /// <param name="selectedHit">The hit on the selected target.</param>
        /// <returns>The selected raycaster, or null.</returns>
        private static FuRaycaster getRaycasterForInput(string containerID, GameObject raycastableGameObject, out RaycastHit selectedHit)
        {
            selectedHit = default;
            bool hasLatestRaycaster = latestRaycasters.TryGetValue(containerID, out FuRaycaster latestRaycaster) && latestRaycaster != null;
            bool latestRaycasterStillHits = false;
            RaycastHit latestRaycasterHit = default;
            FuRaycaster inputCandidate = null;
            RaycastHit inputCandidateHit = default;
            float inputCandidateDistance = float.MaxValue;
            FuRaycaster closestCandidate = null;
            RaycastHit closestCandidateHit = default;
            float closestDistance = float.MaxValue;

            foreach (FuRaycaster raycaster in _raycasters.Values)
            {
                if (!tryGetRaycasterHit(raycaster, raycastableGameObject, out RaycastHit raycasterHit))
                {
                    continue;
                }

                raycaster.UpdateInputState();
                if (hasLatestRaycaster && latestRaycaster == raycaster)
                {
                    latestRaycasterStillHits = true;
                    latestRaycasterHit = raycasterHit;
                }

                if (raycasterHasInput(raycaster) &&
                    (inputCandidate == null || raycasterHit.distance < inputCandidateDistance))
                {
                    inputCandidate = raycaster;
                    inputCandidateHit = raycasterHit;
                    inputCandidateDistance = raycasterHit.distance;
                }

                float distance = raycasterHit.distance;
                if (closestCandidate == null || distance < closestDistance)
                {
                    closestCandidate = raycaster;
                    closestCandidateHit = raycasterHit;
                    closestDistance = distance;
                }
            }

            if (hasLatestRaycaster && latestRaycasterStillHits && raycasterHasInput(latestRaycaster))
            {
                selectedHit = latestRaycasterHit;
                return latestRaycaster;
            }

            if (inputCandidate != null)
            {
                selectedHit = inputCandidateHit;
                return inputCandidate;
            }

            if (hasLatestRaycaster && latestRaycasterStillHits)
            {
                selectedHit = latestRaycasterHit;
                return latestRaycaster;
            }

            selectedHit = closestCandidateHit;
            return closestCandidate;
        }

        /// <summary>
        /// Attempts to retrieve a raycaster hit for the requested target this frame.
        /// </summary>
        /// <param name="raycaster">Raycaster to inspect.</param>
        /// <param name="raycastableGameObject">Target object.</param>
        /// <param name="hit">Target hit.</param>
        /// <returns>True if the raycaster hit the target.</returns>
        private static bool tryGetRaycasterHit(FuRaycaster raycaster, GameObject raycastableGameObject, out RaycastHit hit)
        {
            hit = default;
            return raycaster != null &&
                   raycaster.TryGetHit(raycastableGameObject, out hit);
        }

        /// <summary>
        /// Returns whether the raycaster currently carries input that should own the target.
        /// </summary>
        /// <param name="raycaster">Raycaster to inspect.</param>
        /// <returns>True if a button or wheel input is active.</returns>
        private static bool raycasterHasInput(FuRaycaster raycaster)
        {
            return raycaster != null &&
                   (raycaster.GetMouseButton(0) ||
                    raycaster.GetMouseButton(1) ||
                    raycaster.GetMouseButton(2) ||
                    Mathf.Abs(raycaster.GetMouseWheelDelta()) > Mathf.Epsilon);
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
        /// Returns whether a raycaster mouse/controller button started being pressed this frame.
        /// </summary>
        /// <param name="raycasterName">Raycaster name.</param>
        /// <param name="button">Button to inspect.</param>
        /// <returns>True if the raycaster button went down this frame.</returns>
        public static bool IsMouseButtonDownThisFrame(string raycasterName, FuMouseButton button)
        {
            if (TryGetRaycaster(raycasterName, out FuRaycaster raycaster))
            {
                return raycaster.GetMouseButtonDownThisFrame((int)button);
            }

            return Fugui.TryGetBlockedFrameRawMouseDown(button, out bool rawMouseDown) && rawMouseDown;
        }

        /// <summary>
        /// Returns whether a raycaster mouse/controller button is currently pressed.
        /// </summary>
        /// <param name="raycasterName">Raycaster name.</param>
        /// <param name="button">Button to inspect.</param>
        /// <returns>True if the raycaster button is pressed.</returns>
        public static bool IsMouseButtonPressed(string raycasterName, FuMouseButton button)
        {
            if (TryGetRaycaster(raycasterName, out FuRaycaster raycaster))
            {
                return raycaster.GetMouseButton((int)button);
            }

            return Fugui.TryGetBlockedFrameRawMousePressed(button, out bool rawMousePressed) && rawMousePressed;
        }

        /// <summary>
        /// Returns whether a raycaster mouse/controller button was already pressed before this frame.
        /// </summary>
        /// <param name="raycasterName">Raycaster name.</param>
        /// <param name="button">Button to inspect.</param>
        /// <returns>True if the raycaster button is held from an earlier frame.</returns>
        public static bool IsMouseButtonPressedBeforeCurrentFrame(string raycasterName, FuMouseButton button)
        {
            if (TryGetRaycaster(raycasterName, out FuRaycaster raycaster))
            {
                return raycaster.IsMouseButtonPressedBeforeCurrentFrame((int)button);
            }

            return Fugui.IsMouseButtonPressedBeforeCurrentFrame(button);
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
