using ImGuiNET;
using System;
using System.Linq;
using UnityEngine;

namespace Fu
{
    public class FuUnityContext : FuContext
    {
        public Camera Camera;
        public Rect PixelRect;
        public RenderTexture TargetTexture {  get; private set; }
        public bool IsOffscreen => TargetTexture != null;
        private PlatformBase _platform;

        public FuUnityContext(int index, float scale, float fontScale, Action onInitialize, Camera camera) : base(index, scale, fontScale, onInitialize)
        {
            Camera = camera;
            PixelRect = camera != null ? camera.pixelRect : new Rect(0f, 0f, 1f, 1f);
            initialize(onInitialize);
        }

        public FuUnityContext(int index, float scale, float fontScale, Action onInitialize, Rect pixelRect) : base(index, scale, fontScale, onInitialize)
        {
            Camera = null;
            PixelRect = pixelRect;
            initialize(onInitialize);
        }

        /// <summary>
        /// Set the target texture of this context. If it's null, this context will be rendered on screen, otherwise it will be rendered on the target texture. Don't call it, Fugui layout handle it for you
        /// </summary>
        /// <param name="targetTexture"> new target texture of this context</param>
        public void SetTargetTexture(RenderTexture targetTexture)
        {
            TargetTexture = targetTexture;
        }

        /// <summary>
        /// Destroy this context. Don't call it, Fugui layout handle it for you
        /// </summary>
        internal override void Destroy()
        {
            FuContext previousContext = Fugui.CurrentContext;
            Fugui.SetCurrentContext(this);
            base.Destroy();
            SetPlatform(null, IO, PlatformIO);
            ImGui.DestroyContext(ImGuiContext);
            ImGuiContext = IntPtr.Zero;
            RestorePreviousContext(previousContext);
        }

        private void RestorePreviousContext(FuContext previousContext)
        {
            if (previousContext != null && previousContext != this && Fugui.ContextExists(previousContext.ID))
            {
                Fugui.SetCurrentContext(previousContext);
                return;
            }

            if (Fugui.DefaultContext != null && Fugui.ContextExists(Fugui.DefaultContext.ID))
            {
                Fugui.SetCurrentContext(Fugui.DefaultContext);
                return;
            }

            Fugui.SetCurrentContext(null);
        }

        /// <summary>
        /// End the context render. Don't call it, Fugui layout handle it for you
        /// </summary>
        internal override bool EndRender()
        {
            if (!RenderPrepared)
            {
                Debug.LogWarning("[Fugui] Trying to end render for context that is not prepared.");
                return false;
            }

            // cancel drag drop for this context if left click is up this frame and it's not the first frame of the current drag drop operation
            if (_isDraggingPayload && !_firstFrameDragging && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                CancelDragDrop();
            }

            RenderPrepared = false;
            ImGui.EndFrame();
            return true;
        }

        /// <summary>
        /// Prepare render for next frame. Don't call it, Fugui layout handle it for you
        /// </summary>
        internal override bool PrepareRender()
        {
            // render is already prepared
            if (RenderPrepared)
            {
                return true;
            }

            // set this contextr as current
            Fugui.SetCurrentContext(this);

            // prepare frames textures and platform data (inputs)
            TextureManager.PrepareFrame(IO);
            Rect rect = Camera != null ? Camera.pixelRect : PixelRect;
            _platform.PrepareFrame(IO, rect, AutoUpdateMouse, AutoUpdateKeyboard);

            // execute OnPrepare event if needed and return onPrepare result if not null
            if (!TryExecuteOnPrepareEvent())
            {
                return false;
            }

            // start new imgui frame
            ImGui.NewFrame();
            // assume we are prepared
            RenderPrepared = true;

            // execute OnFramePrepared event if needed
            TryExecuteOnFramePreparedEvent();

            return RenderPrepared;
        }

        /// <summary>
        /// Set the pixel rect of this context. It will be used by the platform to update mouse position and for some renderers to set the viewport. Don't call it, Fugui layout handle it for you
        /// </summary>
        /// <param name="rect"> new pixel rect of this context</param>
        public void SetPixelRect(Rect rect)
        {
            PixelRect = rect;
        }

        /// <summary>
        /// Initialize this context. Don't call it, Fugui layout handle it for you
        /// </summary>
        protected override void sub_initialize()
        {
            Fugui.SetCurrentContext(this);

            // create the input manager platform
            PlatformBase platform = isUsingNewInputSystem() ? new InputSystemPlatform() : new InputManagerPlatform();
            SetPlatform(platform, IO, PlatformIO);

            // check if platform is set
            if (_platform == null)
            {
                throw new Exception("imgui platform is null");
            }

            LoadFonts();
            IO.Fonts.Build();
            // font atlas will be copied into GPU and keeped into unit Texture2D used for render pass
            TextureManager.InitializeFontAtlas(IO);
            Fugui.Themes.SetTheme(Fugui.Themes.CurrentTheme);

            SetDefaultImGuiIniFilePath(null);
        }

        /// <summary>
        /// Check if the new input system is used in the project.
        /// </summary>
        /// <returns> True if the new input system is used, false if the legacy input system is used.</returns>
        private bool isUsingNewInputSystem()
        {
            try
            {
                // fake a check for the legacy input system by trying to access a random key.
                // This will throw an exception if the new input system is used.
                Input.GetKey(KeyCode.End);
                return false;
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Set a IPlatform to handle ImGui Inputs into main container (Unity context)
        /// </summary>
        /// <param name="platform">platform (Input system) to use</param>
        /// <param name="io">IO of the ImGui context</param>
        /// <param name="pio">Platform IO of the ImGui context</param>
        private void SetPlatform(PlatformBase platform, ImGuiIOPtr io, ImGuiPlatformIOPtr pio)
        {
            _platform?.Shutdown(io, pio);
            _platform = platform;
            _platform?.Initialize(io, pio);
        }

        /// <summary>
        /// Set the scale of this context
        /// </summary>
        /// <param name="scale">global context scale</param>
        /// <param name="fontScale">context font scale (usualy same value as context scale)</param>
        public override void SetScale(float scale, float fontScale)
        {
            // store old scale
            float oldScale = Scale;
            float oldFontScale = FontScale;

            if (Mathf.Abs(Scale - scale) < 0.0001f && Mathf.Abs(FontScale - fontScale) < 0.0001f)
            {
                return;
            }

            // set scale
            Scale = scale;
            FontScale = fontScale;

            // update font scale
            TextureManager.ClearFontAtlas(oldFontScale);
            LoadFonts();
            IO.Fonts.Build();  
            // font atlas will be copied into GPU and keeped into unit Texture2D used for render pass
            TextureManager.InitializeFontAtlas(IO);
            Fugui.Themes.SetTheme(Fugui.Themes.CurrentTheme);

            // scale windows sizes for windows NOT docked, visible and in this context
            Fugui.UIWindows.Where(win => win.Value.Container.Context == this && win.Value.IsVisible && !win.Value.IsDocked && !win.Value.Is3DWindow).ToList()
                .ForEach((win) =>
                {
                    win.Value.Size = new Vector2Int((int)(win.Value.Size.x * (scale / oldScale)), (int)(win.Value.Size.y * (scale / oldScale)));
                });
        }
    }
}
