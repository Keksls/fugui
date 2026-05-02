using Fu.Framework;
using ImGuiNET;
using System;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Represents the Fu Overlay type.
    /// </summary>
    public class FuOverlay
    {
        #region State
        // ID of the Overlay
        public string ID { get; private set; }
        public FuLayout Layout { get; private set; }
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
        public Action<FuOverlay, FuLayout> UI { get; private set; }
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

        public bool IsDraging { get; private set; } = false;
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

        private float retractButtonWidth => _retractButtonWidth * Fugui.Scale;
        // Private constant for the drag step size

        private float _dragStep = 32f;

        private float dragStep => _dragStep * Fugui.Scale;

        // Private constant for the color of the snap grid

        private Vector4 _gridColor = new Vector4(.1f, .1f, .1f, .18f);
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

        #region Constructors
        /// <summary>
        /// Instantiate an UI Overlay object
        /// </summary>
        /// <param name="id">unique ID of this overlay</param>
        /// <param name="size">size of this overlay</param>
        /// <param name="ui">UI of this overlay</param>
        /// <param name="flags">Overlay comportement flags</param>
        public FuOverlay(string id, Vector2Int size, Action<FuOverlay, FuLayout> ui, FuOverlayFlags flags = FuOverlayFlags.Default, FuOverlayDragPosition dragButtonPosition = FuOverlayDragPosition.Auto)
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
        #endregion

        #region Methods
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
            return true;
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
            if (Window != window && !window.AddOverlay(this))
            {
                return false;
            }

            // reset default state values
            _drawSnapGrid = false;
            _collapsed = false;
            _dragButtonHovered = false;
            IsDraging = false;
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
            _anchorOffset = snapUnscaledOffset(anchorOffset);
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
                IsDraging = false;
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
            Rect overlayScreenRect = getOverlayScreenRect(screenPos);
            LocalRect = new Rect(
                overlayScreenRect.position - Window.LocalPosition,
                overlayScreenRect.size);

            // if we are dragging, draw unsnapped drag ghost and snap grid
            if (IsDraging)
            {
                // set mouse cursor
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                if (_drawSnapGrid)
                {
                    // draw drag snapping grid
                    DrawGrid(ImGui.GetForegroundDrawList(), overlayScreenRect);
                }
                DrawDragPreview(ImGui.GetForegroundDrawList(), unsnappedDragPosition, overlayScreenRect);

                // force render window next frame in case we are dragging but mouse is out of window
                Window.ForceDraw();
            }

            Fugui.Push(ImGuiStyleVar.ChildRounding, 6f * Fugui.Scale);
            Fugui.Push(ImGuiStyleVar.ChildBorderSize, 1f * Fugui.Scale);
            // draw overlay UI
            if (!_collapsed)
            {
                Vector2 contentScreenPos = getContentScreenPos(screenPos);
                ImGuiNative.igSetCursorScreenPos(contentScreenPos);

                _overlayStyle.Push(true);
                DrawOverlayBody(ImGui.GetWindowDrawList(), new Rect(contentScreenPos, Size));
                Fugui.Push(ImGuiCol.ChildBg, Vector4.zero);
                Fugui.Push(ImGuiCol.Border, Vector4.zero);
                Fugui.Push(ImGuiCol.BorderShadow, Vector4.zero);
                Layout = new FuLayout();
                OnPreRender?.Invoke();
                bool childVisible = ImGui.BeginChild(ID, Size, ImGuiChildFlags.Borders, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
                if (childVisible)
                {
                    UI?.Invoke(this, Layout);
                    OnPostRender?.Invoke();
                }
                ImGuiNative.igEndChild();
                Layout.Dispose();
                Fugui.PopColor(3);
                _overlayStyle.Pop();
            }
            // draw handle after the overlay body so it can cover the inner rounded seam.
            if (_collapsable || _draggable)
            {
                drawDragButton(screenPos);
            }
            Fugui.PopStyle(2);

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

        /// <summary>
        /// Draw the drag button
        /// </summary>
        /// <param name="screenPos">screen relative position of the drag button</param>
        private void drawDragButton(Vector2 screenPos)
        {
            Rect handleRect = getHandleScreenRect(screenPos);
            Vector2 retractPos = handleRect.position;
            Vector2 retractButtonSize = handleRect.size;

            ImGui.SetCursorScreenPos(retractPos);
            ImGui.InvisibleButton(ID + "overlayHandle", retractButtonSize);

            bool hovered = ImGui.IsItemHovered();
            bool hoverChanged = hovered != _dragButtonHovered;
            _dragButtonHovered = hovered;

            Vector4 bg = IsDraging
                ? Fugui.Themes.GetColor(FuColors.ButtonActive)
                : hovered
                    ? Fugui.Themes.GetColor(FuColors.ButtonHovered)
                    : Fugui.Themes.GetColor(FuColors.Button);
            bg.w = Mathf.Max(bg.w, hovered || IsDraging ? 0.95f : 0.82f);

            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Rect visualRect = getHandleVisualRect(handleRect, out ImDrawFlags cornerFlags);
            float rounding = Mathf.Min(6f * Fugui.Scale, Mathf.Min(visualRect.width, visualRect.height) * 0.45f);
            drawList.AddRectFilled(visualRect.min, visualRect.max, ImGui.GetColorU32(bg), rounding, cornerFlags);
            DrawGripLine(drawList, retractPos, retractButtonSize, hovered);

            if (hoverChanged)
            {
                Window.ForceDraw();
            }

            if (hovered)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                string tooltip = string.Empty;
                if (_draggable)
                {
                    tooltip = "Drag overlay";
                }
                if (_collapsable)
                {
                    tooltip = string.IsNullOrEmpty(tooltip) ? "Double-click to collapse" : $"{tooltip}. Double-click to collapse";
                }
                if (!_noEditAnchor)
                {
                    tooltip = string.IsNullOrEmpty(tooltip) ? "Right-click for position" : $"{tooltip}. Right-click for position";
                }
                if (!string.IsNullOrEmpty(tooltip))
                {
                    ImGui.SetItemTooltip($"{tooltip}.");
                }

                bool doubleClicked = ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left);
                if (doubleClicked && _collapsable)
                {
                    _collapsed = !_collapsed;
                    Window.ForceDraw();
                }
                else if (ImGui.IsItemClicked(ImGuiMouseButton.Left) && _draggable)
                {
                    IsDraging = true;
                    _dragMousePosition = Window.Mouse.Position - _dragOffset;
                }

                // open context menu if right clicked
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Right) && !_noEditAnchor)
                {
                    OpenOverlayContextMenu();
                }
            }
        }

        /// <summary>
        /// Returns the screen-space rectangle occupied by the overlay.
        /// </summary>
        /// <param name="screenPos">Overlay content origin in screen coordinates.</param>
        /// <returns>Overlay screen rectangle.</returns>
        private Rect getOverlayScreenRect(Vector2 screenPos)
        {
            if (_collapsed)
            {
                return getHandleScreenRect(screenPos);
            }

            return new Rect(screenPos, getOverlayBoundsSize());
        }

        /// <summary>
        /// Returns the screen-space rectangle occupied by the overlay handle.
        /// </summary>
        /// <param name="screenPos">Overlay content origin in screen coordinates.</param>
        /// <returns>Handle screen rectangle.</returns>
        private Rect getHandleScreenRect(Vector2 screenPos)
        {
            switch (_dragButtonPosition)
            {
                case FuOverlayDragPosition.Top:
                    return new Rect(screenPos, new Vector2(Size.x, retractButtonWidth));

                case FuOverlayDragPosition.Right:
                    return new Rect(new Vector2(screenPos.x + Size.x, screenPos.y), new Vector2(retractButtonWidth, Size.y));

                case FuOverlayDragPosition.Bottom:
                    return new Rect(new Vector2(screenPos.x, screenPos.y + Size.y), new Vector2(Size.x, retractButtonWidth));

                default:
                case FuOverlayDragPosition.Left:
                    return new Rect(screenPos, new Vector2(retractButtonWidth, Size.y));
            }
        }

        /// <summary>
        /// Returns the screen-space content origin for the overlay body.
        /// </summary>
        /// <param name="screenPos">Overlay content origin in screen coordinates.</param>
        /// <returns>Content screen position.</returns>
        private Vector2 getContentScreenPos(Vector2 screenPos)
        {
            switch (_dragButtonPosition)
            {
                case FuOverlayDragPosition.Top:
                    return new Vector2(screenPos.x, screenPos.y + retractButtonWidth);

                case FuOverlayDragPosition.Left:
                    return new Vector2(screenPos.x + retractButtonWidth, screenPos.y);

                default:
                    return screenPos;
            }
        }

        /// <summary>
        /// Returns the whole overlay bounds size, including the handle.
        /// </summary>
        /// <returns>Overlay bounds size.</returns>
        private Vector2 getOverlayBoundsSize()
        {
            switch (_dragButtonPosition)
            {
                case FuOverlayDragPosition.Top:
                case FuOverlayDragPosition.Bottom:
                    return new Vector2(Size.x, Size.y + retractButtonWidth);

                case FuOverlayDragPosition.Right:
                case FuOverlayDragPosition.Left:
                    return new Vector2(Size.x + retractButtonWidth, Size.y);

                default:
                    return Size;
            }
        }

        /// <summary>
        /// Draws the overlay body without rounding the side connected to the handle.
        /// </summary>
        /// <param name="drawList">ImGui draw list.</param>
        /// <param name="bodyRect">Overlay body rectangle.</param>
        private void DrawOverlayBody(ImDrawListPtr drawList, Rect bodyRect)
        {
            if (_noBackground)
            {
                return;
            }

            Vector4 bg = Fugui.Themes.GetColor(FuColors.WindowBg);
            float rounding = 6f * Fugui.Scale;
            ImDrawFlags cornerFlags;
            switch (_dragButtonPosition)
            {
                case FuOverlayDragPosition.Top:
                    cornerFlags = ImDrawFlags.RoundCornersBottom;
                    break;

                case FuOverlayDragPosition.Right:
                    cornerFlags = ImDrawFlags.RoundCornersLeft;
                    break;

                case FuOverlayDragPosition.Bottom:
                    cornerFlags = ImDrawFlags.RoundCornersTop;
                    break;

                default:
                case FuOverlayDragPosition.Left:
                    cornerFlags = ImDrawFlags.RoundCornersRight;
                    break;
            }

            drawList.AddRectFilled(bodyRect.min, bodyRect.max, ImGui.GetColorU32(bg), rounding, cornerFlags);
        }

        /// <summary>
        /// Returns the handle visual rectangle with a small body overlap to hide inner rounded seams.
        /// </summary>
        /// <param name="handleRect">Handle interaction rectangle.</param>
        /// <param name="cornerFlags">Rounded corners to draw.</param>
        /// <returns>Handle visual rectangle.</returns>
        private Rect getHandleVisualRect(Rect handleRect, out ImDrawFlags cornerFlags)
        {
            if (_collapsed)
            {
                cornerFlags = ImDrawFlags.RoundCornersAll;
                return handleRect;
            }

            float overlap = Mathf.Min(3f * Fugui.Scale, retractButtonWidth * 0.25f);
            switch (_dragButtonPosition)
            {
                case FuOverlayDragPosition.Top:
                    cornerFlags = ImDrawFlags.RoundCornersTop;
                    return new Rect(handleRect.position, new Vector2(handleRect.width, handleRect.height + overlap));

                case FuOverlayDragPosition.Right:
                    cornerFlags = ImDrawFlags.RoundCornersRight;
                    return new Rect(new Vector2(handleRect.x - overlap, handleRect.y), new Vector2(handleRect.width + overlap, handleRect.height));

                case FuOverlayDragPosition.Bottom:
                    cornerFlags = ImDrawFlags.RoundCornersBottom;
                    return new Rect(new Vector2(handleRect.x, handleRect.y - overlap), new Vector2(handleRect.width, handleRect.height + overlap));

                default:
                case FuOverlayDragPosition.Left:
                    cornerFlags = ImDrawFlags.RoundCornersLeft;
                    return new Rect(handleRect.position, new Vector2(handleRect.width + overlap, handleRect.height));
            }
        }

        /// <summary>
        /// Draws the overlay handle highlight line.
        /// </summary>
        /// <param name="drawList">ImGui draw list.</param>
        /// <param name="position">Handle position.</param>
        /// <param name="size">Handle size.</param>
        /// <param name="hovered">Whether the handle is hovered.</param>
        private void DrawGripLine(ImDrawListPtr drawList, Vector2 position, Vector2 size, bool hovered)
        {
            bool horizontal = _dragButtonPosition == FuOverlayDragPosition.Top || _dragButtonPosition == FuOverlayDragPosition.Bottom;
            float scale = Fugui.Scale;
            float thickness = Mathf.Max(2f, 3f * scale);
            float length = horizontal
                ? Mathf.Clamp(size.x * 0.34f, 18f * scale, 54f * scale)
                : Mathf.Clamp(size.y * 0.44f, 18f * scale, 54f * scale);
            Vector2 center = position + size * 0.5f;
            Vector4 highlight = IsDraging
                ? Fugui.Themes.GetColor(FuColors.HighlightActive)
                : hovered
                    ? Fugui.Themes.GetColor(FuColors.HighlightHovered)
                    : Fugui.Themes.GetColor(FuColors.Highlight);
            highlight.w = hovered || IsDraging ? 1f : 0.82f;

            Vector2 min = horizontal
                ? new Vector2(center.x - length * 0.5f, center.y - thickness * 0.5f)
                : new Vector2(center.x - thickness * 0.5f, center.y - length * 0.5f);
            Vector2 max = horizontal
                ? new Vector2(center.x + length * 0.5f, center.y + thickness * 0.5f)
                : new Vector2(center.x + thickness * 0.5f, center.y + length * 0.5f);
            drawList.AddRectFilled(min, max, ImGui.GetColorU32(highlight), thickness * 0.5f, ImDrawFlags.RoundCornersAll);
        }

        /// <summary>
        /// Draws drag feedback for the unsnapped and snapped overlay positions.
        /// </summary>
        /// <param name="drawList">ImGui draw list.</param>
        /// <param name="unsnappedScreenPos">Unsnapped screen position.</param>
        /// <param name="snappedRect">Snapped overlay rectangle.</param>
        private void DrawDragPreview(ImDrawListPtr drawList, Vector2 unsnappedScreenPos, Rect snappedRect)
        {
            Rect unsnappedRect = getOverlayScreenRect(unsnappedScreenPos);
            Vector4 snappedFill = Fugui.Themes.GetColor(FuColors.ButtonActive);
            Vector4 snappedBorder = Fugui.Themes.GetColor(FuColors.TextInfo);
            Vector4 rawBorder = Fugui.Themes.GetColor(FuColors.TextDisabled);
            snappedFill.w = 0.14f;
            snappedBorder.w = 0.85f;
            rawBorder.w = 0.55f;

            float rounding = 6f * Fugui.Scale;
            float thickness = Mathf.Max(1f, Fugui.Scale);
            drawList.AddRectFilled(snappedRect.min, snappedRect.max, ImGui.GetColorU32(snappedFill), rounding);
            drawList.AddRect(snappedRect.min, snappedRect.max, ImGui.GetColorU32(snappedBorder), rounding, ImDrawFlags.None, thickness);

            if ((unsnappedRect.position - snappedRect.position).sqrMagnitude > 1f)
            {
                drawList.AddRect(unsnappedRect.min, unsnappedRect.max, ImGui.GetColorU32(rawBorder), rounding, ImDrawFlags.None, thickness);
            }
        }

        /// <summary>
        /// Opens the overlay positioning context menu.
        /// </summary>
        private void OpenOverlayContextMenu()
        {
            var builder = FuContextMenuBuilder.Start();
            if (_collapsable)
            {
                builder.AddItem(_collapsed ? "Expand" : "Collapse", () =>
                {
                    _collapsed = !_collapsed;
                    Window.ForceDraw();
                });
            }

            builder.AddItem("Reset position", () =>
            {
                _dragOffset = Vector2.zero;
                _drawSnapGrid = false;
                Window.ForceDraw();
            })
            .BeginChild("Anchor");

            foreach (FuOverlayAnchorLocation location in Enum.GetValues(typeof(FuOverlayAnchorLocation)))
            {
                FuOverlayAnchorLocation capturedLocation = location;
                builder.AddItem(Fugui.AddSpacesBeforeUppercaseDirect(location.ToString()), () =>
                {
                    _anchorLocation = capturedLocation;
                    _dragOffset = Vector2.zero;
                    Window.ForceDraw();
                });
            }

            builder.EndChild().BeginChild("Handle");
            foreach (FuOverlayDragPosition pos in Enum.GetValues(typeof(FuOverlayDragPosition)))
            {
                if (pos == FuOverlayDragPosition.Auto)
                {
                    continue;
                }

                FuOverlayDragPosition capturedPosition = pos;
                builder.AddItem(Fugui.AddSpacesBeforeUppercaseDirect(pos.ToString()), () =>
                {
                    _dragButtonPosition = capturedPosition;
                    _dragOffset = Vector2.zero;
                    Window.ForceDraw();
                });
            }
            builder.EndChild();

            Fugui.PushContextMenuItems(builder.Build());
            Fugui.TryOpenContextMenu();
            Fugui.PopContextMenuItems();
        }

        /// <summary>
        /// get overlay anchor position relative to it anchorLocation and position
        /// </summary>
        /// <returns>container relative position</returns>
        public Vector2 getAnchoredPosition(ref Vector2 unsnappedDragPosition)
        {
            Vector2 localPosition = getBaseAnchoredPosition();

            // handle drag offset
            if (IsDraging)
            {
                Vector2 rawDragOffset = Window.Mouse.Position - _dragMousePosition;
                Vector2 rawPosition = clampPosition(localPosition + rawDragOffset);
                // draw snap grid only if we start to drag (avoid draw grid on double click)
                if (!_drawSnapGrid && Math.Abs(rawDragOffset.x - _dragOffset.x) + Math.Abs(rawDragOffset.y - _dragOffset.y) > 4f * Fugui.Scale)
                {
                    _drawSnapGrid = true;
                }

                // store not snapped drag offset
                unsnappedDragPosition = rawPosition + Window.LocalPosition + Window.WorkingAreaPosition;

                Vector2 snappedPosition = _drawSnapGrid
                    ? clampPosition(snapPosition(rawPosition))
                    : rawPosition;

                _dragOffset = snappedPosition - localPosition;
            }
            // add dragOffset to local position
            localPosition += _dragOffset;

            // clamp position
            localPosition = clampPosition(localPosition);

            // return container relative position
            return localPosition + Window.LocalPosition + Window.WorkingAreaPosition;
        }

        /// <summary>
        /// Computes the anchored overlay position before the user drag offset is applied.
        /// </summary>
        /// <returns>Position relative to the window working area.</returns>
        private Vector2 getBaseAnchoredPosition()
        {
            Vector2 anchorOffset = AnchorOffset;
            Vector2 boundsSize = getOverlayBoundsSize();

            switch (_anchorLocation)
            {
                case FuOverlayAnchorLocation.TopLeft:
                    return anchorOffset;

                case FuOverlayAnchorLocation.TopCenter:
                    return new Vector2((Window.WorkingAreaSize.x - boundsSize.x) * 0.5f + anchorOffset.x, anchorOffset.y);

                case FuOverlayAnchorLocation.TopRight:
                    return new Vector2(Window.WorkingAreaSize.x - boundsSize.x - anchorOffset.x, anchorOffset.y);

                case FuOverlayAnchorLocation.MiddleLeft:
                    return new Vector2(anchorOffset.x, (Window.WorkingAreaSize.y - boundsSize.y) * 0.5f + anchorOffset.y);

                case FuOverlayAnchorLocation.MiddleCenter:
                    return new Vector2((Window.WorkingAreaSize.x - boundsSize.x) * 0.5f + anchorOffset.x, (Window.WorkingAreaSize.y - boundsSize.y) * 0.5f + anchorOffset.y);

                case FuOverlayAnchorLocation.MiddleRight:
                    return new Vector2(Window.WorkingAreaSize.x - boundsSize.x - anchorOffset.x, (Window.WorkingAreaSize.y - boundsSize.y) * 0.5f + anchorOffset.y);

                case FuOverlayAnchorLocation.BottomLeft:
                    return new Vector2(anchorOffset.x, Window.WorkingAreaSize.y - boundsSize.y - anchorOffset.y);

                case FuOverlayAnchorLocation.BottomCenter:
                    return new Vector2((Window.WorkingAreaSize.x - boundsSize.x) * 0.5f + anchorOffset.x, Window.WorkingAreaSize.y - boundsSize.y - anchorOffset.y);

                case FuOverlayAnchorLocation.BottomRight:
                    return new Vector2(Window.WorkingAreaSize.x - boundsSize.x - anchorOffset.x, Window.WorkingAreaSize.y - boundsSize.y - anchorOffset.y);

                default:
                    return anchorOffset;
            }
        }

        /// <summary>
        /// Snaps an unscaled offset to the overlay grid.
        /// </summary>
        /// <param name="offset">Unscaled offset.</param>
        /// <returns>Snapped unscaled offset.</returns>
        private Vector2Int snapUnscaledOffset(Vector2 offset)
        {
            float step = Mathf.Max(1f, _dragStep);
            return new Vector2Int(
                Mathf.RoundToInt(offset.x / step) * Mathf.RoundToInt(step),
                Mathf.RoundToInt(offset.y / step) * Mathf.RoundToInt(step));
        }

        /// <summary>
        /// Snaps a working-area position to the overlay grid.
        /// </summary>
        /// <param name="position">Working-area position in scaled pixels.</param>
        /// <returns>Snapped position.</returns>
        private Vector2 snapPosition(Vector2 position)
        {
            float step = Mathf.Max(1f, dragStep);
            Vector2 referenceOffset = getSnapReferenceOffset();
            Vector2 referencePosition = position + referenceOffset;
            return new Vector2(
                Mathf.Round(referencePosition.x / step) * step,
                Mathf.Round(referencePosition.y / step) * step) - referenceOffset;
        }

        /// <summary>
        /// Returns the local point that should stick to the grid for the current handle side.
        /// </summary>
        /// <returns>Snap reference offset from the overlay content origin.</returns>
        private Vector2 getSnapReferenceOffset()
        {
            Vector2 boundsSize = getOverlayBoundsSize();
            switch (_dragButtonPosition)
            {
                case FuOverlayDragPosition.Right:
                    return new Vector2(boundsSize.x, 0f);

                case FuOverlayDragPosition.Bottom:
                    return new Vector2(0f, boundsSize.y);

                default:
                    return Vector2.zero;
            }
        }

        /// <summary>
        /// clamp position to stay on window working area
        /// </summary>
        /// <param name="localPosition">position to clamp</param>
        /// <returns>clamped position</returns>
        private Vector2 clampPosition(Vector2 localPosition)
        {
            float windowPadding = 6f * Fugui.Scale;
            Vector2 boundsSize = getOverlayBoundsSize();
            if (localPosition.x + boundsSize.x + windowPadding > Window.WorkingAreaSize.x)
                localPosition.x = Window.WorkingAreaSize.x - boundsSize.x - windowPadding;
            if (localPosition.y + boundsSize.y + windowPadding > Window.WorkingAreaSize.y)
                localPosition.y = Window.WorkingAreaSize.y - boundsSize.y - windowPadding;
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
        private void DrawGrid(ImDrawListPtr drawList, Rect snappedRect)
        {
            Vector2 startPos = Window.LocalPosition + Window.WorkingAreaPosition;
            Vector2 endPos = startPos + Window.WorkingAreaSize;
            float step = Mathf.Max(4f, dragStep);
            uint minorColor = ImGui.GetColorU32(_gridColor);
            Vector4 majorGridColor = _gridColor;
            majorGridColor.w = Mathf.Min(1f, _gridColor.w * 1.8f);
            uint majorColor = ImGui.GetColorU32(majorGridColor);
            Vector4 fill = Fugui.Themes.GetColor(FuColors.WindowBg);
            fill.w = 0.10f;
            Vector4 snapCell = Fugui.Themes.GetColor(FuColors.TextInfo);
            snapCell.w = 0.09f;

            drawList.AddRectFilled(startPos, endPos, ImGui.GetColorU32(fill));

            int index = 0;
            for (float x = startPos.x; x <= endPos.x + 0.5f; x += step, index++)
            {
                uint color = index % 4 == 0 ? majorColor : minorColor;
                drawList.AddLine(new Vector2(x, startPos.y), new Vector2(x, endPos.y), color, _gridWidth * Fugui.Scale);
            }

            index = 0;
            for (float y = startPos.y; y <= endPos.y + 0.5f; y += step, index++)
            {
                uint color = index % 4 == 0 ? majorColor : minorColor;
                drawList.AddLine(new Vector2(startPos.x, y), new Vector2(endPos.x, y), color, _gridWidth * Fugui.Scale);
            }

            drawList.AddRectFilled(snappedRect.min, snappedRect.max, ImGui.GetColorU32(snapCell), 6f * Fugui.Scale);
            drawList.AddRect(startPos, endPos, majorColor, 0f, ImDrawFlags.None, Mathf.Max(1f, Fugui.Scale));
        }
        #endregion
    }
}
