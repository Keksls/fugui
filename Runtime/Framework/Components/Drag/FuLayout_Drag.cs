using ImGuiNET;
using UnityEngine;

namespace Fu.Framework
{
    public partial class FuLayout
    {
        #region Drag Float
        ///<summary>
        /// Creates a draggable float input field.
        ///</summary>
        ///<param name="text">The identifier for the input field.</param>
        ///<param name="value">The float value to be displayed in the input field.</param>
        ///<param name="vString">A string to be displayed before the input field. If empty, no string will be displayed.</param>
        ///<param name="speed">Speed of the drag step</param>
        ///<param name="format">string format of the displayed value (default is "%.2f")</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string text, ref float value, string vString = null, float speed = 0.1f, string format = null)
        {
            return Drag(text, ref value, vString, 0, 100, FuFrameStyle.Default, speed, format);
        }

        ///<summary>
        /// Creates a draggable float input field.
        ///</summary>
        ///<param name="text">The identifier for the input field.</param>
        ///<param name="value">The float value to be displayed in the input field.</param>
        ///<param name="vString">A string to be displayed before the input field. If empty, no string will be displayed.</param>
        ///<param name="min">The minimum allowed value for the input field.</param>
        ///<param name="max">The maximum allowed value for the input field.</param>
        ///<param name="speed">Speed of the drag step</param>
        ///<param name="format">string format of the displayed value (default is "%.2f")</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string text, ref float value, string vString, float min, float max, float speed = 0.1f, string format = null)
        {
            return Drag(text, ref value, vString, min, max, FuFrameStyle.Default, speed, format);
        }

        ///<summary>
        /// Creates a draggable float input field.
        ///</summary>
        ///<param name="text">The identifier for the input field.</param>
        ///<param name="value">The float value to be displayed in the input field.</param>
        ///<param name="vString">A string to be displayed before the input field. If empty, no string will be displayed.</param>
        ///<param name="min">The minimum allowed value for the input field.</param>
        ///<param name="max">The maximum allowed value for the input field.</param>
        /// <param name="style">The style of the Drag</param>
        ///<param name="speed">Speed of the drag step</param>
        ///<param name="format">string format of the displayed value (default is "%.2f")</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public virtual bool Drag(string text, ref float value, string vString, float min, float max, FuFrameStyle style, float speed = 0.1f, string format = null)
        {
            beginElement(ref text, style);
            // return if item must no be draw
            if (!_drawElement)
            {
                return false;
            }
            bool valueChanged = dragFloat(text, ref value, vString, min, max, speed, format);
            endElement(style);
            return valueChanged;
        }

        ///<summary>
        /// Creates a draggable float input field.
        ///</summary>
        ///<param name="text">The identifier for the input field.</param>
        ///<param name="value">The float value to be displayed in the input field.</param>
        ///<param name="vString">A string to be displayed before the input field. If empty, no string will be displayed.</param>
        ///<param name="min">The minimum allowed value for the input field.</param>
        ///<param name="max">The maximum allowed value for the input field.</param>
        ///<param name="speed">Speed of the drag step</param>
        ///<param name="format">string format of the displayed value (default is "%.2f")</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        private bool dragFloat(string text, ref float value, string vString, float min, float max, float speed, string format)
        {
            // set the current item ID, so internal calculation can be unique (overwise the V2/V3 and V4 draw will use same ID for each dragFloat)
            LastItemID = text;
            // Display the string before the input field if it was provided
            if (!string.IsNullOrEmpty(vString))
            {
                // verticaly align text to frame padding
                ImGui.AlignTextToFramePadding();
                ImGui.Text(vString);
                ImGui.SameLine();
            }
            // Set the width of the input field and create it
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().x);
            var oldVal = value; // Store the old value in case the input field is disabled
            bool valueChanged = ImGui.DragFloat("##" + text, ref value, speed, min, max, string.IsNullOrEmpty(format) ? getStringFormat(value) : format, _nextIsDisabled ? ImGuiSliderFlags.NoInput : ImGuiSliderFlags.AlwaysClamp);

