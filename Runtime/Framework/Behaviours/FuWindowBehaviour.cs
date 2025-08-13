using Fu.Core;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Fu.Framework
{
    public class FuWindowBehaviour : MonoBehaviour
    {
        [SerializeField]
        protected FuWindowName _windowName;
        [SerializeField]
        protected FuWindowFlags _windowFlags = FuWindowFlags.Default;
        [SerializeField]
        protected Vector2Int _size = Vector2Int.zero;
        [SerializeField]
        protected Vector2Int _position = Vector2Int.zero;
        [SerializeField]
        protected bool _forceCreateAloneOnAwake = false;
        protected FuWindow _fuWindow;
        public FuWindow Window => _fuWindow;

        /// <summary>
        /// Register the window definition and create the window instance if needed.
        /// </summary>
        public virtual void FuguiAwake()
        {
            // don't do anything if the monobehaviour is not enabled
            if (!enabled)
                return;

            // creeate the window definition, it will automaticaly be registered into fugui windows definitions list
            FuWindowDefinition windowDefinition = new FuWindowDefinition(_windowName, OnUI, _position == Vector2Int.zero ? null : _position, _size == Vector2Int.zero ? null : _size, _windowFlags);
            // call the OnWindowDefinitionCreated method to allow further customization
            OnWindowDefinitionCreated(windowDefinition);
            // register the OnUIWindowCreated event to handle the window creation
            windowDefinition.OnUIWindowCreated += WindowDefinition_OnUIWindowCreated;

            // Force the creation of the window immediately on Awake
            if (_forceCreateAloneOnAwake)
                Fugui.CreateWindowAsync(_windowName, null, true);
        }

        /// <summary>
        /// Override this method to perform actions when the window definition is created.
        /// You can create overlays, set properties, or perform any other setup needed for the window.
        /// </summary>
        /// <param name="windowDefinition"> The FuWindowDefinition instance that was created.</param>
        public virtual void OnWindowDefinitionCreated(FuWindowDefinition windowDefinition) { }

        /// <summary>
        /// This method is called when the window is created.
        /// </summary>
        /// <param name="window"> The FuWindow instance that was created.</param>
        protected void WindowDefinition_OnUIWindowCreated(FuWindow window)
        {
            _fuWindow = window;
            OnWindowCreated(window);
        }

        /// <summary>
        /// Override this method to define the UI of the window.
        /// </summary>
        /// <param name="window"> The FuWindow instance that this behaviour is attached to.</param>
        public virtual void OnUI(FuWindow window) { }

        /// <summary>
        /// This method is called when the window is created.
        /// </summary>
        /// <param name="window"> The FuWindow instance that was created.</param>
        public virtual void OnWindowCreated(FuWindow window) { }

        /// <summary>
        /// Get the window name associated with this behaviour.
        /// </summary>
        /// <returns> The FuWindowName of this window.</returns>
        public FuWindowName GetWindowName()
        {
            return _windowName;
        }

        /// <summary>
        /// Set the window name for this behaviour.
        /// </summary>
        /// <param name="value"> The FuWindowName to set.</param>
        public void SetWindowName(FuWindowName value)
        {
            _windowName = value;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(FuWindowBehaviour), true)]
    public class FuWindowBehaviourEditor : Editor
    {
        private List<FuWindowName> availableNames;
        private string[] windowNameOptions;
        private int selectedIndex;

        private SerializedProperty windowFlagsProp;
        private SerializedProperty sizeProp;
        private SerializedProperty positionProp;
        private SerializedProperty forceCreateProp;
        protected readonly HashSet<string> _excludedProps = new()
        {
            "_windowName",
            "_id",
            "_name",
            "_autoInstantiateWindowOnlayoutSet",
            "_idleFPS",
            "_windowFlags",
            "_size",
            "_position",
            "_forceCreateAloneOnAwake",
            "x",
            "y"
        };

        public virtual void OnEnable()
        {
            availableNames = FuWindowNameProvider.GetAllWindowNames().Values.ToList();
            windowNameOptions = availableNames.ConvertAll(n => n.Name).ToArray();

            var behaviour = (FuWindowBehaviour)target;
            selectedIndex = Mathf.Max(0, availableNames.FindIndex(n => n.ID == behaviour.GetWindowName().ID));

            windowFlagsProp = serializedObject.FindProperty("_windowFlags");
            sizeProp = serializedObject.FindProperty("_size");
            positionProp = serializedObject.FindProperty("_position");
            forceCreateProp = serializedObject.FindProperty("_forceCreateAloneOnAwake");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var behaviour = (FuWindowBehaviour)target;

            // Custom popup for FuWindowName
            EditorGUI.BeginChangeCheck();
            selectedIndex = EditorGUILayout.Popup("Window Name", selectedIndex, windowNameOptions);
            if (EditorGUI.EndChangeCheck())
            {
                var newName = availableNames[selectedIndex];
                behaviour.SetWindowName(newName);
                EditorUtility.SetDirty(behaviour);
            }

            // Draw other serialized fields manually
            EditorGUILayout.PropertyField(windowFlagsProp);
            EditorGUILayout.PropertyField(sizeProp);
            EditorGUILayout.PropertyField(positionProp);
            EditorGUILayout.PropertyField(forceCreateProp);

            DrawRemainingProperties();

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void DrawRemainingProperties()
        {
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                if (iterator.name == "m_Script") continue;
                if (_excludedProps.Contains(iterator.name)) continue;

                EditorGUILayout.PropertyField(iterator, true);
                enterChildren = false;
            }
        }
    }
#endif
}