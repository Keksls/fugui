using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fu.Framework
{
    /// <summary>
    /// Represents the Fu Nodal Graph type.
    /// </summary>
    public sealed class FuNodalGraph
    {
        #region State
        public string Version { get; set; } = "1.0.0";
        public int Id { get; set; } = FuNodeId.New();
        public string Name { get; set; } = "New Graph";
        public List<FuNode> Nodes { get; set; } = new List<FuNode>();
        public List<FuNodalEdge> Edges { get; set; } = new List<FuNodalEdge>();
        public FuNodalRegistry Registry { get; private set; } = new FuNodalRegistry();

        private bool _isDirty = false;
        private List<FuNode> _clipboardNodes = new List<FuNode>();
        private List<FuNodalEdge> _clipboardEdges = new List<FuNodalEdge>();
        #endregion

        #region Methods
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
            if (a == null || b == null || Nodes == null || Edges == null)
            {
                Debug.LogWarning("[Nodal] Cannot create a link with null graph data or ports.");
                return false;
            }
            if (Edges.Any(edge => edge == null))
            {
                Debug.LogWarning("[Nodal] Cannot mutate a graph that contains null edges.");
                return false;
            }

            // always OUT→IN
            if (a.Direction == FuNodalPortDirection.In && b.Direction == FuNodalPortDirection.Out)
            {
                (a, b) = (b, a);
                (aNodeId, bNodeId) = (bNodeId, aNodeId);
            }

            // Check OUT→IN valid direction
            if (a.Direction != FuNodalPortDirection.Out || b.Direction != FuNodalPortDirection.In)
                return false;

            FuNode outNode = GetNode(aNodeId);
            FuNode inNode = GetNode(bNodeId);
            if (outNode == null ||
                inNode == null ||
                !outNode.Ports.Values.Any(port => ReferenceEquals(port, a)) ||
                !inNode.Ports.Values.Any(port => ReferenceEquals(port, b)))
            {
                Debug.LogWarning("[Nodal] Cannot create a link with nodes or ports that do not belong to this graph.");
                return false;
            }

            // Check if already connected
            bool alreadyExists = Edges.Exists(e =>
                e != null &&
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
            if (!inNode.CanConnect(a, b))
            {
                Debug.LogWarning("[Nodal] Cannot create link as ports are not compatible.");
                return false;
            }

            // check allowed types
            if (b.AllowedTypes != null && b.AllowedTypes.Count > 0 && !b.AllowedTypes.Contains(a.DataType))
            {
                Debug.LogWarning("[Nodal] Cannot create link as port data types are not compatible.");
                return false;
            }

            // Build the complete replacement collection before publishing any mutation.
            List<FuNodalEdge> committedEdges = new List<FuNodalEdge>(Edges.Count + 1);
            List<FuNodalEdge> replacedEdges = new List<FuNodalEdge>();
            foreach (FuNodalEdge existingEdge in Edges)
            {
                bool replacesInput =
                    b.Multiplicity == FuNodalMultiplicity.Single &&
                    existingEdge.ToNodeId == bNodeId &&
                    existingEdge.ToPortId == b.Id;
                bool replacesOutput =
                    a.Multiplicity == FuNodalMultiplicity.Single &&
                    existingEdge.FromNodeId == aNodeId &&
                    existingEdge.FromPortId == a.Id;
                if (replacesInput || replacesOutput)
                {
                    replacedEdges.Add(existingEdge);
                }
                else
                {
                    committedEdges.Add(existingEdge);
                }
            }

            committedEdges.Add(new FuNodalEdge
            {
                FromNodeId = aNodeId,
                FromPortId = a.Id,
                ToNodeId = bNodeId,
                ToPortId = b.Id
            });

            Dictionary<FuNodalPort, (string DataType, object Data)> affectedPorts =
                new Dictionary<FuNodalPort, (string DataType, object Data)>();
            foreach (FuNodalEdge replacedEdge in replacedEdges)
            {
                if (replacedEdge.ToNodeId == bNodeId && replacedEdge.ToPortId == b.Id)
                {
                    continue;
                }

                FuNodalPort affectedPort = GetNode(replacedEdge.ToNodeId)?.GetPort(replacedEdge.ToPortId);
                if (affectedPort != null && !affectedPorts.ContainsKey(affectedPort))
                {
                    affectedPorts.Add(affectedPort, (affectedPort.DataType, affectedPort.Data));
                }
            }

            string previousInputDataType = b.DataType;
            object previousInputData = b.Data;
            List<FuNodalEdge> previousEdges = Edges;
            try
            {
                // Publish the replacement collection before reconciling ports disconnected by a single output.
                Edges = committedEdges;
                b.DataType = a.DataType;
                b.Data = a.Data;
                foreach (FuNodalPort affectedPort in affectedPorts.Keys)
                {
                    FuNode owner = Nodes.FirstOrDefault(node =>
                        node.Ports.Values.Any(port => ReferenceEquals(port, affectedPort)));
                    SynchronizeInputPort(owner, affectedPort);
                }
            }
            catch
            {
                Edges = previousEdges;
                b.DataType = previousInputDataType;
                b.Data = previousInputData;
                foreach (KeyValuePair<FuNodalPort, (string DataType, object Data)> portState in affectedPorts)
                {
                    portState.Key.DataType = portState.Value.DataType;
                    portState.Key.Data = portState.Value.Data;
                }
                throw;
            }

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
            if (fromNodeId == toNodeId)
            {
                return true;
            }

            HashSet<int> visitedNodeIds = new HashSet<int>();
            Stack<int> nodesToVisit = new Stack<int>();
            nodesToVisit.Push(toNodeId);
            while (nodesToVisit.Count > 0)
            {
                int currentNodeId = nodesToVisit.Pop();
                if (!visitedNodeIds.Add(currentNodeId))
                {
                    continue;
                }
                if (currentNodeId == fromNodeId)
                {
                    return true;
                }

                foreach (FuNodalEdge edge in Edges)
                {
                    if (edge != null && edge.FromNodeId == currentNodeId)
                    {
                        nodesToVisit.Push(edge.ToNodeId);
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Delete an edge from the graph and optionally recompute the graph
        /// </summary>
        /// <param name="edge"> The edge to delete.</param>
        public void DeleteEdge(FuNodalEdge edge)
        {
            if (edge == null || Edges == null)
            {
                return;
            }

            int edgeIndex = Edges.IndexOf(edge);
            if (edgeIndex < 0)
            {
                return;
            }

            FuNode toNode = GetNode(edge.ToNodeId);
            FuNodalPort toPort = toNode?.Ports.Values.FirstOrDefault(port => port.Id == edge.ToPortId);
            string previousDataType = toPort?.DataType;
            object previousData = toPort?.Data;
            List<FuNodalEdge> previousEdges = Edges;
            List<FuNodalEdge> committedEdges = new List<FuNodalEdge>(Edges);
            committedEdges.RemoveAt(edgeIndex);

            try
            {
                // Publish the edge removal before deriving the surviving input value.
                Edges = committedEdges;
                SynchronizeInputPort(toNode, toPort);
                _isDirty = true;
            }
            catch
            {
                // Restore graph connectivity and port state if custom default logic fails.
                Edges = previousEdges;
                if (toPort != null)
                {
                    toPort.DataType = previousDataType;
                    toPort.Data = previousData;
                }
                throw;
            }
        }

        /// <summary>
        /// Recomputes an input port from a surviving edge or restores its node-defined default.
        /// </summary>
        /// <param name="toNode">Node that owns the input port.</param>
        /// <param name="toPort">Input port to synchronize.</param>
        private void SynchronizeInputPort(FuNode toNode, FuNodalPort toPort)
        {
            // Missing owners can only occur while a node itself is being removed.
            if (toNode == null || toPort == null)
            {
                return;
            }

            FuNodalEdge survivingEdge = Edges.LastOrDefault(candidate =>
                candidate != null &&
                candidate.ToNodeId == toNode.Id &&
                candidate.ToPortId == toPort.Id);
            if (survivingEdge == null)
            {
                toNode.SetDefaultValues(toPort);
                return;
            }

            FuNode fromNode = GetNode(survivingEdge.FromNodeId);
            FuNodalPort fromPort = fromNode?.Ports.Values.FirstOrDefault(port => port.Id == survivingEdge.FromPortId);
            if (fromPort == null)
            {
                toNode.SetDefaultValues(toPort);
                return;
            }

            toPort.DataType = fromPort.DataType;
            toPort.Data = fromPort.Data;
        }

        /// <summary>
        /// Removes every edge attached to a port as one rollback-safe graph mutation.
        /// </summary>
        /// <param name="node">Node that owns the port.</param>
        /// <param name="port">Port about to be removed from its node.</param>
        internal void DisconnectPort(FuNode node, FuNodalPort port)
        {
            if (node == null ||
                port == null ||
                !ReferenceEquals(node.Graph, this) ||
                !node.Ports.Values.Any(candidate => ReferenceEquals(candidate, port)))
            {
                throw new ArgumentException("The port does not belong to this graph.");
            }
            if (!Nodes.Contains(node))
            {
                // Registry-created staging nodes cannot have graph edges before AddNode commits ownership.
                return;
            }

            List<FuNodalEdge> previousEdges = Edges;
            List<FuNodalEdge> committedEdges = Edges.Where(edge =>
                edge != null &&
                !(edge.FromNodeId == node.Id && edge.FromPortId == port.Id) &&
                !(edge.ToNodeId == node.Id && edge.ToPortId == port.Id)).ToList();
            Dictionary<FuNodalPort, (string DataType, object Data)> affectedPorts =
                new Dictionary<FuNodalPort, (string DataType, object Data)>();

            foreach (FuNodalEdge edge in Edges)
            {
                if (edge == null ||
                    edge.FromNodeId != node.Id ||
                    edge.FromPortId != port.Id ||
                    edge.ToNodeId == node.Id)
                {
                    continue;
                }

                FuNodalPort destinationPort = GetNode(edge.ToNodeId)?.GetPort(edge.ToPortId);
                if (destinationPort != null && !affectedPorts.ContainsKey(destinationPort))
                {
                    affectedPorts.Add(destinationPort, (destinationPort.DataType, destinationPort.Data));
                }
            }

            try
            {
                // Publish the disconnected edge set before deriving destination defaults.
                Edges = committedEdges;
                foreach (FuNodalPort affectedPort in affectedPorts.Keys)
                {
                    FuNode owner = Nodes.FirstOrDefault(candidate =>
                        candidate.Ports.Values.Any(candidatePort => ReferenceEquals(candidatePort, affectedPort)));
                    SynchronizeInputPort(owner, affectedPort);
                }
                _isDirty = true;
            }
            catch
            {
                Edges = previousEdges;
                foreach (KeyValuePair<FuNodalPort, (string DataType, object Data)> portState in affectedPorts)
                {
                    portState.Key.DataType = portState.Value.DataType;
                    portState.Key.Data = portState.Value.Data;
                }
                throw;
            }
        }

        /// <summary>
        /// Compute the whole graph in a valid dependency order (inputs before consumers).
        /// Starts from nodes with no incoming edges, then propagates.
        /// Logs a warning if a cycle is detected and computes only the acyclic part.
        /// </summary>
        public void ComputeGraphIfDirty()
        {
            if (!IsDirty())
                return;

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
                            // nodes outputs are calculated into compute
                            node.Compute();

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

        /// <summary>
        /// Copy selected nodes and their edges to the clipboard.
        /// </summary>
        /// <param name="nodesToCopy"> The set of node IDs to copy.</param>
        public void CopyNodes(HashSet<int> nodesToCopy)
        {
            if (nodesToCopy == null)
            {
                throw new ArgumentNullException(nameof(nodesToCopy));
            }

            var nodesList = Nodes.Where(n => nodesToCopy.Contains(n.Id)).ToList();
            CopyNodesToClipboard(nodesList);
        }
        #endregion

        #region Nested Types
        /// <summary>
        /// Lightweight clipboard node payload.
        /// </summary>
        private sealed class ClipboardNode
        {
            #region State
            public int OriginalNodeId { get; set; }
            public string NodeTypeId { get; set; }
            public string Json { get; set; }
            public float OffsetX { get; set; }
            public float OffsetY { get; set; }
            #endregion
        }

        /// <summary>
        /// Clipboard edge that references ports by NAME (stable) instead of IDs.
        /// </summary>
        private sealed class ClipboardEdge
        {
            #region State
            public int FromOriginalNodeId { get; set; }
            public string FromPortName { get; set; }
            public int ToOriginalNodeId { get; set; }
            public string ToPortName { get; set; }
            #endregion
        }
        #endregion

        #region State
        private readonly Dictionary<int, ClipboardNode> _clipNodes = new Dictionary<int, ClipboardNode>();
        private readonly List<ClipboardEdge> _clipEdges = new List<ClipboardEdge>();
        #endregion

        #region Methods
        /// <summary>
        /// Copy a set of nodes and their internal edges to the clipboard.
        /// Ports are recorded by NAME so they can be resolved after re-instantiation.
        /// </summary>
        public void CopyNodesToClipboard(List<FuNode> nodesToCopy)
        {
            if (nodesToCopy == null || nodesToCopy.Count == 0)
            {
                _clipNodes.Clear();
                _clipEdges.Clear();
                return;
            }
            if (nodesToCopy.Any(node => node == null || !Nodes.Contains(node)))
            {
                throw new ArgumentException("Every copied node must belong to this graph.", nameof(nodesToCopy));
            }

            // Normalize positions: top-left => (0,0)
            float minX = nodesToCopy.Min(n => n.x);
            float minY = nodesToCopy.Min(n => n.y);

            // Build a quick lookup of selected node IDs
            var selectedIds = new HashSet<int>(nodesToCopy.Select(n => n.Id));
            Dictionary<int, ClipboardNode> stagedNodes = new Dictionary<int, ClipboardNode>();
            List<ClipboardEdge> stagedEdges = new List<ClipboardEdge>();

            // 1) Store nodes as type + json + relative position
            foreach (var node in nodesToCopy)
            {
                string nodeTypeId = Registry.GetNodeTypeId(node);
                if (string.IsNullOrWhiteSpace(nodeTypeId))
                {
                    throw new InvalidOperationException($"Node '{node.Id}' has no registered node type.");
                }

                stagedNodes[node.Id] = new ClipboardNode
                {
                    OriginalNodeId = node.Id,
                    NodeTypeId = nodeTypeId,
                    Json = node.Serialize(),
                    OffsetX = node.x - minX,
                    OffsetY = node.y - minY
                };
            }

            // 2) Store edges among the selection, but by port NAME
            foreach (var edge in Edges)
            {
                if (edge == null)
                    continue;
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

                stagedEdges.Add(new ClipboardEdge
                {
                    FromOriginalNodeId = edge.FromNodeId,
                    FromPortName = fromPortName,
                    ToOriginalNodeId = edge.ToNodeId,
                    ToPortName = toPortName
                });
            }

            // Replace the clipboard only after every custom serializer has succeeded.
            _clipNodes.Clear();
            foreach (KeyValuePair<int, ClipboardNode> pair in stagedNodes)
            {
                _clipNodes.Add(pair.Key, pair.Value);
            }
            _clipEdges.Clear();
            _clipEdges.AddRange(stagedEdges);
        }

        /// <summary>
        /// Paste nodes and edges stored in the clipboard at a given position.
        /// Nodes are recreated from type+json; edges are rebuilt by resolving ports by NAME.
        /// </summary>
        public void PasteNodes(Vector2 position)
        {
            if (_clipNodes.Count == 0)
                return;
            if (float.IsNaN(position.x) ||
                float.IsInfinity(position.x) ||
                float.IsNaN(position.y) ||
                float.IsInfinity(position.y))
            {
                throw new ArgumentOutOfRangeException(nameof(position), "Paste position must be finite.");
            }

            var originalIdToNewNode = new Dictionary<int, FuNode>();
            List<FuNode> stagedNodes = new List<FuNode>(_clipNodes.Count);
            List<FuNodalEdge> stagedEdges = new List<FuNodalEdge>(_clipEdges.Count);
            HashSet<int> allocatedNodeIds = new HashSet<int>(Nodes.Select(node => node.Id));

            // 1) Recreate nodes from clipboard payloads
            foreach (var kvp in _clipNodes)
            {
                var payload = kvp.Value;

                // Recreate fresh node (this will generate NEW node + port IDs)
                FuNode newNode = Registry.CreateNode(payload.NodeTypeId, this);
                if (newNode == null)
                {
                    throw new InvalidOperationException($"Clipboard node type '{payload.NodeTypeId}' is not registered.");
                }
                newNode.Deserialize(payload.Json);

                // Ensure a fresh unique Id anyway
                newNode.Id = FuNodeId.New();
                if (!allocatedNodeIds.Add(newNode.Id))
                {
                    throw new InvalidOperationException($"Generated duplicate node ID '{newNode.Id}'.");
                }

                // Place with offset relative to requested paste position
                newNode.SetPosition(position.x + payload.OffsetX, position.y + payload.OffsetY);
                if (!TryValidatePorts(newNode, out string validationError))
                {
                    throw new InvalidOperationException($"Cannot paste node '{payload.NodeTypeId}': {validationError}");
                }

                stagedNodes.Add(newNode);
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

                FuNodalPort fromPort = fromNode.GetPort(fromPortId);
                FuNodalPort toPort = toNode.GetPort(toPortId);
                if (fromPort == null ||
                    toPort == null ||
                    fromPort.Direction != FuNodalPortDirection.Out ||
                    toPort.Direction != FuNodalPortDirection.In ||
                    (toPort.AllowedTypes.Count > 0 && !toPort.AllowedTypes.Contains(fromPort.DataType)))
                {
                    throw new InvalidOperationException("Clipboard contains an invalid nodal connection.");
                }

                stagedEdges.Add(new FuNodalEdge
                {
                    FromNodeId = fromNode.Id,
                    FromPortId = fromPortId,
                    ToNodeId = toNode.Id,
                    ToPortId = toPortId
                });
            }

            // Allocate both replacement collections before publishing the paste.
            List<FuNode> committedNodes = new List<FuNode>(Nodes.Count + stagedNodes.Count);
            committedNodes.AddRange(Nodes);
            committedNodes.AddRange(stagedNodes);
            List<FuNodalEdge> committedEdges = new List<FuNodalEdge>(Edges.Count + stagedEdges.Count);
            committedEdges.AddRange(Edges);
            committedEdges.AddRange(stagedEdges);
            Nodes = committedNodes;
            Edges = committedEdges;
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

        /// <summary>
        /// Set the nodal registry for this graph.
        /// </summary>
        /// <param name="registry"> The nodal registry to set.</param>
        public void SetRegistry(FuNodalRegistry registry)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            FuNodalRegistry previousRegistry = Registry;
            Registry = registry;
            if (Nodes != null &&
                Nodes.Count > 0 &&
                !TryValidate(out string validationError))
            {
                // Registry replacement is committed only if it understands the complete live graph.
                Registry = previousRegistry;
                throw new ArgumentException($"Registry is incompatible with this graph: {validationError}", nameof(registry));
            }
        }

        /// <summary>
        /// Delete a node and all its associated edges.
        /// </summary>
        /// <param name="nodeId">The node to delete.</param>
        public void DeleteNode(int nodeId)
        {
            FuNode node = GetNode(nodeId);
            if (node == null)
            {
                return;
            }

            List<FuNode> previousNodes = Nodes;
            List<FuNodalEdge> previousEdges = Edges;
            List<FuNode> committedNodes = Nodes.Where(candidate => !ReferenceEquals(candidate, node)).ToList();
            List<FuNodalEdge> committedEdges = Edges
                .Where(edge => edge != null && edge.FromNodeId != nodeId && edge.ToNodeId != nodeId)
                .ToList();
            Dictionary<FuNodalPort, (string DataType, object Data)> affectedPorts =
                new Dictionary<FuNodalPort, (string DataType, object Data)>();

            foreach (FuNodalEdge edge in Edges)
            {
                if (edge == null || edge.FromNodeId != nodeId || edge.ToNodeId == nodeId)
                {
                    continue;
                }

                FuNode destinationNode = GetNode(edge.ToNodeId);
                FuNodalPort destinationPort = destinationNode?.GetPort(edge.ToPortId);
                if (destinationPort != null && !affectedPorts.ContainsKey(destinationPort))
                {
                    affectedPorts.Add(destinationPort, (destinationPort.DataType, destinationPort.Data));
                }
            }

            try
            {
                // Publish the replacement graph, then reconcile only surviving destination ports.
                Nodes = committedNodes;
                Edges = committedEdges;
                foreach (FuNodalPort affectedPort in affectedPorts.Keys)
                {
                    FuNode owner = Nodes.FirstOrDefault(candidate =>
                        candidate.Ports.Values.Any(port => ReferenceEquals(port, affectedPort)));
                    SynchronizeInputPort(owner, affectedPort);
                }

                node.Graph = null;
                _isDirty = true;
            }
            catch
            {
                Nodes = previousNodes;
                Edges = previousEdges;
                node.Graph = this;
                foreach (KeyValuePair<FuNodalPort, (string DataType, object Data)> portState in affectedPorts)
                {
                    portState.Key.DataType = portState.Value.DataType;
                    portState.Key.Data = portState.Value.Data;
                }
                throw;
            }
        }

        /// <summary>
        /// Add a node to the graph if it's not already present.
        /// </summary>
        /// <param name="node"> The node to add.</param>
        public void AddNode(FuNode node)
        {
            if (!TryValidateNodeForInsertion(node, out string validationError))
            {
                throw new ArgumentException(validationError, nameof(node));
            }

            // Node ownership is committed only after all identifiers and ports are known to be valid.
            node.Graph = this;
            Nodes.Add(node);
            _isDirty = true;
        }

        /// <summary>
        /// Validates a node before it is attached to this graph.
        /// </summary>
        /// <param name="node">Candidate node to validate.</param>
        /// <param name="validationError">Description of the first failed invariant.</param>
        /// <returns>True when the node can be inserted safely.</returns>
        private bool TryValidateNodeForInsertion(FuNode node, out string validationError)
        {
            // Validate every externally writable identifier before taking graph ownership.
            if (node == null)
            {
                validationError = "Cannot add a null node.";
                return false;
            }
            if (Nodes == null)
            {
                validationError = "The graph node collection is null.";
                return false;
            }
            if (node.Graph != null && node.Graph != this)
            {
                validationError = $"Node '{node.Id}' already belongs to another graph.";
                return false;
            }
            if (node.Id <= 0 || Nodes.Any(existingNode => existingNode != null && existingNode.Id == node.Id))
            {
                validationError = $"Node ID '{node.Id}' is invalid or already used.";
                return false;
            }
            if (!TryValidatePorts(node, out validationError))
            {
                return false;
            }

            validationError = null;
            return true;
        }

        /// <summary>
        /// Validates the identifiers and metadata of every port owned by a node.
        /// </summary>
        /// <param name="node">Node whose ports are validated.</param>
        /// <param name="validationError">Description of the first failed invariant.</param>
        /// <returns>True when every port is internally consistent.</returns>
        private static bool TryValidatePorts(FuNode node, out string validationError)
        {
            // Port names and IDs are the stable keys used by links and serialization.
            if (node.Ports == null)
            {
                validationError = $"Node '{node.Id}' has a null port collection.";
                return false;
            }

            HashSet<int> portIds = new HashSet<int>();
            foreach (KeyValuePair<string, FuNodalPort> pair in node.Ports)
            {
                FuNodalPort port = pair.Value;
                if (port == null)
                {
                    validationError = $"Node '{node.Id}' contains a null port.";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(pair.Key) ||
                    !string.Equals(pair.Key, port.Name, StringComparison.Ordinal))
                {
                    validationError = $"Node '{node.Id}' contains a port with an inconsistent name.";
                    return false;
                }
                if (port.Id <= 0 || !portIds.Add(port.Id))
                {
                    validationError = $"Node '{node.Id}' contains an invalid or duplicate port ID '{port.Id}'.";
                    return false;
                }
                if (!Enum.IsDefined(typeof(FuNodalPortDirection), port.Direction) ||
                    !Enum.IsDefined(typeof(FuNodalMultiplicity), port.Multiplicity))
                {
                    validationError = $"Port '{port.Name}' on node '{node.Id}' has invalid connection metadata.";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(port.DataType))
                {
                    validationError = $"Port '{port.Name}' on node '{node.Id}' has no data type.";
                    return false;
                }
                if (port.AllowedTypes == null)
                {
                    validationError = $"Port '{port.Name}' on node '{node.Id}' has a null allowed-types collection.";
                    return false;
                }
            }

            validationError = null;
            return true;
        }

        /// <summary>
        /// Validates the complete graph without changing nodes, ports or edges.
        /// </summary>
        /// <param name="validationError">Description of the first failed invariant.</param>
        /// <returns>True when the graph is safe to compute, edit and serialize.</returns>
        public bool TryValidate(out string validationError)
        {
            // Build validated lookup tables once, then use them for all edge invariants.
            if (Registry == null)
            {
                validationError = "The graph registry is null.";
                return false;
            }
            if (Nodes == null || Edges == null)
            {
                validationError = "The graph node or edge collection is null.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(Version) || string.IsNullOrWhiteSpace(Name))
            {
                validationError = "The graph version or name is empty.";
                return false;
            }
            if (Id <= 0)
            {
                validationError = $"Graph ID '{Id}' is invalid.";
                return false;
            }

            Dictionary<int, FuNode> nodesById = new Dictionary<int, FuNode>();
            foreach (FuNode node in Nodes)
            {
                if (node == null)
                {
                    validationError = "The graph contains a null node.";
                    return false;
                }
                if (node.Id <= 0 || nodesById.ContainsKey(node.Id))
                {
                    validationError = $"Node ID '{node.Id}' is invalid or duplicated.";
                    return false;
                }
                if (!ReferenceEquals(node.Graph, this))
                {
                    validationError = $"Node '{node.Id}' is not owned by this graph.";
                    return false;
                }
                if (float.IsNaN(node.x) ||
                    float.IsInfinity(node.x) ||
                    float.IsNaN(node.y) ||
                    float.IsInfinity(node.y))
                {
                    validationError = $"Node '{node.Id}' has an invalid position.";
                    return false;
                }
                if (!TryValidatePorts(node, out validationError))
                {
                    return false;
                }
                foreach (FuNodalPort port in node.Ports.Values)
                {
                    if (!Registry.HasRegisteredType(port.DataType))
                    {
                        validationError = $"Port '{port.Name}' on node '{node.Id}' uses unregistered type '{port.DataType}'.";
                        return false;
                    }
                    foreach (string allowedType in port.AllowedTypes)
                    {
                        if (string.IsNullOrWhiteSpace(allowedType) || !Registry.HasRegisteredType(allowedType))
                        {
                            validationError = $"Port '{port.Name}' on node '{node.Id}' allows unregistered type '{allowedType}'.";
                            return false;
                        }
                    }
                }

                nodesById.Add(node.Id, node);
            }

            HashSet<int> edgeIds = new HashSet<int>();
            HashSet<string> connections = new HashSet<string>(StringComparer.Ordinal);
            Dictionary<string, int> inputConnectionCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            Dictionary<string, int> outputConnectionCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            Dictionary<int, int> indegrees = Nodes.ToDictionary(node => node.Id, node => 0);
            Dictionary<int, List<int>> adjacency = Nodes.ToDictionary(node => node.Id, node => new List<int>());

            foreach (FuNodalEdge edge in Edges)
            {
                if (edge == null)
                {
                    validationError = "The graph contains a null edge.";
                    return false;
                }
                if (edge.Id <= 0 || !edgeIds.Add(edge.Id))
                {
                    validationError = $"Edge ID '{edge.Id}' is invalid or duplicated.";
                    return false;
                }
                if (!nodesById.TryGetValue(edge.FromNodeId, out FuNode fromNode) ||
                    !nodesById.TryGetValue(edge.ToNodeId, out FuNode toNode))
                {
                    validationError = $"Edge '{edge.Id}' references a missing node.";
                    return false;
                }

                FuNodalPort fromPort = fromNode.GetPort(edge.FromPortId);
                FuNodalPort toPort = toNode.GetPort(edge.ToPortId);
                if (fromPort == null || toPort == null)
                {
                    validationError = $"Edge '{edge.Id}' references a missing port.";
                    return false;
                }
                if (fromPort.Direction != FuNodalPortDirection.Out ||
                    toPort.Direction != FuNodalPortDirection.In)
                {
                    validationError = $"Edge '{edge.Id}' does not connect an output to an input.";
                    return false;
                }

                string connectionKey = $"{edge.FromNodeId}:{edge.FromPortId}>{edge.ToNodeId}:{edge.ToPortId}";
                if (!connections.Add(connectionKey))
                {
                    validationError = $"Connection '{connectionKey}' is duplicated.";
                    return false;
                }
                string inputKey = $"{edge.ToNodeId}:{edge.ToPortId}";
                inputConnectionCounts.TryGetValue(inputKey, out int inputCount);
                inputCount++;
                inputConnectionCounts[inputKey] = inputCount;
                if (toPort.Multiplicity == FuNodalMultiplicity.Single && inputCount > 1)
                {
                    validationError = $"Single input '{inputKey}' has more than one connection.";
                    return false;
                }
                string outputKey = $"{edge.FromNodeId}:{edge.FromPortId}";
                outputConnectionCounts.TryGetValue(outputKey, out int outputCount);
                outputCount++;
                outputConnectionCounts[outputKey] = outputCount;
                if (fromPort.Multiplicity == FuNodalMultiplicity.Single && outputCount > 1)
                {
                    validationError = $"Single output '{outputKey}' has more than one connection.";
                    return false;
                }
                if (toPort.AllowedTypes.Count > 0 && !toPort.AllowedTypes.Contains(fromPort.DataType))
                {
                    validationError = $"Edge '{edge.Id}' connects incompatible data types.";
                    return false;
                }
                bool canConnect;
                try
                {
                    canConnect = toNode.CanConnect(fromPort, toPort);
                }
                catch (Exception exception)
                {
                    validationError = $"Node '{toNode.Id}' failed to validate edge '{edge.Id}': {exception.Message}";
                    return false;
                }
                if (!canConnect)
                {
                    validationError = $"Edge '{edge.Id}' is rejected by node '{toNode.Id}'.";
                    return false;
                }

                indegrees[toNode.Id]++;
                adjacency[fromNode.Id].Add(toNode.Id);
            }

            Queue<int> readyNodes = new Queue<int>(indegrees.Where(pair => pair.Value == 0).Select(pair => pair.Key));
            int visitedCount = 0;
            while (readyNodes.Count > 0)
            {
                int nodeId = readyNodes.Dequeue();
                visitedCount++;
                foreach (int childNodeId in adjacency[nodeId])
                {
                    indegrees[childNodeId]--;
                    if (indegrees[childNodeId] == 0)
                    {
                        readyNodes.Enqueue(childNodeId);
                    }
                }
            }
            if (visitedCount != Nodes.Count)
            {
                validationError = "The graph contains a cycle.";
                return false;
            }

            validationError = null;
            return true;
        }

        /// <summary>
        /// Replaces this graph state with a fully validated staging graph.
        /// </summary>
        /// <param name="stagingGraph">Validated graph whose state is committed.</param>
        internal void CommitValidatedState(FuNodalGraph stagingGraph)
        {
            if (stagingGraph == null)
            {
                throw new ArgumentNullException(nameof(stagingGraph));
            }
            if (!stagingGraph.TryValidate(out string validationError))
            {
                throw new InvalidOperationException($"Cannot commit an invalid graph: {validationError}");
            }

            // Ownership is redirected before the collection references become publicly visible.
            foreach (FuNode node in stagingGraph.Nodes)
            {
                node.Graph = this;
            }

            Version = stagingGraph.Version;
            Id = stagingGraph.Id;
            Name = stagingGraph.Name;
            Nodes = stagingGraph.Nodes;
            Edges = stagingGraph.Edges;
            ClearDirty();
        }
        #endregion
    }
}
