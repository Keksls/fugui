using ImGuiNET;
using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework
{
    internal class FuPanelClipper
    {
        internal List<Rect> itemRects = new List<Rect>();
        internal List<Rect> lastItemRects = new List<Rect>();
        internal Vector2 scrollRectY;
        internal static int itemIndex = 0;
        internal bool _clipOutOfBounds = false;
        private bool _drawItem = true;
        private Vector2 _screenToLocalOffset = default;
        internal bool ForceUpdateNextFrame = false;

        internal FuPanelClipper()
        {
            if (FuWindow.CurrentDrawingWindow != null)
            {
                FuWindow.CurrentDrawingWindow.OnResized += CurrentDrawingWindow_OnResized;
                FuWindow.CurrentDrawingWindow.OnClosed += CurrentDrawingWindow_OnClosed;
            }
        }

        private void CurrentDrawingWindow_OnClosed(FuWindow window)
        {
            window.OnResized -= CurrentDrawingWindow_OnResized;
        }

        private void CurrentDrawingWindow_OnResized(FuWindow window)
        {
            ForceUpdateNextFrame = true;
        }

        internal void NewFrame(bool autoClipOutOfView)
        {
            // whatever we want to clip OutOfBounds content
            _clipOutOfBounds = autoClipOutOfView && FuPanel.IsInsidePanel && itemRects.Count == lastItemRects.Count && !ForceUpdateNextFrame;
            itemRects.Clear();
            ForceUpdateNextFrame = false;
            itemIndex = -1;
            Vector2 scrollRectSize = ImGui.GetContentRegionAvail();
            if (_clipOutOfBounds)
            {
                _screenToLocalOffset = ImGui.GetCursorScreenPos() - ImGui.GetCursorPos();
                scrollRectY = new Vector2(ImGuiNative.igGetScrollY(), ImGuiNative.igGetScrollY() + scrollRectSize.y);
                scrollRectY.x -= Fugui.Settings.ClipperSafeRangePx * Fugui.CurrentContext.Scale;
                scrollRectY.y += Fugui.Settings.ClipperSafeRangePx * Fugui.CurrentContext.Scale;
            }
            else
            {
                lastItemRects.Clear();
            }
        }

        internal void EndFrame()
        {
            if (itemRects.Count > 0)
            {
                // TODO : use swap bugffer pattern to avoid copying
                lastItemRects = new List<Rect>(itemRects);
            }
        }

        internal bool BeginDrawElement(bool canBeHidden)
        {
            _drawItem = true;
            itemIndex++;
            // check item rect
            if (_clipOutOfBounds && canBeHidden)
            {
                if (lastItemRects.Count > itemIndex)
                {
                    Rect itemBounds = lastItemRects[itemIndex];
                    float cursorY = ImGuiNative.igGetCursorPosY();
                    // return whatever the next item was in the scroll rect at last frame
                    _drawItem = (cursorY + itemBounds.size.y) > scrollRectY.x && cursorY < scrollRectY.y;

                    // if out of scroll bounds but null size, let's draw it again to get size for next frame
                    if (!_drawItem && (itemBounds.size.x == 0 || itemBounds.size.y == 0))
                    {
                        _drawItem = true;
                    }
                }
            }

            if (!_drawItem)
            {
                // item not draw this frame, let's dummy it
                ImGuiNative.igDummy(lastItemRects[itemIndex].size);

                // Debug, draw a rect a dummy place. Comment this after debugging
                // ImGui.Button("", lastItemRects[itemIndex].size);
            }
            return _drawItem;
        }

        internal void EndDrawElement()
        {
            // the item has just been draw
            if (_drawItem)
            {
                // save item rect
                itemRects.Add(new Rect(ImGui.GetItemRectMin() - _screenToLocalOffset, ImGui.GetItemRectSize()));
            }
            else
            {
                itemRects.Add(lastItemRects[itemIndex]);
            }
        }
    }
}