using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Fu;
using UnityEditor;
using UnityEngine;

namespace Fu.Editor
{
    /// <summary>
    /// Unity Editor window used to author Fugui docking layouts and generated window name definitions.
    /// </summary>
    public sealed class FuguiLayoutEditorWindow : EditorWindow
    {
        #region Constants
        private const string MenuPath = "Tools/Fugui/Layout Editor";
        private const string DefaultLayoutsFolder = "Assets/StreamingAssets/Fugui/Layouts";
        private const string DefaultWindowNamesScript = "Assets/Fugui/Generated/FuWindowsNames.cs";

        private static readonly Color CanvasColor = new Color(0.13f, 0.14f, 0.16f, 1f);
        private static readonly Color NodeColor = new Color(0.21f, 0.23f, 0.27f, 1f);
        private static readonly Color NodeHoverColor = new Color(0.28f, 0.31f, 0.36f, 1f);
        private static readonly Color NodeSelectedColor = new Color(0.25f, 0.42f, 0.65f, 1f);
        private static readonly Color SeparatorColor = new Color(0.85f, 0.86f, 0.88f, 0.9f);
        #endregion

        #region State
        private readonly Dictionary<string, FuDockingLayoutDefinition> _layouts = new Dictionary<string, FuDockingLayoutDefinition>();
        private readonly List<FuWindowName> _editableWindowNames = new List<FuWindowName>();

        private Vector2 _nodePanelScroll;
        private Vector2 _windowListScroll;
        private Vector2 _windowDetailsScroll;
        private int _tab;
        private string _layoutsFolder = DefaultLayoutsFolder;
        private string _windowNamesScriptPath;
        private string _selectedLayoutKey;
        private string _layoutName = string.Empty;
        private string _status = string.Empty;
        private FuDockingLayoutDefinition _currentLayout;
        private FuDockingLayoutDefinition _selectedNode;
        private FuDockingLayoutDefinition _draggingNode;
        private int _selectedWindowIndex = -1;
        private bool _layoutDirty;
        private bool _windowNamesDirty;
        private bool _layoutFilesChangedByWindowNames;
        #endregion

        #region Unity Lifecycle
        /// <summary>
        /// Opens the Fugui layout editor window.
        /// </summary>
        [MenuItem(MenuPath, priority = 2020)]
        public static void ShowWindow()
        {
            FuguiLayoutEditorWindow window = GetWindow<FuguiLayoutEditorWindow>("Fugui Layouts");
            window.minSize = new Vector2(760f, 520f);
            window.Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Fugui Layouts");
            _windowNamesScriptPath = FindWindowNamesScriptPath();
            RefreshAll();
        }

        private void OnGUI()
        {
            DrawHeader();

            EditorGUILayout.Space(6f);
            int nextTab = GUILayout.Toolbar(_tab, new[] { "Layouts", "Window Names" }, EditorStyles.toolbarButton, GUILayout.Height(26f));
            if (nextTab != _tab)
            {
                _tab = nextTab;
                GUI.FocusControl(null);
            }

            EditorGUILayout.Space(6f);
            if (_tab == 0)
            {
                DrawLayoutsTab();
            }
            else
            {
                DrawWindowNamesTab();
            }

            DrawStatusBar();
        }
        #endregion

        #region Header
        private void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField("Fugui Layout Editor", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Create dock layouts and window names as Unity Editor assets.", EditorStyles.miniLabel);
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Refresh", GUILayout.Width(90f), GUILayout.Height(24f)))
                {
                    RefreshAll();
                }
            }
        }
        #endregion

        #region Layouts Tab
        private void DrawLayoutsTab()
        {
            DrawLayoutsToolbar();

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
                {
                    Rect canvasRect = GUILayoutUtility.GetRect(320f, 10000f, 300f, 10000f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                    DrawLayoutCanvas(canvasRect);
                }

                DrawSelectedNodePanel();
            }
        }

        private void DrawLayoutsToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUILayout.LabelField("Folder", GUILayout.Width(42f));
                _layoutsFolder = EditorGUILayout.TextField(_layoutsFolder, EditorStyles.toolbarTextField, GUILayout.MinWidth(220f));

                if (GUILayout.Button("...", EditorStyles.toolbarButton, GUILayout.Width(26f)))
                {
                    string selected = EditorUtility.OpenFolderPanel("Fugui layouts folder", Application.dataPath, string.Empty);
                    if (!string.IsNullOrEmpty(selected))
                    {
                        _layoutsFolder = AbsoluteToAssetPath(selected);
                        LoadLayouts();
                    }
                }

                GUILayout.Space(8f);
                DrawLayoutPopup();

