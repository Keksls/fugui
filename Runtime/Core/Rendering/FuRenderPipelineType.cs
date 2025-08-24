namespace Fu
{
    public enum FuRenderPipelineType
    {
        /// <summary>
        /// Using Unity Built-in Render Pipeline
        /// </summary>
        BuiltIn = 0,
        /// <summary>
        /// Using Unity Universal Render Pipeline
        /// </summary>
        URP = 1,
        /// <summary>
        /// Using Unity High Definition Render Pipeline
        /// </summary>
        HDRP = 4,
        /// <summary>
        /// Using a custom Scriptable Render Pipeline
        /// </summary>
        CustomSRP = 4,
        /// <summary>
        /// Render Pipeline is unknown
        /// </summary>
        Unknown = 8,
        /// <summary>
        /// Render Pipeline is supported (URP or HDRP)
        /// </summary>
        Supported = HDRP | URP,
        /// <summary>
        /// Render Pipeline is not supported (Built-in, Custom or Unknown)
        /// </summary>
        Unsupported = BuiltIn | CustomSRP | Unknown
    }
}