using System.Collections.Generic;
using Fu.Framework.Procedural;
using Fu.Framework.Procedural.Fields;

namespace Fu.Framework
{
    /// <summary>Converts float → Field2D.</summary>
    public sealed class FloatToFieldNode : FuNode
    {
        public override string Title => "Float → Field2D";
        public override float Width => 220f;
        public override System.Nullable<UnityEngine.Color> NodeColor => null;
        public override void CreateDefaultPorts()
        {
            AddPort(new FuNodalPort{ Name="Value", Direction=FuNodalPortDirection.In, DataType="core/float", AllowedTypes=new HashSet<string>{"core/float","core/int"}, Data=0f, Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="Out", Direction=FuNodalPortDirection.Out, DataType="core/field2D", AllowedTypes=new HashSet<string>{"core/field2D"}, Data=new ConstantField2D(0f), Multiplicity=FuNodalMultiplicity.Many });
        }
        public override bool CanConnect(FuNodalPort fromPort, FuNodalPort toPort)=>true;
        public override void Compute()
        {
            float v = GetPortValue<float>("Value", 0f);
            SetPortValue("Out","core/field2D", new ConstantField2D(v));
        }

        public override void OnDraw(FuLayout layout) { }

        public override void SetDefaultValues(FuNodalPort port) { }
    }
}
