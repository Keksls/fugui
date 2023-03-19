using ImGuiNET;
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
        /// Move the current drawing X position of strenght
        /// </summary>
        /// <param name="strenght">quantity of pixels to move on X from here</param>
        public static void MoveX(float strenght)
        {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + strenght);
        }

        /// <summary>
        /// Move the current drawing Y position of strenght
        /// </summary>
        /// <param name="strenght">quantity of pixels to move on Y from here</param>
        public static void MoveY(float strenght)
        {
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + strenght);
        }
    }
}