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
    /// Type of top-level Fugui surface tracked for input arbitration.
    /// </summary>
    public enum FuSurfaceType
    {
        Window = 0,
        Hud = 1,
        Popup = 2,
        Modal = 3,
        Notification = 4,
        Top = 5
    }

    /// <summary>
    /// Fugui-owned surface snapshot used to arbitrate pointer input.
    /// </summary>
    internal struct FuSurface
    {
        public int ContextID;
        public string ID;
        public FuSurfaceType Type;
        public FuLayer Layer;
        public FuWindow OwnerWindow;
        public Rect Rect;
        public bool BlocksLowerSurfaces;
        public bool AcceptsInput;
        public int Order;
    }

    /// <summary>
    /// Fugui popup and drag-drop helpers.
    /// </summary>
    public static partial class Fugui
    {
        private static readonly Dictionary<int, List<FuSurface>> _currentSurfacesByContext = new Dictionary<int, List<FuSurface>>();
        private static readonly Dictionary<int, List<FuSurface>> _lastSurfacesByContext = new Dictionary<int, List<FuSurface>>();
        private static readonly Dictionary<int, int> _surfaceOrderByContext = new Dictionary<int, int>();
        private static readonly List<bool> _modalSurfaceInputStack = new List<bool>();

        /// <summary>
        /// Starts collecting Fugui surfaces for a container render frame.
        /// </summary>
        /// <param name="container">Container that is about to render.</param>
        internal static void BeginSurfaceFrame(IFuWindowContainer container)
        {
            if (!TryGetSurfaceContextID(container, out int contextID))
            {
                return;
            }

            if (!_currentSurfacesByContext.TryGetValue(contextID, out List<FuSurface> current))
            {
                current = new List<FuSurface>();
                _currentSurfacesByContext.Add(contextID, current);
            }

            if (!_lastSurfacesByContext.TryGetValue(contextID, out List<FuSurface> last))
            {
                last = new List<FuSurface>();
                _lastSurfacesByContext.Add(contextID, last);
            }

            last.Clear();
            last.AddRange(current);
            current.Clear();
            _surfaceOrderByContext[contextID] = 0;
        }

        /// <summary>
        /// Registers a drawn FuWindow as a Fugui surface.
        /// </summary>
        /// <param name="window">Window that was just drawn.</param>
        internal static void RegisterWindowSurface(FuWindow window)
        {
            if (window == null || window.Container == null || !window.IsOpened || !window.IsVisible)
            {
                return;
            }

            FuSurfaceType type = window.Layer == FuLayer.Background
                ? FuSurfaceType.Hud
                : window.Layer == FuLayer.Top
                    ? FuSurfaceType.Top
                    : FuSurfaceType.Window;

            RegisterSurface(
                window.Container,
                window.ID,
                type,
                window.Layer,
                window,
                window.LocalRect,
                window.Layer != FuLayer.Background,
                window.IsInterractable);
        }

        /// <summary>
        /// Registers a non-window Fugui surface.
        /// </summary>
        internal static void RegisterSurface(IFuWindowContainer container, string id, FuSurfaceType type, FuLayer layer, FuWindow ownerWindow, Rect rect, bool blocksLowerSurfaces, bool acceptsInput)
        {
            if (!TryGetSurfaceContextID(container, out int contextID) || rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            if (!_currentSurfacesByContext.TryGetValue(contextID, out List<FuSurface> current))
            {
                current = new List<FuSurface>();
                _currentSurfacesByContext.Add(contextID, current);
            }

            int order = 0;
            if (_surfaceOrderByContext.TryGetValue(contextID, out int currentOrder))
            {
                order = currentOrder;
            }
            _surfaceOrderByContext[contextID] = order + 1;

            current.Add(new FuSurface
            {
                ContextID = contextID,
                ID = id,
                Type = type,
                Layer = layer,
                OwnerWindow = ownerWindow,
                Rect = rect,
                BlocksLowerSurfaces = blocksLowerSurfaces,
                AcceptsInput = acceptsInput,
                Order = order
            });
        }

        /// <summary>
        /// Returns whether the current Fugui widget is allowed to receive pointer input.
        /// </summary>
        internal static bool CanInteractWithWidgetSurface(FuWindow ownerWindow, Rect itemRect, bool allowWhenBlockedByPopup)
        {
            if (IsDrawingInsidePopup())
            {
                return IsDrawingPopupFocused();
            }

            if (IsDrawingInsideModalSurface())
            {
                return IsDrawingModalSurfaceAcceptingInput();
            }

            IFuWindowContainer container = ownerWindow?.Container ?? DefaultContainer;
            if (container == null)
            {
                return true;
            }

            Vector2 mousePosition = container.LocalMousePos;
            if (!itemRect.Contains(mousePosition))
            {
                return true;
            }

            if (!allowWhenBlockedByPopup && IsThereAnyOpenPopup())
            {
                if (IsAnyModalOpen() || IsInsideAnyPopup(mousePosition))
                {
                    return false;
                }
            }

            if (!TryGetSurfaceContextID(container, out int contextID) ||
                !_lastSurfacesByContext.TryGetValue(contextID, out List<FuSurface> surfaces))
            {
                return true;
            }

            int ownerLayerRank = ownerWindow != null ? GetSurfaceLayerRank(ownerWindow.Layer) : GetSurfaceLayerRank(FuLayer.Top);
            int ownerOrder = -1;
            for (int i = 0; i < surfaces.Count; i++)
            {
                FuSurface surface = surfaces[i];
                if (ownerWindow != null && surface.OwnerWindow == ownerWindow && surface.Type == FuSurfaceType.Window)
                {
                    ownerOrder = Mathf.Max(ownerOrder, surface.Order);
                    ownerLayerRank = GetSurfaceLayerRank(surface.Layer);
                }
            }

            for (int i = surfaces.Count - 1; i >= 0; i--)
            {
                FuSurface surface = surfaces[i];
                if (!surface.BlocksLowerSurfaces || !surface.Rect.Contains(mousePosition))
                {
                    continue;
                }

                if (ownerWindow != null && surface.OwnerWindow == ownerWindow && surface.Type == FuSurfaceType.Window)
                {
                    continue;
                }

                int surfaceLayerRank = GetSurfaceLayerRank(surface.Layer);
                bool aboveOwner = surfaceLayerRank > ownerLayerRank ||
                                  (surfaceLayerRank == ownerLayerRank && surface.Order > ownerOrder);
                if (aboveOwner)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryGetSurfaceContextID(IFuWindowContainer container, out int contextID)
        {
            if (container?.Context == null)
            {
                contextID = 0;
                return false;
            }

            contextID = container.Context.ID;
            return true;
        }

        private static int GetSurfaceLayerRank(FuLayer layer)
        {
            switch (layer)
            {
                case FuLayer.Background:
                    return 0;
                case FuLayer.Top:
                    return 2;
                default:
                    return 1;
            }
        }

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
        /// Starts a scope where Fugui widgets are drawn inside a modal top surface.
        /// </summary>
        internal static void BeginModalSurfaceDrawing(bool acceptsInput)
        {
            _modalSurfaceInputStack.Add(acceptsInput);
        }

        /// <summary>
        /// Ends the current modal top-surface drawing scope.
        /// </summary>
        internal static void EndModalSurfaceDrawing()
        {
            if (_modalSurfaceInputStack.Count > 0)
            {
                _modalSurfaceInputStack.RemoveAt(_modalSurfaceInputStack.Count - 1);
            }
        }

        /// <summary>
        /// Whether Fugui is currently drawing widgets inside a modal top surface.
        /// </summary>
        internal static bool IsDrawingInsideModalSurface()
        {
            return _modalSurfaceInputStack.Count > 0;
        }

        /// <summary>
        /// Whether the current modal top surface accepts mouse input.
        /// </summary>
        internal static bool IsDrawingModalSurfaceAcceptingInput()
        {
            return _modalSurfaceInputStack.Count > 0 && _modalSurfaceInputStack[_modalSurfaceInputStack.Count - 1];
        }

        /// <summary>
        /// Whatever there is currently at least one popup open
        /// </summary>
        /// <returns>true if there is at least one popup open</returns>
        public static bool IsThereAnyOpenPopup()
        {
            return PopUpIDs.Count > 0 ||
                   _registeredPopups.Count > 0 ||
                   IsContextMenuOpen;
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
            CurrentContext.BeginDragDropSource(payloadID, dragDropFlags, onDraggingUICallback, payload);
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
