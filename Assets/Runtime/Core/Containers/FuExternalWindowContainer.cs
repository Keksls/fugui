using System;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using ImGuiNET;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Collections.Generic;
using Fu.Framework;

namespace Fu.Core
{
    public class FuExternalWindowContainer : NativeWindow, IFuWindowContainer
    {
        #region Fields
        public event Action OnInitialized;
        public bool HasUIWindow
        {
            get
            {
                return UIWindow != null;
            }
        }
        public FuWindow UIWindow { get; private set; }
        public MonitorWindowState MonitorWindowState { get; private set; }
        public bool Dragging { get; private set; }
        UnityEngine.Vector2Int IFuWindowContainer.LocalMousePos { get => _mousePos; }
        UnityEngine.Vector2Int IFuWindowContainer.Position { get => _worldPosition; }
        UnityEngine.Vector2Int IFuWindowContainer.Size { get => _size; }
        public float Scale => 1f;

        private bool _contextInitialized;
        private MouseState _mouseState;
        private KeyboardState _keyboardState;
        private OpenTKImGuiRenderer _controller;
        private IGraphicsContext _context;
        private bool _started = false;
        private bool _needDisposeContexts = false;
        private FuExternalContext _fuguiContext;
        private float _mouseScrollWheel = 0f;
        private Array _keys = Enum.GetValues(typeof(Key));
        private bool _readyToRender = false;
        private bool _readyToNewFrame = false;
        private bool _resized = false;
        private UnityEngine.Vector2Int _startDragOffset;
        private UnityEngine.Vector2Int _mousePos;
        private List<char> _pressedChars = new List<char>();
        private ImGuiBackendFlags _backendFlags;
        private bool _waitingForFirstCompletLoop = true;
        private UnityEngine.Vector4 _backgroundColor;
        private ImGuiFontAtlasData _imGuiFontAtlasData;
        private bool _contextDisposed = false;
        private UnityEngine.Vector2Int _worldPosition;
        private UnityEngine.Vector2Int _lastPosition;
        private UnityEngine.Vector2Int _size;
        private IntPtr _windowHDC;
        private bool _drawWindowStatePanel = false;
        private UnityEngine.Vector2Int _noMotitorStatePosition;
        private UnityEngine.Vector2Int _noMotitorStateSize;
        private DrawData _drawData;
        private bool _isFirstTimeDragging = false;
        private bool _fireOnReadyNextFrame = false;
        #endregion

        public FuExternalWindowContainer(FuWindow window, FuExternalWindowFlags flags = FuExternalWindowFlags.Default)
        {
            // set imguiWindow object to render
            if (!TryAddWindow(window))
            {
                Close();
            }
            window.IsBusy = true;
            // show this window
            Visible = true;
            // set title to this window
            Title = window.ID;
            // show / hide title bar
            WindowBorder = flags.HasFlag(FuExternalWindowFlags.ShowWindowTitle) ? WindowBorder.Fixed : WindowBorder.Hidden;
            // store window device context handle ptr
            _windowHDC = WindowInfo.Handle;
            // assume that this window is started, render thread will set this as true once graphic context is created (used for threads loop)
            _started = true;
            // assume that we are in none monitor state
            MonitorWindowState = MonitorWindowState.None;
            // instantiate ImGui custom draw data
            _drawData = new DrawData();
            // store current imgui context to restore it after
            var lastImGuiContext = Fugui.CurrentContext;
            // create ImGui context
            _fuguiContext = Fugui.CreateExternalContext();
            // set nex imgui context as current
            _fuguiContext.SetAsCurrent();
            // set ImGui theme
            FuThemeManager.SetTheme(FuThemeManager.CurrentTheme);
            // get imgui IO for nex context
            ImGuiIOPtr io = ImGui.GetIO();
            // set default config to FuGui context
            Fugui.Manager.InitialConfiguration.ApplyTo(io);
            // force not to be Always tabBar for external windows
            io.ConfigDockingAlwaysTabBar = false;
            // get GL background color
            _backgroundColor = ImGui.GetStyle().Colors[(int)ImGuiCol.WindowBg];
            // set ImGui backendFlags as Vertex Offset enabled for mesh rendering
            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
            // set OpenTk / ImGui input Key mapping
            SetKeyMappings();
            // get font data to send to render thread (we don't want to do that into another thread)
            io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);
            // store font data to create font atlas GL texture into render thread
            _imGuiFontAtlasData = new ImGuiFontAtlasData()
            {
                Pixels = pixels,
                Width = width,
                Height = height
            };
            // get back to old fugui context
            lastImGuiContext.SetAsCurrent();

            // register Fugui render to main thread after main ImGui context render
            _fuguiContext.OnRender += FuguiContext_OnRender;
            // register Fugui post render to main thread after fuguiContext render
            _fuguiContext.OnPostRender += FuguiContext_OnPostRender;
            // register Fugui prepare frame to inject inputes at this time
            _fuguiContext.OnPrepareFrame += FuguiContext_OnPrepareFrame;
        }

