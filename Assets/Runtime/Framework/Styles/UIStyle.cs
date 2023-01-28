using Fugui.Core;
using ImGuiNET;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fugui.Framework
{
    public struct UIStyle : IUIElementStyle
    {
        private UITextStyle _textStyle;
        private UIFrameStyle _frameStyle;
        private UIPanelStyle _containerStyle;
        private Vector2 _framePadding;
        public Vector2 WindowPadding { get; private set; }

        #region Pressets
        // default layout style
        static UIStyle _defaultGridStyle;
        public static UIStyle Default { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _defaultGridStyle; } }

        // modal layout style
        static UIStyle _modalStyle;
        public static UIStyle Modal { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _modalStyle; } }

        // unpadded layout style
        static UIStyle _unpaddedGridStyle;
        public static UIStyle Unpadded { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _unpaddedGridStyle; } }

        // overlay layout style
        static UIStyle _overlayGridStyle;
        public static UIStyle Overlay { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _overlayGridStyle; } }

        // overlay layout style
        static UIStyle _noBGOverlayGridStyle;
        public static UIStyle NoBackgroundOverlay { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _noBGOverlayGridStyle; } }
        #endregion

        public void Push(bool enabled)
        {
            _frameStyle.Push(enabled);
            _containerStyle.Push(enabled);
            _textStyle.Push(enabled);
            FuGui.Push(ImGuiStyleVar.FramePadding, _framePadding * FuGui.CurrentContext.Scale);
            FuGui.Push(ImGuiStyleVar.WindowPadding, WindowPadding * FuGui.CurrentContext.Scale);
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
            _defaultGridStyle = new UIStyle()
            {
                _containerStyle = UIPanelStyle.Default,
                _frameStyle = UIFrameStyle.Default,
                _textStyle = UITextStyle.Default,
                _framePadding = new Vector2(6f, 1f),
                WindowPadding = new Vector2(2f, 2f)
            };

            // unpadded layout style
            _unpaddedGridStyle = new UIStyle()
            {
                _containerStyle = UIPanelStyle.Default,
                _frameStyle = UIFrameStyle.Default,
                _textStyle = UITextStyle.Default,
                _framePadding = new Vector2(6f, 1f),
                WindowPadding = new Vector2(0f, 0f)
            };

            // overlay layout style
            _overlayGridStyle = new UIStyle()
            {
                _containerStyle = UIPanelStyle.Default,
                _frameStyle = UIFrameStyle.Default,
                _textStyle = UITextStyle.Default,
                _framePadding = new Vector2(6f, 4f),
                WindowPadding = new Vector2(8f, 8f)
            };

            // no background overlay style
            _noBGOverlayGridStyle = new UIStyle()
            {
                _containerStyle = UIPanelStyle.Transparent,
                _frameStyle = UIFrameStyle.Default,
                _textStyle = UITextStyle.Default,
                _framePadding = new Vector2(6f, 4f),
                WindowPadding = new Vector2(8f, 8f)
            };

            // _modal style
            _modalStyle = new UIStyle()
            {
                _containerStyle = UIPanelStyle.Transparent,
                _frameStyle = UIFrameStyle.Default,
                _textStyle = UITextStyle.Default,
                _framePadding = new Vector2(6f, 1f),
                WindowPadding = new Vector2(0f, 0f)
            };
        }
    }
}