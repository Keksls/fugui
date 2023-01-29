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
        /// <summary>
        /// A boolean value indicating whether UI windows should be internalized on mouse release.
        /// </summary>
        [Toggle]
        public bool InternalizeOnMouseRelease = true;
        /// <summary>
        /// The number of FPS to be used when the UI windows are not being manipulated.
        /// </summary>
        [Range(1, 30)]
        [Slider(1, 30)]
        public int IdleFPS = 8;
        /// <summary>
        /// The number of FPS to be used when the UI windows are being manipulated.
        /// </summary>
        [Range(1, 90)]
        [Slider(1, 90)]
        public int ManipulatingFPS = 60;
        /// <summary>
        /// The number of FPS to be used when the UI windows are being manipulated.
        /// </summary>
        [Range(1f, 100f)]
        [Slider(1f, 100f)]
        public float Windows3DScale = 10f;
        /// <summary>
        /// The number of FPS to be used when the UI windows are being manipulated.
        /// </summary>
        [Range(0.5f, 4f)]
        [Slider(0.5f, 4f)]
        public float Windows3DSuperSampling = 2f;
        /// <summary>
        /// The number of FPS to be used when the UI windows are being manipulated.
        /// </summary>
        [Range(1f, 10f)]
        [Slider(1f, 10f)]
        public float Windows3DFontScale = 2f;
        [Range(0.0001f, 0.25f)]
        [Slider(0.0001f, 0.25f)]
        public float UIPanelWidth = 0.066f;
        /// <summary>
        /// The number of ticks to be used when the UI windows are being manipulated externally.
        /// </summary>
        [Range(1, 1000000)]
        [Slider(1, 1000000)]
        public int ExternalManipulatingTicks = 1000;
        /// <summary>
        /// A boolean value indicating whether the title bar should be shown for externally manipulated UI windows.
        /// </summary>
        public FuExternalWindowFlags ExternalWindowFlags = FuExternalWindowFlags.Default;
        /// <summary>
        /// A boolean value indicating whether the debug panel should be drawn.
        /// </summary>
        [Toggle]
        public bool DrawDebugPanel = true;
        /// <summary>
        /// A list of key codes used to trigger externalization of UI windows.
        /// </summary>
        [Hidden]
        public List<KeyCode> ExternalizationKey;
        /// <summary>
        /// A list of key codes used to trigger internalization of UI windows.
        /// </summary>
        [Hidden]
        public List<OpenTK.Input.Key> InternalizationKey;
        /// <summary>
        /// The icon to be used for the maximize button.
        /// </summary>
        [Hidden]
        public Texture2D MaximizeIcon;
        /// <summary>
        /// The icon to be used for the minimize button.
        /// </summary>
        [Hidden]
        public Texture2D MinimizeIcon;
        /// <summary>
        /// The icon to be used for the top docking button.
        /// </summary>
        [Hidden]
        public Texture2D TopIcon;
        /// <summary>
        /// The icon to be used for the bottom docking button.
        /// </summary>
        [Hidden]
        public Texture2D BottomIcon;
        /// <summary>
        /// The icon to be used for the left docking button.
        /// </summary>
        [Hidden]
        public Texture2D LeftIcon;
        /// <summary>
        /// The icon to be used for the right docking button.
        /// </summary>
        [Hidden]
        public Texture2D RightIcon;
        /// <summary>
        /// The icon to be used for the center docking button.
        /// </summary>
        [Hidden]
        public Texture2D CenterIcon;
        /// <summary>
        /// The texture to be used when an OpenGL non-readable texture is loaded.
        /// </summary>
        [Hidden]
        public Texture2D OpenGLNonReadableTexture;
        /// <summary>
        /// The texture to be used as icon into an Info.
        /// </summary>
        [Hidden]
        public Texture2D InfoIcon;
        /// <summary>
        /// The texture to be used as icon into a Warning.
        /// </summary>
        [Hidden]
        public Texture2D WarningIcon;
        /// <summary>
        /// The texture to be used as icon into a Danger.
        /// </summary>
        [Hidden]
        public Texture2D DangerIcon;
        /// <summary>
        /// The texture to be used as icon into a Success.
        /// </summary>
        [Hidden]
        public Texture2D SuccessIcon;
        /// <summary>
        /// The texture of the FuguiLogo.
        /// </summary>
        [Hidden]
        public Texture2D FuguiLogo;
        [Hidden]
        public Material UIPanelMaterial;
        [Hidden]
        public Material UIMaterial;
        [Header("Notifications")]
        [Tooltip("The position anchor of the notification panel")]
        public AnchorLocation NotificationAnchorPosition;
        [Tooltip("Size of the state Icon in notifications title")]
        [Slider(0f, 64f)]
        public float NotifyIconSize = 16f;
        [Tooltip("Width of the notifications Panel")]
        [Slider(128f, 1420f)]
        public float NotifyPanelWidth = 420f;
        [Tooltip("Default duration of a notification (in second)")]
        [Slider(2f, 60f)]
        public float NotificationDefaultDuration = 10f;
        /// <summary>
        /// Fugui Font configuration
        /// </summary>
        [Hidden]
        public FontConfig FontConfig;
        /// <summary>
        /// Layer name used for the UI
        /// </summary>
        [Tooltip("Layer that Fugui will use to raycast 3D UI")]
        public LayerMask UILayer = 0;
        [Tooltip("Maximum distance for Fugui to manipulate a 3D window")]
        public float UIRaycastDistance = 50f;
        /// <summary>
        /// Fugui Themes folder (must be inside streaming assetes folder)
        /// </summary>
        [Tooltip("Fugui Themes folder (must be inside streaming assetes folder)")]
        public string ThemesFolder = "Fugui/Themes";
        [Tooltip("Do the Info/Success/Warning/Danger Modals colorize their buttons")]
        [Toggle]
        public bool StateModalsUseButtonColors = true;
        [Header("Docking")]
        [Tooltip("Docking flags for the main container dockSpace.")]
        public ImGuiDockNodeFlags DockingFlags = ImGuiDockNodeFlags.None;
        [Tooltip("Always display the Tab bar button.")]
        [Toggle]
        public bool DisplayTabBarButton = true;
        [Tooltip("Display the Tab bar button to the right of the tab bar.")]
        [Toggle]
        public bool TabBarButtonRight = true;
        /// <summary>
        /// Fugui Layout folder (must be inside streaming assetes folder)
        /// </summary>
        [Tooltip("Fugui Themes folder (must be inside streaming assetes folder)")]
        public string LayoutsFolder = "Fugui/Layouts";
    }
}