using System;
using System.Collections.Generic;
using UnityEngine;
using ImGuiNET;
using Fu.Framework;

namespace Fu
{
    /// <summary>
    /// A class representing the main UI container.
    /// </summary>
    public class FuMainWindowContainer : IFuWindowContainer
    {
        #region Variables
        /// <summary>
        /// The local mouse position relative to the container.
        /// </summary>
        public Vector2Int LocalMousePos => _mousePos;
        /// <summary>
        /// The related FuContext of this Container.
        /// </summary>
        public FuContext Context => _fuguiContext;
        /// <summary>
        /// The position of the container in world space.
        /// </summary>
        public Vector2Int Position => _worldPosition;
        /// <summary>
        /// The size of the container.
        /// </summary>
        public Vector2Int Size => _size;
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
        // The mouse position relative to the container.
        private Vector2Int _mousePos;
        // The size of the container.
        private Vector2Int _size;
        // current Fugui context
        private FuUnityContext _fuguiContext;
        #endregion

        /// <summary>
        /// Constructs a new instance of the MainUIContainer class.
        /// </summary>
        /// <param name="FuguiContext">Fugui Context that draw this container</param>
        public FuMainWindowContainer(FuUnityContext FuguiContext)
        {
            // set curretn Fugui context
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
        }

        private void _fuguiContext_OnRender()
        {
            RenderFuWindows();
        }

