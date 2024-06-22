using Fu.Core;
using ImGuiNET;
using System;
using UnityEngine;

namespace Fu.Framework
{
    public partial class FuLayout
    {
        private string _loaderFakeID = string.Empty;

        /// <summary>
        /// Drraw a dots spinner that fade around a circle
        /// </summary>
        /// <param name="size">size of the spinner</param>
        /// <param name="circle_count">number of dots</param>
        public void Loader_CircleSpinner(float size, int circle_count)
        {
            beginElement(ref _loaderFakeID, null, true);
            if (!_drawElement)
            {
                return;
            }

            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            size /= 2f;
            Vector2 pos = ImGui.GetCursorScreenPos();
            float circle_radius = size / 10.0f;
            float speed = 4f;
            Vector4 main_color = FuThemeManager.GetColor(FuColors.CheckMark);
            Vector4 backdrop_color = FuThemeManager.GetColor(FuColors.FrameBg);
            float t = (float)ImGui.GetTime();
            float degree_offset = 2.0f * Mathf.PI / circle_count;
            for (int i = 0; i < circle_count; ++i)
            {
                float x = size * Mathf.Sin(degree_offset * i);
                float y = size * Mathf.Cos(degree_offset * i);
                float growth = Mathf.Max(0.0f, Mathf.Sin(t * speed - i * degree_offset));
                Vector4 color;
                color.x = main_color.x * growth + backdrop_color.x * (1.0f - growth);
                color.y = main_color.y * growth + backdrop_color.y * (1.0f - growth);
                color.z = main_color.z * growth + backdrop_color.z * (1.0f - growth);
                color.w = 1.0f;
                drawList.AddCircleFilled(new Vector2(pos.x + size + x, pos.y + size - y), circle_radius + growth * circle_radius, ImGui.GetColorU32(color));
            }

            Dummy(new Vector2(size * 2f, size * 2f));
            endElement();
        }

        /// <summary>
        /// draw a simple default loading spinner
        /// </summary>
        public void Loader_Spinner()
        {
            Loader_Spinner(20f, 6, 2f, false);
        }

        /// <summary>
        /// Draw a simple loading spinner
        /// It's some dots rotating around a circle
        /// </summary>
        /// <param name="size">size of the loader</param>
        /// <param name="numDots">number of dots in the spinner</param>
        /// <param name="dotSize">size of one dot</param>
        /// <param name="doubleColor">if true, evens dots are text color, others are checkmark color</param>
        public void Loader_Spinner(float size, int numDots, float dotSize, bool doubleColor = true)
        {
            beginElement(ref _loaderFakeID, noEditID: true);
            if (!_drawElement)
            {
                return;
            }

            Vector2 padding = FuThemeManager.FramePadding;
            float centerX = ImGui.GetCursorScreenPos().x + size / 2 + padding.x;
            float centerY = ImGui.GetCursorScreenPos().y + size / 2 + padding.y;
            float radius = size / 2;
            float animationTime = (float)ImGui.GetTime();
            float animationSpeed = .50f;
            float rotation = animationTime * animationSpeed * Mathf.PI * 2.0f;

            for (int i = 0; i < numDots; i++)
            {
                float angle = i * Mathf.PI * 2 / numDots + rotation;
                float dotX = centerX + Mathf.Cos(angle) * radius;
                float dotY = centerY + Mathf.Sin(angle) * radius;
                ImGui.GetWindowDrawList().AddCircleFilled(new Vector2(dotX, dotY), dotSize, doubleColor && i % 2 == 0 ? ImGui.GetColorU32(ImGuiCol.CheckMark) : ImGui.GetColorU32(ImGuiCol.Text));
            }

            ImGui.Dummy(new Vector2(size + (padding.x * 2f), size + (padding.y * 2f)));

            // force the current window to draw each frame to keep animation fluidity
            FuWindow.CurrentDrawingWindow?.ForceDraw();

            endElement();
        }

