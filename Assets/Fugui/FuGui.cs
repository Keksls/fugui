// define it to debug whatever Color or Styles are pushed (avoid stack leak metrics)
// it's ressourcefull, si comment it when debug is done. Ensure it's commented before build.
//#define IMDEBUG 
using Fugui.Core;
using Fugui.Core.DearImGui;
using ImGuiNET;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

namespace Fugui.Framework
{
    public static class FuGui
    {
        public static FuguiContext CurrentContext { get; internal set; }
        public static Dictionary<int, FuguiContext> Contexts { get; internal set; } = new Dictionary<int, FuguiContext>();
        public static Queue<int> ToDeleteContexts { get; internal set; } = new Queue<int>();
        // The settings for the Fugui Manager
        public static FuguiSettings Settings { get; internal set; }
        // The public property for the current world mouse position
        public static Vector2Int WorldMousePosition { get; internal set; }
        // The current time
        public static float Time { get; internal set; }
        // The static main UI container
        public static MainUIContainer MainContainer { get; internal set; }
        // Def staticault Fugui Context
        public static FuguiContext DefaultContext { get; internal set; }
        // The static dictionary of UI windows
        public static Dictionary<string, UIWindow> UIWindows { get; internal set; }
        // The static dictionary of UI window definitions
        public static Dictionary<UIWindowName, UIWindowDefinition> UIWindowsDefinitions { get; internal set; }
        // A boolean value indicating whether the render thread has started
        public static bool IsRendering { get; internal set; } = false;
        // The dictionary of external windows
        private static Dictionary<string, ExternalWindowContainer> _externalWindows;
        // The queue of windows to be externalized
        private static Queue<UIWindow> _windowsToExternalize;
        // A boolean value indicating whether a new window can be added
        private static bool _canAddWindow = false;
        // A boolean value indicating whether the render thread has started
        private static bool _renderThreadStarted = false;
        // FuGui Manager instance
        internal static FuguiManager Manager;
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

        static FuGui()
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
            UIWindows = new Dictionary<string, UIWindow>();
            UIWindowsDefinitions = new Dictionary<UIWindowName, UIWindowDefinition>();
            // init dic and queue
            _externalWindows = new Dictionary<string, ExternalWindowContainer>();
            _windowsToExternalize = new Queue<UIWindow>();
            // we can now add window
            _canAddWindow = true;
            // assume that render thread is not already started
            _renderThreadStarted = false;

            // create Default Fugui Context
            DefaultContext = CreateContext(mainContainerUICamera);
            DefaultContext.PrepareRender();

            // need to be called into start, because it will use ImGui context and we need to wait to create it from UImGui Awake
            MainContainer = new MainUIContainer(DefaultContext);
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
                UIWindow imguiWindow = _windowsToExternalize.Dequeue();

