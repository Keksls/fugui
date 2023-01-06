using ImGuiNET;
using System;
using Fugui.Core.DearImGui.Platform;
using Fugui.Core.DearImGui.Renderer;
using Fugui.Core.DearImGui.Texture;
using UnityEngine;
using UnityEngine.Rendering;
using System.Text;
using System.Runtime.InteropServices;
using Fugui.Framework;
using System.Collections.Generic;
using System.IO;

namespace Fugui.Core.DearImGui
{
    public sealed class FuguiContext
    {
        public int ID;
        public Camera Camera;
        public ImGuiIOPtr IO;
        public IntPtr ImGuiContext;
        public IntPtr ImNodesContext;
        public IntPtr ImPlotContext;
        public TextureManager TextureManager;
        public event Action OnLayout;
        public event Action OnPrepareFrame;
        public event Action OnInitialize;
        public bool AutoUpdateMouse = true;
        public bool AutoUpdateKeyboard = true;
        public bool Started { get; private set; }
        private IRenderer _renderer;
        private IPlatform _platform;
        private CommandBuffer _renderCommandBuffer;
        private RenderImGui _renderFeature = null;
        private bool renderPrepared = false;
        // loaded fonts
        internal Dictionary<int, FontSet> Fonts = new Dictionary<int, FontSet>();
        // current default fonts
        internal FontSet DefaultFont { get; private set; }
        // fonts sizes to load
        private float[] _fontSizes = new float[] { 14, 10, 12, 16, 18 };

        /// <summary>
        /// Create new imgui native contexts
        /// </summary>
        /// <param name="index">ID of the context</param>
        /// <param name="camera">camera that will render imgui context</param>
        public FuguiContext(int index, Camera camera, RenderImGui renderFeature)
        {
            _renderFeature = renderFeature;
            ImGuiContext = ImGui.CreateContext();
#if !UIMGUI_REMOVE_IMPLOT
            ImPlotContext = ImPlotNET.ImPlot.CreateContext();
#endif
#if !UIMGUI_REMOVE_IMNODES
            ImNodesContext = imnodesNET.imnodes.CreateContext();
#endif
            TextureManager = new TextureManager();
            Camera = camera;

            FuguiContext lastDFContext = FuGui.CurrentContext;
            IntPtr currentContext = ImGuiNative.igGetCurrentContext();
            FuGui.SetCurrentContext(this);
            ImGuiNative.igSetCurrentContext(ImGuiContext);
            IO = ImGui.GetIO();
            ID = index;
            Initialize();
            OnInitialize?.Invoke();
            if (lastDFContext != null)
            {
                FuGui.SetCurrentContext(lastDFContext);
            }
            else
            {
                ImGuiNative.igSetCurrentContext(currentContext);
            }
            Started = true;
        }

        /// <summary>
        /// Initialize this context. Don't call it, Fugui layout handle it for you
        /// </summary>
        public void Initialize()
        {
            if (Camera != null)
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

                FuGui.SetCurrentContext(this);

                IPlatform platform = PlatformUtility.Create(FuGui.Manager.PlatformType, FuGui.Manager.CursorShapes, null);
                SetPlatform(platform, IO);
                if (_platform == null)
                {
                    throw new Exception("imgui platform is null");
                }

                SetRenderer(RenderUtility.Create(FuGui.Manager.RendererType, FuGui.Manager.Shaders, TextureManager), IO);
                if (_renderer == null)
                {
                    throw new Exception("imgui renderer is null");
                }

                LoadFonts(IO);
                // font atlas will be copied into GPU and keeped into unit Texture2D used for render pass
                TextureManager.InitializeFontAtlas(IO);
                ThemeManager.SetTheme(ThemeManager.CurrentTheme);

                SetDefaultImGuiIniFilePath(null);
            }
            else
            {
                LoadFonts(IO);
                ThemeManager.SetTheme(ThemeManager.CurrentTheme);
                SetDefaultImGuiIniFilePath(null);
            }
        }