        /// <summary>
        /// Draw a loader that look like a clock
        /// </summary>
        /// <param name="size">size of the loader</param>
        public void Loader_Clocker(float size)
        {
            beginElement(ref _loaderFakeID, noEditID: true);
            if (!_drawElement)
            {
                return;
            }

            var drawList = ImGui.GetWindowDrawList();
            float animationTime = (float)ImGui.GetTime();
            float animationSpeed = .10f;
            float animationPosition = (float)Math.Sin(animationTime * animationSpeed * Math.PI * 2.0f);

            // Draw the background circle
            Vector2 center = ImGui.GetCursorScreenPos() + new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.45f;
            drawList.AddCircleFilled(center, radius, ImGui.GetColorU32(ImGuiCol.FrameBg), 32);

            // Draw the moving dot
            float dotRadius = size * 0.1f;
            Vector2 dotPos = center + new Vector2(radius * (float)Math.Cos(animationPosition * Math.PI * 2.0f), radius * (float)Math.Sin(animationPosition * Math.PI * 2.0f));
            drawList.AddCircleFilled(dotPos, dotRadius, ImGui.GetColorU32(ImGuiCol.CheckMark), 32);

            // Draw the connecting line
            Vector2 lineStart = center;
            Vector2 lineEnd = dotPos;
            drawList.AddLine(lineStart, lineEnd, ImGui.GetColorU32(ImGuiCol.CheckMark), 2.0f);

            // Draw the static dots
            int numDots = 12;
            float dotAngle = (float)(Math.PI * 2) / numDots;
            for (int i = 0; i < numDots; i++)
            {
                Vector2 staticDotPos = center + new Vector2(radius * (float)Math.Cos(dotAngle * i), radius * (float)Math.Sin(dotAngle * i));
                drawList.AddCircleFilled(staticDotPos, dotRadius * 0.5f, ImGui.GetColorU32(ImGuiCol.Text), 32);
            }

            ImGui.Dummy(new Vector2(size, size));

            // force the current window to draw each frame to keep animation fluidity
            FuWindow.CurrentDrawingWindow?.ForceDraw();

            endElement();
        }

        /// <summary>
        /// Draw a pulsar animation loader
        /// </summary>
        /// <param name="size">size of the loader</param>
        public void Loader_Pulsar(float size)
        {
            beginElement(ref _loaderFakeID, noEditID: true);
            if (!_drawElement)
            {
                return;
            }

            var drawList = ImGui.GetWindowDrawList();
            float animationTime = (float)ImGui.GetTime();
            float animationSpeed = 1.5f;
            float pulse = (float)Math.Sin(animationTime * animationSpeed * Math.PI * 2.0f) * 0.5f + 0.5f;
            Vector2 center = ImGui.GetCursorScreenPos() + new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.5f * pulse;
            float innerRadius = size * 0.1f;
            float rotation = animationTime * animationSpeed * Mathf.PI * 2.0f;
            Vector2 innerCirclePos = center + new Vector2((float)Math.Cos(rotation), (float)Math.Sin(rotation)) * innerRadius;
            drawList.AddCircleFilled(center, radius, ImGui.GetColorU32(ImGuiCol.FrameBg), 32);
            drawList.AddCircleFilled(innerCirclePos, innerRadius, ImGui.GetColorU32(ImGuiCol.CheckMark), 32);
            ImGui.Dummy(new Vector2(size, size));

            // force the current window to draw each frame to keep animation fluidity
            FuWindow.CurrentDrawingWindow?.ForceDraw();

            endElement();
        }

