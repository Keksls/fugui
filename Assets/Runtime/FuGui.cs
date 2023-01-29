// define it to debug whatever Color or Styles are pushed (avoid stack leak metrics)
// it's ressourcefull, si comment it when debug is done. Ensure it's commented before build.
//#define IMDEBUG 
using Fu.Core;
using ImGuiNET;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;

namespace Fu.Framework
{
    public static partial class Fugui
    {
        public static FuContext CurrentContext { get; internal set; }
        public static Dictionary<int, FuContext> Contexts { get; internal set; } = new Dictionary<int, FuContext>();
        public static Queue<int> ToDeleteContexts { get; internal set; } = new Queue<int>();
        // The settings for the Fugui Manager
        public static FuSettings Settings { get; internal set; }
        // The public property for the current world mouse position
        public static Vector2Int WorldMousePosition { get; internal set; }
        // The current time
        public static float Time { get; internal set; }
        // The static main UI container
        public static FuMainWindowContainer MainContainer { get; internal set; }
        // Default Fugui Context
        public static FuUnityContext DefaultContext { get; internal set; }
        // The static dictionary of UI windows
        public static Dictionary<string, FuWindow> UIWindows { get; internal set; }
        // The static dictionary of UI window definitions
        public static Dictionary<FuWindowsNames, FuWindowDefinition> UIWindowsDefinitions { get; internal set; }
        // A boolean value indicating whether the render thread has started
        public static bool IsRendering { get; internal set; } = false;
        // The dictionary of external windows
        private static Dictionary<string, FuExternalWindowContainer> _externalWindows;
        // The dictionary of external windows
        private static Dictionary<string, Fu3DWindowContainer> _3DWindows;
        // The queue of windows to be externalized
        private static Queue<FuWindow> _windowsToExternalize;
        // A boolean value indicating whether a new window can be added
        private static bool _canAddWindow = false;
        // A boolean value indicating whether the render thread has started
        private static bool _renderThreadStarted = false;
        // FuGui Manager instance
        internal static FuController Manager;
        // counter of Fugui Contexts
        private static int _contextID = 0;

#if IMDEBUG
        public static int NbPushStyle = 0;
        public static int NbPushColor = 0;
        public static int NbPopStyle = 0;
        public static int NbPopColor = 0;
        public static Stack<pushStyleData> StylesStack;
        public static Stack<pushColorData> ColorStack;
#endif
        #region Events
        public static event Action<Exception> OnUIException;
        internal static void DoOnUIException(Exception ex) => OnUIException?.Invoke(ex);
        #endregion

        static Fugui()
        {
#if IMDEBUG
            NewFrame();
#endif
        }

        #region Workflow
        /// <summary>
        /// Initialize FuGui and create Main Container
        /// </summary>
        /// <param name="mainContainerUICamera">Camera that will display UI of main container</param>
        public static void Initialize(Camera mainContainerUICamera)
        {
            // instantiate UIWindows 
            UIWindows = new Dictionary<string, FuWindow>();
            UIWindowsDefinitions = new Dictionary<FuWindowsNames, FuWindowDefinition>();
            // init dic and queue
            _externalWindows = new Dictionary<string, FuExternalWindowContainer>();
            _3DWindows = new Dictionary<string, Fu3DWindowContainer>();
            _windowsToExternalize = new Queue<FuWindow>();
            // we can now add window
            _canAddWindow = true;
            // assume that render thread is not already started
            _renderThreadStarted = false;

            // create Default Fugui Context and initialize themeManager
            DefaultContext = CreateUnityContext(mainContainerUICamera, 1f, 1f, FuThemeManager.Initialize);
            DefaultContext.PrepareRender();

            // need to be called into start, because it will use ImGui context and we need to wait to create it from UImGui Awake
            MainContainer = new FuMainWindowContainer(DefaultContext);
        }

