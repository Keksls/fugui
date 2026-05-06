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
    /// Fugui native external window support.
    /// </summary>
    public static partial class Fugui
    {
#if FU_EXTERNALIZATION
        /// <summary>
        /// Keep Unity updating while SDL external windows have focus.
        /// </summary>
        internal static void EnsureExternalWindowUpdateLoop()
        {
            if (_hasExternalWindowUpdateLoopOverride)
            {
                return;
            }

            _runInBackgroundBeforeExternalWindows = Application.runInBackground;
            _hasExternalWindowUpdateLoopOverride = true;
            Application.runInBackground = true;
        }

        /// <summary>
        /// Restore the user's run-in-background setting once no external window is alive.
        /// </summary>
        internal static void RestoreExternalWindowUpdateLoop(bool force = false)
        {
            if (!_hasExternalWindowUpdateLoopOverride || (!force && ExternalWindows.Count > 0))
            {
                return;
            }

            Application.runInBackground = _runInBackgroundBeforeExternalWindows;
            _hasExternalWindowUpdateLoopOverride = false;
        }

        /// <summary>
        /// Read the physical mouse button state from SDL, even if Unity did not receive the original mouse down.
        /// </summary>
        internal static bool IsGlobalMouseButtonPressed(FuMouseButton button)
        {
            if (!EnsureSDLVideo())
            {
                return false;
            }

            uint sdlButton = button switch
            {
                FuMouseButton.Left => SDL.SDL_BUTTON_LEFT,
                FuMouseButton.Right => SDL.SDL_BUTTON_RIGHT,
                FuMouseButton.Center => SDL.SDL_BUTTON_MIDDLE,
                _ => 0
            };

            if (sdlButton == 0)
            {
                return false;
            }

            uint state = SDL.SDL_GetGlobalMouseState(out _, out _);
            return (state & SDL.SDL_BUTTON(sdlButton)) != 0;
        }

        /// <summary>
        /// Read and cache the current absolute mouse position from SDL.
        /// </summary>
        internal static Vector2Int GetGlobalMousePosition()
        {
            if (!EnsureSDLVideo())
            {
                return AbsoluteMonitorMousePosition;
            }

            SDL.SDL_GetGlobalMouseState(out int x, out int y);
            AbsoluteMonitorMousePosition = new Vector2Int(x, y);
            return AbsoluteMonitorMousePosition;
        }

        /// <summary>
        /// Ensure SDL video is ready before querying global mouse or creating native windows.
        /// </summary>
        internal static bool EnsureSDLVideo()
        {
            if ((SDL.SDL_WasInit(SDL.SDL_INIT_VIDEO) & SDL.SDL_INIT_VIDEO) != 0)
            {
                return true;
            }

            if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) < 0)
            {
                Debug.LogError("SDL init failed: " + SDL.SDL_GetError());
                return false;
            }

            return true;
        }
#endif

        /// <summary>
        /// Externalize a Fugui window into a native external window.
        /// </summary>
        /// <param name="uiWindow">The Fugui window to externalize.</param>
        public static void ExternalizeWindow(FuWindow uiWindow)
        {
#if FU_EXTERNALIZATION
            if (uiWindow == null)
            {
                Debug.LogError("Cannot create an external window from a null Fugui window.");
                return;
            }

            if (!uiWindow.IsExternalizable)
            {
                Debug.LogError($"Cannot externalize window {uiWindow.ID} because it is not externalizable.");
                return;
            }

            if (uiWindow.IsDocked)
            {
                Debug.LogWarning($"Cannot externalize window {uiWindow.ID} because docked windows stay inside the main Fugui container.");
                return;
            }

            if(!Settings.EnableExternalizations)
            {
                Debug.LogWarning("Externalizations are disabled in the settings.");
                return;
            }

            FuExternalWindowContainer sourceExternalContainer = uiWindow.Container as FuExternalWindowContainer;
            bool detachFromExternalContainer = uiWindow.IsExternal && sourceExternalContainer != null;
            if (ExternalWindows.ContainsKey(uiWindow.ID) && !detachFromExternalContainer)
            {
                Debug.LogWarning($"External window for {uiWindow.ID} already exists.");
                return;
            }

            EnsureExternalWindowUpdateLoop();
            if (detachFromExternalContainer && sourceExternalContainer.Windows.Count <= 1)
            {
                return;
            }

            // 1) Create the external Fugui context
            FuExternalContext context = new FuExternalContext(_contextID++, Settings.GlobalScale, Settings.FontGlobalScale, null, uiWindow);
            Contexts.Add(context.ID, context);

            // 2) Create the external container bound to this context
            var container = new FuExternalWindowContainer(uiWindow, context);
            container.SetContainerScaleConfig(GetDefaultContainerScaleConfig());

            // 3) Register the window to this container
            ExternalWindows[uiWindow.ID] = container;
#else
            Debug.LogWarning("You are trying to externalize a window but externalizations are disabled in the settings.\n" +
                "Add FU_EXTERNALIZATION define to your build settings to enable externalizations.");
#endif
        }

        /// <summary>
        /// Runs the internalize window workflow.
        /// </summary>
        /// <param name="uiWindow">The ui Window value.</param>
        public static void InternalizeWindow(FuWindow uiWindow)
        {
#if FU_EXTERNALIZATION
            if (uiWindow == null)
            {
                Debug.LogError("Cannot internalize a null Fugui window.");
                return;
            }
            if (!ExternalWindows.ContainsKey(uiWindow.ID))
            {
                Debug.LogWarning($"No external window found for {uiWindow.ID}.");
                return;
            }
            if(uiWindow.Container is FuExternalWindowContainer externalContainer)
            {
                FuExternalContext externalContext = (FuExternalContext)externalContainer.Context;
                Vector2Int mousePosition = GetGlobalMousePosition();
                bool resumeDrag = externalContext.Window.IsDragging && IsGlobalMouseButtonPressed(FuMouseButton.Left);
                Vector2Int dragMouseOffset = mousePosition - externalContext.Window.Position;
                List<FuWindow> windowsToInternalize = externalContainer.Windows.Values.ToList();
                if (windowsToInternalize.Count == 0)
                {
                    windowsToInternalize.Add(uiWindow);
                }

                externalContext.Window.Close(() => {
                    foreach (FuWindow window in windowsToInternalize)
                    {
                        if (window == null)
                        {
                            continue;
                        }

                        if (resumeDrag && window == uiWindow)
                        {
                            window.RequestInternalizedDragResume(dragMouseOffset);
                        }

                        window.TryAddToContainer(DefaultContainer);
                    }
                });
            }
#else
            Debug.LogWarning("You are trying to internalize a window but externalizations are disabled in the settings.\n" +
                "Add FU_EXTERNALIZATION define to your build settings to enable externalizations.");
#endif
        }

        /// <summary>
        /// Remove an externalized window by instance.
        /// </summary>
        internal static void RemoveExternalWindow(FuWindow uiWindow)
        {
            if (uiWindow == null || uiWindow.Container == null) return;
            RemoveExternalWindow(uiWindow, uiWindow.Container.Context.ID);
        }

        /// <summary>
        /// Remove an externalized window by its owning external context.
        /// </summary>
        internal static void RemoveExternalWindow(FuWindow uiWindow, int contextID)
        {
            if (uiWindow == null) return;
            if (FuWindow.InputFocusedWindow == uiWindow)
            {
                FuWindow.InputFocusedWindow = null;
                FuWindow.NbInputFocusedWindow = 0;
            }
            DestroyContext(contextID);
        }
    }
}
