using ImGuiNET;
using System;
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
        public Vector2Int Position => _context.Window.Position;
        public Vector2Int Size => _size;
        public FuKeyboardState Keyboard => _keyboard;
        public FuMouseState Mouse => _mouse;
        public FuWindow Window => _window;

        private readonly FuExternalContext _context;
        private readonly FuMouseState _mouse;
        private readonly FuKeyboardState _keyboard;

        private FuWindow _window;

        private Vector2Int _mousePos;
        private Vector2Int _size;

        public event Action OnPostRenderWindows;

        public FuExternalWindowContainer(FuWindow window, FuExternalContext context)
        {
            _context = context;

            _mouse = new FuMouseState();
            _keyboard = new FuKeyboardState(_context.IO);
            _size = window.Size;
            _context.OnRender += RenderFuWindows;

            Vector2Int absMousePos = Fugui.AbsoluteMonitorMousePosition;
            Vector2Int winContainerMousePos = window.Container.LocalMousePos;
            Vector2Int absContainerPos = absMousePos - winContainerMousePos;

            _context.Window.Position = absContainerPos + window.LocalPosition;

            TryAddWindow(window);

            _window.OnResized += (window) =>
            {
                _size = window.Size;
            };

            _context.Window.Create(_window.IsDragging);
        }

        #region Update & Render
        /// <summary>
        /// Update the input states for mouse, keyboard, and window.
        /// </summary>
        public void Update()
        {
            Vector2 mousePos = Context.IO.MousePos;
            bool leftMousePressed = Context.IO.MouseDown[0];
            _mousePos = new Vector2Int((int)mousePos.x, (int)mousePos.y);
            _mouse.UpdateState(this);
            _keyboard.UpdateState();
        }

        /// <summary>
        /// Render all FuWindows within this container.
        /// </summary>
        public void RenderFuWindows()
        {
            // render each window
            RenderFuWindow(_window);

            if (_window == null || !_window.IsExternal)
                return;

            // render notifications
            if (!_window.NoContextMenu)
                Fugui.RenderContextMenu();

            // render modal
            if (!_window.NoModal)
                Fugui.RenderModal(this);

            // render popup message
            Fugui.RenderPopupMessage();

            // render notifications
            if (!_window.NoNotify)
                Fugui.RenderNotifications(this);

            OnPostRenderWindows?.Invoke();
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
                _context.Window.UpdateManipulation();
                window.Mouse.Clear();
                window.DrawWindow(true, true);
            }
            else
            {
                _context.Window.UpdateManipulation();
                window.DrawWindow();
            }
            _context.Window.Render();
        }
        #endregion

        #region Container Management
        public void OnEachWindow(Action<FuWindow> callback)
        {
            callback?.Invoke(_window);
        }

        public bool HasWindow(string id) => _window != null && _window.ID == id;

        public bool TryAddWindow(FuWindow window)
        {
            if (window == null)
                return false;
            if (_window != null && !_window.TryRemoveFromContainer())
            {
                return false;
            }

            if (window.Container != null && !window.TryRemoveFromContainer())
            {
                return false;
            }
            _window = window;
            _window.Container = this;
            _window.IsExternal = true;
            _window.LocalPosition = Vector2Int.zero;
            _window.InitializeOnContainer();
            return true;
        }

        public bool TryRemoveWindow(string id)
        {
            if (_window != null && _window.ID == id)
            {
                _window.IsExternal = false;
                _window = null;
                return true;
            }
            return false;
        }

        public bool ForcePos() => true;
        #endregion

        /// <summary>
        /// Close the external window container and its associated window.
        /// </summary>
        /// <param name="onClosed"> Optional callback to execute after the window is closed. </param>
        public void Close(Action onClosed = null)
        {
            _context.Window.Close(onClosed);
        }
    }
}