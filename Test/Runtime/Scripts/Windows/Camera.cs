using Fu.Core;
using Fu.Framework;
using UnityEngine;

public class CameraWindow : MonoBehaviour
{
    public Camera Camera;
    public Test3DRaycaster Raycaster;
    public float HitForce = 50f;
    private FuCameraWindow _cameraWindow;

    private void Awake()
    {
        registerCameraViewWindow();
    }

    private void registerCameraViewWindow()
    {
        // add main camera window
        FuWindowDefinition camWinDef = new FuCameraWindowDefinition(FuWindowsNames.MainCameraView, Camera, "3DView", null, flags: FuWindowFlags.NoInterractions);
        camWinDef.SetCustomWindowType<FuCameraWindow>();
        camWinDef.OnUIWindowCreated += CamWinDef_OnUIWindowCreated;

        // camera FPS overlay
        FuOverlay fps1 = new FuOverlay("oCamFPS", new Vector2Int(102, 52), (overlay) =>
        {
            drawCameraFPSOverlay(_cameraWindow);
        }, FuOverlayFlags.Default, FuOverlayDragPosition.Right);
        fps1.AnchorWindowDefinition(camWinDef, FuOverlayAnchorLocation.TopRight, new Vector2Int(0, 64));

        // camera supersampling overlay
        FuOverlay ss1 = new FuOverlay("oCamSS", new Vector2Int(224, 36), (overlay) =>
        {
            drawSupersamplingOverlay(_cameraWindow);
        }, FuOverlayFlags.Default, FuOverlayDragPosition.Right);
        ss1.AnchorWindowDefinition(camWinDef, FuOverlayAnchorLocation.BottomRight, Vector2Int.zero);
    }

    private void CamWinDef_OnUIWindowCreated(FuWindow camWindow)
    {
        _cameraWindow = (FuCameraWindow)camWindow;
        _cameraWindow.Camera.GetComponent<MouseOrbitImproved>().Camera = _cameraWindow;
    }

    #region overlays
    void drawCameraFPSOverlay(FuCameraWindow cam)
    {
        using (FuGrid grid = new FuGrid("camFPS", new FuGridDefinition(2, new int[] { 42 }, responsiveMinWidth: 0)))
        {
            grid.Text("cam FPS");
            grid.Text(((int)cam.CurrentCameraFPS).ToString());
            grid.Text("ui. FPS");
            grid.Text(((int)cam.CurrentFPS).ToString());
        }
    }

    void drawSupersamplingOverlay(FuCameraWindow cam)
    {
        using (var layout = new FuLayout())
        {
            if (layout.RadioButton("x0.5", cam.SuperSampling == 0.5f))
            {
                cam.SuperSampling = 0.5f;
            }
            layout.SameLine();
            if (layout.RadioButton("x1", cam.SuperSampling == 1f))
            {
                cam.SuperSampling = 1f;
            }
            layout.SameLine();
            if (layout.RadioButton("x1.5", cam.SuperSampling == 1.5f))
            {
                cam.SuperSampling = 1.5f;
            }
            layout.SameLine();
            if (layout.RadioButton("x2", cam.SuperSampling == 2f))
            {
                cam.SuperSampling = 2f;
            }
        }
    }
    #endregion

    private void Update()
    {
        if (_cameraWindow == null || !_cameraWindow.IsInitialized)
        {
            return;
        }

        if (_cameraWindow.Mouse.IsDown(FuMouseButton.Left) && !_cameraWindow.Mouse.IsHoverOverlay && !_cameraWindow.Mouse.IsHoverPopup)
        {
            RaycastHit hit;
            Ray ray = _cameraWindow.GetCameraRay();
            if (Physics.Raycast(ray, out hit))
            {
                Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
                if (rb == null)
                    return;
                rb.AddExplosionForce(HitForce, hit.point, 5f, 0f, ForceMode.Impulse);
            }
        }
    }
}