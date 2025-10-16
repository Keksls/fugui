using System.Collections.Generic;
using UnityEngine;
using Fu.Framework.Procedural;
using Fu.Framework.Procedural.Fields;

namespace Fu.Framework
{
    /// <summary>Multiply two Field2D (with visual preview).</summary>
    public sealed class MulFieldNode : FuNode
    {
        public override string Title => "Multiply (Field2D)";
        public override float Width => 240f;
        public override System.Nullable<Color> NodeColor => null;

        // --- Preview ---
        private Texture2D _preview;
        private bool _drawPreview = true;
        private const int _previewResolution = 128;

        public override void CreateDefaultPorts()
        {
            AddPort(new FuNodalPort { Name = "A", Direction = FuNodalPortDirection.In, DataType = "core/field2D", AllowedTypes = new HashSet<string> { "core/field2D" }, Data = new ConstantField2D(1f), Multiplicity = FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort { Name = "B", Direction = FuNodalPortDirection.In, DataType = "core/field2D", AllowedTypes = new HashSet<string> { "core/field2D" }, Data = new ConstantField2D(1f), Multiplicity = FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort { Name = "Out", Direction = FuNodalPortDirection.Out, DataType = "core/field2D", AllowedTypes = new HashSet<string> { "core/field2D" }, Data = null, Multiplicity = FuNodalMultiplicity.Many });
        }

        public override bool CanConnect(FuNodalPort fromPort, FuNodalPort toPort) => true;

        public override void Compute()
        {
            var a = GetPortValue<IField2D>("A", new ConstantField2D(1f));
            var b = GetPortValue<IField2D>("B", new ConstantField2D(1f));

            var field = new MulField2D(a, b);
            SetPortValue("Out", "core/field2D", field);

            UpdatePreview(field);
        }

        private void UpdatePreview(IField2D field)
        {
            if (_preview == null || _preview.width != _previewResolution || _preview.height != _previewResolution)
            {
                _preview = new Texture2D(_previewResolution, _previewResolution, TextureFormat.RGB24, false, true)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
            }

            var ctx = new SampleContext();
            for (int y = 0; y < _previewResolution; y++)
            {
                for (int x = 0; x < _previewResolution; x++)
                {
                    Vector2 uv = new Vector2((float)x / _previewResolution, (float)y / _previewResolution);
                    ctx.XY = uv;
                    float v = Mathf.Clamp01(field.Sample(ctx));
                    _preview.SetPixel(x, y, new Color(v, v, v, 1f));
                }
            }
            _preview.Apply();
        }

        public override void OnDraw(FuLayout layout)
        {
            if (_drawPreview && _preview != null)
            {
                float avW = layout.GetAvailableWidth() / Fugui.Scale;
                layout.Image("##prevMul" + Id, _preview, new FuElementSize(avW, avW));
            }

            if (layout.Button(_drawPreview ? Icons.ArrowUp_solid : Icons.ArrowDown_solid, new FuElementSize(-1f, 16f)))
                _drawPreview = !_drawPreview;
        }

        public override void SetDefaultValues(FuNodalPort port) { }
    }
}