        #region Container
        /// <summary>
        /// Try to add a window into this container
        /// </summary>
        /// <param name="UIWindow">window to add</param>
        /// <returns>true if succes</returns>
        public bool TryAddWindow(FuWindow uiWindow)
        {
            if (UIWindow != null)
            {
                return false;
            }
            // store UIWindow variable
            UIWindow = uiWindow;
            // set world pos before setting container
            SetPosition((int)UIWindow.WorldPosition.x, (int)UIWindow.WorldPosition.y + 36);
            UIWindow.Container = this;
            // register window events
            UIWindow.OnResize += UIWindow_OnResize;
            UIWindow.OnDrag += UIWindow_OnMove;
            // set pos and size
            UIWindow.LocalPosition = UnityEngine.Vector2Int.zero;
            SetSize(UIWindow.Size.x, UIWindow.Size.y);

            return true;
        }

        /// <summary>
        /// try to remove a window from this container
        /// </summary>
        /// <param name="id">id of the window to remove</param>
        /// <returns>true if succes</returns>
        public bool TryRemoveWindow(string id)
        {
            // check if container has window
            if (HasWindow(id))
            {
                // let's close this container, it will remove UIWindow
                Close();
                return true;
            }
            return false;
        }

        /// <summary>
        /// do this container need to force UI window position inside itsef ?
        /// </summary>
        /// <returns>return always true, because window need to be placed at 0,0 inside this container</returns>
        public bool ForcePos()
        {
            return true;
        }

