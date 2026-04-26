using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
#endif

namespace Fu.Framework
{
    [ExecuteAlways]
    public class Fu3DWindowBehaviour : MonoBehaviour
    {
        public const float Depth = 0.01f;

        [SerializeField]
        protected FuWindowName _windowName;

        [SerializeField]
        protected FuWindowFlags _windowFlags = FuWindowFlags.Default;

        [SerializeField]
        protected bool _forceCreateAloneOnAwake = false;

        protected FuWindow _fuWindow;
        protected Fu3DWindowContainer _container;

        public FuWindow Window
        {
            get { return _fuWindow; }
        }

        public Fu3DWindowContainer Container
        {
            get { return _container; }
        }

        /// <summary>
        /// Register the window definition and create the window instance if needed.
        /// </summary>
        public virtual void FuguiAwake()
        {
            if (!enabled)
                return;

            FuWindowDefinition windowDefinition = new FuWindowDefinition(
                _windowName,
                OnUI,
                Vector2Int.zero,
                GetWindowSizeFromPlaceholder(),
                _windowFlags,
                FuExternalWindowFlags.Default
            );

            OnWindowDefinitionCreated(windowDefinition);

            if (_forceCreateAloneOnAwake)
            {
                FuWindow window = Fugui.CreateWindow(_windowName, false);
                if (window != null)
                {
                    WindowDefinition_OnUIWindowCreated(window);
                }
            }
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
            _container = Fugui.Add3DWindow(_fuWindow, transform.position, transform.rotation);

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
            _windowName = value;
        }

        private void LateUpdate()
        {
            EnforceDepth();
            ApplyPlaceholderToContainer();
        }

        private void ApplyPlaceholderToContainer()
        {
            if (_container == null || _container.IsClosed || _container.Window == null)
            {
                _container = null;
                return;
            }

            _container.SetPosition(transform.position);
            _container.SetRotation(transform.rotation);
            _container.SetLocalSize(GetPlaceholderSize());
        }

        private void Window_OnClosed(FuWindow window)
        {
            if (_fuWindow != window)
                return;

            _fuWindow.OnClosed -= Window_OnClosed;
            _fuWindow = null;
            _container = null;
        }

        private Vector2 GetPlaceholderSize()
        {
            Vector3 worldScale = transform.lossyScale;

            return new Vector2(
                Mathf.Abs(worldScale.x),
                Mathf.Abs(worldScale.y)
            );
        }

        private Vector2Int GetWindowSizeFromPlaceholder()
        {
            Vector2 placeholderSize = GetPlaceholderSize();

            return new Vector2Int(
                Mathf.Max(1, Mathf.RoundToInt(placeholderSize.x)),
                Mathf.Max(1, Mathf.RoundToInt(placeholderSize.y))
            );
        }

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
            EnforceDepth();
        }

        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.color = new Color(0f, 0.6f, 1f, 0.18f);
            Gizmos.DrawCube(new Vector3(0f, 0.5f, 0f), Vector3.one);

            Gizmos.color = new Color(0f, 0.6f, 1f, 0.75f);
            Gizmos.DrawWireCube(new Vector3(0f, 0.5f, 0f), Vector3.one);
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(Fu3DWindowBehaviour), true)]
    public class Fu3DWindowBehaviourEditor : Editor
    {
        private const float HandleSize = 0.08f;
        private const float MinSize = 0.01f;

        private List<FuWindowName> availableNames;
        private string[] windowNameOptions;
        private int selectedIndex;

        private SerializedProperty windowFlagsProp;
        private SerializedProperty forceCreateProp;

        protected readonly HashSet<string> _excludedProps = new HashSet<string>
        {
            "_windowName",
            "_windowFlags",
            "_forceCreateAloneOnAwake",
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

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "The 3D window size is driven by the placeholder scale X/Y. Scale Z is locked to 0.01.",
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
            Vector3 localDelta = transform.InverseTransformVector(worldDelta);

            Vector3 scale = transform.localScale;
            Vector3 position = transform.localPosition;

            if (localDirection.x > 0f)
            {
                float delta = localDelta.x;
                scale.x = Mathf.Max(MinSize, scale.x + delta);
                position += transform.localRotation * new Vector3(delta * 0.5f, 0f, 0f);
            }
            else if (localDirection.x < 0f)
            {
                float delta = localDelta.x;
                scale.x = Mathf.Max(MinSize, scale.x - delta);
                position += transform.localRotation * new Vector3(delta * 0.5f, 0f, 0f);
            }
            else if (localDirection.y > 0f)
            {
                float delta = localDelta.y;
                scale.y = Mathf.Max(MinSize, scale.y + delta);

                // Pivot bas : pas besoin de déplacer le transform quand on resize par le haut.
            }
            else if (localDirection.y < 0f)
            {
                float delta = localDelta.y;
                scale.y = Mathf.Max(MinSize, scale.y - delta);
                position += transform.localRotation * new Vector3(0f, delta, 0f);
            }

            scale.z = Fu3DWindowBehaviour.Depth;

            transform.localScale = scale;
            transform.localPosition = position;

            EditorUtility.SetDirty(window);
        }

        private void EnforceEditorDepth(Transform transform)
        {
            Vector3 scale = transform.localScale;

            if (Mathf.Abs(scale.z - Fu3DWindowBehaviour.Depth) <= 0.0001f)
                return;

            Undo.RecordObject(transform, "Lock Fu 3D Window Depth");
            scale.z = Fu3DWindowBehaviour.Depth;
            transform.localScale = scale;
        }
    }
#endif
}
