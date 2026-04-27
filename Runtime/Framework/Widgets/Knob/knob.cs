using ImGuiNET;
using UnityEngine;

namespace Fu.Framework
{
        /// <summary>
        /// Represents the knob type.
        /// </summary>
        internal class knob
        {
            #region State
            private float rad;

            public float radius => rad * Fugui.Scale;

            public bool value_changed;
            public Vector2 center;
            public bool is_active;
            public bool is_hovered;
            public float angle_min;
            public float angle_max;
            public float angle;
            public float angle_cos;
            public float angle_sin;
            public float t;
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the knob class.
            /// </summary>
            /// <param name="_radius">The radius value.</param>
            public knob(float _radius)
            {
                rad = _radius;
            }
            #endregion

            #region Methods
            /// <summary>
            /// Draws the value.
            /// </summary>
            /// <param name="_label">The label value.</param>
            /// <param name="p_value">The p value value.</param>
            /// <param name="v_min">The v min value.</param>
            /// <param name="v_max">The v max value.</param>
            /// <param name="speed">The speed value.</param>
            /// <param name="format">The format value.</param>
            /// <param name="flags">The flags value.</param>
            /// <param name="disabled">The disabled value.</param>
            public void Draw(string _label, ref float p_value, float v_min, float v_max, float speed, string format, FuKnobFlags flags, bool disabled)
            {
                t = (p_value - v_min) / (v_max - v_min);
                var screen_pos = ImGui.GetCursorScreenPos();
                float oldValue = p_value;

                // Handle dragging
                ImGui.InvisibleButton(_label, new Vector2(radius * 2.0f, radius * 2.0f));
                if (ImGui.IsItemActive() && ImGui.GetIO().MouseDelta.x != 0.0f && !disabled)
                {
                    float step = (v_max - v_min) / 200.0f;
                    p_value += ImGui.GetIO().MouseDelta.x * step;
                    if (p_value < v_min) p_value = v_min;
                    if (p_value > v_max) p_value = v_max;
                    value_changed = true;
                }

                if (!flags.HasFlag(FuKnobFlags.NoInput))
                {
                    value_changed = ImGui.DragFloat("##drag" + _label, ref p_value, speed, v_min, v_max, format, disabled ? ImGuiSliderFlags.NoInput : ImGuiSliderFlags.AlwaysClamp);
                }

                angle_min = Mathf.PI * 0.75f;
                angle_max = Mathf.PI * 2.25f;
                center = new Vector2(screen_pos.x + radius, screen_pos.y + radius);
                is_active = ImGui.IsItemActive();
                is_hovered = ImGui.IsItemHovered();
                angle = angle_min + (angle_max - angle_min) * t;
                angle_cos = Mathf.Cos(angle);
                angle_sin = Mathf.Sin(angle);

                if (disabled)
                {
                    value_changed = false;
                    p_value = oldValue;
                }
            }

            /// <summary>
            /// Runs the draw dot workflow.
            /// </summary>
            /// <param name="size">The size value.</param>
            /// <param name="radius">The radius value.</param>
            /// <param name="angle">The angle value.</param>
            /// <param name="color">The color value.</param>
            /// <param name="filled">The filled value.</param>
            /// <param name="segments">The segments value.</param>
            internal void draw_dot(float size, float radius, float angle, color_set color, bool filled, int segments)
            {
                var dot_size = size * this.radius;
                var dot_radius = radius * this.radius;

                ImGui.GetWindowDrawList().AddCircleFilled(new Vector2(center.x + Mathf.Cos(angle) * dot_radius, center.y + Mathf.Sin(angle) * dot_radius), dot_size, ImGui.GetColorU32(is_active ? color.active : (is_hovered ? color.hovered : color.color)), segments);
            }

            /// <summary>
            /// Runs the draw tick workflow.
            /// </summary>
            /// <param name="start">The start value.</param>
            /// <param name="end">The end value.</param>
            /// <param name="width">The width value.</param>
            /// <param name="angle">The angle value.</param>
            /// <param name="color">The color value.</param>
            internal void draw_tick(float start, float end, float width, float angle, color_set color)
            {
                var tick_start = start * radius;
                var tick_end = end * radius;
                var angle_cos = Mathf.Cos(angle);
                var angle_sin = Mathf.Sin(angle);

                ImGui.GetWindowDrawList().AddLine(
                        new Vector2(center.x + angle_cos * tick_end, center.y + angle_sin * tick_end),
                        new Vector2(center.x + angle_cos * tick_start, center.y + angle_sin * tick_start),
                        ImGui.GetColorU32(is_active ? color.active : (is_hovered ? color.hovered : color.color)),
                        width * radius);
            }

            /// <summary>
            /// Runs the draw circle workflow.
            /// </summary>
            /// <param name="size">The size value.</param>
            /// <param name="color">The color value.</param>
            /// <param name="segments">The segments value.</param>
            internal void draw_circle(float size, color_set color, int segments)
            {
                var circle_radius = size * radius;
                ImGui.GetWindowDrawList().AddCircleFilled(center, circle_radius, ImGui.GetColorU32(is_active ? color.active : (is_hovered ? color.hovered : color.color)), segments);
            }

            /// <summary>
            /// Runs the draw arc workflow.
            /// </summary>
            /// <param name="layout">The layout value.</param>
            /// <param name="radius">The radius value.</param>
            /// <param name="size">The size value.</param>
            /// <param name="start_angle">The start angle value.</param>
            /// <param name="end_angle">The end angle value.</param>
            /// <param name="color">The color value.</param>
            /// <param name="segments">The segments value.</param>
            /// <param name="bezier_count">The bezier count value.</param>
            internal void draw_arc(FuLayout layout, float radius, float size, float start_angle, float end_angle, color_set color, int segments, int bezier_count)
            {
                var track_radius = radius * this.radius;
                var track_size = size * this.radius * 0.5f + 0.0001f;
                layout.draw_arc(
                        center,
                        track_radius,
                        start_angle,
                        end_angle,
                        track_size,
                        is_active ? color.active : (is_hovered ? color.hovered : color.color),
                        segments,
                        bezier_count);
            }
            #endregion
        }
}