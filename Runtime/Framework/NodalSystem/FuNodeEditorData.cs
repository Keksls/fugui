using UnityEngine;

namespace Fu.Framework
{
        /// <summary>
        /// Struct holding precalculated geometry for a node to optimize drawing and interaction.
        /// </summary>
        public class FuNodeEditorData
        {
            #region State
            public bool MustRecalculate = true;

            public Vector2 rectMin, rectMax;
            public float headerHeight;

            public float portLineHeight;
            public float portsStartY;
            public float portsEndY;

            public bool hasIn;
            public bool hasOut;

            public float leftMinX;
            public float leftMaxX;
            public float rightMinX;
            public float rightMaxX;
            #endregion
        }
}