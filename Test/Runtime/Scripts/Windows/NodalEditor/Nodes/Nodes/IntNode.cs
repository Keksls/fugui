using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework.Demo
{
    /// <summary>Int variable node.</summary>
    public sealed class IntNode : FuNode
    {
        public override string Title => "Int";
        public override float Width => 96f;
        public override Color? NodeColor => _color;
        private Color _color;
        public IntNode(Color color) { _color = color; }

        public override bool CanConnect(FuNodalPort fromPort, FuNodalPort toPort) => true;
        public override void CreateDefaultPorts()
        {
            AddPort(new FuNodalPort{ Name="Out", Direction=FuNodalPortDirection.Out, DataType="core/int", AllowedTypes=new HashSet<string>{"core/int"}, Data=0, Multiplicity=FuNodalMultiplicity.Many });
        }
        public override void Compute(){}
        public override void OnDraw(FuLayout layout)
        {
            int v = GetPortValue<int>("Out", 0);
            if (layout.Drag("##"+Id, ref v))
                SetPortValue("Out","core/int", v);
        }
        public override void SetDefaultValues(FuNodalPort port){ port.DataType="core/int"; port.Data=0; }
    }
}