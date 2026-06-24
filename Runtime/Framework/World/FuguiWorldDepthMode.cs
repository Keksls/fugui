namespace Fu
{
    /// <summary>
    /// Describes how a Fugui world surface interacts with the camera depth buffer.
    /// </summary>
    public enum FuguiWorldDepthMode
    {
        /// <summary>
        /// Renders the surface without testing or writing depth.
        /// </summary>
        None,

        /// <summary>
        /// Renders the surface only where it passes the camera depth test.
        /// </summary>
        Test,

        /// <summary>
        /// Renders the surface with depth testing and writes its own depth.
        /// </summary>
        TestAndWrite
    }
}
