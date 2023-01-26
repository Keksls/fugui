using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Fugui.Framework
{
    /// <summary>
    /// A static class for managing the layout of UI windows.
    /// </summary>
    public static class DockingLayoutManager
    {
        #region Variables
        internal const string FUGUI_WINDOWS_DEF_ENUM_PATH = "Assets\\Runtime\\Settings\\FuGuiWindows.cs";
        internal const string FUGUI_DOCKSPACE_FOLDER_PATH = "Assets\\Runtime\\Settings\\Layout\\";
        internal static string _layoutFileName = "default_layout.flg";
        internal static Dictionary<int, string> _fuguiWindows;
        internal static string _windowsToAdd = string.Empty;
        internal static string _selectedWindowDefinition = string.Empty;
        internal static UIDockSpaceDefinition _displayedLayout;
        internal static string _displayedLayoutName = "";
        internal static Dictionary<int, string> _definedDockSpaces;
        internal static ExtensionFilter _flgExtensionFilter;
        public static Dictionary<string, UIDockSpaceDefinition> Layouts { get; private set; }
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
            //Load layouts
            LoadLayouts();

            _flgExtensionFilter = new ExtensionFilter
            {
                Name = "Fugui Layout Configuration",
                Extensions = new string[1] { "flg" }
            };

            _fuguiWindows = enumToDictionary(typeof(FuguiWindows));
        }

        private static int LoadLayouts()
        {            
            // get folder path
            string folderPath = Path.Combine(Application.streamingAssetsPath, FuGui.Settings.LayoutsFolder);

            // create folder if not exists
            if (!Directory.Exists(folderPath))
            {
                try
                {
                    // try to create directory if not exists
                    Directory.CreateDirectory(folderPath);
                }
                catch (Exception ex)
                {
                    // something gone wrong, let's invoke Fugui Exception event
                    FuGui.DoOnUIException(ex);
                    return Layouts.Count;
                }
            }

            List<string> files = Directory.GetFiles(folderPath).ToList();

            Layouts = new Dictionary<string, UIDockSpaceDefinition>();

            // iterate on each file into folder
            foreach (string file in Directory.GetFiles(folderPath))
            {
                string fileName = Path.GetFileName(file);
                UIDockSpaceDefinition tempLayout = UIDockSpaceDefinition.ReadFromFile(file);

                if (tempLayout != null)
                {
                    Layouts.Add(fileName, tempLayout);
                }
            }

            // return number of themes loaded
            return Layouts.Count;
        }

        /// <summary>
        /// static method for refreshing dockspace dictionary
        /// </summary>
        internal static void RefreshDockSpaces()
        {
            if (_displayedLayout != null)
            {
                _definedDockSpaces = getDictionary(_displayedLayout);
            }
        }

        /// <summary>
        /// This method takes in a "UIDockSpaceDefinition" object as a parameter and returns a dictionary containing the ID and name of the root object and all its children.
        /// It calls itself recursively on each child
        /// </summary>
        private static Dictionary<int, string> getDictionary(UIDockSpaceDefinition root)
        {
            Dictionary<int, string> dictionary = new Dictionary<int, string>();
            dictionary.Add(-1, "None");
            dictionary.Add((int) root.ID, root.Name);

            foreach (var child in root.Children)
            {
                var childDictionary = getDictionary(child);

                foreach (KeyValuePair<int, string> entry in childDictionary)
                {
                    if (!dictionary.ContainsKey(entry.Key))
                    {
                        dictionary.Add(entry.Key, entry.Value);
                    }
                }
            }

            return dictionary;
        }

        /// <summary>
        /// Sets the layout of the DockingLayout manager.
        /// </summary>
        public static void SetConfigurationLayout()
        {
            SetLayout(null);
        }

        /// <summary>
        /// Sets the layout of the UI windows to the specified layout.
        /// </summary>
        /// <param name="layout">The layout to be set.</param>
        public static void SetLayout(UIDockSpaceDefinition layout)
        {
            // check whatever we car set Layer
            if (!canSetLayer())
            {
                return;
            }

            FuGui.ShowPopupMessage("Setting Layout...");
            IsSettingLayout = true;

            // break the current docking nodes data before removing windows
            breakDockingLayout();

            // close all opened UI Window
            FuGui.CloseAllWindowsAsync(() =>
            {
                if (layout == null)
                {
                    //setDefaultLayout();
                    setDockSpaceConfigurationLayout();
                }
                else
                {
                    createDynamicLayout(layout);
                }
            });
        }

        /// <summary>
        /// Method that creates a dynamic layout based on the specified UIDockSpaceDefinition. It first retrieves a list of all the windows definitions associated with the dock space and its children recursively, then creates those windows asynchronously, and finally invokes a callback function to complete the layout creation process.
        /// </summary>
        /// <param name="layout">The UIDockSpaceDefinition to use for creating the layout</param>
        private static void createDynamicLayout(UIDockSpaceDefinition layout)
        {
            List<FuguiWindows> windowsToGet = layout.GetAllWindowsDefinitions();

            // create needed UIWindows asyncronously and invoke callback whenever every UIWIndows created and ready to be used
            FuGui.CreateWindowsAsync(windowsToGet, (windows) =>
            {
                if (windows.Count != windowsToGet.Count)
                {
                    Debug.LogError("Layout Error : windows created don't match requested ones. aborted.");
                    return;
                }

                uint MainID = FuGui.MainContainer.Dockspace_id;
                layout.ID = MainID;

                createDocking(windows, layout);

                ImGuiDocking.DockBuilderFinish(MainID);

                endSettingLayout();
            });
        }

        /// <summary>
        /// Method that creates a dock layout based on a UIDockSpaceDefinition object, recursively creating child dock spaces and setting their orientation and proportion.
        /// </summary>
        /// <param name="windows">The windows created</param>
        /// <param name="layout">The UIDockSpaceDefinition object representing the layout to create</param>
        private static void createDocking(Dictionary<FuguiWindows, Core.UIWindow> windows, UIDockSpaceDefinition layout)
        {
            switch (layout.Orientation)
            {
                default:
                case UIDockSpaceOrientation.None:
                    break;
                case UIDockSpaceOrientation.Horizontal:
                    ImGuiDocking.DockBuilderSplitNode(layout.ID, ImGuiDir.Left, layout.Proportion, out layout.Children[0].ID, out layout.Children[1].ID);
                    break;
                case UIDockSpaceOrientation.Vertical:
                    ImGuiDocking.DockBuilderSplitNode(layout.ID, ImGuiDir.Up, layout.Proportion, out layout.Children[0].ID, out layout.Children[1].ID);
                    break;
            }

            if (layout.WindowsDefinition.Count > 0)
            {
                foreach (KeyValuePair<int, string> winDef in layout.WindowsDefinition)
                {
                    ImGuiDocking.DockBuilderDockWindow(windows[(FuguiWindows)winDef.Key].ID, layout.ID);
                }
            }

            foreach (UIDockSpaceDefinition child in layout.Children)
            {
                createDocking(windows, child);
            }
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
        /// Call this whenever a new layout has just been set
        /// </summary>
        private static void endSettingLayout()
        {
            IsSettingLayout = false;
            OnDockLayoutInitialized?.Invoke();
            FuGui.ClosePopupMessage();
        }

        /// <summary>
        /// Sets the "console" layout for the UI windows.
        /// </summary>
        private static void setConsoleLayout()
        {
            // list windows to get for this layout
            List<FuguiWindows> windowsToGet = new List<FuguiWindows>()
            {
                FuguiWindows.Tree,
                FuguiWindows.Captures,
                FuguiWindows.Inspector,
                FuguiWindows.Metadata,
                FuguiWindows.ToolBox,
                FuguiWindows.MainCameraView,
                FuguiWindows.FuguiSettings
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

                ImGuiDocking.DockBuilderDockWindow(windows[FuguiWindows.Tree].ID, left);
                ImGuiDocking.DockBuilderDockWindow(windows[FuguiWindows.Captures].ID, left);
                ImGuiDocking.DockBuilderDockWindow(windows[FuguiWindows.Inspector].ID, right);
                ImGuiDocking.DockBuilderDockWindow(windows[FuguiWindows.Metadata].ID, right);
                ImGuiDocking.DockBuilderDockWindow(windows[FuguiWindows.ToolBox].ID, rightBottom);
                ImGuiDocking.DockBuilderDockWindow(windows[FuguiWindows.MainCameraView].ID, center);
                ImGuiDocking.DockBuilderDockWindow(windows[FuguiWindows.FuguiSettings].ID, centerBottom);
                ImGuiDocking.DockBuilderFinish(Dockspace_id);

                endSettingLayout();
            });
        }

        /// <summary>
        /// Sets the "default" layout for the UI windows.
        /// </summary>
        private static void setDefaultLayout()
        {
            // list windows to get for this layout
            List<FuguiWindows> windowsToGet = new List<FuguiWindows>()
            {
                FuguiWindows.Tree,
                FuguiWindows.Captures,
                FuguiWindows.Inspector,
                FuguiWindows.Metadata,
                FuguiWindows.ToolBox,
                FuguiWindows.MainCameraView,
                FuguiWindows.FuguiSettings
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
                uint tempLeft;
                uint tempRight;
                uint left;
                uint right;
                uint rightBottom;
                uint center;
                ImGuiDocking.DockBuilderSplitNode(Dockspace_id, ImGuiDir.Left, 0.8f, out tempLeft, out tempRight);
                ImGuiDocking.DockBuilderSplitNode(tempRight, ImGuiDir.Up, 0.5f, out right, out rightBottom);
                ImGuiDocking.DockBuilderSplitNode(tempLeft, ImGuiDir.Left, 0.2f, out left, out center);

                ImGuiDocking.DockBuilderDockWindow(windows[FuguiWindows.Tree].ID, left);
                ImGuiDocking.DockBuilderDockWindow(windows[FuguiWindows.Captures].ID, left);
                ImGuiDocking.DockBuilderDockWindow(windows[FuguiWindows.Inspector].ID, right);
                ImGuiDocking.DockBuilderDockWindow(windows[FuguiWindows.Metadata].ID, right);
                ImGuiDocking.DockBuilderDockWindow(windows[FuguiWindows.ToolBox].ID, rightBottom);
                ImGuiDocking.DockBuilderDockWindow(windows[FuguiWindows.FuguiSettings].ID, rightBottom);
                ImGuiDocking.DockBuilderDockWindow(windows[FuguiWindows.MainCameraView].ID, center);
                ImGuiDocking.DockBuilderFinish(Dockspace_id);

                endSettingLayout();
            });
        }

        /// <summary>
        /// Sets the "dockspace configuration" layout for the UI windows.
        /// </summary>
        private static void setDockSpaceConfigurationLayout()
        {
            List<FuguiWindows> windowsToGet = new List<FuguiWindows>
            {
                FuguiWindows.DockSpaceManager,
                FuguiWindows.WindowsDefinitionManager,
                FuguiWindows.MainCameraView
            };

            // create needed UIWindows asyncronously and invoke callback whenever every UIWIndows created and ready to be used
            FuGui.CreateWindowsAsync(windowsToGet, (windows) =>
            {
                if (windows.Count != windowsToGet.Count)
                {
                    UnityEngine.Debug.LogError("Layout Error : windows created don't match requested ones. aborted.");
                    return;
                }

                uint Dockspace_id = FuGui.MainContainer.Dockspace_id;
                uint left;
                uint right;
                ImGuiDocking.DockBuilderSplitNode(Dockspace_id, ImGuiDir.Left, 0.7f, out left, out right);
                ImGuiDocking.DockBuilderDockWindow(windows[FuguiWindows.DockSpaceManager].ID, left);
                ImGuiDocking.DockBuilderDockWindow(windows[FuguiWindows.WindowsDefinitionManager].ID, right);
                ImGuiDocking.DockBuilderDockWindow(windows[FuguiWindows.MainCameraView].ID, right);
                ImGuiDocking.DockBuilderFinish(Dockspace_id);

                endSettingLayout();
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

        /// <summary>
        /// Method that binds a window definition to a dock space by its name 
        /// </summary>
        /// <param name="windowDefID">The unique identifier of the window definition to bind</param>
        /// <param name="dockspaceName">The name of the dock space to bind the window definition to</param>
        internal static void bindWindowToDockspace(int windowDefID, string dockspaceName)
        {
            if (_displayedLayout != null)
            {
                _displayedLayout.RemoveWindowsDefinitionInChildren(windowDefID);

                UIDockSpaceDefinition tempDockSpace = _displayedLayout.SearchInChildren(dockspaceName);

                if (tempDockSpace != null)
                {
                    if (!tempDockSpace.WindowsDefinition.ContainsKey(windowDefID))
                    {
                        tempDockSpace.WindowsDefinition.Add(windowDefID, _fuguiWindows[windowDefID]);
                    }
                }
            }            
        }

        /// <summary>
        /// This function removes the entry in the WindowsDefinition dictionary that corresponds to the given windowDefID from all children of the _dockSpaceDefinitionRoot object recursively.
        /// </summary>
        /// <param name="windowDefID">The unique identifier of the window definition to bind<</param>
        internal static void unbindWindowToDockspace(int windowDefID)
        {
            if (_displayedLayout != null)
            {
                _displayedLayout.RemoveWindowsDefinitionInChildren(windowDefID);
            }
        }

        #region SAVE / DELETE LAYOUT FUNCTIONS

        /// <summary>
        /// Delete currentlky selected layout
        /// </summary>
        internal static void deleteSelectedLayout()
        {
            if (!string.IsNullOrEmpty(_displayedLayoutName))
            {
                // get folder path
                string folderPath = Path.Combine(Application.streamingAssetsPath, FuGui.Settings.LayoutsFolder);

                // create folder if not exists
                if (Directory.Exists(folderPath))
                {
                    try
                    {
                        string filePathToDelete = Path.Combine(folderPath, _displayedLayoutName);

                        if (File.Exists(filePathToDelete))
                        {
                            FuGui.ShowYesNoModal("This action cannot be rollbacked. Are you sure you want to continue ?", confirmDeleteSelectedLayoutFile, UIModalSize.ExtraLarge);
                        }

                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning(ex.GetBaseException().Message);
                    }
                    finally
                    {
                        LoadLayouts();
                    }
                }
            }
        }

        /// <summary>
        /// Callbacked used for user response after delete layout file
        /// </summary>
        /// <param name="result">User result</param>

        private static void confirmDeleteSelectedLayoutFile(bool result)
        {
            try
            {
                string folderPath = Path.Combine(Application.streamingAssetsPath, FuGui.Settings.LayoutsFolder);
                File.Delete(Path.Combine(folderPath, _displayedLayoutName));
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex.GetBaseException().Message);
            }
            finally
            {
                LoadLayouts();
            }
        }

        /// <summary>
        /// Save selected layout
        /// </summary>
        internal static void saveSelectedLayout()
        {
            if (_displayedLayout != null)
            {
                // get folder path
                string folderPath = Path.Combine(Application.streamingAssetsPath, FuGui.Settings.LayoutsFolder);

                // create folder if not exists
                if (!Directory.Exists(folderPath))
                {
                    try
                    {
                        // try to create directory if not exists
                        Directory.CreateDirectory(folderPath);
                    }
                    catch (Exception ex)
                    {
                        // something gone wrong, let's invoke Fugui Exception event
                        FuGui.DoOnUIException(ex);

                        return;
                    }
                }

                string fileName = Path.Combine(folderPath, _displayedLayoutName);

                // If file already exists, ask question
                if (File.Exists(fileName))
                {
                    FuGui.ShowYesNoModal(_displayedLayoutName + " already exits. Are you sure you want to overwrite it ?", confirmSaveLayoutFileAlreadyExists, UIModalSize.Large);
                }
                else
                {
                    //Save file
                    saveLayoutFile();
                }

                //Reload layouts
                LoadLayouts();
            }
        }

        /// <summary>
        /// Callbacked used for user response after overwrite layout file
        /// </summary>
        /// <param name="result">User result</param>
        private static void confirmSaveLayoutFileAlreadyExists(bool result)
        { 
            if (result)
            {
                saveLayoutFile();
            }
        }

        /// <summary>
        /// Used to format selected layout to FuGui layout configuration file 
        /// </summary>
        private static void saveLayoutFile()
        {
            // get folder path
            string folderPath = Path.Combine(Application.streamingAssetsPath, FuGui.Settings.LayoutsFolder);

            // create folder if not exists
            if (!Directory.Exists(folderPath))
            {
                try
                {
                    // try to create directory if not exists
                    Directory.CreateDirectory(folderPath);
                }
                catch (Exception ex)
                {
                    // something gone wrong, let's invoke Fugui Exception event
                    FuGui.DoOnUIException(ex);

                    return;
                }
            }

            string fileName = Path.Combine(folderPath, _displayedLayoutName);
            File.WriteAllText(fileName, UIDockSpaceDefinition.Serialize(_displayedLayout));
        }

        #endregion
    }
}