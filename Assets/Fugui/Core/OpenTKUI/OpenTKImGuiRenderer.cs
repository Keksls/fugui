using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Runtime.CompilerServices;
using ImGuiNET;
using System.Collections.Generic;

namespace Fugui.Core
{
    public class OpenTKImGuiRenderer : IDisposable
    {
        #region Variables
        // ID of the font texture used by ImGui
        public IntPtr FontTextureID;
        // Vertex array object for rendering ImGui graphics
        private int _vertexArray;
        // Vertex buffer object for rendering ImGui graphics
        private int _vertexBuffer;
        // Size of the vertex buffer object
        private int _vertexBufferSize;
        // Index buffer object for rendering ImGui graphics
        private int _indexBuffer;
        // Size of the index buffer object
        private int _indexBufferSize;
        // Shader program used for rendering ImGui graphics
        private int _shader;
        // Location of the "font texture" uniform in the shader program
        private int _shaderFontTextureLocation;
        // Location of the "projection matrix" uniform in the shader program
        private int _shaderProjectionMatrixLocation;
        // Flag indicating whether the KHR_debug extension is available
        private static bool KHRDebugAvailable = false;
        // Dictionary of textures used by ImGui, mapping Unity textures to OpenGL textures
        private Dictionary<UnityEngine.Texture, OpenGLTexture> _textures = new Dictionary<UnityEngine.Texture, OpenGLTexture>();
        #endregion 

        #region Initialization
        /// <summary>
        /// Constructs a new ImGuiController.
        /// </summary>
        public OpenTKImGuiRenderer(IntPtr pixels, int width, int height)
        {
            int major = GL.GetInteger(GetPName.MajorVersion);
            int minor = GL.GetInteger(GetPName.MinorVersion);

            KHRDebugAvailable = (major == 4 && minor >= 3) || IsExtensionSupported("KHR_debug");

            CreateDeviceResources(pixels, width, height);
        }

        /// <summary>
        /// prepare GL context and create font atlas GL texture
        /// </summary>
        /// <param name="pixels">Font Altral pixels array ptr</param>
        /// <param name="width">Font Atlas width</param>
        /// <param name="height">Font Atlas height</param>
        public void CreateDeviceResources(IntPtr pixels, int width, int height)
        {
            _vertexBufferSize = 10000;
            _indexBufferSize = 2000;

            int prevVAO = GL.GetInteger(GetPName.VertexArrayBinding);
            int prevArrayBuffer = GL.GetInteger(GetPName.ArrayBufferBinding);

            _vertexArray = GL.GenVertexArray();
            GL.BindVertexArray(_vertexArray);
            LabelObject(ObjectLabelIdentifier.VertexArray, _vertexArray, "ImGui");

            _vertexBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
            LabelObject(ObjectLabelIdentifier.Buffer, _vertexBuffer, "VBO: ImGui");
            GL.BufferData(BufferTarget.ArrayBuffer, _vertexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

            _indexBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
            LabelObject(ObjectLabelIdentifier.Buffer, _indexBuffer, "EBO: ImGui");
            GL.BufferData(BufferTarget.ElementArrayBuffer, _indexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

            RecreateFontDeviceTexture(pixels, width, height);

            string VertexSource = @"#version 330 core
uniform mat4 projection_matrix;
layout(location = 0) in vec2 in_position;
layout(location = 1) in vec2 in_texCoord;
layout(location = 2) in vec4 in_color;
out vec4 color;
out vec2 texCoord;
void main()
{
    gl_Position = projection_matrix * vec4(in_position, 0, 1);
    color = in_color;
    texCoord = in_texCoord;
}";
            string FragmentSource = @"#version 330 core
uniform sampler2D in_fontTexture;
in vec4 color;
in vec2 texCoord;
out vec4 outputColor;
void main()
{
    outputColor = color * texture(in_fontTexture, texCoord);
}";

            _shader = CreateProgram("ImGui", VertexSource, FragmentSource);
            _shaderProjectionMatrixLocation = GL.GetUniformLocation(_shader, "projection_matrix");
            _shaderFontTextureLocation = GL.GetUniformLocation(_shader, "in_fontTexture");

            int stride = Unsafe.SizeOf<ImDrawVert>();
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 8);
            GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, stride, 16);

            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);

            GL.BindVertexArray(prevVAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, prevArrayBuffer);

            CheckGLError("End of ImGui setup");
        }

