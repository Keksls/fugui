using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework
{
    /// <summary>
    /// Visual flags for lightweight Fugui surface helpers.
    /// </summary>
    [Flags]
    public enum FuSurfaceFlags
    {
        /// <summary>
        /// Draw only the background fill.
        /// </summary>
        None = 0,
        /// <summary>
        /// Draw a thin border around the surface.
        /// </summary>
        Border = 1 << 0,
        /// <summary>
        /// Draw an accent stripe on the left side.
        /// </summary>
        AccentLeft = 1 << 1,
        /// <summary>
        /// Draw an accent stripe on the top side.
        /// </summary>
        AccentTop = 1 << 2,
        /// <summary>
        /// Draw an accent stripe on the right side.
        /// </summary>
        AccentRight = 1 << 3,
        /// <summary>
        /// Draw an accent stripe on the bottom side.
        /// </summary>
        AccentBottom = 1 << 4,
        /// <summary>
        /// Default surface: border plus left accent.
        /// </summary>
        Default = Border | AccentLeft
    }

    /// <summary>
    /// Reusable visual surface and badge widgets for Fugui layouts.
    /// </summary>
    public partial class FuLayout
    {
        #region Methods
        /// <summary>
        /// Draws a reusable non-interactive surface and consumes its layout space.
        /// </summary>
        /// <param name="id">Unique surface id.</param>
        /// <param name="size">Surface size. Use x = -1 for full available width.</param>
        /// <param name="accentColor">Accent stripe color.</param>
        /// <param name="flags">Surface visual flags.</param>
        /// <param name="backgroundAlpha">Frame background alpha multiplier.</param>
        /// <param name="borderAlpha">Border alpha multiplier.</param>
        /// <param name="accentAlpha">Accent alpha multiplier.</param>
        /// <param name="rounding">Corner rounding in unscaled pixels.</param>
        /// <param name="accentThickness">Accent thickness in unscaled pixels.</param>
        /// <returns>The screen-space rectangle drawn by the surface.</returns>
        public Rect Surface(string id, FuElementSize size, FuColors accentColor = FuColors.Highlight, FuSurfaceFlags flags = FuSurfaceFlags.Default, float backgroundAlpha = 0.46f, float borderAlpha = 0.30f, float accentAlpha = 0.72f, float rounding = 6f, float accentThickness = 3f)
        {
            string elementID = id;
            beginElement(ref elementID, null, canBeHidden: false);
            if (!_drawElement)
            {
                return default;
            }

            Vector2 resolvedSize = size.GetSize();
            if (resolvedSize.x <= 0f)
            {
                resolvedSize.x = ImGui.GetContentRegionAvail().x;
            }
            if (resolvedSize.y <= 0f)
            {
                resolvedSize.y = ImGui.GetFrameHeight();
            }

            Vector2 pos = ImGui.GetCursorScreenPos();
            Rect rect = new Rect(pos, resolvedSize);
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            drawSurfaceChrome(drawList, rect, accentColor, flags, backgroundAlpha, borderAlpha, accentAlpha, rounding, accentThickness);

            ImGui.Dummy(resolvedSize);
            setBaseElementState(elementID, pos, resolvedSize, false, false);
            endElement();
            return rect;
        }

        /// <summary>
        /// Draws a compact feature panel with a title, body text and optional pills.
        /// </summary>
        /// <param name="id">Unique panel id.</param>
        /// <param name="title">Panel title.</param>
        /// <param name="body">Panel body text.</param>
        /// <param name="pills">Optional pill labels.</param>
        /// <param name="pillColors">Optional pill colors, matched by index.</param>
        /// <param name="accentColor">Accent stripe color.</param>
        /// <param name="height">Panel height in unscaled pixels.</param>
        /// <returns>The screen-space rectangle drawn by the panel.</returns>
        public Rect FeaturePanel(string id, string title, string body, IList<string> pills = null, IList<FuColors> pillColors = null, FuColors accentColor = FuColors.Highlight, float height = 94f)
        {
            Rect rect = Surface(id, new FuElementSize(-1f, height), accentColor, FuSurfaceFlags.Default, 0.46f, 0.30f, 0.72f, 6f, 3f);
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return rect;
            }

            float scale = Fugui.CurrentContext.Scale;
            Vector2 titlePos = rect.position + new Vector2(16f, 10f) * scale;
            Vector2 textWidth = new Vector2(Mathf.Max(1f, rect.width - 32f * scale), 24f * scale);
            Fugui.PushFont(18, FontType.Bold);
            EnboxedText(title, titlePos, textWidth, Vector2.zero, Vector2.zero, new Vector2(0f, 0.5f), FuTextWrapping.Clip);
            Fugui.PopFont();

            Fugui.Push(ImGuiCol.Text, Fugui.Themes.GetColor(FuColors.TextDisabled, 0.86f));
            EnboxedText(body, titlePos + new Vector2(0f, 27f * scale), new Vector2(rect.width - 32f * scale, 34f * scale), Vector2.zero, Vector2.zero, new Vector2(0f, 0f), FuTextWrapping.Wrap);
            Fugui.PopColor();

            if (pills != null && pills.Count > 0)
            {
                Vector2 pillPos = titlePos + new Vector2(0f, 64f * scale);
                float maxRight = rect.xMax - 12f * scale;
                for (int i = 0; i < pills.Count; i++)
                {
                    FuColors color = pillColors != null && i < pillColors.Count ? pillColors[i] : FuColors.Highlight;
                    Rect pillRect = drawPillAt(pills[i], pillPos, color);
                    pillPos.x = pillRect.xMax + 6f * scale;
                    if (pillPos.x > maxRight)
                    {
                        break;
                    }
                }
            }

            return rect;
        }

        /// <summary>
        /// Draws a theme-aware callout block with a left accent stripe.
        /// </summary>
        /// <param name="id">Unique callout id.</param>
        /// <param name="text">Callout text.</param>
        /// <param name="accentColor">Accent stripe color.</param>
        /// <returns>The screen-space rectangle drawn by the callout.</returns>
        public Rect Callout(string id, string text, FuColors accentColor = FuColors.Highlight)
        {
            float scale = Fugui.CurrentContext.Scale;
            float width = Mathf.Max(1f, GetAvailableWidth());
            Vector2 textSize = Fugui.CalcTextSize(text, FuTextWrapping.Wrap, new Vector2(width - 30f * scale, 300f * scale));
            float height = Mathf.Max(38f * scale, textSize.y + 18f * scale);
            Rect rect = Surface(id, new FuElementSize(-1f, height / scale), accentColor, FuSurfaceFlags.Default, 0.34f, 0.22f, 0.74f, 6f, 3f);
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return rect;
            }

            EnboxedText(text, rect.position + new Vector2(14f, 9f) * scale, new Vector2(rect.width - 24f * scale, rect.height - 16f * scale), Vector2.zero, Vector2.zero, new Vector2(0f, 0f), FuTextWrapping.Wrap);
            Dummy(0f, 7f);
            return rect;
        }

        /// <summary>
        /// Draws a navigation row with hover and selected states.
        /// </summary>
        /// <param name="id">Unique item id.</param>
        /// <param name="text">Item label.</param>
        /// <param name="selected">Whether the row is selected.</param>
        /// <param name="tooltip">Optional tooltip.</param>
        /// <param name="accentColor">Selected accent color.</param>
        /// <param name="height">Item height in unscaled pixels.</param>
        /// <returns>True when clicked.</returns>
        public bool NavigationItem(string id, string text, bool selected, string tooltip = null, FuColors accentColor = FuColors.Highlight, float height = 29f)
        {
            string elementID = id;
            beginElement(ref elementID, null, canBeHidden: false);
            if (!_drawElement)
            {
                return false;
            }

            float scale = Fugui.CurrentContext.Scale;
            Vector2 size = new Vector2(Mathf.Max(1f, ImGui.GetContentRegionAvail().x), Mathf.Max(1f, height * scale));
            Vector2 pos = ImGui.GetCursorScreenPos();
            Rect rect = new Rect(pos, size);
            ImGui.InvisibleButton("##" + elementID, size, ImGuiButtonFlags.MouseButtonLeft);
            bool hovered = ImGui.IsItemHovered();
            bool active = ImGui.IsItemActive();
            bool clicked = ImGui.IsItemClicked(ImGuiMouseButton.Left);

            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            if (selected || hovered || active)
            {
                Vector4 fill = selected
                    ? Fugui.Themes.GetColor(accentColor, active ? 0.24f : 0.18f)
                    : Fugui.Themes.GetColor(FuColors.FrameBgHovered, active ? 0.46f : 0.36f);
                drawList.AddRectFilled(rect.min, rect.max, ImGui.GetColorU32(fill), 4f * scale, ImDrawFlags.RoundCornersAll);
            }

            if (selected)
            {
                Vector4 accent = Fugui.Themes.GetColor(accentColor, 0.92f);
                drawList.AddRectFilled(rect.min + new Vector2(0f, 4f * scale), new Vector2(rect.xMin + 3f * scale, rect.yMax - 4f * scale), ImGui.GetColorU32(accent), 2f * scale, ImDrawFlags.RoundCornersAll);
            }

            Vector4 textColor = selected
                ? Fugui.Themes.GetColor(FuColors.Text, 1f)
                : Fugui.Themes.GetColor(FuColors.Text, hovered ? 0.92f : 0.72f);
            Fugui.Push(ImGuiCol.Text, textColor);
            EnboxedText(text, rect.position + new Vector2(10f, 0f) * scale, rect.size - new Vector2(16f, 0f) * scale, Vector2.zero, Vector2.zero, new Vector2(0f, 0.5f), FuTextWrapping.Clip);
            Fugui.PopColor();

            if (hovered)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }
            if (!string.IsNullOrEmpty(tooltip))
            {
                SetToolTip(elementID + "-tooltip", tooltip, hovered);
            }

            setBaseElementState(elementID, pos, size, true, clicked);
            endElement();
            return clicked;
        }

        /// <summary>
        /// Draws one compact pill label.
        /// </summary>
        /// <param name="id">Unique pill id.</param>
        /// <param name="text">Pill label.</param>
        /// <param name="color">Pill theme color.</param>
        /// <returns>The screen-space rectangle drawn by the pill.</returns>
        public Rect Pill(string id, string text, FuColors color = FuColors.Highlight)
        {
            string elementID = id;
            beginElement(ref elementID, null, canBeHidden: false);
            if (!_drawElement)
            {
                return default;
            }

            Vector2 pos = ImGui.GetCursorScreenPos();
            Rect rect = drawPillAt(text, pos, color);
            ImGui.Dummy(rect.size);
            setBaseElementState(elementID, pos, rect.size, false, false);
            endElement();
            return rect;
        }

        /// <summary>
        /// Draws a wrapping row of compact pill labels.
        /// </summary>
        /// <param name="id">Unique pill row id.</param>
        /// <param name="texts">Pill labels.</param>
        /// <param name="colors">Optional pill colors, matched by index.</param>
        /// <param name="wrap">Whether pills wrap to the next line.</param>
        public void PillRow(string id, IList<string> texts, IList<FuColors> colors = null, bool wrap = true)
        {
            string elementID = id;
            beginElement(ref elementID, null, canBeHidden: false);
            if (!_drawElement)
            {
                return;
            }

            float scale = Fugui.CurrentContext.Scale;
            Vector2 start = ImGui.GetCursorScreenPos();
            float availableWidth = Mathf.Max(1f, ImGui.GetContentRegionAvail().x);
            float maxRight = start.x + availableWidth;
            float gap = 6f * scale;
            float x = start.x;
            float y = start.y;
            float lineHeight = 0f;

            if (texts != null)
            {
                for (int i = 0; i < texts.Count; i++)
                {
                    string text = texts[i];
                    Vector2 size = getPillSize(text);
                    if (wrap && x > start.x && x + size.x > maxRight)
                    {
                        x = start.x;
                        y += lineHeight + 4f * scale;
                        lineHeight = 0f;
                    }

                    FuColors color = colors != null && i < colors.Count ? colors[i] : FuColors.BackgroundInfo;
                    drawPillAt(text, new Vector2(x, y), color, size);
                    x += size.x + gap;
                    lineHeight = Mathf.Max(lineHeight, size.y);
                }
            }

            float totalHeight = Mathf.Max(lineHeight, ImGui.GetFrameHeight());
            if (y > start.y)
            {
                totalHeight += y - start.y;
            }

            ImGui.Dummy(new Vector2(availableWidth, totalHeight));
            setBaseElementState(elementID, start, new Vector2(availableWidth, totalHeight), false, false);
            endElement();
        }

        private void drawSurfaceChrome(ImDrawListPtr drawList, Rect rect, FuColors accentColor, FuSurfaceFlags flags, float backgroundAlpha, float borderAlpha, float accentAlpha, float rounding, float accentThickness)
        {
            float scale = Fugui.CurrentContext.Scale;
            float scaledRounding = Mathf.Min(rounding * scale, Mathf.Min(rect.width, rect.height) * 0.5f);
            float scaledAccent = Mathf.Max(1f, accentThickness * scale);

            Vector4 bg = Fugui.Themes.GetColor(FuColors.FrameBg, backgroundAlpha);
            Vector4 border = Fugui.Themes.GetColor(FuColors.Border, borderAlpha);
            Vector4 accent = Fugui.Themes.GetColor(accentColor, accentAlpha);

            drawList.AddRectFilled(rect.min, rect.max, ImGui.GetColorU32(bg), scaledRounding, ImDrawFlags.RoundCornersAll);
            if (flags.HasFlag(FuSurfaceFlags.Border))
            {
                drawList.AddRect(rect.min, rect.max, ImGui.GetColorU32(border), scaledRounding, ImDrawFlags.RoundCornersAll, Mathf.Max(1f, scale));
            }
            if (flags.HasFlag(FuSurfaceFlags.AccentLeft))
            {
                drawList.AddRectFilled(rect.min, new Vector2(rect.xMin + scaledAccent, rect.yMax), ImGui.GetColorU32(accent), scaledRounding, ImDrawFlags.RoundCornersLeft);
            }
            if (flags.HasFlag(FuSurfaceFlags.AccentTop))
            {
                drawList.AddRectFilled(rect.min, new Vector2(rect.xMax, rect.yMin + scaledAccent), ImGui.GetColorU32(accent), scaledRounding, ImDrawFlags.RoundCornersTop);
            }
            if (flags.HasFlag(FuSurfaceFlags.AccentRight))
            {
                drawList.AddRectFilled(new Vector2(rect.xMax - scaledAccent, rect.yMin), rect.max, ImGui.GetColorU32(accent), scaledRounding, ImDrawFlags.RoundCornersRight);
            }
            if (flags.HasFlag(FuSurfaceFlags.AccentBottom))
            {
                drawList.AddRectFilled(new Vector2(rect.xMin, rect.yMax - scaledAccent), rect.max, ImGui.GetColorU32(accent), scaledRounding, ImDrawFlags.RoundCornersBottom);
            }
        }

        private Rect drawPillAt(string text, Vector2 pos, FuColors color)
        {
            return drawPillAt(text, pos, color, getPillSize(text));
        }

        private Rect drawPillAt(string text, Vector2 pos, FuColors color, Vector2 size)
        {
            float scale = Fugui.CurrentContext.Scale;
            Rect rect = new Rect(pos, size);
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector4 bg = Fugui.Themes.GetColor(color, 0.18f);
            Vector4 border = Fugui.Themes.GetColor(color, 0.42f);
            Vector4 fg = Fugui.Themes.GetColor(FuColors.Text, 0.90f);
            drawList.AddRectFilled(rect.min, rect.max, ImGui.GetColorU32(bg), rect.height * 0.5f, ImDrawFlags.RoundCornersAll);
            drawList.AddRect(rect.min, rect.max, ImGui.GetColorU32(border), rect.height * 0.5f, ImDrawFlags.RoundCornersAll, Mathf.Max(1f, scale));
            Fugui.Push(ImGuiCol.Text, fg);
            EnboxedText(text, rect.position, rect.size, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), FuTextWrapping.Clip);
            Fugui.PopColor();
            return rect;
        }

        private Vector2 getPillSize(string text)
        {
            float scale = Fugui.CurrentContext.Scale;
            return ImGui.CalcTextSize(text ?? string.Empty) + new Vector2(14f, 5f) * scale;
        }
        #endregion
    }
}
