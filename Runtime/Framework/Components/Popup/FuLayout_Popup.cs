using Fu.Core;
using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework
{
    public partial class FuLayout
    {
        private class FuPopupData
        {
            public Action UI;
            public Action OnClose;
            public bool OpenThisFrame = false;
            public bool CloseThisFrame = false;
            public Vector2 Size;
        }
        private static Dictionary<string, FuPopupData> _registeredPopups = new Dictionary<string, FuPopupData>();

        /// <summary>
        /// Get the PopupID unique by current drawing window
        /// </summary>
        /// <param name="ID">ID to unify</param>
        /// <returns>Unique ID</returns>
        private string getUniqueID(string ID)
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
        public void ForceCloseOpenPopup()
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
        public void OpenPopUp(string id, Action ui, Action onClose = null)
        {
            OpenPopUp(id, ui, Vector2.zero, onClose);
        }

        /// <summary>
        /// Give the order to draw a specific popup.
        /// You must call DrawPopup (each frames) to display the popup
        /// </summary>
        /// <param name="id">ID of the popup to draw</param>
        /// <param name="ui">UI to draw inside the popup</param>
        /// <param name="size">default size of the popup</param>
        /// <param name="onClose">callback invoken then the popup close</param>
        public void OpenPopUp(string id, Action ui, Vector2 size, Action onClose = null)
        {
            id = getUniqueID(id);
            // remove from dic if already exists
            _registeredPopups.Remove(id);
            // scale size
            if (size.x > 0)
            {
                size.x *= Fugui.CurrentContext.Scale;
            }
            if (size.y > 0)
            {
                size.y *= Fugui.CurrentContext.Scale;
            }
            // add to dic
            FuPopupData data = new FuPopupData()
            {
                OpenThisFrame = true,
                CloseThisFrame = false,
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
        public bool IsPopupOpen(string id)
        {
            return _registeredPopups.ContainsKey(getUniqueID(id));
        }

        /// <summary>
        /// Draw a registered popup (will draw only after you call OpenPopup
        /// </summary>
        /// <param name="id">ID of the popup to draw</param>
        public void DrawPopup(string id)
        {
            DrawPopup(id, Vector2.zero, Vector2.zero);
        }

        /// <summary>
        /// Draw a registered popup (will draw only after you call OpenPopup
        /// </summary>
        /// <param name="id">ID of the popup to draw</param>
        /// <param name="size">size of the popup</param>
        /// <param name="pos">position of the popup</param>
        public void DrawPopup(string id, Vector2 size, Vector2 pos)
        {
            id = getUniqueID(id);
            if (_registeredPopups.TryGetValue(id, out FuPopupData data))
            {
                // open popup if needed
                if (data.OpenThisFrame)
                {
                    ImGui.OpenPopup(id);
                }

                // scale size
                if (size.x > 0)
                {
                    size.x *= Fugui.CurrentContext.Scale;
                }
                if (size.y > 0)
                {
                    size.y *= Fugui.CurrentContext.Scale;
                }

                // keep size to force popup size
                if (size.x > 0 || size.y > 0)
                {
                    data.Size = size;
                }

                // set size
                ImGui.SetNextWindowSize(data.Size);
                if (pos.x != 0f || pos.y != 0f)
                {
                    ImGui.SetNextWindowPos(pos);
                }

                // draw popup
                if (ImGui.BeginPopupContextWindow(id))
                {
                    data.OpenThisFrame = false;
                    IsInsidePopUp = true;
                    // execute the callback
                    data.UI?.Invoke();
                    // Set the IsInsidePopUp flag to false
                    IsInsidePopUp = false;

                    // Check if the CurrentPopUpID is not equal to the given text
                    if (CurrentPopUpID != id)
                    {
                        // Set the CurrentPopUpWindowID to the current drawing window ID
                        CurrentPopUpWindowID = FuWindow.CurrentDrawingWindow?.ID;
                        // Set the CurrentPopUpID to the given text
                        CurrentPopUpID = id;
                    }
                    // Set CurrentPopUpRect to ImGui item rect
                    CurrentPopUpRect = new Rect(ImGui.GetWindowPos(), ImGui.GetWindowSize());
                    ImGui.EndPopup();
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
        /// Close the popup message
        /// </summary>
        public void ClosePopup(string id)
        {
            id = getUniqueID(id);
            if (_registeredPopups.TryGetValue(id, out FuPopupData data))
            {
                data.CloseThisFrame = true;
            }
        }

        private void _closePopup(string id)
        {
            // Check if the CurrentPopUpID is not equal to the given text
            if (CurrentPopUpID != id)
            {
                // Set the CurrentPopUpWindowID to the current drawing window ID
                CurrentPopUpWindowID = FuWindow.CurrentDrawingWindow?.ID;
                // Set the CurrentPopUpID to the given text
                CurrentPopUpID = id;
            }

            // Set the IsInsidePopUp flag to false
            IsInsidePopUp = false;
            // Check if the CurrentPopUpID is equal to the given text
            if (CurrentPopUpID == id)
            {
                // Set the CurrentPopUpWindowID to null
                CurrentPopUpWindowID = null;
                // Set the CurrentPopUpID to null
                CurrentPopUpID = null;
            }
            // invoke the OnClose callback
            if (_registeredPopups.ContainsKey(id))
            {
                _registeredPopups[id].OnClose?.Invoke();
            }
            // remove from dic
            _registeredPopups.Remove(id);
            // clear popup Rect
            CurrentPopUpRect = default;
        }
    }
}