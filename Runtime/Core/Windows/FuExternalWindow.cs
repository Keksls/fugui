#if FU_EXTERNALIZATION
// Framework 4.7 compatible, IL2CPP-safe
// Requires: SDL2-CS (native SDL2 present), GLMini.cs (mini OpenGL loader)
// Renders Dear ImGui draw data into an external SDL2 OpenGL window (OpenGL 3.0 + GLSL 130)
using Fu.Framework;
using ImGuiNET;
using SDL2;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;
using static SDL2.SDL;

namespace Fu
{
    public class FuExternalWindow
    {
        public FuWindow Window;
        internal int ContextID { get; }
        public IntPtr SdlWindow { get; private set; }
        public uint SdlWindowId { get; private set; }
        public string Title { get; private set; }
        public bool CanInternalize { get; private set; } = false;
        private bool _mouseHasLeftInternalizeBounds;
        public int Width => _size.x;
        public int Height => _size.y;
        private Vector2Int _position;
        private Vector2Int _size;
        public Vector2Int Position
        {
            get => _position;
            set
            {
                _position = value;
                if (SdlWindow != IntPtr.Zero)
                    SDL_SetWindowPosition(SdlWindow, _position.x, _position.y);
            }
        }
        private Action OnClosed;

        private IntPtr _glContext;

        /// <summary>True while the external window is alive and can render.</summary>
        private volatile bool _isRunning = false;

        /// <summary>Set by the event handler when the user clicks the window close button.</summary>
        private volatile bool _shouldClose = false;

        /// <summary>Ensure Close() is executed exactly once.</summary>
        private volatile bool _isClosed = false;
        private bool _isMouseHover = false;
        public bool IsMouseHover => _isMouseHover;

        // GL objects
        private uint _vao;
        private uint _vbo;
        private uint _ebo;
        private uint _shaderProgram;
        private int _locProjMtx;
        private int _locTexture;

        // CPU-side scratch (resized on demand)
        private int _vbCapacity;
        private int _ibCapacity;
        private ImDrawVert[] _offsetVertexBuffer;

        public FuExternalWindow(FuWindow window, int contextID)
        {
            Window = window;
            ContextID = contextID;
            _position = window.WorldPosition;
            _size = window.Size;
            Title = Window.WindowName.Name;
        }

        internal void SetWindow(FuWindow window)
        {
            Window = window;
            if (window == null)
            {
                return;
            }

            Title = window.WindowName.Name;
            if (SdlWindow != IntPtr.Zero)
            {
                SDL_SetWindowTitle(SdlWindow, Title);
            }
        }

        internal void SetNativeSize(Vector2Int size)
        {
            _size = new Vector2Int(Math.Max(1, size.x), Math.Max(1, size.y));
        }

