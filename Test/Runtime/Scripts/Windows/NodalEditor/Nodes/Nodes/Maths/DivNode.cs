using UnityEngine;

namespace Fu.Framework.Demo
{
    public sealed class DivNode : BinaryOperationNode
    {
        public override string Title => "Divide";
        protected override float DefaultValue => 1f;
        protected override float Operate(float a, float b) => Mathf.Approximately(b, 0f) ? 0f : a / b;
    }
}