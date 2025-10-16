using Fu.Framework.Procedural;
using UnityEngine;

namespace Fu.Framework.Procedural.Fields
{
    public sealed class AbsField2D : IField2D
    {
        private readonly IField2D _src;
        public AbsField2D(IField2D src) { _src = src; }
        public float Sample(in SampleContext ctx) => Mathf.Abs(_src.Sample(ctx));
    }

    public sealed class Invert01Field2D : IField2D
    {
        private readonly IField2D _src;
        public Invert01Field2D(IField2D src) { _src = src; }
        public float Sample(in SampleContext ctx) => 1f - _src.Sample(ctx);
    }

    public sealed class PowField2D : IField2D
    {
        private readonly IField2D _src; private readonly float _exp;
        public PowField2D(IField2D src, float exp) { _src = src; _exp = exp; }
        public float Sample(in SampleContext ctx) => Mathf.Pow(Mathf.Max(0f,_src.Sample(ctx)), _exp);
    }
}
