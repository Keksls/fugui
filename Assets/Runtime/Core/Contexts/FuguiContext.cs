using Fugui.Core.DearImGui;
using Fugui.Framework;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Fugui.Core
{
    public abstract class FuguiContext
    {
        public int ID;
        public ImGuiIOPtr IO;
        public IntPtr ImGuiContext;
        public IntPtr ImNodesContext;
        public IntPtr ImPlotContext;
        public event Action OnRender;
        public event Action OnPostRender;
        public event Func<bool> OnPrepareFrame;
        public bool AutoUpdateMouse = true;
        public bool AutoUpdateKeyboard = true;
        public bool Started { get; private set; }
        protected bool renderPrepared = false;
        internal Dictionary<int, FontSet> Fonts = new Dictionary<int, FontSet>();
        internal FontSet DefaultFont { get; private set; }

        /// <summary>
        /// Create new imgui native contexts
        /// </summary>
        /// <param name="index">ID of the context</param>
        public FuguiContext(int index, Action onInitialize)
        {
        }

        /// <summary>
        /// Initialize this context
        /// </summary>
        /// <param name="index"></param>
        protected void initialize(int index, Action onInitialize)
        {
            ImGuiContext = ImGui.CreateContext();
#if !UIMGUI_REMOVE_IMPLOT
            ImPlotContext = ImPlotNET.ImPlot.CreateContext();
#endif
#if !UIMGUI_REMOVE_IMNODES
            ImNodesContext = imnodesNET.imnodes.CreateContext();
#endif
            FuguiContext lastDFContext = FuGui.CurrentContext;
            IntPtr currentContext = ImGuiNative.igGetCurrentContext();
            FuGui.SetCurrentContext(this);
            ImGuiNative.igSetCurrentContext(ImGuiContext);
            IO = ImGui.GetIO();
            ID = index;
            onInitialize?.Invoke();
            sub_initialize();
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
        /// Initialize this context for specific sub class. Don't call it, Fugui layout handle it for you
        /// </summary>
        protected abstract void sub_initialize();

        /// <summary>
        /// set the default path to save imgui.ini file
        /// imgui.ini is the file where imgui store windows data (pos, size, collapsed flag)
        /// send null for no save
        /// </summary>
        protected unsafe void SetDefaultImGuiIniFilePath(string iniPath)
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
        internal void Render()
        {
            if (!renderPrepared)
            {
                return;
            }

            try
            {
                UIStyle.Default.Push(true);
                FuGui.Push(ImGuiStyleVar.FramePadding, ThemeManager.CurrentTheme.FramePadding);
                OnRender?.Invoke();
                FuGui.PopStyle(1);
                UIStyle.Default.Pop();
            }
            catch (Exception ex)
            {
                FuGui.DoOnUIException(ex);
            }
            finally
            {
                ImGui.Render();
            }
            OnPostRender?.Invoke();
        }

        /// <summary>
        /// Prepare render for next frame. Don't call it, Fugui layout handle it for you
        /// </summary>
        internal abstract bool PrepareRender();

        /// <summary>
        /// End the context render. Don't call it, Fugui layout handle it for you
        /// </summary>
        internal abstract void EndRender();

        /// <summary>
        /// Destroy this context. Don't call it, Fugui layout handle it for you
        /// </summary>
        internal abstract void Destroy();

        /// /// <summary>
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

        /// <summary>
        /// Start this context (will be drawn)
        /// </summary>
        public void Start()
        {
            Started = true;
        }

        /// <summary>
        /// Stop this context (will neither Render or Draw until Start() again)
        /// </summary>
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

        /// <summary>
        /// Execute OnPRepare Event if it's not null and return it value (if null, return true)
        /// </summary>
        /// <returns>whathever the render must be switch</returns>
        protected bool TryExecuteOnPrepareEvent()
        {
            if (OnPrepareFrame != null) // check whatever someone is registered on PrepareFrame Event
            {
                // call event and stop here before creating a new frame if event return false
                if (!OnPrepareFrame.Invoke())
                {
                    renderPrepared = false;
                    return renderPrepared;
                }
            }
            return true;
        }

        /// <summary>
        /// Load fonts for this context according to FontConfig into FuGui.Settings.FontConfig
        /// </summary>
        protected unsafe void LoadFonts()
        {
            // get font config from FuguiManager Settings
            FontConfig fontConf = FuGui.Settings.FontConfig;
            // whatever we need to add Icons to fontAtlas
            bool addIcons = !string.IsNullOrEmpty(fontConf.IconsFontName);
            // clear existing font atlas data
            IO.Fonts.Clear(); // Previous FontDefault reference no longer valid.
                              // destroy default font pointer
            IO.NativePtr->FontDefault = default; // NULL uses Fonts[0]

            // declare glythRangePointor in case we need to add Icons to the font atlas
            IntPtr glyphRangePtr = IntPtr.Zero;
            if (addIcons)
            {
                // get native imguiGlyphRangeBuilder ptr
                ImFontGlyphRangesBuilder* builder = ImGuiNative.ImFontGlyphRangesBuilder_ImFontGlyphRangesBuilder();
                // add any glyph between min and max icon range
                for (ushort i = fontConf.StartIconsGlyph; i <= fontConf.EndIconsGlyph; i++)
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
                glyphRangePtr = vecPtr->Data;
            }

            // get default native fontConfig ptr
            ImFontConfig* conf = ImGuiNative.ImFontConfig_ImFontConfig();
            // keep ptr into scope throw manager struct
            ImFontConfigPtr iconConfigPtr = new ImFontConfigPtr(conf);
            // merge icon with last added font
            iconConfigPtr.MergeMode = true;
            iconConfigPtr.GlyphOffset = FuGui.Manager.FontIconsOffset;

            // get and process Folder and Files Paths
            string fontPath = Path.Combine(Application.streamingAssetsPath, fontConf.FontsFolder);
            string regularFile = Path.Combine(fontPath, fontConf.RegularFontName);
            string boldFile = Path.Combine(fontPath, fontConf.BoldFontName);
            string iconFile = Path.Combine(fontPath, fontConf.IconsFontName);

            if (!File.Exists(regularFile))
            {
                Debug.LogError(regularFile + " does not exists");
                IO.Fonts.AddFontDefault();
                IO.Fonts.Build();
                return;
            }

            if (!File.Exists(boldFile))
            {
                Debug.LogError(boldFile + " does not exists");
                IO.Fonts.AddFontDefault();
                IO.Fonts.Build();
                return;
            }

            if (!File.Exists(iconFile))
            {
                Debug.LogError(iconFile + " does not exists");
                IO.Fonts.AddFontDefault();
                IO.Fonts.Build();
                return;
            }

            // add default font
            registerFont(fontConf.DefaultSize);
            // save default font reference
            DefaultFont = Fonts[fontConf.DefaultSize];
            // add additionnal fonts sizes
            foreach (int size in fontConf.AdditionnalFontSizes)
            {
                registerFont(size);
            }

            // ImGui build font atlas
            IO.Fonts.Build();

            // Register a single size font (Bold + Regular + Icons)
            unsafe void registerFont(int size)
            {
                if (!Fonts.ContainsKey(size))
                {
                    Fonts[size] = new FontSet();

                    // add regular + icon font
                    ImFontPtr fontRegular = IO.Fonts.AddFontFromFileTTF(regularFile, size);
                    if (addIcons)
                    {
                        IO.Fonts.AddFontFromFileTTF(iconFile, size - FuGui.Manager.FontIconsSizeOffset, iconConfigPtr, glyphRangePtr);
                    }
                    Fonts[size].Regular = fontRegular;

                    // add bold font
                    if (fontConf.AddBold)
                    {
                        ImFontPtr fontBold = IO.Fonts.AddFontFromFileTTF(boldFile, size);
                        if (addIcons && fontConf.AddIconsToBold)
                        {
                            IO.Fonts.AddFontFromFileTTF(iconFile, size - FuGui.Manager.FontIconsSizeOffset, iconConfigPtr, glyphRangePtr);
                        }
                        Fonts[size].Bold = fontBold;
                    }
                    // if we don't want bold font, let's set regular size ptr as bold
                    else
                    {
                        Fonts[size].Bold = fontRegular;
                    }
                }
            }
        }
    }

    internal class FontSet
    {
        public ImFontPtr Regular;
        public ImFontPtr Bold;
    }

    public enum FuguiContextType
    {
        UnityContext = 0,
        ExternalContext = 1
    }
}