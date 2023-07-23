using Fu.Framework;
using ImGuiNET;
using System;

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
#if !UIMGUI_REMOVE_IMPLOT
            ImPlotNET.ImPlot.DestroyContext(ImPlotContext);
#endif
#if !UIMGUI_REMOVE_IMNODES
            imnodesNET.imnodes.DestroyContext(ImNodesContext);
#endif
        }

        /// <summary>
        /// End the context render. Don't call it, Fugui layout handle it for you
        /// </summary>
        internal override void EndRender()
        {
            Fugui.IsRendering = false;

            if(!RenderPrepared)
            {
                return;
            }

            // cancel drag drop for this context if left click is up this frame and it's not the first frame of the current drag drop operation
            if (_isDraggingPayload && !_firstFrameDragging && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                CancelDragDrop();
            }
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
#if !UIMGUI_REMOVE_IMGUIZMO
            ImGuizmoNET.ImGuizmo.BeginFrame();
#endif
            RenderPrepared = true;
            return RenderPrepared;
        }
    }
}