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
    /// Fugui 3D window management.
    /// </summary>
    public static partial class Fugui
    {
        /// <summary>
        /// Adds an UI window to be in 3D context.
        /// </summary>
        /// <param name="uiWindow">The UI window to be display in 3D.</param>
        public static Fu3DWindowContainer Add3DWindow(FuWindow uiWindow, Fu3DWindowSettings settings, Vector3? position = null, Quaternion? rotation = null)
        {
            if (uiWindow == null)
            {
                Debug.Log("You are trying to create a 3D context to draw a null window.");
                return null;
            }

            if (_3DWindows.TryGetValue(uiWindow.ID, out Fu3DWindowContainer existingContainer))
            {
                existingContainer.Set3DWindowSettings(settings);
                return existingContainer;
            }

            Fu3DWindowContainer container = new Fu3DWindowContainer(uiWindow, settings, position, rotation);
            _3DWindows.Add(uiWindow.ID, container);
            ApplyMainContainerCameraState();
            return container;
        }

        /// <summary>
        /// Prewarms heavy resources used by 3D windows over multiple frames.
        /// </summary>
        /// <param name="fontScales">Font scales to prewarm. Null uses baked atlas scales from settings when available.</param>
        /// <param name="renderTextureSizes">Render texture sizes to prewarm.</param>
        /// <returns>Coroutine enumerator.</returns>
        public static IEnumerator Prewarm3DWindowResources(IEnumerable<float> fontScales = null, IEnumerable<Vector2Int> renderTextureSizes = null)
        {
            FontConfig fontConfig = Settings?.FontConfig;
            if (fontScales == null && fontConfig != null && fontConfig.BakedFontAtlasScales != null)
            {
                fontScales = fontConfig.BakedFontAtlasScales;
            }

            if (fontConfig != null && fontScales != null)
            {
                foreach (float fontScale in fontScales)
                {
                    FuSharedFontAtlasCache.Prewarm(fontConfig, fontScale, Application.streamingAssetsPath);
                    yield return null;
                }
            }

            if (renderTextureSizes != null)
            {
                foreach (Vector2Int renderTextureSize in renderTextureSizes)
                {
                    Fu3DWindowContainer.PrewarmRenderTexture(renderTextureSize);
                    yield return null;
                }
            }
        }

        /// <summary>
        /// Adds a 3D window after prewarming the matching font atlas and render texture over multiple frames.
        /// </summary>
        /// <param name="uiWindow">The UI window to be display in 3D.</param>
        /// <param name="settings">3D window settings.</param>
        /// <param name="onCreated">Callback invoked with the created container.</param>
        /// <param name="position">World 3D position of this container.</param>
        /// <param name="rotation">World 3D rotation of this container.</param>
        /// <returns>Coroutine enumerator.</returns>
        public static IEnumerator Add3DWindowAsync(FuWindow uiWindow, Fu3DWindowSettings settings, Action<Fu3DWindowContainer> onCreated, Vector3? position = null, Quaternion? rotation = null)
        {
            settings.Sanitize();
            yield return Prewarm3DWindowResources(new[] { settings.FontScale }, new[] { settings.Resolution });
            yield return null;

            onCreated?.Invoke(Add3DWindow(uiWindow, settings, position, rotation));
        }

        /// <summary>
        /// Adds a UI window to a 3D panel with an explicit panel size and fixed render resolution.
        /// </summary>
        /// <param name="uiWindow">The UI window to display in 3D.</param>
        /// <param name="panelSize">The world/local size of the 3D panel.</param>
        /// <param name="renderResolution">The render texture and ImGui context resolution.</param>
        /// <param name="position">World 3D position of this container.</param>
        /// <param name="rotation">World 3D rotation of this container.</param>
        /// <param name="scaleConfig">Optional container scaler configuration.</param>
        /// <param name="contextScale">Base context scale.</param>
        /// <param name="fontScale">Base font scale.</param>
        /// <param name="matchPanelAspect">When true, resize changes keep the base render area but adapt the render ratio to the panel ratio.</param>
        /// <param name="panelDepth">Depth of the generated panel extrusion.</param>
        /// <param name="panelCurve">Horizontal panel curve angle in degrees.</param>
        /// <param name="panelRounding">Panel corner radius in world units.</param>
        /// <param name="createExtrudedPanelMesh">Whether to create the optional extruded backing mesh.</param>
        public static Fu3DWindowContainer Add3DWindow(FuWindow uiWindow, Vector2 panelSize, Vector2Int renderResolution, Vector3? position = null, Quaternion? rotation = null, FuContainerScaleConfig? scaleConfig = null, float contextScale = 1f, float fontScale = 1f, bool matchPanelAspect = true, float panelDepth = 0.01f, float panelCurve = 0f, float panelRounding = Fu3DWindowSettings.DefaultPanelRounding, bool createExtrudedPanelMesh = true)
        {
            Fu3DWindowSettings settings = matchPanelAspect
                ? Fu3DWindowSettings.FixedResolutionMatchingPanelAspect(
                    panelSize,
                    renderResolution,
                    panelSize,
                    contextScale,
                    fontScale,
                    panelDepth: panelDepth,
                    panelCurve: panelCurve,
                    panelRounding: panelRounding,
                    createExtrudedPanelMesh: createExtrudedPanelMesh)
                : Fu3DWindowSettings.FixedResolution(
                    panelSize,
                    renderResolution,
                    contextScale,
                    fontScale,
                    panelDepth,
                    panelCurve,
                    panelRounding,
                    createExtrudedPanelMesh);

            if (scaleConfig.HasValue)
            {
                settings.ContainerScaleConfig = scaleConfig.Value;
            }

            return Add3DWindow(uiWindow, settings, position, rotation);
        }

        /// <summary>
        /// Adds a UI window to a 3D panel whose render resolution follows panel size from a reference size.
        /// </summary>
        public static Fu3DWindowContainer Add3DWindowScaledWithPanel(FuWindow uiWindow, Vector2 panelSize, Vector2Int referenceResolution, Vector2 referencePanelSize, Vector3? position = null, Quaternion? rotation = null, FuContainerScaleConfig? scaleConfig = null, float contextScale = 1f, float fontScale = 1f, Vector2Int? minResolution = null, Vector2Int? maxResolution = null, float panelDepth = 0.01f, float panelCurve = 0f, float panelRounding = Fu3DWindowSettings.DefaultPanelRounding, bool createExtrudedPanelMesh = true)
        {
            Fu3DWindowSettings settings = Fu3DWindowSettings.ScaledResolutionWithPanel(
                panelSize,
                referenceResolution,
                referencePanelSize,
                contextScale,
                fontScale,
                minResolution,
                maxResolution,
                panelDepth,
                panelCurve,
                panelRounding,
                createExtrudedPanelMesh);

            if (scaleConfig.HasValue)
            {
                settings.ContainerScaleConfig = scaleConfig.Value;
            }

            return Add3DWindow(uiWindow, settings, position, rotation);
        }

        /// <summary>
        /// Returns the add3 dwindow result.
        /// </summary>
        /// <param name="uiWindow">The ui Window value.</param>
        /// <param name="position">The position value.</param>
        /// <param name="rotation">The rotation value.</param>
        /// <param name="scaleConfig">The scale Config value.</param>
        /// <param name="scale3D">The scale3 D value.</param>
        /// <returns>The result of the operation.</returns>
        [Obsolete("Use Add3DWindow(FuWindow, Fu3DWindowSettings, ...) to provide panel size and render resolution explicitly.")]
        public static Fu3DWindowContainer Add3DWindow(FuWindow uiWindow, Vector3? position = null, Quaternion? rotation = null, FuContainerScaleConfig? scaleConfig = null, float? scale3D = null)
        {
            return Add3DWindow(uiWindow, getLegacy3DWindowSettings(scaleConfig, scale3D), position, rotation);
        }

        /// <summary>
        /// Removes a 3D window with the specified Window.
        /// </summary>
        /// <param name="uiWindow">The 3D window to be removed.</param>
        internal static void Remove3DWindow(FuWindow uiWindow)
        {
            // Close the 3D window and remove it from the list
            Remove3DWindow(uiWindow.ID);
        }

        /// <summary>
        /// Removes an external window with the specified ID.
        /// </summary>
        /// <param name="id">The ID of the external window to be removed.</param>
        internal static void Remove3DWindow(string id)
        {
            // Check if a 3D window with the specified ID exists
            if (!_3DWindows.ContainsKey(id))
            {
                return;
            }
            // Close the 3D window and remove it from the list
            Fu3DWindowContainer container = _3DWindows[id];
            _3DWindows.Remove(id);
            if (container.Window != null)
            {
                container.Window.Close();
            }
            else
            {
                container.Close();
            }
        }

        /// <summary>
        /// Remove a 3D window container from Fugui registry without closing it again.
        /// </summary>
        /// <param name="id">Window ID associated with the 3D container.</param>
        internal static void Unregister3DWindow(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            _3DWindows.Remove(id);
            ApplyMainContainerCameraState();
        }
    }
}
