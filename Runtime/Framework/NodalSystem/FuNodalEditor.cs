using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fu.Framework.Nodal
{
    /// <summary>
    /// Fugui window hosting the nodal editor canvas (pan/zoom, nodes, edges, selection, link).
    /// Uses UnityEngine.Vector2 for all coordinates. Inputs via Window.* APIs.
    /// </summary>
    public class FuNodalEditor
    {
        #region Variables
        public FuNodalGraph Graph { get; set; }
        public bool UseBezierCurves { get; set; } = true;
        public bool ShowCompatiblesNodesOnVoidLink { get; set; } = true;
        public bool AutoLinkColorFromConvertedType { get; set; } = true;
        // View
        private Vector2 _pan = new Vector2(0f, 0f);
        private float _zoom = 1.0f;
        private float MinZoom = 0.5f;
        private float MaxZoom = 2.0f;
        private float fuScale = 1f;

        // Interaction
        private bool _isDraggingNode = false;
        private Vector2 _dragStartMouse;
        private bool _isMinimapDragging = false;
        private bool _isPanning = false;
        private Vector2 _panMouseStart;
        private Vector2 _panStart;

        // Linking
        private bool _isLinking = false;
        private (int nodeId, int portId)? _linkFrom;
        private int voidLinkFromNode;
        private FuNodalPort voidLinkFromPort;

        private Vector2 _canvasOrigin;
        private Vector2 _canvasSize;
        private (FuNode node, FuNodalPort port)? _hoveredPort = null;

        // Selection
        private int? _selectedEdgeIndex = null;
        private int? _hoveredEdgeIndex = null;
        private readonly HashSet<int> _selectedNodeIds = new HashSet<int>();
        private bool _isMarqueeSelecting = false;
        private Vector2 _marqueeStartWA, _marqueeEndWA;
        private readonly Dictionary<int, Vector2> _groupDragStartPositions = new Dictionary<int, Vector2>();
        private bool _isGroupDragging = false;

        // context menu
        private Action<FuContextMenuBuilder> OnDrawContextMenu;

        private Vector2 contextmenuOpenMousePos;
        private Dictionary<int, NodeGeom> _nodeGeometries = new Dictionary<int, NodeGeom>();
        #endregion

        public FuNodalEditor(FuNodalGraph graph, float minZoom = 0.5f, float maxZoom = 2f)
        {
            MinZoom = Mathf.Max(0.1f, minZoom);
            MaxZoom = Mathf.Max(MinZoom, Mathf.Min(4f, maxZoom));
            Graph = graph;
        }

        #region Context menu
        /// <summary>
        /// Draw the context menu for adding nodes.
        /// </summary>
        /// <param name="layout"> The FuLayout to use for ImGui calls.</param>
        /// <param name="types"> The list of node TypeIds to include in the menu.</param>
        /// <param name="drawCustom"> Whether to invoke custom menu items.</param>
        private void DrawContextMenu(FuLayout layout, IEnumerable<string> types, bool drawCustom, bool forceOpen, bool tryAutoConnect)
        {
            FuContextMenuBuilder builder = new FuContextMenuBuilder();

            // Build category tree from TypeId like "Cat/Sub/NodeName"
            var root = new MenuNode();
            foreach (var typeId in types)
            {
                var path = typeId.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (path.Length == 0) continue;

                var curr = root;
                for (int i = 0; i < path.Length - 1; i++)
                {
                    var seg = path[i].Trim();
                    if (seg.Length == 0) continue;
                    if (!curr.Children.TryGetValue(seg, out var child))
                    {
                        child = new MenuNode();
                        curr.Children[seg] = child;
                    }
                    curr = child;
                }

                curr.LeafTypes.Add(typeId);
            }

            // Emit into builder using fluent BeginChild/EndChild
            EmitSubitems(root, builder, tryAutoConnect);

            // invoke custom menu items
            if (drawCustom)
                OnDrawContextMenu?.Invoke(builder);

            Fugui.PushContextMenuItems(builder.Build());
            if (forceOpen)
            {
                contextmenuOpenMousePos = GetLocalMousePosition();
                Fugui.TryOpenContextMenu();
            }
            else
            {
                if (Fugui.TryOpenContextMenuOnWindowClick())
                {
                    contextmenuOpenMousePos = GetLocalMousePosition();
                }
            }
            Fugui.PopContextMenuItems();
        }

        /// <summary>
        /// Recursively emits submenus and items using the fluent builder (BeginChild/EndChild).
        /// </summary>
        private void EmitSubitems(MenuNode node, FuContextMenuBuilder b, bool tryAutoConnect)
        {
            // Submenus first (alpha order)
            foreach (var kv in node.Children.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            {
                b.BeginChild(kv.Key);
                EmitSubitems(kv.Value, b, tryAutoConnect);
                b.EndChild();
            }

            // Then leaf items
            foreach (var type in node.LeafTypes)
            {
                string label = type.Split('/').Last();

                b.AddItem(label, () =>
                {
                    var mouseScreen = _canvasOrigin + contextmenuOpenMousePos;
                    var pos = ScreenToCanvas(mouseScreen);
                    string typeId = type;
                    if (tryAutoConnect)
                    {
                        int index = type.LastIndexOf('(');
                        if (index >= 0)
                            typeId = type.Substring(0, index).TrimEnd();
                    }
                    var n = Graph.Registry.CreateNode(typeId, Graph);
                    n.SetPosition(pos.x, pos.y);
                    Graph.AddNode(n);

                    if (tryAutoConnect)
                    {
                        try
                        {
                            if (voidLinkFromPort != null)
                            {
                                // try get port name
                                var spl = label.Split('(');
                                string portName = spl.Length > 1 ? spl[1].TrimEnd(')') : null;
                                var toPort = n.Ports.FirstOrDefault(p => p.Value.Name.Equals(portName, StringComparison.OrdinalIgnoreCase));

                                if (toPort.Value != null)
                                {
                                    Graph.TryConnect(voidLinkFromPort, toPort.Value, voidLinkFromNode, n.Id);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Fugui.Fire_OnUIException(ex);
                        }
                        finally
                        {
                            voidLinkFromNode = 0;
                            voidLinkFromPort = null;
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Tree node for menu categories.
        /// </summary>
        private sealed class MenuNode
        {
            public Dictionary<string, MenuNode> Children { get; } = new Dictionary<string, MenuNode>(StringComparer.OrdinalIgnoreCase);
            public List<string> LeafTypes { get; } = new List<string>();
        }

        #endregion

        #region Canvas
        /// <summary>
        /// Draw the nodal canvas (pan/zoom, grid, nodes, edges, interaction).
        /// </summary>
        /// <param name="layout"> The FuLayout to use for ImGui calls.</param>
        private void DrawCanvas(FuWindow window)
        {
            _canvasOrigin = ImGui.GetCursorScreenPos();// Window.LocalPosition + Window.WorkingAreaPosition;
            _canvasSize = ImGui.GetContentRegionAvail();// Window.WorkingAreaSize;

            // Create a child region that acts as our canvas
            var avail = ImGui.GetContentRegionAvail();
            ImGui.BeginChild("NodalCanvas", avail, ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar);

            // Cache canvas origin & size AFTER BeginChild (child is now current window)
            var childPos = ImGui.GetWindowPos();
            var childSize = ImGui.GetWindowSize();

            // === Zoom (anchored under cursor) ===
            if (window.IsHoveredContent && IsMouseHoverCanvas())
            {
                float wheel = window.Mouse.Wheel.y;
                if (Mathf.Abs(wheel) > float.Epsilon)
                {
                    // souris en coords écran + relative à la zone
                    var mouseWA = GetLocalMousePosition();      // local working area
                    var mouseScreen = _canvasOrigin + mouseWA;               // écran (si tu en as besoin)
                    var mouseRel = mouseWA;                               // on reste en repère working area

                    var canvasUnderMouse = (mouseRel - _pan) / _zoom;
                    _zoom = Mathf.Clamp(_zoom * (1f + wheel * 0.1f), MinZoom, MaxZoom);
                    _pan = mouseRel - canvasUnderMouse * _zoom; // point canvas reste sous le curseur
                }

                // handle Ctrl + space = reset zoom
                if (window.Keyboard.KeyCtrl && window.Keyboard.GetKeyDown(FuKeysCode.Space))
                {
                    var mouseWA = GetLocalMousePosition();
                    var mouseRel = mouseWA;                               // on reste en repère working area
                    var canvasUnderMouse = (mouseRel - _pan) / _zoom;
                    _zoom = 1.0f;
                    _pan = mouseRel - canvasUnderMouse * _zoom;
                }
            }

            // === Pan (MMB) ===
            if (window.IsHoveredContent && IsMouseHoverCanvas() && window.Mouse.IsDown(FuMouseButton.Center))
            {
                _isPanning = true;
                _panMouseStart = GetLocalMousePosition(); // même repère
                _panStart = _pan;
            }
            if (_isPanning)
            {
                if (window.Mouse.IsPressed(FuMouseButton.Center))
                {
                    var deltaWA = GetLocalMousePosition() - _panMouseStart;
                    _pan = _panStart + deltaWA; // direction naturelle
                }
                else
                {
                    _isPanning = false;
                }
            }

            // === MARQUEE (Selection rectangle) ===
            if (window.IsHoveredContent && IsMouseHoverCanvas())
            {
                bool overNode = IsMouseHoverNode(false, out _); // any node under mouse?
                bool overPort = _hoveredPort.HasValue;

                // Start marquee on empty canvas with LMB down (no node header, no port)
                if (!_isMarqueeSelecting && !overNode && !overPort && window.Mouse.IsDown(FuMouseButton.Left))
                {
                    _isMarqueeSelecting = true;
                    _marqueeStartWA = GetLocalMousePosition();
                    _marqueeEndWA = _marqueeStartWA;
                }
            }

            // Update marquee while pressed; draw the rectangle
            if (_isMarqueeSelecting)
            {
                if (window.Mouse.IsPressed(FuMouseButton.Left))
                {
                    _marqueeEndWA = GetLocalMousePosition();

                    // Draw rectangle in screen space (convert WA → screen by + origin)
                    Vector2 aWA = _marqueeStartWA;
                    Vector2 bWA = _marqueeEndWA;
                    Vector2 minWA = new Vector2(Mathf.Min(aWA.x, bWA.x), Mathf.Min(aWA.y, bWA.y));
                    Vector2 maxWA = new Vector2(Mathf.Max(aWA.x, bWA.x), Mathf.Max(aWA.y, bWA.y));
                    Vector2 minScreen = _canvasOrigin + minWA;
                    Vector2 maxScreen = _canvasOrigin + maxWA;

                    uint fillCol = ImGui.ColorConvertFloat4ToU32(new Color(0.2f, 0.6f, 1f, 0.15f));
                    uint borderCol = ImGui.ColorConvertFloat4ToU32(new Color(0.2f, 0.6f, 1f, 0.8f));
                    var dl = ImGui.GetWindowDrawList();
                    dl.AddRectFilled(minScreen, maxScreen, fillCol, 2f);
                    dl.AddRect(minScreen, maxScreen, borderCol, 2f, ImDrawFlags.RoundCornersAll, 1.5f);
                }
                else
                {
                    // Finalize marquee on release
                    bool additive = window.Keyboard.KeyShift;
                    MarqueeSelect(additive);
                    _isMarqueeSelecting = false;
                }
            }

            // Draw grid
            var drawList = ImGui.GetWindowDrawList();
            DrawGrid(drawList, _canvasOrigin, _canvasSize, _pan, _zoom);

            // Draw edges (under nodes)
            _hoveredEdgeIndex = null;
            for (int i = 0; i < Graph.Edges.Count; i++)
            {
                var e = Graph.Edges[i];

                if (!IsInCanvasFostrum(e))
                    continue;

                var from = Graph.GetNode(e.FromNodeId);
                var to = Graph.GetNode(e.ToNodeId);
                if (from == null || to == null) continue;

                var pFrom = from.GetPort(e.FromPortId);
                var pTo = to.GetPort(e.ToPortId);
                if (pFrom == null || pTo == null) continue;

                Vector2 a = GetPortAnchorScreen(from, pFrom);
                Vector2 b = GetPortAnchorScreen(to, pTo);

                // Check mouse hover
                Vector2 mouse = _canvasOrigin + GetLocalMousePosition();
                bool hovered = false;
                if (UseBezierCurves)
                    hovered = IsMouseNearBezier(mouse, a, b, 8f); // 8px threshold
                else
                    hovered = IsMouseNearStraight(mouse, a, b, 8f);

                // Color from port type
                bool selected = (_selectedEdgeIndex == i);

                // start Color
                uint startCol = selected ? Fugui.Themes.GetColorU32(FuColors.NodeSelectedEdge) :
                           (hovered ? Fugui.Themes.GetColorU32(FuColors.NodeHoveredEdge) : Fugui.Themes.GetColorU32(FuColors.NodeEdge));
                var typeData = Graph.Registry.GetType(pFrom.DataType);
                if (typeData != null && typeData.Color.HasValue)
                {
                    startCol = ImGui.GetColorU32(typeData.Color.Value);
                }
                // end Color
                uint endCol = selected ? Fugui.Themes.GetColorU32(FuColors.NodeSelectedEdge) :
                           (hovered ? Fugui.Themes.GetColorU32(FuColors.NodeHoveredEdge) : Fugui.Themes.GetColorU32(FuColors.NodeEdge));
                var convertedType = to.GetCurrentConvertedType(pTo);
                var toTypeData = Graph.Registry.GetType(convertedType);
                if (toTypeData != null && toTypeData.Color.HasValue)
                {
                    endCol = ImGui.GetColorU32(toTypeData.Color.Value);
                }

                float width = selected ? 4.0f : (hovered ? 3.0f : 2.0f);
                if (UseBezierCurves)
                    DrawBezier(drawList, a, b, width, startCol, endCol);
                else
                    DrawStraightConnection(drawList, a, b, width, startCol, endCol);

                if (hovered)
                {
                    _hoveredEdgeIndex = i;
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }
            }

            // Sélection / suppression de liens
            if (_hoveredEdgeIndex.HasValue && window.Mouse.IsClicked(FuMouseButton.Left))
            {
                _selectedEdgeIndex = _hoveredEdgeIndex;
            }
            else if (window.Mouse.IsClicked(FuMouseButton.Left) && !_hoveredEdgeIndex.HasValue)
            {
                _selectedEdgeIndex = null; // clic vide → désélection
            }

            // Delete selected edge
            if (_selectedEdgeIndex.HasValue && ImGui.IsKeyPressed(ImGuiKey.Delete))
            {
                Graph.DeleteEdge(Graph.Edges[_selectedEdgeIndex.Value]);
                _selectedEdgeIndex = null;
            }

            // Delete selected nodes (and their edges) with DEL
            if (_selectedNodeIds.Count > 0 && ImGui.IsKeyPressed(ImGuiKey.Delete))
            {
                // Copy to avoid modifying while iterating
                var toDelete = Graph.Nodes.Where(n => _selectedNodeIds.Contains(n.Id)).ToList();
                foreach (var n in toDelete)
                {
                    Graph.DeleteNode(n.Id);
                }
                _selectedNodeIds.Clear();
            }

            // Draw nodes (on top)
            _hoveredPort = null;
            fuScale = Fugui.Scale;
            Fugui.CurrentContext.SetTempFakeScale(fuScale * _zoom); // scale globale pour les nodes
            for (int i = 0; i < Graph.Nodes.Count; i++)
            {
                if (!IsInCanvasFostrum(Graph.Nodes[i]))
                    continue;
                DrawNode(drawList, window, Graph.Nodes[i]);
            }
            Fugui.CurrentContext.SetTempFakeScale(fuScale); // reset scale

            // Preview link
            if (_isLinking && _linkFrom.HasValue)
            {
                var fromNode = Graph.GetNode(_linkFrom.Value.nodeId);
                var fromPort = fromNode.GetPort(_linkFrom.Value.portId);
                if (fromNode != null && fromPort != null)
                {
                    Vector2 aPrev = GetPortAnchorScreen(fromNode, fromPort);
                    Vector2 bPrev = _canvasOrigin + GetLocalMousePosition();
                    uint col = Fugui.Themes.GetColorU32(FuColors.NodeLinkPreview);
                    if (UseBezierCurves)
                        DrawBezier(drawList, aPrev, bPrev, 1.5f, col, col);
                    else
                        DrawStraightConnection(drawList, aPrev, bPrev, 1.5f, col, col);
                }
            }

            // Fin du lien sur Up (peu importe où on relâche, TryFinishLink checkera la cible)
            if (_isLinking && _linkFrom.HasValue && window.Mouse.IsUp(FuMouseButton.Left))
            {
                TryFinishLink();
                _isLinking = false;
                _linkFrom = null;
            }

            // Deselect on empty click (no marquee started)
            if (window.IsHoveredContent && window.Mouse.IsClicked(FuMouseButton.Left) && !_isMarqueeSelecting)
            {
                if (!IsMouseHoverNode(false, out _))
                    ClearSelection();
            }

            ImGui.EndChild();
        }

        /// <summary>
        /// Draw the background grid.
        /// </summary>
        /// <param name="dl"> The ImDrawListPtr to use for drawing.</param>
        /// <param name="origin"> Top-left corner of the canvas in screen coordinates.</param>
        /// <param name="size"> Size of the canvas in screen coordinates.</param>
        /// <param name="pan"> Current pan offset in screen coordinates.</param>
        /// <param name="zoom"> Current zoom factor (1.0 = 100%).</param>
        private void DrawGrid(ImDrawListPtr dl, Vector2 origin, Vector2 size, Vector2 pan, float zoom)
        {
            const float baseStep = 64f;

            // screen-space step (same X & Y => squares)
            float step = Mathf.Max(4f, baseStep * zoom);
            while (step < 40f) step *= 2f;
            while (step > 120f) step *= 0.5f;

            int majorEvery = 5;
            float endX = origin.x + size.x;
            float endY = origin.y + size.y;

            uint _colGridMinor = Fugui.Themes.GetColorU32(FuColors.NodeGridMinor);
            uint _colGridMajor = Fugui.Themes.GetColorU32(FuColors.NodeGridMajor);

            // ===== Vertical lines =====
            // Start from the first k whose screen-x is just before the left edge
            int kx = Mathf.FloorToInt((-pan.x) / step) - 1;

            for (; ; kx++)
            {
                float x = origin.x + pan.x + kx * step; // <-- pan in the same sign as your content
                if (x > endX + step * 0.5f) break;

                float px = Mathf.Floor(x) + 0.5f; // pixel snap
                bool isMajor = PosMod(kx, majorEvery) == 0;
                dl.AddLine(new Vector2(px, origin.y), new Vector2(px, endY), isMajor ? _colGridMajor : _colGridMinor, 1f);
            }

            // ===== Horizontal lines =====
            int ky = Mathf.FloorToInt((-pan.y) / step) - 1;

            for (; ; ky++)
            {
                float y = origin.y + pan.y + ky * step;
                if (y > endY + step * 0.5f) break;

                float py = Mathf.Floor(y) + 0.5f;
                bool isMajor = PosMod(ky, majorEvery) == 0;
                dl.AddLine(new Vector2(origin.x, py), new Vector2(endX, py), isMajor ? _colGridMajor : _colGridMinor, 1f);
            }
        }
        private static int PosMod(int a, int m) => (a % m + m) % m;

        /// <summary>
        /// Draw a cubic Bezier curve between two points with control points.
        /// </summary>
        /// <param name="dl"> The ImDrawListPtr to use for drawing.</param>
        /// <param name="a"> Start point.</param>
        /// <param name="b"> End point.</param>
        /// <param name="thickness"> Line thickness.</param>
        /// <param name="startCol"> Line color as a packed uint.</param>
        private void DrawBezier(ImDrawListPtr dl, Vector2 a, Vector2 b, float thickness, uint startCol, uint endCol)
        {
            // Snap anchor points as knobs are centered on half-pixels
            a = new Vector2(Mathf.Floor(a.x) + 0.5f, Mathf.Floor(a.y) + 0.5f);
            b = new Vector2(Mathf.Floor(b.x) + 0.5f, Mathf.Floor(b.y) + 0.5f);

            float dx = Mathf.Abs(b.x - a.x);
            Vector2 c0 = new Vector2(a.x + dx * 0.5f, a.y);
            Vector2 c1 = new Vector2(b.x - dx * 0.5f, b.y);

            // First pass: rough length estimation to choose step count
            int lenSamples = 16;
            Vector2 prev = a;
            float approxLen = 0f;
            for (int i = 1; i <= lenSamples; i++)
            {
                float t = i / (float)lenSamples;
                Vector2 p = CubicBezierPoint(a, c0, c1, b, t);
                approxLen += Vector2.Distance(prev, p);
                prev = p;
            }

            int steps = Mathf.Max(2, (int)(approxLen / 6f)); // ~1 segment per ~6 px
            float w = thickness * _zoom;

            Vector2 p0 = a;
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector2 p1 = CubicBezierPoint(a, c0, c1, b, t);
                uint col = Fugui.LerpColor(startCol, endCol, t);
                dl.AddLine(p0, p1, col, w);
                p0 = p1;
            }
        }

        /// <summary>
        /// Evaluate a cubic Bezier curve at parameter t [0..1].
        /// </summary>
        /// <param name="a"> Start point.</param>
        /// <param name="c0"> First control point.</param>
        /// <param name="c1"> Second control point.</param>
        /// <param name="b"> End point.</param>
        /// <param name="t"> Parameter [0..1].</param>
        /// <returns> Point on the curve at t.</returns>
        private static Vector2 CubicBezierPoint(Vector2 a, Vector2 c0, Vector2 c1, Vector2 b, float t)
        {
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;
            return (uuu * a) + (3f * uu * t * c0) + (3f * u * tt * c1) + (ttt * b);
        }

        /// <summary>
        /// Draw a straight segmented connection between two points.
        /// </summary>
        /// <param name="dl">The ImDrawListPtr to use for drawing.</param>
        /// <param name="a">Start point (on the left).</param>
        /// <param name="b">End point (on the right).</param>
        /// <param name="thickness">Line thickness.</param>
        /// <param name="col">Line color as a packed uint.</param>
        /// <param name="segmentLength">Length of the small horizontal segments at both ends.</param>
        private void DrawStraightConnection(ImDrawListPtr dl, Vector2 a, Vector2 b, float thickness, uint colStart, uint colEnd, float segmentLength = 20f)
        {
            // Snap anchor points (knobs centered on half-pixels)
            a = new Vector2(Mathf.Floor(a.x) + 0.5f, Mathf.Floor(a.y) + 0.5f);
            b = new Vector2(Mathf.Floor(b.x) + 0.5f, Mathf.Floor(b.y) + 0.5f);

            // Scale segment length with zoom
            segmentLength *= _zoom;

            // Fixed horizontal offsets
            Vector2 aEnd = new Vector2(a.x + segmentLength, a.y);
            Vector2 bStart = new Vector2(b.x - segmentLength, b.y);

            float w = thickness * _zoom;

            // --- Small horizontal segments: solid color ---
            dl.AddLine(a, aEnd, colStart, w);
            dl.AddLine(bStart, b, colEnd, w);

            // --- Middle segment: gradient ---
            Fugui.DrawLineGradient(dl, aEnd, bStart, 0f, 1f, w, colStart, colEnd);
        }

        /// <summary>
        /// Returns true if the mouse is hovering over the canvas area.
        /// </summary>
        /// <param name="window"> The FuWindow containing the canvas.</param>
        /// <returns> True if the mouse is over the canvas, false otherwise.</returns>
        private bool IsMouseHoverCanvas()
        {
            Vector2 mousePos = GetLocalMousePosition();
            return (mousePos.x >= 0 && mousePos.y >= 0 && mousePos.x < _canvasSize.x && mousePos.y < _canvasSize.y);
        }

        /// <summary>
        /// Get the mouse position in local canvas coordinates (0,0 = top-left of canvas).
        /// </summary>
        /// <returns> The mouse position in local canvas coordinates.</returns>
        private Vector2 GetLocalMousePosition()
        {
            Vector2 mousePos = ImGui.GetMousePos();
            return mousePos - _canvasOrigin;
        }
        #endregion

        #region Fostrum culling
        /// <summary>
        /// Returns true if the node is at least partially visible in the canvas fostrum.
        /// </summary>
        /// <param name="node"> The node to test.</param>
        /// <returns> True if the node is in the canvas fostrum, false otherwise.</returns>
        private bool IsInCanvasFostrum(FuNode node)
        {
            var g = CalcNodeGeom(node);

            float padding = 10f * _zoom; // pixels around canvas to keep slightly offscreen nodes visible

            float xMin = _canvasOrigin.x - padding;
            float yMin = _canvasOrigin.y - padding;
            float xMax = xMin + _canvasSize.x + padding;
            float yMax = yMin + _canvasSize.y + padding;

            // AABB overlap test (rect-rect intersection)
            return g.rectMax.x > xMin &&
                    g.rectMin.x < xMax &&
                    g.rectMax.y > yMin &&
                    g.rectMin.y < yMax;
        }

        /// <summary>
        /// Returns true if the edge is at least partially visible in the canvas frustum.
        /// </summary>
        private bool IsInCanvasFostrum(FuNodalEdge edge)
        {
            // Get nodes
            var from = Graph.GetNode(edge.FromNodeId);
            var to = Graph.GetNode(edge.ToNodeId);
            if (from == null || to == null)
                return false;

            // If either node is visible, the edge is visible
            if (IsInCanvasFostrum(from) || IsInCanvasFostrum(to))
                return true;

            // Get edge segment
            Vector2 p1 = ScreenToCanvas(GetPortAnchorScreen(from, from.GetPort(edge.FromPortId)));
            Vector2 p2 = ScreenToCanvas(GetPortAnchorScreen(to, to.GetPort(edge.ToPortId)));

            // Define canvas rect
            float xMin = _canvasOrigin.x;
            float yMin = _canvasOrigin.y;
            float xMax = _canvasSize.x;
            float yMax = _canvasSize.y;

            // If the segment intersects the rect, it's visible
            return SegmentIntersectsRect(p1, p2, xMin, yMin, xMax, yMax);
        }

        /// <summary>
        /// Tests if a line segment intersects a rectangle.
        /// </summary>
        private bool SegmentIntersectsRect(Vector2 p1, Vector2 p2, float xMin, float yMin, float xMax, float yMax)
        {
            // Liang–Barsky or Cohen–Sutherland algorithm would be optimal,
            // but we can simplify with bounding and line-rect intersection tests.

            // 1. Quick reject if both points are outside on the same side
            if ((p1.x < xMin && p2.x < xMin) || (p1.x > xMax && p2.x > xMax) ||
                (p1.y < yMin && p2.y < yMin) || (p1.y > yMax && p2.y > yMax))
                return false;

            // 2. If any endpoint is inside → visible
            if (p1.x >= xMin && p1.x <= xMax && p1.y >= yMin && p1.y <= yMax)
                return true;
            if (p2.x >= xMin && p2.x <= xMax && p2.y >= yMin && p2.y <= yMax)
                return true;

            // 3. Test intersection with each rect edge
            Vector2 topLeft = new(xMin, yMin);
            Vector2 topRight = new(xMax, yMin);
            Vector2 bottomRight = new(xMax, yMax);
            Vector2 bottomLeft = new(xMin, yMax);

            return LineSegmentsIntersect(p1, p2, topLeft, topRight)
                || LineSegmentsIntersect(p1, p2, topRight, bottomRight)
                || LineSegmentsIntersect(p1, p2, bottomRight, bottomLeft)
                || LineSegmentsIntersect(p1, p2, bottomLeft, topLeft);
        }

        /// <summary>
        /// Returns true if two line segments intersect.
        /// </summary>
        private bool LineSegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
        {
            float o1 = Orientation(p1, p2, q1);
            float o2 = Orientation(p1, p2, q2);
            float o3 = Orientation(q1, q2, p1);
            float o4 = Orientation(q1, q2, p2);

            if (o1 != o2 && o3 != o4)
                return true; // general case

            return false;
        }

        private float Orientation(Vector2 a, Vector2 b, Vector2 c)
        {
            float val = (b.y - a.y) * (c.x - b.x) - (b.x - a.x) * (c.y - b.y);
            return (val > 0) ? 1 : (val < 0 ? -1 : 0);
        }
        #endregion

        #region Nodes Drawing
        Dictionary<int, float> _nodesHeightCache = new Dictionary<int, float>();
        /// <summary>
        /// Draw a single node at its position.
        /// </summary>
        /// <param name="dl"> The ImDrawListPtr to use for drawing.</param>
        /// <param name="node"> The node to draw.</param>
        private void DrawNode(ImDrawListPtr dl, FuWindow window, FuNode node)
        {
            var g = CalcNodeGeom(node);
            float z = _zoom;

            float startY = g.rectMin.y;
            // utilise g.rectMin/g.rectMax, pas les valeurs du child.
            ImGui.SetCursorScreenPos(g.rectMin);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.zero);
            ImGui.PushStyleColor(ImGuiCol.ChildBg, 0);
            Vector2 size = g.rectMax - g.rectMin;
            size.y = 0;
            ImGui.BeginChild("##node_" + node.Id, size, ImGuiChildFlags.AutoResizeY,
                ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar);

            ImGui.SetWindowFontScale(_zoom);
            var rectMin = ImGui.GetCursorScreenPos();
            var rectMax = rectMin + ImGui.GetContentRegionAvail();

            bool isSelected = IsSelected(node.Id);
            bool isHovered = IsMouseHoverNode(false, out var hovered) && hovered == node;

            // Fond & bord
            uint bodyCol = ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.WindowBg));
            dl.AddRectFilled(rectMin, rectMax, bodyCol, Fugui.Themes.WindowRounding * z);

            Color colNodeColor = Fugui.Themes.GetColor(FuColors.NodeHeader);
            Color borderColor = Fugui.Themes.GetColor(FuColors.Border);
            float borderThickness = 1f;

            if (isHovered && !isSelected)
            {
                borderColor = node.NodeColor ?? Fugui.Themes.GetColor(FuColors.NodeSelectedBorder);
                borderColor *= 0.8f;
                borderThickness = 1.5f;
            }
            else if (isSelected)
            {
                borderColor = node.NodeColor ?? Fugui.Themes.GetColor(FuColors.NodeSelectedBorder);
                borderThickness = 2.0f;
            }
            dl.AddRect(
                  new Vector2(Snap(rectMin.x - borderThickness), Snap(rectMin.y - borderThickness)),
                  new Vector2(Snap(rectMax.x + borderThickness), Snap(rectMax.y + borderThickness)),
                  ImGui.GetColorU32(borderColor),
                  Fugui.Themes.WindowRounding * z,
                  ImDrawFlags.RoundCornersDefault,
                  borderThickness * z
              );

            // Header
            Color headerColor = node.NodeColor ?? colNodeColor;
            dl.AddRectFilled(rectMin, new Vector2(rectMax.x, rectMin.y + g.headerHeight),
                ImGui.GetColorU32(headerColor), Fugui.Themes.WindowRounding * z, ImDrawFlags.RoundCornersTop);

            window.Layout.CenterNextItemH(node.Title);
            window.Layout.CenterNextItemV(node.Title, g.headerHeight);
            window.Layout.Text(node.Title);

            dl.AddLine(
                    new Vector2(rectMin.x, Snap(rectMin.y + g.headerHeight)),
                    new Vector2(rectMax.x, Snap(rectMin.y + g.headerHeight)),
                    ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.Separator)), 1.5f
                );

            // Fonds ports
            if (g.hasIn)
                dl.AddRectFilled(new Vector2(g.leftMinX, g.portsStartY),
                                 new Vector2(g.leftMaxX, g.portsEndY),
                                 ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.ChildBg)));

            // Knobs + labels – positions = GetPortAnchorScreen (même base)
            Vector2 mouse = _canvasOrigin + GetLocalMousePosition();
            float rBase = Fugui.Themes.NodeKnobRadius * z;

            foreach (var p in node.Ports.Values)
            {
                Vector2 c = GetPortAnchorScreen(node, p);

                bool over = Vector2.Distance(mouse, c) <= rBase * 1.8f;
                float r = over ? rBase * 1.5f : rBase;
                uint col = over ? ImGui.GetColorU32(headerColor) : Fugui.Themes.GetColorU32(FuColors.NodePort);
                var typeData = Graph.Registry.GetType(p.DataType);
                if (typeData != null && typeData.Color.HasValue)
                {
                    col = ImGui.GetColorU32(typeData.Color.Value);
                    var convertedType = node.GetCurrentConvertedType(p);
                    var toTypeData = Graph.Registry.GetType(convertedType);
                    if (toTypeData != null && toTypeData.Color.HasValue)
                    {
                        col = ImGui.GetColorU32(toTypeData.Color.Value);
                    }
                }
                dl.AddCircleFilled(c, r, col, 12);

                string label = p.Name ?? p.DataType ?? "?";
                Vector2 lbSize = ImGui.CalcTextSize(label);
                float textDy = -lbSize.y * 0.5f;
                Vector2 labelPos = (p.Direction == FuNodalPortDirection.In)
                    ? (c + new Vector2(10f * z, textDy))
                    : (c + new Vector2(-lbSize.x - 8f * Fugui.Scale, textDy));
                dl.AddText(labelPos, Fugui.Themes.GetColorU32(FuColors.Text), label);

                if (over) { ImGui.SetMouseCursor(ImGuiMouseCursor.Hand); _hoveredPort = (node, p); }
            }

            // UI custom bornée
            float uiStartY = g.portsEndY + ImGui.GetStyle().WindowPadding.y + 4f * Fugui.Scale;
            Vector2 uiMin = new Vector2(rectMin.x + ImGui.GetStyle().FramePadding.x, uiStartY);
            Vector2 uiMax = new Vector2(rectMax.x - ImGui.GetStyle().FramePadding.x, rectMax.y - ImGui.GetStyle().FramePadding.y);
            Vector2 uiSize = uiMax - uiMin;
            uiSize.y = 0f;
            ImGui.SetCursorScreenPos(uiMin);
            ImGui.BeginChild($"##node_ui_{node.Id}", uiSize, ImGuiChildFlags.AutoResizeY,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
            node.OnDraw(window.Layout);
            window.Layout.Dummy(0f, 2f);
            ImGui.EndChild();

            ImGui.SetWindowFontScale(1.0f);
            ImGui.EndChild();
            ImGui.PopStyleVar();
            ImGui.PopStyleColor();
            float endY = ImGui.GetCursorScreenPos().y;
            _nodesHeightCache[node.Id] = endY - startY;

            HandleNodeInputs(node, window, rectMin, rectMax);
        }

        /// <summary>
        /// Handle all inputs for a given node (drag, selection, linking, deletion).
        /// </summary>
        /// <param name="node"> The node to handle inputs for.</param>
        /// <param name="rectMin"> Top-left corner of the node in screen coordinates.</param>
        /// <param name="rectMax"> Bottom-right corner of the node in screen coordinates.</param>
        private void HandleNodeInputs(FuNode node, FuWindow window, Vector2 rectMin, Vector2 rectMax)
        {
            var mouseWA = GetLocalMousePosition();       // mouse in working-area coords
            var mouseScreen = _canvasOrigin + mouseWA;           // screen space

            bool hovered = IsMouseHoverNode(false, out var hoveredNode) && hoveredNode == node;
            bool headerHovered = hovered && IsMouseHoverNode(true, out var headerNode) && headerNode == node;
            if (headerHovered)
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

            // 1) DRAG ON HEADER (group-aware)
            if (headerHovered && window.Mouse.IsDown(FuMouseButton.Left))
            {
                bool ctrl = window.Keyboard.KeyCtrl;
                bool shift = window.Keyboard.KeyShift;

                // Selection semantics: toggle with Ctrl, add with Shift, else replace
                if (!IsSelected(node.Id))
                {
                    SelectNodeInternal(node, additive: shift, toggle: ctrl);
                }
                else if (ctrl)
                {
                    // If already selected and Ctrl is held, toggle off and stop drag intent.
                    SelectNodeInternal(node, additive: false, toggle: true);
                    return;
                }

                // Begin group drag for all selected nodes
                _isDraggingNode = true;
                _isGroupDragging = true;
                _dragStartMouse = mouseScreen;

                _groupDragStartPositions.Clear();
                foreach (var id in _selectedNodeIds)
                {
                    var n = Graph.GetNode(id);
                    if (n != null)
                        _groupDragStartPositions[id] = new Vector2(n.x, n.y); // canvas space
                }
            }

            if (_isDraggingNode && _isGroupDragging)
            {
                if (window.Mouse.IsPressed(FuMouseButton.Left))
                {
                    // Apply delta to all selected nodes (canvas space)
                    var deltaScreen = (_canvasOrigin + GetLocalMousePosition()) - _dragStartMouse;
                    var deltaCanvas = deltaScreen / _zoom;

                    foreach (var kv in _groupDragStartPositions)
                    {
                        var n = Graph.GetNode(kv.Key);
                        if (n == null) continue;
                        Vector2 start = kv.Value;
                        n.SetPosition(start.x + deltaCanvas.x, start.y + deltaCanvas.y);
                    }
                }
                else if (window.Mouse.IsUp(FuMouseButton.Left))
                {
                    _isDraggingNode = false;
                    _isGroupDragging = false;
                    _groupDragStartPositions.Clear();
                }
            }

            // 2) BODY CLICK → selection (no drag)
            if (hovered && !headerHovered && window.Mouse.IsClicked(FuMouseButton.Left))
            {
                bool ctrl = window.Keyboard.KeyCtrl;
                bool shift = window.Keyboard.KeyShift;
                SelectNodeInternal(node, additive: shift, toggle: ctrl);
            }

            // 3) LINKING (on ports) — unchanged
            if (_hoveredPort.HasValue && window.Mouse.IsDown(FuMouseButton.Left))
            {
                var (n, p) = _hoveredPort.Value;
                _isLinking = true;
                _linkFrom = (n.Id, p.Id);
            }
        }

        /// <summary>
        /// Checks if the mouse is hovering over any node.
        /// </summary>
        /// <param name="onlyHeader"> If true, only checks the header area of nodes.</param>
        /// <param name="node"> Outputs the hovered node if found, otherwise null.</param>
        /// <returns> True if hovering over a node, false otherwise.</returns>
        private bool IsMouseHoverNode(bool onlyHeader, out FuNode node)
        {
            var mouse = _canvasOrigin + GetLocalMousePosition();

            for (int i = Graph.Nodes.Count - 1; i >= 0; i--)
            {
                var n = Graph.Nodes[i];
                var g = CalcNodeGeom(n);

                bool inside = (mouse.x >= g.rectMin.x && mouse.x <= g.rectMax.x &&
                               mouse.y >= g.rectMin.y && mouse.y <= g.rectMax.y);
                if (!inside) continue;

                if (onlyHeader)
                {
                    if (mouse.y <= g.rectMin.y + g.headerHeight)
                    {
                        node = n; return true;
                    }
                }
                else
                {
                    node = n; return true;
                }
            }

            node = null; return false;
        }

        /// <summary>
        /// Checks if the mouse is near a Bezier curve defined by points a and b.
        /// </summary>
        private static bool IsMouseNearBezier(Vector2 mouse, Vector2 a, Vector2 b, float threshold)
        {
            // Approximate the Bezier curve with line segments and check distance
            const int segments = 20;
            float dx = Mathf.Abs(b.x - a.x);
            Vector2 c0 = new Vector2(a.x + dx * 0.5f, a.y);
            Vector2 c1 = new Vector2(b.x - dx * 0.5f, b.y);

            float thresholdSq = threshold * threshold;
            Vector2 prev = a;
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                // Cubic Bezier formula
                Vector2 p = Mathf.Pow(1 - t, 3) * a
                    + 3 * Mathf.Pow(1 - t, 2) * t * c0
                    + 3 * (1 - t) * Mathf.Pow(t, 2) * c1
                    + Mathf.Pow(t, 3) * b;

                if (DistancePointSegmentSq(mouse, prev, p) <= thresholdSq)
                    return true;

                prev = p;
            }
            return false;
        }

        /// <summary>
        /// Squared distance from point p to segment ab.
        /// </summary>
        private static float DistancePointSegmentSq(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Vector2.Dot(p - a, ab) / ab.sqrMagnitude;
            t = Mathf.Clamp01(t);
            Vector2 proj = a + ab * t;
            return (p - proj).sqrMagnitude;
        }

        /// <summary>
        /// Vérifie si un point (mouse) est proche d'une connexion droite segmentée (a->b).
        /// </summary>
        /// <param name="mouse">Position de la souris.</param>
        /// <param name="a">Point de départ (à gauche).</param>
        /// <param name="b">Point d'arrivée (à droite).</param>
        /// <param name="threshold">Distance maximale pour considérer la souris "proche".</param>
        /// <param name="segmentLength">Longueur des segments horizontaux aux extrémités.</param>
        /// <returns>True si la souris est proche de la connexion.</returns>
        private static bool IsMouseNearStraight(Vector2 mouse, Vector2 a, Vector2 b, float threshold, float segmentLength = 20f)
        {
            // Snap points comme dans le dessin
            a = new Vector2(Mathf.Floor(a.x) + 0.5f, Mathf.Floor(a.y) + 0.5f);
            b = new Vector2(Mathf.Floor(b.x) + 0.5f, Mathf.Floor(b.y) + 0.5f);

            // Points intermédiaires
            Vector2 aEnd = new Vector2(a.x + segmentLength, a.y);
            Vector2 bStart = new Vector2(b.x - segmentLength, b.y);

            float thresholdSq = threshold * threshold;

            // On teste les 3 segments indépendamment
            if (DistancePointSegmentSq(mouse, a, aEnd) <= thresholdSq)
                return true;
            if (DistancePointSegmentSq(mouse, bStart, b) <= thresholdSq)
                return true;
            if (DistancePointSegmentSq(mouse, aEnd, bStart) <= thresholdSq)
                return true;

            return false;
        }

        /// <summary>
        /// Convert canvas coordinates to screen coordinates.
        /// </summary>
        /// <param name="canvas"> The point in canvas coordinates.</param>
        /// <returns> The point in screen coordinates.</returns>
        private Vector2 CanvasToScreen(Vector2 canvas)
        {
            // screen = origin + pan + canvas * zoom
            return _canvasOrigin + _pan + canvas * _zoom;
        }

        /// <summary>
        /// Convert screen coordinates to canvas coordinates.
        /// </summary>
        /// <param name="screen"> The point in screen coordinates.</param>
        /// <returns> The point in canvas coordinates.</returns>
        private Vector2 ScreenToCanvas(Vector2 screen)
        {
            // canvas = (screen - origin - pan) / zoom
            return (screen - _canvasOrigin - _pan) / _zoom;
        }

        /// <summary>
        /// Snap a float value to the nearest 0.5 (for pixel-perfect alignment).
        /// </summary>
        /// <param name="v"> The value to snap.</param>
        /// <returns> The snapped value.</returns>
        private static float Snap(float v) => Mathf.Floor(v) + 0.5f;

        /// <summary>
        /// Get the screen position of a port's anchor point (where edges connect).
        /// </summary>
        /// <param name="node"> The node containing the port.</param>
        /// <param name="port"> The port to get the anchor for.</param>
        /// <returns> The screen position of the port's anchor point.</returns>
        private Vector2 GetPortAnchorScreen(FuNode node, FuNodalPort port)
        {
            var g = CalcNodeGeom(node);
            float z = _zoom;

            int idx = 0;
            for (int i = 0; i < node.Ports.Count; ++i)
            {
                var p = node.Ports.ElementAt(i).Value;
                if (p.Direction == port.Direction)
                {
                    if (p.Id == port.Id) break;
                    idx++;
                }
            }

            float knobMargin = Fugui.Themes.NodeKnobMargin * z;
            float x = (port.Direction == FuNodalPortDirection.In)
                    ? g.leftMinX + knobMargin
                    : g.rightMaxX - knobMargin;

            float y = g.portsStartY + (idx + 0.5f) * g.portLineHeight;

            // Snap pour aligner pile-poil avec les lignes/edges
            return new Vector2(Snap(x), Snap(y));
        }

        /// <summary>
        /// Struct holding precalculated geometry for a node to optimize drawing and interaction.
        /// </summary>
        private struct NodeGeom
        {
            public Vector2 rectMin, rectMax;
            public float headerHeight;
            public float portLineHeight;
            public float portsStartY, portsEndY;
            public bool hasIn, hasOut;
            public float leftMinX, leftMaxX, rightMinX, rightMaxX; // colonnes ports
        }

        /// <summary>
        /// Calculate and cache the geometry of a node for drawing and interaction.
        /// </summary>
        /// <param name="node"> The node to calculate geometry for.</param>
        /// <returns> The calculated NodeGeom.</returns>
        private NodeGeom CalcNodeGeom(FuNode node)
        {
            if (_nodeGeometries.TryGetValue(node.Id, out var geom))
                return geom;

            float lineH = ImGui.GetTextLineHeightWithSpacing();
            float headerTextH = ImGui.CalcTextSize("Ap").y;
            float padY = ImGui.GetStyle().FramePadding.y;

            float headerHeight = headerTextH + padY * 2.0f;

            int inCount = node.Ports.Count(p => p.Value.Direction == FuNodalPortDirection.In);
            int outCount = node.Ports.Count(p => p.Value.Direction == FuNodalPortDirection.Out);
            int maxPorts = Mathf.Max(inCount, outCount);

            float totalHeight = headerHeight + (maxPorts * lineH)
                                + ImGui.GetStyle().WindowPadding.y * 2f;

            if (_nodesHeightCache.TryGetValue(node.Id, out var hCache))
            {
                totalHeight = hCache; // conserve la hauteur UI si déjà calculée
            }

            Vector2 posScreen = CanvasToScreen(new Vector2(node.x, node.y));
            Vector2 rectMin = posScreen;
            Vector2 rectMax = posScreen + new Vector2(node.Width * _zoom * fuScale, totalHeight);

            float portsStartY = rectMin.y + headerHeight;
            float portsEndY = portsStartY + maxPorts * lineH;

            bool split = (inCount > 0 && outCount > 0);
            float leftMinX = rectMin.x;
            float leftMaxX = split ? (rectMin.x + rectMax.x) * 0.5f : rectMax.x;
            float rightMinX = split ? leftMaxX : rectMin.x;
            float rightMaxX = rectMax.x;

            NodeGeom ng = new NodeGeom
            {
                rectMin = rectMin,
                rectMax = rectMax,
                headerHeight = headerHeight,
                portLineHeight = lineH,
                portsStartY = portsStartY,
                portsEndY = portsEndY,
                hasIn = (inCount > 0),
                hasOut = (outCount > 0),
                leftMinX = leftMinX,
                leftMaxX = leftMaxX,
                rightMinX = rightMinX,
                rightMaxX = rightMaxX
            };
            _nodeGeometries[node.Id] = ng;
            return ng;
        }
        #endregion

        #region Link Handling
        /// <summary>
        /// Try to finish the link operation if possible (called on mouse release).
        /// </summary>
        private void TryFinishLink()
        {
            // Si aucun port source n'est défini, on ne peut rien faire
            if (!_linkFrom.HasValue)
                return;

            var (fromNodeId, fromPortId) = _linkFrom.Value;
            var fromNode = Graph.GetNode(fromNodeId);
            var fromPort = fromNode?.GetPort(fromPortId);

            if (fromNode == null || fromPort == null)
                return;

            // ✅ Si aucun port survolé -> on affiche les nodes compatibles
            if (!_hoveredPort.HasValue)
            {
                // On inverse la direction (input -> output et inversement)
                var oppositeDir = fromPort.Direction == FuNodalPortDirection.In
                    ? FuNodalPortDirection.Out
                    : FuNodalPortDirection.In;

                if (ShowCompatiblesNodesOnVoidLink)
                    DisplayCompatiblesNodes(fromNodeId, fromPort, oppositeDir, fromPort.AllowedTypes);
                return;
            }

            // ✅ Sinon, tentative de connexion normale
            var (targetNode, targetPort) = _hoveredPort.Value;
            if (targetNode == null || targetPort == null)
                return;

            FuNodalPort a = fromPort;
            FuNodalPort b = targetPort;
            int aNodeId = fromNode.Id;
            int bNodeId = targetNode.Id;

            Graph.TryConnect(a, b, aNodeId, bNodeId);
        }

        /// <summary>
        /// Display a context menu with nodes compatible with the given port direction and data type.
        /// </summary>
        /// <param name="direction"> The port direction (In/Out) to find compatible nodes for.</param>
        /// <param name="dataType"> The data type to find compatible nodes for.</param>
        private void DisplayCompatiblesNodes(int fromNode, FuNodalPort fromPort, FuNodalPortDirection direction, HashSet<string> dataTypes)
        {
            var compatibleTypes = Graph.Registry.GetCompatibleNodes(direction, dataTypes);
            voidLinkFromNode = fromNode;
            voidLinkFromPort = fromPort;
            DrawContextMenu(FuWindow.CurrentDrawingWindow.Layout, compatibleTypes, false, true, true);
        }
        #endregion

        #region Selection helpers
        /// <summary>
        /// Returns true if the node with the given id is currently selected.
        /// </summary>
        private bool IsSelected(int id) => _selectedNodeIds.Contains(id);

        /// <summary>
        /// Clears the selection set.
        /// </summary>
        private void ClearSelection() => _selectedNodeIds.Clear();

        /// <summary>
        /// Selects a node with optional add/toggle semantics (Ctrl = toggle, Shift = add).
        /// </summary>
        private void SelectNodeInternal(FuNode node, bool additive, bool toggle)
        {
            if (node == null) return;

            if (toggle)
            {
                if (!_selectedNodeIds.Remove(node.Id))
                    _selectedNodeIds.Add(node.Id);
                return;
            }

            if (!additive)
                _selectedNodeIds.Clear();

            _selectedNodeIds.Add(node.Id);
        }

        /// <summary>
        /// Adds all nodes that intersect the marquee rectangle in working-area space.
        /// </summary>
        private void MarqueeSelect(bool additive)
        {
            // Normalize rect in WorkingArea space
            Vector2 minWA = new Vector2(Mathf.Min(_marqueeStartWA.x, _marqueeEndWA.x), Mathf.Min(_marqueeStartWA.y, _marqueeEndWA.y));
            Vector2 maxWA = new Vector2(Mathf.Max(_marqueeStartWA.x, _marqueeEndWA.x), Mathf.Max(_marqueeStartWA.y, _marqueeEndWA.y));

            if (!additive) _selectedNodeIds.Clear();

            for (int i = 0; i < Graph.Nodes.Count; i++)
            {
                var n = Graph.Nodes[i];
                var g = CalcNodeGeom(n);

                // Convert node rect (screen) to WorkingArea space.
                // screen = origin + WA => WA = screen - origin
                Vector2 nMinWA = g.rectMin - _canvasOrigin;
                Vector2 nMaxWA = g.rectMax - _canvasOrigin;

                bool overlap = !(nMaxWA.x < minWA.x || nMinWA.x > maxWA.x || nMaxWA.y < minWA.y || nMinWA.y > maxWA.y);
                if (overlap) _selectedNodeIds.Add(n.Id);
            }
        }
        #endregion

        #region Minimap
        /// <summary>
        /// Sets a new zoom while keeping a given canvas point under the mouse stable (anchored).
        /// </summary>
        private void SetZoomAroundCanvasPoint(float newZoom, Vector2 canvasPoint)
        {
            float clamped = Mathf.Clamp(newZoom, MinZoom, MaxZoom);
            // current screen pos of that canvas point:
            // screen = origin + pan + canvas * zoom  → we keep it stable ⇒ solve pan'
            // Choose to anchor under current mouse in working area center (or keep same screen point).
            // Here we keep the same screen point for canvasPoint:
            // pan' = screen - origin - canvas * newZoom
            // But we don't have a dedicated "screen" target; use current screen for that canvas point:
            Vector2 currentScreen = _canvasOrigin + _pan + canvasPoint * _zoom;
            _zoom = clamped;
            _pan = (currentScreen - _canvasOrigin) - canvasPoint * _zoom;
        }

        /// <summary>
        /// Fit all graph content in the main viewport (updates _zoom and _pan).
        /// </summary>
        private void FitViewToGraphBounds(Vector2 gMin, Vector2 gMax, float margin = 32f)
        {
            Vector2 gSize = gMax - gMin;
            gSize.x = Mathf.Max(gSize.x, 1f);
            gSize.y = Mathf.Max(gSize.y, 1f);

            Vector2 target = _canvasSize - new Vector2(margin * 2f, margin * 2f);
            target.x = Mathf.Max(target.x, 1f);
            target.y = Mathf.Max(target.y, 1f);

            float sX = target.x / gSize.x;
            float sY = target.y / gSize.y;
            float newZoom = Mathf.Clamp(Mathf.Min(sX, sY), MinZoom, MaxZoom);

            _zoom = newZoom;
            // Center g center in viewport
            Vector2 gCenter = (gMin + gMax) * 0.5f;
            _pan = (_canvasSize * 0.5f) - gCenter * _zoom;
        }
        #endregion

        #region Public API
        /// <summary>
        /// Draw the nodal canvas within the given layout.
        /// </summary>
        /// <param name="layout"> The FuLayout to draw into.</param>
        public void Draw(FuWindow window)
        {
            _nodeGeometries.Clear();

            DrawContextMenu(window.Layout, Graph.Registry.GetRegisteredNode(), true, false, false);

            // push font and scale
            Fugui.Themes.CurrentTheme.Apply(_zoom);
            ImGui.SetWindowFontScale(_zoom);

            // draw canvas
            DrawCanvas(window);

            // restore font and scale
            Fugui.Themes.CurrentTheme.Apply(Fugui.Scale);
            ImGui.SetWindowFontScale(1.0f);
            Fugui.PopFont();

            // copy paste handling
            if (window.Keyboard.KeyCtrl && window.Keyboard.GetKeyDown(FuKeysCode.C) && _selectedNodeIds.Count > 0)
            {
                Graph.CopyNodes(_selectedNodeIds);
            }
            if (window.Keyboard.KeyCtrl && window.Keyboard.GetKeyDown(FuKeysCode.V))
            {
                Vector2 mouseCanvas = ScreenToCanvas(_canvasOrigin + GetLocalMousePosition());
                Graph.PasteNodes(mouseCanvas);
            }

            // PostDraw graph compute
            Graph.ComputeGraphIfDirty();
        }

        /// <summary>
        /// Register a custom context menu callback to add items to the canvas right-click menu.
        /// </summary>
        /// <param name="customContextMenuCallback"> The callback to invoke when building the context menu.</param>
        public void RegisterCustomContextMenu(Action<FuContextMenuBuilder> customContextMenuCallback)
        {
            this.OnDrawContextMenu = customContextMenuCallback;
        }

        /// <summary>
        /// Select a node by its ID (replaces the selection).
        /// </summary>
        /// <param name="nodeId">The ID of the node to select.</param>
        public void SelectNode(int nodeId)
        {
            var n = Graph.GetNode(nodeId);
            _selectedNodeIds.Clear();
            if (n != null) _selectedNodeIds.Add(n.Id);
        }

        /// <summary>
        /// Select a node (replaces the selection).
        /// </summary>
        /// <param name="node">The node to select.</param>
        public void SelectNode(FuNode node)
        {
            _selectedNodeIds.Clear();
            if (node != null && Graph.Nodes.Contains(node))
                _selectedNodeIds.Add(node.Id);
        }

        /// <summary>
        /// Draws a simplified minimap of the current nodal canvas with:
        /// - Node colored rectangles (uses node.NodeColor if set),
        /// - Straight edge segments (optional simplification),
        /// - Current viewport rectangle,
        /// - Click & drag to pan the main view,
        /// - Mouse wheel to zoom the main view (anchored under cursor),
        /// - "Fit All" button to frame the whole graph in the main viewport.
        /// </summary>
        /// <param name="size">Requested minimap size in pixels.</param>
        public void DrawMiniMap(Vector2 size)
        {
            var dl = ImGui.GetWindowDrawList();
            Vector2 mmMin = ImGui.GetCursorScreenPos();
            Vector2 mmMax = mmMin + ImGui.GetContentRegionAvail();

            // Visual style
            uint frameCol = ImGui.GetColorU32(new Color(1f, 1f, 1f, 0.35f));
            uint nodeCol = ImGui.GetColorU32(new Color(0.8f, 0.8f, 0.8f, 0.9f));
            uint edgeCol = ImGui.GetColorU32(new Color(0.7f, 0.7f, 0.7f, 0.8f));
            uint viewCol = ImGui.GetColorU32(new Color(0.2f, 0.6f, 1f, 0.9f));

            // Background
            dl.AddRect(mmMin, mmMax, frameCol, 4f, ImDrawFlags.RoundCornersAll, 1f);

            // Early out if empty graph
            if (Graph == null || (Graph.Nodes.Count == 0 && Graph.Edges.Count == 0))
            {
                return;
            }

            // --- 1) Compute graph bounds in CANVAS space ---
            Vector2 gMin = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 gMax = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

            Action<Vector2> expand = (pt) =>
            {
                gMin.x = Mathf.Min(gMin.x, pt.x); gMin.y = Mathf.Min(gMin.y, pt.y);
                gMax.x = Mathf.Max(gMax.x, pt.x); gMax.y = Mathf.Max(gMax.y, pt.y);
            };

            for (int i = 0; i < Graph.Nodes.Count; i++)
            {
                var n = Graph.Nodes[i];
                var ng = CalcNodeGeom(n);
                Vector2 nMinCanvas = ScreenToCanvas(ng.rectMin);
                Vector2 nMaxCanvas = ScreenToCanvas(ng.rectMax);
                expand(nMinCanvas); expand(nMaxCanvas);
            }

            for (int i = 0; i < Graph.Edges.Count; i++)
            {
                var e = Graph.Edges[i];
                var from = Graph.GetNode(e.FromNodeId);
                var to = Graph.GetNode(e.ToNodeId);
                if (from == null || to == null) continue;

                var pFrom = from.GetPort(e.FromPortId);
                var pTo = to.GetPort(e.ToPortId);
                if (pFrom == null || pTo == null) continue;

                Vector2 aScreen = GetPortAnchorScreen(from, pFrom);
                Vector2 bScreen = GetPortAnchorScreen(to, pTo);
                expand(ScreenToCanvas(aScreen));
                expand(ScreenToCanvas(bScreen));
            }

            if (!float.IsFinite(gMin.x) || !float.IsFinite(gMax.x) || gMin == gMax)
            {
                gMin = new Vector2(-100f, -100f);
                gMax = new Vector2(100f, 100f);
            }

            // --- 2) Canvas → Minimap transform ---
            const float pad = 6f;
            Vector2 innerMin = mmMin + new Vector2(pad, pad);
            Vector2 innerMax = mmMax - new Vector2(pad, pad);
            Vector2 innerSize = innerMax - innerMin;

            Vector2 gSize = gMax - gMin;
            gSize.x = Mathf.Max(gSize.x, 1f);
            gSize.y = Mathf.Max(gSize.y, 1f);

            float sx = innerSize.x / gSize.x;
            float sy = innerSize.y / gSize.y;
            float s = Mathf.Min(sx, sy);

            Func<Vector2, Vector2> MM = (canvasPt) =>
            {
                return innerMin + (canvasPt - gMin) * s;
            };

            // --- 3) Draw edges (straight) ---
            for (int i = 0; i < Graph.Edges.Count; i++)
            {
                var e = Graph.Edges[i];
                var from = Graph.GetNode(e.FromNodeId);
                var to = Graph.GetNode(e.ToNodeId);
                if (from == null || to == null) continue;

                var pFrom = from.GetPort(e.FromPortId);
                var pTo = to.GetPort(e.ToPortId);
                if (pFrom == null || pTo == null) continue;

                Vector2 aCanvas = ScreenToCanvas(GetPortAnchorScreen(from, pFrom));
                Vector2 bCanvas = ScreenToCanvas(GetPortAnchorScreen(to, pTo));
                dl.AddLine(MM(aCanvas), MM(bCanvas), edgeCol, 1.0f);
            }

            // --- 4) Draw nodes (colored) ---
            for (int i = 0; i < Graph.Nodes.Count; i++)
            {
                var n = Graph.Nodes[i];
                var ng = CalcNodeGeom(n);
                Vector2 nMinCanvas = ScreenToCanvas(ng.rectMin);
                Vector2 nMaxCanvas = ScreenToCanvas(ng.rectMax);

                Vector2 a = MM(nMinCanvas);
                Vector2 b = MM(nMaxCanvas);
                if (b.x - a.x < 2f) b.x = a.x + 2f;
                if (b.y - a.y < 2f) b.y = a.y + 2f;

                Color baseColC = n.NodeColor ?? new Color(0.8f, 0.8f, 0.8f, 0.95f);
                uint baseCol = ImGui.GetColorU32(baseColC);
                dl.AddRectFilled(a, b, baseCol, 2f);
            }

            // --- 5) Draw current viewport rectangle ---
            Vector2 viewMinCanvas = -_pan / Mathf.Max(_zoom, 1e-6f);
            Vector2 viewMaxCanvas = (_canvasSize - _pan) / Mathf.Max(_zoom, 1e-6f);
            Vector2 vA = MM(viewMinCanvas);
            Vector2 vB = MM(viewMaxCanvas);
            dl.AddRect(vA, vB, viewCol, 2f, ImDrawFlags.RoundCornersAll, 1.5f);

            // --- 6) Interaction ---
            bool hoveredMini = ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows);

            // 6a) Zoom with mouse wheel (anchored at mouse)
            if (hoveredMini)
            {
                float wheel = ImGui.GetIO().MouseWheel;
                if (Mathf.Abs(wheel) > float.Epsilon)
                {
                    // Map mouse (screen) -> canvas via inverse MM transform
                    Vector2 mouseScreen = ImGui.GetIO().MousePos;
                    // Clamp to inner area for stable mapping
                    Vector2 clamped = new Vector2(
                        Mathf.Clamp(mouseScreen.x, innerMin.x, innerMax.x),
                        Mathf.Clamp(mouseScreen.y, innerMin.y, innerMax.y)
                    );
                    // canvasPt = gMin + (clamped - innerMin)/s
                    Vector2 canvasPt = gMin + (clamped - innerMin) / Mathf.Max(s, 1e-6f);

                    float newZoom = _zoom * (1f + wheel * 0.1f);
                    SetZoomAroundCanvasPoint(newZoom, canvasPt);
                }
            }

            // 6b) Click & drag to pan the main view
            if (hoveredMini && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                _isMinimapDragging = true;
            }
            if (_isMinimapDragging && ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                Vector2 mouseScreen = ImGui.GetIO().MousePos;
                Vector2 clamped = new Vector2(
                    Mathf.Clamp(mouseScreen.x, innerMin.x, innerMax.x),
                    Mathf.Clamp(mouseScreen.y, innerMin.y, innerMax.y)
                );
                Vector2 canvasPt = gMin + (clamped - innerMin) / Mathf.Max(s, 1e-6f);
                _pan = (_canvasSize * 0.5f) - canvasPt * _zoom;
            }
            if (_isMinimapDragging && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                _isMinimapDragging = false;
            }
        }

        /// <summary>
        /// Fit the main canvas view to show the entire graph with an optional margin.
        /// </summary>
        /// <param name="margin"> Margin in pixels around the graph bounds.</param>
        public void FitCanvasToGraph(float margin = 32f)
        {
            if (Graph == null || (Graph.Nodes.Count == 0 && Graph.Edges.Count == 0))
                return;
            // Compute graph bounds in CANVAS space
            Vector2 gMin = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 gMax = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            Action<Vector2> expand = (pt) =>
            {
                gMin.x = Mathf.Min(gMin.x, pt.x); gMin.y = Mathf.Min(gMin.y, pt.y);
                gMax.x = Mathf.Max(gMax.x, pt.x); gMax.y = Mathf.Max(gMax.y, pt.y);
            };
            for (int i = 0; i < Graph.Nodes.Count; i++)
            {
                var n = Graph.Nodes[i];
                var ng = CalcNodeGeom(n);
                Vector2 nMinCanvas = ScreenToCanvas(ng.rectMin);
                Vector2 nMaxCanvas = ScreenToCanvas(ng.rectMax);
                expand(nMinCanvas); expand(nMaxCanvas);
            }
            for (int i = 0; i < Graph.Edges.Count; i++)
            {
                var e = Graph.Edges[i];
                var from = Graph.GetNode(e.FromNodeId);
                var to = Graph.GetNode(e.ToNodeId);
                if (from == null || to == null) continue;
                var pFrom = from.GetPort(e.FromPortId);
                var pTo = to.GetPort(e.ToPortId);
                if (pFrom == null || pTo == null) continue;
                Vector2 aScreen = GetPortAnchorScreen(from, pFrom);
                Vector2 bScreen = GetPortAnchorScreen(to, pTo);
                expand(ScreenToCanvas(aScreen));
                expand(ScreenToCanvas(bScreen));
            }
            if (!float.IsFinite(gMin.x) || !float.IsFinite(gMax.x) || gMin == gMax)
                return;
            FitViewToGraphBounds(gMin, gMax, margin);
        }
        #endregion
    }
}