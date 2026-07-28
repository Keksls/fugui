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
        /// Initializes Fugui and creates the main container for the owning controller.
        /// </summary>
        /// <param name="settings">Runtime settings used by every Fugui context.</param>
        /// <param name="controller">Controller that owns the runtime session.</param>
        /// <param name="mainContainerUICamera">Camera that will display UI of main container</param>
        /// <param name="enableMainContainer">Whether the fullscreen main container is enabled.</param>
        /// <returns>True when this controller owns the initialized session.</returns>
        public static bool Initialize(FuSettings settings, FuController controller, Camera mainContainerUICamera, bool enableMainContainer = true)
        {
            // Validate the complete dependency set before mutating any global runtime state.
            if (settings == null || controller == null || mainContainerUICamera == null)
            {
                Debug.LogError("[Fugui] Initialization requires settings, an owning FuController and a main UI camera.");
                return false;
            }

            if (_lifecycleState == FuguiLifecycleState.Initialized)
            {
                if (ReferenceEquals(Controller, controller))
                {
                    return true;
                }

                string ownerName = Controller != null ? Controller.name : "<destroyed controller>";
                Debug.LogError($"[Fugui] Controller '{controller.name}' cannot initialize Fugui because controller '{ownerName}' already owns the active session.");
                return false;
            }

            if (_lifecycleState != FuguiLifecycleState.Inactive)
            {
                Debug.LogError($"[Fugui] Initialization is not allowed while the runtime is {_lifecycleState}.");
                return false;
            }

            _lifecycleState = FuguiLifecycleState.Initializing;
            RuntimeGeneration++;
            ResetRuntimeState();
            Settings = settings;
            Controller = controller;
            _mainContainerEnabled = enableMainContainer;

            try
            {
                // Build the complete session transactionally so failures return Fugui to the inactive state.
                Themes = new FuThemeManager();
                Layouts = new FuDockingLayoutManager();
                ImGuiAssertHandler.Initialize();
                ResetContextMenu(false);

                DefaultContext = CreateUnityContext(mainContainerUICamera, Settings.GlobalScale, Settings.FontGlobalScale, Themes.Initialize);
                if (DefaultContext == null)
                {
                    throw new InvalidOperationException("The default Fugui context could not be created.");
                }

                DefaultContainer = new FuMainWindowContainer(DefaultContext);
                DefaultContainer.SetContainerScaleConfig(GetDefaultContainerScaleConfig());
                ApplyMainContainerCameraState();

                new FuWindowDefinition(
                    FuSystemWindowsNames.FuguiSettings,
                    FuLayer.Normal,
                    DrawSettings,
                    size: new Vector2Int(256, 256),
                    flags: FuWindowFlags.Default | FuWindowFlags.AllowMultipleWindow);

                _lifecycleState = FuguiLifecycleState.Initialized;

#if FUDEBUG
                initDebugTool();
#endif
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                DisposeRuntimeState();
                return false;
            }
        }

        /// <summary>
        /// Update Fugui Windows Data (Externalizations and add/remove)
        /// Need to be called into MainThread (Update / Late Update / Coroutine)
        /// </summary>
        public static void Update()
        {
            if (!IsInitialized)
            {
                return;
            }

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
        /// Disposes the active Fugui runtime session.
        /// </summary>
        public static void Dispose()
        {
            DisposeRuntimeState();
        }

        /// <summary>
        /// Disposes the runtime session only when the specified controller owns it.
        /// </summary>
        /// <param name="controller">Controller requesting disposal.</param>
        /// <returns>True when the session was disposed or was already inactive.</returns>
        internal static bool Dispose(FuController controller)
        {
            if (_lifecycleState == FuguiLifecycleState.Inactive)
            {
                return true;
            }

            if (!ReferenceEquals(Controller, controller))
            {
                return false;
            }

            DisposeRuntimeState();
            return true;
        }

        /// <summary>
        /// Returns whether the specified controller owns the active Fugui session.
        /// </summary>
        /// <param name="controller">Controller to check.</param>
        /// <returns>True when the controller owns the initialized session.</returns>
        internal static bool IsOwnedBy(FuController controller)
        {
            return IsInitialized && ReferenceEquals(Controller, controller);
        }

        /// <summary>
        /// Shuts down every runtime resource and returns Fugui to its inactive state.
        /// </summary>
        private static void DisposeRuntimeState()
        {
            if (_lifecycleState == FuguiLifecycleState.Inactive ||
                _lifecycleState == FuguiLifecycleState.Disposing)
            {
                return;
            }

            _lifecycleState = FuguiLifecycleState.Disposing;

            try
            {
                // Release higher-level containers before destroying the native contexts they depend on.
                RunShutdownStep(() => ResetContextMenu(true));
                RunShutdownStep(() => RestoreMainContainerCameraState(DefaultContext != null ? DefaultContext.Camera : null));
                RunShutdownStep(ReleaseOffscreenDriverTexture);
                RunShutdownStep(FuLayout.DisposeVideoPlayers);
                RunShutdownStep(FuLayout.ResetGradientEditorState);
                RunShutdownStep(ImGuiDrawListUtils.ShutdownSessionResources);
                RunShutdownStep(DisposeWindowResources);
                RunShutdownStep(Close3DWindowContainers);
                RunShutdownStep(Fu3DWindowContainer.ShutdownSharedResources);
                RunShutdownStep(DisposeListClippers);
                RunShutdownStep(Fugui.World.ShutdownSessionResources);
                RunShutdownStep(DestroyAllContextsImmediately);
                RunShutdownStep(TextureManager.ShutdownSharedResources);
                RunShutdownStep(FuSharedFontAtlasCache.Shutdown);
                RunShutdownStep(() => SetCurrentContext(null));

#if FU_EXTERNALIZATION
                RunShutdownStep(SDL.SDL_Quit);
                RunShutdownStep(() => RestoreExternalWindowUpdateLoop(true));
#endif
            }
            finally
            {
                ResetRuntimeState();
                Settings = null;
                Themes = null;
                Layouts = null;
                Controller = null;
                _lifecycleState = FuguiLifecycleState.Inactive;
            }
        }

        /// <summary>
        /// Executes one shutdown step while allowing the remaining resources to be released if it fails.
        /// </summary>
        /// <param name="shutdownStep">Shutdown operation to execute.</param>
        private static void RunShutdownStep(Action shutdownStep)
        {
            try
            {
                shutdownStep?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        /// <summary>
        /// Closes every registered 3D window container before its context is destroyed.
        /// </summary>
        private static void Close3DWindowContainers()
        {
            if (_3DWindows == null || _3DWindows.Count == 0)
            {
                return;
            }

            List<Fu3DWindowContainer> containers = _3DWindows.Values
                .Where(container => container != null)
                .Distinct()
                .ToList();

            foreach (Fu3DWindowContainer container in containers)
            {
                RunShutdownStep(container.Close);
            }

            _3DWindows.Clear();
        }

        /// <summary>
        /// Releases draw-data and specialized GPU resources owned by every registered window.
        /// </summary>
        private static void DisposeWindowResources()
        {
            if (UIWindows == null || UIWindows.Count == 0)
            {
                return;
            }

            // A window can have multiple registry aliases, but its resources must be disposed exactly once.
            List<FuWindow> windows = UIWindows.Values
                .Where(window => window != null)
                .Distinct()
                .ToList();

            foreach (FuWindow window in windows)
            {
                RunShutdownStep(window.DisposeRuntimeResources);
            }
        }

        /// <summary>
        /// Resets all session-owned managed state without retaining references to the previous session.
        /// </summary>
        private static void ResetRuntimeState()
        {
            // Runtime registries never survive across controller ownership sessions.
            Contexts.Clear();
            ToDeleteContexts.Clear();
            UIWindows = new Dictionary<string, FuWindow>();
            UIWindowsDefinitions = new Dictionary<FuWindowName, FuWindowDefinition>();
            _3DWindows = new Dictionary<string, Fu3DWindowContainer>();
#if FU_EXTERNALIZATION
            ExternalWindows.Clear();
            AbsoluteMonitorMousePosition = Vector2Int.zero;
#endif
            DefaultContext = null;
            DefaultContainer = null;
            CurrentContext = null;
            _contextID = 0;
            Time = 0f;

            _beforeDefaultRenderStack.Clear();
            _afterDefaultRenderStack.Clear();
            _afterCurrentRenderContextStack.Clear();
            _executeInMainThreadActionsStack.Clear();

            // Per-frame drawing and interaction stacks must restart empty.
            PopUpWindowsIDs.Clear();
            PopUpIDs.Clear();
            PopUpRects.Clear();
            IsPopupDrawing.Clear();
            IsPopupFocused.Clear();
            _currentSurfacesByContext.Clear();
            _lastSurfacesByContext.Clear();
            _surfaceOrderByContext.Clear();
            _modalSurfaceInputStack.Clear();

            ClearFrozenUICache();
            _backdropStack.Clear();
            _cursorPositionStack.Clear();
            FuLayout.CurrentDrawerPath.Clear();
            _mainMenuItems.Clear();
            IsMainMenuDisabled = false;
            IsMainMenuVisible = true;
            IsContextMenuDisabled = false;
            IsContextMenuOpen = false;
            _openThisFrameLevel = -1;
            _currentOpenContextID = -1;
            ResetContextMenu(false);

            ResetInputOwnershipCounters();
            ResetWindowInputState();
            FuWindow.InputFocusedWindow = null;
            FuWindow.NbInputFocusedWindow = 0;
            HasRenderWindowThisFrame = false;
            HasHovered3DWindowThisFrame = false;
            IsCursorLocked = false;
            CursorsJustUnlocked = false;
            NbPushColor = 0;
            NbPushStyle = 0;
            NbPushFont = 0;
            _colorStackCount = 0;
            _colorStackHeads = Array.Empty<int>();
            Array.Clear(_colorStack, 0, _colorStack.Length);

            // Camera and scale snapshots belong exclusively to the disposed session.
            _mainContainerEnabled = true;
            _mainContainerCameraStateStored = false;
            _mainContainerCameraHadAdditionalCameraData = false;
            _mainContainerCameraAllowXRRendering = false;
            _mainContainerCameraCullingMask = 0;
            _mainContainerCameraClearFlags = default;
            _mainContainerCameraBackgroundColor = default;
            _mainContainerCameraTargetTexture = null;
            _offscreenDriverTexture = null;
            _targetScale = -1f;
            _targetFontScale = -1f;
        }

        /// <summary>
        /// Clears all per-frame input ownership snapshots retained by the previous session.
        /// </summary>
        private static void ResetWindowInputState()
        {
            WindowInputsBlockedThisFrame = false;
            _windowInputSnapshotCaptured = false;
            Array.Clear(_blockedFrameRawMouseDown, 0, _blockedFrameRawMouseDown.Length);
            Array.Clear(_blockedFrameRawMousePressed, 0, _blockedFrameRawMousePressed.Length);
            Array.Clear(_blockedInputHeldFromOutside, 0, _blockedInputHeldFromOutside.Length);
            Array.Clear(_blockedInputDownEmitted, 0, _blockedInputDownEmitted.Length);
            Array.Clear(_inputSnapshotMouseDown, 0, _inputSnapshotMouseDown.Length);
            Array.Clear(_inputSnapshotMouseClicked, 0, _inputSnapshotMouseClicked.Length);
            Array.Clear(_inputSnapshotMouseReleased, 0, _inputSnapshotMouseReleased.Length);
            Array.Clear(_inputSnapshotMouseDoubleClicked, 0, _inputSnapshotMouseDoubleClicked.Length);
            Array.Clear(_inputSnapshotMouseDownOwned, 0, _inputSnapshotMouseDownOwned.Length);
            Array.Clear(_inputSnapshotMouseDownOwnedUnlessPopupClose, 0, _inputSnapshotMouseDownOwnedUnlessPopupClose.Length);
            Array.Clear(_inputSnapshotMouseClickedCount, 0, _inputSnapshotMouseClickedCount.Length);
            Array.Clear(_inputSnapshotMouseClickedLastCount, 0, _inputSnapshotMouseClickedLastCount.Length);
            Array.Clear(_inputSnapshotMouseDownDuration, 0, _inputSnapshotMouseDownDuration.Length);
            Array.Clear(_inputSnapshotMouseDownDurationPrev, 0, _inputSnapshotMouseDownDurationPrev.Length);
            Array.Clear(_inputSnapshotMouseDragMaxDistanceAbs, 0, _inputSnapshotMouseDragMaxDistanceAbs.Length);
            Array.Clear(_inputSnapshotMouseDragMaxDistanceSqr, 0, _inputSnapshotMouseDragMaxDistanceSqr.Length);
        }
    }
}
