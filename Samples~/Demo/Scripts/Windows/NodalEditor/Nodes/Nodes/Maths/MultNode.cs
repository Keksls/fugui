namespace Fu.Framework.Demo
{
    public sealed class MultNode : BinaryOperationNode
    {
        public override string Title => "Multiply";
        protected override float DefaultValue => 1f;
        protected override float Operate(float a, float b) => a * b;
    }
}