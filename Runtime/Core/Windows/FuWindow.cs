using Fu.Framework;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Class that represent an ImGui Window
    /// ImGuiWindow is used for externalisable and/or dockable window
    /// </summary>
    public class FuWindow
    {
        #region State
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
        public uint CurrentDockID { get; internal set; }
        public FuWindowState State { get; private set; }
        public DrawList DrawList { get; private set; }
        public Dictionary<string, DrawList> ChildrenDrawLists { get; private set; }
        public Mesh RenderMesh { get { return _renderMeshData != null ? _renderMeshData.Mesh : null; } }
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
        public bool CloseOnMiddleClick { get; private set; }
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
        public bool IsHoveredContent { get { return IsHovered && !BlocksWindowInputs && !Mouse.IsHoverOverlay && !Mouse.IsHoverPopup && !Mouse.IsHoverTopBar; } }
        public bool InputsLocked { get { return BlocksWindowInputs; } }
        public bool IsDocked { get; internal set; }
        public bool IsBusy { get; internal set; }
        public bool IsInterractable
        {
            get { return _isInterractable; }
            set
            {
                if (_isInterractable == value)
                {
                    return;
                }

                _isInterractable = value;
                if (!_isInterractable)
                {
                    ReleaseInputFocus();
                }

                UpdateInputLockKeyboardState();

                ForceDraw();
            }
        }
        public bool IsInterractible
        {
            get { return IsInterractable; }
            set { IsInterractable = value; }
        }
        public bool IsVisible { get; private set; }
        public bool DrawingForced { get { return _forceRedraw > 0; } }
        public bool Is3DWindow { get; internal set; }

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
        private bool _isInterractable = true;
        private static int _windowIndex = 0;
        private bool _ignoreTransformThisFrame = false;
        private Vector2Int _customDragStartMousePos;
        private Vector2Int _customDragStartWindowPos;
        private Vector2Int _customResizeStartMousePos;
        private Vector2Int _customResizeStartWindowPos;
        private Vector2Int _customResizeStartWindowSize;
        private bool _customDragging;
        private bool _customResizeLocksWindowInputs;
        private FuWindowResizeEdge _customResizeHoveredEdge = FuWindowResizeEdge.None;
        private FuWindowResizeEdge _customResizeEdge = FuWindowResizeEdge.None;
        private readonly HashSet<string> _inputLockIDs = new HashSet<string>();
        private bool _inputLockedForThisFrame;
        private readonly List<DrawList> _cachedDrawLists = new List<DrawList>();
        private DrawListMesh _renderMeshData;
        private Vector2Int _renderMeshLocalPosition;
        private bool _debugPanelExpanded;
        // var to count how many push are at frame start, so we can pop missing push
        private static int _nbColorPushOnFrameStart = 0;
        private static int _nbStylePushOnFrameStart = 0;
        private static int _nbFontPushOnFrameStart = 0;
        internal bool _releaseFocusNextFrame = false;

        // static fields

        public static FuWindow CurrentDrawingWindow { get; private set; }

        internal bool BlocksWindowInputs { get { return _inputLockedForThisFrame || _inputLockIDs.Count > 0; } }
        internal IReadOnlyList<DrawList> CachedDrawLists { get { return _cachedDrawLists; } }
        internal DrawListMesh RenderMeshData { get { return _renderMeshData; } }
        protected bool DebugPanelExpanded { get { return _debugPanelExpanded; } }
        internal Vector2 RenderMeshOffset
        {
            get
            {
                return new Vector2(
                    LocalPosition.x - _renderMeshLocalPosition.x,
                    LocalPosition.y - _renderMeshLocalPosition.y);
            }
        }
        private bool HasCachedRenderMesh
        {
            get
            {
                return _renderMeshData != null &&
                       _renderMeshData.Mesh != null &&
                       _renderMeshData.SubMeshCount > 0 &&
                       _cachedDrawLists.Count > 0;
            }
        }

        private enum FuWindowResizeEdge
        {
            None,
            Left,
            Right,
            Bottom,
            BottomLeft,
            BottomRight
        }

        private const float DockDragLargeWindowThresholdRatio = 0.85f;
        private const float DockDragMaxSizeRatio = 0.85f;
        private const string DefaultInputLockID = "User";
        protected const float DebugPanelMargin = 8f;
        protected const float DebugPanelWidth = 286f;
        protected const float DebugPanelCollapsedHeight = 116f;
        protected const float DebugPanelHeight = 342f;

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

        #region Constructors
        /// <summary>
        /// Instantiate a new UIWindow object
        /// </summary>
        /// <param name="windowDefinition">UI Window Definition</param>
        public FuWindow(FuWindowDefinition windowDefinition)
        {
            WindowName = windowDefinition.WindowName;
            ID = WindowName.Name + "##" + _windowIndex;
            _windowIndex++;
            Is3DWindow = false;

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
            CloseOnMiddleClick = windowDefinition.CloseOnMiddleClick;
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
        #endregion

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
            ClearDrawDataCache();
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

        /// <summary>
        /// Release any input state currently owned by this window.
        /// </summary>
        private void ReleaseInputFocus()
        {
            if (InputFocusedWindow == this)
            {
                NbInputFocusedWindow = 0;
                InputFocusedWindow = null;
            }

            _releaseFocusNextFrame = false;
            WantCaptureKeyboard = false;
            IsHovered = false;
            IsDragging = false;
            IsResizing = false;
            _customResizeHoveredEdge = FuWindowResizeEdge.None;
            _customResizeLocksWindowInputs = false;
            _inputLockedForThisFrame = false;
        }

        /// <summary>
        /// Lock all input on this window until the same lock ID is released.
        /// </summary>
        /// <param name="lockID">Optional lock owner ID. Use one ID per system that owns a lock.</param>
        public void LockInputs(string lockID = null)
        {
            string id = GetInputLockID(lockID);
            if (!_inputLockIDs.Add(id))
            {
                return;
            }

            ReleaseInputFocus();
            UpdateInputLockKeyboardState();
            ForceDraw();
        }

        /// <summary>
        /// Unlock this window input for the given lock ID.
        /// </summary>
        /// <param name="lockID">Optional lock owner ID. Must match the ID passed to LockInputs.</param>
        public void UnlockInputs(string lockID = null)
        {
            string id = GetInputLockID(lockID);
            if (!_inputLockIDs.Remove(id))
            {
                return;
            }

            UpdateInputLockKeyboardState();
            ForceDraw();
        }

        /// <summary>
        /// Set the input lock state for the given lock ID.
        /// </summary>
        /// <param name="locked">True to lock input, false to release it.</param>
        /// <param name="lockID">Optional lock owner ID.</param>
        public void SetInputsLocked(bool locked, string lockID = null)
        {
            if (locked)
            {
                LockInputs(lockID);
            }
            else
            {
                UnlockInputs(lockID);
            }
        }

        /// <summary>
        /// Return whether this window has the given persistent input lock.
        /// </summary>
        /// <param name="lockID">Optional lock owner ID.</param>
        public bool HasInputLock(string lockID = null)
        {
            return _inputLockIDs.Contains(GetInputLockID(lockID));
        }

        /// <summary>
        /// Clear all persistent input locks owned by user/API code.
        /// </summary>
        public void ClearInputLocks()
        {
            if (_inputLockIDs.Count == 0)
            {
                return;
            }

            _inputLockIDs.Clear();
            UpdateInputLockKeyboardState();
            ForceDraw();
        }

        /// <summary>
        /// Return true when at least one persistent input lock is active.
        /// </summary>
        private bool HasPersistentInputLocks()
        {
            return _inputLockIDs.Count > 0;
        }

        /// <summary>
        /// Normalize an input lock ID for API callers.
        /// </summary>
        private static string GetInputLockID(string lockID)
        {
            return string.IsNullOrEmpty(lockID) ? DefaultInputLockID : lockID;
        }

        /// <summary>
        /// Keep 3D window keyboard capture aligned with the public input lock state.
        /// </summary>
        private void UpdateInputLockKeyboardState()
        {
            if (Is3DWindow && Container?.Context != null)
            {
                Container.Context.AutoUpdateKeyboard = IsInterractable && !InputsLocked;
            }
        }

        /// <summary>
        /// Draw the UI of this window
        /// </summary>
        public virtual void DrawWindow(bool preventUpdatingMouse = false, bool preventUpdatingKeyboard = false)
        {
            if (!IsInitialized)
            {
                return;
            }

            if (Fugui.Layouts != null && !Fugui.Layouts.ShouldDrawWindow(this))
            {
                SkipDrawForCustomDocking();
                return;
            }

            CheckAutoExternalize();
            CheckAutoInternalize();

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

            // Fugui owns move/resize/docking now, so ImGui only hosts a decorationless drawing surface.
            if (Container.ForcePos() || IsDocked || _forceLocationNextFrame)
            {
                ImGui.SetNextWindowPos(LocalPosition, ImGuiCond.Always);
                _forceLocationNextFrame = false;
            }
            if (Container.ForcePos() || IsDocked || _forceSizeNextFrame)
            {
                ImGui.SetNextWindowSize(Size, ImGuiCond.Always);
                _forceSizeNextFrame = false;
            }
            if (_forceFocusNextFrame)
            {
                ImGui.SetNextWindowFocus();
            }
            DrawWindowBody(preventUpdatingMouse, preventUpdatingKeyboard, ref newFrameSize, ref newFramePos);

            // ImGui want to close this window
            if (!IsOpened)
            {
#if FU_EXTERNALIZATION
                if (IsExternal)
                    ((FuExternalWindowContainer)Container).Close(() => { Close(); });
                else
#endif
                Close();
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
        public virtual unsafe void DrawWindowBody(bool preventUpdatingMouse, bool preventUpdatingKeyboard, ref Vector2Int newFrameSize, ref Vector2Int newFramePos)
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
            int windowStylePushCount = 1;
            if (Is3DWindow)
            {
                Fugui.Push(ImGuiStyleVar.WindowMinSize, Vector2.one);
                windowStylePushCount++;
            }
            Fugui.Push(ImGuiStyleVar.WindowBorderSize, 0f);
            windowStylePushCount++;

            // get last frame Hovered state
            bool _lastFrameHovered = IsHovered;
            // try to start drawing window, according to the closable flag state
            bool nativeWantDrawWindow;
            bool externalBefore = IsExternal;
            ImGuiWindowFlags effectiveWindowFlags = _windowFlags;
            UpdateCustomResizeInputBlock();
            if (!IsInterractable)
            {
                effectiveWindowFlags |= ImGuiWindowFlags.NoInputs;
            }
            if (BlocksWindowInputs)
            {
                effectiveWindowFlags |= ImGuiWindowFlags.NoInputs;
            }
            effectiveWindowFlags |= ImGuiWindowFlags.NoTitleBar |
                                    ImGuiWindowFlags.NoResize |
                                    ImGuiWindowFlags.NoMove |
                                    ImGuiWindowFlags.NoCollapse |
                                    ImGuiWindowFlags.NoDocking |
                                    ImGuiWindowFlags.NoSavedSettings;
            if (IsDocked)
            {
                effectiveWindowFlags |= ImGuiWindowFlags.NoBringToFrontOnFocus;
            }
            bool useWindowBackdrop = !externalBefore &&
                (effectiveWindowFlags & ImGuiWindowFlags.NoBackground) == 0 &&
                Fugui.ShouldUseThemeBackdrop(FuColors.WindowBg);
            if (useWindowBackdrop)
            {
                effectiveWindowFlags |= ImGuiWindowFlags.NoBackground;
            }

            if (externalBefore)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
                nativeWantDrawWindow = ImGui.Begin(ID, effectiveWindowFlags);
            }
            else if (IsClosable)
            {
                nativeWantDrawWindow = ImGui.Begin(ID, ref _open, effectiveWindowFlags);
            }
            else
            {
                nativeWantDrawWindow = ImGui.Begin(ID, effectiveWindowFlags);
            }

            // whatever window is hovered
            processHoverState();

            // update mouse buttons states
            if (!preventUpdatingMouse)
                Mouse.UpdateState(this);
            // update keyboard state
            if (!preventUpdatingKeyboard)
                Keyboard.UpdateState();
            ProcessCustomWindowManipulation(ref newFrameSize, ref newFramePos);
            FocusCurrentImGuiWindowIfRequested();

            // draw the window body
            if (nativeWantDrawWindow)
            {
                if (useWindowBackdrop)
                {
                    float backdropRounding = IsDocked || IsExternal ? 0f : Fugui.Themes.WindowRounding;
                    Fugui.DrawCurrentWindowThemeBackdrop(FuColors.WindowBg, 1f, backdropRounding);
                }

                TryDrawUI();

                if (Is3DWindow)
                {
                    newFrameSize = Size;
                }
                else
                {
                    var size = ImGui.GetWindowSize();
                    newFrameSize = new Vector2Int((int)size.x, (int)size.y);
                }
                // get pos of this window
                var pos = ImGui.GetWindowPos();
                newFramePos = new Vector2Int((int)pos.x, (int)pos.y);
                HasFocus = ImGuiNative.igIsWindowFocused(ImGuiFocusedFlags.None) != 0;

                // draw debug data
                DrawDebugPanel();
                ImGui.End();
                Fugui.PopStyle(windowStylePushCount);
                IsVisible = true;
            }
            else
            {
                ImGui.End();
                Fugui.PopStyle(windowStylePushCount);
                IsVisible = false;
                IsHovered = false;
            }
            if (externalBefore)
            {
                ImGui.PopStyleVar();
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
            if (!IsInterractable)
            {
                IsHovered = false;
                return;
            }

            if (Is3DWindow)
            {
                IsHovered = LocalRect.Contains(Container.LocalMousePos);
            }
            else
            {
                IsHovered = (LocalRect.Contains(Container.LocalMousePos) &&
                            ImGuiNative.igIsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows | ImGuiHoveredFlags.AllowWhenBlockedByActiveItem | ImGuiHoveredFlags.AllowWhenBlockedByPopup) != 0) ||
                            Fugui.WindowHasPopupOpen(this);
            }
            if (_customResizeLocksWindowInputs && LocalRect.Contains(Container.LocalMousePos))
            {
                IsHovered = true;
            }

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

            // if a 3D window is already hovered this frame, block non-3D windows only
            if (IsHovered && Fugui.HasHovered3DWindowThisFrame && !Is3DWindow)
            {
                IsHovered = false;
            }

            // register 3D hover lock only after all filters
            if (IsHovered && Is3DWindow)
            {
                Fugui.HasHovered3DWindowThisFrame = true;
            }
        }

        /// <summary>
        /// draw UI if needed
        /// </summary>
        private void TryDrawUI()
        {
            float customTopHeight = GetCustomTopChromeHeight();
            bool customTopBarIsDockTabs = IsDocked && Fugui.Layouts != null && Fugui.Layouts.HasDockedTabBar(this);
            Vector2 baseCursorPos = ImGui.GetCursorScreenPos();
            Vector2 contentRegionAvail = ImGui.GetContentRegionAvail();
            float bottomChromeReserve = GetCustomBottomChromeReserve();

            // save working area size and position
            _workingAreaSize = new Vector2Int(
                (int)contentRegionAvail.x,
                Mathf.Max(0, (int)(contentRegionAvail.y - customTopHeight - HeaderHeight - FooterHeight - bottomChromeReserve)));
            _workingAreaPosition = new Vector2Int(
                (int)baseCursorPos.x,
                (int)(baseCursorPos.y + customTopHeight + HeaderHeight)) - _localPosition;

            DrawCustomWindowFrame(customTopHeight, customTopBarIsDockTabs);
            DrawCustomTopBarContent(customTopHeight, customTopBarIsDockTabs, baseCursorPos);

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

#if FU_EXTERNALIZATION
                if (IsExternal && Fugui.Settings.DrawDebugPanel)
                {
                    ((FuExternalContext)Container.Context).Window.DrawDebug(Layout);
                }
#endif

                UI?.Invoke(this, Layout);
                FuStyle.Content.Pop();

                // invoke body draw event 
                OnBodyDraw?.Invoke(this);

                // draw bottomBar if needed
                if (FooterHeight > 0f && FooterUI != null)
                {
                    Vector2 footerPos = new Vector2(_localPosition.x + _workingAreaPosition.x, _localPosition.y + _workingAreaPosition.y + _workingAreaSize.y);
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
                WantCaptureKeyboard = IsInterractable && ImGui.GetIO().WantTextInput;
                // draw overlays
                DrawOverlays();
                Fugui.Layouts?.DrawDockSplittersForWindow(this);
                DrawCustomWindowChromeOverlay(ImGui.GetWindowDrawList(), ImGui.GetWindowPos(), ImGui.GetWindowSize());

                // if external, draw resize grips and resize hover feedback

#if FU_EXTERNALIZATION
                if (IsExternal)
                {
                    ((FuExternalContext)Container.Context).Window.DrawResizeHandles();
                    DrawExternalWindowTitleButtons();
                }
#endif

                // update FPS and deltaTime
                DeltaTime = Fugui.Time - _lastRenderTime;
                CurrentFPS = 1f / DeltaTime;
                _lastRenderTime = Fugui.Time;
                HasJustBeenDraw = true;
                CurrentDrawingWindow = null;
            }
            else
            {
                CurrentFPS = 0f;
                HasJustBeenDraw = false;
            }
        }

        /// <summary>
        /// Mark a custom-docked inactive tab as not drawn for this frame.
        /// </summary>
        internal void SkipDrawForCustomDocking()
        {
            if (InputFocusedWindow == this)
            {
                ReleaseInputFocus();
            }

            IsVisible = false;
            IsHovered = false;
            HasJustBeenDraw = false;
            WantCaptureKeyboard = false;
            _customResizeHoveredEdge = FuWindowResizeEdge.None;
            _customResizeLocksWindowInputs = false;
            _inputLockedForThisFrame = false;
            if (_lastFrameVisible)
            {
                Fugui.ForceDrawAllWindows();
            }
            _lastFrameVisible = false;
        }

        /// <summary>
        /// Returns the top chrome height used by the custom title bar or docked tab bar.
        /// </summary>
        private float GetCustomTopChromeHeight()
        {
            if (IsDocked)
            {
                return Fugui.Layouts != null ? Fugui.Layouts.GetDockedTabBarHeight(this) : 0f;
            }

            return ShouldDrawCustomTitleBar() ? GetCustomTitleBarHeight() : 0f;
        }

        /// <summary>
        /// Returns whether this window should draw a Fugui title bar.
        /// </summary>
        private bool ShouldDrawCustomTitleBar()
        {
            return !_windowFlags.HasFlag(ImGuiWindowFlags.NoTitleBar);
        }

        /// <summary>
        /// Returns the standard Fugui title bar height.
        /// </summary>
        private float GetCustomTitleBarHeight()
        {
            float scale = Container?.Context?.Scale ?? Fugui.Scale;
            return Mathf.Max(24f * scale, ImGui.CalcTextSize("Ap").y + 10f * scale);
        }

        /// <summary>
        /// Returns the bottom space reserved for Fugui-owned border chrome.
        /// </summary>
        private float GetCustomBottomChromeReserve()
        {
            return Mathf.Max(1f * Fugui.Scale, Fugui.Themes.WindowBorderSize);
        }

        /// <summary>
        /// Draw the top content owned by the custom chrome.
        /// </summary>
        private void DrawCustomTopBarContent(float customTopHeight, bool customTopBarIsDockTabs, Vector2 baseCursorPos)
        {
            if (customTopHeight <= 0f)
            {
                return;
            }

            ImGui.SetCursorScreenPos(baseCursorPos);
            if (customTopBarIsDockTabs && Fugui.Layouts != null)
            {
                Fugui.Layouts.DrawDockedTabs(this, Layout);
            }
            else
            {
                ImGui.Dummy(new Vector2(Mathf.Max(1f, ImGui.GetContentRegionAvail().x), customTopHeight));
            }

            ImGui.SetCursorScreenPos(new Vector2(baseCursorPos.x, baseCursorPos.y + customTopHeight));
        }

        /// <summary>
        /// Draw the Fugui-owned window frame, title bar and resize feedback.
        /// </summary>
        private void DrawCustomWindowFrame(float customTopHeight, bool customTopBarIsDockTabs)
        {
            ImDrawListPtr dl = ImGui.GetWindowDrawList();
            Vector2 pos = ImGui.GetWindowPos();
            Vector2 size = ImGui.GetWindowSize();
            float rounding = IsDocked || IsExternal ? 0f : Fugui.Themes.WindowRounding;

            if (customTopHeight > 0f && !customTopBarIsDockTabs)
            {
                Vector2 titleMax = pos + new Vector2(size.x, customTopHeight);
                uint titleColor = Fugui.Themes.GetColorU32(HasFocus ? FuColors.TitleBgActive : FuColors.TitleBg);
                uint separatorColor = Fugui.Themes.GetColorU32(HasFocus ? FuColors.SeparatorActive : FuColors.Separator, HasFocus ? 0.9f : 0.55f);
                dl.AddRectFilled(pos, titleMax, titleColor, rounding, IsExternal ? ImDrawFlags.None : ImDrawFlags.RoundCornersTop);
                dl.AddLine(new Vector2(pos.x, titleMax.y - 1f * Fugui.Scale), new Vector2(pos.x + size.x, titleMax.y - 1f * Fugui.Scale), separatorColor, Mathf.Max(1f, 1f * Fugui.Scale));
                if (HasFocus)
                {
                    float accentWidth = Mathf.Max(2f, 3f * Fugui.Scale);
                    dl.AddRectFilled(pos, new Vector2(pos.x + accentWidth, titleMax.y), Fugui.Themes.GetColorU32(FuColors.DockingPreview, 0.85f), rounding, ImDrawFlags.RoundCornersTopLeft);
                }

                string title = Fugui.GetUntagedText(WindowName.Name);
                Vector2 textSize = ImGui.CalcTextSize(title);
                float textX = pos.x + (HasFocus ? 12f : 8f) * Fugui.Scale;
                float textY = pos.y + (customTopHeight - textSize.y) * 0.5f;
                Rect closeRect = GetCustomCloseButtonRect(customTopHeight);
                float textClipRight = IsClosable && !IsExternal ? pos.x + closeRect.xMin - 4f * Fugui.Scale : pos.x + size.x - 8f * Fugui.Scale;

                dl.PushClipRect(new Vector2(textX, pos.y), new Vector2(Mathf.Max(textX, textClipRight), pos.y + customTopHeight), true);
                dl.AddText(new Vector2(textX, textY), Fugui.Themes.GetColorU32(HasFocus ? FuColors.HighlightText : FuColors.Text), title);
                dl.PopClipRect();

                if (IsClosable && !IsExternal)
                {
                    DrawCustomCloseButton(dl, pos, closeRect);
                }
            }
        }

        /// <summary>
        /// Draw border and resize feedback after all window content.
        /// </summary>
        private void DrawCustomWindowChromeOverlay(ImDrawListPtr dl, Vector2 pos, Vector2 size)
        {
            Vector2 max = pos + size;
            float rounding = IsDocked || IsExternal ? 0f : Fugui.Themes.WindowRounding;
            float borderSize = Mathf.Max(1f * Fugui.Scale, Fugui.Themes.WindowBorderSize);
            if (borderSize > 0f)
            {
                float borderInset = borderSize * 0.5f;
                Vector2 borderMin = pos + new Vector2(borderInset, borderInset);
                Vector2 borderMax = max - new Vector2(borderInset, borderInset);
                dl.AddRect(borderMin, borderMax, Fugui.Themes.GetColorU32(FuColors.Border), rounding, ImDrawFlags.None, borderSize);
            }

            DrawCustomResizeFeedback(dl, pos, size);
        }

        /// <summary>
        /// Draw and process the custom close button for non-external windows.
        /// </summary>
        private void DrawCustomCloseButton(ImDrawListPtr dl, Vector2 windowPos, Rect closeRect)
        {
            bool hovered = !BlocksWindowInputs && closeRect.Contains(Mouse.Position);
            bool active = hovered && Mouse.IsPressed(FuMouseButton.Left);
            float scale = Fugui.Scale;
            float buttonSize = Mathf.Min(closeRect.height - 4f * scale, Mathf.Max(18f, 22f * scale));
            Vector2 center = windowPos + closeRect.center;
            Vector2 min = center - new Vector2(buttonSize * 0.5f, buttonSize * 0.5f);
            Vector2 max = center + new Vector2(buttonSize * 0.5f, buttonSize * 0.5f);

            if (hovered)
            {
                uint bg = active
                    ? Fugui.Themes.GetColorU32(FuColors.BackgroundDanger, 0.92f)
                    : Fugui.Themes.GetColorU32(FuColors.BackgroundDanger, 0.72f);
                dl.AddRectFilled(min, max, bg, Mathf.Max(3f, 4f * scale));
            }

            float iconHalf = Mathf.Max(4.5f, 5.5f * scale);
            float thickness = hovered ? Mathf.Max(1.35f, 1.55f * scale) : Mathf.Max(1.1f, 1.25f * scale);
            uint iconColor = hovered
                ? Fugui.Themes.GetColorU32(FuColors.HighlightText)
                : Fugui.Themes.GetColorU32(FuColors.Text, 0.78f);
            dl.AddLine(center - new Vector2(iconHalf, iconHalf), center + new Vector2(iconHalf, iconHalf), iconColor, thickness);
            dl.AddLine(center + new Vector2(-iconHalf, iconHalf), center + new Vector2(iconHalf, -iconHalf), iconColor, thickness);

            if (hovered)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                if (Mouse.IsClicked(FuMouseButton.Left))
                {
                    _open = false;
                }
            }
        }

        /// <summary>
        /// Draw hover/active resize edges for custom floating windows.
        /// </summary>
        private void DrawCustomResizeFeedback(ImDrawListPtr dl, Vector2 windowPos, Vector2 windowSize)
        {
            FuWindowResizeEdge edge = _customResizeEdge != FuWindowResizeEdge.None
                ? _customResizeEdge
                : _customResizeHoveredEdge;

            if (edge == FuWindowResizeEdge.None)
            {
                return;
            }

            ImDrawListPtr drawList = _customResizeLocksWindowInputs ? ImGui.GetForegroundDrawList() : dl;
            bool clippedToWindow = _customResizeLocksWindowInputs;
            if (clippedToWindow)
            {
                drawList.PushClipRect(windowPos, windowPos + windowSize, false);
            }

            bool active = _customResizeEdge != FuWindowResizeEdge.None;
            uint feedbackColor = Fugui.Themes.GetColorU32(active ? FuColors.HighlightActive : FuColors.HighlightHovered, active ? 1f : 0.9f);
            uint edgeLineColor = Fugui.Themes.GetColorU32(FuColors.Border, active ? 0.88f : 0.62f);
            uint handleColor = feedbackColor;
            float handleThickness = active ? Mathf.Max(2f, 2.5f * Fugui.Scale) : Mathf.Max(1.5f, 2f * Fugui.Scale);
            float edgeLineThickness = Mathf.Max(1f, 1f * Fugui.Scale);
            float inset = handleThickness * 0.5f;
            Vector2 min = windowPos + new Vector2(inset, inset);
            Vector2 max = windowPos + windowSize - new Vector2(inset, inset);
            float centerX = Mathf.Clamp(windowPos.x + Mouse.Position.x, min.x, max.x);
            float centerY = Mathf.Clamp(windowPos.y + Mouse.Position.y, min.y, max.y);
            float handleShort = Mathf.Max(5f, 5f * Fugui.Scale);
            float handleLong = Mathf.Max(36f, 42f * Fugui.Scale);
            float verticalHandleLong = Mathf.Min(handleLong, Mathf.Max(handleShort, max.y - min.y));
            float horizontalHandleLong = Mathf.Min(handleLong, Mathf.Max(handleShort, max.x - min.x));
            float rounding = handleShort * 0.5f;

            if (edge == FuWindowResizeEdge.Left || edge == FuWindowResizeEdge.BottomLeft)
            {
                drawList.AddLine(min, new Vector2(min.x, max.y), edgeLineColor, edgeLineThickness);
                if (edge == FuWindowResizeEdge.Left)
                {
                    float clampedY = Mathf.Clamp(centerY, min.y + verticalHandleLong * 0.5f, max.y - verticalHandleLong * 0.5f);
                    Rect handle = new Rect(new Vector2(min.x - handleShort * 0.5f + inset, clampedY - verticalHandleLong * 0.5f), new Vector2(handleShort, verticalHandleLong));
                    drawList.AddRectFilled(handle.position, handle.position + handle.size, handleColor, rounding);
                }
            }
            if (edge == FuWindowResizeEdge.Right || edge == FuWindowResizeEdge.BottomRight)
            {
                drawList.AddLine(new Vector2(max.x, min.y), max, edgeLineColor, edgeLineThickness);
                if (edge == FuWindowResizeEdge.Right)
                {
                    float clampedY = Mathf.Clamp(centerY, min.y + verticalHandleLong * 0.5f, max.y - verticalHandleLong * 0.5f);
                    Rect handle = new Rect(new Vector2(max.x - handleShort * 0.5f - inset, clampedY - verticalHandleLong * 0.5f), new Vector2(handleShort, verticalHandleLong));
                    drawList.AddRectFilled(handle.position, handle.position + handle.size, handleColor, rounding);
                }
            }
            if (edge == FuWindowResizeEdge.Bottom || edge == FuWindowResizeEdge.BottomLeft || edge == FuWindowResizeEdge.BottomRight)
            {
                drawList.AddLine(new Vector2(min.x, max.y), max, edgeLineColor, edgeLineThickness);
                if (edge == FuWindowResizeEdge.Bottom)
                {
                    float clampedX = Mathf.Clamp(centerX, min.x + horizontalHandleLong * 0.5f, max.x - horizontalHandleLong * 0.5f);
                    Rect handle = new Rect(new Vector2(clampedX - horizontalHandleLong * 0.5f, max.y - handleShort * 0.5f - inset), new Vector2(horizontalHandleLong, handleShort));
                    drawList.AddRectFilled(handle.position, handle.position + handle.size, handleColor, rounding);
                }
            }
            if (edge == FuWindowResizeEdge.BottomLeft || edge == FuWindowResizeEdge.BottomRight)
            {
                DrawCornerResizeHandle(drawList, edge, min, max, handleColor, handleThickness);
            }

            if (clippedToWindow)
            {
                drawList.PopClipRect();
            }

            switch (edge)
            {
                case FuWindowResizeEdge.Left:
                case FuWindowResizeEdge.Right:
                    ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEW);
                    break;
                case FuWindowResizeEdge.Bottom:
                    ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNS);
                    break;
                case FuWindowResizeEdge.BottomLeft:
                    ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNESW);
                    break;
                case FuWindowResizeEdge.BottomRight:
                    ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNWSE);
                    break;
            }
        }

        /// <summary>
        /// Draw a compact L-shaped corner resize handle.
        /// </summary>
        private void DrawCornerResizeHandle(ImDrawListPtr dl, FuWindowResizeEdge edge, Vector2 min, Vector2 max, uint color, float thickness)
        {
            float length = Mathf.Max(18f, 22f * Fugui.Scale);
            float bar = Mathf.Max(3.5f, 4f * Fugui.Scale);
            float rounding = bar * 0.5f;
            float inset = Mathf.Max(2f, 2.5f * Fugui.Scale);

            if (edge == FuWindowResizeEdge.BottomLeft)
            {
                Vector2 hMin = new Vector2(min.x + inset, max.y - bar - inset);
                Vector2 hMax = new Vector2(Mathf.Min(min.x + inset + length, max.x), max.y - inset);
                Vector2 vMin = new Vector2(min.x + inset, Mathf.Max(min.y, max.y - inset - length));
                Vector2 vMax = new Vector2(min.x + inset + bar, max.y - inset);
                dl.AddRectFilled(hMin, hMax, color, rounding);
                dl.AddRectFilled(vMin, vMax, color, rounding);
                return;
            }

            if (edge == FuWindowResizeEdge.BottomRight)
            {
                Vector2 hMin = new Vector2(Mathf.Max(min.x, max.x - inset - length), max.y - bar - inset);
                Vector2 hMax = new Vector2(max.x - inset, max.y - inset);
                Vector2 vMin = new Vector2(max.x - inset - bar, Mathf.Max(min.y, max.y - inset - length));
                Vector2 vMax = new Vector2(max.x - inset, max.y - inset);
                dl.AddRectFilled(hMin, hMax, color, rounding);
                dl.AddRectFilled(vMin, vMax, color, rounding);
            }
        }

        /// <summary>
        /// Process custom move and resize operations.
        /// </summary>
        private void ProcessCustomWindowManipulation(ref Vector2Int newFrameSize, ref Vector2Int newFramePos)
        {
            if (!CanCustomMoveWindow() && !CanCustomResizeWindow())
            {
                _customDragging = false;
                _customResizeEdge = FuWindowResizeEdge.None;
                return;
            }

            Vector2Int mousePos = Container.LocalMousePos;
            bool mouseBlockedByPopup = Fugui.IsInsideAnyPopup(mousePos);
            bool leftMouseDown = _customResizeLocksWindowInputs
                ? IsRawMouseDown(FuMouseButton.Left)
                : Mouse.IsDown(FuMouseButton.Left);
            bool leftMousePressed = _customResizeEdge != FuWindowResizeEdge.None || _customResizeLocksWindowInputs
                ? IsRawMousePressed(FuMouseButton.Left)
                : Mouse.IsPressed(FuMouseButton.Left);
            if (ShouldCloseOnMiddleClickFromHeader())
            {
                _open = false;
                return;
            }

            if (!IsDocked && leftMouseDown && !mouseBlockedByPopup)
            {
                BringFloatingWindowToFront();
            }

            if (leftMouseDown && !mouseBlockedByPopup)
            {
                FuWindowResizeEdge edge = _customResizeHoveredEdge != FuWindowResizeEdge.None
                    ? _customResizeHoveredEdge
                    : GetHoveredCustomResizeEdge(Mouse.Position);
                if (edge != FuWindowResizeEdge.None)
                {
                    _customResizeEdge = edge;
                    _customResizeStartMousePos = mousePos;
                    _customResizeStartWindowPos = LocalPosition;
                    _customResizeStartWindowSize = Size;
                    IsResizing = true;
                    _customDragging = false;
                }
                else if (CanCustomMoveWindow() && IsCustomTitleBarHovered(Mouse.Position) && !IsCustomCloseButtonHovered(Mouse.Position))
                {
                    _customDragging = true;
                    _customDragStartMousePos = mousePos;
                    _customDragStartWindowPos = LocalPosition;
                    IsDragging = true;
                    _customResizeEdge = FuWindowResizeEdge.None;
                    BringFloatingWindowToFront();
                }
            }

            if (_customResizeEdge != FuWindowResizeEdge.None)
            {
                if (leftMousePressed)
                {
                    ApplyCustomResize(mousePos, ref newFrameSize, ref newFramePos);
                }
                else
                {
                    _customResizeEdge = FuWindowResizeEdge.None;
                }
            }

            if (_customDragging)
            {
                if (Mouse.IsPressed(FuMouseButton.Left))
                {
                    Vector2Int delta = mousePos - _customDragStartMousePos;
                    Vector2Int newPos = _customDragStartWindowPos + delta;
                    if (LocalPosition != newPos)
                    {
                        LocalPosition = newPos;
                        _lastFramePos = newPos;
                        _ignoreTransformThisFrame = true;
                        HasMovedThisFrame = true;
                        Fire_OnDrag();
                    }
                    newFramePos = LocalPosition;
                    Fugui.Layouts?.UpdateDockDragPreview(this, mousePos);
                }
                else
                {
                    bool docked = Fugui.Layouts?.TryDockDraggedWindow(this, mousePos) ?? false;
                    if (!docked)
                    {
                        EnsureHeaderVisibleInContainer();
                    }
                    _customDragging = false;
                    if (InputFocusedWindow == this)
                    {
                        ReleaseInputFocus();
                    }
                }
            }

            if (!mouseBlockedByPopup && CanCustomMoveWindow() && IsCustomTitleBarHovered(Mouse.Position) && !IsCustomCloseButtonHovered(Mouse.Position))
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
            }
        }

        /// <summary>
        /// Start a Fugui-owned drag operation after a docked tab has been detached.
        /// </summary>
        internal void BeginCustomDockDrag(Vector2Int startMousePos, Vector2Int startWindowPos, Vector2Int currentMousePos)
        {
            Vector2Int originalSize = Size;
            int minSize = Mathf.Max(64, Mathf.RoundToInt(64f * Fugui.Scale));
            Vector2 grabRatio = new Vector2(
                originalSize.x > 0 ? Mathf.Clamp01((startMousePos.x - startWindowPos.x) / (float)originalSize.x) : 0.5f,
                originalSize.y > 0 ? Mathf.Clamp01((startMousePos.y - startWindowPos.y) / (float)originalSize.y) : 0f);
            Vector2Int fittedSize = GetDockDragInitialSize(originalSize, minSize);
            Vector2Int fittedStartPos = new Vector2Int(
                Mathf.RoundToInt(currentMousePos.x - fittedSize.x * grabRatio.x),
                Mathf.RoundToInt(currentMousePos.y - fittedSize.y * grabRatio.y));
            ClampHeaderVisibleInContainer(ref fittedStartPos, fittedSize);
            _customResizeEdge = FuWindowResizeEdge.None;
            _customDragging = true;
            _customDragStartMousePos = currentMousePos;
            _customDragStartWindowPos = fittedStartPos;
            IsDragging = true;
            IsResizing = false;
            HasMovedThisFrame = true;
            ApplyProgrammaticRect(fittedStartPos, fittedSize, true);
            Focus();
            _releaseFocusNextFrame = false;
            InputFocusedWindow = this;
            if (NbInputFocusedWindow <= 0)
            {
                NbInputFocusedWindow = 1;
            }
            ForceDraw(2);
            Fugui.ForceDrawAllWindows(2);
        }

        /// <summary>
        /// Keep the docked size for normal windows, reducing only very large windows when detaching.
        /// </summary>
        private Vector2Int GetDockDragInitialSize(Vector2Int originalSize, int minSize)
        {
            if (Container == null)
            {
                return originalSize;
            }

            return new Vector2Int(
                GetDockDragInitialAxisSize(originalSize.x, Container.Size.x, minSize),
                GetDockDragInitialAxisSize(originalSize.y, Container.Size.y, minSize));
        }

        /// <summary>
        /// Return the original axis size unless it is large enough to become awkward as a floating window.
        /// </summary>
        private static int GetDockDragInitialAxisSize(int originalSize, int containerSize, int minSize)
        {
            int containerLimit = Mathf.Max(1, containerSize);
            int desiredSize = Mathf.Clamp(originalSize, 1, containerLimit);
            int thresholdSize = Mathf.RoundToInt(containerLimit * DockDragLargeWindowThresholdRatio);
            if (desiredSize <= thresholdSize)
            {
                return desiredSize;
            }

            int maxFloatingSize = Mathf.RoundToInt(containerLimit * DockDragMaxSizeRatio);
            return Mathf.Clamp(maxFloatingSize, Mathf.Min(minSize, containerLimit), containerLimit);
        }

        /// <summary>
        /// Bring a floating window above the other Fugui windows immediately when possible.
        /// </summary>
        private void BringFloatingWindowToFront()
        {
            if (IsDocked)
            {
                return;
            }

            if (Container is FuMainWindowContainer mainContainer)
            {
                mainContainer.BringWindowToFront(this);
            }

            ImGui.SetWindowFocus(ID);
            BringImGuiWindowToDisplayFront();
            ForceFocusOnNextFrame();
        }

        /// <summary>
        /// Applies a pending Fugui focus request to the current ImGui window after Begin.
        /// </summary>
        private void FocusCurrentImGuiWindowIfRequested()
        {
            if (!_forceFocusNextFrame)
            {
                return;
            }

            ImGui.SetWindowFocus();
            BringImGuiWindowToDisplayFront();
            _forceFocusNextFrame = false;
        }

        /// <summary>
        /// Synchronize ImGui's native display order with Fugui's floating window order.
        /// </summary>
        private unsafe void BringImGuiWindowToDisplayFront()
        {
            if (IsDocked || string.IsNullOrEmpty(ID))
            {
                return;
            }

            int byteCount = Encoding.UTF8.GetByteCount(ID);
            byte* nativeName = stackalloc byte[byteCount + 1];
            int offset = Fugui.GetUtf8(ID, nativeName, byteCount);
            nativeName[offset] = 0;

            ImGuiWindow* nativeWindow = ImGuiInternal.igFindWindowByName(nativeName);
            if (nativeWindow == null)
            {
                return;
            }

            ImGuiInternal.igFocusWindow(nativeWindow, 0);
            ImGuiInternal.igBringWindowToFocusFront(nativeWindow);
            ImGuiInternal.igBringWindowToDisplayFront(nativeWindow);
        }

        /// <summary>
        /// Apply the active custom resize operation.
        /// </summary>
        private void ApplyCustomResize(Vector2Int mousePos, ref Vector2Int newFrameSize, ref Vector2Int newFramePos)
        {
            Vector2Int delta = mousePos - _customResizeStartMousePos;
            Vector2Int newPos = _customResizeStartWindowPos;
            Vector2Int newSize = _customResizeStartWindowSize;
            int minSize = Mathf.Max(64, Mathf.RoundToInt(64f * Fugui.Scale));

            switch (_customResizeEdge)
            {
                case FuWindowResizeEdge.Left:
                    ApplyLeftResize(delta.x, minSize, ref newPos, ref newSize);
                    break;
                case FuWindowResizeEdge.Right:
                    newSize.x = Mathf.Max(minSize, _customResizeStartWindowSize.x + delta.x);
                    break;
                case FuWindowResizeEdge.Bottom:
                    newSize.y = Mathf.Max(minSize, _customResizeStartWindowSize.y + delta.y);
                    break;
                case FuWindowResizeEdge.BottomLeft:
                    ApplyLeftResize(delta.x, minSize, ref newPos, ref newSize);
                    newSize.y = Mathf.Max(minSize, _customResizeStartWindowSize.y + delta.y);
                    break;
                case FuWindowResizeEdge.BottomRight:
                    newSize.x = Mathf.Max(minSize, _customResizeStartWindowSize.x + delta.x);
                    newSize.y = Mathf.Max(minSize, _customResizeStartWindowSize.y + delta.y);
                    break;
            }

            bool moved = LocalPosition != newPos;
            bool resized = Size != newSize;
            if (moved || resized)
            {
                _ignoreTransformThisFrame = true;
            }

            if (moved)
            {
                LocalPosition = newPos;
                _lastFramePos = newPos;
                HasMovedThisFrame = true;
            }

            if (resized)
            {
                Size = newSize;
                _lastFrameSize = newSize;
            }

            newFramePos = newPos;
            newFrameSize = newSize;
            Fire_OnResize();
            ForceDraw();
        }

        /// <summary>
        /// Apply left-edge resize while preserving the minimum width.
        /// </summary>
        private void ApplyLeftResize(int deltaX, int minSize, ref Vector2Int newPos, ref Vector2Int newSize)
        {
            int maxDelta = _customResizeStartWindowSize.x - minSize;
            int clampedDelta = Mathf.Min(deltaX, maxDelta);
            newPos.x = _customResizeStartWindowPos.x + clampedDelta;
            newSize.x = _customResizeStartWindowSize.x - clampedDelta;
        }

        /// <summary>
        /// Clamp this floating window to be fully visible in its container.
        /// </summary>
        internal void EnsureFullyVisibleInContainer()
        {
            Vector2Int pos = LocalPosition;
            Vector2Int size = Size;
            FitRectInsideContainer(ref pos, ref size);
            ApplyProgrammaticRect(pos, size, true);
        }

        /// <summary>
        /// Move this floating window only as much as needed to keep a grabbable title bar area visible.
        /// </summary>
        internal void EnsureHeaderVisibleInContainer()
        {
            Vector2Int pos = LocalPosition;
            ClampHeaderVisibleInContainer(ref pos, Size);
            ApplyProgrammaticRect(pos, Size, false);
        }

        /// <summary>
        /// Apply a Fugui-owned move/resize and notify listeners after the stored rect is up to date.
        /// </summary>
        internal void ApplyProgrammaticRect(Vector2Int position, Vector2Int size, bool notifyResize, bool forceResizeNotification = false)
        {
            bool moved = LocalPosition != position;
            bool resized = Size != size;

            if (moved || resized)
            {
                _ignoreTransformThisFrame = true;
            }

            if (moved)
            {
                LocalPosition = position;
                _lastFramePos = position;
                HasMovedThisFrame = true;
            }

            if (resized)
            {
                Size = size;
                _lastFrameSize = size;
            }

            if (notifyResize && (resized || forceResizeNotification))
            {
                Fire_OnProgrammaticResized();
            }
        }

        /// <summary>
        /// Notify resize listeners for layout-driven size changes without entering an active resize state.
        /// </summary>
        internal void Fire_OnProgrammaticResized()
        {
            if (!_ignoreResizeForThisFrame)
            {
                OnResize?.Invoke(this);
                OnResized?.Invoke(this);
                Fugui.Fire_OnWindowResized(this);
            }
        }

        /// <summary>
        /// Resize and move a rect so it fits entirely in the current container.
        /// </summary>
        private void FitRectInsideContainer(ref Vector2Int pos, ref Vector2Int size)
        {
            if (Container == null)
            {
                return;
            }

            Vector2Int containerSize = Container.Size;
            int maxWidth = Mathf.Max(1, containerSize.x);
            int maxHeight = Mathf.Max(1, containerSize.y);
            size.x = Mathf.Clamp(size.x, 1, maxWidth);
            size.y = Mathf.Clamp(size.y, 1, maxHeight);
            pos.x = Mathf.Clamp(pos.x, 0, Mathf.Max(0, maxWidth - size.x));
            pos.y = Mathf.Clamp(pos.y, 0, Mathf.Max(0, maxHeight - size.y));
        }

        /// <summary>
        /// Clamp only enough position to keep part of the custom title bar visible in the container.
        /// </summary>
        private void ClampHeaderVisibleInContainer(ref Vector2Int pos, Vector2Int size)
        {
            if (Container == null)
            {
                return;
            }

            Vector2Int containerSize = Container.Size;
            int maxWidth = Mathf.Max(1, containerSize.x);
            int maxHeight = Mathf.Max(1, containerSize.y);
            float scale = Container.Context?.Scale ?? Fugui.Scale;
            int headerHeight = Mathf.Max(1, Mathf.RoundToInt(GetCustomTopChromeHeight()));
            int visibleWidth = Mathf.Min(Mathf.Max(72, Mathf.RoundToInt(96f * scale)), Mathf.Max(1, size.x), maxWidth);
            int visibleHeight = Mathf.Min(Mathf.Max(8, Mathf.RoundToInt(10f * scale)), headerHeight, maxHeight);

            pos.x = Mathf.Clamp(pos.x, visibleWidth - Mathf.Max(1, size.x), maxWidth - visibleWidth);
            pos.y = Mathf.Clamp(pos.y, visibleHeight - headerHeight, maxHeight - visibleHeight);
        }

        /// <summary>
        /// Returns the close button rectangle in local window coordinates.
        /// </summary>
        private Rect GetCustomCloseButtonRect(float titleBarHeight)
        {
            float width = Mathf.Max(titleBarHeight, 28f * Fugui.Scale);
            return new Rect(Mathf.Max(0f, Size.x - width), 0f, width, titleBarHeight);
        }

        /// <summary>
        /// Returns true if the local mouse position is over the custom close button.
        /// </summary>
        private bool IsCustomCloseButtonHovered(Vector2 localMousePosition)
        {
            if (!IsClosable || IsExternal)
            {
                return false;
            }

            float titleBarHeight = GetCustomTopChromeHeight();
            return titleBarHeight > 0f && GetCustomCloseButtonRect(titleBarHeight).Contains(localMousePosition);
        }

        /// <summary>
        /// Returns true when the window header should close the window from a middle click.
        /// </summary>
        private bool ShouldCloseOnMiddleClickFromHeader()
        {
            return CloseOnMiddleClick &&
                   IsClosable &&
                   IsInterractable &&
                   Mouse != null &&
                   Mouse.IsClicked(FuMouseButton.Center) &&
                   IsCustomTitleBarHovered(Mouse.Position);
        }

        /// <summary>
        /// Returns true if the local mouse position is over the Fugui title bar.
        /// </summary>
        private bool IsCustomTitleBarHovered(Vector2 localMousePosition)
        {
            if (IsDocked || !ShouldDrawCustomTitleBar())
            {
                return false;
            }

            float titleBarHeight = GetCustomTopChromeHeight();
            return titleBarHeight > 0f &&
                   localMousePosition.y >= 0f &&
                   localMousePosition.y <= titleBarHeight &&
                   localMousePosition.x >= 0f &&
                   localMousePosition.x <= Size.x;
        }

        /// <summary>
        /// Returns true if Fugui can move this window with its own title bar.
        /// </summary>
        private bool CanCustomMoveWindow()
        {
            return IsInterractable &&
                   !HasPersistentInputLocks() &&
                   !IsDocked &&
                   Container != null &&
                   !Container.ForcePos() &&
                   !_windowFlags.HasFlag(ImGuiWindowFlags.NoMove);
        }

        /// <summary>
        /// Lock this window input for the current frame without creating a persistent API lock.
        /// </summary>
        private void LockInputsForCurrentFrame()
        {
            _inputLockedForThisFrame = true;
            ForceDraw(2);
        }

        /// <summary>
        /// Update the resize chrome hit-test before ImGui receives the content input state.
        /// </summary>
        private void UpdateCustomResizeInputBlock()
        {
            _inputLockedForThisFrame = false;
            _customResizeHoveredEdge = FuWindowResizeEdge.None;
            if (HasPersistentInputLocks())
            {
                _customResizeLocksWindowInputs = false;
                _customResizeEdge = FuWindowResizeEdge.None;
                return;
            }

            _customResizeLocksWindowInputs = _customResizeEdge != FuWindowResizeEdge.None;
            if (_customResizeLocksWindowInputs)
            {
                LockInputsForCurrentFrame();
                return;
            }
            if (!CanCustomResizeWindow())
            {
                return;
            }

            if (!IsFrontMostFloatingSurfaceAtMouse())
            {
                return;
            }

            if (Fugui.IsInsideAnyPopup(Container.LocalMousePos))
            {
                return;
            }

            _customResizeHoveredEdge = GetHoveredCustomResizeEdgeRaw(Container.LocalMousePos - LocalPosition);
            _customResizeLocksWindowInputs = _customResizeHoveredEdge != FuWindowResizeEdge.None;
            if (_customResizeLocksWindowInputs)
            {
                LockInputsForCurrentFrame();
            }
        }

        /// <summary>
        /// Returns true if Fugui can resize this window with its own resize handles.
        /// </summary>
        private bool CanCustomResizeWindow()
        {
            return IsInterractable &&
                   !HasPersistentInputLocks() &&
                   !IsDocked &&
                   !IsDragging &&
                   !Fugui.IsDraggingAnything() &&
                   Container != null &&
                   !Container.ForcePos() &&
                   !_windowFlags.HasFlag(ImGuiWindowFlags.NoResize);
        }

        /// <summary>
        /// Get the hovered custom resize edge.
        /// </summary>
        private FuWindowResizeEdge GetHoveredCustomResizeEdge(Vector2Int localMousePosition)
        {
            if (!CanCustomResizeWindow())
            {
                return FuWindowResizeEdge.None;
            }

            return GetHoveredCustomResizeEdgeRaw(localMousePosition);
        }

        /// <summary>
        /// Get the hovered custom resize edge from geometry only.
        /// </summary>
        private FuWindowResizeEdge GetHoveredCustomResizeEdgeRaw(Vector2Int localMousePosition)
        {
            float border = Mathf.Max(4f, 6f * Fugui.Scale);
            float corner = Mathf.Max(10f, 14f * Fugui.Scale);
            bool inVerticalRange = localMousePosition.y >= 0 && localMousePosition.y <= Size.y;
            bool inHorizontalRange = localMousePosition.x >= 0 && localMousePosition.x <= Size.x;
            bool left = inVerticalRange && localMousePosition.x >= 0 && localMousePosition.x <= border;
            bool right = inVerticalRange && localMousePosition.x <= Size.x && localMousePosition.x >= Size.x - border;
            bool bottom = inHorizontalRange && localMousePosition.y <= Size.y && localMousePosition.y >= Size.y - border;
            bool bottomCorner = inHorizontalRange && localMousePosition.y <= Size.y && localMousePosition.y >= Size.y - corner;
            bool leftCorner = inVerticalRange && localMousePosition.x >= 0 && localMousePosition.x <= corner;
            bool rightCorner = inVerticalRange && localMousePosition.x <= Size.x && localMousePosition.x >= Size.x - corner;

            if (bottomCorner && leftCorner)
            {
                return FuWindowResizeEdge.BottomLeft;
            }
            if (bottomCorner && rightCorner)
            {
                return FuWindowResizeEdge.BottomRight;
            }
            if (left)
            {
                return FuWindowResizeEdge.Left;
            }
            if (right)
            {
                return FuWindowResizeEdge.Right;
            }
            if (bottom)
            {
                return FuWindowResizeEdge.Bottom;
            }

            return FuWindowResizeEdge.None;
        }

        /// <summary>
        /// Return true when this floating surface is the top-most surface under the mouse.
        /// </summary>
        private bool IsFrontMostFloatingSurfaceAtMouse()
        {
            if (Container is not FuMainWindowContainer mainContainer || mainContainer.Windows == null)
            {
                return true;
            }

            Vector2Int mousePosition = mainContainer.LocalMousePos;
            FuWindow frontMost = null;
            foreach (FuWindow window in mainContainer.Windows.Values)
            {
                if (window == null ||
                    window.Container != mainContainer ||
                    !window.IsOpened ||
                    !window.IsInitialized ||
                    !(Fugui.Layouts?.ShouldDrawWindow(window) ?? true) ||
                    !IsFloatingSurface(window) ||
                    !window.LocalRect.Contains(mousePosition))
                {
                    continue;
                }

                frontMost = window;
            }

            return frontMost == null || frontMost == this;
        }

        /// <summary>
        /// Return true for normal floating windows and dock groups rendered in the floating pass.
        /// </summary>
        private static bool IsFloatingSurface(FuWindow window)
        {
            return !window.IsDocked ||
                   (Fugui.Layouts?.IsWindowInFloatingDockRoot(window) ?? false);
        }

        /// <summary>
        /// Read mouse buttons directly from ImGui for custom window chrome while content input is blocked.
        /// </summary>
        private static bool IsRawMouseDown(FuMouseButton button)
        {
            return button != FuMouseButton.None && ImGui.IsMouseClicked((ImGuiMouseButton)button);
        }

        /// <summary>
        /// Read mouse buttons directly from ImGui for custom window chrome while content input is blocked.
        /// </summary>
        private static bool IsRawMousePressed(FuMouseButton button)
        {
            return button != FuMouseButton.None && ImGui.IsMouseDown((ImGuiMouseButton)button);
        }

