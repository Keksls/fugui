// define it to debug whatever Color or Styles are pushed (avoid stack leak metrics)
// it's ressourcefull, si comment it when debug is done. Ensure it's commented before build.
//#define FUDEBUG
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR && !FUMOBILE
#define FUMOBILE
#endif
using Fu.Framework;
using ImGuiNET;
#if FU_EXTERNALIZATION
using SDL2;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;

namespace Fu
{
    /// <summary>
    /// Fugui window definitions and registry.
    /// </summary>
    public static partial class Fugui
    {
        /// <summary>
        /// Registers a window definition.
        /// </summary>
        /// <param name="windowDefinition">The window definition to be registered.</param>
        /// <returns>True if the window definition was successfully registered, false otherwise.</returns>
        public static bool RegisterWindowDefinition(FuWindowDefinition windowDefinition)
        {
            // Check if a window definition with the same ID already exists
            if (UIWindowsDefinitions.ContainsKey(windowDefinition.WindowName))
            {
                return false;
            }
            // Add the window definition to the list
            UIWindowsDefinitions.Add(windowDefinition.WindowName, windowDefinition);
            return true;
        }

        /// <summary>
        /// Unregisters a window definition.
        /// </summary>
        /// <param name="windowDefinition">The window definition to be unregistered.</param>
        /// <returns>True if the window definition was successfully unregistered, false otherwise.</returns>
        public static bool UnregisterWindowDefinition(FuWindowDefinition windowDefinition)
        {
            // Check if a window definition with the same ID already exists
            return UIWindowsDefinitions.Remove(windowDefinition.WindowName);
        }

        /// <summary>
        /// Unregisters a window definition.
        /// </summary>
        /// <param name="windowDefinitionName">The name of the window definition to be unregistered.</param>
        /// <returns>True if the window definition was successfully unregistered, false otherwise.</returns>
        public static bool UnregisterWindowDefinition(FuWindowName windowDefinitionName)
        {
            // Check if a window definition with the same ID already exists
            return UIWindowsDefinitions.Remove(windowDefinitionName);
        }

        /// <summary>
        /// Closes all UI windows.
        /// </summary>
        public static void CloseAllWindows()
        {
            CloseAllWindowsAsync(null);
        }

        /// <summary>
        /// Closes all UI windows and invokes a callback when every container has removed its window.
        /// </summary>
        /// <param name="callback">A callback to be invoked after all windows are closed.</param>
        public static void CloseAllWindowsAsync(Action callback)
        {
            ForceDrawAllWindows();

            List<FuWindow> windows = UIWindows.Values.ToList();
            if (windows.Count == 0)
            {
                callback?.Invoke();
                return;
            }

            foreach (FuWindow window in windows)
            {
                void onWindowClosed(FuWindow closedWindow)
                {
                    closedWindow.OnClosed -= onWindowClosed;
                    if (UIWindows.Count == 0)
                    {
                        callback?.Invoke();
                    }
                }

                window.OnClosed += onWindowClosed;
                window.Close();
            }
        }

        /// <summary>
        /// Creates a UI window.
        /// </summary>
        /// <param name="windowToGet">The window name to create.</param>
        /// <param name="autoAddToMainContainer">Add the window to the main container.</param>
        /// <returns>The created window, or null if creation failed.</returns>
        public static FuWindow CreateWindow(FuWindowName windowToGet, bool autoAddToMainContainer = true)
        {
            List<(FuWindowName, FuWindow)> windows = CreateWindows(
                new List<FuWindowName> { windowToGet },
                autoAddToMainContainer);

            if (windows.Count == 0)
            {
                return null;
            }

            return windows[0].Item2;
        }

        /// <summary>
        /// Creates a UI window and invokes the callback once the window is ready.
        /// </summary>
        /// <param name="windowToGet">The window name to create.</param>
        /// <param name="callback">A callback invoked with the created window, or null if creation failed.</param>
        /// <param name="autoAddToMainContainer">Add the window to the main container.</param>
        public static void CreateWindowAsync(FuWindowName windowToGet, Action<FuWindow> callback, bool autoAddToMainContainer = true)
        {
            CreateWindowsAsync(new List<FuWindowName> { windowToGet }, (windows) =>
            {
                if (windows.Count > 0 && windows[0].Item1.Equals(windowToGet))
                {
                    callback?.Invoke(windows[0].Item2);
                    return;
                }

                callback?.Invoke(null);
            }, autoAddToMainContainer);
        }

