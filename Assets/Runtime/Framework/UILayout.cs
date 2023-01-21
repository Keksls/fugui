using Fugui.Core;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Fugui.Framework
{
    /// <summary>
    /// Represents the base class for creating user interface layouts.
    /// </summary>
    public class UILayout : IDisposable
    {
        #region Variables
        // The current pop-up window ID.
        public static string CurrentPopUpWindowID { get; private set; } = null;
        // The current pop-up ID.
        public static string CurrentPopUpID { get; private set; } = null;
        // The current pop-up Rect.
        public static Rect CurrentPopUpRect { get; private set; } = default;
        // A flag indicating whether the layout is inside a pop-up.
        public static bool IsInsidePopUp { get; private set; } = false;
        // A flag indicating whether the element is hover framed.
        private bool _elementHoverFramed = false;
        // A flag indicating whether the next element should be disabled.
        protected bool _nextIsDisabled;
        // An array of strings representing the current tool tips.
        protected string[] _currentToolTips = null;
        // An integer representing the current tool tips index.
        protected int _currentToolTipsIndex = 0;
        // whatever tooltip must be display hover Labels
        protected bool _currentToolTipsOnLabels = false;
        protected bool _animationEnabled = true;
        #endregion

        #region Elements Data
        // A set of strings representing the dragging sliders.
        private static HashSet<string> _draggingSliders = new HashSet<string>();
        // A dictionary of strings representing the drag string formats.
        private static Dictionary<string, string> _dragStringFormats = new Dictionary<string, string>();
        // A dictionary of integers representing the combo selected indices.
        private static Dictionary<string, int> _comboSelectedIndices = new Dictionary<string, int>();
        // A dictionary that store displaying toggle data.
        private static Dictionary<string, UIElementAnimationData> _uiElementAnimationDatas = new Dictionary<string, UIElementAnimationData>();
        // A dictionary that store displaying toggle data.
        private static Dictionary<string, int> _buttonsGroupIndex = new Dictionary<string, int>();
        // A dictionary of strings representing the current PathFields string value.
        private static Dictionary<string, string> _pathFieldValues = new Dictionary<string, string>();
        #endregion

        #region Layout
        /// <summary>
        /// Disposes this object.
        /// </summary>
        public virtual void Dispose()
        {
        }

        /// <summary>
        /// Disables the next element in this layout.
        /// </summary>
        public void DisableNextElement()
        {
            _nextIsDisabled = true;
        }

        /// <summary>
        /// Begins an element in this layout with the specified style.
        /// </summary>
        /// <param name="style">The style to use for this element.</param>
        protected virtual string beginElement(string elementID, IUIElementStyle style = null)
        {
            style?.Push(!_nextIsDisabled);
            return elementID + "##" + (UIWindow.CurrentDrawingWindow?.ID ?? "");
        }

        /// <summary>
        /// Ends an element in this layout with the specified style.
        /// </summary>
        /// <param name="style">The style to use for this element.</param>
        protected virtual void endElement(IUIElementStyle style = null)
        {
            style?.Pop();
            drawHoverFrame();
            _nextIsDisabled = false;
            _elementHoverFramed = false;
        }

        /// <summary>
        /// Draws a hover frame around the current element if needed.
        /// </summary>
        private void drawHoverFrame()
        {
            if (_elementHoverFramed && !_nextIsDisabled)
            {
                if (ImGui.IsItemFocused())
                {
                    ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.GetColorU32(ThemeManager.GetColor(ImGuiCustomCol.FrameSelectedFeedback)), ImGui.GetStyle().FrameRounding);
                }
                else if (ImGui.IsItemHovered())
                {
                    ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.GetColorU32(ThemeManager.GetColor(ImGuiCustomCol.FrameHoverFeedback)), ImGui.GetStyle().FrameRounding);
                }
            }
        }

        /// <summary>
        ///  From this point, animations in this layout are enabled
        /// </summary>
        public void EnableAnimationsFromNow()
        {
            _animationEnabled = true;
        }

        /// <summary>
        ///  From this point, animations in this layout are disabled
        /// </summary>
        public void DisableAnimationsFromNow()
        {
            _animationEnabled = false;
        }
        #endregion

        #region Generic UI Elements
        #region Button
        /// <summary>
        /// Renders a button with the given text. The button will have the default size and style.
        /// </summary>
        /// <param name="text">The text to display on the button.</param>
        /// <returns>True if the button was clicked, false otherwise.</returns>
        public bool Button(string text)
        {
            return Button(text, UIButtonStyle.FullSize, UIButtonStyle.Default);
        }

        /// <summary>
        /// Renders a button with the given text and size. The button will have the default style.
        /// </summary>
        /// <param name="text">The text to display on the button.</param>
        /// <param name="size">The size of the button. If either dimension is set to -1, it will be set to the available content region size in that direction.</param>
        /// <returns>True if the button was clicked, false otherwise.</returns>
        public bool Button(string text, Vector2 size)
        {
            return Button(text, size, UIButtonStyle.Default);
        }

        /// <summary>
        /// Renders a button with the given text and style. The button will have the default size.
        /// </summary>
        /// <param name="text">The text to display on the button.</param>
        /// <param name="style">The style to apply to the button.</param>
        /// <returns>True if the button was clicked, false otherwise.</returns>
        public bool Button(string text, UIButtonStyle style)
        {
            return Button(text, UIButtonStyle.FullSize, style);
        }

        /// <summary>
        /// Renders a button with the given text, size, and style.
        /// </summary>
        /// <param name="text">The text to display on the button.</param>
        /// <param name="size">The size of the button. If either dimension is set to -1, it will be set to the available content region size in that direction.</param>
        /// <param name="style">The style to apply to the button.</param>
        /// <returns>True if the button was clicked, false otherwise.</returns>
        public virtual bool Button(string text, Vector2 size, UIButtonStyle style)
        {
            text = beginElement(text, style); // apply style and check if the element should be disabled
            if (size.x == -1)
            {
                size.x = ImGui.GetContentRegionAvail().x; // set size to the available content region size if it is set to -1
            }
            if (size.y == -1)
            {
                size.y = 0; // set size to 0 if it is set to -1
            }
            bool clicked = ImGui.Button(text, size) & !_nextIsDisabled; // render the button and return true if it was clicked, false otherwise
            displayToolTip(); // display the tooltip if necessary
            endElement(style); // remove the style and draw the hover frame if necessary
            return clicked;
        }
        #endregion

        #region Labels
        /// <summary>
        /// Displays a text string.
        /// </summary>
        /// <param name="text">The string to display.</param>
        public void Text(string text)
        {
            Text(text, UITextStyle.Default);
        }

        /// <summary>
        /// Displays a text string with the specified style.
        /// </summary>
        /// <param name="text">The string to display.</param>
        /// <param name="style">The style to apply to the text.</param>
        public virtual void Text(string text, UITextStyle style)
        {
            beginElement("", style); //apply the style to the element
            // verticaly align text to frame padding
            ImGui.AlignTextToFramePadding();
            ImGui.Text(text); //display the text
            if (_currentToolTipsOnLabels) //if the current tooltips should be displayed on labels
            {
                displayToolTip(); //display the current tooltips
            }
            endElement(style); //remove the style from the element
        }

        /// <summary>
        /// A lot more perf cost than just display a text, use it only if truely needed.
        /// Display a RichText that take some tags. The text automaticaly line break is cursor pass throw container
        /// -<br/> or </br> to break line
        /// -<b></b> to bold
        /// -<size=XX></size> to set text size
        /// -<color=#HEX / rgb(r, g, b) / colorName></color> to set text color
        /// </summary>
        /// <param name="text">Text to display</param>
        public void SmartText(string text)
        {
            SmartText(text, UITextStyle.Default);
        }

        /// <summary>
        /// A lot more perf cost than just display a text, use it only if truely needed.
        /// Display a RichText that take some tags. The text automaticaly line break is cursor pass throw container
        /// - <br/>, </br> or \n to break line
        /// -<b></b> to bold
        /// -<size=XX></size> to set text size
        /// -<color=#HEX / rgb(r, g, b) / colorName></color> to set text color
        /// </summary>
        /// <param name="text">Text to display</param>
        /// <param name="style">default style to use</param>
        public virtual void SmartText(string text, UITextStyle style)
        {
            beginElement("", style);
            _customText(text);
            if (_currentToolTipsOnLabels)
            {
                displayToolTip();
            }
            endElement(style);
        }

        /// <summary>
        /// Draw a text that auto brak line and accept <b></b> <color></color> and <size></size> tags
        /// </summary>
        /// <param name="text">text to draw</param>
        protected void _customText(string text)
        {
            int size = 14;
            Vector4 color = ThemeManager.GetColor(ImGuiCol.Text);
            ImGui.SameLine();
            float startCursorX = ImGui.GetCursorPosX();
            bool bold = false;
            int nbFontPush = 0;
            int nbColorPush = 0;
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < text.Length; i++)
            {
                // the current char is start of a tag
                if (text[i] == '<')
                {
                    // Parse tag
                    int j = i + 1;
                    while (j < text.Length && text[j] != '>' && text[j] != '<')
                    {
                        j++;
                    }

                    // we find a tag
                    if (j < text.Length && text[j] == '>')
                    {
                        string tag = text.Substring(i + 1, j - i - 1);
                        // display preTag text if needed
                        displayText();

                        // BOLD tag
                        if (tag == "b")
                        {
                            FuGui.PushFont(size, FontType.Bold);
                            nbFontPush++;
                        }
                        else if (tag == "/b")
                        {
                            if (nbFontPush > 0)
                            {
                                nbFontPush--;
                                FuGui.PopFont();
                            }
                        }

                        // LINE BREAK tag
                        else if (tag == "br/" || tag == "/br")
                        {
                            float y = ImGui.GetCursorPosY();
                            ImGui.Dummy(Vector2.zero);
                            ImGui.SetCursorPosX(startCursorX);
                            ImGui.SetCursorPosY(y + ImGui.CalcTextSize("pV").y + 2f); // use Maj and underLine letter so we calc max line height
                        }

                        // SIZE tag
                        else if (tag == "/size")
                        {
                            if (nbFontPush > 0)
                            {
                                nbFontPush--;
                                FuGui.PopFont();
                            }
                        }
                        else if (tag.StartsWith("size="))
                        {
                            size = int.Parse(tag.Substring("size=".Length));
                            if (bold)
                            {
                                FuGui.PushFont(size, FontType.Bold);
                                nbFontPush++;
                            }
                            else
                            {
                                FuGui.PushFont(size, FontType.Regular);
                                nbFontPush++;
                            }
                        }

                        // COLOR tag
                        else if (tag == "/color")
                        {
                            if (nbColorPush > 0)
                            {
                                FuGui.PopColor();
                                nbColorPush--;
                            }
                        }
                        else if (tag.StartsWith("color="))
                        {
                            if (ColorUtility.TryParseHtmlString(tag.Substring("color=".Length), out Color col))
                            {
                                color = col;
                            }
                            FuGui.Push(ImGuiCol.Text, color);
                            nbColorPush++;
                        }
                    }
                    else
                    {
                        string tag = text.Substring(i, j - i);
                        sb.Append(tag);
                        displayText();
                    }
                    i = j;
                }
                // the current char is just regular char
                else
                {
                    sb.Append(text[i]);
                    // we can jump line of necessarry if char is a space or a score
                    if (text[i] == ' ' || text[i] == '-')
                    {
                        displayText();
                    }
                }
            }
            // end of parsing, display remaning text
            displayText();


            // internal method that display text
            void displayText()
            {
                if (sb.Length > 0) // display only if needed
                {
                    // get txt width
                    float txtWidth = ImGui.CalcTextSize(sb.ToString() + " ").x;
                    // assumle we always are on same line (simpler that derterminate it at this point)
                    ImGui.SameLine();
                    // check whatever wee pass throw right max
                    if (startCursorX - ImGui.GetCursorPosX() < txtWidth + 4f)
                    {
                        // if true, line break
                        ImGui.Dummy(Vector2.zero);
                    }
                    else
                    {
                        // if not, place X cusror to minus 3, because it's a hard ImGui Text padding
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 3f);
                    }
                    // verticaly align text to frame padding
                    ImGui.AlignTextToFramePadding();
                    // draw the text
                    ImGui.Text(sb.ToString());
                    // clear the string builder
                    sb.Clear();
                }
            }

            // reset pop colors (if user forget to close a color tag)
            if (nbColorPush > 0)
            {
                ImGui.PopStyleColor(nbColorPush);
            }
            // reset pop font (if user forget to close a b or size tag)
            while (nbFontPush > 0)
            {
                FuGui.PopFont();
                nbFontPush--;
            }
        }
        #endregion

        #region CheckBox
        /// <summary>
        /// Renders a checkbox with the given text and returns true if the checkbox was clicked. The value of the checkbox is stored in the provided boolean variable.
        /// </summary>
        /// <param name="text">Text to display next to the checkbox</param>
        /// <param name="isChecked">Boolean variable to store the value of the checkbox</param>
        /// <param name="style">Style to use for the checkbox</param>
        /// <returns>True if the checkbox was clicked, false otherwise</returns>
        public virtual bool CheckBox(string text, ref bool isChecked)
        {
            bool clicked = false;
            text = beginElement(text, null); // Push the style for the checkbox element

            // push colors
            if (_nextIsDisabled)
            {
                FuGui.Push(ImGuiCol.CheckMark, ThemeManager.GetColor(ImGuiCustomCol.Knob) * 0.3f);
                FuGui.Push(ImGuiCol.FrameBg, ThemeManager.GetColor(ImGuiCol.CheckMark) * 0.3f);
                FuGui.Push(ImGuiCol.FrameBgHovered, ThemeManager.GetColor(ImGuiCol.CheckMark) * 0.3f);
                FuGui.Push(ImGuiCol.FrameBgActive, ThemeManager.GetColor(ImGuiCol.CheckMark) * 0.3f);
            }
            else
            {
                if (isChecked)
                {
                    FuGui.Push(ImGuiCol.CheckMark, ThemeManager.GetColor(ImGuiCustomCol.Knob));
                    FuGui.Push(ImGuiCol.FrameBg, ThemeManager.GetColor(ImGuiCol.CheckMark));
                    FuGui.Push(ImGuiCol.FrameBgHovered, ThemeManager.GetColor(ImGuiCol.CheckMark) * 0.9f);
                    FuGui.Push(ImGuiCol.FrameBgActive, ThemeManager.GetColor(ImGuiCol.CheckMark) * 0.8f);
                }
                else
                {
                    FuGui.Push(ImGuiCol.CheckMark, ThemeManager.GetColor(ImGuiCustomCol.Knob));
                    FuGui.Push(ImGuiCol.FrameBg, ThemeManager.GetColor(ImGuiCol.FrameBg));
                    FuGui.Push(ImGuiCol.FrameBgHovered, ThemeManager.GetColor(ImGuiCol.FrameBgHovered));
                    FuGui.Push(ImGuiCol.FrameBgActive, ThemeManager.GetColor(ImGuiCol.FrameBgActive));
                }
            }
            if (_nextIsDisabled)
            {
                bool value = isChecked; // Create a temporary variable to hold the value of isChecked
                ImGui.Checkbox(text, ref value); // Display a disabled checkbox with the given text label
            }
            else
            {
                clicked = ImGui.Checkbox(text, ref isChecked); // Display an enabled checkbox and update the value of isChecked based on user interaction
            }
            displayToolTip(); // Display a tooltip if one has been set for this element
            _elementHoverFramed = true; // Set the flag indicating that this element should have a hover frame drawn around it
            endElement(null); // Pop the style for the checkbox element
            FuGui.PopColor(4);
            return clicked; // Return a boolean indicating whether the checkbox was clicked by the user
        }
        #endregion

        #region Radio Button
        /// <summary>
        /// Renders a Radio Button with the given text and returns true if the checkbox was clicked. The value of the checkbox is stored in the provided boolean variable.
        /// </summary>
        /// <param name="text">Text to display next to the checkbox</param>
        /// <param name="isChecked">Boolean variable to store the value of the checkbox</param>
        /// <returns>True if the checkbox was clicked, false otherwise</returns>
        public bool RadioButton(string text, bool isChecked)
        {
            return RadioButton(text, isChecked, UIFrameStyle.Default);
        }

        /// <summary>
        /// Renders a Radio Button with the given text and returns true if the checkbox was clicked. The value of the checkbox is stored in the provided boolean variable.
        /// </summary>
        /// <param name="text">Text to display next to the checkbox</param>
        /// <param name="isChecked">Boolean variable to store the value of the checkbox</param>
        /// <param name="style">Style to use for the checkbox</param>
        /// <returns>True if the checkbox was clicked, false otherwise</returns>
        public virtual bool RadioButton(string text, bool isChecked, UIFrameStyle style)
        {
            string id = beginElement(text, style); // Push the style for the checkbox element
            text = text.Split(new char[] { '#', '#' })[0];
            // get or create animation data
            if (!_uiElementAnimationDatas.ContainsKey(id))
            {
                _uiElementAnimationDatas.Add(id, new UIElementAnimationData(!isChecked));
            }
            UIElementAnimationData animationData = _uiElementAnimationDatas[id];

            // layout states
            float height = 18f;
            Vector2 pos = ImGui.GetCursorScreenPos();
            Vector2 CircleCenter = new Vector2(pos.x + height / 2f + 2f, pos.y + height / 2f + 2f);
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            // input stats
            bool hovered = ImGui.IsMouseHoveringRect(pos, pos + new Vector2(height + 4f, height));
            bool active = hovered && ImGui.IsMouseDown(0);
            bool clicked = hovered && ImGui.IsMouseReleased(0);
            // frame colors
            Vector4 BGColor = default;
            Vector4 knobColor = default;
            if (_nextIsDisabled)
            {
                BGColor = style.DisabledFrame;
                knobColor = ThemeManager.GetColor(ImGuiCustomCol.Knob) * 0.3f;
            }
            else
            {
                BGColor = style.CheckMark;
                knobColor = ThemeManager.GetColor(ImGuiCustomCol.Knob);
                if (active)
                {
                    BGColor = style.CheckMark * 0.8f;
                    knobColor = ThemeManager.GetColor(ImGuiCustomCol.KnobActive);
                }
                else if (hovered)
                {
                    BGColor = style.CheckMark * 0.9f;
                    knobColor = ThemeManager.GetColor(ImGuiCustomCol.KnobHovered);
                }
            }

            // draw radio button
            drawList.AddCircleFilled(CircleCenter, height / 2f, ImGui.GetColorU32(!isChecked ? ThemeManager.GetColor(ImGuiCol.FrameBg) : BGColor), 64);
            if (animationData.CurrentValue > 0f)
            {
                float knobSize = Mathf.Lerp(0f, height / 5f, animationData.CurrentValue);
                drawList.AddCircleFilled(CircleCenter, knobSize, ImGui.GetColorU32(knobColor), 64);
            }
            else
            {
                drawList.AddCircle(CircleCenter, height / 2f, ImGui.GetColorU32(style.Border), 64);
            }

            //draw hover frame
            if (hovered && !_nextIsDisabled)
            {
                drawList.AddCircle(CircleCenter, height / 2f, ImGui.GetColorU32(ThemeManager.GetColor(ImGuiCustomCol.FrameHoverFeedback)), 64, 1f);
            }

            // update animation data
            animationData.Update(isChecked, _animationEnabled);

            // dummy display button
            ImGui.Dummy(new Vector2(height + 4f, height));
            ImGui.SameLine();
            // align and draw text
            ImGui.AlignTextToFramePadding();
            ImGui.Text(text);

            // display tooltip if needed
            displayToolTip(); // Display a tooltip if one has been set for this element
            _elementHoverFramed = false; // Set the flag indicating that this element should have a hover frame drawn around it
            endElement(style); // Pop the style for the checkbox element
            return clicked; // Return a boolean indicating whether the checkbox was clicked by the user
        }
        #endregion

        #region Slider
        #region Slider Int
        /// <summary>
        /// Creates a horizontal slider widget that allows the user to choose an integer value from a range.
        /// </summary>
        /// <param name="text">The label for the slider.</param>
        /// <param name="value">The current value of the slider, which will be updated if the user interacts with the slider.</param>
        /// <returns>True if the value was changed by the user, false otherwise.</returns>
        public bool Slider(string text, ref int value)
        {
            return Slider(text, ref value, 0, 100);
        }

        /// <summary>
        /// Creates a horizontal slider widget that allows the user to choose an integer value from a range.
        /// </summary>
        /// <param name="text">The label for the slider.</param>
        /// <param name="value">The current value of the slider, which will be updated if the user interacts with the slider.</param>
        /// <param name="min">The minimum value that the user can select.</param>
        /// <param name="max">The maximum value that the user can select.</param>
        /// <returns>True if the value was changed by the user, false otherwise.</returns>
        public bool Slider(string text, ref int value, int min, int max)
        {
            float val = value;
            bool valueChange = _customSlider(text, ref val, min, max, true);
            value = (int)val;
            return valueChange;
        }
        #endregion

        #region Slider Float
        /// <summary>
        /// Creates a horizontal slider widget that allows the user to choose an integer value from a range.
        /// </summary>
        /// <param name="text">The label for the slider.</param>
        /// <param name="value">The current value of the slider, which will be updated if the user interacts with the slider.</param>
        /// <returns>True if the value was changed by the user, false otherwise.</returns>
        public bool Slider(string text, ref float value)
        {
            return Slider(text, ref value, 0f, 100f);
        }

        /// <summary>
        /// Creates a horizontal slider widget that allows the user to choose an integer value from a range.
        /// </summary>
        /// <param name="text">The label for the slider.</param>
        /// <param name="value">The current value of the slider, which will be updated if the user interacts with the slider.</param>
        /// <param name="min">The minimum value that the user can select.</param>
        /// <param name="max">The maximum value that the user can select.</param>
        /// <returns>True if the value was changed by the user, false otherwise.</returns>
        public bool Slider(string text, ref float value, float min, float max)
        {
            return _customSlider(text, ref value, min, max, false);
        }
        #endregion

        /// <summary>
        /// Draw a custom unity-style slider (Label + slider + input)
        /// </summary>
        /// <param name="text">Label and ID of the slider</param>
        /// <param name="value">refered value of the slider</param>
        /// <param name="min">minimum value of the slider</param>
        /// <param name="max">maximum value of the slider</param>
        /// <param name="isInt">whatever the slider is an Int slider (default is float). If true, the value will be rounded</param>
        /// <returns>true if value changed</returns>
        protected virtual bool _customSlider(string text, ref float value, float min, float max, bool isInt)
        {
            text = beginElement(text, null);

            // Calculate the position and size of the slider
            Vector2 cursorPos = ImGui.GetCursorScreenPos();
            float knobRadius = 5f;
            float hoverPaddingY = 4f;
            float height = 20f;
            float lineHeight = 2f;
            float width = ImGui.GetContentRegionAvail().x - 60f;
            float x = cursorPos.x;
            float y = cursorPos.y + height / 2f;
            float oldValue = value;

            // is there place to draw slider
            if (width >= 24f)
            {
                // Calculate the position of the knob
                float knobPos = (x + knobRadius) + (width - knobRadius * 2f) * (value - min) / (max - min);
                // Check if the mouse is hovering over the slider
                bool isLineHovered = ImGui.IsMouseHoveringRect(new Vector2(x, y - hoverPaddingY - lineHeight), new Vector2(x + width, y + hoverPaddingY + lineHeight));
                // Check if the mouse is hovering over the knob
                bool isKnobHovered = ImGui.IsMouseHoveringRect(new Vector2(knobPos - knobRadius, y - knobRadius), new Vector2(knobPos + knobRadius, y + knobRadius));
                // Check if slider is dragging
                bool isDragging = _draggingSliders.Contains(text);
                // Calculate colors
                Vector4 leftLineColor = ThemeManager.GetColor(ImGuiCol.CheckMark);
                Vector4 rightLineColor = ThemeManager.GetColor(ImGuiCol.FrameBg);
                Vector4 knobColor = ThemeManager.GetColor(ImGuiCustomCol.Knob);
                if (_nextIsDisabled)
                {
                    leftLineColor *= 0.5f;
                    rightLineColor *= 0.5f;
                    knobColor *= 0.5f;
                }
                else
                {
                    if (isDragging)
                    {
                        knobColor = ThemeManager.GetColor(ImGuiCustomCol.KnobActive);
                    }
                    else if (isKnobHovered)
                    {
                        knobColor = ThemeManager.GetColor(ImGuiCustomCol.KnobHovered);
                    }
                }
                // Draw the left slider line
                ImGui.GetWindowDrawList().AddLine(new Vector2(x, y), new Vector2(knobPos, y), ImGui.GetColorU32(leftLineColor), lineHeight);
                // Draw the right slider line
                ImGui.GetWindowDrawList().AddLine(new Vector2(knobPos, y), new Vector2(x + width, y), ImGui.GetColorU32(rightLineColor), lineHeight);

                // Draw the knob
                if (!_nextIsDisabled)
                {
                    if (_draggingSliders.Contains(text))
                    {
                        knobColor *= 0.7f;
                    }
                    else if (isKnobHovered)
                    {
                        knobColor *= 0.9f;
                    }
                    knobColor.w = 1f;
                }
                // draw knob border
                ImGui.GetWindowDrawList().AddCircleFilled(new Vector2(knobPos, y), ((isKnobHovered || isDragging) && !_nextIsDisabled ? knobRadius : knobRadius * 0.8f) + 1f, ImGui.GetColorU32(ImGuiCol.Border), 32);
                // draw knob
                ImGui.GetWindowDrawList().AddCircleFilled(new Vector2(knobPos, y), (isKnobHovered || isDragging) && !_nextIsDisabled ? knobRadius : knobRadius * 0.8f, ImGui.GetColorU32(knobColor), 32);

                // start dragging this slider
                if ((isLineHovered || isKnobHovered) && !_draggingSliders.Contains(text) && ImGui.IsMouseClicked(0))
                {
                    _draggingSliders.Add(text);
                }

                // stop dragging this slider
                if (_draggingSliders.Contains(text) && !ImGui.IsMouseDown(0))
                {
                    _draggingSliders.Remove(text);
                }

                // If the mouse is hovering over the knob, change the value when the mouse is clicked
                if (_draggingSliders.Contains(text) && ImGui.IsMouseDown(0) && !_nextIsDisabled)
                {
                    // Calculate the new value based on the mouse position
                    float mouseX = ImGui.GetMousePos().x;
                    value = min + (mouseX - x - knobRadius) / (width - knobRadius * 2f) * (max - min);

                    // Clamp the value to the min and max range
                    value = Math.Clamp(value, min, max);
                    if (isInt)
                    {
                        value = (int)value;
                    }
                }

                ImGui.Dummy(new Vector2(width, 0f));
                ImGui.SameLine();
            }

            // draw drag input
            ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().x - 52f - 4f, 0f));
            ImGui.SameLine();
            string formatString = getFloatString("##sliderInput" + text, value);
            ImGui.PushItemWidth(52f);
            if (ImGui.InputFloat("##sliderInput" + text, ref value, 0f, 0f, isInt ? "%.0f" : formatString, _nextIsDisabled ? ImGuiInputTextFlags.ReadOnly : ImGuiInputTextFlags.None))
            {
                // Clamp the value to the min and max range
                value = Math.Clamp(value, min, max);
                if (isInt)
                {
                    value = (int)value;
                }
            }
            ImGui.PopItemWidth();
            updateFloatString("##sliderInput" + text, value);
            displayToolTip();
            _elementHoverFramed = true;
            endElement(null);
            return value != oldValue;
        }
        #endregion

        #region Drag
        #region Drag Float
        ///<summary>
        /// Creates a draggable float input field.
        ///</summary>
        ///<param name="id">The identifier for the input field.</param>
        ///<param name="value">The float value to be displayed in the input field.</param>
        ///<param name="vString">A string to be displayed before the input field. If empty, no string will be displayed.</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string id, ref float value, string vString = null)
        {
            return Drag(id, ref value, vString, 0, 100, UIFrameStyle.Default);
        }

        ///<summary>
        /// Creates a draggable float input field.
        ///</summary>
        ///<param name="id">The identifier for the input field.</param>
        ///<param name="value">The float value to be displayed in the input field.</param>
        ///<param name="vString">A string to be displayed before the input field. If empty, no string will be displayed.</param>
        ///<param name="min">The minimum allowed value for the input field.</param>
        ///<param name="max">The maximum allowed value for the input field.</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string id, ref float value, string vString, float min, float max)
        {
            return Drag(id, ref value, vString, min, max, UIFrameStyle.Default);
        }

        ///<summary>
        /// Creates a draggable float input field.
        ///</summary>
        ///<param name="id">The identifier for the input field.</param>
        ///<param name="value">The float value to be displayed in the input field.</param>
        ///<param name="vString">A string to be displayed before the input field. If empty, no string will be displayed.</param>
        ///<param name="min">The minimum allowed value for the input field.</param>
        ///<param name="max">The maximum allowed value for the input field.</param>
        /// <param name="style">The style of the Drag</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public virtual bool Drag(string id, ref float value, string vString, float min, float max, UIFrameStyle style)
        {
            id = beginElement(id, style);
            bool valueChanged = dragFloat(id, ref value, vString, min, max);
            endElement(style);
            return valueChanged;
        }

        ///<summary>
        /// Creates a draggable float input field.
        ///</summary>
        ///<param name="id">The identifier for the input field.</param>
        ///<param name="value">The float value to be displayed in the input field.</param>
        ///<param name="vString">A string to be displayed before the input field. If empty, no string will be displayed.</param>
        ///<param name="min">The minimum allowed value for the input field.</param>
        ///<param name="max">The maximum allowed value for the input field.</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        private bool dragFloat(string id, ref float value, string vString, float min, float max)
        {
            // Display the string before the input field if it was provided
            if (!string.IsNullOrEmpty(vString))
            {
                // verticaly align text to frame padding
                ImGui.AlignTextToFramePadding();
                ImGui.Text(vString);
                ImGui.SameLine();
            }
            // Set the width of the input field and create it
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().x);
            var oldVal = value; // Store the old value in case the input field is disabled
            bool valueChanged = ImGui.DragFloat("##" + id, ref value, 0.01f, min, max, getFloatString("##" + id, value), _nextIsDisabled ? ImGuiSliderFlags.NoInput : ImGuiSliderFlags.AlwaysClamp);

            // Update the format string for the input field based on its current value
            updateFloatString("##" + id, value);

            // If the input field is disabled, reset its value and return false for the valueChanged flag
            if (_nextIsDisabled)
            {
                value = oldVal;
                valueChanged = false;
            }

            // Display a tooltip and set the _elementHoverFramed flag
            displayToolTip();
            _elementHoverFramed = true;

            return valueChanged;
        }
        #endregion

        #region Drag Vector2
        ///<summary>
        /// Creates a draggable Vector2 input field.
        ///</summary>
        ///<param name="id">The identifier for the input field.</param>
        ///<param name="value">The Vector2 value to be displayed in the input field.</param>
        ///<param name="v1String">A string to be displayed before the input field X. If empty, no string will be displayed.</param>
        ///<param name="v2String">A string to be displayed before the input field Y. If empty, no string will be displayed.</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string id, ref Vector2 value, string v1String = null, string v2String = null)
        {
            return Drag(id, ref value, v1String, v2String, 0f, 100f, UIFrameStyle.Default);
        }

        ///<summary>
        /// Creates a draggable Vector2 input field.
        ///</summary>
        ///<param name="id">The identifier for the input field.</param>
        ///<param name="value">The Vector2 value to be displayed in the input field.</param>
        ///<param name="v1String">A string to be displayed before the input field X. If empty, no string will be displayed.</param>
        ///<param name="v2String">A string to be displayed before the input field Y. If empty, no string will be displayed.</param>
        ///<param name="min">The minimum allowed value for the input field.</param>
        ///<param name="max">The maximum allowed value for the input field.</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string id, ref Vector2 value, string v1String, string v2String, float min, float max)
        {
            return Drag(id, ref value, v1String, v2String, min, max, UIFrameStyle.Default);
        }

        ///<summary>
        /// Creates a draggable Vector2 input field.
        ///</summary>
        ///<param name="id">The identifier for the input field.</param>
        ///<param name="value">The Vector2 value to be displayed in the input field.</param>
        ///<param name="v1String">A string to be displayed before the input field X. If empty, no string will be displayed.</param>
        ///<param name="v2String">A string to be displayed before the input field Y. If empty, no string will be displayed.</param>
        ///<param name="min">The minimum allowed value for the input field.</param>
        ///<param name="max">The maximum allowed value for the input field.</param>
        /// <param name="style">The style of the Drag</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public virtual bool Drag(string id, ref Vector2 value, string v1String, string v2String, float min, float max, UIFrameStyle style)
        {
            // Begin the element and apply the specified style
            id = beginElement(id, style);

            bool valueChanged = false;
            // Calculate the column width for the table
            float colWidth = ImGui.GetContentRegionAvail().x * 0.5f;
            // Start the table with two columns
            if (ImGui.BeginTable(id + "dragTable", 2))
            {
                // Set up the first column with the given ID and width
                ImGui.TableSetupColumn(id + "col1", ImGuiTableColumnFlags.None, colWidth);
                // Set up the second column with the given ID and width
                ImGui.TableSetupColumn(id + "col2", ImGuiTableColumnFlags.None, colWidth);
                // Move to the first column
                ImGui.TableNextColumn();
                // Create a draggable float for the first value in the table, using the specified ID and value string
                valueChanged |= dragFloat(id + "val1", ref value.x, v1String, min, max);
                // Draw a hover frame around the element if it is hovered
                drawHoverFrame();
                // Move to the second column
                ImGui.TableNextColumn();
                // Create a draggable float for the second value in the table, using the specified ID and value string
                valueChanged |= dragFloat(id + "val2", ref value.y, v2String, min, max);
                // Draw a hover frame around the element if it is hovered
                drawHoverFrame();
                // End the table
                ImGui.EndTable();
            }
            // Reset the flag for whether the element is hovered and framed
            _elementHoverFramed = false;
            // End the element
            endElement(style);
            return valueChanged;
        }

        #endregion

        #region Drag Vector3
        ///<summary>
        /// Creates a draggable Vector3 input field.
        ///</summary>
        ///<param name="id">The identifier for the input field.</param>
        ///<param name="value">The Vector3 value to be displayed in the input field.</param>
        ///<param name="v1String">A string to be displayed before the input field X. If empty, no string will be displayed.</param>
        ///<param name="v2String">A string to be displayed before the input field Y. If empty, no string will be displayed.</param>
        ///<param name="v3String">A string to be displayed before the input field Z. If empty, no string will be displayed.</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string id, ref Vector3 value, string v1String = null, string v2String = null, string v3String = null)
        {
            return Drag(id, ref value, v1String, v2String, v3String, 0f, 100f, UIFrameStyle.Default);
        }

        ///<summary>
        /// Creates a draggable Vector3 input field.
        ///</summary>
        ///<param name="id">The identifier for the input field.</param>
        ///<param name="value">The Vector3 value to be displayed in the input field.</param>
        ///<param name="v1String">A string to be displayed before the input field X. If empty, no string will be displayed.</param>
        ///<param name="v2String">A string to be displayed before the input field Y. If empty, no string will be displayed.</param>
        ///<param name="v3String">A string to be displayed before the input field Z. If empty, no string will be displayed.</param>
        ///<param name="min">The minimum allowed value for the input field.</param>
        ///<param name="max">The maximum allowed value for the input field.</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string id, ref Vector3 value, string v1String, string v2String, string v3String, float min, float max)
        {
            return Drag(id, ref value, v1String, v2String, v3String, min, max, UIFrameStyle.Default);
        }

        ///<summary>
        /// Creates a draggable Vector3 input field.
        ///</summary>
        ///<param name="id">The identifier for the input field.</param>
        ///<param name="value">The Vector3 value to be displayed in the input field.</param>
        ///<param name="v1String">A string to be displayed before the input field X. If empty, no string will be displayed.</param>
        ///<param name="v2String">A string to be displayed before the input field Y. If empty, no string will be displayed.</param>
        ///<param name="v3String">A string to be displayed before the input field Z. If empty, no string will be displayed.</param>
        ///<param name="min">The minimum allowed value for the input field.</param>
        ///<param name="max">The maximum allowed value for the input field.</param>
        /// <param name="style">The style of the Drag</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public virtual bool Drag(string id, ref Vector3 value, string v1String, string v2String, string v3String, float min, float max, UIFrameStyle style)
        {
            id = beginElement(id, style);
            bool valueChanged = false;
            float colWidth = ImGui.GetContentRegionAvail().x / 3f;
            if (ImGui.BeginTable(id + "dragTable", 3))
            {
                // Set up the three columns in the table
                ImGui.TableSetupColumn(id + "col1", ImGuiTableColumnFlags.None, colWidth);
                ImGui.TableSetupColumn(id + "col2", ImGuiTableColumnFlags.None, colWidth);
                ImGui.TableSetupColumn(id + "col3", ImGuiTableColumnFlags.None, colWidth);

                // Begin the first column
                ImGui.TableNextColumn();

                // Drag the first value
                valueChanged |= dragFloat(id + "val1", ref value.x, v1String, min, max);
                drawHoverFrame();

                // Begin the second column
                ImGui.TableNextColumn();

                // Drag the second value
                valueChanged |= dragFloat(id + "val2", ref value.y, v2String, min, max);
                drawHoverFrame();

                // Begin the third column
                ImGui.TableNextColumn();

                // Drag the third value
                valueChanged |= dragFloat(id + "val3", ref value.z, v3String, min, max);
                drawHoverFrame();

                // End the table
                ImGui.EndTable();
            }

            // Reset the hover frame flag
            _elementHoverFramed = false;
            endElement(style);
            return valueChanged;
        }
        #endregion

        #region Drag Vector4
        ///<summary>
        /// Creates a draggable Vector4 input field.
        ///</summary>
        ///<param name="id">The identifier for the input field.</param>
        ///<param name="value">The Vector4 value to be displayed in the input field.</param>
        ///<param name="v1String">A string to be displayed before the input field X. If empty, no string will be displayed.</param>
        ///<param name="v2String">A string to be displayed before the input field Y. If empty, no string will be displayed.</param>
        ///<param name="v3String">A string to be displayed before the input field Z. If empty, no string will be displayed.</param>
        ///<param name="v4String">A string to be displayed before the input field W. If empty, no string will be displayed.</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string id, ref Vector4 value, string v1String = null, string v2String = null, string v3String = null, string v4String = null)
        {
            return Drag(id, ref value, v1String, v2String, v3String, v4String, 0f, 100f, UIFrameStyle.Default);
        }
        ///<summary>
        /// Creates a draggable Vector4 input field.
        ///</summary>
        ///<param name="id">The identifier for the input field.</param>
        ///<param name="value">The Vector4 value to be displayed in the input field.</param>
        ///<param name="v1String">A string to be displayed before the input field X. If empty, no string will be displayed.</param>
        ///<param name="v2String">A string to be displayed before the input field Y. If empty, no string will be displayed.</param>
        ///<param name="v3String">A string to be displayed before the input field Z. If empty, no string will be displayed.</param>
        ///<param name="v4String">A string to be displayed before the input field W. If empty, no string will be displayed.</param>
        ///<param name="min">The minimum allowed value for the input field.</param>
        ///<param name="max">The maximum allowed value for the input field.</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string id, ref Vector4 value, string v1String, string v2String, string v3String, string v4String, float min, float max)
        {
            return Drag(id, ref value, v1String, v2String, v3String, v4String, min, max, UIFrameStyle.Default);
        }
        ///<summary>
        /// Creates a draggable Vector4 input field.
        ///</summary>
        ///<param name="id">The identifier for the input field.</param>
        ///<param name="value">The Vector4 value to be displayed in the input field.</param>
        ///<param name="v1String">A string to be displayed before the input field X. If empty, no string will be displayed.</param>
        ///<param name="v2String">A string to be displayed before the input field Y. If empty, no string will be displayed.</param>
        ///<param name="v3String">A string to be displayed before the input field Z. If empty, no string will be displayed.</param>
        ///<param name="v4String">A string to be displayed before the input field W. If empty, no string will be displayed.</param>
        ///<param name="min">The minimum allowed value for the input field.</param>
        ///<param name="max">The maximum allowed value for the input field.</param>
        /// <param name="style">The style of the Drag</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public virtual bool Drag(string id, ref Vector4 value, string v1String, string v2String, string v3String, string v4String, float min, float max, UIFrameStyle style)
        {
            id = beginElement(id, style);
            bool valueChanged = false;
            float colWidth = ImGui.GetContentRegionAvail().x * 0.25f;
            if (ImGui.BeginTable(id + "dragTable", 4))
            {
                // Set up four columns with equal widths
                ImGui.TableSetupColumn(id + "col1", ImGuiTableColumnFlags.None, colWidth);
                ImGui.TableSetupColumn(id + "col2", ImGuiTableColumnFlags.None, colWidth);
                ImGui.TableSetupColumn(id + "col3", ImGuiTableColumnFlags.None, colWidth);
                ImGui.TableSetupColumn(id + "col4", ImGuiTableColumnFlags.None, colWidth);

                // Move to the first column
                ImGui.TableNextColumn();
                valueChanged |= dragFloat(id + "val1", ref value.x, v1String, min, max); // Drag float for the first value
                drawHoverFrame();
                ImGui.TableNextColumn();
                valueChanged |= dragFloat(id + "val2", ref value.y, v2String, min, max); // Drag float for the second value
                drawHoverFrame();
                ImGui.TableNextColumn();
                valueChanged |= dragFloat(id + "val3", ref value.z, v3String, min, max); // Drag float for the third value
                drawHoverFrame();
                ImGui.TableNextColumn();
                valueChanged |= dragFloat(id + "val4", ref value.w, v4String, min, max); // Drag float for the fourth value
                drawHoverFrame();
                ImGui.EndTable();
            }
            _elementHoverFramed = false;
            endElement(style);
            return valueChanged;
        }

        #endregion

        #region Drag Int
        ///<summary>
        /// Creates a draggable int input field.
        ///</summary>
        ///<param name="id">The identifier for the input field.</param>
        ///<param name="value">The int value to be displayed in the input field.</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string id, ref int value)
        {
            return Drag(id, null, ref value, 0, 100, UIFrameStyle.Default);
        }

        ///<summary>
        /// Creates a draggable int input field.
        ///</summary>
        ///<param name="id">The identifier for the input field.</param>
        ///<param name="value">The int value to be displayed in the input field.</param>
        ///<param name="vString">A string to be displayed before the input field. If empty, no string will be displayed.</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string id, string vString, ref int value)
        {
            return Drag(id, vString, ref value, 0, 100, UIFrameStyle.Default);
        }

        ///<summary>
        /// Creates a draggable int input field.
        ///</summary>
        ///<param name="id">The identifier for the input field.</param>
        ///<param name="value">The int value to be displayed in the input field.</param>
        ///<param name="vString">A string to be displayed before the input field. If empty, no string will be displayed.</param>
        ///<param name="min">The minimum allowed value for the input field.</param>
        ///<param name="max">The maximum allowed value for the input field.</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string id, string vString, ref int value, int min, int max)
        {
            return Drag(id, vString, ref value, min, max, UIFrameStyle.Default);
        }

        ///<summary>
        /// Creates a draggable int input field.
        ///</summary>
        ///<param name="id">The identifier for the input field.</param>
        ///<param name="value">The int value to be displayed in the input field.</param>
        ///<param name="min">The minimum allowed value for the input field.</param>
        ///<param name="max">The maximum allowed value for the input field.</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string id, ref int value, int min, int max)
        {
            return Drag(id, null, ref value, min, max, UIFrameStyle.Default);
        }

        /// <summary>
        /// Creates a draggable integer input element with an optional label.
        /// The element has a range between the given minimum and maximum values.
        /// The element uses the given style for its appearance.
        /// </summary>
        /// <param name="id">A unique identifier for the element.</param>
        /// <param name="vString">The label for the element.</param>
        /// <param name="value">A reference to the integer value to be modified by the element.</param>
        /// <param name="min">The minimum value for the element.</param>
        /// <param name="max">The maximum value for the element.</param>
        /// <param name="style">The style to be used for the element's appearance.</param>
        /// <returns>True if the value was modified, false otherwise.</returns>
        public virtual bool Drag(string id, string vString, ref int value, int min, int max, UIFrameStyle style)
        {
            // start drawing the element
            id = beginElement(id, style);
            // display the label, if there is one
            if (!string.IsNullOrEmpty(vString))
            {
                // verticaly align text to frame padding
                ImGui.AlignTextToFramePadding();
                ImGui.Text(vString);
                ImGui.SameLine();
            }
            // set the width of the element to the available width in the current content region
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().x);
            // store the current value in case the element is disabled
            var oldVal = value;
            // draw the draggable integer input element
            bool valueChanged = ImGui.DragInt("##" + id, ref value, 0.05f, min, max, "%.0f", _nextIsDisabled ? ImGuiSliderFlags.NoInput : ImGuiSliderFlags.AlwaysClamp);
            // if the element is disabled, restore the old value and return false for valueChanged
            if (_nextIsDisabled)
            {
                value = oldVal;
                valueChanged = false;
            }
            // display the tool tip, if there is one
            displayToolTip();
            // this element can draw a fram if it is hovered
            _elementHoverFramed = true;
            // endup the element
            endElement(style);
            // return whatever the value has changed
            return valueChanged;
        }
        #endregion
        #endregion

        #region Combobox
        #region Enums
        /// <summary>
        /// Displays a combobox with all the enum values of type TEnum. The selected item can be changed by the user, and the change will be reported through the itemChange action.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum that will be displayed in the combobox. It must be an enumerated type.</typeparam>
        /// <param name="text">The label text to be displayed next to the combobox</param>
        /// <param name="itemChange">The action that will be called when the selected item changes</param>
        /// <param name="itemGetter">A func that return a way to get current stored value for the combobox. can be null if combobox il not lined to an object's field</param>
        public void ComboboxEnum<TEnum>(string text, Action<TEnum> itemChange, Func<TEnum> itemGetter = null) where TEnum : struct, IConvertible
        {
            ComboboxEnum<TEnum>(text, itemChange, itemGetter, UIComboboxStyle.Default);
        }

        /// <summary>
        /// Displays a combobox with all the enum values of type TEnum. The selected item can be changed by the user, and the change will be reported through the itemChange action.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum that will be displayed in the combobox. It must be an enumerated type.</typeparam>
        /// <param name="text">The label text to be displayed next to the combobox</param>
        /// <param name="itemChange">The action that will be called when the selected item changes</param>
        /// <param name="itemGetter">A func that return a way to get current stored value for the combobox. can be null if combobox il not lined to an object's field</param>
        /// <param name="style">The style to be applied to the combobox</param>
        public void ComboboxEnum<TEnum>(string text, Action<TEnum> itemChange, Func<TEnum> itemGetter, UIComboboxStyle style) where TEnum : struct, IConvertible
        {
            if (!typeof(TEnum).IsEnum)
            {
                throw new ArgumentException("TEnum must be an enumerated type");
            }
            // list to store the enum values
            List<TEnum> enumValues = new List<TEnum>();
            // list to store the combobox items
            List<IComboboxItem> cItems = new List<IComboboxItem>();
            // iterate over the enum values and add them to the lists
            foreach (TEnum enumValue in Enum.GetValues(typeof(TEnum)))
            {
                enumValues.Add(enumValue);
                cItems.Add(new ComboboxTextItem(FuGui.AddSpacesBeforeUppercase(enumValue.ToString()), true));
            }
            // call the custom combobox function, passing in the lists and the itemChange
            _customCombobox(text, cItems, (index) =>
            {
                itemChange?.Invoke(enumValues[index]);
            }, () => { return itemGetter?.Invoke().ToString(); }, style);
        }
        #endregion

        #region Generic Types List
        /// <summary>
        /// Displays a dropdown box with a list of items of type T and calls the specified action with the selected item when changed.
        /// </summary>
        /// <typeparam name="T">The type of the items in the list.</typeparam>
        /// <param name="text">The label of the dropdown box.</param>
        /// <param name="items">The list of items to display in the dropdown box.</param>
        /// <param name="itemChange">The action to call when the selected item changes.</param>
        /// <param name="itemGetter">A func that return a way to get current stored value for the combobox. can be null if combobox il not lined to an object's field</param>
        public void Combobox<T>(string text, List<T> items, Action<T> itemChange, Func<T> itemGetter = null)
        {
            Combobox<T>(text, items, itemChange, itemGetter, UIComboboxStyle.Default);
        }

        /// <summary>
        /// Displays a dropdown box with a list of items of type T and calls the specified action with the selected item when changed.
        /// </summary>
        /// <typeparam name="T">The type of the items in the list.</typeparam>
        /// <param name="text">The label of the dropdown box.</param>
        /// <param name="items">The list of items to display in the dropdown box.</param>
        /// <param name="itemChange">The action to call when the selected item changes.</param>
        /// <param name="itemGetter">A func that return a way to get current stored value for the combobox. can be null if combobox il not lined to an object's field</param>
        /// <param name="style">The style to use for the dropdown box.</param>
        public void Combobox<T>(string text, List<T> items, Action<T> itemChange, Func<T> itemGetter, UIComboboxStyle style)
        {
            // Create a list of combobox items from the list of items
            List<IComboboxItem> cItems = new List<IComboboxItem>();
            foreach (T item in items)
            {
                // the item is already a combobox item
                if (item is IComboboxItem)
                {
                    cItems.Add((IComboboxItem)item);
                }
                else
                {
                    // Add the item to the list of combobox items
                    cItems.Add(new ComboboxTextItem(FuGui.AddSpacesBeforeUppercase(item.ToString()), true));
                }
            }
            // Display the custom combobox and call the specified action when the selected item changes
            _customCombobox(text, cItems, (index) =>
            {
                itemChange?.Invoke(items[index]);
            }, () => { return itemGetter?.Invoke()?.ToString(); }, style);
        }
        #endregion

        #region IComboboxItems
        ///<summary>
        /// Renders a combobox with a list of custom items.
        ///</summary>
        ///<param name="text">The label for the combobox.</param>
        ///<param name="items">The list of custom items to be displayed in the combobox.</param>
        ///<param name="itemChange">The action to be performed when an item is selected.</param>
        /// <param name="itemGetter">A func that return a way to get current stored value for the combobox. can be null if combobox il not lined to an object's field</param>
        ///<param name="style">The style for the combobox element.</param>
        protected virtual void _customCombobox(string text, List<IComboboxItem> items, Action<int> itemChange, Func<string> itemGetter, UIComboboxStyle style)
        {
            text = beginElement(text, style);

            if (!_comboSelectedIndices.ContainsKey(text))
            {
                // Initialize the selected index for the combobox
                _comboSelectedIndices.Add(text, 0);
            }

            // Set current item as setted by getter
            if (itemGetter != null)
            {
                int i = 0;
                string selectedItemString = itemGetter.Invoke();
                if (!string.IsNullOrEmpty(selectedItemString))
                {
                    selectedItemString = FuGui.AddSpacesBeforeUppercase(selectedItemString);
                    foreach (var item in items)
                    {
                        if (item.ToString() == selectedItemString)
                        {
                            _comboSelectedIndices[text] = i;
                            break;
                        }
                        i++;
                    }
                }
            }

            int selectedIndex = _comboSelectedIndices[text];

            if (selectedIndex >= items.Count)
            {
                selectedIndex = items.Count - 1;
            }

            FuGui.Push(ImGuiStyleVar.FramePadding, new Vector2(8f, 2f));
            FuGui.Push(ImGuiStyleVar.WindowPadding, new Vector2(8f, 8f));
            if (ImGui.BeginCombo(text, items[selectedIndex].ToString()))
            {
                // Pop the style to use the default style for the combo dropdown
                FuGui.PopStyle();
                IsInsidePopUp = true;
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].DrawItem(i == selectedIndex) && items[i].Enabled)
                    {
                        // Update the selected index and perform the item change action
                        selectedIndex = i;
                        _comboSelectedIndices[text] = selectedIndex;
                        itemChange?.Invoke(i);
                    }
                }
                IsInsidePopUp = false;

                // Update the current pop-up window and ID
                if (CurrentPopUpID != text)
                {
                    CurrentPopUpWindowID = UIWindow.CurrentDrawingWindow?.ID;
                    CurrentPopUpID = text;
                }
                // Set CurrentPopUpRect to ImGui item rect
                CurrentPopUpRect = new Rect(ImGui.GetWindowPos(), ImGui.GetWindowSize());
                ImGui.EndCombo();
            }
            else
            {
                // Pop the style to use the default style for the combo dropdown
                FuGui.PopStyle();
                if (CurrentPopUpID == text)
                {
                    CurrentPopUpWindowID = null;
                    CurrentPopUpID = null;
                }
            }
            FuGui.PopStyle();
            displayToolTip();
            endElement(style);
        }
        #endregion

        #region Fully custom combobox content
        /// <summary>
        /// Displays a combobox that allows the user to choose from a list of predefined items. 
        /// When an item is selected, the specified callback function is called.
        /// </summary>
        /// <param name="text">The label displayed next to the combobox</param>
        /// <param name="selectedItemText">The currently selected item</param>
        /// <param name="callback">The callback function that is called when an item is selected</param>
        /// <param name="height">The height of the list of items</param>
        public void Combobox(string text, string selectedItemText, Action callback, int height = 0)
        {
            Combobox(text, selectedItemText, callback, UIComboboxStyle.Default, height);
        }

        /// <summary>
        /// Displays a combobox that allows the user to choose from a list of predefined items. 
        /// When an item is selected, the specified callback function is called.
        /// </summary>
        /// <param name="text">The label displayed next to the combobox</param>
        /// <param name="selectedItemText">The currently selected item</param>
        /// <param name="callback">The callback function that is called when an item is selected</param>
        /// <param name="style">The style of the combobox</param>
        /// <param name="height">The height of the list of items</param>
        public virtual void Combobox(string text, string selectedItemText, Action callback, UIComboboxStyle style, int height = 0)
        {
            text = beginElement(text, style);

            // Adjust the padding for the frame and window
            FuGui.Push(ImGuiStyleVar.FramePadding, new Vector2(8f, 2f));
            FuGui.Push(ImGuiStyleVar.WindowPadding, new Vector2(8f, 8f));

            // Begin the combobox
            if (ImGui.BeginCombo(text, selectedItemText))
            {
                // Pop the padding styles
                FuGui.PopStyle();
                IsInsidePopUp = true;
                // Check if a height has been specified
                if (height > 0)
                {
                    // Invoke the callback with a fixed height for the combobox
                    callback?.Invoke();
                }
                else
                {
                    // Invoke the callback without a fixed height for the combobox
                    callback?.Invoke();
                }
                // Set the IsInsidePopUp flag to false
                IsInsidePopUp = false;

                // Check if the CurrentPopUpID is not equal to the given text
                if (CurrentPopUpID != text)
                {
                    // Set the CurrentPopUpWindowID to the current drawing window ID
                    CurrentPopUpWindowID = UIWindow.CurrentDrawingWindow?.ID;
                    // Set the CurrentPopUpID to the given text
                    CurrentPopUpID = text;
                }
                // Set CurrentPopUpRect to ImGui item rect
                CurrentPopUpRect = new Rect(ImGui.GetWindowPos(), ImGui.GetWindowSize());
                // End the combobox
                ImGui.EndCombo();
            }
            else
            {
                // Pop the padding styles
                FuGui.PopStyle();
                // Check if the CurrentPopUpID is equal to the given text
                if (CurrentPopUpID == text)
                {
                    // Set the CurrentPopUpWindowID to null
                    CurrentPopUpWindowID = null;
                    // Set the CurrentPopUpID to null
                    CurrentPopUpID = null;
                }
            }
            // Pop the padding styles
            FuGui.PopStyle();
            // Display the tooltip
            displayToolTip();
            // End the element with the current combobox style
            endElement(style);
        }
        #endregion
        #endregion

        #region Collapsable
        /// <summary>
        /// Displays a collapsable UI element with the given identifier and content.
        /// </summary>
        /// <param name="id">The identifier of the element.</param>
        /// <param name="innerUI">The content to display within the collapsable element.</param>
        public void Collapsable(string id, Action innerUI, float indent = 16f)
        {
            // Use the default style for the collapsable element
            Collapsable(id, innerUI, UICollapsableStyle.Default, indent);
        }

        /// <summary>
        /// Displays a collapsable UI element with the given identifier, content, and style.
        /// </summary>
        /// <param name="id">The identifier of the element.</param>
        /// <param name="innerUI">The content to display within the collapsable element.</param>
        /// <param name="style">The style to apply to the element.</param>
        public void Collapsable(string id, Action innerUI, UICollapsableStyle style, float indent = 16f)
        {
            // Begin the element and apply the specified style
            id = beginElement(id, style);
            // Adjust the padding and spacing for the frame and the items within it
            FuGui.Push(ImGuiStyleVar.FramePadding, new Vector2(8f, 4f));
            FuGui.Push(ImGuiStyleVar.ItemSpacing, new Vector2(0f, 0f));

            // Set the font for the header to be bold and size 14
            FuGui.PushFont(14, FontType.Bold);
            // Display the collapsable header with the given identifier
            bool open = ImGui.CollapsingHeader(id, ImGuiTreeNodeFlags.CollapsingHeader | ImGuiTreeNodeFlags.DefaultOpen);
            // Pop the font changes
            FuGui.PopFont();
            // Display the tool tip for the element
            displayToolTip();
            // Pop the padding and spacing changes
            FuGui.PopStyle(2);
            // End the element
            endElement(style);

            // Draw up and down lines
            Vector2 min = ImGui.GetItemRectMin();
            Vector2 max = ImGui.GetItemRectMax();
            ImGui.GetWindowDrawList().AddLine(new Vector2(min.x, max.y), max, ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.4f)));
            ImGui.GetWindowDrawList().AddLine(min, new Vector2(max.x, min.y), ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.6f)));

            // if collapsable is open, indent content, draw it and unindent
            if (open)
            {
                // dummy for natural frame padding after lines
                ImGui.Dummy(new Vector2(0f, 0f));
                ImGui.Indent(indent);
                innerUI();
                ImGui.Indent(-indent);
            }
        }
        #endregion

        #region textInput
        /// <summary>
        /// Displays a single-line text input field
        /// </summary>
        /// <param name="id">A unique identifier for the text input field</param>
        /// <param name="text">A reference to the string that will be edited</param>
        /// <returns>true if value has just been edited</returns>
        public bool TextInput(string id, ref string text)
        {
            return TextInput(id, "", ref text, 2048, 0, UIFrameStyle.Default);
        }

        /// <summary>
        /// Displays a single-line text input field with a hint
        /// </summary>
        /// <param name="id">A unique identifier for the text input field</param>
        /// <param name="hint">The hint that will be displayed when the field is empty</param>
        /// <param name="text">A reference to the string that will be edited</param>
        /// <returns>true if value has just been edited</returns>
        public bool TextInput(string id, string hint, ref string text)
        {
            return TextInput(id, hint, ref text, 2048, 0, UIFrameStyle.Default);
        }

        /// <summary>
        /// Displays a single-line text input field with a hint
        /// </summary>
        /// <param name="id">A unique identifier for the text input field</param>
        /// <param name="hint">The hint that will be displayed when the field is empty</param>
        /// <param name="text">A reference to the string that will be edited</param>
        /// <param name="size">the maximum size of the text buffer</param>
        /// <returns>true if value has just been edited</returns>
        public bool TextInput(string id, string hint, ref string text, uint size)
        {
            return TextInput(id, hint, ref text, size, 0, UIFrameStyle.Default);
        }

        /// <summary>
        /// Displays a single-line text input field with a hint
        /// </summary>
        /// <param name="id">A unique identifier for the text input field</param>
        /// <param name="hint">The hint that will be displayed when the field is empty</param>
        /// <param name="text">A reference to the string that will be edited</param>
        /// <param name="size">the maximum size of the text buffer</param>
        /// <param name="height">The height of the input field</param>
        /// <returns>true if value has just been edited</returns>
        public bool TextInput(string id, string hint, ref string text, uint size, float height)
        {
            return TextInput(id, hint, ref text, size, height, UIFrameStyle.Default);
        }

        /// <summary>
        /// Displays a single-line text input field with a hint
        /// </summary>
        /// <param name="id">A unique identifier for the text input field</param>
        /// <param name="hint">The hint that will be displayed when the field is empty</param>
        /// <param name="text">A reference to the string that will be edited</param>
        /// <param name="size">the maximum size of the text buffer</param>
        /// <param name="height">The height of the input field</param>
        /// <returns>true if value has just been edited</returns>
        /// <param name="style">the Frame Style to use</param>
        public virtual bool TextInput(string id, string hint, ref string text, uint size, float height, UIFrameStyle style)
        {
            bool edited;
            // Begin the element and apply the specified style
            id = beginElement(id, style);
            // Set the width of the next item to the width of the available content region
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().x);
            // If a height was specified, create a multiline text input
            if (height > 0)
            {
                edited = ImGui.InputTextMultiline(id, ref text, size, new Vector2(ImGui.GetContentRegionAvail().x, height), ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CtrlEnterForNewLine);
            }
            // Otherwise, create a single line text input with a hint
            else
            {
                edited = ImGui.InputTextWithHint(id, hint, ref text, size, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CtrlEnterForNewLine);
            }
            // Display a tool tip if one has been set
            displayToolTip();
            // Mark the element as hover framed
            _elementHoverFramed = true;
            // End the element
            endElement(style);
            // Return whether the text was edited
            return edited;
        }
        #endregion

        #region Image
        /// <summary>
        /// Draw an image
        /// </summary>
        /// <param name="id">ID/Label of the Image</param>
        /// <param name="texture">Texture2D to draw</param>
        /// <param name="size">size of the image</param>
        public virtual void Image(string id, Texture2D texture, Vector2 size)
        {
            UIWindow.CurrentDrawingWindow?.Container.ImGuiImage(texture, size);
            displayToolTip();
        }

        /// <summary>
        /// Draw a RenderTexture
        /// </summary>
        /// <param name="id">ID/Label of the RenderTexture</param>
        /// <param name="texture">RenderTexture to draw</param>
        /// <param name="size">size of the RenderTexture</param>
        public virtual void Image(string id, RenderTexture texture, Vector2 size)
        {
            UIWindow.CurrentDrawingWindow?.Container.ImGuiImage(texture, size);
            displayToolTip();
        }

        /// <summary>
        /// Draw an image button (clickable image)
        /// </summary>
        /// <param name="id">ID/Label of the Image</param>
        /// <param name="texture">Texture2D to draw</param>
        /// <param name="size">size of the image</param>
        /// <returns>true if clicked</returns>
        public virtual bool ImageButton(string id, Texture2D texture, Vector2 size)
        {
            bool clicked = UIWindow.CurrentDrawingWindow?.Container.ImGuiImageButton(texture, size) ?? false;
            displayToolTip();
            return clicked;
        }

        /// <summary>
        /// Draw an image button (clickable image)
        /// </summary>
        /// <param name="id">ID/Label of the Image</param>
        /// <param name="texture">Texture2D to draw</param>
        /// <param name="size">size of the image</param>
        /// <param name="color">tint color of the button</param>
        /// <returns>true if clicked</returns>
        public virtual bool ImageButton(string id, Texture2D texture, Vector2 size, Vector4 color)
        {
            bool clicked = UIWindow.CurrentDrawingWindow?.Container.ImGuiImageButton(texture, size, color) ?? false;
            displayToolTip();
            return clicked;
        }
        #endregion

        #region ColorPicker
        ///<summary>
        /// Displays a color picker widget that allows the user to select a color in the RGB color space.
        ///</summary>
        ///<param name="id">A unique identifier for the color picker widget.</param>
        ///<param name="color">A reference to the vector3 containing the current color value. This value will be updated if the user changes the color.</param>
        public bool ColorPicker(string id, ref Vector4 color)
        {
            return ColorPicker(id, ref color, UIFrameStyle.Default);
        }

        ///<summary>
        /// Displays a color picker widget that allows the user to select a color in the RGBA color space.
        ///</summary>
        ///<param name="id">A unique identifier for the color picker widget.</param>
        ///<param name="color">A reference to the vector4 containing the current color value. This value will be updated if the user changes the color.</param>
        public bool ColorPicker(string id, ref Vector3 color)
        {
            return ColorPicker(id, ref color, UIFrameStyle.Default);
        }

        ///<summary>
        /// Displays a color picker widget that allows the user to select a color in the RGBA color space.
        ///</summary>
        ///<param name="id">A unique identifier for the color picker widget.</param>
        ///<param name="color">A reference to the vector4 containing the current color value. This value will be updated if the user changes the color.</param>
        ///<param name="style">The style to apply to the color picker widget.</param>
        public virtual bool ColorPicker(string id, ref Vector4 color, UIFrameStyle style)
        {
            // Use the custom color picker function to display the widget and get the result
            return _customColorPicker(id, true, ref color, UIFrameStyle.Default);
        }

        ///<summary>
        /// Displays a color picker widget that allows the user to select a color in the RGB color space.
        ///</summary>
        ///<param name="id">A unique identifier for the color picker widget.</param>
        ///<param name="color">A reference to the vector3 containing the current color value. This value will be updated if the user changes the color.</param>
        ///<param name="style">The style to apply to the color picker widget.</param>
        public virtual bool ColorPicker(string id, ref Vector3 color, UIFrameStyle style)
        {
            // Convert the vector3 color value to a vector4 value
            Vector4 col = color;
            // Use the custom color picker function to display the widget and get the result
            bool edited = _customColorPicker(id, false, ref col, UIFrameStyle.Default);
            // If the color was edited, update the vector3 value with the new color
            if (edited)
            {
                color = (Vector3)col;
            }
            // Return whether the color was edited
            return edited;
        }

        /// <summary>
        /// Draw a custom Unity-Like Color Picker
        /// </summary>
        /// <param name="id">ID / Label of the color picker</param>
        /// <param name="alpha">did the color picker must draw alpha line and support alpha</param>
        /// <param name="color">reference of the color to display and edit</param>
        /// <param name="style">the FrameStyle to apply to the right-side input</param>
        /// <returns>true if value has been edited</returns>
        private bool _customColorPicker(string id, bool alpha, ref Vector4 color, UIFrameStyle style)
        {
            bool edited = false;
            id = beginElement(id, style);

            // set padding
            FuGui.Push(ImGuiStyleVar.WindowPadding, new Vector2(8f, 8f));

            float height = 18f;
            float width = ImGui.GetContentRegionAvail().x;
            float rounding = 0f;
            var drawList = ImGui.GetWindowDrawList();

            Vector2 min = ImGui.GetCursorScreenPos();
            min.x += 2;
            min.y += 2;
            Vector2 max = min + new Vector2(width, height);
            max.x -= 2;
            max.y -= 2;
            // draw color part
            Vector4 alphalessColor = color;
            alphalessColor.w = 1f;
            drawList.AddRectFilled(min, new Vector2(max.x, max.y - (alpha ? 4 : 0)), ImGui.GetColorU32(alphalessColor), rounding);
            if (alpha)
            {
                // draw alpha 1
                float alphaWidth = color.w * (max.x - min.x);
                drawList.AddRectFilled(new Vector2(min.x, max.y - 4), new Vector2(min.x + alphaWidth, max.y), ImGui.GetColorU32(Vector4.one), rounding);
                if (color.w < 1.0f)
                {
                    // draw alpha 0
                    drawList.AddRectFilled(new Vector2(min.x + alphaWidth, max.y - 4), new Vector2(max.x, max.y), ImGui.GetColorU32(new Vector4(0, 0, 0, 1)), rounding);
                }
            }

            // draw frame
            drawList.AddRect(min, max, ImGui.GetColorU32(ThemeManager.GetColor(ImGuiCol.Border)), rounding, ImDrawFlags.None, 1f);
            // fake draw the element
            ImGui.Dummy(max - min + Vector2.one * 2f);
            _elementHoverFramed = true;
            drawHoverFrame();
            _elementHoverFramed = false;

            bool hovered = ImGui.IsMouseHoveringRect(min, max) && ImGui.IsWindowHovered();
            displayToolTip(hovered);
            if (hovered && ImGui.IsMouseClicked(0) && !_nextIsDisabled)
            {
                ImGui.OpenPopup("ColorPicker" + id);
            }

            if (alpha)
            {
                ImGui.SetNextWindowSize(new Vector2(256f, 202f));
            }
            else
            {
                ImGui.SetNextWindowSize(new Vector2(256f, 224f));
            }
            if (ImGui.BeginPopup("ColorPicker" + id))
            {
                // Draw the color picker
                ImGui.SetNextItemWidth(184f);
                edited = ImGui.ColorPicker4("##picker" + id, ref color, ImGuiColorEditFlags.DefaultOptions | ImGuiColorEditFlags.DisplayHex | (alpha ? ImGuiColorEditFlags.AlphaBar : ImGuiColorEditFlags.NoAlpha));
                if (CurrentPopUpID != "ColorPicker" + id)
                {
                    CurrentPopUpWindowID = UIWindow.CurrentDrawingWindow?.ID;
                    CurrentPopUpID = "ColorPicker" + id;
                }
                // Set CurrentPopUpRect to ImGui item rect
                CurrentPopUpRect = new Rect(ImGui.GetWindowPos(), ImGui.GetWindowSize());
                ImGui.EndPopup();
            }
            else if (CurrentPopUpID == "ColorPicker" + id)
            {
                CurrentPopUpWindowID = null;
                CurrentPopUpID = null;
            }
            FuGui.PopStyle();
            endElement(style);
            return edited;
        }
        #endregion

        #region ToopTips
        /// <summary>
        /// Set tooltips for the x next element(s)
        /// </summary>
        /// <param name="tooltips">array of tooltips to set</param>
        public void SetNextElementToolTip(params string[] tooltips)
        {
            _currentToolTips = tooltips;
            _currentToolTipsIndex = 0;
            _currentToolTipsOnLabels = false;
        }
        /// <summary>
        /// Set tooltips for the x next element(s), including labels
        /// </summary>
        /// <param name="tooltips">array of tooltips to set</param>
        public void SetNextElementToolTipWithLabel(params string[] tooltips)
        {
            _currentToolTips = tooltips;
            _currentToolTipsIndex = 0;
            _currentToolTipsOnLabels = true;
        }
        #endregion

        #region Toggle
        public bool Toggle(string id, ref bool value, ToggleFlags flags = ToggleFlags.Default)
        {
            return _customToggle(id, ref value, null, null, flags);
        }

        public bool Toggle(string id, ref bool value, string textLeft, string textRight, ToggleFlags flags = ToggleFlags.Default)
        {
            return _customToggle(id, ref value, textLeft, textRight, flags);
        }

        protected virtual bool _customToggle(string id, ref bool value, string textLeft, string textRight, ToggleFlags flags)
        {
            beginElement(id, null);

            // and and get toggle data struct
            if (!_uiElementAnimationDatas.ContainsKey(id))
            {
                _uiElementAnimationDatas.Add(id, new UIElementAnimationData(!value));
            }
            UIElementAnimationData data = _uiElementAnimationDatas[id];

            // process Text Size
            string currentText = value ? textRight : textLeft;
            Vector2 textSize = Vector2.zero;
            if (!string.IsNullOrEmpty(currentText))
            {
                if (flags.HasFlag(ToggleFlags.MaximumTextSize))
                {
                    Vector2 leftTextSize = ImGui.CalcTextSize(textLeft);
                    Vector2 rightTextSize = ImGui.CalcTextSize(textRight);
                    if (leftTextSize.x > rightTextSize.x)
                    {
                        textSize = leftTextSize;
                    }
                    else
                    {
                        textSize = rightTextSize;
                    }
                }
                else
                {
                    textSize = ImGui.CalcTextSize(currentText);
                }
            }

            // draw states
            float height = string.IsNullOrEmpty(currentText) ? 16f : textSize.y + 4f;
            bool valueChanged = false;
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 size = new Vector2(string.IsNullOrEmpty(currentText) ? height * 2f : height * 2f + textSize.x, height);
            if (!flags.HasFlag(ToggleFlags.AlignLeft))
            {
                ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().x - size.x - 4f, 0f));
                ImGui.SameLine();
            }
            Vector2 pos = ImGui.GetCursorScreenPos();
            Vector2 center = pos + new Vector2(size.x / 2, size.y / 2);
            float radius = size.y / 2f - 3f;
            Vector2 startCursorPos = ImGui.GetCursorPos();

            // handle knob position animation
            Vector2 knobLeftPos = new Vector2(pos.x + 3f, center.y - radius);
            Vector2 knobRightPos = new Vector2(pos.x + size.x - (radius * 2f) - 3f, center.y - radius);
            Vector2 knobPos = Vector2.Lerp(knobLeftPos, knobRightPos, data.CurrentValue);

            // input states
            bool hovered = ImGui.IsMouseHoveringRect(pos, pos + size);
            bool clicked = hovered && ImGui.IsMouseReleased(0);
            bool active = hovered && ImGui.IsMouseDown(0);

            Vector4 BGColor = value ? ThemeManager.GetColor(ImGuiCustomCol.Selected) : ThemeManager.GetColor(ImGuiCol.FrameBg);
            Vector4 KnobColor = ThemeManager.GetColor(ImGuiCustomCol.Knob);
            Vector4 TextColor = value ? ThemeManager.GetColor(ImGuiCustomCol.SelectedText) : ThemeManager.GetColor(ImGuiCol.Text);

            if (_nextIsDisabled)
            {
                BGColor *= 0.5f;
                KnobColor *= 0.5f;
            }
            else if (active)
            {
                KnobColor = ThemeManager.GetColor(ImGuiCustomCol.KnobActive);
                BGColor = value ? ThemeManager.GetColor(ImGuiCustomCol.SelectedActive) : ThemeManager.GetColor(ImGuiCol.FrameBgActive);
            }
            else if (hovered)
            {
                KnobColor = ThemeManager.GetColor(ImGuiCustomCol.KnobHovered);
                BGColor = value ? ThemeManager.GetColor(ImGuiCustomCol.SelectedHovered) : ThemeManager.GetColor(ImGuiCol.FrameBgHovered);
            }
            Vector4 BorderColor = BGColor * 0.66f;

            // draw background
            drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(BGColor), 99);
            // draw border
            drawList.AddRect(pos, pos + size, ImGui.GetColorU32(BorderColor), 99);
            // draw knob
            drawList.AddCircleFilled(knobPos + new Vector2(radius, radius), radius, ImGui.GetColorU32(KnobColor), 32);

            // draw text
            if (!string.IsNullOrEmpty(currentText))
            {
                if (!value)
                {
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + radius * 2f + 12f);
                }
                else
                {
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 8f);
                }
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 2f);
                FuGui.Push(ImGuiCol.Text, TextColor);
                ImGui.Text(currentText);
                FuGui.PopColor();
            }

            // draw dummy to match ImGui layout
            ImGui.SetCursorPos(startCursorPos);
            ImGui.Dummy(size);

            // handle hover and click
            if (clicked && !_nextIsDisabled)
            {
                value = !value;
                valueChanged = true;
            }

            data.Update(value, _animationEnabled);

            endElement(null);
            return valueChanged;
        }
        #endregion

        #region Buttons Group
        public void ButtonsGroup<TEnum>(string text, Action<TEnum> itemChange, int defaultSelected = 0, ButtonsGroupFlags flags = ButtonsGroupFlags.Default) where TEnum : struct, IConvertible
        {
            ButtonsGroup<TEnum>(text, itemChange, defaultSelected, flags, UIButtonsGroupStyle.Default);
        }

        public void ButtonsGroup<TEnum>(string text, Action<TEnum> itemChange, int defaultSelected, ButtonsGroupFlags flags, UIButtonsGroupStyle style) where TEnum : struct, IConvertible
        {
            if (!typeof(TEnum).IsEnum)
            {
                throw new ArgumentException("TEnum must be an enumerated type");
            }
            // list to store the enum values
            List<TEnum> enumValues = new List<TEnum>();
            // list to store the combobox items
            List<string> cItems = new List<string>();
            // iterate over the enum values and add them to the lists
            foreach (TEnum enumValue in Enum.GetValues(typeof(TEnum)))
            {
                enumValues.Add(enumValue);
                cItems.Add(FuGui.AddSpacesBeforeUppercase(enumValue.ToString()));
            }
            // call the custom combobox function, passing in the lists and the itemChange
            _buttonsGroup(text, cItems, (index) =>
            {
                itemChange?.Invoke(enumValues[index]);
            }, defaultSelected, flags, style);
        }

        public void ButtonsGroup<T>(string id, List<T> items, Action<T> callback, int defaultSelected, ButtonsGroupFlags flags = ButtonsGroupFlags.Default)
        {
            _buttonsGroup<T>(id, items, (index) => { callback?.Invoke(items[index]); }, defaultSelected, flags, UIButtonsGroupStyle.Default);
        }

        public void ButtonsGroup<T>(string id, List<T> items, Action<T> callback, int defaultSelected, ButtonsGroupFlags flags, UIButtonsGroupStyle style)
        {
            _buttonsGroup<T>(id, items, (index) => { callback?.Invoke(items[index]); }, defaultSelected, flags, style);
        }

        protected virtual void _buttonsGroup<T>(string id, List<T> items, Action<int> callback, int defaultSelected, ButtonsGroupFlags flags, UIButtonsGroupStyle style)
        {
            beginElement(id, style);
            // get selected
            if (!_buttonsGroupIndex.ContainsKey(id))
            {
                _buttonsGroupIndex.Add(id, defaultSelected);
            }
            int selected = _buttonsGroupIndex[id];

            // draw data
            int nbItems = items.Count;
            float cursorPos = ImGui.GetCursorPos().x;
            float avail = ImGui.GetContentRegionAvail().x;
            float itemWidth = avail / nbItems;
            bool autoSize = flags.HasFlag(ButtonsGroupFlags.AutoSizeButtons);

            float naturalSize = 0f;
            // align group to the right
            if (!flags.HasFlag(ButtonsGroupFlags.AlignLeft) && autoSize)
            {
                for (int i = 0; i < nbItems; i++)
                {
                    Vector2 txtSize = ImGui.CalcTextSize(items[i].ToString());
                    naturalSize += 8f + Mathf.Max(txtSize.x, txtSize.y + 4f);
                }
                naturalSize += nbItems;
                cursorPos = cursorPos + ImGui.GetContentRegionAvail().x - naturalSize;
            }

            FuGui.Push(ImGuiStyleVar.FrameRounding, 0f);
            // draw buttons
            for (int i = 0; i < nbItems; i++)
            {
                if (selected == i)
                {
                    style.SelectedButtonStyle.Push(!_nextIsDisabled);
                }
                else
                {
                    style.ButtonStyle.Push(!_nextIsDisabled);
                }

                ImGui.SetCursorPosX(cursorPos);
                if (autoSize)
                {
                    Vector2 txtSize = ImGui.CalcTextSize(items[i].ToString());
                    itemWidth = 8f + Mathf.Max(txtSize.x, txtSize.y + 4f);
                }
                cursorPos += itemWidth - 1f;
                FuGui.Push(ImGuiStyleVar.FramePadding, new Vector4(4f, 4f));
                if (ImGui.Button(items[i].ToString() + "##" + id, new Vector2(itemWidth, 0)) && !_nextIsDisabled)
                {
                    _buttonsGroupIndex[id] = i;
                    callback?.Invoke(i);
                }
                if (i < nbItems - 1)
                {
                    ImGui.SameLine();
                }
                FuGui.PopStyle();
                UIButtonStyle.Default.Pop();
                displayToolTip();
            }
            FuGui.PopStyle();
            endElement();
        }
        #endregion

        #region PathField
        public void InputFolder(string id, Action<string> callback = null, string defaultPath = null, params ExtensionFilter[] extentions)
        {
            _pathField(id, true, callback, UIFrameStyle.Default, defaultPath, extentions);
        }

        public void InputFolder(string id, UIFrameStyle style, Action<string> callback = null, string defaultPath = null, params ExtensionFilter[] extentions)
        {
            _pathField(id, true, callback, style, defaultPath, extentions);
        }

        public void InputFile(string id, Action<string> callback = null, string defaultPath = null, params ExtensionFilter[] extentions)
        {
            _pathField(id, false, callback, UIFrameStyle.Default, defaultPath, extentions);
        }

        public void InputFile(string id, UIFrameStyle style, Action<string> callback = null, string defaultPath = null, params ExtensionFilter[] extentions)
        {
            _pathField(id, false, callback, style, defaultPath, extentions);
        }

        protected virtual void _pathField(string id, bool onlyFolder, Action<string> callback, UIFrameStyle style, string defaultPath = null, params ExtensionFilter[] extentions)
        {
            // apply style and set unique ID
            id = beginElement(id, style);

            // set path if not exist in dic
            if (!_pathFieldValues.ContainsKey(id))
            {
                _pathFieldValues.Add(id, string.IsNullOrEmpty(defaultPath) ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop) : defaultPath);
            }
            string path = _pathFieldValues[id];

            // display values
            float cursorPos = ImGui.GetCursorScreenPos().x;
            float width = ImGui.GetContentRegionAvail().x;
            float buttonWidth = ImGui.CalcTextSize("...").x + 8f;

            // draw input text
            ImGui.SetNextItemWidth(width - buttonWidth);
            bool edited = false;
            if (ImGui.InputText("##" + id, ref path, 2048, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                validatePath();
            }
            // draw text input frame and tooltip
            _elementHoverFramed = true;
            drawHoverFrame();
            displayToolTip(false, true);

            // draw button
            ImGui.SameLine();
            ImGui.SetCursorScreenPos(new Vector2(cursorPos + width - buttonWidth, ImGui.GetCursorScreenPos().y));
            if (ImGui.Button("...##" + id, new Vector2(buttonWidth, 0)))
            {
                string[] paths = null;
                if (onlyFolder)
                {
                    paths = FileBrowser.OpenFolderPanel("Open Folder", "", false);
                }
                else
                {
                    paths = FileBrowser.OpenFilePanel("Open File", "", extentions, false);
                }
                if (paths != null && paths.Length > 0)
                {
                    path = paths[0];
                    validatePath();
                }
            }
            _elementHoverFramed = true;
            endElement(style);

            if (edited)
            {
                callback?.Invoke(_pathFieldValues[id]);
            }

            void validatePath()
            {
                // it must be a directory and it exists
                if (onlyFolder && Directory.Exists(path))
                {
                    _pathFieldValues[id] = path;
                    edited = true;
                }
                // it must be a file and it exists
                else if (File.Exists(path))
                {
                    // we need to check if extention match
                    if (extentions.Length > 0)
                    {
                        string fileExt = Path.GetExtension(path).Replace(".", "");
                        // iterate on filters
                        foreach (var ext in extentions)
                        {
                            // iterate on extentions
                            foreach (string extStr in ext.Extensions)
                            {
                                // check whatever extention is valid
                                if (extStr == "*" || extStr == fileExt)
                                {
                                    _pathFieldValues[id] = path;
                                    edited = true;
                                    return;
                                }
                            }
                        }
                    }
                    // we do not need to check extentions
                    else
                    {
                        _pathFieldValues[id] = path;
                        edited = true;
                    }
                }
            }
        }
        #endregion

        #region Modal

        #endregion

        /// <summary>
        /// Draw a Separator Line
        /// </summary>
        public void Separator()
        {
            ImGui.Separator();
        }

        /// <summary>
        /// Draw a space
        /// </summary>
        public void Spacing()
        {
            ImGui.Spacing();
        }

        /// <summary>
        /// Draw the next element on Same Line as current
        /// </summary>
        public void SameLine()
        {
            ImGui.SameLine();
        }

        /// <summary>
        /// Draw an empty dummy element of size x y
        /// </summary>
        /// <param name="x">width of the dummy</param>
        /// <param name="y">height of the dummy</param>
        public void Dummy(float x = 0f, float y = 0f)
        {
            ImGui.Dummy(new Vector2(x, y));
        }

        /// <summary>
        /// Draw an empty dummy element of size 'size'
        /// </summary>
        /// <param name="size">size of the dummy</param>
        public void Dummy(Vector2 size)
        {
            ImGui.Dummy(size);
        }
        #endregion

        #region private utils
        #region drag decimals
        /// <summary>
        /// Gets the string format for the given id and value
        /// </summary>
        /// <param name="id">ID of the UIElement</param>
        /// <param name="value">float Value</param>
        /// <returns></returns>
        private string getFloatString(string id, float value)
        {
            // If the string format doesn't exist for this id, create it
            if (!_dragStringFormats.ContainsKey(id))
            {
                updateFloatString(id, value);
            }
            // Return the string format for this id
            return _dragStringFormats[id];
        }

        /// <summary>
        /// Update the string format for the given id and value
        /// </summary>
        /// <param name="id">ID of the UIElement</param>
        /// <param name="value">float Value</param>
        private void updateFloatString(string id, float value)
        {
            // If the string format doesn't exist for this id, add it with a default value
            if (!_dragStringFormats.ContainsKey(id))
            {
                _dragStringFormats.Add(id, "%.2f");
            }

            // If the element is focused, set the string format to 4 decimal places
            if (ImGui.IsItemFocused())
            {
                _dragStringFormats[id] = $"%.4f";
                return;
            }
            // Split the value by the decimal point
            string v = value.ToString();
            string[] spl = v.Split(',');
            // If there is a decimal point, set the string format to the number of decimal places (up to 8)
            if (spl.Length > 1)
            {
                int nbDec = Math.Min(8, spl[1].TrimEnd('0').Length);
                _dragStringFormats[id] = $"%.{nbDec}f";
                return;
            }
            // Otherwise, set the string format to 0 decimal places
            _dragStringFormats[id] = "%.0f";
        }
        #endregion

        #region string utils
        /// <summary>
        /// Displays a tooltip if the current element is hovered over, or if force is set to true.
        /// </summary>
        /// <param name="force">Whether to force display the tooltip or not.</param>
        protected void displayToolTip(bool force = false, bool ignoreAvancement = false)
        {
            // If there are tooltips to display and we haven't displayed all of them yet
            if (_currentToolTips != null && _currentToolTipsIndex < _currentToolTips.Length)
            {
                // If the element is hovered over or force is set to true
                if (force || ImGui.IsItemHovered())
                {
                    // set padding and font
                    FuGui.PushDefaultFont();
                    FuGui.Push(ImGuiStyleVar.WindowPadding, new Vector4(8f, 4f));
                    // Display the current tooltip
                    if (_nextIsDisabled)
                    {
                        ImGui.SetTooltip("(Disabled) : " + _currentToolTips[_currentToolTipsIndex]);
                    }
                    else
                    {
                        ImGui.SetTooltip(_currentToolTips[_currentToolTipsIndex]);
                    }
                    FuGui.PopFont();
                    FuGui.PopStyle();
                }

                // is we want to ignore tooltip avancement, let's return without increment the index
                if (ignoreAvancement)
                {
                    return;
                }
                // Move on to the next tooltip
                _currentToolTipsIndex++;
                // If we have displayed all the tooltips, reset the current tooltips array
                if (_currentToolTipsIndex >= _currentToolTips.Length)
                {
                    _currentToolTips = null;
                }
            }
        }
        #endregion
        #endregion
    }
}