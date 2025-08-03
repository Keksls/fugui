using Fu.Framework;
using System;
using UnityEngine;

namespace Fu.Core
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
        #endregion

        #region Unity Methods
        void Awake()
        {
            // store Fugui settings
            Fugui.Controller = this;
            Fugui.Settings = _settings;

            // prepare FuGui before start using it
            Fugui.Initialize(_uiCamera);

            // log errors to Unity console
            if (_logErrors)
            {
                Fugui.OnUIException += FuGui_OnUIException;
            }
        }

        private void Start()
        {
            // if no layouts and settings is set so, display Fugui settings to avoid 'softLocked scene'
            if (FuDockingLayoutManager.CurrentLayout == null && FuDockingLayoutManager.Layouts.Count == 0 && Fugui.Settings.DisplaySettingsIfNoLayout)
            {
                Fugui.CreateWindowAsync(FuSystemWindowsNames.FuguiSettings, null);
            }
        }

        private void FuGui_OnUIException(Exception error)
        {
            Debug.LogException(error);
        }

        private void Update()
        {
            // Update Input Manager
            FuRaycasting.Update();

            // Update Fugui Data
            Fugui.Update();

            // let's update main container inputs and internal stuff
            Fugui.MainContainer.Update();

            Fugui.Render();
        }

        private void LateUpdate()
        {
            // destroy contexts
            while (Fugui.ToDeleteContexts.Count > 0)
            {
                int contextID = Fugui.ToDeleteContexts.Dequeue();
                if (Fugui.ContextExists(contextID))
                {
                    FuContext context = Fugui.GetContext(contextID);
                    context.EndRender();
                    context.Destroy();
                    Fugui.Contexts.Remove(context.ID);
                    Fugui.DefaultContext.SetAsCurrent();
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
}