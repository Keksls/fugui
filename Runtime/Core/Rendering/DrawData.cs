using ImGuiNET;
using System.Collections.Generic;
using UnityEngine;

namespace Fu
{
        /// <summary>
        /// Class that represent all DrawList for a frame
        /// </summary>
        internal class DrawData
        {
            #region State
            public List<DrawList> DrawLists;
            internal List<DrawDataRenderItem> RenderItems;
            public int TotalVtxCount;
            public int TotalIdxCount;
            public Vector2 DisplayPos;
            public Vector2 DisplaySize;
            public Vector2 FramebufferScale;
            public int CmdListsCount;
            private readonly List<DrawList> _transientDrawListPool;
            private int _transientDrawListPoolCursor;
            private DrawListMesh _drawListOnlyMesh;
            private const int MinimumRetainedTransientDrawLists = 8;

            internal bool IsDrawListOnly { get; private set; }
            internal DrawListMesh DrawListOnlyMesh { get { return _drawListOnlyMesh; } }
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Draw Data class.
            /// </summary>
            public DrawData()
            {
                DrawLists = new List<DrawList>();
                RenderItems = new List<DrawDataRenderItem>();
                _transientDrawListPool = new List<DrawList>();
                Clear();
            }
            #endregion

            #region Methods
            /// <summary>
            /// Clear all Draw Lists
            /// </summary>
            public void Clear()
            {
                // Retain the recent working set and release pinned buffers left by older spikes.
                TrimTransientDrawListPool(Mathf.Max(MinimumRetainedTransientDrawLists, _transientDrawListPoolCursor));
                DrawLists.Clear();
                RenderItems.Clear();
                _transientDrawListPoolCursor = 0;
                IsDrawListOnly = false;
                TotalVtxCount = 0;
                TotalIdxCount = 0;
                CmdListsCount = 0;
            }

            /// <summary>
            /// Releases transient draw-list copies above the recent frame working set.
            /// </summary>
            /// <param name="retainedCount">Number of reusable draw-list copies to retain.</param>
            private void TrimTransientDrawListPool(int retainedCount)
            {
                // Tail removal preserves the hot slots reused from index zero on every frame.
                for (int i = _transientDrawListPool.Count - 1; i >= retainedCount; i--)
                {
                    _transientDrawListPool[i].Dispose();
                    _transientDrawListPool.RemoveAt(i);
                }

                int excessiveCapacityThreshold = retainedCount <= int.MaxValue / 4
                    ? Mathf.Max(32, retainedCount * 4)
                    : int.MaxValue;
                if (_transientDrawListPool.Capacity > excessiveCapacityThreshold)
                {
                    _transientDrawListPool.Capacity = Mathf.Max(8, retainedCount);
                }
            }

            /// <summary>
            /// Add some Draw Lists
            /// </summary>
            /// <param name="dLists">Draw Lists to Add</param>
            public void AddDrawLists(IEnumerable<DrawList> dLists)
            {
                foreach (DrawList drawList in dLists)
                {
                    AddDrawList(drawList);
                }
            }

            /// <summary>
            /// Add a DrawList
            /// </summary>
            /// <param name="dList">DrawList to Add</param>
            public void AddDrawList(DrawList dList)
            {
                AddTransientDrawList(dList);
            }

            /// <summary>
            /// Adds cached draw data owned by a Fugui window.
            /// </summary>
            /// <param name="window">Window that owns the cached draw lists and render mesh.</param>
            internal void AddWindowDrawData(FuWindow window)
            {
                if (window == null || window.CachedDrawLists.Count == 0)
                {
                    return;
                }

                RenderItems.Add(DrawDataRenderItem.ForWindow(window));
                foreach (DrawList drawList in window.CachedDrawLists)
                {
                    AddDrawListCounters(drawList);
                }
            }

            /// <summary>
            /// Adds a non-window draw list that still has to be meshed by the renderer.
            /// </summary>
            /// <param name="drawList">Draw list to add.</param>
            internal void AddTransientDrawList(DrawList drawList)
            {
                if (drawList == null)
                {
                    return;
                }

                RenderItems.Add(DrawDataRenderItem.ForDrawList(drawList));
                AddDrawListCounters(drawList);
            }

            /// <summary>
            /// Reuses a transient draw list slot and binds it to a native ImGui draw list.
            /// </summary>
            /// <param name="drawList">Native ImGui draw list.</param>
            /// <returns>Bound transient draw list.</returns>
            internal DrawList AddTransientDrawList(FuDrawList drawList)
            {
                DrawList transientDrawList = BindTransientDrawList(drawList);
                AddTransientDrawList(transientDrawList);
                return transientDrawList;
            }

