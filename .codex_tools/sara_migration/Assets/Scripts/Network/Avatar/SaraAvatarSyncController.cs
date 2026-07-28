using NetSquare.Client;
using NetSquare.Core;
using Saravr.Interaction;
using Saravr.Network.Common;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace Saravr.Network.Avatar
{
    /// <summary>
    /// Coordinates SARA avatar sync controller behavior in the Unity scene.
    /// </summary>
    public sealed class SaraAvatarSyncController : MonoBehaviour
    {
        [Header("Avatars")]
        [SerializeField] private GameObject remoteAvatarPrefab;
        [SerializeField] private GameObject localAvatarPrefab;
        [SerializeField] private bool hideLocalAvatar = true;
        [SerializeField] private bool showLocalAvatarInVR = false;

        [Header("Network")]
        [SerializeField, Min(0.01f)] private float networkSendInterval = 0.05f;
        [SerializeField, Min(0f)] private float interpolationTimeOffset = 0.12f;
        [SerializeField, Min(2)] private int maxBufferedPoses = 32;
        [SerializeField, Min(0f)] private float posePositionChangeThreshold = 0.01f;
        [SerializeField, Range(0f, 180f)] private float poseRotationChangeThreshold = 0.75f;
        [SerializeField, Min(0f)] private float posePointingChangeThreshold = 0.01f;
        [SerializeField, Min(0f)] private float maxSilentPoseInterval = 0f;

        [Header("VR Controller Actions")]
        [SerializeField] private Transform leftControllerTransform;
        [SerializeField] private Transform rightControllerTransform;
        [SerializeField] private InputActionProperty leftHandPositionAction;
        [SerializeField] private InputActionProperty leftHandRotationAction;
        [SerializeField] private InputActionProperty rightHandPositionAction;
        [SerializeField] private InputActionProperty rightHandRotationAction;
        [SerializeField] private InputActionProperty rightPointingAction;
        [SerializeField, Range(0f, 1f)] private float rightPointingPressThreshold = 0.5f;

        private readonly Dictionary<uint, RemoteAvatarState> remoteAvatars = new Dictionary<uint, RemoteAvatarState>();
        private readonly Dictionary<uint, SaraRemoteAvatar> avatarsByClientId = new Dictionary<uint, SaraRemoteAvatar>();
        private readonly Dictionary<uint, SeatType> clientSeatAssignments = new Dictionary<uint, SeatType>();
        private readonly List<uint> clientsToRemove = new List<uint>();

        private SaraRemoteAvatar localAvatar;
        private bool worldEventsRegistered;
        private uint localSequenceId;
        private float nextNetworkSendAt;
        private float lastPoseSentAt;
        private bool hasLastSentPose;
        private bool forceNextPoseSend = true;
        private SaraAvatarPoseFrame lastSentPose;
        private Transform resolvedLeftControllerTransform;
        private Transform resolvedRightControllerTransform;
        private XRRaycaster resolvedLeftRaycaster;
        private XRRaycaster resolvedRightRaycaster;

        #region Lifecycle
        /// <summary>
        /// Initializes cached references before the first frame.
        /// </summary>
        private void Awake()
        {
            SaraAvatarPoseFrame.RegisterDeserializer();
        }

        /// <summary>
        /// Subscribes to runtime events when the component is enabled.
        /// </summary>
        private void OnEnable()
        {
            SaraAvatarPoseFrame.RegisterDeserializer();
            NSClient.OnConnected += NSClient_OnConnected;
            NSClient.OnDisconnected += NSClient_OnDisconnected;
            Sara.Events.OnSessionUpdated += Events_OnSessionUpdated;
            Sara.Events.OnRemoteAvatarsUpdateRequested += Events_OnRemoteAvatarsUpdateRequested;
            Sara.Events.OnLocalAvatarUpdateRequested += Events_OnLocalAvatarUpdateRequested;

            ResetLocalPoseSendState();
            EnableVRInputActions();
            RegisterWorldEvents();
        }

        /// <summary>
        /// Unsubscribes from runtime events when the component is disabled.
        /// </summary>
        private void OnDisable()
        {
            NSClient.OnConnected -= NSClient_OnConnected;
            NSClient.OnDisconnected -= NSClient_OnDisconnected;
            Sara.Events.OnSessionUpdated -= Events_OnSessionUpdated;
            Sara.Events.OnRemoteAvatarsUpdateRequested -= Events_OnRemoteAvatarsUpdateRequested;
            Sara.Events.OnLocalAvatarUpdateRequested -= Events_OnLocalAvatarUpdateRequested;
            DisableVRInputActions();
            UnregisterWorldEvents();
            ClearLocalAvatar();
            ClearRemoteAvatars();
            clientSeatAssignments.Clear();
            ResetLocalPoseSendState();
        }

        #endregion

        #region Network Registration
        /// <summary>
        /// Handles the NS client connected event.
        /// </summary>
        private void NSClient_OnConnected(uint clientId)
        {
            ForceNextLocalPoseSend();
            RegisterWorldEvents();
        }

        /// <summary>
        /// Handles the NS client disconnected event.
        /// </summary>
        /// <param name="info">The structured NetSquare disconnection information.</param>
        private void NSClient_OnDisconnected(DisconnectInfo info)
        {
            UnregisterWorldEvents();
            ClearLocalAvatar();
            ClearRemoteAvatars();
            clientSeatAssignments.Clear();
            ResetLocalPoseSendState();
        }

        /// <summary>
        /// Registers the world events instance.
        /// </summary>
        private void RegisterWorldEvents()
        {
            if (worldEventsRegistered || NSClient.Client == null)
                return;

            NSClient.Client.WorldsManager.OnClientJoinWorld += WorldsManager_OnClientJoinWorld;
            NSClient.Client.WorldsManager.OnClientLeaveWorld += WorldsManager_OnClientLeaveWorld;
            NSClient.Client.WorldsManager.OnReceiveSynchFrames += WorldsManager_OnReceiveSynchFrames;
            NSClient.Client.WorldsManager.OnSynchronize += WorldsManager_OnSynchronize;
            worldEventsRegistered = true;
        }

        /// <summary>
        /// Unregisters the world events instance.
        /// </summary>
        private void UnregisterWorldEvents()
        {
            if (!worldEventsRegistered || NSClient.Client == null)
            {
                worldEventsRegistered = false;
                return;
            }

            NSClient.Client.WorldsManager.OnClientJoinWorld -= WorldsManager_OnClientJoinWorld;
            NSClient.Client.WorldsManager.OnClientLeaveWorld -= WorldsManager_OnClientLeaveWorld;
            NSClient.Client.WorldsManager.OnReceiveSynchFrames -= WorldsManager_OnReceiveSynchFrames;
            NSClient.Client.WorldsManager.OnSynchronize -= WorldsManager_OnSynchronize;
            worldEventsRegistered = false;
        }
        #endregion

        #region Local Pose Capture
        /// <summary>
        /// Handles the centralized local avatar update phase.
        /// </summary>
        private void Events_OnLocalAvatarUpdateRequested()
        {
            RegisterWorldEvents();
            UpdateLocalPoseSender();
        }

        /// <summary>
        /// Updates the local pose sender state.
        /// </summary>
        private void UpdateLocalPoseSender()
        {
            if (!CanSendLocalPose())
            {
                ClearLocalAvatar();
                ResetLocalPoseSendState();
                return;
            }

            if (!TryBuildLocalPose(out SaraAvatarPoseFrame pose))
            {
                ClearLocalAvatar();
                ResetLocalPoseSendState();
                return;
            }

            float now = Time.unscaledTime;
            UpdateLocalAvatar(pose);

            if (now < nextNetworkSendAt || !ShouldSendLocalPose(pose, now))
                return;

            pose.Time = NSClient.ServerTime;
            pose.SequenceID = ++localSequenceId;
            NSClient.Client.WorldsManager.StoreSynchFrame(new SaraAvatarPoseFrame(pose));
            NSClient.Client.WorldsManager.SendFrames();

            lastSentPose = new SaraAvatarPoseFrame(pose);
            hasLastSentPose = true;
            forceNextPoseSend = false;
            lastPoseSentAt = now;
            nextNetworkSendAt = now + networkSendInterval;
        }

        /// <summary>
        /// Returns whether the send local pose action is allowed.
        /// </summary>
        private bool CanSendLocalPose()
        {
            return Sara.CurrentSession != null
                && Sara.CurrentSession.IsMultiplayer
                && Sara.Network != null
                && Sara.Network.LocalUser != null
                && Sara.Network.LocalUser.HasAvatar
                && Sara.Network.HasSelectedSeat
                && NSClient.IsConnected
                && NSClient.Client != null
                && NSClient.Client.WorldsManager.IsInWorld;
        }

        /// <summary>
        /// Updates the local avatar state.
        /// </summary>
        private void UpdateLocalAvatar(SaraAvatarPoseFrame pose)
        {
            if (!showLocalAvatarInVR || !Sara.IsVR)
            {
                ClearLocalAvatar();
                return;
            }

            SaraRemoteAvatar avatar = EnsureLocalAvatar();
            if (avatar != null)
                avatar.ApplyPose(pose);
        }

        /// <summary>
        /// Returns whether the local pose should be sent to the server.
        /// </summary>
        private bool ShouldSendLocalPose(SaraAvatarPoseFrame pose, float now)
        {
            if (pose == null)
                return false;

            if (forceNextPoseSend || !hasLastSentPose || lastSentPose == null)
                return true;

            if (maxSilentPoseInterval > 0f && now - lastPoseSentAt >= maxSilentPoseInterval)
                return true;

            return HasPoseChanged(lastSentPose, pose);
        }

        /// <summary>
        /// Forces the next valid local pose to be sent.
        /// </summary>
        private void ForceNextLocalPoseSend()
        {
            forceNextPoseSend = true;
            nextNetworkSendAt = 0f;
        }

        /// <summary>
        /// Clears cached local pose send state.
        /// </summary>
        private void ResetLocalPoseSendState()
        {
            lastSentPose = null;
            hasLastSentPose = false;
            forceNextPoseSend = true;
            lastPoseSentAt = 0f;
            nextNetworkSendAt = 0f;
        }

        /// <summary>
        /// Returns whether two avatar poses differ enough to require network sync.
        /// </summary>
        private bool HasPoseChanged(SaraAvatarPoseFrame previous, SaraAvatarPoseFrame current)
        {
            if (previous.DeviceKind != current.DeviceKind
                || previous.Seat != current.Seat
                || previous.HasLeftHand != current.HasLeftHand
                || previous.HasRightHand != current.HasRightHand
                || previous.IsRightPointing != current.IsRightPointing
                || previous.HasRightPointingHit != current.HasRightPointingHit)
            {
                return true;
            }

            if (HasPoseTargetChanged(
                previous.HeadX, previous.HeadY, previous.HeadZ, previous.HeadRX, previous.HeadRY, previous.HeadRZ, previous.HeadRW,
                current.HeadX, current.HeadY, current.HeadZ, current.HeadRX, current.HeadRY, current.HeadRZ, current.HeadRW))
            {
                return true;
            }

            if (current.HasLeftHand && HasPoseTargetChanged(
                previous.LeftHandX, previous.LeftHandY, previous.LeftHandZ, previous.LeftHandRX, previous.LeftHandRY, previous.LeftHandRZ, previous.LeftHandRW,
                current.LeftHandX, current.LeftHandY, current.LeftHandZ, current.LeftHandRX, current.LeftHandRY, current.LeftHandRZ, current.LeftHandRW))
            {
                return true;
            }

            if (current.HasRightHand && HasPoseTargetChanged(
                previous.RightHandX, previous.RightHandY, previous.RightHandZ, previous.RightHandRX, previous.RightHandRY, previous.RightHandRZ, previous.RightHandRW,
                current.RightHandX, current.RightHandY, current.RightHandZ, current.RightHandRX, current.RightHandRY, current.RightHandRZ, current.RightHandRW))
            {
                return true;
            }

            return current.IsRightPointing && HasPointingChanged(previous, current);
        }

        /// <summary>
        /// Returns whether one pose target changed beyond configured thresholds.
        /// </summary>
        private bool HasPoseTargetChanged(
            float previousX, float previousY, float previousZ,
            float previousRX, float previousRY, float previousRZ, float previousRW,
            float currentX, float currentY, float currentZ,
            float currentRX, float currentRY, float currentRZ, float currentRW)
        {
            return HasVectorChanged(previousX, previousY, previousZ, currentX, currentY, currentZ, posePositionChangeThreshold)
                || HasRotationChanged(previousRX, previousRY, previousRZ, previousRW, currentRX, currentRY, currentRZ, currentRW);
        }

        /// <summary>
        /// Returns whether the pointing segment changed beyond configured thresholds.
        /// </summary>
        private bool HasPointingChanged(SaraAvatarPoseFrame previous, SaraAvatarPoseFrame current)
        {
            return HasVectorChanged(
                    previous.PointingOriginX, previous.PointingOriginY, previous.PointingOriginZ,
                    current.PointingOriginX, current.PointingOriginY, current.PointingOriginZ,
                    posePointingChangeThreshold)
                || HasVectorChanged(
                    previous.PointingEndX, previous.PointingEndY, previous.PointingEndZ,
                    current.PointingEndX, current.PointingEndY, current.PointingEndZ,
                    posePointingChangeThreshold);
        }

        /// <summary>
        /// Returns whether a vector changed beyond the given threshold.
        /// </summary>
        private static bool HasVectorChanged(
            float previousX, float previousY, float previousZ,
            float currentX, float currentY, float currentZ,
            float threshold)
        {
            Vector3 previous = new Vector3(previousX, previousY, previousZ);
            Vector3 current = new Vector3(currentX, currentY, currentZ);
            float clampedThreshold = Mathf.Max(0f, threshold);
            return (current - previous).sqrMagnitude > clampedThreshold * clampedThreshold;
        }

        /// <summary>
        /// Returns whether a rotation changed beyond the configured threshold.
        /// </summary>
        private bool HasRotationChanged(
            float previousX, float previousY, float previousZ, float previousW,
            float currentX, float currentY, float currentZ, float currentW)
        {
            Quaternion previous = Normalize(new Quaternion(previousX, previousY, previousZ, previousW));
            Quaternion current = Normalize(new Quaternion(currentX, currentY, currentZ, currentW));
            return Quaternion.Angle(previous, current) > Mathf.Max(0f, poseRotationChangeThreshold);
        }

        /// <summary>
        /// Normalizes a quaternion with identity fallback for empty values.
        /// </summary>
        private static Quaternion Normalize(Quaternion rotation)
        {
            float magnitudeSq =
                rotation.x * rotation.x
                + rotation.y * rotation.y
                + rotation.z * rotation.z
                + rotation.w * rotation.w;

            if (magnitudeSq <= Mathf.Epsilon)
                return Quaternion.identity;

            float inverseMagnitude = 1f / Mathf.Sqrt(magnitudeSq);
            return new Quaternion(
                rotation.x * inverseMagnitude,
                rotation.y * inverseMagnitude,
                rotation.z * inverseMagnitude,
                rotation.w * inverseMagnitude);
        }

        /// <summary>
        /// Runs the try build local pose logic.
        /// </summary>
        private bool TryBuildLocalPose(out SaraAvatarPoseFrame pose)
        {
            pose = null;

            if (Sara.Cameras == null || Sara.Cameras.CurrentCamera == null || Sara.Network == null)
                return false;

            Transform head = Sara.Cameras.CurrentCamera.transform;
            pose = new SaraAvatarPoseFrame
            {
                Time = NSClient.ServerTime,
                DeviceKind = Sara.IsVR ? SaraAvatarDeviceKind.VR : SaraAvatarDeviceKind.Flat,
                Seat = Sara.Network.LocalSeat
            };

            SetPoseFromWorld(head.position, head.rotation, pose, AvatarPoseTarget.Head);

            if (Sara.IsVR)
            {
                if (TryGetVRControllerPose(
                    XRControllerType.Left,
                    XRNode.LeftHand,
                    leftHandPositionAction,
                    leftHandRotationAction,
                    leftControllerTransform,
                    out Vector3 leftHandPosition,
                    out Quaternion leftHandRotation))
                {
                    pose.HasLeftHand = true;
                    SetPoseFromWorld(leftHandPosition, leftHandRotation, pose, AvatarPoseTarget.LeftHand);
                }

                if (TryGetVRControllerPose(
                    XRControllerType.Right,
                    XRNode.RightHand,
                    rightHandPositionAction,
                    rightHandRotationAction,
                    rightControllerTransform,
                    out Vector3 rightHandPosition,
                    out Quaternion rightHandRotation))
                {
                    pose.HasRightHand = true;
                    SetPoseFromWorld(rightHandPosition, rightHandRotation, pose, AvatarPoseTarget.RightHand);

                    if (IsRightPointingPressed() && TryBuildVRPointingSegment(out Vector3 origin, out Vector3 end, out bool hasHit))
                        SetPointingSegmentFromWorld(origin, end, hasHit, pose);
                }
            }
            else if (TryBuildFlatPointingSegment(out Vector3 origin, out Vector3 end, out bool hasHit))
            {
                pose.HasRightHand = true;
                SetPointingSegmentFromWorld(origin, end, hasHit, pose);
            }

            return true;
        }

        /// <summary>
        /// Attempts to resolve the VR controller pose value.
        /// </summary>
        private bool TryGetVRControllerPose(
            XRControllerType controllerType,
            XRNode fallbackNode,
            InputActionProperty positionAction,
            InputActionProperty rotationAction,
            Transform assignedTransform,
            out Vector3 worldPosition,
            out Quaternion worldRotation)
        {
            if (TryGetActionPose(positionAction, rotationAction, out worldPosition, out worldRotation))
                return true;

            if (TryGetControllerTransformPose(controllerType, assignedTransform, out worldPosition, out worldRotation))
                return true;

            return TryGetXRNodePose(fallbackNode, out worldPosition, out worldRotation);
        }

        /// <summary>
        /// Attempts to resolve the action pose value.
        /// </summary>
        private static bool TryGetActionPose(
            InputActionProperty positionAction,
            InputActionProperty rotationAction,
            out Vector3 worldPosition,
            out Quaternion worldRotation)
        {
            worldPosition = Vector3.zero;
            worldRotation = Quaternion.identity;

            InputAction positionInput = positionAction.action;
            InputAction rotationInput = rotationAction.action;
            if (positionInput == null || rotationInput == null)
                return false;

            if (positionInput.activeControl == null && rotationInput.activeControl == null)
                return false;

            try
            {
                Vector3 localPosition = positionInput.ReadValue<Vector3>();
                Quaternion localRotation = rotationInput.ReadValue<Quaternion>();
                ConvertTrackedLocalPoseToWorld(localPosition, localRotation, out worldPosition, out worldRotation);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to resolve the controller transform pose value.
        /// </summary>
        private bool TryGetControllerTransformPose(
            XRControllerType controllerType,
            Transform assignedTransform,
            out Vector3 worldPosition,
            out Quaternion worldRotation)
        {
            worldPosition = Vector3.zero;
            worldRotation = Quaternion.identity;

            Transform controllerTransform = assignedTransform != null
                ? assignedTransform
                : ResolveControllerTransform(controllerType);

            if (controllerTransform == null)
                return false;

            worldPosition = controllerTransform.position;
            worldRotation = controllerTransform.rotation;
            return true;
        }

        /// <summary>
        /// Resolves the controller transform reference or value.
        /// </summary>
        private Transform ResolveControllerTransform(XRControllerType controllerType)
        {
            if (controllerType == XRControllerType.Left)
            {
                if (resolvedLeftControllerTransform == null)
                    resolvedLeftControllerTransform = ResolveRaycasterTransform(XRControllerType.Left);

                return resolvedLeftControllerTransform;
            }

            if (resolvedRightControllerTransform == null)
                resolvedRightControllerTransform = ResolveRaycasterTransform(XRControllerType.Right);

            return resolvedRightControllerTransform;
        }

        /// <summary>
        /// Resolves the raycaster transform reference or value.
        /// </summary>
        private Transform ResolveRaycasterTransform(XRControllerType controllerType)
        {
            XRRaycaster raycaster = ResolveRaycaster(controllerType);
            return raycaster != null ? raycaster.transform : null;
        }

        /// <summary>
        /// Resolves the raycaster reference or value.
        /// </summary>
        private XRRaycaster ResolveRaycaster(XRControllerType controllerType)
        {
            if (controllerType == XRControllerType.Left && resolvedLeftRaycaster != null)
                return resolvedLeftRaycaster;

            if (controllerType == XRControllerType.Right && resolvedRightRaycaster != null)
                return resolvedRightRaycaster;

#if UNITY_2023_1_OR_NEWER
            XRRaycaster[] raycasters = FindObjectsByType<XRRaycaster>(FindObjectsInactive.Exclude);
#else
            XRRaycaster[] raycasters = FindObjectsOfType<XRRaycaster>();
#endif
            for (int i = 0; i < raycasters.Length; i++)
            {
                XRRaycaster raycaster = raycasters[i];
                if (raycaster == null || raycaster.controllerType != controllerType)
                    continue;

                if (controllerType == XRControllerType.Left)
                    resolvedLeftRaycaster = raycaster;
                else if (controllerType == XRControllerType.Right)
                    resolvedRightRaycaster = raycaster;

                return raycaster;
            }

            return null;
        }

        /// <summary>
        /// Returns whether the right pointing pressed condition is met.
        /// </summary>
        private bool IsRightPointingPressed()
        {
            if (IsActionPressed(rightPointingAction))
                return true;

            XRRaycaster raycaster = ResolveRaycaster(XRControllerType.Right);
            return raycaster != null && IsActionPressed(raycaster.ClickAction);
        }

        /// <summary>
        /// Returns whether the action pressed condition is met.
        /// </summary>
        private bool IsActionPressed(InputActionProperty actionProperty)
        {
            InputAction action = actionProperty.action;
            if (action == null)
                return false;

            try
            {
                if (action.IsPressed())
                    return true;

                if (action.expectedControlType == "Axis" || action.expectedControlType == "Button")
                    return action.ReadValue<float>() >= rightPointingPressThreshold;
            }
            catch (InvalidOperationException)
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// Attempts to resolve the XR node pose value.
        /// </summary>
        private bool TryGetXRNodePose(XRNode node, out Vector3 worldPosition, out Quaternion worldRotation)
        {
            worldPosition = Vector3.zero;
            worldRotation = Quaternion.identity;

            UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(node);
            if (!device.isValid)
                return false;

            bool hasPosition = device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out Vector3 localPosition);
            bool hasRotation = device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out Quaternion localRotation);

            if (!hasPosition || !hasRotation)
                return false;

            ConvertTrackedLocalPoseToWorld(localPosition, localRotation, out worldPosition, out worldRotation);
            return true;
        }

        /// <summary>
        /// Runs the convert tracked local pose to world logic.
        /// </summary>
        private static void ConvertTrackedLocalPoseToWorld(
            Vector3 localPosition,
            Quaternion localRotation,
            out Vector3 worldPosition,
            out Quaternion worldRotation)
        {
            Transform xrRoot = Sara.Cameras != null && Sara.Cameras.CurrentCameraRoot != null
                ? Sara.Cameras.CurrentCameraRoot.transform
                : null;

            if (xrRoot != null)
            {
                worldPosition = xrRoot.TransformPoint(localPosition);
                worldRotation = xrRoot.rotation * localRotation;
            }
            else
            {
                worldPosition = localPosition;
                worldRotation = localRotation;
            }
        }

        /// <summary>
        /// Sets the pose from world value.
        /// </summary>
        private static void SetPoseFromWorld(Vector3 worldPosition, Quaternion worldRotation, SaraAvatarPoseFrame pose, AvatarPoseTarget target)
        {
            Transform reference = Sara.Aircraft;
            Vector3 localPosition = reference != null ? reference.InverseTransformPoint(worldPosition) : worldPosition;
            Quaternion localRotation = reference != null ? Quaternion.Inverse(reference.rotation) * worldRotation : worldRotation;

            switch (target)
            {
                case AvatarPoseTarget.Head:
                    pose.HeadX = localPosition.x;
                    pose.HeadY = localPosition.y;
                    pose.HeadZ = localPosition.z;
                    pose.HeadRX = localRotation.x;
                    pose.HeadRY = localRotation.y;
                    pose.HeadRZ = localRotation.z;
                    pose.HeadRW = localRotation.w;
                    break;
                case AvatarPoseTarget.LeftHand:
                    pose.LeftHandX = localPosition.x;
                    pose.LeftHandY = localPosition.y;
                    pose.LeftHandZ = localPosition.z;
                    pose.LeftHandRX = localRotation.x;
                    pose.LeftHandRY = localRotation.y;
                    pose.LeftHandRZ = localRotation.z;
                    pose.LeftHandRW = localRotation.w;
                    break;
                case AvatarPoseTarget.RightHand:
                    pose.RightHandX = localPosition.x;
                    pose.RightHandY = localPosition.y;
                    pose.RightHandZ = localPosition.z;
                    pose.RightHandRX = localRotation.x;
                    pose.RightHandRY = localRotation.y;
                    pose.RightHandRZ = localRotation.z;
                    pose.RightHandRW = localRotation.w;
                    break;
            }
        }


        #endregion

        #region Remote Avatar Sync
        /// <summary>
        /// Handles the centralized remote avatar update phase.
        /// </summary>
        private void Events_OnRemoteAvatarsUpdateRequested()
        {
            // Remote poses are Aircraft-local, so apply them after Aircraft has reached this frame's transform.
            UpdateRemoteAvatars();
        }

        /// <summary>
        /// Updates the remote avatars state.
        /// </summary>
        private void UpdateRemoteAvatars()
        {
            float poseTime = NSClient.ServerTime - interpolationTimeOffset;
            foreach (RemoteAvatarState state in remoteAvatars.Values)
            {
                if (state.Buffer.TryGetPose(poseTime, out SaraAvatarPoseFrame pose))
                    state.Avatar.ApplyPose(pose);
            }
        }

        /// <summary>
        /// Handles the worlds manager client join world event.
        /// </summary>
        private void WorldsManager_OnClientJoinWorld(uint clientId, NetsquareTransformFrame transformFrame, NetworkMessage message)
        {
            if (clientId != 0 && clientId != NSClient.ClientID)
                ForceNextLocalPoseSend();

            if (ShouldIgnoreRemoteClient(clientId))
                return;

            EnsureRemoteAvatar(clientId);
        }

        /// <summary>
        /// Handles the worlds manager client leave world event.
        /// </summary>
        private void WorldsManager_OnClientLeaveWorld(uint clientId)
        {
            clientSeatAssignments.Remove(clientId);
            RemoveRemoteAvatar(clientId);
        }

        /// <summary>
        /// Handles the worlds manager synchronize event.
        /// </summary>
        private void WorldsManager_OnSynchronize(NetworkMessage message)
        {
            if (!SaraAvatarSeatAssignmentMessage.TryRead(message, out uint clientId, out SeatType seat))
                return;

            ApplySeatAssignment(clientId, seat, true);
        }

        /// <summary>
        /// Handles the worlds manager receive synch frames event.
        /// </summary>
        private void WorldsManager_OnReceiveSynchFrames(uint clientId, INetSquareSynchFrame[] frames)
        {
            if (clientId == 0 || (hideLocalAvatar && clientId == NSClient.ClientID) || frames == null)
                return;

            RemoteAvatarState state = null;
            for (int i = 0; i < frames.Length; i++)
            {
                SaraAvatarPoseFrame poseFrame = frames[i] as SaraAvatarPoseFrame;
                if (poseFrame == null)
                    continue;

                CacheSeatAssignment(clientId, poseFrame.Seat);

                if (ShouldIgnoreRemoteClient(clientId))
                    continue;

                state = state ?? EnsureRemoteAvatar(clientId);
                state.Buffer.MaxFrames = maxBufferedPoses;
                state.Buffer.Add(poseFrame);
            }
        }

        /// <summary>
        /// Runs the ensure remote avatar logic.
        /// </summary>
        private RemoteAvatarState EnsureRemoteAvatar(uint clientId)
        {
            if (remoteAvatars.TryGetValue(clientId, out RemoteAvatarState state))
                return state;

            SaraRemoteAvatar avatar = CreateRemoteAvatar(clientId);
            state = new RemoteAvatarState(avatar, maxBufferedPoses);
            remoteAvatars.Add(clientId, state);
            avatarsByClientId[clientId] = avatar;

            if (TryGetSeatForClient(clientId, out SeatType seat))
                avatar.SetSeat(seat);

            RefreshAvatarDisplayName(avatar, clientId);
            return state;
        }

        /// <summary>
        /// Creates the remote avatar instance or data.
        /// </summary>
        private SaraRemoteAvatar CreateRemoteAvatar(uint clientId)
        {
            return CreateAvatar(remoteAvatarPrefab, clientId, "Sara Remote Avatar " + clientId, true);
        }

        /// <summary>
        /// Creates the avatar instance or data.
        /// </summary>
        private SaraRemoteAvatar CreateAvatar(GameObject prefab, uint clientId, string avatarName, bool allowFallback)
        {
            SaraRemoteAvatar avatar;
            if (prefab != null)
            {
                GameObject instance = Instantiate(prefab);
                avatar = instance.GetComponent<SaraRemoteAvatar>();
                if (avatar == null)
                    avatar = instance.AddComponent<SaraRemoteAvatar>();
                avatar.Initialize(clientId);
            }
            else if (allowFallback)
            {
                avatar = SaraRemoteAvatar.CreateFallback(clientId);
            }
            else
            {
                return null;
            }

            avatar.gameObject.name = avatarName;

            if (Sara.Aircraft != null)
                avatar.transform.SetParent(Sara.Aircraft, true);

            return avatar;
        }

        #endregion

        #region Seat And Name Mapping
        /// <summary>
        /// Handles the session updated event.
        /// </summary>
        private void Events_OnSessionUpdated(SaraSession session)
        {
            if (session == null || !session.IsMultiplayer || session.Seats == null)
            {
                ClearLocalAvatar();
                ClearRemoteAvatars();
                clientSeatAssignments.Clear();
                ResetLocalPoseSendState();
                return;
            }

            CacheSessionSeatAssignments(session);
            ForceNextLocalPoseSend();

            clientsToRemove.Clear();
            foreach (KeyValuePair<uint, RemoteAvatarState> pair in remoteAvatars)
            {
                if (session.Seats.TryGetSeatTypeByClientID(pair.Key, out SeatType seat))
                {
                    ApplySeatAssignment(pair.Key, seat, false);
                    RefreshAvatarDisplayName(pair.Value.Avatar, pair.Key);
                }
                else
                {
                    clientSeatAssignments.Remove(pair.Key);
                    clientsToRemove.Add(pair.Key);
                }
            }

            for (int i = 0; i < clientsToRemove.Count; i++)
                RemoveRemoteAvatar(clientsToRemove[i]);

            clientsToRemove.Clear();

            if (Sara.Network != null && Sara.Network.HasSelectedSeat && localAvatar != null)
            {
                localAvatar.SetSeat(Sara.Network.LocalSeat);
                RefreshAvatarDisplayName(localAvatar, NSClient.ClientID);
            }
        }

        /// <summary>
        /// Applies the seat assignment state.
        /// </summary>
        private void ApplySeatAssignment(uint clientId, SeatType seat, bool createAvatarIfNeeded)
        {
            if (clientId == 0 || !Seats.IsValidSeat(seat))
                return;

            CacheSeatAssignment(clientId, seat);

            if (clientId == NSClient.ClientID)
                ForceNextLocalPoseSend();

            if (avatarsByClientId.TryGetValue(clientId, out SaraRemoteAvatar avatar) && avatar != null)
            {
                avatar.SetSeat(seat);
                return;
            }

            if (!createAvatarIfNeeded || ShouldIgnoreRemoteClient(clientId))
                return;

            EnsureRemoteAvatar(clientId).Avatar.SetSeat(seat);
        }

        /// <summary>
        /// Runs the cache seat assignment logic.
        /// </summary>
        private void CacheSeatAssignment(uint clientId, SeatType seat)
        {
            if (clientId == 0 || !Seats.IsValidSeat(seat))
                return;

            clientSeatAssignments[clientId] = seat;
        }

        /// <summary>
        /// Runs the cache session seat assignments logic.
        /// </summary>
        private void CacheSessionSeatAssignments(SaraSession session)
        {
            if (session == null || session.Seats == null)
                return;

            CacheSessionSeatAssignment(session, SeatType.Pilot);
            CacheSessionSeatAssignment(session, SeatType.Center);
            CacheSessionSeatAssignment(session, SeatType.CoPilot);
        }

        /// <summary>
        /// Runs the cache session seat assignment logic.
        /// </summary>
        private void CacheSessionSeatAssignment(SaraSession session, SeatType seat)
        {
            Seat sessionSeat = session.Seats[seat];
            if (sessionSeat == null || !sessionSeat.IsOccupied)
                return;

            CacheSeatAssignment(sessionSeat.OccupiedByClientID, seat);
        }

        /// <summary>
        /// Attempts to resolve the seat for client value.
        /// </summary>
        private bool TryGetSeatForClient(uint clientId, out SeatType seat)
        {
            if (clientSeatAssignments.TryGetValue(clientId, out seat))
                return true;

            seat = SeatType.Pilot;
            return Sara.CurrentSession != null
                && Sara.CurrentSession.Seats != null
                && Sara.CurrentSession.Seats.TryGetSeatTypeByClientID(clientId, out seat);
        }

        /// <summary>
        /// Returns whether the ignore remote client path should run.
        /// </summary>
        private bool ShouldIgnoreRemoteClient(uint clientId)
        {
            if (clientId == 0 || (hideLocalAvatar && clientId == NSClient.ClientID))
                return true;

            return !clientSeatAssignments.ContainsKey(clientId)
                && (Sara.CurrentSession == null
                    || Sara.CurrentSession.Seats == null
                    || !Sara.CurrentSession.Seats.ContainsClientID(clientId));
        }

        /// <summary>
        /// Runs the ensure local avatar logic.
        /// </summary>
        private SaraRemoteAvatar EnsureLocalAvatar()
        {
            if (localAvatar != null)
                return localAvatar;

            localAvatar = CreateAvatar(localAvatarPrefab, NSClient.ClientID, "Sara Local Avatar", false);
            if (localAvatar != null)
            {
                avatarsByClientId[NSClient.ClientID] = localAvatar;
                if (TryGetSeatForClient(NSClient.ClientID, out SeatType seat))
                    localAvatar.SetSeat(seat);

                RefreshAvatarDisplayName(localAvatar, NSClient.ClientID);
            }

            return localAvatar;
        }

        /// <summary>
        /// Runs the refresh avatar display name logic.
        /// </summary>
        private void RefreshAvatarDisplayName(SaraRemoteAvatar avatar, uint clientId)
        {
            if (avatar == null)
                return;

            avatar.SetDisplayName(GetDisplayNameForClient(clientId));
        }

        /// <summary>
        /// Returns the display name for client value.
        /// </summary>
        private static string GetDisplayNameForClient(uint clientId)
        {
            if (Sara.Network == null)
                return null;

            if (clientId == NSClient.ClientID
                && Sara.Network.LocalUser != null
                && !string.IsNullOrWhiteSpace(Sara.Network.LocalUser.Name))
            {
                return Sara.Network.LocalUser.Name;
            }

            if (Sara.Network.TryGetUser(clientId, out SaraUser user)
                && user != null
                && !string.IsNullOrWhiteSpace(user.Name))
            {
                return user.Name;
            }

            return null;
        }


        /// <summary>
        /// Runs the try build VR pointing segment logic.
        /// </summary>
        private bool TryBuildVRPointingSegment(out Vector3 origin, out Vector3 end, out bool hasHit)
        {
            XRRaycaster raycaster = ResolveRaycaster(XRControllerType.Right);
            return TryBuildPointingSegment(raycaster, out origin, out end, out hasHit);
        }

        /// <summary>
        /// Runs the try build flat pointing segment logic.
        /// </summary>
        private static bool TryBuildFlatPointingSegment(out Vector3 origin, out Vector3 end, out bool hasHit)
        {
            return TryBuildPointingSegment(FlatRaycaster.Current, out origin, out end, out hasHit);
        }

        /// <summary>
        /// Runs the try build pointing segment logic.
        /// </summary>
        private static bool TryBuildPointingSegment(IInteractionRaycaster raycaster, out Vector3 origin, out Vector3 end, out bool hasHit)
        {
            origin = default;
            end = default;
            hasHit = false;

            return raycaster != null
                && raycaster.TryGetInteractionRay(out Ray ray)
                && InteractionManager.TryGetVisualSegment(raycaster, ray, out origin, out end, out hasHit);
        }

        #endregion

        #region Input Actions And Cleanup
        /// <summary>
        /// Runs the enable VR input actions logic.
        /// </summary>
        private void EnableVRInputActions()
        {
            EnableAction(leftHandPositionAction);
            EnableAction(leftHandRotationAction);
            EnableAction(rightHandPositionAction);
            EnableAction(rightHandRotationAction);
            EnableAction(rightPointingAction);
        }

        /// <summary>
        /// Runs the disable VR input actions logic.
        /// </summary>
        private void DisableVRInputActions()
        {
            DisableAction(leftHandPositionAction);
            DisableAction(leftHandRotationAction);
            DisableAction(rightHandPositionAction);
            DisableAction(rightHandRotationAction);
            DisableAction(rightPointingAction);
        }

        /// <summary>
        /// Runs the enable action logic.
        /// </summary>
        private static void EnableAction(InputActionProperty actionProperty)
        {
            actionProperty.action?.Enable();
        }

        /// <summary>
        /// Runs the disable action logic.
        /// </summary>
        private static void DisableAction(InputActionProperty actionProperty)
        {
            actionProperty.action?.Disable();
        }


        /// <summary>
        /// Runs the remove remote avatar logic.
        /// </summary>
        private void RemoveRemoteAvatar(uint clientId)
        {
            if (!remoteAvatars.TryGetValue(clientId, out RemoteAvatarState state))
                return;

            remoteAvatars.Remove(clientId);
            avatarsByClientId.Remove(clientId);
            if (state.Avatar != null)
                Destroy(state.Avatar.gameObject);
        }

        /// <summary>
        /// Clears the remote avatars state.
        /// </summary>
        private void ClearRemoteAvatars()
        {
            foreach (RemoteAvatarState state in remoteAvatars.Values)
            {
                if (state.Avatar != null)
                    Destroy(state.Avatar.gameObject);
            }

            remoteAvatars.Clear();
            avatarsByClientId.Clear();
            if (localAvatar != null)
                avatarsByClientId[NSClient.ClientID] = localAvatar;
        }

        /// <summary>
        /// Clears the local avatar state.
        /// </summary>
        private void ClearLocalAvatar()
        {
            if (localAvatar == null)
                return;

            avatarsByClientId.Remove(NSClient.ClientID);
            Destroy(localAvatar.gameObject);
            localAvatar = null;
        }

        #endregion

        #region Pose Conversion Helpers
        /// <summary>
        /// Sets the pointing segment from world value.
        /// </summary>
        private static void SetPointingSegmentFromWorld(Vector3 worldOrigin, Vector3 worldEnd, bool hasHit, SaraAvatarPoseFrame pose)
        {
            Transform reference = Sara.Aircraft;
            Vector3 localOrigin = reference != null ? reference.InverseTransformPoint(worldOrigin) : worldOrigin;
            Vector3 localEnd = reference != null ? reference.InverseTransformPoint(worldEnd) : worldEnd;

            pose.PointingOriginX = localOrigin.x;
            pose.PointingOriginY = localOrigin.y;
            pose.PointingOriginZ = localOrigin.z;
            pose.PointingEndX = localEnd.x;
            pose.PointingEndY = localEnd.y;
            pose.PointingEndZ = localEnd.z;
            pose.IsRightPointing = true;
            pose.HasRightPointingHit = hasHit;
        }

        /// <summary>
        /// Lists the supported avatar pose target values.
        /// </summary>
        private enum AvatarPoseTarget
        {
            Head,
            LeftHand,
            RightHand
        }

        /// <summary>
        /// Implements the remote avatar state logic.
        /// </summary>
        private sealed class RemoteAvatarState
        {
            public readonly SaraRemoteAvatar Avatar;
            public readonly SaraAvatarPoseBuffer Buffer;

            /// <summary>
            /// Creates a new remote avatar state instance.
            /// </summary>
            public RemoteAvatarState(SaraRemoteAvatar avatar, int maxBufferedPoses)
            {
                Avatar = avatar;
                Buffer = new SaraAvatarPoseBuffer { MaxFrames = maxBufferedPoses };
            }
        }
    }

    /// <summary>
    /// Implements the SARA avatar seat assignment message logic.
    /// </summary>
    internal static class SaraAvatarSeatAssignmentMessage
    {
        /// <summary>
        /// Runs the create logic.
        /// </summary>
        public static NetworkMessage Create(uint clientId, SeatType seat)
        {
            return new NetworkMessage(Messages.ToClients_AvatarSeatAssignment, clientId)
                .Set(clientId)
                .Set((byte)seat);
        }

        /// <summary>
        /// Runs the try read logic.
        /// </summary>
        public static bool TryRead(NetworkMessage message, out uint clientId, out SeatType seat)
        {
            clientId = 0u;
            seat = SeatType.Pilot;

            if (message == null || message.HeadID != (ushort)Messages.ToClients_AvatarSeatAssignment)
                return false;

            try
            {
                message.RestartRead();
                clientId = message.Serializer.GetUInt();
                seat = (SeatType)message.Serializer.GetByte();
                return clientId != 0u && Seats.IsValidSeat(seat);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Invalid avatar seat assignment message: " + ex.Message);
                return false;
            }
        }
        #endregion
    }
}
