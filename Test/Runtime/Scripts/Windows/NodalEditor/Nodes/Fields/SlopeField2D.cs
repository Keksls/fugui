using UnityEngine;
using Fu.Framework.Procedural;

namespace Fu.Framework.Procedural.Fields
{
    /// <summary>Gradient magnitude using finite differences.</summary>
    public sealed class SlopeField2D : IField2D
    {
        private readonly IField2D _src;
        private readonly float _h;

        public SlopeField2D(IField2D src, float worldStep) { _src=src; _h = Mathf.Max(1e-5f, worldStep); }

        public float Sample(in SampleContext ctx)
        {
            var c = ctx;
            c.XY = new Vector2(ctx.XY.x - _h, ctx.XY.y);
            float hm = _src.Sample(c);
            c.XY = new Vector2(ctx.XY.x + _h, ctx.XY.y);
            float hp = _src.Sample(c);
            c.XY = new Vector2(ctx.XY.x, ctx.XY.y - _h);
            float vm = _src.Sample(c);
            c.XY = new Vector2(ctx.XY.x, ctx.XY.y + _h);
            float vp = _src.Sample(c);

            float dx = (hp - hm) * 0.5f / _h;
            float dy = (vp - vm) * 0.5f / _h;
            return Mathf.Sqrt(dx*dx + dy*dy);
        }
    }
}
