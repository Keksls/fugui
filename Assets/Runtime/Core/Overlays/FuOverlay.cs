using Fu.Framework;
using ImGuiNET;
using System;
using UnityEngine;

namespace Fu.Core
{
    public class FuOverlay
    {
        #region Variables
        // ID of the UI window
        public string ID { get; private set; }
        // Size of the UI window
        public Vector2 Size { get; set; }
        // Offset of the anchor point from the top-left corner of the window
        public Vector2 AnchorOffset { get; set; }
        // Custom UI display function for the window
        public Action<FuOverlay> UI { get; private set; }
        // Public variable that store local Rect of this overlay
        public Rect LocalRect { get; private set; }
        // Public variable for the UIWindow instance
        public FuWindow UIWindow { get; private set; }

        // Flag to indicate if the window is collapsible
        private bool _collapsable;
        // Flag to indicate if the window is draggable
        private bool _draggable;
        // Flag to indicate if the window has a background
        private bool _noBackground;
        // Flag to indicate if the window has a background
        private bool _noEditAnchor;
        // Private variable for the anchor location of the window
        private AnchorLocation _anchorLocation;
        // Private variable to track the collapsed state of the window
        private bool _collapsed;

        // Private variable to track the hover state of the drag button
        private bool _dragButtonHovered = false;
        // Private variable to track the dragging state of the window
        private bool _draging = false;
        // Private variable to track the mouse position during a drag operation
        private Vector2 _dragMousePosition = Vector2.zero;
        // Private variable to track the offset of the window during a drag operation
        private Vector2 _dragOffset = Vector2.zero;
        // Private variable to track whether to draw the snap grid
        private bool _drawSnapGrid = false;
        // Private variable that reprsant the position of the drag button
        private FuOverlayDragPosition _dragButtonPosition;
        // Private constant for the width of the collapse button
        private float _retractButtonWidth = 12f;
        // Private constant for the drag step size
        private float _dragStep = 32f;
        // Private constant for the color of the snap grid
        private Vector4 _gridColor = new Vector4(.1f, .1f, .1f, .25f);
        // Private constant for the width of the snap grid lines
        private float _gridWidth = 1f;
        private AnchorLocation _defaultAnchorLocation;
        private Vector2 _defaultAnchorOffset;
        #endregion

        /// <summary>
        /// Instantiate an UI Overlay object
        /// </summary>
        /// <param name="id">unique ID of this overlay</param>
        /// <param name="size">size of this overlay</param>
        /// <param name="ui">UI of this overlay</param>
        /// <param name="flags">Overlay comportement flags</param>
        public FuOverlay(string id, Vector2 size, Action<FuOverlay> ui, FuOverlayFlags flags = FuOverlayFlags.Default, FuOverlayDragPosition dragButtonPosition = FuOverlayDragPosition.Auto)
        {
            // Set the ID of the window
            ID = id;
            // Set the size of the window
            Size = size;
            // Set the UI display function
            UI = ui;
            // Set the default anchor offset
            AnchorOffset = new Vector2(8f, 8f);
            // Set the initial collapsed state to false
            _collapsed = false;

            // Set the collapsable behavior based on the provided parameter
            _collapsable = !flags.HasFlag(FuOverlayFlags.NoClose);
            // Set the draggable behavior based on the provided parameter
            _draggable = !flags.HasFlag(FuOverlayFlags.NoMove);
            // Set the draggable behavior based on the provided parameter
            _noBackground = flags.HasFlag(FuOverlayFlags.NoBackground);
            // Set the flag that allow Anchor Position to be changed
            _noEditAnchor = flags.HasFlag(FuOverlayFlags.NoEditAnchor);
            // Set the position of the drag button
            _dragButtonPosition = dragButtonPosition;
            // if not collapsable and not draggable, we will hide drag button, let's set drag button size to 0 to avoid useless offset
            if (!_collapsable && !_draggable)
            {
                _retractButtonWidth = 0f;
            }

            // set default AnchorLocation and Offset
            _defaultAnchorLocation = AnchorLocation.TopLeft;
            _defaultAnchorOffset = Vector2.zero;
        }

        /// <summary>
        /// Anchor this overlay to a WindoDefinition. 
        /// Once the according winDef will create a UIWindow, Anchor will be added to UIWindow
        /// </summary>
        /// <param name="window">window definition to add overlay</param>
        /// <param name="anchor">location anchor to anchor the overlay</param>
        /// <param name="anchorOffset">offset position of the overlay according to it location anchor</param>
        /// <returns>true if added</returns>
        public bool AnchorWindowDefinition(FuWindowDefinition window, AnchorLocation anchor, Vector2 anchorOffset)
        {
            if (!window.AddOverlay(this))
            {
                return false;
            }
            _defaultAnchorLocation = anchor;
            _defaultAnchorOffset = anchorOffset;
            return window.AddOverlay(this);
        }

