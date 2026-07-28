using Assets.Scripts.UI.Windows.Common;
using Fu;

using System;
using UnityEngine;

/// <summary>
/// Implements the remote name keyboard widget logic.
/// </summary>
public class RemoteNameKeyboardWidget
{
    /// <summary>
    /// Stores key spec data.
    /// </summary>
    private struct KeySpec
    {
        public string Label;
        public string Value;
        public float Weight;
        public KeyAction Action;

        /// <summary>
        /// Creates a new key spec instance.
        /// </summary>
        public KeySpec(string label, string value, float weight, KeyAction action)
        {
            Label = label;
            Value = value;
            Weight = weight;
            Action = action;
        }
    }

    /// <summary>
    /// Lists the supported key action values.
    /// </summary>
    private enum KeyAction
    {
        Text,
        Space,
        Backspace,
        Clear,
        Shift,
        Submit
    }

    private const float CompactHeightThreshold = 360f;

    private static readonly Color DisabledColor = new Color(0.48f, 0.52f, 0.58f, 1f);

    private readonly string[][] letterRows =
    {
        new[] { "a", "z", "e", "r", "t", "y", "u", "i", "o", "p" },
        new[] { "q", "s", "d", "f", "g", "h", "j", "k", "l", "m" },
        new[] { "w", "x", "c", "v", "b", "n" }
    };

    private TimelineWidgetTheme _theme;
    private Action<string> _insertText;
    private Action _backspace;
    private Action _clear;
    private Action _submit;
    private Func<bool> _canEdit;
    private Func<bool> _canSubmit;
    private bool _shift = true;

    public TimelineWidgetTheme Theme
    {
        get { return _theme != null ? _theme : TimelineWidgetTheme.LoadDefault(); }
        set { _theme = value; }
    }

    /// <summary>
    /// Runs the bind logic.
    /// </summary>
    public void Bind(Action<string> insertText, Action backspace, Action clear, Action submit, Func<bool> canEdit, Func<bool> canSubmit)
    {
        _insertText = insertText;
        _backspace = backspace;
        _clear = clear;
        _submit = submit;
        _canEdit = canEdit;
        _canSubmit = canSubmit;
        _shift = true;
    }

    /// <summary>
    /// Runs the unbind logic.
    /// </summary>
    public void Unbind()
    {
        _insertText = null;
        _backspace = null;
        _clear = null;
        _submit = null;
        _canEdit = null;
        _canSubmit = null;
    }


    /// <summary>
    /// Draws the keyboard panel UI.
    /// </summary>
    public Rect DrawKeyboardPanel(FuWindow window)
    {
        if (window == null || window.Container == null)
            return new Rect();

        Vector2 containerSize = new Vector2(window.Container.Size.x, window.Container.Size.y);
        return DrawKeyboardPanel(Fugui.GetCurrentWindowDrawList(), window.LocalPosition, containerSize);
    }

    /// <summary>
    /// Draws the keyboard panel UI.
    /// </summary>
    private Rect DrawKeyboardPanel(FuDrawList drawList, Vector2 origin, Vector2 containerSize)
    {
        if (containerSize.x <= 0f || containerSize.y <= 0f)
            return new Rect(origin.x, origin.y, 0f, 0f);

        float scale = Fugui.Scale;
        TimelineWidgetTheme theme = Theme;
        bool compact = containerSize.y < CompactHeightThreshold * scale;
        Rect panelRect = new Rect(origin, containerSize);

        Fugui.PushFont(18);
        DrawKeyboardPanel(drawList, panelRect, compact, scale, theme);
        Fugui.PopFont();

        Fugui.SetCursorScreenPos(new Vector2(origin.x, origin.y + containerSize.y));
        return panelRect;
    }

