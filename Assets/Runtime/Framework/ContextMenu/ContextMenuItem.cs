using System;
using System.Collections.Generic;

namespace Fu.Framework
{
    // Define a struct to store the data for a single menu item
    public class ContextMenuItem
    {
        public string Label;
        public string Shortcut;
        public bool Enabled;
        public bool IsSeparator;
        public Action ClickAction;
        public List<ContextMenuItem> Children;

        public ContextMenuItem(string label, string shortcut, bool enabled, bool isSeparator, Action clickAction, List<ContextMenuItem> children = null)
        {
            Label = label;
            Shortcut = shortcut;
            Enabled = enabled;
            IsSeparator = isSeparator;
            ClickAction = clickAction;
            Children = children ?? new List<ContextMenuItem>();
        }
    }
}