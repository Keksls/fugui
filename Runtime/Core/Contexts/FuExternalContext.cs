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
        public SDLPlatform Platform => _platform as SDLPlatform;
        public string Title => _window.Title;
        public int Width => _window.Width;
        public int Height => _window.Height;

        public FuExternalContext(int index, float scale, float fontScale, System.Action onInitialize, FuWindow window) : base(index, scale, fontScale, onInitialize)
        {
            _window = new FuExternalWindow(window);
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
            IO.BackendFlags |= ImGuiBackendFlags.HasMouseCursors | ImGuiBackendFlags.HasSetMousePos;
            IO.DisplaySize = new Vector2(Width, Height);

            // Load fonts and atlas
            if (!ApplySharedFontAtlas())
            {
                LoadFonts();
                IO.Fonts.Build();
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

        internal override void Destroy()
        {
            _window?.Close(() =>
            {
                FuContext previousContext = Fugui.CurrentContext;
                Fugui.SetCurrentContext(this);
                _platform?.Shutdown(IO, PlatformIO);
                _platform = null;
                base.Destroy();

                ImGui.DestroyContext(ImGuiContext);
                ImGuiContext = IntPtr.Zero;
                _window = null;
                RestorePreviousContext(previousContext);
            });
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

        public override void SetScale(float scale, float fontScale)
        {
            fontScale = QuantizeFontScale(fontScale);
            float oldFontScale = FontScale;

            if (Mathf.Abs(Scale - scale) < 0.0001f && Mathf.Abs(FontScale - fontScale) < 0.0001f)
            {
                return;
            }

            Scale = scale;
            FontScale = fontScale;

            TextureManager.ClearFontAtlas(oldFontScale);
            if (!SwitchSharedFontAtlas(fontScale))
            {
                LoadFonts();
                IO.Fonts.Build();
            }
            TextureManager.InitializeFontAtlas(IO);
            Fugui.Themes.SetTheme(Fugui.Themes.CurrentTheme);
        }
    }
}
#endif
