using ImGuiNET;
using UnityEngine;

namespace Fugui.Framework
{
    public partial class UILayout
    {
        #region Drag Float
        ///<summary>
        /// Creates a draggable float input field.
        ///</summary>
        ///<param name="id">The identifier for the input field.</param>
        ///<param name="value">The float value to be displayed in the input field.</param>
        ///<param name="vString">A string to be displayed before the input field. If empty, no string will be displayed.</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string id, ref float value, string vString = null)
        {
            return Drag(id, ref value, vString, 0, 100, UIFrameStyle.Default);
        }

        ///<summary>
        /// Creates a draggable float input field.
        ///</summary>
        ///<param name="id">The identifier for the input field.</param>
        ///<param name="value">The float value to be displayed in the input field.</param>
        ///<param name="vString">A string to be displayed before the input field. If empty, no string will be displayed.</param>
        ///<param name="min">The minimum allowed value for the input field.</param>
        ///<param name="max">The maximum allowed value for the input field.</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string id, ref float value, string vString, float min, float max)
        {
            return Drag(id, ref value, vString, min, max, UIFrameStyle.Default);
        }

        ///<summary>
        /// Creates a draggable float input field.
        ///</summary>
        ///<param name="id">The identifier for the input field.</param>
        ///<param name="value">The float value to be displayed in the input field.</param>
        ///<param name="vString">A string to be displayed before the input field. If empty, no string will be displayed.</param>
        ///<param name="min">The minimum allowed value for the input field.</param>
        ///<param name="max">The maximum allowed value for the input field.</param>
        /// <param name="style">The style of the Drag</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public virtual bool Drag(string id, ref float value, string vString, float min, float max, UIFrameStyle style)
        {
            id = beginElement(id, style);
            bool valueChanged = dragFloat(id, ref value, vString, min, max);
            endElement(style);
            return valueChanged;
        }

