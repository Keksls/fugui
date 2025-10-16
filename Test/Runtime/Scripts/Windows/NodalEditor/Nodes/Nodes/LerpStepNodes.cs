using System.Collections.Generic;
using Fu.Framework.Procedural;
using Fu.Framework.Procedural.Fields;

namespace Fu.Framework
{
    public sealed class LerpFieldNode : FuNode
    {
        public override string Title => "Lerp (Field2D)";
        public override float Width => 260f;
        public override System.Nullable<UnityEngine.Color> NodeColor => null;
        public override void CreateDefaultPorts()
        {
            AddPort(new FuNodalPort{ Name="A", Direction=FuNodalPortDirection.In, DataType="core/field2D", AllowedTypes=new HashSet<string>{"core/field2D"}, Data=new ConstantField2D(0f), Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="B", Direction=FuNodalPortDirection.In, DataType="core/field2D", AllowedTypes=new HashSet<string>{"core/field2D"}, Data=new ConstantField2D(1f), Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="T", Direction=FuNodalPortDirection.In, DataType="core/field2D", AllowedTypes=new HashSet<string>{"core/field2D"}, Data=new ConstantField2D(0.5f), Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="Out", Direction=FuNodalPortDirection.Out, DataType="core/field2D", AllowedTypes=new HashSet<string>{"core/field2D"}, Data=null, Multiplicity=FuNodalMultiplicity.Many });
        }
        public override bool CanConnect(FuNodalPort fromPort, FuNodalPort toPort)=>true;
        public override void Compute()
        {
            var a = GetPortValue<IField2D>("A", new ConstantField2D(0f));
            var b = GetPortValue<IField2D>("B", new ConstantField2D(1f));
            var t = GetPortValue<IField2D>("T", new ConstantField2D(0.5f));
            SetPortValue("Out","core/field2D", new LerpField2D(a,b,t));
        }

        public override void OnDraw(FuLayout layout) { }

        public override void SetDefaultValues(FuNodalPort port) { }
    }

    public sealed class StepFieldNode : FuNode
    {
        public override string Title => "Step (Field2D)";
        public override float Width => 220f;
        public override System.Nullable<UnityEngine.Color> NodeColor => null;
        public override void CreateDefaultPorts()
        {
            AddPort(new FuNodalPort{ Name="Src", Direction=FuNodalPortDirection.In, DataType="core/field2D", AllowedTypes=new HashSet<string>{"core/field2D"}, Data=new ConstantField2D(0f), Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="Edge", Direction=FuNodalPortDirection.In, DataType="core/float", AllowedTypes=new HashSet<string>{"core/float"}, Data=0.5f, Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="Out", Direction=FuNodalPortDirection.Out, DataType="core/field2D", AllowedTypes=new HashSet<string>{"core/field2D"}, Data=null, Multiplicity=FuNodalMultiplicity.Many });
        }
        public override bool CanConnect(FuNodalPort fromPort, FuNodalPort toPort)=>true;
        public override void Compute()
        {
            var src = GetPortValue<IField2D>("Src", new ConstantField2D(0f));
            float edge = GetPortValue<float>("Edge", 0.5f);
            SetPortValue("Out","core/field2D", new StepField2D(src, edge));
        }

        public override void OnDraw(FuLayout layout) { }

        public override void SetDefaultValues(FuNodalPort port) { }
    }

    public sealed class SmoothStepFieldNode : FuNode
    {
        public override string Title => "SmoothStep (Field2D)";
        public override float Width => 260f;
        public override System.Nullable<UnityEngine.Color> NodeColor => null;
        public override void CreateDefaultPorts()
        {
            AddPort(new FuNodalPort{ Name="Src", Direction=FuNodalPortDirection.In, DataType="core/field2D", AllowedTypes=new HashSet<string>{"core/field2D"}, Data=new ConstantField2D(0f), Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="Edge0", Direction=FuNodalPortDirection.In, DataType="core/float", AllowedTypes=new HashSet<string>{"core/float"}, Data=0.25f, Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="Edge1", Direction=FuNodalPortDirection.In, DataType="core/float", AllowedTypes=new HashSet<string>{"core/float"}, Data=0.75f, Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="Out", Direction=FuNodalPortDirection.Out, DataType="core/field2D", AllowedTypes=new HashSet<string>{"core/field2D"}, Data=null, Multiplicity=FuNodalMultiplicity.Many });
        }
        public override bool CanConnect(FuNodalPort fromPort, FuNodalPort toPort)=>true;
        public override void Compute()
        {
            var src = GetPortValue<IField2D>("Src", new ConstantField2D(0f));
            float e0 = GetPortValue<float>("Edge0", 0.25f);
            float e1 = GetPortValue<float>("Edge1", 0.75f);
            SetPortValue("Out","core/field2D", new SmoothStepField2D(src, e0, e1));
        }

        public override void OnDraw(FuLayout layout) { }

        public override void SetDefaultValues(FuNodalPort port) { }
    }
}
