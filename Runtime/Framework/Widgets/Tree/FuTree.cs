using Fu;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fu.Framework
{
    /// <summary>
    /// Represents a custom tree UI framework for Unity using DearImgui.
    /// </summary>
    public class FuTree<T>
    {
        #region State
        /// <summary>Delegate for displaying an element in the tree.</summary>
        private Action<T, FuLayout> _display;
        /// <summary>Delegate for checking if an element is open.</summary>
        private Func<T, bool> _isOpen;
        /// <summary>Delegate for checking if an element is selected.</summary>
        private Func<T, bool> _isSelected;
        /// <summary>Delegate for getting the selectable size of an element.</summary>
        private Func<T, float, Vector2> _getSelectableSize;
        /// <summary>Delegate for handling an element being opened.</summary>
        private Action<T> _onOpen;
        /// <summary>Delegate for handling an element being closed.</summary>
        private Action<T> _onClose;
        /// <summary>Delegate for handling selection of one or multiple elements.</summary>
        private Action<List<T>> _onSelect;
        /// <summary>Delegate for handling deselection of one or multiple elements.</summary>
        private Action<List<T>> _onDeSelect;
        /// <summary>Delegate for getting the level of an element in the tree.</summary>
        private Func<T, int> _getLevel;
        /// <summary>Delegate for checking if two elements are equal.</summary>
        private Func<T, T, bool> _equals;
        /// <summary>Delegate for getting the direct children of an element.</summary>
        private Func<T, IEnumerable<T>> _getDirectChildren;
        /// <summary>Delegate for getting all the elements in the tree.</summary>
        private Func<IEnumerable<T>> _getAll;
        /// <summary>The unique identifier of the tree.</summary>
        private string _id;
        /// <summary>The height of each tree item.</summary>
        private float _itemHeight;
        /// <summary>The list of elements in the tree.</summary>
        private List<T> _elements = new List<T>();
        /// <summary>Action to be performed after rendering the tree.</summary>
        private Action _onPostRenderAction = null;
        /// <summary>The text style of the caret.</summary>
        private FuTextStyle _carretStyle;
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor for FuTree.
        /// </summary>
        ///<param name="id">The unique identifier of the tree.</param>
        ///<param name="getAll">Delegate for getting all the elements in the tree.</param>
        ///<param name="carretStyle">The text style of the caret.</param>
        ///<param name="display">Delegate for displaying an element in the tree.</param>
        ///<param name="getSelectableSize">Delegate for getting the selectable size of an element.</param>
        ///<param name="onOpen">Delegate for handling an element being opened.</param>
        ///<param name="onClose">Delegate for handling an element being closed.</param>
        ///<param name="onSelect">Delegate for handling selection of one or multiple elements.</param>
        ///<param name="onDeSelect">Delegate for handling deselection of one or multiple elements.</param>
        ///<param name="getLevel">Delegate for getting the level of an element in the tree.</param>
        ///<param name="equals">Delegate for checking if two elements are equal.</param>
        ///<param name="getDirectChildren">Delegate for getting the direct children of an element.</param>
        ///<param name="isOpen">Delegate for checking if an element is open.</param>
        ///<param name="isSelected">Delegate for checking if an element is selected.</param>
        ///<param name="itemHeight">The height of each tree item.</param>
        public FuTree(string id, Func<IEnumerable<T>> getAll, FuTextStyle carretStyle, Action<T, FuLayout> display, Func<T, float, Vector2> getSelectableSize, Action<T> onOpen, Action<T> onClose, Action<IEnumerable<T>> onSelect, Action<IEnumerable<T>> onDeSelect, Func<T, int> getLevel, Func<T, T, bool> equals, Func<T, IEnumerable<T>> getDirectChildren, Func<T, bool> isOpen, Func<T, bool> isSelected, float itemHeight)
        {
            _id = id;
            _getAll = getAll;
            _carretStyle = carretStyle;
            _onSelect = onSelect;
            _onDeSelect = onDeSelect;
            _onOpen = onOpen;
            _onClose = onClose;
            _getLevel = getLevel;
            _equals = equals;
            _getDirectChildren = getDirectChildren;
            _display = display;
            _getSelectableSize = getSelectableSize;
            _isSelected = isSelected;
            _isOpen = isOpen;
            _itemHeight = itemHeight;
            UpdateTree(_getAll());
        }
        #endregion

        #region Methods
        /// <summary>
        /// Tries to open an element in the tree.
        /// </summary>
        ///<param name="element">The element to be opened.</param>
        ///<param name="recursively">Flag to indicate whether to open all children recursively or not.</param>
        public void TryOpen(T element, bool recursively)
        {
            int index = getIndex(element);
            if (index == -1)
            {
                Debug.LogError("Tree fail to get element " + element.ToString());
                return;
            }

            var children = _getDirectChildren(element);
            if (children == null)
            {
                return;
            }

            // get all open children under these children
            getOpenChildren(children, out List<T> allChildren, recursively, true);

            _onPostRenderAction = () =>
            {
                _elements.InsertRange(index + 1, allChildren);
                _onPostRenderAction = null;
                _onOpen(element);
            };
        }

        /// <summary>
        /// Tries to close an element in the tree.
        /// </summary>
        ///<param name="element">The element to be closed.</param>
        ///<param name="recursively">Flag to indicate whether to close all children recursively or not.</param>
        public void TryClose(T element, bool recursively)
        {
            int elementIndex = getIndex(element);
            if (elementIndex == -1)
            {
                Debug.LogError("Tree fail to get element " + element.ToString());
                return;
            }

            int index = elementIndex + 1;
            int level = _getLevel(element);
            int maxIndex = index;
            for (int i = index; i < _elements.Count; i++)
            {
                // we reach the same level that we want to close
                if (_getLevel(_elements[i]) == level)
                {
                    maxIndex = i;
                    break;
                }
            }

            // we reach the end before finding an element at the same level, so it's the end of the tree
            if (maxIndex == index)
            {
                maxIndex = _elements.Count;
            }

            int count = maxIndex - index;

            // recursively close
            if (recursively)
            {
                for (int i = index; i < index + count; i++)
                {
                    _onClose(_elements[i]);
                }
            }

            // register the post render action
            _onPostRenderAction = () =>
            {
                _elements.RemoveRange(index, count);
                _onPostRenderAction = null;
                _onClose(element);
            };
        }

        /// <summary>
        /// Gets all the open children of a list of elements.
        /// </summary>
        ///<param name="elements">The list of elements to get the open children for.</param>
        ///<param name="fullList">The list of all open children.</param>
        ///<param name="recursively">Flag to indicate whether to get all children recursively or not.</param>
        ///<param name="autoOpen">Flag to indicate whether to automatically open unopened elements or not.</param>
        private void getOpenChildren(IEnumerable<T> elements, out List<T> fullList, bool recursively, bool autoOpen)
        {
            fullList = new List<T>();
            foreach (T element in elements)
            {
                getOpenChildren(element, fullList, recursively, autoOpen);
            }
        }

        /// <summary>
        /// Gets all the open children of an element.
        /// </summary>
        ///<param name="element">The element to get the open children for.</param>
        ///<param name="elements">The list of all open children.</param>
        ///<param name="recursively">Flag to indicate whether to get all children recursively or not.</param>
        ///<param name="autoOpen">Flag to indicate whether to automatically open unopened elements or not.</param>
        private void getOpenChildren(T element, List<T> elements, bool recursively, bool autoOpen)
        {
            elements.Add(element);
            bool open = _isOpen(element);
            if (recursively || open)
            {
                if (!open && autoOpen)
                {
                    _onOpen(element);
                }

                var children = _getDirectChildren(element);
                if (children != null)
                {
                    foreach (T child in children)
                    {
                        getOpenChildren(child, elements, recursively, autoOpen);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the index of an element in the list of elements.
        /// </summary>
        ///<param name="element">The element to get the index for.</param>
        ///<returns>The index of the element in the list of elements.</returns>
        private int getIndex(T element)
        {
            int index = -1;
            for (int i = 0; i < _elements.Count; i++)
            {
                if (_equals(_elements[i], element))
                {
                    return i;
                }
            }
            return index;
        }

        /// <summary>
        /// Updates the list of elements in the tree with a new set of elements.
        /// </summary>
        ///<param name="elements">The new set of elements to update the tree with.</param>
        public void UpdateTree(IEnumerable<T> elements)
        {
            if (elements != null)
            {
                _elements = new List<T>();
                bindListFromItems(elements);
            }
        }

        /// <summary>
        /// Updates the list of elements in the tree with a single element.
        /// </summary>
        ///<param name="element">The single element to update the tree with.</param>
        public void UpdateTree(T element)
        {
            _elements = new List<T>
            {
                element
            };
            if (_isOpen(element))
            {
                var children = _getDirectChildren(element);
                if (children != null)
                {
                    bindListFromItems(children);
                }
            }
        }

        /// <summary>
        /// Binds the list of elements in the tree with a set of elements.
        /// </summary>
        ///<param name="elements">The set of elements to bind the tree with.</param>
        private void bindListFromItems(IEnumerable<T> elements)
        {
            foreach (T element in elements)
            {
                _elements.Add(element);
                if (_isOpen(element))
                {
                    var children = _getDirectChildren(element);
                    if (children != null)
                    {
                        bindListFromItems(children);
                    }
                }
            }
        }

        /// <summary>
        /// Selects an element in the tree.
        /// </summary>
        ///<param name="element">The element to be selected.</param>
        ///<param name="recursive">Flag to indicate whether to select all children recursively or not.</param>
        ///<param name="startEnd">Flag to indicate whether to select all children between the start and end of the current selection.</param>
        ///<param name="additive">Flag to indicate whether to add the selection to the current selection or not.</param>
        ///<param name="currentSelection">The current selection to add the new selection to, if any.</param>
        public void Select(T element, bool recursive, bool startEnd, bool additive, IEnumerable<T> currentSelection = null)
        {
            setSelected(element, true, recursive, startEnd, additive, currentSelection);
        }

        /// <summary>
        /// Deselects an element in the tree.
        /// </summary>
        ///<param name="element">The element to be deselected.</param>
        ///<param name="recursive">Flag to indicate whether to deselect all children recursively or not.</param>
        ///<param name="startEnd">Flag to indicate whether to deselect all children between the start and end of the current selection.</param>
        ///<param name="additive">Flag to indicate whether to subtract the selection from the current selection or not.</param>
        ///<param name="currentSelection">The current selection to remove the selection from, if any.</param>
        public void Deselect(T element, bool recursive, bool startEnd, bool additive, IEnumerable<T> currentSelection = null)
        {
            setSelected(element, false, recursive, startEnd, additive, currentSelection);
        }

        /// <summary>
        /// Selects all elements in the tree.
        /// </summary>
        public void SelectAll()
        {
            _onSelect(_getAll().Where(element => !_isSelected(element)).ToList());
        }

        /// <summary>
        /// Deselects all elements in the tree.
        /// </summary>
        public void DeselectAll()
        {
            _onDeSelect(_getAll().Where(element => _isSelected(element)).ToList());
        }

        /// <summary>
        /// Sets the selected status of an element in the tree.
        /// </summary>
        ///<param name="element">The element to set the selected status for.</param>
        ///<param name="selected">Flag to indicate whether to select or deselect the element.</param>
        ///<param name="recursive">Flag to indicate whether to apply the selection recursively to all children.</param>
        ///<param name="startEnd">Flag to indicate whether to select or deselect all children between the start and end of the current selection.</param>
        ///<param name="additive">Flag to indicate whether to add or subtract the selection from the current selection.</param>
        ///<param name="currentSelection">The current selection to add or remove the selection from, if any.</param>
        private void setSelected(T element, bool selected, bool recursive, bool startEnd, bool additive, IEnumerable<T> currentSelection = null)
        {
            List<T> selection = new List<T>();
            var all = _getAll().ToList();
            int elementIndex = all.IndexOf(element);
            bool alreadySelected = _isSelected(element);

            // if not additive, unselect all
            if (selected && !additive && !startEnd)
            {
                if (currentSelection == null)
                {
                    currentSelection = all.Where(x => _isSelected(x));
                }
                // Unselect all elements
                _onDeSelect(currentSelection.ToList());
            }

            // Start-end with shift
            if (startEnd)
            {
                // Find the first and last selected elements
                int firstSelected = all.FindIndex(x => _isSelected(x));
                int lastSelected = all.FindLastIndex(x => _isSelected(x));

                // If no element is selected, select the single element
                if (firstSelected == -1)
                {
                    firstSelected = elementIndex;
                    lastSelected = elementIndex;
                }
                // If the first selected element is after the current element, update first selected to current element
                else if (elementIndex < firstSelected)
                {
                    firstSelected = elementIndex;
                }
                // If the last selected element is before the current element, update last selected to current element
                else if (elementIndex > lastSelected)
                {
                    lastSelected = elementIndex;
                }

                // Add all elements between first selected and last selected to the selection list
                for (int i = firstSelected; i <= lastSelected; i++)
                {
                    selection.Add(all[i]);
                }
            }
            // Recursive
            else
            {
                // Select the current element and its children if recursive is true
                if (recursive)
                {
                    selection = new List<T>();
                    getOpenChildren(element, selection, true, false);
                }
                // Select only the current element if recursive is false
                else
                {
                    selection = new List<T> { element };
                }
            }

            // do selection or deselection
            if (selected)
            {
                _onSelect(selection);
            }
            else
            {
                _onDeSelect(selection);
            }
        }

        /// <summary>
        ///Draws the tree using DearImgui.
        /// </summary>
        public void DrawTree()
        {
            // Get the scale and height.
            float height = Fugui.CurrentContext.Scale * _itemHeight;
            float indent = 18f * Fugui.CurrentContext.Scale;
            // Get the draw list and color.
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            // Using a layout object and a list clipper, loop through the elements and draw them.
            using (FuLayout layout = new FuLayout())
            {
                int count = _elements.Count;
                Fugui.ListClipperBegin(Math.Max(1, count), height);
                while (Fugui.ListClipperStep())
                {
                    int start = Fugui.ListClipperDisplayStart();
                    int end = Fugui.ListClipperDisplayEnd();
                    for (int i = start; i < end; i++)
                    {
                        int level = _getLevel(_elements[i]);
                        Vector2 startPos = ImGui.GetCursorScreenPos();
                        // Get the item rect.
                        Rect itemRect = new Rect(startPos, new Vector2(ImGui.GetContentRegionAvail().x, height));
                        if (level > 0)
                        {
                            drawTreeGuides(drawList, startPos, itemRect, indent, level);
                            // Indent the cursor.
                            ImGui.Indent(indent * level);
                            // Draw the element.
                            drawElement(_elements[i], layout, drawList, itemRect);
                            // Cancel the indent.
                            ImGui.Indent(-indent * level);
                        }
                        else
                        {
                            drawElement(_elements[i], layout, drawList, itemRect);
                        }
                    }
                }
                Fugui.ListClipperEnd();
            }
            // Invoke the post render action if there is any.
            _onPostRenderAction?.Invoke();
        }

        /// <summary>
        /// Draws subtle branch guides for nested tree rows.
        /// </summary>
        ///<param name="drawList">The draw list.</param>
        ///<param name="startPos">The screen position of the row.</param>
        ///<param name="itemRect">The row rect.</param>
        ///<param name="indent">The indentation size.</param>
        ///<param name="level">The tree level.</param>
        private void drawTreeGuides(ImDrawListPtr drawList, Vector2 startPos, Rect itemRect, float indent, int level)
        {
            Vector4 guideColor = Fugui.Themes.GetColor(FuColors.Border);
            guideColor.w *= 0.26f;
            uint guideCol = ImGui.GetColorU32(guideColor);
            float scale = Fugui.CurrentContext.Scale;
            float thickness = Mathf.Max(1f, scale);
            float yMid = itemRect.yMin + itemRect.height * 0.5f;

            for (int j = 0; j < level; j++)
            {
                float x = startPos.x + indent * j + indent * 0.5f;
                drawList.AddLine(new Vector2(x, itemRect.yMin + 4f * scale), new Vector2(x, itemRect.yMax - 4f * scale), guideCol, thickness);
            }

            guideColor.w *= 1.35f;
            guideCol = ImGui.GetColorU32(guideColor);
            float branchX = startPos.x + indent * (level - 1) + indent * 0.5f;
            drawList.AddLine(new Vector2(branchX, yMid), new Vector2(branchX + indent * 0.62f, yMid), guideCol, thickness);
        }

        /// <summary>
        ///Draws an element of the tree.
        /// </summary>
        ///<param name="element">The element to draw.</param>
        ///<param name="layout">The layout object.</param>
        ///<param name="drawList">The draw list.</param>
        ///<param name="itemRect">The rect of the item.</param>
        private void drawElement(T element, FuLayout layout, ImDrawListPtr drawList, Rect itemRect)
        {
            float scale = Fugui.CurrentContext.Scale;
            float height = Fugui.CurrentContext.Scale * _itemHeight;
            Vector2 cursorPos = ImGui.GetCursorScreenPos();
            Vector4 color = _carretStyle.Text;
            uint col = ImGui.GetColorU32(color);
            float carretSize = 7f * scale;
            float carretHitWidth = 20f * scale;
            Vector2 selectableSize = _getSelectableSize(element, ImGui.GetContentRegionAvail().x - carretHitWidth);
            selectableSize.x = Mathf.Clamp(selectableSize.x, 0f, Mathf.Max(0f, itemRect.xMax - cursorPos.x - carretHitWidth));
            selectableSize.y = Mathf.Max(height, selectableSize.y);
            Rect selectableRect = new Rect(cursorPos + new Vector2(carretHitWidth, 0f), selectableSize);
            Rect rowRect = new Rect(
                itemRect.position + new Vector2(1f * scale, 1f * scale),
                new Vector2(Mathf.Max(1f, itemRect.width - 2f * scale), Mathf.Max(1f, itemRect.height - 2f * scale)));
            bool rowHovered = ImGui.IsMouseHoveringRect(selectableRect.min, selectableRect.max);
            bool selected = _isSelected(element);
            float rounding = Mathf.Min(6f * scale, rowRect.height * 0.35f);

            // draw hover / selected rect
            if (rowHovered || selected)
            {
                Vector4 rowColor;
                if (selected)
                {
                    rowColor = Fugui.Themes.GetColor(
                        FuWindow.CurrentDrawingWindow.Mouse.IsDown(0) && rowHovered ? FuColors.HighlightActive : FuColors.Highlight,
                        rowHovered ? 0.28f : 0.20f);
                }
                else
                {
                    rowColor = Fugui.Themes.GetColor(FuColors.HeaderHovered, FuWindow.CurrentDrawingWindow.Mouse.IsDown(0) ? 0.40f : 0.26f);
                }
                drawList.AddRectFilled(rowRect.min, rowRect.max, ImGui.GetColorU32(rowColor), rounding, ImDrawFlags.RoundCornersAll);

                if (selected)
                {
                    Vector4 accent = Fugui.Themes.GetColor(FuColors.Highlight);
                    accent.w = Mathf.Max(accent.w, 0.9f);
                    drawList.AddRectFilled(rowRect.min, new Vector2(rowRect.xMin + 3f * scale, rowRect.yMax), ImGui.GetColorU32(accent), rounding, ImDrawFlags.RoundCornersLeft);

                    Vector4 border = accent;
                    border.w *= rowHovered ? 0.52f : 0.34f;
                    drawList.AddRect(rowRect.min, rowRect.max, ImGui.GetColorU32(border), rounding, ImDrawFlags.RoundCornersAll, Mathf.Max(1f, scale));
                }

                // click on element
                if (rowHovered && FuWindow.CurrentDrawingWindow.Mouse.IsClicked(FuMouseButton.Left))
                {
                    setSelected(element, true, FuWindow.CurrentDrawingWindow.Keyboard.KeyAlt, FuWindow.CurrentDrawingWindow.Keyboard.KeyShift, FuWindow.CurrentDrawingWindow.Keyboard.KeyCtrl);
                }
            }

            // draw a leaf
            var child = _getDirectChildren(element);
            if (child == null || !child.Any())
            {
                Vector2 dotCenter = cursorPos + new Vector2(carretHitWidth * 0.5f, height * 0.5f);
                color.w *= 0.64f;
                drawList.AddCircleFilled(dotCenter, Mathf.Max(2f * scale, carretSize * 0.28f), ImGui.GetColorU32(color), 16);
                ImGui.Dummy(new Vector2(carretHitWidth, height));
            }
            // draw a carret
            else
            {
                ImGui.Dummy(new Vector2(carretHitWidth, height));
                Rect carretRect = new Rect(cursorPos, new Vector2(carretHitWidth, height));
                bool hover = ImGui.IsMouseHoveringRect(carretRect.min, carretRect.max);

                // set mouse cursor
                if (hover)
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }

                // get 
                if (hover && ImGui.IsMouseDown(ImGuiMouseButton.Left))
                {
                    color.w = 0.9f;
                    col = ImGui.GetColorU32(color);
                }
                else if (hover)
                {
                    color.w = 0.8f;
                    col = ImGui.GetColorU32(color);
                }
                bool open = _isOpen(element);
                if (hover)
                {
                    Vector4 toggleBg = Fugui.Themes.GetColor(FuColors.FrameHoverFeedback, 0.22f);
                    drawList.AddCircleFilled(carretRect.center, height * 0.32f, ImGui.GetColorU32(toggleBg), 20);
                }

                Vector2 carretPos = new Vector2(cursorPos.x + (carretHitWidth - carretSize) * 0.5f, cursorPos.y);
                if (open)
                {
                    drawList.AddTriangleFilled(
                        new Vector2(carretPos.x, cursorPos.y + (height / 2f) - (carretSize / 2f)),
                        new Vector2(carretPos.x + carretSize, cursorPos.y + (height / 2f) - (carretSize / 2f)),
                        new Vector2(carretPos.x + carretSize / 2f, cursorPos.y + (height / 2f) + (carretSize / 2f)),
                        col);
                }
                else
                {
                    drawList.AddTriangleFilled(
                        new Vector2(carretPos.x, cursorPos.y + (height / 2f) - (carretSize / 2f)),
                        new Vector2(carretPos.x + carretSize, cursorPos.y + (height / 2f)),
                        new Vector2(carretPos.x, cursorPos.y + (height / 2f) + (carretSize / 2f)),
                        col);
                }

                // click on carret
                if (hover && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                {
                    if (open)
                    {
                        TryClose(element, FuWindow.CurrentDrawingWindow.Keyboard.KeyCtrl);
                    }
                    else
                    {
                        TryOpen(element, FuWindow.CurrentDrawingWindow.Keyboard.KeyCtrl);
                    }
                }
            }

            ImGui.SameLine(0f, 0f);

            // draw custom element
            _display(element, layout);

            // Keep the row height stable for the internal list clipper.
            ImGui.SetCursorScreenPos(new Vector2(itemRect.xMin, itemRect.yMin + height));
        }
        #endregion
    }
}
