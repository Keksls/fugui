using ImGuiNET;
using System;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Represents one temporary Fugui draw-list surface rendered in world space.
    /// </summary>
    public unsafe sealed class FuguiWorldSurface : IDisposable
    {
        #region State
        private readonly FuguiWorld _owner;
        private ImDrawList* _nativeDrawList;
        private FuDrawList _drawList;
        private FuContext _context;
        private FuguiWorldSurfaceDesc _desc;
        private bool _isOpen;
        private int _id;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the draw list used to populate this world surface.
        /// </summary>
        public FuDrawList DrawList => _drawList;

        /// <summary>
        /// Gets the stable surface id used by render-side mesh caches.
        /// </summary>
        internal int ID => _id;

        /// <summary>
        /// Gets the context that owns texture ids used by this surface.
        /// </summary>
        internal FuContext Context => _context;

        /// <summary>
        /// Gets the world-space surface description.
        /// </summary>
        internal FuguiWorldSurfaceDesc Desc => _desc;

        /// <summary>
        /// Gets the native draw list filled by user code.
        /// </summary>
        internal FuDrawList NativeDrawList => _drawList;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a reusable world surface wrapper.
        /// </summary>
        /// <param name="owner">World renderer that owns this surface.</param>
        /// <param name="id">Stable surface id.</param>
        internal FuguiWorldSurface(FuguiWorld owner, int id)
        {
            _owner = owner;
            _id = id;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Opens this surface for the current Fugui frame.
        /// </summary>
        /// <param name="context">Context that owns texture ids.</param>
        /// <param name="desc">Surface description.</param>
        internal void Begin(FuContext context, FuguiWorldSurfaceDesc desc)
        {
            _context = context;
            _desc = desc.Sanitized();
            EnsureNativeDrawList();

            _drawList = new FuDrawList(_nativeDrawList);
            _drawList._ResetForNewFrame();

            // A world surface owns a full local clip rect and starts with the active font atlas texture.
            _drawList.PushClipRect(Vector2.zero, new Vector2(_desc.Resolution.x, _desc.Resolution.y), false);
            _drawList.PushTextureID(context.TextureManager.GetFontAtlasTextureId());
            _isOpen = true;
        }

        /// <summary>
        /// Closes the surface and queues its copied draw data for world rendering.
        /// </summary>
        public void Dispose()
        {
            if (!_isOpen)
            {
                return;
            }

            _isOpen = false;
            _drawList.PopTextureID();
            _drawList.PopClipRect();
            _owner.EndSurface(this);
        }

        /// <summary>
        /// Ensures that this wrapper owns a native ImGui draw list.
        /// </summary>
        private void EnsureNativeDrawList()
        {
            if (_nativeDrawList != null)
            {
                return;
            }

            _nativeDrawList = ImGuiNative.ImDrawList_ImDrawList(ImGui.GetDrawListSharedData());
        }
        #endregion
    }
}
