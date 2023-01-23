using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fugui.Framework
{
    public partial class UIGrid : UILayout
    {
        #region Variables
        // comment : The current column index being used in the grid
        private int _currentColIndex = 0;
        // The current row index being used in the grid
        private int _currentRowIndex = 0;
        // The current grid definition
        private UIGridDefinition _currentGridDef;
        // Flag to indicate if labels should be automatically drawn
        private bool _autoDrawLabel = true;
        // Flag to indicate if labels should not be disabled
        private bool _dontDisableLabels = true;
        // Flag to indicate if labels should always have tooltip
        private bool _alwaysAutoTooltipsOnLabels = false;
        // The name of the grid
        private string _gridName;
        // Flag to indicate if the grid has been created
        private bool _gridCreated = false;
        // The minimum line height for elements in the grid
        private float _minLineHeight = 20f;
        // The Y padding to draw on top of the next element of the list
        private float _nextElementYPadding = 0f;
        // The y-coordinate of the cursor position when the current group of elements began
        private float _beginElementCursorY = 0;
        // Flag to indicate if the grid is responsively resized
        private bool _isResponsivelyResized = false;
        // A dictionary to store grid descriptions for different types
        private static Dictionary<Type, UIObjectDescription> _objectsDescriptions = new Dictionary<Type, UIObjectDescription>();
        #endregion

        /// <summary>
        /// Create a new UI Grid
        /// </summary>
        /// <param name="gridName">Unique Name of the Grid</param>
        /// <param name="flags">Bitmask that constraint specific grid behaviour</param>
        /// <param name="rowsPadding">spaces in pixel between rows</param>
        /// <param name="cellPadding">spaces in pixel between cells</param>
        /// <param name="outterPadding">grid outter padding. Represent the space at the Left and Right of the Grid</param>
        public UIGrid(string gridName, UIGridFlag flags = UIGridFlag.Default, float cellPadding = 8f, float rowsPadding = 2f, float outterPadding = 4f)
        {
            _autoDrawLabel = !flags.HasFlag(UIGridFlag.NoAutoLabels);
            _dontDisableLabels = flags.HasFlag(UIGridFlag.DoNotDisableLabels);
            _alwaysAutoTooltipsOnLabels = flags.HasFlag(UIGridFlag.AutoToolTipsOnLabels);
            _currentGridDef = UIGridDefinition.DefaultFixed;
            _gridName = gridName;
            setGrid(flags.HasFlag(UIGridFlag.LinesBackground), cellPadding, rowsPadding, outterPadding);
        }

        /// <summary>
        /// Create a new UI Grid
        /// </summary>
        /// <param name="gridName">Unique Name of the Grid</param>
        /// <param name="gridDef">Definition of the Grid. Assume grid behaviour and style. Can be fully custom or use a presset (UIGridDefinition.XXX)</param>
        /// <param name="flags">Bitmask that constraint specific grid behaviour</param>
        /// <param name="rowsPadding">spaces in pixel between rows</param>
        /// <param name="cellPadding">spaces in pixel between cells</param>
        /// <param name="outterPadding">grid outter padding. Represent the space at the Left and Right of the Grid</param>
        public UIGrid(string gridName, UIGridDefinition gridDef, UIGridFlag flags = UIGridFlag.Default, float cellPadding = 8f, float rowsPadding = 2f, float outterPadding = 4f)
        {
            _autoDrawLabel = !flags.HasFlag(UIGridFlag.NoAutoLabels);
            _dontDisableLabels = flags.HasFlag(UIGridFlag.DoNotDisableLabels);
            _alwaysAutoTooltipsOnLabels = flags.HasFlag(UIGridFlag.AutoToolTipsOnLabels);
            _currentGridDef = gridDef;
            _gridName = gridName;
            setGrid(flags.HasFlag(UIGridFlag.LinesBackground), cellPadding, rowsPadding, outterPadding);
        }

        #region Grid
        /// <summary>
        /// Switch from current column to the next.
        /// this is automaticaly call, use it only for custom cases
        /// </summary>
        public void NextColumn()
        {
            if (!_gridCreated)
            {
                return;
            }
            ImGui.TableNextColumn();
            if (_isResponsivelyResized)
            {
                _currentRowIndex++;
                _currentColIndex = 0;
            }
            else
            {
                _currentColIndex++;
                if (_currentColIndex >= _currentGridDef.NbColumns)
                {
                    _currentColIndex = 0;
                    _currentRowIndex++;
                }
            }
        }

        /// <summary>
        /// Switch from current to next row
        /// this is not needed, you can switch lines to add spaces
        /// </summary>
        public void NextLine()
        {
            if (!_gridCreated)
            {
                return;
            }
            ImGui.TableNextRow();
            _currentRowIndex++;
            _currentColIndex = 0;
        }

        /// <summary>
        /// From this call, mininmum lines (rows) height will be as you wish
        /// </summary>
        /// <param name="minLineHeight">wished minimum lines (rows) height</param>
        public void SetMinimumLineHeight(float minLineHeight)
        {
            _minLineHeight = minLineHeight;
        }

        /// <summary>
        /// Apply a padding on top of the next element to draw
        /// </summary>
        /// <param name="padding">padding to apply on top of the next drawed element</param>
        public void NextElementYPadding(float padding)
        {
            _nextElementYPadding = padding;
        }

        /// <summary>
        /// Set the grid according to the current grid definition
        /// </summary>
        /// <param name="linesBg">Colorise evens rows</param>
        /// <param name="rowPadding">rows padding</param>
        private void setGrid(bool linesBg, float cellPadding, float rowPadding, float outterPadding)
        {
            if (IsInsidePopUp)
            {
                Debug.LogError("You are trying to create a grid inside a PopUp, wich is not a good idee. Please check your code and remove it.");
            }
            FuGui.Push(ImGuiStyleVar.CellPadding, new Vector2(cellPadding, rowPadding));
            _gridCreated = _currentGridDef.SetupTable(_gridName, outterPadding, linesBg, ref _isResponsivelyResized);
            if (!_gridCreated)
            {
                return;
            }
        }
        #endregion

        #region private Layout
        /// <summary>
        /// call this right before drawing an element. this will apply style and handle the grid layout for you
        /// </summary>
        /// <param name="style">Element Style to apply</param>
        protected override string beginElement(string elementID, IUIElementStyle style = null)
        {
            elementID = base.beginElement(elementID, style);
            if (!_gridCreated)
            {
                return elementID;
            }
            NextColumn();

            //// add padding to value if grid is responsively resized
            //if (_isResponsivelyResized && _currentRowIndex % 2 == 0)
            //{
            //    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 4f);
            //}

            // apply additionnal Y padding
            if (_nextElementYPadding > 0f)
            {
                Vector2 cursorPos = ImGui.GetCursorScreenPos();
                cursorPos.y += _nextElementYPadding;
                ImGui.SetCursorScreenPos(cursorPos);
                _nextElementYPadding = 0f;
            }

            // ready to draw next element
            _beginElementCursorY = ImGui.GetCursorPosY();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().x);
            return elementID;
        }

        /// <summary>
        /// call this after drawing an element. handle style and row height
        /// </summary>
        /// <param name="style"></param>
        protected override void endElement(IUIElementStyle style = null)
        {
            if (!_gridCreated)
            {
                return;
            }
            base.endElement(style);

            float lineHeight = ImGui.GetCursorPosY() - _beginElementCursorY;
            if (lineHeight < _minLineHeight)
            {
                ImGui.Dummy(new Vector2(0f, _minLineHeight - lineHeight));
            }
        }

        /// <summary>
        /// If you see this summary, you failed.
        /// This should never been call manualy. => it work, but we are trying to unify things here.
        /// Please use USING statement
        /// </summary>
        public override void Dispose()
        {
            if (_gridCreated)
            {
                ImGui.EndTable();
            }
            FuGui.PopStyle();
        }
        #endregion

        #region Object
        /// <summary>
        /// Draw the object instance
        /// </summary>
        /// <typeparam name="T">Type of the object to draw</typeparam>
        /// <param name="objectInstance">object instance to draw</param>
        /// <returns>true if some value has just been edited</returns>
        public bool DrawObject<T>(T objectInstance)
        {
            Type type = typeof(T);
            // type already registered
            if (!_objectsDescriptions.ContainsKey(type))
            {
                // register type
                _objectsDescriptions.Add(type, new UIObjectDescription());
            }
            // draw object into this grid
            return _objectsDescriptions[type].DrawObject<T>(this, objectInstance);
        }
        #endregion

        #region private Utils
        /// <summary>
        /// Draw the Label of an element (if _autoDrawLabel is set to true)
        /// </summary>
        /// <param name="text">the text of the label</param>
        /// <param name="style">the UITextStyle of the label (Text color whatever it's enabled or disabled)</param>
        private void drawElementLabel(string text, UITextStyle style)
        {
            if (!_gridCreated)
            {
                return;
            }
            if (!_autoDrawLabel)
            {
                return;
            }

            // we need to display tooltip on this label and there are no tooltips to display
            if (_alwaysAutoTooltipsOnLabels && (_currentToolTips == null || _currentToolTipsIndex >= _currentToolTips.Length))
            {
                // add layout natural toolTip on label
                SetNextElementToolTipWithLabel(text);
            }

            // keep padding if needed
            float wantedNextElementYpadding = _nextElementYPadding;
            _nextElementYPadding = 0f;

            bool disabled = _nextIsDisabled;
            _nextIsDisabled = _nextIsDisabled && !_dontDisableLabels;
            Text(FuGui.AddSpacesBeforeUppercase(text), style);
            _nextIsDisabled = disabled;
            _nextElementYPadding = wantedNextElementYpadding;
        }
        #endregion
    }
}