#if (UNITY_ANDROID || UNITY_IOS)// && !UNITY_EDITOR && !FUMOBILE
#define FUMOBILE
#endif
using System.Collections.Generic;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Represents the Fugui type.
    /// </summary>
    public partial class Fugui
    {
        #region State
        private static List<Vector2> mobileTouches = new List<Vector2>();
        #endregion

        /// <summary>
        /// Runs the begin mobile frame workflow.
        /// </summary>
        public static void BeginMobileFrame()
        {
#if FUMOBILE
            TouchScrollBeginFrame();
            handleMobileTouches();
#endif
        }

        /// <summary>
        /// Runs the end mobile frame workflow.
        /// </summary>
        public static void EndMobileFrame()
        {
#if FUMOBILE
            DrawMobileTouchFeedback();
#endif
        }

        /// <summary>
        /// Runs the handle mobile touches workflow.
        /// </summary>
        private static void handleMobileTouches()
        {
#if FUMOBILE
            mobileTouches.Clear();
#if ENABLE_INPUT_SYSTEM
            var touches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;
            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                mobileTouches.Add(new Vector2(touch.screenPosition.x, touch.screenPosition.y));
            }
#else
            for (int i = 0; i < UnityEngine.Input.touchCount; i++)
            {
                var touch = UnityEngine.Input.GetTouch(i);
                mobileTouches.Add(new Vector2(touch.position.x, touch.position.y));
            }
#endif
#endif
        }
    }
}