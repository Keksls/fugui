using Fu.Framework;
using ImGuiNET;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Represents the Fugui type.
    /// </summary>
    public static partial class Fugui
    {
        #region Methods
        /// <summary>
        /// Draw the Fugui settings panel
        /// </summary>
        public static void DrawSettings(FuWindow window)
        {
            // Title
            string titleText = "FuGui Setting Panel";
            PushFont(18, FontType.Bold);
            HorizontalAlignNextElement(ImGui.CalcTextSize(titleText).x, FuElementAlignement.Center);
            Layout.Text(titleText);
            PopFont();

            // Fugui Logo
            HorizontalAlignNextElement(64f, FuElementAlignement.Center);
            Layout.Image("fLogo", Settings.FuguiLogo, new Vector2(64f, 64f));

            // Settings Panel
            using (FuPanel panel = new FuPanel("fsP", FuStyle.Unpadded, useClipper: true))
            {
                // Settings
                Layout.Collapsable("Settings", () =>
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
        #endregion
    }
}
