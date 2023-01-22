using Fugui.Core;
using Fugui.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public class LayoutConfiguration : MonoBehaviour
{
    #region ATTRIBUTES
    private const string FUGUI_WINDOWS_DEFINTION_ENUM_PATH = "Assets\\Runtime\\Settings\\FuGuiWindows.cs";

    private Dictionary<int, string> _fuguiWindows;
    private string _windowsToAdd = string.Empty;
    private string _selectedValue = string.Empty;
    #endregion

    #region UNITY METHODS
    private void Start()
    {
        IUIWindowContainer mainContainer = FuGui.MainContainer;
        new UIWindowDefinition(FuGuiWindows.DockSpaceManager, "DockSpace Manager", DockSpaceManager);
        new UIWindowDefinition(FuGuiWindows.WindowsDefinitionManager, "Windows Definition Manager", WindowsDefinitionManager);

        _fuguiWindows = EnumToDictionary(typeof(FuGuiWindows));

        void DockSpaceManager(UIWindow window)
        {
            using (new UIPanel("mainPanel"))
            {
                using (UIGrid grid = new UIGrid("mainGrid"))
                {
                    
                }
            }
        }

        void WindowsDefinitionManager(UIWindow window)
        {
            using (new UIPanel("mainPanel"))
            {
                using (UIGrid grid = new UIGrid("mainGrid"))
                {
                    grid.Combobox("Windows definition :", _fuguiWindows.Values.ToList(), (x) => { _selectedValue = x; });
                    grid.Spacing();
                    grid.TextInput("Window name :", ref _windowsToAdd);

                    using (UILayout layout = new UILayout())
                    {
                        if (!string.IsNullOrEmpty(_windowsToAdd))
                        {
                            if (_fuguiWindows.Values.Contains(_windowsToAdd))
                            {
                                layout.SmartText(string.Format($"<color=red>The name <b>'{_windowsToAdd}'</b> is already present in the current FuGui windows definition !</color>"));
                            }
                            else
                            {
                                if (!IsAlphaNumeric(_windowsToAdd))
                                {
                                    layout.SmartText(string.Format($"<color=red>The name <b>'{_windowsToAdd}'</b> is not a valid name for a FuGui window !</color>"));
                                }
                                else
                                {
                                    layout.Spacing();
                                    if (layout.Button("Add new FuGui window definition", UIButtonStyle.FullSize, UIButtonStyle.Default))
                                    {
                                        if (!_fuguiWindows.Values.Contains(_windowsToAdd))
                                        {
                                            int newIndex = _fuguiWindows.Max(x => x.Key) + 1;
                                            _fuguiWindows.Add(_fuguiWindows.Keys.Last() + 1, _windowsToAdd);
                                            WriteToFile(FUGUI_WINDOWS_DEFINTION_ENUM_PATH, GenerateEnum("FuGuiWindows", _fuguiWindows));
                                            _windowsToAdd = string.Empty;
                                        }
                                    }
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(_selectedValue) && _selectedValue != "None")
                        {
                            if (layout.Button(string.Format($"Remove {_selectedValue}", UIButtonStyle.FullSize, UIButtonStyle.Default)))
                            {
                                if (_fuguiWindows.Values.Contains(_selectedValue))
                                {
                                    int keyToDelete = -1;

                                    foreach (KeyValuePair<int, string> item in _fuguiWindows)
                                    {
                                        if (item.Value == _selectedValue)
                                        {
                                            keyToDelete = item.Key;
                                            break;
                                        }
                                    }

                                    if (keyToDelete != -1)
                                    {
                                        _fuguiWindows.Remove(keyToDelete);
                                        WriteToFile(FUGUI_WINDOWS_DEFINTION_ENUM_PATH, GenerateEnum("FuGuiWindows", _fuguiWindows));
                                        _selectedValue = "None";
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        DockingLayoutManager.SetLayout(UIDockingLayout.DockSpaceConfiguration);
    }
    private bool IsAlphaNumeric(string input)
    {
        return Regex.IsMatch(input, @"^[a-zA-Z0-9]+$");
    }

    private void WriteToFile(string filePath, string content)
    {
        if (!File.Exists(filePath))
        {
            File.Create(filePath);
        }

        using (var streamWriter = new StreamWriter(filePath, false))
        {
            streamWriter.Write(content);
        }
    }

    private string GenerateEnum(string enumName, Dictionary<int, string> values)
    {
        var sb = new StringBuilder();
        sb.AppendLine("public enum " + enumName);
        sb.AppendLine("{");

        foreach (var item in values)
        {
            sb.AppendFormat("    {0} = {1},", item.Value, item.Key);
            sb.AppendLine();
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    Dictionary<int, string> EnumToDictionary(Type enumType)
    {
        return Enum.GetValues(enumType)
            .Cast<int>()
            .ToDictionary(x => x, x => Enum.GetName(enumType, x));
    }
    #endregion
}