            // If the input field is disabled, reset its value and return false for the valueChanged flag
            if (_nextIsDisabled)
            {
                value = oldVal;
                valueChanged = false;
            }

            // set states for this element
            setBaseElementState(text, ImGui.GetItemRectMin(), ImGui.GetItemRectSize(), true, valueChanged);
            // Display a tooltip and set the _elementHoverFramed flag
            displayToolTip();
            _elementHoverFramed = true;
            return valueChanged;
        }
        #endregion

        #region Drag Vector2
        ///<summary>
        /// Creates a draggable Vector2 input field.
        ///</summary>
        ///<param name="text">The identifier for the input field.</param>
        ///<param name="value">The Vector2 value to be displayed in the input field.</param>
        ///<param name="v1String">A string to be displayed before the input field X. If empty, no string will be displayed.</param>
        ///<param name="v2String">A string to be displayed before the input field Y. If empty, no string will be displayed.</param>
        ///<param name="speed">Speed of the drag step</param>
        ///<param name="format">string format of the displayed value (default is "%.2f")</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string text, ref Vector2 value, string v1String = null, string v2String = null, float speed = 0.1f, string format = null)
        {
            return Drag(text, ref value, v1String, v2String, 0f, 100f, FuFrameStyle.Default, speed, format);
        }

        ///<summary>
        /// Creates a draggable Vector2 input field.
        ///</summary>
        ///<param name="text">The identifier for the input field.</param>
        ///<param name="value">The Vector2 value to be displayed in the input field.</param>
        ///<param name="v1String">A string to be displayed before the input field X. If empty, no string will be displayed.</param>
        ///<param name="v2String">A string to be displayed before the input field Y. If empty, no string will be displayed.</param>
        ///<param name="min">The minimum allowed value for the input field.</param>
        ///<param name="max">The maximum allowed value for the input field.</param>
        ///<param name="speed">Speed of the drag step</param>
        ///<param name="format">string format of the displayed value (default is "%.2f")</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string text, ref Vector2 value, string v1String, string v2String, float min, float max, float speed = 0.1f, string format = null)
        {
            return Drag(text, ref value, v1String, v2String, min, max, FuFrameStyle.Default, speed, format);
        }

        ///<summary>
        /// Creates a draggable Vector2 input field.
        ///</summary>
        ///<param name="text">The identifier for the input field.</param>
        ///<param name="value">The Vector2 value to be displayed in the input field.</param>
        ///<param name="v1String">A string to be displayed before the input field X. If empty, no string will be displayed.</param>
        ///<param name="v2String">A string to be displayed before the input field Y. If empty, no string will be displayed.</param>
        ///<param name="min">The minimum allowed value for the input field.</param>
        ///<param name="max">The maximum allowed value for the input field.</param>
        /// <param name="style">The style of the Drag</param>
        ///<param name="speed">Speed of the drag step</param>
        ///<param name="format">string format of the displayed value (default is "%.2f")</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public virtual bool Drag(string text, ref Vector2 value, string v1String, string v2String, float min, float max, FuFrameStyle style, float speed = 0.1f, string format = null)
        {
            // Begin the element and apply the specified style
            beginElement(ref text, style);
            // return if item must no be draw
            if (!_drawElement)
            {
                return false;
            }

            Fugui.Push(ImGuiStyleVar.CellPadding, new Vector2(4f, 0f) * Fugui.CurrentContext.Scale);
            bool valueChanged = false;
            // Calculate the column width for the table
            float colWidth = ImGui.GetContentRegionAvail().x * 0.5f;
            // Start the table with two columns
            if (ImGui.BeginTable(text + "dragTable", 2))
            {
                // Set up the first column with the given ID and width
                ImGui.TableSetupColumn(text + "col1", ImGuiTableColumnFlags.None, colWidth);
                // Set up the second column with the given ID and width
                ImGui.TableSetupColumn(text + "col2", ImGuiTableColumnFlags.None, colWidth);
                // Move to the first column
                ImGui.TableNextColumn();
                // Create a draggable float for the first value in the table, using the specified ID and value string
                valueChanged |= dragFloat(text + "val1", ref value.x, v1String, min, max, speed, format);
                // Draw a hover frame around the element if it is hovered
                drawHoverFrame();
                // Move to the second column
                ImGui.TableNextColumn();
                // Create a draggable float for the second value in the table, using the specified ID and value string
                valueChanged |= dragFloat(text + "val2", ref value.y, v2String, min, max, speed, format);
                // Draw a hover frame around the element if it is hovered
                drawHoverFrame();
                // End the table
                ImGui.EndTable();
            }
            Fugui.PopStyle();
            // prevent to draw full element hover frame
            _elementHoverFramed = false;
            // reset last item ID (has been change before to use a unique ID per dragFloat)
            LastItemID = text;
            // set states for this element
            setBaseElementState(text, _currentItemStartPos, ImGui.GetItemRectMax() - _currentItemStartPos, true, valueChanged);
            // End the element
            endElement(style);
            return valueChanged;
        }
        #endregion

