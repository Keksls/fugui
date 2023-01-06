using System;
using Fugui.Framework;
using OpenTK.Graphics.OpenGL;
using Unity.Collections;
using UnityEngine.Rendering;

namespace Fugui.Core
{
    public static class OpenGLTextureFactory
    {
        /// <summary>
        /// Generates an OpenGLTexture object from a UnityEngine.Texture object. If the input texture is not readable, a default error message texture is used instead.
        /// </summary>
        /// <param name="texture">The UnityEngine.Texture object to generate the OpenGLTexture from.</param>
        /// <returns>An instance of OpenGLTexture.</returns>
        public static OpenGLTexture GetOpenGLTexture(UnityEngine.Texture texture)
        {
            // check whatever the texture is read/write enabled
            // if not, display defaut error message texture
            if (!texture.isReadable)
            {
                texture = FuGui.Settings.OpenGLNonReadableTexture;
            }

            // generate OpenGLTexture for RenderTexture and Texture2D
            if (texture is UnityEngine.RenderTexture)
            {
                return new DynamicOpenGLTexture((UnityEngine.RenderTexture)texture);
            }
            else
            {
                return new StaticOpenGLTexture((UnityEngine.Texture2D)texture);
            }
        }
    }

    /// <summary>
    /// class that represent a Link between Unity generic graphic context
    /// and OpenTk OpenGL GL context
    /// </summary>
    public abstract class OpenGLTexture
    {
        // OpenTK Gl related texture Ptr (GL ID)
        public int TexturePtr;
        // OpenTK GL related texture's pixels Ptr (GL ID)
        public IntPtr TextureDataPtr;
        public int Width; // width of the texture
        public int Height; // height of the texture
        // Is this texture Dynamic ? true for render texture
        // if true, means that this texture will be updated any frames
        public bool IsRegistered; // is this texture registred into GL context
        public UnityEngine.Texture Texture; // related Unity Texture
        protected UnityEngine.Color[] _pixels = new UnityEngine.Color[0];

        protected OpenGLTexture(UnityEngine.Texture texture)
        {
            Texture = texture;
            Width = texture.width;
            Height = texture.height;
            IsRegistered = false;
            TexturePtr = -1;
            TextureDataPtr = IntPtr.Zero;
        }

        /// <summary>
        /// Get Unity texture data and store it to send to GL context once on the right thread
        /// ! [Main thread]
        /// </summary>
        public abstract void GetTextureData();

        /// <summary>
        /// Dispose GL texture
        /// ! [OpenGL Thread]
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// Create GL texture
        /// ! [OpenGL Thread]
        /// </summary>
        public abstract void CreateTexture();

        /// <summary>
        /// Update GL texture
        /// ! [OpenGL Thread]
        /// </summary>
        public abstract void UpdateTexture();
    }

    // Subclass for static textures (e.g. Texture2D)
    public class StaticOpenGLTexture : OpenGLTexture
    {
        public StaticOpenGLTexture(UnityEngine.Texture2D texture) : base(texture)
        {
            GetTextureData();
        }

        /// <summary>
        /// Dispose GL texture
        /// ! [OpenGL Thread]
        /// </summary>
        public override void Dispose()
        {
            GL.DeleteTexture(TexturePtr);
        }

        /// <summary>
        /// Get Unity texture data and store it to send to GL context once on the right thread
        /// ! [Main thread]
        /// </summary>
        public unsafe override void GetTextureData()
        {
            _pixels = ((UnityEngine.Texture2D)Texture).GetPixels();

            // get pixels array fixed ptr (fixed means Garbage collector will not moveor destroy this ptr)
            // need this to send data into Graphic Card
            fixed (void* ptr = _pixels)
            {
                TextureDataPtr = (IntPtr)ptr;
            }
        }

