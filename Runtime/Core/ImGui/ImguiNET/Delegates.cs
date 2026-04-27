using System;
using UnityEngine;

namespace ImGuiNET
{
    /// <summary>
    /// Defines the Platform Create Window callback signature.
    /// </summary>
    /// <param name="vp">The vp value.</param>
    public delegate void Platform_CreateWindow(ImGuiViewportPtr vp);                    // Create a new platform window for the given viewport
    /// <summary>
    /// Defines the Platform Destroy Window callback signature.
    /// </summary>
    /// <param name="vp">The vp value.</param>
    public delegate void Platform_DestroyWindow(ImGuiViewportPtr vp);
    /// <summary>
    /// Defines the Platform Show Window callback signature.
    /// </summary>
    /// <param name="vp">The vp value.</param>
    public delegate void Platform_ShowWindow(ImGuiViewportPtr vp);                      // Newly created windows are initially hidden so SetWindowPos/Size/Title can be called on them first
    /// <summary>
    /// Defines the Platform Set Window Pos callback signature.
    /// </summary>
    /// <param name="vp">The vp value.</param>
    /// <param name="pos">The pos value.</param>
    public delegate void Platform_SetWindowPos(ImGuiViewportPtr vp, Vector2 pos);
    /// <summary>
    /// Defines the Platform Get Window Pos callback signature.
    /// </summary>
    /// <param name="vp">The vp value.</param>
    /// <param name="outPos">The out Pos value.</param>
    public unsafe delegate void Platform_GetWindowPos(ImGuiViewportPtr vp, Vector2* outPos);
    /// <summary>
    /// Defines the Platform Set Window Size callback signature.
    /// </summary>
    /// <param name="vp">The vp value.</param>
    /// <param name="size">The size value.</param>
    public delegate void Platform_SetWindowSize(ImGuiViewportPtr vp, Vector2 size);
    /// <summary>
    /// Defines the Platform Get Window Size callback signature.
    /// </summary>
    /// <param name="vp">The vp value.</param>
    /// <param name="outSize">The out Size value.</param>
    public unsafe delegate void Platform_GetWindowSize(ImGuiViewportPtr vp, Vector2* outSize);
    /// <summary>
    /// Defines the Platform Set Window Focus callback signature.
    /// </summary>
    /// <param name="vp">The vp value.</param>
    public delegate void Platform_SetWindowFocus(ImGuiViewportPtr vp);                  // Move window to front and set input focus
    /// <summary>
    /// Defines the Platform Get Window Focus callback signature.
    /// </summary>
    /// <param name="vp">The vp value.</param>
    /// <returns>The result of the operation.</returns>
    public delegate byte Platform_GetWindowFocus(ImGuiViewportPtr vp);
    /// <summary>
    /// Defines the Platform Get Window Minimized callback signature.
    /// </summary>
    /// <param name="vp">The vp value.</param>
    /// <returns>The result of the operation.</returns>
    public delegate byte Platform_GetWindowMinimized(ImGuiViewportPtr vp);
    /// <summary>
    /// Defines the Platform Set Window Title callback signature.
    /// </summary>
    /// <param name="vp">The vp value.</param>
    /// <param name="title">The title value.</param>
    public delegate void Platform_SetWindowTitle(ImGuiViewportPtr vp, IntPtr title);
}