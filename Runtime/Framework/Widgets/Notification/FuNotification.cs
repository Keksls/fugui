using ImGuiNET;
using System;
using UnityEngine;

namespace Fu.Framework
{
    /// <summary>Notification card: accent side/top/bottom per anchor, header click-to-collapse, body bg, AutoResizeY.</summary>
    internal class FuguiNotification
    {
        #region Data
        internal string Title;
        internal string Message;
        internal StateType Type;
        internal float Duration;
        internal float Height;
        internal Texture2D Icon;
        internal int Quantity;
        internal FuTextStyle TextColor;
        internal FuButtonStyle BGColor;

        internal Action ClickCallback;
        internal bool IsCollapsed;
        internal bool CanCollapse;

        private float _animationEnlapsed = 0f;
        private bool _removing = false;
        private bool _hasSpawn = false;

        private float _borderWidth => 1.5f * Fugui.CurrentContext.Scale;

        private float _initialDuration;
        #endregion

        #region Ctor
        /// <summary>Instantiate a new Notification object.</summary>
        internal FuguiNotification(string title, string message, StateType type, float duration, Action onClick = null, bool? startCollapsed = null)
        {
            Title = title;
            Message = message;
            Type = type;
            Duration = duration;
            _initialDuration = Mathf.Max(0.001f, duration);
            Height = 64f;
            Icon = null;
            Quantity = 1;
            TextColor = FuTextStyle.Default;
            BGColor = FuButtonStyle.Default;
            ClickCallback = onClick;
            _animationEnlapsed = Fugui.Settings.NotifyAnimlationDuration;

            switch (type)
            {
                case StateType.Danger: Icon = Fugui.Settings.DangerIcon; TextColor = FuTextStyle.Danger; BGColor = FuButtonStyle.Danger; break;
                case StateType.Success: Icon = Fugui.Settings.SuccessIcon; TextColor = FuTextStyle.Success; BGColor = FuButtonStyle.Success; break;
                case StateType.Info: Icon = Fugui.Settings.InfoIcon; TextColor = FuTextStyle.Info; BGColor = FuButtonStyle.Info; break;
                case StateType.Warning: Icon = Fugui.Settings.WarningIcon; TextColor = FuTextStyle.Warning; BGColor = FuButtonStyle.Warning; break;
            }

            CanCollapse = !string.IsNullOrEmpty(Title) && !string.IsNullOrEmpty(Message);
            IsCollapsed = false;
            if (startCollapsed.HasValue && CanCollapse) IsCollapsed = startCollapsed.Value;
        }
        #endregion

        #region Public API
        /// <summary>Remove this notification (animated).</summary>
        public void Close() { _removing = true; _animationEnlapsed = Fugui.Settings.NotifyAnimlationDuration; }

        /// <summary>Add a stacked notification (refresh timer and quantity).</summary>
        public void AddStackedNotification(float duration) { Duration = duration; _initialDuration = Mathf.Max(_initialDuration, duration); _animationEnlapsed = Fugui.Settings.NotifyAnimlationDuration; Quantity++; }

        /// <summary>Force collapsed state if collapsible.</summary>
        public void ForceCollapsed(bool collapsed) { if (CanCollapse) IsCollapsed = collapsed; }
        #endregion

