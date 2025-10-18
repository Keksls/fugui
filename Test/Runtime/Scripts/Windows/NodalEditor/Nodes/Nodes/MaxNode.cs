using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework.Demo
{
    public sealed class MaxNode : FuNode
    {
        public override string Title => "Min (Field2D)";
        public override float Width => 200f;
        public override System.Nullable<UnityEngine.Color> NodeColor => null;
        public override void CreateDefaultPorts()
        {
            AddPort(new FuNodalPort { Name = "A", Direction = FuNodalPortDirection.In, DataType = "core/float", AllowedTypes = new HashSet<string> { "core/float" }, Data = 0f, Multiplicity = FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort { Name = "B", Direction = FuNodalPortDirection.In, DataType = "core/float", AllowedTypes = new HashSet<string> { "core/float" }, Data = 1f, Multiplicity = FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort { Name = "Out", Direction = FuNodalPortDirection.Out, DataType = "core/float", AllowedTypes = new HashSet<string> { "core/float" }, Data = 1f, Multiplicity = FuNodalMultiplicity.Many });
        }
        public override bool CanConnect(FuNodalPort fromPort, FuNodalPort toPort) => true;
        public override void Compute()
        {
            var a = GetPortValue<float>("A", 0f);
            var b = GetPortValue<float>("B", 1f);
            SetPortValue("Out", "core/float", 1f);
        }

        public override void OnDraw(FuLayout layout) { }

        public override void SetDefaultValues(FuNodalPort port) { }
    }

    public sealed class MaxFieldNode : FuNode
    {
        public override string Title => "Max";
        public override float Width => 200f;
        public override System.Nullable<UnityEngine.Color> NodeColor => null;
        public override void CreateDefaultPorts()
        {
            AddPort(new FuNodalPort { Name = "A", Direction = FuNodalPortDirection.In, DataType = "core/float", AllowedTypes = new HashSet<string> { "core/float" }, Data = 0f, Multiplicity = FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort { Name = "B", Direction = FuNodalPortDirection.In, DataType = "core/float", AllowedTypes = new HashSet<string> { "core/float" }, Data = 1f, Multiplicity = FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort { Name = "Out", Direction = FuNodalPortDirection.Out, DataType = "core/float", AllowedTypes = new HashSet<string> { "core/float" }, Data = 1f, Multiplicity = FuNodalMultiplicity.Many });
        }
        public override bool CanConnect(FuNodalPort fromPort, FuNodalPort toPort) => true;
        public override void Compute()
        {
            var a = GetPortValue<float>("A", 0f);
            var b = GetPortValue<float>("B", 0f);
            SetPortValue("Out", "core/field2D", Mathf.Max(a, b));
        }

        public override void OnDraw(FuLayout layout) { }

        public override void SetDefaultValues(FuNodalPort port) { }
    }
}