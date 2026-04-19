#if (UNITY_ANDROID || UNITY_IOS)// && !UNITY_EDITOR && !FUMOBILE
#define FUMOBILE
#endif
using System.Collections.Generic;
using UnityEngine;

namespace Fu
{
    public partial class Fugui
    {
        private static List<Vector2> mobileTouches = new List<Vector2>();

        public static void BeginMobileFrame()
        {
#if FUMOBILE
            TouchScrollBeginFrame();
            handleMobileTouches();
#endif
        }

        public static void EndMobileFrame()
        {
#if FUMOBILE
            DrawMobileTouchFeedback();
#endif
        }

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
