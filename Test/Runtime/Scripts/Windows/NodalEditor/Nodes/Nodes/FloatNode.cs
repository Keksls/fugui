using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework.Demo
{
    /// <summary>Float variable node.</summary>
    public sealed class FloatNode : FuNode
    {
        public override string Title => "Float";
        public override float Width => 200f;
        public override Color? NodeColor => _color;
        private Color _color;
        private bool _slider = false;
        private float _min = 0f, _max = 100f;
        public FloatNode(Color color) { _color = color; }

        public override bool CanConnect(FuNodalPort fromPort, FuNodalPort toPort) => true;

        public override void CreateDefaultPorts()
        {
            AddPort(new FuNodalPort { Name = "Out", Direction = FuNodalPortDirection.Out, DataType = "core/float", AllowedTypes = new HashSet<string> { "core/float" }, Data = 0f, Multiplicity = FuNodalMultiplicity.Many });
        }

        public override void Compute() { }

        public override void OnDraw(FuLayout layout)
        {
            float v = GetPortValue<float>("Out", 0f);
            if (layout.ClickableText(Icons.Settings_solid))
            {
                Fugui.ShowModal("Float Settings " + Id, (l) =>
                {
                    l.CheckBox("Use Slider", ref _slider);
                    l.Drag("Min", ref _min);
                    l.Drag("Max", ref _max);
                }, FuModalSize.Small, new FuModalButton("OK", FuKeysCode.Enter));
            }
            layout.SameLine();
            if (_slider)
            {
                if (layout.Slider("##" + Id, ref v, _min, _max, flags: FuSliderFlags.NoDrag))
                    SetPortValue("Out", "core/float", v);
            }
            else
            {
                if (layout.Drag("##" + Id, ref v, "", _min, _max))
                    SetPortValue("Out", "core/float", v);
            }
        }

        public override void SetDefaultValues(FuNodalPort port)
        {
            port.DataType = "core/float";
            port.Data = 1f;
        }
    }
}