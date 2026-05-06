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
    /// Fugui main container camera management.
    /// </summary>
    public static partial class Fugui
    {
        /// <summary>
        /// Applies the main container state to the dedicated fullscreen UI camera.
        /// </summary>
        private static void ApplyMainContainerCameraState()
        {
            Camera camera = DefaultContext != null ? DefaultContext.Camera : null;

            if (camera == null)
            {
                return;
            }

            if (_mainContainerEnabled)
            {
                RestoreMainContainerCameraState(camera);
                camera.enabled = true;
                return;
            }

            if (!HasOffscreenDriverWork())
            {
                RestoreMainContainerCameraState(camera);
                camera.enabled = false;
                return;
            }

            ConfigureMainContainerCameraAsOffscreenDriver(camera);
        }

        /// <summary>
        /// Returns true when the camera is the hidden non-XR camera used to render offscreen contexts.
        /// </summary>
        /// <param name="camera">Camera tested by the render feature.</param>
        /// <returns>True when the camera should drive offscreen Fugui render textures.</returns>
        internal static bool IsOffscreenDriverCamera(Camera camera)
        {
            return !_mainContainerEnabled &&
                   HasOffscreenDriverWork() &&
                   camera != null &&
                   DefaultContext != null &&
                   ReferenceEquals(camera, DefaultContext.Camera);
        }

        /// <summary>
        /// Returns true when offscreen UI render textures need a camera-driven URP pass.
        /// </summary>
        /// <returns>True when a 3D window is currently registered.</returns>
        private static bool HasOffscreenDriverWork()
        {
            return _3DWindows != null && _3DWindows.Count > 0;
        }

        /// <summary>
        /// Store user-authored camera state before temporarily using the UI camera as an offscreen driver.
        /// </summary>
        /// <param name="camera">Camera to snapshot.</param>
        private static void StoreMainContainerCameraState(Camera camera)
        {
            if (_mainContainerCameraStateStored || camera == null)
            {
                return;
            }

            _mainContainerCameraTargetTexture = camera.targetTexture;
            _mainContainerCameraCullingMask = camera.cullingMask;
            _mainContainerCameraClearFlags = camera.clearFlags;
            _mainContainerCameraBackgroundColor = camera.backgroundColor;

            UniversalAdditionalCameraData additionalCameraData = camera.GetComponent<UniversalAdditionalCameraData>();
            _mainContainerCameraHadAdditionalCameraData = additionalCameraData != null;
            _mainContainerCameraAllowXRRendering = additionalCameraData == null || additionalCameraData.allowXRRendering;
            _mainContainerCameraStateStored = true;
        }

        /// <summary>
        /// Restore the UI camera when the fullscreen main container is enabled again.
        /// </summary>
        /// <param name="camera">Camera to restore.</param>
        private static void RestoreMainContainerCameraState(Camera camera)
        {
            if (!_mainContainerCameraStateStored || camera == null)
            {
                return;
            }

            camera.targetTexture = _mainContainerCameraTargetTexture;
            camera.cullingMask = _mainContainerCameraCullingMask;
            camera.clearFlags = _mainContainerCameraClearFlags;
            camera.backgroundColor = _mainContainerCameraBackgroundColor;
            SetCameraXRRendering(camera, _mainContainerCameraAllowXRRendering);
            RemoveTemporaryCameraData(camera);
            _mainContainerCameraStateStored = false;
        }

        /// <summary>
        /// Use the UI camera as an invisible non-XR render driver for 3D window render textures.
        /// </summary>
        /// <param name="camera">Camera to configure.</param>
        private static void ConfigureMainContainerCameraAsOffscreenDriver(Camera camera)
        {
            StoreMainContainerCameraState(camera);

            camera.targetTexture = GetOrCreateOffscreenDriverTexture();
            camera.cullingMask = 0;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.enabled = true;
            SetCameraXRRendering(camera, false);
        }

        /// <summary>
        /// Enable or disable XR rendering on a URP camera when the component exists.
        /// </summary>
        /// <param name="camera">Camera to configure.</param>
        /// <param name="allowXRRendering">Whether URP may render this camera through XR.</param>
        private static void SetCameraXRRendering(Camera camera, bool allowXRRendering)
        {
            UniversalAdditionalCameraData additionalCameraData = camera != null ? camera.GetComponent<UniversalAdditionalCameraData>() : null;
            if (additionalCameraData == null && camera != null && !allowXRRendering)
            {
                additionalCameraData = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }

            if (additionalCameraData != null)
            {
                additionalCameraData.allowXRRendering = allowXRRendering;
            }
        }

        /// <summary>
        /// Remove runtime-added URP camera data when restoring a camera that did not originally own it.
        /// </summary>
        /// <param name="camera">Camera to restore.</param>
        private static void RemoveTemporaryCameraData(Camera camera)
        {
            if (_mainContainerCameraHadAdditionalCameraData || camera == null)
            {
                return;
            }

            UniversalAdditionalCameraData additionalCameraData = camera.GetComponent<UniversalAdditionalCameraData>();
            if (additionalCameraData != null)
            {
                UnityEngine.Object.Destroy(additionalCameraData);
            }
        }

        /// <summary>
        /// Gets the tiny target used to keep the offscreen driver camera away from the GameView.
        /// </summary>
        /// <returns>The hidden offscreen driver render texture.</returns>
        private static RenderTexture GetOrCreateOffscreenDriverTexture()
        {
            if (_offscreenDriverTexture != null &&
                _offscreenDriverTexture.IsCreated() &&
                _offscreenDriverTexture.depthStencilFormat != GraphicsFormat.None)
            {
                return _offscreenDriverTexture;
            }

            ReleaseOffscreenDriverTexture();

            RenderTextureDescriptor descriptor = new RenderTextureDescriptor(1, 1)
            {
                graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm,
                depthStencilFormat = GraphicsFormat.D16_UNorm,
                msaaSamples = 1,
                useMipMap = false,
                autoGenerateMips = false,
                useDynamicScale = false
            };

            _offscreenDriverTexture = new RenderTexture(descriptor)
            {
                name = "Fugui Offscreen Driver",
                hideFlags = HideFlags.HideAndDontSave
            };
            _offscreenDriverTexture.Create();
            return _offscreenDriverTexture;
        }

        /// <summary>
        /// Release the offscreen driver target.
        /// </summary>
        private static void ReleaseOffscreenDriverTexture()
        {
            if (_offscreenDriverTexture == null)
            {
                return;
            }

            _offscreenDriverTexture.Release();
            UnityEngine.Object.Destroy(_offscreenDriverTexture);
            _offscreenDriverTexture = null;
        }
    }
}
