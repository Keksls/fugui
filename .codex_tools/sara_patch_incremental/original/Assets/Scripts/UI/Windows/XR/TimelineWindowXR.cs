using Assets.Scripts.UI.Windows.Common;
using Fu;
using Fu.Framework;

using Saravr.Engine.Cameras;
using Saravr.Engine.Visuals;
using Saravr.Network.Common;
using UnityEngine;

/// <summary>
/// Implements the timeline window XR logic.
/// </summary>
public class TimelineWindowXR : Fu3DWindowBehaviour
{
    #region Constants
    private const float CurvedTimelineCurve = 40f;
    private const float FlatTimelineCurve = 0f;
    #endregion

    #region State
    private bool startEnablingManipulator;
    [SerializeField]
    private TimelineWidgetTheme timelineTheme;
    [SerializeField]
    private bool followCameraRootOnSeatChanges = true;
    private readonly TimelineWidget timelineWidget = new TimelineWidget();
    private Fu3DWindowManipulator _manipulator;
    private Transform _panelTransform;
    private Transform _initialParent;
    private Vector3 _initialLocalPosition;
    private Quaternion _initialLocalRotation;
    private Vector3 _initialLocalScale;
    private Vector3 _initialWorldPosition;
    private Quaternion _initialWorldRotation;
    private Vector3 _initialWorldScale;
    private Vector3 _cameraRootPositionOffset;
    private bool _hasInitialPose;
    private bool _hasCameraRootPositionOffset;
    private bool _isCurved;
    private bool _manipulatorEnabled;
    #endregion

    #region Unity lifecycle
    /// <summary>
    /// Captures the original timeline pose used by the VR debug reset control.
    /// </summary>
    private void Awake()
    {
        _isCurved = !Mathf.Approximately(Curve, FlatTimelineCurve);
        CaptureInitialPose();
        if(startEnablingManipulator)
            SetManipulatorEnabled(true);
    }

    /// <summary>
    /// Subscribes to runtime events when the component is enabled.
    /// </summary>
    private void OnEnable()
    {
        if (Sara.Events != null)
        {
            Sara.Events.OnCameraChanged += Events_OnCameraChanged;
            Sara.Events.OnSeatChanged += Events_OnSeatChanged;
        }
    }

    /// <summary>
    /// Unsubscribes from runtime events when the component is disabled.
    /// </summary>
    private void OnDisable()
    {
        if (Sara.Events != null)
        {
            Sara.Events.OnCameraChanged -= Events_OnCameraChanged;
            Sara.Events.OnSeatChanged -= Events_OnSeatChanged;
        }
    }

    /// <summary>
    /// Creates the 3D timeline window immediately or waits for the flight loading callback.
    /// </summary>
    void Start()
    {
        if (Sara.IsReady)
            CreateTimeline3DWindow();
        else
            Sara.Loader.OnLoadingComplete += Loader_OnLoadingComplete;
    }

    /// <summary>
    /// Creates the 3D timeline window after loading and removes the one-shot callback.
    /// </summary>
    private void Loader_OnLoadingComplete()
    {
        CreateTimeline3DWindow();
        Sara.Loader.OnLoadingComplete -= Loader_OnLoadingComplete;
    }

