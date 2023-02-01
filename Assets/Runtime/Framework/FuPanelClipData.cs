using ImGuiNET;
using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework
{
    internal class FuPanelClipper
    {
        internal List<Vector2> itemRects = new List<Vector2>();
        internal List<Vector2> lastItemRects = new List<Vector2>();
        internal Rect scrollRect;
        internal static int itemIndex = 0;
        internal float _currentCursorY = 0f;
        internal bool _clipOutOfBounds = false;
        internal string _currentParentLayoutID = string.Empty;
        private bool _drawItem = true;

        internal void NewFrame(bool autoClipOutOfView)
        {
            // whatever we want to clip OutOfBounds content
            _clipOutOfBounds = (autoClipOutOfView && FuPanel.IsInsidePanel) || !string.IsNullOrEmpty(_currentParentLayoutID);
            if (_clipOutOfBounds)
            {
                itemIndex = -1;
                itemRects.Clear();
                scrollRect = GetCurrentScrollBounds();
                _currentCursorY = ImGui.GetCursorPosY();
            }
        }

        internal void EndFrame()
        {
            if (itemRects.Count > 0)
            {
                lastItemRects = new List<Vector2>(itemRects);
            }
        }

        internal bool BeginDrawElement()
        {
            _drawItem = true;
            _currentCursorY = ImGui.GetCursorPosY();
            // check item rect
            if (_clipOutOfBounds)
            {
                itemIndex++;
                if (lastItemRects.Count > itemIndex)
                {
                    Vector2 itemYBounds = lastItemRects[itemIndex];
                    // return whatever the next item was in the scroll rect at last frame
                    _drawItem = itemYBounds.y >= scrollRect.min.y + 32f && itemYBounds.x <= scrollRect.max.y - 32f;
                }
                // if out of scroll bounds, we must dummy the element rect
                if (!_drawItem)
                {
                    float height = lastItemRects[itemIndex].y - lastItemRects[itemIndex].x;
                    if (height != 0)
                    {
                        ImGui.Button(itemIndex.ToString(), new Vector2(64f, height));
                    }
                }
            }
            return _drawItem;
        }

        internal void EndDrawElement()
        {
            if (_drawItem)
            {
                // save item rect
                itemRects.Add(new Vector2(_currentCursorY, ImGui.GetCursorPosY()));
            }
            else
            {
                itemRects.Add(lastItemRects[itemIndex]);
            }
        }

        private Rect GetCurrentScrollBounds()
        {
            Vector2 scrollSize = ImGui.GetContentRegionAvail();
            float scrollMaxY = ImGuiNative.igGetScrollMaxY();
            if (scrollSize.y < 1 || float.IsNaN(scrollMaxY) || scrollMaxY == 0)
            {
                _clipOutOfBounds = false;
                return default;
            }
            float scrollAmount = ImGuiNative.igGetScrollY() / scrollMaxY;
            Vector2 scrollPos = new Vector2(0f, scrollSize.y * scrollAmount);
            Rect scrollBounds = new Rect(scrollPos, scrollSize);
            return scrollBounds;
        }
    }
}