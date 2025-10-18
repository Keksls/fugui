using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework.Demo
{
    /// <summary>
    /// Vector3 variable node.
    /// </summary>
    public sealed class Vector3Node : FuNode
    {
        public override string Title => "Vector3";
        public override float Width => 220f;
        public override Color? NodeColor => _color;
        private Color _color;
        public Vector3Node(Color color) { _color = color; }

        public override bool CanConnect(FuNodalPort fromPort, FuNodalPort toPort) => true;

        public override void CreateDefaultPorts()
        {
            AddPort(new FuNodalPort{ Name="Out", Direction=FuNodalPortDirection.Out, DataType="core/v3", AllowedTypes=new HashSet<string>{"core/v3"}, Data=Vector3.zero, Multiplicity=FuNodalMultiplicity.Many });
        }

        public override void Compute(){}

        public override void OnDraw(FuLayout layout)
        {
            Vector3 v = GetPortValue<Vector3>("Out", Vector3.zero);
            if (layout.Drag("##"+Id, ref v))
                SetPortValue("Out","core/v3", v);
        }

        public override void SetDefaultValues(FuNodalPort port){ port.DataType="core/v3"; port.Data=Vector3.zero; }
    }
}