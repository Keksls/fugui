using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace Fu.Framework.Nodal
{
    public enum NodalPortDirection { In = 0, Out = 1 }
    public enum NodalMultiplicity { Single = 0, Many = 1 }

    [DataContract]
    public sealed class NodalPort
    {
        [DataMember(Order = 0)] public Guid Id { get; set; } = Guid.NewGuid();
        [DataMember(Order = 1)] public string Name { get; set; }
        [DataMember(Order = 2)] public NodalPortDirection Direction { get; set; }
        [DataMember(Order = 3)] public string DataType { get; set; }
        [DataMember(Order = 4)] public NodalMultiplicity Multiplicity { get; set; } = NodalMultiplicity.Single;
        [DataMember(Order = 5)] public List<NodalProperty> UserData { get; set; } = new List<NodalProperty>();
        [DataMember(Order = 6)] public float y { get; set; }
    }

    [DataContract]
    public sealed class NodalProperty
    {
        [DataMember(Order = 0)] public string Key { get; set; }
        [DataMember(Order = 1)] public string Value { get; set; }
    }

    [DataContract]
    public sealed class NodalEdge
    {
        [DataMember(Order = 0)] public Guid Id { get; set; } = Guid.NewGuid();
        [DataMember(Order = 1)] public Guid FromNodeId { get; set; }
        [DataMember(Order = 2)] public Guid FromPortId { get; set; }
        [DataMember(Order = 3)] public Guid ToNodeId { get; set; }
        [DataMember(Order = 4)] public Guid ToPortId { get; set; }
    }

    [DataContract]
    public class NodalNode
    {
        [DataMember(Order = 0)] public Guid Id { get; set; } = Guid.NewGuid();
        [DataMember(Order = 1)] public string TypeId { get; set; }
        [DataMember(Order = 2)] public string Title { get; set; }
        [DataMember(Order = 3)] public float x { get; set; }
        [DataMember(Order = 4)] public float y { get; set; }
        [DataMember(Order = 5)] public float width { get; set; } = 200f;
        [DataMember(Order = 6)] public List<NodalPort> Ports { get; set; } = new List<NodalPort>();
        [DataMember(Order = 7)] public List<NodalProperty> State { get; set; } = new List<NodalProperty>();
        [IgnoreDataMember] public float CustomUIHeight { get; private set; }
        [IgnoreDataMember] public Action<NodalNode, FuLayout> CustomUI { get; private set; }
        [IgnoreDataMember] public NodalCompute Compute { get; private set; }
        [IgnoreDataMember] public Color? NodeColor; 

        public NodalNode() { }

        internal void ApplyRuntime(NodeDefinition def)
        {
            TypeId = def.TypeId;
            Title = def.Title;
            width = def.Width;
            NodeColor = def.NodeColor;

            Ports = new List<NodalPort>(def.Ports.Count);
            for (int i = 0; i < def.Ports.Count; i++)
            {
                var p = def.Ports[i];
                Ports.Add(new NodalPort
                {
                    Id = Guid.NewGuid(),
                    Name = p.Name,
                    Direction = p.Direction,
                    DataType = p.DataType,
                    Multiplicity = p.Multiplicity
                });
            }

            CustomUIHeight = def.CustomUIHeight;
            CustomUI = def.CustomUI;
            Compute = def.Compute;
        }

        /// <summary>
        /// Get the value of a port by its name, with an optional default value if the port is not found or conversion fails.
        /// </summary>
        /// <typeparam name="T"> The type to which the port value should be converted.</typeparam>
        /// <param name="portName"> The name of the port whose value is to be retrieved.</param>
        /// <param name="defaultValue"> The default value to return if the port is not found or conversion fails. Defaults to the default value of type T.</param>
        /// <returns> The value of the port converted to type T, or the default value if not found or conversion fails.</returns>
        public T GetPortvalue<T>(string portName, T defaultValue = default)
        {
            for (int i = 0; i < State.Count; i++)
            {
                if (State[i].Key == portName)
                {
                    try
                    {
                        var t = typeof(T);
                        var s = State[i].Value;

                        if (t == typeof(float)) return (T)(object)float.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
                        if (t == typeof(double)) return (T)(object)double.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
                        if (t == typeof(int)) return (T)(object)int.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
                        if (t == typeof(bool)) return (T)(object)bool.Parse(s);

                        return (T)Convert.ChangeType(s, t, System.Globalization.CultureInfo.InvariantCulture);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[Nodal] Error converting '{State[i].Value}' to {typeof(T)} for port '{portName}': {e.Message}");
                        return defaultValue;
                    }
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Set the value of an output port by its name. If the port does not exist, it will not be created.
        /// </summary>
        /// <typeparam name="T"> The type of the value to be set.</typeparam>
        /// <param name="portName"> The name of the port whose value is to be set.</param>
        /// <param name="value"> The value to set for the port.</param>
        public void SetPortValue<T>(string portName, T value)
        {
            // vérifier qu'il existe un port OUT correspondant
            for (int i = 0; i < Ports.Count; i++)
            {
                if (Ports[i].Name == portName && Ports[i].Direction == NodalPortDirection.Out)
                {
                    string s = value is IFormattable f
                        ? f.ToString(null, System.Globalization.CultureInfo.InvariantCulture)
                        : value?.ToString();

                    for (int j = 0; j < State.Count; j++)
                    {
                        if (State[j].Key == portName)
                        {
                            State[j].Value = s;
                            return;
                        }
                    }
                    State.Add(new NodalProperty { Key = portName, Value = s });
                    return;
                }
            }
        }
    }

    [DataContract]
    public sealed class NodalGraph
    {
        [DataMember(Order = 0)] public string Version { get; set; } = "1.0.0";
        [DataMember(Order = 1)] public Guid Id { get; set; } = Guid.NewGuid();
        [DataMember(Order = 2)] public string Name { get; set; } = "New Graph";
        [DataMember(Order = 3)] public List<NodalNode> Nodes { get; set; } = new List<NodalNode>();
        [DataMember(Order = 4)] public List<NodalEdge> Edges { get; set; } = new List<NodalEdge>();
        [DataMember(Order = 5)] public List<NodalProperty> Properties { get; set; } = new List<NodalProperty>();

        public NodalNode FindNode(Guid id)
        {
            for (int i = 0; i < Nodes.Count; i++) if (Nodes[i].Id == id) return Nodes[i];
            return null;
        }

        public NodalPort FindPort(NodalNode node, Guid portId)
        {
            if (node == null) return null;
            for (int i = 0; i < node.Ports.Count; i++) if (node.Ports[i].Id == portId) return node.Ports[i];
            return null;
        }
    }
}