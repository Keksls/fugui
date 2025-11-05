using System;
using System.Collections.Generic;

namespace Fu.Framework
{
    public class FuNodalRegistry
    {
        private static Dictionary<string, Func<FuNode>> _nodesRegistry = new Dictionary<string, Func<FuNode>>();
        private static Dictionary<string, FuNodalType> _typeRegistry = new Dictionary<string, FuNodalType>();
        private static Dictionary<Type, string> _nodesTypesCache = null;

        /// <summary>
        /// Register a node type with its constructor function
        /// </summary>
        /// <param name="typeId"> The unique identifier for the node type.</param>
        /// <param name="constructor"> A function that constructs and returns an instance of the node type.</param>
        public void RegisterNode(string typeId, Func<FuNode> constructor)
        {
            if (!_nodesRegistry.ContainsKey(typeId))
                _nodesRegistry.Add(typeId, constructor);
        }

        /// <summary>
        /// Create an instance of a registered node type by its typeId
        /// </summary>
        /// <param name="typeId"> The unique identifier for the node type to be created.</param>
        /// <returns> An instance of the node type if found; otherwise, null.</returns>
        public FuNode CreateNode(string typeId, FuNodalGraph graph)
        {
            if (_nodesRegistry.ContainsKey(typeId))
            {
                FuNode node = _nodesRegistry[typeId].Invoke();
                node.Graph = graph;
                node.CreateDefaultPorts();
                return node;
            }
            return null;
        }

        /// <summary>
        /// Get the typeId of a registered node instance
        /// </summary>
        /// <param name="node"> The node instance whose typeId is to be retrieved.</param>
        /// <returns> The typeId of the node if found; otherwise, null.</returns>
        public string GetNodeTypeId(FuNode node)
        {
            // get node type
            Type nodeType = node.GetType();

            // lazy init cache
            if (_nodesTypesCache == null || _nodesTypesCache.Count == 0 || !_nodesTypesCache.ContainsKey(nodeType))
            {
                _nodesTypesCache = new Dictionary<Type, string>();
                foreach (var kvp in _nodesRegistry)
                {
                    FuNode n = kvp.Value();
                    if (!_nodesTypesCache.ContainsKey(n.GetType()))
                        _nodesTypesCache.Add(n.GetType(), kvp.Key);
                }
            }

            // find in cache
            if (_nodesTypesCache.ContainsKey(nodeType))
                return _nodesTypesCache[nodeType];

            // not found
            return string.Empty;
        }

        /// <summary>
        /// Get all registered node type identifiers
        /// </summary>
        /// <returns> An enumerable collection of registered node type identifiers.</returns>
        public IEnumerable<string> GetRegisteredNode()
        {
            return _nodesRegistry.Keys;
        }

        /// <summary>
        /// Check if a node type is registered by its typeId
        /// </summary>
        /// <param name="typeId"> The unique identifier for the node type to check.</param>
        /// <returns> True if the node type is registered; otherwise, false.</returns>
        public bool HasRegisteredNode(string typeId)
        {
            return _nodesRegistry.ContainsKey(typeId);
        }

        /// <summary>
        /// Get all node type identifiers compatible with a given port direction and type
        /// </summary>
        /// <param name="direction"> The direction of the port (input/output).</param>
        /// <param name="type"> The type to check compatibility against.</param>
        /// <returns> An enumerable collection of compatible node type identifiers.</returns>
        public IEnumerable<string> GetCompatibleNodes(FuNodalPortDirection direction, HashSet<string> types)
        {
            HashSet<string> compatibleNodes = new HashSet<string>();
            foreach (var kvp in _nodesRegistry)
            {
                FuNode node = kvp.Value.Invoke();
                node.CreateDefaultPorts();
                foreach (var port in node.Ports)
                {
                    if (port.Value.Direction != direction)
                        continue;

                    foreach (var type in types)
                    {
                        if (port.Value.AllowedTypes.Contains(type))
                        {
                            string nodeId = kvp.Key.Replace("(", "").Replace(")", "").Trim() + " (" + port.Value.Name + ")";
                            if (!compatibleNodes.Contains(nodeId))
                                compatibleNodes.Add(nodeId);
                        }
                    }
                }
            }
            return compatibleNodes;
        }

        /// <summary>
        /// Register a FuNodalType in the type registry
        /// </summary>
        /// <param name="type"> The FuNodalType to be registered.</param>
        public void RegisterType(FuNodalType type)
        {
            if (!_typeRegistry.ContainsKey(type.Name))
                _typeRegistry.Add(type.Name, type);
        }

        /// <summary>
        /// Check if a FuNodalType is registered by its name
        /// </summary>
        /// <param name="typeName"> The name of the FuNodalType to check.</param>
        /// <returns> True if the FuNodalType is registered; otherwise, false.</returns>
        public bool HasRegisteredType(string typeName)
        {
            return _typeRegistry.ContainsKey(typeName);
        }

        /// <summary>
        /// Get a FuNodalType by its name
        /// </summary>
        /// <param name="typeName"> The name of the FuNodalType to retrieve.</param>
        /// <returns> The FuNodalType if found; otherwise, null.</returns>
        public FuNodalType GetType(string typeName)
        {
            if (_typeRegistry.ContainsKey(typeName))
                return _typeRegistry[typeName];
            return null;
        }

        /// <summary>
        /// Get all registered FuNodalTypes
        /// </summary>
        /// <returns> An enumerable collection of all registered FuNodalTypes.</returns>
        public IEnumerable<string> GetRegisteredTypes()
        {
            return _typeRegistry.Keys;
        }
    }
}