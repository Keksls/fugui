﻿using Fu.Core;
using Fu.Framework;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fu
{
    public partial class Fugui
    {
        // the index of the current popup
        private static int _currentPopupIndex = 0;
        private static Dictionary<string, FuPopupData> _registeredPopups = new Dictionary<string, FuPopupData>();

        /// <summary>
        /// Get the PopupID unique by current drawing window
        /// </summary>
        /// <param name="ID">ID to unify</param>
        /// <returns>Unique ID</returns>
        public static string GetUniquePopupID(string ID)
        {
            if (FuWindow.CurrentDrawingWindow != null)
            {
                return ID + "##" + FuWindow.CurrentDrawingWindow.ID;
            }
            return ID;
        }

        /// <summary>
        /// Force to close the current openpopup (if there is some)
        /// Work with : Context menu, Popup, Combobox
        /// </summary>
        public static void ForceCloseOpenPopup()
        {
            ImGui.CloseCurrentPopup();
        }

        /// <summary>
        /// Give the order to draw a specific popup.
        /// You must call DrawPopup (each frames) to display the popup
        /// </summary>
        /// <param name="id">ID of the popup to draw</param>
        /// <param name="ui">UI to draw inside the popup</param>
        /// <param name="size">default size of the popup</param>
        /// <param name="onClose">callback invoken then the popup close</param>
        public static void OpenPopUp(string id, Action ui, Action onClose = null, bool isComboBoxPopup = false)
        {
            OpenPopUp(id, ui, Vector2.zero, onClose, isComboBoxPopup);
        }

        /// <summary>
        /// Give the order to draw a specific popup.
        /// You must call DrawPopup (each frames) to display the popup
        /// </summary>
        /// <param name="id">ID of the popup to draw</param>
        /// <param name="ui">UI to draw inside the popup</param>
        /// <param name="size">default size of the popup</param>
        /// <param name="onClose">callback invoken then the popup close</param>
        public static void OpenPopUp(string id, Action ui, Vector2 size, Action onClose = null, bool isComboBoxPopup = false)
        {
            id = GetUniquePopupID(id);
            // remove from dic if already exists
            _registeredPopups.Remove(id);
            // scale size
            if (size.x > 0)
            {
                size.x *= CurrentContext.Scale;
            }
            if (size.y > 0)
            {
                size.y *= CurrentContext.Scale;
            }
            // add to dic
            FuPopupData data = new FuPopupData()
            {
                LastFrameRender = ImGui.GetFrameCount(),
                OpenThisFrame = true,
                CloseThisFrame = false,
                isComboBox = isComboBoxPopup,
                Size = size,
                UI = ui,
                OnClose = onClose
            };
            _registeredPopups.Add(id, data);
        }

        /// <summary>
        /// Get whatevr a Popup is curently open
        /// </summary>
        /// <param name="id">id of  the popup to check</param>
        /// <returns>True if popup if open</returns>
        public static bool IsPopupOpen(string id)
        {
            return _registeredPopups.ContainsKey(GetUniquePopupID(id));
        }

        /// <summary>
        /// Draw a registered popup (will draw only after you call OpenPopup
        /// </summary>
        /// <param name="id">ID of the popup to draw</param>
        public static void DrawPopup(string id)
        {
            DrawPopup(id, Vector2.zero, Vector2.zero);
        }

        /// <summary>
        /// Draw a registered popup (will draw only after you call OpenPopup
        /// </summary>
        /// <param name="id">ID of the popup to draw</param>
        /// <param name="size">size of the popup</param>
        /// <param name="pos">position of the popup</param>
        public static void DrawPopup(string id, Vector2 size, Vector2 pos, bool autoScale = false)
        {
            // get unique ID for this popup
            id = GetUniquePopupID(id);
            if (_registeredPopups.TryGetValue(id, out FuPopupData data))
            {
                // open popup if needed
                if (data.OpenThisFrame)
                {
                    ImGui.OpenPopup(id);
                }

                // scale size
                if (autoScale)
                {
                    if (size.x > 0)
                    {
                        size.x *= CurrentContext.Scale;
                    }
                    if (size.y > 0)
                    {
                        size.y *= CurrentContext.Scale;
                    }
                }

                // keep size to force popup size
                if (size.x > 0 || size.y > 0)
                {
                    data.Size = size;
                }

                // set size
                ImGui.SetNextWindowSize(data.Size);
                if (data.isComboBox)
                {
                    ImGui.SetNextWindowSizeConstraints(Vector2.zero, new Vector2(data.Size.x, FuLayout.COMBOBOX_POPUP_MAXIMUM_HEIGHT));
                }
                if (pos.x != 0f || pos.y != 0f)
                {
                    ImGui.SetNextWindowPos(pos);
                }

                // draw popup
                if (ImGui.BeginPopup(id))
                {
                    data.OpenThisFrame = false;
                    data.LastFrameRender = ImGui.GetFrameCount();

                    // push popup to stack
                    if (data.PopupIndex == -1)
                    {
                        data.PopupIndex = _currentPopupIndex++;
                        PopUpWindowsIDs.Add(FuWindow.CurrentDrawingWindow?.ID);
                        PopUpIDs.Add(id);
                        IsPopupDrawing.Add(true);
                        IsPopupFocused.Add(true);
                        PopUpRects.Add(new Rect(ImGui.GetWindowPos(), ImGui.GetWindowSize()));
                    }
                    IsPopupDrawing[data.PopupIndex] = true;
                    IsPopupFocused[data.PopupIndex] = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows | ImGuiFocusedFlags.NoPopupHierarchy);

                    try
                    {
                        // execute the callback
                        data.UI?.Invoke();

                        // update popup value on stack
                        IsPopupDrawing[data.PopupIndex] = false;
                        PopUpRects[data.PopupIndex] = new Rect(ImGui.GetWindowPos(), ImGui.GetWindowSize());
                    }
                    catch (Exception ex)
                    {
                        ImGui.EndPopup();
                        _closePopup(id);
                        Fire_OnUIException(ex);
                    }
                    finally
                    {
                        ImGui.EndPopup();
                    }
                }
                else
                {
                    data.CloseThisFrame = true;
                }
                if (data.CloseThisFrame)
                {
                    _closePopup(id);
                }
            }
        }

        /// <summary>
        /// get the Rect of a specific popup on last frame
        /// </summary>
        /// <param name="id">ID of the popup to get Rect on</param>
        /// <returns>The rect of the popup at the end of the last frame</returns>
        public static Rect GetPopupLastFrameRect(string id)
        {
            if (_registeredPopups.TryGetValue(id, out FuPopupData data))
            {
                if (data.PopupIndex >= 0 && data.PopupIndex < PopUpRects.Count)
                {
                    return PopUpRects[data.PopupIndex];
                }
                else
                {
                    return new Rect(Vector2.zero, data.Size);
                }
            }
            return default;
        }

        /// <summary>
        /// Close the popup message
        /// </summary>
        public static void ClosePopup(string id)
        {
            id = GetUniquePopupID(id);
            if (_registeredPopups.TryGetValue(id, out FuPopupData data))
            {
                data.CloseThisFrame = true;
            }
        }

        /// <summary>
        /// Close the popup message
        /// </summary>
        private static void _closePopup(string id)
        {
            // invoke the OnClose callback
            if (_registeredPopups.ContainsKey(id))
            {
                var data = _registeredPopups[id];

                // pop popup from stack
                PopUpWindowsIDs.RemoveAt(data.PopupIndex);
                PopUpIDs.RemoveAt(data.PopupIndex);
                IsPopupDrawing.RemoveAt(data.PopupIndex);
                PopUpRects.RemoveAt(data.PopupIndex);
                IsPopupFocused.RemoveAt(data.PopupIndex);

                // update PopupIndex of each deeper popups
                foreach (var popupData in _registeredPopups.Values)
                {
                    if (popupData.PopupIndex > data.PopupIndex)
                    {
                        popupData.PopupIndex--;
                    }
                }

                // downsample static popup stack index
                _currentPopupIndex--;

                // invoke popup's onClose event if there is one
                _registeredPopups[id].OnClose?.Invoke();
            }
            // remove from dic
            _registeredPopups.Remove(id);
        }

        /// <summary>
        /// Smart clean of the popup stack (must be call on start of each frame)
        /// </summary>
        public static void CleanPopupStack()
        {
            List<string> popupIDs = _registeredPopups.Keys.ToList();
            int lastFrameCount = ImGui.GetFrameCount() - 1;
            foreach (string popupID in popupIDs)
            {
                if (_registeredPopups[popupID].LastFrameRender < lastFrameCount)
                {
                    _closePopup(popupID);
                }
            }
        }

        #region private class
        private class FuPopupData
        {
            public int LastFrameRender = 0;
            public int PopupIndex = -1;
            public Action UI;
            public Action OnClose;
            public bool isComboBox = false;
            public bool OpenThisFrame = false;
            public bool CloseThisFrame = false;
            public Vector2 Size;
        }
        #endregion
    }
}