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
    /// Fugui scale helpers.
    /// </summary>
    public static partial class Fugui
    {
        /// <summary>
        /// Build the default scale configuration used by main and external containers.
        /// </summary>
        /// <returns>The default container scale configuration.</returns>
        public static FuContainerScaleConfig GetDefaultContainerScaleConfig()
        {
            if (Settings == null)
            {
                return FuContainerScaleConfig.Disabled(1f, 1f);
            }

            if (!Settings.EnableContainerScaler)
            {
                return FuContainerScaleConfig.Disabled(Settings.GlobalScale, Settings.FontGlobalScale);
            }

            return FuContainerScaleConfig.Reference(
                Settings.ContainerReferenceResolution,
                Settings.ContainerMatchWidthOrHeight,
                Settings.ContainerMinScale,
                Settings.ContainerMaxScale,
                Settings.GlobalScale,
                Settings.FontGlobalScale,
                Settings.ContainerScaleFonts,
                Settings.ContainerUseDpiScale,
                Settings.ContainerReferenceDpi
            );
        }

        /// <summary>
        /// Returns the get legacy3 dwindow settings result.
        /// </summary>
        /// <param name="scaleConfig">The scale Config value.</param>
        /// <param name="scale3D">The scale3 D value.</param>
        /// <returns>The result of the operation.</returns>
        private static Fu3DWindowSettings getLegacy3DWindowSettings(FuContainerScaleConfig? scaleConfig, float? scale3D)
        {
            float contextScale = Settings != null ? Settings.Windows3DSuperSampling : 1f;
            float fontScale = Settings != null ? Settings.Windows3DFontScale : 1f;
            float windows3DScale = scale3D.HasValue
                ? Mathf.Max(0.0001f, scale3D.Value)
                : (Settings != null ? Mathf.Max(0.0001f, Settings.Windows3DScale) : 10f);
            Vector2Int resolution = new Vector2Int(512, 512);
            Vector2 panelSize = new Vector2(
                resolution.x / contextScale * windows3DScale / 1000f,
                resolution.y / contextScale * windows3DScale / 1000f);

            float panelDepth = Settings != null ? Mathf.Max(0.0001f, Settings.UIPanelWidth) : 0.01f;
            Fu3DWindowSettings settings = Fu3DWindowSettings.FixedResolution(panelSize, resolution, contextScale, fontScale, panelDepth);
            if (scaleConfig.HasValue)
            {
                settings.ContainerScaleConfig = scaleConfig.Value;
            }
            return settings;
        }

        /// <summary>
        /// Set the scale of all context
        /// </summary>
        /// <param name="scale">global all context scale</param>
        /// <param name="fontScale">context all font scale (usualy same value as context scale)</param>
        public static void SetScale(float scale, float fontScale)
        {
            if (scale <= 0f)
            {
                Debug.LogError("Fugui global scale must be greater than 0");
                return;
            }
            if (scale > 5f)
            {
                Debug.LogError("Fugui global scale must be less than 10");
                return;
            }

            ExecuteInMainThread(() =>
            {
                _targetScale = scale;
                _targetFontScale = fontScale;
            });
        }
    }
}
