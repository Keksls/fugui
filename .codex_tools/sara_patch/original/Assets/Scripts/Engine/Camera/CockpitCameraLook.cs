using Fu;
using Fu.Framework;

using Saravr.Engine.Cameras;
using Saravr.Engine.Visuals;
using Saravr.Network.Common;
using Saravr.Utils;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Adds flat cockpit camera manipulation around the selected seat orientation.
/// </summary>
[DisallowMultipleComponent]
public class CockpitCameraLook : MonoBehaviour
{
    [Header("Rotation Limits")]
    [SerializeField] private float maxYaw = 55f;
    [SerializeField] private float maxPitch = 32f;

    [Header("Rotation Sensitivity")]
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float touchSensitivity = 0.10f;
    [SerializeField] private float rotationSmoothTime = 0.045f;

    [Header("Zoom")]
    [SerializeField] private float defaultFieldOfView = 60f;
    [SerializeField] private float minFieldOfView = 35f;
    [SerializeField] private float maxFieldOfView = 72f;
    [SerializeField] private float mouseWheelZoomStep = 3.5f;
    [SerializeField] private float touchPinchZoomSensitivity = 0.035f;
    [SerializeField] private float zoomSmoothTime = 0.06f;

    private readonly TouchControl[] _activeTouches = new TouchControl[2];

    private AnchorFollow _follower;
    private Camera _camera;

    private float _targetYaw;
    private float _targetPitch;
    private float _currentYaw;
    private float _currentPitch;
    private float _yawVelocity;
    private float _pitchVelocity;

    private float _targetFieldOfView;
    private float _currentFieldOfView;
    private float _fieldOfViewVelocity;

    private bool _mouseDragging;
    private bool _touchDragging;
    private bool _pinching;
    private Vector2 _lastMousePosition;
    private Vector2 _lastTouchPosition;
    private float _lastPinchDistance;

    /// <summary>
    /// Initializes cached references before the first frame.
    /// </summary>
    private void Awake()
    {
        RefreshReferences();
        InitializeZoomFromCamera();
    }

    /// <summary>
    /// Subscribes to runtime events when the component is enabled.
    /// </summary>
    private void OnEnable()
    {
        Sara.Events.OnSeatChanged += Events_OnSeatChanged;
    }

    /// <summary>
    /// Unsubscribes from runtime events when the component is disabled.
    /// </summary>
    private void OnDisable()
    {
        Sara.Events.OnSeatChanged -= Events_OnSeatChanged;
    }

    /// <summary>
    /// Runs per-frame runtime updates.
    /// </summary>
    private void Update()
    {
        if (!CanManipulateCamera())
            return;

        bool handledTouch = HandleTouchInput();
        if (!handledTouch)
        {
            HandleMouseInput();
        }

        ApplyRotation();
        ApplyZoom();
    }


    /// <summary>
    /// Runs the configure logic.
    /// </summary>
    public void Configure(AnchorFollow follower, Camera controlledCamera)
    {
        _follower = follower;
        _camera = controlledCamera;
        InitializeZoomFromCamera();
    }

    /// <summary>
    /// Resets the view state.
    /// </summary>
    public void ResetView()
    {
        _targetYaw = 0f;
        _targetPitch = 0f;
        _currentYaw = 0f;
        _currentPitch = 0f;
        _yawVelocity = 0f;
        _pitchVelocity = 0f;

        _mouseDragging = false;
        _touchDragging = false;
        _pinching = false;

        _targetFieldOfView = Mathf.Clamp(defaultFieldOfView, minFieldOfView, maxFieldOfView);
        _currentFieldOfView = _targetFieldOfView;
        _fieldOfViewVelocity = 0f;

        ApplyRotationOffset(0f, 0f);
        if (_camera != null)
        {
            _camera.fieldOfView = _currentFieldOfView;
        }
    }

    /// <summary>
    /// Handles the seat changed event.
    /// </summary>
    private void Events_OnSeatChanged(SeatType seatType)
    {
        RefreshReferences();
        ResetView();
    }

