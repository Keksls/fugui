using Fu;
using Fu.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace FuguiDemo
{
    /// <summary>
    /// Represents the Tree Window type.
    /// </summary>
    public class TreeWindow : FuWindowBehaviour
    {
        #region State
        private List<treeTestItem> _treeItems;
        private FuTree<treeTestItem> _tree = null;
        float _treeItemHeight = 16f;
        #endregion

        #region Methods
        /// <summary>
        /// Runs the awake workflow.
        /// </summary>
        public void Awake()
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
            layout.SetNextElementToolTipStyles(FuTextStyle.Info);
            layout.AlignTextToFramePadding();
            layout.Text(Icons.Info_duotone + " ");

            // draw the next element on same line
            layout.SameLine();

            // draw the displa name of the tree item
            layout.Text(item.DisplayName);

            // draw the next element on same line
            layout.SameLine();

            // add some empty space to place cursor on right pos - 20px
            layout.Dummy(layout.GetAvailableWidth() - (14f * Fugui.CurrentContext.Scale));
            layout.SameLine();

            // draw a small delete button as a clickable text
            Fugui.PushFont(10, FontType.Regular);
            layout.SetNextElementToolTipWithLabel("Delete this element");
            if (layout.ClickableText(Icons.Close, FuTextStyle.Danger))
            {
                // display a confirmation modal with 'danger' modal layout
                Fugui.ShowDanger("Tree element remove",
                    "Are you sure you want to remove this tree element ?\n - " + item.DisplayName,
                    FuModalSize.Medium,
                    new FuModalButton("Yes", () => { deleteItem(item); }, FuButtonStyle.Danger),
                    new FuModalButton("No", null, FuButtonStyle.Default));
            }
            Fugui.PopFont();
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
        /// Called each frame to draw the UI of this window
        /// </summary>
        /// <param name="window"> the window that is drawing this UI</param>
        public override void OnUI(FuWindow window, FuLayout layout)
        {
            // create a panel to draw tree on it (we don't need to use cliper because FuTree has its own clipping system)
            using (FuPanel panel = new FuPanel("treePanel"))
            {
                // draw a button to add random item on runtime
                if (layout.Button("add item"))
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
        }
        #endregion
    }
}