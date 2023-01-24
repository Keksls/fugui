using Fugui.Core;
using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fugui.Framework
{
    /// <summary>
    /// Represents the base class for creating user interface layouts.
    /// </summary>
    public partial class UILayout : IDisposable
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
        protected bool _animationEnabled = true;
        #endregion

        #region Elements Data
        // A set of strings representing the dragging sliders.
        private static HashSet<string> _draggingSliders = new HashSet<string>();
        // A dictionary of strings representing the drag string formats.
        private static Dictionary<string, string> _dragStringFormats = new Dictionary<string, string>();
        // A dictionary of integers representing the combo selected indices.
        private static Dictionary<string, int> _comboSelectedIndices = new Dictionary<string, int>();
        // A dictionary that store displaying toggle data.
        private static Dictionary<string, UIElementAnimationData> _uiElementAnimationDatas = new Dictionary<string, UIElementAnimationData>();
        // A dictionary that store displaying toggle data.
        private static Dictionary<string, int> _buttonsGroupIndex = new Dictionary<string, int>();
        // A dictionary of strings representing the current PathFields string value.
        private static Dictionary<string, string> _pathFieldValues = new Dictionary<string, string>();
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
        protected virtual string beginElement(string elementID, IUIElementStyle style = null)
        {
            style?.Push(!_nextIsDisabled);
            return elementID + "##" + (UIWindow.CurrentDrawingWindow?.ID ?? "");
        }

        /// <summary>
        /// Ends an element in this layout with the specified style.
        /// </summary>
        /// <param name="style">The style to use for this element.</param>
        protected virtual void endElement(IUIElementStyle style = null)
        {
            style?.Pop();
            drawHoverFrame();
            _nextIsDisabled = false;
            _elementHoverFramed = false;
        }

        /// <summary>
        /// Draws a hover frame around the current element if needed.
        /// </summary>
        private void drawHoverFrame()
        {
            if (_elementHoverFramed && !_nextIsDisabled)
            {
                if (ImGui.IsItemFocused())
                {
                    ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.GetColorU32(ThemeManager.GetColor(FuguiColors.FrameSelectedFeedback)), ImGui.GetStyle().FrameRounding);
                }
                else if (ImGui.IsItemHovered())
                {
                    ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.GetColorU32(ThemeManager.GetColor(FuguiColors.FrameHoverFeedback)), ImGui.GetStyle().FrameRounding);
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
            ImGui.Separator();
        }

        /// <summary>
        /// Draw a space of size ItemSpacing of the current theme
        /// </summary>
        public void Spacing()
        {
            ImGui.Spacing();
        }

        /// <summary>
        /// Draw the next element on Same Line as current
        /// </summary>
        public void SameLine()
        {
            ImGui.SameLine();
        }

        /// <summary>
        /// Draw an empty dummy element of size x y
        /// </summary>
        /// <param name="x">width of the dummy</param>
        /// <param name="y">height of the dummy</param>
        public void Dummy(float x = 0f, float y = 0f)
        {
            ImGui.Dummy(new Vector2(x, y));
        }

        /// <summary>
        /// Draw an empty dummy element of size 'size'
        /// </summary>
        /// <param name="size">size of the dummy</param>
        public void Dummy(Vector2 size)
        {
            ImGui.Dummy(size);
        }

        #region private utils
        #region drag decimals
        /// <summary>
        /// Gets the string format for the given id and value
        /// </summary>
        /// <param name="id">ID of the UIElement</param>
        /// <param name="value">float Value</param>
        /// <returns></returns>
        private string getFloatString(string id, float value)
        {
            // If the string format doesn't exist for this id, create it
            if (!_dragStringFormats.ContainsKey(id))
            {
                updateFloatString(id, value);
            }
            // Return the string format for this id
            return _dragStringFormats[id];
        }

        /// <summary>
        /// Update the string format for the given id and value
        /// </summary>
        /// <param name="id">ID of the UIElement</param>
        /// <param name="value">float Value</param>
        private void updateFloatString(string id, float value)
        {
            // If the string format doesn't exist for this id, add it with a default value
            if (!_dragStringFormats.ContainsKey(id))
            {
                _dragStringFormats.Add(id, "%.2f");
            }

            // If the element is focused, set the string format to 4 decimal places
            if (ImGui.IsItemFocused())
            {
                _dragStringFormats[id] = $"%.4f";
                return;
            }
            // Split the value by the decimal point
            string v = value.ToString();
            string[] spl = v.Split(',');
            // If there is a decimal point, set the string format to the number of decimal places (up to 8)
            if (spl.Length > 1)
            {
                int nbDec = Math.Min(8, spl[1].TrimEnd('0').Length);
                _dragStringFormats[id] = $"%.{nbDec}f";
                return;
            }
            // Otherwise, set the string format to 0 decimal places
            _dragStringFormats[id] = "%.0f";
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
                if (force || ImGui.IsItemHovered())
                {
                    // set padding and font
                    FuGui.PushDefaultFont();
                    FuGui.Push(ImGuiStyleVar.WindowPadding, new Vector4(8f, 4f));
                    // Display the current tooltip
                    if (_nextIsDisabled)
                    {
                        ImGui.SetTooltip("(Disabled) : " + _currentToolTips[_currentToolTipsIndex]);
                    }
                    else
                    {
                        ImGui.SetTooltip(_currentToolTips[_currentToolTipsIndex]);
                    }
                    FuGui.PopFont();
                    FuGui.PopStyle();
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
        #endregion
    }
}