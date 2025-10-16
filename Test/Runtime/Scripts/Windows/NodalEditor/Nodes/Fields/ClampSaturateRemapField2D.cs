using Fu.Framework.Procedural;
using UnityEngine;

namespace Fu.Framework.Procedural.Fields
{
    /// <summary>Clamp field to [min,max].</summary>
    public sealed class ClampField2D : IField2D
    {
        private readonly IField2D _src; private readonly float _min,_max;
        public ClampField2D(IField2D src, float min, float max) { _src = src; _min=min; _max=max; }
        public float Sample(in SampleContext ctx) => Mathf.Clamp(_src.Sample(ctx), _min, _max);
    }

    /// <summary>Saturate to [0,1].</summary>
    public sealed class SaturateField2D : IField2D
    {
        private readonly IField2D _src;
        public SaturateField2D(IField2D src) { _src = src; }
        public float Sample(in SampleContext ctx) => Mathf.Clamp01(_src.Sample(ctx));
    }

    /// <summary>Remap input from [inMin,inMax] to [outMin,outMax].</summary>
    public sealed class RemapField2D : IField2D
    {
        private readonly IField2D _src; private readonly float _inMin,_inMax,_outMin,_outMax;
        public RemapField2D(IField2D src, float inMin, float inMax, float outMin, float outMax)
        { _src=src; _inMin=inMin; _inMax=inMax; _outMin=outMin; _outMax=outMax; }
        public float Sample(in SampleContext ctx)
        {
            float t = Mathf.InverseLerp(_inMin, _inMax, _src.Sample(ctx));
            return Mathf.Lerp(_outMin, _outMax, t);
        }
    }
}
