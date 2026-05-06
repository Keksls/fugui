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
    /// Fugui window input blocking internals.
    /// </summary>
    public static partial class Fugui
    {
        internal static void BlockWindowInputsForFrame()
        {
            WindowInputsBlockedThisFrame = true;
            SuppressCurrentContextWindowInputs();
        }

        internal static bool TryGetBlockedFrameRawMouseDown(FuMouseButton button, out bool isDown)
        {
            int index = (int)button;
            if (!WindowInputsBlockedThisFrame || index < 0 || index >= WindowInputBlockMouseButtonCount)
            {
                isDown = false;
                return false;
            }

            isDown = _blockedFrameRawMouseDown[index];
            return true;
        }

        internal static bool TryGetBlockedFrameRawMousePressed(FuMouseButton button, out bool isPressed)
        {
            int index = (int)button;
            if (!WindowInputsBlockedThisFrame || index < 0 || index >= WindowInputBlockMouseButtonCount)
            {
                isPressed = false;
                return false;
            }

            isPressed = _blockedFrameRawMousePressed[index];
            return true;
        }

        internal static bool IsMouseButtonPressedBeforeCurrentFrame(FuMouseButton button)
        {
            int index = (int)button;
            if (index < 0 || index >= WindowInputBlockMouseButtonCount)
            {
                return false;
            }

            if (_windowInputSnapshotCaptured)
            {
                return _inputSnapshotMouseDown[index] && !_inputSnapshotMouseClicked[index];
            }

            if (CurrentContext == null || CurrentContext.ImGuiContext == IntPtr.Zero)
            {
                return false;
            }

            ImGuiIOPtr io = CurrentContext.IO;
            return io.MouseDown[index] && !io.MouseClicked[index];
        }

        private static void ResetWindowInputBlockForFrame()
        {
            WindowInputsBlockedThisFrame = false;
            _windowInputSnapshotCaptured = false;
            for (int i = 0; i < WindowInputBlockMouseButtonCount; i++)
            {
                _blockedFrameRawMouseDown[i] = false;
                _blockedFrameRawMousePressed[i] = false;
            }
        }

        private static void SuppressCurrentContextWindowInputs()
        {
            if (CurrentContext == null || CurrentContext.ImGuiContext == IntPtr.Zero)
            {
                return;
            }

            ImGuiIOPtr io = CurrentContext.IO;
            if (!_windowInputSnapshotCaptured)
            {
                CaptureWindowInputSnapshot(io);
            }
            io.MouseWheel = 0f;
            io.MouseWheelH = 0f;

            for (int i = 0; i < WindowInputBlockMouseButtonCount; i++)
            {
                bool mousePressed = io.MouseDown[i];
                bool mouseClicked = io.MouseClicked[i];
                if (!mousePressed)
                {
                    _blockedInputHeldFromOutside[i] = false;
                    _blockedInputDownEmitted[i] = false;
                }
                else if (!mouseClicked && !_blockedInputDownEmitted[i])
                {
                    _blockedInputHeldFromOutside[i] = true;
                }

                bool emitRawDown = mouseClicked && !_blockedInputHeldFromOutside[i] && !_blockedInputDownEmitted[i];
                if (emitRawDown)
                {
                    _blockedInputDownEmitted[i] = true;
                }

                _blockedFrameRawMouseDown[i] |= emitRawDown;
                _blockedFrameRawMousePressed[i] |= mousePressed;

                io.MouseDown[i] = false;
                io.MouseClicked[i] = false;
                io.MouseReleased[i] = false;
                io.MouseDoubleClicked[i] = false;
                io.MouseDownOwned[i] = false;
                io.MouseDownOwnedUnlessPopupClose[i] = false;
                io.MouseClickedCount[i] = 0;
                io.MouseClickedLastCount[i] = 0;
                io.MouseDownDuration[i] = -1f;
                io.MouseDownDurationPrev[i] = -1f;
                io.MouseDragMaxDistanceAbs[i] = Vector2.zero;
                io.MouseDragMaxDistanceSqr[i] = 0f;
            }
        }

        private static void CaptureWindowInputSnapshot(ImGuiIOPtr io)
        {
            _windowInputSnapshotCaptured = true;
            for (int i = 0; i < WindowInputBlockMouseButtonCount; i++)
            {
                _inputSnapshotMouseDown[i] = io.MouseDown[i];
                _inputSnapshotMouseClicked[i] = io.MouseClicked[i];
                _inputSnapshotMouseReleased[i] = io.MouseReleased[i];
                _inputSnapshotMouseDoubleClicked[i] = io.MouseDoubleClicked[i];
                _inputSnapshotMouseDownOwned[i] = io.MouseDownOwned[i];
                _inputSnapshotMouseDownOwnedUnlessPopupClose[i] = io.MouseDownOwnedUnlessPopupClose[i];
                _inputSnapshotMouseClickedCount[i] = io.MouseClickedCount[i];
                _inputSnapshotMouseClickedLastCount[i] = io.MouseClickedLastCount[i];
                _inputSnapshotMouseDownDuration[i] = io.MouseDownDuration[i];
                _inputSnapshotMouseDownDurationPrev[i] = io.MouseDownDurationPrev[i];
                _inputSnapshotMouseDragMaxDistanceAbs[i] = io.MouseDragMaxDistanceAbs[i];
                _inputSnapshotMouseDragMaxDistanceSqr[i] = io.MouseDragMaxDistanceSqr[i];
            }
        }

        private static void RestoreWindowInputsAfterFrame()
        {
            if (!_windowInputSnapshotCaptured || CurrentContext == null || CurrentContext.ImGuiContext == IntPtr.Zero)
            {
                _windowInputSnapshotCaptured = false;
                return;
            }

            ImGuiIOPtr io = CurrentContext.IO;
            for (int i = 0; i < WindowInputBlockMouseButtonCount; i++)
            {
                io.MouseDown[i] = _inputSnapshotMouseDown[i];
                io.MouseClicked[i] = _inputSnapshotMouseClicked[i];
                io.MouseReleased[i] = _inputSnapshotMouseReleased[i];
                io.MouseDoubleClicked[i] = _inputSnapshotMouseDoubleClicked[i];
                io.MouseDownOwned[i] = _inputSnapshotMouseDownOwned[i];
                io.MouseDownOwnedUnlessPopupClose[i] = _inputSnapshotMouseDownOwnedUnlessPopupClose[i];
                io.MouseClickedCount[i] = _inputSnapshotMouseClickedCount[i];
                io.MouseClickedLastCount[i] = _inputSnapshotMouseClickedLastCount[i];
                io.MouseDownDuration[i] = _inputSnapshotMouseDownDuration[i];
                io.MouseDownDurationPrev[i] = _inputSnapshotMouseDownDurationPrev[i];
                io.MouseDragMaxDistanceAbs[i] = _inputSnapshotMouseDragMaxDistanceAbs[i];
                io.MouseDragMaxDistanceSqr[i] = _inputSnapshotMouseDragMaxDistanceSqr[i];
            }

            _windowInputSnapshotCaptured = false;
        }
    }
}
