using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Fugui.Framework
{
    /// <summary>
    /// A static class for managing the layout of UI windows.
    /// </summary>
    public static class DockingLayoutManager
    {
        #region Variables
        internal const string FUGUI_WINDOWS_DEFINTION_ENUM_PATH = "Assets\\Runtime\\Settings\\FuGuiWindows.cs";
        internal static Dictionary<int, string> _fuguiWindows;
        internal static string _windowsToAdd = string.Empty;
        internal static string _selectedValue = string.Empty;
        internal static Dictionary<int, string> _dockSpaces;
        internal static Dictionary<string, string> _dockSpacesToWindow;
        /// <summary>
        /// Whatever we already are setting Layer right now
        /// </summary>
        public static bool IsSettingLayout { get; private set; }
        public static event Action OnDockLayoutInitialized;
        #endregion

        /// <summary>
        /// static ctor of this class
        /// </summary>
        static DockingLayoutManager()
        {
            _fuguiWindows = enumToDictionary(typeof(FuGuiWindows));

            _dockSpaces = new Dictionary<int, string>();
            _dockSpaces.Add(0, "Center");
            _dockSpaces.Add(1, "Top");
            _dockSpaces.Add(2, "Bottom");
            _dockSpaces.Add(3, "Right");
            _dockSpaces.Add(4, "Left");

            _dockSpacesToWindow = new Dictionary<string, string>();

            foreach (KeyValuePair<int, string> fuguiWindow in _fuguiWindows)
            {
                if (fuguiWindow.Value != "None")
                {
                    _dockSpacesToWindow.Add(fuguiWindow.Value, "Center");
                }
            }
        }

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

        /// <summary>
        /// break the current DOckingLayout and create a new empty one
        /// </summary>
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
            List<FuGuiWindows> windowsToGet = new List<FuGuiWindows>()
            {
                FuGuiWindows.Tree,
                FuGuiWindows.Captures,
                FuGuiWindows.Inspector,
                FuGuiWindows.Metadata,
                FuGuiWindows.ToolBox,
                FuGuiWindows.MainCameraView,
                FuGuiWindows.Theme
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

                ImGuiDocking.DockBuilderDockWindow(windows[FuGuiWindows.Tree].ID, left);
                ImGuiDocking.DockBuilderDockWindow(windows[FuGuiWindows.Captures].ID, left);
                ImGuiDocking.DockBuilderDockWindow(windows[FuGuiWindows.Inspector].ID, right);
                ImGuiDocking.DockBuilderDockWindow(windows[FuGuiWindows.Metadata].ID, right);
                ImGuiDocking.DockBuilderDockWindow(windows[FuGuiWindows.ToolBox].ID, rightBottom);
                ImGuiDocking.DockBuilderDockWindow(windows[FuGuiWindows.MainCameraView].ID, center);
                ImGuiDocking.DockBuilderDockWindow(windows[FuGuiWindows.Theme].ID, centerBottom);
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
            List<FuGuiWindows> windowsToGet = new List<FuGuiWindows>()
            {
                FuGuiWindows.Tree,
                FuGuiWindows.Captures,
                FuGuiWindows.Inspector,
                FuGuiWindows.Metadata,
                FuGuiWindows.ToolBox,
                FuGuiWindows.MainCameraView,
                FuGuiWindows.Theme
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

                ImGuiDocking.DockBuilderDockWindow(windows[FuGuiWindows.Tree].ID, left);
                ImGuiDocking.DockBuilderDockWindow(windows[FuGuiWindows.Captures].ID, left);
                ImGuiDocking.DockBuilderDockWindow(windows[FuGuiWindows.Inspector].ID, right);
                ImGuiDocking.DockBuilderDockWindow(windows[FuGuiWindows.Metadata].ID, right);
                ImGuiDocking.DockBuilderDockWindow(windows[FuGuiWindows.ToolBox].ID, rightBottom);
                ImGuiDocking.DockBuilderDockWindow(windows[FuGuiWindows.Theme].ID, rightBottom);
                ImGuiDocking.DockBuilderDockWindow(windows[FuGuiWindows.MainCameraView].ID, center);
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
            List<FuGuiWindows> windowsToGet = new List<FuGuiWindows>
            {
                FuGuiWindows.DockSpaceManager,
                FuGuiWindows.WindowsDefinitionManager
            };

            // create needed UIWindows asyncronously and invoke callback whenever every UIWIndows created and ready to be used
            FuGui.CreateWindowsAsync(windowsToGet, (windows) =>
            {
                if (windows.Count != windowsToGet.Count)
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
                ImGuiDocking.DockBuilderDockWindow(windows[FuGuiWindows.DockSpaceManager].ID, left);
                ImGuiDocking.DockBuilderDockWindow(windows[FuGuiWindows.WindowsDefinitionManager].ID, right);
                ImGuiDocking.DockBuilderFinish(mainDockSpace);

                IsSettingLayout = true;
                OnDockLayoutInitialized?.Invoke();
            });
        }

        /// <summary>
        /// Write some string content into a file
        /// </summary>
        /// <param name="filePath">path of the file to write</param>
        /// <param name="content">content to write</param>
        internal static void writeToFile(string filePath, string content)
        {
            // create file if not exists
            if (!File.Exists(filePath))
            {
                File.Create(filePath);
            }

            // write content into the file
            using (var streamWriter = new StreamWriter(filePath, false))
            {
                streamWriter.Write(content);
            }
        }

        /// <summary>
        /// Generate the enum source code for Window Names
        /// </summary>
        /// <param name="enumName">name of the enum</param>
        /// <param name="values">values of the enum</param>
        /// <returns>string that represent source code</returns>
        internal static string generateEnum(string enumName, Dictionary<int, string> values)
        {
            var sb = new StringBuilder();
            // enum namespace and declaration
            sb.AppendLine("namespace Fugui")
                .AppendLine("{")
                .AppendLine("    public enum " + enumName)
                .AppendLine("    {");

            // iterate on values to write enum inner keys
            foreach (var item in values)
            {
                sb.AppendFormat("        {0} = {1},", item.Value, item.Key);
                sb.AppendLine();
            }

            // end of scope
            sb.AppendLine("    }").AppendLine("}");
            return sb.ToString();
        }

        /// <summary>
        /// Convert en enum to a dictionnary
        /// </summary>
        /// <param name="enumType"></param>
        /// <returns></returns>
        internal static Dictionary<int, string> enumToDictionary(Type enumType)
        {
            // get each enum values as int array and write them into dic of key value
            return Enum.GetValues(enumType)
                .Cast<int>()
                .ToDictionary(x => x, x => Enum.GetName(enumType, x));
        }
    }

    // TODO : generate this throw DockingLayoutUI
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