    /// <summary>
    /// Returns whether the manipulate camera action is allowed.
    /// </summary>
    private bool CanManipulateCamera()
    {
        if (Sara.IsVR)
            return false;

        if (Sara.Cameras != null && Sara.Cameras.CurrentCameraMode != CameraMode.Cockpit)
            return false;

        RefreshReferences();
        return _follower != null && _camera != null;
    }


    /// <summary>
    /// Runs the refresh references logic.
    /// </summary>
    private void RefreshReferences()
    {
        if (_follower == null)
        {
            _follower = GetComponent<AnchorFollow>();
        }

        if (_camera == null)
        {
            _camera = GetComponentInChildren<Camera>();
        }
    }

    /// <summary>
    /// Initializes the zoom from camera state.
    /// </summary>
    private void InitializeZoomFromCamera()
    {
        if (_camera == null)
            return;

        defaultFieldOfView = Mathf.Clamp(defaultFieldOfView, minFieldOfView, maxFieldOfView);
        _targetFieldOfView = Mathf.Clamp(_camera.fieldOfView, minFieldOfView, maxFieldOfView);
        _currentFieldOfView = _targetFieldOfView;
    }


    /// <summary>
    /// Handles the mouse input flow.
    /// </summary>
    private void HandleMouseInput()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        Vector2 position = mouse.position.ReadValue();
        bool pointerBlocked = IsPointerBlockedByUi(position);