        /// <summary>
        /// did this container has a window according to a it's id
        /// </summary>
        /// <param name="id">id of the window to check</param>
        /// <returns>true if contains</returns>
        public bool HasWindow(string id)
        {
            if (HasUIWindow && UIWindow.ID == id)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Render any IUIWindows that contains this container
        /// </summary>
        public void RenderUIWindows()
        {
            ImGuiIOPtr io = ImGui.GetIO();

            // if it's first frame, let's set textureID to UI (in this main thread)
            if (_waitingForFirstCompletLoop)
            {
                io.Fonts.SetTexID(_controller.FontTextureID);
                io.Fonts.ClearTexData();
            }

            // set UI per-frame data
            _backendFlags = io.BackendFlags;
            io.DisplaySize = new UnityEngine.Vector2(Width, Height);
            io.DisplayFramebufferScale = UnityEngine.Vector2.one;
            //io.DeltaTime = UIWindow.DeltaTime;

            // render UI window
            RenderUIWindow(UIWindow);
        }

        /// <summary>
        /// Render a single UIWindow into this container
        /// </summary>
        /// <param name="UIWindow">UIWindow to render</param>
        public void RenderUIWindow(FuWindow UIWindow)
        {
            // force set UI pos on appear (don't need to any frame because ForcePos() return true, it's checked into UIWindow src)
            ImGui.SetNextWindowPos(UnityEngine.Vector2.zero, ImGuiCond.Appearing);
            // call UIWindow.DrawWindow
            UIWindow.DrawWindow();
            // if needed, will draw state panel
            drawWindowStatePanel();
            // internalization check
            if (UIWindow.WantToEnter(Fugui.MainContainer))
            {
                Fugui.RemoveExternalWindow(UIWindow.ID);
                UIWindow.TryAddToContainer(Fugui.MainContainer);
            }
        }

        /// <summary>
        /// Get GL ID of a Unity Texture2D into GL related context
        /// </summary>
        /// <param name="texture">texture to get ID</param>
        /// <returns>GL context related texture ID</returns>
        public IntPtr GetTextureID(UnityEngine.Texture2D texture)
        {
            return _controller.GetTextureID(texture);
        }

        /// <summary>
        /// Get GL ID of a Unity RenderTexture into GL related context
        /// </summary>
        /// <param name="texture">texture to get ID</param>
        /// <returns>GL context related texture ID</returns>
        public IntPtr GetTextureID(UnityEngine.RenderTexture texture)
        {
            return _controller.GetTextureID(texture);
        }

        /// <summary>
        /// Draw an UI Image according to this container GL context
        /// </summary>
        /// <param name="texture">Texture2D to draw</param>
        /// <param name="size">size of the texture</param>
        public void ImGuiImage(UnityEngine.Texture2D texture, UnityEngine.Vector2 size)
        {
            ImGui.Image(GetTextureID(texture), size, UnityEngine.Vector2.zero, new UnityEngine.Vector2(1f, -1f));
        }

        /// <summary>
        /// Draw an UI Image according to this container GL context
        /// </summary>
        /// <param name="texture">RenderTexture to draw</param>
        /// <param name="size">size of the texture</param>
        public void ImGuiImage(UnityEngine.RenderTexture texture, UnityEngine.Vector2 size)
        {
            ImGui.Image(GetTextureID(texture), size, UnityEngine.Vector2.zero, new UnityEngine.Vector2(1f, -1f));
        }

        /// <summary>
        /// Draw an UI Image according to this container GL context
        /// </summary>
        /// <param name="texture">Texture2D to draw</param>
        /// <param name="size">size of the texture</param>
        /// <param name="color">tint color of the texture</param>
        public void ImGuiImage(UnityEngine.Texture2D texture, UnityEngine.Vector2 size, UnityEngine.Vector4 color)
        {
            ImGui.Image(GetTextureID(texture), size, UnityEngine.Vector2.zero, UnityEngine.Vector2.one, color);
        }

        /// <summary>
        /// Draw an UI Image according to this container GL context
        /// </summary>
        /// <param name="texture">RenderTexture to draw</param>
        /// <param name="size">size of the texture</param>
        /// <param name="color">tint color of the texture</param>
        public void ImGuiImage(UnityEngine.RenderTexture texture, UnityEngine.Vector2 size, UnityEngine.Vector4 color)
        {
            ImGui.Image(GetTextureID(texture), size, UnityEngine.Vector2.zero, UnityEngine.Vector2.one, color);
        }

        /// <summary>
        /// Draw an UI ImageButton according to this container GL context
        /// </summary>
        /// <param name="texture">Texture2D to draw</param>
        /// <param name="size">size of the texture</param>
        public bool ImGuiImageButton(UnityEngine.Texture2D texture, UnityEngine.Vector2 size)
        {
            // TODO : add ID to image button
            return ImGui.ImageButton("", GetTextureID(texture), size, UnityEngine.Vector2.zero, new UnityEngine.Vector2(1f, -1f));
        }

        /// <summary>
        /// Draw an UI ImageButton according to this container GL context
        /// </summary>
        /// <param name="texture">Texture2D to draw</param>
        /// <param name="size">size of the texture</param>
        /// <param name="color">tint additive color</param>
        public bool ImGuiImageButton(UnityEngine.Texture2D texture, UnityEngine.Vector2 size, UnityEngine.Vector4 color)
        {
            // TODO : add ID to image button
            return ImGui.ImageButton("", GetTextureID(texture), size, UnityEngine.Vector2.zero, new UnityEngine.Vector2(1f, -1f), ImGui.GetStyle().Colors[(int)ImGuiCol.Button], color);
        }
        #endregion

        #region UIWindow Events
        /// <summary>
        /// invoked when UIWindow move
        /// </summary>
        /// <param name="UIWindow">UIWindow that just move</param>
        private void UIWindow_OnMove(FuWindow UIWindow)
        {
            if (!_started)
            {
                return;
            }

            // set new window position according to current drag
            if (Dragging)
            {
                UnityEngine.Vector2 windowPos = Fugui.WorldMousePosition - _startDragOffset;
                SetPosition((int)windowPos.x, (int)windowPos.y, true);
                UIWindow.LocalPosition = UnityEngine.Vector2Int.zero;
            }
            else
            {
                SetPosition((int)UIWindow.WorldPosition.x, (int)UIWindow.WorldPosition.y);
            }
        }

        /// <summary>
        /// invoken when UIWindow resize
        /// </summary>
        /// <param name="UIWindow">UIWindow that just resize</param>
        private void UIWindow_OnResize(FuWindow UIWindow)
        {
            SetSize(UIWindow.Size.x, UIWindow.Size.y);
        }
        #endregion

        #region Window Methods
        /// <summary>
        /// invoken when this window want to close
        /// </summary>
        /// <param name="e">Cancel Event Args => allow to cancel closing</param>
        protected override void OnClosing(CancelEventArgs e)
        {
            // check if this window is ready to close (can close once context is disposed)
            if (!_contextDisposed)
            {
                // cancel closing, context need to be disposed first
                e.Cancel = true;
                // assume we are not started anymore => used by thread to stop
                _started = false;
                // we must now dispose GL contexts, will be done on render thread (TryDestroyContext())
                _needDisposeContexts = true;

                // unregister UIWindow events if exists
                if (UIWindow != null)
                {
                    UIWindow.OnResize -= UIWindow_OnResize;
                }
            }

            // send CancelEventArgs to base OpenTK NativeWindow
            base.OnClosing(e);
        }

        /// <summary>
        /// invoked when this window will surely close right now
        /// </summary>
        /// <param name="e">nothing usefull, just generic evt args</param>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            // unregister UI render loop event
            _fuguiContext.OnRender -= FuguiContext_OnRender;
            // dispose this window
            Dispose();
            UIWindow.Fire_OnRemovedFromContainer();
        }

