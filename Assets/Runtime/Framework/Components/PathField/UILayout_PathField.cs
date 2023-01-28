using ImGuiNET;
using System;
using System.IO;
using UnityEngine;

namespace Fugui.Framework
{
    public partial class UILayout
    {
        public void InputFolder(string id, Action<string> callback = null, string defaultPath = null, params ExtensionFilter[] extentions)
        {
            _pathField(id, true, callback, UIFrameStyle.Default, defaultPath, extentions);
        }

        public void InputFolder(string id, UIFrameStyle style, Action<string> callback = null, string defaultPath = null, params ExtensionFilter[] extentions)
        {
            _pathField(id, true, callback, style, defaultPath, extentions);
        }

        public void InputFile(string id, Action<string> callback = null, string defaultPath = null, params ExtensionFilter[] extentions)
        {
            _pathField(id, false, callback, UIFrameStyle.Default, defaultPath, extentions);
        }

        public void InputFile(string id, UIFrameStyle style, Action<string> callback = null, string defaultPath = null, params ExtensionFilter[] extentions)
        {
            _pathField(id, false, callback, style, defaultPath, extentions);
        }

        protected virtual void _pathField(string id, bool onlyFolder, Action<string> callback, UIFrameStyle style, string defaultPath = "", params ExtensionFilter[] extentions)
        {
            // apply style and set unique ID
            id = beginElement(id, style);

            // set path if not exist in dic
            if (!_pathFieldValues.ContainsKey(id))
            {
                _pathFieldValues.Add(id, string.IsNullOrEmpty(defaultPath) ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop) : defaultPath);
            }
            string path = _pathFieldValues[id];

            // display values
            float cursorPos = ImGui.GetCursorScreenPos().x;
            float width = ImGui.GetContentRegionAvail().x;
            float buttonWidth = ImGui.CalcTextSize("...").x + 8f * FuGui.CurrentContext.Scale;

            // draw input text
            ImGui.SetNextItemWidth(width - buttonWidth);
            bool edited = false;
            if (ImGui.InputText("##" + id, ref path, 2048, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                validatePath();
            }
            // draw text input frame and tooltip
            _elementHoverFramed = true;
            drawHoverFrame();
            displayToolTip(false, true);

            // draw button
            ImGui.SameLine();
            ImGui.SetCursorScreenPos(new Vector2(cursorPos + width - buttonWidth, ImGui.GetCursorScreenPos().y));
            if (ImGui.Button("...##" + id, new Vector2(buttonWidth, 0)))
            {
                string[] paths = null;
                if (onlyFolder)
                {
                    paths = FileBrowser.OpenFolderPanel("Open Folder", defaultPath, false);
                }
                else
                {
                    paths = FileBrowser.OpenFilePanel("Open File", defaultPath, extentions, false);
                }
                if (paths != null && paths.Length > 0)
                {
                    path = paths[0];
                    validatePath();
                }
            }
            _elementHoverFramed = true;
            endElement(style);

            if (edited)
            {
                callback?.Invoke(_pathFieldValues[id]);
            }

            void validatePath()
            {
                // it must be a directory and it exists
                if (onlyFolder && Directory.Exists(path))
                {
                    _pathFieldValues[id] = path;
                    edited = true;
                }
                // it must be a file and it exists
                else if (File.Exists(path))
                {
                    // we need to check if extention match
                    if (extentions.Length > 0)
                    {
                        string fileExt = Path.GetExtension(path).Replace(".", "");
                        // iterate on filters
                        foreach (var ext in extentions)
                        {
                            // iterate on extentions
                            foreach (string extStr in ext.Extensions)
                            {
                                // check whatever extention is valid
                                if (extStr == "*" || extStr == fileExt)
                                {
                                    _pathFieldValues[id] = path;
                                    edited = true;
                                    return;
                                }
                            }
                        }
                    }
                    // we do not need to check extentions
                    else
                    {
                        _pathFieldValues[id] = path;
                        edited = true;
                    }
                }
            }
        }
    }
}