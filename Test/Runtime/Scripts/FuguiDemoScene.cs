using Fu;
using Fu.Core;
using Fu.Framework;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// this sample show how to use Fugui API
/// </summary>
public class FuguiDemoScene : MonoBehaviour
{
    [SerializeField]
    private bool _showImGuiDemoWindow = false;

    private void Awake()
    {
        // register Fugui Settings Window
        new FuWindowDefinition(FuSystemWindowsNames.FuguiSettings, (window) =>
        {
            Fugui.DrawSettings();
        }, size: new Vector2Int(256, 256), flags: FuWindowFlags.AllowMultipleWindow);

        // register DockingLayoutManager events
        FuDockingLayoutManager.OnDockLayoutReloaded += DockingLayoutManager_OnDockLayoutReloaded;
        FuDockingLayoutManager.OnDockLayoutSet += DockingLayoutManager_OnDockLayoutSet;

        // register on render event of the main container's context to draw ImGui Demo Window
        Fugui.MainContainer.Context.OnRender += MainContainerContext_OnRender;

        // Initialize the DockingLayoutManager and give it the list of custom windows names (see DockingLayout on doc)
        FuDockingLayoutManager.Initialize(FuWindowsNames.GetAllWindowsNames());
    }

    private void Start()
    {
        // register the main menu items
        registerMainMenuItems();

        // set default layout (will create UIWindows instances)
        if (FuDockingLayoutManager.Layouts.Count > 0)
        {
            FuDockingLayoutManager.SetLayout("DemoScene");
        }
    }

    /// <summary>
    /// Register demo menu settings
    /// </summary>
    private void registerMainMenuItems()
    {
        // add Fugui menu
        Fugui.RegisterMainMenuItem("Fugui", null);
        // add 'Settings' menu items to open settings (its a child of 'Fugui' item)
        Fugui.RegisterMainMenuItem("Settings", () => { }, "Fugui");
        // add 'Settings' menu items to open settings (its a child of 'Fugui' item)
        Fugui.RegisterMainMenuItem("Imgui Demo", () => { _showImGuiDemoWindow = true; }, "Fugui");

        // register all alyout so user can switch between them
        Fugui.RegisterMainMenuItem("Layout", null);
        foreach (KeyValuePair<string, FuDockingLayoutDefinition> layoutDefinition in FuDockingLayoutManager.Layouts)
        {
            string menuName = Fugui.AddSpacesBeforeUppercase(layoutDefinition.Key);
            if (!Fugui.IsMainMenuRegisteredItem(menuName))
            {
                Fugui.RegisterMainMenuItem(menuName, () => FuDockingLayoutManager.SetLayout(layoutDefinition.Key), "Layout");
            }
        }

        // register all windows from registered windows definitions so user can instantiate them from here
        Fugui.RegisterMainMenuItem("Windows", null);
        foreach (FuWindowDefinition windowName in Fugui.UIWindowsDefinitions.Values)
        {
            Fugui.RegisterMainMenuItem(windowName.WindowName.ToString(), () => Fugui.CreateWindowAsync(windowName.WindowName, null), "Windows");
        }

        // register all windows from registered windows definitions, once instantiated, the window will be added to its own 3d container so it can be render in 3D scene
        Fugui.RegisterMainMenuItem("3D Windows", null);
        foreach (FuWindowDefinition windowName in Fugui.UIWindowsDefinitions.Values)
        {
            Fugui.RegisterMainMenuItem("3D " + windowName.WindowName.ToString(), () => Fugui.CreateWindowAsync(windowName.WindowName, (window) =>
            {
                Fugui.Add3DWindow(window, new Vector3(0f, -2f, 0f), Quaternion.Euler(Vector3.up * 180f));
            }, false), "3D Windows");
        }
    }

    /// <summary>
    /// Whenever a docking Layout is set
    /// </summary>
    private void DockingLayoutManager_OnDockLayoutSet()
    {
        // instantiate test 3D window
        Fugui.CreateWindowAsync(FuSystemWindowsNames.FuguiSettings, (window) =>
        {
            Fugui.Add3DWindow(window, new Vector3(0f, -2f, 0f), Quaternion.Euler(Vector3.up * 180f));
        }, false);
    }

    /// <summary>
    /// Whenever the DockingLayoutManager reload its list of registered Layouts
    /// </summary>
    private void DockingLayoutManager_OnDockLayoutReloaded()
    {
        //Unregistered menu and all children
        Fugui.UnregisterMainMenuItem("Layout");

        //Register the layout menu empty
        Fugui.RegisterMainMenuItem("Layout", null);

        foreach (KeyValuePair<string, FuDockingLayoutDefinition> layoutDefinition in FuDockingLayoutManager.Layouts)
        {
            //Add new children
            string menuName = Fugui.AddSpacesBeforeUppercase(layoutDefinition.Key);
            if (!Fugui.IsMainMenuRegisteredItem(menuName))
            {
                Fugui.RegisterMainMenuItem(menuName, () => FuDockingLayoutManager.SetLayout(layoutDefinition.Value, layoutDefinition.Key), "Layout");
            }
        }
    }

    /// <summary>
    /// Whenever the Fugui render Context on the main container do a render tick
    /// </summary>
    private void MainContainerContext_OnRender()
    {
        // draw imgui demo winfow (if _showImGuiDemoWindow is true)
        if (_showImGuiDemoWindow)
        {
            ImGuiNET.ImGui.ShowDemoWindow(ref _showImGuiDemoWindow);
        }
    }
}