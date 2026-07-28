using Assets.Scripts.UI.Windows.Common;
using Fu;
using Fu.Framework;

using Saravr.Network.Common;
using System;
using UnityEngine;

/// <summary>
/// Draws the VR belt Fugui widget inside an editor placed XR window.
/// </summary>
public class VRBelt3DWindow : EditorPlacedXRWindowBehaviour
{
    #region State
    [SerializeField]
    private TimelineWidgetTheme timelineTheme;
    [SerializeField]
    private VRBeltPanelSwitcher panelSwitcher;
    [SerializeField]
    private VRBeltWidgetButton[] buttons;
    private readonly VRBeltWidget _beltWidget = new VRBeltWidget();
    private VRBeltWidgetButton[] _visibleButtons = Array.Empty<VRBeltWidgetButton>();
    private VRBeltWidgetButton[] _visibleButtonSource;
    private bool _visibleButtonsIncludeVoice;
    private bool _visibleButtonsIncludeAdmin;
    #endregion

    #region Unity lifecycle
    /// <summary>
    /// Fills default belt buttons when the component is first added.
    /// </summary>
    private void Reset()
    {
        ResolvePanelSwitcher();
        buttons = new[]
        {
            CreateButton(VRBeltPanelId.Timeline, "T", new Color(0.14f, 0.18f, 0.22f, 1f)),
            CreateButton(VRBeltPanelId.Settings, "S", new Color(0.04f, 0.52f, 0.58f, 1f)),
            CreateButton(VRBeltPanelId.Voice, "V", new Color(0.10f, 0.42f, 0.84f, 1f)),
            CreateButton(VRBeltPanelId.Admin, "A", new Color(0.88f, 0.28f, 0.20f, 1f)),
        };
    }
    #endregion

    #region Window callbacks
    /// <summary>
    /// Applies XR window flags after Fugui creates the belt panel.
    /// </summary>
    public override void OnWindowCreated(FuWindow window)
    {
        window.IsInterractable = true;
        window.AddWindowFlag(FuWindowStyleFlags.NoDecoration);
        window.AddWindowFlag(FuWindowStyleFlags.NoMove);
        window.AddWindowFlag(FuWindowStyleFlags.NoScrollWithMouse);
        window.AddWindowFlag(FuWindowStyleFlags.NoScrollbar);
        ParentPanelToAnchor();
    }

    /// <summary>
    /// Draws the VR belt toolbar and dispatches panel selection to the switcher.
    /// </summary>
    public override void OnUI(FuWindow window, FuLayout layout)
    {
        VRBeltPanelSwitcher switcher = ResolvePanelSwitcher();
        VRBeltPanelId activePanel = switcher != null && switcher.HasActivePanel
            ? switcher.ActivePanel
            : VRBeltPanelId.Timeline;
        bool voicePanelAvailable = IsVoicePanelAvailable();
        bool adminPanelAvailable = IsAdminPanelAvailable();
        if (!IsPanelAvailable(activePanel, voicePanelAvailable, adminPanelAvailable))
        {
            if (switcher != null)
                switcher.ShowPanel(VRBeltPanelId.Timeline);

            activePanel = VRBeltPanelId.Timeline;
        }

        VRBeltWidgetButton[] visibleButtons = ResolveVisibleButtons(voicePanelAvailable, adminPanelAvailable);

        _beltWidget.SetTheme(ResolveTimelineTheme());
        _beltWidget.Draw(
            new Rect(Fugui.GetWindowPos(), Fugui.GetWindowSize()),
            activePanel,
            visibleButtons,
            ShowPanel);
    }
    #endregion

    #region Configuration
    /// <summary>
    /// Configures the concrete Fugui window identity and static flags.
    /// </summary>
    protected override void ConfigureEditorPlacedWindow()
    {
        SetWindowName(FuWindowsNames.VRBeltXR);
        _windowFlags = FuWindowFlags.NoExternalization | FuWindowFlags.NoDocking | FuWindowFlags.NoDockingOverMe;
        _runtimeResizable = false;
        _scaleFontWithContainer = true;
    }

