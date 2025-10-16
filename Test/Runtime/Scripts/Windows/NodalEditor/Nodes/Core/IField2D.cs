using UnityEngine;

namespace Fu.Framework.Procedural
{
    /// <summary>Sampling context propagated through the graph.</summary>
    public struct SampleContext
    {
        public Vector2 XY;
        public Vector2 UV;
        public int LOD;
    }

    /// <summary>Scalar 2D field interface.</summary>
    public interface IField2D
    {
        /// <summary>Sample the field at the given context.</summary>
        float Sample(in SampleContext ctx);
    }
}
