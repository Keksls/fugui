#if FU_EXTERNALIZATION
using ImGuiNET;
using System;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// External ImGui context (used for windows detached from Unity rendering).
    /// This class mirrors FuUnityContext but targets a native window (DX/GL surface).
    /// </summary>
    public unsafe class FuExternalContext : FuContext
    {
        private FuExternalWindow _window;
        public FuExternalWindow Window => _window;
        private PlatformBase _platform;
        internal SDLPlatform Platform => _platform as SDLPlatform;
        public string Title => _window.Title;
        public int Width => _window.Width;
        public int Height => _window.Height;

        public FuExternalContext(int index, float scale, float fontScale, System.Action onInitialize, FuWindow window) : base(index, scale, fontScale, onInitialize)
        {
            _window = new FuExternalWindow(window, ID);
            initialize(onInitialize);
        }

        protected override void sub_initialize()
        {
            Fugui.SetCurrentContext(this);

            // Platform abstraction for Win32 inputs (keyboard + mouse)
            _platform = new SDLPlatform(_window);
            _platform.Initialize(IO, PlatformIO, "Fugui SDL Platform");

            // Initialize ImGui IO
            IO.ConfigFlags |= ImGuiConfigFlags.DockingEnable | ImGuiConfigFlags.NavEnableKeyboard;
            IO.BackendFlags |= ImGuiBackendFlags.HasMouseCursors |
                               ImGuiBackendFlags.HasSetMousePos |
                               ImGuiBackendFlags.RendererHasVtxOffset;
            IO.DisplaySize = new Vector2(Width, Height);

            // Load fonts and atlas
            if (!ApplySharedFontAtlas())
            {
                using (FuFontLoadResources fontResources = LoadFonts())
                {
                    // Glyph ranges only need to remain alive until the atlas build completes.
                    if (!IO.Fonts.Build())
                    {
                        throw new InvalidOperationException($"ImGui failed to build the font atlas for external context {ID}.");
                    }
                }
            }
            TextureManager.InitializeFontAtlas(IO);

            Fugui.Themes.SetTheme(Fugui.Themes.CurrentTheme);

            SetDefaultImGuiIniFilePath(null);
        }

        internal override bool PrepareRender()
        {
            if (RenderPrepared)
                return true;

            Fugui.SetCurrentContext(this);

            // Prepare IO
            TextureManager.PrepareFrame(IO);
            _platform.PrepareFrame(IO, new Rect(0, 0, Width, Height), AutoUpdateMouse, AutoUpdateKeyboard);

            if (!TryExecuteOnPrepareEvent())
                return false;

            ImGui.NewFrame();
            RenderPrepared = true;

            // execute OnFramePrepared event if needed
            TryExecuteOnFramePreparedEvent();

            return true;
        }

        internal override bool EndRender()
        {
            if (!RenderPrepared)
                return false;

            if (_isDraggingPayload && !_firstFrameDragging && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                CancelDragDrop();

            ImGui.EndFrame();
            RenderPrepared = false;
            return true;
        }

        /// <summary>
        /// Closes the native window and synchronously destroys this external context.
        /// </summary>
        internal override void Destroy()
        {
            if (IsDestroyed)
            {
                return;
            }

            // Session shutdown cannot wait for a later external-window render tick.
            if (_window == null)
            {
                DestroyContextResources();
                return;
            }

            _window.CloseImmediately(DestroyContextResources);
        }

        /// <summary>
        /// Releases the platform, atlas and native ImGui resources owned by this context.
        /// </summary>
        private void DestroyContextResources()
        {
            FuContext previousContext = Fugui.CurrentContext;
            Fugui.SetCurrentContext(this);

            try
            {
                try
                {
                    // SDL platform callbacks borrow this context's IO and must be removed first.
                    _platform?.Shutdown(IO, PlatformIO);
                }
                finally
                {
                    _platform = null;
                    base.Destroy();
                }
            }
            finally
            {
                try
                {
                    if (ImGuiContext != IntPtr.Zero)
                    {
                        ImGui.DestroyContext(ImGuiContext);
                    }
                }
                finally
                {
                    ImGuiContext = IntPtr.Zero;
                    _window = null;
                    RestorePreviousContext(previousContext);
                }
            }
        }

        /// <summary>
        /// Restores a still-registered context after this external context has been destroyed.
        /// </summary>
        /// <param name="previousContext">Context that was current before native resource cleanup.</param>
        private void RestorePreviousContext(FuContext previousContext)
        {
            // Prefer the previous context, then the default context, and otherwise clear native ImGui state.
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

        public override void SetScale(float scale, float fontScale)
        {
            fontScale = QuantizeFontScale(fontScale);
            bool fontScaleChanged = Mathf.Abs(FontScale - fontScale) >= 0.0001f;

            if (Mathf.Abs(Scale - scale) < 0.0001f && !fontScaleChanged)
            {
                return;
            }

            if (fontScaleChanged && UsesSharedFontAtlas)
            {
                // Acquire and apply the replacement before releasing any resource used by the current scale.
                if (!SwitchSharedFontAtlas(fontScale))
                {
                    throw new InvalidOperationException($"Unable to acquire the shared font atlas for scale {fontScale:0.###}.");
                }

                TextureManager.ClearFontAtlas();
            }
            else if (fontScaleChanged)
            {
                FontScale = fontScale;
                using (FuFontLoadResources fontResources = LoadFonts())
                {
                    // Glyph ranges only need to remain alive until the atlas build completes.
                    if (!IO.Fonts.Build())
                    {
                        throw new InvalidOperationException($"ImGui failed to rebuild the font atlas for external context {ID}.");
                    }
                }

                // Keep the previous GPU atlas alive until its native replacement has built successfully.
                TextureManager.ClearFontAtlas();
            }

            Scale = scale;
            if (fontScaleChanged)
            {
                // A changed native atlas receives a matching shared Unity texture exactly once.
                TextureManager.InitializeFontAtlas(IO);
            }

            Fugui.Themes.SetTheme(Fugui.Themes.CurrentTheme);
        }
    }
}
#endif
