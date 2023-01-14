using ImGuiNET;
using UnityEngine;

namespace Fugui.Framework
{
    public class FuguiTheme
    {
        [SliderFloatAtttribute(0.1f, 1f)]
        public float Alpha = 1.0f;
        [DragVector2Atttribute(0f, 10f)]
        public Vector2 WindowPadding = new Vector2(1.0f, 0.0f);
        [SliderFloatAtttribute(0f, 8f)]
        public float WindowRounding = 2.0f;
        [SliderFloatAtttribute(0f, 4f)]
        public float WindowBorderSize = 1.0f;
        [DragVector2Atttribute(1f, 100f)]
        public Vector2 WindowMinSize = new Vector2(64.0f, 64.0f);
        [DragVector2Atttribute(0f, 1f)]
        public Vector2 WindowTitleAlign = new Vector2(0.0f, 0.5f);
        [ComboboxAtttribute]
        public ImGuiDir WindowMenuButtonPosition = ImGuiDir.Right;
        [SliderFloatAtttribute(0f, 10f)]
        public float ChildRounding = 2.0f;
        [SliderFloatAtttribute(0f, 10f)]
        public float ChildBorderSize = 1.0f;
        [SliderFloatAtttribute(0f, 10f)]
        public float PopupRounding = 2.0f;
        [SliderFloatAtttribute(0f, 10f)]
        public float PopupBorderSize = 1.0f;
        [DragVector2Atttribute(0f, 10f)]
        public Vector2 FramePadding = new Vector2(8f, 4f);
        [SliderFloatAtttribute(0f, 10f)]
        public float FrameRounding = 2.0f;
        [SliderFloatAtttribute(0f, 10f)]
        public float FrameBorderSize = 1.1f;
        [DragVector2Atttribute(0f, 20f)]
        public Vector2 ItemSpacing = new Vector2(4.0f, 6.0f);
        [DragVector2Atttribute(0f, 20f)]
        public Vector2 ItemInnerSpacing = new Vector2(4.0f, 6.0f);
        [DragVector2Atttribute(0f, 20f)]
        public Vector2 CellPadding = new Vector2(6.0f, 1.0f);
        [SliderFloatAtttribute(0f, 50f)]
        public float IndentSpacing = 24.0f;
        [SliderFloatAtttribute(0f, 50f)]
        public float ColumnsMinSpacing = 6.0f;
        [SliderFloatAtttribute(0f, 50f)]
        public float ScrollbarSize = 14.0f;
        [SliderFloatAtttribute(0f, 10f)]
        public float ScrollbarRounding = 2.0f;
        [SliderFloatAtttribute(0f, 50f)]
        public float GrabMinSize = 10.0f;
        [SliderFloatAtttribute(0f, 10f)]
        public float GrabRounding = 2.0f;
        [SliderFloatAtttribute(0f, 10f)]
        public float TabRounding = 2.0f;
        [SliderFloatAtttribute(0f, 10f)]
        public float TabBorderSize = 1.0f;
        [SliderFloatAtttribute(0f, 10f)]
        public float TabMinWidthForCloseButton = 0.0f;
        [ComboboxAtttribute]
        public ImGuiDir ColorButtonPosition = ImGuiDir.Right;
        [DragVector2Atttribute(0f, 1f)]
        public Vector2 ButtonTextAlign = new Vector2(0.5f, 0.5f);
        [DragVector2Atttribute(0f, 1f)]
        public Vector2 SelectableTextAlign = new Vector2(0.0f, 0.0f);
        [SliderFloatAtttribute(0.001f, 2f)]
        public float CircleTessellationMaxError = 0.01f;
        [ToggleAtttribute]
        public bool AntiAliasedLinesUseTex = false;
        [ToggleAtttribute]
        public bool AntiAliasedLines = true;
        [ToggleAtttribute]
        public bool AntiAliasedFill = true;

        public void Apply()
        {
            var style = ImGui.GetStyle();
            style.Alpha = Alpha;
            style.WindowPadding = WindowPadding;
            style.WindowRounding = WindowRounding;
            style.WindowBorderSize = WindowBorderSize;
            style.WindowMinSize = WindowMinSize;
            style.WindowTitleAlign = WindowTitleAlign;
            style.WindowMenuButtonPosition = ImGuiDir.Right;
            style.ChildRounding = ChildRounding;
            style.ChildBorderSize = ChildBorderSize;
            style.PopupRounding = PopupRounding;
            style.PopupBorderSize = PopupBorderSize;
            style.FramePadding = FramePadding;
            style.FrameRounding = FrameRounding;
            style.FrameBorderSize = FrameBorderSize;
            style.ItemSpacing = ItemSpacing;
            style.ItemInnerSpacing = ItemInnerSpacing;
            style.CellPadding = CellPadding;
            style.IndentSpacing = IndentSpacing;
            style.ColumnsMinSpacing = ColumnsMinSpacing;
            style.ScrollbarSize = ScrollbarSize;
            style.ScrollbarRounding = ScrollbarRounding;
            style.GrabMinSize = GrabMinSize;
            style.GrabRounding = GrabRounding;
            style.TabRounding = TabRounding;
            style.TabBorderSize = TabBorderSize;
            style.TabMinWidthForCloseButton = TabMinWidthForCloseButton;
            style.ColorButtonPosition = ColorButtonPosition;
            style.ButtonTextAlign = ButtonTextAlign;
            style.SelectableTextAlign = SelectableTextAlign;
            style.CircleTessellationMaxError = CircleTessellationMaxError;
            style.AntiAliasedLinesUseTex = AntiAliasedLinesUseTex;
            style.AntiAliasedLines = AntiAliasedLines;
            style.AntiAliasedFill = AntiAliasedFill;
        }
    }
}