        #region Drag Vector3
        ///<summary>
        /// Creates a draggable Vector3 input field.
        ///</summary>
        ///<param name="text">The identifier for the input field.</param>
        ///<param name="value">The Vector3 value to be displayed in the input field.</param>
        ///<param name="v1String">A string to be displayed before the input field X. If empty, no string will be displayed.</param>
        ///<param name="v2String">A string to be displayed before the input field Y. If empty, no string will be displayed.</param>
        ///<param name="v3String">A string to be displayed before the input field Z. If empty, no string will be displayed.</param>
        ///<param name="speed">Speed of the drag step</param>
        ///<param name="format">string format of the displayed value (default is "%.2f")</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string text, ref Vector3 value, string v1String = null, string v2String = null, string v3String = null, float speed = 0.1f, string format = null)
        {
            return Drag(text, ref value, v1String, v2String, v3String, 0f, 100f, FuFrameStyle.Default, speed, format);
        }

        ///<summary>
        /// Creates a draggable Vector3 input field.
        ///</summary>
        ///<param name="text">The identifier for the input field.</param>
        ///<param name="value">The Vector3 value to be displayed in the input field.</param>
        ///<param name="v1String">A string to be displayed before the input field X. If empty, no string will be displayed.</param>
        ///<param name="v2String">A string to be displayed before the input field Y. If empty, no string will be displayed.</param>
        ///<param name="v3String">A string to be displayed before the input field Z. If empty, no string will be displayed.</param>
        ///<param name="min">The minimum allowed value for the input field.</param>
        ///<param name="max">The maximum allowed value for the input field.</param>
        ///<param name="speed">Speed of the drag step</param>
        ///<param name="format">string format of the displayed value (default is "%.2f")</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string text, ref Vector3 value, string v1String, string v2String, string v3String, float min, float max, float speed = 0.1f, string format = null)
        {
            return Drag(text, ref value, v1String, v2String, v3String, min, max, FuFrameStyle.Default, speed, format);
        }

