using Assets.Scripts.UI.Windows.Common;
using Fu;
using Fu.Framework;

using Saravr.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.UI.Shortcuts
{
    /// <summary>
    /// Draws the flat shortcut remapping popup.
    /// </summary>
    public sealed class ShortcutSettingsWidget
    {
        private const float HeaderHeight = 64f;
        private const float FooterHeight = 58f;
        private const float RowHeight = 48f;
        private const float PanelWidth = 620f;
        private const float PanelPadding = 18f;

        private bool _open;
        private bool _capturing;
        private SaraShortcutAction _capturingAction;
        private string _message;

        /// <summary>
        /// Gets whether the popup is open.
        /// </summary>
        public bool IsOpen => _open;

        /// <summary>
        /// Opens the shortcut settings popup.
        /// </summary>
        public void Open()
        {
            _open = true;
            _capturing = false;
            _message = null;
        }

        /// <summary>
        /// Draws the popup if open.
        /// </summary>
        public void Draw(Vector2 containerSize, TimelineWidgetTheme theme)
        {
            if (!_open)
                return;

            FlatRaycaster.Current?.EndPointing();
            FlatCameraInputBlocker.BlockAllForFrame();

            float scale = Fugui.Scale;
            FuDrawList drawList = Fugui.GetCurrentWindowDrawList();
            Rect overlayRect = new Rect(0f, 0f, containerSize.x, containerSize.y);
            SaraShortcutAction[] actions = SaraShortcutSettings.GetActions();
            float contentHeight = HeaderHeight + FooterHeight + PanelPadding * 2f + actions.Length * RowHeight + 42f;
            float width = Mathf.Min(PanelWidth * scale, containerSize.x * 0.90f);
            float height = Mathf.Min(contentHeight * scale, containerSize.y * 0.90f);
            Rect panelRect = new Rect(
                (containerSize.x - width) * 0.5f,
                (containerSize.y - height) * 0.5f,
                width,
                height);

            drawList.AddRectFilled(overlayRect.min, overlayRect.max, ColorU32(theme.SettingsOverlay, 0.92f));
            drawList.AddRectFilled(panelRect.min, panelRect.max, ColorU32(theme.SettingsPanelBackground), theme.MediumRadius * scale);
            drawList.AddRect(panelRect.min, panelRect.max, ColorU32(theme.DockBorder), theme.MediumRadius * scale, FuDrawFlags.None, Mathf.Max(1f, scale));
            FlatCameraInputBlocker.RegisterRect(panelRect);

            if (WasKeyPressed(Key.Escape))
            {
                if (_capturing)
                {
                    _capturing = false;
                    _message = null;
                }
                else
                {
                    Close();
                    return;
                }
            }

            if (_capturing)
                HandleCapture();

            DrawHeader(drawList, panelRect, theme);
            DrawRows(drawList, panelRect, theme, actions);
            DrawFooter(drawList, panelRect, theme);
        }

        /// <summary>
        /// Closes the shortcut settings popup.
        /// </summary>
        private void Close()
        {
            _open = false;
            _capturing = false;
            _message = null;
        }

        /// <summary>
        /// Handles key capture for the selected action.
        /// </summary>
        private void HandleCapture()
        {
            if (WasKeyPressed(Key.Backspace) || WasKeyPressed(Key.Delete))
            {
                SaraShortcutSettings.SetBinding(_capturingAction, new SaraShortcutBinding(Key.None));
                _capturing = false;
                _message = "Shortcut cleared.";
                return;
            }

            if (!SaraShortcutSettings.TryCapturePressedBinding(out SaraShortcutBinding binding))
                return;

            if (SaraShortcutSettings.TryFindConflict(_capturingAction, binding, out SaraShortcutAction conflict))
            {
                _message = "Already used by " + SaraShortcutSettings.GetActionLabel(conflict) + ".";
                return;
            }

            SaraShortcutSettings.SetBinding(_capturingAction, binding);
            _capturing = false;
            _message = "Shortcut updated.";
        }

        /// <summary>
        /// Draws the popup header.
        /// </summary>
        private void DrawHeader(FuDrawList drawList, Rect panelRect, TimelineWidgetTheme theme)
        {
            float scale = Fugui.Scale;
            Rect headerRect = new Rect(panelRect.x, panelRect.y, panelRect.width, HeaderHeight * scale);
            Rect titleRect = new Rect(headerRect.x + 22f * scale, headerRect.y, headerRect.width - 84f * scale, headerRect.height);
            Rect closeRect = new Rect(headerRect.xMax - 52f * scale, headerRect.y + 18f * scale, 30f * scale, 30f * scale);

            PushFont(18, true);
            DrawTextLeftCentered(drawList, titleRect, "Keyboard shortcuts", ColorU32(theme.Text), 0f);
            PopFont(true);

            drawList.AddLine(
                new Vector2(headerRect.x, headerRect.yMax),
                new Vector2(headerRect.xMax, headerRect.yMax),
                ColorU32(theme.DockBorder, 0.62f),
                Mathf.Max(1f, scale));

            if (DrawIconCloseButton(drawList, closeRect, theme))
                Close();
        }

        /// <summary>
        /// Draws all shortcut rows.
        /// </summary>
        private void DrawRows(FuDrawList drawList, Rect panelRect, TimelineWidgetTheme theme, SaraShortcutAction[] actions)
        {
            float scale = Fugui.Scale;
            float y = panelRect.y + HeaderHeight * scale + PanelPadding * scale;
            Rect rowsRect = new Rect(
                panelRect.x + PanelPadding * scale,
                y,
                panelRect.width - PanelPadding * 2f * scale,
                panelRect.height - (HeaderHeight + FooterHeight + PanelPadding * 2f) * scale);

            drawList.PushClipRect(rowsRect.min, rowsRect.max, true);

            for (int i = 0; i < actions.Length; i++)
            {
                SaraShortcutAction action = actions[i];
                Rect rowRect = new Rect(rowsRect.x, y + i * RowHeight * scale, rowsRect.width, RowHeight * scale);
                DrawShortcutRow(drawList, rowRect, theme, action);
            }

            drawList.PopClipRect();
        }

        /// <summary>
        /// Draws one shortcut row.
        /// </summary>
        private void DrawShortcutRow(FuDrawList drawList, Rect rect, TimelineWidgetTheme theme, SaraShortcutAction action)
        {
            float scale = Fugui.Scale;
            bool isCapturing = _capturing && _capturingAction == action;
            drawList.AddLine(rect.min, new Vector2(rect.xMax, rect.y), ColorU32(theme.SettingsRowDivider), Mathf.Max(1f, scale));

            Rect labelRect = new Rect(rect.x + 4f * scale, rect.y, rect.width * 0.44f, rect.height);
            Rect keyRect = new Rect(rect.x + rect.width * 0.50f, rect.y + 8f * scale, rect.width * 0.24f, rect.height - 16f * scale);
            Rect changeRect = new Rect(rect.xMax - 128f * scale, rect.y + 8f * scale, 72f * scale, rect.height - 16f * scale);
            Rect clearRect = new Rect(rect.xMax - 48f * scale, rect.y + 8f * scale, 44f * scale, rect.height - 16f * scale);

            PushFont(14, true);
            DrawTextLeftCentered(drawList, labelRect, ClipTextToWidth(SaraShortcutSettings.GetActionLabel(action), labelRect.width), ColorU32(theme.Text), 0f);
            PopFont(true);

            string keyLabel = isCapturing ? "Press key..." : SaraShortcutSettings.GetDisplayString(action);
            DrawReadonlyPill(drawList, keyRect, keyLabel, theme, isCapturing);

            if (DrawSmallButton(drawList, changeRect, isCapturing ? "Cancel" : "Change", theme, true))
            {
                if (isCapturing)
                {
                    _capturing = false;
                    _message = null;
                }
                else
                {
                    _capturing = true;
                    _capturingAction = action;
                    _message = "Press a key. Backspace clears, Escape cancels.";
                }
            }

            if (DrawSmallButton(drawList, clearRect, "Clear", theme, SaraShortcutSettings.GetBinding(action).IsAssigned))
            {
                SaraShortcutSettings.SetBinding(action, new SaraShortcutBinding(Key.None));
                if (isCapturing)
                    _capturing = false;
                _message = "Shortcut cleared.";
            }
        }

        /// <summary>
        /// Draws the popup footer.
        /// </summary>
        private void DrawFooter(FuDrawList drawList, Rect panelRect, TimelineWidgetTheme theme)
        {
            float scale = Fugui.Scale;
            Rect footerRect = new Rect(panelRect.x, panelRect.yMax - FooterHeight * scale, panelRect.width, FooterHeight * scale);
            Rect messageRect = new Rect(footerRect.x + 22f * scale, footerRect.y, footerRect.width - 232f * scale, footerRect.height);
            Rect resetRect = new Rect(footerRect.xMax - 202f * scale, footerRect.y + 12f * scale, 90f * scale, footerRect.height - 24f * scale);
            Rect doneRect = new Rect(footerRect.xMax - 102f * scale, footerRect.y + 12f * scale, 80f * scale, footerRect.height - 24f * scale);

            drawList.AddLine(
                new Vector2(footerRect.x, footerRect.y),
                new Vector2(footerRect.xMax, footerRect.y),
                ColorU32(theme.DockBorder, 0.62f),
                Mathf.Max(1f, scale));

            string message = _message;
            if (string.IsNullOrEmpty(message))
                message = _capturing ? "Press a key. Backspace clears, Escape cancels." : "Desktop shortcuts are disabled while a panel captures input.";

            PushFont(12, false);
            DrawTextLeftCentered(drawList, messageRect, ClipTextToWidth(message, messageRect.width), ColorU32(theme.TextFaint), 0f);
            PopFont(false);

            if (DrawSmallButton(drawList, resetRect, "Reset", theme, true))
            {
                SaraShortcutSettings.ResetToDefaults();
                _capturing = false;
                _message = "Defaults restored.";
            }

            if (DrawSmallButton(drawList, doneRect, "Done", theme, true))
                Close();
        }

        /// <summary>
        /// Draws a readonly shortcut pill.
        /// </summary>
        private static void DrawReadonlyPill(FuDrawList drawList, Rect rect, string label, TimelineWidgetTheme theme, bool active)
        {
            float scale = Fugui.Scale;
            Color fill = active ? theme.PillBackgroundActive : theme.SettingsDropdownBackground;
            Color text = active ? theme.Accent : theme.TextDim;
            drawList.AddRectFilled(rect.min, rect.max, ColorU32(fill), rect.height * 0.5f);
            drawList.AddRect(rect.min, rect.max, ColorU32(theme.DockBorder), rect.height * 0.5f, FuDrawFlags.None, Mathf.Max(1f, scale));

            PushFont(11, true);
            DrawTextCentered(drawList, rect, ClipTextToWidth(label, rect.width - 12f * scale), ColorU32(text));
            PopFont(true);
        }

        /// <summary>
        /// Draws a compact button.
        /// </summary>
        private static bool DrawSmallButton(FuDrawList drawList, Rect rect, string label, TimelineWidgetTheme theme, bool enabled)
        {
            bool hovered = enabled && rect.Contains(Fugui.GetCurrentMouse().Position);
            bool active = hovered && Fugui.GetCurrentMouse().IsPressed(FuMouseButton.Left);
            bool clicked = hovered && Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left);
            float scale = Fugui.Scale;
            Color fill = active ? theme.PillBackgroundActive : hovered ? theme.PillBackgroundHover : theme.SettingsDropdownBackground;
            Color text = enabled ? theme.TextDim : WithAlpha(theme.TextFaint, 0.45f);

            FlatCameraInputBlocker.RegisterRect(rect);
            drawList.AddRectFilled(rect.min, rect.max, ColorU32(fill), rect.height * 0.5f);
            drawList.AddRect(rect.min, rect.max, ColorU32(theme.DockBorder), rect.height * 0.5f, FuDrawFlags.None, Mathf.Max(1f, scale));

            PushFont(11, true);
            DrawTextCentered(drawList, rect, ClipTextToWidth(label, rect.width - 8f * scale), ColorU32(text));
            PopFont(true);

            if (hovered)
                Fugui.SetMouseCursor(FuMouseCursor.Hand);

            return clicked;
        }

        /// <summary>
        /// Draws the close icon button.
        /// </summary>
        private static bool DrawIconCloseButton(FuDrawList drawList, Rect rect, TimelineWidgetTheme theme)
        {
            bool hovered = rect.Contains(Fugui.GetCurrentMouse().Position);
            bool active = hovered && Fugui.GetCurrentMouse().IsPressed(FuMouseButton.Left);
            bool clicked = hovered && Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left);
            float scale = Fugui.Scale;
            Color bg = hovered || active ? theme.SettingsCloseBackgroundHover : theme.SettingsCloseBackground;
            Color iconColor = hovered || active ? theme.Text : theme.TextDim;

            FlatCameraInputBlocker.RegisterRect(rect);
            drawList.AddRectFilled(rect.min, rect.max, ColorU32(bg), 8f * scale);
            drawList.AddRect(rect.min, rect.max, ColorU32(theme.DockBorder), 8f * scale);

            float pad = 9f * scale;
            float thickness = Mathf.Max(2f * scale, 1f);
            uint col = ColorU32(iconColor);
            drawList.AddLine(rect.min + new Vector2(pad, pad), rect.max - new Vector2(pad, pad), col, thickness);
            drawList.AddLine(new Vector2(rect.xMax - pad, rect.y + pad), new Vector2(rect.x + pad, rect.yMax - pad), col, thickness);

            if (hovered)
                Fugui.SetMouseCursor(FuMouseCursor.Hand);

            return clicked;
        }

        /// <summary>
        /// Runs the color u 32 logic.
        /// </summary>
        private static uint ColorU32(Color color)
        {
            return Fugui.GetColorU32(new Vector4(color.r, color.g, color.b, color.a));
        }

        /// <summary>
        /// Returns whether a keyboard key was pressed this frame.
        /// </summary>
        private static bool WasKeyPressed(Key key)
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard[key].wasPressedThisFrame;
        }

        /// <summary>
        /// Runs the color u 32 logic.
        /// </summary>
        private static uint ColorU32(Color color, float opacity)
        {
            return Fugui.GetColorU32(new Vector4(color.r, color.g, color.b, color.a * Mathf.Clamp01(opacity)));
        }

        /// <summary>
        /// Runs the with alpha logic.
        /// </summary>
        private static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
        }

        /// <summary>
        /// Runs the push font logic.
        /// </summary>
        private static void PushFont(int size, bool bold)
        {
            Fugui.PushFont(size);
            if (bold)
                Fugui.PushFont(FontType.Bold);
        }

        /// <summary>
        /// Runs the pop font logic.
        /// </summary>
        private static void PopFont(bool bold)
        {
            if (bold)
                Fugui.PopFont();
            Fugui.PopFont();
        }

        /// <summary>
        /// Draws text centered in a rect.
        /// </summary>
        private static void DrawTextCentered(FuDrawList drawList, Rect rect, string text, uint color)
        {
            Vector2 textSize = Fugui.CalcTextSize(text);
            Vector2 textPos = new Vector2(rect.x + (rect.width - textSize.x) * 0.5f, rect.y + (rect.height - textSize.y) * 0.5f);
            drawList.AddText(textPos, color, text);
        }

        /// <summary>
        /// Draws text left-centered in a rect.
        /// </summary>
        private static void DrawTextLeftCentered(FuDrawList drawList, Rect rect, string text, uint color, float padding)
        {
            Vector2 textSize = Fugui.CalcTextSize(text);
            Vector2 textPos = new Vector2(rect.x + padding, rect.y + (rect.height - textSize.y) * 0.5f);
            drawList.AddText(textPos, color, text);
        }

        /// <summary>
        /// Clips text to a maximum width.
        /// </summary>
        private static string ClipTextToWidth(string text, float maxWidth)
        {
            if (string.IsNullOrEmpty(text) || Fugui.CalcTextSize(text).x <= maxWidth)
                return text;

            const string suffix = "...";
            float suffixWidth = Fugui.CalcTextSize(suffix).x;
            if (suffixWidth >= maxWidth)
                return suffix;

            for (int i = text.Length - 1; i > 0; i--)
            {
                string candidate = text.Substring(0, i).TrimEnd() + suffix;
                if (Fugui.CalcTextSize(candidate).x <= maxWidth)
                    return candidate;
            }

            return suffix;
        }
    }
}
