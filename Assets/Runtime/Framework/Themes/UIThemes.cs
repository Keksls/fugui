using System.Linq;
using UnityEngine;

namespace Fugui.Framework
{
    public static partial class FuGui
    {
        public static void DrawThemes()
        {
            using (new UIPanel("themeManagerContainer", UIStyle.Unpadded))
            {
                using (UILayout layout = new UILayout())
                {
                    layout.Collapsable("Theme Managment", () =>
                    {
                        using (UIGrid grid = new UIGrid("themeManagmentGrid"))
                        {
                            grid.Combobox("Current theme", ThemeManager.Themes.Values.ToList(), (theme) =>
                            {
                                ThemeManager.SetTheme(theme);
                            }, () => { return ThemeManager.CurrentTheme; });
                        }
                        using (UIGrid grid = new UIGrid("themeManagmentActions", UIGridDefinition.DefaultRatio, cellPadding: 0f))
                        {
                            if (grid.Button("Save"))
                            {
                                ThemeManager.SaveTheme(ThemeManager.CurrentTheme);
                                ThemeManager.LoadAllThemes();
                                ThemeManager.SetTheme(ThemeManager.CurrentTheme);
                            }
                            if (grid.Button("New"))
                            {
                                // TODO
                            }
                        }
                    });

                    layout.Collapsable("Theme Variables", () =>
                    {
                        using (UIGrid grid = new UIGrid("FuguiThemeVariablesGrid", UIGridFlag.LinesBackground | UIGridFlag.AutoToolTipsOnLabels))
                        {
                            if (grid.DrawObject<FuguiTheme>(ThemeManager.CurrentTheme))
                            {
                                ThemeManager.SetTheme(ThemeManager.CurrentTheme);
                                ForceDrawAllWindows();
                            }
                        }
                    });

                    layout.Collapsable("Theme Colors", () =>
                    {
                        using (UIGrid grid = new UIGrid("FuguiThemeColorGrid", new UIGridDefinition(2, new int[] { 196 }), UIGridFlag.AutoToolTipsOnLabels | UIGridFlag.LinesBackground, 4f))
                        {
                            for (int i = 0; i < (int)FuguiColors.COUNT; i++)
                            {
                                Vector4 selectedColor = ThemeManager.CurrentTheme.Colors[i];
                                string colorName = ((FuguiColors)i).ToString();
                                colorName = AddSpacesBeforeUppercase(colorName);
                                if (grid.ColorPicker(colorName, ref selectedColor))
                                {
                                    ThemeManager.CurrentTheme.Colors[i] = selectedColor;
                                    ThemeManager.SetTheme(ThemeManager.CurrentTheme);
                                    ForceDrawAllWindows();
                                }
                            }
                        }
                    });
                }
            }
        }
    }
}