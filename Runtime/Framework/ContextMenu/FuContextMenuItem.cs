using ImGuiNET;
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
        public Action ClickAction;
        // Children items for the item
        public List<FuContextMenuItem> Children;
        // Optional image displayed in the context menu 
        public Texture2D Image;
        // Optional image size
        public FuElementSize ImageSize;
        //Optional border pixel size
        public int Border;
        // Defines the type of this item (Normal, Separator, or Image)
        public FuContextMenuItemType Type;

        /// <summary>
        /// Constructor for a centered title item (displayed at the top of the context menu)
        /// </summary>
        /// <param name="title">The title text to display</param>
        public FuContextMenuItem(string title)
        {
            Label = title;
            Shortcut = null;
            Enabled = null;
            ClickAction = null;
            Children = new List<FuContextMenuItem>();
            Image = null;
            Border = 0;
            Type = FuContextMenuItemType.Title;
        }

        /// <summary>
        /// Constructor only use for separator
        /// </summary>
        public FuContextMenuItem()
        {
            Label = null;
            Shortcut = null;
            Enabled = null;
            ClickAction = null;
            Children = new List<FuContextMenuItem>();
            Image = null;
            Border = 0;
            Type = FuContextMenuItemType.Separator;
        }

        /// <summary>
        /// Constructor for a standard context menu item
        /// </summary>
        /// <param name="label">The label for the item</param>
        /// <param name="shortcut">The shortcut for the item</param>
        /// <param name="enabled">Whether the item is enabled</param>
        /// <param name="isSeparator">Whether the item is a separator</param>
        /// <param name="clickAction">The action to perform when the item is clicked</param>
        /// <param name="children">The children items for the item</param>
        public FuContextMenuItem(string label, string shortcut, Func<bool> enabled, Action clickAction, Texture2D image = null, FuElementSize imageSize = default, List<FuContextMenuItem> children = null)
        {
            Label = label;
            Image = image;
            ImageSize = imageSize;
            Shortcut = shortcut;
            Enabled = enabled;
            ClickAction = clickAction;
            Children = children ?? new List<FuContextMenuItem>();
            Type = FuContextMenuItemType.Item;
            Border = 0;
        }
    }

    /// <summary>
    /// Defines the kind of item displayed in a context menu
    /// </summary>
    public enum FuContextMenuItemType
    {
        Item,
        Separator,
        Title
    }
}