        /// <summary>
        /// Create and start the external SDL2 OpenGL window
        /// </summary>
        public void Create(bool dragOnStart, Vector2Int dragStartMouseOffset)
        {
            _mouseHasLeftInternalizeBounds = false;
            SDL_SetHint(SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH, "1");
            SDL_SetHint(SDL_HINT_MOUSE_AUTO_CAPTURE, "1");
            SDL_SetHint(SDL_HINT_WINDOWS_HANDLE_MOUSE_ACTIVATION, "1");
            if (!Fugui.EnsureSDLVideo())
            {
                return;
            }

            // GL attributes (OpenGL 3.0 core-ish; GLSL 130)
            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, 3);
            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, 0);
            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_PROFILE_MASK, 1);
            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_DOUBLEBUFFER, 1);
            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_RED_SIZE, 8);
            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_GREEN_SIZE, 8);
            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_BLUE_SIZE, 8);
            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_ALPHA_SIZE, 8);

            bool continueDragOnCreate = false;
            Vector2Int createMouseAbs = Vector2Int.zero;
            Vector2Int createMouseLocal = dragStartMouseOffset;
            if (dragOnStart)
            {
                uint mouseState = SDL_GetGlobalMouseState(out int mx, out int my);
                continueDragOnCreate = (mouseState & SDL_BUTTON(SDL_BUTTON_LEFT)) != 0;
                if (continueDragOnCreate)
                {
                    createMouseAbs = new Vector2Int(mx, my);
                    _position = ClampDragPositionToVisibleBounds(_position, createMouseAbs);
                    createMouseLocal = createMouseAbs - _position;
                }
            }

            SDL_WindowFlags flags = SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL_WindowFlags.SDL_WINDOW_RESIZABLE | SDL_WindowFlags.SDL_WINDOW_SHOWN;
            //if (Window.NoTaskBarIcon)
            //    flags |= SDL_WindowFlags.SDL_WINDOW_SKIP_TASKBAR;
            //if(!Window.UseNativeTitleBar)
            flags |= SDL_WindowFlags.SDL_WINDOW_BORDERLESS;
            if (Window.AlwaysOnTop)
                flags |= SDL_WindowFlags.SDL_WINDOW_ALWAYS_ON_TOP;
            SdlWindow = SDL_CreateWindow(
                Title,
                _position.x,
                _position.y,
                Width,
                Height,
                flags);

            if (SdlWindow == IntPtr.Zero)
            {
                Debug.LogError("SDL_CreateWindow failed: " + SDL_GetError());
                return;
            }
            SdlWindowId = SDL_GetWindowID(SdlWindow);
            int actualX = 0, actualY = 0;
            SDL_GetWindowPosition(SdlWindow, ref actualX, ref actualY);
            _position = new Vector2Int(actualX, actualY);
            int actualW = 0, actualH = 0;
            SDL_GetWindowSize(SdlWindow, ref actualW, ref actualH);
            SetNativeSize(new Vector2Int(actualW, actualH));

            // check is left click is down to start dragging right away
            if (dragOnStart)
            {
                if (continueDragOnCreate)
                {
                    SDL_CaptureMouse(SDL_bool.SDL_TRUE);
                    Vector2Int mouseAbs = createMouseAbs;
                    Vector2Int mouseLocal = createMouseLocal;
                    Position = ClampDragPositionToVisibleBounds(mouseAbs - mouseLocal, mouseAbs);
                    mouseLocal = mouseAbs - Position;
                    StartDrag(mouseLocal, mouseAbs, true);

                    // push SDL mouse left button down event to avoid missing it
                    SDL_Event evt = new SDL_Event();
                    evt.type = SDL_EventType.SDL_MOUSEBUTTONDOWN;
                    evt.button.windowID = SdlWindowId;
                    evt.button.button = (byte)SDL_BUTTON_LEFT;
                    evt.button.state = SDL_PRESSED;
                    evt.button.x = Math.Max(0, Math.Min(mouseLocal.x, Math.Max(0, Width - 1)));
                    evt.button.y = Math.Max(0, Math.Min(mouseLocal.y, Math.Max(0, Height - 1)));
                    Fugui.SDLEventRooter.Push(SdlWindowId, ref evt);
                }
                else
                {
                    Window.IsDragging = false;
                }
            }

            _glContext = SDL_GL_CreateContext(SdlWindow);
            if (_glContext == IntPtr.Zero)
            {
                Debug.LogError("SDL_GL_CreateContext failed: " + SDL_GetError());
                SDL_DestroyWindow(SdlWindow);
                return;
            }

            SDL_GL_MakeCurrent(SdlWindow, _glContext);
            GLMini.Load(name => SDL_GL_GetProcAddress(name));
            SDL_GL_SetSwapInterval(1); // vsync

            // Hook GL loader
            GLMini.GetProc = SDL_GL_GetProcAddress;

            try
            {
                GLMini.LoadAll();
            }
            catch (Exception e)
            {
                Debug.LogError("OpenGL load failed: " + e);
                CleanUp();
                return;
            }

            // Create GL pipeline (VAO/VBO/EBO + shader)
            if (!CreatePipeline())
            {
                Debug.LogError("Failed to create GL pipeline");
                CleanUp();
                return;
            }

            // Create fallback white texture
            CreateFallbackWhiteTexture();
            CreateFallbackTexturePlaceholder();

            // Register window to sdl event rooter
            Fugui.SDLEventRooter.RegisterWindow(SdlWindowId);

            _isRunning = true;
        }

        SDL_Event evt;
        public void Render()
        {
            if (!_isRunning)
                return;

            // Main loop
            while (Fugui.SDLEventRooter.Poll(SdlWindowId, out evt))
                HandleEvent(evt);

            // If a close was requested, stop running and close resources once
            if (_shouldClose)
            {
                _isRunning = false;
                CleanUp(); // idempotent (guarded inside)
                return;
            }

            // Only render if the window has been marked as dirty
            if (!Window.HasJustBeenDraw)
                return;

            // Render frame from queued batches (if any)
            RenderFrame();

            // perform buffer swap
            SDL_GL_SwapWindow(SdlWindow);

            int w = 0, h = 0;
            SDL_GetWindowSize(SdlWindow, ref w, ref h);
            SetNativeSize(new Vector2Int(w, h));
            if (PrimaryWindowFillsNativeWindow())
            {
                Window.LocalPosition = Vector2Int.zero;
                Window.Size = _size;
            }

            // clamp sdl window poisition to screen bounds
            if (!IsDragging && !IsResizing) // don't clamp while dragging/resizing
            {
                // get current position
                int x = 0, y = 0;
                SDL_GetWindowPosition(SdlWindow, ref x, ref y);
                _position = new Vector2Int(x, y);

                // get monitor bounds and clamp
                var monitorIndex = SDL_GetWindowDisplayIndex(SdlWindow);
                SDL_Rect monitorRect = default;
                SDL_GetDisplayUsableBounds(monitorIndex, ref monitorRect);
                if (!monitorRect.Equals(default)) // valid rect
                {
                    int clampedX = Math.Max(monitorRect.x, Math.Min(x, monitorRect.x + monitorRect.w - Width));
                    int clampedY = Math.Max(monitorRect.y, Math.Min(y, monitorRect.y + monitorRect.h - Height));
                    if (clampedX != x || clampedY != y)
                    {
                        // window is on top of screen bounds, let's maximize it
                        if (clampedY > y)
                        {
                            MaximizeBorderlessWindow();
                            return;
                        }

                        SDL_SetWindowPosition(SdlWindow, clampedX, clampedY);
                        _position = new Vector2Int(clampedX, clampedY);
                    }
                }
            }
        }

        /// <summary>
        /// Draw debug information about the external window
        /// </summary>
        /// <param name="layout"> The layout to draw into </param>
        public void DrawDebug(FuLayout layout)
        {
            layout.Text($"Position: {_position.x}, {_position.y}");
            layout.Text($"Size: {Width} x {Height}");
            layout.Separator();

            // mouse position and state
            Vector2 mousePos = Window.Container.Context.IO.MousePos;
            layout.Text($"Mouse Pos: {mousePos.x}, {mousePos.y}");
            layout.Text($"Mouse Buttons: L[{(Window.Mouse.IsPressed(FuMouseButton.Left) ? "X" : " ")}] " +
            $"M[{(Window.Mouse.IsPressed(FuMouseButton.Center) ? "X" : " ")}] R[{(Window.Mouse.IsPressed(FuMouseButton.Right) ? "X" : " ")}]");
        }

        /// <summary>
        /// Signal the window to close
        /// </summary>
        public void Close(Action onClosed)
        {
            if (_isClosed)
            {
                onClosed?.Invoke();
                return;
            }
            OnClosed = onClosed;
            _shouldClose = true;
        }

        /// <summary>
        /// Cleanup GL and SDL resources
        /// </summary>
        private void CleanUp()
        {
            if (_isClosed) return; // already closed
            _isClosed = true;

            try
            {
                // Delete GL textures we created via our registry
                foreach (var kv in _registeredTextures)
                {
                    uint id = kv.Value;
                    if (id != 0) glDeleteTexture(id);
                }
                _registeredTextures.Clear();

                // Also delete fallback textures
                if (_fallbackWhiteTex != 0) { glDeleteTexture(_fallbackWhiteTex); _fallbackWhiteTex = 0; }
                if (_fallbackTexturePlaceholderTex != 0) { glDeleteTexture(_fallbackTexturePlaceholderTex); _fallbackTexturePlaceholderTex = 0; }

                // GL objects
                if (_shaderProgram != 0) { GLMini.glUseProgram(0); glDeleteProgram(_shaderProgram); _shaderProgram = 0; }
                if (_vbo != 0) { glDeleteBuffer(_vbo); _vbo = 0; }
                if (_ebo != 0) { glDeleteBuffer(_ebo); _ebo = 0; }
                if (_vao != 0) { glDeleteVertexArray(_vao); _vao = 0; }

                // Unbind current before destroying the context
                if (SdlWindow != IntPtr.Zero)
                    SDL_GL_MakeCurrent(SdlWindow, IntPtr.Zero);

                if (_glContext != IntPtr.Zero)
                {
                    SDL_GL_DeleteContext(_glContext);
                    _glContext = IntPtr.Zero;
                }

                if (SdlWindow != IntPtr.Zero)
                {
                    SDL_DestroyWindow(SdlWindow);
                    SdlWindow = IntPtr.Zero;
                }

                Fugui.RemoveExternalWindow(Window, ContextID);

                // unregister from SDL event rooter
                Fugui.SDLEventRooter.UnregisterWindow(SdlWindowId);
            }
            catch (Exception e)
            {
                Debug.LogError("Error during external window cleanup: " + e);
            }
            finally
            {
                OnClosed?.Invoke();
            }
        }

        /// <summary>
        /// Update the window (make context current, update events)
        /// </summary>
        public void Update()
        {
            // make context current
            SDL_GL_MakeCurrent(SdlWindow, _glContext);
            // update SDL events
            Fugui.SDLEventRooter.Update();

            // get global mouse position
            int mx, my;
            SDL_GetGlobalMouseState(out mx, out my);
            Vector2Int absMousePos = new Vector2Int(mx, my);
            Vector2Int windowPos = Position;
            Vector2Int windowSize = _size;
            _isMouseHover = absMousePos.x >= windowPos.x && absMousePos.x < windowPos.x + windowSize.x &&
                             absMousePos.y >= windowPos.y && absMousePos.y < windowPos.y + windowSize.y;
        }

        // Fallback white texture (1x1) used when a texture is missing
        private uint _fallbackWhiteTex = 0;
        // Visible placeholder used when an image texture cannot be copied into the external GL context
        private uint _fallbackTexturePlaceholderTex = 0;
        // Keep for compatibility with rest of the pipeline
        private readonly Dictionary<IntPtr, uint> _registeredTextures = new Dictionary<IntPtr, uint>();
        // Per-unityId state
        private readonly Dictionary<IntPtr, PBOPair> _gpu = new Dictionary<IntPtr, PBOPair>();
        private readonly Dictionary<IntPtr, ReadbackState> _rb = new Dictionary<IntPtr, ReadbackState>();
        private readonly HashSet<IntPtr> _textureUploadFailures = new HashSet<IntPtr>();
        private readonly HashSet<IntPtr> _textureUploadWarnings = new HashSet<IntPtr>();

        // ===== Runtime structures =====
        private sealed class PBOPair
        {
            public uint glTex;
            public readonly uint[] pbo = new uint[2];
            public int index = 0;
            public int w, h;
            public int byteSize => w * h * 4;
        }

        // Async GPU → CPU readback per Unity texture (RenderTexture only)
        private sealed class ReadbackState
        {
            public AsyncGPUReadbackRequest request;
            public bool requested = false;
            public Queue<NativeArray<byte>> ready = new Queue<NativeArray<byte>>(); // frames awaiting upload
            public int w, h;
        }

        /// <summary>
        /// Create a fallback white texture (1x1 white pixel) for ImGui usage.
        /// </summary>
        private void CreateFallbackWhiteTexture()
        {
            GLMini.glGenTextures(1, out _fallbackWhiteTex);
            GLMini.glBindTexture(GLMini.GL_TEXTURE_2D, _fallbackWhiteTex);

            GLMini.glTexParameteri(GLMini.GL_TEXTURE_2D, GLMini.GL_TEXTURE_MIN_FILTER, (int)GLMini.GL_LINEAR);
            GLMini.glTexParameteri(GLMini.GL_TEXTURE_2D, GLMini.GL_TEXTURE_MAG_FILTER, (int)GLMini.GL_LINEAR);
            GLMini.glTexParameteri(GLMini.GL_TEXTURE_2D, GLMini.GL_TEXTURE_WRAP_S, (int)GLMini.GL_CLAMP_TO_EDGE);
            GLMini.glTexParameteri(GLMini.GL_TEXTURE_2D, GLMini.GL_TEXTURE_WRAP_T, (int)GLMini.GL_CLAMP_TO_EDGE);

            GLMini.glPixelStorei(GLMini.GL_UNPACK_ALIGNMENT, 1);

            unsafe
            {
                byte* white = stackalloc byte[4] { 255, 255, 255, 255 };
                GLMini.glTexImage2D(GLMini.GL_TEXTURE_2D, 0, (int)GLMini.GL_RGBA,
                    1, 1, 0, GLMini.GL_RGBA, GLMini.GL_UNSIGNED_BYTE, (IntPtr)white);
            }

            GLMini.glBindTexture(GLMini.GL_TEXTURE_2D, 0);
        }

        /// <summary>
        /// Create a visible placeholder texture for external images that cannot be uploaded.
        /// </summary>
        private unsafe void CreateFallbackTexturePlaceholder()
        {
            GLMini.glGenTextures(1, out _fallbackTexturePlaceholderTex);
            GLMini.glBindTexture(GLMini.GL_TEXTURE_2D, _fallbackTexturePlaceholderTex);

            GLMini.glTexParameteri(GLMini.GL_TEXTURE_2D, GLMini.GL_TEXTURE_MIN_FILTER, (int)GLMini.GL_LINEAR);
            GLMini.glTexParameteri(GLMini.GL_TEXTURE_2D, GLMini.GL_TEXTURE_MAG_FILTER, (int)GLMini.GL_LINEAR);
            GLMini.glTexParameteri(GLMini.GL_TEXTURE_2D, GLMini.GL_TEXTURE_WRAP_S, (int)GLMini.GL_CLAMP_TO_EDGE);
            GLMini.glTexParameteri(GLMini.GL_TEXTURE_2D, GLMini.GL_TEXTURE_WRAP_T, (int)GLMini.GL_CLAMP_TO_EDGE);
            GLMini.glPixelStorei(GLMini.GL_UNPACK_ALIGNMENT, 1);

            const int size = 16;
            byte[] pixels = new byte[size * size * 4];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool checker = ((x / 4) + (y / 4)) % 2 == 0;
                    int i = (y * size + x) * 4;
                    pixels[i] = checker ? (byte)255 : (byte)55;
                    pixels[i + 1] = checker ? (byte)0 : (byte)55;
                    pixels[i + 2] = checker ? (byte)255 : (byte)55;
                    pixels[i + 3] = 255;
                }
            }

            fixed (byte* ptr = pixels)
            {
                GLMini.glTexImage2D(GLMini.GL_TEXTURE_2D, 0, (int)GLMini.GL_RGBA,
                    size, size, 0, GLMini.GL_RGBA, GLMini.GL_UNSIGNED_BYTE, (IntPtr)ptr);
            }

            GLMini.glBindTexture(GLMini.GL_TEXTURE_2D, 0);
        }

        private uint GetTexturePlaceholder()
        {
            return _fallbackTexturePlaceholderTex != 0 ? _fallbackTexturePlaceholderTex : _fallbackWhiteTex;
        }

        private void WarnTextureUploadFallback(IntPtr unityId, Texture texture, string reason)
        {
            if (!_textureUploadWarnings.Add(unityId))
                return;

            string textureName = texture != null && !string.IsNullOrEmpty(texture.name) ? texture.name : "<unnamed>";
            int width = texture != null ? texture.width : 0;
            int height = texture != null ? texture.height : 0;
            Debug.LogWarning($"External window cannot upload texture '{textureName}' ({width}x{height}): {reason}. Drawing a placeholder instead.");
        }

        /// <summary>
        /// Ensure we have a GL texture + double PBOs for this Unity texture (created in our SDL GL context).
        /// </summary>
        private PBOPair EnsurePBOPair(IntPtr unityId, int w, int h)
        {
            if (_gpu.TryGetValue(unityId, out var gpu))
            {
                if (gpu.w == w && gpu.h == h) return gpu;

                // Size changed → recreate (simple path)
                gpu = null;
                _gpu.Remove(unityId);
            }

            var pair = new PBOPair { w = w, h = h };

            // GL texture
            GLMini.glGenTextures(1, out pair.glTex);
            GLMini.glBindTexture(GLMini.GL_TEXTURE_2D, pair.glTex);
            GLMini.glTexParameteri(GLMini.GL_TEXTURE_2D, GLMini.GL_TEXTURE_MIN_FILTER, (int)GLMini.GL_LINEAR);
            GLMini.glTexParameteri(GLMini.GL_TEXTURE_2D, GLMini.GL_TEXTURE_MAG_FILTER, (int)GLMini.GL_LINEAR);
            GLMini.glTexParameteri(GLMini.GL_TEXTURE_2D, GLMini.GL_TEXTURE_WRAP_S, (int)GLMini.GL_CLAMP_TO_EDGE);
            GLMini.glTexParameteri(GLMini.GL_TEXTURE_2D, GLMini.GL_TEXTURE_WRAP_T, (int)GLMini.GL_CLAMP_TO_EDGE);
            GLMini.glPixelStorei(GLMini.GL_UNPACK_ALIGNMENT, 1);
            GLMini.glTexImage2D(GLMini.GL_TEXTURE_2D, 0, (int)GLMini.GL_RGBA, w, h, 0, GLMini.GL_RGBA, GLMini.GL_UNSIGNED_BYTE, IntPtr.Zero);

            // Double PBO
            GLMini.glGenBuffers(2, out pair.pbo[0]);
            for (int i = 0; i < 2; i++)
            {
                GLMini.glBindBuffer(GLMini.GL_PIXEL_UNPACK_BUFFER, pair.pbo[i]);
                GLMini.glBufferData(GLMini.GL_PIXEL_UNPACK_BUFFER, (IntPtr)pair.byteSize, IntPtr.Zero, GLMini.GL_STREAM_DRAW);
            }
            GLMini.glBindBuffer(GLMini.GL_PIXEL_UNPACK_BUFFER, 0);

            _gpu[unityId] = pair;
            _registeredTextures[unityId] = pair.glTex; // we own this GL texture (will be deleted in cleanup)
            return pair;
        }

        /// <summary>
        /// CPU → GPU upload using double PBO (no stalls).
        /// </summary>
        private unsafe void UploadWithPBO(PBOPair gpu, IntPtr src, int byteLen)
        {
            int next = (gpu.index + 1) % 2;

            // Fill current PBO
            GLMini.glBindBuffer(GLMini.GL_PIXEL_UNPACK_BUFFER, gpu.pbo[gpu.index]);
            IntPtr ptr = GLMini.glMapBufferRange(
                GLMini.GL_PIXEL_UNPACK_BUFFER,
                IntPtr.Zero,
                (IntPtr)byteLen,
                GLMini.GL_MAP_WRITE_BIT | GLMini.GL_MAP_INVALIDATE_BUFFER_BIT);

            if (ptr != IntPtr.Zero)
            {
                Buffer.MemoryCopy((void*)src, (void*)ptr, byteLen, byteLen);
                GLMini.glUnmapBuffer(GLMini.GL_PIXEL_UNPACK_BUFFER);
            }

            // Issue async upload from the other PBO
            GLMini.glBindBuffer(GLMini.GL_PIXEL_UNPACK_BUFFER, gpu.pbo[next]);
            GLMini.glBindTexture(GLMini.GL_TEXTURE_2D, gpu.glTex);
            GLMini.glPixelStorei(GLMini.GL_UNPACK_ALIGNMENT, 1);
            GLMini.glTexSubImage2D(GLMini.GL_TEXTURE_2D, 0, 0, 0, gpu.w, gpu.h, GLMini.GL_RGBA, GLMini.GL_UNSIGNED_BYTE, IntPtr.Zero);

            // Cleanup
            GLMini.glBindBuffer(GLMini.GL_PIXEL_UNPACK_BUFFER, 0);
            gpu.index = next;
        }

        /// <summary>
        /// For Texture2D readable: zero-allocation fast path (no ToArray()).
        /// </summary>
        private unsafe void UploadTexture2D(IntPtr unityId, Texture2D t2d)
        {
            if (!t2d.isReadable)
                return;

            NativeArray<byte> raw;
            try
            {
                raw = t2d.GetRawTextureData<byte>(); // NativeArray<byte>
            }
            catch
            {
                return;
            }

            if (!raw.IsCreated || raw.Length == 0) return;

            PBOPair gpu = EnsurePBOPair(unityId, t2d.width, t2d.height);
            void* src = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(raw);
            UploadWithPBO(gpu, (IntPtr)src, raw.Length);
        }

        /// <summary>
        /// Upload a Unity Texture2D into the currently bound GL texture.
        /// Alpha-only textures are expanded to white RGBA so ImGui's standard shader can tint them.
        /// </summary>
        private unsafe bool UploadTexture2DImage(IntPtr unityId, Texture2D t2d)
        {
            if (!t2d.isReadable)
            {
                WarnTextureUploadFallback(unityId, t2d, $"texture is not readable (format: {t2d.format})");
                return false;
            }

            NativeArray<byte> raw;
            try
            {
                raw = t2d.GetRawTextureData<byte>();
            }
            catch (Exception e)
            {
                WarnTextureUploadFallback(unityId, t2d, e.Message);
                return false;
            }

            if (!raw.IsCreated || raw.Length == 0)
            {
                WarnTextureUploadFallback(unityId, t2d, $"texture has no readable pixel data (format: {t2d.format})");
                return false;
            }

            int pixelCount = t2d.width * t2d.height;
            GLMini.glPixelStorei(GLMini.GL_UNPACK_ALIGNMENT, 1);

            if (t2d.format == TextureFormat.Alpha8 || raw.Length == pixelCount)
            {
                byte[] rgba = new byte[pixelCount * 4];
                for (int src = 0, dst = 0; src < pixelCount; src++, dst += 4)
                {
                    byte alpha = raw[src];
                    rgba[dst] = 255;
                    rgba[dst + 1] = 255;
                    rgba[dst + 2] = 255;
                    rgba[dst + 3] = alpha;
                }

                fixed (byte* ptr = rgba)
                {
                    GLMini.glTexImage2D(GLMini.GL_TEXTURE_2D, 0, (int)GLMini.GL_RGBA,
                        t2d.width, t2d.height, 0, GLMini.GL_RGBA, GLMini.GL_UNSIGNED_BYTE,
                        (IntPtr)ptr);
                }
                return true;
            }

            if (raw.Length < pixelCount * 4)
            {
                WarnTextureUploadFallback(unityId, t2d, $"unsupported readable texture format {t2d.format}");
                return false;
            }

            void* srcPtr = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(raw);
            GLMini.glTexImage2D(GLMini.GL_TEXTURE_2D, 0, (int)GLMini.GL_RGBA,
                t2d.width, t2d.height, 0, GLMini.GL_RGBA, GLMini.GL_UNSIGNED_BYTE,
                (IntPtr)srcPtr);
            return true;
        }

        /// <summary>
        /// For RenderTexture: fully async GPU→CPU using AsyncGPUReadback, then CPU→GPU PBO upload without stalls.
        /// </summary>
        private void PumpRenderTextureReadback(IntPtr unityId, RenderTexture rt)
        {
            if (!_rb.TryGetValue(unityId, out var state))
            {
                state = new ReadbackState { w = rt.width, h = rt.height };
                _rb[unityId] = state;
            }

            // Handle size change
            if (state.w != rt.width || state.h != rt.height)
            {
                // Drop old pending frames
                while (state.ready.Count > 0)
                {
                    var na = state.ready.Dequeue();
                    if (na.IsCreated) na.Dispose();
                }
                state.w = rt.width; state.h = rt.height;
                state.requested = false;
            }

            // If no request in flight, schedule one
            if (!state.requested)
            {
                // RT must be RGBA32-like; if not, Graphics.Blit into an intermediate ARGB32 RT upstream.
                state.request = AsyncGPUReadback.Request(rt, 0, TextureFormat.RGBA32, req =>
                {
                    if (req.hasError) return;
                    var data = req.GetData<byte>(); // NativeArray<byte>
                                                    // Keep a copy-owned NativeArray we must dispose after upload
                    var copy = new NativeArray<byte>(data.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                    copy.CopyFrom(data);
                    state.ready.Enqueue(copy);
                });
                state.requested = true;
            }
            else
            {
                // If completed, allow issuing a new one next frame
                if (state.request.done) state.requested = false;
            }
        }

        /// <summary>
        /// Consume one ready frame (if any) and upload via PBO.
        /// </summary>
        private unsafe void ConsumeRenderTextureFrame(IntPtr unityId, int w, int h)
        {
            if (!_rb.TryGetValue(unityId, out var state)) return;
            if (state.ready.Count == 0) return;

            var na = state.ready.Dequeue();
            try
            {
                PBOPair gpu = EnsurePBOPair(unityId, w, h);
                void* src = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(na);
                UploadWithPBO(gpu, (IntPtr)src, na.Length);
            }
            finally
            {
                if (na.IsCreated) na.Dispose();
            }
        }

        /// <summary>
        /// Get the GL texture to bind for ImGui (portable path).
        /// </summary>
        public uint GetRegisteredTexture(IntPtr unityId)
        {
            if (!Window.Container.Context.TextureManager.TryGetTexture(unityId, out Texture tex) || tex == null)
                return _fallbackWhiteTex;

            // Texture2D (readable): direct NativeArray → PBO upload (no stalls)
            if (tex is Texture2D t2d)
            {
                if (_textureUploadFailures.Contains(unityId))
                    return GetTexturePlaceholder();

                if (!_gpu.TryGetValue(unityId, out var gpu))
                {
                    // Create GL texture once
                    uint texId;
                    GLMini.glGenTextures(1, out texId);
                    GLMini.glBindTexture(GLMini.GL_TEXTURE_2D, texId);

                    GLMini.glTexParameteri(GLMini.GL_TEXTURE_2D, GLMini.GL_TEXTURE_MIN_FILTER, (int)GLMini.GL_LINEAR);
                    GLMini.glTexParameteri(GLMini.GL_TEXTURE_2D, GLMini.GL_TEXTURE_MAG_FILTER, (int)GLMini.GL_LINEAR);
                    GLMini.glTexParameteri(GLMini.GL_TEXTURE_2D, GLMini.GL_TEXTURE_WRAP_S, (int)GLMini.GL_CLAMP_TO_EDGE);
                    GLMini.glTexParameteri(GLMini.GL_TEXTURE_2D, GLMini.GL_TEXTURE_WRAP_T, (int)GLMini.GL_CLAMP_TO_EDGE);

                    bool uploaded;
                    try
                    {
                        uploaded = UploadTexture2DImage(unityId, t2d);
                    }
                    catch (Exception e)
                    {
                        WarnTextureUploadFallback(unityId, t2d, e.Message);
                        uploaded = false;
                    }

                    GLMini.glBindTexture(GLMini.GL_TEXTURE_2D, 0);

                    if (!uploaded)
                    {
                        _textureUploadFailures.Add(unityId);
                        glDeleteTexture(texId);
                        return GetTexturePlaceholder();
                    }

                    // Store it as if it were a PBOPair, but minimal
                    _gpu[unityId] = new PBOPair { glTex = texId, w = t2d.width, h = t2d.height };
                    _registeredTextures[unityId] = texId;
                }

                return _gpu[unityId].glTex;
            }

            // RenderTexture: schedule/consume async readbacks
            if (tex is RenderTexture rt)
            {
                PumpRenderTextureReadback(unityId, rt);                  // schedule GPU→CPU if possible
                ConsumeRenderTextureFrame(unityId, rt.width, rt.height); // upload one completed frame if ready
                return EnsurePBOPair(unityId, rt.width, rt.height).glTex;
            }

            // Unsupported types → fallback
            return _fallbackWhiteTex;
        }

        /// <summary>
        /// Handle an SDL event
        /// </summary>
        /// <param name="e"> the event </param>
        private void HandleEvent(SDL_Event e)
        {
            // Window close
            if (e.type == SDL_EventType.SDL_QUIT ||
               (e.type == SDL_EventType.SDL_WINDOWEVENT && e.window.windowEvent == SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE))
            {
                Close(null);
            }

            if (e.type == SDL.SDL_EventType.SDL_WINDOWEVENT)
            {
                switch (e.window.windowEvent)
                {
                    // Window resize
                    case SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED:
                        SetNativeSize(new Vector2Int(e.window.data1, e.window.data2));
                        if (PrimaryWindowFillsNativeWindow())
                        {
                            Window.Size = _size;
                        }
                        Fugui.ForceDrawAllWindows(2);
                        break;

                    // Window moved
                    case SDL_WindowEventID.SDL_WINDOWEVENT_MOVED:
                        Position = new Vector2Int(e.window.data1, e.window.data2);
                        break;

                        //// Mouse enter
                        //case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_ENTER:
                        //    _isMouseHover = true;
                        //    break;

                        //// Mouse leave
                        //case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_LEAVE:
                        //    _isMouseHover = false;
                        //    break;
                }
            }

            // Forward event to rooter for ImGui processing
            if (e.type == SDL_EventType.SDL_MOUSEBUTTONDOWN)
            {
                Fugui.SDLEventRooter.Push(SdlWindowId, ref e);
            }
        }

        public bool IsDragging { get; private set; } = false;
        private Vector2Int dragStartMousePos;
        private Vector2Int dragStartWindowPos;

        internal bool ShouldAutoInternalize(Rect mainContainerRect, Vector2Int mouseAbs)
        {
            if (!CanInternalize || !IsDragging)
            {
                return false;
            }

            if (!mainContainerRect.Contains(mouseAbs))
            {
                _mouseHasLeftInternalizeBounds = true;
                return false;
            }

            return _mouseHasLeftInternalizeBounds;
        }

        public ResizeEdge HoverResizeEdge { get; private set; } = ResizeEdge.None;
        public bool IsResizing { get; private set; } = false;
        private Vector2Int resizeStartMousePos;
        private Vector2Int resizeStartWindowPos;
        private Vector2Int resizeStartWindowSize;
        private bool _leftMouseWasPressed;

        private ResizeEdge currentResizeEdge = ResizeEdge.None;

        /// <summary>
        /// Handle window dragging via title bar
        /// Handle window raising on mouse down
        /// Handle SDL events
        /// Handle resizing
        /// </summary>
        public void UpdateManipulation()
        {
            if (!_isRunning || _shouldClose || _isClosed)
                return;

            SDL.SDL_GetMouseState(out int mx, out int my);
            uint globalMouseState = SDL.SDL_GetGlobalMouseState(out int gx, out int gy);
            bool leftMousePhysicalPressed = (globalMouseState & SDL.SDL_BUTTON(SDL.SDL_BUTTON_LEFT)) != 0;
            bool leftMouseDown = leftMousePhysicalPressed && !_leftMouseWasPressed;
            bool leftMousePressedBeforeHover = leftMousePhysicalPressed && !leftMouseDown;
            _leftMouseWasPressed = leftMousePhysicalPressed;

            if (!IsResizing && !IsDragging && !_isMouseHover)
            {
                HoverResizeEdge = ResizeEdge.None;
                currentResizeEdge = ResizeEdge.None;
                return;
            }

            Vector2Int mouseLocal = new Vector2Int(mx, my);
            Vector2Int mouseAbs = new Vector2Int(gx, gy);
            Vector2Int windowSize = _size;
            bool leftMousePressed = Window.Mouse.IsPressed(FuMouseButton.Left) || leftMousePhysicalPressed;
            bool mouseBlockedByPopup = Fugui.IsInsideAnyPopup(mouseLocal);

            //
            // --- RESIZE ---
            //
            // Detect hover edge (even when not resizing)
            if (!IsDragging && !IsResizing)
            {
                if (IsMaximized || mouseBlockedByPopup || leftMousePressedBeforeHover)
                    HoverResizeEdge = ResizeEdge.None;   // interdit resize
                else
                    HoverResizeEdge = GetHoveredResizeEdge(mouseLocal, windowSize);
            }
            else
            {
                // while dragging or resizing, force hover edge to None
                HoverResizeEdge = ResizeEdge.None;
            }

            if (HoverResizeEdge != ResizeEdge.None || IsResizing)
            {
                Fugui.BlockWindowInputsForFrame();
            }

            // detect edge if not resizing or dragging
            if (!IsMaximized && !mouseBlockedByPopup && !IsDragging && !IsResizing && leftMouseDown)
            {
                currentResizeEdge = GetHoveredResizeEdge(mouseLocal, windowSize);

                if (currentResizeEdge != ResizeEdge.None)
                {
                    // begin resize
                    IsResizing = true;
                    resizeStartMousePos = mouseAbs;
                    resizeStartWindowPos = Position;
                    resizeStartWindowSize = windowSize;
                }
            }

            // continue resize
            if (IsResizing)
            {
                if (leftMousePressed)
                {
                    Vector2Int delta = mouseAbs - resizeStartMousePos;

                    Vector2Int newPos = resizeStartWindowPos;
                    Vector2Int newSize = resizeStartWindowSize;

                    switch (currentResizeEdge)
                    {
                        case ResizeEdge.Left:
                            newPos.x += delta.x;
                            newSize.x -= delta.x;
                            break;

                        case ResizeEdge.Right:
                            newSize.x += delta.x;
                            break;

                        case ResizeEdge.Bottom:
                            newSize.y += delta.y;
                            break;

                        case ResizeEdge.BottomLeft:
                            newPos.x += delta.x;
                            newSize.x -= delta.x;
                            newSize.y += delta.y;
                            break;

                        case ResizeEdge.BottomRight:
                            newSize.x += delta.x;
                            newSize.y += delta.y;
                            break;
                    }

                    // enforce minimum
                    newSize.x = Math.Max(newSize.x, 50);
                    newSize.y = Math.Max(newSize.y, 50);

                    // assign
                    Position = newPos;
                    SetNativeSize(newSize);
                    if (PrimaryWindowFillsNativeWindow())
                    {
                        Window.Size = _size;
                    }
                    Fugui.ForceDrawAllWindows(2);
                    SDL_SetWindowSize(SdlWindow, newSize.x, newSize.y);
                }
                else
                {
                    // stop resizing
                    IsResizing = false;
                    if (FuWindow.InputFocusedWindow == Window)
                    {
                        FuWindow.InputFocusedWindow = null;
                        FuWindow.NbInputFocusedWindow = 0;
                    }
                    currentResizeEdge = ResizeEdge.None;
                }
            }
            else
            {
                currentResizeEdge = ResizeEdge.None;
            }

            //
            // --- DRAG ---
            //
            if (Window.Mouse.IsDown(FuMouseButton.Left))
            {
                StartDrag(mouseLocal, mouseAbs);
            }

            if (IsDragging)
            {
                if (leftMousePressed)
                {
                    Vector2Int delta = mouseAbs - dragStartMousePos;
                    Position = ClampDragPositionToVisibleBounds(dragStartWindowPos + delta, mouseAbs);
                    Window.Fire_OnDrag();
                }
                else
                {
                    IsDragging = false;
                    Window.IsDragging = false;
                    CanInternalize = true;
                    SDL_CaptureMouse(SDL_bool.SDL_FALSE);
                }
            }

            //
            // --- RAISE WINDOW ---
            //
            if (Window.Mouse.IsDown(FuMouseButton.Left) || Window.Mouse.IsDown(FuMouseButton.Right) || Window.Mouse.IsDown(FuMouseButton.Center))
            {
                RaiseWindow();
            }
        }

        private void RaiseWindow()
        {
            if (SdlWindow == IntPtr.Zero)
                return;

            if (Window.AlwaysOnTop)
            {
                SDL_SetWindowAlwaysOnTop(SdlWindow, SDL_bool.SDL_TRUE);
                return;
            }

            SDL_SetWindowAlwaysOnTop(SdlWindow, SDL_bool.SDL_FALSE);
            SDL_RaiseWindow(SdlWindow);
        }

        private bool PrimaryWindowFillsNativeWindow()
        {
            return Window != null;
        }

        private Vector2Int ClampDragPositionToVisibleBounds(Vector2Int desiredPosition, Vector2Int mouseAbs)
        {
            if (!TryGetDisplayUsableBounds(mouseAbs, out SDL_Rect bounds))
            {
                return desiredPosition;
            }

            Vector2Int size = _size;
            int minVisibleWidth = Math.Min(Math.Max(64, (int)(96f * Fugui.Scale)), Math.Max(1, size.x));
            int minVisibleHeight = Math.Min(Math.Max(24, (int)(32f * Fugui.Scale)), Math.Max(1, size.y));

            int minX = bounds.x - size.x + minVisibleWidth;
            int maxX = bounds.x + bounds.w - minVisibleWidth;
            int minY = bounds.y;
            int maxY = bounds.y + bounds.h - minVisibleHeight;

            return new Vector2Int(
                Math.Max(minX, Math.Min(desiredPosition.x, maxX)),
                Math.Max(minY, Math.Min(desiredPosition.y, maxY)));
        }

        private bool TryGetDisplayUsableBounds(Vector2Int point, out SDL_Rect bounds)
        {
            bounds = default;
            int displayCount = SDL_GetNumVideoDisplays();
            if (displayCount <= 0)
            {
                return false;
            }

            int fallbackDisplay = 0;
            long fallbackDistance = long.MaxValue;
            for (int i = 0; i < displayCount; i++)
            {
                SDL_Rect rect = default;
                if (SDL_GetDisplayUsableBounds(i, ref rect) != 0)
                {
                    continue;
                }

                if (point.x >= rect.x && point.x < rect.x + rect.w && point.y >= rect.y && point.y < rect.y + rect.h)
                {
                    bounds = rect;
                    return true;
                }

                long centerX = rect.x + rect.w / 2;
                long centerY = rect.y + rect.h / 2;
                long dx = point.x - centerX;
                long dy = point.y - centerY;
                long distance = dx * dx + dy * dy;
                if (distance < fallbackDistance)
                {
                    fallbackDistance = distance;
                    fallbackDisplay = i;
                    bounds = rect;
                }
            }

            return fallbackDistance != long.MaxValue && SDL_GetDisplayUsableBounds(fallbackDisplay, ref bounds) == 0;
        }

        private void StartDrag(Vector2Int mouseLocal, Vector2Int mouseAbs, bool forceMouseDown = false)
        {
            float titleBarHeight = Window.WorkingAreaPosition.y;
            float titleBarWidth = Width - (64f * Fugui.Scale);

            bool inTitleBar =
                mouseLocal.y < titleBarHeight &&
                mouseLocal.x < titleBarWidth;

            // pas dans la title bar → pas de drag
            if (IsResizing || (!forceMouseDown && !inTitleBar))
                return;

            if (!IsDragging && (forceMouseDown || Window.Mouse.IsDown(FuMouseButton.Left)))
            {
                if (IsMaximized)
                {
                    // 1. On calcule la fraction horizontale du click dans la titlebar
                    float ratio = (float)mouseLocal.x / (float)Math.Max(1, Width);

                    // 2. Restore immédiatement
                    RestoreBorderlessWindow();

                    // 3. Positionner la fenêtre sous la souris
                    //    (important pour un drag naturel)
                    int newX = mouseAbs.x - (int)(restoreRect.w * ratio);
                    int newY = mouseAbs.y - (int)(titleBarHeight * 0.5f);

                    Position = new Vector2Int(newX, newY);
                }

                // 4. Commencer drag normal
                IsDragging = true;
                Window.IsDragging = true;
                CanInternalize = true;
                dragStartMousePos = mouseAbs;
                dragStartWindowPos = Position;
            }
        }

        private float RESIZE_BORDER => 5 * Fugui.Scale;
        private float RESIZE_CORNER_SIZE => 18 * Fugui.Scale;
        private ResizeEdge GetHoveredResizeEdge(Vector2Int mouseLocal, Vector2Int windowSize)
        {
            bool inLeftCorner = mouseLocal.x <= RESIZE_CORNER_SIZE;
            bool inRightCorner = mouseLocal.x >= windowSize.x - RESIZE_CORNER_SIZE;
            bool inBottomCorner = mouseLocal.y >= windowSize.y - RESIZE_CORNER_SIZE;

            // Corners first (using corner size)
            if (inBottomCorner && inLeftCorner) return ResizeEdge.BottomLeft;
            if (inBottomCorner && inRightCorner) return ResizeEdge.BottomRight;

            // Borders (using border size)
            bool left = mouseLocal.x <= RESIZE_BORDER;
            bool right = mouseLocal.x >= windowSize.x - RESIZE_BORDER;
            bool bottom = mouseLocal.y >= windowSize.y - RESIZE_BORDER;

            if (left) return ResizeEdge.Left;
            if (right) return ResizeEdge.Right;
            if (bottom) return ResizeEdge.Bottom;

            return ResizeEdge.None;
        }

        public void DrawResizeHandles()
        {
            if (!_isRunning || _shouldClose || _isClosed)
                return;

            // check if mouse is inside window
            if (!IsResizing && !IsDragging && !_isMouseHover)
            {
                HoverResizeEdge = ResizeEdge.None;
                currentResizeEdge = ResizeEdge.None;
                return;
            }

            var dl = ImGui.GetForegroundDrawList();

            Vector2 windowSize = new Vector2(Width, Height);

            // colors
            uint normalColor = Fugui.Themes.GetColorU32(FuColors.Highlight, 0.45f);
            uint hoverColor = Fugui.Themes.GetColorU32(FuColors.HighlightHovered);
            uint activeColor = Fugui.Themes.GetColorU32(FuColors.HighlightActive);

            // thickness
            float normalThickness = 1f * Fugui.Scale;
            float hoverThickness = 8f * Fugui.Scale;
            float activeThickness = 4f * Fugui.Scale;

            // get hovered or active edge
            ResizeEdge edge = HoverResizeEdge;
            if (IsResizing)
                edge = currentResizeEdge;

            //
            // LEFT EDGE
            //
            {
                bool hovered = (edge == ResizeEdge.Left);
                bool active = IsResizing && hovered;
                uint color = active ? activeColor : (hovered ? hoverColor : normalColor);
                float thickness = active ? activeThickness : (hovered ? hoverThickness : normalThickness);

                Vector2 p1 = new Vector2(0, 0);
                Vector2 p2 = new Vector2(0, windowSize.y);
                dl.AddLine(p1, p2, color, thickness);
            }

            //
            // RIGHT EDGE
            //
            {
                bool hovered = (edge == ResizeEdge.Right);
                bool active = IsResizing && hovered;
                uint color = active ? activeColor : (hovered ? hoverColor : normalColor);
                float thickness = active ? activeThickness : (hovered ? hoverThickness : normalThickness);

                Vector2 p1 = new Vector2(windowSize.x, 0);
                Vector2 p2 = new Vector2(windowSize.x, windowSize.y);
                dl.AddLine(p1, p2, color, thickness);
            }

            //
            // BOTTOM EDGE
            //
            {
                bool hovered = (edge == ResizeEdge.Bottom);
                bool active = IsResizing && hovered;
                uint color = active ? activeColor : (hovered ? hoverColor : normalColor);
                float thickness = active ? activeThickness : (hovered ? hoverThickness : normalThickness);

                Vector2 p1 = new Vector2(0, windowSize.y);
                Vector2 p2 = new Vector2(windowSize.x, windowSize.y);
                dl.AddLine(p1, p2, color, thickness);
            }

            //
            // CORNER (bottom-right) — triangle
            //
            {
                bool hovered = (edge == ResizeEdge.BottomRight);
                bool active = IsResizing && hovered;
                uint color = active ? activeColor : (hovered ? hoverColor : normalColor);

                if (hovered || active)
                {
                    float s = 12f * Fugui.Scale;

                    Vector2 c = new Vector2(windowSize.x, windowSize.y);
                    Vector2 a = c + new Vector2(-s, 0);    // left
                    Vector2 b = c + new Vector2(0, -s);    // up

                    dl.AddTriangleFilled(a, b, c, color);
                }
            }

            //
            // CORNER (bottom-left) — triangle
            //
            {
                bool hovered = (edge == ResizeEdge.BottomLeft);
                bool active = IsResizing && hovered;
                uint color = active ? activeColor : (hovered ? hoverColor : normalColor);

                if (hovered || active)
                {
                    float s = 12f * Fugui.Scale;

                    Vector2 c = new Vector2(0, windowSize.y);
                    Vector2 a = c + new Vector2(s, 0);     // right
                    Vector2 b = c + new Vector2(0, -s);    // up

                    dl.AddTriangleFilled(a, b, c, color);
                }
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void glDeleteVertexArrays_t(int n, ref uint arrays);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void glDeleteBuffers_t(int n, ref uint buffers);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void glBindAttribLocation_t(uint program, uint index, string name);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void glDeleteProgram_t(uint program);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void glDeleteTextures_t(int n, ref uint textures);

        private static glDeleteVertexArrays_t _glDeleteVertexArrays;
        private static glDeleteBuffers_t _glDeleteBuffers;
        private static glBindAttribLocation_t _glBindAttribLocation;
        private static glDeleteProgram_t _glDeleteProgram;
        private static glDeleteTextures_t _glDeleteTextures;

        private static void EnsureExtraFns()
        {
            if (_glDeleteVertexArrays == null) _glDeleteVertexArrays = GLMini.Load<glDeleteVertexArrays_t>("glDeleteVertexArrays");
            if (_glDeleteBuffers == null) _glDeleteBuffers = GLMini.Load<glDeleteBuffers_t>("glDeleteBuffers");
            if (_glBindAttribLocation == null) _glBindAttribLocation = GLMini.Load<glBindAttribLocation_t>("glBindAttribLocation");
            if (_glDeleteProgram == null) _glDeleteProgram = GLMini.Load<glDeleteProgram_t>("glDeleteProgram");
            if (_glDeleteTextures == null) _glDeleteTextures = GLMini.Load<glDeleteTextures_t>("glDeleteTextures");
        }

        private static void glDeleteVertexArray(uint id) { EnsureExtraFns(); _glDeleteVertexArrays(1, ref id); }
        private static void glDeleteBuffer(uint id) { EnsureExtraFns(); _glDeleteBuffers(1, ref id); }
        private static void glBindAttribLocation(uint program, uint index, string name) { EnsureExtraFns(); _glBindAttribLocation(program, index, name); }
        private static void glDeleteProgram(uint program) { EnsureExtraFns(); _glDeleteProgram(program); }
        private static void glDeleteTexture(uint id) { EnsureExtraFns(); _glDeleteTextures(1, ref id); }

        /// <summary>
        /// Create GL pipeline for ImGui rendering
        /// </summary>
        /// <returns> True if successful </returns>
        private bool CreatePipeline()
        {
            // Create buffers
            GLMini.glGenVertexArrays(1, out _vao);
            GLMini.glBindVertexArray(_vao);

            GLMini.glGenBuffers(1, out _vbo);
            GLMini.glBindBuffer(GLMini.GL_ARRAY_BUFFER, _vbo);

            GLMini.glGenBuffers(1, out _ebo);
            GLMini.glBindBuffer(GLMini.GL_ELEMENT_ARRAY_BUFFER, _ebo);

            // Compile shader
            _shaderProgram = CreateImGuiShaderProgram(out _locProjMtx, out _locTexture);
            if (_shaderProgram == 0)
            {
                Debug.LogError("Failed to create ImGui shader program");
                return false;
            }

            // Vertex format = ImDrawVert { pos(2f), uv(2f), col(4ub) }
            // Attribute locations: 0=Position, 1=UV, 2=Color
            const int stride = 4 * 4 + 4; // 20 bytes -> but OpenGL needs aligned strides; we'll use tightly packed 20 bytes (valid)
            // However, many drivers prefer 4-byte alignment; 20 is fine.

            GLMini.glEnableVertexAttribArray(0); // Position
            GLMini.glEnableVertexAttribArray(1); // UV
            GLMini.glEnableVertexAttribArray(2); // Color (normalized)
            ConfigureVertexAttribPointers(0, stride);

            // Unbind
            GLMini.glBindVertexArray(0);
            return true;
        }

        private unsafe void ConfigureVertexAttribPointers(uint vertexOffset, int stride = 0)
        {
            if (stride == 0)
            {
                stride = sizeof(ImDrawVert);
            }

            long baseOffset = (long)vertexOffset * stride;
            GLMini.glVertexAttribPointer(0, 2, GLMini.GL_FLOAT, false, stride, (IntPtr)baseOffset);
            GLMini.glVertexAttribPointer(1, 2, GLMini.GL_FLOAT, false, stride, (IntPtr)(baseOffset + 4 * 2));
            GLMini.glVertexAttribPointer(2, 4, GLMini.GL_UNSIGNED_BYTE, true, stride, (IntPtr)(baseOffset + 4 * 4));
        }

        /// <summary>
        /// Create ImGui shader program
        /// </summary>
        /// <param name="locProjMtx"> the location of the projection matrix uniform </param>
        /// <param name="locTex"> the location of the texture uniform </param>
        /// <returns> the shader program ID </returns>
        private uint CreateImGuiShaderProgram(out int locProjMtx, out int locTex)
        {
            locProjMtx = -1; locTex = -1;

            string vs = @"
#version 130
                uniform mat4 ProjMtx;
                in vec2 Position;
                in vec2 UV;
                in vec4 Color;
                out vec2 Frag_UV;
                out vec4 Frag_Color;
                void main()
                {
                    Frag_UV = UV;
                    Frag_Color = Color;
                    gl_Position = ProjMtx * vec4(Position.xy, 0.0, 1.0);
                }";

            string fs = @"
#version 130
                uniform sampler2D Texture;
                in vec2 Frag_UV;
                in vec4 Frag_Color;
                out vec4 Out_Color;

                void main()
                {
                    vec2 uv = vec2(Frag_UV.x, 1.0 - Frag_UV.y);
                    vec4 tex = texture(Texture, uv);
                    Out_Color = tex * Frag_Color;
                }";

            uint vsId = GLMini.glCreateShader(GLMini.GL_VERTEX_SHADER);
            GLMini.ShaderSource(vsId, vs);
            GLMini.glCompileShader(vsId);

            int ok;
            GLMini.glGetShaderiv(vsId, GLMini.GL_COMPILE_STATUS, out ok);
            if (ok == 0)
            {
                Debug.LogError("VS error: " + ReadShaderLog(vsId));
                GLMini.glDeleteShader(vsId);
                return 0;
            }

            uint fsId = GLMini.glCreateShader(GLMini.GL_FRAGMENT_SHADER);
            GLMini.ShaderSource(fsId, fs);
            GLMini.glCompileShader(fsId);
            GLMini.glGetShaderiv(fsId, GLMini.GL_COMPILE_STATUS, out ok);
            if (ok == 0)
            {
                Debug.LogError("FS error: " + ReadShaderLog(fsId));
                GLMini.glDeleteShader(vsId);
                GLMini.glDeleteShader(fsId);
                return 0;
            }

            uint prog = GLMini.glCreateProgram();
            GLMini.glAttachShader(prog, vsId);
            GLMini.glAttachShader(prog, fsId);

            glBindAttribLocation(prog, 0, "Position");
            glBindAttribLocation(prog, 1, "UV");
            glBindAttribLocation(prog, 2, "Color");

            GLMini.glLinkProgram(prog);
            GLMini.glGetProgramiv(prog, GLMini.GL_LINK_STATUS, out ok);

            GLMini.glDeleteShader(vsId);
            GLMini.glDeleteShader(fsId);

            if (ok == 0)
            {
                Debug.LogError("Program link error: " + ReadProgramLog(prog));
                glDeleteProgram(prog);
                return 0;
            }

            locProjMtx = GLMini.glGetUniformLocation(prog, "ProjMtx");
            locTex = GLMini.glGetUniformLocation(prog, "Texture");
            return prog;
        }

        /// <summary>
        /// Read shader compilation log
        /// </summary>
        /// <param name="shader"> the shader ID </param>
        /// <returns> the log string </returns>
        private static string ReadShaderLog(uint shader)
        {
            // small util: ask 4096 bytes
            const int max = 4096;
            IntPtr buf = Marshal.AllocHGlobal(max);
            try
            {
                int len;
                GLMini.glGetShaderInfoLog(shader, max, out len, buf);
                if (len <= 0) return string.Empty;
                return Marshal.PtrToStringAnsi(buf, Math.Min(len, max)) ?? string.Empty;
            }
            finally { Marshal.FreeHGlobal(buf); }
        }

        /// <summary>
        /// Read program link log
        /// </summary>
        /// <param name="prog"> the program ID </param>
        /// <returns> the log string </returns>
        private static string ReadProgramLog(uint prog)
        {
            const int max = 4096;
            IntPtr buf = Marshal.AllocHGlobal(max);
            try
            {
                int len;
                GLMini.glGetProgramInfoLog(prog, max, out len, buf);
                if (len <= 0) return string.Empty;
                return Marshal.PtrToStringAnsi(buf, Math.Min(len, max)) ?? string.Empty;
            }
            finally { Marshal.FreeHGlobal(buf); }
        }

        /// <summary>
        /// Render a frame into the external window
        /// </summary>
        private unsafe void RenderFrame()
        {
            Window.ForceDraw(10);
            lock (Window.Container.Context.DrawData)
            {
                var drawData = Window.Container.Context.DrawData;
                if (drawData == null || drawData.CmdListsCount == 0)
                    return;

                // Clear screen
                GLMini.glViewport(0, 0, Width, Height);
                var bgColor = Fugui.Themes.GetColor(FuColors.WindowBg);
                GLMini.glClearColor(bgColor.x, bgColor.y, bgColor.z, bgColor.w);
                GLMini.glClear(GLMini.GL_COLOR_BUFFER_BIT);

                RenderImGuiDrawData(drawData);
            }
        }

        /// <summary>
        /// Render ImGui draw data into the current OpenGL context
        /// </summary>
        /// <param name="drawData"> ImGui draw data </param>
        private unsafe void RenderImGuiDrawData(DrawData drawData)
        {
            int fbWidth = (int)(drawData.DisplaySize.x * drawData.FramebufferScale.x);
            int fbHeight = (int)(drawData.DisplaySize.y * drawData.FramebufferScale.y);
            if (fbWidth <= 0 || fbHeight <= 0)
                return;

            // GL state
            GLMini.glEnable(GLMini.GL_BLEND);
            GLMini.glBlendEquation(GLMini.GL_FUNC_ADD);
            GLMini.glBlendFuncSeparate(GLMini.GL_SRC_ALPHA, GLMini.GL_ONE_MINUS_SRC_ALPHA,
                                       GLMini.GL_ONE, GLMini.GL_ONE_MINUS_SRC_ALPHA);

            GLMini.glDisable(GLMini.GL_CULL_FACE);
            GLMini.glDisable(GLMini.GL_DEPTH_TEST);
            GLMini.glEnable(GLMini.GL_SCISSOR_TEST);

            // Use program + VAO
            GLMini.glUseProgram(_shaderProgram);
            GLMini.glBindVertexArray(_vao);

            // Projection matrix
            float L = drawData.DisplayPos.x;
            float R = drawData.DisplayPos.x + drawData.DisplaySize.x;
            float T = drawData.DisplayPos.y;
            float B = drawData.DisplayPos.y + drawData.DisplaySize.y;

            float[] ortho =
            {
                2f/(R-L), 0,         0, 0,
                0,        2f/(T-B),  0, 0,
                0,        0,        -1, 0,
                (R+L)/(L-R), (T+B)/(B-T), 0, 1
            };

            fixed (float* p = ortho)
                GLMini.glUniformMatrix4fv(_locProjMtx, 1, false, (IntPtr)p);

            if (drawData.RenderItems != null && drawData.RenderItems.Count > 0)
            {
                for (int n = 0; n < drawData.RenderItems.Count; n++)
                {
                    DrawDataRenderItem item = drawData.RenderItems[n];
                    IReadOnlyList<DrawList> drawLists = item.DrawLists;
                    if (drawLists == null || drawLists.Count == 0)
                    {
                        continue;
                    }

                    Vector2 renderOffset = item.IsWindow ? item.Window.RenderMeshOffset : Vector2.zero;
                    RenderImGuiDrawLists(drawLists, drawData, fbWidth, fbHeight, renderOffset);
                }
            }
            else
            {
                RenderImGuiDrawLists(drawData.DrawLists, drawData, fbWidth, fbHeight, Vector2.zero);
            }

            // Restore state
            GLMini.glDisable(GLMini.GL_SCISSOR_TEST);
            GLMini.glBindTexture(GLMini.GL_TEXTURE_2D, 0);
            GLMini.glBindVertexArray(0);
            GLMini.glUseProgram(0);
        }

        private unsafe void RenderImGuiDrawLists(IReadOnlyList<DrawList> drawLists, DrawData drawData, int fbWidth, int fbHeight, Vector2 renderOffset)
        {
            bool hasRenderOffset = Mathf.Abs(renderOffset.x) > 0.001f || Mathf.Abs(renderOffset.y) > 0.001f;

            for (int n = 0; n < drawLists.Count; n++)
            {
                DrawList cmdList = drawLists[n];
                if (cmdList == null || cmdList.VtxBuffer.Length == 0 || cmdList.IdxBuffer.Length == 0)
                {
                    continue;
                }

                int vtxSize = cmdList.VtxBuffer.Length * sizeof(ImDrawVert);
                int idxSize = cmdList.IdxBuffer.Length * sizeof(ushort);

                // Resize buffers if needed
                GLMini.glBindBuffer(GLMini.GL_ARRAY_BUFFER, _vbo);
                if (vtxSize > _vbCapacity)
                {
                    _vbCapacity = NextPow2(vtxSize);
                    GLMini.glBufferData(GLMini.GL_ARRAY_BUFFER, (IntPtr)_vbCapacity, IntPtr.Zero, GLMini.GL_DYNAMIC_DRAW);
                }

                GLMini.glBindBuffer(GLMini.GL_ELEMENT_ARRAY_BUFFER, _ebo);
                if (idxSize > _ibCapacity)
                {
                    _ibCapacity = NextPow2(idxSize);
                    GLMini.glBufferData(GLMini.GL_ELEMENT_ARRAY_BUFFER, (IntPtr)_ibCapacity, IntPtr.Zero, GLMini.GL_DYNAMIC_DRAW);
                }

                if (hasRenderOffset)
                {
                    ImDrawVert[] vertices = GetOffsetVertexBuffer(cmdList.VtxBuffer, renderOffset);
                    fixed (ImDrawVert* v = vertices)
                        GLMini.glBufferSubData(GLMini.GL_ARRAY_BUFFER, IntPtr.Zero, (IntPtr)vtxSize, (IntPtr)v);
                }
                else
                {
                    fixed (ImDrawVert* v = cmdList.VtxBuffer)
                        GLMini.glBufferSubData(GLMini.GL_ARRAY_BUFFER, IntPtr.Zero, (IntPtr)vtxSize, (IntPtr)v);
                }

                fixed (ushort* i = cmdList.IdxBuffer)
                    GLMini.glBufferSubData(GLMini.GL_ELEMENT_ARRAY_BUFFER, IntPtr.Zero, (IntPtr)idxSize, (IntPtr)i);

                uint currentVertexOffset = uint.MaxValue;

                foreach (var cmd in cmdList.CmdBuffer)
                {
                    if (cmd.UserCallback != IntPtr.Zero)
                    {
                        Debug.Log("unhandled user callback");
                        continue;
                    }

                    Vector4 clipRect = cmd.ClipRect;
                    if (hasRenderOffset)
                    {
                        clipRect.x += renderOffset.x;
                        clipRect.y += renderOffset.y;
                        clipRect.z += renderOffset.x;
                        clipRect.w += renderOffset.y;
                    }

                    float clipMinX = (clipRect.x - drawData.DisplayPos.x) * drawData.FramebufferScale.x;
                    float clipMinY = (clipRect.y - drawData.DisplayPos.y) * drawData.FramebufferScale.y;
                    float clipMaxX = (clipRect.z - drawData.DisplayPos.x) * drawData.FramebufferScale.x;
                    float clipMaxY = (clipRect.w - drawData.DisplayPos.y) * drawData.FramebufferScale.y;

                    if (clipMinX >= fbWidth || clipMinY >= fbHeight || clipMaxX < 0f || clipMaxY < 0f)
                    {
                        continue;
                    }

                    clipMinX = Mathf.Max(clipMinX, 0f);
                    clipMinY = Mathf.Max(clipMinY, 0f);
                    clipMaxX = Mathf.Min(clipMaxX, fbWidth);
                    clipMaxY = Mathf.Min(clipMaxY, fbHeight);

                    float clipW = clipMaxX - clipMinX;
                    float clipH = clipMaxY - clipMinY;
                    if (clipW <= 0f || clipH <= 0f)
                    {
                        continue;
                    }

                    //Debug.Log($" DrawCmd: ElemCount={cmd.ElemCount}, ClipRect=({cmd.ClipRect.x},{cmd.ClipRect.y},{cmd.ClipRect.z},{cmd.ClipRect.w}), Scissor=({(int)clipX},{(int)(fbHeight - (int)(clipY + clipH))},{(int)clipW},{(int)clipH})");

                    GLMini.glScissor(
                        (int)clipMinX,
                        fbHeight - (int)clipMaxY,
                        (int)clipW,
                        (int)clipH
                    );

                    uint texId = GetRegisteredTexture(cmd.TextureId);

                    if (cmd.VtxOffset != currentVertexOffset)
                    {
                        ConfigureVertexAttribPointers(cmd.VtxOffset);
                        currentVertexOffset = cmd.VtxOffset;
                    }

                    GLMini.glBindTexture(GLMini.GL_TEXTURE_2D, texId);
                    GLMini.glDrawElements(GLMini.GL_TRIANGLES,
                                          (int)cmd.ElemCount,
                                          GLMini.GL_UNSIGNED_SHORT,
                                          (IntPtr)((long)cmd.IdxOffset * sizeof(ushort)));
                }
            }
        }

        private ImDrawVert[] GetOffsetVertexBuffer(ImDrawVert[] source, Vector2 renderOffset)
        {
            if (_offsetVertexBuffer == null || _offsetVertexBuffer.Length < source.Length)
            {
                _offsetVertexBuffer = new ImDrawVert[source.Length];
            }

            for (int i = 0; i < source.Length; i++)
            {
                ImDrawVert vertex = source[i];
                vertex.pos.x += renderOffset.x;
                vertex.pos.y += renderOffset.y;
                _offsetVertexBuffer[i] = vertex;
            }

            return _offsetVertexBuffer;
        }

        /// <summary>
        /// Compute the next power of two greater than or equal to v
        /// </summary>
        /// <param name="v"> input value </param>
        /// <returns> next power of two </returns>
        private static int NextPow2(int v)
        {
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++;
            return v < 256 ? 256 : v;
        }

        /// <summary>
        /// Minimize the external window
        /// </summary>
        public void Minimize()
        {
            if (_isRunning && !_shouldClose && !_isClosed)
            {
                SDL_MinimizeWindow(SdlWindow);
            }
        }

        public bool IsMaximized { get; private set; } = false;
        private SDL_Rect restoreRect;
        /// <summary>
        /// Toggle maximize / restore of the borderless window
        /// </summary>
        public void ToggleMaximize()
        {
            if (IsMaximized)
                RestoreBorderlessWindow();
            else
                MaximizeBorderlessWindow();
        }

        /// <summary>
        /// Maximize the borderless window
        /// </summary>
        private void MaximizeBorderlessWindow()
        {
            int x = 0, y = 0, w = 0, h = 0;
            SDL_GetWindowPosition(SdlWindow, ref x, ref y);
            SDL_GetWindowSize(SdlWindow, ref w, ref h);

            restoreRect = new SDL_Rect { x = x, y = y, w = w, h = h };

            int displayIndex = SDL_GetWindowDisplayIndex(SdlWindow);

            SDL_Rect usableBounds = default;
            SDL_GetDisplayUsableBounds(displayIndex, ref usableBounds);

            SDL_SetWindowPosition(SdlWindow, usableBounds.x, usableBounds.y);
            SDL_SetWindowSize(SdlWindow, usableBounds.w, usableBounds.h);
            RaiseWindow();

            IsMaximized = true;
        }

        /// <summary>
        /// Restore the borderless window
        /// </summary>
        private void RestoreBorderlessWindow()
        {
            SDL_SetWindowPosition(SdlWindow, restoreRect.x, restoreRect.y);
            SDL_SetWindowSize(SdlWindow, restoreRect.w, restoreRect.h);
            RaiseWindow();

            IsMaximized = false;
        }
    }

    public enum ResizeEdge
    {
        None,
        Left,
        Right,
        Bottom,
        BottomLeft,
        BottomRight
    }
}
#endif