        /// <summary>
        /// Recreates the device texture used to render text.
        /// </summary>
        public void RecreateFontDeviceTexture(IntPtr pixels, int width, int height)
        {
            int mips = (int)Math.Floor(Math.Log(Math.Max(width, height), 2));

            int prevActiveTexture = GL.GetInteger(GetPName.ActiveTexture);
            GL.ActiveTexture(TextureUnit.Texture0);
            int prevTexture2D = GL.GetInteger(GetPName.TextureBinding2D);

            FontTextureID = (IntPtr)GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, (int)FontTextureID);
            GL.TexStorage2D(TextureTarget2d.Texture2D, mips, SizedInternalFormat.Rgba8, width, height);
            LabelObject(ObjectLabelIdentifier.Texture, (int)FontTextureID, "ImGui Text Atlas");

            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, width, height, PixelFormat.Bgra, PixelType.UnsignedByte, pixels);

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, mips - 1);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);

            // Restore state
            GL.BindTexture(TextureTarget.Texture2D, prevTexture2D);
            GL.ActiveTexture((TextureUnit)prevActiveTexture);
        }

        /// <summary>
        /// Frees all graphics resources used by the renderer.
        /// </summary>
        public void Dispose()
        {
            GL.DeleteVertexArray(_vertexArray);
            GL.DeleteBuffer(_vertexBuffer);
            GL.DeleteBuffer(_indexBuffer);

            GL.DeleteTexture((int)FontTextureID);

            foreach (var texture in _textures)
            {
                texture.Value.Dispose();
            }

            GL.DeleteProgram(_shader);
        }

        /// <summary>
        /// 'tag' a GL object to get error on it if needed
        /// </summary>
        /// <param name="objLabelIdent">'tag' type</param>
        /// <param name="glObject">id of object to 'tag'</param>
        /// <param name="name">'tag' to give to object</param>
        public static void LabelObject(ObjectLabelIdentifier objLabelIdent, int glObject, string name)
        {
            if (KHRDebugAvailable)
                GL.ObjectLabel(objLabelIdent, glObject, name.Length, name);
        }

        /// <summary>
        /// check whatever an externtion is supported into current GL version
        /// </summary>
        /// <param name="name">extention to check</param>
        /// <returns>true if supported</returns>
        static bool IsExtensionSupported(string name)
        {
            int n = GL.GetInteger(GetPName.NumExtensions);
            for (int i = 0; i < n; i++)
            {
                string extension = GL.GetString(StringNameIndexed.Extensions, i);
                if (extension == name) return true;
            }

            return false;
        }

        /// <summary>
        /// create a GL program (shader) and return it's GL related ID
        /// </summary>
        /// <param name="name">name of the shader</param>
        /// <param name="vertexSource">vert shader part source</param>
        /// <param name="fragmentSoruce">frag shader part source</param>
        /// <returns>GL related ID of the shader</returns>
        public static int CreateProgram(string name, string vertexSource, string fragmentSoruce)
        {
            int program = GL.CreateProgram();
            LabelObject(ObjectLabelIdentifier.Program, program, $"Program: {name}");

            int vertex = CompileShader(name, ShaderType.VertexShader, vertexSource);
            int fragment = CompileShader(name, ShaderType.FragmentShader, fragmentSoruce);

            GL.AttachShader(program, vertex);
            GL.AttachShader(program, fragment);

            GL.LinkProgram(program);

            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
            if (success == 0)
            {
                string info = GL.GetProgramInfoLog(program);
                UnityEngine.Debug.Log($"GL.LinkProgram had info log [{name}]:\n{info}");
            }

            GL.DetachShader(program, vertex);
            GL.DetachShader(program, fragment);

            GL.DeleteShader(vertex);
            GL.DeleteShader(fragment);

            return program;
        }

        /// <summary>
        /// Compile a shader into current GL context
        /// </summary>
        /// <param name="name">name of the sahder</param>
        /// <param name="type">type of the shader</param>
        /// <param name="source">full shader source</param>
        /// <returns>GL related ID of the shader</returns>
        private static int CompileShader(string name, ShaderType type, string source)
        {
            int shader = GL.CreateShader(type);
            LabelObject(ObjectLabelIdentifier.Shader, shader, $"Shader: {name}");

            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);

            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string info = GL.GetShaderInfoLog(shader);
                UnityEngine.Debug.Log($"GL.CompileShader for shader '{name}' [{type}] had info log:\n{info}");
            }

            return shader;
        }

        /// <summary>
        /// check for any GL error on 'tagged' GL objects
        /// </summary>
        /// <param name="title">debug text</param>
        public static void CheckGLError(string title)
        {
            ErrorCode error;
            int i = 1;
            while ((error = GL.GetError()) != ErrorCode.NoError)
            {
                UnityEngine.Debug.Log($"{title} ({i++}): {error}");
            }
        }
        #endregion

        #region Render
        /// <summary>
        /// Render ImGui DrawData
        /// </summary>
        /// <param name="draw_data">drawData to draw</param>
        /// <param name="renderSize">size of the render</param>
        /// <param name="backendFlags">backend flag (used to check if vtx can beresized)</param>
        public unsafe void RenderImDrawData(DrawData draw_data, UnityEngine.Vector2 renderSize, ImGuiBackendFlags backendFlags)
        {
            // don't do anything if there is nothing to draw
            if (draw_data.CmdListsCount == 0)
            {
                return;
            }

            // prepare textures
            foreach (OpenGLTexture ti in _textures.Values)
            {
                // if this texture need to be created, let's create it
                ti.CreateTexture();
                // if it's a render texture, let's update it
                ti.UpdateTexture();
            }

            // Get intial state.
            int prevVAO = GL.GetInteger(GetPName.VertexArrayBinding);
            int prevArrayBuffer = GL.GetInteger(GetPName.ArrayBufferBinding);
            int prevProgram = GL.GetInteger(GetPName.CurrentProgram);
            bool prevBlendEnabled = GL.GetBoolean(GetPName.Blend);
            bool prevScissorTestEnabled = GL.GetBoolean(GetPName.ScissorTest);
            int prevBlendEquationRgb = GL.GetInteger(GetPName.BlendEquationRgb);
            int prevBlendEquationAlpha = GL.GetInteger(GetPName.BlendEquationAlpha);
            int prevBlendFuncSrcRgb = GL.GetInteger(GetPName.BlendSrcRgb);
            int prevBlendFuncSrcAlpha = GL.GetInteger(GetPName.BlendSrcAlpha);
            int prevBlendFuncDstRgb = GL.GetInteger(GetPName.BlendDstRgb);
            int prevBlendFuncDstAlpha = GL.GetInteger(GetPName.BlendDstAlpha);
            bool prevCullFaceEnabled = GL.GetBoolean(GetPName.CullFace);
            bool prevDepthTestEnabled = GL.GetBoolean(GetPName.DepthTest);
            int prevActiveTexture = GL.GetInteger(GetPName.ActiveTexture);
            GL.ActiveTexture(TextureUnit.Texture0);
            int prevTexture2D = GL.GetInteger(GetPName.TextureBinding2D);
            Span<int> prevScissorBox = stackalloc int[4];
            unsafe
            {
                fixed (int* iptr = &prevScissorBox[0])
                {
                    GL.GetInteger(GetPName.ScissorBox, iptr);
                }
            }

            // Bind the element buffer (thru the VAO) so that we can resize it.
            GL.BindVertexArray(_vertexArray);
            // Bind the vertex buffer so that we can resize it.
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
            for (int i = 0; i < draw_data.CmdListsCount; i++)
            {
                DrawList cmd_list = draw_data.DrawLists[i];

                int vertexSize = cmd_list.VtxBuffer.Length * Unsafe.SizeOf<ImDrawVert>();
                if (vertexSize > _vertexBufferSize)
                {
                    int newSize = (int)Math.Max(_vertexBufferSize * 1.5f, vertexSize);

                    GL.BufferData(BufferTarget.ArrayBuffer, newSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
                    _vertexBufferSize = newSize;

                    Console.WriteLine($"Resized dear imgui vertex buffer to new size {_vertexBufferSize}");
                }

                int indexSize = cmd_list.IdxBuffer.Length * sizeof(ushort);
                if (indexSize > _indexBufferSize)
                {
                    int newSize = (int)Math.Max(_indexBufferSize * 1.5f, indexSize);
                    GL.BufferData(BufferTarget.ElementArrayBuffer, newSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
                    _indexBufferSize = newSize;

                    Console.WriteLine($"Resized dear imgui index buffer to new size {_indexBufferSize}");
                }
            }

            // Setup orthographic projection matrix into our constant buffer
            Matrix4 mvp = Matrix4.CreateOrthographicOffCenter(
                0.0f,
                renderSize.x,
                renderSize.y,
                0.0f,
                -1.0f,
                1.0f);

            GL.UseProgram(_shader);
            GL.UniformMatrix4(_shaderProjectionMatrixLocation, false, ref mvp);
            GL.Uniform1(_shaderFontTextureLocation, 0);
            CheckGLError("Projection");

            GL.BindVertexArray(_vertexArray);
            CheckGLError("VAO");

            //draw_data.ScaleClipRects(io.DisplayFramebufferScale);

            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.ScissorTest);
            GL.BlendEquation(BlendEquationMode.FuncAdd);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.DepthTest);

            // Render command lists
            for (int n = 0; n < draw_data.CmdListsCount; n++)
            {
                DrawList cmd_list = draw_data.DrawLists[n];

                GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, cmd_list.VtxBuffer.Length * Unsafe.SizeOf<ImDrawVert>(), cmd_list.VtxPtr);
                CheckGLError($"Data Vert {n}");

                GL.BufferSubData(BufferTarget.ElementArrayBuffer, IntPtr.Zero, cmd_list.IdxBuffer.Length * sizeof(ushort), cmd_list.IdxPtr);
                CheckGLError($"Data Idx {n}");

                for (int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Length; cmd_i++)
                {
                    ImDrawCmd pcmd = cmd_list.CmdBuffer[cmd_i];
                    if (pcmd.UserCallback != IntPtr.Zero)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        GL.ActiveTexture(TextureUnit.Texture0);
                        GL.BindTexture(TextureTarget.Texture2D, (int)pcmd.TextureId);
                        CheckGLError("Texture");

                        // We do _windowHeight - (int)clip.W instead of (int)clip.Y because gl has flipped Y when it comes to these coordinates
                        var clip = pcmd.ClipRect;
                        GL.Scissor((int)clip.x, (int)renderSize.y - (int)clip.w, (int)(clip.z - clip.x), (int)(clip.w - clip.y));
                        CheckGLError("Scissor");

                        if ((backendFlags & ImGuiBackendFlags.RendererHasVtxOffset) != 0)
                        {
                            GL.DrawElementsBaseVertex(PrimitiveType.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, (IntPtr)(pcmd.IdxOffset * sizeof(ushort)), unchecked((int)pcmd.VtxOffset));
                        }
                        else
                        {
                            GL.DrawElements(BeginMode.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, (int)pcmd.IdxOffset * sizeof(ushort));
                        }
                        CheckGLError("Draw");
                    }
                }
            }

            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.ScissorTest);

            // Reset state
            GL.BindTexture(TextureTarget.Texture2D, prevTexture2D);
            GL.ActiveTexture((TextureUnit)prevActiveTexture);
            GL.UseProgram(prevProgram);
            GL.BindVertexArray(prevVAO);
            GL.Scissor(prevScissorBox[0], prevScissorBox[1], prevScissorBox[2], prevScissorBox[3]);
            GL.BindBuffer(BufferTarget.ArrayBuffer, prevArrayBuffer);
            GL.BlendEquationSeparate((BlendEquationMode)prevBlendEquationRgb, (BlendEquationMode)prevBlendEquationAlpha);
            GL.BlendFuncSeparate(
                (BlendingFactorSrc)prevBlendFuncSrcRgb,
                (BlendingFactorDest)prevBlendFuncDstRgb,
                (BlendingFactorSrc)prevBlendFuncSrcAlpha,
                (BlendingFactorDest)prevBlendFuncDstAlpha);
            if (prevBlendEnabled) GL.Enable(EnableCap.Blend); else GL.Disable(EnableCap.Blend);
            if (prevDepthTestEnabled) GL.Enable(EnableCap.DepthTest); else GL.Disable(EnableCap.DepthTest);
            if (prevCullFaceEnabled) GL.Enable(EnableCap.CullFace); else GL.Disable(EnableCap.CullFace);
            if (prevScissorTestEnabled) GL.Enable(EnableCap.ScissorTest); else GL.Disable(EnableCap.ScissorTest);
        }
        #endregion

        #region Textures
        /// <summary>
        /// Get a texture ID, need to be called into Unity Main thread
        /// </summary>
        /// <param name="texture">texture to get ID</param>
        /// <returns>ID ptr of this texture</returns>
        public IntPtr GetTextureID(UnityEngine.Texture2D texture)
        {
            // add texture if to list if don't already exists
            if (!_textures.ContainsKey(texture))
            {
                _textures.Add(texture, OpenGLTextureFactory.GetOpenGLTexture(texture));
            }
            // first time, it will return -1,
            // texture is not sended to GL context
            // we are not in the right thread to do that, it will be done into RenderImDrawData()
            return (IntPtr)_textures[texture].TexturePtr;
        }

        /// <summary>
        /// Get a texture ID, need to be called into Unity Main thread
        /// </summary>
        /// <param name="texture">texture to get ID</param>
        /// <returns>ID ptr of this texture</returns>
        public IntPtr GetTextureID(UnityEngine.RenderTexture texture)
        {
            // add texture if to list if don't already exists
            if (!_textures.ContainsKey(texture))
            {
                _textures.Add(texture, OpenGLTextureFactory.GetOpenGLTexture(texture));
            }
            else
            {
                // if texture already exist into local list,
                // let's get new texture data while we are into main thread
                // texture data will be updated into GLTex into RenderImDrawData()
                _textures[texture].GetTextureData();
            }
            // first time, it will return -1,
            // texture is not sended to GL context
            // we are not in the right thread to do that, it will be done into RenderImDrawData()
            return (IntPtr)_textures[texture].TexturePtr;
        }
        #endregion
    }
}