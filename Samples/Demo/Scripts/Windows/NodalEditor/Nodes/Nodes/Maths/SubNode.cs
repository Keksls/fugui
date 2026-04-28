namespace Fu.Framework.Demo
{
    /// <summary>
    /// Represents the Sub Node type.
    /// </summary>
    public sealed class SubNode : BinaryOperationNode
    {
        #region State
        public override string Title => "Subtract";
        protected override float DefaultValue => 0f;
        #endregion

        #region Methods
        /// <summary>
        /// Returns the operate result.
        /// </summary>
        /// <param name="a">The a value.</param>
        /// <param name="b">The b value.</param>
        /// <returns>The result of the operation.</returns>
        protected override float Operate(float a, float b) => a - b;
        #endregion
    }
}