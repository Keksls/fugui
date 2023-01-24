using ImGuiNET;
using UnityEngine;

namespace Fugui.Framework
{
    public static partial class FuGui
    {
        public static void DrawSettings()
        {
            using (UILayout layout = new UILayout())
            {
                string titleText = "FuGui Setting Panel";
                PushFont(18, FontType.Bold);
                HorizontalAlignNextElement(ImGui.CalcTextSize(titleText).x, ElementAlignement.Center);
                layout.Text(titleText);
                PopFont();

                HorizontalAlignNextElement(128f, ElementAlignement.Center);
                layout.Image("FuguiLogo", Settings.FuguiLogo, new Vector2(128f, 128f));

                UIStyle.Unpadded.Push(true);
                using (UIGrid grid = new UIGrid("fuguiSettingActionGrid", new UIGridDefinition(1, new float[] { 1f / 1f })))
                {
                    if(grid.Button("Docking Layout", UIButtonStyle.Highlight))
                    {
                        DockingLayoutManager.SetLayout(UIDockingLayout.DockSpaceConfiguration);
                    }
                }
                UIStyle.Unpadded.Pop();

                using (UIPanel panel = new UIPanel("FuguiSettingsPanel", UIStyle.Unpadded))
                {
                    layout.Collapsable("Settings", () =>
                    {
                        using (UIGrid grid = new UIGrid("FuguiSettingGrid", UIGridFlag.AutoToolTipsOnLabels))
                        {
                            grid.DrawObject(Settings);
                        }
                    });

                    DrawThemes();
                }
            }
        }
    }
}