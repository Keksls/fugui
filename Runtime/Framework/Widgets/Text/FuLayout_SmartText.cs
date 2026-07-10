using Fu;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Fu.Framework
{
    /// <summary>
    /// Contains the cached parser and draw-list renderer used by SmartText.
    /// </summary>
    public partial class FuLayout
    {
        #region SmartText State
        private const int SmartTextCacheLimit = 512;
        private const int SmartTextMaxFontSize = 512;
        private static readonly Dictionary<string, FuSmartTextCacheEntry> _smartTextCache = new Dictionary<string, FuSmartTextCacheEntry>(SmartTextCacheLimit);
        private static readonly Queue<string> _smartTextCacheOrder = new Queue<string>(SmartTextCacheLimit);
        #endregion

        #region SmartText Rendering
        /// <summary>
        /// Draws parsed rich text with the current draw list and advances the ImGui cursor with a single item rect.
        /// </summary>
        /// <param name="text">Tagged text to draw.</param>
        private void DrawSmartText(string text)
        {
            // The raw rich text is parsed once, while layout stays frame-dependent.
            string visibleText = string.IsNullOrEmpty(text) ? string.Empty : Fugui.GetUntagedText(text);
            if (string.IsNullOrEmpty(visibleText))
            {
                ImGui.Dummy(Vector2.zero);
                return;
            }

            FuSmartTextCacheEntry entry = GetSmartTextCacheEntry(visibleText);
            if (entry.Segments.Count == 0)
            {
                ImGui.Dummy(Vector2.zero);
                return;
            }

            RenderSmartText(entry);
        }

        /// <summary>
        /// Renders a cached SmartText entry and measures the item bounds used by hover and tooltips.
        /// </summary>
        /// <param name="entry">Parsed SmartText entry.</param>
        private void RenderSmartText(FuSmartTextCacheEntry entry)
        {
            // DrawList rendering avoids creating one ImGui item per chunk.
            FuDrawList drawList = Fugui.GetCurrentWindowDrawList();
            Vector2 startPosition = ImGui.GetCursorScreenPos();
            Vector2 cursorPosition = startPosition;
            float availableWidth = ImGui.GetContentRegionAvail().x;
            float defaultLineHeight = ImGui.GetTextLineHeight();
            float lineHeight = defaultLineHeight;
            float currentLineWidth = 0f;
            float maxLineWidth = 0f;
            float totalHeight = 0f;
            int defaultFontSize = Fugui.GetFontSize();
            bool hasLineContent = false;

            for (int i = 0; i < entry.Segments.Count; i++)
            {
                FuSmartTextSegment segment = entry.Segments[i];
                if (segment.LineBreak)
                {
                    FinishLine(true);
                    continue;
                }

                bool fontPushed = PushSmartTextFont(segment, defaultFontSize);
                try
                {
                    Vector2 segmentSize = CalcSmartTextSegmentSize(segment);
                    if (availableWidth > 0f && currentLineWidth > 0f && currentLineWidth + segmentSize.x > availableWidth && segmentSize.x < availableWidth)
                    {
                        FinishLine(false);
                    }

                    DrawSmartTextSegment(drawList, cursorPosition, GetSmartTextColor(segment), segment);
                    cursorPosition.x += segmentSize.x;
                    currentLineWidth += segmentSize.x;
                    lineHeight = Mathf.Max(lineHeight, segmentSize.y);
                    maxLineWidth = Mathf.Max(maxLineWidth, currentLineWidth);
                    hasLineContent = true;
                }
                finally
                {
                    PopSmartTextFont(fontPushed);
                }
            }

            if (hasLineContent)
            {
                totalHeight += lineHeight;
            }

            ImGui.Dummy(new Vector2(maxLineWidth, totalHeight));

            void FinishLine(bool forceHeight)
            {
                if (hasLineContent || forceHeight)
                {
                    totalHeight += lineHeight;
                }

                cursorPosition.x = startPosition.x;
                cursorPosition.y = startPosition.y + totalHeight;
                currentLineWidth = 0f;
                lineHeight = defaultLineHeight;
                hasLineContent = false;
            }
        }

        /// <summary>
        /// Pushes the font requested by a SmartText segment when it differs from the current font.
        /// </summary>
        /// <param name="segment">Segment whose font should be active.</param>
        /// <param name="defaultFontSize">Font size currently used by the layout.</param>
        /// <returns>True when a font was pushed and must be popped.</returns>
        private static bool PushSmartTextFont(FuSmartTextSegment segment, int defaultFontSize)
        {
            int fontSize = segment.FontSize > 0 ? segment.FontSize : defaultFontSize;
            FontType fontType = segment.Bold ? FontType.Bold : FontType.Regular;
            if (!segment.Bold && segment.FontSize <= 0)
            {
                return false;
            }

            int previousFontPushCount = Fugui.NbPushFont;
            Fugui.PushFont(fontSize, fontType);
            return Fugui.NbPushFont > previousFontPushCount;
        }

        /// <summary>
        /// Pops a SmartText font only when this renderer pushed one.
        /// </summary>
        /// <param name="fontPushed">Whether the current segment pushed a font.</param>
        private static void PopSmartTextFont(bool fontPushed)
        {
            if (fontPushed)
            {
                Fugui.PopFont();
            }
        }

        /// <summary>
        /// Measures a cached UTF-8 SmartText segment using the active ImGui font.
        /// </summary>
        /// <param name="segment">Segment to measure.</param>
        /// <returns>Segment size.</returns>
        private static unsafe Vector2 CalcSmartTextSegmentSize(FuSmartTextSegment segment)
        {
            if (segment.Utf8Length <= 0 || segment.Utf8Text == null)
            {
                return Vector2.zero;
            }

            Vector2 result;
            fixed (byte* text = segment.Utf8Text)
            {
                ImGuiNative.igCalcTextSize(&result, text, text + segment.Utf8Length, 0, -1f);
            }
            return result;
        }

        /// <summary>
        /// Draws a cached UTF-8 SmartText segment into the current draw list.
        /// </summary>
        /// <param name="drawList">Target draw list.</param>
        /// <param name="position">Screen position.</param>
        /// <param name="color">Packed text color.</param>
        /// <param name="segment">Segment to draw.</param>
        private static unsafe void DrawSmartTextSegment(FuDrawList drawList, Vector2 position, uint color, FuSmartTextSegment segment)
        {
            if (segment.Utf8Length <= 0 || segment.Utf8Text == null)
            {
                return;
            }

            fixed (byte* text = segment.Utf8Text)
            {
                ImGuiNative.ImDrawList_AddText_Vec2(drawList.NativePtr, position, color, text, text + segment.Utf8Length);
            }
        }

        /// <summary>
        /// Resolves the packed color used by a SmartText segment.
        /// </summary>
        /// <param name="segment">Segment whose color should be resolved.</param>
        /// <returns>Packed ImGui color.</returns>
        private uint GetSmartTextColor(FuSmartTextSegment segment)
        {
            if (!segment.HasColor)
            {
                return ImGui.GetColorU32(ImGuiCol.Text);
            }

            Color color = LastItemDisabled ? segment.Color * 0.5f : segment.Color;
            return ImGui.GetColorU32(color);
        }
        #endregion

        #region SmartText Cache
        /// <summary>
        /// Gets a parsed SmartText entry from cache, parsing the text on first use.
        /// </summary>
        /// <param name="text">Visible tagged text.</param>
        /// <returns>Cached parsed entry.</returns>
        private static FuSmartTextCacheEntry GetSmartTextCacheEntry(string text)
        {
            if (_smartTextCache.TryGetValue(text, out FuSmartTextCacheEntry entry))
            {
                return entry;
            }

            entry = ParseSmartText(text);
            if (_smartTextCache.Count >= SmartTextCacheLimit && _smartTextCacheOrder.Count > 0)
            {
                string oldestKey = _smartTextCacheOrder.Dequeue();
                _smartTextCache.Remove(oldestKey);
            }

            _smartTextCache.Add(text, entry);
            _smartTextCacheOrder.Enqueue(text);
            return entry;
        }

        /// <summary>
        /// Parses SmartText tags into stable render segments.
        /// </summary>
        /// <param name="text">Visible tagged text.</param>
        /// <returns>Parsed SmartText entry.</returns>
        private static FuSmartTextCacheEntry ParseSmartText(string text)
        {
            // The parser emits render-ready chunks split at natural wrap points.
            FuSmartTextCacheEntry entry = new FuSmartTextCacheEntry();
            FuSmartTextStyleState currentStyle = FuSmartTextStyleState.Default;
            List<FuSmartTextStackEntry> styleStack = new List<FuSmartTextStackEntry>(4);
            int segmentStart = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char current = text[i];
                if (current == '<' && TryReadSmartTextTag(text, i, out int tagEnd, out string tag))
                {
                    if (IsSmartTextLineBreakTag(tag))
                    {
                        AddSmartTextSegment(entry, text, segmentStart, i, currentStyle);
                        entry.Segments.Add(FuSmartTextSegment.Break);
                        i = tagEnd;
                        segmentStart = i + 1;
                        continue;
                    }

                    FuSmartTextStyleState previousStyle = currentStyle;
                    if (ApplySmartTextTag(tag, ref currentStyle, styleStack))
                    {
                        AddSmartTextSegment(entry, text, segmentStart, i, previousStyle);
                        i = tagEnd;
                        segmentStart = i + 1;
                    }
                    continue;
                }

                if (current == '\n')
                {
                    AddSmartTextSegment(entry, text, segmentStart, i, currentStyle);
                    entry.Segments.Add(FuSmartTextSegment.Break);
                    segmentStart = i + 1;
                    continue;
                }

                if (current == ' ' || current == '-' || current == '_')
                {
                    AddSmartTextSegment(entry, text, segmentStart, i + 1, currentStyle);
                    segmentStart = i + 1;
                }
            }

            AddSmartTextSegment(entry, text, segmentStart, text.Length, currentStyle);
            return entry;
        }

        /// <summary>
        /// Adds one parsed text segment when the input range is not empty.
        /// </summary>
        /// <param name="entry">Entry receiving the segment.</param>
        /// <param name="text">Source text.</param>
        /// <param name="start">Start index.</param>
        /// <param name="end">End index.</param>
        /// <param name="style">Style active for the segment.</param>
        private static void AddSmartTextSegment(FuSmartTextCacheEntry entry, string text, int start, int end, FuSmartTextStyleState style)
        {
            int length = end - start;
            if (length <= 0)
            {
                return;
            }

            entry.Segments.Add(new FuSmartTextSegment(text, start, length, style));
        }
        #endregion

        #region SmartText Parsing
        /// <summary>
        /// Reads a tag body from the given opening bracket.
        /// </summary>
        /// <param name="text">Source text.</param>
        /// <param name="start">Opening bracket index.</param>
        /// <param name="tagEnd">Closing bracket index.</param>
        /// <param name="tag">Tag content without brackets.</param>
        /// <returns>True when a full tag was found.</returns>
        private static bool TryReadSmartTextTag(string text, int start, out int tagEnd, out string tag)
        {
            tagEnd = start + 1;
            while (tagEnd < text.Length && text[tagEnd] != '>' && text[tagEnd] != '<')
            {
                tagEnd++;
            }

            if (tagEnd >= text.Length || text[tagEnd] != '>')
            {
                tag = string.Empty;
                return false;
            }

            tag = text.Substring(start + 1, tagEnd - start - 1).Trim();
            return tag.Length > 0;
        }

        /// <summary>
        /// Applies a parsed SmartText tag to the current style stack.
        /// </summary>
        /// <param name="tag">Tag content without brackets.</param>
        /// <param name="currentStyle">Current style state.</param>
        /// <param name="styleStack">Style restore stack.</param>
        /// <returns>True when the tag was recognized and consumed.</returns>
        private static bool ApplySmartTextTag(string tag, ref FuSmartTextStyleState currentStyle, List<FuSmartTextStackEntry> styleStack)
        {
            if (tag == "b")
            {
                PushSmartTextStyle(styleStack, FuSmartTextTagKind.Bold, currentStyle);
                currentStyle.Bold = true;
                return true;
            }

            if (tag == "/b")
            {
                RestoreSmartTextStyle(styleStack, FuSmartTextTagKind.Bold, ref currentStyle);
                return true;
            }

            if (tag == "/size")
            {
                RestoreSmartTextStyle(styleStack, FuSmartTextTagKind.Size, ref currentStyle);
                return true;
            }

            if (tag.StartsWith("size=", StringComparison.Ordinal) && TryParseSmartTextSize(tag, out int size))
            {
                PushSmartTextStyle(styleStack, FuSmartTextTagKind.Size, currentStyle);
                currentStyle.FontSize = size;
                return true;
            }

            if (tag == "/color")
            {
                RestoreSmartTextStyle(styleStack, FuSmartTextTagKind.Color, ref currentStyle);
                return true;
            }

            if (tag.StartsWith("color=", StringComparison.Ordinal) && TryParseSmartTextColor(tag.Substring("color=".Length), out Color color))
            {
                PushSmartTextStyle(styleStack, FuSmartTextTagKind.Color, currentStyle);
                currentStyle.HasColor = true;
                currentStyle.Color = color;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks whether a tag requests a hard line break.
        /// </summary>
        /// <param name="tag">Tag content without brackets.</param>
        /// <returns>True when the tag is a SmartText line break.</returns>
        private static bool IsSmartTextLineBreakTag(string tag)
        {
            return tag == "br/" || tag == "/br";
        }

        /// <summary>
        /// Pushes a style restore point.
        /// </summary>
        /// <param name="styleStack">Style restore stack.</param>
        /// <param name="kind">Tag kind that owns the restore point.</param>
        /// <param name="currentStyle">Style to restore later.</param>
        private static void PushSmartTextStyle(List<FuSmartTextStackEntry> styleStack, FuSmartTextTagKind kind, FuSmartTextStyleState currentStyle)
        {
            styleStack.Add(new FuSmartTextStackEntry(kind, currentStyle));
        }

        /// <summary>
        /// Restores the last matching style tag and drops malformed nested state safely.
        /// </summary>
        /// <param name="styleStack">Style restore stack.</param>
        /// <param name="kind">Tag kind to restore.</param>
        /// <param name="currentStyle">Current style state.</param>
        private static void RestoreSmartTextStyle(List<FuSmartTextStackEntry> styleStack, FuSmartTextTagKind kind, ref FuSmartTextStyleState currentStyle)
        {
            for (int i = styleStack.Count - 1; i >= 0; i--)
            {
                if (styleStack[i].Kind != kind)
                {
                    continue;
                }

                currentStyle = styleStack[i].PreviousStyle;
                styleStack.RemoveRange(i, styleStack.Count - i);
                return;
            }
        }

        /// <summary>
        /// Parses and clamps a SmartText size tag.
        /// </summary>
        /// <param name="tag">Full size tag.</param>
        /// <param name="size">Parsed size.</param>
        /// <returns>True when the tag contains a valid size.</returns>
        private static bool TryParseSmartTextSize(string tag, out int size)
        {
            if (!int.TryParse(tag.Substring("size=".Length), out size))
            {
                size = 0;
                return false;
            }

            size = Mathf.Clamp(size, 1, SmartTextMaxFontSize);
            return true;
        }

        /// <summary>
        /// Parses a SmartText color tag value.
        /// </summary>
        /// <param name="value">Color value after color=.</param>
        /// <param name="color">Parsed color.</param>
        /// <returns>True when a supported color format was parsed.</returns>
        private static bool TryParseSmartTextColor(string value, out Color color)
        {
            value = value.Trim();
            if (TryParseSmartTextRgbColor(value, out color))
            {
                return true;
            }

            return ColorUtility.TryParseHtmlString(value, out color);
        }

        /// <summary>
        /// Parses rgb() and rgba() SmartText color values.
        /// </summary>
        /// <param name="value">Color value.</param>
        /// <param name="color">Parsed color.</param>
        /// <returns>True when an rgb or rgba value was parsed.</returns>
        private static bool TryParseSmartTextRgbColor(string value, out Color color)
        {
            color = default;
            bool rgba = value.StartsWith("rgba(", StringComparison.OrdinalIgnoreCase);
            bool rgb = value.StartsWith("rgb(", StringComparison.OrdinalIgnoreCase);
            if (!rgba && !rgb)
            {
                return false;
            }

            int openIndex = value.IndexOf('(');
            int closeIndex = value.LastIndexOf(')');
            if (openIndex < 0 || closeIndex <= openIndex)
            {
                return false;
            }

            string[] parts = value.Substring(openIndex + 1, closeIndex - openIndex - 1).Split(',');
            if ((!rgba && parts.Length != 3) || (rgba && parts.Length != 4))
            {
                return false;
            }

            if (!TryParseSmartTextColorChannel(parts[0], out float r) ||
                !TryParseSmartTextColorChannel(parts[1], out float g) ||
                !TryParseSmartTextColorChannel(parts[2], out float b))
            {
                return false;
            }

            float a = 1f;
            if (rgba && !TryParseSmartTextAlphaChannel(parts[3], out a))
            {
                return false;
            }

            color = new Color(r, g, b, a);
            return true;
        }

        /// <summary>
        /// Parses a 0-255 color channel.
        /// </summary>
        /// <param name="value">Channel value.</param>
        /// <param name="channel">Normalized channel value.</param>
        /// <returns>True when the channel was parsed.</returns>
        private static bool TryParseSmartTextColorChannel(string value, out float channel)
        {
            if (!float.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            {
                channel = 0f;
                return false;
            }

            channel = Mathf.Clamp01(parsed / 255f);
            return true;
        }

        /// <summary>
        /// Parses an alpha channel as 0-1 or 0-255.
        /// </summary>
        /// <param name="value">Alpha value.</param>
        /// <param name="alpha">Normalized alpha value.</param>
        /// <returns>True when the alpha channel was parsed.</returns>
        private static bool TryParseSmartTextAlphaChannel(string value, out float alpha)
        {
            if (!float.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            {
                alpha = 1f;
                return false;
            }

            alpha = parsed > 1f ? Mathf.Clamp01(parsed / 255f) : Mathf.Clamp01(parsed);
            return true;
        }
        #endregion

        #region SmartText Types
        private sealed class FuSmartTextCacheEntry
        {
            public readonly List<FuSmartTextSegment> Segments = new List<FuSmartTextSegment>(8);
        }

        private readonly struct FuSmartTextSegment
        {
            public static FuSmartTextSegment Break => new FuSmartTextSegment();

            public readonly int FontSize;
            public readonly bool Bold;
            public readonly bool HasColor;
            public readonly Color Color;
            public readonly bool LineBreak;
            public readonly byte[] Utf8Text;
            public readonly int Utf8Length;

            /// <summary>
            /// Initializes a text segment with the provided style.
            /// </summary>
            /// <param name="source">Source text.</param>
            /// <param name="start">Start index.</param>
            /// <param name="length">Text length.</param>
            /// <param name="style">Style state active on the segment.</param>
            public FuSmartTextSegment(string source, int start, int length, FuSmartTextStyleState style)
            {
                FontSize = style.FontSize;
                Bold = style.Bold;
                HasColor = style.HasColor;
                Color = style.Color;
                LineBreak = false;
                Utf8Text = Encoding.UTF8.GetBytes(source.Substring(start, length));
                Utf8Length = Utf8Text.Length;
            }
        }

        private struct FuSmartTextStyleState
        {
            public static FuSmartTextStyleState Default => new FuSmartTextStyleState();

            public int FontSize;
            public bool Bold;
            public bool HasColor;
            public Color Color;
        }

        private readonly struct FuSmartTextStackEntry
        {
            public readonly FuSmartTextTagKind Kind;
            public readonly FuSmartTextStyleState PreviousStyle;

            /// <summary>
            /// Initializes a style stack entry.
            /// </summary>
            /// <param name="kind">Tag kind that owns the entry.</param>
            /// <param name="previousStyle">Style active before the tag was opened.</param>
            public FuSmartTextStackEntry(FuSmartTextTagKind kind, FuSmartTextStyleState previousStyle)
            {
                Kind = kind;
                PreviousStyle = previousStyle;
            }
        }

        private enum FuSmartTextTagKind
        {
            Bold,
            Size,
            Color
        }
        #endregion
    }
}
