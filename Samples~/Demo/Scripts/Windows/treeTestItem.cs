using Fu.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FuguiDemo
{
    /// <summary>
    /// Represents the tree Test Item type.
    /// </summary>
    internal class treeTestItem
    {
        #region State
        private static int _nextId = 0;

        public string Id;
        public string DisplayName;
        public string Detail;
        public string Kind;
        public string Icon;
        public StateType State;
        public byte IsOpen = 0;
        public byte IsSelected = 0;
        public int Level = 0;
        public treeTestItem Parent;
        public List<treeTestItem> Children;
        #endregion

        #region Properties
        public int ChildCount => Children == null ? 0 : Children.Count;

        public int DescendantCount
        {
            get
            {
                if (Children == null)
                {
                    return 0;
                }

                int count = Children.Count;
                foreach (treeTestItem child in Children)
                {
                    count += child.DescendantCount;
                }
                return count;
            }
        }

        public string StatusLabel
        {
            get
            {
                switch (State)
                {
                    case StateType.Success:
                        return "Ready";
                    case StateType.Warning:
                        return "Review";
                    case StateType.Danger:
                        return "Issue";
                    default:
                        return "Info";
                }
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the tree Test Item class.
        /// </summary>
        /// <param name="text">The text value.</param>
        /// <param name="level">The level value.</param>
        /// <param name="children">The children value.</param>
        /// <param name="icon">The icon value.</param>
        /// <param name="detail">The detail value.</param>
        /// <param name="kind">The kind value.</param>
        /// <param name="state">The state value.</param>
        public treeTestItem(string text, int level, List<treeTestItem> children = null, string icon = null, string detail = null, string kind = null, StateType state = StateType.Info)
        {
            Id = (++_nextId).ToString();
            Level = level;
            DisplayName = text;
            Detail = string.IsNullOrEmpty(detail) ? "Tree item" : detail;
            Kind = string.IsNullOrEmpty(kind) ? "Node" : kind;
            Icon = string.IsNullOrEmpty(icon) ? Icons.Box : icon;
            State = state;
            Children = children;

            if (Children != null)
            {
                foreach (treeTestItem child in Children)
                {
                    child.Parent = this;
                    child.Level = Level + 1;
                    child.UpdateChildLevels();
                }
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Runs the add child workflow.
        /// </summary>
        /// <param name="child">The child value.</param>
        public treeTestItem AddChild(treeTestItem child)
        {
            if (Children == null)
            {
                Children = new List<treeTestItem>();
            }

            child.Parent = this;
            child.Level = Level + 1;
            child.UpdateChildLevels();
            Children.Add(child);
            return child;
        }

        /// <summary>
        /// Returns whether this item or one of its children matches search text.
        /// </summary>
        /// <param name="search">Search text.</param>
        /// <returns>Whether the item matches.</returns>
        public bool ContainsSearch(string search)
        {
            if (MatchesSearch(search))
            {
                return true;
            }

            return Children != null && Children.Any(child => child.ContainsSearch(search));
        }

        /// <summary>
        /// Returns whether this item matches search text.
        /// </summary>
        /// <param name="search">Search text.</param>
        /// <returns>Whether the item matches.</returns>
        public bool MatchesSearch(string search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return true;
            }

            return DisplayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                || Detail.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                || Kind.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                || StatusLabel.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Gets a curated hierarchy for the modern tree demo.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public static List<treeTestItem> GetDemoHierarchy()
        {
            _nextId = 0;

            treeTestItem scene = new treeTestItem("Fugui Demo Scene", 0, new List<treeTestItem>(), Icons.TreeList_duotone, "Runtime workspace", "Scene", StateType.Success) { IsOpen = 1 };
            treeTestItem runtime = scene.AddChild(new treeTestItem("Runtime Windows", 1, new List<treeTestItem>(), Icons.Screen_solid, "Docked UI surfaces", "Group", StateType.Info) { IsOpen = 1 });
            runtime.AddChild(new treeTestItem("Inspector Window", 2, null, Icons.Settings_solid, "Camera and transform controls", "Window", StateType.Success));
            runtime.AddChild(new treeTestItem("Widgets Gallery", 2, null, Icons.Cube, "Buttons, inputs and charts", "Window", StateType.Success));
            runtime.AddChild(new treeTestItem("Nodal Editor", 2, null, Icons.LinkOrigin, "Graph sample with typed ports", "Window", StateType.Warning));

            treeTestItem assets = scene.AddChild(new treeTestItem("Scene Assets", 1, new List<treeTestItem>(), Icons.FolderOpen, "Materials and resources", "Folder", StateType.Info) { IsOpen = 1 });
            treeTestItem materials = assets.AddChild(new treeTestItem("Materials", 2, new List<treeTestItem>(), Icons.Materials_solid, "Renderable swatches", "Folder", StateType.Success) { IsOpen = 1 });
            materials.AddChild(new treeTestItem("Fugui Material", 3, null, Icons.Sealant, "Primary panel surface", "Material", StateType.Success));
            materials.AddChild(new treeTestItem("Accent Material", 3, null, Icons.Magic_solid, "Highlight feedback", "Material", StateType.Info));
            materials.AddChild(new treeTestItem("Legacy Red Mat", 3, null, Icons.Warning, "Needs cleanup", "Material", StateType.Warning));

            treeTestItem systems = scene.AddChild(new treeTestItem("Systems", 1, new List<treeTestItem>(), Icons.Database, "Input and rendering services", "Group", StateType.Info) { IsOpen = 1 });
            systems.AddChild(new treeTestItem("Input Router", 2, null, Icons.CursorSelection_light, "Mouse, keyboard and touch state", "Service", StateType.Success));
            systems.AddChild(new treeTestItem("Theme Manager", 2, null, Icons.Light, "DarkSky palette active", "Service", StateType.Success));
            systems.AddChild(new treeTestItem("Font Atlas", 2, null, Icons.Resources, "Icon and text glyph cache", "Service", StateType.Info));

            treeTestItem backlog = new treeTestItem("Implementation Notes", 0, new List<treeTestItem>(), Icons.Note, "Design and UX checklist", "Notes", StateType.Warning) { IsOpen = 1 };
            backlog.AddChild(new treeTestItem("Keyboard Navigation", 1, null, Icons.Native, "Queued for a future pass", "Task", StateType.Warning));
            backlog.AddChild(new treeTestItem("Drag Reordering", 1, null, Icons.SubTask, "Prototype interaction", "Task", StateType.Info));
            backlog.AddChild(new treeTestItem("Delete Confirmation", 1, null, Icons.CheckV, "Modal guard enabled", "Task", StateType.Success));

            return new List<treeTestItem> { scene, backlog };
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
        public static List<treeTestItem> getAll(IEnumerable<treeTestItem> items)
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

        /// <summary>
        /// Applies the current level to every descendant.
        /// </summary>
        private void UpdateChildLevels()
        {
            if (Children == null)
            {
                return;
            }

            foreach (treeTestItem child in Children)
            {
                child.Parent = this;
                child.Level = Level + 1;
                child.UpdateChildLevels();
            }
        }
        #endregion
    }
}
