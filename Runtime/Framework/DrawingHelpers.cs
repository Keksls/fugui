using Fu.Framework;
using ImGuiNET;
using System;
using UnityEngine;

namespace Fu
{
    public static partial class Fugui
    {
        ///<summary>
        /// Draws a downward-pointing caret.
        /// <param name="drawList">Drawing list to add the caret to.</param>
        /// <param name="pos">Position of the caret.</param>
        /// <param name="carretSize">Size of the caret.</param>
        /// <param name="containerHeight">Height of the container.</param>
        /// <param name="color">Color of the caret.</param>
        ///</summary>
        public static void DrawCarret_Down(ImDrawListPtr drawList, Vector2 pos, float carretSize, float containerHeight, Color color)
        {
            // Add a filled triangle with vertices pointing downwards
            drawList.AddTriangleFilled(
                new Vector2(pos.x, pos.y + (containerHeight / 2f) - (carretSize / 2f)),
                new Vector2(pos.x + carretSize, pos.y + (containerHeight / 2f) - (carretSize / 2f)),
                new Vector2(pos.x + carretSize / 2f, pos.y + (containerHeight / 2f) + (carretSize / 2f)),
                ImGui.GetColorU32(color));
        }

        ///<summary>
        /// Draws an upward-pointing caret.
        /// <param name="drawList">Drawing list to add the caret to.</param>
        /// <param name="pos">Position of the caret.</param>
        /// <param name="carretSize">Size of the caret.</param>
        /// <param name="containerHeight">Height of the container.</param>
        /// <param name="color">Color of the caret.</param>
        ///</summary>
        public static void DrawCarret_Top(ImDrawListPtr drawList, Vector2 pos, float carretSize, float containerHeight, Color color)
        {
            // Add a filled triangle with vertices pointing upwards
            drawList.AddTriangleFilled(
                new Vector2(pos.x, pos.y + (containerHeight / 2f) + (carretSize / 2f)),
                new Vector2(pos.x + carretSize, pos.y + (containerHeight / 2f) + (carretSize / 2f)),
                new Vector2(pos.x + carretSize / 2f, pos.y + (containerHeight / 2f) - (carretSize / 2f)),
                ImGui.GetColorU32(color));
        }

        ///<summary>
        /// Draws a right-pointing caret.
        /// <param name="drawList">Drawing list to add the caret to.</param>
        /// <param name="pos">Position of the caret.</param>
        /// <param name="carretSize">Size of the caret.</param>
        /// <param name="containerHeight">Height of the container.</param>
        /// <param name="color">Color of the caret.</param>
        ///</summary>
        public static void DrawCarret_Right(ImDrawListPtr drawList, Vector2 pos, float carretSize, float containerHeight, Color color)
        {
            // Add a filled triangle with vertices pointing to the right
            drawList.AddTriangleFilled(
                new Vector2(pos.x, pos.y + (containerHeight / 2f) - (carretSize / 2f)),
                new Vector2(pos.x + carretSize, pos.y + (containerHeight / 2f)),
                new Vector2(pos.x, pos.y + (containerHeight / 2f) + (carretSize / 2f)),
                ImGui.GetColorU32(color));
        }

        ///<summary>
        /// Draws a left-pointing caret.
        /// <param name="drawList">Drawing list to add the caret to.</param>
        /// <param name="pos">Position of the caret.</param>
        /// <param name="carretSize">Size of the caret.</param>
        /// <param name="containerHeight">Height of the container.</param>
        /// <param name="color">Color of the caret.</param>
        ///</summary>
        public static void DrawCarret_Left(ImDrawListPtr drawList, Vector2 pos, float carretSize, float containerHeight, Color color)
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
        public static void DrawBubble(ImDrawListPtr drawList, Vector2 pos, float radius, string text, Color backgroundColor, Color textColor)
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
        public static void DrawBubbleWithTooltip(ImDrawListPtr drawList, Vector2 pos, float radius, string text, Color backgroundColor, Color textColor, FuTextStyle tooltipStyle, string tooltip = "")
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
        public static void DrawCheckMark(ImDrawListPtr drawList, Vector2 pos, Color col, float size)
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
        /// Move the current drawing X position of strenght
        /// </summary>
        /// <param name="strenght">quantity of pixels to move on X from here</param>
        public static void MoveX(float strenght)
        {
            if(strenght == 0) return;
            if (strenght < 0)
            {
                MoveXUnscaled(strenght * Scale);
                return;
            }
            ImGui.SetCursorScreenPos(new Vector2(ImGui.GetCursorScreenPos().x + strenght * Scale, ImGui.GetCursorScreenPos().y));
        }

