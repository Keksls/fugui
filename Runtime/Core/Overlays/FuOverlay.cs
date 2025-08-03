using Fu.Framework;
using ImGuiNET;
using System;
using UnityEngine;

namespace Fu.Core
{
    public class FuOverlay
    {
        #region Variables
        // ID of the Overlay
        public string ID { get; private set; }
        // unscaled private size of  the Overlay
        private Vector2Int _size;
        // unscaled public size of  the Overlay
        public Vector2Int UnscaledSize => _size;
        // Size of the Overlay
        public Vector2Int Size
        {
            get
            {
                return new Vector2Int((int)(_size.x * Fugui.CurrentContext.Scale), (int)(_size.y * Fugui.CurrentContext.Scale));
            }
            set
            {
                _size = value;
            }
        }
        // unscaled private Offset of the anchor point from the top-left corner of the overlay
        private Vector2Int _anchorOffset;
        // Offset of the anchor point from the top-left corner of the overlay
        public Vector2Int AnchorOffset
        {
            get
            {
                return new Vector2Int((int)(_anchorOffset.x * Fugui.CurrentContext.Scale), (int)(_anchorOffset.y * Fugui.CurrentContext.Scale));
            }
            set
            {
                _anchorOffset = value;
            }
        }
        // Custom UI display function for the overlay
        public Action<FuOverlay> UI { get; private set; }
        // Whenever the Overlay will render just right now
        public event Action OnPreRender;
        // Whenever the Overlay just render right now
        public event Action OnPostRender;
        // Public variable that store local Rect of this overlay
        public Rect LocalRect { get; private set; }
        // Public variable that store local Rect of this overlay
        public Rect WorldRect { get => new Rect(LocalRect.x + Window.LocalRect.x, LocalRect.y + Window.LocalRect.y, LocalRect.width, LocalRect.height); }
        // Public variable for the UIWindow instance
        public FuWindow Window { get; private set; }
        // Minimum Size of the UI window to display this overlay
        public Vector2Int MinimumWindowSize { get; private set; }

        // Flag to indicate if the window is collapsible
        private bool _collapsable;
        // Flag to indicate if the window is draggable
        private bool _draggable;
        // Flag to indicate if the window has a background
        private bool _noBackground;
        // Flag to indicate if the window has a background
        private bool _noEditAnchor;
        // Private variable for the anchor location of the window
        private FuOverlayAnchorLocation _anchorLocation;
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
        // the default location of the overlay anchor
        private FuOverlayAnchorLocation _defaultAnchorLocation;
        // the default offset of the overlay aanchor
        private Vector2Int _defaultAnchorOffset;
        // the style of the overlay panel
        private FuStyle _overlayStyle;
        // var to count how many push are at frame start, so we can pop missing push
        private static int _nbColorPushOnFrameStart = 0;
        private static int _nbStylePushOnFrameStart = 0;
        private static int _nbFontPushOnFrameStart = 0;
        // whatever the overlay need to be drawn
        private bool _isVisible = true;
        #endregion

        /// <summary>
        /// Instantiate an UI Overlay object
        /// </summary>
        /// <param name="id">unique ID of this overlay</param>
        /// <param name="size">size of this overlay</param>
        /// <param name="ui">UI of this overlay</param>
        /// <param name="flags">Overlay comportement flags</param>
        public FuOverlay(string id, Vector2Int size, Action<FuOverlay> ui, FuOverlayFlags flags = FuOverlayFlags.Default, FuOverlayDragPosition dragButtonPosition = FuOverlayDragPosition.Auto)
        {
            // Set the ID of the window
            ID = id;
            // Set the size of the window
            _size = size;
            // Set the UI display function
            UI = ui;
            // Set the default anchor offset
            AnchorOffset = new Vector2Int(8, 8);
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
            _defaultAnchorLocation = FuOverlayAnchorLocation.TopLeft;
            _defaultAnchorOffset = Vector2Int.zero;
            _overlayStyle = FuStyle.Overlay;
            // show the overlay
            _isVisible = true;
        }

        #region Public Utils
        /// <summary>
        /// Show the overlay. It will be drawn until you call Hide()
        /// </summary>
        public void Show()
        {
            _isVisible = true;
        }

