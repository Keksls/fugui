using System.Collections.Generic;
using Fu.Framework.Procedural;
using Fu.Framework.Procedural.Fields;

namespace Fu.Framework
{
    public sealed class AbsFieldNode : FuNode
    {
        public override string Title => "Abs (Field2D)";
        public override float Width => 200f;
        public override System.Nullable<UnityEngine.Color> NodeColor => null;
        public override void CreateDefaultPorts()
        {
            AddPort(new FuNodalPort{ Name="Src", Direction=FuNodalPortDirection.In, DataType="core/field2D", AllowedTypes=new HashSet<string>{"core/field2D"}, Data=new ConstantField2D(0f), Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="Out", Direction=FuNodalPortDirection.Out, DataType="core/field2D", AllowedTypes=new HashSet<string>{"core/field2D"}, Data=null, Multiplicity=FuNodalMultiplicity.Many });
        }
        public override bool CanConnect(FuNodalPort fromPort, FuNodalPort toPort)=>true;
        public override void Compute()
        {
            var src = GetPortValue<IField2D>("Src", new ConstantField2D(0f));
            SetPortValue("Out","core/field2D", new AbsField2D(src));
        }

        public override void OnDraw(FuLayout layout) { }

        public override void SetDefaultValues(FuNodalPort port)
        {

        }
    }

    public sealed class Invert01FieldNode : FuNode
    {
        public override string Title => "Invert01 (Field2D)";
        public override float Width => 220f;
        public override System.Nullable<UnityEngine.Color> NodeColor => null;
        public override void CreateDefaultPorts()
        {
            AddPort(new FuNodalPort{ Name="Src", Direction=FuNodalPortDirection.In, DataType="core/field2D", AllowedTypes=new HashSet<string>{"core/field2D"}, Data=new ConstantField2D(0f), Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="Out", Direction=FuNodalPortDirection.Out, DataType="core/field2D", AllowedTypes=new HashSet<string>{"core/field2D"}, Data=null, Multiplicity=FuNodalMultiplicity.Many });
        }
        public override bool CanConnect(FuNodalPort fromPort, FuNodalPort toPort)=>true;
        public override void Compute()
        {
            var src = GetPortValue<IField2D>("Src", new ConstantField2D(0f));
            SetPortValue("Out","core/field2D", new Invert01Field2D(src));
        }

        public override void OnDraw(FuLayout layout) { }

        public override void SetDefaultValues(FuNodalPort port)
        {
        }
    }

    public sealed class PowFieldNode : FuNode
    {
        public override string Title => "Pow (Field2D)";
        public override float Width => 220f;
        public override System.Nullable<UnityEngine.Color> NodeColor => null;
        public override void CreateDefaultPorts()
        {
            AddPort(new FuNodalPort{ Name="Src", Direction=FuNodalPortDirection.In, DataType="core/field2D", AllowedTypes=new HashSet<string>{"core/field2D"}, Data=new ConstantField2D(0f), Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="Exponent", Direction=FuNodalPortDirection.In, DataType="core/float", AllowedTypes=new HashSet<string>{"core/float"}, Data=1f, Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="Out", Direction=FuNodalPortDirection.Out, DataType="core/field2D", AllowedTypes=new HashSet<string>{"core/field2D"}, Data=null, Multiplicity=FuNodalMultiplicity.Many });
        }
        public override bool CanConnect(FuNodalPort fromPort, FuNodalPort toPort)=>true;
        public override void Compute()
        {
            var src = GetPortValue<IField2D>("Src", new ConstantField2D(0f));
            float e = GetPortValue<float>("Exponent", 1f);
            SetPortValue("Out","core/field2D", new PowField2D(src, e));
        }

        public override void OnDraw(FuLayout layout) { }

        public override void SetDefaultValues(FuNodalPort port)
        {

        }
    }
}
