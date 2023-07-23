using System;
using System.Collections.Generic;
using UnityEngine;
using ImGuiNET;
using Fu.Framework;

namespace Fu.Core
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
        // get the scale of this container (fixed to 100% for now, must be DPi aware => just get DPI using context.IO and divide by 96 : not tested)
        public float Scale => 1f;
        public FuMouseState Mouse => _fuMouseState;
        public FuKeyboardState Keyboard => _fuKeyboardState;

        private FuMouseState _fuMouseState;
        private FuKeyboardState _fuKeyboardState;
        // the height of the footer ( <= 0 will hide footer)
        private float _footerHeight = -1f;
        // the UI callback of the footer
        private Action _footerUI = null;
        // The world position of the container.
        private Vector2Int _worldPosition;
        // A queue of windows to be externalized.
        private Queue<FuWindow> _toExternalizeWindows;
        // A queue of windows to be removed.
        private Queue<FuWindow> _toRemoveWindows;
        // A queue of windows to be added.
        private Queue<FuWindow> _toAddWindows;
        // A flag indicating whether externalization is allowed this frame.
        private bool _canExternalizeThisFrame = false;
        // The mouse position relative to the container.
        private Vector2Int _mousePos;
        // The mouse position in world space from the previous frame.
        private Vector2Int _lastFrameWorldMousePos;
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
            _toExternalizeWindows = new Queue<FuWindow>();
            _toRemoveWindows = new Queue<FuWindow>();
            _toAddWindows = new Queue<FuWindow>();

            // instantiate inputs states
            _fuMouseState = new FuMouseState();
            _fuKeyboardState = new FuKeyboardState(_fuguiContext.IO);

            // Subscribe to the Layout event of the given FuguiContext
            _fuguiContext.OnRender += _fuguiContext_OnRender;
            // Set the docking style color to current theme
            FuThemeManager.SetTheme(FuThemeManager.CurrentTheme);
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

            // externalize windows
            while (_toExternalizeWindows.Count > 0)
            {
                FuWindow window = _toExternalizeWindows.Dequeue();
                window.Externalize();
            }

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
            Vector2Int newMousePos = new Vector2Int((int)Input.mousePosition.x, _size.y - (int)Input.mousePosition.y);

            // world mouse has moved but local mouse don't move acordingly
            // let's ignore container pos to avoid glitching when lose focus if exeternal window are shown
            if (Fugui.WorldMousePosition != _lastFrameWorldMousePos && newMousePos == _mousePos)
            {
                return;
            }

            // all mouses has moved, let's update mouse and container pos
            _mousePos = newMousePos;
            _worldPosition = Fugui.WorldMousePosition - _mousePos;
            _lastFrameWorldMousePos = Fugui.WorldMousePosition;
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
        /// <param name="UIWindow"></param>
        public void RenderFuWindow(FuWindow UIWindow)
        {
            // Do draw window
            UIWindow.DrawWindow();
        }

        /// <summary>
        /// Render any Windows in this container
        /// </summary>
        public void RenderFuWindows()
        {
            DrawMainDockSpace();

            // whatever the user want to externalize a window this frame
            _canExternalizeThisFrame = Fugui.Settings.ExternalizationKey.Count == 0;
            foreach (KeyCode key in Fugui.Settings.ExternalizationKey)
            {
                if (Input.GetKey(key))
                {
                    _canExternalizeThisFrame = true;
                    break;
                }
            }

            // render every windows into this container
            bool leftButtonState = Input.GetMouseButton(0);
            foreach (FuWindow window in Windows.Values)
            {
                // update window state
                window.UpdateState(leftButtonState);
                // check whatever window must be draw
                RenderFuWindow(window);
                // add to externalize list
                if (_canExternalizeThisFrame && window.WantToLeave())
                {
                    _toExternalizeWindows.Enqueue(window);
                }
            }

            // render notifications
            Fugui.RenderContextMenu();

            // render notifications
            Fugui.RenderNotifications(this);

            // render modal
            Fugui.RenderModal(this);

            // render popup message
            Fugui.RenderPopupMessage();
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
            Fugui.RenderMainMenu();
            float mainMenuHeight = 24f;
            // draw main menu separator
            ImGui.GetBackgroundDrawList().AddLine(new Vector2(0f, mainMenuHeight - 1f), new Vector2(_size.x, mainMenuHeight - 1f), ImGui.GetColorU32(FuThemeManager.GetColor(FuColors.HeaderHovered)));

            // draw main dockspace
            uint viewPortID = 0;
            ImGuiDockNodeFlags dockspace_flags = Fugui.Settings.DockingFlags;
            ImGui.SetNextWindowPos(new Vector2(0f, mainMenuHeight));
            ImGui.SetNextWindowSize(new Vector2(_size.x, _size.y - mainMenuHeight - Mathf.Max(0f, _footerHeight)));
            ImGui.SetNextWindowViewport(viewPortID);
            Fugui.Push(ImGuiStyleVar.WindowRounding, 0.0f);
            Fugui.Push(ImGuiStyleVar.WindowBorderSize, 0.0f);
            Fugui.Push(ImGuiStyleVar.ItemSpacing, new Vector2(0f, 0f));
            Fugui.Push(ImGuiStyleVar.ItemInnerSpacing, new Vector2(0f, 0f));
            Fugui.Push(ImGuiStyleVar.WindowPadding, Vector2.zero);
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
                Fugui.Push(ImGuiCol.WindowBg, FuThemeManager.GetColor(FuColors.MenuBarBg));
                ImGui.SetNextWindowPos(new Vector2(0f, _size.y - _footerHeight));
                ImGui.SetNextWindowSize(new Vector2(_size.x, _footerHeight));
                ImGui.SetNextWindowViewport(viewPortID);
                if (ImGui.Begin("FuguiFooter", window_flags))
                {
                    _footerUI?.Invoke();
                    ImGui.End();
                }
                Fugui.PopColor();
            }
            Fugui.PopStyle(5);
        }
        #endregion
    }
}