using Fu.Framework.Procedural;
using UnityEngine;

namespace Fu.Framework.Procedural.Fields
{
    public sealed class LerpField2D : IField2D
    {
        private readonly IField2D _a, _b, _t;
        public LerpField2D(IField2D a, IField2D b, IField2D t) { _a=a; _b=b; _t=t; }
        public float Sample(in SampleContext ctx) => Mathf.Lerp(_a.Sample(ctx), _b.Sample(ctx), Mathf.Clamp01(_t.Sample(ctx)));
    }

    public sealed class StepField2D : IField2D
    {
        private readonly IField2D _src; private readonly float _edge;
        public StepField2D(IField2D src, float edge) { _src=src; _edge=edge; }
        public float Sample(in SampleContext ctx) => _src.Sample(ctx) < _edge ? 0f : 1f;
    }

    public sealed class SmoothStepField2D : IField2D
    {
        private readonly IField2D _src; private readonly float _e0,_e1;
        public SmoothStepField2D(IField2D src, float edge0, float edge1) { _src=src; _e0=edge0; _e1=edge1; }
        public float Sample(in SampleContext ctx) => Mathf.SmoothStep(_e0, _e1, _src.Sample(ctx));
    }
}
