using ImGuiNET;
using UnityEngine;

namespace Fugui.Framework
{
    public class FuguiTheme
    {
        [Disabled]
        public string ThemeName = "Fugui Theme";
        [Disabled]
        [Slider(0.1f, 1f)]
        public float Alpha = 1.0f;
        [Drag(0f, 10f)]
        public Vector2 WindowPadding = new Vector2(1.0f, 0.0f);
        [Slider(0f, 8f)]
        public float WindowRounding = 2.0f;
        [Slider(0f, 4f)]
        public float WindowBorderSize = 1.0f;
        [Drag(1f, 100f, "width", "height")]
        public Vector2 WindowMinSize = new Vector2(64.0f, 64.0f);
        [Drag(0f, 1f)]
        public Vector2 WindowTitleAlign = new Vector2(0.0f, 0.5f);
        public ImGuiDir WindowMenuButtonPosition = ImGuiDir.Right;
        [Slider(0f, 10f)]
        public float ChildRounding = 2.0f;
        [Slider(0f, 10f)]
        public float ChildBorderSize = 1.0f;
        [Slider(0f, 10f)]
        public float PopupRounding = 2.0f;
        [Slider(0f, 10f)]
        public float PopupBorderSize = 1.0f;
        [Drag(0f, 10f)]
        public Vector2 FramePadding = new Vector2(8f, 4f);
        [Slider(0f, 10f)]
        public float FrameRounding = 2.0f;
        [Slider(0f, 10f)]
        public float FrameBorderSize = 1.1f;
        [Drag(0f, 20f)]
        public Vector2 ItemSpacing = new Vector2(4.0f, 6.0f);
        [Drag(0f, 20f)]
        public Vector2 ItemInnerSpacing = new Vector2(4.0f, 6.0f);
        [Drag(0f, 20f)]
        public Vector2 CellPadding = new Vector2(6.0f, 1.0f);
        [Slider(0f, 50f)]
        public float IndentSpacing = 24.0f;
        [Slider(0f, 50f)]
        public float ColumnsMinSpacing = 6.0f;
        [Slider(0f, 50f)]
        public float ScrollbarSize = 14.0f;
        [Slider(0f, 10f)]
        public float ScrollbarRounding = 2.0f;
        [Slider(0f, 50f)]
        public float GrabMinSize = 10.0f;
        [Slider(0f, 10f)]
        public float GrabRounding = 2.0f;
        [Slider(0f, 10f)]
        public float TabRounding = 2.0f;
        [Slider(0f, 10f)]
        public float TabBorderSize = 1.0f;
        [Slider(0f, 64f)]
        public float TabMinWidthForCloseButton = 16f;
        public ImGuiDir ColorButtonPosition = ImGuiDir.Right;
        [Drag(0f, 1f)]
        public Vector2 ButtonTextAlign = new Vector2(0.5f, 0.5f);
        [Drag(0f, 1f)]
        public Vector2 SelectableTextAlign = new Vector2(0.0f, 0.0f);
        [Slider(0.001f, 2f)]
        public float CircleTessellationMaxError = 0.01f;
        [Toggle]
        public bool AntiAliasedLinesUseTex = false;
        [Toggle]
        public bool AntiAliasedLines = true;
        [Toggle]
        public bool AntiAliasedFill = true;
        // colors
        [Hidden]
        public Vector4[] Colors;

        /// <summary>
        /// Instantiate a new FuguiTheme instance. Default values are Dark theme
        /// </summary>
        /// <param name="name">name of the theme (must be unique, overise it will overwite the last theme with same name)</param>
        public FuguiTheme(string name)
        {
            ThemeName = name;
            // set default colors
            SetAsDefaultDarkTheme();
        }

        /// <summary>
        /// try to register this theme to ThemManager
        /// </summary>
        /// <returns>true if success, false if a theme with the same name already exists in theme manager</returns>
        public bool RegisterToThemeManager()
        {
            return ThemeManager.RegisterTheme(this);
        }

