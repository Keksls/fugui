using Fu.Core;
using ImGuiNET;
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
        /// <param name="width">width of the gradient picker popup</param>
        /// <param name="height">height of the gradient preview on popup</param>
        /// <returns>whatever the gradient has been edited this frame</returns>
        public virtual bool Gradient(string text, ref FuGradient gradient, float width = 256f, float height = 24f)
        {
            beginElement(ref text);
            string ppID = text + "gpPp";
            _currentGradient = gradient;

            // get gradient data
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 mousePos = ImGui.GetMousePos();
            Vector2 startPos = ImGui.GetCursorScreenPos();
            Rect gradientRect = new Rect(startPos, new Vector2(ImGui.GetContentRegionAvail().x, 18f * Fugui.CurrentContext.Scale));
            Texture2D gradientTexture = gradient.GetGradientTexture((int)gradientRect.size.x);

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
            bool hovered = isItemHovered(startPos, gradientRect.size);
            if (hovered)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            // draw invisible button to prevent draw popup and handle native ImGui state on gradient image and carrets
            ImGui.SetCursorScreenPos(startPos);
            if (ImGui.InvisibleButton(text + "nvsbB", gradientRect.size))
            {
                OpenPopUp(ppID, drawPicker);
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
                Spacing();
                Spacing();
                SameLine();
                BeginGroup();
                _gradientUpdated = _customGradientPicker(text, width, height);
                EndGroup();
                SameLine();
                Spacing();
                Spacing();
            }

            // draw the popup if needed
            DrawPopup(ppID, new Vector2(width + (FuThemeManager.CurrentTheme.ItemSpacing.x * 2f * Fugui.CurrentContext.Scale), 0f), Vector2.zero);
            gradient = _currentGradient;

            // return whatever the gradient was edited this frame
            return _gradientUpdated;
        }

        private bool _customGradientPicker(string text, float width, float height)
        {
            text = "##" + text;
            bool edited = false;
            float colorKeySize = COLOR_KEY_SIZE * Fugui.CurrentContext.Scale;
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            // draw blending mode combobox
            using (FuGrid grid = new FuGrid(text + "grd"))
            {
                grid.SetNextElementToolTipWithLabel("However you want this gradient to blend color values");
                grid.ComboboxEnum<FuGradientBlendMode>("Blending", (index) =>
                {
                    _currentGradient.SetBlendMode((FuGradientBlendMode)index);
                }, () => _currentGradient.BlendMode);
            }

            Vector2 mousePos = ImGui.GetMousePos();
            Vector2 startPos = ImGui.GetCursorScreenPos();
            Rect gradientRect = new Rect(startPos, new Vector2(width, height));
            Texture2D gradientTexture = _currentGradient.GetGradientTexture((int)width);

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

            // draw invisible button to prevent draw popup and handle native ImGui state on gradient image and carrets
            ImGui.SetCursorScreenPos(startPos);
            ImGui.InvisibleButton(text + "nvsbB", new Vector2(width, height + colorKeySize + 4f));

            bool isAnyKeyHovered = false;
            // Draw the color keys
            for (int i = 0; i < _currentGradient.GetKeysCount(); i++)
            {
                if (_currentGradient.GetKey(i, out FuGradientColorKey key))
                {
                    Vector2 keyPos = new Vector2(gradientRect.x + key.Time * gradientRect.width, gradientRect.yMax);
                    Rect colorKeyRect = new Rect(gradientRect.x + key.Time * gradientRect.width - colorKeySize / 2, gradientRect.yMax + 4, colorKeySize, colorKeySize);

                    // get key states
                    bool hovered = isItemHovered(colorKeyRect.position, colorKeyRect.size);
                    isAnyKeyHovered |= hovered;
                    bool active = _selectedColorKeyIndex == i;

                    // draw Line
                    drawList.AddLine(keyPos - new Vector2(0f, gradientRect.height), keyPos, ImGui.GetColorU32(FuThemeManager.GetColor(FuColors.Knob)), active ? 3f : 1f);

                    // set tooltip and start drag on mouse douse
                    if (hovered)
                    {
                        SetToolTip(text + "key" + i, string.Format("Time: {0:F2}\nColor: ({1:F2}, {2:F2}, {3:F2})", key.Time, key.Color.r, key.Color.g, key.Color.b), FuTextStyle.Default);

                        if (!isDraggingColorKey && ImGui.IsMouseDown(ImGuiMouseButton.Left))
                        {
                            isDraggingColorKey = true;
                            _selectedColorKeyIndex = i;
                        }
                    }

                    // draw carret
                    Color caretColor = FuThemeManager.GetColor(FuColors.Knob);
                    if (hovered)
                    {
                        caretColor = FuThemeManager.GetColor(FuColors.KnobHovered);
                    }
                    if (active)
                    {
                        caretColor = FuThemeManager.GetColor(FuColors.KnobActive);
                        Fugui.DrawCarret_Top(drawList, colorKeyRect.position, colorKeySize, colorKeySize, caretColor);
                    }
                    else
                    {
                        Fugui.DrawCarret_Top(drawList, colorKeyRect.position + (Vector2.one * colorKeySize * 0.25f), colorKeySize * 0.5f, colorKeySize * 0.5f, caretColor);
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
            if (!isDraggingColorKey)
            {
                if (!isAnyKeyHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    if (isItemHovered(startPos, gradientRect.size))
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
                    SetToolTip(text + "key" + _selectedColorKeyIndex, string.Format("Time: {0:F2}\nColor: ({1:F2}, {2:F2}, {3:F2})", key.Time, key.Color.r, key.Color.g, key.Color.b), FuTextStyle.Default);
                    float t = Mathf.Clamp01((mousePos.x - gradientRect.x) / gradientRect.width);
                    _currentGradient.SetKeyTime(_selectedColorKeyIndex, t);
                    edited = true;
                }
            }

            // display color picker if needed
            if (_selectedColorKeyIndex >= 0)
            {
                if (_currentGradient.GetKey(_selectedColorKeyIndex, out FuGradientColorKey key))
                {
                    Vector4 col = key.Color;
                    if (ColorPicker(text + "cp", ref col))
                    {
                        _currentGradient.SetKeyColor(_selectedColorKeyIndex, col);
                    }
                }
            }

            // set mouse cursor if hover a key
            if (isAnyKeyHovered)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            return edited;
        }
    }
}