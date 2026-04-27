using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Fu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Fu.Editor
{
    public sealed class FuguiSetupWizard : EditorWindow
    {
        private const string MenuPath = "Tools/Fugui/Setup Wizard";
        private const string StreamingAssetsFolder = "Assets/StreamingAssets/Fugui";
        private const string FontsFolder = "Fugui/Fonts/current/";
        private const string ThemesFolder = "Fugui/Themes";
        private const string LayoutsFolder = "Fugui/Layouts";
        private const int DefaultUiLayer = 5;

        private readonly List<SetupCheck> _checks = new List<SetupCheck>();
        private Vector2 _scroll;
        private string _packageRoot;
        private Texture2D _logo;
        private PipelineKind _pipelineKind;
        private RenderPipelineAsset _pipelineAsset;

        [MenuItem(MenuPath, priority = 2000)]
        public static void ShowWindow()
        {
            var window = GetWindow<FuguiSetupWizard>("Fugui Setup");
            window.minSize = new Vector2(520f, 520f);
            window.Refresh();
            window.Show();
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void OnGUI()
        {
            DrawHeader();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh", GUILayout.Height(28f)))
                {
                    Refresh();
                }

                using (new EditorGUI.DisabledScope(!_checks.Any(check => check.CanFix && check.Selected)))
                {
                    if (GUILayout.Button("Apply Selected", GUILayout.Height(28f)))
                    {
                        ApplySelected();
                    }
                }

                using (new EditorGUI.DisabledScope(!_checks.Any(check => check.CanFix && check.State != CheckState.Ok)))
                {
                    if (GUILayout.Button("Fix All", GUILayout.Height(28f)))
                    {
                        foreach (var check in _checks)
                        {
                            check.Selected = check.CanFix && check.State != CheckState.Ok;
                        }

                        ApplySelected();
                    }
                }
            }

            EditorGUILayout.Space(8f);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            foreach (var check in _checks)
            {
                DrawCheck(check);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                if (_logo != null)
                {
                    GUILayout.Label(_logo, GUILayout.Width(72f), GUILayout.Height(72f));
                }

                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Space(6f);
                    EditorGUILayout.LabelField("Fugui Setup Wizard", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Pipeline: {GetPipelineLabel()}", EditorStyles.wordWrappedLabel);
                    EditorGUILayout.LabelField($"Package: {(_packageRoot ?? "not found")}", EditorStyles.miniLabel);
                }
            }
        }

        private void DrawCheck(SetupCheck check)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (check.CanFix && check.State != CheckState.Ok)
                    {
                        check.Selected = EditorGUILayout.Toggle(check.Selected, GUILayout.Width(18f));
                    }
                    else
                    {
                        GUILayout.Space(22f);
                    }

                    EditorGUILayout.LabelField(GetStateLabel(check.State), GUILayout.Width(70f));
                    EditorGUILayout.LabelField(check.Title, EditorStyles.boldLabel);
                }

                EditorGUILayout.LabelField(check.Details, EditorStyles.wordWrappedLabel);
            }
        }

        private void Refresh()
        {
            _checks.Clear();
            _packageRoot = FindPackageRoot();
            _logo = LoadAsset<Texture2D>("Logo/ColorFull/FuGui_Full_Logo_NP_256.png");
            DetectPipeline();

            AddPackageCheck();
            AddPipelineCheck();
            AddRenderFeatureCheck();
            AddStreamingAssetsCheck();
            AddFontConfigCheck();
            AddInputCheck();
            AddControllerCheck();
            AddSceneCameraCheck();

            Repaint();
        }

        private void ApplySelected()
        {
            AssetDatabase.StartAssetEditing();

            try
            {
                foreach (var check in _checks.Where(check => check.Selected && check.CanFix).ToArray())
                {
                    check.Fix();
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Fugui Setup", exception.Message, "OK");
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Refresh();
        }

        private void AddPackageCheck()
        {
            bool found = !string.IsNullOrEmpty(_packageRoot);
            _checks.Add(new SetupCheck(
                "Fugui package assets",
                found
                    ? "Runtime resources, shaders, fonts and setup assets were found."
                    : "The wizard could not locate Runtime/Resources/FuguiController.prefab.",
                found ? CheckState.Ok : CheckState.Error));
        }

        private void AddPipelineCheck()
        {
            CheckState state;
            string details;

            switch (_pipelineKind)
            {
                case PipelineKind.URP:
                    state = CheckState.Ok;
                    details = $"URP is active: {_pipelineAsset.name}. The wizard can install or repair FuguiRenderFeature.";
                    break;
                case PipelineKind.HDRP:
                    state = CheckState.Warning;
                    details = "HDRP is active. The HDRP Fugui mesh shader is available, but this package currently exposes an URP ScriptableRendererFeature installer only.";
                    break;
                case PipelineKind.BuiltIn:
                    state = CheckState.Error;
                    details = "The built-in render pipeline is active. Fugui rendering requires a supported Scriptable Render Pipeline setup.";
                    break;
                default:
                    state = CheckState.Warning;
                    details = _pipelineAsset != null
                        ? $"A custom render pipeline is active: {_pipelineAsset.GetType().Name}."
                        : "No render pipeline asset was detected.";
                    break;
            }

            _checks.Add(new SetupCheck("Render pipeline", details, state));
        }

        private void AddRenderFeatureCheck()
        {
            if (_pipelineKind != PipelineKind.URP)
            {
                Shader hdrpShader = LoadAsset<Shader>("Runtime/Resources/Shaders/Fugui_HDRP_Mesh.shader");
                _checks.Add(new SetupCheck(
                    "Render feature",
                    hdrpShader != null
                        ? "URP render feature setup is skipped for this pipeline. HDRP shader asset is present for custom HDRP integration."
                        : "URP render feature setup is skipped for this pipeline, and the HDRP shader asset was not found.",
                    _pipelineKind == PipelineKind.HDRP && hdrpShader != null ? CheckState.Warning : CheckState.Error));
                return;
            }

            ScriptableRendererData rendererData = GetDefaultRendererData();
            Shader shader = LoadAsset<Shader>("Runtime/Resources/Shaders/Fugui_URP_Mesh.shader") ?? Shader.Find("Fugui/URP_Mesh");
            FuguiRenderFeature feature = FindRenderFeature(rendererData);

            if (rendererData == null)
            {
                _checks.Add(new SetupCheck("Render feature", "No default URP renderer data was found on the active pipeline asset.", CheckState.Error));
                return;
            }

            if (feature != null && feature._shader == shader && feature._cameraLayer == GetUiLayer())
            {
                _checks.Add(new SetupCheck(
                    "Render feature",
                    $"FuguiRenderFeature is installed on {rendererData.name} with the URP shader and UI camera layer.",
                    CheckState.Ok));
                return;
            }

            _checks.Add(new SetupCheck(
                "Render feature",
                feature == null
                    ? $"FuguiRenderFeature is missing from {rendererData.name}."
                    : "FuguiRenderFeature exists, but the shader or camera layer should be repaired.",
                shader != null ? CheckState.Warning : CheckState.Error,
                shader != null,
                ConfigureUrpRenderFeature));
        }

        private void AddStreamingAssetsCheck()
        {
            bool complete = HasStreamingAssets();
            _checks.Add(new SetupCheck(
                "StreamingAssets",
                complete
                    ? "Fonts, themes and layouts are present under Assets/StreamingAssets/Fugui."
                    : "Fugui runtime assets should be copied to Assets/StreamingAssets/Fugui so Application.streamingAssetsPath can find them.",
                complete ? CheckState.Ok : CheckState.Warning,
                !complete && HasPackagePath("StreamingAssets/Fugui"),
                CopyStreamingAssets));
        }

        private void AddFontConfigCheck()
        {
            FontConfig fontConfig = LoadAsset<FontConfig>("Runtime/Resources/FontConfig.asset");
            bool fontConfigOk = fontConfig != null && NormalizeFolder(fontConfig.FontsFolder) == NormalizeFolder(FontsFolder);
            bool fontFilesOk = HasRequiredFontFiles();

            if (fontConfigOk && fontFilesOk)
            {
                _checks.Add(new SetupCheck("Fonts", "FontConfig.asset is valid and the regular, bold and icon fonts exist in StreamingAssets.", CheckState.Ok));
                return;
            }

            _checks.Add(new SetupCheck(
                "Fonts",
                fontConfig == null
                    ? "FontConfig.asset was not found in Runtime/Resources."
                    : "FontConfig.asset or the StreamingAssets font files need setup.",
                fontConfig != null ? CheckState.Warning : CheckState.Error,
                fontConfig != null && HasPackagePath("StreamingAssets/Fugui/Fonts"),
                () =>
                {
                    CopyStreamingAssets();
                    ConfigureControllers();
                }));
        }

        private void AddInputCheck()
        {
            int? inputHandling = GetActiveInputHandling();
            bool canSet = CanSetActiveInputHandling();

            if (!inputHandling.HasValue)
            {
                _checks.Add(new SetupCheck("Input", "The active input handling setting could not be read. Fugui will still auto-detect legacy vs Input System at runtime.", CheckState.Warning));
                return;
            }

            if (inputHandling.Value == 2)
            {
                _checks.Add(new SetupCheck("Input", "Active Input Handling is set to Both, which is the most forgiving mode for Fugui projects.", CheckState.Ok));
                return;
            }

            _checks.Add(new SetupCheck(
                "Input",
                $"Active Input Handling is {InputHandlingLabel(inputHandling.Value)}. Fugui can run with this, but Both avoids surprises when samples or user code mix input APIs.",
                CheckState.Warning,
                canSet,
                SetActiveInputHandlingBoth));
        }

        private void AddControllerCheck()
        {
            FuController[] controllers = FindControllers();

            if (controllers.Length > 0)
            {
                _checks.Add(new SetupCheck(
                    "FuguiController prefab",
                    controllers.Length == 1
                        ? "A FuController is present in the current scene."
                        : $"{controllers.Length} FuController instances are present. That can be intentional, but most projects should keep only one main controller.",
                    controllers.Length == 1 ? CheckState.Ok : CheckState.Warning,
                    true,
                    ConfigureControllers));
                return;
            }

            _checks.Add(new SetupCheck(
                "FuguiController prefab",
                "No FuController was found in the current scene. The wizard can add Runtime/Resources/FuguiController.prefab.",
                CheckState.Warning,
                HasPackagePath("Runtime/Resources/FuguiController.prefab"),
                AddControllerPrefab));
        }

        private void AddSceneCameraCheck()
        {
            FuController[] controllers = FindControllers();

            if (controllers.Length == 0)
            {
                _checks.Add(new SetupCheck("UI camera", "No FuController is present yet, so the UI camera will be created when the prefab is added.", CheckState.Warning));
                return;
            }

            bool allOk = controllers.All(HasValidUiCamera);
            _checks.Add(new SetupCheck(
                "UI camera",
                allOk
                    ? "Every FuController has a UI camera on the layer used by FuguiRenderFeature."
                    : "One or more FuController instances need a UI camera reference or layer repair.",
                allOk ? CheckState.Ok : CheckState.Warning,
                !allOk,
                ConfigureControllers));
        }

        private void ConfigureUrpRenderFeature()
        {
            ScriptableRendererData rendererData = GetDefaultRendererData();

            if (rendererData == null)
            {
                throw new InvalidOperationException("No default URP renderer data found.");
            }

            Shader shader = LoadAsset<Shader>("Runtime/Resources/Shaders/Fugui_URP_Mesh.shader") ?? Shader.Find("Fugui/URP_Mesh");

            if (shader == null)
            {
                throw new InvalidOperationException("Fugui URP shader was not found.");
            }

            FuguiRenderFeature feature = FindRenderFeature(rendererData);
            bool created = feature == null;

            if (created)
            {
                feature = CreateInstance<FuguiRenderFeature>();
                feature.name = nameof(FuguiRenderFeature);
                Undo.RegisterCreatedObjectUndo(feature, "Add Fugui Render Feature");

                if (EditorUtility.IsPersistent(rendererData))
                {
                    AssetDatabase.AddObjectToAsset(feature, rendererData);
                }
            }

            Undo.RecordObject(feature, "Configure Fugui Render Feature");
            feature._shader = shader;
            feature._cameraLayer = GetUiLayer();
            feature.PassEvent = RenderPassEvent.AfterRenderingTransparents;
            feature.SetActive(true);
            EditorUtility.SetDirty(feature);

            if (created)
            {
                AddFeatureToRendererData(rendererData, feature);
            }
            else
            {
                EditorUtility.SetDirty(rendererData);
            }
        }

        private void AddFeatureToRendererData(ScriptableRendererData rendererData, FuguiRenderFeature feature)
        {
            var serializedRenderer = new SerializedObject(rendererData);
            SerializedProperty features = serializedRenderer.FindProperty("m_RendererFeatures");
            SerializedProperty featureMap = serializedRenderer.FindProperty("m_RendererFeatureMap");

            serializedRenderer.Update();
            int index = features.arraySize;
            features.arraySize++;
            features.GetArrayElementAtIndex(index).objectReferenceValue = feature;

            if (featureMap != null)
            {
                featureMap.arraySize++;
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string _, out long localId);
                featureMap.GetArrayElementAtIndex(featureMap.arraySize - 1).longValue = localId;
            }

            serializedRenderer.ApplyModifiedProperties();
            EditorUtility.SetDirty(rendererData);
        }

        private void CopyStreamingAssets()
        {
            string source = GetPackagePath("StreamingAssets/Fugui");
            string destination = Path.GetFullPath(StreamingAssetsFolder);

            if (string.IsNullOrEmpty(source) || !Directory.Exists(source))
            {
                throw new DirectoryNotFoundException("Fugui source StreamingAssets folder was not found.");
            }

            if (Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar) == destination.TrimEnd(Path.DirectorySeparatorChar))
            {
                return;
            }

            CopyDirectoryWithoutMeta(source, destination);
        }

        private void AddControllerPrefab()
        {
            string prefabPath = GetPackagePath("Runtime/Resources/FuguiController.prefab");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab == null)
            {
                throw new FileNotFoundException("FuguiController.prefab was not found.", prefabPath);
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, SceneManager.GetActiveScene());
            Undo.RegisterCreatedObjectUndo(instance, "Add Fugui Controller");
            instance.name = "FuguiController";
            ConfigureController(instance.GetComponent<FuController>());
            EditorSceneManager.MarkSceneDirty(instance.scene);
        }

        private void ConfigureControllers()
        {
            foreach (FuController controller in FindControllers())
            {
                ConfigureController(controller);
            }
        }

        private void ConfigureController(FuController controller)
        {
            if (controller == null)
            {
                return;
            }

            Undo.RecordObject(controller, "Configure Fugui Controller");

            SerializedObject serializedController = new SerializedObject(controller);
            SerializedProperty settings = serializedController.FindProperty("_settings");
            SerializedProperty uiCameraProperty = serializedController.FindProperty("_uiCamera");
            Camera uiCamera = uiCameraProperty.objectReferenceValue as Camera;

            if (uiCamera == null)
            {
                uiCamera = controller.GetComponentsInChildren<Camera>(true).FirstOrDefault(camera => camera.name == "UI Camera")
                    ?? CreateUiCamera(controller.transform);
            }

            ConfigureUiCamera(uiCamera);
            uiCameraProperty.objectReferenceValue = uiCamera;

            SetRelativeObject(settings, "FontConfig", LoadAsset<FontConfig>("Runtime/Resources/FontConfig.asset"));
            SetRelativeObject(settings, "InfoIcon", LoadAsset<Texture2D>("Runtime/Resources/Images/info.png"));
            SetRelativeObject(settings, "WarningIcon", LoadAsset<Texture2D>("Runtime/Resources/Images/warning.png"));
            SetRelativeObject(settings, "DangerIcon", LoadAsset<Texture2D>("Runtime/Resources/Images/error.png"));
            SetRelativeObject(settings, "SuccessIcon", LoadAsset<Texture2D>("Runtime/Resources/Images/success.png"));
            SetRelativeObject(settings, "FuguiLogo", LoadAsset<Texture2D>("Logo/ColorFull/FuGui_Full_Logo_NP_512.png"));
            SetRelativeObject(settings, "UIPanelMaterial", LoadAsset<Material>("Runtime/Framework/3DWindow/UIPanelMaterial.mat"));
            SetRelativeObject(settings, "UIMaterial", LoadAsset<Material>("Runtime/Framework/3DWindow/UIMaterial.mat"));
            SetRelativeObject(settings, "CursorShapes", LoadAsset<CursorShapesAsset>("Runtime/Resources/Cursors/cursors_default.asset"));
            SetRelativeString(settings, "ThemesFolder", ThemesFolder);
            SetRelativeString(settings, "LayoutsFolder", LayoutsFolder);
            SetRelativeLayerMask(settings, "UILayer", 1 << GetUiLayer());

            serializedController.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        }

        private Camera CreateUiCamera(Transform parent)
        {
            GameObject cameraObject = new GameObject("UI Camera");
            Undo.RegisterCreatedObjectUndo(cameraObject, "Create Fugui UI Camera");
            cameraObject.transform.SetParent(parent, false);
            Camera camera = cameraObject.AddComponent<Camera>();
            return camera;
        }

        private void ConfigureUiCamera(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            Undo.RecordObject(camera.gameObject, "Configure Fugui UI Camera");
            Undo.RecordObject(camera, "Configure Fugui UI Camera");

            camera.gameObject.layer = GetUiLayer();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.176f, 0.176f, 0.176f, 1f);
            camera.cullingMask = 0;
            camera.depth = 1f;
            camera.allowHDR = true;
            camera.allowMSAA = true;

            UniversalAdditionalCameraData additionalCameraData = camera.GetComponent<UniversalAdditionalCameraData>();

            if (_pipelineKind == PipelineKind.URP && additionalCameraData == null)
            {
                additionalCameraData = Undo.AddComponent<UniversalAdditionalCameraData>(camera.gameObject);
            }

            if (additionalCameraData != null)
            {
                Undo.RecordObject(additionalCameraData, "Configure Fugui UI Camera");
                additionalCameraData.renderPostProcessing = false;
                EditorUtility.SetDirty(additionalCameraData);
            }

            EditorUtility.SetDirty(camera);
            EditorUtility.SetDirty(camera.gameObject);
        }

        private bool HasValidUiCamera(FuController controller)
        {
            SerializedObject serializedController = new SerializedObject(controller);
            Camera camera = serializedController.FindProperty("_uiCamera").objectReferenceValue as Camera;
            return camera != null && camera.gameObject.layer == GetUiLayer();
        }

        private FuguiRenderFeature FindRenderFeature(ScriptableRendererData rendererData)
        {
            return rendererData == null ? null : rendererData.rendererFeatures.OfType<FuguiRenderFeature>().FirstOrDefault();
        }

        private ScriptableRendererData GetDefaultRendererData()
        {
            var urpAsset = _pipelineAsset as UniversalRenderPipelineAsset;

            if (urpAsset == null)
            {
                return null;
            }

            var rendererDataList = urpAsset.rendererDataList;

            if (rendererDataList.Length == 0)
            {
                return null;
            }

            int index = 0;
            var serializedAsset = new SerializedObject(urpAsset);
            SerializedProperty defaultRendererIndex = serializedAsset.FindProperty("m_DefaultRendererIndex");

            if (defaultRendererIndex != null)
            {
                index = Mathf.Clamp(defaultRendererIndex.intValue, 0, rendererDataList.Length - 1);
            }

            return rendererDataList[index];
        }

        private void DetectPipeline()
        {
            _pipelineAsset = GraphicsSettings.currentRenderPipeline;

            if (_pipelineAsset == null)
            {
                _pipelineAsset = QualitySettings.renderPipeline;
            }

            if (_pipelineAsset == null)
            {
                _pipelineKind = PipelineKind.BuiltIn;
                return;
            }

            string typeName = _pipelineAsset.GetType().FullName;

            if (typeName.Contains("UniversalRenderPipelineAsset"))
            {
                _pipelineKind = PipelineKind.URP;
            }
            else if (typeName.Contains("HDRenderPipelineAsset"))
            {
                _pipelineKind = PipelineKind.HDRP;
            }
            else
            {
                _pipelineKind = PipelineKind.Custom;
            }
        }

        private string FindPackageRoot()
        {
            string[] prefabGuids = AssetDatabase.FindAssets("FuguiController t:Prefab");

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');

                if (path.EndsWith("/Runtime/Resources/FuguiController.prefab", StringComparison.Ordinal))
                {
                    return path.Substring(0, path.Length - "/Runtime/Resources/FuguiController.prefab".Length);
                }
            }

            string[] asmdefGuids = AssetDatabase.FindAssets("Fugui t:AssemblyDefinitionAsset");

            foreach (string guid in asmdefGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');

                if (path.EndsWith("/Runtime/Fugui.asmdef", StringComparison.Ordinal))
                {
                    return path.Substring(0, path.Length - "/Runtime/Fugui.asmdef".Length);
                }
            }

            return null;
        }

        private T LoadAsset<T>(string relativePath) where T : UnityEngine.Object
        {
            string path = GetPackagePath(relativePath);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private string GetPackagePath(string relativePath)
        {
            return string.IsNullOrEmpty(_packageRoot) ? null : $"{_packageRoot}/{relativePath}".Replace('\\', '/');
        }

        private bool HasPackagePath(string relativePath)
        {
            string path = GetPackagePath(relativePath);

            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            string absolute = Path.GetFullPath(path);
            return File.Exists(absolute) || Directory.Exists(absolute);
        }

        private bool HasStreamingAssets()
        {
            return File.Exists(Path.GetFullPath($"{StreamingAssetsFolder}/Fonts/current/regular.ttf"))
                && File.Exists(Path.GetFullPath($"{StreamingAssetsFolder}/Fonts/current/bold.ttf"))
                && File.Exists(Path.GetFullPath($"{StreamingAssetsFolder}/Fonts/current/icons.ttf"))
                && File.Exists(Path.GetFullPath($"{StreamingAssetsFolder}/Themes/themes_index.json"))
                && File.Exists(Path.GetFullPath($"{StreamingAssetsFolder}/Layouts/layouts_index.json"));
        }

        private bool HasRequiredFontFiles()
        {
            return File.Exists(Path.GetFullPath($"{StreamingAssetsFolder}/Fonts/current/regular.ttf"))
                && File.Exists(Path.GetFullPath($"{StreamingAssetsFolder}/Fonts/current/bold.ttf"))
                && File.Exists(Path.GetFullPath($"{StreamingAssetsFolder}/Fonts/current/icons.ttf"));
        }

        private static void CopyDirectoryWithoutMeta(string source, string destination)
        {
            Directory.CreateDirectory(destination);

            foreach (string directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                string relative = MakeRelativePath(source, directory);
                Directory.CreateDirectory(Path.Combine(destination, relative));
            }

            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase) || Path.GetFileName(file) == ".DS_Store")
                {
                    continue;
                }

                string relative = MakeRelativePath(source, file);
                string target = Path.Combine(destination, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(file, target, true);
            }
        }

        private static string MakeRelativePath(string root, string path)
        {
            string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string fullPath = Path.GetFullPath(path);

            if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetFileName(path);
            }

            return fullPath.Substring(fullRoot.Length);
        }

        private FuController[] FindControllers()
        {
            return UnityEngine.Object.FindObjectsByType<FuController>(FindObjectsInactive.Include);
        }

        private static void SetRelativeObject(SerializedProperty root, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = root?.FindPropertyRelative(propertyName);

            if (property != null && value != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void SetRelativeString(SerializedProperty root, string propertyName, string value)
        {
            SerializedProperty property = root?.FindPropertyRelative(propertyName);

            if (property != null)
            {
                property.stringValue = value;
            }
        }

        private static void SetRelativeLayerMask(SerializedProperty root, string propertyName, int value)
        {
            SerializedProperty property = root?.FindPropertyRelative(propertyName);
            SerializedProperty bits = property?.FindPropertyRelative("m_Bits");

            if (bits != null)
            {
                bits.intValue = value;
            }
        }

        private static int GetUiLayer()
        {
            int layer = LayerMask.NameToLayer("UI");
            return layer >= 0 ? layer : DefaultUiLayer;
        }

        private string GetPipelineLabel()
        {
            return _pipelineAsset != null ? $"{_pipelineKind} ({_pipelineAsset.name})" : _pipelineKind.ToString();
        }

        private static string GetStateLabel(CheckState state)
        {
            switch (state)
            {
                case CheckState.Ok:
                    return "OK";
                case CheckState.Warning:
                    return "Action";
                case CheckState.Error:
                    return "Error";
                default:
                    return "Info";
            }
        }

        private static string NormalizeFolder(string folder)
        {
            return (folder ?? string.Empty).Replace('\\', '/').Trim().TrimEnd('/') + "/";
        }

        private static int? GetActiveInputHandling()
        {
            PropertyInfo property = typeof(PlayerSettings).GetProperty("activeInputHandling", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (property == null || !property.CanRead)
            {
                return null;
            }

            object value = property.GetValue(null, null);
            return Convert.ToInt32(value);
        }

        private static bool CanSetActiveInputHandling()
        {
            PropertyInfo property = typeof(PlayerSettings).GetProperty("activeInputHandling", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            return property != null && property.CanWrite;
        }

        private static void SetActiveInputHandlingBoth()
        {
            PropertyInfo property = typeof(PlayerSettings).GetProperty("activeInputHandling", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (property == null || !property.CanWrite)
            {
                throw new InvalidOperationException("PlayerSettings.activeInputHandling is not writable in this Unity version.");
            }

            object enumValue = Enum.ToObject(property.PropertyType, 2);
            property.SetValue(null, enumValue, null);
            AssetDatabase.SaveAssets();
            Debug.Log("[Fugui] Active Input Handling set to Both. Unity may ask for an editor restart.");
        }

        private static string InputHandlingLabel(int value)
        {
            switch (value)
            {
                case 0:
                    return "Input Manager";
                case 1:
                    return "Input System";
                case 2:
                    return "Both";
                default:
                    return $"Unknown ({value})";
            }
        }

        private enum PipelineKind
        {
            BuiltIn,
            URP,
            HDRP,
            Custom
        }

        private enum CheckState
        {
            Ok,
            Warning,
            Error
        }

        private sealed class SetupCheck
        {
            public readonly string Title;
            public readonly string Details;
            public readonly CheckState State;
            public readonly bool CanFix;
            public readonly Action Fix;
            public bool Selected;

            public SetupCheck(string title, string details, CheckState state, bool canFix = false, Action fix = null)
            {
                Title = title;
                Details = details;
                State = state;
                CanFix = canFix && fix != null;
                Fix = fix;
                Selected = CanFix && state != CheckState.Ok;
            }
        }
    }
}
