// define it to debug whatever Color or Styles are pushed (avoid stack leak metrics)
// it's ressourcefull, si comment it when debug is done. Ensure it's commented before build.
//#define FUDEBUG
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR && !FUMOBILE
#define FUMOBILE
#endif
using Fu.Framework;
#if FU_EXTERNALIZATION
#endif
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Fugui state and events.
    /// </summary>
    public static partial class Fugui
    {
        /// <summary>
        /// The current Context Fugui is drawing on
        /// </summary>
        public static FuContext CurrentContext { get; internal set; }
        /// <summary>
        /// The current scale of the UI (based on current context scale)
        /// </summary>
        public static float Scale => CurrentContext != null ? CurrentContext.Scale : 1f;
        /// <summary>
        /// All registered contexts
        /// </summary>
        public static Dictionary<int, FuContext> Contexts { get; internal set; } = new Dictionary<int, FuContext>();
        /// <summary>
        /// context fugui need to delete from now
        /// </summary>
        internal static Queue<int> ToDeleteContexts { get; private set; } = new Queue<int>();
        /// <summary>
        /// The settings for the Fugui Manager
        /// </summary>
        public static FuSettings Settings { get; internal set; }
        /// <summary>
        /// The current time
        /// </summary>
        public static float Time { get; internal set; }
        /// <summary>
        /// The static main UI container (this is the unity default 3D view)
        /// </summary>
        public static FuMainWindowContainer DefaultContainer { get; internal set; }
        /// <summary>
        /// Whether the fullscreen main UI container is rendered and receives global flat input.
        /// </summary>
        public static bool MainContainerEnabled
        {
            get => _mainContainerEnabled;
            set
            {
                _mainContainerEnabled = value;
                ApplyMainContainerCameraState();
            }
        }
        /// <summary>
        /// Default Fugui Context (it's the main unity context)
        /// </summary>
        public static FuUnityContext DefaultContext { get; internal set; }
        /// <summary>
        /// The static dictionary of UI windows
        /// </summary>
        public static Dictionary<string, FuWindow> UIWindows { get; internal set; }
        /// <summary>
        /// The static dictionary of UI window definitions
        /// </summary>
        public static Dictionary<FuWindowName, FuWindowDefinition> UIWindowsDefinitions { get; internal set; }
        /// <summary>
        /// A boolean value indicating whether a window has been render this frame
        /// </summary>
        public static bool HasRenderWindowThisFrame { get; internal set; } = false;
        /// <summary>
        /// Whatever Fugui is allowed to set mouse cursor icon
        /// </summary>
        public static bool IsCursorLocked { get; internal set; } = false;
        /// <summary>
        /// The Fugui Theme Manager instance
        /// </summary>
        public static FuThemeManager Themes { get; private set; }
        /// <summary>
        /// The Fugui Docking Layout Manager instance
        /// </summary>
        public static FuDockingLayoutManager Layouts { get; private set; }
        /// <summary>
        /// The Fugui Layout of the current context
        /// </summary>
        public static FuLayout ContextLayout => CurrentContext != null ? CurrentContext.contextLayout : null;

        /// <summary>
        /// FuGui Controller instance
        /// </summary>
        internal static FuController Controller;

        /// <summary>
        /// counter of color push
        /// </summary>
        internal static int NbPushColor { get; private set; } = 0;
        /// <summary>
        /// counter of style push
        /// </summary>
        internal static int NbPushStyle { get; private set; } = 0;
        /// <summary>
        /// counter of font push
        /// </summary>
        internal static int NbPushFont { get; private set; } = 0;
        /// <summary>
        /// The ID of the window in wich a popup is open (if there is some)
        /// </summary>
        internal static List<string> PopUpWindowsIDs { get; private set; } = new List<string>();
        /// <summary>
        /// The ID of the currently open pop-up (if there is some)
        /// </summary>
        internal static List<string> PopUpIDs { get; private set; } = new List<string>();
        /// <summary>
        /// The Rect of the currently open pop-up (if there is some)
        /// </summary>
        internal static List<Rect> PopUpRects { get; private set; } = new List<Rect>();
        /// <summary>
        /// A flag indicating whether the layout is inside a pop-up.
        /// </summary>
        internal static List<bool> IsPopupDrawing { get; private set; } = new List<bool>();
        /// <summary>
        /// A flag indicating whether the popup has focus
        /// </summary>
        internal static List<bool> IsPopupFocused { get; private set; } = new List<bool>();
        /// <summary>
        /// Whatever cursors has just been unlocked
        /// </summary>
        internal static bool CursorsJustUnlocked = false;
        /// <summary>
        /// Whatever a 3D window has been hovered this frame (used for input management between 3D windows and main container)
        /// </summary>
        internal static bool HasHovered3DWindowThisFrame;
        /// <summary>
        /// True while a Fugui chrome handle owns the current frame pointer input.
        /// </summary>
        internal static bool WindowInputsBlockedThisFrame { get; private set; }
        /// <summary>
        /// Number of windows currently being dragged.
        /// </summary>
        internal static int WindowDraggingCount { get; private set; }
        /// <summary>
        /// Number of windows currently being resized.
        /// </summary>
        internal static int WindowResizingCount { get; private set; }
        /// <summary>
        /// Number of windows currently hovered.
        /// </summary>
        internal static int WindowHoveredCount { get; private set; }
        /// <summary>
        /// Number of windows currently hovered on their content area.
        /// </summary>
        internal static int WindowHoveredContentCount { get; private set; }
        /// <summary>
        /// Number of windows currently focused by ImGui.
        /// </summary>
        internal static int WindowFocusedCount { get; private set; }
        /// <summary>
        /// Number of windows currently requesting keyboard/text input capture.
        /// </summary>
        internal static int WindowWantCaptureInputCount { get; private set; }
        /// <summary>
        /// Number of overlays currently being dragged.
        /// </summary>
        internal static int OverlayDraggingCount { get; private set; }
        /// <summary>
        /// Number of drag-drop payloads currently active across contexts.
        /// </summary>
        internal static int DraggingPayloadCount { get; private set; }
        private const int WindowInputBlockMouseButtonCount = 5;
        private static readonly bool[] _blockedFrameRawMouseDown = new bool[WindowInputBlockMouseButtonCount];
        private static readonly bool[] _blockedFrameRawMousePressed = new bool[WindowInputBlockMouseButtonCount];
        private static readonly bool[] _blockedInputHeldFromOutside = new bool[WindowInputBlockMouseButtonCount];
        private static readonly bool[] _blockedInputDownEmitted = new bool[WindowInputBlockMouseButtonCount];
        private static bool _windowInputSnapshotCaptured;
        private static readonly bool[] _inputSnapshotMouseDown = new bool[WindowInputBlockMouseButtonCount];
        private static readonly bool[] _inputSnapshotMouseClicked = new bool[WindowInputBlockMouseButtonCount];
        private static readonly bool[] _inputSnapshotMouseReleased = new bool[WindowInputBlockMouseButtonCount];
        private static readonly bool[] _inputSnapshotMouseDoubleClicked = new bool[WindowInputBlockMouseButtonCount];
        private static readonly bool[] _inputSnapshotMouseDownOwned = new bool[WindowInputBlockMouseButtonCount];
        private static readonly bool[] _inputSnapshotMouseDownOwnedUnlessPopupClose = new bool[WindowInputBlockMouseButtonCount];
        private static readonly ushort[] _inputSnapshotMouseClickedCount = new ushort[WindowInputBlockMouseButtonCount];
        private static readonly ushort[] _inputSnapshotMouseClickedLastCount = new ushort[WindowInputBlockMouseButtonCount];
        private static readonly float[] _inputSnapshotMouseDownDuration = new float[WindowInputBlockMouseButtonCount];
        private static readonly float[] _inputSnapshotMouseDownDurationPrev = new float[WindowInputBlockMouseButtonCount];
        private static readonly Vector2[] _inputSnapshotMouseDragMaxDistanceAbs = new Vector2[WindowInputBlockMouseButtonCount];
        private static readonly float[] _inputSnapshotMouseDragMaxDistanceSqr = new float[WindowInputBlockMouseButtonCount];
        // The dictionary of 3D windows
        private static Dictionary<string, Fu3DWindowContainer> _3DWindows;
        // dictionary of external windows
#if FU_EXTERNALIZATION
        internal static Dictionary<string, FuExternalWindowContainer> ExternalWindows = new Dictionary<string, FuExternalWindowContainer>();
        internal static SDLEventRooter SDLEventRooter { get; private set; } = new SDLEventRooter();
        private static bool _hasExternalWindowUpdateLoopOverride;
        private static bool _runInBackgroundBeforeExternalWindows;
        /// <summary>
        /// The absolute mouse position on the monitor (used for multi context / multi window support)
        /// </summary>
        public static Vector2Int AbsoluteMonitorMousePosition { get; internal set; }
#endif
        // counter of Fugui Contexts
        private static int _contextID = 0;
        // queue of callback to execute BEFORE default render
        private static Queue<Action> _beforeDefaultRenderStack = new Queue<Action>();
        // queue of callback to execute AFTER default render
        private static Queue<Action> _afterDefaultRenderStack = new Queue<Action>();
        // queue of callback to execute after the currently rendered context
        private static Queue<Action> _afterCurrentRenderContextStack = new Queue<Action>();
        // stack of action we will want to execute into unity main thread
        private static Queue<Action> _executeInMainThreadActionsStack = new Queue<Action>();

        private static bool _mainContainerEnabled = true;
        private static bool _mainContainerCameraStateStored;
        private static bool _mainContainerCameraHadAdditionalCameraData;
        private static bool _mainContainerCameraAllowXRRendering;
        private static int _mainContainerCameraCullingMask;
        private static CameraClearFlags _mainContainerCameraClearFlags;
        private static Color _mainContainerCameraBackgroundColor;
        private static RenderTexture _mainContainerCameraTargetTexture;
        private static RenderTexture _offscreenDriverTexture;
        private static float _targetScale = -1f;
        private static float _targetFontScale = -1f;

        /// <summary>
        /// Event invoken whenever an exception happend within the UI render loop
        /// </summary>
        public static event Action<Exception> OnUIException;

        #region Methods
        /// <summary>
        /// Fire the UI exception event
        /// </summary>
        /// <param name="ex">exception of the event</param>
        internal static void Fire_OnUIException(Exception ex) => OnUIException?.Invoke(ex);

        internal static void TrackWindowDragging(bool active) => WindowDraggingCount = AddInputOwnershipCount(WindowDraggingCount, active);
        internal static void TrackWindowResizing(bool active) => WindowResizingCount = AddInputOwnershipCount(WindowResizingCount, active);
        internal static void TrackWindowHovered(bool active) => WindowHoveredCount = AddInputOwnershipCount(WindowHoveredCount, active);
        internal static void TrackWindowHoveredContent(bool active) => WindowHoveredContentCount = AddInputOwnershipCount(WindowHoveredContentCount, active);
        internal static void TrackWindowFocused(bool active) => WindowFocusedCount = AddInputOwnershipCount(WindowFocusedCount, active);
        internal static void TrackWindowWantCaptureInput(bool active) => WindowWantCaptureInputCount = AddInputOwnershipCount(WindowWantCaptureInputCount, active);
        internal static void TrackOverlayDragging(bool active) => OverlayDraggingCount = AddInputOwnershipCount(OverlayDraggingCount, active);
        internal static void TrackDraggingPayload(bool active) => DraggingPayloadCount = AddInputOwnershipCount(DraggingPayloadCount, active);

        private static int AddInputOwnershipCount(int count, bool active)
        {
            return Math.Max(0, count + (active ? 1 : -1));
        }

        internal static int ApplyGlobalFPSLimit(int fps, bool zeroMeansUnlimited = false)
        {
            int targetFPS = zeroMeansUnlimited && fps <= 0 ? int.MaxValue : Math.Max(0, fps);
            int maxFPS = Settings != null ? Settings.MaxFPS : 0;
            if (maxFPS <= 0 || targetFPS <= 0)
            {
                return targetFPS;
            }

            return Math.Min(targetFPS, maxFPS);
        }

        internal static float GetDeltaTimeForFPS(int fps)
        {
            return fps <= 0 ? float.PositiveInfinity : 1f / fps;
        }

        internal static bool ShouldPublishContextDrawData(FuContext context)
        {
            int targetFPS = GetContextFPSLimit(context);
            return targetFPS <= 0 || Time >= context.LastPublishedDrawDataTime + GetDeltaTimeForFPS(targetFPS);
        }

        internal static void MarkContextDrawDataPublished(FuContext context)
        {
            if (context != null)
            {
                context.LastPublishedDrawDataTime = Time;
            }
        }

        private static int GetContextFPSLimit(FuContext context)
        {
            if (Settings == null || context == null)
            {
                return 0;
            }

            int targetFPS = Math.Max(0, Settings.MaxFPS);
            if (Settings.ManipulatingFPS > 0 && IsContextManipulating(context))
            {
                targetFPS = targetFPS > 0
                    ? Math.Min(targetFPS, Settings.ManipulatingFPS)
                    : Settings.ManipulatingFPS;
            }

            return targetFPS;
        }

        private static bool IsContextManipulating(FuContext context)
        {
            if (UIWindows == null)
            {
                return false;
            }

            foreach (FuWindow window in UIWindows.Values)
            {
                if (window?.Container?.Context == context && window.State == FuWindowState.Manipulating)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ResetInputOwnershipCounters()
        {
            WindowDraggingCount = 0;
            WindowResizingCount = 0;
            WindowHoveredCount = 0;
            WindowHoveredContentCount = 0;
            WindowFocusedCount = 0;
            WindowWantCaptureInputCount = 0;
            OverlayDraggingCount = 0;
            DraggingPayloadCount = 0;
        }
        #endregion

        #region State
        /// <summary>
        /// Event invoken whenever a FuWindow is externalized from main container to external window container
        /// </summary>
        public static event Action<FuWindow> OnWindowExternalized;
        #endregion

        #region Methods
        /// <summary>
        /// Fire the Event invoken whenever a FuWindow is externalized from main container to external window container
        /// </summary>
        /// <param name="window">FuWindow that has just been externalized</param>
        internal static void Fire_OnWindowExternalized(FuWindow window) => OnWindowExternalized?.Invoke(window);
        #endregion

        #region State
        /// <summary>
        /// Whenever a window is resized
        /// </summary>
        public static event Action<FuWindow> OnWindowResized;
        /// <summary>
        /// Whenever a window is closed
        /// </summary>
        public static event Action<FuWindow> OnWindowClosed;
        /// <summary>
        /// Whenever a window is docked
        /// </summary>
        public static event Action<FuWindow> OnWindowDocked;
        /// <summary>
        /// Whenever a window is undocked
        /// </summary>
        public static event Action<FuWindow> OnWindowUnDocked;
        /// <summary>
        /// Whenever a window is added to a container
        /// </summary>
        public static event Action<FuWindow> OnWindowAddToContainer;
        /// <summary>
        /// Whenever a window is removed from a container
        /// </summary>
        public static event Action<FuWindow> OnWindowRemovedFromContainer;
        #endregion

        #region Methods
        /// <summary>
        /// Fire event whenever a window is resized
        /// </summary>
        internal static void Fire_OnWindowResized(FuWindow window)
        {
            OnWindowResized?.Invoke(window);
        }
        /// <summary>
        /// Fire event whenever a window is closed
        /// </summary>
        internal static void Fire_OnWindowClosed(FuWindow window)
        {
            OnWindowClosed?.Invoke(window);
        }
        /// <summary>
        /// Fire event whenever a window is docked
        /// </summary>
        internal static void Fire_OnWindowDocked(FuWindow window)
        {
            OnWindowDocked?.Invoke(window);
        }
        /// <summary>
        /// Fire event whenever a window is undocked
        /// </summary>
        internal static void Fire_OnWindowUnDocked(FuWindow window)
        {
            OnWindowUnDocked?.Invoke(window);
        }
        /// <summary>
        /// Fire event whenever a window is added to a container
        /// </summary>
        internal static void Fire_OnWindowAddToContainer(FuWindow window)
        {
            OnWindowAddToContainer?.Invoke(window);
        }
        /// <summary>
        /// Fire event whenever a window is removed from a container
        /// </summary>
        internal static void Fire_OnWindowRemovedFromContainer(FuWindow window)
        {
            OnWindowRemovedFromContainer?.Invoke(window);
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the Fugui class.
        /// </summary>
        static Fugui()
        {
            Initialize(null, null, null);
        }
        #endregion
    }
}
