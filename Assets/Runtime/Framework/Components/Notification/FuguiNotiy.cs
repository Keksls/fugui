﻿using System.Collections.Generic;
using Fugui.Core;
using ImGuiNET;
using UnityEngine;

namespace Fugui.Framework
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
        internal UITextStyle TextColor;
        internal UIButtonStyle BGColor;
        private const float NOTIFICATION_ANIMATION_DURATION = 0.15f;
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
            TextColor = UITextStyle.Default;
            BGColor = UIButtonStyle.Default;
            _animationEnlapsed = NOTIFICATION_ANIMATION_DURATION;

            switch (type)
            {
                case StateType.Danger:
                    Icon = FuGui.Settings.DangerIcon;
                    TextColor = UITextStyle.Danger;
                    BGColor = UIButtonStyle.Danger;
                    break;

                case StateType.Success:
                    Icon = FuGui.Settings.SuccessIcon;
                    TextColor = UITextStyle.Success;
                    BGColor = UIButtonStyle.Success;
                    break;

                case StateType.Info:
                    Icon = FuGui.Settings.InfoIcon;
                    TextColor = UITextStyle.Info;
                    BGColor = UIButtonStyle.Info;
                    break;

                case StateType.Warning:
                    Icon = FuGui.Settings.WarningIcon;
                    TextColor = UITextStyle.Warning;
                    BGColor = UIButtonStyle.Warning;
                    break;
            }
        }

        /// <summary>
        /// update the notificatipn duration and animation
        /// </summary>
        /// <param name="deltaTime"></param>
        /// <returns></returns>
        private bool update(float deltaTime)
        {
            // update animation state
            if (_removing)
            {
                if (_animationEnlapsed > 0f)
                {
                    _animationEnlapsed = Mathf.Clamp(_animationEnlapsed - deltaTime, 0f, NOTIFICATION_ANIMATION_DURATION);
                }
                if (_animationEnlapsed == 0f)
                {
                    return true;
                }
            }
            else
            {
                if (_animationEnlapsed < NOTIFICATION_ANIMATION_DURATION)
                {
                    _animationEnlapsed = Mathf.Clamp(_animationEnlapsed + deltaTime, 0f, NOTIFICATION_ANIMATION_DURATION);
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
            _animationEnlapsed = NOTIFICATION_ANIMATION_DURATION;
        }

        /// <summary>
        /// Draw the notification
        /// </summary>
        /// <param name="drawList">draw list of the notifyPanel window</param>
        /// <param name="i">index of this notification in the display list</param>
        /// <returns>whatever the notification must be removed from draw list</returns>
        public bool Draw(ImDrawListPtr drawList, int i, float deltaTime)
        {
            bool toRemove = update(deltaTime);

            float lineWidth = 8f;
            Vector2 panelPos = ImGui.GetCursorScreenPos();
            FuGui.Push(ImGuiStyleVar.ChildRounding, 0f);
            // draw notify
            Vector2 fullSize = new Vector2(FuGui.Settings.NotifyPanelWidth, Height);
            Vector2 collapsedSize = _removing ? new Vector2(FuGui.Settings.NotifyPanelWidth, 1f) : new Vector2(32f, Height);
            ImGui.BeginChild("notificationPanel" + i, Vector2.Lerp(collapsedSize, fullSize, _animationEnlapsed / NOTIFICATION_ANIMATION_DURATION), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
            {
                ImGui.Dummy(Vector2.zero);
                float cursorY = ImGui.GetCursorScreenPos().y;
                // draw title
                using (UIGrid grid = new UIGrid("notificationGrid" + i, new UIGridDefinition(3, new int[] { (int)FuGui.Settings.NotifyIconSize + 8, (int)FuGui.Settings.NotifyPanelWidth - (int)FuGui.Settings.NotifyIconSize - 82 }), UIGridFlag.NoAutoLabels, outterPadding: 8f))
                {
                    grid.Image("notificationIcon" + i, Icon, new Vector2(FuGui.Settings.NotifyIconSize, FuGui.Settings.NotifyIconSize), TextColor.Text);
                    
                    if(string.IsNullOrEmpty(Title))
                    {
                        grid.TextWrapped(Message, TextColor);
                    }
                    else
                    {
                        FuGui.PushFont(FuGui.CurrentContext.DefaultFont.Size, FontType.Bold);
                        grid.Text(Title, TextColor);
                        FuGui.PopFont();
                    }
                    if (_animationEnlapsed == NOTIFICATION_ANIMATION_DURATION)
                    {
                        if (grid.Button("x", UIButtonStyle.AutoSize, BGColor))
                        {
                            Close();
                        }
                    }
                }

                // draw message
                if (!string.IsNullOrEmpty(Message) && !string.IsNullOrEmpty(Title))
                {
                    ImGui.Dummy(Vector2.one * 8f);
                    ImGui.SameLine();
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 4f);
                    using (UILayout grid = new UILayout())
                    {
                        grid.TextWrapped(Message, TextColor);
                    }
                }

                if (!_hasSpawn || _animationEnlapsed == NOTIFICATION_ANIMATION_DURATION)
                {
                    Height = ImGui.GetCursorScreenPos().y - cursorY + 4f;
                }

                if (!_hasSpawn)
                {
                    _animationEnlapsed = 0f;
                    _hasSpawn = true;
                }
            }
            ImGui.EndChild();
            FuGui.PopStyle();
            Height = Mathf.Max(Height, FuGui.Settings.NotifyIconSize + 8f);

            // draw left line
            if (_removing)
            {
                drawList.AddLine(new Vector2(panelPos.x - lineWidth / 2f, panelPos.y), new Vector2(panelPos.x - lineWidth / 2f, panelPos.y + Mathf.Lerp(1f, Height, _animationEnlapsed / NOTIFICATION_ANIMATION_DURATION)), ImGui.GetColorU32(BGColor.Button), lineWidth);
            }
            else
            {
                drawList.AddLine(new Vector2(panelPos.x - lineWidth / 2f, panelPos.y), new Vector2(panelPos.x - lineWidth / 2f, panelPos.y + Height), ImGui.GetColorU32(BGColor.Button), lineWidth);
            }
            // draw border rect
            drawList.AddRect(panelPos, panelPos + ImGui.GetItemRectSize(), ImGui.GetColorU32(BGColor.Button), 0f, ImDrawFlags.None, 1f);

            return toRemove;
        }
    }

    public static partial class FuGui
    {
        private static bool _notifyPanelOpen = true;
        private static List<FuguiNotification> _notifications = new List<FuguiNotification>();
        private static Vector2 _notificationPadding = new Vector2(8f, 8f);
        private static Vector2 _panelSize = new Vector2(256f, 256f);

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
            // check whatever title and message are null
            if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(message))
            {
                Debug.LogWarning("You are trying to notify the user with empty title and empty message. The Notification will not show.");
                return;
            }
            // add notification object to list
            _notifications.Add(new FuguiNotification(title, message, type, duration));
        }

        /// <summary>
        /// Draw the Notifications Panel into a given container
        /// This not mean that you don't need to call it inside the UIWindowContainer.
        /// Please call it in your UIWindowContainer after render any windows, and give the UIWindowContainer as parameter (used for anchor position)
        /// </summary>
        /// <param name="container">UIWindowContainer to draw notification panel in</param>
        public static void RenderNotifications(IUIWindowContainer container)
        {
            // do not do anything if there is nothing to draw
            if (_notifications.Count == 0)
            {
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
            // set style and color of the notifyPanel
            Push(ImGuiStyleVar.WindowPadding, Vector2.zero);
            Push(ImGuiCol.WindowBg, Vector4.zero);
            Push(ImGuiStyleVar.WindowBorderSize, 0f);
            // start drawing the notifyPanel
            ImGui.Begin("notifyPanel", ref _notifyPanelOpen, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize);
            {
                // get the notifyPanel draw list
                ImDrawListPtr drawList = ImGui.GetForegroundDrawList();
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
                _panelSize = new Vector2(Settings.NotifyPanelWidth, ImGui.GetCursorScreenPos().y - cursorPos + 8f);
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
        private static Vector2 getContainerPosition(IUIWindowContainer container)
        {
            Vector2 localPosition = default;
            // Calculate the position of the widget based on the anchor point
            switch (Settings.NotificationAnchorPosition)
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