        /// <summary>
        /// anchor this overlay to a UIWIndow using default Anchor Location and Offset
        /// </summary>
        /// <param name="window">Window to anchor this overlay on</param>
        /// <returns>tue if overlay added</returns>
        public bool AnchorWindow(FuWindow window)
        {
            return AnchorWindow(window, _defaultAnchorLocation, _defaultAnchorOffset);
        }

        /// <summary>
        /// Anchor this overlay into an UIWindow
        /// </summary>
        /// <param name="window">window to anchor overlay on</param>
        /// <param name="anchor">location to anchor this overlay</param>
        /// <param name="anchorOffset">anchor position offset</param>
        /// <returns>true if success</returns>
        public bool AnchorWindow(FuWindow window, AnchorLocation anchor, Vector2 anchorOffset)
        {
            // try add container to the UIWIndow
            if (!window.AddOverlay(this))
            {
                return false;
            }

            // reset default state values
            _drawSnapGrid = false;
            _collapsed = false;
            _dragButtonHovered = false;
            _draging = false;
            _dragMousePosition = Vector2.zero;
            _dragOffset = Vector2.zero;

            // set anchored window
            UIWindow = window;

            // set drag button position if set on Auto
            if (_dragButtonPosition == FuOverlayDragPosition.Auto)
            {
                switch (anchor)
                {
                    case AnchorLocation.TopCenter:
                        _dragButtonPosition = FuOverlayDragPosition.Top;
                        break;

                    case AnchorLocation.TopLeft:
                    case AnchorLocation.MiddleLeft:
                    case AnchorLocation.MiddleCenter:
                    case AnchorLocation.BottomLeft:
                        _dragButtonPosition = FuOverlayDragPosition.Left;
                        break;

                    case AnchorLocation.MiddleRight:
                    case AnchorLocation.BottomRight:
                    case AnchorLocation.TopRight:
                        _dragButtonPosition = FuOverlayDragPosition.Right;
                        break;

                    case AnchorLocation.BottomCenter:
                        _dragButtonPosition = FuOverlayDragPosition.Bottom;
                        break;
                }
            }

            // set anchor location
            _anchorLocation = anchor;
            // snap anchored offset
            AnchorOffset = new Vector2((float)Math.Floor(anchorOffset.x / _dragStep) * _dragStep, (float)Math.Floor(anchorOffset.y / _dragStep) * _dragStep);
            return true;
        }

        /// <summary>
        /// Draw the overlay, will be call by anchored window
        /// </summary>
        internal void Draw()
        {
            // stop dragging if mouse release
            if (!ImGui.GetIO().MouseDown[0])
            {
                _draging = false;
                _drawSnapGrid = false;
            }

            // get anchored container position
            Vector2 unsnappedDragPosition = Vector2.zero;
            Vector2 screenPos = getAnchoredPosition(ref unsnappedDragPosition);

            // set overlay local rect
            LocalRect = new Rect(screenPos - UIWindow.LocalPosition, new Vector2(Size.x + _retractButtonWidth, Size.y));

            // if we are dragging, draw unsnapped drag ghost and snap grid
            if (_draging)
            {
                // set mouse cursor
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                if (_drawSnapGrid)
                {
                    // draw drag snapping grid
                    DrawGrid(ImGui.GetWindowDrawList());
                }
                // set unsnapped position
                ImGui.SetCursorScreenPos(unsnappedDragPosition);
                Fugui.Push(ImGuiCol.ChildBg, new Vector4(0.1f, 0.1f, 0.1f, 0.25f));
                ImGui.BeginChild(ID + "draginGhost", _collapsed ? new Vector2(12f, Size.y) : new Vector2(12f + Size.x, Size.y));
                ImGui.EndChild();
                Fugui.PopColor();
            }

            // force child to have no rounding
            Fugui.Push(ImGuiStyleVar.ChildRounding, 0f);

            // draw drag button
            if (_collapsable || _draggable)
            {
                FuButtonStyle.Default.Push(true);
                drawDragButton(screenPos);
                FuButtonStyle.Default.Pop();
            }

            // draw overlay UI
            if (!_collapsed)
            {
                switch (_dragButtonPosition)
                {
                    case FuOverlayDragPosition.Top:
                        ImGui.SetCursorScreenPos(new Vector2(screenPos.x, screenPos.y + _retractButtonWidth));
                        break;

                    case FuOverlayDragPosition.Right:
                        ImGui.SetCursorScreenPos(screenPos);
                        break;

                    case FuOverlayDragPosition.Bottom:
                        ImGui.SetCursorScreenPos(new Vector2(screenPos.x, screenPos.y - _retractButtonWidth));
                        break;

                    default:
                    case FuOverlayDragPosition.Left:
                        ImGui.SetCursorScreenPos(new Vector2(screenPos.x + _retractButtonWidth, screenPos.y));
                        break;
                }
                if (_noBackground)
                {
                    FuStyle.NoBackgroundOverlay.Push(true);
                }
                else
                {
                    FuStyle.Overlay.Push(true);
                }
                ImGui.BeginChild(ID, Size, true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
                {
                    UI?.Invoke(this);
                }
                FuStyle.Overlay.Pop();
                ImGui.EndChild();
            }
            Fugui.PopStyle();
        }

        private void drawDragButton(Vector2 screenPos)
        {
            // set draggingColor
            if (_draging)
            {
                Fugui.Push(ImGuiCol.ChildBg, ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive]);
            }
            // set hovered color
            else if (_dragButtonHovered)
            {
                Fugui.Push(ImGuiCol.ChildBg, ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonHovered]);
            }
            // set default color
            else
            {
                Fugui.Push(ImGuiCol.ChildBg, ImGui.GetStyle().Colors[(int)ImGuiCol.Button]);
            }

