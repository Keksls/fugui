using System;
using System.Collections.Generic;

namespace Fu.Framework.Nodal
{
    public delegate void NodalCompute(NodalEvalContext ctx, NodalNode node);

    public interface INodalType
    {
        string TypeId { get; }
        Type ClrType { get; }
        string Serialize(object value);
        object Deserialize(string data);
    }

    public static class NodalTypeRegistry
    {
        private static readonly Dictionary<string, INodalType> _byId = new();
        private static readonly Dictionary<Type, INodalType> _byType = new();

        public static void Register(INodalType t)
        {
            _byId[t.TypeId] = t;
            _byType[t.ClrType] = t;
        }

        public static INodalType Get(string typeId) => _byId[typeId];
        public static INodalType Get(Type clrType) => _byType[clrType];

        public static bool TryGet(string typeId, out INodalType t) => _byId.TryGetValue(typeId, out t);
        public static bool TryGet(Type clrType, out INodalType t) => _byType.TryGetValue(clrType, out t);
    }

    public sealed class NodalEvalContext
    {
        private readonly NodalGraph _graph;
        private readonly Dictionary<(Guid nodeId, string portName), object> _cache = new();
        private readonly HashSet<Guid> _evalStack = new(); // détecter cycles

        public NodalEvalContext(NodalGraph graph) { _graph = graph; }

        // Récupère la valeur d'un input (en évaluant la source si nécessaire)
        public T GetInput<T>(NodalNode node, string portName, T defaultValue = default)
        {
            // trouve le port IN
            var pIn = node.Ports.Find(p => p.Name == portName && p.Direction == NodalPortDirection.In);
            if (pIn == null) return defaultValue;

            // Trouve une edge qui arrive sur ce port
            var e = _graph.Edges.FindLast(ed => ed.ToNodeId == node.Id && ed.ToPortId == pIn.Id);
            if (e == null) return defaultValue;

            // source
            var srcNode = _graph.FindNode(e.FromNodeId);
            var srcPort = _graph.FindPort(srcNode, e.FromPortId);
            if (srcNode == null || srcPort == null) return defaultValue;

            // évalue la sortie correspondante
            return GetOutput<T>(srcNode, srcPort.Name, defaultValue);
        }

        // Donne la valeur d'un output (évalue le node si pas encore fait)
        public T GetOutput<T>(NodalNode node, string outPortName, T defaultValue = default)
        {
            var key = (node.Id, outPortName);
            if (_cache.TryGetValue(key, out var v))
                return v is T tv ? tv : defaultValue;

            // protection cycle
            if (_evalStack.Contains(node.Id))
                throw new InvalidOperationException("Cycle detected during evaluation.");

            _evalStack.Add(node.Id);

            // exécuter le compute du node (doit setter ses outputs)
            node.Compute?.Invoke(this, node);

            _evalStack.Remove(node.Id);

            // re-check
            if (_cache.TryGetValue(key, out v))
                return v is T tv2 ? tv2 : defaultValue;

            return defaultValue;
        }

        // Setter d’un output (utilisé par Compute)
        public void SetOutput<T>(NodalNode node, string outPortName, T value)
        {
            _cache[(node.Id, outPortName)] = value!;
        }
    }
}