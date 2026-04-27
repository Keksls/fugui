using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
#endif

namespace Fu.Framework
{
    /// <summary>
    /// Represents the Fu3 DWindow Behaviour type.
    /// </summary>
    [ExecuteAlways]
    public class Fu3DWindowBehaviour : MonoBehaviour
    {
        #region State
        [Tooltip("Depth of the generated 3D panel extrusion.")]
        public float Depth = 0.01f;

        [Tooltip("Horizontal curve angle of the generated 3D panel in degrees. 0 keeps the panel flat.")]
        public float Curve = 0f;

        [SerializeField]
        protected FuWindowName _windowName;

        [SerializeField]
        protected FuWindowFlags _windowFlags = FuWindowFlags.Default;

        [SerializeField]
        protected bool _forceCreateAloneOnAwake = false;

        [SerializeField]
        protected bool _runtimeResizable = true;

        [SerializeField]
        [Tooltip("Render texture and ImGui context resolution for this 3D window.")]
        protected Vector2Int _renderResolution = new Vector2Int(1024, 1024);

        [SerializeField, HideInInspector]
        protected bool _scaleResolutionWithPanel = false;

        [SerializeField, HideInInspector]
        protected Vector2 _referencePanelSize = Vector2.zero;

        [SerializeField, HideInInspector]
        protected Vector2Int _minRenderResolution = Vector2Int.one;

        [SerializeField, HideInInspector]
        protected Vector2Int _maxRenderResolution = Vector2Int.zero;

        [SerializeField]
        protected bool _useContainerScaler = false;

        [SerializeField]
        protected Vector2Int _referenceResolution = new Vector2Int(1920, 1080);

        [SerializeField, Range(0f, 1f)]
        protected float _matchWidthOrHeight = 0.5f;

        [SerializeField]
        protected float _minContainerScale = 0.5f;

        [SerializeField]
        protected float _maxContainerScale = 4f;

        [SerializeField]
        protected float _baseContextScale = 0f;

        [SerializeField]
        protected float _baseFontScale = 0f;

        [SerializeField]
        protected bool _scaleFontWithContainer = true;

        protected FuWindow _fuWindow;
        protected Fu3DWindowContainer _container;
        protected FuWindowDefinition _windowDefinition;
        private Vector2 _autoReferencePanelSize = Vector2.zero;

        public FuWindow Window
        {
            get { return _fuWindow; }
        }

        public Fu3DWindowContainer Container
        {
            get { return _container; }
        }

        public FuWindowDefinition WindowDefinition
        {
            get { return _windowDefinition; }
        }
        #endregion

        /// <summary>
        /// Register the window definition and create the window instance if needed.
        /// </summary>
        public virtual void FuguiAwake()
        {
            if (!enabled)
                return;

            EnsureWindowDefinitionRegistered();

            if (_forceCreateAloneOnAwake)
            {
                Create3DWindow();
            }
        }

        /// <summary>
        /// Returns the ensure window definition registered result.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public FuWindowDefinition EnsureWindowDefinitionRegistered()
        {
            if (_windowDefinition != null)
            {
                return _windowDefinition;
            }

            if (Fugui.UIWindowsDefinitions != null &&
                Fugui.UIWindowsDefinitions.TryGetValue(_windowName, out FuWindowDefinition existingDefinition))
            {
                _windowDefinition = existingDefinition;
            }
            else
            {
                _windowDefinition = new FuWindowDefinition(
                    _windowName,
                    OnUI,
                    Vector2Int.zero,
                    GetWindowSizeFromPlaceholder(),
                    _windowFlags,
                    FuExternalWindowFlags.Default
                );

                OnWindowDefinitionCreated(_windowDefinition);
            }

            _windowDefinition.OnUIWindowCreated -= WindowDefinition_OnUIWindowCreated;
            _windowDefinition.OnUIWindowCreated += WindowDefinition_OnUIWindowCreated;
            return _windowDefinition;
        }

        /// <summary>
        /// Creates the 3 dwindow.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public Fu3DWindowContainer Create3DWindow()
        {
            if (_container != null && !_container.IsClosed)
            {
                return _container;
            }

            EnsureWindowDefinitionRegistered();
            FuWindow window = Fugui.CreateWindow(_windowName, false);
            if (window != null && (_container == null || _container.IsClosed))
            {
                AttachWindow(window);
            }

            if ((_container == null || _container.IsClosed) && Fugui.UIWindows != null)
            {
                foreach (FuWindow existingWindow in Fugui.UIWindows.Values)
                {
                    if (existingWindow != null && existingWindow.WindowName.Equals(_windowName))
                    {
                        AttachWindow(existingWindow);
                        break;
                    }
                }
            }
            return _container;
        }