            // get retract button position
            Vector2 retractPos = screenPos;
            Vector2 retractButtonSize = default;

            switch (_dragButtonPosition)
            {
                case FuOverlayDragPosition.Top:
                    retractPos = screenPos;
                    retractButtonSize = new Vector2(Size.x, _retractButtonWidth);
                    break;

                case FuOverlayDragPosition.Right:
                    retractPos = new Vector2(screenPos.x + Size.x, screenPos.y);
                    retractButtonSize = new Vector2(_retractButtonWidth, Size.y);
                    break;

                case FuOverlayDragPosition.Bottom:
                    retractPos = new Vector2(screenPos.x, screenPos.y + Size.y - _retractButtonWidth);
                    retractButtonSize = new Vector2(Size.x, _retractButtonWidth);
                    break;

                default:
                case FuOverlayDragPosition.Left:
                    retractPos = screenPos;
                    retractButtonSize = new Vector2(_retractButtonWidth, Size.y);
                    break;
            }

            // draw retract button
            ImGui.SetCursorScreenPos(retractPos);
            ImGui.BeginChild(ID + "collapsable", retractButtonSize);
            ImGui.EndChild();
            Fugui.PopColor();
            // whatever the retract button is retracted
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.ChildWindows))
            {
                _dragButtonHovered = true;
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) && _draggable)
                {
                    _draging = true;
                    _dragMousePosition = Fugui.WorldMousePosition - _dragOffset;
                }
                // will show / hide overlay on double click
                if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    _collapsed = !_collapsed;
                }
            }
            else
            {
                _dragButtonHovered = false;
            }
        }

        /// <summary>
        /// get overlay anchor position relative to it anchorLocation and position
        /// </summary>
        /// <returns>container relative position</returns>
        private Vector2 getAnchoredPosition(ref Vector2 unsnappedDragPosition)
        {
            Vector2 localPosition = default;

            // Calculate the position of the widget based on the anchor point
            switch (_anchorLocation)
            {
                case AnchorLocation.TopLeft:
                    localPosition = AnchorOffset; // position at top left corner
                    break;
                case AnchorLocation.TopCenter:
                    localPosition = new Vector2((UIWindow.WorkingAreaSize.x - Size.x - AnchorOffset.x) * 0.5f, AnchorOffset.y); // position at top center
                    break;
                case AnchorLocation.TopRight:
                    localPosition = new Vector2(UIWindow.WorkingAreaSize.x - Size.x - AnchorOffset.x, AnchorOffset.y); // position at top right corner
                    break;
                case AnchorLocation.MiddleLeft:
                    localPosition = new Vector2(AnchorOffset.x, (UIWindow.WorkingAreaSize.y - Size.y - AnchorOffset.y) * 0.5f); // position at middle left side
                    break;
                case AnchorLocation.MiddleCenter:
                    localPosition = new Vector2((UIWindow.WorkingAreaSize.x - Size.x - AnchorOffset.x) * 0.5f, (UIWindow.WorkingAreaSize.y - Size.y - AnchorOffset.y) * 0.5f); // position at middle center
                    break;
                case AnchorLocation.MiddleRight:
                    localPosition = new Vector2(UIWindow.WorkingAreaSize.x - Size.x - AnchorOffset.x, (UIWindow.WorkingAreaSize.y - Size.y - AnchorOffset.y) * 0.5f); // position at middle right side
                    break;
                case AnchorLocation.BottomLeft:
                    localPosition = new Vector2(AnchorOffset.x, UIWindow.WorkingAreaSize.y - Size.y - AnchorOffset.y); // position at bottom left corner
                    break;
                case AnchorLocation.BottomCenter:
                    localPosition = new Vector2((UIWindow.WorkingAreaSize.x - Size.x - AnchorOffset.x) * 0.5f, UIWindow.WorkingAreaSize.y - Size.y - AnchorOffset.y); // position at bottom center
                    break;
                case AnchorLocation.BottomRight:
                    localPosition = new Vector2(UIWindow.WorkingAreaSize.x - Size.x - AnchorOffset.x, UIWindow.WorkingAreaSize.y - Size.y - AnchorOffset.y); // position at bottom right corner
                    break;
            }

            // handle drag offset
            if (_draging)
            {
                _dragOffset = Fugui.WorldMousePosition - _dragMousePosition;
                // draw snap grid only if we start to drag (avoid draw grid on double click)
                if (!_drawSnapGrid && Math.Abs(_dragOffset.x) + Math.Abs(_dragOffset.y) > 4)
                {
                    _drawSnapGrid = true;
                }
                // store not snapped drag offset
                unsnappedDragPosition = localPosition + _dragOffset;
                // clamp unsnapped drag position
                unsnappedDragPosition = clampPosition(unsnappedDragPosition) + UIWindow.LocalPosition + UIWindow.WorkingAreaPosition;
                // snap drag offset to grid
                _dragOffset.x = (float)Math.Floor(_dragOffset.x / _dragStep) * _dragStep;
                _dragOffset.y = (float)Math.Floor(_dragOffset.y / _dragStep) * _dragStep;
            }
            // add dragOffset to local position
            localPosition += _dragOffset;

            // clamp position
            localPosition = clampPosition(localPosition);

            // return container relative position
            return localPosition + UIWindow.LocalPosition + UIWindow.WorkingAreaPosition;
        }

        /// <summary>
        /// clamp position to stay on window working area
        /// </summary>
        /// <param name="localPosition">position to clamp</param>
        /// <returns>clamped position</returns>
        private Vector2 clampPosition(Vector2 localPosition)
        {
            if (localPosition.x + Size.x + 8f + _retractButtonWidth > UIWindow.WorkingAreaSize.x)
                localPosition.x = UIWindow.WorkingAreaSize.x - Size.x - 8f - _retractButtonWidth;
            if (localPosition.y + Size.y + 8f > UIWindow.WorkingAreaSize.y)
                localPosition.y = UIWindow.WorkingAreaSize.y - Size.y - 8f;
            if (localPosition.x < 8f)
                localPosition.x = 8f;
            if (localPosition.y < 8f)
                localPosition.y = 8f;
            return localPosition;
        }

        /// <summary>
        /// Draw snap grid
        /// </summary>
        /// <param name="drawList">ImGui draw list to draw snap grid on</param>
        private void DrawGrid(ImDrawListPtr drawList)
        {
            // Convert the grid color to a 32-bit integer color
            uint color = ImGui.GetColorU32(_gridColor);

            // Calculate the number of columns and rows in the snap grid
            int cols = (int)(UIWindow.WorkingAreaSize.x / _dragStep) + 1;
            int rows = (int)(UIWindow.WorkingAreaSize.y / _dragStep) + 1;

            // Calculate the starting position of the snap grid
            Vector2 startPos = UIWindow.LocalPosition + UIWindow.WorkingAreaPosition;

            // TODO : add offset according to drag button position
            //if (!_collapseButtonLeft)
            //{
            //    startPos += new Vector2(_dragStep / 2f, 0f);
            //}

            // Draw the vertical lines of the snap grid
            for (int x = 0; x < cols; x++)
            {
                // Calculate the start and end points of the line
                Vector2 p1 = new Vector2(x * _dragStep, -_dragStep);
                Vector2 p2 = new Vector2(x * _dragStep, UIWindow.WorkingAreaSize.y + _dragStep);
                // Add the line to the draw list
                drawList.AddLine(p1 + startPos, p2 + startPos, color, _gridWidth);
            }

            // Draw the horizontal lines of the snap grid
            for (int y = 0; y < rows; y++)
            {
                // Calculate the start and end points of the line
                Vector2 p1 = new Vector2(-_dragStep, y * _dragStep);
                Vector2 p2 = new Vector2(UIWindow.WorkingAreaSize.x + _dragStep, y * _dragStep);
                // Add the line to the draw list
                drawList.AddLine(p1 + startPos, p2 + startPos, color, _gridWidth);
            }
        }
    }

    // Define an enum to represent the different anchor points
    public enum AnchorLocation
    {
        TopLeft,      // anchor to top left corner
        TopCenter,    // anchor to top center
        TopRight,     // anchor to top right corner
        MiddleLeft,   // anchor to middle left side
        MiddleCenter, // anchor to middle center
        MiddleRight,  // anchor to middle right side
        BottomLeft,   // anchor to bottom left corner
        BottomCenter, // anchor to bottom center
        BottomRight   // anchor to bottom right corner
    }
}