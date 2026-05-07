// define it to debug whatever Color or Styles are pushed (avoid stack leak metrics)
// it's ressourcefull, si comment it when debug is done. Ensure it's commented before build.
//#define FUDEBUG
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR && !FUMOBILE
#define FUMOBILE
#endif
using Fu.Framework;
using ImGuiNET;
#if FU_EXTERNALIZATION
using SDL2;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;

namespace Fu
{
    /// <summary>
    /// Fugui drawing helpers.
    /// </summary>
    public static partial class Fugui
    {
        private const ushort MIN_DUOTONE_GLYPH_RANGE = 60543;
        private const ushort MAX_DUOTONE_GLYPH_RANGE = 63743;

        /// <summary>
        /// Reserved texture identifier used to carry backdrop blur draw commands through ImGui draw lists.
        /// </summary>
        internal static readonly IntPtr BackdropTextureID = new IntPtr(-67001);

        private static readonly Stack<FuBackdropStyle> _backdropStack = new Stack<FuBackdropStyle>();

        /// <summary>
        /// Gets the draw list of the current Fugui window.
        /// </summary>
        public static FuDrawList GetWindowDrawList()
        {
            unsafe
            {
                return new FuDrawList(ImGui.GetWindowDrawList().NativePtr);
            }
        }

        /// <summary>
        /// Gets the foreground draw list of the current Fugui context.
        /// </summary>
        public static FuDrawList GetForegroundDrawList()
        {
            unsafe
            {
                return new FuDrawList(ImGui.GetForegroundDrawList().NativePtr);
            }
        }

        /// <summary>
        /// Gets the background draw list of the current Fugui context.
        /// </summary>
        public static FuDrawList GetBackgroundDrawList()
        {
            unsafe
            {
                return new FuDrawList(ImGui.GetBackgroundDrawList().NativePtr);
            }
        }

        /// <summary>
        /// Converts a color to Fugui's packed 32-bit draw color.
        /// </summary>
        public static uint ColorToU32(Color color)
        {
            return ImGui.GetColorU32(color);
        }

        /// <summary>
        /// Converts a color to Fugui's packed 32-bit draw color.
        /// </summary>
        public static uint ColorToU32(Vector4 color)
        {
            return ImGui.GetColorU32(color);
        }

        /// <summary>
        /// Sets the platform mouse cursor requested by Fugui.
        /// </summary>
        public static void SetMouseCursor(FuMouseCursor cursor)
        {
            ImGui.SetMouseCursor(cursor.ToImGui());
        }

        public static void DrawCarret_Down(FuDrawList drawList, Vector2 pos, float carretSize, float containerHeight, Color color)
        {
            DrawCarret_Down(drawList.ToImGui(), pos, carretSize, containerHeight, color);
        }

        public static void DrawCarret_Top(FuDrawList drawList, Vector2 pos, float carretSize, float containerHeight, Color color)
        {
            DrawCarret_Top(drawList.ToImGui(), pos, carretSize, containerHeight, color);
        }

        public static void DrawCarret_Right(FuDrawList drawList, Vector2 pos, float carretSize, float containerHeight, Color color)
        {
            DrawCarret_Right(drawList.ToImGui(), pos, carretSize, containerHeight, color);
        }

        public static void DrawCarret_Left(FuDrawList drawList, Vector2 pos, float carretSize, float containerHeight, Color color)
        {
            DrawCarret_Left(drawList.ToImGui(), pos, carretSize, containerHeight, color);
        }

        public static void DrawBubble(FuDrawList drawList, Vector2 pos, float radius, string text, Color backgroundColor, Color textColor)
        {
            DrawBubble(drawList.ToImGui(), pos, radius, text, backgroundColor, textColor);
        }

        public static void DrawBubbleWithTooltip(FuDrawList drawList, Vector2 pos, float radius, string text, Color backgroundColor, Color textColor, FuTextStyle tooltipStyle, string tooltip = "")
        {
            DrawBubbleWithTooltip(drawList.ToImGui(), pos, radius, text, backgroundColor, textColor, tooltipStyle, tooltip);
        }

        public static void DrawCheckMark(FuDrawList drawList, Vector2 pos, Color col, float size)
        {
            DrawCheckMark(drawList.ToImGui(), pos, col, size);
        }

        public static void DrawTilesBackground(FuDrawList drawList, Vector2 pos, Vector2 size, int numRows = 2)
        {
            DrawTilesBackground(drawList.ToImGui(), pos, size, numRows);
        }

        public static void DrawGradientRect(Vector2 pos, Vector2 size, float gradientStrenght, float rounding, FuDrawList drawList, Vector4 color)
        {
            unsafe
            {
                DrawGradientRect(pos, size, gradientStrenght, rounding, drawList.ToImGui(), color);
            }
        }

