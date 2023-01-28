using UnityEngine;
using System.Runtime.CompilerServices;
using ImGuiNET;

namespace Fugui.Framework
{
    public struct UIElementSize
    {
        private Vector2 _size;

        #region Presset
        static UIElementSize _autoSize = new UIElementSize(Vector2.zero);
        public static UIElementSize AutoSize { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _autoSize; } }
        static UIElementSize _fullSize = new UIElementSize(new Vector2(-1f, 0f));
        public static UIElementSize FullSize { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _fullSize; } }
        #endregion

        public UIElementSize(Vector2 size)
        {
            _size = size;
        }

        /// <summary>
        /// Get calculated size according to given size
        /// </summary>
        /// <returns>calculated size</returns>
        public Vector2 GetSize()
        {
            Vector2 size = _size;
            if (_size.x == -1)
            {
                size.x = ImGui.GetContentRegionAvail().x;
            }
            else if (_size.x > 0)
            {
                _size.x *= FuGui.CurrentContext.Scale;
            }
            if (_size.y > 0)
            {
                _size.y *= FuGui.CurrentContext.Scale;
            }
            return size;
        }

        /// <summary>
        /// Implcite operator to auto cast UIElementSize to Vector2
        /// </summary>
        public static implicit operator Vector2(UIElementSize size) => size.GetSize();
        /// <summary>
        /// Implcite operator to auto cast Vector2 to UIElementSize
        /// </summary>
        public static implicit operator UIElementSize(Vector2 size) => new UIElementSize(size);
    }
}