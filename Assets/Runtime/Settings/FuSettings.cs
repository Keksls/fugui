using Fu.Core.DearImGui;
using Fu.Framework;
using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu.Core
{
    [Serializable]
    /// <summary>
    /// This class contains settings for the Fugui Manager.
    /// </summary>
    public class FuSettings
    {
        [Tooltip("A boolean value indicating whether UI windows should be internalized on mouse release.")]
        [FuTooltip("A boolean value indicating whether UI windows should be internalized on mouse release.")]
        [FuToggle]
        public bool InternalizeOnMouseRelease = true;

        [Tooltip("The number of FPS to be used when the UI windows are not being manipulated.")]
        [FuTooltip("The number of FPS to be used when the UI windows are not being manipulated.")]
        [Range(1, 30)]
        [FuSlider(1, 30)]
        public int IdleFPS = 8;

        [Tooltip("The number of FPS to be used when the UI windows are being manipulated.")]
        [FuTooltip("The number of FPS to be used when the UI windows are being manipulated.")]
        [Range(1, 90)]
        [FuSlider(1, 90)]
        public int ManipulatingFPS = 60;

        [Tooltip("The number of FPS to be used when the UI windows are being manipulated.")]
        [FuTooltip("The number of FPS to be used when the UI windows are being manipulated.")]
        [Range(1f, 100f)]
        [FuSlider(1f, 100f)]
        public float Windows3DScale = 10f;

        [Tooltip("The number of FPS to be used when the UI windows are being manipulated.")]
        [FuTooltip("The number of FPS to be used when the UI windows are being manipulated.")]
        [Range(0.5f, 4f)]
        [FuSlider(0.5f, 4f)]
        public float Windows3DSuperSampling = 2f;

        [Tooltip("The number of FPS to be used when the UI windows are being manipulated.")]
        [FuTooltip("The number of FPS to be used when the UI windows are being manipulated.")]
        [Range(1f, 10f)]
        [FuSlider(1f, 10f)]
        public float Windows3DFontScale = 2f;

        [Tooltip("Width of the UI 3D Panel.")]
        [FuTooltip("Width of the UI 3D Panel.")]
        [Range(0.0001f, 0.25f)]
        [FuSlider(0.0001f, 0.25f)]
        public float UIPanelWidth = 0.066f;

        [Tooltip("The number of ticks to be used when the UI windows are being manipulated externally.")]
        [FuTooltip("The number of ticks to be used when the UI windows are being manipulated externally.")]
        [Range(1, 1000000)]
        [FuSlider(1, 1000000)]
        public int ExternalManipulatingTicks = 1000;

        [Tooltip("FuPanelClipper safe range (offset to draw clipped elements outside of scrollRect bounds)")]
        [FuTooltip("FuPanelClipper safe range (offset to draw clipped elements outside of scrollRect bounds)")]
        [Range(-64, 64)]
        [FuSlider(-64, 64)]
        public int ClipperSafeRangePx = 8;

        [Tooltip("A boolean value indicating whether the title bar should be shown for externally manipulated UI windows.")]
        [FuTooltip("A boolean value indicating whether the title bar should be shown for externally manipulated UI windows.")]
        public FuExternalWindowFlags ExternalWindowFlags = FuExternalWindowFlags.Default;

        [Tooltip("A boolean value indicating whether the debug panel should be drawn")]
        [FuTooltip("A boolean value indicating whether the debug panel should be drawn")]
        [FuToggle]
        public bool DrawDebugPanel = true;

        [Tooltip("Duration of the modal activation animation in seconds")]
        [FuTooltip("Duration of the modal activation animation in seconds")]
        [Range(0f, 0.5f)]
        [FuSlider(0f, 0.5f)]
        public float ModalAnimationDuration = 0.2f;

        [Tooltip("The duration of the Elements animations in seconds (used for Toggles, Radiobuttons, etc)")]
        [FuTooltip("The duration of the Elements animations in seconds (used for Toggles, Radiobuttons, etc)")]
        [Range(0f, 0.5f)]
        [FuSlider(0f, 0.5f)]
        public float ElementsAnimationDuration = 0.1f;

        [Tooltip("Duration of the Notify activation/desactivation animation in seconds")]
        [FuTooltip("Duration of the Notify activation/desactivation animation in seconds")]
        [Range(0f, 0.5f)]
        [FuSlider(0f, 0.5f)]
        public float NotidyAnimlationDuration = 0.15f;

        [Tooltip("A list of key codes used to trigger externalization of UI windows.")]
        [FuTooltip("A list of key codes used to trigger externalization of UI windows.")]
        [FuHidden]
        public List<KeyCode> ExternalizationKey;

        [Tooltip("A list of key codes used to trigger internalization of UI windows.")]
        [FuTooltip("A list of key codes used to trigger internalization of UI windows.")]
        [FuHidden]
        public List<OpenTK.Input.Key> InternalizationKey;

        [Tooltip("The icon to be used for the maximize button.")]
        [FuTooltip("The icon to be used for the maximize button.")]
        [FuHidden]
        public Texture2D MaximizeIcon;

        [Tooltip("The icon to be used for the minimize button.")]
        [FuTooltip("The icon to be used for the minimize button.")]
        [FuHidden]
        public Texture2D MinimizeIcon;

        [Tooltip("The icon to be used for the top docking button.")]
        [FuTooltip("The icon to be used for the top docking button.")]
        [FuHidden]
        public Texture2D TopIcon;

        [Tooltip("The icon to be used for the bottom docking button.")]
        [FuTooltip("The icon to be used for the bottom docking button.")]
        [FuHidden]
        public Texture2D BottomIcon;

        [Tooltip("The icon to be used for the left docking button.")]
        [FuTooltip("The icon to be used for the left docking button.")]
        [FuHidden]
        public Texture2D LeftIcon;

        [Tooltip("The icon to be used for the right docking button.")]
        [FuTooltip("The icon to be used for the right docking button.")]
        [FuHidden]
        public Texture2D RightIcon;

        [Tooltip("The icon to be used for the center docking button.")]
        [FuTooltip("The icon to be used for the center docking button.")]
        [FuHidden]
        public Texture2D CenterIcon;

        [Tooltip("The texture to be used when an OpenGL non-readable texture is loaded.")]
        [FuTooltip("The texture to be used when an OpenGL non-readable texture is loaded.")]
        [FuHidden]
        public Texture2D OpenGLNonReadableTexture;

        [Tooltip("The texture to be used as icon into an Info.")]
        [FuTooltip("The texture to be used as icon into an Info.")]
        [FuHidden]
        public Texture2D InfoIcon;

        [Tooltip("The texture to be used as icon into a Warning.")]
        [FuTooltip("The texture to be used as icon into a Warning.")]
        [FuHidden]
        public Texture2D WarningIcon;

        [Tooltip("The texture to be used as icon into a Danger.")]
        [FuTooltip("The texture to be used as icon into a Danger.")]
        [FuHidden]
        public Texture2D DangerIcon;

        [Tooltip("The texture to be used as icon into a Success.")]
        [FuTooltip("The texture to be used as icon into a Success.")]
        [FuHidden]
        public Texture2D SuccessIcon;

        [Tooltip("The texture of the FuguiLogo.")]
        [FuTooltip("The texture of the FuguiLogo.")]
        [FuHidden]
        public Texture2D FuguiLogo;

        [Tooltip("Material of the UI Panel")]
        [FuTooltip("Material of the UI Panel")]
        [FuHidden]
        public Material UIPanelMaterial;

        [Tooltip("Material of the 3D UI")]
        [FuTooltip("Material of the 3D UI")]
        [FuHidden]
        public Material UIMaterial;

        [Header("Notifications")]
        [Tooltip("The position anchor of the notification panel")]
        [FuTooltip("The position anchor of the notification panel")]
        public AnchorLocation NotificationAnchorPosition;

        [Tooltip("Size of the state Icon in notifications title")]
        [FuTooltip("Size of the state Icon in notifications title")]
        [FuSlider(0f, 64f)]
        public float NotifyIconSize = 16f;

        [Tooltip("Width of the notifications Panel")]
        [FuTooltip("Width of the notifications Panel")]
        [FuSlider(128f, 1420f)]
        public float NotifyPanelWidth = 420f;

        [Tooltip("Default duration of a notification (in second)")]
        [FuTooltip("Default duration of a notification (in second)")]
        [FuSlider(2f, 60f)]
        public float NotificationDefaultDuration = 10f;

        [Tooltip("Fugui Font configuration")]
        [FuTooltip("Fugui Font configuration")]
        [FuHidden]
        public FontConfig FontConfig;

        [Tooltip("Layer that Fugui will use to raycast 3D UI")]
        [FuTooltip("Layer that Fugui will use to raycast 3D UI")]
        public LayerMask UILayer = 0;

        [Tooltip("Maximum distance for Fugui to manipulate a 3D window")]
        [FuTooltip("Maximum distance for Fugui to manipulate a 3D window")]
        public float UIRaycastDistance = 50f;

        [Tooltip("Fugui Themes folder (must be inside streaming assetes folder)")]
        [FuTooltip("Fugui Themes folder (must be inside streaming assetes folder)")]
        public string ThemesFolder = "Fugui/Themes";

        [Tooltip("Do the Info/Success/Warning/Danger Modals colorize their buttons")]
        [FuTooltip("Do the Info/Success/Warning/Danger Modals colorize their buttons")]
        [FuToggle]
        public bool StateModalsUseButtonColors = true;

        [Header("Docking")]
        [Tooltip("Docking flags for the main container dockSpace.")]
        [FuTooltip("Docking flags for the main container dockSpace.")]
        public ImGuiDockNodeFlags DockingFlags = ImGuiDockNodeFlags.None;

        [Tooltip("Always display the Tab bar button.")]
        [FuTooltip("Always display the Tab bar button.")]
        [FuToggle]
        public bool DisplayTabBarButton = true;

        [Tooltip("Display the Tab bar button to the right of the tab bar.")]
        [FuTooltip("Display the Tab bar button to the right of the tab bar.")]
        [FuToggle]
        public bool TabBarButtonRight = true;

        [Tooltip("Fugui Themes folder (must be inside streaming assetes folder)")]
        [FuTooltip("Fugui Themes folder (must be inside streaming assetes folder)")]
        public string LayoutsFolder = "Fugui/Layouts";
    }
}