        public static void DrawArc(FuDrawList drawList, Vector2 center, float radius, float thickness, float angle, uint color)
        {
            DrawArc(drawList.ToImGui(), center, radius, thickness, angle, color);
        }

        public static void DrawLineGradient(FuDrawList drawList, Vector2 p0, Vector2 p1, float g0, float g1, float thickness, uint colStart, uint colEnd)
        {
            DrawLineGradient(drawList.ToImGui(), p0, p1, g0, g1, thickness, colStart, colEnd);
        }

        public static void DrawDuotoneSecondaryGlyph(string text, Vector2 textPos, FuDrawList drawList, bool disabled)
        {
            DrawDuotoneSecondaryGlyph(text, textPos, drawList.ToImGui(), disabled);
        }

        /// <summary>
        /// Draws a downward-pointing caret.
        /// <param name="drawList">Drawing list to add the caret to.</param>
        /// <param name="pos">Position of the caret.</param>
        /// <param name="carretSize">Size of the caret.</param>
        /// <param name="containerHeight">Height of the container.</param>
        /// <param name="color">Color of the caret.</param>
        /// </summary>
        internal static void DrawCarret_Down(ImDrawListPtr drawList, Vector2 pos, float carretSize, float containerHeight, Color color)
        {
            // Add a filled triangle with vertices pointing downwards
            drawList.AddTriangleFilled(
                new Vector2(pos.x, pos.y + (containerHeight / 2f) - (carretSize / 2f)),
                new Vector2(pos.x + carretSize, pos.y + (containerHeight / 2f) - (carretSize / 2f)),
                new Vector2(pos.x + carretSize / 2f, pos.y + (containerHeight / 2f) + (carretSize / 2f)),
                ImGui.GetColorU32(color));
        }

        /// <summary>
        /// Draws an upward-pointing caret.
        /// <param name="drawList">Drawing list to add the caret to.</param>
        /// <param name="pos">Position of the caret.</param>
        /// <param name="carretSize">Size of the caret.</param>
        /// <param name="containerHeight">Height of the container.</param>
        /// <param name="color">Color of the caret.</param>
        /// </summary>
        internal static void DrawCarret_Top(ImDrawListPtr drawList, Vector2 pos, float carretSize, float containerHeight, Color color)
        {
            // Add a filled triangle with vertices pointing upwards
            drawList.AddTriangleFilled(
                new Vector2(pos.x, pos.y + (containerHeight / 2f) + (carretSize / 2f)),
                new Vector2(pos.x + carretSize, pos.y + (containerHeight / 2f) + (carretSize / 2f)),
                new Vector2(pos.x + carretSize / 2f, pos.y + (containerHeight / 2f) - (carretSize / 2f)),
                ImGui.GetColorU32(color));
        }

        /// <summary>
        /// Draws a right-pointing caret.
        /// <param name="drawList">Drawing list to add the caret to.</param>
        /// <param name="pos">Position of the caret.</param>
        /// <param name="carretSize">Size of the caret.</param>
        /// <param name="containerHeight">Height of the container.</param>
        /// <param name="color">Color of the caret.</param>
        /// </summary>
        internal static void DrawCarret_Right(ImDrawListPtr drawList, Vector2 pos, float carretSize, float containerHeight, Color color)
        {
            // Add a filled triangle with vertices pointing to the right
            drawList.AddTriangleFilled(
                new Vector2(pos.x, pos.y + (containerHeight / 2f) - (carretSize / 2f)),
                new Vector2(pos.x + carretSize, pos.y + (containerHeight / 2f)),
                new Vector2(pos.x, pos.y + (containerHeight / 2f) + (carretSize / 2f)),
                ImGui.GetColorU32(color));
        }

        /// <summary>
        /// Draws a left-pointing caret.
        /// <param name="drawList">Drawing list to add the caret to.</param>
        /// <param name="pos">Position of the caret.</param>
        /// <param name="carretSize">Size of the caret.</param>
        /// <param name="containerHeight">Height of the container.</param>
        /// <param name="color">Color of the caret.</param>
        /// </summary>
        internal static void DrawCarret_Left(ImDrawListPtr drawList, Vector2 pos, float carretSize, float containerHeight, Color color)
        {
            // Add a filled triangle with vertices pointing to the left
            drawList.AddTriangleFilled(
                new Vector2(pos.x + carretSize, pos.y + (containerHeight / 2f) - (carretSize / 2f)),
                new Vector2(pos.x, pos.y + (containerHeight / 2f)),
                new Vector2(pos.x + carretSize, pos.y + (containerHeight / 2f) + (carretSize / 2f)),
                ImGui.GetColorU32(color));
        }

