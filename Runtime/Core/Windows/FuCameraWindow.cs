using ImGuiNET;
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Fu
{
    /// <summary>
    /// Represents the Fu Camera Window type.
    /// </summary>
    public class FuCameraWindow : FuWindow
    {
        #region State
        public float _superSampling = 1.0f;

        public float SuperSampling
        {
            get { return _superSampling; }
            set
            {
                _superSampling = value;
                NeedToUpdateCamera = true;
            }
        }
        public bool NeedToUpdateCamera { get; set; }
        public int TargetCameraFPS
        {
            get
            {
                return (int)(1f / _targetCameraDeltaTimeMs);
            }
            set
            {
                _targetCameraDeltaTimeMs = Fugui.GetDeltaTimeForFPS(Fugui.ApplyGlobalFPSLimit(value));
            }
        }
        public float CameraDeltaTime { get; internal set; }
        public float CurrentCameraFPS { get; internal set; }
        public IntPtr PixelsPtr { get; private set; }
        public Camera Camera { get; private set; }
        public bool AutoCameraFPS { get; set; }
        public int IdleCameraFPS { get; private set; }
        public int ManipulatingCameraFPS { get; private set; }

        private bool _forceCameraRender;
        private float _targetCameraDeltaTimeMs;
        private float _lastCameraRenderTime;
        private RenderTexture _rTexture;
        private FuRaycaster _raycaster;
        private UnityEngine.Experimental.Rendering.GraphicsFormat _currentTextureFormat;
        private int _currentTextureDepth = 24;
        private RenderTexture _previousCameraTargetTexture;
        private Rect _previousCameraPixelRect;
        private bool _previousCameraAllowMSAA;
        private bool _previousCameraAllowDynamicResolution;
        private bool _previousCameraEnabled;
        private bool _cameraStateCaptured;
        private bool _hasRenderedCameraFrame;
        #endregion

        /// <summary>
        /// Initializes a new instance of the Fu Camera Window class.
        /// </summary>
        /// <param name="windowDefinition">The window Definition value.</param>
        public FuCameraWindow(FuCameraWindowDefinition windowDefinition) : base(windowDefinition)
        {
            AutoCameraFPS = true;
            SuperSampling = windowDefinition.SuperSampling;
            Camera = windowDefinition.Camera;
            if (Camera == null)
            {
                throw new ArgumentException("A camera window requires a valid Unity camera.", nameof(windowDefinition));
            }

            IdleCameraFPS = windowDefinition.IdleCameraFPS;
            ManipulatingCameraFPS = windowDefinition.ManipuatingCameraFPS;
            CaptureCameraState();

            try
            {
                // The window temporarily configures the borrowed camera for an explicit offscreen render target.
                Camera.allowMSAA = windowDefinition.MSAASamples != MSAASamples.None;
                Camera.allowDynamicResolution = false;
                _currentTextureFormat = Camera.allowMSAA
                    ? UnityEngine.Experimental.Rendering.GraphicsFormat.B10G11R11_UFloatPack32
                    : UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat;
                _currentTextureDepth = Camera.allowMSAA ? 24 : 0;

                // Create the render target without mutating the project-wide URP asset.
                _rTexture = new RenderTexture(Mathf.Max(Size.x, 1), Mathf.Max(Size.y, 1), _currentTextureDepth, _currentTextureFormat);

                _rTexture.antiAliasing = (int)windowDefinition.MSAASamples;
#if UNITY_6000_4_OR_NEWER
                _rTexture.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D16_UNorm;
#else
                bool isRenderGraphEnabled = !GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>()?.enableRenderCompatibilityMode ?? false;
                if (isRenderGraphEnabled)
                    _rTexture.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D16_UNorm;
                else
                    _rTexture.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.None;
#endif
                _rTexture.useDynamicScale = false;
                if (!_rTexture.Create())
                {
                    throw new InvalidOperationException($"Unable to create the render texture for camera window '{ID}'.");
                }

                Camera.targetTexture = _rTexture;
                OnResize += ImGuiCameraWindow_OnResize;
                OnResized += ImGuiCameraWindow_OnResize;
                OnDock += ImGuiCameraWindow_OnDock;
                OnUnDock += ImGuiCameraWindow_OnDock;
                ImGuiCameraWindow_OnResize(this);
                _windowFlags |= FuWindowStyleFlags.NoScrollbar;
                _windowFlags |= FuWindowStyleFlags.NoScrollWithMouse;
                NeedToUpdateCamera = true;
                _lastCameraRenderTime = 0f;
                IsInterractable = true;
                Camera.enabled = false;

                UI = (window) =>
                {
                    Vector2 cursorPos = ImGui.GetCursorScreenPos();
                    ImGui.Image(Container.Context.TextureManager.GetTextureId(_rTexture), WorkingAreaSize);
                    ImGui.SetCursorScreenPos(cursorPos);
                    windowDefinition.UI?.Invoke(this);
                };

                // Register camera input only after every GPU resource has initialized successfully.
                _raycaster = new FuRaycaster(ID, GetCameraRay,
                    () => Container != null && !InputsLocked && Container.Mouse.IsPressed(FuMouseButton.Left),
                    () => Container != null && !InputsLocked && Container.Mouse.IsPressed(FuMouseButton.Right),
                    () => false,
                    () => Container == null || InputsLocked ? 0f : Container.Mouse.Wheel.y,
                    () => Container != null && !InputsLocked && LocalRect.Contains(Container.Mouse.Position));
                FuRaycasting.RegisterRaycaster(_raycaster);
            }
            catch
            {
                // A partially constructed window must not retain the camera lease or native render target.
                ReleaseOwnedResources();
                throw;
            }
        }

        #region Methods
        /// <summary>
        /// Captures the state of the borrowed Unity camera before the window configures it.
        /// </summary>
        private void CaptureCameraState()
        {
            _previousCameraTargetTexture = Camera.targetTexture;
            _previousCameraPixelRect = Camera.pixelRect;
            _previousCameraAllowMSAA = Camera.allowMSAA;
            _previousCameraAllowDynamicResolution = Camera.allowDynamicResolution;
            _previousCameraEnabled = Camera.enabled;
            _cameraStateCaptured = true;
        }

        /// <summary>
        /// Restores the borrowed Unity camera to the state it had before this window acquired it.
        /// </summary>
        private void RestoreCameraState()
        {
            if (!_cameraStateCaptured)
            {
                return;
            }

            try
            {
                if (Camera != null)
                {
                    // Preserve a target assigned externally while the window was alive.
                    if (ReferenceEquals(Camera.targetTexture, _rTexture))
                    {
                        Camera.targetTexture = _previousCameraTargetTexture;
                    }

                    Camera.pixelRect = _previousCameraPixelRect;
                    Camera.allowMSAA = _previousCameraAllowMSAA;
                    Camera.allowDynamicResolution = _previousCameraAllowDynamicResolution;
                    Camera.enabled = _previousCameraEnabled;
                }
            }
            finally
            {
                _cameraStateCaptured = false;
                _previousCameraTargetTexture = null;
            }
        }

        /// <summary>
        /// Releases the raycaster and render target owned by this camera window.
        /// </summary>
        protected override void ReleaseOwnedResources()
        {
            // Detach callbacks before releasing the camera target they can resize.
            FuRaycasting.UnRegisterRaycaster(ID);
            OnResize -= ImGuiCameraWindow_OnResize;
            OnResized -= ImGuiCameraWindow_OnResize;
            OnDock -= ImGuiCameraWindow_OnDock;
            OnUnDock -= ImGuiCameraWindow_OnDock;
            _raycaster = null;

            RestoreCameraState();

            if (_rTexture != null)
            {
                RenderTexture ownedTexture = _rTexture;
                _rTexture = null;
                try
                {
                    ownedTexture.Release();
                }
                finally
                {
                    // Unity object ownership ends even if releasing the native render surface reports an error.
                    DestroyOwnedRenderTexture(ownedTexture);
                }
            }

            UI = null;
            base.ReleaseOwnedResources();
        }

        /// <summary>
        /// Destroys a render texture owned by this window in play mode or Edit Mode.
        /// </summary>
        /// <param name="renderTexture">Render texture to destroy.</param>
        private static void DestroyOwnedRenderTexture(RenderTexture renderTexture)
        {
            if (renderTexture == null)
            {
                return;
            }

            // Edit-mode windows need immediate destruction because no later frame flush is guaranteed.
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(renderTexture);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        /// <summary>
        /// draw debug panel
        /// </summary>
        internal override void DrawDebugPanel()
        {
            base.DrawDebugPanel();

            if (!Fugui.Settings.DrawDebugPanel || !DebugPanelExpanded)
            {
                return;
            }

            Vector2 previousCursorPos = ImGui.GetCursorScreenPos();
            Vector2 panelSize = GetDebugPanelSize(278f, 126f);
            Vector2 basePanelSize = GetDebugPanelSize(DebugPanelWidth, DebugPanelHeight);
            float scale = Container?.Context?.Scale ?? Fugui.Scale;
            float yOffset = 0f;
            if (ImGui.GetWindowSize().x < basePanelSize.x + panelSize.x + DebugPanelMargin * scale * 3f)
            {
                yOffset = basePanelSize.y + DebugPanelMargin * scale;
            }

            ImGui.SetCursorScreenPos(GetDebugPanelPosition(panelSize, true, yOffset));

            FuImGuiStackSnapshot stackSnapshot = Fugui.CaptureImGuiStackSnapshot();
            try
            {
                Fugui.Push(ImGuiStyleVar.ChildRounding, 5f * Fugui.Scale);
                Fugui.Push(ImGuiStyleVar.ChildBorderSize, 1f);
                Fugui.Push(ImGuiStyleVar.WindowPadding, new Vector2(8f, 6f) * Fugui.Scale);
                Fugui.Push(ImGuiCol.ChildBg, new Vector4(.055f, .065f, .085f, .88f));
                Fugui.Push(ImGuiCol.Border, new Vector4(.2f, .55f, 1f, .55f));

                bool childBegan = false;
                try
                {
                    bool childVisible = ImGui.BeginChild(ID + "cs", panelSize, ImGuiChildFlags.Borders | ImGuiChildFlags.AlwaysUseWindowPadding, ImGuiWindowFlags.NoSavedSettings);
                    childBegan = true;
                    if (childVisible)
                    {
                        ImGui.Text("Camera debug");
                        ImGui.Separator();
                        // Super sampling controls update the render target on the next window frame.
                        if (ImGui.RadioButton("x0.5", _superSampling == 0.5f))
                        {
                            SuperSampling = 0.5f;
                        }
                        ImGui.SameLine();
                        if (ImGui.RadioButton("x1", _superSampling == 1f))
                        {
                            SuperSampling = 1f;
                        }
                        ImGui.SameLine();
                        if (ImGui.RadioButton("x1.5", _superSampling == 1.5f))
                        {
                            SuperSampling = 1.5f;
                        }
                        ImGui.SameLine();
                        if (ImGui.RadioButton("x2", _superSampling == 2f))
                        {
                            SuperSampling = 2f;
                        }

                        DrawDebugLine("State", State.ToString());
                        DrawDebugLine("FPS", (int)CurrentCameraFPS + " (" + (CameraDeltaTime * 1000f).ToString("f2") + " ms)");
                        DrawDebugLine("Target", TargetCameraFPS + " (" + ((int)(_targetCameraDeltaTimeMs * 1000)).ToString() + " ms)");
                    }
                }
                finally
                {
                    if (childBegan)
                    {
                        Fugui.EndRawChild();
                    }
                }
            }
            finally
            {
                try
                {
                    Fugui.RestoreImGuiStackSnapshot(stackSnapshot);
                }
                finally
                {
                    ImGui.SetCursorScreenPos(previousCursorPos);
                }
            }
        }

        /// <summary>
        /// resize camera on next frame when window dock state change
        /// </summary>
        /// <param name="window">related UIWindow</param>
        private void ImGuiCameraWindow_OnDock(FuWindow window)
        {
            NeedToUpdateCamera = true;
            ForceRenderCamera();
        }

        /// <summary>
        /// resize camera on next frame when window resize
        /// </summary>
        /// <param name="window">related UIWindow</param>
        private void ImGuiCameraWindow_OnResize(FuWindow window)
        {
            NeedToUpdateCamera = true;
            ForceRenderCamera();
        }

        /// <summary>
        /// update camera and render texture size
        /// </summary>
        private void updateCameraSize()
        {
            if (!NeedToUpdateCamera)
            {
                return;
            }

            // Keep the resize request pending while the window has no drawable working area.
            if (Camera == null || _rTexture == null ||
                WorkingAreaSize.x <= 10 || WorkingAreaSize.y <= 10 || _superSampling <= 0.1f)
            {
                return;
            }

            int maxTextureSize = Mathf.Max(1, SystemInfo.maxTextureSize);
            int targetWidth = Mathf.Clamp(Mathf.RoundToInt(WorkingAreaSize.x * _superSampling), 1, maxTextureSize);
            int targetHeight = Mathf.Clamp(Mathf.RoundToInt(WorkingAreaSize.y * _superSampling), 1, maxTextureSize);
            bool textureSizeChanged = _rTexture.width != targetWidth || _rTexture.height != targetHeight || !_rTexture.IsCreated();
            if (textureSizeChanged)
            {
                int previousWidth = _rTexture.width;
                int previousHeight = _rTexture.height;
                _rTexture.Release();
                _rTexture.width = targetWidth;
                _rTexture.height = targetHeight;
                if (!_rTexture.Create())
                {
                    // Restore the previous allocation so a failed resize does not blank a working camera window.
                    _rTexture.width = previousWidth;
                    _rTexture.height = previousHeight;
                    _rTexture.Create();
                    Debug.LogError($"Unable to resize camera window '{ID}' render texture to {targetWidth}x{targetHeight}.");
                    return;
                }
            }

            Camera.targetTexture = _rTexture;
            // Match camera projection helpers to the actual render target size.
            Camera.pixelRect = new Rect(0, 0, targetWidth, targetHeight);

            NeedToUpdateCamera = false;
            ForceRenderCamera();
        }

        /// <summary>
        /// check whatever camera must be enabled or disabled to reach target camera FPS
        /// </summary>
        private void updateCameraRender()
        {
            if (Camera == null || _rTexture == null || !_rTexture.IsCreated())
            {
                return;
            }

            // Render manually only when the camera cadence elapsed or an explicit refresh was requested.
            if ((Fugui.Time > _lastCameraRenderTime + _targetCameraDeltaTimeMs) || _forceCameraRender)
            {
                Camera.Render();
                if (_hasRenderedCameraFrame)
                {
                    CameraDeltaTime = Mathf.Max(0f, Fugui.Time - _lastCameraRenderTime);
                    CurrentCameraFPS = CameraDeltaTime > Mathf.Epsilon ? 1f / CameraDeltaTime : 0f;
                }
                else
                {
                    CameraDeltaTime = 0f;
                    CurrentCameraFPS = 0f;
                    _hasRenderedCameraFrame = true;
                }

                _lastCameraRenderTime = Fugui.Time;
            }
            _forceCameraRender = false;
        }

        /// <summary>
        /// Refresh this window target FPS.
        /// </summary>
        internal override void RefreshPerformanceFPS()
        {
            base.RefreshPerformanceFPS();
            if (AutoCameraFPS)
            {
                switch (State)
                {
                    default:
                    case FuWindowState.Idle:
                        TargetCameraFPS = Fugui.ApplyGlobalFPSLimit(IdleCameraFPS);
                        break;

                    case FuWindowState.Manipulating:
                        TargetCameraFPS = Fugui.ApplyGlobalFPSLimit(ManipulatingCameraFPS);
                        break;
                }
            }
        }

        /// <summary>
        /// draw the window and do some camera related process
        /// </summary>
        public override void DrawWindow(bool preventUpdatingMouse = false, bool preventUpdatingKeyboard = false)
        {
            base.DrawWindow(preventUpdatingMouse, preventUpdatingKeyboard);
            updateCameraSize();
            updateCameraRender();
        }

        /// <summary>
        /// force camera to render next frame
        /// </summary>
        public void ForceRenderCamera()
        {
            _forceCameraRender = true;
        }

        /// <summary>
        /// Get a ray for this camera according to current window mouse position
        /// </summary>
        /// <returns>Ray from this camera</returns>
        public Ray GetCameraRay()
        {
            if (WorkingAreaSize.x == 0 || WorkingAreaSize.y == 0)
            {
                return default;
            }
            float normH = (float)WorkingAreaMousePosition.x / (float)WorkingAreaSize.x;
            float normV = 1f - ((float)WorkingAreaMousePosition.y / (float)WorkingAreaSize.y);
            return Camera.ViewportPointToRay(new Vector3(normH, normV, 0f), Camera.MonoOrStereoscopicEye.Mono);
        }
        #endregion
    }
}
