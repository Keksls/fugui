using System.Collections.Generic;

namespace Fu.Framework
{
        /// <summary>
        /// Represents the Fu Nodal Port type.
        /// </summary>
        public sealed class FuNodalPort
        {
            #region State
            public int Id { get; set; } = FuNodeId.New();
            public string Name { get; set; }
            public FuNodalPortDirection Direction { get; set; }
            public FuNodalMultiplicity Multiplicity { get; set; } = FuNodalMultiplicity.Single;
            public HashSet<string> AllowedTypes { get; set; } = new HashSet<string>();

            public string DataType { get; set; }
            public object Data { get; set; }
            #endregion
        }
}