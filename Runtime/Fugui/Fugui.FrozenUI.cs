using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Fugui helpers for caching static ImGui UI blocks.
    /// </summary>
    public static partial class Fugui
    {
        #region State
        private const int FrozenUICacheCapacity = 1024;
        private static readonly Dictionary<string, FuFrozenUIData> _frozenUICache = new Dictionary<string, FuFrozenUIData>();
        private static readonly Stack<FuFrozenUIContext> _frozenUIStack = new Stack<FuFrozenUIContext>();
        private static readonly List<string> _frozenUICleanupKeys = new List<string>();
        #endregion

        #region Methods
        /// <summary>
        /// Begins a UI block that can be captured and replayed without rerunning its callback.
        /// </summary>
        /// <param name="id">Unique frozen UI block ID.</param>
        /// <param name="nbFrameBeforeCache">Number of live frames to draw before replaying the cache.</param>
        /// <param name="autoInvalidateAfterInvisibleFrames">Number of invisible frames before the cache is removed.</param>
        /// <returns>True when the caller must draw the live UI block, false when the cached draw output was replayed.</returns>
        public static bool BeginFrozenUI(string id, int nbFrameBeforeCache = 3, int autoInvalidateAfterInvisibleFrames = 1)
        {
            if (string.IsNullOrEmpty(id))
            {
                _frozenUIStack.Push(new FuFrozenUIContext(null, false, false, default, 0, Vector2.zero, Vector2.zero, Vector2.zero, true));
                return true;
            }

            nbFrameBeforeCache = Mathf.Max(1, nbFrameBeforeCache);
            autoInvalidateAfterInvisibleFrames = Mathf.Max(1, autoInvalidateAfterInvisibleFrames);
            FuFrozenUIData data = GetOrCreateFrozenUIData(id, autoInvalidateAfterInvisibleFrames, true);
            FuDrawList drawList = Fugui.GetCurrentWindowDrawList();
            Vector2 windowPosition = ImGui.GetWindowPos();

            if (data.DrawFrameCount >= nbFrameBeforeCache)
            {
                ReplayFrozenUIData(data, drawList, windowPosition);
                _frozenUIStack.Push(new FuFrozenUIContext(data, false, true, drawList, 0, Vector2.zero, windowPosition, Vector2.zero, true));
                return false;
            }

            _frozenUIStack.Push(new FuFrozenUIContext(
                data,
                true,
                false,
                drawList,
                drawList.IdxBuffer.Size,
                ImGui.GetCursorScreenPos(),
                windowPosition,
                ImGui.GetWindowSize(),
                true));
            return true;
        }

        /// <summary>
        /// Begins a frozen block that captures an explicit raw draw list in screen space.
        /// </summary>
        /// <param name="id">Unique frozen UI block ID.</param>
        /// <param name="drawList">Raw draw list that receives both live and replayed geometry.</param>
        /// <param name="nbFrameBeforeCache">Number of live frames to draw before replaying the cache.</param>
        /// <param name="autoInvalidateAfterInvisibleFrames">Number of invisible frames before the cache is removed.</param>
        /// <returns>True when the caller must draw the live block, false when cached geometry was replayed.</returns>
        public static bool BeginFrozenUI(string id, FuDrawList drawList, int nbFrameBeforeCache = 3, int autoInvalidateAfterInvisibleFrames = 1)
        {
            return BeginFrozenUI(id, drawList, Vector2.zero, nbFrameBeforeCache, autoInvalidateAfterInvisibleFrames);
        }

        /// <summary>
        /// Begins a frozen block that captures an explicit raw draw list relative to a caller-defined origin.
        /// </summary>
        /// <param name="id">Unique frozen UI block ID.</param>
        /// <param name="drawList">Raw draw list that receives both live and replayed geometry.</param>
        /// <param name="origin">Current screen-space origin used to offset replayed geometry.</param>
        /// <param name="nbFrameBeforeCache">Number of live frames to draw before replaying the cache.</param>
        /// <param name="autoInvalidateAfterInvisibleFrames">Number of invisible frames before the cache is removed.</param>
        /// <returns>True when the caller must draw the live block, false when cached geometry was replayed.</returns>
        public static bool BeginFrozenUI(string id, FuDrawList drawList, Vector2 origin, int nbFrameBeforeCache = 3, int autoInvalidateAfterInvisibleFrames = 1)
        {
            if (!drawList.IsValid)
            {
                throw new ArgumentException("Frozen raw UI requires a valid draw list.", nameof(drawList));
            }

            if (string.IsNullOrEmpty(id))
            {
                _frozenUIStack.Push(new FuFrozenUIContext(null, false, false, drawList, 0, Vector2.zero, origin, Vector2.zero, false));
                return true;
            }

            nbFrameBeforeCache = Mathf.Max(1, nbFrameBeforeCache);
            autoInvalidateAfterInvisibleFrames = Mathf.Max(1, autoInvalidateAfterInvisibleFrames);
            FuFrozenUIData data = GetOrCreateFrozenUIData(id, autoInvalidateAfterInvisibleFrames, false);

            if (data.DrawFrameCount >= nbFrameBeforeCache)
            {
                ReplayFrozenUIData(data, drawList, origin);
                _frozenUIStack.Push(new FuFrozenUIContext(data, false, true, drawList, 0, Vector2.zero, origin, Vector2.zero, false));
                return false;
            }

            _frozenUIStack.Push(new FuFrozenUIContext(data, true, false, drawList, drawList.IdxBuffer.Size, origin, origin, Vector2.zero, false));
            return true;
        }

        /// <summary>
        /// Ends the current frozen UI block.
        /// </summary>
        public static void EndFrozenUI()
        {
            if (_frozenUIStack.Count == 0)
            {
                return;
            }

            FuFrozenUIContext context = _frozenUIStack.Pop();
            if (context.Replayed)
            {
                if (context.UsesWindowLayout && (context.Data.ContentSize.x > 0f || context.Data.ContentSize.y > 0f))
                {
                    ImGui.Dummy(context.Data.ContentSize);
                }
                return;
            }

            if (!context.Capturing)
            {
                return;
            }

            CaptureFrozenUIData(
                context.Data,
                context.DrawList,
                context.StartIndex,
                context.StartCursorScreenPos,
                context.CaptureOrigin,
                context.CaptureSize,
                context.UsesWindowLayout);
            context.Data.DrawFrameCount++;
        }

        /// <summary>
        /// Draws a callback inside a frozen UI block.
        /// </summary>
        /// <param name="id">Unique frozen UI block ID.</param>
        /// <param name="callback">UI callback to draw while the block is warming up.</param>
        /// <param name="nbFrameBeforeCache">Number of live frames to draw before replaying the cache.</param>
        /// <param name="autoInvalidateAfterInvisibleFrames">Number of invisible frames before the cache is removed.</param>
        public static void FrozenUI(string id, Action callback, int nbFrameBeforeCache = 3, int autoInvalidateAfterInvisibleFrames = 1)
        {
            bool isBegun = false;
            try
            {
                bool shouldDraw = BeginFrozenUI(id, nbFrameBeforeCache, autoInvalidateAfterInvisibleFrames);
                isBegun = true;
                if (shouldDraw)
                {
                    callback?.Invoke();
                }
            }
            finally
            {
                if (isBegun)
                {
                    EndFrozenUI();
                }
            }
        }

        /// <summary>
        /// Draws a callback inside a frozen block targeting an explicit raw draw list.
        /// </summary>
        /// <param name="id">Unique frozen UI block ID.</param>
        /// <param name="drawList">Raw draw list that receives both live and replayed geometry.</param>
        /// <param name="callback">UI callback to draw while the block is warming up.</param>
        /// <param name="nbFrameBeforeCache">Number of live frames to draw before replaying the cache.</param>
        /// <param name="autoInvalidateAfterInvisibleFrames">Number of invisible frames before the cache is removed.</param>
        public static void FrozenUI(string id, FuDrawList drawList, Action callback, int nbFrameBeforeCache = 3, int autoInvalidateAfterInvisibleFrames = 1)
        {
            FrozenUI(id, drawList, Vector2.zero, callback, nbFrameBeforeCache, autoInvalidateAfterInvisibleFrames);
        }

        /// <summary>
        /// Draws a callback inside a frozen block targeting an explicit raw draw list and origin.
        /// </summary>
        /// <param name="id">Unique frozen UI block ID.</param>
        /// <param name="drawList">Raw draw list that receives both live and replayed geometry.</param>
        /// <param name="origin">Current screen-space origin used to offset replayed geometry.</param>
        /// <param name="callback">UI callback to draw while the block is warming up.</param>
        /// <param name="nbFrameBeforeCache">Number of live frames to draw before replaying the cache.</param>
        /// <param name="autoInvalidateAfterInvisibleFrames">Number of invisible frames before the cache is removed.</param>
        public static void FrozenUI(string id, FuDrawList drawList, Vector2 origin, Action callback, int nbFrameBeforeCache = 3, int autoInvalidateAfterInvisibleFrames = 1)
        {
            bool isBegun = false;
            try
            {
                bool shouldDraw = BeginFrozenUI(id, drawList, origin, nbFrameBeforeCache, autoInvalidateAfterInvisibleFrames);
                isBegun = true;
                if (shouldDraw)
                {
                    callback?.Invoke();
                }
            }
            finally
            {
                if (isBegun)
                {
                    EndFrozenUI();
                }
            }
        }

        /// <summary>
        /// Invalidates one frozen UI cache entry.
        /// </summary>
        /// <param name="id">Unique frozen UI block ID.</param>
        public static void InvalidateFrozenUI(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                _frozenUICache.Remove(id);
            }
        }

        /// <summary>
        /// Clears every frozen UI cache entry.
        /// </summary>
        public static void ClearFrozenUICache()
        {
            // Frozen geometry and cleanup scratch data belong to one runtime session.
            _frozenUICache.Clear();
            _frozenUIStack.Clear();
            _frozenUICleanupKeys.Clear();
        }

        /// <summary>
        /// Gets the cached window size for a ready frozen UI block.
        /// </summary>
        /// <param name="id">Unique frozen UI block ID.</param>
        /// <param name="size">Cached window size.</param>
        /// <param name="nbFrameBeforeCache">Number of live frames required before the cache is considered ready.</param>
        /// <returns>True when the cache exists and is ready to replay.</returns>
        public static bool TryGetFrozenUISize(string id, out Vector2 size, int nbFrameBeforeCache = 3)
        {
            size = Vector2.zero;
            nbFrameBeforeCache = Mathf.Max(1, nbFrameBeforeCache);

            if (string.IsNullOrEmpty(id) ||
                !_frozenUICache.TryGetValue(id, out FuFrozenUIData data) ||
                data.DrawFrameCount < nbFrameBeforeCache)
            {
                return false;
            }

            size = data.Size;
            return size.x > 0f || size.y > 0f;
        }

        /// <summary>
        /// Removes frozen UI entries that were not submitted this frame.
        /// </summary>
        internal static void CleanFrozenUICache()
        {
            if (_frozenUICache.Count == 0)
            {
                return;
            }

            int frame = UnityEngine.Time.frameCount;
            _frozenUICleanupKeys.Clear();
            foreach (KeyValuePair<string, FuFrozenUIData> pair in _frozenUICache)
            {
                FuFrozenUIData data = pair.Value;
                if (frame - data.LastSubmittedFrame >= data.AutoInvalidateAfterInvisibleFrames)
                {
                    _frozenUICleanupKeys.Add(pair.Key);
                }
            }

            for (int i = 0; i < _frozenUICleanupKeys.Count; i++)
            {
                _frozenUICache.Remove(_frozenUICleanupKeys[i]);
            }
            _frozenUICleanupKeys.Clear();
        }

        /// <summary>
        /// Removes the frozen UI entry that has gone unused for the longest time.
        /// </summary>
        private static void EvictLeastRecentlySubmittedFrozenUI()
        {
            // A linear scan is allocation-free and runs only when a new key reaches the hard limit.
            string oldestId = null;
            int oldestFrame = int.MaxValue;
            foreach (KeyValuePair<string, FuFrozenUIData> pair in _frozenUICache)
            {
                if (pair.Value.LastSubmittedFrame < oldestFrame)
                {
                    oldestId = pair.Key;
                    oldestFrame = pair.Value.LastSubmittedFrame;
                }
            }

            if (oldestId != null)
            {
                _frozenUICache.Remove(oldestId);
            }
        }

        /// <summary>
        /// Returns a cache entry configured for the requested frozen target kind.
        /// </summary>
        /// <param name="id">Unique frozen UI block ID.</param>
        /// <param name="autoInvalidateAfterInvisibleFrames">Number of invisible frames before eviction.</param>
        /// <param name="usesWindowLayout">Whether the entry belongs to a regular ImGui window layout.</param>
        /// <returns>Prepared frozen UI cache entry.</returns>
        private static FuFrozenUIData GetOrCreateFrozenUIData(string id, int autoInvalidateAfterInvisibleFrames, bool usesWindowLayout)
        {
            if (!_frozenUICache.TryGetValue(id, out FuFrozenUIData data))
            {
                if (_frozenUICache.Count >= FrozenUICacheCapacity)
                {
                    EvictLeastRecentlySubmittedFrozenUI();
                }

                data = new FuFrozenUIData
                {
                    UsesWindowLayout = usesWindowLayout
                };
                _frozenUICache[id] = data;
            }
            else if (data.UsesWindowLayout != usesWindowLayout)
            {
                // One ID cannot safely alternate between window-relative and raw screen-space geometry.
                data.Commands.Clear();
                data.DrawFrameCount = 0;
                data.UsesWindowLayout = usesWindowLayout;
            }

            data.LastSubmittedFrame = UnityEngine.Time.frameCount;
            data.AutoInvalidateAfterInvisibleFrames = autoInvalidateAfterInvisibleFrames;
            return data;
        }

        /// <summary>
        /// Capture draw commands emitted by a frozen UI block.
        /// </summary>
        /// <param name="data">Frozen UI data to update.</param>
        /// <param name="drawList">Current draw list.</param>
        /// <param name="idxStart">First index emitted by the block.</param>
        /// <param name="startCursorScreenPos">Cursor or raw origin position before the block drew.</param>
        /// <param name="captureOrigin">Screen-space origin used to make captured vertices relative.</param>
        /// <param name="captureSize">Window size for layout-aware blocks.</param>
        /// <param name="usesWindowLayout">Whether ImGui cursor layout must be preserved.</param>
        private static unsafe void CaptureFrozenUIData(
            FuFrozenUIData data,
            FuDrawList drawList,
            int idxStart,
            Vector2 startCursorScreenPos,
            Vector2 captureOrigin,
            Vector2 captureSize,
            bool usesWindowLayout)
        {
            data.Position = captureOrigin;
            data.Size = captureSize;
            data.ContentSize = usesWindowLayout ? ImGui.GetCursorScreenPos() - startCursorScreenPos : Vector2.zero;
            data.Commands.Clear();
            bool hasDrawnBounds = false;
            Vector2 drawnContentMin = Vector2.zero;
            Vector2 drawnContentSize = Vector2.zero;
#if FU_CUSTOM_MATERIALS_ENABLED
            FuCustomDrawMaterialState customMaterialState = default;
#endif

            int idxEnd = drawList.IdxBuffer.Size;
            if (idxEnd <= idxStart)
            {
                return;
            }

            for (int cmdIndex = 0; cmdIndex < drawList.CmdBuffer.Size; cmdIndex++)
            {
                ImDrawCmd cmd = *drawList.CmdBuffer[cmdIndex].NativePtr;
#if FU_CUSTOM_MATERIALS_ENABLED
                if (cmd.UserCallback != IntPtr.Zero)
                {
                    customMaterialState.TryHandleCommand(cmd);
                    continue;
                }

                if (cmd.ElemCount == 0)
                {
                    continue;
                }
#else
                if (cmd.UserCallback != IntPtr.Zero || cmd.ElemCount == 0)
                {
                    continue;
                }
#endif

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
                bool commandInvalid = false;

                for (int idx = captureStart; idx < captureEnd; idx++)
                {
                    int originalVertexIndex = (int)cmd.VtxOffset + drawList.IdxBuffer[idx];
                    if (originalVertexIndex < 0 || originalVertexIndex >= drawList.VtxBuffer.Size)
                    {
                        commandInvalid = true;
                        break;
                    }

                    if (!remap.TryGetValue(originalVertexIndex, out ushort localIndex))
                    {
                        if (vertices.Count >= ushort.MaxValue)
                        {
                            commandInvalid = true;
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

                if (commandInvalid)
                {
                    continue;
                }

                ImDrawVert[] commandVertices = vertices.ToArray();
                for (int vertexIndex = 0; vertexIndex < commandVertices.Length; vertexIndex++)
                {
                    Vector2 relativePosition = commandVertices[vertexIndex].pos - startCursorScreenPos;
                    if (!hasDrawnBounds)
                    {
                        drawnContentMin = relativePosition;
                        drawnContentSize = relativePosition;
                        hasDrawnBounds = true;
                    }
                    else
                    {
                        drawnContentMin.x = Mathf.Min(drawnContentMin.x, relativePosition.x);
                        drawnContentMin.y = Mathf.Min(drawnContentMin.y, relativePosition.y);
                        drawnContentSize.x = Mathf.Max(drawnContentSize.x, relativePosition.x);
                        drawnContentSize.y = Mathf.Max(drawnContentSize.y, relativePosition.y);
                    }
                }

                data.Commands.Add(new FuFrozenUICommand
                {
                    ClipRect = cmd.ClipRect,
                    TextureId = cmd.TextureId,
#if FU_CUSTOM_MATERIALS_ENABLED
                    MaterialBindingId = customMaterialState.ActiveBindingId,
#endif
                    Vertices = commandVertices,
                    Indices = indices
                });
            }

            if (hasDrawnBounds)
            {
                if (!usesWindowLayout)
                {
                    data.Size = new Vector2(
                        Mathf.Max(0f, drawnContentSize.x - drawnContentMin.x),
                        Mathf.Max(0f, drawnContentSize.y - drawnContentMin.y));
                }

                data.ContentSize = new Vector2(
                    Mathf.Max(data.ContentSize.x, drawnContentSize.x),
                    Mathf.Max(data.ContentSize.y, drawnContentSize.y));
            }
            data.ContentSize = new Vector2(Mathf.Max(0f, data.ContentSize.x), Mathf.Max(0f, data.ContentSize.y));
        }

        /// <summary>
        /// Replay captured draw commands for a frozen UI block.
        /// </summary>
        /// <param name="data">Captured frozen UI data.</param>
        /// <param name="drawList">Target draw list that receives replayed geometry.</param>
        /// <param name="currentOrigin">Current screen-space origin for the target block.</param>
        private static void ReplayFrozenUIData(FuFrozenUIData data, FuDrawList drawList, Vector2 currentOrigin)
        {
            Vector2 offset = currentOrigin - data.Position;

            for (int commandIndex = 0; commandIndex < data.Commands.Count; commandIndex++)
            {
                FuFrozenUICommand command = data.Commands[commandIndex];
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
#if FU_CUSTOM_MATERIALS_ENABLED
                bool materialPushed = false;
#endif
                try
                {
#if FU_CUSTOM_MATERIALS_ENABLED
                    if (command.MaterialBindingId != IntPtr.Zero)
                    {
                        drawList.PushMaterialBinding(command.MaterialBindingId);
                        materialPushed = true;
                    }
#endif
                    drawList.PushTextureID(command.TextureId);
                    try
                    {
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
                    }
                    finally
                    {
                        drawList.PopTextureID();
                    }
                }
                finally
                {
#if FU_CUSTOM_MATERIALS_ENABLED
                    if (materialPushed)
                    {
                        drawList.PopMaterialBinding();
                    }
#endif
                    drawList.PopClipRect();
                }
            }
        }
        #endregion

        #region Nested Types
        private class FuFrozenUIData
        {
            #region State
            public Vector2 Position;
            public Vector2 Size;
            public Vector2 ContentSize;
            public int DrawFrameCount;
            public int LastSubmittedFrame;
            public int AutoInvalidateAfterInvisibleFrames = 1;
            public bool UsesWindowLayout;
            public readonly List<FuFrozenUICommand> Commands = new List<FuFrozenUICommand>();
            #endregion
        }

        private struct FuFrozenUIContext
        {
            #region State
            public readonly FuFrozenUIData Data;
            public readonly bool Capturing;
            public readonly bool Replayed;
            public readonly FuDrawList DrawList;
            public readonly int StartIndex;
            public readonly Vector2 StartCursorScreenPos;
            public readonly Vector2 CaptureOrigin;
            public readonly Vector2 CaptureSize;
            public readonly bool UsesWindowLayout;
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes one active frozen capture or replay scope.
            /// </summary>
            /// <param name="data">Cache entry used by the scope.</param>
            /// <param name="capturing">Whether the scope is capturing live geometry.</param>
            /// <param name="replayed">Whether the scope already replayed cached geometry.</param>
            /// <param name="drawList">Exact draw list targeted by the scope.</param>
            /// <param name="startIndex">First index emitted by the live block.</param>
            /// <param name="startCursorScreenPos">Cursor or raw origin at capture start.</param>
            /// <param name="captureOrigin">Screen-space origin used for replay offsets.</param>
            /// <param name="captureSize">Window size used by layout-aware blocks.</param>
            /// <param name="usesWindowLayout">Whether the scope preserves ImGui window layout.</param>
            public FuFrozenUIContext(
                FuFrozenUIData data,
                bool capturing,
                bool replayed,
                FuDrawList drawList,
                int startIndex,
                Vector2 startCursorScreenPos,
                Vector2 captureOrigin,
                Vector2 captureSize,
                bool usesWindowLayout)
            {
                Data = data;
                Capturing = capturing;
                Replayed = replayed;
                DrawList = drawList;
                StartIndex = startIndex;
                StartCursorScreenPos = startCursorScreenPos;
                CaptureOrigin = captureOrigin;
                CaptureSize = captureSize;
                UsesWindowLayout = usesWindowLayout;
            }
            #endregion
        }

        private struct FuFrozenUICommand
        {
            #region State
            public Vector4 ClipRect;
            public IntPtr TextureId;
#if FU_CUSTOM_MATERIALS_ENABLED
            public IntPtr MaterialBindingId;
#endif
            public ImDrawVert[] Vertices;
            public ushort[] Indices;
            #endregion
        }
        #endregion
    }
}
