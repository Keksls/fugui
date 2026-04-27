using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework.Demo
{
    /// <summary>
    /// Vector2 variable node (coords).
    /// </summary>
    public sealed class Vector2Node : FuNode
    {
        #region State
        public override string Title => "Vector2";
        public override float Width => 100f;
        public override Color? NodeColor => _color;

        private Color _color;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the Vector2 Node class.
        /// </summary>
        /// <param name="color">The color value.</param>
        public Vector2Node(Color color) { _color = color; }
        #endregion

        #region Methods
        /// <summary>
        /// Creates the default ports.
        /// </summary>
        public override void CreateDefaultPorts()
        {
            AddPort(new FuNodalPort{ Name="Out", Direction=FuNodalPortDirection.Out, DataType="core/v2", AllowedTypes=new HashSet<string>{"core/v2"}, Data=Vector2.one, Multiplicity=FuNodalMultiplicity.Many });
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
            Vector2 v = GetPortValue<Vector2>("Out", Vector2.zero);
            if (layout.Drag("##"+Id, ref v))
                SetPortValue("Out","core/v2", v);
        }

        /// <summary>
        /// Sets the default values.
        /// </summary>
        /// <param name="port">The port value.</param>
        public override void SetDefaultValues(FuNodalPort port){ port.DataType="core/v2"; port.Data=Vector2.zero; }
        #endregion
    }
}