        /// <summary>
        /// Hide the overlay. It will not be dranw until you call Show()
        /// </summary>
        public void Hide()
        {
            _isVisible = false;
        }

        /// <summary>
        /// Anchor this overlay to a WindoDefinition. 
        /// Once the according winDef will create a UIWindow, Anchor will be added to UIWindow
        /// </summary>
        /// <param name="window">window definition to add overlay</param>
        /// <param name="anchor">location anchor to anchor the overlay</param>
        /// <returns>true if added</returns>
        public bool AnchorWindowDefinition(FuWindowDefinition window, FuOverlayAnchorLocation anchor)
        {
            return AnchorWindowDefinition(window, anchor, Vector2Int.zero);
        }

        /// <summary>
        /// Anchor this overlay to a WindoDefinition. 
        /// Once the according winDef will create a UIWindow, Anchor will be added to UIWindow
        /// </summary>
        /// <param name="window">window definition to add overlay</param>
        /// <param name="anchor">location anchor to anchor the overlay</param>
        /// <param name="anchorOffset">offset position of the overlay according to it location anchor</param>
        /// <returns>true if added</returns>
        public bool AnchorWindowDefinition(FuWindowDefinition window, FuOverlayAnchorLocation anchor, Vector2Int anchorOffset)
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
        public bool AnchorWindow(FuWindow window, FuOverlayAnchorLocation anchor, Vector2 anchorOffset)
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
            Window = window;

            // set drag button position if set on Auto
            if (_dragButtonPosition == FuOverlayDragPosition.Auto)
            {
                switch (anchor)
                {
                    case FuOverlayAnchorLocation.TopCenter:
                        _dragButtonPosition = FuOverlayDragPosition.Top;
                        break;

                    case FuOverlayAnchorLocation.TopLeft:
                    case FuOverlayAnchorLocation.MiddleLeft:
                    case FuOverlayAnchorLocation.MiddleCenter:
                    case FuOverlayAnchorLocation.BottomLeft:
                        _dragButtonPosition = FuOverlayDragPosition.Left;
                        break;

                    case FuOverlayAnchorLocation.MiddleRight:
                    case FuOverlayAnchorLocation.BottomRight:
                    case FuOverlayAnchorLocation.TopRight:
                        _dragButtonPosition = FuOverlayDragPosition.Right;
                        break;

                    case FuOverlayAnchorLocation.BottomCenter:
                        _dragButtonPosition = FuOverlayDragPosition.Bottom;
                        break;
                }
            }

            // set anchor location
            _anchorLocation = anchor;
            // snap anchored offset
            AnchorOffset = new Vector2Int((int)(Mathf.FloorToInt(anchorOffset.x / _dragStep) * _dragStep), (int)(Mathf.FloorToInt(anchorOffset.y / _dragStep) * _dragStep));
            return true;
        }

        /// <summary>
        /// The the minimum size of the window that display this overlay to display it
        /// The overlay will be hidden if the window is smaller that this Vector2
        /// </summary>
        /// <param name="minimumWindowSize">minimum size of the window</param>
        public void SetMinimumWindowSize(Vector2Int minimumWindowSize)
        {
            MinimumWindowSize = minimumWindowSize;
        }

        /// <summary>
        /// Set the FuStyle of  this overlay's panel (default is FuStyle.Overlay)
        /// </summary>
        /// <param name="style">FuStyle to set on this overlay</param>
        public void SetStyle(FuStyle style)
        {
            _overlayStyle = style;
        }

        /// <summary>
        /// Set the local rectangle describing the overlay
        /// </summary>
        /// <param name="rect">Rectangle describing the overlay within the window</param>
        public void SetLocalRect(Rect rect)
        {
            LocalRect = rect;
        }
        #endregion

