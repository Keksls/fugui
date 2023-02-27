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
        #region Text
        /// <summary>
        /// Displays a text string.
        /// </summary>
        /// <param name="text">The string to display.</param>
        public void Text(string text)
        {
            Text(text, FuTextStyle.Default);
        }

        /// <summary>
        /// Displays a text string with the specified style.
        /// </summary>
        /// <param name="text">The string to display.</param>
        /// <param name="style">The style to apply to the text.</param>
        public virtual void Text(string text, FuTextStyle style)
        {
            beginElement(ref text, style, true); //apply the style to the element
            // return if item must no be draw
            if (!_drawElement)
            {
                return;
            }

            // verticaly align text to frame padding
            ImGui.AlignTextToFramePadding();
            ImGui.Text(text); //display the text
            // set states for this element
            setBaseElementState(text, _currentItemStartPos, ImGui.GetItemRectMax() - _currentItemStartPos, true, false);
            if (_currentToolTipsOnLabels) //if the current tooltips should be displayed on labels
            {
                displayToolTip(); //display the current tooltips
            }
            endElement(style); //remove the style from the element
        }
        #endregion

        #region Wrapped
        /// <summary>
        /// Displays a text string.
        /// </summary>
        /// <param name="text">The string to display.</param>
        public void TextWrapped(string text)
        {
            TextWrapped(text, FuTextStyle.Default);
        }

        /// <summary>
        /// Displays a text string with the specified style.
        /// </summary>
        /// <param name="text">The string to display.</param>
        /// <param name="style">The style to apply to the text.</param>
        public virtual void TextWrapped(string text, FuTextStyle style)
        {
            beginElement(ref text, style, true); //apply the style to the element
            // return if item must no be draw
            if (!_drawElement)
            {
                return;
            }

            // verticaly align text to frame padding
            ImGui.AlignTextToFramePadding();
            ImGui.TextWrapped(text); //display the text
            // set states for this element
            setBaseElementState(text, _currentItemStartPos, ImGui.GetItemRectMax() - _currentItemStartPos, true, false);
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
                    ImGui.SameLine();
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
                Fugui.PopFont();
                nbFontPush--;
            }
        }
        #endregion

        #region Clipped
        /// <summary>
        /// Draw a text that clip and replace the end by '...' if there is not enought place to fully display it
        /// The text will not displace cursor, so please do it yourself if needed
        /// </summary>
        /// <param name="text">text to draw</param>
        /// <param name="maxWidth">maximum size of the text</param>
        public unsafe void TextClipped(string text, float maxWidth)
        {
            TextClipped(new Vector2(maxWidth, 0f), text, ImGui.GetCursorScreenPos(), Vector2.zero, ImGui.CalcTextSize(text, true), new Vector2(0f, 0.5f), FuTextStyle.Default);
        }

        /// <summary>
        /// Draw a text that clip and replace the end by '...' if there is not enought place to fully display it
        /// The text will not displace cursor, so please do it yourself if needed
        /// </summary>
        /// <param name="text">text to draw</param>
        /// <param name="maxWidth">maximum size of the text</param>
        /// <param name="alignment">the Horizontal alignment of the text (between 0f and 1f)</param>
        public unsafe void TextClipped(string text, float maxWidth, float alignment)
        {
            TextClipped(new Vector2(maxWidth, 0f), text, ImGui.GetCursorScreenPos(), Vector2.zero, ImGui.CalcTextSize(text, true), new Vector2(alignment, 0.5f), FuTextStyle.Default);
        }

        /// <summary>
        /// Draw a text that clip and replace the end by '...' if there is not enought place to fully display it
        /// The text will not displace cursor, so please do it yourself if needed
        /// </summary>
        /// <param name="maxSize">maximum size of the text</param>
        /// <param name="text">text to draw</param>
        /// <param name="padding">padding of the text inside the rect (pos + maxSize)</param>
        public unsafe void TextClipped(Vector2 maxSize, string text, Vector2 padding)
        {
            TextClipped(maxSize, text, ImGui.GetCursorScreenPos(), padding, ImGui.CalcTextSize(text, true), new Vector2(0f, 0.5f), FuTextStyle.Default);
        }

        /// <summary>
        /// Draw a text that clip and replace the end by '...' if there is not enought place to fully display it
        /// The text will not displace cursor, so please do it yourself if needed
        /// </summary>
        /// <param name="maxSize">maximum size of the text</param>
        /// <param name="text">text to draw</param>
        /// <param name="padding">padding of the text inside the rect (pos + maxSize)</param>
        /// <param name="text_size">size of the text</param>
        public unsafe void TextClipped(Vector2 maxSize, string text, Vector2 padding, Vector2 text_size)
        {
            TextClipped(maxSize, text, ImGui.GetCursorScreenPos(), padding, text_size, new Vector2(0f, 0.5f), FuTextStyle.Default);
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
        public unsafe void TextClipped(Vector2 maxSize, string text, Vector2 padding, Vector2 text_size, Vector2 pos)
        {
            TextClipped(maxSize, text, pos, padding, text_size, new Vector2(0f, 0.5f), FuTextStyle.Default);
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
        internal unsafe void TextClipped(Vector2 maxSize, string text, Vector2 pos, Vector2 padding, Vector2 text_size, Vector2 alignment, FuTextStyle style)
        {
            beginElement(ref text, style, true);
            if (!_drawElement)
            {
                return;
            }
            Vector2 size = _customTextClipped(maxSize, text, pos, padding, text_size, alignment);
            Rect rect = new Rect(ImGui.GetCursorScreenPos(), size);
            ImGui.Dummy(size);
            bool hovered = false;
            if (FuWindow.CurrentDrawingWindow != null)
            {
                hovered = ImGui.IsMouseHoveringRect(rect.min, rect.max) && FuWindow.CurrentDrawingWindow.IsHovered &&
                    !FuWindow.CurrentDrawingWindow.Mouse.IsHoverOverlay && !FuWindow.CurrentDrawingWindow.Mouse.IsHoverPopup;
            }
            else
            {
                hovered = ImGui.IsMouseHoveringRect(rect.min, rect.max);
            }
            // set states for this element
            setBaseElementState(text, _currentItemStartPos, ImGui.GetItemRectMax() - _currentItemStartPos, true, false);
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
        internal unsafe Vector2 _customTextClipped(Vector2 maxSize, string label, Vector2 pos, Vector2 padding, Vector2 text_size, Vector2 alignment)
        {
            // get maxSize Y 
            if (maxSize.y <= 0f)
            {
                maxSize.y = text_size.y;
            }

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
                _customTextClipped(pos + padding, (pos + maxSize) - padding, label, text_size, alignment, new ImRect(pos, pos + maxSize));
                // draw dots
                Vector2 size = maxSize + dotsSize;
                maxSize.y = 0;
                ImGui.GetWindowDrawList().AddText(pos + maxSize, ImGui.GetColorU32(ImGuiCol.Text), "...");
                return size;
            }
            else
            {
                // draw the clipped text
                return _customTextClipped(pos + padding, (pos + maxSize) - padding, label, text_size, alignment, new ImRect(pos, pos + maxSize));
            }
        }

        private unsafe Vector2 _customTextClipped(Vector2 pos_min, Vector2 pos_max, string label, Vector2 text_size_if_known, Vector2 align, ImRect clip_rect)
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
            return pos_max - pos_min;
        }
        #endregion

        #region Frammed
        /// <summary>
        /// Draw a text inside a frame background
        /// </summary>
        /// <param name="text">text to draw</param>
        public void FramedText(string text)
        {
            _customFramedText(text, 0f, new Vector2(-1f, 0f), FuFrameStyle.Default);
        }

        /// <summary>
        /// Draw a text inside a frame background
        /// </summary>
        /// <param name="text">text to draw</param>
        /// <param name="size">size of the frame</param>
        public void FramedText(string text, FuElementSize size)
        {
            _customFramedText(text, 0f, size, FuFrameStyle.Default);
        }

        /// <summary>
        /// Draw a text inside a frame background
        /// </summary>
        /// <param name="text">text to draw</param>
        /// <param name="alignment">horizontal alignment of the text inside the frame.
        /// 0f to 1f, 0 si left, 1 is right, 0.5 is center</param>
        public void FramedText(string text, float alignment = 0f)
        {
            _customFramedText(text, alignment, new Vector2(-1f, 0f), FuFrameStyle.Default);
        }

        /// <summary>
        /// Draw a text inside a frame background
        /// </summary>
        /// <param name="text">text to draw</param>
        /// <param name="size">size of the frame</param>
        /// <param name="alignment">horizontal alignment of the text inside the frame.
        /// 0f to 1f, 0 si left, 1 is right, 0.5 is center</param>
        public void FramedText(string text, FuElementSize size, float alignment = 0f)
        {
            _customFramedText(text, alignment, size, FuFrameStyle.Default);
        }

        /// <summary>
        /// Draw a text inside a frame background
        /// </summary>
        /// <param name="text">text to draw</param>
        /// <param name="size">size of the frame</param>
        /// <param name="alignment">horizontal alignment of the text inside the frame.
        /// 0f to 1f, 0 si left, 1 is right, 0.5 is center</param>
        /// <param name="style">style of the frame and text</param>
        public void FramedText(string text, FuElementSize size, float alignment, FuFrameStyle style)
        {
            _customFramedText(text, alignment, size, style);
        }

        /// <summary>
        /// Draw a text inside a frame background
        /// </summary>
        /// <param name="text">text to draw</param>
        /// <param name="size">size of the frame</param>
        /// <param name="alignment">horizontal alignment of the text inside the frame.
        /// 0f to 1f, 0 si left, 1 is right, 0.5 is center</param>
        /// <param name="style">style of the frame and text</param>
        private void _customFramedText(string text, float alignment, Vector2 size, FuFrameStyle style)
        {
            beginElement(ref text, style, true);
            if (!_drawElement)
            {
                return;
            }

            Vector2 pos = ImGui.GetCursorScreenPos();
            Vector2 padding = ImGui.GetStyle().FramePadding;
            Vector2 textSize = ImGui.CalcTextSize(text, true);
            Vector2 alignmentV2 = new Vector2(Mathf.Clamp01(alignment), 0.5f);

            // calc item size
            Vector2 region_max = default;
            if (size.x < 0.0f || size.y < 0.0f)
                region_max = ImGui.GetContentRegionMax();
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
            drawList.AddRectFilled(pos, pos + size, ImGuiNative.igGetColorU32_Col(ImGuiCol.FrameBg, 1f), FuThemeManager.CurrentTheme.FrameRounding);
            drawList.AddRect(pos, pos + size, ImGuiNative.igGetColorU32_Col(ImGuiCol.Border, 1f), FuThemeManager.CurrentTheme.FrameRounding);

            // draw text
            Vector2 realSize = _customTextClipped(size, text, pos, padding, textSize, alignmentV2);

            // fake dummy
            Rect rect = new Rect(ImGui.GetCursorScreenPos(), realSize);
            ImGui.Dummy(size);
            bool hovered = false;
            if (FuWindow.CurrentDrawingWindow != null)
            {
                hovered = ImGui.IsMouseHoveringRect(rect.min, rect.max) && FuWindow.CurrentDrawingWindow.IsHovered &&
                    !FuWindow.CurrentDrawingWindow.Mouse.IsHoverOverlay && !FuWindow.CurrentDrawingWindow.Mouse.IsHoverPopup;
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

        #region Clickable
        /// <summary>
        /// Draw a clickable text element
        /// </summary>
        /// <param name="text">text to draw</param>
        /// <param name="style">style of the text to draw</param>
        /// <returns>whatever the text is clicked</returns>
        public virtual bool ClickableText(string text, FuTextStyle style)
        {
            beginElement(ref text, style, true);
            // return if item must no be draw
            if (!_drawElement)
            {
                return false;
            }

            Vector2 rectMin = ImGui.GetCursorScreenPos() - new Vector2(4f, 0f);
            Vector2 rectMax = rectMin + ImGui.CalcTextSize(text) + FuThemeManager.CurrentTheme.FramePadding;
            bool hovered = ImGui.IsMouseHoveringRect(rectMin, rectMax);
            bool active = hovered && ImGui.IsMouseDown(ImGuiMouseButton.Left);
            bool clicked = hovered && ImGui.IsMouseReleased(ImGuiMouseButton.Left);

            // set mouse cursor
            if (hovered && !_nextIsDisabled)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            Color textColor = style.Text;
            if (active)
            {
                textColor *= 0.8f;
            }
            else if (hovered)
            {
                textColor *= 0.9f;
            }
            Fugui.Push(ImGuiCol.Text, textColor);
            ImGui.Text(text);
            Fugui.PopColor();
            // set states for this element
            setBaseElementState(text, _currentItemStartPos, ImGui.GetItemRectMax() - _currentItemStartPos, true, false);
            if (_currentToolTipsOnLabels)
            {
                displayToolTip();
            }
            endElement(style);
            return clicked;
        }

        /// <summary>
        /// Draw a clickable URL text element
        /// </summary>
        /// <param name="text">text to draw</param>
        /// <param name="URL">URL to open on text click</param>
        /// <param name="style">style of the text to draw</param>
        /// <returns>whatever the text is clicked</returns>
        public virtual void TextURL(string text, string URL, FuTextStyle style)
        {
            beginElement(ref text, style, true);
            // return if item must no be draw
            if (!_drawElement)
            {
                return;
            }

            Vector2 rectMin = ImGui.GetCursorScreenPos() - new Vector2(4f, 0f);
            Vector2 rectMax = rectMin + ImGui.CalcTextSize(text) + FuThemeManager.CurrentTheme.FramePadding;
            bool hovered = ImGui.IsMouseHoveringRect(rectMin, rectMax);
            bool active = hovered && ImGui.IsMouseDown(ImGuiMouseButton.Left);
            bool clicked = hovered && ImGui.IsMouseReleased(ImGuiMouseButton.Left);

            Color textColor = style.Text;
            if (active)
            {
                textColor *= 0.8f;
            }
            else if (hovered)
            {
                textColor *= 0.9f;
            }
            Fugui.Push(ImGuiCol.Text, textColor);
            ImGui.Text(text);
            if (hovered)
            {
                if (!_nextIsDisabled)
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
    }
}