#if FU_EXTERNALIZATION
        /// <summary>
        /// Draw the title buttons for external window (minimize, maximize, close)
        /// </summary>
        private void DrawExternalWindowTitleButtons()
        {
            // -----------------------------
            // PARAMETERS
            // -----------------------------
            FuElementSize btnSize = new FuElementSize(16f, 16f);    // Button size
            float btnPaddingH = 4f * Fugui.Scale;                                // Horizontal padding between buttons
            float iconPadding = 4f * Fugui.Scale;                                // Internal padding for icons
            float iconThickness = 1f * Fugui.Scale;                               // Icon line thickness
            float iconRounding = 2f * Fugui.Scale;  // Icon rounding for maximize/restore
            float TitleBar_Height = WorkingAreaPosition.y;

            bool IsMaximized = ((FuExternalContext)((FuExternalWindowContainer)Container).Context).Window.IsMaximized;
            float btnHeight = btnSize.ScaledSize.y;                      // Vertical size forced by the title bar
            float btnCenterOffset = (TitleBar_Height - btnSize.ScaledSize.y) * 0.5f;

            // Colors
            uint iconColor = Fugui.Themes.GetColorU32(FuColors.Text);

            // -----------------------------
            // PRECALC
            // -----------------------------
            float fullBtnWidth = btnSize.ScaledSize.x + btnPaddingH;
            float startX = Size.x - (fullBtnWidth * 3f) - 4f * Fugui.Scale;

            ImDrawListPtr dl = ImGui.GetForegroundDrawList();

            Vector2 mousePos = Mouse.Position;
            bool mouseDown = Mouse.IsDown(FuMouseButton.Left);
            bool mouseClicked = Mouse.IsClicked(FuMouseButton.Left);

            // Helper for hit test
            Func<Vector2, bool> isHovered = (pos) =>
            {
                return mousePos.x >= pos.x &&
                       mousePos.x <= pos.x + btnSize.ScaledSize.x &&
                       mousePos.y >= pos.y &&
                       mousePos.y <= pos.y + TitleBar_Height;
            };

            // Helper to draw background
            Action<Vector2, bool> drawButtonBG = (pos, hovered) =>
            {
                uint col = hovered
                    ? (mouseDown
                        ? Fugui.Themes.GetColorU32(FuColors.ButtonActive)
                        : Fugui.Themes.GetColorU32(FuColors.ButtonHovered))
                    : 0;

                Vector2 pMin = pos;
                Vector2 pMax = new Vector2(pos.x + btnSize.ScaledSize.x, pos.y + btnHeight);
                dl.AddRectFilled(pMin, pMax, col, Fugui.Themes.FrameRounding);
            };

            // -------------------------------------------------------
            // 1) MINIMIZE BUTTON
            // -------------------------------------------------------
            Vector2 btnPos = new Vector2(startX, btnCenterOffset);
            bool hovered = isHovered(btnPos);
            drawButtonBG(btnPos, hovered);

            // Icon "-"
            Vector2 lineStart = btnPos + new Vector2(iconPadding, btnSize.ScaledSize.y * 0.65f);
            Vector2 lineEnd = btnPos + new Vector2(btnSize.ScaledSize.x - iconPadding, btnSize.ScaledSize.y * 0.65f);
            dl.AddLine(lineStart, lineEnd, iconColor, iconThickness);

            if (hovered && mouseClicked)
                ((FuExternalContext)((FuExternalWindowContainer)Container).Context).Window.Minimize();

            // -------------------------------------------------------
            // 2) MAXIMIZE / RESTORE BUTTON
            // -------------------------------------------------------
            btnPos = new Vector2(startX + fullBtnWidth, btnCenterOffset);
            hovered = isHovered(btnPos);
            drawButtonBG(btnPos, hovered);

            if (!IsMaximized)
            {
                // MAXIMIZE icon (□)
                Vector2 p1 = btnPos + new Vector2(iconPadding, iconPadding);
                Vector2 p2 = btnPos + new Vector2(btnSize.ScaledSize.x - iconPadding, btnSize.ScaledSize.y - iconPadding);
                dl.AddRect(p1, p2, iconColor, iconRounding, ImDrawFlags.None, iconThickness);
            }
            else
            {
                // RESTORE icon (two overlapping rectangles)
                float off = iconPadding * 0.6f;

                // BASE RECTANGLES (positions)
                Vector2 a1 = btnPos + new Vector2(iconPadding + off, iconPadding);
                Vector2 a2 = btnPos + new Vector2(btnSize.ScaledSize.x - iconPadding, btnSize.ScaledSize.y - iconPadding - off);

                Vector2 b1 = btnPos + new Vector2(iconPadding, iconPadding + off);
                Vector2 b2 = btnPos + new Vector2(btnSize.ScaledSize.x - iconPadding - off, btnSize.ScaledSize.y - iconPadding);

                // Draw the back rectangle first
                dl.AddRect(a1, a2, iconColor, iconRounding, ImDrawFlags.None, iconThickness); // top-right

                // BACKGROUND RECT (fill) for the top rectangle
                // This prevents seeing the rear rectangle through the front one.
                uint bgCol = hovered
                    ? (mouseDown
                        ? Fugui.Themes.GetColorU32(FuColors.ButtonActive)
                        : Fugui.Themes.GetColorU32(FuColors.ButtonHovered))
                    : Fugui.Themes.GetColorU32(FuColors.WindowBg);
                dl.AddRectFilled(b1, b2, bgCol, iconRounding);
                // Slightly shrink the bottom rectangle to avoid overlapping lines
                Vector2 b1Bis = b1 - Vector2.one * iconThickness;
                Vector2 b2Bis = b2 + Vector2.one * iconThickness;
                dl.AddRect(b1Bis, b2Bis, bgCol, iconRounding, ImDrawFlags.None, iconThickness); // bottom-left

                // Draw the front rectangle
                dl.AddRect(b1, b2, iconColor, iconRounding, ImDrawFlags.None, iconThickness); // bottom-left
            }

            if (hovered && mouseClicked)
                ((FuExternalContext)((FuExternalWindowContainer)Container).Context).Window.ToggleMaximize();

            // -------------------------------------------------------
            // 3) CLOSE BUTTON
            // -------------------------------------------------------
            btnPos = new Vector2(startX + fullBtnWidth * 2f, btnCenterOffset);
            hovered = isHovered(btnPos);
            drawButtonBG(btnPos, hovered);

            // X icon
            Vector2 pA1 = btnPos + new Vector2(iconPadding, iconPadding);
            Vector2 pA2 = btnPos + new Vector2(btnSize.ScaledSize.x - iconPadding, btnSize.ScaledSize.y - iconPadding);

            Vector2 pB1 = new Vector2(pA1.x, pA2.y);
            Vector2 pB2 = new Vector2(pA2.x, pA1.y);

            dl.AddLine(pA1, pA2, iconColor, iconThickness);
            dl.AddLine(pB1, pB2, iconColor, iconThickness);

            if (hovered && mouseClicked)
                _open = false;
        }
