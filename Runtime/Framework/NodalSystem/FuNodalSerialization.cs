using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fu.Framework
{
    #region DTOs
    /// <summary>
    /// Flat, serializable graph DTO.
    /// </summary>
    public sealed class FuGraphDto
    {
        public List<FuNodeDto> Nodes { get; set; } = new List<FuNodeDto>();
        public List<FuNodalEdge> Edges { get; set; } = new List<FuNodalEdge>();
    }

    /// <summary>
    /// Flat node DTO; NodeType is used to rebuild the concrete FuNode.
    /// </summary>
    public sealed class FuNodeDto
    {
        public Guid Id { get; set; }
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
        public Guid Id { get; set; }
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
        public static string ToJson(string dataType, object value)
        {
            if (value == null)
                return null;

            FuNodalType type = FuNodalRegistry.GetType(dataType);
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
        public static object FromJson(string dataType, string json)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            FuNodalType type = FuNodalRegistry.GetType(dataType);
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
        public static string SaveToJson(FuNodalGraph graph)
        {
            var dto = ToDto(graph);
            var json = JsonConvert.SerializeObject(dto, Formatting.Indented);
            return json;
        }

        /// <summary>
        /// Import a runtime graph from a JSON string.
        /// </summary>
        public static FuNodalGraph LoadFromJson(string json)
        {
            var dto = JsonConvert.DeserializeObject<FuGraphDto>(json);
            return FromDto(dto);
        }

        /// <summary>
        /// Map runtime graph to DTO (no Unity refs).
        /// </summary>
        public static FuGraphDto ToDto(FuNodalGraph graph)
        {
            var dto = new FuGraphDto();
            foreach (var n in graph.Nodes)
            {
                var nDto = new FuNodeDto
                {
                    Id = n.Id,
                    NodeType = FuNodalRegistry.GetNodeTypeId(n),
                    CustomNodeDataJson = JsonConvert.SerializeObject(n.Serialize()),
                    X = n.x,
                    Y = n.y
                };

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
                        DataJson = FuPortValueSerializer.ToJson(p.DataType, p.Data)
                    };
                    nDto.Ports.Add(pDto);
                }

                dto.Nodes.Add(nDto);
            }

            // Edges are already flat and serializable
            dto.Edges.AddRange(graph.Edges);
            return dto;
        }

        /// <summary>
        /// Rebuild a runtime graph from its DTO. Factories are used to instantiate nodes.
        /// </summary>
        public static FuNodalGraph FromDto(FuGraphDto dto)
        {
            var graph = new FuNodalGraph();
            var nodeMap = new Dictionary<Guid, FuNode>();

            // 1) Instantiate nodes via NodeRegistry, create default ports
            foreach (var nDto in dto.Nodes)
            {
                var node = FuNodalRegistry.CreateNode(nDto.NodeType);
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
                    port.Data = FuPortValueSerializer.FromJson(pDto.DataType, pDto.DataJson);
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

            return graph;
        }
    }
    #endregion
}