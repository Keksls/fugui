using System;
using System.Runtime.InteropServices;

namespace Fu
{
    /// <summary>
    /// Minimal, Unity-safe OpenGL loader using function pointers resolved via SDL_GL_GetProcAddress.
    /// You must assign GLMini.GetProc before calling any Load* method:
    /// GLMini.GetProc = SDL.SDL_GL_GetProcAddress;
    /// </summary>
    public static class GLMini
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate uint glGetError_t();
        private static glGetError_t _glGetError;

        public static uint glGetError()
        {
            return _glGetError();
        }

        public static void Load(Func<string, IntPtr> loader)
        {
            _glGetError = LoadFunction<glGetError_t>(loader, "glGetError");
        }

        private static T LoadFunction<T>(Func<string, IntPtr> loader, string name)
        {
            IntPtr ptr = loader(name);
            if (ptr == IntPtr.Zero)
                throw new Exception($"OpenGL function {name} not found!");
            return Marshal.GetDelegateForFunctionPointer<T>(ptr);
        }

        #region Delegates loader
        /// <summary>
        /// Function used to resolve GL procedure addresses. Must be assigned to SDL.SDL_GL_GetProcAddress.
        /// </summary>
        public static Func<string, IntPtr> GetProc;

        /// <summary>
        /// Load an OpenGL function pointer into a C# delegate.
        /// Throws if missing.
        /// </summary>
        public static T Load<T>(string name) where T : class
        {
            if (GetProc == null) throw new InvalidOperationException("GLMini.GetProc is null. Assign SDL_GL_GetProcAddress first.");
            IntPtr p = GetProc(name);
            if (p == IntPtr.Zero) throw new MissingMethodException("OpenGL function not found: " + name);
            return Marshal.GetDelegateForFunctionPointer(p, typeof(T)) as T;
        }
        #endregion

        #region Constants (subset for ImGui-style pipeline)
        public const uint GL_COLOR_BUFFER_BIT = 0x00004000;
        public const uint GL_TRIANGLES = 0x0004;
        public const uint GL_BLEND = 0x0BE2;
        public const uint GL_SCISSOR_TEST = 0x0C11;
        public const uint GL_SRC_ALPHA = 0x0302;
        public const uint GL_ONE_MINUS_SRC_ALPHA = 0x0303;
        public const uint GL_ARRAY_BUFFER = 0x8892;
        public const uint GL_ELEMENT_ARRAY_BUFFER = 0x8893;
        public const uint GL_STATIC_DRAW = 0x88E4;
        public const uint GL_DYNAMIC_DRAW = 0x88E8;
        public const uint GL_TEXTURE_2D = 0x0DE1;
        public const uint GL_TEXTURE0 = 0x84C0;
        public const uint GL_UNSIGNED_BYTE = 0x1401;
        public const uint GL_UNSIGNED_SHORT = 0x1403;
        public const uint GL_FLOAT = 0x1406;
        public const uint GL_FALSE = 0;
        public const uint GL_TRUE = 1;
        public const uint GL_VERTEX_SHADER = 0x8B31;
        public const uint GL_FRAGMENT_SHADER = 0x8B30;
        public const uint GL_COMPILE_STATUS = 0x8B81;
        public const uint GL_LINK_STATUS = 0x8B82;
        public const uint GL_TEXTURE_MIN_FILTER = 0x2801;
        public const uint GL_TEXTURE_MAG_FILTER = 0x2800;
        public const uint GL_TEXTURE_WRAP_S = 0x2802;
        public const uint GL_TEXTURE_WRAP_T = 0x2803;
        public const uint GL_NEAREST = 0x2600;
        public const uint GL_LINEAR = 0x2601;
        public const uint GL_CLAMP_TO_EDGE = 0x812F;
        public const uint GL_RGB = 0x1907;
        public const uint GL_RGBA = 0x1908;
        public const uint GL_BGRA = 0x80E1;
        public const uint GL_UNPACK_ALIGNMENT = 0x0CF5;
        public const uint GL_FUNC_ADD = 0x8006;
        public const uint GL_BLEND_EQUATION = 0x8009;
        public const uint GL_BLEND_SRC_RGB = 0x80C9;
        public const uint GL_BLEND_DST_RGB = 0x80C8;
        public const uint GL_BLEND_SRC_ALPHA = 0x80CB;
        public const uint GL_BLEND_DST_ALPHA = 0x80CA;
        public const uint GL_CULL_FACE = 0x0B44;
        public const uint GL_DEPTH_TEST = 0x0B71;
        public const uint GL_ONE = 1;
        public const uint GL_ZERO = 0;
        public const uint GL_SRC_COLOR = 0x0300;
        public const uint GL_ONE_MINUS_SRC_COLOR = 0x0301;
        public const uint GL_DST_ALPHA = 0x0304;
        public const uint GL_ONE_MINUS_DST_ALPHA = 0x0305;
        public const uint GL_DST_COLOR = 0x0306;
        public const uint GL_ONE_MINUS_DST_COLOR = 0x0307;
        public const uint GL_SRC_ALPHA_SATURATE = 0x0308;
        public const uint GL_TRIANGLE_STRIP = 0x0005;
        public const uint GL_VERTEX_ARRAY_BINDING = 0x85B5;
        // --- PBO & map access ---
        public const uint GL_PIXEL_UNPACK_BUFFER = 0x88EC;
        public const uint GL_PIXEL_PACK_BUFFER = 0x88EB; // (optionnel, utile si tu veux lire depuis GL)
        public const uint GL_STREAM_DRAW = 0x88E0;
        public const uint GL_READ_ONLY = 0x88B8; // (optionnel)
        public const uint GL_WRITE_ONLY = 0x88B9; // utilisé par glMapBuffer
        public const uint GL_READ_WRITE = 0x88BA; // (optionnel)
        public const uint GL_MAP_WRITE_BIT = 0x0002;
        public const uint GL_MAP_INVALIDATE_BUFFER_BIT = 0x0008;

