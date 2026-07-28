using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fu.Framework
{
        /// <summary>
        /// Convert between runtime graph and DTO/JSON.
        /// </summary>
        public static class FuGraphSerializer
        {
            #region Methods
            /// <summary>
            /// Export a runtime graph to a portable JSON string.
            /// </summary>
            public static string ToJson(this FuNodalGraph graph)
            {
                if (graph == null)
                {
                    throw new ArgumentNullException(nameof(graph));
                }
                if (!graph.TryValidate(out string validationError))
                {
                    throw new InvalidOperationException($"Cannot serialize an invalid nodal graph: {validationError}");
                }

                var dto = new FuGraphDto();

                // Basic graph info
                dto.Version = graph.Version;
                dto.Id = graph.Id;
                dto.Name = graph.Name;

                // Nodes
                foreach (var n in graph.Nodes)
                {
                    string nodeType = graph.Registry.GetNodeTypeId(n);
                    if (string.IsNullOrWhiteSpace(nodeType))
                    {
                        throw new InvalidOperationException($"Node '{n.Id}' has no registered node type.");
                    }

                    var nDto = new FuNodeDto
                    {
                        Id = n.Id,
                        NodeType = nodeType,
                        CustomNodeDataJson = JsonConvert.SerializeObject(n.Serialize()),
                        X = n.x,
                        Y = n.y
                    };

                    // Ports
                    foreach (var p in n.Ports.Values)
                    {
                        var pDto = new FuPortDto
                        {
                            Id = p.Id,
                            Name = p.Name,
                            Direction = p.Direction,
                            Multiplicity = p.Multiplicity,
                            AllowedTypes = p.AllowedTypes?.ToList() ?? new List<string>(),
                            DataType = p.DataType,
                            DataJson = FuPortValueSerializer.ToJson(graph, p.DataType, p.Data)
                        };
                        nDto.Ports.Add(pDto);
                    }

                    dto.Nodes.Add(nDto);
                }

                // Edges are already flat and serializable
                dto.Edges.AddRange(graph.Edges);

                return JsonConvert.SerializeObject(dto, Formatting.Indented);
            }

            /// <summary>
            /// Import a runtime graph from a JSON string.
            /// </summary>
            public static void FromJson(this FuNodalGraph graph, string dtoJson)
            {
                if (graph == null)
                {
                    throw new ArgumentNullException(nameof(graph));
                }
                if (string.IsNullOrWhiteSpace(dtoJson))
                {
                    throw new ArgumentException("Graph JSON cannot be null or empty.", nameof(dtoJson));
                }

                FuGraphDto dto = JsonConvert.DeserializeObject<FuGraphDto>(dtoJson);
                if (dto == null)
                {
                    throw new InvalidOperationException("Graph JSON did not contain a graph object.");
                }
                if (dto.Nodes == null || dto.Edges == null)
                {
                    throw new InvalidOperationException("Graph JSON contains null node or edge collections.");
                }

                // Build the complete graph in isolation so a failure cannot erase the live graph.
                FuNodalGraph stagingGraph = new FuNodalGraph
                {
                    Version = dto.Version,
                    Id = dto.Id,
                    Name = dto.Name
                };
                stagingGraph.SetRegistry(graph.Registry);
                var nodeMap = new Dictionary<int, FuNode>();

                // Ensure registry has all needed types/nodes
                foreach (var nDto in dto.Nodes)
                {
                    if (nDto == null)
                    {
                        throw new InvalidOperationException("Graph JSON contains a null node.");
                    }
                    if (nDto.Ports == null)
                    {
                        throw new InvalidOperationException($"Node '{nDto.Id}' has a null port collection.");
                    }
                    if (nDto.Id <= 0)
                    {
                        throw new InvalidOperationException($"Node ID '{nDto.Id}' is invalid.");
                    }
                    if (!graph.Registry.HasRegisteredNode(nDto.NodeType))
                    {
                        throw new Exception($"Node type '{nDto.NodeType}' not registered in NodeRegistry. Cannot deserialize graph.\n" +
                            $"Please ensure all custom nodes are registered before loading the graph.\n" +
                            $"The graph must have a registry that match the one used to serialize it.");
                    }

                    HashSet<string> portNames = new HashSet<string>(StringComparer.Ordinal);
                    HashSet<int> portIds = new HashSet<int>();
                    foreach (var pDto in nDto.Ports)
                    {
                        if (pDto == null)
                        {
                            throw new InvalidOperationException($"Node '{nDto.Id}' contains a null port.");
                        }
                        if (string.IsNullOrWhiteSpace(pDto.Name) || !portNames.Add(pDto.Name))
                        {
                            throw new InvalidOperationException($"Node '{nDto.Id}' contains an empty or duplicate port name.");
                        }
                        if (pDto.Id <= 0 || !portIds.Add(pDto.Id))
                        {
                            throw new InvalidOperationException($"Node '{nDto.Id}' contains invalid or duplicate port ID '{pDto.Id}'.");
                        }
                        if (!graph.Registry.HasRegisteredType(pDto.DataType))
                        {
                            throw new Exception($"Port data type '{pDto.DataType}' not registered in NodeRegistry. Cannot deserialize graph.\n" +
                            $"Please ensure all custom types are registered before loading the graph.\n" +
                                $"The graph must have a registry that match the one used to serialize it.");
                        }
                        foreach (string allowedType in pDto.AllowedTypes ?? Enumerable.Empty<string>())
                        {
                            if (string.IsNullOrWhiteSpace(allowedType) ||
                                !graph.Registry.HasRegisteredType(allowedType))
                            {
                                throw new InvalidOperationException(
                                    $"Port '{pDto.Name}' uses unregistered allowed type '{allowedType}'.");
                            }
                        }
                    }
                }

                // 1) Instantiate nodes via NodeRegistry, create default ports
                foreach (var nDto in dto.Nodes)
                {
                    if (nodeMap.ContainsKey(nDto.Id))
                    {
                        throw new InvalidOperationException($"Node ID '{nDto.Id}' is duplicated.");
                    }

                    var node = graph.Registry.CreateNode(nDto.NodeType, stagingGraph);
                    if (node == null)
                    {
                        throw new InvalidOperationException($"Registered node type '{nDto.NodeType}' returned null.");
                    }

                    node.Deserialize(JsonConvert.DeserializeObject<string>(nDto.CustomNodeDataJson));

                    node.Id = nDto.Id;
                    node.SetPosition(nDto.X, nDto.Y);

                    // 2) Reconcile ports by name (safer than by order), then push values
                    foreach (var pDto in nDto.Ports)
                    {
                        FuNodalPort port;
                        if (!node.Ports.TryGetValue(pDto.Name, out port))
                        {
                            // Create a fully valid dynamic port before exposing it through the node API.
                            port = new FuNodalPort
                            {
                                Id = pDto.Id,
                                Name = pDto.Name,
                                Direction = pDto.Direction,
                                Multiplicity = pDto.Multiplicity,
                                AllowedTypes = new HashSet<string>(pDto.AllowedTypes ?? new List<string>()),
                                DataType = pDto.DataType,
                                Data = FuPortValueSerializer.FromJson(stagingGraph, pDto.DataType, pDto.DataJson)
                            };
                            node.AddPort(port);
                            if (!node.Ports.TryGetValue(pDto.Name, out port))
                            {
                                throw new InvalidOperationException(
                                    $"Node '{nDto.Id}' rejected serialized port '{pDto.Name}'.");
                            }
                        }

                        port.Id = pDto.Id;
                        port.Direction = pDto.Direction;
                        port.Multiplicity = pDto.Multiplicity;
                        port.AllowedTypes = new HashSet<string>(pDto.AllowedTypes ?? new List<string>());
                        port.DataType = pDto.DataType;
                        port.Data = FuPortValueSerializer.FromJson(stagingGraph, pDto.DataType, pDto.DataJson);
                    }

                    nodeMap[node.Id] = node;
                    stagingGraph.AddNode(node);
                }

                // 3) Restore edges into the isolated graph.
                foreach (var e in dto.Edges)
                {
                    if (e == null)
                    {
                        throw new InvalidOperationException("Graph JSON contains a null edge.");
                    }

                    stagingGraph.Edges.Add(new FuNodalEdge
                    {
                        Id = e.Id,
                        FromNodeId = e.FromNodeId,
                        FromPortId = e.FromPortId,
                        ToNodeId = e.ToNodeId,
                        ToPortId = e.ToPortId
                    });
                }

                if (!stagingGraph.TryValidate(out string validationError))
                {
                    throw new InvalidOperationException($"Cannot import an invalid nodal graph: {validationError}");
                }

                // 4) Commit only after construction and validation both succeeded.
                graph.CommitValidatedState(stagingGraph);
                FuNodeId.Sync(
                    graph.Nodes.Select(node => node.Id)
                        .Concat(graph.Nodes.SelectMany(node => node.Ports.Values.Select(port => port.Id)))
                        .Concat(graph.Edges.Select(edge => edge.Id)));
            }
            #endregion
        }
}
