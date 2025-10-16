using UnityEngine;
using Fu.Framework.Procedural;

namespace Fu.Framework.Procedural.Fields
{
    /// <summary>Ridged noise built from Perlin: 1 - |noise * 2 - 1|</summary>
    public sealed class RidgeField2D : IField2D
    {
        public float Frequency = 1f;
        public float Amplitude = 1f;
        public Vector2 Offset = Vector2.zero;

        public float Sample(in SampleContext ctx)
        {
            float n = Mathf.PerlinNoise(ctx.XY.x * Frequency + Offset.x, ctx.XY.y * Frequency + Offset.y);
            n = 1f - Mathf.Abs(n * 2f - 1f);
            return n * Amplitude;
        }
    }
}
