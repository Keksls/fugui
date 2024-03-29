﻿using Fu.Framework;
using System;
using System.Linq;
using UnityEngine;

namespace Fu
{
    public static partial class Fugui
    {
        private static string _newThemeName = string.Empty;

        public static void DrawThemes()
        {
            using (FuLayout layout = new FuLayout())
            {
                layout.Collapsable("Theme Managment", () =>
                {
                    using (FuGrid grid = new FuGrid("themeManagmentGrid"))
                    {
                        grid.Combobox("Current theme", FuThemeManager.Themes.Values.ToList(), (index) =>
                        {
                            FuThemeManager.SetTheme(FuThemeManager.Themes.Values.ToList()[index]);
                        }, () => { return FuThemeManager.CurrentTheme; });
                    }
                    using (FuGrid grid = new FuGrid("themeManagmentActions", new FuGridDefinition(3, new float[] { 1f / 3f, 1f / 3f, 1f / 3f }), cellPadding: 0f))
                    {
                        // save theme
                        if (grid.Button("Save"))
                        {
                            FuThemeManager.SaveTheme(FuThemeManager.CurrentTheme);
                            FuThemeManager.SetTheme(FuThemeManager.CurrentTheme);
                        }
                        // create new theme
                        if (grid.Button("New"))
                        {
                            _newThemeName = string.Empty;
                            ShowModal("Create new Theme", () =>
                            {
                                using (FuGrid grid = new FuGrid("newThemeGrid"))
                                {
                                    grid.TextInput("Theme Name", "new theme", ref _newThemeName);
                                }
                            }, FuModalSize.Medium,
                            new FuModalButton("OK", () =>
                            {
                                FuTheme theme = new FuTheme(_newThemeName);
                                theme.RegisterToThemeManager();
                                FuThemeManager.SaveTheme(theme);
                                FuThemeManager.SetTheme(theme);
                                CloseModal();
                            }, FuButtonStyle.Success),
                            new FuModalButton("Cancel", CloseModal, FuButtonStyle.Default));
                        }
                        // delete this theme
                        if (grid.Button("Delete", FuButtonStyle.Danger))
                        {
                            _newThemeName = string.Empty;
                            ShowModal("Delete this theme", () =>
                            {
                                using (FuLayout layout = new FuLayout())
                                {
                                    layout.Dummy();
                                    layout.Text("Are you sure you want to delete this theme?\nThis can't be undone.");
                                }
                            }, FuModalSize.Medium,
                            new FuModalButton("Yes", () =>
                            {
                                FuThemeManager.DeleteTheme(FuThemeManager.CurrentTheme);
                                CloseModal();
                            }, FuButtonStyle.Danger),
                            new FuModalButton("No", CloseModal, FuButtonStyle.Default));
                        }
                    }
                });

                layout.Collapsable("Theme Variables", () =>
                {
                    using (FuGrid grid = new FuGrid("FuguiThemeVariablesGrid", FuGridFlag.LinesBackground | FuGridFlag.AutoToolTipsOnLabels))
                    {
                        if (grid.DrawObject("FuguiTheme", FuThemeManager.CurrentTheme))
                        {
                            FuThemeManager.SetTheme(FuThemeManager.CurrentTheme);
                            ForceDrawAllWindows();
                        }
                    }
                });

                layout.Collapsable("Theme Colors", () =>
                {
                    using (FuGrid grid = new FuGrid("FuguiThemeColorGrid", new FuGridDefinition(2, new int[] { 196 }), FuGridFlag.AutoToolTipsOnLabels | FuGridFlag.LinesBackground, 4f))
                    {
                        for (int i = 0; i < (int)FuColors.COUNT; i++)
                        {
                            if (FuThemeManager.CurrentTheme.Colors.Length > i)
                            {
                                Vector4 selectedColor = FuThemeManager.CurrentTheme.Colors[i];
                                string colorName = ((FuColors)i).ToString();
                                colorName = AddSpacesBeforeUppercase(colorName);
                                if (grid.ColorPicker(colorName, ref selectedColor))
                                {
                                    FuThemeManager.CurrentTheme.Colors[i] = selectedColor;
                                    FuThemeManager.SetTheme(FuThemeManager.CurrentTheme);
                                }
                            }
                        }

                        /////////////////////////////////// To change if extension is not activated
						for (int i = 0; i < FuTheme.ThemeExtensionCount; i++)
						{
                            Vector4 selectedColor = FuThemeManager.CurrentTheme.Colors[(int)FuColors.COUNT + i];
                            string colorName = Enum.GetName(FuTheme.ThemeExtension.GetType(), i);
                            colorName = AddSpacesBeforeUppercase(colorName);
                            if (grid.ColorPicker(colorName, ref selectedColor))
                            {
                                FuThemeManager.CurrentTheme.Colors[(int)FuColors.COUNT + i] = selectedColor;
                                FuThemeManager.SetTheme(FuThemeManager.CurrentTheme);
                            }
                        }
                    }
                });
            }
        }
    }
}