                // create new window
                ExternalWindowContainer window = new ExternalWindowContainer(imguiWindow, Settings.ExternalShowTitleBar);
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
            foreach (ExternalWindowContainer window in _externalWindows.Values)
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
        public static void AddExternalWindow(UIWindow uiWindow)
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
        /// Registers a window definition with the specified ID.
        /// </summary>
        /// <param name="windowDefinition">The window definition to be registered.</param>
        /// <returns>True if the window definition was successfully registered, false otherwise.</returns>
        public static bool RegisterWindowDefinition(UIWindowDefinition windowDefinition)
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
            List<UIWindow> windows = UIWindows.Values.ToList();
            if (windows.Count == 0)
            {
                callback?.Invoke();
            }
            else
            {
                // Iterate over the windows
                foreach (UIWindow window in windows)
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
        public static void CreateWindowAsync(UIWindowName windowToGet, Action<UIWindow> callback)
        {
            CreateWindowsAsync(new List<UIWindowName>() { windowToGet }, (windows) =>
            {
                if(windows.ContainsKey(windowToGet))
                {
                    callback?.Invoke(windows[windowToGet]);
                }
                else
                {
                    callback?.Invoke(null);
                }
            });
        }

        /// <summary>
        /// Creates UI windows asynchronously.
        /// </summary>
        /// <param name="windowsToGet">A list of window names to be created.</param>
        /// <param name="callback">A callback to be invoked after all windows are created, passing a dictionary of the created windows.</param>
        public static void CreateWindowsAsync(List<UIWindowName> windowsToGet, Action<Dictionary<UIWindowName, UIWindow>> callback)
        {
            // Initialize counters for the number of windows to add and the number of windows added
            int nbWIndowToAdd = 0;
            int nbWIndowAdded = 0;

            // Initialize a list of window definitions
            List<UIWindowDefinition> winDefs = new List<UIWindowDefinition>();
            // Iterate over the window names
            foreach (UIWindowName windowName in windowsToGet)
            {
                // Check if a window definition with the specified name exists
                if (UIWindowsDefinitions.ContainsKey(windowName))
                {
                    // Add the window definition to the list and increment the window to add counter
                    winDefs.Add(UIWindowsDefinitions[windowName]);
                    nbWIndowToAdd++;
                }
            }

            // Initialize a dictionary of UI windows
            Dictionary<UIWindowName, UIWindow> windows = new Dictionary<UIWindowName, UIWindow>();
            // Iterate over the window definitions
            foreach (UIWindowDefinition winDef in winDefs)
            {
                // Create an event handler for the OnReady event of the window
                Action<UIWindow> onWindowReady = null;
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
                UIWindow win = winDef.CreateUIWindow();
                // Subscribe to the OnReady event of the window
                win.OnReady += onWindowReady;
                // add UIWindow to main container
                win.TryAddToContainer(MainContainer);
            }
        }

        /// <summary>
        /// Try to add UIWindow to the current displayed windows List
        /// must be called by UIWindow constructor.
        /// </summary>
        /// <param name="window">the window to add</param>
        /// <returns>true if success</returns>
        internal static bool TryAddUIWindow(UIWindow window)
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
        internal static bool TryRemoveUIWindow(UIWindow window)
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
                    context.Value.PrepareRender();
                    context.Value.Render();
                    context.Value.EndRender();
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
                    foreach (ExternalWindowContainer window in _externalWindows.Values)
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
                return;
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
        public static unsafe FuguiContext CreateContext(Camera camera)
        {
            return CreateContext(_contextID++, camera);
        }

        private static unsafe FuguiContext CreateContext(int index, Camera camera)
        {
            if (Contexts.ContainsKey(index))
                return null;

            // create and add context
            FuguiContext context = new FuguiContext(index, camera, FuGui.Manager.Render);
            Contexts.Add(index, context);

            return context;
        }

        public static void DestroyContext(int contextID)
        {
            if (ContextExists(contextID))
            {
                GetContext(contextID).Stop();
                ToDeleteContexts.Enqueue(contextID);
            }
        }

        public static void DestroyContext(FuguiContext context)
        {
            ToDeleteContexts.Enqueue(context.ID);
        }

        public static FuguiContext GetContext(int contextID)
        {
            if (Contexts.ContainsKey(contextID))
            {
                return Contexts[contextID];
            }
            return null;
        }

        public static bool ContextExists(int contextID)
        {
            return Contexts.ContainsKey(contextID);
        }

        public static void SetCurrentContext(int contextID)
        {
            if (Contexts.ContainsKey(contextID))
            {
                SetCurrentContext(Contexts[contextID]);
            }
        }

        public static void SetCurrentContext(FuguiContext context)
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
        public static bool GetWantCaptureInputs(bool excludeDesktop = false)
        {
            foreach (var pair in Contexts)
            {
                if (!excludeDesktop || pair.Key > 0)
                {
                    if (pair.Value.IO.WantCaptureMouse || pair.Value.IO.WantCaptureKeyboard || pair.Value.IO.WantTextInput)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool IsMouseHoverUI(bool excludeDesktop = false)
        {
            foreach (var pair in Contexts)
            {
                if (!excludeDesktop || pair.Key > 0)
                {
                    if (pair.Value.IO.WantCaptureMouse)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool GetWantCaptureInputsDesktop()
        {
            ImGuiIOPtr io = GetContext(0).IO;
            return io.WantCaptureMouse || io.WantCaptureKeyboard || io.WantTextInput;
        }

        public static bool GetWantCaptureKeyboard()
        {
            foreach (var pair in Contexts)
            {
                if (pair.Value.IO.WantCaptureKeyboard || pair.Value.IO.WantTextInput)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool GetWantCaptureMouse(bool excludeDesktop = false)
        {
            foreach (var pair in Contexts)
            {
                if (!excludeDesktop || pair.Key > 0)
                {
                    if (pair.Value.IO.WantCaptureMouse)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool IsMouseHoverCurrentWindow()
        {
            Vector2 pos = ImGui.GetWindowPos();
            Vector2 size = ImGui.GetWindowSize();
            Vector2 mousePos = ImGui.GetMousePos();
            //mousePos.y = Screen.height - mousePos.y;
            return mousePos.x >= pos.x && mousePos.x <= (pos + size).x && mousePos.y >= pos.y && mousePos.y <= (pos + size).y;
        }

        public static bool IsMouseHoverWindow(Vector2 pos, Vector2 size)
        {
            Vector2 mousePos = ImGui.GetMousePos();
            return mousePos.x >= pos.x && mousePos.x <= (pos + size).x && mousePos.y >= pos.y && mousePos.y <= (pos + size).y;
        }
        #endregion

        #region Clipper
        private static unsafe readonly ImGuiListClipper* s_clipper = ImGuiNative.ImGuiListClipper_ImGuiListClipper();

        public static unsafe void ListClipperBegin(int count = -1, float itemHeight = -1f)
        {
            ImGuiNative.ImGuiListClipper_Begin(s_clipper, count, itemHeight);
        }

        public static unsafe bool ListClipperStep()
        {
            return ImGuiNative.ImGuiListClipper_Step(s_clipper) == 1;
        }

        public static unsafe void ListClipperEnd()
        {
            ImGuiNative.ImGuiListClipper_End(s_clipper);
        }

        public static unsafe int ListClipperDisplayStart()
        {
            return s_clipper->DisplayStart;
        }

        public static unsafe int ListClipperDisplayEnd()
        {
            return s_clipper->DisplayEnd;
        }
        #endregion
    }

    public enum FontType
    {
        Regular,
        Bold
    }
}