        public void Update()
        {
            // update mouse state
            _fuMouseState.UpdateState(this);
            _fuKeyboardState.UpdateState();

            // remove windows
            while (_toRemoveWindows.Count > 0)
            {
                FuWindow window = _toRemoveWindows.Dequeue();
                window.OnClosed -= UIWindow_OnClose;
                Windows.Remove(window.ID);
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

            // set size and pos for this frame
            _size = new Vector2Int(Screen.width, Screen.height);

            // get unity local mouse position
            Vector2Int newMousePos = new Vector2Int((int)Context.IO.MousePos.x, (int)Context.IO.MousePos.y);
            _mousePos = newMousePos;
            _worldPosition = Screen.mainWindowPosition;
        }

        #region Footer
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
        #endregion

        #region Container
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
            // we clamp size and position BEFORE drawing window to allow dev to set size and pos inside the drawing callback
            // prevent clamping if window is dragging to avoid clipping
            if (!window.IsDragging && !window.IsDocked && !window.IsResizing)
            {
                // clamp window size
                Vector2Int size = window.Size;
                if (size.x < (int)(64f * Fugui.CurrentContext.Scale))
                {
                    size.x = (int)(64f * Fugui.CurrentContext.Scale);
                }
                if (size.y < (int)(64f * Fugui.CurrentContext.Scale))
                {
                    size.y = (int)(64f * Fugui.CurrentContext.Scale);
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
                if (pos.y > Size.y - (int)(32f * Fugui.CurrentContext.Scale))
                {
                    pos.y = Size.y - (int)(64f * Fugui.CurrentContext.Scale);
                }
                if (pos.x > Size.x - (int)(32f * Fugui.CurrentContext.Scale))
                {
                    pos.x = Size.x - (int)(64f * Fugui.CurrentContext.Scale);
                }
                if (pos.x < -window.Size.x - (int)(32f * Fugui.CurrentContext.Scale))
                {
                    pos.x = -window.Size.x + (int)(64f * Fugui.CurrentContext.Scale);
                }
                if (pos.y < -window.Size.y - (int)(32f * Fugui.CurrentContext.Scale))
                {
                    pos.y = -window.Size.y + (int)(64f * Fugui.CurrentContext.Scale);
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
            DrawMainDockSpace();

            // render every windows into this container
            bool leftButtonState = ImGui.GetIO().MouseDown[0];
            foreach (FuWindow window in Windows.Values)
            {
                // update window state
                window.UpdateState(leftButtonState);
                // check whatever window must be draw
                RenderFuWindow(window);
            }

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

        #region UIWindow Events
        /// <summary>
        /// Whenever a window is closed
        /// </summary>
        /// <param name="UIWindow"></param>
        private void UIWindow_OnClose(FuWindow UIWindow)
        {
            TryRemoveWindow(UIWindow.ID);
        }
        #endregion

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
        /// get texture ID for current graphic context
        /// </summary>
        /// <param name="texture">texture to get id</param>
        /// <returns>graphic ID of the texture</returns>
        public IntPtr GetTextureID(Texture2D texture)
        {
            return _fuguiContext.TextureManager.GetTextureId(texture);
        }

        /// <summary>
        /// get texture ID for current graphic context
        /// </summary>
        /// <param name="texture">texture to get id</param>
        /// <returns>graphic ID of the texture</returns>
        public IntPtr GetTextureID(RenderTexture texture)
        {
            return _fuguiContext.TextureManager.GetTextureId(texture);
        }

        /// <summary>
        /// Draw ImGui Image regardless to GL context
        /// </summary>
        /// <param name="texture">renderTexture to draw</param>
        /// <param name="size">size of the image</param>
        public void ImGuiImage(RenderTexture texture, Vector2 size)
        {
            if (texture == null)
            {
                ImGui.Dummy(size);
                return;
            }
            ImGui.Image(GetTextureID(texture), size);
        }

        /// <summary>
        /// Draw ImGui Image regardless to GL context
        /// </summary>
        /// <param name="texture">renderTexture to draw</param>
        /// <param name="size">size of the image</param>
        public void ImGuiImage(Texture2D texture, Vector2 size)
        {
            if (texture == null)
            {
                ImGui.Dummy(size);
                return;
            }
            ImGui.Image(GetTextureID(texture), size);
        }

        /// <summary>
        /// Draw ImGui Image regardless to GL context
        /// </summary>
        /// <param name="texture">texture2D to draw</param>
        /// <param name="size">size of the image</param>
        /// <param name="color">tint color of the image</param>
        public void ImGuiImage(RenderTexture texture, Vector2 size, Vector4 color)
        {
            if (texture == null)
            {
                ImGui.Dummy(size);
                return;
            }
            ImGui.Image(GetTextureID(texture), size, Vector2.zero, Vector2.one, color);
        }

        /// <summary>
        /// Draw ImGui Image regardless to GL context
        /// </summary>
        /// <param name="texture">texture2D to draw</param>
        /// <param name="size">size of the image</param>
        /// <param name="color">tint color of the image</param>
        public void ImGuiImage(Texture2D texture, Vector2 size, Vector4 color)
        {
            if (texture == null)
            {
                ImGui.Dummy(size);
                return;
            }
            ImGui.Image(GetTextureID(texture), size, Vector2.zero, Vector2.one, color);
        }

        /// <summary>
        /// Draw ImGui ImageButton regardless to GL context
        /// </summary>
        /// <param name="texture">texture2D to draw</param>
        /// <param name="size">size of the image</param>
        /// <returns>true if clicked</returns>
        public bool ImGuiImageButton(Texture2D texture, Vector2 size)
        {
            if (texture == null)
            {
                ImGui.Dummy(size);
                return false;
            }
            return ImGui.ImageButton("", GetTextureID(texture), size);
        }

        /// <summary>
        /// Draw ImGui ImageButton regardless to GL context
        /// </summary>
        /// <param name="texture">texture2D to draw</param>
        /// <param name="size">size of the image</param>
        /// <param name="color">tint additive color of the image</param>
        /// <returns>true if clicked</returns>
        public bool ImGuiImageButton(Texture2D texture, Vector2 size, Vector4 color)
        {
            if (texture == null)
            {
                ImGui.Dummy(size);
                return false;
            }
            return ImGui.ImageButton("", GetTextureID(texture), size, Vector2.zero, Vector2.one, ImGui.GetStyle().Colors[(int)ImGuiCol.Button], color);
        }
        #endregion

        #region Docking
        /// <summary>
        /// Draw the Main Container DockSpace
        /// </summary>
        private void DrawMainDockSpace()
        {
            // draw main menu
            float mainMenuHeight = 0f;
            if (Fugui.RenderMainMenu())
            {
                mainMenuHeight = 24f * Context.Scale;
                // draw main menu separator
                ImGui.GetBackgroundDrawList().AddLine(new Vector2(0f, mainMenuHeight - Context.Scale), new Vector2(_size.x, mainMenuHeight - Context.Scale), ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.HeaderHovered)));
            }

            // draw main dockspace
            uint viewPortID = 0;
            ImGuiDockNodeFlags dockspace_flags = Fugui.Settings.DockingFlags;
            if (Fugui.Layouts.CurrentLayout != null && Fugui.Layouts.CurrentLayout.AutoHideTopBar)
            {
                dockspace_flags |= ImGuiDockNodeFlags.AutoHideTabBar;
            }
            ImGui.SetNextWindowPos(new Vector2(0f, mainMenuHeight));
            ImGui.SetNextWindowSize(new Vector2(_size.x, _size.y - mainMenuHeight - Mathf.Max(0f, _footerHeight * Context.Scale)));
            ImGui.SetNextWindowViewport(viewPortID);
            // We are using the UIWindowFlags_NoDocking flag to make the parent window not dockable into,
            // because it would be confusing to have two docking targets within each others.
            ImGuiWindowFlags window_flags = ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;

            // draw DockSpace main container
            if (ImGui.Begin("MainDockSpace", window_flags | ImGuiWindowFlags.NoBackground))
            {
                Dockspace_id = ImGui.GetID("DockSpace");
                ImGui.DockSpace(Dockspace_id, Vector2.zero, dockspace_flags);
                ImGui.End();
            }

            // draw footer
            if (_footerHeight > 0f)
            {
                Fugui.Push(ImGuiStyleVar.WindowRounding, 0.0f);
                Fugui.Push(ImGuiStyleVar.WindowBorderSize, 0.0f);
                Fugui.Push(ImGuiStyleVar.ItemSpacing, new Vector2(0f, 0f));
                Fugui.Push(ImGuiStyleVar.ItemInnerSpacing, new Vector2(0f, 0f));
                Fugui.Push(ImGuiStyleVar.WindowPadding, Vector2.zero);
                Fugui.Push(ImGuiCol.WindowBg, Fugui.Themes.GetColor(FuColors.MenuBarBg));
                ImGui.SetNextWindowPos(new Vector2(0f, _size.y - (_footerHeight * Context.Scale)));
                ImGui.SetNextWindowSize(new Vector2(_size.x, (_footerHeight * Context.Scale)));
                ImGui.SetNextWindowViewport(viewPortID);
                if (ImGui.Begin("FuguiFooter", window_flags))
                {
                    _footerUI?.Invoke();
                    ImGui.End();
                }
                Fugui.PopColor();
                Fugui.PopStyle(4);
            }
        }
        #endregion
    }
}