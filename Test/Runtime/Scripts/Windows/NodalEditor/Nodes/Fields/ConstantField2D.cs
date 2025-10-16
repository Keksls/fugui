using Fu.Framework.Procedural;

namespace Fu.Framework.Procedural.Fields
{
    /// <summary>Constant scalar field.</summary>
    public sealed class ConstantField2D : IField2D
    {
        private readonly float _v;
        public ConstantField2D(float v) { _v = v; }
        public float Sample(in SampleContext ctx) => _v;
    }
}
