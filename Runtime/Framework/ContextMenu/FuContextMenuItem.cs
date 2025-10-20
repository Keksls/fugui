using System;
using System.Collections.Generic;
using UnityEngine;

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
        // Optional image displayed in the context menu 
        public Texture2D Image;
        // Optional image size
        public FuElementSize Size;
        // Defines the type of this item (Normal, Separator, or Image)
        public FuContextMenuItemType Type;

        /// <summary>
        /// Constructor for a standard context menu item
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
            Image = null;
            Shortcut = shortcut;
            Enabled = enabled;
            IsSeparator = isSeparator;
            ClickAction = clickAction;
            Children = children ?? new List<FuContextMenuItem>();
            Type = isSeparator ? FuContextMenuItemType.Separator : FuContextMenuItemType.Normal;
        }


        /// <summary>
        /// Constructor for an image item (always displayed full width, preserving aspect ratio)
        /// </summary>
        /// <param name="image">The texture to display in the menu</param>
        /// <param name="size">Image size</param>
        /// <param name="clickAction">Action on image click</param>
        /// <param name="children">The children items for the item</param>
        public FuContextMenuItem(Texture2D image, FuElementSize size, Action clickAction = null, List<FuContextMenuItem> children = null)
        {
            Label = null;
            Image = image;
            Shortcut = null;
            Children = new List<FuContextMenuItem>();
            IsSeparator = false;
            Size = size;
            ClickAction = clickAction;
            Children = children ?? new List<FuContextMenuItem>();
            Type = FuContextMenuItemType.Image;
        }
    }

    /// <summary>
    /// Defines the kind of item displayed in a context menu
    /// </summary>
    public enum FuContextMenuItemType
    {
        Normal,
        Separator,
        Image
    }
}
