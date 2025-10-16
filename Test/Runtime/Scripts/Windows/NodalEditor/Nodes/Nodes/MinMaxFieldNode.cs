using System.Collections.Generic;
using Fu.Framework.Procedural;
using Fu.Framework.Procedural.Fields;

namespace Fu.Framework
{
    public sealed class MinFieldNode : FuNode
    {
        public override string Title => "Min (Field2D)";
        public override float Width => 200f;
        public override System.Nullable<UnityEngine.Color> NodeColor => null;
        public override void CreateDefaultPorts()
        {
            AddPort(new FuNodalPort{ Name="A", Direction=FuNodalPortDirection.In, DataType="core/field2D", AllowedTypes=new HashSet<string>{"core/field2D"}, Data=new ConstantField2D(0f), Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="B", Direction=FuNodalPortDirection.In, DataType="core/field2D", AllowedTypes=new HashSet<string>{"core/field2D"}, Data=new ConstantField2D(0f), Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="Out", Direction=FuNodalPortDirection.Out, DataType="core/field2D", AllowedTypes=new HashSet<string>{"core/field2D"}, Data=null, Multiplicity=FuNodalMultiplicity.Many });
        }
        public override bool CanConnect(FuNodalPort fromPort, FuNodalPort toPort)=>true;
        public override void Compute()
        {
            var a = GetPortValue<IField2D>("A", new ConstantField2D(0f));
            var b = GetPortValue<IField2D>("B", new ConstantField2D(0f));
            SetPortValue("Out","core/field2D", new MinField2D(a,b));
        }

        public override void OnDraw(FuLayout layout) { }

        public override void SetDefaultValues(FuNodalPort port) { }
    }

    public sealed class MaxFieldNode : FuNode
    {
        public override string Title => "Max (Field2D)";
        public override float Width => 200f;
        public override System.Nullable<UnityEngine.Color> NodeColor => null;
        public override void CreateDefaultPorts()
        {
            AddPort(new FuNodalPort{ Name="A", Direction=FuNodalPortDirection.In, DataType="core/field2D", AllowedTypes=new HashSet<string>{"core/field2D"}, Data=new ConstantField2D(0f), Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="B", Direction=FuNodalPortDirection.In, DataType="core/field2D", AllowedTypes=new HashSet<string>{"core/field2D"}, Data=new ConstantField2D(0f), Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="Out", Direction=FuNodalPortDirection.Out, DataType="core/field2D", AllowedTypes=new HashSet<string>{"core/field2D"}, Data=null, Multiplicity=FuNodalMultiplicity.Many });
        }
        public override bool CanConnect(FuNodalPort fromPort, FuNodalPort toPort)=>true;
        public override void Compute()
        {
            var a = GetPortValue<IField2D>("A", new ConstantField2D(0f));
            var b = GetPortValue<IField2D>("B", new ConstantField2D(0f));
            SetPortValue("Out","core/field2D", new MaxField2D(a,b));
        }

        public override void OnDraw(FuLayout layout) { }

        public override void SetDefaultValues(FuNodalPort port) { }
    }
}