        ///<summary>
        /// Creates a draggable float input field.
        ///</summary>
        ///<param name="id">The identifier for the input field.</param>
        ///<param name="value">The float value to be displayed in the input field.</param>
        ///<param name="vString">A string to be displayed before the input field. If empty, no string will be displayed.</param>
        ///<param name="min">The minimum allowed value for the input field.</param>
        ///<param name="max">The maximum allowed value for the input field.</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        private bool dragFloat(string id, ref float value, string vString, float min, float max)
        {
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
            bool valueChanged = ImGui.DragFloat("##" + id, ref value, 0.01f, min, max, getFloatString("##" + id, value), _nextIsDisabled ? ImGuiSliderFlags.NoInput : ImGuiSliderFlags.AlwaysClamp);

            // Update the format string for the input field based on its current value
            updateFloatString("##" + id, value);

            // If the input field is disabled, reset its value and return false for the valueChanged flag
            if (_nextIsDisabled)
            {
                value = oldVal;
                valueChanged = false;
            }

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
        ///<param name="id">The identifier for the input field.</param>
        ///<param name="value">The Vector2 value to be displayed in the input field.</param>
        ///<param name="v1String">A string to be displayed before the input field X. If empty, no string will be displayed.</param>
        ///<param name="v2String">A string to be displayed before the input field Y. If empty, no string will be displayed.</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string id, ref Vector2 value, string v1String = null, string v2String = null)
        {
            return Drag(id, ref value, v1String, v2String, 0f, 100f, UIFrameStyle.Default);
        }

        ///<summary>
        /// Creates a draggable Vector2 input field.
        ///</summary>
        ///<param name="id">The identifier for the input field.</param>
        ///<param name="value">The Vector2 value to be displayed in the input field.</param>
        ///<param name="v1String">A string to be displayed before the input field X. If empty, no string will be displayed.</param>
        ///<param name="v2String">A string to be displayed before the input field Y. If empty, no string will be displayed.</param>
        ///<param name="min">The minimum allowed value for the input field.</param>
        ///<param name="max">The maximum allowed value for the input field.</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string id, ref Vector2 value, string v1String, string v2String, float min, float max)
        {
            return Drag(id, ref value, v1String, v2String, min, max, UIFrameStyle.Default);
        }

        ///<summary>
        /// Creates a draggable Vector2 input field.
        ///</summary>
        ///<param name="id">The identifier for the input field.</param>
        ///<param name="value">The Vector2 value to be displayed in the input field.</param>
        ///<param name="v1String">A string to be displayed before the input field X. If empty, no string will be displayed.</param>
        ///<param name="v2String">A string to be displayed before the input field Y. If empty, no string will be displayed.</param>
        ///<param name="min">The minimum allowed value for the input field.</param>
        ///<param name="max">The maximum allowed value for the input field.</param>
        /// <param name="style">The style of the Drag</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public virtual bool Drag(string id, ref Vector2 value, string v1String, string v2String, float min, float max, UIFrameStyle style)
        {
            // Begin the element and apply the specified style
            id = beginElement(id, style);
            FuGui.Push(ImGuiStyleVar.CellPadding, new Vector2(4f, 0f));
            bool valueChanged = false;
            // Calculate the column width for the table
            float colWidth = ImGui.GetContentRegionAvail().x * 0.5f;
            // Start the table with two columns
            if (ImGui.BeginTable(id + "dragTable", 2))
            {
                // Set up the first column with the given ID and width
                ImGui.TableSetupColumn(id + "col1", ImGuiTableColumnFlags.None, colWidth);
                // Set up the second column with the given ID and width
                ImGui.TableSetupColumn(id + "col2", ImGuiTableColumnFlags.None, colWidth);
                // Move to the first column
                ImGui.TableNextColumn();
                // Create a draggable float for the first value in the table, using the specified ID and value string
                valueChanged |= dragFloat(id + "val1", ref value.x, v1String, min, max);
                // Draw a hover frame around the element if it is hovered
                drawHoverFrame();
                // Move to the second column
                ImGui.TableNextColumn();
                // Create a draggable float for the second value in the table, using the specified ID and value string
                valueChanged |= dragFloat(id + "val2", ref value.y, v2String, min, max);
                // Draw a hover frame around the element if it is hovered
                drawHoverFrame();
                // End the table
                ImGui.EndTable();
            }
            FuGui.PopStyle();
            // Reset the flag for whether the element is hovered and framed
            _elementHoverFramed = false;
            // End the element
            endElement(style);
            return valueChanged;
        }

        #endregion

        #region Drag Vector3
        ///<summary>
        /// Creates a draggable Vector3 input field.
        ///</summary>
        ///<param name="id">The identifier for the input field.</param>
        ///<param name="value">The Vector3 value to be displayed in the input field.</param>
        ///<param name="v1String">A string to be displayed before the input field X. If empty, no string will be displayed.</param>
        ///<param name="v2String">A string to be displayed before the input field Y. If empty, no string will be displayed.</param>
        ///<param name="v3String">A string to be displayed before the input field Z. If empty, no string will be displayed.</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string id, ref Vector3 value, string v1String = null, string v2String = null, string v3String = null)
        {
            return Drag(id, ref value, v1String, v2String, v3String, 0f, 100f, UIFrameStyle.Default);
        }

        ///<summary>
        /// Creates a draggable Vector3 input field.
        ///</summary>
        ///<param name="id">The identifier for the input field.</param>
        ///<param name="value">The Vector3 value to be displayed in the input field.</param>
        ///<param name="v1String">A string to be displayed before the input field X. If empty, no string will be displayed.</param>
        ///<param name="v2String">A string to be displayed before the input field Y. If empty, no string will be displayed.</param>
        ///<param name="v3String">A string to be displayed before the input field Z. If empty, no string will be displayed.</param>
        ///<param name="min">The minimum allowed value for the input field.</param>
        ///<param name="max">The maximum allowed value for the input field.</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string id, ref Vector3 value, string v1String, string v2String, string v3String, float min, float max)
        {
            return Drag(id, ref value, v1String, v2String, v3String, min, max, UIFrameStyle.Default);
        }

        ///<summary>
        /// Creates a draggable Vector3 input field.
        ///</summary>
        ///<param name="id">The identifier for the input field.</param>
        ///<param name="value">The Vector3 value to be displayed in the input field.</param>
        ///<param name="v1String">A string to be displayed before the input field X. If empty, no string will be displayed.</param>
        ///<param name="v2String">A string to be displayed before the input field Y. If empty, no string will be displayed.</param>
        ///<param name="v3String">A string to be displayed before the input field Z. If empty, no string will be displayed.</param>
        ///<param name="min">The minimum allowed value for the input field.</param>
        ///<param name="max">The maximum allowed value for the input field.</param>
        /// <param name="style">The style of the Drag</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public virtual bool Drag(string id, ref Vector3 value, string v1String, string v2String, string v3String, float min, float max, UIFrameStyle style)
        {
            id = beginElement(id, style);
            bool valueChanged = false;
            float colWidth = ImGui.GetContentRegionAvail().x / 3f;
            FuGui.Push(ImGuiStyleVar.CellPadding, new Vector2(4f, 0f));
            if (ImGui.BeginTable(id + "dragTable", 3))
            {
                // Set up the three columns in the table
                ImGui.TableSetupColumn(id + "col1", ImGuiTableColumnFlags.None, colWidth);
                ImGui.TableSetupColumn(id + "col2", ImGuiTableColumnFlags.None, colWidth);
                ImGui.TableSetupColumn(id + "col3", ImGuiTableColumnFlags.None, colWidth);

                // Begin the first column
                ImGui.TableNextColumn();

                // Drag the first value
                valueChanged |= dragFloat(id + "val1", ref value.x, v1String, min, max);
                drawHoverFrame();

                // Begin the second column
                ImGui.TableNextColumn();

                // Drag the second value
                valueChanged |= dragFloat(id + "val2", ref value.y, v2String, min, max);
                drawHoverFrame();

                // Begin the third column
                ImGui.TableNextColumn();

                // Drag the third value
                valueChanged |= dragFloat(id + "val3", ref value.z, v3String, min, max);
                drawHoverFrame();

                // End the table
                ImGui.EndTable();
            }
            FuGui.PopStyle();

            // Reset the hover frame flag
            _elementHoverFramed = false;
            endElement(style);
            return valueChanged;
        }
        #endregion

        #region Drag Vector4
        ///<summary>
        /// Creates a draggable Vector4 input field.
        ///</summary>
        ///<param name="id">The identifier for the input field.</param>
        ///<param name="value">The Vector4 value to be displayed in the input field.</param>
        ///<param name="v1String">A string to be displayed before the input field X. If empty, no string will be displayed.</param>
        ///<param name="v2String">A string to be displayed before the input field Y. If empty, no string will be displayed.</param>
        ///<param name="v3String">A string to be displayed before the input field Z. If empty, no string will be displayed.</param>
        ///<param name="v4String">A string to be displayed before the input field W. If empty, no string will be displayed.</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string id, ref Vector4 value, string v1String = null, string v2String = null, string v3String = null, string v4String = null)
        {
            return Drag(id, ref value, v1String, v2String, v3String, v4String, 0f, 100f, UIFrameStyle.Default);
        }
        ///<summary>
        /// Creates a draggable Vector4 input field.
        ///</summary>
        ///<param name="id">The identifier for the input field.</param>
        ///<param name="value">The Vector4 value to be displayed in the input field.</param>
        ///<param name="v1String">A string to be displayed before the input field X. If empty, no string will be displayed.</param>
        ///<param name="v2String">A string to be displayed before the input field Y. If empty, no string will be displayed.</param>
        ///<param name="v3String">A string to be displayed before the input field Z. If empty, no string will be displayed.</param>
        ///<param name="v4String">A string to be displayed before the input field W. If empty, no string will be displayed.</param>
        ///<param name="min">The minimum allowed value for the input field.</param>
        ///<param name="max">The maximum allowed value for the input field.</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string id, ref Vector4 value, string v1String, string v2String, string v3String, string v4String, float min, float max)
        {
            return Drag(id, ref value, v1String, v2String, v3String, v4String, min, max, UIFrameStyle.Default);
        }
        ///<summary>
        /// Creates a draggable Vector4 input field.
        ///</summary>
        ///<param name="id">The identifier for the input field.</param>
        ///<param name="value">The Vector4 value to be displayed in the input field.</param>
        ///<param name="v1String">A string to be displayed before the input field X. If empty, no string will be displayed.</param>
        ///<param name="v2String">A string to be displayed before the input field Y. If empty, no string will be displayed.</param>
        ///<param name="v3String">A string to be displayed before the input field Z. If empty, no string will be displayed.</param>
        ///<param name="v4String">A string to be displayed before the input field W. If empty, no string will be displayed.</param>
        ///<param name="min">The minimum allowed value for the input field.</param>
        ///<param name="max">The maximum allowed value for the input field.</param>
        /// <param name="style">The style of the Drag</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public virtual bool Drag(string id, ref Vector4 value, string v1String, string v2String, string v3String, string v4String, float min, float max, UIFrameStyle style)
        {
            id = beginElement(id, style);
            bool valueChanged = false;
            FuGui.Push(ImGuiStyleVar.CellPadding, new Vector2(4f, 0f));
            float colWidth = ImGui.GetContentRegionAvail().x * 0.25f;
            if (ImGui.BeginTable(id + "dragTable", 4))
            {
                // Set up four columns with equal widths
                ImGui.TableSetupColumn(id + "col1", ImGuiTableColumnFlags.None, colWidth);
                ImGui.TableSetupColumn(id + "col2", ImGuiTableColumnFlags.None, colWidth);
                ImGui.TableSetupColumn(id + "col3", ImGuiTableColumnFlags.None, colWidth);
                ImGui.TableSetupColumn(id + "col4", ImGuiTableColumnFlags.None, colWidth);

                // Move to the first column
                ImGui.TableNextColumn();
                valueChanged |= dragFloat(id + "val1", ref value.x, v1String, min, max); // Drag float for the first value
                drawHoverFrame();
                ImGui.TableNextColumn();
                valueChanged |= dragFloat(id + "val2", ref value.y, v2String, min, max); // Drag float for the second value
                drawHoverFrame();
                ImGui.TableNextColumn();
                valueChanged |= dragFloat(id + "val3", ref value.z, v3String, min, max); // Drag float for the third value
                drawHoverFrame();
                ImGui.TableNextColumn();
                valueChanged |= dragFloat(id + "val4", ref value.w, v4String, min, max); // Drag float for the fourth value
                drawHoverFrame();
                ImGui.EndTable();
            }
            FuGui.PopStyle();
            _elementHoverFramed = false;
            endElement(style);
            return valueChanged;
        }

        #endregion

        #region Drag Int
        ///<summary>
        /// Creates a draggable int input field.
        ///</summary>
        ///<param name="id">The identifier for the input field.</param>
        ///<param name="value">The int value to be displayed in the input field.</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string id, ref int value)
        {
            return Drag(id, null, ref value, 0, 100, UIFrameStyle.Default);
        }

        ///<summary>
        /// Creates a draggable int input field.
        ///</summary>
        ///<param name="id">The identifier for the input field.</param>
        ///<param name="value">The int value to be displayed in the input field.</param>
        ///<param name="vString">A string to be displayed before the input field. If empty, no string will be displayed.</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string id, string vString, ref int value)
        {
            return Drag(id, vString, ref value, 0, 100, UIFrameStyle.Default);
        }

        ///<summary>
        /// Creates a draggable int input field.
        ///</summary>
        ///<param name="id">The identifier for the input field.</param>
        ///<param name="value">The int value to be displayed in the input field.</param>
        ///<param name="vString">A string to be displayed before the input field. If empty, no string will be displayed.</param>
        ///<param name="min">The minimum allowed value for the input field.</param>
        ///<param name="max">The maximum allowed value for the input field.</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string id, string vString, ref int value, int min, int max)
        {
            return Drag(id, vString, ref value, min, max, UIFrameStyle.Default);
        }

        ///<summary>
        /// Creates a draggable int input field.
        ///</summary>
        ///<param name="id">The identifier for the input field.</param>
        ///<param name="value">The int value to be displayed in the input field.</param>
        ///<param name="min">The minimum allowed value for the input field.</param>
        ///<param name="max">The maximum allowed value for the input field.</param>
        ///<returns>True if the value in the input field was changed, false otherwise.</returns>
        public bool Drag(string id, ref int value, int min, int max)
        {
            return Drag(id, null, ref value, min, max, UIFrameStyle.Default);
        }

        /// <summary>
        /// Creates a draggable integer input element with an optional label.
        /// The element has a range between the given minimum and maximum values.
        /// The element uses the given style for its appearance.
        /// </summary>
        /// <param name="id">A unique identifier for the element.</param>
        /// <param name="vString">The label for the element.</param>
        /// <param name="value">A reference to the integer value to be modified by the element.</param>
        /// <param name="min">The minimum value for the element.</param>
        /// <param name="max">The maximum value for the element.</param>
        /// <param name="style">The style to be used for the element's appearance.</param>
        /// <returns>True if the value was modified, false otherwise.</returns>
        public virtual bool Drag(string id, string vString, ref int value, int min, int max, UIFrameStyle style)
        {
            // start drawing the element
            id = beginElement(id, style);
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
            bool valueChanged = ImGui.DragInt("##" + id, ref value, 0.05f, min, max, "%.0f", _nextIsDisabled ? ImGuiSliderFlags.NoInput : ImGuiSliderFlags.AlwaysClamp);
            // if the element is disabled, restore the old value and return false for valueChanged
            if (_nextIsDisabled)
            {
                value = oldVal;
                valueChanged = false;
            }
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