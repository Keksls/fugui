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
        /// Give the order to draw a specific popup.
        /// You must call DrawPopup (each frames) to display the popup
        /// </summary>
        /// <param name="id">ID of the popup to draw</param>
        /// <param name="ui">UI to draw inside the popup</param>
        /// <param name="size">default size of the popup</param>
        /// <param name="onClose">callback invoken then the popup close</param>
        public void OpenPopUp(string id, Action ui, Vector2 size, Action onClose = null)
        {
            // remove from dic if already exists
            _registeredPopups.Remove(id);
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
        /// Draw a registered popup (will draw only after you call OpenPopup
        /// </summary>
        /// <param name="id">ID of the popup to draw</param>
        public void DrawPopup(string id)
        {
            DrawPopup(id, Vector2.zero);
        }

        /// <summary>
        /// Draw a registered popup (will draw only after you call OpenPopup
        /// </summary>
        /// <param name="id">ID of the popup to draw</param>
        /// <param name="size">size of the popup</param>
        public void DrawPopup(string id, Vector2 size)
        {
            if (_registeredPopups.TryGetValue(id, out FuPopupData data))
            {
                if (data.OpenThisFrame)
                {
                    ImGui.OpenPopup(id);
                }
                if (size.x > 0 || size.y > 0)
                {
                    data.Size = size;
                }

                ImGui.SetNextWindowSize(data.Size);
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
            if(_registeredPopups.ContainsKey(id))
            {
                _registeredPopups[id].OnClose?.Invoke();
            }
            // remove from dic
            _registeredPopups.Remove(id);
        }
    }
}