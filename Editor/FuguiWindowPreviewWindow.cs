using Fu.Framework;
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Fu.Editor
{
    /// <summary>
    /// Unity Editor tool that previews a FuWindowBehaviour through the regular Fugui runtime pipeline.
    /// </summary>
    public sealed class FuguiWindowPreviewWindow : EditorWindow
    {
        private const string MenuPath = "Tools/Fugui/Window Preview";
        private const ushort PreviewFallbackWindowId = 65000;

        private static readonly BindingFlags InstanceFieldFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [SerializeField]
        private FuWindowBehaviour _target;
        [SerializeField]
        private Color _backgroundColor = new Color(0.176f, 0.176f, 0.176f, 1f);
        [SerializeField]
        private bool _livePreview = true;
        [SerializeField]
        private bool _interactivePreview = true;
        [SerializeField]
        private bool _autoUseSelection = true;

        private GameObject _previewRoot;
        private Camera _previewCamera;
        private FuController _previewController;
        private RenderTexture _previewTexture;
        private FuWindowDefinition _previewDefinition;
        private FuWindow _previewWindow;
        private FuWindowBehaviour _initializedTarget;
        private Vector2Int _previewSize = new Vector2Int(640, 360);
        private bool _initialized;
        private bool _needsRecreate = true;
        private bool _isRenderingFrame;
        private string _status;
        private MessageType _statusType = MessageType.Info;
        private Rect _textureDrawRect;
        private Vector2 _editorMousePosition = new Vector2(-1000f, -1000f);
        private Vector2 _editorMouseWheel;
        private readonly bool[] _editorMouseButtons = new bool[3];

        /// <summary>
        /// Opens the Fugui window preview.
        /// </summary>
        [MenuItem(MenuPath)]
        public static void Open()
        {
            FuguiWindowPreviewWindow window = GetWindow<FuguiWindowPreviewWindow>("Fugui Preview");
            window.minSize = new Vector2(420f, 320f);
            window.TryUseSelection();
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            Selection.selectionChanged += OnSelectionChanged;
            AssemblyReloadEvents.beforeAssemblyReload += CleanupPreviewRuntime;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            TryUseSelection();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            Selection.selectionChanged -= OnSelectionChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= CleanupPreviewRuntime;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            CleanupPreviewRuntime();
        }

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            DrawToolbar();
            DrawTargetControls();
            if (EditorGUI.EndChangeCheck())
            {
                MarkPreviewDirty();
            }

            Rect previewRect = GUILayoutUtility.GetRect(
                10f,
                100000f,
                10f,
                100000f,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));

            DrawPreview(previewRect);
            HandlePreviewEvents(Event.current);
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _livePreview = GUILayout.Toggle(_livePreview, "Live", EditorStyles.toolbarButton, GUILayout.Width(54f));
                _interactivePreview = GUILayout.Toggle(_interactivePreview, "Input", EditorStyles.toolbarButton, GUILayout.Width(58f));
                _autoUseSelection = GUILayout.Toggle(_autoUseSelection, "Selection", EditorStyles.toolbarButton, GUILayout.Width(78f));

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(_target == null))
                {
                    if (GUILayout.Button("Recreate", EditorStyles.toolbarButton, GUILayout.Width(74f)))
                    {
                        MarkPreviewDirty();
                    }
                }
            }
        }

        private void DrawTargetControls()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                FuWindowBehaviour nextTarget = (FuWindowBehaviour)EditorGUILayout.ObjectField(
                    "Window",
                    _target,
                    typeof(FuWindowBehaviour),
                    true);

                if (nextTarget != _target)
                {
                    _target = nextTarget;
                    MarkPreviewDirty();
                }

                _backgroundColor = EditorGUILayout.ColorField("Background", _backgroundColor);
            }
        }

        private void DrawPreview(Rect previewRect)
        {
            UpdatePreviewSize(previewRect);
            EditorGUI.DrawRect(previewRect, _backgroundColor);

            if (!string.IsNullOrEmpty(_status))
            {
                Rect helpRect = new Rect(previewRect.x + 8f, previewRect.y + 8f, previewRect.width - 16f, 42f);
                EditorGUI.HelpBox(helpRect, _status, _statusType);
            }

            if (_previewTexture == null)
            {
                _textureDrawRect = Rect.zero;
                return;
            }

            _textureDrawRect = previewRect;
            GUI.DrawTexture(_textureDrawRect, _previewTexture, ScaleMode.StretchToFill, false);
            Handles.BeginGUI();
            Handles.color = new Color(0f, 0f, 0f, 0.35f);
            Handles.DrawAAPolyLine(
                1f,
                new Vector3(_textureDrawRect.xMin, _textureDrawRect.yMin),
                new Vector3(_textureDrawRect.xMax, _textureDrawRect.yMin),
                new Vector3(_textureDrawRect.xMax, _textureDrawRect.yMax),
                new Vector3(_textureDrawRect.xMin, _textureDrawRect.yMax),
                new Vector3(_textureDrawRect.xMin, _textureDrawRect.yMin));
            Handles.EndGUI();
        }

        private void UpdatePreviewSize(Rect previewRect)
        {
            if (Event.current.type == EventType.Layout || previewRect.width <= 1f || previewRect.height <= 1f)
            {
                return;
            }

            Vector2Int nextSize = new Vector2Int(
                Mathf.Clamp(Mathf.RoundToInt(previewRect.width), 1, 8192),
                Mathf.Clamp(Mathf.RoundToInt(previewRect.height), 1, 8192));

            if (nextSize == _previewSize)
            {
                return;
            }

            _previewSize = nextSize;

            if (_previewCamera != null)
            {
                CreatePreviewTexture();
            }

            ForcePreviewWindowToContainer();
            Repaint();
        }

        private void HandlePreviewEvents(Event currentEvent)
        {
            if (!_interactivePreview || _textureDrawRect.width <= 0f || _textureDrawRect.height <= 0f)
            {
                _editorMousePosition = new Vector2(-1000f, -1000f);
                Array.Clear(_editorMouseButtons, 0, _editorMouseButtons.Length);
                return;
            }

            bool isInsidePreview = _textureDrawRect.Contains(currentEvent.mousePosition);
            if (isInsidePreview)
            {
                _editorMousePosition = GUIToPreviewPosition(currentEvent.mousePosition);
            }
            else if (currentEvent.rawType == EventType.MouseMove || currentEvent.rawType == EventType.Repaint)
            {
                _editorMousePosition = new Vector2(-1000f, -1000f);
            }

            switch (currentEvent.type)
            {
                case EventType.MouseDown:
                    if (isInsidePreview && TrySetMouseButton(currentEvent.button, true))
                    {
                        Focus();
                        currentEvent.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (TrySetMouseButton(currentEvent.button, false))
                    {
                        currentEvent.Use();
                    }
                    break;
                case EventType.MouseDrag:
                case EventType.MouseMove:
                    if (isInsidePreview)
                    {
                        currentEvent.Use();
                    }
                    break;
                case EventType.ScrollWheel:
                    if (isInsidePreview)
                    {
                        _editorMouseWheel += new Vector2(-currentEvent.delta.x, -currentEvent.delta.y);
                        currentEvent.Use();
                    }
                    break;
                case EventType.MouseLeaveWindow:
                    _editorMousePosition = new Vector2(-1000f, -1000f);
                    Array.Clear(_editorMouseButtons, 0, _editorMouseButtons.Length);
                    break;
            }
        }

        private Vector2 GUIToPreviewPosition(Vector2 guiPosition)
        {
            float normalizedX = Mathf.InverseLerp(_textureDrawRect.xMin, _textureDrawRect.xMax, guiPosition.x);
            float normalizedY = Mathf.InverseLerp(_textureDrawRect.yMin, _textureDrawRect.yMax, guiPosition.y);
            return new Vector2(
                normalizedX * _previewSize.x,
                normalizedY * _previewSize.y);
        }

        private bool TrySetMouseButton(int editorButton, bool pressed)
        {
            int index = editorButton switch
            {
                0 => 0,
                1 => 1,
                2 => 2,
                _ => -1
            };

            if (index < 0)
            {
                return false;
            }

            _editorMouseButtons[index] = pressed;
            return true;
        }

        private void OnEditorUpdate()
        {
            if (!_livePreview && !_needsRecreate)
            {
                return;
            }

            RenderPreviewFrame();
        }

        private void RenderPreviewFrame()
        {
            if (_isRenderingFrame)
            {
                return;
            }

            _isRenderingFrame = true;
            try
            {
                if (!EnsurePreviewRuntime())
                {
                    Repaint();
                    return;
                }

                Fugui.Update();
                Fugui.Render();

                RenderTexture previousActive = RenderTexture.active;
                try
                {
                    _previewCamera.Render();
                }
                finally
                {
                    RenderTexture.active = previousActive;
                }

                _status = null;
                Repaint();
            }
            catch (Exception exception)
            {
                _status = exception.Message;
                _statusType = MessageType.Error;
                Repaint();
            }
            finally
            {
                _isRenderingFrame = false;
                _needsRecreate = false;
            }
        }

        private bool EnsurePreviewRuntime()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SetStatus("Preview is disabled while Unity is entering or running Play Mode.", MessageType.Info);
                return false;
            }

            if (_target == null)
            {
                CleanupPreviewRuntime();
                SetStatus("Assign a FuWindowBehaviour or select one in the Hierarchy.", MessageType.Info);
                return false;
            }

            if (_initialized && !_needsRecreate && _initializedTarget == _target)
            {
                return true;
            }

            CleanupPreviewRuntime();

            FuSettings settings = FindPreviewSettings();
            if (settings == null)
            {
                SetStatus("No Fugui settings were found. Add/configure a FuController or keep the package prefab available.", MessageType.Error);
                return false;
            }

            FuguiRenderFeature feature = FindActiveRenderFeature();
            if (feature == null || feature._shader == null)
            {
                SetStatus("The active URP renderer does not have a configured FuguiRenderFeature.", MessageType.Warning);
                return false;
            }

            CreatePreviewObjects(feature);
            CreatePreviewTexture();

            Fugui.Initialize(settings, _previewController, _previewCamera, true);
            if (Fugui.DefaultContext == null)
            {
                SetStatus("Fugui did not create a preview context. Check settings, camera and font setup.", MessageType.Error);
                CleanupPreviewRuntime();
                return false;
            }

            Fugui.DefaultContext.AutoUpdateMouse = false;
            Fugui.DefaultContext.AutoUpdateKeyboard = false;
            Fugui.DefaultContext.OnPrepareFrame += FeedEditorInput;

            CreatePreviewWindow();

            _initialized = true;
            _initializedTarget = _target;
            return true;
        }

        private void CreatePreviewObjects(FuguiRenderFeature feature)
        {
            _previewRoot = new GameObject("Fugui Window Preview")
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            _previewController = _previewRoot.AddComponent<FuController>();
            _previewCamera = _previewRoot.AddComponent<Camera>();
            _previewRoot.layer = feature != null ? feature._cameraLayer : 5;

            _previewCamera.enabled = false;
            _previewCamera.pixelRect = new Rect(0f, 0f, _previewSize.x, _previewSize.y);
            _previewCamera.clearFlags = CameraClearFlags.SolidColor;
            _previewCamera.backgroundColor = _backgroundColor;
            _previewCamera.cullingMask = 0;
            _previewCamera.allowHDR = true;
            _previewCamera.allowMSAA = true;
            _previewCamera.depth = 1f;

            UniversalAdditionalCameraData cameraData = _previewRoot.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = false;
            cameraData.allowXRRendering = false;
        }

        private void CreatePreviewTexture()
        {
            ReleasePreviewTexture();

            if (_previewCamera == null)
            {
                return;
            }

            _previewCamera.pixelRect = new Rect(0f, 0f, _previewSize.x, _previewSize.y);

            RenderTextureDescriptor descriptor = new RenderTextureDescriptor(_previewSize.x, _previewSize.y)
            {
                depthBufferBits = 24,
                msaaSamples = 1,
                useMipMap = false,
                autoGenerateMips = false,
                useDynamicScale = false,
                graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm
            };

            _previewTexture = new RenderTexture(descriptor)
            {
                name = "Fugui Window Preview",
                hideFlags = HideFlags.HideAndDontSave
            };
            _previewTexture.Create();
            _previewCamera.targetTexture = _previewTexture;
        }

        private void CreatePreviewWindow()
        {
            FuWindowName windowName = GetPreviewWindowName(_target.GetWindowName(), _target.GetType());

            FuWindowFlags flags = GetFieldValue(_target, "_windowFlags", FuWindowFlags.Default) |
                FuWindowFlags.NoExternalization |
                FuWindowFlags.NoDocking |
                FuWindowFlags.NoClosable;
            FuExternalWindowFlags externalFlags = FuExternalWindowFlags.Default;
            FuWindowStyleFlags styleFlags = GetFieldValue(_target, "_windowStyleFlags", FuWindowStyleFlags.Default) |
                FuWindowStyleFlags.NoDocking |
                FuWindowStyleFlags.NoMove |
                FuWindowStyleFlags.NoResize |
                FuWindowStyleFlags.NoSavedSettings;
            FuLayer layer = GetFieldValue(_target, "_windowLayer", FuLayer.Normal);

            _previewDefinition = new FuWindowDefinition(
                windowName,
                layer,
                DrawTargetWindow,
                Vector2Int.zero,
                _previewSize,
                flags,
                externalFlags,
                styleFlags,
                FuWindowResizeSides.None);

            _target.OnWindowDefinitionCreated(_previewDefinition);
            ApplyPreviewDefinitionConstraints(_previewDefinition, _previewSize);
            _previewDefinition.OnUIWindowCreated += PreviewDefinition_OnUIWindowCreated;
            _previewWindow = Fugui.CreateWindow(windowName, true);

            if (_previewWindow == null)
            {
                SetStatus("Fugui could not create the preview window.", MessageType.Error);
            }
            else
            {
                ForcePreviewWindowToContainer();
                _previewWindow.ForceDraw();
            }
        }

        private static void ApplyPreviewDefinitionConstraints(FuWindowDefinition definition, Vector2Int size)
        {
            if (definition == null)
            {
                return;
            }

            definition
                .SetExternalizable(false)
                .SetDockable(false)
                .SetClosable(false)
                .SetPosition(Vector2Int.zero)
                .SetSize(size)
                .SetResizableSides(FuWindowResizeSides.None)
                .SetWindowStyleFlags(
                    definition.WindowStyleFlags |
                    FuWindowStyleFlags.NoDocking |
                    FuWindowStyleFlags.NoMove |
                    FuWindowStyleFlags.NoResize |
                    FuWindowStyleFlags.NoSavedSettings);
        }

        private void PreviewDefinition_OnUIWindowCreated(FuWindow window)
        {
            _previewWindow = window;
            SetFieldValue(_target, "_fuWindow", window);
            _target.OnWindowCreated(window);
            ForcePreviewWindowToContainer();
        }

        private void DrawTargetWindow(FuWindow window, FuLayout layout)
        {
            if (_target == null)
            {
                return;
            }

            _target.OnUI(window, layout);
        }

        private void ForcePreviewWindowToContainer()
        {
            if (_previewWindow == null)
            {
                return;
            }

            Vector2Int size = Fugui.DefaultContainer != null && Fugui.DefaultContainer.Size.x > 0 && Fugui.DefaultContainer.Size.y > 0
                ? Fugui.DefaultContainer.Size
                : _previewSize;

            if (size.x <= 0 || size.y <= 0)
            {
                return;
            }

            if (_previewDefinition != null && _previewDefinition.Size != size)
            {
                _previewDefinition.SetSize(size);
            }

            bool changed = _previewWindow.LocalPosition != Vector2Int.zero || _previewWindow.Size != size;
            _previewWindow.LocalPosition = Vector2Int.zero;
            _previewWindow.Size = size;

            if (changed)
            {
                _previewWindow.ForceDraw();
            }
        }

        private bool FeedEditorInput()
        {
            if (Fugui.DefaultContext == null)
            {
                return true;
            }

            Vector2 mousePosition = _interactivePreview
                ? _editorMousePosition
                : new Vector2(-1000f, -1000f);

            Fugui.DefaultContext.UpdateMouse(
                mousePosition,
                _editorMouseWheel,
                _interactivePreview && _editorMouseButtons[0],
                _interactivePreview && _editorMouseButtons[1],
                _interactivePreview && _editorMouseButtons[2]);

            ForcePreviewWindowToContainer();
            _editorMouseWheel = Vector2.zero;
            return true;
        }

        private void MarkPreviewDirty()
        {
            _needsRecreate = true;
            Repaint();
        }

        private void CleanupPreviewRuntime()
        {
            if (Fugui.DefaultContext != null)
            {
                Fugui.DefaultContext.OnPrepareFrame -= FeedEditorInput;
            }

            if (_initializedTarget != null)
            {
                SetFieldValue(_initializedTarget, "_fuWindow", null);
            }

            _previewDefinition = null;
            _previewWindow = null;
            _initialized = false;
            _initializedTarget = null;

            DestroyPreviewContexts();

            ReleasePreviewTexture();

            if (_previewRoot != null)
            {
                DestroyImmediate(_previewRoot);
                _previewRoot = null;
                _previewCamera = null;
                _previewController = null;
            }

            Array.Clear(_editorMouseButtons, 0, _editorMouseButtons.Length);
            _editorMousePosition = new Vector2(-1000f, -1000f);
            _editorMouseWheel = Vector2.zero;
        }

        private void ReleasePreviewTexture()
        {
            if (_previewTexture == null)
            {
                return;
            }

            if (_previewCamera != null && _previewCamera.targetTexture == _previewTexture)
            {
                _previewCamera.targetTexture = null;
            }

            _previewTexture.Release();
            DestroyImmediate(_previewTexture);
            _previewTexture = null;
        }

        private void SetStatus(string status, MessageType statusType)
        {
            _status = status;
            _statusType = statusType;
        }

        private void OnSelectionChanged()
        {
            if (!_autoUseSelection)
            {
                return;
            }

            if (TryUseSelection())
            {
                MarkPreviewDirty();
            }
        }

        private bool TryUseSelection()
        {
            FuWindowBehaviour selected = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponent<FuWindowBehaviour>()
                : Selection.activeObject as FuWindowBehaviour;

            if (selected == null || selected == _target)
            {
                return false;
            }

            _target = selected;
            return true;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.EnteredPlayMode)
            {
                CleanupPreviewRuntime();
                Repaint();
            }
        }

        private static FuWindowName GetPreviewWindowName(FuWindowName source, Type targetType)
        {
            string displayName = string.IsNullOrWhiteSpace(source.Name)
                ? targetType.Name
                : source.Name;

            if (source.ID == 0)
            {
                return new FuWindowName(PreviewFallbackWindowId, displayName, true, -1);
            }

            return new FuWindowName(source);
        }

        private static FuSettings FindPreviewSettings()
        {
            FuController sceneController = Resources.FindObjectsOfTypeAll<FuController>()
                .FirstOrDefault(controller =>
                    controller != null &&
                    !EditorUtility.IsPersistent(controller) &&
                    controller.gameObject.scene.IsValid());

            FuSettings sceneSettings = GetFieldValue<FuSettings>(sceneController, "_settings");
            if (sceneSettings != null)
            {
                return CreatePreviewSettings(sceneSettings);
            }

            string prefabPath = FindPackageAssetPath("Runtime/Resources/FuguiController.prefab");
            GameObject prefab = string.IsNullOrEmpty(prefabPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            FuController prefabController = prefab != null ? prefab.GetComponent<FuController>() : null;
            return CreatePreviewSettings(GetFieldValue<FuSettings>(prefabController, "_settings"));
        }

        private static FuSettings CreatePreviewSettings(FuSettings source)
        {
            if (source == null)
            {
                return null;
            }

            MethodInfo cloneMethod = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
            FuSettings settings = cloneMethod != null
                ? (FuSettings)cloneMethod.Invoke(source, null)
                : source;

            settings.EnableExternalizations = false;
            settings.NoDocking = true;
            settings.DisplaySettingsIfNoLayout = false;
            settings.DisplayOpenWindowsInMainMenu = false;
            settings.ConfigFlags &= ~(FuConfigFlags.DockingEnable | FuConfigFlags.ViewportsEnable);
            return settings;
        }

        private static void DestroyPreviewContexts()
        {
            if (Fugui.Contexts == null || Fugui.Contexts.Count == 0)
            {
                return;
            }

            FuContext[] contexts = Fugui.Contexts.Values.Where(context => context != null).ToArray();

            foreach (FuContext context in contexts)
            {
                try
                {
                    context.Stop();
                    InvokeNonPublic(context, "Destroy");
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("[Fugui Preview] Context cleanup failed: " + exception.Message);
                }
            }

            Fugui.Contexts.Clear();
            Fugui.UIWindows?.Clear();
            Fugui.UIWindowsDefinitions?.Clear();
            Fugui.SetCurrentContext(null);
            ClearQueuedContextDeletes();
            SetStaticProperty("DefaultContext", null);
            SetStaticProperty("DefaultContainer", null);
            SetStaticProperty("Settings", null);
            SetStaticProperty("Themes", null);
            SetStaticProperty("Layouts", null);
            SetStaticField("Controller", null);
            SetStaticField("_contextID", 0);
        }

        private static FuguiRenderFeature FindActiveRenderFeature()
        {
            ScriptableRendererData rendererData = GetDefaultRendererData();
            return rendererData == null
                ? null
                : rendererData.rendererFeatures.OfType<FuguiRenderFeature>().FirstOrDefault();
        }

        private static ScriptableRendererData GetDefaultRendererData()
        {
            RenderPipelineAsset pipelineAsset = GraphicsSettings.currentRenderPipeline;
            if (pipelineAsset == null)
            {
                pipelineAsset = QualitySettings.renderPipeline;
            }

            UniversalRenderPipelineAsset urpAsset = pipelineAsset as UniversalRenderPipelineAsset;
            if (urpAsset == null || urpAsset.rendererDataList == null || urpAsset.rendererDataList.Length == 0)
            {
                return null;
            }

            int index = 0;
            SerializedObject serializedAsset = new SerializedObject(urpAsset);
            SerializedProperty defaultRendererIndex = serializedAsset.FindProperty("m_DefaultRendererIndex");
            if (defaultRendererIndex != null)
            {
                index = Mathf.Clamp(defaultRendererIndex.intValue, 0, urpAsset.rendererDataList.Length - 1);
            }

            return urpAsset.rendererDataList[index];
        }

        private static string FindPackageAssetPath(string relativePath)
        {
            string normalizedRelativePath = relativePath.Replace('\\', '/').TrimStart('/');
            string suffix = "/" + normalizedRelativePath;
            string[] guids = AssetDatabase.FindAssets("Fugui t:AssemblyDefinitionAsset");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');
                if (path.EndsWith("/Runtime/Fugui.asmdef", StringComparison.Ordinal))
                {
                    string packageRoot = path.Substring(0, path.Length - "/Runtime/Fugui.asmdef".Length);
                    string candidate = packageRoot + suffix;
                    if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(candidate)))
                    {
                        return candidate;
                    }
                }
            }

            return null;
        }

        private static T GetFieldValue<T>(object instance, string fieldName, T fallback = default)
        {
            if (instance == null)
            {
                return fallback;
            }

            FieldInfo field = FindField(instance.GetType(), fieldName);
            if (field == null)
            {
                return fallback;
            }

            object value = field.GetValue(instance);
            return value is T typedValue ? typedValue : fallback;
        }

        private static void SetFieldValue(object instance, string fieldName, object value)
        {
            if (instance == null)
            {
                return;
            }

            FieldInfo field = FindField(instance.GetType(), fieldName);
            field?.SetValue(instance, value);
        }

        private static void InvokeNonPublic(object instance, string methodName)
        {
            if (instance == null)
            {
                return;
            }

            MethodInfo method = FindMethod(instance.GetType(), methodName);
            method?.Invoke(instance, null);
        }

        private static void ClearQueuedContextDeletes()
        {
            PropertyInfo property = typeof(Fugui).GetProperty("ToDeleteContexts", BindingFlags.Static | BindingFlags.NonPublic);
            object queue = property?.GetValue(null);
            queue?.GetType().GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public)?.Invoke(queue, null);
        }

        private static void SetStaticProperty(string propertyName, object value)
        {
            PropertyInfo property = typeof(Fugui).GetProperty(propertyName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            property?.SetValue(null, value);
        }

        private static void SetStaticField(string fieldName, object value)
        {
            FieldInfo field = typeof(Fugui).GetField(fieldName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            field?.SetValue(null, value);
        }

        private static FieldInfo FindField(Type type, string fieldName)
        {
            while (type != null)
            {
                FieldInfo field = type.GetField(fieldName, InstanceFieldFlags);
                if (field != null)
                {
                    return field;
                }

                type = type.BaseType;
            }

            return null;
        }

        private static MethodInfo FindMethod(Type type, string methodName)
        {
            while (type != null)
            {
                MethodInfo method = type.GetMethod(methodName, InstanceFieldFlags);
                if (method != null)
                {
                    return method;
                }

                type = type.BaseType;
            }

            return null;
        }
    }
}
