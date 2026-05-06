#if FU_EXTERNALIZATION
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Container used by an external ImGui context.
    /// It renders FuWindow instances in a separate native GL context without running Fugui docking.
    /// </summary>
    public class FuExternalWindowContainer : IFuWindowContainer
    {
        public Vector2Int LocalMousePos => _mousePos;
        public FuContext Context => _context;
        public Vector2Int Position => _context.Window.Position;
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

        private FuWindow _window;

        private Vector2Int _mousePos;
        private Vector2Int _size;

        public event Action OnPostRenderWindows;

        public FuExternalWindowContainer(FuWindow window, FuExternalContext context)
        {
            _context = context;

            Windows = new Dictionary<string, FuWindow>();
            _mouse = new FuMouseState();
            _keyboard = new FuKeyboardState(_context.IO);
            _size = window.Size;
            _context.OnPrepareFrame += context_OnPrepareFrame;
            _context.OnRender += RenderFuWindows;

            Vector2Int absMousePos = Fugui.GetGlobalMousePosition();
            Vector2Int winContainerMousePos = window.Container.LocalMousePos;
            Vector2Int absContainerPos = absMousePos - winContainerMousePos;
            Vector2Int sourceLocalPosition = window.LocalPosition;
            Vector2Int initialWindowPosition = absContainerPos + sourceLocalPosition;
            Vector2Int dragStartMouseOffset = absMousePos - initialWindowPosition;

            _context.Window.Position = initialWindowPosition;

            TryAddWindow(window);

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
            ApplyPendingWindowOrder();
            _context.Window.UpdateManipulation();
            SyncPrimaryWindowToNativeBounds();

            if (_window == null || !_window.IsExternal)
            {
                _context.Window.Render();
                return;
            }

            foreach (FuWindow window in Windows.Values.ToList())
            {
                RenderFuWindow(window);
            }

            _context.Window.Render();
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

            _window.LocalPosition = Vector2Int.zero;
            _window.Size = _size;
        }

        /// <summary>
        /// Render a FuWindow within this container.
        /// </summary>
        /// <param name="window"> The FuWindow to render. </param>
        public void RenderFuWindow(FuWindow window)
        {
            if (window == null)
                return;

            window.UpdateState(Context.IO.MouseDown[0]);
            // during window manipulation, we block fugui mouse events to avoid conflicts
            if (_context.Window.IsResizing)
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
            if (window.IsDocked)
            {
                Debug.LogWarning($"Cannot add docked window {window.ID} to an external Fugui container.");
                return false;
            }
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
            window.LocalPosition = worldPosition - Position;
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
                    ((FuExternalContext)_context).Window.SetWindow(_window);
                }
            }
            if (window.Container == this)
            {
                window.Container = null;
            }
            return true;
        }

        public bool ForcePos() => true;

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
            _context.Window.Close(onClosed);
        }
    }
}
#endif
