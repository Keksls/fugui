using Fugui.Core.DearImGui;
using Fugui.Core.DearImGui.Assets;
using Fugui.Core.DearImGui.Platform;
using Fugui.Core.DearImGui.Renderer;
using Fugui.Framework;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Fugui.Core
{
    public class FuguiManager : MonoBehaviour
    {
        #region Variables
        // The settings for the Fugui Manager
        [SerializeField]
        private FuguiSettings _settings;
        // Camera used to render main UI container
        [SerializeField]
        private Camera _uiCamera;
        // The current OpenTK toolkit
        private OpenTK.Toolkit _currentOpenTKToolkit;
        // size offset of icons glyphs in font
        public float FontIconsSizeOffset = 4;
        // pos offset of icons glyphs in font
        public Vector2 FontIconsOffset = Vector2.zero;
        // renderType to use to render imgui DrawLists (for main container)
        public RenderType RendererType = RenderType.Mesh;
        // platform (input type) to use for main container
        public InputType PlatformType = InputType.InputManager;
        // ImGui default config
        [Header("Configuration")]
        public UIOConfig InitialConfiguration = new UIOConfig
        {
            ImGuiConfig = ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.DockingEnable,

            DoubleClickTime = 0.30f,
            DoubleClickMaxDist = 6.0f,

            DragThreshold = 6.0f,

            KeyRepeatDelay = 0.250f,
            KeyRepeatRate = 0.050f,

            FontGlobalScale = 1.0f,
            FontAllowUserScaling = false,

            DisplayFramebufferScale = Vector2.one,

            MouseDrawCursor = false,
            TextCursorBlink = false,

            ResizeFromEdges = true,
            MoveFromTitleOnly = true,
            ConfigMemoryCompactTimer = 1f,
        };
        // shaders to use to render imgui (main container)
        [Header("Customization")]
        public ShaderResourcesAsset Shaders = null;
        // cursors pack to use
        [SerializeField]
        public CursorShapesAsset CursorShapes = null;
        // URP renderer, not used for now. Keep it for URP eventualy
        [HideInInspector] // unHide this is using URP
        public RenderImGui Render { get; private set; }
        [SerializeField]
        private bool _logErrors = true;
        #endregion

        #region Unity Methods
        void Awake()
        {
            // store Fugui settings
            FuGui.Manager = this;
            FuGui.Settings = _settings;

            // init OpenTK, prevent UI Window externalization
            _currentOpenTKToolkit = OpenTK.Toolkit.Init();

            // prepare FuGui before start using it
            FuGui.Initialize(_uiCamera);

            // log errors to Unity console
            if (_logErrors)
            {
                FuGui.OnUIException += FuGui_OnUIException;
            }
        }

        private void FuGui_OnUIException(Exception error)
        {
            Debug.LogException(error);
        }

        private void Update()
        {
            // Update Fugui Data
            FuGui.Update();

            // let's update main container inputs and internal stuff
            FuGui.MainContainer.Update();

            // render all Fugui Contexts
            FuGui.Render();
        }

        private void LateUpdate()
        {
            // destroy contexts
            while (FuGui.ToDeleteContexts.Count > 0)
            {
                int contextID = FuGui.ToDeleteContexts.Dequeue();
                if (FuGui.ContextExists(contextID))
                {
                    FuguiContext context = FuGui.GetContext(contextID);
                    context.EndRender();
                    context.Destroy();
                    FuGui.Contexts.Remove(context.ID);
                    FuGui.DefaultContext.SetAsCurrent();
                }
            }
        }

        private void OnDisable()
        {
            Dispose();
        }

        private void OnApplicationQuit()
        {
            Dispose();
        }
        #endregion

        #region public Utils
        /// <summary>
        /// Disposes the external windows and stops the render thread.
        /// </summary>
        public void Dispose()
        {
            // Dispose the current OpenTK toolkit
            _currentOpenTKToolkit.Dispose();
            FuGui.Dispose();
        }
        #endregion

    }

    [Serializable]
    /// <summary>
    /// This class contains settings for the Fugui Manager.
    /// </summary>
    public class FuguiSettings
    {
        /// <summary>
        /// A boolean value indicating whether UI windows should be internalized on mouse release.
        /// </summary>
        public bool InternalizeOnMouseRelease = true;
        /// <summary>
        /// The number of FPS to be used when the UI windows are not being manipulated.
        /// </summary>
        [Range(1, 30)]
        public int IdleFPS = 8;
        /// <summary>
        /// The number of FPS to be used when the UI windows are being manipulated.
        /// </summary>
        [Range(1, 90)]
        public int ManipulatingFPS = 60;
        /// <summary>
        /// The number of ticks to be used when the UI windows are being manipulated externally.
        /// </summary>
        [Range(1, 1000000)]
        public long ExternalManipulatingTicks = 1000;
        /// <summary>
        /// A boolean value indicating whether the title bar should be shown for externally manipulated UI windows.
        /// </summary>
        public bool ExternalShowTitleBar = false;
        /// <summary>
        /// A boolean value indicating whether the debug panel should be drawn.
        /// </summary>
        public bool DrawDebugPanel = true;
        /// <summary>
        /// A list of key codes used to trigger externalization of UI windows.
        /// </summary>
        public List<KeyCode> ExternalizationKey;
        /// <summary>
        /// A list of key codes used to trigger internalization of UI windows.
        /// </summary>
        public List<OpenTK.Input.Key> InternalizationKey;
        /// <summary>
        /// The icon to be used for the maximize button.
        /// </summary>
        public Texture2D MaximizeIcon;
        /// <summary>
        /// The icon to be used for the minimize button.
        /// </summary>
        public Texture2D MinimizeIcon;
        /// <summary>
        /// The icon to be used for the top docking button.
        /// </summary>
        public Texture2D TopIcon;
        /// <summary>
        /// The icon to be used for the bottom docking button.
        /// </summary>
        public Texture2D BottomIcon;
        /// <summary>
        /// The icon to be used for the left docking button.
        /// </summary>
        public Texture2D LeftIcon;
        /// <summary>
        /// The icon to be used for the right docking button.
        /// </summary>
        public Texture2D RightIcon;
        /// <summary>
        /// The icon to be used for the center docking button.
        /// </summary>
        public Texture2D CenterIcon;
        /// <summary>
        /// The texture to be used when an OpenGL non-readable texture is loaded.
        /// </summary>
        public Texture2D OpenGLNonReadableTexture;

        [Header("Docking")]
        [Tooltip("Docking flags for the main container dockSpace.")]
        public ImGuiDockNodeFlags DockingFlags = ImGuiDockNodeFlags.None;
        [Tooltip("Always display the Tab bar button.")]
        public bool DisplayTabBarButton = true;
        [Tooltip("Display the Tab bar button to the right of the tab bar.")]
        public bool TabBarButtonRight = true;
    }
}