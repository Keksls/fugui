// define it to debug whatever Color or Styles are pushed (avoid stack leak metrics)
// it's ressourcefull, si comment it when debug is done. Ensure it's commented before build.
//#define FUDEBUG
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR && !FUMOBILE
#define FUMOBILE
#endif
using Fu.Framework;
using ImGuiNET;
#if FU_EXTERNALIZATION
using SDL2;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;

namespace Fu
{
    /// <summary>
    /// Represents the Fugui type.
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
        /// Reserved texture identifier used to carry backdrop blur draw commands through ImGui draw lists.
        /// </summary>
        internal static readonly IntPtr BackdropTextureID = new IntPtr(-67001);

        private static readonly Stack<FuBackdropStyle> _backdropStack = new Stack<FuBackdropStyle>();

        /// <summary>
        /// Whatever cursors has just been unlocked
        /// </summary>
        internal static bool CursorsJustUnlocked = false;
        /// <summary>
        /// Whatever a 3D window has been hovered this frame (used for input management between 3D windows and main container)
        /// </summary>
        internal static bool HasHovered3DWindowThisFrame;
        // The dictionary of 3D windows
        private static Dictionary<string, Fu3DWindowContainer> _3DWindows;
        // dictionary of external windows
#if FU_EXTERNALIZATION
        internal static Dictionary<string, FuExternalWindowContainer> ExternalWindows = new Dictionary<string, FuExternalWindowContainer>();
        internal static SDLEventRooter SDLEventRooter { get; private set; } = new SDLEventRooter();
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

        private const ushort MIN_DUOTONE_GLYPH_RANGE = 60543;
        private const ushort MAX_DUOTONE_GLYPH_RANGE = 63743;

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

        /// <summary>
        /// Initialize FuGui and create Main Container
        /// </summary>
        /// <param name="mainContainerUICamera">Camera that will display UI of main container</param>
        public static void Initialize(FuSettings settings, FuController controller, Camera mainContainerUICamera, bool enableMainContainer = true)
        {
            Settings = settings;
            Controller = controller;
            MainContainerEnabled = enableMainContainer;
            // instantiate UIWindows 
            UIWindows = new Dictionary<string, FuWindow>();
            UIWindowsDefinitions = new Dictionary<FuWindowName, FuWindowDefinition>();
            // init dic and queue
            _3DWindows = new Dictionary<string, Fu3DWindowContainer>();
            Themes = new FuThemeManager();
            Layouts = new FuDockingLayoutManager();
            // handle native ImGui assert handler
            ImGuiAssertHandler.Initialize();
            // prepare context menu
            ResetContextMenu(true);

            // prevnt null ref
            if (settings == null || controller == null || mainContainerUICamera == null)
                return;

            // create Default Fugui Context and initialize themeManager
            DefaultContext = CreateUnityContext(mainContainerUICamera, Settings.GlobalScale, Settings.FontGlobalScale, Fugui.Themes.Initialize);

            // need to be called into start, because it will use ImGui context and we need to wait to create it from UImGui Awake
            DefaultContainer = new FuMainWindowContainer(DefaultContext);
            DefaultContainer.SetContainerScaleConfig(GetDefaultContainerScaleConfig());
            ApplyMainContainerCameraState();

            // register Fugui Settings Window
            new FuWindowDefinition(FuSystemWindowsNames.FuguiSettings, DrawSettings, size: new Vector2Int(256, 256), flags: FuWindowFlags.Default | FuWindowFlags.AllowMultipleWindow);

#if FUDEBUG
            // initialize debug tool if debug is enabled
            initDebugTool();
#endif
        }

        /// <summary>
        /// Applies the main container state to the dedicated fullscreen UI camera.
        /// </summary>
        private static void ApplyMainContainerCameraState()
        {
            Camera camera = DefaultContext != null ? DefaultContext.Camera : null;

            if (camera == null)
            {
                return;
            }

            if (_mainContainerEnabled)
            {
                RestoreMainContainerCameraState(camera);
                camera.enabled = true;
                return;
            }

            if (!HasOffscreenDriverWork())
            {
                RestoreMainContainerCameraState(camera);
                camera.enabled = false;
                return;
            }

            ConfigureMainContainerCameraAsOffscreenDriver(camera);
        }

        /// <summary>
        /// Returns true when the camera is the hidden non-XR camera used to render offscreen contexts.
        /// </summary>
        /// <param name="camera">Camera tested by the render feature.</param>
        /// <returns>True when the camera should drive offscreen Fugui render textures.</returns>
        internal static bool IsOffscreenDriverCamera(Camera camera)
        {
            return !_mainContainerEnabled &&
                   HasOffscreenDriverWork() &&
                   camera != null &&
                   DefaultContext != null &&
                   ReferenceEquals(camera, DefaultContext.Camera);
        }

        /// <summary>
        /// Returns true when offscreen UI render textures need a camera-driven URP pass.
        /// </summary>
        /// <returns>True when a 3D window is currently registered.</returns>
        private static bool HasOffscreenDriverWork()
        {
            return _3DWindows != null && _3DWindows.Count > 0;
        }

        /// <summary>
        /// Store user-authored camera state before temporarily using the UI camera as an offscreen driver.
        /// </summary>
        /// <param name="camera">Camera to snapshot.</param>
        private static void StoreMainContainerCameraState(Camera camera)
        {
            if (_mainContainerCameraStateStored || camera == null)
            {
                return;
            }

            _mainContainerCameraTargetTexture = camera.targetTexture;
            _mainContainerCameraCullingMask = camera.cullingMask;
            _mainContainerCameraClearFlags = camera.clearFlags;
            _mainContainerCameraBackgroundColor = camera.backgroundColor;

            UniversalAdditionalCameraData additionalCameraData = camera.GetComponent<UniversalAdditionalCameraData>();
            _mainContainerCameraHadAdditionalCameraData = additionalCameraData != null;
            _mainContainerCameraAllowXRRendering = additionalCameraData == null || additionalCameraData.allowXRRendering;
            _mainContainerCameraStateStored = true;
        }

        /// <summary>
        /// Restore the UI camera when the fullscreen main container is enabled again.
        /// </summary>
        /// <param name="camera">Camera to restore.</param>
        private static void RestoreMainContainerCameraState(Camera camera)
        {
            if (!_mainContainerCameraStateStored || camera == null)
            {
                return;
            }

            camera.targetTexture = _mainContainerCameraTargetTexture;
            camera.cullingMask = _mainContainerCameraCullingMask;
            camera.clearFlags = _mainContainerCameraClearFlags;
            camera.backgroundColor = _mainContainerCameraBackgroundColor;
            SetCameraXRRendering(camera, _mainContainerCameraAllowXRRendering);
            RemoveTemporaryCameraData(camera);
            _mainContainerCameraStateStored = false;
        }

        /// <summary>
        /// Use the UI camera as an invisible non-XR render driver for 3D window render textures.
        /// </summary>
        /// <param name="camera">Camera to configure.</param>
        private static void ConfigureMainContainerCameraAsOffscreenDriver(Camera camera)
        {
            StoreMainContainerCameraState(camera);

            camera.targetTexture = GetOrCreateOffscreenDriverTexture();
            camera.cullingMask = 0;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.enabled = true;
            SetCameraXRRendering(camera, false);
        }

        /// <summary>
        /// Enable or disable XR rendering on a URP camera when the component exists.
        /// </summary>
        /// <param name="camera">Camera to configure.</param>
        /// <param name="allowXRRendering">Whether URP may render this camera through XR.</param>
        private static void SetCameraXRRendering(Camera camera, bool allowXRRendering)
        {
            UniversalAdditionalCameraData additionalCameraData = camera != null ? camera.GetComponent<UniversalAdditionalCameraData>() : null;
            if (additionalCameraData == null && camera != null && !allowXRRendering)
            {
                additionalCameraData = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }

            if (additionalCameraData != null)
            {
                additionalCameraData.allowXRRendering = allowXRRendering;
            }
        }

        /// <summary>
        /// Remove runtime-added URP camera data when restoring a camera that did not originally own it.
        /// </summary>
        /// <param name="camera">Camera to restore.</param>
        private static void RemoveTemporaryCameraData(Camera camera)
        {
            if (_mainContainerCameraHadAdditionalCameraData || camera == null)
            {
                return;
            }

            UniversalAdditionalCameraData additionalCameraData = camera.GetComponent<UniversalAdditionalCameraData>();
            if (additionalCameraData != null)
            {
                UnityEngine.Object.Destroy(additionalCameraData);
            }
        }

        /// <summary>
        /// Gets the tiny target used to keep the offscreen driver camera away from the GameView.
        /// </summary>
        /// <returns>The hidden offscreen driver render texture.</returns>
        private static RenderTexture GetOrCreateOffscreenDriverTexture()
        {
            if (_offscreenDriverTexture != null &&
                _offscreenDriverTexture.IsCreated() &&
                _offscreenDriverTexture.depthStencilFormat != GraphicsFormat.None)
            {
                return _offscreenDriverTexture;
            }

            ReleaseOffscreenDriverTexture();

            RenderTextureDescriptor descriptor = new RenderTextureDescriptor(1, 1)
            {
                graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm,
                depthStencilFormat = GraphicsFormat.D16_UNorm,
                msaaSamples = 1,
                useMipMap = false,
                autoGenerateMips = false,
                useDynamicScale = false
            };

            _offscreenDriverTexture = new RenderTexture(descriptor)
            {
                name = "Fugui Offscreen Driver",
                hideFlags = HideFlags.HideAndDontSave
            };
            _offscreenDriverTexture.Create();
            return _offscreenDriverTexture;
        }

        /// <summary>
        /// Release the offscreen driver target.
        /// </summary>
        private static void ReleaseOffscreenDriverTexture()
        {
            if (_offscreenDriverTexture == null)
            {
                return;
            }

            _offscreenDriverTexture.Release();
            UnityEngine.Object.Destroy(_offscreenDriverTexture);
            _offscreenDriverTexture = null;
        }

        /// <summary>
        /// Build the default scale configuration used by main and external containers.
        /// </summary>
        /// <returns>The default container scale configuration.</returns>
        public static FuContainerScaleConfig GetDefaultContainerScaleConfig()
        {
            if (Settings == null)
            {
                return FuContainerScaleConfig.Disabled(1f, 1f);
            }

            if (!Settings.EnableContainerScaler)
            {
                return FuContainerScaleConfig.Disabled(Settings.GlobalScale, Settings.FontGlobalScale);
            }

            return FuContainerScaleConfig.Reference(
                Settings.ContainerReferenceResolution,
                Settings.ContainerMatchWidthOrHeight,
                Settings.ContainerMinScale,
                Settings.ContainerMaxScale,
                Settings.GlobalScale,
                Settings.FontGlobalScale,
                Settings.ContainerScaleFonts,
                Settings.ContainerUseDpiScale,
                Settings.ContainerReferenceDpi
            );
        }

