namespace Fu.Framework
{
    public partial class FuGrid
    {
        protected override bool _customToggle(string text, ref bool value, string textLeft, string textRight, FuToggleFlags flags)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(text, FuTextStyle.Default);
            return base._customToggle(text, ref value, textLeft, textRight, flags);
        }
    }
}