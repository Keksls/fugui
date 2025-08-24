using System;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Represents a definition for a UI window that displays a camera view.
    /// </summary>
    public class FuCameraWindowDefinition : FuWindowDefinition
    {
        /// <summary>
        /// Gets the camera associated with the UI window.
        /// </summary>
        public Camera Camera { get; private set; }
        /// <summary>
        /// Get the default supersampling for camera window
        /// </summary>
        public float SuperSampling { get; private set; }

        /// <summary>
        /// Initializes a new instance of the UICameraWindowDefinition class with the specified parameters.
        /// </summary>
        /// <param name="camera">The camera to be displayed in the UI window.</param>
        /// <param name="id">The unique identifier for the UI window.</param>
        /// <param name="ui">The action to be performed on the UI window.</param>
        /// <param name="pos">The position of the UI window. If not specified, the default value is (256, 256).</param>
        /// <param name="size">The size of the UI window. If not specified, the default value is (256, 128).</param>
        /// <param name="flags">Behaviour flag of this window definition</param>
        public FuCameraWindowDefinition(FuWindowName windowName, Camera camera, Action<FuWindow> ui = null, Vector2Int? pos = null, Vector2Int? size = null, FuWindowFlags flags = FuWindowFlags.Default) : base(windowName, ui, pos, size, flags)
        {
            // set default camera window supersampling
            SuperSampling = 1f;
            // Assign the specified camera to the Camera field
            Camera = camera;
        }

        /// <summary>
        /// Sets the camera associated with the UI window.
        /// </summary>
        /// <param name="camera">The camera to be displayed in the UI window.</param>
        public void SetCamera(Camera camera)
        {
            // Assign the specified camera to the Camera field
            Camera = camera;
        }

        /// <summary>
        /// Sets the default Supersampling for the UI window.
        /// </summary>
        /// <param name="superSampling">The default camera window supersampling.</param>
        public void SetSupersampling(float superSampling)
        {
            // Assign the default camera supersampling
            SuperSampling = superSampling;
        }
    }
}