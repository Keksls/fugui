using Fu;
using Fu.Core;
using Fu.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace FuguiDemo
{
    public class TreeWindow : MonoBehaviour
    {
        private List<treeTestItem> _treeItems;
        private FuTree<treeTestItem> _tree = null;
        float _treeItemHeight = 16f;

        private void Awake()
        {
            // generate and save tree items
            _treeItems = treeTestItem.GetRandomHierarchie(20, 3);

            // create a FuTree (helper to draw some simple hierarchical struct)
            _tree = new FuTree<treeTestItem>("testTree",
                () => treeTestItem.getAll(_treeItems),
                FuTextStyle.Info,
                // how to draw an item
                drawTreeItem,
                // get selectable size
                (item, availWidth) => new Vector2(availWidth - (20f * Fugui.CurrentContext.Scale), _treeItemHeight * Fugui.CurrentContext.Scale),
                // when an item just open
                (item) => { item.IsOpen = 1; },
                // when an item just close
                (item) => { item.IsOpen = 0; },
                // when some item just selected
                selectItems,
                // when some item just deselected
                deselectItems,
                // get the level of an item
                (item) => item.Level,
                // are two items equals ?
                (a, b) => a == b,
                // how to get direct children
                (item) => item.Children,
                // whatever an item is open
                (item) => item.IsOpen == 1,
                // whatever an item is selected
                (item) => item.IsSelected == 1,
                // items heigh
                _treeItemHeight);

            // update the tree to prepare it with all items
            _tree.UpdateTree(_treeItems);

            // register the Tree window definition
            registerUIWindow();
        }

        /// <summary>
        /// A Method that deselect some tree items
        /// </summary>
        /// <param name="items">items to deselect</param>
        private static void deselectItems(IEnumerable<treeTestItem> items)
        {
            foreach (var item in items)
            {
                item.IsSelected = 0;
            }
        }

        /// <summary>
        /// A Method that select some tree items
        /// </summary>
        /// <param name="items">items to select</param>
        private static void selectItems(IEnumerable<treeTestItem> items)
        {
            foreach (var item in items)
            {
                item.IsSelected = 1;
            }
        }

        /// <summary>
        /// A Method that draw a tree item
        /// </summary>
        /// <param name="item">item to draw</param>
        /// <param name="layout">layout to draw item with</param>
        private void drawTreeItem(treeTestItem item, FuLayout layout)
        {
            // exemple of warning icon with tooltip and color
            layout.SetNextElementToolTipWithLabel("Warning");
            layout.SetNextElementToolTipStyles(FuTextStyle.Warning);
            layout.AlignTextToFramePadding();
            layout.Text(Icons.Warning + " ", FuTextStyle.Warning);

            // draw the next element on same line
            layout.SameLine();

            // draw the displa name of the tree item
            layout.Text(item.DisplayName);

            // draw the next element on same line
            layout.SameLine();

            // add some empty space to place cursor on right pos - 20px
            layout.Dummy(layout.GetAvailableWidth() - (20f * Fugui.CurrentContext.Scale));
            layout.SameLine();

            // draw a small delete button
            if (layout.Button(Icons.Delete, FuElementSize.AutoSize, new Vector2(2f, 2f), new Vector2(0.7f, -2.2f), FuButtonStyle.Danger))
            {
                // display a confirmation modal with 'danger' modal layout
                Fugui.ShowDanger("Tree element remove",
                    "Are you sure you want to remove this tree element ?\n - " + item.DisplayName,
                    FuModalSize.Medium,
                    new FuModalButton("Yes", () => { deleteItem(item); }, FuButtonStyle.Danger),
                    new FuModalButton("No", null, FuButtonStyle.Default));
            }
        }

        /// <summary>
        /// Delete an element of the tree
        /// </summary>
        /// <param name="item">item to delete</param>
        private void deleteItem(treeTestItem item)
        {
            if (item.Parent != null)
            {
                item.Parent.Children.Remove(item);
            }
            else
            {
                _treeItems.Remove(item);
            }
            _tree.UpdateTree(_treeItems);
        }

        /// <summary>
        /// Register the Tree window definition
        /// </summary>
        private void registerUIWindow()
        {
            // create a new FuWindowDefinition, Fugui will automaticaly register it so it can instantiate a FuWiindow when needed
            new FuWindowDefinition(FuWindowsNames.Tree, "Tree", (window) =>
            {
                // create a panel to draw tree on it (we don't need to use cliper because FuTree has its own clipping system)
                using (FuPanel panel = new FuPanel("treePanel", false))
                {
                    // draw a button to add random item on runtime
                    if (new FuLayout().Button("add item"))
                    {
                        treeTestItem item = new treeTestItem("added " + System.Guid.NewGuid().ToString(), 0, null);
                        _treeItems.Insert(Random.Range(0, _treeItems.Count), item);
                        _tree.UpdateTree(_treeItems);
                    }

                    // set sacing to none (just a UI choice, you don't have to do this)
                    Fugui.Push(ImGuiNET.ImGuiStyleVar.ItemSpacing, Vector2.zero);
                    // draw the tree
                    _tree.DrawTree();
                    Fugui.PopStyle();
                }
            }, flags: FuWindowFlags.AllowMultipleWindow);
        }
    }

    internal class treeTestItem
    {
        public string DisplayName;
        public byte IsOpen = 0;
        public byte IsSelected = 0;
        public int Level = 0;
        public treeTestItem Parent;
        public List<treeTestItem> Children;

        public treeTestItem(string text, int level, List<treeTestItem> children = null)
        {
            Level = level;
            DisplayName = text;
            Children = children;
        }

        public void AddChild(treeTestItem child)
        {
            child.Parent = this;
            Children.Add(child);
        }

        public static List<treeTestItem> GetRandomHierarchie(int numberOfItemsPerLevel, int numberOfLevels)
        {
            List<treeTestItem> items = new List<treeTestItem>();

            for (int i = 0; i < numberOfItemsPerLevel; i++)
            {
                treeTestItem parent = new treeTestItem("Parent " + i, 0, new List<treeTestItem>());
                parent.Children = GetChildren(parent, numberOfItemsPerLevel, numberOfLevels, 1);
                items.Add(parent);
            }
            return items;
        }

        private static List<treeTestItem> GetChildren(treeTestItem parent, int numberOfItemsPerLevel, int numberOfLevels, int level)
        {
            if (numberOfLevels == level)
            {
                return null;
            }

            List<treeTestItem> children = new List<treeTestItem>();
            for (int i = 0; i < numberOfItemsPerLevel; i++)
            {
                treeTestItem child = new treeTestItem("Child_" + level + "_" + i, level, new List<treeTestItem>());
                child.Parent = parent;
                child.Children = GetChildren(child, numberOfItemsPerLevel, numberOfLevels, level + 1);
                children.Add(child);
            }
            return children;
        }

        public static List<treeTestItem> getAll(List<treeTestItem> items)
        {
            List<treeTestItem> all = new List<treeTestItem>();
            if (items != null)
            {
                foreach (var item in items)
                {
                    getAll(item, all);
                }
            }
            return all;
        }

        private static void getAll(treeTestItem item, List<treeTestItem> items)
        {
            items.Add(item);
            if (item.Children != null)
            {
                foreach (var it in item.Children)
                {
                    getAll(it, items);
                }
            }
        }
    }
}
