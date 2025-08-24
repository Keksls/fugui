namespace Fu
{
    public enum FuguiRenderingState
    {
        /// <summary>
        /// The rendering state is not set or is in an idle state.
        /// </summary>
        None = 0,
        /// <summary>
        /// The rendering state is currently updating.
        /// This indicates that Fugui is processing updates to the UI.
        /// Calling all the update methods and window methods.
        /// </summary>
        Updating = 1,
        /// <summary>
        /// The rendering state has completed the update phase.
        /// Fugui is now ready to render the UI.
        /// </summary>
        UpdateComplete = 2,
        /// <summary>
        /// The rendering state is currently rendering.
        /// This is handled by Render Feature.
        /// </summary>
        Rendering = 3,
        /// <summary>
        /// The rendering state has completed the rendering phase.
        /// We can now restart the rendering process on next frame.
        /// </summary>
        RenderComplete = 4,
    }
}