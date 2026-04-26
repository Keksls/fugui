using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Fu.Framework
{
    public class FuCameraWindowBehaviour : FuWindowBehaviour
    {
        [SerializeField]
        private Camera _camera;
        [SerializeField]
        private MSAASamples _msaa = MSAASamples.None;
        [SerializeField]
        private int _idleCameraFPS = 24;
        [SerializeField]
        private int _manipulatingCameraFPS = 60;
        public Camera Camera => _camera;
        public FuCameraWindow CameraWindow => _fuWindow as FuCameraWindow;
        [SerializeField]
        [Range(0.5f, 2f)]
        private float _superSampling = 2f;

        public override void FuguiAwake()
        {
            // creeate the window definition, it will automaticaly be registered into fugui windows definitions list
            FuCameraWindowDefinition windowDefinition = new FuCameraWindowDefinition(_windowName, _camera, _idleCameraFPS, _manipulatingCameraFPS, _msaa, OnUI, _position == Vector2Int.zero ? null : _position, _size == Vector2Int.zero ? null : _size, _windowFlags);
            windowDefinition.SetSupersampling(_superSampling);
            windowDefinition.SetCustomWindowType((winDef) => {
                return new FuCameraWindow((FuCameraWindowDefinition)winDef);
            });
            // call the OnWindowDefinitionCreated method to allow further customization
            OnWindowDefinitionCreated(windowDefinition);
            // register the OnUIWindowCreated event to handle the window creation
            windowDefinition.OnUIWindowCreated += WindowDefinition_OnUIWindowCreated;

            // Force the creation of the window immediately on Awake
            if (_forceCreateAloneOnAwake)
                Fugui.CreateWindow(_windowName, true);
        }
    }
}