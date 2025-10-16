using Fu.Framework.Procedural;

namespace Fu.Framework.Procedural.Fields
{
    /// <summary>Component-wise multiply at sample time.</summary>
    public sealed class MulField2D : IField2D
    {
        private readonly IField2D _a, _b;
        public MulField2D(IField2D a, IField2D b) { _a = a; _b = b; }
        public float Sample(in SampleContext ctx) => _a.Sample(ctx) * _b.Sample(ctx);
    }
}
