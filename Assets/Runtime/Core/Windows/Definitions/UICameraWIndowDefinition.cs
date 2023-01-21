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
        /// <param name="externalizable">A boolean value indicating whether the UI window can be externalized.</param>
        /// <param name="dockable">A boolean value indicating whether the UI window can be docked.</param>
        /// <param name="isInterractible">A boolean value indicating whether the UI window is interactible.</param>
        /// <param name="noDockingOverMe">A boolean value indicating whether docking is allowed over the UI window.</param>
        public UICameraWindowDefinition(int windowName, Camera camera, string id, Action<UIWindow> ui = null, Vector2Int? pos = null, Vector2Int? size = null, bool externalizable = true, bool dockable = true, bool isInterractible = true, bool noDockingOverMe = false) : base(windowName, id, ui, pos, size, externalizable, dockable, isInterractible, noDockingOverMe)
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