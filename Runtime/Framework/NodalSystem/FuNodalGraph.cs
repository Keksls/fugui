using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fu.Framework
{
    public sealed class FuNodalGraph
    {
        public string Version { get; set; } = "1.0.0";
        public int Id { get; set; } = FuNodeId.New();
        public string Name { get; set; } = "New Graph";
        public List<FuNode> Nodes { get; set; } = new List<FuNode>();
        public List<FuNodalEdge> Edges { get; set; } = new List<FuNodalEdge>();
        public FuNodalRegistry Registry { get; private set; } = new FuNodalRegistry();
        private bool _isDirty = false;
        private List<FuNode> _clipboardNodes = new List<FuNode>();
        private List<FuNodalEdge> _clipboardEdges = new List<FuNodalEdge>();

        /// <summary>
        /// Find a node by its unique identifier
        /// </summary>
        /// <param name="id"> The unique identifier of the node to find</param>
        /// <returns> The node with the specified identifier, or null if no such node exists</returns>
        public FuNode GetNode(int id)
        {
            for (int i = 0; i < Nodes.Count; i++)
                if (Nodes[i].Id == id)
                    return Nodes[i];
            return null;
        }

        #region Edge Management
        /// <summary>
        /// Check if connecting two nodes would create a cycle in the graph
        /// </summary>
        /// <param name="a"> The a port node where the connection starts (output of another node)</param>
        /// <param name="b"> The b port node where the connection ends (input of this node)</param>
        /// <param name="aNodeId"> The unique identifier of the a node</param>
        /// <param name="bNodeId"> The unique identifier of the b node</param>
        /// <returns> True if connecting the nodes would create a cycle, false otherwise</returns>
        public bool TryConnect(FuNodalPort a, FuNodalPort b, int aNodeId, int bNodeId)
        {
            // always OUT→IN
            if (a.Direction == FuNodalPortDirection.In && b.Direction == FuNodalPortDirection.Out)
            {
                (a, b) = (b, a);
                (aNodeId, bNodeId) = (bNodeId, aNodeId);
            }

            // Check OUT→IN valid direction
            if (a.Direction != FuNodalPortDirection.Out || b.Direction != FuNodalPortDirection.In)
                return false;

            // Check if already connected
            bool alreadyExists = Edges.Exists(e =>
                e.FromNodeId == aNodeId &&
                e.FromPortId == a.Id &&
                e.ToNodeId == bNodeId &&
                e.ToPortId == b.Id);
            if (alreadyExists)
            {
                Debug.LogWarning("[Nodal] Link already exists between these two ports.");
                return false;
            }

            // Check recursive connection
            if (CheckLinkCycle(aNodeId, bNodeId))
            {
                Debug.LogWarning("[Nodal] Cannot create link as it would create a cycle in the graph.");
                return false;
            }

            // Check compatibility
            FuNode inNode = GetNode(bNodeId);
            if (!inNode.CanConnect(a, b))
            {
                Debug.LogWarning("[Nodal] Cannot create link as ports are not compatible.");
                return false;
            }

            // check allowed types
            if (b.AllowedTypes.Count > 0 && !b.AllowedTypes.Contains(a.DataType))
            {
                Debug.LogWarning("[Nodal] Cannot create link as port data types are not compatible.");
                return false;
            }

            // check multiplicity
            if (b.Multiplicity == FuNodalMultiplicity.Single)
            {
                // Remove existing edges to this input port
                var edgesToRemove = Edges.FindAll(e => e.ToNodeId == bNodeId && e.ToPortId == b.Id);
                foreach (var edge in edgesToRemove)
                {
                    DeleteEdge(edge);
                }
            }

            // Add the edge
            Edges.Add(new FuNodalEdge
            {
                FromNodeId = aNodeId,
                FromPortId = a.Id,
                ToNodeId = bNodeId,
                ToPortId = b.Id
            });

            // Set input port data type to match output port
            b.DataType = a.DataType;
            b.Data = a.Data;

            // Mark graph as dirty
            _isDirty = true;

            return true;
        }

        /// <summary>
        /// Checks recursively if adding a link from fromNodeId to toNodeId would create a cycle.
        /// </summary>
        /// <param name="fromNodeId"> The starting node ID (where the link originates).</param>
        /// <param name="toNodeId"> The target node ID (where the link points to).</param>
        /// <returns> True if a cycle would be created, false otherwise.</returns>
        private bool CheckLinkCycle(int fromNodeId, int toNodeId)
        {
            // Si on revient sur le point de départ, boucle détectée
            if (fromNodeId == toNodeId)
                return true;

            // Récupère toutes les connexions sortantes du nœud cible
            var children = Edges
                .FindAll(e => e.FromNodeId == toNodeId)
                .ConvertAll(e => e.ToNodeId);

            // Vérifie récursivement si l'un des descendants pointe vers l'origine
            foreach (var child in children)
            {
                if (CheckLinkCycle(fromNodeId, child))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Delete an edge from the graph and optionally recompute the graph
        /// </summary>
        /// <param name="edge"> The edge to delete.</param>
        /// <param name="recompute"> Whether to recompute the graph after deletion. Default is true.</param>
        public void DeleteEdge(FuNodalEdge edge)
        {
            // reset input port data type and data
            FuNode toNode = GetNode(edge.ToNodeId);
            if (toNode != null)
            {
                var toPort = toNode.Ports.Values.FirstOrDefault(p => p.Id == edge.ToPortId);
                toNode.SetDefaultValues(toPort);
            }
            // remove edge and mark dirty
            Edges.Remove(edge);
            _isDirty = true;
        }
        #endregion

        #region Compute and Dirtyness
        /// <summary>
        /// Compute the whole graph in a valid dependency order (inputs before consumers).
        /// Starts from nodes with no incoming edges, then propagates.
        /// Logs a warning if a cycle is detected and computes only the acyclic part.
        /// </summary>
        public void ComputeGraphIfDirty()
        {
            if (!IsDirty())
                return;
            ClearDirty();

            try
            {
                // Build indegree (number of incoming edges per node) and adjacency (outgoing neighbors).
                var indegree = Nodes.ToDictionary(n => n.Id, n => 0);
                var adjacency = Nodes.ToDictionary(n => n.Id, n => new List<int>());

                for (int i = 0; i < Edges.Count; i++)
                {
                    var e = Edges[i];
                    if (!indegree.ContainsKey(e.ToNodeId) || !adjacency.ContainsKey(e.FromNodeId))
                        continue; // Edge references a missing node; ignore safely.

                    indegree[e.ToNodeId] += 1;
                    adjacency[e.FromNodeId].Add(e.ToNodeId);
                }

                // Initialize queue with nodes that have no inputs.
                var queue = new Queue<int>(indegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));

                int processed = 0;
                while (queue.Count > 0)
                {
                    var nodeId = queue.Dequeue();
                    var node = GetNode(nodeId);
                    if (node != null)
                    {
                        try
                        {
                            // propagate in nodes out values to connected nodes inputs
                            var outgoingEdges = Edges.FindAll(e => e.FromNodeId == nodeId);
                            if (outgoingEdges != null)
                            {
                                for (int i = 0; i < outgoingEdges.Count; i++)
                                {
                                    var edge = outgoingEdges[i];
                                    var toNode = GetNode(edge.ToNodeId);
                                    if (toNode == null) continue;
                                    var fromPort = node.Ports.Values.FirstOrDefault(p => p.Id == edge.FromPortId);
                                    var toPort = toNode.Ports.Values.FirstOrDefault(p => p.Id == edge.ToPortId);
                                    if (fromPort == null || toPort == null) continue;
                                    // set input port data type to match output port
                                    toPort.DataType = fromPort.DataType;
                                    toPort.Data = fromPort.Data;
                                }
                            }

                            // nodes outputs are calculated into compute
                            node.Compute();
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[Nodal] Compute failed on node '{node?.GetType().Name}' ({nodeId}): {ex}");
                        }
                    }

                    processed++;

                    // Decrease indegree of each neighbor; enqueue when all its inputs are satisfied.
                    var neighbors = adjacency.TryGetValue(nodeId, out var list) ? list : null;
                    if (neighbors == null) continue;

                    for (int i = 0; i < neighbors.Count; i++)
                    {
                        var to = neighbors[i];
                        if (!indegree.ContainsKey(to)) continue;
                        indegree[to] -= 1;
                        if (indegree[to] == 0)
                            queue.Enqueue(to);
                    }
                }

                if (processed < Nodes.Count)
                {
                    // If this ever triggers, there is a cycle or dangling edge set.
                    Debug.LogWarning($"[Nodal] Cycle or invalid edges detected. Computed {processed}/{Nodes.Count} nodes.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Nodal] Graph computation failed: {ex}");
            }
            finally
            {
                // Always clear dirty to avoid infinite recompute loops.
                ClearDirty();
            }
        }

        /// <summary>
        /// Returns nodes in a valid topological order (if possible).
        /// Useful if you prefer to inspect or run custom passes before calling <see cref="ComputeGraphIfDirty"/>.
        /// </summary>
        /// <param name="order">Output list in dependency-respecting order.</param>
        /// <returns>True if all nodes are ordered (acyclic), false if a cycle was detected.</returns>
        public bool TryGetExecutionOrder(out List<FuNode> order)
        {
            order = new List<FuNode>(Nodes.Count);

            var indegree = Nodes.ToDictionary(n => n.Id, n => 0);
            var adjacency = Nodes.ToDictionary(n => n.Id, n => new List<int>());

            for (int i = 0; i < Edges.Count; i++)
            {
                var e = Edges[i];
                if (!indegree.ContainsKey(e.ToNodeId) || !adjacency.ContainsKey(e.FromNodeId))
                    continue;

                indegree[e.ToNodeId] += 1;
                adjacency[e.FromNodeId].Add(e.ToNodeId);
            }

            var queue = new Queue<int>(indegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
            int processed = 0;

            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                var node = GetNode(id);
                if (node != null)
                    order.Add(node);

                processed++;

                var neighbors = adjacency[id];
                for (int i = 0; i < neighbors.Count; i++)
                {
                    var to = neighbors[i];
                    indegree[to] -= 1;
                    if (indegree[to] == 0)
                        queue.Enqueue(to);
                }
            }

            return processed == Nodes.Count;
        }

        /// <summary>
        /// Check if the graph or any of its nodes are dirty (have unsaved changes).
        /// </summary>
        /// <returns> True if the graph or any node is dirty, false otherwise.</returns>
        public bool IsDirty()
        {
            if (_isDirty)
                return true;
            return Nodes.Exists(n => n.Dirty);
        }

        /// <summary>
        /// Clear the dirty flag on the graph and all its nodes.
        /// </summary>
        public void ClearDirty()
        {
            _isDirty = false;
            Nodes.ForEach(n => n.Dirty = false);
        }

        /// <summary>
        /// Mark the graph as dirty (having unsaved changes).
        /// </summary>
        public void MarkDirty()
        {
            _isDirty = true;
        }
        #endregion

        #region Copy Paste
        /// <summary>
        /// Copy selected nodes and their edges to the clipboard.
        /// </summary>
        /// <param name="nodesToCopy"> The set of node IDs to copy.</param>
        public void CopyNodes(HashSet<int> nodesToCopy)
        {
            var nodesList = Nodes.Where(n => nodesToCopy.Contains(n.Id)).ToList();
            CopyNodesToClipboard(nodesList);
        }

        /// <summary>
        /// Lightweight clipboard node payload.
        /// </summary>
        private sealed class ClipboardNode
        {
            public int OriginalNodeId { get; set; }
            public string NodeTypeId { get; set; }
            public string Json { get; set; }
            public float OffsetX { get; set; }
            public float OffsetY { get; set; }
        }

        /// <summary>
        /// Clipboard edge that references ports by NAME (stable) instead of IDs.
        /// </summary>
        private sealed class ClipboardEdge
        {
            public int FromOriginalNodeId { get; set; }
            public string FromPortName { get; set; }
            public int ToOriginalNodeId { get; set; }
            public string ToPortName { get; set; }
        }

        private readonly Dictionary<int, ClipboardNode> _clipNodes = new Dictionary<int, ClipboardNode>();
        private readonly List<ClipboardEdge> _clipEdges = new List<ClipboardEdge>();

        /// <summary>
        /// Copy a set of nodes and their internal edges to the clipboard.
        /// Ports are recorded by NAME so they can be resolved after re-instantiation.
        /// </summary>
        public void CopyNodesToClipboard(List<FuNode> nodesToCopy)
        {
            _clipNodes.Clear();
            _clipEdges.Clear();

            if (nodesToCopy == null || nodesToCopy.Count == 0)
                return;

            // Normalize positions: top-left => (0,0)
            float minX = nodesToCopy.Min(n => n.x);
            float minY = nodesToCopy.Min(n => n.y);

            // Build a quick lookup of selected node IDs
            var selectedIds = new HashSet<int>(nodesToCopy.Select(n => n.Id));

            // 1) Store nodes as type + json + relative position
            foreach (var node in nodesToCopy)
            {
                _clipNodes[node.Id] = new ClipboardNode
                {
                    OriginalNodeId = node.Id,
                    NodeTypeId = Registry.GetNodeTypeId(node),
                    Json = node.Serialize(),
                    OffsetX = node.x - minX,
                    OffsetY = node.y - minY
                };
            }

            // 2) Store edges among the selection, but by port NAME
            foreach (var edge in Edges)
            {
                if (!selectedIds.Contains(edge.FromNodeId) || !selectedIds.Contains(edge.ToNodeId))
                    continue;

                // Resolve current (original) port names from their IDs
                var fromNode = nodesToCopy.FirstOrDefault(n => n.Id == edge.FromNodeId);
                var toNode = nodesToCopy.FirstOrDefault(n => n.Id == edge.ToNodeId);
                if (fromNode == null || toNode == null)
                    continue;

                string fromPortName = GetPortNameById(fromNode, edge.FromPortId);
                string toPortName = GetPortNameById(toNode, edge.ToPortId);
                if (fromPortName == null || toPortName == null)
                    continue;

                _clipEdges.Add(new ClipboardEdge
                {
                    FromOriginalNodeId = edge.FromNodeId,
                    FromPortName = fromPortName,
                    ToOriginalNodeId = edge.ToNodeId,
                    ToPortName = toPortName
                });
            }
        }

        /// <summary>
        /// Paste nodes and edges stored in the clipboard at a given position.
        /// Nodes are recreated from type+json; edges are rebuilt by resolving ports by NAME.
        /// </summary>
        public void PasteNodes(Vector2 position)
        {
            if (_clipNodes.Count == 0)
                return;

            var originalIdToNewNode = new Dictionary<int, FuNode>();

            // 1) Recreate nodes from clipboard payloads
            foreach (var kvp in _clipNodes)
            {
                var payload = kvp.Value;

                // Recreate fresh node (this will generate NEW node + port IDs)
                FuNode newNode = Registry.CreateNode(payload.NodeTypeId, this);
                newNode.Deserialize(payload.Json);

                // Ensure a fresh unique Id anyway
                var oldNewId = newNode.Id;
                newNode.Id = FuNodeId.New();

                // Place with offset relative to requested paste position
                newNode.SetPosition(position.x + payload.OffsetX, position.y + payload.OffsetY);

                Nodes.Add(newNode);
                originalIdToNewNode[payload.OriginalNodeId] = newNode;
            }

            // 2) Recreate edges by resolving ports by NAME on the newly created nodes
            foreach (var clipEdge in _clipEdges)
            {
                if (!originalIdToNewNode.TryGetValue(clipEdge.FromOriginalNodeId, out var fromNode))
                    continue;
                if (!originalIdToNewNode.TryGetValue(clipEdge.ToOriginalNodeId, out var toNode))
                    continue;

                var fromPortId = GetPortIdByName(fromNode, clipEdge.FromPortName);
                var toPortId = GetPortIdByName(toNode, clipEdge.ToPortName);
                if (fromPortId == 0 || toPortId == 0)
                    continue;

                Edges.Add(new FuNodalEdge
                {
                    FromNodeId = fromNode.Id,
                    FromPortId = fromPortId,
                    ToNodeId = toNode.Id,
                    ToPortId = toPortId
                });
            }

            _isDirty = true;
        }

        /// <summary>
        /// Return the NAME of a port on a given node from its ID. Null if not found.
        /// </summary>
        private string GetPortNameById(FuNode node, int portId)
        {
            // Adjust this to your real API (e.g., node.Inputs/Outputs or node.Ports)
            foreach (var p in node.Ports)
                if (p.Value.Id == portId)
                    return p.Value.Name;
            return null;
        }

        /// <summary>
        /// Return the ID of a port on a given node from its NAME. int.Empty if not found.
        /// </summary>
        private int GetPortIdByName(FuNode node, string portName)
        {
            // Adjust this to your real API (e.g., node.Inputs/Outputs or node.Ports)
            foreach (var p in node.Ports)
                if (string.Equals(p.Value.Name, portName, StringComparison.Ordinal))
                    return p.Value.Id;
            return 0;
        }
        #endregion

        #region Registery
        /// <summary>
        /// Set the nodal registry for this graph.
        /// </summary>
        /// <param name="registry"> The nodal registry to set.</param>
        public void SetRegistry(FuNodalRegistry registry)
        {
            Registry = registry;
        }
        #endregion

        /// <summary>
        /// Delete a node and all its associated edges.
        /// </summary>
        /// <param name="nodeId">The node to delete.</param>
        public void DeleteNode(int nodeId)
        {
            // Remove all edges connected to the node
            List<FuNodalEdge> edgesToRemove = Edges.Where(e => e.FromNodeId == nodeId || e.ToNodeId == nodeId).ToList();
            foreach (var edge in edgesToRemove)
            {
                DeleteEdge(edge);
            }

            // Remove the node itself
            FuNode fuNode = GetNode(nodeId);
            if (fuNode != null)
                Nodes.Remove(fuNode);
            _isDirty = true;
        }

        /// <summary>
        /// Add a node to the graph if it's not already present.
        /// </summary>
        /// <param name="node"> The node to add.</param>
        public void AddNode(FuNode node)
        {
            if (!Nodes.Contains(node))
                Nodes.Add(node);
        }
    }
}