        /// <summary>
        /// Draws a notification bubble with text
        /// </summary>
        /// <param name="drawList">Drawing list to add the bubble to.</param>
        /// <param name="pos">Position of the bubble</param>
        /// <param name="radius">Radius of the bubble</param>
        /// <param name="text">Text to display</param>
        /// <param name="backgroundColor">Color of the background</param>
        /// <param name="textColor">Color of the text</param>
        internal static void DrawBubble(ImDrawListPtr drawList, Vector2 pos, float radius, string text, Color backgroundColor, Color textColor)
        {
            drawList.AddCircleFilled(
                pos,
                radius,
                ImGui.GetColorU32(backgroundColor));

            //Calc for center text in the notification bubble
            Vector2 textSize = ImGui.CalcTextSize(text);
            pos.y -= textSize.y / 2f + 1f;
            pos.x -= textSize.x / 2f;

            drawList.AddText(
                pos,
                ImGui.GetColorU32(textColor),
                text);
        }

        /// <summary>
        /// Draws a notification bubble with text
        /// </summary>
        /// <param name="drawList">Drawing list to add the bubble to.</param>
        /// <param name="pos">Position of the bubble</param>
        /// <param name="radius">Radius of the bubble</param>
        /// <param name="text">Text to display</param>
        /// <param name="backgroundColor">Color of the background</param>
        /// <param name="textColor">Color of the text</param>
        /// <param name="tooltipStyle">Fustyle of the tooltip text</param>
        /// <param name="tooltip">TooltipText</param>
        internal static void DrawBubbleWithTooltip(ImDrawListPtr drawList, Vector2 pos, float radius, string text, Color backgroundColor, Color textColor, FuTextStyle tooltipStyle, string tooltip = "")
        {
            drawList.AddCircleFilled(
                pos,
                radius,
                ImGui.GetColorU32(backgroundColor));

            //Calc for center text in the notification bubble
            Vector2 textSize = ImGui.CalcTextSize(text);
            pos.y -= textSize.y / 2f + 1f;
            pos.x -= textSize.x / 2f;

            drawList.AddText(
                pos,
                ImGui.GetColorU32(textColor),
                text);

            //Show tooltip
            Vector2 tooltipRectMin = pos;
            Vector2 tooltipRectMax = pos;
            tooltipRectMin.x -= radius;
            tooltipRectMax.x += radius;
            tooltipRectMin.y -= radius;
            tooltipRectMax.y += radius;

            if (!string.IsNullOrEmpty(tooltip) && ImGui.IsMouseHoveringRect(tooltipRectMin, tooltipRectMax))
            {
                tooltipStyle.Push(true);
                Fugui.Push(ImGuiStyleVar.WindowPadding, new Vector4(8f, 4f));
                Fugui.PushDefaultFont();
                ImGui.SetTooltip(tooltip);
                Fugui.PopFont();
                Fugui.PopStyle();
                tooltipStyle.Pop();
            }
        }

        /// <summary>
        /// Draw a check mark at the given position, size and color
        /// </summary>
        /// <param name="drawList"> The draw list to use to draw the check mark</param>
        /// <param name="pos"> Position to draw the check mark</param>
        /// <param name="col"> Color of the check mark</param>
        /// <param name="size"> Size of the check mark</param>
        internal static void DrawCheckMark(ImDrawListPtr drawList, Vector2 pos, Color col, float size)
        {
            float thickness = Mathf.Max(size / 5.0f, 1.0f);
            size -= thickness * 0.5f;
            pos += new Vector2(thickness * 0.25f, thickness * 0.25f);

            float third = size / 3.0f;
            float bx = pos.x + third;
            float by = pos.y + size - third * 0.5f;
            drawList.PathLineTo(new Vector2(bx - third, by - third));
            drawList.PathLineTo(new Vector2(bx, by));
            drawList.PathLineTo(new Vector2(bx + third * 2.0f, by - third * 2.0f));
            drawList.PathStroke(ImGui.GetColorU32(col), 0, thickness);
        }

        /// <summary>
        /// This method generates a tiled background using a given number of rows and an alternating pattern of dark and light tiles.
        /// It takes in an ImDrawList object, which is used to draw the tiles, as well as the position and size of the background.
        /// </summary>
        /// <param name="drawList">The ImDrawList object used to draw the tiles.</param>
        /// <param name="pos">The position of the tiled background.</param>
        /// <param name="size">The size of the tiled background.</param>
        /// <param name="numRows">The number of rows to use for the tiled background. Default value is 2.</param>
        internal static void DrawTilesBackground(ImDrawListPtr drawList, Vector2 pos, Vector2 size, int numRows = 2)
        {
            // Calculate the size of each tile
            float tileSize = size.y / numRows;
            // Calculate the number of columns based on the tile size
            int numCols = (int)(size.x / tileSize);

            // Define the dark and light colors
            uint darkColor = ImGui.GetColorU32(new Vector4(0.25f, 0.25f, 0.25f, 1f));
            uint lightColor = ImGui.GetColorU32(new Vector4(0.5f, 0.5f, 0.5f, 1f));

            // Iterate over each tile and draw it
            for (int row = 0; row < numRows; row++)
            {
                for (int col = 0; col < numCols; col++)
                {
                    // Calculate the position of the top-left and bottom-right corners of the tile
                    Vector2 topLeft = new Vector2(pos.x + col * tileSize, pos.y + row * tileSize);
                    Vector2 bottomRight = new Vector2(pos.x + (col + 1) * tileSize, pos.y + (row + 1) * tileSize);

                    // Determine the color of the tile based on its position
                    bool isEvenRow = row % 2 == 0;
                    bool isEvenCol = col % 2 == 0;
                    uint color = isEvenRow ^ isEvenCol ? darkColor : lightColor; // alternate black and white squares

                    // Draw the tile
                    drawList.AddRectFilled(topLeft, bottomRight, color);
                }
            }
        }

