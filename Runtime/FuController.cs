using System;
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
        private FuguiUpdateMode _updateMode = FuguiUpdateMode.Update;
        #endregion

        /// <summary>
        /// Runs the awake workflow.
        /// </summary>
        void Awake()
        {
            // prepare FuGui before start using it
            Fugui.Initialize(_settings, this, _uiCamera, EnableMainContainer);

            // log errors to Unity console
            if (_logErrors)
            {
                Fugui.OnUIException += FuGui_OnUIException;
            }

            // awake all FuWindowBehaviour instances
#if UNITY_6000_4_OR_NEWER
            foreach (var mono in GameObject.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            {
                mono.SendMessage("FuguiAwake", SendMessageOptions.DontRequireReceiver);
            }
#else
            foreach (var mono in GameObject.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                mono.SendMessage("FuguiAwake", SendMessageOptions.DontRequireReceiver);
            }
#endif
        }

        /// <summary>
        /// Runs the start workflow.
        /// </summary>
        private void Start()
        {
            // if no layouts and settings is set so, display Fugui settings to avoid 'softLocked scene'
            if (Fugui.MainContainerEnabled && Fugui.Layouts.CurrentLayout == null && Fugui.Layouts.Layouts.Count == 0 && Fugui.Settings.DisplaySettingsIfNoLayout)
            {
                Fugui.CreateWindow(FuSystemWindowsNames.FuguiSettings);
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
            if (_updateMode == FuguiUpdateMode.LateUpdate)
            {
                FuUpdate();
            }

            // destroy contexts
            while (Fugui.ToDeleteContexts.Count > 0)
            {
                int contextID = Fugui.ToDeleteContexts.Dequeue();
                if (Fugui.ContextExists(contextID))
                {
                    FuContext context = Fugui.GetContext(contextID);

#if FU_EXTERNALIZATION
                    if(context is FuExternalContext externalContext)
                    {
                        // remove external window from dictionary
                        Fugui.ExternalWindows.Remove(externalContext.Window.Window.ID);
                    }
#endif
                    if (context.RenderPrepared)
                    {
                        context.EndRender();
                    }
                    context.Destroy();

                    Fugui.Contexts.Remove(context.ID);
                    Fugui.SetCurrentContext(Fugui.DefaultContext);
                }
            }
        }

        /// <summary>
        /// Handles the Disable event.
        /// </summary>
        private void OnDisable()
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
        /// Disposes the external windows and stops the render thread.
        /// </summary>
        public void Dispose()
        {
            Fugui.Dispose();
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
