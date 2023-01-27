using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        internal static UIDockSpaceDefinition DisplayedLayout;
        internal static string DisplayLayoutName = "";
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

            // Select first layout
            if (Layouts.Count > 0)
            {
                KeyValuePair<string, UIDockSpaceDefinition> firstLayoutInfo = Layouts.ElementAt(0);
                DisplayedLayout = firstLayoutInfo.Value;
                DisplayLayoutName = firstLayoutInfo.Key;
            }
            else
            {
                DisplayedLayout = null;
                DisplayLayoutName = string.Empty;
            }

            // return number of themes loaded
            return Layouts.Count;
        }

        /// <summary>
        /// static method for refreshing dockspace dictionary
        /// </summary>
        internal static void RefreshDockSpaces()
        {
            if (DisplayedLayout != null)
            {
                _definedDockSpaces = getDictionary(DisplayedLayout);
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
            dictionary.Add((int)root.ID, root.Name);

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
                    Debug.LogError("Layout Error : windows created don't match requested ones. aborted.");
                    return;
                }

                uint Dockspace_id = FuGui.MainContainer.Dockspace_id;
                uint left;
                uint right;
                ImGuiDocking.DockBuilderSplitNode(Dockspace_id, ImGuiDir.Left, 0.7f, out left, out right);
                ImGuiDocking.DockBuilderDockWindow(windows[FuguiWindows.MainCameraView].ID, left);
                ImGuiDocking.DockBuilderDockWindow(windows[FuguiWindows.DockSpaceManager].ID, left);
                ImGuiDocking.DockBuilderDockWindow(windows[FuguiWindows.WindowsDefinitionManager].ID, right);
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
            if (DisplayedLayout != null)
            {
                DisplayedLayout.RemoveWindowsDefinitionInChildren(windowDefID);

                UIDockSpaceDefinition tempDockSpace = DisplayedLayout.SearchInChildren(dockspaceName);

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
        /// Method that gets the name of the dock space that a specific window definition is currently binded to
        /// </summary>
        /// <param name="windowDefID">The unique identifier of the window definition to check for binding</param>
        /// <returns>The name of the dock space that the window definition is currently binded to, or an empty string if the window definition is not currently binded to any dock space</returns>
        internal static string getBindedLayout(int windowDefID)
        {
            // Initialize a variable to store the name of the binded dock space
            string bindedDockspaceName = "None";

            // Search for the dock space that the window definition is binded to
            UIDockSpaceDefinition bindedDockspace = DisplayedLayout.SearchInChildren(windowDefID);

            // If the dock space is found
            if (bindedDockspace != null)
            {
                // Get the name of the dock space
                bindedDockspaceName = bindedDockspace.Name;
            }

            // Return the name of the binded dock space
            return bindedDockspaceName;
        }

        /// <summary>
        /// This function removes the entry in the WindowsDefinition dictionary that corresponds to the given windowDefID from all children of the _dockSpaceDefinitionRoot object recursively.
        /// </summary>
        /// <param name="windowDefID">The unique identifier of the window definition to bind<</param>
        internal static void unbindWindowToDockspace(int windowDefID)
        {
            if (DisplayedLayout != null)
            {
                DisplayedLayout.RemoveWindowsDefinitionInChildren(windowDefID);
            }
        }

        #region SAVE / DELETE LAYOUT FUNCTIONS
        /// <summary>
        /// Create a new layout and select if
        /// </summary>
        internal static void createNewLayout()
        {
            int count = Layouts.Where(file => file.Key.StartsWith("New layout")).Count();
            string newFileName = "New_layout_" + count + ".flg";

            if (!Layouts.ContainsKey(newFileName))
            {
                Layouts.Add(newFileName, new UIDockSpaceDefinition(newFileName, 0));

                UIDockSpaceDefinition newLayout = Layouts[newFileName];
                DisplayedLayout = newLayout;
                DisplayLayoutName = newFileName;
            }
        }

        /// <summary>
        /// Delete currentlky selected layout
        /// </summary>
        internal static void deleteSelectedLayout()
        {
            if (!string.IsNullOrEmpty(DisplayLayoutName))
            {
                // get folder path
                string folderPath = Path.Combine(Application.streamingAssetsPath, FuGui.Settings.LayoutsFolder);

                // create folder if not exists
                if (Directory.Exists(folderPath))
                {
                    try
                    {
                        string filePathToDelete = Path.Combine(folderPath, DisplayLayoutName);

                        if (File.Exists(filePathToDelete))
                        {
                            FuGui.ShowYesNoModal("This action cannot be rollbacked. Are you sure you want to continue ?", confirmDeleteSelectedLayoutFile, UIModalSize.ExtraLarge);
                        }

                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning(ex.GetBaseException().Message);
                        FuGui.Notify("Error", ex.GetBaseException().Message, StateType.Danger);
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
                File.Delete(Path.Combine(folderPath, DisplayLayoutName));
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex.GetBaseException().Message);
                FuGui.Notify("Error", ex.GetBaseException().Message, StateType.Danger);
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
            if (DisplayedLayout != null)
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
                        FuGui.Notify("Error", ex.GetBaseException().Message, StateType.Danger);

                        return;
                    }
                }

                string fileName = Path.Combine(folderPath, DisplayLayoutName);

                // If file already exists, ask question
                if (File.Exists(fileName))
                {
                    FuGui.ShowYesNoModal(DisplayLayoutName + " already exits. Are you sure you want to overwrite it ?", confirmSaveLayoutFileAlreadyExists, UIModalSize.Large);
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

            string fileName = Path.Combine(folderPath, DisplayLayoutName);
            File.WriteAllText(fileName, UIDockSpaceDefinition.Serialize(DisplayedLayout));
        }

        internal static bool checkSelectedName()
        {
            string pattern = @"^[a-zA-Z0-9_-]+\.flg$";

            return (!string.IsNullOrEmpty(DisplayLayoutName) && Regex.IsMatch(DisplayLayoutName, pattern));
        }

        #endregion
    }
}