        /// <summary>
        /// Create GL texture
        /// ! [OpenGL Thread]
        /// </summary>
        public override void CreateTexture()
        {
            if (IsRegistered || TextureDataPtr == IntPtr.Zero)
            {
                return;
            }
            IsRegistered = true;

            int prevActiveTexture = GL.GetInteger(GetPName.ActiveTexture);
            GL.ActiveTexture(TextureUnit.Texture0);
            int prevTexture2D = GL.GetInteger(GetPName.TextureBinding2D);

            TexturePtr = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, TexturePtr);
            GL.TexStorage2D(TextureTarget2d.Texture2D, 1, SizedInternalFormat.Rgba32f, Width, Height);

            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, Width, Height, PixelFormat.Rgba, PixelType.Float, TextureDataPtr);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);

            // Restore state
            GL.BindTexture(TextureTarget.Texture2D, prevTexture2D);
            GL.ActiveTexture((TextureUnit)prevActiveTexture);
        }

        /// <summary>
        /// Update GL texture
        /// ! [OpenGL Thread]
        /// </summary>
        public override void UpdateTexture()
        {
        }
    }

    // Subclass for dynamic textures (e.g. RenderTexture)
    public class DynamicOpenGLTexture : OpenGLTexture
    {
        public bool NeedToRecreate; // do we need to recreate this texture
        private int _framebufferObjectPtr;
        private bool _requestingGPU = false;
        private bool _updatingTexture = false;
        private NativeArray<UnityEngine.Color> _gpuPixelsBuffer;

        public unsafe DynamicOpenGLTexture(UnityEngine.RenderTexture texture) : base(texture)
        {
            _framebufferObjectPtr = -1;
            Texture = texture;
            Width = texture.width;
            Height = texture.height;
            TextureDataPtr = IntPtr.Zero;
            _gpuPixelsBuffer = new NativeArray<UnityEngine.Color>(Width * Height, Allocator.Persistent);
            GetTextureData();
        }

        /// <summary>
        /// Dispose GL texture
        /// ! [OpenGL Thread]
        /// </summary>
        public override void Dispose()
        {
            GL.DeleteTexture(TexturePtr);
            GL.DeleteFramebuffer(_framebufferObjectPtr);
            _gpuPixelsBuffer.Dispose();
        }

        /// <summary>
        /// Get Unity texture data and store it to send to GL context once on the right thread
        /// ! [Main thread]
        /// </summary>
        public unsafe override void GetTextureData()
        {
            // immediate return if we are already requesting GPU data
            if(_requestingGPU || _updatingTexture)
            {
                return;
            }

            // we start requesting GPU
            _requestingGPU = true;

            // check whatever texture has been resized
            if (Texture.width != Width || Texture.height != Height)
            {
                NeedToRecreate = true;
                Width = Texture.width;
                Height = Texture.height;
                _gpuPixelsBuffer.Dispose();
                _gpuPixelsBuffer = new NativeArray<UnityEngine.Color>(Width * Height, Allocator.Persistent);
            }

            // async gpu request for renderTexture Data
            AsyncGPUReadback.RequestIntoNativeArray(ref _gpuPixelsBuffer, Texture, 0, UnityEngine.TextureFormat.RGBAFloat, (req) =>
            {
                // GPU request ended
                _requestingGPU = false;
                _updatingTexture = true;

                // check whatever GPU request just fail
                if (req.hasError || !req.done)
                {
                    UnityEngine.Debug.Log("AsyncGPUReadback request Error");
                    return;
                }

                // check whatever pixel data match texture size
                if(_gpuPixelsBuffer.Length != Width * Height)
                {
                    // pixel size missmatch, texture has been resized during async operation
                    // we must abort updating pixels buffer Ptr
                    return;
                }

                // copy pixels to ptr buffer (_gpuPixelsBuffer will be destroyed too soon before updating Tex2D into render thread)
                _pixels = _gpuPixelsBuffer.ToArray();
                // get pointer of the ram copy to send it to GC FBO
                fixed (void* ptr = _pixels)
                {
                    TextureDataPtr = (IntPtr)ptr;
                }
            });
        }

        /// <summary>
        /// Create GL texture
        /// ! [OpenGL Thread]
        /// </summary>
        public override void CreateTexture()
        {
            if (IsRegistered && !NeedToRecreate || _requestingGPU)
            {
                return;
            }
            _updatingTexture = true;
            IsRegistered = true;

            if (NeedToRecreate)
            {
                GL.DeleteTexture(TexturePtr);
                GL.DeleteFramebuffer(_framebufferObjectPtr);
            }

            int prevActiveTexture = GL.GetInteger(GetPName.ActiveTexture);
            GL.ActiveTexture(TextureUnit.Texture0);
            int prevTexture2D = GL.GetInteger(GetPName.TextureBinding2D);
            int prevFBO = GL.GetInteger(GetPName.FramebufferBinding);

            // Generate and bind the FBO
            _framebufferObjectPtr = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebufferObjectPtr);

            // Create and bind the texture
            TexturePtr = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, TexturePtr);

            // Set the texture parameters
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);

            // Allocate storage for the texture
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, Width, Height, 0, PixelFormat.Rgba, PixelType.Float, TextureDataPtr);

            // Attach the texture to the FBO
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, TexturePtr, 0);

            // Restore state
            GL.BindTexture(TextureTarget.Texture2D, prevTexture2D);
            GL.ActiveTexture((TextureUnit)prevActiveTexture);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, prevFBO);

            NeedToRecreate = false;
            _updatingTexture = false;
        }

        /// <summary>
        /// Update GL texture
        /// ! [OpenGL Thread]
        /// </summary>
        public override void UpdateTexture()
        {
            if (!IsRegistered || _requestingGPU)
            {
                return;
            }
            _updatingTexture = true;

            int prevActiveTexture = GL.GetInteger(GetPName.ActiveTexture);
            GL.ActiveTexture(TextureUnit.Texture0);
            int prevTexture2D = GL.GetInteger(GetPName.TextureBinding2D);
            int prevFBO = GL.GetInteger(GetPName.FramebufferBinding);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebufferObjectPtr);
            GL.BindTexture(TextureTarget.Texture2D, TexturePtr);
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, Width, Height, PixelFormat.Rgba, PixelType.Float, TextureDataPtr);

            // Restore state
            GL.BindTexture(TextureTarget.Texture2D, prevTexture2D);
            GL.ActiveTexture((TextureUnit)prevActiveTexture);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, prevFBO);

            _updatingTexture = false;
        }
    }
}