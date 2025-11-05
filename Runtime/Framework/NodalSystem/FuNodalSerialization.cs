using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fu.Framework
{
    #region DTOs
    /// <summary>
    /// Flat, serializable graph DTO.
    /// </summary>
    public sealed class FuGraphDto
    {
        public string Version { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public List<FuNodeDto> Nodes { get; set; } = new List<FuNodeDto>();
        public List<FuNodalEdge> Edges { get; set; } = new List<FuNodalEdge>();
    }

    /// <summary>
    /// Flat node DTO; NodeType is used to rebuild the concrete FuNode.
    /// </summary>
    public sealed class FuNodeDto
    {
        public int Id { get; set; }
        public string NodeType { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public string CustomNodeDataJson { get; set; }
        public List<FuPortDto> Ports { get; set; } = new List<FuPortDto>();
    }

    /// <summary>
    /// Flat port DTO; Data is stored as JSON by DataType.
    /// </summary>
    public sealed class FuPortDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public FuNodalPortDirection Direction { get; set; }
        public FuNodalMultiplicity Multiplicity { get; set; }
        public List<string> AllowedTypes { get; set; } = new List<string>();
        public string DataType { get; set; }
        public string DataJson { get; set; }
    }
    #endregion

    #region Port value serializer
    /// <summary>
    /// Serialize/Deserialize port values based on DataType.
    /// </summary>
    public static class FuPortValueSerializer
    {
        /// <summary>
        /// Serialize a port runtime value to JSON according to its DataType.
        /// </summary>
        public static string ToJson(FuNodalGraph graph, string dataType, object value)
        {
            if (value == null)
                return null;

            FuNodalType type = graph.Registry.GetType(dataType);
            if (type != null && type.SerializationFunc != null)
            {
                // Custom serialization if provided
                return type.SerializationFunc(value);
            }

            return JsonConvert.SerializeObject(value);
        }

        /// <summary>
        /// Deserialize JSON to a runtime value according to DataType.
        /// </summary>
        public static object FromJson(FuNodalGraph graph, string dataType, string json)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            FuNodalType type = graph.Registry.GetType(dataType);
            if (type != null && type.DeserializationFunc != null)
            {
                // Custom deserialization if provided
                return type.DeserializationFunc(json);
            }

            // Unknown types: return raw string or attempt object
            return JsonConvert.DeserializeObject<object>(json);
        }
    }
    #endregion

    #region Graph serializer
    /// <summary>
    /// Convert between runtime graph and DTO/JSON.
    /// </summary>
    public static class FuGraphSerializer
    {
        /// <summary>
        /// Export a runtime graph to a portable JSON string.
        /// </summary>
        public static string ToJson(this FuNodalGraph graph)
        {
            var dto = new FuGraphDto();

            // Basic graph info
            dto.Version = graph.Version;
            dto.Id = graph.Id;
            dto.Name = graph.Name;

            // Nodes
            foreach (var n in graph.Nodes)
            {
                var nDto = new FuNodeDto
                {
                    Id = n.Id,
                    NodeType = graph.Registry.GetNodeTypeId(n),
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
            FuGraphDto dto = JsonConvert.DeserializeObject<FuGraphDto>(dtoJson);
            var nodeMap = new Dictionary<int, FuNode>();

            // Basic graph info
            graph.Version = dto.Version;
            graph.Id = dto.Id;
            graph.Name = dto.Name;

            // Clear existing graph
            graph.Nodes.Clear();
            graph.Edges.Clear();

            // Ensure registry has all needed types/nodes
            foreach (var nDto in dto.Nodes)
            {
                if (!graph.Registry.HasRegisteredNode(nDto.NodeType))
                {
                    throw new Exception($"Node type '{nDto.NodeType}' not registered in NodeRegistry. Cannot deserialize graph.\n" +
                        $"Please ensure all custom nodes are registered before loading the graph.\n" +
                        $"The graph must have a registry that match the one used to serialize it.");
                }
                foreach (var pDto in nDto.Ports)
                {
                    if (!graph.Registry.HasRegisteredType(pDto.DataType))
                    {
                        throw new Exception($"Port data type '{pDto.DataType}' not registered in NodeRegistry. Cannot deserialize graph.\n" +
                        $"Please ensure all custom types are registered before loading the graph.\n" +
                        $"The graph must have a registry that match the one used to serialize it.");
                    }
                }
            }

            // 1) Instantiate nodes via NodeRegistry, create default ports
            foreach (var nDto in dto.Nodes)
            {
                var node = graph.Registry.CreateNode(nDto.NodeType, graph);
                if (node == null) continue;

                node.Deserialize(JsonConvert.DeserializeObject<string>(nDto.CustomNodeDataJson));

                node.Id = nDto.Id;
                node.SetPosition(nDto.X, nDto.Y);

                // 2) Reconcile ports by name (safer than by order), then push values
                foreach (var pDto in nDto.Ports)
                {
                    FuNodalPort port;
                    if (!node.Ports.TryGetValue(pDto.Name, out port))
                    {
                        // If the node created ports differently, create/patch one
                        port = new FuNodalPort { Name = pDto.Name };
                        node.AddPort(port);
                    }

                    port.Id = pDto.Id;
                    port.Direction = pDto.Direction;
                    port.Multiplicity = pDto.Multiplicity;
                    port.AllowedTypes = new HashSet<string>(pDto.AllowedTypes ?? new List<string>());
                    port.DataType = pDto.DataType;
                    port.Data = FuPortValueSerializer.FromJson(graph, pDto.DataType, pDto.DataJson);
                }

                nodeMap[node.Id] = node;
                graph.Nodes.Add(node);
            }

            // 3) Restore edges (already portable via GUIDs)
            foreach (var e in dto.Edges)
            {
                // Optional: validate node/port existence before adding
                graph.Edges.Add(e);
            }

            // 4) Ensure FuNodeId is synced
            FuNodeId.Sync(graph.Nodes.Select(n => n.Id));
        }
    }
    #endregion
}