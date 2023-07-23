using ImGuiNET;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fu.Framework
{
    public struct FuElementSize
    {
        private Vector2 _size;
        public Vector2 BrutSize => _size;

        #region Presset
        static FuElementSize _autoSize = new FuElementSize(Vector2.zero);
        public static FuElementSize AutoSize { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _autoSize; } }
        static FuElementSize _fullSize = new FuElementSize(new Vector2(-1f, 0f));
        public static FuElementSize FullSize { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _fullSize; } }
        #endregion

        public FuElementSize(Vector2 size)
        {
            _size = size;
        }

        public FuElementSize(float x, float y)
        {
            _size = new Vector2(x, y);
        }

        /// <summary>
        /// Get calculated size according to given size
        /// </summary>
        /// <returns>calculated size</returns>
        public Vector2 GetSize()
        {
            Vector2 size = _size;
            
            if (_size.x < 0)
            {
                size.x = ImGui.GetContentRegionAvail().x;
            }
            else if (_size.x > 0)
            {
                size.x *= Fugui.CurrentContext.Scale;
            }
            if (_size.y > 0)
            {
                size.y *= Fugui.CurrentContext.Scale;
            }
            return size;
        }

        /// <summary>
        /// Implcite operator to auto cast UIElementSize to Vector2
        /// </summary>
        public static implicit operator Vector2(FuElementSize size) => size.GetSize();
        /// <summary>
        /// Implcite operator to auto cast Vector2 to UIElementSize
        /// </summary>
        public static implicit operator FuElementSize(Vector2 size) => new FuElementSize(size);
    }
}