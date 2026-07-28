using Fu;
using Fu.Framework;

using System;
using UnityEngine;

namespace Assets.Scripts.UI.Windows.Common
{
    /// <summary>
    /// Draws the compact VR belt toolbar used to switch local XR panels.
    /// </summary>
    public sealed class VRBeltWidget
    {
        #region Constants
        private const float MinWidth = 450f;
        private const float OuterPadding = 8f;
        private const float ButtonSize = 46f;
        private const float ButtonGap = 8f;
        private const float TimelineControlsGap = 10f;
        #endregion

        #region State
        private readonly TimelineWidget _timelineWidget = new TimelineWidget();
        private TimelineWidgetTheme _theme;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the active widget theme.
        /// </summary>
        public TimelineWidgetTheme Theme
        {
            get { return _theme != null ? _theme : TimelineWidgetTheme.LoadDefault(); }
            set { _theme = value; }
        }
        #endregion

        #region Public API
        /// <summary>
        /// Sets the active widget theme.
        /// </summary>
        public void SetTheme(TimelineWidgetTheme theme)
        {
            _theme = theme;
            _timelineWidget.SetTheme(theme);
        }

        /// <summary>
        /// Draws the belt and dispatches panel button clicks through the supplied callback.
        /// </summary>
        public void Draw(Rect rect, VRBeltPanelId activePanel, VRBeltWidgetButton[] buttons, Action<VRBeltPanelId> onPanelClicked)
        {
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            TimelineWidgetTheme theme = Theme;
            _timelineWidget.SetTheme(theme);

            float scale = Fugui.Scale;
            Rect normalizedRect = NormalizeRect(rect, scale);
            FuDrawList drawList = Fugui.GetCurrentWindowDrawList();
            float rounding = Mathf.Min(normalizedRect.height * 0.35f, 18f * scale);

            FlatCameraInputBlocker.RegisterRect(normalizedRect);
            drawList.AddRectFilled(normalizedRect.min, normalizedRect.max, ColorU32(theme.DockBackground), rounding);
            drawList.AddRect(normalizedRect.min, normalizedRect.max, ColorU32(theme.DockBorder), rounding, FuDrawFlags.None, Mathf.Max(1f, scale));

            Rect toolbarRect = GetToolbarRect(normalizedRect, scale);
            DrawToolbar(drawList, toolbarRect, activePanel, buttons, onPanelClicked, theme, scale);
        }
        #endregion

        #region Layout
        /// <summary>
        /// Normalizes the belt rect to a usable minimum width.
        /// </summary>
        private static Rect NormalizeRect(Rect rect, float scale)
        {
            float minWidth = MinWidth * scale;
            if (rect.width >= minWidth)
                return rect;

            return new Rect(rect.x, rect.y, minWidth, rect.height);
        }

        /// <summary>
        /// Returns the toolbar row rectangle.
        /// </summary>
        private static Rect GetToolbarRect(Rect rect, float scale)
        {
            float padding = OuterPadding * scale;
            return new Rect(
                rect.x + padding,
                rect.y + padding,
                Mathf.Max(1f, rect.width - padding * 2f),
                Mathf.Max(1f, rect.height - padding * 2f));
        }
        #endregion

        #region Toolbar
        /// <summary>
        /// Draws the top row containing timeline controls and panel buttons.
        /// </summary>
        private void DrawToolbar(
            FuDrawList drawList,
            Rect rect,
            VRBeltPanelId activePanel,
            VRBeltWidgetButton[] buttons,
            Action<VRBeltPanelId> onPanelClicked,
            TimelineWidgetTheme theme,
            float scale)
        {
            float controlsWidth = Mathf.Min(theme.ControlsWidth * scale, rect.width);
            Rect controlsRect = new Rect(rect.x, rect.y, controlsWidth, rect.height);
            float buttonsX = controlsRect.xMax + TimelineControlsGap * scale;
            Rect buttonsRect = new Rect(buttonsX, rect.y, Mathf.Max(1f, rect.xMax - buttonsX), rect.height);

            // Standard timeline controls stay available regardless of the panel displayed above the belt.
            _timelineWidget.DrawControls(controlsRect);
            DrawPanelButtons(drawList, buttonsRect, activePanel, buttons, onPanelClicked, theme, scale);
        }

