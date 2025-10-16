using System.Collections.Generic;
using Fu.Framework.Procedural;
using Fu.Framework.Procedural.Fields;

namespace Fu.Framework
{
    /// <summary>Domain warp with U,V displacement fields.</summary>
    public sealed class WarpFieldNode : FuNode
    {
        public override string Title => "Warp (Field2D)";
        public override float Width => 260f;
        public override System.Nullable<UnityEngine.Color> NodeColor => null;

        public override void CreateDefaultPorts()
        {
            AddPort(new FuNodalPort{ Name="Src", Direction=FuNodalPortDirection.In, DataType="core/field2D", AllowedTypes=new System.Collections.Generic.HashSet<string>{"core/field2D"}, Data=new Fu.Framework.Procedural.Fields.ConstantField2D(0f), Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="U", Direction=FuNodalPortDirection.In, DataType="core/field2D", AllowedTypes=new System.Collections.Generic.HashSet<string>{"core/field2D"}, Data=new Fu.Framework.Procedural.Fields.ConstantField2D(0f), Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="V", Direction=FuNodalPortDirection.In, DataType="core/field2D", AllowedTypes=new System.Collections.Generic.HashSet<string>{"core/field2D"}, Data=new Fu.Framework.Procedural.Fields.ConstantField2D(0f), Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="Strength", Direction=FuNodalPortDirection.In, DataType="core/float", AllowedTypes=new System.Collections.Generic.HashSet<string>{"core/float"}, Data=1f, Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="Out", Direction=FuNodalPortDirection.Out, DataType="core/field2D", AllowedTypes=new System.Collections.Generic.HashSet<string>{"core/field2D"}, Data=null, Multiplicity=FuNodalMultiplicity.Many });
        }

        public override bool CanConnect(FuNodalPort fromPort, FuNodalPort toPort)=>true;

        public override void Compute()
        {
            var src = GetPortValue<IField2D>("Src", new Fu.Framework.Procedural.Fields.ConstantField2D(0f));
            var u = GetPortValue<IField2D>("U", new Fu.Framework.Procedural.Fields.ConstantField2D(0f));
            var v = GetPortValue<IField2D>("V", new Fu.Framework.Procedural.Fields.ConstantField2D(0f));
            float s = GetPortValue<float>("Strength", 1f);
            SetPortValue("Out","core/field2D", new Fu.Framework.Procedural.Fields.WarpField2D(src,u,v,s));
        }

        public override void OnDraw(FuLayout layout) { }

        public override void SetDefaultValues(FuNodalPort port) { }
    }
}
