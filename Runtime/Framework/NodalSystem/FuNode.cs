using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fu.Framework
{
    public abstract class FuNode
    {
        public int Id { get; set; } = FuNodeId.New();
        public abstract string Title { get; }
        public abstract float Width { get; }
        public float x { get; private set; }
        public float y { get; private set; }
        public Dictionary<string, FuNodalPort> Ports { get; private set; } = new Dictionary<string, FuNodalPort>();
        public virtual Color? NodeColor { get; } = null;
        public FuNodalGraph Graph { get;  internal set; }
        internal bool Dirty { get; set; } = false;
        internal FuNodeEditorData EditorData { get; set; } = new FuNodeEditorData();

        #region Abstract Methods
        /// <summary>
        /// Override to draw custom UI inside the node
        /// </summary>
        /// <param name="layout"> the current layout to draw ui in</param>
        public abstract void OnDraw(FuLayout layout);

        /// <summary>
        /// get input values from state
        /// process values
        /// set output values to state
        /// </summary>
        public abstract void Compute();

        /// <summary>
        /// Create the default ports for the node
        /// </summary>
        public abstract void CreateDefaultPorts();

        /// <summary>
        /// Determine if a connection can be made between two ports
        /// </summary>
        /// <param name="fromPort"> the port where the connection starts (output of another node)</param>
        /// <param name="toPort"> the port where the connection ends (input of this node)</param>
        /// <returns> true if the connection can be made, false otherwise</returns>
        public virtual bool CanConnect(FuNodalPort fromPort, FuNodalPort toPort) => true;

        /// <summary>
        /// Get the current converted type of a port
        /// Used for link color preview
        /// </summary>
        /// <param name="port"> the port to get the converted type for </param>
        /// <returns> the converted type as a string </returns>
        public virtual string GetCurrentConvertedType(FuNodalPort port)
        {
            return port.DataType;
        }

        /// <summary>
        /// Set the default values for a port when it is created
        /// </summary>
        /// <param name="port"> the port to set default values for</param>
        public abstract void SetDefaultValues(FuNodalPort port);

        /// <summary>
        /// Serialize the node's data to a string
        /// </summary>
        /// <returns> A string representation of the node's data.</returns>
        public virtual string Serialize() { return ""; }

        /// <summary>
        /// Deserialize the node's data from a string
        /// </summary>
        /// <param name="data"> A string representation of the node's data.</param>
        public virtual void Deserialize(string data) { }
        #endregion

        #region Public Methods
        /// <summary>
        /// Mark the node as dirty, indicating that it needs to be recomputed
        /// </summary>
        public void MarkDirty()
        {
            Dirty = true;
        }

        /// <summary>
        /// Get the data type of a port by its name
        /// </summary>
        /// <param name="portName"> The name of the port whose data type is to be retrieved.</param>
        /// <returns> The data type of the port as a string, or null if the port is not found.</returns>
        public string GetPortType(string portName)
        {
            FuNodalPort port = Ports.Values.FirstOrDefault(p => p.Name == portName);
            return port != null ? port.DataType : null;
        }

        /// <summary>
        /// Get a port by its unique identifier
        /// </summary>
        /// <param name="portId"> The unique identifier of the port to be retrieved.</param>
        /// <returns> The port with the specified unique identifier, or null if not found.</returns>
        public FuNodalPort GetPort(int portId)
        {
            return Ports.Values.FirstOrDefault(p => p.Id == portId);
        }

        /// <summary>
        /// Get the value of a port by its name, with an optional default value if the port is not found or conversion fails.
        /// </summary>
        /// <typeparam name="T"> The type to which the port value should be converted.</typeparam>
        /// <param name="portName"> The name of the port whose value is to be retrieved.</param>
        /// <param name="defaultValue"> The default value to return if the port is not found or conversion fails. Defaults to the default value of type T.</param>
        /// <returns> The value of the port converted to type T, or the default value if not found or conversion fails.</returns>
        public T GetPortValue<T>(string portName, T defaultValue = default)
        {
            if (Ports.TryGetValue(portName, out FuNodalPort port))
            {
                if (port.Data is T value)
                {
                    return value;
                }
                try
                {
                    return (T)port.Data;
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Set the value of a port by its name and type
        /// </summary>
        /// <typeparam name="T"> The type of the value to be set.</typeparam>
        /// <param name="portName"> The name of the port whose value is to be set.</param>
        /// <param name="portType"> The data type of the port as a string.</param>
        /// <param name="value"> The value to be set to the port.</param>
        public void SetPortValue<T>(string portName, string portType, T value)
        {
            if (Ports.TryGetValue(portName, out FuNodalPort port))
            {
                port.DataType = portType;
                port.Data = value;
                Dirty = true;
            }
            else
            {
                Debug.LogWarning($"[Nodal] Port '{portName}' not found in node '{Title}'");
            }
        }

        /// <summary>
        /// Add a port to the node
        /// </summary>
        /// <param name="port"> The port to be added to the node.</param>
        public void AddPort(FuNodalPort port)
        {
            if (port == null)
            {
                Debug.LogError($"[Nodal] Cannot add a null port to node '{Title}'");
                return;
            }
            if (Ports.ContainsKey(port.Name))
            {
                Debug.LogWarning($"[Nodal] Port '{port.Name}' already exists in node '{Title}'");
                return;
            }
            Ports[port.Name] = port;
            Dirty = true;
        }

        /// <summary>
        /// Remove a port from the node by its name
        /// </summary>
        /// <param name="portName"> The name of the port to be removed from the node.</param>
        public void RemovePort(string portName)
        {
            if (!Ports.ContainsKey(portName))
            {
                Debug.LogWarning($"[Nodal] Port '{portName}' not found in node '{Title}'");
                return;
            }
            Ports.Remove(portName);
            Dirty = true;
        }

        /// <summary>
        /// Set the position of the node in the graph
        /// </summary>
        /// <param name="x"> The x-coordinate of the node's position.</param>
        /// <param name="y"> The y-coordinate of the node's position.</param>
        public void SetPosition(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        /// <summary>
        /// Get the input node connected to a specific input port by its name
        /// </summary>
        /// <param name="portName"> The name of the input port whose connected node is to be retrieved.</param>
        /// <returns> The input node connected to the specified port, or null if not found.</returns>
        public FuNode GetInputNode(string portName)
        {
            if (Graph == null)
            {
                return null;
            }
            FuNodalPort port = Ports.Values.FirstOrDefault(p => p.Name == portName && p.Direction == FuNodalPortDirection.In);
            if (port == null)
            {
                return null;
            }
            FuNodalEdge edge = Graph.Edges.FirstOrDefault(e => e.ToNodeId == Id && e.ToPortId == port.Id);
            if (edge == null)
            {
                return null;
            }
            return Graph.GetNode(edge.FromNodeId);
        }
        #endregion
    }

    /// <summary>
    /// Struct holding precalculated geometry for a node to optimize drawing and interaction.
    /// </summary>
    public class FuNodeEditorData
    {
        public bool MustRecalculate = true;

        public Vector2 rectMin, rectMax;
        public float headerHeight;

        public float portLineHeight;
        public float portsStartY;
        public float portsEndY;

        public bool hasIn;
        public bool hasOut;

        public float leftMinX;
        public float leftMaxX;
        public float rightMinX;
        public float rightMaxX;
    }

    public sealed class FuNodalPort
    {
        public int Id { get; set; } = FuNodeId.New();
        public string Name { get; set; }
        public FuNodalPortDirection Direction { get; set; }
        public FuNodalMultiplicity Multiplicity { get; set; } = FuNodalMultiplicity.Single;
        public HashSet<string> AllowedTypes { get; set; } = new HashSet<string>();

        public string DataType { get; set; }
        public object Data { get; set; }
    }

    public sealed class FuNodalEdge
    {
        public int Id { get; set; } = FuNodeId.New();
        public int FromNodeId { get; set; }
        public int FromPortId { get; set; }
        public int ToNodeId { get; set; }
        public int ToPortId { get; set; }
    }

    public sealed class FuNodalType
    {
        public string Name { get; private set; }
        public Type Type { get; private set; }
        public Color? Color { get; private set; }
        public object DefaultValue { get; private set; }
        public Func<object, bool> ValidationFunc { get; private set; } = null;
        public Func<object, string> SerializationFunc { get; private set; } = null;
        public Func<string, object> DeserializationFunc { get; private set; } = null;

        /// <summary>
        /// Create a new FuNodalType instance with the specified parameters.
        /// </summary>
        /// <typeparam name="T"> The type of the nodal type. Must be a non-nullable type.</typeparam>
        /// <param name="name"> The name of the nodal type.</param>
        /// <param name="defaultValue"> The default value for the nodal type.</param>
        /// <param name="serializationFunc"> A function that serializes the nodal type value to a string.</param>
        /// <param name="deserializationFunc"> A function that deserializes a string to the nodal type value.</param>
        /// <param name="validationFunc"> An optional function that validates the nodal type value. If not provided, a default validation function that checks if the value is of type T will be used.</param>
        /// <param name="color"> An optional color associated with the nodal type.</param>
        /// <returns> A new instance of FuNodalType with the specified parameters.</returns>
        public static FuNodalType Create<T>(string name, T defaultValue, Func<T, string> serializationFunc, Func<string, T> deserializationFunc, Func<object, bool> validationFunc = null, Color? color = null)
        {
            return new FuNodalType()
            {
                Type = typeof(T),
                Name = name,
                DefaultValue = defaultValue,
                Color = color,
                ValidationFunc = validationFunc == null ? (obj) => obj is T : validationFunc,
                SerializationFunc = (obj) => serializationFunc((T)obj),
                DeserializationFunc = (str) => deserializationFunc(str)
            };
        }
    }
}