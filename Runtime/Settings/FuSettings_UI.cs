using Fu.Core;
using Fu.Framework;
using ImGuiNET;
using System.Collections.Generic;
using UnityEngine;

namespace Fu
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
                HorizontalAlignNextElement(64f, FuElementAlignement.Center);
                layout.Image("fLogo", Settings.FuguiLogo, new Vector2(64f, 64f));

                // buttons
                FuStyle.Unpadded.Push(true);
                using (FuGrid grid = new FuGrid("fsAG", new FuGridDefinition(2, new float[] { 1f / 2f })))
                {
                    if (grid.Button("Docking Layout", FuButtonStyle.Highlight))
                    {
                        FuDockingLayoutManager.SetConfigurationLayout();
                    }
                    if (grid.Button("Font Helper", FuButtonStyle.Highlight))
                    {
                        // set custom layout
                        var dld = new FuDockingLayoutDefinition();
                        dld.Proportion = 0f;
                        dld.Orientation = UIDockSpaceOrientation.None;
                        dld.Children = new List<FuDockingLayoutDefinition>();
                        dld.WindowsDefinition = new List<ushort>();
                        dld.WindowsDefinition.Add(FuSystemWindowsNames.FontHelper.ID);
                        FuDockingLayoutManager.SetLayout(dld, "");
                    }
                }
                FuStyle.Unpadded.Pop();

                // Settings Panel
                using (FuPanel panel = new FuPanel("fsP", FuStyle.Unpadded))
                {
                    // Settings
                    layout.Collapsable("Settings", () =>
                    {
                        using (FuGrid grid = new FuGrid("fsG", FuGridFlag.AutoToolTipsOnLabels))
                        {
                            grid.DrawObject("FuguiSettings", Settings);
                        }
                    });

                    // Themes
                    DrawThemes();
                }
            }
        }
    }
}