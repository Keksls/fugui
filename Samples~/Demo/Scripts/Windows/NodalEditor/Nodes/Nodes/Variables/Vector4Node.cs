using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework.Demo
{
    /// <summary>
    /// Vector4 variable node.
    /// </summary>
    public sealed class Vector4Node : FuNode
    {
        #region State
        public override string Title => "Vector4";
        public override float Width => 152f;
        public override Color? NodeColor => _color;

        private Color _color;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the Vector4 Node class.
        /// </summary>
        /// <param name="color">The color value.</param>
        public Vector4Node(Color color) { _color = color; }
        #endregion

        #region Methods
        /// <summary>
        /// Creates the default ports.
        /// </summary>
        public override void CreateDefaultPorts()
        {
            AddPort(new FuNodalPort { Name = "Out", Direction = FuNodalPortDirection.Out, DataType = "core/v4", AllowedTypes = new HashSet<string> { "core/v4" }, Data = Vector4.one, Multiplicity = FuNodalMultiplicity.Many });
        }

        /// <summary>
        /// Runs the compute workflow.
        /// </summary>
        public override void Compute() { }

        /// <summary>
        /// Handles the Draw event.
        /// </summary>
        /// <param name="layout">The layout value.</param>
        public override void OnDraw(FuLayout layout)
        {
            Vector4 v = GetPortValue<Vector4>("Out", Vector4.zero);
            if (layout.Drag("##" + Id, ref v))
                SetPortValue("Out", "core/v4", v);
        }

        /// <summary>
        /// Sets the default values.
        /// </summary>
        /// <param name="port">The port value.</param>
        public override void SetDefaultValues(FuNodalPort port) { port.DataType = "core/v4"; port.Data = Vector4.zero; }
        #endregion
    }
}