        #endregion

        #region Basic function delegates (increment as needed)
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate IntPtr glGetString_t(uint name);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glViewport_t(int x, int y, int w, int h);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glClearColor_t(float r, float g, float b, float a);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glClear_t(uint mask);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glEnable_t(uint cap);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glDisable_t(uint cap);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glBlendFunc_t(uint sfactor, uint dfactor);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glScissor_t(int x, int y, int w, int h);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glActiveTexture_t(uint tex);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glGenVertexArrays_t(int n, out uint id);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glBindVertexArray_t(uint id);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glGenBuffers_t(int n, out uint id);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glBindBuffer_t(uint target, uint buffer);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glBufferData_t(uint target, IntPtr size, IntPtr data, uint usage);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glBufferSubData_t(uint target, IntPtr offset, IntPtr size, IntPtr data);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glEnableVertexAttribArray_t(uint index);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glVertexAttribPointer_t(uint index, int size, uint type, bool normalized, int stride, IntPtr pointer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate uint glCreateShader_t(uint type);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glShaderSource_t(uint shader, int count, IntPtr strings, IntPtr lengths);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glCompileShader_t(uint shader);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glGetShaderiv_t(uint shader, uint pname, out int param);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glGetShaderInfoLog_t(uint shader, int maxLen, out int len, IntPtr infoLog);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glDeleteShader_t(uint shader);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate uint glCreateProgram_t();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glAttachShader_t(uint program, uint shader);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glLinkProgram_t(uint program);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glGetProgramiv_t(uint program, uint pname, out int param);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glGetProgramInfoLog_t(uint program, int maxLen, out int len, IntPtr infoLog);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glUseProgram_t(uint program);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate int glGetUniformLocation_t(uint program, string name);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glUniform1i_t(int loc, int v);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glUniformMatrix4fv_t(int loc, int count, bool transpose, IntPtr value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glGenTextures_t(int n, out uint id);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glBindTexture_t(uint target, uint tex);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glTexParameteri_t(uint target, uint pname, int param);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glTexImage2D_t(uint target, int level, int internalFormat, int width, int height, int border, uint format, uint type, IntPtr data);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glTexSubImage2D_t(uint target, int level, int x, int y, int width, int height, uint format, uint type, IntPtr data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glDrawElements_t(uint mode, int count, uint type, IntPtr indices);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glPixelStorei_t(uint pname, int param);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glBlendEquation_t(uint mode);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glBlendFuncSeparate_t(uint srcRGB, uint dstRGB, uint srcAlpha, uint dstAlpha);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void glDrawArrays_t(uint mode, int first, int count);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate IntPtr glMapBuffer_t(uint target, uint access);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate bool glUnmapBuffer_t(uint target);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate IntPtr glMapBufferRange_t(uint target, IntPtr offset, IntPtr length, uint access);
        #endregion

        #region Fields (loaded function pointers)
        public static glGetString_t glGetString;
        public static glViewport_t glViewport;
        public static glClearColor_t glClearColor;
        public static glClear_t glClear;
        public static glEnable_t glEnable;
        public static glDisable_t glDisable;
        public static glBlendFunc_t glBlendFunc;
        public static glScissor_t glScissor;
        public static glActiveTexture_t glActiveTexture;

        public static glGenVertexArrays_t glGenVertexArrays;
        public static glBindVertexArray_t glBindVertexArray;
        public static glGenBuffers_t glGenBuffers;
        public static glBindBuffer_t glBindBuffer;
        public static glBufferData_t glBufferData;
        public static glBufferSubData_t glBufferSubData;
        public static glEnableVertexAttribArray_t glEnableVertexAttribArray;
        public static glVertexAttribPointer_t glVertexAttribPointer;

        public static glCreateShader_t glCreateShader;
        public static glShaderSource_t glShaderSource;
        public static glCompileShader_t glCompileShader;
        public static glGetShaderiv_t glGetShaderiv;
        public static glGetShaderInfoLog_t glGetShaderInfoLog;
        public static glDeleteShader_t glDeleteShader;

        public static glCreateProgram_t glCreateProgram;
        public static glAttachShader_t glAttachShader;
        public static glLinkProgram_t glLinkProgram;
        public static glGetProgramiv_t glGetProgramiv;
        public static glGetProgramInfoLog_t glGetProgramInfoLog;
        public static glUseProgram_t glUseProgram;
        public static glGetUniformLocation_t glGetUniformLocation;
        public static glUniform1i_t glUniform1i;
        public static glUniformMatrix4fv_t glUniformMatrix4fv;

        public static glGenTextures_t glGenTextures;
        public static glBindTexture_t glBindTexture;
        public static glTexParameteri_t glTexParameteri;
        public static glTexImage2D_t glTexImage2D;
        public static glTexSubImage2D_t glTexSubImage2D;

        public static glDrawElements_t glDrawElements;
        public static glPixelStorei_t glPixelStorei;
        public static glBlendEquation_t glBlendEquation;
        public static glBlendFuncSeparate_t glBlendFuncSeparate;
        public static glDrawArrays_t glDrawArrays;

        public static glMapBuffer_t glMapBuffer;
        public static glUnmapBuffer_t glUnmapBuffer;
        public static glMapBufferRange_t glMapBufferRange;
        #endregion

        #region Public loaders
        /// <summary>
        /// Load a minimal set (clear/viewport) to smoke-test context.
        /// </summary>
        public static void LoadAll()
        {
            glGetString = Load<glGetString_t>("glGetString");
            glViewport = Load<glViewport_t>("glViewport");
            glClearColor = Load<glClearColor_t>("glClearColor");
            glClear = Load<glClear_t>("glClear");
            glEnable = Load<glEnable_t>("glEnable");
            glDisable = Load<glDisable_t>("glDisable");
            glBlendFunc = Load<glBlendFunc_t>("glBlendFunc");
            glPixelStorei = Load<glPixelStorei_t>("glPixelStorei");
            glBlendEquation = Load<glBlendEquation_t>("glBlendEquation");
            glBlendFuncSeparate = Load<glBlendFuncSeparate_t>("glBlendFuncSeparate");
            glScissor = Load<glScissor_t>("glScissor");
            glDrawArrays = Load<glDrawArrays_t>("glDrawArrays");

            glActiveTexture = Load<glActiveTexture_t>("glActiveTexture");

            glGenVertexArrays = Load<glGenVertexArrays_t>("glGenVertexArrays");
            glBindVertexArray = Load<glBindVertexArray_t>("glBindVertexArray");
            glGenBuffers = Load<glGenBuffers_t>("glGenBuffers");
            glBindBuffer = Load<glBindBuffer_t>("glBindBuffer");
            glBufferData = Load<glBufferData_t>("glBufferData");
            glBufferSubData = Load<glBufferSubData_t>("glBufferSubData");
            glEnableVertexAttribArray = Load<glEnableVertexAttribArray_t>("glEnableVertexAttribArray");
            glVertexAttribPointer = Load<glVertexAttribPointer_t>("glVertexAttribPointer");

            glCreateShader = Load<glCreateShader_t>("glCreateShader");
            glShaderSource = Load<glShaderSource_t>("glShaderSource");
            glCompileShader = Load<glCompileShader_t>("glCompileShader");
            glGetShaderiv = Load<glGetShaderiv_t>("glGetShaderiv");
            glGetShaderInfoLog = Load<glGetShaderInfoLog_t>("glGetShaderInfoLog");
            glDeleteShader = Load<glDeleteShader_t>("glDeleteShader");

            glCreateProgram = Load<glCreateProgram_t>("glCreateProgram");
            glAttachShader = Load<glAttachShader_t>("glAttachShader");
            glLinkProgram = Load<glLinkProgram_t>("glLinkProgram");
            glGetProgramiv = Load<glGetProgramiv_t>("glGetProgramiv");
            glGetProgramInfoLog = Load<glGetProgramInfoLog_t>("glGetProgramInfoLog");
            glUseProgram = Load<glUseProgram_t>("glUseProgram");
            glGetUniformLocation = Load<glGetUniformLocation_t>("glGetUniformLocation");
            glUniform1i = Load<glUniform1i_t>("glUniform1i");
            glUniformMatrix4fv = Load<glUniformMatrix4fv_t>("glUniformMatrix4fv");

            glGenTextures = Load<glGenTextures_t>("glGenTextures");
            glBindTexture = Load<glBindTexture_t>("glBindTexture");
            glTexParameteri = Load<glTexParameteri_t>("glTexParameteri");
            glTexImage2D = Load<glTexImage2D_t>("glTexImage2D");
            glTexSubImage2D = Load<glTexSubImage2D_t>("glTexSubImage2D");

            glDrawElements = Load<glDrawElements_t>("glDrawElements");

            glMapBuffer = Load<glMapBuffer_t>("glMapBuffer");
            glUnmapBuffer = Load<glUnmapBuffer_t>("glUnmapBuffer");
            glMapBufferRange = Load<glMapBufferRange_t>("glMapBufferRange");
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Marshal a string[] to the (char**) expected by glShaderSource.
        /// </summary>
        public static unsafe void ShaderSource(uint shader, string src)
        {
            var strPtr = Marshal.StringToHGlobalAnsi(src);
            try
            {
                IntPtr* arr = stackalloc IntPtr[1];
                arr[0] = strPtr;
                glShaderSource(shader, 1, (IntPtr)arr, IntPtr.Zero);
            }
            finally
            {
                Marshal.FreeHGlobal(strPtr);
            }
        }

        public delegate void GetInfoLogFn(uint handle, int maxLength, out int length, IntPtr infoLog);
        /// <summary>
        /// Read a GL info log (shader/program).
        /// </summary>
        public static string ReadInfoLog(uint handle, GetInfoLogFn getLog)
        {
            const int max = 4096;
            IntPtr buf = Marshal.AllocHGlobal(max);

            try
            {
                int len;
                getLog(handle, max, out len, buf);
                if (len <= 0)
                    return string.Empty;

                return Marshal.PtrToStringAnsi(buf, Math.Min(len, max)) ?? string.Empty;
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }
        #endregion
    }
}