using Fu.Core.DearImGui;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Fu.Core
{
    public abstract class FuContext
    {
        public int ID;
        public ImGuiIOPtr IO;
        public ImGuiPlatformIOPtr PlatformIO;
        public IntPtr ImGuiContext;
        public IntPtr ImNodesContext;
        public IntPtr ImPlotContext;
        /// <summary>
        /// Whenever the context render
        /// </summary>
        public event Action OnRender;
        /// <summary>
        /// Whenever the context has render its windows, but you still can render ImGUi native window 
        /// </summary>
        public event Action OnLastRender;
        /// <summary>
        /// Whenever the context has compute its draw calls, you can't render UI in it
        /// </summary>
        public event Action OnPostRender;
        public event Func<bool> OnPrepareFrame;
        public bool AutoUpdateMouse = true;
        public bool AutoUpdateKeyboard = true;
        public bool Started { get; private set; }
        public float Scale { get; protected set; }
        public float FontScale { get; protected set; }
        public DrawData DrawData { get; protected set; } = new DrawData();
        public bool RenderPrepared { get; protected set; } = false;
        internal Dictionary<int, FontSet> Fonts = new Dictionary<int, FontSet>();
        internal FontSet DefaultFont { get; private set; }
        // var to count how many push are at frame start, so we can pop missing push
        private static int _nbColorPushOnFrameStart = 0;
        private static int _nbStylePushOnFrameStart = 0;
        private static int _nbFontPushOnFrameStart = 0;
        // the payload of draggDrop operation
        internal object _dragDropPayload = null;
        // Whatever fugui is currently dragging a payload (using Drag Drop)
        internal bool _isDraggingPayload = false;
        // ID of the current dragging payload (if there is some, else is null)
        internal string _draggingPayloadID;
        // Is it the first frame of the current drag drop operation
        internal bool _firstFrameDragging;

        /// <summary>
        /// Create new imgui native contexts
        /// </summary>
        /// <param name="index">ID of the context</param>
        public FuContext(int index, float scale, float fontScale, Action onInitialize)
        {
            Scale = scale;
            FontScale = fontScale;
            ID = index;
        }

        /// <summary>
        /// Initialize this context
        /// </summary>
        /// <param name="index"></param>
        protected void initialize(Action onInitialize)
        {
            ImGuiContext = ImGui.CreateContext();
            FuContext lastDFContext = Fugui.CurrentContext;
            IntPtr currentContext = ImGuiNative.igGetCurrentContext();
            Fugui.SetCurrentContext(this);
            ImGuiNative.igSetCurrentContext(ImGuiContext);
            IO = ImGui.GetIO();
            PlatformIO = ImGui.GetPlatformIO();
            onInitialize?.Invoke();
            sub_initialize();
            if (lastDFContext != null)
            {
                Fugui.SetCurrentContext(lastDFContext);
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
            if (!RenderPrepared)
            {
                return;
            }

            // count nb push at render begin
            _nbColorPushOnFrameStart = Fugui.NbPushColor;
            _nbStylePushOnFrameStart = Fugui.NbPushStyle;
            _nbFontPushOnFrameStart = Fugui.NbPushFont;
            try
            {
                OnRender?.Invoke();
                OnLastRender?.Invoke();
            }
            catch (Exception ex)
            {
                Fugui.Fire_OnUIException(ex);
            }
            finally
            {
                // pop missing push
                int nbMissingColor = Fugui.NbPushColor - _nbColorPushOnFrameStart;
                if (nbMissingColor > 0)
                {
                    Fugui.PopColor(nbMissingColor);
                }
                int nbMissingStyle = Fugui.NbPushStyle - _nbStylePushOnFrameStart;
                if (nbMissingStyle > 0)
                {
                    Fugui.PopStyle(nbMissingStyle);
                }
                int nbMissingFont = Fugui.NbPushFont - _nbFontPushOnFrameStart;
                if (nbMissingFont > 0)
                {
                    Fugui.PopFont(nbMissingFont);
                }

                ImGuiNative.igRender();
            }
            OnPostRender?.Invoke();

            // keep draw data for this context while rendering
            DrawData = ImGuiDrawListUtils.GetDrawCmd(Fugui.UIWindows, ImGui.GetDrawData());
        }

        /// <summary>
        /// Prepare render for next frame. Don't call it, Fugui layout handle it for you
        /// </summary>
        internal abstract bool PrepareRender();

        /// <summary>
        /// End the context render. Don't call it, Fugui layout handle it for you
        /// </summary>
        internal abstract bool EndRender();

        /// <summary>
        /// Destroy this context. Don't call it, Fugui layout handle it for you
        /// </summary>
        internal abstract void Destroy();

        /// /// <summary>
        /// Set this context as current. Don't call it, Fugui layout handle it for you
        /// </summary>
        public unsafe void SetAsCurrent()
        {
            Fugui.CurrentContext = this;
            ImGui.SetCurrentContext(ImGuiContext);
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
            IO.MousePos = mousePos;

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
                    return RenderPrepared = false;
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
            FontConfig fontConf = Fugui.Settings.FontConfig;
            // get global font file path
            string fontPath = Path.Combine(Application.streamingAssetsPath, fontConf.FontsFolder);
            // clear existing font atlas data
            IO.Fonts.Clear(); // Previous FontDefault reference no longer valid.
            // destroy default font pointer
            IO.NativePtr->FontDefault = default; // NULL uses Fonts[0]
            // clear fonts
            Fonts.Clear();
            // clear default font
            DefaultFont = null;

            // generate each fonts
            foreach (FontSizeConfig font in fontConf.Fonts)
            {
                // add font to font set context's dictionary
                Fonts[font.Size] = new FontSet(font.Size);

                // get and add Regular subfonts
                SubFontConfig[] subFonts = font.SubFonts_Regular.Where(f => File.Exists(fontPath + @"\" + f.FileName)).ToArray();
                if (processSubFont(font, subFonts, out ImFontPtr fontPtrRegular))
                {
                    Fonts[font.Size].Regular = fontPtrRegular;
                }

                // get and add Bold subfonts
                subFonts = font.SubFonts_Bold.Where(f => File.Exists(fontPath + @"\" + f.FileName)).ToArray();
                if (processSubFont(font, subFonts, out ImFontPtr fontPtrBold))
                {
                    Fonts[font.Size].Bold = fontPtrRegular;
                }

                // get and add Italic subfonts
                subFonts = font.SubFonts_Italic.Where(f => File.Exists(fontPath + @"\" + f.FileName)).ToArray();
                if (processSubFont(font, subFonts, out ImFontPtr fontPtrItalic))
                {
                    Fonts[font.Size].Italic = fontPtrItalic;
                }

                // set  as default if needed
                if (font.Size == fontConf.DefaultSize)
                {
                    DefaultFont = Fonts[font.Size];
                }
            }

            bool processSubFont(FontSizeConfig font, SubFontConfig[] subFonts, out ImFontPtr fontPtr)
            {
                fontPtr = default;

                // ignore this font if there is no sub fonts
                if (subFonts.Length == 0)
                {
                    return false;
                }

                // set fontPtr as default
                fontPtr = default;

                // add all subfonts
                int subFontIndex = 0;
                foreach (SubFontConfig subFont in subFonts)
                {
                    // determinate whatever this subFont use default glyph ranges (basicaly latin glyph ranges)
                    bool useDefaultGlyphRange = subFont.StartGlyph == 0 && subFont.EndGlyph == 0 && subFont.CustomGlyphRanges.Length == 0;

                    #region Prepare Glyphs Ranges
                    // prepare glyph ranges if needed
                    if (!useDefaultGlyphRange)
                    {
                        // reset glythRangePointors before processing ranges
                        subFont.GlyphRangePtr = IntPtr.Zero;

                        // get native imguiGlyphRangeBuilder ptr
                        ImFontGlyphRangesBuilder* builder = ImGuiNative.ImFontGlyphRangesBuilder_ImFontGlyphRangesBuilder();
                        // add glyph chars from custom array if it's binded
                        if (subFont.CustomGlyphRanges.Length > 0)
                        {
                            // add any glyph between min and max glyph range
                            for (int i = 0; i <= subFont.CustomGlyphRanges.Length; i++)
                            {
                                ImGuiNative.ImFontGlyphRangesBuilder_AddChar(builder, subFont.CustomGlyphRanges[i]);
                            }
                        }
                        // add glyph chars from start and end bounds
                        else
                        {
                            // add any glyph between min and max glyph range
                            for (ushort i = subFont.StartGlyph; i <= subFont.EndGlyph; i++)
                            {
                                ImGuiNative.ImFontGlyphRangesBuilder_AddChar(builder, i);
                            }
                        }
                        // create default imVector struct ref
                        ImVector vec = default;
                        // get vector ptr
                        ImVector* vecPtr = &vec;
                        // native build ranges
                        ImGuiNative.ImFontGlyphRangesBuilder_BuildRanges(builder, vecPtr);
                        // get range and keep it into managed scope (glyphRangePtr is static, because imgui lazy use glyphRangePtr)
                        subFont.GlyphRangePtr = vecPtr->Data;
                    }
                    #endregion

                    #region Prepare Font Config
                    // get default native fontConfig ptr
                    ImFontConfig* conf = ImGuiNative.ImFontConfig_ImFontConfig();
                    // keep ptr into scope throw manager struct
                    subFont.FontConfigPtr = new ImFontConfigPtr(conf);
                    // set config on merge mode
                    subFont.FontConfigPtr.MergeMode = subFontIndex > 0;
                    subFont.FontConfigPtr.GlyphOffset = subFont.GlyphOffset;
                    #endregion

                    // get font file path
                    string fontFilePath = fontPath + @"\" + subFont.FileName;
                    // add regular + icon font
                    ImFontPtr tmpFontPtr = default;

                    // add font from file without custom glyph ranges
                    if (useDefaultGlyphRange)
                    {
                        tmpFontPtr = IO.Fonts.AddFontFromFileTTF(fontFilePath, (font.Size + subFont.SizeOffset) * FontScale, subFont.FontConfigPtr);
                    }
                    // add font from file using custom glyph ranges
                    else
                    {
                        tmpFontPtr = IO.Fonts.AddFontFromFileTTF(fontFilePath, (font.Size + subFont.SizeOffset) * FontScale, subFont.FontConfigPtr, subFont.GlyphRangePtr);
                    }

                    // save font ptr if it's first time we process it
                    if (subFontIndex == 0)
                    {
                        fontPtr = tmpFontPtr;
                    }
                    // increment subFont index
                    subFontIndex++;
                }

                // return the main font pointer
                return true;
            }

            // add default font if non has been loaded
            if (Fonts.Count == 0)
            {
                IO.Fonts.AddFontDefault();
            }

            // ImGui build font atlas
            IO.Fonts.Build();
        }

        #region Drag Drop
        /// <summary>
        /// Must be placed just after an UI element so this one can be dragged
        /// </summary>
        /// <param name="payloadID">Unique ID of the payload for the drag drop operation (must be same as used in BeginDragDropTarget method)</param>
        /// <param name="dragDropFlags">lags for this drag drop operation (see ImGuiDragDropFlags on google)</param>
        /// <param name="onDraggingUICallback">Callback called each frame while a drag drop operation. Use it to draw the preview drag drop window UI)</param>
        /// <param name="payload">payload to set, will be passed to the target on Drop frame</param>
        public void BeginDragDropSource(string payloadID, ImGuiDragDropFlags dragDropFlags, Action onDraggingUICallback, object payload)
        {
            if (ImGui.BeginDragDropSource(dragDropFlags))
            {
                ImGui.SetDragDropPayload(payloadID, IntPtr.Zero, 0);
                _firstFrameDragging = !_isDraggingPayload;
                _isDraggingPayload = true;
                _draggingPayloadID = payloadID;
                _dragDropPayload = payload;
                onDraggingUICallback?.Invoke();
                ImGui.EndDragDropSource();

                // force render current window if there is some
                FuWindow.CurrentDrawingWindow?.ForceDraw();
            }
        }

        /// <summary>
        /// Must be placed just after an UI element so this can be dropped
        /// </summary>
        /// <typeparam name="T">Type of the drag drop payload to get (must be same as set as 'payload' arg in BeginDragDropSource method)</typeparam>
        /// <param name="payloadID">Unique ID of the payload for the drag drop operation (must be same as used in BeginDragDropTarget method)</param>
        /// <param name="onDropCallback">Callback called whenever the user drop the dragging payload on this UI element</param>
        public void BeginDragDropTarget<T>(string payloadID, Action<T> onDropCallback)
        {
            if (ImGui.BeginDragDropTarget())
            {
                unsafe
                {
                    ImGuiPayloadPtr payload;
                    if ((payload = ImGui.AcceptDragDropPayload(payloadID)).NativePtr != null)
                    {
                        onDropCallback?.Invoke((T)_dragDropPayload);
                        _isDraggingPayload = false;
                        _draggingPayloadID = null;
                        _dragDropPayload = null;
                    }
                    ImGui.EndDragDropTarget();
                }
            }
        }

        /// <summary>
        /// Cancel a drag drop operation related to the given payloadID
        /// </summary>
        /// <param name="payloadID">ID of the payload to cancel (keep null to cancel any current drag drop operation)</param>
        public void CancelDragDrop(string payloadID = null)
        {
            if (string.IsNullOrEmpty(payloadID) || _draggingPayloadID == payloadID)
            {
                _isDraggingPayload = false;
                _draggingPayloadID = null;
                _dragDropPayload = null;
            }
        }

        /// <summary>
        /// Get the current drag drop payload (null if there is no drag drop operation for now)
        /// </summary>
        /// <typeparam name="T">Type of the current payload</typeparam>
        /// <returns>return the current drag drop payload if there is one</returns>
        public T GetDragDropPayload<T>()
        {
            return (T)_dragDropPayload;
        }

        /// <summary>
        /// Whatever we are performing a drag drop operation right now with the given payloadID
        /// </summary>
        /// <param name="payloadID">ID of the payload (Drag Drop data ID) to check</param>
        /// <returns>true if user if performing a drag drop operation for the given payload ID</returns>
        public bool IsDraggingPayload(string payloadID)
        {
            return _isDraggingPayload && _draggingPayloadID == payloadID;
        }
        #endregion

        /// <summary>
        /// Set the scale of this context
        /// </summary>
        /// <param name="scale">global scale of this context</param>
        /// <param name="fontScale">font scale of this context</param>
        public abstract void SetScale(float scale, float fontScale);
    }

    internal class FontSet
    {
        public int Size;
        public ImFontPtr Regular;
        public ImFontPtr Bold;
        public ImFontPtr Italic;

        internal FontSet(int size)
        {
            Size = size;
        }
    }

    public enum FuguiContextType
    {
        UnityContext = 0,
        ExternalContext = 1
    }

    enum ImGuiFreeTypeBuilderFlags
    {
        NoHinting = 1 << 0,   // Disable hinting. This generally generates 'blurrier' bitmap glyphs when the glyph are rendered in any of the anti-aliased modes.
        NoAutoHint = 1 << 1,   // Disable auto-hinter.
        ForceAutoHint = 1 << 2,   // Indicates that the auto-hinter is preferred over the font's native hinter.
        LightHinting = 1 << 3,   // A lighter hinting algorithm for gray-level modes. Many generated glyphs are fuzzier but better resemble their original shape. This is achieved by snapping glyphs to the pixel grid only vertically (Y-axis), as is done by Microsoft's ClearType and Adobe's proprietary font renderer. This preserves inter-glyph spacing in horizontal text.
        MonoHinting = 1 << 4,   // Strong hinting algorithm that should only be used for monochrome output.
        Bold = 1 << 5,   // Styling: Should we artificially embolden the font?
        Oblique = 1 << 6,   // Styling: Should we slant the font, emulating italic style?
        Monochrome = 1 << 7,   // Disable anti-aliasing. Combine this with MonoHinting for best results!
        LoadColor = 1 << 8,   // Enable FreeType color-layered glyphs
        Bitmap = 1 << 9    // Enable FreeType bitmap glyphs
    };
}