using Fu.Core;
using ImGuiNET;
using System;
using System.Diagnostics;
using System.Text;
using UnityEngine;

namespace Fu.Framework
{
    public partial class FuLayout
    {
        private static StringBuilder _textChunkStringBuilder = new StringBuilder();

        #region Text
        /// <summary>
        /// Displays a text string.
        /// </summary>
        /// <param name="text">The string to display.</param>
        /// <param name="wrapping">However you want to wrapp the text</param>
        public void Text(string text, FuTextWrapping wrapping = FuTextWrapping.None)
        {
            Text(text, FuTextStyle.Default, wrapping);
        }

        /// <summary>
        /// Displays a text string.
        /// </summary>
        /// <param name="text">The string to display.</param>
        /// <param name="size">size of the text</param>
        /// <param name="wrapping">However you want to wrapp the text</param>
        public void Text(string text, Vector2 size, FuTextWrapping wrapping = FuTextWrapping.None)
        {
            Text(text, FuTextStyle.Default, size, wrapping);
        }

        /// <summary>
        /// Displays a text string with the specified style.
        /// </summary>
        /// <param name="text">The string to display.</param>
        /// <param name="style">The style to apply to the text.</param>
        /// <param name="wrapping">However you want to wrapp the text</param>
        public void Text(string text, FuTextStyle style, FuTextWrapping wrapping = FuTextWrapping.None)
        {
            Text(text, style, Vector2.zero, wrapping);
        }

        /// <summary>
        /// Displays a text string with the specified style.
        /// </summary>
        /// <param name="text">The string to display.</param>
        /// <param name="style">The style to apply to the text.</param>
        /// <param name="size">size of the text</param>
        /// <param name="wrapping">However you want to wrapp the text</param>
        public virtual void Text(string text, FuTextStyle style, Vector2 size, FuTextWrapping wrapping = FuTextWrapping.None)
        {
            beginElement(ref text, style, true); //apply the style to the element
            // return if item must no be draw
            if (!_drawElement)
            {
                return;
            }

            _text(text, wrapping, size); //display the text
            // set states for this element
            setBaseElementState(text, _currentItemStartPos, ImGui.GetItemRectSize(), false, false);
            if (_currentToolTipsOnLabels) //if the current tooltips should be displayed on labels
            {
                displayToolTip(); //display the current tooltips
            }
            endElement(style); //remove the style from the element
        }
        #endregion

        #region Smart
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
            SmartText(text, FuTextStyle.Default);
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
        public virtual void SmartText(string text, FuTextStyle style)
        {
            beginElement(ref text, style, true);
            // return if item must no be draw
            if (!_drawElement)
            {
                return;
            }

            _customText(text);
            // set states for this element
            setBaseElementState(text, _currentItemStartPos, ImGui.GetItemRectMax() - _currentItemStartPos, true, false);
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
            text = Fugui.GetUntagedText(text);
            int size = 14;
            Vector4 color = FuThemeManager.GetColor(FuColors.Text);
            float startCursorX = ImGui.GetCursorPosX();
            bool bold = false;
            int nbFontPush = 0;
            int nbColorPush = 0;
            bool firstChunk = true;
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
                            Fugui.PushFont(size, FontType.Bold);
                            nbFontPush++;
                        }
                        else if (tag == "/b")
                        {
                            if (nbFontPush > 0)
                            {
                                nbFontPush--;
                                Fugui.PopFont();
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
                                Fugui.PopFont();
                            }
                        }
                        else if (tag.StartsWith("size="))
                        {
                            size = int.Parse(tag.Substring("size=".Length));
                            if (bold)
                            {
                                Fugui.PushFont(size, FontType.Bold);
                                nbFontPush++;
                            }
                            else
                            {
                                Fugui.PushFont(size, FontType.Regular);
                                nbFontPush++;
                            }
                        }