        /// <summary>
        /// Returns the attach window result.
        /// </summary>
        /// <param name="window">The window value.</param>
        /// <returns>The result of the operation.</returns>
        public Fu3DWindowContainer AttachWindow(FuWindow window)
        {
            if (window == null)
            {
                return null;
            }

            WindowDefinition_OnUIWindowCreated(window);
            return _container;
        }

        /// <summary>
        /// Runs the close3 dwindow workflow.
        /// </summary>
        public void Close3DWindow()
        {
            _fuWindow?.Close();
        }

        /// <summary>
        /// Override this method to customize the window definition after creation.
        /// </summary>
        /// <param name="windowDefinition">The created window definition.</param>
        public virtual void OnWindowDefinitionCreated(FuWindowDefinition windowDefinition)
        {
        }

        /// <summary>
        /// Handle the Fugui window creation and attach it to a 3D container.
        /// </summary>
        /// <param name="window">The created Fugui window.</param>
        protected void WindowDefinition_OnUIWindowCreated(FuWindow window)
        {
            if (_fuWindow != null)
            {
                _fuWindow.OnClosed -= Window_OnClosed;
            }

            _fuWindow = window;
            _fuWindow.OnClosed += Window_OnClosed;
            if (_autoReferencePanelSize.x <= 0f || _autoReferencePanelSize.y <= 0f)
            {
                _autoReferencePanelSize = GetPlaceholderSize();
            }
            _container = Fugui.Add3DWindow(_fuWindow, Get3DWindowSettings(), transform.position, transform.rotation);
            if (_container != null)
            {
                _container.OnRuntimeResized -= Container_OnRuntimeResized;
                _container.OnRuntimeResized += Container_OnRuntimeResized;
                _container.SetRuntimeResizable(_runtimeResizable);
            }

            ApplyPlaceholderToContainer();

            OnWindowCreated(window);
        }

        /// <summary>
        /// Override this method to draw the window UI.
        /// </summary>
        /// <param name="window">The Fugui window.</param>
        /// <param name="layout">The Fugui layout.</param>
        public virtual void OnUI(FuWindow window, FuLayout layout)
        {
        }

        /// <summary>
        /// Override this method to react when the window is created.
        /// </summary>
        /// <param name="window">The created Fugui window.</param>
        public virtual void OnWindowCreated(FuWindow window)
        {
        }

        /// <summary>
        /// Get the window name associated with this behaviour.
        /// </summary>
        /// <returns>The window name.</returns>
        public FuWindowName GetWindowName()
        {
            return _windowName;
        }

        /// <summary>
        /// Set the window name associated with this behaviour.
        /// </summary>
        /// <param name="value">The window name.</param>
        public void SetWindowName(FuWindowName value)
        {
            if (_windowDefinition != null)
            {
                _windowDefinition.OnUIWindowCreated -= WindowDefinition_OnUIWindowCreated;
                _windowDefinition = null;
            }
            _windowName = value;
        }

        /// <summary>
        /// Returns the is runtime resizable result.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public bool IsRuntimeResizable()
        {
            return _runtimeResizable;
        }

        /// <summary>
        /// Sets the runtime resizable.
        /// </summary>
        /// <param name="value">The value value.</param>
        public void SetRuntimeResizable(bool value)
        {
            _runtimeResizable = value;
            _container?.SetRuntimeResizable(value);
        }

        /// <summary>
        /// Gets the container scale config.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public FuContainerScaleConfig GetContainerScaleConfig()
        {
            float baseScale = _baseContextScale > 0f
                ? _baseContextScale
                : 1f;
            float baseFontScale = _baseFontScale > 0f
                ? _baseFontScale
                : 1f;

            if (!_useContainerScaler)
            {
                return FuContainerScaleConfig.Disabled(baseScale, baseFontScale);
            }

            return FuContainerScaleConfig.Reference(
                _referenceResolution,
                _matchWidthOrHeight,
                _minContainerScale,
                _maxContainerScale,
                baseScale,
                baseFontScale,
                _scaleFontWithContainer
            );
        }

