using ImGuiNET;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Fu.Framework
{
    /// <summary>
    /// Represents the Fu Layout type.
    /// </summary>
    public partial class FuLayout
    {
        #region State
        private static Dictionary<string, Vector4> _pickersColors = new Dictionary<string, Vector4>();
        private static Dictionary<string, bool> _pickersEditedStates = new Dictionary<string, bool>();
        private static Dictionary<string, FuColorPickerPopupState> _pickerPopupStates = new Dictionary<string, FuColorPickerPopupState>();
        private static Dictionary<string, string> _pickerHexValues = new Dictionary<string, string>();
        private static Dictionary<string, bool> _pickerHexActiveStates = new Dictionary<string, bool>();
        private static readonly Vector4[] _modernColorPickerSwatches = new Vector4[]
        {
            new Vector4(0.96f, 0.26f, 0.21f, 1f),
            new Vector4(1.00f, 0.60f, 0.00f, 1f),
            new Vector4(1.00f, 0.84f, 0.00f, 1f),
            new Vector4(0.30f, 0.69f, 0.31f, 1f),
            new Vector4(0.00f, 0.74f, 0.83f, 1f),
            new Vector4(0.13f, 0.59f, 0.95f, 1f),
            new Vector4(0.40f, 0.23f, 0.72f, 1f),
            new Vector4(0.91f, 0.12f, 0.39f, 1f),
            new Vector4(0.13f, 0.13f, 0.13f, 1f),
            new Vector4(0.38f, 0.38f, 0.38f, 1f),
            new Vector4(0.78f, 0.78f, 0.78f, 1f),
            new Vector4(1.00f, 1.00f, 1.00f, 1f),
        };
        #endregion

        #region Methods
        //private static bool _colorPickerEdited = false;

        /// <summary>
        /// Displays a color picker widget that allows the user to select a color in the RGB color space.
        /// </summary>
        ///<param name="text">A unique identifier for the color picker widget.</param>
        ///<param name="color">A reference to the vector3 containing the current color value. This value will be updated if the user changes the color.</param>
        public bool ColorPicker(string text, ref Vector4 color)
        {
            // Use the custom color picker function to display the widget and get the result
            return _customColorPicker(text, true, ref color, FuFrameStyle.Default);
        }

        /// <summary>
        /// Displays a color picker widget that allows the user to select a color in the RGBA color space.
        /// </summary>
        ///<param name="text">A unique identifier for the color picker widget.</param>
        ///<param name="color">A reference to the vector4 containing the current color value. This value will be updated if the user changes the color.</param>
        public bool ColorPicker(string text, ref Vector3 color)
        {
            // Convert the vector3 color value to a vector4 value
            Vector4 col = color;
            // Use the custom color picker function to display the widget and get the result
            if (_customColorPicker(text, false, ref col, FuFrameStyle.Default))
            {
                // If the color was edited, update the vector3 value with the new color
                color = (Vector3)col;
                // Return whether the color was edited
                return true;
            }
            return false;
        }

        /// <summary>
        /// Displays a color picker widget that allows the user to select a color in the RGBA color space.
        /// </summary>
        ///<param name="text">A unique identifier for the color picker widget.</param>
        ///<param name="color">A reference to the vector4 containing the current color value. This value will be updated if the user changes the color.</param>
        ///<param name="style">The style to apply to the color picker widget.</param>
        public bool ColorPicker(string text, ref Vector4 color, FuFrameStyle style)
        {
            // Use the custom color picker function to display the widget and get the result
            return _customColorPicker(text, true, ref color, style);
        }

        /// <summary>
        /// Displays a color picker widget that allows the user to select a color in the RGB color space.
        /// </summary>
        ///<param name="text">A unique identifier for the color picker widget.</param>
        ///<param name="color">A reference to the vector3 containing the current color value. This value will be updated if the user changes the color.</param>
        ///<param name="style">The style to apply to the color picker widget.</param>
        public bool ColorPicker(string text, ref Vector3 color, FuFrameStyle style)
        {
            // Convert the vector3 color value to a vector4 value
            Vector4 col = color;
            // Use the custom color picker function to display the widget and get the result
            if (_customColorPicker(text, false, ref col, style))
            {
                // If the color was edited, update the vector3 value with the new color
                color = (Vector3)col;
                // Return whether the color was edited
                return true;
            }
            return false;
        }

        /// <summary>
        /// Draw a custom Unity-Like Color Picker
        /// </summary>
        /// <param name="text">ID / Label of the color picker</param>
        /// <param name="alpha">did the color picker must draw alpha line and support alpha</param>
        /// <param name="color">reference of the color to display and edit</param>
        /// <param name="style">the FrameStyle to apply to the right-side input</param>
        /// <returns>true if value has been edited</returns>
        protected virtual bool _customColorPicker(string text, bool alpha, ref Vector4 color, FuFrameStyle style)
        {
            beginElement(ref text, style);
            // return if item must no be draw
            if (!_drawElement)
            {
                return false;
            }

            // register color so we can ref inside the popup lambda callback
            if (!_pickersColors.ContainsKey(text))
            {
                _pickersColors.Add(text, color);
            }
            _pickersColors[text] = color;
            // register edited states so we can predict Activation / Deractivation states from lambda callback
            if (!_pickersEditedStates.ContainsKey(text))
            {
                _pickersEditedStates.Add(text, false);
            }

            float scale = Fugui.CurrentContext.Scale;
            float height = 20f * scale;
            float width = ImGui.GetContentRegionAvail().x;
            float rounding = Mathf.Min(8f * scale, height * 0.45f);
            var drawList = ImGui.GetWindowDrawList();

            Vector2 min = ImGui.GetCursorScreenPos();
            Vector2 max = min + new Vector2(width, height);
            max.x -= 2 * Fugui.CurrentContext.Scale;
            max.y -= 2 * Fugui.CurrentContext.Scale;

            if (alpha)
            {
                drawCheckerboard(drawList, min, max - min, 5f * scale, rounding);
            }
            Vector4 previewColor = color;
            previewColor.w = alpha ? Mathf.Clamp01(previewColor.w) : 1f;
            drawList.AddRectFilled(min, max, ImGui.GetColorU32(previewColor), rounding);
            drawColorPreviewAlphaStrip(drawList, min, max - min, color, alpha, rounding);

            // draw frame
            drawList.AddRect(min, max, ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.Border, 0.82f)), rounding, ImDrawFlags.RoundCornersDefault, 1f);
            // fake draw the element
            ImGui.Dummy(max - min + Vector2.one * 2f);
            _elementHoverFramedEnabled = true;
            DrawHoverFrame();
            _elementHoverFramedEnabled = false;

            // set states for this element
            setBaseElementState(text, min, max - min, true, false);
            displayToolTip(_lastItemHovered);

            // set mouse cursor
            if (_lastItemHovered && !LastItemDisabled)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            endElement(style);

            string popupID = "ColorPicker" + text;
            if (_lastItemClickedButton == FuMouseButton.Left)
            {
                FuColorPickerPopupState popupState = getColorPickerPopupState(text, color);
                popupState.OpenColor = clampColor(color);
                popupState.HasOpenColor = true;
                Fugui.OpenPopUp(popupID, () =>
                {
                    Vector4 col = _pickersColors[text];
                    if (drawModernColorPickerPopup(text, alpha, ref col))
                    {
                        if (!_pickersEditedStates[text])
                        {
                            _pickersEditedStates[text] = true;
                            _lastItemJustActivated = true;
                        }
                        _pickersColors[text] = col;
                    }
                    else if (_pickersEditedStates[text] && !ImGui.IsMouseDown(ImGuiMouseButton.Left))
                    {
                        _pickersEditedStates[text] = false;
                        _lastItemJustDeactivated = true;
                    }
                }, alpha ? new Vector2(326f, 388f) : new Vector2(300f, 358f));
            }
            Fugui.Push(ImGuiStyleVar.WindowPadding, Vector2.zero);
            Fugui.DrawPopup(popupID);
            Fugui.PopStyle();
            if (_pickersEditedStates[text])
            {
                color = _pickersColors[text];
            }

            bool edited = _pickersEditedStates[text];
            return edited;
        }

        private bool drawModernColorPickerPopup(string id, bool alpha, ref Vector4 color)
        {
            FuColorPickerPopupState state = getColorPickerPopupState(id, color);
            bool edited = false;
            float scale = Fugui.CurrentContext.Scale;
            float padding = 12f * scale;
            float headerHeight = 32f * scale;
            float pickerSize = (alpha ? 178f : 186f) * scale;
            float gap = 8f * scale;
            float barWidth = 14f * scale;
            float popupWidth = alpha ? 326f * scale : 300f * scale;
            float popupHeight = alpha ? 388f * scale : 358f * scale;

            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 popupPos = ImGui.GetCursorScreenPos();
            Vector2 popupSize = new Vector2(popupWidth, popupHeight);

            drawList.AddRectFilled(popupPos, popupPos + popupSize, ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.PopupBg, 0.98f)), Fugui.Themes.PopupRounding * scale);

            Vector2 contentPos = popupPos + new Vector2(padding, padding);
            Vector2 headerPreviewSize = new Vector2(44f * scale, 30f * scale);
            drawColorPreview(drawList, contentPos, headerPreviewSize, color, alpha, 8f * scale);

            Vector2 resetButtonSize = new Vector2(headerPreviewSize.y, headerPreviewSize.y);
            Vector2 resetButtonPos = contentPos + new Vector2(headerPreviewSize.x + gap, 0f);
            bool canReset = state.HasOpenColor && !colorsClose(color, state.OpenColor);
            if (drawColorPickerResetSwatch(id + "Reset", resetButtonPos, resetButtonSize, state.OpenColor, alpha, canReset))
            {
                color = alpha ? state.OpenColor : new Vector4(state.OpenColor.x, state.OpenColor.y, state.OpenColor.z, 1f);
                syncColorPickerState(state, color);
                _pickerHexValues[id] = formatColorHex(color, alpha);
                edited = true;
            }

            Vector2 hexPos = resetButtonPos + new Vector2(resetButtonSize.x + gap, 0f);
            if (drawHexInput(id, ref color, alpha, hexPos, popupWidth - padding * 2f - headerPreviewSize.x - resetButtonSize.x - gap * 2f))
            {
                syncColorPickerState(state, color);
                edited = true;
            }

            Vector2 svPos = contentPos + new Vector2(0f, headerHeight + gap);
            Vector2 svSize = new Vector2(pickerSize, pickerSize);
            if (drawSaturationValueArea(id + "SV", state, svPos, svSize))
            {
                color = colorFromPickerState(state, alpha);
                edited = true;
            }

            Vector2 huePos = svPos + new Vector2(pickerSize + gap, 0f);
            if (drawHueSlider(id + "Hue", state, huePos, new Vector2(barWidth, pickerSize)))
            {
                color = colorFromPickerState(state, alpha);
                edited = true;
            }

            if (alpha)
            {
                Vector2 alphaPos = huePos + new Vector2(barWidth + gap * 0.8f, 0f);
                if (drawAlphaSlider(id + "Alpha", state, alphaPos, new Vector2(barWidth, pickerSize)))
                {
                    color = colorFromPickerState(state, true);
                    edited = true;
                }
            }

            Vector2 controlsPos = svPos + new Vector2(0f, pickerSize + 14f * scale);
            float controlsWidth = popupWidth - padding * 2f;
            Vector2 sliderPos = controlsPos;
            float sliderWidth = controlsWidth;
            if (drawColorChannelSlider(id + "R", "R", ref color, 0, sliderPos, sliderWidth))
            {
                syncColorPickerState(state, color);
                edited = true;
            }
            if (drawColorChannelSlider(id + "G", "G", ref color, 1, sliderPos + new Vector2(0f, 24f * scale), sliderWidth))
            {
                syncColorPickerState(state, color);
                edited = true;
            }
            if (drawColorChannelSlider(id + "B", "B", ref color, 2, sliderPos + new Vector2(0f, 48f * scale), sliderWidth))
            {
                syncColorPickerState(state, color);
                edited = true;
            }
            if (alpha && drawColorChannelSlider(id + "A", "A", ref color, 3, sliderPos + new Vector2(0f, 72f * scale), sliderWidth))
            {
                syncColorPickerState(state, color);
                edited = true;
            }

            Vector2 swatchPos = sliderPos + new Vector2(0f, (alpha ? 88f : 66f) * scale);
            drawList.AddText(swatchPos, ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.TextDisabled, 0.82f)), "Presets");
            Vector2 swatchGridPos = swatchPos + new Vector2(0f, 22f * scale);
            if (drawColorSwatches(id, ref color, alpha, swatchGridPos, controlsWidth))
            {
                syncColorPickerState(state, color);
                edited = true;
            }

            if (edited)
            {
                state.Color = color;
                if (!_pickerHexActiveStates.TryGetValue(id, out bool hexActive) || !hexActive)
                {
                    _pickerHexValues[id] = formatColorHex(color, alpha);
                }
            }

            ImGui.SetCursorScreenPos(popupPos + popupSize - new Vector2(2f * scale, 7f * scale));
            ImGui.Dummy(Vector2.one * scale);
            return edited;
        }

        private FuColorPickerPopupState getColorPickerPopupState(string id, Vector4 color)
        {
            if (!_pickerPopupStates.TryGetValue(id, out FuColorPickerPopupState state))
            {
                state = new FuColorPickerPopupState();
                _pickerPopupStates[id] = state;
            }

            if (!state.Initialized || !colorsClose(state.Color, color))
            {
                syncColorPickerState(state, color);
            }

            if (!_pickerHexValues.ContainsKey(id))
            {
                _pickerHexValues[id] = formatColorHex(color, true);
            }

            return state;
        }

        private static void syncColorPickerState(FuColorPickerPopupState state, Vector4 color)
        {
            ImGui.ColorConvertRGBtoHSV(Mathf.Clamp01(color.x), Mathf.Clamp01(color.y), Mathf.Clamp01(color.z), out float h, out float s, out float v);
            if (!state.Initialized || s > 0.0001f)
            {
                state.Hue = h;
            }
            state.Saturation = s;
            state.Value = v;
            state.Alpha = Mathf.Clamp01(color.w);
            state.Color = clampColor(color);
            state.Initialized = true;
        }

        private static Vector4 colorFromPickerState(FuColorPickerPopupState state, bool alpha)
        {
            ImGui.ColorConvertHSVtoRGB(Mathf.Repeat(state.Hue, 1f), Mathf.Clamp01(state.Saturation), Mathf.Clamp01(state.Value), out float r, out float g, out float b);
            return new Vector4(r, g, b, alpha ? Mathf.Clamp01(state.Alpha) : 1f);
        }

        private bool drawSaturationValueArea(string id, FuColorPickerPopupState state, Vector2 pos, Vector2 size)
        {
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            float rounding = 8f * Fugui.CurrentContext.Scale;
            Vector4 hueColor = colorFromHSV(state.Hue, 1f, 1f, 1f);
            Vector2 max = pos + size;

            drawList.AddRectFilled(pos, max, ImGui.GetColorU32(hueColor), rounding);
            drawRoundedHorizontalGradient(drawList, pos, size, Vector4.one, new Vector4(1f, 1f, 1f, 0f), rounding);
            drawRoundedVerticalGradient(drawList, pos, size, Vector4.zero, new Vector4(0f, 0f, 0f, 1f), rounding);
            drawList.AddRect(pos, max, ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.Border, 0.72f)), rounding);

            ImGui.SetCursorScreenPos(pos);
            ImGui.InvisibleButton(id, size, ImGuiButtonFlags.MouseButtonLeft);
            bool active = !LastItemDisabled && ImGui.IsItemActive();
            bool hovered = !LastItemDisabled && ImGui.IsItemHovered();
            bool edited = false;
            if (active)
            {
                Vector2 mousePos = ImGui.GetMousePos();
                float saturation = Mathf.Clamp01((mousePos.x - pos.x) / size.x);
                float value = 1f - Mathf.Clamp01((mousePos.y - pos.y) / size.y);
                edited = !Mathf.Approximately(saturation, state.Saturation) || !Mathf.Approximately(value, state.Value);
                state.Saturation = saturation;
                state.Value = value;
            }
            if (hovered)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            Vector2 handlePos = pos + new Vector2(state.Saturation * size.x, (1f - state.Value) * size.y);
            drawList.AddCircleFilled(handlePos, 6f * Fugui.CurrentContext.Scale, ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.45f)), 24);
            drawList.AddCircle(handlePos, 6f * Fugui.CurrentContext.Scale, ImGui.GetColorU32(Vector4.one), 24, 1.8f * Fugui.CurrentContext.Scale);
            drawList.AddCircle(handlePos, 7.5f * Fugui.CurrentContext.Scale, ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.48f)), 24, 1.2f * Fugui.CurrentContext.Scale);
            return edited;
        }

        private bool drawHueSlider(string id, FuColorPickerPopupState state, Vector2 pos, Vector2 size)
        {
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            float rounding = size.x * 0.5f;
            drawRoundedHueGradient(drawList, pos, size, rounding);
            drawList.AddRect(pos, pos + size, ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.Border, 0.72f)), rounding);

            ImGui.SetCursorScreenPos(pos);
            ImGui.InvisibleButton(id, size, ImGuiButtonFlags.MouseButtonLeft);
            bool active = !LastItemDisabled && ImGui.IsItemActive();
            bool hovered = !LastItemDisabled && ImGui.IsItemHovered();
            bool edited = false;
            if (active)
            {
                float hue = Mathf.Clamp01((ImGui.GetMousePos().y - pos.y) / size.y);
                edited = !Mathf.Approximately(hue, state.Hue);
                state.Hue = hue;
            }
            if (hovered)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            drawVerticalHandle(drawList, pos, size, state.Hue);
            return edited;
        }

        private bool drawAlphaSlider(string id, FuColorPickerPopupState state, Vector2 pos, Vector2 size)
        {
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            drawCheckerboard(drawList, pos, size, 4f * Fugui.CurrentContext.Scale, size.x * 0.5f);
            Vector4 opaque = colorFromPickerState(state, true);
            opaque.w = 1f;
            Vector4 transparent = opaque;
            transparent.w = 0f;
            drawRoundedVerticalGradient(drawList, pos, size, opaque, transparent, size.x * 0.5f);
            drawList.AddRect(pos, pos + size, ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.Border, 0.72f)), size.x * 0.5f);

            ImGui.SetCursorScreenPos(pos);
            ImGui.InvisibleButton(id, size, ImGuiButtonFlags.MouseButtonLeft);
            bool active = !LastItemDisabled && ImGui.IsItemActive();
            bool hovered = !LastItemDisabled && ImGui.IsItemHovered();
            bool edited = false;
            if (active)
            {
                float alpha = 1f - Mathf.Clamp01((ImGui.GetMousePos().y - pos.y) / size.y);
                edited = !Mathf.Approximately(alpha, state.Alpha);
                state.Alpha = alpha;
            }
            if (hovered)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            drawVerticalHandle(drawList, pos, size, 1f - state.Alpha);
            return edited;
        }

        private bool drawHexInput(string id, ref Vector4 color, bool alpha, Vector2 pos, float width)
        {
            float scale = Fugui.CurrentContext.Scale;
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            string hexID = id + "HexInput";
            bool wasActive = _pickerHexActiveStates.TryGetValue(id, out bool activeState) && activeState;
            if (!_pickerHexValues.ContainsKey(id) || !wasActive)
            {
                _pickerHexValues[id] = formatColorHex(color, alpha);
            }

            drawList.AddText(pos + new Vector2(0f, 7f * scale), ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.TextDisabled, 0.86f)), "HEX");

            Vector2 inputPos = pos + new Vector2(42f * scale, 0f);
            float inputWidth = width - 42f * scale;
            string hex = _pickerHexValues[id];
            ImGui.SetCursorScreenPos(inputPos);
            ImGui.SetNextItemWidth(inputWidth);
            Fugui.Push(ImGuiStyleVar.FrameRounding, 8f * scale);
            Fugui.Push(ImGuiStyleVar.FramePadding, new Vector2(8f * scale, 5f * scale));
            Fugui.Push(ImGuiCol.FrameBg, Fugui.Themes.GetColor(FuColors.Header, 0.54f));
            Fugui.Push(ImGuiCol.FrameBgHovered, Fugui.Themes.GetColor(FuColors.HeaderHovered, 0.72f));
            Fugui.Push(ImGuiCol.FrameBgActive, Fugui.Themes.GetColor(FuColors.HeaderActive, 0.86f));
            bool edited = ImGui.InputTextWithHint("##" + hexID, alpha ? "RRGGBBAA" : "RRGGBB", ref hex, alpha ? 9u : 7u, ImGuiInputTextFlags.CharsHexadecimal | ImGuiInputTextFlags.CharsUppercase | ImGuiInputTextFlags.CharsNoBlank);
            bool isActive = ImGui.IsItemActive();
            Fugui.PopColor(3);
            Fugui.PopStyle(2);

            _pickerHexActiveStates[id] = isActive;
            _pickerHexValues[id] = hex;
            if (edited && tryParseHexColor(hex, alpha, color.w, out Vector4 parsedColor))
            {
                color = parsedColor;
                return true;
            }
            return false;
        }

        private bool drawColorChannelSlider(string id, string label, ref Vector4 color, int channel, Vector2 pos, float width)
        {
            float scale = Fugui.CurrentContext.Scale;
            float rowHeight = 18f * scale;
            float labelWidth = 18f * scale;
            float valueWidth = 34f * scale;
            float trackWidth = width - labelWidth - valueWidth - 12f * scale;
            Vector2 trackPos = pos + new Vector2(labelWidth + 6f * scale, 3f * scale);
            Vector2 trackSize = new Vector2(trackWidth, 8f * scale);
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            drawList.AddText(pos + new Vector2(0f, 1f * scale), ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.TextDisabled, 0.86f)), label);
            float value = getColorChannel(color, channel);
            Vector4 minColor = color;
            Vector4 maxColor = color;
            setColorChannel(ref minColor, channel, 0f);
            setColorChannel(ref maxColor, channel, 1f);
            if (channel != 3)
            {
                minColor.w = 1f;
                maxColor.w = 1f;
            }

            if (channel == 3)
            {
                drawCheckerboard(drawList, trackPos, trackSize, 4f * scale, trackSize.y * 0.5f);
            }

            bool edited = drawHorizontalColorSlider(id, ref value, trackPos, trackSize, minColor, maxColor);
            if (edited)
            {
                setColorChannel(ref color, channel, value);
            }

            string valueText = Mathf.RoundToInt(Mathf.Clamp01(getColorChannel(color, channel)) * 255f).ToString();
            Vector2 valueSize = ImGui.CalcTextSize(valueText);
            Vector2 valuePos = pos + new Vector2(width - valueSize.x, rowHeight * 0.5f - valueSize.y * 0.5f);
            drawList.AddText(valuePos, ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.Text, 0.92f)), valueText);
            return edited;
        }

        private bool drawHorizontalColorSlider(string id, ref float value, Vector2 pos, Vector2 size, Vector4 minColor, Vector4 maxColor)
        {
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            float rounding = size.y * 0.5f;
            drawRoundedHorizontalGradient(drawList, pos, size, minColor, maxColor, rounding);
            drawList.AddRect(pos, pos + size, ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.Border, 0.55f)), rounding);

            ImGui.SetCursorScreenPos(pos - new Vector2(0f, 5f * Fugui.CurrentContext.Scale));
            ImGui.InvisibleButton(id, new Vector2(size.x, size.y + 10f * Fugui.CurrentContext.Scale), ImGuiButtonFlags.MouseButtonLeft);
            bool active = !LastItemDisabled && ImGui.IsItemActive();
            bool hovered = !LastItemDisabled && ImGui.IsItemHovered();
            bool edited = false;
            if (active)
            {
                float newValue = Mathf.Clamp01((ImGui.GetMousePos().x - pos.x) / size.x);
                edited = !Mathf.Approximately(newValue, value);
                value = newValue;
            }
            if (hovered)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            float x = pos.x + value * size.x;
            drawList.AddCircleFilled(new Vector2(x, pos.y + size.y * 0.5f), 5.5f * Fugui.CurrentContext.Scale, ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.46f)), 20);
            drawList.AddCircle(new Vector2(x, pos.y + size.y * 0.5f), 5.5f * Fugui.CurrentContext.Scale, ImGui.GetColorU32(Vector4.one), 20, 1.5f * Fugui.CurrentContext.Scale);
            return edited;
        }

        private bool drawColorSwatches(string id, ref Vector4 color, bool alpha, Vector2 pos, float width)
        {
            float scale = Fugui.CurrentContext.Scale;
            float size = 18f * scale;
            float gap = (width - size * _modernColorPickerSwatches.Length) / (_modernColorPickerSwatches.Length - 1);
            bool edited = false;
            for (int i = 0; i < _modernColorPickerSwatches.Length; i++)
            {
                Vector2 swatchPos = pos + new Vector2(i * (size + gap), 0f);
                Vector4 swatchColor = _modernColorPickerSwatches[i];
                if (!alpha)
                {
                    swatchColor.w = 1f;
                }
                if (drawColorSwatch(id + "Swatch" + i, swatchColor, swatchPos, new Vector2(size, size), colorsCloseRgb(color, swatchColor)))
                {
                    color = new Vector4(swatchColor.x, swatchColor.y, swatchColor.z, alpha ? color.w : 1f);
                    edited = true;
                }
            }
            return edited;
        }

        private bool drawColorSwatch(string id, Vector4 color, Vector2 pos, Vector2 size, bool selected)
        {
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            float rounding = Mathf.Min(6f * Fugui.CurrentContext.Scale, size.y * 0.45f);
            drawCheckerboard(drawList, pos, size, 4f * Fugui.CurrentContext.Scale, rounding);
            drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(color), rounding);

            ImGui.SetCursorScreenPos(pos);
            bool clicked = !LastItemDisabled && ImGui.InvisibleButton(id, size, ImGuiButtonFlags.MouseButtonLeft);
            bool hovered = !LastItemDisabled && ImGui.IsItemHovered();
            if (hovered)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }
            drawList.AddRect(pos, pos + size, ImGui.GetColorU32(Fugui.Themes.GetColor(selected ? FuColors.Highlight : FuColors.Border, selected ? 1f : 0.72f)), rounding, ImDrawFlags.RoundCornersDefault, selected ? 2f * Fugui.CurrentContext.Scale : 1f);
            return clicked;
        }

        private bool drawColorPickerResetSwatch(string id, Vector2 pos, Vector2 size, Vector4 color, bool alpha, bool enabled)
        {
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            float scale = Fugui.CurrentContext.Scale;
            float rounding = Mathf.Min(8f * scale, size.y * 0.45f);
            ImGui.SetCursorScreenPos(pos);
            ImGui.InvisibleButton(id, size, ImGuiButtonFlags.MouseButtonLeft);
            bool hovered = enabled && !LastItemDisabled && ImGui.IsItemHovered();
            bool clicked = enabled && !LastItemDisabled && ImGui.IsItemClicked(ImGuiMouseButton.Left);

            if (alpha)
            {
                drawCheckerboard(drawList, pos, size, 4f * scale, rounding);
            }
            Vector4 preview = color;
            if (!alpha)
            {
                preview.w = 1f;
            }
            drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(preview), rounding);

            Vector4 border = hovered
                ? Fugui.Themes.GetColor(FuColors.Highlight, 0.95f)
                : Fugui.Themes.GetColor(FuColors.Border, enabled ? 0.74f : 0.42f);
            drawList.AddRect(pos, pos + size, ImGui.GetColorU32(border), rounding, ImDrawFlags.RoundCornersDefault, hovered ? 1.6f * scale : 1f);

            if (hovered)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }
            return clicked;
        }

        private static void drawColorPreview(ImDrawListPtr drawList, Vector2 pos, Vector2 size, Vector4 color, bool alpha, float rounding)
        {
            if (alpha)
            {
                drawCheckerboard(drawList, pos, size, 4f * Fugui.CurrentContext.Scale, rounding);
            }
            Vector4 preview = color;
            if (!alpha)
            {
                preview.w = 1f;
            }
            drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(preview), rounding);
            drawList.AddRect(pos, pos + size, ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.Border, 0.76f)), rounding);
        }

        private static void drawColorPreviewAlphaStrip(ImDrawListPtr drawList, Vector2 pos, Vector2 size, Vector4 color, bool alpha, float rounding)
        {
            if (!alpha)
            {
                return;
            }

            float scale = Fugui.CurrentContext.Scale;
            float stripHeight = Mathf.Min(4f * scale, size.y * 0.32f);
            Vector2 stripPos = pos + new Vector2(0f, size.y - stripHeight);
            Vector2 stripSize = new Vector2(size.x, stripHeight);

            drawCheckerboardClipped(drawList, pos, size, stripPos, stripSize, 4f * scale, rounding);
            Vector4 opaque = Vector4.one;
            float alphaWidth = Mathf.Clamp01(color.w) * stripSize.x;
            if (alphaWidth > 0f)
            {
                drawRoundedClippedRectFilled(drawList, pos, size, stripPos, stripPos + new Vector2(alphaWidth, stripHeight), ImGui.GetColorU32(opaque), rounding);
            }
            drawList.AddLine(stripPos, stripPos + new Vector2(stripSize.x, 0f), ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.28f)), Mathf.Max(1f, scale));
        }

        private static void drawVerticalHandle(ImDrawListPtr drawList, Vector2 pos, Vector2 size, float t)
        {
            float scale = Fugui.CurrentContext.Scale;
            float y = pos.y + Mathf.Clamp01(t) * size.y;
            Vector2 left = new Vector2(pos.x - 3f * scale, y);
            Vector2 right = new Vector2(pos.x + size.x + 3f * scale, y);
            drawList.AddLine(left, right, ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.55f)), 4f * scale);
            drawList.AddLine(left, right, ImGui.GetColorU32(Vector4.one), 2f * scale);
        }

        private static void drawCheckerboard(ImDrawListPtr drawList, Vector2 pos, Vector2 size, float tileSize, float rounding)
        {
            rounding = clampRounding(size, rounding);
            uint dark = ImGui.GetColorU32(new Vector4(0.24f, 0.24f, 0.24f, 1f));
            uint light = ImGui.GetColorU32(new Vector4(0.46f, 0.46f, 0.46f, 1f));
            drawList.AddRectFilled(pos, pos + size, light, rounding);
            int columns = Mathf.CeilToInt(size.x / tileSize);
            int rows = Mathf.CeilToInt(size.y / tileSize);
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    if (((x + y) & 1) == 0)
                    {
                        Vector2 tileMin = pos + new Vector2(x * tileSize, y * tileSize);
                        Vector2 tileMax = new Vector2(Mathf.Min(tileMin.x + tileSize, pos.x + size.x), Mathf.Min(tileMin.y + tileSize, pos.y + size.y));
                        drawRoundedClippedRectFilled(drawList, pos, size, tileMin, tileMax, dark, rounding);
                    }
                }
            }
        }

        private static void drawCheckerboardClipped(ImDrawListPtr drawList, Vector2 shapePos, Vector2 shapeSize, Vector2 pos, Vector2 size, float tileSize, float rounding)
        {
            rounding = clampRounding(shapeSize, rounding);
            uint dark = ImGui.GetColorU32(new Vector4(0.24f, 0.24f, 0.24f, 1f));
            uint light = ImGui.GetColorU32(new Vector4(0.46f, 0.46f, 0.46f, 1f));
            drawRoundedClippedRectFilled(drawList, shapePos, shapeSize, pos, pos + size, light, rounding);
            int columns = Mathf.CeilToInt(size.x / tileSize);
            int rows = Mathf.CeilToInt(size.y / tileSize);
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    if (((x + y) & 1) == 0)
                    {
                        Vector2 tileMin = pos + new Vector2(x * tileSize, y * tileSize);
                        Vector2 tileMax = new Vector2(Mathf.Min(tileMin.x + tileSize, pos.x + size.x), Mathf.Min(tileMin.y + tileSize, pos.y + size.y));
                        drawRoundedClippedRectFilled(drawList, shapePos, shapeSize, tileMin, tileMax, dark, rounding);
                    }
                }
            }
        }

        private static void drawRoundedHorizontalGradient(ImDrawListPtr drawList, Vector2 pos, Vector2 size, Vector4 leftColor, Vector4 rightColor, float rounding)
        {
            rounding = clampRounding(size, rounding);
            int steps = Mathf.Max(1, Mathf.CeilToInt(size.y / Mathf.Max(1f, Fugui.CurrentContext.Scale)));
            float step = size.y / steps;
            for (int i = 0; i < steps; i++)
            {
                float localY0 = i * step;
                float localY1 = (i + 1) * step;
                float inset = roundedHorizontalInset(size, rounding, (localY0 + localY1) * 0.5f);
                float x0 = pos.x + inset;
                float x1 = pos.x + size.x - inset;
                if (x1 <= x0)
                {
                    continue;
                }

                float leftT = size.x <= 0f ? 0f : inset / size.x;
                float rightT = size.x <= 0f ? 1f : 1f - inset / size.x;
                Vector4 stripLeft = lerpColor(leftColor, rightColor, leftT);
                Vector4 stripRight = lerpColor(leftColor, rightColor, rightT);
                drawList.AddRectFilledMultiColor(
                    new Vector2(x0, pos.y + localY0),
                    new Vector2(x1, Mathf.Min(pos.y + localY1 + 0.5f, pos.y + size.y)),
                    ImGui.GetColorU32(stripLeft),
                    ImGui.GetColorU32(stripRight),
                    ImGui.GetColorU32(stripRight),
                    ImGui.GetColorU32(stripLeft));
            }
        }

        private static void drawRoundedVerticalGradient(ImDrawListPtr drawList, Vector2 pos, Vector2 size, Vector4 topColor, Vector4 bottomColor, float rounding)
        {
            rounding = clampRounding(size, rounding);
            int steps = Mathf.Max(1, Mathf.CeilToInt(size.y / Mathf.Max(1f, Fugui.CurrentContext.Scale)));
            float step = size.y / steps;
            for (int i = 0; i < steps; i++)
            {
                float localY0 = i * step;
                float localY1 = (i + 1) * step;
                float inset = roundedHorizontalInset(size, rounding, (localY0 + localY1) * 0.5f);
                float x0 = pos.x + inset;
                float x1 = pos.x + size.x - inset;
                if (x1 <= x0)
                {
                    continue;
                }

                Vector4 stripTop = lerpColor(topColor, bottomColor, size.y <= 0f ? 0f : localY0 / size.y);
                Vector4 stripBottom = lerpColor(topColor, bottomColor, size.y <= 0f ? 1f : localY1 / size.y);
                drawList.AddRectFilledMultiColor(
                    new Vector2(x0, pos.y + localY0),
                    new Vector2(x1, Mathf.Min(pos.y + localY1 + 0.5f, pos.y + size.y)),
                    ImGui.GetColorU32(stripTop),
                    ImGui.GetColorU32(stripTop),
                    ImGui.GetColorU32(stripBottom),
                    ImGui.GetColorU32(stripBottom));
            }
        }

        private static void drawRoundedHueGradient(ImDrawListPtr drawList, Vector2 pos, Vector2 size, float rounding)
        {
            rounding = clampRounding(size, rounding);
            int steps = Mathf.Max(24, Mathf.CeilToInt(size.y / Mathf.Max(1f, Fugui.CurrentContext.Scale)));
            float step = size.y / steps;
            for (int i = 0; i < steps; i++)
            {
                float localY0 = i * step;
                float localY1 = (i + 1) * step;
                float inset = roundedHorizontalInset(size, rounding, (localY0 + localY1) * 0.5f);
                float x0 = pos.x + inset;
                float x1 = pos.x + size.x - inset;
                if (x1 <= x0)
                {
                    continue;
                }

                Vector4 stripTop = colorFromHSV(size.y <= 0f ? 0f : localY0 / size.y, 1f, 1f, 1f);
                Vector4 stripBottom = colorFromHSV(size.y <= 0f ? 1f : localY1 / size.y, 1f, 1f, 1f);
                drawList.AddRectFilledMultiColor(
                    new Vector2(x0, pos.y + localY0),
                    new Vector2(x1, Mathf.Min(pos.y + localY1 + 0.5f, pos.y + size.y)),
                    ImGui.GetColorU32(stripTop),
                    ImGui.GetColorU32(stripTop),
                    ImGui.GetColorU32(stripBottom),
                    ImGui.GetColorU32(stripBottom));
            }
        }

        private static void drawRoundedClippedRectFilled(ImDrawListPtr drawList, Vector2 shapePos, Vector2 shapeSize, Vector2 rectMin, Vector2 rectMax, uint color, float rounding)
        {
            rounding = clampRounding(shapeSize, rounding);
            float yStart = Mathf.Max(rectMin.y, shapePos.y);
            float yEnd = Mathf.Min(rectMax.y, shapePos.y + shapeSize.y);
            int steps = Mathf.Max(1, Mathf.CeilToInt((yEnd - yStart) / Mathf.Max(1f, Fugui.CurrentContext.Scale)));
            float step = (yEnd - yStart) / steps;
            for (int i = 0; i < steps; i++)
            {
                float y0 = yStart + i * step;
                float y1 = yStart + (i + 1) * step + 0.5f;
                float localMidY = ((y0 + y1) * 0.5f) - shapePos.y;
                float inset = roundedHorizontalInset(shapeSize, rounding, localMidY);
                float x0 = Mathf.Max(rectMin.x, shapePos.x + inset);
                float x1 = Mathf.Min(rectMax.x, shapePos.x + shapeSize.x - inset);
                if (x1 > x0)
                {
                    drawList.AddRectFilled(new Vector2(x0, y0), new Vector2(x1, Mathf.Min(y1, yEnd)), color);
                }
            }
        }

        private static float roundedHorizontalInset(Vector2 size, float rounding, float localY)
        {
            rounding = clampRounding(size, rounding);
            if (rounding <= 0f)
            {
                return 0f;
            }

            localY = Mathf.Clamp(localY, 0f, size.y);
            if (localY < rounding)
            {
                float dy = rounding - localY;
                return rounding - Mathf.Sqrt(Mathf.Max(0f, rounding * rounding - dy * dy));
            }
            if (localY > size.y - rounding)
            {
                float dy = localY - (size.y - rounding);
                return rounding - Mathf.Sqrt(Mathf.Max(0f, rounding * rounding - dy * dy));
            }
            return 0f;
        }

        private static float clampRounding(Vector2 size, float rounding)
        {
            return Mathf.Clamp(rounding, 0f, Mathf.Min(size.x, size.y) * 0.5f);
        }

        private static Vector4 lerpColor(Vector4 from, Vector4 to, float t)
        {
            t = Mathf.Clamp01(t);
            return new Vector4(
                Mathf.Lerp(from.x, to.x, t),
                Mathf.Lerp(from.y, to.y, t),
                Mathf.Lerp(from.z, to.z, t),
                Mathf.Lerp(from.w, to.w, t));
        }

        private static Vector4 colorFromHSV(float h, float s, float v, float a)
        {
            ImGui.ColorConvertHSVtoRGB(Mathf.Repeat(h, 1f), Mathf.Clamp01(s), Mathf.Clamp01(v), out float r, out float g, out float b);
            return new Vector4(r, g, b, a);
        }

        private static Vector4 clampColor(Vector4 color)
        {
            return new Vector4(Mathf.Clamp01(color.x), Mathf.Clamp01(color.y), Mathf.Clamp01(color.z), Mathf.Clamp01(color.w));
        }

        private static bool colorsClose(Vector4 a, Vector4 b)
        {
            return Mathf.Abs(a.x - b.x) < 0.001f &&
                   Mathf.Abs(a.y - b.y) < 0.001f &&
                   Mathf.Abs(a.z - b.z) < 0.001f &&
                   Mathf.Abs(a.w - b.w) < 0.001f;
        }

        private static bool colorsCloseRgb(Vector4 a, Vector4 b)
        {
            return Mathf.Abs(a.x - b.x) < 0.001f &&
                   Mathf.Abs(a.y - b.y) < 0.001f &&
                   Mathf.Abs(a.z - b.z) < 0.001f;
        }

        private static float getColorChannel(Vector4 color, int channel)
        {
            switch (channel)
            {
                case 0: return color.x;
                case 1: return color.y;
                case 2: return color.z;
                default: return color.w;
            }
        }

        private static void setColorChannel(ref Vector4 color, int channel, float value)
        {
            value = Mathf.Clamp01(value);
            switch (channel)
            {
                case 0:
                    color.x = value;
                    break;
                case 1:
                    color.y = value;
                    break;
                case 2:
                    color.z = value;
                    break;
                default:
                    color.w = value;
                    break;
            }
        }

        private static string formatColorHex(Vector4 color, bool alpha)
        {
            color = clampColor(color);
            int r = Mathf.RoundToInt(color.x * 255f);
            int g = Mathf.RoundToInt(color.y * 255f);
            int b = Mathf.RoundToInt(color.z * 255f);
            int a = Mathf.RoundToInt(color.w * 255f);
            return alpha ? string.Format("{0:X2}{1:X2}{2:X2}{3:X2}", r, g, b, a) : string.Format("{0:X2}{1:X2}{2:X2}", r, g, b);
        }

        private static bool tryParseHexColor(string hex, bool alpha, float fallbackAlpha, out Vector4 color)
        {
            color = Vector4.zero;
            if (string.IsNullOrWhiteSpace(hex))
            {
                return false;
            }

            hex = hex.Trim().TrimStart('#');
            if (hex.Length == 3)
            {
                hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);
            }
            else if (hex.Length == 4)
            {
                hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2], hex[3], hex[3]);
            }

            if (hex.Length != 6 && hex.Length != 8)
            {
                return false;
            }

            if (!uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint value))
            {
                return false;
            }

            float r;
            float g;
            float b;
            float a = alpha ? Mathf.Clamp01(fallbackAlpha) : 1f;
            if (hex.Length == 8)
            {
                r = ((value >> 24) & 0xFF) / 255f;
                g = ((value >> 16) & 0xFF) / 255f;
                b = ((value >> 8) & 0xFF) / 255f;
                a = alpha ? (value & 0xFF) / 255f : 1f;
            }
            else
            {
                r = ((value >> 16) & 0xFF) / 255f;
                g = ((value >> 8) & 0xFF) / 255f;
                b = (value & 0xFF) / 255f;
            }

            color = new Vector4(r, g, b, a);
            return true;
        }

        private class FuColorPickerPopupState
        {
            public bool Initialized;
            public float Hue;
            public float Saturation;
            public float Value;
            public float Alpha;
            public Vector4 Color;
            public bool HasOpenColor;
            public Vector4 OpenColor;
        }
        #endregion
    }
}