    /// <summary>
    /// Removes pending loading callbacks when the behaviour is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        if (Sara.Loader != null)
            Sara.Loader.OnLoadingComplete -= Loader_OnLoadingComplete;
    }

    /// <summary>
    /// Runs per-frame updates after standard Update processing.
    /// </summary>
    private void LateUpdate()
    {
        if (!followCameraRootOnSeatChanges)
            return;

        CaptureCameraRootOffset();
    }
    #endregion

    #region Debug controls API
    /// <summary>
    /// Gets whether the timeline panel is currently rendered with curvature.
    /// </summary>
    public bool IsCurved => _isCurved;

    /// <summary>
    /// Gets whether the VR timeline manipulator is enabled.
    /// </summary>
    public bool ManipulatorEnabled => _manipulatorEnabled;

    /// <summary>
    /// Switches the timeline 3D panel between curved and flat rendering.
    /// </summary>
    public void SetCurved(bool curved)
    {
        _isCurved = curved;
        float targetCurve = curved ? CurvedTimelineCurve : FlatTimelineCurve;

        Curve = targetCurve;
        if (Container != null)
            Container.SetPanelCurve(targetCurve);

        Window?.ForceDraw();
    }

    /// <summary>
    /// Enables or disables runtime movement of the timeline through Fugui raycasters.
    /// </summary>
    public void SetManipulatorEnabled(bool enabled)
    {
        _manipulatorEnabled = enabled;
        ConfigureManipulator(enabled);

        Window?.ForceDraw();
    }

    /// <summary>
    /// Restores the timeline to the pose captured from the prefab at startup.
    /// </summary>
    public void ResetToInitialPose()
    {
        if (!_hasInitialPose)
            return;

        if (_initialParent != null)
        {
            transform.SetParent(_initialParent, false);
            transform.localPosition = _initialLocalPosition;
            transform.localRotation = _initialLocalRotation;
            transform.localScale = _initialLocalScale;
        }
        else
        {
            transform.position = _initialWorldPosition;
            transform.rotation = _initialWorldRotation;
            transform.localScale = _initialWorldScale;
        }

        ParentPanelToTimeline();

        ApplyContainerPose();

        _manipulator?.Anchor();
        CaptureCameraRootOffset();
    }
    #endregion

    #region Seat follow
    /// <summary>
    /// Restores the configured timeline placement whenever the active XR camera changes.
    /// </summary>
    private void Events_OnCameraChanged(Camera camera, GameObject cameraRoot, CameraMode cameraMode, bool isVR)
    {
        if (!followCameraRootOnSeatChanges)
            return;

        if (!isVR)
            return;

        ResetToInitialPose();
        _hasCameraRootPositionOffset = false;
    }

    /// <summary>
    /// Restores the last known distance from the XR camera root after the seat root has moved.
    /// </summary>
    private void Events_OnSeatChanged(SeatType seatType)
    {
        if (!followCameraRootOnSeatChanges)
            return;

        Transform cameraRoot = GetCurrentCameraRootTransform();
        if (cameraRoot == null)
            return;

        if (!_hasCameraRootPositionOffset)
        {
            CaptureCameraRootOffset(cameraRoot);
            return;
        }

        transform.position = cameraRoot.position + _cameraRootPositionOffset;
        ApplyContainerPose();
        _manipulator?.Anchor();
        CaptureCameraRootOffset(cameraRoot);
    }

    /// <summary>
    /// Tracks the current world-space offset between this timeline anchor and the XR camera root.
    /// </summary>
    private void CaptureCameraRootOffset()
    {
        Transform cameraRoot = GetCurrentCameraRootTransform();
        if (cameraRoot == null)
            return;

        CaptureCameraRootOffset(cameraRoot);
    }

    /// <summary>
    /// Runs the capture camera root offset logic.
    /// </summary>
    private void CaptureCameraRootOffset(Transform cameraRoot)
    {
        _cameraRootPositionOffset = transform.position - cameraRoot.position;
        _hasCameraRootPositionOffset = true;
    }

    /// <summary>
    /// Returns the current camera root transform value.
    /// </summary>
    private Transform GetCurrentCameraRootTransform()
    {
        if (Sara.Cameras == null || Sara.Cameras.CurrentCameraRoot == null)
            return null;

        return Sara.Cameras.CurrentCameraRoot.transform;
    }

    /// <summary>
    /// Applies the container pose state.
    /// </summary>
    private void ApplyContainerPose()
    {
        if (Container == null)
            return;

        Container.SetPosition(transform.position);
        Container.SetRotation(transform.rotation);
    }
    #endregion

    #region Window callbacks
    /// <summary>
    /// Applies non-interfering ImGui flags to the VR timeline window.
    /// </summary>
    /// <param name="window">Created Fu 3D window.</param>
    public override void OnWindowCreated(FuWindow window)
    {
        window.IsInterractable = true;
        window.AddWindowFlag(FuWindowStyleFlags.NoDecoration);
        window.AddWindowFlag(FuWindowStyleFlags.NoMove);
        window.AddWindowFlag(FuWindowStyleFlags.NoScrollWithMouse);
        ParentPanelToTimeline();
        SetCurved(_isCurved);
        ConfigureManipulator(_manipulatorEnabled);
    }

    /// <summary>
    /// Draws timeline, phase navigation and playback speed controls in VR.
    /// </summary>
    /// <param name="window">Current Fu 3D window.</param>
    /// <param name="layout">Current Fu layout used by the widget.</param>
    public override void OnUI(FuWindow window, FuLayout layout)
    {
        TimelineWidgetTheme theme = ResolveTimelineTheme();
        timelineWidget.SetTheme(theme);
        //timelineWidget.SetEvents(new[]
        //{
        //    CreateTimelineTestEvent(30f, "Top of climb", EventSeverity.Info),
        //    CreateTimelineTestEvent(42f, "Step climb", EventSeverity.Info),
        //    CreateTimelineTestEvent(67f, "High descent rate", EventSeverity.Medium),
        //    CreateTimelineTestEvent(67.8f, "Flaps extended", EventSeverity.Info),
        //    CreateTimelineTestEvent(68.4f, "Speed deviation", EventSeverity.Medium),
        //    CreateTimelineTestEvent(69f, "Gear down", EventSeverity.Info),
        //    CreateTimelineTestEvent(81f, "GPWS alert", EventSeverity.High),
        //    CreateTimelineTestEvent(82f, "Bank angle", EventSeverity.High),
        //    CreateTimelineTestEvent(92f, "Touchdown", EventSeverity.Info),
        //});

        DrawTimelineBody(layout, theme);
    }

    /// <summary>
    /// Creates the timeline test event instance or data.
    /// </summary>
    private static TimelineEvent CreateTimelineTestEvent(float percent, string label, EventSeverity severity)
    {
        double normalized = Mathf.Clamp01(percent / 100f);
        double deltaTid = (Sara.Time.LastTid - Sara.Time.FirstTid) * normalized;
        return new TimelineEvent(Sara.Time.FirstTid + deltaTid, label, severity, (int)severity >= (int)EventSeverity.Medium);
    }
    #endregion

    /// <summary>
    /// Resolves the timeline theme reference or value.
    /// </summary>
    private TimelineWidgetTheme ResolveTimelineTheme()
    {
        if (timelineTheme == null)
            timelineTheme = TimelineWidgetTheme.LoadDefault();

        return timelineTheme;
    }

    /// <summary>
    /// Draws only the timeline body because transport controls are always available in the VR belt.
    /// </summary>
    private void DrawTimelineBody(FuLayout layout, TimelineWidgetTheme theme)
    {
        float scale = Fugui.Scale;
        float availableWidth = layout != null ? layout.GetAvailableWidth() : Fugui.GetContentRegionAvail().x;
        float width = Mathf.Max(16f * scale, availableWidth - 2f * scale);
        float height = Mathf.Max(1f, theme.DockHeight - theme.DockPaddingTop - theme.DockPaddingBottom) * scale;

        if (layout != null)
            layout.CenterNextItemH(width);

        // The separate panel only renders the timeline while the VR belt owns all transport controls.
        Vector2 startPosition = Fugui.GetCursorScreenPos();
        Rect rect = new Rect(startPosition.x, startPosition.y, width, height);
        timelineWidget.DrawTimeline(rect);
        Fugui.SetCursorScreenPos(new Vector2(startPosition.x, rect.yMax + 2f * scale));
    }

    #region Manipulator
    /// <summary>
    /// Stores the transform pose that the reset button should restore.
    /// </summary>
    private void CaptureInitialPose()
    {
        _initialParent = transform.parent;
        _initialLocalPosition = transform.localPosition;
        _initialLocalRotation = transform.localRotation;
        _initialLocalScale = transform.localScale;
        _initialWorldPosition = transform.position;
        _initialWorldRotation = transform.rotation;
        _initialWorldScale = transform.lossyScale;
        _hasInitialPose = true;
    }

    /// <summary>
    /// Creates the Fugui timeline window, then reparents its generated panel under this anchor object.
    /// </summary>
    private void CreateTimeline3DWindow()
    {
        Create3DWindow();
        ParentPanelToTimeline();
    }

    /// <summary>
    /// Keeps the Fugui generated panel attached to this timeline anchor instead of the scene root.
    /// </summary>
    private void ParentPanelToTimeline()
    {
        if (Container == null || Container.PanelTransform == null)
            return;

        _panelTransform = Container.PanelTransform;
        if (_panelTransform == transform || _panelTransform.parent == transform)
            return;

        _panelTransform.SetParent(transform, true);
    }

    /// <summary>
    /// Configures the existing Fugui 3D window manipulator for the timeline.
    /// </summary>
    private void ConfigureManipulator(bool enabled)
    {
        if (!enabled)
        {
            if (_manipulator != null)
            {
                _manipulator.RuntimeMovable = false;
                _manipulator.CreateGrabHandle = false;
                _manipulator.Anchor();
                SetGeneratedGrabHandleVisible(false);
                _manipulator.enabled = false;
            }

            return;
        }

        _manipulator = _manipulator != null ? _manipulator : EnsureManipulator();
        _manipulator.enabled = true;
        _manipulator.RuntimeMovable = true;
        _manipulator.CreateGrabHandle = true;
        _manipulator.GrabHandleWidthRatio = 0.35f;
        _manipulator.GrabHandleMinWidth = 0.12f;
        _manipulator.GrabHandleMaxWidth = 0.28f;
        _manipulator.GrabHandleHeight = 0.025f;
        _manipulator.GrabHandleEdgeOffset = 0.018f;
        _manipulator.GrabHandleFrontOffset = 0.004f;
        _manipulator.GrabHandlePosition = Fu3DWindowManipulator.Fu3DWindowManipulatorHandlePosition.Bottom;
        _manipulator.AnchorOnRelease = true;
        _manipulator.StartAnchored = true;
        _manipulator.FaceTargetWhileDragging = false;
        _manipulator.FaceTargetWhenUnanchored = false;
        _manipulator.FacingConstraint = Fu3DWindowManipulator.Fu3DWindowFacingConstraint.None;
        _manipulator.WorldUpAxis = Vector3.up;
        _manipulator.Anchor();
    }

    /// <summary>
    /// Shows or hides the grab handle generated by Fu3DWindowManipulator.
    /// </summary>
    private void SetGeneratedGrabHandleVisible(bool visible)
    {
        Transform panelTransform = Container != null ? Container.PanelTransform : _panelTransform;
        if (panelTransform == null)
            return;

        for (int i = 0; i < panelTransform.childCount; i++)
        {
            Transform child = panelTransform.GetChild(i);
            if (child.name.StartsWith("Fu3DWindowManipulator_") && child.name.EndsWith("_GrabHandle"))
                child.gameObject.SetActive(visible);
        }
    }

    #endregion
}
