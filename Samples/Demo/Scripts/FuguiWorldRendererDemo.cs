using Fu;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Sample component that demonstrates Fugui world-space draw-list surfaces.
/// </summary>
public class FuguiWorldRendererDemo : MonoBehaviour
{
    #region State
    [Header("Camera")]
    [SerializeField]
    private Camera _camera;

    [SerializeField]
    private float _cameraSurfaceDistance = 1.25f;

    [SerializeField]
    private Vector3 _cameraSurfaceOffset = new Vector3(0f, -0.35f, 0f);

    [Header("Raycast")]
    [SerializeField]
    private LayerMask _sceneRaycastMask = ~0;

    [SerializeField]
    private float _sceneRaycastDistance = 200f;

    [SerializeField]
    private Vector3 _hitSurfaceOffset = new Vector3(0f, 0.12f, 0f);

    [SerializeField]
    private string _raycasterID = "FuguiWorldRendererDemoMouse";

    private FuRaycaster _raycaster;
    private FuContext _hookedContext;
    private bool _raycasterRegistered;
    private bool _hasHit;
    private RaycastHit _hit;
    #endregion

    #region Methods
    /// <summary>
    /// Handles component enable and registers the demo raycaster.
    /// </summary>
    private void OnEnable()
    {
        EnsureCamera();
        RegisterRaycaster();
    }

    /// <summary>
    /// Handles component disable and unregisters runtime hooks.
    /// </summary>
    private void OnDisable()
    {
        UnhookContext();
        UnregisterRaycaster();
    }

    /// <summary>
    /// Updates the demo raycast and render callback registration.
    /// </summary>
    private void Update()
    {
        EnsureCamera();
        EnsureContextHook();
        UpdateSceneHit();
    }

    /// <summary>
    /// Ensures a camera is available for the sample.
    /// </summary>
    private void EnsureCamera()
    {
        if (_camera != null)
        {
            return;
        }

        _camera = Camera.main;
    }

    /// <summary>
    /// Registers the FuRaycaster used by the sample.
    /// </summary>
    private void RegisterRaycaster()
    {
        if (_raycasterRegistered)
        {
            return;
        }

#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;

        _raycaster = new FuRaycaster(
            _raycasterID,
            GetMouseRay,
            () => Mouse.current != null && Mouse.current.leftButton.isPressed,
            () => Mouse.current != null && Mouse.current.rightButton.isPressed,
            () => Mouse.current != null && Mouse.current.middleButton.isPressed,
            () => Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f,
            () => isActiveAndEnabled && _camera != null && Mouse.current != null,
            () => _camera != null ? _camera.transform : null);
#else
_raycaster = new FuRaycaster(
    _raycasterID,
    GetMouseRay,
    () => Input.GetMouseButton(0),
    () => Input.GetMouseButton(1),
    () => Input.GetMouseButton(2),
    () => Input.mouseScrollDelta.y,
    () => isActiveAndEnabled && _camera != null,
    () => _camera != null ? _camera.transform : null);
#endif

        _raycasterRegistered = FuRaycasting.RegisterRaycaster(_raycaster);
    }

    /// <summary>
    /// Unregisters the sample FuRaycaster.
    /// </summary>
    private void UnregisterRaycaster()
    {
        if (!_raycasterRegistered)
        {
            return;
        }

        FuRaycasting.UnRegisterRaycaster(_raycasterID);
        _raycasterRegistered = false;
        _raycaster = null;
    }

    /// <summary>
    /// Ensures the sample draws during the active default Fugui context.
    /// </summary>
    private void EnsureContextHook()
    {
        FuContext context = Fugui.DefaultContext;
        if (_hookedContext == context)
        {
            return;
        }

        UnhookContext();
        _hookedContext = context;
        if (_hookedContext != null)
        {
            _hookedContext.OnLastRender += DrawWorldSurfaces;
        }
    }

    /// <summary>
    /// Removes the render callback from the currently hooked context.
    /// </summary>
    private void UnhookContext()
    {
        if (_hookedContext == null)
        {
            return;
        }

        _hookedContext.OnLastRender -= DrawWorldSurfaces;
        _hookedContext = null;
    }

    /// <summary>
    /// Returns the mouse ray used by the demo raycaster.
    /// </summary>
    /// <returns>The current mouse ray.</returns>
    private Ray GetMouseRay()
    {
        if (_camera == null)
        {
            return new Ray(transform.position, transform.forward);
        }

#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return new Ray(_camera.transform.position, _camera.transform.forward);
        }

