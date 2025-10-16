using UnityEngine;
using Fu.Framework.Procedural;

namespace Fu.Framework.Procedural.Fields
{
    /// <summary>Domain warp via U and V displacement fields.</summary>
    public sealed class WarpField2D : IField2D
    {
        private readonly IField2D _src, _u, _v;
        private readonly float _strength;

        public WarpField2D(IField2D src, IField2D u, IField2D v, float strength) { _src=src; _u=u; _v=v; _strength=strength; }

        public float Sample(in SampleContext ctx)
        {
            float du = _u.Sample(ctx) * _strength;
            float dv = _v.Sample(ctx) * _strength;
            var n = ctx; n.XY = new Vector2(ctx.XY.x + du, ctx.XY.y + dv);
            return _src.Sample(n);
        }
    }
}
