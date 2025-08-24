using ImGuiNET;
using System;
using UnityEngine;
using UnityEngine.Rendering;
#if HAS_URP
using UnityEngine.Rendering.Universal;
#elif HAS_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

namespace Fu
{
    public class FuCameraWindow : FuWindow
    {
        #region Variables
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
                _targetCameraDeltaTimeMs = 1f / value;
            }
        }
        public float CameraDeltaTime { get; internal set; }
        public float CurrentCameraFPS { get; internal set; }
        public IntPtr PixelsPtr { get; private set; }
        public Camera Camera { get; private set; }
        public bool AutoCameraFPS { get; set; }
        private bool _forceCameraRender;
        private float _targetCameraDeltaTimeMs;
        private float _lastCameraRenderTime;
        private RenderTexture _rTexture;
        private FuRaycaster _raycaster;
        private UnityEngine.Experimental.Rendering.GraphicsFormat _currentTextureFormat;
        private int _currentTextureDepth = 24;
        private UniversalAdditionalCameraData _postProcessLayer;
        private AntialiasingMode _defaultAntiAliasing;
        private bool _defaultCameraMSAA = false;
        #endregion

        public FuCameraWindow(FuCameraWindowDefinition windowDefinition) : base(windowDefinition)
        {
            AutoCameraFPS = true;
            SuperSampling = windowDefinition.SuperSampling;
            Camera = windowDefinition.Camera;

            // get post process data
            _postProcessLayer = Camera.GetComponent<UniversalAdditionalCameraData>();
            if (_postProcessLayer != null)
            {
                _defaultAntiAliasing = _postProcessLayer.antialiasing;
            }
            _defaultCameraMSAA = Camera.allowMSAA;

            // set default MSAA friendly texture format
            _currentTextureFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.B10G11R11_UFloatPack32;
            _currentTextureDepth = 24;

            // get URP pipeline settings
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            int msaaSamples = urpAsset?.msaaSampleCount ?? 1;
            bool isRenderGraphEnabled = !GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>().enableRenderCompatibilityMode;
            bool supportsHDR = urpAsset?.supportsHDR ?? false;

            // create the render texture (dyanmic settings from project settings)
            _rTexture = new RenderTexture(Mathf.Max(Size.x, 1), Mathf.Max(Size.y, 1), _currentTextureDepth, _currentTextureFormat)
            {
                antiAliasing = Fugui.GetSrpMsaaSampleCount(msaaSamples),
                depthStencilFormat = isRenderGraphEnabled 
                    ? UnityEngine.Experimental.Rendering.GraphicsFormat.D24_UNorm_S8_UInt 
                    : UnityEngine.Experimental.Rendering.GraphicsFormat.None,
                useDynamicScale = true,
                useMipMap = supportsHDR,
                autoGenerateMips = false,
                enableRandomWrite = false
            };

            _rTexture.Create();
            Camera.targetTexture = _rTexture;

            OnResized += ImGuiCameraWindow_OnResize;
            OnDock += ImGuiCameraWindow_OnDock;
            OnClosed += UICameraWindow_OnClosed;
            ImGuiCameraWindow_OnResize(this);
            _windowFlags |= ImGuiWindowFlags.NoScrollbar;
            _windowFlags |= ImGuiWindowFlags.NoScrollWithMouse;
            NeedToUpdateCamera = true;
            _lastCameraRenderTime = float.MinValue;
            IsInterractable = true;
            Camera.enabled = false;

            OnAddToContainer += FuCameraWindow_OnAddToContainer;

            UI = (window) =>
            {
                Vector2 cursorPos = ImGui.GetCursorScreenPos();
                Container.ImGuiImage(_rTexture, WorkingAreaSize);
                ImGui.SetCursorScreenPos(cursorPos);
                windowDefinition.UI?.Invoke(this);
            };

            // register raycaster
            _raycaster = new FuRaycaster(ID, GetCameraRay, () => Mouse.IsPressed(FuMouseButton.Left), () => Mouse.IsPressed(FuMouseButton.Right), () => false, () => Mouse.Wheel.y, () => IsHoveredContent);
            FuRaycasting.RegisterRaycaster(_raycaster);
        }

        private void FuCameraWindow_OnAddToContainer(FuWindow window)
        {
            // set default post process data
            if (_postProcessLayer != null)
            {
                _postProcessLayer.antialiasing = _defaultAntiAliasing;
            }
            Camera.allowMSAA = _defaultCameraMSAA;

            // set default MSAA friendly texture format
            _currentTextureFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.B10G11R11_UFloatPack32;
            _currentTextureDepth = 24;

            // release previous render texture
            _rTexture.Release();

            // get URP pipeline settings
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            int msaaSamples = urpAsset?.msaaSampleCount ?? 1;
            bool isRenderGraphEnabled = !GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>().enableRenderCompatibilityMode;
            bool supportsHDR = urpAsset?.supportsHDR ?? false;

            // recreate render texture with dynamic settings
            _rTexture = new RenderTexture(Mathf.Max(Size.x, 1), Mathf.Max(Size.y, 1), _currentTextureDepth, _currentTextureFormat)
            {
                antiAliasing = Fugui.GetSrpMsaaSampleCount(msaaSamples),
                depthStencilFormat = isRenderGraphEnabled 
                    ? UnityEngine.Experimental.Rendering.GraphicsFormat.D24_UNorm_S8_UInt 
                    : UnityEngine.Experimental.Rendering.GraphicsFormat.None,
                useDynamicScale = true,
                useMipMap = supportsHDR,
                autoGenerateMips = false,
                enableRandomWrite = false
            };

            _rTexture.Create();
            Camera.targetTexture = _rTexture;

            // resize cam target
            Camera.pixelRect = new Rect(0, 0, (int)(WorkingAreaSize.x * _superSampling), (int)(WorkingAreaSize.y * _superSampling));

            // force update camera
            NeedToUpdateCamera = true;
        }

        private void UICameraWindow_OnClosed(FuWindow window)
        {
            FuRaycasting.UnRegisterRaycaster(window.ID);
            window.OnClosed -= UICameraWindow_OnClosed;
            // set default post process data
            if (_postProcessLayer != null)
            {
                _postProcessLayer.antialiasing = _defaultAntiAliasing;
            }
            Camera.allowMSAA = _defaultCameraMSAA;
        }

        /// <summary>
        /// draw debug panel
        /// </summary>
        internal override void DrawDebugPanel()
        {
            base.DrawDebugPanel();

            if (!Fugui.Settings.DrawDebugPanel)
            {
                return;
            }

            ImGui.SetCursorPos(new Vector2(ImGui.GetContentRegionAvail().x - 232f, 16f));
            Fugui.Push(ImGuiStyleVar.ChildRounding, 4f);
            Fugui.Push(ImGuiCol.ChildBg, new Vector4(.1f, .1f, .1f, 1f));
            if (ImGui.BeginChild(ID + "cs", new Vector2(224f, 96f)))
            {
                // super sampling
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
                // states
                ImGui.Text("State : " + State);
                ImGui.Text("FPS : " + (int)CurrentCameraFPS + " (" + (CameraDeltaTime * 1000f).ToString("f2") + " ms)");
                ImGui.Text("Target : " + TargetCameraFPS + "  (" + ((int)(_targetCameraDeltaTimeMs * 1000)).ToString() + " ms)"); ImGui.Dummy(new Vector2(4f, 0f));
            }
            ImGuiNative.igEndChild();
            Fugui.PopColor();
            Fugui.PopStyle();
        }

        /// <summary>
        /// resize camera on next frame when window dock state change
        /// </summary>
        /// <param name="window">related UIWindow</param>
        private void ImGuiCameraWindow_OnDock(FuWindow window)
        {
            NeedToUpdateCamera = true;
        }

        /// <summary>
        /// resize camera on next frame when window resize
        /// </summary>
        /// <param name="window">related UIWindow</param>
        private void ImGuiCameraWindow_OnResize(FuWindow window)
        {
            NeedToUpdateCamera = true;
        }

        /// <summary>
        /// update camera and render texture size
        /// </summary>
        /// <summary>
        /// update camera and render texture size
        /// </summary>
        /// <summary>
        /// update camera and render texture size
        /// </summary>
        private void updateCameraSize()
        {
            if (!NeedToUpdateCamera)
            {
                return;
            }
        
            NeedToUpdateCamera = false;
        
            if (WorkingAreaSize.x <= 10 || WorkingAreaSize.y <= 10 || _superSampling <= 0.1f)
            {
                return;
            }
        
            // release previous render texture
            _rTexture.Release();
        
            // get URP pipeline settings
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            int msaaSamples = urpAsset?.msaaSampleCount ?? 1;
            bool isRenderGraphEnabled = !GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>().enableRenderCompatibilityMode;
            bool supportsHDR = urpAsset?.supportsHDR ?? false;
        
            // platform-specific MSAA handling
            bool isMacOS = Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor;
            int resolvedMsaa = isMacOS && isRenderGraphEnabled ? 1 : Fugui.GetSrpMsaaSampleCount(msaaSamples);
        
            // recreate render texture with updated size and dynamic settings
            _rTexture = new RenderTexture(
                Mathf.Max((int)(WorkingAreaSize.x * _superSampling), 1),
                Mathf.Max((int)(WorkingAreaSize.y * _superSampling), 1),
                _currentTextureDepth,
                _currentTextureFormat)
            {
                antiAliasing = resolvedMsaa,
                depthStencilFormat = isRenderGraphEnabled 
                    ? UnityEngine.Experimental.Rendering.GraphicsFormat.D24_UNorm_S8_UInt 
                    : UnityEngine.Experimental.Rendering.GraphicsFormat.None,
                useDynamicScale = true,
                useMipMap = supportsHDR,
                autoGenerateMips = false,
                enableRandomWrite = false
            };
        
            _rTexture.Create();
            Camera.targetTexture = _rTexture;
        
            // macOS-specific workaround: disable post-processing and MSAA on camera
            if (isMacOS)
            {
                if (_postProcessLayer != null)
                {
                    _postProcessLayer.renderPostProcessing = false;
                    _postProcessLayer.antialiasing = AntialiasingMode.None;
                }
        
                Camera.allowMSAA = false;
            }
        
            // resize cam target
            Camera.pixelRect = new Rect(0, 0, _rTexture.width, _rTexture.height);
        
            ForceRenderCamera();
            updateCameraRender();
        }


        /// <summary>
        /// check whatever camera must be enabled or disabled to reach target camera FPS
        /// </summary>
        private void updateCameraRender()
        {
            // did the camera must be enabled for a frame
            if ((Fugui.Time > _lastCameraRenderTime + _targetCameraDeltaTimeMs) || _forceCameraRender)
            {
                Camera.Render();
                CameraDeltaTime = Fugui.Time - _lastCameraRenderTime;
                CurrentCameraFPS = 1f / CameraDeltaTime;
                _lastCameraRenderTime = Fugui.Time;
            }
            _forceCameraRender = false;
        }

        /// <summary>
        /// set performance state for this window
        /// will auto set target FPS
        /// </summary>
        /// <param name="state">performance state to set</param>
        internal override void SetPerformanceState(FuWindowState state)
        {
            base.SetPerformanceState(state);
            if (AutoCameraFPS)
            {
                switch (state)
                {
                    default:
                    case FuWindowState.Idle:
                        TargetCameraFPS = Fugui.Settings.IdleCameraFPS;
                        break;

                    case FuWindowState.Manipulating:
                        TargetCameraFPS = Fugui.Settings.ManipulatingFPS;
                        break;
                }
            }
        }

        /// <summary>
        /// draw the window and do some camera related process
        /// </summary>
        public override void DrawWindow()
        {
            base.DrawWindow();
            updateCameraRender();
            updateCameraSize();
        }

        #region public Utils
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