        /// <summary>
        /// Draw a gradient rect
        /// Used as Button background
        /// </summary>
        /// <param name="pos">position of the rect</param>
        /// <param name="size">Size of the background</param>
        /// <param name="gradientStrenght">strenght of the gradient (> 0 && < 1)</param>
        /// <param name="rounding">Rect rounding</param>
        /// <param name="drawList">drawList to use to draw the rect</param>
        /// <param name="color">The color of the rect</param>
        internal static unsafe void DrawGradientRect(Vector2 pos, Vector2 size, float gradientStrenght, float rounding, ImDrawListPtr drawList, Vector4 color)
        {
            Vector4 bg2f = new Vector4(color.x * gradientStrenght, color.y * gradientStrenght, color.z * gradientStrenght, color.w);
            // draw button frame
            int vert_start_idx = drawList.VtxBuffer.Size;
            drawList.AddRectFilled(pos, pos + size, ImGuiNative.igGetColorU32_Vec4(color), rounding);
            int vert_end_idx = drawList.VtxBuffer.Size;
            ImGuiInternal.igShadeVertsLinearColorGradientKeepAlpha(drawList.NativePtr, vert_start_idx, vert_end_idx, pos, new Vector2(pos.x, pos.y + size.y), ImGuiNative.igGetColorU32_Vec4(color), ImGuiNative.igGetColorU32_Vec4(bg2f));
        }

        /// <summary>
        /// Draws a circular arc on the current ImGui window.
        /// </summary>
        /// <param name="center">The center position of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="thickness">The stroke thickness of the arc.</param>
        /// <param name="angle">Normalized angle between 0 and 1 (0 = none, 1 = full circle).</param>
        /// <param name="color">The color of the arc (ImGui packed color).</param>
        internal static void DrawArc(ImDrawListPtr drawList, Vector2 center, float radius, float thickness, float angle, uint color)
        {
            if (angle <= 0f) return;

            float startAngle = -MathF.PI / 2f; // Start at the top
            float endAngle = startAngle + angle * MathF.PI * 2f;

            int segments = (int)(64 * angle);
            if (segments < 4) segments = 4;

            drawList.PathArcTo(center, radius, startAngle, endAngle, segments);
            drawList.PathStroke(color, ImDrawFlags.None, thickness);
        }

        /// <summary>
        /// Draw a line with gradient colors from start to end
        /// </summary>
        /// <param name="drawList"> The draw list to use to draw the line</param>
        /// <param name="p0"> Starting point of the line</param>
        /// <param name="p1"> Ending point of the line</param>
        /// <param name="g0"> Gradient progression at the start point (0 = colStart, 1 = colEnd)</param>
        /// <param name="g1"> Gradient progression at the end point (0 = colStart, 1 = colEnd)</param>
        /// <param name="thickness"> Thickness of the line</param>
        /// <param name="colStart"> Color at the start of the line</param>
        /// <param name="colEnd"> Color at the end of the line</param>
        internal static void DrawLineGradient(ImDrawListPtr drawList, Vector2 p0, Vector2 p1, float g0, float g1, float thickness, uint colStart, uint colEnd)
        {
            // Skip if fully transparent
            if (((colStart | colEnd) & 0xFF000000) == 0)
                return;

            Vector2 diff = p1 - p0;
            float len = diff.magnitude;
            if (len <= 0.0001f)
                return;

            if(thickness <= 1.0f)
                thickness = 1.0f;

            Vector2 dir = diff / len;
            Vector2 normal = new Vector2(-dir.y, dir.x) * (thickness * .5f);

            // Compute quad corners
            Vector2 v0 = p0 + normal;
            Vector2 v1 = p1 + normal;
            Vector2 v2 = p1 - normal;
            Vector2 v3 = p0 - normal;

            // Interpolate colors along gradient progression
            uint c0 = LerpColor(colStart, colEnd, g0);
            uint c1 = LerpColor(colStart, colEnd, g1);

            // Get the white pixel UV (ImGui built-in)
            var uv = ImGui.GetIO().Fonts.TexUvWhitePixel;

            // Reserve vertices + indices
            drawList.PrimReserve(6, 4);

            // Write indices (2 triangles)
            drawList.PrimWriteIdx((ushort)(drawList._VtxCurrentIdx));
            drawList.PrimWriteIdx((ushort)(drawList._VtxCurrentIdx + 1));
            drawList.PrimWriteIdx((ushort)(drawList._VtxCurrentIdx + 2));
            drawList.PrimWriteIdx((ushort)(drawList._VtxCurrentIdx));
            drawList.PrimWriteIdx((ushort)(drawList._VtxCurrentIdx + 2));
            drawList.PrimWriteIdx((ushort)(drawList._VtxCurrentIdx + 3));

            // Write vertices with gradient colors
            drawList.PrimWriteVtx(v0, uv, c0);
            drawList.PrimWriteVtx(v1, uv, c1);
            drawList.PrimWriteVtx(v2, uv, c1);
            drawList.PrimWriteVtx(v3, uv, c0);
        }

