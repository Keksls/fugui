using System.Collections.Generic;
using Fugui.Core;
using ImGuiNET;
using UnityEngine;

namespace Fugui.Framework
{
    public enum NotificationType { Error, Success, Info, Warning }

    public class Notification
    {
        public string Title;
        public string Message;
        public NotificationType Type;
        public float Duration;
        public float Height;
        public Texture2D Icon;
        public UITextStyle TextColor;
        public UIButtonStyle BGColor;

        public Notification(string title, string message, NotificationType type, float duration)
        {
            Title = title;
            Message = message;
            Type = type;
            Duration = duration;
            Height = 64f;
            Icon = null;
            TextColor = UITextStyle.Default;
            BGColor = UIButtonStyle.Default;

            switch (type)
            {
                case NotificationType.Error:
                    Icon = FuGui.Settings.DangerIcon;
                    TextColor = UITextStyle.Danger;
                    BGColor = UIButtonStyle.Danger;
                    break;

                case NotificationType.Success:
                    Icon = FuGui.Settings.SuccessIcon;
                    TextColor = UITextStyle.Success;
                    BGColor = UIButtonStyle.Success;
                    break;

                case NotificationType.Info:
                    Icon = FuGui.Settings.InfoIcon;
                    TextColor = UITextStyle.Info;
                    BGColor = UIButtonStyle.Info;
                    break;

                case NotificationType.Warning:
                    Icon = FuGui.Settings.WarningIcon;
                    TextColor = UITextStyle.Warning;
                    BGColor = UIButtonStyle.Warning;
                    break;
            }
        }
    }

    public static class FuguiNotify
    {
        private static float _iconSize = 16f;
        private static float _panelWidth = 320f;
        private static List<Notification> _notifications = new List<Notification>();
        private static Vector2 _notificationPadding = new Vector2(8f, 8f);
        private static Vector2 _panelSize = new Vector2(_panelWidth, 256f);

        public static void Notify(string title, string message = null, NotificationType type = NotificationType.Info, float duration = 5f)
        {
            var notification = new Notification(title, message, type, duration);
            _notifications.Add(notification);
        }

        public static void RenderNotifications(IUIWindowContainer container)
        {
            if (_notifications.Count == 0) return;
            bool open = true;
            float deltaTime = ImGui.GetIO().DeltaTime;
            Vector2 panelPosition = getContainerPosition(container);
            ImGui.SetNextWindowSize(_panelSize, ImGuiCond.Always);
            ImGui.SetNextWindowPos(panelPosition, ImGuiCond.Always);
            FuGui.Push(ImGuiCol.WindowBg, Vector4.zero);
            FuGui.Push(ImGuiStyleVar.WindowBorderSize, 0f);
            ImGui.Begin("##NotificationContainer", ref open, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize);

            // draw background
            ImDrawListPtr drawList = ImGui.GetForegroundDrawList();

            float cursorPos = ImGui.GetCursorScreenPos().y;
            int removeIndex = -1;
            for (int i = 0; i < _notifications.Count; i++)
            {
                var notification = _notifications[i];
                notification.Duration -= deltaTime;
                if (notification.Duration <= 0f)
                {
                    removeIndex = i;
                }

                float lineWidth = 8f;
                Vector2 panelPos = ImGui.GetCursorScreenPos();
                FuGui.Push(ImGuiStyleVar.ChildRounding, 0f);
                // draw notify
                ImGui.BeginChild("notificationPanel" + i, new Vector2(0f, notification.Height), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
                {
                    ImGui.Dummy(Vector2.zero);
                    FuGui.PopColor();
                    float cursorY = ImGui.GetCursorScreenPos().y;
                    // draw title
                    using (UIGrid grid = new UIGrid("notificationGrid" + i, new UIGridDefinition(3, new int[] { (int)_iconSize + 8, (int)_panelWidth - (int)_iconSize - 82 }), UIGridFlag.NoAutoLabels, outterPadding: 8f))
                    {
                        grid.Image("notificationIcon" + i, notification.Icon, new Vector2(_iconSize, _iconSize), notification.TextColor.Text);
                        FuGui.PushFont(FuGui.CurrentContext.DefaultFont.Size, FontType.Bold);
                        grid.Text(notification.Title, notification.TextColor);
                        FuGui.PopFont();
                        if (grid.Button("x", UIButtonStyle.AutoSize, notification.BGColor))
                        {
                            removeIndex = i;
                        }
                    }

                    // draw message
                    if (!string.IsNullOrEmpty(notification.Message))
                    {
                        ImGui.Dummy(Vector2.one * 8f);
                        ImGui.SameLine();
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 4f);
                        using (UILayout grid = new UILayout())
                        {
                            grid.TextWrapped(notification.Message, notification.TextColor);
                        }
                    }

                    notification.Height = ImGui.GetCursorScreenPos().y - cursorY + 4f;
                }
                ImGui.EndChild();
                FuGui.PopStyle();
                notification.Height = Mathf.Max(notification.Height, _iconSize + 8f);

                // draw left line
                drawList.AddLine(new Vector2(panelPos.x - lineWidth / 2f, panelPos.y), new Vector2(panelPos.x - lineWidth / 2f, panelPos.y + notification.Height), ImGui.GetColorU32(notification.BGColor.Button), lineWidth);
                // draw border rect
                drawList.AddRect(panelPos, panelPos + ImGui.GetItemRectSize()   , ImGui.GetColorU32(notification.BGColor.Button), 0f, ImDrawFlags.None, 1f);
            }
            _panelSize = new Vector2(_panelWidth, ImGui.GetCursorScreenPos().y - cursorPos + 8f);
            // End the main container
            ImGui.End();
            FuGui.PopStyle();
            FuGui.PopColor();

            // remove ended notification
            if (removeIndex > -1)
            {
                _notifications.RemoveAt(removeIndex);
            }
        }