        /// <summary>
        /// Sets the container scale config.
        /// </summary>
        /// <param name="config">The config value.</param>
        public void SetContainerScaleConfig(FuContainerScaleConfig config)
        {
            config.Sanitize();
            _useContainerScaler = config.Enabled;
            _referenceResolution = config.ReferenceResolution;
            _matchWidthOrHeight = config.MatchWidthOrHeight;
            _minContainerScale = config.MinScale;
            _maxContainerScale = config.MaxScale;
            _baseContextScale = config.BaseScale;
            _baseFontScale = config.BaseFontScale;
            _scaleFontWithContainer = config.ScaleFont;
            _container?.SetContainerScaleConfig(config);
        }

        /// <summary>
        /// Gets the 3 dwindow settings.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public Fu3DWindowSettings Get3DWindowSettings()
        {
            float contextScale = _baseContextScale > 0f ? _baseContextScale : 1f;
            float fontScale = _baseFontScale > 0f ? _baseFontScale : 1f;
            Vector2 panelSize = GetPlaceholderSize();
            Fu3DWindowSettings settings = Fu3DWindowSettings.FixedResolutionMatchingPanelAspect(
                panelSize,
                _renderResolution,
                GetResolutionReferencePanelSize(),
                contextScale,
                fontScale,
                _minRenderResolution,
                _maxRenderResolution,
                Depth,
                Curve);
            settings.ContainerScaleConfig = GetContainerScaleConfig();
            settings.Sanitize();
            return settings;
        }

        /// <summary>
        /// Gets the resolution reference panel size.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        private Vector2 GetResolutionReferencePanelSize()
        {
            if (_referencePanelSize.x > 0f && _referencePanelSize.y > 0f)
            {
                return _referencePanelSize;
            }

            if (_autoReferencePanelSize.x <= 0f || _autoReferencePanelSize.y <= 0f)
            {
                _autoReferencePanelSize = GetPlaceholderSize();
            }

            return _autoReferencePanelSize;
        }

        /// <summary>
        /// Runs the late update workflow.
        /// </summary>
        private void LateUpdate()
        {
            EnforceDepth();
            ApplyPlaceholderToContainer();
        }

        /// <summary>
        /// Runs the apply placeholder to container workflow.
        /// </summary>
        private void ApplyPlaceholderToContainer()
        {
            if (_container == null || _container.IsClosed || _container.Window == null)
            {
                _container = null;
                return;
            }

            _container.SetPosition(transform.position);
            _container.SetRotation(transform.rotation);
            if (!_container.IsRuntimeResizing)
            {
                _container.Set3DWindowSettings(Get3DWindowSettings());
            }
            _container.SetRuntimeResizable(_runtimeResizable);
        }

        /// <summary>
        /// Runs the window on closed workflow.
        /// </summary>
        /// <param name="window">The window value.</param>
        private void Window_OnClosed(FuWindow window)
        {
            if (_fuWindow != window)
                return;

            _fuWindow.OnClosed -= Window_OnClosed;
            if (_container != null)
            {
                _container.OnRuntimeResized -= Container_OnRuntimeResized;
            }
            _fuWindow = null;
            _container = null;
            _autoReferencePanelSize = Vector2.zero;
        }

        /// <summary>
        /// Runs the container on runtime resized workflow.
        /// </summary>
        /// <param name="position">The position value.</param>
        /// <param name="localSize">The local Size value.</param>
        private void Container_OnRuntimeResized(Vector3 position, Vector2 localSize)
        {
            transform.position = position;

            Vector3 localScale = transform.localScale;
            Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;

            localScale.x = getSignedScale(localScale.x, localSize.x, parentScale.x);
            localScale.y = getSignedScale(localScale.y, localSize.y, parentScale.y);
            localScale.z = Depth;
            transform.localScale = localScale;
        }

        /// <summary>
        /// Returns the get signed scale result.
        /// </summary>
        /// <param name="currentLocalScale">The current Local Scale value.</param>
        /// <param name="targetWorldSize">The target World Size value.</param>
        /// <param name="parentWorldScale">The parent World Scale value.</param>
        /// <returns>The result of the operation.</returns>
        private float getSignedScale(float currentLocalScale, float targetWorldSize, float parentWorldScale)
        {
            float sign = currentLocalScale < 0f ? -1f : 1f;
            float parentScale = Mathf.Abs(parentWorldScale);
            if (parentScale < 0.0001f)
            {
                parentScale = 1f;
            }

            return sign * Mathf.Max(0.0001f, targetWorldSize / parentScale);
        }

