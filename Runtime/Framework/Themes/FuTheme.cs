using ImGuiNET;
using System;
using UnityEngine;

namespace Fu.Framework
{
    public class FuTheme
    {
        [FuDisabled]
        public string ThemeName = "Fugui Theme";
        [FuDisabled]
        [FuSlider(0.1f, 1f)]
        public float Alpha = 1.0f;
        [FuDrag(0f, 10f)]
        public Vector2 WindowPadding = new Vector2(1.0f, 0.0f);
        [FuSlider(0f, 8f)]
        public float WindowRounding = 2.0f;
        [FuSlider(0f, 4f)]
        public float WindowBorderSize = 1.0f;
        [FuDrag(1f, 100f, "width", "height")]
        public Vector2 WindowMinSize = new Vector2(16.0f, 16.0f);
        [FuDrag(0f, 1f)]
        public Vector2 WindowTitleAlign = new Vector2(0.0f, 0.5f);
        public ImGuiDir WindowMenuButtonPosition = ImGuiDir.Right;
        [FuSlider(0f, 10f)]
        public float ChildRounding = 2.0f;
        [FuSlider(0f, 10f)]
        public float ChildBorderSize = 1.0f;
        [FuSlider(0f, 10f)]
        public float PopupRounding = 2.0f;
        [FuSlider(0f, 10f)]
        public float PopupBorderSize = 1.0f;
        [FuSlider(0f, 1f)]
        public float ButtonsGradientStrenght = 0.0f;
        [FuSlider(0f, 1f)]
        public float CollapsableGradientStrenght = 0.0f;
        [FuDrag(0f, 10f)]
        public Vector2 FramePadding = new Vector2(8f, 4f);
        [FuSlider(0f, 10f)]
        public float FrameRounding = 2.0f;
        [FuSlider(0f, 10f)]
        public float FrameBorderSize = 1.1f;
        [FuDrag(0f, 20f)]
        public Vector2 ItemSpacing = new Vector2(4.0f, 6.0f);
        [FuDrag(0f, 20f)]
        public Vector2 ItemInnerSpacing = new Vector2(4.0f, 6.0f);
        [FuDrag(0f, 20f)]
        public Vector2 CellPadding = new Vector2(6.0f, 1.0f);
        [FuSlider(0f, 50f)]
        public float IndentSpacing = 24.0f;
        [FuSlider(0f, 50f)]
        public float ColumnsMinSpacing = 6.0f;
        [FuSlider(0f, 50f)]
        public float ScrollbarSize = 14.0f;
        [FuSlider(0f, 10f)]
        public float ScrollbarRounding = 2.0f;
        [FuSlider(0f, 50f)]
        public float GrabMinSize = 10.0f;
        [FuSlider(0f, 10f)]
        public float GrabRounding = 2.0f;
        [FuSlider(0f, 10f)]
        public float TabRounding = 2.0f;
        [FuSlider(0f, 10f)]
        public float TabBorderSize = 1.0f;
        [FuSlider(0f, 64f)]
        public float TabMinWidthForCloseButton = 16f;
        public ImGuiDir ColorButtonPosition = ImGuiDir.Right;
        [FuDrag(0f, 1f)]
        public Vector2 ButtonTextAlign = new Vector2(0.5f, 0.5f);
        [FuDrag(0f, 1f)]
        public Vector2 SelectableTextAlign = new Vector2(0.0f, 0.0f);
        [FuSlider(0.001f, 2f)]
        public float CircleTessellationMaxError = 0.01f;
        [FuToggle]
        public bool AntiAliasedLinesUseTex = false;
        [FuToggle]
        public bool AntiAliasedLines = true;
        [FuToggle]
        public bool AntiAliasedFill = true;
        // colors
        [FuHidden]
        public Vector4[] Colors;

        public static Enum ThemeExtension { get; private set; } = null;
        public static int ThemeExtensionCount { get; private set; } = 0;

        /// <summary>
        /// Instantiate a new FuguiTheme instance. Default values are Dark theme
        /// </summary>
        /// <param name="name">name of the theme (must be unique, overise it will overwite the last theme with same name)</param>
        public FuTheme(string name)
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
            return FuThemeManager.RegisterTheme(this);
        }

