using Fu;
using Fu.Framework;
using ImGuiNET;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FuguiDemo
{
    /// <summary>
    /// Represents the Tree Window type.
    /// </summary>
    public class TreeWindow : FuWindowBehaviour
    {
        #region State
        private const float TreeActionZoneWidth = 34f;

        private List<treeTestItem> _treeItems;
        private FuTree<treeTestItem> _tree = null;
        private string _searchText = string.Empty;
        private int _addedItemsCount = 1;
        private readonly float _treeItemHeight = 30f;
        #endregion

        #region Methods
        /// <summary>
        /// Runs the awake workflow.
        /// </summary>
        public void Awake()
        {
            // Generate and save curated tree items for the demo.
            _treeItems = treeTestItem.GetDemoHierarchy();

            // Create a FuTree helper to draw the hierarchical structure.
            _tree = new FuTree<treeTestItem>("modernDemoTree",
                getVisibleItems,
                FuTextStyle.Highlight,
                drawTreeItem,
                (item, availWidth) => new Vector2(Mathf.Max(1f, availWidth - TreeActionZoneWidth * Fugui.CurrentContext.Scale), _treeItemHeight * Fugui.CurrentContext.Scale),
                (item) => { item.IsOpen = 1; },
                (item) => { item.IsOpen = 0; },
                selectItems,
                deselectItems,
                (item) => item.Level,
                (a, b) => a == b,
                getVisibleChildren,
                (item) => item.IsOpen == 1,
                (item) => item.IsSelected == 1,
                _treeItemHeight);

            _tree.UpdateTree(getVisibleRoots());
        }

        /// <summary>
        /// A Method that deselect some tree items.
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
        /// A Method that select some tree items.
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
        /// A Method that draw a tree item.
        /// </summary>
        /// <param name="item">item to draw</param>
        /// <param name="layout">layout to draw item with</param>
        private void drawTreeItem(treeTestItem item, FuLayout layout)
        {
            float scale = Fugui.CurrentContext.Scale;
            float rowHeight = _treeItemHeight * scale;
            Vector2 pos = ImGui.GetCursorScreenPos();
            float width = Mathf.Max(1f, ImGui.GetContentRegionAvail().x);
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            bool selected = item.IsSelected == 1;
            Rect rowRect = new Rect(new Vector2(pos.x - 20f * scale, pos.y), new Vector2(width + 20f * scale, rowHeight));
            bool rowHovered = ImGui.IsMouseHoveringRect(rowRect.min, rowRect.max);

            float iconSize = 18f * scale;
            float iconRadius = 5f * scale;
            float iconX = pos.x + 8f * scale;
            float iconY = pos.y + (rowHeight - iconSize) * 0.5f;
            Rect iconRect = new Rect(new Vector2(iconX, iconY), new Vector2(iconSize, iconSize));
            Vector4 accent = getStateColor(item.State, 1f);
            Vector4 iconBg = accent;
            iconBg.w = selected ? 0.24f : rowHovered ? 0.20f : 0.15f;
            drawList.AddRectFilled(iconRect.min, iconRect.max, ImGui.GetColorU32(iconBg), iconRadius, ImDrawFlags.RoundCornersAll);
            Fugui.Push(ImGuiCol.Text, accent);
            layout.EnboxedText(item.Icon, iconRect.position, iconRect.size, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), FuTextWrapping.Clip);
            Fugui.PopColor();

            float actionSize = 22f * scale;
            Rect deleteRect = new Rect(new Vector2(pos.x + width - actionSize - 8f * scale, pos.y + (rowHeight - actionSize) * 0.5f), new Vector2(actionSize, actionSize));
            bool showAction = rowHovered || selected;
            bool deleteHovered = showAction && ImGui.IsMouseHoveringRect(deleteRect.min, deleteRect.max);
            if (showAction)
            {
                if (deleteHovered)
                {
                    Vector4 deleteBg = Fugui.Themes.GetColor(FuColors.BackgroundDanger, 0.22f);
                    drawList.AddRectFilled(deleteRect.min, deleteRect.max, ImGui.GetColorU32(deleteBg), 5f * scale, ImDrawFlags.RoundCornersAll);
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }
                Fugui.Push(ImGuiCol.Text, Fugui.Themes.GetColor(FuColors.TextDanger, deleteHovered ? 1f : 0.50f));
                layout.EnboxedText(Icons.Close, deleteRect.position, deleteRect.size, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), FuTextWrapping.Clip);
                Fugui.PopColor();
            }
            layout.SetToolTip("deleteTreeItem" + item.Id, "Delete " + item.DisplayName, deleteHovered);

            if (deleteHovered && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                showDeleteConfirmation(item);
            }

            string badgeText = item.StatusLabel;
            Vector2 badgePadding = new Vector2(7f, 2f) * scale;
            Vector2 badgeSize = ImGui.CalcTextSize(badgeText) + badgePadding * 2f;
            Rect badgeRect = new Rect(new Vector2(deleteRect.xMin - badgeSize.x - 6f * scale, pos.y + (rowHeight - badgeSize.y) * 0.5f), badgeSize);
            Vector4 badgeBg = accent;
            badgeBg.w = selected ? 0.24f : 0.17f;
            drawList.AddRectFilled(badgeRect.min, badgeRect.max, ImGui.GetColorU32(badgeBg), badgeRect.height * 0.5f, ImDrawFlags.RoundCornersAll);
            Fugui.Push(ImGuiCol.Text, accent);
            layout.EnboxedText(badgeText, badgeRect.position, badgeRect.size, badgePadding, Vector2.zero, new Vector2(0.5f, 0.5f), FuTextWrapping.Clip);
            Fugui.PopColor();

            float textX = iconRect.xMax + 9f * scale;
            float rightLimit = Mathf.Max(textX + 24f * scale, badgeRect.xMin - 8f * scale);
            Vector2 titlePos = new Vector2(textX, pos.y + 3f * scale);
            Vector2 titleSize = new Vector2(rightLimit - textX, 15f * scale);
            Fugui.Push(ImGuiCol.Text, Fugui.Themes.GetColor(FuColors.Text, selected ? 1f : 0.95f));
            layout.EnboxedText(item.DisplayName, titlePos, titleSize, Vector2.zero, Vector2.zero, new Vector2(0f, 0f), FuTextWrapping.Clip);
            Fugui.PopColor();

            string details = item.Kind + " / " + item.Detail;
            if (item.DescendantCount > 0)
            {
                details += " / " + item.DescendantCount + " items";
            }
            Fugui.Push(ImGuiCol.Text, Fugui.Themes.GetColor(FuColors.TextDisabled, selected ? 0.95f : 0.78f));
            layout.EnboxedText(details, new Vector2(textX, pos.y + 16f * scale), new Vector2(rightLimit - textX, 13f * scale), Vector2.zero, Vector2.zero, new Vector2(0f, 0f), FuTextWrapping.Clip);
            Fugui.PopColor();

            ImGui.Dummy(new Vector2(width, rowHeight));
        }

        /// <summary>
        /// Delete an element of the tree.
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
            _tree.UpdateTree(getVisibleRoots());
        }

        /// <summary>
        /// Called each frame to draw the UI of this window.
        /// </summary>
        /// <param name="window"> the window that is drawing this UI</param>
        /// <param name="layout">the layout that is drawing this UI</param>
        public override void OnUI(FuWindow window, FuLayout layout)
        {
            drawToolbar(layout);
            layout.Dummy(0f, 4f);

            // Create a bordered panel to draw the tree on it.
            using (FuPanel panel = new FuPanel("treePanel", false, 0, 0, FuPanelFlags.DrawBorders))
            {
                Fugui.Push(ImGuiStyleVar.ItemSpacing, Vector2.zero);
                _tree.DrawTree();
                Fugui.PopStyle();
            }
        }

        /// <summary>
        /// Draws the tree toolbar.
        /// </summary>
        /// <param name="layout">The layout.</param>
        private void drawToolbar(FuLayout layout)
        {
            float scale = Fugui.CurrentContext.Scale;
            float headerHeight = 78f * scale;
            float padding = 10f * scale;
            Rect headerRect = layout.Surface("treeHeaderSurface", new FuElementSize(-1f, headerHeight / scale), FuColors.Highlight, FuSurfaceFlags.Default, 0.68f, 0.58f, 0.80f, 8f);
            Vector2 headerPos = headerRect.position;
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            Rect iconRect = new Rect(headerPos + new Vector2(padding + 2f * scale, 8f * scale), new Vector2(22f * scale, 22f * scale));
            Vector4 iconBg = Fugui.Themes.GetColor(FuColors.Highlight, 0.20f);
            drawList.AddRectFilled(iconRect.min, iconRect.max, ImGui.GetColorU32(iconBg), 6f * scale, ImDrawFlags.RoundCornersAll);
            Fugui.Push(ImGuiCol.Text, Fugui.Themes.GetColor(FuColors.HighlightText, 0.95f));
            layout.EnboxedText(Icons.TreeList_solid, iconRect.position, iconRect.size, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), FuTextWrapping.Clip);
            Fugui.PopColor();

            string summary = getVisibleItems().Count + " matching / " + treeTestItem.getAll(_treeItems).Count + " total / " + getSelectedCount() + " selected";
            Vector2 titlePos = headerPos + new Vector2(padding + 31f * scale, 8f * scale);
            float titleNaturalWidth = ImGui.CalcTextSize("Tree Explorer").x;
            Vector2 summaryPadding = new Vector2(8f, 2f) * scale;
            Vector2 summaryNaturalSize = ImGui.CalcTextSize(summary) + summaryPadding * 2f;
            Vector2 summarySize = summaryNaturalSize;
            float summaryMinX = titlePos.x + titleNaturalWidth + 18f * scale;
            float summaryAvailableWidth = Mathf.Max(0f, headerRect.xMax - padding - summaryMinX);
            summarySize.x = Mathf.Min(summaryNaturalSize.x, summaryAvailableWidth);
            Rect summaryRect = new Rect(new Vector2(headerRect.xMax - padding - summarySize.x, headerPos.y + 9f * scale), summarySize);
            float titleRight = Mathf.Max(titlePos.x, summaryRect.xMin - 10f * scale);
            layout.EnboxedText("Tree Explorer", titlePos, new Vector2(titleRight - titlePos.x, 20f * scale), Vector2.zero, Vector2.zero, new Vector2(0f, 0.5f), FuTextWrapping.Clip);

            Vector4 summaryBg = Fugui.Themes.GetColor(FuColors.ChildBg, 0.58f);
            if (summarySize.x > 1f && summarySize.y > 1f)
            {
                drawList.AddRectFilled(summaryRect.min, summaryRect.max, ImGui.GetColorU32(summaryBg), summaryRect.height * 0.5f, ImDrawFlags.RoundCornersAll);
                Fugui.Push(ImGuiCol.Text, Fugui.Themes.GetColor(FuColors.TextDisabled, 0.92f));
                layout.EnboxedText(summary, summaryRect.position, summaryRect.size, summaryPadding, Vector2.zero, new Vector2(0.5f, 0.5f), FuTextWrapping.Clip);
                Fugui.PopColor();
            }

            float controlHeight = ImGui.GetFrameHeight();
            float buttonSize = Mathf.Max(controlHeight, 32f * scale);
            float gap = 7f * scale;
            float controlsWidth = buttonSize * 4f + gap * 3f;
            float controlY = headerPos.y + 43f * scale;
            float controlX = headerRect.xMax - padding - controlsWidth;
            float searchX = headerPos.x + padding;
            float searchWidth = Mathf.Max(48f * scale, controlX - gap - searchX);

            ImGui.SetCursorScreenPos(new Vector2(searchX, controlY));
            bool searchUpdated = layout.SearchBox("treeSearch", ref _searchText, "Search nodes, assets or states...", searchWidth / scale);

            Vector2 buttonPos = new Vector2(controlX, controlY);

            if (drawHeaderIconButton(layout, "treeAdd", Icons.Plus_solid, new Rect(buttonPos, new Vector2(buttonSize, controlHeight)), FuButtonStyle.Highlight, "Add a node", FuColors.HighlightText))
            {
                addItem();
            }

            buttonPos.x += buttonSize + gap;
            if (drawHeaderIconButton(layout, "treeExpand", Icons.ArrowDown_solid, new Rect(buttonPos, new Vector2(buttonSize, controlHeight)), FuButtonStyle.Default, "Expand all"))
            {
                setAllOpen(true);
            }

            buttonPos.x += buttonSize + gap;
            if (drawHeaderIconButton(layout, "treeCollapse", Icons.ArrowRight_solid, new Rect(buttonPos, new Vector2(buttonSize, controlHeight)), FuButtonStyle.Default, "Fold all"))
            {
                setAllOpen(false);
            }

            buttonPos.x += buttonSize + gap;
            if (drawHeaderIconButton(layout, "treeClear", Icons.Close, new Rect(buttonPos, new Vector2(buttonSize, controlHeight)), FuButtonStyle.Default, "Clear selection"))
            {
                _tree.DeselectAll();
            }

            if (searchUpdated)
            {
                applySearchExpansion();
                _tree.UpdateTree(getVisibleRoots());
            }

            ImGui.SetCursorScreenPos(headerPos + new Vector2(0f, headerHeight + 8f * scale));
        }

        /// <summary>
        /// Draws a compact icon-only button for the tree header.
        /// </summary>
        /// <param name="layout">The layout.</param>
        /// <param name="id">The button id.</param>
        /// <param name="icon">The icon text.</param>
        /// <param name="rect">The button rect.</param>
        /// <param name="style">The button style.</param>
        /// <param name="tooltip">The tooltip.</param>
        /// <returns>Whether the button was clicked.</returns>
        private bool drawHeaderIconButton(FuLayout layout, string id, string icon, Rect rect, FuButtonStyle style, string tooltip, FuColors textColorName = FuColors.Text)
        {
            float scale = Fugui.CurrentContext.Scale;
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            ImGui.SetCursorScreenPos(rect.position);
            ImGui.InvisibleButton("##" + id, rect.size, ImGuiButtonFlags.MouseButtonLeft);
            bool hovered = ImGui.IsItemHovered();
            bool active = ImGui.IsItemActive();
            bool clicked = ImGui.IsItemClicked(ImGuiMouseButton.Left);

            Vector4 bg = active ? style.ButtonActive : hovered ? style.ButtonHovered : style.Button;
            Vector4 border = Fugui.Themes.GetColor(FuColors.Border, hovered ? 0.72f : 0.42f);
            float rounding = Mathf.Min(5f * scale, rect.height * 0.28f);
            drawList.AddRectFilled(rect.min, rect.max, ImGui.GetColorU32(bg), rounding, ImDrawFlags.RoundCornersAll);
            drawList.AddRect(rect.min, rect.max, ImGui.GetColorU32(border), rounding, ImDrawFlags.RoundCornersAll, Mathf.Max(1f, scale));

            Vector4 textColor = Fugui.Themes.GetColor(textColorName, active ? 0.92f : 1f);
            if (hovered)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                textColor.w = Mathf.Max(textColor.w, 0.96f);
            }

            string displayIcon = Fugui.GetUntagedText(icon);
            Vector2 iconSize = ImGui.CalcTextSize(displayIcon);
            Vector2 iconPos = rect.position + (rect.size - iconSize) * 0.5f;
            drawList.AddText(iconPos, ImGui.GetColorU32(textColor), displayIcon);
            layout.SetToolTip(id + "Tip", tooltip, hovered);
            return clicked;
        }

        /// <summary>
        /// Adds a new demo item.
        /// </summary>
        private void addItem()
        {
            treeTestItem parent = treeTestItem.getAll(_treeItems).FirstOrDefault(item => item.IsSelected == 1 && item.ChildCount > 0);
            if (parent == null)
            {
                parent = _treeItems.FirstOrDefault();
            }

            treeTestItem item = new treeTestItem("Generated Node " + _addedItemsCount, 0, null, Icons.Plus_solid, "Created at runtime", "Generated", StateType.Info);
            _addedItemsCount++;

            if (parent != null)
            {
                parent.IsOpen = 1;
                parent.AddChild(item);
            }
            else
            {
                _treeItems.Add(item);
            }

            _tree.UpdateTree(getVisibleRoots());
        }

        /// <summary>
        /// Opens or closes the whole hierarchy.
        /// </summary>
        /// <param name="open">Whether to open all items.</param>
        private void setAllOpen(bool open)
        {
            foreach (treeTestItem item in treeTestItem.getAll(_treeItems))
            {
                item.IsOpen = open ? (byte)1 : (byte)0;
            }
            _tree.UpdateTree(getVisibleRoots());
        }

        /// <summary>
        /// Gets the visible root items.
        /// </summary>
        /// <returns>The visible root items.</returns>
        private IEnumerable<treeTestItem> getVisibleRoots()
        {
            if (string.IsNullOrWhiteSpace(_searchText))
            {
                return _treeItems;
            }

            return _treeItems.Where(item => item.ContainsSearch(_searchText));
        }

        /// <summary>
        /// Gets the visible children for one item.
        /// </summary>
        /// <param name="item">The parent item.</param>
        /// <returns>The visible children.</returns>
        private IEnumerable<treeTestItem> getVisibleChildren(treeTestItem item)
        {
            if (item.Children == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(_searchText))
            {
                return item.Children;
            }

            return item.Children.Where(child => child.ContainsSearch(_searchText));
        }

        /// <summary>
        /// Gets every item currently visible to the tree model.
        /// </summary>
        /// <returns>The visible items.</returns>
        private List<treeTestItem> getVisibleItems()
        {
            List<treeTestItem> items = new List<treeTestItem>();
            foreach (treeTestItem item in getVisibleRoots())
            {
                addVisibleItem(item, items);
            }
            return items;
        }

        /// <summary>
        /// Adds a visible item and descendants to a list.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="items">The target list.</param>
        private void addVisibleItem(treeTestItem item, List<treeTestItem> items)
        {
            items.Add(item);
            IEnumerable<treeTestItem> children = getVisibleChildren(item);
            if (children == null)
            {
                return;
            }

            foreach (treeTestItem child in children)
            {
                addVisibleItem(child, items);
            }
        }

        /// <summary>
        /// Opens branches that contain search results.
        /// </summary>
        private void applySearchExpansion()
        {
            if (string.IsNullOrWhiteSpace(_searchText))
            {
                return;
            }

            foreach (treeTestItem item in _treeItems)
            {
                applySearchExpansion(item);
            }
        }

        /// <summary>
        /// Opens branches that contain search results.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>Whether a match was found.</returns>
        private bool applySearchExpansion(treeTestItem item)
        {
            bool childMatch = false;
            if (item.Children != null)
            {
                foreach (treeTestItem child in item.Children)
                {
                    childMatch |= applySearchExpansion(child);
                }
            }

            if (childMatch)
            {
                item.IsOpen = 1;
            }

            return childMatch || item.MatchesSearch(_searchText);
        }

        /// <summary>
        /// Gets selected item count.
        /// </summary>
        /// <returns>The selected item count.</returns>
        private int getSelectedCount()
        {
            return treeTestItem.getAll(_treeItems).Count(item => item.IsSelected == 1);
        }

        /// <summary>
        /// Shows the delete confirmation modal.
        /// </summary>
        /// <param name="item">The item to delete.</param>
        private void showDeleteConfirmation(treeTestItem item)
        {
            Fugui.ShowDanger("Remove tree element",
                "Are you sure you want to remove this tree element?\n - " + item.DisplayName,
                FuModalSize.Medium,
                new FuModalButton("Yes", () => { deleteItem(item); }, FuButtonStyle.Danger),
                new FuModalButton("No", null, FuButtonStyle.Default));
        }

        /// <summary>
        /// Gets a theme color for a state.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="alpha">The alpha multiplier.</param>
        /// <returns>The color.</returns>
        private Vector4 getStateColor(StateType state, float alpha)
        {
            switch (state)
            {
                case StateType.Success:
                    return Fugui.Themes.GetColor(FuColors.TextSuccess, alpha);
                case StateType.Warning:
                    return Fugui.Themes.GetColor(FuColors.TextWarning, alpha);
                case StateType.Danger:
                    return Fugui.Themes.GetColor(FuColors.TextDanger, alpha);
                default:
                    return Fugui.Themes.GetColor(FuColors.TextInfo, alpha);
            }
        }
        #endregion
    }
}
