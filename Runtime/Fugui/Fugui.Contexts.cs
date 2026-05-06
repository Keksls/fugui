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
    /// Fugui context creation and selection.
    /// </summary>
    public static partial class Fugui
    {
        /// <summary>
        /// Create a new Fugui context to render into unity
        /// </summary>
        /// <param name="camera">Camera that will render the context</param>
        /// <param name="onInitialize">invoked on context initialization</param>
        /// <returns>the context created</returns>
        public static unsafe FuUnityContext CreateUnityContext(Camera camera, float scale = 1f, float fontScale = 1f, Action onInitialize = null)
        {
            return CreateUnityContext(_contextID++, camera, scale, fontScale, onInitialize);
        }

        /// <summary>
        /// Create a new Fugui context to render into unity
        /// </summary>
        /// <param name="index">index of the context</param>
        /// <param name="camera">Camera that will render the context</param>
        /// <param name="onInitialize">invoked on context initialization</param>
        /// <returns>the context created</returns>
        private static FuUnityContext CreateUnityContext(int index, Camera camera, float scale = 1f, float fontScale = 1f, Action onInitialize = null)
        {
            if (Contexts.ContainsKey(index))
                return null;

            // create and add context
            FuUnityContext context = new FuUnityContext(index, scale, fontScale, onInitialize, camera);
            Contexts.Add(index, context);

            return context;
        }

        /// <summary>
        /// Create a new Fugui context to render into unity
        /// </summary>
        /// <param name="pixelRect"> Rect in pixel to render the context into, relative to the main container camera</param>
        /// <param name="scale"> initial scale of the context, keep 1f to use global scale from settings</param>
        /// <param name="fontScale"> initial font scale of the context, keep 1f to use global font scale from settings</param>
        /// <param name="onInitialize"> invoked on context initialization</param>
        /// <returns> the context created</returns>
        public static unsafe FuUnityContext CreateUnityContext(Rect pixelRect, float scale = 1f, float fontScale = 1f, Action onInitialize = null)
        {
            return CreateUnityContext(_contextID++, pixelRect, scale, fontScale, onInitialize);
        }

        /// <summary>
        /// Create a new Fugui context to render into unity
        /// </summary>
        /// <param name="index"> index of the context</param>
        /// <param name="pixelRect"> Rect in pixel to render the context into, relative to the main container camera</param>
        /// <param name="scale"> initial scale of the context, keep 1f to use global scale from settings</param>
        /// <param name="fontScale"> initial font scale of the context, keep 1f to use global font scale from settings</param>
        /// <param name="onInitialize"> invoked on context initialization</param>
        /// <returns> the context created</returns>
        private static FuUnityContext CreateUnityContext(int index, Rect pixelRect, float scale = 1f, float fontScale = 1f, Action onInitialize = null)
        {
            if (Contexts.ContainsKey(index))
                return null;
            // create and add context
            FuUnityContext context = new FuUnityContext(index, scale, fontScale, onInitialize, pixelRect);
            Contexts.Add(index, context);
            return context;
        }

        /// <summary>
        /// Destroy a fugui context by it's ID
        /// </summary>
        /// <param name="contextID">ID of the fugui context</param>
        public static void DestroyContext(int contextID)
        {
            if (ContextExists(contextID))
            {
                GetContext(contextID).Stop();
                if (!ToDeleteContexts.Contains(contextID))
                {
                    ToDeleteContexts.Enqueue(contextID);
                }
            }
        }

        /// <summary>
        /// Destroy a fugui context by it's context instance
        /// </summary>
        /// <param name="context">the fugui context to destroy</param>
        public static void DestroyContext(FuContext context)
        {
            if (context == null)
            {
                return;
            }

            DestroyContext(context.ID);
        }

        /// <summary>
        /// Get a fugui context by it's ID
        /// </summary>
        /// <param name="contextID">ID of the context to get</param>
        /// <returns>null if context's ID does not exists</returns>
        public static FuContext GetContext(int contextID)
        {
            if (Contexts.ContainsKey(contextID))
            {
                return Contexts[contextID];
            }
            return null;
        }

        /// <summary>
        /// Whatever a context exists
        /// </summary>
        /// <param name="contextID">ID of the context to check</param>
        /// <returns>true if exists</returns>
        public static bool ContextExists(int contextID)
        {
            return Contexts.ContainsKey(contextID);
        }

        /// <summary>
        /// set the current fugui context by ID
        /// </summary>
        /// <param name="contextID">ID of the fugui context</param>
        public static void SetCurrentContext(int contextID)
        {
            if (Contexts.ContainsKey(contextID))
            {
                SetCurrentContext(Contexts[contextID]);
            }
        }

        /// <summary>
        /// set the current fugui context
        /// </summary>
        /// <param name="context">instance of the fugui context</param>
        public static void SetCurrentContext(FuContext context)
        {
            if (context != null)
            {
                context.SetAsCurrent();
                CurrentContext = context;
            }
            else
            {
                CurrentContext = null;
                ImGui.SetCurrentContext(IntPtr.Zero);
            }
        }
    }
}
