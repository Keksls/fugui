using Fu.Framework;
using ImGuiNET;
using System;
using System.Linq;
using UnityEngine;

namespace Fu.Core
{
    /// <summary>
    /// class that represent an External window Fugui Context.
    /// Must be used to render external window Container
    /// </summary>
    public class FuExternalContext : FuContext
    {
        public FuExternalContext(int index, float scale, float fontScale, Action onInitialize = null) : base(index, scale, fontScale, onInitialize)
        {
            initialize(onInitialize);
        }

        /// <summary>
        /// Initialize this context for specific sub class. Don't call it, Fugui layout handle it for you
        /// </summary>
        protected override void sub_initialize()
        {
            LoadFonts();
            FuThemeManager.SetTheme(FuThemeManager.CurrentTheme);
            SetDefaultImGuiIniFilePath(null);
        }

        /// <summary>
        /// Destroy this context. Don't call it, Fugui layout handle it for you
        /// </summary>
        internal override void Destroy()
        {
            ImGui.DestroyContext(ImGuiContext);
        }

        /// <summary>
        /// End the context render. Don't call it, Fugui layout handle it for you
        /// </summary>
        internal override bool EndRender()
        {
            Fugui.IsRendering = false;

            if (!RenderPrepared)
                return false;

            // cancel drag drop for this context if left click is up this frame and it's not the first frame of the current drag drop operation
            if (_isDraggingPayload && !_firstFrameDragging && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                CancelDragDrop();
            }

            return true;
        }

        /// <summary>
        /// Prepare render for next frame. Don't call it, Fugui layout handle it for you
        /// </summary>
        internal override bool PrepareRender()
        {
            Fugui.IsRendering = true;
            Fugui.SetCurrentContext(this);
            if (!TryExecuteOnPrepareEvent())
            {
                return false;
            }
            ImGui.NewFrame();
            RenderPrepared = true;
            return RenderPrepared;
        }

        /// <summary>
        /// Set the scale of this context
        /// </summary>
        /// <param name="scale">global context scale</param>
        /// <param name="fontScale">context font scale (usualy same value as context scale)</param>
        public override void SetScale(float scale, float fontScale)
        {
            float oldScale = Scale;
            // set scale
            Scale = scale;
            FontScale = fontScale;

            // update font scale
            LoadFonts();
            FuThemeManager.SetTheme(FuThemeManager.CurrentTheme);

            // scale windows sizes for windows NOT docked, visible and in this context
            Fugui.UIWindows.Where(win => win.Value.Container.Context == this && win.Value.IsVisible && !win.Value.IsDocked).ToList()
                .ForEach((win) =>
                {
                    win.Value.Size = new Vector2Int((int)(win.Value.Size.x * (scale / oldScale)), (int)(win.Value.Size.y * (scale / oldScale)));
                });
        }
    }
}