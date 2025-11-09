using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework
{
    public class FuLayoutSetter : MonoBehaviour
    {
        [SerializeField]
        private string _layoutName;
        [SerializeField]
        private string _themeName;
        [SerializeField]
        private bool _addWindowsToMainMenu = true;
        [SerializeField]
        private bool _addLayoutsToMainMenu = true;
        [SerializeField]
        private bool _addFuguiToMainMenu = true;
        [SerializeField]
        private bool _showImGuiDemoWindow = false;
        [SerializeField]
        private bool _forceHideMainMenu = false;

        private void Awake()
        {
            // register DockingLayoutManager events
            Fugui.Layouts.OnDockLayoutReloaded += DockingLayoutManager_OnDockLayoutReloaded;
            // register on render event of the main container's context to draw ImGui Demo Window
            Fugui.DefaultContainer.Context.OnRender += MainContainerContext_OnRender;
        }

        private void Start()
        {
            // register the main menu items
            registerMainMenuItems();

            // set default layout (will create UIWindows instances)
            if (Fugui.Layouts.Layouts.Count > 0)
            {
                Fugui.Layouts.SetLayout(_layoutName);
            }

            // set default theme
            if (Fugui.Themes.Themes.Count > 0)
            {
                Fugui.Themes.Themes.TryGetValue(_themeName, out FuTheme theme);
                if (theme != null)
                {
                    Fugui.Themes.SetTheme(theme);
                }
            }
        }

        /// <summary>
        /// Register demo menu settings
        /// </summary>
        private void registerMainMenuItems()
        {
            if (_addFuguiToMainMenu)
            {
                // add Fugui menu
                Fugui.RegisterMainMenuItem("Fugui", null);
                // add 'Settings' menu items to open settings (its a child of 'Fugui' item)
                Fugui.RegisterMainMenuItem(FuIcons.Settings_duotone + " Settings", () => Fugui.CreateWindowAsync(FuSystemWindowsNames.FuguiSettings, null), "Fugui");
                // add 'Settings' menu items to open settings (its a child of 'Fugui' item)
                Fugui.RegisterMainMenuItem(FuIcons.Screen_duotone + " Imgui Demo", () => { _showImGuiDemoWindow = true; }, "Fugui");
            }

            if (_addLayoutsToMainMenu)
            {
                // register all layout so user can switch between them
                foreach (KeyValuePair<string, FuDockingLayoutDefinition> layoutDefinition in Fugui.Layouts.Layouts)
                {
                    string menuName = Fugui.AddSpacesBeforeUppercase(layoutDefinition.Key);
                    if (!Fugui.IsMainMenuRegisteredItem(menuName))
                    {
                        Fugui.RegisterMainMenuItem(menuName, () => Fugui.Layouts.SetLayout(layoutDefinition.Key), "Layout");
                    }
                }
            }

            if (_addWindowsToMainMenu)
            {
                // register all windows from registered windows definitions so user can instantiate them from here
                Fugui.RegisterMainMenuItem("Windows", null);
                foreach (FuWindowDefinition windowName in Fugui.UIWindowsDefinitions.Values)
                {
                    Fugui.RegisterMainMenuItem(windowName.WindowName.ToString(), () => Fugui.CreateWindowAsync(windowName.WindowName, null), "Windows");
                }
            }

            // hide main menu if required
            if (_forceHideMainMenu)
            {
                Fugui.HideMainMenu();
            }
        }

        /// <summary>
        /// Whenever the DockingLayoutManager reload its list of registered Layouts
        /// </summary>
        private void DockingLayoutManager_OnDockLayoutReloaded()
        {
            if (!_addLayoutsToMainMenu)
                return;
            //Unregistered menu and all children
            Fugui.UnregisterMainMenuItem("Layout");

            //Register the layout menu empty
            Fugui.RegisterMainMenuItem("Layout", null);

            foreach (KeyValuePair<string, FuDockingLayoutDefinition> layoutDefinition in Fugui.Layouts.Layouts)
            {
                //Add new children
                string menuName = Fugui.AddSpacesBeforeUppercase(layoutDefinition.Key);
                if (!Fugui.IsMainMenuRegisteredItem(menuName))
                {
                    Fugui.RegisterMainMenuItem(menuName, () => Fugui.Layouts.SetLayout(layoutDefinition.Value), "Layout");
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
}