using Fu.Core;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fu.Framework
{
    public class FuTree<T>
    {
        private Action<T, FuLayout> _display;
        private Func<T, bool> _isOpen;
        private Func<T, bool> _isSelected;
        private Func<T, float, Vector2> _getSelectableSize;
        private Action<T> _onOpen;
        private Action<T> _onClose;
        private Action<List<T>> _onSelect;
        private Action<List<T>> _onDeSelect;
        private Func<T, int> _getLevel;
        private Func<T, T, bool> _equals;
        private Func<T, IEnumerable<T>> _getDirectChildren;
        private Func<IEnumerable<T>> _getAll;
        private string _id;
        private float _itemHeight;
        private List<T> _elements = new List<T>();
        private Action _onPostRenderAction = null;
        private FuTextStyle _carretStyle;

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

        public void TryOpen(T element, bool recursively)
        {
            int index = GetIndex(element);
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

        public void TryClose(T element, bool recursively)
        {
            int index = GetIndex(element) + 1;
            if (index == -1)
            {
                Debug.LogError("Tree fail to get element " + element.ToString());
                return;
            }

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
                for (int i = index; i <= count; i++)
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

        private void getOpenChildren(IEnumerable<T> elements, out List<T> fullList, bool recursively, bool autoOpen)
        {
            fullList = new List<T>();
            foreach (T element in elements)
            {
                getOpenChildren(element, fullList, recursively, autoOpen);
            }
        }

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

        private int GetIndex(T element)
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

        public void UpdateTree(IEnumerable<T> elements)
        {
            if (elements != null)
            {
                _elements = new List<T>();
                bindListFromItems(elements);
            }
        }

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

        public void Select(T element, bool recursive, bool startEnd, bool additive, IEnumerable<T> currentSelection = null)
        {
            setSelected(element, true, recursive, startEnd, additive, currentSelection);
        }

        public void Deselect(T element, bool recursive, bool startEnd, bool additive, IEnumerable<T> currentSelection = null)
        {
            setSelected(element, false, recursive, startEnd, additive, currentSelection);
        }

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
                    selection = new List<T> { element };
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

        public void DrawTree()
        {
            float height = Fugui.CurrentContext.Scale * _itemHeight;
            float indent = 16f * Fugui.CurrentContext.Scale;
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            uint col = ImGui.GetColorU32(new Vector4(_carretStyle.Text.r, _carretStyle.Text.g, _carretStyle.Text.b, 0.1f));
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
                        // get item rect
                        Rect itemRect = new Rect(ImGui.GetCursorScreenPos(), new Vector2(ImGui.GetContentRegionAvail().x, (_itemHeight * Fugui.CurrentContext.Scale)));
                        if (level > 0)
                        {
                            // indent the cursor
                            ImGui.Indent(indent * level);
                            // draw the V lines
                            for (int j = 0; j < level; j++)
                            {
                                drawList.AddLine(new Vector2(startPos.x + (indent * j), startPos.y - 2f * Fugui.CurrentContext.Scale),
                                    new Vector2(startPos.x + (indent * j), startPos.y + height), col, 1f);
                            }
                            // draw the element
                            drawElement(_elements[i], layout, drawList, itemRect);
                            // cancel the indent
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
            _onPostRenderAction?.Invoke();
        }

        private void drawElement(T element, FuLayout layout, ImDrawListPtr drawList, Rect itemRect)
        {
            float height = Fugui.CurrentContext.Scale * _itemHeight;
            Vector2 cursorPos = ImGui.GetCursorScreenPos();
            Vector4 color = _carretStyle.Text;
            uint col = ImGui.GetColorU32(color);
            float carretSize = 6f * Fugui.CurrentContext.Scale;
            Vector2 selectableSize = _getSelectableSize(element, ImGui.GetContentRegionAvail().x - (carretSize + 4f * Fugui.CurrentContext.Scale));

            // draw hover rect
            Vector2 rectMin = cursorPos + new Vector2(carretSize + 4f * Fugui.CurrentContext.Scale, 0f);
            if (ImGui.IsMouseHoveringRect(rectMin, rectMin + selectableSize))
            {
                drawList.AddRectFilled(itemRect.min, itemRect.max, FuWindow.CurrentDrawingWindow.Mouse.IsDown(0) ? ImGui.GetColorU32(ImGuiCol.HeaderActive) : ImGui.GetColorU32(ImGuiCol.HeaderHovered));
                // click on element
                if (FuWindow.CurrentDrawingWindow.Mouse.IsUp(0))
                {
                    setSelected(element, true, !FuWindow.CurrentDrawingWindow.Keyboard.KeyAlt, FuWindow.CurrentDrawingWindow.Keyboard.KeyShift, FuWindow.CurrentDrawingWindow.Keyboard.KeyCtrl);
                }
            }
            // draw selected rect
            else if (_isSelected(element))
            {
                drawList.AddRectFilled(itemRect.min, itemRect.max, ImGui.GetColorU32(ImGuiCol.Header));
            }

            // draw a leaf
            var child = _getDirectChildren(element);
            if (child == null || child.Count() == 0)
            {
                carretSize = 4f * Fugui.CurrentContext.Scale;
                drawList.AddCircleFilled(cursorPos + new Vector2(2f * Fugui.CurrentContext.Scale, height / 2f - carretSize / 2f), carretSize / 2f, col);
                ImGui.Dummy(new Vector2(carretSize + 4f * Fugui.CurrentContext.Scale, carretSize));
            }
            // draw a carret
            else
            {
                ImGui.Dummy(new Vector2(carretSize + 4f * Fugui.CurrentContext.Scale, carretSize));
                bool hover = ImGui.IsMouseHoveringRect(cursorPos, cursorPos + new Vector2(carretSize + 4f * Fugui.CurrentContext.Scale, height));
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
                if (open)
                {
                    drawList.AddTriangleFilled(
                        new Vector2(cursorPos.x, cursorPos.y + (height / 2f) - (carretSize / 2f)),
                        new Vector2(cursorPos.x + carretSize, cursorPos.y + (height / 2f) - (carretSize / 2f)),
                        new Vector2(cursorPos.x + carretSize / 2f, cursorPos.y + (height / 2f) + (carretSize / 2f)),
                        col);
                }
                else
                {
                    drawList.AddTriangleFilled(
                        new Vector2(cursorPos.x, cursorPos.y + (height / 2f) - (carretSize / 2f)),
                        new Vector2(cursorPos.x + carretSize, cursorPos.y + (height / 2f)),
                        new Vector2(cursorPos.x, cursorPos.y + (height / 2f) + (carretSize / 2f)),
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

            ImGui.SameLine();

            // draw custom element
            _display(element, layout);
        }
    }
}