        /// <summary>
        /// Handles the Destroy event.
        /// </summary>
        private void OnDestroy()
        {
            if (_windowDefinition != null)
            {
                _windowDefinition.OnUIWindowCreated -= WindowDefinition_OnUIWindowCreated;
            }

            if (_fuWindow != null)
            {
                _fuWindow.OnClosed -= Window_OnClosed;
            }

            if (_container != null)
            {
                _container.OnRuntimeResized -= Container_OnRuntimeResized;
            }
        }

        /// <summary>
        /// Gets the placeholder size.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        private Vector2 GetPlaceholderSize()
        {
            Vector3 worldScale = transform.lossyScale;

            return new Vector2(
                Mathf.Abs(worldScale.x),
                Mathf.Abs(worldScale.y)
            );
        }

        /// <summary>
        /// Gets the window size from placeholder.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        private Vector2Int GetWindowSizeFromPlaceholder()
        {
            return new Vector2Int(
                Mathf.Max(1, _renderResolution.x),
                Mathf.Max(1, _renderResolution.y)
            );
        }

        /// <summary>
        /// Runs the enforce depth workflow.
        /// </summary>
        private void EnforceDepth()
        {
            Vector3 scale = transform.localScale;

            if (Mathf.Abs(scale.z - Depth) > 0.0001f)
            {
                scale.z = Depth;
                transform.localScale = scale;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Depth = Mathf.Max(0.0001f, Depth);
            Curve = Mathf.Clamp(Curve, 0f, 359.9f);
            EnforceDepth();
            _referenceResolution = new Vector2Int(
                Mathf.Max(1, _referenceResolution.x),
                Mathf.Max(1, _referenceResolution.y)
            );
            _matchWidthOrHeight = Mathf.Clamp01(_matchWidthOrHeight);
            _minContainerScale = Mathf.Max(0.0001f, _minContainerScale);
            _maxContainerScale = Mathf.Max(_minContainerScale, _maxContainerScale);
            _baseContextScale = Mathf.Max(0f, _baseContextScale);
            _baseFontScale = Mathf.Max(0f, _baseFontScale);
            _renderResolution = new Vector2Int(
                Mathf.Max(1, _renderResolution.x),
                Mathf.Max(1, _renderResolution.y)
            );
            _referencePanelSize = new Vector2(
                Mathf.Max(0f, _referencePanelSize.x),
                Mathf.Max(0f, _referencePanelSize.y)
            );
            _minRenderResolution = new Vector2Int(
                Mathf.Max(1, _minRenderResolution.x),
                Mathf.Max(1, _minRenderResolution.y)
            );
            _maxRenderResolution = new Vector2Int(
                Mathf.Max(0, _maxRenderResolution.x),
                Mathf.Max(0, _maxRenderResolution.y)
            );
            if (_maxRenderResolution.x > 0)
            {
                _maxRenderResolution.x = Mathf.Max(_minRenderResolution.x, _maxRenderResolution.x);
            }
            if (_maxRenderResolution.y > 0)
            {
                _maxRenderResolution.y = Mathf.Max(_minRenderResolution.y, _maxRenderResolution.y);
            }
            if (!Application.isPlaying)
            {
                _autoReferencePanelSize = Vector2.zero;
            }
        }

        private void OnDrawGizmos()
        {
            if (Curve > 0.001f)
            {
                DrawCurvedPlaceholderGizmo();
                return;
            }

            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.color = new Color(0f, 0.6f, 1f, 0.18f);
            Gizmos.DrawCube(new Vector3(0f, 0.5f, 0f), Vector3.one);

            Gizmos.color = new Color(0f, 0.6f, 1f, 0.75f);
            Gizmos.DrawWireCube(new Vector3(0f, 0.5f, 0f), Vector3.one);
        }

        private void DrawCurvedPlaceholderGizmo()
        {
            Vector2 size = GetPlaceholderSize();
            float width = Mathf.Max(0.0001f, size.x);
            float height = Mathf.Max(0.0001f, size.y);
            float depth = Mathf.Max(0.0001f, Depth);
            float curveAngle = Mathf.Clamp(Curve, 0f, 359.9f);
            int segments = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(24f, curveAngle / 4f)), 8, 96);

