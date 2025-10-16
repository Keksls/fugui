using UnityEngine;
using Fu.Framework.Procedural;

namespace Fu.Framework.Procedural.Fields
{
    /// <summary>Fractal Brownian Motion over Perlin.</summary>
    public sealed class FBMField2D : IField2D
    {
        public float Frequency = 1f;
        public float Amplitude = 1f;
        public int Octaves = 5;
        public float Lacunarity = 2.0f;
        public float Gain = 0.5f;
        public Vector2 Offset = Vector2.zero;

        public float Sample(in SampleContext ctx)
        {
            float amp = Amplitude;
            float freq = Frequency;
            float sum = 0f;
            float x = ctx.XY.x + Offset.x;
            float y = ctx.XY.y + Offset.y;
            for (int i = 0; i < Mathf.Max(1, Octaves); i++)
            {
                sum += amp * Mathf.PerlinNoise(x * freq, y * freq);
                freq *= Lacunarity;
                amp *= Gain;
            }
            return sum;
        }
    }
}
