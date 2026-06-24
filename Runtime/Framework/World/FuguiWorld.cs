using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Provides draw-list based Fugui rendering on Unity world-space surfaces.
    /// </summary>
    public sealed class FuguiWorld
    {
        #region State
        private readonly List<FuguiWorldDrawItem> _items = new List<FuguiWorldDrawItem>();
        private readonly List<FuguiWorldSurface> _surfacePool = new List<FuguiWorldSurface>();
        private readonly Stack<DrawList> _drawListPool = new Stack<DrawList>();
        private readonly HashSet<Camera> _renderCameras = new HashSet<Camera>();
        private int _surfacePoolCursor;
        private int _lastFrame = -1;
        private int _nextSurfaceId = 1;
        #endregion

        #region Methods
        /// <summary>
        /// Begins a Fugui draw-list surface rendered directly in world space.
        /// </summary>
        /// <param name="desc">Surface description.</param>
        /// <returns>The opened world surface.</returns>
        public FuguiWorldSurface Surface(FuguiWorldSurfaceDesc desc)
        {
            BeginFrameIfNeeded();
            FuContext context = Fugui.CurrentContext;
            if (context == null || !context.RenderPrepared)
            {
                throw new InvalidOperationException("Fugui.World.Surface must be called while a Fugui context is rendering.");
            }

            FuguiWorldSurface surface = GetSurfaceFromPool();
            surface.Begin(context, desc);
            return surface;
        }

        /// <summary>
        /// Opens a surface, invokes a draw callback, and closes the surface automatically.
        /// </summary>
        /// <param name="desc">Surface description.</param>
        /// <param name="draw">Draw callback that receives the surface draw list.</param>
        public void Surface(FuguiWorldSurfaceDesc desc, Action<FuDrawList> draw)
        {
            using (FuguiWorldSurface surface = Surface(desc))
            {
                draw?.Invoke(surface.DrawList);
            }
        }

        /// <summary>
        /// Registers a camera allowed to render Fugui world-space surfaces.
        /// </summary>
        /// <param name="camera">Camera allowed to render world surfaces.</param>
        public void RegisterRenderCamera(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            _renderCameras.Add(camera);
        }

        /// <summary>
        /// Unregisters a camera from Fugui world-space rendering.
        /// </summary>
        /// <param name="camera">Camera to remove from world rendering.</param>
        public void UnregisterRenderCamera(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            _renderCameras.Remove(camera);
        }

        /// <summary>
        /// Returns whether a camera is explicitly registered for Fugui world-space rendering.
        /// </summary>
        /// <param name="camera">Camera to test.</param>
        /// <returns>True when the camera is registered.</returns>
        public bool IsRenderCameraRegistered(Camera camera)
        {
            return camera != null && _renderCameras.Contains(camera);
        }

        /// <summary>
        /// Returns whether any world item was submitted for the current Unity frame.
        /// </summary>
        /// <returns>True when the current frame has world items.</returns>
        internal bool HasCurrentFrameItems()
        {
            BeginFrameIfNeeded();
            return _items.Count > 0;
        }

        /// <summary>
        /// Gets the current frame world draw items.
        /// </summary>
        /// <returns>The current frame draw items.</returns>
        internal IReadOnlyList<FuguiWorldDrawItem> GetCurrentFrameItems()
        {
            BeginFrameIfNeeded();
            return _items;
        }

        /// <summary>
        /// Returns whether the render feature should render world surfaces for a camera.
        /// </summary>
        /// <param name="camera">Camera currently rendered by URP.</param>
        /// <returns>True when the camera is registered for world rendering.</returns>
        internal bool ShouldRenderCamera(Camera camera)
        {
            return IsRenderCameraRegistered(camera);
        }

        /// <summary>
        /// Ends a world surface and stores a copied draw list for render-side consumption.
        /// </summary>
        /// <param name="surface">Surface to end.</param>
        internal void EndSurface(FuguiWorldSurface surface)
        {
            if (surface == null || surface.Context == null)
            {
                return;
            }

            DrawList drawList = GetDrawListFromPool();
            drawList.Bind(surface.NativeDrawList);
            if (drawList.VtxCount > 0 && drawList.IdxCount > 0 && drawList.CmdCount > 0)
            {
                _items.Add(new FuguiWorldDrawItem(
                    surface.ID,
                    surface.Desc,
                    drawList,
                    surface.Context.TextureManager,
                    UnityEngine.Time.frameCount));
            }
            else
            {
                _drawListPool.Push(drawList);
            }
        }

        /// <summary>
        /// Clears old frame data and makes reusable objects available again.
        /// </summary>
        private void BeginFrameIfNeeded()
        {
            int frame = UnityEngine.Time.frameCount;
            if (_lastFrame == frame)
            {
                return;
            }

            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].DrawList != null)
                {
                    _drawListPool.Push(_items[i].DrawList);
                }
            }

            _items.Clear();
            _surfacePoolCursor = 0;
            _lastFrame = frame;
        }

        /// <summary>
        /// Gets a reusable surface wrapper for the current frame.
        /// </summary>
        /// <returns>The reusable surface wrapper.</returns>
        private FuguiWorldSurface GetSurfaceFromPool()
        {
            if (_surfacePoolCursor < _surfacePool.Count)
            {
                return _surfacePool[_surfacePoolCursor++];
            }

            FuguiWorldSurface surface = new FuguiWorldSurface(this, _nextSurfaceId++);
            _surfacePool.Add(surface);
            _surfacePoolCursor++;
            return surface;
        }

        /// <summary>
        /// Gets a reusable managed draw-list copy.
        /// </summary>
        /// <returns>The managed draw-list copy.</returns>
        private DrawList GetDrawListFromPool()
        {
            return _drawListPool.Count > 0 ? _drawListPool.Pop() : new DrawList();
        }
        #endregion
    }

    /// <summary>
    /// Immutable render item produced by a Fugui world surface.
    /// </summary>
    internal readonly struct FuguiWorldDrawItem
    {
        #region State
        /// <summary>
        /// Stable surface id used by mesh caches.
        /// </summary>
        internal readonly int SurfaceId;

        /// <summary>
        /// Surface description.
        /// </summary>
        internal readonly FuguiWorldSurfaceDesc Desc;

        /// <summary>
        /// Copied draw-list data.
        /// </summary>
        internal readonly DrawList DrawList;

        /// <summary>
        /// Texture manager that owns draw command texture ids.
        /// </summary>
        internal readonly TextureManager TextureManager;

        /// <summary>
        /// Unity frame when this item was submitted.
        /// </summary>
        internal readonly int Frame;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a world draw item.
        /// </summary>
        /// <param name="surfaceId">Stable surface id.</param>
        /// <param name="desc">Surface description.</param>
        /// <param name="drawList">Copied draw-list data.</param>
        /// <param name="textureManager">Texture manager that owns command texture ids.</param>
        /// <param name="frame">Unity frame when the item was submitted.</param>
        internal FuguiWorldDrawItem(int surfaceId, FuguiWorldSurfaceDesc desc, DrawList drawList, TextureManager textureManager, int frame)
        {
            SurfaceId = surfaceId;
            Desc = desc;
            DrawList = drawList;
            TextureManager = textureManager;
            Frame = frame;
        }
        #endregion
    }
}
