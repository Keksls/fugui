
namespace Fu.Framework
{
        /// <summary>
        /// Represents the Fu Nodal Edge type.
        /// </summary>
        public sealed class FuNodalEdge
        {
            #region State
            public int Id { get; set; } = FuNodeId.New();
            public int FromNodeId { get; set; }
            public int FromPortId { get; set; }
            public int ToNodeId { get; set; }
            public int ToPortId { get; set; }
            #endregion
        }
}