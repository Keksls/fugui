using Fu.Core;
using ImGuiNET;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fu.Framework
{
    public struct FuStyle : IFuElementStyle
    {
        private FuTextStyle _textStyle;
        private FuFrameStyle _frameStyle;
        private FuPanelStyle _containerStyle;
        private Vector2 _framePadding;
        public Vector2 WindowPadding { get; private set; }

        #region Pressets
        // default layout style
        static FuStyle _defaultGridStyle;
        public static FuStyle Default { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _defaultGridStyle; } }

        // no BG layout style
        static FuStyle _noBGGridStyle;
        public static FuStyle NoBackground { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _noBGGridStyle; } }

        // no BG unpadded layout style
        static FuStyle _noBGUnpaddedGridStyle;
        public static FuStyle NoBackgroundUnpadded { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _noBGUnpaddedGridStyle; } }

        // modal layout style
        static FuStyle _modalStyle;
        public static FuStyle Modal { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _modalStyle; } }

        // unpadded layout style
        static FuStyle _unpaddedGridStyle;
        public static FuStyle Unpadded { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _unpaddedGridStyle; } }

        // overlay layout style
        static FuStyle _overlayGridStyle;
        public static FuStyle Overlay { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _overlayGridStyle; } }

        // overlay layout style
        static FuStyle _noBGOverlayGridStyle;
        public static FuStyle NoBackgroundOverlay { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _noBGOverlayGridStyle; } }
        #endregion

        public void Push(bool enabled)
        {
            _frameStyle.Push(enabled);
            _containerStyle.Push(enabled);
            _textStyle.Push(enabled);
            Fugui.Push(ImGuiStyleVar.FramePadding, _framePadding * Fugui.CurrentContext.Scale);
            Fugui.Push(ImGuiStyleVar.WindowPadding, WindowPadding * Fugui.CurrentContext.Scale);
        }

        public void Pop()
        {
            Fugui.PopStyle(2);
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
            _defaultGridStyle = new FuStyle()
            {
                _containerStyle = FuPanelStyle.Default,
                _frameStyle = FuFrameStyle.Default,
                _textStyle = FuTextStyle.Default,
                _framePadding = new Vector2(6f, 1f),
                WindowPadding = new Vector2(2f, 2f)
            };

            // no background layout style
            _noBGGridStyle = new FuStyle()
            {
                _containerStyle = FuPanelStyle.Transparent,
                _frameStyle = FuFrameStyle.Default,
                _textStyle = FuTextStyle.Default,
                _framePadding = new Vector2(6f, 1f),
                WindowPadding = new Vector2(2f, 2f)
            };

            // no background unpadded layout style
            _noBGUnpaddedGridStyle = new FuStyle()
            {
                _containerStyle = FuPanelStyle.Transparent,
                _frameStyle = FuFrameStyle.Default,
                _textStyle = FuTextStyle.Default,
                _framePadding = new Vector2(6f, 1f),
                WindowPadding = new Vector2(0f, 0f)
            };

            // unpadded layout style
            _unpaddedGridStyle = new FuStyle()
            {
                _containerStyle = FuPanelStyle.Default,
                _frameStyle = FuFrameStyle.Default,
                _textStyle = FuTextStyle.Default,
                _framePadding = new Vector2(6f, 1f),
                WindowPadding = new Vector2(0f, 0f)
            };

            // overlay layout style
            _overlayGridStyle = new FuStyle()
            {
                _containerStyle = FuPanelStyle.Default,
                _frameStyle = FuFrameStyle.Default,
                _textStyle = FuTextStyle.Default,
                _framePadding = new Vector2(6f, 4f),
                WindowPadding = new Vector2(8f, 8f)
            };

            // no background overlay style
            _noBGOverlayGridStyle = new FuStyle()
            {
                _containerStyle = FuPanelStyle.Transparent,
                _frameStyle = FuFrameStyle.Default,
                _textStyle = FuTextStyle.Default,
                _framePadding = new Vector2(6f, 4f),
                WindowPadding = new Vector2(8f, 8f)
            };

            // _modal style
            _modalStyle = new FuStyle()
            {
                _containerStyle = FuPanelStyle.Transparent,
                _frameStyle = FuFrameStyle.Default,
                _textStyle = FuTextStyle.Default,
                _framePadding = new Vector2(6f, 1f),
                WindowPadding = new Vector2(0f, 0f)
            };
        }
    }
}