#if FU_EXTERNALIZATION
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Container used by an external ImGui context.
    /// It manages one or several FuWindow instances in a separate GL context.
    /// </summary>
    public class FuExternalWindowContainer : IFuWindowContainer
    {
        public Vector2Int LocalMousePos => _mousePos;
        public FuContext Context => _context;
        public Vector2Int Position => NativeWindow?.Position ?? Vector2Int.zero;
        public Vector2Int Size => _size;
        public FuKeyboardState Keyboard => _keyboard;
        public FuMouseState Mouse => _mouse;
        public FuContainerScaleConfig ContainerScaleConfig => _context.ContainerScaleConfig;
        public FuWindow Window => _window;
        public Dictionary<string, FuWindow> Windows { get; private set; }

        private readonly FuExternalContext _context;
        private readonly FuMouseState _mouse;
        private readonly FuKeyboardState _keyboard;
        private readonly List<string> _pendingBringToFrontWindowIds = new List<string>();
        private readonly List<FuWindow> _renderWindowsSnapshot = new List<FuWindow>();

        private FuWindow _window;

        private Vector2Int _mousePos;
        private Vector2Int _size;
        private FuExternalWindow NativeWindow => _context?.Window;

        public event Action OnPostRenderWindows;

        public FuExternalWindowContainer(FuWindow window, FuExternalContext context, IReadOnlyList<FuWindow> windows = null, Rect? externalizationRect = null)
        {
            _context = context;

            Windows = new Dictionary<string, FuWindow>();
            _mouse = new FuMouseState();
            _keyboard = new FuKeyboardState(_context.IO);
            _size = externalizationRect.HasValue
                ? new Vector2Int(Mathf.Max(1, Mathf.RoundToInt(externalizationRect.Value.width)), Mathf.Max(1, Mathf.RoundToInt(externalizationRect.Value.height)))
                : window.Size;
            _context.OnPrepareFrame += context_OnPrepareFrame;
            _context.OnRender += RenderFuWindows;

            Vector2Int absMousePos = Fugui.GetGlobalMousePosition();
            Vector2Int winContainerMousePos = window.Container.LocalMousePos;
            Vector2Int absContainerPos = absMousePos - winContainerMousePos;
            Vector2Int sourceLocalPosition = externalizationRect.HasValue
                ? new Vector2Int(Mathf.RoundToInt(externalizationRect.Value.x), Mathf.RoundToInt(externalizationRect.Value.y))
                : window.LocalPosition;
            Vector2Int initialWindowPosition = absContainerPos + sourceLocalPosition;
            Vector2Int dragStartMouseOffset = absMousePos - initialWindowPosition;

            _context.Window.Position = initialWindowPosition;

            if (externalizationRect.HasValue)
            {
                window.Size = _size;
            }

            IReadOnlyList<FuWindow> windowsToAdd = windows != null && windows.Count > 0 ? windows : new List<FuWindow> { window };
            for (int i = 0; i < windowsToAdd.Count; i++)
            {
                TryAddWindow(windowsToAdd[i]);
            }

            _context.Window.SetNativeSize(_size);
            _context.Window.Create(window.IsDragging, dragStartMouseOffset);
        }

        /// <summary>
        /// Update the input states for mouse, keyboard, and window.
        /// </summary>
        public void Update()
        {
            Vector2Int contextSize = new Vector2Int(_context.Width, _context.Height);
            if (contextSize.x > 0 && contextSize.y > 0 && contextSize != _size)
            {
                _size = contextSize;
            }

            Vector2 mousePos = Context.IO.MousePos;
            bool leftMousePressed = Context.IO.MouseDown[0];
            _mousePos = new Vector2Int((int)mousePos.x, (int)mousePos.y);
            _mouse.UpdateState(this);
            _keyboard.UpdateState();
        }

        private bool context_OnPrepareFrame()
        {
            Vector2Int contextSize = new Vector2Int(_context.Width, _context.Height);
            if (contextSize.x > 0 && contextSize.y > 0)
            {
                _size = contextSize;
            }

            _context.UpdateContainerScale(_size);
            return true;
        }

        /// <summary>
        /// Render all FuWindows within this container.
        /// </summary>
        public void RenderFuWindows()
        {
            FuExternalWindow nativeWindow = NativeWindow;
            if (nativeWindow == null)
            {
                return;
            }

            ApplyPendingWindowOrder();
            nativeWindow.UpdateManipulation();
            Fugui.Layouts?.UpdateCustomLayout(this, new Rect(Vector2.zero, Size));
            SyncPrimaryWindowToNativeBounds();

            if (_window == null || !_window.IsExternal)
            {
                nativeWindow.Render();
                return;
            }

            List<FuWindow> windowsSnapshot = GetRenderWindowSnapshot();
            for (int i = 0; i < windowsSnapshot.Count; i++)
            {
                FuWindow window = windowsSnapshot[i];
                if (window.IsDocked && !(Fugui.Layouts?.IsWindowInFloatingDockRoot(window) ?? false))
                {
                    if (Fugui.Layouts?.RenderDockspaceForWindow(window, nativeWindow.IsResizing, nativeWindow.IsResizing) ?? false)
                    {
                        continue;
                    }
                    RenderFuWindow(window);
                }
            }

            windowsSnapshot = GetRenderWindowSnapshot();
            for (int i = 0; i < windowsSnapshot.Count; i++)
            {
                FuWindow window = windowsSnapshot[i];
                bool floatingDockWindow = window.IsDocked && (Fugui.Layouts?.IsWindowInFloatingDockRoot(window) ?? false);
                if ((!window.IsDocked && !window.IsDragging) || floatingDockWindow)
                {
                    if (window.IsDocked && (Fugui.Layouts?.RenderDockspaceForWindow(window, nativeWindow.IsResizing, nativeWindow.IsResizing) ?? false))
                    {
                        continue;
                    }
                    RenderFuWindow(window);
                }
            }

            windowsSnapshot = GetRenderWindowSnapshot();
            for (int i = 0; i < windowsSnapshot.Count; i++)
            {
                FuWindow window = windowsSnapshot[i];
                if (!window.IsDocked && window.IsDragging)
                {
                    RenderFuWindow(window);
                }
            }

            Fugui.Layouts?.DrawExternalWindowDockPreview(this);
            nativeWindow.Render();
            FuWindow primaryWindow = _window;
            if (primaryWindow == null)
            {
                return;
            }

            // render notifications
            if (!primaryWindow.NoContextMenu)
                Fugui.RenderContextMenu();

            // render modal
            if (!primaryWindow.NoModal)
                Fugui.RenderModal(this);

            // render popup message
            Fugui.RenderPopupMessage();

            // render notifications
            if (!primaryWindow.NoNotify)
                Fugui.RenderNotifications(this);

            OnPostRenderWindows?.Invoke();
        }

        private void SyncPrimaryWindowToNativeBounds()
        {
            if (_window == null)
            {
                return;
            }

            if (Windows.Count > 1)
            {
                return;
            }

            bool floatingDockRoot = _window.IsDocked && (Fugui.Layouts?.IsWindowInFloatingDockRoot(_window) ?? false);
            if (floatingDockRoot)
            {
                return;
            }

            _window.LocalPosition = Vector2Int.zero;
            _window.Size = _size;
        }

        internal void SetNativeBounds(Vector2Int position, Vector2Int size)
        {
            FuExternalWindow nativeWindow = NativeWindow;
            if (nativeWindow == null)
            {
                return;
            }

            size = new Vector2Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y));
            nativeWindow.SetNativeBounds(position, size);
            _size = new Vector2Int(Mathf.Max(1, nativeWindow.Width), Mathf.Max(1, nativeWindow.Height));
            _context.UpdateContainerScale(_size);
        }

        /// <summary>
        /// Render a FuWindow within this container.
        /// </summary>
        /// <param name="window"> The FuWindow to render. </param>
        public void RenderFuWindow(FuWindow window)
        {
            if (!OwnsWindow(window))
                return;

            FuExternalWindow nativeWindow = NativeWindow;
            if (nativeWindow == null)
                return;

            window.UpdateState(Context.IO.MouseDown[0]);
            // during window manipulation, we block fugui mouse events to avoid conflicts
            if (nativeWindow.IsResizing)
            {
                // to block fugui mouse events, we first need to update mouse state so manipulations update have current inputs
                // then we clear mouse events before drawing
                window.Mouse.UpdateState(window);
                window.Mouse.Clear();
                window.DrawWindow(true, true);
            }
            else
            {
                window.DrawWindow();
            }
        }

        public void OnEachWindow(Action<FuWindow> callback)
        {
            foreach (FuWindow window in Windows.Values)
            {
                callback?.Invoke(window);
            }
        }

        public bool HasWindow(string id) => !string.IsNullOrEmpty(id) && Windows.ContainsKey(id);

        internal bool IsNativeChromeOwner(FuWindow window)
        {
            return window != null && window == _window;
        }

        public bool TryAddWindow(FuWindow window)
        {
            if (window == null)
                return false;
            if (Windows.ContainsKey(window.ID))
                return false;

            Vector2Int worldPosition = window.Container != null ? window.WorldPosition : Position + window.LocalPosition;
            if (window.Container != null && !window.TryRemoveFromContainer())
            {
                return false;
            }

            if (_window == null)
            {
                _window = window;
            }

            Windows.Add(window.ID, window);
            window.Container = this;
            window.IsExternal = true;
            if (!window.IsDocked)
            {
                window.LocalPosition = worldPosition - Position;
            }
            window.InitializeOnContainer();
#if FU_EXTERNALIZATION
            Vector2Int handoffMousePos = Fugui.GetGlobalMousePosition() - Position;
            bool handoffLeftMousePressed = Fugui.IsGlobalMouseButtonPressed(FuMouseButton.Left);
            window.TryBeginPendingInternalizedDrag(handoffMousePos, handoffLeftMousePressed);
#endif
            return true;
        }

        public bool TryRemoveWindow(string id)
        {
            if (string.IsNullOrEmpty(id) || !Windows.TryGetValue(id, out FuWindow window))
            {
                return false;
            }

            Windows.Remove(id);
            window.IsExternal = false;
            if (_window == window)
            {
                _window = Windows.Count > 0 ? Windows.Values.First() : null;
                if (_window != null)
                {
                    NativeWindow?.SetWindow(_window);
                }
            }
            if (window.Container == this)
            {
                window.Container = null;
            }
            return true;
        }

        public bool ForcePos() => true;

        private List<FuWindow> GetRenderWindowSnapshot()
        {
            _renderWindowsSnapshot.Clear();
            foreach (FuWindow window in Windows.Values)
            {
                _renderWindowsSnapshot.Add(window);
            }

            return _renderWindowsSnapshot;
        }

        private bool OwnsWindow(FuWindow window)
        {
            return window != null &&
                   window.Container == this &&
                   !string.IsNullOrEmpty(window.ID) &&
                   Windows.ContainsKey(window.ID);
        }

        internal void BringWindowsToFront(IEnumerable<string> windowIds)
        {
            if (windowIds == null)
            {
                return;
            }

            foreach (string windowId in windowIds)
            {
                if (!string.IsNullOrEmpty(windowId) && Windows.ContainsKey(windowId))
                {
                    _pendingBringToFrontWindowIds.Add(windowId);
                }
            }
        }

        private void ApplyPendingWindowOrder()
        {
            if (_pendingBringToFrontWindowIds.Count == 0)
            {
                return;
            }

            foreach (string windowId in _pendingBringToFrontWindowIds)
            {
                MoveWindowToEnd(windowId);
            }

            _pendingBringToFrontWindowIds.Clear();
        }

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
        /// Configure how this container scales its context.
        /// </summary>
        /// <param name="config">Scale configuration.</param>
        public void SetContainerScaleConfig(FuContainerScaleConfig config)
        {
            _context.SetContainerScaleConfig(config, _size);
        }

        /// <summary>
        /// Close the external window container and its associated window.
        /// </summary>
        /// <param name="onClosed"> Optional callback to execute after the window is closed. </param>
        public void Close(Action onClosed = null)
        {
            _context.OnPrepareFrame -= context_OnPrepareFrame;
            _context.OnRender -= RenderFuWindows;
            FuExternalWindow nativeWindow = NativeWindow;
            if (nativeWindow == null)
            {
                onClosed?.Invoke();
                return;
            }

            nativeWindow.Close(onClosed);
        }
    }
}
#endif
