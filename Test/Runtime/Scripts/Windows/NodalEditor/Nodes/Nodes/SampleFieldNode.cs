using System.Collections.Generic;
using UnityEngine;
using Fu.Framework.Procedural;

namespace Fu.Framework
{
    /// <summary>Sample a Field2D at Position (Vector2) → float.</summary>
    public sealed class SampleFieldNode : FuNode
    {
        public override string Title => "Sample Field";
        public override float Width => 220f;
        public override Color? NodeColor => null;

        public override void CreateDefaultPorts()
        {
            AddPort(new FuNodalPort{ Name="Field", Direction=FuNodalPortDirection.In, DataType="core/field2D", AllowedTypes=new HashSet<string>{"core/field2D"}, Data=null, Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="Position", Direction=FuNodalPortDirection.In, DataType="core/v2", AllowedTypes=new HashSet<string>{"core/v2"}, Data=Vector2.zero, Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="Out", Direction=FuNodalPortDirection.Out, DataType="core/float", AllowedTypes=new HashSet<string>{"core/float"}, Data=0f, Multiplicity=FuNodalMultiplicity.Many });
        }

        public override bool CanConnect(FuNodalPort fromPort, FuNodalPort toPort)=>true;

        public override void Compute()
        {
            var field = GetPortValue<IField2D>("Field", null);
            Vector2 p = GetPortValue<Vector2>("Position", Vector2.zero);
            if (field == null) { SetPortValue("Out","core/float", 0f); return; }
            var ctx = new Fu.Framework.Procedural.SampleContext{ XY = p };
            SetPortValue("Out","core/float", field.Sample(ctx));
        }

        public override void OnDraw(FuLayout layout) { }

        public override void SetDefaultValues(FuNodalPort port) { }
    }
}