        /// <summary>
        /// Draw the overlay, will be call by anchored window
        /// </summary>
        internal void Draw()
        {
            if (!_isVisible)
            {
                return;
            }

            // count nb push at render begin
            _nbColorPushOnFrameStart = Fugui.NbPushColor;
            _nbStylePushOnFrameStart = Fugui.NbPushStyle;
            _nbFontPushOnFrameStart = Fugui.NbPushFont;

            // stop dragging if mouse release
            if (!Fugui.CurrentContext.IO.MouseDown[0])
            {
                _draging = false;
                _drawSnapGrid = false;
            }

            if (Window.WorkingAreaSize.x < MinimumWindowSize.x || Window.WorkingAreaSize.y < MinimumWindowSize.y)
            {
                return;
            }

            // get anchored container position
            Vector2 unsnappedDragPosition = Vector2.zero;
            Vector2 screenPos = getAnchoredPosition(ref unsnappedDragPosition);

            // set overlay local rect
            switch (_dragButtonPosition)
            {
                case FuOverlayDragPosition.Top:
                    if (_collapsed)
                    {
                        LocalRect = new Rect(screenPos - Window.LocalPosition, new Vector2(Size.x, _retractButtonWidth));
                    }
                    else
                    {
                        LocalRect = new Rect(screenPos - Window.LocalPosition, new Vector2(Size.x, Size.y + _retractButtonWidth));
                    }
                    break;
                case FuOverlayDragPosition.Right:
                    if (_collapsed)
                    {
                        LocalRect = new Rect(screenPos - Window.LocalPosition + new Vector2(Size.x, 0f), new Vector2(_retractButtonWidth, Size.y));
                    }
                    else
                    {
                        LocalRect = new Rect(screenPos - Window.LocalPosition, new Vector2(Size.x + _retractButtonWidth, Size.y));
                    }
                    break;
                case FuOverlayDragPosition.Bottom:
                    if (_collapsed)
                    {
                        LocalRect = new Rect(screenPos - Window.LocalPosition + new Vector2(0f, Size.y), new Vector2(Size.x, _retractButtonWidth));
                    }
                    else
                    {
                        LocalRect = new Rect(screenPos - Window.LocalPosition, new Vector2(Size.x, Size.y + _retractButtonWidth));
                    }
                    break;
                case FuOverlayDragPosition.Left:
                    if (_collapsed)
                    {
                        LocalRect = new Rect(screenPos - Window.LocalPosition, new Vector2(_retractButtonWidth, Size.y));
                    }
                    else
                    {
                        LocalRect = new Rect(screenPos - Window.LocalPosition, new Vector2(Size.x + _retractButtonWidth, Size.y));
                    }
                    break;
                default:
                    if (_collapsed)
                    {
                        LocalRect = new Rect(screenPos - Window.LocalPosition, new Vector2(_retractButtonWidth, _retractButtonWidth));
                    }
                    else
                    {
                        LocalRect = new Rect(screenPos - Window.LocalPosition, new Vector2(Size.x, Size.y));
                    }
                    break;
            }

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
                Fugui.Push(ImGuiCols.ChildBg, new Vector4(0.1f, 0.1f, 0.1f, 0.25f));
                ImGui.BeginChild(ID + "draginGhost", _collapsed ? new Vector2(12f, Size.y) : new Vector2(12f + Size.x, Size.y));
                ImGuiNative.igEndChild();
                Fugui.PopColor();

                // force render window next frame in case we are dragging but ùouse is out of window
                Window.ForceDraw();
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
                        ImGuiNative.igSetCursorScreenPos(new Vector2(screenPos.x, screenPos.y + _retractButtonWidth));
                        break;

                    case FuOverlayDragPosition.Right:
                        ImGuiNative.igSetCursorScreenPos(screenPos);
                        break;

                    case FuOverlayDragPosition.Bottom:
                        ImGuiNative.igSetCursorScreenPos(new Vector2(screenPos.x, screenPos.y - _retractButtonWidth));
                        break;

                    default:
                    case FuOverlayDragPosition.Left:
                        ImGuiNative.igSetCursorScreenPos(new Vector2(screenPos.x + _retractButtonWidth, screenPos.y));
                        break;
                }

