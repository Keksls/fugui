﻿using ImGuiNET;
using System.Text;
using UnityEngine;

namespace Fu.Framework
{
    public partial class FuLayout
    {
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
            if (_currentToolTipsOnLabels) //if the current tooltips should be displayed on labels
            {
                displayToolTip(); //display the current tooltips
            }
            endElement(style); //remove the style from the element
        }

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
            beginElement(ref text, style);
            // return if item must no be draw
            if (!_drawElement)
            {
                return;
            }

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
            Vector4 color = FuThemeManager.GetColor(FuColors.Text);
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

        /// <summary>
        /// Draw a text that clip and replace the end by '...' if there is not enought place to fully display it
        /// The text will not displace cursor, so please do it yourself if needed
        /// </summary>
        /// <param name="maxSize">maximum size of the text</param>
        /// <param name="label">text to draw</param>
        public static unsafe void TextClipped(Vector2 maxSize, string label)
        {
            TextClipped(maxSize, label, ImGui.GetCursorScreenPos(), Vector2.zero, ImGui.CalcTextSize(label, true), new Vector2(0f, 0.5f));
        }

        /// <summary>
        /// Draw a text that clip and replace the end by '...' if there is not enought place to fully display it
        /// The text will not displace cursor, so please do it yourself if needed
        /// </summary>
        /// <param name="maxSize">maximum size of the text</param>
        /// <param name="label">text to draw</param>
        /// <param name="padding">padding of the text inside the rect (pos + maxSize)</param>
        public static unsafe void TextClipped(Vector2 maxSize, string label, Vector2 padding)
        {
            TextClipped(maxSize, label, ImGui.GetCursorScreenPos(), padding, ImGui.CalcTextSize(label, true), new Vector2(0f, 0.5f));
        }

        /// <summary>
        /// Draw a text that clip and replace the end by '...' if there is not enought place to fully display it
        /// The text will not displace cursor, so please do it yourself if needed
        /// </summary>
        /// <param name="maxSize">maximum size of the text</param>
        /// <param name="label">text to draw</param>
        /// <param name="padding">padding of the text inside the rect (pos + maxSize)</param>
        /// <param name="text_size">size of the text</param>
        public static unsafe void TextClipped(Vector2 maxSize, string label, Vector2 padding, Vector2 text_size)
        {
            TextClipped(maxSize, label, ImGui.GetCursorScreenPos(), padding, text_size, new Vector2(0f, 0.5f));
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
        public static unsafe void TextClipped(Vector2 maxSize, string label, Vector2 padding, Vector2 text_size, Vector2 pos)
        {
            TextClipped(maxSize, label, pos, padding, text_size, new Vector2(0f, 0.5f));
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
        public static unsafe void TextClipped(Vector2 maxSize, string label, Vector2 pos, Vector2 padding, Vector2 text_size, Vector2 alignment)
        {
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
                TextClipped(pos + padding, (pos + maxSize) - padding, label, text_size, alignment, new ImRect(pos, pos + maxSize));
                // draw dots
                maxSize.y = 4f;
                ImGui.GetWindowDrawList().AddText(pos + maxSize, ImGui.GetColorU32(ImGuiCol.Text), "...");
            }
            else
            {
                // draw the clipped text
                TextClipped(pos + padding, (pos + maxSize) - padding, label, text_size, alignment, new ImRect(pos, pos + maxSize));
            }
        }

        private static unsafe void TextClipped(Vector2 pos_min, Vector2 pos_max, string label, Vector2 text_size_if_known, Vector2 align, ImRect clip_rect)
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