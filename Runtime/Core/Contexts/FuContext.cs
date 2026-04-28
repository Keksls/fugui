#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR && !FUMOBILE
#define FUMOBILE
#endif
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Represents the Fu Context type.
    /// </summary>
    public abstract class FuContext
    {
        #region State
        public int ID;
        public ImGuiIOPtr IO;
        public ImGuiPlatformIOPtr PlatformIO;
        public IntPtr ImGuiContext;

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
        public event Action OnFramePrepared;

        public bool AutoUpdateMouse = true;
        public bool AutoUpdateKeyboard = true;
        public TextureManager TextureManager;

        public bool Started { get; protected set; }
        public float Scale { get; protected set; }
        public float FontScale { get; protected set; }

        protected DrawData _drawData = new DrawData();

        public DrawData DrawData => _drawData;
        public bool RenderPrepared { get; protected set; } = false;
        public FuContainerScaleConfig ContainerScaleConfig { get; private set; }

        internal Dictionary<int, FontSet> Fonts = new Dictionary<int, FontSet>();

        internal FontSet DefaultFont { get; set; }
        internal bool UsesSharedFontAtlas => _sharedFontAtlas != null;

        private Vector2Int _lastContainerScaleSize = new Vector2Int(-1, -1);
        private FuSharedFontAtlasCache.Entry _sharedFontAtlas;
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
        #endregion

        #region Constructors
        /// <summary>
        /// Create new imgui native contexts
        /// </summary>
        /// <param name="index">ID of the context</param>
        public FuContext(int index, float scale, float fontScale, Action onInitialize)
        {
            Scale = scale;
            FontScale = QuantizeFontScale(fontScale);
            ContainerScaleConfig = FuContainerScaleConfig.Disabled(scale, FontScale);
            ID = index;
            TextureManager = new TextureManager();
        }
        #endregion

        /// <summary>
        /// Initialize this context
        /// </summary>
        /// <param name="index"></param>
        protected void initialize(Action onInitialize)
        {
            _sharedFontAtlas = AcquireSharedFontAtlas(FontScale);
            ImGuiContext = _sharedFontAtlas != null
                ? ImGui.CreateContext(_sharedFontAtlas.Atlas)
                : ImGui.CreateContext();
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
                int offset = Fugui.GetUtf8(iniPath, nativeName, byteCount);
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
                Debug.LogWarning("[Fugui] Render called without PrepareRender being called or returning false. Skipping Render.");
                return;
            }

            // count nb push at render begin
            _nbColorPushOnFrameStart = Fugui.NbPushColor;
            _nbStylePushOnFrameStart = Fugui.NbPushStyle;
            _nbFontPushOnFrameStart = Fugui.NbPushFont;
            try
            {
                // prepare for mobile
                Fugui.BeginMobileFrame();

                OnRender?.Invoke();
                OnLastRender?.Invoke();

                // end mobile render
                Fugui.EndMobileFrame();
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
            lock (DrawData)
            {
                ImGuiDrawListUtils.GetDrawCmd(Fugui.UIWindows, ImGui.GetDrawData(), ref _drawData);
            }
            //Debug.Log(this.ID + " Rendered with " + _drawData.CmdListsCount + " Draw Lists and " + _drawData.TotalVtxCount + " vertices.");
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
        /// Configure this context to scale from its container size.
        /// </summary>
        /// <param name="config">Scaler configuration.</param>
        /// <param name="containerSize">Current container size in pixels.</param>
        public void SetContainerScaleConfig(FuContainerScaleConfig config, Vector2Int containerSize)
        {
            config.Sanitize();
            ContainerScaleConfig = config;
            _lastContainerScaleSize = new Vector2Int(-1, -1);

            if (ContainerScaleConfig.Enabled)
            {
                UpdateContainerScale(containerSize);
            }
            else
            {
                applyScaleIfNeeded(ContainerScaleConfig.BaseScale, ContainerScaleConfig.BaseFontScale, false);
            }
        }

        /// <summary>
        /// Update context scale from a container size when container scaling is enabled.
        /// </summary>
        /// <param name="containerSize">Current container size in pixels.</param>
        /// <param name="force">Force the scale application even if size did not change.</param>
        /// <returns>True when the scale changed.</returns>
        public bool UpdateContainerScale(Vector2Int containerSize, bool force = false)
        {
            if (!ContainerScaleConfig.Enabled)
            {
                return false;
            }

            if (!force && _lastContainerScaleSize == containerSize)
            {
                return false;
            }

            _lastContainerScaleSize = containerSize;
            float scaleFactor = ContainerScaleConfig.ComputeScale(containerSize);
            float targetScale = Mathf.Clamp(
                ContainerScaleConfig.BaseScale * scaleFactor,
                ContainerScaleConfig.MinScale,
                ContainerScaleConfig.MaxScale);
            float targetFontScale = ContainerScaleConfig.ScaleFont
                ? Mathf.Clamp(
                    ContainerScaleConfig.BaseFontScale * scaleFactor,
                    ContainerScaleConfig.MinScale,
                    ContainerScaleConfig.MaxScale)
                : ContainerScaleConfig.BaseFontScale;

            return applyScaleIfNeeded(targetScale, targetFontScale, force);
        }

        /// <summary>
        /// Returns the apply scale if needed result.
        /// </summary>
        /// <param name="targetScale">The target Scale value.</param>
        /// <param name="targetFontScale">The target Font Scale value.</param>
        /// <param name="force">The force value.</param>
        /// <returns>The result of the operation.</returns>
        private bool applyScaleIfNeeded(float targetScale, float targetFontScale, bool force)
        {
            targetScale = Mathf.Max(0.0001f, targetScale);
            targetFontScale = Mathf.Max(0.0001f, targetFontScale);

            if (!force &&
                Mathf.Abs(Scale - targetScale) < 0.0001f &&
                Mathf.Abs(FontScale - targetFontScale) < 0.0001f)
            {
                return false;
            }

            SetScale(targetScale, targetFontScale);
            return true;
        }

        /// <summary>
        /// Destroy this context. Don't call it, Fugui layout handle it for you
        /// </summary>
        internal virtual void Destroy()
        {
            TextureManager.Shutdown();
            FuSharedFontAtlasCache.Release(_sharedFontAtlas);
            _sharedFontAtlas = null;
        }

        /// /// <summary>
        /// Set this context as current. Don't call it, Fugui layout handle it for you
        /// </summary>
        public unsafe void SetAsCurrent()
        {
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
            IO.AddMousePosEvent(mousePos.x, mousePos.y);

            IO.AddMouseWheelEvent(mouseWheel.x, mouseWheel.y);

            IO.AddMouseButtonEvent(0, clickbtn0);
            IO.AddMouseButtonEvent(1, clickbtn1);
            IO.AddMouseButtonEvent(2, clickbtn2);
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
        /// Execute OnFramePrepared Event if it's not null
        /// </summary>
        protected void TryExecuteOnFramePreparedEvent()
        {
            OnFramePrepared?.Invoke();
        }

        /// <summary>
        /// Load fonts for this context according to FontConfig into FuGui.Settings.FontConfig
        /// </summary>
        protected void LoadFonts()
        {
            FuFontLoader.LoadFonts(
                IO,
                Fugui.Settings?.FontConfig,
                FontScale,
                Application.streamingAssetsPath,
                Fonts,
                out FontSet defaultFont,
                _loadedFontBuffers);

            DefaultFont = defaultFont;
        }

        /// <summary>
        /// Applies a shared font atlas to this context when one is available.
        /// </summary>
        /// <returns>True when the context uses a shared atlas.</returns>
        protected unsafe bool ApplySharedFontAtlas()
        {
            if (_sharedFontAtlas == null)
            {
                return false;
            }

            IO.NativePtr->Fonts = _sharedFontAtlas.Atlas.NativePtr;
            IO.NativePtr->FontDefault = default;
            Fonts.Clear();

            foreach (KeyValuePair<int, FontSet> font in _sharedFontAtlas.Fonts)
            {
                Fonts[font.Key] = font.Value;
            }

            DefaultFont = _sharedFontAtlas.DefaultFont;
            return true;
        }

        /// <summary>
        /// Switches this context to a shared atlas matching a new font scale.
        /// </summary>
        /// <param name="fontScale">Requested font scale.</param>
        /// <returns>True when a shared atlas was applied.</returns>
        protected bool SwitchSharedFontAtlas(float fontScale)
        {
            if (_sharedFontAtlas == null)
            {
                return false;
            }

            FuSharedFontAtlasCache.Entry next = AcquireSharedFontAtlas(fontScale);
            if (next == null)
            {
                return false;
            }

            FuSharedFontAtlasCache.Release(_sharedFontAtlas);
            _sharedFontAtlas = next;
            FontScale = next.FontScale;
            ApplySharedFontAtlas();
            return true;
        }

        /// <summary>
        /// Quantizes a font scale according to the current FontConfig.
        /// </summary>
        protected static float QuantizeFontScale(float fontScale)
        {
            return FuFontAtlasCache.QuantizeFontScale(Fugui.Settings?.FontConfig, fontScale);
        }

        private static FuSharedFontAtlasCache.Entry AcquireSharedFontAtlas(float fontScale)
        {
            FontConfig fontConfig = Fugui.Settings?.FontConfig;
            if (!FuSharedFontAtlasCache.IsEnabled(fontConfig))
            {
                return null;
            }

            return FuSharedFontAtlasCache.GetOrCreate(fontConfig, fontScale, Application.streamingAssetsPath);
        }
        #region State
        private readonly List<byte[]> _loadedFontBuffers = new List<byte[]>();
        #endregion

        #region Methods
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

        /// <summary>
        /// Set the scale of this context
        /// </summary>
        /// <param name="scale">global scale of this context</param>
        /// <param name="fontScale">font scale of this context</param>
        public abstract void SetScale(float scale, float fontScale);

        /// <summary>
        /// Set a temporary fake scale for this context (used to trick Fugui, eg : zoom on editor that draw UI)
        /// </summary>
        /// <param name="scale"> fake scale</param>
        public void SetTempFakeScale(float scale)
        {
            Scale = scale;
        }
        #endregion
    }
    /// <summary>
    /// Lists the available Fugui Context Type values.
    /// </summary>
    public enum FuguiContextType
    {
        UnityContext = 0,
        ExternalContext = 1
    }

    /// <summary>
    /// Lists the available Im Gui Free Type Builder Flags values.
    /// </summary>
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
