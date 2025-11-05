namespace Fu.Framework.Demo
{
    public sealed class AddNode : BinaryOperationNode
    {
        public override string Title => "Add";
        protected override float DefaultValue => 0f;
        protected override float Operate(float a, float b) => a + b;
    }
}