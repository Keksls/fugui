using Fu.Framework.Procedural;

namespace Fu.Framework.Procedural.Fields
{
    /// <summary>Component-wise addition at sample time.</summary>
    public sealed class AddField2D : IField2D
    {
        private readonly IField2D _a, _b;
        public AddField2D(IField2D a, IField2D b) { _a = a; _b = b; }
        public float Sample(in SampleContext ctx) => _a.Sample(ctx) + _b.Sample(ctx);
    }
}
