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
    /// Fugui input query helpers.
    /// </summary>
    public static partial class Fugui
    {
        /// <summary>
        /// Lock fugui auto set cursor icons
        /// </summary>
        public static void LockCursors()
        {
            IsCursorLocked = true;
        }

        /// <summary>
        /// Unlock fugui auto set cursor icons
        /// </summary>
        public static void UnlockCursors()
        {
            IsCursorLocked = false;
            CursorsJustUnlocked = true;
        }

        /// <summary>
        /// Get Whatever fugui want to capture a user input at this frame
        /// </summary>
        /// <param name="onlyCurrentContext">Whatever you want to check only the current Fugui context</param>
        /// <returns>true if Fugui want to capture user inputs this frame</returns>
        public static bool GetWantCaptureInputs(bool onlyCurrentContext)
        {
            switch (onlyCurrentContext)
            {
                default:
                case true:
                    ImGuiIOPtr io = CurrentContext != null ? CurrentContext.IO : ImGui.GetIO();
                    return io.WantTextInput;

                case false:
                    bool wantCapture = false;
                    foreach (FuContext context in Contexts.Values)
                    {
                        if (!MainContainerEnabled && ReferenceEquals(context, DefaultContext))
                        {
                            continue;
                        }

                        wantCapture |= context.IO.WantTextInput;
                    }
                    return wantCapture;
            }
        }

        /// <summary>
        /// Get whether Fugui currently owns, blocks or wants pointer input.
        /// </summary>
        /// <returns>true if Fugui needs the pointer; otherwise, false.</returns>
        public static bool GetWantCapturePointer()
        {
            return WindowInputsBlockedThisFrame ||
                   IsAnyWindowHovered() ||
                   IsAnyWindowInputFocused() ||
                   IsAnyWindowWantCaptureInput() ||
                   IsThereAnyOpenPopup() ||
                   IsAnyModalOpen() ||
                   IsAnyWindowResizing() ||
                   IsDraggingAnything() ||
                   (Layouts?.IsCustomDockManipulating ?? false) ||
                   GetAnyContextWantCapturePointer();
        }

        /// <summary>
        /// Get whether Fugui currently owns, blocks or wants pointer input outside a given window.
        /// </summary>
        /// <param name="ignoredWindow">Window whose own hover/focus/drag/resize state should be ignored.</param>
        /// <returns>true if another Fugui surface needs the pointer; otherwise, false.</returns>
        public static bool GetWantCapturePointer(FuWindow ignoredWindow)
        {
            if (ignoredWindow == null)
            {
                return GetWantCapturePointer();
            }

            bool hasOtherWindowHovered = WindowHoveredCount > (ignoredWindow.IsHovered ? 1 : 0);
            bool hasOtherWindowWantCaptureInput = WindowWantCaptureInputCount > (ignoredWindow.WantCaptureKeyboard ? 1 : 0);
            bool hasOtherWindowResizing = WindowResizingCount > (ignoredWindow.IsResizing ? 1 : 0);
            bool hasOtherWindowDragging = WindowDraggingCount > (ignoredWindow.IsDragging ? 1 : 0);
            bool hasOtherInputFocusedWindow = FuWindow.InputFocusedWindow != null
                ? FuWindow.InputFocusedWindow != ignoredWindow
                : FuWindow.NbInputFocusedWindow > 0;

            return hasOtherWindowHovered ||
                   hasOtherInputFocusedWindow ||
                   hasOtherWindowWantCaptureInput ||
                   IsThereAnyOpenPopup() ||
                   IsAnyModalOpen() ||
                   hasOtherWindowResizing ||
                   hasOtherWindowDragging ||
                   OverlayDraggingCount > 0 ||
                   DraggingPayloadCount > 0 ||
                   (Layouts?.IsCustomDockManipulating ?? false);
        }

        /// <summary>
        /// Get whether no Fugui window, popup, modal or manipulation currently owns the pointer.
        /// </summary>
        /// <returns>true if the pointer can be used by non-Fugui systems; otherwise, false.</returns>
        public static bool IsPointerFree()
        {
            return !GetWantCapturePointer();
        }

        private static bool GetAnyContextWantCapturePointer()
        {
            if (Contexts == null || Contexts.Count == 0)
            {
                return false;
            }

            foreach (FuContext context in Contexts.Values)
            {
                if (context == null || context.ImGuiContext == IntPtr.Zero)
                {
                    continue;
                }

                if (!MainContainerEnabled && ReferenceEquals(context, DefaultContext))
                {
                    continue;
                }

                ImGuiIOPtr io = context.IO;
                if (io.WantCaptureMouse || io.WantTextInput)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check Whatever a Key is Down for some given FuWIndowNames.
        /// If WindowNames array is empty, Fugui will check for any windows of any containers
        /// </summary>
        /// <param name="key">Key to check down state</param>
        /// <param name="windowsNames">windows names to check key satet on (you can leave this empty, it will check on any windows of any containers)</param>
        /// <returns>true if the key is pressed into the given scope</returns>
        public static bool GetKeyDown(FuKeysCode key, params FuWindowName[] windowsNames)
        {
            bool isDown = false;
            if (windowsNames == null || windowsNames.Length == 0)
            {
                isDown |= MainContainerEnabled && DefaultContainer != null && DefaultContainer.Keyboard.GetKeyDown(key);
                if (!isDown)
                {
                    foreach (var threeDWindowContainer in _3DWindows.Values)
                    {
                        if (threeDWindowContainer.Keyboard.GetKeyDown(key))
                        {
                            isDown = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                foreach (FuWindowName windowName in windowsNames)
                {
                    foreach (var window in UIWindows)
                    {
                        if (window.Value == null || window.Value.Keyboard == null)
                        {
                            continue;
                        }
                        if (window.Value.WindowName.Equals(windowName))
                        {
                            if (window.Value.Keyboard.GetKeyDown(key))
                            {
                                isDown = true;
                                break;
                            }
                        }
                    }
                }
            }
            return isDown;
        }

        /// <summary>
        /// Check Whatever a Key is Pressed for some given FuWIndowNames.
        /// If WindowNames array is empty, Fugui will check for any windows of any containers
        /// </summary>
        /// <param name="key">Key to check down state</param>
        /// <param name="windowsNames">windows names to check key satet on (you can leave this empty, it will check on any windows of any containers)</param>
        /// <returns>true if the key is pressed into the given scope</returns>
        public static bool GetKeyPressed(FuKeysCode key, params FuWindowName[] windowsNames)
        {
            bool isPressed = false;
            if (windowsNames == null || windowsNames.Length == 0)
            {
                isPressed |= MainContainerEnabled && DefaultContainer != null && DefaultContainer.Keyboard.GetKeyPressed(key);
                if (!isPressed)
                {
                    foreach (var threeDWindowContainer in _3DWindows.Values)
                    {
                        if (threeDWindowContainer.Keyboard.GetKeyPressed(key))
                        {
                            isPressed = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                foreach (FuWindowName windowName in windowsNames)
                {
                    foreach (var window in UIWindows)
                    {
                        if (window.Value == null || window.Value.Keyboard == null)
                        {
                            continue;
                        }
                        if (window.Value.WindowName.Equals(windowName))
                        {
                            if (window.Value.Keyboard.GetKeyPressed(key))
                            {
                                isPressed = true;
                                break;
                            }
                        }
                    }
                }
            }
            return isPressed;
        }

        /// <summary>
        /// Check Whatever a Key is Up for some given FuWIndowNames.
        /// If WindowNames array is empty, Fugui will check for any windows of any containers
        /// </summary>
        /// <param name="key">Key to check down state</param>
        /// <param name="windowsNames">windows names to check key satet on (you can leave this empty, it will check on any windows of any containers)</param>
        /// <returns>true if the key is pressed into the given scope</returns>
        public static bool GetKeyUp(FuKeysCode key, params FuWindowName[] windowsNames)
        {
            bool isUp = false;
            if (windowsNames == null || windowsNames.Length == 0)
            {
                isUp |= MainContainerEnabled && DefaultContainer != null && DefaultContainer.Keyboard.GetKeyUp(key);
                if (!isUp)
                {
                    foreach (var threeDWindowContainer in _3DWindows.Values)
                    {
                        if (threeDWindowContainer.Keyboard.GetKeyUp(key))
                        {
                            isUp = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                foreach (FuWindowName windowName in windowsNames)
                {
                    foreach (var window in UIWindows)
                    {
                        if (window.Value == null || window.Value.Keyboard == null)
                        {
                            continue;
                        }
                        if (window.Value.WindowName.Equals(windowName))
                        {
                            if (window.Value.Keyboard.GetKeyUp(key))
                            {
                                isUp = true;
                                break;
                            }
                        }
                    }
                }
            }
            return isUp;
        }

        /// <summary>
        /// Get the current mouse state
        /// </summary>
        /// <returns> current mouse state</returns>
        public static FuMouseState GetCurrentMouse()
        {
            if (FuWindow.CurrentDrawingWindow != null)
            {
                return FuWindow.CurrentDrawingWindow.Mouse;
            }
            return DefaultContainer.Mouse;
        }

        /// <summary>
        /// Returns the raw pressed state for a mouse button in the current Fugui context.
        /// </summary>
        internal static bool IsMousePressed(FuMouseButton mouseButton)
        {
            return ImGuiNative.igIsMouseDown_Nil((ImGuiMouseButton)mouseButton) != 0;
        }

        /// <summary>
        /// Returns the raw clicked state for a mouse button in the current Fugui context.
        /// </summary>
        internal static bool IsMouseClicked(FuMouseButton mouseButton)
        {
            return ImGuiNative.igIsMouseClicked_Bool((ImGuiMouseButton)mouseButton, 0) != 0;
        }

        /// <summary>
        /// Returns the raw released state for a mouse button in the current Fugui context.
        /// </summary>
        internal static bool IsMouseReleased(FuMouseButton mouseButton)
        {
            return ImGuiNative.igIsMouseReleased_Nil((ImGuiMouseButton)mouseButton) != 0;
        }

        /// <summary>
        /// Returns whether the last submitted native item is active.
        /// </summary>
        internal static bool IsCurrentItemActive()
        {
            return ImGui.IsItemActive();
        }

        /// <summary>
        /// Returns whether the last submitted native item is focused.
        /// </summary>
        internal static bool IsCurrentItemFocused()
        {
            return ImGui.IsItemFocused();
        }

        /// <summary>
        /// Returns whether the last submitted native item is hovered.
        /// </summary>
        internal static bool IsCurrentItemHovered(ImGuiHoveredFlags flags = ImGuiHoveredFlags.None)
        {
            return ImGui.IsItemHovered(flags);
        }

        /// <summary>
        /// Returns whether the last submitted native item was clicked with the requested button.
        /// </summary>
        internal static bool IsCurrentItemClicked(FuMouseButton mouseButton)
        {
            return ImGui.IsItemClicked((ImGuiMouseButton)mouseButton);
        }
    }
}
