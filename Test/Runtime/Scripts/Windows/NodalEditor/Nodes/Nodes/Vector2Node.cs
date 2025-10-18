using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework.Demo
{
    /// <summary>
    /// Vector2 variable node (coords).
    /// </summary>
    public sealed class Vector2Node : FuNode
    {
        public override string Title => "Vector2";
        public override float Width => 220f;
        public override Color? NodeColor => _color;
        private Color _color;
        public Vector2Node(Color color) { _color = color; }

        public override bool CanConnect(FuNodalPort fromPort, FuNodalPort toPort) => true;

        public override void CreateDefaultPorts()
        {
            AddPort(new FuNodalPort{ Name="Out", Direction=FuNodalPortDirection.Out, DataType="core/v2", AllowedTypes=new HashSet<string>{"core/v2"}, Data=Vector2.zero, Multiplicity=FuNodalMultiplicity.Many });
        }

        public override void Compute(){}

        public override void OnDraw(FuLayout layout)
        {
            Vector2 v = GetPortValue<Vector2>("Out", Vector2.zero);
            if (layout.Drag("##"+Id, ref v))
                SetPortValue("Out","core/v2", v);
        }

        public override void SetDefaultValues(FuNodalPort port){ port.DataType="core/v2"; port.Data=Vector2.zero; }
    }
}