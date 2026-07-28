using Assets.Scripts.UI.Windows.Common;
using Fu;
using Fu.Framework;

using UnityEngine;

/// <summary>
/// Implements the timeline header window XR logic.
/// </summary>
public class TimelineHeaderWindowXR : Fu3DWindowBehaviour
{
    #region Constants
    private float CurvedTimelineCurve = 40f;
    private const float FlatTimelineCurve = 0f;
    #endregion

    #region State
    [SerializeField]
    private TimelineWidgetTheme timelineTheme;
    private readonly TimelineWidget timelineWidget = new TimelineWidget();
    private Transform _panelTransform;
    private bool _isCurved;
    #endregion

    #region Unity lifecycle
    /// <summary>
    /// Configures the generated Fugui window identity before the 3D container is created.
    /// </summary>
    private void Awake()
    {
        CurvedTimelineCurve = Curve;
        ConfigureWindow();
        _isCurved = !Mathf.Approximately(Curve, FlatTimelineCurve);
    }

    /// <summary>
    /// Creates the 3D header window immediately or waits for the flight loading callback.
    /// </summary>
    private void Start()
    {
        ConfigureWindow();

        if (Sara.IsReady)
            CreateHeader3DWindow();
        else
            Sara.Loader.OnLoadingComplete += Loader_OnLoadingComplete;
    }

    /// <summary>
    /// Removes pending loader callbacks when the behaviour is destroyed before SARA finishes loading.
    /// </summary>
    private void OnDestroy()
    {
        if (Sara.Loader != null)
            Sara.Loader.OnLoadingComplete -= Loader_OnLoadingComplete;
    }

    /// <summary>
    /// Creates the 3D header after loading and removes the one-shot callback.
    /// </summary>
    private void Loader_OnLoadingComplete()
    {
        CreateHeader3DWindow();
        Sara.Loader.OnLoadingComplete -= Loader_OnLoadingComplete;
    }
    #endregion

    #region Public API
    /// <summary>
    /// Switches the header panel between curved and flat rendering.
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
    /// Ensures the generated Fugui mesh stays parented to this anchor transform.
    /// </summary>
    public void ParentPanelToHeader()
    {
        if (Container == null || Container.PanelTransform == null)
            return;

        _panelTransform = Container.PanelTransform;
        if (_panelTransform == transform || _panelTransform.parent == transform)
            return;

        _panelTransform.SetParent(transform, true);
    }
    #endregion

    #region Window callbacks
    /// <summary>
    /// Applies non-interfering Fugui flags to the VR timeline header window.
    /// </summary>
    public override void OnWindowCreated(FuWindow window)
    {
        window.IsInterractable = true;
        window.AddWindowFlag(FuWindowStyleFlags.NoDecoration);
        window.AddWindowFlag(FuWindowStyleFlags.NoMove);
        window.AddWindowFlag(FuWindowStyleFlags.NoScrollWithMouse);
        window.AddWindowFlag(FuWindowStyleFlags.NoScrollbar);

        ParentPanelToHeader();
        SetCurved(_isCurved);
    }

    /// <summary>
    /// Draws the compact transport controls in the small XR header panel.
    /// </summary>
    public override void OnUI(FuWindow window, FuLayout layout)
    {
        TimelineWidgetTheme theme = ResolveTimelineTheme();
        timelineWidget.SetTheme(theme);

        layout.Spacing();
        DrawCenteredControls(layout, theme);
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
    /// Draws the centered controls UI.
    /// </summary>
    private void DrawCenteredControls(FuLayout layout, TimelineWidgetTheme theme)
    {
        float scale = Fugui.Scale;
        float availableWidth = layout != null ? layout.GetAvailableWidth() : Fugui.GetContentRegionAvail().x;
        float width = Mathf.Max(16f * scale, availableWidth - 16f * scale);
        float height = Mathf.Max(theme.PlayButtonSize, theme.DockHeight - theme.DockPaddingTop - theme.DockPaddingBottom) * scale;

        if (layout != null)
            layout.CenterNextItemH(width);

        Vector2 startPos = Fugui.GetCursorScreenPos();
        Rect rect = new Rect(startPos.x, startPos.y, width, height);
        timelineWidget.DrawControls(rect);
        Fugui.SetCursorScreenPos(new Vector2(startPos.x, rect.yMax + 2f * scale));
    }

    #region Helpers
    /// <summary>
    /// Configures the Fugui window name and static 3D panel behaviour.
    /// </summary>
    private void ConfigureWindow()
    {
        SetWindowName(FuWindowsNames.TimelineHeaderXR);
        _windowFlags = FuWindowFlags.NoExternalization | FuWindowFlags.NoDocking | FuWindowFlags.NoDockingOverMe;
        _runtimeResizable = false;
        _scaleFontWithContainer = true;
    }

    /// <summary>
    /// Creates the Fugui header window, then reparents its generated panel under this anchor object.
    /// </summary>
    private void CreateHeader3DWindow()
    {
        Create3DWindow();
        ParentPanelToHeader();
    }
    #endregion
}
