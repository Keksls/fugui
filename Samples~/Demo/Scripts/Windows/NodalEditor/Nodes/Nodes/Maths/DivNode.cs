using UnityEngine;

namespace Fu.Framework.Demo
{
    /// <summary>
    /// Represents the Div Node type.
    /// </summary>
    public sealed class DivNode : BinaryOperationNode
    {
        #region State
        public override string Title => "Divide";
        protected override float DefaultValue => 1f;
        #endregion

        #region Methods
        /// <summary>
        /// Returns the operate result.
        /// </summary>
        /// <param name="a">The a value.</param>
        /// <param name="b">The b value.</param>
        /// <returns>The result of the operation.</returns>
        protected override float Operate(float a, float b) => Mathf.Approximately(b, 0f) ? 0f : a / b;
        #endregion
    }
}