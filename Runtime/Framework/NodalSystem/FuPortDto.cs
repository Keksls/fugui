using System.Collections.Generic;

namespace Fu.Framework
{
        /// <summary>
        /// Flat port DTO; Data is stored as JSON by DataType.
        /// </summary>
        public sealed class FuPortDto
        {
            #region State
            public int Id { get; set; }
            public string Name { get; set; }
            public FuNodalPortDirection Direction { get; set; }
            public FuNodalMultiplicity Multiplicity { get; set; }
            public List<string> AllowedTypes { get; set; } = new List<string>();
            public string DataType { get; set; }
            public string DataJson { get; set; }
            #endregion
        }
}