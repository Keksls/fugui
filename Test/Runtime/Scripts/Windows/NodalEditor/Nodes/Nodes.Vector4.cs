using UnityEngine;
using System.Collections.Generic;

namespace Fu.Framework
{
    /// <summary>
    /// Vector4 variable node
    /// </summary>
    public sealed class Vector4Node : FuNode
    {
        public override string Title => "Vector4";
        public override float Width => 200f;
        public override Color? NodeColor => color;
        private Color color;

        public Vector4Node(Color col)
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
                DataType = "core/v4",
                AllowedTypes = new HashSet<string> { "core/v4" },
                Data = Vector4.zero,
                Multiplicity = FuNodalMultiplicity.Many
            };
            AddPort(portOut);
        }

        public override void OnDraw(FuLayout layout)
        {
            var val = GetPortValue<Vector4>("Out", Vector4.zero);
            if(layout.Drag("##" + Id, ref val))
                SetPortValue("Out", "core/v4", val);
        }

        public override void SetDefaultValues(FuNodalPort port)
        {
            port.DataType = "core/v4";
            port.Data = Vector4.zero;
        }
    }
}
