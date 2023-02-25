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
        public static bool LastItemHovered { get; private set; } = false;
        /// <summary>
        /// A flag indicating last drawed item is currently used by current pointer.
        /// </summary>
        public static bool LastItemActive { get; private set; } = false;
        /// <summary>
        /// A flag indicating last drawed item was active laft frame and is no more this frame.
        /// </summary>
        public static bool LastItemJustDeactivated { get; private set; } = false;
        /// <summary>
        /// A flag indicating last drawed item has just done an update operation this frame.
        /// </summary>
        public static bool LastItemUpdate { get; private set; } = false;
        /// <summary>
        /// A flag indicating last drawed item has just done an update operation this frame.
        /// </summary>
        public static FuMouseButton LastItemClickedButton { get; private set; } = FuMouseButton.None;

        // A flag indicating whether the element is hover framed.
        private bool _elementHoverFramed = false;
        // A flag indicating whether the next element should be disabled.
        protected bool _nextIsDisabled;
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

        #region Layout
        public FuLayout()
        {
            CurrentDrawer = this;
        }

        /// <summary>
        /// Disposes this object.
        /// </summary>
        public virtual void Dispose()
        {
            CurrentDrawer = null;
        }

        /// <summary>
        /// Disables the next element in this layout.
        /// </summary>
        public void DisableNextElement()
        {
            _nextIsDisabled = true;
        }

        /// <summary>
        /// Disables all next elements in this layout from now.
        /// Call 'EnableNextElements' to stop disabling
        /// </summary>
        public void DisableNextElements()
        {
            _nextIsDisabled = true;
            _longDisabled = true;
        }

        /// <summary>
        /// Enables all next element in this layout.
        /// </summary>
        public void EnableNextElements()
        {
            _nextIsDisabled = false;
            _longDisabled = false;
        }

        /// <summary>
        /// Begins an element in this layout with the specified style.
        /// </summary>
        /// <param name="style">The style to use for this element.</param>
        protected virtual void beginElement(ref string elementID, IFuElementStyle style = null, bool noEditID = false, bool canBeHidden = true)
        {
            LastItemActive = false;
            LastItemHovered = false;
            LastItemJustDeactivated = false;
            LastItemUpdate = false;
            LastItemClickedButton = FuMouseButton.None;

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
                style?.Push(!_nextIsDisabled);
                if (!noEditID && FuWindow.CurrentDrawingWindow != null)
                {
                    elementID = elementID + "##" + FuWindow.CurrentDrawingWindow.ID;
                }
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
                if(LastItemClickedButton == FuMouseButton.Right)
                {
                    Fugui.TryOpenContextMenu();
                }
            }
            if (!_longDisabled)
            {
                _nextIsDisabled = false;
            }
            _elementHoverFramed = false;
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
            if (_elementHoverFramed && !_nextIsDisabled)
            {
                if (ImGuiNative.igIsItemFocused() != 0)
                {
                    ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.GetColorU32(FuThemeManager.GetColor(FuColors.FrameSelectedFeedback)), ImGui.GetStyle().FrameRounding);
                }
                else if (LastItemHovered)
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

        /// <summary>
        /// Add a line under the last drawed element
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

        #region private utils
        #region drag decimals
        /// <summary>
        /// Gets the string format for the given id and value
        /// </summary>
        /// <param name="id">ID of the UIElement</param>
        /// <param name="value">float Value</param>
        /// <returns></returns>
        private string getFloatString(float value)
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

        #region string utils
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
                if (force || LastItemHovered || ImGui.IsItemHovered())
                {
                    FuTextStyle style = FuTextStyle.Default;
                    // push tooltip styles
                    if (_currentToolTipsStyles != null && _currentToolTipsIndex < _currentToolTipsStyles.Length)
                    {
                        style = _currentToolTipsStyles[_currentToolTipsIndex];
                    }

                    SetToolTip(_currentToolTips[_currentToolTipsIndex], style);
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
        public void SetToolTip(string text, FuTextStyle style)
        {
            style.Push(!_nextIsDisabled);
            // set padding and font
            Fugui.PushDefaultFont();
            Fugui.Push(ImGuiStyleVar.WindowPadding, new Vector4(8f, 4f));
            // Display the current tooltip
            if (_nextIsDisabled)
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
            if (_nextIsDisabled)
            {
                // the item is disabled but it was the last active
                if (_activeItem == uniqueID)
                {
                    _activeItem = null;
                }
                return;
            }

            // get hover state
            LastItemHovered = isItemHovered(pos, size);

            // get click state
            if (clickable && LastItemHovered)
            {
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    LastItemClickedButton = FuMouseButton.Left;
                    if (updateOnClick)
                    {
                        updated = true;
                    }
                    _activeItem = uniqueID;
                }
                else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                {
                    LastItemClickedButton = FuMouseButton.Right;
                }
            }

            // get deactivated state
            LastItemJustDeactivated = _activeItem == uniqueID && ImGui.IsMouseReleased(ImGuiMouseButton.Left);
            if (LastItemJustDeactivated)
            {
                _activeItem = null;
            }
            // get active state
            LastItemActive = _activeItem == uniqueID;
            // get update state
            LastItemUpdate = updated;
        }

        /// <summary>
        /// Clean check whatever an item is hovered
        /// </summary>
        /// <param name="pos">screen position of the item to check</param>
        /// <param name="size">size of the item to check</param>
        /// <returns>true ifhovered</returns>
        protected bool isItemHovered(Vector2 pos, Vector2 size)
        {
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

            // we are NOT inside a popup but there is a popup, assuming we can't hover anything
            if (!IsInsidePopUp && !string.IsNullOrEmpty(CurrentPopUpID))
            {
                hovered = false;
            }
            return hovered;
        }
    }
    #endregion
    #endregion
}