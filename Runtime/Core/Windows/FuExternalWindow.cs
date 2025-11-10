// Framework 4.7 compatible, IL2CPP-safe
// Requires: SDL2-CS (native SDL2 present), GLMini.cs (mini OpenGL loader)
// Renders Dear ImGui draw data into an external SDL2 OpenGL window (OpenGL 3.0 + GLSL 130)
using ImGuiNET;
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
        #region Variables
        public FuWindow Window;
        public IntPtr SdlWindow { get; private set; }
        public uint SdlWindowId { get; private set; }
        public string Title { get; private set; }
        public int Width => Window.Size.x;
        public int Height => Window.Size.y;
        private Vector2Int _containerPosition;
        public Vector2Int ContainerPosition
        {
            get => _containerPosition;
            set
            {
                _containerPosition = value;
                if (SdlWindow != IntPtr.Zero)
                    SDL_SetWindowPosition(SdlWindow, _containerPosition.x, _containerPosition.y);
            }
        }

        private IntPtr _glContext;

        /// <summary>True while the external window is alive and can render.</summary>
        private volatile bool _isRunning = false;

        /// <summary>Set by the event handler when the user clicks the window close button.</summary>
        private volatile bool _shouldClose = false;

        /// <summary>Ensure Close() is executed exactly once.</summary>
        private volatile bool _isClosed = false;

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
        #endregion

        public FuExternalWindow(FuWindow window)
        {
            Window = window;
            _containerPosition = window.WorldPosition;
            Title = Window.WindowName.Name;
        }

        #region Workflow
        /// <summary>
        /// Main run loop for the external window thread
        /// </summary>
        public void Start()
        {
            if (SDL_Init(SDL_INIT_VIDEO) < 0)
            {
                Debug.LogError("SDL init failed: " + SDL_GetError());
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

            SDL_WindowFlags flags = SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL_WindowFlags.SDL_WINDOW_RESIZABLE | SDL_WindowFlags.SDL_WINDOW_SHOWN;
            if(Window.NoTaskBarIcon)
                flags |= SDL_WindowFlags.SDL_WINDOW_SKIP_TASKBAR;
            if(!Window.UseNativeTitleBar)
                flags |= SDL_WindowFlags.SDL_WINDOW_BORDERLESS;
            if(Window.AlwaysOnTop)
                flags |= SDL_WindowFlags.SDL_WINDOW_ALWAYS_ON_TOP;
            SdlWindow = SDL_CreateWindow(
                Title,
                _containerPosition.x,
                _containerPosition.y,
                Window.Size.x,
                Window.Size.y,
                flags);

            if (SdlWindow == IntPtr.Zero)
            {
                Debug.LogError("SDL_CreateWindow failed: " + SDL_GetError());
                return;
            }
            SdlWindowId = SDL_GetWindowID(SdlWindow);

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

            // ensure local position is zero
            Window.LocalPosition = Vector2Int.zero;
        }

        /// <summary>
        /// Update SDL events for this window
        /// </summary>
        public void UpdateEvents()
        {
            SDL_GL_MakeCurrent(SdlWindow, _glContext);
            Fugui.SDLEventRooter.Update();
        }

        /// <summary>
        /// Signal the window to close
        /// </summary>
        public void Close()
        {
            if (_isClosed) return; // already closed
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

                // Also delete fallback texture
                if (_fallbackWhiteTex != 0) { glDeleteTexture(_fallbackWhiteTex); _fallbackWhiteTex = 0; }

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

                Fugui.RemoveExternalWindow(Window);

                // unregister from SDL event rooter
                Fugui.SDLEventRooter.UnregisterWindow(SdlWindowId);
            }
            catch (Exception e)
            {
                Debug.LogError("Error during external window cleanup: " + e);
            }
        }
        #endregion

        #region Textures Management
        // Fallback white texture (1x1) used when a texture is missing
        private uint _fallbackWhiteTex = 0;
        // Keep for compatibility with rest of the pipeline
        private readonly Dictionary<IntPtr, uint> _registeredTextures = new Dictionary<IntPtr, uint>();
        // Per-unityId state
        private readonly Dictionary<IntPtr, PBOPair> _gpu = new Dictionary<IntPtr, PBOPair>();
        private readonly Dictionary<IntPtr, ReadbackState> _rb = new Dictionary<IntPtr, ReadbackState>();

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
                byte white = 128;
                GLMini.glTexImage2D(GLMini.GL_TEXTURE_2D, 0, (int)GLMini.GL_RGBA,
                    1, 1, 0, GLMini.GL_RGBA, GLMini.GL_UNSIGNED_BYTE, (IntPtr)(&white));
            }

            GLMini.glBindTexture(GLMini.GL_TEXTURE_2D, 0);
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
            var raw = t2d.GetRawTextureData<byte>(); // NativeArray<byte>
            if (!raw.IsCreated || raw.Length == 0) return;

            PBOPair gpu = EnsurePBOPair(unityId, t2d.width, t2d.height);
            void* src = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(raw);
            UploadWithPBO(gpu, (IntPtr)src, raw.Length);
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

                    var raw = t2d.GetRawTextureData<byte>();
                    unsafe
                    {
                        void* ptr = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(raw);
                        GLMini.glTexImage2D(GLMini.GL_TEXTURE_2D, 0, (int)GLMini.GL_RGBA,
                            t2d.width, t2d.height, 0, GLMini.GL_RGBA, GLMini.GL_UNSIGNED_BYTE,
                            (IntPtr)ptr);
                    }

                    GLMini.glBindTexture(GLMini.GL_TEXTURE_2D, 0);

                    // Store it as if it were a PBOPair, but minimal
                    _gpu[unityId] = new PBOPair { glTex = texId, w = t2d.width, h = t2d.height };
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
        #endregion

        #region SDL events
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
                Close();
            }

            // Window resize
            else if (e.type == SDL_EventType.SDL_WINDOWEVENT && (e.window.windowEvent == SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED ||
                e.window.windowEvent == SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED))
            {
                Window.Size = new Vector2Int(e.window.data1, e.window.data2);
            }

            // Window move
            else if (e.type == SDL_EventType.SDL_WINDOWEVENT && e.window.windowEvent == SDL_WindowEventID.SDL_WINDOWEVENT_MOVED)
            {
                ContainerPosition = new Vector2Int(e.window.data1, e.window.data2);
            }
        }
        #endregion

        #region GL helpers (minimal P/Invoke for deletes / attrib binding not in GLMini)
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
        #endregion

        #region Pipeline & Shader
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
            GLMini.glVertexAttribPointer(0, 2, GLMini.GL_FLOAT, false, stride, (IntPtr)0);

            GLMini.glEnableVertexAttribArray(1); // UV
            GLMini.glVertexAttribPointer(1, 2, GLMini.GL_FLOAT, false, stride, (IntPtr)(4 * 2));

            GLMini.glEnableVertexAttribArray(2); // Color (normalized)
            GLMini.glVertexAttribPointer(2, 4, GLMini.GL_UNSIGNED_BYTE, true, stride, (IntPtr)(4 * 4));

            // Unbind
            GLMini.glBindVertexArray(0);
            return true;
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
        #endregion

        #region Frame rendering (OpenGL)
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
                GLMini.glViewport(0, 0, Window.Size.x, Window.Size.y);
                GLMini.glClearColor(0, 0, 0, 1f);
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

            // Upload + draw each command list
            for (int n = 0; n < drawData.CmdListsCount; n++)
            {
                var cmdList = drawData.DrawLists[n];

                int vtxSize = cmdList.VtxBuffer.Length * sizeof(ImDrawVert);
                int idxSize = cmdList.IdxBuffer.Length * sizeof(ushort);

                // Resize buffers if needed
                if (vtxSize > _vbCapacity)
                {
                    _vbCapacity = NextPow2(vtxSize);
                    GLMini.glBindBuffer(GLMini.GL_ARRAY_BUFFER, _vbo);
                    GLMini.glBufferData(GLMini.GL_ARRAY_BUFFER, (IntPtr)_vbCapacity, IntPtr.Zero, GLMini.GL_DYNAMIC_DRAW);
                }

                if (idxSize > _ibCapacity)
                {
                    _ibCapacity = NextPow2(idxSize);
                    GLMini.glBindBuffer(GLMini.GL_ELEMENT_ARRAY_BUFFER, _ebo);
                    GLMini.glBufferData(GLMini.GL_ELEMENT_ARRAY_BUFFER, (IntPtr)_ibCapacity, IntPtr.Zero, GLMini.GL_DYNAMIC_DRAW);
                }

                // Upload vertex/index buffers directly
                fixed (ImDrawVert* v = cmdList.VtxBuffer)
                    GLMini.glBufferSubData(GLMini.GL_ARRAY_BUFFER, IntPtr.Zero, (IntPtr)vtxSize, (IntPtr)v);

                fixed (ushort* i = cmdList.IdxBuffer)
                    GLMini.glBufferSubData(GLMini.GL_ELEMENT_ARRAY_BUFFER, IntPtr.Zero, (IntPtr)idxSize, (IntPtr)i);

                int idxOffset = 0;

                foreach (var cmd in cmdList.CmdBuffer)
                {
                    float clipX = (cmd.ClipRect.x - drawData.DisplayPos.x) * drawData.FramebufferScale.x;
                    float clipY = (cmd.ClipRect.y - drawData.DisplayPos.y) * drawData.FramebufferScale.y;
                    float clipW = (cmd.ClipRect.z - cmd.ClipRect.x) * drawData.FramebufferScale.x;
                    float clipH = (cmd.ClipRect.w - cmd.ClipRect.y) * drawData.FramebufferScale.y;

                    //Debug.Log($" DrawCmd: ElemCount={cmd.ElemCount}, ClipRect=({cmd.ClipRect.x},{cmd.ClipRect.y},{cmd.ClipRect.z},{cmd.ClipRect.w}), Scissor=({(int)clipX},{(int)(fbHeight - (int)(clipY + clipH))},{(int)clipW},{(int)clipH})");

                    GLMini.glScissor(
                        (int)clipX,
                        fbHeight - (int)(clipY + clipH),
                        (int)clipW,
                        (int)clipH
                    );

                    uint texId = GetRegisteredTexture(cmd.TextureId);

                    GLMini.glBindTexture(GLMini.GL_TEXTURE_2D, texId);
                    GLMini.glDrawElements(GLMini.GL_TRIANGLES,
                                          (int)cmd.ElemCount,
                                          GLMini.GL_UNSIGNED_SHORT,
                                          (IntPtr)(idxOffset * 2));

                    idxOffset += (int)cmd.ElemCount;
                }
            }

            // Restore state
            GLMini.glDisable(GLMini.GL_SCISSOR_TEST);
            GLMini.glBindTexture(GLMini.GL_TEXTURE_2D, 0);
            GLMini.glBindVertexArray(0);
            GLMini.glUseProgram(0);
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
        #endregion
    }
}