        /// <summary>
        /// try to unregister this theme from ThemManager
        /// </summary>
        /// <returns>true if success, false if a no with the same name exists in theme manager</returns>
        public bool UnregisterToThemeManager()
        {
            return FuThemeManager.UnregisterTheme(this);
        }

        /// <summary>
        /// Extend theme with more colors
        /// </summary>
        /// <param name="themesExtension">Enum of colors to add to all themes</param>
        internal static void ExtendThemes(Enum themesExtension)
        {
            ThemeExtension = themesExtension;
            ThemeExtensionCount = Enum.GetNames(ThemeExtension.GetType()).Length;
        }

        /// <summary>
        /// Updates theme accordingly with the themes extension
        /// </summary>
        internal void UpdateThemeWithExtension()
        {
            int colorsCount = (int)FuColors.COUNT + ThemeExtensionCount;
            if (Colors.Length < colorsCount)
            {
                Vector4[] colors = new Vector4[colorsCount];
                Colors.CopyTo(colors, 0);
                for (int i = Colors.Length; i < colorsCount; i++)
                {
                    colors[i] = Vector4.one;
                }
                Colors = colors;
            }
        }

        /// <summary>
        /// Removes themes extension
        /// </summary>
        internal static void ReduceThemes()
        {
            ThemeExtension = null;
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

            int colorsCount = ThemeExtension != null ? (int)FuColors.COUNT + ThemeExtensionCount : (int)FuColors.COUNT;
            Colors = new Vector4[colorsCount];
            // imgui colors
            Colors[(int)FuColors.Text] = new Vector4(223f / 255f, 223f / 255f, 223f / 255f, 1.0f);
            Colors[(int)FuColors.TextDisabled] = new Vector4(0.3764705955982208f, 0.3764705955982208f, 0.3764705955982208f, 1.0f);
            Colors[(int)FuColors.WindowBg] = new Vector4(46f / 255f, 46f / 255f, 46f / 255f, 1.0f);
            Colors[(int)FuColors.ChildBg] = new Vector4(0.164705f, 0.164705f, 0.164705f, 1.0f);
            Colors[(int)FuColors.PopupBg] = new Vector4(0.1882352977991104f, 0.1882352977991104f, 0.1882352977991104f, 1.0f);
            Colors[(int)FuColors.Border] = new Vector4(24f / 255f, 24f / 255f, 24f / 255f, 1.0f);
            Colors[(int)FuColors.BorderShadow] = Vector4.zero;//  new Vector4(0.0f, 0.0f, 0.0f, 0.28f);
            Colors[(int)FuColors.FrameBg] = new Vector4(35f / 255f, 35f / 255f, 35f / 255f, 1.0f);
            Colors[(int)FuColors.FrameBgHovered] = new Vector4(0.09442061185836792f, 0.09441966563463211f, 0.09441966563463211f, 0.5400000214576721f);
            Colors[(int)FuColors.FrameBgActive] = new Vector4(0.1802556961774826f, 0.1802569776773453f, 0.1802574992179871f, 1.0f);
            Colors[(int)FuColors.TitleBg] = new Vector4(34f / 255f, 34f / 255f, 34f / 255f, 1.0f);
            Colors[(int)FuColors.TitleBgActive] = new Vector4(34f / 255f, 34f / 255f, 34f / 255f, 1.0f);
            Colors[(int)FuColors.TitleBgCollapsed] = new Vector4(34f / 255f, 34f / 255f, 34f / 255f, 1.0f);
            Colors[(int)FuColors.MenuBarBg] = new Vector4(0.09411764889955521f, 0.09411764889955521f, 0.09411764889955521f, 1.0f);
            Colors[(int)FuColors.ScrollbarBg] = new Vector4(0.0470588244497776f, 0.0470588244497776f, 0.0470588244497776f, 0.5411764979362488f);
            Colors[(int)FuColors.ScrollbarGrab] = new Vector4(0.2823529541492462f, 0.2823529541492462f, 0.2823529541492462f, 0.5411764979362488f);
            Colors[(int)FuColors.ScrollbarGrabHovered] = new Vector4(0.321568638086319f, 0.321568638086319f, 0.321568638086319f, 0.5411764979362488f);
            Colors[(int)FuColors.ScrollbarGrabActive] = new Vector4(0.4392156898975372f, 0.4392156898975372f, 0.4392156898975372f, 0.5411764979362488f);
            Colors[(int)FuColors.CheckMark] = new Vector4(41f / 255f, 126f / 255f, 204f / 255f, 1f);//new Vector4(0.3294117748737335f, 0.6666666865348816f, 0.8588235378265381f, 1.0f);
            Colors[(int)FuColors.SliderGrab] = new Vector4(0.3372549116611481f, 0.3372549116611481f, 0.3372549116611481f, 0.5400000214576721f);
            Colors[(int)FuColors.SliderGrabActive] = new Vector4(0.5568627715110779f, 0.5568627715110779f, 0.5568627715110779f, 0.5400000214576721f);
            Colors[(int)FuColors.Button] = new Vector4(58f / 255f, 58f / 255f, 58f / 255f, 1f);
            Colors[(int)FuColors.ButtonHovered] = new Vector4(64f / 255f, 64f / 255f, 64f / 255f, 1f);
            Colors[(int)FuColors.ButtonActive] = new Vector4(42f / 255f, 68f / 255f, 83f / 255f, 1f);
            Colors[(int)FuColors.Header] = new Vector4(0.1098039224743843f, 0.1098039224743843f, 0.1098039224743843f, 1.0f);
            Colors[(int)FuColors.HeaderHovered] = new Vector4(42f / 255f, 68f / 255f, 83f / 255f, 0.5f);
            Colors[(int)FuColors.HeaderActive] = new Vector4(0.2000000029802322f, 0.2196078449487686f, 0.2274509817361832f, 0.3300000131130219f);
            Colors[(int)FuColors.Separator] = new Vector4(0.1098039224743843f, 0.1098039224743843f, 0.1098039224743843f, 1.0f);
            Colors[(int)FuColors.SeparatorHovered] = new Vector4(0.0313725508749485f, 0.0313725508749485f, 0.0313725508749485f, 1.0f);
            Colors[(int)FuColors.SeparatorActive] = new Vector4(0.250980406999588f, 0.250980406999588f, 0.250980406999588f, 1.0f);
            Colors[(int)FuColors.ResizeGrip] = new Vector4(0.1098039224743843f, 0.1098039224743843f, 0.1098039224743843f, 1.0f);
            Colors[(int)FuColors.ResizeGripHovered] = new Vector4(0.0313725508749485f, 0.0313725508749485f, 0.0313725508749485f, 1.0f);
            Colors[(int)FuColors.ResizeGripActive] = new Vector4(0.250980406999588f, 0.250980406999588f, 0.250980406999588f, 1.0f);
            Colors[(int)FuColors.Tab] = new Vector4(34f / 255f, 34f / 255f, 34f / 255f, 1.0f);
            Colors[(int)FuColors.TabHovered] = new Vector4(0.250980406999588f, 0.250980406999588f, 0.250980406999588f, 1.0f);
            Colors[(int)FuColors.TabActive] = new Vector4(46f / 255f, 46f / 255f, 46f / 255f, 1.0f);
            Colors[(int)FuColors.TabUnfocused] = new Vector4(34f / 255f, 34f / 255f, 34f / 255f, 1.0f);
            Colors[(int)FuColors.TabUnfocusedActive] = new Vector4(46f / 255f, 46f / 255f, 46f / 255f, 1.0f);
            Colors[(int)FuColors.PlotLines] = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
            Colors[(int)FuColors.PlotLinesHovered] = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
            Colors[(int)FuColors.PlotHistogram] = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
            Colors[(int)FuColors.PlotHistogramHovered] = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
            Colors[(int)FuColors.TableHeaderBg] = new Vector4(0.0f, 0.0f, 0.0f, 0.5199999809265137f);
            Colors[(int)FuColors.TableBorderStrong] = new Vector4(0.0f, 0.0f, 0.0f, 0.5199999809265137f);
            Colors[(int)FuColors.TableBorderLight] = new Vector4(0.2784313857555389f, 0.2784313857555389f, 0.2784313857555389f, 0.2899999916553497f);
            Colors[(int)FuColors.TableRowBg] = new Vector4(1.0f, 1.0f, 1.0f, 1f / 255f);
            Colors[(int)FuColors.TableRowBgAlt] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
            Colors[(int)FuColors.TextSelectedBg] = new Vector4(0.2000000029802322f, 0.2196078449487686f, 0.2274509817361832f, 1.0f);
            Colors[(int)FuColors.DragDropTarget] = new Vector4(0.3294117748737335f, 0.6666666865348816f, 0.8588235378265381f, 1.0f);
            Colors[(int)FuColors.NavHighlight] = new Vector4(0.3294117748737335f, 0.6666666865348816f, 0.8588235378265381f, 1.0f);
            Colors[(int)FuColors.NavWindowingHighlight] = new Vector4(0.1764705926179886f, 0.407843142747879f, 0.5372549295425415f, 1.0f);
            Colors[(int)FuColors.NavWindowingDimBg] = new Vector4(0.09411764889955521f, 0.09411764889955521f, 0.09411764889955521f, 0.7843137383460999f);
            Colors[(int)FuColors.ModalWindowDimBg] = new Vector4(0.09411764889955521f, 0.09411764889955521f, 0.09411764889955521f, 0.7843137383460999f);
            Colors[(int)FuColors.DockingEmptyBg] = Colors[(int)FuColors.FrameBg];
            Colors[(int)FuColors.DockingPreview] = Color.black;

            // custom colors
            Colors[(int)FuColors.Highlight] = new Vector4(1f / 255f, 122f / 255f, 1f, 1f);
            Colors[(int)FuColors.HighlightHovered] = new Vector4(1f / 255f * 0.9f, 122f / 255f * 0.9f, 1f * 0.9f, 1f);
            Colors[(int)FuColors.HighlightActive] = new Vector4(1f / 255f * 0.8f, 122f / 255f * 0.8f, 1f * 0.8f, 1f);
            Colors[(int)FuColors.HighlightDisabled] = new Vector4(1f / 255f * 0.5f, 122f / 255f * 0.5f, 1f * 0.5f, 1f);

            Colors[(int)FuColors.FrameHoverFeedback] = new Vector4(0.8f, 0.8f, 0.8f, 0.2f);
            Colors[(int)FuColors.FrameSelectedFeedback] = new Vector4(42f / 255f, 68f / 255f, 83f / 255f, 1f);

            Colors[(int)FuColors.Collapsable] = new Vector4(0.205f, 0.205f, 0.205f, 1f);
            Colors[(int)FuColors.CollapsableHovered] = new Vector4(0.22f, 0.22f, 0.22f, 1f);
            Colors[(int)FuColors.CollapsableActive] = new Vector4(0.25f, 0.25f, 0.25f, 1f);
            Colors[(int)FuColors.CollapsableDisabled] = new Vector4(0.18f, 0.18f, 0.18f, 1f) * 0.5f;

            Colors[(int)FuColors.Selected] = new Vector4(18f / 255f, 98f / 255f, 181f / 255f, 1f); //new Vector4(35f / 255f, 74f / 255f, 108f / 255f, 1f);
            Colors[(int)FuColors.SelectedHovered] = Colors[(int)FuColors.Selected] * 0.9f;
            Colors[(int)FuColors.SelectedActive] = Colors[(int)FuColors.Selected] * 0.8f;
            Colors[(int)FuColors.SelectedText] = Vector4.one;
            Colors[(int)FuColors.Knob] = new Vector4(1f, 1f, 1f, 1f);
            Colors[(int)FuColors.KnobHovered] = new Vector4(0.9f, 0.9f, 0.9f, 1f);
            Colors[(int)FuColors.KnobActive] = new Vector4(0.8f, 0.8f, 0.8f, 1f);
            Colors[(int)FuColors.MainMenuText] = Colors[(int)FuColors.Text];
            Colors[(int)FuColors.HighlightText] = Colors[(int)FuColors.Text];
            Colors[(int)FuColors.HighlightTextDisabled] = Colors[(int)FuColors.TextDisabled];

            Colors[(int)FuColors.TextDanger] = new Vector4(223f / 255f, 70f / 255f, 85f / 255f, 1f);
            Colors[(int)FuColors.TextInfo] = new Vector4(81f / 255f, 212f / 255f, 233f / 255f, 1f);
            Colors[(int)FuColors.TextSuccess] = new Vector4(97f / 255f, 217f / 255f, 124f / 255f, 1f);
            Colors[(int)FuColors.TextWarning] = new Vector4(255f / 255f, 199f / 255f, 30f / 255f, 1f);

            Colors[(int)FuColors.BackgroundDanger] = new Vector4(223f / 255f, 70f / 255f, 85f / 255f, 1f);
            Colors[(int)FuColors.BackgroundInfo] = new Vector4(81f / 255f, 212f / 255f, 233f / 255f, 1f);
            Colors[(int)FuColors.BackgroundSuccess] = new Vector4(97f / 255f, 217f / 255f, 124f / 255f, 1f);
            Colors[(int)FuColors.BackgroundWarning] = new Vector4(255f / 255f, 199f / 255f, 30f / 255f, 1f);

			for (int i = (int)FuColors.COUNT; i < colorsCount; i++)
			{
                Colors[i] = Vector4.one;
			}
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

            int colorsCount = ThemeExtension != null ? (int)FuColors.COUNT + ThemeExtensionCount : (int)FuColors.COUNT;
            Colors = new Vector4[colorsCount];
            // imgui colors
            Colors[(int)FuColors.Text] = new Vector4(0.15f, 0.15f, 0.15f, 1.00f);
            Colors[(int)FuColors.TextDisabled] = new Vector4(0.60f, 0.60f, 0.60f, 1.00f);
            Colors[(int)FuColors.WindowBg] = new Vector4(0.87f, 0.87f, 0.87f, 1.00f);
            Colors[(int)FuColors.ChildBg] = new Vector4(0.87f, 0.87f, 0.87f, 1.00f);
            Colors[(int)FuColors.PopupBg] = new Vector4(0.87f, 0.87f, 0.87f, 1.00f);
            Colors[(int)FuColors.Border] = new Vector4(0.89f, 0.89f, 0.89f, 1.00f);
            Colors[(int)FuColors.BorderShadow] = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
            Colors[(int)FuColors.FrameBg] = new Vector4(0.93f, 0.93f, 0.93f, 1.00f);
            Colors[(int)FuColors.FrameBgHovered] = new Vector4(1.00f, 0.69f, 0.07f, 0.69f);
            Colors[(int)FuColors.FrameBgActive] = new Vector4(1.00f, 0.82f, 0.46f, 0.69f);
            Colors[(int)FuColors.TitleBg] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            Colors[(int)FuColors.TitleBgActive] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            Colors[(int)FuColors.TitleBgCollapsed] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            Colors[(int)FuColors.MenuBarBg] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            Colors[(int)FuColors.ScrollbarBg] = new Vector4(0.87f, 0.87f, 0.87f, 1.00f);
            Colors[(int)FuColors.ScrollbarGrab] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            Colors[(int)FuColors.ScrollbarGrabHovered] = new Vector4(1.00f, 0.69f, 0.07f, 0.69f);
            Colors[(int)FuColors.ScrollbarGrabActive] = new Vector4(1.00f, 0.82f, 0.46f, 0.69f);
            Colors[(int)FuColors.CheckMark] = new Vector4(41f / 255f, 126f / 255f, 204f / 255f, 1f);//new Vector4(0.3294117748737335f, 0.6666666865348816f, 0.8588235378265381f, 1.0f);
            Colors[(int)FuColors.SliderGrab] = new Vector4(1.00f, 0.69f, 0.07f, 0.69f);
            Colors[(int)FuColors.SliderGrabActive] = new Vector4(1.00f, 0.82f, 0.46f, 0.69f);
            Colors[(int)FuColors.Button] = new Vector4(0.83f, 0.83f, 0.83f, 1.00f);
            Colors[(int)FuColors.ButtonHovered] = new Vector4(1.00f, 0.69f, 0.07f, 0.69f);
            Colors[(int)FuColors.ButtonActive] = new Vector4(1.00f, 0.82f, 0.46f, 0.69f);
            Colors[(int)FuColors.Header] = new Vector4(0.67f, 0.67f, 0.67f, 1.00f);
            Colors[(int)FuColors.HeaderHovered] = new Vector4(1.00f, 0.69f, 0.07f, 1.00f);
            Colors[(int)FuColors.HeaderActive] = new Vector4(1.00f, 0.82f, 0.46f, 0.69f);
            Colors[(int)FuColors.Separator] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            Colors[(int)FuColors.SeparatorHovered] = new Vector4(1.00f, 0.69f, 0.07f, 1.00f);
            Colors[(int)FuColors.SeparatorActive] = new Vector4(1.00f, 0.82f, 0.46f, 0.69f);
            Colors[(int)FuColors.ResizeGrip] = new Vector4(1.00f, 1.00f, 1.00f, 0.18f);
            Colors[(int)FuColors.ResizeGripHovered] = new Vector4(1.00f, 0.69f, 0.07f, 1.00f);
            Colors[(int)FuColors.ResizeGripActive] = new Vector4(1.00f, 0.82f, 0.46f, 0.69f);
            Colors[(int)FuColors.Tab] = new Vector4(0.16f, 0.16f, 0.16f, 0.00f);
            Colors[(int)FuColors.TabHovered] = new Vector4(1.00f, 0.69f, 0.07f, 1.00f);
            Colors[(int)FuColors.TabActive] = new Vector4(1.00f, 0.69f, 0.07f, 1.00f);
            Colors[(int)FuColors.TabUnfocused] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            Colors[(int)FuColors.TabUnfocusedActive] = new Vector4(0.87f, 0.87f, 0.87f, 1.00f);
            Colors[(int)FuColors.DockingPreview] = new Vector4(1.00f, 0.82f, 0.46f, 0.69f);
            Colors[(int)FuColors.DockingEmptyBg] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            Colors[(int)FuColors.PlotLines] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            Colors[(int)FuColors.PlotLinesHovered] = new Vector4(0.90f, 0.70f, 0.00f, 1.00f);
            Colors[(int)FuColors.PlotHistogram] = new Vector4(0.90f, 0.70f, 0.00f, 1.00f);
            Colors[(int)FuColors.PlotHistogramHovered] = new Vector4(1.00f, 0.60f, 0.00f, 1.00f);
            Colors[(int)FuColors.TextSelectedBg] = new Vector4(1.00f, 0.69f, 0.07f, 1.00f);
            Colors[(int)FuColors.DragDropTarget] = new Vector4(1.00f, 0.69f, 0.07f, 1.00f);
            Colors[(int)FuColors.NavHighlight] = new Vector4(1.00f, 0.69f, 0.07f, 1.00f);
            Colors[(int)FuColors.NavWindowingHighlight] = new Vector4(1.00f, 1.00f, 1.00f, 0.70f);
            Colors[(int)FuColors.NavWindowingDimBg] = new Vector4(0.87f, 0.87f, 0.87f, 1.00f);
            Colors[(int)FuColors.ModalWindowDimBg] = new Vector4(0.20f, 0.20f, 0.20f, 0.35f);
            Colors[(int)FuColors.DockingEmptyBg] = Colors[(int)FuColors.FrameBg];
            Colors[(int)FuColors.DockingPreview] = Color.white;

            // custom colors
            Colors[(int)FuColors.Highlight] = new Vector4(1f / 255f, 122f / 255f, 1f, 1f);
            Colors[(int)FuColors.HighlightHovered] = new Vector4(1f / 255f * 0.9f, 122f / 255f * 0.9f, 1f * 0.9f, 1f);
            Colors[(int)FuColors.HighlightActive] = new Vector4(1f / 255f * 0.8f, 122f / 255f * 0.8f, 1f * 0.8f, 1f);
            Colors[(int)FuColors.HighlightDisabled] = new Vector4(1f / 255f * 0.5f, 122f / 255f * 0.5f, 1f * 0.5f, 1f);

            Colors[(int)FuColors.FrameHoverFeedback] = new Vector4(0.8f, 0.8f, 0.8f, 0.2f);
            Colors[(int)FuColors.FrameSelectedFeedback] = new Vector4(42f / 255f, 68f / 255f, 83f / 255f, 1f);

            Colors[(int)FuColors.Collapsable] = new Vector4(0.205f, 0.205f, 0.205f, 1f);
            Colors[(int)FuColors.CollapsableHovered] = new Vector4(0.22f, 0.22f, 0.22f, 1f);
            Colors[(int)FuColors.CollapsableActive] = new Vector4(0.25f, 0.25f, 0.25f, 1f);
            Colors[(int)FuColors.CollapsableDisabled] = new Vector4(0.18f, 0.18f, 0.18f, 1f) * 0.5f;

            Colors[(int)FuColors.Selected] = new Vector4(32f / 255f, 67f / 255f, 99f / 255f, 1f);
            Colors[(int)FuColors.SelectedHovered] = Colors[(int)FuColors.Selected] * 0.9f;
            Colors[(int)FuColors.SelectedActive] = Colors[(int)FuColors.Selected] * 0.8f;
            Colors[(int)FuColors.SelectedText] = Vector4.one;

            Colors[(int)FuColors.Knob] = new Vector4(0f, 0f, 0f, 1f);
            Colors[(int)FuColors.KnobHovered] = new Vector4(0.1f, 0.1f, 0.1f, 1f);
            Colors[(int)FuColors.KnobActive] = new Vector4(0.2f, 0.2f, 0.2f, 1f);
            Colors[(int)FuColors.MainMenuText] = Colors[(int)FuColors.Text];
            Colors[(int)FuColors.HighlightText] = Colors[(int)FuColors.Text];
            Colors[(int)FuColors.HighlightTextDisabled] = Colors[(int)FuColors.TextDisabled];

            Colors[(int)FuColors.TextDanger] = new Vector4(223f / 255f, 70f / 255f, 85f / 255f, 1f);
            Colors[(int)FuColors.TextInfo] = new Vector4(81f / 255f, 212f / 255f, 233f / 255f, 1f);
            Colors[(int)FuColors.TextSuccess] = new Vector4(97f / 255f, 217f / 255f, 124f / 255f, 1f);
            Colors[(int)FuColors.TextWarning] = new Vector4(255f / 255f, 199f / 255f, 30f / 255f, 1f);

            Colors[(int)FuColors.BackgroundDanger] = new Vector4(220f / 255f, 53f / 255f, 69f / 255f, 1f);
            Colors[(int)FuColors.BackgroundInfo] = new Vector4(23f / 255f, 162f / 255f, 184f / 255f, 1f);
            Colors[(int)FuColors.BackgroundSuccess] = new Vector4(40f / 255f, 167f / 255f, 69f / 255f, 1f);
            Colors[(int)FuColors.BackgroundWarning] = new Vector4(255f / 255f, 193f / 255f, 7f / 255f, 1f);

            for (int i = (int)FuColors.COUNT; i < colorsCount; i++)
            {
                Colors[i] = Vector4.one;
            }
        }