                _overlayStyle.Push(true);
                if (_noBackground)
                {
                    Fugui.Push(ImGuiCols.ChildBg, Vector4.zero);
                    Fugui.Push(ImGuiCols.Border, Vector4.zero);
                    Fugui.Push(ImGuiCols.BorderShadow, Vector4.zero);
                }
                OnPreRender?.Invoke();
                if (ImGui.BeginChild(ID, Size, ImGuiChildFlags.Borders, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                {
                    UI?.Invoke(this);
                    OnPostRender?.Invoke();
                    if (_noBackground)
                    {
                        Fugui.PopColor(3);
                    }
                    _overlayStyle.Pop();
                }
                ImGuiNative.igEndChild();
            }
            Fugui.PopStyle();

            // pop missing push
            int nbMissingColor = Fugui.NbPushColor - _nbColorPushOnFrameStart;
            if (nbMissingColor > 0)
            {
                Fugui.PopColor(nbMissingColor);
            }
            int nbMissingStyle = Fugui.NbPushStyle - _nbStylePushOnFrameStart;
            if (nbMissingStyle > 0)
            {
                Fugui.PopStyle(nbMissingStyle);
            }
            int nbMissingFont = Fugui.NbPushFont - _nbFontPushOnFrameStart;
            if (nbMissingFont > 0)
            {
                Fugui.PopFont(nbMissingFont);
            }
        }

