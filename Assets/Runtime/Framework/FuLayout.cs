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
        // The current pop-up window ID.
        public static string CurrentPopUpWindowID { get; private set; } = null;
        // The current pop-up ID.
        public static string CurrentPopUpID { get; private set; } = null;
        // The current pop-up Rect.
        public static Rect CurrentPopUpRect { get; private set; } = default;
        // A flag indicating whether the layout is inside a pop-up.
        public static bool IsInsidePopUp { get; private set; } = false;
        // A flag indicating whether the element is hover framed.
        private bool _elementHoverFramed = false;
        // A flag indicating whether the next element should be disabled.
        protected bool _nextIsDisabled;
        // An array of strings representing the current tool tips.
        protected string[] _currentToolTips = null;
        // An integer representing the current tool tips index.
        protected int _currentToolTipsIndex = 0;
        // whatever tooltip must be display hover Labels
        protected bool _currentToolTipsOnLabels = false;
        // has animations enabled
        protected bool _animationEnabled = true;
        #endregion

        #region Elements Data
        // A set of strings representing the dragging sliders.
        private static HashSet<string> _draggingSliders = new HashSet<string>();
        // A dictionary of integers representing the combo selected indices.
        private static Dictionary<string, int> _comboSelectedIndices = new Dictionary<string, int>();
        // A dictionary of integers representing the listbox selected indices.
        private static Dictionary<string, int> _listboxSelectedItem = new Dictionary<string, int>();
        // A dictionary that store displaying toggle data.
        private static Dictionary<string, FuElementAnimationData> _uiElementAnimationDatas = new Dictionary<string, FuElementAnimationData>();
        // A dictionary that store displaying toggle data.
        private static Dictionary<string, int> _buttonsGroupIndex = new Dictionary<string, int>();
        // A dictionary of strings representing the current PathFields string value.
        private static Dictionary<string, string> _pathFieldValues = new Dictionary<string, string>();
        // A dictionary to store enum values according to the type of the enum
        private static Dictionary<Type, List<IConvertible>> _enumValues = new Dictionary<Type, List<IConvertible>>();
        // A dictionary to store enum values as string according to the type of the enum
        private static Dictionary<Type, List<string>> _enumValuesString = new Dictionary<Type, List<string>>();
        protected bool _drawItem = true;
        #endregion

        #region Layout
        /// <summary>
        /// Disposes this object.
        /// </summary>
        public virtual void Dispose()
        {
        }

        /// <summary>
        /// Disables the next element in this layout.
        /// </summary>
        public void DisableNextElement()
        {
            _nextIsDisabled = true;
        }

        /// <summary>
        /// Begins an element in this layout with the specified style.
        /// </summary>
        /// <param name="style">The style to use for this element.</param>
        protected virtual void beginElement(ref string elementID, IFuElementStyle style = null, bool noEditID = false, bool canBeHidden = true)
        {
            // whatever we must draw the next item
            _drawItem = true;
            if(FuPanel.IsInsidePanel && FuPanel.Clipper != null)
            {
                _drawItem = FuPanel.Clipper.BeginDrawElement(canBeHidden);
            }
            if (_drawItem)
            {
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
            if (_drawItem)
            {
                style?.Pop();
                drawHoverFrame();
            }
            _nextIsDisabled = false;
            _elementHoverFramed = false;
            if (FuPanel.IsInsidePanel && FuPanel.Clipper != null)
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
                else if (ImGuiNative.igIsItemHovered(ImGuiHoveredFlags.None) != 0)
                {
                    // ImGui fail on inputText since version 1.88, check on new version
                    ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.GetColorU32(FuThemeManager.GetColor(FuColors.FrameHoverFeedback)), ImGui.GetStyle().FrameRounding);
                }
            }
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
                if (force || ImGuiNative.igIsItemHovered(ImGuiHoveredFlags.None) != 0)
                {
                    // set padding and font
                    Fugui.PushDefaultFont();
                    Fugui.Push(ImGuiStyleVar.WindowPadding, new Vector4(8f, 4f));
                    // Display the current tooltip
                    if (_nextIsDisabled)
                    {
                        ImGui.SetTooltip("(Disabled) : " + _currentToolTips[_currentToolTipsIndex]);
                    }
                    else
                    {
                        ImGui.SetTooltip(_currentToolTips[_currentToolTipsIndex]);
                    }
                    Fugui.PopFont();
                    Fugui.PopStyle();
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
        #endregion
    }
}