using Fu.Core.DearImGui.Platform;
using Fu.Core.DearImGui.Texture;
using Fu.Framework;
using ImGuiNET;
using System;
using System.Linq;
using UnityEngine;

namespace Fu.Core
{
    public class FuUnityContext : FuContext
    {
        public Camera Camera;
        private PlatformBase _platform;

        public FuUnityContext(int index, float scale, float fontScale, Action onInitialize, Camera camera) : base(index, scale, fontScale, onInitialize)
        {
            Camera = camera;
            initialize(onInitialize);
        }

        /// <summary>
        /// Destroy this context. Don't call it, Fugui layout handle it for you
        /// </summary>
        internal override void Destroy()
        {
            base.Destroy();
            Fugui.SetCurrentContext(this);
            SetPlatform(null, IO, PlatformIO);
            Fugui.SetCurrentContext(null);
            ImGui.DestroyContext(ImGuiContext);
        }

        /// <summary>
        /// End the context render. Don't call it, Fugui layout handle it for you
        /// </summary>
        internal override bool EndRender()
        {
            Fugui.IsRendering = false;
            if (!RenderPrepared)
                return false;

            // cancel drag drop for this context if left click is up this frame and it's not the first frame of the current drag drop operation
            if (_isDraggingPayload && !_firstFrameDragging && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                CancelDragDrop();
            }

            RenderPrepared = false;
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

            Fugui.IsRendering = true;
            // set this contextr as current
            Fugui.SetCurrentContext(this);

            // prepare frames textures and platform data (inputs)
            TextureManager.PrepareFrame(IO);
            _platform.PrepareFrame(IO, Camera.pixelRect, AutoUpdateMouse, AutoUpdateKeyboard);

            // execute OnPrepare event if needed and return onPrepare result if not null
            if (!TryExecuteOnPrepareEvent())
            {
                return false;
            }

            // start new imgui frame
            ImGui.NewFrame();
            // assume we are prepared
            RenderPrepared = true;
            return RenderPrepared;
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
            FuThemeManager.SetTheme(FuThemeManager.CurrentTheme);

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

            // set scale
            Scale = scale;
            FontScale = fontScale;

            // update font scale
            TextureManager.ClearFontAtlas(oldScale);
            LoadFonts();
            IO.Fonts.Build();  
            // font atlas will be copied into GPU and keeped into unit Texture2D used for render pass
            TextureManager.InitializeFontAtlas(IO);
            FuThemeManager.SetTheme(FuThemeManager.CurrentTheme);

            // scale windows sizes for windows NOT docked, visible and in this context
            Fugui.UIWindows.Where(win => win.Value.Container.Context == this && win.Value.IsVisible && !win.Value.IsDocked).ToList()
                .ForEach((win) =>
                {
                    win.Value.Size = new Vector2Int((int)(win.Value.Size.x * (scale / oldScale)), (int)(win.Value.Size.y * (scale / oldScale)));
                });
        }
    }
}