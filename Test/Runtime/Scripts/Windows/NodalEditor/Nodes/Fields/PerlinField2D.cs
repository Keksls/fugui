using UnityEngine;
using Fu.Framework.Procedural;

namespace Fu.Framework.Procedural.Fields
{
    /// <summary>Perlin noise field based on Mathf.PerlinNoise.</summary>
    public sealed class PerlinField2D : IField2D
    {
        public float Frequency = 1f;
        public float Amplitude = 1f;
        public Vector2 Offset = Vector2.zero;
        public float Sample(in SampleContext ctx)
        {
            float x = ctx.XY.x * Frequency + Offset.x;
            float y = ctx.XY.y * Frequency + Offset.y;
            return Amplitude * Mathf.PerlinNoise(x, y);
        }
    }
}