        private static Vector2 getContainerPosition(IUIWindowContainer container)
        {
            Vector2 localPosition = default;
            // Calculate the position of the widget based on the anchor point
            switch (FuGui.Settings.NotificationAnchorPosition)
            {
                case AnchorLocation.TopLeft:
                    localPosition = Vector2.zero; // position at top left corner
                    break;
                case AnchorLocation.TopCenter:
                    localPosition = new Vector2((container.Size.x - _panelSize.x - _notificationPadding.x) * 0.5f, _notificationPadding.y); // position at top center
                    break;
                case AnchorLocation.TopRight:
                    localPosition = new Vector2(container.Size.x - _panelSize.x - _notificationPadding.x, _notificationPadding.y); // position at top right corner
                    break;
                case AnchorLocation.MiddleLeft:
                    localPosition = new Vector2(_notificationPadding.x, (container.Size.y - _panelSize.y - _notificationPadding.y) * 0.5f); // position at middle left side
                    break;
                case AnchorLocation.MiddleCenter:
                    localPosition = new Vector2((container.Size.x - _panelSize.x - _notificationPadding.x) * 0.5f, (container.Size.y - _panelSize.y - _notificationPadding.y) * 0.5f); // position at middle center
                    break;
                case AnchorLocation.MiddleRight:
                    localPosition = new Vector2(container.Size.x - _panelSize.x - _notificationPadding.x, (container.Size.y - _panelSize.y - _notificationPadding.y) * 0.5f); // position at middle right side
                    break;
                case AnchorLocation.BottomLeft:
                    localPosition = new Vector2(_notificationPadding.x, container.Size.y - _panelSize.y - _notificationPadding.y); // position at bottom left corner
                    break;
                case AnchorLocation.BottomCenter:
                    localPosition = new Vector2((container.Size.x - _panelSize.x - _notificationPadding.x) * 0.5f, container.Size.y - _panelSize.y - _notificationPadding.y); // position at bottom center
                    break;
                case AnchorLocation.BottomRight:
                    localPosition = new Vector2(container.Size.x - _panelSize.x - _notificationPadding.x, container.Size.y - _panelSize.y - _notificationPadding.y); // position at bottom right corner
                    break;
            }
            return localPosition;
        }
    }
}