using Assets.Scripts.UI.Windows.Common;
using Fu;
using Fu.Framework;

using UnityEngine;

/// <summary>
/// Draws the multiplayer admin widget inside an editor placed XR window.
/// </summary>
public class AdminWindowXR : EditorPlacedXRWindowBehaviour
{
    #region State
    [SerializeField]
    private TimelineWidgetTheme timelineTheme;
    private readonly AdminPanelWidget _adminPanelWidget = new AdminPanelWidget();
    #endregion

    #region Window callbacks
    /// <summary>
    /// Applies XR window flags after Fugui creates the admin panel.
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
    /// Draws the admin panel content.
    /// </summary>
    public override void OnUI(FuWindow window, FuLayout layout)
    {
        _adminPanelWidget.SetTheme(ResolveTimelineTheme());
        Rect panelRect = window.LocalRect;
        if (_adminPanelWidget.Draw(panelRect, 1f))
            gameObject.SetActive(false);
    }
    #endregion

    #region Configuration
    /// <summary>
    /// Configures the concrete Fugui window identity and static flags.
    /// </summary>
    protected override void ConfigureEditorPlacedWindow()
    {
        SetWindowName(FuWindowsNames.AdminXR);
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
}
