using Fugui.Core;
using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fugui.Framework
{
    /// <summary>
    /// A static class for managing the layout of UI windows.
    /// </summary>
    public static class DockingLayoutManager
    {
        /// <summary>
        /// Whatever we already are setting Layer right now
        /// </summary>
        public static bool IsSettingLayout { get; private set; }
        public static event Action OnDockLayoutInitialized;

        /// <summary>
        /// Sets the layout of the UI windows to the specified layout.
        /// </summary>
        /// <param name="layout">The layout to be set.</param>
        public static void SetLayout(UIDockingLayout layout)
        {
            // check whatever we car set Layer
            if (!canSetLayer())
            {
                return;
            }

            IsSettingLayout = true;

            // break the current docking nodes data before removing windows
            breakDockingLayout();

            // close all opened UI Window
            FuGui.CloseAllWindowsAsync(() =>
            {
                // Switch on the layout
                switch (layout)
                {
                    // If the layout is not recognized, fall back to the default layout
                    default:
                    case UIDockingLayout.Default:
                        setDefaultLayout();
                        break;

                    case UIDockingLayout.Console:
                        setConsoleLayout();
                        break;

                    case UIDockingLayout.DockSpaceConfiguration:
                        setDockSpaceConfigurationLayout();
                        break;
                }
            });
        }

        /// <summary>
        /// whetever we can set a Layer now
        /// </summary>
        /// <returns>true if possible</returns>
        private static bool canSetLayer()
        {
            // already setting Layer
            if (IsSettingLayout)
            {
                return false;
            }

            // check whatever a window is busy (changing container, initializing or quitting contexts, etc)
            foreach (var pair in FuGui.UIWindows)
            {
                if (pair.Value.IsBusy)
                {
                    return false;
                }
            }

            return true;
        }

        private static void breakDockingLayout()
        {
            uint Dockspace_id = FuGui.MainContainer.Dockspace_id;
            ImGuiDocking.DockBuilderRemoveNode(Dockspace_id); // Clear out existing layout
            ImGuiDocking.DockBuilderAddNode(Dockspace_id, ImGuiDockNodeFlags.None); // Add empty node
            ImGuiDocking.DockBuilderSetNodeSize(Dockspace_id, FuGui.MainContainer.Size);
        }

        /// <summary>
        /// Sets the "console" layout for the UI windows.
        /// </summary>
        private static void setConsoleLayout()
        {
            // list windows to get for this layout
            List<int> windowsToGet = new List<int>() {

            };

            // create needed UIWindows asyncronously and invoke callback whenever every UIWIndows created and ready to be used
            FuGui.CreateWindowsAsync(windowsToGet, (windows) =>
            {
                if (windows.Count != windowsToGet.Count)
                {
                    UnityEngine.Debug.LogError("Layout Error : windows created don't match requested ones. aborted.");
                    return;
                }

                //breakDockingLayout();
                uint Dockspace_id = FuGui.MainContainer.Dockspace_id;
                uint left;
                uint right;
                uint rightBottom;
                uint center;
                uint centerBottom;
                ImGuiDocking.DockBuilderSplitNode(Dockspace_id, ImGuiDir.Left, 0.8f, out left, out right);
                ImGuiDocking.DockBuilderSplitNode(right, ImGuiDir.Down, 0.5f, out rightBottom, out right);
                ImGuiDocking.DockBuilderSplitNode(left, ImGuiDir.Right, 0.8f, out center, out left);
                ImGuiDocking.DockBuilderSplitNode(center, ImGuiDir.Down, 0.2f, out centerBottom, out center);

                //ImGuiDocking.DockBuilderDockWindow(windows[UIWindowName.Tree].ID, left);
                //ImGuiDocking.DockBuilderDockWindow(windows[UIWindowName.Captures].ID, left);
                //ImGuiDocking.DockBuilderDockWindow(windows[UIWindowName.Inspector].ID, right);
                //ImGuiDocking.DockBuilderDockWindow(windows[UIWindowName.Metadata].ID, right);
                //ImGuiDocking.DockBuilderDockWindow(windows[UIWindowName.ToolBox].ID, rightBottom);
                //ImGuiDocking.DockBuilderDockWindow(windows[UIWindowName.MainCameraView].ID, center);
                //ImGuiDocking.DockBuilderDockWindow(windows[UIWindowName.Theme].ID, centerBottom);
                ImGuiDocking.DockBuilderFinish(Dockspace_id);

                IsSettingLayout = false;
                OnDockLayoutInitialized?.Invoke();
            });
        }

        /// <summary>
        /// Sets the "default" layout for the UI windows.
        /// </summary>
        private static void setDefaultLayout()
        {
            // list windows to get for this layout
            List<int> windowsToGet = new List<int>() {
            };

            // create needed UIWindows asyncronously and invoke callback whenever every UIWIndows created and ready to be used
            FuGui.CreateWindowsAsync(windowsToGet, (windows) =>
            {
                if (windows.Count != windowsToGet.Count)
                {
                    UnityEngine.Debug.LogError("Layout Error : windows created don't match requested ones. aborted.");
                    return;
                }


                //breakDockingLayout();
                uint Dockspace_id = FuGui.MainContainer.Dockspace_id;
                uint left;
                uint right;
                uint rightBottom;
                uint center;
                ImGuiDocking.DockBuilderSplitNode(Dockspace_id, ImGuiDir.Left, 0.8f, out left, out right);
                ImGuiDocking.DockBuilderSplitNode(right, ImGuiDir.Down, 0.5f, out rightBottom, out right);
                ImGuiDocking.DockBuilderSplitNode(left, ImGuiDir.Right, 0.8f, out center, out left);

                //ImGuiDocking.DockBuilderDockWindow(windows[UIWindowName.Tree].ID, left);
                //ImGuiDocking.DockBuilderDockWindow(windows[UIWindowName.Captures].ID, left);
                //ImGuiDocking.DockBuilderDockWindow(windows[UIWindowName.Inspector].ID, right);
                //ImGuiDocking.DockBuilderDockWindow(windows[UIWindowName.Metadata].ID, right);
                //ImGuiDocking.DockBuilderDockWindow(windows[UIWindowName.ToolBox].ID, rightBottom);
                //ImGuiDocking.DockBuilderDockWindow(windows[UIWindowName.Theme].ID, rightBottom);
                //ImGuiDocking.DockBuilderDockWindow(windows[UIWindowName.MainCameraView].ID, center);
                ImGuiDocking.DockBuilderFinish(Dockspace_id);

                IsSettingLayout = false;
                OnDockLayoutInitialized?.Invoke();
            });
        }

        /// <summary>
        /// Sets the "dockspace configuration" layout for the UI windows.
        /// </summary>
        private static void setDockSpaceConfigurationLayout()
        {
            List<int> registeredWindowsKeys = FuGui.GetRegisteredWindows();

            // create needed UIWindows asyncronously and invoke callback whenever every UIWIndows created and ready to be used
            FuGui.CreateWindowsAsync(registeredWindowsKeys, (windows) =>
            {
                if (windows.Count != registeredWindowsKeys.Count)
                {
                    UnityEngine.Debug.LogError("Layout Error : windows created don't match requested ones. aborted.");
                    return;
                }

                uint mainDockSpace = FuGui.MainContainer.Dockspace_id;
                uint left;
                uint right;
                uint center;
                uint bottom;
                ImGuiDocking.DockBuilderSplitNode(mainDockSpace, ImGuiDir.Up, 0.1f, out bottom, out center);
                ImGuiDocking.DockBuilderSplitNode(center, ImGuiDir.Left, 0.5f, out left, out right);
                ImGuiDocking.DockBuilderDockWindow(windows[0].ID, left);
                ImGuiDocking.DockBuilderDockWindow(windows[1].ID, right);
                ImGuiDocking.DockBuilderFinish(mainDockSpace);

                IsSettingLayout = true;
                OnDockLayoutInitialized?.Invoke();
            });
        }
    }

    /// <summary>
    /// An enumeration of possible UI layouts.
    /// </summary>
    public enum UIDockingLayout
    {
        /// <summary>
        /// The default layout.
        /// </summary>
        Default,
        /// <summary>
        /// The "Console" layout.
        /// </summary>
        Console,
        /// <summary>
        /// The "DockSpace Configuration" layout.
        /// </summary>
        DockSpaceConfiguration
    }
}