        /// <summary>
        /// Update Fugui Windows Data (Externalizations and add/remove)
        /// Need to be called into MainThread (Update / Late Update / Coroutine)
        /// </summary>
        public static void Update()
        {
            // set shared time
            Time = UnityEngine.Time.unscaledTime;
            // get absolute monitors cursor pos
            Vector2Int _worldMousePosition;
            if (GetCursorPos(out _worldMousePosition))
            {
                WorldMousePosition = _worldMousePosition;
            }

            if (_windowsToExternalize.Count > 0 && _canAddWindow)
            {
                // start openTK render loop thread if not already started
                if (!_renderThreadStarted)
                {
                    _renderThreadStarted = true;
                    Thread openTKRenderThread = new Thread(openTKRenderLoop);
                    openTKRenderThread.IsBackground = true;
                    openTKRenderThread.Start();
                }

                _canAddWindow = false;
                FuWindow imguiWindow = _windowsToExternalize.Dequeue();

                // create new window
                FuExternalWindowContainer window = new FuExternalWindowContainer(imguiWindow, Settings.ExternalWindowFlags);
                window.Closed += (sender, args) =>
                {
                    lock (_externalWindows)
                    {
                        _externalWindows.Remove(imguiWindow.ID);
                    }
                };
                window.OnInitialized += () =>
                {
                    _canAddWindow = true;
                };
                lock (_externalWindows)
                {
                    _externalWindows.Add(imguiWindow.ID, window);
                }
            }
        }

