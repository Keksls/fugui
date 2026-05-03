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
        /// <summary>
        /// Whether a custom docking drag, resize or tab drag is currently active.
        /// </summary>
        internal bool IsCustomDockManipulating
        {
            get
            {
                return _draggedFloatingRoot != null ||
                       _resizedFloatingRoot != null ||
                       _activeResizeNodeId != 0u ||
                       !string.IsNullOrEmpty(_pendingTabDragWindowId);
            }
        }

        private bool _hasPendingLayoutRequest;
        private bool _pendingLayoutRetryQueued;
        private FuDockingLayoutDefinition _pendingLayout;
        private bool _pendingGetOnlyAutoInstantiated;
        private readonly Dictionary<string, uint> _windowDockNodeIds = new Dictionary<string, uint>();
        private readonly Dictionary<uint, List<string>> _nodeWindowIds = new Dictionary<uint, List<string>>();
        private readonly Dictionary<uint, int> _nodeSelectedIndices = new Dictionary<uint, int>();
        private readonly Dictionary<uint, Rect> _nodeRects = new Dictionary<uint, Rect>();
        private readonly Dictionary<uint, FuDockingLayoutDefinition> _nodesById = new Dictionary<uint, FuDockingLayoutDefinition>();
        private readonly Dictionary<uint, FuDockingLayoutDefinition> _nodeParents = new Dictionary<uint, FuDockingLayoutDefinition>();
        private readonly List<FloatingDockRoot> _floatingDockRoots = new List<FloatingDockRoot>();
        private uint _nextRuntimeDockNodeId = 1u;
        private string _pendingTabDragWindowId;
        private uint _pendingTabDragNodeId;
        private Vector2Int _pendingTabDragStartMousePos;
        private Vector2Int _pendingTabDragStartWindowPos;
        private DockDropTarget _dockDragPreviewTarget;
        private uint _activeResizeNodeId;
        private FloatingDockRoot _draggedFloatingRoot;
        private Vector2Int _floatingRootDragStartMousePos;
        private Rect _floatingRootDragStartRect;
        private FloatingDockRoot _resizedFloatingRoot;
        private Vector2Int _floatingRootResizeStartMousePos;
        private Rect _floatingRootResizeStartRect;
        private FloatingDockRootResizeEdge _floatingRootResizeEdge = FloatingDockRootResizeEdge.None;

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
            if (window == null || CurrentLayout == null)
            {
                return false;
            }

            EnsureCustomLayoutPrepared(CurrentLayout);
            FuDockingLayoutDefinition targetNode = FindLeafForWindow(CurrentLayout, window.WindowName.ID);
            if (targetNode == null)
            {
                return false;
            }

            RegisterWindowToDockNode(window, targetNode);
            Fugui.ForceDrawAllWindows(2);
            return true;
        }

        /// <summary>
        /// recursively iterrate over each dockspaces in a dockingLayoutDefinition and dock the window in the right dockSpace
        /// </summary>
        /// <param name="window">window to dock</param>
        /// <param name="dockSpaceDefinition">dockspaceDefinition to iterate on</param>
        /// <param name="success">whatever the window has been docked</param>
        private void tryAutoDockWindow(FuWindow window, FuDockingLayoutDefinition dockSpaceDefinition, ref bool success)
        {
            FuDockingLayoutDefinition targetNode = FindLeafForWindow(dockSpaceDefinition, window != null ? window.WindowName.ID : (ushort)0);
            if (targetNode == null)
                return;

            RegisterWindowToDockNode(window, targetNode);
            success = true;
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
                    PrepareCustomLayout(dockSpaceDefinition);
                    createDocking(windows, dockSpaceDefinition);
                    selectFirstTabOnEachDockSpaces(dockSpaceDefinition);
                    CurrentLayout = dockSpaceDefinition;
                    Fugui.ForceDrawAllWindows(2);
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
                        FuWindow window = Fugui.UIWindows.TryGetValue(id, out FuWindow registeredWindow) ? registeredWindow : null;
                        if (window != null)
                        {
                            RegisterWindowToDockNode(window, layout);
                        }
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
            if (layout.WindowsDefinition.Count > 0 && _nodeWindowIds.TryGetValue(layout.ID, out List<string> nodeWindows) && nodeWindows.Count > 0)
            {
                _nodeSelectedIndices[layout.ID] = 0;
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
            ClearCustomDocking();
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
        /// Prepare runtime ids and lookup tables for the custom docking layout.
        /// </summary>
        private void PrepareCustomLayout(FuDockingLayoutDefinition layout)
        {
            ClearCustomDocking();
            NormalizeRuntimeNodeIds(layout);
            CurrentLayout = layout;
            RebuildNodeIndex();
        }

        /// <summary>
        /// Ensure a loaded layout has runtime ids and lookup data before direct AutoDock calls.
        /// </summary>
        private void EnsureCustomLayoutPrepared(FuDockingLayoutDefinition layout)
        {
            if (layout == null)
                return;

            if (_nodesById.Count > 0 && _nodesById.ContainsKey(layout.ID))
                return;

            _nodesById.Clear();
            _nodeParents.Clear();
            _nodeSelectedIndices.Clear();
            NormalizeRuntimeNodeIds(layout);
            RebuildNodeIndex();
        }

        /// <summary>
        /// Clear runtime custom docking data and mark currently docked windows as floating.
        /// </summary>
        private void ClearCustomDocking()
        {
            foreach (string windowId in _windowDockNodeIds.Keys.ToList())
            {
                if (Fugui.UIWindows != null && Fugui.UIWindows.TryGetValue(windowId, out FuWindow window))
                {
                    window.IsDocked = false;
                    window.CurrentDockID = 0u;
                    window.Fire_OnUnDock();
                }
            }

            _windowDockNodeIds.Clear();
            _nodeWindowIds.Clear();
            _nodeSelectedIndices.Clear();
            _nodeRects.Clear();
            _nodesById.Clear();
            _nodeParents.Clear();
            _floatingDockRoots.Clear();
            _activeResizeNodeId = 0u;
            ClearDockDragState();
            ClearFloatingRootDragState();
            ClearFloatingRootResizeState();
        }

        /// <summary>
        /// Assign unique ids to layout nodes that have no valid runtime id.
        /// </summary>
        private void NormalizeRuntimeNodeIds(FuDockingLayoutDefinition layout)
        {
            HashSet<uint> usedIds = new HashSet<uint>();
            _nextRuntimeDockNodeId = 1u;
            NormalizeRuntimeNodeIdsRecursive(layout, usedIds);
        }

        /// <summary>
        /// Recursively assign unique runtime ids.
        /// </summary>
        private void NormalizeRuntimeNodeIdsRecursive(FuDockingLayoutDefinition node, HashSet<uint> usedIds)
        {
            if (node == null)
                return;

            if (node.Children == null)
                node.Children = new List<FuDockingLayoutDefinition>();
            if (node.WindowsDefinition == null)
                node.WindowsDefinition = new List<ushort>();

            while (node.ID == 0u || usedIds.Contains(node.ID))
            {
                node.ID = _nextRuntimeDockNodeId++;
            }

            usedIds.Add(node.ID);
            if (node.ID >= _nextRuntimeDockNodeId)
            {
                _nextRuntimeDockNodeId = node.ID + 1u;
            }

            foreach (FuDockingLayoutDefinition child in node.Children)
            {
                NormalizeRuntimeNodeIdsRecursive(child, usedIds);
            }
        }

        /// <summary>
        /// Rebuild node lookup and parent tables for the main and floating layouts.
        /// </summary>
        private void RebuildNodeIndex()
        {
            _nodesById.Clear();
            _nodeParents.Clear();
            if (CurrentLayout != null)
            {
                IndexNodes(CurrentLayout, null);
            }

            foreach (FloatingDockRoot floatingRoot in _floatingDockRoots)
            {
                IndexNodes(floatingRoot.Layout, null);
            }

            if (_activeResizeNodeId != 0u && !_nodesById.ContainsKey(_activeResizeNodeId))
            {
                _activeResizeNodeId = 0u;
            }
        }

        /// <summary>
        /// Build node lookup and parent tables.
        /// </summary>
        private void IndexNodes(FuDockingLayoutDefinition node, FuDockingLayoutDefinition parent)
        {
            if (node == null)
                return;

            _nodesById[node.ID] = node;
            if (parent != null)
            {
                _nodeParents[node.ID] = parent;
            }
            if (!_nodeSelectedIndices.ContainsKey(node.ID))
            {
                _nodeSelectedIndices[node.ID] = 0;
            }

            if (node.Children == null)
                return;

            foreach (FuDockingLayoutDefinition child in node.Children)
            {
                IndexNodes(child, node);
            }
        }

        /// <summary>
        /// Find the leaf node that accepts a window definition id.
        /// </summary>
        private FuDockingLayoutDefinition FindLeafForWindow(FuDockingLayoutDefinition node, ushort windowId)
        {
            if (node == null)
                return null;

            if (node.WindowsDefinition != null && node.WindowsDefinition.Contains(windowId))
            {
                return node;
            }

            if (node.Children == null)
                return null;

            foreach (FuDockingLayoutDefinition child in node.Children)
            {
                FuDockingLayoutDefinition result = FindLeafForWindow(child, windowId);
                if (result != null)
                    return result;
            }

            return null;
        }

        /// <summary>
        /// Allocate a runtime dock node id that is unique in the current custom docking graph.
        /// </summary>
        private uint AllocateDockNodeId()
        {
            while (_nextRuntimeDockNodeId == 0u || _nodesById.ContainsKey(_nextRuntimeDockNodeId))
            {
                _nextRuntimeDockNodeId++;
            }

            return _nextRuntimeDockNodeId++;
        }

        /// <summary>
        /// Create a runtime dock node with initialized collections.
        /// </summary>
        private FuDockingLayoutDefinition CreateRuntimeDockNode(string name)
        {
            FuDockingLayoutDefinition node = new FuDockingLayoutDefinition(name, AllocateDockNodeId())
            {
                AutoHideTopBar = CurrentLayout != null && CurrentLayout.AutoHideTopBar
            };
            _nodesById[node.ID] = node;
            _nodeSelectedIndices[node.ID] = 0;
            return node;
        }

        /// <summary>
        /// Register a window into a custom dock node.
        /// </summary>
        private void RegisterWindowToDockNode(FuWindow window, FuDockingLayoutDefinition node)
        {
            if (window == null || node == null)
                return;

            if (_windowDockNodeIds.TryGetValue(window.ID, out uint currentNodeId) && currentNodeId != node.ID)
            {
                RemoveWindowFromDockNode(window, false);
            }

            _nodesById[node.ID] = node;
            if (!_nodeSelectedIndices.ContainsKey(node.ID))
            {
                _nodeSelectedIndices[node.ID] = 0;
            }

            if (!_nodeWindowIds.TryGetValue(node.ID, out List<string> nodeWindows))
            {
                nodeWindows = new List<string>();
                _nodeWindowIds[node.ID] = nodeWindows;
            }

            if (!nodeWindows.Contains(window.ID))
            {
                nodeWindows.Add(window.ID);
            }

            _windowDockNodeIds[window.ID] = node.ID;
            bool wasDocked = window.IsDocked;
            window.CurrentDockID = node.ID;
            window.IsDocked = true;
            _nodeSelectedIndices[node.ID] = Mathf.Max(0, nodeWindows.IndexOf(window.ID));
            ApplyDockNodeRectToWindow(window, node.ID, wasDocked);
            if (!wasDocked)
            {
                window.Fire_OnDock();
            }
            window.ForceDraw(2);
        }

        /// <summary>
        /// Remove a window from its custom dock node.
        /// </summary>
        private bool RemoveWindowFromDockNode(FuWindow window, bool fireUndock, bool pruneEmpty = true)
        {
            if (window == null || !_windowDockNodeIds.TryGetValue(window.ID, out uint nodeId))
            {
                return false;
            }

            _windowDockNodeIds.Remove(window.ID);
            if (_nodeWindowIds.TryGetValue(nodeId, out List<string> nodeWindows))
            {
                int removedIndex = nodeWindows.IndexOf(window.ID);
                if (removedIndex >= 0)
                {
                    nodeWindows.RemoveAt(removedIndex);
                }

                if (nodeWindows.Count == 0)
                {
                    _nodeWindowIds.Remove(nodeId);
                    _nodeSelectedIndices.Remove(nodeId);
                }
                else
                {
                    int selected = _nodeSelectedIndices.TryGetValue(nodeId, out int selectedIndex) ? selectedIndex : 0;
                    if (removedIndex >= 0 && selected > removedIndex)
                    {
                        selected--;
                    }
                    _nodeSelectedIndices[nodeId] = Mathf.Clamp(selected, 0, nodeWindows.Count - 1);
                }
            }

            window.CurrentDockID = 0u;
            if (window.IsDocked)
            {
                window.IsDocked = false;
                if (fireUndock)
                {
                    window.Fire_OnUnDock();
                }
            }

            window.ForceDraw(2);
            if (pruneEmpty)
            {
                PruneEmptyDockRoots();
            }
            return true;
        }

        /// <summary>
        /// Apply the current node rect to a docked window immediately when available.
        /// </summary>
        private void ApplyDockNodeRectToWindow(FuWindow window, uint nodeId, bool notifyResize = true)
        {
            if (window == null || !_nodeRects.TryGetValue(nodeId, out Rect rect))
            {
                return;
            }

            Vector2Int pos = new Vector2Int(Mathf.RoundToInt(rect.x), Mathf.RoundToInt(rect.y));
            Vector2Int size = new Vector2Int(Mathf.Max(1, Mathf.RoundToInt(rect.width)), Mathf.Max(1, Mathf.RoundToInt(rect.height)));
            window.ApplyProgrammaticRect(pos, size, notifyResize);
        }

        /// <summary>
        /// Update manual dock node rectangles and apply them to their windows.
        /// </summary>
        internal void UpdateCustomLayout(FuMainWindowContainer container, Rect contentRect)
        {
            if (container == null)
            {
                return;
            }

            if (CurrentLayout != null)
            {
                EnsureCustomLayoutPrepared(CurrentLayout);
            }
            CleanupClosedWindows();
            _nodeRects.Clear();
            if (CurrentLayout != null)
            {
                UpdateCustomLayoutRecursive(CurrentLayout, contentRect, container);
            }
            foreach (FloatingDockRoot floatingRoot in _floatingDockRoots.ToList())
            {
                if (!_floatingDockRoots.Contains(floatingRoot))
                {
                    continue;
                }

                ProcessFloatingDockRoot(floatingRoot, container);
                if (!_floatingDockRoots.Contains(floatingRoot))
                {
                    continue;
                }

                UpdateCustomLayoutRecursive(floatingRoot.Layout, GetFloatingDockRootContentRect(floatingRoot), container);
            }
        }

        /// <summary>
        /// Recursively compute and apply custom dock rects.
        /// </summary>
        private void UpdateCustomLayoutRecursive(FuDockingLayoutDefinition node, Rect rect, FuMainWindowContainer container = null)
        {
            if (node == null)
                return;

            if (node.Children != null && node.Children.Count >= 2 && node.Orientation != UIDockSpaceOrientation.None)
            {
                ProcessDockSplitter(node, rect, container);
                float proportion = Mathf.Clamp(node.Proportion, 0.05f, 0.95f);
                if (node.Orientation == UIDockSpaceOrientation.Horizontal)
                {
                    float firstWidth = Mathf.Round(rect.width * proportion);
                    Rect first = new Rect(rect.x, rect.y, firstWidth, rect.height);
                    Rect second = new Rect(rect.x + firstWidth, rect.y, Mathf.Max(1f, rect.width - firstWidth), rect.height);
                    UpdateCustomLayoutRecursive(node.Children[0], first, container);
                    UpdateCustomLayoutRecursive(node.Children[1], second, container);
                }
                else
                {
                    float firstHeight = Mathf.Round(rect.height * proportion);
                    Rect first = new Rect(rect.x, rect.y, rect.width, firstHeight);
                    Rect second = new Rect(rect.x, rect.y + firstHeight, rect.width, Mathf.Max(1f, rect.height - firstHeight));
                    UpdateCustomLayoutRecursive(node.Children[0], first, container);
                    UpdateCustomLayoutRecursive(node.Children[1], second, container);
                }

                return;
            }

            _nodeRects[node.ID] = rect;
            if (!_nodeWindowIds.TryGetValue(node.ID, out List<string> windowIds))
            {
                return;
            }

            for (int i = 0; i < windowIds.Count; i++)
            {
                if (!Fugui.UIWindows.TryGetValue(windowIds[i], out FuWindow window))
                {
                    continue;
                }

                Vector2Int pos = new Vector2Int(Mathf.RoundToInt(rect.x), Mathf.RoundToInt(rect.y));
                Vector2Int size = new Vector2Int(Mathf.Max(1, Mathf.RoundToInt(rect.width)), Mathf.Max(1, Mathf.RoundToInt(rect.height)));
                window.CurrentDockID = node.ID;
                window.IsDocked = true;
                window.ApplyProgrammaticRect(pos, size, true);
            }
        }

        /// <summary>
        /// Draw and process the splitter between two dock children.
        /// </summary>
        private void ProcessDockSplitter(FuDockingLayoutDefinition node, Rect rect, FuMainWindowContainer container)
        {
            if (node == null || container == null || rect.width <= 1f || rect.height <= 1f)
            {
                return;
            }
            if (_draggedFloatingRoot != null || _resizedFloatingRoot != null || (_activeResizeNodeId == 0u && Fugui.IsDraggingAnything()))
            {
                return;
            }

            float proportion = Mathf.Clamp(node.Proportion, 0.05f, 0.95f);
            float scale = container.Context != null ? container.Context.Scale : Fugui.Scale;
            float grabThickness = Mathf.Max(7f, 8f * scale);
            float visualThickness = Mathf.Max(1.5f, 2f * scale);
            bool horizontal = node.Orientation == UIDockSpaceOrientation.Horizontal;
            float split = horizontal ? rect.x + rect.width * proportion : rect.y + rect.height * proportion;
            Rect grabRect = horizontal
                ? new Rect(split - grabThickness * 0.5f, rect.y, grabThickness, rect.height)
                : new Rect(rect.x, split - grabThickness * 0.5f, rect.width, grabThickness);

            Vector2 mousePos = container.LocalMousePos;
            bool active = _activeResizeNodeId == node.ID;
            bool hovered = _activeResizeNodeId == 0u && grabRect.Contains(mousePos);

            if (hovered && container.Mouse.IsDown(FuMouseButton.Left))
            {
                _activeResizeNodeId = node.ID;
                active = true;
            }

            if (active)
            {
                if (container.Mouse.IsPressed(FuMouseButton.Left))
                {
                    float raw = horizontal ? (mousePos.x - rect.x) / rect.width : (mousePos.y - rect.y) / rect.height;
                    node.Proportion = Mathf.Clamp(raw, 0.05f, 0.95f);
                    Fugui.ForceDrawAllWindows(2);
                }
                else
                {
                    _activeResizeNodeId = 0u;
                    active = false;
                }
            }

            if (hovered || active)
            {
                ImGui.SetMouseCursor(horizontal ? ImGuiMouseCursor.ResizeEW : ImGuiMouseCursor.ResizeNS);
            }

            DrawDockSplitter(grabRect, horizontal, hovered, active, visualThickness, scale);
        }

        /// <summary>
        /// Draw the dock splitter feedback.
        /// </summary>
        private void DrawDockSplitter(Rect grabRect, bool horizontal, bool hovered, bool active, float visualThickness, float scale)
        {
            ImDrawListPtr drawList = ImGui.GetForegroundDrawList();
            uint lineColor = Fugui.Themes.GetColorU32(active ? FuColors.SeparatorActive : hovered ? FuColors.SeparatorHovered : FuColors.Separator, hovered || active ? 0.95f : 0.45f);
            uint handleColor = Fugui.Themes.GetColorU32(active ? FuColors.DockingPreview : FuColors.Border, hovered || active ? 0.95f : 0.35f);
            float centerX = grabRect.x + grabRect.width * 0.5f;
            float centerY = grabRect.y + grabRect.height * 0.5f;

            if (horizontal)
            {
                drawList.AddLine(new Vector2(centerX, grabRect.y), new Vector2(centerX, grabRect.yMax), lineColor, visualThickness);
                if (hovered || active)
                {
                    Vector2 handleSize = new Vector2(Mathf.Max(5f, 5f * scale), Mathf.Max(36f, 42f * scale));
                    Rect handle = new Rect(new Vector2(centerX - handleSize.x * 0.5f, centerY - handleSize.y * 0.5f), handleSize);
                    drawList.AddRectFilled(handle.position, handle.position + handle.size, handleColor, handleSize.x * 0.5f);
                }
            }
            else
            {
                drawList.AddLine(new Vector2(grabRect.x, centerY), new Vector2(grabRect.xMax, centerY), lineColor, visualThickness);
                if (hovered || active)
                {
                    Vector2 handleSize = new Vector2(Mathf.Max(36f, 42f * scale), Mathf.Max(5f, 5f * scale));
                    Rect handle = new Rect(new Vector2(centerX - handleSize.x * 0.5f, centerY - handleSize.y * 0.5f), handleSize);
                    drawList.AddRectFilled(handle.position, handle.position + handle.size, handleColor, handleSize.y * 0.5f);
                }
            }
        }

        /// <summary>
        /// Draw and process the movable shell around a floating dock tree.
        /// </summary>
        private void ProcessFloatingDockRoot(FloatingDockRoot floatingRoot, FuMainWindowContainer container)
        {
            if (floatingRoot == null || floatingRoot.Layout == null || container == null)
            {
                return;
            }

            Vector2Int mousePos = container.LocalMousePos;
            bool active = _draggedFloatingRoot == floatingRoot;
            bool resizing = _resizedFloatingRoot == floatingRoot;
            if (!active)
            {
                floatingRoot.HeaderHovered = false;
            }
            FloatingDockRootResizeEdge hoveredResizeEdge = _draggedFloatingRoot == null &&
                                                           _resizedFloatingRoot == null &&
                                                           _activeResizeNodeId == 0u &&
                                                           !Fugui.IsDraggingAnything()
                ? GetHoveredFloatingDockRootResizeEdge(floatingRoot.Rect, mousePos)
                : FloatingDockRootResizeEdge.None;
            floatingRoot.HeaderActive = active;
            floatingRoot.ResizeEdge = resizing ? _floatingRootResizeEdge : hoveredResizeEdge;
            floatingRoot.ResizeActive = resizing;

            if (hoveredResizeEdge != FloatingDockRootResizeEdge.None && container.Mouse.IsDown(FuMouseButton.Left))
            {
                _resizedFloatingRoot = floatingRoot;
                _floatingRootResizeStartMousePos = mousePos;
                _floatingRootResizeStartRect = floatingRoot.Rect;
                _floatingRootResizeEdge = hoveredResizeEdge;
                MoveFloatingDockRootToFront(floatingRoot, container);
                resizing = true;
                floatingRoot.ResizeActive = true;
                floatingRoot.ResizeEdge = hoveredResizeEdge;
            }

            if (resizing)
            {
                if (container.Mouse.IsPressed(FuMouseButton.Left))
                {
                    ApplyFloatingDockRootResize(floatingRoot, mousePos);
                    Fugui.ForceDrawAllWindows(2);
                }
                else
                {
                    floatingRoot.Rect = FitFloatingDockRootInsideContainer(floatingRoot.Rect, container);
                    ClearFloatingRootResizeState();
                    return;
                }
            }

            if (active)
            {
                if (container.Mouse.IsPressed(FuMouseButton.Left))
                {
                    Vector2Int delta = mousePos - _floatingRootDragStartMousePos;
                    floatingRoot.Rect = new Rect(
                        _floatingRootDragStartRect.x + delta.x,
                        _floatingRootDragStartRect.y + delta.y,
                        _floatingRootDragStartRect.width,
                        _floatingRootDragStartRect.height);
                    _dockDragPreviewTarget = ResolveDockDropTarget(mousePos, floatingRoot, true, container);
                    DrawDockDragPreview();
                    Fugui.ForceDrawAllWindows(2);
                }
                else
                {
                    DockDropTarget target = ResolveDockDropTarget(mousePos, floatingRoot, false, container);
                    if (target.IsValid && DockFloatingRootToTarget(floatingRoot, target, container))
                    {
                        Fugui.ForceDrawAllWindows(2);
                    }
                    else if (_floatingDockRoots.Contains(floatingRoot))
                    {
                        floatingRoot.Rect = FitFloatingDockRootInsideContainer(floatingRoot.Rect, container);
                    }

                    floatingRoot.HeaderHovered = false;
                    floatingRoot.HeaderActive = false;
                    ClearFloatingRootDragState();
                    return;
                }
            }

            if (active)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
            }
            else if (hoveredResizeEdge != FloatingDockRootResizeEdge.None || resizing)
            {
                SetFloatingDockRootResizeCursor(floatingRoot.ResizeEdge);
            }
        }

        /// <summary>
        /// Apply the active resize operation to a floating dock root.
        /// </summary>
        private void ApplyFloatingDockRootResize(FloatingDockRoot floatingRoot, Vector2Int mousePos)
        {
            if (floatingRoot == null)
            {
                return;
            }

            Vector2 delta = mousePos - _floatingRootResizeStartMousePos;
            Rect rect = _floatingRootResizeStartRect;
            float scale = Fugui.CurrentContext != null ? Fugui.CurrentContext.Scale : Fugui.Scale;
            float minWidth = Mathf.Max(128f, 128f * scale);
            float minHeight = Mathf.Max(GetFloatingDockRootHeaderHeight() + 64f, GetFloatingDockRootHeaderHeight() + 64f * scale);

            switch (_floatingRootResizeEdge)
            {
                case FloatingDockRootResizeEdge.Left:
                    ApplyFloatingRootLeftResize(delta.x, minWidth, ref rect);
                    break;
                case FloatingDockRootResizeEdge.Right:
                    rect.width = Mathf.Max(minWidth, _floatingRootResizeStartRect.width + delta.x);
                    break;
                case FloatingDockRootResizeEdge.Top:
                    ApplyFloatingRootTopResize(delta.y, minHeight, ref rect);
                    break;
                case FloatingDockRootResizeEdge.Bottom:
                    rect.height = Mathf.Max(minHeight, _floatingRootResizeStartRect.height + delta.y);
                    break;
                case FloatingDockRootResizeEdge.TopLeft:
                    ApplyFloatingRootLeftResize(delta.x, minWidth, ref rect);
                    ApplyFloatingRootTopResize(delta.y, minHeight, ref rect);
                    break;
                case FloatingDockRootResizeEdge.TopRight:
                    rect.width = Mathf.Max(minWidth, _floatingRootResizeStartRect.width + delta.x);
                    ApplyFloatingRootTopResize(delta.y, minHeight, ref rect);
                    break;
                case FloatingDockRootResizeEdge.BottomLeft:
                    ApplyFloatingRootLeftResize(delta.x, minWidth, ref rect);
                    rect.height = Mathf.Max(minHeight, _floatingRootResizeStartRect.height + delta.y);
                    break;
                case FloatingDockRootResizeEdge.BottomRight:
                    rect.width = Mathf.Max(minWidth, _floatingRootResizeStartRect.width + delta.x);
                    rect.height = Mathf.Max(minHeight, _floatingRootResizeStartRect.height + delta.y);
                    break;
            }

            floatingRoot.Rect = rect;
        }

        /// <summary>
        /// Apply left-edge floating dock root resize while preserving the minimum width.
        /// </summary>
        private void ApplyFloatingRootLeftResize(float deltaX, float minWidth, ref Rect rect)
        {
            float maxDelta = _floatingRootResizeStartRect.width - minWidth;
            float clampedDelta = Mathf.Min(deltaX, maxDelta);
            rect.x = _floatingRootResizeStartRect.x + clampedDelta;
            rect.width = _floatingRootResizeStartRect.width - clampedDelta;
        }

        /// <summary>
        /// Apply top-edge floating dock root resize while preserving the minimum height.
        /// </summary>
        private void ApplyFloatingRootTopResize(float deltaY, float minHeight, ref Rect rect)
        {
            float maxDelta = _floatingRootResizeStartRect.height - minHeight;
            float clampedDelta = Mathf.Min(deltaY, maxDelta);
            rect.y = _floatingRootResizeStartRect.y + clampedDelta;
            rect.height = _floatingRootResizeStartRect.height - clampedDelta;
        }

        /// <summary>
        /// Return the hovered resize edge for a floating dock root.
        /// </summary>
        private FloatingDockRootResizeEdge GetHoveredFloatingDockRootResizeEdge(Rect rect, Vector2 mousePos)
        {
            float scale = Fugui.CurrentContext != null ? Fugui.CurrentContext.Scale : Fugui.Scale;
            float border = Mathf.Max(4f, 6f * scale);
            float corner = Mathf.Max(10f, 14f * scale);
            bool inVerticalRange = mousePos.y >= rect.yMin && mousePos.y <= rect.yMax;
            bool inHorizontalRange = mousePos.x >= rect.xMin && mousePos.x <= rect.xMax;
            bool left = inVerticalRange && mousePos.x >= rect.xMin && mousePos.x <= rect.xMin + border;
            bool right = inVerticalRange && mousePos.x <= rect.xMax && mousePos.x >= rect.xMax - border;
            bool top = inHorizontalRange && mousePos.y >= rect.yMin && mousePos.y <= rect.yMin + border;
            bool bottom = inHorizontalRange && mousePos.y <= rect.yMax && mousePos.y >= rect.yMax - border;
            bool topCorner = inHorizontalRange && mousePos.y >= rect.yMin && mousePos.y <= rect.yMin + corner;
            bool bottomCorner = inHorizontalRange && mousePos.y <= rect.yMax && mousePos.y >= rect.yMax - corner;
            bool leftCorner = inVerticalRange && mousePos.x >= rect.xMin && mousePos.x <= rect.xMin + corner;
            bool rightCorner = inVerticalRange && mousePos.x <= rect.xMax && mousePos.x >= rect.xMax - corner;

            if (topCorner && leftCorner)
            {
                return FloatingDockRootResizeEdge.TopLeft;
            }
            if (topCorner && rightCorner)
            {
                return FloatingDockRootResizeEdge.TopRight;
            }
            if (bottomCorner && leftCorner)
            {
                return FloatingDockRootResizeEdge.BottomLeft;
            }
            if (bottomCorner && rightCorner)
            {
                return FloatingDockRootResizeEdge.BottomRight;
            }
            if (left)
            {
                return FloatingDockRootResizeEdge.Left;
            }
            if (right)
            {
                return FloatingDockRootResizeEdge.Right;
            }
            if (top)
            {
                return FloatingDockRootResizeEdge.Top;
            }
            if (bottom)
            {
                return FloatingDockRootResizeEdge.Bottom;
            }

            return FloatingDockRootResizeEdge.None;
        }

        /// <summary>
        /// Set the mouse cursor for a floating dock root resize edge.
        /// </summary>
        private void SetFloatingDockRootResizeCursor(FloatingDockRootResizeEdge edge)
        {
            switch (edge)
            {
                case FloatingDockRootResizeEdge.Left:
                case FloatingDockRootResizeEdge.Right:
                    ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEW);
                    break;
                case FloatingDockRootResizeEdge.Top:
                case FloatingDockRootResizeEdge.Bottom:
                    ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNS);
                    break;
                case FloatingDockRootResizeEdge.TopRight:
                case FloatingDockRootResizeEdge.BottomLeft:
                    ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNESW);
                    break;
                case FloatingDockRootResizeEdge.TopLeft:
                case FloatingDockRootResizeEdge.BottomRight:
                    ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNWSE);
                    break;
            }
        }

        /// <summary>
        /// Find the floating root that contains a node.
        /// </summary>
        private FloatingDockRoot FindFloatingDockRootForNode(uint nodeId)
        {
            for (int i = _floatingDockRoots.Count - 1; i >= 0; i--)
            {
                FloatingDockRoot floatingRoot = _floatingDockRoots[i];
                if (floatingRoot != null && NodeTreeContainsNode(floatingRoot.Layout, nodeId))
                {
                    return floatingRoot;
                }
            }

            return null;
        }

        /// <summary>
        /// Move a floating dock root and its windows above the other docked surfaces.
        /// </summary>
        private void MoveFloatingDockRootToFront(FloatingDockRoot floatingRoot, FuMainWindowContainer container)
        {
            if (floatingRoot == null)
            {
                return;
            }

            _floatingDockRoots.Remove(floatingRoot);
            _floatingDockRoots.Add(floatingRoot);
            container?.BringWindowsToFront(GetRuntimeWindowIdsInNodeTree(floatingRoot.Layout));
        }

        /// <summary>
        /// Return the content rect available to the actual dock tree inside a floating dock root.
        /// </summary>
        private Rect GetFloatingDockRootContentRect(FloatingDockRoot floatingRoot)
        {
            return floatingRoot.Rect;
        }

        /// <summary>
        /// Return the height of a floating dock root title strip.
        /// </summary>
        private float GetFloatingDockRootHeaderHeight()
        {
            float scale = Fugui.CurrentContext != null ? Fugui.CurrentContext.Scale : Fugui.Scale;
            float textHeight = ImGui.CalcTextSize("Ap").y;
            return Mathf.Max(24f * scale, textHeight + 10f * scale);
        }

        /// <summary>
        /// Build a floating root outer rect from a content rect, preserving content size when possible.
        /// </summary>
        private Rect BuildFloatingDockRootRectFromContent(Rect contentRect, IFuWindowContainer container)
        {
            Rect rect = contentRect;
            return container is FuMainWindowContainer mainContainer
                ? FitFloatingDockRootInsideContainer(rect, mainContainer)
                : rect;
        }

        /// <summary>
        /// Clamp a floating dock root so it stays fully visible in the main container.
        /// </summary>
        private Rect FitFloatingDockRootInsideContainer(Rect rect, FuMainWindowContainer container)
        {
            if (container == null)
            {
                return rect;
            }

            float maxWidth = Mathf.Max(1f, container.Size.x);
            float maxHeight = Mathf.Max(1f, container.Size.y);
            rect.width = Mathf.Clamp(rect.width, 1f, maxWidth);
            rect.height = Mathf.Clamp(rect.height, 1f, maxHeight);
            rect.x = Mathf.Clamp(rect.x, 0f, Mathf.Max(0f, maxWidth - rect.width));
            rect.y = Mathf.Clamp(rect.y, 0f, Mathf.Max(0f, maxHeight - rect.height));
            return rect;
        }

        /// <summary>
        /// Remove closed windows from custom dock tabs.
        /// </summary>
        private void CleanupClosedWindows()
        {
            bool needsPrune = false;
            foreach (KeyValuePair<uint, List<string>> pair in _nodeWindowIds.ToList())
            {
                List<string> windows = pair.Value;
                for (int i = windows.Count - 1; i >= 0; i--)
                {
                    string windowId = windows[i];
                    if (Fugui.UIWindows != null && Fugui.UIWindows.ContainsKey(windowId))
                    {
                        continue;
                    }

                    windows.RemoveAt(i);
                    _windowDockNodeIds.Remove(windowId);
                    needsPrune = true;
                }

                if (windows.Count == 0)
                {
                    _nodeWindowIds.Remove(pair.Key);
                    _nodeSelectedIndices.Remove(pair.Key);
                    needsPrune = true;
                    continue;
                }

                int selected = _nodeSelectedIndices.TryGetValue(pair.Key, out int selectedIndex) ? selectedIndex : 0;
                _nodeSelectedIndices[pair.Key] = Mathf.Clamp(selected, 0, windows.Count - 1);
            }

            if (needsPrune)
            {
                PruneEmptyDockRoots();
            }
        }

        /// <summary>
        /// Remove empty leaves and collapse split nodes that only have one useful child left.
        /// </summary>
        private void PruneEmptyDockRoots()
        {
            if (CurrentLayout != null)
            {
                PruneNode(CurrentLayout);
            }

            for (int i = _floatingDockRoots.Count - 1; i >= 0; i--)
            {
                FloatingDockRoot floatingRoot = _floatingDockRoots[i];
                if (!PruneNode(floatingRoot.Layout))
                {
                    UnregisterNodeTree(floatingRoot.Layout);
                    _floatingDockRoots.RemoveAt(i);
                    if (_draggedFloatingRoot == floatingRoot)
                    {
                        ClearFloatingRootDragState();
                    }
                    if (_resizedFloatingRoot == floatingRoot)
                    {
                        ClearFloatingRootResizeState();
                    }
                    continue;
                }

                List<string> windowIds = GetRuntimeWindowIdsInNodeTree(floatingRoot.Layout);
                if (windowIds.Count <= 1)
                {
                    BreakFloatingDockRoot(floatingRoot, windowIds.Count == 1 ? windowIds[0] : null);
                }
            }

            RebuildNodeIndex();
            Fugui.ForceDrawAllWindows(2);
        }

        /// <summary>
        /// Destroy a floating dock root and keep its last window as a regular floating window.
        /// </summary>
        private void BreakFloatingDockRoot(FloatingDockRoot floatingRoot, string remainingWindowId)
        {
            if (floatingRoot == null)
            {
                return;
            }

            Rect rect = floatingRoot.Rect;
            _floatingDockRoots.Remove(floatingRoot);
            UnregisterNodeTree(floatingRoot.Layout);
            if (_draggedFloatingRoot == floatingRoot)
            {
                ClearFloatingRootDragState();
            }
            if (_resizedFloatingRoot == floatingRoot)
            {
                ClearFloatingRootResizeState();
            }

            if (string.IsNullOrEmpty(remainingWindowId) ||
                Fugui.UIWindows == null ||
                !Fugui.UIWindows.TryGetValue(remainingWindowId, out FuWindow window))
            {
                return;
            }

            Vector2Int position = new Vector2Int(Mathf.RoundToInt(rect.x), Mathf.RoundToInt(rect.y));
            Vector2Int size = new Vector2Int(Mathf.Max(1, Mathf.RoundToInt(rect.width)), Mathf.Max(1, Mathf.RoundToInt(rect.height)));
            window.CurrentDockID = 0u;
            if (window.IsDocked)
            {
                window.IsDocked = false;
                window.ApplyProgrammaticRect(position, size, false);
                window.Fire_OnUnDock();
            }
            else
            {
                window.ApplyProgrammaticRect(position, size, true);
            }

            window.ForceDraw(2);
        }

        /// <summary>
        /// Prune one node recursively.
        /// </summary>
        private bool PruneNode(FuDockingLayoutDefinition node)
        {
            if (node == null)
            {
                return false;
            }

            if (node.Children == null)
            {
                node.Children = new List<FuDockingLayoutDefinition>();
            }

            for (int i = node.Children.Count - 1; i >= 0; i--)
            {
                FuDockingLayoutDefinition child = node.Children[i];
                if (PruneNode(child))
                {
                    continue;
                }

                UnregisterNodeTree(child);
                node.Children.RemoveAt(i);
            }

            if (node.Children.Count == 1)
            {
                CollapseNodeInto(node, node.Children[0]);
            }

            if (node.Children.Count == 0)
            {
                node.Orientation = UIDockSpaceOrientation.None;
                return NodeHasRuntimeWindows(node);
            }

            return true;
        }

        /// <summary>
        /// Collapse a node into its only remaining child while preserving the node id.
        /// </summary>
        private void CollapseNodeInto(FuDockingLayoutDefinition target, FuDockingLayoutDefinition source)
        {
            if (target == null || source == null || target == source)
            {
                return;
            }

            uint targetId = target.ID;
            uint sourceId = source.ID;
            List<string> sourceWindowIds = _nodeWindowIds.TryGetValue(sourceId, out List<string> windows)
                ? new List<string>(windows)
                : null;
            int sourceSelected = _nodeSelectedIndices.TryGetValue(sourceId, out int selectedIndex) ? selectedIndex : 0;

            target.Name = source.Name;
            target.Proportion = source.Proportion;
            target.Orientation = source.Orientation;
            target.AutoHideTopBar = source.AutoHideTopBar;
            target.LayoutType = source.LayoutType;
            target.WindowsDefinition = source.WindowsDefinition != null ? new List<ushort>(source.WindowsDefinition) : new List<ushort>();
            target.Children = source.Children != null ? source.Children : new List<FuDockingLayoutDefinition>();

            _nodeWindowIds.Remove(targetId);
            _nodeWindowIds.Remove(sourceId);
            if (sourceWindowIds != null && sourceWindowIds.Count > 0)
            {
                _nodeWindowIds[targetId] = sourceWindowIds;
                _nodeSelectedIndices[targetId] = Mathf.Clamp(sourceSelected, 0, sourceWindowIds.Count - 1);
                foreach (string windowId in sourceWindowIds)
                {
                    _windowDockNodeIds[windowId] = targetId;
                    if (Fugui.UIWindows != null && Fugui.UIWindows.TryGetValue(windowId, out FuWindow window))
                    {
                        window.CurrentDockID = targetId;
                    }
                }
            }
            else
            {
                _nodeSelectedIndices.Remove(targetId);
            }

            _nodeSelectedIndices.Remove(sourceId);
            _nodesById.Remove(sourceId);
            _nodeParents.Remove(sourceId);
            _nodeRects.Remove(sourceId);
        }

        /// <summary>
        /// Remove runtime state for a node subtree.
        /// </summary>
        private void UnregisterNodeTree(FuDockingLayoutDefinition node)
        {
            if (node == null)
            {
                return;
            }

            if (_nodeWindowIds.TryGetValue(node.ID, out List<string> windowIds))
            {
                foreach (string windowId in windowIds)
                {
                    _windowDockNodeIds.Remove(windowId);
                }
            }

            _nodeWindowIds.Remove(node.ID);
            _nodeSelectedIndices.Remove(node.ID);
            _nodeRects.Remove(node.ID);
            _nodesById.Remove(node.ID);
            _nodeParents.Remove(node.ID);

            if (node.Children == null)
            {
                return;
            }

            foreach (FuDockingLayoutDefinition child in node.Children)
            {
                UnregisterNodeTree(child);
            }
        }

        /// <summary>
        /// Return whether a node leaf contains runtime windows.
        /// </summary>
        private bool NodeHasRuntimeWindows(FuDockingLayoutDefinition node)
        {
            return node != null &&
                   _nodeWindowIds.TryGetValue(node.ID, out List<string> windowIds) &&
                   windowIds.Count > 0;
        }

        /// <summary>
        /// Return all runtime window ids registered under a dock node tree.
        /// </summary>
        private List<string> GetRuntimeWindowIdsInNodeTree(FuDockingLayoutDefinition node)
        {
            List<string> windowIds = new List<string>();
            CollectRuntimeWindowIdsInNodeTree(node, windowIds);
            return windowIds;
        }

        /// <summary>
        /// Recursively collect runtime window ids under a dock node.
        /// </summary>
        private void CollectRuntimeWindowIdsInNodeTree(FuDockingLayoutDefinition node, List<string> windowIds)
        {
            if (node == null || windowIds == null)
            {
                return;
            }

            if (_nodeWindowIds.TryGetValue(node.ID, out List<string> nodeWindowIds))
            {
                foreach (string windowId in nodeWindowIds)
                {
                    if (!windowIds.Contains(windowId))
                    {
                        windowIds.Add(windowId);
                    }
                }
            }

            if (node.Children == null)
            {
                return;
            }

            foreach (FuDockingLayoutDefinition child in node.Children)
            {
                CollectRuntimeWindowIdsInNodeTree(child, windowIds);
            }
        }

        /// <summary>
        /// Return whether a dock node tree contains a node id.
        /// </summary>
        private bool NodeTreeContainsNode(FuDockingLayoutDefinition root, uint nodeId)
        {
            if (root == null)
            {
                return false;
            }

            if (root.ID == nodeId)
            {
                return true;
            }

            if (root.Children == null)
            {
                return false;
            }

            foreach (FuDockingLayoutDefinition child in root.Children)
            {
                if (NodeTreeContainsNode(child, nodeId))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Return whether a window should be rendered this frame.
        /// </summary>
        internal bool ShouldDrawWindow(FuWindow window)
        {
            if (window == null || !window.IsDocked)
            {
                return true;
            }

            if (!_windowDockNodeIds.TryGetValue(window.ID, out uint nodeId))
            {
                return true;
            }

            if (!_nodeWindowIds.TryGetValue(nodeId, out List<string> windowIds) || windowIds.Count == 0)
            {
                return true;
            }

            int selected = _nodeSelectedIndices.TryGetValue(nodeId, out int selectedIndex) ? selectedIndex : 0;
            selected = Mathf.Clamp(selected, 0, windowIds.Count - 1);
            return windowIds[selected] == window.ID;
        }

        /// <summary>
        /// Return the custom dock node id for a window.
        /// </summary>
        internal uint GetDockNodeId(FuWindow window)
        {
            if (window == null)
            {
                return 0u;
            }

            return _windowDockNodeIds.TryGetValue(window.ID, out uint nodeId) ? nodeId : 0u;
        }

        /// <summary>
        /// Return whether this window is docked inside a floating dock root.
        /// </summary>
        internal bool IsWindowInFloatingDockRoot(FuWindow window)
        {
            uint nodeId = GetDockNodeId(window);
            return nodeId != 0u && FindFloatingDockRootForNode(nodeId) != null;
        }

        /// <summary>
        /// Return whether this docked window owns a visible Fugui tab bar.
        /// </summary>
        internal bool HasDockedTabBar(FuWindow window)
        {
            uint nodeId = GetDockNodeId(window);
            if (nodeId == 0u || !_nodeWindowIds.TryGetValue(nodeId, out List<string> windowIds))
            {
                return false;
            }

            return windowIds.Count > 1 || window.IsDockable || CurrentLayout == null || !CurrentLayout.AutoHideTopBar;
        }

        /// <summary>
        /// Return the custom docked tab bar height.
        /// </summary>
        internal float GetDockedTabBarHeight(FuWindow window)
        {
            if (!HasDockedTabBar(window))
            {
                return 0f;
            }

            float scale = Fugui.CurrentContext != null ? Fugui.CurrentContext.Scale : 1f;
            float textHeight = ImGui.CalcTextSize("Ap").y;
            float minHeight = 22f * scale;
            float verticalPadding = Fugui.Themes.TabPadding.y * 0.65f;
            float witnessReserve = 2f * scale;
            return Mathf.Max(minHeight, textHeight + verticalPadding * 2f + witnessReserve);
        }

        /// <summary>
        /// Draw docked window tabs using the Fugui custom tabs widget.
        /// </summary>
        internal void DrawDockedTabs(FuWindow window, FuLayout layout)
        {
            uint nodeId = GetDockNodeId(window);
            if (nodeId == 0u || layout == null || !_nodeWindowIds.TryGetValue(nodeId, out List<string> windowIds) || windowIds.Count == 0)
            {
                return;
            }

            FloatingDockRoot floatingRoot = FindFloatingDockRootForNode(nodeId);

            List<string> labels = new List<string>();
            for (int i = 0; i < windowIds.Count; i++)
            {
                string windowId = windowIds[i];
                if (Fugui.UIWindows.TryGetValue(windowId, out FuWindow tabWindow))
                {
                    labels.Add(tabWindow.WindowName.Name + "##" + tabWindow.ID);
                }
                else
                {
                    labels.Add("Missing##" + windowId);
                }
            }

            int selected = _nodeSelectedIndices.TryGetValue(nodeId, out int selectedIndex) ? selectedIndex : 0;
            selected = Mathf.Clamp(selected, 0, labels.Count - 1);
            string tabBarId = "customDockTabs" + nodeId;
            FuTabsFlags tabFlags = FuTabsFlags.Compact;
            tabFlags |= floatingRoot != null ? FuTabsFlags.ReserveTrailingSpace : FuTabsFlags.Stretch;
            if (layout.Tabs(tabBarId, labels, ref selected, tabFlags))
            {
                _nodeSelectedIndices[nodeId] = selected;
                Fugui.ForceDrawAllWindows(2);
                if (selected >= 0 && selected < windowIds.Count && Fugui.UIWindows.TryGetValue(windowIds[selected], out FuWindow selectedWindow))
                {
                    selectedWindow.ForceFocusOnNextFrame();
                }
            }
            else
            {
                _nodeSelectedIndices[nodeId] = selected;
            }

            if (floatingRoot != null)
            {
                ProcessFloatingDockRootTabHeader(floatingRoot, window, tabBarId);
            }
            ProcessDockedTabDrag(window, nodeId, windowIds);
        }

        /// <summary>
        /// Use the trailing part of a floating dock root tab bar as the group drag handle.
        /// </summary>
        private void ProcessFloatingDockRootTabHeader(FloatingDockRoot floatingRoot, FuWindow ownerWindow, string tabBarId)
        {
            if (floatingRoot == null ||
                ownerWindow == null ||
                ownerWindow.Container == null ||
                !FuLayout.TryGetLastTabTrailingRect(tabBarId, out Rect grabRect))
            {
                return;
            }

            FuMouseState mouse = ownerWindow.Container.Mouse;
            Vector2Int mousePos = ownerWindow.Container.LocalMousePos;
            bool hovered = _draggedFloatingRoot == null &&
                           _resizedFloatingRoot == null &&
                           _activeResizeNodeId == 0u &&
                           !Fugui.IsDraggingAnything() &&
                           grabRect.Contains(mousePos);

            if (hovered)
            {
                floatingRoot.HeaderHovered = true;
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
                if (mouse.IsDown(FuMouseButton.Left))
                {
                    BeginFloatingDockRootDrag(floatingRoot, ownerWindow.Container, mousePos);
                }
            }

            DrawFloatingDockRootTabGrab(ownerWindow, grabRect, hovered || floatingRoot.HeaderActive, floatingRoot.HeaderActive);
        }

        /// <summary>
        /// Start dragging a floating dock root from its tab header.
        /// </summary>
        private void BeginFloatingDockRootDrag(FloatingDockRoot floatingRoot, IFuWindowContainer container, Vector2Int mousePos)
        {
            if (floatingRoot == null)
            {
                return;
            }

            _draggedFloatingRoot = floatingRoot;
            _floatingRootDragStartMousePos = mousePos;
            _floatingRootDragStartRect = floatingRoot.Rect;
            floatingRoot.HeaderActive = true;
            MoveFloatingDockRootToFront(floatingRoot, container as FuMainWindowContainer);
            ClearPendingTabDrag();
            Fugui.ForceDrawAllWindows(2);
        }

        /// <summary>
        /// Draw a subtle grip in the trailing part of a floating dock root tab bar.
        /// </summary>
        private void DrawFloatingDockRootTabGrab(FuWindow ownerWindow, Rect grabRect, bool hovered, bool active)
        {
            if (ownerWindow == null || grabRect.width <= 1f || grabRect.height <= 1f)
            {
                return;
            }

            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            float scale = Fugui.CurrentContext != null ? Fugui.CurrentContext.Scale : Fugui.Scale;
            uint bg = active
                ? Fugui.Themes.GetColorU32(FuColors.ButtonActive, 0.42f)
                : hovered
                    ? Fugui.Themes.GetColorU32(FuColors.ButtonHovered, 0.34f)
                    : Fugui.Themes.GetColorU32(FuColors.Header, 0.18f);
            uint dotColor = Fugui.Themes.GetColorU32(FuColors.Text, hovered || active ? 0.88f : 0.52f);
            float rounding = Mathf.Max(4f, 5f * scale);

            drawList.AddRectFilled(grabRect.position, grabRect.position + grabRect.size, bg, rounding);
            float dotRadius = Mathf.Max(1.15f, 1.25f * scale);
            float spacing = Mathf.Max(5f, 5f * scale);
            Vector2 center = grabRect.center;
            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    drawList.AddCircleFilled(center + new Vector2(x * spacing, y * spacing), dotRadius, dotColor, 10);
                }
            }
        }

        /// <summary>
        /// Track a docked tab drag and detach it once the pointer moves past click distance.
        /// </summary>
        private void ProcessDockedTabDrag(FuWindow ownerWindow, uint nodeId, List<string> windowIds)
        {
            if (ownerWindow == null || ownerWindow.Container == null || windowIds == null || windowIds.Count == 0)
            {
                return;
            }

            FuMouseState mouse = ownerWindow.Container.Mouse;
            Vector2Int mousePos = ownerWindow.Container.LocalMousePos;
            string tabBarId = "customDockTabs" + nodeId;

            if (mouse.IsDown(FuMouseButton.Left) && FuLayout.TryGetLastTabHitIndex(tabBarId, mousePos, out int hitTabIndex))
            {
                hitTabIndex = Mathf.Clamp(hitTabIndex, 0, windowIds.Count - 1);
                if (Fugui.UIWindows.TryGetValue(windowIds[hitTabIndex], out FuWindow tabWindow) && tabWindow.IsDockable)
                {
                    _pendingTabDragWindowId = tabWindow.ID;
                    _pendingTabDragNodeId = nodeId;
                    _pendingTabDragStartMousePos = mousePos;
                    _pendingTabDragStartWindowPos = _nodeRects.TryGetValue(nodeId, out Rect nodeRect)
                        ? new Vector2Int(Mathf.RoundToInt(nodeRect.x), Mathf.RoundToInt(nodeRect.y))
                        : tabWindow.LocalPosition;
                }
                return;
            }

            if (string.IsNullOrEmpty(_pendingTabDragWindowId))
            {
                return;
            }

            if (!mouse.IsPressed(FuMouseButton.Left))
            {
                ClearPendingTabDrag();
                return;
            }

            if (_pendingTabDragNodeId != nodeId)
            {
                return;
            }

            if (FuLayout.TryGetLastTabHitIndex(tabBarId, mousePos, out int hoveredTabIndex))
            {
                hoveredTabIndex = Mathf.Clamp(hoveredTabIndex, 0, windowIds.Count - 1);
                ReorderDockedTab(nodeId, windowIds, _pendingTabDragWindowId, hoveredTabIndex);
                return;
            }

            float dragDistance = (mousePos - _pendingTabDragStartMousePos).magnitude;
            float threshold = Mathf.Max(4f, Fugui.Settings.ClickMaxDist * Fugui.Scale);
            if (dragDistance < threshold)
            {
                return;
            }

            if (!Fugui.UIWindows.TryGetValue(_pendingTabDragWindowId, out FuWindow draggedWindow) || !draggedWindow.IsDockable)
            {
                ClearPendingTabDrag();
                return;
            }

            BeginDockedWindowDrag(draggedWindow, _pendingTabDragStartMousePos, _pendingTabDragStartWindowPos, mousePos);
            ClearPendingTabDrag();
        }

        /// <summary>
        /// Reorder a dragged tab inside its current dock node.
        /// </summary>
        private void ReorderDockedTab(uint nodeId, List<string> windowIds, string draggedWindowId, int targetIndex)
        {
            if (string.IsNullOrEmpty(draggedWindowId) || windowIds == null || windowIds.Count <= 1)
            {
                return;
            }

            int currentIndex = windowIds.IndexOf(draggedWindowId);
            if (currentIndex < 0)
            {
                return;
            }

            targetIndex = Mathf.Clamp(targetIndex, 0, windowIds.Count - 1);
            if (currentIndex == targetIndex)
            {
                return;
            }

            windowIds.RemoveAt(currentIndex);
            windowIds.Insert(targetIndex, draggedWindowId);
            _nodeSelectedIndices[nodeId] = targetIndex;
            Fugui.ForceDrawAllWindows(2);
        }

        /// <summary>
        /// Detach a tab and turn it into an active floating drag.
        /// </summary>
        private void BeginDockedWindowDrag(FuWindow window, Vector2Int startMousePos, Vector2Int startWindowPos, Vector2Int currentMousePos)
        {
            if (window == null)
            {
                return;
            }

            RemoveWindowFromDockNode(window, true);
            window.BeginCustomDockDrag(startMousePos, startWindowPos, currentMousePos);
            UpdateDockDragPreview(window, currentMousePos);
        }

        /// <summary>
        /// Update and draw the current dock target preview for a dragged floating window.
        /// </summary>
        internal void UpdateDockDragPreview(FuWindow window, Vector2Int mousePos)
        {
            if (window == null || !window.IsDockable || window.IsDocked)
            {
                ClearDockDragState();
                return;
            }

            _dockDragPreviewTarget = ResolveDockDropTarget(mousePos, window, true);
            DrawDockDragPreview();
        }

        /// <summary>
        /// Dock a dragged floating window on drop if it is over a custom target.
        /// </summary>
        internal bool TryDockDraggedWindow(FuWindow window, Vector2Int mousePos)
        {
            if (window == null || !window.IsDockable)
            {
                ClearDockDragState();
                return false;
            }

            DockDropTarget target = ResolveDockDropTarget(mousePos, window, false);
            ClearDockDragState();
            if (!target.IsValid)
            {
                return false;
            }

            bool docked = DockWindowToTarget(window, target);
            if (docked)
            {
                Fugui.ForceDrawAllWindows(2);
            }
            return docked;
        }

        /// <summary>
        /// Dock a floating window into a resolved target.
        /// </summary>
        private bool DockWindowToTarget(FuWindow window, DockDropTarget target)
        {
            if (target.FloatingWindow != null)
            {
                return DockWindowWithFloatingWindow(window, target.FloatingWindow, target.Zone);
            }

            if (target.NodeId == 0u || !_nodesById.TryGetValue(target.NodeId, out FuDockingLayoutDefinition targetNode))
            {
                return false;
            }

            if (target.Zone == DockDropZone.Center || !NodeHasRuntimeWindows(targetNode))
            {
                RegisterWindowToDockNode(window, targetNode);
                return true;
            }

            return SplitDockNodeAndRegister(window, targetNode, target.Zone);
        }

        /// <summary>
        /// Create a floating dock root from two floating windows.
        /// </summary>
        private bool DockWindowWithFloatingWindow(FuWindow draggedWindow, FuWindow targetWindow, DockDropZone zone)
        {
            if (draggedWindow == null ||
                targetWindow == null ||
                draggedWindow == targetWindow ||
                !targetWindow.IsDockable ||
                targetWindow.IsDocked)
            {
                return false;
            }

            Rect rootRect = targetWindow.LocalRect;
            if (rootRect.width <= 1f || rootRect.height <= 1f)
            {
                rootRect = new Rect(targetWindow.LocalPosition, targetWindow.Size);
            }
            rootRect = BuildFloatingDockRootRectFromContent(rootRect, targetWindow.Container);

            FuDockingLayoutDefinition root = CreateRuntimeDockNode("FloatingDockRoot");
            root.AutoHideTopBar = false;
            FloatingDockRoot newFloatingRoot = new FloatingDockRoot(root, rootRect);
            _floatingDockRoots.Add(newFloatingRoot);

            if (zone == DockDropZone.Center || zone == DockDropZone.None)
            {
                RegisterWindowToDockNode(targetWindow, root);
                RegisterWindowToDockNode(draggedWindow, root);
                UpdateCustomLayoutRecursive(root, GetFloatingDockRootContentRect(newFloatingRoot));
                return true;
            }

            FuDockingLayoutDefinition existingLeaf = CreateRuntimeDockNode("FloatingDockExisting");
            FuDockingLayoutDefinition incomingLeaf = CreateRuntimeDockNode("FloatingDockIncoming");
            ConfigureSplitNode(root, existingLeaf, incomingLeaf, zone);
            RebuildNodeIndex();

            RegisterWindowToDockNode(targetWindow, existingLeaf);
            RegisterWindowToDockNode(draggedWindow, incomingLeaf);
            UpdateCustomLayoutRecursive(root, GetFloatingDockRootContentRect(newFloatingRoot));
            return true;
        }

        /// <summary>
        /// Dock an entire floating dock root into a resolved target.
        /// </summary>
        private bool DockFloatingRootToTarget(FloatingDockRoot floatingRoot, DockDropTarget target, FuMainWindowContainer container)
        {
            if (floatingRoot == null || floatingRoot.Layout == null || !target.IsValid)
            {
                return false;
            }

            if (target.FloatingWindow != null)
            {
                return DockFloatingRootWithFloatingWindow(floatingRoot, target.FloatingWindow, target.Zone, container);
            }

            if (target.NodeId == 0u || !_nodesById.TryGetValue(target.NodeId, out FuDockingLayoutDefinition targetNode))
            {
                return false;
            }
            if (NodeTreeContainsNode(floatingRoot.Layout, targetNode.ID))
            {
                return false;
            }

            bool docked = target.Zone == DockDropZone.Center || !NodeHasRuntimeWindows(targetNode)
                ? MergeFloatingRootIntoNode(floatingRoot, targetNode)
                : SplitDockNodeWithFloatingRoot(floatingRoot, targetNode, target.Zone);

            if (docked)
            {
                RebuildNodeIndex();
                PruneEmptyDockRoots();
            }

            return docked;
        }

        /// <summary>
        /// Merge all windows from a floating root as tabs in an existing dock leaf.
        /// </summary>
        private bool MergeFloatingRootIntoNode(FloatingDockRoot floatingRoot, FuDockingLayoutDefinition targetNode)
        {
            if (floatingRoot == null || targetNode == null)
            {
                return false;
            }

            List<string> windowIds = GetRuntimeWindowIdsInNodeTree(floatingRoot.Layout);
            if (windowIds.Count == 0)
            {
                return false;
            }

            _floatingDockRoots.Remove(floatingRoot);
            UnregisterNodeTree(floatingRoot.Layout);
            foreach (string windowId in windowIds)
            {
                if (Fugui.UIWindows.TryGetValue(windowId, out FuWindow window))
                {
                    RegisterWindowToDockNode(window, targetNode);
                }
            }

            return true;
        }

        /// <summary>
        /// Split a target node and insert an entire floating dock root as the incoming side.
        /// </summary>
        private bool SplitDockNodeWithFloatingRoot(FloatingDockRoot floatingRoot, FuDockingLayoutDefinition targetNode, DockDropZone zone)
        {
            if (floatingRoot == null || floatingRoot.Layout == null || targetNode == null)
            {
                return false;
            }

            if (!_nodeWindowIds.TryGetValue(targetNode.ID, out List<string> existingWindowIds) || existingWindowIds.Count == 0)
            {
                return MergeFloatingRootIntoNode(floatingRoot, targetNode);
            }

            List<string> movedWindowIds = new List<string>(existingWindowIds);
            int movedSelected = _nodeSelectedIndices.TryGetValue(targetNode.ID, out int selectedIndex) ? selectedIndex : 0;

            FuDockingLayoutDefinition existingLeaf = CreateRuntimeDockNode(targetNode.Name + "_Existing");
            existingLeaf.WindowsDefinition = targetNode.WindowsDefinition != null
                ? new List<ushort>(targetNode.WindowsDefinition)
                : new List<ushort>();

            _nodeWindowIds.Remove(targetNode.ID);
            _nodeSelectedIndices.Remove(targetNode.ID);
            _nodeWindowIds[existingLeaf.ID] = movedWindowIds;
            _nodeSelectedIndices[existingLeaf.ID] = Mathf.Clamp(movedSelected, 0, movedWindowIds.Count - 1);
            foreach (string movedWindowId in movedWindowIds)
            {
                _windowDockNodeIds[movedWindowId] = existingLeaf.ID;
                if (Fugui.UIWindows.TryGetValue(movedWindowId, out FuWindow movedWindow))
                {
                    movedWindow.CurrentDockID = existingLeaf.ID;
                }
            }

            targetNode.WindowsDefinition = new List<ushort>();
            _floatingDockRoots.Remove(floatingRoot);
            ConfigureSplitNode(targetNode, existingLeaf, floatingRoot.Layout, zone);
            return true;
        }

        /// <summary>
        /// Dock a floating root with a regular floating window.
        /// </summary>
        private bool DockFloatingRootWithFloatingWindow(FloatingDockRoot floatingRoot, FuWindow targetWindow, DockDropZone zone, FuMainWindowContainer container)
        {
            if (floatingRoot == null ||
                floatingRoot.Layout == null ||
                targetWindow == null ||
                targetWindow.IsDocked ||
                !targetWindow.IsDockable)
            {
                return false;
            }

            Rect targetRect = targetWindow.LocalRect;
            if (targetRect.width <= 1f || targetRect.height <= 1f)
            {
                targetRect = new Rect(targetWindow.LocalPosition, targetWindow.Size);
            }

            Rect rootRect = BuildFloatingDockRootRectFromContent(targetRect, targetWindow.Container);
            if (zone == DockDropZone.Center || zone == DockDropZone.None)
            {
                List<string> windowIds = GetRuntimeWindowIdsInNodeTree(floatingRoot.Layout);
                _floatingDockRoots.Remove(floatingRoot);
                UnregisterNodeTree(floatingRoot.Layout);

                FuDockingLayoutDefinition root = CreateRuntimeDockNode("FloatingDockRoot");
                root.AutoHideTopBar = false;
                FloatingDockRoot newFloatingRoot = new FloatingDockRoot(root, rootRect);
                _floatingDockRoots.Add(newFloatingRoot);

                RegisterWindowToDockNode(targetWindow, root);
                foreach (string windowId in windowIds)
                {
                    if (Fugui.UIWindows.TryGetValue(windowId, out FuWindow window))
                    {
                        RegisterWindowToDockNode(window, root);
                    }
                }
                UpdateCustomLayoutRecursive(root, GetFloatingDockRootContentRect(newFloatingRoot), container);
                return true;
            }

            FuDockingLayoutDefinition rootNode = CreateRuntimeDockNode("FloatingDockRoot");
            rootNode.AutoHideTopBar = false;
            FuDockingLayoutDefinition existingLeaf = CreateRuntimeDockNode("FloatingDockExisting");
            _floatingDockRoots.Remove(floatingRoot);
            FloatingDockRoot combinedRoot = new FloatingDockRoot(rootNode, rootRect);
            _floatingDockRoots.Add(combinedRoot);

            ConfigureSplitNode(rootNode, existingLeaf, floatingRoot.Layout, zone);
            RegisterWindowToDockNode(targetWindow, existingLeaf);
            RebuildNodeIndex();
            UpdateCustomLayoutRecursive(rootNode, GetFloatingDockRootContentRect(combinedRoot), container);
            return true;
        }

        /// <summary>
        /// Split an existing dock leaf and register the incoming window into the new leaf.
        /// </summary>
        private bool SplitDockNodeAndRegister(FuWindow window, FuDockingLayoutDefinition targetNode, DockDropZone zone)
        {
            if (window == null || targetNode == null || !_nodeRects.TryGetValue(targetNode.ID, out Rect targetRect))
            {
                return false;
            }

            if (!_nodeWindowIds.TryGetValue(targetNode.ID, out List<string> existingWindowIds) || existingWindowIds.Count == 0)
            {
                RegisterWindowToDockNode(window, targetNode);
                return true;
            }

            List<string> movedWindowIds = new List<string>(existingWindowIds);
            int movedSelected = _nodeSelectedIndices.TryGetValue(targetNode.ID, out int selectedIndex) ? selectedIndex : 0;

            FuDockingLayoutDefinition existingLeaf = CreateRuntimeDockNode(targetNode.Name + "_Existing");
            existingLeaf.WindowsDefinition = targetNode.WindowsDefinition != null
                ? new List<ushort>(targetNode.WindowsDefinition)
                : new List<ushort>();
            FuDockingLayoutDefinition incomingLeaf = CreateRuntimeDockNode(targetNode.Name + "_Incoming");
            incomingLeaf.WindowsDefinition.Add(window.WindowName.ID);

            _nodeWindowIds.Remove(targetNode.ID);
            _nodeSelectedIndices.Remove(targetNode.ID);
            _nodeWindowIds[existingLeaf.ID] = movedWindowIds;
            _nodeSelectedIndices[existingLeaf.ID] = Mathf.Clamp(movedSelected, 0, movedWindowIds.Count - 1);
            foreach (string movedWindowId in movedWindowIds)
            {
                _windowDockNodeIds[movedWindowId] = existingLeaf.ID;
                if (Fugui.UIWindows.TryGetValue(movedWindowId, out FuWindow movedWindow))
                {
                    movedWindow.CurrentDockID = existingLeaf.ID;
                }
            }

            targetNode.WindowsDefinition = new List<ushort>();
            ConfigureSplitNode(targetNode, existingLeaf, incomingLeaf, zone);
            RebuildNodeIndex();

            RegisterWindowToDockNode(window, incomingLeaf);
            UpdateCustomLayoutRecursive(targetNode, targetRect);
            return true;
        }

        /// <summary>
        /// Configure a parent node as a split between an existing and incoming leaf.
        /// </summary>
        private void ConfigureSplitNode(FuDockingLayoutDefinition parent, FuDockingLayoutDefinition existingLeaf, FuDockingLayoutDefinition incomingLeaf, DockDropZone zone)
        {
            parent.Children = new List<FuDockingLayoutDefinition>();
            parent.Proportion = 0.5f;
            parent.Orientation = zone == DockDropZone.Left || zone == DockDropZone.Right
                ? UIDockSpaceOrientation.Horizontal
                : UIDockSpaceOrientation.Vertical;

            bool incomingFirst = zone == DockDropZone.Left || zone == DockDropZone.Top;
            if (incomingFirst)
            {
                parent.Children.Add(incomingLeaf);
                parent.Children.Add(existingLeaf);
            }
            else
            {
                parent.Children.Add(existingLeaf);
                parent.Children.Add(incomingLeaf);
            }
        }

        /// <summary>
        /// Resolve a dock target only when the pointer is over an explicit dock zone button.
        /// </summary>
        private DockDropTarget ResolveDockDropTarget(Vector2 mousePos, FuWindow draggedWindow, bool drawZones)
        {
            DockSurfaceTarget surface = FindDockSurfaceTarget(mousePos, draggedWindow, null, null);
            return ResolveDockDropTarget(mousePos, surface, drawZones);
        }

        /// <summary>
        /// Resolve a dock target for an entire floating dock root.
        /// </summary>
        private DockDropTarget ResolveDockDropTarget(Vector2 mousePos, FloatingDockRoot draggedRoot, bool drawZones, FuMainWindowContainer container)
        {
            DockSurfaceTarget surface = FindDockSurfaceTarget(mousePos, null, draggedRoot, container);
            return ResolveDockDropTarget(mousePos, surface, drawZones);
        }

        /// <summary>
        /// Resolve a dock target from an already selected dock surface.
        /// </summary>
        private DockDropTarget ResolveDockDropTarget(Vector2 mousePos, DockSurfaceTarget surface, bool drawZones)
        {
            if (!surface.IsValid)
            {
                return DockDropTarget.Invalid;
            }

            DockDropZone hoveredZone = DockDropZone.None;
            Dictionary<DockDropZone, Rect> zoneRects = GetDockZoneButtonRects(surface.Rect);
            foreach (KeyValuePair<DockDropZone, Rect> pair in zoneRects)
            {
                if (pair.Value.Contains(mousePos))
                {
                    hoveredZone = pair.Key;
                    break;
                }
            }

            if (drawZones)
            {
                DrawDockZoneButtons(zoneRects, hoveredZone);
            }

            if (hoveredZone == DockDropZone.None)
            {
                return DockDropTarget.Invalid;
            }

            Rect previewRect = GetDropPreviewRect(surface.Rect, hoveredZone);
            return surface.FloatingWindow != null
                ? DockDropTarget.ForFloatingWindow(surface.FloatingWindow, hoveredZone, previewRect)
                : DockDropTarget.ForNode(surface.NodeId, hoveredZone, previewRect);
        }

        /// <summary>
        /// Find the smallest dockable surface under the pointer.
        /// </summary>
        private DockSurfaceTarget FindDockSurfaceTarget(Vector2 mousePos, FuWindow draggedWindow, FloatingDockRoot draggedRoot, FuMainWindowContainer container)
        {
            DockSurfaceTarget frontFloatingTarget = draggedWindow != null
                ? FindFrontFloatingDockSurfaceTarget(mousePos, draggedWindow)
                : FindFrontFloatingDockSurfaceTarget(mousePos, container);
            if (frontFloatingTarget.IsValid)
            {
                return frontFloatingTarget;
            }

            DockSurfaceTarget bestTarget = DockSurfaceTarget.Invalid;
            float bestArea = float.MaxValue;
            foreach (KeyValuePair<uint, Rect> pair in _nodeRects)
            {
                if (!_nodesById.ContainsKey(pair.Key))
                {
                    continue;
                }
                if (draggedRoot != null && NodeTreeContainsNode(draggedRoot.Layout, pair.Key))
                {
                    continue;
                }

                Rect rect = pair.Value;
                if (!rect.Contains(mousePos))
                {
                    continue;
                }

                float area = rect.width * rect.height;
                if (area < bestArea)
                {
                    bestArea = area;
                    bestTarget = DockSurfaceTarget.ForNode(pair.Key, rect);
                }
            }

            return bestTarget;
        }

        /// <summary>
        /// Find the front-most floating dockable window under the pointer.
        /// </summary>
        private DockSurfaceTarget FindFrontFloatingDockSurfaceTarget(Vector2 mousePos, FuWindow draggedWindow)
        {
            if (draggedWindow == null || draggedWindow.Container is not FuMainWindowContainer mainContainer)
            {
                return DockSurfaceTarget.Invalid;
            }

            return FindFrontFloatingDockSurfaceTarget(mousePos, mainContainer, draggedWindow);
        }

        /// <summary>
        /// Find the front-most floating dockable window under the pointer inside a given container.
        /// </summary>
        private DockSurfaceTarget FindFrontFloatingDockSurfaceTarget(Vector2 mousePos, FuMainWindowContainer mainContainer)
        {
            return FindFrontFloatingDockSurfaceTarget(mousePos, mainContainer, null);
        }

        /// <summary>
        /// Find the front-most floating dockable window under the pointer inside a given container.
        /// </summary>
        private DockSurfaceTarget FindFrontFloatingDockSurfaceTarget(Vector2 mousePos, FuMainWindowContainer mainContainer, FuWindow draggedWindow)
        {
            if (mainContainer == null)
            {
                return DockSurfaceTarget.Invalid;
            }

            List<FuWindow> windows = mainContainer.Windows.Values.ToList();
            for (int i = windows.Count - 1; i >= 0; i--)
            {
                FuWindow targetWindow = windows[i];
                if (targetWindow == null ||
                    targetWindow == draggedWindow ||
                    targetWindow.IsDocked ||
                    !targetWindow.IsDockable ||
                    targetWindow.IsExternal ||
                    targetWindow.Container != mainContainer ||
                    targetWindow.IsDragging ||
                    !targetWindow.LocalRect.Contains(mousePos))
                {
                    continue;
                }

                return DockSurfaceTarget.ForFloatingWindow(targetWindow, targetWindow.LocalRect);
            }

            return DockSurfaceTarget.Invalid;
        }

        /// <summary>
        /// Build the visible dock zone button rectangles for a target surface.
        /// </summary>
        private Dictionary<DockDropZone, Rect> GetDockZoneButtonRects(Rect surfaceRect)
        {
            float scale = Fugui.CurrentContext != null ? Fugui.CurrentContext.Scale : Fugui.Scale;
            float buttonSize = Mathf.Clamp(38f * scale, 32f, 48f);
            float edgeInset = Mathf.Max(18f * scale, buttonSize * 0.65f);
            Vector2 center = surfaceRect.center;

            return new Dictionary<DockDropZone, Rect>
            {
                { DockDropZone.Center, CenteredRect(center, buttonSize) },
                { DockDropZone.Left, CenteredRect(new Vector2(surfaceRect.xMin + edgeInset, center.y), buttonSize) },
                { DockDropZone.Right, CenteredRect(new Vector2(surfaceRect.xMax - edgeInset, center.y), buttonSize) },
                { DockDropZone.Top, CenteredRect(new Vector2(center.x, surfaceRect.yMin + edgeInset), buttonSize) },
                { DockDropZone.Bottom, CenteredRect(new Vector2(center.x, surfaceRect.yMax - edgeInset), buttonSize) }
            };
        }

        /// <summary>
        /// Build a square rect centered on a screen-space point.
        /// </summary>
        private Rect CenteredRect(Vector2 center, float size)
        {
            return new Rect(center - new Vector2(size * 0.5f, size * 0.5f), new Vector2(size, size));
        }

        /// <summary>
        /// Draw the explicit docking zone buttons.
        /// </summary>
        private void DrawDockZoneButtons(Dictionary<DockDropZone, Rect> zoneRects, DockDropZone hoveredZone)
        {
            if (zoneRects == null || zoneRects.Count == 0)
            {
                return;
            }

            ImDrawListPtr drawList = ImGui.GetForegroundDrawList();
            foreach (KeyValuePair<DockDropZone, Rect> pair in zoneRects)
            {
                DrawDockZoneButton(drawList, pair.Value, pair.Key, pair.Key == hoveredZone);
            }
        }

        /// <summary>
        /// Draw one premium docking zone button.
        /// </summary>
        private void DrawDockZoneButton(ImDrawListPtr drawList, Rect rect, DockDropZone zone, bool hovered)
        {
            float scale = Fugui.CurrentContext != null ? Fugui.CurrentContext.Scale : Fugui.Scale;
            float rounding = Mathf.Max(8f, 9f * scale);
            Vector2 min = rect.position;
            Vector2 max = rect.position + rect.size;
            Vector2 center = rect.center;

            uint shadow = ImGui.GetColorU32(new Vector4(0f, 0f, 0f, hovered ? 0.34f : 0.22f));
            uint bg = hovered
                ? Fugui.Themes.GetColorU32(FuColors.DockingPreview, 0.92f)
                : Fugui.Themes.GetColorU32(FuColors.WindowBg, 0.88f);
            uint border = hovered
                ? Fugui.Themes.GetColorU32(FuColors.DockingPreview)
                : Fugui.Themes.GetColorU32(FuColors.Border, 0.85f);
            uint inner = hovered
                ? Fugui.Themes.GetColorU32(FuColors.Text)
                : Fugui.Themes.GetColorU32(FuColors.Text, 0.84f);

            drawList.AddRectFilled(min + new Vector2(0f, 3f * scale), max + new Vector2(0f, 3f * scale), shadow, rounding);
            drawList.AddRectFilled(min, max, bg, rounding);
            drawList.AddRect(min, max, border, rounding, ImDrawFlags.None, hovered ? Mathf.Max(2f, 2f * scale) : Mathf.Max(1f, 1f * scale));
            DrawDockZoneIcon(drawList, center, rect.size.x, zone, inner, scale);
        }

        /// <summary>
        /// Draw a simple symbolic icon for a docking zone.
        /// </summary>
        private void DrawDockZoneIcon(ImDrawListPtr drawList, Vector2 center, float buttonSize, DockDropZone zone, uint color, float scale)
        {
            float s = buttonSize * 0.26f;
            float thickness = Mathf.Max(1.6f, 1.8f * scale);
            switch (zone)
            {
                case DockDropZone.Left:
                    drawList.AddTriangleFilled(center + new Vector2(-s, 0f), center + new Vector2(s * 0.45f, -s), center + new Vector2(s * 0.45f, s), color);
                    break;
                case DockDropZone.Right:
                    drawList.AddTriangleFilled(center + new Vector2(s, 0f), center + new Vector2(-s * 0.45f, -s), center + new Vector2(-s * 0.45f, s), color);
                    break;
                case DockDropZone.Top:
                    drawList.AddTriangleFilled(center + new Vector2(0f, -s), center + new Vector2(-s, s * 0.45f), center + new Vector2(s, s * 0.45f), color);
                    break;
                case DockDropZone.Bottom:
                    drawList.AddTriangleFilled(center + new Vector2(0f, s), center + new Vector2(-s, -s * 0.45f), center + new Vector2(s, -s * 0.45f), color);
                    break;
                default:
                    Rect tabIcon = new Rect(center - new Vector2(s, s * 0.75f), new Vector2(s * 2f, s * 1.5f));
                    drawList.AddRect(tabIcon.position, tabIcon.position + tabIcon.size, color, 3f * scale, ImDrawFlags.None, thickness);
                    drawList.AddLine(tabIcon.position + new Vector2(0f, s * 0.42f), tabIcon.position + new Vector2(tabIcon.width, s * 0.42f), color, thickness);
                    break;
            }
        }

        /// <summary>
        /// Return the preview rect for a target rect and drop zone.
        /// </summary>
        private Rect GetDropPreviewRect(Rect rect, DockDropZone zone)
        {
            switch (zone)
            {
                case DockDropZone.Left:
                    return new Rect(rect.x, rect.y, rect.width * 0.5f, rect.height);
                case DockDropZone.Right:
                    return new Rect(rect.x + rect.width * 0.5f, rect.y, rect.width * 0.5f, rect.height);
                case DockDropZone.Top:
                    return new Rect(rect.x, rect.y, rect.width, rect.height * 0.5f);
                case DockDropZone.Bottom:
                    return new Rect(rect.x, rect.y + rect.height * 0.5f, rect.width, rect.height * 0.5f);
                default:
                    float inset = Mathf.Max(2f, 3f * Fugui.Scale);
                    return new Rect(rect.x + inset, rect.y + inset, Mathf.Max(1f, rect.width - inset * 2f), Mathf.Max(1f, rect.height - inset * 2f));
            }
        }

        /// <summary>
        /// Draw the current docking preview overlay.
        /// </summary>
        private void DrawDockDragPreview()
        {
            if (!_dockDragPreviewTarget.IsValid)
            {
                return;
            }

            Rect rect = _dockDragPreviewTarget.PreviewRect;
            ImDrawListPtr drawList = ImGui.GetForegroundDrawList();
            Vector2 min = rect.position;
            Vector2 max = rect.position + rect.size;
            float thickness = Mathf.Max(2f, 2f * Fugui.Scale);
            drawList.AddRectFilled(min, max, Fugui.Themes.GetColorU32(FuColors.DockingPreview, 0.25f), 0f);
            drawList.AddRect(min, max, Fugui.Themes.GetColorU32(FuColors.DockingPreview), 0f, ImDrawFlags.None, thickness);
        }

        /// <summary>
        /// Clear the pending tab drag state.
        /// </summary>
        private void ClearPendingTabDrag()
        {
            _pendingTabDragWindowId = null;
            _pendingTabDragNodeId = 0u;
            _pendingTabDragStartMousePos = Vector2Int.zero;
            _pendingTabDragStartWindowPos = Vector2Int.zero;
        }

        /// <summary>
        /// Clear active dock drag preview state.
        /// </summary>
        private void ClearDockDragState()
        {
            ClearPendingTabDrag();
            _dockDragPreviewTarget = DockDropTarget.Invalid;
        }

        /// <summary>
        /// Clear the active floating dock root drag state.
        /// </summary>
        private void ClearFloatingRootDragState()
        {
            _draggedFloatingRoot = null;
            _floatingRootDragStartMousePos = Vector2Int.zero;
            _floatingRootDragStartRect = default;
            _dockDragPreviewTarget = DockDropTarget.Invalid;
        }

        /// <summary>
        /// Clear the active floating dock root resize state.
        /// </summary>
        private void ClearFloatingRootResizeState()
        {
            if (_resizedFloatingRoot != null)
            {
                _resizedFloatingRoot.ResizeActive = false;
                _resizedFloatingRoot.ResizeEdge = FloatingDockRootResizeEdge.None;
            }

            _resizedFloatingRoot = null;
            _floatingRootResizeStartMousePos = Vector2Int.zero;
            _floatingRootResizeStartRect = default;
            _floatingRootResizeEdge = FloatingDockRootResizeEdge.None;
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
        private enum DockDropZone
        {
            None,
            Center,
            Left,
            Right,
            Top,
            Bottom
        }

        private enum FloatingDockRootResizeEdge
        {
            None,
            Top,
            Left,
            Right,
            Bottom,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        private struct DockDropTarget
        {
            public uint NodeId;
            public FuWindow FloatingWindow;
            public DockDropZone Zone;
            public Rect PreviewRect;
            public bool IsValid;

            public static DockDropTarget Invalid
            {
                get
                {
                    return new DockDropTarget
                    {
                        NodeId = 0u,
                        FloatingWindow = null,
                        Zone = DockDropZone.None,
                        PreviewRect = default,
                        IsValid = false
                    };
                }
            }

            public static DockDropTarget ForNode(uint nodeId, DockDropZone zone, Rect previewRect)
            {
                return new DockDropTarget
                {
                    NodeId = nodeId,
                    FloatingWindow = null,
                    Zone = zone == DockDropZone.None ? DockDropZone.Center : zone,
                    PreviewRect = previewRect,
                    IsValid = nodeId != 0u
                };
            }

            public static DockDropTarget ForFloatingWindow(FuWindow window, DockDropZone zone, Rect previewRect)
            {
                return new DockDropTarget
                {
                    NodeId = 0u,
                    FloatingWindow = window,
                    Zone = zone == DockDropZone.None ? DockDropZone.Center : zone,
                    PreviewRect = previewRect,
                    IsValid = window != null
                };
            }
        }

        private struct DockSurfaceTarget
        {
            public uint NodeId;
            public FuWindow FloatingWindow;
            public Rect Rect;
            public bool IsValid;

            public static DockSurfaceTarget Invalid
            {
                get
                {
                    return new DockSurfaceTarget
                    {
                        NodeId = 0u,
                        FloatingWindow = null,
                        Rect = default,
                        IsValid = false
                    };
                }
            }

            public static DockSurfaceTarget ForNode(uint nodeId, Rect rect)
            {
                return new DockSurfaceTarget
                {
                    NodeId = nodeId,
                    FloatingWindow = null,
                    Rect = rect,
                    IsValid = nodeId != 0u
                };
            }

            public static DockSurfaceTarget ForFloatingWindow(FuWindow window, Rect rect)
            {
                return new DockSurfaceTarget
                {
                    NodeId = 0u,
                    FloatingWindow = window,
                    Rect = rect,
                    IsValid = window != null
                };
            }
        }

        private class FloatingDockRoot
        {
            public FuDockingLayoutDefinition Layout;
            public Rect Rect;
            public bool HeaderHovered;
            public bool HeaderActive;
            public FloatingDockRootResizeEdge ResizeEdge;
            public bool ResizeActive;

            public FloatingDockRoot(FuDockingLayoutDefinition layout, Rect rect)
            {
                Layout = layout;
                Rect = rect;
            }
        }

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