        #region Rendering
        /// <summary>Draw the card (accent per anchor, header click collapse, body background, AutoResizeY).</summary>
        public bool Draw(ImDrawListPtr parentDrawList, int i, float deltaTime)
        {
            float width = Fugui.Settings.NotifyPanelWidth * Fugui.CurrentContext.Scale;
            Vector2 childTopLeft = ImGui.GetCursorScreenPos();

            // child with AutoResizeY (pas AlwaysAutoResize)
            bool opened = ImGui.BeginChild("notificationPanel" + i, new Vector2(width, 0f), ImGuiChildFlags.AutoResizeY, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
            if (opened)
            {
                var dl = ImGui.GetWindowDrawList();
                Vector2 winPos = ImGui.GetWindowPos();
                Vector2 winSize = ImGui.GetWindowSize();
                Vector2 closebtnPos = default;

                // HEADER
                ImGuiNative.igSpacing();
                float headerStartY = ImGui.GetCursorScreenPos().y;
                using (FuGrid grid = new FuGrid("notificationGrid" + i, new FuGridDefinition(3, new int[] { (int)(Fugui.Settings.NotifyIconSize), -36 }), FuGridFlag.NoAutoLabels, outterPadding:6f))
                {
                    grid.Image("notificationIcon" + i, Icon, new Vector2(Fugui.Settings.NotifyIconSize, Fugui.Settings.NotifyIconSize), TextColor.Text);

                    if (string.IsNullOrEmpty(Title))
                    {
                        grid.Text((Quantity > 1 ? "(" + Quantity + ") " : "") + Message, TextColor, FuTextWrapping.Wrap);
                    }
                    else
                    {
                        Fugui.PushFont(Fugui.CurrentContext.DefaultFont.Size, FontType.Bold);
                        grid.Text((Quantity > 1 ? "(" + Quantity + ") " : "") + Title, TextColor);
                        Fugui.PopFont();
                    }

                    Fugui.Push(ImGuiStyleVar.FrameRounding, 20f);
                    if (grid.Button("x", new Vector2(22, 22), Vector2.zero, Vector2.zero, FuButtonStyle.Default))
                    {
                        Close();
                    }
                    closebtnPos = grid.LastItemRect.center;
                    Fugui.PopStyle();
                }
               
                // header click→collapse (simple clic)
                Vector2 headerStart = new Vector2(winPos.x, headerStartY);
                Vector2 headerEnd = new Vector2(winPos.x + winSize.x, ImGui.GetCursorScreenPos().y);
                ImGui.SetCursorScreenPos(headerStart);
                ImGui.InvisibleButton("notify_header_" + i, headerEnd - headerStart);
                bool headerHovered = ImGui.IsItemHovered();
                if (headerHovered && ImGui.IsItemClicked(ImGuiMouseButton.Left) && CanCollapse) IsCollapsed = !IsCollapsed;

                // SEPARATOR + BODY BG
                if (!string.IsNullOrEmpty(Title) && !string.IsNullOrEmpty(Message) && !IsCollapsed)
                {
                    dl = ImGui.GetWindowDrawList();
                    Fugui.MoveY(-8f);
                    Vector2 sepA = new Vector2(winPos.x, ImGui.GetCursorScreenPos().y);
                    Vector2 sepB = new Vector2(winPos.x + winSize.x, ImGui.GetCursorScreenPos().y);
                    dl.AddLine(sepA, sepB, ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.Border)), 1f);

                    float bodyPad = 6f * Fugui.CurrentContext.Scale;

                    Vector2 textTL = new Vector2(ImGui.GetCursorScreenPos().x + bodyPad, ImGui.GetCursorScreenPos().y);
                    float beforeY = ImGui.GetCursorScreenPos().y;
                    ImGui.SetCursorScreenPos(textTL);
                    ImGuiNative.igSpacing();

                    Fugui.MoveXUnscaled(bodyPad);
                    float wrapWidth = ImGui.GetContentRegionAvail().x - bodyPad * 2.0f;
                    ImGui.PushTextWrapPos(ImGui.GetCursorPos().x + wrapWidth);
                    ImGui.TextWrapped(Message);
                    ImGui.Dummy(new Vector2(0f, bodyPad));
                    float afterY = ImGui.GetCursorScreenPos().y;

                    Vector2 bodyTL = new Vector2(winPos.x, beforeY);
                    Vector2 bodyBR = new Vector2(winPos.x + winSize.x, afterY);
                    uint bodyBG = ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.WindowBg));
                    dl.AddRectFilled(bodyTL + new Vector2(Fugui.Scale, 0f), bodyBR + new Vector2(-Fugui.Scale, -Fugui.Scale), bodyBG, Fugui.Themes.ChildRounding, ImDrawFlags.RoundCornersBottom);

                    ImGui.SetCursorScreenPos(textTL);
                    ImGuiNative.igSpacing();

                    Fugui.MoveXUnscaled(bodyPad);
                    ImGui.PushTextWrapPos(ImGui.GetCursorPos().x + wrapWidth);
                    ImGui.TextWrapped(Message);
                    ImGui.Dummy(new Vector2(0f, bodyPad));
                }

                // PROGRESS (close btn)
                uint pbCol = ImGui.GetColorU32(BGColor.Button);
                float ratio = Mathf.Clamp01(Duration / Mathf.Max(0.0001f, _initialDuration));
                Fugui.DrawArc(dl, closebtnPos, 11f * Fugui.Scale, 2f * Fugui.Scale, ratio, pbCol);

                bool hoverChild = ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows);

                ImGuiNative.igEndChild();
                Fugui.PopStyle();

                // ACCENT après EndChild (on a la bbox exacte)
                Vector2 childMin = childTopLeft;
                Vector2 childMax = new Vector2(childTopLeft.x + width, childTopLeft.y + winSize.y);
                // bord fin
                dl.AddRect(childMin, childMax, ImGui.GetColorU32(BGColor.ButtonHovered), Fugui.Themes.ChildRounding, ImDrawFlags.None, _borderWidth);

                // lifetime
                bool toRemove = update(deltaTime, pauseTimer: hoverChild);
                return toRemove;
            }
            else
                ImGuiNative.igEndChild();
            return false;
        }
        #endregion

        #region Internals
        /// <summary>Update animation and lifetime. Returns true when the card must be removed.</summary>
        private bool update(float deltaTime, bool pauseTimer)
        {
            if (_removing)
            {
                if (_animationEnlapsed > 0f) _animationEnlapsed = Mathf.Clamp(_animationEnlapsed - deltaTime, 0f, Fugui.Settings.NotifyAnimlationDuration);
                return _animationEnlapsed == 0f;
            }
            else
            {
                if (_animationEnlapsed < Fugui.Settings.NotifyAnimlationDuration) _animationEnlapsed = Mathf.Clamp(_animationEnlapsed + deltaTime, 0f, Fugui.Settings.NotifyAnimlationDuration);
                else if (!pauseTimer) Duration -= deltaTime;
                if (Duration <= 0f) Close();
                return false;
            }
        }
        #endregion
    }
}