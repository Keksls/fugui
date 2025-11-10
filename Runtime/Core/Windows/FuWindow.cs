using Fu.Framework;
using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Class that represent an ImGui Window
    /// ImGuiWindow is used for externalisable and/or dockable window
    /// </summary>
    public class FuWindow
    {
        #region Variables
        // Container
        private IFuWindowContainer _container;
        public IFuWindowContainer Container
        {
            get { return _container; }
            internal set
            {
                _container = value;
                if (value == null)
                {
                    OnRemovedFromContainer?.Invoke(this);
                    Fugui.Fire_OnWindowRemovedFromContainer(this);
                }
                else
                {
                    OnAddToContainer?.Invoke(this);
                    Fugui.Fire_OnWindowAddToContainer(this);
                }

                // release focus if this window is focued
                if (InputFocusedWindow == this)
                {
                    // this will release focus on next frame
                    NbInputFocusedWindow = 0;
                    InputFocusedWindow = null;
                }
            }
        }
        // properties
        public FuWindowName WindowName { get; private set; }
        public string ID { get; private set; }
        public Action<FuWindow, FuLayout> UI { get; set; }
        public Action<FuWindow, Vector2> HeaderUI { get; set; }
        public Action<FuWindow, Vector2> FooterUI { get; set; }
        public bool HasMovedThisFrame { get; private set; }
        public bool HasJustBeenDraw { get; set; }
        public bool WantCaptureKeyboard { get; private set; }
        public bool CanInternalize { get; internal set; }
        public int TargetFPS
        {
            get
            {
                return (int)(1f / _targetDeltaTimeMs);
            }
            internal set
            {
                _targetDeltaTimeMs = 1f / value;
            }
        }
        public float DeltaTime { get; internal set; }
        public float CurrentFPS { get; internal set; }
        public uint CurrentDockID { get; private set; }
        public FuWindowState State { get; private set; }
        public DrawList DrawList { get; private set; }
        public Dictionary<string, DrawList> ChildrenDrawLists { get; private set; }
        public FuMouseState Mouse { get; private set; }
        public FuKeyboardState Keyboard { get; private set; }
        public Dictionary<string, FuOverlay> Overlays { get; private set; }
        private static FuWindow _inputFocusedWindow;
        public static FuWindow InputFocusedWindow
        {
            get { return _inputFocusedWindow; }
            internal set { _inputFocusedWindow = value; }
        }
        private static int _nbInputFocusedWindow;
        public static int NbInputFocusedWindow
        {
            get { return _nbInputFocusedWindow; }
            internal set
            {
                _nbInputFocusedWindow = value;
                if (_nbInputFocusedWindow <= 0 && InputFocusedWindow != null)
                {
                    InputFocusedWindow._releaseFocusNextFrame = true;
                }
            }
        }
        public FuLayout Layout { get; internal set; }

        // states flags
        public bool HasFocus { get; internal set; }
        public bool IsOpened { get { return _open; } internal set { _open = value; } }

        // behaviour flags
        public bool IsDockable { get; private set; }
        public bool IsClosable { get; private set; }
        public bool NoDockingOverMe { get; private set; }
        public bool IsExternalizable { get; private set; }
        public bool IsInitialized { get; private set; }
        public bool IsExternal { get; internal set; }

        // external window flags
        public bool UseNativeTitleBar { get; private set; }
        public bool NoTaskBarIcon { get; private set; }
        public bool NoFocusOnAppearing { get; private set; }
        public bool AlwaysOnTop { get; private set; }
        public bool NoModal { get; private set; }
        public bool NoNotify { get; private set; }
        public bool NoContextMenu { get; private set; }

        public bool IsResizing { get; private set; }
        public bool IsDragging { get; internal set; }
        public bool IsHovered { get; internal set; }
        public bool IsHoveredContent { get { return IsHovered && !Mouse.IsHoverOverlay && !Mouse.IsHoverPopup && !Mouse.IsHoverTopBar; } }
        public bool IsDocked { get; internal set; }
        internal bool IsImguiDocked => IsDocked;
        public bool IsBusy { get; internal set; }
        public bool IsInterractable { get; set; }
        public bool IsVisible { get; private set; }
        public bool DrawingForced { get { return _forceRedraw > 0; } }

        // events
        public event Action<FuWindow> OnResize;
        public event Action<FuWindow> OnResized;
        public event Action<FuWindow> OnDrag;
        public event Action<FuWindow> OnClosed;
        public event Action<FuWindow> OnDock;
        public event Action<FuWindow> OnUnDock;
        public event Action<FuWindow> OnInitialized;
        public event Action<FuWindow> OnRemovedFromContainer;
        public event Action<FuWindow> OnAddToContainer;
        public event Action<FuWindow> OnPreDraw;
        public event Action<FuWindow> OnPostDraw;
        public event Action<FuWindow> OnBodyDraw;
        public event Action<FuWindow> OnHoverIn;
        public event Action<FuWindow> OnHoverOut;

        // private fields
        internal ImGuiWindowFlags _windowFlags;
        private bool _forceLocationNextFrame = false;
        private bool _forceSizeNextFrame = false;
        private bool _forceFocusNextFrame = false;
        private int _forceRedraw = 0;
        private bool _sendReadyNextFrame = false;
        internal bool _ignoreResizeForThisFrame = false;
        private Vector2Int _lastFrameSize;
        private Vector2Int _lastFramePos;
        private bool _lastFrameVisible;
        internal float _targetDeltaTimeMs = 1;
        internal float _lastRenderTime = 0;
        private bool _open = true;
        private static int _windowIndex = 0;
        private bool _ignoreTransformThisFrame = false;
        // var to count how many push are at frame start, so we can pop missing push
        private static int _nbColorPushOnFrameStart = 0;
        private static int _nbStylePushOnFrameStart = 0;
        private static int _nbFontPushOnFrameStart = 0;
        internal bool _releaseFocusNextFrame = false;

        // static fields
        public static FuWindow CurrentDrawingWindow { get; private set; }
        #endregion

        #region Window Location
        // unscaled private The height of the window topBar (optional)   
        private float _headerHeight;
        public float HeaderHeight
        {
            get
            {
                return _headerHeight * (Container?.Context.Scale ?? 1f);
            }
            set
            {
                _headerHeight = value;
            }
        }

        // unscaled private The height of the window bottomBar (optional)        
        private float _footerHeight;
        public float FooterHeight
        {
            get
            {
                return _footerHeight * (Container?.Context.Scale ?? 1f);
            }
            set
            {
                _footerHeight = value;
            }
        }
        internal Vector2Int _size;
        public Vector2Int Size
        {
            get { return _size; }
            set
            {
                _size = value;
                _localRect = new Rect(_localPosition, _size);
                _forceSizeNextFrame = true;
            }
        }
        private Vector2Int _workingAreaSize;
        public Vector2Int WorkingAreaSize
        {
            get { return _workingAreaSize; }
        }
        private Vector2Int _workingAreaPosition;
        public Vector2Int WorkingAreaPosition
        {
            get { return _workingAreaPosition; }
        }
        public Vector2Int WorkingAreaMousePosition
        {
            get { return Mouse.Position - _workingAreaPosition; }
        }

        // absolute
        public Rect WorldRect
        {
            get { return new Rect(WorldPosition, Size); }
        }
        public Vector2Int WorldPosition
        {
            get { return _localPosition + Container?.Position ?? Vector2Int.zero; }
        }

        // local
        private Rect _localRect;
        private Vector2Int _localPosition;
        public Rect LocalRect { get { return _localRect; } }
        public Vector2Int LocalPosition
        {
            get { return _localPosition; }
            set
            {
                _localPosition = value;
                _localRect = new Rect(_localPosition, _size);
                _forceLocationNextFrame = true;
            }
        }
        #endregion

        /// <summary>
        /// Instantiate a new UIWindow object
        /// </summary>
        /// <param name="windowDefinition">UI Window Definition</param>
        public FuWindow(FuWindowDefinition windowDefinition)
        {
            WindowName = windowDefinition.WindowName;
            ID = WindowName.Name + "##" + _windowIndex;
            _windowIndex++;

            if (!Fugui.TryAddUIWindow(this))
            {
                return;
            }
            _ignoreTransformThisFrame = true;
            IsBusy = true;
            IsInitialized = false;
            UI = windowDefinition.UI;
            IsOpened = true;
            HasFocus = false;
            Size = windowDefinition.Size;
            _lastFrameVisible = false;

            // behaviour flags
            IsDockable = windowDefinition.IsDockable;
            IsExternalizable = windowDefinition.IsExternalizable;
            IsInterractable = windowDefinition.IsInterractif;
            IsClosable = windowDefinition.IsClosable;
            LocalPosition = windowDefinition.Position;
            NoDockingOverMe = windowDefinition.NoDockingOverMe;

            // external window flags
            UseNativeTitleBar = windowDefinition.UseNativeTitleBar;
            NoTaskBarIcon = windowDefinition.NoTaskBarIcon;
            NoFocusOnAppearing = windowDefinition.NoFocusOnAppearing;
            AlwaysOnTop = windowDefinition.AlwaysOnTop;
            NoModal = windowDefinition.NoModal;
            NoNotify = windowDefinition.NoNotify;
            NoContextMenu = windowDefinition.NoContextMenu;

            // top and bottom bar
            HeaderHeight = windowDefinition.HeaderHeight;
            FooterHeight = windowDefinition.BottomBarHeight;
            HeaderUI = windowDefinition.HeaderUI;
            FooterUI = windowDefinition.FooterUI;

            // add default overlays
            Overlays = new Dictionary<string, FuOverlay>();
            foreach (FuOverlay overlay in windowDefinition.Overlays.Values)
            {
                overlay.AnchorWindow(this);
            }
        }

        /// <summary>
        /// Initialize the window inide a container.
        /// This must be called whenever the container of this window is set and ready.
        /// </summary>
        public void InitializeOnContainer()
        {
            Keyboard = new FuKeyboardState(Container.Context.IO, this);
            _forceLocationNextFrame = true;
            _forceSizeNextFrame = true;
            Mouse = new FuMouseState();
            DrawList = new DrawList();
            ChildrenDrawLists = new Dictionary<string, DrawList>();
            _lastFrameSize = Size;
            _lastFramePos = LocalPosition;
            // prevent to replace window flag if there is some
            if (_windowFlags == ImGuiWindowFlags.None)
            {
                _windowFlags = ImGuiWindowFlags.NoCollapse;
            }
            if (!IsDockable)
            {
                _windowFlags |= ImGuiWindowFlags.NoDocking;
            }
            // assume that we are Idle
            State = FuWindowState.Idle;
            TargetFPS = Fugui.Settings.IdleFPS;
            // compute last render time
            _lastRenderTime = Fugui.Time;
            IsInitialized = true;
            IsBusy = false;
            _sendReadyNextFrame = true;
            // place window in center of container if location is -1 -1
            if (LocalPosition == new Vector2Int(-1, -1))
            {
                LocalPosition = new Vector2Int((Container.Size.x - Size.x) / 2, (Container.Size.y - Size.y) / 2);
            }
            ForceDraw();
        }

        #region Drawing
        /// <summary>
        /// Draw the UI of this window
        /// </summary>
        public virtual void DrawWindow()
        {
            if (!IsInitialized)
            {
                return;
            }

            _ignoreResizeForThisFrame = false;
            HasMovedThisFrame = false;
            Vector2Int newFrameSize = Size;
            Vector2Int newFramePos = LocalPosition;

            // release input focused window if needed
            if (_releaseFocusNextFrame)
            {
                InputFocusedWindow = null;
                _releaseFocusNextFrame = false;
            }

            // update mouse buttons states
            Mouse.UpdateState(this);
            Keyboard.UpdateState();

            // we need to draw ImGui Window component (Begin/End)
            if (!IsDocked)
            {
                if (Container.ForcePos() || _forceLocationNextFrame)
                {     // it's a floating dock node window, because user set 'ConfigDockingAlwaysTabBar' to true in settings
                    if (IsImguiDocked && CurrentDockID != 0)
                    {
                        unsafe
                        {
                            ImGuiDockNode* node = NativeDocking.igDockBuilderGetNode(CurrentDockID);
                            if (new IntPtr(node) != IntPtr.Zero && NativeDocking.ImGuiDockNode_IsFloatingNode(node))
                            {
                                NativeDocking.igDockBuilderSetNodePos(CurrentDockID, LocalPosition);
                                NativeDocking.igDockBuilderFinish(CurrentDockID);
                            }
                        }
                    }
                    else
                    {
                        ImGui.SetNextWindowPos(LocalPosition, ImGuiCond.Always);
                    }
                    _forceLocationNextFrame = false;
                }
                if (_forceSizeNextFrame)
                {
                    // it's a floating dock node window, because user set 'ConfigDockingAlwaysTabBar' to true in settings
                    if (IsImguiDocked && CurrentDockID != 0)
                    {
                        unsafe
                        {
                            ImGuiDockNode* node = NativeDocking.igDockBuilderGetNode(CurrentDockID);
                            if (new IntPtr(node) != IntPtr.Zero && NativeDocking.ImGuiDockNode_IsFloatingNode(node))
                            {
                                NativeDocking.igDockBuilderSetNodeSize(CurrentDockID, Size);
                                NativeDocking.igDockBuilderFinish(CurrentDockID);
                            }
                        }
                    }
                    // it's a regular window
                    else
                    {
                        ImGui.SetNextWindowSize(Size);
                    }
                }
                _forceSizeNextFrame = false;
            }
            if (_forceFocusNextFrame)
            {
                ImGui.SetNextWindowFocus();
                _forceFocusNextFrame = false;
            }
            DrawWindowBody(ref newFrameSize, ref newFramePos);

            // ImGui want to close this window
            if (!IsOpened)
            {
                Close(null);
                return;
            }

            if (!_ignoreTransformThisFrame)
            {
                // handle ImGui window local move
                if (_lastFramePos != newFramePos)
                {
                    LocalPosition = newFramePos;
                    HasMovedThisFrame = true;
                    Fire_OnDrag();
                    if (!IsDragging && Container.Mouse.IsPressed(FuMouseButton.Left))
                    {
                        IsDragging = true;
                    }
                    Fugui.ForceDrawAllWindows();
                }

                // handle ImGui window resize
                if (_lastFrameSize != newFrameSize)
                {
                    if (!_forceSizeNextFrame || IsDocked)
                    {
                        _size = newFrameSize;
                        _localRect = new Rect(_localPosition, _size);
                    }
                    Fire_OnResize();
                    Fugui.ForceDrawAllWindows();
                }
            }
            _ignoreTransformThisFrame = false;

            // drag state
            if (IsDragging && !Container.Mouse.IsPressed(FuMouseButton.Left))
            {
                IsDragging = false;
            }

            // save frame size and position
            _lastFramePos = LocalPosition;
            _lastFrameSize = Size;
        }

        /// <summary>
        /// Draw the body of this window
        /// </summary>
        /// <param name="newFrameSize"></param>
        /// <param name="newFramePos"></param>
        public virtual unsafe void DrawWindowBody(ref Vector2Int newFrameSize, ref Vector2Int newFramePos)
        {
            Layout = new FuLayout();
            // invoke pre draw event
            OnPreDraw?.Invoke(this);

            // set tab color of hovered window
            if (IsHovered)
            {
                Fugui.Push(ImGuiCol.TabSelected, Fugui.Themes.GetColor(FuColors.HoveredWindowTab));
                Fugui.Push(ImGuiCol.TabDimmedSelected, Fugui.Themes.GetColor(FuColors.HoveredWindowTab));
            }
            else
            {
                Fugui.Push(ImGuiCol.TabSelected, Fugui.Themes.GetColor(FuColors.TabSelected));
                Fugui.Push(ImGuiCol.TabDimmedSelected, Fugui.Themes.GetColor(FuColors.TabDimmedSelected));
            }
            Fugui.Push(ImGuiStyleVar.FramePadding, new Vector2(6f, 4f));

            // get last frame Hovered state
            bool _lastFrameHovered = IsHovered;
            // try to start drawing window, according to the closable flag state
            bool nativeWantDrawWindow;
            if (IsExternal)
            {
                nativeWantDrawWindow = ImGui.Begin(ID, _windowFlags | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove);
            }
            else if (IsClosable)
            {
                nativeWantDrawWindow = ImGui.Begin(ID, ref _open, _windowFlags);
            }
            else
            {
                nativeWantDrawWindow = ImGui.Begin(ID, _windowFlags);
            }
            // draw the window body
            if (nativeWantDrawWindow)
            {
                bool docked = ImGuiNative.igIsWindowDocked() != 0;
                // prevent floating node to fake docked state
                //if (docked)
                //{
                //    var dockId = ImGuiNative.igGetWindowDockID();
                //    ImGuiDockNode* dockNode = NativeDocking.igDockBuilderGetNode(dockId);
                //    // check if this is a floating node
                //    bool isFloatingNode = NativeDocking.ImGuiDockNode_IsFloatingNode(dockNode);
                //    docked = !isFloatingNode;
                //}
                if (docked != IsDocked)
                {
                    IsDocked = docked;
                    if (IsDocked)
                    {
                        Fire_OnDock();
                        Fugui.ForceDrawAllWindows();
                    }
                    else
                    {
                        Fugui.ForceDrawAllWindows();
                    }
                    Fire_OnResized();
                }

                // if docked, get size according to avail w and h
                if (IsImguiDocked || IsDocked)
                {
                    CurrentDockID = ImGuiNative.igGetWindowDockID();
                    if (IsDocked)
                    {
                        // get size of this window
                        ImGuiDockNodePtr node = ImGuiDocking.DockBuilderGetNode(CurrentDockID);
                        ImRect rect = node.Rect();
                        var size = rect.Max - rect.Min;
                        newFrameSize = new Vector2Int((int)size.x, (int)size.y);
                    }
                }
                else
                {
                    CurrentDockID = 0;
                }

                // Draw UI container. this is needed to store drawList even if window is not render
                if (IsImguiDocked)
                {
                    Fugui.Push(ImGuiStyleVar.ChildRounding, 0f);
                    Fugui.Push(ImGuiCol.ChildBg, ImGui.GetStyle().Colors[(int)ImGuiCol.WindowBg]); // it's computed by byte, not float, so minimum is 1 / 255 ~= 0.0039216f
                    if (ImGui.BeginChild(ID + "ctnr", Vector2.zero, ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                    {
                        TryDrawUI();
                    }
                    ImGuiNative.igEndChild();
                    Fugui.PopColor();
                    Fugui.PopStyle();
                }
                // Do draw UI if needed
                else
                {
                    TryDrawUI();
                }

                // if not docked, get size according to window size api
                if (!IsDocked)
                {
                    // get size of this window
                    var size = ImGui.GetWindowSize();
                    newFrameSize = new Vector2Int((int)size.x, (int)size.y);
                }
                // get pos of this window
                var pos = ImGui.GetWindowPos();
                newFramePos = new Vector2Int((int)pos.x, (int)pos.y);
                HasFocus = ImGuiNative.igIsWindowFocused(ImGuiFocusedFlags.None) != 0;
                // whatever window is hovered
                processHoverState();

                // draw debug data
                DrawDebugPanel();
                ImGui.End();
                Fugui.PopStyle();
                IsVisible = true;
            }
            else
            {
                ImGui.End();
                Fugui.PopStyle();
                IsVisible = false;
                IsHovered = false;
            }

            // hovered state just changed, handle fire onHover events
            if (_lastFrameHovered != IsHovered)
            {
                if (IsHovered)
                {
                    OnHoverIn?.Invoke(this);
                }
                else
                {
                    OnHoverOut?.Invoke(this);
                }
            }

            // restore tab color of hovered window
            Fugui.PopColor(2);

            // handle visible state (whatever ImGui draw the window this frame)
            if (_lastFrameVisible != IsVisible)
            {
                Fugui.ForceDrawAllWindows();
            }
            _lastFrameVisible = IsVisible;
            // invoke post draw event
            OnPostDraw?.Invoke(this);
            if (_forceRedraw > 0)
            {
                _forceRedraw--;
            }
            // if we need to send that we are ready this frame, let's invoke event
            if (_sendReadyNextFrame)
            {
                _sendReadyNextFrame = false;
                OnInitialized?.Invoke(this);
            }
            Layout.Dispose();
        }

        /// <summary>
        /// Process the hover state of this window.
        /// </summary>
        private void processHoverState()
        {
            IsHovered = (LocalRect.Contains(Container.LocalMousePos) &&
                                ImGuiNative.igIsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows | ImGuiHoveredFlags.AllowWhenBlockedByActiveItem | ImGuiHoveredFlags.AllowWhenBlockedByPopup) != 0) ||
                                Fugui.WindowHasPopupOpen(this);

            // increase window hover state according to input focused window IF we are not dragging any payload
            if (_inputFocusedWindow != null && !Fugui.CurrentContext._isDraggingPayload)
            {
                if (_inputFocusedWindow != this)
                {
                    IsHovered = false;
                }
                else
                {
                    IsHovered = true;
                }
            }

            // prevent input if imgui is using mouse to resize docking area
            if (IsHovered)
            {
                // TODO : Wrapp imguiInternal.h so we can check if imgui want to resize something and prevent input on resising window
                // because we have no acces to ImguiInternal.h for now (not wrapper by cimgui and Imgui.NET), we are pushed to do this ugly trick
                // so for now, all we can do is to check if the current mouse cursor is a resizing one, if it is, prevent input for this window
                ImGuiMouseCursor currentMouseCursor = ImGui.GetMouseCursor();
                switch (currentMouseCursor)
                {
                    case ImGuiMouseCursor.ResizeAll:
                    case ImGuiMouseCursor.ResizeNS:
                    case ImGuiMouseCursor.ResizeEW:
                    case ImGuiMouseCursor.ResizeNESW:
                    case ImGuiMouseCursor.ResizeNWSE:
                        IsHovered = false;
                        break;
                }
            }
        }

        /// <summary>
        /// draw UI if needed
        /// </summary>
        private void TryDrawUI()
        {
            // save working area size and position
            _workingAreaSize = new Vector2Int((int)ImGui.GetContentRegionAvail().x, (int)(ImGui.GetContentRegionAvail().y - HeaderHeight - FooterHeight));
            _workingAreaPosition = new Vector2Int((int)ImGui.GetCursorScreenPos().x, (int)(ImGui.GetCursorScreenPos().y + HeaderHeight)) - _localPosition;

            if (MustBeDraw())
            {
                Fugui.HasRenderWindowThisFrame = true;
                CurrentDrawingWindow = this;
                // draw topBar if needed
                if (HeaderHeight > 0f && HeaderUI != null)
                {
                    Vector2 screenCursorPos = ImGui.GetCursorScreenPos();
                    HeaderUI.Invoke(this, new Vector2(_workingAreaSize.x, HeaderHeight));
                    ImGui.SetCursorScreenPos(screenCursorPos + new Vector2(0f, HeaderHeight));
                }

                // count nb push at render begin
                _nbColorPushOnFrameStart = Fugui.NbPushColor;
                _nbStylePushOnFrameStart = Fugui.NbPushStyle;
                _nbFontPushOnFrameStart = Fugui.NbPushFont;

                // draw user UI callback
                FuStyle.Default.Push(true);

                if (Layout.Button("Externalize##" + ID))
                {
                    Fugui.ExternalizeWindow(this);
                }

                UI?.Invoke(this, Layout);
                FuStyle.Content.Pop();

                // invoke body draw event 
                OnBodyDraw?.Invoke(this);

                // draw bottomBar if needed
                if (FooterHeight > 0f && FooterUI != null)
                {
                    Vector2 footerPos = new Vector2(_localPosition.x + _workingAreaPosition.x, _localPosition.y + _workingAreaPosition.y + _workingAreaSize.y - FooterHeight + HeaderHeight);
                    ImGui.SetCursorScreenPos(footerPos);
                    FooterUI.Invoke(this, new Vector2(_workingAreaSize.x, FooterHeight));
                }

                // pop missing push
                int nbMissingColor = Fugui.NbPushColor - _nbColorPushOnFrameStart;
                if (nbMissingColor > 0)
                {
                    Fugui.PopColor(nbMissingColor);
                }
                int nbMissingStyle = Fugui.NbPushStyle - _nbStylePushOnFrameStart;
                if (nbMissingStyle > 0)
                {
                    Fugui.PopStyle(nbMissingStyle);
                }
                int nbMissingFont = Fugui.NbPushFont - _nbFontPushOnFrameStart;
                if (nbMissingFont > 0)
                {
                    Fugui.PopFont(nbMissingFont);
                }

                // save whatever ImGui want capture Keyboard
                WantCaptureKeyboard = ImGui.GetIO().WantTextInput;
                // draw overlays
                DrawOverlays();

                // update FPS and deltaTime
                DeltaTime = Fugui.Time - _lastRenderTime;
                CurrentFPS = 1f / DeltaTime;
                _lastRenderTime = Fugui.Time;
                HasJustBeenDraw = true;
                CurrentDrawingWindow = null;
            }
        }

        /// <summary>
        /// Draw the windows Debug Panel
        /// </summary>
        internal virtual void DrawDebugPanel()
        {
            if (!Fugui.Settings.DrawDebugPanel)
            {
                return;
            }

            ImGui.SetCursorPos(new Vector2(0f, 32f));
            ImGui.Dummy(Vector2.one);
            Fugui.Push(ImGuiStyleVar.ChildRounding, 4f);
            Fugui.Push(ImGuiCol.ChildBg, new Vector4(.1f, .1f, .1f, 1f));
            if (ImGui.BeginChild(ID + "d", new Vector2(196f, 202f)))
            {
                // states
                ImGui.Text("State : " + State);
                if (!this.HasJustBeenDraw && State == FuWindowState.Idle && float.IsInfinity(_targetDeltaTimeMs))
                {
                    CurrentFPS = 0;
                    DeltaTime = 0f;
                }
                ImGui.Text("FPS : " + Mathf.RoundToInt(CurrentFPS) + " (" + (DeltaTime * 1000f).ToString("f2") + " ms)");

                string target = "infinity";
                if (!float.IsInfinity(_targetDeltaTimeMs))
                {
                    target = ((int)(_targetDeltaTimeMs * 1000)).ToString() + " ms";
                }
                ImGui.Text("Target : " + TargetFPS + "  (" + target + ")");
                ImGui.Dummy(new Vector2(4f, 0f));

                // pos and size
                ImGui.Text("l Mouse : " + Mouse.Position);
                ImGui.Text("c Mouse : " + Container.LocalMousePos);
                ImGui.Text("c Pos : " + Container.Position);
                ImGui.Text("w Pos : " + WorldPosition);
                ImGui.Text("l Pos : " + LocalPosition);
                ImGui.Text("size : " + Size);
                ImGui.Text("wa size : " + WorkingAreaSize);
                ImGui.Text("wa pos: " + WorkingAreaPosition);
                ImGui.Text("wa mouse : " + WorkingAreaMousePosition);
                ImGui.Text("hovered : " + IsHovered);
                ImGui.Text("dl child : " + ChildrenDrawLists.Count);
            }
            ImGuiNative.igEndChild();
            Fugui.PopColor();
            Fugui.PopStyle();

            var dl = ImGui.GetForegroundDrawList();
            // draw working area rect
            var pos = WorkingAreaPosition + LocalPosition;
            dl.AddRect(pos, pos + WorkingAreaSize, ImGui.ColorConvertFloat4ToU32(Color.red), 1f, ImDrawFlags.None, 2f);

            // draw mouse rect
            pos = WorkingAreaMousePosition + WorkingAreaPosition + LocalPosition;
            dl.AddCircleFilled(pos, 2, ImGui.ColorConvertFloat4ToU32(Color.red));
        }

        /// <summary>
        /// force this window to be draw next render tick
        /// </summary>
        public void ForceDraw(int nbFrames = 1)
        {
            _forceRedraw += nbFrames;
            if (_forceRedraw > 10)
            {
                _forceRedraw = 10;
            }
        }

        /// <summary>
        /// whatever the window must be draw right now
        /// </summary>
        /// <returns>true if we must draw this UI this frame</returns>
        public bool MustBeDraw()
        {
            return (Fugui.Time > _lastRenderTime + _targetDeltaTimeMs && ((!Fugui.HasRenderWindowThisFrame && WindowName.IdleFPS != -1) || Fugui.HasRenderWindowThisFrame))
                || _forceRedraw > 0
                || (IsInterractable && (IsHovered || WantCaptureKeyboard || State == FuWindowState.Manipulating));
        }
        #endregion

        #region Events
        /// <summary>
        /// Fire event that we are resizing (every frames)
        /// </summary>
        public void Fire_OnResize()
        {
            IsResizing = true;
            if (!_ignoreResizeForThisFrame)
            {
                OnResize?.Invoke(this);
            }
        }

        /// <summary>
        /// Fire event that we just finish resizing (last frames)
        /// </summary>
        public void Fire_OnResized()
        {
            if (!_ignoreResizeForThisFrame)
            {
                OnResized?.Invoke(this);
                Fugui.Fire_OnWindowResized(this);
            }
        }

        /// <summary>
        ///  Fire event that we are dragging (every frames)
        /// </summary>
        public void Fire_OnDrag()
        {
            IsDragging = true;
            OnDrag?.Invoke(this);
        }

        /// <summary>
        /// Fire event that we just Dock this window
        /// </summary>
        public void Fire_OnDock()
        {
            OnDock?.Invoke(this);
            OnResize?.Invoke(this);
            OnDrag?.Invoke(this);
            Fugui.Fire_OnWindowDocked(this);
        }

        /// <summary>
        /// Fire event that we just UnDock this window
        /// </summary>
        public void Fire_OnUnDock()
        {
            OnUnDock?.Invoke(this);
            OnResize?.Invoke(this);
            OnDrag?.Invoke(this);
            Fugui.Fire_OnWindowUnDocked(this);
        }
        #endregion

        #region Container
        /// <summary>
        /// Try to add this window to a container
        /// </summary>
        /// <param name="container">container to add on</param>
        /// <returns>true if success</returns>
        public bool TryAddToContainer(IFuWindowContainer container)
        {
            IsBusy = true;
            ForceDraw(10);
            return container.TryAddWindow(this);
        }

        /// <summary>
        /// try to remove this window from it current container
        /// </summary>
        /// <returns>true if success</returns>
        public bool TryRemoveFromContainer()
        {
            IsBusy = true;
            ForceDraw(10);
            return Container?.TryRemoveWindow(ID) ?? false;
        }
        #endregion

        #region Overlays
        /// <summary>
        /// Adds the specified UI overlay to the list of overlays.
        /// </summary>
        /// <param name="overlay">The UI overlay to add.</param>
        /// <returns>True if the overlay was added successfully, false if the overlay already exists in the list.</returns>
        internal bool AddOverlay(FuOverlay overlay)
        {
            // Check if the overlay already exists in the list
            if (Overlays.ContainsKey(overlay.ID))
            {
                // Return false if the overlay already exists
                return false;
            }

            // Add the overlay to the list
            Overlays.Add(overlay.ID, overlay);
            // Return true to indicate that the overlay was added successfully
            return true;
        }

        /// <summary>
        /// Removes the UI overlay with the specified ID from the list of overlays.
        /// </summary>
        /// <param name="overlayID">The ID of the UI overlay to remove.</param>
        /// <returns>True if the overlay was removed successfully, false if the overlay was not found in the list.</returns>
        internal bool RemoveOverlay(string overlayID)
        {
            // Remove the overlay with the specified ID from the list and return the result
            return Overlays.Remove(overlayID);
        }

        /// <summary>
        /// Draws all the UI overlays in the list.
        /// </summary>
        private void DrawOverlays()
        {
            // Iterate through all the overlays in the list
            foreach (FuOverlay overlay in Overlays.Values)
            {
                // Draw the current overlay
                overlay.Draw();
            }
        }
        #endregion

        #region Public Utils
        /// <summary>
        /// Try to dock the window in the right DockSpace in the current DockingLayout
        /// </summary>
        /// <returns>whatever the window has been docked</returns>
        public bool AutoDock()
        {
            return Fugui.Layouts.AutoDockWindow(this);
        }

        /// <summary>
        /// Remove this window from it container and from Manager windows list
        /// </summary>
        public void Close(Action callback)
        {
            // on removeFromContainer delegate
            void onremovedFromContainerDelegate(FuWindow window)
            {
                window.OnRemovedFromContainer -= onremovedFromContainerDelegate;
                Fugui.TryRemoveUIWindow(this);
                OnClosed?.Invoke(this);
                Fugui.Fire_OnWindowClosed(this);
                callback?.Invoke();
            }

            OnRemovedFromContainer += onremovedFromContainerDelegate;

            if (!TryRemoveFromContainer())
            {
                Fugui.TryRemoveUIWindow(this);
                OnClosed?.Invoke(this);
                Fugui.Fire_OnWindowClosed(this);
                callback?.Invoke();
            }
        }

        /// <summary>
        /// Force this window to go foreward (focus) on next draw frame
        /// </summary>
        public void ForceFocusOnNextFrame()
        {
            _forceFocusNextFrame = true;
        }

        /// <summary>
        /// Compute current window state and set target FPS accordingly
        /// Use Dragging, Resizing and WantCaptureInput states, recompute dragging and resizing
        /// Must be called by container every frame, event if this window will not be redraw
        /// </summary>
        public void UpdateState(bool leftMouseButtonState)
        {
            // determine if we are dragging or resizing
            if (IsDragging && !leftMouseButtonState)
            {
                IsDragging = false;
            }
            if (IsResizing && !leftMouseButtonState)
            {
                IsResizing = false;
                Fire_OnResized();
            }

            // check for manipulating
            if (IsDragging || IsResizing || IsHovered)
            {
                if (State != FuWindowState.Manipulating)
                {
                    SetPerformanceState(FuWindowState.Manipulating);
                }
            }
            else
            {
                if (State != FuWindowState.Idle)
                {
                    SetPerformanceState(FuWindowState.Idle);
                }
            }
        }

        /// <summary>
        /// Add a window flag to this windows
        /// </summary>
        /// <param name="flag">flag to add</param>
        internal void AddWindowFlag(ImGuiWindowFlags flag)
        {
            _windowFlags |= flag;
        }

        /// <summary>
        /// Remove a window flag from this window
        /// </summary>
        /// <param name="flag">flag to remove</param>
        internal void RemoveWindowFlag(ImGuiWindowFlags flag)
        {
            _windowFlags &= ~flag;
        }

        /// <summary>
        /// set current internal state
        /// will refresh target FPS and force draw on next frame
        /// </summary>
        /// <param name="state">state to set</param>
        internal virtual void SetPerformanceState(FuWindowState state)
        {
            State = state;
            ForceDraw();
            switch (State)
            {
                default:
                case FuWindowState.Idle:
                    TargetFPS = WindowName.IdleFPS == -1 ? Fugui.Settings.IdleFPS : WindowName.IdleFPS;
                    break;

                case FuWindowState.Manipulating:
                    TargetFPS = Fugui.Settings.ManipulatingFPS;
                    break;
            }
        }
        #endregion
    }
}