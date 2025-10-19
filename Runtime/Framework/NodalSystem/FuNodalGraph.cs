using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fu.Framework
{
    public sealed class FuNodalGraph
    {
        public string Version { get; set; } = "1.0.0";
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "New Graph";
        public List<FuNode> Nodes { get; set; } = new List<FuNode>();
        public List<FuNodalEdge> Edges { get; set; } = new List<FuNodalEdge>();
        private bool _isDirty = false;

        /// <summary>
        /// Find a node by its unique identifier
        /// </summary>
        /// <param name="id"> The unique identifier of the node to find</param>
        /// <returns> The node with the specified identifier, or null if no such node exists</returns>
        public FuNode GetNode(Guid id)
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
        public bool TryConnect(FuNodalPort a, FuNodalPort b, Guid aNodeId, Guid bNodeId)
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

            // check allowed types
            if (b.AllowedTypes.Count > 0 && !b.AllowedTypes.Contains(a.DataType))
            {
                Debug.LogWarning("[Nodal] Cannot create link as port data types are not compatible.");
                return false;
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
        private bool CheckLinkCycle(Guid fromNodeId, Guid toNodeId)
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
                var adjacency = Nodes.ToDictionary(n => n.Id, n => new List<Guid>());

                for (int i = 0; i < Edges.Count; i++)
                {
                    var e = Edges[i];
                    if (!indegree.ContainsKey(e.ToNodeId) || !adjacency.ContainsKey(e.FromNodeId))
                        continue; // Edge references a missing node; ignore safely.

                    indegree[e.ToNodeId] += 1;
                    adjacency[e.FromNodeId].Add(e.ToNodeId);
                }

                // Initialize queue with nodes that have no inputs.
                var queue = new Queue<Guid>(indegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));

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
            var adjacency = Nodes.ToDictionary(n => n.Id, n => new List<Guid>());

            for (int i = 0; i < Edges.Count; i++)
            {
                var e = Edges[i];
                if (!indegree.ContainsKey(e.ToNodeId) || !adjacency.ContainsKey(e.FromNodeId))
                    continue;

                indegree[e.ToNodeId] += 1;
                adjacency[e.FromNodeId].Add(e.ToNodeId);
            }

            var queue = new Queue<Guid>(indegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
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

        /// <summary>
        /// Delete a node and all its associated edges.
        /// </summary>
        /// <param name="nodeId">The node to delete.</param>
        public void DeleteNode(Guid nodeId)
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