        /// <summary>
        /// Linear interpolation between two ImGui packed colors (IM_COL32 RGBA).
        /// </summary>
        /// <param name="c0"> First color (IM_COL32 RGBA).</param>
        /// <param name="c1"> Second color (IM_COL32 RGBA).</param>
        /// <param name="t"> Interpolation factor (0.0 to 1.0).</param>
        public static uint LerpColor(uint c0, uint c1, float t)
        {
            t = Mathf.Clamp01(t);

            int r0 = (int)(c0 & 0xFF);
            int g0 = (int)((c0 >> 8) & 0xFF);
            int b0 = (int)((c0 >> 16) & 0xFF);
            int a0 = (int)((c0 >> 24) & 0xFF);

            int r1 = (int)(c1 & 0xFF);
            int g1 = (int)((c1 >> 8) & 0xFF);
            int b1 = (int)((c1 >> 16) & 0xFF);
            int a1 = (int)((c1 >> 24) & 0xFF);

            int r = (int)Mathf.Round(Mathf.Lerp(r0, r1, t));
            int g = (int)Mathf.Round(Mathf.Lerp(g0, g1, t));
            int b = (int)Mathf.Round(Mathf.Lerp(b0, b1, t));
            int a = (int)Mathf.Round(Mathf.Lerp(a0, a1, t));

            return (uint)((a << 24) | (b << 16) | (g << 8) | r);
        }

        /// <summary>
        /// Check whatever a Char gyph is a Duotone Icon glyph
        /// </summary>
        /// <param name="character">char to check</param>
        /// <returns>true if it should be a duotone icon glyph char</returns>
        public static bool IsDuoToneChar(char character)
        {
            ushort charUS = (ushort)character;
            return charUS >= MIN_DUOTONE_GLYPH_RANGE && charUS <= MAX_DUOTONE_GLYPH_RANGE;
        }

        /// <summary>
        /// Render the secondary duotone glyph on top of a text
        /// </summary>
        /// <param name="text">text that will or have been draw outside</param>
        /// <param name="textPos">position of the text</param>
        /// <param name="drawList">drawList that draw the text</param>
        /// <param name="disabled">if true, render the secondary glyph in disabled color</param>
        internal static void DrawDuotoneSecondaryGlyph(string text, Vector2 textPos, ImDrawListPtr drawList, bool disabled)
        {
            // look for duoTone icons within text
            for (int i = 0; i < text.Length; i++)
            {
                // this char is Duotone, let's render secondary Glyph
                if (IsDuoToneChar(text[i]))
                {
                    // get preText string
                    char[] preTextCharArray = new char[i];
                    for (int j = 0; j < i; j++)
                    {
                        preTextCharArray[j] = text[i];
                    }
                    string preText = new string(preTextCharArray);
                    // get pretext size
                    Vector2 size = CalcTextSize(preText, FuTextWrapping.None);
                    // place virtual cursor to right position
                    textPos.x += size.x;
                    uint secondaryColor = GetSecondaryDuotoneColor(disabled);

                    // render secondary glyph
                    drawList.AddText(textPos, secondaryColor, ((char)(((ushort)text[i]) + 1)).ToString());
                    break;
                }
            }
        }

        /// <summary>
        /// Return the current primary duotone glyph color
        /// </summary>
        /// <param name="disabled">if true, return the disabled duotone color</param>
        /// <returns>primary duotone color</returns>
        public static uint GetPrimaryDuotoneColor(bool disabled)
        {
            // Get current ImGui text color (from style)
            Vector4 currentTextColor = ImGui.GetStyle().Colors[(int)FuColors.Text];

            // Get FuGui theme reference text color
            Vector4 themeTextColor = Fugui.Themes.GetColor(FuColors.Text);

            // If ImGui style uses a custom color (i.e., it's different from the theme)
            if (!ColorsAreEqual(currentTextColor, themeTextColor))
            {
                return ImGui.GetColorU32(currentTextColor);
            }

            // Else return duotone from theme (disabled or not)
            var color = Fugui.Themes.GetColor(disabled ? FuColors.TextDisabled : FuColors.DuotonePrimaryColor);
            return ImGui.GetColorU32(color);
        }