        ///<summary>
        /// Creates a draggable Vector3 input field.
        ///</summary>
        ///<param name="text">The identifier for the input field.</param>
        ///<param name="value">The Vector3 value to be displayed in the input field.</param>
        ///<param name="v1String">A string to be displayed before the input field X. If empty, no string will be displayed.</param>
        ///<param name="v2String">A string to be displayed before the input field Y. If empty, no string will be displayed.</param>
        ///<param name="v3String">A string to be displayed before the input field Z. If empty, no string will be displayed.</param>
        ///<param name="min">The minimum allowed value for the input field.</param>
        ///<param name="max">The maximum allowed value for the input field.</param>
        /// <param name="style">The style of the Drag</param>
        ///<param name="speed">Speed of the drag step</param>
        ///<param name="format">string format of the displayed value (default is "%.2f")</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public virtual bool Drag(string text, ref Vector3 value, string v1String, string v2String, string v3String, float min, float max, FuFrameStyle style, float speed = 0.1f, string format = null)
        {
            beginElement(ref text, style);
            // return if item must no be draw
            if (!_drawElement)
            {
                return false;
            }

            bool valueChanged = false;
            float colWidth = ImGui.GetContentRegionAvail().x / 3f;
            Fugui.Push(ImGuiStyleVar.CellPadding, new Vector2(4f, 0f) * Fugui.CurrentContext.Scale);
            if (ImGui.BeginTable(text + "dragTable", 3))
            {
                // Set up the three columns in the table
                ImGui.TableSetupColumn(text + "col1", ImGuiTableColumnFlags.None, colWidth);
                ImGui.TableSetupColumn(text + "col2", ImGuiTableColumnFlags.None, colWidth);
                ImGui.TableSetupColumn(text + "col3", ImGuiTableColumnFlags.None, colWidth);

                // Begin the first column
                ImGui.TableNextColumn();

                // Drag the first value
                valueChanged |= dragFloat(text + "val1", ref value.x, v1String, min, max, speed, format);
                drawHoverFrame();

                // Begin the second column
                ImGui.TableNextColumn();

                // Drag the second value
                valueChanged |= dragFloat(text + "val2", ref value.y, v2String, min, max, speed, format);
                drawHoverFrame();

                // Begin the third column
                ImGui.TableNextColumn();

                // Drag the third value
                valueChanged |= dragFloat(text + "val3", ref value.z, v3String, min, max, speed, format);
                drawHoverFrame();

                // End the table
                ImGui.EndTable();
            }
            Fugui.PopStyle();

            // reset last item ID (has been change before to use a unique ID per dragFloat)
            LastItemID = text;
            // set states for this element
            setBaseElementState(text, _currentItemStartPos, ImGui.GetItemRectMax() - _currentItemStartPos, true, valueChanged);
            // prevent to draw full element hover frame
            _elementHoverFramed = false;
            endElement(style);
            return valueChanged;
        }
        #endregion