                        // COLOR tag
                        else if (tag == "/color")
                        {
                            if (nbColorPush > 0)
                            {
                                Fugui.PopColor();
                                nbColorPush--;
                            }
                        }
                        else if (tag.StartsWith("color="))
                        {
                            if (ColorUtility.TryParseHtmlString(tag.Substring("color=".Length), out Color col))
                            {
                                color = col;
                            }
                            if (LastItemDisabled)
                            {
                                color *= 0.5f;
                            }
                            Fugui.Push(ImGuiCol.Text, color);
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
                    float txtWidth = ImGui.CalcTextSize(sb.ToString()).x;
                    // assumle we always are on same line (simpler that derterminate it at this point)
                    if (!firstChunk)
                    {
                        ImGui.SameLine();
                    }
                    firstChunk = false;
                    // check whatever wee pass throw right max
                    if (ImGui.GetContentRegionAvail().x < txtWidth + 4f)
                    {
                        // if true, line break
                        ImGui.Dummy(Vector2.zero);
                    }
                    else
                    {
                        // if not, place X cusror to minus 3, because it's a hard ImGui Text padding
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 3f);
                    }
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
                Fugui.PopFont();
                nbFontPush--;
            }
        }
        #endregion

        #region Frammed
        /// <summary>
        /// Draw a text inside a frame background
        /// </summary>
        /// <param name="text">text to draw</param>
        /// <param name="wrapping">However you want to wrapp the text</param>
        public void FramedText(string text, float alignment = 0.5f, FuTextWrapping wrapping = FuTextWrapping.Clip)
        {
            _customFramedText(text, alignment, new Vector2(-1f, 0f), FuFrameStyle.Default, wrapping, 0f, 0f);
        }

        /// <summary>
        /// Draw a text inside a frame background
        /// </summary>
        /// <param name="text">text to draw</param>
        /// <param name="size">size of the frame</param>
        /// <param name="wrapping">However you want to wrapp the text</param>
        public void FramedText(string text, FuElementSize size, float alignment = 0.5f, FuTextWrapping wrapping = FuTextWrapping.Clip)
        {
            _customFramedText(text, alignment, size, FuFrameStyle.Default, wrapping, 0f, 0f);
        }

        /// <summary>
        /// Draw a text inside a frame background
        /// </summary>
        /// <param name="text">text to draw</param>
        /// <param name="size">size of the frame</param>
        /// <param name="style">style of the frame and text</param>
        /// <param name="wrapping">However you want to wrapp the text</param>
        public void FramedText(string text, FuElementSize size, FuFrameStyle style, float alignment = 0.5f, FuTextWrapping wrapping = FuTextWrapping.Clip, float leftPadding = 0f, float rightPadding = 0f)
        {
            _customFramedText(text, alignment, size, style, wrapping, leftPadding, rightPadding);
        }

        /// <summary>
        /// Draw a text inside a frame background
        /// </summary>
        /// <param name="text">text to draw</param>
        /// <param name="size">size of the frame</param>
        /// <param name="style">style of the frame and text</param>
        /// <param name="wrapping">However you want to wrapp the text</param>
        private void _customFramedText(string text, float alignment, Vector2 size, FuFrameStyle style, FuTextWrapping wrapping, float leftPadding, float rightPadding)
        {
            beginElement(ref text, style, true);
            if (!_drawElement)
            {
                return;
            }

            Vector2 pos = ImGui.GetCursorScreenPos();
            Vector2 padding = ImGui.GetStyle().FramePadding;
            Vector2 textSize = Fugui.CalcTextSize(text, wrapping, size);

            // calc item size
            Vector2 region_max = default;
            if (size.x < 0.0f || size.y < 0.0f)
                region_max = ImGui.GetContentRegionAvail() + ImGui.GetCursorScreenPos() - ImGui.GetWindowPos();
            if (size.x == 0.0f)
                size.x = textSize.x + padding.x * 2f;
            else if (size.x < 0.0f)
                size.x = Mathf.Max(4.0f, region_max.x - ImGuiNative.igGetCursorPosX() + size.x);
            if (size.y == 0.0f)
                size.y = textSize.y + padding.y * 2f;
            else if (size.y < 0.0f)
                size.y = Mathf.Max(4.0f, region_max.y - ImGuiNative.igGetCursorPosY() + size.y);

            // draw frame
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            drawList.AddRectFilled(pos, pos + size, ImGuiNative.igGetColorU32_Col(ImGuiCol.FrameBg, 1f), FuThemeManager.FrameRounding);
            drawList.AddRect(pos, pos + size, ImGuiNative.igGetColorU32_Col(ImGuiCol.Border, 1f), FuThemeManager.FrameRounding);

            // draw text
            EnboxedText(text, pos + new Vector2(leftPadding, 0f), size - new Vector2(leftPadding + rightPadding, 0f), padding, Vector2.zero, new Vector2(alignment, 0.5f), wrapping);

            // fake dummy
            Rect rect = new Rect(pos, Fugui.CalcTextSize(text, wrapping, size - (padding * 2f)));
            ImGui.Dummy(size);
            bool hovered;
            if (FuWindow.CurrentDrawingWindow != null)
            {
                hovered = ImGui.IsMouseHoveringRect(rect.min, rect.max) && FuWindow.CurrentDrawingWindow.IsHoveredContent;
            }
            else
            {
                hovered = ImGui.IsMouseHoveringRect(rect.min, rect.max);
            }

            // set states for this element
            setBaseElementState(text, _currentItemStartPos, ImGui.GetItemRectMax() - _currentItemStartPos, true, false);
            // tooltip and end element
            if (_currentToolTipsOnLabels)
            {
                displayToolTip(hovered);
            }
            endElement(style);
        }
        #endregion

        #region Enboxed
        /// <summary>
        /// Draw a text inside a box
        /// </summary>
        /// <param name="text">text to draw</param>
        /// <param name="boxPos">position of the box</param>
        /// <param name="boxSize">size of the box</param>
        /// <param name="padding">padding of the text rect inside the box</param>
        /// <param name="offset">offset of the text (optional)</param>
        /// <param name="alignment">alignement of the text</param>
        /// <param name="wrapping">text wrapping behaviour</param>
        public void EnboxedText(string text, Vector2 boxPos, Vector2 boxSize, Vector2 padding, Vector2 offset, Vector2 alignment, FuTextWrapping wrapping)
        {
            offset *= Fugui.CurrentContext.Scale;
            Vector2 maxTextSize = boxSize - (padding * 2f);
            Vector2 textSize = Fugui.CalcTextSize(text, wrapping, maxTextSize);
            Vector2 text_offset = maxTextSize - textSize;
            Vector2 text_pos = boxPos + padding + offset + new Vector2(Mathf.Lerp(0f, text_offset.x, Mathf.Clamp01(alignment.x)), Mathf.Lerp(0f, text_offset.y, Mathf.Clamp01(alignment.y)));
            _text(text, wrapping, maxTextSize, text_pos, false);
        }

        /// <summary>
        /// Get the position of a text that would be inside a the given box
        /// </summary>
        /// <param name="text">text to draw</param>
        /// <param name="boxPos">position of the box</param>
        /// <param name="boxSize">size of the box</param>
        /// <param name="padding">padding of the text rect inside the box</param>
        /// <param name="offset">offset of the text (optional)</param>
        /// <param name="alignment">alignement of the text</param>
        /// <param name="wrapping">text wrapping behaviour</param>
        public Vector2 GetEnboxedTextPosition(string text, Vector2 boxPos, Vector2 boxSize, Vector2 padding, Vector2 offset, Vector2 alignment, FuTextWrapping wrapping)
        {
            offset *= Fugui.CurrentContext.Scale;
            Vector2 maxTextSize = boxSize - (padding * 2f);
            Vector2 textSize = Fugui.CalcTextSize(text, wrapping, maxTextSize);
            Vector2 text_offset = maxTextSize - textSize;
            return boxPos + padding + offset + new Vector2(Mathf.Lerp(0f, text_offset.x, Mathf.Clamp01(alignment.x)), Mathf.Lerp(0f, text_offset.y, Mathf.Clamp01(alignment.y)));
        }
        #endregion

        #region Clickable
        /// <summary>
        /// Draw a clickable text element
        /// </summary>
        /// <param name="text">text to draw</param>
        /// <returns>whatever the text is clicked</returns>
        public virtual bool ClickableText(string text)
        {
            return ClickableText(text, FuTextStyle.Default);
        }

        /// <summary>
        /// Draw a clickable text element
        /// </summary>
        /// <param name="text">text to draw</param>
        /// <param name="style">style of the text to draw</param>
        /// <param name="wrapping">However you want to wrapp the text</param>
        /// <returns>whatever the text is clicked</returns>
        public virtual bool ClickableText(string text, FuTextStyle style, FuTextWrapping wrapping = FuTextWrapping.None)
        {
            string id = text;
            text = Fugui.GetUntagedText(text);
            beginElement(ref id, style, true);
            // return if item must no be draw
            if (!_drawElement)
            {
                return false;
            }

            bool clicked = _internalClickableText(text, style, out Rect textRect, wrapping);
            // set states for this element
            setBaseElementState(id, textRect.min, textRect.size, true, false);
            if (_currentToolTipsOnLabels)
            {
                displayToolTip();
            }
            endElement(style);
            return clicked;
        }

        private bool _internalClickableText(string text, FuTextStyle style, out Rect textRect, FuTextWrapping wrapping)
        {
            textRect = new Rect(ImGui.GetCursorScreenPos() - new Vector2(4f, 0f), Fugui.CalcTextSize(text, wrapping) + FuThemeManager.FramePadding);
            bool hovered = IsItemHovered(textRect.min, textRect.size);
            bool active = hovered && ImGui.IsMouseDown(ImGuiMouseButton.Left);
            bool clicked = hovered && ImGui.IsMouseReleased(ImGuiMouseButton.Left) && !LastItemDisabled;

            // set mouse cursor
            if (hovered && !LastItemDisabled)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            Color textColor = LastItemDisabled ? style.DisabledText : style.Text;
            if (!LastItemDisabled)
            {
                if (active)
                {
                    textColor *= 0.8f;
                }
                else if (hovered)
                {
                    textColor *= 0.9f;
                }
            }
            Fugui.Push(ImGuiCol.Text, textColor);
            _text(text, wrapping, Vector2.zero);
            Fugui.PopColor();
            return clicked;
        }

        /// <summary>
        /// Draw a clickable URL text element
        /// </summary>
        /// <param name="text">text to draw</param>
        /// <param name="URL">URL to open on text click</param>
        /// <param name="wrapping">However you want to wrapp the text</param>
        /// <returns>whatever the text is clicked</returns>
        public virtual void TextURL(string text, string URL, FuTextWrapping wrapping = FuTextWrapping.None)
        {
            TextURL(text, URL, FuTextStyle.Default);
        }

        /// <summary>
        /// Draw a clickable URL text element
        /// </summary>
        /// <param name="text">text to draw</param>
        /// <param name="URL">URL to open on text click</param>
        /// <param name="style">style of the text to draw</param>
        /// <param name="wrapping">However you want to wrapp the text</param>
        /// <returns>whatever the text is clicked</returns>
        public virtual void TextURL(string text, string URL, FuTextStyle style, FuTextWrapping wrapping = FuTextWrapping.None)
        {
            beginElement(ref text, style, true);
            // return if item must no be draw
            if (!_drawElement)
            {
                return;
            }

            Vector2 rectMin = ImGui.GetCursorScreenPos() - new Vector2(4f, 0f);
            Vector2 rectMax = rectMin + Fugui.CalcTextSize(text, wrapping) + FuThemeManager.FramePadding;
            bool hovered = ImGui.IsMouseHoveringRect(rectMin, rectMax);
            bool active = hovered && ImGui.IsMouseDown(ImGuiMouseButton.Left);
            bool clicked = hovered && ImGui.IsMouseReleased(ImGuiMouseButton.Left) && !LastItemDisabled;

            Color textColor = LastItemDisabled ? style.DisabledText : style.LinkText;
            if (!LastItemDisabled)
            {
                if (active)
                {
                    textColor *= 0.8f;
                }
                else if (hovered)
                {
                    textColor *= 0.9f;
                }
            }
            Fugui.Push(ImGuiCol.Text, textColor);
            _text(text, wrapping, Vector2.zero);
            if (hovered)
            {
                if (!LastItemDisabled)
                {
                    // set mouse cursor
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                    // underline the hovered text
                    AddUnderLine();
                }
            }
            Fugui.PopColor();

            // open the URL on click
            if (clicked)
            {
                try
                {
                    Process.Start(URL);
                }
                catch (Exception ex)
                {
                    Fugui.Notify("fail to open URI", ex.Message, StateType.Danger);
                }
            }

            // set states for this element
            setBaseElementState(text, _currentItemStartPos, ImGui.GetItemRectMax() - _currentItemStartPos, true, clicked);
            if (_currentToolTipsOnLabels)
            {
                displayToolTip();
            }
            endElement(style);
        }
        #endregion

        #region Clipped
        /// <summary>
        /// Draw a text that clip and replace the end by '...' if there is not enought place to fully display it
        /// The text will not displace cursor, so please do it yourself if needed
        /// </summary>
        /// <param name="text">text to draw</param>
        /// <param name="maxWidth">maximum size of the text</param>
        public unsafe void TextClipped_Fast(string text, float maxWidth)
        {
            TextClipped_Fast(new Vector2(maxWidth, 0f), text, ImGui.GetCursorScreenPos(), Vector2.zero, ImGui.CalcTextSize(text, true), new Vector2(0f, 0.5f), FuTextStyle.Default);
        }

        /// <summary>
        /// Draw a text that clip and replace the end by '...' if there is not enought place to fully display it
        /// The text will not displace cursor, so please do it yourself if needed
        /// </summary>
        /// <param name="text">text to draw</param>
        /// <param name="maxWidth">maximum size of the text</param>
        /// <param name="alignment">the Horizontal alignment of the text (between 0f and 1f)</param>
        public unsafe void TextClipped_Fast(string text, float maxWidth, float alignment)
        {
            TextClipped_Fast(new Vector2(maxWidth, 0f), text, ImGui.GetCursorScreenPos(), Vector2.zero, ImGui.CalcTextSize(text, true), new Vector2(alignment, 0.5f), FuTextStyle.Default);
        }

        /// <summary>
        /// Draw a text that clip and replace the end by '...' if there is not enought place to fully display it
        /// The text will not displace cursor, so please do it yourself if needed
        /// </summary>
        /// <param name="maxSize">maximum size of the text</param>
        /// <param name="text">text to draw</param>
        /// <param name="padding">padding of the text inside the rect (pos + maxSize)</param>
        public unsafe void TextClipped_Fast(Vector2 maxSize, string text, Vector2 padding)
        {
            TextClipped_Fast(maxSize, text, ImGui.GetCursorScreenPos(), padding, ImGui.CalcTextSize(text, true), new Vector2(0f, 0.5f), FuTextStyle.Default);
        }

        /// <summary>
        /// Draw a text that clip and replace the end by '...' if there is not enought place to fully display it
        /// The text will not displace cursor, so please do it yourself if needed
        /// </summary>
        /// <param name="maxSize">maximum size of the text</param>
        /// <param name="text">text to draw</param>
        /// <param name="padding">padding of the text inside the rect (pos + maxSize)</param>
        /// <param name="text_size">size of the text</param>
        public unsafe void TextClipped_Fast(Vector2 maxSize, string text, Vector2 padding, Vector2 text_size)
        {
            TextClipped_Fast(maxSize, text, ImGui.GetCursorScreenPos(), padding, text_size, new Vector2(0f, 0.5f), FuTextStyle.Default);
        }

        /// <summary>
        /// Draw a text that clip and replace the end by '...' if there is not enought place to fully display it
        /// The text will not displace cursor, so please do it yourself if needed
        /// </summary>
        /// <param name="maxSize">maximum size of the text</param>
        /// <param name="text">text to draw</param>
        /// <param name="pos">position of the text (screen space)</param>
        /// <param name="padding">padding of the text inside the rect (pos + maxSize)</param>
        /// <param name="text_size">size of the text</param>
        public unsafe void TextClipped_Fast(Vector2 maxSize, string text, Vector2 padding, Vector2 text_size, Vector2 pos)
        {
            TextClipped_Fast(maxSize, text, pos, padding, text_size, new Vector2(0f, 0.5f), FuTextStyle.Default);
        }

        /// <summary>
        /// Draw a text that clip and replace the end by '...' if there is not enought place to fully display it
        /// The text will not displace cursor, so please do it yourself if needed
        /// </summary>
        /// <param name="maxSize">maximum size of the text</param>
        /// <param name="text">text to draw</param>
        /// <param name="pos">position of the text (screen space)</param>
        /// <param name="padding">padding of the text inside the rect (pos + maxSize)</param>
        /// <param name="text_size">size of the text</param>
        /// <param name="style">The style to apply to the text.</param>
        public unsafe void TextClipped_Fast(Vector2 maxSize, string text, Vector2 padding, Vector2 text_size, Vector2 pos, FuTextStyle style)
        {
            TextClipped_Fast(maxSize, text, pos, padding, text_size, new Vector2(0f, 0.5f), style);
        }

        /// <summary>
        /// Draw a text that clip and replace the end by '...' if there is not enought place to fully display it
        /// The text will not displace cursor, so please do it yourself if needed
        /// </summary>
        /// <param name="maxSize">maximum size of the text</param>
        /// <param name="text">text to draw</param>
        /// <param name="pos">position of the text (screen space)</param>
        /// <param name="padding">padding of the text inside the rect (pos + maxSize)</param>
        /// <param name="text_size">size of the text</param>
        /// <param name="alignment">text alignement (between 0f and 1f)</param>
        /// <param name="style">The style to apply to the text.</param>
        internal unsafe void TextClipped_Fast(Vector2 maxSize, string text, Vector2 pos, Vector2 padding, Vector2 text_size, Vector2 alignment, FuTextStyle style)
        {
            beginElement(ref text, style, true);
            if (!_drawElement)
            {
                return;
            }
            Vector2 size = _customTextClipped(maxSize, text, pos, padding, text_size, alignment, style);
            Rect rect = new Rect(pos, size);
            ImGui.Dummy(size);
            bool hovered;
            if (FuWindow.CurrentDrawingWindow != null)
            {
                hovered = ImGui.IsMouseHoveringRect(rect.min, rect.max) && FuWindow.CurrentDrawingWindow.IsHoveredContent;
            }
            else
            {
                hovered = ImGui.IsMouseHoveringRect(rect.min, rect.max);
            }
            // set states for this element
            setBaseElementState(text, pos, size, true, false);
            if (_currentToolTipsOnLabels)
            {
                displayToolTip(hovered);
            }
            endElement(style);
        }

        /// <summary>
        /// Draw a text that clip and replace the end by '...' if there is not enought place to fully display it
        /// The text will not displace cursor, so please do it yourself if needed
        /// </summary>
        /// <param name="maxSize">maximum size of the text</param>
        /// <param name="label">text to draw</param>
        /// <param name="pos">position of the text (screen space)</param>
        /// <param name="padding">padding of the text inside the rect (pos + maxSize)</param>
        /// <param name="text_size">size of the text</param>
        /// <param name="alignment">text alignement (between 0f and 1f)</param>
        internal unsafe Vector2 _customTextClipped(Vector2 maxSize, string label, Vector2 pos, Vector2 padding, Vector2 text_size, Vector2 alignment, FuTextStyle style)
        {
            style.Push(!LastItemDisabled);
            // get maxSize Y 
            if (maxSize.y <= 0f)
            {
                maxSize.y = text_size.y;
            }
            Vector2 size;

            // we need to crop the text, so let's reduce the size so we draw dots at the end
            if (text_size.x > maxSize.x - padding.x * 2f)
            {
                // get dots size
                Vector2 dotsSize = ImGui.CalcTextSize("...");
                dotsSize.y = 0f;
                dotsSize.x += 2f;
                // reduce max size
                maxSize -= dotsSize;
                // draw the clipped text
                _customTextClipped(pos + padding, (pos + maxSize) - padding, label, text_size, alignment, new ImRect(pos, pos + maxSize), style);
                // draw dots
                size = maxSize + dotsSize;
                maxSize.y = 0;
                ImGui.GetWindowDrawList().AddText(pos + maxSize, ImGui.GetColorU32(ImGuiCol.Text), "...");
            }
            else
            {
                // draw the clipped text
                size = _customTextClipped(pos + padding, (pos + maxSize) - padding, label, text_size, alignment, new ImRect(pos, pos + maxSize), style);
            }
            style.Pop();
            return size;
        }

        private unsafe Vector2 _customTextClipped(Vector2 pos_min, Vector2 pos_max, string label, Vector2 text_size_if_known, Vector2 align, ImRect clip_rect, FuTextStyle style)
        {
            style.Push(!LastItemDisabled);
            // get str ptr
            int num = 0;
            byte* ptr = null;
            if (label != null)
            {
                num = Encoding.UTF8.GetByteCount(label);
                ptr = Util.Allocate(num + 1);
                int utf = Util.GetUtf8(label, ptr, num);
                ptr[utf] = 0;
            }

            // render cliped text
            ImGuiInternal.igRenderTextClipped(pos_min, pos_max, ptr, null, &text_size_if_known, align, &clip_rect);

            if (num > 2048)
            {
                Util.Free(ptr);
            }
            Vector2 size = pos_max - pos_min;
            style.Pop();
            return size;
        }
        #endregion

        /// <summary>
        /// Draw a text with wrapping type and Duotone icons handling
        /// </summary>
        /// <param name="text">text to draw</param>
        /// <param name="wrapping">However you want to wrapp the text</param>
        /// <param name="maxSize">maximum size (for clipping or wrapping). Keep Vector2.zero to use maximum available region</param>
        /// <param name="moveCursor">Whatever you want to move cursor or just render text</param>
        internal void _text(string text, FuTextWrapping wrapping, Vector2 maxSize, bool moveCursor = true)
        {
            _text(text, wrapping, maxSize, ImGui.GetCursorScreenPos(), moveCursor);
        }

        /// <summary>
        /// Draw a text with wrapping type and Duotone icons handling
        /// </summary>
        /// <param name="text">text to draw</param>
        /// <param name="wrapping">However you want to wrapp the text</param>
        /// <param name="maxSize">maximum size (for clipping or wrapping). Keep Vector2.zero to use maximum available region</param>
        /// <param name="position">position to draw text</param>
        /// <param name="moveCursor">Whatever you want to move cursor or just render text</param>
        internal void _text(string text, FuTextWrapping wrapping, Vector2 maxSize, Vector2 position, bool moveCursor = true)
        {
            text = Fugui.GetUntagedText(text);
            _textChunkStringBuilder.Clear();
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 currentPosition = position;
            uint textColor = ImGui.GetColorU32(ImGuiCol.Text);
            bool cancel = false;
            maxSize.x = maxSize.x == 0f ? ImGui.GetContentRegionAvail().x : maxSize.x;
            maxSize.y = maxSize.y == 0f ? ImGui.GetContentRegionAvail().y : maxSize.y;
            float maxX = currentPosition.x + maxSize.x;
            Vector2 currentLineSize = Vector2.zero;
            float fullTextHeight = ImGui.GetTextLineHeight();
            float fullTextWidth = 0f;

            for (int i = 0; i < text.Length; i++)
            {
                if (cancel)
                {
                    //drawList.AddRect(position, position + new Vector2(fullTextWidth, fullTextHeight), ImGui.GetColorU32(Color.red));
                    //drawList.AddRect(position, position + CalcTextSize(text, wrapping, maxSize), ImGui.GetColorU32(Color.blue));
                    return;
                }

                if (Fugui.IsDuoToneChar(text[i]))
                {
                    _renderTextChunk();
                    _renderDuotone(text[i]);
                }
                else
                {
                    _textChunkStringBuilder.Append(text[i]);
                    switch (wrapping)
                    {
                        default:
                            switch (text[i])
                            {
                                case '\n':
                                    fullTextHeight += ImGui.GetTextLineHeight();
                                    break;
                            }
                            break;

                        case FuTextWrapping.Wrap:
                            switch (text[i])
                            {
                                case ' ':
                                case '-':
                                case '_':
                                case '\n':
                                    _renderTextChunk();
                                    break;
                            }
                            break;
                    }
                }
            }

            _renderTextChunk();
            if (moveCursor)
            {
                ImGui.Dummy(new Vector2(fullTextWidth, fullTextHeight));
            }

            //drawList.AddRect(position, position + new Vector2(fullTextWidth, fullTextHeight), ImGui.GetColorU32(Color.red));
            //drawList.AddRect(position, position + CalcTextSize(text, wrapping, maxSize), ImGui.GetColorU32(Color.blue));

            void _renderTextChunk()
            {
                if (_textChunkStringBuilder.Length == 0)
                {
                    return;
                }

                string textChunk = _textChunkStringBuilder.ToString();
                _textChunkStringBuilder.Clear();
                Vector2 chunkSize = ImGui.CalcTextSize(textChunk);

                _beginDrawText(chunkSize);

                // draw clipped text if wrapping is in clipping mode and text chunk is too long
                switch (wrapping)
                {
                    case FuTextWrapping.Clip:
                        if (currentPosition.x + chunkSize.x > maxX)
                        {
                            float h = GetAvailableHeight();
                            if (h <= 0f)
                            {
                                h = ImGui.GetTextLineHeightWithSpacing();
                            }
                            ImRect rect = new ImRect(currentPosition, new Vector2(maxX, h + position.y));

                            // get dots size
                            Vector2 dotsSize = ImGui.CalcTextSize("...");
                            dotsSize.y = 0f;
                            dotsSize.x += 2f;
                            // reduce max size
                            rect.Max -= dotsSize;
                            // draw the clipped text
                            _customTextClipped(rect.Min, rect.Max, textChunk, chunkSize, Vector2.zero, rect);
                            // draw dots
                            rect.Max.y = currentPosition.y;
                            drawList.AddText(rect.Max, ImGui.GetColorU32(ImGuiCol.Text), "...");
                            _endDrawText(chunkSize);
                            return;
                        }
                        break;

                    // prevent drawing next line if wrap text is out of max size height
                    case FuTextWrapping.Wrap:
                        if (fullTextHeight > maxSize.y)
                        {
                            cancel = true;
                            return;
                        }
                        break;
                }

                // draw text chunk
                drawList.AddText(currentPosition, textColor, textChunk);
                _endDrawText(chunkSize);
            }

            void _renderDuotone(char icon)
            {
                // get secondaty char
                char secondary = (char)(((ushort)icon) + 1);
                // get both char sized
                Vector2 primarySize = ImGui.CalcTextSize(icon.ToString());
                Vector2 secondarySize = ImGui.CalcTextSize(secondary.ToString());
                // get full icon size
                Vector2 iconSize = new Vector2(Mathf.Max(primarySize.x, secondarySize.x), Mathf.Max(primarySize.y, secondarySize.y));

                _beginDrawText(iconSize);

                // render primary and secondary glyphs
                drawList.AddText(currentPosition, Fugui.GetPrimaryDuotoneColor(LastItemDisabled), icon.ToString());
                drawList.AddText(currentPosition, Fugui.GetSecondaryDuotoneColor(LastItemDisabled), secondary.ToString());

                _endDrawText(iconSize);
            }

            void _beginDrawText(Vector2 size)
            {
                switch (wrapping)
                {
                    default:
                        break;

                    case FuTextWrapping.Wrap:
                        if (currentPosition.x + size.x > maxX && size.x < maxSize.x)
                        {
                            currentPosition.x = position.x;
                            currentPosition.y += ImGui.GetTextLineHeight();
                            currentLineSize = Vector2.zero;
                            fullTextHeight += ImGui.GetTextLineHeight();
                        }
                        break;

                    case FuTextWrapping.Clip:
                        if (currentPosition.x + size.x > maxX && size.x < maxSize.x)
                        {
                            cancel = true;
                        }
                        break;
                }
            }

            void _endDrawText(Vector2 size)
            {
                currentPosition.x += size.x;
                currentLineSize.x += size.x;
                fullTextWidth = Mathf.Max(fullTextWidth, currentLineSize.x);
                currentLineSize.y = Mathf.Max(size.y, currentLineSize.y);
            }

            unsafe void _customTextClipped(Vector2 pos_min, Vector2 pos_max, string label, Vector2 text_size_if_known, Vector2 align, ImRect clip_rect)
            {
                // get str ptr
                int num = 0;
                byte* ptr = null;
                if (label != null)
                {
                    num = Encoding.UTF8.GetByteCount(label);
                    ptr = Util.Allocate(num + 1);
                    int utf = Util.GetUtf8(label, ptr, num);
                    ptr[utf] = 0;
                }

                // render cliped text
                ImGuiInternal.igRenderTextClipped(pos_min, pos_max, ptr, null, &text_size_if_known, align, &clip_rect);

                if (num > 2048)
                {
                    Util.Free(ptr);
                }
            }
        }
    }
}