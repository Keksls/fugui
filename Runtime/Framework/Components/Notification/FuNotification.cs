using Fu.Core;
using ImGuiNET;
using UnityEngine;

namespace Fu.Framework
{
    /// <summary>
    /// a class that hold data that represent a notification
    /// </summary>
    internal class FuguiNotification
    {
        internal string Title;
        internal string Message;
        internal StateType Type;
        internal float Duration;
        internal float Height;
        internal Texture2D Icon;
        internal int Quantity;
        internal FuTextStyle TextColor;
        internal FuButtonStyle BGColor;
        private float _animationEnlapsed = 0f;
        private bool _removing = false;
        private bool _hasSpawn = false;

        /// <summary>
        /// Instantiate a new Notification object
        /// </summary>
        /// <param name="title">Title of the notification (can be null if message is not)</param>
        /// <param name="message">Message of the notification (can be null if title is not)</param>
        /// <param name="type">Type of the notification</param>
        /// <param name="duration">Duration before this notification disapear</param>
        internal FuguiNotification(string title, string message, StateType type, float duration)
        {
            Title = title;
            Message = message;
            Type = type;
            Duration = duration;
            Height = 64f;
            Icon = null;
            Quantity = 1;
            TextColor = FuTextStyle.Default;
            BGColor = FuButtonStyle.Default;
            _animationEnlapsed = Fugui.Settings.NotifyAnimlationDuration;

            switch (type)
            {
                case StateType.Danger:
                    Icon = Fugui.Settings.DangerIcon;
                    TextColor = FuTextStyle.Danger;
                    BGColor = FuButtonStyle.Danger;
                    break;

                case StateType.Success:
                    Icon = Fugui.Settings.SuccessIcon;
                    TextColor = FuTextStyle.Success;
                    BGColor = FuButtonStyle.Success;
                    break;

                case StateType.Info:
                    Icon = Fugui.Settings.InfoIcon;
                    TextColor = FuTextStyle.Info;
                    BGColor = FuButtonStyle.Info;
                    break;

                case StateType.Warning:
                    Icon = Fugui.Settings.WarningIcon;
                    TextColor = FuTextStyle.Warning;
                    BGColor = FuButtonStyle.Warning;
                    break;
            }
        }

        /// <summary>
        /// update the notificatipn duration and animation
        /// </summary>
        /// <param name="deltaTime">time that have passes since last draw frame</param>
        /// <returns>true if time is out, we need to remove this notification</returns>
        private bool update(float deltaTime)
        {
            // update animation state
            if (_removing)
            {
                if (_animationEnlapsed > 0f)
                {
                    _animationEnlapsed = Mathf.Clamp(_animationEnlapsed - deltaTime, 0f, Fugui.Settings.NotifyAnimlationDuration);
                }
                if (_animationEnlapsed == 0f)
                {
                    return true;
                }
            }
            else
            {
                if (_animationEnlapsed < Fugui.Settings.NotifyAnimlationDuration)
                {
                    _animationEnlapsed = Mathf.Clamp(_animationEnlapsed + deltaTime, 0f, Fugui.Settings.NotifyAnimlationDuration);
                }
                else
                {
                    // update animation duration once apear
                    Duration -= deltaTime;
                }
                if (Duration <= 0f)
                {
                    Close();
                }
            }
            return false;
        }

        /// <summary>
        /// Remove this notification from the list
        /// </summary>
        public void Close()
        {
            _removing = true;
            _animationEnlapsed = Fugui.Settings.NotifyAnimlationDuration;
        }

        /// <summary>
        /// Add a stacked notification to this one (an exacte same notify, let's increment quantity)
        /// </summary>
        /// <param name="duration">duration of the notification</param>
        public void AddStackedNotification(float duration)
        {
            Duration = duration;
            _animationEnlapsed = Fugui.Settings.NotifyAnimlationDuration;
            Quantity++;
        }

