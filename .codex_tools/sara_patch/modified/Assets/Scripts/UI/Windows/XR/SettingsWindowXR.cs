using Assets.Scripts.UI.Windows.Common;
using Fu;
using Fu.Framework;

using Saravr.Core.Performance;
using Saravr.Network.Common;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;

/// <summary>
/// Implements the settings window XR logic.
/// </summary>
public class SettingsWindowXR : Fu3DWindowBehaviour
{
    private const float PanelWidth = 0.2f;
    private const float PanelHeight = 0.4f;
    private const float PanelDepth = 0.005f;
    private const float TabStripHeight = 46f;
    private static readonly bool ShowXrRenderDebugControls = false;
    private static readonly int[] MsaaValues = { 0, 2, 4, 8 };
    private static readonly SaraXrRenderPriority[] XrRenderPriorities =
    {
        SaraXrRenderPriority.QualityGround,
        SaraXrRenderPriority.Balanced,
        SaraXrRenderPriority.Cockpit
    };
    private static readonly List<string> XrRenderPriorityLabels = new List<string>
    {
        GetRenderPriorityLabel(SaraXrRenderPriority.QualityGround),
        GetRenderPriorityLabel(SaraXrRenderPriority.Balanced),
        GetRenderPriorityLabel(SaraXrRenderPriority.Cockpit)
    };

    [SerializeField]
    private TimelineWidgetTheme timelineTheme;
    [SerializeField]
    private bool editorPlacedMode;
    [SerializeField]
    private bool showAdminTab = true;

    private readonly SettingsWidget settingsWidget = new SettingsWidget();
    private readonly AdminPanelWidget adminPanelWidget = new AdminPanelWidget();
    private Transform _panelTransform;
    private int _selectedPanelIndex;
    private bool _subscribedToLoader;

    /// <summary>
    /// Gets or sets whether the open state is active.
    /// </summary>
    public bool IsOpen => Container != null && !Container.IsClosed;

    /// <summary>
    /// Initializes cached references before the first frame.
    /// </summary>
    private void Awake()
    {
        ConfigureWindow();
        _runtimeResizable = false;
    }

    /// <summary>
    /// Creates the editor placed settings window when this GameObject is enabled.
    /// </summary>
    private void OnEnable()
    {
        if (editorPlacedMode)
            TryCreateEditorPlacedWindow();
    }

    /// <summary>
    /// Creates the editor placed settings window after startup if needed.
    /// </summary>
    private void Start()
    {
        if (editorPlacedMode)
            TryCreateEditorPlacedWindow();
    }

    /// <summary>
    /// Closes the editor placed settings window when this GameObject is disabled.
    /// </summary>
    private void OnDisable()
    {
        if (!editorPlacedMode)
            return;

        UnsubscribeFromLoader();
        Close3DWindow();
    }

