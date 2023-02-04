using Fu.Framework;
using ImGuiNET;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu.Core
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
                IsInMainContainer = !(value is FuExternalWindowContainer);
            }
        }

        // properties
        public FuWindowsNames WindowName { get; private set; }
        public string ID { get; private set; }
        public Action<FuWindow> UI { get; internal set; }
        public Action<FuWindow> Constraints { get; internal set; }
        public bool HasMovedThisFrame { get; private set; }
        public bool HasJustBeenDraw { get; set; }
        public bool ShowDebugPanel { get; set; }
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

        // states flags
        public bool HasFocus { get; internal set; }
        public bool IsOpened { get { return _open; } internal set { _open = value; } }
        public bool IsDockable { get; private set; }
        public bool NoDockingOverMe { get; private set; }
        public bool IsExternalizable { get; private set; }
        public bool IsInitialized { get; private set; }
        public bool IsExternal { get; internal set; }
        public bool IsResizing { get; private set; }
        public bool IsInMainContainer { get; private set; }
        public bool IsDragging { get; internal set; }
        public bool IsHovered { get; internal set; }
        public bool IsDocked { get; internal set; }
        public bool IsBusy { get; internal set; }
        public bool IsInterractible { get; set; }

        // events
        public event Action<FuWindow> OnResize;
        public event Action<FuWindow> OnResized;
        public event Action<FuWindow> OnDrag;
        public event Action<FuWindow> OnClosed;
        public event Action<FuWindow> OnDock;
        public event Action<FuWindow> OnUnDock;
        public event Action<FuWindow> OnReady;
        public event Action<FuWindow> OnRemovedFromContainer;

        // private fields
        internal ImGuiWindowFlags _windowFlags;
        private bool _forceLocationNextFrame = false;
        private bool _forceFocusNextFrame = false;
        private bool _forceRedraw = false;
        private bool _sendReadyNextFrame = false;
        internal bool _ignoreResizeForThisFrame = false;
        private Vector2Int _lastFrameSize;
        private Vector2Int _lastFramePos;
        private float _leaveThreshold = 0.8f;
        internal float _targetDeltaTimeMs = 1;
        internal float _lastRenderTime = 0;
        private bool _open = true;
        private static int _windowIndex = 0;

        // static fields
        public static FuWindow CurrentDrawingWindow { get; private set; }
        #endregion

        #region Window Location
        internal Vector2Int _size;
        public Vector2Int Size
        {
            get { return _size; }
            set
            {
                _size = new Vector2Int((int)(value.x * Container?.Scale ?? 1f), (int)(value.y * Container?.Scale ?? 1f));
                _localRect = new Rect(_localPosition, _size);
                _forceLocationNextFrame = true;
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
            ID = windowDefinition.Id + "##" + _windowIndex;
            _windowIndex++;

            if (!Fugui.TryAddUIWindow(this))
            {
                return;
            }

            // debug
            ShowDebugPanel = false;
            IsBusy = true;
            IsInitialized = false;
            UI = windowDefinition.UI;
            IsOpened = true;
            HasFocus = false;
            WindowName = windowDefinition.WindowName;
            IsDockable = windowDefinition.IsDockable;
            IsExternalizable = windowDefinition.IsExternalizable;
            IsInterractible = windowDefinition.IsInterractible;
            Mouse = new FuMouseState();
            Keyboard = new FuKeyboardState(this);
            DrawList = new DrawList();
            ChildrenDrawLists = new Dictionary<string, DrawList>();
            Size = windowDefinition.Size;
            LocalPosition = windowDefinition.Position;
            _lastFrameSize = Size;
            _lastFramePos = LocalPosition;
            IsInitialized = true;
            _forceLocationNextFrame = true;
            _windowFlags = ImGuiWindowFlags.NoCollapse;
            // assume that we are Idle
            State = FuWindowState.Idle;
            TargetFPS = Fugui.Settings.IdleFPS;
            // compute last render time
            _lastRenderTime = Fugui.Time;
            NoDockingOverMe = windowDefinition.NoDockingOverMe;
            // add default overlays
            Overlays = new Dictionary<string, FuOverlay>();
            foreach (FuOverlay overlay in windowDefinition.Overlays.Values)
            {
                overlay.AnchorWindow(this);
            }
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

            // update mouse buttons states
            updateMouseState();

            // we need to draw ImGui Window component (Begin/End)
            if (!IsDocked)
            {
                if (Container.ForcePos() || _forceLocationNextFrame)
                {
                    ImGui.SetNextWindowPos(LocalPosition);
                }
                if (_forceLocationNextFrame)
                {
                    ImGui.SetNextWindowSize(Size);
                }
                _forceLocationNextFrame = false;
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

            // handle ImGui window local move
            if (_lastFramePos != newFramePos)
            {
                LocalPosition = newFramePos;
                HasMovedThisFrame = true;
                Fire_OnDrag();
                if (!IsDragging && Mouse.IsPressed(0))
                {
                    IsDragging = true;
                }
            }

            // handle ImGui window resize
            if (_lastFrameSize != newFrameSize)
            {
                if (!_forceLocationNextFrame || IsDocked)
                {
                    _size = newFrameSize;
                    _localRect = new Rect(_localPosition, _size);
                }
                Fire_OnResize();
            }

            // drag state
            if (IsDragging && !Mouse.IsPressed(0))
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
        public virtual void DrawWindowBody(ref Vector2Int newFrameSize, ref Vector2Int newFramePos)
        {
            // if we are in main window container and this window is docked, we must surround UI by ImGuiChild
            // Child will have name that will be used by DrawCmd to store idx and vtx without recompute DrawList
            bool createChild = IsDocked && IsInMainContainer;
            // invoke user custom constraints
            Constraints?.Invoke(this);
            // set current theme frame padding
            if (ImGui.Begin(ID, ref _open, _windowFlags))
            {
                bool docked = ImGui.IsWindowDocked();
                if (docked != IsDocked)
                {
                    IsDocked = docked;
                    if (IsDocked)
                    {
                        Fire_OnDock();
                    }
                    else
                    {
                        Fire_OnUnDock();
                    }
                    Fire_OnResized();
                }

                // if docked, get size according to avail w and h
                if (IsDocked)
                {
                    // get size of this window
                    CurrentDockID = ImGui.GetWindowDockID();
                    ImGuiDockNodePtr node = ImGuiDocking.DockBuilderGetNode(CurrentDockID);
                    ImRect rect = node.Rect();
                    var size = rect.Max - rect.Min;
                    newFrameSize = new Vector2Int((int)size.X, (int)size.Y);
                }
                else
                {
                    CurrentDockID = 0;
                }

                // Draw UI container. this is needed to store drawList even if window is not render
                if (createChild)
                {
                    Fugui.Push(ImGuiStyleVar.ChildRounding, 0f);
                    Fugui.Push(ImGuiCol.ChildBg, ImGui.GetStyle().Colors[(int)ImGuiCol.WindowBg]); // it's computed by byte, not float, so minimum is 1 / 255 ~= 0.0.0039216f
                    ImGui.BeginChild(ID + "ctnr", Vector2.zero, false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
                    {
                        tryDrawUI();
                    }
                    ImGui.EndChild();
                    Fugui.PopColor();
                    Fugui.PopStyle();
                }
                // Do draw UI if needed
                else
                {
                    tryDrawUI();
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
                HasFocus = ImGui.IsWindowFocused();
                // whatever window is hovered
                IsHovered = (LocalRect.Contains(Container.LocalMousePos) &&
                    ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows | ImGuiHoveredFlags.AllowWhenBlockedByActiveItem | ImGuiHoveredFlags.AllowWhenBlockedByPopup)) ||
                    FuLayout.CurrentPopUpWindowID == ID;

                // draw debug data
                drawDebugPanel();
                ImGui.End();
            }
        }

        /// <summary>
        /// draw UI if needed
        /// </summary>
        private void tryDrawUI()
        {
            // save working area size and position
            _workingAreaSize = new Vector2Int((int)ImGui.GetContentRegionAvail().x, (int)ImGui.GetContentRegionAvail().y);
            _workingAreaPosition = new Vector2Int((int)ImGui.GetCursorScreenPos().x, (int)ImGui.GetCursorScreenPos().y) - _localPosition;
            if (MustBeDraw())
            {
                CurrentDrawingWindow = this;
                _forceRedraw = false;
                // draw user UI callback
                UI?.Invoke(this);
                // save whatever ImGui want capture Keyboard
                WantCaptureKeyboard = ImGui.GetIO().WantTextInput;
                // draw overlays
                drawOverlays();
                // if we need to send that we are ready this frame, let's invoke event
                if (_sendReadyNextFrame)
                {
                    _sendReadyNextFrame = false;
                    OnReady?.Invoke(this);
                }

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
        internal virtual void drawDebugPanel()
        {
            if (!ShowDebugPanel && !Fugui.Settings.DrawDebugPanel)
            {
                return;
            }

            ImGui.SetCursorPos(new Vector2(0f, 32f));
            ImGui.Dummy(Vector2.one);
            Fugui.Push(ImGuiStyleVar.ChildRounding, 4f);
            Fugui.Push(ImGuiCol.ChildBg, new Vector4(.1f, .1f, .1f, 1f));
            ImGui.BeginChild(ID + "d", new Vector2(196f, 282f));
            {
                // states
                ImGui.Text("State : " + State);
                ImGui.Text("FPS : " + (int)CurrentFPS + " (" + (DeltaTime * 1000f).ToString("f2") + " ms)");
                ImGui.Text("Target : " + TargetFPS + "  (" + ((int)(_targetDeltaTimeMs * 1000)).ToString() + " ms)"); ImGui.Dummy(new Vector2(4f, 0f));
                // pos and size
                ImGui.Text("w Mouse : " + Fugui.WorldMousePosition);
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
            ImGui.EndChild();
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
        public void ForceDraw()
        {
            _forceRedraw = true;
        }

        /// <summary>
        /// whatever the window must be draw right now
        /// </summary>
        /// <returns>true if we must draw this UI this frame</returns>
        public bool MustBeDraw()
        {
            switch (IsInMainContainer)
            {
                case true:
                    return Fugui.Time > _lastRenderTime + _targetDeltaTimeMs
                        || _forceRedraw
                        || (IsInterractible && (IsHovered || WantCaptureKeyboard || State == FuWindowState.Manipulating));

                case false:
                    return Fugui.Time > _lastRenderTime + _targetDeltaTimeMs
                        || _forceRedraw
                        || LocalRect.Contains(Container.LocalMousePos);
            }
        }

        /// <summary>
        /// Update current mouse state for this window
        /// </summary>
        private void updateMouseState()
        {
            // set mouse buttons state
            Mouse.ButtonStates[0].SetState(ImGui.IsMouseDown(ImGuiMouseButton.Left) & IsHovered);
            Mouse.ButtonStates[1].SetState(ImGui.IsMouseDown(ImGuiMouseButton.Right) & IsHovered);
            // set mouse pos and wheel
            Mouse.SetPosition(this);
            Mouse.SetWheel(new Vector2(ImGui.GetIO().MouseWheelH, ImGui.GetIO().MouseWheel));
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
        }

        /// <summary>
        /// Fire event that we just UnDock this window
        /// </summary>
        public void Fire_OnUnDock()
        {
            OnUnDock?.Invoke(this);
            OnResize?.Invoke(this);
            OnDrag?.Invoke(this);
        }

        /// <summary>
        ///  Fire event whenever the UIWindow is ready to be used (added to a container and initialized)
        /// </summary>
        public void Fire_OnReady()
        {
            _sendReadyNextFrame = true;
        }

        /// <summary>
        ///  Fire event whenever the UIWindow is ready to be used (added to a container and initialized)
        /// </summary>
        public void Fire_OnRemovedFromContainer()
        {
            OnRemovedFromContainer?.Invoke(this);
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
            return container.TryAddWindow(this);
        }

        /// <summary>
        /// try to remove this window from it current container
        /// </summary>
        /// <returns>true if success</returns>
        public bool TryRemoveFromContainer()
        {
            IsBusy = true;
            return Container?.TryRemoveWindow(ID) ?? false;
        }
        #endregion

        #region Viewports
        /// <summary>
        /// Externalize this window
        /// </summary>
        public void Externalize()
        {
            if (!IsExternalizable || IsExternal)
            {
                return;
            }

            if (TryRemoveFromContainer())
            {
                Fugui.AddExternalWindow(this);
                IsExternal = true;
            }
        }

        /// <summary>
        /// Whatever this window want to leave from it container
        /// </summary>
        /// <returns>true if need to externalize</returns>
        public bool WantToLeave()
        {
            if (!IsDragging || IsBusy || !IsExternalizable || IsExternal)
            {
                return false;
            }

            if (WorldPosition.x < Container.Position.x - (Size.x * _leaveThreshold))
                return true;
            if (WorldPosition.x > Container.Position.x + Container.Size.x - (Size.x * (1f - _leaveThreshold)))
                return true;

            if (WorldPosition.y < Container.Position.y - (Size.y * _leaveThreshold))
                return true;
            if (WorldPosition.y > Container.Position.y + Container.Size.y - (Size.y * (1f - _leaveThreshold)))
                return true;

            return false;
        }

        /// <summary>
        /// Whatever this window cant to enter into a given container
        /// </summary>
        /// <param name="container">container to check entry on</param>
        /// <returns>true if need to enter the given container</returns>
        public bool WantToEnter(IFuWindowContainer container)
        {
            if (!IsDragging || IsBusy || !IsExternalizable || !IsExternal || !CanInternalize)
            {
                return false;
            }

            if (WorldPosition.x < container.Position.x)
                return false;
            if (WorldPosition.x + Size.x > container.Position.x + container.Size.x)
                return false;
            if (WorldPosition.y < container.Position.y)
                return false;
            if (WorldPosition.y + Size.y > container.Position.y + container.Size.y)
                return false;

            return true;
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
        private void drawOverlays()
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
        /// Remove this window from it container and from Manager windows list
        /// </summary>
        public void Close(Action callback)
        {
            // on removeFromContainer delegate
            Action<FuWindow> onremovedFromContainerDelegate = null;
            onremovedFromContainerDelegate = (window) =>
            {
                window.OnRemovedFromContainer -= onremovedFromContainerDelegate;
                Fugui.TryRemoveUIWindow(this);
                OnClosed?.Invoke(this);
                callback?.Invoke();
            };
            OnRemovedFromContainer += onremovedFromContainerDelegate;

            if (!TryRemoveFromContainer())
            {
                Fugui.TryRemoveUIWindow(this);
                OnClosed?.Invoke(this);
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
            if (State == FuWindowState.Manipulating)
            {
                TargetFPS = Fugui.Settings.ManipulatingFPS;
            }
            else if (State == FuWindowState.Idle)
            {
                TargetFPS = Fugui.Settings.IdleFPS;
            }
        }

        /// <summary>
        /// Set window ImGui constraints callback
        /// </summary>
        /// <param name="callback">will be called just before ImGui.Begin()</param>
        public void SetWindowConstraintCallback(Action<FuWindow> callback)
        {
            Constraints = callback;
        }
        #endregion
    }
}