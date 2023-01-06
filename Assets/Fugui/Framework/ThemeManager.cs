using UnityEngine;
using ImGuiNET;
using System.Collections.Generic;
using System;
using System.Reflection;

namespace Fugui.Framework
{
    public static class ThemeManager
    {
        public static Vector4[] CurrentStyleColor = null;
        public static event Action<Theme> OnThemeSet;
        public static Theme CurrentTheme { get; private set; }
        private static List<Type> UIElementStyleTypes;

        static ThemeManager()
        {
            // we could not bind it using reflection because some element style use others, so we need to set presset within a specific order
            UIElementStyleTypes = new List<Type>();
            UIElementStyleTypes.Add(typeof(UITextStyle));
            UIElementStyleTypes.Add(typeof(UIButtonStyle));
            UIElementStyleTypes.Add(typeof(UIFrameStyle));
            UIElementStyleTypes.Add(typeof(UISliderStyle));
            UIElementStyleTypes.Add(typeof(UIComboboxStyle));
            UIElementStyleTypes.Add(typeof(UIContainerStyle));
            UIElementStyleTypes.Add(typeof(UILayoutStyle));
            UIElementStyleTypes.Add(typeof(UICollapsableStyle));
        }

        public static void SetTheme(Theme theme)
        {
            switch (theme)
            {
                default:
                case Theme.Dark:
                    setDarkTheme();
                    break;
                case Theme.Light:
                    setLightTheme();
                    break;
            }
            CurrentTheme = theme;
            // call OnThemeSet on each structs that inherit from 
            foreach (Type structType in UIElementStyleTypes)
            {
                var myFunctionMethod = structType.GetMethod("OnThemeSet", BindingFlags.NonPublic | BindingFlags.Static);
                myFunctionMethod.Invoke(null, null);
            }
            OnThemeSet?.Invoke(CurrentTheme);
        }