    /// <summary>
    /// Draws the keyboard panel UI.
    /// </summary>
    private void DrawKeyboardPanel(FuDrawList drawList, Rect panelRect, bool compact, float scale, TimelineWidgetTheme theme)
    {
        float padding = (compact ? 14f : 18f) * scale;
        float rounding = theme.DockRadius * scale;

        drawList.AddRectFilled(panelRect.min + new Vector2(0f, 5f * scale), panelRect.max + new Vector2(0f, 7f * scale), ColorU32(theme.DockShadow, 0.70f), rounding);
        drawList.AddRectFilled(panelRect.min, panelRect.max, ColorU32(theme.DockBackground), rounding);
        drawList.AddRect(panelRect.min, panelRect.max, ColorU32(theme.DockBorder), rounding);

        Rect contentRect = new Rect(
            panelRect.x + padding,
            panelRect.y + padding,
            Mathf.Max(1f, panelRect.width - padding * 2f),
            Mathf.Max(1f, panelRect.height - padding * 2f));

        Fugui.PushClipRect(panelRect.min, panelRect.max, true);
        DrawRows(drawList, contentRect, compact, scale, theme);
        Fugui.PopClipRect();
    }

    /// <summary>
    /// Draws the rows UI.
    /// </summary>
    private void DrawRows(FuDrawList drawList, Rect rect, bool compact, float scale, TimelineWidgetTheme theme)
    {
        KeySpec[][] rows = BuildRows();
        float rowGap = (compact ? 8f : 10f) * scale;
        float keyGap = (compact ? 7f : 9f) * scale;
        float rowHeight = (rect.height - rowGap * (rows.Length - 1)) / rows.Length;

        for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
        {
            KeySpec[] row = rows[rowIndex];
            float totalWeight = 0f;
            for (int i = 0; i < row.Length; i++)
                totalWeight += row[i].Weight;

            float availableWidth = rect.width - keyGap * (row.Length - 1);
            float unitWidth = availableWidth / Mathf.Max(1f, totalWeight);
            float x = rect.x;
            float y = rect.y + rowIndex * (rowHeight + rowGap);

            for (int i = 0; i < row.Length; i++)
            {
                float width = unitWidth * row[i].Weight;
                Rect keyRect = new Rect(x, y, width, rowHeight);
                DrawKey(drawList, keyRect, row[i], rowIndex, i, scale, theme);
                x += width + keyGap;
            }
        }
    }

    /// <summary>
    /// Builds the rows data.
    /// </summary>
    private KeySpec[][] BuildRows()
    {
        return new[]
        {
            BuildLetterRow(letterRows[0]),
            BuildLetterRow(letterRows[1]),
            new[]
            {
                new KeySpec("SHIFT", string.Empty, 1.55f, KeyAction.Shift),
                TextKey("w"), TextKey("x"), TextKey("c"), TextKey("v"), TextKey("b"), TextKey("n"),
                new KeySpec("DEL", string.Empty, 1.55f, KeyAction.Backspace)
            },
            new[]
            {
                new KeySpec("CLEAR", string.Empty, 1.55f, KeyAction.Clear),
                new KeySpec("-", "-", 0.9f, KeyAction.Text),
                new KeySpec("'", "'", 0.9f, KeyAction.Text),
                new KeySpec("SPACE", " ", 4.1f, KeyAction.Space),
                new KeySpec("OK", string.Empty, 1.55f, KeyAction.Submit)
            }
        };
    }

    /// <summary>
    /// Builds the letter row data.
    /// </summary>
    private KeySpec[] BuildLetterRow(string[] values)
    {
        KeySpec[] keys = new KeySpec[values.Length];
        for (int i = 0; i < values.Length; i++)
            keys[i] = TextKey(values[i]);

        return keys;
    }

    /// <summary>
    /// Runs the text key logic.
    /// </summary>
    private KeySpec TextKey(string value)
    {
        string label = _shift ? value.ToUpperInvariant() : value;
        return new KeySpec(label, value, 1f, KeyAction.Text);
    }

