using System.Collections.Generic;

namespace Fu.Framework
{
        /// <summary>
        /// Flat node DTO; NodeType is used to rebuild the concrete FuNode.
        /// </summary>
        public sealed class FuNodeDto
        {
            #region State
            public int Id { get; set; }
            public string NodeType { get; set; }
            public float X { get; set; }
            public float Y { get; set; }
            public string CustomNodeDataJson { get; set; }
            public List<FuPortDto> Ports { get; set; } = new List<FuPortDto>();
            #endregion
        }
}