        /// <summary>
        /// Draws the panel switch buttons centered inside their available area.
        /// </summary>
        private void DrawPanelButtons(
            FuDrawList drawList,
            Rect rect,
            VRBeltPanelId activePanel,
            VRBeltWidgetButton[] buttons,
            Action<VRBeltPanelId> onPanelClicked,
            TimelineWidgetTheme theme,
            float scale)
        {
            if (buttons == null || buttons.Length == 0)
                return;

            float buttonSize = Mathf.Min(ButtonSize * scale, rect.height);
            float gap = ButtonGap * scale;
            float totalWidth = buttons.Length * buttonSize + Mathf.Max(0, buttons.Length - 1) * gap;
            float x = rect.x + Mathf.Max(0f, (rect.width - totalWidth) * 0.5f);
            float y = rect.y + (rect.height - buttonSize) * 0.5f;

            for (int i = 0; i < buttons.Length; i++)
            {
                Rect buttonRect = new Rect(x + i * (buttonSize + gap), y, buttonSize, buttonSize);
                DrawPanelButton(drawList, buttonRect, buttons[i], activePanel == buttons[i].PanelId, onPanelClicked, theme, scale);
            }
        }

        /// <summary>
        /// Draws one panel switch button.
        /// </summary>
        private static void DrawPanelButton(
            FuDrawList drawList,
            Rect rect,
            VRBeltWidgetButton button,
            bool selected,
            Action<VRBeltPanelId> onPanelClicked,
            TimelineWidgetTheme theme,
            float scale)
        {
            bool hovered = rect.Contains(Fugui.GetCurrentMouse().Position);
            bool active = hovered && Fugui.GetCurrentMouse().IsPressed(FuMouseButton.Left);
            bool clicked = hovered && Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left);
            Color background = button.Background.a > 0f ? button.Background : theme.PillBackground;
            if (selected)
                background = Color.Lerp(background, theme.Accent, 0.35f);
            else if (active)
                background = theme.ButtonPressed;
            else if (hovered)
                background = theme.ButtonHover;

            FlatCameraInputBlocker.RegisterRect(rect);
            drawList.AddRectFilled(rect.min, rect.max, ColorU32(background), theme.MediumRadius * scale);
            drawList.AddRect(rect.min, rect.max, ColorU32(selected ? theme.Accent : theme.DockBorder), theme.MediumRadius * scale, FuDrawFlags.None, Mathf.Max(1f, scale));

            if (button.Icon != null)
                DrawTextureIcon(drawList, rect, button.Icon, scale);
            else
                DrawFallbackIcon(drawList, rect, button, theme, scale);

            if (hovered)
                Fugui.SetMouseCursor(FuMouseCursor.Hand);

            if (clicked)
                onPanelClicked?.Invoke(button.PanelId);
        }
        #endregion

        #region Drawing utilities
        /// <summary>
        /// Draws an image icon from a Texture2D.
        /// </summary>
        private static void DrawTextureIcon(FuDrawList drawList, Rect rect, Texture2D icon, float scale)
        {
            float inset = 10f * scale;
            Rect iconRect = new Rect(rect.x + inset, rect.y + inset, Mathf.Max(1f, rect.width - inset * 2f), Mathf.Max(1f, rect.height - inset * 2f));
            drawList.AddImage(icon, iconRect.min, iconRect.max);
        }

        /// <summary>
        /// Draws a simple fallback icon when no texture is assigned.
        /// </summary>
        private static void DrawFallbackIcon(FuDrawList drawList, Rect rect, VRBeltWidgetButton button, TimelineWidgetTheme theme, float scale)
        {
            Rect glyphRect = new Rect(rect.x + 13f * scale, rect.y + 13f * scale, rect.width - 26f * scale, rect.height - 26f * scale);
            drawList.AddRectFilled(glyphRect.min, glyphRect.max, ColorU32(theme.Text, 0.88f), theme.SmallRadius * scale);

            if (string.IsNullOrWhiteSpace(button.FallbackLabel))
                return;

            Fugui.PushFont(10);
            Fugui.PushFont(FontType.Bold);
            DrawTextCentered(drawList, rect, button.FallbackLabel.Substring(0, 1).ToUpperInvariant(), ColorU32(theme.TextInk));
            Fugui.PopFont();
            Fugui.PopFont();
        }

        /// <summary>
        /// Draws centered text inside a rectangle.
        /// </summary>
        private static void DrawTextCentered(FuDrawList drawList, Rect rect, string text, uint color)
        {
            Vector2 textSize = Fugui.CalcTextSize(text);
            Vector2 textPos = new Vector2(rect.x + (rect.width - textSize.x) * 0.5f, rect.y + (rect.height - textSize.y) * 0.5f);
            drawList.AddText(textPos, color, text);
        }

        /// <summary>
        /// Converts a color to a packed Fugui color.
        /// </summary>
        private static uint ColorU32(Color color)
        {
            return Fugui.GetColorU32(new Vector4(color.r, color.g, color.b, color.a));
        }

        /// <summary>
        /// Converts a color and opacity to a packed Fugui color.
        /// </summary>
        private static uint ColorU32(Color color, float opacity)
        {
            return Fugui.GetColorU32(new Vector4(color.r, color.g, color.b, color.a * Mathf.Clamp01(opacity)));
        }

        #endregion
    }
}
