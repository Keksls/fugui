using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework
{
    public partial class FuGrid
    {
        /// <summary>
        /// Draw a gradient picker
        /// </summary>
        /// <param name="text">text / ID of the gradient</param>
        /// <param name="gradient">gradient to edit</param>
        /// <param name="addKeyOnGradientClick">if enabled, allow the user to add a key on gradient click</param>
        /// <param name="allowAlpha">Whatever the gradient allow transparency on color keys</param>
        /// <param name="relativeMin">The value represented when time = 0. If bigger or equal to RelativeMax, gradient will not take this in account</param>
        /// <param name="relativeMax">The value represented when time = 1. If smaller or equal to RelativeMin, gradient will not take this in account</param>
        /// <param name="defaultGradientValues">the values to set for reseting this gradient</param>
        /// <returns>whatever the gradient has been edited this frame</returns>
        public override bool Gradient(string text, ref FuGradient gradient, bool addKeyOnGradientClick = true, bool allowAlpha = true, float relativeMin = 0, float relativeMax = 0, FuGradientColorKey[] defaultGradientValues = null)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(text, FuTextStyle.Default);
            return base.Gradient(text, ref gradient, addKeyOnGradientClick, allowAlpha, relativeMin, relativeMax, defaultGradientValues);
        }
    }
}