using Fu.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Fu
{
    public class FuThemeManager
    {
        #region Variables
        public event Action<FuTheme> OnThemeSet;
        public FuTheme CurrentTheme { get; private set; }
        public Dictionary<string, FuTheme> Themes { get; private set; }
        private List<Type> _uiElementStyleTypes;
        public const string DEFAULT_FUGUI_THEME_NAME = "DarkSky";
        #endregion

        #region Current Theme Variables
        public Vector2 WindowPadding { get; private set; }
        public Vector2 WindowMinSize { get; private set; }
        public Vector2 FramePadding { get; private set; }
        public Vector2 ItemSpacing { get; private set; }
        public Vector2 ItemInnerSpacing { get; private set; }
        public Vector2 CellPadding { get; private set; }
        public float WindowRounding { get; private set; }
        public float WindowBorderSize { get; private set; }
        public float ChildRounding { get; private set; }
        public float ChildBorderSize { get; private set; }
        public float PopupRounding { get; private set; }
        public float PopupBorderSize { get; private set; }
        public float FrameRounding { get; private set; }
        public float FrameBorderSize { get; private set; }
        public float IndentSpacing { get; private set; }
        public float ColumnsMinSpacing { get; private set; }
        public float ScrollbarSize { get; private set; }
        public float ScrollbarRounding { get; private set; }
        public float GrabMinSize { get; private set; }
        public float GrabRounding { get; private set; }
        public float TabRounding { get; private set; }
        public float TabBorderSize { get; private set; }
        public float TabMinWidthForCloseButton { get; private set; }
        public float NodeKnobRadius { get; private set; }
        public float NodeKnobMargin { get; private set; }

        /// <summary>
        /// Update variables with current theme scale
        /// </summary>
        internal void CurrentThemeUpdate()
        {
            float scale = Fugui.CurrentContext.Scale;
            WindowPadding = CurrentTheme.WindowPadding * scale;
            WindowRounding = CurrentTheme.WindowRounding * scale;
            WindowBorderSize = CurrentTheme.WindowBorderSize * scale;
            WindowMinSize = CurrentTheme.WindowMinSize * scale;
            ChildRounding = CurrentTheme.ChildRounding * scale;
            ChildBorderSize = CurrentTheme.ChildBorderSize * scale;
            PopupRounding = CurrentTheme.PopupRounding * scale;
            PopupBorderSize = CurrentTheme.PopupBorderSize * scale;
            FramePadding = CurrentTheme.FramePadding * scale;
            FrameRounding = CurrentTheme.FrameRounding * scale;
            FrameBorderSize = CurrentTheme.FrameBorderSize * scale;
            ItemSpacing = CurrentTheme.ItemSpacing * scale;
            ItemInnerSpacing = CurrentTheme.ItemInnerSpacing * scale;
            CellPadding = CurrentTheme.CellPadding * scale;
            IndentSpacing = CurrentTheme.IndentSpacing * scale;
            ColumnsMinSpacing = CurrentTheme.ColumnsMinSpacing * scale;
            ScrollbarSize = CurrentTheme.ScrollbarSize * scale;
            ScrollbarRounding = CurrentTheme.ScrollbarRounding * scale;
            GrabMinSize = CurrentTheme.GrabMinSize * scale;
            GrabRounding = CurrentTheme.GrabRounding * scale;
            TabRounding = CurrentTheme.TabRounding * scale;
            TabBorderSize = CurrentTheme.TabBorderSize * scale;
            TabMinWidthForCloseButton = CurrentTheme.TabMinWidthForCloseButton * scale;
            NodeKnobRadius = CurrentTheme.NodeKnobRadius * scale;
            NodeKnobMargin = CurrentTheme.NodeKnobMargin * scale;
        }
        #endregion

        public FuThemeManager()
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
        internal void Initialize()
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
        public void SetTheme(FuTheme theme, bool allContexts = true)
        {
            if (CurrentTheme == null)
            {
                // we have no theme yet, Fugui just start, we have not started to draw anything, let's set theme directly
                setTheme(theme, allContexts);
            }
            else
            {
                // Fugui has start, we have no way to know where we are in the render workflow. 
                // Let's wait to apply theme before starting to draw a frame, let's finish the current frame with old theme
                Fugui.ExecuteInMainThread(() =>
                {
                    setTheme(theme, allContexts);
                });
            }

            void setTheme(FuTheme theme, bool allContexts)
            {
                if (allContexts && Fugui.Contexts.Count > 1)
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
                CurrentThemeUpdate();
                // call OnThemeSet on each structs that inherit from 
                foreach (Type structType in _uiElementStyleTypes)
                {
                    var myFunctionMethod = structType.GetMethod("OnThemeSet", BindingFlags.NonPublic | BindingFlags.Static);
                    myFunctionMethod.Invoke(null, null);
                }
                OnThemeSet?.Invoke(CurrentTheme);
                Fugui.ForceDrawAllWindows(2);
            }
        }

        #region Public Utils
        /// <summary>
        /// get a them throw it's name
        /// </summary>
        /// <param name="themeName">name of the theme to get</param>
        /// <param name="theme">the theme (if exists)</param>
        /// <returns>whatever the theme exists</returns>
        public bool GetTheme(string themeName, out FuTheme theme)
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
        public Vector4 GetColor(FuColors color)
        {
            return CurrentTheme.Colors[(int)color];
        }

        /// <summary>
        /// return the color of the current theme that match with the given enum parameter as U32
        /// </summary>
        /// <param name="color"> color enum to get value of</param>
        /// <returns> color value as U32</returns>
        public uint GetColorU32(FuColors color)
        {
            Vector4 col = CurrentTheme.Colors[(int)color];
            return ImGuiNET.ImGui.GetColorU32(col);
        }

        /// <summary>
        /// return the color of the current theme that match with the given enum parameter
        /// </summary>
        /// <param name="color">color enum to get value of</param>
        /// <param name="alphaMult">alpha multiplicator of the color</param>
        /// <returns>color value as Vector4</returns>
        public Vector4 GetColor(FuColors color, float alphaMult)
        {
            Vector4 colorV4 = CurrentTheme.Colors[(int)color];
            colorV4.w *= alphaMult;
            return colorV4;
        }

        /// <summary>
        /// return the color of the current theme that match with the given enum parameter as U32
        /// </summary>
        /// <param name="color"> color enum to get value of</param>
        /// <param name="alphaMult"> alpha multiplicator of the color</param>
        /// <returns> color value as U32</returns>
        public uint GetColorU32(FuColors color, float alphaMult)
        {
            Vector4 col = CurrentTheme.Colors[(int)color];
            col.w *= alphaMult;
            return ImGuiNET.ImGui.GetColorU32(col);
        }

        /// <summary>
        /// return the color of the current theme extension that match with the given enum parameter
        /// </summary>
        /// <param name="color">color enum to get value of</param>
        /// <returns>color value as Vector4</returns>
        public Vector4 GetExtensionColor(int color)
        {
            return CurrentTheme.Colors[(int)FuColors.COUNT + color];
        }
        #endregion

        #region Themes managments
        /// <summary>
        /// Load all themes from 
        /// </summary>
        /// <returns></returns>
        public int LoadAllThemes()
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
        public bool SaveTheme(FuTheme theme)
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
        public bool LoadTheme(string themeName, out FuTheme theme)
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
                theme.UpdateThemeWithExtension();
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
        public void DeleteTheme(FuTheme theme)
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
        public void SetDefaultTheme()
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
        public bool RegisterTheme(FuTheme theme)
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
        public bool UnregisterTheme(FuTheme theme)
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

        /// <summary>
        /// Extend theme with more colors
        /// </summary>
        /// <param name="themesExtension">Enum of colors to add to all themes</param>
        public void ExtendThemes(Enum themesExtension)
        {
            FuTheme.ExtendThemes(themesExtension);
            foreach (FuTheme theme in Themes.Values)
            {
                theme.UpdateThemeWithExtension();
            }
            LoadAllThemes();
            if (GetTheme(CurrentTheme.ThemeName, out FuTheme currentTheme))
            {
                SetTheme(currentTheme);
            }
        }

        /// <summary>
        /// Removes themes extension
        /// </summary>
        public void ReduceThemes()
        {
            FuTheme.ReduceThemes();
        }
        #endregion
    }
}