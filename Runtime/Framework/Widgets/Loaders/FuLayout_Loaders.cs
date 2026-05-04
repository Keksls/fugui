using ImGuiNET;
using UnityEngine;

namespace Fu.Framework
{
    /// <summary>
    /// Represents the Fu Layout type.
    /// </summary>
    public partial class FuLayout
    {
        #region State
        private const float LoaderTau = 6.28318530718f;
        private string _loaderFakeID = string.Empty;
        #endregion

        #region Methods
        /// <summary>
        /// Draw a modern arc spinner with a quiet track and a rotating highlighted segment.
        /// </summary>
        /// <param name="size">Size of the loader.</param>
        /// <param name="thickness">Arc thickness.</param>
        public void Loader_Arc(float size, float thickness = 3f)
        {
            if (!beginLoader(new Vector2(size, size), out ImDrawListPtr drawList, out _, out Vector2 center, out Vector2 scaledSize))
            {
                return;
            }

            float minSize = Mathf.Min(scaledSize.x, scaledSize.y);
            float stroke = scaledThickness(thickness, minSize);
            float radius = Mathf.Max(1f, minSize * 0.5f - stroke * 0.5f - Fugui.Scale);
            float time = (float)ImGui.GetTime();
            float pulse = pulse01(time * 0.55f);
            float start = time * 2.55f;
            float sweep = LoaderTau * Mathf.Lerp(0.48f, 0.70f, pulse);

            drawList.AddCircle(center, radius, loaderColor(FuColors.Text, 0.11f), 64, stroke);
            drawArc(drawList, center, radius, start, start + sweep, loaderColor(FuColors.CheckMark), stroke);

            finishLoader(scaledSize);
        }

        /// <summary>
        /// Draw two counter-rotating clean circular strokes.
        /// </summary>
        /// <param name="size">Size of the loader.</param>
        /// <param name="thickness">Arc thickness.</param>
        public void Loader_DualRing(float size, float thickness = 3f)
        {
            if (!beginLoader(new Vector2(size, size), out ImDrawListPtr drawList, out _, out Vector2 center, out Vector2 scaledSize))
            {
                return;
            }

            float minSize = Mathf.Min(scaledSize.x, scaledSize.y);
            float stroke = scaledThickness(thickness, minSize);
            float outerRadius = Mathf.Max(1f, minSize * 0.42f);
            float innerRadius = Mathf.Max(1f, outerRadius - stroke * 2.3f);
            float time = (float)ImGui.GetTime();

            drawList.AddCircle(center, outerRadius, loaderColor(FuColors.Text, 0.09f), 64, stroke);
            drawList.AddCircle(center, innerRadius, loaderColor(FuColors.Text, 0.07f), 64, Mathf.Max(1f, stroke * 0.72f));
            drawArc(drawList, center, outerRadius, time * 2.2f, time * 2.2f + LoaderTau * 0.58f, loaderColor(FuColors.CheckMark), stroke);
            drawArc(drawList, center, innerRadius, -time * 2.9f, -time * 2.9f + LoaderTau * 0.38f, loaderColor(FuColors.Text, 0.76f), Mathf.Max(1f, stroke * 0.72f));

            finishLoader(scaledSize);
        }

        /// <summary>
        /// Draw breathing dots aligned on a compact horizontal rail.
        /// </summary>
        /// <param name="size">Size of the loader.</param>
        /// <param name="dotCount">Number of dots.</param>
        public void Loader_BreathingDots(Vector2 size, int dotCount = 3)
        {
            if (!beginLoader(size, out ImDrawListPtr drawList, out Vector2 pos, out Vector2 center, out Vector2 scaledSize))
            {
                return;
            }

            dotCount = Mathf.Clamp(dotCount, 2, 8);
            float time = (float)ImGui.GetTime();
            float gap = scaledSize.x / (dotCount + 1f);
            float radius = Mathf.Clamp(Mathf.Min(gap, scaledSize.y) * 0.18f, 1.6f * Fugui.Scale, scaledSize.y * 0.32f);

            for (int i = 0; i < dotCount; i++)
            {
                float phase = pulse01(time * 1.35f - i * 0.18f);
                float eased = easeInOut(phase);
                float dotRadius = radius * Mathf.Lerp(0.72f, 1.22f, eased);
                float y = center.y - (eased - 0.5f) * scaledSize.y * 0.14f;
                Vector2 dotPos = new Vector2(pos.x + gap * (i + 1f), y);
                uint color = ImGui.GetColorU32(Vector4.Lerp(
                    Fugui.Themes.GetColor(FuColors.Text, 0.34f * loaderAlpha()),
                    Fugui.Themes.GetColor(FuColors.CheckMark, loaderAlpha()),
                    eased));
                drawList.AddCircleFilled(dotPos, dotRadius, color, 24);
            }

            finishLoader(scaledSize);
        }

