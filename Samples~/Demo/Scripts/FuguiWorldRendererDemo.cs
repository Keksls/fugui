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

#if FU_CUSTOM_MATERIALS_ENABLED
    private const string HolographicOverlayShaderResourcePath = "FuguiCustomMaterials/FuguiDemoHolographicGlass";
    private const string HolographicWorldShaderResourcePath = "FuguiCustomMaterials/FuguiDemoHolographicWorld";

    private static readonly int GlowProperty = Shader.PropertyToID("_Glow");
    private static readonly int InteractionProperty = Shader.PropertyToID("_Interaction");

    private Material _holographicOverlayMaterial;
    private Material _holographicWorldMaterial;
    private FuDrawMaterial _holographicWorldDrawMaterial;
    private bool _customMaterialLoadAttempted;
#endif
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
    /// Releases the optional custom materials created by the world-space showcase.
    /// </summary>
    private void OnDestroy()
    {
#if FU_CUSTOM_MATERIALS_ENABLED
        // FuDrawMaterial leaves ownership to the caller, so the sample destroys both runtime instances.
        DestroyRuntimeMaterial(_holographicOverlayMaterial);
        DestroyRuntimeMaterial(_holographicWorldMaterial);
        _holographicOverlayMaterial = null;
        _holographicWorldMaterial = null;
        _holographicWorldDrawMaterial = null;
#endif
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
            DrawPanel(
                surface.DrawList,
                new Rect(0f, 0f, 360f, 88f),
                "Holographic world surface",
                "PushMaterial / PopMaterial - depth tested",
                new Color(0.08f, 0.15f, 0.20f, 0.82f),
                true);
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
    /// <param name="useCustomBackground">Whether to try the opt-in holographic world material.</param>
    private void DrawPanel(
        FuDrawList drawList,
        Rect rect,
        string title,
        string subtitle,
        Color background,
        bool useCustomBackground = false)
    {
        uint backgroundColor = Fugui.GetColorU32(background);
        uint borderColor = Fugui.GetColorU32(new Color(1f, 1f, 1f, 0.45f));
        uint titleColor = Fugui.GetColorU32(new Color(1f, 1f, 1f, 0.98f));
        uint subtitleColor = Fugui.GetColorU32(new Color(0.78f, 0.88f, 0.92f, 0.92f));
        Vector2 min = rect.min;
        Vector2 max = rect.max;

        bool customBackgroundDrawn = false;
#if FU_CUSTOM_MATERIALS_ENABLED
        // Fall back to the ordinary primitive if the optional Resources shaders are unavailable.
        customBackgroundDrawn = useCustomBackground && DrawCustomWorldBackground(drawList, min, max);
#endif
        if (!customBackgroundDrawn)
        {
            drawList.AddRectFilled(min, max, backgroundColor, 10f, FuDrawFlags.RoundCornersAll);
        }

        drawList.AddRect(min, max, borderColor, 10f, FuDrawFlags.RoundCornersAll, 2f);
        drawList.AddText(min + new Vector2(18f, 16f), titleColor, title);
        drawList.AddText(min + new Vector2(18f, 48f), subtitleColor, subtitle);
    }

#if FU_CUSTOM_MATERIALS_ENABLED
    /// <summary>
    /// Draws the camera-following panel background with the custom world-space shader.
    /// </summary>
    /// <param name="drawList">World draw list receiving the image geometry.</param>
    /// <param name="min">Minimum panel coordinate in surface pixels.</param>
    /// <param name="max">Maximum panel coordinate in surface pixels.</param>
    /// <returns>True when the custom background was emitted.</returns>
    private bool DrawCustomWorldBackground(FuDrawList drawList, Vector2 min, Vector2 max)
    {
        if (!EnsureWorldDrawMaterial())
        {
            return false;
        }

        // Animate a subtle intensity pulse to make the world-space material obvious in the demo scene.
        float interaction = 0.42f + Mathf.Sin(Time.unscaledTime * 1.8f) * 0.16f;
        _holographicWorldMaterial.SetFloat(InteractionProperty, interaction);

        drawList.PushMaterial(_holographicWorldDrawMaterial, Texture2D.whiteTexture);
        try
        {
            drawList.AddImageRounded(
                Texture2D.whiteTexture,
                min,
                max,
                Vector2.zero,
                Vector2.one,
                Vector4.one,
                10f,
                FuDrawFlags.RoundCornersAll);
        }
        finally
        {
            // Always restore the standard world material before drawing the border and text.
            drawList.PopMaterial();
        }

        return true;
    }

    /// <summary>
    /// Lazily creates the overlay and three-pass world materials required by FuDrawMaterial.
    /// </summary>
    /// <returns>True when the holographic world material configuration is ready.</returns>
    private bool EnsureWorldDrawMaterial()
    {
        if (_holographicWorldDrawMaterial != null)
        {
            return true;
        }

        if (_customMaterialLoadAttempted)
        {
            return false;
        }

        // Resources keeps this scene component reference-free while ensuring the sample shaders are included.
        _customMaterialLoadAttempted = true;
        Shader overlayShader = Resources.Load<Shader>(HolographicOverlayShaderResourcePath);
        Shader worldShader = Resources.Load<Shader>(HolographicWorldShaderResourcePath);
        if (overlayShader == null || worldShader == null)
        {
            return false;
        }

        _holographicOverlayMaterial = CreateRuntimeMaterial(overlayShader, "Fugui Demo - Holographic Overlay");
        _holographicWorldMaterial = CreateRuntimeMaterial(worldShader, "Fugui Demo - Holographic World");
        _holographicWorldMaterial.SetFloat(GlowProperty, 1.45f);
        _holographicWorldDrawMaterial = new FuDrawMaterial(
            _holographicOverlayMaterial,
            0,
            _holographicWorldMaterial,
            0,
            1,
            2);
        return true;
    }

    /// <summary>
    /// Creates a hidden runtime material from a demo shader.
    /// </summary>
    /// <param name="shader">Shader used by the material.</param>
    /// <param name="name">Diagnostic material name.</param>
    /// <returns>The newly created caller-owned material.</returns>
    private static Material CreateRuntimeMaterial(Shader shader, string name)
    {
        // HideAndDontSave keeps temporary demo resources out of the scene and project.
        return new Material(shader)
        {
            name = name,
            hideFlags = HideFlags.HideAndDontSave,
        };
    }

    /// <summary>
    /// Destroys a caller-owned runtime material using the current Unity lifetime rules.
    /// </summary>
    /// <param name="material">Runtime material to destroy.</param>
    private static void DestroyRuntimeMaterial(Material material)
    {
        if (material == null)
        {
            return;
        }

        // Unity requires delayed destruction in play mode and immediate destruction while editing.
        if (Application.isPlaying)
        {
            Destroy(material);
        }
        else
        {
            DestroyImmediate(material);
        }
    }
#endif

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
