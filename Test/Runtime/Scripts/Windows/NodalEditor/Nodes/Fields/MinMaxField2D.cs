using Fu.Framework.Procedural;
using UnityEngine;

namespace Fu.Framework.Procedural.Fields
{
    /// <summary>Min(a,b) at sample time.</summary>
    public sealed class MinField2D : IField2D
    {
        private readonly IField2D _a, _b;
        public MinField2D(IField2D a, IField2D b) { _a = a; _b = b; }
        public float Sample(in SampleContext ctx) => Mathf.Min(_a.Sample(ctx), _b.Sample(ctx));
    }

    /// <summary>Max(a,b) at sample time.</summary>
    public sealed class MaxField2D : IField2D
    {
        private readonly IField2D _a, _b;
        public MaxField2D(IField2D a, IField2D b) { _a = a; _b = b; }
        public float Sample(in SampleContext ctx) => Mathf.Max(_a.Sample(ctx), _b.Sample(ctx));
    }
}
