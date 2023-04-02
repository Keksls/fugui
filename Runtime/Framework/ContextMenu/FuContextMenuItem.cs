using System;
using System.Collections.Generic;

namespace Fu.Framework
{
    /// <summary>
    /// A struct that will store the data for a single context menu item
    /// </summary>
    public class FuContextMenuItem
    {
        // Label for the item
        public string Label;
        // Shortcut for the item
        public string Shortcut;
        // Whether the item is enabled
        public Func<bool> Enabled;
        // Whether the item is a separator
        public bool IsSeparator;
        // Action to perform when the item is clicked
        public Action ClickAction;
        // Children items for the item
        public List<FuContextMenuItem> Children;

        /// <summary>
        /// Constructor for the context menu item
        /// </summary>
        /// <param name="label">The label for the item</param>
        /// <param name="shortcut">The shortcut for the item</param>
        /// <param name="enabled">Whether the item is enabled</param>
        /// <param name="isSeparator">Whether the item is a separator</param>
        /// <param name="clickAction">The action to perform when the item is clicked</param>
        /// <param name="children">The children items for the item</param>
        public FuContextMenuItem(string label, string shortcut, Func<bool> enabled, bool isSeparator, Action clickAction, List<FuContextMenuItem> children = null)
        {
            Label = label;
            Shortcut = shortcut;
            Enabled = enabled;
            IsSeparator = isSeparator;
            ClickAction = clickAction;
            Children = children ?? new List<FuContextMenuItem>();
        }
    }

}