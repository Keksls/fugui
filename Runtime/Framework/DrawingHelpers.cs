using ImGuiNET;
using UnityEngine;

namespace Fu
{
    public static partial class Fugui
    {
        public static void DrawCarret_Down(ImDrawListPtr drawList, Vector2 pos, float carretSize, float containerHeight, Color color)
        {
            drawList.AddTriangleFilled(
                new Vector2(pos.x, pos.y + (containerHeight / 2f) - (carretSize / 2f)),
                new Vector2(pos.x + carretSize, pos.y + (containerHeight / 2f) - (carretSize / 2f)),
                new Vector2(pos.x + carretSize / 2f, pos.y + (containerHeight / 2f) + (carretSize / 2f)),
                ImGui.GetColorU32(color));
        }

        public static void DrawCarret_Right(ImDrawListPtr drawList, Vector2 pos, float carretSize, float containerHeight, Color color)
        {
            drawList.AddTriangleFilled(
                new Vector2(pos.x, pos.y + (containerHeight / 2f) - (carretSize / 2f)),
                new Vector2(pos.x + carretSize, pos.y + (containerHeight / 2f)),
                new Vector2(pos.x, pos.y + (containerHeight / 2f) + (carretSize / 2f)),
                ImGui.GetColorU32(color));
        }
    }
}