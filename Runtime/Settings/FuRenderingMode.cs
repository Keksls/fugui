namespace Fu
{
    /// <summary>
    /// Lists the available Fugui rendering pipelines.
    /// </summary>
    public enum FuRenderingMode
    {
        /// <summary>
        /// Uses the regular window-aware rendering pipeline.
        /// </summary>
        Standard,

        /// <summary>
        /// Uses the optimized pipeline for contexts that only submit global or raw draw lists.
        /// </summary>
        DrawListOnly
    }
}