        /// <summary>
        /// Draw a complex pulsar animation loader with animated lines
        /// </summary>
        /// <param name="size">size of the loader</param>
        public void Loader_PulsingLines(Vector2 size)
        {
            beginElement(ref _loaderFakeID, noEditID: true);
            if (!_drawElement)
            {
                return;
            }

            var drawList = ImGui.GetWindowDrawList();

            float animationTime = (float)ImGui.GetTime();
            float animationSpeed = 2.0f;

            // Get the animation position
            float animationPosition = (float)Math.Sin(animationTime * animationSpeed * Math.PI * 2.0f) * 0.5f + 0.5f;

            // Get the animation scale
            float animationScale = (float)Math.Sin(animationTime * animationSpeed * Math.PI) * 0.5f + 0.5f;

            // Draw the outer circle
            Vector2 outerCirclePos = ImGui.GetCursorScreenPos() + size * 0.5f;
            Vector2 outerCircleSize = size * animationScale;
            drawList.AddCircleFilled(outerCirclePos, outerCircleSize.x * 0.5f, ImGui.GetColorU32(ImGuiCol.Text), 36);

            // Draw the inner circle
            Vector2 innerCirclePos = ImGui.GetCursorScreenPos() + size * 0.5f;
            Vector2 innerCircleSize = size * 0.8f * animationScale;
            drawList.AddCircleFilled(innerCirclePos, innerCircleSize.x * 0.5f, ImGui.GetColorU32(ImGuiCol.FrameBg), 36);

            // Draw the rectangles
            Vector2 rectangleSize = new Vector2(size.x * 0.1f, size.y * 0.8f * animationScale);
            for (int i = 0; i < 4; i++)
            {
                Vector2 rectanglePos = ImGui.GetCursorScreenPos() + new Vector2(size.x * 0.5f + size.x * 0.3f * (float)Math.Cos(animationPosition + i * Math.PI / 2.0f), size.y * 0.5f + size.y * 0.3f * (float)Math.Sin(animationPosition + i * Math.PI / 2.0f));
                drawList.AddRectFilled(rectanglePos - rectangleSize * 0.5f, rectanglePos + rectangleSize * 0.5f, ImGui.GetColorU32(i % 2 == 0 ? ImGuiCol.CheckMark : ImGuiCol.Text));
            }

            // Advance the cursor position
            ImGui.Dummy(size);

            // force the current window to draw each frame to keep animation fluidity
            FuWindow.CurrentDrawingWindow?.ForceDraw();

            endElement();
        }

        /// <summary>
        /// A circle trying to reach a square
        /// </summary>
        /// <param name="size">size of the loader</param>
        public void Loader_SquareCircleDance(float size)
        {
            beginElement(ref _loaderFakeID, noEditID: true);
            if (!_drawElement)
            {
                return;
            }

            var drawList = ImGui.GetWindowDrawList();
            float animationTime = (float)ImGui.GetTime();
            float animationSpeed = .5f;

            // Calculate the position of the first shape
            float shape1X = (float)Math.Sin(animationTime * animationSpeed * Math.PI * 2.0f) * (size / 2f);
            float shape1Y = (float)Math.Cos(animationTime * animationSpeed * Math.PI * 2.0f) * (size / 2f);

            // Draw the first shape
            Vector2 shape1Pos = new Vector2(shape1X, shape1Y) + ImGui.GetCursorScreenPos();
            drawList.AddCircleFilled(shape1Pos, size * 0.1f, ImGui.GetColorU32(ImGuiCol.CheckMark));

            // Calculate the position of the second shape
            float shape2X = (float)Math.Sin((animationTime + 0.5f) * animationSpeed * Math.PI * 2.0f) * (size / 2f) * 0.75f;
            float shape2Y = (float)Math.Cos((animationTime + 0.5f) * animationSpeed * Math.PI * 2.0f) * (size / 2f) * 0.75f;

            // Draw the second shape
            Vector2 shape2Pos = new Vector2(shape2X, shape2Y) + ImGui.GetCursorScreenPos();
            drawList.AddRectFilled(shape2Pos - new Vector2(size * 0.1f, size * 0.1f), shape2Pos + new Vector2(size * 0.1f, size * 0.1f), ImGui.GetColorU32(ImGuiCol.Text));

            ImGui.Dummy(new Vector2(size, size));

            // force the current window to draw each frame to keep animation fluidity
            FuWindow.CurrentDrawingWindow?.ForceDraw();

            endElement();
        }

