using Fu.Framework;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        private readonly Dictionary<uint, Rect> _nodeContentRects = new Dictionary<uint, Rect>();
        private readonly Dictionary<uint, FuDockingLayoutDefinition> _nodesById = new Dictionary<uint, FuDockingLayoutDefinition>();
        private readonly Dictionary<uint, FuDockingLayoutDefinition> _nodeParents = new Dictionary<uint, FuDockingLayoutDefinition>();
        private readonly List<FloatingDockRoot> _floatingDockRoots = new List<FloatingDockRoot>();
        private readonly List<DockSplitterDrawData> _mainDockSplitters = new List<DockSplitterDrawData>();
        private readonly List<DockSplitterDrawData> _floatingDockSplitters = new List<DockSplitterDrawData>();
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
        private bool _floatingRootDragUsesGlobalMouseButton;
        private FloatingDockRoot _resizedFloatingRoot;
        private Vector2Int _floatingRootResizeStartMousePos;
        private Rect _floatingRootResizeStartRect;
        private FloatingDockRootResizeEdge _floatingRootResizeEdge = FloatingDockRootResizeEdge.None;
#if FU_EXTERNALIZATION
        private Vector2Int _floatingRootResizeStartNativeWindowPosition;
        private ExternalDockPreviewState _externalDockPreview;