        /// <summary>
        /// set the default path to save imgui.ini file
        /// imgui.ini is the file where imgui store windows data (pos, size, collapsed flag)
        /// send null for no save
        /// </summary>
        private unsafe void SetDefaultImGuiIniFilePath(string iniPath)
        {
            var io = ImGuiNative.igGetIO();
            if (iniPath != null)
            {
                int byteCount = Encoding.UTF8.GetByteCount(iniPath);
                byte* nativeName = (byte*)Marshal.AllocHGlobal(byteCount + 1);
                int offset = Utils.GetUtf8(iniPath, nativeName, byteCount);
                nativeName[offset] = 0x0;
                io->IniFilename = nativeName;
            }
            else
            {
                io->IniFilename = (byte*)0;
            }
        }

        /// <summary>
        /// Do render this frame. Don't call it, Imgui layout handle it for you
        /// </summary>
        public void Render()
        {
            if (!renderPrepared)
            {
                return;
            }

            try
            {
                OnLayout?.Invoke();
            }
            catch (Exception ex)
            {
                FuGui.DoOnUIException(ex);
            }
            finally
            {
                ImGui.Render();
            }
        }

        /// <summary>
        /// Prepare render for next frame. Don't call it, Fugui layout handle it for you
        /// </summary>
        public void PrepareRender()
        {
            FuGui.SetCurrentContext(this);

            if (Camera != null)
            {
                TextureManager.PrepareFrame(IO);
                _platform.PrepareFrame(IO, Camera.pixelRect, AutoUpdateMouse, AutoUpdateKeyboard);
            }
            OnPrepareFrame?.Invoke();
            ImGui.NewFrame();
#if !UIMGUI_REMOVE_IMGUIZMO
            ImGuizmoNET.ImGuizmo.BeginFrame();
#endif
            renderPrepared = true;
        }

        /// <summary>
        /// End the context render. Don't call it, Fugui layout handle it for you
        /// </summary>
        public void EndRender()
        {
            if (!renderPrepared || Camera == null)
                return;
            renderPrepared = false;
            _renderCommandBuffer.Clear();
            _renderer.RenderDrawLists(_renderCommandBuffer, ImGuiDrawListUtils.GetDrawCmd(FuGui.UIWindows, ImGui.GetDrawData()));
        }

        /// <summary>
        /// Destroy this context. Don't call it, Fugui layout handle it for you
        /// </summary>
        public void Destroy()
        {
            FuGui.SetCurrentContext(this);

            SetRenderer(null, IO);
            SetPlatform(null, IO);

            if (Camera != null)
            {
                TextureManager.Shutdown();

                FuGui.SetCurrentContext(null);

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
            }

            ImGui.DestroyContext(ImGuiContext);
#if !UIMGUI_REMOVE_IMPLOT
            ImPlotNET.ImPlot.DestroyContext(ImPlotContext);
#endif
#if !UIMGUI_REMOVE_IMNODES
            imnodesNET.imnodes.DestroyContext(ImNodesContext);
#endif
        }

        /// <summary>
        /// Set this context as current. Don't call it, Fugui layout handle it for you
        /// </summary>
        public unsafe void SetAsCurrent()
        {
            FuGui.CurrentContext = this;
            ImGui.SetCurrentContext(ImGuiContext);

#if !UIMGUI_REMOVE_IMPLOT
            ImPlotNET.ImPlot.SetImGuiContext(ImGuiContext);
#endif
#if !UIMGUI_REMOVE_IMGUIZMO
            ImGuizmoNET.ImGuizmo.SetImGuiContext(ImGuiContext);
#endif
#if !UIMGUI_REMOVE_IMNODES
            imnodesNET.imnodesNative.imnodes_SetImGuiContext(ImGuiContext);
#endif
        }

        public void Start()
        {
            Started = true;
        }

        public void Stop()
        {
            Started = false;
        }

