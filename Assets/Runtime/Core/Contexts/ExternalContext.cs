using Fugui.Framework;
using ImGuiNET;
using System;

namespace Fugui.Core
{
    public class ExternalContext : FuguiContext
    {
        public ExternalContext(int index, Action onInitialize = null) : base(index, onInitialize)
        {
            initialize(index, onInitialize);
        }

        protected override void sub_initialize()
        {
            LoadFonts();
            ThemeManager.SetTheme(ThemeManager.CurrentTheme);
            SetDefaultImGuiIniFilePath(null);
        }

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

        internal override void EndRender()
        {
        }

        internal override bool PrepareRender()
        {
            FuGui.SetCurrentContext(this);
            if (!TryExecuteOnPrepareEvent())
            {
                return false;
            }
            ImGui.NewFrame();
#if !UIMGUI_REMOVE_IMGUIZMO
            ImGuizmoNET.ImGuizmo.BeginFrame();
#endif
            renderPrepared = true;
            return renderPrepared;
        }
    }
}