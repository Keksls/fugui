﻿using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework
{
    public unsafe partial class FuGrid : FuLayout
    {
        #region Variables
        // comment : The current column index being used in the grid
        private int _currentColIndex = 0;
        // The current row index being used in the grid
        private int _currentRowIndex = 0;
        // The current grid definition
        private FuGridDefinition _currentGridDef;
        // Flag to indicate if labels should be automatically drawn
        private bool _autoDrawLabel = true;
        // Flag to indicate if labels should not be disabled
        private bool _dontDisableLabels = true;
        // Flag to indicate if labels should always have tooltip
        private bool _alwaysAutoTooltipsOnLabels = false;
        // Flag to indicate if the grid has been created
        internal bool _gridCreated = false;
        // The minimum line height for elements in the grid
        private float _minLineHeight = 20f;
        // The Y padding to draw on top of the next element of the list
        private float _nextElementYPadding = 0f;
        // The y-coordinate of the cursor position when the current group of elements began
        private float _beginElementCursorY = 0;
        // Flag to indicate if the grid is responsively resized
        private bool _isResponsivelyResized = false;
        // A dictionary to store grid descriptions for different types
        private static Dictionary<Type, FuObjectDescription> _objectsDescriptions = new Dictionary<Type, FuObjectDescription>();
        protected string _ID;
        private bool disposed = false;
        #endregion

        /// <summary>
        /// Create a new UI Grid
        /// </summary>
        /// <param name="ID">Unique Name of the Grid</param>
        /// <param name="flags">Bitmask that constraint specific grid behaviour</param>
        /// <param name="rowsPadding">spaces in pixel between rows</param>
        /// <param name="cellPadding">spaces in pixel between cells</param>
        /// <param name="outterPadding">grid outter padding. Represent the space at the Left and Right of the Grid</param>
        public FuGrid(string ID, FuGridFlag flags = FuGridFlag.Default, float cellPadding = -1f, float rowsPadding = -1f, float outterPadding = 4f, float width = -1f) : base()
        {
            _ID = ID;
            _autoDrawLabel = !flags.HasFlag(FuGridFlag.NoAutoLabels);
            _dontDisableLabels = flags.HasFlag(FuGridFlag.DoNotDisableLabels);
            _alwaysAutoTooltipsOnLabels = flags.HasFlag(FuGridFlag.AutoToolTipsOnLabels);
            _currentGridDef = FuGridDefinition.DefaultFixed;
            setGrid(flags.HasFlag(FuGridFlag.LinesBackground), cellPadding, rowsPadding, outterPadding, width);
        }

        /// <summary>
        /// Create a new UI Grid
        /// </summary>
        /// <param name="ID">Unique Name of the Grid</param>
        /// <param name="gridDef">Definition of the Grid. Assume grid behaviour and style. Can be fully custom or use a presset (UIGridDefinition.XXX)</param>
        /// <param name="flags">Bitmask that constraint specific grid behaviour</param>
        /// <param name="rowsPadding">spaces in pixel between rows</param>
        /// <param name="cellPadding">spaces in pixel between cells</param>
        /// <param name="outterPadding">grid outter padding. Represent the space at the Left and Right of the Grid</param>
        public FuGrid(string ID, FuGridDefinition gridDef, FuGridFlag flags = FuGridFlag.Default, float cellPadding = -1f, float rowsPadding = -1f, float outterPadding = 4f, float width = -1f) : base()
        {
            _ID = ID;
            _autoDrawLabel = !flags.HasFlag(FuGridFlag.NoAutoLabels);
            _dontDisableLabels = flags.HasFlag(FuGridFlag.DoNotDisableLabels);
            _alwaysAutoTooltipsOnLabels = flags.HasFlag(FuGridFlag.AutoToolTipsOnLabels);
            _currentGridDef = gridDef;
            setGrid(flags.HasFlag(FuGridFlag.LinesBackground), cellPadding, rowsPadding, outterPadding, width);
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
            ImGuiNative.igTableNextColumn();
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
            ImGuiNative.igTableNextRow(ImGuiTableRowFlags.None, 0f);
            _currentRowIndex++;
            _currentColIndex = 0;
        }

        /// <summary>
        /// From this call, mininmum lines (rows) height will be as you wish
        /// </summary>
        /// <param name="minLineHeight">wished minimum lines (rows) height</param>
        public void SetMinimumLineHeight(float minLineHeight)
        {
            _minLineHeight = minLineHeight * Fugui.CurrentContext.Scale;
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
        private void setGrid(bool linesBg, float cellPadding, float rowPadding, float outterPadding, float width)
        {
            if (cellPadding < 0f)
            {
                cellPadding = FuThemeManager.CellPadding.x;
            }
            if (rowPadding < 0f)
            {
                rowPadding = FuThemeManager.CellPadding.y;
            }
            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(cellPadding, rowPadding));
            _gridCreated = _currentGridDef.SetupTable(_ID, cellPadding, outterPadding, linesBg, ref _isResponsivelyResized, width);
        }
        #endregion

        #region private Layout
        /// <summary>
        /// call this right before drawing an element. this will apply style and handle the grid layout for you
        /// </summary>
        /// <param name="style">Element Style to apply</param>
        protected override void beginElement(ref string elementID, IFuElementStyle style = null, bool noReturn = false, bool canBeHidden = true)
        {
            if (!_gridCreated)
            {
                return;
            }
            NextColumn();
            base.beginElement(ref elementID, style, noReturn, canBeHidden);

            if (!_drawElement)
            {
                return;
            }
            //// add padding to value if grid is responsively resized
            //if (_isResponsivelyResized && _currentRowIndex % 2 == 0)
            //{
            //    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 4f);
            //}

            // apply additionnal Y padding
            if (_nextElementYPadding > 0f)
            {
                Vector2 cursorPos = default;
                ImGuiNative.igGetCursorScreenPos(&cursorPos);
                cursorPos.y += _nextElementYPadding;
                ImGuiNative.igSetCursorScreenPos(cursorPos);
                _nextElementYPadding = 0f;
            }

            // ready to draw next element
            _beginElementCursorY = ImGuiNative.igGetCursorPosY();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().x);
        }

        /// <summary>
        /// call this after drawing an element. handle style and row height
        /// </summary>
        /// <param name="style"></param>
        protected override void endElement(IFuElementStyle style = null)
        {
            if (!_gridCreated)
            {
                return;
            }
            base.endElement(style);

            if (!_drawElement)
            {

            }
            float lineHeight = ImGuiNative.igGetCursorPosY() - _beginElementCursorY;
            if (lineHeight < _minLineHeight)
            {
                ImGuiNative.igDummy(new Vector2(0f, _minLineHeight - lineHeight));
            }
        }

        /// <summary>
        /// If you see this summary, you failed.
        /// This should never been call manualy. => it work, but we are trying to unify things here.
        /// Please use USING statement
        /// </summary>
        public override void Dispose()
        {
            if (disposed)
            {
                return;
            }
            base.Dispose();
            if (_gridCreated)
            {
                ImGuiNative.igEndTable();
            }
            ImGui.PopStyleVar();
            disposed = true;
        }
        #endregion

        #region Object
        /// <summary>
        /// Draw the object instance
        /// </summary>
        /// <typeparam name="T">Type of the object to draw</typeparam>
        /// <param name="objectID">ID of the object to draw</param>
        /// <param name="objectInstance">object instance to draw</param>
        /// <returns>true if some value has just been edited</returns>
        public bool DrawObject<T>(string objectID, T objectInstance)
        {
            Type type = typeof(T);
            // type already registered
            if (!_objectsDescriptions.ContainsKey(type))
            {
                // register type
                _objectsDescriptions.Add(type, new FuObjectDescription());
            }
            // draw object into this grid
            return _objectsDescriptions[type].DrawObject<T>(objectID, this, objectInstance);
        }
        #endregion

        #region private Utils
        /// <summary>
        /// Draw the Label of an element (if _autoDrawLabel is set to true)
        /// </summary>
        /// <param name="text">the text of the label</param>
        /// <param name="style">the UITextStyle of the label (Text color whatever it's enabled or disabled)</param>
        private void drawElementLabel(string text, FuTextStyle style)
        {
            if (!_gridCreated || !_autoDrawLabel)
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

            bool disabled = LastItemDisabled;
            LastItemDisabled = LastItemDisabled && !_dontDisableLabels;
            Text(Fugui.AddSpacesBeforeUppercase(Fugui.GetUntagedText(text)), style);
            LastItemDisabled = disabled;
            _nextElementYPadding = wantedNextElementYpadding;
        }
        #endregion
    }
}