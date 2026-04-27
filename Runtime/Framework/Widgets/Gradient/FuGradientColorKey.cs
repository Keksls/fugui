using System;
using UnityEngine;

/// <summary>
/// Represents the Fu Gradient Color Key data structure.
/// </summary>
[Serializable]
public struct FuGradientColorKey
{
    #region State
    public float Time;
    public Color Color;
    #endregion

    #region Constructors
    /// <summary>
    /// Initializes a new instance of the Fu Gradient Color Key class.
    /// </summary>
    /// <param name="time">The time value.</param>
    /// <param name="color">The color value.</param>
    public FuGradientColorKey(float time, Color color)
    {
        Time = time;
        Color = color;
    }
    #endregion
}