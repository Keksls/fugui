using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework.Nodal
{
    public sealed class NodeDefinition
    {
        public string TypeId { get; internal set; }
        public string Title { get; internal set; }
        public float Width { get; internal set; } = 200f;
        public readonly List<NodalPort> Ports = new List<NodalPort>();
        public float CustomUIHeight { get; internal set; } = 0f;
        public Action<NodalNode, FuLayout> CustomUI { get; internal set; } = null;
        public NodalCompute Compute { get; internal set; }
        public Color? NodeColor { get; internal set; } = null;
    }

    public sealed class NodeBuilder
    {
        private readonly NodeDefinition _def = new();
        private NodeBuilder(string typeId, string title) { _def.TypeId = typeId; _def.Title = title; }

        public static NodeBuilder Define(string typeId, string title) => new(typeId, title);
        public NodeBuilder Width(float w) { _def.Width = w; return this; }
        public NodeBuilder In(string name, string dataType, NodalMultiplicity mult = NodalMultiplicity.Single)
        { _def.Ports.Add(new NodalPort { Name = name, Direction = NodalPortDirection.In, DataType = dataType, Multiplicity = mult }); return this; }
        public NodeBuilder Out(string name, string dataType, NodalMultiplicity mult = NodalMultiplicity.Many)
        { _def.Ports.Add(new NodalPort { Name = name, Direction = NodalPortDirection.Out, DataType = dataType, Multiplicity = mult }); return this; }

        public NodeBuilder UI(float height, Action<NodalNode, FuLayout> onDraw)
        { _def.CustomUIHeight = height; _def.CustomUI = onDraw; return this; }

        public NodeBuilder Compute(NodalCompute compute)
        { _def.Compute = compute; return this; }
        public NodeBuilder Color(Color color)
        { _def.NodeColor = color; return this; }

        public NodeDefinition Build()
        {
            return _def;
        }
    }

    public static class NodalNodeRegistry
    {
        private static readonly Dictionary<string, NodeDefinition> _defs = new();

        public static void Register(NodeDefinition def)
        {
            if (def == null || string.IsNullOrEmpty(def.TypeId))
                throw new ArgumentException("NodeDefinition.TypeId required");
            _defs[def.TypeId] = def;
        }

        public static NodalNode Create(string typeId)
        {
            if (!_defs.TryGetValue(typeId, out var def))
                throw new Exception($"No NodeDefinition for '{typeId}'");
            var n = new NodalNode();
            n.ApplyRuntime(def);
            return n;
        }

        public static void ApplyRuntimeTo(NodalNode node)
        {
            if (node != null && _defs.TryGetValue(node.TypeId, out var def))
                node.ApplyRuntime(def);
        }

        public static bool TryGet(string typeId, out NodeDefinition def) => _defs.TryGetValue(typeId, out def);

        public static List<NodeDefinition> GetAllDefinitions()
        {
            return new List<NodeDefinition>(_defs.Values);
        }
    }
}