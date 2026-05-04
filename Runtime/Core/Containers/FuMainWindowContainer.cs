using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Fu
{
    /// <summary>
    /// A class representing the main UI container.
    /// </summary>
    public class FuMainWindowContainer : IFuWindowContainer
    {
        #region State
        /// <summary>
        /// The local mouse position relative to the container.
        /// </summary>
        public Vector2Int LocalMousePos => _mousePos;
        /// <summary>
        /// The related FuContext of this Container.
        /// </summary>
        public FuContext Context => _fuguiContext;
        /// <summary>
        /// The scale configuration applied to this container.
        /// </summary>
        public FuContainerScaleConfig ContainerScaleConfig => _fuguiContext.ContainerScaleConfig;
        /// <summary>
        /// The position of the container in world space.
        /// </summary>
        public Vector2Int Position => _worldPosition;
        /// <summary>
        /// The size of the container.
        /// </summary>
        public Vector2Int Size => _size;
        /// <summary>
        /// The drawable content area left after the main menu and footer.
        /// </summary>
        public Rect ContentRect { get; private set; }

        /// <summary>
        /// A dictionary of UI windows contained in the container.
        /// </summary>
        public Dictionary<string, FuWindow> Windows;

        /// <summary>
        /// The ID of the dockspace.
        /// </summary>
        public uint Dockspace_id { get; private set; } = uint.MaxValue;
        /// <summary>
        /// Get Mouse data for this container
        /// </summary>
        public FuMouseState Mouse => _fuMouseState;
        /// <summary>
        /// Get Keyboard data for this container
        /// </summary>
        public FuKeyboardState Keyboard => _fuKeyboardState;

        /// <summary>
        /// Whenever all windows are rendered, but before Modals, ContextMenu, Notify, etc
        /// </summary>
        public event Action OnPostRenderWindows;

        private FuMouseState _fuMouseState;
        private FuKeyboardState _fuKeyboardState;
        // the height of the footer ( <= 0 will hide footer)
        private float _footerHeight = -1f;
        // the UI callback of the footer
        private Action _footerUI = null;
        // The world position of the container.
        private Vector2Int _worldPosition;
        // A queue of windows to be removed.
        private Queue<FuWindow> _toRemoveWindows;
        // A queue of windows to be added.
        private Queue<FuWindow> _toAddWindows;
        private readonly List<string> _pendingBringToFrontWindowIds = new List<string>();
        private bool _isRenderingWindows;
        private readonly List<string> _floatingWindowSwitchOrder = new List<string>();
        private bool _floatingWindowSwitcherOpen;
        private int _floatingWindowSwitchIndex = -1;
        private string _floatingWindowSwitchOriginId;
        // The mouse position relative to the container.
        private Vector2Int _mousePos;
        // The size of the container.
        private Vector2Int _size;
        // current Fugui context
        private FuUnityContext _fuguiContext;
        #endregion

        #region Constructors
        /// <summary>
        /// Constructs a new instance of the MainUIContainer class.
        /// </summary>
        /// <param name="FuguiContext">Fugui Context that draw this container</param>
        public FuMainWindowContainer(FuUnityContext FuguiContext)
        {
            // set current Fugui context
            _fuguiContext = FuguiContext;
            // Initialize the windows dictionary
            Windows = new Dictionary<string, FuWindow>();
            // Initialize the queues for windows
            _toRemoveWindows = new Queue<FuWindow>();
            _toAddWindows = new Queue<FuWindow>();

            // instantiate inputs states
            _fuMouseState = new FuMouseState();
            _fuKeyboardState = new FuKeyboardState(_fuguiContext.IO);

            // Subscribe to the Layout event of the given FuguiContext
            _fuguiContext.OnRender += _fuguiContext_OnRender;
            // Set the docking style color to current theme
            Fugui.Themes.SetTheme(Fugui.Themes.CurrentTheme);
            // Subscribe to the PrepareFrame event to update scale before ImGui.NewFrame.
            _fuguiContext.OnPrepareFrame += context_OnPrepareFrame;
            // Subscribe to the PrepareFrame event of the given FuguiContext to update this container data before rendering
            _fuguiContext.OnFramePrepared += context_OnFramePrepared;
        }
        #endregion

        /// <summary>
        /// Runs the fugui context on render workflow.
        /// </summary>
        private void _fuguiContext_OnRender()
        {
            RenderFuWindows();
        }

        /// <summary>
        /// Returns the context on prepare frame result.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public bool context_OnPrepareFrame()
        {
            Vector2Int previousSize = _size;
            _size = getContextSize();
            _fuguiContext.UpdateContainerScale(_size);
            if (previousSize.x > 0 && previousSize.y > 0 && previousSize != _size)
            {
                Fugui.ForceDrawAllWindows(2);
            }
            return true;
        }

        /// <summary>
        /// Runs the context on frame prepared workflow.
        /// </summary>
        public void context_OnFramePrepared()
        {
            // set size and pos for this frame
            _size = getContextSize();

            // get unity local mouse position
            Vector2Int newMousePos = new Vector2Int((int)Context.IO.MousePos.x, (int)Context.IO.MousePos.y);
            _mousePos = newMousePos;
#if UNITY_EDITOR
            _worldPosition = GameViewUtils.GetPos();
#else
            _worldPosition = Screen.mainWindowPosition;
#endif

            // update mouse state
            _fuMouseState.UpdateState(this);
            _fuKeyboardState.UpdateState();

            // remove windows
            while (_toRemoveWindows.Count > 0)
            {
                FuWindow window = _toRemoveWindows.Dequeue();
                window.OnClosed -= UIWindow_OnClose;
                Windows.Remove(window.ID);
                if (window.Container == this)
                    window.Container = null;
            }

            // add windows
            while (_toAddWindows.Count > 0)
            {
                FuWindow window = _toAddWindows.Dequeue();
                // store UIWindow variable
                Windows.Add(window.ID, window);
                // place window into this container and keep old position
                if (window.Container != null)
                {
                    Vector2Int worldPos = window.WorldPosition;
                    worldPos.y -= 40;
                    window.Container = this;
                    window.LocalPosition = worldPos - Position;
                }
                else
                {
                    window.Container = this;
                }
                window.IsExternal = false;
                // register window events
                window.OnClosed += UIWindow_OnClose;
                window.InitializeOnContainer();
            }
        }

        /// <summary>
        /// Set the footer UI and Height
        /// </summary>
        /// <param name="height">height of the footer (<= 0 will hide footer)</param>
        /// <param name="callbackUI">UI callback of the footer</param>
        public void SetFooter(float height, Action callbackUI)
        {
            _footerHeight = height;
            _footerUI = callbackUI;
        }

        /// <summary>
        /// Configure how this container scales its context.
        /// </summary>
        /// <param name="config">Scale configuration.</param>
        public void SetContainerScaleConfig(FuContainerScaleConfig config)
        {
            Vector2Int size = _size.x > 0 && _size.y > 0
                ? _size
                : getContextSize();
            _fuguiContext.SetContainerScaleConfig(config, size);
        }

        /// <summary>
        /// Returns this container's current drawable context size.
        /// </summary>
        /// <returns>The context size in pixels.</returns>
        private Vector2Int getContextSize()
        {
            if (_fuguiContext != null)
            {
                RenderTexture targetTexture = _fuguiContext.TargetTexture;
                if (targetTexture != null)
                {
                    return new Vector2Int(
                        Mathf.Max(1, targetTexture.width),
                        Mathf.Max(1, targetTexture.height));
                }

                Rect rect = _fuguiContext.Camera != null
                    ? _fuguiContext.Camera.pixelRect
                    : _fuguiContext.PixelRect;

                if (rect.width > 0f && rect.height > 0f)
                {
                    return new Vector2Int(
                        Mathf.Max(1, Mathf.RoundToInt(rect.width)),
                        Mathf.Max(1, Mathf.RoundToInt(rect.height)));
                }
            }

            return new Vector2Int(
                Mathf.Max(1, Screen.width),
                Mathf.Max(1, Screen.height));
        }

        /// <summary>
        /// Execute a callback on each windows on this container
        /// </summary>
        /// <param name="callback">callback to execute on each windows</param>
        public void OnEachWindow(Action<FuWindow> callback)
        {
            foreach (FuWindow window in Windows.Values)
            {
                callback?.Invoke(window);
            }
        }

        /// <summary>
        /// Whatever this container own a specific window
        /// </summary>
        /// <param name="id">ID of the window</param>
        /// <returns>whatever the window is in this container</returns>
        public bool HasWindow(string id)
        {
            return Windows.ContainsKey(id);
        }

        /// <summary>
        /// Render a specific FuWindow
        /// </summary>
        /// <param name="window"></param>
        public void RenderFuWindow(FuWindow window)
        {
            // update window state
            window.UpdateState(Context.IO.MouseDown[0]);

            // we clamp size and position BEFORE drawing window to allow dev to set size and pos inside the drawing callback
            // prevent clamping if window is dragging to avoid clipping
            if (!window.IsDragging && !window.IsDocked && !window.IsResizing)
            {
                // clamp window size
                Vector2Int size = window.Size;
                if (size.x < (int)(64f * Fugui.Scale))
                {
                    size.x = (int)(64f * Fugui.Scale);
                }
                if (size.y < (int)(64f * Fugui.Scale))
                {
                    size.y = (int)(64f * Fugui.Scale);
                }
                if (size.x > Size.x)
                {
                    size.x = Size.x;
                }
                if (size.y > Size.y)
                {
                    size.y = Size.y;
                }
                if (window.Size.x != size.x || window.Size.y != size.y)
                {
                    window.Size = size;
                }

                // clamp window position
                Vector2Int pos = window.LocalPosition;

                // ensure that at least 32 x 32 pixels of the header of the window is visible
                if (pos.y > Size.y - (int)(32f * Fugui.Scale))
                {
                    pos.y = Size.y - (int)(64f * Fugui.Scale);
                }
                if (pos.x > Size.x - (int)(32f * Fugui.Scale))
                {
                    pos.x = Size.x - (int)(64f * Fugui.Scale);
                }
                if (pos.x < -window.Size.x - (int)(32f * Fugui.Scale))
                {
                    pos.x = -window.Size.x + (int)(64f * Fugui.Scale);
                }
                if (pos.y < -window.Size.y - (int)(32f * Fugui.Scale))
                {
                    pos.y = -window.Size.y + (int)(64f * Fugui.Scale);
                }

                if (window.LocalPosition.x != pos.x || window.LocalPosition.y != pos.y)
                {
                    window.LocalPosition = pos;
                }
            }

            // Do draw window
            window.DrawWindow();
        }

        /// <summary>
        /// Render any Windows in this container
        /// </summary>
        public void RenderFuWindows()
        {
            ApplyPendingWindowOrder();
            ProcessFloatingWindowSwitcherInput();
            ApplyPendingWindowOrder();
            DrawMainLayout();
            Fugui.Layouts?.UpdateCustomLayout(this, ContentRect);

            _isRenderingWindows = true;
            try
            {
                // Main-layout docked windows are the background layout surface.
                foreach (FuWindow window in Windows.Values)
                {
                    if (window.IsDocked && !IsFloatingDockGroupWindow(window))
                    {
                        RenderFuWindow(window);
                    }
                }
            }
            finally
            {
                _isRenderingWindows = false;
            }
            ApplyPendingWindowOrder();

            _isRenderingWindows = true;
            try
            {
                foreach (FuWindow window in Windows.Values)
                {
                    if ((!window.IsDocked && !window.IsDragging) || IsFloatingDockGroupWindow(window))
                    {
                        RenderFuWindow(window);
                    }
                }
            }
            finally
            {
                _isRenderingWindows = false;
            }
            ApplyPendingWindowOrder();

            _isRenderingWindows = true;
            try
            {
                foreach (FuWindow window in Windows.Values)
                {
                    if (!window.IsDocked && window.IsDragging)
                    {
                        RenderFuWindow(window);
                    }
                }
            }
            finally
            {
                _isRenderingWindows = false;
            }
            ApplyPendingWindowOrder();

            // invoke OnPostRenderWindows event
            OnPostRenderWindows?.Invoke();

            // render notifications
            Fugui.RenderContextMenu();

            // render modal
            Fugui.RenderModal(this);

            // render popup message
            Fugui.RenderPopupMessage();

            // render notifications
            Fugui.RenderNotifications(this);

            DrawFloatingWindowSwitcherOverlay();
        }

        /// <summary>
        /// Return whether a window belongs to a floating dock group and should be rendered in the floating pass.
        /// </summary>
        private bool IsFloatingDockGroupWindow(FuWindow window)
        {
            return window != null &&
                   window.IsDocked &&
                   Fugui.Layouts != null &&
                   Fugui.Layouts.IsWindowInFloatingDockRoot(window);
        }

        /// <summary>
        /// Process Ctrl+Tab-style cycling between floating Fugui windows in this container.
        /// </summary>
        private void ProcessFloatingWindowSwitcherInput()
        {
            if (Keyboard == null)
            {
                return;
            }

            bool ctrlPressed = Keyboard.KeyCtrl;
            bool tabDown = Keyboard.GetKeyDown(FuKeysCode.Tab);
            if (ctrlPressed && tabDown)
            {
                if (_floatingWindowSwitcherOpen)
                {
                    CycleFloatingWindowSwitcher(Keyboard.KeyShift);
                }
                else
                {
                    BeginFloatingWindowSwitcher(Keyboard.KeyShift);
                }
                return;
            }

            if (!_floatingWindowSwitcherOpen)
            {
                return;
            }

            if (Keyboard.GetKeyDown(FuKeysCode.Escape))
            {
                CancelFloatingWindowSwitcher();
                return;
            }

            if (!ctrlPressed)
            {
                ActivateSelectedFloatingWindow();
                CloseFloatingWindowSwitcher();
            }
        }

        /// <summary>
        /// Start a floating window switch gesture.
        /// </summary>
        private void BeginFloatingWindowSwitcher(bool reverse)
        {
            RebuildFloatingWindowSwitchOrder();
            if (_floatingWindowSwitchOrder.Count == 0)
            {
                CloseFloatingWindowSwitcher();
                return;
            }

            _floatingWindowSwitchOriginId = GetActiveFloatingWindowSwitchCandidateId();
            _floatingWindowSwitchIndex = _floatingWindowSwitchOrder.IndexOf(_floatingWindowSwitchOriginId);
            if (_floatingWindowSwitchIndex < 0)
            {
                _floatingWindowSwitchIndex = _floatingWindowSwitchOrder.Count - 1;
            }

            if (_floatingWindowSwitchOrder.Count == 1)
            {
                ActivateSelectedFloatingWindow();
                CloseFloatingWindowSwitcher();
                return;
            }

            _floatingWindowSwitcherOpen = true;
            CycleFloatingWindowSwitcher(reverse);
        }

        /// <summary>
        /// Move the switcher selection to the next or previous candidate.
        /// </summary>
        private void CycleFloatingWindowSwitcher(bool reverse)
        {
            if (!RefreshFloatingWindowSwitchOrder())
            {
                CloseFloatingWindowSwitcher();
                return;
            }

            int direction = reverse ? 1 : -1;
            _floatingWindowSwitchIndex = WrapIndex(_floatingWindowSwitchIndex + direction, _floatingWindowSwitchOrder.Count);
            Fugui.ForceDrawAllWindows(2);
        }

        /// <summary>
        /// Activate the currently selected floating window.
        /// </summary>
        private void ActivateSelectedFloatingWindow()
        {
            if (_floatingWindowSwitchIndex < 0 ||
                _floatingWindowSwitchIndex >= _floatingWindowSwitchOrder.Count ||
                !Windows.TryGetValue(_floatingWindowSwitchOrder[_floatingWindowSwitchIndex], out FuWindow window))
            {
                return;
            }

            ActivateWindow(window);
        }

        /// <summary>
        /// Restore the original active window when the switch gesture is cancelled.
        /// </summary>
        private void CancelFloatingWindowSwitcher()
        {
            if (!string.IsNullOrEmpty(_floatingWindowSwitchOriginId) &&
                Windows.TryGetValue(_floatingWindowSwitchOriginId, out FuWindow window))
            {
                ActivateWindow(window);
            }

            CloseFloatingWindowSwitcher();
        }

        /// <summary>
        /// End the current floating window switch gesture.
        /// </summary>
        private void CloseFloatingWindowSwitcher()
        {
            _floatingWindowSwitcherOpen = false;
            _floatingWindowSwitchOrder.Clear();
            _floatingWindowSwitchIndex = -1;
            _floatingWindowSwitchOriginId = null;
        }

        /// <summary>
        /// Bring a switch candidate to the front and request ImGui focus.
        /// </summary>
        internal void ActivateWindow(FuWindow window)
        {
            if (window == null || window.Container != this)
            {
                return;
            }

            if (window.IsDocked)
            {
                if (Fugui.Layouts != null && Fugui.Layouts.ActivateDockedWindow(window, this))
                {
                    return;
                }

                return;
            }

            BringWindowToFront(window);
            ImGui.SetWindowFocus(window.ID);
            window.ForceFocusOnNextFrame();
            window.ForceDraw(2);
            Fugui.ForceDrawAllWindows(2);
        }

        /// <summary>
        /// Rebuild candidate order while preserving the selected window when possible.
        /// </summary>
        private bool RefreshFloatingWindowSwitchOrder()
        {
            string selectedId = _floatingWindowSwitchIndex >= 0 && _floatingWindowSwitchIndex < _floatingWindowSwitchOrder.Count
                ? _floatingWindowSwitchOrder[_floatingWindowSwitchIndex]
                : null;
            RebuildFloatingWindowSwitchOrder();
            if (_floatingWindowSwitchOrder.Count == 0)
            {
                return false;
            }

            int selectedIndex = !string.IsNullOrEmpty(selectedId) ? _floatingWindowSwitchOrder.IndexOf(selectedId) : -1;
            _floatingWindowSwitchIndex = selectedIndex >= 0
                ? selectedIndex
                : Mathf.Clamp(_floatingWindowSwitchIndex, 0, _floatingWindowSwitchOrder.Count - 1);
            return true;
        }

        /// <summary>
        /// Collect floating windows in bottom-to-top render order.
        /// </summary>
        private void RebuildFloatingWindowSwitchOrder()
        {
            _floatingWindowSwitchOrder.Clear();
            foreach (FuWindow window in Windows.Values)
            {
                if (IsFloatingWindowSwitchCandidate(window))
                {
                    _floatingWindowSwitchOrder.Add(window.ID);
                }
            }
        }

        /// <summary>
        /// Return whether a window participates in the floating window switcher.
        /// </summary>
        private bool IsFloatingWindowSwitchCandidate(FuWindow window)
        {
            if (window == null ||
                window.Container != this ||
                window.Is3DWindow ||
                !window.IsInitialized ||
                !window.IsOpened ||
                !window.IsInterractable)
            {
                return false;
            }

            return !window.IsDocked || IsFloatingDockGroupWindow(window);
        }

        /// <summary>
        /// Return the currently active floating candidate, falling back to the topmost candidate.
        /// </summary>
        private string GetActiveFloatingWindowSwitchCandidateId()
        {
            FuWindow inputFocusedWindow = FuWindow.InputFocusedWindow;
            if (inputFocusedWindow != null && _floatingWindowSwitchOrder.Contains(inputFocusedWindow.ID))
            {
                return inputFocusedWindow.ID;
            }

            for (int i = _floatingWindowSwitchOrder.Count - 1; i >= 0; i--)
            {
                string windowId = _floatingWindowSwitchOrder[i];
                if (Windows.TryGetValue(windowId, out FuWindow window) && window.HasFocus)
                {
                    return windowId;
                }
            }

            return _floatingWindowSwitchOrder.Count > 0 ? _floatingWindowSwitchOrder[_floatingWindowSwitchOrder.Count - 1] : null;
        }

        /// <summary>
        /// Draw the Ctrl+Tab floating window switcher overlay.
        /// </summary>
        private void DrawFloatingWindowSwitcherOverlay()
        {
            if (!_floatingWindowSwitcherOpen)
            {
                return;
            }
            if (!RefreshFloatingWindowSwitchOrder())
            {
                CloseFloatingWindowSwitcher();
                return;
            }

            float scale = Context != null ? Context.Scale : Fugui.Scale;
            float screenMargin = Mathf.Max(16f, 16f * scale);
            float width = Mathf.Min(Mathf.Max(320f, 360f * scale), Mathf.Max(160f, Size.x - screenMargin * 2f));
            float rowHeight = Mathf.Max(30f, 34f * scale);
            float padding = Mathf.Max(8f, 8f * scale);
            float maxHeight = Mathf.Max(rowHeight + padding * 2f, Size.y - screenMargin * 2f);
            float height = Mathf.Min(maxHeight, padding * 2f + rowHeight * _floatingWindowSwitchOrder.Count);
            Vector2 overlayPos = new Vector2(Size.x * 0.5f, Mathf.Max(screenMargin, Size.y * 0.18f));

            ImGui.SetNextWindowPos(overlayPos, ImGuiCond.Always, new Vector2(0.5f, 0f));
            ImGui.SetNextWindowSize(new Vector2(width, height), ImGuiCond.Always);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, Mathf.Max(4f, 5f * scale));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(padding, padding));
            ImGui.PushStyleColor(ImGuiCol.WindowBg, Fugui.Themes.GetColor(FuColors.PopupBg, 0.96f));
            ImGui.PushStyleColor(ImGuiCol.Border, Fugui.Themes.GetColor(FuColors.Border, 0.85f));

            ImGuiWindowFlags flags =
                ImGuiWindowFlags.NoDecoration |
                ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoSavedSettings |
                ImGuiWindowFlags.NoFocusOnAppearing |
                ImGuiWindowFlags.NoBringToFrontOnFocus |
                ImGuiWindowFlags.NoNav |
                ImGuiWindowFlags.NoDocking |
                ImGuiWindowFlags.NoMouseInputs;

            if (ImGui.Begin("FuguiFloatingWindowSwitcher", flags))
            {
                for (int i = 0; i < _floatingWindowSwitchOrder.Count; i++)
                {
                    if (!Windows.TryGetValue(_floatingWindowSwitchOrder[i], out FuWindow window))
                    {
                        continue;
                    }

                    bool selected = i == _floatingWindowSwitchIndex;
                    DrawFloatingWindowSwitcherRow(window, selected, rowHeight, scale);
                    if (selected)
                    {
                        ImGui.SetScrollHereY(0.5f);
                    }
                }
            }

            ImGui.End();
            ImGui.PopStyleColor(2);
            ImGui.PopStyleVar(2);
        }

        /// <summary>
        /// Draw a single switcher row.
        /// </summary>
        private void DrawFloatingWindowSwitcherRow(FuWindow window, bool selected, float rowHeight, float scale)
        {
            Vector2 rowMin = ImGui.GetCursorScreenPos();
            Vector2 rowSize = new Vector2(Mathf.Max(1f, ImGui.GetContentRegionAvail().x), rowHeight);
            Vector2 rowMax = rowMin + rowSize;
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            uint rowColor = selected
                ? Fugui.Themes.GetColorU32(FuColors.HeaderActive, 0.95f)
                : Fugui.Themes.GetColorU32(FuColors.FrameBg, 0.82f);
            uint textColor = Fugui.Themes.GetColorU32(selected ? FuColors.HighlightText : FuColors.Text);
            float rounding = Mathf.Max(3f, 4f * scale);

            drawList.AddRectFilled(rowMin, rowMax, rowColor, rounding);

            string label = Fugui.GetUntagedText(window.WindowName.Name);
            Vector2 textSize = ImGui.CalcTextSize(label);
            Vector2 textPos = rowMin + new Vector2(Mathf.Max(10f, 10f * scale), (rowHeight - textSize.y) * 0.5f);
            drawList.PushClipRect(rowMin, rowMax, true);
            drawList.AddText(textPos, textColor, label);
            drawList.PopClipRect();

            ImGui.Dummy(rowSize);
            ImGui.Dummy(new Vector2(1f, Mathf.Max(2f, 2f * scale)));
        }

        /// <summary>
        /// Wrap an index into a collection count.
        /// </summary>
        private static int WrapIndex(int index, int count)
        {
            if (count <= 0)
            {
                return -1;
            }

            index %= count;
            if (index < 0)
            {
                index += count;
            }
            return index;
        }

        /// <summary>
        /// Queue a floating window to be rendered above the other floating windows.
        /// </summary>
        internal void BringWindowToFront(FuWindow window)
        {
            if (window == null || window.IsDocked || !Windows.ContainsKey(window.ID))
            {
                return;
            }

            if (_isRenderingWindows)
            {
                QueuePendingWindowOrder(window.ID);
                return;
            }

            MoveWindowToEnd(window.ID);
        }

        /// <summary>
        /// Move a set of windows to the top of the render order while preserving their relative order.
        /// </summary>
        internal void BringWindowsToFront(IEnumerable<string> windowIds)
        {
            if (windowIds == null)
            {
                return;
            }

            if (_isRenderingWindows)
            {
                QueuePendingWindowOrder(windowIds);
                return;
            }

            foreach (string windowId in windowIds)
            {
                MoveWindowToEnd(windowId);
            }
        }

        /// <summary>
        /// Queue a window id to move after the current render pass.
        /// </summary>
        private void QueuePendingWindowOrder(string windowId)
        {
            if (!string.IsNullOrEmpty(windowId) && Windows.ContainsKey(windowId))
            {
                _pendingBringToFrontWindowIds.Add(windowId);
            }
        }

        /// <summary>
        /// Queue window ids to move after the current render pass.
        /// </summary>
        private void QueuePendingWindowOrder(IEnumerable<string> windowIds)
        {
            if (windowIds == null)
            {
                return;
            }

            foreach (string windowId in windowIds)
            {
                QueuePendingWindowOrder(windowId);
            }
        }

        /// <summary>
        /// Move queued windows to the end of the dictionary insertion order.
        /// </summary>
        private void ApplyPendingWindowOrder()
        {
            if (_pendingBringToFrontWindowIds.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _pendingBringToFrontWindowIds.Count; i++)
            {
                MoveWindowToEnd(_pendingBringToFrontWindowIds[i]);
            }

            _pendingBringToFrontWindowIds.Clear();
        }

        /// <summary>
        /// Move a window to the end of the dictionary insertion order.
        /// </summary>
        private void MoveWindowToEnd(string windowId)
        {
            if (!Windows.TryGetValue(windowId, out FuWindow window))
            {
                return;
            }

            Windows.Remove(windowId);
            Windows.Add(windowId, window);
        }

        /// <summary>
        /// Try to add a window in this container
        /// </summary>
        /// <param name="UIWindow">the window to add</param>
        /// <returns>true if success (not already in it)</returns>
        public bool TryAddWindow(FuWindow UIWindow)
        {
            if (Windows.ContainsKey(UIWindow.ID))
            {
                return false;
            }
            _toAddWindows.Enqueue(UIWindow);
            return true;
        }

        /// <summary>
        /// Whenever a window is closed
        /// </summary>
        /// <param name="UIWindow"></param>
        private void UIWindow_OnClose(FuWindow UIWindow)
        {
            TryRemoveWindow(UIWindow.ID);
        }

        /// <summary>
        /// Try to remove a window from this container
        /// </summary>
        /// <param name="id">ID of the window to remove</param>
        /// <returns>true if success (contained)</returns>
        public bool TryRemoveWindow(string id)
        {
            if (!Windows.ContainsKey(id))
            {
                return false;
            }
            _toRemoveWindows.Enqueue(Windows[id]);
            return true;
        }

        /// <summary>
        /// Whatever this container must force the position of it's windows
        /// </summary>
        /// <returns>true if force</returns>
        public bool ForcePos()
        {
            return false;
        }

        /// <summary>
        /// Draw the Main Layout of the container, including main menu and footer.
        /// </summary>
        /// <summary>
        /// Draws the main layout of the container, including the main menu and footer.
        /// </summary>
        private void DrawMainLayout()
        {
            uint viewPortID = 0;

            ImGuiWindowFlags window_flags =
                ImGuiWindowFlags.NoDocking |
                ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.NoCollapse |
                ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoBringToFrontOnFocus |
                ImGuiWindowFlags.NoNavFocus;

            float mainMenuHeight = 0f;

            if (Fugui.RenderMainMenu())
            {
                mainMenuHeight = 24f * Context.Scale;

                ImGui.GetBackgroundDrawList().AddLine(
                    new Vector2(0f, mainMenuHeight - Context.Scale),
                    new Vector2(_size.x, mainMenuHeight - Context.Scale),
                    ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.HeaderHovered)));
            }

            float footerHeight = Mathf.Max(0f, _footerHeight * Context.Scale);
            ContentRect = new Rect(0f, mainMenuHeight, _size.x, Mathf.Max(1f, _size.y - mainMenuHeight - footerHeight));

            if (_footerHeight > 0f)
            {
                Fugui.Push(ImGuiStyleVar.WindowRounding, 0.0f);
                Fugui.Push(ImGuiStyleVar.WindowBorderSize, 0.0f);
                Fugui.Push(ImGuiStyleVar.ItemSpacing, Vector2.zero);
                Fugui.Push(ImGuiStyleVar.ItemInnerSpacing, Vector2.zero);
                Fugui.Push(ImGuiStyleVar.WindowPadding, Vector2.zero);
                Fugui.Push(ImGuiCol.WindowBg, Fugui.Themes.GetColor(FuColors.MenuBarBg));

                ImGui.SetNextWindowPos(new Vector2(0f, _size.y - (_footerHeight * Context.Scale)));
                ImGui.SetNextWindowSize(new Vector2(_size.x, _footerHeight * Context.Scale));
                ImGui.SetNextWindowViewport(viewPortID);

                if (ImGui.Begin("FuguiFooter", window_flags))
                {
                    _footerUI?.Invoke();
                }

                ImGui.End();

                Fugui.PopColor();
                Fugui.PopStyle(5);
            }
        }
    }

#if UNITY_EDITOR
    public static class GameViewUtils
    {
        private static EditorWindow gameView = null;

        private static void Init()
        {
            // Get GameView type
            var gameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            if (gameViewType == null) return;
            // Find GameView instance
            gameView = EditorWindow.GetWindow(gameViewType);
        }

        public static Vector2Int GetPos()
        {
            if (gameView == null) Init();
            if (gameView != null)
            {
                return new Vector2Int((int)gameView.position.x, (int)gameView.position.y);
            }
            return Vector2Int.zero;
        }
    }
#endif
}