        /// <summary>
        /// Return the current secondary duotone glyph color
        /// </summary>
        /// <param name="disabled">if true, return the disabled color</param>
        /// <returns>secondary duotone color</returns>
        public static uint GetSecondaryDuotoneColor(bool disabled)
        {
            // Get current ImGui text color (possibly overridden)
            Vector4 currentTextColor = ImGui.GetStyle().Colors[(int)FuColors.Text];

            // Get FuGui theme base text color
            Vector4 themeTextColor = Fugui.Themes.GetColor(FuColors.Text);

            // If user has pushed a custom text color
            if (!ColorsAreEqual(currentTextColor, themeTextColor))
            {
                // Extrapolate secondary duotone based on currentTextColor * 0.9
                Vector4 extrapolated = new Vector4(
                    currentTextColor.x * 0.9f,
                    currentTextColor.y * 0.9f,
                    currentTextColor.z * 0.9f,
                    currentTextColor.w
                );

                return ImGui.GetColorU32(extrapolated);
            }

            // Fallback to theme duotone or disabled color
            var color = Fugui.Themes.GetColor(disabled ? FuColors.TextDisabled : FuColors.DuotoneSecondaryColor);
            return ImGui.GetColorU32(color);
        }

        /// <summary>
        /// Check if two colors are equal within a tolerance
        /// </summary>
        /// <param name="a"> first color</param>
        /// <param name="b"> second color</param>
        /// <param name="tolerance"> tolerance for color comparison</param>
        /// <returns> true if colors are equal within the tolerance</returns>
        public static bool ColorsAreEqual(Vector4 a, Vector4 b, float tolerance = 0.001f)
        {
            return MathF.Abs(a.x - b.x) < tolerance &&
                   MathF.Abs(a.y - b.y) < tolerance &&
                   MathF.Abs(a.z - b.z) < tolerance &&
                   MathF.Abs(a.w - b.w) < tolerance;
        }

        /// <summary>
        /// Pushes a backdrop style used by DrawBackdrop overloads that do not receive explicit colors.
        /// </summary>
        /// <param name="color">Overlay tint drawn above the blurred content.</param>
        /// <param name="blurRadius">Blur radius in current Fugui UI pixels. Zero draws only the color.</param>
        public static void PushBackdrop(Color color, float blurRadius = 0f)
        {
            _backdropStack.Push(new FuBackdropStyle(color, blurRadius));
        }

        /// <summary>
        /// Pops the last pushed backdrop style.
        /// </summary>
        public static void PopBackdrop()
        {
            if (_backdropStack.Count > 0)
            {
                _backdropStack.Pop();
            }
        }

        /// <summary>
        /// Draws the currently pushed backdrop style in a screen-space rect.
        /// </summary>
        /// <param name="rect">Screen-space rect to cover.</param>
        /// <param name="rounding">Optional corner rounding.</param>
        /// <param name="flags">Rounded corner flags.</param>
        public static void DrawBackdrop(Rect rect, float rounding = 0f, FuDrawFlags flags = FuDrawFlags.RoundCornersAll)
        {
            FuBackdropStyle style = _backdropStack.Count > 0
                ? _backdropStack.Peek()
                : FuBackdropStyle.Default;
            DrawBackdropInternal(ImGui.GetWindowDrawList(), rect, style.Color, style.BlurRadius, rounding, flags.ToImGui());
        }

        /// <summary>
        /// Draws a backdrop in the current window draw list.
        /// </summary>
        /// <param name="rect">Screen-space rect to cover.</param>
        /// <param name="color">Overlay tint drawn above the blurred content.</param>
        /// <param name="blurRadius">Blur radius in current Fugui UI pixels. Zero draws only the color.</param>
        /// <param name="rounding">Optional corner rounding.</param>
        /// <param name="flags">Rounded corner flags.</param>
        public static void DrawBackdrop(Rect rect, Color color, float blurRadius = 0f, float rounding = 0f, FuDrawFlags flags = FuDrawFlags.RoundCornersAll)
        {
            DrawBackdropInternal(ImGui.GetWindowDrawList(), rect, color, blurRadius, rounding, flags.ToImGui());
        }

        /// <summary>
        /// Draws a backdrop in any ImGui draw list.
        /// </summary>
        /// <param name="drawList">Target draw list.</param>
        /// <param name="rect">Screen-space rect to cover.</param>
        /// <param name="color">Overlay tint drawn above the blurred content.</param>
        /// <param name="blurRadius">Blur radius in current Fugui UI pixels. Zero draws only the color.</param>
        /// <param name="rounding">Optional corner rounding.</param>
        /// <param name="flags">Rounded corner flags.</param>
        public static void DrawBackdrop(FuDrawList drawList, Rect rect, Color color, float blurRadius = 0f, float rounding = 0f, FuDrawFlags flags = FuDrawFlags.RoundCornersAll)
        {
            DrawBackdropInternal(drawList.ToImGui(), rect, color, blurRadius, rounding, flags.ToImGui());
        }

