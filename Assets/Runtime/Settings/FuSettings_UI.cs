using ImGuiNET;
using UnityEngine;

namespace Fu.Framework
{
    public static partial class Fugui
    {
        /// <summary>
        /// Draw the Fugui settings panel
        /// </summary>
        public static void DrawSettings()
        {
            using (FuLayout layout = new FuLayout())
            {
                // Title
                string titleText = "FuGui Setting Panel";
                PushFont(18, FontType.Bold);
                HorizontalAlignNextElement(ImGui.CalcTextSize(titleText).x, FuElementAlignement.Center);
                layout.Text(titleText);
                PopFont();

                // Fugui Logo
                HorizontalAlignNextElement(128f, FuElementAlignement.Center);
                layout.Image("FuguiLogo", Settings.FuguiLogo, new Vector2(128f, 128f));

                // buttons
                FuStyle.Unpadded.Push(true);
                using (FuGrid grid = new FuGrid("fuguiSettingActionGrid", new FuGridDefinition(1, new float[] { 1f / 1f })))
                {
                    if (grid.Button("Docking Layout", FuButtonStyle.Highlight))
                    {
                        FuDockingLayoutManager.SetConfigurationLayout();
                    }
                }
                FuStyle.Unpadded.Pop();

                // Settings Panel
                using (FuPanel panel = new FuPanel("FuguiSettingsPanel", FuStyle.Unpadded))
                {
                    // Settings
                    layout.Collapsable("Settings", () =>
                    {
                        using (FuGrid grid = new FuGrid("FuguiSettingGrid", FuGridFlag.AutoToolTipsOnLabels))
                        {
                            grid.DrawObject(Settings);
                        }
                    });

                    // Themes
                    DrawThemes();
                }
            }
        }
    }
}