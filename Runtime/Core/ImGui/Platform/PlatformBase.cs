using ImGuiNET;
using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace Fu.Core.DearImGui.Platform
{
    internal abstract class PlatformBase
    {
        protected readonly PlatformCallbacks _callbacks = new PlatformCallbacks();
        protected ImGuiMouseCursor _lastCursor = ImGuiMouseCursor.COUNT;

        private const int TargetCursorSize = 32;

        internal PlatformBase() { }

        public virtual bool Initialize(ImGuiIOPtr io, ImGuiPlatformIOPtr pio, string platformName = null)
        {
            io.SetBackendPlatformName(platformName);
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

        public virtual void PrepareFrame(ImGuiIOPtr io, Rect displayRect, bool updateMouse, bool updateKeyboard)
        {
            Assert.IsTrue(io.Fonts.IsBuilt(), "Font atlas not built! Generally built by the renderer. Missing call to renderer NewFrame() function?");
            io.DisplaySize = displayRect.size;
            io.DeltaTime = Time.unscaledDeltaTime;
        }

        public virtual void Shutdown(ImGuiIOPtr io, ImGuiPlatformIOPtr pio)
        {
            io.SetBackendPlatformName(null);
            _callbacks.Unset(pio);
        }

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
                if (texture.width > TargetCursorSize || texture.height > TargetCursorSize)
                {
                    texture = ResizeCursorTexture(texture, TargetCursorSize, TargetCursorSize);
                }

                // Hotspot ajusté proportionnellement
                Vector2 hotspot = new Vector2(
                    Mathf.Clamp(shape.Hotspot.x * ((float)texture.width / shape.Texture.width), 0, texture.width),
                    Mathf.Clamp(shape.Hotspot.y * ((float)texture.height / shape.Texture.height), 0, texture.height)
                );

                Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
            }
        }

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

        private Texture2D RebuildCursorTexture(Texture2D source)
        {
            Texture2D rebuilt = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            Graphics.CopyTexture(source, rebuilt);
            rebuilt.Apply();
            return rebuilt;
        }
    }
}