    /// <summary>
    /// Removes pending loading callbacks when this behaviour is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        UnsubscribeFromLoader();
    }

    /// <summary>
    /// Runs the show at logic.
    /// </summary>
    public void ShowAt(Transform source, Transform facingTarget, Vector3 sourceLocalOffset)
    {
        ConfigureWindow();
        PlaceFromSource(source, facingTarget, sourceLocalOffset);
        Create3DWindow();
        ParentPanelToSettings();
        ConfigureManipulator(facingTarget);
    }

    /// <summary>
    /// Runs the show at world position logic.
    /// </summary>
    public void ShowAtWorldPosition(Vector3 worldPosition, Transform facingTarget)
    {
        ConfigureWindow();
        PlaceAtWorldPosition(worldPosition, facingTarget);
        Create3DWindow();
        ParentPanelToSettings();
        ConfigureManipulator(facingTarget);
    }


    /// <summary>
    /// Overrides the base on window created behavior.
    /// </summary>
    public override void OnWindowCreated(FuWindow window)
    {
        window.AddWindowFlag(FuWindowStyleFlags.NoDecoration);
        window.AddWindowFlag(FuWindowStyleFlags.NoMove);
        window.AddWindowFlag(FuWindowStyleFlags.NoBackground);
        window.AddWindowFlag(FuWindowStyleFlags.NoScrollWithMouse);
        window.AddWindowFlag(FuWindowStyleFlags.NoScrollbar);
        window.IsInterractable = true;
        ParentPanelToSettings();
    }

    /// <summary>
    /// Overrides the base on UI behavior.
    /// </summary>
    public override void OnUI(FuWindow window, FuLayout layout)
    {
        TimelineWidgetTheme theme = ResolveTimelineTheme();
        settingsWidget.SetTheme(theme);
        adminPanelWidget.SetTheme(theme);

        Rect panelRect = window.LocalRect;
        DrawPanelBackground(panelRect, theme);

        if (showAdminTab && IsAdminPanelAvailable())
        {
            Rect contentRect = DrawPanelTabs(panelRect, theme);
            if (_selectedPanelIndex == 1)
            {
                if (adminPanelWidget.Draw(contentRect, 1f))
                    CloseFromUi();
            }
            else if (settingsWidget.Draw(contentRect, 1f))
            {
                CloseFromUi();
            }
        }
        else
        {
            _selectedPanelIndex = 0;
            if (settingsWidget.Draw(panelRect, 1f))
                CloseFromUi();
        }

        if (ShowXrRenderDebugControls)
            DrawXrRenderControls(layout);
    }

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
    /// Draws the panel background UI.
    /// </summary>
    private static void DrawPanelBackground(Rect panelRect, TimelineWidgetTheme theme)
    {
        FuDrawList drawList = Fugui.GetCurrentWindowDrawList();
        float scale = Fugui.Scale;
        float rounding = theme.MediumRadius * scale;

        drawList.AddRectFilled(panelRect.min, panelRect.max, ColorU32(theme.SettingsPanelBackground), rounding);
        drawList.AddRect(panelRect.min, panelRect.max, ColorU32(theme.DockBorder), rounding, FuDrawFlags.None, Mathf.Max(1f, scale));
    }

    /// <summary>
    /// Draws the settings/admin tab strip and returns the remaining content rect.
    /// </summary>
    private Rect DrawPanelTabs(Rect panelRect, TimelineWidgetTheme theme)
    {
        FuDrawList drawList = Fugui.GetCurrentWindowDrawList();
        float scale = Fugui.Scale;
        float height = TabStripHeight * scale;
        Rect stripRect = new Rect(panelRect.x, panelRect.y, panelRect.width, height);
        Rect contentRect = new Rect(panelRect.x, stripRect.yMax, panelRect.width, Mathf.Max(0f, panelRect.height - height));
        float pad = 10f * scale;
        float gap = 8f * scale;
        float tabWidth = (stripRect.width - pad * 2f - gap) * 0.5f;
        Rect settingsRect = new Rect(stripRect.x + pad, stripRect.y + 8f * scale, tabWidth, stripRect.height - 16f * scale);
        Rect adminRect = new Rect(settingsRect.xMax + gap, settingsRect.y, tabWidth, settingsRect.height);

        if (DrawTabButton(drawList, settingsRect, "Settings", _selectedPanelIndex == 0, theme))
            _selectedPanelIndex = 0;

        if (DrawTabButton(drawList, adminRect, "Admin", _selectedPanelIndex == 1, theme, HasPendingObserverUnmuteRequest()))
            _selectedPanelIndex = 1;

        drawList.AddLine(
            new Vector2(panelRect.x, stripRect.yMax),
            new Vector2(panelRect.xMax, stripRect.yMax),
            ColorU32(theme.DockBorder),
            Mathf.Max(1f, scale));

        return contentRect;
    }

    /// <summary>
    /// Draws one tab strip button.
    /// </summary>
    private static bool DrawTabButton(FuDrawList drawList, Rect rect, string label, bool selected, TimelineWidgetTheme theme, bool alert = false)
    {
        bool hovered = rect.Contains(Fugui.GetCurrentMouse().Position);
        bool active = hovered && Fugui.GetCurrentMouse().IsPressed(FuMouseButton.Left);
        bool clicked = hovered && Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left);
        float scale = Fugui.Scale;
        Color background = selected ? theme.PillBackgroundActive : active ? theme.PillBackgroundActive : hovered ? theme.PillBackgroundHover : theme.SettingsDropdownBackground;
        Color text = selected ? theme.Accent : theme.TextDim;

        drawList.AddRectFilled(rect.min, rect.max, ColorU32(background), rect.height * 0.5f);
        drawList.AddRect(rect.min, rect.max, ColorU32(theme.DockBorder), rect.height * 0.5f, FuDrawFlags.None, Mathf.Max(1f, scale));

        Fugui.PushFont(12);
        Fugui.PushFont(FontType.Bold);
        DrawTextCentered(drawList, rect, label, ColorU32(text));
        Fugui.PopFont();
        Fugui.PopFont();

        if (alert)
            DrawAlertDot(drawList, rect);

        if (hovered)
            Fugui.SetMouseCursor(FuMouseCursor.Hand);

        return clicked;
    }

    /// <summary>
    /// Draws a red notification dot on a tab.
    /// </summary>
    private static void DrawAlertDot(FuDrawList drawList, Rect rect)
    {
        float scale = Fugui.Scale;
        Vector2 center = new Vector2(rect.xMax - 11f * scale, rect.y + 11f * scale);
        drawList.AddCircleFilled(center, 5f * scale, ColorU32(new Color(1f, 0.13f, 0.13f, 1f)), 16);
        drawList.AddCircle(center, 5f * scale, ColorU32(new Color(1f, 1f, 1f, 0.72f)), 16, Mathf.Max(1f, scale));
    }

    /// <summary>
    /// Returns whether admin controls are available for the local user.
    /// </summary>
    private static bool IsAdminPanelAvailable()
    {
        SaraUser user = Sara.CurrentSession != null ? Sara.CurrentSession.User : null;
        return Sara.CurrentSession != null
            && Sara.CurrentSession.IsMultiplayer
            && user != null
            && user.IsAdmin;
    }

    /// <summary>
    /// Returns whether at least one observer is waiting for admin unmute.
    /// </summary>
    private static bool HasPendingObserverUnmuteRequest()
    {
        SaraSession session = Sara.CurrentSession;
        if (session == null || session.Users == null)
            return false;

        for (int i = 0; i < session.Users.Length; i++)
        {
            SaraSessionUser sessionUser = session.Users[i];
            if (sessionUser != null
                && sessionUser.User != null
                && sessionUser.User.IsObservator
                && sessionUser.User.WantsUnmute)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Runs the color u 32 logic.
    /// </summary>
    private static uint ColorU32(Color color)
    {
        return Fugui.GetColorU32(new Vector4(color.r, color.g, color.b, color.a));
    }

    /// <summary>
    /// Draws centered text.
    /// </summary>
    private static void DrawTextCentered(FuDrawList drawList, Rect rect, string text, uint color)
    {
        Vector2 textSize = Fugui.CalcTextSize(text);
        Vector2 textPos = new Vector2(rect.x + (rect.width - textSize.x) * 0.5f, rect.y + (rect.height - textSize.y) * 0.5f);
        drawList.AddText(textPos, color, text);
    }


    /// <summary>
    /// Draws the XR render controls UI.
    /// </summary>
    private void DrawXrRenderControls(FuLayout layout)
    {
        float scale = Fugui.Scale > 0f ? Fugui.Scale : 1f;
        float width = Mathf.Max(220f * scale, Fugui.GetContentRegionAvail().x);
        FuElementSize sliderSize = new FuElementSize(width / scale, 4f);

        layout.Spacing();
        layout.Separator();
        layout.Spacing();
        Fugui.PushFont(FontType.Bold);
        layout.Text("XR Rendering");
        Fugui.PopFont();

        DrawXrRenderPriorityControl(layout);

        UniversalRenderPipelineAsset urpAsset = UniversalRenderPipeline.asset;
        if (urpAsset != null)
        {
            float renderScale = urpAsset.renderScale;
            layout.Text("URP render scale  " + renderScale.ToString("F2"));
            if (layout.Slider("##XRUrpRenderScale", ref renderScale, 0.5f, 2f, sliderSize, 0.01f, format: "%.2f"))
                urpAsset.renderScale = Mathf.Clamp(renderScale, 0.5f, 2f);

            int msaaIndex = GetMsaaIndex(GetDisplayMsaaFromUrp(urpAsset.msaaSampleCount));
            layout.Text("MSAA  x" + MsaaValues[msaaIndex]);
            if (layout.Slider("##XRMsaa", ref msaaIndex, 0, MsaaValues.Length - 1, sliderSize, format: "x%.0f"))
            {
                int msaa = MsaaValues[Mathf.Clamp(msaaIndex, 0, MsaaValues.Length - 1)];
                ApplyMsaa(urpAsset, msaa);
            }
        }
        else
        {
            DrawDisabledText(layout, "URP asset unavailable");
        }

        float eyeScale = XRSettings.eyeTextureResolutionScale;
        layout.Text("XR eye scale  " + eyeScale.ToString("F2"));
        if (layout.Slider("##XREyeScale", ref eyeScale, 0.5f, 2f, sliderSize, 0.01f, format: "%.2f"))
            XRSettings.eyeTextureResolutionScale = Mathf.Clamp(eyeScale, 0.5f, 2f);

        DrawDisabledText(layout, "Eye " + XRSettings.eyeTextureWidth + " x " + XRSettings.eyeTextureHeight);
    }

    /// <summary>
    /// Draws the XR render priority control UI.
    /// </summary>
    private static void DrawXrRenderPriorityControl(FuLayout layout)
    {
        SaraUnityQualitySettings settings = GetCurrentUnityQualitySettings();
        if (settings == null || !settings.ApplyXrRenderPriority)
        {
            DrawDisabledText(layout, "Render priority unavailable");
            return;
        }

        SaraXrRenderPriority currentPriority = settings.XrRenderPriority;
        SaraXrRenderPrioritySettings currentPreset = settings.GetXrRenderPrioritySettings(currentPriority);
        layout.Text("Render priority");
        layout.Combobox(
            "##XRRenderPriority",
            XrRenderPriorityLabels,
            index =>
            {
                if (index >= 0 && index < XrRenderPriorities.Length)
                {
                    SaraXrRenderPriority priority = XrRenderPriorities[index];
                    SaraPerformanceProfileApplier.ApplyXrRenderPriority(settings, priority);
                }
            },
            () => GetRenderPriorityLabel(settings.XrRenderPriority));

        currentPreset = settings.GetXrRenderPrioritySettings(settings.XrRenderPriority);
        if (currentPreset != null)
            DrawDisabledText(layout, "Preset  MSAA x" + currentPreset.MSAA + " / RS " + currentPreset.RenderScale.ToString("F2"));
    }

    /// <summary>
    /// Draws informational text using Fugui's disabled element state.
    /// </summary>
    private static void DrawDisabledText(FuLayout layout, string text)
    {
        layout.DisableNextElement();
        layout.Text(text);
    }


    /// <summary>
    /// Returns the current unity quality settings value.
    /// </summary>
    private static SaraUnityQualitySettings GetCurrentUnityQualitySettings()
    {
        SaraPerformanceProfile profile = Sara.Performance != null && Sara.Performance.CurrentProfile != null
            ? Sara.Performance.CurrentProfile
            : SaraPerformanceProfileApplier.CurrentProfile;

        return profile != null ? profile.Unity : null;
    }

    /// <summary>
    /// Returns the msaa index value.
    /// </summary>
    private static int GetMsaaIndex(int msaa)
    {
        int closestIndex = 0;
        int closestDistance = Mathf.Abs(MsaaValues[0] - msaa);

        for (int i = 1; i < MsaaValues.Length; i++)
        {
            int distance = Mathf.Abs(MsaaValues[i] - msaa);
            if (distance < closestDistance)
            {
                closestIndex = i;
                closestDistance = distance;
            }
        }

        return closestIndex;
    }

    /// <summary>
    /// Applies the msaa state.
    /// </summary>
    private static void ApplyMsaa(UniversalRenderPipelineAsset urpAsset, int msaa)
    {
        int normalizedMsaa = NormalizeMsaaForUrp(msaa);
        urpAsset.msaaSampleCount = normalizedMsaa;
        QualitySettings.antiAliasing = normalizedMsaa <= 1 ? 0 : normalizedMsaa;
    }

    /// <summary>
    /// Normalizes the msaa for urp value.
    /// </summary>
    private static int NormalizeMsaaForUrp(int msaa)
    {
        if (msaa >= 8)
            return 8;
        if (msaa >= 4)
            return 4;
        if (msaa >= 2)
            return 2;

        return 1;
    }

    /// <summary>
    /// Returns the display msaa from urp value.
    /// </summary>
    private static int GetDisplayMsaaFromUrp(int urpMsaa)
    {
        return urpMsaa <= 1 ? 0 : urpMsaa;
    }


    /// <summary>
    /// Returns the render priority label value.
    /// </summary>
    private static string GetRenderPriorityLabel(SaraXrRenderPriority priority)
    {
        switch (priority)
        {
            case SaraXrRenderPriority.Cockpit:
                return "Cockpit";
            case SaraXrRenderPriority.Balanced:
                return "Balanced";
            default:
                return "Quality Ground";
        }
    }

    /// <summary>
    /// Configures the window state.
    /// </summary>
    private void ConfigureWindow()
    {
        SetWindowName(FuWindowsNames.Settings3D);
        _windowFlags = FuWindowFlags.NoExternalization | FuWindowFlags.NoDocking | FuWindowFlags.NoDockingOverMe;
        _runtimeResizable = false;

        if (editorPlacedMode)
            return;

        _renderResolution = new Vector2Int(700, 1400);
        _baseContextScale = 1.75f;
        _baseFontScale = 1.75f;
        _scaleFontWithContainer = true;
        _useDpiScale = false;
        Depth = PanelDepth;
        Curve = 30f;
        Rounding = 0.008f;
        transform.localScale = new Vector3(PanelWidth, PanelHeight, PanelDepth);
    }

    /// <summary>
    /// Closes the settings panel from its close button.
    /// </summary>
    private void CloseFromUi()
    {
        if (editorPlacedMode)
            gameObject.SetActive(false);
        else
            Close3DWindow();
    }

    /// <summary>
    /// Ensures the generated Fugui settings panel remains under this anchor.
    /// </summary>
    private void ParentPanelToSettings()
    {
        if (Container == null || Container.PanelTransform == null)
            return;

        _panelTransform = Container.PanelTransform;
        if (_panelTransform == transform || _panelTransform.parent == transform)
            return;

        _panelTransform.SetParent(transform, true);
    }

    /// <summary>
    /// Creates the editor placed Fugui window immediately or waits for loading completion.
    /// </summary>
    private void TryCreateEditorPlacedWindow()
    {
        if (!isActiveAndEnabled || IsOpen)
            return;

        ConfigureWindow();
        if (Sara.IsReady)
        {
            Create3DWindow();
            ParentPanelToSettings();
            Window?.ForceDraw();
            return;
        }

        SubscribeToLoader();
    }

    /// <summary>
    /// Handles the loading complete event for editor placed windows.
    /// </summary>
    private void Loader_OnLoadingComplete()
    {
        UnsubscribeFromLoader();
        TryCreateEditorPlacedWindow();
    }

    /// <summary>
    /// Subscribes to loading completion when available.
    /// </summary>
    private void SubscribeToLoader()
    {
        if (_subscribedToLoader || Sara.Loader == null)
            return;

        Sara.Loader.OnLoadingComplete += Loader_OnLoadingComplete;
        _subscribedToLoader = true;
    }

    /// <summary>
    /// Unsubscribes from loading completion when needed.
    /// </summary>
    private void UnsubscribeFromLoader()
    {
        if (!_subscribedToLoader || Sara.Loader == null)
        {
            _subscribedToLoader = false;
            return;
        }

        Sara.Loader.OnLoadingComplete -= Loader_OnLoadingComplete;
        _subscribedToLoader = false;
    }


    /// <summary>
    /// Configures the manipulator state.
    /// </summary>
    private void ConfigureManipulator(Transform facingTarget)
    {
        Fu3DWindowManipulator manipulator = GetComponent<Fu3DWindowManipulator>();
        if (manipulator == null)
            manipulator = gameObject.AddComponent<Fu3DWindowManipulator>();

        manipulator.RuntimeMovable = true;
        manipulator.CreateGrabHandle = true;
        manipulator.GrabHandleWidthRatio = 0.45f;
        manipulator.GrabHandleMinWidth = 0.18f;
        manipulator.GrabHandleMaxWidth = 0.34f;
        manipulator.GrabHandleHeight = 0.035f;
        manipulator.GrabHandleEdgeOffset = 0.025f;
        manipulator.FacingTarget = facingTarget;
        manipulator.FaceTargetWhileDragging = true;
        manipulator.FaceTargetWhenUnanchored = false;
        manipulator.AnchorOnRelease = true;
        manipulator.StartAnchored = true;

        manipulator.GrabHandlePosition = Fu3DWindowManipulator.Fu3DWindowManipulatorHandlePosition.Bottom;
        manipulator.FacingConstraint = Fu3DWindowManipulator.Fu3DWindowFacingConstraint.YawOnly;
        manipulator.ApplyFacing();
    }

    /// <summary>
    /// Runs the place from source logic.
    /// </summary>
    private void PlaceFromSource(Transform source, Transform facingTarget, Vector3 sourceLocalOffset)
    {
        Transform target = facingTarget != null ? facingTarget : Camera.main != null ? Camera.main.transform : null;

        if (source != null)
        {
            transform.position = source.TransformPoint(sourceLocalOffset);
        }
        else if (target != null)
        {
            transform.position = target.position + target.forward * 0.45f - target.right * 0.24f - Vector3.up * 0.12f;
        }

        FaceTarget(target);
    }

    /// <summary>
    /// Runs the place at world position logic.
    /// </summary>
    private void PlaceAtWorldPosition(Vector3 worldPosition, Transform facingTarget)
    {
        Transform target = facingTarget != null ? facingTarget : Camera.main != null ? Camera.main.transform : null;
        transform.position = worldPosition;
        FaceTarget(target);
    }

    /// <summary>
    /// Runs the face target logic.
    /// </summary>
    private void FaceTarget(Transform target)
    {
        if (target == null)
            return;

        Vector3 up = Vector3.up;
        Vector3 panelForward = Vector3.ProjectOnPlane(transform.position - target.position, up);
        if (panelForward.sqrMagnitude <= 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(panelForward.normalized, up);
    }
}
