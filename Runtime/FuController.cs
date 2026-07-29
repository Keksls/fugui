using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Represents the Fu Controller type.
    /// </summary>
    public class FuController : MonoBehaviour
    {
        #region State
        // The settings for the Fugui Manager
        [SerializeField]
        private FuSettings _settings;
        // Camera used to render main UI container
        [SerializeField]
        private Camera _uiCamera;
        public bool EnableMainContainer = true;
        [SerializeField]
        private bool _logErrors = true;
        [SerializeField]
        private bool _keepControllerBetweenScenes = true;
        [SerializeField]
        private FuguiUpdateMode _updateMode = FuguiUpdateMode.Update;
        private bool _ownsFugui;
        private bool _uiExceptionSubscribed;
        private bool _hasStarted;
        #endregion

        /// <summary>
        /// Initializes the unique Fugui runtime session before scene behaviours begin using it.
        /// </summary>
        private void Awake()
        {
            InitializeRuntimeSession();
        }

        /// <summary>
        /// Recreates the Fugui runtime session when an already-started controller is re-enabled.
        /// </summary>
        private void OnEnable()
        {
            if (!_hasStarted || _ownsFugui)
            {
                return;
            }

            if (InitializeRuntimeSession())
            {
                CompleteRuntimeSessionInitialization();
            }
        }

        /// <summary>
        /// Completes scene registration after every enabled behaviour has received its Unity Awake callback.
        /// </summary>
        private void Start()
        {
            _hasStarted = true;
            if (_ownsFugui)
            {
                CompleteRuntimeSessionInitialization();
            }
        }

        /// <summary>
        /// Creates the Fugui runtime session and subscribes controller-owned callbacks.
        /// </summary>
        /// <returns>True when this controller owns the initialized session.</returns>
        private bool InitializeRuntimeSession()
        {
            _ownsFugui = Fugui.Initialize(_settings, this, _uiCamera, EnableMainContainer);
            if (!_ownsFugui)
            {
                enabled = false;
                return false;
            }

            if (_logErrors)
            {
                Fugui.OnUIException += FuGui_OnUIException;
                _uiExceptionSubscribed = true;
            }

            if (_keepControllerBetweenScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            return true;
        }

        /// <summary>
        /// Registers scene behaviours and applies the startup fallback for an empty layout collection.
        /// </summary>
        private static void CompleteRuntimeSessionInitialization()
        {
            // Unity has completed scene Awake callbacks before Start, so Fugui registrations see initialized behaviours.
            NotifyFuguiBehaviours();

            // if no layouts and settings is set so, display Fugui settings to avoid 'softLocked scene'
            if (Fugui.Settings.RenderingMode == FuRenderingMode.Standard &&
                Fugui.MainContainerEnabled &&
                Fugui.Layouts.CurrentLayout == null &&
                Fugui.Layouts.Layouts.Count == 0 &&
                Fugui.Settings.DisplaySettingsIfNoLayout)
            {
                Fugui.CreateWindow(FuSystemWindowsNames.FuguiSettings);
            }
        }

        /// <summary>
        /// Notifies scene behaviours that a new Fugui runtime session is ready.
        /// </summary>
        private static void NotifyFuguiBehaviours()
        {
            // SendMessage targets every component on a GameObject, so notify each GameObject only once.
            HashSet<GameObject> notifiedGameObjects = new HashSet<GameObject>();
#if UNITY_6000_4_OR_NEWER
            foreach (var mono in GameObject.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
#else
            foreach (var mono in GameObject.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
#endif
            {
                GameObject target = mono.gameObject;
                if (notifiedGameObjects.Add(target))
                {
                    target.SendMessage("FuguiAwake", SendMessageOptions.DontRequireReceiver);
                }
            }
        }

        /// <summary>
        /// Runs the fu gui on uiexception workflow.
        /// </summary>
        /// <param name="error">The error value.</param>
        private void FuGui_OnUIException(Exception error)
        {
            Debug.LogException(error);
        }

        /// <summary>
        /// Updates the value.
        /// </summary>
        private void Update()
        {
            if (_updateMode == FuguiUpdateMode.Update)
            {
                FuUpdate();
            }
        }

        /// <summary>
        /// Runs the fu update workflow.
        /// </summary>
        public void FuUpdate()
        {
            if (!_ownsFugui || !Fugui.IsOwnedBy(this))
            {
                return;
            }

            // Update Input Manager
            FuRaycasting.Update();

            // Update Fugui Data
            Fugui.Update();

            // Render Fugui (this will prepare the rendering data and call all fugui implementations code but it will NOT draw the UI, the Drawing is handeled by Render Feature)
            Fugui.Render();
        }

        /// <summary>
        /// Runs the late update workflow.
        /// </summary>
        private void LateUpdate()
        {
            if (!_ownsFugui || !Fugui.IsOwnedBy(this))
            {
                return;
            }

            if (_updateMode == FuguiUpdateMode.LateUpdate)
            {
                FuUpdate();
            }

            Fugui.ProcessPendingContextDestructions();
        }

        /// <summary>
        /// Releases the Fugui runtime session when its owning controller becomes inactive.
        /// </summary>
        private void OnDisable()
        {
            Dispose();
        }

        /// <summary>
        /// Ensures the Fugui runtime session is released when its owning controller is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            Dispose();
        }

        /// <summary>
        /// Handles the Application Quit event.
        /// </summary>
        private void OnApplicationQuit()
        {
            Dispose();
        }

        /// <summary>
        /// Disposes the Fugui runtime session when this controller owns it.
        /// </summary>
        public void Dispose()
        {
            if (_uiExceptionSubscribed)
            {
                Fugui.OnUIException -= FuGui_OnUIException;
                _uiExceptionSubscribed = false;
            }

            if (Fugui.Dispose(this))
            {
                _ownsFugui = false;
            }
        }
    }

    /// <summary>
    /// Lists the available Fugui Update Mode values.
    /// </summary>
    public enum FuguiUpdateMode
    {
        Update,
        LateUpdate,
        Manual
    }
}
