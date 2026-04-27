using System.Collections.Generic;

namespace Fu.Framework
{
    /// <summary>
    /// Flat, serializable graph DTO.
    /// </summary>
    public sealed class FuGraphDto
    {
        #region State
        public string Version { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public List<FuNodeDto> Nodes { get; set; } = new List<FuNodeDto>();
        public List<FuNodalEdge> Edges { get; set; } = new List<FuNodalEdge>();
        #endregion
    }
}