        /// <summary>
        /// Draw a compact equalizer-style loader.
        /// </summary>
        /// <param name="size">Size of the loader.</param>
        /// <param name="barCount">Number of bars.</param>
        public void Loader_Bars(Vector2 size, int barCount = 5)
        {
            if (!beginLoader(size, out ImDrawListPtr drawList, out Vector2 pos, out _, out Vector2 scaledSize))
            {
                return;
            }

            barCount = Mathf.Clamp(barCount, 3, 9);
            float time = (float)ImGui.GetTime();
            float gap = Mathf.Max(2f * Fugui.Scale, scaledSize.x * 0.045f);
            float barWidth = Mathf.Max(2f * Fugui.Scale, (scaledSize.x - gap * (barCount - 1f)) / barCount);
            float rounding = barWidth * 0.5f;

            for (int i = 0; i < barCount; i++)
            {
                float phase = easeInOut(pulse01(time * 1.42f - i * 0.13f));
                float height = Mathf.Lerp(scaledSize.y * 0.28f, scaledSize.y * 0.92f, phase);
                float x = pos.x + i * (barWidth + gap);
                float y = pos.y + (scaledSize.y - height) * 0.5f;
                uint color = ImGui.GetColorU32(Vector4.Lerp(
                    Fugui.Themes.GetColor(FuColors.Text, 0.28f * loaderAlpha()),
                    Fugui.Themes.GetColor(FuColors.CheckMark, loaderAlpha()),
                    phase));
                drawList.AddRectFilled(new Vector2(x, y), new Vector2(x + barWidth, y + height), color, rounding);
            }

            finishLoader(scaledSize);
        }

        /// <summary>
        /// Draw a subtle shimmering skeleton bar.
        /// </summary>
        /// <param name="size">Size of the loader.</param>
        public void Loader_Shimmer(Vector2 size)
        {
            if (!beginLoader(size, out ImDrawListPtr drawList, out Vector2 pos, out _, out Vector2 scaledSize))
            {
                return;
            }

            Vector2 max = pos + scaledSize;
            float rounding = Mathf.Min(Fugui.Themes.FrameRounding * Fugui.Scale, scaledSize.y * 0.5f);
            uint bg = loaderColor(FuColors.FrameBg, 0.72f);
            uint border = loaderColor(FuColors.Border, 0.42f);
            drawList.AddRectFilled(pos, max, bg, rounding);
            drawList.AddRect(pos, max, border, rounding, ImDrawFlags.RoundCornersAll, Mathf.Max(1f, Fugui.Scale));

            float time = (float)ImGui.GetTime();
            float bandWidth = Mathf.Max(12f * Fugui.Scale, scaledSize.x * 0.34f);
            float x = pos.x - bandWidth + Mathf.Repeat(time * 0.72f, 1f) * (scaledSize.x + bandWidth * 2f);
            float mid = x + bandWidth * 0.5f;
            uint clear = ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0f));
            uint shine = loaderColor(FuColors.Text, 0.13f);

            drawList.PushClipRect(pos, max, true);
            drawList.AddRectFilledMultiColor(new Vector2(x, pos.y), new Vector2(mid, max.y), clear, shine, shine, clear);
            drawList.AddRectFilledMultiColor(new Vector2(mid, pos.y), new Vector2(x + bandWidth, max.y), shine, clear, clear, shine);
            drawList.PopClipRect();

