using System.Collections.Generic;
using Fu.Framework.Procedural;
using Fu.Framework.Procedural.Fields;
using UnityEngine;

namespace Fu.Framework
{
    /// <summary>Transform coordinates before sampling Field.</summary>
    public sealed class TransformFieldNode : FuNode
    {
        public override string Title => "Transform (Field2D)";
        public override float Width => 260f;
        public override Color? NodeColor => null;

        public override void CreateDefaultPorts()
        {
            AddPort(new FuNodalPort{ Name="Src", Direction=FuNodalPortDirection.In, DataType="core/field2D", AllowedTypes=new HashSet<string>{"core/field2D"}, Data=new ConstantField2D(0f), Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="Translate", Direction=FuNodalPortDirection.In, DataType="core/v2", AllowedTypes=new HashSet<string>{"core/v2"}, Data=Vector2.zero, Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="Scale", Direction=FuNodalPortDirection.In, DataType="core/v2", AllowedTypes=new HashSet<string>{"core/v2"}, Data=new Vector2(1f,1f), Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="RotationDeg", Direction=FuNodalPortDirection.In, DataType="core/float", AllowedTypes=new HashSet<string>{"core/float"}, Data=0f, Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="Out", Direction=FuNodalPortDirection.Out, DataType="core/field2D", AllowedTypes=new HashSet<string>{"core/field2D"}, Data=null, Multiplicity=FuNodalMultiplicity.Many });
        }

        public override bool CanConnect(FuNodalPort fromPort, FuNodalPort toPort) => true;

        public override void Compute()
        {
            var src = GetPortValue<IField2D>("Src", new ConstantField2D(0f));
            Vector2 t = GetPortValue<Vector2>("Translate", Vector2.zero);
            Vector2 s = GetPortValue<Vector2>("Scale", Vector2.one);
            float r = GetPortValue<float>("RotationDeg", 0f);
            SetPortValue("Out","core/field2D", new TransformField2D(src, t, s, r));
        }

        public override void OnDraw(FuLayout layout) { }

        public override void SetDefaultValues(FuNodalPort port) { }
    }
}
