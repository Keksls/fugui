using Fu.Framework;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// A class for managing the layout of UI windows.
    /// </summary>
    public class FuDockingLayoutManager
    {
        #region State
        public FuDockingLayoutDefinition CurrentLayout { get; internal set; }

        internal ExtensionFilter FlgExtensionFilter;

        public Dictionary<string, FuDockingLayoutDefinition> Layouts { get; private set; }
        /// <summary>
        /// Whatever we are setting a layout right now
        /// </summary>
        public bool IsSettingLayout { get; private set; }

        private bool _hasPendingLayoutRequest;
        private bool _pendingLayoutRetryQueued;
        private FuDockingLayoutDefinition _pendingLayout;
        private bool _pendingGetOnlyAutoInstantiated;

        public event Action OnDockLayoutSet;
        public event Action OnBeforeDockLayoutSet;
        public event Action OnDockLayoutReloaded;

        public const string FUGUI_DOCKING_LAYOUT_EXTENTION = "fdl";
        #endregion

        #region Constructors
        /// <summary>
        /// ctor of this class
        /// </summary>
        public FuDockingLayoutManager()
        {
            Layouts = new Dictionary<string, FuDockingLayoutDefinition>();

            if (Fugui.Settings == null)
                return;

            //Load layouts
            LoadLayouts(Path.Combine(Application.streamingAssetsPath, Fugui.Settings.LayoutsFolder));

            // create layout file extention filter
            FlgExtensionFilter = new ExtensionFilter
            {
                Name = "Fugui Layout Configuration",
                Extensions = new string[1] { FUGUI_DOCKING_LAYOUT_EXTENTION }
            };
        }
        #endregion

        #region Methods
        /// <summary>
        /// Load all layouts from files
        /// </summary>
        /// <returns>number of loaded layouts</returns>
        public int LoadLayouts(string folderPath)
        {
            try
            {
                string indexPath = $"{folderPath}/layouts_index.json";
                string json = Fugui.ReadAllText(indexPath);

                if (!string.IsNullOrEmpty(json))
                {
                    FuLayoutIndex index = JsonUtility.FromJson<FuLayoutIndex>(json);

                    if (index != null && index.Layouts != null)
                    {
                        for (int i = 0; i < index.Layouts.Length; i++)
                        {
                            string layoutName = index.Layouts[i];

                            if (string.IsNullOrEmpty(layoutName))
                                continue;

                            string filePath = $"{folderPath}/{layoutName}.fdl";

                            FuDockingLayoutDefinition tempLayout = FuDockingLayoutDefinition.Deserialize(filePath);

                            if (tempLayout != null && !Layouts.ContainsKey(layoutName))
                            {
                                Layouts.Add(layoutName, tempLayout);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Fugui.Fire_OnUIException(ex);
            }

            // Select first layout
            if (CurrentLayout == null && Layouts.Count > 0)
            {
                var firstLayoutInfo = Layouts.ElementAt(0);
                CurrentLayout = firstLayoutInfo.Value;
            }

            OnDockLayoutReloaded?.Invoke();

            return Layouts.Count;
        }

        /// <summary>
        /// Try to dock the window to current DockingLayoutDefinition
        /// </summary>
        /// <param name="window">widow to dock</param>
        /// <returns>whatever the window has been docked</returns>
        public bool AutoDockWindow(FuWindow window)
        {
            if (CurrentLayout == null)
            {
                return false;
            }

            bool success = false;
            tryAutoDockWindow(window, CurrentLayout, ref success);
            if (success)
            {
                uint MainID = Fugui.DefaultContainer.Dockspace_id;
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
        private unsafe void tryAutoDockWindow(FuWindow window, FuDockingLayoutDefinition dockSpaceDefinition, ref bool success)
        {
            if (window == null || dockSpaceDefinition == null || dockSpaceDefinition.WindowsDefinition == null)
            {
                return;
            }

            foreach (ushort windowID in dockSpaceDefinition.WindowsDefinition)
            {
                if (window.WindowName.ID == windowID && ImGuiDocking.DockBuilderGetNode(dockSpaceDefinition.ID).NativePtr != null)
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
        /// Sets the layout of the UI windows to the specified layout.
        /// </summary>
        /// <param name="layoutName">The name of the layout to be set.</param>
        /// <param name="getOnlyAutoInstantiated">Whatever you only want windows in this layout that will auto instantiated by layout</param>
        public void SetLayout(string layoutName, bool getOnlyAutoInstantiated = true)
        {
            if (!Layouts.ContainsKey(layoutName))
            {
                return;
            }

            SetLayout(Layouts[layoutName], getOnlyAutoInstantiated);
        }

        /// <summary>
        /// Sets the layout of the UI windows to the specified layout.
        /// </summary>
        /// <param name="layout">The layout to be set.</param>
        /// <param name="getOnlyAutoInstantiated">Whatever you only want windows in this layout that will auto instantiated by layout</param>
        public void SetLayout(FuDockingLayoutDefinition layout, bool getOnlyAutoInstantiated = true)
        {
            if (layout == null)
            {
                Debug.LogWarning("[Fugui] Cannot set a null docking layout.");
                return;
            }

            // check whatever we car set Layer
            if (!canSetLayer())
            {
                requestLayoutWhenReady(layout, getOnlyAutoInstantiated);
                return;
            }

            OnBeforeDockLayoutSet?.Invoke();
            Fugui.ShowPopupMessage("Setting Layout...");
            IsSettingLayout = true;

            // break the current docking nodes data before removing windows
            breakDockingLayout();

            // close only windows owned by the regular docking layout.
            closeLayoutWindowsAsync(() =>
            {
                createDynamicLayout(layout, getOnlyAutoInstantiated);
            });
        }

        /// <summary>
        /// Keep the latest layout request and retry it once current window/container transitions are finished.
        /// </summary>
        /// <param name="layout">Layout to set.</param>
        /// <param name="getOnlyAutoInstantiated">Whatever only auto-instantiated windows should be created.</param>
        private void requestLayoutWhenReady(FuDockingLayoutDefinition layout, bool getOnlyAutoInstantiated)
        {
            _pendingLayout = layout;
            _pendingGetOnlyAutoInstantiated = getOnlyAutoInstantiated;
            _hasPendingLayoutRequest = true;

            if (_pendingLayoutRetryQueued)
            {
                return;
            }

            _pendingLayoutRetryQueued = true;
            Fugui.ExecuteInMainThread(trySetPendingLayout);
        }

        /// <summary>
        /// Retry the latest pending layout request.
        /// </summary>
        private void trySetPendingLayout()
        {
            _pendingLayoutRetryQueued = false;

            if (!_hasPendingLayoutRequest)
            {
                return;
            }

            FuDockingLayoutDefinition layout = _pendingLayout;
            bool getOnlyAutoInstantiated = _pendingGetOnlyAutoInstantiated;
            _hasPendingLayoutRequest = false;

            SetLayout(layout, getOnlyAutoInstantiated);
        }

        /// <summary>
        /// Close windows managed by the main docking layout without touching 3D window containers.
        /// </summary>
        /// <param name="callback">Callback invoked once all layout windows are closed.</param>
        private void closeLayoutWindowsAsync(Action callback)
        {
            Fugui.ForceDrawAllWindows();

            List<FuWindow> windows = Fugui.UIWindows.Values
                .Where(window => !window.Is3DWindow)
                .ToList();

            if (windows.Count == 0)
            {
                callback?.Invoke();
                return;
            }

            foreach (FuWindow window in windows)
            {
                void onWindowClosed(FuWindow closedWindow)
                {
                    closedWindow.OnClosed -= onWindowClosed;

                    bool hasLayoutWindows = Fugui.UIWindows.Values.Any(openWindow => !openWindow.Is3DWindow);
                    if (!hasLayoutWindows)
                    {
                        callback?.Invoke();
                    }
                }

                window.OnClosed += onWindowClosed;
                window.Close();
            }
        }

        /// <summary>
        /// Method that creates a dynamic layout based on the specified UIDockSpaceDefinition. It first retrieves a list of all the windows definitions associated with the dock space and its children recursively, then creates those windows asynchronously, and finally invokes a callback function to complete the layout creation process.
        /// </summary>
        /// <param name="dockSpaceDefinition">The FuguiDockSpaceDefinition to use for creating the layout</param>
        /// <param name="getOnlyAutoInstantiated">Whatever you only want windows in this layout that will auto instantiated by layout</param>
        private void createDynamicLayout(FuDockingLayoutDefinition dockSpaceDefinition, bool getOnlyAutoInstantiated)
        {
            List<FuWindowName> windowsToGet = dockSpaceDefinition.GetAllWindowsNames(getOnlyAutoInstantiated);

            // create needed UIWindows asyncronously and invoke callback whenever every UIWIndows created and ready to be used
            Fugui.CreateWindowsAsync(windowsToGet, (windows) =>
            {
                Fugui.ExecuteAfterRenderWindows(() =>
                {
                    uint MainID = Fugui.DefaultContainer.Dockspace_id;
                    dockSpaceDefinition.ID = MainID;

                    createDocking(windows, dockSpaceDefinition);
                    selectFirstTabOnEachDockSpaces(dockSpaceDefinition);
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
        private void createDocking(List<(FuWindowName, FuWindow)> windows, FuDockingLayoutDefinition layout)
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
                    if (!FuWindowNameProvider.GetAllWindowNames().TryGetValue(windowID, out FuWindowName windowName))
                    {
                        continue;
                    }

                    var ids = windows.Where(w => w.Item1.Equals(windowName)).Select(w => w.Item2.ID);
                    foreach (string id in ids)
                    {
                        ImGuiDocking.DockBuilderDockWindow(id, layout.ID);
                    }
                }
            }

            foreach (FuDockingLayoutDefinition child in layout.Children)
            {
                createDocking(windows, child);
            }
        }

        /// <summary>
        /// Get and force focus to select each first tab on each dock spaces (first window of each nodes)
        /// </summary>
        /// <param name="windows">windows to check</param>
        /// <param name="layout">applyed layout</param>
        private void selectFirstTabOnEachDockSpaces(FuDockingLayoutDefinition layout)
        {
            if (layout.WindowsDefinition.Count > 0)
            {
                if (FuWindowNameProvider.GetAllWindowNames().TryGetValue(layout.WindowsDefinition[0], out FuWindowName windowName))
                {
                    var instances = Fugui.GetWindowInstances(windowName);
                    if (instances.Count > 0)
                    {
                        instances[0].ForceFocusOnNextFrame();
                    }
                }
            }
            foreach (FuDockingLayoutDefinition child in layout.Children)
            {
                selectFirstTabOnEachDockSpaces(child);
            }
        }

        /// <summary>
        /// whetever we can set a Layer now
        /// </summary>
        /// <returns>true if possible</returns>
        private bool canSetLayer()
        {
            // already setting Layer
            if (IsSettingLayout)
            {
                return false;
            }

            // check whatever a window is busy (changing container, initializing or quitting contexts, etc)
            foreach (var pair in Fugui.UIWindows)
            {
                if (pair.Value.Is3DWindow)
                {
                    continue;
                }

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
        private void breakDockingLayout()
        {
            uint Dockspace_id = Fugui.DefaultContainer.Dockspace_id;
            ImGuiDocking.DockBuilderRemoveNode(Dockspace_id); // Clear out existing layout
            ImGuiDocking.DockBuilderAddNode(Dockspace_id, ImGuiDockNodeFlags.None); // Add empty node

            // ensure Fugui.MainContainer.Size is more than 0
            Vector2 size = Fugui.DefaultContainer.Size;
            if (size.x <= 0 || size.y <= 0)
            {
                size = new Vector2(800, 600); // Default size if not set
            }

            ImGuiDocking.DockBuilderSetNodeSize(Dockspace_id, size);
        }

        /// <summary>
        /// Call this whenever a new layout has just been set
        /// </summary>
        private void endSettingLayout()
        {
            IsSettingLayout = false;
            OnDockLayoutSet?.Invoke();
            Fugui.ClosePopupMessage();
        }

        /// <summary>
        /// Delete a layout file from disk and remove it from loaded layouts
        /// </summary>
        /// <param name="folderPath"> folder path where layout files are stored</param>
        /// <param name="layoutName"> layout name to delete</param>
        /// <param name="showConfigModal"> whatever you want to show a confirmation modal before delete</param>
        /// <param name="callback"> callback to invoke after delete</param>
        public void DeleteLayout(string folderPath, string layoutName, bool showConfigModal = true, Action callback = null)
        {
            if (Layouts.ContainsKey(layoutName))
            {
                // create folder if not exists
                if (Directory.Exists(folderPath))
                {
                    try
                    {
                        string filePathToDelete = Path.Combine(folderPath, layoutName) + "." + FUGUI_DOCKING_LAYOUT_EXTENTION;

                        if (File.Exists(filePathToDelete))
                        {
                            if (showConfigModal)
                            {
                                Fugui.ShowModal("Delete Docking Layout", (layout) =>
                                {
                                    layout.Text("This action cannot be rollbacked. Are you sure you want to continue ?", FuTextWrapping.Wrap);
                                    if (Fugui.DefaultContainer.Keyboard.GetKeyDown(FuKeysCode.Enter))
                                    {
                                        confirmDeleteSelectedLayoutFile(folderPath, layoutName, callback);
                                        Fugui.CloseModal();
                                    }
                                }, FuModalSize.Medium,
                                new FuModalButton("Yes", () => confirmDeleteSelectedLayoutFile(folderPath, layoutName, callback), FuButtonStyle.Danger, FuKeysCode.Enter),
                                new FuModalButton("No", null, FuButtonStyle.Default, FuKeysCode.Escape));
                            }
                            else
                            {
                                confirmDeleteSelectedLayoutFile(folderPath, layoutName, callback);
                            }
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
        /// <param name="callback">callback to invoke after delete</param>
        /// <param name="folderPath">folder path where layout files are stored</param>
        /// <param name="layoutName"> layout name to delete</param>
        private void confirmDeleteSelectedLayoutFile(string folderPath, string layoutName, Action callback)
        {
            try
            {
                File.Delete(Path.Combine(folderPath, layoutName + "." + FUGUI_DOCKING_LAYOUT_EXTENTION));
                Layouts.Remove(layoutName);
                saveLayoutIndex(folderPath);
                OnDockLayoutReloaded?.Invoke();
                Fugui.Notify("Layout deleted", type: StateType.Success, duration: 2f);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex.GetBaseException().Message);
                Fugui.Notify("Error", ex.GetBaseException().Message, StateType.Danger);
            }
            finally
            {
                callback?.Invoke();
            }
        }

        /// <summary>
        /// Used to format selected layout to FuGui layout configuration file 
        /// </summary>
        public void SaveLayoutFile(string folderPath, FuDockingLayoutDefinition dockingLayout, bool notify = true)
        {
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

            Layouts[dockingLayout.Name] = dockingLayout;
            saveLayoutIndex(folderPath);
            OnDockLayoutReloaded?.Invoke();

            if (notify)
            {
                Fugui.Notify("Layout saved", type: StateType.Success, duration: 2f);
            }
        }

        /// <summary>
        /// Saves the layouts index file next to the layout files.
        /// </summary>
        /// <param name="folderPath">Folder that contains Fugui layout files.</param>
        private void saveLayoutIndex(string folderPath)
        {
            FuLayoutIndex index = new FuLayoutIndex
            {
                Layouts = Layouts.Keys.OrderBy(layoutName => layoutName).ToArray()
            };

            string indexPath = Path.Combine(folderPath, "layouts_index.json");
            File.WriteAllText(indexPath, JsonUtility.ToJson(index, true));
        }

        /// <summary>
        /// Generate current layout synchronously
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public FuDockingLayoutDefinition GenerateCurrentLayout()
        {
            HashSet<uint> visitedDockIDs = new HashSet<uint>();
            List<dockSpaceData> dockSpaces = new List<dockSpaceData>();
            Vector2 padding = new Vector2(2f, 2f);

            try
            {
                //Get visibles windows
                List<FuWindow> visibleWindows = new List<FuWindow>();
                foreach (FuWindow win in Fugui.UIWindows.Values)
                {
                    if (win.IsVisible)
                    {
                        visibleWindows.Add(win);
                    }
                }

                //Force each window to draw to ensure position
                foreach (FuWindow win in visibleWindows)
                {
                    win.ForceDraw();
                }

                foreach (FuWindow win in Fugui.UIWindows.Values)
                {
                    if (win.IsDocked && win.IsOpened && !visitedDockIDs.Contains(win.CurrentDockID))
                    {
                        visitedDockIDs.Add(win.CurrentDockID);

                        dockSpaceData space = new dockSpaceData
                        {
                            Rect = new Rect(win.LocalPosition, win.Size + padding),
                            WindowNames = new List<FuWindowName>()
                        };

                        foreach (FuWindow w in Fugui.UIWindows.Values)
                        {
                            if (w.IsDocked && w.CurrentDockID == win.CurrentDockID)
                            {
                                space.WindowNames.Add(w.WindowName);
                            }
                        }

                        dockSpaces.Add(space);
                    }
                }

                //Dockspaces fusion
                int safety = 0;
                while (dockSpaces.Count > 1 && safety < 100)
                {
                    dockSpaces = dockSpaces.OrderBy(x => x.Rect.size.x * x.Rect.size.y).ToList();
                    bool merged = false;

                    for (int i = 0; i < dockSpaces.Count && !merged; i++)
                    {
                        dockSpaceData a = dockSpaces[i];
                        dockSpaceData b = null;
                        dockSpaceData combined = new dockSpaceData();

                        b = dockSpaces.FirstOrDefault(d =>
                            d.Rect.min.x == a.Rect.min.x + a.Rect.size.x &&
                            d.Rect.min.y == a.Rect.min.y &&
                            d.Rect.size.y == a.Rect.size.y);

                        if (b != null)
                        {
                            combined.Rect = new Rect(new Vector2(Mathf.Min(a.Rect.min.x, b.Rect.min.x), a.Rect.min.y), new Vector2(a.Rect.size.x + b.Rect.size.x, a.Rect.size.y));
                            combined.SplitSpaceRatio = a.Rect.size.x / combined.Rect.size.x;
                            combined.Dir = UIDockSpaceOrientation.Horizontal;
                            combined.Children = new List<dockSpaceData> { a, b };
                        }

                        if (b == null)
                        {
                            b = dockSpaces.FirstOrDefault(d =>
                                d.Rect.min.x == a.Rect.min.x &&
                                d.Rect.min.y == a.Rect.min.y + a.Rect.size.y &&
                                d.Rect.size.x == a.Rect.size.x);

                            if (b != null)
                            {
                                combined.Rect = new Rect(new Vector2(a.Rect.min.x, Mathf.Min(a.Rect.min.y, b.Rect.min.y)), new Vector2(a.Rect.size.x, a.Rect.size.y + b.Rect.size.y));
                                combined.SplitSpaceRatio = a.Rect.size.y / combined.Rect.size.y;
                                combined.Dir = UIDockSpaceOrientation.Vertical;
                                combined.Children = new List<dockSpaceData> { a, b };
                            }
                        }

                        if (b != null)
                        {
                            dockSpaces.Remove(a);
                            dockSpaces.Remove(b);
                            dockSpaces.Add(combined);
                            merged = true;
                        }
                    }

                    safety++;
                }

                if (dockSpaces.Count == 0)
                {
                    Debug.LogWarning("[Fugui] No dockspace found to generate layout.");
                    return null;
                }

                FuDockingLayoutDefinition rootLayout = new FuDockingLayoutDefinition("GeneratedLayout", 0u);
                convertDockSpaceDataToDockingLayoutDefinition(dockSpaces[0], rootLayout);

                return rootLayout.Children.Count > 0 ? rootLayout.Children[0] : rootLayout;
            }
            catch (Exception ex)
            {
                Debug.LogError("[Fugui] GenerateCurrentLayoutSync failed: " + ex);
                return null;
            }
        }

        /// <summary>
        /// Generate current layout and call an action
        /// </summary>
        /// <returns>current custom FuDockingLayoutDefinition OR null if failed</returns>
        public void GenerateCurrentLayout(Action<FuDockingLayoutDefinition> callback)
        {
            try
            {
                FuDockingLayoutDefinition layout = GenerateCurrentLayout();
                callback?.Invoke(layout);
            }
            catch (Exception ex)
            {
                Debug.LogError("[Fugui] GenerateCurrentLayout async wrapper failed: " + ex);
                callback?.Invoke(null);
            }
        }

        /// <summary>
        /// Convert a dockSpaceData to a DockingLayoutDefinition recursively all children
        /// </summary>
        /// <param name="dockSpaceData">source dockspacedata</param>
        /// <param name="parent">target FuDockingLayoutDefinition</param>
        private void convertDockSpaceDataToDockingLayoutDefinition(dockSpaceData dockSpaceData, FuDockingLayoutDefinition parent)
        {
            // create new DockingLayoutDefinition
            FuDockingLayoutDefinition newDockingLayoutDefinition = new FuDockingLayoutDefinition("DockSpace_" + dockSpaceData.Rect.ToString(), 0);
            newDockingLayoutDefinition.ID = Fugui.DefaultContainer.Dockspace_id;
            newDockingLayoutDefinition.Name = "DockSpace_" + dockSpaceData.Rect.ToString();
            newDockingLayoutDefinition.Orientation = dockSpaceData.Dir;
            newDockingLayoutDefinition.Proportion = dockSpaceData.SplitSpaceRatio;

            // add windows
            if (dockSpaceData.WindowNames != null)
            {
                foreach (FuWindowName windowName in dockSpaceData.WindowNames)
                {
                    newDockingLayoutDefinition.WindowsDefinition.Add(windowName.ID);
                }
            }
            else
            {
                newDockingLayoutDefinition.WindowsDefinition = new List<ushort>();
            }

            // add to parent
            parent.Children.Add(newDockingLayoutDefinition);

            // recursively convert children
            if (dockSpaceData.Children != null)
            {
                foreach (dockSpaceData child in dockSpaceData.Children)
                {
                    convertDockSpaceDataToDockingLayoutDefinition(child, newDockingLayoutDefinition);
                }
            }
        }
        #endregion

        #region Nested Types
        /// <summary>
        /// Represents the dock Space Data type.
        /// </summary>
        private class dockSpaceData
        {
            #region State
            public float SplitSpaceRatio;
            public UIDockSpaceOrientation Dir;
            public List<dockSpaceData> Children;
            public Rect Rect;
            public List<FuWindowName> WindowNames;
            #endregion
        }
        #endregion
    }
}