        internal static void DrawBackdropInternal(ImDrawListPtr drawList, Rect rect, Color color, float blurRadius = 0f, float rounding = 0f, ImDrawFlags flags = ImDrawFlags.RoundCornersAll)
        {
            Vector2 min = rect.position;
            Vector2 max = rect.position + rect.size;
            if (rect.width <= 0f || rect.height <= 0f || (color.a <= 0f && blurRadius <= 0f))
            {
                return;
            }

            uint colorU32 = ImGui.GetColorU32(color);
            float scaledRounding = Mathf.Max(0f, rounding);
            float scaledBlurRadius = Mathf.Max(0f, blurRadius);
#if !FU_BACKDROP_ENABLED
            scaledBlurRadius = 0f;
#endif

            if (scaledBlurRadius <= 0f)
            {
                if (color.a <= 0f)
                {
                    return;
                }

                drawList.AddRectFilled(min, max, colorU32, scaledRounding, flags);
                return;
            }

            Vector2 uv = new Vector2(scaledBlurRadius, 0f);
            Vector2 uvMax = new Vector2(scaledBlurRadius, 1f);
            if (scaledRounding > 0f)
            {
                drawList.AddImageRounded(BackdropTextureID, min, max, uv, uvMax, colorU32, scaledRounding, flags);
                return;
            }

            drawList.AddImage(BackdropTextureID, min, max, uv, uvMax, colorU32);
        }

        /// <summary>
        /// Draws a theme-backed backdrop in the current window draw list.
        /// </summary>
        /// <param name="rect">Screen-space rect to cover.</param>
        /// <param name="color">Theme color used as overlay tint.</param>
        /// <param name="alphaMult">Alpha multiplier applied to the theme color.</param>
        /// <param name="rounding">Optional corner rounding.</param>
        /// <param name="flags">Rounded corner flags.</param>
        public static void DrawThemeBackdrop(Rect rect, FuColors color, float alphaMult = 1f, float rounding = 0f, FuDrawFlags flags = FuDrawFlags.RoundCornersAll)
        {
            DrawThemeBackdropInternal(ImGui.GetWindowDrawList(), rect, color, alphaMult, rounding, flags.ToImGui());
        }

        /// <summary>
        /// Draws a theme-backed backdrop in any ImGui draw list.
        /// </summary>
        /// <param name="drawList">Target draw list.</param>
        /// <param name="rect">Screen-space rect to cover.</param>
        /// <param name="color">Theme color used as overlay tint.</param>
        /// <param name="alphaMult">Alpha multiplier applied to the theme color.</param>
        /// <param name="rounding">Optional corner rounding.</param>
        /// <param name="flags">Rounded corner flags.</param>
        public static void DrawThemeBackdrop(FuDrawList drawList, Rect rect, FuColors color, float alphaMult = 1f, float rounding = 0f, FuDrawFlags flags = FuDrawFlags.RoundCornersAll)
        {
            DrawThemeBackdropInternal(drawList.ToImGui(), rect, color, alphaMult, rounding, flags.ToImGui());
        }

        internal static void DrawThemeBackdropInternal(ImDrawListPtr drawList, Rect rect, FuColors color, float alphaMult = 1f, float rounding = 0f, ImDrawFlags flags = ImDrawFlags.RoundCornersAll)
        {
            Vector4 themeColor = Themes != null
                ? Themes.GetColor(color, alphaMult)
                : Vector4.zero;
            DrawBackdropInternal(drawList, rect, themeColor, GetThemeBackdropBlur(color), rounding, flags);
        }

        /// <summary>
        /// Draws a backdrop over the current ImGui window rectangle.
        /// </summary>
        /// <param name="rounding">Optional corner rounding. Negative values use the current Fugui window rounding.</param>
        public static void DrawCurrentWindowBackdrop(float rounding = -1f)
        {
            float resolvedRounding = rounding >= 0f ? rounding : (Themes != null ? Themes.WindowRounding : 0f);
            DrawBackdrop(new Rect(ImGui.GetWindowPos(), ImGui.GetWindowSize()), resolvedRounding);
        }

