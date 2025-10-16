using UnityEngine;
using Fu.Framework.Procedural;

namespace Fu.Framework.Procedural.Fields
{
    /// <summary>Approx local thermal-like erosion (preview-friendly).</summary>
    public sealed class ErodedField2D : IField2D
    {
        private readonly IField2D _src;
        private readonly int _iterations;
        private readonly float _talus;
        private readonly float _strength;
        private readonly float _h;

        public ErodedField2D(IField2D src, int iterations, float talus, float strength, float worldStep)
        {
            _src=src; _iterations=Mathf.Max(1,iterations); _talus=Mathf.Max(0f,talus); _strength=Mathf.Clamp01(strength); _h=Mathf.Max(1e-4f,worldStep);
        }

        public float Sample(in SampleContext ctx)
        {
            float hL = SampleAt(ctx, -_h, 0);
            float hC = SampleAt(ctx, 0, 0);
            float hR = SampleAt(ctx, +_h, 0);
            float hD = SampleAt(ctx, 0, -_h);
            float hU = SampleAt(ctx, 0, +_h);

            float center = hC;
            for (int it=0; it<_iterations; it++)
            {
                float delta = 0f;
                TryMove(ref center, hL, ref delta);
                TryMove(ref center, hR, ref delta);
                TryMove(ref center, hD, ref delta);
                TryMove(ref center, hU, ref delta);
                center += delta;
            }
            return center;
        }

        private void TryMove(ref float c, float nb, ref float delta)
        {
            float diff = c - nb;
            if (diff > _talus)
            {
                float move = (diff - _talus) * _strength * 0.25f;
                delta -= move;
            }
        }

        private float SampleAt(SampleContext ctx, float dx, float dy)
        {
            var n = ctx; n.XY = new UnityEngine.Vector2(ctx.XY.x + dx, ctx.XY.y + dy);
            return _src.Sample(n);
        }
    }
}
