using UnityEngine;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.IO;
using System.Linq;
using Fugui.Core;

namespace Fugui.Framework
{
    public static class ThemeManager
    {
        #region Variables
        public static event Action<FuguiTheme> OnThemeSet;
        public static FuguiTheme CurrentTheme { get; private set; }
        public static Dictionary<string, FuguiTheme> Themes { get; private set; }
        private static List<Type> _uiElementStyleTypes;
        public const string DEFAULT_FUGUI_THEME_NAME = "DarkSky";
        #endregion

        static ThemeManager()
        {
            // we could not bind it using reflection because some element style use others, so we need to set presset within a specific order
            _uiElementStyleTypes = new List<Type>();
            _uiElementStyleTypes.Add(typeof(UITextStyle));
            _uiElementStyleTypes.Add(typeof(UIButtonStyle));
            _uiElementStyleTypes.Add(typeof(UIFrameStyle));
            _uiElementStyleTypes.Add(typeof(UIComboboxStyle));
            _uiElementStyleTypes.Add(typeof(UIPanelStyle));
            _uiElementStyleTypes.Add(typeof(UIButtonsGroupStyle));
            _uiElementStyleTypes.Add(typeof(UIStyle));
            _uiElementStyleTypes.Add(typeof(UICollapsableStyle));
        }

        /// <summary>
        /// Initialize the theme manager and set default theme
        /// </summary>
        internal static void Initialize()
        {
            LoadAllThemes();
            if (GetTheme(DEFAULT_FUGUI_THEME_NAME, out FuguiTheme theme))
            {
                SetTheme(theme);
            }
            else
            {
                SetTheme(new FuguiTheme(DEFAULT_FUGUI_THEME_NAME));
            }
        }

        /// <summary>
        /// set and apply a FuguiTheme
        /// </summary>
        /// <param name="theme">Theme to set</param>
        public static void SetTheme(FuguiTheme theme, bool allContexts = true)
        {
            if (allContexts)
            {
                // get current context id
                int currentContextID = FuGui.CurrentContext.ID;
                // apply theme on each contexts
                foreach (FuguiContext context in FuGui.Contexts.Values)
                {
                    context.SetAsCurrent();
                    theme.Apply();
                }
                // set current last context
                FuGui.GetContext(currentContextID)?.SetAsCurrent();
            }
            else
            {
                theme.Apply();
            }

            CurrentTheme = theme;
            // call OnThemeSet on each structs that inherit from 
            foreach (Type structType in _uiElementStyleTypes)
            {
                var myFunctionMethod = structType.GetMethod("OnThemeSet", BindingFlags.NonPublic | BindingFlags.Static);
                myFunctionMethod.Invoke(null, null);
            }
            OnThemeSet?.Invoke(CurrentTheme);
        }

        #region Public Utils
        /// <summary>
        /// get a them throw it's name
        /// </summary>
        /// <param name="themeName">name of the theme to get</param>
        /// <param name="theme">the theme (if exists)</param>
        /// <returns>whatever the theme exists</returns>
        public static bool GetTheme(string themeName, out FuguiTheme theme)
        {
            theme = null;
            if (Themes.ContainsKey(themeName))
            {
                theme = Themes[themeName];
                return true;
            }
            return false;
        }

        /// <summary>
        /// return the color of the current theme that match with the giver enum parameter
        /// </summary>
        /// <param name="color">color enum to get value of</param>
        /// <returns>color value as Vector4</returns>
        public static Vector4 GetColor(FuguiColors color)
        {
            return CurrentTheme.Colors[(int)color];
        }
        #endregion

        #region UI
        public static void DrawThemeManagerUI()
        {
            using (new UIPanel("themeManagerContainer", UIStyle.Unpadded))
            {
                using (UILayout layout = new UILayout())
                {
                    layout.Collapsable("Theme Managment", () =>
                    {
                        using (UIGrid grid = new UIGrid("themeManagmentGrid"))
                        {
                            grid.Combobox("Current theme", Themes.Values.ToList(), (theme) =>
                            {
                                SetTheme(theme);
                            }, () => { return CurrentTheme; });
                        }
                        using (UIGrid grid = new UIGrid("themeManagmentActions", UIGridDefinition.DefaultRatio, cellPadding: 0f))
                        {
                            if (grid.Button("Save"))
                            {
                                SaveTheme(CurrentTheme);
                                LoadAllThemes();
                                SetTheme(CurrentTheme);
                            }
                            if (grid.Button("New"))
                            {

                            }
                        }
                    });

                    layout.Collapsable("Theme Variables", () =>
                    {
                        using (UIGrid grid = new UIGrid("FuguiThemeVariablesGrid", UIGridFlag.LinesBackground | UIGridFlag.AutoToolTipsOnLabels))
                        {
                            if (grid.DrawObject<FuguiTheme>(CurrentTheme))
                            {
                                SetTheme(CurrentTheme);
                                FuGui.ForceDrawAllWindows();
                            }
                        }
                    });

                    layout.Collapsable("Theme Colors", () =>
                    {
                        using (UIGrid grid = new UIGrid("FuguiThemeColorGrid", new UIGridDefinition(2, new int[] { 196 }), UIGridFlag.AutoToolTipsOnLabels | UIGridFlag.LinesBackground, 4f))
                        {
                            for (int i = 0; i < (int)FuguiColors.COUNT; i++)
                            {
                                Vector4 selectedColor = CurrentTheme.Colors[i];
                                string colorName = ((FuguiColors)i).ToString();
                                colorName = FuGui.AddSpacesBeforeUppercase(colorName);
                                if (grid.ColorPicker(colorName, ref selectedColor))
                                {
                                    CurrentTheme.Colors[i] = selectedColor;
                                    SetTheme(CurrentTheme);
                                    FuGui.ForceDrawAllWindows();
                                }
                            }
                        }
                    });
                }
            }
        }
        #endregion

