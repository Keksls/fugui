namespace Fugui.Framework
{
    public partial class UIGrid
    {
        protected override bool _customToggle(string text, ref bool value, string textLeft, string textRight, ToggleFlags flags)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(text, UITextStyle.Default);
            return base._customToggle(text, ref value, textLeft, textRight, flags);
        }
    }
}