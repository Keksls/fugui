using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Represents the mouse button state data structure.
    /// </summary>
    public struct MouseButtonState
    {
        #region State
        private bool _button0;
        private bool _button1;
        private bool _button2;
        #endregion

        #region Properties
        public bool Button0 { get { return _button0; } }
        public bool Button1 { get { return _button1; } }
        public bool Button2 { get { return _button2; } }

        public bool this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return _button0;
                    case 1:
                        return _button1;
                    case 2:
                        return _button2;
                    default:
                        throw new System.IndexOutOfRangeException();
                }
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the Mouse Button State structure.
        /// </summary>
        /// <param name="button0">The button 0 state.</param>
        /// <param name="button1">The button 1 state.</param>
        /// <param name="button2">The button 2 state.</param>
        public MouseButtonState(bool button0, bool button1, bool button2)
        {
            _button0 = button0;
            _button1 = button1;
            _button2 = button2;
        }
        #endregion
    }

        /// <summary>
        /// Represents the Input State data structure.
        /// </summary>
        public struct InputState
        {
            #region State
            private string _raycasterID;
            private bool _hovered;
            private float _mouseWheel;
            private MouseButtonState _mouseDown;
            private Vector2 _mousePosition;

            public string RaycasterID { get { return _raycasterID; } }
            public float MouseWheel { get { return _mouseWheel; } }
            public bool Hovered { get { return _hovered; } }
            public Vector2 MousePosition { get { return _mousePosition; } }
            public MouseButtonState MouseButtons { get { return _mouseDown; } }
            public bool[] MouseDown { get { return new bool[] { _mouseDown.Button0, _mouseDown.Button1, _mouseDown.Button2 }; } }
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
                _mouseDown = new MouseButtonState(mouseDown0, mouseDown1, mouseDown2);
                _mousePosition = mousePosition;
            }
            #endregion
        }
}
