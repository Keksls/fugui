using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework.Demo
{
    /// <summary>Float variable node.</summary>
    public sealed class FloatNode : FuNode
    {
        #region State
        public override string Title => "Float";
        public override float Width => 96f;
        public override Color? NodeColor => _color;

        private Color _color;
        private bool _slider = false;
        private float _min = 0f, _max = 100f;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the Float Node class.
        /// </summary>
        /// <param name="color">The color value.</param>
        public FloatNode(Color color) { _color = color; }
        #endregion

        #region Methods
        /// <summary>
        /// Creates the default ports.
        /// </summary>
        public override void CreateDefaultPorts()
        {
            AddPort(new FuNodalPort { Name = "Out", Direction = FuNodalPortDirection.Out, DataType = "core/float", AllowedTypes = new HashSet<string> { "core/float" }, Data = 1f, Multiplicity = FuNodalMultiplicity.Many });
        }

        /// <summary>
        /// Runs the compute workflow.
        /// </summary>
        public override void Compute() { }

        /// <summary>
        /// Handles the Draw event.
        /// </summary>
        public override void OnDraw()
        {
            float v = GetPortValue<float>("Out", 0f);
            if (Fugui.Layout.ClickableText(Icons.Settings_solid))
            {
                Fugui.ShowModal("Float Settings " + Id, () =>
                {
                    Fugui.Layout.CheckBox("Use Slider", ref _slider);
                    Fugui.Layout.Drag("Min", ref _min);
                    Fugui.Layout.Drag("Max", ref _max);
                }, FuModalSize.Small, new FuModalButton("OK", FuKeysCode.Enter));
            }
            Fugui.Layout.SameLine();
            if (_slider)
            {
                if (Fugui.Layout.Slider("##" + Id, ref v, _min, _max, flags: FuSliderFlags.NoDrag))
                    SetPortValue("Out", "core/float", v);
            }
            else
            {
                if (Fugui.Layout.Drag("##" + Id, ref v, "", _min, _max))
                    SetPortValue("Out", "core/float", v);
            }
        }

        /// <summary>
        /// Sets the default values.
        /// </summary>
        /// <param name="port">The port value.</param>
        public override void SetDefaultValues(FuNodalPort port)
        {
            port.DataType = "core/float";
            port.Data = 1f;
        }
        #endregion
    }
}
