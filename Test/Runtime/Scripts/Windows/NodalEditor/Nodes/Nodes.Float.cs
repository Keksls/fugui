using UnityEngine;
using System.Collections.Generic;

namespace Fu.Framework
{
    /// <summary>
    /// Float variable node
    /// </summary>
    public sealed class FloatNode : FuNode
    {
        public override string Title => "Float";
        public override float Width => 200f;
        public override Color? NodeColor => color;

        private bool slider = false;
        private float min = float.MinValue;
        private float max = float.MaxValue;
        private Color color;

        public FloatNode(Color col)
        {
            color = col;
        }

        public override bool CanConnect(FuNodalPort fromPort, FuNodalPort toPort) => true;

        public override void Compute() { }

        public override void CreateDefaultPorts()
        {
            FuNodalPort portOut = new FuNodalPort
            {
                Name = "Out",
                Direction = FuNodalPortDirection.Out,
                DataType = "core/float",
                AllowedTypes = new HashSet<string> { "core/float" },
                Data = 0f,
                Multiplicity = FuNodalMultiplicity.Many
            };
            AddPort(portOut);
        }

        public override void OnDraw(FuLayout layout)
        {
            var val = GetPortValue<float>("Out", 0f);
            if (layout.ClickableText(Icons.Settings_solid))
            {
                Fugui.ShowModal("Float Settings " + Id, (layout) =>
                {
                    layout.CheckBox("Use Slider", ref slider);
                    layout.Drag("Min", ref min);
                    layout.Drag("Max", ref max);
                }, FuModalSize.Small, new FuModalButton("OK", FuKeysCode.Enter));
            }
            layout.SameLine();
            if (slider)
            {
                if (layout.Slider("##" + Id, ref val, min, max, flags: FuSliderFlags.NoDrag))
                    SetPortValue("Out", "core/float", val);
            }
            else
            {
                if (layout.Drag("##" + Id, ref val, "", min, max))
                    SetPortValue("Out", "core/float", val);
            }
        }

        public override void SetDefaultValues(FuNodalPort port)
        {
            port.DataType = "core/float";
            port.Data = 0f;
        }
    }
}