#endif

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
                CurrentLayout = firstLayoutInfo.Value.Clone();
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

            setLayoutInstance(layout.Clone(), getOnlyAutoInstantiated);
        }

        /// <summary>
        /// Set a runtime layout instance. The instance can be mutated freely by the custom docking system.
        /// </summary>
        /// <param name="layout">Runtime layout instance to set.</param>
        /// <param name="getOnlyAutoInstantiated">Whatever you only want windows in this layout that will auto instantiated by layout</param>
        private void setLayoutInstance(FuDockingLayoutDefinition layout, bool getOnlyAutoInstantiated)
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

            setLayoutInstance(layout, getOnlyAutoInstantiated);
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
            _nodeContentRects.Clear();
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
            if (window == null || !TryGetDockNodeContentRect(nodeId, out Rect rect))
            {
                return;
            }

            Vector2Int pos = new Vector2Int(Mathf.RoundToInt(rect.x), Mathf.RoundToInt(rect.y));
            Vector2Int size = new Vector2Int(Mathf.Max(1, Mathf.RoundToInt(rect.width)), Mathf.Max(1, Mathf.RoundToInt(rect.height)));
            window.ApplyProgrammaticRect(pos, size, notifyResize);
        }

        private bool TryGetDockNodeContentRect(uint nodeId, out Rect rect)
        {
            if (_nodeContentRects.TryGetValue(nodeId, out rect))
            {
                return true;
            }

            if (_nodeRects.TryGetValue(nodeId, out Rect nodeRect))
            {
                rect = GetDockNodeContentRect(nodeId, nodeRect);
                return true;
            }

            rect = default;
            return false;
        }

        private Rect GetDockNodeContentRect(uint nodeId, Rect nodeRect)
        {
            float tabHeight = Mathf.Min(GetDockedTabBarHeight(nodeId), Mathf.Max(0f, nodeRect.height));
            float chrome = GetDockNodeChromeThickness();
            float contentX = nodeRect.x + chrome;
            float contentY = nodeRect.y + Mathf.Min(tabHeight, Mathf.Max(0f, nodeRect.height - chrome));
            float contentWidth = Mathf.Max(1f, nodeRect.width - chrome * 2f);
            float contentHeight = Mathf.Max(1f, nodeRect.yMax - contentY - chrome);
            return new Rect(contentX, contentY, contentWidth, contentHeight);
        }

        private float GetDockNodeChromeThickness()
        {
            float scale = Fugui.CurrentContext != null ? Fugui.CurrentContext.Scale : Fugui.Scale;
            return Mathf.Max(1f, Fugui.Themes.WindowBorderSize, 1f * scale);
        }

        private float GetFloatingDockRootResizeGutter()
        {
            float scale = Fugui.CurrentContext != null ? Fugui.CurrentContext.Scale : Fugui.Scale;
            return Mathf.Max(5f, 5f * scale);
        }

        /// <summary>
        /// Update manual dock node rectangles and apply them to their windows.
        /// </summary>
        internal void UpdateCustomLayout(IFuWindowContainer container, Rect contentRect)
        {
            if (container == null)
            {
                return;
            }

            bool isMainContainer = container == Fugui.DefaultContainer;
            if (isMainContainer && CurrentLayout != null)
            {
                EnsureCustomLayoutPrepared(CurrentLayout);
            }
            CleanupClosedWindows();
            _nodeRects.Clear();
            _nodeContentRects.Clear();
            _mainDockSplitters.Clear();
            _floatingDockSplitters.Clear();
            if (isMainContainer && CurrentLayout != null)
            {
                UpdateCustomLayoutRecursive(CurrentLayout, contentRect, container, true);
            }
            foreach (FloatingDockRoot floatingRoot in _floatingDockRoots.ToList())
            {
                if (!_floatingDockRoots.Contains(floatingRoot))
                {
                    continue;
                }

                if (floatingRoot.Container != container)
                {
                    continue;
                }

                ProcessFloatingDockRoot(floatingRoot, container);
                if (!_floatingDockRoots.Contains(floatingRoot))
                {
                    continue;
                }

#if FU_EXTERNALIZATION
                SyncExternalFloatingDockRootToNativeContainer(floatingRoot, container);
#endif
                UpdateCustomLayoutRecursive(floatingRoot.Layout, GetFloatingDockRootContentRect(floatingRoot), container, false, floatingRoot);
            }
        }

        /// <summary>
        /// Recursively compute and apply custom dock rects.
        /// </summary>
        private void UpdateCustomLayoutRecursive(FuDockingLayoutDefinition node, Rect rect, IFuWindowContainer container = null, bool deferSplitterDraw = false, FloatingDockRoot owningFloatingRoot = null)
        {
            if (node == null)
                return;

            if (node.Children != null && node.Children.Count >= 2 && node.Orientation != UIDockSpaceOrientation.None)
            {
                ProcessDockSplitter(node, rect, container, deferSplitterDraw, owningFloatingRoot);
                FuDockingLayoutDefinition firstChild = node.Children[0];
                FuDockingLayoutDefinition secondChild = node.Children[1];
                float proportion = Mathf.Clamp(node.Proportion, 0.05f, 0.95f);
                if (node.Orientation == UIDockSpaceOrientation.Horizontal)
                {
                    float firstWidth = Mathf.Round(rect.width * proportion);
                    Rect first = new Rect(rect.x, rect.y, firstWidth, rect.height);
                    Rect second = new Rect(rect.x + firstWidth, rect.y, Mathf.Max(1f, rect.width - firstWidth), rect.height);
                    UpdateCustomLayoutRecursive(firstChild, first, container, deferSplitterDraw, owningFloatingRoot);
                    UpdateCustomLayoutRecursive(secondChild, second, container, deferSplitterDraw, owningFloatingRoot);
                }
                else
                {
                    float firstHeight = Mathf.Round(rect.height * proportion);
                    Rect first = new Rect(rect.x, rect.y, rect.width, firstHeight);
                    Rect second = new Rect(rect.x, rect.y + firstHeight, rect.width, Mathf.Max(1f, rect.height - firstHeight));
                    UpdateCustomLayoutRecursive(firstChild, first, container, deferSplitterDraw, owningFloatingRoot);
                    UpdateCustomLayoutRecursive(secondChild, second, container, deferSplitterDraw, owningFloatingRoot);
                }

                return;
            }

            _nodeRects[node.ID] = rect;
            Rect contentRect = GetDockNodeContentRect(node.ID, rect);
            _nodeContentRects[node.ID] = contentRect;
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

                Vector2Int pos = new Vector2Int(Mathf.RoundToInt(contentRect.x), Mathf.RoundToInt(contentRect.y));
                Vector2Int size = new Vector2Int(Mathf.Max(1, Mathf.RoundToInt(contentRect.width)), Mathf.Max(1, Mathf.RoundToInt(contentRect.height)));
                window.CurrentDockID = node.ID;
                window.IsDocked = true;
                window.ApplyProgrammaticRect(pos, size, true);
            }
        }

        /// <summary>
        /// Draw and process the splitter between two dock children.
        /// </summary>
        private void ProcessDockSplitter(FuDockingLayoutDefinition node, Rect rect, IFuWindowContainer container, bool deferDraw, FloatingDockRoot owningFloatingRoot)
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
            bool mouseInGrabRect = grabRect.Contains(mousePos);
            bool inputBlocked = (active || mouseInGrabRect) &&
                                (IsDockInteractionBlockedByHigherFloatingSurface(container, mousePos, owningFloatingRoot) ||
                                 IsDockInteractionBlockedByInputOwner(owningFloatingRoot) ||
                                 IsPointerOverDockedTabBar(mousePos));
            if (active && inputBlocked)
            {
                _activeResizeNodeId = 0u;
                active = false;
            }

            bool hovered = _activeResizeNodeId == 0u && !inputBlocked && mouseInGrabRect;

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
                Fugui.ForceDrawAllWindows(2);
            }

            if (deferDraw || owningFloatingRoot != null)
            {
                DockSplitterDrawData drawData = new DockSplitterDrawData
                {
                    GrabRect = grabRect,
                    Horizontal = horizontal,
                    Hovered = hovered,
                    Active = active,
                    VisualThickness = visualThickness,
                    Scale = scale,
                    MousePosition = mousePos,
                    ClipRect = owningFloatingRoot != null ? owningFloatingRoot.Rect : GetContainerContentRect(container),
                    OwningFloatingRoot = owningFloatingRoot
                };

                if (owningFloatingRoot != null)
                {
                    _floatingDockSplitters.Add(drawData);
                }
                else
                {
                    _mainDockSplitters.Add(drawData);
                }
            }
            else
            {
                _mainDockSplitters.Add(new DockSplitterDrawData
                {
                    GrabRect = grabRect,
                    Horizontal = horizontal,
                    Hovered = hovered,
                    Active = active,
                    VisualThickness = visualThickness,
                    Scale = scale,
                    MousePosition = mousePos,
                    ClipRect = GetContainerContentRect(container),
                    OwningFloatingRoot = null
                });
            }
        }

        /// <summary>
        /// Draw dock splitters from the current docked window draw list so normal window z-order can hide them.
        /// </summary>
        internal void DrawDockSplittersForWindow(FuWindow window)
        {
            if (window == null ||
                !window.IsDocked ||
                window.Container == null)
            {
                return;
            }

            IFuWindowContainer container = window.Container;
            FloatingDockRoot floatingRoot = FindFloatingDockRootForNode(GetDockNodeId(window));
            List<DockSplitterDrawData> splitters = floatingRoot != null ? _floatingDockSplitters : _mainDockSplitters;
            if (!IsDockSplitterDrawOwner(window, container, floatingRoot))
            {
                return;
            }

            Rect clipRect = floatingRoot != null ? floatingRoot.Rect : GetContainerContentRect(container);
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            drawList.PushClipRect(clipRect.position, clipRect.position + clipRect.size, false);
            for (int i = 0; i < splitters.Count; i++)
            {
                DockSplitterDrawData splitter = splitters[i];
                if (splitter.OwningFloatingRoot != floatingRoot)
                {
                    continue;
                }

                DrawDockSplitter(drawList, splitter.GrabRect, splitter.Horizontal, splitter.Hovered, splitter.Active, splitter.VisualThickness, splitter.Scale, splitter.MousePosition, splitter.ClipRect);
            }
            drawList.PopClipRect();
        }

        /// <summary>
        /// Return whether the current docked window is the top-most visible window for its dock surface.
        /// </summary>
        private bool IsDockSplitterDrawOwner(FuWindow candidate, IFuWindowContainer container, FloatingDockRoot floatingRoot)
        {
            FuWindow topMost = null;
            foreach (FuWindow window in GetContainerWindows(container))
            {
                if (window == null ||
                    !window.IsDocked ||
                    window.Container != container ||
                    !window.IsOpened ||
                    !ShouldDrawWindow(window))
                {
                    continue;
                }

                if (FindFloatingDockRootForNode(GetDockNodeId(window)) == floatingRoot)
                {
                    topMost = window;
                }
            }

            return topMost == candidate;
        }

        /// <summary>
        /// Draw the dock splitter feedback.
        /// </summary>
        private void DrawDockSplitter(ImDrawListPtr drawList, Rect grabRect, bool horizontal, bool hovered, bool active, float visualThickness, float scale)
        {
            DrawDockSplitter(drawList, grabRect, horizontal, hovered, active, visualThickness, scale, grabRect.center, grabRect);
        }

        /// <summary>
        /// Draw the dock splitter feedback.
        /// </summary>
        private void DrawDockSplitter(ImDrawListPtr drawList, Rect grabRect, bool horizontal, bool hovered, bool active, float visualThickness, float scale, Vector2 mousePos, Rect clipRect)
        {
            bool highlighted = hovered || active;
            uint edgeLineColor = Fugui.Themes.GetColorU32(FuColors.Border, highlighted ? active ? 0.88f : 0.62f : 0.35f);
            uint handleColor = Fugui.Themes.GetColorU32(active ? FuColors.HighlightActive : FuColors.HighlightHovered, active ? 1f : 0.9f);
            float edgeLineThickness = Mathf.Max(1f, 1f * scale);
            float handleShort = Mathf.Max(5f, 5f * scale);
            float handleLong = Mathf.Max(36f, 42f * scale);
            float centerX = grabRect.x + grabRect.width * 0.5f;
            float centerY = grabRect.y + grabRect.height * 0.5f;

            if (horizontal)
            {
                drawList.AddLine(new Vector2(centerX, grabRect.y), new Vector2(centerX, grabRect.yMax), edgeLineColor, edgeLineThickness);
                if (highlighted)
                {
                    Vector2 handleSize = new Vector2(handleShort, Mathf.Min(handleLong, Mathf.Max(handleShort, grabRect.height)));
                    float handleY = Mathf.Clamp(mousePos.y, grabRect.y + handleSize.y * 0.5f, grabRect.yMax - handleSize.y * 0.5f);
                    Rect handle = new Rect(new Vector2(centerX - handleSize.x * 0.5f, handleY - handleSize.y * 0.5f), handleSize);
                    DrawResizeFeedbackHandle(drawList, handle, handleColor, handleSize.x * 0.5f, clipRect);
                }
            }
            else
            {
                drawList.AddLine(new Vector2(grabRect.x, centerY), new Vector2(grabRect.xMax, centerY), edgeLineColor, edgeLineThickness);
                if (highlighted)
                {
                    Vector2 handleSize = new Vector2(Mathf.Min(handleLong, Mathf.Max(handleShort, grabRect.width)), handleShort);
                    float handleX = Mathf.Clamp(mousePos.x, grabRect.x + handleSize.x * 0.5f, grabRect.xMax - handleSize.x * 0.5f);
                    Rect handle = new Rect(new Vector2(handleX - handleSize.x * 0.5f, centerY - handleSize.y * 0.5f), handleSize);
                    DrawResizeFeedbackHandle(drawList, handle, handleColor, handleSize.y * 0.5f, clipRect);
                }
            }
        }

        /// <summary>
        /// Draw an accented resize handle above child content while keeping it inside the dock surface.
        /// </summary>
        private void DrawResizeFeedbackHandle(ImDrawListPtr drawList, Rect handle, uint color, float rounding, Rect clipRect)
        {
            drawList.PushClipRect(clipRect.position, clipRect.position + clipRect.size, false);
            drawList.AddRectFilled(handle.position, handle.position + handle.size, color, rounding);
            drawList.PopClipRect();
        }

        /// <summary>
        /// Draw hover/active resize feedback for a floating dock root with the same visual language as floating windows.
        /// </summary>
        private void DrawFloatingDockRootResizeFeedback(ImDrawListPtr drawList, FloatingDockRoot floatingRoot, Vector2 mousePos)
        {
            if (floatingRoot == null || floatingRoot.ResizeEdge == FloatingDockRootResizeEdge.None)
            {
                return;
            }

            Rect rect = floatingRoot.Rect;
            if (rect.width <= 1f || rect.height <= 1f)
            {
                return;
            }

            bool active = floatingRoot.ResizeActive;
            float scale = Fugui.CurrentContext != null ? Fugui.CurrentContext.Scale : Fugui.Scale;
            uint feedbackColor = Fugui.Themes.GetColorU32(active ? FuColors.HighlightActive : FuColors.HighlightHovered, active ? 1f : 0.9f);
            uint edgeLineColor = Fugui.Themes.GetColorU32(FuColors.Border, active ? 0.88f : 0.62f);
            float handleThickness = active ? Mathf.Max(2f, 2.5f * scale) : Mathf.Max(1.5f, 2f * scale);
            float edgeLineThickness = Mathf.Max(1f, 1f * scale);
            float inset = handleThickness * 0.5f;
            Vector2 min = rect.position + new Vector2(inset, inset);
            Vector2 max = rect.position + rect.size - new Vector2(inset, inset);
            float handleShort = Mathf.Max(5f, 5f * scale);
            float handleLong = Mathf.Max(36f, 42f * scale);
            float verticalHandleLong = Mathf.Min(handleLong, Mathf.Max(handleShort, max.y - min.y));
            float horizontalHandleLong = Mathf.Min(handleLong, Mathf.Max(handleShort, max.x - min.x));
            float rounding = handleShort * 0.5f;

            drawList.PushClipRect(rect.position, rect.position + rect.size, false);

            FloatingDockRootResizeEdge edge = floatingRoot.ResizeEdge;
            if (edge == FloatingDockRootResizeEdge.Left || edge == FloatingDockRootResizeEdge.BottomLeft)
            {
                drawList.AddLine(min, new Vector2(min.x, max.y), edgeLineColor, edgeLineThickness);
                if (edge == FloatingDockRootResizeEdge.Left)
                {
                    float clampedY = Mathf.Clamp(mousePos.y, min.y + verticalHandleLong * 0.5f, max.y - verticalHandleLong * 0.5f);
                    Rect handle = new Rect(new Vector2(min.x - handleShort * 0.5f + inset, clampedY - verticalHandleLong * 0.5f), new Vector2(handleShort, verticalHandleLong));
                    drawList.AddRectFilled(handle.position, handle.position + handle.size, feedbackColor, rounding);
                }
            }
            if (edge == FloatingDockRootResizeEdge.Right || edge == FloatingDockRootResizeEdge.BottomRight)
            {
                drawList.AddLine(new Vector2(max.x, min.y), max, edgeLineColor, edgeLineThickness);
                if (edge == FloatingDockRootResizeEdge.Right)
                {
                    float clampedY = Mathf.Clamp(mousePos.y, min.y + verticalHandleLong * 0.5f, max.y - verticalHandleLong * 0.5f);
                    Rect handle = new Rect(new Vector2(max.x - handleShort * 0.5f - inset, clampedY - verticalHandleLong * 0.5f), new Vector2(handleShort, verticalHandleLong));
                    drawList.AddRectFilled(handle.position, handle.position + handle.size, feedbackColor, rounding);
                }
            }
            if (edge == FloatingDockRootResizeEdge.Bottom || edge == FloatingDockRootResizeEdge.BottomLeft || edge == FloatingDockRootResizeEdge.BottomRight)
            {
                drawList.AddLine(new Vector2(min.x, max.y), max, edgeLineColor, edgeLineThickness);
                if (edge == FloatingDockRootResizeEdge.Bottom)
                {
                    float clampedX = Mathf.Clamp(mousePos.x, min.x + horizontalHandleLong * 0.5f, max.x - horizontalHandleLong * 0.5f);
                    Rect handle = new Rect(new Vector2(clampedX - horizontalHandleLong * 0.5f, max.y - handleShort * 0.5f - inset), new Vector2(horizontalHandleLong, handleShort));
                    drawList.AddRectFilled(handle.position, handle.position + handle.size, feedbackColor, rounding);
                }
            }
            if (edge == FloatingDockRootResizeEdge.BottomLeft || edge == FloatingDockRootResizeEdge.BottomRight)
            {
                DrawFloatingDockRootCornerResizeHandle(drawList, edge, min, max, feedbackColor, handleThickness, scale);
            }

            drawList.PopClipRect();
        }

        /// <summary>
        /// Draw a compact L-shaped corner resize handle for floating dock roots.
        /// </summary>
        private void DrawFloatingDockRootCornerResizeHandle(ImDrawListPtr drawList, FloatingDockRootResizeEdge edge, Vector2 min, Vector2 max, uint color, float thickness, float scale)
        {
            float length = Mathf.Max(18f, 22f * scale);
            float bar = Mathf.Max(thickness, 4f * scale);
            float rounding = bar * 0.5f;
            float inset = Mathf.Max(2f, 2.5f * scale);

            if (edge == FloatingDockRootResizeEdge.BottomLeft)
            {
                Vector2 hMin = new Vector2(min.x + inset, max.y - bar - inset);
                Vector2 hMax = new Vector2(Mathf.Min(min.x + inset + length, max.x), max.y - inset);
                Vector2 vMin = new Vector2(min.x + inset, Mathf.Max(min.y, max.y - inset - length));
                Vector2 vMax = new Vector2(min.x + inset + bar, max.y - inset);
                drawList.AddRectFilled(hMin, hMax, color, rounding);
                drawList.AddRectFilled(vMin, vMax, color, rounding);
                return;
            }

            if (edge == FloatingDockRootResizeEdge.BottomRight)
            {
                Vector2 hMin = new Vector2(Mathf.Max(min.x, max.x - inset - length), max.y - bar - inset);
                Vector2 hMax = new Vector2(max.x - inset, max.y - inset);
                Vector2 vMin = new Vector2(max.x - inset - bar, Mathf.Max(min.y, max.y - inset - length));
                Vector2 vMax = new Vector2(max.x - inset, max.y - inset);
                drawList.AddRectFilled(hMin, hMax, color, rounding);
                drawList.AddRectFilled(vMin, vMax, color, rounding);
            }
        }

        /// <summary>
        /// Return whether a floating window or floating dock root above the owner should receive this pointer interaction.
        /// </summary>
        private bool IsDockInteractionBlockedByHigherFloatingSurface(IFuWindowContainer container, Vector2 mousePos, FloatingDockRoot owningFloatingRoot)
        {
            if (container == null)
            {
                return false;
            }

            if (Fugui.IsInsideAnyPopup(mousePos))
            {
                return true;
            }

            List<FuWindow> windows = GetContainerWindows(container);
            for (int i = windows.Count - 1; i >= 0; i--)
            {
                FuWindow window = windows[i];
                if (!IsDockInteractionSurface(window, container))
                {
                    continue;
                }

                if (window.IsDocked)
                {
                    FloatingDockRoot windowFloatingRoot = FindFloatingDockRootForNode(GetDockNodeId(window));
                    if (windowFloatingRoot == null || !windowFloatingRoot.Rect.Contains(mousePos))
                    {
                        continue;
                    }

                    return windowFloatingRoot != owningFloatingRoot;
                }

                if (window.LocalRect.Contains(mousePos))
                {
                    return true;
                }
            }

            return false;
        }

        internal bool IsWindowOccludedByFloatingDockSurface(FuWindow window)
        {
            if (window == null || window.Container == null)
            {
                return false;
            }

            Vector2 mousePos = window.Container.LocalMousePos;
            FloatingDockRoot owningFloatingRoot = window.IsDocked
                ? FindFloatingDockRootForNode(GetDockNodeId(window))
                : null;

            for (int i = _floatingDockRoots.Count - 1; i >= 0; i--)
            {
                FloatingDockRoot floatingRoot = _floatingDockRoots[i];
                if (floatingRoot == null ||
                    floatingRoot.Container != window.Container ||
                    floatingRoot == owningFloatingRoot ||
                    !floatingRoot.Rect.Contains(mousePos))
                {
                    continue;
                }

                if (IsFloatingDockRootAboveWindow(floatingRoot, window, owningFloatingRoot))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsFloatingDockRootAboveWindow(FloatingDockRoot floatingRoot, FuWindow window, FloatingDockRoot owningFloatingRoot)
        {
            if (floatingRoot == null || window == null)
            {
                return false;
            }

            if (owningFloatingRoot != null)
            {
                return _floatingDockRoots.IndexOf(floatingRoot) > _floatingDockRoots.IndexOf(owningFloatingRoot);
            }

            if (window.IsDocked)
            {
                return true;
            }

            bool foundWindow = false;
            bool rootWindowAfterTarget = false;
            window.Container.OnEachWindow(containerWindow =>
            {
                if (rootWindowAfterTarget)
                {
                    return;
                }

                if (containerWindow == window)
                {
                    foundWindow = true;
                    return;
                }

                if (foundWindow &&
                    containerWindow != null &&
                    NodeTreeContainsWindowId(floatingRoot.Layout, containerWindow.ID) &&
                    ShouldDrawWindow(containerWindow))
                {
                    rootWindowAfterTarget = true;
                }
            });

            return rootWindowAfterTarget;
        }

        /// <summary>
        /// Return whether an already pressed window should keep dock splitters from taking the same drag.
        /// </summary>
        private bool IsDockInteractionBlockedByInputOwner(FloatingDockRoot owningFloatingRoot)
        {
            FuWindow inputOwner = FuWindow.InputFocusedWindow;
            if (inputOwner == null)
            {
                return false;
            }

            if (!inputOwner.IsDocked)
            {
                return true;
            }

            FloatingDockRoot ownerRoot = FindFloatingDockRootForNode(GetDockNodeId(inputOwner));
            return ownerRoot != owningFloatingRoot;
        }

        /// <summary>
        /// Return whether the pointer is over a Fugui docked tab/header strip.
        /// </summary>
        private bool IsPointerOverDockedTabBar(Vector2 mousePos)
        {
            foreach (KeyValuePair<uint, List<string>> pair in _nodeWindowIds)
            {
                if (!_nodeRects.TryGetValue(pair.Key, out Rect nodeRect) || pair.Value == null || pair.Value.Count == 0)
                {
                    continue;
                }

                int selected = _nodeSelectedIndices.TryGetValue(pair.Key, out int selectedIndex) ? selectedIndex : 0;
                selected = Mathf.Clamp(selected, 0, pair.Value.Count - 1);
                if (!Fugui.UIWindows.TryGetValue(pair.Value[selected], out FuWindow window))
                {
                    continue;
                }

                float tabHeight = GetDockedTabBarHeight(window);
                if (tabHeight <= 0f)
                {
                    continue;
                }

                Rect tabRect = new Rect(nodeRect.x, nodeRect.y, nodeRect.width, Mathf.Min(tabHeight, nodeRect.height));
                if (tabRect.Contains(mousePos))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Return whether a window participates in floating mouse occlusion for custom docking.
        /// </summary>
        private bool IsDockInteractionSurface(FuWindow window, IFuWindowContainer container)
        {
            return window != null &&
                   window.Container == container &&
                   window.IsOpened &&
                   window.IsInitialized &&
                   window.IsInterractable &&
                   !window.Is3DWindow;
        }

        /// <summary>
        /// Draw and process the movable shell around a floating dock tree.
        /// </summary>
        private void ProcessFloatingDockRoot(FloatingDockRoot floatingRoot, IFuWindowContainer container)
        {
            if (floatingRoot == null || floatingRoot.Layout == null || container == null)
            {
                return;
            }

            bool active = _draggedFloatingRoot == floatingRoot;
            Vector2Int mousePos = active ? GetFloatingRootDragMousePosition(container) : container.LocalMousePos;
            bool resizing = _resizedFloatingRoot == floatingRoot;
            if (!active)
            {
                floatingRoot.HeaderHovered = false;
            }
            floatingRoot.ExplodeHovered = false;
            floatingRoot.ExplodeActive = false;
            bool inputBlocked = IsDockInteractionBlockedByHigherFloatingSurface(container, mousePos, floatingRoot);
            if (ProcessFloatingDockRootHeader(floatingRoot, container, inputBlocked))
            {
                return;
            }

            active = _draggedFloatingRoot == floatingRoot;
            mousePos = active ? GetFloatingRootDragMousePosition(container) : container.LocalMousePos;
            resizing = _resizedFloatingRoot == floatingRoot;
            if (resizing && inputBlocked)
            {
                ClearFloatingRootResizeState();
                resizing = false;
            }
            FloatingDockRootResizeEdge hoveredResizeEdge = _draggedFloatingRoot == null &&
                                                           _resizedFloatingRoot == null &&
                                                           _activeResizeNodeId == 0u &&
                                                           !inputBlocked &&
                                                           !Fugui.IsDraggingAnything()
                ? GetHoveredFloatingDockRootResizeEdge(floatingRoot.Rect, mousePos)
                : FloatingDockRootResizeEdge.None;
            floatingRoot.HeaderActive = active;
            floatingRoot.ResizeEdge = resizing ? _floatingRootResizeEdge : hoveredResizeEdge;
            floatingRoot.ResizeActive = resizing;

            if (hoveredResizeEdge != FloatingDockRootResizeEdge.None && container.Mouse.IsDown(FuMouseButton.Left))
            {
                _resizedFloatingRoot = floatingRoot;
                _floatingRootResizeStartMousePos = GetFloatingRootResizeMousePosition(container);
                _floatingRootResizeStartRect = floatingRoot.Rect;
                _floatingRootResizeEdge = hoveredResizeEdge;
#if FU_EXTERNALIZATION
                _floatingRootResizeStartNativeWindowPosition = container is FuExternalWindowContainer externalContainer ? externalContainer.Position : Vector2Int.zero;
#endif
                MoveFloatingDockRootToFront(floatingRoot, container);
                resizing = true;
                floatingRoot.ResizeActive = true;
                floatingRoot.ResizeEdge = hoveredResizeEdge;
            }

            if (resizing)
            {
                if (container.Mouse.IsPressed(FuMouseButton.Left))
                {
                    ApplyFloatingDockRootResize(floatingRoot, GetFloatingRootResizeMousePosition(container));
                    Fugui.ForceDrawAllWindows(2);
                }
                else
                {
                    floatingRoot.Rect = ClampFloatingDockRootHeaderVisibleInContainer(floatingRoot.Rect, container);
                    ClearFloatingRootResizeState();
                    return;
                }
            }

            if (active)
            {
                if (IsFloatingRootDragMousePressed(container))
                {
                    mousePos = GetFloatingRootDragMousePosition(container);
                    Vector2Int delta = mousePos - _floatingRootDragStartMousePos;
                    floatingRoot.Rect = new Rect(
                        _floatingRootDragStartRect.x + delta.x,
                        _floatingRootDragStartRect.y + delta.y,
                        _floatingRootDragStartRect.width,
                        _floatingRootDragStartRect.height);
                    if (TryExternalizeFloatingDockRootDrag(floatingRoot, container, mousePos))
                    {
                        return;
                    }
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
                        floatingRoot.Rect = ClampFloatingDockRootHeaderVisibleInContainer(floatingRoot.Rect, container);
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
                Fugui.ForceDrawAllWindows(2);
            }
        }

        private bool IsFloatingRootDragMousePressed(IFuWindowContainer container)
        {
            bool localPressed = container?.Mouse != null && container.Mouse.IsPressed(FuMouseButton.Left);
#if FU_EXTERNALIZATION
            return localPressed ||
                   (_floatingRootDragUsesGlobalMouseButton && Fugui.IsGlobalMouseButtonPressed(FuMouseButton.Left));
#else
            return localPressed;
#endif
        }

        private Vector2Int GetFloatingRootDragMousePosition(IFuWindowContainer container)
        {
#if FU_EXTERNALIZATION
            if (_floatingRootDragUsesGlobalMouseButton)
            {
                Vector2Int globalMouse = Fugui.GetGlobalMousePosition();
                if (container is FuMainWindowContainer mainContainer)
                {
                    return mainContainer.AbsoluteScreenToLocalPosition(globalMouse);
                }

                return container != null ? globalMouse - container.Position : Vector2Int.zero;
            }
#endif

            return container != null ? container.LocalMousePos : Vector2Int.zero;
        }

        /// <summary>
        /// Move a floating dock root to a native external window when it is dragged outside its current container.
        /// </summary>
        private bool TryExternalizeFloatingDockRootDrag(FloatingDockRoot floatingRoot, IFuWindowContainer container, Vector2Int mousePos)
        {
#if FU_EXTERNALIZATION
            if (floatingRoot == null || floatingRoot.Layout == null || container == null)
            {
                return false;
            }

            Rect containerRect = new Rect(Vector2.zero, container.Size);
            if (containerRect.Contains(mousePos))
            {
                return false;
            }

            FuWindow representativeWindow = GetFirstRuntimeWindowInNodeTree(floatingRoot.Layout);
            if (representativeWindow == null || !representativeWindow.IsExternalizable)
            {
                return false;
            }

            bool wasDragging = representativeWindow.IsDragging;
            representativeWindow.IsDragging = true;
            Fugui.ExternalizeWindow(representativeWindow);
            if (representativeWindow.Container == container)
            {
                representativeWindow.IsDragging = wasDragging;
                return false;
            }

            floatingRoot.HeaderHovered = false;
            floatingRoot.HeaderActive = false;
            ClearFloatingRootDragState();
            return true;
#else
            return false;
#endif
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
                case FloatingDockRootResizeEdge.Bottom:
                    rect.height = Mathf.Max(minHeight, _floatingRootResizeStartRect.height + delta.y);
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
#if FU_EXTERNALIZATION
            ApplyExternalFloatingDockRootNativeResize(floatingRoot);
#endif
        }

        private Vector2Int GetFloatingRootResizeMousePosition(IFuWindowContainer container)
        {
#if FU_EXTERNALIZATION
            if (container is FuExternalWindowContainer)
            {
                return Fugui.GetGlobalMousePosition();
            }
#endif

            return container != null ? container.LocalMousePos : Vector2Int.zero;
        }

#if FU_EXTERNALIZATION
        private void ApplyExternalFloatingDockRootNativeResize(FloatingDockRoot floatingRoot)
        {
            if (!IsExternalNativeDockRoot(floatingRoot, floatingRoot?.Container) ||
                floatingRoot.Container is not FuExternalWindowContainer externalContainer)
            {
                return;
            }

            Rect rect = floatingRoot.Rect;
            Vector2Int nativeOffset = new Vector2Int(Mathf.RoundToInt(rect.x), Mathf.RoundToInt(rect.y));
            Vector2Int nativePosition = _floatingRootResizeStartNativeWindowPosition + nativeOffset;
            Vector2Int nativeSize = new Vector2Int(Mathf.Max(1, Mathf.RoundToInt(rect.width)), Mathf.Max(1, Mathf.RoundToInt(rect.height)));
            externalContainer.SetNativeBounds(nativePosition, nativeSize);
            floatingRoot.Rect = new Rect(Vector2.zero, nativeSize);
        }

        private void SyncExternalFloatingDockRootToNativeContainer(FloatingDockRoot floatingRoot, IFuWindowContainer container)
        {
            if (_resizedFloatingRoot == floatingRoot ||
                _draggedFloatingRoot == floatingRoot ||
                !IsExternalNativeDockRoot(floatingRoot, container))
            {
                return;
            }

            Vector2Int size = container.Size;
            Rect nativeRect = new Rect(Vector2.zero, new Vector2(Mathf.Max(1, size.x), Mathf.Max(1, size.y)));
            if (Mathf.Abs(floatingRoot.Rect.x) < 0.5f &&
                Mathf.Abs(floatingRoot.Rect.y) < 0.5f &&
                Mathf.Abs(floatingRoot.Rect.width - nativeRect.width) < 0.5f &&
                Mathf.Abs(floatingRoot.Rect.height - nativeRect.height) < 0.5f)
            {
                return;
            }

            floatingRoot.Rect = nativeRect;
        }

        private bool IsExternalNativeDockRoot(FloatingDockRoot floatingRoot, IFuWindowContainer container)
        {
            if (floatingRoot == null ||
                floatingRoot.Layout == null ||
                container is not FuExternalWindowContainer externalContainer)
            {
                return false;
            }

            foreach (string windowId in GetRuntimeWindowIdsInNodeTree(floatingRoot.Layout))
            {
                if (Fugui.UIWindows.TryGetValue(windowId, out FuWindow window) &&
                    window.Container == container &&
                    externalContainer.IsNativeChromeOwner(window))
                {
                    return true;
                }
            }

            return false;
        }
#endif

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
            bool bottom = inHorizontalRange && mousePos.y <= rect.yMax && mousePos.y >= rect.yMax - border;
            bool bottomCorner = inHorizontalRange && mousePos.y <= rect.yMax && mousePos.y >= rect.yMax - corner;
            bool leftCorner = inVerticalRange && mousePos.x >= rect.xMin && mousePos.x <= rect.xMin + corner;
            bool rightCorner = inVerticalRange && mousePos.x <= rect.xMax && mousePos.x >= rect.xMax - corner;

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
                case FloatingDockRootResizeEdge.Bottom:
                    ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNS);
                    break;
                case FloatingDockRootResizeEdge.BottomLeft:
                    ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNESW);
                    break;
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
        private void MoveFloatingDockRootToFront(FloatingDockRoot floatingRoot, IFuWindowContainer container)
        {
            if (floatingRoot == null)
            {
                return;
            }

            _floatingDockRoots.Remove(floatingRoot);
            _floatingDockRoots.Add(floatingRoot);
            BringContainerWindowsToFront(container, GetRuntimeWindowIdsInNodeTree(floatingRoot.Layout));
        }

        private List<FuWindow> GetContainerWindows(IFuWindowContainer container)
        {
            List<FuWindow> windows = new List<FuWindow>();
            container?.OnEachWindow(window =>
            {
                if (window != null)
                {
                    windows.Add(window);
                }
            });
            return windows;
        }

        private Rect GetContainerContentRect(IFuWindowContainer container)
        {
            if (container is FuMainWindowContainer mainContainer)
            {
                return mainContainer.ContentRect;
            }

            if (container == null)
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            return new Rect(Vector2.zero, container.Size);
        }

        private void BringContainerWindowsToFront(IFuWindowContainer container, IEnumerable<string> windowIds)
        {
            if (container == null || windowIds == null)
            {
                return;
            }

            if (container is FuMainWindowContainer mainContainer)
            {
                mainContainer.BringWindowsToFront(windowIds);
            }
#if FU_EXTERNALIZATION
            else if (container is FuExternalWindowContainer externalContainer)
            {
                externalContainer.BringWindowsToFront(windowIds);
            }
#endif
        }

        /// <summary>
        /// Return the content rect available to the actual dock tree inside a floating dock root.
        /// </summary>
        private Rect GetFloatingDockRootContentRect(FloatingDockRoot floatingRoot)
        {
            if (floatingRoot == null)
            {
                return default;
            }

            float headerHeight = Mathf.Min(GetFloatingDockRootHeaderHeight(), Mathf.Max(0f, floatingRoot.Rect.height));
            float border = GetDockNodeChromeThickness();
            float resizeGutter = GetFloatingDockRootResizeGutter();
            float contentX = floatingRoot.Rect.x + resizeGutter;
            float contentY = floatingRoot.Rect.y + headerHeight + border;
            float contentWidth = Mathf.Max(1f, floatingRoot.Rect.width - resizeGutter * 2f);
            float contentHeight = Mathf.Max(1f, floatingRoot.Rect.yMax - contentY - resizeGutter);
            return new Rect(
                contentX,
                contentY,
                contentWidth,
                contentHeight);
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
            float headerHeight = GetFloatingDockRootHeaderHeight();
            float border = GetDockNodeChromeThickness();
            float resizeGutter = GetFloatingDockRootResizeGutter();
            Rect rect = new Rect(
                contentRect.x - resizeGutter,
                contentRect.y - headerHeight - border,
                contentRect.width + resizeGutter * 2f,
                contentRect.height + headerHeight + border + resizeGutter);
            return FitFloatingDockRootInsideContainer(rect, container);
        }

        private Rect GetFloatingDockRootHeaderRect(FloatingDockRoot floatingRoot)
        {
            if (floatingRoot == null)
            {
                return default;
            }

            float headerHeight = Mathf.Min(GetFloatingDockRootHeaderHeight(), Mathf.Max(0f, floatingRoot.Rect.height));
            return new Rect(floatingRoot.Rect.x, floatingRoot.Rect.y, floatingRoot.Rect.width, headerHeight);
        }

        internal void DrawDockspaceBeforeWindow(FuWindow window)
        {
            RenderDockspaceForWindow(window);
        }

        internal bool RenderDockspaceForWindow(FuWindow window, bool preventUpdatingMouse = false, bool preventUpdatingKeyboard = false)
        {
            if (window == null || !window.IsDocked || window.Container == null)
            {
                return false;
            }

            uint nodeId = GetDockNodeId(window);
            FloatingDockRoot floatingRoot = FindFloatingDockRootForNode(nodeId);
            FuDockingLayoutDefinition root = floatingRoot != null ? floatingRoot.Layout : CurrentLayout;
            if (root == null || !NodeTreeContainsNode(root, nodeId))
            {
                return true;
            }

            if (!IsDockspaceRenderOwner(window, root, floatingRoot))
            {
                return true;
            }

            Rect rect = floatingRoot != null ? floatingRoot.Rect : GetContainerContentRect(window.Container);
            Rect contentRect = floatingRoot != null ? GetFloatingDockRootContentRect(floatingRoot) : rect;
            DrawDockspaceWindow(root, rect, contentRect, window.Container, floatingRoot, preventUpdatingMouse, preventUpdatingKeyboard);
            return true;
        }

        private bool IsDockspaceRenderOwner(FuWindow candidate, FuDockingLayoutDefinition root, FloatingDockRoot floatingRoot)
        {
            if (candidate == null || root == null || candidate.Container == null)
            {
                return false;
            }

            bool foundOwner = false;
            bool isOwner = false;
            candidate.Container.OnEachWindow(containerWindow =>
            {
                if (foundOwner)
                {
                    return;
                }

                if (containerWindow == null ||
                    containerWindow.Container != candidate.Container ||
                    !containerWindow.IsDocked ||
                    !ShouldDrawWindow(containerWindow))
                {
                    return;
                }

                uint nodeId = GetDockNodeId(containerWindow);
                if (floatingRoot != null)
                {
                    if (FindFloatingDockRootForNode(nodeId) != floatingRoot)
                    {
                        return;
                    }
                }
                else if (FindFloatingDockRootForNode(nodeId) != null || !NodeTreeContainsNode(root, nodeId))
                {
                    return;
                }

                foundOwner = true;
                isOwner = containerWindow == candidate;
            });

            return isOwner;
        }

        private void DrawDockspaceWindow(
            FuDockingLayoutDefinition root,
            Rect rect,
            Rect contentRect,
            IFuWindowContainer container,
            FloatingDockRoot floatingRoot,
            bool preventUpdatingMouse,
            bool preventUpdatingKeyboard)
        {
            if (root == null || container == null || rect.width <= 1f || rect.height <= 1f)
            {
                return;
            }

            float rounding = floatingRoot != null ? Mathf.Max(2f, Fugui.Themes.WindowRounding) : 0f;
            ImGui.SetNextWindowPos(rect.position, ImGuiCond.Always);
            ImGui.SetNextWindowSize(rect.size, ImGuiCond.Always);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.zero);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, rounding);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
            ImGuiWindowFlags flags =
                ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoCollapse |
                ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse |
                ImGuiWindowFlags.NoSavedSettings |
                ImGuiWindowFlags.NoDocking |
                ImGuiWindowFlags.NoNav |
                ImGuiWindowFlags.NoFocusOnAppearing |
                ImGuiWindowFlags.NoBringToFrontOnFocus |
                ImGuiWindowFlags.NoBackground;

            string dockspaceWindowId = GetDockspaceWindowId(root, floatingRoot);
            bool beganDockspaceWindow = false;
            try
            {
                bool visible = ImGui.Begin(dockspaceWindowId, flags);
                beganDockspaceWindow = true;
                if (floatingRoot == null)
                {
                    BringImGuiWindowToDisplayBack(dockspaceWindowId);
                }

                if (visible)
                {
                    ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                    if (floatingRoot != null)
                    {
                        DrawFloatingDockRootShell(drawList, floatingRoot, container);
                    }
                    else
                    {
                        drawList.AddRectFilled(rect.position, rect.position + rect.size, Fugui.Themes.GetColorU32(FuColors.WindowBg, 1f), 0f);
                    }

                    DrawDockNodeTree(root, contentRect, container, preventUpdatingMouse, preventUpdatingKeyboard);
                    DrawDockspaceSplitters(drawList, container, floatingRoot);
                }
            }
            finally
            {
                if (beganDockspaceWindow)
                {
                    ImGui.End();
                }
                ImGui.PopStyleVar(3);
            }
        }

        private string GetDockspaceWindowId(FuDockingLayoutDefinition root, FloatingDockRoot floatingRoot)
        {
            if (floatingRoot != null && floatingRoot.Layout != null)
            {
                return "##FuFloatingDockspace_" + floatingRoot.Layout.ID;
            }

            return root != null ? "##FuDockspace_" + root.ID : "##FuDockspace";
        }

        private unsafe void BringImGuiWindowToDisplayBack(string windowId)
        {
            if (string.IsNullOrEmpty(windowId))
            {
                return;
            }

            int byteCount = Encoding.UTF8.GetByteCount(windowId);
            byte* nativeName = stackalloc byte[byteCount + 1];
            int offset = Fugui.GetUtf8(windowId, nativeName, byteCount);
            nativeName[offset] = 0;

            ImGuiWindow* nativeWindow = ImGuiInternal.igFindWindowByName(nativeName);
            if (nativeWindow == null)
            {
                return;
            }

            ImGuiInternal.igBringWindowToDisplayBack(nativeWindow);
        }

        private void DrawDockNodeTree(
            FuDockingLayoutDefinition node,
            Rect rect,
            IFuWindowContainer container,
            bool preventUpdatingMouse,
            bool preventUpdatingKeyboard)
        {
            if (node == null || container == null || rect.width <= 1f || rect.height <= 1f)
            {
                return;
            }

            if (node.Children != null && node.Children.Count >= 2 && node.Orientation != UIDockSpaceOrientation.None)
            {
                FuDockingLayoutDefinition firstChild = node.Children[0];
                FuDockingLayoutDefinition secondChild = node.Children[1];
                float proportion = Mathf.Clamp(node.Proportion, 0.05f, 0.95f);
                if (node.Orientation == UIDockSpaceOrientation.Horizontal)
                {
                    float firstWidth = Mathf.Round(rect.width * proportion);
                    DrawDockNodeTree(firstChild, new Rect(rect.x, rect.y, firstWidth, rect.height), container, preventUpdatingMouse, preventUpdatingKeyboard);
                    DrawDockNodeTree(secondChild, new Rect(rect.x + firstWidth, rect.y, Mathf.Max(1f, rect.width - firstWidth), rect.height), container, preventUpdatingMouse, preventUpdatingKeyboard);
                }
                else
                {
                    float firstHeight = Mathf.Round(rect.height * proportion);
                    DrawDockNodeTree(firstChild, new Rect(rect.x, rect.y, rect.width, firstHeight), container, preventUpdatingMouse, preventUpdatingKeyboard);
                    DrawDockNodeTree(secondChild, new Rect(rect.x, rect.y + firstHeight, rect.width, Mathf.Max(1f, rect.height - firstHeight)), container, preventUpdatingMouse, preventUpdatingKeyboard);
                }

                return;
            }

            DrawDockLeaf(node, rect, preventUpdatingMouse, preventUpdatingKeyboard);
        }

        private void DrawDockLeaf(FuDockingLayoutDefinition node, Rect rect, bool preventUpdatingMouse, bool preventUpdatingKeyboard)
        {
            if (node == null ||
                !_nodeWindowIds.TryGetValue(node.ID, out List<string> windowIds) ||
                windowIds.Count == 0)
            {
                return;
            }

            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            uint body = Fugui.Themes.GetColorU32(FuColors.WindowBg, 1f);
            uint border = Fugui.Themes.GetColorU32(FuColors.Border, 0.72f);
            drawList.PushClipRect(rect.position, rect.position + rect.size, false);
            drawList.AddRectFilled(rect.position, rect.position + rect.size, body, 0f);

            float tabHeight = GetDockedTabBarHeight(node.ID);
            if (tabHeight > 0f)
            {
                ImGui.SetCursorScreenPos(rect.position);
                using (FuLayout tabLayout = new FuLayout())
                {
                    FuWindow ownerWindow = GetSelectedWindowForNode(node.ID);
                    DrawDockNodeTabs(node.ID, ownerWindow, tabLayout);
                }
            }

            drawList.AddRect(
                rect.position,
                rect.position + rect.size,
                border,
                0f,
                ImDrawFlags.None,
                Mathf.Max(1f, Fugui.Themes.WindowBorderSize));
            drawList.PopClipRect();

            if (!_nodeContentRects.TryGetValue(node.ID, out Rect contentRect))
            {
                contentRect = GetDockNodeContentRect(node.ID, rect);
            }

            FuWindow selectedWindow = GetSelectedWindowForNode(node.ID);
            if (selectedWindow == null)
            {
                return;
            }

            ImGui.SetCursorScreenPos(contentRect.position);
            selectedWindow.DrawDockedChildContent(contentRect.size, preventUpdatingMouse, preventUpdatingKeyboard);
        }

        private FuWindow GetSelectedWindowForNode(uint nodeId)
        {
            if (!_nodeWindowIds.TryGetValue(nodeId, out List<string> windowIds) || windowIds.Count == 0)
            {
                return null;
            }

            int selected = _nodeSelectedIndices.TryGetValue(nodeId, out int selectedIndex) ? selectedIndex : 0;
            selected = Mathf.Clamp(selected, 0, windowIds.Count - 1);
            return Fugui.UIWindows.TryGetValue(windowIds[selected], out FuWindow window) ? window : null;
        }

        private void DrawDockspaceSplitters(ImDrawListPtr drawList, IFuWindowContainer container, FloatingDockRoot floatingRoot)
        {
            List<DockSplitterDrawData> splitters = floatingRoot != null ? _floatingDockSplitters : _mainDockSplitters;
            Rect clipRect = floatingRoot != null ? floatingRoot.Rect : GetContainerContentRect(container);
            drawList.PushClipRect(clipRect.position, clipRect.position + clipRect.size, false);
            for (int i = 0; i < splitters.Count; i++)
            {
                DockSplitterDrawData splitter = splitters[i];
                if (splitter.OwningFloatingRoot != floatingRoot)
                {
                    continue;
                }

                DrawDockSplitter(drawList, splitter.GrabRect, splitter.Horizontal, splitter.Hovered, splitter.Active, splitter.VisualThickness, splitter.Scale, splitter.MousePosition, splitter.ClipRect);
            }
            drawList.PopClipRect();
        }

        private void DrawFloatingDockRootShell(ImDrawListPtr drawList, FloatingDockRoot floatingRoot, IFuWindowContainer container)
        {
            if (floatingRoot == null)
            {
                return;
            }

            float rounding = Mathf.Max(2f, Fugui.Themes.WindowRounding);
            uint body = Fugui.Themes.GetColorU32(FuColors.WindowBg, 1f);
            uint border = Fugui.Themes.GetColorU32(FuColors.Border, 0.86f);
            Rect headerRect = GetFloatingDockRootHeaderRect(floatingRoot);

            drawList.PushClipRect(floatingRoot.Rect.position, floatingRoot.Rect.position + floatingRoot.Rect.size, false);
            drawList.AddRectFilled(floatingRoot.Rect.position, floatingRoot.Rect.position + floatingRoot.Rect.size, body, rounding);
            drawList.AddRect(
                floatingRoot.Rect.position,
                floatingRoot.Rect.position + floatingRoot.Rect.size,
                border,
                rounding,
                ImDrawFlags.None,
                Mathf.Max(1f, Fugui.Themes.WindowBorderSize));
            if (headerRect.width > 0f && headerRect.height > 0f)
            {
                bool externalNativeRoot = false;
#if FU_EXTERNALIZATION
                externalNativeRoot = IsExternalNativeDockRoot(floatingRoot, container);
#endif
                Rect explodeRect = GetFloatingDockRootExplodeButtonRect(headerRect, externalNativeRoot);
                DrawFloatingDockRootHeader(drawList, floatingRoot, container, headerRect, externalNativeRoot, floatingRoot.HeaderHovered, explodeRect, floatingRoot.ExplodeHovered, floatingRoot.ExplodeActive);
            }
            DrawFloatingDockRootResizeFeedback(drawList, floatingRoot, GetFloatingRootDragMousePosition(container));
            drawList.PopClipRect();
        }

        private bool IsFloatingDockRootShellDrawOwner(FuWindow window, FloatingDockRoot floatingRoot)
        {
            if (window == null || floatingRoot == null)
            {
                return false;
            }

            bool foundOwner = false;
            bool isOwner = false;
            window.Container.OnEachWindow(containerWindow =>
            {
                if (foundOwner)
                {
                    return;
                }

                if (containerWindow == null ||
                    containerWindow.Container != window.Container ||
                    !containerWindow.IsDocked ||
                    !ShouldDrawWindow(containerWindow))
                {
                    return;
                }

                if (FindFloatingDockRootForNode(GetDockNodeId(containerWindow)) != floatingRoot)
                {
                    return;
                }

                foundOwner = true;
                isOwner = containerWindow == window;
            });

            return isOwner;
        }

        private bool ProcessFloatingDockRootHeader(FloatingDockRoot floatingRoot, IFuWindowContainer container, bool inputBlocked)
        {
            if (floatingRoot == null || container == null || container.Mouse == null)
            {
                return false;
            }

            Rect headerRect = GetFloatingDockRootHeaderRect(floatingRoot);
            if (headerRect.width <= 1f || headerRect.height <= 1f)
            {
                return false;
            }

            Vector2 mousePos = container.LocalMousePos;
            bool externalNativeRoot = false;
#if FU_EXTERNALIZATION
            externalNativeRoot = IsExternalNativeDockRoot(floatingRoot, container);
#endif
            Rect explodeRect = GetFloatingDockRootExplodeButtonRect(headerRect, externalNativeRoot);
            bool explodeHovered = !inputBlocked && explodeRect.Contains(mousePos);
            bool explodeActive = explodeHovered && container.Mouse.IsPressed(FuMouseButton.Left);
            floatingRoot.ExplodeHovered = explodeHovered;
            floatingRoot.ExplodeActive = explodeActive;
            if (explodeHovered)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                if (container.Mouse.IsClicked(FuMouseButton.Left))
                {
                    ExplodeFloatingDockRoot(floatingRoot, container);
                    return true;
                }
            }

            float rightReserve = GetFloatingDockRootHeaderRightReserve(externalNativeRoot);
            Rect dragRect = new Rect(headerRect.x, headerRect.y, Mathf.Max(0f, headerRect.width - rightReserve), headerRect.height);
            bool hovered = _draggedFloatingRoot == null &&
                           _resizedFloatingRoot == null &&
                           _activeResizeNodeId == 0u &&
                           !inputBlocked &&
                           !explodeHovered &&
                           !Fugui.IsDraggingAnything() &&
                           dragRect.Contains(mousePos);
            floatingRoot.HeaderHovered = hovered;
            if (hovered)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
                if (!externalNativeRoot && container.Mouse.IsDown(FuMouseButton.Left))
                {
                    BeginFloatingDockRootDrag(floatingRoot, container, container.LocalMousePos);
                }
            }

            return false;
        }

        private void DrawFloatingDockRootHeader(
            ImDrawListPtr drawList,
            FloatingDockRoot floatingRoot,
            IFuWindowContainer container,
            Rect headerRect,
            bool externalNativeRoot,
            bool hovered,
            Rect explodeRect,
            bool explodeHovered,
            bool explodeActive)
        {
            float scale = Fugui.CurrentContext != null ? Fugui.CurrentContext.Scale : Fugui.Scale;
            bool active = floatingRoot.HeaderActive || (_draggedFloatingRoot == floatingRoot);
            uint bg = active
                ? Fugui.Themes.GetColorU32(FuColors.TitleBgActive, 1f)
                : hovered
                    ? Fugui.Themes.GetColorU32(FuColors.TitleBgActive, 1f)
                    : Fugui.Themes.GetColorU32(FuColors.TitleBg, 1f);
            uint border = Fugui.Themes.GetColorU32(FuColors.Border, 0.86f);
            uint separator = Fugui.Themes.GetColorU32(FuColors.Border, 0.55f);
            uint icon = Fugui.Themes.GetColorU32(FuColors.Text, hovered || active ? 0.88f : 0.62f);

            Vector2 min = headerRect.position;
            Vector2 max = headerRect.position + headerRect.size;
            drawList.AddRectFilled(min, max, bg, Mathf.Max(2f, Fugui.Themes.WindowRounding));
            drawList.AddLine(new Vector2(min.x, max.y - scale), new Vector2(max.x, max.y - scale), separator, Mathf.Max(1f, scale));
            drawList.AddRect(floatingRoot.Rect.position, floatingRoot.Rect.position + floatingRoot.Rect.size, border, Mathf.Max(2f, Fugui.Themes.WindowRounding), ImDrawFlags.None, Mathf.Max(1f, Fugui.Themes.WindowBorderSize));

            float logoSize = Mathf.Max(16f * scale, headerRect.height - 8f * scale);
            Rect logoRect = new Rect(
                headerRect.x + 8f * scale,
                headerRect.y + (headerRect.height - logoSize) * 0.5f,
                logoSize,
                logoSize);
            DrawDockRootGrabLogo(drawList, logoRect, icon);

            DrawDockRootExplodeButton(drawList, explodeRect, explodeHovered, explodeActive);
#if FU_EXTERNALIZATION
            if (externalNativeRoot && container is FuExternalWindowContainer externalContainer)
            {
                DrawExternalDockRootWindowControls(drawList, externalContainer, headerRect);
            }
#endif
        }

        private void DrawDockRootGrabLogo(ImDrawListPtr drawList, Rect rect, uint color)
        {
            float scale = Fugui.CurrentContext != null ? Fugui.CurrentContext.Scale : Fugui.Scale;
            float radius = Mathf.Max(1.1f, 1.35f * scale);
            float step = Mathf.Max(4.5f, 5f * scale);
            Vector2 center = rect.center;
            for (int y = -1; y <= 1; y++)
            {
                drawList.AddCircleFilled(center + new Vector2(-step * 0.5f, y * step), radius, color, 10);
                drawList.AddCircleFilled(center + new Vector2(step * 0.5f, y * step), radius, color, 10);
            }
        }

        private void DrawDockRootExplodeButton(ImDrawListPtr drawList, Rect rect, bool hovered, bool active)
        {
            float scale = Fugui.CurrentContext != null ? Fugui.CurrentContext.Scale : Fugui.Scale;
            uint bg = active
                ? Fugui.Themes.GetColorU32(FuColors.ButtonActive, 0.95f)
                : hovered
                    ? Fugui.Themes.GetColorU32(FuColors.ButtonHovered, 0.90f)
                    : Fugui.Themes.GetColorU32(FuColors.Button, 0.34f);
            uint icon = Fugui.Themes.GetColorU32(FuColors.Text, hovered || active ? 0.95f : 0.70f);
            float rounding = Mathf.Max(3f, 4f * scale);
            drawList.AddRectFilled(rect.position, rect.position + rect.size, bg, rounding);

            Vector2 c = rect.center;
            float inner = Mathf.Max(2.5f, 3f * scale);
            float outer = Mathf.Max(6f, 7f * scale);
            float thickness = Mathf.Max(1f, 1.25f * scale);
            DrawExplodeRay(drawList, c, new Vector2(-inner, -inner), new Vector2(-outer, -outer), icon, thickness);
            DrawExplodeRay(drawList, c, new Vector2(inner, -inner), new Vector2(outer, -outer), icon, thickness);
            DrawExplodeRay(drawList, c, new Vector2(-inner, inner), new Vector2(-outer, outer), icon, thickness);
            DrawExplodeRay(drawList, c, new Vector2(inner, inner), new Vector2(outer, outer), icon, thickness);
        }

        private void DrawExplodeRay(ImDrawListPtr drawList, Vector2 center, Vector2 inner, Vector2 outer, uint color, float thickness)
        {
            Vector2 start = center + inner;
            Vector2 end = center + outer;
            drawList.AddLine(start, end, color, thickness);
            Vector2 direction = (outer - inner).normalized;
            Vector2 side = new Vector2(-direction.y, direction.x);
            float arrow = Mathf.Max(2f, thickness * 2f);
            drawList.AddLine(end, end - direction * arrow + side * arrow * 0.65f, color, thickness);
            drawList.AddLine(end, end - direction * arrow - side * arrow * 0.65f, color, thickness);
        }

        private Rect GetFloatingDockRootExplodeButtonRect(Rect headerRect, bool externalNativeRoot)
        {
            float scale = Fugui.CurrentContext != null ? Fugui.CurrentContext.Scale : Fugui.Scale;
            float size = GetFloatingDockRootHeaderButtonSize(headerRect.height);
            float right = headerRect.xMax - 6f * scale;
            if (externalNativeRoot)
            {
                right -= GetExternalNativeWindowControlsWidth(headerRect.height);
            }

            return new Rect(
                right - size,
                headerRect.y + (headerRect.height - size) * 0.5f,
                size,
                size);
        }

        private float GetFloatingDockRootHeaderRightReserve(bool externalNativeRoot)
        {
            float scale = Fugui.CurrentContext != null ? Fugui.CurrentContext.Scale : Fugui.Scale;
            float headerHeight = GetFloatingDockRootHeaderHeight();
            float reserve = GetFloatingDockRootHeaderButtonSize(headerHeight) + 14f * scale;
            if (externalNativeRoot)
            {
                reserve += GetExternalNativeWindowControlsWidth(headerHeight);
            }

            return reserve;
        }

        private float GetFloatingDockRootHeaderButtonSize(float headerHeight)
        {
            float scale = Fugui.CurrentContext != null ? Fugui.CurrentContext.Scale : Fugui.Scale;
            return Mathf.Max(18f * scale, headerHeight - 7f * scale);
        }

        private float GetExternalNativeWindowControlsWidth(float headerHeight)
        {
            float scale = Fugui.CurrentContext != null ? Fugui.CurrentContext.Scale : Fugui.Scale;
            return GetExternalNativeWindowControlButtonWidth(headerHeight) * 3f + 4f * scale;
        }

        private float GetExternalNativeWindowControlButtonWidth(float headerHeight)
        {
            float scale = Fugui.CurrentContext != null ? Fugui.CurrentContext.Scale : Fugui.Scale;
            return Mathf.Max(26f * scale, headerHeight);
        }