        /// <summary>
        /// Draw the notification
        /// </summary>
        /// <param name="parentDrawList">draw list of the notifyPanel window</param>
        /// <param name="i">index of this notification in the display list</param>
        /// <param name="deltaTime">time that have passes since last draw frame</param>
        /// <returns>whatever the notification must be removed from draw list</returns>
        public bool Draw(ImDrawListPtr parentDrawList, int i, float deltaTime)
        {
            bool toRemove = update(deltaTime);
            float width = Fugui.Settings.NotifyPanelWidth * Fugui.CurrentContext.Scale;
            float lineWidth = 8f * Fugui.CurrentContext.Scale;
            float borderWidth = 1f * Fugui.CurrentContext.Scale;
            width -= lineWidth;
            Vector2 panelPos = ImGui.GetCursorScreenPos();
            panelPos.x += lineWidth;
            Fugui.Push(ImGuiStyleVar.ChildRounding, 0f);
            // draw notify
            Vector2 fullSize = new Vector2(width, Height);
            Vector2 collapsedSize = _removing ? new Vector2(width, 1f) : new Vector2(32f, Height);
            Vector2 size = Vector2.Lerp(collapsedSize, fullSize, _animationEnlapsed / Fugui.Settings.NotifyAnimlationDuration);
            Fugui.MoveXUnscaled(lineWidth);
            if(ImGui.BeginChild("notificationPanel" + i, size, ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                // draw rect background
                drawList.AddRectFilled(panelPos, panelPos + size, ImGui.GetColorU32(FuThemeManager.GetColor(FuColors.ChildBg)));

                ImGui.Dummy(Vector2.zero);
                float cursorY = ImGui.GetCursorScreenPos().y;
                // draw title
                using (FuGrid grid = new FuGrid("notificationGrid" + i, new FuGridDefinition(3, new int[] { (int)(Fugui.Settings.NotifyIconSize ), -32 }), FuGridFlag.NoAutoLabels, outterPadding: 8f))
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
                    if (_animationEnlapsed == Fugui.Settings.NotifyAnimlationDuration)
                    {
                        Fugui.PushFont(Fugui.CurrentContext.DefaultFont.Size, FontType.Bold);
                        if (grid.ClickableText("X", FuTextStyle.Default))
                        {
                            Close();
                        }
                        Fugui.PopFont();
                    }
                }

                // draw message
                if (!string.IsNullOrEmpty(Message) && !string.IsNullOrEmpty(Title))
                {
                    ImGui.Dummy(Vector2.one * 8f * Fugui.CurrentContext.Scale);
                    ImGui.SameLine();
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 4f * Fugui.CurrentContext.Scale);
                    ImGui.TextWrapped(Message);
                    ImGui.Dummy(Vector2.one * 8f * Fugui.CurrentContext.Scale);
                }

                if (!_hasSpawn || _animationEnlapsed == Fugui.Settings.NotifyAnimlationDuration)
                {
                    Height = ImGui.GetCursorScreenPos().y - cursorY + 4f * Fugui.CurrentContext.Scale;
                }

                if (!_hasSpawn)
                {
                    _animationEnlapsed = 0f;
                    _hasSpawn = true;
                }

                // draw left line
                if (_removing)
                {
                    parentDrawList.AddLine(new Vector2(panelPos.x - lineWidth / 2f, panelPos.y), new Vector2(panelPos.x - lineWidth / 2f, panelPos.y + Mathf.Lerp(1f, Height, _animationEnlapsed / Fugui.Settings.NotifyAnimlationDuration)), ImGui.GetColorU32(BGColor.Button), lineWidth);
                }
                else
                {
                    parentDrawList.AddLine(new Vector2(panelPos.x - lineWidth / 2f, panelPos.y), new Vector2(panelPos.x - lineWidth / 2f, panelPos.y + Height), ImGui.GetColorU32(BGColor.Button), lineWidth);
                }
                // draw border rect
                drawList.AddRect(panelPos, (ImGui.GetCursorScreenPos() + ImGui.GetContentRegionAvail()), ImGui.GetColorU32(BGColor.Button), 0f, ImDrawFlags.None, borderWidth);
            }
            ImGuiNative.igEndChild();
            Fugui.PopStyle();
            Height = Mathf.Max(Height, Fugui.Settings.NotifyIconSize + 8f * Fugui.CurrentContext.Scale);
            return toRemove;
        }
    }
}