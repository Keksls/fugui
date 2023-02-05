using Fu.Core.DearImGui;
using Fu.Core.DearImGui.Platform;
using Fu.Core.DearImGui.Renderer;
using Fu.Core.DearImGui.Texture;
using Fu.Framework;
using ImGuiNET;
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fu.Core
{
    public class FuUnityContext : FuContext
    {
        public Camera Camera;
        public TextureManager TextureManager;
        private IRenderer _renderer;
        private IPlatform _platform;
        private CommandBuffer _renderCommandBuffer;
        private RenderImGui _renderFeature = null;

        public FuUnityContext(int index, float scale, float fontScale, Action onInitialize, Camera camera, RenderImGui renderFeature) : base(index, scale, fontScale, onInitialize)
        {
            _renderFeature = renderFeature;
            TextureManager = new TextureManager();
            Camera = camera;
            initialize(onInitialize);
        }

        /// <summary>
        /// Destroy this context. Don't call it, Fugui layout handle it for you
        /// </summary>
        internal override void Destroy()
        {
            Fugui.SetCurrentContext(this);

            SetRenderer(null, IO);
            SetPlatform(null, IO);

            TextureManager.Shutdown();

            Fugui.SetCurrentContext(null);

            if (RenderUtility.IsUsingURP())
            {
                if (_renderFeature != null)
                {
#if HAS_URP
                    _renderFeature.Camera = null;
#endif
                    _renderFeature.CommandBuffer = null;
                }
            }
            else
            {
                if (Camera != null)
                {
                    Camera.RemoveCommandBuffer(CameraEvent.AfterEverything, _renderCommandBuffer);
                }
            }

            if (_renderCommandBuffer != null)
            {
                RenderUtility.ReleaseCommandBuffer(_renderCommandBuffer);
            }

            _renderCommandBuffer = null;

            ImGui.DestroyContext(ImGuiContext);
#if !UIMGUI_REMOVE_IMPLOT
            ImPlotNET.ImPlot.DestroyContext(ImPlotContext);
#endif
#if !UIMGUI_REMOVE_IMNODES
            imnodesNET.imnodes.DestroyContext(ImNodesContext);
#endif
        }

        /// <summary>
        /// End the context render. Don't call it, Fugui layout handle it for you
        /// </summary>
        internal override void EndRender()
        {
            if (!renderPrepared)
                return;
            renderPrepared = false;
            _renderCommandBuffer.Clear();
            _renderer.RenderDrawLists(_renderCommandBuffer, ImGuiDrawListUtils.GetDrawCmd(Fugui.UIWindows, ImGui.GetDrawData()));
        }

        /// <summary>
        /// Prepare render for next frame. Don't call it, Fugui layout handle it for you
        /// </summary>
        internal override bool PrepareRender()
        {
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
#if !UIMGUI_REMOVE_IMGUIZMO
            ImGuizmoNET.ImGuizmo.BeginFrame();
#endif
            // assume we are prepared
            renderPrepared = true;
            return renderPrepared;
        }

        /// <summary>
        /// Initialize this context. Don't call it, Fugui layout handle it for you
        /// </summary>
        protected override void sub_initialize()
        {
            if (_renderFeature == null && RenderUtility.IsUsingURP())
            {
                throw new Exception("render feature must be set if using URP");
            }

            _renderCommandBuffer = RenderUtility.GetCommandBuffer(Constants.UImGuiCommandBuffer);

            if (RenderUtility.IsUsingURP())
            {
#if HAS_URP
                _renderFeature.Camera = Camera;
#endif
                _renderFeature.CommandBuffer = _renderCommandBuffer;
            }
            else if (!RenderUtility.IsUsingHDRP())
            {
                Camera.AddCommandBuffer(CameraEvent.AfterEverything, _renderCommandBuffer);
            }

            Fugui.SetCurrentContext(this);

            IPlatform platform = PlatformUtility.Create(Fugui.Settings.PlatformType, Fugui.Settings.CursorShapes, null);
            SetPlatform(platform, IO);
            if (_platform == null)
            {
                throw new Exception("imgui platform is null");
            }

            SetRenderer(RenderUtility.Create(Fugui.Settings.RendererType, Fugui.Settings.Shaders, TextureManager), IO);
            if (_renderer == null)
            {
                throw new Exception("imgui renderer is null");
            }

            LoadFonts();
            // font atlas will be copied into GPU and keeped into unit Texture2D used for render pass
            TextureManager.InitializeFontAtlas(IO);
            FuThemeManager.SetTheme(FuThemeManager.CurrentTheme);

            SetDefaultImGuiIniFilePath(null);
        }

        /// <summary>
        /// Set a IRenderer to render ImGui into main container (Unity context)
        /// </summary>
        /// <param name="renderer">Renderer to use</param>
        /// <param name="io">IO of the ImGui context</param>
        private void SetRenderer(IRenderer renderer, ImGuiIOPtr io)
        {
            _renderer?.Shutdown(io);
            _renderer = renderer;
            _renderer?.Initialize(io);
        }

        /// <summary>
        /// Set a IPlatform to handle ImGui Inputs into main container (Unity context)
        /// </summary>
        /// <param name="platform">platform (Input system) to use</param>
        /// <param name="io">IO of the ImGui context</param>
        private void SetPlatform(IPlatform platform, ImGuiIOPtr io)
        {
            _platform?.Shutdown(io);
            _platform = platform;
            _platform?.Initialize(io, "Unity " + Fugui.Settings.PlatformType.ToString());
        }
    }
}