#if FU_EXTERNALIZATION
        private void DrawExternalDockRootWindowControls(ImDrawListPtr drawList, FuExternalWindowContainer externalContainer, Rect headerRect)
        {
            if (externalContainer == null || externalContainer.Context is not FuExternalContext externalContext || externalContext.Window == null)
            {
                return;
            }

            FuExternalWindow externalWindow = externalContext.Window;
            FuMouseState mouse = externalContainer.Mouse;
            Vector2 mousePos = externalContainer.LocalMousePos;
            bool mouseDown = mouse != null && mouse.IsPressed(FuMouseButton.Left);
            bool mouseClicked = mouse != null && mouse.IsClicked(FuMouseButton.Left);
            float scale = Fugui.CurrentContext != null ? Fugui.CurrentContext.Scale : Fugui.Scale;
            float buttonWidth = GetExternalNativeWindowControlButtonWidth(headerRect.height);
            float iconSize = Mathf.Max(12f * scale, headerRect.height - 12f * scale);
            float iconPadding = Mathf.Max(4f * scale, (headerRect.height - iconSize) * 0.5f);
            float thickness = Mathf.Max(1f, 1.15f * scale);
            float startX = headerRect.xMax - buttonWidth * 3f - 4f * scale;
            uint iconColor = Fugui.Themes.GetColorU32(FuColors.Text, 0.86f);

            Rect minimizeRect = new Rect(startX, headerRect.y, buttonWidth, headerRect.height);
            DrawExternalHeaderButtonBackground(drawList, minimizeRect, minimizeRect.Contains(mousePos), mouseDown);
            Vector2 minLineA = new Vector2(minimizeRect.x + iconPadding, minimizeRect.y + minimizeRect.height * 0.66f);
            Vector2 minLineB = new Vector2(minimizeRect.xMax - iconPadding, minimizeRect.y + minimizeRect.height * 0.66f);
            drawList.AddLine(minLineA, minLineB, iconColor, thickness);
            if (minimizeRect.Contains(mousePos) && mouseClicked)
            {
                externalWindow.Minimize();
            }

            Rect maximizeRect = new Rect(startX + buttonWidth, headerRect.y, buttonWidth, headerRect.height);
            bool maximizeHovered = maximizeRect.Contains(mousePos);
            DrawExternalHeaderButtonBackground(drawList, maximizeRect, maximizeHovered, mouseDown);
            DrawExternalHeaderMaximizeIcon(drawList, maximizeRect, externalWindow.IsMaximized, iconColor, iconPadding, thickness);
            if (maximizeHovered && mouseClicked)
            {
                externalWindow.ToggleMaximize();
            }

            Rect closeRect = new Rect(startX + buttonWidth * 2f, headerRect.y, buttonWidth, headerRect.height);
            bool closeHovered = closeRect.Contains(mousePos);
            DrawExternalHeaderButtonBackground(drawList, closeRect, closeHovered, mouseDown);
            Vector2 closeA = new Vector2(closeRect.x + iconPadding, closeRect.y + iconPadding);
            Vector2 closeB = new Vector2(closeRect.xMax - iconPadding, closeRect.yMax - iconPadding);
            Vector2 closeC = new Vector2(closeA.x, closeB.y);
            Vector2 closeD = new Vector2(closeB.x, closeA.y);
            drawList.AddLine(closeA, closeB, iconColor, thickness);
            drawList.AddLine(closeC, closeD, iconColor, thickness);
            if (closeHovered && mouseClicked)
            {
                externalWindow.Close(null);
            }
        }

        private void DrawExternalHeaderButtonBackground(ImDrawListPtr drawList, Rect rect, bool hovered, bool mouseDown)
        {
            if (!hovered)
            {
                return;
            }

            uint bg = mouseDown
                ? Fugui.Themes.GetColorU32(FuColors.ButtonActive, 0.88f)
                : Fugui.Themes.GetColorU32(FuColors.ButtonHovered, 0.72f);
            drawList.AddRectFilled(rect.position, rect.position + rect.size, bg, 0f);
        }

        private void DrawExternalHeaderMaximizeIcon(ImDrawListPtr drawList, Rect rect, bool restoredIcon, uint color, float padding, float thickness)
        {
            float rounding = Mathf.Max(1f, Fugui.Themes.FrameRounding * 0.5f);
            if (!restoredIcon)
            {
                drawList.AddRect(
                    rect.position + new Vector2(padding, padding),
                    rect.position + rect.size - new Vector2(padding, padding),
                    color,
                    rounding,
                    ImDrawFlags.None,
                    thickness);
                return;
            }

            float offset = Mathf.Max(2f, padding * 0.45f);
            Rect back = new Rect(
                rect.x + padding + offset,
                rect.y + padding,
                rect.width - padding * 2f - offset,
                rect.height - padding * 2f - offset);
            Rect front = new Rect(
                rect.x + padding,
                rect.y + padding + offset,
                rect.width - padding * 2f - offset,
                rect.height - padding * 2f - offset);
            drawList.AddRect(back.position, back.position + back.size, color, rounding, ImDrawFlags.None, thickness);
            drawList.AddRectFilled(front.position, front.position + front.size, Fugui.Themes.GetColorU32(FuColors.TitleBg), rounding);
            drawList.AddRect(front.position, front.position + front.size, color, rounding, ImDrawFlags.None, thickness);
        }

        internal float GetExternalNativeDragHeaderHeight(FuWindow window)
        {
            if (window != null && IsWindowInExternalNativeDockRoot(window))
            {
                return GetFloatingDockRootHeaderHeight();
            }

            return window != null ? window.GetExternalNativeTitleBarHeight() : 0f;
        }

        internal float GetExternalNativeDragHeaderRightReserve(FuWindow window)
        {
            float scale = Fugui.CurrentContext != null ? Fugui.CurrentContext.Scale : Fugui.Scale;
            if (window != null && IsWindowInExternalNativeDockRoot(window))
            {
                return GetFloatingDockRootHeaderRightReserve(true);
            }

            return 64f * scale;
        }