        if (mouse.leftButton.wasPressedThisFrame)
        {
            _mouseDragging = !pointerBlocked;
            _lastMousePosition = position;
        }

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            _mouseDragging = false;
        }

        if (_mouseDragging && mouse.leftButton.isPressed)
        {
            Vector2 delta = position - _lastMousePosition;
            _lastMousePosition = position;
            ApplyRotationDelta(delta, mouseSensitivity);
        }

        Vector2 scroll = mouse.scroll.ReadValue();
        if (!pointerBlocked && Mathf.Abs(scroll.y) > Mathf.Epsilon)
        {
            ApplyZoomDelta(-(scroll.y / 120f) * mouseWheelZoomStep);
        }
    }

    /// <summary>
    /// Handles the touch input flow.
    /// </summary>
    private bool HandleTouchInput()
    {
        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen == null)
            return false;

        int touchCount = CollectActiveTouches(touchscreen);
        if (touchCount == 0)
        {
            _touchDragging = false;
            _pinching = false;
            return false;
        }

        if (touchCount >= 2)
        {
            HandlePinch(_activeTouches[0], _activeTouches[1]);
            return true;
        }

        _pinching = false;
        HandleSingleTouch(_activeTouches[0]);
        return true;
    }

    /// <summary>
    /// Runs the collect active touches logic.
    /// </summary>
    private int CollectActiveTouches(Touchscreen touchscreen)
    {
        int count = 0;
        foreach (TouchControl touch in touchscreen.touches)
        {
            if (!touch.press.isPressed)
                continue;

            _activeTouches[count] = touch;
            count++;

            if (count >= _activeTouches.Length)
                break;
        }

        return count;
    }

    /// <summary>
    /// Handles the single touch flow.
    /// </summary>
    private void HandleSingleTouch(TouchControl touch)
    {
        Vector2 position = touch.position.ReadValue();
        int pointerId = touch.touchId.ReadValue();

        if (touch.press.wasPressedThisFrame)
        {
            _touchDragging = !IsPointerBlockedByUi(position, pointerId);
            _lastTouchPosition = position;
            return;
        }

        if (!_touchDragging)
            return;

        Vector2 delta = position - _lastTouchPosition;
        _lastTouchPosition = position;
        ApplyRotationDelta(delta, touchSensitivity);
    }

    /// <summary>
    /// Handles the pinch flow.
    /// </summary>
    private void HandlePinch(TouchControl firstTouch, TouchControl secondTouch)
    {
        Vector2 firstPosition = firstTouch.position.ReadValue();
        Vector2 secondPosition = secondTouch.position.ReadValue();
        float distance = Vector2.Distance(firstPosition, secondPosition);

        if (!_pinching || firstTouch.press.wasPressedThisFrame || secondTouch.press.wasPressedThisFrame)
        {
            _pinching =
                !IsPointerBlockedByUi(firstPosition, firstTouch.touchId.ReadValue()) &&
                !IsPointerBlockedByUi(secondPosition, secondTouch.touchId.ReadValue());
            _touchDragging = false;
            _lastPinchDistance = distance;
            return;
        }

        if (!_pinching)
            return;

        float pinchDelta = distance - _lastPinchDistance;
        _lastPinchDistance = distance;
        ApplyZoomDelta(-pinchDelta * touchPinchZoomSensitivity);
    }


    /// <summary>
    /// Applies the rotation delta state.
    /// </summary>
    private void ApplyRotationDelta(Vector2 delta, float sensitivity)
    {
        _targetYaw = Mathf.Clamp(_targetYaw + delta.x * sensitivity, -maxYaw, maxYaw);
        _targetPitch = Mathf.Clamp(_targetPitch - delta.y * sensitivity, -maxPitch, maxPitch);
    }

    /// <summary>
    /// Applies the zoom delta state.
    /// </summary>
    private void ApplyZoomDelta(float delta)
    {
        _targetFieldOfView = Mathf.Clamp(_targetFieldOfView + delta, minFieldOfView, maxFieldOfView);
    }

    /// <summary>
    /// Applies the rotation state.
    /// </summary>
    private void ApplyRotation()
    {
        _currentYaw = Mathf.SmoothDampAngle(_currentYaw, _targetYaw, ref _yawVelocity, rotationSmoothTime);
        _currentPitch = Mathf.SmoothDampAngle(_currentPitch, _targetPitch, ref _pitchVelocity, rotationSmoothTime);
        ApplyRotationOffset(_currentPitch, _currentYaw);
    }

    /// <summary>
    /// Applies the rotation offset state.
    /// </summary>
    private void ApplyRotationOffset(float pitch, float yaw)
    {
        if (_follower == null)
            return;

        _follower.rotationOffset = new Vector3(pitch, yaw, 0f);
        _follower.rotationOffsetSpace = Space.Self;
    }

    /// <summary>
    /// Applies the zoom state.
    /// </summary>
    private void ApplyZoom()
    {
        if (_camera == null)
            return;

        _currentFieldOfView = Mathf.SmoothDamp(
            _currentFieldOfView,
            _targetFieldOfView,
            ref _fieldOfViewVelocity,
            zoomSmoothTime);
        _camera.fieldOfView = _currentFieldOfView;
    }

    /// <summary>
    /// Returns whether the pointer blocked by UI condition is met.
    /// </summary>
    private static bool IsPointerBlockedByUi(Vector2 screenPosition, int pointerId = -1)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null)
        {
            if (pointerId >= 0 && eventSystem.IsPointerOverGameObject(pointerId))
                return true;

            if (pointerId < 0 && eventSystem.IsPointerOverGameObject())
                return true;
        }

        Vector2 imguiPosition = ScreenToImGuiPosition(screenPosition);
        if (FlatCameraInputBlocker.IsPointerBlocked(imguiPosition))
            return true;

        if (Fugui.IsThereAnyOpenPopup() && Fugui.IsInsideAnyPopup(imguiPosition))
            return true;

        if (Fugui.CurrentContext.RenderPrepared)
            return false;

        return Fugui.IsAnyItemActive() || Fugui.IsAnyItemHovered() || FuLayout.IsAnyItemActive;
    }

    /// <summary>
    /// Runs the screen to im gui position logic.
    /// </summary>
    private static Vector2 ScreenToImGuiPosition(Vector2 screenPosition)
    {
        return new Vector2(screenPosition.x, Screen.height - screenPosition.y);
    }
}