        return _camera.ScreenPointToRay(mouse.position.ReadValue());
#else
    return _camera.ScreenPointToRay(Input.mousePosition);
#endif
    }

    /// <summary>
    /// Updates the scene hit point driven by the FuRaycaster ray.
    /// </summary>
    private void UpdateSceneHit()
    {
        if (_raycaster == null || _camera == null)
        {
            _hasHit = false;
            return;
        }

        Ray ray = _raycaster.GetRay();
        _hasHit = Physics.Raycast(ray, out _hit, _sceneRaycastDistance, _sceneRaycastMask, QueryTriggerInteraction.Ignore);
    }

    /// <summary>
    /// Draws all sample world-space Fugui surfaces.
    /// </summary>
    private void DrawWorldSurfaces()
    {
        if (_camera == null)
        {
            return;
        }

        DrawCameraFollowerSurface();
        DrawHitPointSurface();
    }

    /// <summary>
    /// Draws a surface attached to the camera transform.
    /// </summary>
    private void DrawCameraFollowerSurface()
    {
        Transform cameraTransform = _camera.transform;
        Vector3 position = cameraTransform.position +
            cameraTransform.forward * _cameraSurfaceDistance +
            cameraTransform.TransformVector(_cameraSurfaceOffset);

        FuguiWorldSurfaceDesc desc = FuguiWorldSurfaceDesc.Default;
        desc.Position = position;
        desc.Rotation = cameraTransform.rotation;
        desc.Scale = Vector3.one;
        desc.Size = new Vector2(0.9f, 0.22f);
        desc.Resolution = new Vector2Int(360, 88);
        desc.Pivot = FuguiWorldPivot.Center;
        desc.DepthMode = FuguiWorldDepthMode.Test;
        desc.SortingOrder = 0;

        using (FuguiWorldSurface surface = Fugui.World.Surface(desc))
        {
            DrawPanel(surface.DrawList, new Rect(0f, 0f, 360f, 88f), "Camera follower", "World draw-list surface", new Color(0.08f, 0.15f, 0.20f, 0.82f));
        }
    }

    /// <summary>
    /// Draws a surface above the latest scene raycast hit point.
    /// </summary>
    private void DrawHitPointSurface()
    {
        if (!_hasHit)
        {
            return;
        }

        Vector3 position = _hit.point + _hitSurfaceOffset;
        FuguiWorldSurfaceDesc desc = FuguiWorldSurfaceDesc.Default;
        desc.Position = position;
        desc.Rotation = _camera.transform.rotation;
        desc.Scale = Vector3.one;
        desc.Size = new Vector2(1.05f, 0.28f);
        desc.Resolution = new Vector2Int(420, 112);
        desc.Pivot = FuguiWorldPivot.Center;
        desc.DepthMode = FuguiWorldDepthMode.Test;
        desc.SortingOrder = 1;

        using (FuguiWorldSurface surface = Fugui.World.Surface(desc))
        {
            string title = _hit.collider != null ? _hit.collider.name : "Hit";
            string positionText = FormatVector(_hit.point);
            DrawPanel(surface.DrawList, new Rect(0f, 0f, 420f, 112f), title, positionText, new Color(0.15f, 0.08f, 0.10f, 0.86f));
            DrawHitMarker(surface.DrawList);
        }
    }

    /// <summary>
    /// Draws a generic panel using only FuDrawList primitives.
    /// </summary>
    /// <param name="drawList">Draw list to populate.</param>
    /// <param name="rect">Panel rectangle in surface pixels.</param>
    /// <param name="title">Title text.</param>
    /// <param name="subtitle">Subtitle text.</param>
    /// <param name="background">Panel background color.</param>
    private void DrawPanel(FuDrawList drawList, Rect rect, string title, string subtitle, Color background)
    {
        uint backgroundColor = Fugui.GetColorU32(background);
        uint borderColor = Fugui.GetColorU32(new Color(1f, 1f, 1f, 0.45f));
        uint titleColor = Fugui.GetColorU32(new Color(1f, 1f, 1f, 0.98f));
        uint subtitleColor = Fugui.GetColorU32(new Color(0.78f, 0.88f, 0.92f, 0.92f));
        Vector2 min = rect.min;
        Vector2 max = rect.max;

        drawList.AddRectFilled(min, max, backgroundColor, 10f, FuDrawFlags.RoundCornersAll);
        drawList.AddRect(min, max, borderColor, 10f, FuDrawFlags.RoundCornersAll, 2f);
        drawList.AddText(min + new Vector2(18f, 16f), titleColor, title);
        drawList.AddText(min + new Vector2(18f, 48f), subtitleColor, subtitle);
    }

    /// <summary>
    /// Draws a small marker line in the hit point panel.
    /// </summary>
    /// <param name="drawList">Draw list to populate.</param>
    private void DrawHitMarker(FuDrawList drawList)
    {
        uint markerColor = Fugui.GetColorU32(new Color(1f, 0.25f, 0.22f, 0.95f));
        drawList.AddLine(new Vector2(18f, 92f), new Vector2(402f, 92f), markerColor, 3f);
    }

    /// <summary>
    /// Formats a world position for display.
    /// </summary>
    /// <param name="value">World position.</param>
    /// <returns>Formatted position string.</returns>
    private static string FormatVector(Vector3 value)
    {
        return $"World: {value.x:0.00}, {value.y:0.00}, {value.z:0.00}";
    }
    #endregion
}