            finishLoader(scaledSize);
        }

        /// <summary>
        /// Draw clean dots orbiting around a small center dot.
        /// </summary>
        /// <param name="size">Size of the loader.</param>
        /// <param name="dotCount">Number of orbiting dots.</param>
        public void Loader_Orbit(float size, int dotCount = 3)
        {
            if (!beginLoader(new Vector2(size, size), out ImDrawListPtr drawList, out _, out Vector2 center, out Vector2 scaledSize))
            {
                return;
            }

            dotCount = Mathf.Clamp(dotCount, 1, 6);
            float minSize = Mathf.Min(scaledSize.x, scaledSize.y);
            float radius = minSize * 0.34f;
            float dotRadius = Mathf.Max(1.6f * Fugui.Scale, minSize * 0.075f);
            float time = (float)ImGui.GetTime();

            drawList.AddCircle(center, radius, loaderColor(FuColors.Text, 0.10f), 48, Mathf.Max(1f, Fugui.Scale));
            drawList.AddCircleFilled(center, dotRadius * 0.58f, loaderColor(FuColors.Text, 0.34f), 18);

            for (int i = 0; i < dotCount; i++)
            {
                float offset = i / (float)dotCount;
                float angle = time * 2.25f + offset * LoaderTau;
                float glow = easeInOut(pulse01(time * 0.85f + offset));
                Vector2 dotPos = center + angleToVector(angle) * radius;
                uint color = ImGui.GetColorU32(Vector4.Lerp(
                    Fugui.Themes.GetColor(FuColors.Text, 0.46f * loaderAlpha()),
                    Fugui.Themes.GetColor(FuColors.CheckMark, loaderAlpha()),
                    glow));
                drawList.AddCircleFilled(dotPos, dotRadius * Mathf.Lerp(0.82f, 1.18f, glow), color, 24);
            }

            finishLoader(scaledSize);
        }

        /// <summary>
        /// Draw a dots spinner that fades around a circle.
        /// </summary>
        /// <param name="size">Size of the spinner.</param>
        /// <param name="circle_count">Number of dots.</param>
        public void Loader_CircleSpinner(float size, int circle_count)
        {
            drawDotsSpinner(size, circle_count, Mathf.Max(1.8f, size * 0.075f), false, 1.55f);
        }

        /// <summary>
        /// Draw a simple default loading spinner.
        /// </summary>
        public void Loader_Spinner()
        {
            Loader_Spinner(20f, 8, 2.2f, false);
        }

        /// <summary>
        /// Draw a clean dot trail rotating around a circle.
        /// </summary>
        /// <param name="size">Size of the loader.</param>
        /// <param name="numDots">Number of dots in the spinner.</param>
        /// <param name="dotSize">Size of one dot.</param>
        /// <param name="doubleColor">If true, alternate dots between text and accent colors.</param>
        public void Loader_Spinner(float size, int numDots, float dotSize, bool doubleColor = true)
        {
            drawDotsSpinner(size, numDots, dotSize, doubleColor, 1.15f);
        }

        /// <summary>
        /// Draw a clock-like loader with clean ticks and moving hands.
        /// </summary>
        /// <param name="size">Size of the loader.</param>
        public void Loader_Clocker(float size)
        {
            if (!beginLoader(new Vector2(size, size), out ImDrawListPtr drawList, out _, out Vector2 center, out Vector2 scaledSize))
            {
                return;
            }

            float minSize = Mathf.Min(scaledSize.x, scaledSize.y);
            float radius = minSize * 0.39f;
            float stroke = Mathf.Max(1.1f * Fugui.Scale, minSize * 0.045f);
            float time = (float)ImGui.GetTime();

            drawList.AddCircle(center, radius, loaderColor(FuColors.Text, 0.14f), 64, stroke);
            for (int i = 0; i < 12; i++)
            {
                float angle = i / 12f * LoaderTau;
                Vector2 outer = center + angleToVector(angle) * radius;
                Vector2 inner = center + angleToVector(angle) * (radius - stroke * (i % 3 == 0 ? 2.3f : 1.35f));
                drawRoundedLine(drawList, inner, outer, loaderColor(FuColors.Text, i % 3 == 0 ? 0.34f : 0.18f), Mathf.Max(1f, stroke * 0.62f));
            }

            Vector2 second = center + angleToVector(time * LoaderTau * 0.62f - Mathf.PI * 0.5f) * (radius * 0.72f);
            Vector2 minute = center + angleToVector(time * LoaderTau * 0.18f - Mathf.PI * 0.5f) * (radius * 0.48f);
            drawRoundedLine(drawList, center, minute, loaderColor(FuColors.Text, 0.62f), stroke * 0.8f);
            drawRoundedLine(drawList, center, second, loaderColor(FuColors.CheckMark), stroke);
            drawList.AddCircleFilled(center, stroke * 1.15f, loaderColor(FuColors.CheckMark), 18);

            finishLoader(scaledSize);
        }

        /// <summary>
        /// Draw a soft pulsar made of expanding rings.
        /// </summary>
        /// <param name="size">Size of the loader.</param>
        public void Loader_Pulsar(float size)
        {
            if (!beginLoader(new Vector2(size, size), out ImDrawListPtr drawList, out _, out Vector2 center, out Vector2 scaledSize))
            {
                return;
            }

            float minSize = Mathf.Min(scaledSize.x, scaledSize.y);
            float stroke = Mathf.Max(1f, minSize * 0.035f);
            float time = (float)ImGui.GetTime();

            for (int i = 0; i < 3; i++)
            {
                float phase = Mathf.Repeat(time * 0.72f + i / 3f, 1f);
                float eased = easeOut(phase);
                float radius = Mathf.Lerp(minSize * 0.12f, minSize * 0.43f, eased);
                float alpha = (1f - eased) * 0.44f;
                drawList.AddCircle(center, radius, loaderColor(i == 0 ? FuColors.CheckMark : FuColors.Text, alpha), 64, stroke);
            }

            drawList.AddCircleFilled(center, minSize * 0.075f, loaderColor(FuColors.CheckMark), 24);

            finishLoader(scaledSize);
        }

        /// <summary>
        /// Draw animated vertical bars.
        /// </summary>
        /// <param name="size">Size of the loader.</param>
        public void Loader_PulsingLines(Vector2 size)
        {
            Loader_Bars(size, 5);
        }

        /// <summary>
        /// Draw two clean dots orbiting on a compact oval path.
        /// </summary>
        /// <param name="size">Size of the loader.</param>
        public void Loader_SquareCircleDance(float size)
        {
            if (!beginLoader(new Vector2(size, size), out ImDrawListPtr drawList, out _, out Vector2 center, out Vector2 scaledSize))
            {
                return;
            }

            float minSize = Mathf.Min(scaledSize.x, scaledSize.y);
            Vector2 radius = new Vector2(minSize * 0.28f, minSize * 0.16f);
            float dotRadius = Mathf.Max(1.8f * Fugui.Scale, minSize * 0.08f);
            float time = (float)ImGui.GetTime();

            drawEllipse(drawList, center, radius, loaderColor(FuColors.Text, 0.11f), Mathf.Max(1f, Fugui.Scale), 48);
            for (int i = 0; i < 2; i++)
            {
                float angle = time * 2.6f + i * Mathf.PI;
                Vector2 dotPos = center + new Vector2(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y);
                drawList.AddCircleFilled(dotPos, dotRadius, loaderColor(i == 0 ? FuColors.CheckMark : FuColors.Text, i == 0 ? 1f : 0.72f), 24);
            }

            finishLoader(scaledSize);
        }

        /// <summary>
        /// Draw an animated wave line.
        /// </summary>
        /// <param name="size">Size of the loader.</param>
        /// <param name="frequency">Frequency of the wave.</param>
        /// <param name="doubleColor">If true, draw the wave with an accent-to-text gradient.</param>
        public void Loader_WavyLine(Vector2 size, float frequency, bool doubleColor = true)
        {
            if (!beginLoader(size, out ImDrawListPtr drawList, out Vector2 pos, out _, out Vector2 scaledSize))
            {
                return;
            }

            float time = (float)ImGui.GetTime();
            int segments = Mathf.Clamp(Mathf.CeilToInt(scaledSize.x / (4f * Fugui.Scale)), 12, 96);
            float amplitude = scaledSize.y * 0.32f;
            float waveFrequency = Mathf.Clamp(frequency, 0.5f, 24f);
            float stroke = Mathf.Max(1.4f * Fugui.Scale, scaledSize.y * 0.09f);
            uint startColor = loaderColor(doubleColor ? FuColors.CheckMark : FuColors.Text, doubleColor ? 0.95f : 0.70f);
            uint endColor = loaderColor(FuColors.Text, doubleColor ? 0.58f : 0.28f);
            Vector2 previous = new Vector2(pos.x, pos.y + scaledSize.y * 0.5f);

            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                float wave = Mathf.Sin(t * waveFrequency + time * 3.2f);
                Vector2 current = new Vector2(pos.x + scaledSize.x * t, pos.y + scaledSize.y * 0.5f + wave * amplitude);
                Fugui.DrawLineGradient(drawList, previous, current, (i - 1f) / segments, t, stroke, startColor, endColor);
                previous = current;
            }

            float headT = Mathf.Repeat(time * 0.38f, 1f);
            float headWave = Mathf.Sin(headT * waveFrequency + time * 3.2f);
            Vector2 head = new Vector2(pos.x + scaledSize.x * headT, pos.y + scaledSize.y * 0.5f + headWave * amplitude);
            drawList.AddCircleFilled(head, stroke * 1.05f, loaderColor(FuColors.CheckMark), 18);

            finishLoader(scaledSize);
        }

        /// <summary>
        /// Draw a clean 2x2 chasing-squares loader.
        /// </summary>
        /// <param name="size">Size of the loader.</param>
        public void Loader_Squares(float size)
        {
            if (!beginLoader(new Vector2(size, size), out ImDrawListPtr drawList, out _, out Vector2 center, out Vector2 scaledSize))
            {
                return;
            }

            float minSize = Mathf.Min(scaledSize.x, scaledSize.y);
            float squareSize = minSize * 0.19f;
            float radius = Mathf.Max(1.5f * Fugui.Scale, squareSize * 0.24f);
            float offset = minSize * 0.18f;
            float time = (float)ImGui.GetTime();
            Vector2[] positions =
            {
                new Vector2(-offset, -offset),
                new Vector2(offset, -offset),
                new Vector2(offset, offset),
                new Vector2(-offset, offset)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                float phase = 1f - Mathf.Repeat(time * 0.95f - i / 4f, 1f);
                float active = Mathf.Pow(phase, 2.2f);
                float s = squareSize * Mathf.Lerp(0.82f, 1.28f, active);
                Vector2 squareCenter = center + positions[i] * Mathf.Lerp(0.82f, 1.14f, active);
                uint color = ImGui.GetColorU32(Vector4.Lerp(
                    Fugui.Themes.GetColor(FuColors.Text, 0.30f * loaderAlpha()),
                    Fugui.Themes.GetColor(FuColors.CheckMark, loaderAlpha()),
                    active));
                drawList.AddRectFilled(squareCenter - Vector2.one * s * 0.5f, squareCenter + Vector2.one * s * 0.5f, color, radius);
            }

            finishLoader(scaledSize);
        }

        /// <summary>
        /// Draw a clean segmented wheel.
        /// </summary>
        /// <param name="size">Size of the loader.</param>
        /// <param name="slides">Number of segments.</param>
        /// <param name="doubleColor">If true, mix text and accent colors.</param>
        public void Loader_SpikedWheel(Vector2 size, int slides = 10, bool doubleColor = true)
        {
            if (!beginLoader(size, out ImDrawListPtr drawList, out _, out Vector2 center, out Vector2 scaledSize))
            {
                return;
            }

            int segments = Mathf.Clamp(slides, 6, 24);
            float minSize = Mathf.Min(scaledSize.x, scaledSize.y);
            float radius = minSize * 0.38f;
            float tickLength = minSize * 0.15f;
            float stroke = Mathf.Max(1.4f * Fugui.Scale, minSize * 0.055f);
            float time = (float)ImGui.GetTime() * 1.25f;

            for (int i = 0; i < segments; i++)
            {
                float dot = i / (float)segments;
                float trail = 1f - Mathf.Repeat(dot - time, 1f);
                float active = Mathf.Pow(trail, 2.1f);
                float angle = dot * LoaderTau + time * LoaderTau * 0.12f;
                Vector2 direction = angleToVector(angle);
                Vector2 start = center + direction * (radius - tickLength);
                Vector2 end = center + direction * radius;
                FuColors baseColor = doubleColor && i % 2 == 0 ? FuColors.Text : FuColors.CheckMark;
                drawRoundedLine(drawList, start, end, loaderColor(baseColor, Mathf.Lerp(0.16f, 0.94f, active)), stroke);
            }

            finishLoader(scaledSize);
        }

        /// <summary>
        /// Draw a simple wheel with rotating clean arc dashes.
        /// </summary>
        /// <param name="size">Size of the wheel.</param>
        /// <param name="doubleColor">If true, alternate text and accent colors.</param>
        public void Loader_Wheel(float size, bool doubleColor = true)
        {
            if (!beginLoader(new Vector2(size, size), out ImDrawListPtr drawList, out _, out Vector2 center, out Vector2 scaledSize))
            {
                return;
            }

            float minSize = Mathf.Min(scaledSize.x, scaledSize.y);
            float stroke = scaledThickness(3f, minSize);
            float radius = Mathf.Max(1f, minSize * 0.38f);
            float time = (float)ImGui.GetTime() * 2.2f;

            drawList.AddCircle(center, radius, loaderColor(FuColors.Text, 0.10f), 64, stroke);
            for (int i = 0; i < 4; i++)
            {
                float phase = i / 4f;
                float start = time + phase * LoaderTau;
                float active = 1f - Mathf.Repeat(phase - time * 0.08f, 1f);
                FuColors color = doubleColor && i % 2 == 0 ? FuColors.Text : FuColors.CheckMark;
                drawArc(drawList, center, radius, start, start + LoaderTau * 0.105f, loaderColor(color, Mathf.Lerp(0.40f, 1f, active)), stroke);
            }

            finishLoader(scaledSize);
        }

        /// <summary>
        /// Draw an ellipse dots spinner.
        /// </summary>
        /// <param name="size">Size of the loader.</param>
        /// <param name="numDots">Number of dots.</param>
        /// <param name="dotSize">Size of a dot.</param>
        /// <param name="doubleColor">If true, alternate text and accent colors.</param>
        public void Loader_EllipseSpinner(float size, int numDots, float dotSize = 2f, bool doubleColor = true)
        {
            if (!beginLoader(new Vector2(size, size), out ImDrawListPtr drawList, out _, out Vector2 center, out Vector2 scaledSize))
            {
                return;
            }

            int dots = Mathf.Clamp(numDots, 4, 32);
            float scaledDot = Mathf.Clamp(dotSize * Fugui.Scale, 1f * Fugui.Scale, scaledSize.y * 0.12f);
            float time = (float)ImGui.GetTime();
            Vector2 radius = new Vector2(scaledSize.x * 0.39f, scaledSize.y * 0.23f);
            float rotation = time * 2.15f;

            drawEllipse(drawList, center, radius, loaderColor(FuColors.Text, 0.10f), Mathf.Max(1f, Fugui.Scale), 64);
            for (int i = 0; i < dots; i++)
            {
                float t = i / (float)dots;
                float trail = 1f - Mathf.Repeat(t - time * 0.38f, 1f);
                float active = Mathf.Pow(trail, 1.85f);
                float angle = t * LoaderTau + rotation;
                Vector2 dotPos = center + new Vector2(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y);
                FuColors color = doubleColor && i % 2 == 0 ? FuColors.Text : FuColors.CheckMark;
                drawList.AddCircleFilled(dotPos, scaledDot * Mathf.Lerp(0.72f, 1.35f, active), loaderColor(color, Mathf.Lerp(0.20f, 0.96f, active)), 20);
            }

            finishLoader(scaledSize);
        }

        /// <summary>
        /// Draw an ellipse dots spinner.
        /// </summary>
        /// <param name="size">Size of the loader.</param>
        /// <param name="numDots">Number of dots.</param>
        /// <param name="dotSize">Size of a dot.</param>
        /// <param name="doubleColor">If true, alternate text and accent colors.</param>
        public void Loader_ElipseSpinner(float size, int numDots, float dotSize = 2f, bool doubleColor = true)
        {
            Loader_EllipseSpinner(size, numDots, dotSize, doubleColor);
        }

        private bool beginLoader(Vector2 size, out ImDrawListPtr drawList, out Vector2 pos, out Vector2 center, out Vector2 scaledSize)
        {
            beginElement(ref _loaderFakeID, noEditID: true);
            if (!_drawElement)
            {
                drawList = default;
                pos = default;
                center = default;
                scaledSize = default;
                return false;
            }

            scaledSize = new Vector2(Mathf.Max(4f, size.x), Mathf.Max(4f, size.y)) * Fugui.Scale;
            drawList = ImGui.GetWindowDrawList();
            pos = ImGui.GetCursorScreenPos();
            center = pos + scaledSize * 0.5f;
            return true;
        }

        private void finishLoader(Vector2 scaledSize)
        {
            ImGui.Dummy(scaledSize);
            FuWindow.CurrentDrawingWindow?.ForceDraw();
            endElement();
        }

        private void drawDotsSpinner(float size, int numDots, float dotSize, bool doubleColor, float speed)
        {
            if (!beginLoader(new Vector2(size, size), out ImDrawListPtr drawList, out _, out Vector2 center, out Vector2 scaledSize))
            {
                return;
            }

            int dots = Mathf.Clamp(numDots, 3, 32);
            float minSize = Mathf.Min(scaledSize.x, scaledSize.y);
            float scaledDot = Mathf.Clamp(dotSize * Fugui.Scale, 1f * Fugui.Scale, minSize * 0.14f);
            float radius = Mathf.Max(1f, minSize * 0.42f - scaledDot);
            float time = (float)ImGui.GetTime();

            for (int i = 0; i < dots; i++)
            {
                float offset = i / (float)dots;
                float trail = 1f - Mathf.Repeat(offset - time * speed * 0.18f, 1f);
                float active = Mathf.Pow(trail, 1.9f);
                float angle = offset * LoaderTau + time * speed;
                Vector2 dotPos = center + angleToVector(angle) * radius;
                FuColors baseColor = doubleColor && i % 2 == 0 ? FuColors.Text : FuColors.CheckMark;
                drawList.AddCircleFilled(
                    dotPos,
                    scaledDot * Mathf.Lerp(0.70f, 1.32f, active),
                    loaderColor(baseColor, Mathf.Lerp(0.17f, 0.95f, active)),
                    20);
            }

            finishLoader(scaledSize);
        }

        private static void drawArc(ImDrawListPtr drawList, Vector2 center, float radius, float startAngle, float endAngle, uint color, float thickness)
        {
            float sweep = Mathf.Abs(endAngle - startAngle);
            int segments = Mathf.Clamp(Mathf.CeilToInt(sweep / LoaderTau * 72f), 8, 96);
            drawList.PathArcTo(center, radius, startAngle, endAngle, segments);
            drawList.PathStroke(color, ImDrawFlags.None, thickness);

            float capRadius = thickness * 0.5f;
            drawList.AddCircleFilled(center + angleToVector(startAngle) * radius, capRadius, color, 16);
            drawList.AddCircleFilled(center + angleToVector(endAngle) * radius, capRadius, color, 16);
        }

        private static void drawEllipse(ImDrawListPtr drawList, Vector2 center, Vector2 radius, uint color, float thickness, int segments)
        {
            segments = Mathf.Clamp(segments, 16, 128);
            Vector2 previous = center + new Vector2(radius.x, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i / (float)segments * LoaderTau;
                Vector2 current = center + new Vector2(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y);
                drawList.AddLine(previous, current, color, thickness);
                previous = current;
            }
        }

        private static void drawRoundedLine(ImDrawListPtr drawList, Vector2 start, Vector2 end, uint color, float thickness)
        {
            drawList.AddLine(start, end, color, thickness);
            float radius = thickness * 0.5f;
            drawList.AddCircleFilled(start, radius, color, 12);
            drawList.AddCircleFilled(end, radius, color, 12);
        }

        private static float scaledThickness(float thickness, float minSize)
        {
            float scaled = thickness * Fugui.Scale;
            return Mathf.Clamp(scaled, Mathf.Max(1f, Fugui.Scale), Mathf.Max(Fugui.Scale, minSize * 0.18f));
        }

        private float loaderAlpha()
        {
            return LastItemDisabled ? 0.42f : 1f;
        }

        private uint loaderColor(FuColors color, float alpha = 1f)
        {
            return Fugui.Themes.GetColorU32(color, alpha * loaderAlpha());
        }

        private static Vector2 angleToVector(float angle)
        {
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        private static float pulse01(float value)
        {
            return (Mathf.Sin(value * LoaderTau) + 1f) * 0.5f;
        }

        private static float easeInOut(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static float easeOut(float value)
        {
            value = Mathf.Clamp01(value);
            return 1f - Mathf.Pow(1f - value, 2.4f);
        }
        #endregion
    }
}