        /// <summary>
        /// Returns the get legacy3 dwindow settings result.
        /// </summary>
        /// <param name="scaleConfig">The scale Config value.</param>
        /// <param name="scale3D">The scale3 D value.</param>
        /// <returns>The result of the operation.</returns>
        private static Fu3DWindowSettings getLegacy3DWindowSettings(FuContainerScaleConfig? scaleConfig, float? scale3D)
        {
            float contextScale = Settings != null ? Settings.Windows3DSuperSampling : 1f;
            float fontScale = Settings != null ? Settings.Windows3DFontScale : 1f;
            float windows3DScale = scale3D.HasValue
                ? Mathf.Max(0.0001f, scale3D.Value)
                : (Settings != null ? Mathf.Max(0.0001f, Settings.Windows3DScale) : 10f);
            Vector2Int resolution = new Vector2Int(512, 512);
            Vector2 panelSize = new Vector2(
                resolution.x / contextScale * windows3DScale / 1000f,
                resolution.y / contextScale * windows3DScale / 1000f);

            float panelDepth = Settings != null ? Mathf.Max(0.0001f, Settings.UIPanelWidth) : 0.01f;
            Fu3DWindowSettings settings = Fu3DWindowSettings.FixedResolution(panelSize, resolution, contextScale, fontScale, panelDepth);
            if (scaleConfig.HasValue)
            {
                settings.ContainerScaleConfig = scaleConfig.Value;
            }
            return settings;
        }

        /// <summary>
        /// Update Fugui Windows Data (Externalizations and add/remove)
        /// Need to be called into MainThread (Update / Late Update / Coroutine)
        /// </summary>
        public static void Update()
        {
#if FUDEBUG
            // prepare debug new frame
            newFrame();
#endif
            // set shared time
            Time = UnityEngine.Time.unscaledTime;

            // execute mainThread actions stack
            while (_executeInMainThreadActionsStack.Count > 0)
            {
                _executeInMainThreadActionsStack.Dequeue()?.Invoke();
            }
        }

        /// <summary>
        /// Disposes the external windows and stops the render thread.
        /// </summary>
        public static void Dispose()
        {
            RestoreMainContainerCameraState(DefaultContext != null ? DefaultContext.Camera : null);
            ReleaseOffscreenDriverTexture();

#if FU_EXTERNALIZATION
            // close all external windows
            var externalWindowIDs = ExternalWindows.Keys.ToList();
            foreach (string windowID in externalWindowIDs)
            {
                ((FuExternalContext)ExternalWindows[windowID].Context).Window.Close(null);
            }
#endif
            // Dispose Fugui Contexts
            var ids = Contexts.Keys.ToList();
            foreach (int contextID in ids)
            {
                DestroyContext(contextID);
            }

#if FU_EXTERNALIZATION
            SDL.SDL_Quit();
#endif
        }

        /// <summary>
        /// Lock fugui auto set cursor icons
        /// </summary>
        public static void LockCursors()
        {
            IsCursorLocked = true;
        }

        /// <summary>
        /// Unlock fugui auto set cursor icons
        /// </summary>
        public static void UnlockCursors()
        {
            IsCursorLocked = false;
            CursorsJustUnlocked = true;
        }

        /// <summary>
        /// Execute an action into main thread, will be raised on next Fugui.Update call
        /// </summary>
        /// <param name="callback">callback you want to raise into unity's main thread</param>
        public static void ExecuteInMainThread(Action callback)
        {
            _executeInMainThreadActionsStack.Enqueue(callback);
        }

        /// <summary>
        /// Execute a callback after a quick async waiting routine
        /// </summary>
        /// <param name="callback">callback to execute</param>
        /// <param name="sleep">time to wait before execute</param>
        public static void ExecuteCallbackAfterAsyncSleep(Action callback, float sleep)
        {
            Controller.StartCoroutine(executeCallbackAfterAsyncSleep_Routine(callback, sleep));
        }

        /// <summary>
        /// Execute a callback after a quick async waiting routine
        /// </summary>
        /// <param name="callback">callback to execute</param>
        /// <param name="sleep">time to wait before execute</param>
        /// <returns>awaiter</returns>
        private static IEnumerator executeCallbackAfterAsyncSleep_Routine(Action callback, float sleep)
        {
            yield return new WaitForSeconds(sleep); // <= remove that shit
            callback?.Invoke();
        }

        /// <summary>
        /// Adds an UI window to be in 3D context.
        /// </summary>
        /// <param name="uiWindow">The UI window to be display in 3D.</param>
        public static Fu3DWindowContainer Add3DWindow(FuWindow uiWindow, Fu3DWindowSettings settings, Vector3? position = null, Quaternion? rotation = null)
        {
            if (uiWindow == null)
            {
                Debug.Log("You are trying to create a 3D context to draw a null window.");
                return null;
            }

            if (_3DWindows.TryGetValue(uiWindow.ID, out Fu3DWindowContainer existingContainer))
            {
                existingContainer.Set3DWindowSettings(settings);
                return existingContainer;
            }

            Fu3DWindowContainer container = new Fu3DWindowContainer(uiWindow, settings, position, rotation);
            _3DWindows.Add(uiWindow.ID, container);
            ApplyMainContainerCameraState();
            return container;
        }

        /// <summary>
        /// Prewarms heavy resources used by 3D windows over multiple frames.
        /// </summary>
        /// <param name="fontScales">Font scales to prewarm. Null uses baked atlas scales from settings when available.</param>
        /// <param name="renderTextureSizes">Render texture sizes to prewarm.</param>
        /// <returns>Coroutine enumerator.</returns>
        public static IEnumerator Prewarm3DWindowResources(IEnumerable<float> fontScales = null, IEnumerable<Vector2Int> renderTextureSizes = null)
        {
            FontConfig fontConfig = Settings?.FontConfig;
            if (fontScales == null && fontConfig != null && fontConfig.BakedFontAtlasScales != null)
            {
                fontScales = fontConfig.BakedFontAtlasScales;
            }

            if (fontConfig != null && fontScales != null)
            {
                foreach (float fontScale in fontScales)
                {
                    FuSharedFontAtlasCache.Prewarm(fontConfig, fontScale, Application.streamingAssetsPath);
                    yield return null;
                }
            }

            if (renderTextureSizes != null)
            {
                foreach (Vector2Int renderTextureSize in renderTextureSizes)
                {
                    Fu3DWindowContainer.PrewarmRenderTexture(renderTextureSize);
                    yield return null;
                }
            }
        }

        /// <summary>
        /// Adds a 3D window after prewarming the matching font atlas and render texture over multiple frames.
        /// </summary>
        /// <param name="uiWindow">The UI window to be display in 3D.</param>
        /// <param name="settings">3D window settings.</param>
        /// <param name="onCreated">Callback invoked with the created container.</param>
        /// <param name="position">World 3D position of this container.</param>
        /// <param name="rotation">World 3D rotation of this container.</param>
        /// <returns>Coroutine enumerator.</returns>
        public static IEnumerator Add3DWindowAsync(FuWindow uiWindow, Fu3DWindowSettings settings, Action<Fu3DWindowContainer> onCreated, Vector3? position = null, Quaternion? rotation = null)
        {
            settings.Sanitize();
            yield return Prewarm3DWindowResources(new[] { settings.FontScale }, new[] { settings.Resolution });
            yield return null;

            onCreated?.Invoke(Add3DWindow(uiWindow, settings, position, rotation));
        }

        /// <summary>
        /// Adds a UI window to a 3D panel with an explicit panel size and fixed render resolution.
        /// </summary>
        /// <param name="uiWindow">The UI window to display in 3D.</param>
        /// <param name="panelSize">The world/local size of the 3D panel.</param>
        /// <param name="renderResolution">The render texture and ImGui context resolution.</param>
        /// <param name="position">World 3D position of this container.</param>
        /// <param name="rotation">World 3D rotation of this container.</param>
        /// <param name="scaleConfig">Optional container scaler configuration.</param>
        /// <param name="contextScale">Base context scale.</param>
        /// <param name="fontScale">Base font scale.</param>
        /// <param name="matchPanelAspect">When true, resize changes keep the base render area but adapt the render ratio to the panel ratio.</param>
        /// <param name="panelDepth">Depth of the generated panel extrusion.</param>
        /// <param name="panelCurve">Horizontal panel curve angle in degrees.</param>
        /// <param name="panelRounding">Panel corner radius in world units.</param>
        /// <param name="createExtrudedPanelMesh">Whether to create the optional extruded backing mesh.</param>
        public static Fu3DWindowContainer Add3DWindow(FuWindow uiWindow, Vector2 panelSize, Vector2Int renderResolution, Vector3? position = null, Quaternion? rotation = null, FuContainerScaleConfig? scaleConfig = null, float contextScale = 1f, float fontScale = 1f, bool matchPanelAspect = true, float panelDepth = 0.01f, float panelCurve = 0f, float panelRounding = Fu3DWindowSettings.DefaultPanelRounding, bool createExtrudedPanelMesh = true)
        {
            Fu3DWindowSettings settings = matchPanelAspect
                ? Fu3DWindowSettings.FixedResolutionMatchingPanelAspect(
                    panelSize,
                    renderResolution,
                    panelSize,
                    contextScale,
                    fontScale,
                    panelDepth: panelDepth,
                    panelCurve: panelCurve,
                    panelRounding: panelRounding,
                    createExtrudedPanelMesh: createExtrudedPanelMesh)
                : Fu3DWindowSettings.FixedResolution(
                    panelSize,
                    renderResolution,
                    contextScale,
                    fontScale,
                    panelDepth,
                    panelCurve,
                    panelRounding,
                    createExtrudedPanelMesh);

            if (scaleConfig.HasValue)
            {
                settings.ContainerScaleConfig = scaleConfig.Value;
            }

            return Add3DWindow(uiWindow, settings, position, rotation);
        }

        /// <summary>
        /// Adds a UI window to a 3D panel whose render resolution follows panel size from a reference size.
        /// </summary>
        public static Fu3DWindowContainer Add3DWindowScaledWithPanel(FuWindow uiWindow, Vector2 panelSize, Vector2Int referenceResolution, Vector2 referencePanelSize, Vector3? position = null, Quaternion? rotation = null, FuContainerScaleConfig? scaleConfig = null, float contextScale = 1f, float fontScale = 1f, Vector2Int? minResolution = null, Vector2Int? maxResolution = null, float panelDepth = 0.01f, float panelCurve = 0f, float panelRounding = Fu3DWindowSettings.DefaultPanelRounding, bool createExtrudedPanelMesh = true)
        {
            Fu3DWindowSettings settings = Fu3DWindowSettings.ScaledResolutionWithPanel(
                panelSize,
                referenceResolution,
                referencePanelSize,
                contextScale,
                fontScale,
                minResolution,
                maxResolution,
                panelDepth,
                panelCurve,
                panelRounding,
                createExtrudedPanelMesh);

            if (scaleConfig.HasValue)
            {
                settings.ContainerScaleConfig = scaleConfig.Value;
            }

            return Add3DWindow(uiWindow, settings, position, rotation);
        }

        /// <summary>
        /// Returns the add3 dwindow result.
        /// </summary>
        /// <param name="uiWindow">The ui Window value.</param>
        /// <param name="position">The position value.</param>
        /// <param name="rotation">The rotation value.</param>
        /// <param name="scaleConfig">The scale Config value.</param>
        /// <param name="scale3D">The scale3 D value.</param>
        /// <returns>The result of the operation.</returns>
        [Obsolete("Use Add3DWindow(FuWindow, Fu3DWindowSettings, ...) to provide panel size and render resolution explicitly.")]
        public static Fu3DWindowContainer Add3DWindow(FuWindow uiWindow, Vector3? position = null, Quaternion? rotation = null, FuContainerScaleConfig? scaleConfig = null, float? scale3D = null)
        {
            return Add3DWindow(uiWindow, getLegacy3DWindowSettings(scaleConfig, scale3D), position, rotation);
        }

