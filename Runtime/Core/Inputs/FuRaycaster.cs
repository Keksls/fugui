using System;
using UnityEngine;

namespace Fu
{
        /// <summary>
        /// Represents the Fu Raycaster type.
        /// </summary>
        public class FuRaycaster
        {
            #region State
            public Func<bool> IsActif { get; private set; }
            public Func<bool> MouseButton0 { get; private set; }
            public Func<bool> MouseButton1 { get; private set; }
            public Func<bool> MouseButton2 { get; private set; }
            public Func<float> MouseWheel { get; private set; }
            public Func<Ray> GetRay { get; private set; }
            public string ID { get; private set; }
            public bool RaycastThisFrame { get; private set; }
            public RaycastHit Hit { get; private set; }
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Fu Raycaster class.
            /// </summary>
            /// <param name="name">The name value.</param>
            /// <param name="rayGetter">The ray Getter value.</param>
            /// <param name="mouseButton0">The mouse Button0 value.</param>
            /// <param name="mouseButton1">The mouse Button1 value.</param>
            /// <param name="mouseButton2">The mouse Button2 value.</param>
            /// <param name="mouseWheel">The mouse Wheel value.</param>
            /// <param name="actifGetter">The actif Getter value.</param>
            public FuRaycaster(string name, Func<Ray> rayGetter, Func<bool> mouseButton0, Func<bool> mouseButton1, Func<bool> mouseButton2, Func<float> mouseWheel, Func<bool> actifGetter)
            {
                ID = name;
                IsActif = actifGetter;
                MouseButton0 = mouseButton0;
                MouseButton1 = mouseButton1;
                MouseButton2 = mouseButton2;
                MouseWheel = mouseWheel;
                GetRay = rayGetter;
            }
            #endregion

            #region Methods
            /// <summary>
            /// Runs the raycast workflow.
            /// </summary>
            internal void Raycast()
            {
                RaycastThisFrame = false;
                if (IsActif())
                {
                    if (Physics.Raycast(GetRay(), out RaycastHit hit, Fugui.Settings.UIRaycastDistance, Fugui.Settings.UILayer.value))
                    {
                        Hit = hit;
                        RaycastThisFrame = true;
                    }
                }
            }
            #endregion
        }
}