        #region Drag Vector4
        ///<summary>
        /// Creates a draggable Vector4 input field.
        ///</summary>
        ///<param name="text">The identifier for the input field.</param>
        ///<param name="value">The Vector4 value to be displayed in the input field.</param>
        ///<param name="v1String">A string to be displayed before the input field X. If empty, no string will be displayed.</param>
        ///<param name="v2String">A string to be displayed before the input field Y. If empty, no string will be displayed.</param>
        ///<param name="v3String">A string to be displayed before the input field Z. If empty, no string will be displayed.</param>
        ///<param name="v4String">A string to be displayed before the input field W. If empty, no string will be displayed.</param>
        ///<param name="speed">Speed of the drag step</param>
        ///<param name="format">string format of the displayed value (default is "%.2f")</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string text, ref Vector4 value, string v1String = null, string v2String = null, string v3String = null, string v4String = null, float speed = 0.1f, string format = null)
        {
            return Drag(text, ref value, v1String, v2String, v3String, v4String, 0f, 100f, FuFrameStyle.Default, speed, format);
        }
        ///<summary>
        /// Creates a draggable Vector4 input field.
        ///</summary>
        ///<param name="text">The identifier for the input field.</param>
        ///<param name="value">The Vector4 value to be displayed in the input field.</param>
        ///<param name="v1String">A string to be displayed before the input field X. If empty, no string will be displayed.</param>
        ///<param name="v2String">A string to be displayed before the input field Y. If empty, no string will be displayed.</param>
        ///<param name="v3String">A string to be displayed before the input field Z. If empty, no string will be displayed.</param>
        ///<param name="v4String">A string to be displayed before the input field W. If empty, no string will be displayed.</param>
        ///<param name="min">The minimum allowed value for the input field.</param>
        ///<param name="max">The maximum allowed value for the input field.</param>
        ///<param name="speed">Speed of the drag step</param>
        ///<param name="format">string format of the displayed value (default is "%.2f")</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string text, ref Vector4 value, string v1String, string v2String, string v3String, string v4String, float min, float max, float speed = 0.1f, string format = null)
        {
            return Drag(text, ref value, v1String, v2String, v3String, v4String, min, max, FuFrameStyle.Default, speed, format);
        }
        ///<summary>
        /// Creates a draggable Vector4 input field.
        ///</summary>
        ///<param name="text">The identifier for the input field.</param>
        ///<param name="value">The Vector4 value to be displayed in the input field.</param>
        ///<param name="v1String">A string to be displayed before the input field X. If empty, no string will be displayed.</param>
        ///<param name="v2String">A string to be displayed before the input field Y. If empty, no string will be displayed.</param>
        ///<param name="v3String">A string to be displayed before the input field Z. If empty, no string will be displayed.</param>
        ///<param name="v4String">A string to be displayed before the input field W. If empty, no string will be displayed.</param>
        ///<param name="min">The minimum allowed value for the input field.</param>
        ///<param name="max">The maximum allowed value for the input field.</param>
        /// <param name="style">The style of the Drag</param>
        ///<param name="speed">Speed of the drag step</param>
        ///<param name="format">string format of the displayed value (default is "%.2f")</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public virtual bool Drag(string text, ref Vector4 value, string v1String, string v2String, string v3String, string v4String, float min, float max, FuFrameStyle style, float speed = 0.1f, string format = null)
        {
            beginElement(ref text, style);
            // return if item must no be draw
            if (!_drawElement)
            {
                return false;
            }

            bool valueChanged = false;
            Fugui.Push(ImGuiStyleVar.CellPadding, new Vector2(4f, 0f) * Fugui.CurrentContext.Scale);
            float colWidth = ImGui.GetContentRegionAvail().x * 0.25f;
            if (ImGui.BeginTable(text + "dragTable", 4))
            {
                // Set up four columns with equal widths
                ImGui.TableSetupColumn(text + "col1", ImGuiTableColumnFlags.None, colWidth);
                ImGui.TableSetupColumn(text + "col2", ImGuiTableColumnFlags.None, colWidth);
                ImGui.TableSetupColumn(text + "col3", ImGuiTableColumnFlags.None, colWidth);
                ImGui.TableSetupColumn(text + "col4", ImGuiTableColumnFlags.None, colWidth);

                // Move to the first column
                ImGui.TableNextColumn();
                valueChanged |= dragFloat(text + "val1", ref value.x, v1String, min, max, speed, format); // Drag float for the first value
                drawHoverFrame();
                ImGui.TableNextColumn();
                valueChanged |= dragFloat(text + "val2", ref value.y, v2String, min, max, speed, format); // Drag float for the second value
                drawHoverFrame();
                ImGui.TableNextColumn();
                valueChanged |= dragFloat(text + "val3", ref value.z, v3String, min, max, speed, format); // Drag float for the third value
                drawHoverFrame();
                ImGui.TableNextColumn();
                valueChanged |= dragFloat(text + "val4", ref value.w, v4String, min, max, speed, format); // Drag float for the fourth value
                drawHoverFrame();
                ImGui.EndTable();
            }
            Fugui.PopStyle();
            // prevent to draw full element hover frame
            _elementHoverFramed = false;
            // reset last item ID (has been change before to use a unique ID per dragFloat)
            LastItemID = text;
            // set states for this element
            setBaseElementState(text, _currentItemStartPos, ImGui.GetItemRectMax() - _currentItemStartPos, true, valueChanged);
            endElement(style);
            return valueChanged;
        }
        #endregion

        #region Drag Int
        ///<summary>
        /// Creates a draggable int input field.
        ///</summary>
        ///<param name="text">The identifier for the input field.</param>
        ///<param name="value">The int value to be displayed in the input field.</param>
        ///<param name="format">string format of the displayed value (default is "%.0f")</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string text, ref int value, string format = "%.0f")
        {
            return Drag(text, null, ref value, 0, 100, FuFrameStyle.Default);
        }

