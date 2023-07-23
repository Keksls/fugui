using Fu.Core;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Fu.Framework
{
    /// <summary>
    /// A static class for managing the layout of UI windows.
    /// </summary>
    public static class FuDockingLayoutManager
    {
        #region Variables
        internal static Dictionary<ushort, FuWindowName> RegisteredWindowsNames;
        internal static FuDockingLayoutDefinition CurrentLayout;
        internal static Dictionary<int, string> DefinedDockSpaces;
        internal static ExtensionFilter FlgExtensionFilter;
        public static Dictionary<string, FuDockingLayoutDefinition> Layouts { get; private set; }
        /// <summary>
        /// Whatever we are setting a layout right now
        /// </summary>
        public static bool IsSettingLayout { get; private set; }
        public static event Action OnDockLayoutSet;
        public static event Action OnDockLayoutReloaded;
        public const string FUGUI_DOCKING_LAYOUT_EXTENTION = "fdl";
        #endregion

        /// <summary>
        /// static ctor of this class
        /// </summary>
        static FuDockingLayoutManager()
        {
            //Load layouts
            LoadLayouts();

            // create layout file extention filter
            FlgExtensionFilter = new ExtensionFilter
            {
                Name = "Fugui Layout Configuration",
                Extensions = new string[1] { FUGUI_DOCKING_LAYOUT_EXTENTION }
            };

            // create dockaspace windows definitions
            new FuWindowDefinition(FuSystemWindowsNames.DockSpaceManager, (window) => Fugui.DrawDockSpacelayoutCreator());
            new FuWindowDefinition(FuSystemWindowsNames.WindowsDefinitionManager, (window) => Fugui.DrawWindowsDefinitionManager());

            RegisteredWindowsNames = null;
        }

        /// <summary>
        /// give to the layout manager the custom names of the FuWindows on your application
        /// </summary>
        /// <param name="windowsNames">names of the windows (mostly FuWindowsNames.GetAll())</param>
        public static void Initialize(List<FuWindowName> windowsNames)
        {
            RegisteredWindowsNames = new Dictionary<ushort, FuWindowName>();
            foreach (FuWindowName windowName in windowsNames)
            {
                RegisteredWindowsNames.Add(windowName.ID, windowName);
            }
        }

        /// <summary>
        /// Load all layouts from files
        /// </summary>
        /// <returns>number of loaded layouts</returns>
        private static int LoadLayouts()
        {
            // get folder path
            string folderPath = Path.Combine(Application.streamingAssetsPath, Fugui.Settings.LayoutsFolder);

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
                    Fugui.Fire_OnUIException(ex);
                    return Layouts.Count;
                }
            }

            List<string> files = Directory.GetFiles(folderPath).ToList();

            Layouts = new Dictionary<string, FuDockingLayoutDefinition>();

            // iterate on each file into folder
            foreach (string file in Directory.GetFiles(folderPath))
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                FuDockingLayoutDefinition tempLayout = FuDockingLayoutDefinition.ReadFromFile(file);

                if (tempLayout != null)
                {
                    Layouts.Add(fileName, tempLayout);
                }
            }

            // Select first layout
            if (Layouts.Count > 0)
            {
                KeyValuePair<string, FuDockingLayoutDefinition> firstLayoutInfo = Layouts.ElementAt(0);
                CurrentLayout = firstLayoutInfo.Value;
            }
            else
            {
                CurrentLayout = null;
            }

            OnDockLayoutReloaded?.Invoke();

            // return number of themes loaded
            return Layouts.Count;
        }

        /// <summary>
        /// static method for refreshing dockspace dictionary
        /// </summary>
        internal static void RefreshDockSpaces()
        {
            if (CurrentLayout != null)
            {
                DefinedDockSpaces = getDictionaryFromDockSpace(CurrentLayout);
            }
        }

        /// <summary>
        /// This method takes in a "UIDockSpaceDefinition" object as a parameter and returns a dictionary containing the ID and name of the root object and all its children.
        /// It calls itself recursively on each child
        /// </summary>
        private static Dictionary<int, string> getDictionaryFromDockSpace(FuDockingLayoutDefinition root)
        {
            Dictionary<int, string> dictionary = new Dictionary<int, string>();
            dictionary.Add(-1, "None");
            dictionary.Add((int)root.ID, root.Name);

            foreach (var child in root.Children)
            {
                var childDictionary = getDictionaryFromDockSpace(child);

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
        /// Try to dock the window to current DockingLayoutDefinition
        /// </summary>
        /// <param name="window">widow to dock</param>
        /// <returns>whatever the window has been docked</returns>
        public static bool AutoDockWindow(FuWindow window)
        {
            bool success = false;
            tryAutoDockWindow(window, CurrentLayout, ref success);
            if (success)
            {
                uint MainID = Fugui.MainContainer.Dockspace_id;
                ImGuiDocking.DockBuilderFinish(MainID);
            }
            return success;
        }

        /// <summary>
        /// recursively iterrate over each dockspaces in a dockingLayoutDefinition and dock the window in the right dockSpace
        /// </summary>
        /// <param name="window">window to dock</param>
        /// <param name="dockSpaceDefinition">dockspaceDefinition to iterate on</param>
        /// <param name="success">whatever the window has been docked</param>
        private static void tryAutoDockWindow(FuWindow window, FuDockingLayoutDefinition dockSpaceDefinition, ref bool success)
        {
            foreach (ushort windowID in dockSpaceDefinition.WindowsDefinition)
            {
                if (window.WindowName.ID == windowID)
                {
                    ImGuiDocking.DockBuilderDockWindow(window.ID, dockSpaceDefinition.ID);
                    success = true;
                    return;
                }
            }
            if (dockSpaceDefinition.Children == null)
            {
                return;
            }
            foreach (var child in dockSpaceDefinition.Children)
            {
                tryAutoDockWindow(window, child, ref success);
            }
        }

        /// <summary>
        /// Sets the layout of the DockingLayout manager.
        /// </summary>
        public static void SetConfigurationLayout()
        {
            SetLayout(null, "FuguiConfigurationLayout");
        }

        /// <summary>
        /// Sets the layout of the UI windows to the specified layout.
        /// </summary>
        /// <param name="layoutName">The name of the layout to be set.</param>
        public static void SetLayout(string layoutName)
        {
            if (!Layouts.ContainsKey(layoutName))
            {
                return;
            }

            SetLayout(Layouts[layoutName], layoutName);
        }

        /// <summary>
        /// Sets the layout of the UI windows to the specified layout.
        /// </summary>
        /// <param name="layout">The layout to be set.</param>
        public static void SetLayout(FuDockingLayoutDefinition layout, string layoutName)
        {
            // check whatever the layout manager knows the custom application windows names
            if (RegisteredWindowsNames == null)
            {
                Debug.Log("Can't set layout because Layout Manager is not Initialized." + Environment.NewLine +
                    "Please call FuDockingLayoutManager.Initialize() befose setting a layout.");
                return;
            }

            // check whatever we car set Layer
            if (!canSetLayer())
            {
                return;
            }

            Fugui.ShowPopupMessage("Setting Layout...");
            IsSettingLayout = true;

            // break the current docking nodes data before removing windows
            breakDockingLayout();

            // close all opened UI Window
            Fugui.CloseAllWindowsAsync(() =>
            {
                if (layout == null)
                {
                    setDockSpaceConfigurationLayout(layoutName);
                }
                else
                {
                    createDynamicLayout(layout, layoutName);
                }
            });
        }

        /// <summary>
        /// Method that creates a dynamic layout based on the specified UIDockSpaceDefinition. It first retrieves a list of all the windows definitions associated with the dock space and its children recursively, then creates those windows asynchronously, and finally invokes a callback function to complete the layout creation process.
        /// </summary>
        /// <param name="dockSpaceDefinition">The FuguiDockSpaceDefinition to use for creating the layout</param>
        private static void createDynamicLayout(FuDockingLayoutDefinition dockSpaceDefinition, string layoutName)
        {
            List<FuWindowName> windowsToGet = dockSpaceDefinition.GetAllWindowsNames(true);

            // create needed UIWindows asyncronously and invoke callback whenever every UIWIndows created and ready to be used
            Fugui.CreateWindowsAsync(windowsToGet, (windows) =>
            {
                Fugui.ExecuteAfterRenderWindows(() =>
                {
                    uint MainID = Fugui.MainContainer.Dockspace_id;
                    dockSpaceDefinition.ID = MainID;

                    createDocking(windows, dockSpaceDefinition);
                    selectFirstTabOnEachDockSpaces(windows, dockSpaceDefinition);
                    ImGuiDocking.DockBuilderFinish(MainID);
                    CurrentLayout = dockSpaceDefinition;
                    endSettingLayout();
                });
            });
        }

        /// <summary>
        /// Method that creates a dock layout based on a UIDockSpaceDefinition object, recursively creating child dock spaces and setting their orientation and proportion.
        /// </summary>
        /// <param name="windows">The windows created</param>
        /// <param name="layout">The UIDockSpaceDefinition object representing the layout to create</param>
        private static void createDocking(Dictionary<FuWindowName, FuWindow> windows, FuDockingLayoutDefinition layout)
        {
            switch (layout.Orientation)
            {
                default:
                case UIDockSpaceOrientation.None:
                    break;
                case UIDockSpaceOrientation.Horizontal:
                    if (layout.Proportion > 0.5)
                    {
                        ImGuiDocking.DockBuilderSplitNode(layout.ID, ImGuiDir.Right, 1 - layout.Proportion, out layout.Children[1].ID, out layout.Children[0].ID);
                    }
                    else
                    {
                        ImGuiDocking.DockBuilderSplitNode(layout.ID, ImGuiDir.Left, layout.Proportion, out layout.Children[0].ID, out layout.Children[1].ID);
                    }
                    break;
                case UIDockSpaceOrientation.Vertical:
                    if (layout.Proportion > 0.5)
                    {
                        ImGuiDocking.DockBuilderSplitNode(layout.ID, ImGuiDir.Down, 1 - layout.Proportion, out layout.Children[1].ID, out layout.Children[0].ID);
                    }
                    else
                    {
                        ImGuiDocking.DockBuilderSplitNode(layout.ID, ImGuiDir.Up, layout.Proportion, out layout.Children[0].ID, out layout.Children[1].ID);
                    }
                    break;
            }

            if (layout.WindowsDefinition.Count > 0)
            {
                foreach (ushort windowID in layout.WindowsDefinition)
                {
                    if (windows.ContainsKey(RegisteredWindowsNames[windowID]))
                    {
                        ImGuiDocking.DockBuilderDockWindow(windows[RegisteredWindowsNames[windowID]].ID, layout.ID);
                    }
                }
            }

            foreach (FuDockingLayoutDefinition child in layout.Children)
            {
                createDocking(windows, child);
            }
        }

        private static void selectFirstTabOnEachDockSpaces(Dictionary<FuWindowName, FuWindow> windows, FuDockingLayoutDefinition layout)
        {
            if (layout.WindowsDefinition.Count > 0)
            {
                var instances = Fugui.GetWindowInstances(RegisteredWindowsNames[layout.WindowsDefinition[0]]);
                if (instances.Count > 0)
                {
                    instances[0].ForceFocusOnNextFrame();
                }
            }
            foreach (FuDockingLayoutDefinition child in layout.Children)
            {
                selectFirstTabOnEachDockSpaces(windows, child);
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
            foreach (var pair in Fugui.UIWindows)
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
            uint Dockspace_id = Fugui.MainContainer.Dockspace_id;
            ImGuiDocking.DockBuilderRemoveNode(Dockspace_id); // Clear out existing layout
            ImGuiDocking.DockBuilderAddNode(Dockspace_id, ImGuiDockNodeFlags.None); // Add empty node
            ImGuiDocking.DockBuilderSetNodeSize(Dockspace_id, Fugui.MainContainer.Size);
        }

        /// <summary>
        /// Call this whenever a new layout has just been set
        /// </summary>
        private static void endSettingLayout()
        {
            IsSettingLayout = false;
            OnDockLayoutSet?.Invoke();
            Fugui.ClosePopupMessage();
        }

        /// <summary>
        /// Sets the "dockspace configuration" layout for the UI windows.
        /// </summary>
        private static void setDockSpaceConfigurationLayout(string layoutName)
        {
            List<FuWindowName> windowsToGet = new List<FuWindowName>
            {
                FuSystemWindowsNames.DockSpaceManager,
                FuSystemWindowsNames.WindowsDefinitionManager
            };

            // create needed UIWindows asyncronously and invoke callback whenever every UIWIndows created and ready to be used
            Fugui.CreateWindowsAsync(windowsToGet, (windows) =>
            {
                if (windows.Count != windowsToGet.Count)
                {
                    Debug.LogError("Layout Error : windows created don't match requested ones. aborted.");
                    return;
                }

                uint Dockspace_id = Fugui.MainContainer.Dockspace_id;
                uint left;
                uint right;
                ImGuiDocking.DockBuilderSplitNode(Dockspace_id, ImGuiDir.Left, 0.7f, out left, out right);
                ImGuiDocking.DockBuilderDockWindow(windows[FuSystemWindowsNames.DockSpaceManager].ID, left);
                ImGuiDocking.DockBuilderDockWindow(windows[FuSystemWindowsNames.WindowsDefinitionManager].ID, right);
                ImGuiDocking.DockBuilderFinish(Dockspace_id);

                CurrentLayout = null;
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
        /// <param name="className">name of the enum</param>
        /// <param name="values">values of the enum</param>
        /// <returns>string that represent source code</returns>
        internal static string generateEnum(string className, Dictionary<ushort, FuWindowName> values)
        {
            var sb = new StringBuilder();
            // enum namespace and declaration
            sb.AppendLine("using System.Runtime.CompilerServices;")
                .AppendLine("using System.Collections.Generic;")
                .AppendLine()
                .AppendLine("namespace Fu.Core")
                .AppendLine("{")
                .AppendLine("    public class " + className + " : FuSystemWindowsNames")
                .AppendLine("    {");

            // iterate on values to write static values
            foreach (var item in values)
            {
                if (item.Key > FuSystemWindowsNames.FuguiReservedLastID)
                {
                    sb.AppendLine("        private static FuWindowName _" + Fugui.RemoveSpaceAndCapitalize(item.Value.Name) + " = new FuWindowName(" + item.Key + ", \"" + item.Value + "\", " + item.Value.AutoInstantiateWindowOnlayoutSet.ToString().ToLower() + ", " + item.Value.IdleFPS + ");")
                        .AppendLine("        public static FuWindowName " + Fugui.RemoveSpaceAndCapitalize(item.Value.Name) + " { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _" + Fugui.RemoveSpaceAndCapitalize(item.Value.Name) + "; }");
                }
            }

            // add get window list
            sb.AppendLine("        public static List<FuWindowName> GetAllWindowsNames()")
                .AppendLine("        {")
                .AppendLine("            return new List<FuWindowName>()")
                .AppendLine("            {");
            foreach (var item in values)
            {
                sb.AppendLine("                _" + Fugui.RemoveSpaceAndCapitalize(item.Value.ToString()) + ",");
            }

            // close scopes
            sb.AppendLine("            };")
                .AppendLine("        }")
                .AppendLine("    }")
                .Append("}");

            return sb.ToString();
        }

        /// <summary>
        /// Method that binds a window definition to a dock space by its name 
        /// </summary>
        /// <param name="windowID">The unique identifier of the window definition to bind</param>
        /// <param name="dockspaceName">The name of the dock space to bind the window definition to</param>
        internal static void bindWindowToDockspace(ushort windowID, string dockspaceName)
        {
            if (CurrentLayout != null)
            {
                CurrentLayout.RemoveWindowsDefinitionInChildren(windowID);

                FuDockingLayoutDefinition tempDockSpace = CurrentLayout.SearchInChildren(dockspaceName);

                if (tempDockSpace != null)
                {
                    if (!tempDockSpace.WindowsDefinition.Contains(windowID))
                    {
                        tempDockSpace.WindowsDefinition.Add(windowID);
                    }
                }
            }
        }

        /// <summary>
        /// Method that gets the name of the dock space that a specific window definition is currently binded to
        /// </summary>
        /// <param name="windowDefID">The unique identifier of the window definition to check for binding</param>
        /// <returns>The name of the dock space that the window definition is currently binded to, or an empty string if the window definition is not currently binded to any dock space</returns>
        internal static string getBindedLayout(ushort windowDefID)
        {
            // Initialize a variable to store the name of the binded dock space
            string bindedDockspaceName = "None";

            // Search for the dock space that the window definition is binded to
            FuDockingLayoutDefinition bindedDockspace = CurrentLayout.SearchInChildren(windowDefID);

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
        internal static void unbindWindowToDockspace(ushort windowDefID)
        {
            if (CurrentLayout != null)
            {
                CurrentLayout.RemoveWindowsDefinitionInChildren(windowDefID);
            }
        }

        #region Create / Save / Delete
        /// <summary>
        /// Create a new layout and select if
        /// </summary>
        internal static void createNewLayout()
        {
            int count = Layouts.Where(file => file.Key.StartsWith("Layout_")).Count();
            string newFileName = "Layout_" + count;

            if (!Layouts.ContainsKey(newFileName))
            {
                Layouts.Add(newFileName, new FuDockingLayoutDefinition(newFileName, 0));

                FuDockingLayoutDefinition newLayout = Layouts[newFileName];
                CurrentLayout = newLayout;
            }
        }

        /// <summary>
        /// Delete currentlky selected layout
        /// </summary>
        internal static void deleteSelectedLayout()
        {
            if (CurrentLayout != null)
            {
                // get folder path
                string folderPath = Path.Combine(Application.streamingAssetsPath, Fugui.Settings.LayoutsFolder);

                // create folder if not exists
                if (Directory.Exists(folderPath))
                {
                    try
                    {
                        string filePathToDelete = Path.Combine(folderPath, CurrentLayout.Name) + "." + FUGUI_DOCKING_LAYOUT_EXTENTION;

                        if (File.Exists(filePathToDelete))
                        {
                            Fugui.ShowModal("Delete Docking Layout", () =>
                            {
                                using (FuLayout layout = new FuLayout())
                                {
                                    layout.TextWrapped("This action cannot be rollbacked. Are you sure you want to continue ?");
                                }
                            }, FuModalSize.Medium, new FuModalButton("Yes", confirmDeleteSelectedLayoutFile, FuButtonStyle.Danger),
                            new FuModalButton("No", null, FuButtonStyle.Default));
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning(ex.Message);
                        Fugui.Notify("Error", ex.Message, StateType.Danger);
                    }
                }
            }
        }

        /// <summary>
        /// Callbacked used for user response after delete layout file
        /// </summary>
        /// <param name="result">User result</param>
        private static void confirmDeleteSelectedLayoutFile()
        {
            try
            {
                string folderPath = Path.Combine(Application.streamingAssetsPath, Fugui.Settings.LayoutsFolder);
                File.Delete(Path.Combine(folderPath, CurrentLayout.Name + "." + FUGUI_DOCKING_LAYOUT_EXTENTION));
                Fugui.Notify("Layout deleted", type: StateType.Success, duration: 2f);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex.GetBaseException().Message);
                Fugui.Notify("Error", ex.GetBaseException().Message, StateType.Danger);
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
            if (CurrentLayout != null)
            {
                saveLayoutFile(CurrentLayout);

                //Reload layouts
                LoadLayouts();
            }
        }

        /// <summary>
        /// Used to format selected layout to FuGui layout configuration file 
        /// </summary>
        internal static void saveLayoutFile(FuDockingLayoutDefinition dockingLayout)
        {
            // get folder path
            string folderPath = Path.Combine(Application.streamingAssetsPath, Fugui.Settings.LayoutsFolder);

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
                    Fugui.Fire_OnUIException(ex);

                    return;
                }
            }

            string fileName = Path.Combine(folderPath, dockingLayout.Name) + "." + FUGUI_DOCKING_LAYOUT_EXTENTION;
            File.WriteAllText(fileName, FuDockingLayoutDefinition.Serialize(dockingLayout));
            Fugui.Notify("Layout saved", type: StateType.Success, duration: 2f);
        }

        internal static bool checkLayoutName(string layoutName)
        {
            return layoutName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
        }
        #endregion
    }
}