        /// <summary>
        /// Removes a 3D window with the specified Window.
        /// </summary>
        /// <param name="uiWindow">The 3D window to be removed.</param>
        internal static void Remove3DWindow(FuWindow uiWindow)
        {
            // Close the 3D window and remove it from the list
            Remove3DWindow(uiWindow.ID);
        }

        /// <summary>
        /// Removes an external window with the specified ID.
        /// </summary>
        /// <param name="id">The ID of the external window to be removed.</param>
        internal static void Remove3DWindow(string id)
        {
            // Check if a 3D window with the specified ID exists
            if (!_3DWindows.ContainsKey(id))
            {
                return;
            }
            // Close the 3D window and remove it from the list
            Fu3DWindowContainer container = _3DWindows[id];
            _3DWindows.Remove(id);
            if (container.Window != null)
            {
                container.Window.Close();
            }
            else
            {
                container.Close();
            }
        }

        /// <summary>
        /// Remove a 3D window container from Fugui registry without closing it again.
        /// </summary>
        /// <param name="id">Window ID associated with the 3D container.</param>
        internal static void Unregister3DWindow(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            _3DWindows.Remove(id);
            ApplyMainContainerCameraState();
        }

        /// <summary>
        /// Registers a window definition.
        /// </summary>
        /// <param name="windowDefinition">The window definition to be registered.</param>
        /// <returns>True if the window definition was successfully registered, false otherwise.</returns>
        public static bool RegisterWindowDefinition(FuWindowDefinition windowDefinition)
        {
            // Check if a window definition with the same ID already exists
            if (UIWindowsDefinitions.ContainsKey(windowDefinition.WindowName))
            {
                return false;
            }
            // Add the window definition to the list
            UIWindowsDefinitions.Add(windowDefinition.WindowName, windowDefinition);
            return true;
        }

        /// <summary>
        /// Unregisters a window definition.
        /// </summary>
        /// <param name="windowDefinition">The window definition to be unregistered.</param>
        /// <returns>True if the window definition was successfully unregistered, false otherwise.</returns>
        public static bool UnregisterWindowDefinition(FuWindowDefinition windowDefinition)
        {
            // Check if a window definition with the same ID already exists
            return UIWindowsDefinitions.Remove(windowDefinition.WindowName);
        }

        /// <summary>
        /// Unregisters a window definition.
        /// </summary>
        /// <param name="windowDefinitionName">The name of the window definition to be unregistered.</param>
        /// <returns>True if the window definition was successfully unregistered, false otherwise.</returns>
        public static bool UnregisterWindowDefinition(FuWindowName windowDefinitionName)
        {
            // Check if a window definition with the same ID already exists
            return UIWindowsDefinitions.Remove(windowDefinitionName);
        }

        /// <summary>
        /// Closes all UI windows.
        /// </summary>
        public static void CloseAllWindows()
        {
            CloseAllWindowsAsync(null);
        }

        /// <summary>
        /// Closes all UI windows and invokes a callback when every container has removed its window.
        /// </summary>
        /// <param name="callback">A callback to be invoked after all windows are closed.</param>
        public static void CloseAllWindowsAsync(Action callback)
        {
            ForceDrawAllWindows();

            List<FuWindow> windows = UIWindows.Values.ToList();
            if (windows.Count == 0)
            {
                callback?.Invoke();
                return;
            }

            foreach (FuWindow window in windows)
            {
                void onWindowClosed(FuWindow closedWindow)
                {
                    closedWindow.OnClosed -= onWindowClosed;
                    if (UIWindows.Count == 0)
                    {
                        callback?.Invoke();
                    }
                }

                window.OnClosed += onWindowClosed;
                window.Close();
            }
        }

        /// <summary>
        /// Creates a UI window.
        /// </summary>
        /// <param name="windowToGet">The window name to create.</param>
        /// <param name="autoAddToMainContainer">Add the window to the main container.</param>
        /// <returns>The created window, or null if creation failed.</returns>
        public static FuWindow CreateWindow(FuWindowName windowToGet, bool autoAddToMainContainer = true)
        {
            List<(FuWindowName, FuWindow)> windows = CreateWindows(
                new List<FuWindowName> { windowToGet },
                autoAddToMainContainer);

            if (windows.Count == 0)
            {
                return null;
            }

            return windows[0].Item2;
        }

        /// <summary>
        /// Creates a UI window and invokes the callback once the window is ready.
        /// </summary>
        /// <param name="windowToGet">The window name to create.</param>
        /// <param name="callback">A callback invoked with the created window, or null if creation failed.</param>
        /// <param name="autoAddToMainContainer">Add the window to the main container.</param>
        public static void CreateWindowAsync(FuWindowName windowToGet, Action<FuWindow> callback, bool autoAddToMainContainer = true)
        {
            CreateWindowsAsync(new List<FuWindowName> { windowToGet }, (windows) =>
            {
                if (windows.Count > 0 && windows[0].Item1.Equals(windowToGet))
                {
                    callback?.Invoke(windows[0].Item2);
                    return;
                }

                callback?.Invoke(null);
            }, autoAddToMainContainer);
        }

        /// <summary>
        /// Creates UI windows.
        /// </summary>
        /// <param name="windowsToGet">The window names to create.</param>
        /// <param name="autoAddToMainContainer">Add the windows to the main container.</param>
        /// <returns>The created windows.</returns>
        public static List<(FuWindowName, FuWindow)> CreateWindows(List<FuWindowName> windowsToGet, bool autoAddToMainContainer = true)
        {
            List<(FuWindowName, FuWindow)> windows = new List<(FuWindowName, FuWindow)>();

            foreach (FuWindowName windowName in windowsToGet)
            {
                if (!UIWindowsDefinitions.TryGetValue(windowName, out FuWindowDefinition windowDefinition))
                {
                    continue;
                }

                if (!windowDefinition.CreateUIWindow(out FuWindow window))
                {
                    continue;
                }

                if (autoAddToMainContainer)
                {
                    window.Size = new Vector2Int(
                        (int)(windowDefinition.Size.x * DefaultContainer.Context.Scale),
                        (int)(windowDefinition.Size.y * DefaultContainer.Context.Scale));

                    window.TryAddToContainer(DefaultContainer);
                }

                window.ForceDraw();
                windows.Add((windowDefinition.WindowName, window));
            }

            return windows;
        }

        /// <summary>
        /// Creates UI windows and invokes the callback once every requested window has either failed or is ready.
        /// </summary>
        /// <param name="windowsToGet">The window names to create.</param>
        /// <param name="callback">A callback invoked with the created windows.</param>
        /// <param name="autoAddToMainContainer">Add the windows to the main container.</param>
        public static void CreateWindowsAsync(List<FuWindowName> windowsToGet, Action<List<(FuWindowName, FuWindow)>> callback, bool autoAddToMainContainer = true)
        {
            List<(FuWindowName, FuWindow)> windows = new List<(FuWindowName, FuWindow)>();
            List<FuWindowDefinition> windowDefinitions = new List<FuWindowDefinition>();

            foreach (FuWindowName windowName in windowsToGet)
            {
                if (UIWindowsDefinitions.TryGetValue(windowName, out FuWindowDefinition windowDefinition))
                {
                    windowDefinitions.Add(windowDefinition);
                }
            }

            if (windowDefinitions.Count == 0)
            {
                callback?.Invoke(windows);
                return;
            }

            int pendingWindows = windowDefinitions.Count;

            void completeOne()
            {
                pendingWindows--;
                if (pendingWindows <= 0)
                {
                    callback?.Invoke(windows);
                }
            }

            foreach (FuWindowDefinition windowDefinition in windowDefinitions)
            {
                if (!windowDefinition.CreateUIWindow(out FuWindow window))
                {
                    completeOne();
                    continue;
                }

                if (!autoAddToMainContainer)
                {
                    window.ForceDraw();
                    windows.Add((windowDefinition.WindowName, window));
                    completeOne();
                    continue;
                }

                Action<FuWindow> onWindowInitialized = null;
                onWindowInitialized = (initializedWindow) =>
                {
                    initializedWindow.OnInitialized -= onWindowInitialized;
                    initializedWindow.ForceDraw();
                    windows.Add((windowDefinition.WindowName, initializedWindow));
                    completeOne();
                };

                window.OnInitialized += onWindowInitialized;
                window.Size = new Vector2Int(
                    (int)(windowDefinition.Size.x * DefaultContainer.Context.Scale),
                    (int)(windowDefinition.Size.y * DefaultContainer.Context.Scale));

                if (!window.TryAddToContainer(DefaultContainer))
                {
                    window.OnInitialized -= onWindowInitialized;
                    window.ForceDraw();
                    windows.Add((windowDefinition.WindowName, window));
                    completeOne();
                }
            }
        }

        /// <summary>
        /// Try to add UIWindow to the current displayed windows List
        /// must be called by UIWindow constructor.
        /// </summary>
        /// <param name="window">the window to add</param>
        /// <returns>true if success</returns>
        internal static bool TryAddUIWindow(FuWindow window)
        {
            // check whatever a window is already open with the same ID
            if (UIWindows.ContainsKey(window.ID))
            {
                window.TryRemoveFromContainer();
                return false;
            }

            // add the window to the list of current opened windows
            UIWindows.Add(window.ID, window);
            return true;
        }

        /// <summary>
        /// try remove UIWindow from current opened windows list
        /// </summary>
        /// <param name="window">window to remove from the list</param>
        /// <returns>true if success</returns>
        internal static bool TryRemoveUIWindow(FuWindow window)
        {
            return UIWindows.Remove(window.ID);
        }