#endif

        /// <summary>
        /// Draw the windows Debug Panel
        /// </summary>
        internal virtual void DrawDebugPanel()
        {
            if (!Fugui.Settings.DrawDebugPanel)
            {
                return;
            }

            Vector2 previousCursorPos = ImGui.GetCursorScreenPos();
            if (_debugPanelExpanded)
            {
                DrawDebugOverlayMarkers();
            }

            Vector2 panelSize = GetDebugPanelSize(DebugPanelWidth, _debugPanelExpanded ? DebugPanelHeight : DebugPanelCollapsedHeight);
            ImGui.SetCursorScreenPos(GetDebugPanelPosition(panelSize, false));

            Fugui.Push(ImGuiStyleVar.ChildRounding, 5f * Fugui.Scale);
            Fugui.Push(ImGuiStyleVar.ChildBorderSize, 1f);
            Fugui.Push(ImGuiStyleVar.WindowPadding, new Vector2(8f, 6f) * Fugui.Scale);
            Fugui.Push(ImGuiCol.ChildBg, new Vector4(.06f, .065f, .07f, .86f));
            Fugui.Push(ImGuiCol.Border, new Vector4(1f, .25f, .2f, .55f));
            if (ImGui.BeginChild(ID + "d", panelSize, ImGuiChildFlags.Borders | ImGuiChildFlags.AlwaysUseWindowPadding, ImGuiWindowFlags.NoSavedSettings))
            {
                if (ImGui.ArrowButton(ID + "dToggle", _debugPanelExpanded ? ImGuiDir.Down : ImGuiDir.Right))
                {
                    _debugPanelExpanded = !_debugPanelExpanded;
                    ForceDraw();
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(_debugPanelExpanded ? "Collapse debug panel" : "Expand debug panel");
                }
                ImGui.SameLine();
                ImGui.Text(_debugPanelExpanded ? "FuWindow debug" : "Debug");
                ImGui.Separator();
                DrawDebugLine("State", State.ToString());
                if (!this.HasJustBeenDraw && State == FuWindowState.Idle && float.IsInfinity(_targetDeltaTimeMs))
                {
                    CurrentFPS = 0;
                    DeltaTime = 0f;
                }
                DrawDebugLine("FPS", Mathf.RoundToInt(CurrentFPS) + " (" + (DeltaTime * 1000f).ToString("f2") + " ms)");
                DrawDebugLine("Mesh", GetDebugMeshSummary());

                if (_debugPanelExpanded)
                {
                    string target = "infinity";
                    if (!float.IsInfinity(_targetDeltaTimeMs))
                    {
                        target = ((int)(_targetDeltaTimeMs * 1000)).ToString() + " ms";
                    }
                    DrawDebugLine("Target", TargetFPS + " (" + target + ")");
                    ImGui.Separator();

                    DrawDebugLine("Local mouse", Mouse.Position.ToString());
                    DrawDebugLine("Container mouse", Container.LocalMousePos.ToString());
                    DrawDebugLine("Container pos", Container.Position.ToString());
                    DrawDebugLine("World pos", WorldPosition.ToString());
                    DrawDebugLine("Local pos", LocalPosition.ToString());
                    DrawDebugLine("Size", Size.ToString());
                    DrawDebugLine("Work area size", WorkingAreaSize.ToString());
                    DrawDebugLine("Work area pos", WorkingAreaPosition.ToString());
                    DrawDebugLine("Work area mouse", WorkingAreaMousePosition.ToString());
                    DrawDebugLine("Hovered", IsHovered.ToString());
                    DrawDebugLine("Child draw lists", (ChildrenDrawLists != null ? ChildrenDrawLists.Count : 0).ToString());
                }
            }
            ImGui.EndChild();
            Fugui.PopColor(2);
            Fugui.PopStyle(3);
            ImGui.SetCursorScreenPos(previousCursorPos);
        }

        /// <summary>
        /// Draw debug overlay markers in the current window draw list so they are cached with the window mesh.
        /// </summary>
        private void DrawDebugOverlayMarkers()
        {
            ImDrawListPtr dl = ImGui.GetWindowDrawList();
            // draw working area rect
            Vector2 windowPos = ImGui.GetWindowPos();
            Vector2 pos = windowPos + new Vector2(WorkingAreaPosition.x, WorkingAreaPosition.y);
            dl.AddRect(pos, pos + WorkingAreaSize, ImGui.ColorConvertFloat4ToU32(Color.red), 1f, ImDrawFlags.None, 2f * Fugui.Scale);

            // draw mouse rect
            pos = windowPos + new Vector2(WorkingAreaPosition.x + WorkingAreaMousePosition.x, WorkingAreaPosition.y + WorkingAreaMousePosition.y);
            dl.AddCircleFilled(pos, 3f * Fugui.Scale, ImGui.ColorConvertFloat4ToU32(Color.red));
        }

        /// <summary>
        /// Get a debug panel size clamped to the current window.
        /// </summary>
        protected Vector2 GetDebugPanelSize(float width, float height)
        {
            float scale = Container?.Context?.Scale ?? Fugui.Scale;
            Vector2 windowSize = ImGui.GetWindowSize();
            Vector2 margin = Vector2.one * DebugPanelMargin * scale;
            float availableWidth = Mathf.Max(1f, windowSize.x - margin.x * 2f);
            float availableHeight = Mathf.Max(1f, windowSize.y - margin.y * 2f);
            float minWidth = Mathf.Min(160f * scale, availableWidth);
            float minHeight = Mathf.Min(92f * scale, availableHeight);

            return new Vector2(
                Mathf.Clamp(width * scale, minWidth, availableWidth),
                Mathf.Clamp(height * scale, minHeight, availableHeight));
        }

        /// <summary>
        /// Get a debug panel overlay position inside the current window.
        /// </summary>
        protected Vector2 GetDebugPanelPosition(Vector2 panelSize, bool alignRight, float yOffset = 0f)
        {
            float scale = Container?.Context?.Scale ?? Fugui.Scale;
            Vector2 windowPos = ImGui.GetWindowPos();
            Vector2 windowSize = ImGui.GetWindowSize();
            Vector2 margin = Vector2.one * DebugPanelMargin * scale;

            float minX = windowPos.x + margin.x;
            float maxX = Mathf.Max(minX, windowPos.x + windowSize.x - panelSize.x - margin.x);
            float minY = windowPos.y + margin.y;
            float maxY = Mathf.Max(minY, windowPos.y + windowSize.y - panelSize.y - margin.y);
            float x = alignRight ? maxX : minX;
            float y = windowPos.y + Mathf.Max(0f, WorkingAreaPosition.y) + margin.y + yOffset;

            return new Vector2(Mathf.Clamp(x, minX, maxX), Mathf.Clamp(y, minY, maxY));
        }

        /// <summary>
        /// Draw a compact key/value debug row.
        /// </summary>
        protected void DrawDebugLine(string label, string value)
        {
            ImGui.Text(label + " :");
            ImGui.SameLine((_debugPanelExpanded ? 128f : 58f) * Fugui.Scale);
            ImGui.Text(value);
        }

        /// <summary>
        /// Get a compact summary of the cached mesh owned by this window.
        /// </summary>
        protected string GetDebugMeshSummary()
        {
            if (!HasCachedRenderMesh)
            {
                return "no cached mesh";
            }

            return _cachedDrawLists.Count + "dl / " +
                   _renderMeshData.SubMeshCount + "sub / " +
                   _renderMeshData.TotalVtxCount + "v / " +
                   _renderMeshData.TotalIdxCount + "i";
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
            bool mustBeDraw = _forceRedraw > 0;
            switch (State)
            {
                default:
                case FuWindowState.Idle:
                    mustBeDraw |= Fugui.Time > _lastRenderTime + _targetDeltaTimeMs;
                    break;

                case FuWindowState.Manipulating:
                    if (!IsDragging || IsResizing || !HasCachedRenderMesh)
                    {
                        mustBeDraw |= IsInterractable;
                    }
                    break;
            }

            return mustBeDraw;
        }

        /// <summary>
        /// Externalize this window
        /// </summary>
        public void Externalize()
        {
            Fugui.ExternalizeWindow(this);
        }

        /// <summary>
        /// Check if we need to auto externalize this window
        /// </summary>
        private void CheckAutoExternalize()
        {
            if (!IsExternalizable || IsExternal || !IsDragging || Container == null || Container is not FuMainWindowContainer)
            {
                return;
            }

            // if mouse is out of container bounds, externalize
            Vector2Int mousePos = Container.LocalMousePos;
            Rect containerRect = new Rect(Vector2.zero, Container.Size);
            if (!containerRect.Contains(mousePos))
            {
                Fugui.ExternalizeWindow(this);
            }
        }

        /// <summary>
        /// Internalize this window
        /// </summary>
        public void Internalize()
        {
            Fugui.InternalizeWindow(this);
        }

        /// <summary>
        /// Check if we need to auto internalize this window
        /// </summary>
        private void CheckAutoInternalize()
        {
#if FU_EXTERNALIZATION
            if (!IsExternal || Container == null || Container is not FuExternalWindowContainer)
            {
                return;
            }
            FuExternalWindowContainer externalContainer = (FuExternalWindowContainer)Container;
            FuExternalContext externalContext = (FuExternalContext)externalContainer.Context;
            if (!externalContext.Window.CanInternalize || !externalContext.Window.IsDragging)
                return;

            FuMainWindowContainer mainContainer = Fugui.DefaultContainer;
            Vector2Int mousePos = externalContainer.Window.Mouse.Position + externalContainer.Position;
            Rect mainContainerRect = new Rect(mainContainer.Position, mainContainer.Size);
            if (mainContainerRect.Contains(mousePos))
            {
                Fugui.InternalizeWindow(this);
            }
#endif
        }

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
            Fire_OnProgrammaticResized();
            OnDrag?.Invoke(this);
            Fugui.Fire_OnWindowDocked(this);
        }

        /// <summary>
        /// Fire event that we just UnDock this window
        /// </summary>
        public void Fire_OnUnDock()
        {
            OnUnDock?.Invoke(this);
            Fire_OnProgrammaticResized();
            OnDrag?.Invoke(this);
            Fugui.Fire_OnWindowUnDocked(this);
        }

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

        /// <summary>
        /// Try to dock the window in the right DockSpace in the current DockingLayout
        /// </summary>
        /// <returns>whatever the window has been docked</returns>
        public bool AutoDock()
        {
            return Fugui.Layouts.AutoDockWindow(this);
        }

        /// <summary>
        /// Give focus to this window, selecting its dock tab or moving it above other floating windows.
        /// </summary>
        public void Focus()
        {
            if (Container is FuMainWindowContainer mainContainer)
            {
                mainContainer.ActivateWindow(this);
                return;
            }

            ImGui.SetWindowFocus(ID);
            ForceFocusOnNextFrame();
            ForceDraw(2);
            Fugui.ForceDrawAllWindows(2);
        }

        /// <summary>
        /// Remove this window from it container and from Manager windows list
        /// </summary>
        /// <summary>
        /// Remove this window from its container and from the manager windows list.
        /// </summary>
        public void Close()
        {
            void onRemovedFromContainerDelegate(FuWindow window)
            {
                window.OnRemovedFromContainer -= onRemovedFromContainerDelegate;
                FinalizeClose();
            }

            OnRemovedFromContainer += onRemovedFromContainerDelegate;

            if (!TryRemoveFromContainer())
            {
                OnRemovedFromContainer -= onRemovedFromContainerDelegate;
                FinalizeClose();
            }
        }

        /// <summary>
        /// Finalizes the window close process.
        /// </summary>
        private void FinalizeClose()
        {
            ClearDrawDataCache();
            Fugui.TryRemoveUIWindow(this);
            Fugui.ForceDrawAllWindows(2);
            OnClosed?.Invoke(this);
            Fugui.Fire_OnWindowClosed(this);
        }

        /// <summary>
        /// Clears cached draw lists owned by this window.
        /// </summary>
        internal void ClearDrawDataCache()
        {
            DrawList?.Dispose();
            DrawList = new DrawList();

            ClearCachedChildDrawLists();
            ChildrenDrawLists ??= new Dictionary<string, DrawList>();
            _cachedDrawLists.Clear();
            _renderMeshData?.Destroy();
            _renderMeshData = null;
            HasJustBeenDraw = false;
        }

        /// <summary>
        /// Store the latest draw lists and rebuild this window render mesh.
        /// </summary>
        /// <param name="orderedDrawLists">Draw lists owned by this window in ImGui render order.</param>
        /// <param name="displaySize">Draw data display size.</param>
        /// <param name="framebufferScale">Draw data framebuffer scale.</param>
        internal void CacheDrawData(List<DrawList> orderedDrawLists, Vector2 displaySize, Vector2 framebufferScale)
        {
            ClearCachedChildDrawLists();
            ChildrenDrawLists ??= new Dictionary<string, DrawList>();
            _cachedDrawLists.Clear();

            if (orderedDrawLists != null)
            {
                for (int i = 0; i < orderedDrawLists.Count; i++)
                {
                    DrawList drawList = orderedDrawLists[i];
                    if (drawList == null)
                    {
                        continue;
                    }

                    _cachedDrawLists.Add(drawList);
                    if (!ReferenceEquals(drawList, DrawList))
                    {
                        AddCachedChildDrawList(drawList, i);
                    }
                }
            }

            _renderMeshData ??= new DrawListMesh(ID + "_RenderMesh");
            _renderMeshLocalPosition = LocalPosition;
            _renderMeshData.Update(_cachedDrawLists, displaySize, framebufferScale);
        }

        /// <summary>
        /// Dispose cached child draw lists without touching the root window draw list.
        /// </summary>
        private void ClearCachedChildDrawLists()
        {
            if (ChildrenDrawLists == null)
            {
                return;
            }

            foreach (DrawList child in ChildrenDrawLists.Values)
            {
                child.Dispose();
            }
            ChildrenDrawLists.Clear();
        }

        /// <summary>
        /// Register a cached child draw list under a stable unique key.
        /// </summary>
        /// <param name="drawList">Child draw list to register.</param>
        /// <param name="index">Draw list index in the cached order.</param>
        private void AddCachedChildDrawList(DrawList drawList, int index)
        {
            string key = !string.IsNullOrEmpty(drawList.WindowName)
                ? drawList.WindowName
                : ID + "/Child_" + index;

            if (!ChildrenDrawLists.ContainsKey(key))
            {
                ChildrenDrawLists.Add(key, drawList);
                return;
            }

            int suffix = 1;
            string uniqueKey;
            do
            {
                uniqueKey = key + "_" + suffix;
                suffix++;
            }
            while (ChildrenDrawLists.ContainsKey(uniqueKey));

            ChildrenDrawLists.Add(uniqueKey, drawList);
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
            if (!IsInterractable)
            {
                ReleaseInputFocus();
                if (State != FuWindowState.Idle)
                {
                    SetPerformanceState(FuWindowState.Idle);
                }
                return;
            }

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
            if (IsDragging || IsResizing || IsHovered || WantCaptureKeyboard)
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
        public void AddWindowFlag(ImGuiWindowFlags flag)
        {
            _windowFlags |= flag;
        }

        /// <summary>
        /// Remove a window flag from this window
        /// </summary>
        /// <param name="flag">flag to remove</param>
        public void RemoveWindowFlag(ImGuiWindowFlags flag)
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
                    TargetFPS = int.MaxValue;
                    break;
            }
        }
    }
}
