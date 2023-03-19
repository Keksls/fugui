using UnityEngine;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.IO;
using Fu.Core;

namespace Fu.Framework
{
    public static class FuThemeManager
    {
        #region Variables
        public static event Action<FuTheme> OnThemeSet;
        public static FuTheme CurrentTheme { get; private set; }
        public static Dictionary<string, FuTheme> Themes { get; private set; }
        private static List<Type> _uiElementStyleTypes;
        public const string DEFAULT_FUGUI_THEME_NAME = "DarkSky";
        #endregion

        static FuThemeManager()
        {
            // we could not bind it using reflection because some element style use others, so we need to set presset within a specific order
            _uiElementStyleTypes = new List<Type>
            {
                typeof(FuTextStyle),
                typeof(FuButtonStyle),
                typeof(FuFrameStyle),
                typeof(FuComboboxStyle),
                typeof(FuPanelStyle),
                typeof(FuButtonsGroupStyle),
                typeof(FuStyle)
            };
        }

        /// <summary>
        /// Initialize the theme manager and set default theme
        /// </summary>
        internal static void Initialize()
        {
            LoadAllThemes();
            if (GetTheme(DEFAULT_FUGUI_THEME_NAME, out FuTheme theme))
            {
                SetTheme(theme);
            }
            else
            {
                SetTheme(new FuTheme(DEFAULT_FUGUI_THEME_NAME));
            }
        }

        /// <summary>
        /// set and apply a FuguiTheme
        /// </summary>
        /// <param name="theme">Theme to set</param>
        public static void SetTheme(FuTheme theme, bool allContexts = true)
        {
            if (allContexts)
            {
                // get current context id
                int currentContextID = Fugui.CurrentContext.ID;
                // apply theme on each contexts
                foreach (FuContext context in Fugui.Contexts.Values)
                {
                    context.SetAsCurrent();
                    theme.Apply(context.Scale);
                }
                // set current last context
                Fugui.GetContext(currentContextID)?.SetAsCurrent();
            }
            else
            {
                theme.Apply(Fugui.CurrentContext.Scale);
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
        public static bool GetTheme(string themeName, out FuTheme theme)
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
        /// return the color of the current theme that match with the given enum parameter
        /// </summary>
        /// <param name="color">color enum to get value of</param>
        /// <returns>color value as Vector4</returns>
        public static Vector4 GetColor(FuColors color)
        {
            return CurrentTheme.Colors[(int)color];
        }
        #endregion

        #region Themes managments
        /// <summary>
        /// Load all themes from 
        /// </summary>
        /// <returns></returns>
        public static int LoadAllThemes()
        {
            Themes = new Dictionary<string, FuTheme>();
            // get folder path
            string folderPath = Path.Combine(Application.streamingAssetsPath, Fugui.Settings.ThemesFolder);
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
                    Fugui.Fire_OnUIException(ex);
                    return Themes.Count;
                }
            }

            // iterate on each file into folder
            foreach (string file in Directory.GetFiles(folderPath))
            {
                // try to load theme from file
                if (LoadTheme(Path.GetFileNameWithoutExtension(file), out FuTheme theme))
                {
                    // if theme loaded, add it to dic
                    Themes.Add(theme.ThemeName, theme);
                }
            }

            // add default Theme if needed
            if (!Themes.ContainsKey(DEFAULT_FUGUI_THEME_NAME))
            {
                Themes.Add(DEFAULT_FUGUI_THEME_NAME, new FuTheme(DEFAULT_FUGUI_THEME_NAME));
            }

            // return number of themes loaded
            return Themes.Count;
        }

        /// <summary>
        /// Save this theme into a folder (be carefull to Theme name)
        /// </summary>
        /// <param name="theme">Theme to save</param>
        /// <returns>true if success</returns>
        public static bool SaveTheme(FuTheme theme)
        {
            // get folder path
            string folderPath = Path.Combine(Application.streamingAssetsPath, Fugui.Settings.ThemesFolder);
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
                    Fugui.Fire_OnUIException(ex);
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
        public static bool LoadTheme(string themeName, out FuTheme theme)
        {
            theme = null;
            // get folder path
            string folderPath = Path.Combine(Application.streamingAssetsPath, Fugui.Settings.ThemesFolder);
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
                theme = JsonUtility.FromJson<FuTheme>(json);
                Vector4[] colors = new Vector4[(int)FuColors.COUNT];
                for (int i = 0; i < theme.Colors.Length; i++)
                {
                    colors[i] = theme.Colors[i];
                }
                theme.Colors = colors;
            }
            catch (Exception ex)
            {
                // something gone wrong, let's invoke Fugui Exception event
                Fugui.Fire_OnUIException(ex);
                return false;
            }
            return true;
        }

        /// <summary>
        /// delete a given theme from manager and file
        /// </summary>
        /// <param name="theme">theme to delete</param>
        public static void DeleteTheme(FuTheme theme)
        {
            theme.UnregisterToThemeManager();
            // get folder path
            string folderPath = Path.Combine(Application.streamingAssetsPath, Fugui.Settings.ThemesFolder);
            string filePath = Path.Combine(folderPath, theme.ThemeName + ".fskin");
            // remove file if exists
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            else
            {
                Debug.LogError("you are trying to delete a theme at " + filePath + " but the file does not exists.");
                return;
            }

            // set default theme
            SetDefaultTheme();
        }

        /// <summary>
        /// set default fugui theme as current
        /// </summary>
        public static void SetDefaultTheme()
        {
            // add default Theme if needed
            if (!Themes.ContainsKey(DEFAULT_FUGUI_THEME_NAME))
            {
                Themes.Add(DEFAULT_FUGUI_THEME_NAME, new FuTheme(DEFAULT_FUGUI_THEME_NAME));
            }

            // get and set default theme
            GetTheme(DEFAULT_FUGUI_THEME_NAME, out FuTheme defaultTheme);
            SetTheme(defaultTheme);
        }

        /// <summary>
        /// try to register this theme to ThemManager
        /// </summary>
        /// <returns>true if success, false if a theme with the same name already exists in theme manager</returns>
        public static bool RegisterTheme(FuTheme theme)
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
        public static bool UnregisterTheme(FuTheme theme)
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