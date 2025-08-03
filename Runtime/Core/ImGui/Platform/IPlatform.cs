using ImGuiNET;
using UnityEngine;

namespace Fu.Core.DearImGui.Platform
{
    /// <summary>
    /// Platform bindings for ImGui in Unity in charge of: mouse/keyboard/gamepad inputs, cursor shape, timing, windowing.
    /// </summary>
    internal interface IPlatform
    {
        /// <summary>
        /// Initialize the platform bindings.
        /// </summary>
        /// <param name="io"> ImGuiIOPtr instance containing IO data.</param>
        /// <param name="pio"> ImGuiPlatformIOPtr instance containing platform IO data.</param>
        /// <param name="platformName"> Name of the platform.</param>
        /// <returns> true if initialization was successful, false otherwise.</returns>
        bool Initialize(ImGuiIOPtr io, ImGuiPlatformIOPtr pio, string platformName);

        /// <summary>
        /// Shutdown the platform bindings.
        /// </summary>
        /// <param name="io"> ImGuiIOPtr instance containing IO data.</param>
        /// <param name="pio"> ImGuiPlatformIOPtr instance containing platform IO data.</param>
        void Shutdown(ImGuiIOPtr io, ImGuiPlatformIOPtr pio);

        /// <summary>
        /// Prepare the frame for rendering.
        /// </summary>
        /// <param name="io"> ImGuiIOPtr instance containing IO data.</param>
        /// <param name="displayRect"> The rectangle representing the display area.</param>
		/// <param name="updateMouse"> Whether to update mouse input.</param>
		/// <param name="updateKeyboard"> Whether to update keyboard input.</param>
        void PrepareFrame(ImGuiIOPtr io, Rect displayRect, bool updateMouse, bool updateKeyboard);
    }
}