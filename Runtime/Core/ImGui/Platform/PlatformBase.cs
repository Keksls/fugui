using ImGuiNET;
using System;
using Fu.Core.DearImGui.Assets;
using UnityEngine;
using UnityEngine.Assertions;

namespace Fu.Core.DearImGui.Platform
{
    internal class PlatformBase : IPlatform
    {
        protected readonly IniSettingsAsset _iniSettings;
        protected readonly CursorShapesAsset _cursorShapes;

        protected readonly PlatformCallbacks _callbacks = new PlatformCallbacks();

        protected ImGuiMouseCursor _lastCursor = ImGuiMouseCursor.COUNT;

        internal PlatformBase(CursorShapesAsset cursorShapes, IniSettingsAsset iniSettings)
        {
            _cursorShapes = cursorShapes;
            _iniSettings = iniSettings;
        }

        /// <summary>
        /// Initialize the platform backend.
        /// </summary>
        /// <param name="io"> ImGuiIOPtr instance to set up the platform backend.</param>
        /// <param name="pio"> ImGuiPlatformIOPtr instance to set up the platform backend.</param>
        /// <param name="platformName"> Name of the platform, used for debugging purposes.</param>
        /// <returns> True if the platform backend was successfully initialized, false otherwise.</returns>
        public virtual bool Initialize(ImGuiIOPtr io, ImGuiPlatformIOPtr pio, string platformName)
        {
            io.SetBackendPlatformName("Unity Input System");
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

            if (_iniSettings != null)
            {
                ImGui.LoadIniSettingsFromMemory(_iniSettings.Load());
            }

            return true;
        }

        /// <summary>
        /// Prepare the frame for rendering.
        /// </summary>
        /// <param name="io"> ImGuiIOPtr instance to prepare the frame.</param>
        /// <param name="displayRect"> The display rectangle for the frame, used to set the display size.</param>
        /// <param name="updateMouse"> Whether to update mouse input for the frame.</param>
        /// <param name="updateKeyboard"> Whether to update keyboard input for the frame.</param>
        public virtual void PrepareFrame(ImGuiIOPtr io, Rect displayRect, bool updateMouse, bool updateKeyboard)
        {
            Assert.IsTrue(io.Fonts.IsBuilt(), "Font atlas not built! Generally built by the renderer. Missing call to renderer NewFrame() function?");

            io.DisplaySize = displayRect.size; // TODO: dpi aware, scale, etc.

            io.DeltaTime = Time.unscaledDeltaTime;

            if (_iniSettings != null && io.WantSaveIniSettings)
            {
                _iniSettings.Save(ImGui.SaveIniSettingsToMemory());
                io.WantSaveIniSettings = false;
            }
        }

        /// <summary>
        /// Shutdown the platform backend.
        /// </summary>
        /// <param name="io"> ImGuiIOPtr instance to shut down the platform backend.</param>
        /// <param name="pio"> ImGuiPlatformIOPtr instance to shut down the platform backend.</param>
        public virtual void Shutdown(ImGuiIOPtr io, ImGuiPlatformIOPtr pio)
        {
            io.SetBackendPlatformName(null);
            _callbacks.Unset(pio);
        }

        /// <summary>
        /// Update the cursor based on the current ImGui state.
        /// </summary>
        /// <param name="io"> ImGuiIOPtr instance to update the cursor.</param>
        /// <param name="cursor"> The current ImGui mouse cursor type.</param>
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
            Cursor.visible = cursor != ImGuiMouseCursor.None; // Hide cursor if ImGui is drawing it or if it wants no cursor.
            if (_cursorShapes != null)
            {
                Cursor.SetCursor(_cursorShapes[cursor].Texture, _cursorShapes[cursor].Hotspot, CursorMode.Auto);
            }
        }
    }
}