        /// <summary>
        /// Whatever a FuWIndowName has at least an instance
        /// </summary>
        /// <param name="windowName">WindowName to check</param>
        /// <returns>True if instancied at least once</returns>
        public static bool IsWindowOpen(FuWindowName windowName)
        {
            foreach (var window in UIWindows)
            {
                if (window.Value.WindowName.Equals(windowName))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get a list of all instances of the given FuWIndowName
        /// </summary>
        /// <param name="windowName">WindowName to get</param>
        /// <returns>list of windows instances</returns>
        public static List<FuWindow> GetWindowInstances(FuWindowName windowName)
        {
            List<FuWindow> windows = new List<FuWindow>();
            foreach (var window in UIWindows)
            {
                if (window.Value.WindowName.Equals(windowName))
                {
                    windows.Add(window.Value);
                }
            }
            return windows;
        }

        /// <summary>
        /// Refresh UI render of all instances of the given FuWIndowName
        /// </summary>
        /// <param name="windowName">WindowName to refresh</param>
        public static void RefreshWindowsInstances(FuWindowName windowName)
        {
            foreach (var window in UIWindows)
            {
                if (window.Value.WindowName.Equals(windowName))
                {
                    window.Value.ForceDraw();
                }
            }
        }

        /// <summary>
        /// Get the number of instances for a given FuWIndowName
        /// </summary>
        /// <param name="windowName">WindowName to check</param>
        /// <returns>number of instancied windows</returns>
        public static int GetNbWindowInstances(FuWindowName windowName)
        {
            int nbWindows = 0;
            foreach (var window in UIWindows)
            {
                if (window.Value.WindowName.Equals(windowName))
                {
                    nbWindows++;
                }
            }
            return nbWindows;
        }

        /// <summary>
        /// Render each FuGui contexts
        /// </summary>
        public static void Render()
        {
            Fugui.HasHovered3DWindowThisFrame = false;

#if FU_EXTERNALIZATION
            SDL.SDL_GetGlobalMouseState(out int x, out int y);
            AbsoluteMonitorMousePosition = new Vector2Int(x, y);
#endif

            // clear context menu stack in case dev forgot to pop something OR exception raise between push and pop
            ClearContextMenuStack();
            // clean popup stack to prevent popup to stay on stack if they close unexpectedly
            CleanPopupStack();

            // render any other contexts BEFORE, because 3D containers need to handle input before default to handle HasHovered3DWindowThisFrame
            foreach (var context in Contexts)
            {
                if (context.Key != 0 && context.Value.Started)
                {
                    if (context.Value.PrepareRender())
                    {
                        HasRenderWindowThisFrame = false;

#if FU_EXTERNALIZATION
                        FuExternalWindowContainer externalWindowContainer = null;
                        if (context.Value is FuExternalContext externalContext)
                        {
                            // Update external window container
                            externalWindowContainer = ((FuExternalWindowContainer)externalContext.Window.Window.Container);
                            externalWindowContainer.Update();
                        }
#endif

                        context.Value.Render();
                        ExecuteAfterCurrentRenderContextCallbacks();
                        context.Value.EndRender();
                        if (_targetScale != -1f)
                        {
                            context.Value.SetScale(_targetScale, _targetFontScale);
                        }
                    }
                }
            }

            if (MainContainerEnabled && DefaultContext != null)
            {
                // no one has render for now
                HasRenderWindowThisFrame = false;
                // prepare a new frame for default render
                DefaultContext.PrepareRender();
                // execute before default renderer render actions
                if (DefaultContext.RenderPrepared)
                {
                    while (_beforeDefaultRenderStack.Count > 0)
                    {
                        _beforeDefaultRenderStack.Dequeue()?.Invoke();
                    }
                }

                // Render default context
                DefaultContext.Render();
                ExecuteAfterCurrentRenderContextCallbacks();
                // execute after default renderer render actions
                if (DefaultContext.RenderPrepared)
                {
                    while (_afterDefaultRenderStack.Count > 0)
                    {
                        _afterDefaultRenderStack.Dequeue()?.Invoke();
                    }
                }

                DefaultContext.EndRender();
            }
            else if (DefaultContext != null)
            {
                while (_beforeDefaultRenderStack.Count > 0)
                {
                    _beforeDefaultRenderStack.Dequeue();
                }

                while (_afterDefaultRenderStack.Count > 0)
                {
                    _afterDefaultRenderStack.Dequeue();
                }

                SetCurrentContext(DefaultContext);
            }

            if (_targetScale != -1f && DefaultContext != null)
            {
                DefaultContext.SetScale(_targetScale, _targetFontScale);
            }

            // prevent rescaling each frames
            _targetScale = -1f;
        }

        /// <summary>
        /// Externalize a Fugui window into a native external window.
        /// </summary>
        /// <param name="uiWindow">The Fugui window to externalize.</param>
        public static void ExternalizeWindow(FuWindow uiWindow)
        {
#if FU_EXTERNALIZATION
            if (uiWindow == null)
            {
                Debug.LogError("Cannot create an external window from a null Fugui window.");
                return;
            }

            if (!uiWindow.IsExternalizable)
            {
                Debug.LogError($"Cannot externalize window {uiWindow.ID} because it is not externalizable.");
                return;
            }

            if(!Settings.EnableExternalizations)
            {
                Debug.LogWarning("Externalizations are disabled in the settings.");
                return;
            }

            if (ExternalWindows.ContainsKey(uiWindow.ID))
            {
                Debug.LogWarning($"External window for {uiWindow.ID} already exists.");
                return;
            }

            // 1) Create the external Fugui context
            FuExternalContext context = new FuExternalContext(_contextID++, Settings.GlobalScale, Settings.FontGlobalScale, null, uiWindow);
            Contexts.Add(context.ID, context);

            // 2) Create the external container bound to this context
            var container = new FuExternalWindowContainer(uiWindow, context);
            container.SetContainerScaleConfig(GetDefaultContainerScaleConfig());

            // 3) Register and attach the window to this container
            ExternalWindows.Add(uiWindow.ID, container);
#else
            Debug.LogWarning("You are trying to externalize a window but externalizations are disabled in the settings.\n" +
                "Add FU_EXTERNALIZATION define to your build settings to enable externalizations.");
#endif
        }

        /// <summary>
        /// Runs the internalize window workflow.
        /// </summary>
        /// <param name="uiWindow">The ui Window value.</param>
        public static void InternalizeWindow(FuWindow uiWindow)
        {
#if FU_EXTERNALIZATION
            if (uiWindow == null)
            {
                Debug.LogError("Cannot internalize a null Fugui window.");
                return;
            }
            if (!ExternalWindows.ContainsKey(uiWindow.ID))
            {
                Debug.LogWarning($"No external window found for {uiWindow.ID}.");
                return;
            }
            // Close the external window container
            if(uiWindow.Container is FuExternalWindowContainer externalContainer)
            {
                FuExternalContext externalContext = (FuExternalContext)externalContainer.Context;
                Vector2Int windPose = externalContext.Window.Position;
                Vector2Int defContainerPos = DefaultContainer.Position;
                Vector2Int finalPos = windPose - defContainerPos;

                externalContext.Window.Close(() => {
                    // Re-add the window to the default container
                    uiWindow.TryAddToContainer(DefaultContainer);
                    //uiWindow.LocalPosition = finalPos;
                });
            }
#else
            Debug.LogWarning("You are trying to internalize a window but externalizations are disabled in the settings.\n" +
                "Add FU_EXTERNALIZATION define to your build settings to enable externalizations.");
#endif
        }

        /// <summary>
        /// Remove an externalized window by instance.
        /// </summary>
        internal static void RemoveExternalWindow(FuWindow uiWindow)
        {
            if (uiWindow == null) return;
            if (FuWindow.InputFocusedWindow == uiWindow)
            {
                FuWindow.InputFocusedWindow = null;
                FuWindow.NbInputFocusedWindow = 0;
            }
            DestroyContext(uiWindow.Container.Context.ID);
        }

#if !FUDEBUG

        /// <summary>
        /// Push a color style to ImGui color stack
        /// </summary>
        /// <param name="imCol">ImGui color to push</param>
        /// <param name="color">colot value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Push(ImGuiCol imCol, Vector4 color)
        {
            ImGuiNative.igPushStyleColor_Vec4(imCol, color);
            NbPushColor++;
        }

        /// <summary>
        /// Push a color style to ImGui color stack
        /// </summary>
        /// <param name="imCol">ImGui color to push</param>
        /// <param name="color">colot value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Push(FuColors imCol, Vector4 color)
        {
            if ((int)imCol >= (int)ImGuiCol.COUNT)
            {
                Debug.LogError("You are trying to push a color that is not in ImGuiCol enum, use ImGuiCol instead.");
                return;
            }

            ImGuiNative.igPushStyleColor_Vec4((ImGuiCol)imCol, color);
            NbPushColor++;
        }

        /// <summary>
        /// Push a style var to ImGui style var stack
        /// </summary>
        /// <param name="imVar">ImGUi style var to push</param>
        /// <param name="value">style var value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Push(ImGuiStyleVar imVar, Vector2 value)
        {
            ImGuiNative.igPushStyleVar_Vec2(imVar, value * CurrentContext.Scale);
            NbPushStyle++;
        }

        /// <summary>
        /// Push a style var to ImGui style var stack
        /// </summary>
        /// <param name="imVar">ImGUi style var to push</param>
        /// <param name="value">style var value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Push(ImGuiStyleVar imVar, float value)
        {
            ImGuiNative.igPushStyleVar_Float(imVar, value * CurrentContext.Scale);
            NbPushStyle++;
        }

        /// <summary>
        /// Pop some colors from ImGui color stack
        /// </summary>
        /// <param name="nb">quantity of color to pop</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PopColor(int nb = 1)
        {
            if (nb > NbPushColor)
            {
                nb = NbPushColor;
            }
            if (NbPushColor > 0)
            {
                ImGuiNative.igPopStyleColor(nb);
                NbPushColor -= nb;
            }
        }

        /// <summary>
        /// Pop some style var from ImGui style stack
        /// </summary>
        /// <param name="nb">quantity of style var to pop</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PopStyle(int nb = 1)
        {
            if (nb > NbPushStyle)
            {
                nb = NbPushStyle;
            }
            if (NbPushStyle > 0)
            {
                ImGuiNative.igPopStyleVar(nb);
                NbPushStyle -= nb;
            }
        }
#endif

        /// <summary>
        /// Push the current font
        /// </summary>
        /// <param name="size">size of the font</param>
        /// <param name="type">type of the font</param>
        public static void PushFont(int size, FontType type = FontType.Regular)
        {
            if (!CurrentContext.Fonts.ContainsKey(size))
            {
                Debug.LogError("you are trying to push font for " + size + "px but it does not exists.");
                size = Settings.FontConfig.DefaultSize;
            }
            switch (type)
            {
                default:
                case FontType.Regular:
                    ImGui.PushFont(CurrentContext.Fonts[size].Regular);
                    break;
                case FontType.Bold:
                    ImGui.PushFont(CurrentContext.Fonts[size].Bold);
                    break;
                case FontType.Italic:
                    ImGui.PushFont(CurrentContext.Fonts[size].Italic);
                    break;
            }
            NbPushFont++;
        }

        /// <summary>
        /// Push the current font type
        /// </summary>
        /// <param name="type">type of the font</param>
        public static void PushFont(FontType type)
        {
            PushFont(GetFontSize(), type);
        }

        /// <summary>
        /// Get the current font size
        /// </summary>
        /// <returns> size of the current font</returns>
        public static int GetFontSize()
        {
            float fontScale = CurrentContext != null ? CurrentContext.FontScale : 1f;
            if (fontScale <= 0f)
            {
                fontScale = 1f;
            }

            return Mathf.RoundToInt(ImGuiNative.igGetFontSize() / fontScale);
        }

        /// <summary>
        /// Pop the current font
        /// </summary>
        public static void PopFont()
        {
            if (NbPushFont > 0)
            {
                ImGui.PopFont();
                NbPushFont--;
            }
        }

        /// <summary>
        /// Pop the n current fonts
        /// </summary>
        /// <param name="nbPop">number of fonts to pop</param>
        public static void PopFont(int nbPop)
        {
            for (int i = 0; i < nbPop; i++)
            {
                PopFont();
            }
        }

        /// <summary>
        /// Push the defaut font to current
        /// </summary>
        public static void PushDefaultFont()
        {
            ImGui.PushFont(CurrentContext.DefaultFont.Regular);
            NbPushFont++;
        }

        /// <summary>
        /// Create a new Fugui context to render into unity
        /// </summary>
        /// <param name="camera">Camera that will render the context</param>
        /// <param name="onInitialize">invoked on context initialization</param>
        /// <returns>the context created</returns>
        public static unsafe FuUnityContext CreateUnityContext(Camera camera, float scale = 1f, float fontScale = 1f, Action onInitialize = null)
        {
            return CreateUnityContext(_contextID++, camera, scale, fontScale, onInitialize);
        }

        /// <summary>
        /// Create a new Fugui context to render into unity
        /// </summary>
        /// <param name="index">index of the context</param>
        /// <param name="camera">Camera that will render the context</param>
        /// <param name="onInitialize">invoked on context initialization</param>
        /// <returns>the context created</returns>
        private static FuUnityContext CreateUnityContext(int index, Camera camera, float scale = 1f, float fontScale = 1f, Action onInitialize = null)
        {
            if (Contexts.ContainsKey(index))
                return null;

            // create and add context
            FuUnityContext context = new FuUnityContext(index, scale, fontScale, onInitialize, camera);
            Contexts.Add(index, context);

            return context;
        }

        /// <summary>
        /// Create a new Fugui context to render into unity
        /// </summary>
        /// <param name="pixelRect"> Rect in pixel to render the context into, relative to the main container camera</param>
        /// <param name="scale"> initial scale of the context, keep 1f to use global scale from settings</param>
        /// <param name="fontScale"> initial font scale of the context, keep 1f to use global font scale from settings</param>
        /// <param name="onInitialize"> invoked on context initialization</param>
        /// <returns> the context created</returns>
        public static unsafe FuUnityContext CreateUnityContext(Rect pixelRect, float scale = 1f, float fontScale = 1f, Action onInitialize = null)
        {
            return CreateUnityContext(_contextID++, pixelRect, scale, fontScale, onInitialize);
        }

        /// <summary>
        /// Create a new Fugui context to render into unity
        /// </summary>
        /// <param name="index"> index of the context</param>
        /// <param name="pixelRect"> Rect in pixel to render the context into, relative to the main container camera</param>
        /// <param name="scale"> initial scale of the context, keep 1f to use global scale from settings</param>
        /// <param name="fontScale"> initial font scale of the context, keep 1f to use global font scale from settings</param>
        /// <param name="onInitialize"> invoked on context initialization</param>
        /// <returns> the context created</returns>
        private static FuUnityContext CreateUnityContext(int index, Rect pixelRect, float scale = 1f, float fontScale = 1f, Action onInitialize = null)
        {
            if (Contexts.ContainsKey(index))
                return null;
            // create and add context
            FuUnityContext context = new FuUnityContext(index, scale, fontScale, onInitialize, pixelRect);
            Contexts.Add(index, context);
            return context;
        }

        /// <summary>
        /// Destroy a fugui context by it's ID
        /// </summary>
        /// <param name="contextID">ID of the fugui context</param>
        public static void DestroyContext(int contextID)
        {
            if (ContextExists(contextID))
            {
                GetContext(contextID).Stop();
                if (!ToDeleteContexts.Contains(contextID))
                {
                    ToDeleteContexts.Enqueue(contextID);
                }
            }
        }

        /// <summary>
        /// Destroy a fugui context by it's context instance
        /// </summary>
        /// <param name="context">the fugui context to destroy</param>
        public static void DestroyContext(FuContext context)
        {
            if (context == null)
            {
                return;
            }

            DestroyContext(context.ID);
        }

        /// <summary>
        /// Get a fugui context by it's ID
        /// </summary>
        /// <param name="contextID">ID of the context to get</param>
        /// <returns>null if context's ID does not exists</returns>
        public static FuContext GetContext(int contextID)
        {
            if (Contexts.ContainsKey(contextID))
            {
                return Contexts[contextID];
            }
            return null;
        }

        /// <summary>
        /// Whatever a context exists
        /// </summary>
        /// <param name="contextID">ID of the context to check</param>
        /// <returns>true if exists</returns>
        public static bool ContextExists(int contextID)
        {
            return Contexts.ContainsKey(contextID);
        }

        /// <summary>
        /// set the current fugui context by ID
        /// </summary>
        /// <param name="contextID">ID of the fugui context</param>
        public static void SetCurrentContext(int contextID)
        {
            if (Contexts.ContainsKey(contextID))
            {
                SetCurrentContext(Contexts[contextID]);
            }
        }

        /// <summary>
        /// set the current fugui context
        /// </summary>
        /// <param name="context">instance of the fugui context</param>
        public static void SetCurrentContext(FuContext context)
        {
            if (context != null)
            {
                context.SetAsCurrent();
                CurrentContext = context;
            }
            else
            {
                CurrentContext = null;
                ImGui.SetCurrentContext(IntPtr.Zero);
            }
        }

        /// <summary>
        /// Get text size according to it's wrapping behaviour
        /// </summary>
        /// <param name="text">text to get size of</param>
        /// <param name="wrapping">however the text need to be wrapped</param>
        /// <returns>Size of the text</returns>
        public static Vector2 CalcTextSize(string text, FuTextWrapping wrapping)
        {
            return CalcTextSize(text, wrapping, Vector2.zero);
        }

        /// <summary>
        /// Get text size according to it's wrapping behaviour
        /// </summary>
        /// <param name="text">text to get size of</param>
        /// <param name="wrapping">however the text need to be wrapped</param>
        /// <param name="maxSize">maximum size (for clipping or wrapping). Keep Vector2.zero to use maximum available region</param>
        /// <returns>Size of the text</returns>
        public static Vector2 CalcTextSize(string text, FuTextWrapping wrapping, Vector2 maxSize)
        {
            if ((text.Length == 1 || Fugui.GetUntagedText(text).Length == 1) && Fugui.IsDuoToneChar(text[0]))
            {
                // get secondaty char
                char secondary = (char)(((ushort)text[0]) + 1);
                // get both char sized
                Vector2 primarySize = ImGui.CalcTextSize(text[0].ToString());
                Vector2 secondarySize = ImGui.CalcTextSize(secondary.ToString());
                // get full icon size
                return new Vector2(Mathf.Max(primarySize.x, secondarySize.x), Mathf.Max(primarySize.y, secondarySize.y));
            }

            Vector2 textSize;
            switch (wrapping)
            {
                default:
                case FuTextWrapping.None:
                    textSize = ImGui.CalcTextSize(text, true);
                    break;

                case FuTextWrapping.Clip:
                    textSize = ImGui.CalcTextSize(text, true);
                    textSize.x = Mathf.Min(textSize.x, maxSize.x == 0f ? ImGui.GetContentRegionAvail().x : maxSize.x);
                    break;

                case FuTextWrapping.Wrap:
                    textSize = ImGui.CalcTextSize(text, true, maxSize.x == -1f ? ImGui.GetContentRegionAvail().x : maxSize.x);
                    if (maxSize.y > 0f && textSize.y > maxSize.y)
                    {
                        textSize.y = maxSize.y;
                    }
                    break;
            }
            return textSize;
        }

        /// <summary>
        /// Check whatever a Char gyph is a Duotone Icon glyph
        /// </summary>
        /// <param name="character">char to check</param>
        /// <returns>true if it should be a duotone icon glyph char</returns>
        public static bool IsDuoToneChar(char character)
        {
            ushort charUS = (ushort)character;
            return charUS >= MIN_DUOTONE_GLYPH_RANGE && charUS <= MAX_DUOTONE_GLYPH_RANGE;
        }

        /// <summary>
        /// Render the secondary duotone glyph on top of a text
        /// </summary>
        /// <param name="text">text that will or have been draw outside</param>
        /// <param name="textPos">position of the text</param>
        /// <param name="drawList">drawList that draw the text</param>
        /// <param name="disabled">if true, render the secondary glyph in disabled color</param>
        public static void DrawDuotoneSecondaryGlyph(string text, Vector2 textPos, ImDrawListPtr drawList, bool disabled)
        {
            // look for duoTone icons within text
            for (int i = 0; i < text.Length; i++)
            {
                // this char is Duotone, let's render secondary Glyph
                if (IsDuoToneChar(text[i]))
                {
                    // get preText string
                    char[] preTextCharArray = new char[i];
                    for (int j = 0; j < i; j++)
                    {
                        preTextCharArray[j] = text[i];
                    }
                    string preText = new string(preTextCharArray);
                    // get pretext size
                    Vector2 size = CalcTextSize(preText, FuTextWrapping.None);
                    // place virtual cursor to right position
                    textPos.x += size.x;
                    uint secondaryColor = GetSecondaryDuotoneColor(disabled);

                    // render secondary glyph
                    drawList.AddText(textPos, secondaryColor, ((char)(((ushort)text[i]) + 1)).ToString());
                    break;
                }
            }
        }

        /// <summary>
        /// Return the current primary duotone glyph color
        /// </summary>
        /// <param name="disabled">if true, return the disabled duotone color</param>
        /// <returns>primary duotone color</returns>
        public static uint GetPrimaryDuotoneColor(bool disabled)
        {
            // Get current ImGui text color (from style)
            Vector4 currentTextColor = ImGui.GetStyle().Colors[(int)FuColors.Text];

            // Get FuGui theme reference text color
            Vector4 themeTextColor = Fugui.Themes.GetColor(FuColors.Text);

            // If ImGui style uses a custom color (i.e., it's different from the theme)
            if (!ColorsAreEqual(currentTextColor, themeTextColor))
            {
                return ImGui.GetColorU32(currentTextColor);
            }

            // Else return duotone from theme (disabled or not)
            var color = Fugui.Themes.GetColor(disabled ? FuColors.TextDisabled : FuColors.DuotonePrimaryColor);
            return ImGui.GetColorU32(color);
        }

        /// <summary>
        /// Return the current secondary duotone glyph color
        /// </summary>
        /// <param name="disabled">if true, return the disabled color</param>
        /// <returns>secondary duotone color</returns>
        public static uint GetSecondaryDuotoneColor(bool disabled)
        {
            // Get current ImGui text color (possibly overridden)
            Vector4 currentTextColor = ImGui.GetStyle().Colors[(int)FuColors.Text];

            // Get FuGui theme base text color
            Vector4 themeTextColor = Fugui.Themes.GetColor(FuColors.Text);

            // If user has pushed a custom text color
            if (!ColorsAreEqual(currentTextColor, themeTextColor))
            {
                // Extrapolate secondary duotone based on currentTextColor * 0.9
                Vector4 extrapolated = new Vector4(
                    currentTextColor.x * 0.9f,
                    currentTextColor.y * 0.9f,
                    currentTextColor.z * 0.9f,
                    currentTextColor.w
                );

                return ImGui.GetColorU32(extrapolated);
            }

            // Fallback to theme duotone or disabled color
            var color = Fugui.Themes.GetColor(disabled ? FuColors.TextDisabled : FuColors.DuotoneSecondaryColor);
            return ImGui.GetColorU32(color);
        }

        /// <summary>
        /// Check if two colors are equal within a tolerance
        /// </summary>
        /// <param name="a"> first color</param>
        /// <param name="b"> second color</param>
        /// <param name="tolerance"> tolerance for color comparison</param>
        /// <returns> true if colors are equal within the tolerance</returns>
        public static bool ColorsAreEqual(Vector4 a, Vector4 b, float tolerance = 0.001f)
        {
            return MathF.Abs(a.x - b.x) < tolerance &&
                   MathF.Abs(a.y - b.y) < tolerance &&
                   MathF.Abs(a.z - b.z) < tolerance &&
                   MathF.Abs(a.w - b.w) < tolerance;
        }

        /// <summary>
        /// Check if any window is currently being dragged
        /// </summary>
        /// <returns> true if any window is being dragged, false otherwise</returns>
        public static bool IsAnyWindowDragging()
        {
            foreach (var window in UIWindows)
            {
                if (window.Value.IsDragging)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check if any window is currently being resized.
        /// </summary>
        /// <returns> true if any window is being resized, false otherwise</returns>
        public static bool IsAnyWindowResizing()
        {
            foreach (var window in UIWindows)
            {
                if (window.Value.IsResizing)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Determines whether any window currently displays hover content.
        /// </summary>
        /// <returns>true if at least one window is displaying hover content; otherwise, false.</returns>
        public static bool IsAnyWindowHoverContent()
        {
            foreach (var window in UIWindows)
            {
                if (window.Value.IsHoveredContent)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check if any overlay of any window is currently being dragged
        /// </summary>
        /// <returns> true if any overlay is being dragged, false otherwise</returns>
        public static bool IsAnyOverlayDragging()
        {
            foreach (var window in UIWindows)
            {
                foreach (var overlay in window.Value.Overlays)
                {
                    if (overlay.Value.IsDraging)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Check if any payload is currently being dragged in any window or overlay
        /// </summary>
        /// <returns> true if any payload is being dragged, false otherwise</returns>
        public static bool IsDraggingAnyPayload()
        {
            return CurrentContext?._isDraggingPayload ?? false;
        }

        /// <summary>
        /// Check if any payload, window or overlay is currently being dragged
        /// </summary>
        /// <returns> true if any payload, window or overlay is being dragged, false otherwise</returns>
        public static bool IsDraggingAnything()
        {
            return IsDraggingAnyPayload() || IsAnyWindowDragging() || IsAnyOverlayDragging();
        }

        /// <summary>
        /// Read all bytes from a file, using UnityWebRequest on Android to support streaming assets, and File.ReadAllBytes on other platforms
        /// </summary>
        /// <param name="filePath"> path of the file to read</param>
        /// <returns> byte array of the file content, or null if an error occurs</returns>
        public static byte[] ReadAllBytes(string filePath)
        {
#if FUMOBILE
    using (var request = UnityEngine.Networking.UnityWebRequest.Get(filePath))
    {
        var operation = request.SendWebRequest();
        while (!operation.isDone)
        {
        }

        if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[FontLoader] Failed to load font from streaming assets: {filePath} - {request.error}");
            return null;
        }

        return request.downloadHandler.data;
    }
#else
            if (!File.Exists(filePath))
            {
                Debug.LogError($"[FontLoader] Font file not found: {filePath}");
                return null;
            }

            return File.ReadAllBytes(filePath);
#endif
        }

        /// <summary>
        /// Read all text from a file, using UnityWebRequest on Android to support streaming assets, and File.ReadAllText on other platforms
        /// </summary>
        /// <param name="filePath"> path of the file to read</param>
        /// <returns> string of the file content, or null if an error occurs</returns>
        public static string ReadAllText(string filePath)
        {
            string text = Encoding.UTF8.GetString(ReadAllBytes(filePath));
            return text;
        }

        /// <summary>
        /// Convert a string to UTF8 byte array and return the number of bytes written to the array
        /// </summary>
        /// <param name="s"> string to convert</param>
        /// <param name="utf8Bytes"> pointer to the byte array that will receive the UTF8 bytes</param>
        /// <param name="utf8ByteCount"> size of the byte array</param>
        /// <returns> number of bytes written to the array</returns>
        public unsafe static int GetUtf8(string s, byte* utf8Bytes, int utf8ByteCount)
        {
            fixed (char* utf16Ptr = s)
            {
                return Encoding.UTF8.GetBytes(utf16Ptr, s.Length, utf8Bytes, utf8ByteCount);
            }
        }

        /// <summary>
        /// Execute a callback after each window of default context has render
        /// </summary>
        /// <param name="callback"> callback to execute</param>
        public static void ExecuteAfterRenderWindows(Action callback)
        {
            if (callback != null)
            {
                _afterDefaultRenderStack.Enqueue(callback);
            }
        }

        /// <summary>
        /// Execute a callback after the currently rendered Fugui context has finished drawing its windows.
        /// </summary>
        /// <param name="callback">Callback to execute.</param>
        private static void ExecuteAfterCurrentRenderContext(Action callback)
        {
            if (callback != null)
            {
                _afterCurrentRenderContextStack.Enqueue(callback);
            }
        }

        /// <summary>
        /// Executes callbacks waiting for the end of the currently rendered Fugui context.
        /// </summary>
        private static void ExecuteAfterCurrentRenderContextCallbacks()
        {
            while (_afterCurrentRenderContextStack.Count > 0)
            {
                _afterCurrentRenderContextStack.Dequeue()?.Invoke();
            }
        }

        /// <summary>
        /// Execute a callback after each window of default context has render
        /// </summary>
        /// <param name="callback"></param>
        public static void ExecuteBeforeRenderWindows(Action callback)
        {
            if (callback != null)
            {
                _beforeDefaultRenderStack.Enqueue(callback);
            }
        }

        #region State
        /// <summary>
        /// Adds spaces before uppercase letters in the input string.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>The input string with spaces added before uppercase letters.</returns>
        static Dictionary<string, string> _niceStrings = new Dictionary<string, string>();
        #endregion

        #region Methods
        /// <summary>
        /// Returns the add spaces before uppercase result.
        /// </summary>
        /// <param name="input">The input value.</param>
        /// <returns>The result of the operation.</returns>
        public static string AddSpacesBeforeUppercase(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            if (!_niceStrings.TryGetValue(input, out string niceString))
            {
                // Use a regular expression to add spaces before uppercase letters, but ignore the first letter of the string and avoid adding a space if it is preceded by whitespace
                niceString = AddSpacesBeforeUppercaseDirect(input);
                _niceStrings.Add(input, niceString);
            }
            return niceString;
        }

        /// <summary>
        /// Adds spaces before uppercase letters in the input string. return the value directly without saving it
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>The input string with spaces added before uppercase letters.</returns>
        public static string AddSpacesBeforeUppercaseDirect(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            return Regex.Replace(input, @"(?<=[a-z])(?=[A-Z])", " ");
        }
        #endregion

        #region State
        private static Dictionary<string, string> _untagedStrings = new Dictionary<string, string>();
        #endregion

        #region Methods
        /// <summary>
        /// Get a text without tag "##xxxxxx"
        /// </summary>
        /// <param name="input">taged text</param>
        /// <returns>untaged text</returns>
        public static string GetUntagedText(string input)
        {
            if (!_untagedStrings.TryGetValue(input, out string untagedString))
            {
                int tagIndex = input.IndexOf("##", StringComparison.Ordinal);
                untagedString = tagIndex >= 0 ? input.Substring(0, tagIndex) : input;
                _untagedStrings.Add(input, untagedString);
            }
            return untagedString;
        }

        /// <summary>
        /// Align next element Horizontaly
        /// </summary>
        /// <param name="nextElementWidth">width of the next element</param>
        /// <param name="alignement">alignement type</param>
        public static void HorizontalAlignNextElement(float nextElementWidth, FuElementAlignement alignement)
        {
            switch (alignement)
            {
                case FuElementAlignement.Center:
                    float pad = ImGui.GetContentRegionAvail().x / 2f - nextElementWidth / 2f;
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + pad);
                    break;

                case FuElementAlignement.Right:
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().x - nextElementWidth);
                    break;
            }
        }

        /// <summary>
        /// Each open window will be draw next frame
        /// </summary>
        /// <param name="nbFrames">number of frames to force drawing</param>
        public static void ForceDrawAllWindows(int nbFrames = 1)
        {
            foreach (FuWindow window in UIWindows.Values)
            {
                window.ForceDraw(nbFrames);
            }
        }

        /// <summary>
        /// Check if input string contains only alphanumeric characters and spaces.
        /// Spaces are allowed only if they are followed by an alphanumeric character
        /// </summary>
        /// <param name="input">The input string</param>
        /// <returns>True if input contains only alphanumeric characters and spaces, false otherwise</returns>
        public static bool IsAlphaNumericWithSpaces(string input)
        {
            return Regex.IsMatch(input, @"^[a-zA-Z0-9]+(\s[a-zA-Z0-9]+)*$");
        }

        /// <summary>
        /// Replaces spaces followed by an alphanumeric character with the same alphanumeric character capitalized
        /// </summary>
        /// <param name="input">The input string</param>
        /// <returns>The modified string</returns>
        public static string RemoveSpaceAndCapitalize(string input)
        {
            return Regex.Replace(input, @"\s([a-zA-Z0-9])", x => x.Groups[1].Value.ToUpper());
        }
        #endregion

        #region State
        // Clipper

        private static Dictionary<int, ImGuiListClipperPtr> _clippers = new Dictionary<int, ImGuiListClipperPtr>();
        #endregion

        #region Methods
        //private static unsafe readonly ImGuiListClipper* _clipper = ImGuiNative.ImGuiListClipper_ImGuiListClipper();

        /// <summary>
        /// Beggin a list clipper. Use it to help drawing only visible items of a list (items need to have fixed height)
        /// </summary>
        /// <param name="count">number of items</param>
        /// <param name="itemHeight">height of an item</param>
        public static unsafe void ListClipperBegin(int count = -1, float itemHeight = -1f)
        {
            if (count <= 0)
                count = 1;
            if (itemHeight <= 0f)
                itemHeight = 1f;

            int ctxId = CurrentContext != null ? CurrentContext.ID : 0;
            if (!_clippers.ContainsKey(ctxId))
            {
                _clippers.Add(ctxId, new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper()));
            }

            var _clipper = _clippers[ctxId];
            ImGuiNative.ImGuiListClipper_Begin(_clipper.NativePtr, count, itemHeight);
        }

        /// <summary>
        /// End a list clipper
        /// </summary>
        public static unsafe void ListClipperEnd()
        {
            int ctxId = CurrentContext != null ? CurrentContext.ID : 0;
            if (!_clippers.ContainsKey(ctxId))
            {
                _clippers.Add(ctxId, new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper()));
            }

            var _clipper = _clippers[ctxId];
            ImGuiNative.ImGuiListClipper_End(_clipper.NativePtr);
        }

        /// <summary>
        /// do one step inside the list clipper. should be called like while(Step())
        /// </summary>
        /// <returns>true if step success</returns>
        public static unsafe bool ListClipperStep()
        {
            int ctxId = CurrentContext != null ? CurrentContext.ID : 0;
            if (!_clippers.ContainsKey(ctxId))
            {
                _clippers.Add(ctxId, new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper()));
            }

            var _clipper = _clippers[ctxId];
            return ImGuiNative.ImGuiListClipper_Step(_clipper.NativePtr) == 1;
        }

        /// <summary>
        /// Get the index of the first list item to draw
        /// </summary>
        /// <returns>index of the item</returns>
        public static unsafe int ListClipperDisplayStart()
        {
            int ctxId = CurrentContext != null ? CurrentContext.ID : 0;
            if (!_clippers.ContainsKey(ctxId))
            {
                _clippers.Add(ctxId, new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper()));
            }

            var _clipper = _clippers[ctxId];
            return _clipper.DisplayStart;
        }

        /// <summary>
        /// Get the index of the last list item to draw
        /// </summary>
        /// <returns>index of the item</returns>
        public static unsafe int ListClipperDisplayEnd()
        {
            int ctxId = CurrentContext != null ? CurrentContext.ID : 0;
            if (!_clippers.ContainsKey(ctxId))
            {
                _clippers.Add(ctxId, new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper()));
            }

            var _clipper = _clippers[ctxId];
            return _clipper.DisplayEnd;
        }

        /// <summary>
        /// Get Whatever fugui want to capture a user input at this frame
        /// </summary>
        /// <param name="onlyCurrentContext">Whatever you want to check only the current Fugui context</param>
        /// <returns>true if Fugui want to capture user inputs this frame</returns>
        public static bool GetWantCaptureInputs(bool onlyCurrentContext)
        {
            switch (onlyCurrentContext)
            {
                default:
                case true:
                    ImGuiIOPtr io = CurrentContext != null ? CurrentContext.IO : ImGui.GetIO();
                    return io.WantTextInput;

                case false:
                    bool wantCapture = false;
                    foreach (FuContext context in Contexts.Values)
                    {
                        if (!MainContainerEnabled && ReferenceEquals(context, DefaultContext))
                        {
                            continue;
                        }

                        wantCapture |= context.IO.WantTextInput;
                    }
                    return wantCapture;
            }
        }

        /// <summary>
        /// Check Whatever a Key is Down for some given FuWIndowNames.
        /// If WindowNames array is empty, Fugui will check for any windows of any containers
        /// </summary>
        /// <param name="key">Key to check down state</param>
        /// <param name="windowsNames">windows names to check key satet on (you can leave this empty, it will check on any windows of any containers)</param>
        /// <returns>true if the key is pressed into the given scope</returns>
        public static bool GetKeyDown(FuKeysCode key, params FuWindowName[] windowsNames)
        {
            bool isDown = false;
            if (windowsNames == null || windowsNames.Length == 0)
            {
                isDown |= MainContainerEnabled && DefaultContainer != null && DefaultContainer.Keyboard.GetKeyDown(key);
                if (!isDown)
                {
                    foreach (var threeDWindowContainer in _3DWindows.Values)
                    {
                        if (threeDWindowContainer.Keyboard.GetKeyDown(key))
                        {
                            isDown = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                foreach (FuWindowName windowName in windowsNames)
                {
                    foreach (var window in UIWindows)
                    {
                        if (window.Value == null || window.Value.Keyboard == null)
                        {
                            continue;
                        }
                        if (window.Value.WindowName.Equals(windowName))
                        {
                            if (window.Value.Keyboard.GetKeyDown(key))
                            {
                                isDown = true;
                                break;
                            }
                        }
                    }
                }
            }
            return isDown;
        }

        /// <summary>
        /// Check Whatever a Key is Pressed for some given FuWIndowNames.
        /// If WindowNames array is empty, Fugui will check for any windows of any containers
        /// </summary>
        /// <param name="key">Key to check down state</param>
        /// <param name="windowsNames">windows names to check key satet on (you can leave this empty, it will check on any windows of any containers)</param>
        /// <returns>true if the key is pressed into the given scope</returns>
        public static bool GetKeyPressed(FuKeysCode key, params FuWindowName[] windowsNames)
        {
            bool isPressed = false;
            if (windowsNames == null || windowsNames.Length == 0)
            {
                isPressed |= MainContainerEnabled && DefaultContainer != null && DefaultContainer.Keyboard.GetKeyPressed(key);
                if (!isPressed)
                {
                    foreach (var threeDWindowContainer in _3DWindows.Values)
                    {
                        if (threeDWindowContainer.Keyboard.GetKeyPressed(key))
                        {
                            isPressed = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                foreach (FuWindowName windowName in windowsNames)
                {
                    foreach (var window in UIWindows)
                    {
                        if (window.Value == null || window.Value.Keyboard == null)
                        {
                            continue;
                        }
                        if (window.Value.WindowName.Equals(windowName))
                        {
                            if (window.Value.Keyboard.GetKeyPressed(key))
                            {
                                isPressed = true;
                                break;
                            }
                        }
                    }
                }
            }
            return isPressed;
        }

        /// <summary>
        /// Check Whatever a Key is Up for some given FuWIndowNames.
        /// If WindowNames array is empty, Fugui will check for any windows of any containers
        /// </summary>
        /// <param name="key">Key to check down state</param>
        /// <param name="windowsNames">windows names to check key satet on (you can leave this empty, it will check on any windows of any containers)</param>
        /// <returns>true if the key is pressed into the given scope</returns>
        public static bool GetKeyUp(FuKeysCode key, params FuWindowName[] windowsNames)
        {
            bool isUp = false;
            if (windowsNames == null || windowsNames.Length == 0)
            {
                isUp |= MainContainerEnabled && DefaultContainer != null && DefaultContainer.Keyboard.GetKeyUp(key);
                if (!isUp)
                {
                    foreach (var threeDWindowContainer in _3DWindows.Values)
                    {
                        if (threeDWindowContainer.Keyboard.GetKeyUp(key))
                        {
                            isUp = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                foreach (FuWindowName windowName in windowsNames)
                {
                    foreach (var window in UIWindows)
                    {
                        if (window.Value == null || window.Value.Keyboard == null)
                        {
                            continue;
                        }
                        if (window.Value.WindowName.Equals(windowName))
                        {
                            if (window.Value.Keyboard.GetKeyUp(key))
                            {
                                isUp = true;
                                break;
                            }
                        }
                    }
                }
            }
            return isUp;
        }

        /// <summary>
        /// Get the current mouse state
        /// </summary>
        /// <returns> current mouse state</returns>
        public static FuMouseState GetCurrentMouse()
        {
            if (FuWindow.CurrentDrawingWindow != null)
            {
                return FuWindow.CurrentDrawingWindow.Mouse;
            }
            return DefaultContainer.Mouse;
        }

        /// <summary>
        /// Whatever a popup is open from a specific window
        /// </summary>
        /// <param name="window">window to check</param>
        /// <returns>true if a popup if open from this window</returns>
        public static bool WindowHasPopupOpen(FuWindow window)
        {
            return PopUpWindowsIDs.Any(popupWindowID => window.ID == popupWindowID);
        }

        /// <summary>
        /// Get whatever a world position (container-relative) is inside an open popup
        /// </summary>
        /// <param name="worldPosition">world position (container-relative) to check</param>
        /// <returns>true if the position is inside some currently open popup</returns>
        public static bool IsInsideAnyPopup(Vector2 worldPosition)
        {
            return PopUpRects.Any(popupRect => popupRect.Contains(worldPosition)) || isInsideNotifyPanel(worldPosition);
        }

        /// <summary>
        /// Whatever fugui is currently drawing inside a popup
        /// </summary>
        /// <returns>true if we are drawing on a popup</returns>
        public static bool IsDrawingInsidePopup()
        {
            return IsPopupDrawing.Any(isDrawing => isDrawing);
        }

        /// <summary>
        /// Whatever there is currently at least one popup open
        /// </summary>
        /// <returns>true if there is at least one popup open</returns>
        public static bool IsThereAnyOpenPopup()
        {
            return PopUpIDs.Count > 0;
        }

        /// <summary>
        /// Whatever the current drawing popup has focus
        /// </summary>
        /// <returns>true if the current drawing popup has focus</returns>
        public static bool IsDrawingPopupFocused()
        {
            for (int i = 0; i < IsPopupFocused.Count; i++)
            {
                if (IsPopupDrawing[i] && IsPopupFocused[i])
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Pushes a backdrop style used by DrawBackdrop overloads that do not receive explicit colors.
        /// </summary>
        /// <param name="color">Overlay tint drawn above the blurred content.</param>
        /// <param name="blurRadius">Blur radius in current Fugui UI pixels. Zero draws only the color.</param>
        public static void PushBackdrop(Color color, float blurRadius = 0f)
        {
            _backdropStack.Push(new FuBackdropStyle(color, blurRadius));
        }

        /// <summary>
        /// Pops the last pushed backdrop style.
        /// </summary>
        public static void PopBackdrop()
        {
            if (_backdropStack.Count > 0)
            {
                _backdropStack.Pop();
            }
        }

        /// <summary>
        /// Draws the currently pushed backdrop style in a screen-space rect.
        /// </summary>
        /// <param name="rect">Screen-space rect to cover.</param>
        /// <param name="rounding">Optional corner rounding.</param>
        /// <param name="flags">Rounded corner flags.</param>
        public static void DrawBackdrop(Rect rect, float rounding = 0f, ImDrawFlags flags = ImDrawFlags.RoundCornersAll)
        {
            FuBackdropStyle style = _backdropStack.Count > 0
                ? _backdropStack.Peek()
                : FuBackdropStyle.Default;
            DrawBackdrop(ImGui.GetWindowDrawList(), rect, style.Color, style.BlurRadius, rounding, flags);
        }

        /// <summary>
        /// Draws a backdrop in the current window draw list.
        /// </summary>
        /// <param name="rect">Screen-space rect to cover.</param>
        /// <param name="color">Overlay tint drawn above the blurred content.</param>
        /// <param name="blurRadius">Blur radius in current Fugui UI pixels. Zero draws only the color.</param>
        /// <param name="rounding">Optional corner rounding.</param>
        /// <param name="flags">Rounded corner flags.</param>
        public static void DrawBackdrop(Rect rect, Color color, float blurRadius = 0f, float rounding = 0f, ImDrawFlags flags = ImDrawFlags.RoundCornersAll)
        {
            DrawBackdrop(ImGui.GetWindowDrawList(), rect, color, blurRadius, rounding, flags);
        }

        /// <summary>
        /// Draws a backdrop in any ImGui draw list.
        /// </summary>
        /// <param name="drawList">Target draw list.</param>
        /// <param name="rect">Screen-space rect to cover.</param>
        /// <param name="color">Overlay tint drawn above the blurred content.</param>
        /// <param name="blurRadius">Blur radius in current Fugui UI pixels. Zero draws only the color.</param>
        /// <param name="rounding">Optional corner rounding.</param>
        /// <param name="flags">Rounded corner flags.</param>
        public static void DrawBackdrop(ImDrawListPtr drawList, Rect rect, Color color, float blurRadius = 0f, float rounding = 0f, ImDrawFlags flags = ImDrawFlags.RoundCornersAll)
        {
            Vector2 min = rect.position;
            Vector2 max = rect.position + rect.size;
            if (rect.width <= 0f || rect.height <= 0f || (color.a <= 0f && blurRadius <= 0f))
            {
                return;
            }

            uint colorU32 = ImGui.GetColorU32(color);
            float scaledRounding = Mathf.Max(0f, rounding);
            float scaledBlurRadius = Mathf.Max(0f, blurRadius);
#if !FU_BACKDROP_ENABLED
            scaledBlurRadius = 0f;
#endif

            if (scaledBlurRadius <= 0f)
            {
                if (color.a <= 0f)
                {
                    return;
                }

                drawList.AddRectFilled(min, max, colorU32, scaledRounding, flags);
                return;
            }

            Vector2 uv = new Vector2(scaledBlurRadius, 0f);
            Vector2 uvMax = new Vector2(scaledBlurRadius, 1f);
            if (scaledRounding > 0f)
            {
                drawList.AddImageRounded(BackdropTextureID, min, max, uv, uvMax, colorU32, scaledRounding, flags);
                return;
            }

            drawList.AddImage(BackdropTextureID, min, max, uv, uvMax, colorU32);
        }

        /// <summary>
        /// Draws a theme-backed backdrop in the current window draw list.
        /// </summary>
        /// <param name="rect">Screen-space rect to cover.</param>
        /// <param name="color">Theme color used as overlay tint.</param>
        /// <param name="alphaMult">Alpha multiplier applied to the theme color.</param>
        /// <param name="rounding">Optional corner rounding.</param>
        /// <param name="flags">Rounded corner flags.</param>
        public static void DrawThemeBackdrop(Rect rect, FuColors color, float alphaMult = 1f, float rounding = 0f, ImDrawFlags flags = ImDrawFlags.RoundCornersAll)
        {
            DrawThemeBackdrop(ImGui.GetWindowDrawList(), rect, color, alphaMult, rounding, flags);
        }

        /// <summary>
        /// Draws a theme-backed backdrop in any ImGui draw list.
        /// </summary>
        /// <param name="drawList">Target draw list.</param>
        /// <param name="rect">Screen-space rect to cover.</param>
        /// <param name="color">Theme color used as overlay tint.</param>
        /// <param name="alphaMult">Alpha multiplier applied to the theme color.</param>
        /// <param name="rounding">Optional corner rounding.</param>
        /// <param name="flags">Rounded corner flags.</param>
        public static void DrawThemeBackdrop(ImDrawListPtr drawList, Rect rect, FuColors color, float alphaMult = 1f, float rounding = 0f, ImDrawFlags flags = ImDrawFlags.RoundCornersAll)
        {
            Vector4 themeColor = Themes != null
                ? Themes.GetColor(color, alphaMult)
                : Vector4.zero;
            DrawBackdrop(drawList, rect, themeColor, GetThemeBackdropBlur(color), rounding, flags);
        }

        /// <summary>
        /// Draws a backdrop over the current ImGui window rectangle.
        /// </summary>
        /// <param name="rounding">Optional corner rounding. Negative values use the current Fugui window rounding.</param>
        public static void DrawCurrentWindowBackdrop(float rounding = -1f)
        {
            float resolvedRounding = rounding >= 0f ? rounding : (Themes != null ? Themes.WindowRounding : 0f);
            DrawBackdrop(new Rect(ImGui.GetWindowPos(), ImGui.GetWindowSize()), resolvedRounding);
        }

        /// <summary>
        /// Draws a theme-backed backdrop over the current ImGui window rectangle.
        /// </summary>
        /// <param name="color">Theme color used as overlay tint.</param>
        /// <param name="alphaMult">Alpha multiplier applied to the theme color.</param>
        /// <param name="rounding">Optional corner rounding. Negative values resolve from the theme color family.</param>
        /// <param name="flags">Rounded corner flags.</param>
        public static void DrawCurrentWindowThemeBackdrop(FuColors color, float alphaMult = 1f, float rounding = -1f, ImDrawFlags flags = ImDrawFlags.RoundCornersAll)
        {
            float resolvedRounding = rounding >= 0f ? rounding : GetThemeBackdropRounding(color);
            DrawThemeBackdrop(new Rect(ImGui.GetWindowPos(), ImGui.GetWindowSize()), color, alphaMult, resolvedRounding, flags);
        }

        /// <summary>
        /// Returns the PopupBg color to push before opening a popup that draws its own backdrop.
        /// </summary>
        internal static Vector4 GetPopupBackdropStyleColor(float alphaMult = 0.98f)
        {
            return Themes.GetColor(FuColors.PopupBg, ShouldUseThemeBackdrop(FuColors.PopupBg, alphaMult) ? 0f : alphaMult);
        }

        /// <summary>
        /// Draws the standard popup backdrop over the current ImGui popup window.
        /// </summary>
        internal static void DrawCurrentPopupThemeBackdrop(float alphaMult = 0.98f, float rounding = -1f, float borderSize = -1f)
        {
            if (!ShouldUseThemeBackdrop(FuColors.PopupBg, alphaMult))
            {
                return;
            }

            ImGuiStylePtr style = ImGui.GetStyle();
            float resolvedRounding = rounding >= 0f ? rounding : style.PopupRounding;
            float resolvedBorderSize = borderSize >= 0f ? borderSize : style.PopupBorderSize;
            Rect backdropRect = new Rect(ImGui.GetWindowPos(), ImGui.GetWindowSize());
            float inset = Mathf.Max(0f, resolvedBorderSize);
            if (inset > 0f)
            {
                Vector2 insetVector = new Vector2(inset, inset);
                backdropRect.position += insetVector;
                backdropRect.size -= insetVector * 2f;
            }

            DrawThemeBackdrop(backdropRect, FuColors.PopupBg, alphaMult, Mathf.Max(0f, resolvedRounding - inset));
        }

        /// <summary>
        /// Returns whether a theme color should use a backdrop draw command instead of a native opaque background.
        /// </summary>
        /// <param name="color">Theme color to inspect.</param>
        /// <param name="alphaMult">Alpha multiplier applied by the caller.</param>
        /// <returns>True when blur is compiled in and visible for this theme color.</returns>
        internal static bool ShouldUseThemeBackdrop(FuColors color, float alphaMult = 1f)
        {
            Vector4 themeColor = Themes != null
                ? Themes.GetColor(color, alphaMult)
                : Vector4.zero;
            return ShouldUseBackdrop(themeColor, GetThemeBackdropBlur(color));
        }

        /// <summary>
        /// Returns whether a resolved color and blur radius should use a backdrop draw command.
        /// </summary>
        internal static bool ShouldUseBackdrop(Vector4 color, float blurRadius)
        {
#if FU_BACKDROP_ENABLED
            return blurRadius > 0f && color.w < 0.999f;
#else
            return false;
#endif
        }

        /// <summary>
        /// Returns whether the texture id represents a Fugui backdrop command.
        /// </summary>
        /// <param name="textureId">ImGui texture id.</param>
        /// <returns>True when this command should be handled by the backdrop renderer.</returns>
        internal static bool IsBackdropTextureID(IntPtr textureId)
        {
#if FU_BACKDROP_ENABLED
            return textureId == BackdropTextureID;
#else
            return false;
#endif
        }

        /// <summary>
        /// Returns the theme blur radius for the supported backdrop color families.
        /// </summary>
        internal static float GetThemeBackdropBlur(FuColors color)
        {
            if (Themes == null)
            {
                return 0f;
            }

            switch (color)
            {
                case FuColors.WindowBg:
                    return Mathf.Max(0f, Themes.WindowBlur);
                case FuColors.ChildBg:
                    return Mathf.Max(0f, Themes.ChildBlur);
                case FuColors.PopupBg:
                    return Mathf.Max(0f, Themes.PopupBlur);
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Returns the theme rounding for the supported backdrop color families.
        /// </summary>
        private static float GetThemeBackdropRounding(FuColors color)
        {
            if (Themes == null)
            {
                return 0f;
            }

            switch (color)
            {
                case FuColors.WindowBg:
                    return Themes.WindowRounding;
                case FuColors.ChildBg:
                    return Themes.ChildRounding;
                case FuColors.PopupBg:
                    return Themes.PopupRounding;
                default:
                    return 0f;
            }
        }

        private struct FuBackdropStyle
        {
            public static readonly FuBackdropStyle Default = new FuBackdropStyle(new Color(0f, 0f, 0f, 0f), 0f);

            public Color Color;
            public float BlurRadius;

            public FuBackdropStyle(Color color, float blurRadius)
            {
                Color = color;
                BlurRadius = blurRadius;
            }
        }

        /// <summary>
        /// Must be placed just after an UI element so this one can be dragged
        /// </summary>
        /// <param name="payloadID">Unique ID of the payload for the drag drop operation (must be same as used in BeginDragDropTarget method)</param>
        /// <param name="dragDropFlags">lags for this drag drop operation (see ImGuiDragDropFlags on google)</param>
        /// <param name="onDraggingUICallback">Callback called each frame while a drag drop operation. Use it to draw the preview drag drop window UI)</param>
        /// <param name="payload">payload to set, will be passed to the target on Drop frame</param>
        public static void BeginDragDropSource(string payloadID, ImGuiDragDropFlags dragDropFlags, Action onDraggingUICallback, object payload)
        {
            CurrentContext.BeginDragDropSource(payloadID, dragDropFlags, onDraggingUICallback, payload);
        }

        /// <summary>
        /// Must be placed just after an UI element so this can be dropped
        /// </summary>
        /// <typeparam name="T">Type of the drag drop payload to get (must be same as set as 'payload' arg in BeginDragDropSource method)</typeparam>
        /// <param name="payloadID">Unique ID of the payload for the drag drop operation (must be same as used in BeginDragDropTarget method)</param>
        /// <param name="onDropCallback">Callback called whenever the user drop the dragging payload on this UI element</param>
        public static void BeginDragDropTarget<T>(string payloadID, Action<T> onDropCallback)
        {
            CurrentContext.BeginDragDropTarget<T>(payloadID, onDropCallback);
        }

        /// <summary>
        /// Cancel a drag drop operation related to the given payloadID
        /// </summary>
        /// <param name="payloadID">ID of the payload to cancel (keep null to cancel any current drag drop operation)</param>
        public static void CancelDragDrop(string payloadID = null)
        {
            CurrentContext.CancelDragDrop(payloadID);
        }

        /// <summary>
        /// Get the current drag drop payload (null if there is no drag drop operation for now)
        /// </summary>
        /// <typeparam name="T">Type of the current payload</typeparam>
        /// <returns>return the current drag drop payload if there is one</returns>
        public static T GetDragDropPayload<T>()
        {
            return CurrentContext.GetDragDropPayload<T>();
        }

        /// <summary>
        /// Whatever we are performing a drag drop operation right now with the given payloadID
        /// </summary>
        /// <param name="payloadID">ID of the payload (Drag Drop data ID) to check</param>
        /// <returns>true if user if performing a drag drop operation for the given payload ID</returns>
        public static bool IsDraggingPayload(string payloadID)
        {
            return CurrentContext.IsDraggingPayload(payloadID);
        }

        /// <summary>
        /// Set the scale of all context
        /// </summary>
        /// <param name="scale">global all context scale</param>
        /// <param name="fontScale">context all font scale (usualy same value as context scale)</param>
        public static void SetScale(float scale, float fontScale)
        {
            if (scale <= 0f)
            {
                Debug.LogError("Fugui global scale must be greater than 0");
                return;
            }
            if (scale > 5f)
            {
                Debug.LogError("Fugui global scale must be less than 10");
                return;
            }

            ExecuteInMainThread(() =>
            {
                _targetScale = scale;
                _targetFontScale = fontScale;
            });
        }
        #endregion
    }
}