        /// <summary>
        /// try to unregister this theme from ThemManager
        /// </summary>
        /// <returns>true if success, false if a no with the same name exists in theme manager</returns>
        public bool UnregisterToThemeManager()
        {
            return ThemeManager.UnregisterTheme(this);
        }

        /// <summary>
        /// Set default Dark Theme colors to this theme
        /// </summary>
        public void SetAsDefaultDarkTheme()
        {
            Alpha = 1.0f;
            WindowPadding = new Vector2(1.0f, 0.0f);
            WindowRounding = 2.0f;
            WindowBorderSize = 1.0f;
            WindowMinSize = new Vector2(64.0f, 64.0f);
            WindowTitleAlign = new Vector2(0.0f, 0.5f);
            WindowMenuButtonPosition = ImGuiDir.Right;
            ChildRounding = 2.0f;
            ChildBorderSize = 1.0f;
            PopupRounding = 2.0f;
            PopupBorderSize = 1.0f;
            FramePadding = new Vector2(8f, 4f);
            FrameRounding = 2.0f;
            FrameBorderSize = 1.1f;
            ItemSpacing = new Vector2(4.0f, 6.0f);
            ItemInnerSpacing = new Vector2(4.0f, 6.0f);
            CellPadding = new Vector2(6.0f, 1.0f);
            IndentSpacing = 24.0f;
            ColumnsMinSpacing = 6.0f;
            ScrollbarSize = 14.0f;
            ScrollbarRounding = 2.0f;
            GrabMinSize = 10.0f;
            GrabRounding = 2.0f;
            TabRounding = 2.0f;
            TabBorderSize = 1.0f;
            TabMinWidthForCloseButton = 0.0f;
            ColorButtonPosition = ImGuiDir.Right;
            ButtonTextAlign = new Vector2(0.5f, 0.5f);
            SelectableTextAlign = new Vector2(0.0f, 0.0f);
            CircleTessellationMaxError = 0.01f;
            AntiAliasedLinesUseTex = false;
            AntiAliasedLines = true;
            AntiAliasedFill = true;

            Colors = new Vector4[(int)FuguiColors.COUNT];
            // imgui colors
            Colors[(int)FuguiColors.Text] = new Vector4(223f / 255f, 223f / 255f, 223f / 255f, 1.0f);
            Colors[(int)FuguiColors.TextDisabled] = new Vector4(0.3764705955982208f, 0.3764705955982208f, 0.3764705955982208f, 1.0f);
            Colors[(int)FuguiColors.WindowBg] = new Vector4(46f / 255f, 46f / 255f, 46f / 255f, 1.0f);
            Colors[(int)FuguiColors.ChildBg] = new Vector4(0.164705f, 0.164705f, 0.164705f, 1.0f);
            Colors[(int)FuguiColors.PopupBg] = new Vector4(0.1882352977991104f, 0.1882352977991104f, 0.1882352977991104f, 1.0f);
            Colors[(int)FuguiColors.Border] = new Vector4(24f / 255f, 24f / 255f, 24f / 255f, 1.0f);
            Colors[(int)FuguiColors.BorderShadow] = Vector4.zero;//  new Vector4(0.0f, 0.0f, 0.0f, 0.28f);
            Colors[(int)FuguiColors.FrameBg] = new Vector4(35f / 255f, 35f / 255f, 35f / 255f, 1.0f);
            Colors[(int)FuguiColors.FrameBgHovered] = new Vector4(0.09442061185836792f, 0.09441966563463211f, 0.09441966563463211f, 0.5400000214576721f);
            Colors[(int)FuguiColors.FrameBgActive] = new Vector4(0.1802556961774826f, 0.1802569776773453f, 0.1802574992179871f, 1.0f);
            Colors[(int)FuguiColors.TitleBg] = new Vector4(34f / 255f, 34f / 255f, 34f / 255f, 1.0f);
            Colors[(int)FuguiColors.TitleBgActive] = new Vector4(34f / 255f, 34f / 255f, 34f / 255f, 1.0f);
            Colors[(int)FuguiColors.TitleBgCollapsed] = new Vector4(34f / 255f, 34f / 255f, 34f / 255f, 1.0f);
            Colors[(int)FuguiColors.MenuBarBg] = new Vector4(0.09411764889955521f, 0.09411764889955521f, 0.09411764889955521f, 1.0f);
            Colors[(int)FuguiColors.ScrollbarBg] = new Vector4(0.0470588244497776f, 0.0470588244497776f, 0.0470588244497776f, 0.5411764979362488f);
            Colors[(int)FuguiColors.ScrollbarGrab] = new Vector4(0.2823529541492462f, 0.2823529541492462f, 0.2823529541492462f, 0.5411764979362488f);
            Colors[(int)FuguiColors.ScrollbarGrabHovered] = new Vector4(0.321568638086319f, 0.321568638086319f, 0.321568638086319f, 0.5411764979362488f);
            Colors[(int)FuguiColors.ScrollbarGrabActive] = new Vector4(0.4392156898975372f, 0.4392156898975372f, 0.4392156898975372f, 0.5411764979362488f);
            Colors[(int)FuguiColors.CheckMark] = new Vector4(41f / 255f, 126f / 255f, 204f / 255f, 1f);//new Vector4(0.3294117748737335f, 0.6666666865348816f, 0.8588235378265381f, 1.0f);
            Colors[(int)FuguiColors.SliderGrab] = new Vector4(0.3372549116611481f, 0.3372549116611481f, 0.3372549116611481f, 0.5400000214576721f);
            Colors[(int)FuguiColors.SliderGrabActive] = new Vector4(0.5568627715110779f, 0.5568627715110779f, 0.5568627715110779f, 0.5400000214576721f);
            Colors[(int)FuguiColors.Button] = new Vector4(58f / 255f, 58f / 255f, 58f / 255f, 1f);
            Colors[(int)FuguiColors.ButtonHovered] = new Vector4(64f / 255f, 64f / 255f, 64f / 255f, 1f);
            Colors[(int)FuguiColors.ButtonActive] = new Vector4(42f / 255f, 68f / 255f, 83f / 255f, 1f);
            Colors[(int)FuguiColors.Header] = new Vector4(0.1098039224743843f, 0.1098039224743843f, 0.1098039224743843f, 1.0f);
            Colors[(int)FuguiColors.HeaderHovered] = new Vector4(42f / 255f, 68f / 255f, 83f / 255f, 0.5f);
            Colors[(int)FuguiColors.HeaderActive] = new Vector4(0.2000000029802322f, 0.2196078449487686f, 0.2274509817361832f, 0.3300000131130219f);
            Colors[(int)FuguiColors.Separator] = new Vector4(0.1098039224743843f, 0.1098039224743843f, 0.1098039224743843f, 1.0f);
            Colors[(int)FuguiColors.SeparatorHovered] = new Vector4(0.0313725508749485f, 0.0313725508749485f, 0.0313725508749485f, 1.0f);
            Colors[(int)FuguiColors.SeparatorActive] = new Vector4(0.250980406999588f, 0.250980406999588f, 0.250980406999588f, 1.0f);
            Colors[(int)FuguiColors.ResizeGrip] = new Vector4(0.1098039224743843f, 0.1098039224743843f, 0.1098039224743843f, 1.0f);
            Colors[(int)FuguiColors.ResizeGripHovered] = new Vector4(0.0313725508749485f, 0.0313725508749485f, 0.0313725508749485f, 1.0f);
            Colors[(int)FuguiColors.ResizeGripActive] = new Vector4(0.250980406999588f, 0.250980406999588f, 0.250980406999588f, 1.0f);
            Colors[(int)FuguiColors.Tab] = new Vector4(34f / 255f, 34f / 255f, 34f / 255f, 1.0f);
            Colors[(int)FuguiColors.TabHovered] = new Vector4(0.250980406999588f, 0.250980406999588f, 0.250980406999588f, 1.0f);
            Colors[(int)FuguiColors.TabActive] = new Vector4(46f / 255f, 46f / 255f, 46f / 255f, 1.0f);
            Colors[(int)FuguiColors.TabUnfocused] = new Vector4(34f / 255f, 34f / 255f, 34f / 255f, 1.0f);
            Colors[(int)FuguiColors.TabUnfocusedActive] = new Vector4(46f / 255f, 46f / 255f, 46f / 255f, 1.0f);
            Colors[(int)FuguiColors.PlotLines] = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
            Colors[(int)FuguiColors.PlotLinesHovered] = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
            Colors[(int)FuguiColors.PlotHistogram] = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
            Colors[(int)FuguiColors.PlotHistogramHovered] = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
            Colors[(int)FuguiColors.TableHeaderBg] = new Vector4(0.0f, 0.0f, 0.0f, 0.5199999809265137f);
            Colors[(int)FuguiColors.TableBorderStrong] = new Vector4(0.0f, 0.0f, 0.0f, 0.5199999809265137f);
            Colors[(int)FuguiColors.TableBorderLight] = new Vector4(0.2784313857555389f, 0.2784313857555389f, 0.2784313857555389f, 0.2899999916553497f);
            Colors[(int)FuguiColors.TableRowBg] = new Vector4(1.0f, 1.0f, 1.0f, 1f / 255f);
            Colors[(int)FuguiColors.TableRowBgAlt] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
            Colors[(int)FuguiColors.TextSelectedBg] = new Vector4(0.2000000029802322f, 0.2196078449487686f, 0.2274509817361832f, 1.0f);
            Colors[(int)FuguiColors.DragDropTarget] = new Vector4(0.3294117748737335f, 0.6666666865348816f, 0.8588235378265381f, 1.0f);
            Colors[(int)FuguiColors.NavHighlight] = new Vector4(0.3294117748737335f, 0.6666666865348816f, 0.8588235378265381f, 1.0f);
            Colors[(int)FuguiColors.NavWindowingHighlight] = new Vector4(0.1764705926179886f, 0.407843142747879f, 0.5372549295425415f, 1.0f);
            Colors[(int)FuguiColors.NavWindowingDimBg] = new Vector4(0.09411764889955521f, 0.09411764889955521f, 0.09411764889955521f, 0.7843137383460999f);
            Colors[(int)FuguiColors.ModalWindowDimBg] = new Vector4(0.09411764889955521f, 0.09411764889955521f, 0.09411764889955521f, 0.7843137383460999f);
            Colors[(int)FuguiColors.DockingEmptyBg] = Colors[(int)FuguiColors.FrameBg];
            Colors[(int)FuguiColors.DockingPreview] = Color.black;

            // custom colors
            Colors[(int)FuguiColors.Highlight] = new Vector4(1f / 255f, 122f / 255f, 1f, 1f);
            Colors[(int)FuguiColors.HighlightHovered] = new Vector4(1f / 255f * 0.9f, 122f / 255f * 0.9f, 1f * 0.9f, 1f);
            Colors[(int)FuguiColors.HighlightActive] = new Vector4(1f / 255f * 0.8f, 122f / 255f * 0.8f, 1f * 0.8f, 1f);
            Colors[(int)FuguiColors.HighlightDisabled] = new Vector4(1f / 255f * 0.5f, 122f / 255f * 0.5f, 1f * 0.5f, 1f);

            Colors[(int)FuguiColors.FrameHoverFeedback] = new Vector4(0.8f, 0.8f, 0.8f, 0.2f);
            Colors[(int)FuguiColors.FrameSelectedFeedback] = new Vector4(42f / 255f, 68f / 255f, 83f / 255f, 1f);

            Colors[(int)FuguiColors.Collapsable] = new Vector4(0.205f, 0.205f, 0.205f, 1f);
            Colors[(int)FuguiColors.CollapsableHovered] = new Vector4(0.22f, 0.22f, 0.22f, 1f);
            Colors[(int)FuguiColors.CollapsableActive] = new Vector4(0.25f, 0.25f, 0.25f, 1f);
            Colors[(int)FuguiColors.CollapsableDisabled] = new Vector4(0.18f, 0.18f, 0.18f, 1f) * 0.5f;

            Colors[(int)FuguiColors.Selected] = new Vector4(18f / 255f, 98f / 255f, 181f / 255f, 1f); //new Vector4(35f / 255f, 74f / 255f, 108f / 255f, 1f);
            Colors[(int)FuguiColors.SelectedHovered] = Colors[(int)FuguiColors.Selected] * 0.9f;
            Colors[(int)FuguiColors.SelectedActive] = Colors[(int)FuguiColors.Selected] * 0.8f;
            Colors[(int)FuguiColors.SelectedText] = Vector4.one;
            Colors[(int)FuguiColors.Knob] = new Vector4(1f, 1f, 1f, 1f);
            Colors[(int)FuguiColors.KnobHovered] = new Vector4(0.9f, 0.9f, 0.9f, 1f);
            Colors[(int)FuguiColors.KnobActive] = new Vector4(0.8f, 0.8f, 0.8f, 1f);
            Colors[(int)FuguiColors.MainMenuText] = Colors[(int)FuguiColors.Text];
            Colors[(int)FuguiColors.HighlightText] = Colors[(int)FuguiColors.Text];
            Colors[(int)FuguiColors.HighlightTextDisabled] = Colors[(int)FuguiColors.TextDisabled];

            Colors[(int)FuguiColors.TextDanger] = new Vector4(223f / 255f, 70f / 255f, 85f / 255f, 1f);
            Colors[(int)FuguiColors.TextInfo] = new Vector4(81f / 255f, 212f / 255f, 233f / 255f, 1f);
            Colors[(int)FuguiColors.TextSuccess] = new Vector4(97f / 255f, 217f / 255f, 124f / 255f, 1f);
            Colors[(int)FuguiColors.TextWarning] = new Vector4(255f / 255f, 199f / 255f, 30f / 255f, 1f);

            Colors[(int)FuguiColors.BackgroundDanger] = new Vector4(223f / 255f, 70f / 255f, 85f / 255f, 1f);
            Colors[(int)FuguiColors.BackgroundInfo] = new Vector4(81f / 255f, 212f / 255f, 233f / 255f, 1f);
            Colors[(int)FuguiColors.BackgroundSuccess] = new Vector4(97f / 255f, 217f / 255f, 124f / 255f, 1f);
            Colors[(int)FuguiColors.BackgroundWarning] = new Vector4(255f / 255f, 199f / 255f, 30f / 255f, 1f);
        }