        private static void setDarkTheme()
        {
            var style = ImGui.GetStyle();
            style.Alpha = 1.0f;
            style.WindowPadding = new Vector2(1.0f, 0.0f);
            style.WindowRounding = 2.0f;
            style.WindowBorderSize = 1.0f;
            style.WindowMinSize = new Vector2(64.0f, 64.0f);
            style.WindowTitleAlign = new Vector2(0.0f, 0.5f);
            style.WindowMenuButtonPosition = ImGuiDir.Right;
            style.ChildRounding = 2.0f;
            style.ChildBorderSize = 1.0f;
            style.PopupRounding = 2.0f;
            style.PopupBorderSize = 1.0f;
            style.FramePadding = new Vector2(8f, 4f);
            style.FrameRounding = 2.0f;
            style.FrameBorderSize = 1.1f;
            style.ItemSpacing = new Vector2(4.0f, 6.0f);
            style.ItemInnerSpacing = new Vector2(4.0f, 6.0f);
            style.CellPadding = new Vector2(6.0f, 1.0f);
            style.IndentSpacing = 24.0f;
            style.ColumnsMinSpacing = 6.0f;
            style.ScrollbarSize = 14.0f;
            style.ScrollbarRounding = 2.0f;
            style.GrabMinSize = 10.0f;
            style.GrabRounding = 2.0f;
            style.TabRounding = 2.0f;
            style.TabBorderSize = 1.0f;
            style.TabMinWidthForCloseButton = 0.0f;
            style.ColorButtonPosition = ImGuiDir.Right;
            style.ButtonTextAlign = new Vector2(0.5f, 0.5f);
            style.SelectableTextAlign = new Vector2(0.0f, 0.0f);
            style.CircleTessellationMaxError = 0.75f;
            style.AntiAliasedLinesUseTex = false;
            style.AntiAliasedLines = true;
            style.AntiAliasedFill = true;

            CurrentStyleColor = new Vector4[(int)ImGuiCustomCol.COUNT];
            // imgui colors
            CurrentStyleColor[(int)ImGuiCol.Text] = new Vector4(186f / 255f, 183f / 255f, 176f / 255f, 1.0f);
            CurrentStyleColor[(int)ImGuiCol.TextDisabled] = new Vector4(0.3764705955982208f, 0.3764705955982208f, 0.3764705955982208f, 1.0f);
            CurrentStyleColor[(int)ImGuiCol.WindowBg] = new Vector4(46f / 255f, 46f / 255f, 46f / 255f, 1.0f);
            CurrentStyleColor[(int)ImGuiCol.ChildBg] = new Vector4(0.164705f, 0.164705f, 0.164705f, 1.0f);
            CurrentStyleColor[(int)ImGuiCol.PopupBg] = new Vector4(0.1882352977991104f, 0.1882352977991104f, 0.1882352977991104f, 1.0f);
            CurrentStyleColor[(int)ImGuiCol.Border] = new Vector4(24f / 255f, 24f / 255f, 24f / 255f, 1.0f);
            CurrentStyleColor[(int)ImGuiCol.BorderShadow] = Vector4.zero;//  new Vector4(0.0f, 0.0f, 0.0f, 0.28f);
            CurrentStyleColor[(int)ImGuiCol.FrameBg] = new Vector4(35f / 255f, 35f / 255f, 35f / 255f, 1.0f);
            CurrentStyleColor[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.09442061185836792f, 0.09441966563463211f, 0.09441966563463211f, 0.5400000214576721f);
            CurrentStyleColor[(int)ImGuiCol.FrameBgActive] = new Vector4(0.1802556961774826f, 0.1802569776773453f, 0.1802574992179871f, 1.0f);
            CurrentStyleColor[(int)ImGuiCol.TitleBg] = new Vector4(34f / 255f, 34f / 255f, 34f / 255f, 1.0f);
            CurrentStyleColor[(int)ImGuiCol.TitleBgActive] = new Vector4(34f / 255f, 34f / 255f, 34f / 255f, 1.0f);
            CurrentStyleColor[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(34f / 255f, 34f / 255f, 34f / 255f, 1.0f);
            CurrentStyleColor[(int)ImGuiCol.MenuBarBg] = new Vector4(0.09411764889955521f, 0.09411764889955521f, 0.09411764889955521f, 1.0f);
            CurrentStyleColor[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.0470588244497776f, 0.0470588244497776f, 0.0470588244497776f, 0.5411764979362488f);
            CurrentStyleColor[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.2823529541492462f, 0.2823529541492462f, 0.2823529541492462f, 0.5411764979362488f);
            CurrentStyleColor[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.321568638086319f, 0.321568638086319f, 0.321568638086319f, 0.5411764979362488f);
            CurrentStyleColor[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0.4392156898975372f, 0.4392156898975372f, 0.4392156898975372f, 0.5411764979362488f);
            CurrentStyleColor[(int)ImGuiCol.CheckMark] = new Vector4(0.3294117748737335f, 0.6666666865348816f, 0.8588235378265381f, 1.0f);
            CurrentStyleColor[(int)ImGuiCol.SliderGrab] = new Vector4(0.3372549116611481f, 0.3372549116611481f, 0.3372549116611481f, 0.5400000214576721f);
            CurrentStyleColor[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.5568627715110779f, 0.5568627715110779f, 0.5568627715110779f, 0.5400000214576721f);
            CurrentStyleColor[(int)ImGuiCol.Button] = new Vector4(58f / 255f, 58f / 255f, 58f / 255f, 1f);
            CurrentStyleColor[(int)ImGuiCol.ButtonHovered] = new Vector4(64f / 255f, 64f / 255f, 64f / 255f, 1f);
            CurrentStyleColor[(int)ImGuiCol.ButtonActive] = new Vector4(42f / 255f, 68f / 255f, 83f / 255f, 1f);
            CurrentStyleColor[(int)ImGuiCol.Header] = new Vector4(0.1098039224743843f, 0.1098039224743843f, 0.1098039224743843f, 1.0f);
            CurrentStyleColor[(int)ImGuiCol.HeaderHovered] = new Vector4(42f / 255f, 68f / 255f, 83f / 255f, 0.5f);
            CurrentStyleColor[(int)ImGuiCol.HeaderActive] = new Vector4(0.2000000029802322f, 0.2196078449487686f, 0.2274509817361832f, 0.3300000131130219f);
            CurrentStyleColor[(int)ImGuiCol.Separator] = new Vector4(0.1098039224743843f, 0.1098039224743843f, 0.1098039224743843f, 1.0f);
            CurrentStyleColor[(int)ImGuiCol.SeparatorHovered] = new Vector4(0.0313725508749485f, 0.0313725508749485f, 0.0313725508749485f, 1.0f);
            CurrentStyleColor[(int)ImGuiCol.SeparatorActive] = new Vector4(0.250980406999588f, 0.250980406999588f, 0.250980406999588f, 1.0f);
            CurrentStyleColor[(int)ImGuiCol.ResizeGrip] = new Vector4(0.1098039224743843f, 0.1098039224743843f, 0.1098039224743843f, 1.0f);
            CurrentStyleColor[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.0313725508749485f, 0.0313725508749485f, 0.0313725508749485f, 1.0f);
            CurrentStyleColor[(int)ImGuiCol.ResizeGripActive] = new Vector4(0.250980406999588f, 0.250980406999588f, 0.250980406999588f, 1.0f);
            CurrentStyleColor[(int)ImGuiCol.Tab] = new Vector4(34f / 255f, 34f / 255f, 34f / 255f, 1.0f);
            CurrentStyleColor[(int)ImGuiCol.TabHovered] = new Vector4(0.250980406999588f, 0.250980406999588f, 0.250980406999588f, 1.0f);
            CurrentStyleColor[(int)ImGuiCol.TabActive] = new Vector4(46f / 255f, 46f / 255f, 46f / 255f, 1.0f);
            CurrentStyleColor[(int)ImGuiCol.TabUnfocused] = new Vector4(34f / 255f, 34f / 255f, 34f / 255f, 1.0f);
            CurrentStyleColor[(int)ImGuiCol.TabUnfocusedActive] = new Vector4(46f / 255f, 46f / 255f, 46f / 255f, 1.0f);
            CurrentStyleColor[(int)ImGuiCol.PlotLines] = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
            CurrentStyleColor[(int)ImGuiCol.PlotLinesHovered] = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
            CurrentStyleColor[(int)ImGuiCol.PlotHistogram] = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
            CurrentStyleColor[(int)ImGuiCol.PlotHistogramHovered] = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
            CurrentStyleColor[(int)ImGuiCol.TableHeaderBg] = new Vector4(0.0f, 0.0f, 0.0f, 0.5199999809265137f);
            CurrentStyleColor[(int)ImGuiCol.TableBorderStrong] = new Vector4(0.0f, 0.0f, 0.0f, 0.5199999809265137f);
            CurrentStyleColor[(int)ImGuiCol.TableBorderLight] = new Vector4(0.2784313857555389f, 0.2784313857555389f, 0.2784313857555389f, 0.2899999916553497f);
            CurrentStyleColor[(int)ImGuiCol.TableRowBg] = new Vector4(1.0f, 1.0f, 1.0f, 1f / 255f);
            CurrentStyleColor[(int)ImGuiCol.TableRowBgAlt] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
            CurrentStyleColor[(int)ImGuiCol.TextSelectedBg] = new Vector4(0.2000000029802322f, 0.2196078449487686f, 0.2274509817361832f, 1.0f);
            CurrentStyleColor[(int)ImGuiCol.DragDropTarget] = new Vector4(0.3294117748737335f, 0.6666666865348816f, 0.8588235378265381f, 1.0f);
            CurrentStyleColor[(int)ImGuiCol.NavHighlight] = new Vector4(0.3294117748737335f, 0.6666666865348816f, 0.8588235378265381f, 1.0f);
            CurrentStyleColor[(int)ImGuiCol.NavWindowingHighlight] = new Vector4(0.1764705926179886f, 0.407843142747879f, 0.5372549295425415f, 1.0f);
            CurrentStyleColor[(int)ImGuiCol.NavWindowingDimBg] = new Vector4(0.09411764889955521f, 0.09411764889955521f, 0.09411764889955521f, 0.7843137383460999f);
            CurrentStyleColor[(int)ImGuiCol.ModalWindowDimBg] = new Vector4(0.09411764889955521f, 0.09411764889955521f, 0.09411764889955521f, 0.7843137383460999f);
            CurrentStyleColor[(int)ImGuiCol.DockingEmptyBg] = CurrentStyleColor[(int)ImGuiCol.FrameBg];
            CurrentStyleColor[(int)ImGuiCol.DockingPreview] = Color.black;
            // custom colors
            CurrentStyleColor[(int)ImGuiCustomCol.Highlight] = new Vector4(1f / 255f, 122f / 255f, 1f, 1f);
            CurrentStyleColor[(int)ImGuiCustomCol.HighlightHovered] = new Vector4(1f / 255f * 0.9f, 122f / 255f * 0.9f, 1f * 0.9f, 1f);
            CurrentStyleColor[(int)ImGuiCustomCol.HighlightActive] = new Vector4(1f / 255f * 0.8f, 122f / 255f * 0.8f, 1f * 0.8f, 1f);
            CurrentStyleColor[(int)ImGuiCustomCol.HighlightDisabled] = new Vector4(1f / 255f * 0.5f, 122f / 255f * 0.5f, 1f * 0.5f, 1f);
            CurrentStyleColor[(int)ImGuiCustomCol.FrameHoverFeedback] = new Vector4(0.8f, 0.8f, 0.8f, 0.2f);
            CurrentStyleColor[(int)ImGuiCustomCol.FrameSelectedFeedback] = new Vector4(42f / 255f, 68f / 255f, 83f / 255f, 1f);
            CurrentStyleColor[(int)ImGuiCustomCol.Collapsable] = new Vector4(0.205f, 0.205f, 0.205f, 1f);
            CurrentStyleColor[(int)ImGuiCustomCol.CollapsableHovered] = new Vector4(0.22f, 0.22f, 0.22f, 1f);
            CurrentStyleColor[(int)ImGuiCustomCol.CollapsableActive] = new Vector4(0.25f, 0.25f, 0.25f, 1f);
            CurrentStyleColor[(int)ImGuiCustomCol.CollapsableDisabled] = new Vector4(0.18f, 0.18f, 0.18f, 1f) * 0.5f;
            CurrentStyleColor[(int)ImGuiCustomCol.SliderLine] = new Vector4(0.5f, 0.5f, 0.5f, 1f);
            CurrentStyleColor[(int)ImGuiCustomCol.SliderKnob] = CurrentStyleColor[(int)ImGuiCol.CheckMark];
            CurrentStyleColor[(int)ImGuiCustomCol.SliderLineDisabled] = new Vector4(0.5f, 0.5f, 0.5f, 0.75f);
            CurrentStyleColor[(int)ImGuiCustomCol.SliderKnobDisabled] = new Vector4(0.5f, 0.5f, 0.5f, 0.75f);

            // set style colors
            for (int i = 0; i < (int)ImGuiCol.COUNT; i++)
            {
                style.Colors[i] = CurrentStyleColor[i];
            }
        }

        // ugly for now but, if we want to change theme at runtime, we need to refresh backed IUIElementStyle structs. 
        // no nice way to do that. simplest way is to restart the soft, but we don't want to
        private static void setLightTheme()
        {
            var style = ImGui.GetStyle();
            style.Alpha = 1.0f;
            style.WindowPadding = new Vector2(1.0f, 0.0f);
            style.WindowRounding = 2.0f;
            style.WindowBorderSize = 1.0f;
            style.WindowMinSize = new Vector2(64.0f, 64.0f);
            style.WindowTitleAlign = new Vector2(0.0f, 0.5f);
            style.WindowMenuButtonPosition = ImGuiDir.Right;
            style.ChildRounding = 2.0f;
            style.ChildBorderSize = 1.0f;
            style.PopupRounding = 2.0f;
            style.PopupBorderSize = 1.0f;
            style.FramePadding = new Vector2(8f, 4f);
            style.FrameRounding = 2.0f;
            style.FrameBorderSize = 1.1f;
            style.ItemSpacing = new Vector2(4.0f, 6.0f);
            style.ItemInnerSpacing = new Vector2(4.0f, 6.0f);
            style.CellPadding = new Vector2(6.0f, 1.0f);
            style.IndentSpacing = 24.0f;
            style.ColumnsMinSpacing = 6.0f;
            style.ScrollbarSize = 14.0f;
            style.ScrollbarRounding = 2.0f;
            style.GrabMinSize = 10.0f;
            style.GrabRounding = 2.0f;
            style.TabRounding = 2.0f;
            style.TabBorderSize = 1.0f;
            style.TabMinWidthForCloseButton = 0.0f;
            style.ColorButtonPosition = ImGuiDir.Right;
            style.ButtonTextAlign = new Vector2(0.5f, 0.5f);
            style.SelectableTextAlign = new Vector2(0.0f, 0.0f);
            style.CircleTessellationMaxError = 0.75f;
            style.AntiAliasedLinesUseTex = false;
            style.AntiAliasedLines = true;
            style.AntiAliasedFill = true;

            // imgui colors
            CurrentStyleColor = new Vector4[(int)ImGuiCustomCol.COUNT];

            CurrentStyleColor[(int)ImGuiCol.Text] = new Vector4(0.15f, 0.15f, 0.15f, 1.00f);
            CurrentStyleColor[(int)ImGuiCol.TextDisabled] = new Vector4(0.60f, 0.60f, 0.60f, 1.00f);
            CurrentStyleColor[(int)ImGuiCol.WindowBg] = new Vector4(0.87f, 0.87f, 0.87f, 1.00f);
            CurrentStyleColor[(int)ImGuiCol.ChildBg] = new Vector4(0.87f, 0.87f, 0.87f, 1.00f);
            CurrentStyleColor[(int)ImGuiCol.PopupBg] = new Vector4(0.87f, 0.87f, 0.87f, 1.00f);
            CurrentStyleColor[(int)ImGuiCol.Border] = new Vector4(0.89f, 0.89f, 0.89f, 1.00f);
            CurrentStyleColor[(int)ImGuiCol.BorderShadow] = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
            CurrentStyleColor[(int)ImGuiCol.FrameBg] = new Vector4(0.93f, 0.93f, 0.93f, 1.00f);
            CurrentStyleColor[(int)ImGuiCol.FrameBgHovered] = new Vector4(1.00f, 0.69f, 0.07f, 0.69f);
            CurrentStyleColor[(int)ImGuiCol.FrameBgActive] = new Vector4(1.00f, 0.82f, 0.46f, 0.69f);
            CurrentStyleColor[(int)ImGuiCol.TitleBg] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            CurrentStyleColor[(int)ImGuiCol.TitleBgActive] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            CurrentStyleColor[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            CurrentStyleColor[(int)ImGuiCol.MenuBarBg] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            CurrentStyleColor[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.87f, 0.87f, 0.87f, 1.00f);
            CurrentStyleColor[(int)ImGuiCol.ScrollbarGrab] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            CurrentStyleColor[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(1.00f, 0.69f, 0.07f, 0.69f);
            CurrentStyleColor[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(1.00f, 0.82f, 0.46f, 0.69f);
            CurrentStyleColor[(int)ImGuiCol.CheckMark] = new Vector4(0.01f, 0.01f, 0.01f, 0.63f);
            CurrentStyleColor[(int)ImGuiCol.SliderGrab] = new Vector4(1.00f, 0.69f, 0.07f, 0.69f);
            CurrentStyleColor[(int)ImGuiCol.SliderGrabActive] = new Vector4(1.00f, 0.82f, 0.46f, 0.69f);
            CurrentStyleColor[(int)ImGuiCol.Button] = new Vector4(0.83f, 0.83f, 0.83f, 1.00f);
            CurrentStyleColor[(int)ImGuiCol.ButtonHovered] = new Vector4(1.00f, 0.69f, 0.07f, 0.69f);
            CurrentStyleColor[(int)ImGuiCol.ButtonActive] = new Vector4(1.00f, 0.82f, 0.46f, 0.69f);
            CurrentStyleColor[(int)ImGuiCol.Header] = new Vector4(0.67f, 0.67f, 0.67f, 1.00f);
            CurrentStyleColor[(int)ImGuiCol.HeaderHovered] = new Vector4(1.00f, 0.69f, 0.07f, 1.00f);
            CurrentStyleColor[(int)ImGuiCol.HeaderActive] = new Vector4(1.00f, 0.82f, 0.46f, 0.69f);
            CurrentStyleColor[(int)ImGuiCol.Separator] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            CurrentStyleColor[(int)ImGuiCol.SeparatorHovered] = new Vector4(1.00f, 0.69f, 0.07f, 1.00f);
            CurrentStyleColor[(int)ImGuiCol.SeparatorActive] = new Vector4(1.00f, 0.82f, 0.46f, 0.69f);
            CurrentStyleColor[(int)ImGuiCol.ResizeGrip] = new Vector4(1.00f, 1.00f, 1.00f, 0.18f);
            CurrentStyleColor[(int)ImGuiCol.ResizeGripHovered] = new Vector4(1.00f, 0.69f, 0.07f, 1.00f);
            CurrentStyleColor[(int)ImGuiCol.ResizeGripActive] = new Vector4(1.00f, 0.82f, 0.46f, 0.69f);
            CurrentStyleColor[(int)ImGuiCol.Tab] = new Vector4(0.16f, 0.16f, 0.16f, 0.00f);
            CurrentStyleColor[(int)ImGuiCol.TabHovered] = new Vector4(1.00f, 0.69f, 0.07f, 1.00f);
            CurrentStyleColor[(int)ImGuiCol.TabActive] = new Vector4(1.00f, 0.69f, 0.07f, 1.00f);
            CurrentStyleColor[(int)ImGuiCol.TabUnfocused] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            CurrentStyleColor[(int)ImGuiCol.TabUnfocusedActive] = new Vector4(0.87f, 0.87f, 0.87f, 1.00f);
            CurrentStyleColor[(int)ImGuiCol.DockingPreview] = new Vector4(1.00f, 0.82f, 0.46f, 0.69f);
            CurrentStyleColor[(int)ImGuiCol.DockingEmptyBg] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            CurrentStyleColor[(int)ImGuiCol.PlotLines] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            CurrentStyleColor[(int)ImGuiCol.PlotLinesHovered] = new Vector4(0.90f, 0.70f, 0.00f, 1.00f);
            CurrentStyleColor[(int)ImGuiCol.PlotHistogram] = new Vector4(0.90f, 0.70f, 0.00f, 1.00f);
            CurrentStyleColor[(int)ImGuiCol.PlotHistogramHovered] = new Vector4(1.00f, 0.60f, 0.00f, 1.00f);
            CurrentStyleColor[(int)ImGuiCol.TextSelectedBg] = new Vector4(1.00f, 0.69f, 0.07f, 1.00f);
            CurrentStyleColor[(int)ImGuiCol.DragDropTarget] = new Vector4(1.00f, 0.69f, 0.07f, 1.00f);
            CurrentStyleColor[(int)ImGuiCol.NavHighlight] = new Vector4(1.00f, 0.69f, 0.07f, 1.00f);
            CurrentStyleColor[(int)ImGuiCol.NavWindowingHighlight] = new Vector4(1.00f, 1.00f, 1.00f, 0.70f);
            CurrentStyleColor[(int)ImGuiCol.NavWindowingDimBg] = new Vector4(0.87f, 0.87f, 0.87f, 1.00f);
            CurrentStyleColor[(int)ImGuiCol.ModalWindowDimBg] = new Vector4(0.20f, 0.20f, 0.20f, 0.35f);
            CurrentStyleColor[(int)ImGuiCol.DockingEmptyBg] = CurrentStyleColor[(int)ImGuiCol.FrameBg];
            CurrentStyleColor[(int)ImGuiCol.DockingPreview] = Color.white;

            // custom colors
            CurrentStyleColor[(int)ImGuiCustomCol.Highlight] = new Vector4(1f / 255f, 122f / 255f, 1f, 1f);
            CurrentStyleColor[(int)ImGuiCustomCol.HighlightHovered] = new Vector4(1f / 255f * 0.9f, 122f / 255f * 0.9f, 1f * 0.9f, 1f);
            CurrentStyleColor[(int)ImGuiCustomCol.HighlightActive] = new Vector4(1f / 255f * 0.8f, 122f / 255f * 0.8f, 1f * 0.8f, 1f);
            CurrentStyleColor[(int)ImGuiCustomCol.HighlightDisabled] = new Vector4(1f / 255f * 0.5f, 122f / 255f * 0.5f, 1f * 0.5f, 1f);
            CurrentStyleColor[(int)ImGuiCustomCol.FrameHoverFeedback] = new Vector4(0.8f, 0.8f, 0.8f, 0.2f);
            CurrentStyleColor[(int)ImGuiCustomCol.FrameSelectedFeedback] = new Vector4(42f / 255f, 68f / 255f, 83f / 255f, 1f);
            CurrentStyleColor[(int)ImGuiCustomCol.Collapsable] = new Vector4(0.805f, 0.805f, 0.805f, 1f);
            CurrentStyleColor[(int)ImGuiCustomCol.CollapsableHovered] = new Vector4(0.752f, 0.752f, 0.752f, 1f);
            CurrentStyleColor[(int)ImGuiCustomCol.CollapsableActive] = new Vector4(0.625f, 0.625f, 0.625f, 1f);
            CurrentStyleColor[(int)ImGuiCustomCol.CollapsableDisabled] = new Vector4(0.618f, 0.618f, 0.618f, 1f) * 0.5f;
            CurrentStyleColor[(int)ImGuiCustomCol.SliderLine] = new Vector4(0.5f, 0.5f, 0.5f, 1f);
            CurrentStyleColor[(int)ImGuiCustomCol.SliderKnob] = CurrentStyleColor[(int)ImGuiCol.CheckMark];
            CurrentStyleColor[(int)ImGuiCustomCol.SliderLineDisabled] = new Vector4(0.5f, 0.5f, 0.5f, 0.75f);
            CurrentStyleColor[(int)ImGuiCustomCol.SliderKnobDisabled] = new Vector4(0.5f, 0.5f, 0.5f, 0.75f);

            // set style colors
            for (int i = 0; i < (int)ImGuiCol.COUNT; i++)
            {
                style.Colors[i] = CurrentStyleColor[i];
            }
        }

        public static Vector4 GetColor(ImGuiCol color)
        {
            return CurrentStyleColor[(int)color];
        }

        public static Vector4 GetColor(ImGuiCustomCol color)
        {
            return CurrentStyleColor[(int)color];
        }
    }

    public enum ImGuiCustomCol // must start at ImGuiCol.COUNT
    {
        Highlight = 56,
        HighlightHovered = 57,
        HighlightActive = 58,
        HighlightDisabled = 59,
        FrameHoverFeedback = 60,
        FrameSelectedFeedback = 61,
        Collapsable = 62,
        CollapsableHovered = 63,
        CollapsableActive = 64,
        CollapsableDisabled = 65,
        SliderLine = 66,
        SliderLineDisabled = 67,
        SliderKnob = 68,
        SliderKnobDisabled = 69,

        COUNT = 70
    }

    public enum Theme
    {
        Dark,
        Light
    }
}