            Matrix4x4 previousMatrix = Handles.matrix;
            Color previousColor = Handles.color;
            Handles.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

            Color fillColor = new Color(0f, 0.6f, 1f, 0.12f);
            Color wireColor = new Color(0f, 0.6f, 1f, 0.75f);

            for (int i = 0; i < segments; i++)
            {
                float t0 = (float)i / segments;
                float t1 = (float)(i + 1) / segments;
                Vector3 frontBottom0 = GetCurvedPlaceholderPoint(t0, 0f, width, height, curveAngle);
                Vector3 frontTop0 = GetCurvedPlaceholderPoint(t0, 1f, width, height, curveAngle);
                Vector3 frontBottom1 = GetCurvedPlaceholderPoint(t1, 0f, width, height, curveAngle);
                Vector3 frontTop1 = GetCurvedPlaceholderPoint(t1, 1f, width, height, curveAngle);

                Handles.color = fillColor;
                Handles.DrawAAConvexPolygon(new Vector3[] { frontBottom0, frontTop0, frontTop1, frontBottom1 });

                Vector3 backBottom0 = frontBottom0 - GetCurvedPlaceholderNormal(t0, width, curveAngle) * depth;
                Vector3 backTop0 = frontTop0 - GetCurvedPlaceholderNormal(t0, width, curveAngle) * depth;
                Vector3 backBottom1 = frontBottom1 - GetCurvedPlaceholderNormal(t1, width, curveAngle) * depth;
                Vector3 backTop1 = frontTop1 - GetCurvedPlaceholderNormal(t1, width, curveAngle) * depth;

                Handles.color = wireColor;
                Handles.DrawLine(frontBottom0, frontBottom1);
                Handles.DrawLine(frontTop0, frontTop1);
                Handles.DrawLine(backBottom0, backBottom1);
                Handles.DrawLine(backTop0, backTop1);

                if (i == 0)
                {
                    Handles.DrawLine(frontBottom0, frontTop0);
                    Handles.DrawLine(backBottom0, backTop0);
                    Handles.DrawLine(frontBottom0, backBottom0);
                    Handles.DrawLine(frontTop0, backTop0);
                }

                if (i == segments - 1)
                {
                    Handles.DrawLine(frontBottom1, frontTop1);
                    Handles.DrawLine(backBottom1, backTop1);
                    Handles.DrawLine(frontBottom1, backBottom1);
                    Handles.DrawLine(frontTop1, backTop1);
                }
            }

            Handles.color = previousColor;
            Handles.matrix = previousMatrix;
        }

        private Vector3 GetCurvedPlaceholderPoint(float normalizedX, float normalizedY, float width, float height, float curveAngle)
        {
            float angleRad = Mathf.Max(0.0001f, curveAngle * Mathf.Deg2Rad);
            float radius = width / angleRad;
            float flatX = Mathf.Lerp(-width * 0.5f, width * 0.5f, normalizedX);
            float theta = flatX / radius;

            return new Vector3(
                Mathf.Sin(theta) * radius,
                normalizedY * height,
                (Mathf.Cos(theta) - 1f) * radius);
        }

