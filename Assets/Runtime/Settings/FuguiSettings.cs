using Fugui.Core.DearImGui;
using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fugui.Core
{
    [Serializable]
    /// <summary>
    /// This class contains settings for the Fugui Manager.
    /// </summary>
    public class FuguiSettings
    {
        /// <summary>
        /// A boolean value indicating whether UI windows should be internalized on mouse release.
        /// </summary>
        public bool InternalizeOnMouseRelease = true;
        /// <summary>
        /// The number of FPS to be used when the UI windows are not being manipulated.
        /// </summary>
        [Range(1, 30)]
        public int IdleFPS = 8;
        /// <summary>
        /// The number of FPS to be used when the UI windows are being manipulated.
        /// </summary>
        [Range(1, 90)]
        public int ManipulatingFPS = 60;
        /// <summary>
        /// The number of ticks to be used when the UI windows are being manipulated externally.
        /// </summary>
        [Range(1, 1000000)]
        public long ExternalManipulatingTicks = 1000;
        /// <summary>
        /// A boolean value indicating whether the title bar should be shown for externally manipulated UI windows.
        /// </summary>
        public UIExternalWindowFlags ExternalWindowFlags = UIExternalWindowFlags.Default;
        /// <summary>
        /// A boolean value indicating whether the debug panel should be drawn.
        /// </summary>
        public bool DrawDebugPanel = true;
        /// <summary>
        /// A list of key codes used to trigger externalization of UI windows.
        /// </summary>
        public List<KeyCode> ExternalizationKey;
        /// <summary>
        /// A list of key codes used to trigger internalization of UI windows.
        /// </summary>
        public List<OpenTK.Input.Key> InternalizationKey;
        /// <summary>
        /// The icon to be used for the maximize button.
        /// </summary>
        public Texture2D MaximizeIcon;
        /// <summary>
        /// The icon to be used for the minimize button.
        /// </summary>
        public Texture2D MinimizeIcon;
        /// <summary>
        /// The icon to be used for the top docking button.
        /// </summary>
        public Texture2D TopIcon;
        /// <summary>
        /// The icon to be used for the bottom docking button.
        /// </summary>
        public Texture2D BottomIcon;
        /// <summary>
        /// The icon to be used for the left docking button.
        /// </summary>
        public Texture2D LeftIcon;
        /// <summary>
        /// The icon to be used for the right docking button.
        /// </summary>
        public Texture2D RightIcon;
        /// <summary>
        /// The icon to be used for the center docking button.
        /// </summary>
        public Texture2D CenterIcon;
        /// <summary>
        /// The texture to be used when an OpenGL non-readable texture is loaded.
        /// </summary>
        public Texture2D OpenGLNonReadableTexture;
        /// <summary>
        /// The texture to be used as icon into an Info Modal.
        /// </summary>
        public Texture2D InfoIcon;
        /// <summary>
        /// The texture to be used as icon into a Warning Modal.
        /// </summary>
        public Texture2D WarningIcon;
        /// <summary>
        /// The texture to be used as icon into a Danger Modal.
        /// </summary>
        public Texture2D DangerIcon;
        /// <summary>
        /// The texture to be used as icon into a Success Modal.
        /// </summary>
        public Texture2D SuccessIcon;
        /// <summary>
        /// The position anchor of the notification panel
        /// </summary>
        public AnchorLocation NotificationAnchorPosition;
        /// <summary>
        /// Fugui Font configuration
        /// </summary>
        public FontConfig FontConfig;
        /// <summary>
        /// Fugui Themes folder (must be inside streaming assetes folder)
        /// </summary>
        [Tooltip("Fugui Themes folder (must be inside streaming assetes folder)")]
        public string ThemesFolder = "Fugui/Themes";
        [Tooltip("Do the Info/Success/Warning/Danger Modals colorize their buttons")]
        public bool StateModalsUseButtonColors = true;
        [Header("Docking")]
        [Tooltip("Docking flags for the main container dockSpace.")]
        public ImGuiDockNodeFlags DockingFlags = ImGuiDockNodeFlags.None;
        [Tooltip("Always display the Tab bar button.")]
        public bool DisplayTabBarButton = true;
        [Tooltip("Display the Tab bar button to the right of the tab bar.")]
        public bool TabBarButtonRight = true;
    }
}