        /// <summary>
        /// Draw an animated wave line
        /// </summary>
        /// <param name="size">size of the loader</param>
        /// <param name="frequency">frequency of the wave</param>
        public void Loader_WavyLine(Vector2 size, float frequency, bool doubleColor = true)
        {
            beginElement(ref _loaderFakeID, noEditID: true);
            if (!_drawElement)
            {
                return;
            }

            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            float animationTime = (float)ImGui.GetTime();
            Vector2 startPos = ImGui.GetCursorScreenPos();
            Vector2 endPos = startPos + size;
            float waveAmplitude = size.y / 2;
            float waveFrequency = frequency;
            int numSegments = (int)(size.x / 2);
            Vector2 prevPos = startPos;
            Vector4 startColor = FuThemeManager.GetColor(doubleColor ? FuColors.CheckMark : FuColors.Text);
            Vector4 endColor = FuThemeManager.GetColor(FuColors.Text);
            for (int i = 0; i <= numSegments; i++)
            {
                float t = (float)i / numSegments;
                float wavePhase = t * waveFrequency + animationTime * 2f;
                Vector2 currentPos = new Vector2(startPos.x + t * size.x, startPos.y + (float)Math.Sin(wavePhase) * waveAmplitude + size.y / 2f);
                if (i > 0)
                {
                    drawList.AddLine(prevPos, currentPos, ImGui.GetColorU32(Vector4.Lerp(startColor, endColor, (float)i / (float)numSegments)), 2);
                }
                prevPos = currentPos;
            }
            ImGui.Dummy(size);

            // force the current window to draw each frame to keep animation fluidity
            FuWindow.CurrentDrawingWindow?.ForceDraw();

            endElement();
        }

        /// <summary>
        /// Two dancing squares
        /// </summary>
        /// <param name="size">size of the loader</param>
        public void Loader_Squares(float size)
        {
            beginElement(ref _loaderFakeID, noEditID: true);
            if (!_drawElement)
            {
                return;
            }

            var drawList = ImGui.GetWindowDrawList();
            float animationTime = (float)ImGui.GetTime();
            float animationSpeed = 1f;
            float animationPosition = (float)Math.Sin(animationTime * animationSpeed * Math.PI * 2.0f) * 0.5f + 0.5f;
            Vector2 center = new Vector2(ImGui.GetCursorScreenPos().x + size * 0.5f, ImGui.GetCursorScreenPos().y + size * 0.5f);
            Vector2 offset = new Vector2(size * 0.4f * animationPosition, 0);
            Vector2 rectSize = new Vector2(size * 0.2f, size * 0.2f);

            drawList.AddRectFilled(center - rectSize + offset, center + rectSize + offset, ImGui.GetColorU32(ImGuiCol.Text));
            drawList.AddRectFilled(center - rectSize - offset, center + rectSize - offset, ImGui.GetColorU32(ImGuiCol.CheckMark));

            ImGui.Dummy(new Vector2(size, size));

            // force the current window to draw each frame to keep animation fluidity
            FuWindow.CurrentDrawingWindow?.ForceDraw();

            endElement();
        }

        /// <summary>
        /// Draw a wheel made of triangles slices
        /// </summary>
        /// <param name="size">size of the loader</param>
        /// <param name="slides">number of slides</param>
        /// <param name="doubleColor">if true, evens dots are text color, others are checkmark color</param>
        public void Loader_SpikedWheel(Vector2 size, int slides = 10, bool doubleColor = true)
        {
            beginElement(ref _loaderFakeID, noEditID: true);
            if (!_drawElement)
            {
                return;
            }

            float time = (float)ImGui.GetTime();
            time *= 2f;

            Vector2 center = ImGui.GetCursorScreenPos() + size * 0.5f;
            float radius = size.x * 0.35f;
            int numSides = slides;
            float angleStep = (float)(Math.PI * 2.0) / numSides;
            float innerRadius = radius * 0.75f;
            float outerRadius = radius;
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            for (int i = 0; i < numSides; i++)
            {
                float angle1 = angleStep * i + time;
                float angle2 = angleStep * (i + 1) + time;
                float angle3 = angleStep * (i + 2) + time;

                Vector2 inner1 = center + new Vector2((float)Math.Cos(angle1), (float)Math.Sin(angle1)) * innerRadius;
                Vector2 inner2 = center + new Vector2((float)Math.Cos(angle2), (float)Math.Sin(angle2)) * innerRadius;
                Vector2 outer1 = center + new Vector2((float)Math.Cos(angle2), (float)Math.Sin(angle2)) * outerRadius;
                Vector2 outer2 = center + new Vector2((float)Math.Cos(angle3), (float)Math.Sin(angle3)) * outerRadius;

                drawList.AddTriangleFilled(center, inner1, outer1, ImGui.GetColorU32(i % 2 == 0 && doubleColor ? ImGuiCol.Text : ImGuiCol.CheckMark));
                drawList.AddTriangleFilled(center, inner2, outer2, ImGui.GetColorU32(ImGuiCol.FrameBg));
            }
            ImGui.Dummy(size);

            // force the current window to draw each frame to keep animation fluidity
            FuWindow.CurrentDrawingWindow?.ForceDraw();

            endElement();
        }

