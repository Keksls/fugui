using Fugui.Core;
using ImGuiNET;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fugui.Framework
{
    public struct UILayoutStyle : IUIElementStyle
    {
        private UITextStyle _textStyle;
        private UIFrameStyle _frameStyle;
        private UIContainerStyle _containerStyle;
        private Vector2 _framePadding;
        public Vector2 WindowPadding { get; private set; }

        #region Pressets
        // default layout style
        static UILayoutStyle _defaultGridStyle;
        public static UILayoutStyle Default { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _defaultGridStyle; } }

        // unpadded layout style
        static UILayoutStyle _unpaddedGridStyle;
        public static UILayoutStyle Unpadded { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _unpaddedGridStyle; } }

        // overlay layout style
        static UILayoutStyle _overlayGridStyle;
        public static UILayoutStyle Overlay { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _overlayGridStyle; } }

        // overlay layout style
        static UILayoutStyle _noBGOverlayGridStyle;
        public static UILayoutStyle NoBackgroundOverlay { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _noBGOverlayGridStyle; } }
        #endregion

        public void Push(bool enabled)
        {
            _frameStyle.Push(enabled);
            _containerStyle.Push(enabled);
            _textStyle.Push(enabled);
            FuGui.Push(ImGuiStyleVar.FramePadding, _framePadding);
            FuGui.Push(ImGuiStyleVar.WindowPadding, WindowPadding);
        }

        public void Pop()
        {
            FuGui.PopStyle(2);
            _textStyle.Pop();
            _containerStyle.Pop();
            _frameStyle.Pop();
        }

        /// <summary>
        /// Event rised whenever a them is set.
        /// Use this event to set static presset according to UIThemeManager.CurrentTheme
        /// </summary>
        private static void OnThemeSet()
        {
            // default layout style
            _defaultGridStyle = new UILayoutStyle()
            {
                _containerStyle = UIContainerStyle.Default,
                _frameStyle = UIFrameStyle.Default,
                _textStyle = UITextStyle.Default,
                _framePadding = new Vector2(6f, 1f),
                WindowPadding = new Vector2(2f, 2f)
            };

            // unpadded layout style
            _unpaddedGridStyle = new UILayoutStyle()
            {
                _containerStyle = UIContainerStyle.Default,
                _frameStyle = UIFrameStyle.Default,
                _textStyle = UITextStyle.Default,
                _framePadding = new Vector2(6f, 1f),
                WindowPadding = new Vector2(0f, 0f)
            };

            // overlay layout style
            _overlayGridStyle = new UILayoutStyle()
            {
                _containerStyle = UIContainerStyle.Default,
                _frameStyle = UIFrameStyle.Default,
                _textStyle = UITextStyle.Default,
                _framePadding = new Vector2(6f, 4f),
                WindowPadding = new Vector2(8f, 8f)
            };

            // no background overlay style
            _noBGOverlayGridStyle = new UILayoutStyle()
            {
                _containerStyle = UIContainerStyle.Transparent,
                _frameStyle = UIFrameStyle.Default,
                _textStyle = UITextStyle.Default,
                _framePadding = new Vector2(6f, 4f),
                WindowPadding = new Vector2(8f, 8f)
            };
        }
    }
}