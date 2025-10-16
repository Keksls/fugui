using System.Collections.Generic;
using UnityEngine;
using Fu.Framework.Procedural.Fields;
using Fu.Framework.Procedural;

namespace Fu.Framework
{
    /// <summary>Perlin noise field generator (with small preview).</summary>
    public sealed class NoiseFieldNode : FuNode
    {
        public override string Title => "Noise Field";
        public override float Width => 240f;
        public override Color? NodeColor => null;
        private Texture2D _prev; private bool _preview = true;

        public override void CreateDefaultPorts()
        {
            AddPort(new FuNodalPort{ Name="Frequency", Direction=FuNodalPortDirection.In, DataType="core/float", AllowedTypes=new HashSet<string>{"core/float"}, Data=1f, Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="Amplitude", Direction=FuNodalPortDirection.In, DataType="core/float", AllowedTypes=new HashSet<string>{"core/float"}, Data=1f, Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="Offset", Direction=FuNodalPortDirection.In, DataType="core/v2", AllowedTypes=new HashSet<string>{"core/v2"}, Data=Vector2.zero, Multiplicity=FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort{ Name="Out", Direction=FuNodalPortDirection.Out, DataType="core/field2D", AllowedTypes=new HashSet<string>{"core/field2D"}, Data=null, Multiplicity=FuNodalMultiplicity.Many });
        }

        public override bool CanConnect(FuNodalPort fromPort, FuNodalPort toPort)=>true;

        public override void Compute()
        {
            float freq = GetPortValue<float>("Frequency", 1f);
            float amp = GetPortValue<float>("Amplitude", 1f);
            Vector2 off = GetPortValue<Vector2>("Offset", Vector2.zero);
            var f = new PerlinField2D{ Frequency=freq, Amplitude=amp, Offset=off };
            SetPortValue("Out","core/field2D", f);

            if (_preview)
            {
                const int W=96,H=96;
                if (_prev==null || _prev.width!=W || _prev.height!=H)
                {
                    _prev = new Texture2D(W,H,TextureFormat.RGB24,false,true);
                    _prev.filterMode=FilterMode.Point; _prev.wrapMode=TextureWrapMode.Clamp;
                }
                var ctx = new Fu.Framework.Procedural.SampleContext();
                for(int y=0;y<H;y++)
                for(int x=0;x<W;x++)
                {
                    ctx.XY = new Vector2((float)x/W,(float)y/H);
                    float v = f.Sample(ctx);
                    _prev.SetPixel(x,y,new Color(v,v,v,1f));
                }
                _prev.Apply();
            }
        }

        public override void OnDraw(FuLayout layout)
        {
            if (_preview && _prev!=null)
            {
                float avW = (layout.GetAvailableWidth()/Fugui.Scale);
                layout.Image("##prev"+Id, _prev, new FuElementSize(avW,avW));
            }
            if (layout.Button(_preview ? Icons.ArrowUp_solid : Icons.ArrowDown_solid, new FuElementSize(-1f,16f)))
                _preview = !_preview;
        }

        public override void SetDefaultValues(FuNodalPort port) { }
    }
}
