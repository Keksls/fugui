using Fu.Core;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Fu.Framework
{
    public class FuCameraWindowBehaviour : FuWindowBehaviour
    {
        [SerializeField]
        private Camera _camera;
        public Camera Camera => _camera;
        public FuCameraWindow CameraWindow => _fuWindow as FuCameraWindow;
        [SerializeField]
        [Range(0.5f, 2f)]
        private float _superSampling = 2f;

        public override void FuguiAwake()
        {
            // creeate the window definition, it will automaticaly be registered into fugui windows definitions list
            FuCameraWindowDefinition windowDefinition = new FuCameraWindowDefinition(_windowName, _camera, OnUI, _position == Vector2Int.zero ? null : _position, _size == Vector2Int.zero ? null : _size, _windowFlags);
            windowDefinition.SetSupersampling(_superSampling);
            windowDefinition.SetCustomWindowType<FuCameraWindow>();
            // call the OnWindowDefinitionCreated method to allow further customization
            OnWindowDefinitionCreated(windowDefinition);
            // register the OnUIWindowCreated event to handle the window creation
            windowDefinition.OnUIWindowCreated += WindowDefinition_OnUIWindowCreated;

            // Force the creation of the window immediately on Awake
            if (_forceCreateAloneOnAwake)
                Fugui.CreateWindowAsync(_windowName, null, true);
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(FuCameraWindowBehaviour))]
    public class FuCameraWindowBehaviourEditor : FuWindowBehaviourEditor
    {
        private SerializedProperty _cameraProp;
        private SerializedProperty _superSamplingProp;

        public override void OnEnable()
        {
            base.OnEnable(); // Initialise les propriétés de base

            _cameraProp = serializedObject.FindProperty("_camera");
            _superSamplingProp = serializedObject.FindProperty("_superSampling");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI(); // Affiche les propriétés communes

            // Affiche les propriétés spécifiques à FuCameraWindowBehaviour
            EditorGUILayout.LabelField("Camera Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_cameraProp);
            EditorGUILayout.PropertyField(_superSamplingProp);
            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}