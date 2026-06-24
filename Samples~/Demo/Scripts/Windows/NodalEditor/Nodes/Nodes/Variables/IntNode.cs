using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework.Demo
{
    /// <summary>Int variable node.</summary>
    public sealed class IntNode : FuNode
    {
        #region State
        public override string Title => "Int";
        public override float Width => 96f;
        public override Color? NodeColor => _color;

        private Color _color;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the Int Node class.
        /// </summary>
        /// <param name="color">The color value.</param>
        public IntNode(Color color) { _color = color; }
        #endregion

         #region Methods
         /// <summary>
         /// Creates the default ports.
         /// </summary>
         public override void CreateDefaultPorts()
        {
            AddPort(new FuNodalPort{ Name="Out", Direction=FuNodalPortDirection.Out, DataType="core/int", AllowedTypes=new HashSet<string>{"core/int"}, Data=1, Multiplicity=FuNodalMultiplicity.Many });
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
            int v = GetPortValue<int>("Out", 0);
            if (layout.Drag("##"+Id, ref v))
                SetPortValue("Out","core/int", v);
        }
        /// <summary>
        /// Sets the default values.
        /// </summary>
        /// <param name="port">The port value.</param>
        public override void SetDefaultValues(FuNodalPort port){ port.DataType="core/int"; port.Data=0; }
         #endregion
    }
}