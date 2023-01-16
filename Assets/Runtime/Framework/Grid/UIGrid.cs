using ImGuiNET;
using SFB;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fugui.Framework
{
    public class UIGrid : UILayout
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
        /// <param name="outterPadding">grid outter padding. Represent the space at the Left and Right of the Grid</param>
        public UIGrid(string gridName = null, UIGridFlag flags = UIGridFlag.Default, float rowsPadding = 2f, float outterPadding = 4f)
        {
            if (gridName == null)
            {
                gridName = Guid.NewGuid().ToString();
            }
            _autoDrawLabel = !flags.HasFlag(UIGridFlag.NoAutoLabels);
            _dontDisableLabels = flags.HasFlag(UIGridFlag.DoNotDisableLabels);
            _alwaysAutoTooltipsOnLabels = flags.HasFlag(UIGridFlag.AutoToolTipsOnLabels);
            _currentGridDef = UIGridDefinition.DefaultFixed;
            _gridName = gridName;
            setGrid(flags.HasFlag(UIGridFlag.LinesBackground), rowsPadding, outterPadding);
        }

        /// <summary>
        /// Create a new UI Grid
        /// </summary>
        /// <param name="gridName">Unique Name of the Grid</param>
        /// <param name="gridDef">Definition of the Grid. Assume grid behaviour and style. Can be fully custom or use a presset (UIGridDefinition.XXX)</param>
        /// <param name="flags">Bitmask that constraint specific grid behaviour</param>
        /// <param name="rowsPadding">spaces in pixel between rows</param>
        /// <param name="outterPadding">grid outter padding. Represent the space at the Left and Right of the Grid</param>
        public UIGrid(UIGridDefinition gridDef, string gridName = null, UIGridFlag flags = UIGridFlag.Default, float rowsPadding = 2f, float outterPadding = 4f)
        {
            if (gridName == null)
            {
                gridName = Guid.NewGuid().ToString();
            }
            _autoDrawLabel = !flags.HasFlag(UIGridFlag.NoAutoLabels);
            _dontDisableLabels = flags.HasFlag(UIGridFlag.DoNotDisableLabels);
            _alwaysAutoTooltipsOnLabels = flags.HasFlag(UIGridFlag.AutoToolTipsOnLabels);
            _currentGridDef = gridDef;
            _gridName = gridName;
            setGrid(flags.HasFlag(UIGridFlag.LinesBackground), rowsPadding, outterPadding);
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
        /// Set the grid according to the current grid definition
        /// </summary>
        /// <param name="linesBg">Colorise evens rows</param>
        /// <param name="rowPadding">rows padding</param>
        private void setGrid(bool linesBg, float rowPadding, float outterPadding)
        {
            if (IsInsidePopUp)
            {
                Debug.LogError("You are trying to create a grid inside a PopUp, wich is not a good idee. Please check your code and remove it.");
            }
            FuGui.Push(ImGuiStyleVar.CellPadding, new Vector2(8f, rowPadding));
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

            // add padding to value if grid is responsively resized
            if (_isResponsivelyResized && _currentRowIndex % 2 == 0)
            {
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 4f);
            }

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

        #region Generic UI Elements
        #region Label
        /// <summary>
        /// Display a Horizontaly centered text (centered according to minimum line height)
        /// </summary>
        /// <param name="text">text to draw</param>
        /// <param name="style">Text Style</param>
        public override void Text(string text, UITextStyle style)
        {
            if (!_gridCreated)
            {
                return;
            }
            beginElement("", style);
            // horizontaly center Label
            float textHeight = ImGui.CalcTextSize(text).y;
            if (textHeight < _minLineHeight)
            {
                float padding = (_minLineHeight - textHeight) / 2f;
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + padding - 1f);
            }
            // draw text
            ImGui.Text(text);
            // handle tooltip
            if (_currentToolTipsOnLabels)
            {
                displayToolTip();
            }
            endElement(style);
        }

        /// <summary>
        /// Display a Horizontaly centered Smart Text (tagged richtext)
        /// </summary>
        /// <param name="text">text to draw</param>
        /// <param name="style">Text Style</param>
        public override void SmartText(string text, UITextStyle style)
        {
            if (!_gridCreated)
            {
                return;
            }
            beginElement("", style);
            // horizontaly center Label
            float textHeight = ImGui.CalcTextSize(text).y;
            if (textHeight < _minLineHeight)
            {
                float padding = (_minLineHeight - textHeight) / 2f;
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + padding - 1f);
            }
            // draw text
            _customText(text);
            // handle tooltip
            if (_currentToolTipsOnLabels)
            {
                displayToolTip();
            }
            endElement(style);
        }
        #endregion

        #region CheckBox
        /// <summary>
        /// Draw a CheckBox
        /// </summary>
        /// <param name="text">Element ID and Label</param>
        /// <param name="isChecked">whatever the checkbox is checked</param>
        /// <param name="style">Checkbox style to apply</param>
        /// <returns>true if value change</returns>
        public override bool CheckBox(string text, ref bool isChecked, UICheckboxStyle style)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(text, style.Unchecked.TextStyle);
            return base.CheckBox("##" + text, ref isChecked, style);
        }
        #endregion

        #region Radio Button
        /// <summary>
        /// Draw a Radio Button
        /// </summary>
        /// <param name="text">Element ID and Label</param>
        /// <param name="isChecked">whatever the checkbox is checked</param>
        /// <param name="style">Checkbox style to apply</param>
        /// <returns>true if value change</returns>
        public override bool RadioButton(string text, bool isChecked, UIFrameStyle style)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(text, style.TextStyle);
            return base.RadioButton("##" + text, isChecked, style);
        }
        #endregion

        #region Slider
        /// <summary>
        /// Draw a custom unity-style slider (Label + slider + input)
        /// </summary>
        /// <param name="text">Label and ID of the slider</param>
        /// <param name="value">refered value of the slider</param>
        /// <param name="min">minimum value of the slider</param>
        /// <param name="max">maximum value of the slider</param>
        /// <param name="isInt">whatever the slider is an Int slider (default is float). If true, the value will be rounded</param>
        /// <param name="style">slider style</param>
        /// <returns>true if value changed</returns>
        protected override bool _customSlider(string text, ref float value, float min, float max, bool isInt, UISliderStyle style)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(text, style.Frame.TextStyle);
            return base._customSlider(text, ref value, min, max, isInt, style);
        }
        #endregion

        #region Drag
        /// <summary>
        /// Draw a drag element.
        /// This is a Label/ID + value name (optional) + Input
        /// </summary>
        /// <param name="text">Label/ID of the drag</param>
        /// <param name="value">refered value of the drag</param>
        /// <param name="vString">(optional nullable) name of the drag value</param>
        /// <param name="min">minimum value of the drag</param>
        /// <param name="max">minimum value of the drag</param>
        /// <param name="style">style of the drag (FrameStyle)</param>
        /// <returns>true if value changes</returns>
        public override bool Drag(string text, ref float value, string vString, float min, float max, UIFrameStyle style)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(text, style.TextStyle);
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().x);
            return base.Drag(text, ref value, vString, min, max, style);
        }

        /// <summary>
        /// Draw a drag element.
        /// This is a Label/ID + value name (optional) + Input
        /// </summary>
        /// <param name="text">Label/ID of the drag</param>
        /// <param name="value">refered value of the drag</param>
        /// <param name="v1String">(optional nullable) name of the drag value 1</param>
        /// <param name="v2String">(optional nullable) name of the drag value 2</param>
        /// <param name="min">minimum value of the drag</param>
        /// <param name="max">minimum value of the drag</param>
        /// <param name="style">style of the drag (FrameStyle)</param>
        /// <returns>true if value changes</returns>
        public override bool Drag(string text, ref Vector2 value, string v1String, string v2String, float min, float max, UIFrameStyle style)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(text, style.TextStyle);
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().x);
            return base.Drag(text, ref value, v1String, v2String, min, max, style);
        }

        /// <summary>
        /// Draw a drag element.
        /// This is a Label/ID + value name (optional) + Input
        /// </summary>
        /// <param name="text">Label/ID of the drag</param>
        /// <param name="value">refered value of the drag</param>
        /// <param name="v1String">(optional nullable) name of the drag value 1</param>
        /// <param name="v2String">(optional nullable) name of the drag value 2</param>
        /// <param name="v3String">(optional nullable) name of the drag value 3</param>
        /// <param name="min">minimum value of the drag</param>
        /// <param name="max">minimum value of the drag</param>
        /// <param name="style">style of the drag (FrameStyle)</param>
        /// <returns>true if value changes</returns>
        public override bool Drag(string text, ref Vector3 value, string v1String, string v2String, string v3String, float min, float max, UIFrameStyle style)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(text, style.TextStyle);
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().x);
            return base.Drag(text, ref value, v1String, v2String, v3String, min, max, style);
        }

        /// <summary>
        /// Draw a drag element.
        /// This is a Label/ID + value name (optional) + Input
        /// </summary>
        /// <param name="text">Label/ID of the drag</param>
        /// <param name="value">refered value of the drag</param>
        /// <param name="v1String">(optional nullable) name of the drag value 1</param>
        /// <param name="v2String">(optional nullable) name of the drag value 2</param>
        /// <param name="v3String">(optional nullable) name of the drag value 3</param>
        /// <param name="v4String">(optional nullable) name of the drag value 4</param>
        /// <param name="min">minimum value of the drag</param>
        /// <param name="max">minimum value of the drag</param>
        /// <param name="style">style of the drag (FrameStyle)</param>
        /// <returns>true if value changes</returns>
        public override bool Drag(string text, ref Vector4 value, string v1String, string v2String, string v3String, string v4String, float min, float max, UIFrameStyle style)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(text, style.TextStyle);
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().x);
            return base.Drag(text, ref value, v1String, v2String, v3String, v4String, min, max, style);
        }

        /// <summary>
        /// Draw a drag element.
        /// This is a Label/ID + value name (optional) + Input
        /// </summary>
        /// <param name="text">Label/ID of the drag</param>
        /// <param name="value">refered value of the drag</param>
        /// <param name="vString">(optional nullable) name of the drag value</param>
        /// <param name="min">minimum value of the drag</param>
        /// <param name="max">minimum value of the drag</param>
        /// <param name="style">style of the drag (FrameStyle)</param>
        /// <returns>true if value changes</returns>
        public override bool Drag(string text, string vString, ref int value, int min, int max, UIFrameStyle style)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(text, style.TextStyle);
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().x);
            return base.Drag(text, vString, ref value, min, max, style);
        }
        #endregion

        #region Combobox
        /// <summary>
        /// Draw a combobox using a list of IComboboxItem
        /// </summary>
        /// <param name="text">Label/ID of the combobox</param>
        /// <param name="items">List of items of the combobox</param>
        /// <param name="itemChange">event raised on item change. When raised, param (int) is ID of new selected item in items list</param>
        /// <param name="itemGetter">A func that return a way to get current stored value for the combobox. can be null if combobox il not lined to an object's field</param>
        /// <param name="style">Combobox style to apply</param>
        protected override void _customCombobox(string text, List<IComboboxItem> items, Action<int> itemChange, Func<string> itemGetter, UIComboboxStyle style)
        {
            if (!_gridCreated)
            {
                return;
            }
            drawElementLabel(text, style.ButtonStyle.TextStyle);
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().x);
            base._customCombobox("##" + text, items, itemChange, itemGetter, style);
        }

        /// <summary>
        /// Draw a combobox using fully custom UI
        /// </summary>
        /// <param name="text">Label/ID of the combobox</param>
        /// <param name="selectedItemText">text displayed on combobox</param>
        /// <param name="callback">custom UI to draw when Combobox is open</param>
        /// <param name="style">Combobox style to apply</param>
        /// <param name="height">Height of the open UI</param>
        public override void Combobox(string text, string selectedItemText, Action callback, UIComboboxStyle style, int height)
        {
            if (!_gridCreated)
            {
                return;
            }
            drawElementLabel(text, style.ButtonStyle.TextStyle);
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().x);
            base.Combobox("##" + text, selectedItemText, callback, style, height);
        }
        #endregion

        #region textInput
        /// <summary>
        /// Input Text Element
        /// </summary>
        /// <param name="label">Label/ID of the Element</param>
        /// <param name="hint">hover text of the input text (only if height = 0, don't wor on multiline textbox)</param>
        /// <param name="text">text value of the TextInput</param>
        /// <param name="size">buffer size of the text value</param>
        /// <param name="height">height of the input.</param>
        /// <param name="style">UIFrameStyle of the UI element</param>
        /// <returns>true if value change</returns>
        public override bool TextInput(string label, string hint, ref string text, uint size, float height, UIFrameStyle style)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(label, style.TextStyle);
            return base.TextInput(label, hint, ref text, size, height, style);
        }
        #endregion

        #region Image
        /// <summary>
        /// Display and immage
        /// </summary>
        /// <param name="id">ID/Label of the image</param>
        /// <param name="texture">Texture to draw</param>
        /// <param name="size">Size of the image</param>
        public override void Image(string id, Texture2D texture, Vector2 size)
        {
            if (!_gridCreated)
            {
                return;
            }
            drawElementLabel(id, UITextStyle.Default);
            base.Image(id, texture, size);
        }

        /// <summary>
        /// Display and immage
        /// </summary>
        /// <param name="id">ID/Label of the image</param>
        /// <param name="texture">Texture to draw</param>
        /// <param name="size">Size of the image</param>
        public override void Image(string id, RenderTexture texture, Vector2 size)
        {
            if (!_gridCreated)
            {
                return;
            }
            drawElementLabel(id, UITextStyle.Default);
            base.Image(id, texture, size);
        }

        /// <summary>
        /// Draw a clickable image (button)
        /// </summary>
        /// <param name="id">ID/Label of the image</param>
        /// <param name="texture">Texture to draw</param>
        /// <param name="size">Size of the image</param>
        /// <returns>true if clicked</returns>
        public override bool ImageButton(string id, Texture2D texture, Vector2 size)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(id, UITextStyle.Default);
            return base.ImageButton(id, texture, size);
        }

        /// <summary>
        /// Draw a clickable image (button)
        /// </summary>
        /// <param name="id">ID/Label of the image</param>
        /// <param name="texture">Texture to draw</param>
        /// <param name="size">Size of the image</param>
        /// <returns>true if clicked</returns>
        public override bool ImageButton(string id, Texture2D texture, Vector2 size, Vector4 color)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(id, UITextStyle.Default);
            return base.ImageButton(id, texture, size, color);
        }
        #endregion

        #region ColorPicker
        /// <summary>
        /// Display a custom color picker
        /// </summary>
        /// <param name="id">ID/Label of the colorpicked</param>
        /// <param name="color">reference od the color value</param>
        /// <param name="style">UIFrameStyle of the colorpicker</param>
        /// <returns>true if value change</returns>
        public override bool ColorPicker(string id, ref Vector4 color, UIFrameStyle style)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(id, UITextStyle.Default);
            return base.ColorPicker(id, ref color, style);
        }

        /// <summary>
        /// Display a alphaless custom color picker (without alpha)
        /// </summary>
        /// <param name="id">ID/Label of the colorpicked</param>
        /// <param name="color">reference od the color value</param>
        /// <param name="style">UIFrameStyle of the colorpicker</param>
        /// <returns>true if value change</returns>
        public override bool ColorPicker(string id, ref Vector3 color, UIFrameStyle style)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(id, UITextStyle.Default);
            return base.ColorPicker(id, ref color, style);
        }
        #endregion

        #region Toggle
        protected override bool _customToggle(string text, ref bool value, string textLeft, string textRight, ToggleFlags flags, UIToggleStyle style)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(text, UITextStyle.Default);
            return base._customToggle(text, ref value, textLeft, textRight, flags, style);
        }
        #endregion

        #region Buttons Group
        protected override void _buttonsGroup<T>(string text, List<T> items, Action<int> callback, int defaultSelected, ButtonsGroupFlags flags, UIButtonsGroupStyle style)
        {
            if (!_gridCreated)
            {
                return;
            }
            drawElementLabel(text, UITextStyle.Default);
            base._buttonsGroup<T>(text, items, callback, defaultSelected, flags, style);
        }
        #endregion

        #region Path Field
        protected override void _pathField(string text, bool onlyFolder, Action<string> callback, UIFrameStyle style, string defaultPath = null, params ExtensionFilter[] extentions)
        {
            if (!_gridCreated)
            {
                return;
            }
            drawElementLabel(text, UITextStyle.Default);
            base._pathField(text, onlyFolder, callback, style, defaultPath, extentions);
        }
        #endregion
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

            bool disabled = _nextIsDisabled;
            _nextIsDisabled = _nextIsDisabled && !_dontDisableLabels;
            Text(FuGui.AddSpacesBeforeUppercase(text), style);
            _nextIsDisabled = disabled;
        }
        #endregion
    }
}