        /// <summary>
        /// Creates UI windows.
        /// </summary>
        /// <param name="windowsToGet">The window names to create.</param>
        /// <param name="autoAddToMainContainer">Add the windows to the main container.</param>
        /// <returns>The created windows.</returns>
        public static List<(FuWindowName, FuWindow)> CreateWindows(List<FuWindowName> windowsToGet, bool autoAddToMainContainer = true)
        {
            List<(FuWindowName, FuWindow)> windows = new List<(FuWindowName, FuWindow)>();

            foreach (FuWindowName windowName in windowsToGet)
            {
                if (!UIWindowsDefinitions.TryGetValue(windowName, out FuWindowDefinition windowDefinition))
                {
                    continue;
                }

                if (!windowDefinition.CreateUIWindow(out FuWindow window))
                {
                    continue;
                }

                if (autoAddToMainContainer)
                {
                    window.Size = GetScaledMainContainerWindowSize(windowDefinition);

                    window.TryAddToContainer(DefaultContainer);
                }

                window.ForceDraw();
                windows.Add((windowDefinition.WindowName, window));
            }

            return windows;
        }

        /// <summary>
        /// Creates UI windows and invokes the callback once every requested window has either failed or is ready.
        /// </summary>
        /// <param name="windowsToGet">The window names to create.</param>
        /// <param name="callback">A callback invoked with the created windows.</param>
        /// <param name="autoAddToMainContainer">Add the windows to the main container.</param>
        public static void CreateWindowsAsync(List<FuWindowName> windowsToGet, Action<List<(FuWindowName, FuWindow)>> callback, bool autoAddToMainContainer = true)
        {
            List<(FuWindowName, FuWindow)> windows = new List<(FuWindowName, FuWindow)>();
            List<FuWindowDefinition> windowDefinitions = new List<FuWindowDefinition>();

            foreach (FuWindowName windowName in windowsToGet)
            {
                if (UIWindowsDefinitions.TryGetValue(windowName, out FuWindowDefinition windowDefinition))
                {
                    windowDefinitions.Add(windowDefinition);
                }
            }

            if (windowDefinitions.Count == 0)
            {
                callback?.Invoke(windows);
                return;
            }

            int pendingWindows = windowDefinitions.Count;

            void completeOne()
            {
                pendingWindows--;
                if (pendingWindows <= 0)
                {
                    callback?.Invoke(windows);
                }
            }

            foreach (FuWindowDefinition windowDefinition in windowDefinitions)
            {
                if (!windowDefinition.CreateUIWindow(out FuWindow window))
                {
                    completeOne();
                    continue;
                }

                if (!autoAddToMainContainer)
                {
                    window.ForceDraw();
                    windows.Add((windowDefinition.WindowName, window));
                    completeOne();
                    continue;
                }

                Action<FuWindow> onWindowInitialized = null;
                onWindowInitialized = (initializedWindow) =>
                {
                    initializedWindow.OnInitialized -= onWindowInitialized;
                    initializedWindow.ForceDraw();
                    windows.Add((windowDefinition.WindowName, initializedWindow));
                    completeOne();
                };

                window.OnInitialized += onWindowInitialized;
                window.Size = GetScaledMainContainerWindowSize(windowDefinition);

                if (!window.TryAddToContainer(DefaultContainer))
                {
                    window.OnInitialized -= onWindowInitialized;
                    window.ForceDraw();
                    windows.Add((windowDefinition.WindowName, window));
                    completeOne();
                }
            }
        }

        /// <summary>
        /// Converts a window definition size authored at the reference resolution into the main container scale.
        /// </summary>
        /// <param name="windowDefinition">The window definition to scale.</param>
        /// <returns>The scaled runtime window size.</returns>
        private static Vector2Int GetScaledMainContainerWindowSize(FuWindowDefinition windowDefinition)
        {
            float scale = DefaultContainer != null && DefaultContainer.Context != null
                ? DefaultContainer.Context.Scale
                : 1f;

            return new Vector2Int(
                Mathf.Max(1, Mathf.RoundToInt(windowDefinition.Size.x * scale)),
                Mathf.Max(1, Mathf.RoundToInt(windowDefinition.Size.y * scale)));
        }

        /// <summary>
        /// Try to add UIWindow to the current displayed windows List
        /// must be called by UIWindow constructor.
        /// </summary>
        /// <param name="window">the window to add</param>
        /// <returns>true if success</returns>
        internal static bool TryAddUIWindow(FuWindow window)
        {
            // check whatever a window is already open with the same ID
            if (UIWindows.ContainsKey(window.ID))
            {
                window.TryRemoveFromContainer();
                return false;
            }

            // add the window to the list of current opened windows
            UIWindows.Add(window.ID, window);
            return true;
        }

        /// <summary>
        /// try remove UIWindow from current opened windows list
        /// </summary>
        /// <param name="window">window to remove from the list</param>
        /// <returns>true if success</returns>
        internal static bool TryRemoveUIWindow(FuWindow window)
        {
            return UIWindows.Remove(window.ID);
        }