        /// <summary>
        /// Set default Light Theme colors to this theme
        /// </summary>
        public void SetAsDefaultLightTheme()
        {
            Alpha = 1.0f;
            WindowPadding = new Vector2(1.0f, 0.0f);
            WindowRounding = 2.0f;
            WindowBorderSize = 1.0f;
            WindowMinSize = new Vector2(64.0f, 64.0f);
            WindowTitleAlign = new Vector2(0.0f, 0.5f);
            WindowMenuButtonPosition = ImGuiDir.Right;
            ChildRounding = 2.0f;
            ChildBorderSize = 1.0f;
            PopupRounding = 2.0f;
            PopupBorderSize = 1.0f;
            FramePadding = new Vector2(8f, 4f);
            FrameRounding = 2.0f;
            FrameBorderSize = 1.1f;
            ItemSpacing = new Vector2(4.0f, 6.0f);
            ItemInnerSpacing = new Vector2(4.0f, 6.0f);
            CellPadding = new Vector2(6.0f, 1.0f);
            IndentSpacing = 24.0f;
            ColumnsMinSpacing = 6.0f;
            ScrollbarSize = 14.0f;
            ScrollbarRounding = 2.0f;
            GrabMinSize = 10.0f;
            GrabRounding = 2.0f;
            TabRounding = 2.0f;
            TabBorderSize = 1.0f;
            TabMinWidthForCloseButton = 0.0f;
            ColorButtonPosition = ImGuiDir.Right;
            ButtonTextAlign = new Vector2(0.5f, 0.5f);
            SelectableTextAlign = new Vector2(0.0f, 0.0f);
            CircleTessellationMaxError = 0.01f;
            AntiAliasedLinesUseTex = false;
            AntiAliasedLines = true;
            AntiAliasedFill = true;

            // imgui colors
            Colors = new Vector4[(int)FuguiColors.COUNT];

            Colors[(int)FuguiColors.Text] = new Vector4(0.15f, 0.15f, 0.15f, 1.00f);
            Colors[(int)FuguiColors.TextDisabled] = new Vector4(0.60f, 0.60f, 0.60f, 1.00f);
            Colors[(int)FuguiColors.WindowBg] = new Vector4(0.87f, 0.87f, 0.87f, 1.00f);
            Colors[(int)FuguiColors.ChildBg] = new Vector4(0.87f, 0.87f, 0.87f, 1.00f);
            Colors[(int)FuguiColors.PopupBg] = new Vector4(0.87f, 0.87f, 0.87f, 1.00f);
            Colors[(int)FuguiColors.Border] = new Vector4(0.89f, 0.89f, 0.89f, 1.00f);
            Colors[(int)FuguiColors.BorderShadow] = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
            Colors[(int)FuguiColors.FrameBg] = new Vector4(0.93f, 0.93f, 0.93f, 1.00f);
            Colors[(int)FuguiColors.FrameBgHovered] = new Vector4(1.00f, 0.69f, 0.07f, 0.69f);
            Colors[(int)FuguiColors.FrameBgActive] = new Vector4(1.00f, 0.82f, 0.46f, 0.69f);
            Colors[(int)FuguiColors.TitleBg] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            Colors[(int)FuguiColors.TitleBgActive] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            Colors[(int)FuguiColors.TitleBgCollapsed] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            Colors[(int)FuguiColors.MenuBarBg] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            Colors[(int)FuguiColors.ScrollbarBg] = new Vector4(0.87f, 0.87f, 0.87f, 1.00f);
            Colors[(int)FuguiColors.ScrollbarGrab] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            Colors[(int)FuguiColors.ScrollbarGrabHovered] = new Vector4(1.00f, 0.69f, 0.07f, 0.69f);
            Colors[(int)FuguiColors.ScrollbarGrabActive] = new Vector4(1.00f, 0.82f, 0.46f, 0.69f);
            Colors[(int)FuguiColors.CheckMark] = new Vector4(41f / 255f, 126f / 255f, 204f / 255f, 1f);//new Vector4(0.3294117748737335f, 0.6666666865348816f, 0.8588235378265381f, 1.0f);
            Colors[(int)FuguiColors.SliderGrab] = new Vector4(1.00f, 0.69f, 0.07f, 0.69f);
            Colors[(int)FuguiColors.SliderGrabActive] = new Vector4(1.00f, 0.82f, 0.46f, 0.69f);
            Colors[(int)FuguiColors.Button] = new Vector4(0.83f, 0.83f, 0.83f, 1.00f);
            Colors[(int)FuguiColors.ButtonHovered] = new Vector4(1.00f, 0.69f, 0.07f, 0.69f);
            Colors[(int)FuguiColors.ButtonActive] = new Vector4(1.00f, 0.82f, 0.46f, 0.69f);
            Colors[(int)FuguiColors.Header] = new Vector4(0.67f, 0.67f, 0.67f, 1.00f);
            Colors[(int)FuguiColors.HeaderHovered] = new Vector4(1.00f, 0.69f, 0.07f, 1.00f);
            Colors[(int)FuguiColors.HeaderActive] = new Vector4(1.00f, 0.82f, 0.46f, 0.69f);
            Colors[(int)FuguiColors.Separator] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            Colors[(int)FuguiColors.SeparatorHovered] = new Vector4(1.00f, 0.69f, 0.07f, 1.00f);
            Colors[(int)FuguiColors.SeparatorActive] = new Vector4(1.00f, 0.82f, 0.46f, 0.69f);
            Colors[(int)FuguiColors.ResizeGrip] = new Vector4(1.00f, 1.00f, 1.00f, 0.18f);
            Colors[(int)FuguiColors.ResizeGripHovered] = new Vector4(1.00f, 0.69f, 0.07f, 1.00f);
            Colors[(int)FuguiColors.ResizeGripActive] = new Vector4(1.00f, 0.82f, 0.46f, 0.69f);
            Colors[(int)FuguiColors.Tab] = new Vector4(0.16f, 0.16f, 0.16f, 0.00f);
            Colors[(int)FuguiColors.TabHovered] = new Vector4(1.00f, 0.69f, 0.07f, 1.00f);
            Colors[(int)FuguiColors.TabActive] = new Vector4(1.00f, 0.69f, 0.07f, 1.00f);
            Colors[(int)FuguiColors.TabUnfocused] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            Colors[(int)FuguiColors.TabUnfocusedActive] = new Vector4(0.87f, 0.87f, 0.87f, 1.00f);
            Colors[(int)FuguiColors.DockingPreview] = new Vector4(1.00f, 0.82f, 0.46f, 0.69f);
            Colors[(int)FuguiColors.DockingEmptyBg] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            Colors[(int)FuguiColors.PlotLines] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            Colors[(int)FuguiColors.PlotLinesHovered] = new Vector4(0.90f, 0.70f, 0.00f, 1.00f);
            Colors[(int)FuguiColors.PlotHistogram] = new Vector4(0.90f, 0.70f, 0.00f, 1.00f);
            Colors[(int)FuguiColors.PlotHistogramHovered] = new Vector4(1.00f, 0.60f, 0.00f, 1.00f);
            Colors[(int)FuguiColors.TextSelectedBg] = new Vector4(1.00f, 0.69f, 0.07f, 1.00f);
            Colors[(int)FuguiColors.DragDropTarget] = new Vector4(1.00f, 0.69f, 0.07f, 1.00f);
            Colors[(int)FuguiColors.NavHighlight] = new Vector4(1.00f, 0.69f, 0.07f, 1.00f);
            Colors[(int)FuguiColors.NavWindowingHighlight] = new Vector4(1.00f, 1.00f, 1.00f, 0.70f);
            Colors[(int)FuguiColors.NavWindowingDimBg] = new Vector4(0.87f, 0.87f, 0.87f, 1.00f);
            Colors[(int)FuguiColors.ModalWindowDimBg] = new Vector4(0.20f, 0.20f, 0.20f, 0.35f);
            Colors[(int)FuguiColors.DockingEmptyBg] = Colors[(int)FuguiColors.FrameBg];
            Colors[(int)FuguiColors.DockingPreview] = Color.white;

            // custom colors
            Colors[(int)FuguiColors.Highlight] = new Vector4(1f / 255f, 122f / 255f, 1f, 1f);
            Colors[(int)FuguiColors.HighlightHovered] = new Vector4(1f / 255f * 0.9f, 122f / 255f * 0.9f, 1f * 0.9f, 1f);
            Colors[(int)FuguiColors.HighlightActive] = new Vector4(1f / 255f * 0.8f, 122f / 255f * 0.8f, 1f * 0.8f, 1f);
            Colors[(int)FuguiColors.HighlightDisabled] = new Vector4(1f / 255f * 0.5f, 122f / 255f * 0.5f, 1f * 0.5f, 1f);

            Colors[(int)FuguiColors.FrameHoverFeedback] = new Vector4(0.8f, 0.8f, 0.8f, 0.2f);
            Colors[(int)FuguiColors.FrameSelectedFeedback] = new Vector4(42f / 255f, 68f / 255f, 83f / 255f, 1f);

            Colors[(int)FuguiColors.Collapsable] = new Vector4(0.205f, 0.205f, 0.205f, 1f);
            Colors[(int)FuguiColors.CollapsableHovered] = new Vector4(0.22f, 0.22f, 0.22f, 1f);
            Colors[(int)FuguiColors.CollapsableActive] = new Vector4(0.25f, 0.25f, 0.25f, 1f);
            Colors[(int)FuguiColors.CollapsableDisabled] = new Vector4(0.18f, 0.18f, 0.18f, 1f) * 0.5f;

            Colors[(int)FuguiColors.Selected] = new Vector4(32f / 255f, 67f / 255f, 99f / 255f, 1f);
            Colors[(int)FuguiColors.SelectedHovered] = Colors[(int)FuguiColors.Selected] * 0.9f;
            Colors[(int)FuguiColors.SelectedActive] = Colors[(int)FuguiColors.Selected] * 0.8f;
            Colors[(int)FuguiColors.SelectedText] = Vector4.one;

            Colors[(int)FuguiColors.Knob] = new Vector4(0f, 0f, 0f, 1f);
            Colors[(int)FuguiColors.KnobHovered] = new Vector4(0.1f, 0.1f, 0.1f, 1f);
            Colors[(int)FuguiColors.KnobActive] = new Vector4(0.2f, 0.2f, 0.2f, 1f);
            Colors[(int)FuguiColors.MainMenuText] = Colors[(int)FuguiColors.Text];
            Colors[(int)FuguiColors.HighlightText] = Colors[(int)FuguiColors.Text];
            Colors[(int)FuguiColors.HighlightTextDisabled] = Colors[(int)FuguiColors.TextDisabled];

            Colors[(int)FuguiColors.TextDanger] = new Vector4(223f / 255f, 70f / 255f, 85f / 255f, 1f);
            Colors[(int)FuguiColors.TextInfo] = new Vector4(81f / 255f, 212f / 255f, 233f / 255f, 1f);
            Colors[(int)FuguiColors.TextSuccess] = new Vector4(97f / 255f, 217f / 255f, 124f / 255f, 1f);
            Colors[(int)FuguiColors.TextWarning] = new Vector4(255f / 255f, 199f / 255f, 30f / 255f, 1f);

            Colors[(int)FuguiColors.BackgroundDanger] = new Vector4(220f / 255f, 53f / 255f, 69f / 255f, 1f);
            Colors[(int)FuguiColors.BackgroundInfo] = new Vector4(23f / 255f, 162f / 255f, 184f / 255f, 1f);
            Colors[(int)FuguiColors.BackgroundSuccess] = new Vector4(40f / 255f, 167f / 255f, 69f / 255f, 1f);
            Colors[(int)FuguiColors.BackgroundWarning] = new Vector4(255f / 255f, 193f / 255f, 7f / 255f, 1f);
        }

        /// <summary>
        /// Apply this theme to the current ImGui context
        /// </summary>
        internal void Apply()
        {
            var style = ImGui.GetStyle();
            // set style var
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
            // set style colors
            for (int i = 0; i < (int)ImGuiCol.COUNT; i++)
            {
                style.Colors[i] = Colors[i];
            }

            // set missing colors
            FuguiTheme defaultTheme = new FuguiTheme("defaultTmp");
            defaultTheme.SetAsDefaultDarkTheme();
            for (int i = (int)ImGuiCol.COUNT; i < (int)FuguiColors.COUNT; i++)
            {
                if (Colors[i] == Vector4.zero)
                {
                    Colors[i] = defaultTheme.Colors[i];
                }
            }
        }

        public override string ToString()
        {
            return ThemeName;
        }
    }
}