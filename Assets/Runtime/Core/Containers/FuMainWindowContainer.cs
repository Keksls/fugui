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
        public float Scale => 1f;

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

            // Subscribe to the Layout event of the given FuguiContext
            _fuguiContext.OnRender += _fuguiContext_OnRender;
            // Set the docking style color to current theme
            FuThemeManager.SetTheme(FuThemeManager.CurrentTheme);
        }

        private void _fuguiContext_OnRender()
        {
            RenderUIWindows();
        }

        public void Update()
        {
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
                window.Fire_OnRemovedFromContainer();
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
                window.IsBusy = false;
                window.Fire_OnReady();
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

        #region Container
        public bool HasWindow(string id)
        {
            return Windows.ContainsKey(id);
        }

        public void RenderUIWindow(FuWindow UIWindow)
        {
            // Do draw window
            UIWindow.DrawWindow();
        }

        public void RenderUIWindows()
        {
            DrawMainContainer();

            _canExternalizeThisFrame = false;
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
                // check whatever window must be draw
                RenderUIWindow(window);
                // update window state
                window.UpdateState(leftButtonState);
                // add to externalize list
                if (_canExternalizeThisFrame && window.WantToLeave())
                {
                    _toExternalizeWindows.Enqueue(window);
                }
            }
        }

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
        private void UIWindow_OnClose(FuWindow UIWindow)
        {
            TryRemoveWindow(UIWindow.ID);
        }
        #endregion

        public bool TryRemoveWindow(string id)
        {
            if (!Windows.ContainsKey(id))
            {
                return false;
            }
            _toRemoveWindows.Enqueue(Windows[id]);
            return true;
        }

        public bool ForcePos()
        {
            return false;
        }

        public IntPtr GetTextureID(Texture2D texture)
        {
            return _fuguiContext.TextureManager.GetTextureId(texture);
        }

        public IntPtr GetTextureID(RenderTexture texture)
        {
            return _fuguiContext.TextureManager.GetTextureId(texture);
        }

        public void ImGuiImage(RenderTexture texture, Vector2 size)
        {
            ImGui.Image(GetTextureID(texture), size);
        }

        public void ImGuiImage(Texture2D texture, Vector2 size)
        {
            ImGui.Image(GetTextureID(texture), size);
        }

        public void ImGuiImage(RenderTexture texture, Vector2 size, Vector4 color)
        {
            ImGui.Image(GetTextureID(texture), size, Vector2.zero, Vector2.one, color);
        }

        public void ImGuiImage(Texture2D texture, Vector2 size, Vector4 color)
        {
            ImGui.Image(GetTextureID(texture), size, Vector2.zero, Vector2.one, color);
        }

        public bool ImGuiImageButton(Texture2D texture, Vector2 size)
        {
            // TODO : add ID to image button
            return ImGui.ImageButton("", GetTextureID(texture), size);
        }

        public bool ImGuiImageButton(Texture2D texture, Vector2 size, Vector4 color)
        {
            // TODO : add ID to image button
            return ImGui.ImageButton("", GetTextureID(texture), size, Vector2.zero, Vector2.one, ImGui.GetStyle().Colors[(int)ImGuiCol.Button], color);
        }
        #endregion

        #region Docking
        private void DrawMainContainer()
        {
            // draw main menu
            FuMainMenu.Draw();
            float mainMenuHeight = 31f;
            // draw main menu separator
            ImGui.GetBackgroundDrawList().AddLine(new Vector2(0f, mainMenuHeight - 1f), new Vector2(_size.x, mainMenuHeight - 1f), ImGui.GetColorU32(FuThemeManager.GetColor(FuColors.HeaderHovered)));

            // draw main dockspace
            uint viewPortID = 0;
            ImGuiDockNodeFlags dockspace_flags = Fugui.Settings.DockingFlags;
            ImGui.SetNextWindowPos(new Vector2(0f, mainMenuHeight));
            ImGui.SetNextWindowSize(new Vector2(_size.x, _size.y - mainMenuHeight));
            ImGui.SetNextWindowViewport(viewPortID);
            Fugui.Push(ImGuiStyleVar.WindowRounding, 0.0f);
            Fugui.Push(ImGuiStyleVar.WindowBorderSize, 0.0f);
            Fugui.Push(ImGuiStyleVar.ItemSpacing, new Vector2(0f, 0f));
            Fugui.Push(ImGuiStyleVar.ItemInnerSpacing, new Vector2(0f, 0f));
            Fugui.Push(ImGuiStyleVar.WindowPadding, Vector2.zero);
            // We are using the UIWindowFlags_NoDocking flag to make the parent window not dockable into,
            // because it would be confusing to have two docking targets within each others.
            ImGuiWindowFlags window_flags = ImGuiWindowFlags.NoDocking;
            window_flags |= ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;
            window_flags |= ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.NoBackground;

            // draw DockSpace main container
            if (ImGui.Begin("MainDockSpace", window_flags))
            {
                Dockspace_id = ImGui.GetID("DockSpace");
                ImGui.DockSpace(Dockspace_id, Vector2.zero, dockspace_flags);
                ImGui.End();
            }
            Fugui.PopStyle(5);

            // draw notifications
            Fugui.RenderNotifications(this);

            // draw modal
            Fugui.RenderModal(this);

            // draw popup message
            Fugui.RenderPopupMessage();
        }
        #endregion
    }
}