#endif

        private void ExplodeFloatingDockRoot(FloatingDockRoot floatingRoot, IFuWindowContainer container)
        {
            if (floatingRoot == null || floatingRoot.Layout == null || container == null)
            {
                return;
            }

            List<string> windowIds = GetRuntimeWindowIdsInNodeTree(floatingRoot.Layout);
            if (windowIds.Count == 0)
            {
                return;
            }

            Rect contentRect = GetFloatingDockRootContentRect(floatingRoot);
#if FU_EXTERNALIZATION
            if (container is FuExternalWindowContainer externalContainer)
            {
                ExplodeExternalFloatingDockRoot(floatingRoot, externalContainer, windowIds, contentRect);
                return;
            }
#endif

            RemoveFloatingDockRootState(floatingRoot);

            float scale = Fugui.CurrentContext != null ? Fugui.CurrentContext.Scale : Fugui.Scale;
            float cascade = Mathf.Max(22f, 28f * scale);
            Vector2 baseSize = new Vector2(
                Mathf.Min(Mathf.Max(1f, contentRect.width), Mathf.Max(160f * scale, contentRect.width * 0.72f)),
                Mathf.Min(Mathf.Max(1f, contentRect.height), Mathf.Max(120f * scale, contentRect.height * 0.78f)));

            for (int i = 0; i < windowIds.Count; i++)
            {
                if (!Fugui.UIWindows.TryGetValue(windowIds[i], out FuWindow window) || window == null)
                {
                    continue;
                }

                bool wasDocked = window.IsDocked;
                window.CurrentDockID = 0u;
                window.IsDocked = false;
                Vector2 position = contentRect.position + new Vector2(i * cascade, i * cascade);
                Rect rect = FitWindowRectInsideContainer(new Rect(position, baseSize), container);
                window.ApplyProgrammaticRect(
                    new Vector2Int(Mathf.RoundToInt(rect.x), Mathf.RoundToInt(rect.y)),
                    new Vector2Int(Mathf.Max(1, Mathf.RoundToInt(rect.width)), Mathf.Max(1, Mathf.RoundToInt(rect.height))),
                    true,
                    true);
                if (wasDocked)
                {
                    window.Fire_OnUnDock();
                }
                window.ForceDraw(2);
            }

            RebuildNodeIndex();
            BringContainerWindowsToFront(container, windowIds);
            Fugui.ForceDrawAllWindows(2);
        }

        private void RemoveFloatingDockRootState(FloatingDockRoot floatingRoot)
        {
            if (floatingRoot == null)
            {
                return;
            }

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
        }

