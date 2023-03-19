using Fu.Core;
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
        #region Variables
        /// <summary>
        /// The current Layout or grid that is drawing at time.
        /// </summary>
        public static FuLayout CurrentDrawer { get; protected set; } = null;
        /// <summary>
        /// The ID of the window in wich a popup is open (if there is some)
        /// </summary>
        public static string CurrentPopUpWindowID { get; private set; } = null;
        /// <summary>
        /// The ID of the currently open pop-up (if there is some)
        /// </summary>
        public static string CurrentPopUpID { get; private set; } = null;
        /// <summary>
        /// The Rect of the currently open pop-up (if there is some)
        /// </summary>
        public static Rect CurrentPopUpRect { get; private set; } = default;
        /// <summary>
        /// A flag indicating whether the layout is inside a pop-up.
        /// </summary>
        public static bool IsInsidePopUp { get; private set; } = false;
        /// <summary>
        /// A flag indicating last drawed item is hovered by current pointer.
        /// </summary>
        public bool LastItemHovered { get => _lastItemHovered; }
        private static bool _lastItemHovered = false;
        /// <summary>
        /// A flag indicating last drawed item is currently used by current pointer.
        /// </summary>
        public bool LastItemActive { get => _lastItemActive; }
        private static bool _lastItemActive = false;
        /// <summary>
        /// A flag indicating last drawed item was active laft frame and is no more this frame.
        /// </summary>
        public bool LastItemJustDeactivated { get => _lastItemJustDeactivated; }
        private static bool _lastItemJustDeactivated = false;
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
        private static Vector2 _currentItemStartPos;
        // whatever elements are currently disabled (if true)
        private bool _longDisabled = false;
        // the time before an hovered element display it's tooltip
        private static float _tooltipAppearDuration = 1.0f;
        // the time at the fame the current hovered element start to be hovered
        private static float _currentHoveredStartHoverTime = 0f;
        // the ID if the element that want to display tooltips
        private static string _currentHoveredElementId = string.Empty;
        #endregion

        #region Elements Data
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
        #endregion

        public FuLayout()
        {
            CurrentDrawer = this;
            _tooltipAppearDuration = 1.0f;
        }

        /// <summary>
        /// Disposes this object.
        /// </summary>
        public virtual void Dispose()
        {
            CurrentDrawer = null;
        }

        #region elements utils
        /// <summary>
        /// Begins an element in this layout with the specified style.
        /// </summary>
        /// <param name="style">The style to use for this element.</param>
        protected virtual void beginElement(ref string elementID, IFuElementStyle style = null, bool noEditID = false, bool canBeHidden = true)
        {
            _lastItemActive = false;
            _lastItemHovered = false;
            _lastItemJustDeactivated = false;
            _lastItemUpdate = false;
            _lastItemClickedButton = FuMouseButton.None;

            // whatever we must draw the next item
            _drawElement = true;
            if (!IsInsidePopUp && FuPanel.IsInsidePanel && FuPanel.Clipper != null)
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
                drawHoverFrame();
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
            if (!IsInsidePopUp && FuPanel.IsInsidePanel && FuPanel.Clipper != null)
            {
                FuPanel.Clipper.EndDrawElement();
            }
        }

        /// <summary>
        /// Draws a hover frame around the current element if needed.
        /// </summary>
        private void drawHoverFrame()
        {
            if (_elementHoverFramedEnabled && !LastItemDisabled)
            {
                if (ImGuiNative.igIsItemFocused() != 0)
                {
                    ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.GetColorU32(FuThemeManager.GetColor(FuColors.FrameSelectedFeedback)), ImGui.GetStyle().FrameRounding);
                }
                else if (ImGui.IsItemHovered())
                {
                    // ImGui fail on inputText since version 1.88, check on new version
                    ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.GetColorU32(FuThemeManager.GetColor(FuColors.FrameHoverFeedback)), ImGui.GetStyle().FrameRounding);
                }
            }
        }

        /// <summary>
        /// Draws a frame border at the given rect.
        /// </summary>
        /// <param name="rect">rect of the frame (pos + size)</param>
        private void drawBorderFrame(Rect rect, bool rounded = true)
        {
            ImGui.GetWindowDrawList().AddRect(rect.min, rect.max, ImGui.GetColorU32(FuThemeManager.GetColor(FuColors.FrameSelectedFeedback)), rounded ? ImGui.GetStyle().FrameRounding : 0f);
        }
        #endregion

        #region ToopTips
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
        #endregion

        #region Public Utils
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
        /// </summary>
        /// <param name="size">size of the dummy</param>
        private void Dummy(Rect size)
        {
            ImGuiNative.igDummy(size.size);
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
            return ImGui.GetContentRegionAvail();
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
            return ImGui.GetContentRegionAvail().y;
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
        #endregion

        #region Center Item
        /// <summary>
        /// Prepare centering for the next item (next item should be a text)
        /// </summary>
        /// <param name="nextItemText">text of the next item</param>
        public void CenterNextItem(string nextItemText)
        {
            float txtWidth = ImGui.CalcTextSize(nextItemText).x;
            float avWidth = ImGui.GetContentRegionAvail().x;
            Dummy(avWidth / 2f - txtWidth / 2f);
            SameLine();
        }

        /// <summary>
        /// Prepare centering for the next item
        /// </summary>
        /// <param name="itemWidth">width of the next item</param>
        public void CenterNextItem(float itemWidth)
        {
            float avWidth = ImGui.GetContentRegionAvail().x;
            Dummy(avWidth / 2f - itemWidth / 2f);
            SameLine();
        }
        #endregion

        #region private utils
        #region string formats
        /// <summary>
        /// Gets the string format for the given id and value
        /// </summary>
        /// <param name="id">ID of the UIElement</param>
        /// <param name="value">float Value</param>
        /// <returns></returns>
        private string getStringFormat(float value)
        {
            // If the element is focused, set the string format to 4 decimal places
            if (ImGuiNative.igIsItemFocused() != 0)
            {
                return "%.4f";
            }

            // Split the value by the decimal point
            string v = value.ToString();
            string[] spl = v.Split(',');
            // If there is a decimal point, set the string format to the number of decimal places (up to 8)
            if (spl.Length > 1)
            {
                int nbDec = Math.Min(8, spl[1].TrimEnd('0').Length);
                return "%." + nbDec.ToString() + "f";
            }
            // Otherwise, set the string format to 0 decimal places
            return "%.0f";
        }
        #endregion

        #region tooltip utils
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
                if (force || _lastItemHovered || ImGui.IsItemHovered())
                {
                    FuTextStyle style = FuTextStyle.Default;
                    // push tooltip styles
                    if (_currentToolTipsStyles != null && _currentToolTipsIndex < _currentToolTipsStyles.Length)
                    {
                        style = _currentToolTipsStyles[_currentToolTipsIndex];
                    }

                    setToolTip(_lastItemID, _currentToolTips[_currentToolTipsIndex], style);
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
        /// <param name="text">text of the tooltip</param>
        /// <param name="style">style on the tooltip</param>
        private void setToolTip(string ID, string text, FuTextStyle style)
        {
            // handle delayed display
            if (ID != _currentHoveredElementId)
            {
                _currentHoveredElementId = ID;
                _currentHoveredStartHoverTime = Fugui.Time;
            }

            if (Fugui.Time - _currentHoveredStartHoverTime >= _tooltipAppearDuration)
            {
                style.Push(!LastItemDisabled);
                // set padding and font
                Fugui.PushDefaultFont();
                Fugui.Push(ImGuiStyleVar.WindowPadding, new Vector4(8f, 4f));
                // Display the current tooltip
                if (LastItemDisabled)
                {
                    ImGui.SetTooltip("(Disabled) : " + text);
                }
                else
                {
                    ImGui.SetTooltip(text);
                }
                Fugui.PopStyle();
                Fugui.PopFont();
                style.Pop();
            }
        }
        #endregion

        #region enum utils
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

        #region element state
        private static string _activeItem = null;
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
        protected void setBaseElementState(string uniqueID, Vector2 pos, Vector2 size, bool clickable, bool updated, bool updateOnClick = false)
        {
            // do nothing if the item is disabled
            if (LastItemDisabled)
            {
                // the item is disabled but it was the last active
                if (_activeItem == uniqueID)
                {
                    _activeItem = null;
                }
                return;
            }

            // get hover state
            _lastItemHovered = isItemHovered(pos, size);

            // get click state
            if (clickable && _lastItemHovered)
            {
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    _lastItemClickedButton = FuMouseButton.Left;
                    if (updateOnClick)
                    {
                        updated = true;
                    }
                    _activeItem = uniqueID;
                }
                else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                {
                    _lastItemClickedButton = FuMouseButton.Right;
                }
            }

            // get deactivated state
            _lastItemJustDeactivated = _activeItem == uniqueID && ImGui.IsMouseReleased(ImGuiMouseButton.Left);
            if (_lastItemJustDeactivated)
            {
                _activeItem = null;
            }
            // get active state
            _lastItemActive = _activeItem == uniqueID;
            // force full FPS the current active window
            if (_lastItemActive)
            {
                FuWindow.CurrentDrawingWindow?.ForceDraw();
            }
            // get update state
            _lastItemUpdate = updated;
        }

        /// <summary>
        /// Clean check whatever an item is hovered
        /// </summary>
        /// <param name="pos">screen position of the item to check</param>
        /// <param name="size">size of the item to check</param>
        /// <returns>true ifhovered</returns>
        protected bool isItemHovered(Vector2 pos, Vector2 size)
        {
            // we are NOT inside a popup but there is a popup, assuming we can't hover anything
            if (!IsInsidePopUp && !string.IsNullOrEmpty(CurrentPopUpID))
            {
                return false;
            }

            bool hovered;
            Vector2 mousePos = ImGui.GetMousePos();
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
    }
    #endregion
    #endregion
}