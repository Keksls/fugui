using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework.Demo
{
    /// <summary>
    /// Vector3 variable node.
    /// </summary>
    public sealed class Vector3Node : FuNode
    {
        #region State
        public override string Title => "Vector3";
        public override float Width => 128f;
        public override Color? NodeColor => _color;

        private Color _color;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the Vector3 Node class.
        /// </summary>
        /// <param name="color">The color value.</param>
        public Vector3Node(Color color) { _color = color; }
        #endregion

        #region Methods
        /// <summary>
        /// Creates the default ports.
        /// </summary>
        public override void CreateDefaultPorts()
        {
            AddPort(new FuNodalPort{ Name="Out", Direction=FuNodalPortDirection.Out, DataType="core/v3", AllowedTypes=new HashSet<string>{"core/v3"}, Data=Vector3.one, Multiplicity=FuNodalMultiplicity.Many });
        }

        /// <summary>
        /// Runs the compute workflow.
        /// </summary>
        public override void Compute(){}

        /// <summary>
        /// Handles the Draw event.
        /// </summary>
        /// <param name="layout">The layout value.</param>
        public override void OnDraw(FuLayout layout)
        {
            Vector3 v = GetPortValue<Vector3>("Out", Vector3.zero);
            if (layout.Drag("##"+Id, ref v))
                SetPortValue("Out","core/v3", v);
        }

        /// <summary>
        /// Sets the default values.
        /// </summary>
        /// <param name="port">The port value.</param>
        public override void SetDefaultValues(FuNodalPort port){ port.DataType="core/v3"; port.Data=Vector3.zero; }
        #endregion
    }
}