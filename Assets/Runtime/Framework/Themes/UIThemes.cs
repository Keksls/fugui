using System.Linq;
using UnityEngine;

namespace Fugui.Framework
{
    public static partial class FuGui
    {
        private static string _newThemeName = string.Empty;

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
                        using (UIGrid grid = new UIGrid("themeManagmentActions", new UIGridDefinition(3, new float[] { 1f / 3f, 1f / 3f, 1f / 3f }), cellPadding: 0f))
                        {
                            // save theme
                            if (grid.Button("Save"))
                            {
                                ThemeManager.SaveTheme(ThemeManager.CurrentTheme);
                                ThemeManager.LoadAllThemes();
                                ThemeManager.SetTheme(ThemeManager.CurrentTheme);
                            }
                            // create new theme
                            if (grid.Button("New"))
                            {
                                _newThemeName = string.Empty;
                                ShowModal("Create new Theme", () =>
                                {
                                    using (UIGrid grid = new UIGrid("newThemeGrid"))
                                    {
                                        grid.TextInput("Theme Name", "new theme", ref _newThemeName);
                                    }
                                }, UIModalSize.Medium,
                                new UIModalButton("OK", () =>
                                {
                                    FuguiTheme theme = new FuguiTheme(_newThemeName);
                                    theme.RegisterToThemeManager();
                                    ThemeManager.SaveTheme(theme);
                                    ThemeManager.SetTheme(theme);
                                    CloseModal();
                                }, UIButtonStyle.Success),
                                new UIModalButton("Cancel", CloseModal, UIButtonStyle.Default));
                            }
                            // delete this theme
                            if (grid.Button("Delete", UIButtonStyle.Danger))
                            {
                                _newThemeName = string.Empty;
                                ShowModal("Delete this theme", () =>
                                {
                                    using (UILayout layout = new UILayout())
                                    {
                                        layout.Dummy();
                                        layout.Text("Are you sure you want to delete this theme?\nThis can't be undone.");
                                    }
                                }, UIModalSize.Medium,
                                new UIModalButton("Yes", () =>
                                {
                                    ThemeManager.DeleteTheme(ThemeManager.CurrentTheme);
                                    CloseModal();
                                }, UIButtonStyle.Danger),
                                new UIModalButton("No", CloseModal, UIButtonStyle.Default));
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