using ImGuiNET;
using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework
{
    internal struct color_set
    {
        internal Vector4 color;
        internal Vector4 hovered;
        internal Vector4 active;

        internal color_set(Vector4 _color, Vector4 _hovered, Vector4 _active)
        {
            color = _color;
            hovered = _hovered;
            active = _active;
        }

        internal color_set(Vector4 _color)
        {
            color = _color;
            hovered = _color;
            active = _color;
        }
    }

    internal class knob
    {
        public float radius;
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

        public knob(float _radius)
        {
            radius = _radius;
        }

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

        internal void draw_dot(float size, float radius, float angle, color_set color, bool filled, int segments)
        {
            var dot_size = size * this.radius;
            var dot_radius = radius * this.radius;

            ImGui.GetWindowDrawList().AddCircleFilled(new Vector2(center.x + Mathf.Cos(angle) * dot_radius, center.y + Mathf.Sin(angle) * dot_radius), dot_size, ImGui.GetColorU32(is_active ? color.active : (is_hovered ? color.hovered : color.color)), segments);
        }

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

        internal void draw_circle(float size, color_set color, int segments)
        {
            var circle_radius = size * radius;
            ImGui.GetWindowDrawList().AddCircleFilled(center, circle_radius, ImGui.GetColorU32(is_active ? color.active : (is_hovered ? color.hovered : color.color)), segments);
        }

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
    }

    public partial class FuLayout
    {
        internal Dictionary<string, knob> _knobs = new Dictionary<string, knob>();

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
    }
}