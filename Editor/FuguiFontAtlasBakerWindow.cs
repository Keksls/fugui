using Fu;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Fu.Editor
{
    public sealed class FuguiFontAtlasBakerWindow : EditorWindow
    {
        private const string MenuPath = "Tools/Fugui/Font Atlas Baker";

        private FontConfig _fontConfig;
        private SerializedObject _serializedConfig;
        private SerializedProperty _useBakedAtlasProperty;
        private SerializedProperty _bakedAtlasFolderProperty;
        private SerializedProperty _bakedAtlasScalesProperty;
        private Vector2 _scroll;

        [MenuItem(MenuPath, priority = 2010)]
        public static void ShowWindow()
        {
            FuguiFontAtlasBakerWindow window = GetWindow<FuguiFontAtlasBakerWindow>("Font Atlas Baker");
            window.minSize = new Vector2(460f, 300f);
            window.Show();
        }

        private void OnEnable()
        {
            if (_fontConfig == null)
            {
                _fontConfig = FindDefaultFontConfig();
            }

            BindSerializedConfig();
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.LabelField("Fugui Font Atlas Baker", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            EditorGUI.BeginChangeCheck();
            _fontConfig = (FontConfig)EditorGUILayout.ObjectField("Font Config", _fontConfig, typeof(FontConfig), false);
            if (EditorGUI.EndChangeCheck())
            {
                BindSerializedConfig();
            }

            if (_fontConfig == null)
            {
                EditorGUILayout.HelpBox("Select a FontConfig asset to bake font atlas textures.", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            _serializedConfig.Update();
            EditorGUILayout.PropertyField(_useBakedAtlasProperty);
            EditorGUILayout.PropertyField(_bakedAtlasFolderProperty);
            EditorGUILayout.PropertyField(_bakedAtlasScalesProperty, true);
            _serializedConfig.ApplyModifiedProperties();

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                $"Atlases will be written under Assets/StreamingAssets/{_fontConfig.BakedFontAtlasFolder}/<font-hash>/scale_<scale>.png.",
                MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!CanBake()))
                {
                    if (GUILayout.Button("Bake Atlases", GUILayout.Height(28f)))
                    {
                        BakeAtlases();
                    }
                }

                if (GUILayout.Button("Refresh Hash", GUILayout.Height(28f), GUILayout.Width(110f)))
                {
                    FuFontAtlasCache.ClearHashCache();
                    Repaint();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void BindSerializedConfig()
        {
            _serializedConfig = _fontConfig != null ? new SerializedObject(_fontConfig) : null;
            _useBakedAtlasProperty = _serializedConfig?.FindProperty(nameof(FontConfig.UseBakedFontAtlas));
            _bakedAtlasFolderProperty = _serializedConfig?.FindProperty(nameof(FontConfig.BakedFontAtlasFolder));
            _bakedAtlasScalesProperty = _serializedConfig?.FindProperty(nameof(FontConfig.BakedFontAtlasScales));
        }

        private bool CanBake()
        {
            return _fontConfig != null &&
                   _fontConfig.BakedFontAtlasScales != null &&
                   _fontConfig.BakedFontAtlasScales.Any(scale => scale > 0f);
        }

        private void BakeAtlases()
        {
            if (!CanBake())
            {
                EditorUtility.DisplayDialog("Fugui Font Atlas Baker", "Add at least one positive font scale to bake.", "OK");
                return;
            }

            FuFontAtlasCache.ClearHashCache();
            Directory.CreateDirectory(Application.streamingAssetsPath);

            List<float> scales = _fontConfig.BakedFontAtlasScales
                .Where(scale => scale > 0f)
                .Select(scale => Mathf.Round(scale * 1000f) / 1000f)
                .Distinct()
                .OrderBy(scale => scale)
                .ToList();

            int bakedCount = 0;
            List<string> errors = new List<string>();

            try
            {
                EditorUtility.DisplayProgressBar("Fugui Font Atlas Baker", "Preparing font atlas bake...", 0f);

                for (int i = 0; i < scales.Count; i++)
                {
                    float scale = scales[i];
                    EditorUtility.DisplayProgressBar(
                        "Fugui Font Atlas Baker",
                        $"Baking scale {scale:0.###}",
                        (float)i / scales.Count);

                    if (!FuFontAtlasBaker.TryBuildTexture(_fontConfig, scale, Application.streamingAssetsPath, out Texture2D texture, out string error))
                    {
                        errors.Add($"Scale {scale:0.###}: {error}");
                        continue;
                    }

                    string relativePath = FuFontAtlasCache.GetAtlasRelativePath(_fontConfig, scale, Application.streamingAssetsPath);
                    string absolutePath = FuFontAtlasCache.CombineStreamingPath(Application.streamingAssetsPath, relativePath);
                    string directory = Path.GetDirectoryName(absolutePath);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
                    DestroyImmediate(texture);
                    bakedCount++;
                    Debug.Log($"[FontAtlasBaker] Baked font atlas scale {scale:0.###}: {absolutePath}");
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }

            string message = errors.Count == 0
                ? $"Baked {bakedCount} font atlas texture(s)."
                : $"Baked {bakedCount} font atlas texture(s).\n\n{string.Join("\n", errors)}";

            if (bakedCount > 0 && !_fontConfig.UseBakedFontAtlas)
            {
                Undo.RecordObject(_fontConfig, "Enable Fugui baked font atlas");
                _fontConfig.UseBakedFontAtlas = true;
                EditorUtility.SetDirty(_fontConfig);
                AssetDatabase.SaveAssets();
                message += "\n\nBaked atlas loading has been enabled on the FontConfig.";
            }

            EditorUtility.DisplayDialog("Fugui Font Atlas Baker", message, "OK");
        }

        private static FontConfig FindDefaultFontConfig()
        {
            string[] guids = AssetDatabase.FindAssets("FontConfig t:FontConfig");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                FontConfig config = AssetDatabase.LoadAssetAtPath<FontConfig>(path);
                if (config != null)
                {
                    return config;
                }
            }

            return null;
        }
    }
}