        /// <summary>
        /// Apply this theme to the current ImGui context
        /// </summary>
        internal void Apply(float scale)
        {
            var style = ImGui.GetStyle();
            // set style var
            style.Alpha = Alpha;
            style.WindowPadding = WindowPadding * scale;
            style.WindowRounding = WindowRounding * scale;
            style.WindowBorderSize = WindowBorderSize * scale;
            style.WindowMinSize = WindowMinSize * scale;
            style.WindowTitleAlign = WindowTitleAlign;
            style.WindowMenuButtonPosition = WindowMenuButtonPosition;
            style.ChildRounding = ChildRounding * scale;
            style.ChildBorderSize = ChildBorderSize * scale;
            style.PopupRounding = PopupRounding * scale;
            style.PopupBorderSize = PopupBorderSize * scale;
            style.FramePadding = FramePadding * scale;
            style.FrameRounding = FrameRounding * scale;
            style.FrameBorderSize = FrameBorderSize * scale;
            style.ItemSpacing = ItemSpacing * scale;
            style.ItemInnerSpacing = ItemInnerSpacing * scale;
            style.CellPadding = CellPadding * scale;
            style.IndentSpacing = IndentSpacing * scale;
            style.ColumnsMinSpacing = ColumnsMinSpacing * scale;
            style.ScrollbarSize = ScrollbarSize * scale;
            style.ScrollbarRounding = ScrollbarRounding * scale;
            style.GrabMinSize = GrabMinSize * scale;
            style.GrabRounding = GrabRounding * scale;
            style.TabRounding = TabRounding * scale;
            style.TabBorderSize = TabBorderSize * scale;
            style.TabMinWidthForCloseButton = TabMinWidthForCloseButton * scale;
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
        }

        public override string ToString()
        {
            return ThemeName;
        }
    }
}