using System.Collections.Generic;
using UnityEngine;
using Fu.Framework.Procedural;
using Fu.Framework.Procedural.Fields;

namespace Fu.Framework
{
    /// <summary>Approximate erosion node (fast, local) with visual preview.</summary>
    public sealed class ErosionNode : FuNode
    {
        public override string Title => "Erosion (Field2D)";
        public override float Width => 280f;
        public override Color? NodeColor => null;

        private Texture2D _preview;
        private bool _drawPreview = true;
        private const int _previewResolution = 128;

        public override void CreateDefaultPorts()
        {
            AddPort(new FuNodalPort
            {
                Name = "Src",
                Direction = FuNodalPortDirection.In,
                DataType = "core/field2D",
                AllowedTypes = new HashSet<string> { "core/field2D" },
                Data = new ConstantField2D(0f),
                Multiplicity = FuNodalMultiplicity.Single
            });
            AddPort(new FuNodalPort
            {
                Name = "Iterations",
                Direction = FuNodalPortDirection.In,
                DataType = "core/int",
                AllowedTypes = new HashSet<string> { "core/int" },
                Data = 8,
                Multiplicity = FuNodalMultiplicity.Single
            });
            AddPort(new FuNodalPort
            {
                Name = "Talus",
                Direction = FuNodalPortDirection.In,
                DataType = "core/float",
                AllowedTypes = new HashSet<string> { "core/float" },
                Data = 0.02f,
                Multiplicity = FuNodalMultiplicity.Single
            });
            AddPort(new FuNodalPort
            {
                Name = "Strength",
                Direction = FuNodalPortDirection.In,
                DataType = "core/float",
                AllowedTypes = new HashSet<string> { "core/float" },
                Data = 0.25f,
                Multiplicity = FuNodalMultiplicity.Single
            });
            AddPort(new FuNodalPort
            {
                Name = "WorldStep",
                Direction = FuNodalPortDirection.In,
                DataType = "core/float",
                AllowedTypes = new HashSet<string> { "core/float" },
                Data = 0.005f,
                Multiplicity = FuNodalMultiplicity.Single
            });
            AddPort(new FuNodalPort
            {
                Name = "Out",
                Direction = FuNodalPortDirection.Out,
                DataType = "core/field2D",
                AllowedTypes = new HashSet<string> { "core/field2D" },
                Data = null,
                Multiplicity = FuNodalMultiplicity.Many
            });
        }

        public override bool CanConnect(FuNodalPort fromPort, FuNodalPort toPort) => true;

        public override void Compute()
        {
            var src = GetPortValue<IField2D>("Src", new ConstantField2D(0f));
            int iterations = GetPortValue<int>("Iterations", 8);
            float talus = GetPortValue<float>("Talus", 0.02f);
            float strength = GetPortValue<float>("Strength", 0.25f);
            float step = GetPortValue<float>("WorldStep", 0.005f);

            var field = new ErodedField2D(src, iterations, talus, strength, step);
            SetPortValue("Out", "core/field2D", field);

            UpdatePreview(field);
        }

        private void UpdatePreview(IField2D field)
        {
            if (_preview == null)
            {
                _preview = new Texture2D(_previewResolution, _previewResolution, TextureFormat.RGB24, false, true)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
            }

            for (int y = 0; y < _previewResolution; y++)
            {
                for (int x = 0; x < _previewResolution; x++)
                {
                    Vector2 uv = new Vector2((float)x / _previewResolution, (float)y / _previewResolution);
                    float v = Mathf.Clamp01(field.Sample(new SampleContext() { XY = uv }));
                    _preview.SetPixel(x, y, new Color(v, v, v, 1f));
                }
            }

            _preview.Apply();
        }

        public override void OnDraw(FuLayout layout)
        {
            if (_drawPreview && _preview != null)
            {
                float width = layout.GetAvailableWidth() / Fugui.Scale;
                layout.Image("##" + Id, _preview, new FuElementSize(width, width));
            }

            if (layout.Button(_drawPreview ? Icons.ArrowUp_solid : Icons.ArrowDown_solid, new FuElementSize(-1f, 16f)))
            {
                _drawPreview = !_drawPreview;
            }
        }

        public override void SetDefaultValues(FuNodalPort port) { }
    }
}