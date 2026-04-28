using System.Collections.Generic;

namespace FuguiDemo
{
        /// <summary>
        /// Represents the tree Test Item type.
        /// </summary>
        internal class treeTestItem
        {
            #region State
            public string DisplayName;
            public byte IsOpen = 0;
            public byte IsSelected = 0;
            public int Level = 0;
            public treeTestItem Parent;
            public List<treeTestItem> Children;
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the tree Test Item class.
            /// </summary>
            /// <param name="text">The text value.</param>
            /// <param name="level">The level value.</param>
            /// <param name="children">The children value.</param>
            public treeTestItem(string text, int level, List<treeTestItem> children = null)
            {
                Level = level;
                DisplayName = text;
                Children = children;
            }
            #endregion

            #region Methods
            /// <summary>
            /// Runs the add child workflow.
            /// </summary>
            /// <param name="child">The child value.</param>
            public void AddChild(treeTestItem child)
            {
                child.Parent = this;
                Children.Add(child);
            }

            /// <summary>
            /// Gets the random hierarchie.
            /// </summary>
            /// <param name="numberOfItemsPerLevel">The number Of Items Per Level value.</param>
            /// <param name="numberOfLevels">The number Of Levels value.</param>
            /// <returns>The result of the operation.</returns>
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

            /// <summary>
            /// Gets the children.
            /// </summary>
            /// <param name="parent">The parent value.</param>
            /// <param name="numberOfItemsPerLevel">The number Of Items Per Level value.</param>
            /// <param name="numberOfLevels">The number Of Levels value.</param>
            /// <param name="level">The level value.</param>
            /// <returns>The result of the operation.</returns>
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

            /// <summary>
            /// Returns the get all result.
            /// </summary>
            /// <param name="items">The items value.</param>
            /// <returns>The result of the operation.</returns>
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

            /// <summary>
            /// Runs the get all workflow.
            /// </summary>
            /// <param name="item">The item value.</param>
            /// <param name="items">The items value.</param>
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
            #endregion
        }
}