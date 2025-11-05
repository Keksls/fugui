// define it to debug whatever Color or Styles are pushed (avoid stack leak metrics)
// it's ressourcefull, si comment it when debug is done. Ensure it's commented before build.
//#define FUDEBUG 
using Fu.Framework;
using ImGuiNET;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Fu
{
    public static partial class Fugui
    {
        #region Variables
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
        public static FuMainWindowContainer MainContainer { get; internal set; }
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
        /// A boolean value indicating whether the render thread has started
        /// </summary>
        public static bool IsRendering { get; internal set; } = false;
        /// <summary>
        /// A boolean value indicating whether a window has been render this frame
        /// </summary>
        public static bool HasRenderWindowThisFrame { get; internal set; } = false;
        /// <summary>
        /// Whatever Fugui is allowed to set mouse cursor icon
        /// </summary>
        public static bool IsCursorLocked { get; internal set; } = false;
        /// <summary>
        /// The rendering state of Fugui, used to determine the current rendering phase
        /// </summary>
        public static FuguiRenderingState RenderingState = FuguiRenderingState.None;
        /// <summary>
        /// The type of render pipeline currently in use (Built-in, URP, HDRP)
        /// </summary>
        public static FuRenderPipelineType RenderPipelineType { get; private set; }
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
        /// Whatever cursors has just been unlocked
        /// </summary>
        internal static bool CursorsJustUnlocked = false;
        // The dictionary of external windows
        private static Dictionary<string, Fu3DWindowContainer> _3DWindows;
        // counter of Fugui Contexts
        private static int _contextID = 0;
        // queue of callback to execute BEFORE default render
        private static Queue<Action> _beforeDefaultRenderStack = new Queue<Action>();
        // queue of callback to execute AFTER default render
        private static Queue<Action> _afterDefaultRenderStack = new Queue<Action>();
        // stack of action we will want to execute into unity main thread
        private static Queue<Action> _executeInMainThreadActionsStack = new Queue<Action>();

        private static float _targetScale = -1f;
        private static float _targetFontScale = -1f;
        #endregion

        #region Constants
        private const ushort MIN_DUOTONE_GLYPH_RANGE = 60543;
        private const ushort MAX_DUOTONE_GLYPH_RANGE = 63743;
        #endregion

        #region Events
        /// <summary>
        /// Event invoken whenever an exception happend within the UI render loop
        /// </summary>
        public static event Action<Exception> OnUIException;
        /// <summary>
        /// Fire the UI exception event
        /// </summary>
        /// <param name="ex">exception of the event</param>
        internal static void Fire_OnUIException(Exception ex) => OnUIException?.Invoke(ex);
        /// <summary>
        /// Event invoken whenever a FuWindow is externalized from main container to external window container
        /// </summary>
        public static event Action<FuWindow> OnWindowExternalized;
        /// <summary>
        /// Fire the Event invoken whenever a FuWindow is externalized from main container to external window container
        /// </summary>
        /// <param name="window">FuWindow that has just been externalized</param>
        internal static void Fire_OnWindowExternalized(FuWindow window) => OnWindowExternalized?.Invoke(window);

        #region Global Windows Events
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
        #endregion

        static Fugui()
        {
            Initialize(null, null, null);
        }

        #region Workflow
        /// <summary>
        /// Initialize FuGui and create Main Container
        /// </summary>
        /// <param name="mainContainerUICamera">Camera that will display UI of main container</param>
        public static void Initialize(FuSettings settings, FuController controller, Camera mainContainerUICamera)
        {
            Settings = settings;
            Controller = controller;
            GetCurrentRenderPipeline();
            if (RenderPipelineType == FuRenderPipelineType.Unsupported)
            {
                Debug.LogWarning($"[Fugui] Fugui has detected an unsupported render pipeline ({RenderPipelineType}). Fugui is only supporting Universal Render Pipeline (URP) and High Definition Render Pipeline (HDRP). You can still use Fugui with Built-in or custom SRP, but some features may not work as expected.");
            }
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
            DefaultContext.PrepareRender();

            // need to be called into start, because it will use ImGui context and we need to wait to create it from UImGui Awake
            MainContainer = new FuMainWindowContainer(DefaultContext);

            // register Fugui Settings Window
            new FuWindowDefinition(FuSystemWindowsNames.FuguiSettings, DrawSettings, size: new Vector2Int(256, 256), flags: FuWindowFlags.AllowMultipleWindow);

            // initialize debug tool if debug is enabled
#if FUDEBUG
            initDebugTool();
#endif

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
            // Dispose Fugui Contexts
            var ids = Contexts.Keys.ToList();
            foreach (int contextID in ids)
            {
                DestroyContext(contextID);
            }
        }
        #endregion

        #region Windows Native PInvoke
        /// <summary>
        /// windows user32 method to get world mouse pos
        /// </summary>
        /// <param name="lpMousePosition"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out Vector2Int lpMousePosition);
        #endregion

        #region public Utils
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
        public static void Add3DWindow(FuWindow uiWindow, Vector3? position = null, Quaternion? rotation = null)
        {
            if (uiWindow == null)
            {
                Debug.Log("You are trying to create a 3D context to draw a null window.");
                return;
            }
            // Add the UIwindow to it's own 3DContainer
            _3DWindows.Add(uiWindow.ID, new Fu3DWindowContainer(uiWindow, position, rotation));
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
            _3DWindows[id].Close();
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
        /// Closes all UI windows asynchronously.
        /// </summary>
        /// <param name="callback">A callback to be invoked after all windows are closed.</param>
        public static void CloseAllWindowsAsync(Action callback)
        {
            ForceDrawAllWindows();
            // Get a list of all UI windows
            List<FuWindow> windows = UIWindows.Values.ToList();
            if (windows.Count == 0)
            {
                callback?.Invoke();
            }
            else
            {
                // Iterate over the windows
                foreach (FuWindow window in windows)
                {
                    // Close this window
                    window.Close(() =>
                    {
                        if (UIWindows.Count == 0)
                        {
                            callback?.Invoke();
                        }
                    });
                }
            }
        }

        /// <summary>
        /// Creates UI windows asynchronously.
        /// </summary>
        /// <param name="windowToGet">window names to be created.</param>
        /// <param name="callback">A callback to be invoked after the windows was created, passing the instance of the created windows (null if fail).</param>
        /// <param name="autoAddToMainContainer">Add the window to the Main Container</param>
        public static void CreateWindowAsync(FuWindowName windowToGet, Action<FuWindow> callback, bool autoAddToMainContainer = true)
        {
            CreateWindowsAsync(new List<FuWindowName>() { windowToGet }, (windows) =>
            {
                if (windows.Count > 0 && windows[0].Item1.Equals(windowToGet))
                {
                    callback?.Invoke(windows[0].Item2);
                }
                else
                {
                    callback?.Invoke(null);
                }
            }, autoAddToMainContainer);
        }

        /// <summary>
        /// Creates UI windows asynchronously.
        /// </summary>
        /// <param name="windowsToGet">A list of window names to be created.</param>
        /// <param name="callback">A callback to be invoked after all windows are created, passing a dictionary of the created windows.</param>
        /// <param name="autoAddToMainContainer">Add the window to the Main Container</param>
        public static void CreateWindowsAsync(List<FuWindowName> windowsToGet, Action<List<(FuWindowName, FuWindow)>> callback, bool autoAddToMainContainer = true)
        {
            // Initialize counters for the number of windows to add and the number of windows added
            int nbWIndowToAdd = 0;
            int nbWIndowAdded = 0;

            // Initialize a list of window definitions
            List<FuWindowDefinition> winDefs = new List<FuWindowDefinition>();
            // Iterate over the window names
            foreach (FuWindowName windowID in windowsToGet)
            {
                // Check if a window definition with the specified name exists
                if (UIWindowsDefinitions.ContainsKey(windowID))
                {
                    // Add the window definition to the list and increment the window to add counter
                    winDefs.Add(UIWindowsDefinitions[windowID]);
                    nbWIndowToAdd++;
                }
            }

            // Initialize a dictionary of UI windows
            List<(FuWindowName, FuWindow)> windows = new List<(FuWindowName, FuWindow)>();
            // Iterate over the window definitions
            foreach (FuWindowDefinition winDef in winDefs)
            {
                // Create an event handler for the OnReady event of the window
                Action<FuWindow> onWindowReady = null;
                onWindowReady = (window) =>
                {
                    // Add the window to the dictionary and increment the window added counter
                    windows.Add(new(winDef.WindowName, window));
                    nbWIndowAdded++;
                    // Force window to draw first frame
                    window.ForceDraw();
                    // Unsubscribe from the OnReady event
                    window.OnInitialized -= onWindowReady;
                    // Invoke the callback if all windows are added
                    if (nbWIndowAdded == nbWIndowToAdd)
                    {
                        callback?.Invoke(windows);
                    }
                };
                // create the UIWindow
                if (winDef.CreateUIWindow(out FuWindow win))
                {
                    if (autoAddToMainContainer)
                    {
                        // Subscribe to the OnReady event of the window
                        win.OnInitialized += onWindowReady;
                        win.Size = new Vector2Int((int)(winDef.Size.x * MainContainer.Context.Scale), (int)(winDef.Size.y * MainContainer.Context.Scale));
                        // add UIWindow to main container
                        win.TryAddToContainer(MainContainer);
                    }
                    else
                    {
                        onWindowReady?.Invoke(win);
                    }
                }
                else
                {
                    nbWIndowAdded++;
                    // Invoke the callback if all windows are added
                    if (nbWIndowAdded == nbWIndowToAdd)
                    {
                        callback(windows);
                    }
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
        #endregion

        #region Rendering
        /// <summary>
        /// Render each FuGui contexts
        /// </summary>
        public static void Render()
        {
            // clear context menu stack in case dev forgot to pop something OR exception raise between push and pop
            ClearContextMenuStack();
            // clean popup stack to prevent popup to stay on stack if they close unexpectedly
            CleanPopupStack();
            // no one has render for now
            HasRenderWindowThisFrame = false;

            // prepare a new frame for default render
            DefaultContext.PrepareRender();
            // execute after default renderer render actions
            if (DefaultContext.RenderPrepared)
            {
                while (_beforeDefaultRenderStack.Count > 0)
                {
                    _beforeDefaultRenderStack.Dequeue()?.Invoke();
                }
            }
            // Render default context
            DefaultContext.Render();
            // execute after default renderer render actions
            if (DefaultContext.RenderPrepared)
            {
                while (_afterDefaultRenderStack.Count > 0)
                {
                    _afterDefaultRenderStack.Dequeue()?.Invoke();
                }
            }
            if (_targetScale != -1f)
            {
                DefaultContext.SetScale(_targetScale, _targetFontScale);
            }

            // check if render graph is enabled
            bool isRenderGraphEnabled = !GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>().enableRenderCompatibilityMode;

            // render any other contexts
            foreach (var context in Contexts)
            {
                if (context.Key != 0 && context.Value.Started)
                {
                    if (context.Value.PrepareRender())
                    {
                        HasRenderWindowThisFrame = false;
                        context.Value.Render();
                        if (_targetScale != -1f)
                        {
                            context.Value.SetScale(_targetScale, _targetFontScale);
                        }
                    }
                }
            }

            // prevent rescaling each frames
            _targetScale = -1f;
        }

        /// <summary>
        /// Get the current render pipeline type
        /// </summary>
        private static void GetCurrentRenderPipeline()
        {
            try
            {
                // Try the most specific first, then fallbacks.
                RenderPipelineAsset asset =
                      GraphicsSettings.currentRenderPipeline
                   ?? QualitySettings.renderPipeline
                   ?? GraphicsSettings.defaultRenderPipeline;

                // If null, it's Built-in RP.
                if (asset == null)
                {
                    RenderPipelineType = FuRenderPipelineType.BuiltIn;
                    return;
                }
#if HAS_URP
                RenderPipelineType = FuRenderPipelineType.URP;
#elif HAS_HDRP
            RenderPipelineType = FuRenderPipelineType.HDRP;
#else
            RenderPipelineType = FuRenderPipelineType.CustomSRP;
#endif
            }
            catch (Exception ex)
            {
                RenderPipelineType = FuRenderPipelineType.Unknown;
                Debug.LogWarning("Fugui: Unable to determine current render pipeline. " + ex.Message);
            }
        }
        #endregion

        #region Styles and Colors
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
        #endregion

        #region Fonts
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
            return (int)(ImGuiNative.igGetFontSize() / Scale);
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
        #endregion

        #region Contexts
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
        private static unsafe FuUnityContext CreateUnityContext(int index, Camera camera, float scale = 1f, float fontScale = 1f, Action onInitialize = null)
        {
            if (Contexts.ContainsKey(index))
                return null;

            // create and add context
            FuUnityContext context = new FuUnityContext(index, scale, fontScale, onInitialize, camera);
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
                ToDeleteContexts.Enqueue(contextID);
            }
        }

        /// <summary>
        /// Destroy a fugui context by it's context instance
        /// </summary>
        /// <param name="context">the fugui context to destroy</param>
        public static void DestroyContext(FuContext context)
        {
            ToDeleteContexts.Enqueue(context.ID);
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
            }
            else
            {
                CurrentContext = null;
                ImGui.SetCurrentContext(IntPtr.Zero);
            }
        }
        #endregion

        #region Font Utils
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
        #endregion

        #region public Utils
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

        /// <summary>
        /// Adds spaces before uppercase letters in the input string.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>The input string with spaces added before uppercase letters.</returns>
        static Dictionary<string, string> _niceStrings = new Dictionary<string, string>();
        public static string AddSpacesBeforeUppercase(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            if (!_niceStrings.ContainsKey(input))
            {
                // Use a regular expression to add spaces before uppercase letters, but ignore the first letter of the string and avoid adding a space if it is preceded by whitespace
                _niceStrings.Add(input, AddSpacesBeforeUppercaseDirect(input));
            }
            return _niceStrings[input];
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

        private static Dictionary<string, string> _untagedStrings = new Dictionary<string, string>();
        /// <summary>
        /// Get a text without tag "##xxxxxx"
        /// </summary>
        /// <param name="input">taged text</param>
        /// <returns>untaged text</returns>
        public static string GetUntagedText(string input)
        {
            if (!_untagedStrings.ContainsKey(input))
            {
                _untagedStrings.Add(input, input.Split(new char[] { '#', '#' })[0]);
            }
            return _untagedStrings[input];
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

        // Clipper
        private static unsafe readonly ImGuiListClipper* _clipper = ImGuiNative.ImGuiListClipper_ImGuiListClipper();

        /// <summary>
        /// Beggin a list clipper. Use it to help drawing only visible items of a list (items need to have fixed height)
        /// </summary>
        /// <param name="count">number of items</param>
        /// <param name="itemHeight">height of an item</param>
        public static unsafe void ListClipperBegin(int count = -1, float itemHeight = -1f)
        {
            ImGuiNative.ImGuiListClipper_Begin(_clipper, count, itemHeight);
        }

        /// <summary>
        /// End a list clipper
        /// </summary>
        public static unsafe void ListClipperEnd()
        {
            ImGuiNative.ImGuiListClipper_End(_clipper);
        }

        /// <summary>
        /// do one step inside the list clipper. should be called like while(Step())
        /// </summary>
        /// <returns>true if step success</returns>
        public static unsafe bool ListClipperStep()
        {
            return ImGuiNative.ImGuiListClipper_Step(_clipper) == 1;
        }

        /// <summary>
        /// Get the index of the first list item to draw
        /// </summary>
        /// <returns>index of the item</returns>
        public static unsafe int ListClipperDisplayStart()
        {
            return _clipper->DisplayStart;
        }

        /// <summary>
        /// Get the index of the last list item to draw
        /// </summary>
        /// <returns>index of the item</returns>
        public static unsafe int ListClipperDisplayEnd()
        {
            return _clipper->DisplayEnd;
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
                isDown |= MainContainer.Keyboard.GetKeyDown(key);
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
                isPressed |= MainContainer.Keyboard.GetKeyPressed(key);
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
                isUp |= MainContainer.Keyboard.GetKeyUp(key);
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
        #endregion

        #region Popup Utils
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
            return PopUpRects.Any(popupRect => popupRect.Contains(worldPosition));
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
        #endregion

        #region Drag Drop
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
        #endregion

        #region Scaling
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