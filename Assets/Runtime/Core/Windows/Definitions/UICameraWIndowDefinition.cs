using System;
using UnityEngine;

namespace Fugui.Core
{
    /// <summary>
    /// Represents a definition for a UI window that displays a camera view.
    /// </summary>
    public class UICameraWindowDefinition : UIWindowDefinition
    {
        /// <summary>
        /// Gets the camera associated with the UI window.
        /// </summary>
        public Camera Camera { get; private set; }

        /// <summary>
        /// Initializes a new instance of the UICameraWindowDefinition class with the specified parameters.
        /// </summary>
        /// <param name="camera">The camera to be displayed in the UI window.</param>
        /// <param name="id">The unique identifier for the UI window.</param>
        /// <param name="ui">The action to be performed on the UI window.</param>
        /// <param name="pos">The position of the UI window. If not specified, the default value is (256, 256).</param>
        /// <param name="size">The size of the UI window. If not specified, the default value is (256, 128).</param>
        /// <param name="flags">Behaviour flag of this window definition</param>
        public UICameraWindowDefinition(FuguiWindows windowName, Camera camera, string id, Action<UIWindow> ui = null, Vector2Int? pos = null, Vector2Int? size = null, UIWindowFlags flags = UIWindowFlags.Default) : base(windowName, id, ui, pos, size, flags)
        {
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
    }
}