using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework.Demo
{
    /// <summary>
    /// Vector4 variable node.
    /// </summary>
    public sealed class Vector4Node : FuNode
    {
        public override string Title => "Vector4";
        public override float Width => 152f;
        public override Color? NodeColor => _color;
        private Color _color;
        public Vector4Node(Color color) { _color = color; }

        public override void CreateDefaultPorts()
        {
            AddPort(new FuNodalPort { Name = "Out", Direction = FuNodalPortDirection.Out, DataType = "core/v4", AllowedTypes = new HashSet<string> { "core/v4" }, Data = Vector4.one, Multiplicity = FuNodalMultiplicity.Many });
        }

        public override void Compute() { }

        public override void OnDraw(FuLayout layout)
        {
            Vector4 v = GetPortValue<Vector4>("Out", Vector4.zero);
            if (layout.Drag("##" + Id, ref v))
                SetPortValue("Out", "core/v4", v);
        }

        public override void SetDefaultValues(FuNodalPort port) { port.DataType = "core/v4"; port.Data = Vector4.zero; }
    }
}