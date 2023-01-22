using Fugui.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fugui.Core
{
    public class UIWindowDefinition
    {
        #region Variables
        public FuGuiWindows WindowID { get; private set; }
        // A unique identifier for the window
        public string Id { get; private set; }
        // A delegate for updating the window's UI
        public Action<UIWindow> UI { get; private set; }
        // The position of the window on the screen
        public Vector2Int Position { get; private set; }
        // The size of the window
        public Vector2Int Size { get; private set; }
        // A flag indicating whether the window can be serialized
        public bool IsExternalizable { get; private set; }
        // A flag indicating whether the window can be docked
        public bool IsDockable { get; private set; }
        // A flag indicating whether the window is interactible
        public bool IsInterractible { get; private set; }
        // A flag indicating whether other windows can dock over this window
        public bool NoDockingOverMe { get; private set; }
        // A dictionary that store default overlays for this window
        public Dictionary<string, UIOverlay> Overlays { get; private set; }
        // the type of the UIWindow to instantiate
        internal Type _uiWindowType;
        // public event invoked when UIWindow is Creating according to this current UIWindowDefinition
        public event Action<UIWindow> OnUIWindowCreated;
        #endregion

        /// <summary>
        /// Initializes a new instance of the UIWindowDefinition class with the specified parameters.
        /// </summary>
        /// <param name="windowName">The FuGui window definition</param>
        /// <param name="id">The unique identifier for the UI window.</param>
        /// <param name="ui">The action to be performed on the UI window.</param>
        /// <param name="pos">The position of the UI window. If not specified, the default value is (256, 256).</param>
        /// <param name="size">The size of the UI window. If not specified, the default value is (256, 128).</param>
        /// <param name="externalizable">A boolean value indicating whether the UI window can be externalized.</param>
        /// <param name="dockable">A boolean value indicating whether the UI window can be docked.</param>
        /// <param name="isInterractible">A boolean value indicating whether the UI window is interactible.</param>
        /// <param name="noDockingOverMe">A boolean value indicating whether docking is allowed over the UI window.</param>
        public UIWindowDefinition(FuGuiWindows windowName, string id, Action<UIWindow> ui = null, Vector2Int? pos = null, Vector2Int? size = null, bool externalizable = true, bool dockable = true, bool isInterractible = true, bool noDockingOverMe = false)
        {
            // Assign the specified values to the corresponding fields
            WindowID = windowName;
            Id = id;
            UI = ui;
            Position = pos.HasValue ? pos.Value : new Vector2Int(256, 256);
            Size = size.HasValue ? size.Value : new Vector2Int(256, 128);
            IsExternalizable = externalizable;
            IsDockable = dockable;
            IsInterractible = isInterractible;
            NoDockingOverMe = noDockingOverMe;
            _uiWindowType = typeof(UIWindow);
            Overlays = new Dictionary<string, UIOverlay>();
            FuGui.RegisterWindowDefinition(this);
        }

        #region Overlays
        /// <summary>
        /// Adds the specified UI overlay to the list of overlays.
        /// </summary>
        /// <param name="overlay">The UI overlay to add.</param>
        /// <returns>True if the overlay was added successfully, false if the overlay already exists in the list.</returns>
        internal bool AddOverlay(UIOverlay overlay)
        {
            // Check if the overlay already exists in the list
            if (Overlays.ContainsKey(overlay.ID))
            {
                // Return false if the overlay already exists
                return false;
            }

            // Add the overlay to the list
            Overlays.Add(overlay.ID, overlay);
            // Return true to indicate that the overlay was added successfully
            return true;
        }

        /// <summary>
        /// Removes the UI overlay with the specified ID from the list of overlays.
        /// </summary>
        /// <param name="overlayID">The ID of the UI overlay to remove.</param>
        /// <returns>True if the overlay was removed successfully, false if the overlay was not found in the list.</returns>
        internal bool RemoveOverlay(string overlayID)
        {
            // Remove the overlay with the specified ID from the list and return the result
            return Overlays.Remove(overlayID);
        }

        #endregion

        #region UIWindow Creation
        /// <summary>
        /// Sets the custom type of the UI window to the specified UIWindow subclass.
        /// </summary>
        /// <typeparam name="T">The type of UIWindow subclass to set as the custom type.</typeparam>
        public UIWindowDefinition SetCustomWindowType<T>() where T : UIWindow
        {
            // Assign the specified UIWindow subclass type to the _uiWindowType field
            _uiWindowType = typeof(T);
            return this;
        }

        /// <summary>
        /// Creates a new instance of the UIWindow class with the current UIWindowDefinition object as the parameter.
        /// </summary>
        /// <returns>A new instance of the UIWindow class.</returns>
        public UIWindow CreateUIWindow()
        {
            // Use the Activator class to create a new instance of the UIWindow class with the current UIWindowDefinition object as the parameter
            UIWindow window = (UIWindow)Activator.CreateInstance(_uiWindowType, this);
            OnUIWindowCreated?.Invoke(window);
            return window;
        }

        /// <summary>
        /// Creates a new instance of the specified UIWindow subclass with the current UIWindowDefinition object as the parameter.
        /// </summary>
        /// <typeparam name="T">The type of UIWindow subclass to create.</typeparam>
        /// <returns>A new instance of the specified UIWindow subclass.</returns>
        public T CreateUIWindow<T>() where T : UIWindow
        {
            // Call the CreateUIWindow method to create a new instance of the UIWindow class
            // and cast it to the specified UIWindow subclass type
            return (T)CreateUIWindow();
        }
        #endregion

        #region Default definitions fields setters
        /// <summary>
        /// Sets the action to be performed on the UI window.
        /// </summary>
        /// <param name="ui">The action to be performed on the UI window.</param>
        /// <returns>The current UIWindowDefinition object.</returns>
        public UIWindowDefinition SetUI(Action<UIWindow> ui = null)
        {
            // Assign the specified action to the UI field
            UI = ui;
            // Return the current UIWindowDefinition object
            return this;
        }

        /// <summary>
        /// Sets the position of the UI window.
        /// </summary>
        /// <param name="pos">The position of the UI window. If not specified, the default value is (256, 256).</param>
        /// <returns>The current UIWindowDefinition object.</returns>
        public UIWindowDefinition SetPosition(Vector2Int? pos = null)
        {
            // Assign the specified position or the default value to the Position field
            Position = pos.HasValue ? pos.Value : new Vector2Int(256, 256);
            // Return the current UIWindowDefinition object
            return this;
        }

        /// <summary>
        /// Sets the size of the UI window.
        /// </summary>
        /// <param name="size">The size of the UI window. If not specified, the default value is (256, 128).</param>
        /// <returns>The current UIWindowDefinition object.</returns>
        public UIWindowDefinition SetSize(Vector2Int? size = null)
        {
            // Assign the specified size or the default value to the Size field
            Size = size.HasValue ? size.Value : new Vector2Int(256, 128);
            // Return the current UIWindowDefinition object
            return this;
        }

        /// <summary>
        /// Sets the externalizability of the UI window.
        /// </summary>
        /// <param name="isExternalizable">A boolean value indicating whether the UI window can be externalized.</param>
        /// <returns>The current UIWindowDefinition object.</returns>
        public UIWindowDefinition SetExternalizable(bool isExternalizable)
        {
            // Assign the specified value to the IsExternalizable field
            IsExternalizable = isExternalizable;
            // Return the current UIWindowDefinition object
            return this;
        }

        /// <summary>
        /// Sets the dockability of the UI window.
        /// </summary>
        /// <param name="isDockable">A boolean value indicating whether the UI window can be docked.</param>
        /// <returns>The current UIWindowDefinition object.</returns>
        public UIWindowDefinition SetDockable(bool isDockable)
        {
            // Assign the specified value to the IsDockable field
            IsDockable = isDockable;
            // Return the current UIWindowDefinition object
            return this;
        }

        /// <summary>
        /// Sets the interactibility of the UI window.
        /// </summary>
        /// <param name="isInterractible">A boolean value indicating whether the UI window is interactible.</param>
        /// <returns>The current UIWindowDefinition object.</returns>
        public UIWindowDefinition SetInterractible(bool isInterractible)
        {
            // Assign the specified value to the IsInterractible field
            IsInterractible = isInterractible;
            // Return the current UIWindowDefinition object
            return this;
        }

        /// <summary>
        /// Sets the no docking over me property of the UI window.
        /// </summary>
        /// <param name="noDockingOverMe">A boolean value indicating whether other UI windows can dock over the current UI window.</param>
        /// <returns>The current UIWindowDefinition object.</returns>
        public UIWindowDefinition SetNoDockingOverMe(bool noDockingOverMe)
        {
            // Assign the specified value to the NoDockingOverMe field
            NoDockingOverMe = noDockingOverMe;
            // Return the current UIWindowDefinition object
            return this;
        }
        #endregion
    }
}