        /// <summary>
        /// Update Imgui Mouse position for this context. Must be call on event 'OnPrepareFrame'
        /// </summary>
        /// <param name="mousePos">Mouse Position</param>
        /// <param name="mouseWheel">Mouse Whell scroll (H / V)</param>
        /// <param name="clickbtn0">Mouse button 0 state</param>
        /// <param name="clickbtn1">Mouse button 1 state</param>
        /// <param name="clickbtn2">Mouse button 2 state</param>
        public void UpdateMouse(Vector2 mousePos, Vector2 mouseWheel, bool clickbtn0, bool clickbtn1, bool clickbtn2)
        {
            IO.MousePos = mousePos;// Utils.ScreenToImGui(mousePos);

            IO.MouseWheelH = mouseWheel.x;
            IO.MouseWheel = mouseWheel.y;

            IO.MouseDown[0] = clickbtn0;
            IO.MouseDown[1] = clickbtn1;
            IO.MouseDown[2] = clickbtn2;
        }

        private void SetRenderer(IRenderer renderer, ImGuiIOPtr io)
        {
            _renderer?.Shutdown(io);
            _renderer = renderer;
            _renderer?.Initialize(io);
        }

        private void SetPlatform(IPlatform platform, ImGuiIOPtr io)
        {
            _platform?.Shutdown(io);
            _platform = platform;
            _platform?.Initialize(io, FuGui.Manager.InitialConfiguration, "Unity " + FuGui.Manager.PlatformType.ToString());
        }

        private unsafe void LoadFonts(ImGuiIOPtr io)
        {
            // clear existing font atlas data
            io.Fonts.Clear(); // Previous FontDefault reference no longer valid.
                              // destroy default font pointer
            io.NativePtr->FontDefault = default; // NULL uses Fonts[0]

            // get native imguiGlyphRangeBuilder ptr
            ImFontGlyphRangesBuilder* builder = ImGuiNative.ImFontGlyphRangesBuilder_ImFontGlyphRangesBuilder();
            // add any glyph between min and max icon range
            for (ushort i = Icons._minChar; i <= Icons._maxChar; i++)
            {
                ImGuiNative.ImFontGlyphRangesBuilder_AddChar(builder, i);
            }
            // create default imVector struct ref
            ImVector vec = default;
            // get vector ptr
            ImVector* vecPtr = &vec;
            // native build ranges
            ImGuiNative.ImFontGlyphRangesBuilder_BuildRanges(builder, vecPtr);
            // get range and keep it into managed scope (glyphRangePtr is static, because imgui lazy use glyphRangePtr)
            IntPtr glyphRangePtr = vecPtr->Data;

            // get default native fontConfig ptr
            ImFontConfig* conf = ImGuiNative.ImFontConfig_ImFontConfig();
            // keep ptr into scope throw manager struct
            ImFontConfigPtr iconConfigPtr = new ImFontConfigPtr(conf);
            // merge icon with last added font
            iconConfigPtr.MergeMode = true;
            iconConfigPtr.GlyphOffset = FuGui.Manager.FontIconsOffset;

            string fontPath = Path.Combine(Application.streamingAssetsPath + @"/fonts/current/");
            string iconFile = Path.Combine(fontPath, "icons.ttf");
            foreach (int size in _fontSizes)
            {
                if (!Fonts.ContainsKey(size))
                {
                    Fonts[size] = new FontSet();
                    // add regular + icon font
                    ImFontPtr fontRegular = io.Fonts.AddFontFromFileTTF(fontPath + "regular.ttf", size);
                    io.Fonts.AddFontFromFileTTF(iconFile, size - FuGui.Manager.FontIconsSizeOffset, iconConfigPtr, glyphRangePtr);
                    Fonts[size].Regular = fontRegular;
                    // add bold font
                    Fonts[size].Bold = io.Fonts.AddFontFromFileTTF(fontPath + "bold.ttf", size);
                    if (size == 14)
                    {
                        DefaultFont = Fonts[size];
                    }
                }
            }
            io.Fonts.Build();
        }
    }

    internal class FontSet
    {
        public ImFontPtr Regular;
        public ImFontPtr Bold;
    }
}