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
    /// Fugui context rendering loop.
    /// </summary>
    public static partial class Fugui
    {
        /// <summary>
        /// Render each FuGui contexts
        /// </summary>
        public static void Render()
        {
            Fugui.HasHovered3DWindowThisFrame = false;

#if FU_EXTERNALIZATION
            SDL.SDL_GetGlobalMouseState(out int x, out int y);
            AbsoluteMonitorMousePosition = new Vector2Int(x, y);
#endif

            // clear context menu stack in case dev forgot to pop something OR exception raise between push and pop
            ClearContextMenuStack();
            // clean popup stack to prevent popup to stay on stack if they close unexpectedly
            CleanPopupStack();

            // render any other contexts BEFORE, because 3D containers need to handle input before default to handle HasHovered3DWindowThisFrame
            foreach (var contextPair in Contexts.ToList())
            {
                if (!Contexts.TryGetValue(contextPair.Key, out FuContext context) ||
                    contextPair.Key == 0 ||
                    !context.Started)
                {
                    continue;
                }

#if FU_EXTERNALIZATION
                FuExternalWindowContainer externalWindowContainer = null;
                if (context is FuExternalContext externalContext)
                {
                    externalWindowContainer = externalContext.Window?.Window?.Container as FuExternalWindowContainer;
                    if (externalWindowContainer != null)
                    {
                        externalWindowContainer.Update();
                    }
                }
#endif

                ResetWindowInputBlockForFrame();
                if (context.PrepareRender())
                {
                    bool publishDrawData = ShouldPublishContextDrawData(context);
                    if (publishDrawData)
                    {
                        MarkContextDrawDataPublished(context);
                    }
                    HasRenderWindowThisFrame = false;

                    context.Render(publishDrawData);
                    ExecuteAfterCurrentRenderContextCallbacks();
                    context.EndRender();
                    RestoreWindowInputsAfterFrame();
                    if (_targetScale != -1f)
                    {
                        context.SetScale(_targetScale, _targetFontScale);
                    }
                }
                else
                {
                    RestoreWindowInputsAfterFrame();
                }
            }

            if (MainContainerEnabled && DefaultContext != null)
            {
                // no one has render for now
                HasRenderWindowThisFrame = false;
                ResetWindowInputBlockForFrame();
                // prepare a new frame for default render
                DefaultContext.PrepareRender();
                bool publishDrawData = DefaultContext.RenderPrepared && ShouldPublishContextDrawData(DefaultContext);
                // execute before default renderer render actions
                if (DefaultContext.RenderPrepared)
                {
                    if (publishDrawData)
                    {
                        MarkContextDrawDataPublished(DefaultContext);
                    }
                    while (_beforeDefaultRenderStack.Count > 0)
                    {
                        _beforeDefaultRenderStack.Dequeue()?.Invoke();
                    }
                }

                // Render default context
                DefaultContext.Render(publishDrawData);
                ExecuteAfterCurrentRenderContextCallbacks();
                // execute after default renderer render actions
                if (DefaultContext.RenderPrepared)
                {
                    while (_afterDefaultRenderStack.Count > 0)
                    {
                        _afterDefaultRenderStack.Dequeue()?.Invoke();
                    }
                }

                DefaultContext.EndRender();
                RestoreWindowInputsAfterFrame();
            }
            else if (DefaultContext != null)
            {
                while (_beforeDefaultRenderStack.Count > 0)
                {
                    _beforeDefaultRenderStack.Dequeue();
                }

                while (_afterDefaultRenderStack.Count > 0)
                {
                    _afterDefaultRenderStack.Dequeue();
                }

                SetCurrentContext(DefaultContext);
            }

            if (_targetScale != -1f && DefaultContext != null)
            {
                DefaultContext.SetScale(_targetScale, _targetFontScale);
            }

            CleanFrozenUICache();

            // prevent rescaling each frames
            _targetScale = -1f;
        }
    }
}