        private Vector3 GetCurvedPlaceholderNormal(float normalizedX, float width, float curveAngle)
        {
            float angleRad = Mathf.Max(0.0001f, curveAngle * Mathf.Deg2Rad);
            float radius = width / angleRad;
            float flatX = Mathf.Lerp(-width * 0.5f, width * 0.5f, normalizedX);
            float theta = flatX / radius;

            return new Vector3(-Mathf.Sin(theta), 0f, -Mathf.Cos(theta)).normalized;
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(Fu3DWindowBehaviour), true)]
    public class Fu3DWindowBehaviourEditor : Editor
    {
        private const float HandleSize = 0.08f;
        private const float MinSize = 0.0001f;

        private List<FuWindowName> availableNames;
        private string[] windowNameOptions;
        private int selectedIndex;

        private SerializedProperty windowFlagsProp;
        private SerializedProperty forceCreateProp;
        private SerializedProperty runtimeResizableProp;
        private SerializedProperty depthProp;
        private SerializedProperty curveProp;
        private SerializedProperty renderResolutionProp;
        private SerializedProperty useContainerScalerProp;
        private SerializedProperty referenceResolutionProp;
        private SerializedProperty matchWidthOrHeightProp;
        private SerializedProperty minContainerScaleProp;
        private SerializedProperty maxContainerScaleProp;
        private SerializedProperty baseContextScaleProp;
        private SerializedProperty baseFontScaleProp;
        private SerializedProperty scaleFontWithContainerProp;

        protected readonly HashSet<string> _excludedProps = new HashSet<string>
        {
            "_windowName",
            "_windowFlags",
            "_forceCreateAloneOnAwake",
            "_runtimeResizable",
            "Depth",
            "Curve",
            "_renderResolution",
            "_scaleResolutionWithPanel",
            "_referencePanelSize",
            "_minRenderResolution",
            "_maxRenderResolution",
            "_useContainerScaler",
            "_referenceResolution",
            "_matchWidthOrHeight",
            "_minContainerScale",
            "_maxContainerScale",
            "_baseContextScale",
            "_baseFontScale",
            "_scaleFontWithContainer",
            "x",
            "y",
            "z"
        };

        public virtual void OnEnable()
        {
            availableNames = FuWindowNameProvider.GetAllWindowNames().Values.ToList();
            windowNameOptions = availableNames.ConvertAll(n => n.Name).ToArray();

            Fu3DWindowBehaviour behaviour = (Fu3DWindowBehaviour)target;
            selectedIndex = Mathf.Max(0, availableNames.FindIndex(n => n.ID == behaviour.GetWindowName().ID));

            windowFlagsProp = serializedObject.FindProperty("_windowFlags");
            forceCreateProp = serializedObject.FindProperty("_forceCreateAloneOnAwake");
            runtimeResizableProp = serializedObject.FindProperty("_runtimeResizable");
            depthProp = serializedObject.FindProperty("Depth");
            curveProp = serializedObject.FindProperty("Curve");
            renderResolutionProp = serializedObject.FindProperty("_renderResolution");
            useContainerScalerProp = serializedObject.FindProperty("_useContainerScaler");
            referenceResolutionProp = serializedObject.FindProperty("_referenceResolution");
            matchWidthOrHeightProp = serializedObject.FindProperty("_matchWidthOrHeight");
            minContainerScaleProp = serializedObject.FindProperty("_minContainerScale");
            maxContainerScaleProp = serializedObject.FindProperty("_maxContainerScale");
            baseContextScaleProp = serializedObject.FindProperty("_baseContextScale");
            baseFontScaleProp = serializedObject.FindProperty("_baseFontScale");
            scaleFontWithContainerProp = serializedObject.FindProperty("_scaleFontWithContainer");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            Fu3DWindowBehaviour behaviour = (Fu3DWindowBehaviour)target;

            EditorGUI.BeginChangeCheck();
            selectedIndex = EditorGUILayout.Popup("Window Name", selectedIndex, windowNameOptions);
            if (EditorGUI.EndChangeCheck())
            {
                FuWindowName newName = availableNames[selectedIndex];
                behaviour.SetWindowName(newName);
                EditorUtility.SetDirty(behaviour);
            }

            EditorGUILayout.PropertyField(windowFlagsProp);
            EditorGUILayout.PropertyField(forceCreateProp);
            EditorGUILayout.PropertyField(runtimeResizableProp);
            EditorGUILayout.PropertyField(depthProp);
            EditorGUILayout.PropertyField(curveProp);
            EditorGUILayout.PropertyField(renderResolutionProp);
            EditorGUILayout.PropertyField(useContainerScalerProp);

            using (new EditorGUI.DisabledScope(!useContainerScalerProp.boolValue))
            {
                EditorGUILayout.PropertyField(referenceResolutionProp);
                EditorGUILayout.PropertyField(matchWidthOrHeightProp);
                EditorGUILayout.PropertyField(minContainerScaleProp);
                EditorGUILayout.PropertyField(maxContainerScaleProp);
                EditorGUILayout.PropertyField(scaleFontWithContainerProp);
            }

            EditorGUILayout.PropertyField(baseContextScaleProp);
            EditorGUILayout.PropertyField(baseFontScaleProp);

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "The 3D panel size is driven by the placeholder scale X/Y. Render Resolution drives the texture and ImGui context size. Scale Z is locked to Depth. Curve is a horizontal angle in degrees.",
                MessageType.Info
            );

            DrawRemainingProperties();

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void DrawRemainingProperties()
        {
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                if (iterator.name == "m_Script")
                    continue;

                if (_excludedProps.Contains(iterator.name))
                    continue;

                EditorGUILayout.PropertyField(iterator, true);
                enterChildren = false;
            }
        }

        private void OnSceneGUI()
        {
            Fu3DWindowBehaviour window = (Fu3DWindowBehaviour)target;
            Transform transform = window.transform;

            EnforceEditorDepth(transform);

            DrawEdgeHandle(window, transform, Vector3.right);
            DrawEdgeHandle(window, transform, Vector3.left);
            DrawEdgeHandle(window, transform, Vector3.up);
            DrawEdgeHandle(window, transform, Vector3.down);
        }

        private void DrawEdgeHandle(Fu3DWindowBehaviour window, Transform transform, Vector3 localDirection)
        {
            Vector3 localPosition;

            if (localDirection.x > 0f)
                localPosition = new Vector3(0.5f, 0.5f, 0f);
            else if (localDirection.x < 0f)
                localPosition = new Vector3(-0.5f, 0.5f, 0f);
            else if (localDirection.y > 0f)
                localPosition = new Vector3(0f, 1f, 0f);
            else
                localPosition = new Vector3(0f, 0f, 0f);

            Vector3 worldPosition = transform.TransformPoint(localPosition);
            Vector3 worldDirection = transform.TransformDirection(localDirection).normalized;

            float size = HandleUtility.GetHandleSize(worldPosition) * HandleSize;

            EditorGUI.BeginChangeCheck();

            Vector3 newWorldPosition = Handles.Slider(
                worldPosition,
                worldDirection,
                size,
                Handles.CubeHandleCap,
                0f
            );

            if (!EditorGUI.EndChangeCheck())
                return;

            Undo.RecordObject(transform, "Resize Fu 3D Window");

            Vector3 worldDelta = newWorldPosition - worldPosition;
            float scaleDelta = getLocalAxisDeltaFromWorldDelta(transform, localDirection, worldDelta);

            Vector3 scale = transform.localScale;
            Vector3 position = transform.localPosition;

            if (localDirection.x > 0f)
            {
                float delta = scaleDelta;
                scale.x = Mathf.Max(MinSize, scale.x + delta);
                position += transform.localRotation * new Vector3(delta * 0.5f, 0f, 0f);
            }
            else if (localDirection.x < 0f)
            {
                float delta = scaleDelta;
                scale.x = Mathf.Max(MinSize, scale.x - delta);
                position += transform.localRotation * new Vector3(delta * 0.5f, 0f, 0f);
            }
            else if (localDirection.y > 0f)
            {
                float delta = scaleDelta;
                scale.y = Mathf.Max(MinSize, scale.y + delta);

                // Pivot bas : pas besoin de déplacer le transform quand on resize par le haut.
            }
            else if (localDirection.y < 0f)
            {
                float delta = scaleDelta;
                scale.y = Mathf.Max(MinSize, scale.y - delta);
                position += transform.localRotation * new Vector3(0f, delta, 0f);
            }

            scale.z = window.Depth;

            transform.localScale = scale;
            transform.localPosition = position;

            EditorUtility.SetDirty(window);
        }

        private float getLocalAxisDeltaFromWorldDelta(Transform transform, Vector3 localDirection, Vector3 worldDelta)
        {
            Vector3 localAxis = Mathf.Abs(localDirection.x) > 0f ? Vector3.right : Vector3.up;
            Vector3 worldAxis = transform.TransformDirection(localAxis).normalized;
            float worldDeltaOnAxis = Vector3.Dot(worldDelta, worldAxis);
            float parentScale = getParentScaleOnLocalAxis(transform, localAxis);
            return worldDeltaOnAxis / parentScale;
        }

        private float getParentScaleOnLocalAxis(Transform transform, Vector3 localDirection)
        {
            Vector3 axisInParentSpace = transform.localRotation * localDirection.normalized;
            Vector3 axisInWorld = transform.parent != null
                ? transform.parent.TransformVector(axisInParentSpace)
                : axisInParentSpace;

            return Mathf.Max(0.0001f, axisInWorld.magnitude);
        }

        private void EnforceEditorDepth(Transform transform)
        {
            Fu3DWindowBehaviour window = (Fu3DWindowBehaviour)target;
            Vector3 scale = transform.localScale;

            if (Mathf.Abs(scale.z - window.Depth) <= 0.0001f)
                return;

            Undo.RecordObject(transform, "Lock Fu 3D Window Depth");
            scale.z = window.Depth;
            transform.localScale = scale;
        }
    }
#endif
}