#if FU_EXTERNALIZATION
        private void ExplodeExternalFloatingDockRoot(FloatingDockRoot floatingRoot, FuExternalWindowContainer sourceContainer, List<string> windowIds, Rect contentRect)
        {
            if (floatingRoot == null || sourceContainer == null || windowIds == null || windowIds.Count == 0)
            {
                return;
            }

            RemoveFloatingDockRootState(floatingRoot);

            float scale = Fugui.CurrentContext != null ? Fugui.CurrentContext.Scale : Fugui.Scale;
            float cascade = Mathf.Max(22f, 28f * scale);
            Vector2 baseSize = new Vector2(
                Mathf.Min(Mathf.Max(1f, contentRect.width), Mathf.Max(160f * scale, contentRect.width * 0.72f)),
                Mathf.Min(Mathf.Max(1f, contentRect.height), Mathf.Max(120f * scale, contentRect.height * 0.78f)));
            List<FuWindow> windowsToExternalize = new List<FuWindow>();

            for (int i = 0; i < windowIds.Count; i++)
            {
                if (!Fugui.UIWindows.TryGetValue(windowIds[i], out FuWindow window) || window == null)
                {
                    continue;
                }

                bool wasDocked = window.IsDocked;
                window.CurrentDockID = 0u;
                window.IsDocked = false;
                window.IsDragging = false;
                Vector2 position = contentRect.position + new Vector2(i * cascade, i * cascade);
                Rect rect = FitWindowRectInsideContainer(new Rect(position, baseSize), sourceContainer);
                window.ApplyProgrammaticRect(
                    new Vector2Int(Mathf.RoundToInt(rect.x), Mathf.RoundToInt(rect.y)),
                    new Vector2Int(Mathf.Max(1, Mathf.RoundToInt(rect.width)), Mathf.Max(1, Mathf.RoundToInt(rect.height))),
                    true,
                    true);
                if (wasDocked)
                {
                    window.Fire_OnUnDock();
                }
                window.ForceDraw(2);
                windowsToExternalize.Add(window);
            }

            RebuildNodeIndex();
            for (int i = 0; i < windowsToExternalize.Count; i++)
            {
                Fugui.ExternalizeWindow(windowsToExternalize[i], true);
            }

            if (sourceContainer.Windows.Count == 0 && sourceContainer.Context is FuExternalContext sourceContext)
            {
                sourceContext.Window.Close(null);
            }

            Fugui.ForceDrawAllWindows(2);
        }
