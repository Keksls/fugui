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
    /// Fugui initialization and update lifecycle.
    /// </summary>
    public static partial class Fugui
    {
        /// <summary>
        /// Initialize FuGui and create Main Container
        /// </summary>
        /// <param name="mainContainerUICamera">Camera that will display UI of main container</param>
        public static void Initialize(FuSettings settings, FuController controller, Camera mainContainerUICamera, bool enableMainContainer = true)
        {
            Settings = settings;
            Controller = controller;
            MainContainerEnabled = enableMainContainer;
            // instantiate UIWindows 
            UIWindows = new Dictionary<string, FuWindow>();
            UIWindowsDefinitions = new Dictionary<FuWindowName, FuWindowDefinition>();
            ResetInputOwnershipCounters();
            // init dic and queue
            _3DWindows = new Dictionary<string, Fu3DWindowContainer>();
            Themes = new FuThemeManager();
            Layouts = new FuDockingLayoutManager();
            // handle native ImGui assert handler
            ImGuiAssertHandler.Initialize();
            // prepare context menu
            ResetContextMenu(true);

            // prevnt null ref
            if (settings == null || controller == null || mainContainerUICamera == null)
                return;

            // create Default Fugui Context and initialize themeManager
            DefaultContext = CreateUnityContext(mainContainerUICamera, Settings.GlobalScale, Settings.FontGlobalScale, Fugui.Themes.Initialize);

            // need to be called into start, because it will use ImGui context and we need to wait to create it from UImGui Awake
            DefaultContainer = new FuMainWindowContainer(DefaultContext);
            DefaultContainer.SetContainerScaleConfig(GetDefaultContainerScaleConfig());
            ApplyMainContainerCameraState();

            // register Fugui Settings Window
            new FuWindowDefinition(FuSystemWindowsNames.FuguiSettings, FuLayer.Normal, DrawSettings, size: new Vector2Int(256, 256), flags: FuWindowFlags.Default | FuWindowFlags.AllowMultipleWindow);

#if FUDEBUG
            // initialize debug tool if debug is enabled
            initDebugTool();
#endif
        }

        /// <summary>
        /// Update Fugui Windows Data (Externalizations and add/remove)
        /// Need to be called into MainThread (Update / Late Update / Coroutine)
        /// </summary>
        public static void Update()
        {
#if FUDEBUG
            // prepare debug new frame
            newFrame();
#endif
            // set shared time
            Time = UnityEngine.Time.unscaledTime;

            // Execute only the actions that were queued before this update.
            // Actions queued by callbacks must wait for the next Unity tick, otherwise
            // retry loops such as deferred layout changes can spin forever in one frame.
            int mainThreadActionsCount = _executeInMainThreadActionsStack.Count;
            for (int i = 0; i < mainThreadActionsCount; i++)
            {
                _executeInMainThreadActionsStack.Dequeue()?.Invoke();
            }
        }

        /// <summary>
        /// Disposes the external windows and stops the render thread.
        /// </summary>
        public static void Dispose()
        {
            RestoreMainContainerCameraState(DefaultContext != null ? DefaultContext.Camera : null);
            ReleaseOffscreenDriverTexture();

#if FU_EXTERNALIZATION
            // close all external windows
            var externalWindowIDs = ExternalWindows.Keys.ToList();
            foreach (string windowID in externalWindowIDs)
            {
                ((FuExternalContext)ExternalWindows[windowID].Context).Window.Close(null);
            }
#endif
            // Dispose Fugui Contexts
            var ids = Contexts.Keys.ToList();
            foreach (int contextID in ids)
            {
                DestroyContext(contextID);
            }

#if FU_EXTERNALIZATION
            SDL.SDL_Quit();
            RestoreExternalWindowUpdateLoop(true);
#endif
        }
    }
}
