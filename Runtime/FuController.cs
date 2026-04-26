using System;
using UnityEngine;

namespace Fu
{
    public class FuController : MonoBehaviour
    {
        #region Variables
        // The settings for the Fugui Manager
        [SerializeField]
        private FuSettings _settings;
        // Camera used to render main UI container
        [SerializeField]
        private Camera _uiCamera;
        [SerializeField]
        private bool _logErrors = true;
        [SerializeField]
        private FuguiUpdateMode _updateMode = FuguiUpdateMode.Update;
        #endregion

        #region Unity Methods
        void Awake()
        {
            // prepare FuGui before start using it
            Fugui.Initialize(_settings, this, _uiCamera);

            // log errors to Unity console
            if (_logErrors)
            {
                Fugui.OnUIException += FuGui_OnUIException;
            }

            // awake all FuWindowBehaviour instances
            foreach (var mono in GameObject.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            {
                mono.SendMessage("FuguiAwake", SendMessageOptions.DontRequireReceiver);
            }
        }

        private void Start()
        {
            // if no layouts and settings is set so, display Fugui settings to avoid 'softLocked scene'
            if (Fugui.Layouts.CurrentLayout == null && Fugui.Layouts.Layouts.Count == 0 && Fugui.Settings.DisplaySettingsIfNoLayout)
            {
                Fugui.CreateWindow(FuSystemWindowsNames.FuguiSettings);
            }
        }

        private void FuGui_OnUIException(Exception error)
        {
            Debug.LogException(error);
        }

        private void Update()
        {
            if (_updateMode == FuguiUpdateMode.Update)
            {
                FuUpdate();
            }
        }

        public void FuUpdate()
        {
            // Update Input Manager
            FuRaycasting.Update();

            // Update Fugui Data
            Fugui.Update();

            // Render Fugui (this will prepare the rendering data and call all fugui implementations code but it will NOT draw the UI, the Drawing is handeled by Render Feature)
            Fugui.Render();
        }

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

        private void OnDisable()
        {
            Dispose();
        }

        private void OnApplicationQuit()
        {
            Dispose();
        }
        #endregion

        #region public Utils
        /// <summary>
        /// Disposes the external windows and stops the render thread.
        /// </summary>
        public void Dispose()
        {
            Fugui.Dispose();
        }
        #endregion
    }

    public enum FuguiUpdateMode
    {
        Update,
        LateUpdate,
        Manual
    }
}
