namespace Fu.Framework
{
    public partial class FuGrid
    {
        /// <summary>
        /// Draw a knob button
        /// </summary>
        /// <param name="label">Laberl of the knob</param>
        /// <param name="value">value of the knob</param>
        /// <param name="min">minimum value</param>
        /// <param name="max">maximum value</param>
        /// <param name="variant">type of knob variant (Try it !)</param>
        /// <param name="steps">number of step of the knob (only for stepped knob)</param>
        /// <param name="speed">speed of the drag</param>
        /// <param name="format">string format of the drag</param>
        /// <param name="size">size of the knob</param>
        /// <param name="flags">behaviour flag</param>
        /// <returns>true if value chage</returns>
        public override bool Knob(string label, ref float value, float min = 0, float max = 100, FuKnobVariant variant = FuKnobVariant.Wiper, int steps = 10, float speed = 1, string format = null, float size = 64, FuKnobFlags flags = FuKnobFlags.Default)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(label, FuTextStyle.Default);
            return base.Knob(label, ref value, min, max, variant, steps, speed, format, size, flags);
        }
    }
}