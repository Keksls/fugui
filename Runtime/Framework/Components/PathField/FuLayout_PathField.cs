using ImGuiNET;
using System;
using System.IO;
using UnityEngine;

namespace Fu.Framework
{
    public partial class FuLayout
    {
        public void InputFolder(string text, Action<string> callback = null, string defaultPath = null, params ExtensionFilter[] extentions)
        {
            _pathField(text, true, callback, FuFrameStyle.Default, defaultPath, extentions);
        }

        public void InputFolder(string text, FuFrameStyle style, Action<string> callback = null, string defaultPath = null, params ExtensionFilter[] extentions)
        {
            _pathField(text, true, callback, style, defaultPath, extentions);
        }

        public void InputFile(string text, Action<string> callback = null, string defaultPath = null, params ExtensionFilter[] extentions)
        {
            _pathField(text, false, callback, FuFrameStyle.Default, defaultPath, extentions);
        }

        public void InputFile(string text, FuFrameStyle style, Action<string> callback = null, string defaultPath = null, params ExtensionFilter[] extentions)
        {
            _pathField(text, false, callback, style, defaultPath, extentions);
        }

        protected virtual void _pathField(string text, bool onlyFolder, Action<string> callback, FuFrameStyle style, string defaultPath = "", params ExtensionFilter[] extentions)
        {
            // apply style and set unique ID
            beginElement(ref text, style);
            // return if item must no be draw
            if (!_drawElement)
            {
                return;
            }

            // set path if not exist in dic
            if (!_pathFieldValues.ContainsKey(text))
            {
                _pathFieldValues.Add(text, string.IsNullOrEmpty(defaultPath) ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop) : defaultPath);
            }
            string path = _pathFieldValues[text];

            // display values
            float cursorPos = ImGui.GetCursorScreenPos().x;
            float width = ImGui.GetContentRegionAvail().x;
            float buttonWidth = ImGui.CalcTextSize("...").x + 8f * Fugui.CurrentContext.Scale;

            // draw input text
            ImGui.SetNextItemWidth(width - buttonWidth);
            bool edited = false;
            // set default flag as validate when user press enter
            ImGuiInputTextFlags flags = ImGuiInputTextFlags.EnterReturnsTrue;
            // prevent user to edit disabled widget
            if(_nextIsDisabled)
            {
                flags |= ImGuiInputTextFlags.ReadOnly;
            }
            if (ImGui.InputText("##" + text, ref path, 2048, flags))
            {
                validatePath();
            }
            // set states for this element
            setBaseElementState(text, _currentItemStartPos, ImGui.GetItemRectMax() - _currentItemStartPos, true, false);
            // draw text input frame and tooltip
            _elementHoverFramedEnabled = true;
            drawHoverFrame();
            displayToolTip(false, true);

            // draw button
            ImGui.SameLine();
            ImGui.SetCursorScreenPos(new Vector2(cursorPos + width - buttonWidth, ImGui.GetCursorScreenPos().y));
            if (ImGui.Button("...##" + text, new Vector2(buttonWidth, 0)))
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
            _elementHoverFramedEnabled = true;
            endElement(style);

            if (edited)
            {
                callback?.Invoke(_pathFieldValues[text]);
            }

            void validatePath()
            {
                // it must be a directory and it exists
                if (onlyFolder && Directory.Exists(path))
                {
                    _pathFieldValues[text] = path;
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
                                    _pathFieldValues[text] = path;
                                    edited = true;
                                    return;
                                }
                            }
                        }
                    }
                    // we do not need to check extentions
                    else
                    {
                        _pathFieldValues[text] = path;
                        edited = true;
                    }
                }
            }
        }
    }
}