// define it to debug whatever Color or Styles are pushed (avoid stack leak metrics)
// it's ressourcefull, si comment it when debug is done. Ensure it's commented before build.
//#define FUDEBUG
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR && !FUMOBILE
#define FUMOBILE
#endif
using Fu.Framework;
using ImGuiNET;
#if FU_EXTERNALIZATION
using SDL2;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;

namespace Fu
{
    /// <summary>
    /// Fugui deferred callback queues.
    /// </summary>
    public static partial class Fugui
    {
        /// <summary>
        /// Execute an action into main thread, will be raised on next Fugui.Update call
        /// </summary>
        /// <param name="callback">callback you want to raise into unity's main thread</param>
        public static void ExecuteInMainThread(Action callback)
        {
            _executeInMainThreadActionsStack.Enqueue(callback);
        }

        /// <summary>
        /// Execute a callback after a quick async waiting routine
        /// </summary>
        /// <param name="callback">callback to execute</param>
        /// <param name="sleep">time to wait before execute</param>
        public static void ExecuteCallbackAfterAsyncSleep(Action callback, float sleep)
        {
            Controller.StartCoroutine(executeCallbackAfterAsyncSleep_Routine(callback, sleep));
        }

        /// <summary>
        /// Execute a callback after a quick async waiting routine
        /// </summary>
        /// <param name="callback">callback to execute</param>
        /// <param name="sleep">time to wait before execute</param>
        /// <returns>awaiter</returns>
        private static IEnumerator executeCallbackAfterAsyncSleep_Routine(Action callback, float sleep)
        {
            yield return new WaitForSeconds(sleep); // <= remove that shit
            callback?.Invoke();
        }

        /// <summary>
        /// Execute a callback after each window of default context has render
        /// </summary>
        /// <param name="callback"> callback to execute</param>
        public static void ExecuteAfterRenderWindows(Action callback)
        {
            if (callback != null)
            {
                _afterDefaultRenderStack.Enqueue(callback);
            }
        }

        /// <summary>
        /// Execute a callback after the currently rendered Fugui context has finished drawing its windows.
        /// </summary>
        /// <param name="callback">Callback to execute.</param>
        private static void ExecuteAfterCurrentRenderContext(Action callback)
        {
            if (callback != null)
            {
                _afterCurrentRenderContextStack.Enqueue(callback);
            }
        }

        /// <summary>
        /// Executes callbacks waiting for the end of the currently rendered Fugui context.
        /// </summary>
        private static void ExecuteAfterCurrentRenderContextCallbacks()
        {
            while (_afterCurrentRenderContextStack.Count > 0)
            {
                _afterCurrentRenderContextStack.Dequeue()?.Invoke();
            }
        }

        /// <summary>
        /// Execute a callback after each window of default context has render
        /// </summary>
        /// <param name="callback"></param>
        public static void ExecuteBeforeRenderWindows(Action callback)
        {
            if (callback != null)
            {
                _beforeDefaultRenderStack.Enqueue(callback);
            }
        }
    }
}
