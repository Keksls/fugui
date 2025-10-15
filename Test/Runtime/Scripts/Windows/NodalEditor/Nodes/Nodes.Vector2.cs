using UnityEngine;
using System.Collections.Generic;

namespace Fu.Framework
{
    /// <summary>
    /// Vector2 variable node
    /// </summary>
    public sealed class Vector2Node : FuNode
    {
        public override string Title => "Vector2";
        public override float Width => 200f;
        public override Color? NodeColor => new Color(0.8f,0.8f,0.8f);

        public override bool CanConnect(FuNodalPort fromPort, FuNodalPort toPort) => true;

        public override void Compute() { }

        public override void CreateDefaultPorts()
        {
            FuNodalPort portOut = new FuNodalPort
            {
                Name = "Out",
                Direction = FuNodalPortDirection.Out,
                DataType = "core/v2",
                AllowedTypes = new HashSet<string> { "core/v2" },
                Data = Vector2.zero,
                Multiplicity = FuNodalMultiplicity.Many
            };
            AddPort(portOut);
        }

        public override void OnDraw(FuLayout layout)
        {
            var val = GetPortValue<Vector2>("Out", Vector2.zero);
            if(layout.Drag("##" + Id, ref val))
                SetPortValue("Out", "core/v2", val);
        }

        public override void SetDefaultValues(FuNodalPort port)
        {
            port.DataType = "core/v2";
            port.Data = Vector2.zero;
        }
    }
}
