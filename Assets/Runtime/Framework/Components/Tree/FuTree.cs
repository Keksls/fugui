using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fu.Framework
{
    public class FuTree<T>
    {
        public delegate void DisplayDelegate(T element, FuLayout layout);
        private DisplayDelegate _display;
        public delegate bool IsOpenDelegate(T element);
        private IsOpenDelegate _isOpen;
        public delegate void OnOpenDelegate(T element);
        private OnOpenDelegate _onOpen;
        public delegate void OnCloseDelegate(T element);
        private OnCloseDelegate _onClose;
        public delegate int GetLevelDelegate(T element);
        private GetLevelDelegate _getLevel;
        public delegate bool EqualsDelegate(T elementA, T elementB);
        private EqualsDelegate _equals;
        public delegate IEnumerable<T> GetChildrenDelegate(T element);
        private GetChildrenDelegate _getDirectChildren;
        private string _id;
        private float _itemHeight;
        private List<T> _elements = new List<T>();
        private Action _onPostRenderAction = null;
        private FuTextStyle _carretStyle;

        public FuTree(string id, IEnumerable<T> elements, FuTextStyle carretStyle, DisplayDelegate display, OnOpenDelegate onOpen, OnCloseDelegate onClose, GetLevelDelegate getLevel, EqualsDelegate equals, GetChildrenDelegate getDirectChildren, IsOpenDelegate isOpen, float itemHeight)
        {
            _id = id;
            _carretStyle = carretStyle;
            _onOpen = onOpen;
            _onClose = onClose;
            _getLevel = getLevel;
            _equals = equals;
            _getDirectChildren = getDirectChildren;
            _display = display;
            _isOpen = isOpen;
            _itemHeight = itemHeight;
            UpdateTree(elements);
        }

        public void TryOpen(T element)
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
            getOpenChildren(children, out List<T> allChildren);

            _onPostRenderAction = () =>
            {
                _elements.InsertRange(index + 1, allChildren);
                _onPostRenderAction = null;
                _onOpen(element);
            };
        }

        public void TryClose(T element)
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

            // register the post render action
            int count = maxIndex - index;
            _onPostRenderAction = () =>
            {
                _elements.RemoveRange(index, count);
                _onPostRenderAction = null;
                _onClose(element);
            };
        }

        private void getOpenChildren(IEnumerable<T> elements, out List<T> fullList)
        {
            fullList = new List<T>();
            foreach (T element in elements)
            {
                getOpenChildren(element, fullList);
            }
        }

        private void getOpenChildren(T element, List<T> elements)
        {
            elements.Add(element);
            if (_isOpen(element))
            {
                var children = _getDirectChildren(element);
                if (children != null)
                {
                    foreach (T child in children)
                    {
                        getOpenChildren(child, elements);
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
            _elements = new List<T>();
            bindListFromItems(elements);
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
                        if (level > 0)
                        {
                            ImGui.Indent(indent * level);
                            for (int j = 0; j < level; j++)
                            {
                                drawList.AddLine(new Vector2(startPos.x + (indent * j), startPos.y - 2f * Fugui.CurrentContext.Scale),
                                    new Vector2(startPos.x + (indent * j), startPos.y + height), col, 1f);
                            }
                            drawElement(_elements[i], layout, drawList);
                            ImGui.Indent(-indent * level);
                        }
                        else
                        {
                            drawElement(_elements[i], layout, drawList);
                        }
                    }
                }
                Fugui.ListClipperEnd();
            }
            _onPostRenderAction?.Invoke();
        }


        private void drawElement(T element, FuLayout layout, ImDrawListPtr drawList)
        {
            float height = Fugui.CurrentContext.Scale * _itemHeight;
            Vector2 cursorPos = ImGui.GetCursorScreenPos();
            Vector4 color = _carretStyle.Text;
            uint col = ImGui.GetColorU32(color);
            float carretSize = 6f * Fugui.CurrentContext.Scale;
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
                        TryClose(element);
                    }
                    else
                    {
                        TryOpen(element);
                    }
                }
            }

            ImGui.SameLine();
            // draw custom element
            _display(element, layout);
        }
    }
}