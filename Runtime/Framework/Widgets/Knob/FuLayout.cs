using ImGuiNET;
using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework
{
        /// <summary>
        /// Represents the Fu Layout type.
        /// </summary>
        public partial class FuLayout
        {
            #region State
            internal Dictionary<string, knob> _knobs = new Dictionary<string, knob>();
            #endregion

            #region Methods
            /// <summary>
            /// Runs the draw arc1 workflow.
            /// </summary>
            /// <param name="center">The center value.</param>
            /// <param name="radius">The radius value.</param>
            /// <param name="start_angle">The start angle value.</param>
            /// <param name="end_angle">The end angle value.</param>
            /// <param name="thickness">The thickness value.</param>
            /// <param name="color">The color value.</param>
            /// <param name="num_segments">The num segments value.</param>
            internal void draw_arc1(Vector2 center, float radius, float start_angle, float end_angle, float thickness, Color color, int num_segments)
            {
                Vector2 start = new Vector2(center[0] + Mathf.Cos(start_angle) * radius, center[1] + Mathf.Sin(start_angle) * radius);
                Vector2 end = new Vector2(center[0] + Mathf.Cos(end_angle) * radius, center[1] + Mathf.Sin(end_angle) * radius);

                // Calculate bezier arc points
                float ax = start[0] - center[0];
                float ay = start[1] - center[1];
                float bx = end[0] - center[0];
                float by = end[1] - center[1];
                float q1 = ax * ax + ay * ay;
                float q2 = q1 + ax * bx + ay * by;
                float k2 = (4.0f / 3.0f) * (Mathf.Sqrt((2.0f * q1 * q2)) - q2) / (ax * by - ay * bx);
                Vector2 arc1 = new Vector2(center[0] + ax - k2 * ay, center[1] + ay + k2 * ax);
                Vector2 arc2 = new Vector2(center[0] + bx + k2 * by, center[1] + by - k2 * bx);

                ImDrawListPtr draw_list = ImGui.GetWindowDrawList();
                draw_list.AddBezierCubic(start, arc1, arc2, end, ImGui.GetColorU32(color), thickness, num_segments);
            }

            /// <summary>
            /// Runs the draw arc workflow.
            /// </summary>
            /// <param name="center">The center value.</param>
            /// <param name="radius">The radius value.</param>
            /// <param name="start_angle">The start angle value.</param>
            /// <param name="end_angle">The end angle value.</param>
            /// <param name="thickness">The thickness value.</param>
            /// <param name="color">The color value.</param>
            /// <param name="num_segments">The num segments value.</param>
            /// <param name="bezier_count">The bezier count value.</param>
            internal void draw_arc(Vector2 center, float radius, float start_angle, float end_angle, float thickness, Color color, int num_segments, int bezier_count)
            {
                // Overlap and angle of ends of bezier curves needs work, only looks good when not transperant
                float overlap = thickness * radius * 0.00001f * Mathf.PI;
                float delta = end_angle - start_angle;
                float bez_step = 1.0f / bezier_count;
                float mid_angle = start_angle + overlap;
                for (var i = 0; i < bezier_count - 1; i++)
                {
                    var mid_angle2 = delta * bez_step + mid_angle;
                    draw_arc1(center, radius, mid_angle - overlap, mid_angle2 + overlap, thickness, color, num_segments);
                    mid_angle = mid_angle2;
                }
                draw_arc1(center, radius, mid_angle - overlap, end_angle, thickness, color, num_segments);
            }

            /// <summary>
            /// Returns the knob with drag result.
            /// </summary>
            /// <param name="label">The label value.</param>
            /// <param name="p_value">The p value value.</param>
            /// <param name="v_min">The v min value.</param>
            /// <param name="v_max">The v max value.</param>
            /// <param name="_speed">The speed value.</param>
            /// <param name="format">The format value.</param>
            /// <param name="size">The size value.</param>
            /// <param name="flags">The flags value.</param>
            /// <returns>The result of the operation.</returns>
            knob knob_with_drag(string label, ref float p_value, float v_min, float v_max, float _speed, string format, float size, FuKnobFlags flags)
            {
                var speed = _speed == 0 ? (v_max - v_min) / 250.0f : _speed;
                ImGui.PushID(label);
                var width = size == 0 ? ImGui.GetTextLineHeight() * 4.0f : size * ImGui.GetIO().FontGlobalScale;
                ImGui.PushItemWidth(width);
                ImGui.BeginGroup();

                // Draw knob
                if (!_knobs.ContainsKey(label))
                {
                    _knobs.Add(label, new knob(width * 0.5f));
                }
                knob k = _knobs[label];

                FuFrameStyle.Default.Push(!LastItemDisabled);
                k.Draw(label, ref p_value, v_min, v_max, speed, format, flags, LastItemDisabled);
                FuFrameStyle.Default.Pop();

                // Draw tooltip
                if (flags.HasFlag(FuKnobFlags.ValueTooltip) && (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled) || ImGui.IsItemActive()))
                {
                    ImGui.SetTooltip(p_value.ToString("f2"));
                }

                ImGui.EndGroup();
                ImGui.PopItemWidth();
                ImGui.PopID();

                return k;
            }

            /// <summary>
            /// Gets the primary color set.
            /// </summary>
            /// <returns>The result of the operation.</returns>
            color_set GetPrimaryColorSet()
            {
                if (LastItemDisabled)
                {
                    return new color_set(
                        Fugui.Themes.GetColor(FuColors.CheckMark) * 0.5f,
                        Fugui.Themes.GetColor(FuColors.CheckMark) * 0.5f,
                        Fugui.Themes.GetColor(FuColors.CheckMark) * 0.5f);
                }
                else
                {
                    return new color_set(
                        Fugui.Themes.GetColor(FuColors.CheckMark),
                        Fugui.Themes.GetColor(FuColors.CheckMark),
                        Fugui.Themes.GetColor(FuColors.CheckMark));
                }
            }

            /// <summary>
            /// Gets the secondary color set.
            /// </summary>
            /// <returns>The result of the operation.</returns>
            color_set GetSecondaryColorSet()
            {
                if (LastItemDisabled)
                {
                    Vector4 active = Fugui.Themes.GetColor(FuColors.FrameBg) * 0.8f;
                    Vector4 hovered = Fugui.Themes.GetColor(FuColors.FrameBg) * 0.8f;
                    return new color_set(active, hovered, hovered);
                }
                else
                {
                    Vector4 active = Fugui.Themes.GetColor(FuColors.FrameBg) * 0.4f;
                    Vector4 hovered = Fugui.Themes.GetColor(FuColors.FrameBg) * 0.4f;
                    return new color_set(active, hovered, hovered);
                }
            }

            /// <summary>
            /// Gets the track color set.
            /// </summary>
            /// <returns>The result of the operation.</returns>
            color_set GetTrackColorSet()
            {
                if (LastItemDisabled)
                {
                    return new color_set(
                        Fugui.Themes.GetColor(FuColors.FrameBg) * 0.5f,
                        Fugui.Themes.GetColor(FuColors.FrameBg) * 0.5f,
                        Fugui.Themes.GetColor(FuColors.FrameBg) * 0.5f);
                }
                else
                {
                    return new color_set(
                        Fugui.Themes.GetColor(FuColors.FrameBg),
                        Fugui.Themes.GetColor(FuColors.FrameBg),
                        Fugui.Themes.GetColor(FuColors.FrameBg));
                }
            }

            /// <summary>
            /// Returns the base knob result.
            /// </summary>
            /// <param name="label">The label value.</param>
            /// <param name="p_value">The p value value.</param>
            /// <param name="v_min">The v min value.</param>
            /// <param name="v_max">The v max value.</param>
            /// <param name="speed">The speed value.</param>
            /// <param name="format">The format value.</param>
            /// <param name="variant">The variant value.</param>
            /// <param name="size">The size value.</param>
            /// <param name="flags">The flags value.</param>
            /// <param name="steps">The steps value.</param>
            /// <returns>The result of the operation.</returns>
            bool BaseKnob(string label, ref float p_value, float v_min, float v_max, float speed, string format, FuKnobVariant variant, float size, FuKnobFlags flags, int steps = 10)
            {
                var knob = knob_with_drag(label, ref p_value, v_min, v_max, speed, format, size, flags);

                switch (variant)
                {
                    case FuKnobVariant.Tick:
                        {
                            knob.draw_circle(0.85f, GetSecondaryColorSet(), 32);
                            knob.draw_tick(0.5f, 0.85f, 0.08f, knob.angle, GetPrimaryColorSet());
                            break;
                        }
                    case FuKnobVariant.Dot:
                        {
                            knob.draw_circle(0.85f, GetSecondaryColorSet(), 32);
                            knob.draw_dot(0.12f, 0.6f, knob.angle, GetPrimaryColorSet(), true, 12);
                            break;
                        }

                    case FuKnobVariant.Wiper:
                        {
                            knob.draw_circle(0.7f, GetSecondaryColorSet(), 32);
                            knob.draw_arc(this, 0.8f, 0.41f, knob.angle_min, knob.angle_max, GetTrackColorSet(), 16, 2);

                            if (knob.t > 0.01f)
                            {
                                knob.draw_arc(this, 0.8f, 0.43f, knob.angle_min, knob.angle, GetPrimaryColorSet(), 16, 2);
                            }
                            break;
                        }
                    case FuKnobVariant.WiperOnly:
                        {
                            knob.draw_arc(this, 0.8f, 0.41f, knob.angle_min, knob.angle_max, GetTrackColorSet(), 32, 2);

                            if (knob.t > 0.01)
                            {
                                knob.draw_arc(this, 0.8f, 0.43f, knob.angle_min, knob.angle, GetPrimaryColorSet(), 16, 2);
                            }
                            break;
                        }
                    case FuKnobVariant.WiperDot:
                        {
                            knob.draw_circle(0.6f, GetSecondaryColorSet(), 32);
                            knob.draw_arc(this, 0.85f, 0.41f, knob.angle_min, knob.angle_max, GetTrackColorSet(), 16, 2);
                            knob.draw_dot(0.1f, 0.85f, knob.angle, GetPrimaryColorSet(), true, 12);
                            break;
                        }
                    case FuKnobVariant.Stepped:
                        {
                            for (var n = 0.0f; n < steps; n++)
                            {
                                var a = n / (steps - 1);
                                var angle = knob.angle_min + (knob.angle_max - knob.angle_min) * a;
                                knob.draw_tick(0.7f, 0.9f, 0.04f, angle, GetPrimaryColorSet());
                            }

                            knob.draw_circle(0.6f, GetSecondaryColorSet(), 32);
                            knob.draw_dot(0.12f, 0.4f, knob.angle, GetPrimaryColorSet(), true, 12);
                            break;
                        }
                    case FuKnobVariant.Space:
                        {
                            knob.draw_circle(0.3f - knob.t * 0.1f, GetSecondaryColorSet(), 16);

                            if (knob.t > 0.01f)
                            {
                                knob.draw_arc(this, 0.4f, 0.15f, knob.angle_min - 1.0f, knob.angle - 1.0f, GetPrimaryColorSet(), 16, 2);
                                knob.draw_arc(this, 0.6f, 0.15f, knob.angle_min + 1.0f, knob.angle + 1.0f, GetPrimaryColorSet(), 16, 2);
                                knob.draw_arc(this, 0.8f, 0.15f, knob.angle_min + 3.0f, knob.angle + 3.0f, GetPrimaryColorSet(), 16, 2);
                            }
                            break;
                        }
                }

                return knob.value_changed;
            }

            /// <summary>
            /// Draw a knob button
            /// </summary>
            /// <param name="text">Laberl of the knob</param>
            /// <param name="value">value of the knob</param>
            /// <param name="min">minimum value</param>
            /// <param name="max">maximum value</param>
            /// <param name="variant">type of knob variant (Try it !)</param>
            /// <param name="steps">number of step of the knob (only for stepped knob)</param>
            /// <param name="speed">speed of the drag</param>
            /// <param name="format">string format of the drag</param>
            /// <param name="size">size of the knob</param>
            /// <param name="flags">behaviour flag</param>
            /// <returns>true if value chage</returns>
            public virtual bool Knob(string text, ref float value, float min = 0f, float max = 100f, FuKnobVariant variant = FuKnobVariant.Wiper, int steps = 10, float speed = 1f, string format = null, float size = 64f, FuKnobFlags flags = FuKnobFlags.Default)
            {
                beginElement(ref text);
                if (!_drawElement)
                {
                    return false;
                }
                string _format = format == null ? "%.3f" : format;
                value = Mathf.Clamp(value, min, max);
                bool updated = BaseKnob(text, ref value, min, max, speed, _format, variant, size, flags, steps);
                // set states for this element
                setBaseElementState(text, _currentItemStartPos, ImGui.GetItemRectMax() - _currentItemStartPos, true, updated);
                displayToolTip();
                endElement();
                return updated;
            }
            #endregion
        }
}