        /// <summary>
        /// Move the current drawing Y position of strenght
        /// </summary>
        /// <param name="strenght">quantity of pixels to move on Y from here</param>
        public static void MoveY(float strenght)
        {
            if (strenght == 0) return;
            if (strenght < 0)
            {
                MoveYUnscaled(strenght * Scale);
                return;
            }
            ImGui.SetCursorScreenPos(new Vector2(ImGui.GetCursorScreenPos().x, ImGui.GetCursorScreenPos().y + strenght * Scale));
        }

        /// <summary>
        /// Move the current drawing X position of strenght
        /// </summary>
        /// <param name="strenght">quantity of pixels to move on X from here</param>
        public static void MoveXUnscaled(float strenght)
        {
            if (strenght == 0) return;
            if (strenght < 0)
            {
                strenght = ImGui.GetContentRegionAvail().x + strenght;
            }
            ImGui.SetCursorScreenPos(new Vector2(ImGui.GetCursorScreenPos().x + strenght, ImGui.GetCursorScreenPos().y));
        }

        /// <summary>
        /// Move the current drawing Y position of strenght
        /// </summary>
        /// <param name="strenght">quantity of pixels to move on Y from here</param>
        public static void MoveYUnscaled(float strenght)
        {
            if (strenght == 0) return;
            if (strenght < 0)
            {
                strenght = ImGui.GetContentRegionAvail().y + strenght;
            }
            ImGui.SetCursorScreenPos(new Vector2(ImGui.GetCursorScreenPos().x, ImGui.GetCursorScreenPos().y + strenght));
        }

        /// <summary>
        /// Move the current drawing X position to new local pos
        /// </summary>
        /// <param name="x">Local X position</param>
        public static void SetLocalX(float x)
        {
            ImGui.SetCursorPosX(x * Scale);
        }

        /// <summary>
        /// Move the current drawing Y position to new local pos
        /// </summary>
        /// <param name="y">Local Y position</param>
        public static void SetLocalY(float y)
        {
            ImGui.SetCursorPosY(y * Scale);
        }

        ///<summary>
        /// This method generates a tiled background using a given number of rows and an alternating pattern of dark and light tiles.
        /// It takes in an ImDrawList object, which is used to draw the tiles, as well as the position and size of the background.
        ///</summary>
        /// <param name="drawList">The ImDrawList object used to draw the tiles.</param>
        /// <param name="pos">The position of the tiled background.</param>
        /// <param name="size">The size of the tiled background.</param>
        /// <param name="numRows">The number of rows to use for the tiled background. Default value is 2.</param>
        public static void DrawTilesBackground(ImDrawListPtr drawList, Vector2 pos, Vector2 size, int numRows = 2)
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
        public static unsafe void DrawGradientRect(Vector2 pos, Vector2 size, float gradientStrenght, float rounding, ImDrawListPtr drawList, Vector4 color)
        {
            Vector4 bg2f = new Vector4(color.x * gradientStrenght, color.y * gradientStrenght, color.z * gradientStrenght, color.w);
            // draw button frame
            int vert_start_idx = drawList.VtxBuffer.Size;
            drawList.AddRectFilled(pos, pos + size, ImGuiNative.igGetColorU32_Vec4(color), rounding);
            int vert_end_idx = drawList.VtxBuffer.Size;
            ImGuiInternal.igShadeVertsLinearColorGradientKeepAlpha(drawList.NativePtr, vert_start_idx, vert_end_idx, pos, new Vector2(pos.x, pos.y + size.y), ImGuiNative.igGetColorU32_Vec4(color), ImGuiNative.igGetColorU32_Vec4(bg2f));
        }
    }
}