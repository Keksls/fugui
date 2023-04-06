using System;
using UnityEngine;

[Serializable]
public struct FuGradientColorKey
{
    #region Variables
    public float Time;
    public Color Color;
    #endregion
    
    public FuGradientColorKey(float time, Color color)
    {
        Time = time;
        Color = color;
    }
}