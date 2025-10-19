﻿using Fu.Framework;
using System;
using System.Collections.Generic;

namespace Fu.Framework
{
    public static class FuNodalRegistry
    {
        private static Dictionary<string, Func<FuNode>> _nodesRegistry = new Dictionary<string, Func<FuNode>>();
        private static Dictionary<string, FuNodalType> _typeRegistry = new Dictionary<string, FuNodalType>();
        private static Dictionary<Type, string> _nodesTypesCache = null;

        /// <summary>
        /// Register a node type with its constructor function
        /// </summary>
        /// <param name="typeId"> The unique identifier for the node type.</param>
        /// <param name="constructor"> A function that constructs and returns an instance of the node type.</param>
        public static void RegisterNode(string typeId, Func<FuNode> constructor)
        {
            if (!_nodesRegistry.ContainsKey(typeId))
                _nodesRegistry.Add(typeId, constructor);
        }

        /// <summary>
        /// Create an instance of a registered node type by its typeId
        /// </summary>
        /// <param name="typeId"> The unique identifier for the node type to be created.</param>
        /// <returns> An instance of the node type if found; otherwise, null.</returns>
        public static FuNode CreateNode(string typeId, FuNodalGraph graph)
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
        public static string GetNodeTypeId(FuNode node)
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
        public static IEnumerable<string> GetRegisteredNode()
        {
            return _nodesRegistry.Keys;
        }

        /// <summary>
        /// Register a FuNodalType in the type registry
        /// </summary>
        /// <param name="type"> The FuNodalType to be registered.</param>
        public static void RegisterType(FuNodalType type)
        {
            if (!_typeRegistry.ContainsKey(type.Name))
                _typeRegistry.Add(type.Name, type);
        }

        /// <summary>
        /// Get a FuNodalType by its name
        /// </summary>
        /// <param name="typeName"> The name of the FuNodalType to retrieve.</param>
        /// <returns> The FuNodalType if found; otherwise, null.</returns>
        public static FuNodalType GetType(string typeName)
        {
            if (_typeRegistry.ContainsKey(typeName))
                return _typeRegistry[typeName];
            return null;
        }

        /// <summary>
        /// Get all registered FuNodalTypes
        /// </summary>
        /// <returns> An enumerable collection of all registered FuNodalTypes.</returns>
        public static IEnumerable<string> GetRegisteredTypes()
        {
            return _typeRegistry.Keys;
        }
    }
}