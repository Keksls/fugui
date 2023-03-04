using Fu.Core.DearImGui;
using Fu.Core.DearImGui.Assets;
using Fu.Core.DearImGui.Platform;
using Fu.Core.DearImGui.Renderer;
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
        [Tooltip("Path of the class that will store the FuWindow Names.")]
        [FuTooltip("Path of the class that will store the FuWindow Names.")]
        public string FUGUI_WINDOWS_DEF_ENUM_PATH = "Assets\\Runtime\\Settings\\FuWindowsNames.cs";

        [Tooltip("Path of the folder that will store the Fugui layouts.")]
        [FuTooltip("Path of the folder that will store the Fugui layouts.")]
        public string FUGUI_DOCKSPACE_FOLDER_PATH = "Assets\\Runtime\\Settings\\Layout\\";

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

        [Tooltip("The number of Camera FPS to be used when the UI windows are Idle.")]
        [FuTooltip("The number of Camera FPS to be used when the UI windows are Idle.")]
        [Range(1, 90)]
        [FuSlider(1, 90)]
        public int IdleCameraFPS = 4;

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
        [FuImage]
        public Texture2D MaximizeIcon;

        [Tooltip("The icon to be used for the minimize button.")]
        [FuTooltip("The icon to be used for the minimize button.")]
        [FuImage]
        public Texture2D MinimizeIcon;

        [Tooltip("The icon to be used for the top docking button.")]
        [FuTooltip("The icon to be used for the top docking button.")]
        [FuImage]
        public Texture2D TopIcon;

        [Tooltip("The icon to be used for the bottom docking button.")]
        [FuTooltip("The icon to be used for the bottom docking button.")]
        [FuImage]
        public Texture2D BottomIcon;

        [Tooltip("The icon to be used for the left docking button.")]
        [FuTooltip("The icon to be used for the left docking button.")]
        [FuImage]
        public Texture2D LeftIcon;

        [Tooltip("The icon to be used for the right docking button.")]
        [FuTooltip("The icon to be used for the right docking button.")]
        [FuImage]
        public Texture2D RightIcon;

        [Tooltip("The icon to be used for the center docking button.")]
        [FuTooltip("The icon to be used for the center docking button.")]
        [FuImage]
        public Texture2D CenterIcon;

        [Tooltip("The texture to be used when an OpenGL non-readable texture is loaded.")]
        [FuTooltip("The texture to be used when an OpenGL non-readable texture is loaded.")]
        [FuImage]
        public Texture2D OpenGLNonReadableTexture;

        [Tooltip("The texture to be used as icon into an Info.")]
        [FuTooltip("The texture to be used as icon into an Info.")]
        [FuImage]
        public Texture2D InfoIcon;

        [Tooltip("The texture to be used as icon into a Warning.")]
        [FuTooltip("The texture to be used as icon into a Warning.")]
        [FuImage]
        public Texture2D WarningIcon;

        [Tooltip("The texture to be used as icon into a Danger.")]
        [FuTooltip("The texture to be used as icon into a Danger.")]
        [FuImage]
        public Texture2D DangerIcon;

        [Tooltip("The texture to be used as icon into a Success.")]
        [FuTooltip("The texture to be used as icon into a Success.")]
        [FuImage]
        public Texture2D SuccessIcon;

        [Tooltip("The texture of the FuguiLogo.")]
        [FuTooltip("The texture of the FuguiLogo.")]
        [FuImage]
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
        public FuOverlayAnchorLocation NotificationAnchorPosition;

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

        [Tooltip("size offset of icons glyphs in font. Be carefull, it this value exceed font size, it may crash on awake")]
        [FuTooltip("size offset of icons glyphs in font. \nBe carefull, it this value exceed font size, it may crash on awake")]
        [FuSlider(-16f, 16f)]
        public float FontIconsSizeOffset = 4;

        [Tooltip("pos offset of icons glyphs in font.")]
        [FuTooltip("pos offset of icons glyphs in font.")]
        public Vector2 FontIconsOffset = Vector2.zero;

        [Tooltip("renderType to use to render imgui DrawLists (for main container).")]
        [FuTooltip("renderType to use to render imgui DrawLists (for main container).")]
        public RenderType RendererType = RenderType.Mesh;

        [Tooltip("platform (input type) to use for main container.")]
        [FuTooltip("platform (input type) to use for main container.")]
        public InputType PlatformType = InputType.InputManager;

        [Tooltip("For more info look the imgui.h:1380(~). (default=NavEnableKeyboard | DockingEnable)")]
        [FuTooltip("For more info look the imgui.h:1380(~). (default=NavEnableKeyboard | DockingEnable)")]
        public ImGuiConfigFlags ImGuiConfig = ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.DockingEnable;

        [Tooltip("Time for a double-click, in seconds. (default=0.30f)")]
        [FuTooltip("Time for a double-click, in seconds. (default=0.30f)")]
        [Range(0.01f, 1f)]
        [FuSlider(0.01f, 1f)]
        public float DoubleClickTime = 0.3f;

        [Tooltip("Distance threshold to stay in to validate a double-click, in pixels. (default=6.0f)")]
        [FuTooltip("Distance threshold to stay in to validate a double-click, in pixels. (default=6.0f)")]
        [Range(0.01f, 64f)]
        [FuSlider(0.1f, 64f)]
        public float DoubleClickMaxDist = 6.0f;

        [Tooltip("Distance threshold before considering we are dragging. (default=6.0f)")]
        [FuTooltip("Distance threshold before considering we are dragging. (default=6.0f)")]
        [Range(0.01f, 64f)]
        [FuSlider(0.1f, 64f)]
        public float DragThreshold = 6.0f;

        [Tooltip("When holding a key/button, time before it starts repeating, in seconds. (default=0.250f)")]
        [FuTooltip("When holding a key/button, time before it starts repeating, in seconds. (default=0.250f)")]
        [Range(0.01f, 1f)]
        [FuSlider(0.1f, 1f)]
        public float KeyRepeatDelay = 0.25f;

        [Tooltip("When holding a key/button, rate at which it repeats, in seconds. (default=0.050f)")]
        [FuTooltip("When holding a key/button, rate at which it repeats, in seconds. (default=0.050f)")]
        [Range(0.01f, 1f)]
        [FuSlider(0.1f, 1f)]
        public float KeyRepeatRate = 0.05f;

        [Tooltip("Global scale all fonts. (default=1.0f)")]
        [FuTooltip("Global scale all fonts. (default=1.0f)")]
        [Range(0.1f, 4f)]
        [FuSlider(0.1f, 4f)]
        public float FontGlobalScale = 1.0f;

        [Tooltip("Allow user scaling text of individual window with CTRL+Wheel. (default=false)")]
        [FuTooltip("Allow user scaling text of individual window with CTRL+Wheel. (default=false)")]
        [FuToggle]
        public bool FontAllowUserScaling = false;

        [Tooltip("[TEST] For retina display or other situations where window coordinates are different from framebuffer coordinates. " +
            "This generally ends up in ImDrawData::FramebufferScale. (default=1, 1)")]
        [FuTooltip("[TEST] For retina display or other situations where window coordinates are different from framebuffer coordinates. \n" +
            "This generally ends up in ImDrawData::FramebufferScale. (default=1, 1)")]
        public Vector2 DisplayFramebufferScale = Vector2.one;

        [Tooltip("Request ImGui to draw a mouse cursor for you (if you are on a platform without a mouse cursor). " +
            "Cannot be easily renamed to 'io.ConfigXXX' because this is frequently used by backend implementations. " +
            "(default=false)")]
        [FuTooltip("Request ImGui to draw a mouse cursor for you (if you are on a platform without a mouse cursor). \n" +
            "Cannot be easily renamed to 'io.ConfigXXX' because this is frequently used by backend implementations. \n" +
            "(default=false)")]
        [FuToggle]
        public bool MouseDrawCursor = false;

        [Tooltip("Set to false to disable blinking cursor.")]
        [FuTooltip("Set to false to disable blinking cursor.")]
        [FuToggle]
        public bool TextCursorBlink = false;

        [Tooltip("Enable resizing from the edges and from the lower-left corner.")]
        [FuTooltip("Enable resizing from the edges and from the lower-left corner.")]
        [FuToggle]
        public bool ResizeFromEdges = true;

        [Tooltip("Set to true to only allow moving windows when clicked+dragged from the title bar. Windows without a title bar are not affected.")]
        [FuTooltip("Set to true to only allow moving windows when clicked+dragged from the title bar. \nWindows without a title bar are not affected.")]
        [FuToggle]
        public bool MoveFromTitleOnly = true;

        [Tooltip("Compact window memory usage when unused. Set to -1.0f to disable.")]
        [FuTooltip("Compact window memory usage when unused. Set to -1.0f to disable.")]
        [Range(-1f, 10f)]
        [FuSlider(-1f, 10f)]
        public float ConfigMemoryCompactTimer = 1f;

        [Tooltip("Simplified docking mode: disable window splitting, so docking is limited to merging multiple windows together into tab-bars.")]
        [FuTooltip("Simplified docking mode: disable window splitting, so docking is limited to merging multiple windows together into tab-bars.")]
        [FuToggle]
        public bool ConfigDockingNoSplit = false;

        [Tooltip("[BETA] [FIXME: This currently creates regression with auto-sizing and general overhead] " +
            "Make every single floating window display within a docking node.")]
        [FuTooltip("[BETA] [FIXME: This currently creates regression with auto-sizing and general overhead] \n" +
            "Make every single floating window display within a docking node.")]
        [FuToggle]
        public bool ConfigDockingAlwaysTabBar = true;

        [Tooltip("[BETA] Make window or viewport transparent when docking and only display docking boxes on the target viewport. " +
            "Useful if rendering of multiple viewport cannot be synced. Best used with ConfigViewportsNoAutoMerge.")]
        [FuTooltip("[BETA] Make window or viewport transparent when docking and only display docking boxes on the target viewport. \n" +
            "Useful if rendering of multiple viewport cannot be synced. Best used with ConfigViewportsNoAutoMerge.")]
        [FuToggle]
        public bool ConfigDockingTransparentPayload = false;

        // Store your own data for retrieval by callbacks
        [FuHidden]
        [NonSerialized]
        public IntPtr UserData;

        // shaders to use to render imgui (main container)
        [FuHidden]
        public ShaderResourcesAsset Shaders = null;

        // cursors pack to use
        [FuHidden]
        public CursorShapesAsset CursorShapes = null;

        // URP renderer, not used for now. Keep it for URP eventualy
        // unHide this is using URP
        [HideInInspector]
        [FuHidden]
        public RenderImGui Render;

        /// <summary>
        /// Apply Imgui IO config variables to the given Imgui IO
        /// </summary>
        /// <param name="io">current Imgui IO</param>
        public void ApplyTo(ImGuiIOPtr io)
        {
            io.ConfigFlags = ImGuiConfig;

            io.MouseDoubleClickTime = DoubleClickTime;
            io.MouseDoubleClickMaxDist = DoubleClickMaxDist;
            io.MouseDragThreshold = DragThreshold;

            io.KeyRepeatDelay = KeyRepeatDelay;
            io.KeyRepeatRate = KeyRepeatRate;

            io.FontGlobalScale = FontGlobalScale;
            io.FontAllowUserScaling = FontAllowUserScaling;

            io.DisplayFramebufferScale = DisplayFramebufferScale;
            io.MouseDrawCursor = MouseDrawCursor;

            io.ConfigDockingNoSplit = ConfigDockingNoSplit;
            io.ConfigDockingAlwaysTabBar = ConfigDockingAlwaysTabBar;
            io.ConfigDockingTransparentPayload = ConfigDockingTransparentPayload;

            io.ConfigInputTextCursorBlink = TextCursorBlink;
            io.ConfigWindowsResizeFromEdges = ResizeFromEdges;
            io.ConfigWindowsMoveFromTitleBarOnly = MoveFromTitleOnly;
            io.ConfigMemoryCompactTimer = ConfigMemoryCompactTimer;

            io.UserData = UserData;
        }
    }
}