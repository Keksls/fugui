using Fu.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Represents the Fugui type.
    /// </summary>
    public static partial class Fugui
    {
        #region State
        private static string _newThemeName = string.Empty;
        private static readonly List<FuTheme> _themeSelectionItems = new List<FuTheme>(16);
        private static readonly Action<int> _setThemeBySelectionIndex = SetThemeBySelectionIndex;
        private static readonly Func<FuTheme> _getCurrentTheme = GetCurrentTheme;
        private static readonly FuGridDefinition _themeActionsGridDefinition =
            new FuGridDefinition(3, new float[] { 1f / 3f, 1f / 3f, 1f / 3f });
        private static readonly FuGridDefinition _themeColorsGridDefinition =
            new FuGridDefinition(2, new int[] { 196 });
        private static readonly string[] _themeColorNames = new string[(int)FuColors.COUNT];
        private static readonly FuBoundedCache<(Type ExtensionType, int Index), string> _themeExtensionColorNames =
            new FuBoundedCache<(Type ExtensionType, int Index), string>(256);
        #endregion

        #region Methods
        /// <summary>
        /// Clears theme-editor references retained by the previous Fugui session.
        /// </summary>
        internal static void ResetThemeUiRuntimeState()
        {
            // The selection buffer contains theme instances owned by the current manager.
            _themeSelectionItems.Clear();
            if (_themeSelectionItems.Capacity > 16)
            {
                _themeSelectionItems.Capacity = 16;
            }
            _newThemeName = string.Empty;
            _themeExtensionColorNames.Clear();
        }

        /// <summary>
        /// Refreshes the reusable list backing the theme selection combobox.
        /// </summary>
        private static void RefreshThemeSelectionItems()
        {
            _themeSelectionItems.Clear();
            if (Themes?.Themes == null)
            {
                return;
            }

            foreach (FuTheme theme in Themes.Themes.Values)
            {
                _themeSelectionItems.Add(theme);
            }

            // Release an obsolete exceptional spike while preserving normal list reuse.
            if (_themeSelectionItems.Capacity > 64 &&
                _themeSelectionItems.Capacity > Math.Max(16, _themeSelectionItems.Count * 4))
            {
                _themeSelectionItems.Capacity = Math.Max(16, _themeSelectionItems.Count * 2);
            }
        }

        /// <summary>
        /// Applies the theme selected by the reusable combobox list.
        /// </summary>
        /// <param name="index">Selected list index.</param>
        private static void SetThemeBySelectionIndex(int index)
        {
            if (Themes != null && index >= 0 && index < _themeSelectionItems.Count)
            {
                Themes.SetTheme(_themeSelectionItems[index]);
            }
        }

        /// <summary>
        /// Gets the current theme for the selection combobox.
        /// </summary>
        /// <returns>Current Fugui theme, or null while the manager is unavailable.</returns>
        private static FuTheme GetCurrentTheme()
        {
            return Themes?.CurrentTheme;
        }

        /// <summary>
        /// Gets a cached display name for a built-in theme color.
        /// </summary>
        /// <param name="color">Built-in color identifier.</param>
        /// <returns>Human-readable cached color name.</returns>
        private static string GetThemeColorName(FuColors color)
        {
            int index = (int)color;
            string name = _themeColorNames[index];
            if (name == null)
            {
                name = AddSpacesBeforeUppercase(color.ToString());
                _themeColorNames[index] = name;
            }

            return name;
        }

        /// <summary>
        /// Gets a cached display name for a theme-extension color.
        /// </summary>
        /// <param name="extensionType">Theme extension enum type.</param>
        /// <param name="index">Extension color index.</param>
        /// <returns>Human-readable cached extension color name.</returns>
        private static string GetThemeExtensionColorName(Type extensionType, int index)
        {
            (Type ExtensionType, int Index) key = (extensionType, index);
            if (!_themeExtensionColorNames.TryGetValue(key, out string name))
            {
                name = AddSpacesBeforeUppercase(Enum.GetName(extensionType, index));
                _themeExtensionColorNames.Set(key, name);
            }

            return name;
        }

        /// <summary>
        /// Draws the themes.
        /// </summary>
        /// <param name="layout">The layout value.</param>
        public static void DrawThemes(FuLayout layout)
        {
            RefreshThemeSelectionItems();
            layout.Collapsable("Theme Managment", () =>
            {
                using (FuGrid grid = new FuGrid("themeManagmentGrid"))
                {
                    grid.Combobox("Current theme", _themeSelectionItems, _setThemeBySelectionIndex, _getCurrentTheme);
                }
                using (FuGrid grid = new FuGrid("themeManagmentActions", _themeActionsGridDefinition, cellPadding: 0f))
                {
                    // save theme
                    if (grid.Button("Save"))
                    {
                        Fugui.Themes.SaveTheme(Fugui.Themes.CurrentTheme);
                        Fugui.Themes.SetTheme(Fugui.Themes.CurrentTheme);
                    }
                    // create new theme
                    if (grid.Button("New"))
                    {
                        _newThemeName = string.Empty;
                        ShowModal("Create new Theme", (layout) =>
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
                            Fugui.Themes.SaveTheme(theme);
                            Fugui.Themes.SetTheme(theme);
                            CloseModal();
                        }, FuButtonStyle.Success),
                        new FuModalButton("Cancel", CloseModal, FuButtonStyle.Default));
                    }
                    // delete this theme
                    if (grid.Button("Delete", FuButtonStyle.Danger))
                    {
                        _newThemeName = string.Empty;
                        ShowModal("Delete this theme", (layout) =>
                        {
                            layout.Dummy();
                            layout.Text("Are you sure you want to delete this theme?\nThis can't be undone.");
                        }, FuModalSize.Medium,
                        new FuModalButton("Yes", () =>
                        {
                            Fugui.Themes.DeleteTheme(Fugui.Themes.CurrentTheme);
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
                    if (grid.DrawObject("FuguiTheme", Fugui.Themes.CurrentTheme))
                    {
                        Fugui.Themes.SetTheme(Fugui.Themes.CurrentTheme);
                        ForceDrawAllWindows();
                    }
                }
            });

            layout.Collapsable("Theme Colors", () =>
            {
                using (FuGrid grid = new FuGrid("FuguiThemeColorGrid", _themeColorsGridDefinition, FuGridFlag.AutoToolTipsOnLabels | FuGridFlag.LinesBackground, 4f))
                {
                    for (int i = 0; i < (int)FuColors.COUNT; i++)
                    {
                        if (Fugui.Themes.CurrentTheme.Colors.Length > i)
                        {
                            Vector4 selectedColor = Fugui.Themes.CurrentTheme.Colors[i];
                            string colorName = GetThemeColorName((FuColors)i);
                            if (grid.ColorPicker(colorName, ref selectedColor))
                            {
                                Fugui.Themes.CurrentTheme.Colors[i] = selectedColor;
                                Fugui.Themes.SetTheme(Fugui.Themes.CurrentTheme);
                            }
                        }
                    }

                    /////////////////////////////////// To change if extension is not activated
                    for (int i = 0; i < FuTheme.ThemeExtensionCount; i++)
                    {
                        Vector4 selectedColor = Fugui.Themes.CurrentTheme.Colors[(int)FuColors.COUNT + i];
                        string colorName = GetThemeExtensionColorName(FuTheme.ThemeExtension.GetType(), i);
                        if (grid.ColorPicker(colorName, ref selectedColor))
                        {
                            Fugui.Themes.CurrentTheme.Colors[(int)FuColors.COUNT + i] = selectedColor;
                            Fugui.Themes.SetTheme(Fugui.Themes.CurrentTheme);
                        }
                    }
                }
            });
        }
        #endregion
    }
}
