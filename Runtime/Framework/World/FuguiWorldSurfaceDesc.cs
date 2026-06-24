using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Describes a Fugui draw-list surface rendered directly in Unity world space.
    /// </summary>
    public struct FuguiWorldSurfaceDesc
    {
        #region State
        /// <summary>
        /// World position of the surface pivot.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// World rotation of the surface.
        /// </summary>
        public Quaternion Rotation;

        /// <summary>
        /// World scale applied to the surface mesh.
        /// </summary>
        public Vector3 Scale;

        /// <summary>
        /// Local world-unit size of the surface before scale is applied.
        /// </summary>
        public Vector2 Size;

        /// <summary>
        /// Draw-list coordinate space size in Fugui pixels.
        /// </summary>
        public Vector2Int Resolution;

        /// <summary>
        /// Local pivot used as the transform origin.
        /// </summary>
        public FuguiWorldPivot Pivot;

        /// <summary>
        /// Depth behavior used by the world render pass.
        /// </summary>
        public FuguiWorldDepthMode DepthMode;

        /// <summary>
        /// Unity layer used for camera culling-mask filtering.
        /// </summary>
        public int Layer;

        /// <summary>
        /// Optional stable order used before drawing surfaces with the same depth mode.
        /// </summary>
        public int SortingOrder;
        #endregion

        #region Properties
        /// <summary>
        /// Returns a default centered surface description.
        /// </summary>
        public static FuguiWorldSurfaceDesc Default
        {
            get
            {
                return new FuguiWorldSurfaceDesc
                {
                    Position = Vector3.zero,
                    Rotation = Quaternion.identity,
                    Scale = Vector3.one,
                    Size = Vector2.one,
                    Resolution = new Vector2Int(256, 256),
                    Pivot = FuguiWorldPivot.Center,
                    DepthMode = FuguiWorldDepthMode.Test,
                    Layer = 0,
                    SortingOrder = 0
                };
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Returns a sanitized copy that is safe for mesh generation.
        /// </summary>
        /// <returns>The sanitized surface description.</returns>
        internal FuguiWorldSurfaceDesc Sanitized()
        {
            FuguiWorldSurfaceDesc desc = this;
            if (desc.Rotation.x == 0f && desc.Rotation.y == 0f && desc.Rotation.z == 0f && desc.Rotation.w == 0f)
            {
                desc.Rotation = Quaternion.identity;
            }

            if (desc.Scale == Vector3.zero)
            {
                desc.Scale = Vector3.one;
            }

            desc.Size = new Vector2(
                Mathf.Max(0.0001f, Mathf.Abs(desc.Size.x)),
                Mathf.Max(0.0001f, Mathf.Abs(desc.Size.y)));
            desc.Resolution = new Vector2Int(
                Mathf.Max(1, desc.Resolution.x),
                Mathf.Max(1, desc.Resolution.y));
            desc.Layer = Mathf.Clamp(desc.Layer, 0, 31);
            return desc;
        }

        /// <summary>
        /// Returns the local-to-world matrix used to draw the surface mesh.
        /// </summary>
        /// <returns>The local-to-world matrix.</returns>
        internal Matrix4x4 GetLocalToWorldMatrix()
        {
            return Matrix4x4.TRS(Position, Rotation, Scale);
        }

        /// <summary>
        /// Returns the normalized pivot location measured from the top-left surface corner.
        /// </summary>
        /// <returns>The normalized pivot location.</returns>
        internal Vector2 GetNormalizedPivot()
        {
            switch (Pivot)
            {
                case FuguiWorldPivot.TopLeft:
                    return new Vector2(0f, 0f);
                case FuguiWorldPivot.TopCenter:
                    return new Vector2(0.5f, 0f);
                case FuguiWorldPivot.TopRight:
                    return new Vector2(1f, 0f);
                case FuguiWorldPivot.MiddleLeft:
                    return new Vector2(0f, 0.5f);
                case FuguiWorldPivot.MiddleRight:
                    return new Vector2(1f, 0.5f);
                case FuguiWorldPivot.BottomLeft:
                    return new Vector2(0f, 1f);
                case FuguiWorldPivot.BottomCenter:
                    return new Vector2(0.5f, 1f);
                case FuguiWorldPivot.BottomRight:
                    return new Vector2(1f, 1f);
                case FuguiWorldPivot.Center:
                default:
                    return new Vector2(0.5f, 0.5f);
            }
        }
        #endregion
    }
}