    /// <summary>
    /// Draws the key UI.
    /// </summary>
    private void DrawKey(FuDrawList drawList, Rect rect, KeySpec key, int row, int column, float scale, TimelineWidgetTheme theme)
    {
        bool enabled = IsEnabled(key.Action);
        bool selected = key.Action == KeyAction.Shift && _shift;
        bool clicked = DrawInvisibleButton(rect, "remoteNameKeyboard" + row + "_" + column, enabled, out bool hovered, out bool active);
        Color background = selected
            ? theme.PillBackgroundActive
            : active ? theme.PillBackgroundActive : hovered ? theme.PillBackgroundHover : theme.PillBackground;
        Color border = selected ? theme.Accent : enabled ? theme.DockBorder : DisabledColor;
        Color text = enabled ? theme.Text : theme.TextFaint;

        drawList.AddRectFilled(rect.min, rect.max, ColorU32(background, enabled ? 1f : 0.44f), theme.SmallRadius * scale);
        drawList.AddRect(rect.min, rect.max, ColorU32(border, selected ? 0.90f : enabled ? 0.72f : 0.35f), theme.SmallRadius * scale, FuDrawFlags.None, Mathf.Max(1f, scale));

        int fontSize = key.Label.Length > 1 ? 11 : 15;
        PushFont(fontSize, key.Action == KeyAction.Submit || key.Action == KeyAction.Shift);
        DrawTextCentered(drawList, rect, key.Label, ColorU32(text, enabled ? 1f : 0.55f));
        PopFont(key.Action == KeyAction.Submit || key.Action == KeyAction.Shift);

        if (hovered)
            Fugui.SetMouseCursor(FuMouseCursor.Hand);

        if (clicked)
            ExecuteKey(key);
    }

    /// <summary>
    /// Returns whether the enabled condition is met.
    /// </summary>
    private bool IsEnabled(KeyAction action)
    {
        if (action == KeyAction.Submit)
            return _canSubmit == null || _canSubmit();

        return _canEdit == null || _canEdit();
    }

    /// <summary>
    /// Runs the execute key logic.
    /// </summary>
    private void ExecuteKey(KeySpec key)
    {
        switch (key.Action)
        {
            case KeyAction.Text:
                _insertText?.Invoke(_shift ? key.Value.ToUpperInvariant() : key.Value);
                if (_shift)
                    _shift = false;
                break;
            case KeyAction.Space:
                _insertText?.Invoke(" ");
                break;
            case KeyAction.Backspace:
                _backspace?.Invoke();
                break;
            case KeyAction.Clear:
                _clear?.Invoke();
                _shift = true;
                break;
            case KeyAction.Shift:
                _shift = !_shift;
                break;
            case KeyAction.Submit:
                _submit?.Invoke();
                break;
        }
    }

    /// <summary>
    /// Draws the invisible button UI.
    /// </summary>
    private static bool DrawInvisibleButton(Rect rect, string id, bool enabled, out bool hovered, out bool active)
    {
        Fugui.SetCursorScreenPos(rect.min);
        Fugui.InvisibleButton("##" + id, rect.size);
        hovered = enabled && Fugui.IsItemHovered();
        active = enabled && Fugui.IsItemActive();
        return enabled && Fugui.IsItemClicked(ImGuiMouseButton.Left);
    }


    /// <summary>
    /// Runs the color u 32 logic.
    /// </summary>
    private static uint ColorU32(Color color)
    {
        return Fugui.GetColorU32(new Vector4(color.r, color.g, color.b, color.a));
    }

    /// <summary>
    /// Runs the color u 32 logic.
    /// </summary>
    private static uint ColorU32(Color color, float opacity)
    {
        return Fugui.GetColorU32(new Vector4(color.r, color.g, color.b, color.a * Mathf.Clamp01(opacity)));
    }

    /// <summary>
    /// Runs the push font logic.
    /// </summary>
    private static void PushFont(int size, bool bold)
    {
        Fugui.PushFont(size, bold ? FontType.Bold : FontType.Regular);
    }

    /// <summary>
    /// Runs the pop font logic.
    /// </summary>
    private static void PopFont(bool bold)
    {
        Fugui.PopFont();
    }

    /// <summary>
    /// Draws the text centered UI.
    /// </summary>
    private static void DrawTextCentered(FuDrawList drawList, Rect rect, string text, uint color)
    {
        Vector2 textSize = Fugui.CalcTextSize(text);
        Vector2 textPos = new Vector2(
            rect.x + (rect.width - textSize.x) * 0.5f,
            rect.y + (rect.height - textSize.y) * 0.5f);
        drawList.AddText(textPos, color, text);
    }
}
