using Fu.Core;
using Fu.Framework;
using UnityEngine;

public class CameraWindow : FuCameraWindowBehaviour
{
    public float HitForce = 50f;

    /// <summary>
    /// Whenever the window is created, set the camera to the MouseOrbitImproved component
    /// </summary>
    /// <param name="window"> FuWindow instance</param>
    public override void OnWindowCreated(FuWindow window)
    {
        Camera.GetComponent<MouseOrbitImproved>().Camera = CameraWindow;
    }

    /// <summary>
    /// This method is called when the window definition is created.
    /// </summary>
    /// <param name="windowDefinition"> The FuWindowDefinition instance that was created.</param>
    public override void OnWindowDefinitionCreated(FuWindowDefinition windowDefinition)
    {
        // camera FPS overlay
        FuOverlay fps1 = new FuOverlay("oCamFPS", new Vector2Int(102, 52), (overlay) =>
        {
            drawCameraFPSOverlay(CameraWindow);
        }, FuOverlayFlags.Default, FuOverlayDragPosition.Right);
        fps1.AnchorWindowDefinition(windowDefinition, FuOverlayAnchorLocation.TopRight);

        // camera supersampling overlay
        FuOverlay ss1 = new FuOverlay("oCamSS", new Vector2Int(196, 36), (overlay) =>
        {
            drawSupersamplingOverlay(CameraWindow);
        }, FuOverlayFlags.Default, FuOverlayDragPosition.Left);
        ss1.AnchorWindowDefinition(windowDefinition, FuOverlayAnchorLocation.TopLeft);
    }

    #region overlays
    /// <summary>
    /// Draw camera FPS on an overlay
    /// </summary>
    /// <param name="cam">FuCameraWindow definition</param>
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

    /// <summary>
    /// Draw camera supersampling settings overlay
    /// </summary>
    /// <param name="cam">FuCameraWindow definition</param>
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
        if (CameraWindow == null || !CameraWindow.IsInitialized)
        {
            return;
        }

        // just for fun and for demo raycast from camera window
        if (CameraWindow.Mouse.IsDown(FuMouseButton.Left) && !CameraWindow.Mouse.IsHoverOverlay && !CameraWindow.Mouse.IsHoverPopup)
        {
            RaycastHit hit;
            Ray ray = CameraWindow.GetCameraRay();
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