        #region Themes managments
        /// <summary>
        /// Load all themes from 
        /// </summary>
        /// <returns></returns>
        public static int LoadAllThemes()
        {
            Themes = new Dictionary<string, FuguiTheme>();
            // get folder path
            string folderPath = Path.Combine(Application.streamingAssetsPath, FuGui.Settings.ThemesFolder);
            // create folder if not exists
            if (!Directory.Exists(folderPath))
            {
                try
                {
                    // try to create directory if not exists
                    Directory.CreateDirectory(folderPath);
                }
                catch (Exception ex)
                {
                    // something gone wrong, let's invoke Fugui Exception event
                    FuGui.DoOnUIException(ex);
                    return Themes.Count;
                }
            }

            // iterate on each file into folder
            foreach (string file in Directory.GetFiles(folderPath))
            {
                // try to load theme from file
                if (LoadTheme(Path.GetFileNameWithoutExtension(file), out FuguiTheme theme))
                {
                    // if theme loaded, add it to dic
                    Themes.Add(theme.ThemeName, theme);
                }
            }

            // add default Theme if needed
            if (!Themes.ContainsKey(DEFAULT_FUGUI_THEME_NAME))
            {
                Themes.Add(DEFAULT_FUGUI_THEME_NAME, new FuguiTheme(DEFAULT_FUGUI_THEME_NAME));
            }

            // return number of themes loaded
            return Themes.Count;
        }

        /// <summary>
        /// Save this theme into a folder (be carefull to Theme name)
        /// </summary>
        /// <param name="theme">Theme to save</param>
        /// <returns>true if success</returns>
        public static bool SaveTheme(FuguiTheme theme)
        {
            // get folder path
            string folderPath = Path.Combine(Application.streamingAssetsPath, FuGui.Settings.ThemesFolder);
            // create folder if not exists
            if (!Directory.Exists(folderPath))
            {
                try
                {
                    // try to create directory if not exists
                    Directory.CreateDirectory(folderPath);
                }
                catch (Exception ex)
                {
                    // something gone wrong, let's invoke Fugui Exception event
                    FuGui.DoOnUIException(ex);
                    return false;
                }
            }
            // get json data
            string json = JsonUtility.ToJson(theme);
            // write json to file
            File.WriteAllText(Path.Combine(folderPath, theme.ThemeName) + ".fskin", json);
            return true;
        }

        /// <summary>
        /// Load theme data from a file and set it to this theme
        /// </summary>
        /// <param name="themeName">name of the theme to load</param>
        /// <returns>true if success</returns>
        public static bool LoadTheme(string themeName, out FuguiTheme theme)
        {
            theme = null;
            // get folder path
            string folderPath = Path.Combine(Application.streamingAssetsPath, FuGui.Settings.ThemesFolder);
            string filePath = Path.Combine(folderPath, themeName + ".fskin");
            // check whatever thee file exists
            if (!File.Exists(filePath))
            {
                return false;
            }

            // try to get theme from path
            try
            {
                // read json data from file
                string json = File.ReadAllText(filePath);
                // deserialize json data
                theme = JsonUtility.FromJson<FuguiTheme>(json);
                Vector4[] colors = new Vector4[(int)FuguiColors.COUNT];
                for (int i = 0; i < theme.Colors.Length; i++)
                {
                    colors[i] = theme.Colors[i];
                }
                theme.Colors = colors;
            }
            catch (Exception ex)
            {
                // something gone wrong, let's invoke Fugui Exception event
                FuGui.DoOnUIException(ex);
                return false;
            }
            return true;
        }

        /// <summary>
        /// try to register this theme to ThemManager
        /// </summary>
        /// <returns>true if success, false if a theme with the same name already exists in theme manager</returns>
        public static bool RegisterTheme(FuguiTheme theme)
        {
            // check whatever a theme with the same name already exists in theme manager
            if (Themes.ContainsKey(theme.ThemeName))
            {
                return false;
            }
            // register the theme
            Themes.Add(theme.ThemeName, theme);
            return true;
        }

        /// <summary>
        /// try to unregister this theme from ThemManager
        /// </summary>
        /// <returns>true if success, false if a no with the same name exists in theme manager</returns>
        public static bool UnregisterTheme(FuguiTheme theme)
        {
            // check whatever a theme with the same name already exists in theme manager
            if (!Themes.ContainsKey(theme.ThemeName))
            {
                return false;
            }
            // unregister the theme
            Themes.Remove(theme.ThemeName);
            return true;
        }
        #endregion
    }
}