        /// <summary>
        /// A simple wheel with some lines
        /// </summary>
        /// <param name="size">size of the wheel</param>
        /// <param name="doubleColor">if true, evens dots are text color, others are checkmark color</param>
        public void Loader_Wheel(float size, bool doubleColor = true)
        {
            beginElement(ref _loaderFakeID, noEditID: true);
            if (!_drawElement)
            {
                return;
            }

            var drawList = ImGui.GetWindowDrawList();
            float animationTime = (float)ImGui.GetTime();
            float animationSpeed = 3.0f;
            float animationPosition = Mathf.Sin(animationTime * animationSpeed * Mathf.PI * 2.0f);

            Vector2 center = ImGui.GetCursorScreenPos() + new Vector2(size / 2, size / 2);
            float radius = size / 3;
            float lineLength = radius * 0.8f;

            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI / 2 + animationTime * animationSpeed;
                Vector2 start = center + new Vector2((float)Math.Cos(angle) * radius, (float)Math.Sin(angle) * radius);
                Vector2 end = center + new Vector2((float)Math.Cos(angle) * (radius + lineLength), (float)Math.Sin(angle) * (radius + lineLength));

                drawList.AddLine(start, end, ImGui.GetColorU32(doubleColor && i % 2 == 0 ? ImGuiCol.Text : ImGuiCol.CheckMark), 3.0f);
            }

            drawList.AddCircleFilled(center, radius, ImGui.GetColorU32(ImGuiCol.FrameBg), 12);

            ImGui.Dummy(new Vector2(size, size));

            // force the current window to draw each frame to keep animation fluidity
            FuWindow.CurrentDrawingWindow?.ForceDraw();

            endElement();
        }

        /// <summary>
        /// Draw an elipse dots spinned
        /// </summary>
        /// <param name="size">size of the loader</param>
        /// <param name="numDots">number of dots</param>
        /// <param name="dotSize">size of a dot</param>
        /// <param name="doubleColor">if true, evens dots are text color, others are checkmark color</param>
        public void Loader_ElipseSpinner(float size, int numDots, float dotSize = 2f, bool doubleColor = true)
        {
            beginElement(ref _loaderFakeID, noEditID: true);
            if (!_drawElement)
            {
                return;
            }

            float animationTime = (float)ImGui.GetTime();
            float animationSpeed = 0.5f;
            float animationPosition = (float)Math.Sin(animationTime * animationSpeed * Math.PI * 2.0f);

            Vector2 center = ImGui.GetCursorScreenPos() + new Vector2(size / 2, size / 2);
            Vector2 radius = new Vector2(size / 2, size / 4);
            Vector2 dotRadius = new Vector2(dotSize, dotSize);

            for (int i = 0; i < numDots; i++)
            {
                float angle = i * 2 * (float)Math.PI / numDots - animationPosition;
                Vector2 pos = center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;
                ImGui.GetWindowDrawList().AddCircleFilled(pos, dotSize, ImGui.GetColorU32(doubleColor && i % 2 == 0 ? ImGuiCol.Text : ImGuiCol.CheckMark));
            }

            ImGui.Dummy(new Vector2(size, size));

            // force the current window to draw each frame to keep animation fluidity
            FuWindow.CurrentDrawingWindow?.ForceDraw();

            endElement();
        }
    }
}