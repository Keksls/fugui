using Fugui.Core;
using ImGuiNET;
using UnityEngine;

namespace Fugui.Framework
{
    public class UIElementAnimationData
    {
        public const float ANIMATION_DURATION = 0.1f;
        public float CurrentValue { get { return _currentValue; } }
        private float _currentValue;
        private bool _animating;
        private bool _lastFrameValue;
        private float _targetValue;
        private float _startValue;
        private float _enlapsed;

        public UIElementAnimationData(bool defaultValue)
        {
            _lastFrameValue = defaultValue;
        }

        private void animate(bool value)
        {
            _animating = true;
            _currentValue = value ? 0f : 1f;
            _targetValue = value ? 1f : 0f;
            _startValue = CurrentValue;
            _enlapsed = 0f;
        }

        public void Update(bool value, bool enableAnimation)
        {
            if (!enableAnimation)
            {
                _currentValue = value ? 1f : 0f;
                return;
            }

            if (value != _lastFrameValue)
            {
                animate(value);
                _lastFrameValue = value;
                return;
            }

            _lastFrameValue = value;

            if (!_animating)
            {
                return;
            }
            UIWindow.CurrentDrawingWindow?.ForceDraw();
            _currentValue = Mathf.Lerp(_startValue, _targetValue, _enlapsed / ANIMATION_DURATION);
            if (UIWindow.CurrentDrawingWindow != null)
            {
                _enlapsed += UIWindow.CurrentDrawingWindow.DeltaTime;
            }
            else
            {
                _enlapsed += ImGui.GetIO().DeltaTime;
            }
            if (_enlapsed > ANIMATION_DURATION)
            {
                _animating = false;
                _currentValue = _targetValue;
            }
        }
    }
}