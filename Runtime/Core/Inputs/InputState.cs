using UnityEngine;

namespace Fu
{
        /// <summary>
        /// Represents the Input State data structure.
        /// </summary>
        public struct InputState
        {
            #region State
            private string _raycasterID;
            private bool _hovered;
            private float _mouseWheel;
            private bool[] _mouseDown;
            private Vector2 _mousePosition;

            public string RaycasterID { get { return _raycasterID; } }
            public float MouseWheel { get { return _mouseWheel; } }
            public bool Hovered { get { return _hovered; } }
            public Vector2 MousePosition { get { return _mousePosition; } }
            public bool[] MouseDown { get { return _mouseDown; } }
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Input State class.
            /// </summary>
            /// <param name="raycasterID">The raycaster ID value.</param>
            /// <param name="hovered">The hovered value.</param>
            /// <param name="mouseDown0">The mouse Down0 value.</param>
            /// <param name="mouseDown1">The mouse Down1 value.</param>
            /// <param name="mouseDown2">The mouse Down2 value.</param>
            /// <param name="mouseWheel">The mouse Wheel value.</param>
            /// <param name="mousePosition">The mouse Position value.</param>
            public InputState(string raycasterID, bool hovered, bool mouseDown0, bool mouseDown1, bool mouseDown2, float mouseWheel, Vector2 mousePosition)
            {
                _mouseWheel = mouseWheel;
                _raycasterID = raycasterID;
                _hovered = hovered;
                _mouseDown = new bool[] { mouseDown0, mouseDown1, mouseDown2 };
                _mousePosition = mousePosition;
            }
            #endregion
        }
}