#endif

        private Rect FitWindowRectInsideContainer(Rect rect, IFuWindowContainer container)
        {
            if (container == null)
            {
                return rect;
            }

            Vector2 size = new Vector2(
                Mathf.Clamp(rect.width, 1f, Mathf.Max(1, container.Size.x)),
                Mathf.Clamp(rect.height, 1f, Mathf.Max(1, container.Size.y)));
            Vector2 position = rect.position;
            position.x = Mathf.Clamp(position.x, 0f, Mathf.Max(0f, container.Size.x - size.x));
            position.y = Mathf.Clamp(position.y, 0f, Mathf.Max(0f, container.Size.y - size.y));
            return new Rect(position, size);
        }

        /// <summary>
        /// Clamp a floating dock root so it stays fully visible in the main container.
        /// </summary>
        private Rect FitFloatingDockRootInsideContainer(Rect rect, IFuWindowContainer container)
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
        /// Move a floating dock root only enough to keep part of its tab header visible.
        /// </summary>
        private Rect ClampFloatingDockRootHeaderVisibleInContainer(Rect rect, IFuWindowContainer container)
        {
            if (container == null)
            {
                return rect;
            }

            float maxWidth = Mathf.Max(1f, container.Size.x);
            float maxHeight = Mathf.Max(1f, container.Size.y);
            float scale = Fugui.CurrentContext != null ? Fugui.CurrentContext.Scale : Fugui.Scale;
            float headerHeight = Mathf.Max(1f, GetFloatingDockRootHeaderHeight());
            float visibleWidth = Mathf.Min(Mathf.Max(72f, 96f * scale), Mathf.Max(1f, rect.width), maxWidth);
            float visibleHeight = Mathf.Min(Mathf.Max(8f, 10f * scale), headerHeight, maxHeight);

            rect.x = Mathf.Clamp(rect.x, visibleWidth - Mathf.Max(1f, rect.width), maxWidth - visibleWidth);
            rect.y = Mathf.Clamp(rect.y, visibleHeight - headerHeight, maxHeight - visibleHeight);
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
            _nodeContentRects.Remove(sourceId);
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
            _nodeContentRects.Remove(node.ID);
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

        private FuWindow GetFirstRuntimeWindowInNodeTree(FuDockingLayoutDefinition node)
        {
            foreach (string windowId in GetRuntimeWindowIdsInNodeTree(node))
            {
                if (Fugui.UIWindows.TryGetValue(windowId, out FuWindow window))
                {
                    return window;
                }
            }

            return null;
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

        private bool NodeTreeContainsWindowId(FuDockingLayoutDefinition root, string windowId)
        {
            if (root == null || string.IsNullOrEmpty(windowId))
            {
                return false;
            }

            if (_nodeWindowIds.TryGetValue(root.ID, out List<string> windowIds) && windowIds.Contains(windowId))
            {
                return true;
            }

            if (root.Children == null)
            {
                return false;
            }

            foreach (FuDockingLayoutDefinition child in root.Children)
            {
                if (NodeTreeContainsWindowId(child, windowId))
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

#if FU_EXTERNALIZATION
        internal bool IsWindowInExternalNativeDockRoot(FuWindow window)
        {
            if (window == null)
            {
                return false;
            }

            FloatingDockRoot floatingRoot = FindFloatingDockRootForNode(GetDockNodeId(window));
            return IsExternalNativeDockRoot(floatingRoot, window.Container);
        }
#endif

        internal List<FuWindow> GetExternalizationGroup(FuWindow window)
        {
            FloatingDockRoot floatingRoot = FindFloatingDockRootForNode(GetDockNodeId(window));
            if (floatingRoot == null)
            {
                return window != null ? new List<FuWindow> { window } : new List<FuWindow>();
            }

            List<FuWindow> windows = new List<FuWindow>();
            foreach (string windowId in GetRuntimeWindowIdsInNodeTree(floatingRoot.Layout))
            {
                if (Fugui.UIWindows.TryGetValue(windowId, out FuWindow dockedWindow))
                {
                    windows.Add(dockedWindow);
                }
            }

            return windows.Count > 0 ? windows : new List<FuWindow> { window };
        }

        internal bool TryGetFloatingDockRootRect(FuWindow window, out Rect rect)
        {
            FloatingDockRoot floatingRoot = FindFloatingDockRootForNode(GetDockNodeId(window));
            if (floatingRoot != null)
            {
                rect = floatingRoot.Rect;
                return true;
            }

            rect = default;
            return false;
        }

        internal void MoveFloatingDockRootToContainer(FuWindow window, IFuWindowContainer container, Rect rect)
        {
            FloatingDockRoot floatingRoot = FindFloatingDockRootForNode(GetDockNodeId(window));
            if (floatingRoot == null || container == null)
            {
                return;
            }

            floatingRoot.Container = container;
            floatingRoot.Rect = rect;
            MoveFloatingDockRootToFront(floatingRoot, container);
            UpdateCustomLayoutRecursive(floatingRoot.Layout, GetFloatingDockRootContentRect(floatingRoot), container);
        }

        internal bool TryBeginFloatingDockRootDrag(FuWindow window, IFuWindowContainer container, Vector2Int mousePos)
        {
            return TryBeginFloatingDockRootDrag(window, container, mousePos, false);
        }

        internal bool TryBeginFloatingDockRootDrag(FuWindow window, IFuWindowContainer container, Vector2Int mousePos, bool useGlobalMouseButton)
        {
            FloatingDockRoot floatingRoot = FindFloatingDockRootForNode(GetDockNodeId(window));
            if (floatingRoot == null || container == null)
            {
                return false;
            }

            BeginFloatingDockRootDrag(floatingRoot, container, mousePos, useGlobalMouseButton);
            return true;
        }

#if FU_EXTERNALIZATION
        internal void UpdateExternalWindowDockPreview(FuWindow sourceWindow, FuExternalWindowContainer sourceContainer, FuExternalWindowContainer targetContainer, Vector2Int mousePosition)
        {
            if (sourceWindow == null || sourceContainer == null || targetContainer == null || sourceContainer == targetContainer)
            {
                ClearExternalWindowDockPreview(sourceWindow);
                return;
            }

            if (_externalDockPreview == null || _externalDockPreview.SourceWindow != sourceWindow)
            {
                _externalDockPreview = new ExternalDockPreviewState();
            }

            _externalDockPreview.SourceWindow = sourceWindow;
            _externalDockPreview.SourceContainer = sourceContainer;
            _externalDockPreview.TargetContainer = targetContainer;
            _externalDockPreview.MousePosition = mousePosition;
        }

        internal void ClearExternalWindowDockPreview(FuWindow sourceWindow)
        {
            if (_externalDockPreview == null)
            {
                return;
            }

            if (sourceWindow == null || _externalDockPreview.SourceWindow == sourceWindow)
            {
                _externalDockPreview = null;
            }
        }

        internal void DrawExternalWindowDockPreview(FuExternalWindowContainer container)
        {
            if (_externalDockPreview == null ||
                container == null ||
                _externalDockPreview.SourceWindow == null ||
                _externalDockPreview.SourceContainer == null ||
                _externalDockPreview.TargetContainer == null)
            {
                return;
            }

            if (container != _externalDockPreview.TargetContainer &&
                container != _externalDockPreview.SourceContainer)
            {
                return;
            }

            bool drawTargetContainer = container == _externalDockPreview.TargetContainer;
            if (drawTargetContainer)
            {
                UpdateAndDrawExternalDockPreviewInTargetContainer(_externalDockPreview);
                return;
            }

            DrawExternalDockPreviewInSourceContainer(_externalDockPreview);
        }

        internal bool TryDockExternalWindowOnPreview(FuWindow sourceWindow, Vector2Int mousePosition)
        {
            if (_externalDockPreview == null ||
                sourceWindow == null ||
                _externalDockPreview.SourceWindow != sourceWindow ||
                _externalDockPreview.SourceContainer == null ||
                _externalDockPreview.TargetContainer == null ||
                !_externalDockPreview.ResolvedTarget.IsValid)
            {
                ClearExternalWindowDockPreview(sourceWindow);
                return false;
            }

            FuExternalWindowContainer sourceContainer = _externalDockPreview.SourceContainer;
            FuExternalWindowContainer targetContainer = _externalDockPreview.TargetContainer;
            DockDropZone releaseZone = GetHoveredDockZone(_externalDockPreview.ZoneRects, mousePosition - targetContainer.Position);
            if (releaseZone == DockDropZone.None)
            {
                ClearExternalWindowDockPreview(sourceWindow);
                return false;
            }

            if (releaseZone != _externalDockPreview.HoveredZone)
            {
                _externalDockPreview.HoveredZone = releaseZone;
                Rect previewRect = GetDropPreviewRect(_externalDockPreview.TargetSurfaceRect, releaseZone);
                _externalDockPreview.ResolvedTarget = _externalDockPreview.ResolvedTarget.FloatingWindow != null
                    ? DockDropTarget.ForFloatingWindow(_externalDockPreview.ResolvedTarget.FloatingWindow, releaseZone, previewRect)
                    : DockDropTarget.ForNode(_externalDockPreview.ResolvedTarget.NodeId, releaseZone, previewRect);
            }

            DockDropTarget target = _externalDockPreview.ResolvedTarget;
            FloatingDockRoot floatingRoot = FindFloatingDockRootForNode(GetDockNodeId(sourceWindow));
            List<FuWindow> windowsToMove = sourceContainer.Windows.Values.ToList();
            if (windowsToMove.Count == 0)
            {
                windowsToMove.Add(sourceWindow);
            }

            bool docked = floatingRoot != null
                ? DockFloatingRootToTarget(floatingRoot, target, targetContainer)
                : DockWindowToTarget(sourceWindow, target);
            if (!docked)
            {
                ClearExternalWindowDockPreview(sourceWindow);
                return false;
            }

            foreach (FuWindow window in windowsToMove)
            {
                if (window == null)
                {
                    continue;
                }

                window.IsDragging = false;
                window.TryAddToContainer(targetContainer);
                Fugui.ExternalWindows[window.ID] = targetContainer;
            }

            if (sourceContainer.Windows.Count == 0)
            {
                ((FuExternalContext)sourceContainer.Context).Window.Close(null);
            }

            ClearExternalWindowDockPreview(sourceWindow);
            Fugui.ForceDrawAllWindows(2);
            return true;
        }

        private void UpdateAndDrawExternalDockPreviewInTargetContainer(ExternalDockPreviewState state)
        {
            Vector2Int localMouse = state.MousePosition - state.TargetContainer.Position;
            FloatingDockRoot draggedRoot = FindFloatingDockRootForNode(GetDockNodeId(state.SourceWindow));
            DockSurfaceTarget surface = FindDockSurfaceTarget(localMouse, null, draggedRoot, state.TargetContainer);
            if (!surface.IsValid)
            {
                state.ZoneRects = null;
                state.ResolvedTarget = DockDropTarget.Invalid;
                return;
            }

            state.TargetSurfaceRect = surface.Rect;
            state.ZoneRects = GetDockZoneButtonRects(surface.Rect);
            state.HoveredZone = GetHoveredDockZone(state.ZoneRects, localMouse);
            DrawDockZoneButtons(state.ZoneRects, state.HoveredZone);

            state.ResolvedTarget = state.HoveredZone == DockDropZone.None
                ? DockDropTarget.Invalid
                : surface.FloatingWindow != null
                    ? DockDropTarget.ForFloatingWindow(surface.FloatingWindow, state.HoveredZone, GetDropPreviewRect(surface.Rect, state.HoveredZone))
                    : DockDropTarget.ForNode(surface.NodeId, state.HoveredZone, GetDropPreviewRect(surface.Rect, state.HoveredZone));

            if (state.ResolvedTarget.IsValid)
            {
                DrawDockDragPreview(state.ResolvedTarget.PreviewRect);
            }
        }

        private void DrawExternalDockPreviewInSourceContainer(ExternalDockPreviewState state)
        {
            if (state.ZoneRects == null || state.ZoneRects.Count == 0)
            {
                return;
            }

            Vector2 offset = state.TargetContainer.Position - state.SourceContainer.Position;
            Dictionary<DockDropZone, Rect> transformedZoneRects = OffsetDockZoneRects(state.ZoneRects, offset);
            DrawDockZoneButtons(transformedZoneRects, state.HoveredZone);
            if (state.ResolvedTarget.IsValid)
            {
                DrawDockDragPreview(OffsetRect(state.ResolvedTarget.PreviewRect, offset));
            }
        }

        private DockDropZone GetHoveredDockZone(Dictionary<DockDropZone, Rect> zoneRects, Vector2 mousePos)
        {
            if (zoneRects == null)
            {
                return DockDropZone.None;
            }

            foreach (KeyValuePair<DockDropZone, Rect> pair in zoneRects)
            {
                if (pair.Value.Contains(mousePos))
                {
                    return pair.Key;
                }
            }

            return DockDropZone.None;
        }

        private Dictionary<DockDropZone, Rect> OffsetDockZoneRects(Dictionary<DockDropZone, Rect> zoneRects, Vector2 offset)
        {
            Dictionary<DockDropZone, Rect> result = new Dictionary<DockDropZone, Rect>();
            foreach (KeyValuePair<DockDropZone, Rect> pair in zoneRects)
            {
                result[pair.Key] = OffsetRect(pair.Value, offset);
            }
            return result;
        }

        private Rect OffsetRect(Rect rect, Vector2 offset)
        {
            return new Rect(rect.position + offset, rect.size);
        }
#endif

        /// <summary>
        /// Select and bring forward a docked window.
        /// </summary>
        internal bool ActivateDockedWindow(FuWindow window, IFuWindowContainer container)
        {
            uint nodeId = GetDockNodeId(window);
            if (window == null || nodeId == 0u)
            {
                return false;
            }

            if (_nodeWindowIds.TryGetValue(nodeId, out List<string> windowIds))
            {
                int selectedIndex = windowIds.IndexOf(window.ID);
                if (selectedIndex >= 0)
                {
                    _nodeSelectedIndices[nodeId] = selectedIndex;
                }
            }

            FloatingDockRoot floatingRoot = FindFloatingDockRootForNode(nodeId);
            if (floatingRoot != null)
            {
                MoveFloatingDockRootToFront(floatingRoot, container);
            }

            ImGui.SetWindowFocus(window.ID);
            window.ForceFocusOnNextFrame();
            window.ForceDraw(2);
            Fugui.ForceDrawAllWindows(2);
            return true;
        }

        /// <summary>
        /// Return whether this docked window owns a visible Fugui tab bar.
        /// </summary>
        internal bool HasDockedTabBar(FuWindow window)
        {
            uint nodeId = GetDockNodeId(window);
            return HasDockedTabBar(nodeId);
        }

        private bool HasDockedTabBar(uint nodeId)
        {
            return nodeId != 0u &&
                   _nodeWindowIds.TryGetValue(nodeId, out List<string> windowIds) &&
                   windowIds.Count > 1;
        }

        /// <summary>
        /// Return the custom docked tab bar height.
        /// </summary>
        internal float GetDockedTabBarHeight(FuWindow window)
        {
            return GetDockedTabBarHeight(GetDockNodeId(window));
        }

        private float GetDockedTabBarHeight(uint nodeId)
        {
            if (!HasDockedTabBar(nodeId))
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
            DrawDockNodeTabs(nodeId, window, layout);
        }

        private void DrawDockNodeTabs(uint nodeId, FuWindow ownerWindow, FuLayout layout)
        {
            if (nodeId == 0u || layout == null || !_nodeWindowIds.TryGetValue(nodeId, out List<string> windowIds) || windowIds.Count <= 1)
            {
                return;
            }

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

            ProcessDockedTabSelectionFallback(ownerWindow, nodeId, windowIds, tabBarId);
            if (ProcessDockedTabMiddleClickClose(ownerWindow, nodeId, windowIds))
            {
                return;
            }
            ProcessDockedTabDrag(ownerWindow, nodeId, windowIds);
        }

        /// <summary>
        /// Keep docked tab selection robust when the container mouse space differs from ImGui screen space.
        /// </summary>
        private void ProcessDockedTabSelectionFallback(FuWindow ownerWindow, uint nodeId, List<string> windowIds, string tabBarId)
        {
            if (ownerWindow == null ||
                ownerWindow.Container == null ||
                windowIds == null ||
                windowIds.Count == 0 ||
                string.IsNullOrEmpty(tabBarId))
            {
                return;
            }

            FuMouseState mouse = ownerWindow.Container.Mouse;
            if (mouse == null || !mouse.IsClicked(FuMouseButton.Left))
            {
                return;
            }

            Vector2Int mousePos = ownerWindow.Container.LocalMousePos;
            if (IsDockInteractionBlockedByHigherFloatingSurface(ownerWindow.Container, mousePos, FindFloatingDockRootForNode(nodeId)))
            {
                return;
            }

            if (!FuLayout.TryGetLastTabHitIndex(tabBarId, ImGui.GetMousePos(), out int hitTabIndex))
            {
                return;
            }

            hitTabIndex = Mathf.Clamp(hitTabIndex, 0, windowIds.Count - 1);
            if (_nodeSelectedIndices.TryGetValue(nodeId, out int selectedIndex) && selectedIndex == hitTabIndex)
            {
                return;
            }

            _nodeSelectedIndices[nodeId] = hitTabIndex;
            if (Fugui.UIWindows.TryGetValue(windowIds[hitTabIndex], out FuWindow selectedWindow))
            {
                selectedWindow.ForceFocusOnNextFrame();
            }
            Fugui.ForceDrawAllWindows(2);
        }

        /// <summary>
        /// Close a docked window when its tab is middle-clicked and the window allows it.
        /// </summary>
        private bool ProcessDockedTabMiddleClickClose(FuWindow ownerWindow, uint nodeId, List<string> windowIds)
        {
            if (ownerWindow == null ||
                ownerWindow.Container == null ||
                windowIds == null ||
                windowIds.Count == 0)
            {
                return false;
            }

            FuMouseState mouse = ownerWindow.Container.Mouse;
            if (mouse == null || !mouse.IsClicked(FuMouseButton.Center))
            {
                return false;
            }

            Vector2Int mousePos = ownerWindow.Container.LocalMousePos;
            if (IsDockInteractionBlockedByHigherFloatingSurface(ownerWindow.Container, mousePos, FindFloatingDockRootForNode(nodeId)))
            {
                return false;
            }

            string tabBarId = "customDockTabs" + nodeId;
            if (!FuLayout.TryGetLastTabHitIndex(tabBarId, ImGui.GetMousePos(), out int hitTabIndex))
            {
                return false;
            }

            hitTabIndex = Mathf.Clamp(hitTabIndex, 0, windowIds.Count - 1);
            if (!Fugui.UIWindows.TryGetValue(windowIds[hitTabIndex], out FuWindow tabWindow) ||
                !tabWindow.IsClosable ||
                !tabWindow.CloseOnMiddleClick)
            {
                return false;
            }

            ClearPendingTabDrag();
            tabWindow.Close();
            Fugui.ForceDrawAllWindows(2);
            return true;
        }

        /// <summary>
        /// Start dragging a floating dock root from its global header.
        /// </summary>
        private void BeginFloatingDockRootDrag(FloatingDockRoot floatingRoot, IFuWindowContainer container, Vector2Int mousePos, bool useGlobalMouseButton = false)
        {
            if (floatingRoot == null)
            {
                return;
            }

            _draggedFloatingRoot = floatingRoot;
            _floatingRootDragStartMousePos = mousePos;
            _floatingRootDragStartRect = floatingRoot.Rect;
            _floatingRootDragUsesGlobalMouseButton = useGlobalMouseButton;
            floatingRoot.HeaderActive = true;
            MoveFloatingDockRootToFront(floatingRoot, container);
            ClearPendingTabDrag();
            Fugui.ForceDrawAllWindows(2);
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
            if (IsDockInteractionBlockedByHigherFloatingSurface(ownerWindow.Container, mousePos, FindFloatingDockRootForNode(nodeId)))
            {
                if (_pendingTabDragNodeId == nodeId)
                {
                    ClearPendingTabDrag();
                }
                return;
            }

            string tabBarId = "customDockTabs" + nodeId;
            Vector2 tabMousePos = ImGui.GetMousePos();

            if (mouse.IsDown(FuMouseButton.Left) && FuLayout.TryGetLastTabHitIndex(tabBarId, tabMousePos, out int hitTabIndex))
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
                    FuWindow.InputFocusedWindow = tabWindow;
                    if (FuWindow.NbInputFocusedWindow <= 0)
                    {
                        FuWindow.NbInputFocusedWindow = 1;
                    }
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

            float dragDistance = (mousePos - _pendingTabDragStartMousePos).magnitude;
            float threshold = Mathf.Max(4f, Fugui.Settings.ClickMaxDist * Fugui.Scale);
            if (dragDistance < threshold)
            {
                return;
            }

            if (FuLayout.TryGetLastTabHitIndex(tabBarId, tabMousePos, out int hoveredTabIndex))
            {
                hoveredTabIndex = Mathf.Clamp(hoveredTabIndex, 0, windowIds.Count - 1);
                ReorderDockedTab(nodeId, windowIds, _pendingTabDragWindowId, hoveredTabIndex);
                return;
            }

            if (!Fugui.UIWindows.TryGetValue(_pendingTabDragWindowId, out FuWindow draggedWindow) || !draggedWindow.IsDockable)
            {
                ClearPendingTabDrag();
                return;
            }

            Vector2Int startMousePos = _pendingTabDragStartMousePos;
            Vector2Int startWindowPos = _pendingTabDragStartWindowPos;
            ClearPendingTabDrag();
            BeginDockedWindowDrag(draggedWindow, startMousePos, startWindowPos, mousePos);
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
            FloatingDockRoot newFloatingRoot = new FloatingDockRoot(root, rootRect, targetWindow.Container);
            _floatingDockRoots.Add(newFloatingRoot);

            if (zone == DockDropZone.Center || zone == DockDropZone.None)
            {
                RegisterWindowToDockNode(targetWindow, root);
                RegisterWindowToDockNode(draggedWindow, root);
                UpdateCustomLayoutRecursive(root, GetFloatingDockRootContentRect(newFloatingRoot), targetWindow.Container);
                return true;
            }

            FuDockingLayoutDefinition existingLeaf = CreateRuntimeDockNode("FloatingDockExisting");
            FuDockingLayoutDefinition incomingLeaf = CreateRuntimeDockNode("FloatingDockIncoming");
            ConfigureSplitNode(root, existingLeaf, incomingLeaf, zone);
            RebuildNodeIndex();

            RegisterWindowToDockNode(targetWindow, existingLeaf);
            RegisterWindowToDockNode(draggedWindow, incomingLeaf);
            UpdateCustomLayoutRecursive(root, GetFloatingDockRootContentRect(newFloatingRoot), targetWindow.Container);
            return true;
        }

        /// <summary>
        /// Dock an entire floating dock root into a resolved target.
        /// </summary>
        private bool DockFloatingRootToTarget(FloatingDockRoot floatingRoot, DockDropTarget target, IFuWindowContainer container)
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
        private bool DockFloatingRootWithFloatingWindow(FloatingDockRoot floatingRoot, FuWindow targetWindow, DockDropZone zone, IFuWindowContainer container)
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
                FloatingDockRoot newFloatingRoot = new FloatingDockRoot(root, rootRect, targetWindow.Container);
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
            FloatingDockRoot combinedRoot = new FloatingDockRoot(rootNode, rootRect, targetWindow.Container);
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
            UpdateCustomLayoutRecursive(targetNode, targetRect, window.Container);
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
        private DockDropTarget ResolveDockDropTarget(Vector2 mousePos, FloatingDockRoot draggedRoot, bool drawZones, IFuWindowContainer container)
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
        private DockSurfaceTarget FindDockSurfaceTarget(Vector2 mousePos, FuWindow draggedWindow, FloatingDockRoot draggedRoot, IFuWindowContainer container)
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
            if (draggedWindow == null || draggedWindow.Container == null)
            {
                return DockSurfaceTarget.Invalid;
            }

            return FindFrontFloatingDockSurfaceTarget(mousePos, draggedWindow.Container, draggedWindow);
        }

        /// <summary>
        /// Find the front-most floating dockable window under the pointer inside a given container.
        /// </summary>
        private DockSurfaceTarget FindFrontFloatingDockSurfaceTarget(Vector2 mousePos, IFuWindowContainer container)
        {
            return FindFrontFloatingDockSurfaceTarget(mousePos, container, null);
        }

        /// <summary>
        /// Find the front-most floating dockable window under the pointer inside a given container.
        /// </summary>
        private DockSurfaceTarget FindFrontFloatingDockSurfaceTarget(Vector2 mousePos, IFuWindowContainer container, FuWindow draggedWindow)
        {
            if (container == null)
            {
                return DockSurfaceTarget.Invalid;
            }

            List<FuWindow> windows = GetContainerWindows(container);
            for (int i = windows.Count - 1; i >= 0; i--)
            {
                FuWindow targetWindow = windows[i];
                if (targetWindow == null ||
                    targetWindow == draggedWindow ||
                    targetWindow.IsDocked ||
                    !targetWindow.IsDockable ||
                    targetWindow.Container != container ||
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

            DrawDockDragPreview(_dockDragPreviewTarget.PreviewRect);
        }

        private void DrawDockDragPreview(Rect rect)
        {
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
            if (!string.IsNullOrEmpty(_pendingTabDragWindowId) &&
                FuWindow.InputFocusedWindow != null &&
                FuWindow.InputFocusedWindow.ID == _pendingTabDragWindowId)
            {
                FuWindow.InputFocusedWindow = null;
                FuWindow.NbInputFocusedWindow = 0;
            }

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
            _floatingRootDragUsesGlobalMouseButton = false;
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
#if FU_EXTERNALIZATION
            _floatingRootResizeStartNativeWindowPosition = Vector2Int.zero;
#endif
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

            Layouts[dockingLayout.Name] = dockingLayout.Clone();
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
            Left,
            Right,
            Bottom,
            BottomLeft,
            BottomRight
        }

        private struct DockSplitterDrawData
        {
            public Rect GrabRect;
            public bool Horizontal;
            public bool Hovered;
            public bool Active;
            public float VisualThickness;
            public float Scale;
            public Vector2 MousePosition;
            public Rect ClipRect;
            public FloatingDockRoot OwningFloatingRoot;
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

#if FU_EXTERNALIZATION
        private class ExternalDockPreviewState
        {
            public FuWindow SourceWindow;
            public FuExternalWindowContainer SourceContainer;
            public FuExternalWindowContainer TargetContainer;
            public Vector2Int MousePosition;
            public Rect TargetSurfaceRect;
            public Dictionary<DockDropZone, Rect> ZoneRects;
            public DockDropZone HoveredZone;
            public DockDropTarget ResolvedTarget;
        }
#endif

        private sealed class FloatingDockRoot
        {
            public FuDockingLayoutDefinition Layout;
            public Rect Rect;
            public IFuWindowContainer Container;
            public bool HeaderHovered;
            public bool HeaderActive;
            public bool ExplodeHovered;
            public bool ExplodeActive;
            public FloatingDockRootResizeEdge ResizeEdge;
            public bool ResizeActive;

            public FloatingDockRoot(FuDockingLayoutDefinition layout, Rect rect, IFuWindowContainer container)
            {
                Layout = layout;
                Rect = rect;
                Container = container;
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
