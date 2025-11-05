namespace Fu.Framework.Demo
{
    public sealed class SubNode : BinaryOperationNode
    {
        public override string Title => "Subtract";
        protected override float DefaultValue => 0f;
        protected override float Operate(float a, float b) => a - b;
    }
}