using Fu.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FuGradient
{
    #region Variables
    ///<summary>
    ///The blend mode for the gradient.
    ///</summary>
    public FuGradientBlendMode BlendMode { get; private set; }
    // The maximum number of colors that can be in a Unity gradient.
    private const int MAX_COLORS_IN_UNITY_GRADIENT = 8;
    // The list of color keys for the gradient.
    [SerializeField]
    private List<FuGradientColorKey> _keys;
    // The horizontal texture for the gradient.
    private Texture2D _horizontalTexture;
    // The vertical texture for the gradient.
    private Texture2D _verticalTexture;
    #endregion

    #region Constructors
    ///<summary>
    ///Creates a new instance of the FuGradient class with default color keys.
    ///</summary>
    ///<param name="blendMode">The blend mode for the gradient.</param>
    public FuGradient(FuGradientBlendMode blendMode = FuGradientBlendMode.Continious)
    {
        _keys = new List<FuGradientColorKey>();

        AddColorKey(0f, new Color(0f, 0f, 0f));
        AddColorKey(1f, new Color(1f, 1f, 1f));

        BlendMode = blendMode;
    }

    ///<summary>
    ///Creates a new instance of the FuGradient class with the specified start and end colors.
    ///</summary>
    ///<param name="start">The start color of the gradient.</param>
    ///<param name="end">The end color of the gradient.</param>
    ///<param name="blendMode">The blend mode for the gradient.</param>
    public FuGradient(Color start, Color end, FuGradientBlendMode blendMode = FuGradientBlendMode.Continious)
    {
        _keys = new List<FuGradientColorKey>();

        AddColorKey(0f, start);
        AddColorKey(1f, end);

        BlendMode = blendMode;
    }

    ///<summary>
    ///Creates a new instance of the FuGradient class with the specified color keys.
    ///</summary>
    ///<param name="keys">The color keys for the gradient.</param>
    ///<param name="blendMode">The blend mode for the gradient.</param>
    ///<param name="relativeMin">The value represented when time = 0. If bigger or equal to RelativeMax, gradient will not take this in account</param>
    ///<param name="relativeMax">The value represented when time = 1. If smaller or equal to RelativeMin, gradient will not take this in account</param>
    public FuGradient(FuGradientColorKey[] keys, FuGradientBlendMode blendMode = FuGradientBlendMode.Continious)
    {
        _keys = new List<FuGradientColorKey>();

        if (keys.Length >= 2)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                AddColorKey(keys[i].Time, keys[i].Color);
            }
        }
        else
        {
            AddColorKey(0f, new Color(0f, 0f, 0f));
            AddColorKey(1f, new Color(1f, 1f, 1f));
        }

        BlendMode = blendMode;
    }
    #endregion

    #region Public utils
    ///<summary>
    ///Evaluates the gradient at the specified time and returns the resulting color.
    ///</summary>
    ///<param name="time">The time at which to evaluate the gradient.</param>
    ///<returns>The color at the specified time.</returns>
    public Color Evaluate(float time)
    {
        // Set the keyBefore variable to the first color key in the list of keys.
        FuGradientColorKey keyBefore = _keys[0];
        // Set the keyAfter variable to the last color key in the list of keys.
        FuGradientColorKey keyAfter = _keys[_keys.Count - 1];

        // Loop through all the color keys in the list of keys.
        for (int i = 0; i < _keys.Count; i++)
        {
            // If the current color key's time is less than the specified time,
            // set the keyBefore variable to the current color key.
            if (_keys[i].Time < time)
            {
                keyBefore = _keys[i];
            }

            // If the current color key's time is greater than the specified time,
            // set the keyAfter variable to the current color key and break out of the loop.
            if (_keys[i].Time > time)
            {
                keyAfter = _keys[i];
                break;
            }
        }

        // If the blend mode of the gradient is continuous,
        // calculate the blend time between the keyBefore and keyAfter color keys.
        if (BlendMode == FuGradientBlendMode.Continious)
        {
            float blendTime = Mathf.InverseLerp(keyBefore.Time, keyAfter.Time, time);

            // Return the color that is a blend of the keyBefore and keyAfter colors,
            // based on the calculated blend time.
            return Color.Lerp(keyBefore.Color, keyAfter.Color, blendTime);
        }

        // If the blend mode of the gradient is not continuous,
        // return the color of the keyAfter color key.
        return keyAfter.Color;
    }

    ///<summary>
    ///Converts the gradient to a Unity gradient object.
    ///</summary>
    ///<returns>The Unity gradient object.</returns>
    public Gradient ToUnityGradient()
    {
        // Create a new Gradient object
        Gradient tempGradient = new Gradient();

        // Declare arrays to store color and alpha keys
        GradientColorKey[] colorsKeys;
        GradientAlphaKey[] alphaKeys;

        // Check if the number of color keys in the FuGradient is less than or equal to the maximum allowed in a Unity Gradient
        if (_keys.Count <= MAX_COLORS_IN_UNITY_GRADIENT)
        {
            // If yes, initialize arrays with the size of the color keys list
            colorsKeys = new GradientColorKey[_keys.Count];
            alphaKeys = new GradientAlphaKey[_keys.Count];
            // Set the mode of the Gradient object based on the blend mode of the FuGradient
            tempGradient.mode = BlendMode == FuGradientBlendMode.Continious ? GradientMode.Blend : GradientMode.Fixed;

            // Loop through the color keys in the FuGradient and add them to the arrays
            for (int i = 0; i < _keys.Count; i++)
            {
                colorsKeys[i] = new GradientColorKey(_keys[i].Color, _keys[i].Time);
                alphaKeys[i] = new GradientAlphaKey(_keys[i].Color.a, _keys[i].Time);
            }

            // Set the color and alpha keys of the Gradient object
            tempGradient.SetKeys(colorsKeys, alphaKeys);

        }
        else
        {
            // If the number of color keys in the FuGradient is greater than the maximum allowed in a Unity Gradient
            // Initialize arrays with the size of the maximum allowed keys
            colorsKeys = new GradientColorKey[MAX_COLORS_IN_UNITY_GRADIENT];
            alphaKeys = new GradientAlphaKey[MAX_COLORS_IN_UNITY_GRADIENT];

            // Set the mode of the Gradient object based on the blend mode of the FuGradient
            tempGradient.mode = BlendMode == FuGradientBlendMode.Continious ? GradientMode.Blend : GradientMode.Fixed;

            // Calculate the offset between each color key
            float offsetKey = 1f / MAX_COLORS_IN_UNITY_GRADIENT;

            // Loop through the maximum allowed number of color keys in the Gradient object
            for (int i = 0; i < MAX_COLORS_IN_UNITY_GRADIENT; i++)
            {
                // Calculate the time offset of the current color key
                float tempOffset = i * offsetKey;

                // Evaluate the color of the current color key from the FuGradient
                Color tempColor = Evaluate(tempOffset);

                // Add the current color key to the arrays
                colorsKeys[i] = new GradientColorKey(tempColor, tempOffset);
                alphaKeys[i] = new GradientAlphaKey(tempColor.a, tempOffset);
            }

            // Set the color and alpha keys of the Gradient object
            tempGradient.SetKeys(colorsKeys, alphaKeys);
        }

        // Return the Gradient object
        return tempGradient;
    }

    ///<summary>
    ///Adds a color key to the gradient.
    ///</summary>
    ///<param name="time">The time of the color key.</param>
    ///<param name="color">The color of the color key.</param>
    public int AddColorKey(float time, Color color)
    {
        // Create a new color key with the given time and color
        FuGradientColorKey tempKey = new FuGradientColorKey(time, color);

        // Assume that the new key hasn't been added to the list yet
        bool added = false;

        int keyIndex = 0;
        // Loop through the existing color keys to find the position to add the new key
        for (int i = 0; i < _keys.Count; i++)
        {
            // If the new key comes before the current key in the loop
            if (tempKey.Time < _keys[i].Time)
            {
                // Insert the new key before the current key
                _keys.Insert(i, tempKey);
                keyIndex = i;
                added = true;
                break;
            }
        }

        // If the new key wasn't added yet, add it to the end of the list
        if (!added)
        {
            _keys.Add(tempKey);
        }

        // Regenerate the gradient textures with the new color key added
        UpdateGradientTextures();

        return keyIndex;
    }

    ///<summary>
    ///Removes a color key from the gradient.
    ///</summary>
    ///<param name="index">The index of the color key to remove.</param>
    public void RemoveColorKey(int index)
    {
        // Removes a color key from the gradient if the index is valid
        // and there are at least two color keys in the gradient
        if (_keys.Count >= 2 && _keys.Count > index)
        {
            _keys.RemoveAt(index);
            // generates the gradient textures after the key is removed.
            UpdateGradientTextures();
        }
    }

    ///<summary>
    ///Sets the time of a color key in the gradient.
    ///</summary>
    ///<param name="index">The index of the color key to modify.</param>
    ///<param name="time">The new time of the color key.</param>
    public void SetKeyTime(int index, float time)
    {
        // If the index is within the bounds of the keys list:
        if (index >= 0 && index < _keys.Count)
        {
            // Store the key to be modified and its color.
            FuGradientColorKey tempKey = _keys[index];
            Color tempColor = tempKey.Color;

            // Remove the key from its current position and add it back with the new time and the same color.
            RemoveColorKey(index);
            AddColorKey(time, tempColor);

            // Regenerate the gradient textures.
            UpdateGradientTextures();
        }
    }

    ///<summary>
    ///Sets the color of a color key in the gradient.
    ///</summary>
    ///<param name="index">The index of the color key to modify.</param>
    ///<param name="color">The new color of the color key.</param>
    public void SetKeyColor(int index, Color color)
    {
        // Check if index is valid (within the range of available keys)
        if (index >= 0 && index < _keys.Count)
        {
            // Replace the existing key with a new key that has the same time and the new color
            _keys[index] = new FuGradientColorKey(_keys[index].Time, color);
            // Generate gradient textures with the updated key values
            UpdateGradientTextures();

        }
    }

    ///<summary>
    ///Gets a color key from the gradient.
    ///</summary>
    ///<param name="index">The index of the color key to retrieve.</param>
    ///<param name="colorKey">The color key at the specified index, if it exists.</param>
    ///<returns>True if the color key exists, false otherwise.</returns>
    public bool GetKey(int index, out FuGradientColorKey colorKey)
    {
        // Check if the index is within the bounds of the keys list
        if (index >= 0 && index < _keys.Count)
        {
            // Get the color key at the specified index and assign it to the output parameter
            colorKey = _keys[index];
            // Return true to indicate that the color key was found
            return true;
        }
        else
        {
            // Assign the default value to the output parameter and return false to indicate that the color key was not found
            colorKey = default;
            return false;
        }
    }

    ///<summary>
    ///Gets the gradient texture for the gradient.
    ///</summary>
    ///<param name="vertical">Whether to retrieve the vertical or horizontal texture.</param>
    ///<returns>The gradient texture.</returns>
    public Texture2D GetGradientTexture(bool vertical = false)
    {
        // Return the corresponding texture based on the provided orientation
        return vertical ? _verticalTexture : _horizontalTexture;
    }

    ///<summary>
    ///Gets the number of color keys in the gradient.
    ///</summary>
    ///<returns>The number of color keys in the gradient.</returns>
    public int GetKeysCount()
    {
        return _keys.Count;
    }

    /// <summary>
    /// Set the blend mode of the gradient
    /// </summary>
    /// <param name="blendMode">blend mode to set</param>
    public void SetBlendMode(FuGradientBlendMode blendMode)
    {
        BlendMode = blendMode;
        UpdateGradientTextures();
    }

    ///<summary>
    ///Generates the gradient textures for the gradient.
    ///</summary>
    public void UpdateGradientTextures()
    {
        if (_horizontalTexture == null || _verticalTexture == null)
        {
            // Create two new texture objects with the specified sizes
            _horizontalTexture = new Texture2D(512, 1);
            _verticalTexture = new Texture2D(1, 512);

            // Set the wrap mode of the texture objects to clamp
            _horizontalTexture.wrapMode = TextureWrapMode.Clamp;
            _verticalTexture.wrapMode = TextureWrapMode.Clamp;

        }
        // Create an array of colors with the specified size
        Color[] colors = new Color[512];

        // Loop through each pixel of the texture and set the corresponding color
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Evaluate((float)i / (colors.Length - 1));
        }

        // Set the pixels of the texture objects to the array of colors
        _horizontalTexture.SetPixels(colors);
        _verticalTexture.SetPixels(colors);

        // Apply the texture changes
        _horizontalTexture.Apply();
        _verticalTexture.Apply();
    }
    #endregion

    #region Private Utils
    #endregion
}