        #region Private utils
        /// <summary>
        /// Draw the drag button
        /// </summary>
        /// <param name="screenPos">screen relative position of the drag button</param>
        private void drawDragButton(Vector2 screenPos)
        {
            // set draggingColor
            if (_draging)
            {
                Fugui.Push(ImGuiCols.ChildBg, FuThemeManager.GetColor(FuColors.ButtonActive));
            }
            // set hovered color
            else if (_dragButtonHovered)
            {
                Fugui.Push(ImGuiCols.ChildBg, FuThemeManager.GetColor(FuColors.ButtonHovered));
            }
            // set default color
            else
            {
                Fugui.Push(ImGuiCols.ChildBg, FuThemeManager.GetColor(FuColors.Button));
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
            ImGuiNative.igEndChild();
            Fugui.PopColor();
            // whatever the retract button is retracted
            if (ImGui.IsItemHovered())
            {
                _dragButtonHovered = true;
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
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
                // open context menu if right clicked
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Right) && !_noEditAnchor)
                {
                    // build context menu items
                    var builder = FuContextMenuBuilder.Start()
                        .BeginChild("Overlay Anchor");
                    foreach (FuOverlayAnchorLocation location in Enum.GetValues(typeof(FuOverlayAnchorLocation)))
                    {
                        builder.AddItem(Fugui.AddSpacesBeforeUppercaseDirect(location.ToString()), () =>
                        {
                            _anchorLocation = location;
                        });
                    }
                    builder.EndChild().BeginChild("Overlay Drag");
                    foreach (FuOverlayDragPosition pos in Enum.GetValues(typeof(FuOverlayDragPosition)))
                    {
                        builder.AddItem(Fugui.AddSpacesBeforeUppercaseDirect(pos.ToString()), () =>
                        {
                            _dragButtonPosition = pos;
                        });
                    }
                    builder.EndChild();

                    // push items to context menu
                    Fugui.PushContextMenuItems(builder.Build());
                    // open context menu
                    Fugui.TryOpenContextMenu();
                    // pop items to context menu
                    Fugui.PopContextMenuItems();
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
        public Vector2 getAnchoredPosition(ref Vector2 unsnappedDragPosition)
        {
            Vector2 localPosition = default;

            // Calculate the position of the widget based on the anchor point
            switch (_anchorLocation)
            {
                case FuOverlayAnchorLocation.TopLeft:
                    localPosition = AnchorOffset; // position at top left corner
                    break;
                case FuOverlayAnchorLocation.TopCenter:
                    localPosition = new Vector2((Window.WorkingAreaSize.x - Size.x - AnchorOffset.x) * 0.5f, AnchorOffset.y); // position at top center
                    break;
                case FuOverlayAnchorLocation.TopRight:
                    localPosition = new Vector2(Window.WorkingAreaSize.x - Size.x - AnchorOffset.x, AnchorOffset.y); // position at top right corner
                    break;
                case FuOverlayAnchorLocation.MiddleLeft:
                    localPosition = new Vector2(AnchorOffset.x, (Window.WorkingAreaSize.y - Size.y - AnchorOffset.y) * 0.5f); // position at middle left side
                    break;
                case FuOverlayAnchorLocation.MiddleCenter:
                    localPosition = new Vector2((Window.WorkingAreaSize.x - Size.x - AnchorOffset.x) * 0.5f, (Window.WorkingAreaSize.y - Size.y - AnchorOffset.y) * 0.5f); // position at middle center
                    break;
                case FuOverlayAnchorLocation.MiddleRight:
                    localPosition = new Vector2(Window.WorkingAreaSize.x - Size.x - AnchorOffset.x, (Window.WorkingAreaSize.y - Size.y - AnchorOffset.y) * 0.5f); // position at middle right side
                    break;
                case FuOverlayAnchorLocation.BottomLeft:
                    localPosition = new Vector2(AnchorOffset.x, Window.WorkingAreaSize.y - Size.y - AnchorOffset.y); // position at bottom left corner
                    break;
                case FuOverlayAnchorLocation.BottomCenter:
                    localPosition = new Vector2((Window.WorkingAreaSize.x - Size.x - AnchorOffset.x) * 0.5f, Window.WorkingAreaSize.y - Size.y - AnchorOffset.y); // position at bottom center
                    break;
                case FuOverlayAnchorLocation.BottomRight:
                    localPosition = new Vector2(Window.WorkingAreaSize.x - Size.x - AnchorOffset.x, Window.WorkingAreaSize.y - Size.y - AnchorOffset.y); // position at bottom right corner
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
                unsnappedDragPosition = clampPosition(unsnappedDragPosition) + Window.LocalPosition + Window.WorkingAreaPosition;
                // snap drag offset to grid
                _dragOffset.x = (float)Math.Floor(_dragOffset.x / _dragStep) * _dragStep;
                _dragOffset.y = (float)Math.Floor(_dragOffset.y / _dragStep) * _dragStep;
            }
            // add dragOffset to local position
            localPosition += _dragOffset;

            // clamp position
            localPosition = clampPosition(localPosition);

            // return container relative position
            return localPosition + Window.LocalPosition + Window.WorkingAreaPosition;
        }

        /// <summary>
        /// clamp position to stay on window working area
        /// </summary>
        /// <param name="localPosition">position to clamp</param>
        /// <returns>clamped position</returns>
        private Vector2 clampPosition(Vector2 localPosition)
        {
            float windowPadding = 4f;
            if (localPosition.x + Size.x + windowPadding + _retractButtonWidth > Window.WorkingAreaSize.x)
                localPosition.x = Window.WorkingAreaSize.x - Size.x - windowPadding - _retractButtonWidth;
            if (localPosition.y + Size.y + windowPadding > Window.WorkingAreaSize.y)
                localPosition.y = Window.WorkingAreaSize.y - Size.y - windowPadding;
            if (localPosition.x < windowPadding)
                localPosition.x = windowPadding;
            if (localPosition.y < windowPadding)
                localPosition.y = windowPadding;
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
            int cols = (int)(Window.WorkingAreaSize.x / _dragStep) + 1;
            int rows = (int)(Window.WorkingAreaSize.y / _dragStep) + 1;

            // Calculate the starting position of the snap grid
            Vector2 startPos = Window.LocalPosition + Window.WorkingAreaPosition;

            // Draw the vertical lines of the snap grid
            for (int x = 0; x < cols; x++)
            {
                // Calculate the start and end points of the line
                Vector2 p1 = new Vector2(x * _dragStep, -_dragStep);
                Vector2 p2 = new Vector2(x * _dragStep, Window.WorkingAreaSize.y + _dragStep);
                // Add the line to the draw list
                drawList.AddLine(p1 + startPos, p2 + startPos, color, _gridWidth);
            }

            // Draw the horizontal lines of the snap grid
            for (int y = 0; y < rows; y++)
            {
                // Calculate the start and end points of the line
                Vector2 p1 = new Vector2(-_dragStep, y * _dragStep);
                Vector2 p2 = new Vector2(Window.WorkingAreaSize.x + _dragStep, y * _dragStep);
                // Add the line to the draw list
                drawList.AddLine(p1 + startPos, p2 + startPos, color, _gridWidth);
            }
        }
        #endregion
    }
}