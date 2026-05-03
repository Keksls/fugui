//#define PUSH_POP_DEBUG
using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework
{
    /// <summary>
    /// Represents the base class for creating user interface layouts.
    /// </summary>
    public unsafe partial class FuLayout : IDisposable
    {
        /// <summary>
        /// The current Layout or grid that is drawing at time.
        /// </summary>
        public static Stack<FuLayout> CurrentDrawerPath { get; protected set; } = new Stack<FuLayout>();
        /// <summary>
        /// A flag indicating last drawed item is hovered by current pointer.
        /// </summary>
        public bool LastItemHovered { get => _lastItemHovered; }

        private static bool _lastItemHovered = false;

        /// <summary>
        /// A rectangle representing the last drawed item position and size.
        /// </summary>
        public Rect LastItemRect { get => _lastItemRect; }

        private static Rect _lastItemRect = default;

        /// <summary>
        /// A flag indicating last drawed item is currently used by current pointer.
        /// </summary>
        public bool LastItemActive { get => _lastItemActive; }

        private static bool _lastItemActive = false;

        /// <summary>
        /// A flag indicating last drawed item was active last frame and is no more this frame.
        /// </summary>
        public bool LastItemJustDeactivated { get => _lastItemJustDeactivated; }

        private static bool _lastItemJustDeactivated = false;

        /// <summary>
        /// A flag indicating last drawed item was NOT active last frame and is this frame.
        /// </summary>
        public bool LastItemJustActivated { get => _lastItemJustActivated; }

        private static bool _lastItemJustActivated = false;

        /// <summary>
        /// A flag indicating last drawed item has just done an update operation this frame.
        /// </summary>
        public bool LastItemUpdate { get => _lastItemUpdate; }

        private static bool _lastItemUpdate = false;

        /// <summary>
        /// The ID of the item that just been draw.
        /// </summary>
        public string LastItemID { get => _lastItemID; }

        private static string _lastItemID = string.Empty;

        /// <summary>
        /// the button just clicked on the last draw item.
        /// </summary>
        public FuMouseButton LastItemClickedButton { get => _lastItemClickedButton; }

        private static FuMouseButton _lastItemClickedButton = FuMouseButton.None;

        /// <summary>
        /// A flag indicating whether the Last element has been disabled.
        /// </summary>
        public bool LastItemDisabled { get; protected set; } = false;

        // A flag indicating whether the element is hover framed.

        private bool _elementHoverFramedEnabled = false;
        // An array of strings representing the current tool tips.
        protected string[] _currentToolTips = null;
        // An array of styles representing the current tool tips styles.
        protected FuTextStyle[] _currentToolTipsStyles = null;
        // An integer representing the current tool tips index.
        protected int _currentToolTipsIndex = 0;
        // whatever tooltip must be display hover Labels
        protected bool _currentToolTipsOnLabels = false;
        // has animations enabled
        protected bool _animationEnabled = true;
        // screen relative pos of the current drawing item
        protected static Vector2 _currentItemStartPos;
        // whatever elements are currently disabled (if true)
        private bool _longDisabled = false;
        // the time before an hovered element display it's tooltip
        private static float _tooltipAppearDuration = 1.0f;
        // the time at the fame the current hovered element start to be hovered
        private static float _currentHoveredStartHoverTime = 0f;
        // the ID if the element that want to display tooltips
        private static string _currentHoveredElementId = string.Empty;
#if PUSH_POP_DEBUG
        Dictionary<string, Stack<IFuElementStyle>> pushedStyles = new Dictionary<string, Stack<IFuElementStyle>>();
#endif

        // A set of strings representing the dragging sliders.
        private static HashSet<string> _draggingSliders = new HashSet<string>();
        // A dictionary that store displaying toggle data.
        private static Dictionary<string, FuElementAnimationData> _uiElementAnimationDatas = new Dictionary<string, FuElementAnimationData>();
        // A dictionary of strings representing the current PathFields string value.
        private static Dictionary<string, string> _pathFieldValues = new Dictionary<string, string>();
        // A dictionary to store enum values according to the type of the enum
        private static Dictionary<Type, List<IConvertible>> _enumValues = new Dictionary<Type, List<IConvertible>>();
        // A dictionary to store enum values as string according to the type of the enum
        private static Dictionary<Type, List<string>> _enumValuesString = new Dictionary<Type, List<string>>();
        protected bool _drawElement = true;

        public static bool IsThereAnyDraggingSlider => _draggingSliders.Count > 0;

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the Fu Layout class.
        /// </summary>
        public FuLayout()
        {
            CurrentDrawerPath.Push(this);
            _tooltipAppearDuration = 0.7f;
        }
        #endregion

        /// <summary>
        /// Disposes this Layout
        /// </summary>
        public virtual void Dispose()
        {
            CurrentDrawerPath.Pop();
#if PUSH_POP_DEBUG
            if (pushedStyles.Count > 0)
            {
                Debug.Log("missing style pop : ");
                foreach (var pair in pushedStyles)
                {
                    Debug.Log(pair.Key + " : " + pair.Value.GetType().ToString() + " (" + (pair.Value == null ? "null" : "") + ")");
                }
            }
#endif
        }

        /// <summary>
        /// Begins an element in this layout with the specified style.
        /// </summary>
        /// <param name="style">The style to use for this element.</param>
        protected virtual void beginElement(ref string elementID, IFuElementStyle style = null, bool noEditID = false, bool canBeHidden = true)
        {
            _lastItemRect = default;
            _lastItemActive = false;
            _lastItemHovered = false;
            _lastItemJustDeactivated = false;
            _lastItemUpdate = false;
            _lastItemJustActivated = false;
            _lastItemClickedButton = FuMouseButton.None;

            // whatever we must draw the next item
            _drawElement = true;
            if (!Fugui.IsDrawingInsidePopup() && FuPanel.IsInsidePanel && FuPanel.Clipper != null)
            {
                _drawElement = FuPanel.Clipper.BeginDrawElement(canBeHidden);
            }
            if (_drawElement)
            {
                _currentItemStartPos = ImGui.GetCursorScreenPos();
                // we must prepare next item
                style?.Push(!LastItemDisabled);
                if (!noEditID && FuWindow.CurrentDrawingWindow != null)
                {
                    elementID = elementID + "##" + FuWindow.CurrentDrawingWindow.ID;
                }
                _lastItemID = elementID;

#if PUSH_POP_DEBUG
                if (!pushedStyles.ContainsKey(elementID))
                {
                    pushedStyles.Add(elementID, new Stack<IFuElementStyle>());
                }
                pushedStyles[elementID].Push(style);
# endif
            }
            // if out of scroll bounds, we must dummy the element rect
            else
            {
                endElement(style);
            }
        }

        /// <summary>
        /// Ends an element in this layout with the specified style.
        /// </summary>
        /// <param name="style">The style to use for this element.</param>
        protected virtual void endElement(IFuElementStyle style = null)
        {
            // whatever the item has just been draw
            if (_drawElement)
            {
                style?.Pop();
#if PUSH_POP_DEBUG
                if (pushedStyles.ContainsKey(_lastItemID))
                {
                    pushedStyles[_lastItemID].Pop();
                    if (pushedStyles[_lastItemID].Count == 0)
                    {
                        pushedStyles.Remove(_lastItemID);
                    }
                }
                else
                {
                    Debug.Log("Try to pop for element " + _lastItemID + " but no style has been pushed");
                }
# endif
                DrawHoverFrame();
                if (_lastItemClickedButton == FuMouseButton.Right)
                {
                    Fugui.TryOpenContextMenu();
                }
            }
            if (!_longDisabled)
            {
                LastItemDisabled = false;
            }
            _elementHoverFramedEnabled = false;
            if (!Fugui.IsDrawingInsidePopup() && FuPanel.IsInsidePanel && FuPanel.Clipper != null)
            {
                FuPanel.Clipper.EndDrawElement();
            }
        }

        /// <summary>
        /// Draws a hover frame around the current element if needed.
        /// </summary>
        /// <param name="force">force drawing over frame</param>
        public void DrawHoverFrame(bool force = false)
        {
            if(Fugui.IsScrolling) // prevent hover frame to change during scroll (since mouse pos is changing but we are still on the same item)
            {
                return;
            }
            if (_elementHoverFramedEnabled && !LastItemDisabled)
            {
                if (ImGuiNative.igIsItemFocused() != 0)
                {
                    ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.FrameSelectedFeedback)), ImGui.GetStyle().FrameRounding);
                }
                else if (ImGui.IsItemHovered())
                {
                    // ImGui fail on inputText since version 1.88, check on new version
                    ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.FrameHoverFeedback)), ImGui.GetStyle().FrameRounding);
                }
            }
            else if (force)
            {
                ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.FrameHoverFeedback)), ImGui.GetStyle().FrameRounding);
            }
        }

        /// <summary>
        /// Draws a frame border at the given rect.
        /// </summary>
        /// <param name="rect">rect of the frame (pos + size)</param>
        private void drawBorderFrame(Rect rect, bool rounded = true)
        {
            ImGui.GetWindowDrawList().AddRect(rect.min, rect.max, ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.FrameSelectedFeedback)), rounded ? ImGui.GetStyle().FrameRounding : 0f);
        }

        /// <summary>
        /// Draws a subtle hover/focus ring around a custom widget frame.
        /// </summary>
        /// <param name="drawList">Draw list.</param>
        /// <param name="rect">Frame rectangle.</param>
        /// <param name="focused">Whether the widget has focus.</param>
        /// <param name="hovered">Whether the widget is hovered.</param>
        /// <param name="disabled">Whether the widget is disabled.</param>
        /// <param name="rounding">Frame rounding.</param>
        private void DrawWidgetFeedback(ImDrawListPtr drawList, Rect rect, bool focused, bool hovered, bool disabled, float rounding)
        {
            if (disabled)
            {
                return;
            }

            Vector4 color;
            float thickness;
            if (focused)
            {
                color = Fugui.Themes.GetColor(FuColors.FrameSelectedFeedback);
                color.w = Mathf.Max(color.w, 0.9f);
                thickness = Mathf.Max(1.5f, 1.5f * Fugui.CurrentContext.Scale);
            }
            else if (hovered)
            {
                color = Fugui.Themes.GetColor(FuColors.FrameHoverFeedback);
                color.w = Mathf.Max(color.w, 0.55f);
                thickness = Mathf.Max(1f, Fugui.CurrentContext.Scale);
            }
            else
            {
                return;
            }

            drawList.AddRect(rect.min, rect.max, ImGui.GetColorU32(color), rounding, ImDrawFlags.RoundCornersAll, thickness);
        }

        /// <summary>
        /// Enables anti-aliased fills for the next custom filled primitive.
        /// </summary>
        /// <param name="drawList">Draw list.</param>
        /// <returns>Previous draw list flags.</returns>
        private static ImDrawListFlags PushAntiAliasedFill(ImDrawListPtr drawList)
        {
            ImDrawListFlags previousFlags = drawList.Flags;
            drawList.Flags = previousFlags | ImDrawListFlags.AntiAliasedFill;
            return previousFlags;
        }

        /// <summary>
        /// Restores draw list flags after a local anti-aliased fill draw.
        /// </summary>
        /// <param name="drawList">Draw list.</param>
        /// <param name="previousFlags">Previous draw list flags.</param>
        private static void PopAntiAliasedFill(ImDrawListPtr drawList, ImDrawListFlags previousFlags)
        {
            drawList.Flags = previousFlags;
        }

        private static void AddRectFilledAntiAliased(ImDrawListPtr drawList, Vector2 min, Vector2 max, uint color, float rounding, ImDrawFlags flags = ImDrawFlags.None)
        {
            ImDrawListFlags previousFlags = PushAntiAliasedFill(drawList);
            drawList.AddRectFilled(min, max, color, rounding, flags);
            PopAntiAliasedFill(drawList, previousFlags);
        }

        private static void AddCircleFilledAntiAliased(ImDrawListPtr drawList, Vector2 center, float radius, uint color, int segments = 0)
        {
            ImDrawListFlags previousFlags = PushAntiAliasedFill(drawList);
            drawList.AddCircleFilled(center, radius, color, segments);
            PopAntiAliasedFill(drawList, previousFlags);
        }

        /// <summary>
        /// Draws a rounded filled bar segment.
        /// </summary>
        /// <param name="drawList">Draw list.</param>
        /// <param name="min">Minimum point.</param>
        /// <param name="max">Maximum point.</param>
        /// <param name="color">Fill color.</param>
        /// <param name="rounding">Requested rounding.</param>
        /// <param name="antiAliasFill">Whether to enable anti-aliased fill locally.</param>
        private void DrawRoundedSegment(ImDrawListPtr drawList, Vector2 min, Vector2 max, Vector4 color, float rounding, bool antiAliasFill = false)
        {
            if (max.x <= min.x || max.y <= min.y)
            {
                return;
            }

            rounding = Mathf.Min(rounding, (max.y - min.y) * 0.5f, (max.x - min.x) * 0.5f);
            uint packedColor = ImGui.GetColorU32(color);
            if (antiAliasFill)
            {
                AddRectFilledAntiAliased(drawList, min, max, packedColor, rounding, ImDrawFlags.RoundCornersAll);
            }
            else
            {
                drawList.AddRectFilled(min, max, packedColor, rounding, ImDrawFlags.RoundCornersAll);
            }
        }

        /// <summary>
        /// Draws a compact magnifying glass icon.
        /// </summary>
        /// <param name="drawList">Draw list.</param>
        /// <param name="center">Icon center.</param>
        /// <param name="radius">Lens radius.</param>
        /// <param name="color">Icon color.</param>
        private void DrawSearchGlyph(ImDrawListPtr drawList, Vector2 center, float radius, Vector4 color)
        {
            float thickness = Mathf.Max(1.2f, 1.4f * Fugui.CurrentContext.Scale);
            uint packed = ImGui.GetColorU32(color);
            drawList.AddCircle(center - new Vector2(radius * 0.12f, radius * 0.12f), radius, packed, 20, thickness);
            drawList.AddLine(center + new Vector2(radius * 0.58f, radius * 0.58f), center + new Vector2(radius * 1.12f, radius * 1.12f), packed, thickness);
        }

        /// <summary>
        /// Draws the visual state for a custom combobox button.
        /// </summary>
        /// <param name="drawList">Draw list.</param>
        /// <param name="rect">Button rectangle.</param>
        /// <param name="caretWidth">Caret zone width.</param>
        /// <param name="opened">Whether popup is open.</param>
        /// <param name="disabled">Whether disabled.</param>
        private void DrawComboboxChrome(ImDrawListPtr drawList, Rect rect, float caretWidth, bool opened, bool disabled)
        {
            float scale = Fugui.CurrentContext.Scale;
            float rounding = ImGui.GetStyle().FrameRounding;
            Rect caretRect = new Rect(new Vector2(rect.xMax - caretWidth, rect.y), new Vector2(caretWidth, rect.height));

            Vector4 separator = Fugui.Themes.GetColor(FuColors.Border);
            separator.w = disabled ? 0.16f : opened ? 0.42f : 0.26f;
            float inset = Mathf.Max(5f, 6f * scale);
            drawList.AddLine(new Vector2(caretRect.xMin, caretRect.yMin + inset), new Vector2(caretRect.xMin, caretRect.yMax - inset), ImGui.GetColorU32(separator), Mathf.Max(1f, scale));

            DrawWidgetFeedback(drawList, rect, opened, _lastItemHovered, disabled, rounding);
        }

        /// <summary>
        /// Draws a compact clear icon with a hover affordance.
        /// </summary>
        /// <param name="drawList">Draw list.</param>
        /// <param name="center">Icon center.</param>
        /// <param name="radius">Icon radius.</param>
        /// <param name="color">Icon color.</param>
        /// <param name="hovered">Whether the icon is hovered.</param>
        private void DrawClearGlyph(ImDrawListPtr drawList, Vector2 center, float radius, Vector4 color, bool hovered)
        {
            if (hovered)
            {
                Vector4 bg = Fugui.Themes.GetColor(FuColors.FrameHoverFeedback);
                bg.w = Mathf.Max(bg.w, 0.22f);
                drawList.AddCircleFilled(center, radius * 1.15f, ImGui.GetColorU32(bg), 24);
            }

            float cross = radius * 0.48f;
            float thickness = Mathf.Max(1.2f, 1.4f * Fugui.CurrentContext.Scale);
            uint packed = ImGui.GetColorU32(color);
            drawList.AddLine(center - new Vector2(cross, cross), center + new Vector2(cross, cross), packed, thickness);
            drawList.AddLine(center + new Vector2(cross, -cross), center + new Vector2(-cross, cross), packed, thickness);
        }

        /// <summary>
        /// Draws a flat slider knob with a hover ring.
        /// </summary>
        /// <param name="drawList">Draw list.</param>
        /// <param name="center">Knob center.</param>
        /// <param name="radius">Knob radius.</param>
        /// <param name="color">Knob fill color.</param>
        /// <param name="hovered">Whether hovered.</param>
        /// <param name="active">Whether active.</param>
        /// <param name="disabled">Whether disabled.</param>
        private void DrawValueKnob(ImDrawListPtr drawList, Vector2 center, float radius, Vector4 color, bool hovered, bool active, bool disabled)
        {
            float scale = Fugui.CurrentContext.Scale;

            if ((hovered || active) && !disabled)
            {
                Vector4 ring = active ? Fugui.Themes.GetColor(FuColors.HighlightActive) : Fugui.Themes.GetColor(FuColors.HighlightHovered);
                ring.w = active ? 0.55f : 0.35f;
                drawList.AddCircle(center, radius + 3f * scale, ImGui.GetColorU32(ring), 32, Mathf.Max(1f, 1.5f * scale));
            }

            Vector4 border = Fugui.Themes.GetColor(FuColors.Border);
            border.w = disabled ? 0.35f : 0.75f;
            AddCircleFilledAntiAliased(drawList, center, radius + Mathf.Max(1f, scale), ImGui.GetColorU32(border), 32);
            AddCircleFilledAntiAliased(drawList, center, radius, ImGui.GetColorU32(color), 32);
        }

        /// <summary>
        /// Draws a slider knob with the window clip instead of the tighter content clip.
        /// </summary>
        /// <param name="drawList">Draw list.</param>
        /// <param name="center">Knob center.</param>
        /// <param name="radius">Knob radius.</param>
        /// <param name="color">Knob fill color.</param>
        /// <param name="hovered">Whether hovered.</param>
        /// <param name="active">Whether active.</param>
        /// <param name="disabled">Whether disabled.</param>
        private void DrawValueKnobWithWindowClip(ImDrawListPtr drawList, Vector2 center, float radius, Vector4 color, bool hovered, bool active, bool disabled)
        {
            Vector2 clipMin = ImGui.GetWindowPos();
            Vector2 clipMax = clipMin + ImGui.GetWindowSize();
            drawList.PushClipRect(clipMin, clipMax, false);
            DrawValueKnob(drawList, center, radius, color, hovered, active, disabled);
            drawList.PopClipRect();
        }

        /// <summary>
        /// Draws a compact value bubble above a dragged slider knob.
        /// </summary>
        /// <param name="drawList">Draw list.</param>
        /// <param name="text">Value text.</param>
        /// <param name="anchor">Anchor position.</param>
        private void DrawValueBubble(ImDrawListPtr drawList, string text, Vector2 anchor)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            float scale = Fugui.CurrentContext.Scale;
            Vector2 padding = new Vector2(7f, 3f) * scale;
            Vector2 textSize = ImGui.CalcTextSize(text);
            Vector2 size = textSize + padding * 2f;
            Vector2 pos = new Vector2(anchor.x - size.x * 0.5f, anchor.y - size.y - 10f * scale);
            float rounding = Mathf.Min(5f * scale, size.y * 0.45f);

            Vector4 bg = Fugui.Themes.GetColor(FuColors.PopupBg);
            bg.w = Mathf.Max(bg.w, 0.96f);
            Vector4 border = Fugui.Themes.GetColor(FuColors.Highlight);
            border.w = 0.65f;
            drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(bg), rounding, ImDrawFlags.RoundCornersAll);
            drawList.AddRect(pos, pos + size, ImGui.GetColorU32(border), rounding, ImDrawFlags.RoundCornersAll, Mathf.Max(1f, scale));
            drawList.AddText(pos + padding, ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.Text)), text);
        }

        /// <summary>
        /// Formats a slider value for transient visual feedback.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="isInt">Whether it is an integer slider.</param>
        /// <param name="format">Optional printf-like format.</param>
        /// <returns>Display text.</returns>
        private string FormatValueBubble(float value, bool isInt, string format)
        {
            if (isInt)
            {
                return Mathf.RoundToInt(value).ToString();
            }

            int decimals = 2;
            string fmt = string.IsNullOrEmpty(format) ? getStringFormat(value) : format;
            int dotIndex = fmt.IndexOf('.');
            int fIndex = fmt.IndexOf('f');
            if (dotIndex >= 0 && fIndex > dotIndex)
            {
                string precision = fmt.Substring(dotIndex + 1, fIndex - dotIndex - 1);
                if (int.TryParse(precision, out int parsed))
                {
                    decimals = Mathf.Clamp(parsed, 0, 6);
                }
            }
            return value.ToString("F" + decimals);
        }

        /// <summary>
        /// Picks a readable text color for text over a filled surface.
        /// </summary>
        /// <param name="background">Background color.</param>
        /// <param name="disabled">Whether disabled.</param>
        /// <returns>Readable text color.</returns>
        private Vector4 GetReadableTextColor(Vector4 background, bool disabled)
        {
            float luminance = background.x * 0.299f + background.y * 0.587f + background.z * 0.114f;
            Vector4 text = luminance > 0.58f ? new Vector4(0.05f, 0.05f, 0.05f, 1f) : Fugui.Themes.GetColor(FuColors.Text);
            if (disabled)
            {
                text.w *= 0.5f;
            }
            return text;
        }

        /// <summary>
        /// Set tooltips for the x next element(s)
        /// </summary>
        /// <param name="tooltips">array of tooltips to set</param>
        public void SetNextElementToolTip(params string[] tooltips)
        {
            _currentToolTips = tooltips;
            _currentToolTipsIndex = 0;
            _currentToolTipsOnLabels = false;
        }

        /// <summary>
        /// Set tooltips for the x next element(s), including labels
        /// </summary>
        /// <param name="tooltips">array of tooltips to set</param>
        public void SetNextElementToolTipWithLabel(params string[] tooltips)
        {
            _currentToolTips = tooltips;
            _currentToolTipsIndex = 0;
            _currentToolTipsOnLabels = true;
        }

        /// <summary>
        /// Set tooltips styles for the x next elements
        /// </summary>
        /// <param name="styles">array of styles to set</param>
        public void SetNextElementToolTipStyles(params FuTextStyle[] styles)
        {
            _currentToolTipsStyles = styles;
        }

        /// <summary>
        /// Set the time user have to keep hover an elemenet before showing its tooltip
        /// </summary>
        /// <param name="tooltipAppearDuration"></param>
        public void SetToolTipAppearDuration(float tooltipAppearDuration)
        {
            _tooltipAppearDuration = Mathf.Clamp(tooltipAppearDuration, 0f, 5f);
        }

        /// <summary>
        /// Add a line under the last drawed element
        /// You can change its color by pushing Text color
        /// </summary>
        public void AddUnderLine()
        {
            Vector2 min = ImGui.GetItemRectMin();
            Vector2 max = ImGui.GetItemRectMax();
            min.y = max.y;
            ImGui.GetWindowDrawList().AddLine(min, max, ImGui.GetColorU32(ImGuiCol.Text), 1.0f);
        }

        /// <summary>
        ///  From this point, animations in this layout are enabled
        /// </summary>
        public void EnableAnimationsFromNow()
        {
            _animationEnabled = true;
        }

        /// <summary>
        ///  From this point, animations in this layout are disabled
        /// </summary>
        public void DisableAnimationsFromNow()
        {
            _animationEnabled = false;
        }

        /// <summary>
        /// Disables the next element in this layout.
        /// </summary>
        public void DisableNextElement()
        {
            LastItemDisabled = true;
        }

        /// <summary>
        /// Disables all next elements in this layout from now.
        /// Call 'EnableNextElements' to stop disabling
        /// </summary>
        public void DisableNextElements()
        {
            LastItemDisabled = true;
            _longDisabled = true;
        }

        /// <summary>
        /// Enables all next element in this layout.
        /// </summary>
        public void EnableNextElements()
        {
            LastItemDisabled = false;
            _longDisabled = false;
        }
        /// <summary>
        /// Draw a Separator Line
        /// </summary>
        public void Separator()
        {
            ImGuiNative.igSeparator();
        }

        /// <summary>
        /// Draw a space of size ItemSpacing of the current theme
        /// </summary>
        public void Spacing()
        {
            ImGuiNative.igSpacing();
        }

        /// <summary>
        /// Draw the next element on Same Line as current
        /// </summary>
        public void SameLine()
        {
            ImGuiNative.igSameLine(0f, -1f);
        }

        /// <summary>
        /// Draw an empty dummy element of size x y
        /// </summary>
        /// <param name="x">width of the dummy</param>
        /// <param name="y">height of the dummy</param>
        public void Dummy(float x = 0f, float y = 0f)
        {
            ImGuiNative.igDummy(new Vector2(x * Fugui.CurrentContext.Scale, y * Fugui.CurrentContext.Scale));
        }

        /// <summary>
        /// Draw an empty dummy element of size 'size'
        /// NO SCALE APPLYED
        /// </summary>
        /// <param name="size">size of the dummy</param>
        public void DummyUnscaled(Vector2 size)
        {
            ImGuiNative.igDummy(size);
        }

        /// <summary>
        /// Draw an empty dummy element of size x y
        /// NO SCALE APPLYED
        /// </summary>
        /// <param name="x">width of the dummy</param>
        /// <param name="y">height of the dummy</param>
        public void DummyUnscaled(float x = 0f, float y = 0f)
        {
            ImGuiNative.igDummy(new Vector2(x, y));
        }

        /// <summary>
        /// Draw an empty dummy element of size 'size'
        /// </summary>
        /// <param name="size">size of the dummy</param>
        public void Dummy(Vector2 size)
        {
            ImGuiNative.igDummy(size * Fugui.CurrentContext.Scale);
        }

        /// <summary>
        /// Get the current available area of the drawing region
        /// </summary>
        /// <returns>Vector2 that represent the available area (x = width, y = height)</returns>
        public Vector2 GetAvailable()
        {
            Vector2 imAvail = ImGui.GetContentRegionAvail();

            float windowHeight = float.MaxValue;
            if (FuWindow.CurrentDrawingWindow != null)
            {
                float cursoY = ImGui.GetCursorScreenPos().y;
                FuWindow win = FuWindow.CurrentDrawingWindow;
                float windowMaxY = win.LocalPosition.y + win.WorkingAreaPosition.y + win.WorkingAreaSize.y;
                windowHeight = windowMaxY - cursoY;
            }

            return new Vector2(imAvail.x, Mathf.Min(windowHeight, imAvail.y));
        }

        /// <summary>
        /// Get the current available width of the drawing region
        /// </summary>
        /// <returns>float that represent the available width</returns>
        public float GetAvailableWidth()
        {
            return ImGui.GetContentRegionAvail().x;
        }

        /// <summary>
        /// Get the current available height of the drawing region
        /// </summary>
        /// <returns>float that represent the available height</returns>
        public float GetAvailableHeight()
        {
            float windowHeight = float.MaxValue;
            if (FuWindow.CurrentDrawingWindow != null)
            {
                float cursoY = ImGui.GetCursorScreenPos().y;
                FuWindow win = FuWindow.CurrentDrawingWindow;
                float windowMaxY = win.LocalPosition.y + win.WorkingAreaPosition.y + win.WorkingAreaSize.y;
                windowHeight = windowMaxY - cursoY;
            }

            return Mathf.Min(windowHeight, ImGui.GetContentRegionAvail().y);
        }

        /// <summary>
        /// Align the next text widget to add a frame padding around it
        /// </summary>
        public void AlignTextToFramePadding()
        {
            ImGuiNative.igAlignTextToFramePadding();
        }

        /// <summary>
        /// Beggin a group of widgets
        /// The cursor default pos is now according to the group pos
        /// You can SameLine groups
        /// </summary>
        public void BeginGroup()
        {
            ImGuiNative.igBeginGroup();
        }

        /// <summary>
        /// End of a group of widgets
        /// </summary>
        public void EndGroup()
        {
            ImGuiNative.igEndGroup();
        }

        /// <summary>
        /// Clean check whatever an item is hovered
        /// </summary>
        /// <param name="pos">screen position of the item to check</param>
        /// <param name="size">size of the item to check</param>
        /// <returns>true ifhovered</returns>
        public bool IsItemHovered(Vector2 pos, Vector2 size, bool forcePanelClippingInsidePopup = false, bool allowWhenBlockedByPopup = false) // TODO : Remove this freak once clipping rect stack are handled
        {
            if(Fugui.IsScrolling) // prevent hover state to change during scroll (since mouse pos is changing but we are still on the same item)
            {
                return false;
            }

            Vector2 mousePos = ImGui.GetMousePos();
            bool isDrawingInsidePopup = Fugui.IsDrawingInsidePopup();
            // a popup is drawing
            if (isDrawingInsidePopup)
            {
                // the drawing popup does NOT have the focus
                if (!Fugui.IsDrawingPopupFocused())
                {
                    return false;
                }
            }
            // we are not drawing inside a popup but there is some
            else if (Fugui.IsThereAnyOpenPopup())
            {
                if (!allowWhenBlockedByPopup || Fugui.IsInsideAnyPopup(mousePos))
                {
                    return false;
                }
            }

            bool hovered;

            // we are inside a panel, let's ignore if mouse is outside the panel clipping rect only if ye are NOT inside a popup
            // TODO : Maybe it's a good idea to store a stack of current clipping rect (for panels inside panels or popups drawn by panels => otherwise we MUST draw popups OUTSIDE panels)
            if (FuPanel.IsInsidePanel && (!isDrawingInsidePopup || forcePanelClippingInsidePopup))
            {
                if (!FuPanel.CurrentPanelRect.Contains(mousePos))
                {
                    return false;
                }
            }

            // the element is drawed inside a window
            if (FuWindow.CurrentDrawingWindow != null)
            {
                // get hover state on window
                hovered = FuWindow.CurrentDrawingWindow.IsHovered && mousePos.x > pos.x && mousePos.x < pos.x + size.x && mousePos.y > pos.y && mousePos.y < pos.y + size.y;
            }
            else
            {
                // get hover state
                hovered = mousePos.x > pos.x && mousePos.x < pos.x + size.x && mousePos.y > pos.y && mousePos.y < pos.y + size.y;
            }

            return hovered;
        }

        /// <summary>
        /// Prepare centering for the next item (next item should be a text)
        /// </summary>
        /// <param name="nextItemText">text of the next item</param>
        /// <param name="availWidth">available width, if 0 use all available width, if negative use all available width minus this value</param>
        /// <param name="scale">whatever the avail width must be scaled</param>
        public void CenterNextItemH(string nextItemText, float availWidth = 0f, bool scale = false)
        {
            float txtWidth = ImGui.CalcTextSize(nextItemText).x;
            CenterNextItemH(txtWidth, availWidth, scale);
        }

        /// <summary>
        /// Prepare centering for the next item
        /// </summary>
        /// <param name="itemWidth">width of the next item</param>
        /// <param name="availWidth">available width, if 0 use all available width, if negative use all available width minus this value</param>
        /// <param name="scale">whatever the avail width must be scaled</param>
        public void CenterNextItemH(float itemWidth, float availWidth = 0f, bool scale = false)
        {
            if (availWidth == 0f)
            {
                availWidth = ImGui.GetContentRegionAvail().x;
            }
            else if (availWidth < 0f)
            {
                availWidth = ImGui.GetContentRegionAvail().x - availWidth * Fugui.Scale;
            }
            else if (scale)
            {
                availWidth *= Fugui.Scale;
            }
            float offset = availWidth / 2f - itemWidth / 2f;
            if (offset > 0f)
            {
                Fugui.MoveXUnscaled(offset);
            }
        }

        /// <summary>
        /// Prepare centering for the next item vertically (next item should be a text)
        /// </summary>
        /// <param name="nextItemText"> text of the next item</param>
        /// <param name="availHeight"> max height available, if -1 use all available height</param>
        /// <param name="scale">whatever the avail height must be scaled</param>
        public void CenterNextItemV(string nextItemText, float availHeight = 0, bool scale = false)
        {
            float txtHeight = ImGui.CalcTextSize(nextItemText).y;
            CenterNextItemV(txtHeight, availHeight, scale);
        }

        /// <summary>
        /// Prepare centering for the next item vertically (next item should be a text)
        /// </summary>
        /// <param name="itemHeight"> height of the next item</param>
        /// <param name="availHeight"> max height available, if -1 use all available height</param>
        /// <param name="scale">whatever the avail height must be scaled</param>
        public void CenterNextItemV(float itemHeight, float availHeight = 0, bool scale = false)
        {
            if (availHeight == 0f)
            {
                availHeight = ImGui.GetContentRegionAvail().y;
            }
            else if (availHeight < 0f)
            {
                availHeight = ImGui.GetContentRegionAvail().y - availHeight * Fugui.Scale;
            }
            else if (scale)
            {
                availHeight *= Fugui.Scale;
            }
            float offset = availHeight / 2f - itemHeight / 2f;
            if (offset > 0f)
            {
                Fugui.MoveYUnscaled(offset);
            }
        }

        /// <summary>
        /// Prepare centering for the next item horizontally and vertically (next item should be a text)
        /// </summary>
        /// <param name="nextItemText"> text of the next item</param>
        /// <param name="availWidth"> available width, if 0 use all available width, if negative use all available width minus this value</param>
        /// <param name="availHeight"> max height available, if -1 use all available height</param>
        /// <param name="scale">whatever the avail width and height must be scaled</param>
        public void CenterNextItemHV(string nextItemText, float availWidth = 0f, float availHeight = -1, bool scale = false)
        {
            Vector2 txtSize = ImGui.CalcTextSize(nextItemText);
            float txtWidth = txtSize.x;
            float txtHeight = txtSize.y;
            CenterNextItemH(txtWidth, availWidth, scale);
            CenterNextItemV(txtHeight, availHeight, scale);
        }

        /// <summary>
        /// Prepare centering for the next item horizontally and vertically
        /// </summary>
        /// <param name="itemWidth"> width of the next item</param>
        /// <param name="itemHeight"> height of the next item</param>
        /// <param name="availWidth"> available width, if 0 use all available width, if negative use all available width minus this value</param>
        /// <param name="availHeight"> max height available, if -1 use all available height</param>
        /// <param name="scale">whatever the avail width and height must be scaled</param>
        public void CenterNextItemHV(float itemWidth, float itemHeight, float availWidth = 0f, float availHeight = -1, bool scale = false)
        {
            CenterNextItemH(itemWidth, availWidth, scale);
            CenterNextItemV(itemHeight, availHeight, scale);
        }

        /// <summary>
        /// Gets the string format for the given id and value
        /// </summary>
        /// <param name="id">ID of the UIElement</param>
        /// <param name="value">float Value</param>
        /// <returns>The result of the operation.</returns>
        private static unsafe string getStringFormat(float value)
        {
            // Fast path: if focused
            if (ImGuiNative.igIsItemFocused() != 0)
                return "%.4f";

            if (isDisplayZero(value))
                return "%.0f";

            // Fast no alloc path: get exponent
            int bits = *(int*)&value;
            int exponent = ((bits >> 23) & 0xFF) - 127;
            if (exponent >= 23) // No decimals
                return "%.0f";

            // Determine number of decimals based on value magnitude
            int nbDec = 0;
            float absVal = value < 0 ? -value : value;
            if (absVal < 1e-7f) nbDec = 8;
            else if (absVal < 1e-6f) nbDec = 7;
            else if (absVal < 1e-5f) nbDec = 6;
            else if (absVal < 1e-4f) nbDec = 5;
            else if (absVal < 1e-3f) nbDec = 4;
            else if (absVal < 1e-2f) nbDec = 3;
            else if (absVal < 1e-1f) nbDec = 2;
            else if (absVal < 1f) nbDec = 1;
            else nbDec = 0;

            // Clamp to max 8 decimals
            if (nbDec > 8) nbDec = 8;
            return formats[nbDec];
        }

        #region State
        private const float ZeroDisplayEpsilon = 1e-6f;

        private static readonly string[] formats = new[]
        {
            "%.0f", "%.1f", "%.2f", "%.3f", "%.4f", "%.5f", "%.6f", "%.7f", "%.8f"
        };
        #endregion

        #region Methods
        private static bool isDisplayZero(float value)
        {
            return value > -ZeroDisplayEpsilon && value < ZeroDisplayEpsilon;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Displays a tooltip if the current element is hovered over, or if force is set to true.
        /// </summary>
        /// <param name="force">Whether to force display the tooltip or not.</param>
        protected void displayToolTip(bool force = false, bool ignoreAvancement = false)
        {
            // If there are tooltips to display and we haven't displayed all of them yet
            if (_currentToolTips != null && _currentToolTipsIndex < _currentToolTips.Length)
            {
                // If the element is hovered over or force is set to true
                if (force || _lastItemHovered)
                {
                    FuTextStyle style = FuTextStyle.Default;
                    // push tooltip styles
                    if (_currentToolTipsStyles != null && _currentToolTipsIndex < _currentToolTipsStyles.Length)
                    {
                        style = _currentToolTipsStyles[_currentToolTipsIndex];
                    }

                    SetToolTip(_lastItemID, _currentToolTips[_currentToolTipsIndex], _lastItemHovered, !LastItemDisabled, style);
                }
                // cancel smooth tooltip display
                else if (_lastItemID == _currentHoveredElementId)
                {
                    _currentHoveredElementId = string.Empty;
                }

                // is we want to ignore tooltip avancement, let's return without increment the index
                if (ignoreAvancement)
                {
                    return;
                }
                // Move on to the next tooltip
                _currentToolTipsIndex++;
                // If we have displayed all the tooltips, reset the current tooltips array
                if (_currentToolTipsIndex >= _currentToolTips.Length)
                {
                    _currentToolTips = null;
                }
            }
        }

        /// <summary>
        /// Imediate Display a tooltip
        /// </summary>
        /// <param name="id">unique id of the tooltip</param>
        /// <param name="text">text of the tooltip</param>
        /// <param name="hoveredState">whatever the item is hovered</param>
        public void SetToolTip(string id, string text, bool hoveredState)
        {
            SetToolTip(id, text, hoveredState, true, FuTextStyle.Default);
        }

        /// <summary>
        /// Imediate Display a tooltip
        /// </summary>
        /// <param name="id">unique id of the tooltip</param>
        /// <param name="text">text of the tooltip</param>
        /// <param name="hoveredState">whatever the item is hovered</param>
        /// <param name="enabled">whatever the item is enabled</param>
        /// <param name="style">style on the tooltip</param>
        public void SetToolTip(string id, string text, bool hoveredState, bool enabled, FuTextStyle style)
        {
            SetToolTip(id, () =>
            {
                style.Push(enabled);
                ImGui.Text(text);
                style.Pop();
            }, hoveredState);
        }

        /// <summary>
        /// Imediate Display a tooltip
        /// </summary>
        /// <param name="id">unique id of the tooltip</param>
        /// <param name="callback">callback to draw inside tooltip popup</param>
        /// <param name="hoveredState">whatever the item is hovered</param>
        public void SetToolTip(string id, Action callback, bool hoveredState)
        {
            if (hoveredState)
            {
                // handle delayed display
                if (id != _currentHoveredElementId)
                {
                    _currentHoveredElementId = id;
                    _currentHoveredStartHoverTime = Fugui.Time;
                }

                if (Fugui.Time - _currentHoveredStartHoverTime >= _tooltipAppearDuration)
                {
                    // set padding and font
                    Fugui.PushDefaultFont();
                    Fugui.Push(ImGuiStyleVar.WindowPadding, new Vector4(8f, 4f));
                    // Display the current tooltip
                    ImGui.BeginTooltip();
                    callback?.Invoke();
                    ImGui.EndTooltip();
                    Fugui.PopStyle();
                    Fugui.PopFont();
                }
            }
            else if (id == _currentHoveredElementId)
            {
                _currentHoveredElementId = string.Empty;
            }
        }

        /// <summary>
        /// Returns the try get enum values result.
        /// </summary>
        /// <param name="type">The type value.</param>
        /// <param name="values">The values value.</param>
        /// <param name="strValues">The str Values value.</param>
        /// <returns>The result of the operation.</returns>
        protected bool tryGetEnumValues<TEnum>(Type type, out List<IConvertible> values, out List<string> strValues)
        {
            values = null;
            strValues = null;
            if (!type.IsEnum)
            {
                Debug.LogError("TEnum must be an enumerated type.");
                return false;
            }

            // check whatever the enum type is already knew
            if (!_enumValues.ContainsKey(type))
            {
                // list to store the enum values
                values = new List<IConvertible>();
                // list to store the combobox items
                strValues = new List<string>();
                // iterate over the enum values and add them to the lists
                foreach (IConvertible enumValue in Enum.GetValues(typeof(TEnum)))
                {
                    values.Add(enumValue);
                    strValues.Add(Fugui.AddSpacesBeforeUppercase(enumValue.ToString()));
                }
                // store enum values and string into dict
                _enumValues.Add(type, values);
                _enumValuesString.Add(type, strValues);
            }

            // set lists values from dict
            values = _enumValues[type];
            strValues = _enumValuesString[type];

            return true;
        }
        #endregion

        #region State
        private static string _activeItem = null;

        public static bool IsAnyItemActive => !string.IsNullOrEmpty(_activeItem);
        #endregion

        #region Methods
        /// <summary>
        /// Set the states of an items for this frame (until another item is draw)
        ///     LastItemHovered
        ///     LastItemClickedButton
        ///     LastItemJustDeactivated
        ///     LastItemActive
        ///     LastItemUpdate
        /// </summary>
        /// <param name="uniqueID"></param>
        /// <param name="pos"></param>
        /// <param name="size"></param>
        /// <param name="clickable"></param>
        /// <param name="updated"></param>
        /// <param name="updateOnClick"></param>
        protected void setBaseElementState(string uniqueID, Vector2 pos, Vector2 size, bool clickable, bool updated, bool updateOnClick = false, bool allowWhenBlockedByPopup = false)
        {
            _lastItemRect = new Rect(pos, size);

            if (LastItemDisabled)
            {
                _lastItemHovered = IsItemHovered(pos, size, false, allowWhenBlockedByPopup);
                if (_activeItem == uniqueID)
                {
                    _activeItem = null;
                }
                return;
            }

            _lastItemHovered = IsItemHovered(pos, size, false, allowWhenBlockedByPopup);

            if (clickable)
            {
                // Activation on press
                if (_lastItemHovered && Fugui.GetCurrentMouse().IsDown(FuMouseButton.Left))
                {
                    _activeItem = uniqueID;
                    _lastItemJustActivated = true;
                }

                if (_lastItemHovered && Fugui.GetCurrentMouse().IsDown(FuMouseButton.Right))
                {
                    _lastItemClickedButton = FuMouseButton.Right;
                }

                // Click validation on release, based on active item rather than current hover
                if (_activeItem == uniqueID)
                {
                    if (Fugui.GetCurrentMouse().IsClicked(FuMouseButton.Left))
                    {
                        _lastItemClickedButton = FuMouseButton.Left;

                        if (updateOnClick)
                        {
                            updated = true;
                        }
                    }
                }

                if (_activeItem == uniqueID && Fugui.GetCurrentMouse().IsUp(FuMouseButton.Left))
                {
                    _activeItem = null;
                    _lastItemJustDeactivated = true;
                }
            }

            _lastItemActive = _activeItem == uniqueID;
            _lastItemUpdate = updated;
        }
        #endregion
    }
}
