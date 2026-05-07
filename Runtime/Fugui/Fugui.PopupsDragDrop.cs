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
    /// Fugui popup and drag-drop helpers.
    /// </summary>
    public static partial class Fugui
    {
        /// <summary>
        /// Whatever a popup is open from a specific window
        /// </summary>
        /// <param name="window">window to check</param>
        /// <returns>true if a popup if open from this window</returns>
        public static bool WindowHasPopupOpen(FuWindow window)
        {
            return PopUpWindowsIDs.Any(popupWindowID => window.ID == popupWindowID);
        }

        /// <summary>
        /// Get whatever a world position (container-relative) is inside an open popup
        /// </summary>
        /// <param name="worldPosition">world position (container-relative) to check</param>
        /// <returns>true if the position is inside some currently open popup</returns>
        public static bool IsInsideAnyPopup(Vector2 worldPosition)
        {
            return PopUpRects.Any(popupRect => popupRect.Contains(worldPosition)) || isInsideNotifyPanel(worldPosition);
        }

        /// <summary>
        /// Whatever fugui is currently drawing inside a popup
        /// </summary>
        /// <returns>true if we are drawing on a popup</returns>
        public static bool IsDrawingInsidePopup()
        {
            return IsPopupDrawing.Any(isDrawing => isDrawing);
        }

        /// <summary>
        /// Whatever there is currently at least one popup open
        /// </summary>
        /// <returns>true if there is at least one popup open</returns>
        public static bool IsThereAnyOpenPopup()
        {
            return PopUpIDs.Count > 0;
        }

        /// <summary>
        /// Whatever the current drawing popup has focus
        /// </summary>
        /// <returns>true if the current drawing popup has focus</returns>
        public static bool IsDrawingPopupFocused()
        {
            for (int i = 0; i < IsPopupFocused.Count; i++)
            {
                if (IsPopupDrawing[i] && IsPopupFocused[i])
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Must be placed just after an UI element so this one can be dragged
        /// </summary>
        /// <param name="payloadID">Unique ID of the payload for the drag drop operation (must be same as used in BeginDragDropTarget method)</param>
        /// <param name="dragDropFlags">Flags for this drag drop operation.</param>
        /// <param name="onDraggingUICallback">Callback called each frame while a drag drop operation. Use it to draw the preview drag drop window UI)</param>
        /// <param name="payload">payload to set, will be passed to the target on Drop frame</param>
        public static void BeginDragDropSource(string payloadID, FuDragDropFlags dragDropFlags, Action onDraggingUICallback, object payload)
        {
            CurrentContext.BeginDragDropSource(payloadID, dragDropFlags.ToImGui(), onDraggingUICallback, payload);
        }

        internal static void BeginDragDropSource(string payloadID, ImGuiDragDropFlags dragDropFlags, Action onDraggingUICallback, object payload)
        {
            CurrentContext.BeginDragDropSource(payloadID, dragDropFlags, onDraggingUICallback, payload);
        }

        /// <summary>
        /// Must be placed just after an UI element so this can be dropped
        /// </summary>
        /// <typeparam name="T">Type of the drag drop payload to get (must be same as set as 'payload' arg in BeginDragDropSource method)</typeparam>
        /// <param name="payloadID">Unique ID of the payload for the drag drop operation (must be same as used in BeginDragDropTarget method)</param>
        /// <param name="onDropCallback">Callback called whenever the user drop the dragging payload on this UI element</param>
        public static void BeginDragDropTarget<T>(string payloadID, Action<T> onDropCallback)
        {
            CurrentContext.BeginDragDropTarget<T>(payloadID, onDropCallback);
        }

        /// <summary>
        /// Cancel a drag drop operation related to the given payloadID
        /// </summary>
        /// <param name="payloadID">ID of the payload to cancel (keep null to cancel any current drag drop operation)</param>
        public static void CancelDragDrop(string payloadID = null)
        {
            CurrentContext.CancelDragDrop(payloadID);
        }

        /// <summary>
        /// Get the current drag drop payload (null if there is no drag drop operation for now)
        /// </summary>
        /// <typeparam name="T">Type of the current payload</typeparam>
        /// <returns>return the current drag drop payload if there is one</returns>
        public static T GetDragDropPayload<T>()
        {
            return CurrentContext.GetDragDropPayload<T>();
        }

        /// <summary>
        /// Whatever we are performing a drag drop operation right now with the given payloadID
        /// </summary>
        /// <param name="payloadID">ID of the payload (Drag Drop data ID) to check</param>
        /// <returns>true if user if performing a drag drop operation for the given payload ID</returns>
        public static bool IsDraggingPayload(string payloadID)
        {
            return CurrentContext.IsDraggingPayload(payloadID);
        }
    }
}
