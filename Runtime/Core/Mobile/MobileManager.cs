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
        private const int MobileTouchRetainedCapacity = 16;
        private static readonly List<Vector2> mobileTouches = new List<Vector2>(MobileTouchRetainedCapacity);
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
        /// Clears mobile input and touch-scroll caches owned by the current Fugui session.
        /// </summary>
        internal static void ResetMobileState()
        {
            // Touch samples and child ownership cannot cross runtime sessions.
            mobileTouches.Clear();
            if (mobileTouches.Capacity > MobileTouchRetainedCapacity)
            {
                mobileTouches.Capacity = MobileTouchRetainedCapacity;
            }
            ResetTouchState();
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
            // A malformed or synthetic touch spike must not retain a large array indefinitely.
            if (mobileTouches.Count <= MobileTouchRetainedCapacity &&
                mobileTouches.Capacity > MobileTouchRetainedCapacity * 4)
            {
                mobileTouches.Capacity = MobileTouchRetainedCapacity;
            }
#endif
        }
    }
}
