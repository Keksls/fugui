using UnityEngine;
using System.Collections.Generic;

namespace Fu.Framework
{
    /// <summary>
    /// Vector3 variable node
    /// </summary>
    public sealed class Vector3Node : FuNode
    {
        public override string Title => "Vector3";
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
                DataType = "core/v3",
                AllowedTypes = new HashSet<string> { "core/v3" },
                Data = Vector3.zero,
                Multiplicity = FuNodalMultiplicity.Many
            };
            AddPort(portOut);
        }

        public override void OnDraw(FuLayout layout)
        {
            var val = GetPortValue<Vector3>("Out", Vector3.zero);
            if(layout.Drag("##" + Id, ref val))
                SetPortValue("Out", "core/v3", val);
        }

        public override void SetDefaultValues(FuNodalPort port)
        {
            port.DataType = "core/v3";
            port.Data = Vector3.zero;
        }
    }
}
