using UnityEngine;
using System.Collections.Generic;

namespace Fu.Framework
{
    /// <summary>
    /// Int variable node
    /// </summary>
    public sealed class IntNode : FuNode
    {
        public override string Title => "Int";
        public override float Width => 200f;
        public override Color? NodeColor => color;
        private Color color;

        public IntNode(Color col)
        {
            color = col;
        }

        public override bool CanConnect(FuNodalPort fromPort, FuNodalPort toPort) => true;

        public override void Compute() { }

        public override void CreateDefaultPorts()
        {
            FuNodalPort portOut = new FuNodalPort
            {
                Name = "Out",
                Direction = FuNodalPortDirection.Out,
                DataType = "core/int",
                AllowedTypes = new HashSet<string> { "core/int" },
                Data = 0,
                Multiplicity = FuNodalMultiplicity.Many
            };
            AddPort(portOut);
        }

        public override void OnDraw(FuLayout layout)
        {
            var val = GetPortValue<int>("Out", 0);
            if(layout.Drag("##" + Id, ref val))
                SetPortValue("Out", "core/int", val);
        }

        public override void SetDefaultValues(FuNodalPort port)
        {
            port.DataType = "core/int";
            port.Data = 0;
        }
    }
}
