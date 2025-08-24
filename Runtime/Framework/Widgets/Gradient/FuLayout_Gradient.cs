using Fu;
using ImGuiNET;
using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework
{
    public partial class FuLayout
    {
        private const float COLOR_KEY_SIZE = 10f;
        private static bool isDraggingColorKey = false;
        private static int _selectedColorKeyIndex = 0;
        private static FuGradient _currentGradient;
        private static FuGradient _updatedGradient;
        private static bool _gradientUpdated = false;

        /// <summary>
        /// Draw a gradient picker
        /// </summary>
        /// <param name="text">text / ID of the gradient</param>
        /// <param name="gradient">gradient to edit</param>
        /// <param name="addKeyOnGradientClick">if enabled, allow the user to add a key on gradient click</param>
        /// <param name="allowAlpha">Whatever the gradient allow transparency on color keys</param>
        /// <param name="relativeMin">The value represented when time = 0. If bigger or equal to RelativeMax, gradient will not take this in account</param>
        /// <param name="relativeMax">The value represented when time = 1. If smaller or equal to RelativeMin, gradient will not take this in account</param>
        /// <param name="defaultGradientValues">the values to set for reseting this gradient</param>
        /// <returns>whatever the gradient has been edited this frame</returns>
        public virtual bool Gradient(string text, ref FuGradient gradient, bool addKeyOnGradientClick = true, bool allowAlpha = true, float relativeMin = 0, float relativeMax = 0, FuGradientColorKey[] defaultGradientValues = null)
        {
            beginElement(ref text);
            string ppID = text + "gpPp";
            _currentGradient = gradient;

            // get gradient data
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 mousePos = ImGui.GetMousePos();
            Vector2 startPos = ImGui.GetCursorScreenPos();
            Rect gradientRect = new Rect(startPos, new Vector2(ImGui.GetContentRegionAvail().x, 18f * Fugui.CurrentContext.Scale));
            gradientRect.xMax -= 2 * Fugui.CurrentContext.Scale;
            gradientRect.yMax -= 2 * Fugui.CurrentContext.Scale;
            Texture2D gradientTexture = gradient.GetGradientTexture();

            // Draw tile background
            Fugui.DrawTilesBackground(drawList, startPos, gradientRect.size);

            // Draw the gradient texture
            if (FuWindow.CurrentDrawingWindow == null)
            {
                Fugui.MainContainer.ImGuiImage(gradientTexture, gradientRect.size, Color.white);
            }
            else
            {
                FuWindow.CurrentDrawingWindow.Container.ImGuiImage(gradientTexture, gradientRect.size, Color.white);
            }

            // draw border
            drawList.AddRect(gradientRect.min, gradientRect.max, ImGui.GetColorU32(ImGuiCol.Border));

            // check whatever the preview is hovered
            bool hovered = IsItemHovered(startPos, gradientRect.size);
            if (hovered)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            // draw invisible button to prevent draw popup and handle native ImGui state on gradient image and carrets
            ImGui.SetCursorScreenPos(startPos);
            if (ImGui.InvisibleButton(text + "nvsbB", gradientRect.size))
            {
                Fugui.OpenPopUp(ppID, drawPicker);
            }

            // set element states
            setBaseElementState(text, startPos, gradientRect.size, true, false, false);
            // display tooltip if needed
            displayToolTip(false);
            // end the element and draw a hover border if needed
            _elementHoverFramedEnabled = true;
            endElement();

            // callback to draw popup
            void drawPicker()
            {
                // Gradient editor title
                Fugui.Push(ImGuiStyleVar.FramePadding, new Vector4(4f, 4f));
                FramedText("Gradient Editor");
                Fugui.PopStyle();

                Spacing();
                SameLine();
                BeginGroup();
                _gradientUpdated = _customGradientPicker(text, addKeyOnGradientClick, allowAlpha, relativeMin, relativeMax, defaultGradientValues);
                EndGroup();
                SameLine();
                Spacing();
            }

            // draw the popup if needed
            Fugui.DrawPopup(ppID, new Vector2(320f, 0f), Vector2.zero);
            gradient = _currentGradient;

            // return whatever the gradient was edited this frame
            return _gradientUpdated;
        }

        private bool _customGradientPicker(string text, bool addKeyOnGradientClick, bool allowAlpha, float relativeMin, float relativeMax, FuGradientColorKey[] defaultGradientValues)
        {
            text = "##" + text;
            bool edited = false;
            float colorKeySize = COLOR_KEY_SIZE * Fugui.CurrentContext.Scale;
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            FuLayout layout = new FuLayout();
            // Draw Header
            // TODO : Add Fugui icons and use it to draw buttons glyphs

            // Add a new color key
            if (layout.Button("+##addKey" + text, new FuElementSize(24f, 0f)))
            {
                // get selected key
                if (_currentGradient.GetKey(_selectedColorKeyIndex, out FuGradientColorKey selectedKey))
                {
                    float selectedTime = selectedKey.Time;

                    // get neighbor key
                    int neighborIndex = _selectedColorKeyIndex + 1;
                    if (_currentGradient.GetKeysCount() <= neighborIndex)
                    {
                        neighborIndex = _selectedColorKeyIndex - 1;
                    }
                    if (_currentGradient.GetKey(neighborIndex, out FuGradientColorKey neighborKey))
                    {
                        float neighborTime = neighborKey.Time;

                        // Add new key at avg time
                        float newKeyTime = (selectedTime + neighborTime) / 2f;
                        _selectedColorKeyIndex = _currentGradient.AddColorKey(newKeyTime, _currentGradient.Evaluate(newKeyTime));
                        edited = true;
                    }
                }

            }
            layout.SameLine();

            // Remove selected color key
            if (layout.Button("-##remKey" + text, new FuElementSize(24f, 0f)))
            {
                _currentGradient.RemoveColorKey(_selectedColorKeyIndex);
                edited = true;
                _selectedColorKeyIndex--;
                if (_selectedColorKeyIndex < 0)
                {
                    _selectedColorKeyIndex = 0;
                }
            }
            layout.SameLine();

            // draw Key index
            ImGui.SetNextItemWidth(64f);
            Fugui.MoveY(2f);
            int keyIndex = _selectedColorKeyIndex + 1;
            if (ImGui.DragInt("##kNdx" + text, ref keyIndex, 0.1f, 1, _currentGradient.GetKeysCount(), "%d / " + _currentGradient.GetKeysCount()))
            {
                _selectedColorKeyIndex = keyIndex - 1;
            }
            layout.SameLine();

            // set the blending mode
            layout.SetNextElementToolTipWithLabel("However you want this gradient to blend color values");
            layout.ComboboxEnum<FuGradientBlendMode>("Blending", (index) =>
            {
                _currentGradient.SetBlendMode((FuGradientBlendMode)index);
            }, () => _currentGradient.BlendMode, new Vector2(GetAvailableWidth() - 52f * Fugui.CurrentContext.Scale, 0f), Vector2.zero, FuButtonStyle.Default);
            layout.SameLine();
            layout.Combobox("##GpStng" + text, FuIcons.Fu_Gear_Duotone, () =>
            {
                if (ImGui.Selectable("Reset gradient"))
                {
                    if (defaultGradientValues == null)
                    {
                        _currentGradient.SetKeys(new FuGradientColorKey[]{
                        new FuGradientColorKey(0f, Color.black),
                        new FuGradientColorKey(1f, Color.white)
                        });
                    }
                    else
                    {
                        _currentGradient.SetKeys(defaultGradientValues);
                    }

                    Debug.Log("TODO : Implement gradient reset");
                }
                if (ImGui.Selectable("Invert gradient"))
                {
                    _currentGradient.Invert();
                }
            }, FuElementSize.FullSize, new Vector2(102f, -1f), FuButtonStyle.Default);
            Fugui.PopContextMenuItems();
            Fugui.PopContextMenuItems();
            layout.Separator();

            Vector2 mousePos = ImGui.GetMousePos();
            Vector2 startPos = ImGui.GetCursorScreenPos();
            Rect gradientRect = new Rect(startPos, new Vector2(ImGui.GetContentRegionAvail().x - 4f, 32f));
            Texture2D gradientTexture = _currentGradient.GetGradientTexture();

            // Draw tile background
            Fugui.DrawTilesBackground(drawList, startPos, gradientRect.size);

            // Draw the gradient texture
            if (FuWindow.CurrentDrawingWindow == null)
            {
                Fugui.MainContainer.ImGuiImage(gradientTexture, gradientRect.size, Color.white);
            }
            else
            {
                FuWindow.CurrentDrawingWindow.Container.ImGuiImage(gradientTexture, gradientRect.size, Color.white);
            }

            bool isAnyKeyHovered = false;
            // Draw the color keys
            for (int i = 0; i < _currentGradient.GetKeysCount(); i++)
            {
                if (_currentGradient.GetKey(i, out FuGradientColorKey key))
                {
                    Vector2 keyPos = new Vector2(gradientRect.x + key.Time * gradientRect.width, gradientRect.yMax);
                    Rect colorKeyRect = new Rect(gradientRect.x + key.Time * gradientRect.width - colorKeySize / 2, gradientRect.yMax + 4, colorKeySize, colorKeySize);
                    ImGui.SetCursorScreenPos(colorKeyRect.min);
                    ImGui.InvisibleButton(text + "ck" + i, colorKeyRect.size);

                    // get key states
                    bool hovered = IsItemHovered(colorKeyRect.position, colorKeyRect.size);
                    isAnyKeyHovered |= hovered;
                    bool active = _selectedColorKeyIndex == i;

                    // draw Line
                    //drawList.AddLine(keyPos - new Vector2(0f, gradientRect.height), keyPos, ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.Knob)), active ? 3f : 1f);
                    if (active)
                    {
                        drawList.AddLine(keyPos - new Vector2(0f, gradientRect.height), keyPos, ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.Knob)), 1f);
                    }

                    // set tooltip
                    SetToolTip(text + "key" + i, string.Format("Time: {0:F2}\nColor: ({1:F2}, {2:F2}, {3:F2})", key.Time, key.Color.r, key.Color.g, key.Color.b), hovered, !LastItemDisabled, FuTextStyle.Default);

                    // start drag on mouse down
                    if (hovered)
                    {
                        if (!isDraggingColorKey && ImGui.IsMouseDown(ImGuiMouseButton.Left))
                        {
                            isDraggingColorKey = true;
                            _selectedColorKeyIndex = i;
                        }
                    }

                    // draw carret
                    Color caretColor = Fugui.Themes.GetColor(FuColors.Knob);
                    if (hovered)
                    {
                        caretColor = Fugui.Themes.GetColor(FuColors.KnobHovered);
                    }
                    if (active)
                    {
                        caretColor = Fugui.Themes.GetColor(FuColors.KnobActive);
                        Fugui.DrawCarret_Top(drawList, colorKeyRect.position, colorKeySize, colorKeySize, caretColor);
                    }
                    else
                    {
                        Fugui.DrawCarret_Top(drawList, colorKeyRect.position + (Vector2.one * colorKeySize * 0.25f), colorKeySize * 0.5f, colorKeySize * 0.5f, key.Color/*Color caretColor*/);
                    }

                    // remove on right click
                    if (hovered && !isDraggingColorKey && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                    {
                        _currentGradient.RemoveColorKey(i);
                        edited = true;
                        if (i == _selectedColorKeyIndex)
                        {
                            _selectedColorKeyIndex = 0;
                        }
                    }
                }
            }

            // Handle mouse events
            if (!isDraggingColorKey && addKeyOnGradientClick)
            {
                if (!isAnyKeyHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    if (IsItemHovered(startPos, gradientRect.size))
                    {
                        // Add a new color key
                        float t = Mathf.Clamp01((mousePos.x - gradientRect.x) / gradientRect.width);
                        Color color = _currentGradient.Evaluate(t);
                        _currentGradient.AddColorKey(t, color);
                        edited = true;
                    }
                }
            }

            // stop dragging key
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) && isDraggingColorKey)
            {
                isDraggingColorKey = false;
            }

            // Update the selected color key's position
            if (isDraggingColorKey)
            {
                if (_currentGradient.GetKey(_selectedColorKeyIndex, out FuGradientColorKey key))
                {
                    SetToolTip(text + "key" + _selectedColorKeyIndex, string.Format("Time: {0:F2}\nColor: ({1:F2}, {2:F2}, {3:F2})", key.Time, key.Color.r, key.Color.g, key.Color.b), true);
                    float t = Mathf.Clamp01((mousePos.x - gradientRect.x) / gradientRect.width);
                    _currentGradient.SetKeyTime(_selectedColorKeyIndex, t);
                    edited = true;
                }
            }

            // display color picker and key time if needed
            if (_selectedColorKeyIndex >= 0)
            {
                if (_currentGradient.GetKey(_selectedColorKeyIndex, out FuGradientColorKey key))
                {
                    layout.Separator();
                    using (FuGrid grid = new FuGrid(text + "grdPkrFtrGrd", new FuGridDefinition(2, new float[] { 0.5f, 0.5f })))
                    {
                        grid.NextColumn();
                        layout.Text("Color");
                        layout.SameLine();
                        // color with Alpha
                        if (allowAlpha)
                        {
                            Vector4 col = key.Color;
                            if (layout.ColorPicker(text + "cp", ref col))
                            {
                                _currentGradient.SetKeyColor(_selectedColorKeyIndex, col);
                            }
                        }
                        // color without Alpha
                        else
                        {
                            Vector3 col = (Vector4)key.Color;
                            if (layout.ColorPicker(text + "cp", ref col))
                            {
                                Vector4 newCol = (Vector4)col;
                                newCol.w = 1f;
                                _currentGradient.SetKeyColor(_selectedColorKeyIndex, newCol);
                            }
                        }
                        grid.NextColumn();
                        layout.Text("Location");
                        layout.SameLine();
                        if (relativeMin >= relativeMax)
                        {
                            float time = key.Time * 100f;
                            if (layout.Drag("##drag" + text, ref time, string.Empty, 0f, 100f, 0.01f, "%.1f %%"))
                            {
                                _currentGradient.SetKeyTime(_selectedColorKeyIndex, time / 100f);
                            }
                        }
                        else
                        {
                            float relativeTime = Mathf.Lerp(relativeMin, relativeMax, key.Time);
                            if (layout.Drag("##drag" + text, ref relativeTime, string.Empty, relativeMin, relativeMax, format: "%.2f"))
                            {
                                float time = (relativeTime - relativeMin) / (relativeMax - relativeMin);
                                _currentGradient.SetKeyTime(_selectedColorKeyIndex, time);
                            }
                        }
                    }
                }
            }

            // set mouse cursor if hover a key
            if (isAnyKeyHovered)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }
            layout.Dispose();

            return edited;
        }
    }
}