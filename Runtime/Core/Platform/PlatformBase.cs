using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;

namespace Fu
{
    public abstract class PlatformBase
    {
        public readonly PlatformCallbacks _callbacks = new PlatformCallbacks();
        public ImGuiMouseCursor _lastCursor = ImGuiMouseCursor.COUNT;
        private readonly HashSet<IntPtr> _managedAllocations = new HashSet<IntPtr>();

        public PlatformBase() { }

        /// <summary>
        /// Initialize the platform backend with the given ImGuiIO and ImGuiPlatformIO instances.
        /// </summary>
        /// <param name="io"> The ImGuiIOPtr instance. </param>
        /// <param name="pio"> The ImGuiPlatformIOPtr instance. </param>
        /// <param name="platformName"> The name for the platform backend. </param>
        /// <returns> True if initialization was successful, false otherwise. </returns>
        public virtual bool Initialize(ImGuiIOPtr io, ImGuiPlatformIOPtr pio, string platformName = null)
        {
            SetBackendPlatformName(io, platformName);
            io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;

            if ((Fugui.Settings.ImGuiConfig & ImGuiConfigFlags.NavEnableKeyboard) != 0 ||
                (Fugui.Settings.ImGuiConfig & ImGuiConfigFlags.NavEnableGamepad) != 0)
            {
                io.BackendFlags |= ImGuiBackendFlags.HasSetMousePos;
                io.WantSetMousePos = true;
            }
            else
            {
                io.BackendFlags &= ~ImGuiBackendFlags.HasSetMousePos;
                io.WantSetMousePos = false;
            }

            unsafe
            {
                PlatformCallbacks.SetClipboardFunctions(PlatformCallbacks.GetClipboardTextCallback, PlatformCallbacks.SetClipboardTextCallback);
            }

            _callbacks.Assign(pio);
            pio.Platform_ClipboardUserData = IntPtr.Zero;
            return true;
        }

        /// <summary>
        /// Perform per-frame setup tasks, such as updating display size and delta time.
        /// </summary>
        /// <param name="io"> The ImGuiIOPtr instance. </param>
        /// <param name="displayRect"> The current display rectangle. </param>
        /// <param name="updateMouse"> Whether to update mouse state. </param>
        /// <param name="updateKeyboard"> Whether to update keyboard state. </param>
        public virtual void PrepareFrame(ImGuiIOPtr io, Rect displayRect, bool updateMouse, bool updateKeyboard)
        {
            //Assert.IsTrue(io.Fonts.IsBuilt(), "Font atlas not built! Generally built by the renderer. Missing call to renderer NewFrame() function?");
            io.DisplaySize = displayRect.size;
            io.DeltaTime = Time.unscaledDeltaTime;
        }

        /// <summary>
        /// Perform end-of-frame tasks, such as updating the mouse cursor.
        /// </summary>
        /// <param name="io"> The ImGuiIOPtr instance. </param>
        /// <param name="pio"> The ImGuiPlatformIOPtr instance. </param>
        public virtual void Shutdown(ImGuiIOPtr io, ImGuiPlatformIOPtr pio)
        {
            SetBackendPlatformName(io, null);
            _callbacks.Unset(pio);
        }

        /// <summary>
        /// Update the mouse cursor based on ImGui's current state.
        /// </summary>
        /// <param name="io"> The ImGuiIOPtr instance. </param>
        /// <param name="cursor"> The current ImGuiMouseCursor. </param>
        protected void UpdateCursor(ImGuiIOPtr io, ImGuiMouseCursor cursor)
        {
            if (Fugui.IsCursorLocked)
                return;

            if (cursor == ImGuiMouseCursor.Arrow && ImGui.IsAnyItemHovered())
                cursor = ImGuiMouseCursor.Hand;

            if (io.MouseDrawCursor)
            {
                cursor = ImGuiMouseCursor.None;
            }

            if (_lastCursor == cursor && !Fugui.CursorsJustUnlocked)
            {
                Fugui.CursorsJustUnlocked = false;
                return;
            }

            Fugui.CursorsJustUnlocked = false;
            if ((io.ConfigFlags & ImGuiConfigFlags.NoMouseCursorChange) != 0) return;

            _lastCursor = cursor;
            Cursor.visible = cursor != ImGuiMouseCursor.None;

            if (Fugui.Settings.CursorShapes != null && Fugui.Settings.CursorShapes[cursor].Texture != null)
            {
                var shape = Fugui.Settings.CursorShapes[cursor];
                Texture2D texture = shape.Texture;

                // Vérification des contraintes Unity
                if (!texture.isReadable || texture.mipmapCount > 1 || texture.format != TextureFormat.RGBA32)
                {
                    texture = RebuildCursorTexture(texture);
                }

                // Redimensionnement si nécessaire
                if (texture.width != Fugui.Settings.TargetCursorSize || texture.height != Fugui.Settings.TargetCursorSize)
                {
                    texture = ResizeCursorTexture(texture, Fugui.Settings.TargetCursorSize, Fugui.Settings.TargetCursorSize);
                }

                // Hotspot ajusté proportionnellement
                Vector2 hotspot = new Vector2(
                    Mathf.Clamp(shape.Hotspot.x * ((float)texture.width / shape.Texture.width), 0, texture.width),
                    Mathf.Clamp(shape.Hotspot.y * ((float)texture.height / shape.Texture.height), 0, texture.height)
                );

                Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
            }
        }

        /// <summary>
        /// Resize the cursor texture to fit within the target dimensions while maintaining aspect ratio.
        /// </summary>
        /// <param name="source"> The source texture. </param>
        /// <param name="targetWidth"> </param>
        /// <param name="targetHeight" > </param>
        /// <returns> A new Texture2D that fits within the target dimensions. </returns>
        private Texture2D ResizeCursorTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
            Graphics.Blit(source, rt);
            RenderTexture.active = rt;

            Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            result.Apply();

            RenderTexture.ReleaseTemporary(rt);
            RenderTexture.active = null;

            return result;
        }

        /// <summary>
        /// Rebuild the cursor texture to ensure it is readable and in RGBA32 format.
        /// </summary>
        /// <param name="source"> The source texture. </param>
        /// <returns> A new Texture2D that is readable and in RGBA32 format. </returns>
        private Texture2D RebuildCursorTexture(Texture2D source)
        {
            Texture2D rebuilt = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            Graphics.CopyTexture(source, rebuilt);
            rebuilt.Apply();
            return rebuilt;
        }

        /// <summary>
        /// Set the backend platform name in a safe manner, managing memory allocations.
        /// </summary>
        /// <param name="io"> The ImGuiIOPtr instance. </param>
        /// <param name="name"> The name to set, or null to clear. </param>
        private unsafe void SetBackendPlatformName(ImGuiIOPtr io, string name)
        {
            if (io.NativePtr->BackendPlatformName != (byte*)0)
            {
                if (_managedAllocations.Contains((IntPtr)io.NativePtr->BackendPlatformName))
                {
                    Marshal.FreeHGlobal(new IntPtr(io.NativePtr->BackendPlatformName));
                }
                io.NativePtr->BackendPlatformName = (byte*)0;
            }
            if (name != null)
            {
                int byteCount = Encoding.UTF8.GetByteCount(name);
                byte* nativeName = (byte*)Marshal.AllocHGlobal(byteCount + 1);
                int offset = Fugui.GetUtf8(name, nativeName, byteCount);

                nativeName[offset] = 0;

                io.NativePtr->BackendPlatformName = nativeName;
                _managedAllocations.Add((IntPtr)nativeName);
            }
        }
    }
}
