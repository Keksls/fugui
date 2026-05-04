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
        public static void DrawSettings(FuWindow window, FuLayout layout)
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

            // Settings Panel
            using (FuPanel panel = new FuPanel("fsP", FuStyle.Unpadded, useClipper: true))
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
                DrawThemes(layout);
            }
        }
        #endregion
    }
}
