using Fu.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu
{
    public class FuWindowDefinition
    {
        #region Variables
        // A unique identifier for the window
        public FuWindowName WindowName { get; private set; }
        // A delegate for updating the window's UI
        public Action<FuWindow, FuLayout> UI { get; private set; }
        // The position of the window on the screen
        public Vector2Int Position { get; private set; }
        // The size of the window
        public Vector2Int Size { get; private set; }

        // A flag indicating whether the window can be serialized
        public bool IsExternalizable { get; private set; }
        // A flag indicating whether the window can be docked
        public bool IsDockable { get; private set; }
        // A flag indicating whether the window can be closed
        public bool IsClosable { get; private set; }
        // A flag indicating whether the window is interactible
        public bool IsInterractif { get; private set; }
        // A flag indicating whether other windows can dock over this window
        public bool NoDockingOverMe { get; private set; }
        // A flag indicating whether this window definition can instantiate more than one window at time
        public bool AllowMultipleWindow { get; private set; }

        // A flag indicating whether this window will use native title bar once externalized
        public bool UseNativeTitleBar { get; private set; }
        // A flag indicating if this window will have a task bar icon once externalized
        public bool NoTaskBarIcon { get; private set; }
        // A flag indicating whether this window will have no focus when appearing
        public bool NoFocusOnAppearing { get; private set; }
        // A flag indicating whether this window will be always on top once externalized
        public bool AlwaysOnTop { get; private set; }
        // A flag indicating whether this window will be non modal once externalized
        public bool NoModal { get; private set; }
        // A flag indicating whether this window will send no notification once externalized
        public bool NoNotify { get; private set; }
        // A flag indicating whether this window will show no context menu once externalized
        public bool NoContextMenu { get; private set; }

        // A dictionary that store default overlays for this window
        public Dictionary<string, FuOverlay> Overlays { get; private set; }
        // public event invoked when UIWindow is Creating according to this current UIWindowDefinition
        public event Action<FuWindow> OnUIWindowCreated;
        // the callback UI of the optional toolbar of this window
        public Action<FuWindow, Vector2> HeaderUI { get; set; }
        // the callback UI of the optional footer of this window
        public Action<FuWindow, Vector2> FooterUI { get; set; }
        // The height of the window topBar (optional)
        public float HeaderHeight { get; private set; }
        // The height of the window topBar (optional)
        public float BottomBarHeight { get; private set; }
        // the type of the UIWindow to instantiate
        internal Type _uiWindowType;
        #endregion

        /// <summary>
        /// Initializes a new instance of the UIWindowDefinition class with the specified parameters.
        /// </summary>
        /// <param name="windowName">The FuGui window definition</param>
        /// <param name="ui">The action to be performed on the UI window.</param>
        /// <param name="pos">The position of the UI window. If not specified, the default value is (256, 256).</param>
        /// <param name="size">The size of the UI window. If not specified, the default value is (256, 128).</param>
        /// <param name="flags">Behaviour flag of this window definition</param>
        /// <param name="externalFlags">External window flags of this window definition</param>
        public FuWindowDefinition(FuWindowName windowName, Action<FuWindow, FuLayout> ui = null, Vector2Int? pos = null, Vector2Int? size = null, FuWindowFlags flags = FuWindowFlags.Default, FuExternalWindowFlags externalFlags = FuExternalWindowFlags.Default)
        {
            // Assign the specified values to the corresponding fields
            WindowName = windowName;
            UI = ui;
            Position = pos.HasValue ? pos.Value : new Vector2Int(-1, -1);
            Size = size.HasValue ? size.Value : new Vector2Int(256, 128);
            IsExternalizable = !flags.HasFlag(FuWindowFlags.NoExternalization);
            IsDockable = !flags.HasFlag(FuWindowFlags.NoDocking);
            IsInterractif = !flags.HasFlag(FuWindowFlags.NoInterractions);
            IsClosable = !flags.HasFlag(FuWindowFlags.NoClosable);
            NoDockingOverMe = !flags.HasFlag(FuWindowFlags.NoDockingOverMe);
            AllowMultipleWindow = flags.HasFlag(FuWindowFlags.AllowMultipleWindow);
            _uiWindowType = typeof(FuWindow);
            Overlays = new Dictionary<string, FuOverlay>();

            // external window flags
            UseNativeTitleBar = externalFlags.HasFlag(FuExternalWindowFlags.UseNativeTitleBar);
            NoTaskBarIcon = externalFlags.HasFlag(FuExternalWindowFlags.NoTaskBarIcon);
            NoFocusOnAppearing = externalFlags.HasFlag(FuExternalWindowFlags.NoFocusOnAppearing);
            AlwaysOnTop = externalFlags.HasFlag(FuExternalWindowFlags.AlwaysOnTop);
            NoModal = externalFlags.HasFlag(FuExternalWindowFlags.NoModal);
            NoNotify = externalFlags.HasFlag(FuExternalWindowFlags.NoNotify);
            NoContextMenu = externalFlags.HasFlag(FuExternalWindowFlags.NoContextMenu);

            if (!flags.HasFlag(FuWindowFlags.NoAutoRegisterWindowDefinition))
            {
                Fugui.RegisterWindowDefinition(this);
            }
        }

        #region Overlays
        /// <summary>
        /// Adds the specified UI overlay to the list of overlays.
        /// </summary>
        /// <param name="overlay">The UI overlay to add.</param>
        /// <returns>True if the overlay was added successfully, false if the overlay already exists in the list.</returns>
        internal bool AddOverlay(FuOverlay overlay)
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
        public FuWindowDefinition SetCustomWindowType<T>() where T : FuWindow
        {
            // Assign the specified UIWindow subclass type to the _uiWindowType field
            _uiWindowType = typeof(T);
            return this;
        }

        /// <summary>
        /// Creates a new instance of the UIWindow class with the current UIWindowDefinition object as the parameter.
        /// </summary>
        /// <returns>A new instance of the UIWindow class.</returns>
        public bool CreateUIWindow(out FuWindow window)
        {
            // check whatever this winDef already has an instance and we do not want it to be ducplicated
            if (!AllowMultipleWindow && AlreadyHasInstance())
            {
                window = null;
                return false;
            }

            // Use the Activator class to create a new instance of the UIWindow class with the current UIWindowDefinition object as the parameter
            window = (FuWindow)Activator.CreateInstance(_uiWindowType, this);
            OnUIWindowCreated?.Invoke(window);
            return true;
        }

        /// <summary>
        /// Creates a new instance of the specified UIWindow subclass with the current UIWindowDefinition object as the parameter.
        /// </summary>
        /// <typeparam name="T">The type of UIWindow subclass to create.</typeparam>
        /// <returns>A new instance of the specified UIWindow subclass.</returns>
        public bool CreateUIWindow<T>(out T window) where T : FuWindow
        {
            // Call the CreateUIWindow method to create a new instance of the UIWindow class
            // and implicitly cast it to the specified UIWindow subclass type
            return CreateUIWindow(out window);
        }

        /// <summary>
        /// whatever an instance of this window already exists
        /// </summary>
        /// <returns>true if already exists</returns>
        public bool AlreadyHasInstance()
        {
            foreach (FuWindow window in Fugui.UIWindows.Values)
            {
                if (window.WindowName.Equals(WindowName))
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region Default definitions fields setters
        /// <summary>
        /// Sets the UI of the topBar of this window
        /// This will be called before main UI callback and set the cursor
        /// Sets the height of the topBar of this window
        /// The WorkingArea Size and Position will be calculated according to this value.
        /// The topBar will not be draw by Fugui, you will have to do it yourself
        /// </summary>
        /// <param name="headerUI">The callback UI of the optional window Header.</param>
        /// <param name="headerHeight">The height of the optional window Header.</param>
        /// <returns>The current UIWindowDefinition object.</returns>
        public FuWindowDefinition SetHeaderUI(Action<FuWindow, Vector2> headerUI, float headerHeight)
        {
            // Ensure the header height is not negative
            HeaderHeight = Mathf.Max(0f, headerHeight);
            // Assign the specified callback UI to the HeaderUI field
            HeaderUI = headerUI;
            return this;
        }

        /// <summary>
        /// Sets the UI of the bottomBar of this window
        /// </summary>
        /// <param name="bottomBarUI"> The callback UI of the optional window bottomBar.</param>
        /// <param name="bottomBarHeight" >The height of the optional window bottomBar.</param>
        /// <returns> The current UIWindowDefinition object.</returns>
        public FuWindowDefinition SetFooterUI(Action<FuWindow, Vector2> bottomBarUI, float bottomBarHeight)
        {
            // Ensure the bottom bar height is not negative
            BottomBarHeight = Mathf.Max(0f, bottomBarHeight);
            // Assign the specified callback UI to the FooterUI field
            FooterUI = bottomBarUI;
            return this;
        }

        /// <summary>
        /// Sets the action to be performed on the UI window.
        /// </summary>
        /// <param name="ui">The action to be performed on the UI window.</param>
        /// <returns>The current UIWindowDefinition object.</returns>
        public FuWindowDefinition SetUI(Action<FuWindow, FuLayout> ui = null)
        {
            // Assign the specified action to the UI field
            UI = ui;
            return this;
        }

        /// <summary>
        /// Sets the position of the UI window.
        /// </summary>
        /// <param name="pos">The position of the UI window. If not specified, the default value is (256, 256).</param>
        /// <returns>The current UIWindowDefinition object.</returns>
        public FuWindowDefinition SetPosition(Vector2Int? pos = null)
        {
            // Assign the specified position or the default value to the Position field
            Position = pos.HasValue ? pos.Value : new Vector2Int(256, 256);
            return this;
        }

        /// <summary>
        /// Sets the size of the UI window.
        /// </summary>
        /// <param name="size">The size of the UI window. If not specified, the default value is (256, 128).</param>
        /// <returns>The current UIWindowDefinition object.</returns>
        public FuWindowDefinition SetSize(Vector2Int? size = null)
        {
            // Assign the specified size or the default value to the Size field
            Size = size.HasValue ? size.Value : new Vector2Int(256, 128);
            return this;
        }

        /// <summary>
        /// Sets the externalizability of the UI window.
        /// </summary>
        /// <param name="isExternalizable">A boolean value indicating whether the UI window can be externalized.</param>
        /// <returns>The current UIWindowDefinition object.</returns>
        public FuWindowDefinition SetExternalizable(bool isExternalizable)
        {
            // Assign the specified value to the IsExternalizable field
            IsExternalizable = isExternalizable;
            return this;
        }

        /// <summary>
        /// Sets the dockability of the UI window.
        /// </summary>
        /// <param name="isDockable">A boolean value indicating whether the UI window can be docked.</param>
        /// <returns>The current UIWindowDefinition object.</returns>
        public FuWindowDefinition SetDockable(bool isDockable)
        {
            // Assign the specified value to the IsDockable field
            IsDockable = isDockable;
            return this;
        }

        /// <summary>
        /// Sets the closability of the UI window.
        /// </summary>
        /// <param name="isDockable">A boolean value indicating whether the UI window can be closed.</param>
        /// <returns>The current UIWindowDefinition object.</returns>
        public FuWindowDefinition SetClosable(bool isClosable)
        {
            // Assign the specified value to the isClosable field
            IsClosable = isClosable;
            return this;
        }

        /// <summary>
        /// Sets the interactibility of the UI window.
        /// </summary>
        /// <param name="isInterractif">A boolean value indicating whether the UI window is interactible.</param>
        /// <returns>The current UIWindowDefinition object.</returns>
        public FuWindowDefinition SetInterractif(bool isInterractif)
        {
            // Assign the specified value to the IsInterractif field
            IsInterractif = isInterractif;
            return this;
        }

        /// <summary>
        /// Sets the no docking over me property of the UI window.
        /// </summary>
        /// <param name="noDockingOverMe">A boolean value indicating whether other UI windows can dock over the current UI window.</param>
        /// <returns>The current UIWindowDefinition object.</returns>
        public FuWindowDefinition SetNoDockingOverMe(bool noDockingOverMe)
        {
            // Assign the specified value to the NoDockingOverMe field
            NoDockingOverMe = noDockingOverMe;
            return this;
        }
        #endregion
    }
}