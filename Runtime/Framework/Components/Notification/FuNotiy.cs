using System.Collections.Generic;
using Fu.Core;
using Fu.Framework;
using ImGuiNET;
using UnityEngine;

namespace Fu
{
    public static partial class Fugui
    {
        private static bool _notifyPanelOpen = true;
        private static List<FuguiNotification> _notifications = new List<FuguiNotification>();
        private static Vector2 _notificationPadding = new Vector2(8f, 8f);
        private static Vector2 _panelSize = new Vector2(256f + 8f, 256f);
        private static bool _hasSpawn = false;

        /// <summary>
        /// Notify the user with a notification PopUp
        /// </summary>
        /// <param name="title">Title of the notification (can be null if message is not)</param>
        /// <param name="message">Message of the notification (can be null if title is not)</param>
        /// <param name="type">Type of the notification</param>
        /// <param name="duration">Duration before this notification disapear</param>
        public static void Notify(string title, string message = null, StateType type = StateType.Info, float duration = -1f)
        {
            // set default duration if needed
            if (duration <= 0f)
            {
                duration = Settings.NotificationDefaultDuration;
            }

            // do not add notif if already exists
            foreach (FuguiNotification notification in _notifications)
            {
                if (notification.Title == title && notification.Message == message && notification.Type == type)
                {
                    notification.AddStackedNotification(duration);
                    return;
                }
            }
            // check whatever title and message are null
            if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(message))
            {
                Debug.LogWarning("You are trying to notify the user with empty title and empty message. The Notification will not show.");
                return;
            }
            // add notification object to list
            _notifications.Add(new FuguiNotification(title, message, type, duration));
            _hasSpawn = false;
        }

        /// <summary>
        /// Draw the Notifications Panel into a given container
        /// This not mean that you don't need to call it inside the UIWindowContainer.
        /// Please call it in your UIWindowContainer after render any windows, and give the UIWindowContainer as parameter (used for anchor position)
        /// </summary>
        /// <param name="container">UIWindowContainer to draw notification panel in</param>
        public static void RenderNotifications(IFuWindowContainer container)
        {
            // do not do anything if there is nothing to draw
            if (_notifications.Count == 0)
            {
                _hasSpawn = false;
                return;
            }

            // this will store the index of the notification to remove from list if there is some
            int removeIndex = -1;
            // get last frame delta time
            float deltaTime = ImGui.GetIO().DeltaTime;
            // get the notifyPanel container relative position
            Vector2 panelPosition = getContainerPosition(container);
            // place the notifyPanel
            ImGui.SetNextWindowSize(_panelSize, ImGuiCond.Always);
            ImGui.SetNextWindowPos(panelPosition, ImGuiCond.Always);
            if (!_hasSpawn)
            {
                ImGui.SetNextWindowFocus();
                _hasSpawn = true;
            }
            // set style and color of the notifyPanel
            Push(ImGuiStyleVar.WindowPadding, Vector2.zero);
            Push(ImGuiCol.WindowBg, Vector4.zero);
            Push(ImGuiStyleVar.WindowBorderSize, 0f);
            // start drawing the notifyPanel
            ImGui.Begin("notifyPanel", ref _notifyPanelOpen, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize);
            {
                // get the notifyPanel draw list
                ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                // get the current position of the drawing cursor (to get computed height of the wole panel
                float cursorPos = ImGui.GetCursorScreenPos().y;
                // iterate on notifications list to draw theme
                for (int i = 0; i < _notifications.Count; i++)
                {
                    // draw the notification
                    if (_notifications[i].Draw(drawList, i, deltaTime))
                    {
                        // if notification need to be closed, let's keep it's index
                        removeIndex = i;
                    }
                }
                // calculate notifyPanel size according to the cursorPosition offset
                _panelSize = new Vector2(Settings.NotifyPanelWidth * container.Context.Scale, ImGui.GetCursorScreenPos().y - cursorPos + 8f);
            }
            // end drawing the notifyPanel
            ImGui.End();
            // pop styles and colors
            PopStyle(2);
            PopColor();

            // remove ended notification
            if (removeIndex > -1)
            {
                _notifications.RemoveAt(removeIndex);
            }
        }

        /// <summary>
        /// get the position of the notifyPanel according to it's Anchor
        /// </summary>
        /// <param name="container">UIWindowContainer within notifications must be drawn</param>
        /// <returns>the container's relative position of the notifyPanel</returns>
        private static Vector2 getContainerPosition(IFuWindowContainer container)
        {
            Vector2 localPosition = default;
            // Calculate the position of the widget based on the anchor point
            switch (Settings.NotificationAnchorPosition)
            {
                case FuOverlayAnchorLocation.TopLeft:
                    localPosition = Vector2.zero; // position at top left corner
                    break;
                case FuOverlayAnchorLocation.TopCenter:
                    localPosition = new Vector2((container.Size.x - _panelSize.x - _notificationPadding.x * container.Context.Scale) * 0.5f, _notificationPadding.y * container.Context.Scale); // position at top center
                    break;
                case FuOverlayAnchorLocation.TopRight:
                    localPosition = new Vector2(container.Size.x - _panelSize.x - _notificationPadding.x * container.Context.Scale, _notificationPadding.y * container.Context.Scale); // position at top right corner
                    break;
                case FuOverlayAnchorLocation.MiddleLeft:
                    localPosition = new Vector2(_notificationPadding.x * container.Context.Scale, (container.Size.y - _panelSize.y - _notificationPadding.y) * 0.5f); // position at middle left side
                    break;
                case FuOverlayAnchorLocation.MiddleCenter:
                    localPosition = new Vector2((container.Size.x - _panelSize.x - _notificationPadding.x * container.Context.Scale) * 0.5f, (container.Size.y - _panelSize.y - _notificationPadding.y * container.Context.Scale) * 0.5f); // position at middle center
                    break;
                case FuOverlayAnchorLocation.MiddleRight:
                    localPosition = new Vector2(container.Size.x - _panelSize.x - _notificationPadding.x, (container.Size.y - _panelSize.y - _notificationPadding.y * container.Context.Scale) * 0.5f); // position at middle right side
                    break;
                case FuOverlayAnchorLocation.BottomLeft:
                    localPosition = new Vector2(_notificationPadding.x * container.Context.Scale, container.Size.y - _panelSize.y - _notificationPadding.y * container.Context.Scale); // position at bottom left corner
                    break;
                case FuOverlayAnchorLocation.BottomCenter:
                    localPosition = new Vector2((container.Size.x - _panelSize.x - _notificationPadding.x * container.Context.Scale) * 0.5f, container.Size.y - _panelSize.y - _notificationPadding.y * container.Context.Scale); // position at bottom center
                    break;
                case FuOverlayAnchorLocation.BottomRight:
                    localPosition = new Vector2(container.Size.x - _panelSize.x - _notificationPadding.x * container.Context.Scale, container.Size.y - _panelSize.y - _notificationPadding.y * container.Context.Scale); // position at bottom right corner
                    break;
            }
            return localPosition;
        }
    }
}