    /// <summary>
    /// Resolves the configured timeline theme.
    /// </summary>
    private TimelineWidgetTheme ResolveTimelineTheme()
    {
        if (timelineTheme == null)
            timelineTheme = TimelineWidgetTheme.LoadDefault();

        return timelineTheme;
    }
    #endregion

    #region Panel switching
    /// <summary>
    /// Shows the requested panel through the assigned switcher.
    /// </summary>
    private void ShowPanel(VRBeltPanelId panelId)
    {
        if (!IsPanelAvailable(panelId, IsVoicePanelAvailable(), IsAdminPanelAvailable()))
            return;

        VRBeltPanelSwitcher switcher = ResolvePanelSwitcher();
        if (switcher != null)
            switcher.ShowPanel(panelId);
    }

    /// <summary>
    /// Resolves the currently visible belt buttons.
    /// </summary>
    private VRBeltWidgetButton[] ResolveVisibleButtons(bool includeVoice, bool includeAdmin)
    {
        if (buttons == null || buttons.Length == 0)
            return Array.Empty<VRBeltWidgetButton>();

        if (_visibleButtonSource == buttons && _visibleButtonsIncludeVoice == includeVoice && _visibleButtonsIncludeAdmin == includeAdmin)
            return _visibleButtons;

        _visibleButtonSource = buttons;
        _visibleButtonsIncludeVoice = includeVoice;
        _visibleButtonsIncludeAdmin = includeAdmin;
        _visibleButtons = BuildVisibleButtons(includeVoice, includeAdmin);
        return _visibleButtons;
    }

    /// <summary>
    /// Builds the belt button list for the current permission state.
    /// </summary>
    private VRBeltWidgetButton[] BuildVisibleButtons(bool includeVoice, bool includeAdmin)
    {
        int visibleCount = 0;
        for (int i = 0; i < buttons.Length; i++)
        {
            if (IsButtonVisible(buttons[i].PanelId, includeVoice, includeAdmin))
                visibleCount++;
        }

        VRBeltWidgetButton[] visibleButtons = new VRBeltWidgetButton[visibleCount];
        int visibleIndex = 0;
        for (int i = 0; i < buttons.Length; i++)
        {
            // Voice and admin are intentionally removed when the current session cannot use them.
            if (!IsButtonVisible(buttons[i].PanelId, includeVoice, includeAdmin))
                continue;

            visibleButtons[visibleIndex] = buttons[i];
            visibleIndex++;
        }

        return visibleButtons;
    }

    /// <summary>
    /// Returns whether a belt button should be visible for the current permission state.
    /// </summary>
    private static bool IsButtonVisible(VRBeltPanelId panelId, bool includeVoice, bool includeAdmin)
    {
        return IsPanelAvailable(panelId, includeVoice, includeAdmin);
    }

    /// <summary>
    /// Returns whether the requested panel can be used for the current permission state.
    /// </summary>
    private static bool IsPanelAvailable(VRBeltPanelId panelId, bool voicePanelAvailable, bool adminPanelAvailable)
    {
        if (panelId == VRBeltPanelId.Voice)
            return voicePanelAvailable;

        if (panelId == VRBeltPanelId.Admin)
            return adminPanelAvailable;

        return true;
    }

    /// <summary>
    /// Returns whether the local user can access the voice panel.
    /// </summary>
    private static bool IsVoicePanelAvailable()
    {
        return Sara.CurrentSession != null && Sara.CurrentSession.IsMultiplayer;
    }

    /// <summary>
    /// Returns whether the local user can access the admin panel.
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
    /// Resolves the belt panel switcher from the inspector or parent hierarchy.
    /// </summary>
    private VRBeltPanelSwitcher ResolvePanelSwitcher()
    {
        if (panelSwitcher != null)
            return panelSwitcher;

        panelSwitcher = GetComponentInParent<VRBeltPanelSwitcher>(true);
        return panelSwitcher;
    }

    /// <summary>
    /// Creates a default belt button configuration.
    /// </summary>
    private static VRBeltWidgetButton CreateButton(VRBeltPanelId panelId, string fallbackLabel, Color background)
    {
        return new VRBeltWidgetButton
        {
            PanelId = panelId,
            FallbackLabel = fallbackLabel,
            Background = background,
        };
    }
    #endregion
}
