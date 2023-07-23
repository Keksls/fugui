using System.Collections.Generic;
using UnityEngine;

namespace Fu
{
    public static class FuguiExtentions
    {
        //
        // Résumé :
        //     Returns true if the x and y components of point is a point inside this rectangle.
        //     If allowInverse is present and true, the width and height of the Rect are allowed
        //     to take negative values (ie, the min value is greater than the max), and the
        //     test will still work.
        //
        // Paramètres :
        //   point:
        //     Point to test.
        //
        // Retourne :
        //     True if the point lies within the specified rectangle.
        public static bool Contains(this Dictionary<string, Rect> dic, Vector2 position)
        {
            foreach (var key in dic)
            {
                if (key.Value.Contains(position))
                {
                    return true;
                }
            }
            return false;
        }

        //
        // Résumé :
        //     Returns true if the x and y components of point is a point inside this rectangle.
        //     If allowInverse is present and true, the width and height of the Rect are allowed
        //     to take negative values (ie, the min value is greater than the max), and the
        //     test will still work.
        //
        // Paramètres :
        //   point:
        //     Point to test.
        //
        // Retourne :
        //     True if the point lies within the specified rectangle.
        public static bool Contains(this Dictionary<string, Rect> dic, Vector2Int position)
        {
            foreach (var key in dic)
            {
                if (key.Value.Contains(position))
                {
                    return true;
                }
            }
            return false;
        }
    }
}