        /// <summary>
        /// Whatever a FuWIndowName has at least an instance
        /// </summary>
        /// <param name="windowName">WindowName to check</param>
        /// <returns>True if instancied at least once</returns>
        public static bool IsWindowOpen(FuWindowName windowName)
        {
            foreach (var window in UIWindows)
            {
                if (window.Value.WindowName.Equals(windowName))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get a list of all instances of the given FuWIndowName
        /// </summary>
        /// <param name="windowName">WindowName to get</param>
        /// <returns>list of windows instances</returns>
        public static List<FuWindow> GetWindowInstances(FuWindowName windowName)
        {
            List<FuWindow> windows = new List<FuWindow>();
            foreach (var window in UIWindows)
            {
                if (window.Value.WindowName.Equals(windowName))
                {
                    windows.Add(window.Value);
                }
            }
            return windows;
        }

        /// <summary>
        /// Refresh UI render of all instances of the given FuWIndowName
        /// </summary>
        /// <param name="windowName">WindowName to refresh</param>
        public static void RefreshWindowsInstances(FuWindowName windowName)
        {
            foreach (var window in UIWindows)
            {
                if (window.Value.WindowName.Equals(windowName))
                {
                    window.Value.ForceDraw();
                }
            }
        }

        /// <summary>
        /// Get the number of instances for a given FuWIndowName
        /// </summary>
        /// <param name="windowName">WindowName to check</param>
        /// <returns>number of instancied windows</returns>
        public static int GetNbWindowInstances(FuWindowName windowName)
        {
            int nbWindows = 0;
            foreach (var window in UIWindows)
            {
                if (window.Value.WindowName.Equals(windowName))
                {
                    nbWindows++;
                }
            }
            return nbWindows;
        }

        /// <summary>
        /// Check if any window is currently being dragged
        /// </summary>
        /// <returns> true if any window is being dragged, false otherwise</returns>
        public static bool IsAnyWindowDragging()
        {
            return WindowDraggingCount > 0;
        }

        /// <summary>
        /// Check if any window is currently being resized.
        /// </summary>
        /// <returns> true if any window is being resized, false otherwise</returns>
        public static bool IsAnyWindowResizing()
        {
            return WindowResizingCount > 0;
        }

        /// <summary>
        /// Determines whether any window currently displays hover content.
        /// </summary>
        /// <returns>true if at least one window is displaying hover content; otherwise, false.</returns>
        public static bool IsAnyWindowHoverContent()
        {
            return WindowHoveredContentCount > 0;
        }

        /// <summary>
        /// Determines whether any window is currently hovered.
        /// </summary>
        /// <returns>true if at least one window is hovered; otherwise, false.</returns>
        public static bool IsAnyWindowHovered()
        {
            return WindowHoveredCount > 0;
        }

        /// <summary>
        /// Determines whether any window is currently focused by ImGui.
        /// </summary>
        /// <returns>true if at least one window has focus; otherwise, false.</returns>
        public static bool IsAnyWindowFocused()
        {
            return WindowFocusedCount > 0;
        }

        /// <summary>
        /// Determines whether any window owns an active pointer input.
        /// </summary>
        /// <returns>true if a window has input focus; otherwise, false.</returns>
        public static bool IsAnyWindowInputFocused()
        {
            return FuWindow.InputFocusedWindow != null || FuWindow.NbInputFocusedWindow > 0;
        }

        /// <summary>
        /// Determines whether any window wants keyboard or text input capture.
        /// </summary>
        /// <returns>true if at least one window wants input capture; otherwise, false.</returns>
        public static bool IsAnyWindowWantCaptureInput()
        {
            return WindowWantCaptureInputCount > 0;
        }

        /// <summary>
        /// Check if any overlay of any window is currently being dragged
        /// </summary>
        /// <returns> true if any overlay is being dragged, false otherwise</returns>
        public static bool IsAnyOverlayDragging()
        {
            return OverlayDraggingCount > 0;
        }

        /// <summary>
        /// Check if any payload is currently being dragged in any window or overlay
        /// </summary>
        /// <returns> true if any payload is being dragged, false otherwise</returns>
        public static bool IsDraggingAnyPayload()
        {
            return DraggingPayloadCount > 0;
        }

        /// <summary>
        /// Check if any payload, window or overlay is currently being dragged
        /// </summary>
        /// <returns> true if any payload, window or overlay is being dragged, false otherwise</returns>
        public static bool IsDraggingAnything()
        {
            return IsDraggingAnyPayload() || IsAnyWindowDragging() || IsAnyOverlayDragging();
        }

        /// <summary>
        /// Each open window will be draw next frame
        /// </summary>
        /// <param name="nbFrames">number of frames to force drawing</param>
        public static void ForceDrawAllWindows(int nbFrames = 1)
        {
            foreach (FuWindow window in UIWindows.Values)
            {
                window.ForceDraw(nbFrames);
            }
        }
    }
}
