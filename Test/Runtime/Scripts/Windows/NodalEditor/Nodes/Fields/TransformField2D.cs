using UnityEngine;
using Fu.Framework.Procedural;

namespace Fu.Framework.Procedural.Fields
{
    /// <summary>Affine transform of sampling coordinates.</summary>
    public sealed class TransformField2D : IField2D
    {
        private readonly IField2D _src;
        private readonly Vector2 _translate;
        private readonly Vector2 _scale;
        private readonly float _rotationDeg;

        public TransformField2D(IField2D src, Vector2 translate, Vector2 scale, float rotationDeg)
        {
            _src = src;
            _translate = translate;
            _scale = new Vector2(scale.x==0f?1e-6f:scale.x, scale.y==0f?1e-6f:scale.y);
            _rotationDeg = rotationDeg;
        }

        public float Sample(in SampleContext ctx)
        {
            float rad = _rotationDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);

            Vector2 p = ctx.XY;
            Vector2 t = p - _translate;
            Vector2 s = new Vector2(t.x/_scale.x, t.y/_scale.y);
            Vector2 r = new Vector2(cos*s.x + sin*s.y, -sin*s.x + cos*s.y);

            var nctx = ctx; nctx.XY = r;
            return _src.Sample(nctx);
        }
    }
}