        ///<summary>
        /// Creates a draggable int input field.
        ///</summary>
        ///<param name="text">The identifier for the input field.</param>
        ///<param name="value">The int value to be displayed in the input field.</param>
        ///<param name="vString">A string to be displayed before the input field. If empty, no string will be displayed.</param>
        ///<param name="format">string format of the displayed value (default is "%.0f")</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string text, string vString, ref int value, string format = "%.0f")
        {
            return Drag(text, vString, ref value, 0, 100, FuFrameStyle.Default);
        }

        ///<summary>
        /// Creates a draggable int input field.
        ///</summary>
        ///<param name="text">The identifier for the input field.</param>
        ///<param name="value">The int value to be displayed in the input field.</param>
        ///<param name="vString">A string to be displayed before the input field. If empty, no string will be displayed.</param>
        ///<param name="min">The minimum allowed value for the input field.</param>
        ///<param name="max">The maximum allowed value for the input field.</param>
        ///<param name="format">string format of the displayed value (default is "%.0f")</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string text, string vString, ref int value, int min, int max, string format = "%.0f")
        {
            return Drag(text, vString, ref value, min, max, FuFrameStyle.Default);
        }

        ///<summary>
        /// Creates a draggable int input field.
        ///</summary>
        ///<param name="text">The identifier for the input field.</param>
        ///<param name="value">The int value to be displayed in the input field.</param>
        ///<param name="min">The minimum allowed value for the input field.</param>
        ///<param name="max">The maximum allowed value for the input field.</param>
        ///<param name="format">string format of the displayed value (default is "%.0f")</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string text, ref int value, int min, int max, string format = "%.0f")
        {
            return Drag(text, null, ref value, min, max, FuFrameStyle.Default);
        }

        /// <summary>
        /// Creates a draggable integer input element with an optional label.
        /// The element has a range between the given minimum and maximum values.
        /// The element uses the given style for its appearance.
        /// </summary>
        /// <param name="text">A unique identifier for the element.</param>
        /// <param name="vString">The label for the element.</param>
        /// <param name="value">A reference to the integer value to be modified by the element.</param>
        /// <param name="min">The minimum value for the element.</param>
        /// <param name="max">The maximum value for the element.</param>
        /// <param name="style">The style to be used for the element's appearance.</param>
        ///<param name="format">string format of the displayed value (default is "%.0f")</param>
        /// <returns>True if the value was modified, false otherwise.</returns>
        public virtual bool Drag(string text, string vString, ref int value, int min, int max, FuFrameStyle style, string format = "%.0f")
        {
            // start drawing the element
            beginElement(ref text, style);
            // return if item must no be draw
            if (!_drawElement)
            {
                return false;
            }

            // display the label, if there is one
            if (!string.IsNullOrEmpty(vString))
            {
                // verticaly align text to frame padding
                ImGui.AlignTextToFramePadding();
                ImGui.Text(vString);
                ImGui.SameLine();
            }
            // set the width of the element to the available width in the current content region
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().x);
            // store the current value in case the element is disabled
            var oldVal = value;
            // draw the draggable integer input element
            bool valueChanged = ImGui.DragInt("##" + text, ref value, 0.05f, min, max, format, _nextIsDisabled ? ImGuiSliderFlags.NoInput : ImGuiSliderFlags.AlwaysClamp);
            // if the element is disabled, restore the old value and return false for valueChanged
            if (_nextIsDisabled)
            {
                value = oldVal;
                valueChanged = false;
            }
            // set states for this element
            setBaseElementState(text, _currentItemStartPos, ImGui.GetItemRectMax() - _currentItemStartPos, true, valueChanged);
            // display the tool tip, if there is one
            displayToolTip();
            // this element can draw a fram if it is hovered
            _elementHoverFramed = true;
            // endup the element
            endElement(style);
            // return whatever the value has changed
            return valueChanged;
        }
        #endregion
    }
}