            /// <summary>
            /// Binds native ImGui draw data to the optimized raw draw-list cache.
            /// </summary>
            /// <param name="imDrawData">Native draw data produced by the current context.</param>
            /// <param name="contextId">Context identifier used to name the persistent mesh.</param>
            internal void BindDrawListOnly(ImDrawDataPtr imDrawData, int contextId)
            {
                Clear();
                IsDrawListOnly = true;

                for (int i = 0; i < imDrawData.CmdListsCount; i++)
                {
                    DrawList drawList = BindTransientDrawList(Fugui.ToFuDrawList(imDrawData.CmdLists[i]));
                    AddDrawListCounters(drawList);
                }

                FramebufferScale = imDrawData.FramebufferScale;
                DisplayPos = imDrawData.DisplayPos;
                DisplaySize = imDrawData.DisplaySize;

                if (_drawListOnlyMesh == null)
                {
                    _drawListOnlyMesh = new DrawListMesh("FuguiDrawListOnlyMesh_" + contextId);
                }

                // Upload once while publishing; render passes only reuse this persistent GPU mesh.
                _drawListOnlyMesh.Update(DrawLists, DisplaySize, FramebufferScale);
            }

            /// <summary>
            /// Reuses one retained transient slot and copies a native ImGui draw list into it.
            /// </summary>
            /// <param name="drawList">Native draw list to retain after the ImGui frame ends.</param>
            /// <returns>Retained draw-list copy.</returns>
            private DrawList BindTransientDrawList(FuDrawList drawList)
            {
                DrawList transientDrawList;
                if (_transientDrawListPoolCursor < _transientDrawListPool.Count)
                {
                    transientDrawList = _transientDrawListPool[_transientDrawListPoolCursor];
                }
                else
                {
                    transientDrawList = new DrawList();
                    _transientDrawListPool.Add(transientDrawList);
                }

                _transientDrawListPoolCursor++;
                transientDrawList.Bind(drawList);
                return transientDrawList;
            }

            /// <summary>
            /// Adds a draw list to aggregate counters without creating a render item.
            /// </summary>
            /// <param name="drawList">Draw list to count.</param>
            private void AddDrawListCounters(DrawList drawList)
            {
                DrawLists.Add(drawList);
                CmdListsCount++;
                TotalVtxCount += drawList.VtxCount;
                TotalIdxCount += drawList.IdxCount;
            }

            /// <summary>
            /// Bind this drawData from ImGui drawData Ptr
            /// </summary>
            /// <param name="imDrawData">ImDrawDataPtr for this frame</param>
            public void Bind(ImDrawDataPtr imDrawData)
            {
                Clear();
                for (int i = 0; i < imDrawData.CmdListsCount; i++)
                {
                    AddTransientDrawList(Fugui.ToFuDrawList(imDrawData.CmdLists[i]));
                }
                FramebufferScale = imDrawData.FramebufferScale;
                DisplayPos = imDrawData.DisplayPos;
                DisplaySize = imDrawData.DisplaySize;
            }

            /// <summary>
            /// Releases every pinned transient draw-list buffer owned by this context.
            /// </summary>
            internal void Dispose()
            {
                // Window draw lists are owned by their FuWindow; only the transient pool belongs here.
                for (int i = 0; i < _transientDrawListPool.Count; i++)
                {
                    _transientDrawListPool[i].Dispose();
                }

                _transientDrawListPool.Clear();
                _drawListOnlyMesh?.Destroy();
                _drawListOnlyMesh = null;
                Clear();
            }
            #endregion
        }

        /// <summary>
        /// Render item for either a cached Fugui window or a transient ImGui draw list.
        /// </summary>
        internal struct DrawDataRenderItem
        {
            #region State
            public FuWindow Window { get; private set; }
            public DrawList DrawList { get; private set; }
            public bool IsWindow { get { return Window != null; } }
            #endregion

            #region Constructors
            private DrawDataRenderItem(FuWindow window, DrawList drawList)
            {
                Window = window;
                DrawList = drawList;
            }
            #endregion

            #region Methods
            public static DrawDataRenderItem ForWindow(FuWindow window)
            {
                return new DrawDataRenderItem(window, null);
            }

            public static DrawDataRenderItem ForDrawList(DrawList drawList)
            {
                return new DrawDataRenderItem(null, drawList);
            }
            #endregion
        }

}