        /// <summary>
        /// invoked when window size change
        /// </summary>
        /// <param name="e">nothing usefull, just generic evt args</param>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            // assume we just resize
            _resized = true;
        }

        /// <summary>
        /// invoked when mouse wheel value change
        /// </summary>
        /// <param name="e">Mouse Wheel Event Args</param>
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            // save current scroll (will be send to UI when needed)
            _mouseScrollWheel = e.DeltaPrecise;
        }

        /// <summary>
        /// invoked when a char key is pressed while this window got focus
        /// This event give char
        /// </summary>
        /// <param name="e">Key Press Event Args</param>
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            // add pressed char to list. the list will be sended to UI once needed
            _pressedChars.Add(e.KeyChar);
        }

        /// <summary>
        /// invoked when a key is pressed while this window got focus
        /// </summary>
        /// <param name="e">Keyboard Key EventArgs</param>
        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);

            // draw window panel if left control is pressed
            if (e.Key == Key.ControlLeft)
            {
                _drawWindowStatePanel = true;
            }

            // ceck if any externalisation key is pressed
            // if any, this window can now be internalized
            foreach (Key key in Fugui.Settings.InternalizationKey)
            {
                if (e.Key == key)
                {
                    UIWindow.CanInternalize = true;
                    break;
                }
            }
        }

        /// <summary>
        /// invoken when a key is not pressed anymore while this window got focus
        /// </summary>
        /// <param name="e">Keyboard Key EventArgs</param>
        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            base.OnKeyUp(e);
            // we release left control, stop displaying state panel
            if (e.Key == Key.ControlLeft)
            {
                _drawWindowStatePanel = false;
            }

            // check for internalization keys
            // if one of thes keys is released, we cannot internalize anymore
            foreach (Key key in Fugui.Settings.InternalizationKey)
            {
                if (e.Key == key)
                {
                    UIWindow.CanInternalize = false;
                    break;
                }
            }
        }

        /// <summary>
        /// invoked when mouse down on window
        /// </summary>
        /// <param name="e">mouse button evt args</param>
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButton.Left && !Dragging)
            {
                // click on window header, start dragging until release mouse
                if (e.X >= 0f && e.X < Width && e.Y >= 0f && e.Y < 20f)
                {
                    // assume we are dragging now
                    _startDragOffset = Fugui.WorldMousePosition - new UnityEngine.Vector2Int(X, Y);
                    Dragging = true;
                    UIWindow.IsDragging = true;
                }
            }
        }

        /// <summary>
        /// invoked when mouse up while window is focuses
        /// </summary>
        /// <param name="e">mouse button evt args</param>
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            // maximize if release window on top of screen
            if (Dragging && MonitorWindowState != MonitorWindowState.Maximized && _worldPosition.y <= MonitorsUtils.GetCurrentMonitor(_worldPosition.x + (_size.x / 2), false).WorkingArea.top)
            {
                SetMonitorState(MonitorWindowState.Maximized);
            }
            // set position according to monitor space
            else if (Dragging)
            {
                SetPosition(_worldPosition.x, _worldPosition.y);
            }
            // we are not dragging anymore
            Dragging = false;
            UIWindow.IsDragging = true;
        }

        /// <summary>
        /// invoked when mouse enter window rect
        /// </summary>
        /// <param name="e">nothing usefull, just generic evt args</param>
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            // force draw on next frame to be reactive on state update when mouse enter window rect
            UIWindow.ForceDraw();
        }
        #endregion

        #region Windows Native PInvoke
        /// <summary>
        /// check native material key state
        /// </summary>
        /// <param name="nVirtKey">material virtual key ID</param>
        /// <returns></returns>
        [DllImport("USER32.dll")]
        static extern short GetKeyState(int nVirtKey);
        private const int VK_LBUTTON = 0x01; // mouse left button virtual key
        private const int VK_RBUTTON = 0x02; // mouse right button vk
        private const int KEY_PRESSED = 0x8000; // key pressed state mask
        /// <summary>
        /// check whatever left moouse button is pressed
        /// </summary>
        /// <returns>true if pressed</returns>
        private bool isLeftMouseButtonPressed()
        {
            return Convert.ToBoolean(GetKeyState(VK_LBUTTON) & KEY_PRESSED);
        }

        /// <summary>
        /// Method to use to minimize / maximize window
        /// </summary>
        /// <param name="hWnd">handle of window device context</param>
        /// <param name="nCmdShow">state to set (max : 3, min : 6)</param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        // const that represent window maximize state
        private const int SW_MAXIMIZE = 3;
        // const that represent window minimize state
        private const int SW_MINIMIZE = 6;

        /// <summary>
        /// windows user32 method to map point to a window
        /// </summary>
        /// <param name="hWndFrom">window container (keep IntPtr.zer to related desktop)</param>
        /// <param name="hWndTo">windows to map (this window)</param>
        /// <param name="lpPoints">points to map (in world pos)</param>
        /// <param name="cPoints">nb of points</param>
        /// <returns>nb of inside points</returns>
        [DllImport("user32.dll")]
        private static extern int MapWindowPoints(IntPtr hWndFrom, IntPtr hWndTo, ref UnityEngine.Vector2Int lpPoints, uint cPoints);
        #endregion

        #region Inputs
        /// <summary>
        /// Inject window inputs to UI IO
        /// Must be call every UI frame (into UI thread)
        /// </summary>
        private void InjectImGuiInput()
        {
            // Get UI IO
            ImGuiIOPtr io = ImGui.GetIO();

            // send mouse buttons state to UI IO
            io.MouseDown[0] = _mouseState[MouseButton.Left];
            io.MouseDown[1] = _mouseState[MouseButton.Right];
            io.MouseDown[2] = _mouseState[MouseButton.Middle];

            // send keyboard keys states to UI IO
            foreach (Key key in _keys)
            {
                if (key == Key.Unknown)
                {
                    continue;
                }
                io.KeysDown[(int)key] = _keyboardState.IsKeyDown(key);
            }

            // send pressed chars to UI IO
            foreach (var c in _pressedChars)
            {
                io.AddInputCharacter(c);
            }
            _pressedChars.Clear();

            // send specials keys states to UI IO
            io.KeyCtrl = _keyboardState.IsKeyDown(Key.ControlLeft) || _keyboardState.IsKeyDown(Key.ControlRight);
            io.KeyAlt = _keyboardState.IsKeyDown(Key.AltLeft) || _keyboardState.IsKeyDown(Key.AltRight);
            io.KeyShift = _keyboardState.IsKeyDown(Key.ShiftLeft) || _keyboardState.IsKeyDown(Key.ShiftRight);
            io.KeySuper = _keyboardState.IsKeyDown(Key.WinLeft) || _keyboardState.IsKeyDown(Key.WinRight);

            // set UI IO mouse pos
            io.MousePos = _mousePos;
            // set UI IO mouse scroll wheel
            io.MouseWheel = _mouseScrollWheel;
            io.MouseWheelH = 0f;
            _mouseScrollWheel = 0f;

            // set new window position according to current drag
            if (Dragging)
            {
                UnityEngine.Vector2 windowPos = Fugui.WorldMousePosition - _startDragOffset;
                SetPosition((int)windowPos.x, (int)windowPos.y, true);
            }
        }

        /// <summary>
        /// Initialize key mapping to UI IO
        /// Must be call once at UI context initialization
        /// </summary>
        private static void SetKeyMappings()
        {
            ImGuiIOPtr io = ImGui.GetIO();
            io.KeyMap[(int)ImGuiKey.Tab] = (int)Key.Tab;
            io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Key.Left;
            io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Key.Right;
            io.KeyMap[(int)ImGuiKey.UpArrow] = (int)Key.Up;
            io.KeyMap[(int)ImGuiKey.DownArrow] = (int)Key.Down;
            io.KeyMap[(int)ImGuiKey.PageUp] = (int)Key.PageUp;
            io.KeyMap[(int)ImGuiKey.PageDown] = (int)Key.PageDown;
            io.KeyMap[(int)ImGuiKey.Home] = (int)Key.Home;
            io.KeyMap[(int)ImGuiKey.End] = (int)Key.End;
            io.KeyMap[(int)ImGuiKey.Delete] = (int)Key.Delete;
            io.KeyMap[(int)ImGuiKey.Backspace] = (int)Key.BackSpace;
            io.KeyMap[(int)ImGuiKey.Enter] = (int)Key.Enter;
            io.KeyMap[(int)ImGuiKey.Escape] = (int)Key.Escape;
            io.KeyMap[(int)ImGuiKey.A] = (int)Key.A;
            io.KeyMap[(int)ImGuiKey.C] = (int)Key.C;
            io.KeyMap[(int)ImGuiKey.V] = (int)Key.V;
            io.KeyMap[(int)ImGuiKey.X] = (int)Key.X;
            io.KeyMap[(int)ImGuiKey.Y] = (int)Key.Y;
            io.KeyMap[(int)ImGuiKey.Z] = (int)Key.Z;
        }

        /// <summary>
        /// Compute GL fixed update (process window inputs and native events)
        /// Must be called in window creation thread
        /// </summary>
        public void RegisterInputs()
        {
            if (!_started)
            {
                return;
            }

            // first smooth externalizationdrag just end
            if (_isFirstTimeDragging && !isLeftMouseButtonPressed())
            {
                Dragging = false;
                UIWindow.IsDragging = false;
                _isFirstTimeDragging = false;
            }

            // it's first frame, check if we still need to drag window for smooth externalization
            if (_waitingForFirstCompletLoop)
            {
                // if clicked, start dragg. be carefull, NativeWindow does not received this event
                // so we will need to handler stop dragg for this first drag
                if (isLeftMouseButtonPressed())
                {
                    // place center of window header to mouse cursor, maybe not
                    SetPosition((int)Fugui.WorldMousePosition.x - _size.x / 2, (int)Fugui.WorldMousePosition.y - 20, true);
                    _startDragOffset = Fugui.WorldMousePosition - new UnityEngine.Vector2Int(_worldPosition.x, _worldPosition.y);
                    Dragging = true;
                    UIWindow.IsDragging = true;
                    _isFirstTimeDragging = true;
                }
            }

            // process window events
            ProcessEvents();

            // get current mouse & keyboard state (fail as fuck => do it ourself)
            _mouseState = Mouse.GetState();
            _keyboardState = Keyboard.GetState();

            UnityEngine.Vector2Int absMousePos = new UnityEngine.Vector2Int(Fugui.WorldMousePosition.x, Fugui.WorldMousePosition.y);
            // map cursor pos to window pos. first is IntPtr.Zero for relative desktop context
            MapWindowPoints(IntPtr.Zero, _windowHDC, ref absMousePos, 1);
            // save mouse relative pos
            _mousePos = new UnityEngine.Vector2Int(absMousePos.x, absMousePos.y);

            // Update Window State
            UIWindow.UpdateState(_mouseState[MouseButton.Left]);
        }
        #endregion

        #region Rendering
        /// <summary>
        /// Create Graphic context if needed
        /// </summary>
        public void TryCreateContext()
        {
            // ignore it if context already initialized
            if (_contextInitialized)
            {
                return;
            }

            // create window related OpenGL graphic context
            _context = new GraphicsContext(GraphicsMode.Default, WindowInfo);
            // load all default data into OpenGL context
            _context.LoadAll();

            // wait until the end of a frame (to ensure we are setting UIcontext outside of UUI render loop)
            while (Fugui.IsRendering)
            {
                continue;
            }
            // set graphic context as Current
            _context.MakeCurrent(WindowInfo);
            // create UI OpenTK renderer and UI font atlas
            _controller = new OpenTKImGuiRenderer(_imGuiFontAtlasData.Pixels, _imGuiFontAtlasData.Width, _imGuiFontAtlasData.Height);

            // set this window as render started (used to avoid crash on multiple window close)
            _readyToNewFrame = true;
            _readyToRender = false;
            _contextInitialized = true;
        }

        /// <summary>
        /// Destroy Graphic context if needed
        /// </summary>
        public void TryDestroyContext()
        {
            if (!_needDisposeContexts)
            {
                return;
            }

            // set context as current to dispose controller
            try
            {
                _context.MakeCurrent(WindowInfo);
            }
            catch
            {
                UnityEngine.Debug.Log("fail to make current context before killing it");
            }
            // dispose controller (dispone GL resources such as textures and shaders)
            try
            {
                _controller.Dispose();
            }
            catch
            {
                UnityEngine.Debug.Log("fail to dispose controller");
            }
            // set current controller as null to release GC handle
            try
            {
                _context.MakeCurrent(null);
            }
            catch
            {
                UnityEngine.Debug.Log("fail to make current context as null before killing it");
            }
            // dispose the window related GL context
            try
            {
                _context.Dispose();
            }
            catch
            {
                UnityEngine.Debug.Log("fail to dispose context");
            }
            _contextDisposed = true;
            _needDisposeContexts = false;
            UnityEngine.Debug.Log("context disposed");
            Close();
        }

        /// <summary>
        /// this will be called every frame after main UI render
        /// </summary>
        private void FuguiContext_OnRender()
        {
            // prevent UI to render until next frame is draw
            _readyToNewFrame = false;
            // do UI render callback into main thread
            RenderUIWindows();
            // notify that UI is ready to render in GL thread
            _readyToRender = true;

            // notify OnReady if needed
            if (_fireOnReadyNextFrame)
            {
                UIWindow.Fire_OnReady();
                _fireOnReadyNextFrame = false;
            }
        }

        /// <summary>
        /// this will be called every frame after FuguiContext render
        /// </summary>
        private void FuguiContext_OnPostRender()
        {
            _drawData.Bind(ImGui.GetDrawData());
        }

        /// <summary>
        /// this will be called every frame berore FuguiContext create NewFrame
        /// It's time to inject inputs if needed
        /// </summary>
        private bool FuguiContext_OnPrepareFrame()
        {
            // return we are not started or no context is initialized
            if (!_started || !_contextInitialized || !_readyToNewFrame || !UIWindow.MustBeDraw())
            {
                return false;
            }

            // update UI Inputs
            InjectImGuiInput();
            // do fixed update (that will register inputs)
            RegisterInputs();

            return true;
        }

        /// <summary>
        /// Do GL render
        /// Must be called in GL context creation thread
        /// </summary>
        public void GLRender()
        {
            // wait for UI ended render
            if (!_readyToRender || !_started)
            {
                return;
            }

            // notify that GL is now not ready to render in GL thread
            _readyToRender = false;

            // set window graphic context as OpenGl current context
            _context.MakeCurrent(WindowInfo);
            if (_resized)
            {
                // Update the opengl viewport if needed
                GL.Viewport(0, 0, ClientSize.Width, ClientSize.Height);
                _resized = false;
            }
            // OpenGL context clear buffer
            GL.ClearColor(_backgroundColor.x, _backgroundColor.y, _backgroundColor.z, _backgroundColor.w);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            // draw UI frame
            _controller.RenderImDrawData(_drawData, new UnityEngine.Vector2(Width, Height), _backendFlags);
            // swap buffers (OpenGL work with 2 buffer, a back and a front, this switch beetwin them to display the current frame)
            _context.SwapBuffers();
            // reliease Gl current context so we can switch next time
            _context.MakeCurrent(null);

            // notify that this window is initialized
            if (_waitingForFirstCompletLoop)
            {
                OnInitialized?.Invoke();
                UIWindow.IsBusy = false;
                _waitingForFirstCompletLoop = false;
                UIWindow_OnResize(UIWindow);
                UIWindow_OnMove(UIWindow);
                _fireOnReadyNextFrame = true;
            }

            // assume we are ready to compute next frame
            _readyToNewFrame = true;
        }
        #endregion

        #region Positioning and Sizing
        /// <summary>
        /// smart set position of this window in monitor space
        /// </summary>
        /// <param name="x">x position we try to reach</param>
        /// <param name="y">y position we try to reach</param>
        /// <param name="force">do we force window to go to the position</param>
        public void SetPosition(int x, int y, bool force = false)
        {
            if (!force)
            {
                _worldPosition = MonitorsUtils.GetBestPos(x, y, _size.x, _size.y, _lastPosition.x, _lastPosition.y);
                _lastPosition = _worldPosition;
            }
            else
            {
                _worldPosition = new UnityEngine.Vector2Int(x, y);
            }
            X = _worldPosition.x;
            Y = _worldPosition.y;

            // ensure we take back Idle size if we drag as we was monitor docked
            if (!force && !UIWindow.IsBusy && Dragging)
            {
                switch (MonitorWindowState)
                {
                    case MonitorWindowState.Maximized:
                    case MonitorWindowState.HalfLeft:
                    case MonitorWindowState.HalfRight:
                    case MonitorWindowState.HalfTop:
                    case MonitorWindowState.HalfBottom:
                        SetMonitorState(MonitorWindowState.None);
                        break;
                }
            }
        }

        /// <summary>
        /// smart set size of this window in monitor space
        /// </summary>
        /// <param name="width">width we try to reach</param>
        /// <param name="height">height we try to reach</param>
        /// <param name="force">do we force window to resize</param>
        public void SetSize(int width, int height, bool force = false)
        {
            if (!force)
            {
                _size = MonitorsUtils.GetBestSize(_worldPosition.x, _worldPosition.y, width, height);
            }
            else
            {
                _size = new UnityEngine.Vector2Int(width, height);
            }
            UIWindow.Size = _size;
            Width = _size.x;
            Height = _size.y;
        }

        /// <summary>
        /// set current window state
        /// </summary>
        /// <param name="state">state to set</param>
        public void SetMonitorState(MonitorWindowState state)
        {
            MonitorRect maxRect = MonitorsUtils.GetCurrentMonitor(_worldPosition.x + (_size.x / 2), false).WorkingArea;
            int x, y, width, height;
            switch (state)
            {
                case MonitorWindowState.Maximized:
                    x = maxRect.left;
                    y = maxRect.top;
                    width = maxRect.right - maxRect.left;
                    height = maxRect.bottom - maxRect.top;
                    break;

                case MonitorWindowState.Minimized:
                    ShowWindow(_windowHDC, SW_MINIMIZE);
                    return;

                case MonitorWindowState.HalfLeft:
                    width = (maxRect.right - maxRect.left) / 2;
                    height = maxRect.bottom - maxRect.top;
                    x = maxRect.left;
                    y = maxRect.top;
                    break;

                case MonitorWindowState.HalfRight:
                    width = (maxRect.right - maxRect.left) / 2;
                    height = maxRect.bottom - maxRect.top;
                    x = maxRect.right - width;
                    y = maxRect.top;
                    break;

                case MonitorWindowState.HalfTop:
                    width = maxRect.right - maxRect.left;
                    height = (maxRect.bottom - maxRect.top) / 2;
                    x = maxRect.left;
                    y = maxRect.top;
                    break;

                case MonitorWindowState.HalfBottom:
                    width = maxRect.right - maxRect.left;
                    height = (maxRect.bottom - maxRect.top) / 2;
                    x = maxRect.left;
                    y = maxRect.bottom - height;
                    break;

                default:
                case MonitorWindowState.Center:
                    width = (maxRect.right - maxRect.left) / 2;
                    height = (maxRect.bottom - maxRect.top) / 2;
                    x = maxRect.left + (width / 2);
                    y = maxRect.top + (height / 2);
                    break;

                case MonitorWindowState.None:
                    width = _noMotitorStateSize.x;
                    height = _noMotitorStateSize.y;
                    x = _noMotitorStatePosition.x;
                    y = _noMotitorStatePosition.y;
                    break;
            }
            // save old state if we was into free mode (none motinor state)
            if (MonitorWindowState == MonitorWindowState.None)
            {
                _noMotitorStatePosition = _worldPosition;
                _noMotitorStateSize = _size;
            }

            SetPosition(x, y, true);
            SetSize(width, height, true);
            MonitorWindowState = state;
            Dragging = false;

            // if new state is None and we are dragging, let's keep dragging and re pos 
            if (MonitorWindowState == MonitorWindowState.None && _mouseState[MouseButton.Left])
            {
                // place center of window header to mouse cursor
                SetPosition((int)Fugui.WorldMousePosition.x - _size.x / 2, (int)Fugui.WorldMousePosition.y - 20, true);
                _startDragOffset = Fugui.WorldMousePosition - new UnityEngine.Vector2Int(_worldPosition.x, _worldPosition.y);
                Dragging = true;
                UIWindow.IsDragging = true;
            }

            UIWindow.Fire_OnResize();
            UIWindow.Fire_OnDrag();
        }

        /// <summary>
        /// draw window state panel
        /// </summary>
        private void drawWindowStatePanel()
        {
            if (!_drawWindowStatePanel)
            {
                return;
            }

            ImGui.SetNextWindowPos(new UnityEngine.Vector2(0f, 0f), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new UnityEngine.Vector2(_size.x, -1f), ImGuiCond.Always);
            ImGui.SetNextWindowBgAlpha(0f);
            ImGui.Begin(UIWindow.ID + "statePanel", ImGuiWindowFlags.NoDecoration);

            // calc sizes
            float width = Math.Min(_size.x, _size.y);
            if (width > 3f * 64f)
                width = 3f * 64f;
            float colSize = width / 3f;
            UnityEngine.Vector2 buttonSize = new UnityEngine.Vector2(colSize - 8f, colSize - 8f);
            UnityEngine.Vector4 buttonIconColor = new UnityEngine.Vector4(1, 1, 1, 0.5f);

            ImGui.SetCursorPosX((_size.x) / 2f - (width / 2f));
            ImGui.SetCursorPosY((_size.y) / 2f - (width / 2f));
            ImGui.BeginChild("windowMonitorStatePanel", new UnityEngine.Vector2(width, width), false);
            {
                ImGui.Columns(3, "windowMonitorStatePanelCols", false);
                ImGui.SetColumnWidth(0, colSize);
                ImGui.SetColumnWidth(1, colSize);
                ImGui.SetColumnWidth(2, colSize);

                // switch up left column
                ImGui.NextColumn();

                // half top icon
                Fugui.Push(ImGuiCol.Button, new UnityEngine.Vector4(0.1f, 0.1f, 0.1f, 0.1f));
                if (ImGuiImageButton(Fugui.Settings.TopIcon, buttonSize, buttonIconColor))
                {
                    UIWindow.IsBusy = true;
                    SetMonitorState(MonitorWindowState.HalfTop);
                    UIWindow.IsBusy = false;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Send to screen top");
                }
                ImGui.NextColumn();

                // maximize icon
                if (ImGuiImageButton(Fugui.Settings.MaximizeIcon, buttonSize, buttonIconColor))
                {
                    UIWindow.IsBusy = true;
                    SetMonitorState(MonitorWindowState.Maximized);
                    UIWindow.IsBusy = false;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Maximize this window");
                }
                ImGui.NextColumn();

                // left icon
                if (ImGuiImageButton(Fugui.Settings.LeftIcon, buttonSize, buttonIconColor))
                {
                    UIWindow.IsBusy = true;
                    SetMonitorState(MonitorWindowState.HalfLeft);
                    UIWindow.IsBusy = false;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Send to screen left");
                }
                ImGui.NextColumn();

                // center icon
                if (ImGuiImageButton(Fugui.Settings.CenterIcon, buttonSize, buttonIconColor))
                {
                    UIWindow.IsBusy = true;
                    SetMonitorState(MonitorWindowState.Center);
                    UIWindow.IsBusy = false;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Send to screen center");
                }
                ImGui.NextColumn();

                // right icon
                if (ImGuiImageButton(Fugui.Settings.RightIcon, buttonSize, buttonIconColor))
                {
                    UIWindow.IsBusy = true;
                    SetMonitorState(MonitorWindowState.HalfRight);
                    UIWindow.IsBusy = false;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Send to screen right");
                }
                ImGui.NextColumn();

                // minimized icon
                if (ImGuiImageButton(Fugui.Settings.MinimizeIcon, buttonSize, buttonIconColor))
                {
                    UIWindow.IsBusy = true;
                    SetMonitorState(MonitorWindowState.Minimized);
                    UIWindow.IsBusy = false;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Minimize this window");
                }
                ImGui.NextColumn();

                // bottom icon
                if (ImGuiImageButton(Fugui.Settings.BottomIcon, buttonSize, buttonIconColor))
                {
                    UIWindow.IsBusy = true;
                    SetMonitorState(MonitorWindowState.HalfBottom);
                    UIWindow.IsBusy = false;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Send to screen bottom");
                }
                Fugui.PopColor();

                ImGui.NextColumn();
                ImGui.Columns(1);
            }
            ImGui.EndChild();
            ImGui.End();
        }
        #endregion
    }

    /// <summary>
    /// struct that contains imgui font atlas data
    /// </summary>
    internal struct ImGuiFontAtlasData
    {
        public IntPtr Pixels;
        public int Width;
        public int Height;
    }
}