using Fu.Core;
using Fu.Framework;
using ImGuiNET;
using UnityEngine;

public class FuElementAnimationData
{
    ///<summary>
    /// Property that returns the current value of the animation
    ///</summary>
    public float CurrentValue { get { return _currentValue; } }
    private float _currentValue;
    private bool _animating;
    private bool _lastFrameValue;
    private float _targetValue;
    private float _startValue;
    private float _enlapsed;

    ///<summary>
    /// Constructor that takes a boolean "defaultValue" and sets the initial value of _lastFrameValue
    ///</summary>
    public FuElementAnimationData(bool defaultValue)
    {
        _lastFrameValue = defaultValue;
    }

    ///<summary>
    /// Private method that sets the initial values for the animation
    ///</summary>
    private void animate(bool value)
    {
        _animating = true;
        _currentValue = value ? 0f : 1f;
        _targetValue = value ? 1f : 0f;
        _startValue = CurrentValue;
        _enlapsed = 0f;
    }

    ///<summary>
    /// Public method that updates the animation based on the current state of the element and a boolean "enableAnimation" flag
    ///</summary>
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
        // This line forces the current window to redraw
        FuWindow.CurrentDrawingWindow?.ForceDraw();
        // This line updates the current value of the animation based on the time elapsed
        _currentValue = Mathf.Lerp(_startValue, _targetValue, _enlapsed / Fugui.Settings.ElementsAnimationDuration);
        if (FuWindow.CurrentDrawingWindow != null)
        {
            // This line updates the elapsed time based on the time since the last frame
            _enlapsed += FuWindow.CurrentDrawingWindow.DeltaTime;
        }
        else
        {
            // This line updates the elapsed time based on the time since the last frame
            _enlapsed += ImGui.GetIO().DeltaTime;
        }
        // This line checks if the animation has reached its target value
        if (_enlapsed > Fugui.Settings.ElementsAnimationDuration)
        {
            _animating = false;
            _currentValue = _targetValue;
        }
    }
}