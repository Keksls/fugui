using Fu.Core;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fu.Framework
{
    public partial class FuLayout
    {
        private static Dictionary<string, List<List<int>>> _hierarchicalPathRedo = new Dictionary<string, List<List<int>>>();

        /// <summary>
        /// Draw an interactive hierarchical path using given path
        /// </summary>
        /// <param name="ID">ID of the hierarchical path</param>
        /// <param name="rootItems">list of root items</param>
        /// <param name="path">a dictionary that contain path data. Keys are items of the path, Values are list of item chilren</param>
        /// <param name="onPathUpdated">callback called when the path is updated</param>
        /// <param name="width">width of the path</param>
        /// <param name="height">height of the path</param>
        public void HierarchicalPath(string ID, List<string> rootItems, List<(string, List<string>)> path, Action<List<int>> onPathUpdated, float width = 0f, float height = 22f)
        {
            #region drawing variables
            // determinate available width
            if (width == 0f)
            {
                width = GetAvailableWidth();
            }
            else if (width < 0f)
            {
                width = GetAvailableWidth() + width * Fugui.CurrentContext.Scale;
            }
            else
            {
                width *= Fugui.CurrentContext.Scale;
            }

            // determinate height
            height *= Fugui.CurrentContext.Scale;

            // declare some drawing variables
            Vector2 itemPadding = new Vector2(4f, 4f) * Fugui.CurrentContext.Scale;
            Vector2 itemCarretSize = new Vector2(14f * Fugui.CurrentContext.Scale + itemPadding.x * 2f, height);
            Vector2 popupPos = default;
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            float drawedWidth = 0f;

            // get current MouseState
            FuMouseState mouse = FuWindow.CurrentDrawingWindow != null ? FuWindow.CurrentDrawingWindow.Mouse : Fugui.MainContainer.Mouse;
            #endregion

            // draw path rect
            Rect pathRect = new Rect(ImGui.GetCursorScreenPos(), new Vector2(width, height));
            drawList.AddRectFilled(pathRect.position, pathRect.position + pathRect.size, ImGui.GetColorU32(ImGuiCol.FrameBg));
            drawList.AddRect(pathRect.position, pathRect.position + pathRect.size, ImGui.GetColorU32(ImGuiCol.Border));

            #region undo redo buttons
            // draw undo button
            Rect undoRect = new Rect(ImGui.GetCursorScreenPos(), new Vector2(height, height));
            bool isUndoHovered = IsItemHovered(undoRect.position, undoRect.size);
            bool hasUndo = GetCurrentPath().Count > 1;
            if (isUndoHovered && hasUndo)
            {
                // draw bg frame
                drawList.AddRectFilled(undoRect.position, undoRect.position + undoRect.size, ImGui.GetColorU32(ImGuiCol.HeaderHovered));
                // set mouse cursor
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                // click on undo
                if (mouse.IsDown(FuMouseButton.Left))
                {
                    // get current path
                    List<int> currentPath = GetCurrentPath();
                    // remove last item in current path
                    currentPath.RemoveAt(currentPath.Count - 1);
                    // send new path
                    SendNewPath(currentPath, true);
                }
            }
            Fugui.Push(ImGuiCols.Text, hasUndo ? FuThemeManager.GetColor(FuColors.Text) : FuThemeManager.GetColor(FuColors.TextDisabled));
            // draw undo icon
            EnboxedText("<##" + ID, undoRect.position, undoRect.size, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), FuTextWrapping.None);
            Fugui.PopColor();
            // draw rect border
            drawList.AddRect(undoRect.position, undoRect.position + undoRect.size, ImGui.GetColorU32(ImGuiCol.Border));
            // place cursor
            Fugui.MoveXUnscaled(undoRect.size.x);
            drawedWidth += undoRect.size.x;

            // draw redo button
            Rect redoRect = new Rect(ImGui.GetCursorScreenPos(), new Vector2(height, height));
            bool isRedoHovered = IsItemHovered(redoRect.position, redoRect.size);
            bool hasRedo = _hierarchicalPathRedo.ContainsKey(ID) && _hierarchicalPathRedo[ID].Count > 0;
            if (isRedoHovered && hasRedo)
            {
                // draw bg frame
                drawList.AddRectFilled(redoRect.position, redoRect.position + redoRect.size, ImGui.GetColorU32(ImGuiCol.HeaderHovered));
                // set mouse cursor
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                // click on redo
                if (mouse.IsDown(FuMouseButton.Left))
                {
                    // get last path
                    List<int> lastPath = _hierarchicalPathRedo[ID].Last();
                    // remove it from history
                    _hierarchicalPathRedo[ID].RemoveAt(_hierarchicalPathRedo[ID].Count - 1);
                    // send new path
                    SendNewPath(lastPath);
                }
            }
            Fugui.Push(ImGuiCols.Text, hasRedo ? FuThemeManager.GetColor(FuColors.Text) : FuThemeManager.GetColor(FuColors.TextDisabled));
            // draw redo icon
            EnboxedText(">##" + ID, redoRect.position, redoRect.size, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), FuTextWrapping.None);
            Fugui.PopColor();
            // draw rect border
            drawList.AddRect(redoRect.position, redoRect.position + redoRect.size, ImGui.GetColorU32(ImGuiCol.Border));
            // place cursor
            Fugui.MoveXUnscaled(redoRect.size.x);
            drawedWidth += redoRect.size.x;
            #endregion

            #region root items
            // draw root items carret
            Rect rootItemsRect = new Rect(ImGui.GetCursorScreenPos(), new Vector2(itemCarretSize.x, height));
            bool isRootItemsHovered = IsItemHovered(rootItemsRect.position, rootItemsRect.size);
            if (isRootItemsHovered)
            {
                // draw bg frame
                drawList.AddRectFilled(rootItemsRect.position, rootItemsRect.position + rootItemsRect.size, ImGui.GetColorU32(ImGuiCol.HeaderHovered));
                // draw down carret
                float cw = itemCarretSize.x / 3f;
                Fugui.DrawCarret_Down(drawList, rootItemsRect.position + new Vector2(itemCarretSize.x / 2f - cw / 2f, 0f), cw, itemCarretSize.y, FuThemeManager.GetColor(FuColors.Text));
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                // click on root items
                if (mouse.IsDown(FuMouseButton.Left))
                {
                    // open popup with root items
                    Fugui.OpenPopUp(ID + "popup", () =>
                    {
                        // draw children
                        int indx = 0;
                        foreach (string child in rootItems)
                        {
                            // draw child item
                            if (ImGui.Selectable(child, false))
                            {
                                // call callback
                                SendNewPath(new List<int> { indx });
                                // clear redo
                                _hierarchicalPathRedo.Remove(ID);
                            }
                            indx++;
                        }
                    });
                    // get popup pos
                    popupPos = rootItemsRect.position + new Vector2(0f, rootItemsRect.size.y);
                }
            }
            else
            {
                // draw carret right
                float cw = itemCarretSize.x / 3f;
                Fugui.DrawCarret_Right(drawList, rootItemsRect.position + new Vector2(itemCarretSize.x / 2f - cw / 2f, 0f), cw, itemCarretSize.y, FuThemeManager.GetColor(FuColors.Text));
            }
            // draw rect border
            drawList.AddRect(rootItemsRect.position, rootItemsRect.position + rootItemsRect.size, ImGui.GetColorU32(ImGuiCol.Border));
            // move cursor to draw items after it
            Fugui.MoveXUnscaled(rootItemsRect.size.x);
            drawedWidth += rootItemsRect.size.x;
            #endregion

            #region precalculate visible items
            // count nb visible items, starting by the end
            int nbVisibleItems = 0;
            float currentWidth = 0f;
            for (int i = path.Count - 1; i >= 0; i--)
            {
                float itemWidth = GetItemWidth(path[i]);
                if (currentWidth + itemWidth > width - drawedWidth)
                {
                    break;
                }
                currentWidth += itemWidth;
                nbVisibleItems++;
            }
            #endregion

            #region draw items
            // draw items side by side using clickableText
            for (int i = path.Count - nbVisibleItems; i < path.Count; i++)
            {
                // get drawing item
                (string, List<string>) item = path[i];
                // determinate whatever we need to draw caret
                bool drawCarret = item.Item2 != null && item.Item2.Count > 0;

                // determinate item rect
                Rect rect = new Rect(ImGui.GetCursorScreenPos(), new Vector2(GetItemWidth(item), height));
                // determinate caret rect
                Rect caretRect = new Rect(rect.position + new Vector2(rect.size.x - itemCarretSize.x, 0f), drawCarret ? itemCarretSize : Vector2.zero);
                // determinate whatever this item is hovered
                bool isHovered = IsItemHovered(rect.position, rect.size);
                // determinate whatever the caret is hovered
                bool isCaretHovered = IsItemHovered(caretRect.position, caretRect.size) && drawCarret;

                if (isHovered)
                {
                    // draw bg frame
                    Fugui.DrawGradientRect(rect.position, rect.size, 0.8f, 0f, drawList, FuThemeManager.GetColor(FuColors.HeaderHovered));

                    if (drawCarret)
                    {
                        // draw separator between item and caret
                        drawList.AddLine(rect.position + new Vector2(rect.size.x - itemCarretSize.x, 0f), rect.position + new Vector2(rect.size.x - itemCarretSize.x, rect.size.y), ImGui.GetColorU32(ImGuiCol.Border));
                        // draw caret
                        float cw = itemCarretSize.x / 3f;
                        Fugui.DrawCarret_Down(drawList, caretRect.position + new Vector2(itemCarretSize.x / 2f - cw / 2f, 0f), cw, itemCarretSize.y, FuThemeManager.GetColor(FuColors.Text));
                    }

                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                    // click on caret
                    if (isCaretHovered && mouse.IsDown(FuMouseButton.Left))
                    {
                        int currentIndex = i;
                        // open popup
                        Fugui.OpenPopUp(ID + "popup", () =>
                        {
                            // draw children
                            int indx = 0;
                            foreach (string child in path[currentIndex].Item2)
                            {
                                // draw child item
                                if (ImGui.Selectable(child, false))
                                {
                                    // determinate new path
                                    List<int> newPath = new List<int>();
                                    for (int j = 0; j < currentIndex + 1; j++)
                                    {
                                        // get index of item
                                        int index = 0;
                                        if (j == 0)
                                        {
                                            foreach (string rootItem in rootItems)
                                            {
                                                if (rootItem == path[j].Item1)
                                                {
                                                    break;
                                                }
                                                index++;
                                            }
                                            newPath.Add(index);
                                        }
                                        else
                                        {
                                            foreach (string brother in path[j - 1].Item2)
                                            {
                                                if (brother == path[j].Item1)
                                                {
                                                    break;
                                                }
                                                index++;
                                            }
                                            newPath.Add(index);
                                        }
                                    }
                                    newPath.Add(indx);
                                    // call callback
                                    SendNewPath(newPath);
                                    // clear redo
                                    _hierarchicalPathRedo.Remove(ID);
                                }
                                indx++;
                            }
                        });
                        // get popup pos
                        popupPos = rect.position + new Vector2(rect.size.x - itemCarretSize.x, rect.size.y);
                    }
                    // click on item
                    else if (mouse.IsDown(FuMouseButton.Left))
                    {
                        // determinate new path
                        List<int> newPath = new List<int>();
                        for (int j = 0; j <= i; j++)
                        {
                            // get index of item
                            int index = 0;
                            if (j == 0)
                            {
                                foreach (string rootItem in rootItems)
                                {
                                    if (rootItem == path[j].Item1)
                                    {
                                        break;
                                    }
                                    index++;
                                }
                                newPath.Add(index);
                            }
                            else
                            {
                                foreach (string brother in path[j - 1].Item2)
                                {
                                    if (brother == path[j].Item1)
                                    {
                                        break;
                                    }
                                    index++;
                                }
                                newPath.Add(index);
                            }
                        }
                        // call callback
                        SendNewPath(newPath);
                        // clear redo
                        _hierarchicalPathRedo.Remove(ID);
                    }
                }
                else
                {
                    if (drawCarret)
                    {
                        // draw caret
                        float cw = itemCarretSize.x / 3f;
                        Fugui.DrawCarret_Right(drawList, caretRect.position + new Vector2(itemCarretSize.x / 2f - cw / 2f, 0f), cw, itemCarretSize.y, FuThemeManager.GetColor(FuColors.Text));
                    }
                }

                // draw item text
                EnboxedText(item.Item1, rect.position, rect.size - new Vector2(drawCarret ? itemCarretSize.x : 0f, 0f), itemPadding, Vector2.zero, new Vector2(0f, 0.5f), FuTextWrapping.Clip);

                // draw rect border
                drawList.AddRect(rect.position, rect.position + rect.size, ImGui.GetColorU32(ImGuiCol.Border));

                // move cursor after drawing item
                Fugui.MoveXUnscaled(rect.size.x);
            }
            #endregion

            // Draw dummy to place cursor for ImGui
            ImGui.SetCursorScreenPos(pathRect.min);
            ImGui.Dummy(pathRect.size);

            // Draw popup
            Fugui.DrawPopup(ID + "popup", new Vector2(256f, 0f), popupPos);

            /// <summary>
            /// Het the width of a path item
            /// </summary>
            float GetItemWidth((string, List<string>) item)
            {
                float itemWidth = ImGui.CalcTextSize(item.Item1).x + itemPadding.x * 2f;
                if (item.Item2 != null && item.Item2.Count > 0)
                {
                    itemWidth += itemCarretSize.x;
                }
                return itemWidth;
            }

            /// <summary>
            /// Send the new path to the callback
            /// </summary>
            void SendNewPath(List<int> newPath, bool addToRedo = false)
            {
                if (addToRedo)
                {
                    // add to redo
                    if (!_hierarchicalPathRedo.ContainsKey(ID))
                    {
                        _hierarchicalPathRedo.Add(ID, new List<List<int>>());
                    }
                    _hierarchicalPathRedo[ID].Add(GetCurrentPath());
                    // keep only 20 last history
                    if (_hierarchicalPathRedo[ID].Count > 20)
                    {
                        _hierarchicalPathRedo[ID].RemoveAt(0);
                    }
                }

                // call callback in main thread (the purpuse here is not to be thread safe but to avoid to call the callback in the middle of the render loop)
                Fugui.ExecuteInMainThread(() =>
                {
                    onPathUpdated?.Invoke(newPath);
                });
            }

            /// <summary>
            /// Get the current full path
            /// </summary>
            List<int> GetCurrentPath()
            {
                // determinate current path
                List<int> currentPath = new List<int>();

                for (int j = 0; j < path.Count; j++)
                {
                    // get index of item
                    int index = 0;
                    if (j == 0)
                    {
                        foreach (string rootItem in rootItems)
                        {
                            if (rootItem == path[j].Item1)
                            {
                                break;
                            }
                            index++;
                        }
                        currentPath.Add(index);
                    }
                    else
                    {
                        foreach (string brother in path[j - 1].Item2)
                        {
                            if (brother == path[j].Item1)
                            {
                                break;
                            }
                            index++;
                        }
                        currentPath.Add(index);
                    }
                }

                return currentPath;
            }
        }

        /// <summary>
        /// Clean the history of a hierarchical path
        /// </summary>
        /// <param name="ID">ID of the hierarchical path top clean</param>
        public void CleanHierarchicalPathHistory(string ID)
        {
            _hierarchicalPathRedo.Remove(ID);
        }
    }
}