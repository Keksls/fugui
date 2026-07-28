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
            if (!CanCreateContext())
            {
                return null;
            }

            int contextID = _contextID;
            FuUnityContext context = CreateUnityContext(contextID, camera, scale, fontScale, onInitialize);
            if (context != null)
            {
                _contextID++;
            }

            return context;
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
            if (!CanCreateContext())
            {
                return null;
            }

            int contextID = _contextID;
            FuUnityContext context = CreateUnityContext(contextID, pixelRect, scale, fontScale, onInitialize);
            if (context != null)
            {
                _contextID++;
            }

            return context;
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

#if FU_EXTERNALIZATION
        /// <summary>
        /// Creates and registers a native external Fugui context.
        /// </summary>
        /// <param name="window">Window transferred into the external context.</param>
        /// <param name="scale">Initial UI scale.</param>
        /// <param name="fontScale">Initial font scale.</param>
        /// <returns>The registered context, or null when creation is not allowed.</returns>
        internal static FuExternalContext CreateExternalContext(FuWindow window, float scale, float fontScale)
        {
            if (!CanCreateContext())
            {
                return null;
            }

            int contextID = _contextID;
            FuExternalContext context = new FuExternalContext(contextID, scale, fontScale, null, window);
            Contexts.Add(contextID, context);
            _contextID++;
            return context;
        }
#endif

        /// <summary>
        /// Destroy a fugui context by it's ID
        /// </summary>
        /// <param name="contextID">ID of the fugui context</param>
        public static void DestroyContext(int contextID)
        {
            if (ContextExists(contextID))
            {
                FuContext context = GetContext(contextID);
                if (ReferenceEquals(context, DefaultContext))
                {
                    Debug.LogError("[Fugui] The default context belongs to the runtime session and cannot be destroyed independently. Dispose the Fugui session instead.");
                    return;
                }

                context.Stop();
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
        /// Destroys every context currently queued for deferred destruction.
        /// </summary>
        internal static void ProcessPendingContextDestructions()
        {
            while (ToDeleteContexts.Count > 0)
            {
                DestroyContextImmediately(ToDeleteContexts.Dequeue());
            }
        }

        /// <summary>
        /// Destroys every registered context immediately in reverse creation order.
        /// </summary>
        private static void DestroyAllContextsImmediately()
        {
            int[] contextIDs = Contexts.Keys
                .OrderByDescending(contextID => contextID)
                .ToArray();

            foreach (int contextID in contextIDs)
            {
                DestroyContextImmediately(contextID);
            }

            ToDeleteContexts.Clear();
            SetCurrentContext(null);
        }

        /// <summary>
        /// Destroys one context immediately and removes all registries that reference it.
        /// </summary>
        /// <param name="contextID">Identifier of the context to destroy.</param>
        private static void DestroyContextImmediately(int contextID)
        {
            if (!Contexts.TryGetValue(contextID, out FuContext context))
            {
                return;
            }

            // Unregister first so callbacks raised during native cleanup cannot enqueue the same context again.
            Contexts.Remove(contextID);
            RunShutdownStep(() => DisposeListClipper(contextID));
            bool isDefaultContext = ReferenceEquals(context, DefaultContext);
            if (isDefaultContext)
            {
                DefaultContext = null;
                DefaultContainer = null;
            }

#if FU_EXTERNALIZATION
            RunShutdownStep(() => RemoveExternalContextRegistrations(context));
#endif

            try
            {
                // A prepared ImGui frame must be ended before its native context is destroyed.
                context.Stop();
                SetCurrentContext(context);
                Fugui.World.ReleaseContextResources(context);
                if (context.RenderPrepared)
                {
                    context.EndRender();
                }
                context.Destroy();
            }
            catch (Exception exception)
            {
                Fire_OnUIException(exception);
                Debug.LogException(exception);
            }
            finally
            {
                FuContext fallbackContext = DefaultContext != null && ContextExists(DefaultContext.ID)
                    ? DefaultContext
                    : Contexts.Values.FirstOrDefault(existingContext => existingContext != null);
                SetCurrentContext(fallbackContext);
            }
        }

#if FU_EXTERNALIZATION
        /// <summary>
        /// Removes every external-window registry entry owned by a context.
        /// </summary>
        /// <param name="context">Context being destroyed.</param>
        private static void RemoveExternalContextRegistrations(FuContext context)
        {
            List<string> externalWindowIDs = ExternalWindows
                .Where(pair => pair.Value != null && ReferenceEquals(pair.Value.Context, context))
                .Select(pair => pair.Key)
                .ToList();

            foreach (string externalWindowID in externalWindowIDs)
            {
                ExternalWindows.Remove(externalWindowID);
            }

            RestoreExternalWindowUpdateLoop();
        }
#endif

        /// <summary>
        /// Returns whether the current lifecycle state accepts creation of a new context.
        /// </summary>
        /// <returns>True during initialization or while the runtime is initialized.</returns>
        private static bool CanCreateContext()
        {
            if (_lifecycleState == FuguiLifecycleState.Initializing ||
                _lifecycleState == FuguiLifecycleState.Initialized)
            {
                return true;
            }

            Debug.LogError("[Fugui] A context cannot be created while Fugui is inactive.");
            return false;
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
