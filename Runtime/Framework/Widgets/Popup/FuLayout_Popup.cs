using Fu.Framework;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Defines how a Fugui popup draws its content.
    /// </summary>
    public enum FuPopupRenderMode
    {
        /// <summary>
        /// Run the popup UI callback every frame.
        /// </summary>
        Live,

        /// <summary>
        /// Run the popup UI callback once, then replay the captured draw commands until invalidated.
        /// </summary>
        Frozen
    }

    /// <summary>
    /// Options used when opening a Fugui popup.
    /// </summary>
    public struct FuPopupOptions
    {
        #region State
        public Vector2 Size;
        public FuPopupRenderMode Mode;
        public Action OnClose;
        public bool IsComboBoxPopup;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new popup options instance.
        /// </summary>
        /// <param name="size">Default popup size.</param>
        /// <param name="mode">Popup render mode.</param>
        /// <param name="onClose">Callback invoked when the popup closes.</param>
        /// <param name="isComboBoxPopup">Whether the popup uses combobox constraints and style.</param>
        public FuPopupOptions(Vector2 size, FuPopupRenderMode mode = FuPopupRenderMode.Live, Action onClose = null, bool isComboBoxPopup = false)
        {
            Size = size;
            Mode = mode;
            OnClose = onClose;
            IsComboBoxPopup = isComboBoxPopup;
        }
        #endregion
    }

    /// <summary>
    /// Represents the Fugui type.
    /// </summary>
    public static partial class Fugui
    {
        #region State
        // the index of the current popup
        private static int _currentPopupIndex = 0;
        private static Dictionary<string, FuPopupData> _registeredPopups = new Dictionary<string, FuPopupData>();
        #endregion

        #region Methods
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
            OpenPopUp(id, ui, new FuPopupOptions(Vector2.zero, FuPopupRenderMode.Live, onClose, isComboBoxPopup));
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
            OpenPopUp(id, ui, new FuPopupOptions(size, FuPopupRenderMode.Live, onClose, isComboBoxPopup));
        }

        /// <summary>
        /// Give the order to draw a specific popup.
        /// You must call DrawPopup (each frames) to display the popup.
        /// </summary>
        /// <param name="id">ID of the popup to draw</param>
        /// <param name="ui">UI to draw inside the popup</param>
        /// <param name="options">Popup options.</param>
        public static void OpenPopUp(string id, Action ui, FuPopupOptions options)
        {
            id = GetUniquePopupID(id);
            // remove from dic if already exists
            _registeredPopups.Remove(id);
            // scale size
            Vector2 size = options.Size;
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
                LastFrameRender = UnityEngine.Time.frameCount,
                OpenThisFrame = true,
                CloseThisFrame = false,
                isComboBox = options.IsComboBoxPopup,
                RenderMode = options.Mode,
                FrozenDirty = options.Mode == FuPopupRenderMode.Frozen,
                Size = size,
                UI = ui,
                OnClose = options.OnClose
            };
            _registeredPopups.Add(id, data);
        }

        /// <summary>
        /// Give the order to draw a frozen popup.
        /// You must call DrawPopup (each frames) to display the popup.
        /// </summary>
        /// <param name="id">ID of the popup to draw</param>
        /// <param name="ui">UI to draw once inside the popup</param>
        /// <param name="onClose">callback invoken then the popup close</param>
        /// <param name="isComboBoxPopup">Whether the popup uses combobox constraints and style.</param>
        public static void OpenFrozenPopUp(string id, Action ui, Action onClose = null, bool isComboBoxPopup = false)
        {
            OpenPopUp(id, ui, new FuPopupOptions(Vector2.zero, FuPopupRenderMode.Frozen, onClose, isComboBoxPopup));
        }

        /// <summary>
        /// Give the order to draw a frozen popup.
        /// You must call DrawPopup (each frames) to display the popup.
        /// </summary>
        /// <param name="id">ID of the popup to draw</param>
        /// <param name="ui">UI to draw once inside the popup</param>
        /// <param name="size">default size of the popup</param>
        /// <param name="onClose">callback invoken then the popup close</param>
        /// <param name="isComboBoxPopup">Whether the popup uses combobox constraints and style.</param>
        public static void OpenFrozenPopUp(string id, Action ui, Vector2 size, Action onClose = null, bool isComboBoxPopup = false)
        {
            OpenPopUp(id, ui, new FuPopupOptions(size, FuPopupRenderMode.Frozen, onClose, isComboBoxPopup));
        }

        /// <summary>
        /// Invalidate a frozen popup draw cache so its UI callback runs again on the next draw.
        /// </summary>
        /// <param name="id">ID of the popup to invalidate.</param>
        public static void InvalidatePopup(string id)
        {
            id = GetUniquePopupID(id);
            if (_registeredPopups.TryGetValue(id, out FuPopupData data))
            {
                data.FrozenDirty = true;
                data.FrozenDrawData = null;
            }
        }

        /// <summary>
        /// Invalidate a frozen popup draw cache so its UI callback runs again on the next draw.
        /// </summary>
        /// <param name="id">ID of the popup to refresh.</param>
        public static void RefreshFrozenPopup(string id)
        {
            InvalidatePopup(id);
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

                bool usePopupBackdrop = data.isComboBox && Fugui.ShouldUseThemeBackdrop(FuColors.PopupBg, 0.98f);
                if (data.isComboBox)
                {
                    Fugui.Push(ImGuiCol.PopupBg, Fugui.GetPopupBackdropStyleColor());
                }

                // draw popup
                if (ImGui.BeginPopup(id, ImGuiWindowFlags.AlwaysAutoResize))
                {
                    data.OpenThisFrame = false;
                    data.LastFrameRender = UnityEngine.Time.frameCount;
                    if (usePopupBackdrop)
                    {
                        Fugui.DrawCurrentPopupThemeBackdrop();
                    }

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
                        if (data.RenderMode == FuPopupRenderMode.Frozen && data.FrozenDrawData != null && !data.FrozenDirty)
                        {
                            DrawFrozenPopupData(data);
                        }
                        else
                        {
                            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                            int idxStart = drawList.IdxBuffer.Size;

                            // execute the callback
                            data.UI?.Invoke();

                            if (data.RenderMode == FuPopupRenderMode.Frozen)
                            {
                                CaptureFrozenPopupData(data, drawList, idxStart);
                            }
                        }

                        // update popup value on stack
                        IsPopupDrawing[data.PopupIndex] = false;
                        PopUpRects[data.PopupIndex] = new Rect(ImGui.GetWindowPos(), ImGui.GetWindowSize());
                    }
                    catch (Exception ex)
                    {
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
                if (data.isComboBox)
                {
                    Fugui.PopColor();
                }
                if (data.CloseThisFrame)
                {
                    _closePopup(id);
                }
            }
        }

        /// <summary>
        /// Capture draw commands emitted by a frozen popup UI callback.
        /// </summary>
        /// <param name="data">Popup data to update.</param>
        /// <param name="drawList">Current popup draw list.</param>
        /// <param name="idxStart">First index emitted by the callback.</param>
        private static unsafe void CaptureFrozenPopupData(FuPopupData data, ImDrawListPtr drawList, int idxStart)
        {
            int idxEnd = drawList.IdxBuffer.Size;
            if (idxEnd <= idxStart)
            {
                data.FrozenDrawData = new FuFrozenPopupDrawData(ImGui.GetWindowPos(), ImGui.GetWindowSize());
                data.FrozenDirty = false;
                data.Size = ImGui.GetWindowSize();
                return;
            }

            FuFrozenPopupDrawData frozenData = new FuFrozenPopupDrawData(ImGui.GetWindowPos(), ImGui.GetWindowSize());

            for (int cmdIndex = 0; cmdIndex < drawList.CmdBuffer.Size; cmdIndex++)
            {
                ImDrawCmd cmd = *drawList.CmdBuffer[cmdIndex].NativePtr;
                if (cmd.UserCallback != IntPtr.Zero || cmd.ElemCount == 0)
                {
                    continue;
                }

                int cmdIdxStart = (int)cmd.IdxOffset;
                int cmdIdxEnd = cmdIdxStart + (int)cmd.ElemCount;
                int captureStart = Mathf.Max(cmdIdxStart, idxStart);
                int captureEnd = Mathf.Min(cmdIdxEnd, idxEnd);
                if (captureStart >= captureEnd)
                {
                    continue;
                }

                Dictionary<int, ushort> remap = new Dictionary<int, ushort>();
                List<ImDrawVert> vertices = new List<ImDrawVert>();
                ushort[] indices = new ushort[captureEnd - captureStart];
                bool commandTooLarge = false;

                for (int idx = captureStart; idx < captureEnd; idx++)
                {
                    int originalVertexIndex = (int)cmd.VtxOffset + drawList.IdxBuffer[idx];
                    if (originalVertexIndex < 0 || originalVertexIndex >= drawList.VtxBuffer.Size)
                    {
                        commandTooLarge = true;
                        break;
                    }

                    if (!remap.TryGetValue(originalVertexIndex, out ushort localIndex))
                    {
                        if (vertices.Count >= ushort.MaxValue)
                        {
                            commandTooLarge = true;
                            break;
                        }

                        ImDrawVertPtr vertex = drawList.VtxBuffer[originalVertexIndex];
                        localIndex = (ushort)vertices.Count;
                        remap.Add(originalVertexIndex, localIndex);
                        vertices.Add(new ImDrawVert
                        {
                            pos = vertex.pos,
                            uv = vertex.uv,
                            col = vertex.col
                        });
                    }

                    indices[idx - captureStart] = localIndex;
                }

                if (commandTooLarge)
                {
                    data.FrozenDrawData = null;
                    data.FrozenDirty = true;
                    return;
                }

                frozenData.Commands.Add(new FuFrozenPopupCommand
                {
                    ClipRect = cmd.ClipRect,
                    TextureId = cmd.TextureId,
                    Vertices = vertices.ToArray(),
                    Indices = indices
                });
            }

            data.FrozenDrawData = frozenData;
            data.FrozenDirty = false;
            data.Size = frozenData.Size;
        }

        /// <summary>
        /// Replay a frozen popup draw cache in the current popup window.
        /// </summary>
        /// <param name="data">Popup data to draw.</param>
        private static void DrawFrozenPopupData(FuPopupData data)
        {
            FuFrozenPopupDrawData frozenData = data.FrozenDrawData;
            if (frozenData == null)
            {
                return;
            }

            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 offset = ImGui.GetWindowPos() - frozenData.Position;

            for (int commandIndex = 0; commandIndex < frozenData.Commands.Count; commandIndex++)
            {
                FuFrozenPopupCommand command = frozenData.Commands[commandIndex];
                if (command.Indices == null || command.Vertices == null || command.Indices.Length == 0 || command.Vertices.Length == 0)
                {
                    continue;
                }

                Vector4 clipRect = new Vector4(
                    command.ClipRect.x + offset.x,
                    command.ClipRect.y + offset.y,
                    command.ClipRect.z + offset.x,
                    command.ClipRect.w + offset.y);

                drawList.PushClipRect(new Vector2(clipRect.x, clipRect.y), new Vector2(clipRect.z, clipRect.w));
                drawList.PushTextureID(command.TextureId);
                drawList.PrimReserve(command.Indices.Length, command.Vertices.Length);

                uint vtxBase = drawList._VtxCurrentIdx;
                for (int index = 0; index < command.Indices.Length; index++)
                {
                    drawList.PrimWriteIdx((ushort)(vtxBase + command.Indices[index]));
                }

                for (int vertexIndex = 0; vertexIndex < command.Vertices.Length; vertexIndex++)
                {
                    ImDrawVert vertex = command.Vertices[vertexIndex];
                    drawList.PrimWriteVtx(vertex.pos + offset, vertex.uv, vertex.col);
                }

                drawList.PopTextureID();
                drawList.PopClipRect();
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
                _closePopup(id);
            }
        }

        /// <summary>
        /// Close the popup message
        /// </summary>
        private static void _closePopup(string id)
        {
            ExecuteAfterCurrentRenderContext(() => // defer popup removal after the current context render to avoid multiple click on same frame issues
            {
                // invoke the OnClose callback
                if (_registeredPopups.ContainsKey(id))
                {
                    var data = _registeredPopups[id];

                    // pop popup from stack
                    if (data.PopupIndex >= 0)
                    {
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
                    }

                    // invoke popup's onClose event if there is one
                    _registeredPopups[id].OnClose?.Invoke();
                }
                // remove from dic
                _registeredPopups.Remove(id);
            });
        }

        /// <summary>
        /// Smart clean of the popup stack (must be call on start of each frame)
        /// </summary>
        public static void CleanPopupStack()
        {
            List<string> popupIDs = _registeredPopups.Keys.ToList();
            int lastFrameCount = UnityEngine.Time.frameCount - 1;
            foreach (string popupID in popupIDs)
            {
                if (_registeredPopups[popupID].LastFrameRender < lastFrameCount)
                {
                    _closePopup(popupID);
                }
            }
        }
        #endregion

        #region Nested Types
        /// <summary>
        /// Represents the Fu Popup Data type.
        /// </summary>
        private class FuPopupData
        {
            #region State
            public int LastFrameRender = 0;
            public int PopupIndex = -1;
            public Action UI;
            public Action OnClose;
            public bool isComboBox = false;
            public FuPopupRenderMode RenderMode = FuPopupRenderMode.Live;
            public bool FrozenDirty = false;
            public FuFrozenPopupDrawData FrozenDrawData;
            public bool OpenThisFrame = false;
            public bool CloseThisFrame = false;
            public Vector2 Size;
            #endregion
        }

        /// <summary>
        /// Represents captured draw commands for a frozen popup.
        /// </summary>
        private class FuFrozenPopupDrawData
        {
            #region State
            public Vector2 Position;
            public Vector2 Size;
            public List<FuFrozenPopupCommand> Commands = new List<FuFrozenPopupCommand>();
            #endregion

            #region Constructors
            public FuFrozenPopupDrawData(Vector2 position, Vector2 size)
            {
                Position = position;
                Size = size;
            }
            #endregion
        }

        /// <summary>
        /// Represents a captured ImGui draw command.
        /// </summary>
        private struct FuFrozenPopupCommand
        {
            #region State
            public Vector4 ClipRect;
            public IntPtr TextureId;
            public ImDrawVert[] Vertices;
            public ushort[] Indices;
            #endregion
        }
        #endregion
    }
}