        /// <summary>
        /// Draws a theme-backed backdrop over the current ImGui window rectangle.
        /// </summary>
        /// <param name="color">Theme color used as overlay tint.</param>
        /// <param name="alphaMult">Alpha multiplier applied to the theme color.</param>
        /// <param name="rounding">Optional corner rounding. Negative values resolve from the theme color family.</param>
        /// <param name="flags">Rounded corner flags.</param>
        public static void DrawCurrentWindowThemeBackdrop(FuColors color, float alphaMult = 1f, float rounding = -1f, FuDrawFlags flags = FuDrawFlags.RoundCornersAll)
        {
            float resolvedRounding = rounding >= 0f ? rounding : GetThemeBackdropRounding(color);
            DrawThemeBackdrop(new Rect(ImGui.GetWindowPos(), ImGui.GetWindowSize()), color, alphaMult, resolvedRounding, flags);
        }

        /// <summary>
        /// Returns the PopupBg color to push before opening a popup that draws its own backdrop.
        /// </summary>
        internal static Vector4 GetPopupBackdropStyleColor(float alphaMult = 0.98f)
        {
            return Themes.GetColor(FuColors.PopupBg, ShouldUseThemeBackdrop(FuColors.PopupBg, alphaMult) ? 0f : alphaMult);
        }

        /// <summary>
        /// Draws the standard popup backdrop over the current ImGui popup window.
        /// </summary>
        internal static void DrawCurrentPopupThemeBackdrop(float alphaMult = 0.98f, float rounding = -1f, float borderSize = -1f)
        {
            if (!ShouldUseThemeBackdrop(FuColors.PopupBg, alphaMult))
            {
                return;
            }

            ImGuiStylePtr style = ImGui.GetStyle();
            float resolvedRounding = rounding >= 0f ? rounding : style.PopupRounding;
            float resolvedBorderSize = borderSize >= 0f ? borderSize : style.PopupBorderSize;
            Rect backdropRect = new Rect(ImGui.GetWindowPos(), ImGui.GetWindowSize());
            float inset = Mathf.Max(0f, resolvedBorderSize);
            if (inset > 0f)
            {
                Vector2 insetVector = new Vector2(inset, inset);
                backdropRect.position += insetVector;
                backdropRect.size -= insetVector * 2f;
            }

            DrawThemeBackdrop(backdropRect, FuColors.PopupBg, alphaMult, Mathf.Max(0f, resolvedRounding - inset));
        }

        /// <summary>
        /// Returns whether a theme color should use a backdrop draw command instead of a native opaque background.
        /// </summary>
        /// <param name="color">Theme color to inspect.</param>
        /// <param name="alphaMult">Alpha multiplier applied by the caller.</param>
        /// <returns>True when blur is compiled in and visible for this theme color.</returns>
        internal static bool ShouldUseThemeBackdrop(FuColors color, float alphaMult = 1f)
        {
            Vector4 themeColor = Themes != null
                ? Themes.GetColor(color, alphaMult)
                : Vector4.zero;
            return ShouldUseBackdrop(themeColor, GetThemeBackdropBlur(color));
        }

        /// <summary>
        /// Returns whether a resolved color and blur radius should use a backdrop draw command.
        /// </summary>
        internal static bool ShouldUseBackdrop(Vector4 color, float blurRadius)
        {
#if FU_BACKDROP_ENABLED
            return blurRadius > 0f && color.w < 0.999f;
#else
            return false;
#endif
        }

        /// <summary>
        /// Returns whether the texture id represents a Fugui backdrop command.
        /// </summary>
        /// <param name="textureId">ImGui texture id.</param>
        /// <returns>True when this command should be handled by the backdrop renderer.</returns>
        internal static bool IsBackdropTextureID(IntPtr textureId)
        {
#if FU_BACKDROP_ENABLED
            return textureId == BackdropTextureID;
#else
            return false;
#endif
        }

        /// <summary>
        /// Returns the theme blur radius for the supported backdrop color families.
        /// </summary>
        internal static float GetThemeBackdropBlur(FuColors color)
        {
            if (Themes == null)
            {
                return 0f;
            }

            switch (color)
            {
                case FuColors.WindowBg:
                    return Mathf.Max(0f, Themes.WindowBlur);
                case FuColors.ChildBg:
                    return Mathf.Max(0f, Themes.ChildBlur);
                case FuColors.PopupBg:
                    return Mathf.Max(0f, Themes.PopupBlur);
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Returns the theme rounding for the supported backdrop color families.
        /// </summary>
        private static float GetThemeBackdropRounding(FuColors color)
        {
            if (Themes == null)
            {
                return 0f;
            }

            switch (color)
            {
                case FuColors.WindowBg:
                    return Themes.WindowRounding;
                case FuColors.ChildBg:
                    return Themes.ChildRounding;
                case FuColors.PopupBg:
                    return Themes.PopupRounding;
                default:
                    return 0f;
            }
        }

        private struct FuBackdropStyle
        {
            public static readonly FuBackdropStyle Default = new FuBackdropStyle(new Color(0f, 0f, 0f, 0f), 0f);

            public Color Color;
            public float BlurRadius;

            public FuBackdropStyle(Color color, float blurRadius)
            {
                Color = color;
                BlurRadius = blurRadius;
            }
        }
    }
}