        /// <summary>
        /// Disposes the external windows and stops the render thread.
        /// </summary>
        public static void Dispose()
        {
            // Close all external windows
            foreach (FuExternalWindowContainer window in _externalWindows.Values)
            {
                window.Close();
            }
            // Clear the list of external windows
            _externalWindows.Clear();
            // Set the render thread flag to false
            _renderThreadStarted = false;
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
        /// Execute a callback after a quick async waiting routine
        /// </summary>
        /// <param name="callback">callback to execute</param>
        /// <param name="sleep">time to wait before execute</param>
        public static void ExecuteCallbackAfterAsyncSleep(Action callback, float sleep)
        {
            Manager.StartCoroutine(executeCallbackAfterAsyncSleep_Routine(callback, sleep));
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
        /// Adds an UI window to be externalized.
        /// </summary>
        /// <param name="uiWindow">The UI window to be externalized.</param>
        public static void AddExternalWindow(FuWindow uiWindow)
        {
            // Add the IMGUI window to the queue of windows to be externalized
            _windowsToExternalize.Enqueue(uiWindow);
        }

        /// <summary>
        /// Removes an external window with the specified ID.
        /// </summary>
        /// <param name="id">The ID of the external window to be removed.</param>
        internal static void RemoveExternalWindow(string id)
        {
            // Check if an external window with the specified ID exists
            if (!_externalWindows.ContainsKey(id))
            {
                return;
            }
            // Close the external window and remove it from the list
            _externalWindows[id].Close();
        }

        /// <summary>
        /// Adds an UI window to be externalized.
        /// </summary>
        /// <param name="uiWindow">The UI window to be externalized.</param>
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
        /// Removes an external window with the specified ID.
        /// </summary>
        /// <param name="id">The ID of the external window to be removed.</param>
        internal static void Remove3DWindow(string id)
        {
            // Check if an external window with the specified ID exists
            if (!_3DWindows.ContainsKey(id))
            {
                return;
            }
            // Close the external window and remove it from the list
            _3DWindows[id].Close();
        }

        /// <summary>
        /// Registers a window definition with the specified ID.
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
        /// Closes all UI windows asynchronously.
        /// </summary>
        /// <param name="callback">A callback to be invoked after all windows are closed.</param>
        public static void CloseAllWindowsAsync(Action callback)
        {
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
        public static void CreateWindowAsync(FuWindowsNames windowToGet, Action<FuWindow> callback, bool autoAddToMainContainer = true)
        {
            CreateWindowsAsync(new List<FuWindowsNames>() { windowToGet }, (windows) =>
            {
                if (windows.ContainsKey(windowToGet))
                {
                    callback?.Invoke(windows[windowToGet]);
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
        public static void CreateWindowsAsync(List<FuWindowsNames> windowsToGet, Action<Dictionary<FuWindowsNames, FuWindow>> callback, bool autoAddToMainContainer = true)
        {
            // Initialize counters for the number of windows to add and the number of windows added
            int nbWIndowToAdd = 0;
            int nbWIndowAdded = 0;

            // Initialize a list of window definitions
            List<FuWindowDefinition> winDefs = new List<FuWindowDefinition>();
            // Iterate over the window names
            foreach (FuWindowsNames windowID in windowsToGet)
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
            Dictionary<FuWindowsNames, FuWindow> windows = new Dictionary<FuWindowsNames, FuWindow>();
            // Iterate over the window definitions
            foreach (FuWindowDefinition winDef in winDefs)
            {
                // Create an event handler for the OnReady event of the window
                Action<FuWindow> onWindowReady = null;
                onWindowReady = (window) =>
                {
                    // Add the window to the dictionary and increment the window added counter
                    windows.Add(winDef.WindowName, window);
                    nbWIndowAdded++;
                    // Invoke the callback if all windows are added
                    if (nbWIndowAdded == nbWIndowToAdd)
                    {
                        callback(windows);
                    }
                    // Unsubscribe from the OnReady event
                    window.OnReady -= onWindowReady;
                };
                // create the UIWindow
                if (winDef.CreateUIWindow(out FuWindow win))
                {
                    if (autoAddToMainContainer)
                    {
                        // Subscribe to the OnReady event of the window
                        win.OnReady += onWindowReady;
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
        #endregion

        #region Render Thread
        /// <summary>
        /// Render each FuGui contexts
        /// </summary>
        public static void Render()
        {
            // Render default context
            DefaultContext.Render();
            DefaultContext.EndRender();
            // render any other contexts
            foreach (var context in Contexts)
            {
                if (context.Key != 0 && context.Value.Started)
                {
                    if (context.Value.PrepareRender())
                    {
                        context.Value.Render();
                        context.Value.EndRender();
                    }
                }
            }
            // prepare a new frame after all render, so we can use ImGui methods outside FuguiContext.OnLayout events
            DefaultContext.PrepareRender();
        }

        /// <summary>
        /// Thread that handle External OpenTK container graphics contexts render
        /// </summary>
        private static void openTKRenderLoop()
        {
            // Loop while the render thread is started
            while (_renderThreadStarted)
            {
                lock (_externalWindows)
                {
                    // Iterate through all external windows
                    foreach (FuExternalWindowContainer window in _externalWindows.Values)
                    {
                        try
                        {
                            // Try to create the context for the window
                            window.TryCreateContext();

                            // Render the GL for the window
                            window.GLRender();

                            // Try to destroy the context for the window
                            window.TryDestroyContext();
                        }
                        catch (Exception ex)
                        {
                            // Log any errors that occur
                            Debug.LogError(ex);
                        }
                    }
                }
                // Sleep for the specified number of ticks
                Thread.Sleep(new TimeSpan(Settings.ExternalManipulatingTicks));
            }
        }
        #endregion

        #region Styles and Colors
#if IMDEBUG
        public static void NewFrame()
        {
            NbPushStyle = 0;
            NbPushColor = 0;
            NbPopStyle = 0;
            NbPopColor = 0;
            StylesStack = new Stack<pushStyleData>();
            ColorStack = new Stack<pushColorData>();
        }

        public static void Push(ImGuiCol imCol, Vector4 color)
        {
            ImGui.PushStyleColor(imCol, color);
            ColorStack.Push(new pushColorData()
            {
                color = imCol,
                stackTrace = Environment.StackTrace
            });
            NbPushColor++;
        }
        public static void Push(ImGuiStyleVar imVar, Vector2 value)
        {
            ImGui.PushStyleVar(imVar, value);
            StylesStack.Push(new pushStyleData()
            {
                style = imVar,
                stackTrace = Environment.StackTrace
            });
            NbPushStyle++;
        }
        public static void Push(ImGuiStyleVar imVar, float value)
        {
            ImGui.PushStyleVar(imVar, value);
            StylesStack.Push(new pushStyleData()
            {
                style = imVar,
                stackTrace = Environment.StackTrace
            });
            NbPushStyle++;
        }
        public static void PopColor(int nb = 1)
        {
            for (int i = 0; i < nb; i++)
            {
                if (ColorStack.Count > 0)
                {
                    ImGui.PopStyleColor();
                    NbPopColor++;
                    ColorStack.Pop();
                }
            }
        }
        public static void PopStyle(int nb = 1)
        {
            for (int i = 0; i < nb; i++)
            {
                if (StylesStack.Count > 0)
                {
                    ImGui.PopStyleVar();
                    NbPopStyle++;
                    StylesStack.Pop();
                }
            }
        }
#else
        public static void Push(ImGuiCol imCol, Vector4 color)
        {
            ImGui.PushStyleColor(imCol, color);
        }
        public static void Push(ImGuiStyleVar imVar, Vector2 value)
        {
            ImGui.PushStyleVar(imVar, value);
        }
        public static void Push(ImGuiStyleVar imVar, float value)
        {
            ImGui.PushStyleVar(imVar, value);
        }
        public static void PopColor(int nb = 1)
        {
            ImGui.PopStyleColor(nb);
        }
        public static void PopStyle(int nb = 1)
        {
            ImGui.PopStyleVar(nb);
        }
#endif
        #endregion

        #region Fonts
        public static void PushFont(int size, FontType type)
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
            }
        }

        public static void PushFont(FontType type)
        {
            PushFont(CurrentContext.DefaultFont.Size, type);
        }

        public static void PopFont()
        {
            ImGui.PopFont();
        }

        public static void PopFont(int nbPop)
        {
            for (int i = 0; i < nbPop; i++)
            {
                ImGui.PopFont();
            }
        }

        public static void PushDefaultFont()
        {
            ImGui.PushFont(CurrentContext.DefaultFont.Regular);
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
            FuUnityContext context = new FuUnityContext(index, scale, fontScale, onInitialize, camera, Manager.Render);
            Contexts.Add(index, context);

            return context;
        }

        /// <summary>
        /// Create a new Fugui context to render outside of unity
        /// </summary>
        /// <param name="onInitialize">invoked on context initialization</param>
        /// <returns>the context created</returns>
        public static unsafe FuExternalContext CreateExternalContext(float scale = 1f, float fontScale = 1f, Action onInitialize = null)
        {
            return CreateExternalContext(_contextID++, scale, fontScale, onInitialize);
        }

        /// <summary>
        /// Create a new Fugui context to render outside of unity
        /// </summary>
        /// <param name="index">index of the context</param>
        /// <param name="onInitialize">invoked on context initialization</param>
        /// <returns>the context created</returns>
        private static unsafe FuExternalContext CreateExternalContext(int index, float scale = 1f, float fontScale = 1f, Action onInitialize = null)
        {
            if (Contexts.ContainsKey(index))
                return null;

            // create and add context
            FuExternalContext context = new FuExternalContext(index, scale, fontScale, onInitialize);
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

#if !UIMGUI_REMOVE_IMPLOT
                ImPlotNET.ImPlot.SetImGuiContext(IntPtr.Zero);
#endif
#if !UIMGUI_REMOVE_IMGUIZMO
                ImGuizmoNET.ImGuizmo.SetImGuiContext(IntPtr.Zero);
#endif
#if !UIMGUI_REMOVE_IMNODES
                imnodesNET.imnodes.SetImGuiContext(IntPtr.Zero);
#endif
            }
        }
        #endregion

        #region public Utils
        /// <summary>
        /// Adds spaces before uppercase letters in the input string.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>The input string with spaces added before uppercase letters.</returns>
        public static string AddSpacesBeforeUppercase(string input)
        {
            // Use a regular expression to add spaces before uppercase letters, but ignore the first letter of the string and avoid adding a space if it is preceded by whitespace
            return Regex.Replace(input, "(?<!^)(?<!\\s)([A-Z])", " $1");
        }

        /// <summary>
        /// Get a text without tag "##xxxxxx"
        /// </summary>
        /// <param name="input">taged text</param>
        /// <returns>untaged text</returns>
        public static string GetUntagedText(string input)
        {
            int index = input.IndexOf("##");
            if (index != -1)
            {
                return input.Substring(0, index);
            }
            return input;
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
        public static void ForceDrawAllWindows()
        {
            foreach (FuWindow window in UIWindows.Values)
            {
                window.ForceDraw();
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
    }

    public enum FontType
    {
        Regular,
        Bold
    }
}