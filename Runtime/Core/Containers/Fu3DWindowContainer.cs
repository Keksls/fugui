using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// A class that represent a 3D UI Container
    /// </summary>
    public class Fu3DWindowContainer : IFuWindowContainer
    {
        #region State
        public string ID { get; private set; }
        public FuWindow Window { get; private set; }
        public FuContext Context => _fuguiContext;
        public FuContainerScaleConfig ContainerScaleConfig => _fuguiContext != null ? _fuguiContext.ContainerScaleConfig : getDefault3DScaleConfig();
        public bool IsClosed { get; private set; }
        public bool RuntimeResizable => _runtimeResizable;
        public bool IsRuntimeResizing => _activeResizeHandleIndex != -1;
        public float Scale3D => _windows3DScale;
        public float PanelCurve => _panelCurve;
        public float PanelRounding => _panelRounding;
        public Vector2 LocalSize => getCurrentLocalSize();
        public Vector2Int RenderResolution => _useExplicitResolution ? _explicitResolution : _size;
        public Vector2Int LocalMousePos => _localMousePos;
        public Vector2Int Position => Vector2Int.zero;
        public Vector2Int Size => _size;
        public RenderTexture RenderTexture { get; private set; }
        public event Action<Vector3, Vector2> OnRuntimeResized;
        private GameObject _panelGameObject;
        public GameObject PanelGameObject => _panelGameObject;
        public Transform PanelTransform => _panelGameObject != null ? _panelGameObject.transform : null;
        public int FuguiContextID { get { return _fuguiContext != null ? _fuguiContext.ID : -1; } }
        public FuMouseState Mouse => _mouseState;
        public FuKeyboardState Keyboard => _keyboardState;
        private FuMouseState _mouseState;
        private FuKeyboardState _keyboardState;
        private Vector2Int _localMousePos;
        private Vector2Int _size;
        private Vector2 _localSize = Vector2.one;
        private float _windows3DScale;
        private bool _useExplicitResolution;
        private Vector2Int _explicitResolution;
        private bool _scaleResolutionWithPanel;
        private bool _matchResolutionToPanelAspect;
        private Vector2 _referenceLocalSize = Vector2.one;
        private Vector2Int _referenceResolution = Vector2Int.one;
        private Vector2Int _minResolution = Vector2Int.one;
        private Vector2Int _maxResolution = Vector2Int.zero;
        private float _panelDepth = 0.01f;
        private float _panelCurve = 0f;
        private float _panelRounding = Fu3DWindowSettings.DefaultPanelRounding;
        private FuUnityContext _fuguiContext;
        private static int _3DContextindex = 0;
        private Material _uiMaterial;
        private FuPanelMesh _panelMesh;
        private MeshCollider _panelCollider;
        private Material _resizeHandleMaterial;
        private MaterialPropertyBlock _resizeHandlePropertyBlock;
        private GameObject[] _resizeHandles;
        private Mesh[] _resizeHandleMeshes;
        private bool _runtimeResizable;
        private bool _runtimeResizeBlocked;
        private bool _resizeHandlesVisible;
        private int _activeResizeHandleIndex = -1;
        private int _hoveredResizeHandleIndex = -1;
        private string _activeResizeRaycasterID;
        private Vector3 _resizeStartPanelPosition;
        private Quaternion _resizeStartPanelRotation;
        private Vector2 _resizeStartLocalSize;
        private Vector2 _resizeStartHitLocal;
        private const float ResizeHandleOuterRadiusRatio = 0.14f;
        private const float ResizeHandleOuterRadiusMin = 0.075f;
        private const float ResizeHandleOuterRadiusMax = 0.24f;
        private const float ResizeHandleThicknessRatio = 0.26f;
        private const float ResizeHandleThicknessMin = 0.018f;
        private const float ResizeHandleThicknessMax = 0.055f;
        private const float ResizeHandleFrontOffset = -0.018f;
        private const int ResizeHandleArcSegments = 14;
        private const float RuntimeResizeMinSize = 0.0001f;
        private const int MaxPooledRenderTexturesPerKey = 4;
        private static readonly Dictionary<string, Stack<RenderTexture>> _renderTexturePool = new Dictionary<string, Stack<RenderTexture>>();
        private Vector2 _lastPanelMeshSize = new Vector2(-1f, -1f);
        private float _lastPanelMeshScale = -1f;
        private float _lastPanelRound = -1f;
        private float _lastPanelDepth = -1f;
        private float _lastPanelCurve = -1f;
        #endregion

        #region Constructors
        /// <summary>
        /// Instantiate a new 3D Container
        /// </summary>
        /// <param name="window">Window to add to this container</param>
        /// <param name="position">world 3D position of this container</param>
        /// <param name="rotation">world 3D rotation of this container</param>
        public Fu3DWindowContainer(FuWindow window, Fu3DWindowSettings settings, Vector3? position = null, Quaternion? rotation = null)
        {
            settings.Sanitize();
            _3DContextindex++;
            ID = "3DContext_" + _3DContextindex;

            _localMousePos = new Vector2Int(-1, -1);
            _useExplicitResolution = true;
            applyResolutionSettings(settings, true);
            _windows3DScale = getLegacyScaleFromSettings(settings);

            // remove the window from it's old container if has one
            window.TryRemoveFromContainer();
            // add the window to this container
            if (!TryAddWindow(window))
            {
                Debug.Log("Fail to create 3D container.");
                Close();
                return;
            }

            // resize the window
            Window.Size = settings.Resolution;
            Window.Is3DWindow = true;
            _size = window.Size;

            // Create RenderTexture
            RenderTexture = createRenderTexture(_size);

            if (!RenderTexture.IsCreated())
            {
                Debug.LogError("RenderTexture failed to create.");
                return;
            }

            // create ui material
            _uiMaterial = GameObject.Instantiate(Fugui.Settings.UIMaterial);
            _uiMaterial.SetTexture("_MainTex", RenderTexture);

            // create the fugui 3d context
            Rect rect = new Rect(Vector2.zero, new Vector2(_size.x, _size.y));
            _fuguiContext = Fugui.CreateUnityContext(rect, settings.ContextScale, settings.FontScale);
            _fuguiContext.OnRender += RenderFuWindows;
            _fuguiContext.OnPrepareFrame += context_OnPrepareFrame;
            _fuguiContext.OnFramePrepared += _fuguiContext_OnFramePrepared;
            _fuguiContext.AutoUpdateMouse = false;
            _fuguiContext.AutoUpdateKeyboard = Window.IsInterractable;
            _fuguiContext.SetTargetTexture(RenderTexture);
            _fuguiContext.SetContainerScaleConfig(settings.ContainerScaleConfig, _size);

            // create panel game object
            createPanel();

            // instantiate inputs states
            _mouseState = new FuMouseState();
            _keyboardState = new FuKeyboardState(_fuguiContext.IO);

            // apply the theme to this context
            Fugui.Themes.SetTheme(Fugui.Themes.CurrentTheme);

            // register on theme change
            Fugui.Themes.OnThemeSet += ThemeManager_OnThemeSet;

            // set default position
            SetPosition(position.HasValue ? position.Value : Vector3.zero);
            SetRotation(rotation.HasValue ? rotation.Value : Quaternion.identity);

            // initialize the window
            window.InitializeOnContainer();
        }

        /// <summary>
        /// Initializes a new instance of the Fu3 DWindow Container class.
        /// </summary>
        /// <param name="window">The window value.</param>
        /// <param name="position">The position value.</param>
        /// <param name="rotation">The rotation value.</param>
        /// <param name="scaleConfig">The scale Config value.</param>
        /// <param name="scale3D">The scale3 D value.</param>
        [Obsolete("Use Fu3DWindowContainer(FuWindow, Fu3DWindowSettings, ...) to provide panel size and render resolution explicitly.")]
        public Fu3DWindowContainer(FuWindow window, Vector3? position = null, Quaternion? rotation = null, FuContainerScaleConfig? scaleConfig = null, float? scale3D = null)
            : this(window, getLegacySettings(scaleConfig, scale3D), position, rotation)
        {
            _useExplicitResolution = false;
            _explicitResolution = Vector2Int.zero;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Whenever a theme is set
        /// </summary>
        /// <param name="theme">setted theme</param>
        private void ThemeManager_OnThemeSet(FuTheme theme)
        {
            createPanel();
            updateResizeHandleVisualStates();
        }

        /// <summary>
        /// Enable or disable runtime resizing through 3D raycast handles.
        /// </summary>
        /// <param name="resizable">True to expose runtime resize handles.</param>
        public void SetRuntimeResizable(bool resizable)
        {
            if (IsClosed)
                return;

            _runtimeResizable = resizable;

            if (!_runtimeResizable)
            {
                cancelRuntimeResize();
                setResizeHandlesVisible(false);
                return;
            }

            ensureResizeHandles();
            updateResizeHandleTransforms();
            updateResizeHandleVisualStates();
        }

        /// <summary>
        /// Blocks runtime resize input while another external handle owns the pointer.
        /// </summary>
        /// <param name="blocked">Whether runtime resize input should be blocked.</param>
        public void SetRuntimeResizeBlocked(bool blocked)
        {
            if (_runtimeResizeBlocked == blocked)
            {
                return;
            }

            _runtimeResizeBlocked = blocked;
            if (_runtimeResizeBlocked)
            {
                cancelRuntimeResize();
                _hoveredResizeHandleIndex = -1;
                setResizeHandlesVisible(false);
            }
        }

        /// <summary>
        /// Set the world-space size factor used to convert panel local size to render pixels.
        /// Smaller values keep more render pixels when the 3D panel is physically small.
        /// </summary>
        /// <param name="scale3D">World-space units per 1000 logical pixels.</param>
        public void Set3DScale(float scale3D)
        {
            scale3D = Mathf.Max(0.0001f, scale3D);
            if (Mathf.Abs(_windows3DScale - scale3D) < 0.0001f)
            {
                return;
            }

            _windows3DScale = scale3D;
            if (!IsClosed)
            {
                SetLocalSize(_localSize);
            }
        }

        /// <summary>
        /// Configure how this 3D container scales its context from the panel size.
        /// </summary>
        /// <param name="config">Scale configuration.</param>
        public void SetContainerScaleConfig(FuContainerScaleConfig config)
        {
            if (IsClosed || _fuguiContext == null)
                return;

            config.Sanitize();
            if (_localSize.x <= 0f || _localSize.y <= 0f)
            {
                _localSize = getCurrentLocalSize();
            }

            _fuguiContext.SetContainerScaleConfig(config, getScaleSourceSize(_localSize, config));
            SetLocalSize(_localSize);
        }

        /// <summary>
        /// Apply explicit 3D window settings. Resolution drives the render texture and ImGui context size;
        /// panel size drives only the world-space mesh size.
        /// </summary>
        /// <param name="settings">3D window settings.</param>
        public void Set3DWindowSettings(Fu3DWindowSettings settings)
        {
            if (IsClosed || _fuguiContext == null)
                return;

            settings.Sanitize();
            bool panelDepthChanged = Mathf.Abs(_panelDepth - settings.PanelDepth) > 0.0001f;
            bool panelCurveChanged = Mathf.Abs(_panelCurve - settings.PanelCurve) > 0.0001f;
            bool panelRoundingChanged = Mathf.Abs(_panelRounding - settings.PanelRounding) > 0.0001f;
            Vector2 previousLocalSize = _localSize;
            Vector2Int previousResolution = _explicitResolution;
            _useExplicitResolution = true;
            applyResolutionSettings(settings, false);
            _windows3DScale = getLegacyScaleFromSettings(settings);
            _fuguiContext.SetContainerScaleConfig(settings.ContainerScaleConfig, _explicitResolution);
            SetLocalSize(settings.PanelSize);
            if ((panelDepthChanged || panelCurveChanged || panelRoundingChanged) &&
                previousResolution == _explicitResolution &&
                (previousLocalSize - settings.PanelSize).sqrMagnitude <= 0.00000001f)
            {
                createPanel();
                updateResizeHandleTransforms();
            }
        }

        /// <summary>
        /// Change only the render texture and ImGui context resolution while preserving the current panel size.
        /// </summary>
        /// <param name="resolution">New render resolution.</param>
        public void SetRenderResolution(Vector2Int resolution)
        {
            Fu3DWindowSettings settings = Fu3DWindowSettings.FixedResolution(
                getCurrentLocalSize(),
                resolution,
                _fuguiContext != null ? _fuguiContext.ContainerScaleConfig.BaseScale : 1f,
                _fuguiContext != null ? _fuguiContext.ContainerScaleConfig.BaseFontScale : 1f,
                _panelDepth,
                _panelCurve,
                _panelRounding);
            if (_fuguiContext != null)
            {
                settings.ContainerScaleConfig = _fuguiContext.ContainerScaleConfig;
            }
            Set3DWindowSettings(settings);
        }

        /// <summary>
        /// Change the generated panel extrusion depth without changing the window content.
        /// </summary>
        /// <param name="depth">Depth of the generated panel extrusion.</param>
        public void SetPanelDepth(float depth)
        {
            depth = Mathf.Max(0.0001f, depth);
            if (Mathf.Abs(_panelDepth - depth) < 0.0001f)
            {
                return;
            }

            _panelDepth = depth;
            if (!IsClosed)
            {
                createPanel();
                updateResizeHandleTransforms();
            }
        }

        /// <summary>
        /// Change the horizontal curve angle of the generated panel without changing the window content.
        /// </summary>
        /// <param name="curve">Horizontal curve angle in degrees.</param>
        public void SetPanelCurve(float curve)
        {
            curve = Mathf.Clamp(curve, 0f, 359.9f);
            if (Mathf.Abs(_panelCurve - curve) < 0.0001f)
            {
                return;
            }

            _panelCurve = curve;
            if (!IsClosed)
            {
                createPanel();
                updateResizeHandleTransforms();
            }
        }

        /// <summary>
        /// Change the generated panel corner radius without changing the window content.
        /// </summary>
        /// <param name="rounding">Panel corner radius in world units.</param>
        public void SetPanelRounding(float rounding)
        {
            rounding = Mathf.Max(0f, rounding);
            if (Mathf.Abs(_panelRounding - rounding) < 0.0001f)
            {
                return;
            }

            _panelRounding = rounding;
            if (!IsClosed)
            {
                createPanel();
                updateResizeHandleTransforms();
            }
        }

        /// <summary>
        /// Create the render texture used by the 3D UI panel.
        /// </summary>
        /// <param name="size">Pixel size of the render target.</param>
        /// <returns>The created render texture.</returns>
        private static RenderTexture createRenderTexture(Vector2Int size)
        {
            size = sanitizeSize(size);
            int aaSamples = getRenderTextureAntiAliasing();

            if (Fugui.Settings != null && Fugui.Settings.Pool3DWindowRenderTextures)
            {
                string key = getRenderTexturePoolKey(size, aaSamples);
                if (_renderTexturePool.TryGetValue(key, out Stack<RenderTexture> pooledTextures))
                {
                    while (pooledTextures.Count > 0)
                    {
                        RenderTexture pooledTexture = pooledTextures.Pop();
                        if (pooledTexture == null)
                        {
                            continue;
                        }

                        if (!pooledTexture.IsCreated())
                        {
                            pooledTexture.Create();
                        }

                        return pooledTexture;
                    }
                }
            }

            RenderTexture renderTexture = new RenderTexture(size.x, size.y, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB);
            renderTexture.antiAliasing = aaSamples;
            renderTexture.useDynamicScale = false;
            renderTexture.Create();

            return renderTexture;
        }

        /// <summary>
        /// Prewarms one pooled 3D window render texture of the requested size.
        /// </summary>
        /// <param name="size">Render target size to prewarm.</param>
        internal static void PrewarmRenderTexture(Vector2Int size)
        {
            if (Fugui.Settings == null || !Fugui.Settings.Pool3DWindowRenderTextures)
            {
                return;
            }

            RenderTexture renderTexture = createRenderTexture(size);
            ReleaseRenderTexture(renderTexture);
        }

        /// <summary>
        /// Releases a render texture back to the 3D window pool when enabled.
        /// </summary>
        /// <param name="renderTexture">Render texture to release.</param>
        private static void ReleaseRenderTexture(RenderTexture renderTexture)
        {
            if (renderTexture == null)
            {
                return;
            }

            int aaSamples = normalizeAntiAliasing(renderTexture.antiAliasing);
            Vector2Int size = new Vector2Int(renderTexture.width, renderTexture.height);
            if (Fugui.Settings != null && Fugui.Settings.Pool3DWindowRenderTextures)
            {
                string key = getRenderTexturePoolKey(size, aaSamples);
                if (!_renderTexturePool.TryGetValue(key, out Stack<RenderTexture> pooledTextures))
                {
                    pooledTextures = new Stack<RenderTexture>();
                    _renderTexturePool[key] = pooledTextures;
                }

                if (pooledTextures.Count < MaxPooledRenderTexturesPerKey)
                {
                    renderTexture.Release();
                    pooledTextures.Push(renderTexture);
                    return;
                }
            }

            renderTexture.Release();
            UnityEngine.Object.Destroy(renderTexture);
        }

        /// <summary>
        /// Gets the MSAA sample count used for 3D window render textures.
        /// </summary>
        /// <returns>Normalized MSAA sample count.</returns>
        private static int getRenderTextureAntiAliasing()
        {
            int aaSamples = Fugui.Settings != null
                ? Fugui.Settings.Windows3DRenderTextureAntiAliasing
                : QualitySettings.antiAliasing;

            return normalizeAntiAliasing(aaSamples);
        }

        /// <summary>
        /// Normalizes MSAA samples to values Unity accepts.
        /// </summary>
        /// <param name="aaSamples">Requested AA sample count.</param>
        /// <returns>1, 2, 4 or 8.</returns>
        private static int normalizeAntiAliasing(int aaSamples)
        {
            if (aaSamples <= 1)
            {
                return 1;
            }

            if (aaSamples <= 2)
            {
                return 2;
            }

            if (aaSamples <= 4)
            {
                return 4;
            }

            return 8;
        }

        /// <summary>
        /// Gets the render texture pool key for a given size and AA sample count.
        /// </summary>
        /// <param name="size">Texture size.</param>
        /// <param name="aaSamples">MSAA sample count.</param>
        /// <returns>Pool key.</returns>
        private static string getRenderTexturePoolKey(Vector2Int size, int aaSamples)
        {
            return $"{size.x}x{size.y}:aa{aaSamples}:R8G8B8A8_SRGB";
        }

        /// <summary>
        /// Clamp render target sizes to values Unity and ImGui can use.
        /// </summary>
        /// <param name="size">Requested size.</param>
        /// <returns>Sanitized size.</returns>
        private static Vector2Int sanitizeSize(Vector2Int size)
        {
            return new Vector2Int(
                Mathf.Max(1, size.x),
                Mathf.Max(1, size.y)
            );
        }

        /// <summary>
        /// Returns the get default3 dscale config result.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        private static FuContainerScaleConfig getDefault3DScaleConfig()
        {
            float baseScale = Fugui.Settings != null ? Fugui.Settings.Windows3DSuperSampling : 1f;
            float baseFontScale = Fugui.Settings != null ? Fugui.Settings.Windows3DFontScale : 1f;
            return FuContainerScaleConfig.Disabled(baseScale, baseFontScale);
        }

        /// <summary>
        /// Returns the get default3 dscale result.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        private static float getDefault3DScale()
        {
            return Fugui.Settings != null ? Mathf.Max(0.0001f, Fugui.Settings.Windows3DScale) : 10f;
        }

        /// <summary>
        /// Returns the get legacy settings result.
        /// </summary>
        /// <param name="scaleConfig">The scale Config value.</param>
        /// <param name="scale3D">The scale3 D value.</param>
        /// <returns>The result of the operation.</returns>
        private static Fu3DWindowSettings getLegacySettings(FuContainerScaleConfig? scaleConfig, float? scale3D)
        {
            float contextScale = Fugui.Settings != null ? Fugui.Settings.Windows3DSuperSampling : 1f;
            float fontScale = Fugui.Settings != null ? Fugui.Settings.Windows3DFontScale : 1f;
            float windows3DScale = scale3D.HasValue ? Mathf.Max(0.0001f, scale3D.Value) : getDefault3DScale();
            Vector2Int resolution = new Vector2Int(512, 512);
            Vector2 panelSize = new Vector2(
                resolution.x / contextScale * windows3DScale / 1000f,
                resolution.y / contextScale * windows3DScale / 1000f);

            float panelDepth = Fugui.Settings != null ? Mathf.Max(0.0001f, Fugui.Settings.UIPanelWidth) : 0.01f;
            Fu3DWindowSettings settings = Fu3DWindowSettings.FixedResolution(panelSize, resolution, contextScale, fontScale, panelDepth);
            if (scaleConfig.HasValue)
            {
                settings.ContainerScaleConfig = scaleConfig.Value;
            }
            return settings;
        }

        /// <summary>
        /// Runs the apply resolution settings workflow.
        /// </summary>
        /// <param name="settings">The settings value.</param>
        /// <param name="applyPanelSize">The apply Panel Size value.</param>
        private void applyResolutionSettings(Fu3DWindowSettings settings, bool applyPanelSize)
        {
            settings.Sanitize();
            _scaleResolutionWithPanel = settings.ScaleResolutionWithPanel;
            _matchResolutionToPanelAspect = settings.MatchResolutionToPanelAspect;
            _referenceLocalSize = settings.ReferencePanelSize;
            _referenceResolution = settings.ReferenceResolution;
            _minResolution = settings.MinResolution;
            _maxResolution = settings.MaxResolution;
            _panelDepth = settings.PanelDepth;
            _panelCurve = settings.PanelCurve;
            _panelRounding = settings.PanelRounding;
            if (applyPanelSize)
            {
                _localSize = settings.PanelSize;
            }
            _explicitResolution = getExplicitResolutionForLocalSize(settings.PanelSize);
        }

        /// <summary>
        /// Returns the get legacy scale from settings result.
        /// </summary>
        /// <param name="settings">The settings value.</param>
        /// <returns>The result of the operation.</returns>
        private static float getLegacyScaleFromSettings(Fu3DWindowSettings settings)
        {
            if (settings.Resolution.x <= 0)
            {
                return getDefault3DScale();
            }

            return Mathf.Max(0.0001f, settings.PanelSize.x * settings.ContextScale * 1000f / settings.Resolution.x);
        }

        /// <summary>
        /// Apply a new render size to the render texture, Fugui context, material and panel mesh.
        /// </summary>
        /// <param name="size">Target render size in pixels.</param>
        private void setRenderSize(Vector2Int size)
        {
            size = sanitizeSize(size);

            bool sizeChanged = _size != size;
            bool textureInvalid = RenderTexture == null ||
                                  RenderTexture.width != size.x ||
                                  RenderTexture.height != size.y ||
                                  !RenderTexture.IsCreated();

            _size = size;

            if (textureInvalid)
            {
                RenderTexture oldTexture = RenderTexture;
                RenderTexture = createRenderTexture(_size);
                _uiMaterial?.SetTexture("_MainTex", RenderTexture);

                if (_fuguiContext != null)
                {
                    _fuguiContext.SetTargetTexture(RenderTexture);
                }

                if (oldTexture != null)
                {
                    ReleaseRenderTexture(oldTexture);
                }
            }

            if (_fuguiContext != null)
            {
                _fuguiContext.SetPixelRect(new Rect(Vector2.zero, new Vector2(_size.x, _size.y)));
                _fuguiContext.SetTargetTexture(RenderTexture);
            }

            if (sizeChanged || _panelGameObject == null)
            {
                createPanel();
            }
        }

        /// <summary>
        /// Create the 3D UI Panel GameObject of this container
        /// </summary>
        private void createPanel()
        {
            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            bool createdPanelObject = false;
            if (_panelGameObject != null)
            {
                position = _panelGameObject.transform.position;
                rotation = _panelGameObject.transform.rotation;
            }
            else
            {
                _resizeHandles = null;
                _resizeHandlesVisible = false;
                _panelGameObject = new GameObject(ID + "_Panel");
                createdPanelObject = true;
            }

            if (_panelMesh == null)
            {
                _panelMesh = _panelGameObject.GetComponent<FuPanelMesh>();
                if (_panelMesh == null)
                {
                    _panelMesh = _panelGameObject.AddComponent<FuPanelMesh>();
                }
            }

            _panelMesh.Window = Window;
            float meshScale = getMeshScale();
            Vector2 meshSize = getMeshSize(meshScale);
            float round = getPanelMeshRounding(meshScale);

            if (_panelCollider == null)
            {
                _panelCollider = _panelGameObject.GetComponent<MeshCollider>();
                if (_panelCollider == null)
                {
                    _panelCollider = _panelGameObject.AddComponent<MeshCollider>();
                }
            }

            bool meshChanged = createdPanelObject ||
                               _panelCollider.sharedMesh == null ||
                               (_lastPanelMeshSize - meshSize).sqrMagnitude > 0.00000001f ||
                               Mathf.Abs(_lastPanelMeshScale - meshScale) > 0.0001f ||
                               Mathf.Abs(_lastPanelRound - round) > 0.0001f ||
                               Mathf.Abs(_lastPanelDepth - _panelDepth) > 0.0001f ||
                               Mathf.Abs(_lastPanelCurve - _panelCurve) > 0.0001f;

            if (meshChanged)
            {
                Mesh mesh = _panelMesh.CreateMesh(meshSize.x, meshSize.y, meshScale, round, round, round, round, _panelDepth, 32, _uiMaterial, Fugui.Settings.UIPanelMaterial, _panelCurve);
                _panelCollider.sharedMesh = null;
                _panelCollider.sharedMesh = mesh;
                _lastPanelMeshSize = meshSize;
                _lastPanelMeshScale = meshScale;
                _lastPanelRound = round;
                _lastPanelDepth = _panelDepth;
                _lastPanelCurve = _panelCurve;
            }
            else
            {
                _panelMesh.UpdateMaterials(_uiMaterial, Fugui.Settings.UIPanelMaterial, meshSize.x, meshSize.y, meshScale);
            }

            int layer = 0;
            if (Fugui.Settings != null && Fugui.Settings.UILayer.value > 0)
            {
                layer = Mathf.RoundToInt(Mathf.Log(Fugui.Settings.UILayer.value, 2));
            }

            _panelGameObject.layer = layer;
            foreach (Transform child in _panelGameObject.transform)
            {
                child.gameObject.layer = layer;
            }
            _panelGameObject.transform.position = position;
            _panelGameObject.transform.rotation = rotation;

            if (_runtimeResizable)
            {
                ensureResizeHandles();
            }
        }

        /// <summary>
        /// Set the world position of the UI Panel
        /// </summary>
        /// <param name="position">Position of the UI Panel</param>
        public void SetPosition(Vector3 position)
        {
            if (IsClosed || _panelGameObject == null)
                return;

            _panelGameObject.transform.position = position;
        }

        /// <summary>
        /// Set the world rotation of the UI Panel
        /// </summary>
        /// <param name="rotation">Rotation of the UI Panel</param>
        public void SetRotation(Quaternion rotation)
        {
            if (IsClosed || _panelGameObject == null)
                return;

            _panelGameObject.transform.rotation = rotation;
        }

        /// <summary>
        /// Returns the update runtime resize result.
        /// </summary>
        /// <param name="panelInputState">The panel Input State value.</param>
        /// <returns>The result of the operation.</returns>
        private bool updateRuntimeResize(InputState panelInputState)
        {
            if (_runtimeResizeBlocked || !_runtimeResizable || _panelGameObject == null || Window == null || !Window.IsInterractable)
            {
                cancelRuntimeResize();
                _hoveredResizeHandleIndex = -1;
                setResizeHandlesVisible(false);
                return false;
            }

            if (_activeResizeHandleIndex != -1)
            {
                setResizeHandlesVisible(true);
                continueRuntimeResize();
                Window.ForceDraw(2);
                return true;
            }

            bool handleHovered = tryGetHoveredResizeHandle(out int hoveredHandleIndex, out InputState handleInputState);
            _hoveredResizeHandleIndex = handleHovered ? hoveredHandleIndex : -1;
            if (handleHovered && handleInputState.MouseButtons[0])
            {
                startRuntimeResize(hoveredHandleIndex, handleInputState.RaycasterID);
            }

            bool handlesShouldBeVisible = panelInputState.Hovered || handleHovered || _activeResizeHandleIndex != -1;

            setResizeHandlesVisible(handlesShouldBeVisible);

            if (handlesShouldBeVisible)
            {
                Window.ForceDraw(2);
            }

            return handleHovered || _activeResizeHandleIndex != -1;
        }

        /// <summary>
        /// Returns the try get hovered resize handle result.
        /// </summary>
        /// <param name="handleIndex">The handle Index value.</param>
        /// <param name="inputState">The input State value.</param>
        /// <returns>The result of the operation.</returns>
        private bool tryGetHoveredResizeHandle(out int handleIndex, out InputState inputState)
        {
            handleIndex = -1;
            inputState = default;

            if (_resizeHandles == null)
            {
                return false;
            }

            for (int i = 0; i < _resizeHandles.Length; i++)
            {
                GameObject handle = _resizeHandles[i];
                if (handle == null || !handle.activeSelf)
                {
                    continue;
                }

                InputState handleInputState = FuRaycasting.GetInputState(getResizeHandleID(i), handle);
                if (!handleInputState.Hovered)
                {
                    continue;
                }

                handleIndex = i;
                inputState = handleInputState;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Runs the start runtime resize workflow.
        /// </summary>
        /// <param name="handleIndex">The handle Index value.</param>
        /// <param name="raycasterID">The raycaster ID value.</param>
        private void startRuntimeResize(int handleIndex, string raycasterID)
        {
            if (string.IsNullOrEmpty(raycasterID) ||
                !FuRaycasting.TryGetRaycaster(raycasterID, out FuRaycaster raycaster) ||
                !tryGetRayLocalPoint(raycaster, _panelGameObject.transform.position, _panelGameObject.transform.rotation, out Vector2 hitLocal))
            {
                return;
            }

            _activeResizeHandleIndex = handleIndex;
            _hoveredResizeHandleIndex = handleIndex;
            _activeResizeRaycasterID = raycasterID;
            _resizeStartPanelPosition = _panelGameObject.transform.position;
            _resizeStartPanelRotation = _panelGameObject.transform.rotation;
            _resizeStartLocalSize = getCurrentLocalSize();
            _resizeStartHitLocal = hitLocal;
            setResizeHandlesVisible(true);
            updateResizeHandleVisualStates();
            Window?.ForceDraw(10);
        }

        /// <summary>
        /// Runs the continue runtime resize workflow.
        /// </summary>
        private void continueRuntimeResize()
        {
            if (_activeResizeHandleIndex == -1)
            {
                return;
            }

            if (!FuRaycasting.TryGetRaycaster(_activeResizeRaycasterID, out FuRaycaster raycaster) ||
                !raycaster.MouseButton0())
            {
                finishRuntimeResize();
                return;
            }

            if (!tryGetRayLocalPoint(raycaster, _resizeStartPanelPosition, _resizeStartPanelRotation, out Vector2 currentHitLocal))
            {
                return;
            }

            applyRuntimeResize(currentHitLocal);
        }

        /// <summary>
        /// Runs the apply runtime resize workflow.
        /// </summary>
        /// <param name="currentHitLocal">The current Hit Local value.</param>
        private void applyRuntimeResize(Vector2 currentHitLocal)
        {
            Vector2 delta = currentHitLocal - _resizeStartHitLocal;

            float left = -_resizeStartLocalSize.x * 0.5f;
            float right = _resizeStartLocalSize.x * 0.5f;
            float bottom = 0f;
            float top = _resizeStartLocalSize.y;

            bool resizeLeft = isLeftHandle(_activeResizeHandleIndex);
            bool resizeBottom = isBottomHandle(_activeResizeHandleIndex);

            if (resizeLeft)
            {
                left += delta.x;
            }
            else
            {
                right += delta.x;
            }

            if (resizeBottom)
            {
                bottom += delta.y;
            }
            else
            {
                top += delta.y;
            }

            if (right - left < RuntimeResizeMinSize)
            {
                if (resizeLeft)
                {
                    left = right - RuntimeResizeMinSize;
                }
                else
                {
                    right = left + RuntimeResizeMinSize;
                }
            }

            if (top - bottom < RuntimeResizeMinSize)
            {
                if (resizeBottom)
                {
                    bottom = top - RuntimeResizeMinSize;
                }
                else
                {
                    top = bottom + RuntimeResizeMinSize;
                }
            }

            Vector2 newLocalSize = new Vector2(right - left, top - bottom);
            Vector3 newPanelPosition = _resizeStartPanelPosition + _resizeStartPanelRotation * new Vector3((left + right) * 0.5f, bottom, 0f);

            SetPosition(newPanelPosition);
            SetRotation(_resizeStartPanelRotation);
            SetLocalSize(newLocalSize, false);
            setResizeHandlesVisible(true);
            OnRuntimeResized?.Invoke(newPanelPosition, newLocalSize);
            Window?.ForceDraw(2);
        }

        /// <summary>
        /// Runs the finish runtime resize workflow.
        /// </summary>
        private void finishRuntimeResize()
        {
            bool wasResizing = _activeResizeHandleIndex != -1;
            _activeResizeHandleIndex = -1;
            _hoveredResizeHandleIndex = -1;
            _activeResizeRaycasterID = null;
            if (wasResizing)
            {
                Window?.Fire_OnResized();
            }
            setResizeHandlesVisible(false);
            Window?.ForceDraw(2);
        }

        /// <summary>
        /// Runs the cancel runtime resize workflow.
        /// </summary>
        private void cancelRuntimeResize()
        {
            _activeResizeHandleIndex = -1;
            _hoveredResizeHandleIndex = -1;
            _activeResizeRaycasterID = null;
            updateResizeHandleVisualStates();
        }

        /// <summary>
        /// Returns the try get ray local point result.
        /// </summary>
        /// <param name="raycaster">The raycaster value.</param>
        /// <param name="planePosition">The plane Position value.</param>
        /// <param name="planeRotation">The plane Rotation value.</param>
        /// <param name="localPoint">The local Point value.</param>
        /// <returns>The result of the operation.</returns>
        private bool tryGetRayLocalPoint(FuRaycaster raycaster, Vector3 planePosition, Quaternion planeRotation, out Vector2 localPoint)
        {
            localPoint = Vector2.zero;

            if (raycaster == null)
            {
                return false;
            }

            Ray ray = raycaster.GetRay();
            Vector3 normal = planeRotation * Vector3.forward;
            float denominator = Vector3.Dot(normal, ray.direction);

            if (Mathf.Abs(denominator) < 0.0001f)
            {
                return false;
            }

            float distance = Vector3.Dot(planePosition - ray.origin, normal) / denominator;
            if (distance < 0f)
            {
                return false;
            }

            Vector3 hitPoint = ray.GetPoint(distance);
            Vector3 localPoint3D = Quaternion.Inverse(planeRotation) * (hitPoint - planePosition);
            localPoint = new Vector2(localPoint3D.x, localPoint3D.y);
            return true;
        }

        /// <summary>
        /// Returns the is left handle result.
        /// </summary>
        /// <param name="handleIndex">The handle Index value.</param>
        /// <returns>The result of the operation.</returns>
        private bool isLeftHandle(int handleIndex)
        {
            return handleIndex == 0 || handleIndex == 2;
        }

        /// <summary>
        /// Returns the is bottom handle result.
        /// </summary>
        /// <param name="handleIndex">The handle Index value.</param>
        /// <returns>The result of the operation.</returns>
        private bool isBottomHandle(int handleIndex)
        {
            return handleIndex == 0 || handleIndex == 1;
        }

        /// <summary>
        /// Runs the ensure resize handles workflow.
        /// </summary>
        private void ensureResizeHandles()
        {
            if (!_runtimeResizable || _panelGameObject == null)
            {
                return;
            }

            if (_resizeHandles == null || _resizeHandles.Length != 4)
            {
                _resizeHandles = new GameObject[4];
            }

            if (_resizeHandleMeshes == null || _resizeHandleMeshes.Length != 4)
            {
                _resizeHandleMeshes = new Mesh[4];
            }

            for (int i = 0; i < _resizeHandles.Length; i++)
            {
                if (_resizeHandles[i] == null)
                {
                    GameObject handle = new GameObject(getResizeHandleID(i));
                    handle.transform.SetParent(_panelGameObject.transform, false);
                    _resizeHandles[i] = handle;
                }

                configureResizeHandle(_resizeHandles[i], i);
            }

            updateResizeHandleTransforms();
        }

        /// <summary>
        /// Runs the update resize handle transforms workflow.
        /// </summary>
        private void updateResizeHandleTransforms()
        {
            if (!_runtimeResizable || _resizeHandles == null)
            {
                return;
            }

            Vector2 localSize = getCurrentLocalSize();
            float outerRadius = getResizeHandleOuterRadius(localSize);
            float thickness = getResizeHandleThickness(outerRadius);

            for (int i = 0; i < _resizeHandles.Length; i++)
            {
                GameObject handle = _resizeHandles[i];
                if (handle == null)
                {
                    continue;
                }

                handle.transform.localPosition = getResizeHandleLocalPosition(i, localSize);
                handle.transform.localScale = Vector3.one;
                handle.transform.localRotation = Quaternion.identity;
                handle.layer = _panelGameObject != null ? _panelGameObject.layer : handle.layer;
                updateResizeHandleMesh(i, outerRadius, thickness);
                updateResizeHandleCollider(handle, i, outerRadius);
            }

            updateResizeHandleVisualStates();
        }

        /// <summary>
        /// Updates renderer colors and collider states for resize handles.
        /// </summary>
        private void updateResizeHandleVisualStates()
        {
            if (_resizeHandles == null)
            {
                return;
            }

            for (int i = 0; i < _resizeHandles.Length; i++)
            {
                GameObject handle = _resizeHandles[i];
                if (handle == null)
                {
                    continue;
                }

                MeshRenderer renderer = handle.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.enabled = _resizeHandlesVisible;
                    setRendererColor(renderer, getResizeHandleColor(i));
                }

                Collider collider = handle.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.enabled = canResizeHandleInteract(i);
                }
            }
        }

        /// <summary>
        /// Returns whether one resize handle can receive raycast input.
        /// </summary>
        /// <param name="handleIndex">The handle index.</param>
        /// <returns>True when the handle can interact.</returns>
        private bool canResizeHandleInteract(int handleIndex)
        {
            if (!_runtimeResizable || _runtimeResizeBlocked)
            {
                return false;
            }

            return _activeResizeHandleIndex == -1 || _activeResizeHandleIndex == handleIndex;
        }

        /// <summary>
        /// Ensures one runtime resize handle has the expected components.
        /// </summary>
        /// <param name="handle">The handle object.</param>
        /// <param name="handleIndex">The handle index.</param>
        private void configureResizeHandle(GameObject handle, int handleIndex)
        {
            handle.name = getResizeHandleID(handleIndex);
            handle.layer = _panelGameObject != null ? _panelGameObject.layer : handle.layer;
            handle.transform.SetParent(_panelGameObject.transform, false);
            handle.transform.localRotation = Quaternion.identity;
            handle.transform.localScale = Vector3.one;
            handle.SetActive(_runtimeResizable);

            MeshFilter meshFilter = handle.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = handle.AddComponent<MeshFilter>();
            }

            if (_resizeHandleMeshes[handleIndex] == null)
            {
                _resizeHandleMeshes[handleIndex] = new Mesh
                {
                    name = getResizeHandleID(handleIndex) + "_Mesh"
                };
            }

            meshFilter.sharedMesh = _resizeHandleMeshes[handleIndex];

            MeshRenderer renderer = handle.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                renderer = handle.AddComponent<MeshRenderer>();
            }
            renderer.sharedMaterial = getResizeHandleMaterial();
            renderer.enabled = _resizeHandlesVisible;
            setRendererColor(renderer, getResizeHandleColor(handleIndex));

            BoxCollider collider = handle.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = handle.AddComponent<BoxCollider>();
            }
            collider.enabled = canResizeHandleInteract(handleIndex);
        }

        /// <summary>
        /// Updates one resize handle as an external rounded corner arc.
        /// </summary>
        /// <param name="handleIndex">The handle index.</param>
        /// <param name="outerRadius">Outer arc radius in local panel units.</param>
        /// <param name="thickness">Arc thickness in local panel units.</param>
        private void updateResizeHandleMesh(int handleIndex, float outerRadius, float thickness)
        {
            if (_resizeHandleMeshes == null || handleIndex < 0 || handleIndex >= _resizeHandleMeshes.Length)
            {
                return;
            }

            Mesh mesh = _resizeHandleMeshes[handleIndex];
            if (mesh == null)
            {
                return;
            }

            int segments = Mathf.Max(3, ResizeHandleArcSegments);
            Vector3[] vertices = new Vector3[(segments + 1) * 2];
            Vector3[] normals = new Vector3[vertices.Length];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[segments * 6];
            float innerRadius = Mathf.Max(0.0001f, outerRadius - thickness);
            float startAngle = getResizeHandleStartAngle(handleIndex);
            float endAngle = startAngle + Mathf.PI * 0.5f;

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float angle = Mathf.Lerp(startAngle, endAngle, t);
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                int innerIndex = i * 2;
                int outerIndex = innerIndex + 1;

                vertices[innerIndex] = new Vector3(direction.x * innerRadius, direction.y * innerRadius, 0f);
                vertices[outerIndex] = new Vector3(direction.x * outerRadius, direction.y * outerRadius, 0f);
                normals[innerIndex] = -Vector3.forward;
                normals[outerIndex] = -Vector3.forward;
                uvs[innerIndex] = new Vector2(t, 0f);
                uvs[outerIndex] = new Vector2(t, 1f);
            }

            int triangle = 0;
            for (int i = 0; i < segments; i++)
            {
                int inner0 = i * 2;
                int outer0 = inner0 + 1;
                int inner1 = inner0 + 2;
                int outer1 = inner0 + 3;

                triangles[triangle++] = inner0;
                triangles[triangle++] = inner1;
                triangles[triangle++] = outer0;
                triangles[triangle++] = outer0;
                triangles[triangle++] = inner1;
                triangles[triangle++] = outer1;
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
        }

        /// <summary>
        /// Updates the broad raycast area for one external resize handle.
        /// </summary>
        /// <param name="handle">The handle object.</param>
        /// <param name="handleIndex">The handle index.</param>
        /// <param name="outerRadius">Outer arc radius in local panel units.</param>
        private void updateResizeHandleCollider(GameObject handle, int handleIndex, float outerRadius)
        {
            BoxCollider collider = handle.GetComponent<BoxCollider>();
            if (collider == null)
            {
                return;
            }

            Vector2 direction = getResizeHandleOutsideDirection(handleIndex);
            float size = outerRadius;
            collider.center = new Vector3(direction.x * size * 0.5f, direction.y * size * 0.5f, 0f);
            collider.size = new Vector3(size, size, Mathf.Max(0.01f, _panelDepth + 0.04f));
        }

        /// <summary>
        /// Returns the outer radius of the runtime resize corner arcs.
        /// </summary>
        /// <param name="localSize">Current panel local size.</param>
        /// <returns>Outer arc radius in local panel units.</returns>
        private float getResizeHandleOuterRadius(Vector2 localSize)
        {
            return Mathf.Clamp(
                Mathf.Min(localSize.x, localSize.y) * ResizeHandleOuterRadiusRatio,
                ResizeHandleOuterRadiusMin,
                ResizeHandleOuterRadiusMax);
        }

        /// <summary>
        /// Returns the visual thickness of a runtime resize handle.
        /// </summary>
        /// <param name="outerRadius">Outer arc radius in local panel units.</param>
        /// <returns>Arc thickness in local panel units.</returns>
        private float getResizeHandleThickness(float outerRadius)
        {
            return Mathf.Clamp(
                outerRadius * ResizeHandleThicknessRatio,
                ResizeHandleThicknessMin,
                ResizeHandleThicknessMax);
        }

        /// <summary>
        /// Returns the first angle for a resize corner arc.
        /// </summary>
        /// <param name="handleIndex">The handle index.</param>
        /// <returns>Angle in radians.</returns>
        private float getResizeHandleStartAngle(int handleIndex)
        {
            if (handleIndex == 1)
            {
                return -Mathf.PI * 0.5f;
            }

            if (handleIndex == 2)
            {
                return Mathf.PI * 0.5f;
            }

            if (handleIndex == 3)
            {
                return 0f;
            }

            return Mathf.PI;
        }

        /// <summary>
        /// Returns the outside quadrant direction for one handle.
        /// </summary>
        /// <param name="handleIndex">The handle index.</param>
        /// <returns>Outside quadrant direction.</returns>
        private Vector2 getResizeHandleOutsideDirection(int handleIndex)
        {
            return new Vector2(
                isLeftHandle(handleIndex) ? -1f : 1f,
                isBottomHandle(handleIndex) ? -1f : 1f);
        }

        /// <summary>
        /// Returns the get current local size result.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        private Vector2 getCurrentLocalSize()
        {
            if (_useExplicitResolution)
            {
                return _localSize;
            }

            float contextScale = Context != null && Context.Scale > 0f ? Context.Scale : 1f;
            float panelScale = _windows3DScale / 1000f;
            return new Vector2(_size.x / contextScale * panelScale, _size.y / contextScale * panelScale);
        }

        /// <summary>
        /// Returns the get mesh scale result.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        private float getMeshScale()
        {
            if (!_useExplicitResolution)
            {
                return _windows3DScale / 1000f;
            }

            float contextScale = Context != null && Context.Scale > 0f ? Context.Scale : 1f;
            float logicalWidth = Mathf.Max(1f, _size.x / contextScale);
            float logicalHeight = Mathf.Max(1f, _size.y / contextScale);
            float xScale = _localSize.x / logicalWidth;
            float yScale = _localSize.y / logicalHeight;
            return Mathf.Max(0.0001f, Mathf.Min(xScale, yScale));
        }

        /// <summary>
        /// Convert the configured world-space rounding radius to the mesh generator unit space.
        /// </summary>
        /// <param name="meshScale">World units represented by one mesh unit.</param>
        /// <returns>Panel corner radius in mesh generator units.</returns>
        private float getPanelMeshRounding(float meshScale)
        {
            if (_panelRounding <= 0f)
            {
                return 0f;
            }

            return _panelRounding / Mathf.Max(0.0001f, meshScale);
        }

        /// <summary>
        /// Returns the get mesh size result.
        /// </summary>
        /// <param name="meshScale">The mesh Scale value.</param>
        /// <returns>The result of the operation.</returns>
        private Vector2 getMeshSize(float meshScale)
        {
            if (_useExplicitResolution)
            {
                return _localSize / Mathf.Max(0.0001f, meshScale);
            }

            float contextScale = Context != null && Context.Scale > 0f ? Context.Scale : 1f;
            return new Vector2(_size.x / contextScale, _size.y / contextScale);
        }

        /// <summary>
        /// Returns the get scale source size result.
        /// </summary>
        /// <param name="localSize">The local Size value.</param>
        /// <param name="config">The config value.</param>
        /// <returns>The result of the operation.</returns>
        private Vector2Int getScaleSourceSize(Vector2 localSize, FuContainerScaleConfig config)
        {
            if (_useExplicitResolution)
            {
                return getExplicitResolutionForLocalSize(localSize);
            }

            return getRenderSizeForLocalSize(localSize, config.BaseScale);
        }

        /// <summary>
        /// Returns the get render size for local size result.
        /// </summary>
        /// <param name="localSize">The local Size value.</param>
        /// <param name="contextScale">The context Scale value.</param>
        /// <returns>The result of the operation.</returns>
        private Vector2Int getRenderSizeForLocalSize(Vector2 localSize, float contextScale)
        {
            if (_useExplicitResolution)
            {
                return getExplicitResolutionForLocalSize(localSize);
            }

            float inversePanelScale = 1000f / Mathf.Max(0.0001f, _windows3DScale);

            return new Vector2Int(
                Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(localSize.x) * contextScale * inversePanelScale)),
                Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(localSize.y) * contextScale * inversePanelScale))
            );
        }

        /// <summary>
        /// Returns the get explicit resolution for local size result.
        /// </summary>
        /// <param name="localSize">The local Size value.</param>
        /// <returns>The result of the operation.</returns>
        private Vector2Int getExplicitResolutionForLocalSize(Vector2 localSize)
        {
            if (!_scaleResolutionWithPanel)
            {
                return _matchResolutionToPanelAspect
                    ? getAspectMatchedResolutionForLocalSize(localSize)
                    : sanitizeSize(_referenceResolution);
            }

            Vector2 panelSize = new Vector2(
                Mathf.Max(0.0001f, Mathf.Abs(localSize.x)),
                Mathf.Max(0.0001f, Mathf.Abs(localSize.y)));
            Vector2 referencePanelSize = new Vector2(
                Mathf.Max(0.0001f, Mathf.Abs(_referenceLocalSize.x)),
                Mathf.Max(0.0001f, Mathf.Abs(_referenceLocalSize.y)));

            Vector2 scale = new Vector2(
                panelSize.x / referencePanelSize.x,
                panelSize.y / referencePanelSize.y);

            Vector2Int resolution = new Vector2Int(
                Mathf.RoundToInt(_referenceResolution.x * scale.x),
                Mathf.RoundToInt(_referenceResolution.y * scale.y));

            Vector2Int minResolution = sanitizeSize(_minResolution);
            Vector2Int maxResolution = _maxResolution.x > 0 && _maxResolution.y > 0
                ? _maxResolution
                : new Vector2Int(Mathf.Max(1, SystemInfo.maxTextureSize), Mathf.Max(1, SystemInfo.maxTextureSize));

            return new Vector2Int(
                Mathf.Clamp(resolution.x, minResolution.x, Mathf.Max(minResolution.x, maxResolution.x)),
                Mathf.Clamp(resolution.y, minResolution.y, Mathf.Max(minResolution.y, maxResolution.y)));
        }

        /// <summary>
        /// Returns the get aspect matched resolution for local size result.
        /// </summary>
        /// <param name="localSize">The local Size value.</param>
        /// <returns>The result of the operation.</returns>
        private Vector2Int getAspectMatchedResolutionForLocalSize(Vector2 localSize)
        {
            Vector2 panelSize = new Vector2(
                Mathf.Max(0.0001f, Mathf.Abs(localSize.x)),
                Mathf.Max(0.0001f, Mathf.Abs(localSize.y)));
            Vector2 referencePanelSize = new Vector2(
                Mathf.Max(0.0001f, Mathf.Abs(_referenceLocalSize.x)),
                Mathf.Max(0.0001f, Mathf.Abs(_referenceLocalSize.y)));

            float panelAspect = panelSize.x / panelSize.y;
            float referencePanelAspect = referencePanelSize.x / referencePanelSize.y;
            float referenceResolutionAspect = _referenceResolution.x / (float)Mathf.Max(1, _referenceResolution.y);
            float targetAspect = Mathf.Max(0.0001f, referenceResolutionAspect * (panelAspect / referencePanelAspect));
            float referenceArea = Mathf.Max(1f, _referenceResolution.x * _referenceResolution.y);

            Vector2Int resolution = new Vector2Int(
                Mathf.Max(1, Mathf.RoundToInt(Mathf.Sqrt(referenceArea * targetAspect))),
                Mathf.Max(1, Mathf.RoundToInt(Mathf.Sqrt(referenceArea / targetAspect))));

            Vector2Int minResolution = sanitizeSize(_minResolution);
            Vector2Int maxResolution = _maxResolution.x > 0 && _maxResolution.y > 0
                ? _maxResolution
                : new Vector2Int(Mathf.Max(1, SystemInfo.maxTextureSize), Mathf.Max(1, SystemInfo.maxTextureSize));

            return new Vector2Int(
                Mathf.Clamp(resolution.x, minResolution.x, Mathf.Max(minResolution.x, maxResolution.x)),
                Mathf.Clamp(resolution.y, minResolution.y, Mathf.Max(minResolution.y, maxResolution.y)));
        }

        /// <summary>
        /// Returns the get resize handle local position result.
        /// </summary>
        /// <param name="handleIndex">The handle Index value.</param>
        /// <param name="localSize">The local Size value.</param>
        /// <returns>The result of the operation.</returns>
        private Vector3 getResizeHandleLocalPosition(int handleIndex, Vector2 localSize)
        {
            float x = isLeftHandle(handleIndex) ? -localSize.x * 0.5f : localSize.x * 0.5f;
            float y = isBottomHandle(handleIndex) ? 0f : localSize.y;
            if (_panelMesh != null)
            {
                return _panelMesh.GetSurfaceLocalPosition(new Vector2(x, y), ResizeHandleFrontOffset);
            }

            return new Vector3(x, y, ResizeHandleFrontOffset);
        }

        /// <summary>
        /// Runs the set resize handles visible workflow.
        /// </summary>
        /// <param name="visible">The visible value.</param>
        private void setResizeHandlesVisible(bool visible)
        {
            if (visible)
            {
                ensureResizeHandles();
                updateResizeHandleTransforms();
            }

            if (_resizeHandles == null)
            {
                _resizeHandlesVisible = false;
                return;
            }

            _resizeHandlesVisible = visible && _runtimeResizable;

            for (int i = 0; i < _resizeHandles.Length; i++)
            {
                GameObject handle = _resizeHandles[i];
                if (handle != null)
                {
                    handle.SetActive(_runtimeResizable);

                    MeshRenderer renderer = handle.GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        renderer.enabled = _resizeHandlesVisible;
                        setRendererColor(renderer, getResizeHandleColor(i));
                    }

                    Collider collider = handle.GetComponent<Collider>();
                    if (collider != null)
                    {
                        collider.enabled = canResizeHandleInteract(i);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the get resize handle id result.
        /// </summary>
        /// <param name="handleIndex">The handle Index value.</param>
        /// <returns>The result of the operation.</returns>
        private string getResizeHandleID(int handleIndex)
        {
            return ID + "_ResizeHandle_" + handleIndex;
        }

        /// <summary>
        /// Returns the get resize handle material result.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        private Material getResizeHandleMaterial()
        {
            if (_resizeHandleMaterial != null)
            {
                return _resizeHandleMaterial;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader != null)
            {
                _resizeHandleMaterial = new Material(shader);
            }
            else if (Fugui.Settings != null && Fugui.Settings.UIPanelMaterial != null)
            {
                _resizeHandleMaterial = GameObject.Instantiate(Fugui.Settings.UIPanelMaterial);
            }

            setMaterialColor(_resizeHandleMaterial, getThemeColor(FuColors.Highlight, new Color(0f, 0.72f, 1f, 1f)));
            return _resizeHandleMaterial;
        }

        /// <summary>
        /// Returns the themed color for a resize handle state.
        /// </summary>
        /// <param name="handleIndex">The handle index.</param>
        /// <returns>Theme color.</returns>
        private Color getResizeHandleColor(int handleIndex)
        {
            if (_activeResizeHandleIndex == handleIndex)
            {
                return getThemeColor(FuColors.HighlightActive, new Color(0f, 0.58f, 0.8f, 1f));
            }

            if (_hoveredResizeHandleIndex == handleIndex)
            {
                return getThemeColor(FuColors.HighlightHovered, new Color(0f, 0.65f, 0.9f, 1f));
            }

            return getThemeColor(FuColors.Highlight, new Color(0f, 0.72f, 1f, 1f));
        }

        /// <summary>
        /// Returns a Fugui theme color with a safe fallback.
        /// </summary>
        /// <param name="color">Theme color key.</param>
        /// <param name="fallback">Fallback color.</param>
        /// <returns>Theme color.</returns>
        private Color getThemeColor(FuColors color, Color fallback)
        {
            if (Fugui.Themes == null || Fugui.Themes.CurrentTheme == null)
            {
                return fallback;
            }

            return Fugui.Themes.GetColor(color);
        }

        /// <summary>
        /// Applies a per-renderer color without duplicating the material.
        /// </summary>
        /// <param name="renderer">Renderer to update.</param>
        /// <param name="color">Color to apply.</param>
        private void setRendererColor(Renderer renderer, Color color)
        {
            if (renderer == null)
            {
                return;
            }

            if (_resizeHandlePropertyBlock == null)
            {
                _resizeHandlePropertyBlock = new MaterialPropertyBlock();
            }

            renderer.GetPropertyBlock(_resizeHandlePropertyBlock);
            _resizeHandlePropertyBlock.SetColor("_BaseColor", color);
            _resizeHandlePropertyBlock.SetColor("_Color", color);
            renderer.SetPropertyBlock(_resizeHandlePropertyBlock);
        }

        /// <summary>
        /// Runs the set material color workflow.
        /// </summary>
        /// <param name="material">The material value.</param>
        /// <param name="color">The color value.</param>
        private void setMaterialColor(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }

        /// <summary>
        /// Try to prepare the Fugui Context rendering (time to inject inputs)
        /// </summary>
        /// <returns>must return false if the window will not be draw this frame</returns>
        private bool context_OnPrepareFrame()
        {
            if (Window == null)
            {
                return false;
            }

            // get input state for this container
            InputState inputState = FuRaycasting.GetInputState(ID, _panelGameObject);
            bool blockWindowInput = updateRuntimeResize(inputState);
            blockWindowInput |= !Window.IsInterractable;
            _fuguiContext.AutoUpdateKeyboard = Window.IsInterractable;

            // force to draw if hover in
            if (inputState.Hovered && !blockWindowInput)
            {
                if (_useExplicitResolution)
                {
                    float normalizedX = (inputState.MousePosition.x + (_localSize.x * 0.5f)) / Mathf.Max(0.0001f, _localSize.x);
                    float normalizedY = inputState.MousePosition.y / Mathf.Max(0.0001f, _localSize.y);
                    _localMousePos = new Vector2Int(
                        Mathf.RoundToInt(normalizedX * _size.x),
                        Mathf.RoundToInt((1f - normalizedY) * _size.y));
                }
                else
                {
                    Vector2 scaledMousePosition = inputState.MousePosition * (1000f / Mathf.Max(0.0001f, _windows3DScale)) * Context.Scale;
                    // calculate IO mouse pos
                    _localMousePos = new Vector2Int((int)scaledMousePosition.x, (int)scaledMousePosition.y);
                    _localMousePos.x += _size.x / 2;
                    _localMousePos.y = Size.y - _localMousePos.y;
                }
            }
            else
            {
                _localMousePos = new Vector2Int(-1, -1);
            }

            // update context mouse position
            _fuguiContext.UpdateMouse(
                _localMousePos,
                blockWindowInput ? Vector2.zero : new Vector2(0f, inputState.MouseWheel),
                !blockWindowInput && inputState.MouseButtons[0],
                !blockWindowInput && inputState.MouseButtons[1],
                !blockWindowInput && inputState.MouseButtons[2]);

            return true;
        }

        /// <summary>
        /// Runs the fugui context on frame prepared workflow.
        /// </summary>
        private void _fuguiContext_OnFramePrepared()
        {
            // update mouse states
            _mouseState.UpdateState(this);
            _keyboardState.UpdateState();
        }

        /// <summary>
        /// Whatever this container must force the local position of it's windows
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public bool ForcePos()
        {
            return true;
        }

        /// <summary>
        /// Whatever this container own a window
        /// </summary>
        /// <param name="id">name of the window to check</param>
        /// <returns>The result of the operation.</returns>
        public bool HasWindow(string id)
        {
            return Window != null && Window.ID == id;
        }

        /// <summary>
        /// Execute a callback on each windows on this container
        /// </summary>
        /// <param name="callback">callback to execute on each windows</param>
        public void OnEachWindow(Action<FuWindow> callback)
        {
            callback?.Invoke(Window);
        }

        /// <summary>
        /// Resize the 3D window so the generated panel mesh matches a target local world size.
        /// </summary>
        /// <param name="localSize">Target local size of the 3D placeholder.</param>
        public void SetLocalSize(Vector2 localSize)
        {
            SetLocalSize(localSize, true);
        }

        /// <summary>
        /// Sets the local size.
        /// </summary>
        /// <param name="localSize">The local Size value.</param>
        /// <param name="refreshRender">The refresh Render value.</param>
        private void SetLocalSize(Vector2 localSize, bool refreshRender)
        {
            if (IsClosed || Window == null || Context == null)
                return;

            Vector2 previousLocalSize = _localSize;
            localSize = new Vector2(
                Mathf.Max(RuntimeResizeMinSize, Mathf.Abs(localSize.x)),
                Mathf.Max(RuntimeResizeMinSize, Mathf.Abs(localSize.y))
            );
            _localSize = localSize;
            bool localSizeChanged = (previousLocalSize - localSize).sqrMagnitude > 0.00000001f;

            if (!refreshRender)
            {
                if (localSizeChanged)
                {
                    createPanel();
                }

                updateResizeHandleTransforms();
                return;
            }

            if (_useExplicitResolution)
            {
                refreshExplicitRenderSize(localSizeChanged);
                return;
            }

            if (_fuguiContext.ContainerScaleConfig.Enabled)
            {
                _fuguiContext.UpdateContainerScale(getScaleSourceSize(localSize, _fuguiContext.ContainerScaleConfig));
                _localSize = localSize;
            }

            Vector2Int targetSize = getRenderSizeForLocalSize(localSize, Context.Scale);

            if (Window.Size == targetSize &&
                _size == targetSize &&
                RenderTexture != null &&
                RenderTexture.width == targetSize.x &&
                RenderTexture.height == targetSize.y &&
                RenderTexture.IsCreated())
            {
                return;
            }

            if (Window.Size != targetSize)
            {
                Window.Size = targetSize;
            }

            setRenderSize(targetSize);
            updateResizeHandleTransforms();
        }

        /// <summary>
        /// Runs the refresh explicit render size workflow.
        /// </summary>
        /// <param name="rebuildPanelWhenOnlyLocalSizeChanged">The rebuild Panel When Only Local Size Changed value.</param>
        private void refreshExplicitRenderSize(bool rebuildPanelWhenOnlyLocalSizeChanged)
        {
            Vector2Int targetResolution = getExplicitResolutionForLocalSize(_localSize);
            _explicitResolution = targetResolution;

            if (_fuguiContext.ContainerScaleConfig.Enabled)
            {
                _fuguiContext.UpdateContainerScale(targetResolution);
            }

            if (Window.Size != targetResolution)
            {
                Window.Size = targetResolution;
            }

            bool renderSizeChanged = _size != targetResolution ||
                                     RenderTexture == null ||
                                     RenderTexture.width != targetResolution.x ||
                                     RenderTexture.height != targetResolution.y ||
                                     !RenderTexture.IsCreated();

            setRenderSize(targetResolution);

            if (rebuildPanelWhenOnlyLocalSizeChanged && !renderSizeChanged)
            {
                createPanel();
            }

            updateResizeHandleTransforms();
        }

        /// <summary>
        /// Gets the texture id.
        /// </summary>
        /// <param name="texture">The texture value.</param>
        /// <returns>The result of the operation.</returns>
        public IntPtr GetTextureID(Texture2D texture)
        {
            return _fuguiContext.TextureManager.GetTextureId(texture);
        }

        /// <summary>
        /// Gets the texture id.
        /// </summary>
        /// <param name="texture">The texture value.</param>
        /// <returns>The result of the operation.</returns>
        public IntPtr GetTextureID(RenderTexture texture)
        {
            return _fuguiContext.TextureManager.GetTextureId(texture);
        }

        /// <summary>
        /// Runs the im gui image workflow.
        /// </summary>
        /// <param name="texture">The texture value.</param>
        /// <param name="size">The size value.</param>
        public void ImGuiImage(RenderTexture texture, Vector2 size)
        {
            if (texture == null)
            {
                ImGui.Dummy(size);
                return;
            }
            ImGui.Image(GetTextureID(texture), size);
        }

        /// <summary>
        /// Runs the im gui image workflow.
        /// </summary>
        /// <param name="texture">The texture value.</param>
        /// <param name="size">The size value.</param>
        public void ImGuiImage(Texture2D texture, Vector2 size)
        {
            if (texture == null)
            {
                ImGui.Dummy(size);
                return;
            }
            ImGui.Image(GetTextureID(texture), size);
        }

        /// <summary>
        /// Runs the im gui image workflow.
        /// </summary>
        /// <param name="texture">The texture value.</param>
        /// <param name="size">The size value.</param>
        /// <param name="color">The color value.</param>
        public void ImGuiImage(RenderTexture texture, Vector2 size, Vector4 color)
        {
            if (texture == null)
            {
                ImGui.Dummy(size);
                return;
            }
            ImGui.Image(GetTextureID(texture), size, Vector2.zero, Vector2.one, color);
        }

        /// <summary>
        /// Runs the im gui image workflow.
        /// </summary>
        /// <param name="texture">The texture value.</param>
        /// <param name="size">The size value.</param>
        /// <param name="color">The color value.</param>
        public void ImGuiImage(Texture2D texture, Vector2 size, Vector4 color)
        {
            if (texture == null)
            {
                ImGui.Dummy(size);
                return;
            }
            ImGui.Image(GetTextureID(texture), size, Vector2.zero, Vector2.one, color);
        }

        /// <summary>
        /// Returns the im gui image button result.
        /// </summary>
        /// <param name="texture">The texture value.</param>
        /// <param name="size">The size value.</param>
        /// <returns>The result of the operation.</returns>
        public bool ImGuiImageButton(Texture2D texture, Vector2 size)
        {
            if (texture == null)
            {
                ImGui.Dummy(size);
                return false;
            }
            // TODO : add ID to image button
            return ImGui.ImageButton("", GetTextureID(texture), size);
        }

        /// <summary>
        /// Returns the im gui image button result.
        /// </summary>
        /// <param name="texture">The texture value.</param>
        /// <param name="size">The size value.</param>
        /// <param name="color">The color value.</param>
        /// <returns>The result of the operation.</returns>
        public bool ImGuiImageButton(Texture2D texture, Vector2 size, Vector4 color)
        {
            if (texture == null)
            {
                ImGui.Dummy(size);
                return false;
            }
            // TODO : add ID to image button
            return ImGui.ImageButton("", GetTextureID(texture), size, Vector2.zero, Vector2.one, ImGui.GetStyle().Colors[(int)ImGuiCol.Button], color);
        }

        /// <summary>
        /// Render a window into this container
        /// </summary>
        /// <param name="FuWindow">the window to draw</param>
        public void RenderFuWindow(FuWindow FuWindow)
        {
            // force to place window to local container position zero
            if (FuWindow.LocalPosition.x != 0 || FuWindow.LocalPosition.y != 0)
            {
                FuWindow.LocalPosition = Vector2Int.zero;
            }
            // update the window state (Idle / Manipulating etc)
            FuWindow.UpdateState(_fuguiContext.IO.MouseDown[0]);
            // call UIWindow.DrawWindow
            FuWindow.DrawWindow();
        }

        /// <summary>
        /// Render each Windows of this container
        /// </summary>
        public void RenderFuWindows()
        {
            if (Window != null)
            {
                // draw the window
                RenderFuWindow(Window);

                // draw the context menu
                Fugui.RenderContextMenu();
            }
        }

        /// <summary>
        /// Try to add a window into this container
        /// </summary>
        /// <param name="FuWindow">The window to add</param>
        /// <returns>The result of the operation.</returns>
        public bool TryAddWindow(FuWindow FuWindow)
        {
            if (IsClosed)
                return false;

            if (Window == null)
            {
                Window = FuWindow;
                Window.OnClosed += Window_OnClosed;
                Window.OnResized += Window_OnResized;
                Window.LocalPosition = Vector2Int.zero;
                Window.Container = this;
                Window.LocalPosition = Vector2Int.zero;
                Window.AddWindowFlag(ImGuiWindowFlags.NoMove);
                Window.AddWindowFlag(ImGuiWindowFlags.NoResize);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Whenever a window is resized
        /// </summary>
        /// <param name="window">the resized window</param>
        private void Window_OnResized(FuWindow window)
        {
            if (_useExplicitResolution)
            {
                refreshExplicitRenderSize(false);
                return;
            }

            setRenderSize(window.Size);
            _localSize = getCurrentLocalSize();
            updateResizeHandleTransforms();
        }

        /// <summary>
        /// Whenever a window is closed
        /// </summary>
        /// <param name="window">the closed window</param>
        private void Window_OnClosed(FuWindow window)
        {
            Close();
        }

        /// <summary>
        /// Try to remove a window from this container
        /// </summary>
        /// <param name="id">ID of the window to remove</param>
        /// <returns>The result of the operation.</returns>
        public bool TryRemoveWindow(string id)
        {
            if (Window != null && Window.ID == id)
            {
                Close();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Close this container
        /// </summary>
        public void Close()
        {
            if (IsClosed)
                return;

            IsClosed = true;
            string windowID = Window?.ID;

            if (Window != null)
            {
                Window.OnClosed -= Window_OnClosed;
                Window.OnResized -= Window_OnResized;
                Window.Container = null;
                Window.RemoveWindowFlag(ImGuiWindowFlags.NoMove);
                Window.RemoveWindowFlag(ImGuiWindowFlags.NoResize);
                Window.Is3DWindow = false;
                Window = null;
            }
            if (_fuguiContext != null)
            {
                _fuguiContext.OnRender -= RenderFuWindows;
                _fuguiContext.OnPrepareFrame -= context_OnPrepareFrame;
                _fuguiContext.OnFramePrepared -= _fuguiContext_OnFramePrepared;
                Fugui.DestroyContext(_fuguiContext);
                _fuguiContext = null;
            }
            Fugui.Themes.OnThemeSet -= ThemeManager_OnThemeSet;
            if (_panelGameObject != null)
            {
                UnityEngine.Object.Destroy(_panelGameObject);
                _panelGameObject = null;
            }
            _panelMesh = null;
            _panelCollider = null;
            _resizeHandles = null;
            if (_resizeHandleMeshes != null)
            {
                for (int i = 0; i < _resizeHandleMeshes.Length; i++)
                {
                    if (_resizeHandleMeshes[i] != null)
                    {
                        UnityEngine.Object.Destroy(_resizeHandleMeshes[i]);
                        _resizeHandleMeshes[i] = null;
                    }
                }
                _resizeHandleMeshes = null;
            }
            if (_resizeHandleMaterial != null)
            {
                UnityEngine.Object.Destroy(_resizeHandleMaterial);
                _resizeHandleMaterial = null;
            }
            if (RenderTexture != null)
            {
                ReleaseRenderTexture(RenderTexture);
                RenderTexture = null;
            }

            OnRuntimeResized = null;
            Fugui.Unregister3DWindow(windowID);
        }
        #endregion
    }
}
