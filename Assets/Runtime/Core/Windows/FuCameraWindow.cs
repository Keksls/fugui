using Fu.Framework;
using ImGuiNET;
using System;
using UnityEngine;

namespace Fu.Core
{
    public class FuCameraWindow : FuWindow
    {
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
            internal set
            {
                _targetCameraDeltaTimeMs = 1f / value;
            }
        }
        public float CameraDeltaTime { get; internal set; }
        public float CurrentCameraFPS { get; internal set; }
        public IntPtr PixelsPtr { get; private set; }
        public Camera Camera { get; private set; }
        private bool _forceCameraRender;
        private float _targetCameraDeltaTimeMs;
        private float _lastCameraRenderTime;
        private RenderTexture _rTexture;
        private FuRaycaster _raycaster;

        public FuCameraWindow(FuCameraWindowDefinition windowDefinition) : base(windowDefinition)
        {
            SuperSampling = 1f;
            Camera = windowDefinition.Camera;

            // create the render texture
            _rTexture = new RenderTexture(Size.x, Size.y, 32, RenderTextureFormat.RGB111110Float);
            _rTexture.antiAliasing = 8;
            _rTexture.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.None;
            _rTexture.useDynamicScale = true;
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
            IsInterractible = true;
            Camera.enabled = false;

            UI = (window) =>
            {
                Vector2 cursorPos = ImGui.GetCursorScreenPos();
                ImGui.GetWindowDrawList().AddImage(Container.GetTextureID(_rTexture), cursorPos, cursorPos + window.WorkingAreaSize);
                //Container.ImGuiImage(_rTexture, WorkingAreaSize);
                //ImGui.SetCursorScreenPos(cursorPos);
                windowDefinition.UI?.Invoke(this);
            };

            // register raycaster
            _raycaster = new FuRaycaster(ID, GetCameraRay, () => Mouse.IsPressed(0), () => Mouse.IsPressed(1), () => Mouse.IsPressed(2), () => Mouse.Wheel.y, () => IsHovered && !Mouse.IsHoverOverlay && !Mouse.IsHoverPopup);
            FuRaycasting.RegisterRaycaster(_raycaster);
        }

        private void UICameraWindow_OnClosed(FuWindow window)
        {
            FuRaycasting.UnRegisterRaycaster(window.ID);
            window.OnClosed -= UICameraWindow_OnClosed;
        }

        /// <summary>
        /// draw debug panel
        /// </summary>
        internal override void drawDebugPanel()
        {
            base.drawDebugPanel();

            if (!ShowDebugPanel && !Fugui.Settings.DrawDebugPanel)
            {
                return;
            }

            ImGui.SetCursorPos(new Vector2(ImGui.GetContentRegionAvail().x - 232f, 16f));
            Fugui.Push(ImGuiStyleVar.ChildRounding, 4f);
            Fugui.Push(ImGuiCol.ChildBg, new Vector4(.1f, .1f, .1f, 1f));
            ImGui.BeginChild(ID + "cameraSettings", new Vector2(224f, 96f));
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
            ImGui.Text("State : " + WindowPerformanceState);
            ImGui.Text("FPS : " + (int)CurrentCameraFPS + " (" + (CameraDeltaTime * 1000f).ToString("f2") + " ms)");
            ImGui.Text("Target : " + TargetCameraFPS + "  (" + ((int)(_targetCameraDeltaTimeMs * 1000)).ToString() + " ms)"); ImGui.Dummy(new Vector2(4f, 0f));
            ImGui.EndChild();
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
        private void updateCameraSize()
        {
            if (!NeedToUpdateCamera)
            {
                return;
            }

            NeedToUpdateCamera = false;
            // resize render texture
            if(WorkingAreaSize.x <= 10 || WorkingAreaSize.y <= 10 || _superSampling <= 0.1f)
            {
                return;
            }
            _rTexture.Release();
            _rTexture.width = (int)(WorkingAreaSize.x * _superSampling);
            _rTexture.height = (int)(WorkingAreaSize.y * _superSampling);
            Camera.targetTexture = _rTexture;
            // resize cam target
            Camera.pixelRect = new Rect(0, 0, (int)(WorkingAreaSize.x * _superSampling), (int)(WorkingAreaSize.y * _superSampling));
            // will render for next frame
            _forceCameraRender = true;
        }

        /// <summary>
        /// check whatever camera must be enabled or disabled to reach target camera FPS
        /// </summary>
        private void updateCameraRender()
        {
            // did the camera must be enabled for a frame
            bool mustDraw = (Fugui.Time > _lastCameraRenderTime + _targetCameraDeltaTimeMs) || _forceCameraRender;
            _forceCameraRender = false;
            if (mustDraw)
            {
                Camera.Render();
                CameraDeltaTime = Fugui.Time - _lastCameraRenderTime;
                CurrentCameraFPS = 1f / CameraDeltaTime;
                _lastCameraRenderTime = Fugui.Time;
            }
        }

        /// <summary>
        /// set performance state for this window
        /// will auto set target FPS
        /// </summary>
        /// <param name="state">performance state to set</param>
        internal override void SetPerformanceState(FuWindowState state)
        {
            base.SetPerformanceState(state);
            switch (state)
            {
                default:
                case FuWindowState.Idle:
                    TargetCameraFPS = 10;
                    break;

                case FuWindowState.Manipulating:
                    TargetCameraFPS = 48;
                    break;
            }
        }

        /// <summary>
        /// draw the window and do some camera related process
        /// </summary>
        public override void DrawWindow()
        {
            Fugui.Push(ImGuiCol.WindowBg, new Vector4(0f, 0f, 0f, 1f));
            base.DrawWindow();
            Fugui.PopColor();
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