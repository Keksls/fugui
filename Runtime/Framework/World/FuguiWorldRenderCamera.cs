using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Registers a Unity camera as an allowed Fugui world-space render camera.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class FuguiWorldRenderCamera : MonoBehaviour
    {
        #region State
        private Camera _camera;
        #endregion

        #region Methods
        /// <summary>
        /// Registers the attached camera for Fugui world-space rendering.
        /// </summary>
        private void OnEnable()
        {
            _camera = GetComponent<Camera>();
            Fugui.World.RegisterRenderCamera(_camera);
        }

        /// <summary>
        /// Unregisters the attached camera from Fugui world-space rendering.
        /// </summary>
        private void OnDisable()
        {
            Fugui.World.UnregisterRenderCamera(_camera);
            _camera = null;
        }
        #endregion
    }
}