                if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(54f)))
                {
                    CreateNewLayout();
                }

                using (new EditorGUI.DisabledScope(_currentLayout == null))
                {
                    if (GUILayout.Button("Duplicate", EditorStyles.toolbarButton, GUILayout.Width(74f)))
                    {
                        DuplicateCurrentLayout();
                    }

                    if (GUILayout.Button(_layoutDirty ? "Save *" : "Save", EditorStyles.toolbarButton, GUILayout.Width(62f)))
                    {
                        SaveCurrentLayout();
                    }

                    if (GUILayout.Button("Delete", EditorStyles.toolbarButton, GUILayout.Width(62f)))
                    {
                        DeleteCurrentLayout();
                    }
                }
            }
        }

        private void DrawLayoutPopup()
        {
            if (_layouts.Count == 0)
            {
                EditorGUILayout.LabelField("No layout", EditorStyles.toolbarPopup, GUILayout.Width(150f));
                return;
            }

            string[] names = _layouts.Keys.OrderBy(name => name).ToArray();
            int currentIndex = Mathf.Max(0, Array.IndexOf(names, _selectedLayoutKey));
            int nextIndex = EditorGUILayout.Popup(currentIndex, names, EditorStyles.toolbarPopup, GUILayout.Width(190f));

            if (nextIndex != currentIndex && nextIndex >= 0 && nextIndex < names.Length)
            {
                SelectLayout(names[nextIndex]);
            }
        }

        private void DrawLayoutCanvas(Rect canvasRect)
        {
            EditorGUI.DrawRect(canvasRect, CanvasColor);

            Rect titleRect = new Rect(canvasRect.x + 12f, canvasRect.y + 8f, canvasRect.width - 24f, 22f);
            GUI.Label(titleRect, _currentLayout == null ? "No layout selected" : _currentLayout.Name, EditorStyles.whiteBoldLabel);

            if (_currentLayout == null)
            {
                Rect emptyRect = new Rect(canvasRect.center.x - 120f, canvasRect.center.y - 18f, 240f, 36f);
                if (GUI.Button(emptyRect, "Create Layout", EditorStyles.miniButton))
                {
                    CreateNewLayout();
                }

                return;
            }

            Rect innerRect = new Rect(canvasRect.x + 14f, canvasRect.y + 38f, canvasRect.width - 28f, canvasRect.height - 52f);
            if (innerRect.width < 40f || innerRect.height < 40f)
            {
                return;
            }

            DrawNode(_currentLayout, innerRect, true);

            if (_draggingNode != null && Event.current.type == EventType.MouseUp)
            {
                _draggingNode = null;
                Event.current.Use();
            }
        }

        private void DrawNode(FuDockingLayoutDefinition node, Rect rect, bool root)
        {
            NormalizeNode(node);

            if (node.Children.Count == 2 && node.Orientation != UIDockSpaceOrientation.None)
            {
                Rect firstRect;
                Rect secondRect;
                Rect separatorRect;
                SplitRect(rect, node, out firstRect, out secondRect, out separatorRect);

                DrawNode(node.Children[0], firstRect, false);
                DrawNode(node.Children[1], secondRect, false);
                DrawSeparator(node, rect, separatorRect);

                if (root || _selectedNode == node)
                {
                    DrawNodeFrame(rect, node, root);
                }

                return;
            }

            DrawLeafNode(node, rect);
        }

        private void DrawLeafNode(FuDockingLayoutDefinition node, Rect rect)
        {
            Event current = Event.current;
            bool hovered = rect.Contains(current.mousePosition);
            bool selected = _selectedNode == node;
            Color color = selected ? NodeSelectedColor : hovered ? NodeHoverColor : NodeColor;

            EditorGUI.DrawRect(rect, color);
            Handles.DrawSolidRectangleWithOutline(rect, Color.clear, selected ? Color.white : new Color(0f, 0f, 0f, 0.65f));

            Rect nameRect = new Rect(rect.x + 8f, rect.y + 6f, rect.width - 16f, 18f);
            GUI.Label(nameRect, string.IsNullOrEmpty(node.Name) ? "Dock node" : node.Name, EditorStyles.whiteBoldLabel);

            Rect windowsRect = new Rect(rect.x + 8f, rect.y + 28f, rect.width - 16f, rect.height - 36f);
            DrawWindowChips(node, windowsRect);

            if (current.type == EventType.MouseDown && rect.Contains(current.mousePosition))
            {
                _selectedNode = node;

                if (current.button == 1)
                {
                    ShowNodeContextMenu(node);
                }

                current.Use();
                Repaint();
            }
        }

        private void DrawWindowChips(FuDockingLayoutDefinition node, Rect rect)
        {
            Dictionary<ushort, FuWindowName> names = FuWindowNameProvider.GetAllWindowNames();
            float y = rect.y;

            if (node.WindowsDefinition.Count == 0)
            {
                GUI.Label(rect, "No windows", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            foreach (ushort windowId in node.WindowsDefinition)
            {
                string label = names.TryGetValue(windowId, out FuWindowName windowName) ? windowName.Name : "Missing #" + windowId;
                Rect chipRect = new Rect(rect.x, y, Mathf.Min(rect.width, 180f), 19f);
                EditorGUI.DrawRect(chipRect, new Color(0f, 0f, 0f, 0.2f));
                GUI.Label(new Rect(chipRect.x + 6f, chipRect.y + 1f, chipRect.width - 12f, chipRect.height), label, EditorStyles.whiteMiniLabel);
                y += 22f;

                if (y > rect.yMax - 18f)
                {
                    GUI.Label(new Rect(rect.x, y, rect.width, 18f), "...", EditorStyles.whiteMiniLabel);
                    break;
                }
            }
        }

        private void DrawNodeFrame(Rect rect, FuDockingLayoutDefinition node, bool root)
        {
            Color frameColor = root ? new Color(0.65f, 0.7f, 0.78f, 0.7f) : new Color(1f, 1f, 1f, 0.28f);
            Handles.DrawSolidRectangleWithOutline(rect, Color.clear, frameColor);

            if (rect.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                _selectedNode = node;
            }
        }

        private void DrawSeparator(FuDockingLayoutDefinition node, Rect parentRect, Rect separatorRect)
        {
            Event current = Event.current;
            MouseCursor cursor = node.Orientation == UIDockSpaceOrientation.Horizontal ? MouseCursor.ResizeHorizontal : MouseCursor.ResizeVertical;
            EditorGUIUtility.AddCursorRect(separatorRect, cursor);

            if (current.type == EventType.MouseDown && current.button == 0 && separatorRect.Contains(current.mousePosition))
            {
                _draggingNode = node;
                _selectedNode = node;
                current.Use();
            }

            if (_draggingNode == node && current.type == EventType.MouseDrag)
            {
                if (node.Orientation == UIDockSpaceOrientation.Horizontal)
                {
                    node.Proportion = Mathf.InverseLerp(parentRect.xMin, parentRect.xMax, current.mousePosition.x);
                }
                else
                {
                    node.Proportion = Mathf.InverseLerp(parentRect.yMin, parentRect.yMax, current.mousePosition.y);
                }

                node.Proportion = Mathf.Clamp(node.Proportion, 0.08f, 0.92f);
                _layoutDirty = true;
                current.Use();
                Repaint();
            }

            EditorGUI.DrawRect(separatorRect, SeparatorColor);
        }

        private void SplitRect(Rect rect, FuDockingLayoutDefinition node, out Rect firstRect, out Rect secondRect, out Rect separatorRect)
        {
            float separatorSize = 6f;
            node.Proportion = Mathf.Clamp(node.Proportion <= 0f ? 0.5f : node.Proportion, 0.05f, 0.95f);

            if (node.Orientation == UIDockSpaceOrientation.Horizontal)
            {
                float split = Mathf.Round(rect.width * node.Proportion);
                firstRect = new Rect(rect.x, rect.y, split - separatorSize * 0.5f, rect.height);
                separatorRect = new Rect(rect.x + split - separatorSize * 0.5f, rect.y, separatorSize, rect.height);
                secondRect = new Rect(rect.x + split + separatorSize * 0.5f, rect.y, rect.width - split - separatorSize * 0.5f, rect.height);
            }
            else
            {
                float split = Mathf.Round(rect.height * node.Proportion);
                firstRect = new Rect(rect.x, rect.y, rect.width, split - separatorSize * 0.5f);
                separatorRect = new Rect(rect.x, rect.y + split - separatorSize * 0.5f, rect.width, separatorSize);
                secondRect = new Rect(rect.x, rect.y + split + separatorSize * 0.5f, rect.width, rect.height - split - separatorSize * 0.5f);
            }
        }

        private void DrawSelectedNodePanel()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(310f), GUILayout.ExpandHeight(true)))
            {
                EditorGUILayout.LabelField("Selected Node", EditorStyles.boldLabel);

                if (_currentLayout == null)
                {
                    EditorGUILayout.HelpBox("Create or select a layout to edit nodes.", MessageType.Info);
                    return;
                }

                if (_selectedNode == null)
                {
                    _selectedNode = _currentLayout;
                }

                _nodePanelScroll = EditorGUILayout.BeginScrollView(_nodePanelScroll);

                EditorGUI.BeginChangeCheck();
                _layoutName = EditorGUILayout.TextField("Layout", _layoutName);
                if (EditorGUI.EndChangeCheck())
                {
                    _layoutDirty = true;
                }

                EditorGUILayout.Space(8f);

                EditorGUI.BeginChangeCheck();
                _selectedNode.Name = EditorGUILayout.TextField("Name", _selectedNode.Name);
                if (EditorGUI.EndChangeCheck())
                {
                    _layoutDirty = true;
                }

                EditorGUILayout.Space(6f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Split H", GUILayout.Height(28f)))
                    {
                        SplitNode(_selectedNode, UIDockSpaceOrientation.Horizontal);
                    }

                    if (GUILayout.Button("Split V", GUILayout.Height(28f)))
                    {
                        SplitNode(_selectedNode, UIDockSpaceOrientation.Vertical);
                    }
                }

                using (new EditorGUI.DisabledScope(_selectedNode.Children.Count == 0))
                {
                    if (GUILayout.Button("Collapse Node", GUILayout.Height(24f)))
                    {
                        CollapseNode(_selectedNode);
                    }
                }

                if (_selectedNode.Children.Count == 2)
                {
                    EditorGUI.BeginChangeCheck();
                    _selectedNode.Proportion = EditorGUILayout.Slider("Split", _selectedNode.Proportion, 0.08f, 0.92f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        _layoutDirty = true;
                    }
                }

                EditorGUILayout.Space(10f);
                EditorGUILayout.LabelField("Windows", EditorStyles.boldLabel);

                if (_selectedNode.Children.Count > 0)
                {
                    EditorGUILayout.HelpBox("Assign windows on leaf nodes. Select or collapse a child node to edit assignments.", MessageType.Info);
                }
                else
                {
                    DrawWindowAssignmentList(_selectedNode);
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawWindowAssignmentList(FuDockingLayoutDefinition node)
        {
            List<FuWindowName> windows = GetLayoutAssignableWindows();
            if (windows.Count == 0)
            {
                EditorGUILayout.HelpBox("No FuWindowName found. Create one in the Window Names tab.", MessageType.Warning);
                return;
            }

            foreach (FuWindowName windowName in windows)
            {
                bool assigned = node.WindowsDefinition.Contains(windowName.ID);
                EditorGUI.BeginChangeCheck();
                bool nextAssigned = EditorGUILayout.ToggleLeft(windowName.Name + "  #" + windowName.ID, assigned);
                if (EditorGUI.EndChangeCheck())
                {
                    if (nextAssigned)
                    {
                        if (!node.WindowsDefinition.Contains(windowName.ID))
                        {
                            node.WindowsDefinition.Add(windowName.ID);
                        }
                    }
                    else
                    {
                        node.WindowsDefinition.Remove(windowName.ID);
                    }

                    _layoutDirty = true;
                    Repaint();
                }
            }
        }

        #endregion

        #region Window Names Tab
        private void DrawWindowNamesTab()
        {
            DrawWindowNamesToolbar();

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawWindowNamesList();
                DrawWindowNameDetails();
            }
        }

        private void DrawWindowNamesToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUILayout.LabelField("Script", GUILayout.Width(40f));
                _windowNamesScriptPath = EditorGUILayout.TextField(_windowNamesScriptPath, EditorStyles.toolbarTextField, GUILayout.MinWidth(280f));

                if (GUILayout.Button("...", EditorStyles.toolbarButton, GUILayout.Width(26f)))
                {
                    string selected = EditorUtility.SaveFilePanelInProject("FuWindowName script", "FuWindowsNames", "cs", "Choose the generated FuWindowName script path.");
                    if (!string.IsNullOrEmpty(selected))
                    {
                        _windowNamesScriptPath = selected;
                    }
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(54f)))
                {
                    AddWindowName();
                }

                using (new EditorGUI.DisabledScope(_selectedWindowIndex < 0 || _selectedWindowIndex >= _editableWindowNames.Count))
                {
                    if (GUILayout.Button("Delete", EditorStyles.toolbarButton, GUILayout.Width(62f)))
                    {
                        DeleteSelectedWindowName();
                    }
                }

                using (new EditorGUI.DisabledScope(!CanSaveWindowNames()))
                {
                    if (GUILayout.Button(_windowNamesDirty ? "Save *" : "Save", EditorStyles.toolbarButton, GUILayout.Width(62f)))
                    {
                        SaveWindowNamesScript();
                    }
                }
            }
        }

        private void DrawWindowNamesList()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(260f), GUILayout.ExpandHeight(true)))
            {
                EditorGUILayout.LabelField("Window Names", EditorStyles.boldLabel);
                _windowListScroll = EditorGUILayout.BeginScrollView(_windowListScroll);

                if (_editableWindowNames.Count == 0)
                {
                    EditorGUILayout.HelpBox("No editable window names found.", MessageType.Info);
                }

                for (int i = 0; i < _editableWindowNames.Count; i++)
                {
                    FuWindowName windowName = _editableWindowNames[i];
                    GUIStyle style = i == _selectedWindowIndex ? EditorStyles.toolbarButton : EditorStyles.miniButton;
                    Rect rowRect = EditorGUILayout.GetControlRect(false, 26f);
                    Color rowColor = i == _selectedWindowIndex ? NodeSelectedColor : new Color(0.19f, 0.2f, 0.23f, 1f);
                    EditorGUI.DrawRect(rowRect, rowColor);

                    if (GUI.Button(rowRect, windowName.Name + "  #" + windowName.ID, style))
                    {
                        _selectedWindowIndex = i;
                        GUI.FocusControl(null);
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawWindowNameDetails()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                EditorGUILayout.LabelField("Definition", EditorStyles.boldLabel);

                if (_selectedWindowIndex < 0 || _selectedWindowIndex >= _editableWindowNames.Count)
                {
                    EditorGUILayout.HelpBox("Select a window name or create a new one.", MessageType.Info);
                    return;
                }

                _windowDetailsScroll = EditorGUILayout.BeginScrollView(_windowDetailsScroll);
                FuWindowName selected = _editableWindowNames[_selectedWindowIndex];

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.IntField("ID", selected.ID);
                }

                EditorGUI.BeginChangeCheck();
                string name = EditorGUILayout.TextField("Name", selected.Name);
                bool autoInstantiate = EditorGUILayout.Toggle("Auto Instantiate", selected.AutoInstantiateWindowOnlayoutSet);
                int idleFps = EditorGUILayout.IntSlider("Idle FPS", selected.IdleFPS, -1, 144);

                if (EditorGUI.EndChangeCheck())
                {
                    selected.SetName(name);
                    selected.SetAutoInstantiateOnLayoutSet(autoInstantiate);
                    selected.SetIdleFPS((short)idleFps);
                    _editableWindowNames[_selectedWindowIndex] = selected;
                    _windowNamesDirty = true;
                }

                string validation = ValidateWindowName(selected, _selectedWindowIndex);
                if (!string.IsNullOrEmpty(validation))
                {
                    EditorGUILayout.HelpBox(validation, MessageType.Warning);
                }

                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField("Generated Symbol", EditorStyles.boldLabel);
                Dictionary<ushort, string> existingNames = ReadExistingMemberNames(_windowNamesScriptPath);
                string symbol = GetWindowIdentifier(selected, existingNames, new HashSet<string>());
                EditorGUILayout.SelectableLabel(symbol, EditorStyles.textField, GUILayout.Height(20f));

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawStatusBar()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 20f);
            Color color = string.IsNullOrEmpty(_status) ? new Color(0f, 0f, 0f, 0.15f) : new Color(0.13f, 0.17f, 0.2f, 1f);
            EditorGUI.DrawRect(rect, color);

            string dirty = string.Empty;
            if (_layoutDirty)
            {
                dirty += "Layout modified";
            }

            if (_windowNamesDirty)
            {
                dirty += string.IsNullOrEmpty(dirty) ? "Window names modified" : " | Window names modified";
            }

            string label = string.IsNullOrEmpty(_status) ? dirty : _status;
            if (!string.IsNullOrEmpty(label))
            {
                GUI.Label(new Rect(rect.x + 8f, rect.y + 2f, rect.width - 16f, rect.height), label, EditorStyles.miniLabel);
            }
        }

        #endregion

        #region Data Loading
        private void RefreshAll()
        {
            LoadWindowNames();
            LoadLayouts();
            _status = "Refreshed.";
        }

        private void LoadLayouts()
        {
            _layouts.Clear();

            string folder = AssetPathToAbsolute(_layoutsFolder);
            if (!Directory.Exists(folder))
            {
                _currentLayout = null;
                _selectedNode = null;
                _selectedLayoutKey = null;
                return;
            }

            foreach (string layoutName in GetIndexedLayoutNames(folder))
            {
                string filePath = Path.Combine(folder, layoutName + "." + FuDockingLayoutManager.FUGUI_DOCKING_LAYOUT_EXTENTION);
                if (!File.Exists(filePath))
                {
                    continue;
                }

                FuDockingLayoutDefinition layout = FuDockingLayoutDefinition.Deserialize(filePath);
                if (layout == null)
                {
                    continue;
                }

                NormalizeLayout(layout);
                layout.Name = string.IsNullOrEmpty(layout.Name) ? layoutName : layout.Name;
                _layouts[layoutName] = layout;
            }

            foreach (string filePath in Directory.GetFiles(folder, "*.fdl"))
            {
                string layoutName = Path.GetFileNameWithoutExtension(filePath);
                if (_layouts.ContainsKey(layoutName))
                {
                    continue;
                }

                FuDockingLayoutDefinition layout = FuDockingLayoutDefinition.Deserialize(filePath);
                if (layout == null)
                {
                    continue;
                }

                NormalizeLayout(layout);
                layout.Name = string.IsNullOrEmpty(layout.Name) ? layoutName : layout.Name;
                _layouts[layoutName] = layout;
            }

            if (!string.IsNullOrEmpty(_selectedLayoutKey) && _layouts.ContainsKey(_selectedLayoutKey))
            {
                SelectLayout(_selectedLayoutKey);
            }
            else if (_layouts.Count > 0)
            {
                SelectLayout(_layouts.Keys.OrderBy(name => name).First());
            }
            else
            {
                _currentLayout = null;
                _selectedNode = null;
                _selectedLayoutKey = null;
            }

            _layoutDirty = false;
        }

        private IEnumerable<string> GetIndexedLayoutNames(string folder)
        {
            string indexPath = Path.Combine(folder, "layouts_index.json");
            if (!File.Exists(indexPath))
            {
                return Array.Empty<string>();
            }

            try
            {
                FuLayoutIndex index = JsonUtility.FromJson<FuLayoutIndex>(File.ReadAllText(indexPath));
                return index?.Layouts?.Where(name => !string.IsNullOrEmpty(name)) ?? Array.Empty<string>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Fugui] Failed to read layouts_index.json: " + ex.Message);
                return Array.Empty<string>();
            }
        }

        private void LoadWindowNames()
        {
            FuWindowNameProvider.ClearCache();
            _editableWindowNames.Clear();
            _editableWindowNames.AddRange(FuWindowNameProvider.GetAllWindowNames()
                .Values
                .Where(windowName => !IsSystemWindowName(windowName))
                .OrderBy(windowName => windowName.ID));

            if (_editableWindowNames.Count == 0)
            {
                _selectedWindowIndex = -1;
            }
            else
            {
                _selectedWindowIndex = Mathf.Clamp(_selectedWindowIndex, 0, _editableWindowNames.Count - 1);
            }

            _windowNamesDirty = false;
        }

        private void SelectLayout(string key)
        {
            if (!_layouts.TryGetValue(key, out FuDockingLayoutDefinition layout))
            {
                return;
            }

            _selectedLayoutKey = key;
            _currentLayout = layout;
            _layoutName = key;
            _selectedNode = _currentLayout;
            NormalizeLayout(_currentLayout);
            Repaint();
        }

        #endregion

        #region Layout Operations
        private void CreateNewLayout()
        {
            string name = GetAvailableLayoutName("Layout");
            FuDockingLayoutDefinition layout = new FuDockingLayoutDefinition(name, 0);
            _layouts[name] = layout;
            SelectLayout(name);
            _layoutDirty = true;
            _status = "New layout created.";
        }

        private void DuplicateCurrentLayout()
        {
            if (_currentLayout == null)
            {
                return;
            }

            string name = GetAvailableLayoutName(_currentLayout.Name + "_Copy");
            FuDockingLayoutDefinition copy = CloneLayout(_currentLayout);
            copy.Name = name;
            _layouts[name] = copy;
            SelectLayout(name);
            _layoutDirty = true;
            _status = "Layout duplicated.";
        }

        private void SaveCurrentLayout()
        {
            if (_currentLayout == null)
            {
                return;
            }

            string normalizedName = string.IsNullOrEmpty(_layoutName) ? _currentLayout.Name : _layoutName;
            normalizedName = normalizedName.Trim();

            if (!IsValidFileName(normalizedName))
            {
                _status = "Invalid layout name.";
                return;
            }

            string folder = AssetPathToAbsolute(_layoutsFolder);
            Directory.CreateDirectory(folder);

            string previousKey = _selectedLayoutKey;
            if (!string.IsNullOrEmpty(previousKey) && previousKey != normalizedName)
            {
                string oldPath = Path.Combine(folder, previousKey + "." + FuDockingLayoutManager.FUGUI_DOCKING_LAYOUT_EXTENTION);
                if (File.Exists(oldPath))
                {
                    File.Delete(oldPath);
                }

                _layouts.Remove(previousKey);
            }

            _currentLayout.Name = normalizedName;
            NormalizeLayout(_currentLayout);
            _layouts[normalizedName] = _currentLayout;
            _selectedLayoutKey = normalizedName;
            _layoutName = normalizedName;

            string filePath = Path.Combine(folder, normalizedName + "." + FuDockingLayoutManager.FUGUI_DOCKING_LAYOUT_EXTENTION);
            File.WriteAllText(filePath, FuDockingLayoutDefinition.Serialize(_currentLayout));
            SaveLayoutsIndex(folder);
            AssetDatabase.Refresh();

            _layoutDirty = false;
            _layoutFilesChangedByWindowNames = false;
            _status = "Layout saved.";
        }

        private void DeleteCurrentLayout()
        {
            if (_currentLayout == null || string.IsNullOrEmpty(_selectedLayoutKey))
            {
                return;
            }

            if (!EditorUtility.DisplayDialog("Delete Fugui layout", "Delete layout '" + _selectedLayoutKey + "'?", "Delete", "Cancel"))
            {
                return;
            }

            string folder = AssetPathToAbsolute(_layoutsFolder);
            string filePath = Path.Combine(folder, _selectedLayoutKey + "." + FuDockingLayoutManager.FUGUI_DOCKING_LAYOUT_EXTENTION);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            _layouts.Remove(_selectedLayoutKey);
            SaveLayoutsIndex(folder);
            AssetDatabase.Refresh();

            _currentLayout = null;
            _selectedNode = null;
            _selectedLayoutKey = null;
            _layoutDirty = false;

            if (_layouts.Count > 0)
            {
                SelectLayout(_layouts.Keys.OrderBy(name => name).First());
            }

            _status = "Layout deleted.";
        }

        private void SaveLayoutsIndex(string folder)
        {
            Directory.CreateDirectory(folder);
            FuLayoutIndex index = new FuLayoutIndex
            {
                Layouts = _layouts.Keys.OrderBy(name => name).ToArray()
            };

            File.WriteAllText(Path.Combine(folder, "layouts_index.json"), JsonUtility.ToJson(index, true));
        }

        private void SplitNode(FuDockingLayoutDefinition node, UIDockSpaceOrientation orientation)
        {
            if (node == null)
            {
                return;
            }

            NormalizeNode(node);
            List<ushort> existingWindows = new List<ushort>(node.WindowsDefinition);
            node.WindowsDefinition.Clear();
            node.Children.Clear();
            node.Orientation = orientation;
            node.Proportion = 0.5f;

            string firstSuffix = orientation == UIDockSpaceOrientation.Horizontal ? "_Left" : "_Top";
            string secondSuffix = orientation == UIDockSpaceOrientation.Horizontal ? "_Right" : "_Bottom";

            FuDockingLayoutDefinition first = new FuDockingLayoutDefinition(node.Name + firstSuffix, GetNextNodeId());
            FuDockingLayoutDefinition second = new FuDockingLayoutDefinition(node.Name + secondSuffix, GetNextNodeId() + 1);
            first.WindowsDefinition.AddRange(existingWindows);

            node.Children.Add(first);
            node.Children.Add(second);
            _selectedNode = first;
            _layoutDirty = true;
            Repaint();
        }

        private void CollapseNode(FuDockingLayoutDefinition node)
        {
            if (node == null)
            {
                return;
            }

            List<ushort> windows = new List<ushort>();
            CollectWindowIds(node, windows);
            node.Children.Clear();
            node.WindowsDefinition = windows.Distinct().ToList();
            node.Orientation = UIDockSpaceOrientation.None;
            node.Proportion = 0.5f;
            _selectedNode = node;
            _layoutDirty = true;
            Repaint();
        }

        private void ShowNodeContextMenu(FuDockingLayoutDefinition node)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Split Horizontal"), false, () => SplitNode(node, UIDockSpaceOrientation.Horizontal));
            menu.AddItem(new GUIContent("Split Vertical"), false, () => SplitNode(node, UIDockSpaceOrientation.Vertical));
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Collapse"), false, () => CollapseNode(node));
            menu.ShowAsContext();
        }

        #endregion

        #region Window Name Operations
        private void AddWindowName()
        {
            ushort id = GetNextWindowId();
            FuWindowName windowName = new FuWindowName(id, "Window " + id, true, -1);
            _editableWindowNames.Add(windowName);
            _editableWindowNames.Sort((a, b) => a.ID.CompareTo(b.ID));
            _selectedWindowIndex = _editableWindowNames.FindIndex(item => item.ID == id);
            _windowNamesDirty = true;
            _status = "Window name created.";
        }

        private void DeleteSelectedWindowName()
        {
            if (_selectedWindowIndex < 0 || _selectedWindowIndex >= _editableWindowNames.Count)
            {
                return;
            }

            FuWindowName selected = _editableWindowNames[_selectedWindowIndex];
            if (!EditorUtility.DisplayDialog("Delete FuWindowName", "Delete '" + selected.Name + "' and remove it from all layouts?", "Delete", "Cancel"))
            {
                return;
            }

            _editableWindowNames.RemoveAt(_selectedWindowIndex);
            RemoveWindowFromAllLayouts(selected.ID);
            _selectedWindowIndex = Mathf.Clamp(_selectedWindowIndex, 0, _editableWindowNames.Count - 1);
            if (_editableWindowNames.Count == 0)
            {
                _selectedWindowIndex = -1;
            }

            _windowNamesDirty = true;
            _layoutFilesChangedByWindowNames = true;
            _status = "Window name deleted.";
        }

        private void SaveWindowNamesScript()
        {
            if (!CanSaveWindowNames())
            {
                return;
            }

            string path = string.IsNullOrEmpty(_windowNamesScriptPath) ? DefaultWindowNamesScript : _windowNamesScriptPath;
            path = path.Replace('\\', '/');
            string absolutePath = AssetPathToAbsolute(path);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));

            string className = Path.GetFileNameWithoutExtension(path);
            File.WriteAllText(absolutePath, GenerateWindowNamesSource(className, _editableWindowNames));

            if (_layoutFilesChangedByWindowNames)
            {
                SaveAllLayouts();
            }

            _windowNamesScriptPath = path;
            _windowNamesDirty = false;
            _layoutFilesChangedByWindowNames = false;
            FuWindowNameProvider.ClearCache();
            AssetDatabase.Refresh();
            _status = "Window names script saved. Unity will recompile the generated symbols.";
        }

        private void SaveAllLayouts()
        {
            string folder = AssetPathToAbsolute(_layoutsFolder);
            Directory.CreateDirectory(folder);

            foreach (KeyValuePair<string, FuDockingLayoutDefinition> pair in _layouts.ToList())
            {
                NormalizeLayout(pair.Value);
                string filePath = Path.Combine(folder, pair.Key + "." + FuDockingLayoutManager.FUGUI_DOCKING_LAYOUT_EXTENTION);
                File.WriteAllText(filePath, FuDockingLayoutDefinition.Serialize(pair.Value));
            }

            SaveLayoutsIndex(folder);
            _layoutDirty = false;
        }

        private bool CanSaveWindowNames()
        {
            if (string.IsNullOrEmpty(_windowNamesScriptPath))
            {
                return false;
            }

            for (int i = 0; i < _editableWindowNames.Count; i++)
            {
                if (!string.IsNullOrEmpty(ValidateWindowName(_editableWindowNames[i], i)))
                {
                    return false;
                }
            }

            return true;
        }

        private string ValidateWindowName(FuWindowName windowName, int index)
        {
            if (string.IsNullOrWhiteSpace(windowName.Name))
            {
                return "Window name cannot be empty.";
            }

            if (_editableWindowNames.Where((item, itemIndex) => itemIndex != index && item.Name == windowName.Name).Any())
            {
                return "Another window already uses this display name.";
            }

            if (_editableWindowNames.Where((item, itemIndex) => itemIndex != index && item.ID == windowName.ID).Any())
            {
                return "Another window already uses this ID.";
            }

            return string.Empty;
        }

        #endregion

        #region Source Generation
        private string GenerateWindowNamesSource(string className, List<FuWindowName> windows)
        {
            Dictionary<ushort, string> existingNames = ReadExistingMemberNames(_windowNamesScriptPath);
            HashSet<string> usedIdentifiers = new HashSet<string>();
            Dictionary<ushort, string> identifiers = new Dictionary<ushort, string>();
            StringBuilder sb = new StringBuilder();

            className = MakeIdentifier(className);
            if (string.IsNullOrEmpty(className))
            {
                className = "FuWindowsNames";
            }

            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Runtime.CompilerServices;");
            sb.AppendLine();
            sb.AppendLine("namespace Fu");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Generated Fugui window name definitions.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public class " + className + " : FuSystemWindowsNames");
            sb.AppendLine("    {");

            foreach (FuWindowName windowName in windows.OrderBy(window => window.ID))
            {
                identifiers[windowName.ID] = GetWindowIdentifier(windowName, existingNames, usedIdentifiers);
            }

            foreach (FuWindowName windowName in windows.OrderBy(window => window.ID))
            {
                string identifier = identifiers[windowName.ID];
                sb.AppendLine("        private static readonly FuWindowName _" + identifier + " = new FuWindowName(" + windowName.ID + ", \"" + EscapeString(windowName.Name) + "\", " + windowName.AutoInstantiateWindowOnlayoutSet.ToString().ToLowerInvariant() + ", " + windowName.IdleFPS + ");");
                sb.AppendLine();
                sb.AppendLine("        /// <summary>");
                sb.AppendLine("        /// Gets the " + EscapeXml(windowName.Name) + " Fugui window name.");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine("        public static FuWindowName " + identifier + " { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _" + identifier + "; }");
                sb.AppendLine();
            }

            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets all generated Fugui window names.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        /// <returns>The generated Fugui window names.</returns>");
            sb.AppendLine("        public static List<FuWindowName> GetAllWindowsNames()");
            sb.AppendLine("        {");
            sb.AppendLine("            return new List<FuWindowName>");
            sb.AppendLine("            {");

            foreach (FuWindowName windowName in windows.OrderBy(window => window.ID))
            {
                string identifier = identifiers[windowName.ID];
                sb.AppendLine("                _" + identifier + ",");
            }

            sb.AppendLine("            };");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static Dictionary<ushort, string> ReadExistingMemberNames(string assetPath)
        {
            Dictionary<ushort, string> names = new Dictionary<ushort, string>();
            if (string.IsNullOrEmpty(assetPath))
            {
                return names;
            }

            string absolutePath = AssetPathToAbsolute(assetPath);
            if (!File.Exists(absolutePath))
            {
                return names;
            }

            string source = File.ReadAllText(absolutePath);
            MatchCollection matches = Regex.Matches(source, @"private\s+static(?:\s+readonly)?\s+FuWindowName\s+_(\w+)\s*=\s*new\s+FuWindowName\s*\(\s*(\d+)");
            foreach (Match match in matches)
            {
                if (ushort.TryParse(match.Groups[2].Value, out ushort id))
                {
                    names[id] = match.Groups[1].Value;
                }
            }

            return names;
        }

        private static string GetWindowIdentifier(FuWindowName windowName, Dictionary<ushort, string> existingNames, HashSet<string> usedIdentifiers)
        {
            string identifier = existingNames.TryGetValue(windowName.ID, out string existing) ? existing : MakeIdentifier(windowName.Name);
            if (string.IsNullOrEmpty(identifier))
            {
                identifier = "Window" + windowName.ID;
            }

            if (!char.IsLetter(identifier[0]) && identifier[0] != '_')
            {
                identifier = "Window" + identifier;
            }

            string baseIdentifier = identifier;
            int suffix = 1;
            while (!usedIdentifiers.Add(identifier))
            {
                identifier = baseIdentifier + suffix;
                suffix++;
            }

            return identifier;
        }

        private static string MakeIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string[] parts = Regex.Split(value, @"[^a-zA-Z0-9_]+")
                .Where(part => !string.IsNullOrEmpty(part))
                .ToArray();

            if (parts.Length == 0)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            foreach (string part in parts)
            {
                if (part.Length == 0)
                {
                    continue;
                }

                sb.Append(char.ToUpperInvariant(part[0]));
                if (part.Length > 1)
                {
                    sb.Append(part.Substring(1));
                }
            }

            return sb.ToString();
        }

        private static string EscapeString(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string EscapeXml(string value)
        {
            return (value ?? string.Empty)
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }

        private List<FuWindowName> GetLayoutAssignableWindows()
        {
            return FuWindowNameProvider.GetAllWindowNames()
                .Values
                .Where(windowName => !IsNoneOrEditorOnlySystemWindow(windowName))
                .OrderBy(windowName => IsSystemWindowName(windowName) ? 0 : 1)
                .ThenBy(windowName => windowName.ID)
                .ToList();
        }

        #endregion

        #region Helpers
        private static bool IsSystemWindowName(FuWindowName windowName)
        {
            return windowName.ID >= ushort.MaxValue - 4;
        }

        private static bool IsNoneOrEditorOnlySystemWindow(FuWindowName windowName)
        {
            return windowName.Equals(FuSystemWindowsNames.None);
        }

        private ushort GetNextWindowId()
        {
            HashSet<ushort> usedIds = new HashSet<ushort>(_editableWindowNames.Select(windowName => windowName.ID));
            ushort id = 1;

            while (usedIds.Contains(id))
            {
                id++;
            }

            return id;
        }

        private uint GetNextNodeId()
        {
            uint max = 0;
            if (_currentLayout != null)
            {
                TraverseNodes(_currentLayout, node => max = Math.Max(max, node.ID));
            }

            return max + 1;
        }

        private string GetAvailableLayoutName(string baseName)
        {
            string name = baseName;
            int index = 1;

            while (_layouts.ContainsKey(name))
            {
                name = baseName + "_" + index;
                index++;
            }

            return name;
        }

        private void RemoveWindowFromAllLayouts(ushort windowId)
        {
            foreach (FuDockingLayoutDefinition layout in _layouts.Values)
            {
                RemoveWindowFromLayout(layout, windowId);
            }

            _layoutDirty = true;
        }

        private static void RemoveWindowFromLayout(FuDockingLayoutDefinition node, ushort windowId)
        {
            NormalizeNode(node);
            node.WindowsDefinition.Remove(windowId);

            foreach (FuDockingLayoutDefinition child in node.Children)
            {
                RemoveWindowFromLayout(child, windowId);
            }
        }

        private static void CollectWindowIds(FuDockingLayoutDefinition node, List<ushort> windows)
        {
            NormalizeNode(node);
            windows.AddRange(node.WindowsDefinition);

            foreach (FuDockingLayoutDefinition child in node.Children)
            {
                CollectWindowIds(child, windows);
            }
        }

        private static FuDockingLayoutDefinition CloneLayout(FuDockingLayoutDefinition source)
        {
            NormalizeNode(source);
            FuDockingLayoutDefinition copy = new FuDockingLayoutDefinition(source.Name, source.ID, source.Proportion, source.Orientation)
            {
                AutoHideTopBar = source.AutoHideTopBar,
                LayoutType = source.LayoutType,
                WindowsDefinition = new List<ushort>(source.WindowsDefinition),
                Children = new List<FuDockingLayoutDefinition>()
            };

            foreach (FuDockingLayoutDefinition child in source.Children)
            {
                copy.Children.Add(CloneLayout(child));
            }

            return copy;
        }

        private static void NormalizeLayout(FuDockingLayoutDefinition layout)
        {
            NormalizeNode(layout);
        }

        private static void NormalizeNode(FuDockingLayoutDefinition node)
        {
            if (node == null)
            {
                return;
            }

            if (node.Children == null)
            {
                node.Children = new List<FuDockingLayoutDefinition>();
            }

            if (node.WindowsDefinition == null)
            {
                node.WindowsDefinition = new List<ushort>();
            }

            node.Proportion = Mathf.Clamp(node.Proportion <= 0f ? 0.5f : node.Proportion, 0.05f, 0.95f);

            foreach (FuDockingLayoutDefinition child in node.Children)
            {
                NormalizeNode(child);
            }
        }

        private static void TraverseNodes(FuDockingLayoutDefinition node, Action<FuDockingLayoutDefinition> callback)
        {
            NormalizeNode(node);
            callback?.Invoke(node);

            foreach (FuDockingLayoutDefinition child in node.Children)
            {
                TraverseNodes(child, callback);
            }
        }

        private static bool IsValidFileName(string fileName)
        {
            return !string.IsNullOrWhiteSpace(fileName) && fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
        }

        private static string FindWindowNamesScriptPath()
        {
            string[] scriptGuids = AssetDatabase.FindAssets("FuWindowsNames t:MonoScript");
            foreach (string guid in scriptGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');
                if (path.EndsWith("/Runtime/Settings/FuWindowsNames.cs", StringComparison.Ordinal))
                {
                    continue;
                }

                string absolutePath = AssetPathToAbsolute(path);
                if (!File.Exists(absolutePath))
                {
                    continue;
                }

                string source = File.ReadAllText(absolutePath);
                if (source.Contains("GetAllWindowsNames") && source.Contains("FuWindowName"))
                {
                    return path;
                }
            }

            return DefaultWindowNamesScript;
        }

        private static string AssetPathToAbsolute(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return Application.dataPath;
            }

            assetPath = assetPath.Replace('\\', '/');
            if (Path.IsPathRooted(assetPath))
            {
                return assetPath;
            }

            if (assetPath == "Assets")
            {
                return Application.dataPath;
            }

            if (assetPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                return Path.GetFullPath(Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length)));
            }

            return Path.GetFullPath(assetPath);
        }

        private static string AbsoluteToAssetPath(string absolutePath)
        {
            absolutePath = absolutePath.Replace('\\', '/');
            string dataPath = Application.dataPath.Replace('\\', '/');

            if (absolutePath == dataPath)
            {
                return "Assets";
            }

            if (absolutePath.StartsWith(dataPath + "/", StringComparison.Ordinal))
            {
                return "Assets/" + absolutePath.Substring(dataPath.Length + 1);
            }

            return absolutePath;
        }
        #endregion
    }
}
