using System;
using System.Collections.Generic;
using Fu;
using Fu.Framework;
using ImGuiNET;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Represents the Fugui type.
    /// </summary>
    public static partial class Fugui
    {
        #region State
        private static bool _notifyPanelOpen = true;
        private static readonly List<FuguiNotification> _notifications = new List<FuguiNotification>();
        private static readonly Dictionary<int, Rect> _notifyPanelRectsByContext = new Dictionary<int, Rect>();
        private static Vector2 _notificationPadding = new Vector2(8f, 8f);
        private static Vector2 _panelSize = new Vector2(256f + 8f, 256f);
        private static bool _hasSpawn = false;

        private const int MaxVisibleNotifications = 4;
        private const float CardSpacing = 8f;
        #endregion

        #region Methods
        /// <summary>Notify the user with a notification PopUp.</summary>
        public static void Notify(string title, string message = null, StateType type = StateType.Info, float duration = -1f, Action onClick = null, bool? startCollapsed = null)
        {
            if (duration <= 0f) duration = Settings.NotificationDefaultDuration;

            for (int i = 0; i < _notifications.Count; i++)
            {
                var n = _notifications[i];
                if (n.Title == title && n.Message == message && n.Type == type)
                {
                    n.AddStackedNotification(duration);
                    if (onClick != null) n.ClickCallback = onClick;
                    if (startCollapsed.HasValue) n.ForceCollapsed(startCollapsed.Value);
                    return;
                }
            }

            if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(message))
            {
                Debug.LogWarning("You are trying to notify the user with empty title and empty message. The Notification will not show.");
                return;
            }

            _notifications.Add(new FuguiNotification(title, message, type, duration, onClick, startCollapsed));
            _hasSpawn = false;
        }

        /// <summary>Draw the Notifications Panel into a given container.</summary>
        public static void RenderNotifications(IFuWindowContainer container)
        {
            if (_notifications.Count == 0)
            {
                _hasSpawn = false;
                _notifyPanelRectsByContext.Clear();
                return;
            }

            int removeIndex = -1;
            float deltaTime = ImGui.GetIO().DeltaTime;
            float panelWidth = getPanelWidth(container);
            _panelSize.x = panelWidth;
            Vector2 panelPosition = getContainerPosition(container);
            _notifyPanelRectsByContext[container.Context.ID] = new Rect(panelPosition, _panelSize);

            ImGui.SetNextWindowSize(_panelSize, ImGuiCond.Always);
            ImGui.SetNextWindowPos(panelPosition, ImGuiCond.Always);
            if (!_hasSpawn) { ImGui.SetNextWindowFocus(); _hasSpawn = true; }

            Push(ImGuiStyleVar.WindowPadding, Vector2.zero);
            Push(ImGuiCol.WindowBg, Vector4.zero);
            Push(ImGuiStyleVar.WindowBorderSize, 0f);

            ImGui.Begin("notifyPanel", ref _notifyPanelOpen, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize);
            {
                float startY = ImGui.GetCursorScreenPos().y;

                int visible = Math.Min(MaxVisibleNotifications, _notifications.Count);
                for (int i = 0; i < visible; i++)
                {
                    if (_notifications[i].Draw(ImGui.GetWindowDrawList(), i, deltaTime, panelWidth)) removeIndex = i;
                    ImGui.Dummy(new Vector2(1f, CardSpacing * container.Context.Scale));
                }

                int remaining = _notifications.Count - visible;
                if (remaining > 0)
                {
                    float radius = 10f * container.Context.Scale;
                    Vector2 chipSize = new Vector2(Mathf.Max(1f, Mathf.Min(panelWidth, 96f * container.Context.Scale)), 32f * container.Context.Scale);
                    Vector2 chipPos = ImGui.GetCursorScreenPos();
                    ImGui.BeginChild("notify_more_chip", chipSize, ImGuiChildFlags.AutoResizeY, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
                    {
                        var chipDL = ImGui.GetWindowDrawList();
                        uint bg = ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.FrameBg));
                        uint border = ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.Border));
                        chipDL.AddRectFilled(chipPos, chipPos + chipSize, bg, radius);
                        chipDL.AddRect(chipPos, chipPos + chipSize, border, radius);
                        Vector2 pad = new Vector2(10f * container.Context.Scale, 6f * container.Context.Scale);
                        ImGui.SetCursorScreenPos(chipPos + pad);
                        ImGui.Text(chipSize.x < 72f * container.Context.Scale ? $"+{remaining}" : $"+{remaining} more");
                    }
                    ImGui.EndChild();
                }

                _panelSize = new Vector2(panelWidth, ImGui.GetCursorScreenPos().y - startY + 8f);
                _notifyPanelRectsByContext[container.Context.ID] = new Rect(panelPosition, _panelSize);
            }
            ImGui.End();

            PopStyle(2);
            PopColor();

            if (removeIndex > -1) _notifications.RemoveAt(removeIndex);
        }

        /// <summary>Get whether a container-relative position is inside the notification panel.</summary>
        private static bool isInsideNotifyPanel(Vector2 worldPosition)
        {
            if (CurrentContext != null)
            {
                return _notifyPanelRectsByContext.TryGetValue(CurrentContext.ID, out Rect rect) && rect.Contains(worldPosition);
            }

            foreach (Rect rect in _notifyPanelRectsByContext.Values)
            {
                if (rect.Contains(worldPosition))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Get the notification panel width clamped to the available container width.</summary>
        private static float getPanelWidth(IFuWindowContainer container)
        {
            float scale = container.Context.Scale;
            float desiredWidth = Settings.NotifyPanelWidth * scale;
            float horizontalPadding = _notificationPadding.x * scale * 2f;
            float availableWidth = Mathf.Max(1f, container.Size.x - horizontalPadding);
            return Mathf.Min(desiredWidth, availableWidth);
        }

        /// <summary>Get the position of the notifyPanel according to its Anchor.</summary>
        private static Vector2 getContainerPosition(IFuWindowContainer container)
        {
            Vector2 localPosition = default;
            switch (Settings.NotificationAnchorPosition)
            {
                case FuOverlayAnchorLocation.TopLeft: localPosition = Vector2.zero; break;
                case FuOverlayAnchorLocation.TopCenter: localPosition = new Vector2((container.Size.x - _panelSize.x - _notificationPadding.x * container.Context.Scale) * 0.5f, _notificationPadding.y * container.Context.Scale); break;
                case FuOverlayAnchorLocation.TopRight: localPosition = new Vector2(container.Size.x - _panelSize.x - _notificationPadding.x * container.Context.Scale, _notificationPadding.y * container.Context.Scale); break;
                case FuOverlayAnchorLocation.MiddleLeft: localPosition = new Vector2(_notificationPadding.x * container.Context.Scale, (container.Size.y - _panelSize.y - _notificationPadding.y) * 0.5f); break;
                case FuOverlayAnchorLocation.MiddleCenter: localPosition = new Vector2((container.Size.x - _panelSize.x - _notificationPadding.x * container.Context.Scale) * 0.5f, (container.Size.y - _panelSize.y - _notificationPadding.y * container.Context.Scale) * 0.5f); break;
                case FuOverlayAnchorLocation.MiddleRight: localPosition = new Vector2(container.Size.x - _panelSize.x - _notificationPadding.x, (container.Size.y - _panelSize.y - _notificationPadding.y * container.Context.Scale) * 0.5f); break;
                case FuOverlayAnchorLocation.BottomLeft: localPosition = new Vector2(_notificationPadding.x * container.Context.Scale, container.Size.y - _panelSize.y - _notificationPadding.y * container.Context.Scale); break;
                case FuOverlayAnchorLocation.BottomCenter: localPosition = new Vector2((container.Size.x - _panelSize.x - _notificationPadding.x * container.Context.Scale) * 0.5f, container.Size.y - _panelSize.y - _notificationPadding.y * container.Context.Scale); break;
                case FuOverlayAnchorLocation.BottomRight: localPosition = new Vector2(container.Size.x - _panelSize.x - _notificationPadding.x * container.Context.Scale, container.Size.y - _panelSize.y - _notificationPadding.y * container.Context.Scale); break;
            }
            localPosition.x = Mathf.Clamp(localPosition.x, 0f, Mathf.Max(0f, container.Size.x - _panelSize.x));
            localPosition.y = Mathf.Clamp(localPosition.y, 0f, Mathf.Max(0f, container.Size.y - _panelSize.y));
            return localPosition;
        }
        #endregion
    }
}
