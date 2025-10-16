using System.Collections.Generic;
using Fu.Framework.Procedural;
using Fu.Framework.Procedural.Fields;

namespace Fu.Framework
{
    public sealed class ClampFieldNode : FuNode
    {
        public override string Title => "Clamp (Field2D)";
        public override float Width => 220f;
        public override System.Nullable<UnityEngine.Color> NodeColor => null;
        public override void CreateDefaultPorts()
        {
            AddPort(new FuNodalPort{ Name="Src", Direction=FuNodalPortDirection.In, DataType="core/field2D", AllowedTypes=new HashSet<string>{"core/field2D"}, Data=new ConstantField2D(0f), Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="Min", Direction=FuNodalPortDirection.In, DataType="core/float", AllowedTypes=new HashSet<string>{"core/float"}, Data=0f, Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="Max", Direction=FuNodalPortDirection.In, DataType="core/float", AllowedTypes=new HashSet<string>{"core/float"}, Data=1f, Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="Out", Direction=FuNodalPortDirection.Out, DataType="core/field2D", AllowedTypes=new HashSet<string>{"core/field2D"}, Data=null, Multiplicity=FuNodalMultiplicity.Many });
        }
        public override bool CanConnect(FuNodalPort fromPort, FuNodalPort toPort)=>true;
        public override void Compute()
        {
            var src = GetPortValue<IField2D>("Src", new ConstantField2D(0f));
            float mn = GetPortValue<float>("Min", 0f);
            float mx = GetPortValue<float>("Max", 1f);
            SetPortValue("Out","core/field2D", new ClampField2D(src, mn, mx));
        }

        public override void OnDraw(FuLayout layout) { }

        public override void SetDefaultValues(FuNodalPort port) { }
    }

    public sealed class SaturateFieldNode : FuNode
    {
        public override string Title => "Saturate (Field2D)";
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
            SetPortValue("Out","core/field2D", new SaturateField2D(src));
        }

        public override void OnDraw(FuLayout layout) { }

        public override void SetDefaultValues(FuNodalPort port) { }
    }

    public sealed class RemapFieldNode : FuNode
    {
        public override string Title => "Remap (Field2D)";
        public override float Width => 260f;
        public override System.Nullable<UnityEngine.Color> NodeColor => null;
        public override void CreateDefaultPorts()
        {
            AddPort(new FuNodalPort{ Name="Src", Direction=FuNodalPortDirection.In, DataType="core/field2D", AllowedTypes=new HashSet<string>{"core/field2D"}, Data=new ConstantField2D(0f), Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="InMin", Direction=FuNodalPortDirection.In, DataType="core/float", AllowedTypes=new HashSet<string>{"core/float"}, Data=0f, Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="InMax", Direction=FuNodalPortDirection.In, DataType="core/float", AllowedTypes=new HashSet<string>{"core/float"}, Data=1f, Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="OutMin", Direction=FuNodalPortDirection.In, DataType="core/float", AllowedTypes=new HashSet<string>{"core/float"}, Data=0f, Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="OutMax", Direction=FuNodalPortDirection.In, DataType="core/float", AllowedTypes=new HashSet<string>{"core/float"}, Data=1f, Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="Out", Direction=FuNodalPortDirection.Out, DataType="core/field2D", AllowedTypes=new HashSet<string>{"core/field2D"}, Data=null, Multiplicity=FuNodalMultiplicity.Many });
        }
        public override bool CanConnect(FuNodalPort fromPort, FuNodalPort toPort)=>true;
        public override void Compute()
        {
            var src = GetPortValue<IField2D>("Src", new ConstantField2D(0f));
            float inMin = GetPortValue<float>("InMin", 0f);
            float inMax = GetPortValue<float>("InMax", 1f);
            float outMin = GetPortValue<float>("OutMin", 0f);
            float outMax = GetPortValue<float>("OutMax", 1f);
            SetPortValue("Out","core/field2D", new RemapField2D(src, inMin, inMax, outMin, outMax));
        }

        public override void OnDraw(FuLayout layout) { }

        public override void SetDefaultValues(FuNodalPort port) { }
    }
}
